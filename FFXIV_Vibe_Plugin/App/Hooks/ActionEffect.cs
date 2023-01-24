using System;
using System.Collections.Generic;

#region Dalamud deps
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Data;
#endregion

#region FFXIV_Vibe_Plugin deps
using FFXIV_Vibe_Plugin.Commons;
#endregion

namespace FFXIV_Vibe_Plugin.Hooks {
  internal class ActionEffect {
    // Constructor params
    private readonly DataManager? DataManager;
    private readonly Logger Logger;
    private readonly SigScanner Scanner;
    private readonly ClientState ClientState;
    private readonly ObjectTable GameObjects;

    // Lumina excel sheet for actions.
    private readonly Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Action>? LuminaActionSheet;

    // Hooks
    private delegate void HOOK_ReceiveActionEffectDelegate(int sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);
    private Hook<HOOK_ReceiveActionEffectDelegate>? receiveActionEffectHook;

    // Event to dispatch.
    public event EventHandler<HookActionEffects_ReceivedEventArgs>? ReceivedEvent;

    // Constructor
    public ActionEffect(DataManager dataManager, Logger logger, SigScanner scanner, ClientState clientState, ObjectTable gameObjects) {
      this.DataManager = dataManager;
      this.Logger = logger;
      this.Scanner = scanner;
      this.ClientState = clientState;
      this.GameObjects = gameObjects;
      this.InitHook();
      if(DataManager != null) {
        this.LuminaActionSheet = DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>();
      }
    }

    /** Dispose the hook and disable it */
    public void Dispose() {
      receiveActionEffectHook?.Disable();
      receiveActionEffectHook?.Dispose();
    }

    private void InitHook() {
      try {
        // Found on: https://github.com/lmcintyre/DamageInfoPlugin/blob/main/DamageInfoPlugin/DamageInfoPlugin.cs#L133
        IntPtr receiveActionEffectFuncPtr = this.Scanner.ScanText("4C 89 44 24 ?? 55 56 41 54 41 55 41 56");
        receiveActionEffectHook = new Hook<HOOK_ReceiveActionEffectDelegate>(receiveActionEffectFuncPtr, ReceiveActionEffect);

      }
      catch (Exception e) {
        this.Dispose();
        this.Logger.Warn($"Encountered an error loading HookActionEffect: {e.Message}. Disabling it...");
        throw;
      }
      
      receiveActionEffectHook.Enable();
      this.Logger.Log("HookActionEffect was correctly enabled!");
    }


    unsafe private void ReceiveActionEffect(int sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail) {
      Structures.Spell spell = new();
      try {
        // Get data structure
        uint ptr_id = *((uint*)effectHeader.ToPointer() + 0x2);
        uint ptr_animId = *((ushort*)effectHeader.ToPointer() + 0xE);
        ushort ptr_op = *((ushort*)effectHeader.ToPointer() - 0x7);
        byte ptr_targetCount = *(byte*)(effectHeader + 0x21);
        Structures.EffectEntry effect = *(Structures.EffectEntry*)(effectArray);

        // Get more info from data structure
        string playerName = GetCharacterNameFromSourceId(sourceId);
        String spellName = this.GetSpellName(ptr_id, true);
        int[] amounts = this.GetAmounts(ptr_targetCount, effectArray);
        float amountAverage = ComputeAverageAmount(amounts);
        List<Structures.Player> targets = this.GetAllTarget(ptr_targetCount, effectTrail, amounts);
        

        // Spell definition
        spell.Id = (int)ptr_id;
        spell.Name = spellName;
        spell.Player = new Structures.Player(sourceId, playerName);
        spell.Amounts = amounts;
        spell.AmountAverage = amountAverage;
        spell.Targets = targets;
        spell.DamageType = Structures.DamageType.Unknown;

        // WARNING: if there is no target, some information will be wrong !
        // It is needed to avoid effect type if there is no target.
        if(targets.Count == 0) { 
          spell.ActionEffectType = Structures.ActionEffectType.Any;
        } else {
          spell.ActionEffectType = effect.type;
        }
        this.DispatchReceivedEvent(spell);
      } catch(Exception e) {
        this.Logger.Log($"{e.Message} {e.StackTrace}");
      }
      this.RestoreOriginalHook(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
      
    }

    private void RestoreOriginalHook(int sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail) {
      if(receiveActionEffectHook != null) {
        receiveActionEffectHook.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
      }
    }

    unsafe private int[] GetAmounts(byte count, IntPtr effectArray) {
      int[] RESULT = new int[count];
      int targetCount = (int)count;
      int effectsEntries = 0;

      // The packet size depends on the number of target. 
      if(targetCount == 0) {
        effectsEntries = 0;
      } else if(targetCount == 1) {
        effectsEntries = 8;
      } else if(targetCount <= 8) {
        effectsEntries = 64;
      } else if(targetCount <= 16) {
        effectsEntries = 128;
      } else if(targetCount <= 24) {
        effectsEntries = 192;
      } else if(targetCount <= 32) {
        effectsEntries = 256;
      }

      // Creates a list of EffectEntry (the base binary structure of the effect).
      List<Structures.EffectEntry> entries = new(effectsEntries);
      for(int i = 0; i < effectsEntries; i++) {
        entries.Add(*(Structures.EffectEntry*)(effectArray + i * 8));
      }

      // Sum all the damage.
      int counterValueFound = 0;
      for(int i = 0; i < entries.Count; i++) {
        // DEBUG: Logger.Debug(entries[i].ToString());
        if(i % 8 == 0) { // Value of dmg is located every 8
          uint tDmg = entries[i].value;
          if(entries[i].mult != 0) {
            tDmg += ((uint)ushort.MaxValue + 1) * entries[i].mult;
          }
          // We add the value of the damage that we found.
          if(counterValueFound < count) {
            RESULT[counterValueFound] = (int)tDmg;
          }
          counterValueFound++; 
        }
      }
      return RESULT;
    }

    private static int ComputeAverageAmount(int[] amounts) {
      var result = 0;
      for(int i=0; i < amounts.Length; i++) {
        result += amounts[i];
      }
      result = result != 0 ? result / amounts.Length : result;
      return result;
    }

    unsafe private List<Structures.Player> GetAllTarget(byte count, IntPtr effectTrail, int[] amounts) {
      List<Structures.Player> names = new();
      if((int)count >= 1) {
        ulong[] targets = new ulong[(int)count];
        for(int i=0; i < count; i++) {
          targets[i] = *(ulong*)(effectTrail + i * 8);
          var targetId = (int)targets[i];
          var targetName = this.GetCharacterNameFromSourceId(targetId);
          var targetPlayer = new Structures.Player(targetId, targetName, $"{amounts[i]}");
          names.Add(targetPlayer);
        }
      }
      return names;
    }
 
    private string GetSpellName(uint actionId, bool withId) {
      if(this.LuminaActionSheet == null) {
        this.Logger.Warn("HookActionEffect.GetSpellName: LuminaActionSheet is null");
        return "***LUMINA ACTION SHEET NOT LOADED***";  
      }
      var row = this.LuminaActionSheet.GetRow(actionId);
      var spellName = "";
      if(row != null) { 
        if(withId) {
          spellName = $"{row.RowId}:";
        }
        if(row.Name != null) {
          spellName += $"{row.Name}";
        }
      } else {
        spellName = "!Unknown Spell Name!";
      }
      return spellName;
    }

    private string GetCharacterNameFromSourceId(int sourceId) {
      var character = this.GameObjects.SearchById((uint)sourceId);
      var characterName = "";
      if(character != null) {
        characterName = character.Name.TextValue;
      }
      return characterName;
    }

    protected virtual void DispatchReceivedEvent(Structures.Spell spell) {
      HookActionEffects_ReceivedEventArgs args = new();
      args.Spell = spell;
      ReceivedEvent?.Invoke(this, args);
    }
  }

  // EventArgs data HookActionEffects_ReceivedEventArgs the 'Received' event is triggers.
  internal class HookActionEffects_ReceivedEventArgs : EventArgs {
    public Structures.Spell Spell { get; set; }
  }
}

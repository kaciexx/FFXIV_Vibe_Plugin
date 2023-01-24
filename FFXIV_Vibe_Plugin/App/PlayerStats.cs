using System;
using Dalamud.Game.ClientState;

using FFXIV_Vibe_Plugin.Commons;

namespace FFXIV_Vibe_Plugin {
  
  internal class PlayerStats {
    private readonly Logger Logger; 

    // EVENTS
    public event EventHandler? Event_CurrentHpChanged;
    public event EventHandler? Event_MaxHpChanged;

    // Stats of the player
    private float _CurrentHp, _prevCurrentHp = -1;
    private float _MaxHp, _prevMaxHp = -1;
    public string PlayerName = "*unknown*";

    public PlayerStats( Logger logger, ClientState clientState) {
      this.Logger = logger;
      this.UpdatePlayerState(clientState);
    }

    public void Update(ClientState clientState) {
      if(clientState == null || clientState.LocalPlayer == null) { return;  }
      this.UpdatePlayerState(clientState);
      this.UpdatePlayerName(clientState);
      this.UpdateCurrentHp(clientState);
    }

    public void UpdatePlayerState(ClientState clientState) {
      if(clientState != null && clientState.LocalPlayer != null) {
        if(this._CurrentHp == -1 || this._MaxHp == -1) {
          this.Logger.Debug($"UpdatePlayerState {this._CurrentHp} {this._MaxHp}");
          this._CurrentHp = this._prevCurrentHp = clientState.LocalPlayer.CurrentHp;
          this._MaxHp = this._prevMaxHp = clientState.LocalPlayer.MaxHp;
          this.Logger.Debug($"UpdatePlayerState {this._CurrentHp} {this._MaxHp}");
        }
      }
    }

    public string UpdatePlayerName(ClientState clientState) {
      if(clientState != null && clientState.LocalPlayer != null) {
        this.PlayerName = clientState.LocalPlayer.Name.TextValue;
      }
      return this.PlayerName;
    }

    public string GetPlayerName() {
      return this.PlayerName;
    }

    private void UpdateCurrentHp(ClientState clientState) {
      // Updating current values
      if(clientState != null && clientState.LocalPlayer != null) {
        this._CurrentHp = clientState.LocalPlayer.CurrentHp;
        this._MaxHp = clientState.LocalPlayer.MaxHp;
      }

      // Send events after all value updated
      if(this._CurrentHp != this._prevCurrentHp) {
        Event_CurrentHpChanged?.Invoke(this, EventArgs.Empty);
      }
      if(this._MaxHp != this._prevMaxHp) {
        Event_MaxHpChanged?.Invoke(this, EventArgs.Empty);
      }

      // Save previous values
      this._prevCurrentHp = this._CurrentHp;
      this._prevMaxHp = this._MaxHp;

    }

    /***** PUBLIC API ******/
    public float GetCurrentHP() {
      return this._CurrentHp;
    }

    public float GetMaxHP() {
      return this._MaxHp;
    }
  }
}

using System;
using Dalamud.Game.Network;
using FFXIV_Vibe_Plugin.Commons;

namespace FFXIV_Vibe_Plugin.Experimental {
  internal class NetworkCapture {

    private readonly Logger Logger;

    // NetworkCapture experiment
    private readonly GameNetwork? GameNetwork;
    private bool ExperimentalNetworkCaptureStarted = false;

    /** Constructor */
    public NetworkCapture(Logger logger, GameNetwork gameNetwork) {
      this.Logger = logger;
      this.GameNetwork = gameNetwork;
    }

    /** Dispose all experiments */
    public void Dispose() {
      this.StopNetworkCapture();
    }

    /** Monitor the network and caputre some information */
    public void StartNetworkCapture() {
      /*
      this.Logger.Debug("STARTING EXPERIMENTAL");
      this.ExperimentalNetworkCaptureStarted = true;
      if(this.GameNetwork != null) {
        this.GameNetwork.Enable();
        this.GameNetwork.NetworkMessage += this.OnNetworkReceived;
      }*/
    }

    /** Stops the network capture experiment. */
    public void StopNetworkCapture() {
      if(!this.ExperimentalNetworkCaptureStarted) { return; }
      this.Logger.Debug("STOPPING EXPERIMENTAL");
      if(this.GameNetwork != null) {
        this.GameNetwork.NetworkMessage -= this.OnNetworkReceived;
      }
      this.ExperimentalNetworkCaptureStarted = false;
    }

    /**
     * Analyze the network message when received. 
     * 1. We get the opCode
     * 2. We could retrieve the name of the OpCode using our Common.OpCodes
     * 3. If it is a ClientTrigger OpCode, we get the correct bytes using Sapphire structs
     * 4. By analyzing a bit the behavior in the game, we can clearly see that the "param11"
     *    is going from 0 to 1 when the weapon is drawn.
     */
    unsafe private void OnNetworkReceived(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
      int vOut = Convert.ToInt32(opCode);
      string? name = OpCodes.GetName(opCode);

      uint actionId = 111111111;
      if(direction == NetworkMessageDirection.ZoneUp) {
        actionId = *(uint*)(dataPtr + 0x4);
      }
      
      this.Logger.Log($"Hex: {vOut:X} Decimal: {opCode} ActionId: {actionId} SOURCE_ID: {sourceActorId} TARGET_ID: {targetActorId} DIRECTION: {direction} DATA_PTR: {dataPtr} NAME: {name}");

      if(name == "ClientZoneIpcType-ClientTrigger") {
        UInt16 commandId = *(UInt16*)(dataPtr);
        byte unk_1 = *(byte*)(dataPtr + 0x2);
        byte unk_2 = *(byte*)(dataPtr + 0x3);
        uint param11 = *(uint*)(dataPtr + 0x4);
        uint param12 = *(uint*)(dataPtr + 0x8);
        uint param2 = *(uint*)(dataPtr + 0xC);
        uint param4 = *(uint*)(dataPtr + 0x10);
        uint param5 = *(uint*)(dataPtr + 0x14);
        ulong param3 = *(ulong*)(dataPtr + 0x18);
        string extra = "";
        if(param11 == 0) {
          extra += "WeaponIn";
        } else if(param11 == 1) {
          extra += "WeaponOut";
        }
        this.Logger.Log($"{name} {direction} {extra} {commandId} {unk_1} {unk_2} {param11} {param12} {param2} {param2} {param4} {param5} {param3}");
      }

    }
  }
}

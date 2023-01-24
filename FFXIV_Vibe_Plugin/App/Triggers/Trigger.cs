using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FFXIV_Vibe_Plugin.Device;

namespace FFXIV_Vibe_Plugin.Triggers {
  public enum KIND {
    Chat,
    Spell, 
    HPChange
  }

  public enum DIRECTION {
    Any,
    Outgoing,
    Incoming,
    Self
  }

  public class Trigger : IComparable<Trigger> {
    private static readonly int _initAmountMinValue = -1;
    private static readonly int _initAmountMaxValue = 10000000;

    // General
    public bool Enabled = true;
    public int SortOder = -1;
    public readonly string Id = "";
    public string Name = "";
    public string Description = "";
    public int Kind = (int)KIND.Chat;
    public int ActionEffectType = (int)FFXIV_Vibe_Plugin.Commons.Structures.ActionEffectType.Any;
    public int Direction = (int)DIRECTION.Any;
    public string ChatText = "hello world";
    public string SpellText = "";
    public int AmountMinValue = Trigger._initAmountMinValue;
    public int AmountMaxValue = Trigger._initAmountMaxValue;
    public bool AmountInPercentage = false;
    public string FromPlayerName = "";
    public string ToPlayerName = "";
    public float StartAfter = 0;
    public float StopAfter = 0;
    public int Priority = 0;
    public readonly List<int> AllowedChatTypes = new ();

    // Devices associated with this trigger
    public List<TriggerDevice> Devices = new();

    public Trigger(string name) {
      this.Id = Guid.NewGuid().ToString();
      this.Name = name;
    }

    public override string ToString() {
      return $"Trigger(name={this.Name}, id={this.GetShortID()})";
    }

    public int CompareTo(Trigger? other) {
      if(other == null) { return 1; }
      if(this.SortOder < other.SortOder) {
        return 1;
      } else if(this.SortOder > other.SortOder) {
        return -1;
      } else {
        return 0;
      }
    }

    public string GetShortID() {
      return this.Id[..5];
    }

    public void Reset() {
      this.AmountMaxValue = Trigger._initAmountMaxValue;
      this.AmountMinValue = Trigger._initAmountMinValue;
    }
  }

  public class TriggerDevice {
    public string Name = "";
    public bool IsEnabled = false;
    
    public bool ShouldVibrate = false;
    public bool ShouldRotate = false;
    public bool ShouldLinear = false;
    public bool ShouldStop = false;

    public Device.Device? Device;

    // Vibrate states per motor
    public bool[] VibrateSelectedMotors;
    public int[] VibrateMotorsThreshold;
    public int[] VibrateMotorsPattern;

    // Rotate states per motor
    public bool[] RotateSelectedMotors;
    public int[] RotateMotorsThreshold;
    public int[] RotateMotorsPattern;

    // Linear states per motor
    public bool[] LinearSelectedMotors;
    public int[] LinearMotorsThreshold;
    public int[] LinearMotorsPattern;

    public TriggerDevice(Device.Device device) {
      this.Name = device.Name;
      this.Device = device;

      // Init vibration array
      this.VibrateSelectedMotors = new bool[device.CanVibrate ? device.VibrateMotors : 0];
      this.VibrateMotorsThreshold = new int[device.CanVibrate ? device.VibrateMotors : 0];
      this.VibrateMotorsPattern = new int[device.CanVibrate ? device.VibrateMotors : 0];

      // Init rotate array
      this.RotateSelectedMotors = new bool[device.CanRotate ? device.RotateMotors : 0];
      this.RotateMotorsThreshold = new int[device.CanRotate ? device.RotateMotors : 0];
      this.RotateMotorsPattern = new int[device.CanRotate ? device.RotateMotors : 0];

      // Init linear array
      this.LinearSelectedMotors = new bool[device.CanLinear ? device.LinearMotors : 0];
      this.LinearMotorsThreshold = new int[device.CanLinear ? device.LinearMotors : 0];
      this.LinearMotorsPattern = new int[device.CanLinear ? device.LinearMotors : 0];
    }

    public override string ToString() {
      return $"TRIGGER_DEVICE {this.Name}";
    }
  }
}

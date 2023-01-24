using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FFXIV_Vibe_Plugin.Commons;

using Buttplug;

namespace FFXIV_Vibe_Plugin.Device {
  public enum UsableCommand {
    Vibrate,
    Rotate,
    Linear,
    Stop
  }

  public class Device {
    private readonly ButtplugClientDevice? ButtplugClientDevice;
    public int Id = -1;
    public string Name = "UnsetDevice";
    public bool CanVibrate = false;
    public int VibrateMotors = -1;
    public uint[] VibrateSteps = Array.Empty<uint>();
    public bool CanRotate = false;
    public int RotateMotors = -1;
    public uint[] RotateSteps = Array.Empty<uint>();
    public bool CanLinear = false;
    public int LinearMotors = -1;
    public uint[] LinearSteps = Array.Empty<uint>();
    public bool CanBattery = false;
    public bool CanStop = false;    
    public bool IsConnected = false;
    public double BatteryLevel = -1;
    // TODO: use that ?
    public List<UsableCommand> UsableCommands = new();

    public int[] CurrentVibrateIntensity = Array.Empty<int>();
    public int[] CurrentRotateIntensity = Array.Empty<int>();
    public int[] CurrentLinearIntensity = Array.Empty<int>();

    public Device(ButtplugClientDevice buttplugClientDevice) {
      if(buttplugClientDevice != null) {
        this.ButtplugClientDevice = buttplugClientDevice;
        Id = (int)buttplugClientDevice.Index;
        Name = buttplugClientDevice.Name;
        this.SetCommands();
        this.ResetMotors();
        this.UpdateBatteryLevel();
      }
    }

    public override string ToString() {
      List<string> commands = this.GetCommandsInfo();
      return $"Device: {Id}:{Name} (connected={IsConnected}, battery={GetBatteryPercentage()}, commands={String.Join(",", commands)})";
    }

    private void SetCommands() {
      if(this.ButtplugClientDevice == null) { return; }
      foreach(var cmd in this.ButtplugClientDevice.AllowedMessages) {
        if(cmd.Key == ServerMessage.Types.MessageAttributeType.VibrateCmd) {
          this.CanVibrate = true;
          this.VibrateMotors = (int)cmd.Value.FeatureCount;
          this.VibrateSteps = cmd.Value.StepCount;
          this.UsableCommands.Add(UsableCommand.Vibrate);
        } else if(cmd.Key == ServerMessage.Types.MessageAttributeType.RotateCmd) {
          this.CanRotate = true;
          this.RotateMotors = (int)cmd.Value.FeatureCount;
          this.RotateSteps = cmd.Value.StepCount;
          this.UsableCommands.Add(UsableCommand.Rotate);
        } else if(cmd.Key == ServerMessage.Types.MessageAttributeType.LinearCmd) {
          this.CanLinear = true;
          this.LinearMotors = (int)cmd.Value.FeatureCount;
          this.LinearSteps = cmd.Value.StepCount;
          this.UsableCommands.Add(UsableCommand.Linear);
        } else if(cmd.Key == ServerMessage.Types.MessageAttributeType.BatteryLevelCmd) {
          this.CanBattery = true;
        } else if(cmd.Key == ServerMessage.Types.MessageAttributeType.StopDeviceCmd) {
          this.CanStop = true;
          this.UsableCommands.Add(UsableCommand.Stop);
        }
      }
    }

    /** Init all current motors intensity and default to zero */
    private void ResetMotors() {
      if(this.CanVibrate) {
        this.CurrentVibrateIntensity = new int[this.VibrateMotors];
        for(int i=0; i<this.VibrateMotors; i++) { this.CurrentVibrateIntensity[i] = 0; };
      }
      if(this.CanRotate) {
        this.CurrentRotateIntensity = new int[this.RotateMotors];
        for(int i = 0; i < this.RotateMotors; i++) { this.CurrentRotateIntensity[i] = 0; };
      }
      if(this.CanLinear) {
        this.CurrentLinearIntensity = new int[this.LinearMotors];
        for(int i = 0; i < this.LinearMotors; i++) { this.CurrentLinearIntensity[i] = 0; };
      }
    }

    public List<UsableCommand> GetUsableCommands() {
      return this.UsableCommands;
    }

    public List<String> GetCommandsInfo() {
      List<string> commands = new();
      if(CanVibrate) {
        commands.Add($"vibrate motors={VibrateMotors} steps={String.Join(",", VibrateSteps)}");
      }
      if(CanRotate) {
        commands.Add($"rotate motors={RotateMotors} steps={String.Join(",", RotateSteps)}");
      }
      if(CanLinear) {
        commands.Add($"rotate motors={LinearMotors} steps={String.Join(",", LinearSteps)}");
      }
      if(CanBattery) commands.Add("battery");
      if(CanStop) commands.Add("stop");
      return commands;
    }


    public double UpdateBatteryLevel() {
      if(this.ButtplugClientDevice == null) { return 0; }
      if(!CanBattery) {return -1; }
      Task<double> batteryLevelTask = this.ButtplugClientDevice.SendBatteryLevelCmd();
      batteryLevelTask.Wait();
      this.BatteryLevel = batteryLevelTask.Result;
      return this.BatteryLevel;
    }

    public string GetBatteryPercentage() {
      return $"{this.BatteryLevel*100}%";
    }

    public void Stop() {
      if(this.ButtplugClientDevice == null) { return; }
      if(CanVibrate) {
        this.ButtplugClientDevice.SendVibrateCmd(0);
      }
      if(CanRotate) {
        this.ButtplugClientDevice.SendRotateCmd(0f, true);
      }
      if(CanStop) {
        this.ButtplugClientDevice.SendStopDeviceCmd();
      }
      ResetMotors();
    }

    public void SendVibrate(int intensity, int motorId=-1, int threshold=100) {
      if(this.ButtplugClientDevice == null) return;
      if(this.ButtplugClientDevice == null) { return; }
      if(!CanVibrate || !IsConnected) return;
      Dictionary<uint, double> motorIntensity = new();
      for(int i=0; i < this.VibrateMotors; i++) {
        if(motorId == -1 || motorId == i) {
          this.CurrentVibrateIntensity[i] = intensity;
          motorIntensity.Add((uint)i, Helpers.ClampIntensity(intensity, threshold) / 100.0);
        }
      }
      this.ButtplugClientDevice.SendVibrateCmd(motorIntensity);
    }

    public void SendRotate(int intensity, bool clockWise=true, int motorId=-1, int threshold = 100) {
      if(this.ButtplugClientDevice == null) return;  
      if(!CanRotate || !IsConnected) return;
      Dictionary<uint, (double, bool)> motorIntensity = new();
      for(int i = 0; i < this.RotateMotors; i++) {
        if(motorId == -1 || motorId == i) {
          this.CurrentRotateIntensity[i] = intensity;
          (double, bool) values = (Helpers.ClampIntensity(intensity, threshold) / 100.0, clockWise);
          motorIntensity.Add((uint)i, values);
        }
      }
      
      this.ButtplugClientDevice.SendRotateCmd(motorIntensity);
    }

    public void SendLinear(int intensity, int duration=500, int motorId = -1, int threshold = 100) {
      if(this.ButtplugClientDevice == null) return;
      if(!CanLinear || !IsConnected) return;
      Dictionary<uint, (uint, double)> motorIntensity = new();
      for(int i = 0; i < this.LinearMotors; i++) {
        if(motorId == -1 || motorId == i) {
          this.CurrentLinearIntensity[i] = intensity;
          (uint, double) values = ((uint)duration, Helpers.ClampIntensity(intensity, threshold) / 100.0);
          motorIntensity.Add((uint)i, values);
        }
      }
      this.ButtplugClientDevice.SendLinearCmd(motorIntensity);
    }
  }
}

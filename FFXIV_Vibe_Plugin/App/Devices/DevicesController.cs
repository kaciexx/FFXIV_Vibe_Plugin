using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#region FFXIV_Vibe_Plugin deps
using FFXIV_Vibe_Plugin.Commons;
#endregion

#region Other deps
using Buttplug;
#endregion

namespace FFXIV_Vibe_Plugin.Device {
  public class DevicesController {
    private readonly Logger Logger;
    private readonly Configuration Configuration;
    private ConfigurationProfile Profile;
    private readonly Patterns Patterns;
    private Triggers.Trigger? CurrentPlayingTrigger;

    /**
     * State of the current device and motor when it started to play as a unix timestamp.
     * This is used to detect if a thread that runs a pattern should stop
     */
    private readonly Dictionary<string, int> CurrentDeviceAndMotorPlaying = new();

    // Buttplug related
    private ButtplugClient? BPClient;
    private readonly List<Device> Devices = new();
    private bool isScanning = false;

    // Internal variables
    private readonly static Mutex mut = new();

    public DevicesController(Logger logger, Configuration configuration, ConfigurationProfile profile, Patterns patterns) {
      this.Logger = logger;
      this.Configuration = configuration;
      this.Profile = profile;
      this.Patterns = patterns;
    }

    public void Dispose() {
      this.Disconnect();
    }

    public void SetProfile(ConfigurationProfile profile) {
      this.Profile = profile;
    }

    public void Connect(String host, int port) {
      if(this.IsConnected()) {
        this.Logger.Debug("Disconnecting previous instance! Waiting 2sec...");
        this.Disconnect();
        Thread.Sleep(200);
      }

      try {
        this.BPClient = new("bp-dalamud");
      } catch(Exception e) {
        this.Logger.Error($"Can't load bp.", e);
        return;
      }
      this.BPClient.ServerDisconnect += BPClient_ServerDisconnected;
      this.BPClient.DeviceAdded += BPClient_DeviceAdded;
      this.BPClient.DeviceRemoved += BPClient_DeviceRemoved;
      this.BPClient.ScanningFinished += BPClient_OnScanComplete;
      string hostandport = host + ":" + port.ToString();


      try {
        var uri = new Uri($"ws://{hostandport}/buttplug");
        var connector = new ButtplugWebsocketConnectorOptions(uri);
        this.Logger.Log($"Connecting to {hostandport}.");
        Task task = this.BPClient.ConnectAsync(connector);
        task.Wait();
        this.ScanDevice();
      } catch(Exception e) {
        this.Logger.Error($"Could not connect to {hostandport}.", e);
      }

      Thread.Sleep(200);

      if(this.BPClient.Connected) {
        this.Logger.Log($"FVP connected to Intiface!");
      } else {
        this.Logger.Error("Failed connecting (Intiface server is up?)");
        return;
      }
    }

    private void BPClient_ServerDisconnected(object? sender, EventArgs e) {
      this.Logger.Debug("Server disconnected");
      this.Disconnect();
    }

    public bool IsConnected() {
      bool isConnected = false;
      if(this.BPClient != null) {
        isConnected = this.BPClient.Connected;
      }
      return isConnected;
    }

    public void ScanDevice() {
      if(this.BPClient == null) { return; }
      this.Logger.Debug("Scanning for devices...");
      if(this.IsConnected()) {
        try {
          this.isScanning = true;
          var task = this.BPClient.StartScanningAsync();
          task.Wait();
        } catch(Exception e) {
          this.isScanning = false;
          this.Logger.Error("Scanning issue. No 'Device Comm Managers' enabled on Intiface?");
          this.Logger.Error(e.Message);
        }
      }

    }
    public bool IsScanning() {
      return this.isScanning;
    }

    public void StopScanningDevice() {
      if(this.BPClient != null && this.IsConnected()) {
        try {
          Task task = this.BPClient.StopScanningAsync();
          task.Wait();
        } catch(Exception) {
          this.Logger.Debug("StopScanningDevice ignored: already stopped");
        }
      }
      this.isScanning = false;
    }

    private void BPClient_OnScanComplete(object? sender, EventArgs e) {
      this.Logger.Debug("Stop scanning...");
      // FIXME: this is not working, bp client emit the trigger instantly. Let's ignore for the moment.
      // this.isScanning = false;
    }

    private void BPClient_DeviceAdded(object? sender, DeviceAddedEventArgs arg) {
      try {
        mut.WaitOne();
        ButtplugClientDevice BPClientDevice = arg.Device;
        Device device = new(BPClientDevice);
        device.IsConnected = true;
        this.Logger.Log($"{arg.Device.Name}, {BPClientDevice.Name}");
        this.Devices.Add(device);
        if(!this.Profile.VISITED_DEVICES.ContainsKey(device.Name)) {
          this.Profile.VISITED_DEVICES[device.Name] = device;
          this.Configuration.Save();
          this.Logger.Debug($"Adding device to visited list {device})");
        }
        this.Logger.Debug($"Added {device})");
      } finally {
        mut.ReleaseMutex();
      }
    }

    private void BPClient_DeviceRemoved(object? sender, DeviceRemovedEventArgs e) {
      try {
        mut.WaitOne();
        int index = this.Devices.FindIndex(device => device.Id == e.Device.Index);
        if(index > -1) {
          this.Logger.Debug($"Removed {Devices[index]}");
          Device device = Devices[index];
          this.Devices.RemoveAt(index);
          device.IsConnected = false;
        }

      } finally {
        mut.ReleaseMutex();
      }
    }

    public void Disconnect() {
      this.Devices.Clear();
      if(this.BPClient == null || !this.IsConnected()) {
        return;
      }
      try {
        if(this.BPClient.IsScanning) {
          var task = this.BPClient.StopScanningAsync();
          task.Wait();
        }
      } catch(Exception e) {
        this.Logger.Error("Couldn't stop scanning device... Unknown reason.");
        this.Logger.Error(e.Message);
      }
      try {
        for(int i = 0; i < this.BPClient.Devices.Length; i++) {
          this.Logger.Log($"Disconnecting device {i} {this.BPClient.Devices[i].Name}");
          this.BPClient.Devices[i].Dispose();
        }
      } catch(Exception e) {
        this.Logger.Error("Error while disconnecting device", e);
      }
      try {
        Thread.Sleep(1000);
        if(this.BPClient != null) {
          this.BPClient.DisconnectAsync();
          this.Logger.Log("Disconnecting! Bye... Waiting 2sec...");
        }
      } catch(Exception e) {
        // ignore exception, we are trying to do our best
        this.Logger.Error("Error while disconnecting client", e);
      }
      this.BPClient = null;

    }

    public List<Device> GetDevices() {
      return this.Devices;
    }

    public Dictionary<String, Device> GetVisitedDevices() {
      return this.Profile.VISITED_DEVICES;
    }

    public void UpdateAllBatteryLevel() {
      foreach(Device device in this.GetDevices()) {
        device.UpdateBatteryLevel();
      }
    }

    public void StopAll() {
      foreach(Device device in this.GetDevices()) {
        device.Stop();
      }
    }

    public void SendTrigger(Triggers.Trigger trigger, int threshold=100) {
      if(!this.IsConnected()) {
        this.Logger.Debug($"Not connected, cannot send ${trigger}");
        return;
      }
      this.Logger.Debug($"Sending trigger {trigger} (priority={trigger.Priority})");

      // Check if the trigger has the priority
      if(this.CurrentPlayingTrigger == null) {
        this.CurrentPlayingTrigger = trigger;
      }
      if(trigger.Priority < this.CurrentPlayingTrigger.Priority) {
        this.Logger.Debug($"Ignoring trigger because lower priority => {trigger} < {this.CurrentPlayingTrigger}");
        return;
      }
      this.CurrentPlayingTrigger = trigger;

      foreach(Triggers.TriggerDevice triggerDevice in trigger.Devices) {
        Device? device = this.FindDevice(triggerDevice.Name);
        if(device != null && triggerDevice != null) {

          if(triggerDevice.ShouldVibrate) {
            for(int motorId = 0; motorId < triggerDevice.VibrateSelectedMotors?.Length; motorId++) {
              if(triggerDevice.VibrateSelectedMotors != null && triggerDevice.VibrateMotorsThreshold != null) {
                bool motorEnabled = triggerDevice.VibrateSelectedMotors[motorId];
                int motorThreshold = triggerDevice.VibrateMotorsThreshold[motorId] * threshold / 100;
                int motorPatternId = triggerDevice.VibrateMotorsPattern[motorId];
                float startAfter = trigger.StartAfter;
                float stopAfter = trigger.StopAfter;
                if(motorEnabled) {
                  this.Logger.Debug($"Sending {device.Name} vibration to motor: {motorId} patternId={motorPatternId} with threshold: {motorThreshold}!");
                  this.SendPattern("vibrate", device, motorThreshold, motorId, motorPatternId, startAfter, stopAfter);
                }
              }
            }
          }
          if(triggerDevice.ShouldRotate) {
            for(int motorId = 0; motorId < triggerDevice.RotateSelectedMotors?.Length; motorId++) {
              if(triggerDevice.RotateSelectedMotors != null && triggerDevice.RotateMotorsThreshold != null) {
                bool motorEnabled = triggerDevice.RotateSelectedMotors[motorId];
                int motorThreshold = triggerDevice.RotateMotorsThreshold[motorId] * threshold / 100;
                int motorPatternId = triggerDevice.RotateMotorsPattern[motorId];
                float startAfter = trigger.StartAfter;
                float stopAfter = trigger.StopAfter;
                if(motorEnabled) {
                  this.Logger.Debug($"Sending {device.Name} rotation to motor: {motorId} patternId={motorPatternId} with threshold: {motorThreshold}!");
                  this.SendPattern("rotate", device, motorThreshold, motorId, motorPatternId, startAfter, stopAfter);
                }
              }
            }
          }
          if(triggerDevice.ShouldLinear) {
            for(int motorId = 0; motorId < triggerDevice.LinearSelectedMotors?.Length; motorId++) {
              if(triggerDevice.LinearSelectedMotors != null && triggerDevice.LinearMotorsThreshold != null) {
                bool motorEnabled = triggerDevice.LinearSelectedMotors[motorId];
                int motorThreshold = triggerDevice.LinearMotorsThreshold[motorId] * threshold / 100;
                int motorPatternId = triggerDevice.LinearMotorsPattern[motorId];
                float startAfter = trigger.StartAfter;
                float stopAfter = trigger.StopAfter;
                if(motorEnabled) {
                  this.Logger.Debug($"Sending {device.Name} linear to motor: {motorId} patternId={motorPatternId} with threshold: {motorThreshold}!");
                  this.SendPattern("linear", device, motorThreshold, motorId, motorPatternId, startAfter, stopAfter);
                }
              }
            }
          }
          if(triggerDevice.ShouldStop) {
            this.Logger.Debug($"Sending stop to {device.Name}!");
            DevicesController.SendStop(device);
          }
        }
      }
    }

    /** Search for a device with the corresponding text */
    public Device? FindDevice(string text) {
      Device? foundDevice = null;
      foreach(Device device in this.Devices) {
        if(device.Name.Contains(text) && device != null) {
          foundDevice = device;
        }
      }
      return foundDevice;
    }

    /**
     * Sends an itensity vibe to all of the devices 
     * @param {float} intensity
     */
    public void SendVibeToAll(int intensity) {
      if(this.IsConnected() && this.BPClient != null) {
        foreach(Device device in this.Devices) {
          device.SendVibrate(intensity, -1, this.Profile.MAX_VIBE_THRESHOLD);
          device.SendRotate(intensity, true, -1, this.Profile.MAX_VIBE_THRESHOLD);
          device.SendLinear(intensity, 500, -1, this.Profile.MAX_VIBE_THRESHOLD);
        }
      }
    }


    public void SendPattern(string command, Device device, int threshold, int motorId = -1, int patternId = 0, float StartAfter = 0, float StopAfter = 0) {
      this.SaveCurrentMotorAndDevicePlayingState(device, motorId);
      Pattern pattern = Patterns.GetPatternById(patternId);

      string[] patternSegments = pattern.Value.Split("|");
      this.Logger.Log($"SendPattern '{command}' pattern={pattern.Name} ({patternSegments.Length} segments) to {device} motor={motorId} startAfter={StartAfter} stopAfter={StopAfter} threshold={threshold}");
      
      string deviceAndMotorId = $"{device.Name}:{motorId}";
      int startedUnixTime = this.CurrentDeviceAndMotorPlaying[deviceAndMotorId];

      // Make sure things stops if StopAfter is set by sending a zero. 
      // We make sure to send the zero to the correct device and if it is still running.
      bool forceStop = false;
      Thread tStopAfter = new(delegate () {
        if(StopAfter == 0) { return; }
        Thread.Sleep((int)StopAfter * 1000);
        if(startedUnixTime == this.CurrentDeviceAndMotorPlaying[deviceAndMotorId]) {
          forceStop = true;
          this.SendCommand(command, device, 0, motorId);
          this.Logger.Debug($"Force stopping {deviceAndMotorId} because of StopAfter={StopAfter}");
        }
      });
      tStopAfter.Start();

      Thread t = new(delegate () {
        
        Thread.Sleep((int)StartAfter * 1000);
        
        // Stop exectution if a new pattern is sent to the same device and motor.
        if(startedUnixTime != this.CurrentDeviceAndMotorPlaying[deviceAndMotorId]) {
          return;
        }

        // Experimental send a fake command to activate connection
        this.SendCommand(command, device, 0, motorId);
        Thread.Sleep(50); // Yield if necessary

        for(int segIndex = 0; segIndex < patternSegments.Length; segIndex++) {

          // Stop exectution if a new pattern is send to the same device and motor.
          if(startedUnixTime != this.CurrentDeviceAndMotorPlaying[deviceAndMotorId]) {
            break;
          }

          string patternSegment = patternSegments[segIndex];
          string[] patternValues = patternSegment.Split(":");
          int intensity = Helpers.ClampIntensity(Int32.Parse(patternValues[0]), threshold);
          int duration = Int32.Parse(patternValues[1]);
          //this.Logger.Debug($"SENDING SEGMENT: intensity={intensity} duration={duration}");
          
          // Stop after and send 0 intensity
          if(forceStop || (StopAfter > 0 && StopAfter * 1000 + startedUnixTime < Helpers.GetUnix())) {
            this.SendCommand(command, device, 0, motorId, duration);
            break;
          }

          // Send the command \o/
          this.SendCommand(command, device, intensity, motorId, duration);

          Thread.Sleep(duration);
        }

        // Make sure we clean the current playing trigger.
        this.CurrentPlayingTrigger = null;
      });
      t.Start();
    }

    public void SendCommand(string command, Device device, int intensity, int motorId, int duration=500) {
      if(command == "vibrate") {
        this.SendVibrate(device, intensity, motorId);
      } else if(command == "rotate") {
        this.SendRotate(device, intensity, motorId);
      } else if(command == "linear") {
        this.SendLinear(device, intensity, motorId, duration);
      }
    }

    public void SendVibrate(Device device, int intensity, int motorId = -1) {
      device.SendVibrate(intensity, motorId, this.Profile.MAX_VIBE_THRESHOLD);
    }

    public void SendRotate(Device device, int intensity, int motorId = -1, bool clockwise = true) {
      device.SendRotate(intensity, clockwise, motorId, this.Profile.MAX_VIBE_THRESHOLD);
    }

    public void SendLinear(Device device, int intensity, int motorId = -1, int duration = 500) {
      device.SendLinear(intensity, duration, motorId, this.Profile.MAX_VIBE_THRESHOLD);
    }

    public static void SendStop(Device device) {
      device.Stop();
    }

    private void SaveCurrentMotorAndDevicePlayingState(Device device, int motorId) {
      string deviceAndMotorId = $"{device.Name}:{motorId}";
      this.CurrentDeviceAndMotorPlaying[deviceAndMotorId] = Helpers.GetUnix();
    }
  }
}

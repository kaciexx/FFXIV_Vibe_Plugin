using System;
using System.Numerics;

using ImGuiNET;
using Dalamud.Interface.Colors;

using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Device;

namespace FFXIV_Vibe_Plugin.UI {
  internal class UIConnect {

    public static void Draw(Configuration configuration, ConfigurationProfile configurationProfile, App plugin, DevicesController devicesController) {
      ImGui.Spacing();
      ImGui.TextColored(ImGuiColors.DalamudViolet, "Server address & port");
      ImGui.BeginChild("###Server", new Vector2(-1, 40f), true);
      {

        // Connect/disconnect button
        string config_BUTTPLUG_SERVER_HOST = configurationProfile.BUTTPLUG_SERVER_HOST;
        ImGui.SetNextItemWidth(200);
        if(ImGui.InputText("##serverHost", ref config_BUTTPLUG_SERVER_HOST, 99)) {
          configurationProfile.BUTTPLUG_SERVER_HOST = config_BUTTPLUG_SERVER_HOST.Trim().ToLower();
          configuration.Save();
        }

        ImGui.SameLine();
        int config_BUTTPLUG_SERVER_PORT = configurationProfile.BUTTPLUG_SERVER_PORT;
        ImGui.SetNextItemWidth(100);
        if(ImGui.InputInt("##serverPort", ref config_BUTTPLUG_SERVER_PORT, 10)) {
          configurationProfile.BUTTPLUG_SERVER_PORT = config_BUTTPLUG_SERVER_PORT;
          configuration.Save();
        }
      }
      ImGui.EndChild();

      ImGui.Spacing();
      ImGui.BeginChild("###Main_Connection", new Vector2(-1, 40f), true);
      {
        if(!devicesController.IsConnected()) {
          if(ImGui.Button("Connect", new Vector2(100, 24))) {
            plugin.Command_DeviceController_Connect();
          }
        } else {
          if(ImGui.Button("Disconnect", new Vector2(100, 24))) {
            devicesController.Disconnect();
          }
        }

        // Checkbox AUTO_CONNECT
        ImGui.SameLine();
        bool config_AUTO_CONNECT = configurationProfile.AUTO_CONNECT;
        if(ImGui.Checkbox("Automatically connects. ", ref config_AUTO_CONNECT)) {
          configurationProfile.AUTO_CONNECT = config_AUTO_CONNECT;
          configuration.Save();
        }
      }
      ImGui.EndChild();
    }
  }
}

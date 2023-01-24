using System;
using Dalamud.Interface.Components;
using System.Diagnostics;
using ImGuiNET;
using FFXIV_Vibe_Plugin.Commons;

namespace FFXIV_Vibe_Plugin.UI.Components {
  internal class ButtonLink {

    public static void Draw(string text, string link, Dalamud.Interface.FontAwesomeIcon Icon, Logger Logger) {
      if(ImGuiComponents.IconButton(Icon)) {
        try {
          _ = Process.Start(new ProcessStartInfo() {
            FileName = link,
            UseShellExecute = true,
          });
        } catch(Exception e) {
          Logger.Error($"Could not open repoUrl: {link}", e);
        }
      }

      if(ImGui.IsItemHovered()) { ImGui.SetTooltip(text); }
    }
  }
}


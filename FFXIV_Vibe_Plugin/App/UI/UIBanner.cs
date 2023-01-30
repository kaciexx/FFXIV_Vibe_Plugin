using System;
using System.Linq;
using System.Numerics;

using ImGuiNET;
using Dalamud.Interface.Colors;

using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Device;


namespace FFXIV_Vibe_Plugin.UI {
  internal class UIBanner {
    public static void Draw(int frameCounter, Logger logger, ImGuiScene.TextureWrap image, String donationLink, String patreonLink, DevicesController devicesController) {
      ImGui.Columns(2, "###main_header", false);
      float logoScale = 0.2f;
      ImGui.SetColumnWidth(0, (int)(image.Width * logoScale + 20));
      ImGui.Image(image.ImGuiHandle, new Vector2(image.Width * logoScale, image.Height * logoScale));
      ImGui.NextColumn();
      if(devicesController.IsConnected()) {
        int nbrDevices = devicesController.GetDevices().Count;
        ImGui.TextColored(ImGuiColors.ParsedGreen, "Your are connected!");
        ImGui.Text($"Number of device(s): {nbrDevices}");
      } else {
        ImGui.TextColored(ImGuiColors.ParsedGrey, "Your are not connected!");
      }

      if(frameCounter < 200) { // Make blink effect
        ImGui.Text("Donations: ");
      } else {
        ImGui.Text("                     ");
      }
      ImGui.SameLine();
      ImGui.Text($"{donationLink}");
      ImGui.SameLine();
      UI.Components.ButtonLink.Draw("Thanks for the donation ;)", donationLink, Dalamud.Interface.FontAwesomeIcon.Pray, logger);
      ImGui.SameLine();
      ImGui.Text($"{patreonLink}");
      ImGui.SameLine();
      UI.Components.ButtonLink.Draw("Thanks for the donation ;)", patreonLink, Dalamud.Interface.FontAwesomeIcon.HollyBerry, logger);
      

    }
  }
}

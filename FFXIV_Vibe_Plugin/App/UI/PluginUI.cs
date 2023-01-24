using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Plugin;
using FFXIV_Vibe_Plugin.Commons;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;


namespace FFXIV_Vibe_Plugin {

  class PluginUI : IDisposable {

    private int frameCounter = 0;

    private readonly DalamudPluginInterface PluginInterface;
    private readonly Configuration Configuration;
    private ConfigurationProfile ConfigurationProfile;
    private readonly Device.DevicesController DevicesController;
    private readonly Triggers.TriggersController TriggerController;
    private readonly App app;
    private readonly Logger Logger;

    // Images
    private readonly Dictionary<string, ImGuiScene.TextureWrap> loadedImages = new();

    // Patterns
    private readonly Patterns Patterns = new();

    private readonly string DonationLink = "http://paypal.me/kaciedev";

    // this extra bool exists for ImGui, since you can't ref a property
    private bool visible = false;
    public bool Visible {
      get { return this.visible; }
      set { this.visible = value; }
    }
    private bool _expandedOnce = false;
    private readonly int WIDTH = 700;
    private readonly int HEIGHT = 800;
    private readonly int COLUMN0_WIDTH = 130;

    private string _tmp_void = "";

    // The value to send as a test for vibes.
    private int simulator_currentAllIntensity = 0;

    // Temporary UI values
    private int TRIGGER_CURRENT_SELECTED_DEVICE = -1;
    private string CURRENT_TRIGGER_SELECTOR_SEARCHBAR = "";
    private int _tmp_currentDraggingTriggerIndex = -1;

    // Custom Patterns
    readonly string VALID_REGEXP_PATTERN = "^(\\d+:\\d+)+(\\|\\d+:\\d+)*$";
    string CURRENT_PATTERN_SEARCHBAR = "";
    string _tmp_currentPatternNameToAdd = "";
    string _tmp_currentPatternValueToAdd = "";
    string _tmp_currentPatternValueState = "unset"; // unset|valid|unvalid

    // Profile
    string _tmp_currentProfileNameToAdd = "";
    string _tmp_currentProfile_ErrorMsg = "";

    // Some limits
    private readonly int TRIGGER_MIN_AFTER = 0;
    private readonly int TRIGGER_MAX_AFTER = 120;


    // Trigger
    private Triggers.Trigger? SelectedTrigger = null;
    private string triggersViewMode = "default"; // default|edit|delete;

    /** Constructor */
    public PluginUI(
      App currentPlugin,
      Logger logger,
      DalamudPluginInterface pluginInterface,
      Configuration configuration,
      ConfigurationProfile profile,
      Device.DevicesController deviceController,
      Triggers.TriggersController triggersController,
      Patterns Patterns
    ) {
      this.Logger = logger;
      this.Configuration = configuration;
      this.ConfigurationProfile = profile;
      this.PluginInterface = pluginInterface;
      this.app = currentPlugin;
      this.DevicesController = deviceController;
      this.TriggerController = triggersController;
      this.Patterns = Patterns;
      this.LoadImages();
    }

    public void Display() {
      this.Visible = true;
      this._expandedOnce = false;
    }

    /**
     * Function that will load all the images so that they are usable.
     * Don't forget to add the image into the project file.
     */
    private void LoadImages() {
      List<string> images = new();
      images.Add("logo.png");

      string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
      foreach(string img in images) {
        string imagePath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, $"Data\\Images\\{img}");
        this.loadedImages.Add(img, this.PluginInterface.UiBuilder.LoadImage(imagePath));
      }
    }

    public void Dispose() {
      // Dispose all loaded images.
      foreach(KeyValuePair<string, ImGuiScene.TextureWrap> img in this.loadedImages) {
        if(img.Value != null) img.Value.Dispose();
      }
    }

    public void SetProfile(ConfigurationProfile profile) {
      this.ConfigurationProfile = profile;
    }

    public void Draw() {
      // This is our only draw handler attached to UIBuilder, so it needs to be
      // able to draw any windows we might have open.
      // Each method checks its own visibility/state to ensure it only draws when
      // it actually makes sense.
      // There are other ways to do this, but it is generally best to keep the number of
      // draw delegates as low as possible.
      DrawMainWindow();
      frameCounter = (frameCounter+1) % 400;

    }

    public void DrawMainWindow() {
      if(!Visible) {
        return;
      }
      if(!this._expandedOnce) {
        ImGui.SetNextWindowCollapsed(false);
        this._expandedOnce = true;
      }

      ImGui.SetNextWindowPos(new Vector2(100, 100), ImGuiCond.Appearing);
      ImGui.SetNextWindowSize(new Vector2(this.WIDTH, this.HEIGHT), ImGuiCond.Appearing);
      ImGui.SetNextWindowSizeConstraints(new Vector2(this.WIDTH, this.HEIGHT), new Vector2(float.MaxValue, float.MaxValue));
      if(ImGui.Begin("FFXIV Vibe Plugin", ref this.visible, ImGuiWindowFlags.None)) {
        ImGui.Spacing();

        FFXIV_Vibe_Plugin.UI.UIBanner.Draw(this.frameCounter, this.Logger, this.loadedImages["logo.png"], this.DonationLink, this.DevicesController);

        // Back to on column
        ImGui.Columns(1);

        // Tab header
        if(ImGui.BeginTabBar("##ConfigTabBar", ImGuiTabBarFlags.None)) {
          if(ImGui.BeginTabItem("Connect")) {
            FFXIV_Vibe_Plugin.UI.UIConnect.Draw(this.Configuration, this.ConfigurationProfile, this.app, this.DevicesController);
            ImGui.EndTabItem();
          }

          if(ImGui.BeginTabItem("Options")) {
            this.DrawOptionsTab();
            ImGui.EndTabItem();
          }
          if(ImGui.BeginTabItem("Devices")) {
            this.DrawDevicesTab();
            ImGui.EndTabItem();
          }
          if(ImGui.BeginTabItem("Triggers")) {
            this.DrawTriggersTab();
            ImGui.EndTabItem();
          }
          if(ImGui.BeginTabItem("Patterns")) {
            this.DrawPatternsTab();
            ImGui.EndTabItem();
          }
          if(ImGui.BeginTabItem("Help")) {
            this.DrawHelpTab();
            ImGui.EndTabItem();
          }
        }
      }

      ImGui.End();
    }

    public void DrawOptionsTab() {
      ImGui.TextColored(ImGuiColors.DalamudViolet, "Profile settings");
      float CONFIG_PROFILE_ZONE_HEIGHT = this._tmp_currentProfile_ErrorMsg == "" ? 100f : 120f;
      ImGui.BeginChild("###CONFIGURATION_PROFILE_ZONE", new Vector2(-1, CONFIG_PROFILE_ZONE_HEIGHT), true);
      {
        // Init table
        ImGui.BeginTable("###CONFIGURATION_PROFILE_TABLE", 3);
        ImGui.TableSetupColumn("###CONFIGURATION_PROFILE_TABLE_COL1", ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("###CONFIGURATION_PROFILE_TABLE_COL2", ImGuiTableColumnFlags.WidthFixed, 350);
        ImGui.TableSetupColumn("###CONFIGURATION_PROFILE_TABLE_COL3", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        ImGui.Text("Current profile:");
        ImGui.TableNextColumn();
        string[] PROFILES = this.Configuration.Profiles.Select(profile => profile.Name).ToArray();
        int currentProfileIndex = this.Configuration.Profiles.FindIndex(profile => profile.Name == this.Configuration.CurrentProfileName);
        ImGui.SetNextItemWidth(350);
        if(ImGui.Combo("###CONFIGURATION_CURRENT_PROFILE", ref currentProfileIndex, PROFILES, PROFILES.Length)) {
          this.Configuration.CurrentProfileName = this.Configuration.Profiles[currentProfileIndex].Name;
          this.app.SetProfile(this.Configuration.CurrentProfileName);
          this.Logger.Debug($"New profile selected: {this.Configuration.CurrentProfileName}");
          this.Configuration.Save();
        }
        ImGui.TableNextColumn();
        if(ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Trash)) {
          if(this.Configuration.Profiles.Count <= 1) {
            string errorMsg = "You can't delete this profile. At least one profile should exists. Create another one before deleting.";
            this.Logger.Error(errorMsg);
            this._tmp_currentProfile_ErrorMsg = errorMsg;
          } else {
            this.Configuration.RemoveProfile(this.ConfigurationProfile.Name);
            ConfigurationProfile? newProfileToUse = this.Configuration.GetFirstProfile();
            if(newProfileToUse != null) {
              this.app.SetProfile(newProfileToUse.Name);
            }
            this.Configuration.Save();
          }
        }
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Add new profile: ");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(350);
        if(ImGui.InputText("###CONFIGURATION_NEW_PROFILE_NAME", ref _tmp_currentProfileNameToAdd, 150)) {
          this._tmp_currentProfile_ErrorMsg = "";
        }
        ImGui.TableNextColumn();
        if(this._tmp_currentProfileNameToAdd.Length > 0) {
          if(ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus)) {
            if(this._tmp_currentProfileNameToAdd.Trim() != "") {
              bool wasAdded = this.Configuration.AddProfile(this._tmp_currentProfileNameToAdd);
              if(!wasAdded) {
                string errorMsg = $"The current profile name '{this._tmp_currentProfileNameToAdd}' already exists!";
                this.Logger.Error(errorMsg);
                this._tmp_currentProfile_ErrorMsg = errorMsg;
              } else {
                this.app.SetProfile(this._tmp_currentProfileNameToAdd);
                this.Logger.Debug($"New profile added {_tmp_currentProfileNameToAdd}");
                this._tmp_currentProfileNameToAdd = "";
                this.Configuration.Save();
              }
            }
          }
        }
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Rename current profile");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(350);
        if(ImGui.InputText("###CONFIGURATION_CURRENT_PROFILE_RENAME", ref this.ConfigurationProfile.Name, 150)) {
          this.Configuration.CurrentProfileName = this.ConfigurationProfile.Name;
          this.Configuration.Save();
        }
        ImGui.EndTable();


        if(this._tmp_currentProfile_ErrorMsg != "") {
          ImGui.TextColored(ImGuiColors.DalamudRed, this._tmp_currentProfile_ErrorMsg);
        }
      };
      ImGui.EndChild();


      ImGui.TextColored(ImGuiColors.DalamudViolet, "General Settings");
      ImGui.BeginChild("###GENERAL_OPTIONS_ZONE", new Vector2(-1, 125f), true);
      {
        // Init table
        ImGui.BeginTable("###GENERAL_OPTIONS_TABLE", 2);
        ImGui.TableSetupColumn("###GENERAL_OPTIONS_TABLE_COL1", ImGuiTableColumnFlags.WidthFixed, 250);
        ImGui.TableSetupColumn("###GENERAL_OPTIONS_TABLE_COL2", ImGuiTableColumnFlags.WidthStretch);

        // Checkbox AUTO_OPEN
        ImGui.TableNextColumn();
        bool config_AUTO_OPEN = this.ConfigurationProfile.AUTO_OPEN;
        ImGui.Text("Automatically open configuration panel.");
        ImGui.TableNextColumn();
        if(ImGui.Checkbox("###GENERAL_OPTIONS_AUTO_OPEN", ref config_AUTO_OPEN)) {
          this.ConfigurationProfile.AUTO_OPEN = config_AUTO_OPEN;
          this.Configuration.Save();
        }
        ImGui.TableNextRow();


        // Checkbox MAX_VIBE_THRESHOLD
        ImGui.TableNextColumn();
        ImGui.Text("Global threshold: ");
        ImGui.TableNextColumn();
        int config_MAX_VIBE_THRESHOLD = this.ConfigurationProfile.MAX_VIBE_THRESHOLD;
        ImGui.SetNextItemWidth(201);
        if(ImGui.SliderInt("###OPTION_MaximumThreshold", ref config_MAX_VIBE_THRESHOLD, 2, 100)) {
          this.ConfigurationProfile.MAX_VIBE_THRESHOLD = config_MAX_VIBE_THRESHOLD;
          this.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Maximum threshold for vibes (will override every devices).");

        // Checkbox OPTION_VERBOSE_SPELL
        ImGui.TableNextColumn();
        ImGui.Text("Log casted spells:");
        ImGui.TableNextColumn();
        if(ImGui.Checkbox("###OPTION_VERBOSE_SPELL", ref this.ConfigurationProfile.VERBOSE_SPELL)) {
          this.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Use the /xllog to see all casted spells. Disable this to have better ingame performance.");
        ImGui.TableNextRow();

        // Checkbox OPTION_VERBOSE_CHAT
        ImGui.TableNextColumn();
        ImGui.Text("Log chat triggered:");
        ImGui.TableNextColumn();
        if(ImGui.Checkbox("###OPTION_VERBOSE_CHAT", ref this.ConfigurationProfile.VERBOSE_CHAT)) {
          this.Configuration.Save();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Use the /xllog to see all chat message. Disable this to have better ingame performance.");

        ImGui.EndTable();
      }
      ImGui.EndChild();

      if(this.ConfigurationProfile.VERBOSE_CHAT || this.ConfigurationProfile.VERBOSE_SPELL) {
        ImGui.TextColored(ImGuiColors.DalamudOrange, "Please, disabled chat and spell logs for better ingame performance.");
      }
    }

    public void DrawDevicesTab() {
      ImGui.Spacing();

      ImGui.TextColored(ImGuiColors.DalamudViolet, "Actions");
      ImGui.BeginChild("###DevicesTab_General", new Vector2(-1, 40f), true);
      {
        if(this.DevicesController.IsScanning()) {
          if(ImGui.Button("Stop scanning", new Vector2(100, 24))) {
            this.DevicesController.StopScanningDevice();
          }
        } else {
          if(ImGui.Button("Scan device", new Vector2(100, 24))) {
            this.DevicesController.ScanDevice();
          }
        }

        ImGui.SameLine();
        if(ImGui.Button("Update Battery", new Vector2(100, 24))) {
          this.DevicesController.UpdateAllBatteryLevel();
        }
        ImGui.SameLine();
        if(ImGui.Button("Stop All", new Vector2(100, 24))) {
          this.DevicesController.StopAll();
          this.simulator_currentAllIntensity = 0;
        }
      }
      ImGui.EndChild();

      if(ImGui.CollapsingHeader($"All devices")) {
        ImGui.Text("Send to all:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if(ImGui.SliderInt("###SendVibeAll_Intensity", ref this.simulator_currentAllIntensity, 0, 100)) {
          this.DevicesController.SendVibeToAll(this.simulator_currentAllIntensity);
        }
      }

      foreach(Device.Device device in this.DevicesController.GetDevices()) {
        if(ImGui.CollapsingHeader($"[{device.Id}] {device.Name} - Battery: {device.GetBatteryPercentage()}")) {
          ImGui.TextWrapped(device.ToString());
          if(device.CanVibrate) {
            ImGui.TextColored(ImGuiColors.DalamudViolet, "VIBRATE");
            ImGui.Indent(10);
            for(int i = 0; i < device.VibrateMotors; i++) {
              ImGui.Text($"Motor {i + 1}: ");
              ImGui.SameLine();
              ImGui.SetNextItemWidth(200);
              if(ImGui.SliderInt($"###{device.Id} Intensity Vibrate Motor {i}", ref device.CurrentVibrateIntensity[i], 0, 100)) {
                this.DevicesController.SendVibrate(device, device.CurrentVibrateIntensity[i], i);
              }
            }
            ImGui.Unindent(10);
          }

          if(device.CanRotate) {
            ImGui.TextColored(ImGuiColors.DalamudViolet, "ROTATE");
            ImGui.Indent(10);
            for(int i = 0; i < device.RotateMotors; i++) {
              ImGui.Text($"Motor {i + 1}: ");
              ImGui.SameLine();
              ImGui.SetNextItemWidth(200);
              if(ImGui.SliderInt($"###{device.Id} Intensity Rotate Motor {i}", ref device.CurrentRotateIntensity[i], 0, 100)) {
                this.DevicesController.SendRotate(device, device.CurrentRotateIntensity[i], i, true);
              }
            }
            ImGui.Unindent(10);
          }

          if(device.CanLinear) {
            ImGui.TextColored(ImGuiColors.DalamudViolet, "LINEAR VIBES");
            ImGui.Indent(10);
            for(int i = 0; i < device.LinearMotors; i++) {
              ImGui.Text($"Motor {i + 1}: ");
              ImGui.SameLine();
              ImGui.SetNextItemWidth(200);
              if(ImGui.SliderInt($"###{device.Id} Intensity Linear Motor {i}", ref device.CurrentLinearIntensity[i], 0, 100)) {
                this.DevicesController.SendLinear(device, device.CurrentLinearIntensity[i], 500, i);
              }
            }
            ImGui.Unindent(10);
          }
        }

      }
    }

    public unsafe void DrawTriggersTab() {
      List<Triggers.Trigger> triggers = this.TriggerController.GetTriggers();
      string selectedId = this.SelectedTrigger != null ? this.SelectedTrigger.Id : "";
      if(ImGui.BeginChild("###TriggersSelector", new Vector2(ImGui.GetWindowContentRegionMax().X/3, -ImGui.GetFrameHeightWithSpacing()), true)) {
        ImGui.SetNextItemWidth(185);
        ImGui.InputText("###TriggersSelector_SearchBar", ref this.CURRENT_TRIGGER_SELECTOR_SEARCHBAR, 200);
        ImGui.Spacing();
        
        for(int triggerIndex=0; triggerIndex<triggers.Count; triggerIndex++) {
          Triggers.Trigger trigger = triggers[triggerIndex];
          if(trigger != null) {
            string enabled = trigger.Enabled ? "" : "[disabled]";
            string kindStr = $"{Enum.GetName(typeof(Triggers.KIND), trigger.Kind)}";
            if(kindStr != null) {
              kindStr = kindStr.ToUpper();
            }
            string triggerName = $"{enabled}[{ kindStr}] {trigger.Name}";
            string triggerNameWithId = $"{triggerName}###{ trigger.Id}";
            if(!Helpers.RegExpMatch(this.Logger, triggerName, this.CURRENT_TRIGGER_SELECTOR_SEARCHBAR)) {
              continue;
            }
            
            if(ImGui.Selectable($"{triggerNameWithId}", selectedId == trigger.Id)) { // We don't want to show the ID
              this.SelectedTrigger = trigger;
              this.triggersViewMode = "edit";
            }
            if(ImGui.IsItemHovered()) {
              ImGui.SetTooltip($"{triggerName}");
            }
            if(ImGui.BeginDragDropSource()) {
              this._tmp_currentDraggingTriggerIndex = triggerIndex;
              ImGui.Text($"Dragging: {triggerName}");
              ImGui.SetDragDropPayload($"{triggerNameWithId}", (IntPtr)(&triggerIndex), sizeof(int));
              ImGui.EndDragDropSource();
            }
            if(ImGui.BeginDragDropTarget()) {
              if(this._tmp_currentDraggingTriggerIndex > -1 &&   ImGui.IsMouseReleased(ImGuiMouseButton.Left)) {
                int srcIndex = this._tmp_currentDraggingTriggerIndex;
                int targetIndex = triggerIndex;
                (triggers[srcIndex], triggers[targetIndex]) = (triggers[targetIndex], triggers[srcIndex]);
                this._tmp_currentDraggingTriggerIndex = -1;
                this.Configuration.Save();
              }
              ImGui.EndDragDropTarget();
            }

          }
        }
        ImGui.EndChild();
      }

      ImGui.SameLine();
      if(ImGui.BeginChild("###TriggerViewerPanel", new Vector2(0, -ImGui.GetFrameHeightWithSpacing()), true)) {
        if(this.triggersViewMode == "default") {
          ImGui.Text("Please select or add a trigger");
        } else if(this.triggersViewMode == "edit") {
          if(this.SelectedTrigger != null) {

            // Init table
            ImGui.BeginTable("###TRIGGER_FORM_TABLE_GENERAL", 2);
            ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_COL1", ImGuiTableColumnFlags.WidthFixed, COLUMN0_WIDTH);
            ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_COL2", ImGuiTableColumnFlags.WidthStretch);

            // Displaying the trigger ID
            ImGui.TableNextColumn();
            ImGui.Text($"TriggerID:");
            ImGui.TableNextColumn();
            ImGui.Text($"{this.SelectedTrigger.GetShortID()}");
            ImGui.TableNextRow();

            // TRIGGER ENABLED
            ImGui.TableNextColumn();
            ImGui.Text("Enabled:");
            ImGui.TableNextColumn();
            if(ImGui.Checkbox("###TRIGGER_ENABLED", ref this.SelectedTrigger.Enabled)) {
              this.Configuration.Save();
            };
            ImGui.TableNextRow();

            // TRIGGER NAME
            ImGui.TableNextColumn();
            ImGui.Text("Trigger Name:");
            ImGui.TableNextColumn();
            if(ImGui.InputText("###TRIGGER_NAME", ref this.SelectedTrigger.Name, 99)) {
              if(this.SelectedTrigger.Name == "") {
                this.SelectedTrigger.Name = "no_name";
              }
              this.Configuration.Save();
            };
            ImGui.TableNextRow();

            // TRIGGER NAME
            ImGui.TableNextColumn();
            ImGui.Text("Trigger Description:");
            ImGui.TableNextColumn();
            if (ImGui.InputTextMultiline("###TRIGGER_DESCRIPTION", ref this.SelectedTrigger.Description, 500, new Vector2(190, 50))) {
              if (this.SelectedTrigger.Description == "") {
                this.SelectedTrigger.Description = "no_description";
              }
              this.Configuration.Save();
            };
            ImGui.TableNextRow();


            // TRIGGER KIND
            ImGui.TableNextColumn();
            ImGui.Text("Kind:");
            ImGui.TableNextColumn();
            string[] TRIGGER_KIND = System.Enum.GetNames(typeof(Triggers.KIND));
            int currentKind = (int)this.SelectedTrigger.Kind;
            if(ImGui.Combo("###TRIGGER_FORM_KIND", ref currentKind, TRIGGER_KIND, TRIGGER_KIND.Length)) {
              this.SelectedTrigger.Kind = currentKind;
              if(currentKind == (int)Triggers.KIND.HPChange) {
                this.SelectedTrigger.StartAfter = 0;
                this.SelectedTrigger.StopAfter = 0;
              }
              this.Configuration.Save();
            }
            ImGui.TableNextRow();

            // TRIGGER FROM_PLAYER_NAME
            ImGui.TableNextColumn();
            ImGui.Text("Player name:");
            ImGui.TableNextColumn();
            if(ImGui.InputText("###TRIGGER_CHAT_FROM_PLAYER_NAME", ref this.SelectedTrigger.FromPlayerName, 100)) {
              this.SelectedTrigger.FromPlayerName = this.SelectedTrigger.FromPlayerName.Trim();
              this.Configuration.Save();
            };
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("You can use RegExp. Leave empty for any. Ignored if chat listening to 'Echo' and chat message we through it.");
            ImGui.TableNextRow();


            // TRIGGER START_AFTER
            ImGui.TableNextColumn();
            ImGui.Text("Start after");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(185);
            if(ImGui.SliderFloat("###TRIGGER_FORM_START_AFTER", ref this.SelectedTrigger.StartAfter, this.TRIGGER_MIN_AFTER, this.TRIGGER_MAX_AFTER)) {
              this.SelectedTrigger.StartAfter = Helpers.ClampFloat(this.SelectedTrigger.StartAfter, this.TRIGGER_MIN_AFTER, this.TRIGGER_MAX_AFTER);
              this.Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(45);

            if(ImGui.InputFloat("###TRIGGER_FORM_START_AFTER_INPUT", ref this.SelectedTrigger.StartAfter, this.TRIGGER_MIN_AFTER, this.TRIGGER_MAX_AFTER)) {
              this.SelectedTrigger.StartAfter = Helpers.ClampFloat(this.SelectedTrigger.StartAfter, this.TRIGGER_MIN_AFTER, this.TRIGGER_MAX_AFTER);
              this.Configuration.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("In seconds");
            ImGui.TableNextRow();

            // TRIGGER STOP_AFTER
            ImGui.TableNextColumn();
            ImGui.Text("Stop after");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(185);
            if(ImGui.SliderFloat("###TRIGGER_FORM_STOP_AFTER", ref this.SelectedTrigger.StopAfter, this.TRIGGER_MIN_AFTER, this.TRIGGER_MAX_AFTER)) {
              this.SelectedTrigger.StopAfter = Helpers.ClampFloat(this.SelectedTrigger.StopAfter, this.TRIGGER_MIN_AFTER, this.TRIGGER_MAX_AFTER);
              this.Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(45);
            if(ImGui.InputFloat("###TRIGGER_FORM_STOP_AFTER_INPUT", ref this.SelectedTrigger.StopAfter, this.TRIGGER_MIN_AFTER, this.TRIGGER_MAX_AFTER)) {
              this.SelectedTrigger.StopAfter = Helpers.ClampFloat(this.SelectedTrigger.StopAfter, this.TRIGGER_MIN_AFTER, this.TRIGGER_MAX_AFTER);
              this.Configuration.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("In seconds. Use zero to avoid stopping.");
            ImGui.TableNextRow();


            // TRIGGER PRIORITY
            ImGui.TableNextColumn();
            ImGui.Text("Priority");
            ImGui.TableNextColumn();
            if(ImGui.InputInt("###TRIGGER_FORM_PRIORITY", ref this.SelectedTrigger.Priority, 1)) {
              this.Configuration.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("If a trigger have a lower priority, it will be ignored.");
            ImGui.TableNextRow();

            ImGui.EndTable();

            ImGui.Separator();

            // TRIGGER KIND:CHAT OPTIONS
            if(this.SelectedTrigger.Kind == (int)Triggers.KIND.Chat) {

              // TRIGGER FORM_TABLE_KIND_CHAT
              ImGui.BeginTable("###TRIGGER_FORM_TABLE_KIND_CHAT", 2);
              ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_CHAT_COL1", ImGuiTableColumnFlags.WidthFixed, COLUMN0_WIDTH);
              ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_CHAT_COL2", ImGuiTableColumnFlags.WidthStretch);

              // TRIGGER CHAT_TEXT
              ImGui.TableNextColumn();
              ImGui.Text("Chat text:");
              ImGui.TableNextColumn();
              string currentChatText = this.SelectedTrigger.ChatText;
              if(ImGui.InputText("###TRIGGER_CHAT_TEXT", ref currentChatText, 250)) {
                this.SelectedTrigger.ChatText = currentChatText.ToLower(); // ChatMsg is always lower
                this.Configuration.Save();
              };
              ImGui.SameLine();
              ImGuiComponents.HelpMarker("You can use RegExp.");
              ImGui.TableNextRow();

              // TRIGGER CHAT_TEXT_TYPE_ALLOWED
              ImGui.TableNextColumn();
              ImGui.Text("Add chat type:");
              ImGui.TableNextColumn();
              int currentTypeAllowed = 0;
              string[] ChatTypesAllowedStrings = Enum.GetNames(typeof(XivChatType));
              if(ImGui.Combo("###TRIGGER_CHAT_TEXT_TYPE_ALLOWED", ref currentTypeAllowed, ChatTypesAllowedStrings, ChatTypesAllowedStrings.Length)) {
                if(!this.SelectedTrigger.AllowedChatTypes.Contains(currentTypeAllowed)) {
                  int XivChatTypeValue = (int)(XivChatType)Enum.Parse(typeof(XivChatType), ChatTypesAllowedStrings[currentTypeAllowed]);
                  this.SelectedTrigger.AllowedChatTypes.Add(XivChatTypeValue);
                }
                this.Configuration.Save();
              }
              ImGuiComponents.HelpMarker("Select some chats to observe or unselect all to watch every chats.");
              ImGui.TableNextRow();

              if(this.SelectedTrigger.AllowedChatTypes.Count > 0) {

                ImGui.TableNextColumn();
                ImGui.Text("Allowed Type:");
                ImGui.TableNextColumn();
                for(int indexAllowedChatType = 0; indexAllowedChatType < this.SelectedTrigger.AllowedChatTypes.Count; indexAllowedChatType++) {
                  int XivChatTypeValue = this.SelectedTrigger.AllowedChatTypes[indexAllowedChatType];
                  if(ImGuiComponents.IconButton(indexAllowedChatType, Dalamud.Interface.FontAwesomeIcon.Minus)) {
                    this.SelectedTrigger.AllowedChatTypes.RemoveAt(indexAllowedChatType);
                    this.Configuration.Save();
                  };
                  ImGui.SameLine();
                  string XivChatTypeName = ((XivChatType)XivChatTypeValue).ToString();
                  ImGui.Text($"{XivChatTypeName}");

                }
                ImGui.TableNextRow();
              }


              // END OF TABLE
              ImGui.EndTable();
            }

            // TRIGGER FORM_TABLE_KIND_CHAT
            ImGui.BeginTable("###TRIGGER_FORM_TABLE_KIND_SPELL", 2);
            ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_SPELL_COL1", ImGuiTableColumnFlags.WidthFixed, COLUMN0_WIDTH);
            ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_SPELL_COL2", ImGuiTableColumnFlags.WidthStretch);

            // TRIGGER KIND:SPELL OPTIONS
            if(this.SelectedTrigger.Kind == (int)Triggers.KIND.Spell){
              // TRIGGER TYPE
              ImGui.TableNextColumn();
              ImGui.Text("Type:");
              ImGui.TableNextColumn();
              string[] TRIGGER = System.Enum.GetNames(typeof(FFXIV_Vibe_Plugin.Commons.Structures.ActionEffectType));
              int currentEffectType = (int)this.SelectedTrigger.ActionEffectType;
              if(ImGui.Combo("###TRIGGER_FORM_EVENT", ref currentEffectType, TRIGGER, TRIGGER.Length)) {
                this.SelectedTrigger.ActionEffectType = currentEffectType;
                this.SelectedTrigger.Reset();
                this.Configuration.Save();
              }
              ImGui.TableNextRow();

              //TRIGGER SPELL TEXT
              ImGui.TableNextColumn();
              ImGui.Text("Spell Text:");
              ImGui.TableNextColumn();
              if(ImGui.InputText("###TRIGGER_FORM_SPELLNAME", ref this.SelectedTrigger.SpellText, 100)) {
                this.Configuration.Save();
              }
              ImGui.SameLine();
              ImGuiComponents.HelpMarker("You can use RegExp.");
              ImGui.TableNextRow();

              //TRIGGER DIRECTION
              ImGui.TableNextColumn();
              ImGui.Text("Direction:");
              ImGui.TableNextColumn();
              string[] DIRECTIONS = System.Enum.GetNames(typeof(Triggers.DIRECTION));
              int currentDirection = (int)this.SelectedTrigger.Direction;
              if(ImGui.Combo("###TRIGGER_FORM_DIRECTION", ref currentDirection, DIRECTIONS, DIRECTIONS.Length)) {
                this.SelectedTrigger.Direction = currentDirection;
                this.Configuration.Save();
              }
              ImGui.SameLine();
              ImGuiComponents.HelpMarker("Warning: Hitting no target will result to self as if you cast on yourself");
              ImGui.TableNextRow();
            }

            if(
                  this.SelectedTrigger.ActionEffectType == (int)Structures.ActionEffectType.Damage ||
                  this.SelectedTrigger.ActionEffectType == (int)Structures.ActionEffectType.Heal
                 || 
                this.SelectedTrigger.Kind == (int)Triggers.KIND.HPChange)
            {
              // Min/Max amount values
              string type = "";
              if(this.SelectedTrigger.ActionEffectType == (int)Structures.ActionEffectType.Damage) { type = "damage"; }
              if(this.SelectedTrigger.ActionEffectType == (int)Structures.ActionEffectType.Heal) { type = "heal"; }
              if(this.SelectedTrigger.Kind == (int)Triggers.KIND.HPChange) { type = "health"; }
              
              // TRIGGER AMOUNT IN PERCENTAGE
              ImGui.TableNextColumn();
              ImGui.Text("Amount in percentage?");
              ImGui.TableNextColumn();
              if(ImGui.Checkbox("###TRIGGER_AMOUNT_IN_PERCENTAGE", ref this.SelectedTrigger.AmountInPercentage)){
                this.SelectedTrigger.AmountMinValue = 0;
                this.SelectedTrigger.AmountMaxValue = 100;
                this.Configuration.Save();
              }

              

              // TRIGGER MIN_VALUE
              ImGui.TableNextColumn();
              ImGui.Text($"Min {type} value:");
              ImGui.TableNextColumn();
              if (this.SelectedTrigger.AmountInPercentage) {
                if(ImGui.SliderInt("###TRIGGER_FORM_MIN_AMOUNT", ref this.SelectedTrigger.AmountMinValue, 0, 100)) {
                  this.Configuration.Save();
                }
              } else {
                if (ImGui.InputInt("###TRIGGER_FORM_MIN_AMOUNT", ref this.SelectedTrigger.AmountMinValue, 100)) {
                  this.Configuration.Save();
                }
              }
              ImGui.TableNextRow();

              // TRIGGER MAX_VALUE
              ImGui.TableNextColumn();
              ImGui.Text($"Max {type} value:");
              ImGui.TableNextColumn();
              if (this.SelectedTrigger.AmountInPercentage) {
                if (ImGui.SliderInt("###TRIGGER_FORM_MAX_AMOUNT", ref this.SelectedTrigger.AmountMaxValue, 0, 100)) {
                  this.Configuration.Save();
                }
              }
              else {
                if (ImGui.InputInt("###TRIGGER_FORM_MAX_AMOUNT", ref this.SelectedTrigger.AmountMaxValue, 100)) {
                  this.Configuration.Save();
                }
              }
              ImGui.TableNextRow();
            }
            ImGui.EndTable();

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Actions & Devices");
            ImGui.Separator();

            // TRIGGER COMBO_DEVICES
            Dictionary<String, Device.Device> visitedDevice = DevicesController.GetVisitedDevices();
            if(visitedDevice.Count == 0) {
              ImGui.TextColored(ImGuiColors.DalamudRed, "Please connect yourself to intiface and add device(s)...");
            } else {
              string[] devicesStrings = visitedDevice.Keys.ToArray();
              ImGui.Combo("###TRIGGER_FORM_COMBO_DEVICES", ref this.TRIGGER_CURRENT_SELECTED_DEVICE, devicesStrings, devicesStrings.Length);
              ImGui.SameLine();
              List<Triggers.TriggerDevice> triggerDevices = this.SelectedTrigger.Devices;
              if(ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus)) {
                if(this.TRIGGER_CURRENT_SELECTED_DEVICE >= 0) {
                  Device.Device device = visitedDevice[devicesStrings[this.TRIGGER_CURRENT_SELECTED_DEVICE]];
                  Triggers.TriggerDevice newTriggerDevice = new(device);
                  triggerDevices.Add(newTriggerDevice);
                  this.Configuration.Save();
                }
              };

              string[] patternNames = this.Patterns.GetAllPatterns().Select(p => p.Name).ToArray();

              for(int indexDevice = 0; indexDevice < triggerDevices.Count; indexDevice++) {
                string prefixLabel = $"###TRIGGER_FORM_COMBO_DEVICE_${indexDevice}";
                Triggers.TriggerDevice triggerDevice = triggerDevices[indexDevice];
                string deviceName = triggerDevice.Device != null ? triggerDevice.Device.Name : "UnknownDevice";
                if(ImGui.CollapsingHeader($"{deviceName}")) {
                  ImGui.Indent(10);

                  if(triggerDevice != null && triggerDevice.Device != null) {
                    if(triggerDevice.Device.CanVibrate) {
                      if(ImGui.Checkbox($"{prefixLabel}_SHOULD_VIBRATE", ref triggerDevice.ShouldVibrate)) {
                        triggerDevice.ShouldStop = false;
                        this.Configuration.Save();
                      }
                      ImGui.SameLine();
                      ImGui.Text("Should Vibrate");
                      if(triggerDevice.ShouldVibrate) {
                        ImGui.Indent(20);
                        for(int motorId = 0; motorId < triggerDevice.Device.VibrateMotors; motorId++) {
                          ImGui.Text($"Motor {motorId + 1}");
                          ImGui.SameLine();
                          // Display Vibrate Motor checkbox
                          if(ImGui.Checkbox($"{prefixLabel}_SHOULD_VIBRATE_MOTOR_{motorId}", ref triggerDevice.VibrateSelectedMotors[motorId])) {
                            this.Configuration.Save();
                          }

                          if(triggerDevice.VibrateSelectedMotors[motorId]) {
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(90);
                            if(ImGui.Combo($"###{prefixLabel}_VIBRATE_PATTERNS_{motorId}", ref triggerDevice.VibrateMotorsPattern[motorId], patternNames, patternNames.Length)) {
                              this.Configuration.Save();
                            }

                            // Special intensity pattern asks for intensity param.
                            int currentPatternIndex = triggerDevice.VibrateMotorsPattern[motorId];
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(180);
                            if(ImGui.SliderInt($"{prefixLabel}_SHOULD_VIBRATE_MOTOR_{motorId}_THRESHOLD", ref triggerDevice.VibrateMotorsThreshold[motorId], 0, 100)) {
                              if(triggerDevice.VibrateMotorsThreshold[motorId] > 0) {
                                triggerDevice.VibrateSelectedMotors[motorId] = true;
                              }
                              this.Configuration.Save();
                            }
                          }
                        }
                        ImGui.Indent(-20);
                      }
                    }
                    if(triggerDevice.Device.CanRotate) {
                      if(ImGui.Checkbox($"{prefixLabel}_SHOULD_ROTATE", ref triggerDevice.ShouldRotate)) {
                        triggerDevice.ShouldStop = false;
                        this.Configuration.Save();
                      }
                      ImGui.SameLine();
                      ImGui.Text("Should Rotate");
                      if(triggerDevice.ShouldRotate) {
                        ImGui.Indent(20);
                        for(int motorId = 0; motorId < triggerDevice.Device.RotateMotors; motorId++) {
                          ImGui.Text($"Motor {motorId + 1}");
                          ImGui.SameLine();
                          if(ImGui.Checkbox($"{prefixLabel}_SHOULD_ROTATE_MOTOR_{motorId}", ref triggerDevice.RotateSelectedMotors[motorId])) {
                            this.Configuration.Save();
                          }
                          if(triggerDevice.RotateSelectedMotors[motorId]) {
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(90);
                            if(ImGui.Combo($"###{prefixLabel}_ROTATE_PATTERNS_{motorId}", ref triggerDevice.RotateMotorsPattern[motorId], patternNames, patternNames.Length)) {
                              this.Configuration.Save();
                            }
                            // Special intensity pattern asks for intensity param.
                            int currentPatternIndex = triggerDevice.RotateMotorsPattern[motorId];
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(180);
                            if(ImGui.SliderInt($"{prefixLabel}_SHOULD_ROTATE_MOTOR_{motorId}_THRESHOLD", ref triggerDevice.RotateMotorsThreshold[motorId], 0, 100)) {
                              if(triggerDevice.RotateMotorsThreshold[motorId] > 0) {
                                triggerDevice.RotateSelectedMotors[motorId] = true;
                              }
                              this.Configuration.Save();
                            }

                          }
                        }
                        ImGui.Indent(-20);
                      }
                    }
                    if(triggerDevice.Device.CanLinear) {
                      if(ImGui.Checkbox($"{prefixLabel}_SHOULD_LINEAR", ref triggerDevice.ShouldLinear)) {
                        triggerDevice.ShouldStop = false;
                        this.Configuration.Save();
                      }
                      ImGui.SameLine();
                      ImGui.Text("Should Linear");
                      if(triggerDevice.ShouldLinear) {
                        ImGui.Indent(20);
                        for(int motorId = 0; motorId < triggerDevice.Device.LinearMotors; motorId++) {
                          ImGui.Text($"Motor {motorId + 1}");
                          ImGui.SameLine();
                          if(ImGui.Checkbox($"{prefixLabel}_SHOULD_LINEAR_MOTOR_{motorId}", ref triggerDevice.LinearSelectedMotors[motorId])) {
                            this.Configuration.Save();
                          }
                          if(triggerDevice.LinearSelectedMotors[motorId]) {
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(90);
                            if(ImGui.Combo($"###{prefixLabel}_LINEAR_PATTERNS_{motorId}", ref triggerDevice.LinearMotorsPattern[motorId], patternNames, patternNames.Length)) {
                              this.Configuration.Save();
                            }
                            // Special intensity pattern asks for intensity param.
                            int currentPatternIndex = triggerDevice.LinearMotorsPattern[motorId];
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(180);
                            if(ImGui.SliderInt($"{prefixLabel}_SHOULD_LINEAR_MOTOR_{motorId}_THRESHOLD", ref triggerDevice.LinearMotorsThreshold[motorId], 0, 100)) {
                              if(triggerDevice.LinearMotorsThreshold[motorId] > 0) {
                                triggerDevice.LinearSelectedMotors[motorId] = true;
                              }
                              this.Configuration.Save();
                            }
                          }
                        }
                        ImGui.Indent(-20);
                      }
                    }
                    if(triggerDevice.Device.CanStop) {
                      if(ImGui.Checkbox($"{prefixLabel}_SHOULD_STOP", ref triggerDevice.ShouldStop)) {
                        triggerDevice.ShouldVibrate = false;
                        triggerDevice.ShouldRotate = false;
                        triggerDevice.ShouldLinear = false;
                        this.Configuration.Save();
                      }
                      ImGui.SameLine();
                      ImGui.Text("Should stop all motors");
                      ImGui.SameLine();
                      ImGuiComponents.HelpMarker("Instantly stop all motors for this device.");
                    }
                    if(ImGui.Button($"Remove###{prefixLabel}_REMOVE")) {
                      triggerDevices.RemoveAt(indexDevice);
                      this.Logger.Log($"DEBUG: removing {indexDevice}");
                      this.Configuration.Save();
                    }
                  }
                  ImGui.Indent(-10);
                }
              }
            }


          } else {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Current selected trigger is null");
          }
        } else if(this.triggersViewMode == "delete") {
          if(this.SelectedTrigger != null) {
            ImGui.TextColored(ImGuiColors.DalamudRed, $"Are you sure you want to delete trigger ID: {this.SelectedTrigger.Id}");
            if(ImGui.Button("Yes")) {
              if(this.SelectedTrigger != null) {
                this.TriggerController.RemoveTrigger(this.SelectedTrigger);
                this.SelectedTrigger = null;
                this.Configuration.Save();
              }
              this.triggersViewMode = "default";
            };
            ImGui.SameLine();
            if(ImGui.Button("No")) {
              this.SelectedTrigger = null;
              this.triggersViewMode = "default";
            };
          }
        }
        ImGui.EndChild();
      }

      if(ImGui.Button("Add")) {
        Triggers.Trigger trigger = new("New Trigger");
        this.TriggerController.AddTrigger(trigger);
        this.SelectedTrigger = trigger;
        this.triggersViewMode = "edit";
        this.Configuration.Save();
      };
      ImGui.SameLine();
      if(ImGui.Button("Delete")) {
        this.triggersViewMode = "delete";
      }

    }

    public void DrawPatternsTab() {
      ImGui.TextColored(ImGuiColors.DalamudViolet, "Add or edit a new pattern:");
      ImGui.Indent(20);
      List<Pattern> customPatterns = this.Patterns.GetCustomPatterns();
      ImGui.BeginTable("###PATTERN_ADD_FORM", 3);
      ImGui.TableSetupColumn("###PATTERN_ADD_FORM_COL1", ImGuiTableColumnFlags.WidthFixed, 100);
      ImGui.TableSetupColumn("###PATTERN_ADD_FORM_COL2", ImGuiTableColumnFlags.WidthFixed, 300);
      ImGui.TableSetupColumn("###PATTERN_ADD_FORM_COL3", ImGuiTableColumnFlags.WidthStretch);
      ImGui.TableNextColumn();
      ImGui.Text("Pattern Name:");
      ImGui.TableNextColumn();
      ImGui.SetNextItemWidth(300);
      if(ImGui.InputText("###PATTERNS_CURRENT_PATTERN_NAME_TO_ADD", ref this._tmp_currentPatternNameToAdd, 150)) {
        this._tmp_currentPatternNameToAdd = this._tmp_currentPatternNameToAdd.Trim();
      }
      ImGui.TableNextRow();
      ImGui.TableNextColumn();
      ImGui.Text("Pattern Value:");
      ImGui.TableNextColumn();
      ImGui.SetNextItemWidth(300);
      if(ImGui.InputText("###PATTERNS_CURRENT_PATTERN_VALUE_TO_ADD", ref this._tmp_currentPatternValueToAdd, 500)) {
        this._tmp_currentPatternValueToAdd = this._tmp_currentPatternValueToAdd.Trim();
        string value = this._tmp_currentPatternValueToAdd.Trim();
        if(value == "") {
          this._tmp_currentPatternValueState = "unset";
        } else {
          this._tmp_currentPatternValueState = Helpers.RegExpMatch(this.Logger, this._tmp_currentPatternValueToAdd, this.VALID_REGEXP_PATTERN) ? "valid" : "unvalid";
        }
      }




      if(this._tmp_currentPatternNameToAdd.Trim() != "" && this._tmp_currentPatternValueState == "valid") {
        ImGui.TableNextColumn();
        if(ImGui.Button("Save")) {
          Pattern newPattern = new(this._tmp_currentPatternNameToAdd, this._tmp_currentPatternValueToAdd);
          this.Patterns.AddCustomPattern(newPattern);
          this.ConfigurationProfile.PatternList = this.Patterns.GetCustomPatterns();
          this.Configuration.Save();
          this._tmp_currentPatternNameToAdd = "";
          this._tmp_currentPatternValueToAdd = "";
          this._tmp_currentPatternValueState = "unset";
        }
      }
      ImGui.TableNextRow();

      if(this._tmp_currentPatternValueState == "unvalid") {
        ImGui.TableNextColumn();
        ImGui.TextColored(ImGuiColors.DalamudRed, "WRONG FORMAT!");
        ImGui.TableNextColumn();
        ImGui.TextColored(ImGuiColors.DalamudRed, "Format: <int>:<ms>|<int>:<ms>...");
        ImGui.TableNextColumn();
        ImGui.TextColored(ImGuiColors.DalamudRed, "Eg: 10:500|100:1000|20:500|0:0");
      }

      ImGui.EndTable();
      ImGui.Indent(-20);


      ImGui.Separator();


      if(customPatterns.Count == 0) {
        ImGui.Text("No custom patterns, please add some");
      } else {
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Custom Patterns:");
        ImGui.Indent(20);

        ImGui.BeginTable("###PATTERN_CUSTOM_LIST", 3);
        ImGui.TableSetupColumn("###PATTERN_CUSTOM_LIST_COL1", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("###PATTERN_CUSTOM_LIST_COL2", ImGuiTableColumnFlags.WidthFixed, 430);
        ImGui.TableSetupColumn("###PATTERN_CUSTOM_LIST_COL3", ImGuiTableColumnFlags.WidthStretch);

        // Searchbar
        ImGui.TableNextColumn();
        ImGui.TextColored(ImGuiColors.DalamudGrey2, "Search name:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        ImGui.InputText("###PATTERN_SEARCH_BAR", ref CURRENT_PATTERN_SEARCHBAR, 200);
        ImGui.TableNextRow();


        for(int patternIndex = 0; patternIndex < customPatterns.Count; patternIndex++) {
          Pattern pattern = customPatterns[patternIndex];
          if(!Helpers.RegExpMatch(this.Logger, pattern.Name, this.CURRENT_PATTERN_SEARCHBAR)) {
            continue;
          }
          ImGui.TableNextColumn();
          ImGui.Text($"{pattern.Name}");
          if(ImGui.IsItemHovered()) {
            ImGui.SetTooltip($"{pattern.Name}");
          }
          ImGui.TableNextColumn();
          string valueShort = pattern.Value;
          if(valueShort.Length > 70) {
            valueShort = $"{valueShort[..70]}...";
          }
          ImGui.Text(valueShort);
          if(ImGui.IsItemHovered()) {
            ImGui.SetTooltip($"{pattern.Value}");
          }

          ImGui.TableNextColumn();

          if(ImGuiComponents.IconButton(patternIndex, Dalamud.Interface.FontAwesomeIcon.Trash)) {
            bool ok = this.Patterns.RemoveCustomPattern(pattern);
            if(!ok) {
              this.Logger.Error($"Could not remove pattern {pattern.Name}");
            } else {
              List<Pattern> newPatternList = this.Patterns.GetCustomPatterns();
              this.ConfigurationProfile.PatternList = newPatternList;
              this.Configuration.Save();
            }
          }
          ImGui.SameLine();
          if(ImGuiComponents.IconButton(patternIndex, Dalamud.Interface.FontAwesomeIcon.Pen)) {
            this._tmp_currentPatternNameToAdd = pattern.Name;
            this._tmp_currentPatternValueToAdd = pattern.Value;
            this._tmp_currentPatternValueState = "valid";
          }
          ImGui.TableNextRow();
        }
        ImGui.EndTable();
        ImGui.Indent(-20);
      }

    }

    public void DrawHelpTab() {
      string help = App.GetHelp(this.app.CommandName);
      ImGui.TextWrapped(help);
      ImGui.TextColored(ImGuiColors.DalamudViolet, "Plugin information");
      ImGui.Text($"App version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
      ImGui.Text($"Config version: {this.Configuration.Version}");
      ImGui.TextColored(ImGuiColors.DalamudViolet, "Pattern information");
      ImGui.TextWrapped("You should use a string separated by the | (pipe) symbol with a pair of <Intensity> and <Duration in milliseconds>.");
      ImGui.TextWrapped("Below is an example of a pattern that would vibe 1sec at 50pct intensity and 2sec at 100pct:");
      ImGui.TextWrapped("Pattern example:");
      this._tmp_void = "50:1000|100:2000";
      ImGui.InputText("###HELP_PATTERN_EXAMPLE", ref this._tmp_void, 50);
    }

  }
}

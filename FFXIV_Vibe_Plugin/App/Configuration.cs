using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

using FFXIV_Vibe_Plugin.Triggers;

namespace FFXIV_Vibe_Plugin {
  [Serializable]
  public class Configuration : IPluginConfiguration {

    public int Version { get; set; } = 0;
    public string CurrentProfileName = "Default";
    public List<ConfigurationProfile> Profiles = new();


    /** 
     * TODO: 2022.01.12 
     * LEGACY from version 2.0.0. Changed to presets in 2.1.0.
     * This was moved to presets. It should be remove one day */
    public bool VERBOSE_SPELL = false; 
    public bool VERBOSE_CHAT = false;
    public bool VIBE_HP_TOGGLE { get; set; } = false;
    public int VIBE_HP_MODE { get; set; } = 0;
    public int MAX_VIBE_THRESHOLD { get; set; } = 100;
    public bool AUTO_CONNECT { get; set; } = true;
    public bool AUTO_OPEN { get; set; } = false;
    public List<Pattern> PatternList = new();
    public string BUTTPLUG_SERVER_HOST { get; set; } = "127.0.0.1";
    public int BUTTPLUG_SERVER_PORT { get; set; } = 12345;
    public List<Triggers.Trigger> TRIGGERS { get; set; } = new();
    public Dictionary<string, FFXIV_Vibe_Plugin.Device.Device> VISITED_DEVICES = new();


    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private DalamudPluginInterface? pluginInterface;
    public void Initialize(DalamudPluginInterface pluginInterface) {
      this.pluginInterface = pluginInterface;
    }
    public void Save() {
      this.pluginInterface!.SavePluginConfig(this);
    }

    /** 
     * Get the profile specified by name.
     */
    public ConfigurationProfile? GetProfile(String name="") {
      if(name == "") {
        name = this.CurrentProfileName;
      }
      ConfigurationProfile? profile = this.Profiles.Find(i => i.Name == name);

      return profile;
    }

    public ConfigurationProfile GetDefaultProfile() {
      String defaultProfileName = "Default profile";
      ConfigurationProfile? profileToCheck = this.GetProfile(this.CurrentProfileName);
      if(profileToCheck == null) {
        profileToCheck = this.GetProfile(defaultProfileName);
      }
      ConfigurationProfile profileToReturn = profileToCheck ?? (new());
      if(profileToCheck == null) {
        profileToReturn.Name = defaultProfileName;
        this.Profiles.Add(profileToReturn);
        this.CurrentProfileName = defaultProfileName;
        this.Save();
      }
      return profileToReturn;
    }

    public ConfigurationProfile? GetFirstProfile() {
      ConfigurationProfile? profile = null;
      if(profile == null && this.Profiles.Count > 0) {
        profile = this.Profiles[0];
      }
      return profile;
    }

    public void RemoveProfile(String name) {
      ConfigurationProfile? profile = this.GetProfile(name);
      if(profile != null) {
        this.Profiles.Remove(profile);
      }
    }

    public bool AddProfile(String name) {
      ConfigurationProfile? profile = GetProfile(name);
      if(profile == null) {
        profile = new();
        profile.Name = name;
        this.Profiles.Add(profile);
        return true;
      }
      return false;
    }

    public bool SetCurrentProfile(String name) {
      ConfigurationProfile? profile = this.GetProfile(name);
      if(profile != null) {
        this.CurrentProfileName = profile.Name;
        return true;
      }
      return false;
    }
  }

  public class ConfigurationProfile{
    public string Name = "Default";
    public bool VERBOSE_SPELL = false;
    public bool VERBOSE_CHAT = false;
    public bool VIBE_HP_TOGGLE { get; set; } = false;

    public int VIBE_HP_MODE { get; set; } = 0;
    public int MAX_VIBE_THRESHOLD { get; set; } = 100;
    public bool AUTO_CONNECT { get; set; } = true;
    public bool AUTO_OPEN { get; set; } = false;
    public List<Pattern> PatternList = new();

    public string BUTTPLUG_SERVER_HOST { get; set; } = "127.0.0.1";
    public int BUTTPLUG_SERVER_PORT { get; set; } = 12345;

    public List<Triggers.Trigger> TRIGGERS { get; set; } = new();

    public Dictionary<string, FFXIV_Vibe_Plugin.Device.Device> VISITED_DEVICES = new();

  }


}

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Network;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIV_Vibe_Plugin.Windows;
using System.IO;

namespace FFXIV_Vibe_Plugin {
  public sealed class Plugin : IDalamudPlugin {
    // Dalamud plugin definition
    public string Name => "FFXIV Vibe Plugin";
    public static readonly string ShortName = "FVP";
    public readonly string CommandName = "/fvp";

    // Dalamud plugins
    private Dalamud.Game.Gui.ChatGui? DalamudChat { get; init; }
    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }

    public WindowSystem WindowSystem = new("FFXIV_Vibe_Plugin");

    // FFXIV_Vibe_Plugin definition
    // TODO: private PluginUI PluginUi { get; init; }
    private FFXIV_Vibe_Plugin.App app;

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] ClientState clientState,
        [RequiredVersion("1.0")] GameNetwork gameNetwork,
        [RequiredVersion("1.0")] SigScanner scanner,
        [RequiredVersion("1.0")] ObjectTable gameObjects,
        [RequiredVersion("1.0")] DataManager dataManager
        ) {
      this.PluginInterface = pluginInterface;
      this.CommandManager = commandManager;

      this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      this.Configuration.Initialize(this.PluginInterface);

      // you might normally want to embed resources and load them from the manifest stream
      var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "logo.png");
      var logoImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

      WindowSystem.AddWindow(new ConfigWindow(this));
      WindowSystem.AddWindow(new MainWindow(this, logoImage));

      this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
        HelpMessage = "A vibe plugin for fun..."
      });

      this.PluginInterface.UiBuilder.Draw += DrawUI;
      this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

      // Init our own app
      this.app = new FFXIV_Vibe_Plugin.App(CommandName, ShortName, gameNetwork, clientState, dataManager, DalamudChat, Configuration, scanner, gameObjects, pluginInterface);
    }

    public void Dispose() {
      this.WindowSystem.RemoveAllWindows();
      this.CommandManager.RemoveHandler(CommandName);
      this.app.Dispose();
    }


    private void OnCommand(string command, string args) {
      // in response to the slash command, just display our main ui
      WindowSystem.GetWindow("My Amazing Window").IsOpen = true;
      this.app.OnCommand(command, args);
    }

    private void DrawUI() {
      this.WindowSystem.Draw();
      this.app.DrawUI();
    }

    public void DrawConfigUI() {
      WindowSystem.GetWindow("A Wonderful Configuration Window").IsOpen = true;
    }
  }
}

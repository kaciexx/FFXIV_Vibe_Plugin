using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Plugin;
using Dalamud.IoC;

namespace FFXIV_Vibe_Plugin.Commons {

  internal class Logger {

    // Initialize the Dalamud.Gui system.
    private readonly Dalamud.Game.Gui.ChatGui? DalamudChatGui;

    // Logger name.
    private readonly string name = "";

    // Current log level.
    private readonly LogLevel log_level = LogLevel.DEBUG;

    // The prefix symbol of the log message.
    private readonly string prefix = ">";

    // Available log levels.
    public enum LogLevel {
      VERBOSE, DEBUG, LOG, INFO, WARN, ERROR, FATAL,
    }

    /** Constructor */
    public Logger(Dalamud.Game.Gui.ChatGui? DalamudChatGui, string name, LogLevel log_level) {
      this.DalamudChatGui = DalamudChatGui;
      this.name = name;
      this.log_level = log_level;
    }

    /** Printing in the chat gui a message. */
    public void Chat(string msg) {
      if(DalamudChatGui != null) {
        string m = this.FormatMessage(LogLevel.LOG, msg);
        DalamudChatGui.Print(m);
      } else {
        Dalamud.Logging.PluginLog.LogError("No gui chat");
      }
    }

    /** Printing in the chat gui an error message. */
    public void ChatError(string msg) {
      string m = this.FormatMessage(LogLevel.ERROR, msg);
      DalamudChatGui?.PrintError(m);
      this.Error(msg);
    }

    /** Printing in the chat gui an error message with an exception. */
    public void ChatError(string msg, Exception e) {
      string m = this.FormatMessage(LogLevel.ERROR, msg, e);
      DalamudChatGui?.PrintError(m);
      this.Error(m);
    }

    /** Log message as 'debug' to logs. */
    public void Verbose(string msg) {
      if(this.log_level > LogLevel.VERBOSE) { return; }
      string m = this.FormatMessage(LogLevel.VERBOSE, msg);
      Dalamud.Logging.PluginLog.LogVerbose(m);
    }

    /** Log message as 'debug' to logs. */
    public void Debug(string msg) {
      if(this.log_level > LogLevel.DEBUG) { return; }
      string m = this.FormatMessage(LogLevel.DEBUG, msg);
      Dalamud.Logging.PluginLog.LogDebug(m);
    }

    /** Log message as 'log' to logs. */
    public void Log(string msg) {
      if(this.log_level > LogLevel.LOG) { return; }
      string m = this.FormatMessage(LogLevel.LOG, msg);
      Dalamud.Logging.PluginLog.Log(m);
    }

    /** Log message as 'info' to logs. */
    public void Info(string msg) {
      if(this.log_level > LogLevel.INFO) { return; }
      string m = this.FormatMessage(LogLevel.INFO, msg);
      Dalamud.Logging.PluginLog.Information(m);
    }

    /** Log message as 'warning' to logs. */
    public void Warn(string msg) {
      if(this.log_level > LogLevel.WARN) { return; }
      string m = this.FormatMessage(LogLevel.WARN, msg);
      Dalamud.Logging.PluginLog.Warning(m);
    }

    /** Log message as 'error' to logs. */
    public void Error(string msg) {
      if(this.log_level > LogLevel.ERROR) { return; }
      string m = this.FormatMessage(LogLevel.ERROR, msg);
      Dalamud.Logging.PluginLog.Error(m);
    }

    /** Log message as 'error' to logs with an exception. */
    public void Error(string msg, Exception e) {
      if(this.log_level > LogLevel.ERROR) { return; }
      string m = this.FormatMessage(LogLevel.ERROR, msg, e);
      Dalamud.Logging.PluginLog.Error(m);
    }

    /** Log message as 'fatal' to logs. */
    public void Fatal(string msg) {
      if(this.log_level > LogLevel.FATAL) { return; }
      string m = this.FormatMessage(LogLevel.FATAL, msg);
      Dalamud.Logging.PluginLog.Fatal(m);
    }

    /** Log message as 'fatal' to logs with an exception. */
    public void Fatal(string msg, Exception e) {
      if(this.log_level > LogLevel.FATAL) { return; }
      string m = this.FormatMessage(LogLevel.FATAL, msg, e);
      Dalamud.Logging.PluginLog.Fatal(m);
    }

    private string FormatMessage(LogLevel type, string msg) {
      return $"{(name != "" ? name + " " : "")}{type} {this.prefix} {msg}";
    }
    private string FormatMessage(LogLevel type, string msg, Exception e) {
      return $"{(name != "" ? name+" " : "")}{type} {this.prefix} {e.Message}\\n{msg}";
    }
  }
}

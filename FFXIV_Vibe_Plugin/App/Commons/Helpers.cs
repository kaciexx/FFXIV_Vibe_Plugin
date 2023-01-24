using System;
using System.Text.RegularExpressions;


namespace FFXIV_Vibe_Plugin.Commons {
  internal class Helpers {

    /** Get number of milliseconds (unix timestamp) */
    public static int GetUnix() {
      return (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    public static int ClampInt(int value, int min, int max) {
      if(value < min) { return min; } else if( value > max) { return max; }
      return value;
    }

    public static float ClampFloat(float value, float min, float max) {
      if(value < min) { return min; } else if(value > max) { return max; }
      return value;
    }


    public static int ClampIntensity(int intensity, int threshold) {
      intensity = ClampInt(intensity, 0, 100);
      return (int)(intensity / (100.0f / threshold));
    }

    /** Check if a regexp matches the given text */
    public static bool RegExpMatch(Logger Logger, string text, string regexp) {
      bool found = false;

      if(regexp.Trim() == "") {
        found = true;
      } else {
        string patternCheck = String.Concat(@"", regexp);
        try {
          System.Text.RegularExpressions.Match m = Regex.Match(text, patternCheck, RegexOptions.IgnoreCase);
          if(m.Success) {
            found = true;
          }
        } catch(Exception) {
          Logger.Error($"Probably a wrong REGEXP for {regexp}");
        }
      }

      return found;
    }
  }
}

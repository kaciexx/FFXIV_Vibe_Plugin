using System;

namespace FFXIV_Vibe_Plugin.Triggers {
  [Serializable]
  public class ChatTrigger : IComparable {
    
    public ChatTrigger(int intensity, string text) {
        Intensity = intensity;
        Text = text;
      }

      public int Intensity { get; }
      public string Text { get; }

      public override string ToString() {
        return $"Trigger(intensity: {Intensity}, text: '{Text}')";
      }
      public string ToConfigString() {
        return $"{Intensity} {Text}";
      }
      public int CompareTo(object? obj) {
      int thatintensity = obj is ChatTrigger that ? that.Intensity : 0;
      return this.Intensity.CompareTo(thatintensity);
      }
    }
  
}

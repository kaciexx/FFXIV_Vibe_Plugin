using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_Vibe_Plugin {

  public class Patterns {
    private readonly List<Pattern> BuiltinPatterns = new();
    private List<Pattern> CustomPatterns = new();

    /**
     * Pattern is: [intensity]:[duration in ms]
     */
    public Patterns() {
      this.AddBuiltinPattern(new Pattern("intensity", "100:0")); // Don't change this one. 
      this.AddBuiltinPattern(new Pattern("ramp", "10:150|20:150|30:150|40:150|50:150|60:150|70:150|80:150|90:150|100:250|0:0"));
      this.AddBuiltinPattern(new Pattern("bump", "10:150|20:150|30:150|40:150|50:150|60:150|70:150|80:150|90:150|100:250|50:250|100:500|0:0"));
      this.AddBuiltinPattern(new Pattern("square", "100:800|50:800|0:200|100:1000|0:0"));
      this.AddBuiltinPattern(new Pattern("shake", "100:500|20:200|100:500|80:500|100:200|90:100|100:200|90:200|100:800|0:0"));
      this.AddBuiltinPattern(new Pattern("sos", "100:500|50:200|100:500|50:200|100:500|50:200|100:1000|30:200|100:1000|30:200|100:1000|30:200|100:500|50:200|100:500|50:200|100:500|0:0"));
      this.AddBuiltinPattern(new Pattern("xenoWave", "10:650|15:500|20:400|30:400|45:350|60:300|75:300|95:250|100:200|90:250|75:300|60:300|45:350|30:400|20:400|15:500|10:650|5:750|0:0"));
      this.AddBuiltinPattern(new Pattern("slowVibe", "10:1000|20:1000|10:1000|50:1000|0:0"));
      this.AddBuiltinPattern(new Pattern("Vel: Poke Release 25s", "100:3000|50:500|60:500|70:500|80:500|90:500|100:3000|50:500|60:500|70:500|80:500|90:500|100:3000|50:500|60:500|70:500|80:500|90:500|100:3000|50:500|60:500|70:500|80:500|90:500|100:3000|0:0"));
      this.AddBuiltinPattern(new Pattern("Vel: Stop to gentle 14.6s", "40:200|50:200|60:200|70:200|80:200|90:200|100:3000|90:600|80:800|70:1000|60:1200|50:1400|40:1600|30:1800|25:2000|0:0"));
      this.AddBuiltinPattern(new Pattern("Vel: Teleport 7s", "20:500|30:500|40:500|50:500|60:500|70:500|80:500|90:500|100:1000|90:200|80:200|70:200|60:200|50:200|40:200|30:200|20:200|10:200|5:200|0:0"));
      this.AddBuiltinPattern(new Pattern("Vel: Paralysis 7s", "100:200|0:200|100:200|0:200|100:200|0:500|100:200|0:200|100:200|0:200|100:200|0:500|100:200|0:200|100:200|0:200|100:200|0:500|200:100|200:0|200:100|200:0|200:100|500:0|200:100|200:0|200:100|200:0|200:100|0:0"));
      this.AddBuiltinPattern(new Pattern("Vel: Paralysis (longer)	11.3s", "50:200|0:200|100:500|0:200|40:200|0:700|20:200|0:500|100:200|0:200|60:200|0:400|80:200|0:300|90:200|0:200|35:200|0:500|55:200|0:200|40:200|0:700|20:200|0:200|100:900|0:200|60:200|0:200|30:200|0:300|70:200|0:200|100:500|0:200|50:200|0:200|100:400|0:200|100:200|0:0"));
      this.AddBuiltinPattern(new Pattern("Vel: Simple vibe 12s", "50:2000|100:2000|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:200|100:200|0:0"));
      this.AddBuiltinPattern(new Pattern("Vel: Impact 1.25s", "100:500|90:100|80:100|70:100|60:100|50:100|40:100|30:100|20:100|10:100|0:0"));
      this.AddBuiltinPattern(new Pattern("Vel: Skyshard 5.65s", "100:100|20:100|40:100|60:100|20:2500|40:150|20:150|80:150|100:2000|80:150|40:150|0:0"));
      this.AddBuiltinPattern(new Pattern("Vel: Sprint 20s", "100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|100:400|50:400|400:100|400:50|400:100|400:50|400:100|400:50|400:100|400:50|400:100|400:50|400:100|400:50"));
      this.AddBuiltinPattern(new Pattern("Vel: Heartbeat (fast) 8.3s", "50:200|0:200|70:200|0:1000|50:200|0:200|70:200|0:1000|50:200|0:200|70:200|0:1000|50:200|0:200|70:200|0:1000|0:0"));
      this.AddBuiltinPattern(new Pattern("FF Victory Jingle 4.95sec", "20:150|0:150|70:150|0:150|0:500|60:150|0:150|40:150|0:150|0:500|30:150|0:150|80:150|0:150|0:500|100:150|0:150|20:150|0:150|0:500|30:150|0:150|40:150|0:150|0:500|90:150|0:150|80:150|0:150|0:500|20:150|0:150|10:150|0:150|0:500|90:150|0:150|80:150|0:150|0:0"));

      


    }

    public List<Pattern> GetAllPatterns() {
      return this.BuiltinPatterns.Concat(this.CustomPatterns).ToList();
    }

    public List<Pattern> GetBuiltinPatterns() {
      return this.BuiltinPatterns;
    }

    /** Returns a copy of the list to avoid error if any modification happens */
    public List<Pattern> GetCustomPatterns() {
      List<Pattern> newList = new();
      foreach(Pattern pattern in this.CustomPatterns) {
        newList.Add(pattern);
      }
      return newList;
    }

    public void SetCustomPatterns(List<Pattern> customPatterns) {
      this.CustomPatterns = customPatterns;
    }

    public Pattern GetPatternById(int index) {
      return this.GetAllPatterns()[index];
    }

    public void AddBuiltinPattern(Pattern pattern) {
      BuiltinPatterns.Add(pattern);
    }

    public void AddCustomPattern(Pattern pattern) {
      Pattern? foundPattern = CustomPatterns.FirstOrDefault<Pattern>(p => p.Name == pattern.Name );
      if(foundPattern != null) {
        foundPattern.Name = pattern.Name;
        foundPattern.Value = pattern.Value;
      } else {
        CustomPatterns.Add(pattern);
      }
    }

    public bool RemoveCustomPattern(Pattern pattern) {
      int index = CustomPatterns.IndexOf(pattern);
      if(index > -1) {
        this.CustomPatterns.RemoveAt(index);
        return true;
      }
      return false;
    }
  }

  public class Pattern {
    public int Index = -1;
    public string Name = "pattern";
    public string Value = "10:1000";
    public Pattern(string name="pattern", string value="10:1000") {
      this.Name = name;
      this.Value = value;
    }
  }
}

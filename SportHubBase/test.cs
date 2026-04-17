using System;
using System.Collections.Generic;
using System.Linq;

class Program {
    class Match { public int? MatchNumber { get; set; } }
    static void Main() {
        var list = new List<Match> { new Match(), new Match() };
        int max = list.Max(m => m.MatchNumber) ?? 0;
        Console.WriteLine(max);
    }
}

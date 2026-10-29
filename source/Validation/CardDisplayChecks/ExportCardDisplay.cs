using System; using System.Linq; using System.Globalization; using System.Text; using EasternDynasty;
static class ExportCardDisplay {
 static string N(double n) { return n.ToString("R",CultureInfo.InvariantCulture); }
 static string S(string s) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(s??"")); }
 static void Main() { foreach(var g in Catalog.Generals.Where(g=>g.s1))
  Console.WriteLine(string.Join("\t",new[]{g.id,N(g.defenseGrowth),N(g.strategyGrowth),N(g.speedGrowth),N(g.siege),N(g.siegeGrowth),N(g.attackDistance),S(g.biography),S(g.advancedTroops),S(g.combination),S(g.policyName),S(g.policyText)}));
 }
}
namespace Backtest;
public static class Tests {
 private sealed class AlwaysLong:IStrategy { public bool Long(IReadOnlyList<Bar> b,int i)=>true; }
 private static void Assert(bool test,string label){if(!test)throw new Exception("FAILED: "+label);Console.WriteLine("PASS: "+label);}
 public static void Run(){
  var bars=Enumerable.Range(0,20).Select(i=>new Bar(new DateTime(2025,1,1).AddDays(i).ToString("yyyy-MM-dd"),100m,100m)).ToList();
  var result=Engine.Run(bars,new Settings(FeeBps:10,Exposure:1),new AlwaysLong());Assert(result.FinalEquity<10000&&result.Fees>0,"fees reduce flat-market equity");Assert(result.Trades.Count==1&&result.Trades[0].Quantity*100+result.Trades[0].Fee<=10000,"cash budget includes fees");
  var original=Engine.Sample();var altered=original.Select(b=>b).ToList();for(int i=60;i<altered.Count;i++)altered[i]=altered[i] with {Open=altered[i].Open*2,Close=altered[i].Close*2};
  foreach(var strategy in new[]{"sma","momentum"}){var a=Engine.Run(original,new Settings(Strategy:strategy));var b=Engine.Run(altered,new Settings(Strategy:strategy));Assert(a.Curve.Take(60).SequenceEqual(b.Curve.Take(60)),strategy+" has no future-data dependency");}
  var duplicate=bars.ToList();duplicate[1]=duplicate[0];try{Engine.Run(duplicate,new Settings());throw new Exception("Missing validation");}catch(ArgumentException){Console.WriteLine("PASS: reject duplicate dates");}
  var falling=bars.Select((b,i)=>b with {Open=i<3?100m:80m,Close=i<2?100m:80m}).ToList();var stop=Engine.Run(falling,new Settings(),new AlwaysLong());Assert(stop.Trades.Any(t=>t.Reason=="Prior-close stop"),"stop uses prior close and next open");
 }
}

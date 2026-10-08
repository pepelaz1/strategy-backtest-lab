namespace Backtest;
public record Bar(string Date, decimal Open, decimal Close);
public record Settings(string Strategy="sma", decimal FeeBps=10m, decimal Exposure=.8m, decimal InitialCash=10000m, decimal StopLoss=.08m);
public record Trade(string Date,string Side,decimal Price,decimal Quantity,decimal Fee,string Reason);
public record Point(string Date,decimal Equity);
public record Report(decimal InitialCash,decimal FinalEquity,decimal ReturnPercent,decimal MaxDrawdownPercent,decimal Fees,List<Point> Curve,List<Trade> Trades);
public interface IStrategy { bool Long(IReadOnlyList<Bar> bars,int index); }
public sealed class SmaStrategy : IStrategy {
 public bool Long(IReadOnlyList<Bar> bars,int index) => index>=12 && bars.Skip(index-5).Take(5).Average(b=>b.Close)>bars.Skip(index-12).Take(12).Average(b=>b.Close);
}
public sealed class MomentumStrategy : IStrategy {
 public bool Long(IReadOnlyList<Bar> bars,int index) => index>=6 && bars[index-1].Close>bars[index-6].Close;
}
public static class Engine {
 public static List<Bar> Sample()=>Enumerable.Range(0,100).Select(i=>{var close=100m+(decimal)(Math.Sin(i*.18)*10)+i*.12m;var open=i==0?100m:100m+(decimal)(Math.Sin((i-1)*.18)*10)+(i-1)*.12m;return new Bar(new DateTime(2025,1,1).AddDays(i).ToString("yyyy-MM-dd"),decimal.Round(open,2),decimal.Round(close,2));}).ToList();
 public static Report Run(IReadOnlyList<Bar> bars,Settings settings,IStrategy? strategy=null) {
  if(bars.Count<15||bars.Count>5000||settings.InitialCash<=0||settings.InitialCash>10000000||settings.Exposure<=0||settings.Exposure>1||settings.FeeBps<0||settings.FeeBps>100||settings.StopLoss<=0||settings.StopLoss>=1)throw new ArgumentException("Invalid bars or risk settings");
  DateTime? previous=null;
  foreach(var b in bars){if(b.Open<=0||b.Close<=0||b.Open>100000000||b.Close>100000000||!DateTime.TryParseExact(b.Date,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var date)||previous>=date)throw new ArgumentException("Positive prices and strictly increasing ISO dates required");previous=date;}
  strategy??=settings.Strategy switch {"sma"=>new SmaStrategy(),"momentum"=>new MomentumStrategy(),_=>throw new ArgumentException("Unknown strategy")};
  var cash=settings.InitialCash;decimal quantity=0,entry=0,fees=0,peak=cash,drawdown=0;var curve=new List<Point>();var trades=new List<Trade>();var feeRate=settings.FeeBps/10000m;
  for(int i=0;i<bars.Count;i++){
   var bar=bars[i];bool want=strategy.Long(bars,i);bool stopped=quantity>0&&i>0&&bars[i-1].Close<=entry*(1-settings.StopLoss);
   if(quantity>0&&(!want||stopped)){var proceeds=quantity*bar.Open;var fee=proceeds*feeRate;cash+=proceeds-fee;fees+=fee;trades.Add(new(bar.Date,"SELL",bar.Open,quantity,fee,stopped?"Prior-close stop":"Signal exit"));quantity=0;}
   else if(quantity==0&&want){var budget=cash*settings.Exposure;quantity=decimal.Floor(budget/(bar.Open*(1+feeRate))*1000000)/1000000;var cost=quantity*bar.Open;var fee=cost*feeRate;cash-=cost+fee;fees+=fee;entry=bar.Open;if(quantity>0)trades.Add(new(bar.Date,"BUY",bar.Open,quantity,fee,"Prior-close signal"));}
   var equity=cash+quantity*bar.Close;peak=Math.Max(peak,equity);drawdown=Math.Max(drawdown,(peak-equity)/peak);curve.Add(new(bar.Date,decimal.Round(equity,2)));
  }
  var final=cash+quantity*bars[^1].Close;return new(settings.InitialCash,decimal.Round(final,2),decimal.Round((final/settings.InitialCash-1)*100,3),decimal.Round(drawdown*100,3),decimal.Round(fees,2),curve,trades);
 }
}

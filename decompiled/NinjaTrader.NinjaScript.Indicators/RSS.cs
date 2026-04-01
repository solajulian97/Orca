using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Relative Spread Strength of the spread between two moving averages. TASC, October 2006, p. 16.
/// </summary>
public class RSS : Indicator
{
	private EMA ema1;

	private EMA ema2;

	private RSI rsi;

	private SMA sma;

	private Series<double> spread;

	private Series<double> rs;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "EMA1", GroupName = "NinjaScriptParameters", Order = 0)]
	public int EMA1 { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "EMA2", GroupName = "NinjaScriptParameters", Order = 1)]
	public int EMA2 { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Length", GroupName = "NinjaScriptParameters", Order = 2)]
	public int Length { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionRSS;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameRSS;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			EMA1 = 10;
			EMA2 = 40;
			Length = 5;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameRSS);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 20.0, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 80.0, Resource.NinjaScriptIndicatorUpper);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			spread = new Series<double>((NinjaScriptBase)(object)this);
			rs = new Series<double>((NinjaScriptBase)(object)this);
			ema1 = EMA(EMA1);
			ema2 = EMA(EMA2);
			rsi = RSI((ISeries<double>)(object)spread, Length, 1);
			sma = SMA((ISeries<double>)(object)rs, 5);
		}
	}

	protected override void OnBarUpdate()
	{
		spread[0] = ((NinjaScriptBase)ema1)[0] - ((NinjaScriptBase)ema2)[0];
		rs[0] = ((NinjaScriptBase)rsi)[0];
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)sma)[0];
	}
}

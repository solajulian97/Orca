using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The RVI (Relative Volatility Index) was developed by Donald Dorsey as a compliment to and a confirmation of momentum based indicators. When used to confirm other signals, only buy when the RVI is over 50 and only sell when the RVI is under 50.
/// </summary>
public class RVI : Indicator
{
	private double dnAvgH;

	private double dnAvgL;

	private double upAvgH;

	private double upAvgL;

	private double lastDnAvgH;

	private double lastDnAvgL;

	private double lastUpAvgH;

	private double lastUpAvgL;

	private Series<double> dnAvgHSeries;

	private Series<double> dnAvgLSeries;

	private Series<double> upAvgHSeries;

	private Series<double> upAvgLSeries;

	private int savedCurrentBar;

	private StdDev stdDevHigh;

	private StdDev stdDevLow;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Invalid comparison between Unknown and I4
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionRVI;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameRVI;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).IsOverlay = false;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameRVI);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 50.0, Resource.RVISignalLine);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			savedCurrentBar = -1;
			dnAvgH = (dnAvgL = (upAvgH = (upAvgL = (lastDnAvgH = (lastDnAvgL = (lastUpAvgH = (lastUpAvgL = 0.0)))))));
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			stdDevHigh = StdDev(((NinjaScriptBase)this).High, 10);
			stdDevLow = StdDev(((NinjaScriptBase)this).Low, 10);
			if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				dnAvgHSeries = new Series<double>((NinjaScriptBase)(object)this);
				dnAvgLSeries = new Series<double>((NinjaScriptBase)(object)this);
				upAvgHSeries = new Series<double>((NinjaScriptBase)(object)this);
				upAvgLSeries = new Series<double>((NinjaScriptBase)(object)this);
			}
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			((NinjaScriptBase)this).Value[0] = 50.0;
			return;
		}
		if (((NinjaScriptBase)this).CurrentBar != savedCurrentBar)
		{
			dnAvgH = (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported ? dnAvgHSeries[1] : lastDnAvgH);
			dnAvgL = (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported ? dnAvgLSeries[1] : lastDnAvgL);
			upAvgH = (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported ? upAvgHSeries[1] : lastUpAvgH);
			upAvgL = (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported ? upAvgLSeries[1] : lastUpAvgL);
			savedCurrentBar = ((NinjaScriptBase)this).CurrentBar;
		}
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).High[1];
		double num3 = ((NinjaScriptBase)this).Low[0];
		double num4 = ((NinjaScriptBase)this).Low[1];
		double num5 = 0.0;
		double num6 = 0.0;
		if (num > num2)
		{
			num5 = ((NinjaScriptBase)stdDevHigh)[0];
		}
		else if (num < num2)
		{
			num6 = ((NinjaScriptBase)stdDevHigh)[0];
		}
		double num7 = (lastUpAvgH = (upAvgH * (double)(Period - 1) + num5) / (double)Period);
		double num8 = (lastDnAvgH = (dnAvgH * (double)(Period - 1) + num6) / (double)Period);
		double num9 = 100.0 * (num7 / (num7 + num8));
		num5 = 0.0;
		num6 = 0.0;
		if (num3 > num4)
		{
			num5 = ((NinjaScriptBase)stdDevLow)[0];
		}
		else if (num3 < num4)
		{
			num6 = ((NinjaScriptBase)stdDevLow)[0];
		}
		double num10 = (lastUpAvgL = (upAvgL * (double)(Period - 1) + num5) / (double)Period);
		double num11 = (lastDnAvgL = (dnAvgL * (double)(Period - 1) + num6) / (double)Period);
		double num12 = 100.0 * (num10 / (num10 + num11));
		if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported)
		{
			dnAvgHSeries[0] = num8;
			dnAvgLSeries[0] = num11;
			upAvgHSeries[0] = num7;
			upAvgLSeries[0] = num10;
		}
		((NinjaScriptBase)this).Value[0] = ((((NinjaScriptBase)this).CurrentBar == 1) ? 50.0 : ((num9 + num12) / 2.0));
	}
}

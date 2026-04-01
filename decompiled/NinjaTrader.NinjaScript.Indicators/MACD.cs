using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The MACD (Moving Average Convergence/Divergence) is a trend following momentum indicator
/// that shows the relationship between two moving averages of prices.
/// </summary>
public class MACD : Indicator
{
	private Series<double> fastEma;

	private Series<double> slowEma;

	private double constant1;

	private double constant2;

	private double constant3;

	private double constant4;

	private double constant5;

	private double constant6;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Avg => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Default => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Diff => ((NinjaScriptBase)this).Values[2];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Fast { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Slow { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 2)]
	public int Smooth { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Invalid comparison between Unknown and I4
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionMACD;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameMACD;
			Fast = 12;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Slow = 26;
			Smooth = 9;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameMACD);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.NinjaScriptIndicatorAvg);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, 2f), (PlotStyle)0, Resource.NinjaScriptIndicatorDiff);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			constant1 = 2.0 / (double)(1 + Fast);
			constant2 = 1.0 - 2.0 / (double)(1 + Fast);
			constant3 = 2.0 / (double)(1 + Slow);
			constant4 = 1.0 - 2.0 / (double)(1 + Slow);
			constant5 = 2.0 / (double)(1 + Smooth);
			constant6 = 1.0 - 2.0 / (double)(1 + Smooth);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			fastEma = new Series<double>((NinjaScriptBase)(object)this);
			slowEma = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)this).Input[0];
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			fastEma[0] = num;
			slowEma[0] = num;
			((NinjaScriptBase)this).Value[0] = 0.0;
			Avg[0] = 0.0;
			Diff[0] = 0.0;
		}
		else
		{
			double num2 = constant1 * num + constant2 * fastEma[1];
			double num3 = constant3 * num + constant4 * slowEma[1];
			double num4 = num2 - num3;
			double num5 = constant5 * num4 + constant6 * Avg[1];
			fastEma[0] = num2;
			slowEma[0] = num3;
			((NinjaScriptBase)this).Value[0] = num4;
			Avg[0] = num5;
			Diff[0] = num4 - num5;
		}
	}
}

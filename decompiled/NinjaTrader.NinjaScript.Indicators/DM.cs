using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Directional Movement (DM). This is the same indicator as the ADX,
/// with the addition of the two directional movement indicators +DI
/// and -DI. +DI and -DI measure upward and downward momentum. A buy
/// signal is generated when +DI crosses -DI to the upside.
/// A sell signal is generated when -DI crosses +DI to the downside.
/// </summary>
public class DM : Indicator
{
	private Series<double> dmPlus;

	private Series<double> dmMinus;

	private Series<double> sumDmPlus;

	private Series<double> sumDmMinus;

	private Series<double> sumTr;

	private Series<double> tr;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> ADXPlot => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> DiPlus => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> DiMinus => ((NinjaScriptBase)this).Values[2];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Invalid comparison between Unknown and I4
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionDM;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameDM;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DarkSeaGreen, 2f), (PlotStyle)6, Resource.NinjaScriptIndicatorNameADX);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.DMPlusDI);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.DMMinusDI);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, 25.0, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, 75.0, Resource.NinjaScriptIndicatorUpper);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			dmPlus = new Series<double>((NinjaScriptBase)(object)this);
			dmMinus = new Series<double>((NinjaScriptBase)(object)this);
			sumDmPlus = new Series<double>((NinjaScriptBase)(object)this);
			sumDmMinus = new Series<double>((NinjaScriptBase)(object)this);
			sumTr = new Series<double>((NinjaScriptBase)(object)this);
			tr = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		double num3 = num - num2;
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			tr[0] = num3;
			dmPlus[0] = 0.0;
			dmMinus[0] = 0.0;
			sumTr[0] = tr[0];
			sumDmPlus[0] = dmPlus[0];
			sumDmMinus[0] = dmMinus[0];
			ADXPlot[0] = 50.0;
			return;
		}
		double num4 = ((NinjaScriptBase)this).Low[1];
		double num5 = ((NinjaScriptBase)this).High[1];
		double num6 = ((NinjaScriptBase)this).Close[1];
		tr[0] = Math.Max(Math.Abs(num2 - num6), Math.Max(num3, Math.Abs(num - num6)));
		dmPlus[0] = ((num - num5 > num4 - num2) ? Math.Max(num - num5, 0.0) : 0.0);
		dmMinus[0] = ((num4 - num2 > num - num5) ? Math.Max(num4 - num2, 0.0) : 0.0);
		double num7 = sumDmPlus[1];
		double num8 = sumDmMinus[1];
		double num9 = sumTr[1];
		if (((NinjaScriptBase)this).CurrentBar < Period)
		{
			sumTr[0] = num9 + tr[0];
			sumDmPlus[0] = num7 + dmPlus[0];
			sumDmMinus[0] = num8 + dmMinus[0];
		}
		else
		{
			sumTr[0] = num9 - sumTr[1] / (double)Period + tr[0];
			sumDmPlus[0] = num7 - num7 / (double)Period + dmPlus[0];
			sumDmMinus[0] = num8 - num8 / (double)Period + dmMinus[0];
		}
		double num10 = 100.0 * ((sumTr[0] == 0.0) ? 0.0 : (sumDmPlus[0] / sumTr[0]));
		double num11 = 100.0 * ((sumTr[0] == 0.0) ? 0.0 : (sumDmMinus[0] / sumTr[0]));
		double num12 = Math.Abs(num10 - num11);
		double num13 = num10 + num11;
		ADXPlot[0] = ((num13 == 0.0) ? 50.0 : (((double)(Period - 1) * ADXPlot[1] + 100.0 * num12 / num13) / (double)Period));
		DiPlus[0] = num10;
		DiMinus[0] = num11;
	}
}

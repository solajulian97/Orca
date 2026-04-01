using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Directional Movement Index. 
/// An indicator developed by J. Welles Wilder for identifying when a definable trend is present in an instrument. 
/// That is, the DMI tells whether an instrument is trending or not.
/// </summary>
public class DMI : Indicator
{
	private Series<double> dmMinus;

	private Series<double> dmPlus;

	private Series<double> tr;

	private SMA smaTr;

	private SMA smaDmPlus;

	private SMA smaDmMinus;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionDMI;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameDMI;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameDMI);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			dmMinus = new Series<double>((NinjaScriptBase)(object)this);
			dmPlus = new Series<double>((NinjaScriptBase)(object)this);
			tr = new Series<double>((NinjaScriptBase)(object)this);
			smaTr = SMA((ISeries<double>)(object)tr, Period);
			smaDmMinus = SMA((ISeries<double>)(object)dmMinus, Period);
			smaDmPlus = SMA((ISeries<double>)(object)dmPlus, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			dmMinus[0] = 0.0;
			dmPlus[0] = 0.0;
			tr[0] = num - num2;
			((NinjaScriptBase)this).Value[0] = 0.0;
			return;
		}
		double num3 = ((NinjaScriptBase)this).Low[1];
		double num4 = ((NinjaScriptBase)this).High[1];
		double num5 = ((NinjaScriptBase)this).Close[1];
		dmMinus[0] = ((num3 - num2 > num - num4) ? Math.Max(num3 - num2, 0.0) : 0.0);
		dmPlus[0] = ((num - num4 > num3 - num2) ? Math.Max(num - num4, 0.0) : 0.0);
		tr[0] = Math.Max(num - num2, Math.Max(Math.Abs(num - num5), Math.Abs(num2 - num5)));
		double num6 = ((NinjaScriptBase)smaTr)[0];
		double num7 = ((NinjaScriptBase)smaDmMinus)[0];
		double num8 = ((NinjaScriptBase)smaDmPlus)[0];
		double num9 = ((num6 == 0.0) ? 0.0 : (num7 / num6));
		double num10 = ((num6 == 0.0) ? 0.0 : (num8 / num6));
		((NinjaScriptBase)this).Value[0] = ((num10 + num9 == 0.0) ? 0.0 : ((num10 - num9) / (num10 + num9)));
	}
}

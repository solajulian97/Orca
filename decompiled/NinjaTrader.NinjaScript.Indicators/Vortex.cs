using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Vortex indicator is an oscillator used to identify trends. A bullish signal triggers when the VIPlus line crosses above the VIMinus line. A bearish signal triggers when the VIMinus line crosses above the VIPlus line.
/// </summary>
public class Vortex : Indicator
{
	private Series<double> vmPlus;

	private Series<double> vmMinus;

	private Series<double> trueRange;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> VIPlus => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> VIMinus => ((NinjaScriptBase)this).Values[1];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionVortex;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameVortex;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.RoyalBlue, Resource.NinjaScriptIndicatorVIPlus);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Red, Resource.NinjaScriptIndicatorVIMinus);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			vmPlus = new Series<double>((NinjaScriptBase)(object)this);
			vmMinus = new Series<double>((NinjaScriptBase)(object)this);
			trueRange = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar >= 1)
		{
			vmPlus[0] = Math.Abs(((NinjaScriptBase)this).High[0] - ((NinjaScriptBase)this).Low[1]);
			vmMinus[0] = Math.Abs(((NinjaScriptBase)this).Low[0] - ((NinjaScriptBase)this).High[1]);
			trueRange[0] = Math.Max(Math.Max(Math.Abs(((NinjaScriptBase)this).High[0] - ((NinjaScriptBase)this).Close[1]), Math.Abs(((NinjaScriptBase)this).Low[0] - ((NinjaScriptBase)this).Close[1])), ((NinjaScriptBase)this).High[0] - ((NinjaScriptBase)this).Low[0]);
			VIPlus[0] = ((NinjaScriptBase)SUM((ISeries<double>)(object)vmPlus, Period))[0] / ((NinjaScriptBase)SUM((ISeries<double>)(object)trueRange, Period))[0];
			VIMinus[0] = ((NinjaScriptBase)SUM((ISeries<double>)(object)vmMinus, Period))[0] / ((NinjaScriptBase)SUM((ISeries<double>)(object)trueRange, Period))[0];
		}
	}
}

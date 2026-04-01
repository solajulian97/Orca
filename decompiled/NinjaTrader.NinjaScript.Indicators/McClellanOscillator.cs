using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// McClellan Oscillator is the difference between two exponential moving averages of the NYSE advance decline spread. This indicator require ADV and DECL index data.
/// </summary>
public class McClellanOscillator : Indicator
{
	private Series<double> subtractAdvdecl;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "FastPeriod", GroupName = "NinjaScriptParameters", Order = 0)]
	public int FastPeriod { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "SlowPeriod", GroupName = "NinjaScriptParameters", Order = 1)]
	public int SlowPeriod { get; set; }

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NegativeColor", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1800)]
	public Brush NegativeColor { get; set; }

	[Browsable(false)]
	public string NegativeColorSerialize
	{
		get
		{
			return Serialize.BrushToString(NegativeColor);
		}
		set
		{
			NegativeColor = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Invalid comparison between Unknown and I4
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionMcClellanOscillator;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameMcClellanOscillator;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			FastPeriod = 19;
			SlowPeriod = 39;
			NegativeColor = Brushes.Red;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.LimeGreen, (DashStyleHelper)0, 1f), (PlotStyle)6, Resource.NinjaScriptIndicatorMcClellanOscillatorLine);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, 70.0, Resource.NinjaScriptIndicatorOverBoughtLine);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, -70.0, Resource.NinjaScriptIndicatorOverSoldLine);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries("^ADV");
			((NinjaScriptBase)this).AddDataSeries("^DECL");
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			subtractAdvdecl = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).BarsInProgress == 0 && ((NinjaScriptBase)this).CurrentBars[0] >= 0 && ((NinjaScriptBase)this).CurrentBars[1] >= 0 && ((NinjaScriptBase)this).CurrentBars[2] >= 0)
		{
			subtractAdvdecl[0] = ((NinjaScriptBase)this).Closes[1][0] - ((NinjaScriptBase)this).Closes[2][0];
			((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)EMA((ISeries<double>)(object)subtractAdvdecl, FastPeriod))[0] - ((NinjaScriptBase)EMA((ISeries<double>)(object)subtractAdvdecl, SlowPeriod))[0];
			if (((NinjaScriptBase)this).Value[0] < 0.0)
			{
				((NinjaScriptBase)this).PlotBrushes[0][0] = NegativeColor;
			}
		}
	}
}

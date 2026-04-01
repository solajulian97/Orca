using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Moving Average Ribbon is a series of incrementing moving averages.
/// </summary>
public class MovingAverageRibbon : Indicator
{
	[XmlIgnore]
	[Browsable(false)]
	public Series<double> MovingAverage1 => ((NinjaScriptBase)this).Values[0];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> MovingAverage2 => ((NinjaScriptBase)this).Values[1];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> MovingAverage3 => ((NinjaScriptBase)this).Values[2];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> MovingAverage4 => ((NinjaScriptBase)this).Values[3];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> MovingAverage5 => ((NinjaScriptBase)this).Values[4];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> MovingAverage6 => ((NinjaScriptBase)this).Values[5];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> MovingAverage7 => ((NinjaScriptBase)this).Values[6];

	[XmlIgnore]
	[Browsable(false)]
	public Series<double> MovingAverage8 => ((NinjaScriptBase)this).Values[7];

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "MovingAverage", GroupName = "NinjaScriptParameters", Order = 0)]
	public RibbonMAType MovingAverage { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "BasePeriod", GroupName = "NinjaScriptParameters", Order = 1)]
	public int BasePeriod { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "IncrementalPeriod", GroupName = "NinjaScriptParameters", Order = 2)]
	public int IncrementalPeriod { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected O, but got Unknown
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected O, but got Unknown
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Expected O, but got Unknown
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionMovingAverageRibbon;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameMovingAverageRibbon;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			MovingAverage = RibbonMAType.Exponential;
			BasePeriod = 10;
			IncrementalPeriod = 10;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Yellow, (DashStyleHelper)0, 1f), (PlotStyle)6, Resource.MovingAverageRibbonPlot1);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Gold, (DashStyleHelper)0, 1f), (PlotStyle)6, Resource.MovingAverageRibbonPlot2);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Goldenrod, (DashStyleHelper)0, 1f), (PlotStyle)6, Resource.MovingAverageRibbonPlot3);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Orange, (DashStyleHelper)0, 1f), (PlotStyle)6, Resource.MovingAverageRibbonPlot4);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DarkOrange, (DashStyleHelper)0, 1f), (PlotStyle)6, Resource.MovingAverageRibbonPlot5);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Chocolate, (DashStyleHelper)0, 1f), (PlotStyle)6, Resource.MovingAverageRibbonPlot6);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.OrangeRed, (DashStyleHelper)0, 1f), (PlotStyle)6, Resource.MovingAverageRibbonPlot7);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Red, (DashStyleHelper)0, 1f), (PlotStyle)6, Resource.MovingAverageRibbonPlot8);
		}
	}

	protected override void OnBarUpdate()
	{
		for (int i = 0; i < 8; i++)
		{
			Series<double> val = ((NinjaScriptBase)this).Values[i];
			val[0] = MovingAverage switch
			{
				RibbonMAType.Exponential => ((NinjaScriptBase)EMA(((NinjaScriptBase)this).Input, BasePeriod + IncrementalPeriod * i))[0], 
				RibbonMAType.Hull => ((NinjaScriptBase)HMA(((NinjaScriptBase)this).Input, BasePeriod + IncrementalPeriod * i))[0], 
				RibbonMAType.Simple => ((NinjaScriptBase)SMA(((NinjaScriptBase)this).Input, BasePeriod + IncrementalPeriod * i))[0], 
				RibbonMAType.Weighted => ((NinjaScriptBase)WMA(((NinjaScriptBase)this).Input, BasePeriod + IncrementalPeriod * i))[0], 
				_ => ((NinjaScriptBase)this).Values[i][0], 
			};
		}
	}
}

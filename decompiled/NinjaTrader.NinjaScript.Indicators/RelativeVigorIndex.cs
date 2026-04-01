using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Relative Vigor Index measures the strength of a trend by comparing an instruments closing price to its price range. It's based on the fact that prices tend to close higher than they open in up trends, and closer lower than they open in downtrends.
/// </summary>
public class RelativeVigorIndex : Indicator
{
	private Series<double> series1;

	private Series<double> series2;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Default => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Signal => ((NinjaScriptBase)this).Values[1];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionRelativeVigorIndex;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameRelativeVigorIndex;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 10;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Green, Resource.NinjaScriptIndicatorRelativeVigorIndex);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Red, Resource.NinjaScriptIndicatorSignal);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			series1 = new Series<double>((NinjaScriptBase)(object)this);
			series2 = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar >= 3)
		{
			series1[0] = (((NinjaScriptBase)this).Close[0] - ((NinjaScriptBase)this).Open[0] + 2.0 * (((NinjaScriptBase)this).Close[1] - ((NinjaScriptBase)this).Open[1]) + 2.0 * (((NinjaScriptBase)this).Close[2] - ((NinjaScriptBase)this).Open[2]) + (((NinjaScriptBase)this).Close[3] - ((NinjaScriptBase)this).Open[3])) / 6.0;
			series2[0] = (((NinjaScriptBase)this).High[0] - ((NinjaScriptBase)this).Low[0] + 2.0 * (((NinjaScriptBase)this).High[1] - ((NinjaScriptBase)this).Low[1]) + 2.0 * (((NinjaScriptBase)this).High[2] - ((NinjaScriptBase)this).Low[2]) + (((NinjaScriptBase)this).High[3] - ((NinjaScriptBase)this).Low[3])) / 6.0;
			double num = 0.0;
			double num2 = 0.0;
			for (int i = 0; i < Math.Min(((NinjaScriptBase)this).CurrentBar, Period); i++)
			{
				num += series1[i];
				num2 += series2[i];
			}
			if (num2 != 0.0)
			{
				((NinjaScriptBase)this).Value[0] = num / num2;
				Signal[0] = (((NinjaScriptBase)this).Value[0] + 2.0 * ((NinjaScriptBase)this).Value[1] + 2.0 * ((NinjaScriptBase)this).Value[2] + ((NinjaScriptBase)this).Value[3]) / 6.0;
			}
		}
	}
}

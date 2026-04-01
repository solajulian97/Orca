using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.Indicators;

public class FastCandleHighlight : Indicator
{
	private Series<double> barDurations;

	private SMA smaBarDuration;

	[NinjaScriptProperty]
	[Display(Name = "Calculation Mode", Description = "Mode to determine fast candles", Order = 1, GroupName = "Parameters")]
	public HighlightingMode Mode { get; set; }

	[NinjaScriptProperty]
	[XmlIgnore]
	[Display(Name = "Highlight Color", Description = "Color to highlight fast candles", Order = 2, GroupName = "Parameters")]
	public Brush HighlightColor { get; set; }

	[Browsable(false)]
	public string HighlightColorSerializable
	{
		get
		{
			return Serialize.BrushToString(HighlightColor);
		}
		set
		{
			HighlightColor = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Max Seconds (Fixed Mode)", Description = "Maximum seconds for a candle to be highlighted in Fixed Seconds mode", Order = 3, GroupName = "Parameters")]
	public int MaxSeconds { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Average Period (Dynamic Mode)", Description = "Number of bars for the moving average in Dynamic Average mode", Order = 4, GroupName = "Parameters")]
	public int AveragePeriod { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Percentage Threshold (Dynamic)", Description = "Percentage of the average duration under which a candle is highlighted (e.g., 50 means < 50% of the average time)", Order = 5, GroupName = "Parameters")]
	public int PercentageThreshold { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Invalid comparison between Unknown and I4
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Highlights candles that complete under a certain threshold.";
			((NinjaScriptBase)this).Name = "FastCandleHighlight";
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			HighlightColor = Brushes.Yellow;
			Mode = HighlightingMode.FixedSeconds;
			MaxSeconds = 10;
			AveragePeriod = 20;
			PercentageThreshold = 50;
		}
		else if ((int)((NinjaScript)this).State != 2 && (int)((NinjaScript)this).State == 4)
		{
			barDurations = new Series<double>((NinjaScriptBase)(object)this);
			smaBarDuration = SMA((ISeries<double>)(object)barDurations, AveragePeriod);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar < 1)
		{
			return;
		}
		double totalSeconds = (((NinjaScriptBase)this).Time[0] - ((NinjaScriptBase)this).Time[1]).TotalSeconds;
		barDurations[0] = totalSeconds;
		if (Mode == HighlightingMode.FixedSeconds)
		{
			if (totalSeconds <= (double)MaxSeconds)
			{
				((NinjaScriptBase)this).BarBrush = HighlightColor;
			}
		}
		else if (Mode == HighlightingMode.DynamicAverage && ((NinjaScriptBase)this).CurrentBar >= AveragePeriod)
		{
			double num = ((NinjaScriptBase)smaBarDuration)[1] * ((double)PercentageThreshold / 100.0);
			if (totalSeconds <= num)
			{
				((NinjaScriptBase)this).BarBrush = HighlightColor;
			}
		}
	}
}

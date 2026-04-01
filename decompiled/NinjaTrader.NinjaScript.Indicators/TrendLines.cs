using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Windows.Media;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Trend lines automatically plots recent trends by connect high points together for high trends and connecting low points together for low trends.
/// </summary>
public class TrendLines : Indicator
{
	private class TrendRay
	{
		public readonly int StartBar;

		public readonly double StartPrice;

		public readonly int EndBar;

		public readonly double EndPrice;

		public Ray Ray;

		public bool IsHigh;

		public TrendRay(int startBar, double startPrice, int endBar, double endPrice)
		{
			StartBar = startBar;
			StartPrice = startPrice;
			EndBar = endBar;
			EndPrice = endPrice;
		}
	}

	private class TrendQueue : Queue<TrendRay>
	{
		private readonly TrendLines instance;

		private TrendRay lastTrend;

		public new void Enqueue(TrendRay trend)
		{
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			if (((IndicatorRenderBase)instance).ChartControl != null)
			{
				string tag = $"{(trend.IsHigh ? Resource.TrendLinesTrendLineHigh : Resource.TrendLinesTrendLineLow)}_{trend.StartBar}";
				trend.Ray = Draw.Ray((NinjaScriptBase)(object)instance, tag, isAutoScale: false, ((NinjaScriptBase)instance).CurrentBar - trend.StartBar - ((NinjaScriptBase)instance).Displacement, trend.StartPrice, ((NinjaScriptBase)instance).CurrentBar - trend.EndBar - ((NinjaScriptBase)instance).Displacement, trend.EndPrice, trend.IsHigh ? instance.TrendLineHighStroke.Brush : instance.TrendLineLowStroke.Brush, trend.IsHigh ? instance.TrendLineHighStroke.DashStyleHelper : instance.TrendLineLowStroke.DashStyleHelper, (int)(trend.IsHigh ? instance.TrendLineHighStroke.Width : instance.TrendLineLowStroke.Width));
				trend.Ray.Stroke.Opacity = (trend.IsHigh ? instance.TrendLineHighStroke.Opacity : instance.TrendLineLowStroke.Opacity);
				if (lastTrend != null)
				{
					lastTrend.Ray.Stroke.Opacity = instance.OldTrendsOpacity;
				}
			}
			lastTrend = trend;
			base.Enqueue(trend);
			if (base.Count > instance.NumberOfTrendLines)
			{
				TrendRay trendRay = Dequeue();
				if (trendRay.Ray != null)
				{
					((IndicatorRenderBase)instance).RemoveDrawObject(((DrawingTool)trendRay.Ray).Tag);
				}
			}
		}

		public TrendQueue(TrendLines instance, int capacity)
			: base(capacity)
		{
			this.instance = instance;
		}
	}

	private int lastHighBar = -1;

	private int lastLowBar = -1;

	private double lastHighPrice = double.MinValue;

	private double lastLowPrice = double.MaxValue;

	private bool? highTrendIsActive;

	private bool alertIsArmed;

	private TrendRay highTrend;

	private TrendRay lowTrend;

	private TrendQueue trendLines;

	private Swing swing;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Strength", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Strength { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "NumberOfTrendLines", GroupName = "NinjaScriptParameters", Order = 1)]
	public int NumberOfTrendLines { get; set; }

	[Range(0, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "OldTrendsOpacity", GroupName = "NinjaScriptParameters", Order = 2)]
	public int OldTrendsOpacity { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "AlertOnBreak", GroupName = "NinjaScriptParameters", Order = 3)]
	public bool AlertOnBreak { get; set; }

	[PropertyEditor("NinjaTrader.Gui.Tools.FilePathPicker", Filter = "WAV Files (*.wav)|*.wav")]
	[Display(ResourceType = typeof(Resource), Name = "AlertOnBreakSound", GroupName = "NinjaScriptParameters", Order = 4)]
	public string AlertOnBreakSound { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "TrendLinesTrendLineHigh", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1800)]
	public Stroke TrendLineHighStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "TrendLinesTrendLineLow", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1810)]
	public Stroke TrendLineLowStroke { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Invalid comparison between Unknown and I4
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionTrendLines;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameTrendLines;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((IndicatorBase)this).PaintPriceMarkers = false;
			Strength = 5;
			NumberOfTrendLines = 1;
			OldTrendsOpacity = 25;
			AlertOnBreak = false;
			AlertOnBreakSound = Path.Combine(Globals.InstallDir, "sounds", "Alert2.wav");
			TrendLineHighStroke = new Stroke((Brush)Brushes.DarkCyan, 1f);
			TrendLineLowStroke = new Stroke((Brush)Brushes.Goldenrod, 1f);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.White, Resource.TrendLinesCurrentTrendLine);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			swing = Swing(((NinjaScriptBase)this).Input, Strength);
			trendLines = new TrendQueue(this, NumberOfTrendLines);
			if (((IndicatorRenderBase)this).ChartPanel == null)
			{
				Draw.TextFixed((NinjaScriptBase)(object)this, "TrendLinesStrategyAnalyzer", Resource.TrendLinesNotVisible, TextPosition.BottomRight);
			}
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Invalid comparison between Unknown and I4
		//IL_040b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).CurrentBar < 0)
		{
			return;
		}
		int num = swing.SwingHighBar(0, 1, Strength + 1);
		if (num != -1)
		{
			double num2 = ((!(((NinjaScriptBase)this).Input is PriceSeries) && !(((NinjaScriptBase)this).Input is Bars)) ? ((NinjaScriptBase)this).Input[num] : ((NinjaScriptBase)this).High[num]);
			if (num2 < lastHighPrice && lastHighBar > -1)
			{
				highTrend = new TrendRay(lastHighBar, lastHighPrice, ((NinjaScriptBase)this).CurrentBar - num, num2)
				{
					IsHigh = true
				};
				trendLines.Enqueue(highTrend);
				highTrendIsActive = true;
				alertIsArmed = true;
			}
			lastHighBar = ((NinjaScriptBase)this).CurrentBar - num;
			lastHighPrice = num2;
		}
		int num3 = swing.SwingLowBar(0, 1, Strength + 1);
		if (num3 != -1)
		{
			double num4 = ((!(((NinjaScriptBase)this).Input is PriceSeries) && !(((NinjaScriptBase)this).Input is Bars)) ? ((NinjaScriptBase)this).Input[num3] : ((NinjaScriptBase)this).Low[num3]);
			if (num4 > lastLowPrice && lastLowBar > -1)
			{
				lowTrend = new TrendRay(lastLowBar, lastLowPrice, ((NinjaScriptBase)this).CurrentBar - num3, num4);
				trendLines.Enqueue(lowTrend);
				highTrendIsActive = false;
				alertIsArmed = true;
			}
			lastLowBar = ((NinjaScriptBase)this).CurrentBar - num3;
			lastLowPrice = num4;
		}
		if (!highTrendIsActive.HasValue)
		{
			return;
		}
		if (((IndicatorRenderBase)this).ChartControl == null || (int)((IndicatorRenderBase)this).ChartControl.BarSpacingType == 3)
		{
			if (highTrendIsActive.Value)
			{
				double num5 = (highTrend.EndPrice - highTrend.StartPrice) / (double)(highTrend.EndBar - highTrend.StartBar);
				((NinjaScriptBase)this).Values[0][0] = num5 * (double)((NinjaScriptBase)this).CurrentBar - (num5 * (double)highTrend.StartBar - highTrend.StartPrice);
			}
			else
			{
				double num6 = (lowTrend.EndPrice - lowTrend.StartPrice) / (double)(lowTrend.EndBar - lowTrend.StartBar);
				((NinjaScriptBase)this).Values[0][0] = num6 * (double)((NinjaScriptBase)this).CurrentBar - (num6 * (double)lowTrend.StartBar - lowTrend.StartPrice);
			}
		}
		else if (highTrendIsActive.Value)
		{
			double slotIndexByTime = ((IndicatorRenderBase)this).ChartControl.GetSlotIndexByTime(((IndicatorRenderBase)this).ChartBars.GetTimeByBarIdx(((IndicatorRenderBase)this).ChartControl, highTrend.StartBar));
			double slotIndexByTime2 = ((IndicatorRenderBase)this).ChartControl.GetSlotIndexByTime(((IndicatorRenderBase)this).ChartBars.GetTimeByBarIdx(((IndicatorRenderBase)this).ChartControl, highTrend.EndBar));
			double slotIndexByTime3 = ((IndicatorRenderBase)this).ChartControl.GetSlotIndexByTime(((NinjaScriptBase)this).Time[0]);
			double num7 = (highTrend.EndPrice - highTrend.StartPrice) / (slotIndexByTime2 - slotIndexByTime);
			((NinjaScriptBase)this).Values[0][0] = num7 * slotIndexByTime3 - (num7 * slotIndexByTime - highTrend.StartPrice);
		}
		else
		{
			double slotIndexByTime4 = ((IndicatorRenderBase)this).ChartControl.GetSlotIndexByTime(((IndicatorRenderBase)this).ChartBars.GetTimeByBarIdx(((IndicatorRenderBase)this).ChartControl, lowTrend.StartBar));
			double slotIndexByTime5 = ((IndicatorRenderBase)this).ChartControl.GetSlotIndexByTime(((IndicatorRenderBase)this).ChartBars.GetTimeByBarIdx(((IndicatorRenderBase)this).ChartControl, lowTrend.EndBar));
			double slotIndexByTime6 = ((IndicatorRenderBase)this).ChartControl.GetSlotIndexByTime(((NinjaScriptBase)this).Time[0]);
			double num8 = (lowTrend.EndPrice - lowTrend.StartPrice) / (slotIndexByTime5 - slotIndexByTime4);
			((NinjaScriptBase)this).Values[0][0] = num8 * slotIndexByTime6 - (num8 * slotIndexByTime4 - lowTrend.StartPrice);
		}
		if ((int)((NinjaScript)this).State == 7 && AlertOnBreak && alertIsArmed && (((NinjaScriptBase)this).CrossAbove(((NinjaScriptBase)this).Input, ((NinjaScriptBase)this).Values[0][0], 1) || ((NinjaScriptBase)this).CrossBelow(((NinjaScriptBase)this).Input, ((NinjaScriptBase)this).Values[0][0], 1)))
		{
			((NinjaScriptBase)this).Alert(string.Empty, (Priority)0, string.Format(Resource.TrendLinesTrendLineBroken, highTrendIsActive.Value ? Resource.TrendLinesTrendLineHigh : Resource.TrendLinesTrendLineLow), AlertOnBreakSound, 0, (Brush)Brushes.Transparent, highTrendIsActive.Value ? TrendLineHighStroke.Brush : TrendLineLowStroke.Brush);
			alertIsArmed = false;
		}
	}

	public override void OnCalculateMinMax()
	{
		double minValue = double.MaxValue;
		double maxValue = double.MinValue;
		foreach (TrendRay trendLine in trendLines)
		{
			AutoScalePerRay(trendLine.Ray, ref minValue, ref maxValue);
		}
		((IndicatorRenderBase)this).MinValue = minValue;
		((IndicatorRenderBase)this).MaxValue = maxValue;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
	}

	private void AutoScalePerRay(Ray ray, ref double minValue, ref double maxValue)
	{
		if (ray == null)
		{
			return;
		}
		int barIdxByTime = ((IndicatorRenderBase)this).ChartBars.GetBarIdxByTime(((IndicatorRenderBase)this).ChartControl, ray.StartAnchor.Time);
		if (barIdxByTime >= ((IndicatorRenderBase)this).ChartBars.FromIndex - ((NinjaScriptBase)this).Displacement && barIdxByTime <= ((IndicatorRenderBase)this).ChartBars.ToIndex - ((NinjaScriptBase)this).Displacement)
		{
			if (ray.StartAnchor.Price < minValue)
			{
				minValue = ray.StartAnchor.Price;
			}
			if (ray.StartAnchor.Price > maxValue)
			{
				maxValue = ray.StartAnchor.Price;
			}
		}
		int barIdxByTime2 = ((IndicatorRenderBase)this).ChartBars.GetBarIdxByTime(((IndicatorRenderBase)this).ChartControl, ray.EndAnchor.Time);
		if (barIdxByTime2 >= ((IndicatorRenderBase)this).ChartBars.FromIndex - ((NinjaScriptBase)this).Displacement && barIdxByTime2 <= ((IndicatorRenderBase)this).ChartBars.ToIndex - ((NinjaScriptBase)this).Displacement)
		{
			if (ray.EndAnchor.Price < minValue)
			{
				minValue = ray.EndAnchor.Price;
			}
			if (ray.EndAnchor.Price > maxValue)
			{
				maxValue = ray.EndAnchor.Price;
			}
		}
	}
}

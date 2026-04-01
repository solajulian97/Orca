using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;

namespace NinjaTrader.NinjaScript.Indicators;

[TypeConverter("NinjaTrader.NinjaScript.Indicators.FVGTypeConverter")]
public class FVG : Indicator
{
	private class Gap
	{
		public int BarIndex { get; }

		public bool IsUp { get; }

		public Dictionary<double, int> Ticks { get; } = new Dictionary<double, int>();

		public Gap(Instrument instrument, int barIndex, double barsAgoPrice, double currentBarPrice, int barsToKeep = 0)
		{
			BarIndex = barIndex;
			IsUp = barsAgoPrice < currentBarPrice;
			if (IsUp)
			{
				double num;
				for (num = barsAgoPrice; num <= currentBarPrice; num += instrument.MasterInstrument.TickSize)
				{
					num = instrument.MasterInstrument.RoundToTickSize(num);
					Ticks.Add(num, (barsToKeep == 0) ? (-1) : (BarIndex + barsToKeep));
				}
			}
			else
			{
				double num2;
				for (num2 = barsAgoPrice; num2 >= currentBarPrice; num2 -= instrument.MasterInstrument.TickSize)
				{
					num2 = instrument.MasterInstrument.RoundToTickSize(num2);
					Ticks.Add(num2, (barsToKeep == 0) ? (-1) : (BarIndex + barsToKeep));
				}
			}
		}

		public void Check(Instrument instrument, double minPrice, double maxPrice, int barIndex, bool terminateAll)
		{
			if (barIndex < BarIndex + 2)
			{
				return;
			}
			double num;
			for (num = minPrice; num <= maxPrice; num += instrument.MasterInstrument.TickSize)
			{
				num = instrument.MasterInstrument.RoundToTickSize(num);
				if (Ticks.ContainsKey(num) && Ticks[num] == -1)
				{
					if (terminateAll)
					{
						foreach (double item in Ticks.Keys.ToList())
						{
							Ticks[item] = barIndex;
						}
						break;
					}
					Ticks[num] = Math.Max(barIndex, Ticks[num]);
				}
			}
		}
	}

	private readonly Dictionary<int, Gap> gaps = new Dictionary<int, Gap>();

	[Range(1, 3)]
	[Display(ResourceType = typeof(Resource), Name = "FVGExtendUntil", GroupName = "NinjaScriptParameters", Order = 40)]
	[RefreshProperties(RefreshProperties.All)]
	[TypeConverter(typeof(FVGEnumConverter))]
	[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
	public int ExtendUntil { get; set; }

	[Range(1, int.MaxValue)]
	[Display(ResourceType = typeof(Resource), Name = "FVGMaxFVG", GroupName = "NinjaScriptParameters", Order = 70)]
	public int MaxFvg { get; set; }

	[Range(1, int.MaxValue)]
	[Display(ResourceType = typeof(Resource), Name = "FVGMinimumTicks", GroupName = "NinjaScriptParameters", Order = 60)]
	public int MinimumTicks { get; set; } = 10;

	[Range(1, int.MaxValue)]
	[Display(ResourceType = typeof(Resource), Name = "FVGBarsToExtend", GroupName = "NinjaScriptParameters", Order = 50)]
	public int BarsToExtend { get; set; } = 10;

	[Range(0, 100)]
	[Display(ResourceType = typeof(Resource), Name = "Opacity", GroupName = "NinjaScriptParameters", Order = 30)]
	public int Opacity { get; set; } = 40;

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptChartStylePointAndFigureUpColor", GroupName = "NinjaScriptParameters", Order = 10)]
	public Brush FvgColorUp { get; set; }

	[Browsable(false)]
	public string FvgColorUpSerializable
	{
		get
		{
			return Serialize.BrushToString(FvgColorUp);
		}
		set
		{
			FvgColorUp = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptChartStylePointAndFigureDownColor", GroupName = "NinjaScriptParameters", Order = 20)]
	public Brush FvgColorDown { get; set; }

	[Browsable(false)]
	public string FvgColorDownSerializable
	{
		get
		{
			return Serialize.BrushToString(FvgColorDown);
		}
		set
		{
			FvgColorDown = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.FVGDescription;
			((NinjaScriptBase)this).Name = Resource.FVGName;
			ExtendUntil = 1;
			MaxFvg = 10;
			MinimumTicks = 1;
			BarsToExtend = 10;
			FvgColorDown = Brushes.Red;
			FvgColorUp = Brushes.LimeGreen;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((IndicatorRenderBase)this).ZOrder = -1;
		}
	}

	protected override Point[] OnGetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		if (!((IndicatorRenderBase)this).IsSelected || gaps.Count == 0)
		{
			return Array.Empty<Point>();
		}
		List<Point> list = new List<Point>();
		foreach (Gap value in gaps.Values)
		{
			int xByTime = chartControl.GetXByTime(((IndicatorRenderBase)this).ChartBars.GetTimeByBarIdx(chartControl, value.BarIndex + ((NinjaScriptBase)this).Displacement));
			int num = ((value.Ticks[value.Ticks.Keys.Min()] > 0) ? (value.Ticks[value.Ticks.Keys.Min()] + ((NinjaScriptBase)this).Displacement) : ((IndicatorRenderBase)this).ChartBars.ToIndex);
			int xByTime2 = chartControl.GetXByTime(((IndicatorRenderBase)this).ChartBars.GetTimeByBarIdx(chartControl, num));
			int yByValue = chartScale.GetYByValue(value.Ticks.Keys.Min());
			list.Add(new Point(xByTime, yByValue));
			list.Add(new Point(xByTime2, yByValue));
			if (Math.Abs(value.Ticks.Keys.Min() - value.Ticks.Keys.Max()) > ((NinjaScriptBase)this).TickSize * 0.1)
			{
				yByValue = chartScale.GetYByValue(value.Ticks.Keys.Max());
				list.Add(new Point(xByTime, yByValue));
				list.Add(new Point(xByTime2, yByValue));
			}
		}
		return list.ToArray();
	}

	protected override void OnBarUpdate()
	{
		foreach (Gap value in gaps.Values)
		{
			value.Check(((NinjaScriptBase)this).Instrument, ((NinjaScriptBase)this).Low[0], ((NinjaScriptBase)this).High[0], ((NinjaScriptBase)this).CurrentBar, ExtendUntil == 2);
		}
		if (((NinjaScriptBase)this).CurrentBar < 3)
		{
			return;
		}
		int num = (int)Math.Round((((NinjaScriptBase)this).Low[0] - ((NinjaScriptBase)this).High[2]) / ((NinjaScriptBase)this).TickSize);
		int num2 = (int)Math.Round((((NinjaScriptBase)this).Low[2] - ((NinjaScriptBase)this).High[0]) / ((NinjaScriptBase)this).TickSize);
		Gap gap = null;
		if (num >= MinimumTicks)
		{
			gap = new Gap(((NinjaScriptBase)this).Instrument, ((NinjaScriptBase)this).CurrentBar - 1, ((NinjaScriptBase)this).High[2], ((NinjaScriptBase)this).Low[0], (ExtendUntil == 3) ? BarsToExtend : 0);
		}
		else if (num2 >= MinimumTicks)
		{
			gap = new Gap(((NinjaScriptBase)this).Instrument, ((NinjaScriptBase)this).CurrentBar - 1, ((NinjaScriptBase)this).Low[2], ((NinjaScriptBase)this).High[0], (ExtendUntil == 3) ? BarsToExtend : 0);
		}
		if (gap != null)
		{
			if (gaps.ContainsKey(((NinjaScriptBase)this).CurrentBar - 1))
			{
				gaps[((NinjaScriptBase)this).CurrentBar - 1] = gap;
			}
			else
			{
				gaps.Add(((NinjaScriptBase)this).CurrentBar - 1, gap);
			}
		}
		else
		{
			gaps.Remove(((NinjaScriptBase)this).CurrentBar - 1);
		}
		while (gaps.Count > MaxFvg)
		{
			gaps.Remove(gaps.Keys.Min());
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		foreach (Gap value in gaps.Values)
		{
			foreach (KeyValuePair<double, int> tick in value.Ticks)
			{
				double num = chartScale.GetYByValue(tick.Key + ((NinjaScriptBase)this).TickSize * 0.5);
				double num2 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, value.BarIndex + ((NinjaScriptBase)this).Displacement);
				double num3 = (double)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, (tick.Value > 0) ? (tick.Value + ((NinjaScriptBase)this).Displacement) : ((IndicatorRenderBase)this).ChartBars.ToIndex) - num2;
				double num4 = (double)chartScale.GetYByValue(tick.Key - ((NinjaScriptBase)this).TickSize * 0.5) - num;
				((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF((float)num2, (float)num, (float)num3, (float)num4), value.IsUp ? DxExtensions.ToDxBrush(FvgColorUp, ((IndicatorRenderBase)this).RenderTarget, (float)Opacity / 100f) : DxExtensions.ToDxBrush(FvgColorDown, ((IndicatorRenderBase)this).RenderTarget, (float)Opacity / 100f));
			}
		}
	}
}

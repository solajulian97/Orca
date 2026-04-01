using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.NinjaScript;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

[TypeConverter("NinjaTrader.NinjaScript.MarketAnalyzerColumns.ChartMiniConverter")]
public class ChartMini : MarketAnalyzerColumnRenderBase
{
	private Brush color;

	private Brush fillBrush;

	private Pen linePen;

	private int opacity;

	private Brush outlineBrush;

	private DateTime Now
	{
		get
		{
			if (Connection.PlaybackConnection == null)
			{
				return Globals.Now;
			}
			return Connection.PlaybackConnection.Now;
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnChartMiniColor", GroupName = "GuiPropertyCategoryVisual", Order = 10)]
	public Brush Color
	{
		get
		{
			return color;
		}
		set
		{
			color = value;
			fillBrush = null;
		}
	}

	[Range(0, 100)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnChartMiniOpacity", GroupName = "GuiPropertyCategoryVisual", Order = 20)]
	public int Opacity
	{
		get
		{
			return opacity;
		}
		set
		{
			opacity = value;
			fillBrush = null;
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnChartMiniOutline", GroupName = "GuiPropertyCategoryVisual", Order = 30)]
	public Brush OutlineBrush
	{
		get
		{
			return outlineBrush;
		}
		set
		{
			outlineBrush = value;
			linePen = null;
		}
	}

	[Browsable(false)]
	public string OutlineBrushSeralizer
	{
		get
		{
			return Serialize.BrushToString(OutlineBrush);
		}
		set
		{
			OutlineBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnChartMiniSpan", GroupName = "NinjaScriptSetup", Order = 300)]
	public ChartSpan Span { get; set; }

	[Browsable(false)]
	public string UpBrushSeralizer
	{
		get
		{
			return Serialize.BrushToString(Color);
		}
		set
		{
			Color = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Invalid comparison between Unknown and I4
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionChartMini;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameChartMini;
			Color = Brushes.DimGray;
			((NinjaScriptBase)this).IsDataSeriesRequired = true;
			Opacity = 50;
			OutlineBrush = Brushes.DimGray;
			Span = ChartSpan.Day;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).BarsPeriod = ((Span == ChartSpan.Day || Span == ChartSpan.Week) ? new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)4,
				Value = 1
			} : ((Span == ChartSpan.Month || Span == ChartSpan.Year) ? new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)5,
				Value = 1
			} : new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)3,
				Value = 1
			}));
			((MarketAnalyzerColumnBase)this).DaysBack = ((Span == ChartSpan.Week) ? 7 : ((Span == ChartSpan.Month) ? 30 : ((Span != ChartSpan.Year) ? 1 : 365)));
			((MarketAnalyzerColumnBase)this).RangeType = (RangeType)1;
			((MarketAnalyzerColumnBase)this).TradingHoursInstance = TradingHours.All.FirstOrDefault((TradingHours s) => s.Name == TradingHours.SystemDefault);
			((NinjaScriptBase)this).To = Now.Date;
		}
	}

	public override string Format(double value)
	{
		if (value != double.MinValue && ((NinjaScriptBase)this).Instrument != null)
		{
			return ((NinjaScriptBase)this).Instrument.MasterInstrument.FormatPrice(value, true);
		}
		return string.Empty;
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (marketDataUpdate.IsReset)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = double.MinValue;
		}
		else if ((int)marketDataUpdate.MarketDataType == 2)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = marketDataUpdate.Price;
		}
	}

	public override void OnRender(DrawingContext dc, Size renderSize)
	{
		DateTime dateTime = Span switch
		{
			ChartSpan.Min1 => Now.AddMinutes(-1.0), 
			ChartSpan.Min5 => Now.AddMinutes(-5.0), 
			ChartSpan.Min15 => Now.AddMinutes(-15.0), 
			ChartSpan.Min30 => Now.AddMinutes(-30.0), 
			ChartSpan.Min60 => Now.AddMinutes(-60.0), 
			ChartSpan.Min240 => Now.AddMinutes(-240.0), 
			ChartSpan.Week => Now.AddDays(-7.0), 
			ChartSpan.Month => Now.Date.AddMonths(-1), 
			ChartSpan.Year => Now.Date.AddYears(-1), 
			_ => Now.AddDays(-1.0), 
		};
		if (fillBrush == null)
		{
			fillBrush = new SolidColorBrush
			{
				Color = (Color as SolidColorBrush).Color,
				Opacity = (double)Opacity / 100.0
			};
		}
		if (linePen == null)
		{
			linePen = new Pen(OutlineBrush, 1.0);
		}
		int num = Math.Max(0, ((NinjaScriptBase)this).BarsArray[0].GetBar(dateTime) - 1);
		List<LineSegment> list = new List<LineSegment>();
		int num2 = 1;
		double num3 = double.MinValue;
		double num4 = double.MinValue;
		DateTime now = Now;
		int num5 = -1;
		double num6 = double.MaxValue;
		int num7 = num2;
		int num8 = (int)Math.Floor(renderSize.Height) - num2;
		int num9 = (int)Math.Floor(renderSize.Width) - 2 * num2;
		int num10 = -1;
		Point start = default(Point);
		DateTime value = dateTime.Date.AddDays(-1.0);
		int num11 = (int)(0.0 - now.Subtract(value).TotalMinutes / Math.Max(Math.Round(now.Subtract(dateTime).TotalMinutes), 1.0) * (double)num9) + num9 + num7;
		((NinjaScriptBase)this).BarsArray[0].BarsSeries.SyncRoot.EnterReadLock();
		try
		{
			for (int i = num + 1; i < ((NinjaScriptBase)this).BarsArray[0].Count; i++)
			{
				double close = ((NinjaScriptBase)this).BarsArray[0].GetClose(i);
				num6 = Math.Min(close, num6);
				num4 = Math.Max(close, num4);
			}
			if (num6 == double.MaxValue || num4 == double.MinValue)
			{
				return;
			}
			for (int j = num; j < ((NinjaScriptBase)this).BarsArray[0].Count; j++)
			{
				DateTime dateTime2 = new DateTime(Math.Min(Math.Max(((NinjaScriptBase)this).BarsArray[0].GetTime(j).Ticks, dateTime.Ticks), now.Ticks));
				double num12 = ((j == num) ? Math.Max(num6, Math.Min(((NinjaScriptBase)this).BarsArray[0].GetClose(j), num4)) : ((NinjaScriptBase)this).BarsArray[0].GetClose(j));
				int num13 = ((j == num) ? num2 : (num11 + Convert.ToInt32(dateTime2.Subtract(value).TotalSeconds * ((double)num9 / Math.Max(now.Subtract(dateTime).TotalSeconds, 1.0)))));
				if (num13 == num10)
				{
					num3 = Math.Max(num12, num3);
					num12 = num3;
				}
				else
				{
					num3 = num12;
				}
				int num14 = num2 + Convert.ToInt32((num4 - num12) / Math.Max(((NinjaScriptBase)this).BarsArray[0].Instrument.MasterInstrument.TickSize, num4 - num6) * (double)(num8 - num2));
				if (j == num)
				{
					start = new Point(num2, num14);
				}
				if (j == ((NinjaScriptBase)this).BarsArray[0].Count - 1)
				{
					num5 = num13;
				}
				if (num13 == num10)
				{
					list[list.Count - 1] = new LineSegment(new Point(num13, num14), isStroked: true);
				}
				else
				{
					list.Add(new LineSegment(new Point(num13, num14), isStroked: true));
				}
				num10 = num13;
			}
		}
		finally
		{
			((NinjaScriptBase)this).BarsArray[0].BarsSeries.SyncRoot.ExitReadLock();
		}
		if (list.Count > 0)
		{
			PathGeometry geometry = new PathGeometry(new List<PathFigure>
			{
				new PathFigure(start, list.ToArray(), closed: false)
			});
			dc.DrawGeometry(null, linePen, geometry);
			list.Add(new LineSegment(new Point(num5, num2 + num8), isStroked: true));
			list.Add(new LineSegment(new Point(num7, num2 + num8), isStroked: true));
			PathGeometry geometry2 = new PathGeometry(new List<PathFigure>
			{
				new PathFigure(start, list.ToArray(), closed: true)
			});
			dc.DrawGeometry(fillBrush, null, geometry2);
		}
	}
}

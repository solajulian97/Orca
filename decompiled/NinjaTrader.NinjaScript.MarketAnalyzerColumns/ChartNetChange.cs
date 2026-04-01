using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.NinjaScript;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

[TypeConverter("NinjaTrader.NinjaScript.MarketAnalyzerColumns.ChartNetChangeConverter")]
public class ChartNetChange : MarketAnalyzerColumnRenderBase
{
	private Brush downArea;

	private Brush downAreaBrush;

	private Brush downOutline;

	private Pen downOutlinePen;

	private double lastClose = double.MinValue;

	private DateTime nextTradingDayBegin = Globals.MaxDate;

	private int opacity;

	private SessionIterator sessionIterator;

	private Brush upArea;

	private Brush upAreaBrush;

	private Brush upOutline;

	private Pen upOutlinePen;

	private DateTime tradingDayBegin = Globals.MinDate;

	private DateTime tradingDayEnd = Globals.MinDate;

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
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnChartNetChangeDownArea", GroupName = "GuiPropertyCategoryVisual", Order = 30)]
	public Brush DownArea
	{
		get
		{
			return downArea;
		}
		set
		{
			downArea = value;
			downAreaBrush = null;
		}
	}

	[Browsable(false)]
	public string DownAreaSeralizer
	{
		get
		{
			return Serialize.BrushToString(downArea);
		}
		set
		{
			downArea = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnChartNetChangeDownOutline", GroupName = "GuiPropertyCategoryVisual", Order = 60)]
	public Brush DownOutline
	{
		get
		{
			return downOutline;
		}
		set
		{
			downOutline = value;
			downOutlinePen = null;
		}
	}

	[Browsable(false)]
	public string DownOutlineSeralizer
	{
		get
		{
			return Serialize.BrushToString(downOutline);
		}
		set
		{
			downOutline = Serialize.StringToBrush(value);
		}
	}

	[Range(0, 100)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnChartMiniOpacity", GroupName = "GuiPropertyCategoryVisual", Order = 40)]
	public int Opacity
	{
		get
		{
			return opacity;
		}
		set
		{
			opacity = value;
			upAreaBrush = null;
			downAreaBrush = null;
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnChartNetChangeUpArea", GroupName = "GuiPropertyCategoryVisual", Order = 20)]
	public Brush UpArea
	{
		get
		{
			return upArea;
		}
		set
		{
			upArea = value;
			upAreaBrush = null;
		}
	}

	[Browsable(false)]
	public string UpAreaSeralizer
	{
		get
		{
			return Serialize.BrushToString(upArea);
		}
		set
		{
			upArea = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnChartNetChangeUpOutline", GroupName = "GuiPropertyCategoryVisual", Order = 50)]
	public Brush UpOutline
	{
		get
		{
			return upOutline;
		}
		set
		{
			upOutline = value;
			upOutlinePen = null;
		}
	}

	[Browsable(false)]
	public string UpOutlineSeralizer
	{
		get
		{
			return Serialize.BrushToString(upOutline);
		}
		set
		{
			upOutline = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Invalid comparison between Unknown and I4
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionChartNetChange;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameChartNetChange;
			DownArea = Globals.GeneralOptions.BrushDownPrimary;
			DownOutline = Globals.GeneralOptions.BrushDownPrimary;
			((NinjaScriptBase)this).IsDataSeriesRequired = true;
			Opacity = 50;
			UpArea = Globals.GeneralOptions.BrushUpPrimary;
			UpOutline = Globals.GeneralOptions.BrushUpPrimary;
		}
		else
		{
			if ((int)((NinjaScript)this).State != 2)
			{
				return;
			}
			((NinjaScriptBase)this).BarsPeriod = new BarsPeriod
			{
				BarsPeriodType = (BarsPeriodType)4,
				Value = 1
			};
			((MarketAnalyzerColumnBase)this).DaysBack = 0;
			((MarketAnalyzerColumnBase)this).RangeType = (RangeType)1;
			((NinjaScriptBase)this).To = Now.Date;
			if (((NinjaScriptBase)this).Instrument == null)
			{
				return;
			}
			((MarketAnalyzerColumnBase)this).TradingHoursInstance = ((NinjaScriptBase)this).Instrument.MasterInstrument.TradingHours;
			sessionIterator = new SessionIterator(((MarketAnalyzerColumnBase)this).TradingHoursInstance);
			if (sessionIterator.IsInSession(Now, false, true))
			{
				tradingDayBegin = sessionIterator.GetTradingDayBeginLocal(sessionIterator.ActualTradingDayExchange);
				tradingDayEnd = sessionIterator.ActualTradingDayEndLocal;
				return;
			}
			DateTime dateTime = sessionIterator.ActualTradingDayExchange;
			bool flag = true;
			while (true)
			{
				dateTime = dateTime.AddDays(-1.0);
				if (sessionIterator.IsTradingDayDefined(dateTime))
				{
					if (!flag)
					{
						break;
					}
					tradingDayBegin = sessionIterator.GetTradingDayBeginLocal(dateTime);
					tradingDayEnd = sessionIterator.GetTradingDayEndLocal(dateTime);
					flag = false;
				}
			}
			DateTime tradingDayEndLocal = sessionIterator.GetTradingDayEndLocal(dateTime);
			((MarketAnalyzerColumnBase)this).DaysBack = (int)((NinjaScriptBase)this).To.Subtract(tradingDayEndLocal.Date).TotalDays + ((tradingDayEndLocal.Hour == 0 && tradingDayEndLocal.Minute == 0) ? 1 : 0);
			sessionIterator.GetNextSession(tradingDayBegin.AddSeconds(1.0), false);
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (marketDataUpdate.IsReset)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = double.MinValue;
		}
		else if ((int)marketDataUpdate.MarketDataType == 2 && marketDataUpdate.Instrument.MarketData.LastClose != null)
		{
			lastClose = marketDataUpdate.Instrument.MarketData.LastClose.Price;
			((MarketAnalyzerColumnBase)this).CurrentValue = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / marketDataUpdate.Instrument.MarketData.LastClose.Price;
		}
	}

	public override void OnRender(DrawingContext dc, Size renderSize)
	{
		DateTime now = Now;
		if (now > nextTradingDayBegin)
		{
			tradingDayBegin = sessionIterator.GetTradingDayBeginLocal(sessionIterator.ActualTradingDayExchange);
			tradingDayEnd = sessionIterator.ActualTradingDayEndLocal;
			nextTradingDayBegin = Globals.MaxDate;
		}
		else if (sessionIterator.IsNewSession(now, false))
		{
			sessionIterator.GetNextSession(now, false);
			if (sessionIterator.IsInSession(now, false, true))
			{
				tradingDayBegin = sessionIterator.GetTradingDayBeginLocal(sessionIterator.ActualTradingDayExchange);
				tradingDayEnd = sessionIterator.ActualTradingDayEndLocal;
			}
			else
			{
				nextTradingDayBegin = sessionIterator.GetTradingDayBeginLocal(sessionIterator.ActualTradingDayExchange);
			}
		}
		if (downAreaBrush == null)
		{
			downAreaBrush = new SolidColorBrush
			{
				Color = (downArea as SolidColorBrush).Color,
				Opacity = (double)Opacity / 100.0
			};
		}
		if (upAreaBrush == null)
		{
			upAreaBrush = new SolidColorBrush
			{
				Color = (upArea as SolidColorBrush).Color,
				Opacity = (double)Opacity / 100.0
			};
		}
		if (downOutlinePen == null)
		{
			downOutlinePen = new Pen(downOutline, 1.0);
		}
		if (upOutlinePen == null)
		{
			upOutlinePen = new Pen(upOutline, 1.0);
		}
		int num = Math.Max(0, ((NinjaScriptBase)this).BarsArray[0].GetBar(tradingDayBegin));
		bool num2 = now >= tradingDayBegin && now < tradingDayEnd;
		List<LineSegment> list = new List<LineSegment>();
		List<LineSegment> list2 = new List<LineSegment>();
		int num3 = 1;
		double num4 = double.MinValue;
		double num5 = double.MinValue;
		double val = double.MinValue;
		int num6 = -1;
		double val2 = double.MaxValue;
		int num7 = num3;
		int num8 = (int)Math.Floor(renderSize.Height) - num3;
		int num9 = (int)Math.Floor(renderSize.Width) - 2 * num3;
		int num10 = -1;
		int num11 = -1;
		Point start = default(Point);
		int num12 = -1;
		double num13 = ((!num2 && num > 0) ? ((NinjaScriptBase)this).BarsArray[0].GetClose(num - 1) : lastClose);
		((NinjaScriptBase)this).BarsArray[0].BarsSeries.SyncRoot.EnterReadLock();
		try
		{
			val2 = Math.Min(num13, val2);
			val = Math.Max(num13, val);
			for (int i = num; i < ((NinjaScriptBase)this).BarsArray[0].Count; i++)
			{
				double close = ((NinjaScriptBase)this).BarsArray[0].GetClose(i);
				val2 = Math.Min(close, val2);
				val = Math.Max(close, val);
			}
			if (val2 == double.MaxValue || val == double.MinValue || num13 == double.MinValue)
			{
				return;
			}
			for (int j = num; j < ((NinjaScriptBase)this).BarsArray[0].Count; j++)
			{
				DateTime time = ((NinjaScriptBase)this).BarsArray[0].GetTime(j);
				if (time > tradingDayEnd)
				{
					break;
				}
				double close2 = ((NinjaScriptBase)this).BarsArray[0].GetClose(j);
				int num14 = num3 + Convert.ToInt32((double)num9 * time.Subtract(tradingDayBegin).TotalSeconds / Math.Max(1.0, tradingDayEnd.Subtract(tradingDayBegin).TotalSeconds));
				num4 = ((num14 == num10) ? Math.Max(close2, num4) : close2);
				num5 = ((num14 == num10) ? Math.Min(close2, num5) : close2);
				double num15 = ((MathExtentions.ApproxCompare(num5, val2) == 0) ? num5 : ((MathExtentions.ApproxCompare(num4, val) == 0) ? num4 : ((MathExtentions.ApproxCompare(close2, num13) > 0) ? num4 : num5)));
				int num16 = num3 + Convert.ToInt32((val - num15) / Math.Max(((NinjaScriptBase)this).BarsArray[0].Instrument.MasterInstrument.TickSize, val - val2) * (double)(num8 - num3));
				if (j == num)
				{
					num12 = num3 + Convert.ToInt32((val - num13) / Math.Max(((NinjaScriptBase)this).BarsArray[0].Instrument.MasterInstrument.TickSize, val - val2) * (double)(num8 - num3));
					start = new Point(num14, num12);
				}
				if (j == ((NinjaScriptBase)this).BarsArray[0].Count - 1)
				{
					num6 = num14;
				}
				bool flag = (num11 < num12 && num16 > num12) || (num11 > num12 && num16 < num12);
				if (num14 == num10)
				{
					list2[list2.Count - 1] = new LineSegment(new Point(num14, Math.Min(num16, num12)), flag || num16 <= num12);
					list[list2.Count - 1] = new LineSegment(new Point(num14, Math.Max(num16, num12)), flag || num16 > num12);
				}
				else
				{
					list2.Add(new LineSegment(new Point(num14, Math.Min(num16, num12)), flag || num16 <= num12));
					list.Add(new LineSegment(new Point(num14, Math.Max(num16, num12)), flag || num16 > num12));
				}
				num10 = num14;
				num11 = num16;
			}
		}
		finally
		{
			((NinjaScriptBase)this).BarsArray[0].BarsSeries.SyncRoot.ExitReadLock();
		}
		if (list2.Count > 0)
		{
			List<PathFigure> list3 = new List<PathFigure>();
			list3.Add(new PathFigure(start, list2.ToArray(), closed: false));
			dc.DrawGeometry(null, upOutlinePen, new PathGeometry(list3));
			list2.Add(new LineSegment(new Point(num6, num12), isStroked: true));
			list2.Add(new LineSegment(new Point(num7, num12), isStroked: true));
			PathGeometry geometry = new PathGeometry(new List<PathFigure>
			{
				new PathFigure(start, list2.ToArray(), closed: true)
			});
			dc.DrawGeometry(upAreaBrush, null, geometry);
			List<PathFigure> list4 = new List<PathFigure>();
			list4.Add(new PathFigure(start, list.ToArray(), closed: false));
			dc.DrawGeometry(null, downOutlinePen, new PathGeometry(list4));
			list.Add(new LineSegment(new Point(num6, num12), isStroked: true));
			list.Add(new LineSegment(new Point(num7, num12), isStroked: true));
			PathGeometry geometry2 = new PathGeometry(new List<PathFigure>
			{
				new PathFigure(start, list.ToArray(), closed: true)
			});
			dc.DrawGeometry(downAreaBrush, null, geometry2);
		}
	}

	public override string Format(double value)
	{
		if (value == double.MinValue)
		{
			return string.Empty;
		}
		return value.ToString("P", Globals.GeneralOptions.CurrentCulture);
	}
}

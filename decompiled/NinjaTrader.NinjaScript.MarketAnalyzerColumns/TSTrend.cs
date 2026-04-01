using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.NinjaScript;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

[TypeConverter("NinjaTrader.NinjaScript.MarketAnalyzerColumns.TSTrendConverter")]
public class TSTrend : MarketAnalyzerColumnRenderBase
{
	private Brush aboveAsk;

	private Brush atAsk;

	private Brush atBid;

	private Brush belowBid;

	private Brush between;

	private double lastAsk = double.MinValue;

	private double lastBid = double.MinValue;

	private int margin = 1;

	private int maxSlots = 10;

	private List<TrendValue> slots = new List<TrendValue>();

	private List<Tuple<Brush, Pen>> trendColors;

	private List<Tuple<Brush, Pen>> TrendColors
	{
		get
		{
			if (trendColors == null)
			{
				trendColors = new List<Tuple<Brush, Pen>>();
				trendColors.Add(new Tuple<Brush, Pen>(AboveAsk, new Pen(AboveAsk, 1.0)));
				trendColors.Add(new Tuple<Brush, Pen>(AtAsk, new Pen(AtAsk, 1.0)));
				trendColors.Add(new Tuple<Brush, Pen>(Between, new Pen(Between, 1.0)));
				trendColors.Add(new Tuple<Brush, Pen>(AtBid, new Pen(AtBid, 1.0)));
				trendColors.Add(new Tuple<Brush, Pen>(BelowBid, new Pen(BelowBid, 1.0)));
			}
			return trendColors;
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnTSTrendAboveAsk", GroupName = "GuiPropertyCategoryVisual", Order = 32)]
	public Brush AboveAsk
	{
		get
		{
			return aboveAsk;
		}
		set
		{
			aboveAsk = value;
			trendColors = null;
		}
	}

	[Browsable(false)]
	public string AboveAskSeralizer
	{
		get
		{
			return Serialize.BrushToString(AboveAsk);
		}
		set
		{
			AboveAsk = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnTSTrendAtAsk", GroupName = "GuiPropertyCategoryVisual", Order = 34)]
	public Brush AtAsk
	{
		get
		{
			return atAsk;
		}
		set
		{
			atAsk = value;
			trendColors = null;
		}
	}

	[Browsable(false)]
	public string AtAskSeralizer
	{
		get
		{
			return Serialize.BrushToString(AtAsk);
		}
		set
		{
			AtAsk = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnTSTrendAtBid", GroupName = "GuiPropertyCategoryVisual", Order = 36)]
	public Brush AtBid
	{
		get
		{
			return atBid;
		}
		set
		{
			atBid = value;
			trendColors = null;
		}
	}

	[Browsable(false)]
	public string AtBidSeralizer
	{
		get
		{
			return Serialize.BrushToString(AtBid);
		}
		set
		{
			AtBid = Serialize.StringToBrush(value);
		}
	}

	[Range(0, int.MaxValue)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnTSTrendBarWidth", GroupName = "GuiPropertyCategoryMiscellaneous", Order = 10)]
	public int BarWidth { get; set; }

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnTSTrendBelowBid", GroupName = "GuiPropertyCategoryVisual", Order = 38)]
	public Brush BelowBid
	{
		get
		{
			return belowBid;
		}
		set
		{
			belowBid = value;
			trendColors = null;
		}
	}

	[Browsable(false)]
	public string BelowBidSeralizer
	{
		get
		{
			return Serialize.BrushToString(BelowBid);
		}
		set
		{
			BelowBid = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnTSTrendBetween", GroupName = "GuiPropertyCategoryVisual", Order = 40)]
	public Brush Between
	{
		get
		{
			return between;
		}
		set
		{
			between = value;
			trendColors = null;
		}
	}

	[Browsable(false)]
	public string BetweenSeralizer
	{
		get
		{
			return Serialize.BrushToString(Between);
		}
		set
		{
			Between = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionTSTrend;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameTSTrend;
			AboveAsk = Brushes.Gold;
			AtAsk = Brushes.ForestGreen;
			AtBid = Brushes.Chocolate;
			BelowBid = Brushes.DeepPink;
			Between = Brushes.Sienna;
			BarWidth = 4;
			((NinjaScriptBase)this).IsDataSeriesRequired = false;
		}
	}

	public override string Format(double value)
	{
		return string.Empty;
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		if ((int)marketDataUpdate.MarketDataType == 1)
		{
			lastBid = marketDataUpdate.Price;
		}
		else if ((int)marketDataUpdate.MarketDataType == 0)
		{
			lastAsk = marketDataUpdate.Price;
		}
		else if ((int)marketDataUpdate.MarketDataType == 2)
		{
			if (lastAsk == double.MinValue || lastBid == double.MinValue)
			{
				slots.Insert(0, TrendValue.Between);
			}
			else if (MathExtentions.ApproxCompare(marketDataUpdate.Price, lastAsk) == 0)
			{
				slots.Insert(0, TrendValue.AtAsk);
			}
			else if (MathExtentions.ApproxCompare(marketDataUpdate.Price, lastAsk) == 1)
			{
				slots.Insert(0, TrendValue.AboveAsk);
			}
			else if (MathExtentions.ApproxCompare(marketDataUpdate.Price, lastBid) == 0)
			{
				slots.Insert(0, TrendValue.AtBid);
			}
			else if (MathExtentions.ApproxCompare(marketDataUpdate.Price, lastBid) == -1)
			{
				slots.Insert(0, TrendValue.BelowBid);
			}
			else
			{
				slots.Insert(0, TrendValue.Between);
			}
			if (slots.Count > maxSlots)
			{
				slots.RemoveRange(maxSlots - 1, slots.Count - maxSlots);
			}
		}
	}

	public override void OnRender(DrawingContext dc, Size renderSize)
	{
		int num = (int)Math.Floor(renderSize.Height) - margin;
		int num2 = (int)Math.Floor(renderSize.Width) - 2 * margin;
		maxSlots = (int)Math.Ceiling((double)num2 / (double)BarWidth);
		if (slots.Count > maxSlots)
		{
			slots.RemoveRange(maxSlots - 1, slots.Count - maxSlots);
		}
		for (int i = 0; i < maxSlots; i++)
		{
			if (i >= maxSlots - slots.Count)
			{
				int num3 = ((i == 0) ? Math.Max(margin, num2 + (-maxSlots + i) * BarWidth) : (num2 + (-maxSlots + i) * BarWidth));
				int num4 = num2 + (-maxSlots + i + 1) * BarWidth;
				int num5 = margin;
				int num6 = num;
				List<LineSegment> list = new List<LineSegment>();
				Point start = new Point(num3, num5);
				list.Add(new LineSegment(new Point(num3, num6), isStroked: true));
				list.Add(new LineSegment(new Point(num4, num6), isStroked: true));
				list.Add(new LineSegment(new Point(num4, num5), isStroked: true));
				list.Add(new LineSegment(new Point(num4, num5), isStroked: true));
				PathGeometry geometry = new PathGeometry(new List<PathFigure>
				{
					new PathFigure(start, list.ToArray(), closed: true)
				});
				int index = (int)slots[maxSlots - 1 - i];
				dc.DrawGeometry(TrendColors[index].Item1, TrendColors[index].Item2, geometry);
			}
		}
	}
}

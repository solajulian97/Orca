using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Risk Reward IDrawingTool.
/// </summary>
public class RiskReward : DrawingTool
{
	private const int cursorSensitivity = 15;

	private ChartAnchor editingAnchor;

	private double entryPrice;

	private bool needsRatioUpdate = true;

	private double ratio = 2.0;

	private double risk;

	private double reward;

	private double stopPrice;

	private double targetPrice;

	private double textleftPoint;

	private double textRightPoint;

	[Browsable(false)]
	private bool DrawTarget
	{
		get
		{
			ChartAnchor riskAnchor = RiskAnchor;
			if (riskAnchor == null || riskAnchor.IsEditing)
			{
				riskAnchor = RewardAnchor;
				if (riskAnchor != null)
				{
					return !riskAnchor.IsEditing;
				}
				return false;
			}
			return true;
		}
	}

	[Display(Order = 1)]
	public ChartAnchor EntryAnchor { get; set; }

	[Display(Order = 2)]
	public ChartAnchor RiskAnchor { get; set; }

	[Browsable(false)]
	public ChartAnchor RewardAnchor { get; set; }

	public override object Icon => Icons.DrawRiskReward;

	[Range(0.0, double.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRiskRewardRatio", GroupName = "NinjaScriptGeneral", Order = 1)]
	public double Ratio
	{
		get
		{
			return ratio;
		}
		set
		{
			if (MathExtentions.ApproxCompare(ratio, value) != 0)
			{
				ratio = value;
				needsRatioUpdate = true;
			}
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAnchor", GroupName = "NinjaScriptLines", Order = 3)]
	public Stroke AnchorLineStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeEntry", GroupName = "NinjaScriptLines", Order = 6)]
	public Stroke EntryLineStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeRisk", GroupName = "NinjaScriptLines", Order = 4)]
	public Stroke StopLineStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRiskRewardLineStrokeReward", GroupName = "NinjaScriptLines", Order = 5)]
	public Stroke TargetLineStroke { get; set; }

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[3] { EntryAnchor, RiskAnchor, RewardAnchor };

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesRight", GroupName = "NinjaScriptLines", Order = 2)]
	public bool IsExtendedLinesRight { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesLeft", GroupName = "NinjaScriptLines", Order = 1)]
	public bool IsExtendedLinesLeft { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextAlignment", GroupName = "NinjaScriptGeneral", Order = 2)]
	public TextLocation TextAlignment { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRulerYValueDisplayUnit", GroupName = "NinjaScriptGeneral", Order = 3)]
	public ValueUnit DisplayUnit { get; set; }

	public override bool SupportsAlerts => true;

	private void DrawPriceText(ChartAnchor anchor, Point point, double price, ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Expected I4, but got Unknown
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Expected I4, but got Unknown
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Expected I4, but got Unknown
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Unknown result type (might be due to invalid IL or missing references)
		//IL_0436: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Expected I4, but got Unknown
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Expected I4, but got Unknown
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e0: Expected I4, but got Unknown
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_0487: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Expected I4, but got Unknown
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0553: Unknown result type (might be due to invalid IL or missing references)
		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
		//IL_055a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0571: Expected I4, but got Unknown
		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0518: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0585: Unknown result type (might be due to invalid IL or missing references)
		//IL_059f: Unknown result type (might be due to invalid IL or missing references)
		if ((int)TextAlignment == 4)
		{
			return;
		}
		ChartBars attachedToChartBars = ((DrawingTool)this).GetAttachedToChartBars();
		if (attachedToChartBars == null)
		{
			return;
		}
		if (!((DrawingTool)this).IsUserDrawn)
		{
			price = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(anchor.Price);
		}
		string priceString = GetPriceString(price, attachedToChartBars);
		textleftPoint = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale, true).X;
		textRightPoint = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale, true).X;
		Stroke val = ((anchor == RewardAnchor) ? TargetLineStroke : ((anchor == RiskAnchor) ? StopLineStroke : ((anchor != EntryAnchor) ? AnchorLineStroke : EntryLineStroke)));
		TextFormat val2 = ((SimpleFont)(((object)chartControl.Properties.LabelFont) ?? ((object)new SimpleFont()))).ToDirectWriteTextFormat();
		val2.TextAlignment = (TextAlignment)0;
		val2.WordWrapping = (WordWrapping)1;
		TextLayout val3 = new TextLayout(Globals.DirectWriteFactory, priceString, val2, (float)chartPanel.H, val2.FontSize);
		if (RiskAnchor.Time <= EntryAnchor.Time)
		{
			if (!IsExtendedLinesLeft && !IsExtendedLinesRight)
			{
				TextLocation textAlignment = TextAlignment;
				point.X = (int)textAlignment switch
				{
					0 => textleftPoint, 
					2 => textRightPoint - (double)val3.Metrics.Width, 
					1 => textleftPoint, 
					3 => textRightPoint - (double)val3.Metrics.Width, 
					_ => point.X, 
				};
			}
			else if (IsExtendedLinesLeft && !IsExtendedLinesRight)
			{
				TextLocation textAlignment = TextAlignment;
				point.X = (int)textAlignment switch
				{
					0 => textleftPoint, 
					2 => textRightPoint - (double)val3.Metrics.Width, 
					1 => chartPanel.X, 
					3 => textRightPoint - (double)val3.Metrics.Width, 
					_ => point.X, 
				};
			}
			else if (!IsExtendedLinesLeft && IsExtendedLinesRight)
			{
				TextLocation textAlignment = TextAlignment;
				point.X = (int)textAlignment switch
				{
					0 => textleftPoint, 
					2 => textRightPoint - (double)val3.Metrics.Width, 
					1 => textleftPoint, 
					3 => (float)chartPanel.W - val3.Metrics.Width, 
					_ => point.X, 
				};
			}
			else if (IsExtendedLinesLeft && IsExtendedLinesRight)
			{
				TextLocation textAlignment = TextAlignment;
				point.X = (int)textAlignment switch
				{
					0 => textleftPoint, 
					2 => textRightPoint - (double)val3.Metrics.Width, 
					3 => (float)chartPanel.W - val3.Metrics.Width, 
					1 => chartPanel.X, 
					_ => point.X, 
				};
			}
		}
		else if (RiskAnchor.Time >= EntryAnchor.Time)
		{
			if (!IsExtendedLinesLeft && !IsExtendedLinesRight)
			{
				TextLocation textAlignment = TextAlignment;
				point.X = (int)textAlignment switch
				{
					0 => textRightPoint, 
					2 => textleftPoint - (double)val3.Metrics.Width, 
					1 => textRightPoint, 
					3 => textleftPoint - (double)val3.Metrics.Width, 
					_ => point.X, 
				};
			}
			else if (IsExtendedLinesLeft && !IsExtendedLinesRight)
			{
				TextLocation textAlignment = TextAlignment;
				point.X = (int)textAlignment switch
				{
					0 => textRightPoint, 
					2 => textleftPoint - (double)val3.Metrics.Width, 
					1 => chartPanel.X, 
					3 => textleftPoint - (double)val3.Metrics.Width, 
					_ => point.X, 
				};
			}
			else if (!IsExtendedLinesLeft && IsExtendedLinesRight)
			{
				TextLocation textAlignment = TextAlignment;
				point.X = (int)textAlignment switch
				{
					0 => textRightPoint, 
					2 => textleftPoint - (double)val3.Metrics.Width, 
					1 => textRightPoint, 
					3 => (float)chartPanel.W - val3.Metrics.Width, 
					_ => point.X, 
				};
			}
			else if (IsExtendedLinesLeft && IsExtendedLinesRight)
			{
				TextLocation textAlignment = TextAlignment;
				point.X = (int)textAlignment switch
				{
					0 => textRightPoint, 
					2 => textleftPoint - (double)val3.Metrics.Width, 
					3 => (float)chartPanel.W - val3.Metrics.Width, 
					1 => chartPanel.X, 
					_ => point.X, 
				};
			}
		}
		((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2((float)point.X, (float)point.Y), val3, val.BrushDX, (DrawTextOptions)1);
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		return ((DrawingTool)this).Anchors.Select((Func<ChartAnchor, AlertConditionItem>)((ChartAnchor anchor) => new AlertConditionItem
		{
			Name = anchor.DisplayName,
			ShouldOnlyDisplayName = true,
			Tag = anchor
		}));
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		switch ((int)drawingState)
		{
		case 0:
			return Cursors.Pen;
		case 3:
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.No;
		case 1:
			if (!((DrawingTool)this).IsLocked)
			{
				if (editingAnchor != EntryAnchor)
				{
					return Cursors.SizeNWSE;
				}
				return Cursors.SizeNESW;
			}
			return Cursors.No;
		default:
		{
			Point point2 = EntryAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
			if (closestAnchor != null)
			{
				if (!((DrawingTool)this).IsLocked)
				{
					if (closestAnchor != EntryAnchor)
					{
						return Cursors.SizeNWSE;
					}
					return Cursors.SizeNESW;
				}
				return Cursors.Arrow;
			}
			Vector vector = RiskAnchor.GetPoint(chartControl, chartPanel, chartScale, true) - point2;
			if (MathHelper.IsPointAlongVector(point, point2, vector, 15.0))
			{
				if (!((DrawingTool)this).IsLocked)
				{
					return Cursors.SizeAll;
				}
				return Cursors.Arrow;
			}
			if (!DrawTarget)
			{
				return null;
			}
			Vector vector2 = RewardAnchor.GetPoint(chartControl, chartPanel, chartScale, true) - point2;
			if (!MathHelper.IsPointAlongVector(point, point2, vector2, 15.0))
			{
				return null;
			}
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.Arrow;
		}
		}
	}

	private string GetPriceString(double price, ChartBars chartBars)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected I4, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Invalid comparison between Unknown and I4
		double num = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
		double tickSize = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize;
		double pointValue = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.PointValue;
		ValueUnit displayUnit = DisplayUnit;
		switch (displayUnit - 1)
		{
		case 2:
			if ((int)((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.InstrumentType == 4)
			{
				return (price > num) ? Globals.FormatCurrency(((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(price - num) / tickSize * (tickSize * pointValue * (double)Account.All[0].ForexLotSize)) : Globals.FormatCurrency(((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(num - price) / tickSize * (tickSize * pointValue * (double)Account.All[0].ForexLotSize));
			}
			return (price > num) ? Globals.FormatCurrency(((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(price - num) / tickSize * (tickSize * pointValue)) : Globals.FormatCurrency(((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(num - price) / tickSize * (tickSize * pointValue));
		case 0:
			return (price > num) ? (((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(price - num) / num).ToString("P", Globals.GeneralOptions.CurrentCulture) : (((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(num - price) / num).ToString("P", Globals.GeneralOptions.CurrentCulture);
		case 1:
			return (price > num) ? (((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(price - num) / tickSize).ToString("F0") : (((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(num - price) / tickSize).ToString("F0");
		case 3:
			return (price > num) ? (((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(price - num) / tickSize / 10.0).ToString("F0") : (((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(num - price) / tickSize / 10.0).ToString("F0");
		default:
			return chartBars.Bars.Instrument.MasterInstrument.FormatPrice(price, true);
		}
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = EntryAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = RiskAnchor.GetPoint(chartControl, val, chartScale, true);
		if (DrawTarget)
		{
			Point point3 = RewardAnchor.GetPoint(chartControl, val, chartScale, true);
			return new Point[3] { point, point2, point3 };
		}
		return new Point[2] { point, point2 };
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Expected I4, but got Unknown
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Invalid comparison between Unknown and I4
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Invalid comparison between Unknown and I4
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Invalid comparison between Unknown and I4
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Invalid comparison between Unknown and I4
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Invalid comparison between Unknown and I4
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Invalid comparison between Unknown and I4
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Invalid comparison between Unknown and I4
		object tag = conditionItem.Tag;
		ChartAnchor val = (ChartAnchor)((tag is ChartAnchor) ? tag : null);
		if (val == null)
		{
			return false;
		}
		ChartPanel val2 = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		double y = chartScale.GetYByValue(val.Price);
		Point point = EntryAnchor.GetPoint(chartControl, val2, chartScale, true);
		Point point2 = RiskAnchor.GetPoint(chartControl, val2, chartScale, true);
		Point point3 = RewardAnchor.GetPoint(chartControl, val2, chartScale, true);
		double num = (DrawTarget ? new double[3] { point.X, point2.X, point3.X }.Min() : new double[2] { point.X, point2.X }.Min());
		double num2 = (DrawTarget ? new double[3] { point.X, point2.X, point3.X }.Max() : new double[2] { point.X, point2.X }.Max());
		double x = (IsExtendedLinesLeft ? ((double)val2.X) : num);
		double num3 = (IsExtendedLinesRight ? ((double)(val2.X + val2.W)) : num2);
		double num4 = chartControl.GetXByTime(values[0].Time);
		double y2 = chartScale.GetYByValue(values[0].Value);
		if (num3 < num4)
		{
			return false;
		}
		Point lineStartPoint = new Point(x, y);
		Point lineEndPoint = new Point(num3, y);
		Point point4 = new Point(num4, y2);
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(lineStartPoint, lineEndPoint, point4);
		Condition val3 = condition;
		switch ((int)val3)
		{
		case 3:
			return (int)pointLineLocation == 0;
		case 4:
			if ((int)pointLineLocation != 0)
			{
				return (int)pointLineLocation == 2;
			}
			return true;
		case 5:
			return (int)pointLineLocation == 1;
		case 6:
			if ((int)pointLineLocation != 1)
			{
				return (int)pointLineLocation == 2;
			}
			return true;
		case 2:
			return (int)pointLineLocation == 2;
		case 7:
			return (int)pointLineLocation != 2;
		case 0:
		case 1:
			return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate);
		default:
			return false;
		}
		bool Predicate(ChartAlertValue v)
		{
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Invalid comparison between Unknown and I4
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Invalid comparison between Unknown and I4
			double x2 = chartControl.GetXByTime(v.Time);
			double y3 = chartScale.GetYByValue(v.Value);
			Point point5 = new Point(x2, y3);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(lineStartPoint, lineEndPoint, point5);
			if ((int)condition == 0)
			{
				return (int)pointLineLocation2 == 0;
			}
			return (int)pointLineLocation2 == 1;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			return ((DrawingTool)this).Anchors.Any((ChartAnchor a) => a.Time >= firstTimeOnChart && a.Time <= lastTimeOnChart);
		}
		return true;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (!((NinjaScript)this).IsVisible || !((DrawingTool)this).Anchors.Any((ChartAnchor a) => !a.IsEditing))
		{
			return;
		}
		foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
		{
			if (!(anchor.DisplayName == RewardAnchor.DisplayName) || DrawTarget)
			{
				((ChartObject)this).MinValue = Math.Min(anchor.Price, ((ChartObject)this).MinValue);
				((ChartObject)this).MaxValue = Math.Max(anchor.Price, ((ChartObject)this).MaxValue);
			}
		}
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState == 2)
			{
				Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
				editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
				if (editingAnchor != null)
				{
					editingAnchor.IsEditing = true;
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == null)
				{
					((ChartObject)this).IsSelected = false;
				}
				else
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
			}
			return;
		}
		if (EntryAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(EntryAnchor);
			dataPoint.CopyDataValues(RiskAnchor);
			EntryAnchor.IsEditing = false;
			entryPrice = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
		}
		else if (RiskAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(RiskAnchor);
			RiskAnchor.IsEditing = false;
			stopPrice = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RiskAnchor.Price);
			SetReward();
			RewardAnchor.Time = EntryAnchor.Time;
			RewardAnchor.SlotIndex = EntryAnchor.SlotIndex;
			RewardAnchor.IsEditing = false;
		}
		if (!EntryAnchor.IsEditing && !RiskAnchor.IsEditing && !RewardAnchor.IsEditing)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Invalid comparison between Unknown and I4
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Invalid comparison between Unknown and I4
		if ((((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0) || !((NinjaScript)this).IsVisible)
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (EntryAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EntryAnchor);
			}
			else if (RiskAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(RiskAnchor);
			}
			else if (RewardAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(RewardAnchor);
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 1 && editingAnchor != null)
		{
			dataPoint.CopyDataValues(editingAnchor);
			if (editingAnchor != EntryAnchor)
			{
				if (editingAnchor != RewardAnchor && MathExtentions.ApproxCompare(Ratio, 0.0) != 0)
				{
					SetReward();
				}
				else if (MathExtentions.ApproxCompare(Ratio, 0.0) != 0)
				{
					SetRisk();
				}
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 3)
		{
			foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
			{
				anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
		}
		entryPrice = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
		stopPrice = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RiskAnchor.Price);
		targetPrice = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RewardAnchor.Price);
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 1 || (int)((DrawingTool)this).DrawingState == 3)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
		}
		if (editingAnchor != null)
		{
			if (editingAnchor == EntryAnchor)
			{
				SetReward();
				if (MathExtentions.ApproxCompare(Ratio, 0.0) != 0)
				{
					SetRisk();
				}
			}
			editingAnchor.IsEditing = false;
		}
		editingAnchor = null;
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		if (((NinjaScript)this).IsVisible && !((DrawingTool)this).Anchors.All((ChartAnchor a) => a.IsEditing))
		{
			if (needsRatioUpdate && DrawTarget)
			{
				SetReward();
			}
			ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
			Point point = EntryAnchor.GetPoint(chartControl, val, chartScale, true);
			Point point2 = RiskAnchor.GetPoint(chartControl, val, chartScale, true);
			Point point3 = RewardAnchor.GetPoint(chartControl, val, chartScale, true);
			AnchorLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
			EntryLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
			StopLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
			((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point), DxExtensions.ToVector2(point2), AnchorLineStroke.BrushDX, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
			double num = (DrawTarget ? new double[3] { point.X, point2.X, point3.X }.Min() : new double[2] { point.X, point2.X }.Min());
			double num2 = (DrawTarget ? new double[3] { point.X, point2.X, point3.X }.Max() : new double[2] { point.X, point2.X }.Max());
			double num3 = (IsExtendedLinesLeft ? ((double)val.X) : num);
			double num4 = (IsExtendedLinesRight ? ((double)(val.X + val.W)) : num2);
			Vector2 val2 = default(Vector2);
			((Vector2)(ref val2))._002Ector((float)num3, (float)point.Y);
			Vector2 val3 = default(Vector2);
			((Vector2)(ref val3))._002Ector((float)num4, (float)point.Y);
			Vector2 val4 = default(Vector2);
			((Vector2)(ref val4))._002Ector((float)num3, (float)point2.Y);
			Vector2 val5 = default(Vector2);
			((Vector2)(ref val5))._002Ector((float)num4, (float)point2.Y);
			Brush val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : AnchorLineStroke.BrushDX);
			if (DrawTarget)
			{
				AnchorLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
				((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point), DxExtensions.ToVector2(point3), val6, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
				TargetLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
				Vector2 val7 = default(Vector2);
				((Vector2)(ref val7))._002Ector((float)num3, (float)point3.Y);
				Vector2 val8 = default(Vector2);
				((Vector2)(ref val8))._002Ector((float)num4, (float)point3.Y);
				val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : TargetLineStroke.BrushDX);
				((ChartObject)this).RenderTarget.DrawLine(val7, val8, val6, TargetLineStroke.Width, TargetLineStroke.StrokeStyle);
				DrawPriceText(RewardAnchor, point3, targetPrice, chartControl, val, chartScale);
			}
			val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : EntryLineStroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawLine(val2, val3, val6, EntryLineStroke.Width, EntryLineStroke.StrokeStyle);
			DrawPriceText(EntryAnchor, point, entryPrice, chartControl, val, chartScale);
			val6 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : StopLineStroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawLine(val4, val5, val6, StopLineStroke.Width, StopLineStroke.StrokeStyle);
			DrawPriceText(RiskAnchor, point2, stopPrice, chartControl, val, chartScale);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected O, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptDrawingToolRiskRewardDescription;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolRiskRewardName;
			Ratio = 2.0;
			AnchorLineStroke = new Stroke((Brush)Brushes.DarkGray, (DashStyleHelper)0, 1f, 50);
			EntryLineStroke = new Stroke((Brush)Brushes.Goldenrod, (DashStyleHelper)0, 2f);
			StopLineStroke = new Stroke((Brush)Brushes.Crimson, (DashStyleHelper)0, 2f);
			TargetLineStroke = new Stroke((Brush)Brushes.SeaGreen, (DashStyleHelper)0, 2f);
			EntryAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			RiskAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			RewardAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			EntryAnchor.DisplayName = Resource.NinjaScriptDrawingToolRiskRewardAnchorEntry;
			RiskAnchor.DisplayName = Resource.NinjaScriptDrawingToolRiskRewardAnchorRisk;
			RewardAnchor.DisplayName = Resource.NinjaScriptDrawingToolRiskRewardAnchorReward;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void SetReward()
	{
		if (((DrawingTool)this).Anchors != null && ((DrawingTool)this).AttachedTo != null)
		{
			entryPrice = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
			stopPrice = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RiskAnchor.Price);
			risk = entryPrice - stopPrice;
			reward = risk * Ratio;
			targetPrice = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(entryPrice + reward);
			RewardAnchor.Price = targetPrice;
			RewardAnchor.IsEditing = false;
			needsRatioUpdate = false;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void SetRisk()
	{
		if (((DrawingTool)this).Anchors != null && ((DrawingTool)this).AttachedTo != null)
		{
			entryPrice = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EntryAnchor.Price);
			targetPrice = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(RewardAnchor.Price);
			reward = targetPrice - entryPrice;
			risk = reward / Ratio;
			stopPrice = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(entryPrice - risk);
			RiskAnchor.Price = stopPrice;
			RiskAnchor.IsEditing = false;
			needsRatioUpdate = false;
		}
	}
}

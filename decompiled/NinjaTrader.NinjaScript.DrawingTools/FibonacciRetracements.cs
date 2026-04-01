using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
/// Represents an interface that exposes information regarding a Fibonacci Retracements IDrawingTool.
/// </summary>
public class FibonacciRetracements : FibonacciLevels
{
	public override object Icon => Icons.DrawFbRetracement;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesRight", GroupName = "NinjaScriptLines")]
	public bool IsExtendedLinesRight { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesLeft", GroupName = "NinjaScriptLines")]
	public bool IsExtendedLinesLeft { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsTextLocation", GroupName = "NinjaScriptGeneral")]
	public TextLocation TextLocation { get; set; }

	protected bool CheckAlertRetracementLine(Condition condition, Point lineStartPoint, Point lineEndPoint, ChartControl chartControl, ChartScale chartScale, ChartAlertValue[] values)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Invalid comparison between Unknown and I4
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Expected I4, but got Unknown
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Invalid comparison between Unknown and I4
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Invalid comparison between Unknown and I4
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Invalid comparison between Unknown and I4
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Invalid comparison between Unknown and I4
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Invalid comparison between Unknown and I4
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Invalid comparison between Unknown and I4
		if (((DrawingTool)this).Anchors.Count((ChartAnchor a) => a.IsEditing) > 1)
		{
			return false;
		}
		if ((int)values[0].ValueType == 11)
		{
			int xByTime = chartControl.GetXByTime(values[0].Time);
			if (!(lineStartPoint.X >= (double)xByTime))
			{
				return lineEndPoint.X >= (double)xByTime;
			}
			return true;
		}
		double num = chartControl.GetXByTime(values[0].Time);
		double y = chartScale.GetYByValue(values[0].Value);
		Point point = new Point(num, y);
		if (lineEndPoint.X < num)
		{
			return false;
		}
		if (lineStartPoint.X > num)
		{
			return false;
		}
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(lineStartPoint, lineEndPoint, point);
		Condition val = condition;
		switch ((int)val)
		{
		case 3:
			return (int)pointLineLocation == 0;
		case 4:
			if ((int)pointLineLocation == 0 || (int)pointLineLocation == 2)
			{
				return true;
			}
			return false;
		case 5:
			return (int)pointLineLocation == 1;
		case 6:
			if (pointLineLocation - 1 <= 1)
			{
				return true;
			}
			return false;
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
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Invalid comparison between Unknown and I4
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Invalid comparison between Unknown and I4
			if (v.Time == Globals.MinDate)
			{
				return false;
			}
			double x = chartControl.GetXByTime(v.Time);
			double y2 = chartScale.GetYByValue(v.Value);
			Point point2 = new Point(x, y2);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(lineStartPoint, lineEndPoint, point2);
			if ((int)condition == 0)
			{
				return (int)pointLineLocation2 == 0;
			}
			return (int)pointLineLocation2 == 1;
		}
	}

	protected void DrawPriceLevelText(ChartPanel chartPanel, ChartScale _, double minX, double maxX, double y, double price, PriceLevel priceLevel)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Invalid comparison between Unknown and I4
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Invalid comparison between Unknown and I4
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Invalid comparison between Unknown and I4
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		if ((int)TextLocation == 4)
		{
			return;
		}
		object obj;
		if (priceLevel == null)
		{
			obj = null;
		}
		else
		{
			Stroke stroke = priceLevel.Stroke;
			obj = ((stroke != null) ? stroke.BrushDX : null);
		}
		if (obj != null)
		{
			TextFormat val = ((SimpleFont)(((object)chartPanel.ChartControl.Properties.LabelFont) ?? ((object)new SimpleFont()))).ToDirectWriteTextFormat();
			val.TextAlignment = (TextAlignment)0;
			val.WordWrapping = (WordWrapping)1;
			string priceString = GetPriceString(price, priceLevel);
			float num = (float)Math.Abs(maxX - minX);
			TextLayout val2 = new TextLayout(Globals.DirectWriteFactory, priceString, val, num, val.FontSize);
			double num2;
			if (IsExtendedLinesLeft && (int)TextLocation == 1)
			{
				num2 = (double)chartPanel.X + 2.0;
			}
			else if (IsExtendedLinesRight && (int)TextLocation == 3)
			{
				num2 = (float)(chartPanel.X + chartPanel.W) - val2.Metrics.Width;
			}
			else
			{
				TextLocation textLocation = TextLocation;
				bool flag = (int)textLocation <= 1;
				num2 = ((!flag) ? (maxX - 1.0 - (double)val2.Metrics.Width) : (minX - 1.0));
			}
			((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2((float)num2, (float)(y - (double)val.FontSize - 2.0)), val2, priceLevel.Stroke.BrushDX, (DrawTextOptions)1);
			((DisposeBase)val).Dispose();
			((DisposeBase)val2).Dispose();
		}
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected I4, but got Unknown
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
			if (((DrawingTool)this).IsLocked)
			{
				return Cursors.No;
			}
			if (editingAnchor != base.StartAnchor)
			{
				return Cursors.SizeNWSE;
			}
			return Cursors.SizeNESW;
		default:
		{
			Point point2 = base.StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
			if (closestAnchor != null)
			{
				if (((DrawingTool)this).IsLocked)
				{
					return null;
				}
				if (closestAnchor != base.StartAnchor)
				{
					return Cursors.SizeNWSE;
				}
				return Cursors.SizeNESW;
			}
			Vector vector = base.EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true) - point2;
			if (!MathHelper.IsPointAlongVector(point, point2, vector, 15.0))
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

	protected Tuple<Point, Point> GetPriceLevelLinePoints(PriceLevel priceLevel, ChartControl chartControl, ChartScale chartScale, bool isInverted)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double totalPriceRange = base.EndAnchor.Price - base.StartAnchor.Price;
		double num = Math.Min(point.X, point2.X);
		double num2 = Math.Max(point.X, point2.X);
		double x = (IsExtendedLinesLeft ? ((double)val.X) : num);
		double x2 = (IsExtendedLinesRight ? ((double)(val.X + val.W)) : num2);
		double y = priceLevel.GetY(chartScale, base.StartAnchor.Price, totalPriceRange, isInverted);
		return new Tuple<Point, Point>(new Point(x, y), new Point(x2, y));
	}

	private string GetPriceString(double price, PriceLevel priceLevel)
	{
		string text = price.ToString(Globals.GetTickFormatString(((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize));
		return (priceLevel.Value / 100.0).ToString("P", Globals.GeneralOptions.CurrentCulture) + " (" + text + ")";
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = new Point((point.X + point2.X) / 2.0, (point.Y + point2.Y) / 2.0);
		return new Point[3] { point, point3, point2 };
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (!(conditionItem.Tag is PriceLevel priceLevel))
		{
			return false;
		}
		Tuple<Point, Point> priceLevelLinePoints = GetPriceLevelLinePoints(priceLevel, chartControl, chartScale, isInverted: true);
		return CheckAlertRetracementLine(condition, priceLevelLinePoints.Item1, priceLevelLinePoints.Item2, chartControl, chartScale, values);
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		DateTime dateTime = Globals.MaxDate;
		DateTime dateTime2 = Globals.MinDate;
		foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
		{
			if (anchor.Time < dateTime)
			{
				dateTime = anchor.Time;
			}
			if (anchor.Time > dateTime2)
			{
				dateTime2 = anchor.Time;
			}
		}
		if (!IsExtendedLinesLeft && !IsExtendedLinesRight)
		{
			if (!new DateTime[2] { dateTime, dateTime2 }.Any((DateTime t) => t >= firstTimeOnChart && t <= lastTimeOnChart))
			{
				if (dateTime < firstTimeOnChart)
				{
					return dateTime2 > lastTimeOnChart;
				}
				return false;
			}
			return true;
		}
		return true;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (!((NinjaScript)this).IsVisible || ((DrawingTool)this).Anchors.All((ChartAnchor a) => a.IsEditing))
		{
			return;
		}
		double num = base.EndAnchor.Price - base.StartAnchor.Price;
		double price = base.StartAnchor.Price;
		foreach (PriceLevel item in base.PriceLevels.Where((PriceLevel pl) => pl.IsVisible && pl.Stroke != null))
		{
			double val = price + (1.0 - item.Value / 100.0) * num;
			((ChartObject)this).MinValue = Math.Min(((ChartObject)this).MinValue, val);
			((ChartObject)this).MaxValue = Math.Max(((ChartObject)this).MaxValue, val);
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
				else if ((editingAnchor == null || ((DrawingTool)this).IsLocked) && ((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) != null)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
			}
			return;
		}
		if (base.StartAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(base.StartAnchor);
			dataPoint.CopyDataValues(base.EndAnchor);
			base.StartAnchor.IsEditing = false;
		}
		else if (base.EndAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(base.EndAnchor);
			base.EndAnchor.IsEditing = false;
		}
		if (!base.StartAnchor.IsEditing && !base.EndAnchor.IsEditing)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Invalid comparison between Unknown and I4
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Invalid comparison between Unknown and I4
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (base.EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(base.EndAnchor);
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 1 && editingAnchor != null)
		{
			dataPoint.CopyDataValues(editingAnchor);
		}
		else
		{
			if ((int)((DrawingTool)this).DrawingState != 3)
			{
				return;
			}
			foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
			{
				anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if (((int)drawingState == 1 || (int)drawingState == 3) ? true : false)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
		}
		if (editingAnchor != null)
		{
			editingAnchor.IsEditing = false;
		}
		editingAnchor = null;
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Expected O, but got Unknown
		if (((DrawingTool)this).Anchors.All((ChartAnchor a) => a.IsEditing))
		{
			return;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		base.AnchorLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		Brush val2 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : base.AnchorLineStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point), DxExtensions.ToVector2(point2), val2, base.AnchorLineStroke.Width, base.AnchorLineStroke.StrokeStyle);
		if (base.PriceLevels == null || !base.PriceLevels.Any())
		{
			return;
		}
		SetAllPriceLevelsRenderTarget();
		Point point3 = new Point(0.0, 0.0);
		Stroke val3 = null;
		RectangleF val4 = default(RectangleF);
		foreach (PriceLevel item in from p in base.PriceLevels
			where p.IsVisible && p.Stroke != null
			orderby p.Value
			select p)
		{
			Tuple<Point, Point> priceLevelLinePoints = GetPriceLevelLinePoints(item, chartControl, chartScale, isInverted: true);
			double num = ((MathExtentions.ApproxCompare((double)item.Stroke.Width % 2.0, 0.0) == 0) ? 0.5 : 0.0);
			Vector vector = new Vector(num, num);
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(priceLevelLinePoints.Item1 + vector), DxExtensions.ToVector2(priceLevelLinePoints.Item2 + vector), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			if (!((ChartObject)this).IsInHitTest)
			{
				if (val3 == null)
				{
					val3 = new Stroke();
				}
				else
				{
					((RectangleF)(ref val4))._002Ector((float)point3.X, (float)point3.Y, (float)(priceLevelLinePoints.Item2.X + num - point3.X), (float)(priceLevelLinePoints.Item2.Y - point3.Y));
					((ChartObject)this).RenderTarget.FillRectangle(val4, val3.BrushDX);
				}
				item.Stroke.CopyTo(val3);
				val3.Opacity = base.PriceLevelOpacity;
				point3 = priceLevelLinePoints.Item1 + vector;
			}
		}
		if (((ChartObject)this).IsInHitTest)
		{
			return;
		}
		foreach (PriceLevel item2 in base.PriceLevels.Where((PriceLevel pl) => pl.IsVisible && pl.Stroke != null))
		{
			Tuple<Point, Point> priceLevelLinePoints2 = GetPriceLevelLinePoints(item2, chartControl, chartScale, isInverted: true);
			float num2 = ((MathExtentions.ApproxCompare((double)item2.Stroke.Width % 2.0, 0.0) == 0) ? 0.5f : 0f);
			double minX = Math.Min(point.X, point2.X);
			double maxX = Math.Max(point.X, point2.X) + (double)num2;
			double totalPriceRange = base.EndAnchor.Price - base.StartAnchor.Price;
			double price = item2.GetPrice(base.StartAnchor.Price, totalPriceRange, isInverted: true);
			DrawPriceLevelText(val, chartScale, minX, maxX, priceLevelLinePoints2.Item1.Y, price, item2);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			base.AnchorLineStroke = new Stroke((Brush)Brushes.DarkGray, (DashStyleHelper)0, 1f, 50);
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolFibonacciRetracements;
			base.PriceLevelOpacity = 5;
			base.StartAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			base.EndAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			base.StartAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorStart;
			base.EndAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			if (base.PriceLevels.Count == 0)
			{
				base.PriceLevels.Add(new PriceLevel(0.0, Brushes.DarkGray));
				base.PriceLevels.Add(new PriceLevel(23.6, Brushes.DodgerBlue));
				base.PriceLevels.Add(new PriceLevel(38.2, Brushes.CornflowerBlue));
				base.PriceLevels.Add(new PriceLevel(50.0, Brushes.SteelBlue));
				base.PriceLevels.Add(new PriceLevel(61.8, Brushes.DarkCyan));
				base.PriceLevels.Add(new PriceLevel(76.4, Brushes.SeaGreen));
				base.PriceLevels.Add(new PriceLevel(100.0, Brushes.DarkGray));
			}
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}
}

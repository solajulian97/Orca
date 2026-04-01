using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an object that exposes information regarding an Andrews Pitchfork IDrawingTool.
/// </summary>
public class AndrewsPitchfork : PriceLevelContainer
{
	[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
	public enum AndrewsPitchforkCalculationMethod
	{
		StandardPitchfork,
		Schiff,
		ModifiedSchiff
	}

	private const int cursorSensitivity = 15;

	private ChartAnchor editingAnchor;

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[3] { StartAnchor, ExtensionAnchor, EndAnchor };

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAnchor", GroupName = "NinjaScriptLines", Order = 1)]
	public Stroke AnchorLineStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAndrewsPitchforkCalculationMethod", GroupName = "NinjaScriptGeneral", Order = 4)]
	public AndrewsPitchforkCalculationMethod CalculationMethod { get; set; }

	[Display(Order = 3)]
	public ChartAnchor ExtensionAnchor { get; set; }

	[Display(Order = 2)]
	public ChartAnchor EndAnchor { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAndrewsPitchforkRetracement", GroupName = "NinjaScriptLines", Order = 2)]
	public Stroke RetracementLineStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAndrewsPitchforkExtendLinesBack", GroupName = "NinjaScriptLines")]
	public bool IsExtendedLinesBack { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciTimeExtensionsShowText", GroupName = "NinjaScriptGeneral")]
	public bool IsTextDisplayed { get; set; }

	public override object Icon => Icons.DrawAndrewsPitchfork;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolPriceLevelsOpacity", GroupName = "NinjaScriptGeneral")]
	public int PriceLevelOpacity { get; set; }

	[Display(Order = 1)]
	public ChartAnchor StartAnchor { get; set; }

	public override bool SupportsAlerts => true;

	protected void DrawPriceLevelText(double minX, double maxX, Point endPoint, PriceLevel priceLevel, ChartPanel panel)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		TextFormat val = ((SimpleFont)(((object)panel.ChartControl.Properties.LabelFont) ?? ((object)new SimpleFont()))).ToDirectWriteTextFormat();
		string text = $"{priceLevel.Value / 100.0:P}";
		TextLayout val2 = new TextLayout(Globals.DirectWriteFactory, text, val, (float)panel.H, val.FontSize);
		float height = val2.Metrics.Height;
		float width = val2.Metrics.Width;
		Point point = endPoint;
		double num = panel.X + panel.W;
		double num2 = panel.Y + panel.H;
		double num3 = panel.X;
		double num4 = panel.Y;
		if (point.Y + (double)height >= num2)
		{
			point.Y = num2 - (double)height;
		}
		if (point.Y < num4)
		{
			point.Y = num4;
		}
		if (point.X + (double)width >= num)
		{
			point.X = num - (double)width;
		}
		if (point.X < num3)
		{
			point.X = num3;
		}
		((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2((float)point.X, (float)point.Y), val2, priceLevel.Stroke.BrushDX, (DrawTextOptions)1);
		((DisposeBase)val).Dispose();
		((DisposeBase)val2).Dispose();
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		if (base.PriceLevels == null || base.PriceLevels.Count == 0)
		{
			yield break;
		}
		foreach (PriceLevel priceLevel in base.PriceLevels)
		{
			yield return new AlertConditionItem
			{
				Name = priceLevel.Name,
				ShouldOnlyDisplayName = true,
				Tag = priceLevel
			};
		}
	}

	private IEnumerable<Tuple<Point, Point>> GetAndrewsEndPoints(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		double totalPriceRange = EndAnchor.Price - ExtensionAnchor.Price;
		double startPrice = ExtensionAnchor.Price;
		Point anchorExtensionPoint = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		Point anchorStartPoint = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point anchorEndPoint = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point midPointExtension = new Point((anchorExtensionPoint.X + anchorEndPoint.X) / 2.0, (anchorExtensionPoint.Y + anchorEndPoint.Y) / 2.0);
		foreach (PriceLevel item in base.PriceLevels.Where((PriceLevel pl) => pl.IsVisible))
		{
			double num = startPrice + item.Value / 100.0 * totalPriceRange;
			float num2 = chartScale.GetYByValue(num);
			float num3 = ((anchorExtensionPoint.X > anchorEndPoint.X) ? ((float)(anchorExtensionPoint.X - Math.Abs((anchorEndPoint.X - anchorExtensionPoint.X) * (item.Value / 100.0)))) : ((float)(anchorExtensionPoint.X + (anchorEndPoint.X - anchorExtensionPoint.X) * (item.Value / 100.0))));
			Point point = new Point(num3, num2);
			Point point2 = new Point(point.X + (midPointExtension.X - anchorStartPoint.X), point.Y + (midPointExtension.Y - anchorStartPoint.Y));
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(point, point2);
			yield return new Tuple<Point, Point>(new Point(Math.Max(extendedPoint.X, 1.0), Math.Max(extendedPoint.Y, 1.0)), point);
		}
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected I4, but got Unknown
		if (!((NinjaScript)this).IsVisible)
		{
			return null;
		}
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
			if (editingAnchor != StartAnchor)
			{
				return Cursors.SizeNWSE;
			}
			return Cursors.SizeNESW;
		default:
		{
			Point point2 = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
			if (closestAnchor != null)
			{
				if (((DrawingTool)this).IsLocked)
				{
					return Cursors.Arrow;
				}
				if (closestAnchor != StartAnchor)
				{
					return Cursors.SizeNWSE;
				}
				return Cursors.SizeNESW;
			}
			Point point3 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point4 = ExtensionAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point5 = new Point((point3.X + point4.X) / 2.0, (point3.Y + point4.Y) / 2.0);
			Vector vector = point3 - point2;
			Vector vector2 = point4 - point3;
			Vector vector3 = point5 - point2;
			foreach (Tuple<Point, Point> andrewsEndPoint in GetAndrewsEndPoints(chartControl, chartScale))
			{
				Vector vector4 = andrewsEndPoint.Item1 - andrewsEndPoint.Item2;
				if (MathHelper.IsPointAlongVector(point, andrewsEndPoint.Item2, vector4, 15.0))
				{
					return ((DrawingTool)this).IsLocked ? Cursors.Arrow : Cursors.SizeAll;
				}
			}
			if (!MathHelper.IsPointAlongVector(point, point2, vector, 15.0) && !MathHelper.IsPointAlongVector(point, point3, vector2, 15.0) && !MathHelper.IsPointAlongVector(point, point2, vector3, 15.0))
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

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		if (!((NinjaScript)this).IsVisible)
		{
			return Array.Empty<Point>();
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = new Point((point.X + point2.X) / 2.0, (point.Y + point2.Y) / 2.0);
		Point point4 = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		return new Point[4] { point, point3, point2, point4 };
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Invalid comparison between Unknown and I4
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Expected I4, but got Unknown
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Invalid comparison between Unknown and I4
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Invalid comparison between Unknown and I4
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Invalid comparison between Unknown and I4
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Invalid comparison between Unknown and I4
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Invalid comparison between Unknown and I4
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Invalid comparison between Unknown and I4
		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Invalid comparison between Unknown and I4
		if (!(conditionItem.Tag is PriceLevel priceLevel))
		{
			return false;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point4 = new Point((point3.X + point2.X) / 2.0, (point3.Y + point2.Y) / 2.0);
		if (CalculationMethod == AndrewsPitchforkCalculationMethod.Schiff)
		{
			point = new Point(point.X, (point.Y + point2.Y) / 2.0);
		}
		else if (CalculationMethod == AndrewsPitchforkCalculationMethod.ModifiedSchiff)
		{
			point = new Point((point2.X + point.X) / 2.0, (point2.Y + point.Y) / 2.0);
		}
		double num = EndAnchor.Price - ExtensionAnchor.Price;
		double num2 = ExtensionAnchor.Price + priceLevel.Value / 100.0 * num;
		float num3 = chartScale.GetYByValue(num2);
		float num4 = ((point3.X > point2.X) ? ((float)(point3.X - Math.Abs((point2.X - point3.X) * (priceLevel.Value / 100.0)))) : ((float)(point3.X + (point2.X - point3.X) * (priceLevel.Value / 100.0))));
		Point alertStartPoint = new Point(num4, num3);
		Point point5 = new Point(alertStartPoint.X + (point4.X - point.X), alertStartPoint.Y + (point4.Y - point.Y));
		Point alertEndPoint = ((DrawingTool)this).GetExtendedPoint(alertStartPoint, point5);
		double num5 = (((int)values[0].ValueType == 12) ? num4 : ((float)chartControl.GetXByTime(values[0].Time)));
		double y = chartScale.GetYByValue(values[0].Value);
		Point point6 = new Point(num5, y);
		if (IsExtendedLinesBack)
		{
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(alertEndPoint, alertStartPoint);
			if (extendedPoint.X > -1.0 || extendedPoint.Y > -1.0)
			{
				alertStartPoint = extendedPoint;
			}
		}
		if (num5 < alertStartPoint.X || num5 > alertEndPoint.X)
		{
			return false;
		}
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(alertStartPoint, alertEndPoint, point6);
		Condition val2 = condition;
		switch ((int)val2)
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
			double x = chartControl.GetXByTime(v.Time);
			double y2 = chartScale.GetYByValue(v.Value);
			Point point7 = new Point(x, y2);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(alertStartPoint, alertEndPoint, point7);
			if ((int)condition == 0)
			{
				return (int)pointLineLocation2 == 0;
			}
			return (int)pointLineLocation2 == 1;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		bool flag = false;
		bool flag2 = false;
		foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
		{
			if (anchor.IsEditing)
			{
				return true;
			}
			if (anchor.Time >= firstTimeOnChart && anchor.Time <= lastTimeOnChart)
			{
				return true;
			}
			if (anchor.Time < firstTimeOnChart)
			{
				flag = true;
			}
			else if (anchor.Time > lastTimeOnChart)
			{
				flag2 = true;
			}
			if (flag && flag2)
			{
				return true;
			}
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point4 = new Point((point3.X + point2.X) / 2.0, (point3.Y + point2.Y) / 2.0);
		if (CalculationMethod == AndrewsPitchforkCalculationMethod.Schiff)
		{
			point = new Point(point.X, (point.Y + point2.Y) / 2.0);
		}
		else if (CalculationMethod == AndrewsPitchforkCalculationMethod.ModifiedSchiff)
		{
			point = new Point((point2.X + point.X) / 2.0, (point2.Y + point.Y) / 2.0);
		}
		double num = EndAnchor.Price - ExtensionAnchor.Price;
		double price = ExtensionAnchor.Price;
		foreach (PriceLevel item in base.PriceLevels.Where((PriceLevel pl) => pl.IsVisible && pl.Stroke != null))
		{
			double num2 = price + item.Value / 100.0 * num;
			float num3 = chartScale.GetYByValue(num2);
			float num4 = ((!(point3.X > point2.X)) ? ((item.Value >= 0.0) ? ((float)(point3.X + (point2.X - point3.X) * (item.Value / 100.0))) : ((float)(point3.X - Math.Abs((point2.X - point3.X) * (item.Value / 100.0))))) : ((item.Value >= 0.0) ? ((float)(point3.X - Math.Abs((point2.X - point3.X) * (item.Value / 100.0)))) : ((float)(point3.X + (point2.X - point3.X) * (item.Value / 100.0)))));
			Point point5 = new Point(num4, num3);
			Point point6 = new Point(point5.X + (point4.X - point.X), point5.Y + (point4.Y - point.Y));
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(point5, point6);
			double num5 = 5.0;
			Point[] array = new Point[3] { point5, extendedPoint, point6 };
			for (int num6 = 0; num6 < array.Length; num6++)
			{
				Point point7 = array[num6];
				if (point7.X >= (double)val.X - num5 && point7.X <= (double)(val.W + val.X) + num5 && point7.Y >= (double)val.Y - num5 && point7.Y <= (double)(val.Y + val.H) + num5)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (!((NinjaScript)this).IsVisible)
		{
			return;
		}
		foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
		{
			((ChartObject)this).MinValue = Math.Min(((ChartObject)this).MinValue, anchor.Price);
			((ChartObject)this).MaxValue = Math.Max(((ChartObject)this).MaxValue, anchor.Price);
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
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeAll)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeNESW || ((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeNWSE)
				{
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.Arrow)
				{
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == null)
				{
					((ChartObject)this).IsSelected = false;
				}
			}
		}
		else
		{
			if (StartAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(StartAnchor);
				dataPoint.CopyDataValues(EndAnchor);
				StartAnchor.IsEditing = false;
			}
			else if (EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EndAnchor);
				dataPoint.CopyDataValues(ExtensionAnchor);
				EndAnchor.IsEditing = false;
			}
			else if (ExtensionAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(ExtensionAnchor);
				ExtensionAnchor.IsEditing = false;
			}
			if (((DrawingTool)this).Anchors.All((ChartAnchor a) => !a.IsEditing))
			{
				((DrawingTool)this).DrawingState = (DrawingState)2;
				((ChartObject)this).IsSelected = false;
			}
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Invalid comparison between Unknown and I4
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EndAnchor);
			}
			else if (ExtensionAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(ExtensionAnchor);
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
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState == 1 || (int)((DrawingTool)this).DrawingState == 3)
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
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
		//IL_0547: Unknown result type (might be due to invalid IL or missing references)
		//IL_0500: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a9: Expected O, but got Unknown
		//IL_05b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0591: Unknown result type (might be due to invalid IL or missing references)
		//IL_0598: Expected O, but got Unknown
		//IL_0674: Unknown result type (might be due to invalid IL or missing references)
		//IL_0682: Unknown result type (might be due to invalid IL or missing references)
		//IL_0690: Unknown result type (might be due to invalid IL or missing references)
		//IL_0666: Unknown result type (might be due to invalid IL or missing references)
		if (((DrawingTool)this).Anchors.All((ChartAnchor a) => a.IsEditing))
		{
			return;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point4 = new Point((point3.X + point2.X) / 2.0, (point3.Y + point2.Y) / 2.0);
		if (CalculationMethod == AndrewsPitchforkCalculationMethod.Schiff)
		{
			point = new Point(point.X, (point.Y + point2.Y) / 2.0);
		}
		else if (CalculationMethod == AndrewsPitchforkCalculationMethod.ModifiedSchiff)
		{
			point = new Point((point2.X + point.X) / 2.0, (point2.Y + point.Y) / 2.0);
		}
		AnchorLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		RetracementLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		double num = ((AnchorLineStroke.Width % 2f == 0f) ? 0.5 : 0.0);
		Vector vector = new Vector(num, num);
		Vector2 val2 = DxExtensions.ToVector2(point + vector);
		Vector2 val3 = DxExtensions.ToVector2(point2 + vector);
		Brush val4 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : AnchorLineStroke.BrushDX);
		Vector2 val5 = DxExtensions.ToVector2(StartAnchor.GetPoint(chartControl, val, chartScale, true) + vector);
		((ChartObject)this).RenderTarget.DrawLine(val5, val3, val4, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
		if (ExtensionAnchor.IsEditing && EndAnchor.IsEditing)
		{
			return;
		}
		Vector2 val6 = DxExtensions.ToVector2(point3);
		val4 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : RetracementLineStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(val3, val6, val4, RetracementLineStroke.Width, RetracementLineStroke.StrokeStyle);
		if (((ChartObject)this).IsInHitTest || base.PriceLevels == null || !base.PriceLevels.Any())
		{
			return;
		}
		SetAllPriceLevelsRenderTarget();
		double num2 = EndAnchor.Price - ExtensionAnchor.Price;
		double price = ExtensionAnchor.Price;
		float val7 = float.MaxValue;
		float val8 = float.MinValue;
		Point point5 = new Point(0.0, 0.0);
		Point point6 = new Point(0.0, 0.0);
		Stroke val9 = null;
		List<Tuple<PriceLevel, Point>> list = new List<Tuple<PriceLevel, Point>>();
		foreach (PriceLevel item in from pl in base.PriceLevels
			where pl.IsVisible && pl.Stroke != null
			orderby pl.Value
			select pl)
		{
			double num3 = price + item.Value / 100.0 * num2;
			float num4 = chartScale.GetYByValue(num3);
			float num5 = ((!(point3.X > point2.X)) ? ((item.Value >= 0.0) ? ((float)(point3.X + (point2.X - point3.X) * (item.Value / 100.0))) : ((float)(point3.X - Math.Abs((point2.X - point3.X) * (item.Value / 100.0))))) : ((item.Value >= 0.0) ? ((float)(point3.X - Math.Abs((point2.X - point3.X) * (item.Value / 100.0)))) : ((float)(point3.X + (point2.X - point3.X) * (item.Value / 100.0)))));
			Point point7 = new Point(num5, num4);
			Point point8 = new Point(point7.X + (point4.X - point.X), point7.Y + (point4.Y - point.Y));
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(point7, point8);
			if (Math.Abs(item.Value - 50.0) < 1E-16)
			{
				((ChartObject)this).RenderTarget.DrawLine(IsExtendedLinesBack ? DxExtensions.ToVector2(((DrawingTool)this).GetExtendedPoint(point8, point7)) : val2, DxExtensions.ToVector2(extendedPoint), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			}
			else
			{
				((ChartObject)this).RenderTarget.DrawLine(IsExtendedLinesBack ? DxExtensions.ToVector2(((DrawingTool)this).GetExtendedPoint(point8, point7)) : DxExtensions.ToVector2(point7), DxExtensions.ToVector2(extendedPoint), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			}
			if (val9 == null)
			{
				val9 = new Stroke();
			}
			else
			{
				PathGeometry val10 = new PathGeometry(Globals.D2DFactory);
				GeometrySink val11 = val10.Open();
				((SimplifiedGeometrySink)val11).BeginFigure(DxExtensions.ToVector2(point5), (FigureBegin)0);
				if (Math.Abs(point5.Y - extendedPoint.Y) > 0.0 && Math.Abs(point5.X - extendedPoint.X) > 0.0)
				{
					double y;
					double x;
					if (point5.Y <= (double)((ChartObject)this).ChartPanel.Y || point5.Y >= (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
					{
						y = point5.Y;
						x = extendedPoint.X;
					}
					else
					{
						y = extendedPoint.Y;
						x = point5.X;
					}
					val11.AddLine(new Vector2((float)x, (float)y));
				}
				val11.AddLine(DxExtensions.ToVector2(extendedPoint));
				val11.AddLine(DxExtensions.ToVector2(point7));
				val11.AddLine(DxExtensions.ToVector2(point6));
				((SimplifiedGeometrySink)val11).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)val11).Close();
				((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val10, val9.BrushDX);
				((DisposeBase)val10).Dispose();
			}
			if (IsTextDisplayed)
			{
				list.Add(new Tuple<PriceLevel, Point>(item, extendedPoint));
			}
			item.Stroke.CopyTo(val9);
			val9.Opacity = PriceLevelOpacity;
			point6 = point7;
			point5 = extendedPoint;
			val7 = Math.Min(num4, val7);
			val8 = Math.Max(num4, val8);
		}
		if (!IsTextDisplayed)
		{
			return;
		}
		foreach (Tuple<PriceLevel, Point> item2 in list)
		{
			DrawPriceLevelText(0.0, 0.0, item2.Item2, item2.Item1, val);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		((DrawingTool)this).OnStateChange();
		State state = ((NinjaScript)this).State;
		if ((int)state != 1)
		{
			if ((int)state != 2)
			{
				if ((int)state == 8)
				{
					((DrawingTool)this).Dispose();
				}
			}
			else if (base.PriceLevels.Count == 0)
			{
				base.PriceLevels.Add(new PriceLevel(0.0, Brushes.SeaGreen));
				base.PriceLevels.Add(new PriceLevel(50.0, Brushes.SeaGreen));
				base.PriceLevels.Add(new PriceLevel(100.0, Brushes.SeaGreen));
			}
			return;
		}
		AnchorLineStroke = new Stroke((Brush)Brushes.DarkGray, (DashStyleHelper)0, 1f, 50);
		RetracementLineStroke = new Stroke((Brush)Brushes.SeaGreen, (DashStyleHelper)0, 2f);
		((NinjaScript)this).Description = Resource.NinjaScriptDrawingToolAndrewsPitchforkDescription;
		((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolAndrewsPitchfork;
		StartAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this
		};
		ExtensionAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this
		};
		EndAnchor = new ChartAnchor
		{
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this
		};
		StartAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorStart;
		EndAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd;
		ExtensionAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorExtension;
		PriceLevelOpacity = 5;
		IsTextDisplayed = true;
	}
}

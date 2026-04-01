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
/// Represents an interface that exposes information regarding a Fibonacci Extensions IDrawingTool.
/// </summary>
public class FibonacciExtensions : FibonacciRetracements
{
	private Point anchorExtensionPoint;

	[Display(Order = 3)]
	public ChartAnchor ExtensionAnchor { get; set; }

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[3] { base.StartAnchor, base.EndAnchor, ExtensionAnchor };

	public override object Icon => Icons.DrawFbExtensions;

	protected new Tuple<Point, Point> GetPriceLevelLinePoints(PriceLevel priceLevel, ChartControl chartControl, ChartScale chartScale, bool isInverted)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double totalPriceRange = base.EndAnchor.Price - base.StartAnchor.Price;
		double num = Math.Min(point.X, point2.X);
		double num2 = Math.Max(point.X, point2.X);
		double x = (base.IsExtendedLinesLeft ? ((double)val.X) : num);
		double x2 = (base.IsExtendedLinesRight ? ((double)(val.X + val.W)) : num2);
		double y = priceLevel.GetY(chartScale, ExtensionAnchor.Price, totalPriceRange, isInverted);
		return new Tuple<Point, Point>(new Point(x, y), new Point(x2, y));
	}

	private new void DrawPriceLevelText(ChartPanel chartPanel, ChartScale _, double minX, double maxX, double y, double price, PriceLevel priceLevel)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Invalid comparison between Unknown and I4
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Invalid comparison between Unknown and I4
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Invalid comparison between Unknown and I4
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		if ((int)base.TextLocation == 4)
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
			string priceString = GetPriceString(price, priceLevel, chartPanel);
			float num = (float)Math.Abs(maxX - minX);
			TextLayout val2 = new TextLayout(Globals.DirectWriteFactory, priceString, val, num, val.FontSize);
			double num2;
			if (base.IsExtendedLinesLeft && (int)base.TextLocation == 1)
			{
				num2 = (double)chartPanel.X + 2.0;
			}
			else if (base.IsExtendedLinesRight && (int)base.TextLocation == 3)
			{
				num2 = (float)(chartPanel.X + chartPanel.W) - val2.Metrics.Width;
			}
			else
			{
				TextLocation textLocation = base.TextLocation;
				bool flag = (int)textLocation <= 1;
				num2 = ((!flag) ? ((minX > maxX) ? (minX - (double)val2.Metrics.Width) : (maxX - (double)val2.Metrics.Width)) : ((minX <= maxX) ? (minX - 1.0) : (maxX - 1.0)));
			}
			((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2((float)num2, (float)(y - (double)val.FontSize - 2.0)), val2, priceLevel.Stroke.BrushDX, (DrawTextOptions)1);
			((DisposeBase)val).Dispose();
			((DisposeBase)val2).Dispose();
		}
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState != 2)
		{
			return base.GetCursor(chartControl, chartPanel, chartScale, point);
		}
		Point point2 = base.StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
		if (closestAnchor != null)
		{
			if (((DrawingTool)this).IsLocked)
			{
				return Cursors.Arrow;
			}
			if (closestAnchor != base.StartAnchor)
			{
				return Cursors.SizeNWSE;
			}
			return Cursors.SizeNESW;
		}
		Point point3 = base.EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Point point4 = ExtensionAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Tuple<Point, Point> translatedExtensionYLine = GetTranslatedExtensionYLine(chartControl, chartScale);
		Vector item = point3 - point2;
		Vector item2 = point4 - point3;
		Vector item3 = translatedExtensionYLine.Item2 - translatedExtensionYLine.Item1;
		if (new Tuple<Vector, Point>[3]
		{
			new Tuple<Vector, Point>(item, point2),
			new Tuple<Vector, Point>(item2, point3),
			new Tuple<Vector, Point>(item3, translatedExtensionYLine.Item1)
		}.Any((Tuple<Vector, Point> chkTup) => MathHelper.IsPointAlongVector(point, chkTup.Item2, chkTup.Item1, 15.0)))
		{
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.Arrow;
		}
		return null;
	}

	private Point GetEndLineMidpoint(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		return new Point((point.X + point2.X) / 2.0, (point.Y + point2.Y) / 2.0);
	}

	public sealed override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		Point[] selectionPoints = base.GetSelectionPoints(chartControl, chartScale);
		if (!ExtensionAnchor.IsEditing || !base.EndAnchor.IsEditing)
		{
			Tuple<Point, Point> translatedExtensionYLine = GetTranslatedExtensionYLine(chartControl, chartScale);
			Point point = translatedExtensionYLine.Item1 + (translatedExtensionYLine.Item2 - translatedExtensionYLine.Item1) / 2.0;
			Point endLineMidpoint = GetEndLineMidpoint(chartControl, chartScale);
			return selectionPoints.Union(new Point[4] { translatedExtensionYLine.Item1, translatedExtensionYLine.Item2, point, endLineMidpoint }).ToArray();
		}
		return selectionPoints;
	}

	private string GetPriceString(double price, PriceLevel priceLevel, ChartPanel _)
	{
		string text = price.ToString(Globals.GetTickFormatString(((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize));
		return (priceLevel.Value / 100.0).ToString("P", Globals.GeneralOptions.CurrentCulture) + " (" + text + ")";
	}

	private Tuple<Point, Point> GetTranslatedExtensionYLine(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = double.MaxValue;
		foreach (Tuple<Point, Point> item in from pl in base.PriceLevels
			where pl.IsVisible
			select GetPriceLevelLinePoints(pl, chartControl, chartScale, isInverted: false))
		{
			Vector vector = point - point2;
			Point point3 = new Point((item.Item1 + vector).X, item.Item1.Y);
			num = Math.Min(point3.Y, num);
		}
		if (MathExtentions.ApproxCompare(num, double.MaxValue) == 0)
		{
			return new Tuple<Point, Point>(new Point(point.X, point.Y), new Point(point.X, point.Y));
		}
		return new Tuple<Point, Point>(new Point(point.X, num), new Point(point.X, anchorExtensionPoint.Y));
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		if (!(conditionItem.Tag is PriceLevel priceLevel))
		{
			return false;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Tuple<Point, Point> priceLevelLinePoints = GetPriceLevelLinePoints(priceLevel, chartControl, chartScale, isInverted: false);
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Vector vector = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true) - point;
		Point lineStartPoint = priceLevelLinePoints.Item1 + vector;
		Point lineEndPoint = priceLevelLinePoints.Item2 + vector;
		return CheckAlertRetracementLine(condition, lineStartPoint, lineEndPoint, chartControl, chartScale, values);
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Invalid comparison between Unknown and I4
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState != 2)
			{
				return;
			}
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			base.OnMouseDown(chartControl, chartPanel, chartScale, dataPoint);
			if ((int)((DrawingTool)this).DrawingState == 2)
			{
				Tuple<Point, Point> translatedExtensionYLine = GetTranslatedExtensionYLine(chartControl, chartScale);
				Vector vector = translatedExtensionYLine.Item2 - translatedExtensionYLine.Item1;
				if (MathHelper.IsPointAlongVector(new Point(point.X, ((DrawingTool)this).ConvertToVerticalPixels(chartControl, chartPanel, point.Y)), translatedExtensionYLine.Item1, vector, 15.0))
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
				else
				{
					((ChartObject)this).IsSelected = false;
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
			dataPoint.CopyDataValues(ExtensionAnchor);
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

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (!((DrawingTool)this).IsLocked || (int)((DrawingTool)this).DrawingState == 0)
		{
			base.OnMouseMove(chartControl, chartPanel, chartScale, dataPoint);
			if ((int)((DrawingTool)this).DrawingState == 0 && ExtensionAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(ExtensionAnchor);
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Invalid comparison between Unknown and I4
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			base.AnchorLineStroke = new Stroke((Brush)Brushes.DarkGray, (DashStyleHelper)0, 1f, 50);
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolFibonacciExtensions;
			base.PriceLevelOpacity = 5;
			base.StartAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			ExtensionAnchor = new ChartAnchor
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
			ExtensionAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorExtension;
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

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Expected O, but got Unknown
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		if (((DrawingTool)this).Anchors.All((ChartAnchor a) => a.IsEditing))
		{
			return;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		anchorExtensionPoint = ExtensionAnchor.GetPoint(chartControl, val, chartScale, true);
		base.AnchorLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		double num = ((MathExtentions.ApproxCompare((double)base.AnchorLineStroke.Width % 2.0, 0.0) == 0) ? 0.5 : 0.0);
		Vector vector = new Vector(num, num);
		Vector2 val2 = DxExtensions.ToVector2(point + vector);
		Vector2 val3 = DxExtensions.ToVector2(point2 + vector);
		((ChartObject)this).RenderTarget.DrawLine(val2, val3, base.AnchorLineStroke.BrushDX, base.AnchorLineStroke.Width, base.AnchorLineStroke.StrokeStyle);
		if (ExtensionAnchor.IsEditing && base.EndAnchor.IsEditing)
		{
			return;
		}
		Vector2 val4 = DxExtensions.ToVector2(anchorExtensionPoint);
		Brush val5 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : base.AnchorLineStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(val3, val4, val5, base.AnchorLineStroke.Width, base.AnchorLineStroke.StrokeStyle);
		if (base.PriceLevels == null || !base.PriceLevels.Any())
		{
			return;
		}
		SetAllPriceLevelsRenderTarget();
		double num2 = 3.4028234663852886E+38;
		double num3 = -3.4028234663852886E+38;
		Point point3 = new Point(0.0, 0.0);
		Stroke val6 = null;
		int num4 = 0;
		RectangleF val7 = default(RectangleF);
		foreach (PriceLevel item in from pl in base.PriceLevels
			where pl.IsVisible && pl.Stroke != null
			orderby pl.Value
			select pl)
		{
			Tuple<Point, Point> priceLevelLinePoints = GetPriceLevelLinePoints(item, chartControl, chartScale, isInverted: false);
			Vector vector2 = anchorExtensionPoint - point;
			Point point4 = priceLevelLinePoints.Item1 + vector2;
			Point point5 = priceLevelLinePoints.Item2 + vector2;
			double x = (base.IsExtendedLinesLeft ? priceLevelLinePoints.Item1.X : point4.X);
			double x2 = (base.IsExtendedLinesRight ? priceLevelLinePoints.Item2.X : point5.X);
			Point point6 = new Point(x, priceLevelLinePoints.Item1.Y);
			Point point7 = new Point(x2, priceLevelLinePoints.Item2.Y);
			double num5 = ((MathExtentions.ApproxCompare((double)item.Stroke.Width % 2.0, 0.0) == 0) ? 0.5 : 0.0);
			Vector vector3 = new Vector(num5, num5);
			Point point8 = point6 + vector3;
			Point point9 = point7 + vector3;
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point8), DxExtensions.ToVector2(point9), item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			if (val6 == null)
			{
				val6 = new Stroke();
			}
			else if (!((ChartObject)this).IsInHitTest)
			{
				((RectangleF)(ref val7))._002Ector((float)point3.X, (float)point3.Y, (float)(point9.X - point3.X), (float)(point9.Y - point3.Y));
				((ChartObject)this).RenderTarget.FillRectangle(val7, val6.BrushDX);
			}
			item.Stroke.CopyTo(val6);
			val6.Opacity = base.PriceLevelOpacity;
			point3 = point8;
			num2 = Math.Min(point6.Y, num2);
			num3 = Math.Max(point6.Y, num3);
			num4++;
		}
		if (!((ChartObject)this).IsInHitTest)
		{
			foreach (PriceLevel item2 in from pl in base.PriceLevels
				where pl.IsVisible && pl.Stroke != null
				orderby pl.Value
				select pl)
			{
				Tuple<Point, Point> priceLevelLinePoints2 = GetPriceLevelLinePoints(item2, chartControl, chartScale, isInverted: false);
				Vector vector4 = anchorExtensionPoint - point;
				Point point10 = priceLevelLinePoints2.Item1 + vector4;
				double x3 = (base.IsExtendedLinesLeft ? priceLevelLinePoints2.Item1.X : point10.X);
				Point point11 = new Point(x3, priceLevelLinePoints2.Item1.Y);
				double x4 = anchorExtensionPoint.X;
				double maxX = anchorExtensionPoint.X + point2.X - point.X;
				double totalPriceRange = base.EndAnchor.Price - base.StartAnchor.Price;
				double price = item2.GetPrice(ExtensionAnchor.Price, totalPriceRange, isInverted: false);
				DrawPriceLevelText(val, chartScale, x4, maxX, point11.Y, price, item2);
			}
		}
		if (num4 > 0)
		{
			((ChartObject)this).RenderTarget.DrawLine(new Vector2(val4.X, (float)num2), new Vector2(val4.X, (float)num3), base.AnchorLineStroke.BrushDX, base.AnchorLineStroke.Width, base.AnchorLineStroke.StrokeStyle);
		}
	}
}

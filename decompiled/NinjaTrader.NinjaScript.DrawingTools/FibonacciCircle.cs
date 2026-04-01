using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
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
/// Represents an interface that exposes information regarding a Fibonacci Circle IDrawingTool.
/// </summary>
[TypeConverter("NinjaTrader.NinjaScript.DrawingTools.FibonacciCircleTimeTypeConverter")]
public class FibonacciCircle : FibonacciRetracements
{
	public override object Icon => Icons.DrawFbCircle;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciTimeExtensionsShowText", GroupName = "NinjaScriptGeneral")]
	public bool IsTextDisplayed { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciTimeCircleDivideTimeSeparately", GroupName = "NinjaScriptGeneral", Order = 1)]
	public bool IsTimePriceDividedSeparately { get; set; }

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

	private void DrawPriceLevelText(float textX, float textY, PriceLevel priceLevel, double yVal, ChartControl chartControl)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (IsTextDisplayed)
		{
			TextFormat val = ((SimpleFont)(((object)chartControl.Properties.LabelFont) ?? ((object)new SimpleFont()))).ToDirectWriteTextFormat();
			val.TextAlignment = (TextAlignment)0;
			val.WordWrapping = (WordWrapping)1;
			string priceString = GetPriceString(yVal, priceLevel);
			TextLayout val2 = new TextLayout(Globals.DirectWriteFactory, priceString, val, 250f, val.FontSize);
			((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2(textX, textY), val2, priceLevel.Stroke.BrushDX, (DrawTextOptions)1);
			((DisposeBase)val).Dispose();
			((DisposeBase)val2).Dispose();
		}
	}

	private string GetPriceString(double yVal, PriceLevel priceLevel)
	{
		string text = yVal.ToString(Globals.GetTickFormatString(((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize));
		return (priceLevel.Value / 100.0).ToString("P", Globals.GeneralOptions.CurrentCulture) + " (" + text + ")";
	}

	public override IEnumerable<Condition> GetValidAlertConditions()
	{
		return (IEnumerable<Condition>)(object)new Condition[2]
		{
			(Condition)8,
			(Condition)9
		};
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return false;
		}
		if (!(conditionItem.Tag is PriceLevel priceLevel))
		{
			return false;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = Math.Abs(point2.X - point.X);
		double num2 = Math.Abs(point2.Y - point.Y);
		double num3 = Math.Sqrt(Math.Pow(num, 2.0) + Math.Pow(num2, 2.0));
		float num4 = (float)priceLevel.Value / 100f;
		float num5 = (float)((double)num4 * num3);
		float num6 = (float)((double)num4 * num);
		float num7 = (float)((double)num4 * num2);
		float ellipseRadiusX = (IsTimePriceDividedSeparately ? num6 : num5);
		float ellipseRadiusY = (IsTimePriceDividedSeparately ? num7 : num5);
		Point centerPoint = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate);
		bool Predicate(ChartAlertValue v)
		{
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Invalid comparison between Unknown and I4
			Point point3 = new Point(chartControl.GetXByTime(v.Time), chartScale.GetYByValue(v.Value));
			bool flag = MathHelper.IsPointInsideEllipse(centerPoint, point3, (double)ellipseRadiusX, (double)ellipseRadiusY);
			if ((int)condition != 8)
			{
				return !flag;
			}
			return flag;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		double num = base.PriceLevels.Max((PriceLevel pl) => pl.Value);
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double num2 = Math.Abs(point2.X - point.X);
		double num3 = Math.Abs(point2.Y - point.Y);
		double num4 = Math.Sqrt(Math.Pow(num2, 2.0) + Math.Pow(num3, 2.0));
		float num5 = (float)num / 100f;
		float num6 = (float)((double)num5 * num4);
		float num7 = (float)((double)num5 * num2);
		float num8 = (float)((double)num5 * num3);
		float num9 = (IsTimePriceDividedSeparately ? num7 : num6);
		float num10 = (IsTimePriceDividedSeparately ? num8 : num6);
		double num11 = point.X - (double)num9;
		double num12 = point.X + (double)num9;
		DateTime timeByX = chartControl.GetTimeByX((int)num11);
		if (chartControl.GetTimeByX((int)num12) < firstTimeOnChart || timeByX > lastTimeOnChart)
		{
			return false;
		}
		float num13 = (float)point.Y - num10;
		float num14 = (float)point.Y + num10;
		double valueByY = chartScale.GetValueByY(num14);
		if (chartScale.GetValueByY(num13) < chartScale.MinValue || valueByY > chartScale.MaxValue)
		{
			return false;
		}
		return true;
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Expected O, but got Unknown
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Expected O, but got Unknown
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Expected O, but got Unknown
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c6: Unknown result type (might be due to invalid IL or missing references)
		if (((DrawingTool)this).Anchors.All((ChartAnchor a) => a.IsEditing))
		{
			return;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
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
		double num = Math.Abs(point2.X - point.X);
		double num2 = Math.Abs(point2.Y - point.Y);
		double num3 = Math.Sqrt(Math.Pow(num, 2.0) + Math.Pow(num2, 2.0));
		EllipseGeometry val3 = null;
		Vector2 val4 = default(Vector2);
		foreach (PriceLevel item in from pl in base.PriceLevels
			where pl.IsVisible && pl.Stroke != null
			orderby pl.Value
			select pl)
		{
			float num4 = (float)item.Value / 100f;
			float num5 = (float)((double)num4 * num3);
			float num6 = (float)((double)num4 * num);
			float num7 = (float)((double)num4 * num2);
			((Vector2)(ref val4))._002Ector((float)point.X, (float)point.Y);
			Ellipse val5 = (IsTimePriceDividedSeparately ? new Ellipse(val4, num6, num7) : new Ellipse(val4, num5, num5));
			EllipseGeometry val6 = new EllipseGeometry(Globals.D2DFactory, val5);
			((ChartObject)this).RenderTarget.DrawEllipse(val5, item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			if (!((ChartObject)this).IsInHitTest)
			{
				Stroke val7 = new Stroke();
				item.Stroke.CopyTo(val7);
				val7.Opacity = base.PriceLevelOpacity;
				if (val3 == null)
				{
					((ChartObject)this).RenderTarget.FillEllipse(val5, val7.BrushDX);
				}
				else
				{
					GeometryGroup val8 = new GeometryGroup(Globals.D2DFactory, (FillMode)0, (Geometry[])(object)new Geometry[2]
					{
						(Geometry)val3,
						(Geometry)val6
					});
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val8, val7.BrushDX);
					((DisposeBase)val8).Dispose();
				}
				val3 = val6;
			}
		}
		if (val3 != null && !((DisposeBase)val3).IsDisposed)
		{
			((DisposeBase)val3).Dispose();
		}
		if (((ChartObject)this).IsInHitTest)
		{
			return;
		}
		Vector2 val9 = default(Vector2);
		foreach (PriceLevel item2 in from pl in base.PriceLevels
			where pl.IsVisible && pl.Stroke != null
			orderby pl.Value
			select pl)
		{
			((Vector2)(ref val9))._002Ector((float)point.X, (float)point.Y);
			float num8 = (float)item2.Value / 100f;
			float num9 = (float)((double)num8 * num3);
			float num10 = (float)((double)num8 * num2);
			float x = val9.X;
			double yVal = base.StartAnchor.Price + (base.EndAnchor.Price - base.StartAnchor.Price) * (double)num8;
			float textY = (IsTimePriceDividedSeparately ? (val9.Y + num10) : (val9.Y + num9));
			DrawPriceLevelText(x, textY, item2, yVal, chartControl);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Invalid comparison between Unknown and I4
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
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			base.AnchorLineStroke = new Stroke((Brush)Brushes.DarkGray, (DashStyleHelper)0, 1f, 50);
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolFibonacciCircle;
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
			IsTextDisplayed = true;
			IsTimePriceDividedSeparately = false;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			if (base.PriceLevels.Count == 0)
			{
				base.PriceLevels.Add(new PriceLevel(38.2, Brushes.DodgerBlue));
				base.PriceLevels.Add(new PriceLevel(61.8, Brushes.CornflowerBlue));
				base.PriceLevels.Add(new PriceLevel(100.0, Brushes.SteelBlue));
				base.PriceLevels.Add(new PriceLevel(138.2, Brushes.DarkCyan));
				base.PriceLevels.Add(new PriceLevel(161.8, Brushes.SeaGreen));
			}
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
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
/// Represents an interface that exposes information regarding a Fibonacci Time Extensions IDrawingTool.
/// </summary>
[TypeConverter("NinjaTrader.NinjaScript.DrawingTools.FibonacciCircleTimeTypeConverter")]
public class FibonacciTimeExtensions : FibonacciRetracements
{
	public override object Icon => Icons.DrawFbFbTimeExtensions;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciTimeExtensionsShowText", GroupName = "NinjaScriptGeneral")]
	public bool IsTextDisplayed { get; set; }

	public override IEnumerable<Condition> GetValidAlertConditions()
	{
		Condition[] array = new Condition[5];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		return (IEnumerable<Condition>)(object)array;
	}

	private void DrawPriceLevelText(double x, PriceLevel priceLevel, ChartPanel chartPanel)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (IsTextDisplayed)
		{
			TextFormat val = ((SimpleFont)(((object)chartPanel.ChartControl.Properties.LabelFont) ?? ((object)new SimpleFont()))).ToDirectWriteTextFormat();
			val.TextAlignment = (TextAlignment)0;
			val.WordWrapping = (WordWrapping)1;
			string text = (priceLevel.Value / 100.0).ToString("P", Globals.GeneralOptions.CurrentCulture);
			float num = (float)chartPanel.Y + (float)chartPanel.H;
			TextLayout val2 = new TextLayout(Globals.DirectWriteFactory, text, val, (float)chartPanel.W, val.FontSize);
			Vector2 val3 = default(Vector2);
			((Vector2)(ref val3))._002Ector((float)x - val2.Metrics.Height, num);
			Matrix3x2 transform = Matrix3x2.Rotation(MathHelper.DegreesToRadians(-90f), Vector2.Zero) * Matrix3x2.Translation(val3);
			((ChartObject)this).RenderTarget.Transform = transform;
			Stroke val4 = new Stroke();
			priceLevel.Stroke.CopyTo(val4);
			val4.Opacity = 70;
			((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2(0f, 0f), val2, priceLevel.Stroke.BrushDX, (DrawTextOptions)1);
			((ChartObject)this).RenderTarget.Transform = Matrix3x2.Identity;
			((DisposeBase)val).Dispose();
			((DisposeBase)val2).Dispose();
		}
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected I4, but got Unknown
		if (!(conditionItem.Tag is PriceLevel priceLevel))
		{
			return false;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = Math.Abs(base.EndAnchor.GetPoint(chartControl, val, chartScale, true).X - point.X);
		double num2 = priceLevel.Value / 100.0;
		double num3 = point.X + num2 * num;
		double num4 = chartControl.GetXByTime(values[0].Time);
		return (condition - 2) switch
		{
			3 => num3 < num4, 
			4 => num3 <= num4, 
			0 => MathExtentions.ApproxCompare(num3, num4) == 0, 
			1 => num3 > num4, 
			2 => num3 >= num4, 
			_ => false, 
		};
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		DateTime dateTime = Globals.MaxDate;
		DateTime dateTime2 = Globals.MinDate;
		double num = point2.X - point.X;
		foreach (PriceLevel item in base.PriceLevels.Where((PriceLevel p) => p.IsVisible))
		{
			double num2 = item.Value / 100.0;
			double num3 = point.X + num2 * num;
			DateTime timeByX = chartControl.GetTimeByX((int)num3);
			if (timeByX >= firstTimeOnChart && timeByX <= lastTimeOnChart)
			{
				return true;
			}
			if (timeByX < dateTime)
			{
				dateTime = timeByX;
			}
			if (timeByX > dateTime2)
			{
				dateTime2 = timeByX;
			}
		}
		if (dateTime <= firstTimeOnChart)
		{
			return dateTime2 >= lastTimeOnChart;
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

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Invalid comparison between Unknown and I4
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
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			base.AnchorLineStroke = new Stroke((Brush)Brushes.DarkGray, (DashStyleHelper)0, 1f, 50);
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolFibonacciTimeExtensions;
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
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			if (base.PriceLevels.Count == 0)
			{
				base.PriceLevels.Add(new PriceLevel(0.0, Brushes.DarkGray));
				base.PriceLevels.Add(new PriceLevel(38.2, Brushes.DodgerBlue));
				base.PriceLevels.Add(new PriceLevel(61.8, Brushes.CornflowerBlue));
				base.PriceLevels.Add(new PriceLevel(100.0, Brushes.SteelBlue));
				base.PriceLevels.Add(new PriceLevel(138.2, Brushes.DarkCyan));
				base.PriceLevels.Add(new PriceLevel(161.8, Brushes.SeaGreen));
				base.PriceLevels.Add(new PriceLevel(200.0, Brushes.DarkGray));
			}
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Expected O, but got Unknown
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		if (((DrawingTool)this).Anchors.All((ChartAnchor a) => a.IsEditing))
		{
			return;
		}
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = base.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = base.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		base.AnchorLineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		double num = ((MathExtentions.ApproxCompare((double)base.AnchorLineStroke.Width % 2.0, 0.0) == 0) ? 0.5 : 0.0);
		Vector vector = new Vector(num, num);
		Brush val2 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : base.AnchorLineStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point + vector), DxExtensions.ToVector2(point2 + vector), val2, base.AnchorLineStroke.Width, base.AnchorLineStroke.StrokeStyle);
		if (base.PriceLevels == null || !base.PriceLevels.Any())
		{
			return;
		}
		SetAllPriceLevelsRenderTarget();
		Stroke val3 = null;
		Vector2 val4 = default(Vector2);
		((Vector2)(ref val4))._002Ector(0f, 0f);
		double num2 = point2.X - point.X;
		Vector2 val5 = default(Vector2);
		Vector2 val6 = default(Vector2);
		RectangleF val7 = default(RectangleF);
		foreach (PriceLevel item in from pl in base.PriceLevels
			where pl.IsVisible && pl.Stroke != null
			orderby pl.Value
			select pl)
		{
			double num3 = item.Value / 100.0;
			double num4 = point.X + num3 * num2;
			double num5 = ((MathExtentions.ApproxCompare((double)item.Stroke.Width % 2.0, 0.0) == 0) ? 0.5 : 0.0);
			((Vector2)(ref val5))._002Ector((float)(num4 + num5), (float)val.Y);
			((Vector2)(ref val6))._002Ector((float)(num4 + num5), (float)(val.Y + val.H));
			((ChartObject)this).RenderTarget.DrawLine(val5, val6, item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);
			if (!((ChartObject)this).IsInHitTest)
			{
				if (val3 == null)
				{
					val3 = new Stroke();
				}
				else
				{
					((RectangleF)(ref val7))._002Ector(val4.X, val4.Y, val6.X - val4.X, val6.Y - val4.Y);
					((ChartObject)this).RenderTarget.FillRectangle(val7, val3.BrushDX);
				}
				val4 = val5;
				item.Stroke.CopyTo(val3);
				val3.Opacity = base.PriceLevelOpacity;
			}
		}
		if (((ChartObject)this).IsInHitTest)
		{
			return;
		}
		foreach (PriceLevel item2 in from pl in base.PriceLevels
			where pl.IsVisible && pl.Stroke != null
			orderby pl.Value
			select pl)
		{
			double num6 = item2.Value / 100.0;
			double num7 = point.X + num6 * num2;
			double num8 = ((MathExtentions.ApproxCompare((double)item2.Stroke.Width % 2.0, 0.0) == 0) ? 0.5 : 0.0);
			DrawPriceLevelText(num7 + num8, item2, val);
		}
	}
}

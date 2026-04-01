using System;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Square IDrawingTool.
/// </summary>
public class Square : ChartMarker
{
	public override object Icon => Icons.DrawSquare;

	protected void DrawSquare(float width, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		AreaDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
		OutlineDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = base.Anchor.GetPoint(chartControl, val, chartScale, true);
		float num = (float)(point.X - (double)(width / 2f));
		float num2 = (float)(point.Y - (double)(width / 2f));
		Brush val2 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : AreaDeviceBrush.BrushDX);
		if (val2 != null)
		{
			((ChartObject)this).RenderTarget.FillRectangle(new RectangleF(num, num2, width, width), val2);
		}
		val2 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : OutlineDeviceBrush.BrushDX);
		if (val2 != null)
		{
			((ChartObject)this).RenderTarget.DrawRectangle(new RectangleF(num, num2, width, width), val2);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			base.Anchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchor,
				IsEditing = true
			};
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolsChartSquareMarkerName;
			base.AreaBrush = Brushes.Crimson;
			base.OutlineBrush = Brushes.DarkGray;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		if (!base.Anchor.IsEditing)
		{
			float width = Math.Max((float)base.BarWidth * 2f, ChartMarker.MinimumSize * 2f) * GetSizeMultiplier();
			DrawSquare(width, chartControl, chartScale);
		}
	}
}

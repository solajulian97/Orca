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
/// Represents an interface that exposes information regarding a Dot IDrawingTool.
/// </summary>
public class Dot : ChartMarker
{
	public override object Icon => Icons.DrawDot;

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
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolsChartDotMarkerName;
			base.AreaBrush = Brushes.DodgerBlue;
			base.OutlineBrush = Brushes.DarkGray;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		if (!base.Anchor.IsEditing)
		{
			ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
			Point point = base.Anchor.GetPoint(chartControl, val, chartScale, true);
			AreaDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			OutlineDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
			float num = Math.Max((float)base.BarWidth, ChartMarker.MinimumSize) * GetSizeMultiplier();
			Brush val2 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : AreaDeviceBrush.BrushDX);
			if (val2 != null)
			{
				((ChartObject)this).RenderTarget.FillEllipse(new Ellipse(new Vector2((float)point.X, (float)point.Y), num, num), val2);
			}
			val2 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : OutlineDeviceBrush.BrushDX);
			if (val2 != null)
			{
				((ChartObject)this).RenderTarget.DrawEllipse(new Ellipse(new Vector2((float)point.X, (float)point.Y), num, num), val2);
			}
		}
	}
}

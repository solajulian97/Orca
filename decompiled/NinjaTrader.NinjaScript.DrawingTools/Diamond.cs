using System;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Diamond IDrawingTool.
/// </summary>
public class Diamond : Square
{
	public override object Icon => Icons.DrawDiamond;

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
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolsChartDiamondMarkerName;
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
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		if (!base.Anchor.IsEditing)
		{
			ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
			Point point = base.Anchor.GetPoint(chartControl, val, chartScale, true);
			((ChartObject)this).RenderTarget.Transform = Matrix3x2.Rotation(MathHelper.DegreesToRadians(45f), DxExtensions.ToVector2(point));
			AreaDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			OutlineDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			float width = (float)Math.Sqrt(Math.Pow(Math.Max((float)base.BarWidth * 2f, ChartMarker.MinimumSize * 2f), 2.0) * 0.5) * GetSizeMultiplier();
			DrawSquare(width, chartControl, chartScale);
			((ChartObject)this).RenderTarget.Transform = Matrix3x2.Identity;
		}
	}
}

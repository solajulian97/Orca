using System;
using System.ComponentModel;
using System.Windows;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class ArrowMarkerBase : ChartMarker
{
	[XmlIgnore]
	[Browsable(false)]
	protected bool IsUpArrow { get; set; }

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		if (base.Anchor.IsEditing)
		{
			return Array.Empty<Point>();
		}
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = base.Anchor.GetPoint(chartControl, val, chartScale, true);
		return new Point[1]
		{
			new Point(point.X, point.Y)
		};
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState == 3 && !((DrawingTool)this).IsLocked)
		{
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			base.Anchor.UpdateFromPoint(new Point(point.X, point.Y), chartControl, chartScale);
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		if (!base.Anchor.IsEditing)
		{
			AreaDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			OutlineDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
			Vector2 val2 = DxExtensions.ToVector2(base.Anchor.GetPoint(chartControl, val, chartScale, true));
			Matrix3x2 transform = (IsUpArrow ? Matrix3x2.Translation(val2) : (Matrix3x2.Rotation(MathHelper.DegreesToRadians(180f), Vector2.Zero) * Matrix3x2.Translation(val2)));
			((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
			((ChartObject)this).RenderTarget.Transform = transform;
			float num = Math.Max((float)base.BarWidth, ChartMarker.MinimumSize) * GetSizeMultiplier();
			float num2 = num * 3f;
			float num3 = num;
			float num4 = num / 3f;
			PathGeometry val3 = new PathGeometry(Globals.D2DFactory);
			GeometrySink obj = val3.Open();
			((SimplifiedGeometrySink)obj).BeginFigure(Vector2.Zero, (FigureBegin)0);
			obj.AddLine(new Vector2(num, num3));
			obj.AddLine(new Vector2(num4, num3));
			obj.AddLine(new Vector2(num4, num2));
			obj.AddLine(new Vector2(0f - num4, num2));
			obj.AddLine(new Vector2(0f - num4, num3));
			obj.AddLine(new Vector2(0f - num, num3));
			((SimplifiedGeometrySink)obj).EndFigure((FigureEnd)1);
			((SimplifiedGeometrySink)obj).Close();
			Brush val4 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : AreaDeviceBrush.BrushDX);
			if (val4 != null)
			{
				((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val3, val4);
			}
			val4 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : OutlineDeviceBrush.BrushDX);
			if (val4 != null)
			{
				((ChartObject)this).RenderTarget.DrawGeometry((Geometry)(object)val3, val4);
			}
			((DisposeBase)val3).Dispose();
			((ChartObject)this).RenderTarget.Transform = Matrix3x2.Identity;
		}
	}
}

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class TriangleBase : ChartMarker
{
	[XmlIgnore]
	[Browsable(false)]
	public bool IsUpTriangle { get; protected set; }

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		if (!base.Anchor.IsEditing)
		{
			AreaDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			OutlineDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
			Vector2 val2 = DxExtensions.ToVector2(base.Anchor.GetPoint(chartControl, val, chartScale, true));
			Matrix3x2 transform = ((!IsUpTriangle) ? (Matrix3x2.Rotation(MathHelper.DegreesToRadians(180f), Vector2.Zero) * Matrix3x2.Translation(val2)) : Matrix3x2.Translation(val2));
			((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
			((ChartObject)this).RenderTarget.Transform = transform;
			PathGeometry val3 = new PathGeometry(Globals.D2DFactory);
			GeometrySink obj = val3.Open();
			float num = Math.Max((float)base.BarWidth, ChartMarker.MinimumSize) * GetSizeMultiplier();
			((SimplifiedGeometrySink)obj).BeginFigure(Vector2.Zero, (FigureBegin)0);
			obj.AddLine(new Vector2(num, num));
			obj.AddLine(new Vector2(0f - num, num));
			obj.AddLine(Vector2.Zero);
			((SimplifiedGeometrySink)obj).EndFigure((FigureEnd)1);
			((SimplifiedGeometrySink)obj).Close();
			Brush val4 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : OutlineDeviceBrush.BrushDX);
			if (val4 != null)
			{
				((ChartObject)this).RenderTarget.DrawGeometry((Geometry)(object)val3, val4);
			}
			val4 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : AreaDeviceBrush.BrushDX);
			if (val4 != null)
			{
				((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val3, val4);
			}
			((DisposeBase)val3).Dispose();
			((ChartObject)this).RenderTarget.Transform = Matrix3x2.Identity;
		}
	}
}

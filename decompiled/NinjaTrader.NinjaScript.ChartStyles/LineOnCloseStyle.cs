using System;
using System.Windows.Media;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.ChartStyles;

public class LineOnCloseStyle : ChartStyle
{
	private object icon;

	public override object Icon => icon ?? (icon = Icons.ChartLineOnClose);

	public override int GetBarPaintWidth(int barWidth)
	{
		return 1 + 2 * (barWidth - 1) + 2 * barWidth;
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		Bars bars = chartBars.Bars;
		if (chartBars.FromIndex > 0)
		{
			int fromIndex = chartBars.FromIndex;
			chartBars.FromIndex = fromIndex - 1;
		}
		PathGeometry val = new PathGeometry(Globals.D2DFactory);
		GeometrySink val2 = val.Open();
		((SimplifiedGeometrySink)val2).BeginFigure(new Vector2((float)chartControl.GetXByBarIndex(chartBars, (chartBars.FromIndex > -1) ? chartBars.FromIndex : 0), (float)chartScale.GetYByValue(bars.GetClose((chartBars.FromIndex > -1) ? chartBars.FromIndex : 0))), (FigureBegin)0);
		for (int i = chartBars.FromIndex + 1; i <= chartBars.ToIndex; i++)
		{
			double close = bars.GetClose(i);
			float num = chartScale.GetYByValue(close);
			float num2 = chartControl.GetXByBarIndex(chartBars, i);
			val2.AddLine(new Vector2(num2, num));
		}
		((SimplifiedGeometrySink)val2).EndFigure((FigureEnd)0);
		((SimplifiedGeometrySink)val2).Close();
		AntialiasMode antialiasMode = ((ChartStyle)this).RenderTarget.AntialiasMode;
		((ChartStyle)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		((ChartStyle)this).RenderTarget.DrawGeometry((Geometry)(object)val, ((ChartStyle)this).UpBrushDX, (float)Math.Max(1.0, chartBars.Properties.ChartStyle.BarWidth));
		((ChartStyle)this).RenderTarget.AntialiasMode = antialiasMode;
		((DisposeBase)val).Dispose();
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptChartStyleLineOnClose;
			((ChartStyle)this).ChartStyleType = (ChartStyleType)2;
			((ChartStyle)this).UpBrush = Brushes.DimGray;
			((ChartStyle)this).BarWidth = 1.0;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("DownBrush", ignoreCase: true));
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("Stroke", ignoreCase: true));
			((ChartStyle)this).Properties.Remove(((ChartStyle)this).Properties.Find("Stroke2", ignoreCase: true));
			((ChartStyle)this).SetPropertyName("BarWidth", Resource.NinjaScriptChartStyleBarWidth);
			((ChartStyle)this).SetPropertyName("UpBrush", Resource.NinjaScriptChartStyleLineOnCloseColor);
		}
	}
}

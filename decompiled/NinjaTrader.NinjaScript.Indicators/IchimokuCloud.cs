using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.Indicators;

public class IchimokuCloud : Indicator
{
	private Series<double> baseLine;

	private Brush bearishBrushDx;

	private Brush bullishBrushDx;

	private const float CloudOpacity = 0.3f;

	private Series<double> conversionLine;

	private Series<double> laggingLine;

	private Series<double> leadingSpanA;

	private Series<double> leadingSpanB;

	private int normalDisplacement;

	private List<Point> selectionPoints = new List<Point>();

	private int totalLag;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(Name = "Conversion (Tenkan) period", Order = 1, GroupName = "Parameters")]
	public int ConversionPeriod { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(Name = "Base (Kijun) period", Order = 2, GroupName = "Parameters")]
	public int BasePeriod { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(Name = "Leading (Senkou) span B period", Order = 3, GroupName = "Parameters")]
	public int LeadingSpanBPeriod { get; set; }

	[Range(int.MinValue, -1)]
	[NinjaScriptProperty]
	[Display(Name = "Span displacement", Order = 4, GroupName = "Parameters")]
	public int SpanDisplacement { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(Name = "Lagging (Chikou) displacement", Order = 5, GroupName = "Parameters")]
	public int LaggingDisplacement { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Invalid comparison between Unknown and I4
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionIchimokuCloud;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameIchimokuCloud;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((NinjaScriptBase)this).MaximumBarsLookBack = (MaximumBarsLookBack)1;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			BasePeriod = 26;
			ConversionPeriod = 9;
			LaggingDisplacement = 26;
			LeadingSpanBPeriod = 52;
			SpanDisplacement = -26;
			((NinjaScriptBase)this).Displacement = Math.Abs(SpanDisplacement);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Blue, "Conversion (Tenkan)");
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Red, "Base (Kijun)");
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.LimeGreen, "Leading (Senkou) span A");
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Red, "Leading (Senkou) span B");
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Green, "Lagging (Chikou)");
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			baseLine = new Series<double>((NinjaScriptBase)(object)this);
			conversionLine = new Series<double>((NinjaScriptBase)(object)this);
			laggingLine = new Series<double>((NinjaScriptBase)(object)this);
			leadingSpanA = new Series<double>((NinjaScriptBase)(object)this);
			leadingSpanB = new Series<double>((NinjaScriptBase)(object)this);
			((NinjaScriptBase)this).Displacement = Math.Abs(SpanDisplacement);
			normalDisplacement = Math.Abs(SpanDisplacement);
			totalLag = ((NinjaScriptBase)this).Displacement + LaggingDisplacement;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			Brush obj = bullishBrushDx;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			Brush obj2 = bearishBrushDx;
			if (obj2 != null)
			{
				((DisposeBase)obj2).Dispose();
			}
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar > Math.Max(Math.Max(ConversionPeriod, BasePeriod), LeadingSpanBPeriod) + totalLag)
		{
			baseLine[0] = (((NinjaScriptBase)MAX(((NinjaScriptBase)this).High, BasePeriod))[0] + ((NinjaScriptBase)MIN(((NinjaScriptBase)this).Low, BasePeriod))[0]) / 2.0;
			conversionLine[0] = (((NinjaScriptBase)MAX(((NinjaScriptBase)this).High, ConversionPeriod))[0] + ((NinjaScriptBase)MIN(((NinjaScriptBase)this).Low, ConversionPeriod))[0]) / 2.0;
			laggingLine[0] = ((NinjaScriptBase)this).Close[0];
			leadingSpanA[0] = (conversionLine[0] + baseLine[0]) / 2.0;
			leadingSpanB[0] = (((NinjaScriptBase)MAX(((NinjaScriptBase)this).High, LeadingSpanBPeriod))[0] + ((NinjaScriptBase)MIN(((NinjaScriptBase)this).Low, LeadingSpanBPeriod))[0]) / 2.0;
			((NinjaScriptBase)this).Values[0][normalDisplacement] = conversionLine[0];
			((NinjaScriptBase)this).Values[1][normalDisplacement] = baseLine[0];
			((NinjaScriptBase)this).Values[2][0] = leadingSpanA[0];
			((NinjaScriptBase)this).Values[3][0] = leadingSpanB[0];
			((NinjaScriptBase)this).Values[4][totalLag] = laggingLine[0];
		}
	}

	protected override Point[] OnGetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		if (((IndicatorRenderBase)this).IsSelected)
		{
			return selectionPoints.ToArray();
		}
		return Array.Empty<Point>();
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		if (((IndicatorRenderBase)this).ChartBars == null || ((IndicatorRenderBase)this).ChartBars.Count < 2 || ((NinjaScriptBase)this).Values[2].Count == 0 || ((NinjaScriptBase)this).CurrentBar <= Math.Max(Math.Max(ConversionPeriod, BasePeriod), LeadingSpanBPeriod) + totalLag)
		{
			return;
		}
		int num = Math.Abs(SpanDisplacement);
		int fromIndex = ((IndicatorRenderBase)this).ChartBars.FromIndex;
		int num2 = ((IndicatorRenderBase)this).ChartBars.ToIndex + num;
		if (fromIndex < 0 || num2 < 0)
		{
			return;
		}
		List<float> spanA = new List<float>();
		List<float> spanB = new List<float>();
		List<int> barList = new List<int>();
		for (int i = fromIndex; i <= num2; i++)
		{
			int num3 = i - num;
			if (num3 >= 0 && num3 < ((NinjaScriptBase)this).CurrentBar)
			{
				double valueAt = ((NinjaScriptBase)this).Values[2].GetValueAt(num3);
				double valueAt2 = ((NinjaScriptBase)this).Values[3].GetValueAt(num3);
				if (!double.IsNaN(valueAt) && !double.IsNaN(valueAt2))
				{
					spanA.Add((float)valueAt);
					spanB.Add((float)valueAt2);
					barList.Add(i);
				}
			}
		}
		if (spanA.Count < 2)
		{
			return;
		}
		bool flag = spanA[0] >= spanB[0];
		int startIdx = 0;
		selectionPoints = new List<Point>();
		for (int j = 1; j < spanA.Count; j++)
		{
			bool flag2 = spanA[j] >= spanB[j];
			if (flag2 != flag)
			{
				DrawSection(startIdx, j - 1, flag);
				startIdx = j - 1;
				flag = flag2;
			}
		}
		DrawSection(startIdx, spanA.Count - 1, flag);
		void DrawSection(int num4, int endIdx, bool bullish)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected O, but got Unknown
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0148: Unknown result type (might be due to invalid IL or missing references)
			if (endIdx <= num4)
			{
				return;
			}
			PathGeometry val = new PathGeometry(Globals.D2DFactory);
			try
			{
				GeometrySink val2 = val.Open();
				try
				{
					int xByBarIndex = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, barList[num4]);
					int yByValue = chartScale.GetYByValue((double)spanA[num4]);
					((SimplifiedGeometrySink)val2).BeginFigure(DxExtensions.ToVector2(new Point(xByBarIndex, yByValue)), (FigureBegin)0);
					selectionPoints.Add(new Point(xByBarIndex, yByValue));
					for (int k = num4 + 1; k <= endIdx; k++)
					{
						int xByBarIndex2 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, barList[k]);
						int yByValue2 = chartScale.GetYByValue((double)spanA[k]);
						val2.AddLine(DxExtensions.ToVector2(new Point(xByBarIndex2, yByValue2)));
						selectionPoints.Add(new Point(xByBarIndex2, yByValue2));
					}
					for (int num5 = endIdx; num5 >= num4; num5--)
					{
						int xByBarIndex3 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, barList[num5]);
						int yByValue3 = chartScale.GetYByValue((double)spanB[num5]);
						val2.AddLine(DxExtensions.ToVector2(new Point(xByBarIndex3, yByValue3)));
						selectionPoints.Add(new Point(xByBarIndex3, yByValue3));
					}
					((SimplifiedGeometrySink)val2).EndFigure((FigureEnd)1);
					((SimplifiedGeometrySink)val2).Close();
					((IndicatorRenderBase)this).RenderTarget.FillGeometry((Geometry)(object)val, bullish ? bullishBrushDx : bearishBrushDx);
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	public override void OnRenderTargetChanged()
	{
		Brush obj = bullishBrushDx;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		Brush obj2 = bearishBrushDx;
		if (obj2 != null)
		{
			((DisposeBase)obj2).Dispose();
		}
		if (((IndicatorRenderBase)this).RenderTarget != null)
		{
			bullishBrushDx = DxExtensions.ToDxBrush(((Stroke)((NinjaScriptBase)this).Plots[2]).Brush, ((IndicatorRenderBase)this).RenderTarget, 0.3f);
			bearishBrushDx = DxExtensions.ToDxBrush(((Stroke)((NinjaScriptBase)this).Plots[3]).Brush, ((IndicatorRenderBase)this).RenderTarget, 0.3f);
		}
	}
}

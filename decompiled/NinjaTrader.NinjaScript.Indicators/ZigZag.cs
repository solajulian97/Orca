using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The ZigZag indicator shows trend lines filtering out changes below a defined level.
/// </summary>
public class ZigZag : Indicator
{
	private Series<double> zigZagHighZigZags;

	private Series<double> zigZagLowZigZags;

	private Series<double> zigZagHighSeries;

	private Series<double> zigZagLowSeries;

	private double currentZigZagHigh;

	private double currentZigZagLow;

	private int lastSwingIdx;

	private double lastSwingPrice;

	private int startIndex;

	private int trendDir;

	/// <summary>
	/// Gets the ZigZag high points.
	/// </summary>
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "DeviationType", GroupName = "NinjaScriptParameters", Order = 0)]
	public DeviationType DeviationType { get; set; }

	[Range(0, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "DeviationValue", GroupName = "NinjaScriptParameters", Order = 1)]
	public double DeviationValue { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "UseHighLow", GroupName = "NinjaScriptParameters", Order = 2)]
	public bool UseHighLow { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> ZigZagHigh
	{
		get
		{
			((NinjaScriptBase)this).Update();
			return zigZagHighSeries;
		}
	}

	/// <summary>
	/// Gets the ZigZag low points.
	/// </summary>
	[Browsable(false)]
	[XmlIgnore]
	public Series<double> ZigZagLow
	{
		get
		{
			((NinjaScriptBase)this).Update();
			return zigZagLowSeries;
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Invalid comparison between Unknown and I4
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionZigZag;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameZigZag;
			DeviationType = DeviationType.Points;
			DeviationValue = 0.5;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).PaintPriceMarkers = false;
			UseHighLow = false;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNameZigZag);
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).PaintPriceMarkers = false;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			currentZigZagHigh = 0.0;
			currentZigZagLow = 0.0;
			lastSwingIdx = -1;
			lastSwingPrice = 0.0;
			trendDir = 0;
			startIndex = int.MinValue;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			zigZagHighZigZags = new Series<double>((NinjaScriptBase)(object)this, (MaximumBarsLookBack)1);
			zigZagLowZigZags = new Series<double>((NinjaScriptBase)(object)this, (MaximumBarsLookBack)1);
			zigZagHighSeries = new Series<double>((NinjaScriptBase)(object)this, (MaximumBarsLookBack)1);
			zigZagLowSeries = new Series<double>((NinjaScriptBase)(object)this, (MaximumBarsLookBack)1);
		}
	}

	public int LowBar(int barsAgo, int instance, int lookBackPeriod)
	{
		if (instance < 1)
		{
			throw new Exception(string.Format(Resource.ZigZagLowBarInstanceGreaterEqual, ((object)this).GetType().Name, instance));
		}
		if (barsAgo < 0)
		{
			throw new Exception(string.Format(Resource.ZigZigLowBarBarsAgoGreaterEqual, ((object)this).GetType().Name, barsAgo));
		}
		if (barsAgo >= ((NinjaScriptBase)this).Count)
		{
			throw new Exception(string.Format(Resource.ZigZagLowBarBarsAgoOutOfRange, ((object)this).GetType().Name, ((NinjaScriptBase)this).Count - 1, barsAgo));
		}
		((NinjaScriptBase)this).Update();
		for (int num = ((NinjaScriptBase)this).CurrentBar - barsAgo - 1; num >= ((NinjaScriptBase)this).CurrentBar - barsAgo - 1 - lookBackPeriod; num--)
		{
			if (num < 0)
			{
				return -1;
			}
			if (num < zigZagLowZigZags.Count && zigZagLowZigZags.IsValidDataPointAt(num))
			{
				if (instance == 1)
				{
					return ((NinjaScriptBase)this).CurrentBar - num;
				}
				instance--;
			}
		}
		return -1;
	}

	public int HighBar(int barsAgo, int instance, int lookBackPeriod)
	{
		if (instance < 1)
		{
			throw new Exception(string.Format(Resource.ZigZagHighBarInstanceGreaterEqual, ((object)this).GetType().Name, instance));
		}
		if (barsAgo < 0)
		{
			throw new Exception(string.Format(Resource.ZigZigHighBarBarsAgoGreaterEqual, ((object)this).GetType().Name, barsAgo));
		}
		if (barsAgo >= ((NinjaScriptBase)this).Count)
		{
			throw new Exception(string.Format(Resource.ZigZagHighBarBarsAgoOutOfRange, ((object)this).GetType().Name, ((NinjaScriptBase)this).Count - 1, barsAgo));
		}
		((NinjaScriptBase)this).Update();
		for (int num = ((NinjaScriptBase)this).CurrentBar - barsAgo - 1; num >= ((NinjaScriptBase)this).CurrentBar - barsAgo - 1 - lookBackPeriod; num--)
		{
			if (num < 0)
			{
				return -1;
			}
			if (num < zigZagHighZigZags.Count && zigZagHighZigZags.IsValidDataPointAt(num))
			{
				if (instance <= 1)
				{
					return ((NinjaScriptBase)this).CurrentBar - num;
				}
				instance--;
			}
		}
		return -1;
	}

	protected override void OnBarUpdate()
	{
		//IL_044b: Unknown result type (might be due to invalid IL or missing references)
		if (((NinjaScriptBase)this).CurrentBar < 2)
		{
			zigZagHighSeries[0] = 0.0;
			zigZagLowSeries[0] = 0.0;
			return;
		}
		if (lastSwingPrice == 0.0)
		{
			lastSwingPrice = ((NinjaScriptBase)this).Input[0];
		}
		ISeries<double> val = ((NinjaScriptBase)this).High;
		ISeries<double> val2 = ((NinjaScriptBase)this).Low;
		if (!UseHighLow)
		{
			val = ((NinjaScriptBase)this).Input;
			val2 = ((NinjaScriptBase)this).Input;
		}
		bool flag = MathExtentions.ApproxCompare(val[1], val[0]) >= 0 && MathExtentions.ApproxCompare(val[1], val[2]) >= 0;
		bool flag2 = MathExtentions.ApproxCompare(val2[1], val2[0]) <= 0 && MathExtentions.ApproxCompare(val2[1], val2[2]) <= 0;
		bool flag3 = (DeviationType == DeviationType.Percent && IsPriceGreater(val[1], lastSwingPrice * (1.0 + DeviationValue / 100.0))) || (DeviationType == DeviationType.Points && IsPriceGreater(val[1], lastSwingPrice + DeviationValue));
		bool flag4 = (DeviationType == DeviationType.Percent && IsPriceGreater(lastSwingPrice * (1.0 - DeviationValue / 100.0), val2[1])) || (DeviationType == DeviationType.Points && IsPriceGreater(lastSwingPrice - DeviationValue, val2[1]));
		double num = 0.0;
		bool flag5 = false;
		bool flag6 = false;
		bool flag7 = false;
		bool flag8 = false;
		if (!flag && !flag2)
		{
			zigZagHighSeries[0] = currentZigZagHigh;
			zigZagLowSeries[0] = currentZigZagLow;
			return;
		}
		if (trendDir <= 0 && flag && flag3)
		{
			num = val[1];
			flag5 = true;
			trendDir = 1;
		}
		else if (trendDir >= 0 && flag2 && flag4)
		{
			num = val2[1];
			flag6 = true;
			trendDir = -1;
		}
		else if (trendDir == 1 && flag && IsPriceGreater(val[1], lastSwingPrice))
		{
			num = val[1];
			flag7 = true;
		}
		else if (trendDir == -1 && flag2 && IsPriceGreater(lastSwingPrice, val2[1]))
		{
			num = val2[1];
			flag8 = true;
		}
		if (flag5 || flag6 || flag7 || flag8)
		{
			if (flag7 && lastSwingIdx >= 0)
			{
				zigZagHighZigZags.Reset(((NinjaScriptBase)this).CurrentBar - lastSwingIdx);
				((NinjaScriptBase)this).Value.Reset(((NinjaScriptBase)this).CurrentBar - lastSwingIdx);
			}
			else if (flag8 && lastSwingIdx >= 0)
			{
				zigZagLowZigZags.Reset(((NinjaScriptBase)this).CurrentBar - lastSwingIdx);
				((NinjaScriptBase)this).Value.Reset(((NinjaScriptBase)this).CurrentBar - lastSwingIdx);
			}
			if (flag5 || flag7)
			{
				zigZagHighZigZags[1] = num;
				currentZigZagHigh = num;
				zigZagHighSeries[1] = currentZigZagHigh;
				((NinjaScriptBase)this).Value[1] = currentZigZagHigh;
			}
			else
			{
				zigZagLowZigZags[1] = num;
				currentZigZagLow = num;
				zigZagLowSeries[1] = currentZigZagLow;
				((NinjaScriptBase)this).Value[1] = currentZigZagLow;
			}
			lastSwingIdx = ((NinjaScriptBase)this).CurrentBar - 1;
			lastSwingPrice = num;
		}
		zigZagHighSeries[0] = currentZigZagHigh;
		zigZagLowSeries[0] = currentZigZagLow;
		if (startIndex == int.MinValue && ((zigZagHighZigZags.IsValidDataPoint(1) && Math.Abs(zigZagHighZigZags[1] - zigZagHighZigZags[2]) > double.Epsilon) || (zigZagLowZigZags.IsValidDataPoint(1) && zigZagLowZigZags[1] != zigZagLowZigZags[2])))
		{
			startIndex = ((NinjaScriptBase)this).CurrentBar - (((int)((NinjaScriptBase)this).Calculate != 0) ? 1 : 2);
		}
	}

	private static bool IsPriceGreater(double a, double b)
	{
		return MathExtentions.ApproxCompare(a, b) > 0;
	}

	public override void OnCalculateMinMax()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Invalid comparison between Unknown and I4
		((IndicatorRenderBase)this).MinValue = double.MaxValue;
		((IndicatorRenderBase)this).MaxValue = double.MinValue;
		if (((NinjaScriptBase)this).BarsArray[0] == null || ((IndicatorRenderBase)this).ChartBars == null || startIndex == int.MinValue)
		{
			return;
		}
		for (int i = 0; i < ((NinjaScriptBase)this).Values.Length; i++)
		{
			for (int j = ((IndicatorRenderBase)this).ChartBars.FromIndex - ((NinjaScriptBase)this).Displacement; j <= ((IndicatorRenderBase)this).ChartBars.ToIndex + ((NinjaScriptBase)this).Displacement; j++)
			{
				if (j >= 0 && j <= ((NinjaScriptBase)this).Bars.Count - 1 - (((int)((NinjaScriptBase)this).Calculate == 0) ? 1 : 0))
				{
					if (zigZagHighZigZags.IsValidDataPointAt(j))
					{
						((IndicatorRenderBase)this).MaxValue = Math.Max(((IndicatorRenderBase)this).MaxValue, zigZagHighZigZags.GetValueAt(j));
					}
					if (zigZagLowZigZags.IsValidDataPointAt(j))
					{
						((IndicatorRenderBase)this).MinValue = Math.Min(((IndicatorRenderBase)this).MinValue, zigZagLowZigZags.GetValueAt(j));
					}
				}
			}
		}
	}

	protected override Point[] OnGetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Invalid comparison between Unknown and I4
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Invalid comparison between Unknown and I4
		if (!((IndicatorRenderBase)this).IsSelected || ((NinjaScriptBase)this).Count == 0 || BrushExtensions.IsTransparent(((Stroke)((NinjaScriptBase)this).Plots[0]).Brush) || startIndex == int.MinValue)
		{
			return Array.Empty<Point>();
		}
		List<Point> list = new List<Point>();
		int num = (((int)((NinjaScriptBase)this).Calculate == 0) ? (((IndicatorRenderBase)this).ChartBars.ToIndex - 1) : (((IndicatorRenderBase)this).ChartBars.ToIndex - 2));
		for (int i = Math.Max(0, ((IndicatorRenderBase)this).ChartBars.FromIndex - ((NinjaScriptBase)this).Displacement); i <= Math.Max(num, Math.Min(((NinjaScriptBase)this).Bars.Count - (((int)((NinjaScriptBase)this).Calculate != 0) ? 1 : 2), num - ((NinjaScriptBase)this).Displacement)); i++)
		{
			int num2 = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && i + ((NinjaScriptBase)this).Displacement >= ((IndicatorRenderBase)this).ChartBars.Count)) ? chartControl.GetXByTime(((IndicatorRenderBase)this).ChartBars.GetTimeByBarIdx(chartControl, i + ((NinjaScriptBase)this).Displacement)) : chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, i + ((NinjaScriptBase)this).Displacement));
			if (((NinjaScriptBase)this).Value.IsValidDataPointAt(i))
			{
				list.Add(new Point(num2, chartScale.GetYByValue(((NinjaScriptBase)this).Value.GetValueAt(i))));
			}
		}
		return list.ToArray();
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Invalid comparison between Unknown and I4
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Invalid comparison between Unknown and I4
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Invalid comparison between Unknown and I4
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Invalid comparison between Unknown and I4
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Invalid comparison between Unknown and I4
		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Invalid comparison between Unknown and I4
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Expected O, but got Unknown
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		if (((NinjaScriptBase)this).Bars == null || chartControl == null || startIndex == int.MinValue)
		{
			return;
		}
		((NinjaScriptBase)this).IsValidDataPointAt(((NinjaScriptBase)this).Bars.Count - 1 - (((int)((NinjaScriptBase)this).Calculate == 0) ? 1 : 0));
		int num = 1;
		int num2 = ((IndicatorRenderBase)this).ChartBars.FromIndex - 1;
		while (num2 >= 0 && num2 - ((NinjaScriptBase)this).Displacement >= startIndex && num2 - ((NinjaScriptBase)this).Displacement <= ((NinjaScriptBase)this).Bars.Count - 1 - (((int)((NinjaScriptBase)this).Calculate == 0) ? 1 : 0))
		{
			bool num3 = zigZagHighZigZags.IsValidDataPointAt(num2 - ((NinjaScriptBase)this).Displacement);
			bool flag = zigZagLowZigZags.IsValidDataPointAt(num2 - ((NinjaScriptBase)this).Displacement);
			if (num3 || flag)
			{
				break;
			}
			num++;
			num2--;
		}
		num -= ((((NinjaScriptBase)this).Displacement < 0) ? ((NinjaScriptBase)this).Displacement : (-((NinjaScriptBase)this).Displacement));
		int num4 = 0;
		for (int i = ((IndicatorRenderBase)this).ChartBars.ToIndex; i <= zigZagHighZigZags.Count && i - ((NinjaScriptBase)this).Displacement >= startIndex && i - ((NinjaScriptBase)this).Displacement <= ((NinjaScriptBase)this).Bars.Count - 1 - (((int)((NinjaScriptBase)this).Calculate == 0) ? 1 : 0); i++)
		{
			bool num5 = zigZagHighZigZags.IsValidDataPointAt(i - ((NinjaScriptBase)this).Displacement);
			bool flag2 = zigZagLowZigZags.IsValidDataPointAt(i - ((NinjaScriptBase)this).Displacement);
			if (num5 || flag2)
			{
				break;
			}
			num4++;
		}
		num4 += ((((NinjaScriptBase)this).Displacement < 0) ? (-((NinjaScriptBase)this).Displacement) : ((NinjaScriptBase)this).Displacement);
		int num6 = -1;
		double num7 = -1.0;
		PathGeometry val = null;
		GeometrySink val2 = null;
		for (int j = ((IndicatorRenderBase)this).ChartBars.FromIndex - num; j <= ((IndicatorRenderBase)this).ChartBars.ToIndex + num4; j++)
		{
			if (j < startIndex || j > ((NinjaScriptBase)this).Bars.Count - (((int)((NinjaScriptBase)this).Calculate != 0) ? 1 : 2) || j < Math.Max(((NinjaScriptBase)this).BarsRequiredToPlot - ((NinjaScriptBase)this).Displacement, ((NinjaScriptBase)this).Displacement))
			{
				continue;
			}
			bool flag3 = zigZagHighZigZags.IsValidDataPointAt(j);
			bool flag4 = zigZagLowZigZags.IsValidDataPointAt(j);
			if (!flag3 && !flag4)
			{
				continue;
			}
			double num8 = (flag3 ? zigZagHighZigZags.GetValueAt(j) : zigZagLowZigZags.GetValueAt(j));
			if (num6 >= startIndex)
			{
				float num9 = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && j + ((NinjaScriptBase)this).Displacement >= ((IndicatorRenderBase)this).ChartBars.Count)) ? chartControl.GetXByTime(((IndicatorRenderBase)this).ChartBars.GetTimeByBarIdx(chartControl, j + ((NinjaScriptBase)this).Displacement)) : chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, j + ((NinjaScriptBase)this).Displacement));
				float num10 = chartScale.GetYByValue(num8);
				if (val2 == null)
				{
					float num11 = (((int)chartControl.BarSpacingType == 3 || ((int)chartControl.BarSpacingType == 1 && num6 + ((NinjaScriptBase)this).Displacement >= ((IndicatorRenderBase)this).ChartBars.Count)) ? chartControl.GetXByTime(((IndicatorRenderBase)this).ChartBars.GetTimeByBarIdx(chartControl, num6 + ((NinjaScriptBase)this).Displacement)) : chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, num6 + ((NinjaScriptBase)this).Displacement));
					float num12 = chartScale.GetYByValue(num7);
					val = new PathGeometry(Globals.D2DFactory);
					val2 = val.Open();
					((SimplifiedGeometrySink)val2).BeginFigure(new Vector2(num11, num12), (FigureBegin)1);
				}
				val2.AddLine(new Vector2(num9, num10));
			}
			num6 = j;
			num7 = num8;
		}
		if (val2 != null)
		{
			((SimplifiedGeometrySink)val2).EndFigure((FigureEnd)0);
			((SimplifiedGeometrySink)val2).Close();
		}
		if (val != null)
		{
			AntialiasMode antialiasMode = ((IndicatorRenderBase)this).RenderTarget.AntialiasMode;
			((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
			((IndicatorRenderBase)this).RenderTarget.DrawGeometry((Geometry)(object)val, ((Stroke)((NinjaScriptBase)this).Plots[0]).BrushDX, ((Stroke)((NinjaScriptBase)this).Plots[0]).Width, ((Stroke)((NinjaScriptBase)this).Plots[0]).StrokeStyle);
			((IndicatorRenderBase)this).RenderTarget.AntialiasMode = antialiasMode;
			((DisposeBase)val).Dispose();
			((IndicatorRenderBase)this).RemoveDrawObject("NinjaScriptInfo");
		}
		else
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.ZigZagDeviationValueError, TextPosition.BottomRight);
		}
	}
}

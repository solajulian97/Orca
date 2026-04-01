using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Linear regression is used to calculate a best fit line for the price data. In addition an upper and lower band is added by calculating the standard deviation of prices from the regression line.
/// </summary>
public class RegressionChannel : Indicator
{
	private Series<double> interceptSeries;

	private Series<double> slopeSeries;

	private Series<double> stdDeviationSeries;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Lower => ((NinjaScriptBase)this).Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Middle => ((NinjaScriptBase)this).Values[0];

	[Range(2, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptGeneral", Order = 0)]
	public int Period { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Upper => ((NinjaScriptBase)this).Values[1];

	[Range(1.0, double.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Width", GroupName = "NinjaScriptGeneral", Order = 1)]
	public double Width { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionRegressionChannel;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameRegressionChannel;
			((NinjaScriptBase)this).IsAutoScale = false;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 35;
			Width = 2.0;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkGray, Resource.NinjaScriptIndicatorMiddle);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorUpper);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorLower);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			interceptSeries = new Series<double>((NinjaScriptBase)(object)this);
			slopeSeries = new Series<double>((NinjaScriptBase)(object)this);
			stdDeviationSeries = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = (double)Period * (double)(Period - 1) * 0.5;
		double num2 = num * num - (double)Period * (double)Period * (double)(Period - 1) * (double)(2 * Period - 1) / 6.0;
		double num3 = 0.0;
		double num4 = 0.0;
		int num5 = Math.Min(Period, ((NinjaScriptBase)this).CurrentBar);
		for (int i = 0; i < num5; i++)
		{
			num3 += (double)i * ((NinjaScriptBase)this).Input[i];
			num4 += ((NinjaScriptBase)this).Input[i];
		}
		if (MathExtentions.ApproxCompare(num2, 0.0) != 0 || Period != 0)
		{
			double num6 = ((double)Period * num3 - num * num4) / num2;
			double num7 = (num4 - num6 * num) / (double)Period;
			slopeSeries[0] = num6;
			interceptSeries[0] = num7;
			double num8 = 0.0;
			for (int j = 0; j < num5; j++)
			{
				double num9 = num7 + num6 * (double)(Period - 1 - j);
				double num10 = Math.Abs(((NinjaScriptBase)this).Input[j] - num9);
				num8 += num10;
			}
			double num11 = num8 / (double)Math.Min(((NinjaScriptBase)this).CurrentBar - 1, Period);
			num8 = 0.0;
			for (int k = 0; k < num5; k++)
			{
				double num12 = num7 + num6 * (double)(Period - 1 - k);
				double num13 = Math.Abs(((NinjaScriptBase)this).Input[k] - num12);
				num8 += (num13 - num11) * (num13 - num11);
			}
			double num14 = Math.Sqrt(num8 / (double)Math.Min(((NinjaScriptBase)this).CurrentBar + 1, Period));
			stdDeviationSeries[0] = num14;
			double num15 = num7 + num6 * (double)(Period - 1);
			Middle[0] = ((((NinjaScriptBase)this).CurrentBar == 0) ? ((NinjaScriptBase)this).Input[0] : num15);
			Upper[0] = ((MathExtentions.ApproxCompare(num14, 0.0) == 0 || double.IsInfinity(num14)) ? ((NinjaScriptBase)this).Input[0] : (num15 + num14 * Width));
			Lower[0] = ((MathExtentions.ApproxCompare(num14, 0.0) == 0 || double.IsInfinity(num14)) ? ((NinjaScriptBase)this).Input[0] : (num15 - num14 * Width));
		}
	}

	private int GetXPos(int barsBack)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		return ((IndicatorRenderBase)this).ChartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, Math.Max(0, ((NinjaScriptBase)this).Bars.Count - 1 - barsBack - (((int)((NinjaScriptBase)this).Calculate == 0) ? 1 : 0)));
	}

	private static int GetYPos(double price, ChartScale chartScale)
	{
		return chartScale.GetYByValue(price);
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		if (((NinjaScriptBase)this).Bars != null && ((IndicatorRenderBase)this).ChartControl != null)
		{
			((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
			ChartPanel val = chartControl.ChartPanels[((IndicatorRenderBase)this).ChartPanel.PanelIndex];
			int num = ((NinjaScriptBase)this).BarsArray[0].Count - 1 - (((int)((NinjaScriptBase)this).Calculate == 0) ? 1 : 0);
			double valueAt = interceptSeries.GetValueAt(num);
			double valueAt2 = slopeSeries.GetValueAt(num);
			int num2 = (int)Math.Round(stdDeviationSeries.GetValueAt(num) * Width / (chartScale.MaxValue - chartScale.MinValue) * (double)val.H, 0);
			int xPos = GetXPos(Period - 1 - ((NinjaScriptBase)this).Displacement);
			int yPos = GetYPos(valueAt, chartScale);
			int xPos2 = GetXPos(-((NinjaScriptBase)this).Displacement);
			int yPos2 = GetYPos(valueAt + valueAt2 * (double)(Period - 1), chartScale);
			Vector2 val2 = default(Vector2);
			((Vector2)(ref val2))._002Ector((float)xPos, (float)yPos);
			Vector2 val3 = default(Vector2);
			((Vector2)(ref val3))._002Ector((float)xPos2, (float)yPos2);
			((IndicatorRenderBase)this).RenderTarget.DrawLine(val2, val3, ((Stroke)((NinjaScriptBase)this).Plots[0]).BrushDX, ((Stroke)((NinjaScriptBase)this).Plots[0]).Width, ((Stroke)((NinjaScriptBase)this).Plots[0]).StrokeStyle);
			((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(val2.X, val2.Y - (float)num2), new Vector2(val3.X, val3.Y - (float)num2), ((Stroke)((NinjaScriptBase)this).Plots[1]).BrushDX, ((Stroke)((NinjaScriptBase)this).Plots[1]).Width, ((Stroke)((NinjaScriptBase)this).Plots[1]).StrokeStyle);
			((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(val2.X, val2.Y + (float)num2), new Vector2(val3.X, val3.Y + (float)num2), ((Stroke)((NinjaScriptBase)this).Plots[2]).BrushDX, ((Stroke)((NinjaScriptBase)this).Plots[2]).Width, ((Stroke)((NinjaScriptBase)this).Plots[2]).StrokeStyle);
			((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)1;
		}
	}
}

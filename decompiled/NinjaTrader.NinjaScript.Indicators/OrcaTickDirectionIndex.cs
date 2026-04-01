using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.Indicators;

public class OrcaTickDirectionIndex : Indicator
{
	private double prevLast;

	private double runningTickDelta;

	private int lastPrimaryBarProcessed;

	private List<double> barTickDelta;

	private List<double> barCumTickDelta;

	private List<double> barCumOpen;

	private List<double> barCumHigh;

	private List<double> barCumLow;

	private List<double> barUptickVol;

	private List<double> barDowntickVol;

	private List<double> barUnchangedVol;

	private List<int> barUnchangedCount;

	private List<bool> barHasData;

	private List<bool> barCumFirstTick;

	private Brush dxUpBrush;

	private Brush dxDownBrush;

	private Brush dxUpBorderBrush;

	private Brush dxDownBorderBrush;

	private Brush dxZeroBrush;

	private Brush dxNeutralBrush;

	[Display(Name = "Display Mode", Order = 0, GroupName = "Parameters")]
	public TDIDisplayMode Mode { get; set; }

	[Display(Name = "Reset on Session", Order = 1, GroupName = "Parameters")]
	public bool ResetOnSession { get; set; }

	[XmlIgnore]
	[Display(Name = "Color Up", Order = 1, GroupName = "Visual Parameters")]
	public Brush ColorUp { get; set; }

	[Browsable(false)]
	public string ColorUpSerialize
	{
		get
		{
			return Serialize.BrushToString(ColorUp);
		}
		set
		{
			ColorUp = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Color Down", Order = 2, GroupName = "Visual Parameters")]
	public Brush ColorDown { get; set; }

	[Browsable(false)]
	public string ColorDownSerialize
	{
		get
		{
			return Serialize.BrushToString(ColorDown);
		}
		set
		{
			ColorDown = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Color Up Border", Order = 3, GroupName = "Visual Parameters")]
	public Brush ColorUpBorder { get; set; }

	[Browsable(false)]
	public string ColorUpBorderSerialize
	{
		get
		{
			return Serialize.BrushToString(ColorUpBorder);
		}
		set
		{
			ColorUpBorder = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Color Down Border", Order = 4, GroupName = "Visual Parameters")]
	public Brush ColorDownBorder { get; set; }

	[Browsable(false)]
	public string ColorDownBorderSerialize
	{
		get
		{
			return Serialize.BrushToString(ColorDownBorder);
		}
		set
		{
			ColorDownBorder = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Neutral Color", Order = 5, GroupName = "Visual Parameters")]
	public Brush NeutralColor { get; set; }

	[Browsable(false)]
	public string NeutralColorSerialize
	{
		get
		{
			return Serialize.BrushToString(NeutralColor);
		}
		set
		{
			NeutralColor = Serialize.StringToBrush(value);
		}
	}

	[Range(0.0, 1.0)]
	[Display(Name = "Bar Opacity", Order = 6, GroupName = "Visual Parameters")]
	public double BarOpacity { get; set; }

	[Range(1, 100)]
	[Display(Name = "Bar Width %", Order = 7, GroupName = "Visual Parameters")]
	public int BarWidthPercent { get; set; }

	[XmlIgnore]
	[Display(Name = "Zero Line Color", Order = 1, GroupName = "Reference Levels")]
	public Brush ZeroLineColor { get; set; }

	[Browsable(false)]
	public string ZeroLineColorSerialize
	{
		get
		{
			return Serialize.BrushToString(ZeroLineColor);
		}
		set
		{
			ZeroLineColor = Serialize.StringToBrush(value);
		}
	}

	[Range(1, 5)]
	[Display(Name = "Zero Line Width", Order = 2, GroupName = "Reference Levels")]
	public int ZeroLineWidth { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Invalid comparison between Unknown and I4
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Invalid comparison between Unknown and I4
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = "OrcaTickDirectionIndex";
			((NinjaScript)this).Description = "Tick Direction Index (Rewarded Effort Index) — tracks volume classified by tick direction.";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).BarsRequiredToPlot = 0;
			Mode = TDIDisplayMode.BarHistogram;
			ResetOnSession = true;
			ColorUp = Brushes.DodgerBlue;
			ColorDown = Brushes.Tomato;
			ColorUpBorder = Brushes.DodgerBlue;
			ColorDownBorder = Brushes.Tomato;
			NeutralColor = Brushes.Gray;
			BarOpacity = 0.5;
			BarWidthPercent = 90;
			ZeroLineColor = Brushes.DimGray;
			ZeroLineWidth = 1;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DimGray, 1f), (PlotStyle)6, "TickDelta");
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			barTickDelta = new List<double>(4096);
			barCumTickDelta = new List<double>(4096);
			barCumOpen = new List<double>(4096);
			barCumHigh = new List<double>(4096);
			barCumLow = new List<double>(4096);
			barUptickVol = new List<double>(4096);
			barDowntickVol = new List<double>(4096);
			barUnchangedVol = new List<double>(4096);
			barUnchangedCount = new List<int>(4096);
			barHasData = new List<bool>(4096);
			barCumFirstTick = new List<bool>(4096);
			prevLast = double.NaN;
			runningTickDelta = 0.0;
			lastPrimaryBarProcessed = -1;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			DisposeDxResources();
		}
	}

	private void EnsureBarLists(int idx)
	{
		while (barTickDelta.Count <= idx)
		{
			barTickDelta.Add(0.0);
			barCumTickDelta.Add(0.0);
			barCumOpen.Add(0.0);
			barCumHigh.Add(0.0);
			barCumLow.Add(0.0);
			barUptickVol.Add(0.0);
			barDowntickVol.Add(0.0);
			barUnchangedVol.Add(0.0);
			barUnchangedCount.Add(0);
			barHasData.Add(item: false);
			barCumFirstTick.Add(item: false);
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).BarsInProgress == 1)
		{
			double num = ((NinjaScriptBase)this).Close[0];
			long num2 = (long)((NinjaScriptBase)this).Volume[0];
			if (num2 <= 0)
			{
				return;
			}
			if ((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7)
			{
				num2 = (long)Globals.ToCryptocurrencyVolume(num2);
			}
			int bar = ((NinjaScriptBase)this).BarsArray[0].GetBar(((NinjaScriptBase)this).Time[0]);
			if (bar < 0)
			{
				return;
			}
			EnsureBarLists(bar);
			if (bar != lastPrimaryBarProcessed)
			{
				lastPrimaryBarProcessed = bar;
			}
			long num3 = 0L;
			if (!double.IsNaN(prevLast))
			{
				if (num > prevLast)
				{
					num3 = num2;
				}
				else if (num < prevLast)
				{
					num3 = -num2;
				}
			}
			prevLast = num;
			if (num3 == 0L)
			{
				barUnchangedVol[bar] += num2;
				barUnchangedCount[bar]++;
				barHasData[bar] = true;
				return;
			}
			if (num3 > 0)
			{
				barUptickVol[bar] += num2;
			}
			else
			{
				barDowntickVol[bar] += num2;
			}
			barTickDelta[bar] += num3;
			runningTickDelta += num3;
			barCumTickDelta[bar] = runningTickDelta;
			if (!barCumFirstTick[bar])
			{
				barCumOpen[bar] = runningTickDelta;
				barCumHigh[bar] = runningTickDelta;
				barCumLow[bar] = runningTickDelta;
				barCumFirstTick[bar] = true;
			}
			else
			{
				if (runningTickDelta > barCumHigh[bar])
				{
					barCumHigh[bar] = runningTickDelta;
				}
				if (runningTickDelta < barCumLow[bar])
				{
					barCumLow[bar] = runningTickDelta;
				}
			}
			barHasData[bar] = true;
		}
		else
		{
			if (((NinjaScriptBase)this).BarsInProgress != 0 || ((NinjaScriptBase)this).CurrentBar < 0)
			{
				return;
			}
			EnsureBarLists(((NinjaScriptBase)this).CurrentBar);
			if (ResetOnSession && ((NinjaScriptBase)this).Bars.IsFirstBarOfSession)
			{
				runningTickDelta = 0.0;
				prevLast = double.NaN;
			}
			if (((NinjaScriptBase)this).CurrentBar < barHasData.Count && barHasData[((NinjaScriptBase)this).CurrentBar])
			{
				switch (Mode)
				{
				case TDIDisplayMode.CumulativeLine:
					((NinjaScriptBase)this).Value[0] = barCumTickDelta[((NinjaScriptBase)this).CurrentBar];
					break;
				case TDIDisplayMode.BarHistogram:
					((NinjaScriptBase)this).Value[0] = barTickDelta[((NinjaScriptBase)this).CurrentBar];
					break;
				case TDIDisplayMode.RatioLine:
				{
					double num4 = barUptickVol[((NinjaScriptBase)this).CurrentBar];
					double num5 = barDowntickVol[((NinjaScriptBase)this).CurrentBar];
					double num6 = num4 + num5;
					((NinjaScriptBase)this).Value[0] = ((num6 > 0.0) ? (num4 / num6) : 0.5);
					break;
				}
				}
			}
			else
			{
				((NinjaScriptBase)this).Value[0] = double.NaN;
			}
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0425: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c5: Unknown result type (might be due to invalid IL or missing references)
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		if (chartControl == null || chartScale == null || ((NinjaScriptBase)this).Bars == null || ((IndicatorRenderBase)this).ChartBars == null || barTickDelta == null)
		{
			return;
		}
		int fromIndex = ((IndicatorRenderBase)this).ChartBars.FromIndex;
		int toIndex = ((IndicatorRenderBase)this).ChartBars.ToIndex;
		if (fromIndex < 0 || toIndex < 0 || fromIndex > toIndex)
		{
			return;
		}
		EnsureDxResources();
		if (dxUpBrush == null)
		{
			return;
		}
		AntialiasMode antialiasMode = ((IndicatorRenderBase)this).RenderTarget.AntialiasMode;
		((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)1;
		float num = ((IndicatorRenderBase)this).ChartPanel.X;
		float num2 = ((IndicatorRenderBase)this).ChartPanel.W;
		float num3 = ((IndicatorRenderBase)this).ChartPanel.Y;
		float num4 = ((IndicatorRenderBase)this).ChartPanel.H;
		double num5 = ((Mode == TDIDisplayMode.RatioLine) ? 0.5 : 0.0);
		float num6 = chartScale.GetYByValue(num5);
		if (num6 >= num3 && num6 <= num3 + num4)
		{
			((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num, num6), new Vector2(num + num2, num6), dxZeroBrush, (float)ZeroLineWidth);
		}
		if (Mode == TDIDisplayMode.BarHistogram || Mode == TDIDisplayMode.CumulativeLine)
		{
			RectangleF val4 = default(RectangleF);
			RectangleF val9 = default(RectangleF);
			for (int i = fromIndex; i <= toIndex; i++)
			{
				if (i < 0 || i >= barTickDelta.Count || !barHasData[i])
				{
					continue;
				}
				float num7 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, i);
				float num8 = ((i < toIndex) ? ((float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, i + 1) - num7) : ((i <= fromIndex) ? ((float)chartControl.BarWidth) : (num7 - (float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, i - 1))));
				float num9 = (float)((double)(num8 * (float)BarWidthPercent) / 100.0 / 2.0);
				if (num9 < 1f)
				{
					num9 = 1f;
				}
				if (Mode == TDIDisplayMode.BarHistogram)
				{
					double num10 = barTickDelta[i];
					if (num10 != 0.0)
					{
						bool num11 = num10 > 0.0;
						float val = chartScale.GetYByValue(num10);
						float val2 = chartScale.GetYByValue(0.0);
						Brush val3 = (num11 ? dxUpBrush : dxDownBrush);
						float num12 = Math.Min(val, val2);
						float num13 = Math.Max(val, val2) - num12;
						if (num13 < 1f)
						{
							num13 = 1f;
						}
						((RectangleF)(ref val4))._002Ector(num7 - num9, num12, num9 * 2f, num13);
						((IndicatorRenderBase)this).RenderTarget.FillRectangle(val4, val3);
						((IndicatorRenderBase)this).RenderTarget.DrawRectangle(val4, val3, 1f);
					}
				}
				else if (barCumFirstTick[i])
				{
					double num14 = barCumOpen[i];
					double num15 = barCumHigh[i];
					double num16 = barCumLow[i];
					double num17 = barCumTickDelta[i];
					bool num18 = num17 >= num14;
					float val5 = chartScale.GetYByValue(num14);
					float num19 = chartScale.GetYByValue(num15);
					float num20 = chartScale.GetYByValue(num16);
					float val6 = chartScale.GetYByValue(num17);
					Brush val7 = (num18 ? dxUpBrush : dxDownBrush);
					Brush val8 = (num18 ? dxUpBorderBrush : dxDownBorderBrush);
					float num21 = Math.Min(val5, val6);
					float num22 = Math.Max(val5, val6);
					float num23 = num22 - num21;
					if (num23 < 1f)
					{
						num23 = 1f;
					}
					if (num19 < num21)
					{
						((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num7, num19), new Vector2(num7, num21), val8, 1f);
					}
					if (num20 > num22)
					{
						((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num7, num22), new Vector2(num7, num20), val8, 1f);
					}
					((RectangleF)(ref val9))._002Ector(num7 - num9, num21, num9 * 2f, num23);
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(val9, val7);
					((IndicatorRenderBase)this).RenderTarget.DrawRectangle(val9, val8, 1f);
				}
			}
		}
		((IndicatorRenderBase)this).RenderTarget.AntialiasMode = antialiasMode;
	}

	private void EnsureDxResources()
	{
		if (((IndicatorRenderBase)this).RenderTarget != null && dxUpBrush == null)
		{
			float opacity = (float)Math.Max(0.0, Math.Min(1.0, BarOpacity));
			dxUpBrush = DxExtensions.ToDxBrush(ColorUp, ((IndicatorRenderBase)this).RenderTarget);
			dxUpBrush.Opacity = opacity;
			dxDownBrush = DxExtensions.ToDxBrush(ColorDown, ((IndicatorRenderBase)this).RenderTarget);
			dxDownBrush.Opacity = opacity;
			dxUpBorderBrush = DxExtensions.ToDxBrush(ColorUpBorder, ((IndicatorRenderBase)this).RenderTarget);
			dxDownBorderBrush = DxExtensions.ToDxBrush(ColorDownBorder, ((IndicatorRenderBase)this).RenderTarget);
			dxZeroBrush = DxExtensions.ToDxBrush(ZeroLineColor, ((IndicatorRenderBase)this).RenderTarget);
			dxNeutralBrush = DxExtensions.ToDxBrush(NeutralColor, ((IndicatorRenderBase)this).RenderTarget);
		}
	}

	private void DisposeDxResources()
	{
		if (dxUpBrush != null)
		{
			((DisposeBase)dxUpBrush).Dispose();
			dxUpBrush = null;
		}
		if (dxDownBrush != null)
		{
			((DisposeBase)dxDownBrush).Dispose();
			dxDownBrush = null;
		}
		if (dxUpBorderBrush != null)
		{
			((DisposeBase)dxUpBorderBrush).Dispose();
			dxUpBorderBrush = null;
		}
		if (dxDownBorderBrush != null)
		{
			((DisposeBase)dxDownBorderBrush).Dispose();
			dxDownBorderBrush = null;
		}
		if (dxZeroBrush != null)
		{
			((DisposeBase)dxZeroBrush).Dispose();
			dxZeroBrush = null;
		}
		if (dxNeutralBrush != null)
		{
			((DisposeBase)dxNeutralBrush).Dispose();
			dxNeutralBrush = null;
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDxResources();
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}
}

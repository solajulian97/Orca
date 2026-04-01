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
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators;

public class PassiveFlowSuite : Indicator
{
	private readonly object depthLock = new object();

	private Dictionary<int, long> bidDepthByPos;

	private Dictionary<int, long> askDepthByPos;

	private double prevTotalBidSize;

	private double prevTotalAskSize;

	private bool hasPrevDepthSnapshot;

	private double cumulativeBookDelta;

	private Queue<KeyValuePair<DateTime, double>> obiQueue;

	private double cobiSum;

	private List<double> barBookDelta;

	private List<double> barAbsorption;

	private List<double> barAbsorptionSmoothed;

	private List<double> barCOBI;

	private List<bool> barHasData;

	private double lastBid;

	private double lastAsk;

	private double aggressiveDelta;

	private Queue<double> absorptionBuffer;

	private double absorptionBufSum;

	private Brush dxGreenBrush;

	private Brush dxRedBrush;

	private Brush dxBlueFillBrush;

	private Brush dxOrangeFillBrush;

	private Brush dxZeroBrush;

	private Brush dxSepBrush;

	private Brush dxAbsLineBrush;

	private Brush dxAbsGreenBrush;

	private Brush dxAbsRedBrush;

	private Brush dxLabelBrush;

	private TextFormat dxLabelFormat;

	private Factory dwFactory;

	[Range(1, 10)]
	[Display(Name = "Depth Levels", Order = 1, GroupName = "Parameters", Description = "Number of book levels to track for Cumulative Book Delta (Section 1).")]
	public int DepthLevels { get; set; }

	[Range(1, 10)]
	[Display(Name = "OBI Levels", Order = 2, GroupName = "Parameters", Description = "Number of book levels used for OBI calculation (Section 3).")]
	public int OBILevels { get; set; }

	[Range(1, 100)]
	[Display(Name = "Absorption Period", Order = 3, GroupName = "Parameters", Description = "SMA smoothing period for Absorption Ratio (Section 2).")]
	public int AbsorptionPeriod { get; set; }

	[Range(1, 1440)]
	[Display(Name = "COBI Window (min)", Order = 4, GroupName = "Parameters", Description = "Rolling window in minutes for Cumulative OBI (Section 3).")]
	public int COBIWindowMinutes { get; set; }

	[XmlIgnore]
	[Display(Name = "Histogram Up Color", Order = 1, GroupName = "Visual")]
	public Brush HistogramUpColor { get; set; }

	[Browsable(false)]
	public string HistogramUpColorSerialize
	{
		get
		{
			return Serialize.BrushToString(HistogramUpColor);
		}
		set
		{
			HistogramUpColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Histogram Down Color", Order = 2, GroupName = "Visual")]
	public Brush HistogramDownColor { get; set; }

	[Browsable(false)]
	public string HistogramDownColorSerialize
	{
		get
		{
			return Serialize.BrushToString(HistogramDownColor);
		}
		set
		{
			HistogramDownColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Absorption Up Color", Order = 3, GroupName = "Visual")]
	public Brush AbsorptionUpColor { get; set; }

	[Browsable(false)]
	public string AbsorptionUpColorSerialize
	{
		get
		{
			return Serialize.BrushToString(AbsorptionUpColor);
		}
		set
		{
			AbsorptionUpColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Absorption Down Color", Order = 4, GroupName = "Visual")]
	public Brush AbsorptionDownColor { get; set; }

	[Browsable(false)]
	public string AbsorptionDownColorSerialize
	{
		get
		{
			return Serialize.BrushToString(AbsorptionDownColor);
		}
		set
		{
			AbsorptionDownColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Absorption Line Color", Order = 5, GroupName = "Visual")]
	public Brush AbsorptionLineColor { get; set; }

	[Browsable(false)]
	public string AbsorptionLineColorSerialize
	{
		get
		{
			return Serialize.BrushToString(AbsorptionLineColor);
		}
		set
		{
			AbsorptionLineColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "COBI Bull Color", Order = 6, GroupName = "Visual")]
	public Brush COBIBullColor { get; set; }

	[Browsable(false)]
	public string COBIBullColorSerialize
	{
		get
		{
			return Serialize.BrushToString(COBIBullColor);
		}
		set
		{
			COBIBullColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "COBI Bear Color", Order = 7, GroupName = "Visual")]
	public Brush COBIBearColor { get; set; }

	[Browsable(false)]
	public string COBIBearColorSerialize
	{
		get
		{
			return Serialize.BrushToString(COBIBearColor);
		}
		set
		{
			COBIBearColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Zero Line Color", Order = 8, GroupName = "Visual")]
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

	[XmlIgnore]
	[Display(Name = "Separator Color", Order = 9, GroupName = "Visual")]
	public Brush SeparatorColor { get; set; }

	[Browsable(false)]
	public string SeparatorColorSerialize
	{
		get
		{
			return Serialize.BrushToString(SeparatorColor);
		}
		set
		{
			SeparatorColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Label Color", Order = 10, GroupName = "Visual")]
	public Brush LabelColor { get; set; }

	[Browsable(false)]
	public string LabelColorSerialize
	{
		get
		{
			return Serialize.BrushToString(LabelColor);
		}
		set
		{
			LabelColor = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Invalid comparison between Unknown and I4
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Invalid comparison between Unknown and I4
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = "PassiveFlowSuite";
			((NinjaScript)this).Description = "3-section passive flow indicator: Cumulative Book Delta, Absorption Ratio, and Cumulative OBI.";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).BarsRequiredToPlot = 0;
			DepthLevels = 5;
			OBILevels = 3;
			AbsorptionPeriod = 20;
			COBIWindowMinutes = 120;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Transparent, 0f), (PlotStyle)6, "PassiveFlowData");
			HistogramUpColor = Brushes.LimeGreen;
			HistogramDownColor = Brushes.Crimson;
			AbsorptionUpColor = Brushes.LimeGreen;
			AbsorptionDownColor = Brushes.Crimson;
			AbsorptionLineColor = Brushes.DodgerBlue;
			COBIBullColor = Brushes.DodgerBlue;
			COBIBearColor = Brushes.Orange;
			ZeroLineColor = Brushes.DimGray;
			SeparatorColor = Brushes.Gray;
			LabelColor = Brushes.WhiteSmoke;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			bidDepthByPos = new Dictionary<int, long>();
			askDepthByPos = new Dictionary<int, long>();
			obiQueue = new Queue<KeyValuePair<DateTime, double>>();
			barBookDelta = new List<double>(4096);
			barAbsorption = new List<double>(4096);
			barAbsorptionSmoothed = new List<double>(4096);
			barCOBI = new List<double>(4096);
			barHasData = new List<bool>(4096);
			absorptionBuffer = new Queue<double>();
			absorptionBufSum = 0.0;
			prevTotalBidSize = 0.0;
			prevTotalAskSize = 0.0;
			hasPrevDepthSnapshot = false;
			cumulativeBookDelta = 0.0;
			cobiSum = 0.0;
			aggressiveDelta = 0.0;
			lastBid = double.NaN;
			lastAsk = double.NaN;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			DisposeDxResources();
		}
	}

	protected override void OnMarketDepth(MarketDepthEventArgs e)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Invalid comparison between Unknown and I4
		lock (depthLock)
		{
			Dictionary<int, long> dictionary = (((int)e.MarketDataType == 0) ? askDepthByPos : bidDepthByPos);
			if ((int)e.Operation == 0 || (int)e.Operation == 1)
			{
				dictionary[e.Position] = e.Volume;
			}
			else if ((int)e.Operation == 2)
			{
				dictionary.Remove(e.Position);
			}
			double num = 0.0;
			double num2 = 0.0;
			int num3 = Math.Min(DepthLevels, 10);
			foreach (KeyValuePair<int, long> bidDepthByPo in bidDepthByPos)
			{
				if (bidDepthByPo.Key < num3)
				{
					num += (double)bidDepthByPo.Value;
				}
			}
			foreach (KeyValuePair<int, long> askDepthByPo in askDepthByPos)
			{
				if (askDepthByPo.Key < num3)
				{
					num2 += (double)askDepthByPo.Value;
				}
			}
			if (hasPrevDepthSnapshot)
			{
				double num4 = num - prevTotalBidSize;
				double num5 = num2 - prevTotalAskSize;
				cumulativeBookDelta += num4 - num5;
			}
			else
			{
				hasPrevDepthSnapshot = true;
			}
			prevTotalBidSize = num;
			prevTotalAskSize = num2;
			int num6 = Math.Min(OBILevels, 10);
			double num7 = 0.0;
			double num8 = 0.0;
			foreach (KeyValuePair<int, long> bidDepthByPo2 in bidDepthByPos)
			{
				if (bidDepthByPo2.Key < num6)
				{
					num7 += (double)bidDepthByPo2.Value;
				}
			}
			foreach (KeyValuePair<int, long> askDepthByPo2 in askDepthByPos)
			{
				if (askDepthByPo2.Key < num6)
				{
					num8 += (double)askDepthByPo2.Value;
				}
			}
			double num9 = num7 + num8;
			if (num9 > 0.0)
			{
				double num10 = (num7 - num8) / num9;
				DateTime utcNow = DateTime.UtcNow;
				obiQueue.Enqueue(new KeyValuePair<DateTime, double>(utcNow, num10));
				cobiSum += num10;
				DateTime dateTime = utcNow.AddMinutes(-COBIWindowMinutes);
				while (obiQueue.Count > 0 && obiQueue.Peek().Key < dateTime)
				{
					cobiSum -= obiQueue.Dequeue().Value;
				}
			}
		}
	}

	protected override void OnMarketData(MarketDataEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Invalid comparison between Unknown and I4
		if ((int)e.MarketDataType == 1)
		{
			lastBid = e.Price;
		}
		else if ((int)e.MarketDataType == 0)
		{
			lastAsk = e.Price;
		}
		else
		{
			if ((int)e.MarketDataType != 2)
			{
				return;
			}
			if (e.Ask > 0.0 && !double.IsNaN(e.Ask))
			{
				lastAsk = e.Ask;
			}
			if (e.Bid > 0.0 && !double.IsNaN(e.Bid))
			{
				lastBid = e.Bid;
			}
			long num = e.Volume;
			if ((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7)
			{
				num = (long)Globals.ToCryptocurrencyVolume(num);
			}
			if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid))
			{
				if (e.Price >= lastAsk)
				{
					aggressiveDelta += num;
				}
				else if (e.Price <= lastBid)
				{
					aggressiveDelta -= num;
				}
			}
		}
	}

	private void EnsureBarLists(int idx)
	{
		while (barBookDelta.Count <= idx)
		{
			barBookDelta.Add(0.0);
			barAbsorption.Add(0.0);
			barAbsorptionSmoothed.Add(0.0);
			barCOBI.Add(0.0);
			barHasData.Add(item: false);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).BarsInProgress != 0 || ((NinjaScriptBase)this).CurrentBar < 0)
		{
			return;
		}
		EnsureBarLists(((NinjaScriptBase)this).CurrentBar);
		if (((NinjaScriptBase)this).Bars.IsFirstBarOfSession)
		{
			lock (depthLock)
			{
				cumulativeBookDelta = 0.0;
				hasPrevDepthSnapshot = false;
			}
			aggressiveDelta = 0.0;
			absorptionBuffer.Clear();
			absorptionBufSum = 0.0;
		}
		double value;
		double value2;
		lock (depthLock)
		{
			value = cumulativeBookDelta;
			value2 = cobiSum;
		}
		barBookDelta[((NinjaScriptBase)this).CurrentBar] = value;
		double num = ((((NinjaScriptBase)this).TickSize > 0.0) ? (Math.Abs(((NinjaScriptBase)this).Close[0] - ((NinjaScriptBase)this).Open[0]) / ((NinjaScriptBase)this).TickSize) : 0.0);
		double num2 = ((num > 0.0) ? (Math.Abs(aggressiveDelta) / num) : 0.0);
		absorptionBuffer.Enqueue(num2);
		absorptionBufSum += num2;
		while (absorptionBuffer.Count > AbsorptionPeriod)
		{
			absorptionBufSum -= absorptionBuffer.Dequeue();
		}
		double value3 = ((absorptionBuffer.Count > 0) ? (absorptionBufSum / (double)absorptionBuffer.Count) : 0.0);
		barAbsorption[((NinjaScriptBase)this).CurrentBar] = num2;
		barAbsorptionSmoothed[((NinjaScriptBase)this).CurrentBar] = value3;
		barCOBI[((NinjaScriptBase)this).CurrentBar] = value2;
		barHasData[((NinjaScriptBase)this).CurrentBar] = true;
		((NinjaScriptBase)this).Value[0] = 0.0;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_067a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0685: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_08df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0618: Unknown result type (might be due to invalid IL or missing references)
		//IL_0621: Unknown result type (might be due to invalid IL or missing references)
		//IL_0799: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ba: Unknown result type (might be due to invalid IL or missing references)
		if (chartControl == null || chartScale == null || ((NinjaScriptBase)this).Bars == null || ((IndicatorRenderBase)this).ChartBars == null)
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
		if (dxGreenBrush == null)
		{
			return;
		}
		AntialiasMode antialiasMode = ((IndicatorRenderBase)this).RenderTarget.AntialiasMode;
		((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)1;
		float num = ((IndicatorRenderBase)this).ChartPanel.X;
		float num2 = ((IndicatorRenderBase)this).ChartPanel.W;
		float num3 = ((IndicatorRenderBase)this).ChartPanel.Y;
		float num4 = ((IndicatorRenderBase)this).ChartPanel.H;
		float num5 = (num4 - 2f) / 3f;
		float num6 = num3;
		float num7 = num6 + num5;
		float num8 = num7 + 1f;
		float num9 = num8 + num5;
		float num10 = num9 + 1f;
		float num11 = num3 + num4;
		((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num, num7), new Vector2(num + num2, num7), dxSepBrush, 1f);
		((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num, num9), new Vector2(num + num2, num9), dxSepBrush, 1f);
		double num12 = 0.0;
		double num13 = 0.0;
		double num14 = double.MaxValue;
		double num15 = double.MinValue;
		double num16 = 0.0;
		double num17 = 0.0;
		for (int i = fromIndex; i <= toIndex; i++)
		{
			if (i >= 0 && i < barBookDelta.Count && barHasData[i])
			{
				double num18 = barBookDelta[i];
				if (num18 < num12)
				{
					num12 = num18;
				}
				if (num18 > num13)
				{
					num13 = num18;
				}
				double num19 = barAbsorptionSmoothed[i];
				if (num19 < num14)
				{
					num14 = num19;
				}
				if (num19 > num15)
				{
					num15 = num19;
				}
				double num20 = barCOBI[i];
				if (num20 < num16)
				{
					num16 = num20;
				}
				if (num20 > num17)
				{
					num17 = num20;
				}
			}
		}
		double num21 = Math.Max(1.0, (num13 - num12) * 0.1);
		num12 -= num21;
		num13 += num21;
		if (num13 == num12)
		{
			num13 = 1.0;
			num12 = -1.0;
		}
		if (num14 == double.MaxValue)
		{
			num14 = 0.0;
			num15 = 1.0;
		}
		double num22 = Math.Max(0.1, (num15 - num14) * 0.1);
		num14 -= num22;
		num15 += num22;
		if (num15 == num14)
		{
			num15 = num14 + 1.0;
		}
		double num23 = Math.Max(0.1, (num17 - num16) * 0.1);
		num16 -= num23;
		num17 += num23;
		if (num17 == num16)
		{
			num17 = 1.0;
			num16 = -1.0;
		}
		DrawLabel("Cumulative Book Delta", num + 5f, num6 + 2f);
		DrawLabel("Absorption Ratio (" + AbsorptionPeriod + ")", num + 5f, num8 + 2f);
		DrawLabel("Cumulative OBI (" + COBIWindowMinutes + "m Rolling)", num + 5f, num10 + 2f);
		float num24 = MapY(0.0, num12, num13, num6, num7);
		if (num24 >= num6 && num24 <= num7)
		{
			((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num, num24), new Vector2(num + num2, num24), dxZeroBrush, 1f);
		}
		RectangleF val2 = default(RectangleF);
		for (int j = fromIndex; j <= toIndex; j++)
		{
			if (j >= 0 && j < barBookDelta.Count && barHasData[j])
			{
				double num25 = barBookDelta[j];
				float num26 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, j);
				float val = MapY(num25, num12, num13, num6, num7);
				float num27 = (float)((double)GetBarSpacing(chartControl, j, fromIndex, toIndex) * 0.8 / 2.0);
				if (num27 < 1f)
				{
					num27 = 1f;
				}
				float num28 = Math.Min(num24, val);
				float num29 = Math.Max(num24, val);
				_ = num29 - num28;
				_ = 1f;
				num28 = Math.Max(num28, num6);
				num29 = Math.Min(num29, num7);
				if (!(num29 <= num28))
				{
					((RectangleF)(ref val2))._002Ector(num26 - num27, num28, num27 * 2f, num29 - num28);
					Brush val3 = ((num25 >= 0.0) ? dxGreenBrush : dxRedBrush);
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(val2, val3);
				}
			}
		}
		MapY(0.0, num14, num15, num8, num9);
		((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		for (int k = fromIndex + 1; k <= toIndex; k++)
		{
			if (k >= 1 && k < barAbsorptionSmoothed.Count && barHasData[k] && barHasData[k - 1])
			{
				float num30 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, k - 1);
				float num31 = ClampY(MapY(barAbsorptionSmoothed[k - 1], num14, num15, num8, num9), num8, num9);
				float num32 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, k);
				float num33 = ClampY(MapY(barAbsorptionSmoothed[k], num14, num15, num8, num9), num8, num9);
				Brush val4 = ((barAbsorption[k] >= barAbsorptionSmoothed[k]) ? dxAbsGreenBrush : dxAbsRedBrush);
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num30, num31), new Vector2(num32, num33), val4, 2f);
			}
		}
		((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)1;
		float num34 = MapY(0.0, num16, num17, num10, num11);
		if (num34 >= num10 && num34 <= num11)
		{
			((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num, num34), new Vector2(num + num2, num34), dxZeroBrush, 1f);
		}
		RectangleF val8 = default(RectangleF);
		for (int l = fromIndex; l <= toIndex; l++)
		{
			if (l >= 0 && l < barCOBI.Count && barHasData[l])
			{
				double num35 = barCOBI[l];
				float num36 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, l);
				float val5 = MapY(num35, num16, num17, num10, num11);
				float num37 = GetBarSpacing(chartControl, l, fromIndex, toIndex) / 2f;
				if (num37 < 1f)
				{
					num37 = 1f;
				}
				float val6 = Math.Min(num34, val5);
				float val7 = Math.Max(num34, val5);
				val6 = Math.Max(val6, num10);
				val7 = Math.Min(val7, num11);
				if (!(val7 - val6 < 0.5f))
				{
					((RectangleF)(ref val8))._002Ector(num36 - num37, val6, num37 * 2f, val7 - val6);
					Brush val9 = ((num35 >= 0.0) ? dxBlueFillBrush : dxOrangeFillBrush);
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(val8, val9);
				}
			}
		}
		((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		for (int m = fromIndex + 1; m <= toIndex; m++)
		{
			if (m >= 1 && m < barCOBI.Count && barHasData[m] && barHasData[m - 1])
			{
				float num38 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, m - 1);
				float num39 = ClampY(MapY(barCOBI[m - 1], num16, num17, num10, num11), num10, num11);
				float num40 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, m);
				float num41 = ClampY(MapY(barCOBI[m], num16, num17, num10, num11), num10, num11);
				Brush val10 = ((barCOBI[m] >= 0.0) ? dxBlueFillBrush : dxOrangeFillBrush);
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num38, num39), new Vector2(num40, num41), val10, 2f);
			}
		}
		((IndicatorRenderBase)this).RenderTarget.AntialiasMode = antialiasMode;
	}

	/// <summary>Maps a data value to a Y pixel within [secTop, secBot].</summary>
	private float MapY(double value, double dataMin, double dataMax, float secTop, float secBot)
	{
		if (dataMax == dataMin)
		{
			return (secTop + secBot) / 2f;
		}
		double num = (value - dataMin) / (dataMax - dataMin);
		return secBot - (float)(num * (double)(secBot - secTop));
	}

	private float ClampY(float y, float top, float bot)
	{
		if (y < top)
		{
			return top;
		}
		if (y > bot)
		{
			return bot;
		}
		return y;
	}

	private float GetBarSpacing(ChartControl chartControl, int barIdx, int fromIdx, int toIdx)
	{
		float num = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, barIdx);
		if (barIdx < toIdx)
		{
			return (float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, barIdx + 1) - num;
		}
		if (barIdx > fromIdx)
		{
			return num - (float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, barIdx - 1);
		}
		return (float)chartControl.BarWidth;
	}

	private void DrawLabel(string text, float x, float y)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (dxLabelFormat != null && dxLabelBrush != null)
		{
			TextLayout val = new TextLayout(dwFactory, text, dxLabelFormat, 400f, 20f);
			((IndicatorRenderBase)this).RenderTarget.DrawTextLayout(new Vector2(x, y), val, dxLabelBrush);
			((DisposeBase)val).Dispose();
		}
	}

	private void EnsureDxResources()
	{
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected O, but got Unknown
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Expected O, but got Unknown
		if (((IndicatorRenderBase)this).RenderTarget != null && dxGreenBrush == null)
		{
			dxGreenBrush = DxExtensions.ToDxBrush(HistogramUpColor, ((IndicatorRenderBase)this).RenderTarget);
			dxGreenBrush.Opacity = 0.85f;
			dxRedBrush = DxExtensions.ToDxBrush(HistogramDownColor, ((IndicatorRenderBase)this).RenderTarget);
			dxRedBrush.Opacity = 0.85f;
			dxBlueFillBrush = DxExtensions.ToDxBrush(COBIBullColor, ((IndicatorRenderBase)this).RenderTarget);
			dxBlueFillBrush.Opacity = 0.35f;
			dxOrangeFillBrush = DxExtensions.ToDxBrush(COBIBearColor, ((IndicatorRenderBase)this).RenderTarget);
			dxOrangeFillBrush.Opacity = 0.35f;
			dxZeroBrush = DxExtensions.ToDxBrush(ZeroLineColor, ((IndicatorRenderBase)this).RenderTarget);
			dxSepBrush = DxExtensions.ToDxBrush(SeparatorColor, ((IndicatorRenderBase)this).RenderTarget);
			dxAbsLineBrush = DxExtensions.ToDxBrush(AbsorptionLineColor, ((IndicatorRenderBase)this).RenderTarget);
			dxAbsGreenBrush = DxExtensions.ToDxBrush(AbsorptionUpColor, ((IndicatorRenderBase)this).RenderTarget);
			dxAbsRedBrush = DxExtensions.ToDxBrush(AbsorptionDownColor, ((IndicatorRenderBase)this).RenderTarget);
			dxLabelBrush = DxExtensions.ToDxBrush(LabelColor, ((IndicatorRenderBase)this).RenderTarget);
			dwFactory = new Factory();
			dxLabelFormat = new TextFormat(dwFactory, "Segoe UI", 11f);
		}
	}

	private void DisposeDxResources()
	{
		if (dxGreenBrush != null)
		{
			((DisposeBase)dxGreenBrush).Dispose();
			dxGreenBrush = null;
		}
		if (dxRedBrush != null)
		{
			((DisposeBase)dxRedBrush).Dispose();
			dxRedBrush = null;
		}
		if (dxBlueFillBrush != null)
		{
			((DisposeBase)dxBlueFillBrush).Dispose();
			dxBlueFillBrush = null;
		}
		if (dxOrangeFillBrush != null)
		{
			((DisposeBase)dxOrangeFillBrush).Dispose();
			dxOrangeFillBrush = null;
		}
		if (dxZeroBrush != null)
		{
			((DisposeBase)dxZeroBrush).Dispose();
			dxZeroBrush = null;
		}
		if (dxSepBrush != null)
		{
			((DisposeBase)dxSepBrush).Dispose();
			dxSepBrush = null;
		}
		if (dxAbsLineBrush != null)
		{
			((DisposeBase)dxAbsLineBrush).Dispose();
			dxAbsLineBrush = null;
		}
		if (dxAbsGreenBrush != null)
		{
			((DisposeBase)dxAbsGreenBrush).Dispose();
			dxAbsGreenBrush = null;
		}
		if (dxAbsRedBrush != null)
		{
			((DisposeBase)dxAbsRedBrush).Dispose();
			dxAbsRedBrush = null;
		}
		if (dxLabelBrush != null)
		{
			((DisposeBase)dxLabelBrush).Dispose();
			dxLabelBrush = null;
		}
		if (dxLabelFormat != null)
		{
			((DisposeBase)dxLabelFormat).Dispose();
			dxLabelFormat = null;
		}
		if (dwFactory != null)
		{
			((DisposeBase)dwFactory).Dispose();
			dwFactory = null;
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDxResources();
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}
}

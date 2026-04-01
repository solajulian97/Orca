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

public class OrcaCandleVolumeProfile : Indicator
{
	private List<Dictionary<double, long>> barVolumeMaps;

	private List<Dictionary<double, long>> barDeltaMaps;

	private List<double[]> barVACache;

	private double lastBid = double.NaN;

	private double lastAsk = double.NaN;

	private double prevLast = double.NaN;

	private SolidColorBrush bullBodyBrushDx;

	private SolidColorBrush bearBodyBrushDx;

	private SolidColorBrush bullWickBrushDx;

	private SolidColorBrush bearWickBrushDx;

	private SolidColorBrush volBrushDx;

	private SolidColorBrush pocBrushDx;

	private SolidColorBrush posDeltaBrushDx;

	private SolidColorBrush negDeltaBrushDx;

	private SolidColorBrush[] volGradientBrushes;

	private int lastBuiltGradientSteps = -1;

	private SolidColorBrush vaVolBrushDx;

	private SolidColorBrush[] vaGradientBrushes;

	private int lastBuiltVAGradientSteps = -1;

	private SolidColorBrush vaLineBrushDx;

	private StrokeStyle vaLineStrokeDx;

	private SolidColorBrush deltaTextBrushDx;

	private TextFormat textFormatDx;

	private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Tick Compression", GroupName = "Data", Order = 0)]
	public int TickCompression { get; set; }

	[NinjaScriptProperty]
	[Range(2, 100)]
	[Display(Name = "Candle Width (px)", GroupName = "Layout", Order = 1)]
	public int CandleWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Profile Width (px)", GroupName = "Layout", Order = 2)]
	public int ProfileWidthPx { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dynamic Profile Width", Description = "Dynamically adjusts profile width to fit between candles", GroupName = "Layout", Order = 3)]
	public bool DynamicProfileWidth { get; set; }

	[NinjaScriptProperty]
	[Range(0, 50)]
	[Display(Name = "Candle-Profile Gap (px)", GroupName = "Layout", Order = 4)]
	public int CandleProfileGapPx { get; set; }

	[NinjaScriptProperty]
	[Range(0, 10)]
	[Display(Name = "Profile Bar Spacing (px)", GroupName = "Layout", Order = 5)]
	public int ProfileBarSpacingPx { get; set; }

	[NinjaScriptProperty]
	[Range(1, 6)]
	[Display(Name = "Wick Width (px)", GroupName = "Layout", Order = 6)]
	public int WickWidthPx { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show POC", GroupName = "Visibility", Order = 10)]
	public bool ShowPOC { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Delta", GroupName = "Visibility", Order = 11)]
	public bool ShowDelta { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Use Gradient", GroupName = "Visibility", Order = 12)]
	public bool UseGradient { get; set; }

	[NinjaScriptProperty]
	[Range(2, 64)]
	[Display(Name = "Gradient Steps", GroupName = "Visibility", Order = 13)]
	public int GradientSteps { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Value Area", GroupName = "Value Area", Order = 20)]
	public bool ShowValueArea { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VA Color Mode", Description = "Color rows inside the Value Area differently", GroupName = "Value Area", Order = 21)]
	public bool ShowVAColor { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VA Boundary Lines", Description = "Draw dashed lines at VAH and VAL", GroupName = "Value Area", Order = 22)]
	public bool ShowVALines { get; set; }

	[NinjaScriptProperty]
	[Range(50, 95)]
	[Display(Name = "VA Percent", GroupName = "Value Area", Order = 23)]
	public int ValueAreaPercent { get; set; }

	[NinjaScriptProperty]
	[Range(0.5, 6.0)]
	[Display(Name = "VA Line Thickness", GroupName = "Value Area", Order = 24)]
	public float VALineThickness { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VA Line Style", GroupName = "Value Area", Order = 25)]
	public VALineStyleEnum VALineStyle { get; set; }

	[XmlIgnore]
	[Display(Name = "VA Color", GroupName = "Value Area", Order = 26)]
	public Brush VABrush { get; set; }

	[Browsable(false)]
	public string VABrushSerialize
	{
		get
		{
			return Serialize.BrushToString(VABrush);
		}
		set
		{
			VABrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "VA Line Color", GroupName = "Value Area", Order = 27)]
	public Brush VALineBrush { get; set; }

	[Browsable(false)]
	public string VALineBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(VALineBrush);
		}
		set
		{
			VALineBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Bullish Body", GroupName = "Colors", Order = 30)]
	public Brush BullishBodyBrush { get; set; }

	[Browsable(false)]
	public string BullishBodyBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(BullishBodyBrush);
		}
		set
		{
			BullishBodyBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Bearish Body", GroupName = "Colors", Order = 31)]
	public Brush BearishBodyBrush { get; set; }

	[Browsable(false)]
	public string BearishBodyBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(BearishBodyBrush);
		}
		set
		{
			BearishBodyBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Volume Color", GroupName = "Colors", Order = 32)]
	public Brush VolumeBrush { get; set; }

	[Browsable(false)]
	public string VolumeBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(VolumeBrush);
		}
		set
		{
			VolumeBrush = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0.05, 1.0)]
	[Display(Name = "Min Brightness", GroupName = "Colors", Order = 33)]
	public float MinBrightness { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, 1.0)]
	[Display(Name = "Volume Opacity", GroupName = "Colors", Order = 34)]
	public float VolumeOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "POC Color", GroupName = "Colors", Order = 35)]
	public Brush POCBrush { get; set; }

	[Browsable(false)]
	public string POCBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(POCBrush);
		}
		set
		{
			POCBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Positive Delta", GroupName = "Colors", Order = 36)]
	public Brush PositiveDeltaBrush { get; set; }

	[Browsable(false)]
	public string PositiveDeltaBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(PositiveDeltaBrush);
		}
		set
		{
			PositiveDeltaBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Negative Delta", GroupName = "Colors", Order = 37)]
	public Brush NegativeDeltaBrush { get; set; }

	[Browsable(false)]
	public string NegativeDeltaBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(NegativeDeltaBrush);
		}
		set
		{
			NegativeDeltaBrush = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0.1, 1.0)]
	[Display(Name = "Delta Opacity", GroupName = "Colors", Order = 38)]
	public float DeltaOpacity { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Invalid comparison between Unknown and I4
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Invalid comparison between Unknown and I4
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = "OrcaCandleVolumeProfile";
			((NinjaScript)this).Description = "Custom footprint chart: draws candles + per-candle volume profiles with optional delta coloring and Value Area.";
			((NinjaScriptBase)this).Calculate = (Calculate)2;
			((NinjaScriptBase)this).IsOverlay = true;
			TickCompression = 4;
			CandleWidthPx = 14;
			ProfileWidthPx = 80;
			DynamicProfileWidth = true;
			CandleProfileGapPx = 2;
			ProfileBarSpacingPx = 0;
			WickWidthPx = 2;
			ShowPOC = true;
			ShowDelta = false;
			UseGradient = true;
			GradientSteps = 16;
			ShowValueArea = true;
			ShowVAColor = true;
			ShowVALines = true;
			ValueAreaPercent = 70;
			VALineThickness = 1.5f;
			VALineStyle = VALineStyleEnum.Dash;
			BullishBodyBrush = Brushes.MediumSeaGreen;
			BearishBodyBrush = Brushes.Crimson;
			VolumeBrush = Brushes.RoyalBlue;
			VolumeOpacity = 0.85f;
			MinBrightness = 0.2f;
			POCBrush = Brushes.DodgerBlue;
			VABrush = Brushes.CornflowerBlue;
			VALineBrush = Brushes.White;
			PositiveDeltaBrush = Brushes.Lime;
			NegativeDeltaBrush = Brushes.Red;
			DeltaOpacity = 0.85f;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			barVolumeMaps = new List<Dictionary<double, long>>(4096);
			barDeltaMaps = new List<Dictionary<double, long>>(4096);
			barVACache = new List<double[]>(4096);
			textWidthCache.Clear();
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			DisposeDx();
		}
	}

	private void DisposeDx()
	{
		try
		{
			SolidColorBrush obj = bullBodyBrushDx;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			SolidColorBrush obj2 = bearBodyBrushDx;
			if (obj2 != null)
			{
				((DisposeBase)obj2).Dispose();
			}
			SolidColorBrush obj3 = bullWickBrushDx;
			if (obj3 != null)
			{
				((DisposeBase)obj3).Dispose();
			}
			SolidColorBrush obj4 = bearWickBrushDx;
			if (obj4 != null)
			{
				((DisposeBase)obj4).Dispose();
			}
			SolidColorBrush obj5 = volBrushDx;
			if (obj5 != null)
			{
				((DisposeBase)obj5).Dispose();
			}
			SolidColorBrush obj6 = pocBrushDx;
			if (obj6 != null)
			{
				((DisposeBase)obj6).Dispose();
			}
			SolidColorBrush obj7 = posDeltaBrushDx;
			if (obj7 != null)
			{
				((DisposeBase)obj7).Dispose();
			}
			SolidColorBrush obj8 = negDeltaBrushDx;
			if (obj8 != null)
			{
				((DisposeBase)obj8).Dispose();
			}
			SolidColorBrush obj9 = vaVolBrushDx;
			if (obj9 != null)
			{
				((DisposeBase)obj9).Dispose();
			}
			SolidColorBrush obj10 = vaLineBrushDx;
			if (obj10 != null)
			{
				((DisposeBase)obj10).Dispose();
			}
			StrokeStyle obj11 = vaLineStrokeDx;
			if (obj11 != null)
			{
				((DisposeBase)obj11).Dispose();
			}
			SolidColorBrush obj12 = deltaTextBrushDx;
			if (obj12 != null)
			{
				((DisposeBase)obj12).Dispose();
			}
			TextFormat obj13 = textFormatDx;
			if (obj13 != null)
			{
				((DisposeBase)obj13).Dispose();
			}
			if (volGradientBrushes != null)
			{
				for (int i = 0; i < volGradientBrushes.Length; i++)
				{
					SolidColorBrush obj14 = volGradientBrushes[i];
					if (obj14 != null)
					{
						((DisposeBase)obj14).Dispose();
					}
				}
			}
			if (vaGradientBrushes == null)
			{
				return;
			}
			for (int j = 0; j < vaGradientBrushes.Length; j++)
			{
				SolidColorBrush obj15 = vaGradientBrushes[j];
				if (obj15 != null)
				{
					((DisposeBase)obj15).Dispose();
				}
			}
		}
		catch
		{
		}
		finally
		{
			bullBodyBrushDx = null;
			bearBodyBrushDx = null;
			bullWickBrushDx = null;
			bearWickBrushDx = null;
			volBrushDx = null;
			pocBrushDx = null;
			posDeltaBrushDx = null;
			negDeltaBrushDx = null;
			vaVolBrushDx = null;
			vaLineBrushDx = null;
			vaLineStrokeDx = null;
			deltaTextBrushDx = null;
			textFormatDx = null;
			volGradientBrushes = null;
			vaGradientBrushes = null;
			lastBuiltGradientSteps = -1;
			lastBuiltVAGradientSteps = -1;
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDx();
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}

	protected override void OnMarketData(MarketDataEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if ((int)e.MarketDataType == 1)
		{
			lastBid = e.Price;
		}
		else if ((int)e.MarketDataType == 0)
		{
			lastAsk = e.Price;
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).BarsInProgress == 1)
		{
			ProcessTickIntoPrimaryBar();
		}
		else if (((NinjaScriptBase)this).BarsInProgress == 0 && ((NinjaScriptBase)this).CurrentBar >= 0)
		{
			EnsureBarMaps(((NinjaScriptBase)this).CurrentBar);
		}
	}

	private void EnsureBarMaps(int primaryBarIndex)
	{
		while (barVolumeMaps.Count <= primaryBarIndex)
		{
			barVolumeMaps.Add(new Dictionary<double, long>());
		}
		while (barDeltaMaps.Count <= primaryBarIndex)
		{
			barDeltaMaps.Add(new Dictionary<double, long>());
		}
		while (barVACache.Count <= primaryBarIndex)
		{
			barVACache.Add(new double[4]
			{
				double.NaN,
				double.NaN,
				double.NaN,
				0.0
			});
		}
	}

	private void ProcessTickIntoPrimaryBar()
	{
		int bar = ((NinjaScriptBase)this).BarsArray[0].GetBar(((NinjaScriptBase)this).Time[0]);
		if (bar < 0)
		{
			return;
		}
		EnsureBarMaps(bar);
		double num = ((NinjaScriptBase)this).Close[0];
		long num2 = (long)((NinjaScriptBase)this).Volume[0];
		if (num2 <= 0)
		{
			return;
		}
		double num3 = (double)TickCompression * ((NinjaScriptBase)this).TickSize;
		double key = Math.Floor(num / num3 + 1E-06) * num3;
		Dictionary<double, long> dictionary = barVolumeMaps[bar];
		if (dictionary.TryGetValue(key, out var value))
		{
			dictionary[key] = value + num2;
		}
		else
		{
			dictionary[key] = num2;
		}
		long num4 = 0L;
		if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0.0 && lastBid > 0.0 && lastAsk >= lastBid)
		{
			if (num >= lastAsk)
			{
				num4 = num2;
			}
			else if (num <= lastBid)
			{
				num4 = -num2;
			}
			else if (!double.IsNaN(prevLast))
			{
				num4 = ((num > prevLast) ? num2 : ((num < prevLast) ? (-num2) : 0));
			}
		}
		else if (!double.IsNaN(prevLast))
		{
			num4 = ((num > prevLast) ? num2 : ((num < prevLast) ? (-num2) : 0));
		}
		prevLast = num;
		if (num4 != 0L)
		{
			Dictionary<double, long> dictionary2 = barDeltaMaps[bar];
			if (dictionary2.TryGetValue(key, out var value2))
			{
				dictionary2[key] = value2 + num4;
			}
			else
			{
				dictionary2[key] = num4;
			}
		}
	}

	/// <summary>
	/// Calculates Value Area boundaries for a given volume map.
	/// Returns true if valid, with vahPrice and valPrice set.
	/// VA = price range covering ValueAreaPercent% of total volume, expanding outward from POC.
	/// </summary>
	private bool CalcValueArea(Dictionary<double, long> volMap, double pocPrice, out double vahPrice, out double valPrice)
	{
		vahPrice = pocPrice;
		valPrice = pocPrice;
		if (volMap.Count <= 1)
		{
			return false;
		}
		List<double> list = new List<double>(volMap.Keys);
		list.Sort();
		long num = 0L;
		foreach (KeyValuePair<double, long> item in volMap)
		{
			num += item.Value;
		}
		if (num <= 0)
		{
			return false;
		}
		double num2 = (double)num * ((double)ValueAreaPercent / 100.0);
		int num3 = list.IndexOf(pocPrice);
		if (num3 < 0)
		{
			return false;
		}
		long num4 = volMap[pocPrice];
		int num5 = num3;
		int num6 = num3;
		while ((double)num4 < num2 && (num5 > 0 || num6 < list.Count - 1))
		{
			long num7 = ((num5 > 0) ? volMap[list[num5 - 1]] : 0);
			long num8 = ((num6 < list.Count - 1) ? volMap[list[num6 + 1]] : 0);
			if (num5 <= 0)
			{
				num6++;
				num4 += num8;
			}
			else if (num6 >= list.Count - 1)
			{
				num5--;
				num4 += num7;
			}
			else if (num8 >= num7)
			{
				num6++;
				num4 += num8;
			}
			else
			{
				num5--;
				num4 += num7;
			}
		}
		valPrice = list[num5];
		vahPrice = list[num6];
		return true;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
			if (barVolumeMaps == null || ((IndicatorRenderBase)this).ChartBars == null)
			{
				return;
			}
			EnsureDxResources();
			int fromIndex = ((IndicatorRenderBase)this).ChartBars.FromIndex;
			int toIndex = ((IndicatorRenderBase)this).ChartBars.ToIndex;
			float panelTop = ((IndicatorRenderBase)this).ChartPanel.Y;
			float panelBottom = ((IndicatorRenderBase)this).ChartPanel.Y + ((IndicatorRenderBase)this).ChartPanel.H;
			for (int i = fromIndex; i <= toIndex; i++)
			{
				if (i < 0 || i >= ((NinjaScriptBase)this).BarsArray[0].Count)
				{
					continue;
				}
				float num = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, i);
				double open = ((NinjaScriptBase)this).BarsArray[0].GetOpen(i);
				double high = ((NinjaScriptBase)this).BarsArray[0].GetHigh(i);
				double low = ((NinjaScriptBase)this).BarsArray[0].GetLow(i);
				double close = ((NinjaScriptBase)this).BarsArray[0].GetClose(i);
				float val = chartScale.GetYByValue(open);
				float num2 = chartScale.GetYByValue(high);
				float num3 = chartScale.GetYByValue(low);
				float val2 = chartScale.GetYByValue(close);
				bool flag = close >= open;
				float num4 = Math.Min(val, val2);
				float num5 = Math.Max(val, val2);
				float num6 = Math.Max(1f, num5 - num4);
				float num7 = (float)CandleWidthPx / 2f;
				float num8 = num - num7;
				float num9 = num + num7;
				SolidColorBrush val3 = (flag ? bullBodyBrushDx : bearBodyBrushDx);
				SolidColorBrush val4 = (flag ? bullWickBrushDx : bearWickBrushDx);
				float num10 = num;
				float num11 = (float)WickWidthPx / 2f;
				if (num2 < num4)
				{
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(num10 - num11, num2, (float)WickWidthPx, num4 - num2), (Brush)(object)val4);
				}
				if (num3 > num5)
				{
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(num10 - num11, num5, (float)WickWidthPx, num3 - num5), (Brush)(object)val4);
				}
				((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(num8, num4, (float)CandleWidthPx, num6), (Brush)(object)val3);
				if (i < barVolumeMaps.Count && barVolumeMaps[i].Count > 0)
				{
					float drawProfileWidth = ProfileWidthPx;
					if (DynamicProfileWidth)
					{
						float num12 = ((i + 1 < ((IndicatorRenderBase)this).ChartBars.Count) ? ((float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, i + 1)) : ((i <= 0) ? (num + (float)ProfileWidthPx) : (num + (num - (float)chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, i - 1)))));
						float num13 = num12 - num7 - (num9 + (float)CandleProfileGapPx);
						drawProfileWidth = Math.Max(2f, num13 - 1f);
					}
					DrawBarVolumeProfile(chartScale, i, num9 + (float)CandleProfileGapPx, panelTop, panelBottom, drawProfileWidth);
				}
			}
		}
		catch (Exception)
		{
			DisposeDx();
		}
	}

	private void DrawBarVolumeProfile(ChartScale chartScale, int barIdx, float profileLeftX, float panelTop, float panelBottom, float drawProfileWidth)
	{
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_044a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0459: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<double, long> dictionary = barVolumeMaps[barIdx];
		if (dictionary.Count == 0)
		{
			return;
		}
		long num = 0L;
		double num2 = double.NaN;
		double vahPrice = double.NaN;
		double valPrice = double.NaN;
		bool flag = false;
		bool flag2 = barIdx == ((NinjaScriptBase)this).BarsArray[0].Count - 1;
		double[] array = barVACache[barIdx];
		if (double.IsNaN(array[0]) || flag2)
		{
			foreach (KeyValuePair<double, long> item in dictionary)
			{
				if (item.Value > num)
				{
					num = item.Value;
					num2 = item.Key;
				}
			}
			if (num > 0 && ShowValueArea && (ShowVAColor || ShowVALines))
			{
				flag = CalcValueArea(dictionary, num2, out vahPrice, out valPrice);
			}
			if (!flag2)
			{
				array[0] = vahPrice;
				array[1] = valPrice;
				array[2] = num2;
				array[3] = num;
			}
		}
		else
		{
			vahPrice = array[0];
			valPrice = array[1];
			num2 = array[2];
			num = (long)array[3];
			flag = !double.IsNaN(vahPrice);
		}
		if (num <= 0)
		{
			return;
		}
		Dictionary<double, long> dictionary2 = null;
		if (ShowDelta && barIdx < barDeltaMaps.Count && barDeltaMaps[barIdx].Count > 0)
		{
			dictionary2 = barDeltaMaps[barIdx];
		}
		double num3 = (double)TickCompression * ((NinjaScriptBase)this).TickSize;
		RectangleF val = default(RectangleF);
		foreach (KeyValuePair<double, long> item2 in dictionary)
		{
			double key = item2.Key;
			long value = item2.Value;
			int yByValue = chartScale.GetYByValue(key + num3);
			int yByValue2 = chartScale.GetYByValue(key);
			if ((float)yByValue2 < panelTop - 20f || (float)yByValue > panelBottom + 20f)
			{
				continue;
			}
			int num4 = Math.Max(1, Math.Abs(yByValue2 - yByValue) - ProfileBarSpacingPx);
			float num5 = (float)Math.Min(yByValue, yByValue2) + (float)ProfileBarSpacingPx / 2f;
			float num6 = (float)((double)drawProfileWidth * ((double)value / (double)num));
			if (num6 < 0.5f)
			{
				continue;
			}
			((RectangleF)(ref val))._002Ector(profileLeftX, num5, num6, (float)num4);
			bool flag3 = flag && key >= valPrice - ((NinjaScriptBase)this).TickSize * 0.01 && key <= vahPrice + ((NinjaScriptBase)this).TickSize * 0.01;
			SolidColorBrush val2;
			long value2;
			if (ShowPOC && Math.Abs(key - num2) < ((NinjaScriptBase)this).TickSize * 0.01)
			{
				val2 = pocBrushDx;
			}
			else if (ShowDelta && dictionary2 != null && dictionary2.TryGetValue(key, out value2))
			{
				val2 = ((value2 >= 0) ? posDeltaBrushDx : negDeltaBrushDx);
			}
			else if (UseGradient)
			{
				SolidColorBrush[] array2 = ((ShowValueArea && ShowVAColor && flag3 && vaGradientBrushes != null) ? vaGradientBrushes : volGradientBrushes);
				if (array2 != null)
				{
					double num7 = (double)value / (double)num;
					int num8 = array2.Length;
					int num9 = (int)(num7 * (double)(num8 - 1));
					if (num9 < 0)
					{
						num9 = 0;
					}
					if (num9 >= num8)
					{
						num9 = num8 - 1;
					}
					val2 = array2[num9];
				}
				else
				{
					val2 = volBrushDx;
				}
			}
			else
			{
				val2 = ((ShowValueArea && ShowVAColor && flag3) ? vaVolBrushDx : volBrushDx);
			}
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(val, (Brush)(object)val2);
		}
		if (flag && ShowValueArea && ShowVALines && vaLineBrushDx != null)
		{
			float num10 = profileLeftX + drawProfileWidth;
			float num11 = chartScale.GetYByValue(vahPrice + num3);
			if (num11 >= panelTop - 5f && num11 <= panelBottom + 5f)
			{
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(profileLeftX - 2f, num11), new Vector2(num10 + 2f, num11), (Brush)(object)vaLineBrushDx, VALineThickness, vaLineStrokeDx);
			}
			float num12 = chartScale.GetYByValue(valPrice);
			if (num12 >= panelTop - 5f && num12 <= panelBottom + 5f)
			{
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(profileLeftX - 2f, num12), new Vector2(num10 + 2f, num12), (Brush)(object)vaLineBrushDx, VALineThickness, vaLineStrokeDx);
			}
		}
	}

	private float MeasureTextWidth(string text)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (textFormatDx == null)
		{
			return 0f;
		}
		if (textWidthCache.TryGetValue(text, out var value))
		{
			return value;
		}
		TextLayout val = new TextLayout(Globals.DirectWriteFactory, text, textFormatDx, 1000f, 100f);
		try
		{
			value = val.Metrics.Width;
			textWidthCache[text] = value;
			return value;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private void EnsureDxResources()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected O, but got Unknown
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Expected O, but got Unknown
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Expected O, but got Unknown
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Expected O, but got Unknown
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Expected O, but got Unknown
		if (((IndicatorRenderBase)this).RenderTarget == null)
		{
			return;
		}
		if (bullBodyBrushDx == null)
		{
			bullBodyBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(BullishBodyBrush, 1f));
		}
		if (bearBodyBrushDx == null)
		{
			bearBodyBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(BearishBodyBrush, 1f));
		}
		if (bullWickBrushDx == null)
		{
			bullWickBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(BullishBodyBrush, 1f));
		}
		if (bearWickBrushDx == null)
		{
			bearWickBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(BearishBodyBrush, 1f));
		}
		if (volBrushDx == null)
		{
			volBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(VolumeBrush, VolumeOpacity));
		}
		if (pocBrushDx == null)
		{
			pocBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(POCBrush, 1f));
		}
		if (posDeltaBrushDx == null)
		{
			posDeltaBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(PositiveDeltaBrush, DeltaOpacity));
		}
		if (negDeltaBrushDx == null)
		{
			negDeltaBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(NegativeDeltaBrush, DeltaOpacity));
		}
		if (vaVolBrushDx == null)
		{
			vaVolBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(VABrush, VolumeOpacity));
		}
		if (vaLineBrushDx == null)
		{
			vaLineBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(VALineBrush, 1f));
		}
		if (vaLineStrokeDx == null)
		{
			DashStyle dashStyle = (DashStyle)(VALineStyle switch
			{
				VALineStyleEnum.Solid => 0, 
				VALineStyleEnum.Dot => 2, 
				VALineStyleEnum.DashDot => 3, 
				_ => 1, 
			});
			vaLineStrokeDx = new StrokeStyle(((Resource)((IndicatorRenderBase)this).RenderTarget).Factory, new StrokeStyleProperties
			{
				DashStyle = dashStyle
			});
		}
		int num = Math.Max(2, GradientSteps);
		if (UseGradient && (volGradientBrushes == null || lastBuiltGradientSteps != num))
		{
			if (volGradientBrushes != null)
			{
				for (int i = 0; i < volGradientBrushes.Length; i++)
				{
					SolidColorBrush obj = volGradientBrushes[i];
					if (obj != null)
					{
						((DisposeBase)obj).Dispose();
					}
				}
			}
			volGradientBrushes = BuildGradientPalette(VolumeBrush, num);
			lastBuiltGradientSteps = num;
		}
		if (!UseGradient || !ShowValueArea || !ShowVAColor || (vaGradientBrushes != null && lastBuiltVAGradientSteps == num))
		{
			return;
		}
		if (vaGradientBrushes != null)
		{
			for (int j = 0; j < vaGradientBrushes.Length; j++)
			{
				SolidColorBrush obj2 = vaGradientBrushes[j];
				if (obj2 != null)
				{
					((DisposeBase)obj2).Dispose();
				}
			}
		}
		vaGradientBrushes = BuildGradientPalette(VABrush, num);
		lastBuiltVAGradientSteps = num;
	}

	private SolidColorBrush[] BuildGradientPalette(Brush baseBrush, int steps)
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		Color color = BrushToMediaColor(baseBrush);
		SolidColorBrush[] array = (SolidColorBrush[])(object)new SolidColorBrush[steps];
		Color4 val = default(Color4);
		for (int i = 0; i < steps; i++)
		{
			float num = (float)i / (float)(steps - 1);
			float num2 = MinBrightness + num * (1f - MinBrightness);
			((Color4)(ref val))._002Ector((float)(int)color.R / 255f * num2, (float)(int)color.G / 255f * num2, (float)(int)color.B / 255f * num2, (float)(int)color.A / 255f * VolumeOpacity);
			try
			{
				array[i] = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, val);
			}
			catch
			{
				return null;
			}
		}
		return array;
	}

	private static Color BrushToMediaColor(Brush b)
	{
		return (b as SolidColorBrush)?.Color ?? Colors.White;
	}

	private Color4 ToDxColor(Brush b, float alphaMult)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		Color color = BrushToMediaColor(b);
		return new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, (float)(int)color.A / 255f * alphaMult);
	}
}

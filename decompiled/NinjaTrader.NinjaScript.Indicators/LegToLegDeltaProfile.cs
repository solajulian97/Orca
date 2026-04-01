using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

public class LegToLegDeltaProfile : Indicator
{
	private List<Dictionary<double, long>> barDeltaMaps;

	private List<Dictionary<double, long>> barVolMaps;

	private readonly Dictionary<double, long> legDeltaByPrice = new Dictionary<double, long>();

	private readonly Dictionary<double, long> sessionDeltaByPrice = new Dictionary<double, long>();

	private readonly Dictionary<double, long> legVolByPrice = new Dictionary<double, long>();

	private readonly Dictionary<double, long> sessionVolByPrice = new Dictionary<double, long>();

	private int sessionStartBar = -1;

	private int legStartBar = -1;

	private double legHigh = double.NaN;

	private double legLow = double.NaN;

	private int legHighBar = -1;

	private int legLowBar = -1;

	private LegDirection legDir;

	private double lastBid = double.NaN;

	private double lastAsk = double.NaN;

	private double prevLast = double.NaN;

	private TextFormat textFormat;

	private SolidColorBrush posBrushDx;

	private SolidColorBrush negBrushDx;

	private SolidColorBrush textBrushDx;

	private SolidColorBrush spineBrushDx;

	private SolidColorBrush volBrushDx;

	private string lastPosBrushSer;

	private string lastNegBrushSer;

	private string lastTextBrushSer;

	private string lastVolBrushSer;

	private float lastDeltaOpacity = -1f;

	private float lastVolOpacity = -1f;

	private int lastFontSize = -1;

	[NinjaScriptProperty]
	[Display(Name = "Profile Mode", Order = 0, GroupName = "Mode")]
	public ProfileModes ProfileMode { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Rotation Mode", Order = 0, GroupName = "Leg Detection", Description = "DistanceFromExtreme = new profile when price is X away from leg high/low. TrueSwingAlternate = only on reversal X, alternates direction (new leg starts at swing extreme bar).")]
	public LegRotationMode RotationMode { get; set; }

	[NinjaScriptProperty]
	[Range(0.0, 5000.0)]
	[Display(Name = "Rotation (Points)", Order = 1, GroupName = "Leg Detection", Description = "Start a new leg profile when price rotates by this many POINTS. Set to 0 to disable.")]
	public double RotationPoints { get; set; }

	[NinjaScriptProperty]
	[Range(20, 600)]
	[Display(Name = "Max Delta Width (px)", Order = 2, GroupName = "Delta Rendering")]
	public int MaxProfileWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(0, 1000000)]
	[Display(Name = "Min Abs Delta To Show", Order = 3, GroupName = "Delta Rendering")]
	public long MinAbsDeltaToShow { get; set; }

	[NinjaScriptProperty]
	[Range(200, 50000)]
	[Display(Name = "Rebuild Lookback Cap (bars)", Order = 4, GroupName = "Performance")]
	public int RebuildLookbackBarsCap { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Auto Delta Text", Order = 5, GroupName = "Delta Rendering", Description = "When enabled, font size (and whether text is shown) is automatically derived from current row height / zoom.")]
	public bool AutoDeltaText { get; set; }

	[NinjaScriptProperty]
	[Range(6, 30)]
	[Display(Name = "Auto Font Min", Order = 6, GroupName = "Delta Rendering")]
	public int AutoFontMin { get; set; }

	[NinjaScriptProperty]
	[Range(6, 40)]
	[Display(Name = "Auto Font Max", Order = 7, GroupName = "Delta Rendering")]
	public int AutoFontMax { get; set; }

	[NinjaScriptProperty]
	[Range(8, 28)]
	[Display(Name = "Manual Font Size", Order = 8, GroupName = "Delta Rendering", Description = "Used only when Auto Delta Text = false.")]
	public int FontSize { get; set; }

	[NinjaScriptProperty]
	[Range(2, 30)]
	[Display(Name = "Min Row Height (px)", Order = 9, GroupName = "Delta Rendering", Description = "Auto-groups ticks per row when zoomed out to avoid overlap (DELTA only).")]
	public int MinRowHeightPx { get; set; }

	[NinjaScriptProperty]
	[XmlIgnore]
	[Display(Name = "Positive Brush", Order = 10, GroupName = "Delta Colors")]
	public Brush PositiveBrush { get; set; }

	[Browsable(false)]
	public string PositiveBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(PositiveBrush);
		}
		set
		{
			PositiveBrush = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[XmlIgnore]
	[Display(Name = "Negative Brush", Order = 11, GroupName = "Delta Colors")]
	public Brush NegativeBrush { get; set; }

	[Browsable(false)]
	public string NegativeBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(NegativeBrush);
		}
		set
		{
			NegativeBrush = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[XmlIgnore]
	[Display(Name = "Text Brush", Order = 12, GroupName = "Delta Colors")]
	public Brush TextBrush { get; set; }

	[Browsable(false)]
	public string TextBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(TextBrush);
		}
		set
		{
			TextBrush = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0.1, 1.0)]
	[Display(Name = "Delta Opacity", Order = 13, GroupName = "Delta Rendering")]
	public float DeltaOpacity { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Spine", Order = 0, GroupName = "Rendering")]
	public bool ShowSpine { get; set; }

	[NinjaScriptProperty]
	[Range(1, 6)]
	[Display(Name = "Spine Width (px)", Order = 1, GroupName = "Rendering")]
	public int SpineWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(0, 120)]
	[Display(Name = "Right Margin (px)", Order = 2, GroupName = "Rendering")]
	public int RightMarginPx { get; set; }

	[NinjaScriptProperty]
	[Range(0.0, 0.8)]
	[Display(Name = "Right Reserved %", Order = 3, GroupName = "Rendering")]
	public double RightReservedPercent { get; set; }

	[NinjaScriptProperty]
	[Range(-500, 500)]
	[Display(Name = "X Offset (px)", Order = 4, GroupName = "Rendering")]
	public int XOffsetPx { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Volume Profile", Order = 0, GroupName = "Volume Profile")]
	public bool ShowVolumeProfile { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Volume Placement", Order = 1, GroupName = "Volume Profile")]
	public VolumeProfileLayer VolumeLayer { get; set; }

	[NinjaScriptProperty]
	[Range(20, 600)]
	[Display(Name = "Volume Width (px)", Order = 2, GroupName = "Volume Profile")]
	public int VolumeProfileWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(0.05, 1.0)]
	[Display(Name = "Volume Opacity", Order = 3, GroupName = "Volume Profile")]
	public float VolumeOpacity { get; set; }

	[NinjaScriptProperty]
	[XmlIgnore]
	[Display(Name = "Volume Brush", Order = 4, GroupName = "Volume Profile")]
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
	[Range(0, 50)]
	[Display(Name = "Side-by-Side Gap (px)", Order = 5, GroupName = "Volume Profile")]
	public int SideBySideGapPx { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Invalid comparison between Unknown and I4
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Invalid comparison between Unknown and I4
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = "LegToLegDeltaProfile";
			((NinjaScript)this).Description = "Rotation-based leg delta profile OR current-session delta profile, with optional tick-based volume profile layer (behind/infront/side-by-side).";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = true;
			ProfileMode = ProfileModes.LegToLeg;
			RotationPoints = 65.0;
			RotationMode = LegRotationMode.TrueSwingAlternate;
			MaxProfileWidthPx = 70;
			MinAbsDeltaToShow = 1L;
			RebuildLookbackBarsCap = 2000;
			AutoDeltaText = true;
			AutoFontMin = 6;
			AutoFontMax = 18;
			FontSize = 10;
			MinRowHeightPx = 10;
			ShowSpine = false;
			SpineWidthPx = 2;
			RightMarginPx = 0;
			DeltaOpacity = 0.85f;
			RightReservedPercent = 0.1;
			XOffsetPx = 0;
			PositiveBrush = Brushes.Blue;
			NegativeBrush = Brushes.Red;
			TextBrush = Brushes.LightGray;
			ShowVolumeProfile = true;
			VolumeLayer = VolumeProfileLayer.RightOfDelta;
			VolumeProfileWidthPx = 70;
			VolumeOpacity = 0.25f;
			VolumeBrush = Brushes.Gray;
			SideBySideGapPx = 4;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			barDeltaMaps = new List<Dictionary<double, long>>(4096);
			barVolMaps = new List<Dictionary<double, long>>(4096);
			legDeltaByPrice.Clear();
			sessionDeltaByPrice.Clear();
			legVolByPrice.Clear();
			sessionVolByPrice.Clear();
			sessionStartBar = -1;
			legStartBar = -1;
			legHigh = double.NaN;
			legLow = double.NaN;
			legHighBar = -1;
			legLowBar = -1;
			legDir = LegDirection.Unknown;
			lastPosBrushSer = (lastNegBrushSer = (lastTextBrushSer = (lastVolBrushSer = null)));
			lastDeltaOpacity = -1f;
			lastVolOpacity = -1f;
			lastFontSize = -1;
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
			TextFormat obj = textFormat;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			SolidColorBrush obj2 = posBrushDx;
			if (obj2 != null)
			{
				((DisposeBase)obj2).Dispose();
			}
			SolidColorBrush obj3 = negBrushDx;
			if (obj3 != null)
			{
				((DisposeBase)obj3).Dispose();
			}
			SolidColorBrush obj4 = textBrushDx;
			if (obj4 != null)
			{
				((DisposeBase)obj4).Dispose();
			}
			SolidColorBrush obj5 = spineBrushDx;
			if (obj5 != null)
			{
				((DisposeBase)obj5).Dispose();
			}
			SolidColorBrush obj6 = volBrushDx;
			if (obj6 != null)
			{
				((DisposeBase)obj6).Dispose();
			}
		}
		catch
		{
		}
		finally
		{
			textFormat = null;
			posBrushDx = null;
			negBrushDx = null;
			textBrushDx = null;
			spineBrushDx = null;
			volBrushDx = null;
		}
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
		else
		{
			if (((NinjaScriptBase)this).CurrentBar < 1)
			{
				return;
			}
			EnsureBarMaps(((NinjaScriptBase)this).CurrentBar);
			if (((NinjaScriptBase)this).Bars.IsFirstBarOfSession)
			{
				sessionStartBar = ((NinjaScriptBase)this).CurrentBar;
				RebuildSessionFrom(sessionStartBar);
				legStartBar = sessionStartBar;
				RebuildLegFromStart(legStartBar);
				legHigh = ((NinjaScriptBase)this).High[0];
				legLow = ((NinjaScriptBase)this).Low[0];
				legHighBar = ((NinjaScriptBase)this).CurrentBar;
				legLowBar = ((NinjaScriptBase)this).CurrentBar;
				legDir = LegDirection.Unknown;
			}
			if (sessionStartBar < 0)
			{
				sessionStartBar = 0;
				RebuildSessionFrom(sessionStartBar);
			}
			if (legStartBar < 0)
			{
				legStartBar = ((sessionStartBar >= 0) ? sessionStartBar : 0);
				RebuildLegFromStart(legStartBar);
				legHigh = ((NinjaScriptBase)this).High[0];
				legLow = ((NinjaScriptBase)this).Low[0];
				legHighBar = ((NinjaScriptBase)this).CurrentBar;
				legLowBar = ((NinjaScriptBase)this).CurrentBar;
				legDir = LegDirection.Unknown;
			}
			if (ProfileMode != ProfileModes.LegToLeg)
			{
				return;
			}
			if (double.IsNaN(legHigh) || double.IsNaN(legLow))
			{
				legHigh = ((NinjaScriptBase)this).High[0];
				legLow = ((NinjaScriptBase)this).Low[0];
				legHighBar = ((NinjaScriptBase)this).CurrentBar;
				legLowBar = ((NinjaScriptBase)this).CurrentBar;
			}
			else
			{
				if (((NinjaScriptBase)this).High[0] >= legHigh)
				{
					legHigh = ((NinjaScriptBase)this).High[0];
					legHighBar = ((NinjaScriptBase)this).CurrentBar;
				}
				if (((NinjaScriptBase)this).Low[0] <= legLow)
				{
					legLow = ((NinjaScriptBase)this).Low[0];
					legLowBar = ((NinjaScriptBase)this).CurrentBar;
				}
			}
			if (!(RotationPoints > 0.0))
			{
				return;
			}
			double num = ((NinjaScriptBase)this).Close[0];
			if (RotationMode == LegRotationMode.DistanceFromExtreme)
			{
				bool num2 = num <= legHigh - RotationPoints;
				bool flag = num >= legLow + RotationPoints;
				if (num2 || flag)
				{
					StartNewLegAtCurrentBar(flag ? LegDirection.Up : LegDirection.Down);
				}
				return;
			}
			if (legDir == LegDirection.Unknown)
			{
				if (num >= legLow + RotationPoints)
				{
					legDir = LegDirection.Up;
				}
				else if (num <= legHigh - RotationPoints)
				{
					legDir = LegDirection.Down;
				}
			}
			if (legDir == LegDirection.Up)
			{
				if (num <= legHigh - RotationPoints)
				{
					StartNewLegAtBar(legHighBar, LegDirection.Down, num);
				}
			}
			else if (legDir == LegDirection.Down && num >= legLow + RotationPoints)
			{
				StartNewLegAtBar(legLowBar, LegDirection.Up, num);
			}
		}
	}

	private void StartNewLegAtCurrentBar(LegDirection newDir)
	{
		legStartBar = ((NinjaScriptBase)this).CurrentBar;
		legHigh = ((NinjaScriptBase)this).High[0];
		legLow = ((NinjaScriptBase)this).Low[0];
		legHighBar = ((NinjaScriptBase)this).CurrentBar;
		legLowBar = ((NinjaScriptBase)this).CurrentBar;
		legDir = ((RotationMode == LegRotationMode.TrueSwingAlternate) ? newDir : LegDirection.Unknown);
		RebuildLegFromStart(legStartBar);
	}

	private void StartNewLegAtBar(int startBar, LegDirection newDir, double lastPrice)
	{
		legStartBar = Math.Max(0, startBar);
		RebuildLegFromStart(legStartBar);
		legDir = newDir;
		switch (newDir)
		{
		case LegDirection.Down:
			legLow = lastPrice;
			legLowBar = ((NinjaScriptBase)this).CurrentBar;
			break;
		case LegDirection.Up:
			legHigh = lastPrice;
			legHighBar = ((NinjaScriptBase)this).CurrentBar;
			break;
		default:
			legHigh = ((NinjaScriptBase)this).High[0];
			legLow = ((NinjaScriptBase)this).Low[0];
			legHighBar = ((NinjaScriptBase)this).CurrentBar;
			legLowBar = ((NinjaScriptBase)this).CurrentBar;
			break;
		}
	}

	private void EnsureBarMaps(int primaryBarIndex)
	{
		while (barDeltaMaps.Count <= primaryBarIndex)
		{
			barDeltaMaps.Add(new Dictionary<double, long>());
		}
		while (barVolMaps.Count <= primaryBarIndex)
		{
			barVolMaps.Add(new Dictionary<double, long>());
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
		double key = ((NinjaScriptBase)this).Instrument.MasterInstrument.RoundToTickSize(num);
		Dictionary<double, long> dictionary = barVolMaps[bar];
		if (dictionary.TryGetValue(key, out var value))
		{
			dictionary[key] = value + num2;
		}
		else
		{
			dictionary[key] = num2;
		}
		int num3 = ((legStartBar >= 0) ? legStartBar : Math.Max(0, ((NinjaScriptBase)this).BarsArray[0].Count - 1 - RebuildLookbackBarsCap));
		if (bar >= num3)
		{
			if (legVolByPrice.TryGetValue(key, out var value2))
			{
				legVolByPrice[key] = value2 + num2;
			}
			else
			{
				legVolByPrice[key] = num2;
			}
		}
		if (sessionStartBar >= 0 && bar >= sessionStartBar)
		{
			if (sessionVolByPrice.TryGetValue(key, out var value3))
			{
				sessionVolByPrice[key] = value3 + num2;
			}
			else
			{
				sessionVolByPrice[key] = num2;
			}
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
		if (num4 == 0L)
		{
			return;
		}
		Dictionary<double, long> dictionary2 = barDeltaMaps[bar];
		if (dictionary2.TryGetValue(key, out var value4))
		{
			dictionary2[key] = value4 + num4;
		}
		else
		{
			dictionary2[key] = num4;
		}
		if (bar >= num3)
		{
			if (legDeltaByPrice.TryGetValue(key, out var value5))
			{
				legDeltaByPrice[key] = value5 + num4;
			}
			else
			{
				legDeltaByPrice[key] = num4;
			}
		}
		if (sessionStartBar >= 0 && bar >= sessionStartBar)
		{
			if (sessionDeltaByPrice.TryGetValue(key, out var value6))
			{
				sessionDeltaByPrice[key] = value6 + num4;
			}
			else
			{
				sessionDeltaByPrice[key] = num4;
			}
		}
	}

	private void RebuildLegFromStart(int startBar)
	{
		legDeltaByPrice.Clear();
		legVolByPrice.Clear();
		int num = ((NinjaScriptBase)this).BarsArray[0].Count - 1;
		int num2 = Math.Max(0, startBar);
		if (num - num2 > RebuildLookbackBarsCap)
		{
			num2 = num - RebuildLookbackBarsCap;
		}
		for (int i = num2; i <= num; i++)
		{
			if (i < 0 || i >= barDeltaMaps.Count || i >= barVolMaps.Count)
			{
				continue;
			}
			foreach (KeyValuePair<double, long> item in barDeltaMaps[i])
			{
				if (legDeltaByPrice.TryGetValue(item.Key, out var value))
				{
					legDeltaByPrice[item.Key] = value + item.Value;
				}
				else
				{
					legDeltaByPrice[item.Key] = item.Value;
				}
			}
			foreach (KeyValuePair<double, long> item2 in barVolMaps[i])
			{
				if (legVolByPrice.TryGetValue(item2.Key, out var value2))
				{
					legVolByPrice[item2.Key] = value2 + item2.Value;
				}
				else
				{
					legVolByPrice[item2.Key] = item2.Value;
				}
			}
		}
	}

	private void RebuildSessionFrom(int startBar)
	{
		sessionDeltaByPrice.Clear();
		sessionVolByPrice.Clear();
		int num = ((NinjaScriptBase)this).BarsArray[0].Count - 1;
		int num2 = Math.Max(0, startBar);
		if (num - num2 > RebuildLookbackBarsCap)
		{
			num2 = num - RebuildLookbackBarsCap;
		}
		for (int i = num2; i <= num; i++)
		{
			if (i < 0 || i >= barDeltaMaps.Count || i >= barVolMaps.Count)
			{
				continue;
			}
			foreach (KeyValuePair<double, long> item in barDeltaMaps[i])
			{
				if (sessionDeltaByPrice.TryGetValue(item.Key, out var value))
				{
					sessionDeltaByPrice[item.Key] = value + item.Value;
				}
				else
				{
					sessionDeltaByPrice[item.Key] = item.Value;
				}
			}
			foreach (KeyValuePair<double, long> item2 in barVolMaps[i])
			{
				if (sessionVolByPrice.TryGetValue(item2.Key, out var value2))
				{
					sessionVolByPrice[item2.Key] = value2 + item2.Value;
				}
				else
				{
					sessionVolByPrice[item2.Key] = item2.Value;
				}
			}
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		Dictionary<double, long> dictionary = ((ProfileMode == ProfileModes.SessionCurrentDay) ? sessionDeltaByPrice : legDeltaByPrice);
		Dictionary<double, long> dictionary2 = ((ProfileMode == ProfileModes.SessionCurrentDay) ? sessionVolByPrice : legVolByPrice);
		bool flag = dictionary != null && dictionary.Count > 0;
		bool flag2 = ShowVolumeProfile && dictionary2 != null && dictionary2.Count > 0;
		if (!flag && !flag2)
		{
			return;
		}
		EnsureDxResources();
		float num = (float)((double)((IndicatorRenderBase)this).ChartPanel.W * RightReservedPercent);
		float num2 = (float)(chartControl.CanvasRight - RightMarginPx) - num - (float)XOffsetPx;
		float num3 = ((IndicatorRenderBase)this).ChartPanel.Y;
		float num4 = ((IndicatorRenderBase)this).ChartPanel.Y + ((IndicatorRenderBase)this).ChartPanel.H;
		if (ShowSpine)
		{
			RectangleF val = default(RectangleF);
			((RectangleF)(ref val))._002Ector(num2 - (float)SpineWidthPx, num3, (float)SpineWidthPx, num4 - num3);
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(val, (Brush)(object)spineBrushDx);
		}
		float spineX = num2;
		float spineX2 = num2;
		if (flag2 && (VolumeLayer == VolumeProfileLayer.LeftOfDelta || VolumeLayer == VolumeProfileLayer.RightOfDelta))
		{
			if (VolumeLayer == VolumeProfileLayer.LeftOfDelta)
			{
				spineX = num2;
				spineX2 = num2 - (float)MaxProfileWidthPx - (float)SideBySideGapPx;
			}
			else
			{
				spineX2 = num2;
				spineX = num2 - (float)VolumeProfileWidthPx - (float)SideBySideGapPx;
			}
		}
		if (flag2 && VolumeLayer == VolumeProfileLayer.BehindDelta)
		{
			RenderVolumeProfile(chartScale, spineX2, dictionary2);
		}
		if (flag)
		{
			RenderDeltaProfile(chartScale, spineX, dictionary);
		}
		if (flag2 && VolumeLayer == VolumeProfileLayer.InFrontOfDelta)
		{
			RenderVolumeProfile(chartScale, spineX2, dictionary2);
		}
		if (flag2 && (VolumeLayer == VolumeProfileLayer.LeftOfDelta || VolumeLayer == VolumeProfileLayer.RightOfDelta))
		{
			RenderVolumeProfile(chartScale, spineX2, dictionary2);
			if (flag)
			{
				RenderDeltaProfile(chartScale, spineX, dictionary);
			}
		}
	}

	private void RenderDeltaProfile(ChartScale chartScale, float spineX, Dictionary<double, long> activeMap)
	{
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		float num = Math.Abs(chartScale.GetYByValue(((NinjaScriptBase)this).TickSize) - chartScale.GetYByValue(0.0));
		int num2 = 1;
		if (num > 0f && num < (float)MinRowHeightPx)
		{
			num2 = (int)Math.Ceiling((float)MinRowHeightPx / num);
		}
		long num3 = ComputeMaxAbsBin(activeMap, num2);
		if (num3 < MinAbsDeltaToShow)
		{
			return;
		}
		double num4 = ((NinjaScriptBase)this).Instrument.MasterInstrument.RoundToTickSize(activeMap.Keys.Max());
		double num5 = ((NinjaScriptBase)this).Instrument.MasterInstrument.RoundToTickSize(activeMap.Keys.Min());
		float rowHeightPx = Math.Abs(chartScale.GetYByValue(num4) - chartScale.GetYByValue(num4 - ((NinjaScriptBase)this).TickSize * (double)num2));
		int effectiveFontSizeFromRowHeight = GetEffectiveFontSizeFromRowHeight(rowHeightPx);
		EnsureTextFormat(effectiveFontSizeFromRowHeight);
		RectangleF val = default(RectangleF);
		for (double num6 = num4; num6 >= num5 - ((NinjaScriptBase)this).TickSize * 0.5; num6 -= ((NinjaScriptBase)this).TickSize * (double)num2)
		{
			long num7 = 0L;
			for (int i = 0; i < num2; i++)
			{
				double key = num6 - ((NinjaScriptBase)this).TickSize * (double)i;
				if (activeMap.TryGetValue(key, out var value))
				{
					num7 += value;
				}
			}
			long num8 = Math.Abs(num7);
			if (num8 >= MinAbsDeltaToShow)
			{
				float num9 = chartScale.GetYByValue(num6);
				float num10 = chartScale.GetYByValue(num6 - ((NinjaScriptBase)this).TickSize * (double)num2);
				float num11 = Math.Abs(num10 - num9);
				if (num11 < 2f)
				{
					num11 = 2f;
				}
				float num12 = (float)((double)MaxProfileWidthPx * ((double)num8 / (double)num3));
				if (num12 > 0.5f)
				{
					float num13 = Math.Min(num9, num10);
					((RectangleF)(ref val))._002Ector(spineX - num12, num13, num12, num11);
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(val, (Brush)(object)((num7 >= 0) ? posBrushDx : negBrushDx));
					if (effectiveFontSizeFromRowHeight > 0)
					{
						string text = num7.ToString();
						if (num11 >= (float)(effectiveFontSizeFromRowHeight + 2))
						{
							float num14 = MeasureTextWidth(text);
							if (((RectangleF)(ref val)).Width >= num14 + 6f)
							{
								((IndicatorRenderBase)this).RenderTarget.DrawText(text, textFormat, val, (Brush)(object)textBrushDx);
							}
						}
					}
				}
			}
		}
	}

	private long ComputeMaxAbsBin(Dictionary<double, long> activeMap, int groupTicks)
	{
		if (activeMap == null || activeMap.Count == 0)
		{
			return 0L;
		}
		double num = ((NinjaScriptBase)this).Instrument.MasterInstrument.RoundToTickSize(activeMap.Keys.Max());
		double num2 = ((NinjaScriptBase)this).Instrument.MasterInstrument.RoundToTickSize(activeMap.Keys.Min());
		long num3 = 0L;
		for (double num4 = num; num4 >= num2 - ((NinjaScriptBase)this).TickSize * 0.5; num4 -= ((NinjaScriptBase)this).TickSize * (double)groupTicks)
		{
			long num5 = 0L;
			for (int i = 0; i < groupTicks; i++)
			{
				double key = num4 - ((NinjaScriptBase)this).TickSize * (double)i;
				if (activeMap.TryGetValue(key, out var value))
				{
					num5 += value;
				}
			}
			long num6 = Math.Abs(num5);
			if (num6 > num3)
			{
				num3 = num6;
			}
		}
		return num3;
	}

	private int GetEffectiveFontSizeFromRowHeight(float rowHeightPx)
	{
		if (!AutoDeltaText)
		{
			return FontSize;
		}
		if (rowHeightPx < 6f)
		{
			return 0;
		}
		int num = (int)Math.Floor(rowHeightPx * 0.7f);
		if (num < AutoFontMin)
		{
			num = AutoFontMin;
		}
		if (num > AutoFontMax)
		{
			num = AutoFontMax;
		}
		if (rowHeightPx < (float)(num + 2))
		{
			return 0;
		}
		return num;
	}

	private void EnsureTextFormat(int effectiveFont)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		if (effectiveFont > 0 && (textFormat == null || lastFontSize != effectiveFont))
		{
			TextFormat obj = textFormat;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			textFormat = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", (float)effectiveFont)
			{
				TextAlignment = (TextAlignment)2,
				ParagraphAlignment = (ParagraphAlignment)2
			};
			lastFontSize = effectiveFont;
		}
	}

	private void RenderVolumeProfile(ChartScale chartScale, float spineX, Dictionary<double, long> volMap)
	{
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		long num = 0L;
		foreach (KeyValuePair<double, long> item in volMap)
		{
			num = Math.Max(num, item.Value);
		}
		if (num <= 0)
		{
			return;
		}
		double num2 = ((NinjaScriptBase)this).Instrument.MasterInstrument.RoundToTickSize(volMap.Keys.Max());
		double num3 = ((NinjaScriptBase)this).Instrument.MasterInstrument.RoundToTickSize(volMap.Keys.Min());
		RectangleF val = default(RectangleF);
		for (double num4 = num2; num4 >= num3 - ((NinjaScriptBase)this).TickSize * 0.5; num4 -= ((NinjaScriptBase)this).TickSize)
		{
			if (volMap.TryGetValue(num4, out var value) && value > 0)
			{
				float num5 = chartScale.GetYByValue(num4);
				float num6 = chartScale.GetYByValue(num4 - ((NinjaScriptBase)this).TickSize);
				float num7 = Math.Abs(num6 - num5);
				if (num7 < 1f)
				{
					num7 = 1f;
				}
				float num8 = (float)((double)VolumeProfileWidthPx * ((double)value / (double)num));
				if (num8 > 0.5f)
				{
					float num9 = Math.Min(num5, num6);
					((RectangleF)(ref val))._002Ector(spineX - num8, num9, num8, num7);
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(val, (Brush)(object)volBrushDx);
				}
			}
		}
	}

	private void EnsureDxResources()
	{
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Expected O, but got Unknown
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Expected O, but got Unknown
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Expected O, but got Unknown
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Expected O, but got Unknown
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Expected O, but got Unknown
		string text = SafeBrushSerialize(PositiveBrush);
		string text2 = SafeBrushSerialize(NegativeBrush);
		string text3 = SafeBrushSerialize(TextBrush);
		string text4 = SafeBrushSerialize(VolumeBrush);
		if (posBrushDx != null && negBrushDx != null && textBrushDx != null && spineBrushDx != null && volBrushDx != null && !(lastPosBrushSer != text) && !(lastNegBrushSer != text2) && !(lastTextBrushSer != text3) && !(lastVolBrushSer != text4) && !(Math.Abs(lastDeltaOpacity - DeltaOpacity) > 0.0001f) && !(Math.Abs(lastVolOpacity - VolumeOpacity) > 0.0001f))
		{
			return;
		}
		try
		{
			SolidColorBrush obj = posBrushDx;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			SolidColorBrush obj2 = negBrushDx;
			if (obj2 != null)
			{
				((DisposeBase)obj2).Dispose();
			}
			SolidColorBrush obj3 = textBrushDx;
			if (obj3 != null)
			{
				((DisposeBase)obj3).Dispose();
			}
			SolidColorBrush obj4 = spineBrushDx;
			if (obj4 != null)
			{
				((DisposeBase)obj4).Dispose();
			}
			SolidColorBrush obj5 = volBrushDx;
			if (obj5 != null)
			{
				((DisposeBase)obj5).Dispose();
			}
		}
		catch
		{
		}
		posBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(PositiveBrush, DeltaOpacity));
		negBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(NegativeBrush, DeltaOpacity));
		textBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(TextBrush, 1f));
		spineBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4(1f, 1f, 1f, 0.25f));
		volBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(VolumeBrush, VolumeOpacity));
		lastPosBrushSer = text;
		lastNegBrushSer = text2;
		lastTextBrushSer = text3;
		lastVolBrushSer = text4;
		lastDeltaOpacity = DeltaOpacity;
		lastVolOpacity = VolumeOpacity;
	}

	private string SafeBrushSerialize(Brush b)
	{
		try
		{
			return Serialize.BrushToString(b);
		}
		catch
		{
			return b?.ToString() ?? "";
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeDx();
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}

	private float MeasureTextWidth(string text)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (textFormat == null)
		{
			return 0f;
		}
		TextLayout val = new TextLayout(Globals.DirectWriteFactory, text, textFormat, 1000f, 100f);
		try
		{
			return val.Metrics.Width;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static Color BrushToMediaColor(Brush b)
	{
		if (b is SolidColorBrush solidColorBrush)
		{
			return solidColorBrush.Color;
		}
		return Colors.White;
	}

	private Color4 ToDx(Brush b, float alphaMult)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		Color color = BrushToMediaColor(b ?? Brushes.White);
		return new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, (float)(int)color.A / 255f * alphaMult);
	}
}

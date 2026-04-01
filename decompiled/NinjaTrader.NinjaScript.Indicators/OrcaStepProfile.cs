using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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

public class OrcaStepProfile : Indicator
{
	private class StepBlock
	{
		public object SyncObj = new object();

		public DateTime StartTime;

		public DateTime EndTime;

		public int StartBarIndex;

		public int EndBarIndex = -1;

		public double HighPrice;

		public double LowPrice;

		public Dictionary<double, long> VolByPrice = new Dictionary<double, long>();

		public Dictionary<double, long> DeltaByPrice = new Dictionary<double, long>();

		public bool IsVACalculated;

		public double POCPrice = double.NaN;

		public double VAHPrice = double.NaN;

		public double VALPrice = double.NaN;

		public long MaxVol;
	}

	private List<StepBlock> stepBlocks;

	private DateTime previousBarTime = DateTime.MinValue;

	private double lastBid = double.NaN;

	private double lastAsk = double.NaN;

	private double prevLast = double.NaN;

	private SolidColorBrush volBrushDx;

	private SolidColorBrush histVolBrushDx;

	private SolidColorBrush pocBrushDx;

	private SolidColorBrush posDeltaBrushDx;

	private SolidColorBrush negDeltaBrushDx;

	private SolidColorBrush histPosDeltaBrushDx;

	private SolidColorBrush histNegDeltaBrushDx;

	private SolidColorBrush blockSepBrushDx;

	private SolidColorBrush[] volGradientBrushes;

	private SolidColorBrush[] histVolGradientBrushes;

	private int lastBuiltGradientSteps = -1;

	private SolidColorBrush vaVolBrushDx;

	private SolidColorBrush histVaVolBrushDx;

	private SolidColorBrush[] vaGradientBrushes;

	private SolidColorBrush[] histVaGradientBrushes;

	private int lastBuiltVAGradientSteps = -1;

	private SolidColorBrush vaLineBrushDx;

	private StrokeStyle vaLineStrokeDx;

	private SolidColorBrush deltaTextBrushDx;

	private TextFormat deltaTextFormatDx;

	private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();

	private int lastDynamicDeltaComp = -1;

	[NinjaScriptProperty]
	[Display(Name = "Step Interval", GroupName = "Data", Order = 0)]
	public StepIntervalType StepInterval { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Volume Tick Compression", GroupName = "Data", Order = 1)]
	public int VolumeTickCompression { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Delta Tick Compression", GroupName = "Data", Order = 2)]
	public int DeltaTickCompression { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Use Dynamic Aggregation", Description = "Auto adjust delta compression upon zoom", GroupName = "Data", Order = 3)]
	public bool UseDynamicAggregation { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, 10.0)]
	[Display(Name = "Dynamic Aggregation Multiplier", Description = "Lower value = more granular blocks", GroupName = "Data", Order = 4)]
	public double DynamicAggregationMultiplier { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "RTH Only", GroupName = "Data", Order = 4)]
	public bool RTHOnly { get; set; }

	[NinjaScriptProperty]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
	[Display(Name = "RTH Start Time", GroupName = "Data", Order = 5)]
	public DateTime RTHStart { get; set; }

	[NinjaScriptProperty]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
	[Display(Name = "RTH End Time", GroupName = "Data", Order = 6)]
	public DateTime RTHEnd { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Historical Profile Width (px)", GroupName = "Layout", Order = 10)]
	public int HistoricalProfileWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Active Profile Width (px)", GroupName = "Layout", Order = 11)]
	public int ActiveProfileWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Active Delta Width (px)", GroupName = "Layout", Order = 12)]
	public int ActiveDeltaWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Historical Delta Width (px)", GroupName = "Layout", Order = 13)]
	public int HistoricalDeltaWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(0, 500)]
	[Display(Name = "Right Offset (px)", GroupName = "Layout", Order = 14)]
	public int RightOffsetPx { get; set; }

	[NinjaScriptProperty]
	[Range(0, 10)]
	[Display(Name = "Profile Bar Spacing (px)", GroupName = "Layout", Order = 15)]
	public int ProfileBarSpacingPx { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Mirror Profiles", Description = "Draw Delta and Volume profiles in opposite directions from the central axis", GroupName = "Layout", Order = 16)]
	public bool MirrorProfiles { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Active Volume", GroupName = "Visibility", Order = 20)]
	public bool ShowActiveVolume { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Hist Volume", GroupName = "Visibility", Order = 21)]
	public bool ShowHistoricalVolume { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Active Delta", GroupName = "Visibility", Order = 22)]
	public bool ShowActiveDelta { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Historical Delta", GroupName = "Visibility", Order = 23)]
	public bool ShowHistoricalDelta { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show POC", GroupName = "Visibility", Order = 24)]
	public bool ShowPOC { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Block Separators", GroupName = "Visibility", Order = 25)]
	public bool ShowBlockSeparators { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Use Gradient", GroupName = "Gradient", Order = 30)]
	public bool UseGradient { get; set; }

	[NinjaScriptProperty]
	[Range(2, 64)]
	[Display(Name = "Gradient Steps", GroupName = "Gradient", Order = 31)]
	public int GradientSteps { get; set; }

	[NinjaScriptProperty]
	[Range(0.05, 1.0)]
	[Display(Name = "Min Brightness", GroupName = "Gradient", Order = 32)]
	public float MinBrightness { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Value Area", GroupName = "Value Area", Order = 40)]
	public bool ShowValueArea { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VA Color Mode", Description = "Color rows inside the Value Area differently", GroupName = "Value Area", Order = 41)]
	public bool ShowVAColor { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VA Boundary Lines", Description = "Draw lines at VAH and VAL", GroupName = "Value Area", Order = 42)]
	public bool ShowVALines { get; set; }

	[NinjaScriptProperty]
	[Range(50, 95)]
	[Display(Name = "VA Percent", GroupName = "Value Area", Order = 43)]
	public int ValueAreaPercent { get; set; }

	[NinjaScriptProperty]
	[Range(0.5, 6.0)]
	[Display(Name = "VA Line Thickness", GroupName = "Value Area", Order = 44)]
	public float VALineThickness { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VA Line Style", GroupName = "Value Area", Order = 45)]
	public StepVALineStyleEnum VALineStyle { get; set; }

	[XmlIgnore]
	[Display(Name = "VA Color", GroupName = "Value Area", Order = 46)]
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
	[Display(Name = "VA Line Color", GroupName = "Value Area", Order = 47)]
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
	[Display(Name = "Volume Color", GroupName = "Colors", Order = 50)]
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
	[Range(0.1, 1.0)]
	[Display(Name = "Active Volume Opacity", GroupName = "Colors", Order = 51)]
	public float ActiveVolumeOpacity { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, 1.0)]
	[Display(Name = "Hist Volume Opacity", GroupName = "Colors", Order = 52)]
	public float HistoricalVolumeOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "POC Color", GroupName = "Colors", Order = 53)]
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
	[Display(Name = "Positive Delta", GroupName = "Colors", Order = 53)]
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
	[Display(Name = "Negative Delta", GroupName = "Colors", Order = 54)]
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
	[Display(Name = "Active Delta Opacity", GroupName = "Colors", Order = 56)]
	public float ActiveDeltaOpacity { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, 1.0)]
	[Display(Name = "Hist Delta Opacity", GroupName = "Colors", Order = 57)]
	public float HistoricalDeltaOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Block Separator Color", GroupName = "Colors", Order = 58)]
	public Brush BlockSeparatorBrush { get; set; }

	[Browsable(false)]
	public string BlockSeparatorBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(BlockSeparatorBrush);
		}
		set
		{
			BlockSeparatorBrush = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Show Delta Text", GroupName = "Delta Text", Order = 60)]
	public bool ShowDeltaText { get; set; }

	[NinjaScriptProperty]
	[Range(0, 1000000)]
	[Display(Name = "Minimum Threshold", Description = "Minimum delta value (absolute) required to show text", GroupName = "Delta Text", Order = 61)]
	public int DeltaTextMinThreshold { get; set; }

	[NinjaScriptProperty]
	[Range(6, 48)]
	[Display(Name = "Text Font Size", GroupName = "Delta Text", Order = 62)]
	public float DeltaTextFontSize { get; set; }

	[XmlIgnore]
	[Display(Name = "Text Color", GroupName = "Delta Text", Order = 63)]
	public Brush DeltaTextBrush { get; set; }

	[Browsable(false)]
	public string DeltaTextBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(DeltaTextBrush);
		}
		set
		{
			DeltaTextBrush = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Invalid comparison between Unknown and I4
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Invalid comparison between Unknown and I4
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = "OrcaStepProfile";
			((NinjaScript)this).Description = "Time-based volume profiles at fixed intervals with dual volume/delta histograms, gradient, POC, and Value Area.";
			((NinjaScriptBase)this).Calculate = (Calculate)2;
			((NinjaScriptBase)this).IsOverlay = true;
			StepInterval = StepIntervalType.Minutes30;
			VolumeTickCompression = 4;
			DeltaTickCompression = 10;
			UseDynamicAggregation = false;
			DynamicAggregationMultiplier = 1.0;
			RTHOnly = false;
			RTHStart = DateTime.Parse("09:30:00", CultureInfo.InvariantCulture);
			RTHEnd = DateTime.Parse("16:00:00", CultureInfo.InvariantCulture);
			HistoricalProfileWidthPx = 100;
			ActiveProfileWidthPx = 150;
			HistoricalDeltaWidthPx = 60;
			ActiveDeltaWidthPx = 60;
			RightOffsetPx = 60;
			ProfileBarSpacingPx = 0;
			MirrorProfiles = false;
			ShowActiveVolume = true;
			ShowHistoricalVolume = true;
			ShowActiveDelta = true;
			ShowHistoricalDelta = true;
			ShowPOC = true;
			ShowBlockSeparators = true;
			UseGradient = true;
			GradientSteps = 16;
			MinBrightness = 0.2f;
			ShowValueArea = true;
			ShowVAColor = true;
			ShowVALines = true;
			ValueAreaPercent = 70;
			VALineThickness = 1.5f;
			VALineStyle = StepVALineStyleEnum.Dash;
			VolumeBrush = Brushes.RoyalBlue;
			ActiveVolumeOpacity = 0.85f;
			HistoricalVolumeOpacity = 0.5f;
			POCBrush = Brushes.DodgerBlue;
			VABrush = Brushes.CornflowerBlue;
			VALineBrush = Brushes.White;
			PositiveDeltaBrush = Brushes.Lime;
			NegativeDeltaBrush = Brushes.Red;
			ActiveDeltaOpacity = 0.85f;
			HistoricalDeltaOpacity = 0.5f;
			BlockSeparatorBrush = Brushes.DimGray;
			ShowDeltaText = true;
			DeltaTextMinThreshold = 10;
			DeltaTextBrush = Brushes.White;
			DeltaTextFontSize = 11f;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			stepBlocks = new List<StepBlock>(256);
			previousBarTime = DateTime.MinValue;
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
			SolidColorBrush obj = volBrushDx;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			SolidColorBrush obj2 = pocBrushDx;
			if (obj2 != null)
			{
				((DisposeBase)obj2).Dispose();
			}
			SolidColorBrush obj3 = posDeltaBrushDx;
			if (obj3 != null)
			{
				((DisposeBase)obj3).Dispose();
			}
			SolidColorBrush obj4 = negDeltaBrushDx;
			if (obj4 != null)
			{
				((DisposeBase)obj4).Dispose();
			}
			SolidColorBrush obj5 = blockSepBrushDx;
			if (obj5 != null)
			{
				((DisposeBase)obj5).Dispose();
			}
			SolidColorBrush obj6 = vaVolBrushDx;
			if (obj6 != null)
			{
				((DisposeBase)obj6).Dispose();
			}
			SolidColorBrush obj7 = vaLineBrushDx;
			if (obj7 != null)
			{
				((DisposeBase)obj7).Dispose();
			}
			StrokeStyle obj8 = vaLineStrokeDx;
			if (obj8 != null)
			{
				((DisposeBase)obj8).Dispose();
			}
			if (volGradientBrushes != null)
			{
				for (int i = 0; i < volGradientBrushes.Length; i++)
				{
					SolidColorBrush obj9 = volGradientBrushes[i];
					if (obj9 != null)
					{
						((DisposeBase)obj9).Dispose();
					}
				}
			}
			if (vaGradientBrushes == null)
			{
				return;
			}
			for (int j = 0; j < vaGradientBrushes.Length; j++)
			{
				SolidColorBrush obj10 = vaGradientBrushes[j];
				if (obj10 != null)
				{
					((DisposeBase)obj10).Dispose();
				}
			}
		}
		catch
		{
		}
		finally
		{
			volBrushDx = null;
			histVolBrushDx = null;
			pocBrushDx = null;
			posDeltaBrushDx = null;
			negDeltaBrushDx = null;
			histPosDeltaBrushDx = null;
			histNegDeltaBrushDx = null;
			blockSepBrushDx = null;
			vaVolBrushDx = null;
			histVaVolBrushDx = null;
			vaLineBrushDx = null;
			vaLineStrokeDx = null;
			volGradientBrushes = null;
			histVolGradientBrushes = null;
			vaGradientBrushes = null;
			histVaGradientBrushes = null;
			deltaTextBrushDx = null;
			deltaTextFormatDx = null;
			lastBuiltGradientSteps = -1;
			lastBuiltVAGradientSteps = -1;
			textWidthCache.Clear();
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
		if (((NinjaScriptBase)this).BarsInProgress != 1)
		{
			return;
		}
		DateTime dateTime = ((NinjaScriptBase)this).Time[0];
		if (RTHOnly)
		{
			TimeSpan timeOfDay = dateTime.TimeOfDay;
			TimeSpan timeOfDay2 = RTHStart.TimeOfDay;
			TimeSpan timeOfDay3 = RTHEnd.TimeOfDay;
			bool flag = false;
			if (!((!(timeOfDay2 < timeOfDay3)) ? (timeOfDay >= timeOfDay2 || timeOfDay < timeOfDay3) : (timeOfDay >= timeOfDay2 && timeOfDay < timeOfDay3)))
			{
				return;
			}
		}
		if (stepBlocks.Count == 0)
		{
			StartNewBlock(dateTime, ((NinjaScriptBase)this).BarsArray[0].CurrentBar);
		}
		else
		{
			StepBlock stepBlock = stepBlocks[stepBlocks.Count - 1];
			if (dateTime >= stepBlock.EndTime)
			{
				int currentBar = ((NinjaScriptBase)this).BarsArray[0].CurrentBar;
				int num = currentBar;
				if (currentBar >= 0 && ((NinjaScriptBase)this).BarsArray[0].GetTime(currentBar) <= stepBlock.EndTime)
				{
					num = currentBar + 1;
				}
				stepBlock.EndBarIndex = num - 1;
				if (stepBlock.EndBarIndex < stepBlock.StartBarIndex)
				{
					stepBlock.EndBarIndex = stepBlock.StartBarIndex;
				}
				DateTime alignedBlockStart = GetAlignedBlockStart(dateTime, (int)StepInterval);
				StartNewBlock(alignedBlockStart, num);
			}
		}
		if (stepBlocks.Count > 0)
		{
			StepBlock stepBlock2 = stepBlocks[stepBlocks.Count - 1];
			if (stepBlock2.EndBarIndex < 0 || stepBlock2.EndBarIndex < ((NinjaScriptBase)this).BarsArray[0].CurrentBar)
			{
				stepBlock2.EndBarIndex = ((NinjaScriptBase)this).BarsArray[0].CurrentBar;
			}
		}
		previousBarTime = dateTime;
		ProcessTickIntoActiveBlock();
	}

	private DateTime GetAlignedBlockStart(DateTime time, int intervalMinutes)
	{
		if (intervalMinutes >= 1440)
		{
			if (time.Hour < 18)
			{
				return time.Date.AddDays(-1.0).AddHours(18.0);
			}
			return time.Date.AddHours(18.0);
		}
		int num = (time.Hour * 60 + time.Minute) / intervalMinutes * intervalMinutes;
		return time.Date.AddMinutes(num);
	}

	private DateTime GetAlignedBlockEnd(DateTime blockStart, int intervalMinutes)
	{
		if (intervalMinutes >= 1440)
		{
			return blockStart.AddDays(1.0);
		}
		return blockStart.AddMinutes(intervalMinutes);
	}

	private void StartNewBlock(DateTime startTime, int startBarIndex)
	{
		int stepInterval = (int)StepInterval;
		DateTime alignedBlockStart = GetAlignedBlockStart(startTime, stepInterval);
		StepBlock item = new StepBlock
		{
			StartTime = alignedBlockStart,
			EndTime = GetAlignedBlockEnd(alignedBlockStart, stepInterval),
			StartBarIndex = startBarIndex,
			EndBarIndex = -1,
			HighPrice = double.MinValue,
			LowPrice = double.MaxValue
		};
		stepBlocks.Add(item);
	}

	private void ProcessTickIntoActiveBlock()
	{
		if (stepBlocks == null || stepBlocks.Count == 0)
		{
			return;
		}
		StepBlock stepBlock = stepBlocks[stepBlocks.Count - 1];
		double num = ((NinjaScriptBase)this).Close[0];
		long num2 = (long)((NinjaScriptBase)this).Volume[0];
		if (num2 <= 0)
		{
			return;
		}
		if (num > stepBlock.HighPrice)
		{
			stepBlock.HighPrice = num;
		}
		if (num < stepBlock.LowPrice)
		{
			stepBlock.LowPrice = num;
		}
		double num3 = (double)VolumeTickCompression * ((NinjaScriptBase)this).TickSize;
		double key = Math.Floor(num / num3 + 1E-06) * num3;
		lock (stepBlock.SyncObj)
		{
			if (stepBlock.VolByPrice.TryGetValue(key, out var value))
			{
				stepBlock.VolByPrice[key] = value + num2;
			}
			else
			{
				stepBlock.VolByPrice[key] = num2;
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
		double num5 = (double)VolumeTickCompression * ((NinjaScriptBase)this).TickSize;
		double key2 = Math.Floor(num / num5 + 1E-06) * num5;
		lock (stepBlock.SyncObj)
		{
			if (stepBlock.DeltaByPrice.TryGetValue(key2, out var value2))
			{
				stepBlock.DeltaByPrice[key2] = value2 + num4;
			}
			else
			{
				stepBlock.DeltaByPrice[key2] = num4;
			}
		}
	}

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
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		if (stepBlocks == null || stepBlocks.Count == 0 || ((IndicatorRenderBase)this).ChartBars == null)
		{
			return;
		}
		EnsureDxResources();
		int num = DeltaTickCompression;
		if (UseDynamicAggregation)
		{
			ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
			double num2 = (chartScale.MaxValue - chartScale.MinValue) / ((NinjaScriptBase)this).TickSize / (double)Math.Max(1, val.H) * (double)(DeltaTextFontSize + 4f) * DynamicAggregationMultiplier;
			num = ((num2 <= 1.0) ? 1 : ((num2 <= 2.0) ? 2 : ((num2 <= 4.0) ? 4 : ((num2 <= 5.0) ? 5 : ((num2 <= 8.0) ? 8 : ((num2 <= 10.0) ? 10 : ((num2 <= 15.0) ? 15 : ((num2 <= 20.0) ? 20 : ((num2 <= 25.0) ? 25 : ((num2 <= 30.0) ? 30 : ((num2 <= 40.0) ? 40 : ((num2 <= 50.0) ? 50 : ((!(num2 <= 100.0)) ? ((int)(Math.Round(num2 / 50.0) * 50.0)) : ((int)(Math.Round(num2 / 20.0) * 20.0)))))))))))))));
			if (lastDynamicDeltaComp > 0 && (double)Math.Abs(num - lastDynamicDeltaComp) < Math.Max(2.0, (double)num * 0.15))
			{
				num = lastDynamicDeltaComp;
			}
			else
			{
				lastDynamicDeltaComp = num;
			}
		}
		int fromIndex = ((IndicatorRenderBase)this).ChartBars.FromIndex;
		int toIndex = ((IndicatorRenderBase)this).ChartBars.ToIndex;
		float num3 = ((IndicatorRenderBase)this).ChartPanel.Y;
		float num4 = ((IndicatorRenderBase)this).ChartPanel.Y + ((IndicatorRenderBase)this).ChartPanel.H;
		for (int i = 0; i < stepBlocks.Count - 1; i++)
		{
			StepBlock stepBlock = stepBlocks[i];
			if (((stepBlock.EndBarIndex >= 0) ? stepBlock.EndBarIndex : (((NinjaScriptBase)this).BarsArray[0].Count - 1)) >= fromIndex && stepBlock.StartBarIndex <= toIndex)
			{
				float num5 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, Math.Max(stepBlock.StartBarIndex, fromIndex));
				if (ShowHistoricalVolume && stepBlock.VolByPrice.Count > 0)
				{
					DrawBlockProfile(chartControl, chartScale, stepBlock, num5, HistoricalProfileWidthPx, HistoricalDeltaWidthPx, num3, num4, facingRight: true, isActiveProfile: false);
				}
				if (ShowHistoricalDelta && stepBlock.DeltaByPrice.Count > 0)
				{
					DrawBlockDelta(chartControl, chartScale, stepBlock, num5, HistoricalProfileWidthPx, HistoricalDeltaWidthPx, num3, num4, facingRight: true, isActiveProfile: false, num);
				}
				if (ShowBlockSeparators && blockSepBrushDx != null)
				{
					((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num5, num3), new Vector2(num5, num4), (Brush)(object)blockSepBrushDx, 1f);
				}
			}
		}
		if (stepBlocks.Count > 0)
		{
			StepBlock stepBlock2 = stepBlocks[stepBlocks.Count - 1];
			if (ShowActiveVolume && stepBlock2.VolByPrice.Count > 0)
			{
				float baseSpineX = chartControl.CanvasRight - RightOffsetPx;
				DrawBlockProfile(chartControl, chartScale, stepBlock2, baseSpineX, ActiveProfileWidthPx, ActiveDeltaWidthPx, num3, num4, facingRight: false, isActiveProfile: true);
			}
			if (ShowActiveDelta && stepBlock2.DeltaByPrice.Count > 0)
			{
				float baseSpineX2 = chartControl.CanvasRight - RightOffsetPx;
				DrawBlockDelta(chartControl, chartScale, stepBlock2, baseSpineX2, ActiveProfileWidthPx, ActiveDeltaWidthPx, num3, num4, facingRight: false, isActiveProfile: true, num);
			}
			if (ShowBlockSeparators && blockSepBrushDx != null && stepBlock2.StartBarIndex >= fromIndex && stepBlock2.StartBarIndex <= toIndex)
			{
				float num6 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, stepBlock2.StartBarIndex);
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num6, num3), new Vector2(num6, num4), (Brush)(object)blockSepBrushDx, 1f);
			}
		}
	}

	private void DrawBlockProfile(ChartControl chartControl, ChartScale chartScale, StepBlock block, float baseSpineX, int profileWidthPx, int deltaWidthPx, float panelTop, float panelBottom, bool facingRight, bool isActiveProfile)
	{
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_051e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0527: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<double, long> dictionary;
		lock (block.SyncObj)
		{
			if (block.VolByPrice.Count == 0)
			{
				return;
			}
			dictionary = (isActiveProfile ? new Dictionary<double, long>(block.VolByPrice) : block.VolByPrice);
		}
		long num = 0L;
		double num2 = double.NaN;
		double vahPrice = double.NaN;
		double valPrice = double.NaN;
		bool flag = false;
		if (!block.IsVACalculated || isActiveProfile)
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
			if (!isActiveProfile)
			{
				block.MaxVol = num;
				block.POCPrice = num2;
				block.VAHPrice = vahPrice;
				block.VALPrice = valPrice;
				block.IsVACalculated = true;
			}
		}
		else
		{
			num = block.MaxVol;
			num2 = block.POCPrice;
			vahPrice = block.VAHPrice;
			valPrice = block.VALPrice;
			flag = !double.IsNaN(vahPrice);
		}
		if (num <= 0)
		{
			return;
		}
		double num3 = (double)VolumeTickCompression * ((NinjaScriptBase)this).TickSize;
		if ((!isActiveProfile || !ShowActiveVolume) && (isActiveProfile || !ShowHistoricalVolume))
		{
			return;
		}
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
			float num6 = (float)((double)profileWidthPx * ((double)value / (double)num));
			if (num6 < 0.5f)
			{
				continue;
			}
			if (facingRight)
			{
				((RectangleF)(ref val))._002Ector(baseSpineX, num5, num6, (float)num4);
			}
			else if (MirrorProfiles)
			{
				float num7 = baseSpineX - (float)profileWidthPx;
				((RectangleF)(ref val))._002Ector(num7, num5, num6, (float)num4);
			}
			else
			{
				((RectangleF)(ref val))._002Ector(baseSpineX - num6, num5, num6, (float)num4);
			}
			bool flag2 = flag && key >= valPrice - ((NinjaScriptBase)this).TickSize * 0.01 && key <= vahPrice + ((NinjaScriptBase)this).TickSize * 0.01;
			SolidColorBrush val2;
			if (ShowPOC && Math.Abs(key - num2) < ((NinjaScriptBase)this).TickSize * 0.01)
			{
				val2 = pocBrushDx;
			}
			else if (!UseGradient)
			{
				val2 = ((!(ShowValueArea && ShowVAColor && flag2)) ? (isActiveProfile ? volBrushDx : histVolBrushDx) : (isActiveProfile ? vaVolBrushDx : histVaVolBrushDx));
			}
			else
			{
				SolidColorBrush[] array = ((!(ShowValueArea && ShowVAColor && flag2)) ? (isActiveProfile ? volGradientBrushes : histVolGradientBrushes) : (isActiveProfile ? vaGradientBrushes : histVaGradientBrushes));
				if (array != null)
				{
					double num8 = (double)value / (double)num;
					int num9 = array.Length;
					int num10 = (int)(num8 * (double)(num9 - 1));
					if (num10 < 0)
					{
						num10 = 0;
					}
					if (num10 >= num9)
					{
						num10 = num9 - 1;
					}
					val2 = array[num10];
				}
				else
				{
					val2 = (isActiveProfile ? volBrushDx : histVolBrushDx);
				}
			}
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(val, (Brush)(object)val2);
		}
		if (flag && ShowValueArea && ShowVALines && vaLineBrushDx != null)
		{
			float num12;
			float num13;
			if (facingRight)
			{
				int num11 = (isActiveProfile ? ActiveDeltaWidthPx : HistoricalDeltaWidthPx);
				num12 = (MirrorProfiles ? (baseSpineX - (float)num11 - 2f) : (baseSpineX - 2f));
				num13 = baseSpineX + (float)profileWidthPx + 2f;
			}
			else if (MirrorProfiles)
			{
				int num14 = (isActiveProfile ? ActiveDeltaWidthPx : HistoricalDeltaWidthPx);
				float num15 = baseSpineX - (float)profileWidthPx;
				num12 = num15 - (float)num14 - 2f;
				num13 = num15 + (float)profileWidthPx + 2f;
			}
			else
			{
				num12 = baseSpineX - (float)profileWidthPx - 2f;
				num13 = baseSpineX + 2f;
			}
			float num16 = chartScale.GetYByValue(vahPrice + num3);
			if (num16 >= panelTop - 5f && num16 <= panelBottom + 5f)
			{
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num12, num16), new Vector2(num13, num16), (Brush)(object)vaLineBrushDx, VALineThickness, vaLineStrokeDx);
			}
			float num17 = chartScale.GetYByValue(valPrice);
			if (num17 >= panelTop - 5f && num17 <= panelBottom + 5f)
			{
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num12, num17), new Vector2(num13, num17), (Brush)(object)vaLineBrushDx, VALineThickness, vaLineStrokeDx);
			}
		}
	}

	private void DrawBlockDelta(ChartControl chartControl, ChartScale chartScale, StepBlock block, float baseSpineX, int profileWidthPx, int deltaWidthPx, float panelTop, float panelBottom, bool facingRight, bool isActiveProfile, int deltaCompTicks)
	{
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<double, long> dictionary;
		lock (block.SyncObj)
		{
			if (block.DeltaByPrice.Count == 0)
			{
				return;
			}
			dictionary = (isActiveProfile ? new Dictionary<double, long>(block.DeltaByPrice) : block.DeltaByPrice);
		}
		double num = (double)deltaCompTicks * ((NinjaScriptBase)this).TickSize;
		Dictionary<double, long> dictionary2 = new Dictionary<double, long>();
		foreach (KeyValuePair<double, long> item in dictionary)
		{
			double key = Math.Floor(item.Key / num + 1E-06) * num;
			if (dictionary2.TryGetValue(key, out var value))
			{
				dictionary2[key] = value + item.Value;
			}
			else
			{
				dictionary2[key] = item.Value;
			}
		}
		long num2 = 0L;
		foreach (KeyValuePair<double, long> item2 in dictionary2)
		{
			long num3 = Math.Abs(item2.Value);
			if (num3 > num2)
			{
				num2 = num3;
			}
		}
		if (num2 <= 0)
		{
			return;
		}
		RectangleF val2 = default(RectangleF);
		foreach (KeyValuePair<double, long> item3 in dictionary2)
		{
			int yByValue = chartScale.GetYByValue(item3.Key + num);
			int yByValue2 = chartScale.GetYByValue(item3.Key);
			if ((float)yByValue2 < panelTop - 20f || (float)yByValue > panelBottom + 20f)
			{
				continue;
			}
			int num4 = Math.Max(1, Math.Abs(yByValue2 - yByValue) - ProfileBarSpacingPx);
			float num5 = (float)Math.Min(yByValue, yByValue2) + (float)ProfileBarSpacingPx / 2f;
			float num6 = (float)((double)deltaWidthPx * ((double)Math.Abs(item3.Value) / (double)num2));
			if (num6 < 0.5f && (!ShowDeltaText || deltaTextFormatDx == null || deltaTextBrushDx == null))
			{
				continue;
			}
			num6 = Math.Max(num6, 0.5f);
			SolidColorBrush val = ((!isActiveProfile) ? ((item3.Value >= 0) ? histPosDeltaBrushDx : histNegDeltaBrushDx) : ((item3.Value >= 0) ? posDeltaBrushDx : negDeltaBrushDx));
			if (facingRight)
			{
				if (MirrorProfiles)
				{
					((RectangleF)(ref val2))._002Ector(baseSpineX - num6, num5, num6, (float)num4);
				}
				else
				{
					((RectangleF)(ref val2))._002Ector(baseSpineX, num5, num6, (float)num4);
				}
			}
			else if (MirrorProfiles)
			{
				float num7 = baseSpineX - (float)profileWidthPx;
				((RectangleF)(ref val2))._002Ector(num7 - num6, num5, num6, (float)num4);
			}
			else
			{
				((RectangleF)(ref val2))._002Ector(baseSpineX - num6, num5, num6, (float)num4);
			}
			if (num6 >= 0.5f)
			{
				((IndicatorRenderBase)this).RenderTarget.FillRectangle(val2, (Brush)(object)val);
			}
			if (!ShowDeltaText || deltaTextFormatDx == null || deltaTextBrushDx == null)
			{
				continue;
			}
			long num8 = Math.Abs(item3.Value);
			if (!(((RectangleF)(ref val2)).Height >= 6f) || num8 < DeltaTextMinThreshold)
			{
				continue;
			}
			string text = ((item3.Value > 0) ? $"+{item3.Value}" : item3.Value.ToString());
			RectangleF val3 = val2;
			((RectangleF)(ref val3)).Width = Math.Max(1, deltaWidthPx - 4);
			if (facingRight)
			{
				if (MirrorProfiles && !isActiveProfile)
				{
					((RectangleF)(ref val3)).X = baseSpineX - (float)deltaWidthPx;
				}
			}
			else if (MirrorProfiles && isActiveProfile)
			{
				float num9 = baseSpineX - (float)profileWidthPx;
				((RectangleF)(ref val3)).X = num9 - (float)deltaWidthPx;
			}
			else
			{
				((RectangleF)(ref val3)).X = baseSpineX - (float)deltaWidthPx;
			}
			((IndicatorRenderBase)this).RenderTarget.DrawText(text, deltaTextFormatDx, val3, (Brush)(object)deltaTextBrushDx);
		}
	}

	private void EnsureDxResources()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Expected O, but got Unknown
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Expected O, but got Unknown
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Expected O, but got Unknown
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Expected O, but got Unknown
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Expected O, but got Unknown
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Expected O, but got Unknown
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Expected O, but got Unknown
		if (volBrushDx == null)
		{
			volBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(VolumeBrush, ActiveVolumeOpacity));
		}
		if (histVolBrushDx == null)
		{
			histVolBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(VolumeBrush, HistoricalVolumeOpacity));
		}
		if (pocBrushDx == null)
		{
			pocBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(POCBrush, 1f));
		}
		if (posDeltaBrushDx == null)
		{
			posDeltaBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(PositiveDeltaBrush, ActiveDeltaOpacity));
		}
		if (negDeltaBrushDx == null)
		{
			negDeltaBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(NegativeDeltaBrush, ActiveDeltaOpacity));
		}
		if (histPosDeltaBrushDx == null)
		{
			histPosDeltaBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(PositiveDeltaBrush, HistoricalDeltaOpacity));
		}
		if (histNegDeltaBrushDx == null)
		{
			histNegDeltaBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(NegativeDeltaBrush, HistoricalDeltaOpacity));
		}
		if (blockSepBrushDx == null)
		{
			blockSepBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(BlockSeparatorBrush, 0.4f));
		}
		if (vaVolBrushDx == null)
		{
			vaVolBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(VABrush, ActiveVolumeOpacity));
		}
		if (histVaVolBrushDx == null)
		{
			histVaVolBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(VABrush, HistoricalVolumeOpacity));
		}
		if (vaLineBrushDx == null)
		{
			vaLineBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(VALineBrush, 1f));
		}
		if (vaLineStrokeDx == null)
		{
			DashStyle dashStyle = (DashStyle)(VALineStyle switch
			{
				StepVALineStyleEnum.Solid => 0, 
				StepVALineStyleEnum.Dot => 2, 
				StepVALineStyleEnum.DashDot => 3, 
				_ => 1, 
			});
			vaLineStrokeDx = new StrokeStyle(((Resource)((IndicatorRenderBase)this).RenderTarget).Factory, new StrokeStyleProperties
			{
				DashStyle = dashStyle
			});
		}
		if (deltaTextBrushDx == null)
		{
			deltaTextBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(DeltaTextBrush, 1f));
		}
		if (deltaTextFormatDx == null)
		{
			deltaTextFormatDx = new TextFormat(Globals.DirectWriteFactory, "Arial", (FontWeight)400, (FontStyle)0, (FontStretch)5, DeltaTextFontSize)
			{
				TextAlignment = (TextAlignment)1,
				ParagraphAlignment = (ParagraphAlignment)2
			};
		}
		int num = Math.Max(2, GradientSteps);
		if (UseGradient && (volGradientBrushes == null || histVolGradientBrushes == null || lastBuiltGradientSteps != num))
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
			if (histVolGradientBrushes != null)
			{
				for (int j = 0; j < histVolGradientBrushes.Length; j++)
				{
					SolidColorBrush obj2 = histVolGradientBrushes[j];
					if (obj2 != null)
					{
						((DisposeBase)obj2).Dispose();
					}
				}
			}
			volGradientBrushes = BuildGradientPalette(VolumeBrush, num, ActiveVolumeOpacity);
			histVolGradientBrushes = BuildGradientPalette(VolumeBrush, num, HistoricalVolumeOpacity);
			lastBuiltGradientSteps = num;
		}
		if (!UseGradient || !ShowValueArea || !ShowVAColor || (vaGradientBrushes != null && histVaGradientBrushes != null && lastBuiltVAGradientSteps == num))
		{
			return;
		}
		if (vaGradientBrushes != null)
		{
			for (int k = 0; k < vaGradientBrushes.Length; k++)
			{
				SolidColorBrush obj3 = vaGradientBrushes[k];
				if (obj3 != null)
				{
					((DisposeBase)obj3).Dispose();
				}
			}
		}
		if (histVaGradientBrushes != null)
		{
			for (int l = 0; l < histVaGradientBrushes.Length; l++)
			{
				SolidColorBrush obj4 = histVaGradientBrushes[l];
				if (obj4 != null)
				{
					((DisposeBase)obj4).Dispose();
				}
			}
		}
		vaGradientBrushes = BuildGradientPalette(VABrush, num, ActiveVolumeOpacity);
		histVaGradientBrushes = BuildGradientPalette(VABrush, num, HistoricalVolumeOpacity);
		lastBuiltVAGradientSteps = num;
	}

	private SolidColorBrush[] BuildGradientPalette(Brush baseBrush, int steps, float opacity)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		Color color = BrushToMediaColor(baseBrush);
		SolidColorBrush[] array = (SolidColorBrush[])(object)new SolidColorBrush[steps];
		Color4 val = default(Color4);
		for (int i = 0; i < steps; i++)
		{
			float num = (float)i / (float)(steps - 1);
			float num2 = MinBrightness + num * (1f - MinBrightness);
			((Color4)(ref val))._002Ector((float)(int)color.R / 255f * num2, (float)(int)color.G / 255f * num2, (float)(int)color.B / 255f * num2, (float)(int)color.A / 255f * opacity);
			array[i] = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, val);
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

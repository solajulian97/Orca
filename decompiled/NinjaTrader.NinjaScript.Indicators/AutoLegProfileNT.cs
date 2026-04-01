using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators;

public class AutoLegProfileNT : Indicator
{
	private class PriceLeg
	{
		public int StartIndex;

		public int EndIndex;

		public DateTime StartTime;

		public DateTime EndTime;

		public double StartPrice;

		public double EndPrice;

		public double HighPrice;

		public double LowPrice;

		public bool IsUpLeg;

		public Dictionary<double, LevelData> VolumeProfileData = new Dictionary<double, LevelData>();

		public Dictionary<double, LevelData> DeltaProfileData = new Dictionary<double, LevelData>();

		public List<Tuple<DateTime, double>> VwapPoints = new List<Tuple<DateTime, double>>();

		public DateTime LastVwapBarTime = DateTime.MinValue;

		public double TotalVolume;

		public double TotalPV;

		public double CurrentVwapValue;

		public double LegTotalVolume;

		public double MaxVolume;

		public double MaxDeltaAbs;

		public HashSet<double> CachedValueAreaLevels = new HashSet<double>();

		public double LastVAUpdateVolume;

		public void ResetData()
		{
			VolumeProfileData.Clear();
			DeltaProfileData.Clear();
			VwapPoints.Clear();
			CachedValueAreaLevels.Clear();
			LastVAUpdateVolume = 0.0;
			LastVwapBarTime = DateTime.MinValue;
			CurrentVwapValue = 0.0;
			TotalVolume = 0.0;
			TotalPV = 0.0;
			LegTotalVolume = 0.0;
			MaxVolume = 0.0;
			MaxDeltaAbs = 0.0;
		}
	}

	private class LevelData
	{
		public double Volume;

		public double BuyVolume;

		public double SellVolume;

		public double Delta => BuyVolume - SellVolume;
	}

	private List<PriceLeg> completedLegs;

	private PriceLeg currentLeg;

	private double currentExtremePrice;

	private int currentExtremeIndex;

	private DateTime currentExtremeTime;

	private bool isUpLeg;

	private Brush volumeBrush;

	private Brush valueAreaBrush;

	private Brush positiveDeltaBrush;

	private Brush negativeDeltaBrush;

	private Brush vwapBrush;

	private Brush textBrush;

	private Brush labelBackgroundBrush;

	private TextFormat textFormat;

	private double lastBarVolume;

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Reversal Ticks", GroupName = "Parameters", Order = 0)]
	public int ReversalTicks { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Min Leg Ticks", GroupName = "Parameters", Order = 1)]
	public int MinimumLegTicks { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Min Bars Per Leg", GroupName = "Parameters", Order = 2)]
	public int MinimumBarsPerLeg { get; set; }

	[NinjaScriptProperty]
	[Range(0, 1440)]
	[Display(Name = "Min Duration (Min)", GroupName = "Parameters", Order = 3)]
	public int MinimumDurationMinutes { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Vol Compression", GroupName = "Parameters", Order = 4)]
	public int TickCompression { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Delta Compression", GroupName = "Parameters", Order = 5)]
	public int DeltaTickCompression { get; set; }

	[NinjaScriptProperty]
	[Range(1, 50)]
	[Display(Name = "Legs To Display", GroupName = "Parameters", Order = 6)]
	public int LegsToDisplay { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Vol Width", GroupName = "Layout", Order = 7)]
	public int VolumeProfileWidth { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Delta Width", GroupName = "Layout", Order = 8)]
	public int DeltaProfileWidth { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Past Vol Width", GroupName = "Layout", Order = 9)]
	public int PastVolumeWidth { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Past Delta Width", GroupName = "Layout", Order = 10)]
	public int PastDeltaWidth { get; set; }

	[NinjaScriptProperty]
	[Range(0, 500)]
	[Display(Name = "Right Offset", GroupName = "Layout", Order = 11)]
	public int RightOffset { get; set; }

	[NinjaScriptProperty]
	[Range(0, 500)]
	[Display(Name = "Separation", GroupName = "Layout", Order = 12)]
	public int ProfileSeparation { get; set; }

	[NinjaScriptProperty]
	[Range(0, 10)]
	[Display(Name = "Profile Bar Spacing", GroupName = "Layout", Order = 13)]
	public int ProfileBarSpacing { get; set; }

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Merge Overlap %", GroupName = "Logic", Order = 14)]
	public int MergeOverlapPercent { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Mirror Past Profiles", GroupName = "Layout", Order = 15)]
	public bool MirrorPastProfiles { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Volume", GroupName = "Visibility", Order = 16)]
	public bool ShowVolume { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Delta", GroupName = "Visibility", Order = 17)]
	public bool ShowDelta { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Value Area %", GroupName = "Logic", Order = 18)]
	public int ValueAreaPercent { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Current Leg Box", GroupName = "Visibility", Order = 19)]
	public bool ShowCurrentLegBox { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show VWAP", GroupName = "Visibility", Order = 20)]
	public bool ShowVWAP { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Delta Labels", GroupName = "Visibility", Order = 21)]
	public bool ShowDeltaLabels { get; set; }

	[NinjaScriptProperty]
	[Range(5, 50)]
	[Display(Name = "Label Min Height", GroupName = "Layout", Order = 22)]
	public int DeltaLabelMinHeight { get; set; }

	[NinjaScriptProperty]
	[Range(5, 24)]
	[Display(Name = "Delta Label Font Size", GroupName = "Layout", Order = 23)]
	public int DeltaLabelFontSize { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Delta Lbl BG", GroupName = "Visibility", Order = 24)]
	public bool ShowDeltaLabelBackground { get; set; }

	[XmlIgnore]
	[Display(Name = "Pos Delta Color", GroupName = "Colors", Order = 20)]
	public Brush PositiveDeltaColor { get; set; }

	[Browsable(false)]
	public string PositiveDeltaColorSerializable
	{
		get
		{
			return Serialize.BrushToString(PositiveDeltaColor);
		}
		set
		{
			PositiveDeltaColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Neg Delta Color", GroupName = "Colors", Order = 21)]
	public Brush NegativeDeltaColor { get; set; }

	[Browsable(false)]
	public string NegativeDeltaColorSerializable
	{
		get
		{
			return Serialize.BrushToString(NegativeDeltaColor);
		}
		set
		{
			NegativeDeltaColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Vol Color", GroupName = "Colors", Order = 22)]
	public Brush VolumeColor { get; set; }

	[Browsable(false)]
	public string VolumeColorSerializable
	{
		get
		{
			return Serialize.BrushToString(VolumeColor);
		}
		set
		{
			VolumeColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "VA Color", GroupName = "Colors", Order = 23)]
	public Brush ValueAreaColor { get; set; }

	[Browsable(false)]
	public string ValueAreaColorSerializable
	{
		get
		{
			return Serialize.BrushToString(ValueAreaColor);
		}
		set
		{
			ValueAreaColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "VWAP Color", GroupName = "Colors", Order = 29)]
	public Brush VWAPColor { get; set; }

	[Browsable(false)]
	public string VWAPColorSerializable
	{
		get
		{
			return Serialize.BrushToString(VWAPColor);
		}
		set
		{
			VWAPColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Text Color", GroupName = "Colors", Order = 30)]
	public Brush TextColor { get; set; }

	[Browsable(false)]
	public string TextColorSerializable
	{
		get
		{
			return Serialize.BrushToString(TextColor);
		}
		set
		{
			TextColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Label BG Color", GroupName = "Colors", Order = 31)]
	public Brush DeltaLabelBackgroundColor { get; set; }

	[Browsable(false)]
	public string DeltaLabelBackgroundColorSerializable
	{
		get
		{
			return Serialize.BrushToString(DeltaLabelBackgroundColor);
		}
		set
		{
			DeltaLabelBackgroundColor = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Invalid comparison between Unknown and I4
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Automatically creates volume/delta profiles for each price leg";
			((NinjaScriptBase)this).Name = "AutoLegProfileNT";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			ReversalTicks = 20;
			MinimumLegTicks = 20;
			MinimumBarsPerLeg = 1;
			MinimumDurationMinutes = 0;
			TickCompression = 4;
			DeltaTickCompression = 10;
			LegsToDisplay = 10;
			VolumeProfileWidth = 150;
			DeltaProfileWidth = 100;
			PastVolumeWidth = 60;
			PastDeltaWidth = 40;
			RightOffset = 60;
			ProfileSeparation = 20;
			ProfileBarSpacing = 0;
			MergeOverlapPercent = 80;
			MirrorPastProfiles = true;
			ShowVolume = true;
			ShowDelta = true;
			ValueAreaPercent = 70;
			ShowCurrentLegBox = false;
			ShowVWAP = true;
			ShowDeltaLabels = true;
			DeltaLabelMinHeight = 12;
			DeltaLabelFontSize = 12;
			ShowDeltaLabelBackground = true;
			PositiveDeltaColor = Brushes.Lime;
			NegativeDeltaColor = Brushes.Red;
			VolumeColor = Brushes.RoyalBlue;
			ValueAreaColor = Brushes.Gray;
			VWAPColor = Brushes.Magenta;
			TextColor = Brushes.White;
			DeltaLabelBackgroundColor = Brushes.Black;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			completedLegs = new List<PriceLeg>();
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			DisposeResources();
		}
	}

	public override void OnRenderTargetChanged()
	{
		DisposeResources();
	}

	private void DisposeResources()
	{
		if (volumeBrush != null)
		{
			((DisposeBase)volumeBrush).Dispose();
		}
		if (valueAreaBrush != null)
		{
			((DisposeBase)valueAreaBrush).Dispose();
		}
		if (positiveDeltaBrush != null)
		{
			((DisposeBase)positiveDeltaBrush).Dispose();
		}
		if (negativeDeltaBrush != null)
		{
			((DisposeBase)negativeDeltaBrush).Dispose();
		}
		if (vwapBrush != null)
		{
			((DisposeBase)vwapBrush).Dispose();
		}
		if (textBrush != null)
		{
			((DisposeBase)textBrush).Dispose();
		}
		if (labelBackgroundBrush != null)
		{
			((DisposeBase)labelBackgroundBrush).Dispose();
		}
		if (textFormat != null)
		{
			((DisposeBase)textFormat).Dispose();
		}
		volumeBrush = null;
		valueAreaBrush = null;
		positiveDeltaBrush = null;
		negativeDeltaBrush = null;
		vwapBrush = null;
		textBrush = null;
		labelBackgroundBrush = null;
		textFormat = null;
	}

	protected override void OnBarUpdate()
	{
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).CurrentBar < 1)
		{
			return;
		}
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		double num3 = ((NinjaScriptBase)this).Close[0];
		double num4 = ((NinjaScriptBase)this).Volume[0];
		double open = ((NinjaScriptBase)this).Open[0];
		double num5 = ((((NinjaScriptBase)this).CurrentBar == currentLeg?.EndIndex) ? (num4 - lastBarVolume) : num4);
		lastBarVolume = num4;
		DateTime dateTime = ((NinjaScriptBase)this).Time[0];
		if (currentLeg == null)
		{
			isUpLeg = true;
			currentExtremePrice = num;
			currentExtremeIndex = ((NinjaScriptBase)this).CurrentBar;
			currentExtremeTime = dateTime;
			currentLeg = new PriceLeg
			{
				StartIndex = ((NinjaScriptBase)this).CurrentBar,
				StartTime = dateTime,
				StartPrice = num3,
				HighPrice = num,
				LowPrice = num2,
				IsUpLeg = true,
				EndPrice = num3,
				EndTime = dateTime,
				EndIndex = ((NinjaScriptBase)this).CurrentBar
			};
		}
		bool flag = false;
		if (isUpLeg)
		{
			if (num >= currentExtremePrice)
			{
				currentExtremePrice = num;
				currentExtremeIndex = ((NinjaScriptBase)this).CurrentBar;
				currentExtremeTime = dateTime;
				flag = true;
			}
		}
		else if (num2 <= currentExtremePrice)
		{
			currentExtremePrice = num2;
			currentExtremeIndex = ((NinjaScriptBase)this).CurrentBar;
			currentExtremeTime = dateTime;
			flag = true;
		}
		double num6 = (double)ReversalTicks * ((NinjaScriptBase)this).TickSize;
		bool flag2 = false;
		if (!flag)
		{
			bool flag3 = ((NinjaScriptBase)this).CurrentBar - currentLeg.StartIndex >= MinimumBarsPerLeg;
			if (MinimumDurationMinutes > 0)
			{
				flag3 = flag3 && (dateTime - currentLeg.StartTime).TotalMinutes >= (double)MinimumDurationMinutes;
			}
			if (flag3)
			{
				if (isUpLeg && currentExtremePrice - num2 >= num6)
				{
					flag2 = true;
					HandleReversal(toUpLeg: false);
				}
				else if (!isUpLeg && num - currentExtremePrice >= num6)
				{
					flag2 = true;
					HandleReversal(toUpLeg: true);
				}
			}
		}
		if (!flag2 && num5 > 0.0)
		{
			if ((int)((NinjaScript)this).State == 5 && !((NinjaScriptBase)this).IsFirstTickOfBar)
			{
				AccumulateBar(currentLeg, num, num2, open, num3, num5);
			}
			else
			{
				AccumulateTick(currentLeg, num3, num5);
			}
			currentLeg.HighPrice = Math.Max(currentLeg.HighPrice, num);
			currentLeg.LowPrice = Math.Min(currentLeg.LowPrice, num2);
			currentLeg.EndIndex = ((NinjaScriptBase)this).CurrentBar;
			currentLeg.EndTime = dateTime;
			currentLeg.EndPrice = num3;
			UpdateVWAP(currentLeg, num3, num5, dateTime);
			UpdateLegStats(currentLeg);
			if (((NinjaScriptBase)this).IsFirstTickOfBar || currentLeg.LegTotalVolume - currentLeg.LastVAUpdateVolume > 1000.0)
			{
				UpdateValueArea(currentLeg);
			}
		}
	}

	private void UpdateValueArea(PriceLeg leg)
	{
		leg.CachedValueAreaLevels.Clear();
		if (leg.LegTotalVolume == 0.0)
		{
			return;
		}
		double num = leg.LegTotalVolume * ((double)ValueAreaPercent / 100.0);
		double num2 = 0.0;
		foreach (KeyValuePair<double, LevelData> item in leg.VolumeProfileData.OrderByDescending((KeyValuePair<double, LevelData> k) => k.Value.Volume).ToList())
		{
			if (num2 < num)
			{
				leg.CachedValueAreaLevels.Add(item.Key);
				num2 += item.Value.Volume;
				continue;
			}
			break;
		}
		leg.LastVAUpdateVolume = leg.LegTotalVolume;
	}

	private void HandleReversal(bool toUpLeg)
	{
		PriceLeg priceLeg = currentLeg;
		priceLeg.EndIndex = currentExtremeIndex;
		priceLeg.EndTime = currentExtremeTime;
		if (Math.Abs(priceLeg.HighPrice - priceLeg.LowPrice) / ((NinjaScriptBase)this).TickSize >= (double)MinimumLegTicks)
		{
			bool flag = false;
			if (completedLegs.Count > 0 && MergeOverlapPercent > 0)
			{
				PriceLeg priceLeg2 = completedLegs.Last();
				double num = Math.Max(priceLeg2.LowPrice, priceLeg.LowPrice);
				double num2 = Math.Min(priceLeg2.HighPrice, priceLeg.HighPrice);
				double num3 = Math.Max(0.0, num2 - num);
				double num4 = Math.Min(priceLeg2.HighPrice - priceLeg2.LowPrice, priceLeg.HighPrice - priceLeg.LowPrice);
				if (num4 > 0.0 && num3 / num4 * 100.0 >= (double)MergeOverlapPercent)
				{
					MergeLegs(priceLeg2, priceLeg);
					flag = true;
				}
			}
			if (!flag)
			{
				completedLegs.Add(priceLeg);
				if (completedLegs.Count > LegsToDisplay)
				{
					completedLegs.RemoveAt(0);
				}
			}
		}
		isUpLeg = toUpLeg;
		currentLeg = new PriceLeg
		{
			StartIndex = ((NinjaScriptBase)this).CurrentBar,
			StartTime = ((NinjaScriptBase)this).Time[0],
			StartPrice = ((NinjaScriptBase)this).Close[0],
			IsUpLeg = toUpLeg,
			HighPrice = (toUpLeg ? ((NinjaScriptBase)this).High[0] : currentExtremePrice),
			LowPrice = (toUpLeg ? currentExtremePrice : ((NinjaScriptBase)this).Low[0]),
			EndIndex = ((NinjaScriptBase)this).CurrentBar,
			EndTime = ((NinjaScriptBase)this).Time[0],
			EndPrice = ((NinjaScriptBase)this).Close[0]
		};
		currentExtremePrice = (toUpLeg ? ((NinjaScriptBase)this).High[0] : ((NinjaScriptBase)this).Low[0]);
		currentExtremeIndex = ((NinjaScriptBase)this).CurrentBar;
		currentExtremeTime = ((NinjaScriptBase)this).Time[0];
		lastBarVolume = 0.0;
	}

	private void MergeLegs(PriceLeg dest, PriceLeg src)
	{
		dest.HighPrice = Math.Max(dest.HighPrice, src.HighPrice);
		dest.LowPrice = Math.Min(dest.LowPrice, src.LowPrice);
		dest.EndTime = src.EndTime;
		dest.EndIndex = src.EndIndex;
		foreach (KeyValuePair<double, LevelData> volumeProfileDatum in src.VolumeProfileData)
		{
			if (!dest.VolumeProfileData.ContainsKey(volumeProfileDatum.Key))
			{
				dest.VolumeProfileData[volumeProfileDatum.Key] = new LevelData();
			}
			dest.VolumeProfileData[volumeProfileDatum.Key].Volume += volumeProfileDatum.Value.Volume;
			dest.VolumeProfileData[volumeProfileDatum.Key].BuyVolume += volumeProfileDatum.Value.BuyVolume;
			dest.VolumeProfileData[volumeProfileDatum.Key].SellVolume += volumeProfileDatum.Value.SellVolume;
		}
		foreach (KeyValuePair<double, LevelData> deltaProfileDatum in src.DeltaProfileData)
		{
			if (!dest.DeltaProfileData.ContainsKey(deltaProfileDatum.Key))
			{
				dest.DeltaProfileData[deltaProfileDatum.Key] = new LevelData();
			}
			dest.DeltaProfileData[deltaProfileDatum.Key].BuyVolume += deltaProfileDatum.Value.BuyVolume;
			dest.DeltaProfileData[deltaProfileDatum.Key].SellVolume += deltaProfileDatum.Value.SellVolume;
		}
		dest.VwapPoints.AddRange(src.VwapPoints);
		dest.TotalVolume += src.TotalVolume;
		dest.TotalPV += src.TotalPV;
		dest.CurrentVwapValue = src.CurrentVwapValue;
		UpdateLegStats(dest);
		UpdateValueArea(dest);
	}

	private void AccumulateBar(PriceLeg leg, double high, double low, double open, double close, double volume)
	{
		double num = (double)TickCompression * ((NinjaScriptBase)this).TickSize;
		double num2 = Math.Floor(low / num) * num;
		double num3 = Math.Ceiling(high / num) * num;
		for (double num4 = num2; num4 <= num3; num4 += num)
		{
			double key = Math.Floor(num4 / num + 1E-06) * num;
			if (!leg.VolumeProfileData.ContainsKey(key))
			{
				leg.VolumeProfileData[key] = new LevelData();
			}
			double num5 = volume / ((num3 - num2) / num + 1.0);
			leg.VolumeProfileData[key].Volume += num5;
			double num6 = high - low;
			if (num6 > 0.0)
			{
				double num7 = (close - low) / num6;
				leg.VolumeProfileData[key].BuyVolume += num5 * num7;
				leg.VolumeProfileData[key].SellVolume += num5 * (1.0 - num7);
			}
			else
			{
				leg.VolumeProfileData[key].BuyVolume += num5 * 0.5;
				leg.VolumeProfileData[key].SellVolume += num5 * 0.5;
			}
		}
		double num8 = (double)DeltaTickCompression * ((NinjaScriptBase)this).TickSize;
		double num9 = Math.Floor(low / num8) * num8;
		double num10 = Math.Ceiling(high / num8) * num8;
		for (double num11 = num9; num11 <= num10; num11 += num8)
		{
			double key2 = Math.Floor(num11 / num8 + 1E-06) * num8;
			if (!leg.DeltaProfileData.ContainsKey(key2))
			{
				leg.DeltaProfileData[key2] = new LevelData();
			}
			double num12 = volume / ((num10 - num9) / num8 + 1.0);
			if (close > open)
			{
				double num13 = 0.5 + Math.Max(0.0, (close - low) / (high - low) * 0.4);
				leg.DeltaProfileData[key2].BuyVolume += num12 * num13;
				leg.DeltaProfileData[key2].SellVolume += num12 * (1.0 - num13);
			}
			else if (close < open)
			{
				double num14 = 0.5 + Math.Max(0.0, (high - close) / (high - low) * 0.4);
				leg.DeltaProfileData[key2].SellVolume += num12 * num14;
				leg.DeltaProfileData[key2].BuyVolume += num12 * (1.0 - num14);
			}
			else
			{
				leg.DeltaProfileData[key2].BuyVolume += num12 * 0.5;
				leg.DeltaProfileData[key2].SellVolume += num12 * 0.5;
			}
		}
	}

	private void AccumulateTick(PriceLeg leg, double price, double volume)
	{
		double num = (double)TickCompression * ((NinjaScriptBase)this).TickSize;
		double key = Math.Round(price / num) * num;
		if (!leg.VolumeProfileData.ContainsKey(key))
		{
			leg.VolumeProfileData[key] = new LevelData();
		}
		leg.VolumeProfileData[key].Volume += volume;
		double num2 = (double)DeltaTickCompression * ((NinjaScriptBase)this).TickSize;
		double key2 = Math.Round(price / num2) * num2;
		if (!leg.DeltaProfileData.ContainsKey(key2))
		{
			leg.DeltaProfileData[key2] = new LevelData();
		}
		if (((NinjaScriptBase)this).CurrentBar > 0)
		{
			if (MathExtentions.ApproxCompare(price, ((NinjaScriptBase)this).Close[1]) >= 0)
			{
				leg.VolumeProfileData[key].BuyVolume += volume;
				leg.DeltaProfileData[key2].BuyVolume += volume;
			}
			else
			{
				leg.VolumeProfileData[key].SellVolume += volume;
				leg.DeltaProfileData[key2].SellVolume += volume;
			}
		}
		else
		{
			leg.VolumeProfileData[key].BuyVolume += volume * 0.5;
			leg.VolumeProfileData[key].SellVolume += volume * 0.5;
		}
	}

	private void UpdateVWAP(PriceLeg leg, double price, double volume, DateTime time)
	{
		if (!ShowVWAP)
		{
			return;
		}
		leg.TotalVolume += volume;
		leg.TotalPV += price * volume;
		if (leg.TotalVolume > 0.0)
		{
			leg.CurrentVwapValue = leg.TotalPV / leg.TotalVolume;
			if (time > leg.LastVwapBarTime)
			{
				leg.VwapPoints.Add(new Tuple<DateTime, double>(time, leg.CurrentVwapValue));
				leg.LastVwapBarTime = time;
			}
			else if (leg.VwapPoints.Count > 0)
			{
				leg.VwapPoints[leg.VwapPoints.Count - 1] = new Tuple<DateTime, double>(time, leg.CurrentVwapValue);
			}
		}
	}

	private void UpdateLegStats(PriceLeg leg)
	{
		leg.MaxVolume = 0.0;
		leg.LegTotalVolume = 0.0;
		leg.MaxDeltaAbs = 0.0;
		foreach (LevelData value in leg.VolumeProfileData.Values)
		{
			leg.LegTotalVolume += value.Volume;
			if (value.Volume > leg.MaxVolume)
			{
				leg.MaxVolume = value.Volume;
			}
		}
		foreach (LevelData value2 in leg.DeltaProfileData.Values)
		{
			double num = Math.Abs(value2.Delta);
			if (num > leg.MaxDeltaAbs)
			{
				leg.MaxDeltaAbs = num;
			}
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Expected O, but got Unknown
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Expected O, but got Unknown
		if (completedLegs == null)
		{
			return;
		}
		ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
		double num = chartControl.GetXByTime(chartControl.FirstTimePainted);
		double num2 = chartControl.GetXByTime(chartControl.LastTimePainted);
		if (volumeBrush == null)
		{
			volumeBrush = DxExtensions.ToDxBrush(VolumeColor, ((IndicatorRenderBase)this).RenderTarget);
		}
		if (valueAreaBrush == null)
		{
			valueAreaBrush = DxExtensions.ToDxBrush(ValueAreaColor, ((IndicatorRenderBase)this).RenderTarget);
		}
		if (positiveDeltaBrush == null)
		{
			positiveDeltaBrush = DxExtensions.ToDxBrush(PositiveDeltaColor, ((IndicatorRenderBase)this).RenderTarget);
		}
		if (negativeDeltaBrush == null)
		{
			negativeDeltaBrush = DxExtensions.ToDxBrush(NegativeDeltaColor, ((IndicatorRenderBase)this).RenderTarget);
		}
		if (vwapBrush == null)
		{
			vwapBrush = DxExtensions.ToDxBrush(VWAPColor, ((IndicatorRenderBase)this).RenderTarget);
		}
		if (textBrush == null)
		{
			textBrush = DxExtensions.ToDxBrush(TextColor, ((IndicatorRenderBase)this).RenderTarget);
		}
		if (labelBackgroundBrush == null)
		{
			labelBackgroundBrush = DxExtensions.ToDxBrush(DeltaLabelBackgroundColor, ((IndicatorRenderBase)this).RenderTarget);
		}
		if (textFormat == null)
		{
			textFormat = new TextFormat(new Factory(), "Arial", (float)DeltaLabelFontSize);
		}
		foreach (PriceLeg completedLeg in completedLegs)
		{
			int xByTime = chartControl.GetXByTime(completedLeg.StartTime);
			if ((double)(xByTime + PastVolumeWidth + PastDeltaWidth + ProfileSeparation) > num && (double)(xByTime - PastVolumeWidth - PastDeltaWidth - ProfileSeparation) < num2)
			{
				DrawLeg(completedLeg, isCurrent: false, chartControl, chartScale, panel);
			}
		}
		if (currentLeg != null)
		{
			DrawLeg(currentLeg, isCurrent: true, chartControl, chartScale, panel);
		}
	}

	private void DrawLeg(PriceLeg leg, bool isCurrent, ChartControl chartControl, ChartScale chartScale, ChartPanel panel)
	{
		//IL_0575: Unknown result type (might be due to invalid IL or missing references)
		//IL_057a: Unknown result type (might be due to invalid IL or missing references)
		//IL_057f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0586: Expected O, but got Unknown
		//IL_059d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_050b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0436: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		if (leg.VolumeProfileData.Count == 0 && leg.DeltaProfileData.Count == 0)
		{
			return;
		}
		int num = (isCurrent ? VolumeProfileWidth : PastVolumeWidth);
		int num2 = (isCurrent ? DeltaProfileWidth : PastDeltaWidth);
		int num3 = (isCurrent ? (chartControl.CanvasRight - RightOffset) : chartControl.GetXByTime(leg.StartTime));
		int num4 = ((!isCurrent) ? 1 : (-1));
		int num5 = (isCurrent ? (-1) : ((!MirrorPastProfiles) ? 1 : (-1)));
		int num6 = num3;
		int num7 = (isCurrent ? (num3 - num - ProfileSeparation) : (MirrorPastProfiles ? num3 : (num3 + num + ProfileSeparation)));
		if (ShowVolume)
		{
			foreach (KeyValuePair<double, LevelData> volumeProfileDatum in leg.VolumeProfileData)
			{
				int yByValue = chartScale.GetYByValue(volumeProfileDatum.Key);
				if (yByValue >= panel.Y - 50 && yByValue <= panel.Y + panel.H + 50)
				{
					int yByValue2 = chartScale.GetYByValue(volumeProfileDatum.Key + (double)TickCompression * ((NinjaScriptBase)this).TickSize);
					int num8 = Math.Max(2, Math.Abs(yByValue2 - yByValue));
					int num9 = Math.Max(1, num8 - ProfileBarSpacing);
					int num10 = (int)(volumeProfileDatum.Value.Volume / leg.MaxVolume * (double)num);
					int num11 = ((num4 == 1) ? num6 : (num6 - num10));
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF((float)num11, (float)(yByValue - num8 + ProfileBarSpacing / 2), (float)num10, (float)num9), leg.CachedValueAreaLevels.Contains(volumeProfileDatum.Key) ? valueAreaBrush : volumeBrush);
				}
			}
		}
		if (ShowDelta && leg.MaxDeltaAbs > 0.0)
		{
			foreach (KeyValuePair<double, LevelData> deltaProfileDatum in leg.DeltaProfileData)
			{
				int yByValue3 = chartScale.GetYByValue(deltaProfileDatum.Key);
				if (yByValue3 < panel.Y - 50 || yByValue3 > panel.Y + panel.H + 50)
				{
					continue;
				}
				int yByValue4 = chartScale.GetYByValue(deltaProfileDatum.Key + (double)DeltaTickCompression * ((NinjaScriptBase)this).TickSize);
				int num12 = Math.Max(2, Math.Abs(yByValue4 - yByValue3));
				int num13 = Math.Max(1, num12 - ProfileBarSpacing);
				int num14 = (int)(Math.Abs(deltaProfileDatum.Value.Delta) / leg.MaxDeltaAbs * (double)num2);
				if (Math.Abs(deltaProfileDatum.Value.Delta) > 0.0 && num14 == 0)
				{
					num14 = 1;
				}
				int num15 = ((num5 == 1) ? num7 : (num7 - num14));
				float num16 = yByValue3 - num12 + ProfileBarSpacing / 2;
				((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF((float)num15, num16, (float)num14, (float)num13), (deltaProfileDatum.Value.Delta >= 0.0) ? positiveDeltaBrush : negativeDeltaBrush);
				if (ShowDeltaLabels && num13 >= DeltaLabelMinHeight && Math.Abs(deltaProfileDatum.Value.Delta) > 0.0)
				{
					string text = deltaProfileDatum.Value.Delta.ToString("+#;-#;0");
					float num17 = (float)text.Length * ((float)DeltaLabelFontSize * 0.6f);
					float num18 = DeltaLabelFontSize + 2;
					float num19 = (isCurrent ? ((float)(num7 + 2)) : ((num5 == 1) ? ((float)(num15 + num14 + 2)) : ((float)num15 - num17 - 2f)));
					float num20 = num16 + (float)num13 / 2f - num18 / 2f;
					if (ShowDeltaLabelBackground)
					{
						((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(num19 - 1f, num20 - 1f, num17 + 2f, num18 + 2f), labelBackgroundBrush);
					}
					((IndicatorRenderBase)this).RenderTarget.DrawText(text, textFormat, new RectangleF(num19, num20, 200f, num18), textBrush);
				}
			}
		}
		if (ShowVWAP && leg.VwapPoints.Count > 1)
		{
			for (int i = 0; i < leg.VwapPoints.Count - 1; i++)
			{
				float num21 = chartControl.GetXByTime(leg.VwapPoints[i].Item1);
				float num22 = chartScale.GetYByValue(leg.VwapPoints[i].Item2);
				float num23 = chartControl.GetXByTime(leg.VwapPoints[i + 1].Item1);
				float num24 = chartScale.GetYByValue(leg.VwapPoints[i + 1].Item2);
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num21, num22), new Vector2(num23, num24), vwapBrush, 2f);
			}
		}
		if (isCurrent && ShowCurrentLegBox)
		{
			int yByValue5 = chartScale.GetYByValue(leg.HighPrice);
			int yByValue6 = chartScale.GetYByValue(leg.LowPrice);
			int num25 = num + ProfileSeparation + num2;
			Brush val = (Brush)new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, Color.op_Implicit(Color.Yellow));
			try
			{
				((IndicatorRenderBase)this).RenderTarget.DrawRectangle(new RectangleF((float)(num3 - num25), (float)yByValue5, (float)num25, (float)(yByValue6 - yByValue5)), val, 2f);
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}
}

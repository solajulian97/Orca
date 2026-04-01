using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators.PAX;

public class PAX30OpeningRange : Indicator
{
	private struct OrbData
	{
		public double High;

		public double Low;

		public double Mid;

		public bool IsToday;

		public OrbData(double high, double low, double mid, bool isToday)
		{
			High = high;
			Low = low;
			Mid = mid;
			IsToday = isToday;
		}
	}

	private const int DAYS_TO_DISPLAY = 8;

	private DateTime cutoffStartDate = DateTime.MinValue;

	private const int EXTRA_DAYS = 2;

	private double orbHigh;

	private double orbLow;

	private double orbMid;

	private int ORBSeconds;

	private bool inOrbPeriod;

	private DateTime currentOrbDate = DateTime.MinValue;

	private DateTime lastProcessTime = DateTime.MinValue;

	private DateTime lastOrDate = DateTime.MinValue;

	private Dictionary<DateTime, Dictionary<int, DateTime>> upperLevelStartTimes = new Dictionary<DateTime, Dictionary<int, DateTime>>();

	private Dictionary<DateTime, Dictionary<int, DateTime>> lowerLevelStartTimes = new Dictionary<DateTime, Dictionary<int, DateTime>>();

	private DateTime realtimeOrbDate = DateTime.MinValue;

	private Dictionary<string, DateTime> activeLabels = new Dictionary<string, DateTime>();

	private Dictionary<DateTime, OrbData> orbValues = new Dictionary<DateTime, OrbData>();

	private Dictionary<DateTime, List<double>> upperLevels = new Dictionary<DateTime, List<double>>();

	private Dictionary<DateTime, List<double>> lowerLevels = new Dictionary<DateTime, List<double>>();

	private HashSet<string> drawnLevels = new HashSet<string>();

	private bool isValidTimeframe = true;

	[XmlIgnore]
	private SimpleFont cachedFont;

	private DateTime lastCleanupTime = DateTime.MinValue;

	[XmlIgnore]
	[Browsable(false)]
	public TimeSpan ORBStart { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "ORB LocaL Start Time", Order = 2, GroupName = "ORB Parameters")]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditor")]
	[XmlElement("ORBStart")]
	public string ORBStartSerialize
	{
		get
		{
			return ORBStart.ToString();
		}
		set
		{
			ORBStart = TimeSpan.Parse(value);
		}
	}

	[XmlIgnore]
	[Browsable(false)]
	public TimeSpan ORBEndPlot { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "ORB LocaL Line End Time", Order = 5, GroupName = "ORB Parameters")]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditor")]
	[XmlElement("ORBEndPlot")]
	public string ORBEndPlotSerialize
	{
		get
		{
			return ORBEndPlot.ToString();
		}
		set
		{
			ORBEndPlot = TimeSpan.Parse(value);
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "Text Vert Offset ", Order = 7, GroupName = "xyDisplay Settings")]
	[Range(-50, 50)]
	public int TextvertPixels { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Text Horz Offset", Order = 8, GroupName = "xyDisplay Settings")]
	[Range(-100, 100)]
	public int TextHorzOffset { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Font Size", Order = 9, GroupName = "xyDisplay Settings")]
	[Range(6, 36)]
	public int FontSize { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Bold Font", Order = 10, GroupName = "xyDisplay Settings")]
	public bool BoldFont { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Price Label Prefix", Order = 11, GroupName = "xyDisplay Parameters")]
	public string LabelPrefix { get; set; }

	[XmlIgnore]
	[NinjaScriptProperty]
	[Display(Name = "High Line Color", Order = 12, GroupName = "xyORB Colors")]
	public Brush HighLineColor { get; set; }

	[Browsable(false)]
	[XmlElement("HighLineColorSerializable")]
	public string HighLineColorSerializable
	{
		get
		{
			return Serialize.BrushToString(HighLineColor);
		}
		set
		{
			HighLineColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[NinjaScriptProperty]
	[Display(Name = "Low Line Color", Order = 13, GroupName = "xyORB Colors")]
	public Brush LowLineColor { get; set; }

	[Browsable(false)]
	[XmlElement("LowLineColorSerializable")]
	public string LowLineColorSerializable
	{
		get
		{
			return Serialize.BrushToString(LowLineColor);
		}
		set
		{
			LowLineColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[NinjaScriptProperty]
	[Display(Name = "Mid Line Color", Order = 14, GroupName = "xyORB Colors")]
	public Brush MidLineColor { get; set; }

	[Browsable(false)]
	[XmlElement("MidLineColorSerializable")]
	public string MidLineColorSerializable
	{
		get
		{
			return Serialize.BrushToString(MidLineColor);
		}
		set
		{
			MidLineColor = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Display(Name = "High/Low Line Width", Order = 15, GroupName = "xyDisplay Parameters")]
	[Range(1, 10)]
	public int MainLineWidth { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Mid Line Width", Order = 16, GroupName = "xyDisplay Parameters")]
	[Range(1, 10)]
	public int MidLineWidth { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Levels Line Width", Order = 17, GroupName = "xyDisplay Parameters")]
	[Range(1, 10)]
	public int LevelsLineWidth { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Mid Line", Order = 18, GroupName = "xyDisplay Parameters")]
	public bool ShowMid { get; set; }

	/// <summary>
	/// Returns the appropriate level factor based on the instrument symbol
	/// ES/MES = 15 points, NQ/MNQ = 65 points, all others = 0 (no levels)
	/// </summary>
	private double GetMarketLevelFactor()
	{
		if (((NinjaScriptBase)this).Instrument == null || ((NinjaScriptBase)this).Instrument.MasterInstrument == null)
		{
			return 0.0;
		}
		string text = ((NinjaScriptBase)this).Instrument.MasterInstrument.Name.ToUpper();
		if (text.Contains("ES") || text.Contains("MES"))
		{
			return 15.0;
		}
		if (text.Contains("NQ") || text.Contains("MNQ"))
		{
			return 65.0;
		}
		return 0.0;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Invalid comparison between Unknown and I4
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Invalid comparison between Unknown and I4
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Invalid comparison between Unknown and I4
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Invalid comparison between Unknown and I4
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Invalid comparison between Unknown and I4
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Expected O, but got Unknown
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Multi-timeframe 30 second ORB with dynamic levels. Symbol-specific level points. Optimized drawing with real-time label movement.";
			((NinjaScriptBase)this).Name = "PAX30OpeningRange";
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			ORBStart = new TimeSpan(9, 30, 0);
			ORBStartSerialize = ORBStart.ToString();
			ORBSeconds = 30;
			ORBEndPlot = new TimeSpan(17, 0, 0);
			ORBEndPlotSerialize = ORBEndPlot.ToString();
			TextvertPixels = 17;
			TextHorzOffset = 5;
			FontSize = 13;
			BoldFont = false;
			ORBSeconds = 30;
			LabelPrefix = "PAXOR";
			HighLineColor = Brushes.DeepSkyBlue;
			LowLineColor = Brushes.OrangeRed;
			MidLineColor = Brushes.Gold;
			MainLineWidth = 3;
			MidLineWidth = 2;
			LevelsLineWidth = 3;
			ShowMid = false;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			if ((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 4 && (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 3)
			{
				isValidTimeframe = false;
				((NinjaScriptBase)this).Name = "";
				((NinjaScriptBase)this).Calculate = (Calculate)0;
			}
			else
			{
				((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)3, 30);
				((NinjaScriptBase)this).Name = "";
			}
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			if (activeLabels == null)
			{
				activeLabels = new Dictionary<string, DateTime>();
			}
			if (orbValues == null)
			{
				orbValues = new Dictionary<DateTime, OrbData>();
			}
			if (upperLevels == null)
			{
				upperLevels = new Dictionary<DateTime, List<double>>();
			}
			if (lowerLevels == null)
			{
				lowerLevels = new Dictionary<DateTime, List<double>>();
			}
			if (drawnLevels == null)
			{
				drawnLevels = new HashSet<string>();
			}
			cachedFont = new SimpleFont("Arial", FontSize)
			{
				Bold = BoldFont
			};
			if (!isValidTimeframe)
			{
				string timeframeName = GetTimeframeName();
				Draw.TextFixed((NinjaScriptBase)(object)this, "PAXORBWarning", " PAX30OR only supports Minute and Second charts - Disabled on " + timeframeName + " Charts ", TextPosition.Center, Brushes.White, new SimpleFont("Arial", 16)
				{
					Bold = true
				}, Brushes.Transparent, Brushes.DimGray, 100);
			}
			DateTime date = ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date;
			int num = 10;
			cutoffStartDate = date.AddDays(-(num - 1));
		}
		else
		{
			if ((int)((NinjaScript)this).State != 8)
			{
				return;
			}
			if (activeLabels != null)
			{
				activeLabels.Clear();
			}
			if (orbValues != null)
			{
				orbValues.Clear();
			}
			if (upperLevels != null)
			{
				foreach (List<double> value in upperLevels.Values)
				{
					value.Clear();
				}
				upperLevels.Clear();
			}
			if (lowerLevels != null)
			{
				foreach (List<double> value2 in lowerLevels.Values)
				{
					value2.Clear();
				}
				lowerLevels.Clear();
			}
			if (drawnLevels != null)
			{
				drawnLevels.Clear();
			}
		}
	}

	/// <summary>
	/// Returns a user-friendly name for the current chart timeframe
	/// </summary>
	private string GetTimeframeName()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected I4, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		BarsPeriodType barsPeriodType = ((NinjaScriptBase)this).BarsPeriod.BarsPeriodType;
		return (int)barsPeriodType switch
		{
			0 => "Tick", 
			1 => "Volume", 
			2 => "Range", 
			11 => "Renko", 
			5 => "Daily", 
			6 => "Weekly", 
			7 => "Monthly", 
			8 => "Yearly", 
			_ => ((object)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType/*cast due to .constrained prefix*/).ToString(), 
		};
	}

	/// <summary>
	/// Rounds a value to the nearest tick size for the instrument
	/// </summary>
	private double RoundToNearestTick(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			return double.NaN;
		}
		double tickSize = ((NinjaScriptBase)this).TickSize;
		return Math.Round(value / tickSize) * tickSize;
	}

	/// <summary>
	/// Calculates the label time with horizontal offset based on bar interval
	/// </summary>
	private DateTime GetLabelTimeWithOffset(DateTime baseTime, bool isRealtime)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Invalid comparison between Unknown and I4
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Invalid comparison between Unknown and I4
		if (TextHorzOffset == 0)
		{
			return baseTime;
		}
		try
		{
			TimeSpan zero = TimeSpan.Zero;
			BarsPeriod val = ((((NinjaScriptBase)this).BarsArray != null && ((NinjaScriptBase)this).BarsArray.Length != 0 && ((NinjaScriptBase)this).BarsArray[0] != null) ? ((NinjaScriptBase)this).BarsArray[0].BarsPeriod : ((NinjaScriptBase)this).BarsPeriod);
			BarsPeriodType barsPeriodType = val.BarsPeriodType;
			if ((int)barsPeriodType != 3)
			{
				if ((int)barsPeriodType != 4)
				{
					return baseTime;
				}
				zero = TimeSpan.FromMinutes(val.Value * TextHorzOffset);
			}
			else
			{
				zero = TimeSpan.FromSeconds(val.Value * TextHorzOffset);
			}
			return baseTime.Add(zero);
		}
		catch
		{
			return baseTime;
		}
	}

	protected override void OnBarUpdate()
	{
		if (!isValidTimeframe || ((NinjaScriptBase)this).CurrentBar < 1)
		{
			return;
		}
		if (((NinjaScriptBase)this).BarsInProgress == 0)
		{
			if (((NinjaScriptBase)this).IsFirstTickOfBar && activeLabels.Count > 0)
			{
				MoveActiveLabels();
				if (orbValues.ContainsKey(realtimeOrbDate))
				{
					DrawOrbForDay(realtimeOrbDate, isRealtime: true);
				}
			}
		}
		else
		{
			if (((NinjaScriptBase)this).BarsInProgress != 1)
			{
				return;
			}
			DateTime date = ((NinjaScriptBase)this).Time[0].Date;
			if (date < cutoffStartDate)
			{
				return;
			}
			DateTime dateTime = ((NinjaScriptBase)this).Time[0];
			if (dateTime == lastProcessTime)
			{
				return;
			}
			lastProcessTime = dateTime;
			date = dateTime.Date;
			TimeSpan timeOfDay = dateTime.TimeOfDay;
			TimeSpan timeSpan = ORBStart.Add(TimeSpan.FromSeconds(30.0));
			if (timeOfDay == timeSpan && !orbValues.ContainsKey(date))
			{
				currentOrbDate = date;
				orbHigh = ((NinjaScriptBase)this).High[0];
				orbLow = ((NinjaScriptBase)this).Low[0];
				orbMid = RoundToNearestTick(orbLow + (orbHigh - orbLow) * 0.5);
				bool flag = IsCurrentTradingDay(date);
				orbValues[date] = new OrbData(orbHigh, orbLow, orbMid, flag);
				upperLevels[date] = new List<double>();
				lowerLevels[date] = new List<double>();
				double marketLevelFactor = GetMarketLevelFactor();
				if (marketLevelFactor > 0.0)
				{
					upperLevels[date].Add(RoundToNearestTick(orbHigh + marketLevelFactor));
					lowerLevels[date].Add(RoundToNearestTick(orbLow - marketLevelFactor));
				}
				DrawOrbForDay(date, flag);
				if (flag)
				{
					realtimeOrbDate = date;
				}
				inOrbPeriod = false;
			}
			else if (date != currentOrbDate && !orbValues.ContainsKey(date))
			{
				bool flag2 = IsCurrentTradingDay(date);
				if (timeOfDay > timeSpan && timeOfDay <= ORBEndPlot)
				{
					currentOrbDate = date;
					orbHigh = ((NinjaScriptBase)this).High[0];
					orbLow = ((NinjaScriptBase)this).Low[0];
					orbMid = RoundToNearestTick(orbLow + (orbHigh - orbLow) * 0.5);
					orbValues[date] = new OrbData(orbHigh, orbLow, orbMid, flag2);
					upperLevels[date] = new List<double>();
					lowerLevels[date] = new List<double>();
					double marketLevelFactor2 = GetMarketLevelFactor();
					if (marketLevelFactor2 > 0.0)
					{
						upperLevels[date].Add(RoundToNearestTick(orbHigh + marketLevelFactor2));
						lowerLevels[date].Add(RoundToNearestTick(orbLow - marketLevelFactor2));
					}
					DrawOrbForDay(date, flag2);
					if (flag2)
					{
						realtimeOrbDate = date;
					}
					inOrbPeriod = false;
				}
			}
			if (orbValues.ContainsKey(date) && timeOfDay > timeSpan && timeOfDay <= ORBEndPlot)
			{
				CheckAndAddDynamicLevels(date, dateTime);
			}
			if (dateTime.Subtract(lastCleanupTime).TotalHours >= 1.0)
			{
				CleanupOldData(date);
				lastCleanupTime = dateTime;
			}
		}
	}

	/// <summary>
	/// Determines if a given date is the current trading day
	/// </summary>
	private bool IsCurrentTradingDay(DateTime date)
	{
		DateTime now = Globals.Now;
		return date.Date == now.Date;
	}

	/// <summary>
	/// Draws all ORB lines and labels for a specific day
	/// </summary>
	private void DrawOrbForDay(DateTime orbDate, bool isRealtime)
	{
		if (!orbValues.ContainsKey(orbDate))
		{
			return;
		}
		OrbData orbData = orbValues[orbDate];
		double high = orbData.High;
		double low = orbData.Low;
		double mid = orbData.Mid;
		string text = orbDate.ToString("yyyyMMdd");
		DateTime dateTime = orbDate.Add(ORBStart.Add(TimeSpan.FromSeconds(ORBSeconds)));
		DateTime dateTime2 = orbDate.Add(ORBEndPlot);
		DateTime endTime;
		DateTime baseTime;
		if (isRealtime)
		{
			DateTime dateTime3 = ((((NinjaScriptBase)this).Times[0].Count > 0) ? ((NinjaScriptBase)this).Times[0][0] : dateTime);
			endTime = ((dateTime3 < dateTime2) ? dateTime3 : dateTime2);
			baseTime = ((dateTime3 < dateTime2) ? dateTime3 : dateTime2);
		}
		else
		{
			endTime = dateTime2;
			baseTime = dateTime2;
		}
		baseTime = GetLabelTimeWithOffset(baseTime, isRealtime);
		try
		{
			string tag = "PAX_HighLine_" + text;
			Draw.Line((NinjaScriptBase)(object)this, tag, isAutoScale: true, dateTime, high, endTime, high, HighLineColor, (DashStyleHelper)0, MainLineWidth);
			string text2 = "PAX_HighLabel_" + text;
			Draw.Text((NinjaScriptBase)(object)this, text2, isAutoScale: true, LabelPrefix + " " + high.ToString("F2"), baseTime, high, TextvertPixels, HighLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
			if (isRealtime)
			{
				activeLabels[text2] = baseTime;
			}
		}
		catch (Exception)
		{
		}
		try
		{
			string tag2 = "PAX_LowLine_" + text;
			Draw.Line((NinjaScriptBase)(object)this, tag2, isAutoScale: true, dateTime, low, endTime, low, LowLineColor, (DashStyleHelper)0, MainLineWidth);
			string text3 = "PAX_LowLabel_" + text;
			Draw.Text((NinjaScriptBase)(object)this, text3, isAutoScale: true, LabelPrefix + " " + low.ToString("F2"), baseTime, low, TextvertPixels, LowLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
			if (isRealtime)
			{
				activeLabels[text3] = baseTime;
			}
		}
		catch (Exception)
		{
		}
		if (ShowMid)
		{
			string tag3 = "PAX_MidLine_" + text;
			Draw.Line((NinjaScriptBase)(object)this, tag3, isAutoScale: true, dateTime, mid, endTime, mid, MidLineColor, (DashStyleHelper)0, MidLineWidth);
			string text4 = "PAX_MidLabel_" + text;
			Draw.Text((NinjaScriptBase)(object)this, text4, isAutoScale: true, LabelPrefix + " MID " + mid.ToString("F2"), baseTime, mid, TextvertPixels, MidLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
			if (isRealtime)
			{
				activeLabels[text4] = baseTime;
			}
		}
		if (!(GetMarketLevelFactor() > 0.0) || !upperLevels.ContainsKey(orbDate) || !lowerLevels.ContainsKey(orbDate))
		{
			return;
		}
		if (upperLevels[orbDate].Count > 0)
		{
			double num = upperLevels[orbDate][0];
			string tag4 = "PAX_UpperLevel_" + text + "_0";
			Draw.Line((NinjaScriptBase)(object)this, tag4, isAutoScale: true, dateTime, num, endTime, num, HighLineColor, (DashStyleHelper)1, LevelsLineWidth);
			string text5 = "PAX_UpperLabel_" + text + "_0";
			string text6 = LabelPrefix + " " + num.ToString("F2");
			Draw.Text((NinjaScriptBase)(object)this, text5, isAutoScale: true, text6, baseTime, num, TextvertPixels, HighLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
			if (isRealtime)
			{
				activeLabels[text5] = baseTime;
			}
		}
		if (lowerLevels[orbDate].Count > 0)
		{
			double num2 = lowerLevels[orbDate][0];
			string tag5 = "PAX_LowerLevel_" + text + "_0";
			Draw.Line((NinjaScriptBase)(object)this, tag5, isAutoScale: true, dateTime, num2, endTime, num2, LowLineColor, (DashStyleHelper)1, LevelsLineWidth);
			string text7 = "PAX_LowerLabel_" + text + "_0";
			string text8 = LabelPrefix + " " + num2.ToString("F2");
			Draw.Text((NinjaScriptBase)(object)this, text7, isAutoScale: true, text8, baseTime, num2, TextvertPixels, LowLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
			if (isRealtime)
			{
				activeLabels[text7] = baseTime;
			}
		}
	}

	/// <summary>
	/// Checks for price breaks and adds dynamic levels when appropriate
	/// </summary>
	private void CheckAndAddDynamicLevels(DateTime currentDate, DateTime currentTime)
	{
		double marketLevelFactor = GetMarketLevelFactor();
		if (!orbValues.ContainsKey(currentDate) || marketLevelFactor <= 0.0 || !upperLevels.ContainsKey(currentDate) || !lowerLevels.ContainsKey(currentDate))
		{
			return;
		}
		List<double> list = upperLevels[currentDate];
		List<double> list2 = lowerLevels[currentDate];
		if (list.Count <= 0 || list2.Count <= 0)
		{
			return;
		}
		double num = list[list.Count - 1];
		double num2 = list2[list2.Count - 1];
		string text = currentDate.ToString("yyyyMMdd");
		DateTime dateTime = currentDate.Add(ORBEndPlot);
		bool isToday = orbValues[currentDate].IsToday;
		DateTime endTime = (isToday ? currentTime : dateTime);
		if (((NinjaScriptBase)this).High[0] > num)
		{
			double num3 = RoundToNearestTick(num + marketLevelFactor);
			int count = list.Count;
			string text2 = "PAX_UpperLevel_" + text + "_" + count;
			if (!drawnLevels.Contains(text2))
			{
				list.Add(num3);
				drawnLevels.Add(text2);
				if (!upperLevelStartTimes.ContainsKey(currentDate))
				{
					upperLevelStartTimes[currentDate] = new Dictionary<int, DateTime>();
				}
				upperLevelStartTimes[currentDate][count] = currentTime;
				try
				{
					Draw.Line((NinjaScriptBase)(object)this, text2, isAutoScale: true, currentTime, num3, endTime, num3, HighLineColor, (DashStyleHelper)1, LevelsLineWidth);
					DateTime baseTime = (isToday ? currentTime : dateTime);
					baseTime = GetLabelTimeWithOffset(baseTime, isToday);
					string text3 = "PAX_UpperLabel_" + text + "_" + count;
					Draw.Text((NinjaScriptBase)(object)this, text3, isAutoScale: true, LabelPrefix + " " + num3.ToString("F2"), baseTime, num3, TextvertPixels, HighLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
					if (isToday)
					{
						activeLabels[text3] = baseTime;
					}
				}
				catch (Exception)
				{
				}
			}
		}
		if (!(((NinjaScriptBase)this).Low[0] < num2))
		{
			return;
		}
		double num4 = RoundToNearestTick(num2 - marketLevelFactor);
		int count2 = list2.Count;
		string text4 = "PAX_LowerLevel_" + text + "_" + count2;
		if (drawnLevels.Contains(text4))
		{
			return;
		}
		list2.Add(num4);
		drawnLevels.Add(text4);
		if (!lowerLevelStartTimes.ContainsKey(currentDate))
		{
			lowerLevelStartTimes[currentDate] = new Dictionary<int, DateTime>();
		}
		lowerLevelStartTimes[currentDate][count2] = currentTime;
		try
		{
			Draw.Line((NinjaScriptBase)(object)this, text4, isAutoScale: true, currentTime, num4, endTime, num4, LowLineColor, (DashStyleHelper)1, LevelsLineWidth);
			DateTime baseTime2 = (isToday ? currentTime : dateTime);
			baseTime2 = GetLabelTimeWithOffset(baseTime2, isToday);
			string text5 = "PAX_LowerLabel_" + text + "_" + count2;
			Draw.Text((NinjaScriptBase)(object)this, text5, isAutoScale: true, LabelPrefix + " " + num4.ToString("F2"), baseTime2, num4, TextvertPixels, LowLineColor, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
			if (isToday)
			{
				activeLabels[text5] = baseTime2;
			}
		}
		catch (Exception)
		{
		}
	}

	/// <summary>
	/// Moves active labels to follow the current price bar in real-time
	/// </summary>
	private void MoveActiveLabels()
	{
		if (((NinjaScriptBase)this).Times[0].Count < 1 || !orbValues.ContainsKey(realtimeOrbDate))
		{
			return;
		}
		DateTime dateTime = ((NinjaScriptBase)this).Times[0][0];
		DateTime dateTime2 = realtimeOrbDate.Add(ORBEndPlot);
		if (dateTime >= dateTime2)
		{
			return;
		}
		DateTime endTime = ((dateTime < dateTime2) ? dateTime : dateTime2);
		string text = realtimeOrbDate.ToString("yyyyMMdd");
		OrbData orbData = orbValues[realtimeOrbDate];
		DateTime dateTime3 = realtimeOrbDate.Add(ORBStart.Add(TimeSpan.FromSeconds(ORBSeconds)));
		Draw.Line((NinjaScriptBase)(object)this, "PAX_HighLine_" + text, isAutoScale: true, dateTime3, orbData.High, endTime, orbData.High, HighLineColor, (DashStyleHelper)0, MainLineWidth);
		Draw.Line((NinjaScriptBase)(object)this, "PAX_LowLine_" + text, isAutoScale: true, dateTime3, orbData.Low, endTime, orbData.Low, LowLineColor, (DashStyleHelper)0, MainLineWidth);
		if (ShowMid)
		{
			Draw.Line((NinjaScriptBase)(object)this, "PAX_MidLine_" + text, isAutoScale: true, dateTime3, orbData.Mid, endTime, orbData.Mid, MidLineColor, (DashStyleHelper)0, MidLineWidth);
		}
		if (upperLevels.ContainsKey(realtimeOrbDate))
		{
			for (int i = 0; i < upperLevels[realtimeOrbDate].Count; i++)
			{
				DateTime startTime = dateTime3;
				if (i > 0 && upperLevelStartTimes.ContainsKey(realtimeOrbDate) && upperLevelStartTimes[realtimeOrbDate].ContainsKey(i))
				{
					startTime = upperLevelStartTimes[realtimeOrbDate][i];
				}
				Draw.Line((NinjaScriptBase)(object)this, "PAX_UpperLevel_" + text + "_" + i, isAutoScale: true, startTime, upperLevels[realtimeOrbDate][i], endTime, upperLevels[realtimeOrbDate][i], HighLineColor, (DashStyleHelper)1, LevelsLineWidth);
			}
		}
		if (lowerLevels.ContainsKey(realtimeOrbDate))
		{
			for (int j = 0; j < lowerLevels[realtimeOrbDate].Count; j++)
			{
				DateTime startTime2 = dateTime3;
				if (j > 0 && lowerLevelStartTimes.ContainsKey(realtimeOrbDate) && lowerLevelStartTimes[realtimeOrbDate].ContainsKey(j))
				{
					startTime2 = lowerLevelStartTimes[realtimeOrbDate][j];
				}
				Draw.Line((NinjaScriptBase)(object)this, "PAX_LowerLevel_" + text + "_" + j, isAutoScale: true, startTime2, lowerLevels[realtimeOrbDate][j], endTime, lowerLevels[realtimeOrbDate][j], LowLineColor, (DashStyleHelper)1, LevelsLineWidth);
			}
		}
		DateTime labelTimeWithOffset = GetLabelTimeWithOffset(dateTime, isRealtime: true);
		foreach (string item in activeLabels.Keys.ToList())
		{
			double num = 0.0;
			string text2 = "";
			Brush textBrush = Brushes.White;
			if (item.Contains("HighLabel"))
			{
				num = orbData.High;
				text2 = LabelPrefix + " " + num.ToString("F2");
				textBrush = HighLineColor;
			}
			else if (item.Contains("LowLabel"))
			{
				num = orbData.Low;
				text2 = LabelPrefix + " " + num.ToString("F2");
				textBrush = LowLineColor;
			}
			else if (item.Contains("MidLabel"))
			{
				num = orbData.Mid;
				text2 = LabelPrefix + " MID " + num.ToString("F2");
				textBrush = MidLineColor;
			}
			else if (item.Contains("UpperLabel"))
			{
				string[] array = item.Split('_');
				if (array.Length > 1 && int.TryParse(array[array.Length - 1], out var result) && upperLevels.ContainsKey(realtimeOrbDate) && result < upperLevels[realtimeOrbDate].Count)
				{
					num = upperLevels[realtimeOrbDate][result];
					text2 = LabelPrefix + " " + num.ToString("F2");
					textBrush = HighLineColor;
				}
			}
			else if (item.Contains("LowerLabel"))
			{
				string[] array2 = item.Split('_');
				if (array2.Length > 1 && int.TryParse(array2[array2.Length - 1], out var result2) && lowerLevels.ContainsKey(realtimeOrbDate) && result2 < lowerLevels[realtimeOrbDate].Count)
				{
					num = lowerLevels[realtimeOrbDate][result2];
					text2 = LabelPrefix + " " + num.ToString("F2");
					textBrush = LowLineColor;
				}
			}
			if (num > 0.0)
			{
				Draw.Text((NinjaScriptBase)(object)this, item, isAutoScale: true, text2, labelTimeWithOffset, num, TextvertPixels, textBrush, cachedFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
				activeLabels[item] = labelTimeWithOffset;
			}
		}
	}

	/// <summary>
	/// Cleans up old data to prevent memory buildup
	/// </summary>
	private void CleanupOldData(DateTime currentDate)
	{
		foreach (DateTime item in orbValues.Keys.Where((DateTime date) => (currentDate - date).Days >= 8).ToList())
		{
			string dateStr = item.ToString("yyyyMMdd");
			if (item == realtimeOrbDate)
			{
				realtimeOrbDate = DateTime.MinValue;
				activeLabels.Clear();
			}
			drawnLevels.RemoveWhere((string x) => x.Contains(dateStr));
			orbValues.Remove(item);
			if (upperLevels.ContainsKey(item))
			{
				upperLevels[item].Clear();
				upperLevels.Remove(item);
			}
			if (lowerLevels.ContainsKey(item))
			{
				lowerLevels[item].Clear();
				lowerLevels.Remove(item);
			}
			if (upperLevelStartTimes.ContainsKey(item))
			{
				upperLevelStartTimes[item].Clear();
				upperLevelStartTimes.Remove(item);
			}
			if (lowerLevelStartTimes.ContainsKey(item))
			{
				lowerLevelStartTimes[item].Clear();
				lowerLevelStartTimes.Remove(item);
			}
		}
	}
}

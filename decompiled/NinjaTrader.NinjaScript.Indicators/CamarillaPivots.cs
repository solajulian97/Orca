using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators;

[TypeConverter("NinjaTrader.NinjaScript.Indicators.CamarillaPivotsTypeConverter")]
public class CamarillaPivots : Indicator
{
	private DateTime cacheMonthlyEndDate = Globals.MinDate;

	private DateTime cacheSessionDate = Globals.MinDate;

	private DateTime cacheSessionEnd = Globals.MinDate;

	private DateTime cacheTime;

	private DateTime cacheWeeklyEndDate = Globals.MinDate;

	private DateTime currentDate = Globals.MinDate;

	private DateTime currentMonth = Globals.MinDate;

	private DateTime currentWeek = Globals.MinDate;

	private DateTime sessionDateTmp = Globals.MinDate;

	private HLCCalculationMode priorDayHlc;

	private PivotRange pivotRangeType;

	private SessionIterator storedSession;

	private double currentClose;

	private double currentHigh = double.MinValue;

	private double currentLow = double.MaxValue;

	private double dailyBarClose = double.MinValue;

	private double dailyBarHigh = double.MinValue;

	private double dailyBarLow = double.MinValue;

	private double r1;

	private double r2;

	private double r3;

	private double r4;

	private double s1;

	private double s2;

	private double s3;

	private double s4;

	private int cacheBar;

	private readonly List<int> newSessionBarIdxArr = new List<int>();

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "PivotRange", GroupName = "NinjaScriptParameters", Order = 0)]
	public PivotRange PivotRangeType
	{
		get
		{
			return pivotRangeType;
		}
		set
		{
			pivotRangeType = value;
		}
	}

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "HLCCalculationMode", GroupName = "NinjaScriptParameters", Order = 1)]
	[RefreshProperties(RefreshProperties.All)]
	public HLCCalculationMode PriorDayHlc
	{
		get
		{
			return priorDayHlc;
		}
		set
		{
			priorDayHlc = value;
		}
	}

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> R1 => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> R2 => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> R3 => ((NinjaScriptBase)this).Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> R4 => ((NinjaScriptBase)this).Values[3];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> S1 => ((NinjaScriptBase)this).Values[4];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> S2 => ((NinjaScriptBase)this).Values[5];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> S3 => ((NinjaScriptBase)this).Values[6];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> S4 => ((NinjaScriptBase)this).Values[7];

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "UserDefinedClose", GroupName = "NinjaScriptParameters", Order = 2)]
	public double UserDefinedClose { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "UserDefinedHigh", GroupName = "NinjaScriptParameters", Order = 3)]
	public double UserDefinedHigh { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "UserDefinedLow", GroupName = "NinjaScriptParameters", Order = 4)]
	public double UserDefinedLow { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Width", GroupName = "NinjaScriptParameters", Order = 5)]
	public int Width { get; set; } = 20;

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Invalid comparison between Unknown and I4
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Invalid comparison between Unknown and I4
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Invalid comparison between Unknown and I4
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Expected O, but got Unknown
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Invalid comparison between Unknown and I4
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Invalid comparison between Unknown and I4
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Invalid comparison between Unknown and I4
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Invalid comparison between Unknown and I4
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Invalid comparison between Unknown and I4
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Invalid comparison between Unknown and I4
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Invalid comparison between Unknown and I4
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Invalid comparison between Unknown and I4
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Invalid comparison between Unknown and I4
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Invalid comparison between Unknown and I4
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Invalid comparison between Unknown and I4
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Invalid comparison between Unknown and I4
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Invalid comparison between Unknown and I4
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Invalid comparison between Unknown and I4
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionCamarillaPivots;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameCamarillaPivots;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).IsAutoScale = false;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.PivotsR1);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.PivotsR2);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.PivotsR3);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.PivotsR4);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.PivotsS1);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.PivotsS2);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.PivotsS3);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.PivotsS4);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			if (priorDayHlc == HLCCalculationMode.DailyBars)
			{
				((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)5, 1);
			}
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			storedSession = new SessionIterator(((NinjaScriptBase)this).Bars);
		}
		else
		{
			if ((int)((NinjaScript)this).State != 5)
			{
				return;
			}
			if (priorDayHlc == HLCCalculationMode.DailyBars && ((NinjaScriptBase)this).BarsArray[1].DayCount <= 0)
			{
				Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.PiviotsDailyDataError, TextPosition.BottomRight);
				NinjaScript.Log(Resource.PiviotsDailyDataError, (LogLevel)3);
				return;
			}
			if (!((NinjaScriptBase)this).Bars.BarsType.IsIntraday && (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 5 && (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 9 && (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 16 && (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 14) || (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType != 5))
			{
				Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.PiviotsDailyBarsError, TextPosition.BottomRight);
				NinjaScript.Log(Resource.PiviotsDailyBarsError, (LogLevel)3);
			}
			if (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 5 || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 9 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 16 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 14) && (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType == 5)) && pivotRangeType == PivotRange.Daily)
			{
				Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.PiviotsWeeklyBarsError, TextPosition.BottomRight);
				NinjaScript.Log(Resource.PiviotsWeeklyBarsError, (LogLevel)3);
			}
			if (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 5 || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 9 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 16 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 14) && (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType == 5)) && ((NinjaScriptBase)this).BarsPeriod.Value > 1)
			{
				Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.PiviotsPeriodTypeError, TextPosition.BottomRight);
				NinjaScript.Log(Resource.PiviotsPeriodTypeError, (LogLevel)3);
			}
			if ((priorDayHlc == HLCCalculationMode.DailyBars && ((pivotRangeType == PivotRange.Monthly && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddMonths(-1)) || (pivotRangeType == PivotRange.Weekly && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddDays(-7.0)) || (pivotRangeType == PivotRange.Daily && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddDays(-1.0)))) || (pivotRangeType == PivotRange.Monthly && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddMonths(-1)) || (pivotRangeType == PivotRange.Weekly && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddDays(-7.0)) || (pivotRangeType == PivotRange.Daily && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddDays(-1.0)))
			{
				Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.PiviotsInsufficentDataError, TextPosition.BottomRight);
				NinjaScript.Log(Resource.PiviotsInsufficentDataError, (LogLevel)3);
			}
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Invalid comparison between Unknown and I4
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Invalid comparison between Unknown and I4
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Invalid comparison between Unknown and I4
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Invalid comparison between Unknown and I4
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Invalid comparison between Unknown and I4
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Invalid comparison between Unknown and I4
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Invalid comparison between Unknown and I4
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Invalid comparison between Unknown and I4
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Invalid comparison between Unknown and I4
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Invalid comparison between Unknown and I4
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Invalid comparison between Unknown and I4
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Invalid comparison between Unknown and I4
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Invalid comparison between Unknown and I4
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Invalid comparison between Unknown and I4
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).BarsInProgress != 0 || (priorDayHlc == HLCCalculationMode.DailyBars && ((NinjaScriptBase)this).BarsArray[1].DayCount <= 0) || (!((NinjaScriptBase)this).Bars.BarsType.IsIntraday && (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 5 && (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 9 && (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 16 && (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 14) || (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType != 5)) || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 5 || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 9 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 16 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 14) && (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType == 5)) && pivotRangeType == PivotRange.Daily) || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 5 || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 9 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 16 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 14) && (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType == 5)) && ((NinjaScriptBase)this).BarsPeriod.Value > 1) || (priorDayHlc == HLCCalculationMode.DailyBars && ((pivotRangeType == PivotRange.Monthly && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddMonths(-1)) || (pivotRangeType == PivotRange.Weekly && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddDays(-7.0)) || (pivotRangeType == PivotRange.Daily && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddDays(-1.0)))) || (pivotRangeType == PivotRange.Monthly && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddMonths(-1)) || (pivotRangeType == PivotRange.Weekly && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddDays(-7.0)) || (pivotRangeType == PivotRange.Daily && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddDays(-1.0)))
		{
			return;
		}
		((IndicatorRenderBase)this).RemoveDrawObject("NinjaScriptInfo");
		if (PriorDayHlc == HLCCalculationMode.DailyBars && ((NinjaScriptBase)this).CurrentBars[1] >= 0)
		{
			if (cacheTime != ((NinjaScriptBase)this).Times[0][0])
			{
				cacheTime = ((NinjaScriptBase)this).Times[0][0];
				cacheBar = ((NinjaScriptBase)this).BarsArray[1].GetBar(((NinjaScriptBase)this).Times[0][0]);
			}
			dailyBarHigh = ((NinjaScriptBase)this).BarsArray[1].GetHigh(cacheBar);
			dailyBarLow = ((NinjaScriptBase)this).BarsArray[1].GetLow(cacheBar);
			dailyBarClose = ((NinjaScriptBase)this).BarsArray[1].GetClose(cacheBar);
		}
		else
		{
			dailyBarHigh = double.MinValue;
			dailyBarLow = double.MinValue;
			dailyBarClose = double.MinValue;
		}
		double num = ((dailyBarHigh <= double.MinValue) ? ((NinjaScriptBase)this).Highs[0][0] : dailyBarHigh);
		double num2 = ((dailyBarLow <= double.MinValue) ? ((NinjaScriptBase)this).Lows[0][0] : dailyBarLow);
		double num3 = ((dailyBarClose <= double.MinValue) ? ((NinjaScriptBase)this).Closes[0][0] : dailyBarClose);
		DateTime lastBarSessionDate = GetLastBarSessionDate(((NinjaScriptBase)this).Times[0][0], pivotRangeType);
		if ((currentDate != Globals.MinDate && pivotRangeType == PivotRange.Daily && lastBarSessionDate != currentDate) || (currentWeek != Globals.MinDate && pivotRangeType == PivotRange.Weekly && lastBarSessionDate != currentWeek) || (currentMonth != Globals.MinDate && pivotRangeType == PivotRange.Monthly && lastBarSessionDate != currentMonth))
		{
			s1 = currentClose - (currentHigh - currentLow) * 1.1 / 12.0;
			r1 = currentClose + (currentHigh - currentLow) * 1.1 / 12.0;
			s2 = currentClose - (currentHigh - currentLow) * 1.1 / 6.0;
			r2 = currentClose + (currentHigh - currentLow) * 1.1 / 6.0;
			s3 = currentClose - (currentHigh - currentLow) * 1.1 / 4.0;
			r3 = currentClose + (currentHigh - currentLow) * 1.1 / 4.0;
			s4 = currentClose - (currentHigh - currentLow) * 1.1 / 2.0;
			r4 = currentClose + (currentHigh - currentLow) * 1.1 / 2.0;
			currentClose = ((priorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedClose : num3);
			currentHigh = ((priorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedHigh : num);
			currentLow = ((priorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedLow : num2);
		}
		else
		{
			currentClose = ((priorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedClose : num3);
			currentHigh = ((priorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedHigh : Math.Max(currentHigh, num));
			currentLow = ((priorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedLow : Math.Min(currentLow, num2));
		}
		if (pivotRangeType == PivotRange.Daily)
		{
			currentDate = lastBarSessionDate;
		}
		if (pivotRangeType == PivotRange.Weekly)
		{
			currentWeek = lastBarSessionDate;
		}
		if (pivotRangeType == PivotRange.Monthly)
		{
			currentMonth = lastBarSessionDate;
		}
		if ((pivotRangeType == PivotRange.Daily && currentDate != Globals.MinDate) || (pivotRangeType == PivotRange.Weekly && currentWeek != Globals.MinDate) || (pivotRangeType == PivotRange.Monthly && currentMonth != Globals.MinDate))
		{
			R1[0] = r1;
			R2[0] = r2;
			R3[0] = r3;
			R4[0] = r4;
			S1[0] = s1;
			S2[0] = s2;
			S3[0] = s3;
			S4[0] = s4;
		}
	}

	private DateTime GetLastBarSessionDate(DateTime time, PivotRange pivotRange)
	{
		if (time > cacheSessionEnd)
		{
			if (((NinjaScriptBase)this).Bars.BarsType.IsIntraday)
			{
				storedSession.GetNextSession(time, true);
				cacheSessionEnd = storedSession.ActualSessionEnd;
				sessionDateTmp = TimeZoneInfo.ConvertTime(cacheSessionEnd.AddSeconds(-1.0), Globals.GeneralOptions.TimeZoneInfo, ((NinjaScriptBase)this).Bars.TradingHours.TimeZoneInfo).Date;
			}
			else
			{
				sessionDateTmp = time.Date;
			}
		}
		if (pivotRange == PivotRange.Daily)
		{
			if (sessionDateTmp != cacheSessionDate)
			{
				if (newSessionBarIdxArr.Count == 0 || (newSessionBarIdxArr.Count > 0 && ((NinjaScriptBase)this).CurrentBar > newSessionBarIdxArr[newSessionBarIdxArr.Count - 1]))
				{
					newSessionBarIdxArr.Add(((NinjaScriptBase)this).CurrentBar);
				}
				cacheSessionDate = sessionDateTmp;
			}
			return sessionDateTmp;
		}
		DateTime dateTime = RoundUpTimeToPeriodTime(sessionDateTmp, PivotRange.Weekly);
		if (pivotRange == PivotRange.Weekly)
		{
			if (dateTime != cacheWeeklyEndDate)
			{
				if (newSessionBarIdxArr.Count == 0 || (newSessionBarIdxArr.Count > 0 && ((NinjaScriptBase)this).CurrentBar > newSessionBarIdxArr[newSessionBarIdxArr.Count - 1]))
				{
					newSessionBarIdxArr.Add(((NinjaScriptBase)this).CurrentBar);
				}
				cacheWeeklyEndDate = dateTime;
			}
			return dateTime;
		}
		DateTime dateTime2 = RoundUpTimeToPeriodTime(sessionDateTmp, PivotRange.Monthly);
		if (dateTime2 != cacheMonthlyEndDate)
		{
			if (newSessionBarIdxArr.Count == 0 || (newSessionBarIdxArr.Count > 0 && ((NinjaScriptBase)this).CurrentBar > newSessionBarIdxArr[newSessionBarIdxArr.Count - 1]))
			{
				newSessionBarIdxArr.Add(((NinjaScriptBase)this).CurrentBar);
			}
			cacheMonthlyEndDate = dateTime2;
		}
		return dateTime2;
	}

	private DateTime RoundUpTimeToPeriodTime(DateTime time, PivotRange pivotRange)
	{
		return pivotRange switch
		{
			PivotRange.Monthly => Extensions.GetEndOfMonthTime(time), 
			PivotRange.Weekly => Extensions.GetEndOfWeekTime(time), 
			_ => time, 
		};
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Expected O, but got Unknown
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		TextFormat val = chartControl.Properties.LabelFont.ToDirectWriteTextFormat();
		for (int i = 0; i < ((NinjaScriptBase)this).Values.Length; i++)
		{
			double y = -1.0;
			double x = -1.0;
			double x2 = -1.0;
			int num = -1;
			int fromIndex = ((IndicatorRenderBase)this).ChartBars.FromIndex;
			int toIndex = ((IndicatorRenderBase)this).ChartBars.ToIndex;
			Plot val2 = ((NinjaScriptBase)this).Plots[i];
			for (int num2 = newSessionBarIdxArr.Count - 1; num2 >= 0; num2--)
			{
				int num3 = newSessionBarIdxArr[num2];
				if (num3 <= toIndex)
				{
					num = num3;
					break;
				}
			}
			int num4 = toIndex;
			while (num4 >= Math.Max(fromIndex, toIndex - Width) && num4 >= num)
			{
				x = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, num4);
				x2 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, toIndex);
				double valueAt = ((NinjaScriptBase)this).Values[i].GetValueAt(num4);
				y = chartScale.GetYByValue(valueAt);
				num4--;
			}
			Point point = new Point(x, y);
			Point point2 = new Point(x2, y);
			((IndicatorRenderBase)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point), DxExtensions.ToVector2(point2), ((Stroke)val2).BrushDX, ((Stroke)val2).Width, ((Stroke)val2).StrokeStyle);
			TextLayout val3 = new TextLayout(Globals.DirectWriteFactory, val2.Name, val, (float)((IndicatorRenderBase)this).ChartPanel.W, val.FontSize);
			((IndicatorRenderBase)this).RenderTarget.DrawTextLayout(DxExtensions.ToVector2(point), val3, ((Stroke)val2).BrushDX);
			((DisposeBase)val3).Dispose();
		}
		((DisposeBase)val).Dispose();
	}
}

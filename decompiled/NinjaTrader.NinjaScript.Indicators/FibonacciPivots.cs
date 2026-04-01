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

[TypeConverter("NinjaTrader.NinjaScript.Indicators.FibonacciPivotsTypeConverter")]
public class FibonacciPivots : Indicator
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

	private SessionIterator storedSession;

	private double currentClose;

	private double currentHigh = double.MinValue;

	private double currentLow = double.MaxValue;

	private double dailyBarClose = double.MinValue;

	private double dailyBarHigh = double.MinValue;

	private double dailyBarLow = double.MinValue;

	private double pp;

	private double r1;

	private double r2;

	private double r3;

	private double s1;

	private double s2;

	private double s3;

	private int cacheBar;

	private readonly List<int> newSessionBarIdxArr = new List<int>();

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "PivotRange", GroupName = "NinjaScriptParameters", Order = 0)]
	public PivotRange PivotRangeType { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Pp => ((NinjaScriptBase)this).Values[0];

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "HLCCalculationMode", GroupName = "NinjaScriptParameters", Order = 1)]
	[RefreshProperties(RefreshProperties.All)]
	public HLCCalculationMode PriorDayHlc { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> R1 => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> R2 => ((NinjaScriptBase)this).Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> R3 => ((NinjaScriptBase)this).Values[3];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> S1 => ((NinjaScriptBase)this).Values[4];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> S2 => ((NinjaScriptBase)this).Values[5];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> S3 => ((NinjaScriptBase)this).Values[6];

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
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Invalid comparison between Unknown and I4
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Invalid comparison between Unknown and I4
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Invalid comparison between Unknown and I4
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Expected O, but got Unknown
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Invalid comparison between Unknown and I4
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Invalid comparison between Unknown and I4
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Invalid comparison between Unknown and I4
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Invalid comparison between Unknown and I4
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Invalid comparison between Unknown and I4
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Invalid comparison between Unknown and I4
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Invalid comparison between Unknown and I4
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Invalid comparison between Unknown and I4
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Invalid comparison between Unknown and I4
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Invalid comparison between Unknown and I4
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Invalid comparison between Unknown and I4
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Invalid comparison between Unknown and I4
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Invalid comparison between Unknown and I4
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Invalid comparison between Unknown and I4
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionFibonacciPivots;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameFibonacciPivots;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).IsAutoScale = false;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.PivotsPP);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.PivotsR1);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.PivotsR2);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.PivotsR3);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.PivotsS1);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.PivotsS2);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.PivotsS3);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			if (PriorDayHlc == HLCCalculationMode.DailyBars)
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
			if (PriorDayHlc == HLCCalculationMode.DailyBars && ((NinjaScriptBase)this).BarsArray[1].DayCount <= 0)
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
			if (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 5 || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 9 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 16 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 14) && (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType == 5)) && PivotRangeType == PivotRange.Daily)
			{
				Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.PiviotsWeeklyBarsError, TextPosition.BottomRight);
				NinjaScript.Log(Resource.PiviotsWeeklyBarsError, (LogLevel)3);
			}
			if (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 5 || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 9 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 16 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 14) && (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType == 5)) && ((NinjaScriptBase)this).BarsPeriod.Value > 1)
			{
				Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.PiviotsPeriodTypeError, TextPosition.BottomRight);
				NinjaScript.Log(Resource.PiviotsPeriodTypeError, (LogLevel)3);
			}
			if ((PriorDayHlc == HLCCalculationMode.DailyBars && ((PivotRangeType == PivotRange.Monthly && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddMonths(-1)) || (PivotRangeType == PivotRange.Weekly && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddDays(-7.0)) || (PivotRangeType == PivotRange.Daily && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddDays(-1.0)))) || (PivotRangeType == PivotRange.Monthly && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddMonths(-1)) || (PivotRangeType == PivotRange.Weekly && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddDays(-7.0)) || (PivotRangeType == PivotRange.Daily && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddDays(-1.0)))
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
		if (((NinjaScriptBase)this).BarsInProgress != 0 || (PriorDayHlc == HLCCalculationMode.DailyBars && ((NinjaScriptBase)this).BarsArray[1].DayCount <= 0) || (!((NinjaScriptBase)this).Bars.BarsType.IsIntraday && (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 5 && (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 9 && (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 16 && (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType != 14) || (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType != 5)) || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 5 || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 9 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 16 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 14) && (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType == 5)) && PivotRangeType == PivotRange.Daily) || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 5 || (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 9 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 16 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 14) && (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType == 5)) && ((NinjaScriptBase)this).BarsPeriod.Value > 1) || (PriorDayHlc == HLCCalculationMode.DailyBars && ((PivotRangeType == PivotRange.Monthly && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddMonths(-1)) || (PivotRangeType == PivotRange.Weekly && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddDays(-7.0)) || (PivotRangeType == PivotRange.Daily && ((NinjaScriptBase)this).BarsArray[1].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[1].GetTime(((NinjaScriptBase)this).BarsArray[1].Count - 1).Date.AddDays(-1.0)))) || (PivotRangeType == PivotRange.Monthly && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddMonths(-1)) || (PivotRangeType == PivotRange.Weekly && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddDays(-7.0)) || (PivotRangeType == PivotRange.Daily && ((NinjaScriptBase)this).BarsArray[0].GetTime(0).Date >= ((NinjaScriptBase)this).BarsArray[0].GetTime(((NinjaScriptBase)this).BarsArray[0].Count - 1).Date.AddDays(-1.0)))
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
		DateTime lastBarSessionDate = GetLastBarSessionDate(((NinjaScriptBase)this).Times[0][0], PivotRangeType);
		if ((currentDate != Globals.MinDate && PivotRangeType == PivotRange.Daily && lastBarSessionDate != currentDate) || (currentWeek != Globals.MinDate && PivotRangeType == PivotRange.Weekly && lastBarSessionDate != currentWeek) || (currentMonth != Globals.MinDate && PivotRangeType == PivotRange.Monthly && lastBarSessionDate != currentMonth))
		{
			pp = (currentHigh + currentLow + currentClose) / 3.0;
			s1 = pp - (currentHigh - currentLow) * 0.382;
			r1 = pp + (currentHigh - currentLow) * 0.382;
			s2 = pp - (currentHigh - currentLow) * 0.618;
			r2 = pp + (currentHigh - currentLow) * 0.618;
			s3 = pp - (currentHigh - currentLow) * 1.0;
			r3 = pp + (currentHigh - currentLow) * 1.0;
			currentClose = ((PriorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedClose : num3);
			currentHigh = ((PriorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedHigh : num);
			currentLow = ((PriorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedLow : num2);
		}
		else
		{
			currentClose = ((PriorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedClose : num3);
			currentHigh = ((PriorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedHigh : Math.Max(currentHigh, num));
			currentLow = ((PriorDayHlc == HLCCalculationMode.UserDefinedValues) ? UserDefinedLow : Math.Min(currentLow, num2));
		}
		if (PivotRangeType == PivotRange.Daily)
		{
			currentDate = lastBarSessionDate;
		}
		if (PivotRangeType == PivotRange.Weekly)
		{
			currentWeek = lastBarSessionDate;
		}
		if (PivotRangeType == PivotRange.Monthly)
		{
			currentMonth = lastBarSessionDate;
		}
		if ((PivotRangeType == PivotRange.Daily && currentDate != Globals.MinDate) || (PivotRangeType == PivotRange.Weekly && currentWeek != Globals.MinDate) || (PivotRangeType == PivotRange.Monthly && currentMonth != Globals.MinDate))
		{
			Pp[0] = pp;
			R1[0] = r1;
			R2[0] = r2;
			R3[0] = r3;
			S1[0] = s1;
			S2[0] = s2;
			S3[0] = s3;
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
			PivotRange.Weekly => Extensions.GetEndOfWeekTime(time), 
			PivotRange.Monthly => Extensions.GetEndOfMonthTime(time), 
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

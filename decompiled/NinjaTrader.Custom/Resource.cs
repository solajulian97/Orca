using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace NinjaTrader.Custom;

/// <summary>
///   A strongly-typed resource class, for looking up localized strings, etc.
/// </summary>
[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
public class Resource
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	/// <summary>
	///   Returns the cached ResourceManager instance used by this class.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				resourceMan = new ResourceManager("NinjaTrader.Custom.Resource", typeof(Resource).Assembly);
			}
			return resourceMan;
		}
	}

	/// <summary>
	///   Overrides the current thread's CurrentUICulture property for all
	///   resource lookups using this strongly typed resource class.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	/// <summary>
	///   Looks up a localized string similar to Acceleration.
	/// </summary>
	public static string Acceleration => ResourceManager.GetString("Acceleration", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Acceleration max.
	/// </summary>
	public static string AccelerationMax => ResourceManager.GetString("AccelerationMax", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Acceleration step.
	/// </summary>
	public static string AccelerationStep => ResourceManager.GetString("AccelerationStep", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to AD.
	/// </summary>
	public static string ADLAD => ResourceManager.GetString("ADLAD", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Alert on break.
	/// </summary>
	public static string AlertOnBreak => ResourceManager.GetString("AlertOnBreak", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Alert on break sound.
	/// </summary>
	public static string AlertOnBreakSound => ResourceManager.GetString("AlertOnBreakSound", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Modified Schiff.
	/// </summary>
	public static string AndrewsPitchforkCalculationMethod_ModifiedSchiff => ResourceManager.GetString("AndrewsPitchforkCalculationMethod_ModifiedSchiff", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Schiff.
	/// </summary>
	public static string AndrewsPitchforkCalculationMethod_Schiff => ResourceManager.GetString("AndrewsPitchforkCalculationMethod_Schiff", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Standard.
	/// </summary>
	public static string AndrewsPitchforkCalculationMethod_StandardPitchfork => ResourceManager.GetString("AndrewsPitchforkCalculationMethod_StandardPitchfork", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ask line length (% of chart).
	/// </summary>
	public static string AskLineLength => ResourceManager.GetString("AskLineLength", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ask line.
	/// </summary>
	public static string AskLineStroke => ResourceManager.GetString("AskLineStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Copyright &lt;sup&gt;©&lt;/sup&gt; {0}. All rights reserved. NinjaTrader and the NinjaTrader logo. Reg. U.S. Pat. &amp;amp; Tm. Off..
	/// </summary>
	public static string AuthDisclosureText1 => ResourceManager.GetString("AuthDisclosureText1", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to FULL RISK DISCLOSURE: Futures and forex trading contains substantial risk and is not for every investor. An investor could potentially lose all or more than the initial investment. Risk capital is money that can be lost without jeopardizing ones financial security or lifestyle. Only risk capital should be used for trading and only those with sufficient risk capital should consider trading. Past performance is not necessarily indicative of future results..
	/// </summary>
	public static string AuthDisclosureText2 => ResourceManager.GetString("AuthDisclosureText2", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Band percent.
	/// </summary>
	public static string BandPct => ResourceManager.GetString("BandPct", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bar count.
	/// </summary>
	public static string BarCount => ResourceManager.GetString("BarCount", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bar down.
	/// </summary>
	public static string BarDown => ResourceManager.GetString("BarDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bar spacing.
	/// </summary>
	public static string BarSpacing => ResourceManager.GetString("BarSpacing", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bars period type.
	/// </summary>
	public static string BarsPeriodType => ResourceManager.GetString("BarsPeriodType", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Day.
	/// </summary>
	public static string BarsPeriodTypeNameDay => ResourceManager.GetString("BarsPeriodTypeNameDay", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Heiken-Ashi.
	/// </summary>
	public static string BarsPeriodTypeNameHeikenAshi => ResourceManager.GetString("BarsPeriodTypeNameHeikenAshi", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Kagi.
	/// </summary>
	public static string BarsPeriodTypeNameKagi => ResourceManager.GetString("BarsPeriodTypeNameKagi", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line Break.
	/// </summary>
	public static string BarsPeriodTypeNameLineBreak => ResourceManager.GetString("BarsPeriodTypeNameLineBreak", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Minute.
	/// </summary>
	public static string BarsPeriodTypeNameMinute => ResourceManager.GetString("BarsPeriodTypeNameMinute", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Month.
	/// </summary>
	public static string BarsPeriodTypeNameMonth => ResourceManager.GetString("BarsPeriodTypeNameMonth", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Point and Figure.
	/// </summary>
	public static string BarsPeriodTypeNamePointAndFigure => ResourceManager.GetString("BarsPeriodTypeNamePointAndFigure", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Range.
	/// </summary>
	public static string BarsPeriodTypeNameRange => ResourceManager.GetString("BarsPeriodTypeNameRange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Renko.
	/// </summary>
	public static string BarsPeriodTypeNameRenko => ResourceManager.GetString("BarsPeriodTypeNameRenko", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Second.
	/// </summary>
	public static string BarsPeriodTypeNameSecond => ResourceManager.GetString("BarsPeriodTypeNameSecond", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Tick.
	/// </summary>
	public static string BarsPeriodTypeNameTick => ResourceManager.GetString("BarsPeriodTypeNameTick", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume.
	/// </summary>
	public static string BarsPeriodTypeNameVolume => ResourceManager.GetString("BarsPeriodTypeNameVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Week.
	/// </summary>
	public static string BarsPeriodTypeNameWeek => ResourceManager.GetString("BarsPeriodTypeNameWeek", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Year.
	/// </summary>
	public static string BarsPeriodTypeNameYear => ResourceManager.GetString("BarsPeriodTypeNameYear", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bars period value.
	/// </summary>
	public static string BarsPeriodValue => ResourceManager.GetString("BarsPeriodValue", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bar timer disabled since you are currently disconnected from a data provider.
	/// </summary>
	public static string BarTimerDisconnectedError => ResourceManager.GetString("BarTimerDisconnectedError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bar timer disabled since the current time is outside session time or chart end date.
	/// </summary>
	public static string BarTimerSessionTimeError => ResourceManager.GetString("BarTimerSessionTimeError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bar timer only works on intraday time based intervals.
	/// </summary>
	public static string BarTimerTimeBasedError => ResourceManager.GetString("BarTimerTimeBasedError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Time remaining = .
	/// </summary>
	public static string BarTimerTimeRemaining => ResourceManager.GetString("BarTimerTimeRemaining", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to BarTimer waiting for realtime data before starting.
	/// </summary>
	public static string BarTimerWaitingOnDataError => ResourceManager.GetString("BarTimerWaitingOnDataError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bar up.
	/// </summary>
	public static string BarUp => ResourceManager.GetString("BarUp", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Base period.
	/// </summary>
	public static string BasePeriod => ResourceManager.GetString("BasePeriod", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid line length (% of chart).
	/// </summary>
	public static string BidLineLength => ResourceManager.GetString("BidLineLength", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid line.
	/// </summary>
	public static string BidLineStroke => ResourceManager.GetString("BidLineStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Block trade size.
	/// </summary>
	public static string BlockTradeSize => ResourceManager.GetString("BlockTradeSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Lower band.
	/// </summary>
	public static string BollingerLowerBand => ResourceManager.GetString("BollingerLowerBand", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Middle band.
	/// </summary>
	public static string BollingerMiddleBand => ResourceManager.GetString("BollingerMiddleBand", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Upper band.
	/// </summary>
	public static string BollingerUpperBand => ResourceManager.GetString("BollingerUpperBand", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Buy pressure.
	/// </summary>
	public static string BuySellPressureBuyPressure => ResourceManager.GetString("BuySellPressureBuyPressure", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sell pressure.
	/// </summary>
	public static string BuySellPressureSellPressure => ResourceManager.GetString("BuySellPressureSellPressure", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Buys.
	/// </summary>
	public static string BuySellVolumeBuys => ResourceManager.GetString("BuySellVolumeBuys", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sells.
	/// </summary>
	public static string BuySellVolumeSells => ResourceManager.GetString("BuySellVolumeSells", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Pattern found.
	/// </summary>
	public static string CandlestickPatternFound => ResourceManager.GetString("CandlestickPatternFound", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Level 1.
	/// </summary>
	public static string CCILevel1 => ResourceManager.GetString("CCILevel1", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Level 2.
	/// </summary>
	public static string CCILevel2 => ResourceManager.GetString("CCILevel2", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Level -1.
	/// </summary>
	public static string CCILevelMinus1 => ResourceManager.GetString("CCILevelMinus1", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Level -2.
	/// </summary>
	public static string CCILevelMinus2 => ResourceManager.GetString("CCILevelMinus2", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 1 Day.
	/// </summary>
	public static string ChartSpan_Day => ResourceManager.GetString("ChartSpan_Day", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 1 min.
	/// </summary>
	public static string ChartSpan_Min1 => ResourceManager.GetString("ChartSpan_Min1", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 15 min.
	/// </summary>
	public static string ChartSpan_Min15 => ResourceManager.GetString("ChartSpan_Min15", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 240 min.
	/// </summary>
	public static string ChartSpan_Min240 => ResourceManager.GetString("ChartSpan_Min240", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 30 min.
	/// </summary>
	public static string ChartSpan_Min30 => ResourceManager.GetString("ChartSpan_Min30", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 5 min.
	/// </summary>
	public static string ChartSpan_Min5 => ResourceManager.GetString("ChartSpan_Min5", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 60 min.
	/// </summary>
	public static string ChartSpan_Min60 => ResourceManager.GetString("ChartSpan_Min60", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 1 Month.
	/// </summary>
	public static string ChartSpan_Month => ResourceManager.GetString("ChartSpan_Month", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 1 Week.
	/// </summary>
	public static string ChartSpan_Week => ResourceManager.GetString("ChartSpan_Week", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 1 Year.
	/// </summary>
	public static string ChartSpan_Year => ResourceManager.GetString("ChartSpan_Year", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line 1.
	/// </summary>
	public static string ConstantLines1 => ResourceManager.GetString("ConstantLines1", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line 2.
	/// </summary>
	public static string ConstantLines2 => ResourceManager.GetString("ConstantLines2", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line 3.
	/// </summary>
	public static string ConstantLines3 => ResourceManager.GetString("ConstantLines3", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line 4.
	/// </summary>
	public static string ConstantLines4 => ResourceManager.GetString("ConstantLines4", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to COT 1.
	/// </summary>
	public static string COT1 => ResourceManager.GetString("COT1", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to COT 2.
	/// </summary>
	public static string COT2 => ResourceManager.GetString("COT2", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to COT 3.
	/// </summary>
	public static string COT3 => ResourceManager.GetString("COT3", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to COT 4.
	/// </summary>
	public static string COT4 => ResourceManager.GetString("COT4", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to COT 5.
	/// </summary>
	public static string COT5 => ResourceManager.GetString("COT5", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to COT data is not supported for this instrument.
	/// </summary>
	public static string CotDataError => ResourceManager.GetString("CotDataError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to COT data is still being downloaded. Please refresh the indicator in few moments..
	/// </summary>
	public static string CotDataStillDownloading => ResourceManager.GetString("CotDataStillDownloading", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to "Download COT data at startup" must be enabled in Settings to receive the latest data.
	/// </summary>
	public static string CotDataWarning => ResourceManager.GetString("CotDataWarning", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Count down.
	/// </summary>
	public static string CountDown => ResourceManager.GetString("CountDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Trades.
	/// </summary>
	public static string CountType_Trades => ResourceManager.GetString("CountType_Trades", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume.
	/// </summary>
	public static string CountType_Volume => ResourceManager.GetString("CountType_Volume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to CurrentDayOHL only works on intraday intervals.
	/// </summary>
	public static string CurrentDayOHLError => ResourceManager.GetString("CurrentDayOHLError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current high.
	/// </summary>
	public static string CurrentDayOHLHigh => ResourceManager.GetString("CurrentDayOHLHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current low.
	/// </summary>
	public static string CurrentDayOHLLow => ResourceManager.GetString("CurrentDayOHLLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current open.
	/// </summary>
	public static string CurrentDayOHLOpen => ResourceManager.GetString("CurrentDayOHLOpen", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Buy Market.
	/// </summary>
	public static string CustomWindowAddOnBuyMarket => ResourceManager.GetString("CustomWindowAddOnBuyMarket", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sell Market.
	/// </summary>
	public static string CustomWindowAddOnSellMarket => ResourceManager.GetString("CustomWindowAddOnSellMarket", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Custom Window Description.
	/// </summary>
	public static string CustomWindowSampleDescription => ResourceManager.GetString("CustomWindowSampleDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Custom Window Sample.
	/// </summary>
	public static string CustomWindowSampleName => ResourceManager.GetString("CustomWindowSampleName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Daily.
	/// </summary>
	public static string DataBarsTypeDaily => ResourceManager.GetString("DataBarsTypeDaily", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Day.
	/// </summary>
	public static string DataBarsTypeDay => ResourceManager.GetString("DataBarsTypeDay", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Minute{1}.
	/// </summary>
	public static string DataBarsTypeMinute => ResourceManager.GetString("DataBarsTypeMinute", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Month.
	/// </summary>
	public static string DataBarsTypeMonth => ResourceManager.GetString("DataBarsTypeMonth", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Monthly.
	/// </summary>
	public static string DataBarsTypeMonthly => ResourceManager.GetString("DataBarsTypeMonthly", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Point and Figure.
	/// </summary>
	public static string DataBarsTypePointAndFigure => ResourceManager.GetString("DataBarsTypePointAndFigure", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Range{1}.
	/// </summary>
	public static string DataBarsTypeRange => ResourceManager.GetString("DataBarsTypeRange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Renko.
	/// </summary>
	public static string DataBarsTypeRenko => ResourceManager.GetString("DataBarsTypeRenko", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Second.
	/// </summary>
	public static string DataBarsTypeSecond => ResourceManager.GetString("DataBarsTypeSecond", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Tick{1}.
	/// </summary>
	public static string DataBarsTypeTick => ResourceManager.GetString("DataBarsTypeTick", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Volume{1}.
	/// </summary>
	public static string DataBarsTypeVolume => ResourceManager.GetString("DataBarsTypeVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Week.
	/// </summary>
	public static string DataBarsTypeWeek => ResourceManager.GetString("DataBarsTypeWeek", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Weekly.
	/// </summary>
	public static string DataBarsTypeWeekly => ResourceManager.GetString("DataBarsTypeWeekly", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Year.
	/// </summary>
	public static string DataBarsTypeYear => ResourceManager.GetString("DataBarsTypeYear", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Yearly.
	/// </summary>
	public static string DataBarsTypeYearly => ResourceManager.GetString("DataBarsTypeYearly", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Day.
	/// </summary>
	public static string Day => ResourceManager.GetString("Day", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Days.
	/// </summary>
	public static string Days => ResourceManager.GetString("Days", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Deviation type.
	/// </summary>
	public static string DeviationType => ResourceManager.GetString("DeviationType", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Deviation value.
	/// </summary>
	public static string DeviationValue => ResourceManager.GetString("DeviationValue", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to -DI.
	/// </summary>
	public static string DMMinusDI => ResourceManager.GetString("DMMinusDI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to +DI.
	/// </summary>
	public static string DMPlusDI => ResourceManager.GetString("DMPlusDI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Mean.
	/// </summary>
	public static string DonchianChannelMean => ResourceManager.GetString("DonchianChannelMean", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Down bar color.
	/// </summary>
	public static string DownBarColor => ResourceManager.GetString("DownBarColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Drawing tool tile indicator adds the ability to have a floating tile in the chart that can be customized to quickly access the most commonly used drawing tools..
	/// </summary>
	public static string DrawingToolIndicatorDescription => ResourceManager.GetString("DrawingToolIndicatorDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Drawing tool tile.
	/// </summary>
	public static string DrawingToolIndicatorName => ResourceManager.GetString("DrawingToolIndicatorName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Draw lines.
	/// </summary>
	public static string DrawLines => ResourceManager.GetString("DrawLines", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to EMA1 period.
	/// </summary>
	public static string EMA1 => ResourceManager.GetString("EMA1", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to EMA2 period.
	/// </summary>
	public static string EMA2 => ResourceManager.GetString("EMA2", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to  Sent by NinjaTrader.
	/// </summary>
	public static string EmailSignature => ResourceManager.GetString("EmailSignature", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Envelope percentage.
	/// </summary>
	public static string EnvelopePercentage => ResourceManager.GetString("EnvelopePercentage", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Facebook.
	/// </summary>
	public static string FacebookServiceName => ResourceManager.GetString("FacebookServiceName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sent by NinjaTrader.
	/// </summary>
	public static string FacebookSignature => ResourceManager.GetString("FacebookSignature", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fast.
	/// </summary>
	public static string Fast => ResourceManager.GetString("Fast", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fast limit.
	/// </summary>
	public static string FastLimit => ResourceManager.GetString("FastLimit", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fast period.
	/// </summary>
	public static string FastPeriod => ResourceManager.GetString("FastPeriod", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Extreme left.
	/// </summary>
	public static string FibonacciTextAlignment_ExtremeLeft => ResourceManager.GetString("FibonacciTextAlignment_ExtremeLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Extreme right.
	/// </summary>
	public static string FibonacciTextAlignment_ExtremeRight => ResourceManager.GetString("FibonacciTextAlignment_ExtremeRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Left.
	/// </summary>
	public static string FibonacciTextAlignment_Left => ResourceManager.GetString("FibonacciTextAlignment_Left", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Off.
	/// </summary>
	public static string FibonacciTextAlignment_Off => ResourceManager.GetString("FibonacciTextAlignment_Off", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Right.
	/// </summary>
	public static string FibonacciTextAlignment_Right => ResourceManager.GetString("FibonacciTextAlignment_Right", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Any (*.*).
	/// </summary>
	public static string FileFilterAnyLoadingDialog => ResourceManager.GetString("FileFilterAnyLoadingDialog", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Any (*.*)|*.*.
	/// </summary>
	public static string FileFilterAnyWinForms => ResourceManager.GetString("FileFilterAnyWinForms", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to File name.
	/// </summary>
	public static string FileName => ResourceManager.GetString("FileName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Font.
	/// </summary>
	public static string Font => ResourceManager.GetString("Font", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Forecast.
	/// </summary>
	public static string Forecast => ResourceManager.GetString("Forecast", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bars specified.
	/// </summary>
	public static string FVGBarsSpecified => ResourceManager.GetString("FVGBarsSpecified", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bars to extend.
	/// </summary>
	public static string FVGBarsToExtend => ResourceManager.GetString("FVGBarsToExtend", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Fair Value Gap indicator examines three consecutive bars to highlight a gap between the first and third bar..
	/// </summary>
	public static string FVGDescription => ResourceManager.GetString("FVGDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Extend until.
	/// </summary>
	public static string FVGExtendUntil => ResourceManager.GetString("FVGExtendUntil", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Filled.
	/// </summary>
	public static string FVGFilled => ResourceManager.GetString("FVGFilled", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max FVG.
	/// </summary>
	public static string FVGMaxFVG => ResourceManager.GetString("FVGMaxFVG", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Minimum ticks.
	/// </summary>
	public static string FVGMinimumTicks => ResourceManager.GetString("FVGMinimumTicks", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fair Value Gap.
	/// </summary>
	public static string FVGName => ResourceManager.GetString("FVGName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Partially filled.
	/// </summary>
	public static string FVGPartiallyFilled => ResourceManager.GetString("FVGPartiallyFilled", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Down left.
	/// </summary>
	public static string GannFanDirection_DownLeft => ResourceManager.GetString("GannFanDirection_DownLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Down right.
	/// </summary>
	public static string GannFanDirection_DownRight => ResourceManager.GetString("GannFanDirection_DownRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Up left.
	/// </summary>
	public static string GannFanDirection_UpLeft => ResourceManager.GetString("GannFanDirection_UpLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Up right.
	/// </summary>
	public static string GannFanDirection_UpRight => ResourceManager.GetString("GannFanDirection_UpRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Authorize.
	/// </summary>
	public static string GuiAuthorize => ResourceManager.GetString("GuiAuthorize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color for doji bars.
	/// </summary>
	public static string GuiChartStyleDojiBrush => ResourceManager.GetString("GuiChartStyleDojiBrush", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text Position.
	/// </summary>
	public static string GuiPropertyNameTextPosition => ResourceManager.GetString("GuiPropertyNameTextPosition", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Higher high.
	/// </summary>
	public static string HigherHigh => ResourceManager.GetString("HigherHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Higher low.
	/// </summary>
	public static string HigherLow => ResourceManager.GetString("HigherLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Currency.
	/// </summary>
	public static string HighlightVerticalRangeUnit_Currency => ResourceManager.GetString("HighlightVerticalRangeUnit_Currency", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Percent.
	/// </summary>
	public static string HighlightVerticalRangeUnit_Percent => ResourceManager.GetString("HighlightVerticalRangeUnit_Percent", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Pips.
	/// </summary>
	public static string HighlightVerticalRangeUnit_Pips => ResourceManager.GetString("HighlightVerticalRangeUnit_Pips", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Price.
	/// </summary>
	public static string HighlightVerticalRangeUnit_Price => ResourceManager.GetString("HighlightVerticalRangeUnit_Price", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ticks.
	/// </summary>
	public static string HighlightVerticalRangeUnit_Ticks => ResourceManager.GetString("HighlightVerticalRangeUnit_Ticks", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to HLC calculation mode.
	/// </summary>
	public static string HLCCalculationMode => ResourceManager.GetString("HLCCalculationMode", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Calculated from intraday data.
	/// </summary>
	public static string HLCCalculationMode_CalcFromIntradayData => ResourceManager.GetString("HLCCalculationMode_CalcFromIntradayData", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Use daily bars.
	/// </summary>
	public static string HLCCalculationMode_DailyBars => ResourceManager.GetString("HLCCalculationMode_DailyBars", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Use user defined values.
	/// </summary>
	public static string HLCCalculationMode_UserDefinedValues => ResourceManager.GetString("HLCCalculationMode_UserDefinedValues", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Approach for calculation the prior day HLC values..
	/// </summary>
	public static string HLCCalculationModeDescription => ResourceManager.GetString("HLCCalculationModeDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Import.
	/// </summary>
	public static string Import => ResourceManager.GetString("Import", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to NinjaTrader (beginning of bar timestamps).
	/// </summary>
	public static string ImportTypeNinjaTraderBeginningOfBar => ResourceManager.GetString("ImportTypeNinjaTraderBeginningOfBar", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}: Date/Time format error in line {1}: {2}: '{3}'.
	/// </summary>
	public static string ImportTypeNinjaTraderDateTimeFormatError => ResourceManager.GetString("ImportTypeNinjaTraderDateTimeFormatError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to NinjaTrader (end of bar timestamps).
	/// </summary>
	public static string ImportTypeNinjaTraderEndOfBar => ResourceManager.GetString("ImportTypeNinjaTraderEndOfBar", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}: Import field separator could not be identified..
	/// </summary>
	public static string ImportTypeNinjaTraderFieldSeparatorNotIdentified => ResourceManager.GetString("ImportTypeNinjaTraderFieldSeparatorNotIdentified", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}: Format error in line {1}: {2}: '{3}'.
	/// </summary>
	public static string ImportTypeNinjaTraderFormatError => ResourceManager.GetString("ImportTypeNinjaTraderFormatError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Unable to import file '{0}'. Instrument is not supported by repository..
	/// </summary>
	public static string ImportTypeNinjaTraderInstrumentNotSupported => ResourceManager.GetString("ImportTypeNinjaTraderInstrumentNotSupported", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}: Numeric price format not supported..
	/// </summary>
	public static string ImportTypeNinjaTraderNumericPriceFormatError => ResourceManager.GetString("ImportTypeNinjaTraderNumericPriceFormatError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Unable to read data from file '{0}': {1}.
	/// </summary>
	public static string ImportTypeNinjaTraderUnableReadData => ResourceManager.GetString("ImportTypeNinjaTraderUnableReadData", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}: Unexpected number of fields in line '{1}', should be 3, 5 or 6.
	/// </summary>
	public static string ImportTypeNinjaTraderUnexpectedFieldNumber => ResourceManager.GetString("ImportTypeNinjaTraderUnexpectedFieldNumber", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Tick Data, LLC.
	/// </summary>
	public static string ImportTypeTickData => ResourceManager.GetString("ImportTypeTickData", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Incremental period.
	/// </summary>
	public static string IncrementalPeriod => ResourceManager.GetString("IncrementalPeriod", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Intermediate.
	/// </summary>
	public static string Intermediate => ResourceManager.GetString("Intermediate", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Interval.
	/// </summary>
	public static string Interval => ResourceManager.GetString("Interval", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Midline.
	/// </summary>
	public static string KeltnerChannelMidline => ResourceManager.GetString("KeltnerChannelMidline", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Plot 0.
	/// </summary>
	public static string KeyReversalPlot0 => ResourceManager.GetString("KeyReversalPlot0", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Last line length (% of chart).
	/// </summary>
	public static string LastLineLength => ResourceManager.GetString("LastLineLength", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Last line.
	/// </summary>
	public static string LastLineStroke => ResourceManager.GetString("LastLineStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Legend location.
	/// </summary>
	public static string LegendLocation => ResourceManager.GetString("LegendLocation", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bottom left.
	/// </summary>
	public static string LegendLocation_BottomLeft => ResourceManager.GetString("LegendLocation_BottomLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bottom right.
	/// </summary>
	public static string LegendLocation_BottomRight => ResourceManager.GetString("LegendLocation_BottomRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Disabled.
	/// </summary>
	public static string LegendLocation_Disabled => ResourceManager.GetString("LegendLocation_Disabled", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Top left.
	/// </summary>
	public static string LegendLocation_TopLeft => ResourceManager.GetString("LegendLocation_TopLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Top right.
	/// </summary>
	public static string LegendLocation_TopRight => ResourceManager.GetString("LegendLocation_TopRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Length.
	/// </summary>
	public static string Length => ResourceManager.GetString("Length", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line 1 value.
	/// </summary>
	public static string Line1Value => ResourceManager.GetString("Line1Value", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line 2 value.
	/// </summary>
	public static string Line2Value => ResourceManager.GetString("Line2Value", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line 3 value.
	/// </summary>
	public static string Line3Value => ResourceManager.GetString("Line3Value", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line 4 value.
	/// </summary>
	public static string Line4Value => ResourceManager.GetString("Line4Value", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line color.
	/// </summary>
	public static string LineColor => ResourceManager.GetString("LineColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Load.
	/// </summary>
	public static string Load => ResourceManager.GetString("Load", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Location.
	/// </summary>
	public static string Location => ResourceManager.GetString("Location", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Lower high.
	/// </summary>
	public static string LowerHigh => ResourceManager.GetString("LowerHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Lower low.
	/// </summary>
	public static string LowerLow => ResourceManager.GetString("LowerLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to CC:.
	/// </summary>
	public static string MailCcAddress => ResourceManager.GetString("MailCcAddress", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The email address of your carbon copy recipient. Separate multiple addresses with ',' or ';'.
	/// </summary>
	public static string MailCcAddressDescription => ResourceManager.GetString("MailCcAddressDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Email address.
	/// </summary>
	public static string MailServiceMailAddress => ResourceManager.GetString("MailServiceMailAddress", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Email.
	/// </summary>
	public static string MailServiceName => ResourceManager.GetString("MailServiceName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Connection - Port.
	/// </summary>
	public static string MailServicePort => ResourceManager.GetString("MailServicePort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to From name.
	/// </summary>
	public static string MailServiceSenderDisplayName => ResourceManager.GetString("MailServiceSenderDisplayName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Connection - Server.
	/// </summary>
	public static string MailServiceServer => ResourceManager.GetString("MailServiceServer", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Connection - SSL.
	/// </summary>
	public static string MailServiceSSL => ResourceManager.GetString("MailServiceSSL", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Subject:.
	/// </summary>
	public static string MailSubject => ResourceManager.GetString("MailSubject", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The subject of your email message.
	/// </summary>
	public static string MailSubjectDescription => ResourceManager.GetString("MailSubjectDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to To:.
	/// </summary>
	public static string MailToAddress => ResourceManager.GetString("MailToAddress", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The email address of your recipient. Separate multiple addresses with ',' or ';'.
	/// </summary>
	public static string MailToAddressDescription => ResourceManager.GetString("MailToAddressDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to FAMA.
	/// </summary>
	public static string MAMAFAMA => ResourceManager.GetString("MAMAFAMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average period.
	/// </summary>
	public static string MAPeriod => ResourceManager.GetString("MAPeriod", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average type.
	/// </summary>
	public static string MAType => ResourceManager.GetString("MAType", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average.
	/// </summary>
	public static string MovingAverage => ResourceManager.GetString("MovingAverage", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average 1.
	/// </summary>
	public static string MovingAverageRibbonPlot1 => ResourceManager.GetString("MovingAverageRibbonPlot1", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average 2.
	/// </summary>
	public static string MovingAverageRibbonPlot2 => ResourceManager.GetString("MovingAverageRibbonPlot2", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average 3.
	/// </summary>
	public static string MovingAverageRibbonPlot3 => ResourceManager.GetString("MovingAverageRibbonPlot3", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average 4.
	/// </summary>
	public static string MovingAverageRibbonPlot4 => ResourceManager.GetString("MovingAverageRibbonPlot4", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average 5.
	/// </summary>
	public static string MovingAverageRibbonPlot5 => ResourceManager.GetString("MovingAverageRibbonPlot5", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average 6.
	/// </summary>
	public static string MovingAverageRibbonPlot6 => ResourceManager.GetString("MovingAverageRibbonPlot6", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average 7.
	/// </summary>
	public static string MovingAverageRibbonPlot7 => ResourceManager.GetString("MovingAverageRibbonPlot7", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average 8.
	/// </summary>
	public static string MovingAverageRibbonPlot8 => ResourceManager.GetString("MovingAverageRibbonPlot8", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Trigger.
	/// </summary>
	public static string NBarsDownTrigger => ResourceManager.GetString("NBarsDownTrigger", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Negative color.
	/// </summary>
	public static string NegativeColor => ResourceManager.GetString("NegativeColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bottom left.
	/// </summary>
	public static string NetChangePosition_BottomLeft => ResourceManager.GetString("NetChangePosition_BottomLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bottom right.
	/// </summary>
	public static string NetChangePosition_BottomRight => ResourceManager.GetString("NetChangePosition_BottomRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Top left.
	/// </summary>
	public static string NetChangePosition_TopLeft => ResourceManager.GetString("NetChangePosition_TopLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Top right.
	/// </summary>
	public static string NetChangePosition_TopRight => ResourceManager.GetString("NetChangePosition_TopRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Background.
	/// </summary>
	public static string NinjaScriptBackground => ResourceManager.GetString("NinjaScriptBackground", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Day.
	/// </summary>
	public static string NinjaScriptBarsTypeDay => ResourceManager.GetString("NinjaScriptBarsTypeDay", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Heiken Ashi.
	/// </summary>
	public static string NinjaScriptBarsTypeHeikenAshi => ResourceManager.GetString("NinjaScriptBarsTypeHeikenAshi", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Kagi.
	/// </summary>
	public static string NinjaScriptBarsTypeKagi => ResourceManager.GetString("NinjaScriptBarsTypeKagi", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Reversal.
	/// </summary>
	public static string NinjaScriptBarsTypeKagiReversal => ResourceManager.GetString("NinjaScriptBarsTypeKagiReversal", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line Break.
	/// </summary>
	public static string NinjaScriptBarsTypeLineBreak => ResourceManager.GetString("NinjaScriptBarsTypeLineBreak", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line breaks.
	/// </summary>
	public static string NinjaScriptBarsTypeLineBreakLineBreaks => ResourceManager.GetString("NinjaScriptBarsTypeLineBreakLineBreaks", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Minute.
	/// </summary>
	public static string NinjaScriptBarsTypeMinute => ResourceManager.GetString("NinjaScriptBarsTypeMinute", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Month.
	/// </summary>
	public static string NinjaScriptBarsTypeMonth => ResourceManager.GetString("NinjaScriptBarsTypeMonth", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Point and Figure.
	/// </summary>
	public static string NinjaScriptBarsTypePointAndFigure => ResourceManager.GetString("NinjaScriptBarsTypePointAndFigure", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Box size.
	/// </summary>
	public static string NinjaScriptBarsTypePointAndFigureBoxSize => ResourceManager.GetString("NinjaScriptBarsTypePointAndFigureBoxSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Reversal.
	/// </summary>
	public static string NinjaScriptBarsTypePointAndFigureReversal => ResourceManager.GetString("NinjaScriptBarsTypePointAndFigureReversal", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Range.
	/// </summary>
	public static string NinjaScriptBarsTypeRange => ResourceManager.GetString("NinjaScriptBarsTypeRange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Renko.
	/// </summary>
	public static string NinjaScriptBarsTypeRenko => ResourceManager.GetString("NinjaScriptBarsTypeRenko", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Brick size.
	/// </summary>
	public static string NinjaScriptBarsTypeRenkoBrickSize => ResourceManager.GetString("NinjaScriptBarsTypeRenkoBrickSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Second.
	/// </summary>
	public static string NinjaScriptBarsTypeSecond => ResourceManager.GetString("NinjaScriptBarsTypeSecond", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Tick.
	/// </summary>
	public static string NinjaScriptBarsTypeTick => ResourceManager.GetString("NinjaScriptBarsTypeTick", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume.
	/// </summary>
	public static string NinjaScriptBarsTypeVolume => ResourceManager.GetString("NinjaScriptBarsTypeVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Week.
	/// </summary>
	public static string NinjaScriptBarsTypeWeek => ResourceManager.GetString("NinjaScriptBarsTypeWeek", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Border.
	/// </summary>
	public static string NinjaScriptBorder => ResourceManager.GetString("NinjaScriptBorder", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bar width.
	/// </summary>
	public static string NinjaScriptChartStyleBarWidth => ResourceManager.GetString("NinjaScriptChartStyleBarWidth", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Box.
	/// </summary>
	public static string NinjaScriptChartStyleBox => ResourceManager.GetString("NinjaScriptChartStyleBox", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color for down bars.
	/// </summary>
	public static string NinjaScriptChartStyleBoxDownBarsColor => ResourceManager.GetString("NinjaScriptChartStyleBoxDownBarsColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Down bars outline.
	/// </summary>
	public static string NinjaScriptChartStyleBoxDownBarsOutline => ResourceManager.GetString("NinjaScriptChartStyleBoxDownBarsOutline", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color for up bars.
	/// </summary>
	public static string NinjaScriptChartStyleBoxUpBarsColor => ResourceManager.GetString("NinjaScriptChartStyleBoxUpBarsColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Up bars outline.
	/// </summary>
	public static string NinjaScriptChartStyleBoxUpBarsOutline => ResourceManager.GetString("NinjaScriptChartStyleBoxUpBarsOutline", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color for down bars.
	/// </summary>
	public static string NinjaScriptChartStyleCandleDownBarsColor => ResourceManager.GetString("NinjaScriptChartStyleCandleDownBarsColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Candle body outline.
	/// </summary>
	public static string NinjaScriptChartStyleCandleOutline => ResourceManager.GetString("NinjaScriptChartStyleCandleOutline", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Candlestick.
	/// </summary>
	public static string NinjaScriptChartStyleCandlestick => ResourceManager.GetString("NinjaScriptChartStyleCandlestick", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Hollow candlestick.
	/// </summary>
	public static string NinjaScriptChartStyleCandlestickHollow => ResourceManager.GetString("NinjaScriptChartStyleCandlestickHollow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color for up bars.
	/// </summary>
	public static string NinjaScriptChartStyleCandleUpBarsColor => ResourceManager.GetString("NinjaScriptChartStyleCandleUpBarsColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Candle wick.
	/// </summary>
	public static string NinjaScriptChartStyleCandleWick => ResourceManager.GetString("NinjaScriptChartStyleCandleWick", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Equivolume.
	/// </summary>
	public static string NinjaScriptChartStyleEquivolume => ResourceManager.GetString("NinjaScriptChartStyleEquivolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Heiken Ashi.
	/// </summary>
	public static string NinjaScriptChartStyleHeikenAshi => ResourceManager.GetString("NinjaScriptChartStyleHeikenAshi", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Kagi Line.
	/// </summary>
	public static string NinjaScriptChartStyleKagi => ResourceManager.GetString("NinjaScriptChartStyleKagi", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Thick line.
	/// </summary>
	public static string NinjaScriptChartStyleKagiThickLine => ResourceManager.GetString("NinjaScriptChartStyleKagiThickLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Thin line.
	/// </summary>
	public static string NinjaScriptChartStyleKagiThinLine => ResourceManager.GetString("NinjaScriptChartStyleKagiThinLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line on Close.
	/// </summary>
	public static string NinjaScriptChartStyleLineOnClose => ResourceManager.GetString("NinjaScriptChartStyleLineOnClose", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color.
	/// </summary>
	public static string NinjaScriptChartStyleLineOnCloseColor => ResourceManager.GetString("NinjaScriptChartStyleLineOnCloseColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line width.
	/// </summary>
	public static string NinjaScriptChartStyleLineOnCloseWidth => ResourceManager.GetString("NinjaScriptChartStyleLineOnCloseWidth", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line width.
	/// </summary>
	public static string NinjaScriptChartStyleLineWidth => ResourceManager.GetString("NinjaScriptChartStyleLineWidth", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Mountain.
	/// </summary>
	public static string NinjaScriptChartStyleMountain => ResourceManager.GetString("NinjaScriptChartStyleMountain", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color.
	/// </summary>
	public static string NinjaScriptChartStyleMountainColor => ResourceManager.GetString("NinjaScriptChartStyleMountainColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Outline.
	/// </summary>
	public static string NinjaScriptChartStyleMountainOutline => ResourceManager.GetString("NinjaScriptChartStyleMountainOutline", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to OHLC.
	/// </summary>
	public static string NinjaScriptChartStyleOHLC => ResourceManager.GetString("NinjaScriptChartStyleOHLC", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color for down bars.
	/// </summary>
	public static string NinjaScriptChartStyleOhlcDownBarsColor => ResourceManager.GetString("NinjaScriptChartStyleOhlcDownBarsColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color for up bars.
	/// </summary>
	public static string NinjaScriptChartStyleOhlcUpBarsColor => ResourceManager.GetString("NinjaScriptChartStyleOhlcUpBarsColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Open/Close.
	/// </summary>
	public static string NinjaScriptChartStyleOpenClose => ResourceManager.GetString("NinjaScriptChartStyleOpenClose", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color for down bars.
	/// </summary>
	public static string NinjaScriptChartStyleOpenCloseDownBarsColor => ResourceManager.GetString("NinjaScriptChartStyleOpenCloseDownBarsColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Down bars outline.
	/// </summary>
	public static string NinjaScriptChartStyleOpenCloseDownBarsOutline => ResourceManager.GetString("NinjaScriptChartStyleOpenCloseDownBarsOutline", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color for up bars.
	/// </summary>
	public static string NinjaScriptChartStyleOpenCloseUpBarsColor => ResourceManager.GetString("NinjaScriptChartStyleOpenCloseUpBarsColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Up bars outline.
	/// </summary>
	public static string NinjaScriptChartStyleOpenCloseUpBarsOutline => ResourceManager.GetString("NinjaScriptChartStyleOpenCloseUpBarsOutline", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Point and Figure.
	/// </summary>
	public static string NinjaScriptChartStylePointAndFigure => ResourceManager.GetString("NinjaScriptChartStylePointAndFigure", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Down color.
	/// </summary>
	public static string NinjaScriptChartStylePointAndFigureDownColor => ResourceManager.GetString("NinjaScriptChartStylePointAndFigureDownColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Up color.
	/// </summary>
	public static string NinjaScriptChartStylePointAndFigureUpColor => ResourceManager.GetString("NinjaScriptChartStylePointAndFigureUpColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Anchor.
	/// </summary>
	public static string NinjaScriptDrawingToolAnchor => ResourceManager.GetString("NinjaScriptDrawingToolAnchor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to End.
	/// </summary>
	public static string NinjaScriptDrawingToolAnchorEnd => ResourceManager.GetString("NinjaScriptDrawingToolAnchorEnd", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Extension .
	/// </summary>
	public static string NinjaScriptDrawingToolAnchorExtension => ResourceManager.GetString("NinjaScriptDrawingToolAnchorExtension", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Middle.
	/// </summary>
	public static string NinjaScriptDrawingToolAnchorMiddle => ResourceManager.GetString("NinjaScriptDrawingToolAnchorMiddle", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Start.
	/// </summary>
	public static string NinjaScriptDrawingToolAnchorStart => ResourceManager.GetString("NinjaScriptDrawingToolAnchorStart", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text.
	/// </summary>
	public static string NinjaScriptDrawingToolAnchorText => ResourceManager.GetString("NinjaScriptDrawingToolAnchorText", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Andrews pitchfork.
	/// </summary>
	public static string NinjaScriptDrawingToolAndrewsPitchfork => ResourceManager.GetString("NinjaScriptDrawingToolAndrewsPitchfork", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Calculation method.
	/// </summary>
	public static string NinjaScriptDrawingToolAndrewsPitchforkCalculationMethod => ResourceManager.GetString("NinjaScriptDrawingToolAndrewsPitchforkCalculationMethod", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Strokes.
	/// </summary>
	public static string NinjaScriptDrawingToolAndrewsPitchforkCategoryStrokes => ResourceManager.GetString("NinjaScriptDrawingToolAndrewsPitchforkCategoryStrokes", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Andrews pitchfork description.
	/// </summary>
	public static string NinjaScriptDrawingToolAndrewsPitchforkDescription => ResourceManager.GetString("NinjaScriptDrawingToolAndrewsPitchforkDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Extend lines back.
	/// </summary>
	public static string NinjaScriptDrawingToolAndrewsPitchforkExtendLinesBack => ResourceManager.GetString("NinjaScriptDrawingToolAndrewsPitchforkExtendLinesBack", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Extension Line Stroke.
	/// </summary>
	public static string NinjaScriptDrawingToolAndrewsPitchforkExtensionStroke => ResourceManager.GetString("NinjaScriptDrawingToolAndrewsPitchforkExtensionStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Retracement.
	/// </summary>
	public static string NinjaScriptDrawingToolAndrewsPitchforkRetracement => ResourceManager.GetString("NinjaScriptDrawingToolAndrewsPitchforkRetracement", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Arc.
	/// </summary>
	public static string NinjaScriptDrawingToolArc => ResourceManager.GetString("NinjaScriptDrawingToolArc", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Opacity - area (%).
	/// </summary>
	public static string NinjaScriptDrawingToolAreaOpacity => ResourceManager.GetString("NinjaScriptDrawingToolAreaOpacity", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Arrow line.
	/// </summary>
	public static string NinjaScriptDrawingToolArrowLine => ResourceManager.GetString("NinjaScriptDrawingToolArrowLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Background Opacity (%).
	/// </summary>
	public static string NinjaScriptDrawingToolBackgroundOpacity => ResourceManager.GetString("NinjaScriptDrawingToolBackgroundOpacity", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ellipse.
	/// </summary>
	public static string NinjaScriptDrawingToolEllipse => ResourceManager.GetString("NinjaScriptDrawingToolEllipse", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Extended line.
	/// </summary>
	public static string NinjaScriptDrawingToolExtendedLine => ResourceManager.GetString("NinjaScriptDrawingToolExtendedLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fibonacci circle.
	/// </summary>
	public static string NinjaScriptDrawingToolFibonacciCircle => ResourceManager.GetString("NinjaScriptDrawingToolFibonacciCircle", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fibonacci extensions.
	/// </summary>
	public static string NinjaScriptDrawingToolFibonacciExtensions => ResourceManager.GetString("NinjaScriptDrawingToolFibonacciExtensions", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Anchor.
	/// </summary>
	public static string NinjaScriptDrawingToolFibonacciLevelsBaseAnchorLineStroke => ResourceManager.GetString("NinjaScriptDrawingToolFibonacciLevelsBaseAnchorLineStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fibonacci retracements.
	/// </summary>
	public static string NinjaScriptDrawingToolFibonacciRetracements => ResourceManager.GetString("NinjaScriptDrawingToolFibonacciRetracements", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Extend lines left.
	/// </summary>
	public static string NinjaScriptDrawingToolFibonacciRetracementsExtendLinesLeft => ResourceManager.GetString("NinjaScriptDrawingToolFibonacciRetracementsExtendLinesLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Extend lines right.
	/// </summary>
	public static string NinjaScriptDrawingToolFibonacciRetracementsExtendLinesRight => ResourceManager.GetString("NinjaScriptDrawingToolFibonacciRetracementsExtendLinesRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text alignment.
	/// </summary>
	public static string NinjaScriptDrawingToolFibonacciRetracementsTextAlignment => ResourceManager.GetString("NinjaScriptDrawingToolFibonacciRetracementsTextAlignment", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text location.
	/// </summary>
	public static string NinjaScriptDrawingToolFibonacciRetracementsTextLocation => ResourceManager.GetString("NinjaScriptDrawingToolFibonacciRetracementsTextLocation", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Divide time/price separately.
	/// </summary>
	public static string NinjaScriptDrawingToolFibonacciTimeCircleDivideTimeSeparately => ResourceManager.GetString("NinjaScriptDrawingToolFibonacciTimeCircleDivideTimeSeparately", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fibonacci time extensions.
	/// </summary>
	public static string NinjaScriptDrawingToolFibonacciTimeExtensions => ResourceManager.GetString("NinjaScriptDrawingToolFibonacciTimeExtensions", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Show text.
	/// </summary>
	public static string NinjaScriptDrawingToolFibonacciTimeExtensionsShowText => ResourceManager.GetString("NinjaScriptDrawingToolFibonacciTimeExtensionsShowText", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Gann fan.
	/// </summary>
	public static string NinjaScriptDrawingToolGannFan => ResourceManager.GetString("NinjaScriptDrawingToolGannFan", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Display text.
	/// </summary>
	public static string NinjaScriptDrawingToolGannFanDisplayText => ResourceManager.GetString("NinjaScriptDrawingToolGannFanDisplayText", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fan direction.
	/// </summary>
	public static string NinjaScriptDrawingToolGannFanFanDirection => ResourceManager.GetString("NinjaScriptDrawingToolGannFanFanDirection", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Points per bar.
	/// </summary>
	public static string NinjaScriptDrawingToolGannFanPointsPerBar => ResourceManager.GetString("NinjaScriptDrawingToolGannFanPointsPerBar", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Horizontal Line.
	/// </summary>
	public static string NinjaScriptDrawingToolHorizontalLine => ResourceManager.GetString("NinjaScriptDrawingToolHorizontalLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line.
	/// </summary>
	public static string NinjaScriptDrawingToolLine => ResourceManager.GetString("NinjaScriptDrawingToolLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Path.
	/// </summary>
	public static string NinjaScriptDrawingToolPath => ResourceManager.GetString("NinjaScriptDrawingToolPath", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Path begin.
	/// </summary>
	public static string NinjaScriptDrawingToolPathBegin => ResourceManager.GetString("NinjaScriptDrawingToolPathBegin", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Path end.
	/// </summary>
	public static string NinjaScriptDrawingToolPathEnd => ResourceManager.GetString("NinjaScriptDrawingToolPathEnd", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Segment.
	/// </summary>
	public static string NinjaScriptDrawingToolPathSegment => ResourceManager.GetString("NinjaScriptDrawingToolPathSegment", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Show count.
	/// </summary>
	public static string NinjaScriptDrawingToolPathShowCount => ResourceManager.GetString("NinjaScriptDrawingToolPathShowCount", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Polygon.
	/// </summary>
	public static string NinjaScriptDrawingToolPolygon => ResourceManager.GetString("NinjaScriptDrawingToolPolygon", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Price Levels Opacity (%).
	/// </summary>
	public static string NinjaScriptDrawingToolPriceLevelsOpacity => ResourceManager.GetString("NinjaScriptDrawingToolPriceLevelsOpacity", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Price marker.
	/// </summary>
	public static string NinjaScriptDrawingToolPriceMarker => ResourceManager.GetString("NinjaScriptDrawingToolPriceMarker", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ray.
	/// </summary>
	public static string NinjaScriptDrawingToolRay => ResourceManager.GetString("NinjaScriptDrawingToolRay", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Rectangle.
	/// </summary>
	public static string NinjaScriptDrawingToolRectangle => ResourceManager.GetString("NinjaScriptDrawingToolRectangle", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Region.
	/// </summary>
	public static string NinjaScriptDrawingToolRegion => ResourceManager.GetString("NinjaScriptDrawingToolRegion", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Direction.
	/// </summary>
	public static string NinjaScriptDrawingToolRegionHighlightDirection => ResourceManager.GetString("NinjaScriptDrawingToolRegionHighlightDirection", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Direction Stroke.
	/// </summary>
	public static string NinjaScriptDrawingToolRegionHighlightDirectionStroke => ResourceManager.GetString("NinjaScriptDrawingToolRegionHighlightDirectionStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} Bars Time: {1}.
	/// </summary>
	public static string NinjaScriptDrawingToolRegionHighlightHorizontalTextFormat => ResourceManager.GetString("NinjaScriptDrawingToolRegionHighlightHorizontalTextFormat", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Vertical range unit.
	/// </summary>
	public static string NinjaScriptDrawingToolRegionHighlightVerticalRangeUnit => ResourceManager.GetString("NinjaScriptDrawingToolRegionHighlightVerticalRangeUnit", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Range value: {0} {1}.
	/// </summary>
	public static string NinjaScriptDrawingToolRegionHighlightVerticalTextFormat => ResourceManager.GetString("NinjaScriptDrawingToolRegionHighlightVerticalTextFormat", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Region highlight x.
	/// </summary>
	public static string NinjaScriptDrawingToolRegionHiglightX => ResourceManager.GetString("NinjaScriptDrawingToolRegionHiglightX", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Region highlight y.
	/// </summary>
	public static string NinjaScriptDrawingToolRegionHiglightY => ResourceManager.GetString("NinjaScriptDrawingToolRegionHiglightY", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Regression channel.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannel => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannel", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Lower channel.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannelLowerChannel => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannelLowerChannel", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Lower Channel Color.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannelLowerChannelColor => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannelLowerChannelColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Price type.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannelPriceType => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannelPriceType", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Regression.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannelRegressionChannel => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannelRegressionChannel", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Extend left.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannelStandardDeviationExtendLeft => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannelStandardDeviationExtendLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Extend right.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannelStandardDeviationExtendRight => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannelStandardDeviationExtendRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Distance to lower channel.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannelStandardDeviationLowerDistance => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannelStandardDeviationLowerDistance", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Distance to upper channel.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannelStandardDeviationUpperDistance => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannelStandardDeviationUpperDistance", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Mode.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannelType => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannelType", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Upper channel.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannelUpperChannel => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannelUpperChannel", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Upper channel color.
	/// </summary>
	public static string NinjaScriptDrawingToolRegressionChannelUpperChannelColor => ResourceManager.GetString("NinjaScriptDrawingToolRegressionChannelUpperChannelColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Entry anchor.
	/// </summary>
	public static string NinjaScriptDrawingToolRiskRewardAnchorEntry => ResourceManager.GetString("NinjaScriptDrawingToolRiskRewardAnchorEntry", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Anchor.
	/// </summary>
	public static string NinjaScriptDrawingToolRiskRewardAnchorLineStroke => ResourceManager.GetString("NinjaScriptDrawingToolRiskRewardAnchorLineStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Reward anchor.
	/// </summary>
	public static string NinjaScriptDrawingToolRiskRewardAnchorReward => ResourceManager.GetString("NinjaScriptDrawingToolRiskRewardAnchorReward", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Risk anchor.
	/// </summary>
	public static string NinjaScriptDrawingToolRiskRewardAnchorRisk => ResourceManager.GetString("NinjaScriptDrawingToolRiskRewardAnchorRisk", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Colors.
	/// </summary>
	public static string NinjaScriptDrawingToolRiskRewardCategoryColors => ResourceManager.GetString("NinjaScriptDrawingToolRiskRewardCategoryColors", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Automatically calculate your target based off a user defined stop loss.
	/// </summary>
	public static string NinjaScriptDrawingToolRiskRewardDescription => ResourceManager.GetString("NinjaScriptDrawingToolRiskRewardDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Entry extension.
	/// </summary>
	public static string NinjaScriptDrawingToolRiskRewardLineStrokeEntry => ResourceManager.GetString("NinjaScriptDrawingToolRiskRewardLineStrokeEntry", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Reward extension.
	/// </summary>
	public static string NinjaScriptDrawingToolRiskRewardLineStrokeReward => ResourceManager.GetString("NinjaScriptDrawingToolRiskRewardLineStrokeReward", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Risk extension.
	/// </summary>
	public static string NinjaScriptDrawingToolRiskRewardLineStrokeRisk => ResourceManager.GetString("NinjaScriptDrawingToolRiskRewardLineStrokeRisk", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Risk Reward.
	/// </summary>
	public static string NinjaScriptDrawingToolRiskRewardName => ResourceManager.GetString("NinjaScriptDrawingToolRiskRewardName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ratio.
	/// </summary>
	public static string NinjaScriptDrawingToolRiskRewardRatio => ResourceManager.GetString("NinjaScriptDrawingToolRiskRewardRatio", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ruler.
	/// </summary>
	public static string NinjaScriptDrawingToolRuler => ResourceManager.GetString("NinjaScriptDrawingToolRuler", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} days.
	/// </summary>
	public static string NinjaScriptDrawingToolRulerDaysFormat => ResourceManager.GetString("NinjaScriptDrawingToolRulerDaysFormat", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to # bars:.
	/// </summary>
	public static string NinjaScriptDrawingToolRulerNumberBarsText => ResourceManager.GetString("NinjaScriptDrawingToolRulerNumberBarsText", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Time:.
	/// </summary>
	public static string NinjaScriptDrawingToolRulerTimeText => ResourceManager.GetString("NinjaScriptDrawingToolRulerTimeText", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Y value display unit.
	/// </summary>
	public static string NinjaScriptDrawingToolRulerYValueDisplayUnit => ResourceManager.GetString("NinjaScriptDrawingToolRulerYValueDisplayUnit", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Y value:.
	/// </summary>
	public static string NinjaScriptDrawingToolRulerYValueText => ResourceManager.GetString("NinjaScriptDrawingToolRulerYValueText", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Drawing tools.
	/// </summary>
	public static string NinjaScriptDrawingTools => ResourceManager.GetString("NinjaScriptDrawingTools", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Arrow down.
	/// </summary>
	public static string NinjaScriptDrawingToolsChartArrowDownMarkerName => ResourceManager.GetString("NinjaScriptDrawingToolsChartArrowDownMarkerName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Arrow up.
	/// </summary>
	public static string NinjaScriptDrawingToolsChartArrowUpMarkerName => ResourceManager.GetString("NinjaScriptDrawingToolsChartArrowUpMarkerName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Diamond.
	/// </summary>
	public static string NinjaScriptDrawingToolsChartDiamondMarkerName => ResourceManager.GetString("NinjaScriptDrawingToolsChartDiamondMarkerName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Dot.
	/// </summary>
	public static string NinjaScriptDrawingToolsChartDotMarkerName => ResourceManager.GetString("NinjaScriptDrawingToolsChartDotMarkerName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Square.
	/// </summary>
	public static string NinjaScriptDrawingToolsChartSquareMarkerName => ResourceManager.GetString("NinjaScriptDrawingToolsChartSquareMarkerName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Triangle down.
	/// </summary>
	public static string NinjaScriptDrawingToolsChartTriangleDownMarkerName => ResourceManager.GetString("NinjaScriptDrawingToolsChartTriangleDownMarkerName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Triangle up.
	/// </summary>
	public static string NinjaScriptDrawingToolsChartTriangleUpMarkerName => ResourceManager.GetString("NinjaScriptDrawingToolsChartTriangleUpMarkerName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ratio time.
	/// </summary>
	public static string NinjaScriptDrawingToolsGannAngleRatioX => ResourceManager.GetString("NinjaScriptDrawingToolsGannAngleRatioX", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ratio price.
	/// </summary>
	public static string NinjaScriptDrawingToolsGannAngleRatioY => ResourceManager.GetString("NinjaScriptDrawingToolsGannAngleRatioY", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Gann angles.
	/// </summary>
	public static string NinjaScriptDrawingToolsGannAngles => ResourceManager.GetString("NinjaScriptDrawingToolsGannAngles", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 1 Gann angle|{0} Gann angles|Add Gann angle..|Edit Gann angle...|Edit Gann angles....
	/// </summary>
	public static string NinjaScriptDrawingToolsGannAnglesPrompt => ResourceManager.GetString("NinjaScriptDrawingToolsGannAnglesPrompt", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color - area.
	/// </summary>
	public static string NinjaScriptDrawingToolShapesAreaBrush => ResourceManager.GetString("NinjaScriptDrawingToolShapesAreaBrush", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color - outline.
	/// </summary>
	public static string NinjaScriptDrawingToolShapesOutlineBrush => ResourceManager.GetString("NinjaScriptDrawingToolShapesOutlineBrush", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Visible.
	/// </summary>
	public static string NinjaScriptDrawingToolsPriceLevelIsVisible => ResourceManager.GetString("NinjaScriptDrawingToolsPriceLevelIsVisible", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line.
	/// </summary>
	public static string NinjaScriptDrawingToolsPriceLevelLineStroke => ResourceManager.GetString("NinjaScriptDrawingToolsPriceLevelLineStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Levels.
	/// </summary>
	public static string NinjaScriptDrawingToolsPriceLevels => ResourceManager.GetString("NinjaScriptDrawingToolsPriceLevels", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 1 price level|{0} price levels|Add price level..|Edit price level...|Edit price levels....
	/// </summary>
	public static string NinjaScriptDrawingToolsPriceLevelsPrompt => ResourceManager.GetString("NinjaScriptDrawingToolsPriceLevelsPrompt", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Unset.
	/// </summary>
	public static string NinjaScriptDrawingToolsPriceLevelUnset => ResourceManager.GetString("NinjaScriptDrawingToolsPriceLevelUnset", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Value (%).
	/// </summary>
	public static string NinjaScriptDrawingToolsPriceLevelValue => ResourceManager.GetString("NinjaScriptDrawingToolsPriceLevelValue", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line.
	/// </summary>
	public static string NinjaScriptDrawingToolStroke => ResourceManager.GetString("NinjaScriptDrawingToolStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text.
	/// </summary>
	public static string NinjaScriptDrawingToolText => ResourceManager.GetString("NinjaScriptDrawingToolText", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text alignment.
	/// </summary>
	public static string NinjaScriptDrawingToolTextAlignment => ResourceManager.GetString("NinjaScriptDrawingToolTextAlignment", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text background brush.
	/// </summary>
	public static string NinjaScriptDrawingToolTextBackBrush => ResourceManager.GetString("NinjaScriptDrawingToolTextBackBrush", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Color - font.
	/// </summary>
	public static string NinjaScriptDrawingToolTextBrush => ResourceManager.GetString("NinjaScriptDrawingToolTextBrush", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fixed text.
	/// </summary>
	public static string NinjaScriptDrawingToolTextFixed => ResourceManager.GetString("NinjaScriptDrawingToolTextFixed", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to .
	/// </summary>
	public static string NinjaScriptDrawingToolTextFixedTextPosition => ResourceManager.GetString("NinjaScriptDrawingToolTextFixedTextPosition", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Font.
	/// </summary>
	public static string NinjaScriptDrawingToolTextFont => ResourceManager.GetString("NinjaScriptDrawingToolTextFont", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Outline.
	/// </summary>
	public static string NinjaScriptDrawingToolTextOutlineStroke => ResourceManager.GetString("NinjaScriptDrawingToolTextOutlineStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Outline - enabled.
	/// </summary>
	public static string NinjaScriptDrawingToolTextOutlineVisible => ResourceManager.GetString("NinjaScriptDrawingToolTextOutlineVisible", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Time Cycles.
	/// </summary>
	public static string NinjaScriptDrawingToolTimeCycles => ResourceManager.GetString("NinjaScriptDrawingToolTimeCycles", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Trend channel.
	/// </summary>
	public static string NinjaScriptDrawingToolTrendChannel => ResourceManager.GetString("NinjaScriptDrawingToolTrendChannel", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Draws a trend channel using parallel lines.
	/// </summary>
	public static string NinjaScriptDrawingToolTrendChannelDescription => ResourceManager.GetString("NinjaScriptDrawingToolTrendChannelDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Trend end.
	/// </summary>
	public static string NinjaScriptDrawingToolTrendChannelEnd1AnchorDisplayName => ResourceManager.GetString("NinjaScriptDrawingToolTrendChannelEnd1AnchorDisplayName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Parallel.
	/// </summary>
	public static string NinjaScriptDrawingToolTrendChannelParallelStroke => ResourceManager.GetString("NinjaScriptDrawingToolTrendChannelParallelStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Trend start.
	/// </summary>
	public static string NinjaScriptDrawingToolTrendChannelStart1AnchorDisplayName => ResourceManager.GetString("NinjaScriptDrawingToolTrendChannelStart1AnchorDisplayName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Parallel.
	/// </summary>
	public static string NinjaScriptDrawingToolTrendChannelStart2AnchorDisplayName => ResourceManager.GetString("NinjaScriptDrawingToolTrendChannelStart2AnchorDisplayName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Trend.
	/// </summary>
	public static string NinjaScriptDrawingToolTrendChannelTrendStroke => ResourceManager.GetString("NinjaScriptDrawingToolTrendChannelTrendStroke", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Triangle.
	/// </summary>
	public static string NinjaScriptDrawingToolTriangle => ResourceManager.GetString("NinjaScriptDrawingToolTriangle", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Vertical line.
	/// </summary>
	public static string NinjaScriptDrawingToolVerticalLine => ResourceManager.GetString("NinjaScriptDrawingToolVerticalLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to General.
	/// </summary>
	public static string NinjaScriptGeneral => ResourceManager.GetString("NinjaScriptGeneral", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Average performance offset (%).
	/// </summary>
	public static string NinjaScriptGeneticOptimizerAveragePerformanceOffsetPercent => ResourceManager.GetString("NinjaScriptGeneticOptimizerAveragePerformanceOffsetPercent", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Convergence threshold.
	/// </summary>
	public static string NinjaScriptGeneticOptimizerConvergenceThreshold => ResourceManager.GetString("NinjaScriptGeneticOptimizerConvergenceThreshold", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Crossover index.
	/// </summary>
	public static string NinjaScriptGeneticOptimizerCrossoverIndex => ResourceManager.GetString("NinjaScriptGeneticOptimizerCrossoverIndex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Crossover rate (%).
	/// </summary>
	public static string NinjaScriptGeneticOptimizerCrossoverRatePercent => ResourceManager.GetString("NinjaScriptGeneticOptimizerCrossoverRatePercent", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fast generations.
	/// </summary>
	public static string NinjaScriptGeneticOptimizerFastGenerations => ResourceManager.GetString("NinjaScriptGeneticOptimizerFastGenerations", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Generations.
	/// </summary>
	public static string NinjaScriptGeneticOptimizerGenerations => ResourceManager.GetString("NinjaScriptGeneticOptimizerGenerations", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Generation size.
	/// </summary>
	public static string NinjaScriptGeneticOptimizerGenerationSize => ResourceManager.GetString("NinjaScriptGeneticOptimizerGenerationSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Minimum performance.
	/// </summary>
	public static string NinjaScriptGeneticOptimizerMinimumPerformance => ResourceManager.GetString("NinjaScriptGeneticOptimizerMinimumPerformance", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Mutation rate (%).
	/// </summary>
	public static string NinjaScriptGeneticOptimizerMutationRatePercent => ResourceManager.GetString("NinjaScriptGeneticOptimizerMutationRatePercent", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Mutation strength (%).
	/// </summary>
	public static string NinjaScriptGeneticOptimizerMutationStrengthPercent => ResourceManager.GetString("NinjaScriptGeneticOptimizerMutationStrengthPercent", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Reset size (%).
	/// </summary>
	public static string NinjaScriptGeneticOptimizerResetSizePercent => ResourceManager.GetString("NinjaScriptGeneticOptimizerResetSizePercent", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Slow generations.
	/// </summary>
	public static string NinjaScriptGeneticOptimizerSlowGenerations => ResourceManager.GetString("NinjaScriptGeneticOptimizerSlowGenerations", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Stability size (%).
	/// </summary>
	public static string NinjaScriptGeneticOptimizerStabilitySizePercent => ResourceManager.GetString("NinjaScriptGeneticOptimizerStabilitySizePercent", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Threshold generations.
	/// </summary>
	public static string NinjaScriptGeneticOptimizerThresholdGenerations => ResourceManager.GetString("NinjaScriptGeneticOptimizerThresholdGenerations", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Indicator.
	/// </summary>
	public static string NinjaScriptIndicator => ResourceManager.GetString("NinjaScriptIndicator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Avg.
	/// </summary>
	public static string NinjaScriptIndicatorAvg => ResourceManager.GetString("NinjaScriptIndicatorAvg", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Count.
	/// </summary>
	public static string NinjaScriptIndicatorCount => ResourceManager.GetString("NinjaScriptIndicatorCount", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Default.
	/// </summary>
	public static string NinjaScriptIndicatorDefault => ResourceManager.GetString("NinjaScriptIndicatorDefault", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Accumulation/Distribution (AD) study attempts to quantify the amount of volume flowing into or out of an instrument by identifying the position of the close of the period in relation to that period's high/low range..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionADL => ResourceManager.GetString("NinjaScriptIndicatorDescriptionADL", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Average Directional Index measures the strength of a prevailing trend as well as whether movement exists in the market. The ADX is measured on a scale of 0  100. A low ADX value (generally less than 20) can indicate a non-trending market with low volumes whereas a cross above 20 may indicate the start of a trend (either up or down). If the ADX is over 40 and begins to fall, it can indicate the slowdown of a current trend..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionADX => ResourceManager.GetString("NinjaScriptIndicatorDescriptionADX", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Average Directional Movement Rating quantifies momentum change in the ADX. It is calculated by adding two values of ADX (the current value and a value n periods back), then dividing by two. This additional smoothing makes the ADXR slightly less responsive than ADX. The interpretation is the same as the ADX; the higher the value, the stronger the trend..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionADXR => ResourceManager.GetString("NinjaScriptIndicatorDescriptionADXR", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The APZ (Adaptive Prize Zone) forms a steady channel based on double smoothed exponential moving averages around the average price. See S/C, September 2006, p.28..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionAPZ => ResourceManager.GetString("NinjaScriptIndicatorDescriptionAPZ", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Aroon Indicator was developed by Tushar Chande. It is comprised of two plots: one measuring the number of periods since the most recent x-period high (Aroon Up) and the other measuring the number of periods since the most recent x-period low (Aroon Down)..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionAroon => ResourceManager.GetString("NinjaScriptIndicatorDescriptionAroon", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Aroon Oscillator is based upon his Aroon Indicator. Much like the Aroon Indicator, the Aroon Oscillator measures the strength of a trend..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionAroonOscillator => ResourceManager.GetString("NinjaScriptIndicatorDescriptionAroonOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Average True Range (ATR) is a measure of volatility. It was introduced by Welles Wilder in his book 'New Concepts in Technical Trading Systems' and has since been used as a component of many indicators and trading systems..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionATR => ResourceManager.GetString("NinjaScriptIndicatorDescriptionATR", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Displays remaining time of the time based bar.
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionBarTimer => ResourceManager.GetString("NinjaScriptIndicatorDescriptionBarTimer", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Block volume detects block trades and display how many occurred per bar. This can be displayed either as trades or volume. Historical tick data is required to plot historically..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionBlockVolume => ResourceManager.GetString("NinjaScriptIndicatorDescriptionBlockVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bollinger Bands are plotted at standard deviation levels above and below a moving average. Since standard deviation is a measure of volatility, the bands are self-adjusting: widening during volatile markets and contracting during calmer periods..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionBollinger => ResourceManager.GetString("NinjaScriptIndicatorDescriptionBollinger", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The balance of power indicator measures the strength of the bulls vs. bears by assessing the ability of each to push price to an extreme level..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionBOP => ResourceManager.GetString("NinjaScriptIndicatorDescriptionBOP", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Indicates the current buying or selling pressure as a perecentage. This is a tick by tick indicator. If 'Calculate' is set to 'On bar close', the indicator values will always be 100..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionBuySellPressure => ResourceManager.GetString("NinjaScriptIndicatorDescriptionBuySellPressure", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Plots a histogram splitting volume between trades at the ask or higher and trades at the bid and lower.  Only works on historical data if using Tick Replay.
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionBuySellVolume => ResourceManager.GetString("NinjaScriptIndicatorDescriptionBuySellVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Camarilla pivots are a price analysis too that generates potential support and resistance levels by multiplying the prior range then adding or subtracting it from the close..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionCamarillaPivots => ResourceManager.GetString("NinjaScriptIndicatorDescriptionCamarillaPivots", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Detects common candlestick patterns and marks them on the chart.
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionCandlestickPattern => ResourceManager.GetString("NinjaScriptIndicatorDescriptionCandlestickPattern", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Commodity Channel Index (CCI) measures the variation of a security's price from its statistical mean. High values show that prices are unusually high compared to average prices whereas low values indicate that prices are unusually low..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionCCI => ResourceManager.GetString("NinjaScriptIndicatorDescriptionCCI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Calculates the amount of money flow volume over n bars..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionChaikinMoneyFlow => ResourceManager.GetString("NinjaScriptIndicatorDescriptionChaikinMoneyFlow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Calculates the momentum of the accumulation distribution line using the difference between two exponential moving averages..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionChaikinOscillator => ResourceManager.GetString("NinjaScriptIndicatorDescriptionChaikinOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Compares difference between an instruments current and historical range using exponential moving averages..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionChaikinVolatility => ResourceManager.GetString("NinjaScriptIndicatorDescriptionChaikinVolatility", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Choppiness Index is designed to determine if the market is choppy (trading sideways) or not choppy (trading within a trend in either direction).
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionChoppinessIndex => ResourceManager.GetString("NinjaScriptIndicatorDescriptionChoppinessIndex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The CMO differs from other momentum oscillators such as Relative Strength Index (RSI) and Stochastics. It uses both up and down days data in the numerator of the calculation to measure momentum directly. Primarily used to look for extreme overbought and oversold conditions, CMO can also be used to look for trends..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionCMO => ResourceManager.GetString("NinjaScriptIndicatorDescriptionCMO", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Plots lines at user defined values..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionConstantLines => ResourceManager.GetString("NinjaScriptIndicatorDescriptionConstantLines", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The correlation indicator will plot the correlation of the data series to a desired instrument. Values close to 1 indicate movement in the same direction. Values close to -1 indicate movement in opposite directions. Values near 0 indicate no correlation..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionCorrelation => ResourceManager.GetString("NinjaScriptIndicatorDescriptionCorrelation", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The COT indicator plots weekly data from the Commitment Of Traders report, indicating holdings of different participants in the U.S. futures market..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionCOT => ResourceManager.GetString("NinjaScriptIndicatorDescriptionCOT", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Plots the open, high, and low values from the session starting on the current day..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionCurrentDayOHL => ResourceManager.GetString("NinjaScriptIndicatorDescriptionCurrentDayOHL", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Darvas Boxes were taken from the pages of Nicolas Darvas book, How I Made $2,000,000 in the Stock Market. The boxes are used to normalize a trend. A 'buy' signal would be indicated when the price of the stock exceeds the top of the box. A 'sell' signal would be indicated when the price of the stock falls below the bottom of the box..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionDarvas => ResourceManager.GetString("NinjaScriptIndicatorDescriptionDarvas", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Double Exponential Moving Average (DEMA) is a combination of a single exponential moving average and a double exponential moving average..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionDEMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionDEMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Disparity Index measures the difference between the price and an exponential moving average. A value greater could suggest bullish momentum, while a value less than zero could suggest bearish momentum..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionDisparityIndex => ResourceManager.GetString("NinjaScriptIndicatorDescriptionDisparityIndex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Directional Movement (DM). This is the same indicator as the ADX, with the addition of the two directional movement indicators +DI and -DI. +DI and -DI measure upward and downward momentum. A buy signal is generated when +DI crosses -DI to the upside. A sell signal is generated when -DI crosses +DI to the downside..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionDM => ResourceManager.GetString("NinjaScriptIndicatorDescriptionDM", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Directional Movement Index. Directional Movement Index is quite similiar to Welles Wilder's Relative Strength Index. The difference is the DMI uses variable time periods (from 3 to 30) vs. the RSI's fixed periods..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionDMI => ResourceManager.GetString("NinjaScriptIndicatorDescriptionDMI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Dynamic Momentum Index is a variable term RSI. The RSI term varies from 3 to 30. The variable time period makes the RSI more responsive to short-term moves. The more volatile the price is, the shorter the time period is. It is interpreted in the same way as the RSI, but provides signals earlier..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionDMIndex => ResourceManager.GetString("NinjaScriptIndicatorDescriptionDMIndex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Donchian Channel. The Donchian Channel indicator was created by Richard Donchian. It uses the highest high and the lowest low of a period of time to plot the channel..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionDonchianChannel => ResourceManager.GetString("NinjaScriptIndicatorDescriptionDonchianChannel", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Double Stochastics is a variation of the Stochastics indicator developed by William Blau..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionDoubleStochastics => ResourceManager.GetString("NinjaScriptIndicatorDescriptionDoubleStochastics", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Ease of Movement (EMV) indicator emphasizes days in which the stock is moving easily and minimizes the days in which the stock is finding it difficult to move. A buy signal is generated when the EMV crosses above zero, a sell signal when it crosses below zero. When the EMV hovers around zero, then there are small price movements and/or high volume, which is to say, the price is not moving easily..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionEaseOfMovement => ResourceManager.GetString("NinjaScriptIndicatorDescriptionEaseOfMovement", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Exponential Moving Average is an indicator that shows the average value of a security's price over a period of time. When calculating a moving average, the EMA applies more weight to recent prices than the SMA..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionEMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionEMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fibonacci pivots are a price analysis too that generates potential support and resistance levels by multiplying the prior range against Fibonacci values then adding or subtracting it from the average of the prior high, low, and close..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionFibonacciPivots => ResourceManager.GetString("NinjaScriptIndicatorDescriptionFibonacciPivots", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Fisher Transform has sharp and distinct turning points that occur in a timely fashion. The resulting peak swings are used to identify price reversals..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionFisherTransform => ResourceManager.GetString("NinjaScriptIndicatorDescriptionFisherTransform", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Forecast Oscillator (FOSC) is an extension of the linear regression based indicators made popular by Tushar Chande. The Forecast Oscillator plots the percentage difference between the forecast price (generated by an x-period linear regression line) and the actual price. The oscillator is above zero when the forecast price is greater than the actual price.  Conversely, it's less than zero if its below. In the rare case when the forecast price and the actual price are the same, the oscillator would plot z [rest of string was truncated]";.
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionFOSC => ResourceManager.GetString("NinjaScriptIndicatorDescriptionFOSC", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Hull Moving Average (HMA) employs weighted MA calculations to offer superior smoothing, and much less lag, over traditional SMA indicators..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionHMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionHMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Ichimoku Cloud is a charting tool that shows potential support and resistance areas, trend direction, and momentum using a set of moving average-based lines and a shaded area called the 'cloud.' It helps users observe how price interacts with these components over time..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionIchimokuCloud => ResourceManager.GetString("NinjaScriptIndicatorDescriptionIchimokuCloud", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Developed by Perry Kaufman, this indicator is an EMA using an Efficiency Ratio to modify the smoothing constant, which ranges from a minimum of Fast Length to a maximum of Slow Length. Since this moving average is adaptive it tends to follow prices more closely than other MA's..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionKAMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionKAMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Keltner Channel is a similar indicator to Bollinger Bands. Here the midline is a standard moving average with the upper and lower bands offset by the SMA of the difference between the high and low of the previous bars. The offset multiplier as well as the SMA period is configurable..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionKeltnerChannel => ResourceManager.GetString("NinjaScriptIndicatorDescriptionKeltnerChannel", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Returns a value of 1 when the current close is less than the prior close after penetrating the highest high of the last n bars..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionKeyReversalDown => ResourceManager.GetString("NinjaScriptIndicatorDescriptionKeyReversalDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Returns a value of 1 when the current close is greater than the prior close after penetrating the lowest low of the last n bars..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionKeyReversalUp => ResourceManager.GetString("NinjaScriptIndicatorDescriptionKeyReversalUp", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Linear Regression is an indicator that 'predicts' the value of a security's price..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionLinReg => ResourceManager.GetString("NinjaScriptIndicatorDescriptionLinReg", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Linear Regression Intercept provides the intercept value of the Linear Regression trendline..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionLinRegIntercept => ResourceManager.GetString("NinjaScriptIndicatorDescriptionLinRegIntercept", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Linear Regression Slope provides the slope value of the Linear Regression trendline..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionLinRegSlope => ResourceManager.GetString("NinjaScriptIndicatorDescriptionLinRegSlope", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The MACD (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionMACD => ResourceManager.GetString("NinjaScriptIndicatorDescriptionMACD", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Plots % envelopes around a moving average.
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionMAEnvelopes => ResourceManager.GetString("NinjaScriptIndicatorDescriptionMAEnvelopes", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The MAMA (MESA Adaptive Moving Average) was developed by John Ehlers. It adapts to price movement in a new and unique way. The adaptation is based on the Hilbert Transform Discriminator. The advantage of this method features fast attack average and a slow decay average. The MAMA + the FAMA (Following Adaptive Moving Average) lines only cross at major market reversals..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionMAMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionMAMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Maximum shows the maximum of the last n bars..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionMAX => ResourceManager.GetString("NinjaScriptIndicatorDescriptionMAX", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to McClellan Oscillator is the difference between two exponential moving averages of the NYSE advance decline spread. This indicator require ADV and DECL index data..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionMcClellanOscillator => ResourceManager.GetString("NinjaScriptIndicatorDescriptionMcClellanOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The MFI (Money Flow Index) is a momentum indicator that measures the strength of money flowing in and out of a security..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionMFI => ResourceManager.GetString("NinjaScriptIndicatorDescriptionMFI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Minimum shows the minimum of the last n bars..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionMIN => ResourceManager.GetString("NinjaScriptIndicatorDescriptionMIN", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Momentum indicator measures the amount that a security's price has changed over a given time span..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionMomentum => ResourceManager.GetString("NinjaScriptIndicatorDescriptionMomentum", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Money Flow Oscillator measures the amount of money flow volume over a specific period. A move into positive territory indicates buying pressure while a move into negative territory indicates selling pressure..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionMoneyFlowOscillator => ResourceManager.GetString("NinjaScriptIndicatorDescriptionMoneyFlowOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Moving Average Ribbon is a series of incrementing moving averages..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionMovingAverageRibbon => ResourceManager.GetString("NinjaScriptIndicatorDescriptionMovingAverageRibbon", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to This indicator returns 1 when we have n of consecutive bars down, otherwise returns 0. A down bar is defined as a bar where the close is below the open and the bars makes a lower high and a lower low. You can adjust the specific requirements with the indicator options..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionNBarsDown => ResourceManager.GetString("NinjaScriptIndicatorDescriptionNBarsDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to This indicator returns 1 when we have n of consecutive bars up, otherwise returns 0. An up bar is defined as a bar where the close is above the open and the bars makes a higher high and a higher low. You can adjust the specific requirements with the indicator options..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionNBarsUp => ResourceManager.GetString("NinjaScriptIndicatorDescriptionNBarsUp", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Displays net change on the chart..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionNetChangeDisplay => ResourceManager.GetString("NinjaScriptIndicatorDescriptionNetChangeDisplay", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to OBV (On Balance Volume) is a running total of volume. It shows if volume is flowing into or out of a security. When the security closes higher than the previous close, all of the day's volume is considered up-volume. When the security closes lower than the previous close, all of the day's volume is considered down-volume..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionOBV => ResourceManager.GetString("NinjaScriptIndicatorDescriptionOBV", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Parabolic SAR according to Stocks and Commodities magazine V 11:11 (477-479)..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionParabolicSAR => ResourceManager.GetString("NinjaScriptIndicatorDescriptionParabolicSAR", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The PFE (Polarized Fractal Efficiency) is an indicator that uses fractal geometry to determine how efficiently the price is moving..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionPFE => ResourceManager.GetString("NinjaScriptIndicatorDescriptionPFE", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Pivots (Pivot Points) indicator plots the averages of the High, Low, and Close of a prior session or group of prior sessions. This is based on the historical data as provided by your market data feed provider..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionPivots => ResourceManager.GetString("NinjaScriptIndicatorDescriptionPivots", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The PPO (Percentage Price Oscillator) is based on two moving averages expressed as a percentage. The PPO is found by subtracting the longer MA from the shorter MA and then dividing the difference by the longer MA..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionPPO => ResourceManager.GetString("NinjaScriptIndicatorDescriptionPPO", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Displays ask, bid, and/or last lines on the chart..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionPriceLine => ResourceManager.GetString("NinjaScriptIndicatorDescriptionPriceLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Price Oscillator indicator shows the variation among two moving averages for the price of a security..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionPriceOscillator => ResourceManager.GetString("NinjaScriptIndicatorDescriptionPriceOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Plots the open, high, low and close values from the session starting on the prior day..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionPriorDayOHLC => ResourceManager.GetString("NinjaScriptIndicatorDescriptionPriorDayOHLC", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Psychological Line is the ratio of the number of rising bars over the specified number of bars..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionPsychologicalLine => ResourceManager.GetString("NinjaScriptIndicatorDescriptionPsychologicalLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Calculates the range of a bar..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionRange => ResourceManager.GetString("NinjaScriptIndicatorDescriptionRange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Displays the range count of a bar..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionRangeCounter => ResourceManager.GetString("NinjaScriptIndicatorDescriptionRangeCounter", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Linear regression is used to calculate a best fit line for the price data. In addition an upper and lower band is added by calculating the standard deviation of prices from the regression line..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionRegressionChannel => ResourceManager.GetString("NinjaScriptIndicatorDescriptionRegressionChannel", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Relative Vigor Index measures the strength of a trend by comparing an instruments closing price to its price range. It's based on the fact that prices tend to close higher than they open in up trends, and closer lower than they open in downtrends..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionRelativeVigorIndex => ResourceManager.GetString("NinjaScriptIndicatorDescriptionRelativeVigorIndex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to RIND (Range Indicator) compares the intraday range (high - low) to the inter-day (close - previous close) range. When the intraday range is greater than the inter-day range, the Range Indicator will be a high value. This signals an end to the current trend. When the Range Indicator is at a low level, a new trend is about to start..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionRIND => ResourceManager.GetString("NinjaScriptIndicatorDescriptionRIND", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The ROC (Rate-of-Change) indicator displays the percent change between the current price and the price x-time periods ago..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionROC => ResourceManager.GetString("NinjaScriptIndicatorDescriptionROC", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The RSI (Relative Strength Index) is a price-following oscillator that ranges between 0 and 100..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionRSI => ResourceManager.GetString("NinjaScriptIndicatorDescriptionRSI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The R-Squared indicator calculates how well the price approximates a linear regression line. The indicator gets its name from the calculation, which is, the square of the correlation coefficient (referred to in mathematics by the Greek letter rho, or r). The range of the R-Squared is from zero to one..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionRSquared => ResourceManager.GetString("NinjaScriptIndicatorDescriptionRSquared", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Relative Spread Strength of the spread between two moving averages. TASC, October 2006, p. 16..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionRSS => ResourceManager.GetString("NinjaScriptIndicatorDescriptionRSS", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The RVI (Relative Volatility Index) was developed by Donald Dorsey as a compliment to and a confirmation of momentum based indicators. When used to confirm other signals, only buy when the RVI is over 50 and only sell when the RVI is under 50..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionRVI => ResourceManager.GetString("NinjaScriptIndicatorDescriptionRVI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample script to show OnRender() capabilities.
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionSampleCustomRender => ResourceManager.GetString("NinjaScriptIndicatorDescriptionSampleCustomRender", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The SMA (Simple Moving Average) is an indicator that shows the average value of a security's price over a period of time..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionSMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionSMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Standard Deviation is a statistical measure of volatility. Standard Deviation is typically used as a component of other indicators, rather than as a stand-alone indicator. For example, Bollinger Bands are calculated by adding a security's Standard Deviation to a moving average..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionStdDev => ResourceManager.GetString("NinjaScriptIndicatorDescriptionStdDev", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Standard Error shows how near prices go around a linear regression line.  The closer the prices are to the linear regression line, the stronger is the trend..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionStdError => ResourceManager.GetString("NinjaScriptIndicatorDescriptionStdError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Stochastic Oscillator is made up of two lines that oscillate between a vertical scale of 0 to 100. The %K is the main line and it is drawn as a solid line. The second is the %D line and is a moving average of %K. The %D line is drawn as a dotted line. Use as a buy/sell signal generator, buying when fast moves above slow and selling when fast moves below slow..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionStochastics => ResourceManager.GetString("NinjaScriptIndicatorDescriptionStochastics", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Stochastic Oscillator is made up of two lines that oscillate between a vertical scale of 0 to 100. The %K is the main line and it is drawn as a solid line. The second is the %D line and is a moving average of %K. The %D line is drawn as a dotted line. Use as a buy/sell signal generator, buying when fast moves above slow and selling when fast moves below slow..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionStochasticsFast => ResourceManager.GetString("NinjaScriptIndicatorDescriptionStochasticsFast", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The StochRSI is an oscillator similar in computation to the stochastic measure, except instead of price values as input, the StochRSI uses RSI values. The StochRSI computes the current position of the RSI relative to the high and low RSI values over a specified number of days. The intent of this measure, designed by Tushar Chande and Stanley Kroll, is to provide further information about the overbought/oversold nature of the RSI. The StochRSI ranges between 0.0 and 1.0. Values above 0.8 are generally seen t [rest of string was truncated]";.
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionStochRSI => ResourceManager.GetString("NinjaScriptIndicatorDescriptionStochRSI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Sum shows the summation of the last n data points..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionSUM => ResourceManager.GetString("NinjaScriptIndicatorDescriptionSUM", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Swing indicator plots lines that represents the swing high and low points..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionSwing => ResourceManager.GetString("NinjaScriptIndicatorDescriptionSwing", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The T3 is a type of moving average, or smoothing function. It is based on the DEMA. The T3 takes the DEMA calculation and adds a vfactor which is between zero and 1. The resultant function is called the GD, or Generalized DEMA. A GD with vfactor of 1 is the same as the DEMA. A GD with a vfactor of zero is the same as an Exponential Moving Average. The T3 typically uses a vfactor of 0.7..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionT3 => ResourceManager.GetString("NinjaScriptIndicatorDescriptionT3", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The TEMA is a smoothing indicator. It was designed by Patrick Mulloy and is described in his article in the January, 1994 issue of Technical Analysis of Stocks and Commodities magazine..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionTEMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionTEMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Displays tick count of a bar.
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionTickCounter => ResourceManager.GetString("NinjaScriptIndicatorDescriptionTickCounter", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The TMA (Triangular Moving Average) is a weighted moving average. Compared to the WMA which puts more weight on the latest price bar, the TMA puts more weight on the data in the middle of the specified period..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionTMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionTMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to When a high swing is followed by a lower high swing, a trend line high is automatically plotted. When a low swing is followed by a higher low swing, a trend line low is automatically plotted..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionTrendLines => ResourceManager.GetString("NinjaScriptIndicatorDescriptionTrendLines", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The TRIX (Triple Exponential Average) displays the percentage Rate of Change (ROC) of a triple EMA. Trix oscillates above and below the zero value. The indicator applies triple smoothing in an attempt to eliminate insignificant price movements within the trend that you're trying to isolate..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionTRIX => ResourceManager.GetString("NinjaScriptIndicatorDescriptionTRIX", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The TSF (Time Series Forecast) calculates probable future values for the price by fitting a linear regression line over a given number of price bars and following that line forward into the future. A linear regression line is a straight line which is as close to all of the given price points as possible. Also see the Linear Regression indicator..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionTSF => ResourceManager.GetString("NinjaScriptIndicatorDescriptionTSF", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The TSI (True Strength Index) is a momentum-based indicator, developed by William Blau. Designed to determine both trend and overbought/oversold conditions, the TSI is applicable to intraday time frames as well as long term trading..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionTSI => ResourceManager.GetString("NinjaScriptIndicatorDescriptionTSI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Ultimate Oscillator is the weighted sum of three oscillators of different time periods. The typical time periods are 7, 14 and 28. The values of the Ultimate Oscillator range from zero to 100. Values over 70 indicate overbought conditions, and values under 30 indicate oversold conditions. Also look for agreement/divergence with the price to confirm a trend or signal the end of a trend..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionUltimateOscillator => ResourceManager.GetString("NinjaScriptIndicatorDescriptionUltimateOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The VMA (Variable Moving Average, also known as VIDYA or Variable Index Dynamic Average) is an exponential moving average that automatically adjusts the smoothing weight based on the volatility of the data series. VMA solves a problem with most moving averages. In times of low volatility, such as when the price is trending, the moving average time period should be shorter to be sensitive to the inevitable break in the trend. Whereas, in more volatile non-trending times, the moving average time period should [rest of string was truncated]";.
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionVMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionVMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume is simply the number of shares (or contracts) traded during a specified time frame (e.g. hour, day, week, month, etc)..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionVOL => ResourceManager.GetString("NinjaScriptIndicatorDescriptionVOL", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The VOLMA (Volume Moving Average) plots an exponential moving average (EMA) of volume..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionVOLMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionVOLMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Displays the volume count of each bar.
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionVolumeCounter => ResourceManager.GetString("NinjaScriptIndicatorDescriptionVolumeCounter", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Volume Oscillator measures volume by calculating the difference of a fast and a slow moving average of volume. The Volume Oscillator can provide insight into the strength or weakness of a price trend. A positive value suggests there is enough market support to continue driving price activity in the direction of the current trend. A negative value suggests there is a lack of support, that prices may begin to become stagnant or reverse..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionVolumeOscillator => ResourceManager.GetString("NinjaScriptIndicatorDescriptionVolumeOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Plots a horizontal histogram of volume by price..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionVolumeProfile => ResourceManager.GetString("NinjaScriptIndicatorDescriptionVolumeProfile", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Variation of the VOL (Volume) indicator that colors the volume histogram different color depending if the current bar is up or down bar.
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionVolumeUpDown => ResourceManager.GetString("NinjaScriptIndicatorDescriptionVolumeUpDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume Zones plots a horizontal histogram that overlays a price chart. The histogram bars stretch from left to right starting at the left side of the chart. The length of each bar is determined by the cumulative total of all volume bars for the periods during which the price fell within the vertical range of the histogram bar..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionVolumeZones => ResourceManager.GetString("NinjaScriptIndicatorDescriptionVolumeZones", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Vortex indicator is an oscillator used to identify trends. A bullish signal triggers when the VIPlus line crosses above the VIMinus line. A bearish signal triggers when the VIMinus line crosses above the VIPlus line..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionVortex => ResourceManager.GetString("NinjaScriptIndicatorDescriptionVortex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The VROC (Volume Rate-of-Change) shows whether or not a volume trend is developing in either an up or down direction. It is similar to the ROC indicator, but is applied to volume instead..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionVROC => ResourceManager.GetString("NinjaScriptIndicatorDescriptionVROC", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The VWMA (Volume-Weighted Moving Average) returns the volume-weighted moving average for the specified price series and period. VWMA is similar to a Simple Moving Average (SMA), but each bar of data is weighted by the bar's Volume. VWMA places more significance on the days with the largest volume and the least for the days with lowest volume for the period specified..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionVWMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionVWMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Williams %R is a momentum indicator that is designed to identify overbought and oversold areas in a nontrending market..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionWilliamsR => ResourceManager.GetString("NinjaScriptIndicatorDescriptionWilliamsR", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The WMA (Weighted Moving Average) is a Moving Average indicator that shows the average value of a security's price over a period of time with special emphasis on the more recent portions of the time period under analysis as opposed to the earlier..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionWMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionWMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The ZigZag indicator shows trend lines filtering out changes below a defined level..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionZigZag => ResourceManager.GetString("NinjaScriptIndicatorDescriptionZigZag", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The ZLEMA (Zero-Lag Exponential Moving Average) is an EMA variant that attempts to adjust for lag..
	/// </summary>
	public static string NinjaScriptIndicatorDescriptionZLEMA => ResourceManager.GetString("NinjaScriptIndicatorDescriptionZLEMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Diff.
	/// </summary>
	public static string NinjaScriptIndicatorDiff => ResourceManager.GetString("NinjaScriptIndicatorDiff", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Disparity line.
	/// </summary>
	public static string NinjaScriptIndicatorDisparityLine => ResourceManager.GetString("NinjaScriptIndicatorDisparityLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Down.
	/// </summary>
	public static string NinjaScriptIndicatorDown => ResourceManager.GetString("NinjaScriptIndicatorDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Lower.
	/// </summary>
	public static string NinjaScriptIndicatorLower => ResourceManager.GetString("NinjaScriptIndicatorLower", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to McClellan Oscillator line.
	/// </summary>
	public static string NinjaScriptIndicatorMcClellanOscillatorLine => ResourceManager.GetString("NinjaScriptIndicatorMcClellanOscillatorLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Middle.
	/// </summary>
	public static string NinjaScriptIndicatorMiddle => ResourceManager.GetString("NinjaScriptIndicatorMiddle", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Money flow line.
	/// </summary>
	public static string NinjaScriptIndicatorMoneyFlowLine => ResourceManager.GetString("NinjaScriptIndicatorMoneyFlowLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to ADL.
	/// </summary>
	public static string NinjaScriptIndicatorNameADL => ResourceManager.GetString("NinjaScriptIndicatorNameADL", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to ADX.
	/// </summary>
	public static string NinjaScriptIndicatorNameADX => ResourceManager.GetString("NinjaScriptIndicatorNameADX", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to ADXR.
	/// </summary>
	public static string NinjaScriptIndicatorNameADXR => ResourceManager.GetString("NinjaScriptIndicatorNameADXR", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to APZ.
	/// </summary>
	public static string NinjaScriptIndicatorNameAPZ => ResourceManager.GetString("NinjaScriptIndicatorNameAPZ", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Aroon.
	/// </summary>
	public static string NinjaScriptIndicatorNameAroon => ResourceManager.GetString("NinjaScriptIndicatorNameAroon", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Aroon oscillator.
	/// </summary>
	public static string NinjaScriptIndicatorNameAroonOscillator => ResourceManager.GetString("NinjaScriptIndicatorNameAroonOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to ATR.
	/// </summary>
	public static string NinjaScriptIndicatorNameATR => ResourceManager.GetString("NinjaScriptIndicatorNameATR", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bar timer.
	/// </summary>
	public static string NinjaScriptIndicatorNameBarTimer => ResourceManager.GetString("NinjaScriptIndicatorNameBarTimer", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Block volume.
	/// </summary>
	public static string NinjaScriptIndicatorNameBlockVolume => ResourceManager.GetString("NinjaScriptIndicatorNameBlockVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bollinger.
	/// </summary>
	public static string NinjaScriptIndicatorNameBollinger => ResourceManager.GetString("NinjaScriptIndicatorNameBollinger", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to BOP.
	/// </summary>
	public static string NinjaScriptIndicatorNameBOP => ResourceManager.GetString("NinjaScriptIndicatorNameBOP", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Buy sell pressure.
	/// </summary>
	public static string NinjaScriptIndicatorNameBuySellPressure => ResourceManager.GetString("NinjaScriptIndicatorNameBuySellPressure", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Buy sell volume.
	/// </summary>
	public static string NinjaScriptIndicatorNameBuySellVolume => ResourceManager.GetString("NinjaScriptIndicatorNameBuySellVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Camarilla pivots.
	/// </summary>
	public static string NinjaScriptIndicatorNameCamarillaPivots => ResourceManager.GetString("NinjaScriptIndicatorNameCamarillaPivots", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Candlestick pattern.
	/// </summary>
	public static string NinjaScriptIndicatorNameCandlestickPattern => ResourceManager.GetString("NinjaScriptIndicatorNameCandlestickPattern", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to CCI.
	/// </summary>
	public static string NinjaScriptIndicatorNameCCI => ResourceManager.GetString("NinjaScriptIndicatorNameCCI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Chaikin money flow.
	/// </summary>
	public static string NinjaScriptIndicatorNameChaikinMoneyFlow => ResourceManager.GetString("NinjaScriptIndicatorNameChaikinMoneyFlow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Chaikin oscillator.
	/// </summary>
	public static string NinjaScriptIndicatorNameChaikinOscillator => ResourceManager.GetString("NinjaScriptIndicatorNameChaikinOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Chaikin volatility.
	/// </summary>
	public static string NinjaScriptIndicatorNameChaikinVolatility => ResourceManager.GetString("NinjaScriptIndicatorNameChaikinVolatility", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Choppiness index.
	/// </summary>
	public static string NinjaScriptIndicatorNameChoppinessIndex => ResourceManager.GetString("NinjaScriptIndicatorNameChoppinessIndex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to CMO.
	/// </summary>
	public static string NinjaScriptIndicatorNameCMO => ResourceManager.GetString("NinjaScriptIndicatorNameCMO", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Constant lines.
	/// </summary>
	public static string NinjaScriptIndicatorNameConstantLines => ResourceManager.GetString("NinjaScriptIndicatorNameConstantLines", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Correlation.
	/// </summary>
	public static string NinjaScriptIndicatorNameCorrelation => ResourceManager.GetString("NinjaScriptIndicatorNameCorrelation", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to COT.
	/// </summary>
	public static string NinjaScriptIndicatorNameCOT => ResourceManager.GetString("NinjaScriptIndicatorNameCOT", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current day OHL.
	/// </summary>
	public static string NinjaScriptIndicatorNameCurrentDayOHL => ResourceManager.GetString("NinjaScriptIndicatorNameCurrentDayOHL", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Darvas.
	/// </summary>
	public static string NinjaScriptIndicatorNameDarvas => ResourceManager.GetString("NinjaScriptIndicatorNameDarvas", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to DEMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameDEMA => ResourceManager.GetString("NinjaScriptIndicatorNameDEMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Disparity index.
	/// </summary>
	public static string NinjaScriptIndicatorNameDisparityIndex => ResourceManager.GetString("NinjaScriptIndicatorNameDisparityIndex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to DM.
	/// </summary>
	public static string NinjaScriptIndicatorNameDM => ResourceManager.GetString("NinjaScriptIndicatorNameDM", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to DMI.
	/// </summary>
	public static string NinjaScriptIndicatorNameDMI => ResourceManager.GetString("NinjaScriptIndicatorNameDMI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to DM index.
	/// </summary>
	public static string NinjaScriptIndicatorNameDMIndex => ResourceManager.GetString("NinjaScriptIndicatorNameDMIndex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Donchian channel.
	/// </summary>
	public static string NinjaScriptIndicatorNameDonchianChannel => ResourceManager.GetString("NinjaScriptIndicatorNameDonchianChannel", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Double stochastics.
	/// </summary>
	public static string NinjaScriptIndicatorNameDoubleStochastics => ResourceManager.GetString("NinjaScriptIndicatorNameDoubleStochastics", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ease of movement.
	/// </summary>
	public static string NinjaScriptIndicatorNameEaseOfMovement => ResourceManager.GetString("NinjaScriptIndicatorNameEaseOfMovement", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to EMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameEMA => ResourceManager.GetString("NinjaScriptIndicatorNameEMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fibonacci pivots.
	/// </summary>
	public static string NinjaScriptIndicatorNameFibonacciPivots => ResourceManager.GetString("NinjaScriptIndicatorNameFibonacciPivots", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Fisher transform.
	/// </summary>
	public static string NinjaScriptIndicatorNameFisherTransform => ResourceManager.GetString("NinjaScriptIndicatorNameFisherTransform", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to FOSC.
	/// </summary>
	public static string NinjaScriptIndicatorNameFOSC => ResourceManager.GetString("NinjaScriptIndicatorNameFOSC", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to HMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameHMA => ResourceManager.GetString("NinjaScriptIndicatorNameHMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ichimoku Cloud.
	/// </summary>
	public static string NinjaScriptIndicatorNameIchimokuCloud => ResourceManager.GetString("NinjaScriptIndicatorNameIchimokuCloud", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to KAMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameKAMA => ResourceManager.GetString("NinjaScriptIndicatorNameKAMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Keltner channel.
	/// </summary>
	public static string NinjaScriptIndicatorNameKelterChannel => ResourceManager.GetString("NinjaScriptIndicatorNameKelterChannel", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Key reversal down.
	/// </summary>
	public static string NinjaScriptIndicatorNameKeyReversalDown => ResourceManager.GetString("NinjaScriptIndicatorNameKeyReversalDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Key reversal up.
	/// </summary>
	public static string NinjaScriptIndicatorNameKeyReversalUp => ResourceManager.GetString("NinjaScriptIndicatorNameKeyReversalUp", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Lin. reg..
	/// </summary>
	public static string NinjaScriptIndicatorNameLinReg => ResourceManager.GetString("NinjaScriptIndicatorNameLinReg", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Lin. reg. intercept.
	/// </summary>
	public static string NinjaScriptIndicatorNameLinRegIntercept => ResourceManager.GetString("NinjaScriptIndicatorNameLinRegIntercept", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Lin. reg. slope.
	/// </summary>
	public static string NinjaScriptIndicatorNameLinRegSlope => ResourceManager.GetString("NinjaScriptIndicatorNameLinRegSlope", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to MACD.
	/// </summary>
	public static string NinjaScriptIndicatorNameMACD => ResourceManager.GetString("NinjaScriptIndicatorNameMACD", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to MA envelopes.
	/// </summary>
	public static string NinjaScriptIndicatorNameMAEnvelopes => ResourceManager.GetString("NinjaScriptIndicatorNameMAEnvelopes", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to MAMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameMAMA => ResourceManager.GetString("NinjaScriptIndicatorNameMAMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to MAX.
	/// </summary>
	public static string NinjaScriptIndicatorNameMAX => ResourceManager.GetString("NinjaScriptIndicatorNameMAX", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to McClellan oscillator.
	/// </summary>
	public static string NinjaScriptIndicatorNameMcClellanOscillator => ResourceManager.GetString("NinjaScriptIndicatorNameMcClellanOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to MFI.
	/// </summary>
	public static string NinjaScriptIndicatorNameMFI => ResourceManager.GetString("NinjaScriptIndicatorNameMFI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to MIN.
	/// </summary>
	public static string NinjaScriptIndicatorNameMIN => ResourceManager.GetString("NinjaScriptIndicatorNameMIN", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Momentum.
	/// </summary>
	public static string NinjaScriptIndicatorNameMomentum => ResourceManager.GetString("NinjaScriptIndicatorNameMomentum", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Money flow oscillator.
	/// </summary>
	public static string NinjaScriptIndicatorNameMoneyFlowOscillator => ResourceManager.GetString("NinjaScriptIndicatorNameMoneyFlowOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Moving average ribbon.
	/// </summary>
	public static string NinjaScriptIndicatorNameMovingAverageRibbon => ResourceManager.GetString("NinjaScriptIndicatorNameMovingAverageRibbon", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to N bars down.
	/// </summary>
	public static string NinjaScriptIndicatorNameNBarsDown => ResourceManager.GetString("NinjaScriptIndicatorNameNBarsDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to N bars up.
	/// </summary>
	public static string NinjaScriptIndicatorNameNBarsUp => ResourceManager.GetString("NinjaScriptIndicatorNameNBarsUp", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Net change display.
	/// </summary>
	public static string NinjaScriptIndicatorNameNetChangeDisplay => ResourceManager.GetString("NinjaScriptIndicatorNameNetChangeDisplay", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to OBV.
	/// </summary>
	public static string NinjaScriptIndicatorNameOBV => ResourceManager.GetString("NinjaScriptIndicatorNameOBV", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Parabolic SAR.
	/// </summary>
	public static string NinjaScriptIndicatorNameParabolicSAR => ResourceManager.GetString("NinjaScriptIndicatorNameParabolicSAR", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to PFE.
	/// </summary>
	public static string NinjaScriptIndicatorNamePFE => ResourceManager.GetString("NinjaScriptIndicatorNamePFE", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Pivots.
	/// </summary>
	public static string NinjaScriptIndicatorNamePivots => ResourceManager.GetString("NinjaScriptIndicatorNamePivots", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to PPO.
	/// </summary>
	public static string NinjaScriptIndicatorNamePPO => ResourceManager.GetString("NinjaScriptIndicatorNamePPO", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Price line.
	/// </summary>
	public static string NinjaScriptIndicatorNamePriceLine => ResourceManager.GetString("NinjaScriptIndicatorNamePriceLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Price oscillator.
	/// </summary>
	public static string NinjaScriptIndicatorNamePriceOscillator => ResourceManager.GetString("NinjaScriptIndicatorNamePriceOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Prior day OHLC.
	/// </summary>
	public static string NinjaScriptIndicatorNamePriorDayOHLC => ResourceManager.GetString("NinjaScriptIndicatorNamePriorDayOHLC", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Psychological line.
	/// </summary>
	public static string NinjaScriptIndicatorNamePsychologicalLine => ResourceManager.GetString("NinjaScriptIndicatorNamePsychologicalLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Range.
	/// </summary>
	public static string NinjaScriptIndicatorNameRange => ResourceManager.GetString("NinjaScriptIndicatorNameRange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Range counter.
	/// </summary>
	public static string NinjaScriptIndicatorNameRangeCounter => ResourceManager.GetString("NinjaScriptIndicatorNameRangeCounter", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Regression channel.
	/// </summary>
	public static string NinjaScriptIndicatorNameRegressionChannel => ResourceManager.GetString("NinjaScriptIndicatorNameRegressionChannel", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Relative vigor index.
	/// </summary>
	public static string NinjaScriptIndicatorNameRelativeVigorIndex => ResourceManager.GetString("NinjaScriptIndicatorNameRelativeVigorIndex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to RIND.
	/// </summary>
	public static string NinjaScriptIndicatorNameRIND => ResourceManager.GetString("NinjaScriptIndicatorNameRIND", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to ROC.
	/// </summary>
	public static string NinjaScriptIndicatorNameROC => ResourceManager.GetString("NinjaScriptIndicatorNameROC", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to RSI.
	/// </summary>
	public static string NinjaScriptIndicatorNameRSI => ResourceManager.GetString("NinjaScriptIndicatorNameRSI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to R squared.
	/// </summary>
	public static string NinjaScriptIndicatorNameRSquared => ResourceManager.GetString("NinjaScriptIndicatorNameRSquared", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to RSS.
	/// </summary>
	public static string NinjaScriptIndicatorNameRSS => ResourceManager.GetString("NinjaScriptIndicatorNameRSS", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to RVI.
	/// </summary>
	public static string NinjaScriptIndicatorNameRVI => ResourceManager.GetString("NinjaScriptIndicatorNameRVI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample custom render.
	/// </summary>
	public static string NinjaScriptIndicatorNameSampleCustomRender => ResourceManager.GetString("NinjaScriptIndicatorNameSampleCustomRender", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to SMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameSMA => ResourceManager.GetString("NinjaScriptIndicatorNameSMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Std. dev..
	/// </summary>
	public static string NinjaScriptIndicatorNameStdDev => ResourceManager.GetString("NinjaScriptIndicatorNameStdDev", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Std. error.
	/// </summary>
	public static string NinjaScriptIndicatorNameStdError => ResourceManager.GetString("NinjaScriptIndicatorNameStdError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Stochastics.
	/// </summary>
	public static string NinjaScriptIndicatorNameStochastics => ResourceManager.GetString("NinjaScriptIndicatorNameStochastics", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Stochastics fast.
	/// </summary>
	public static string NinjaScriptIndicatorNameStochasticsFast => ResourceManager.GetString("NinjaScriptIndicatorNameStochasticsFast", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Stoch RSI.
	/// </summary>
	public static string NinjaScriptIndicatorNameStochRSI => ResourceManager.GetString("NinjaScriptIndicatorNameStochRSI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to SUM.
	/// </summary>
	public static string NinjaScriptIndicatorNameSUM => ResourceManager.GetString("NinjaScriptIndicatorNameSUM", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Swing.
	/// </summary>
	public static string NinjaScriptIndicatorNameSwing => ResourceManager.GetString("NinjaScriptIndicatorNameSwing", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to T3.
	/// </summary>
	public static string NinjaScriptIndicatorNameT3 => ResourceManager.GetString("NinjaScriptIndicatorNameT3", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to TEMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameTEMA => ResourceManager.GetString("NinjaScriptIndicatorNameTEMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Tick counter.
	/// </summary>
	public static string NinjaScriptIndicatorNameTickCounter => ResourceManager.GetString("NinjaScriptIndicatorNameTickCounter", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to TMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameTMA => ResourceManager.GetString("NinjaScriptIndicatorNameTMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Trend lines.
	/// </summary>
	public static string NinjaScriptIndicatorNameTrendLines => ResourceManager.GetString("NinjaScriptIndicatorNameTrendLines", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to TRIX.
	/// </summary>
	public static string NinjaScriptIndicatorNameTRIX => ResourceManager.GetString("NinjaScriptIndicatorNameTRIX", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to TSF.
	/// </summary>
	public static string NinjaScriptIndicatorNameTSF => ResourceManager.GetString("NinjaScriptIndicatorNameTSF", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to TSI.
	/// </summary>
	public static string NinjaScriptIndicatorNameTSI => ResourceManager.GetString("NinjaScriptIndicatorNameTSI", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ultimate oscillator.
	/// </summary>
	public static string NinjaScriptIndicatorNameUltimateOscillator => ResourceManager.GetString("NinjaScriptIndicatorNameUltimateOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to VMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameVMA => ResourceManager.GetString("NinjaScriptIndicatorNameVMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to VOL.
	/// </summary>
	public static string NinjaScriptIndicatorNameVOL => ResourceManager.GetString("NinjaScriptIndicatorNameVOL", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to VOLMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameVOLMA => ResourceManager.GetString("NinjaScriptIndicatorNameVOLMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume counter.
	/// </summary>
	public static string NinjaScriptIndicatorNameVolumeCounter => ResourceManager.GetString("NinjaScriptIndicatorNameVolumeCounter", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume oscillator.
	/// </summary>
	public static string NinjaScriptIndicatorNameVolumeOscillator => ResourceManager.GetString("NinjaScriptIndicatorNameVolumeOscillator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume profile.
	/// </summary>
	public static string NinjaScriptIndicatorNameVolumeProfile => ResourceManager.GetString("NinjaScriptIndicatorNameVolumeProfile", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume zones.
	/// </summary>
	public static string NinjaScriptIndicatorNameVolumesZones => ResourceManager.GetString("NinjaScriptIndicatorNameVolumesZones", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume up down.
	/// </summary>
	public static string NinjaScriptIndicatorNameVolumeUpDown => ResourceManager.GetString("NinjaScriptIndicatorNameVolumeUpDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Vortex.
	/// </summary>
	public static string NinjaScriptIndicatorNameVortex => ResourceManager.GetString("NinjaScriptIndicatorNameVortex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to VROC.
	/// </summary>
	public static string NinjaScriptIndicatorNameVROC => ResourceManager.GetString("NinjaScriptIndicatorNameVROC", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to VWMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameVWMA => ResourceManager.GetString("NinjaScriptIndicatorNameVWMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Williams R.
	/// </summary>
	public static string NinjaScriptIndicatorNameWilliamsR => ResourceManager.GetString("NinjaScriptIndicatorNameWilliamsR", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to WMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameWMA => ResourceManager.GetString("NinjaScriptIndicatorNameWMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Zig zag.
	/// </summary>
	public static string NinjaScriptIndicatorNameZigZag => ResourceManager.GetString("NinjaScriptIndicatorNameZigZag", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to ZLEMA.
	/// </summary>
	public static string NinjaScriptIndicatorNameZLEMA => ResourceManager.GetString("NinjaScriptIndicatorNameZLEMA", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Neutral.
	/// </summary>
	public static string NinjaScriptIndicatorNeutral => ResourceManager.GetString("NinjaScriptIndicatorNeutral", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Overbought.
	/// </summary>
	public static string NinjaScriptIndicatorOverbought => ResourceManager.GetString("NinjaScriptIndicatorOverbought", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Over bought line.
	/// </summary>
	public static string NinjaScriptIndicatorOverBoughtLine => ResourceManager.GetString("NinjaScriptIndicatorOverBoughtLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Oversold.
	/// </summary>
	public static string NinjaScriptIndicatorOversold => ResourceManager.GetString("NinjaScriptIndicatorOversold", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Over sold line.
	/// </summary>
	public static string NinjaScriptIndicatorOverSoldLine => ResourceManager.GetString("NinjaScriptIndicatorOverSoldLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Relative Vigor Index.
	/// </summary>
	public static string NinjaScriptIndicatorRelativeVigorIndex => ResourceManager.GetString("NinjaScriptIndicatorRelativeVigorIndex", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Signal.
	/// </summary>
	public static string NinjaScriptIndicatorSignal => ResourceManager.GetString("NinjaScriptIndicatorSignal", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Up.
	/// </summary>
	public static string NinjaScriptIndicatorUp => ResourceManager.GetString("NinjaScriptIndicatorUp", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Upper.
	/// </summary>
	public static string NinjaScriptIndicatorUpper => ResourceManager.GetString("NinjaScriptIndicatorUpper", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to VIMinus.
	/// </summary>
	public static string NinjaScriptIndicatorVIMinus => ResourceManager.GetString("NinjaScriptIndicatorVIMinus", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to VIPlus.
	/// </summary>
	public static string NinjaScriptIndicatorVIPlus => ResourceManager.GetString("NinjaScriptIndicatorVIPlus", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Visual.
	/// </summary>
	public static string NinjaScriptIndicatorVisualGroup => ResourceManager.GetString("NinjaScriptIndicatorVisualGroup", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Zero line.
	/// </summary>
	public static string NinjaScriptIndicatorZeroLine => ResourceManager.GetString("NinjaScriptIndicatorZeroLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Visible only when in focus.
	/// </summary>
	public static string NinjaScriptIsVisibleOnlyFocused => ResourceManager.GetString("NinjaScriptIsVisibleOnlyFocused", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line.
	/// </summary>
	public static string NinjaScriptLine => ResourceManager.GetString("NinjaScriptLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Lines.
	/// </summary>
	public static string NinjaScriptLines => ResourceManager.GetString("NinjaScriptLines", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to   Loading data... {0}.
	/// </summary>
	public static string NinjaScriptLoadingData => ResourceManager.GetString("NinjaScriptLoadingData", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current ask price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionAskPrice => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionAskPrice", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current ask size.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionAskSize => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionAskSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Average daily volume.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionAverageDailyVolume => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionAverageDailyVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to A measure of the volatility, or systematic risk, of a security or a portfolio in comparison to the market as a whole..
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionBeta => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionBeta", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The difference between current bid and ask prices.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionBidAskSpread => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionBidAskSpread", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current bid price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionBidPrice => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionBidPrice", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current bid size.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionBidSize => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionBidSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to High price for current calendar year.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionCalendarYearHigh => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionCalendarYearHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Date the high price for current calendar year occurred.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionCalendarYearHighDate => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionCalendarYearHighDate", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Low price for current calendar year.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionCalendarYearLow => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionCalendarYearLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Date the low price for current calendar year occurred.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionCalendarYearLowDate => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionCalendarYearLowDate", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to This Market Analyzer column plots a mini chart per the input properties..
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionChartMini => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionChartMini", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to This Market Analyzer column plots a mini chart per the input properties..
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionChartNetChange => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionChartNetChange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current assets divided by current liabilities.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionCurrentRatio => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionCurrentRatio", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Today's high.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionDailyHigh => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionDailyHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Today's low.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionDailyLow => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionDailyLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Today's volume.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionDailyVolume => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionDailyVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Displays how many days away from rollover to next contract.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionDaysUntilRollover => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionDaysUntilRollover", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Instrument description.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionDescription => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Dividend amount.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionDividendAmount => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionDividendAmount", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Dividend pay date.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionDividendPayDate => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionDividendPayDate", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ratio that shows how much a company pays out in dividends each year relative to its share price. .
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionDividendYield => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionDividendYield", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Portion of a company's earnings allocated to each outstanding share of common stock..
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionEarningsPerShare => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionEarningsPerShare", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Five years growth percentage.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionFiveYearsGrowthPercentage => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionFiveYearsGrowthPercentage", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to High of last 52 weeks.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionHigh52Weeks => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionHigh52Weeks", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Date the high price of last 52 weeks occurred.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionHigh52WeeksDate => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionHigh52WeeksDate", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Realized volatility of an instrument over time.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionHistoricalVolatility => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionHistoricalVolatility", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Instrument name.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionInstrument => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionInstrument", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Close of last trading session.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionLastClose => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionLastClose", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Last traded price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionLastPrice => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionLastPrice", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Last trade size.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionLastSize => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionLastSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Low of last 52 weeks.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionLow52Weeks => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionLow52Weeks", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Date the low price of last 52 weeks occurred.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionLow52WeeksDate => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionLow52WeeksDate", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Market capitalization. The total value of issued shares..
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionMarketCap => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionMarketCap", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current price and net change.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionMarketPrice => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionMarketPrice", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current price compared to last close price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionNetChange => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionNetChange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current low compared to last close price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionNetChangeMaxDown => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionNetChangeMaxDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current high compared to last close price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionNetChangeMaxUp => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionNetChangeMaxUp", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Projected earnings per share.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionNextYearsEarningsPerShare => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionNextYearsEarningsPerShare", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to User definable field. Double click on applied notes column to create or edit notes..
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionNotes => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionNotes", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Open price for current trading session.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionOpening => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionOpening", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The total number of options and/or futures contracts that are not closed or delivered on a particular day.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionOpenInterest => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionOpenInterest", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Percentage of shares held by institutions.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionPercentHeldByInstitutions => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionPercentHeldByInstitutions", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Average entry price of current position.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionPositionAvgPrice => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionPositionAvgPrice", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current position size.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionPositionSize => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionPositionSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current share price compared to its per-share earnings..
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionPriceEarningsRatio => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionPriceEarningsRatio", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Total of unrealized and realized profit and loss. .
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionProfitLoss => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionProfitLoss", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Realized profit or loss.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionRealizedProfitLoss => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionRealizedProfitLoss", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ratio of revenue to share price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionRevenuePerShare => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionRevenuePerShare", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Today's settlement price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionSettlement => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionSettlement", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Number of shares outstanding.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionSharesOutstanding => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionSharesOutstanding", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Quantity of stock shares that investors have sold short but not yet covered or closed out..
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionShortInterest => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionShortInterest", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Short interest divided by average daily volume.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionShortInterestRatio => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionShortInterestRatio", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Time the last trade occurred.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionTimeLastTick => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionTimeLastTick", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Today's filled contracts.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionTradedContracts => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionTradedContracts", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to This columndisplays a colored bar that represents the incoming ticks with the same colors that the T &amp; S window uses.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionTSTrend => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionTSTrend", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Profit or loss for the current position. .
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionUnrealizedProfitLoss => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionUnrealizedProfitLoss", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume weighted average price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnDescriptionVwap => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnDescriptionVwap", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ask price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameAskPrice => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameAskPrice", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ask size.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameAskSize => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameAskSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Average daily volume.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameAverageDailyVolume => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameAverageDailyVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Beta.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameBeta => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameBeta", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid ask spread.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameBidAskSpread => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameBidAskSpread", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameBidPrice => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameBidPrice", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid size.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameBidSize => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameBidSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Calendar year high.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameCalendarYearHigh => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameCalendarYearHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Calendar year high date.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameCalendarYearHighDate => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameCalendarYearHighDate", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Calendar year low.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameCalendarYearLow => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameCalendarYearLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Calendar year low date.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameCalendarYearLowDate => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameCalendarYearLowDate", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Chart - Mini.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameChartMini => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameChartMini", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Chart - Net change.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameChartNetChange => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameChartNetChange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current ratio.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameCurrentRatio => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameCurrentRatio", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Daily high.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameDailyHigh => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameDailyHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Daily low.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameDailyLow => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameDailyLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Daily volume.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameDailyVolume => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameDailyVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Days until rollover.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameDaysUntilRollover => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameDaysUntilRollover", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Description.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameDescription => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Dividend amount.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameDividendAmount => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameDividendAmount", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Dividend pay date.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameDividendPayDate => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameDividendPayDate", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Dividend yield.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameDividendYield => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameDividendYield", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Earnings per share.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameEarningsPerShare => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameEarningsPerShare", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Five years growth percentage.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameFiveYearsGrowthPercentage => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameFiveYearsGrowthPercentage", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to High 52 weeks.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameHigh52Weeks => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameHigh52Weeks", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to High 52 weeks date.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameHigh52WeeksDate => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameHigh52WeeksDate", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Historical volatility.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameHistoricalVolatility => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameHistoricalVolatility", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Instrument.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameInstrument => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameInstrument", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Last close.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameLastClose => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameLastClose", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Last price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameLastPrice => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameLastPrice", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Last size.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameLastSize => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameLastSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Low 52 weeks.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameLow52Weeks => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameLow52Weeks", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Low 52 weeks date.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameLow52WeeksDate => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameLow52WeeksDate", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Market capitalization.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameMarketCap => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameMarketCap", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Market price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameMarketPrice => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameMarketPrice", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Net change.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameNetChange => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameNetChange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Net change max down.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameNetChangeMaxDown => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameNetChangeMaxDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Net change max up.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameNetChangeMaxUp => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameNetChangeMaxUp", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Next year earnings per share.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameNextYearsEarningsPerShare => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameNextYearsEarningsPerShare", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Notes.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameNotes => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameNotes", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Opening.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameOpening => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameOpening", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Open interest.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameOpenInterest => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameOpenInterest", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Percent held by institutions.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNamePercentHeldByInstitutions => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNamePercentHeldByInstitutions", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Position avg. price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNamePositionAvgPrice => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNamePositionAvgPrice", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Position size.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNamePositionSize => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNamePositionSize", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Price earnings ratio.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNamePriceEarningsRatio => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNamePriceEarningsRatio", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Profit loss.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameProfitLoss => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameProfitLoss", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Realized profit loss.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameRealizedProfitLoss => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameRealizedProfitLoss", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Revenue per share.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameRevenuePerShare => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameRevenuePerShare", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Settlement price.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameSettlement => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameSettlement", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Shares outstanding.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameSharesOutstanding => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameSharesOutstanding", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Short interest.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameShortInterest => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameShortInterest", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Short interest ratio.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameShortInterestRatio => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameShortInterestRatio", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Time last tick.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameTimeLastTick => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameTimeLastTick", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Traded contracts.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameTradedContracts => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameTradedContracts", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to T &amp; S trend.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameTSTrend => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameTSTrend", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Unrealized profit loss.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameUnrealizedProfitLoss => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameUnrealizedProfitLoss", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to VWAP.
	/// </summary>
	public static string NinjaScriptMarketAnalyzerColumnNameVwap => ResourceManager.GetString("NinjaScriptMarketAnalyzerColumnNameVwap", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Rows.
	/// </summary>
	public static string NinjaScriptNumberOfRows => ResourceManager.GetString("NinjaScriptNumberOfRows", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} relies on bid/ask tick updates expecting Calculate 'On each tick'.
	/// </summary>
	public static string NinjaScriptOnBarCloseError => ResourceManager.GetString("NinjaScriptOnBarCloseError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} relies on volume updates expecting Calculate 'On each tick' or 'On bar close'.
	/// </summary>
	public static string NinjaScriptOnPriceChangeError => ResourceManager.GetString("NinjaScriptOnPriceChangeError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. avg. favorable excursion.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxAvgMfe => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxAvgMfe", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. avg. favorable excursion (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxAvgMfeLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxAvgMfeLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. avg. favorable excursion (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxAvgMfeShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxAvgMfeShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. avg. profit.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxAvgProfit => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxAvgProfit", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. avg. profit (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxAvgProfitLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxAvgProfitLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. avg. profit (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxAvgProfitShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxAvgProfitShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. net profit.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxNetProfit => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxNetProfit", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. net profit (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxNetProfitLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxNetProfitLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. net profit (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxNetProfitShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxNetProfitShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. % profitable.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxPercentProfitable => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxPercentProfitable", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. % profitable (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxPercentProfitableLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxPercentProfitableLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. % profitable (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxPercentProfitableShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxPercentProfitableShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. probability.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxProbablity => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxProbablity", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. probability (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxProbablityLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxProbablityLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. probability (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxProbablityShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxProbablityShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. profit factor.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxProfitFactor => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxProfitFactor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. profit factor (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxProfitFactorLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxProfitFactorLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. profit factor (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxProfitFactorShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxProfitFactorShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. R^2.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxR2 => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxR2", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. R^2 (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxR2Long => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxR2Long", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. R^2 (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxR2Short => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxR2Short", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. Sharpe ratio.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxSharpeRatio => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxSharpeRatio", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. Sharpe ratio (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxSharpeRatioLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxSharpeRatioLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. Sharpe ratio (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxSharpeRatioShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxSharpeRatioShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. Sortino ratio.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxSortinoRatio => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxSortinoRatio", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. Sortino ratio (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxSortinoRatioLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxSortinoRatioLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. Sortino ratio (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxSortinoRatioShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxSortinoRatioShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. strength.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxStrength => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxStrength", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. strength (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxStrengthLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxStrengthLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. strength (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxStrengthShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxStrengthShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. Ulcer ratio.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxUlcerRatio => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxUlcerRatio", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. Ulcer ratio (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxUlcerRatioLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxUlcerRatioLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. Ulcer ratio (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxUlcerRatioShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxUlcerRatioShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. win/loss ratio.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxWinLossRatio => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxWinLossRatio", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. win/loss ratio (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxWinLossRatioLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxWinLossRatioLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Max. win/loss ratio (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMaxWinLossRatioShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMaxWinLossRatioShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Min. avg. adverse excursion.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMinAvgMae => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMinAvgMae", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Min. avg. adverse excursion (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMinAvgMaeLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMinAvgMaeLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Min. avg. adverse excursion (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMinAvgMaeShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMinAvgMaeShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Min. draw down.
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMinDrawDown => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMinDrawDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Min. draw down (long).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMinDrawDownLong => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMinDrawDownLong", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Min. draw down (short).
	/// </summary>
	public static string NinjaScriptOptimizationFitnessNameMinDrawDownShort => ResourceManager.GetString("NinjaScriptOptimizationFitnessNameMinDrawDownShort", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Default.
	/// </summary>
	public static string NinjaScriptOptimizerDefault => ResourceManager.GetString("NinjaScriptOptimizerDefault", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Genetic.
	/// </summary>
	public static string NinjaScriptOptimizerGenetic => ResourceManager.GetString("NinjaScriptOptimizerGenetic", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Parameters.
	/// </summary>
	public static string NinjaScriptParameters => ResourceManager.GetString("NinjaScriptParameters", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ask background.
	/// </summary>
	public static string NinjaScriptRecentColumnAskBackground => ResourceManager.GetString("NinjaScriptRecentColumnAskBackground", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ask foreground.
	/// </summary>
	public static string NinjaScriptRecentColumnAskForeground => ResourceManager.GetString("NinjaScriptRecentColumnAskForeground", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid background.
	/// </summary>
	public static string NinjaScriptRecentColumnBidBackground => ResourceManager.GetString("NinjaScriptRecentColumnBidBackground", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid foreground.
	/// </summary>
	public static string NinjaScriptRecentColumnBidForeground => ResourceManager.GetString("NinjaScriptRecentColumnBidForeground", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Display.
	/// </summary>
	public static string NinjaScriptRecentColumnDiplay => ResourceManager.GetString("NinjaScriptRecentColumnDiplay", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Reset tolerance.
	/// </summary>
	public static string NinjaScriptRecentColumnResetTolerance => ResourceManager.GetString("NinjaScriptRecentColumnResetTolerance", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Reset when.
	/// </summary>
	public static string NinjaScriptRecentColumnResetWhen => ResourceManager.GetString("NinjaScriptRecentColumnResetWhen", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Setup.
	/// </summary>
	public static string NinjaScriptSetup => ResourceManager.GetString("NinjaScriptSetup", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Advanced trade management sample strategy..
	/// </summary>
	public static string NinjaScriptStrategyDescriptionSampleATMStrategy => ResourceManager.GetString("NinjaScriptStrategyDescriptionSampleATMStrategy", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample to demonstrate usage of custom performance.
	/// </summary>
	public static string NinjaScriptStrategyDescriptionSampleCustomPerformance => ResourceManager.GetString("NinjaScriptStrategyDescriptionSampleCustomPerformance", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to This strategy demonstrates some of the capabilities of the NinjaTrader Development Framework.
	/// </summary>
	public static string NinjaScriptStrategyDescriptionSampleFramework => ResourceManager.GetString("NinjaScriptStrategyDescriptionSampleFramework", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Simple moving average cross over strategy..
	/// </summary>
	public static string NinjaScriptStrategyDescriptionSampleMACrossOver => ResourceManager.GetString("NinjaScriptStrategyDescriptionSampleMACrossOver", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Multi-Instrument sample strategy..
	/// </summary>
	public static string NinjaScriptStrategyDescriptionSampleMultiInstrument => ResourceManager.GetString("NinjaScriptStrategyDescriptionSampleMultiInstrument", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Multi-time frame sample strategy..
	/// </summary>
	public static string NinjaScriptStrategyDescriptionSampleMultiTimeFrame => ResourceManager.GetString("NinjaScriptStrategyDescriptionSampleMultiTimeFrame", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Strategy generator.
	/// </summary>
	public static string NinjaScriptStrategyGenerator => ResourceManager.GetString("NinjaScriptStrategyGenerator", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 1 candle stick pattern|{0} candle stick patterns|Add candle stick pattern...|Configure candle stick pattern...|Configure candle stick patterns....
	/// </summary>
	public static string NinjaScriptStrategyGeneratorCandleStickPatternPrompt => ResourceManager.GetString("NinjaScriptStrategyGeneratorCandleStickPatternPrompt", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Entry conditions.
	/// </summary>
	public static string NinjaScriptStrategyGeneratorEntries => ResourceManager.GetString("NinjaScriptStrategyGeneratorEntries", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to You needed to at least one entry order exit condition..
	/// </summary>
	public static string NinjaScriptStrategyGeneratorEntriesOrExits => ResourceManager.GetString("NinjaScriptStrategyGeneratorEntriesOrExits", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Exception on expression:{0}{1}.
	/// </summary>
	public static string NinjaScriptStrategyGeneratorIndicatorException => ResourceManager.GetString("NinjaScriptStrategyGeneratorIndicatorException", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to 1 indicator|{0} indicators|Add indicator...|Configure indicator...|Configure indicators....
	/// </summary>
	public static string NinjaScriptStrategyGeneratorIndicatorsPrompt => ResourceManager.GetString("NinjaScriptStrategyGeneratorIndicatorsPrompt", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Performance for {0} = {1}.
	/// </summary>
	public static string NinjaScriptStrategyGeneratorPeformance => ResourceManager.GetString("NinjaScriptStrategyGeneratorPeformance", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to AI Generate Properties.
	/// </summary>
	public static string NinjaScriptStrategyGeneratorProperties => ResourceManager.GetString("NinjaScriptStrategyGeneratorProperties", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Strategy generator terminated after {0} generations, since there was no performance improvement for {1} generations.
	/// </summary>
	public static string NinjaScriptStrategyGeneratorTerminated => ResourceManager.GetString("NinjaScriptStrategyGeneratorTerminated", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Candle stick pattern.
	/// </summary>
	public static string NinjaScriptStrategyGeneratorUseCandleStickPattern => ResourceManager.GetString("NinjaScriptStrategyGeneratorUseCandleStickPattern", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Indicators.
	/// </summary>
	public static string NinjaScriptStrategyGeneratorUseIndicators => ResourceManager.GetString("NinjaScriptStrategyGeneratorUseIndicators", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample ATM strategy.
	/// </summary>
	public static string NinjaScriptStrategyNameSampleATMStrategy => ResourceManager.GetString("NinjaScriptStrategyNameSampleATMStrategy", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample custom performance.
	/// </summary>
	public static string NinjaScriptStrategyNameSampleCustomPerformance => ResourceManager.GetString("NinjaScriptStrategyNameSampleCustomPerformance", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample framework.
	/// </summary>
	public static string NinjaScriptStrategyNameSampleFramework => ResourceManager.GetString("NinjaScriptStrategyNameSampleFramework", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample MA crossover.
	/// </summary>
	public static string NinjaScriptStrategyNameSampleMACrossOver => ResourceManager.GetString("NinjaScriptStrategyNameSampleMACrossOver", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample multi-instrument.
	/// </summary>
	public static string NinjaScriptStrategyNameSampleMultiInstrument => ResourceManager.GetString("NinjaScriptStrategyNameSampleMultiInstrument", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample multi-timeframe.
	/// </summary>
	public static string NinjaScriptStrategyNameSampleMultiTimeFrame => ResourceManager.GetString("NinjaScriptStrategyNameSampleMultiTimeFrame", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Strategy parameters.
	/// </summary>
	public static string NinjaScriptStrategyParameters => ResourceManager.GetString("NinjaScriptStrategyParameters", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to APQ.
	/// </summary>
	public static string NinjaScriptSuperDomColumnApq => ResourceManager.GetString("NinjaScriptSuperDomColumnApq", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Error on loading bars series for '{0}/{1}': {2}.
	/// </summary>
	public static string NinjaScriptSuperDomColumnBaseInitializeBarsPoolError => ResourceManager.GetString("NinjaScriptSuperDomColumnBaseInitializeBarsPoolError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Approximate Position In Queue (APQ) indicator gives you a conservative estimation of the current position in the queue for orders you have placed..
	/// </summary>
	public static string NinjaScriptSuperDomColumnDescriptionApq => ResourceManager.GetString("NinjaScriptSuperDomColumnDescriptionApq", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Notes column provides text entry at price points directly in the SuperDOM and can be used to add notes per price level..
	/// </summary>
	public static string NinjaScriptSuperDomColumnDescriptionNotes => ResourceManager.GetString("NinjaScriptSuperDomColumnDescriptionNotes", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Profit and Loss (PnL) column will display the potential profit and loss at each price point once your are in a trade..
	/// </summary>
	public static string NinjaScriptSuperDomColumnDescriptionPnl => ResourceManager.GetString("NinjaScriptSuperDomColumnDescriptionPnl", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Volume column will use historical tick data to display the number of contracts traded at each price level. You can optionally color the bars based on if trades occurred on the ask or bid..
	/// </summary>
	public static string NinjaScriptSuperDomColumnDescriptionVolume => ResourceManager.GetString("NinjaScriptSuperDomColumnDescriptionVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Notes.
	/// </summary>
	public static string NinjaScriptSuperDomColumnNotes => ResourceManager.GetString("NinjaScriptSuperDomColumnNotes", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to PnL.
	/// </summary>
	public static string NinjaScriptSuperDomColumnProfitAndLoss => ResourceManager.GetString("NinjaScriptSuperDomColumnProfitAndLoss", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume.
	/// </summary>
	public static string NinjaScriptSuperDomColumnVolume => ResourceManager.GetString("NinjaScriptSuperDomColumnVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text Position.
	/// </summary>
	public static string NinjaScriptTextPosition => ResourceManager.GetString("NinjaScriptTextPosition", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text Position.
	/// </summary>
	public static string NinjaScriptTextPosition_ => ResourceManager.GetString("NinjaScriptTextPosition ", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Error while loading Drawing tool {0} : {1}.
	/// </summary>
	public static string NinjaScriptTileError => ResourceManager.GetString("NinjaScriptTileError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Y pixel offset.
	/// </summary>
	public static string NinjaScriptYOffset => ResourceManager.GetString("NinjaScriptYOffset", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Number of COT plots.
	/// </summary>
	public static string NumberOfCotPlots => ResourceManager.GetString("NumberOfCotPlots", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Number of trend lines.
	/// </summary>
	public static string NumberOfTrendLines => ResourceManager.GetString("NumberOfTrendLines", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Number of standard deviations.
	/// </summary>
	public static string NumStdDev => ResourceManager.GetString("NumStdDev", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Offset multiplier.
	/// </summary>
	public static string OffsetMultiplier => ResourceManager.GetString("OffsetMultiplier", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Old trends opacity.
	/// </summary>
	public static string OldTrendsOpacity => ResourceManager.GetString("OldTrendsOpacity", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Opacity.
	/// </summary>
	public static string Opacity => ResourceManager.GetString("Opacity", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Arrow.
	/// </summary>
	public static string PathCapMode_Arrow => ResourceManager.GetString("PathCapMode_Arrow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line.
	/// </summary>
	public static string PathCapMode_Line => ResourceManager.GetString("PathCapMode_Line", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Arrow.
	/// </summary>
	public static string PathToolCapMode_Arrow => ResourceManager.GetString("PathToolCapMode_Arrow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Line.
	/// </summary>
	public static string PathToolCapMode_Line => ResourceManager.GetString("PathToolCapMode_Line", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample cum. profit performance metric.
	/// </summary>
	public static string PerformanceMetricSampleCumProfit => ResourceManager.GetString("PerformanceMetricSampleCumProfit", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Period.
	/// </summary>
	public static string Period => ResourceManager.GetString("Period", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Period D.
	/// </summary>
	public static string PeriodD => ResourceManager.GetString("PeriodD", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Period K.
	/// </summary>
	public static string PeriodK => ResourceManager.GetString("PeriodK", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Period Q.
	/// </summary>
	public static string PeriodQ => ResourceManager.GetString("PeriodQ", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Zero.
	/// </summary>
	public static string PFEZero => ResourceManager.GetString("PFEZero", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Intraday or Daily bars must be used for Pivots.
	/// </summary>
	public static string PiviotsDailyBarsError => ResourceManager.GetString("PiviotsDailyBarsError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Insufficient Daily data to calculate Pivots.
	/// </summary>
	public static string PiviotsDailyDataError => ResourceManager.GetString("PiviotsDailyDataError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Insufficient historical data to calculate pivots. Increase chart look back period (DaysToLoad, BarsToLoad, or Start Date).
	/// </summary>
	public static string PiviotsInsufficentDataError => ResourceManager.GetString("PiviotsInsufficentDataError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Period Type will need to be Daily with a Value of 1.
	/// </summary>
	public static string PiviotsPeriodTypeError => ResourceManager.GetString("PiviotsPeriodTypeError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Daily bars require the use of Weekly or Monthly Pivot range.
	/// </summary>
	public static string PiviotsWeeklyBarsError => ResourceManager.GetString("PiviotsWeeklyBarsError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Pivot range.
	/// </summary>
	public static string PivotRange => ResourceManager.GetString("PivotRange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Daily.
	/// </summary>
	public static string PivotRange_Daily => ResourceManager.GetString("PivotRange_Daily", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Monthly.
	/// </summary>
	public static string PivotRange_Monthly => ResourceManager.GetString("PivotRange_Monthly", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Weekly.
	/// </summary>
	public static string PivotRange_Weekly => ResourceManager.GetString("PivotRange_Weekly", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to PP.
	/// </summary>
	public static string PivotsPP => ResourceManager.GetString("PivotsPP", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to R1.
	/// </summary>
	public static string PivotsR1 => ResourceManager.GetString("PivotsR1", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to R2.
	/// </summary>
	public static string PivotsR2 => ResourceManager.GetString("PivotsR2", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to R3.
	/// </summary>
	public static string PivotsR3 => ResourceManager.GetString("PivotsR3", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to R4.
	/// </summary>
	public static string PivotsR4 => ResourceManager.GetString("PivotsR4", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to S1.
	/// </summary>
	public static string PivotsS1 => ResourceManager.GetString("PivotsS1", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to S2.
	/// </summary>
	public static string PivotsS2 => ResourceManager.GetString("PivotsS2", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to S3.
	/// </summary>
	public static string PivotsS3 => ResourceManager.GetString("PivotsS3", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to S4.
	/// </summary>
	public static string PivotsS4 => ResourceManager.GetString("PivotsS4", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Plot current value only.
	/// </summary>
	public static string PlotCurrentValue => ResourceManager.GetString("PlotCurrentValue", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Positive color.
	/// </summary>
	public static string PositiveColor => ResourceManager.GetString("PositiveColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Smoothed.
	/// </summary>
	public static string PPOSmoothed => ResourceManager.GetString("PPOSmoothed", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ask line.
	/// </summary>
	public static string PriceLinePlotAsk => ResourceManager.GetString("PriceLinePlotAsk", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid line.
	/// </summary>
	public static string PriceLinePlotBid => ResourceManager.GetString("PriceLinePlotBid", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Last line.
	/// </summary>
	public static string PriceLinePlotLast => ResourceManager.GetString("PriceLinePlotLast", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Prior close.
	/// </summary>
	public static string PriorDayOHLCClose => ResourceManager.GetString("PriorDayOHLCClose", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Prior high.
	/// </summary>
	public static string PriorDayOHLCHigh => ResourceManager.GetString("PriorDayOHLCHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to PriorDayOHLC only works on intraday intervals.
	/// </summary>
	public static string PriorDayOHLCIntradayError => ResourceManager.GetString("PriorDayOHLCIntradayError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Prior low.
	/// </summary>
	public static string PriorDayOHLCLow => ResourceManager.GetString("PriorDayOHLCLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Prior open.
	/// </summary>
	public static string PriorDayOHLCOpen => ResourceManager.GetString("PriorDayOHLCOpen", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Visual.
	/// </summary>
	public static string PropertyCategoryVisual => ResourceManager.GetString("PropertyCategoryVisual", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ask.
	/// </summary>
	public static string PullingStackingDisplayType_Ask => ResourceManager.GetString("PullingStackingDisplayType_Ask", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid.
	/// </summary>
	public static string PullingStackingDisplayType_Bid => ResourceManager.GetString("PullingStackingDisplayType_Bid", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid &amp; Ask.
	/// </summary>
	public static string PullingStackingDisplayType_BidAsk => ResourceManager.GetString("PullingStackingDisplayType_BidAsk", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid/Ask change.
	/// </summary>
	public static string PullingStackingResetWhen_BidAskChange => ResourceManager.GetString("PullingStackingResetWhen_BidAskChange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to No longer receiving depth data.
	/// </summary>
	public static string PullingStackingResetWhen_NoMoreData => ResourceManager.GetString("PullingStackingResetWhen_NoMoreData", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Range Counter only works on Range bars.
	/// </summary>
	public static string RangeCounterBarError => ResourceManager.GetString("RangeCounterBarError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Range remaining = {0}.
	/// </summary>
	public static string RangeCounterRemaing => ResourceManager.GetString("RangeCounterRemaing", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Range count = {0}.
	/// </summary>
	public static string RangerCounterCount => ResourceManager.GetString("RangerCounterCount", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Range value.
	/// </summary>
	public static string RangeValue => ResourceManager.GetString("RangeValue", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ask.
	/// </summary>
	public static string RecentDisplayType_Ask => ResourceManager.GetString("RecentDisplayType_Ask", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid.
	/// </summary>
	public static string RecentDisplayType_Bid => ResourceManager.GetString("RecentDisplayType_Bid", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid &amp; Ask.
	/// </summary>
	public static string RecentDisplayType_BidAsk => ResourceManager.GetString("RecentDisplayType_BidAsk", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bid/Ask change.
	/// </summary>
	public static string RecentResetWhen_BidAskChange => ResourceManager.GetString("RecentResetWhen_BidAskChange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Price returns.
	/// </summary>
	public static string RecentResetWhen_PriceReturns => ResourceManager.GetString("RecentResetWhen_PriceReturns", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Horizontal.
	/// </summary>
	public static string RegionHighlightDirection_Horizontal => ResourceManager.GetString("RegionHighlightDirection_Horizontal", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Vertical.
	/// </summary>
	public static string RegionHighlightDirection_Vertical => ResourceManager.GetString("RegionHighlightDirection_Vertical", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Segment.
	/// </summary>
	public static string RegressionChannelType_Segment => ResourceManager.GetString("RegressionChannelType_Segment", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Standard deviation distance.
	/// </summary>
	public static string RegressionChannelType_StandardDeviation => ResourceManager.GetString("RegressionChannelType_StandardDeviation", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Rate of change period.
	/// </summary>
	public static string ROCPeriod => ResourceManager.GetString("ROCPeriod", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Signal line.
	/// </summary>
	public static string RVISignalLine => ResourceManager.GetString("RVISignalLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample name description.
	/// </summary>
	public static string SampleAddOnDescription => ResourceManager.GetString("SampleAddOnDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Hi there!.
	/// </summary>
	public static string SampleAddOnHiThere => ResourceManager.GetString("SampleAddOnHiThere", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample AddOn name.
	/// </summary>
	public static string SampleAddOnName => ResourceManager.GetString("SampleAddOnName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sample Cumulative Profit.
	/// </summary>
	public static string SampleCumProfit => ResourceManager.GetString("SampleCumProfit", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Cumulative Profit as a sample of a custom performance metric.
	/// </summary>
	public static string SampleCumProfitDescription => ResourceManager.GetString("SampleCumProfitDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Lower Right Corner.
	/// </summary>
	public static string SampleCustomPlotLowerRightCorner => ResourceManager.GetString("SampleCustomPlotLowerRightCorner", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Upper Left Corner.
	/// </summary>
	public static string SampleCustomPlotUpperLeftCorner => ResourceManager.GetString("SampleCustomPlotUpperLeftCorner", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Select pattern.
	/// </summary>
	public static string SelectPattern => ResourceManager.GetString("SelectPattern", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Choose a pattern to detect.
	/// </summary>
	public static string SelectPatternDescription => ResourceManager.GetString("SelectPatternDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Send alerts.
	/// </summary>
	public static string SendAlerts => ResourceManager.GetString("SendAlerts", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Set true to send alert messages to Alerts Window.
	/// </summary>
	public static string SendAlertsDescription => ResourceManager.GetString("SendAlertsDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to There was a problem calling OnShare with arguments: {0}.
	/// </summary>
	public static string ShareArgsException => ResourceManager.GetString("ShareArgsException", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Share provider returned a Bad Gateway error: '{0}'.
	/// </summary>
	public static string ShareBadGatewayError => ResourceManager.GetString("ShareBadGatewayError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Share provider returned a Bad Request error: '{0}'.
	/// </summary>
	public static string ShareBadRequestError => ResourceManager.GetString("ShareBadRequestError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to A WebException was thrown. Status: '{0}' Message: '{1}'.
	/// </summary>
	public static string ShareException => ResourceManager.GetString("ShareException", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The user could not be found.
	/// </summary>
	public static string ShareFacebookCouldNotRetrieveUser => ResourceManager.GetString("ShareFacebookCouldNotRetrieveUser", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Facebook could not verify the token for this user.
	/// </summary>
	public static string ShareFacebookCouldNotVerifyToken => ResourceManager.GetString("ShareFacebookCouldNotVerifyToken", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Failed to receive response from Facebook.
	/// </summary>
	public static string ShareFacebookNoResult => ResourceManager.GetString("ShareFacebookNoResult", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Needed Facebook permissions were denied by the user.
	/// </summary>
	public static string ShareFacebookPermissionDenied => ResourceManager.GetString("ShareFacebookPermissionDenied", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Could not verify Facebook permissions.
	/// </summary>
	public static string ShareFacebookScopesNotFound => ResourceManager.GetString("ShareFacebookScopesNotFound", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} - Post sent successfully.
	/// </summary>
	public static string ShareFacebookSentSuccessfully => ResourceManager.GetString("ShareFacebookSentSuccessfully", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Share provider returned a Forbidden message: '{0}'.
	/// </summary>
	public static string ShareForbidden => ResourceManager.GetString("ShareForbidden", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Share provider returned a Gateway Timeout error:'{0}'.
	/// </summary>
	public static string ShareGatewayTimeoutError => ResourceManager.GetString("ShareGatewayTimeoutError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The image at location '{0}' cannot be found..
	/// </summary>
	public static string ShareImageNoLongerExists => ResourceManager.GetString("ShareImageNoLongerExists", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Share provider returned an Internal Server Error: '{0}'.
	/// </summary>
	public static string ShareInternalServerError => ResourceManager.GetString("ShareInternalServerError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to There was an error sending a mail message: {0}.
	/// </summary>
	public static string ShareMailException => ResourceManager.GetString("ShareMailException", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to AOL.
	/// </summary>
	public static string ShareMailPreconfiguredAol => ResourceManager.GetString("ShareMailPreconfiguredAol", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Comcast.
	/// </summary>
	public static string ShareMailPreconfiguredComcast => ResourceManager.GetString("ShareMailPreconfiguredComcast", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Gmail.
	/// </summary>
	public static string ShareMailPreconfiguredGmail => ResourceManager.GetString("ShareMailPreconfiguredGmail", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to iCloud.
	/// </summary>
	public static string ShareMailPreconfiguredICloud => ResourceManager.GetString("ShareMailPreconfiguredICloud", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Manual.
	/// </summary>
	public static string ShareMailPreconfiguredManual => ResourceManager.GetString("ShareMailPreconfiguredManual", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Outlook.
	/// </summary>
	public static string ShareMailPreconfiguredOutlook => ResourceManager.GetString("ShareMailPreconfiguredOutlook", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Yahoo.
	/// </summary>
	public static string ShareMailPreconfiguredYahoo => ResourceManager.GetString("ShareMailPreconfiguredYahoo", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to There was an error sending your message: {0}.
	/// </summary>
	public static string ShareMailSendError => ResourceManager.GetString("ShareMailSendError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} - Message sent successfully.
	/// </summary>
	public static string ShareMailSentSuccessfully => ResourceManager.GetString("ShareMailSentSuccessfully", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Share provider returned a {0} error message: '{1}'.
	/// </summary>
	public static string ShareNonSuccessCode => ResourceManager.GetString("ShareNonSuccessCode", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Share provider returned a Not Authorized message: '{0}'.
	/// </summary>
	public static string ShareNotAuthorized => ResourceManager.GetString("ShareNotAuthorized", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Credentials.
	/// </summary>
	public static string ShareServiceParameters => ResourceManager.GetString("ShareServiceParameters", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Password.
	/// </summary>
	public static string ShareServicePassword => ResourceManager.GetString("ShareServicePassword", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to There was an exception in the Share service: '{0}'.
	/// </summary>
	public static string ShareServiceSignature => ResourceManager.GetString("ShareServiceSignature", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to User name.
	/// </summary>
	public static string ShareServiceUserName => ResourceManager.GetString("ShareServiceUserName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to StockTwits account could not be verified.
	/// </summary>
	public static string ShareStockTwitsNoAccount => ResourceManager.GetString("ShareStockTwitsNoAccount", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} - Message sent successfully.
	/// </summary>
	public static string ShareStockTwitsSentSuccessfully => ResourceManager.GetString("ShareStockTwitsSentSuccessfully", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Email.
	/// </summary>
	public static string ShareTextMessageEmail => ResourceManager.GetString("ShareTextMessageEmail", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to To configure the Text message via email Share Service you must first set up an Email Share Service..
	/// </summary>
	public static string ShareTextMessageEmailRequired => ResourceManager.GetString("ShareTextMessageEmailRequired", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to There was an error sending message via {0} email service: '{1}'.
	/// </summary>
	public static string ShareTextMessageErrorOnShare => ResourceManager.GetString("ShareTextMessageErrorOnShare", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to MMS address.
	/// </summary>
	public static string ShareTextMessageMmsAddress => ResourceManager.GetString("ShareTextMessageMmsAddress", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text message via email.
	/// </summary>
	public static string ShareTextMessageName => ResourceManager.GetString("ShareTextMessageName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Phone number.
	/// </summary>
	public static string ShareTextMessagePhoneNumber => ResourceManager.GetString("ShareTextMessagePhoneNumber", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Manual.
	/// </summary>
	public static string ShareTextMessagePreconfiguredManual => ResourceManager.GetString("ShareTextMessagePreconfiguredManual", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sprint.
	/// </summary>
	public static string ShareTextMessagePreconfiguredSprint => ResourceManager.GetString("ShareTextMessagePreconfiguredSprint", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to T-Mobile.
	/// </summary>
	public static string ShareTextMessagePreconfiguredTMobile => ResourceManager.GetString("ShareTextMessagePreconfiguredTMobile", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Verizon.
	/// </summary>
	public static string ShareTextMessagePreconfiguredVerizon => ResourceManager.GetString("ShareTextMessagePreconfiguredVerizon", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} - Text message sent.
	/// </summary>
	public static string ShareTextMessageSentSuccessfully => ResourceManager.GetString("ShareTextMessageSentSuccessfully", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to SMS address.
	/// </summary>
	public static string ShareTextMessageSmsAddress => ResourceManager.GetString("ShareTextMessageSmsAddress", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to The Share provider returned a TooManyRequests message: '{0}'.
	/// </summary>
	public static string ShareTooManyRequests => ResourceManager.GetString("ShareTooManyRequests", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} - Tweet sent successfully.
	/// </summary>
	public static string ShareTwitterSentSuccessfully => ResourceManager.GetString("ShareTwitterSentSuccessfully", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Show ask line.
	/// </summary>
	public static string ShowAskLine => ResourceManager.GetString("ShowAskLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Show bid line.
	/// </summary>
	public static string ShowBidLine => ResourceManager.GetString("ShowBidLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Show close.
	/// </summary>
	public static string ShowClose => ResourceManager.GetString("ShowClose", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Show high.
	/// </summary>
	public static string ShowHigh => ResourceManager.GetString("ShowHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Show last line.
	/// </summary>
	public static string ShowLastLine => ResourceManager.GetString("ShowLastLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Show low.
	/// </summary>
	public static string ShowLow => ResourceManager.GetString("ShowLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Show open.
	/// </summary>
	public static string ShowOpen => ResourceManager.GetString("ShowOpen", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Show pattern count.
	/// </summary>
	public static string ShowPatternCount => ResourceManager.GetString("ShowPatternCount", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Set true to display on chart the count of patterns found.
	/// </summary>
	public static string ShowPatternCountDescription => ResourceManager.GetString("ShowPatternCountDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Show percent.
	/// </summary>
	public static string ShowPercent => ResourceManager.GetString("ShowPercent", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Signal period.
	/// </summary>
	public static string SignalPeriod => ResourceManager.GetString("SignalPeriod", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Slow.
	/// </summary>
	public static string Slow => ResourceManager.GetString("Slow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Slow limit.
	/// </summary>
	public static string SlowLimit => ResourceManager.GetString("SlowLimit", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Slow period.
	/// </summary>
	public static string SlowPeriod => ResourceManager.GetString("SlowPeriod", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Small area color.
	/// </summary>
	public static string SmallAreaColor => ResourceManager.GetString("SmallAreaColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Smooth.
	/// </summary>
	public static string Smooth => ResourceManager.GetString("Smooth", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Smoothing.
	/// </summary>
	public static string Smoothing => ResourceManager.GetString("Smoothing", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to D.
	/// </summary>
	public static string StochasticsD => ResourceManager.GetString("StochasticsD", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to K.
	/// </summary>
	public static string StochasticsK => ResourceManager.GetString("StochasticsK", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Sentiment:.
	/// </summary>
	public static string StockTwitsSentiment => ResourceManager.GetString("StockTwitsSentiment", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Choose Bearish, Neutral, or Bullish for this message.
	/// </summary>
	public static string StockTwitsSentimentDescription => ResourceManager.GetString("StockTwitsSentimentDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to StockTwits.
	/// </summary>
	public static string StockTwitsServiceName => ResourceManager.GetString("StockTwitsServiceName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to  Sent by NinjaTrader.
	/// </summary>
	public static string StockTwitsSignature => ResourceManager.GetString("StockTwitsSignature", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Strength.
	/// </summary>
	public static string Strength => ResourceManager.GetString("Strength", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to SuperDOM column '{0}': Error on calling '{1}' method: {2}.
	/// </summary>
	public static string SuperDomColumnException => ResourceManager.GetString("SuperDomColumnException", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Swing high.
	/// </summary>
	public static string SwingHigh => ResourceManager.GetString("SwingHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Swing low.
	/// </summary>
	public static string SwingLow => ResourceManager.GetString("SwingLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.SwingHighBar: barsAgo must be greater/equal 0 but was {1}.
	/// </summary>
	public static string SwingSwingHighBarBarsAgoGreaterEqual => ResourceManager.GetString("SwingSwingHighBarBarsAgoGreaterEqual", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.SwingHighBar: barsAgo out of valid range 0 through {1}, was {2}..
	/// </summary>
	public static string SwingSwingHighBarBarsAgoOutOfRange => ResourceManager.GetString("SwingSwingHighBarBarsAgoOutOfRange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.SwingHighBar: instance must be greater/equal 1 but was {1}.
	/// </summary>
	public static string SwingSwingHighBarInstanceGreaterEqual => ResourceManager.GetString("SwingSwingHighBarInstanceGreaterEqual", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.SwingLowBar: barsAgo must be greater/equal 0 but was {1}.
	/// </summary>
	public static string SwingSwingLowBarBarsAgoGreaterEqual => ResourceManager.GetString("SwingSwingLowBarBarsAgoGreaterEqual", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.SwingLowBar: barsAgo out of valid range 0 through {1}, was {2}..
	/// </summary>
	public static string SwingSwingLowBarBarsAgoOutOfRange => ResourceManager.GetString("SwingSwingLowBarBarsAgoOutOfRange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.SwingLowBar: instance must be greater/equal 1 but was {1}.
	/// </summary>
	public static string SwingSwingLowBarInstanceGreaterEqual => ResourceManager.GetString("SwingSwingLowBarInstanceGreaterEqual", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to T count.
	/// </summary>
	public static string TCount => ResourceManager.GetString("TCount", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text color.
	/// </summary>
	public static string TextColor => ResourceManager.GetString("TextColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Text font.
	/// </summary>
	public static string TextFont => ResourceManager.GetString("TextFont", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Select font, style, size to display on chart.
	/// </summary>
	public static string TextFontDescription => ResourceManager.GetString("TextFontDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bottom left.
	/// </summary>
	public static string TextPosition_BottomLeft => ResourceManager.GetString("TextPosition_BottomLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bottom right.
	/// </summary>
	public static string TextPosition_BottomRight => ResourceManager.GetString("TextPosition_BottomRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Center.
	/// </summary>
	public static string TextPosition_Center => ResourceManager.GetString("TextPosition_Center", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Top left.
	/// </summary>
	public static string TextPosition_TopLeft => ResourceManager.GetString("TextPosition_TopLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Top right.
	/// </summary>
	public static string TextPosition_TopRight => ResourceManager.GetString("TextPosition_TopRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bottom Left.
	/// </summary>
	public static string TextPositionFine_BottomLeft => ResourceManager.GetString("TextPositionFine_BottomLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bottom Middle.
	/// </summary>
	public static string TextPositionFine_BottomMiddle => ResourceManager.GetString("TextPositionFine_BottomMiddle", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Bottom Right.
	/// </summary>
	public static string TextPositionFine_BottomRight => ResourceManager.GetString("TextPositionFine_BottomRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Middle Left.
	/// </summary>
	public static string TextPositionFine_MiddleLeft => ResourceManager.GetString("TextPositionFine_MiddleLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Middle Right.
	/// </summary>
	public static string TextPositionFine_MiddleRight => ResourceManager.GetString("TextPositionFine_MiddleRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Top Left.
	/// </summary>
	public static string TextPositionFine_TopLeft => ResourceManager.GetString("TextPositionFine_TopLeft", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Top Middle.
	/// </summary>
	public static string TextPositionFine_TopMiddle => ResourceManager.GetString("TextPositionFine_TopMiddle", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Top Right.
	/// </summary>
	public static string TextPositionFine_TopRight => ResourceManager.GetString("TextPositionFine_TopRight", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Tick Counter only works on bars built with a set number of ticks.
	/// </summary>
	public static string TickCounterBarError => ResourceManager.GetString("TickCounterBarError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Tick Count = .
	/// </summary>
	public static string TickCounterTickCount => ResourceManager.GetString("TickCounterTickCount", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Ticks Remaining = .
	/// </summary>
	public static string TickCounterTicksRemaining => ResourceManager.GetString("TickCounterTicksRemaining", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Current trend line.
	/// </summary>
	public static string TrendLinesCurrentTrendLine => ResourceManager.GetString("TrendLinesCurrentTrendLine", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to TrendLines indicator is not visible with Strategy Analyzer.
	/// </summary>
	public static string TrendLinesNotVisible => ResourceManager.GetString("TrendLinesNotVisible", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0} broken.
	/// </summary>
	public static string TrendLinesTrendLineBroken => ResourceManager.GetString("TrendLinesTrendLineBroken", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Trend line high.
	/// </summary>
	public static string TrendLinesTrendLineHigh => ResourceManager.GetString("TrendLinesTrendLineHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Trend line low.
	/// </summary>
	public static string TrendLinesTrendLineLow => ResourceManager.GetString("TrendLinesTrendLineLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Trend strength.
	/// </summary>
	public static string TrendStrength => ResourceManager.GetString("TrendStrength", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Number of bars required to define a trend when a pattern requires a prevailing trend. \nA value of zero will disable trend requirement..
	/// </summary>
	public static string TrendStrengthDescription => ResourceManager.GetString("TrendStrengthDescription", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Signal.
	/// </summary>
	public static string TRIXSignal => ResourceManager.GetString("TRIXSignal", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Account Successfully Authorized.
	/// </summary>
	public static string TwitterAuthHeader => ResourceManager.GetString("TwitterAuthHeader", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to You have successfully authorized {0} to access your Twitter account..
	/// </summary>
	public static string TwitterAuthText1 => ResourceManager.GetString("TwitterAuthText1", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to You may close this window and return to {0}..
	/// </summary>
	public static string TwitterAuthText2 => ResourceManager.GetString("TwitterAuthText2", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Twitter.
	/// </summary>
	public static string TwitterServiceName => ResourceManager.GetString("TwitterServiceName", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to  #NinjaTrader.
	/// </summary>
	public static string TwitterSignature => ResourceManager.GetString("TwitterSignature", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Unit.
	/// </summary>
	public static string Unit => ResourceManager.GetString("Unit", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Up bar color.
	/// </summary>
	public static string UpBarColor => ResourceManager.GetString("UpBarColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Use high low.
	/// </summary>
	public static string UseHighLow => ResourceManager.GetString("UseHighLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to User defined close.
	/// </summary>
	public static string UserDefinedClose => ResourceManager.GetString("UserDefinedClose", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to User defined high.
	/// </summary>
	public static string UserDefinedHigh => ResourceManager.GetString("UserDefinedHigh", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to User defined low.
	/// </summary>
	public static string UserDefinedLow => ResourceManager.GetString("UserDefinedLow", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to V factor.
	/// </summary>
	public static string VFactor => ResourceManager.GetString("VFactor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volatility period.
	/// </summary>
	public static string VolatilityPeriod => ResourceManager.GetString("VolatilityPeriod", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume Counter only works on volume based intervals.
	/// </summary>
	public static string VolumeCounterBarError => ResourceManager.GetString("VolumeCounterBarError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume = .
	/// </summary>
	public static string VolumeCounterVolumeCount => ResourceManager.GetString("VolumeCounterVolumeCount", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume remaining = .
	/// </summary>
	public static string VolumeCounterVolumeRemaining => ResourceManager.GetString("VolumeCounterVolumeRemaining", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume divisor.
	/// </summary>
	public static string VolumeDivisor => ResourceManager.GetString("VolumeDivisor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Down volume.
	/// </summary>
	public static string VolumeDown => ResourceManager.GetString("VolumeDown", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume down color.
	/// </summary>
	public static string VolumeDownColor => ResourceManager.GetString("VolumeDownColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume neutral color.
	/// </summary>
	public static string VolumeNeutralColor => ResourceManager.GetString("VolumeNeutralColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Up volume.
	/// </summary>
	public static string VolumeUp => ResourceManager.GetString("VolumeUp", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume up color.
	/// </summary>
	public static string VolumeUpColor => ResourceManager.GetString("VolumeUpColor", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Volume.
	/// </summary>
	public static string VOLVolume => ResourceManager.GetString("VOLVolume", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Width.
	/// </summary>
	public static string Width => ResourceManager.GetString("Width", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to Williams %R.
	/// </summary>
	public static string WilliamsPercentR => ResourceManager.GetString("WilliamsPercentR", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to "ZigZag can't plot any values since the deviation value is too large. Please reduce it.".
	/// </summary>
	public static string ZigZagDeviationValueError => ResourceManager.GetString("ZigZagDeviationValueError", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.HighBar: barsAgo out of valid range 0 through {1}, was {2}.
	/// </summary>
	public static string ZigZagHighBarBarsAgoOutOfRange => ResourceManager.GetString("ZigZagHighBarBarsAgoOutOfRange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.HighBar: instance must be greater/equal 1 but was {1}.
	/// </summary>
	public static string ZigZagHighBarInstanceGreaterEqual => ResourceManager.GetString("ZigZagHighBarInstanceGreaterEqual", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.LowBar: barsAgo out of valid range 0 through {1}, was {2}.
	/// </summary>
	public static string ZigZagLowBarBarsAgoOutOfRange => ResourceManager.GetString("ZigZagLowBarBarsAgoOutOfRange", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.LowBar: instance must be greater/equal 1 but was {1}.
	/// </summary>
	public static string ZigZagLowBarInstanceGreaterEqual => ResourceManager.GetString("ZigZagLowBarInstanceGreaterEqual", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.HighBar: barsAgo must be greater/equal 0 but was {1}.
	/// </summary>
	public static string ZigZigHighBarBarsAgoGreaterEqual => ResourceManager.GetString("ZigZigHighBarBarsAgoGreaterEqual", resourceCulture);

	/// <summary>
	///   Looks up a localized string similar to {0}.LowBar: barsAgo must be greater/equal 0 but was {1}.
	/// </summary>
	public static string ZigZigLowBarBarsAgoGreaterEqual => ResourceManager.GetString("ZigZigLowBarBarsAgoGreaterEqual", resourceCulture);

	internal Resource()
	{
	}
}

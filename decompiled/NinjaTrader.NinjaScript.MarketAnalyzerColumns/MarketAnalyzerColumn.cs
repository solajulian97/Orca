using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using CustomBsEnumNamespace;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Indicators.Dhonn;
using NinjaTrader.NinjaScript.Indicators.OTM;
using NinjaTrader.NinjaScript.Indicators.PAX;
using NinjaTrader.NinjaScript.Indicators.T3000.MGI;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class MarketAnalyzerColumn : MarketAnalyzerColumnBase
{
	private Indicator indicator;

	[Browsable(false)]
	public bool IsDataSeriesRequired
	{
		get
		{
			return ((NinjaScriptBase)this).IsDataSeriesRequired;
		}
		set
		{
			((NinjaScriptBase)this).IsDataSeriesRequired = value;
			if (indicator != null)
			{
				((NinjaScriptBase)indicator).IsDataSeriesRequired = value;
			}
		}
	}

	public ADL ADL()
	{
		return indicator.ADL(((NinjaScriptBase)this).Input);
	}

	public ADL ADL(ISeries<double> input)
	{
		return indicator.ADL(input);
	}

	public ADX ADX(int period)
	{
		return indicator.ADX(((NinjaScriptBase)this).Input, period);
	}

	public ADX ADX(ISeries<double> input, int period)
	{
		return indicator.ADX(input, period);
	}

	public ADXR ADXR(int interval, int period)
	{
		return indicator.ADXR(((NinjaScriptBase)this).Input, interval, period);
	}

	public ADXR ADXR(ISeries<double> input, int interval, int period)
	{
		return indicator.ADXR(input, interval, period);
	}

	public APZ APZ(double bandPct, int period)
	{
		return indicator.APZ(((NinjaScriptBase)this).Input, bandPct, period);
	}

	public APZ APZ(ISeries<double> input, double bandPct, int period)
	{
		return indicator.APZ(input, bandPct, period);
	}

	public Aroon Aroon(int period)
	{
		return indicator.Aroon(((NinjaScriptBase)this).Input, period);
	}

	public Aroon Aroon(ISeries<double> input, int period)
	{
		return indicator.Aroon(input, period);
	}

	public AroonOscillator AroonOscillator(int period)
	{
		return indicator.AroonOscillator(((NinjaScriptBase)this).Input, period);
	}

	public AroonOscillator AroonOscillator(ISeries<double> input, int period)
	{
		return indicator.AroonOscillator(input, period);
	}

	public ATR ATR(int period)
	{
		return indicator.ATR(((NinjaScriptBase)this).Input, period);
	}

	public ATR ATR(ISeries<double> input, int period)
	{
		return indicator.ATR(input, period);
	}

	public BarTimer BarTimer()
	{
		return indicator.BarTimer(((NinjaScriptBase)this).Input);
	}

	public BarTimer BarTimer(TextPositionFine textPositionFine)
	{
		return indicator.BarTimer(((NinjaScriptBase)this).Input, textPositionFine);
	}

	public BarTimer BarTimer(ISeries<double> input)
	{
		return indicator.BarTimer(input);
	}

	public BarTimer BarTimer(ISeries<double> input, TextPositionFine textPositionFine)
	{
		return indicator.BarTimer(input, textPositionFine);
	}

	public BlockVolume BlockVolume(double blockSize, CountType countType)
	{
		return indicator.BlockVolume(((NinjaScriptBase)this).Input, blockSize, countType);
	}

	public BlockVolume BlockVolume(ISeries<double> input, double blockSize, CountType countType)
	{
		return indicator.BlockVolume(input, blockSize, countType);
	}

	public Bollinger Bollinger(double numStdDev, int period)
	{
		return indicator.Bollinger(((NinjaScriptBase)this).Input, numStdDev, period);
	}

	public Bollinger Bollinger(ISeries<double> input, double numStdDev, int period)
	{
		return indicator.Bollinger(input, numStdDev, period);
	}

	public BOP BOP(int smooth)
	{
		return indicator.BOP(((NinjaScriptBase)this).Input, smooth);
	}

	public BOP BOP(ISeries<double> input, int smooth)
	{
		return indicator.BOP(input, smooth);
	}

	public BuySellPressure BuySellPressure()
	{
		return indicator.BuySellPressure(((NinjaScriptBase)this).Input);
	}

	public BuySellPressure BuySellPressure(ISeries<double> input)
	{
		return indicator.BuySellPressure(input);
	}

	public BuySellVolume BuySellVolume()
	{
		return indicator.BuySellVolume(((NinjaScriptBase)this).Input);
	}

	public BuySellVolume BuySellVolume(ISeries<double> input)
	{
		return indicator.BuySellVolume(input);
	}

	public CamarillaPivots CamarillaPivots(PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		return indicator.CamarillaPivots(((NinjaScriptBase)this).Input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
	}

	public CamarillaPivots CamarillaPivots(ISeries<double> input, PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		return indicator.CamarillaPivots(input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
	}

	public CandlestickPattern CandlestickPattern(ChartPattern pattern, int trendStrength)
	{
		return indicator.CandlestickPattern(((NinjaScriptBase)this).Input, pattern, trendStrength);
	}

	public CandlestickPattern CandlestickPattern(ISeries<double> input, ChartPattern pattern, int trendStrength)
	{
		return indicator.CandlestickPattern(input, pattern, trendStrength);
	}

	public CCI CCI(int period)
	{
		return indicator.CCI(((NinjaScriptBase)this).Input, period);
	}

	public CCI CCI(ISeries<double> input, int period)
	{
		return indicator.CCI(input, period);
	}

	public ChaikinMoneyFlow ChaikinMoneyFlow(int period)
	{
		return indicator.ChaikinMoneyFlow(((NinjaScriptBase)this).Input, period);
	}

	public ChaikinMoneyFlow ChaikinMoneyFlow(ISeries<double> input, int period)
	{
		return indicator.ChaikinMoneyFlow(input, period);
	}

	public ChaikinOscillator ChaikinOscillator(int fast, int slow)
	{
		return indicator.ChaikinOscillator(((NinjaScriptBase)this).Input, fast, slow);
	}

	public ChaikinOscillator ChaikinOscillator(ISeries<double> input, int fast, int slow)
	{
		return indicator.ChaikinOscillator(input, fast, slow);
	}

	public ChaikinVolatility ChaikinVolatility(int mAPeriod, int rOCPeriod)
	{
		return indicator.ChaikinVolatility(((NinjaScriptBase)this).Input, mAPeriod, rOCPeriod);
	}

	public ChaikinVolatility ChaikinVolatility(ISeries<double> input, int mAPeriod, int rOCPeriod)
	{
		return indicator.ChaikinVolatility(input, mAPeriod, rOCPeriod);
	}

	public ChoppinessIndex ChoppinessIndex(int period)
	{
		return indicator.ChoppinessIndex(((NinjaScriptBase)this).Input, period);
	}

	public ChoppinessIndex ChoppinessIndex(ISeries<double> input, int period)
	{
		return indicator.ChoppinessIndex(input, period);
	}

	public CMO CMO(int period)
	{
		return indicator.CMO(((NinjaScriptBase)this).Input, period);
	}

	public CMO CMO(ISeries<double> input, int period)
	{
		return indicator.CMO(input, period);
	}

	public ConstantLines ConstantLines(double line1Value, double line2Value, double line3Value, double line4Value)
	{
		return indicator.ConstantLines(((NinjaScriptBase)this).Input, line1Value, line2Value, line3Value, line4Value);
	}

	public ConstantLines ConstantLines(ISeries<double> input, double line1Value, double line2Value, double line3Value, double line4Value)
	{
		return indicator.ConstantLines(input, line1Value, line2Value, line3Value, line4Value);
	}

	public Correlation Correlation(int period, string correlationSeries)
	{
		return indicator.Correlation(((NinjaScriptBase)this).Input, period, correlationSeries);
	}

	public Correlation Correlation(ISeries<double> input, int period, string correlationSeries)
	{
		return indicator.Correlation(input, period, correlationSeries);
	}

	public COT COT(int number)
	{
		return indicator.COT(((NinjaScriptBase)this).Input, number);
	}

	public COT COT(ISeries<double> input, int number)
	{
		return indicator.COT(input, number);
	}

	public CurrentDayOHL CurrentDayOHL()
	{
		return indicator.CurrentDayOHL(((NinjaScriptBase)this).Input);
	}

	public CurrentDayOHL CurrentDayOHL(ISeries<double> input)
	{
		return indicator.CurrentDayOHL(input);
	}

	public Darvas Darvas()
	{
		return indicator.Darvas(((NinjaScriptBase)this).Input);
	}

	public Darvas Darvas(ISeries<double> input)
	{
		return indicator.Darvas(input);
	}

	public DEMA DEMA(int period)
	{
		return indicator.DEMA(((NinjaScriptBase)this).Input, period);
	}

	public DEMA DEMA(ISeries<double> input, int period)
	{
		return indicator.DEMA(input, period);
	}

	public DisparityIndex DisparityIndex(int period)
	{
		return indicator.DisparityIndex(((NinjaScriptBase)this).Input, period);
	}

	public DisparityIndex DisparityIndex(ISeries<double> input, int period)
	{
		return indicator.DisparityIndex(input, period);
	}

	public DM DM(int period)
	{
		return indicator.DM(((NinjaScriptBase)this).Input, period);
	}

	public DM DM(ISeries<double> input, int period)
	{
		return indicator.DM(input, period);
	}

	public DMI DMI(int period)
	{
		return indicator.DMI(((NinjaScriptBase)this).Input, period);
	}

	public DMI DMI(ISeries<double> input, int period)
	{
		return indicator.DMI(input, period);
	}

	public DMIndex DMIndex(int smooth)
	{
		return indicator.DMIndex(((NinjaScriptBase)this).Input, smooth);
	}

	public DMIndex DMIndex(ISeries<double> input, int smooth)
	{
		return indicator.DMIndex(input, smooth);
	}

	public DonchianChannel DonchianChannel(int period)
	{
		return indicator.DonchianChannel(((NinjaScriptBase)this).Input, period);
	}

	public DonchianChannel DonchianChannel(ISeries<double> input, int period)
	{
		return indicator.DonchianChannel(input, period);
	}

	public DoubleStochastics DoubleStochastics(int period)
	{
		return indicator.DoubleStochastics(((NinjaScriptBase)this).Input, period);
	}

	public DoubleStochastics DoubleStochastics(ISeries<double> input, int period)
	{
		return indicator.DoubleStochastics(input, period);
	}

	public EaseOfMovement EaseOfMovement(int smoothing, int volumeDivisor)
	{
		return indicator.EaseOfMovement(((NinjaScriptBase)this).Input, smoothing, volumeDivisor);
	}

	public EaseOfMovement EaseOfMovement(ISeries<double> input, int smoothing, int volumeDivisor)
	{
		return indicator.EaseOfMovement(input, smoothing, volumeDivisor);
	}

	public EMA EMA(int period)
	{
		return indicator.EMA(((NinjaScriptBase)this).Input, period);
	}

	public EMA EMA(ISeries<double> input, int period)
	{
		return indicator.EMA(input, period);
	}

	public FibonacciPivots FibonacciPivots(PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		return indicator.FibonacciPivots(((NinjaScriptBase)this).Input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
	}

	public FibonacciPivots FibonacciPivots(ISeries<double> input, PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		return indicator.FibonacciPivots(input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
	}

	public FisherTransform FisherTransform(int period)
	{
		return indicator.FisherTransform(((NinjaScriptBase)this).Input, period);
	}

	public FisherTransform FisherTransform(ISeries<double> input, int period)
	{
		return indicator.FisherTransform(input, period);
	}

	public FOSC FOSC(int period)
	{
		return indicator.FOSC(((NinjaScriptBase)this).Input, period);
	}

	public FOSC FOSC(ISeries<double> input, int period)
	{
		return indicator.FOSC(input, period);
	}

	public FVG FVG()
	{
		return indicator.FVG(((NinjaScriptBase)this).Input);
	}

	public FVG FVG(ISeries<double> input)
	{
		return indicator.FVG(input);
	}

	public HMA HMA(int period)
	{
		return indicator.HMA(((NinjaScriptBase)this).Input, period);
	}

	public HMA HMA(ISeries<double> input, int period)
	{
		return indicator.HMA(input, period);
	}

	public IchimokuCloud IchimokuCloud(int conversionPeriod, int basePeriod, int leadingSpanBPeriod, int spanDisplacement, int laggingDisplacement)
	{
		return indicator.IchimokuCloud(((NinjaScriptBase)this).Input, conversionPeriod, basePeriod, leadingSpanBPeriod, spanDisplacement, laggingDisplacement);
	}

	public IchimokuCloud IchimokuCloud(ISeries<double> input, int conversionPeriod, int basePeriod, int leadingSpanBPeriod, int spanDisplacement, int laggingDisplacement)
	{
		return indicator.IchimokuCloud(input, conversionPeriod, basePeriod, leadingSpanBPeriod, spanDisplacement, laggingDisplacement);
	}

	public KAMA KAMA(int fast, int period, int slow)
	{
		return indicator.KAMA(((NinjaScriptBase)this).Input, fast, period, slow);
	}

	public KAMA KAMA(ISeries<double> input, int fast, int period, int slow)
	{
		return indicator.KAMA(input, fast, period, slow);
	}

	public KeltnerChannel KeltnerChannel(double offsetMultiplier, int period)
	{
		return indicator.KeltnerChannel(((NinjaScriptBase)this).Input, offsetMultiplier, period);
	}

	public KeltnerChannel KeltnerChannel(ISeries<double> input, double offsetMultiplier, int period)
	{
		return indicator.KeltnerChannel(input, offsetMultiplier, period);
	}

	public KeyReversalDown KeyReversalDown(int period)
	{
		return indicator.KeyReversalDown(((NinjaScriptBase)this).Input, period);
	}

	public KeyReversalDown KeyReversalDown(ISeries<double> input, int period)
	{
		return indicator.KeyReversalDown(input, period);
	}

	public KeyReversalUp KeyReversalUp(int period)
	{
		return indicator.KeyReversalUp(((NinjaScriptBase)this).Input, period);
	}

	public KeyReversalUp KeyReversalUp(ISeries<double> input, int period)
	{
		return indicator.KeyReversalUp(input, period);
	}

	public LinReg LinReg(int period)
	{
		return indicator.LinReg(((NinjaScriptBase)this).Input, period);
	}

	public LinReg LinReg(ISeries<double> input, int period)
	{
		return indicator.LinReg(input, period);
	}

	public LinRegIntercept LinRegIntercept(int period)
	{
		return indicator.LinRegIntercept(((NinjaScriptBase)this).Input, period);
	}

	public LinRegIntercept LinRegIntercept(ISeries<double> input, int period)
	{
		return indicator.LinRegIntercept(input, period);
	}

	public LinRegSlope LinRegSlope(int period)
	{
		return indicator.LinRegSlope(((NinjaScriptBase)this).Input, period);
	}

	public LinRegSlope LinRegSlope(ISeries<double> input, int period)
	{
		return indicator.LinRegSlope(input, period);
	}

	public MACD MACD(int fast, int slow, int smooth)
	{
		return indicator.MACD(((NinjaScriptBase)this).Input, fast, slow, smooth);
	}

	public MACD MACD(ISeries<double> input, int fast, int slow, int smooth)
	{
		return indicator.MACD(input, fast, slow, smooth);
	}

	public MAEnvelopes MAEnvelopes(double envelopePercentage, int mAType, int period)
	{
		return indicator.MAEnvelopes(((NinjaScriptBase)this).Input, envelopePercentage, mAType, period);
	}

	public MAEnvelopes MAEnvelopes(ISeries<double> input, double envelopePercentage, int mAType, int period)
	{
		return indicator.MAEnvelopes(input, envelopePercentage, mAType, period);
	}

	public MAMA MAMA(double fastLimit, double slowLimit)
	{
		return indicator.MAMA(((NinjaScriptBase)this).Input, fastLimit, slowLimit);
	}

	public MAMA MAMA(ISeries<double> input, double fastLimit, double slowLimit)
	{
		return indicator.MAMA(input, fastLimit, slowLimit);
	}

	public MAX MAX(int period)
	{
		return indicator.MAX(((NinjaScriptBase)this).Input, period);
	}

	public MAX MAX(ISeries<double> input, int period)
	{
		return indicator.MAX(input, period);
	}

	public McClellanOscillator McClellanOscillator(int fastPeriod, int slowPeriod)
	{
		return indicator.McClellanOscillator(((NinjaScriptBase)this).Input, fastPeriod, slowPeriod);
	}

	public McClellanOscillator McClellanOscillator(ISeries<double> input, int fastPeriod, int slowPeriod)
	{
		return indicator.McClellanOscillator(input, fastPeriod, slowPeriod);
	}

	public MFI MFI(int period)
	{
		return indicator.MFI(((NinjaScriptBase)this).Input, period);
	}

	public MFI MFI(ISeries<double> input, int period)
	{
		return indicator.MFI(input, period);
	}

	public MIN MIN(int period)
	{
		return indicator.MIN(((NinjaScriptBase)this).Input, period);
	}

	public MIN MIN(ISeries<double> input, int period)
	{
		return indicator.MIN(input, period);
	}

	public Momentum Momentum(int period)
	{
		return indicator.Momentum(((NinjaScriptBase)this).Input, period);
	}

	public Momentum Momentum(ISeries<double> input, int period)
	{
		return indicator.Momentum(input, period);
	}

	public MoneyFlowOscillator MoneyFlowOscillator(int period)
	{
		return indicator.MoneyFlowOscillator(((NinjaScriptBase)this).Input, period);
	}

	public MoneyFlowOscillator MoneyFlowOscillator(ISeries<double> input, int period)
	{
		return indicator.MoneyFlowOscillator(input, period);
	}

	public MovingAverageRibbon MovingAverageRibbon(RibbonMAType movingAverage, int basePeriod, int incrementalPeriod)
	{
		return indicator.MovingAverageRibbon(((NinjaScriptBase)this).Input, movingAverage, basePeriod, incrementalPeriod);
	}

	public MovingAverageRibbon MovingAverageRibbon(ISeries<double> input, RibbonMAType movingAverage, int basePeriod, int incrementalPeriod)
	{
		return indicator.MovingAverageRibbon(input, movingAverage, basePeriod, incrementalPeriod);
	}

	public NBarsDown NBarsDown(int barCount, bool barDown, bool lowerHigh, bool lowerLow)
	{
		return indicator.NBarsDown(((NinjaScriptBase)this).Input, barCount, barDown, lowerHigh, lowerLow);
	}

	public NBarsDown NBarsDown(ISeries<double> input, int barCount, bool barDown, bool lowerHigh, bool lowerLow)
	{
		return indicator.NBarsDown(input, barCount, barDown, lowerHigh, lowerLow);
	}

	public NBarsUp NBarsUp(int barCount, bool barUp, bool higherHigh, bool higherLow)
	{
		return indicator.NBarsUp(((NinjaScriptBase)this).Input, barCount, barUp, higherHigh, higherLow);
	}

	public NBarsUp NBarsUp(ISeries<double> input, int barCount, bool barUp, bool higherHigh, bool higherLow)
	{
		return indicator.NBarsUp(input, barCount, barUp, higherHigh, higherLow);
	}

	public NetChangeDisplay NetChangeDisplay(PerformanceUnit unit, NetChangePosition location)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return indicator.NetChangeDisplay(((NinjaScriptBase)this).Input, unit, location);
	}

	public NetChangeDisplay NetChangeDisplay(ISeries<double> input, PerformanceUnit unit, NetChangePosition location)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return indicator.NetChangeDisplay(input, unit, location);
	}

	public OBV OBV()
	{
		return indicator.OBV(((NinjaScriptBase)this).Input);
	}

	public OBV OBV(ISeries<double> input)
	{
		return indicator.OBV(input);
	}

	public ParabolicSAR ParabolicSAR(double acceleration, double accelerationMax, double accelerationStep)
	{
		return indicator.ParabolicSAR(((NinjaScriptBase)this).Input, acceleration, accelerationMax, accelerationStep);
	}

	public ParabolicSAR ParabolicSAR(ISeries<double> input, double acceleration, double accelerationMax, double accelerationStep)
	{
		return indicator.ParabolicSAR(input, acceleration, accelerationMax, accelerationStep);
	}

	public PFE PFE(int period, int smooth)
	{
		return indicator.PFE(((NinjaScriptBase)this).Input, period, smooth);
	}

	public PFE PFE(ISeries<double> input, int period, int smooth)
	{
		return indicator.PFE(input, period, smooth);
	}

	public Pivots Pivots(PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		return indicator.Pivots(((NinjaScriptBase)this).Input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
	}

	public Pivots Pivots(ISeries<double> input, PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		return indicator.Pivots(input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
	}

	public PPO PPO(int fast, int slow, int smooth)
	{
		return indicator.PPO(((NinjaScriptBase)this).Input, fast, slow, smooth);
	}

	public PPO PPO(ISeries<double> input, int fast, int slow, int smooth)
	{
		return indicator.PPO(input, fast, slow, smooth);
	}

	public PriceLine PriceLine(bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
	{
		return indicator.PriceLine(((NinjaScriptBase)this).Input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
	}

	public PriceLine PriceLine(ISeries<double> input, bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
	{
		return indicator.PriceLine(input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
	}

	public PriceOscillator PriceOscillator(int fast, int slow, int smooth)
	{
		return indicator.PriceOscillator(((NinjaScriptBase)this).Input, fast, slow, smooth);
	}

	public PriceOscillator PriceOscillator(ISeries<double> input, int fast, int slow, int smooth)
	{
		return indicator.PriceOscillator(input, fast, slow, smooth);
	}

	public PriorDayOHLC PriorDayOHLC()
	{
		return indicator.PriorDayOHLC(((NinjaScriptBase)this).Input);
	}

	public PriorDayOHLC PriorDayOHLC(ISeries<double> input)
	{
		return indicator.PriorDayOHLC(input);
	}

	public PsychologicalLine PsychologicalLine(int period)
	{
		return indicator.PsychologicalLine(((NinjaScriptBase)this).Input, period);
	}

	public PsychologicalLine PsychologicalLine(ISeries<double> input, int period)
	{
		return indicator.PsychologicalLine(input, period);
	}

	public Range Range()
	{
		return indicator.Range(((NinjaScriptBase)this).Input);
	}

	public Range Range(ISeries<double> input)
	{
		return indicator.Range(input);
	}

	public RangeCounter RangeCounter(bool countDown)
	{
		return indicator.RangeCounter(((NinjaScriptBase)this).Input, countDown);
	}

	public RangeCounter RangeCounter(bool countDown, TextPositionFine textPositionFine)
	{
		return indicator.RangeCounter(((NinjaScriptBase)this).Input, countDown, textPositionFine);
	}

	public RangeCounter RangeCounter(ISeries<double> input, bool countDown)
	{
		return indicator.RangeCounter(input, countDown);
	}

	public RangeCounter RangeCounter(ISeries<double> input, bool countDown, TextPositionFine textPositionFine)
	{
		return indicator.RangeCounter(input, countDown, textPositionFine);
	}

	public NinjaTrader.NinjaScript.Indicators.RegressionChannel RegressionChannel(int period, double width)
	{
		return indicator.RegressionChannel(((NinjaScriptBase)this).Input, period, width);
	}

	public NinjaTrader.NinjaScript.Indicators.RegressionChannel RegressionChannel(ISeries<double> input, int period, double width)
	{
		return indicator.RegressionChannel(input, period, width);
	}

	public RelativeVigorIndex RelativeVigorIndex(int period)
	{
		return indicator.RelativeVigorIndex(((NinjaScriptBase)this).Input, period);
	}

	public RelativeVigorIndex RelativeVigorIndex(ISeries<double> input, int period)
	{
		return indicator.RelativeVigorIndex(input, period);
	}

	public RIND RIND(int periodQ, int smooth)
	{
		return indicator.RIND(((NinjaScriptBase)this).Input, periodQ, smooth);
	}

	public RIND RIND(ISeries<double> input, int periodQ, int smooth)
	{
		return indicator.RIND(input, periodQ, smooth);
	}

	public ROC ROC(int period)
	{
		return indicator.ROC(((NinjaScriptBase)this).Input, period);
	}

	public ROC ROC(ISeries<double> input, int period)
	{
		return indicator.ROC(input, period);
	}

	public RSI RSI(int period, int smooth)
	{
		return indicator.RSI(((NinjaScriptBase)this).Input, period, smooth);
	}

	public RSI RSI(ISeries<double> input, int period, int smooth)
	{
		return indicator.RSI(input, period, smooth);
	}

	public RSquared RSquared(int period)
	{
		return indicator.RSquared(((NinjaScriptBase)this).Input, period);
	}

	public RSquared RSquared(ISeries<double> input, int period)
	{
		return indicator.RSquared(input, period);
	}

	public RSS RSS(int eMA1, int eMA2, int length)
	{
		return indicator.RSS(((NinjaScriptBase)this).Input, eMA1, eMA2, length);
	}

	public RSS RSS(ISeries<double> input, int eMA1, int eMA2, int length)
	{
		return indicator.RSS(input, eMA1, eMA2, length);
	}

	public RVI RVI(int period)
	{
		return indicator.RVI(((NinjaScriptBase)this).Input, period);
	}

	public RVI RVI(ISeries<double> input, int period)
	{
		return indicator.RVI(input, period);
	}

	public SampleCustomRender SampleCustomRender()
	{
		return indicator.SampleCustomRender(((NinjaScriptBase)this).Input);
	}

	public SampleCustomRender SampleCustomRender(ISeries<double> input)
	{
		return indicator.SampleCustomRender(input);
	}

	public SMA SMA(int period)
	{
		return indicator.SMA(((NinjaScriptBase)this).Input, period);
	}

	public SMA SMA(ISeries<double> input, int period)
	{
		return indicator.SMA(input, period);
	}

	public StdDev StdDev(int period)
	{
		return indicator.StdDev(((NinjaScriptBase)this).Input, period);
	}

	public StdDev StdDev(ISeries<double> input, int period)
	{
		return indicator.StdDev(input, period);
	}

	public StdError StdError(int period)
	{
		return indicator.StdError(((NinjaScriptBase)this).Input, period);
	}

	public StdError StdError(ISeries<double> input, int period)
	{
		return indicator.StdError(input, period);
	}

	public Stochastics Stochastics(int periodD, int periodK, int smooth)
	{
		return indicator.Stochastics(((NinjaScriptBase)this).Input, periodD, periodK, smooth);
	}

	public Stochastics Stochastics(ISeries<double> input, int periodD, int periodK, int smooth)
	{
		return indicator.Stochastics(input, periodD, periodK, smooth);
	}

	public StochasticsFast StochasticsFast(int periodD, int periodK)
	{
		return indicator.StochasticsFast(((NinjaScriptBase)this).Input, periodD, periodK);
	}

	public StochasticsFast StochasticsFast(ISeries<double> input, int periodD, int periodK)
	{
		return indicator.StochasticsFast(input, periodD, periodK);
	}

	public StochRSI StochRSI(int period)
	{
		return indicator.StochRSI(((NinjaScriptBase)this).Input, period);
	}

	public StochRSI StochRSI(ISeries<double> input, int period)
	{
		return indicator.StochRSI(input, period);
	}

	public SUM SUM(int period)
	{
		return indicator.SUM(((NinjaScriptBase)this).Input, period);
	}

	public SUM SUM(ISeries<double> input, int period)
	{
		return indicator.SUM(input, period);
	}

	public Swing Swing(int strength)
	{
		return indicator.Swing(((NinjaScriptBase)this).Input, strength);
	}

	public Swing Swing(ISeries<double> input, int strength)
	{
		return indicator.Swing(input, strength);
	}

	public T3 T3(int period, int tCount, double vFactor)
	{
		return indicator.T3(((NinjaScriptBase)this).Input, period, tCount, vFactor);
	}

	public T3 T3(ISeries<double> input, int period, int tCount, double vFactor)
	{
		return indicator.T3(input, period, tCount, vFactor);
	}

	public TEMA TEMA(int period)
	{
		return indicator.TEMA(((NinjaScriptBase)this).Input, period);
	}

	public TEMA TEMA(ISeries<double> input, int period)
	{
		return indicator.TEMA(input, period);
	}

	public TickCounter TickCounter(bool countDown, bool showPercent)
	{
		return indicator.TickCounter(((NinjaScriptBase)this).Input, countDown, showPercent, TextPositionFine.BottomRight);
	}

	public TickCounter TickCounter(bool countDown, bool showPercent, TextPositionFine textPositionFine)
	{
		return indicator.TickCounter(((NinjaScriptBase)this).Input, countDown, showPercent, textPositionFine);
	}

	public TickCounter TickCounter(ISeries<double> input, bool countDown, bool showPercent)
	{
		return indicator.TickCounter(input, countDown, showPercent, TextPositionFine.BottomRight);
	}

	public TickCounter TickCounter(ISeries<double> input, bool countDown, bool showPercent, TextPositionFine textPositionFine)
	{
		return indicator.TickCounter(input, countDown, showPercent, textPositionFine);
	}

	public TMA TMA(int period)
	{
		return indicator.TMA(((NinjaScriptBase)this).Input, period);
	}

	public TMA TMA(ISeries<double> input, int period)
	{
		return indicator.TMA(input, period);
	}

	public TrendLines TrendLines(int strength, int numberOfTrendLines, int oldTrendsOpacity, bool alertOnBreak)
	{
		return indicator.TrendLines(((NinjaScriptBase)this).Input, strength, numberOfTrendLines, oldTrendsOpacity, alertOnBreak);
	}

	public TrendLines TrendLines(ISeries<double> input, int strength, int numberOfTrendLines, int oldTrendsOpacity, bool alertOnBreak)
	{
		return indicator.TrendLines(input, strength, numberOfTrendLines, oldTrendsOpacity, alertOnBreak);
	}

	public TRIX TRIX(int period, int signalPeriod)
	{
		return indicator.TRIX(((NinjaScriptBase)this).Input, period, signalPeriod);
	}

	public TRIX TRIX(ISeries<double> input, int period, int signalPeriod)
	{
		return indicator.TRIX(input, period, signalPeriod);
	}

	public TSF TSF(int forecast, int period)
	{
		return indicator.TSF(((NinjaScriptBase)this).Input, forecast, period);
	}

	public TSF TSF(ISeries<double> input, int forecast, int period)
	{
		return indicator.TSF(input, forecast, period);
	}

	public TSI TSI(int fast, int slow)
	{
		return indicator.TSI(((NinjaScriptBase)this).Input, fast, slow);
	}

	public TSI TSI(ISeries<double> input, int fast, int slow)
	{
		return indicator.TSI(input, fast, slow);
	}

	public UltimateOscillator UltimateOscillator(int fast, int intermediate, int slow)
	{
		return indicator.UltimateOscillator(((NinjaScriptBase)this).Input, fast, intermediate, slow);
	}

	public UltimateOscillator UltimateOscillator(ISeries<double> input, int fast, int intermediate, int slow)
	{
		return indicator.UltimateOscillator(input, fast, intermediate, slow);
	}

	public VMA VMA(int period, int volatilityPeriod)
	{
		return indicator.VMA(((NinjaScriptBase)this).Input, period, volatilityPeriod);
	}

	public VMA VMA(ISeries<double> input, int period, int volatilityPeriod)
	{
		return indicator.VMA(input, period, volatilityPeriod);
	}

	public VOL VOL()
	{
		return indicator.VOL(((NinjaScriptBase)this).Input);
	}

	public VOL VOL(ISeries<double> input)
	{
		return indicator.VOL(input);
	}

	public VOLMA VOLMA(int period)
	{
		return indicator.VOLMA(((NinjaScriptBase)this).Input, period);
	}

	public VOLMA VOLMA(ISeries<double> input, int period)
	{
		return indicator.VOLMA(input, period);
	}

	public VolumeCounter VolumeCounter(bool countDown, bool showPercent)
	{
		return indicator.VolumeCounter(((NinjaScriptBase)this).Input, countDown, showPercent, TextPositionFine.BottomRight);
	}

	public VolumeCounter VolumeCounter(bool countDown, bool showPercent, TextPositionFine textPositionFine)
	{
		return indicator.VolumeCounter(((NinjaScriptBase)this).Input, countDown, showPercent, textPositionFine);
	}

	public VolumeCounter VolumeCounter(ISeries<double> input, bool countDown, bool showPercent)
	{
		return indicator.VolumeCounter(input, countDown, showPercent, TextPositionFine.BottomRight);
	}

	public VolumeCounter VolumeCounter(ISeries<double> input, bool countDown, bool showPercent, TextPositionFine textPositionFine)
	{
		return indicator.VolumeCounter(input, countDown, showPercent, textPositionFine);
	}

	public VolumeOscillator VolumeOscillator(int fast, int slow)
	{
		return indicator.VolumeOscillator(((NinjaScriptBase)this).Input, fast, slow);
	}

	public VolumeOscillator VolumeOscillator(ISeries<double> input, int fast, int slow)
	{
		return indicator.VolumeOscillator(input, fast, slow);
	}

	public VolumeProfile VolumeProfile()
	{
		return indicator.VolumeProfile(((NinjaScriptBase)this).Input);
	}

	public VolumeProfile VolumeProfile(ISeries<double> input)
	{
		return indicator.VolumeProfile(input);
	}

	public VolumeUpDown VolumeUpDown()
	{
		return indicator.VolumeUpDown(((NinjaScriptBase)this).Input);
	}

	public VolumeUpDown VolumeUpDown(ISeries<double> input)
	{
		return indicator.VolumeUpDown(input);
	}

	public VolumeZones VolumeZones()
	{
		return indicator.VolumeZones(((NinjaScriptBase)this).Input);
	}

	public VolumeZones VolumeZones(ISeries<double> input)
	{
		return indicator.VolumeZones(input);
	}

	public Vortex Vortex(int period)
	{
		return indicator.Vortex(((NinjaScriptBase)this).Input, period);
	}

	public Vortex Vortex(ISeries<double> input, int period)
	{
		return indicator.Vortex(input, period);
	}

	public VROC VROC(int period, int smooth)
	{
		return indicator.VROC(((NinjaScriptBase)this).Input, period, smooth);
	}

	public VROC VROC(ISeries<double> input, int period, int smooth)
	{
		return indicator.VROC(input, period, smooth);
	}

	public VWMA VWMA(int period)
	{
		return indicator.VWMA(((NinjaScriptBase)this).Input, period);
	}

	public VWMA VWMA(ISeries<double> input, int period)
	{
		return indicator.VWMA(input, period);
	}

	public WilliamsR WilliamsR(int period)
	{
		return indicator.WilliamsR(((NinjaScriptBase)this).Input, period);
	}

	public WilliamsR WilliamsR(ISeries<double> input, int period)
	{
		return indicator.WilliamsR(input, period);
	}

	public WMA WMA(int period)
	{
		return indicator.WMA(((NinjaScriptBase)this).Input, period);
	}

	public WMA WMA(ISeries<double> input, int period)
	{
		return indicator.WMA(input, period);
	}

	public ZigZag ZigZag(DeviationType deviationType, double deviationValue, bool useHighLow)
	{
		return indicator.ZigZag(((NinjaScriptBase)this).Input, deviationType, deviationValue, useHighLow);
	}

	public ZigZag ZigZag(ISeries<double> input, DeviationType deviationType, double deviationValue, bool useHighLow)
	{
		return indicator.ZigZag(input, deviationType, deviationValue, useHighLow);
	}

	public ZLEMA ZLEMA(int period)
	{
		return indicator.ZLEMA(((NinjaScriptBase)this).Input, period);
	}

	public ZLEMA ZLEMA(ISeries<double> input, int period)
	{
		return indicator.ZLEMA(input, period);
	}

	public AutoLegProfile AutoLegProfile(int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
	{
		return indicator.AutoLegProfile(((NinjaScriptBase)this).Input, reversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, tickCompression, deltaTickCompression, legsToDisplay, volumeProfileWidth, deltaProfileWidth, pastVolumeWidth, pastDeltaWidth, rightOffset, profileSeparation, profileBarSpacing, mergeOverlapPercent, mirrorPastProfiles, showVolume, showDelta, valueAreaPercent, showCurrentLegBox, showVWAP, showDeltaLabels, deltaLabelMinHeight, deltaLabelFontSize, showDeltaLabelBackground);
	}

	public AutoLegProfile AutoLegProfile(ISeries<double> input, int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
	{
		return indicator.AutoLegProfile(input, reversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, tickCompression, deltaTickCompression, legsToDisplay, volumeProfileWidth, deltaProfileWidth, pastVolumeWidth, pastDeltaWidth, rightOffset, profileSeparation, profileBarSpacing, mergeOverlapPercent, mirrorPastProfiles, showVolume, showDelta, valueAreaPercent, showCurrentLegBox, showVWAP, showDeltaLabels, deltaLabelMinHeight, deltaLabelFontSize, showDeltaLabelBackground);
	}

	public AutoLegProfileNT AutoLegProfileNT(int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
	{
		return indicator.AutoLegProfileNT(((NinjaScriptBase)this).Input, reversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, tickCompression, deltaTickCompression, legsToDisplay, volumeProfileWidth, deltaProfileWidth, pastVolumeWidth, pastDeltaWidth, rightOffset, profileSeparation, profileBarSpacing, mergeOverlapPercent, mirrorPastProfiles, showVolume, showDelta, valueAreaPercent, showCurrentLegBox, showVWAP, showDeltaLabels, deltaLabelMinHeight, deltaLabelFontSize, showDeltaLabelBackground);
	}

	public AutoLegProfileNT AutoLegProfileNT(ISeries<double> input, int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
	{
		return indicator.AutoLegProfileNT(input, reversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, tickCompression, deltaTickCompression, legsToDisplay, volumeProfileWidth, deltaProfileWidth, pastVolumeWidth, pastDeltaWidth, rightOffset, profileSeparation, profileBarSpacing, mergeOverlapPercent, mirrorPastProfiles, showVolume, showDelta, valueAreaPercent, showCurrentLegBox, showVWAP, showDeltaLabels, deltaLabelMinHeight, deltaLabelFontSize, showDeltaLabelBackground);
	}

	public AutoLegProfileNT2 AutoLegProfileNT2(int reversalTicks, int pastReversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, float volumeOpacity, float deltaOpacity)
	{
		return indicator.AutoLegProfileNT2(((NinjaScriptBase)this).Input, reversalTicks, pastReversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, volumeOpacity, deltaOpacity);
	}

	public AutoLegProfileNT2 AutoLegProfileNT2(ISeries<double> input, int reversalTicks, int pastReversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, float volumeOpacity, float deltaOpacity)
	{
		return indicator.AutoLegProfileNT2(input, reversalTicks, pastReversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, volumeOpacity, deltaOpacity);
	}

	public BarTimes BarTimes(TimeSelector timeUnits)
	{
		return indicator.BarTimes(((NinjaScriptBase)this).Input, timeUnits);
	}

	public BarTimes BarTimes(ISeries<double> input, TimeSelector timeUnits)
	{
		return indicator.BarTimes(input, timeUnits);
	}

	public FastCandleHighlight FastCandleHighlight(HighlightingMode mode, Brush highlightColor, int maxSeconds, int averagePeriod, int percentageThreshold)
	{
		return indicator.FastCandleHighlight(((NinjaScriptBase)this).Input, mode, highlightColor, maxSeconds, averagePeriod, percentageThreshold);
	}

	public FastCandleHighlight FastCandleHighlight(ISeries<double> input, HighlightingMode mode, Brush highlightColor, int maxSeconds, int averagePeriod, int percentageThreshold)
	{
		return indicator.FastCandleHighlight(input, mode, highlightColor, maxSeconds, averagePeriod, percentageThreshold);
	}

	public LegToLegDeltaProfile LegToLegDeltaProfile(ProfileModes profileMode, LegRotationMode rotationMode, double rotationPoints, int maxProfileWidthPx, long minAbsDeltaToShow, int rebuildLookbackBarsCap, bool autoDeltaText, int autoFontMin, int autoFontMax, int fontSize, int minRowHeightPx, Brush positiveBrush, Brush negativeBrush, Brush textBrush, float deltaOpacity, bool showSpine, int spineWidthPx, int rightMarginPx, double rightReservedPercent, int xOffsetPx, bool showVolumeProfile, VolumeProfileLayer volumeLayer, int volumeProfileWidthPx, float volumeOpacity, Brush volumeBrush, int sideBySideGapPx)
	{
		return indicator.LegToLegDeltaProfile(((NinjaScriptBase)this).Input, profileMode, rotationMode, rotationPoints, maxProfileWidthPx, minAbsDeltaToShow, rebuildLookbackBarsCap, autoDeltaText, autoFontMin, autoFontMax, fontSize, minRowHeightPx, positiveBrush, negativeBrush, textBrush, deltaOpacity, showSpine, spineWidthPx, rightMarginPx, rightReservedPercent, xOffsetPx, showVolumeProfile, volumeLayer, volumeProfileWidthPx, volumeOpacity, volumeBrush, sideBySideGapPx);
	}

	public LegToLegDeltaProfile LegToLegDeltaProfile(ISeries<double> input, ProfileModes profileMode, LegRotationMode rotationMode, double rotationPoints, int maxProfileWidthPx, long minAbsDeltaToShow, int rebuildLookbackBarsCap, bool autoDeltaText, int autoFontMin, int autoFontMax, int fontSize, int minRowHeightPx, Brush positiveBrush, Brush negativeBrush, Brush textBrush, float deltaOpacity, bool showSpine, int spineWidthPx, int rightMarginPx, double rightReservedPercent, int xOffsetPx, bool showVolumeProfile, VolumeProfileLayer volumeLayer, int volumeProfileWidthPx, float volumeOpacity, Brush volumeBrush, int sideBySideGapPx)
	{
		return indicator.LegToLegDeltaProfile(input, profileMode, rotationMode, rotationPoints, maxProfileWidthPx, minAbsDeltaToShow, rebuildLookbackBarsCap, autoDeltaText, autoFontMin, autoFontMax, fontSize, minRowHeightPx, positiveBrush, negativeBrush, textBrush, deltaOpacity, showSpine, spineWidthPx, rightMarginPx, rightReservedPercent, xOffsetPx, showVolumeProfile, volumeLayer, volumeProfileWidthPx, volumeOpacity, volumeBrush, sideBySideGapPx);
	}

	public OrcaAbsorptionCandles OrcaAbsorptionCandles(bool showHistoricalColor)
	{
		return indicator.OrcaAbsorptionCandles(((NinjaScriptBase)this).Input, showHistoricalColor);
	}

	public OrcaAbsorptionCandles OrcaAbsorptionCandles(ISeries<double> input, bool showHistoricalColor)
	{
		return indicator.OrcaAbsorptionCandles(input, showHistoricalColor);
	}

	public OrcaAnchoredVWAPs OrcaAnchoredVWAPs(int developingTicks, int standardTicks, int htfTicks, bool useAtrReversal, int atrPeriod, double devAtrMultiplier, double stdAtrMultiplier, double htfAtrMultiplier, bool showStdBands, bool showHtfBands, bool showAllBands, bool showStdDev1, bool showStdDev2, bool showStdDev3, double stdDevMultiplier1, double stdDevMultiplier2, double stdDevMultiplier3, int fillOpacityStdCore1, int fillOpacityStd12, int fillOpacityStd23, int fillOpacityHtfCore1, int fillOpacityHtf12, int fillOpacityHtf23)
	{
		return indicator.OrcaAnchoredVWAPs(((NinjaScriptBase)this).Input, developingTicks, standardTicks, htfTicks, useAtrReversal, atrPeriod, devAtrMultiplier, stdAtrMultiplier, htfAtrMultiplier, showStdBands, showHtfBands, showAllBands, showStdDev1, showStdDev2, showStdDev3, stdDevMultiplier1, stdDevMultiplier2, stdDevMultiplier3, fillOpacityStdCore1, fillOpacityStd12, fillOpacityStd23, fillOpacityHtfCore1, fillOpacityHtf12, fillOpacityHtf23);
	}

	public OrcaAnchoredVWAPs OrcaAnchoredVWAPs(ISeries<double> input, int developingTicks, int standardTicks, int htfTicks, bool useAtrReversal, int atrPeriod, double devAtrMultiplier, double stdAtrMultiplier, double htfAtrMultiplier, bool showStdBands, bool showHtfBands, bool showAllBands, bool showStdDev1, bool showStdDev2, bool showStdDev3, double stdDevMultiplier1, double stdDevMultiplier2, double stdDevMultiplier3, int fillOpacityStdCore1, int fillOpacityStd12, int fillOpacityStd23, int fillOpacityHtfCore1, int fillOpacityHtf12, int fillOpacityHtf23)
	{
		return indicator.OrcaAnchoredVWAPs(input, developingTicks, standardTicks, htfTicks, useAtrReversal, atrPeriod, devAtrMultiplier, stdAtrMultiplier, htfAtrMultiplier, showStdBands, showHtfBands, showAllBands, showStdDev1, showStdDev2, showStdDev3, stdDevMultiplier1, stdDevMultiplier2, stdDevMultiplier3, fillOpacityStdCore1, fillOpacityStd12, fillOpacityStd23, fillOpacityHtfCore1, fillOpacityHtf12, fillOpacityHtf23);
	}

	public OrcaCandleVolumeProfile OrcaCandleVolumeProfile(int tickCompression, int candleWidthPx, int profileWidthPx, bool dynamicProfileWidth, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showPOC, bool showDelta, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
	{
		return indicator.OrcaCandleVolumeProfile(((NinjaScriptBase)this).Input, tickCompression, candleWidthPx, profileWidthPx, dynamicProfileWidth, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showPOC, showDelta, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
	}

	public OrcaCandleVolumeProfile OrcaCandleVolumeProfile(ISeries<double> input, int tickCompression, int candleWidthPx, int profileWidthPx, bool dynamicProfileWidth, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showPOC, bool showDelta, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
	{
		return indicator.OrcaCandleVolumeProfile(input, tickCompression, candleWidthPx, profileWidthPx, dynamicProfileWidth, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showPOC, showDelta, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
	}

	public OrcaCumulativeDelta OrcaCumulativeDelta()
	{
		return indicator.OrcaCumulativeDelta(((NinjaScriptBase)this).Input);
	}

	public OrcaCumulativeDelta OrcaCumulativeDelta(ISeries<double> input)
	{
		return indicator.OrcaCumulativeDelta(input);
	}

	public OrcaExecutionLines OrcaExecutionLines(bool showExecutionLines, bool showLabels, bool showMarkers, bool showIndividualLines, bool showIndividualMarkers, bool showMAEMFE, bool enableShotClock, int shotClockSeconds, bool loadTodayHistory, bool loadSqliteHistory, double riskAmount, int lineWidth, int labelFontSize)
	{
		return indicator.OrcaExecutionLines(((NinjaScriptBase)this).Input, showExecutionLines, showLabels, showMarkers, showIndividualLines, showIndividualMarkers, showMAEMFE, enableShotClock, shotClockSeconds, loadTodayHistory, loadSqliteHistory, riskAmount, lineWidth, labelFontSize);
	}

	public OrcaExecutionLines OrcaExecutionLines(ISeries<double> input, bool showExecutionLines, bool showLabels, bool showMarkers, bool showIndividualLines, bool showIndividualMarkers, bool showMAEMFE, bool enableShotClock, int shotClockSeconds, bool loadTodayHistory, bool loadSqliteHistory, double riskAmount, int lineWidth, int labelFontSize)
	{
		return indicator.OrcaExecutionLines(input, showExecutionLines, showLabels, showMarkers, showIndividualLines, showIndividualMarkers, showMAEMFE, enableShotClock, shotClockSeconds, loadTodayHistory, loadSqliteHistory, riskAmount, lineWidth, labelFontSize);
	}

	public OrcaLegtoLegProfile OrcaLegtoLegProfile(int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
	{
		return indicator.OrcaLegtoLegProfile(((NinjaScriptBase)this).Input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
	}

	public OrcaLegtoLegProfile OrcaLegtoLegProfile(ISeries<double> input, int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
	{
		return indicator.OrcaLegtoLegProfile(input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
	}

	public OrcaRollingProfiles OrcaRollingProfiles(RollingProfilePeriod period, ProfileOperatingMode mode, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int deltaTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
	{
		return indicator.OrcaRollingProfiles(((NinjaScriptBase)this).Input, period, mode, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, deltaTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
	}

	public OrcaRollingProfiles OrcaRollingProfiles(ISeries<double> input, RollingProfilePeriod period, ProfileOperatingMode mode, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int deltaTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
	{
		return indicator.OrcaRollingProfiles(input, period, mode, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, deltaTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
	}

	public OrcaStepProfile OrcaStepProfile(StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
	{
		return indicator.OrcaStepProfile(((NinjaScriptBase)this).Input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, rTHOnly, rTHStart, rTHEnd, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
	}

	public OrcaStepProfile OrcaStepProfile(ISeries<double> input, StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
	{
		return indicator.OrcaStepProfile(input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, rTHOnly, rTHStart, rTHEnd, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
	}

	public OrcaTickDirectionIndex OrcaTickDirectionIndex()
	{
		return indicator.OrcaTickDirectionIndex(((NinjaScriptBase)this).Input);
	}

	public OrcaTickDirectionIndex OrcaTickDirectionIndex(ISeries<double> input)
	{
		return indicator.OrcaTickDirectionIndex(input);
	}

	public OrcaTimeStatistics OrcaTimeStatistics()
	{
		return indicator.OrcaTimeStatistics(((NinjaScriptBase)this).Input);
	}

	public OrcaTimeStatistics OrcaTimeStatistics(ISeries<double> input)
	{
		return indicator.OrcaTimeStatistics(input);
	}

	public OrcaTimeVWAPs OrcaTimeVWAPs(bool globexShowVWAP, TimeSpan globexStartTime, bool globexShowDev1, double globexDev1Mult, bool globexShowDev2, double globexDev2Mult, bool globexShowDev3, double globexDev3Mult, int globexFillOpacityCore, int globexFillOpacity12, int globexFillOpacity23, bool rthShowVWAP, TimeSpan rthStartTime, bool rthShowDev1, double rthDev1Mult, bool rthShowDev2, double rthDev2Mult, bool rthShowDev3, double rthDev3Mult, int rthFillOpacityCore, int rthFillOpacity12, int rthFillOpacity23, bool rollingShowVWAP, RollingVwapPeriod rollingPeriod, int minutesPerDay, bool rollingShowDev1, double rollingDev1Mult, bool rollingShowDev2, double rollingDev2Mult, bool rollingShowDev3, double rollingDev3Mult, int rollingFillOpacityCore, int rollingFillOpacity12, int rollingFillOpacity23, bool weeklyShowVWAP, TimeSpan weeklyStartTime, bool weeklyShowDev1, double weeklyDev1Mult, bool weeklyShowDev2, double weeklyDev2Mult, bool weeklyShowDev3, double weeklyDev3Mult, int weeklyFillOpacityCore, int weeklyFillOpacity12, int weeklyFillOpacity23)
	{
		return indicator.OrcaTimeVWAPs(((NinjaScriptBase)this).Input, globexShowVWAP, globexStartTime, globexShowDev1, globexDev1Mult, globexShowDev2, globexDev2Mult, globexShowDev3, globexDev3Mult, globexFillOpacityCore, globexFillOpacity12, globexFillOpacity23, rthShowVWAP, rthStartTime, rthShowDev1, rthDev1Mult, rthShowDev2, rthDev2Mult, rthShowDev3, rthDev3Mult, rthFillOpacityCore, rthFillOpacity12, rthFillOpacity23, rollingShowVWAP, rollingPeriod, minutesPerDay, rollingShowDev1, rollingDev1Mult, rollingShowDev2, rollingDev2Mult, rollingShowDev3, rollingDev3Mult, rollingFillOpacityCore, rollingFillOpacity12, rollingFillOpacity23, weeklyShowVWAP, weeklyStartTime, weeklyShowDev1, weeklyDev1Mult, weeklyShowDev2, weeklyDev2Mult, weeklyShowDev3, weeklyDev3Mult, weeklyFillOpacityCore, weeklyFillOpacity12, weeklyFillOpacity23);
	}

	public OrcaTimeVWAPs OrcaTimeVWAPs(ISeries<double> input, bool globexShowVWAP, TimeSpan globexStartTime, bool globexShowDev1, double globexDev1Mult, bool globexShowDev2, double globexDev2Mult, bool globexShowDev3, double globexDev3Mult, int globexFillOpacityCore, int globexFillOpacity12, int globexFillOpacity23, bool rthShowVWAP, TimeSpan rthStartTime, bool rthShowDev1, double rthDev1Mult, bool rthShowDev2, double rthDev2Mult, bool rthShowDev3, double rthDev3Mult, int rthFillOpacityCore, int rthFillOpacity12, int rthFillOpacity23, bool rollingShowVWAP, RollingVwapPeriod rollingPeriod, int minutesPerDay, bool rollingShowDev1, double rollingDev1Mult, bool rollingShowDev2, double rollingDev2Mult, bool rollingShowDev3, double rollingDev3Mult, int rollingFillOpacityCore, int rollingFillOpacity12, int rollingFillOpacity23, bool weeklyShowVWAP, TimeSpan weeklyStartTime, bool weeklyShowDev1, double weeklyDev1Mult, bool weeklyShowDev2, double weeklyDev2Mult, bool weeklyShowDev3, double weeklyDev3Mult, int weeklyFillOpacityCore, int weeklyFillOpacity12, int weeklyFillOpacity23)
	{
		return indicator.OrcaTimeVWAPs(input, globexShowVWAP, globexStartTime, globexShowDev1, globexDev1Mult, globexShowDev2, globexDev2Mult, globexShowDev3, globexDev3Mult, globexFillOpacityCore, globexFillOpacity12, globexFillOpacity23, rthShowVWAP, rthStartTime, rthShowDev1, rthDev1Mult, rthShowDev2, rthDev2Mult, rthShowDev3, rthDev3Mult, rthFillOpacityCore, rthFillOpacity12, rthFillOpacity23, rollingShowVWAP, rollingPeriod, minutesPerDay, rollingShowDev1, rollingDev1Mult, rollingShowDev2, rollingDev2Mult, rollingShowDev3, rollingDev3Mult, rollingFillOpacityCore, rollingFillOpacity12, rollingFillOpacity23, weeklyShowVWAP, weeklyStartTime, weeklyShowDev1, weeklyDev1Mult, weeklyShowDev2, weeklyDev2Mult, weeklyShowDev3, weeklyDev3Mult, weeklyFillOpacityCore, weeklyFillOpacity12, weeklyFillOpacity23);
	}

	public OrcaVisualOrders OrcaVisualOrders(int tagOffsetRight, int orderLabelOffsetRight)
	{
		return indicator.OrcaVisualOrders(((NinjaScriptBase)this).Input, tagOffsetRight, orderLabelOffsetRight);
	}

	public OrcaVisualOrders OrcaVisualOrders(ISeries<double> input, int tagOffsetRight, int orderLabelOffsetRight)
	{
		return indicator.OrcaVisualOrders(input, tagOffsetRight, orderLabelOffsetRight);
	}

	public PassiveFlowSuite PassiveFlowSuite()
	{
		return indicator.PassiveFlowSuite(((NinjaScriptBase)this).Input);
	}

	public PassiveFlowSuite PassiveFlowSuite(ISeries<double> input)
	{
		return indicator.PassiveFlowSuite(input);
	}

	public PAX30OpeningRange PAX30OpeningRange(string oRBStartSerialize, string oRBEndPlotSerialize, int textvertPixels, int textHorzOffset, int fontSize, bool boldFont, string labelPrefix, Brush highLineColor, Brush lowLineColor, Brush midLineColor, int mainLineWidth, int midLineWidth, int levelsLineWidth, bool showMid)
	{
		return indicator.PAX30OpeningRange(((NinjaScriptBase)this).Input, oRBStartSerialize, oRBEndPlotSerialize, textvertPixels, textHorzOffset, fontSize, boldFont, labelPrefix, highLineColor, lowLineColor, midLineColor, mainLineWidth, midLineWidth, levelsLineWidth, showMid);
	}

	public PAX30OpeningRange PAX30OpeningRange(ISeries<double> input, string oRBStartSerialize, string oRBEndPlotSerialize, int textvertPixels, int textHorzOffset, int fontSize, bool boldFont, string labelPrefix, Brush highLineColor, Brush lowLineColor, Brush midLineColor, int mainLineWidth, int midLineWidth, int levelsLineWidth, bool showMid)
	{
		return indicator.PAX30OpeningRange(input, oRBStartSerialize, oRBEndPlotSerialize, textvertPixels, textHorzOffset, fontSize, boldFont, labelPrefix, highLineColor, lowLineColor, midLineColor, mainLineWidth, midLineWidth, levelsLineWidth, showMid);
	}

	public NinjaTrader.NinjaScript.Indicators.VWAP VWAP()
	{
		return indicator.VWAP(((NinjaScriptBase)this).Input);
	}

	public NinjaTrader.NinjaScript.Indicators.VWAP VWAP(ISeries<double> input)
	{
		return indicator.VWAP(input);
	}

	public MarketAnalyzerColumn()
	{
		lock (((NinjaScriptBase)this).NinjaScripts)
		{
			Collection<NinjaScriptBase> ninjaScripts = ((NinjaScriptBase)this).NinjaScripts;
			Indicator obj = new Indicator();
			((NinjaScriptBase)obj).IsDataSeriesRequired = IsDataSeriesRequired;
			((NinjaScriptBase)obj).Parent = (NinjaScriptBase)(object)this;
			Indicator item = obj;
			indicator = obj;
			ninjaScripts.Add((NinjaScriptBase)(object)item);
		}
	}

	public TickRefresh TickRefresh(int refreshTimeInterval)
	{
		return indicator.TickRefresh(((NinjaScriptBase)this).Input, refreshTimeInterval);
	}

	public TickRefresh TickRefresh(ISeries<double> input, int refreshTimeInterval)
	{
		return indicator.TickRefresh(input, refreshTimeInterval);
	}

	public WoodiesCCI WoodiesCCI(int chopIndicatorWidth, int neutralBars, int period, int periodEma, int periodLinReg, int periodTurbo, int sideWinderLimit0, int sideWinderLimit1, int sideWinderWidth)
	{
		return indicator.WoodiesCCI(((NinjaScriptBase)this).Input, chopIndicatorWidth, neutralBars, period, periodEma, periodLinReg, periodTurbo, sideWinderLimit0, sideWinderLimit1, sideWinderWidth);
	}

	public WoodiesCCI WoodiesCCI(ISeries<double> input, int chopIndicatorWidth, int neutralBars, int period, int periodEma, int periodLinReg, int periodTurbo, int sideWinderLimit0, int sideWinderLimit1, int sideWinderWidth)
	{
		return indicator.WoodiesCCI(input, chopIndicatorWidth, neutralBars, period, periodEma, periodLinReg, periodTurbo, sideWinderLimit0, sideWinderLimit1, sideWinderWidth);
	}

	public WoodiesPivots WoodiesPivots(HLCCalculationModeWoodie priorDayHlc, int width)
	{
		return indicator.WoodiesPivots(((NinjaScriptBase)this).Input, priorDayHlc, width);
	}

	public WoodiesPivots WoodiesPivots(ISeries<double> input, HLCCalculationModeWoodie priorDayHlc, int width)
	{
		return indicator.WoodiesPivots(input, priorDayHlc, width);
	}

	public WisemanAlligator WisemanAlligator(int jawPeriod, int teethPeriod, int lipsPeriod, int jawOffset, int teethOffset, int lipsOffset)
	{
		return indicator.WisemanAlligator(((NinjaScriptBase)this).Input, jawPeriod, teethPeriod, lipsPeriod, jawOffset, teethOffset, lipsOffset);
	}

	public WisemanAlligator WisemanAlligator(ISeries<double> input, int jawPeriod, int teethPeriod, int lipsPeriod, int jawOffset, int teethOffset, int lipsOffset)
	{
		return indicator.WisemanAlligator(input, jawPeriod, teethPeriod, lipsPeriod, jawOffset, teethOffset, lipsOffset);
	}

	public WisemanAwesomeOscillator WisemanAwesomeOscillator()
	{
		return indicator.WisemanAwesomeOscillator(((NinjaScriptBase)this).Input);
	}

	public WisemanAwesomeOscillator WisemanAwesomeOscillator(ISeries<double> input)
	{
		return indicator.WisemanAwesomeOscillator(input);
	}

	public WisemanFractal WisemanFractal(int strength, int triangleOffset)
	{
		return indicator.WisemanFractal(((NinjaScriptBase)this).Input, strength, triangleOffset);
	}

	public WisemanFractal WisemanFractal(ISeries<double> input, int strength, int triangleOffset)
	{
		return indicator.WisemanFractal(input, strength, triangleOffset);
	}

	public OrderFlowCumulativeDelta OrderFlowCumulativeDelta(CumulativeDeltaType deltaType, CumulativeDeltaPeriod period, int sizeFilter)
	{
		return indicator.OrderFlowCumulativeDelta(((NinjaScriptBase)this).Input, deltaType, period, sizeFilter);
	}

	public OrderFlowCumulativeDelta OrderFlowCumulativeDelta(ISeries<double> input, CumulativeDeltaType deltaType, CumulativeDeltaPeriod period, int sizeFilter)
	{
		return indicator.OrderFlowCumulativeDelta(input, deltaType, period, sizeFilter);
	}

	public OrderFlowMarketDepthMap OrderFlowMarketDepthMap(BaseVolumeRange baseRange, int maxRange, int minRange, OpacityDistribution opacityDistribution, int depthMargin, bool extendLastKnown, bool showBidAskLine)
	{
		return indicator.OrderFlowMarketDepthMap(((NinjaScriptBase)this).Input, baseRange, maxRange, minRange, opacityDistribution, depthMargin, extendLastKnown, showBidAskLine);
	}

	public OrderFlowMarketDepthMap OrderFlowMarketDepthMap(ISeries<double> input, BaseVolumeRange baseRange, int maxRange, int minRange, OpacityDistribution opacityDistribution, int depthMargin, bool extendLastKnown, bool showBidAskLine)
	{
		return indicator.OrderFlowMarketDepthMap(input, baseRange, maxRange, minRange, opacityDistribution, depthMargin, extendLastKnown, showBidAskLine);
	}

	public NinjaTrader.NinjaScript.Indicators.OrderFlowVolumeProfile OrderFlowVolumeProfile(MarketProfileType profileType, MarketProfilePeriod profilePeriod, int sessions, TradingHours tradingHoursInstance, MarketProfileResolution resolution, int valueAreaPercent, int initialBalanceMinutes)
	{
		return indicator.OrderFlowVolumeProfile(((NinjaScriptBase)this).Input, profileType, profilePeriod, sessions, tradingHoursInstance, resolution, valueAreaPercent, initialBalanceMinutes);
	}

	public NinjaTrader.NinjaScript.Indicators.OrderFlowVolumeProfile OrderFlowVolumeProfile(ISeries<double> input, MarketProfileType profileType, MarketProfilePeriod profilePeriod, int sessions, TradingHours tradingHoursInstance, MarketProfileResolution resolution, int valueAreaPercent, int initialBalanceMinutes)
	{
		return indicator.OrderFlowVolumeProfile(input, profileType, profilePeriod, sessions, tradingHoursInstance, resolution, valueAreaPercent, initialBalanceMinutes);
	}

	public OrderFlowVWAP OrderFlowVWAP(VWAPResolution resolution, TradingHours tradingHoursInstance, VWAPStandardDeviations numStandardDeviations, double sD1Multiplier, double sD2Multiplier, double sD3Multiplier)
	{
		return indicator.OrderFlowVWAP(((NinjaScriptBase)this).Input, resolution, tradingHoursInstance, numStandardDeviations, sD1Multiplier, sD2Multiplier, sD3Multiplier);
	}

	public OrderFlowVWAP OrderFlowVWAP(ISeries<double> input, VWAPResolution resolution, TradingHours tradingHoursInstance, VWAPStandardDeviations numStandardDeviations, double sD1Multiplier, double sD2Multiplier, double sD3Multiplier)
	{
		return indicator.OrderFlowVWAP(input, resolution, tradingHoursInstance, numStandardDeviations, sD1Multiplier, sD2Multiplier, sD3Multiplier);
	}

	public OrderFlowTradeDetector OrderFlowTradeDetector(TradeDetectorBaseLargeVolumeOn baseLargeVolumeOn, int minimumVolumeForMarker, int maximumMarkerSize, TradeDetectorSizeBase baseMarkerSizeOn, bool hoverValues)
	{
		return indicator.OrderFlowTradeDetector(((NinjaScriptBase)this).Input, baseLargeVolumeOn, minimumVolumeForMarker, maximumMarkerSize, baseMarkerSizeOn, hoverValues);
	}

	public OrderFlowTradeDetector OrderFlowTradeDetector(ISeries<double> input, TradeDetectorBaseLargeVolumeOn baseLargeVolumeOn, int minimumVolumeForMarker, int maximumMarkerSize, TradeDetectorSizeBase baseMarkerSizeOn, bool hoverValues)
	{
		return indicator.OrderFlowTradeDetector(input, baseLargeVolumeOn, minimumVolumeForMarker, maximumMarkerSize, baseMarkerSizeOn, hoverValues);
	}

	public OTMDeltaBarFree OTMDeltaBarFree()
	{
		return indicator.OTMDeltaBarFree(((NinjaScriptBase)this).Input);
	}

	public OTMDeltaBarFree OTMDeltaBarFree(ISeries<double> input)
	{
		return indicator.OTMDeltaBarFree(input);
	}

	public T3000_LagDetector T3000_LagDetector()
	{
		return indicator.T3000_LagDetector(((NinjaScriptBase)this).Input);
	}

	public T3000_MGI_Daily T3000_MGI_Daily()
	{
		return indicator.T3000_MGI_Daily(((NinjaScriptBase)this).Input);
	}

	public T3000_MGI_Monthly T3000_MGI_Monthly()
	{
		return indicator.T3000_MGI_Monthly(((NinjaScriptBase)this).Input);
	}

	public T3000_MGI_Statistics T3000_MGI_Statistics()
	{
		return indicator.T3000_MGI_Statistics(((NinjaScriptBase)this).Input);
	}

	public T3000_MGI_Weekly T3000_MGI_Weekly()
	{
		return indicator.T3000_MGI_Weekly(((NinjaScriptBase)this).Input);
	}

	public T3000_LagDetector T3000_LagDetector(ISeries<double> input)
	{
		return indicator.T3000_LagDetector(input);
	}

	public T3000_MGI_Daily T3000_MGI_Daily(ISeries<double> input)
	{
		return indicator.T3000_MGI_Daily(input);
	}

	public T3000_MGI_Monthly T3000_MGI_Monthly(ISeries<double> input)
	{
		return indicator.T3000_MGI_Monthly(input);
	}

	public T3000_MGI_Statistics T3000_MGI_Statistics(ISeries<double> input)
	{
		return indicator.T3000_MGI_Statistics(input);
	}

	public T3000_MGI_Weekly T3000_MGI_Weekly(ISeries<double> input)
	{
		return indicator.T3000_MGI_Weekly(input);
	}
}

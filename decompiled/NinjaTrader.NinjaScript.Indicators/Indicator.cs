using System;
using System.Windows.Media;
using CustomBsEnumNamespace;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators.Dhonn;
using NinjaTrader.NinjaScript.Indicators.OTM;
using NinjaTrader.NinjaScript.Indicators.PAX;
using NinjaTrader.NinjaScript.Indicators.T3000.MGI;

namespace NinjaTrader.NinjaScript.Indicators;

public class Indicator : IndicatorRenderBase
{
	private ADL[] cacheADL;

	private ADX[] cacheADX;

	private ADXR[] cacheADXR;

	private APZ[] cacheAPZ;

	private Aroon[] cacheAroon;

	private AroonOscillator[] cacheAroonOscillator;

	private ATR[] cacheATR;

	private BarTimer[] cacheBarTimer;

	private BlockVolume[] cacheBlockVolume;

	private Bollinger[] cacheBollinger;

	private BOP[] cacheBOP;

	private BuySellPressure[] cacheBuySellPressure;

	private BuySellVolume[] cacheBuySellVolume;

	private CamarillaPivots[] cacheCamarillaPivots;

	private CandlestickPattern[] cacheCandlestickPattern;

	private CCI[] cacheCCI;

	private ChaikinMoneyFlow[] cacheChaikinMoneyFlow;

	private ChaikinOscillator[] cacheChaikinOscillator;

	private ChaikinVolatility[] cacheChaikinVolatility;

	private ChoppinessIndex[] cacheChoppinessIndex;

	private CMO[] cacheCMO;

	private ConstantLines[] cacheConstantLines;

	private Correlation[] cacheCorrelation;

	private COT[] cacheCOT;

	private CurrentDayOHL[] cacheCurrentDayOHL;

	private Darvas[] cacheDarvas;

	private DEMA[] cacheDEMA;

	private DisparityIndex[] cacheDisparityIndex;

	private DM[] cacheDM;

	private DMI[] cacheDMI;

	private DMIndex[] cacheDMIndex;

	private DonchianChannel[] cacheDonchianChannel;

	private DoubleStochastics[] cacheDoubleStochastics;

	private EaseOfMovement[] cacheEaseOfMovement;

	private EMA[] cacheEMA;

	private FibonacciPivots[] cacheFibonacciPivots;

	private FisherTransform[] cacheFisherTransform;

	private FOSC[] cacheFOSC;

	private FVG[] cacheFVG;

	private HMA[] cacheHMA;

	private IchimokuCloud[] cacheIchimokuCloud;

	private KAMA[] cacheKAMA;

	private KeltnerChannel[] cacheKeltnerChannel;

	private KeyReversalDown[] cacheKeyReversalDown;

	private KeyReversalUp[] cacheKeyReversalUp;

	private LinReg[] cacheLinReg;

	private LinRegIntercept[] cacheLinRegIntercept;

	private LinRegSlope[] cacheLinRegSlope;

	private MACD[] cacheMACD;

	private MAEnvelopes[] cacheMAEnvelopes;

	private MAMA[] cacheMAMA;

	private MAX[] cacheMAX;

	private McClellanOscillator[] cacheMcClellanOscillator;

	private MFI[] cacheMFI;

	private MIN[] cacheMIN;

	private Momentum[] cacheMomentum;

	private MoneyFlowOscillator[] cacheMoneyFlowOscillator;

	private MovingAverageRibbon[] cacheMovingAverageRibbon;

	private NBarsDown[] cacheNBarsDown;

	private NBarsUp[] cacheNBarsUp;

	private NetChangeDisplay[] cacheNetChangeDisplay;

	private OBV[] cacheOBV;

	private ParabolicSAR[] cacheParabolicSAR;

	private PFE[] cachePFE;

	private Pivots[] cachePivots;

	private PPO[] cachePPO;

	private PriceLine[] cachePriceLine;

	private PriceOscillator[] cachePriceOscillator;

	private PriorDayOHLC[] cachePriorDayOHLC;

	private PsychologicalLine[] cachePsychologicalLine;

	private Range[] cacheRange;

	private RangeCounter[] cacheRangeCounter;

	private RegressionChannel[] cacheRegressionChannel;

	private RelativeVigorIndex[] cacheRelativeVigorIndex;

	private RIND[] cacheRIND;

	private ROC[] cacheROC;

	private RSI[] cacheRSI;

	private RSquared[] cacheRSquared;

	private RSS[] cacheRSS;

	private RVI[] cacheRVI;

	private SampleCustomRender[] cacheSampleCustomRender;

	private SMA[] cacheSMA;

	private StdDev[] cacheStdDev;

	private StdError[] cacheStdError;

	private Stochastics[] cacheStochastics;

	private StochasticsFast[] cacheStochasticsFast;

	private StochRSI[] cacheStochRSI;

	private SUM[] cacheSUM;

	private Swing[] cacheSwing;

	private T3[] cacheT3;

	private TEMA[] cacheTEMA;

	private TickCounter[] cacheTickCounter;

	private TMA[] cacheTMA;

	private TrendLines[] cacheTrendLines;

	private TRIX[] cacheTRIX;

	private TSF[] cacheTSF;

	private TSI[] cacheTSI;

	private UltimateOscillator[] cacheUltimateOscillator;

	private VMA[] cacheVMA;

	private VOL[] cacheVOL;

	private VOLMA[] cacheVOLMA;

	private VolumeCounter[] cacheVolumeCounter;

	private VolumeOscillator[] cacheVolumeOscillator;

	private VolumeProfile[] cacheVolumeProfile;

	private VolumeUpDown[] cacheVolumeUpDown;

	private VolumeZones[] cacheVolumeZones;

	private Vortex[] cacheVortex;

	private VROC[] cacheVROC;

	private VWMA[] cacheVWMA;

	private WilliamsR[] cacheWilliamsR;

	private WMA[] cacheWMA;

	private ZigZag[] cacheZigZag;

	private ZLEMA[] cacheZLEMA;

	private AutoLegProfile[] cacheAutoLegProfile;

	private AutoLegProfileNT[] cacheAutoLegProfileNT;

	private AutoLegProfileNT2[] cacheAutoLegProfileNT2;

	private BarTimes[] cacheBarTimes;

	private FastCandleHighlight[] cacheFastCandleHighlight;

	private LegToLegDeltaProfile[] cacheLegToLegDeltaProfile;

	private OrcaAbsorptionCandles[] cacheOrcaAbsorptionCandles;

	private OrcaAnchoredVWAPs[] cacheOrcaAnchoredVWAPs;

	private OrcaCandleVolumeProfile[] cacheOrcaCandleVolumeProfile;

	private OrcaCumulativeDelta[] cacheOrcaCumulativeDelta;

	private OrcaExecutionLines[] cacheOrcaExecutionLines;

	private OrcaLegtoLegProfile[] cacheOrcaLegtoLegProfile;

	private OrcaRollingProfiles[] cacheOrcaRollingProfiles;

	private OrcaStepProfile[] cacheOrcaStepProfile;

	private OrcaTickDirectionIndex[] cacheOrcaTickDirectionIndex;

	private OrcaTimeStatistics[] cacheOrcaTimeStatistics;

	private OrcaTimeVWAPs[] cacheOrcaTimeVWAPs;

	private OrcaVisualOrders[] cacheOrcaVisualOrders;

	private PassiveFlowSuite[] cachePassiveFlowSuite;

	private PAX30OpeningRange[] cachePAX30OpeningRange;

	private VWAP[] cacheVWAP;

	private TickRefresh[] cacheTickRefresh;

	private WoodiesCCI[] cacheWoodiesCCI;

	private WoodiesPivots[] cacheWoodiesPivots;

	private WisemanAlligator[] cacheWisemanAlligator;

	private WisemanAwesomeOscillator[] cacheWisemanAwesomeOscillator;

	private WisemanFractal[] cacheWisemanFractal;

	private OrderFlowCumulativeDelta[] cacheOrderFlowCumulativeDelta;

	private OrderFlowMarketDepthMap[] cacheOrderFlowMarketDepthMap;

	private OrderFlowVWAP[] cacheOrderFlowVWAP;

	private OrderFlowTradeDetector[] cacheOrderFlowTradeDetector;

	private OrderFlowVolumeProfile[] cacheOrderFlowVP;

	private OTMDeltaBarFree[] cacheOTMDeltaBarFree;

	private T3000_LagDetector[] cacheT3000_LagDetector;

	private T3000_MGI_Daily[] cacheT3000_MGI_Daily;

	private T3000_MGI_Monthly[] cacheT3000_MGI_Monthly;

	private T3000_MGI_Statistics[] cacheT3000_MGI_Statistics;

	private T3000_MGI_Weekly[] cacheT3000_MGI_Weekly;

	public ADL ADL()
	{
		return ADL(((NinjaScriptBase)this).Input);
	}

	public ADL ADL(ISeries<double> input)
	{
		if (cacheADL != null)
		{
			for (int i = 0; i < cacheADL.Length; i++)
			{
				if (cacheADL[i] != null && ((NinjaScriptBase)cacheADL[i]).EqualsInput(input))
				{
					return cacheADL[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ADL>(new ADL(), input, ref cacheADL);
	}

	public ADX ADX(int period)
	{
		return ADX(((NinjaScriptBase)this).Input, period);
	}

	public ADX ADX(ISeries<double> input, int period)
	{
		if (cacheADX != null)
		{
			for (int i = 0; i < cacheADX.Length; i++)
			{
				if (cacheADX[i] != null && cacheADX[i].Period == period && ((NinjaScriptBase)cacheADX[i]).EqualsInput(input))
				{
					return cacheADX[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ADX>(new ADX
		{
			Period = period
		}, input, ref cacheADX);
	}

	public ADXR ADXR(int interval, int period)
	{
		return ADXR(((NinjaScriptBase)this).Input, interval, period);
	}

	public ADXR ADXR(ISeries<double> input, int interval, int period)
	{
		if (cacheADXR != null)
		{
			for (int i = 0; i < cacheADXR.Length; i++)
			{
				if (cacheADXR[i] != null && cacheADXR[i].Interval == interval && cacheADXR[i].Period == period && ((NinjaScriptBase)cacheADXR[i]).EqualsInput(input))
				{
					return cacheADXR[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ADXR>(new ADXR
		{
			Interval = interval,
			Period = period
		}, input, ref cacheADXR);
	}

	public APZ APZ(double bandPct, int period)
	{
		return APZ(((NinjaScriptBase)this).Input, bandPct, period);
	}

	public APZ APZ(ISeries<double> input, double bandPct, int period)
	{
		if (cacheAPZ != null)
		{
			for (int i = 0; i < cacheAPZ.Length; i++)
			{
				if (cacheAPZ[i] != null && cacheAPZ[i].BandPct == bandPct && cacheAPZ[i].Period == period && ((NinjaScriptBase)cacheAPZ[i]).EqualsInput(input))
				{
					return cacheAPZ[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<APZ>(new APZ
		{
			BandPct = bandPct,
			Period = period
		}, input, ref cacheAPZ);
	}

	public Aroon Aroon(int period)
	{
		return Aroon(((NinjaScriptBase)this).Input, period);
	}

	public Aroon Aroon(ISeries<double> input, int period)
	{
		if (cacheAroon != null)
		{
			for (int i = 0; i < cacheAroon.Length; i++)
			{
				if (cacheAroon[i] != null && cacheAroon[i].Period == period && ((NinjaScriptBase)cacheAroon[i]).EqualsInput(input))
				{
					return cacheAroon[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<Aroon>(new Aroon
		{
			Period = period
		}, input, ref cacheAroon);
	}

	public AroonOscillator AroonOscillator(int period)
	{
		return AroonOscillator(((NinjaScriptBase)this).Input, period);
	}

	public AroonOscillator AroonOscillator(ISeries<double> input, int period)
	{
		if (cacheAroonOscillator != null)
		{
			for (int i = 0; i < cacheAroonOscillator.Length; i++)
			{
				if (cacheAroonOscillator[i] != null && cacheAroonOscillator[i].Period == period && ((NinjaScriptBase)cacheAroonOscillator[i]).EqualsInput(input))
				{
					return cacheAroonOscillator[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<AroonOscillator>(new AroonOscillator
		{
			Period = period
		}, input, ref cacheAroonOscillator);
	}

	public ATR ATR(int period)
	{
		return ATR(((NinjaScriptBase)this).Input, period);
	}

	public ATR ATR(ISeries<double> input, int period)
	{
		if (cacheATR != null)
		{
			for (int i = 0; i < cacheATR.Length; i++)
			{
				if (cacheATR[i] != null && cacheATR[i].Period == period && ((NinjaScriptBase)cacheATR[i]).EqualsInput(input))
				{
					return cacheATR[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ATR>(new ATR
		{
			Period = period
		}, input, ref cacheATR);
	}

	public BarTimer BarTimer()
	{
		return BarTimer(((NinjaScriptBase)this).Input, TextPositionFine.BottomRight);
	}

	public BarTimer BarTimer(TextPositionFine textPositionFine)
	{
		return BarTimer(((NinjaScriptBase)this).Input, textPositionFine);
	}

	public BarTimer BarTimer(ISeries<double> input)
	{
		return BarTimer(input, TextPositionFine.BottomRight);
	}

	public BarTimer BarTimer(ISeries<double> input, TextPositionFine textPositionFine)
	{
		if (cacheBarTimer != null)
		{
			for (int i = 0; i < cacheBarTimer.Length; i++)
			{
				if (cacheBarTimer[i] != null && cacheBarTimer[i].TextPositionFine == textPositionFine && ((NinjaScriptBase)cacheBarTimer[i]).EqualsInput(input))
				{
					return cacheBarTimer[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<BarTimer>(new BarTimer(), input, ref cacheBarTimer);
	}

	public BlockVolume BlockVolume(double blockSize, CountType countType)
	{
		return BlockVolume(((NinjaScriptBase)this).Input, blockSize, countType);
	}

	public BlockVolume BlockVolume(ISeries<double> input, double blockSize, CountType countType)
	{
		if (cacheBlockVolume != null)
		{
			for (int i = 0; i < cacheBlockVolume.Length; i++)
			{
				if (cacheBlockVolume[i] != null && cacheBlockVolume[i].BlockSize == blockSize && cacheBlockVolume[i].CountType == countType && ((NinjaScriptBase)cacheBlockVolume[i]).EqualsInput(input))
				{
					return cacheBlockVolume[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<BlockVolume>(new BlockVolume
		{
			BlockSize = blockSize,
			CountType = countType
		}, input, ref cacheBlockVolume);
	}

	public Bollinger Bollinger(double numStdDev, int period)
	{
		return Bollinger(((NinjaScriptBase)this).Input, numStdDev, period);
	}

	public Bollinger Bollinger(ISeries<double> input, double numStdDev, int period)
	{
		if (cacheBollinger != null)
		{
			for (int i = 0; i < cacheBollinger.Length; i++)
			{
				if (cacheBollinger[i] != null && cacheBollinger[i].NumStdDev == numStdDev && cacheBollinger[i].Period == period && ((NinjaScriptBase)cacheBollinger[i]).EqualsInput(input))
				{
					return cacheBollinger[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<Bollinger>(new Bollinger
		{
			NumStdDev = numStdDev,
			Period = period
		}, input, ref cacheBollinger);
	}

	public BOP BOP(int smooth)
	{
		return BOP(((NinjaScriptBase)this).Input, smooth);
	}

	public BOP BOP(ISeries<double> input, int smooth)
	{
		if (cacheBOP != null)
		{
			for (int i = 0; i < cacheBOP.Length; i++)
			{
				if (cacheBOP[i] != null && cacheBOP[i].Smooth == smooth && ((NinjaScriptBase)cacheBOP[i]).EqualsInput(input))
				{
					return cacheBOP[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<BOP>(new BOP
		{
			Smooth = smooth
		}, input, ref cacheBOP);
	}

	public BuySellPressure BuySellPressure()
	{
		return BuySellPressure(((NinjaScriptBase)this).Input);
	}

	public BuySellPressure BuySellPressure(ISeries<double> input)
	{
		if (cacheBuySellPressure != null)
		{
			for (int i = 0; i < cacheBuySellPressure.Length; i++)
			{
				if (cacheBuySellPressure[i] != null && ((NinjaScriptBase)cacheBuySellPressure[i]).EqualsInput(input))
				{
					return cacheBuySellPressure[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<BuySellPressure>(new BuySellPressure(), input, ref cacheBuySellPressure);
	}

	public BuySellVolume BuySellVolume()
	{
		return BuySellVolume(((NinjaScriptBase)this).Input);
	}

	public BuySellVolume BuySellVolume(ISeries<double> input)
	{
		if (cacheBuySellVolume != null)
		{
			for (int i = 0; i < cacheBuySellVolume.Length; i++)
			{
				if (cacheBuySellVolume[i] != null && ((NinjaScriptBase)cacheBuySellVolume[i]).EqualsInput(input))
				{
					return cacheBuySellVolume[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<BuySellVolume>(new BuySellVolume(), input, ref cacheBuySellVolume);
	}

	public CamarillaPivots CamarillaPivots(PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		return CamarillaPivots(((NinjaScriptBase)this).Input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
	}

	public CamarillaPivots CamarillaPivots(ISeries<double> input, PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		if (cacheCamarillaPivots != null)
		{
			for (int i = 0; i < cacheCamarillaPivots.Length; i++)
			{
				if (cacheCamarillaPivots[i] != null && cacheCamarillaPivots[i].PivotRangeType == pivotRangeType && cacheCamarillaPivots[i].PriorDayHlc == priorDayHlc && cacheCamarillaPivots[i].UserDefinedClose == userDefinedClose && cacheCamarillaPivots[i].UserDefinedHigh == userDefinedHigh && cacheCamarillaPivots[i].UserDefinedLow == userDefinedLow && cacheCamarillaPivots[i].Width == width && ((NinjaScriptBase)cacheCamarillaPivots[i]).EqualsInput(input))
				{
					return cacheCamarillaPivots[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<CamarillaPivots>(new CamarillaPivots
		{
			PivotRangeType = pivotRangeType,
			PriorDayHlc = priorDayHlc,
			UserDefinedClose = userDefinedClose,
			UserDefinedHigh = userDefinedHigh,
			UserDefinedLow = userDefinedLow,
			Width = width
		}, input, ref cacheCamarillaPivots);
	}

	public CandlestickPattern CandlestickPattern(ChartPattern pattern, int trendStrength)
	{
		return CandlestickPattern(((NinjaScriptBase)this).Input, pattern, trendStrength);
	}

	public CandlestickPattern CandlestickPattern(ISeries<double> input, ChartPattern pattern, int trendStrength)
	{
		if (cacheCandlestickPattern != null)
		{
			for (int i = 0; i < cacheCandlestickPattern.Length; i++)
			{
				if (cacheCandlestickPattern[i] != null && cacheCandlestickPattern[i].Pattern == pattern && cacheCandlestickPattern[i].TrendStrength == trendStrength && ((NinjaScriptBase)cacheCandlestickPattern[i]).EqualsInput(input))
				{
					return cacheCandlestickPattern[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<CandlestickPattern>(new CandlestickPattern
		{
			Pattern = pattern,
			TrendStrength = trendStrength
		}, input, ref cacheCandlestickPattern);
	}

	public CCI CCI(int period)
	{
		return CCI(((NinjaScriptBase)this).Input, period);
	}

	public CCI CCI(ISeries<double> input, int period)
	{
		if (cacheCCI != null)
		{
			for (int i = 0; i < cacheCCI.Length; i++)
			{
				if (cacheCCI[i] != null && cacheCCI[i].Period == period && ((NinjaScriptBase)cacheCCI[i]).EqualsInput(input))
				{
					return cacheCCI[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<CCI>(new CCI
		{
			Period = period
		}, input, ref cacheCCI);
	}

	public ChaikinMoneyFlow ChaikinMoneyFlow(int period)
	{
		return ChaikinMoneyFlow(((NinjaScriptBase)this).Input, period);
	}

	public ChaikinMoneyFlow ChaikinMoneyFlow(ISeries<double> input, int period)
	{
		if (cacheChaikinMoneyFlow != null)
		{
			for (int i = 0; i < cacheChaikinMoneyFlow.Length; i++)
			{
				if (cacheChaikinMoneyFlow[i] != null && cacheChaikinMoneyFlow[i].Period == period && ((NinjaScriptBase)cacheChaikinMoneyFlow[i]).EqualsInput(input))
				{
					return cacheChaikinMoneyFlow[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ChaikinMoneyFlow>(new ChaikinMoneyFlow
		{
			Period = period
		}, input, ref cacheChaikinMoneyFlow);
	}

	public ChaikinOscillator ChaikinOscillator(int fast, int slow)
	{
		return ChaikinOscillator(((NinjaScriptBase)this).Input, fast, slow);
	}

	public ChaikinOscillator ChaikinOscillator(ISeries<double> input, int fast, int slow)
	{
		if (cacheChaikinOscillator != null)
		{
			for (int i = 0; i < cacheChaikinOscillator.Length; i++)
			{
				if (cacheChaikinOscillator[i] != null && cacheChaikinOscillator[i].Fast == fast && cacheChaikinOscillator[i].Slow == slow && ((NinjaScriptBase)cacheChaikinOscillator[i]).EqualsInput(input))
				{
					return cacheChaikinOscillator[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ChaikinOscillator>(new ChaikinOscillator
		{
			Fast = fast,
			Slow = slow
		}, input, ref cacheChaikinOscillator);
	}

	public ChaikinVolatility ChaikinVolatility(int mAPeriod, int rOCPeriod)
	{
		return ChaikinVolatility(((NinjaScriptBase)this).Input, mAPeriod, rOCPeriod);
	}

	public ChaikinVolatility ChaikinVolatility(ISeries<double> input, int mAPeriod, int rOCPeriod)
	{
		if (cacheChaikinVolatility != null)
		{
			for (int i = 0; i < cacheChaikinVolatility.Length; i++)
			{
				if (cacheChaikinVolatility[i] != null && cacheChaikinVolatility[i].MAPeriod == mAPeriod && cacheChaikinVolatility[i].ROCPeriod == rOCPeriod && ((NinjaScriptBase)cacheChaikinVolatility[i]).EqualsInput(input))
				{
					return cacheChaikinVolatility[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ChaikinVolatility>(new ChaikinVolatility
		{
			MAPeriod = mAPeriod,
			ROCPeriod = rOCPeriod
		}, input, ref cacheChaikinVolatility);
	}

	public ChoppinessIndex ChoppinessIndex(int period)
	{
		return ChoppinessIndex(((NinjaScriptBase)this).Input, period);
	}

	public ChoppinessIndex ChoppinessIndex(ISeries<double> input, int period)
	{
		if (cacheChoppinessIndex != null)
		{
			for (int i = 0; i < cacheChoppinessIndex.Length; i++)
			{
				if (cacheChoppinessIndex[i] != null && cacheChoppinessIndex[i].Period == period && ((NinjaScriptBase)cacheChoppinessIndex[i]).EqualsInput(input))
				{
					return cacheChoppinessIndex[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ChoppinessIndex>(new ChoppinessIndex
		{
			Period = period
		}, input, ref cacheChoppinessIndex);
	}

	public CMO CMO(int period)
	{
		return CMO(((NinjaScriptBase)this).Input, period);
	}

	public CMO CMO(ISeries<double> input, int period)
	{
		if (cacheCMO != null)
		{
			for (int i = 0; i < cacheCMO.Length; i++)
			{
				if (cacheCMO[i] != null && cacheCMO[i].Period == period && ((NinjaScriptBase)cacheCMO[i]).EqualsInput(input))
				{
					return cacheCMO[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<CMO>(new CMO
		{
			Period = period
		}, input, ref cacheCMO);
	}

	public ConstantLines ConstantLines(double line1Value, double line2Value, double line3Value, double line4Value)
	{
		return ConstantLines(((NinjaScriptBase)this).Input, line1Value, line2Value, line3Value, line4Value);
	}

	public ConstantLines ConstantLines(ISeries<double> input, double line1Value, double line2Value, double line3Value, double line4Value)
	{
		if (cacheConstantLines != null)
		{
			for (int i = 0; i < cacheConstantLines.Length; i++)
			{
				if (cacheConstantLines[i] != null && cacheConstantLines[i].Line1Value == line1Value && cacheConstantLines[i].Line2Value == line2Value && cacheConstantLines[i].Line3Value == line3Value && cacheConstantLines[i].Line4Value == line4Value && ((NinjaScriptBase)cacheConstantLines[i]).EqualsInput(input))
				{
					return cacheConstantLines[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ConstantLines>(new ConstantLines
		{
			Line1Value = line1Value,
			Line2Value = line2Value,
			Line3Value = line3Value,
			Line4Value = line4Value
		}, input, ref cacheConstantLines);
	}

	public Correlation Correlation(int period, string correlationSeries)
	{
		return Correlation(((NinjaScriptBase)this).Input, period, correlationSeries);
	}

	public Correlation Correlation(ISeries<double> input, int period, string correlationSeries)
	{
		if (cacheCorrelation != null)
		{
			for (int i = 0; i < cacheCorrelation.Length; i++)
			{
				if (cacheCorrelation[i] != null && cacheCorrelation[i].Period == period && cacheCorrelation[i].CorrelationSeries == correlationSeries && ((NinjaScriptBase)cacheCorrelation[i]).EqualsInput(input))
				{
					return cacheCorrelation[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<Correlation>(new Correlation
		{
			Period = period,
			CorrelationSeries = correlationSeries
		}, input, ref cacheCorrelation);
	}

	public COT COT(int number)
	{
		return COT(((NinjaScriptBase)this).Input, number);
	}

	public COT COT(ISeries<double> input, int number)
	{
		if (cacheCOT != null)
		{
			for (int i = 0; i < cacheCOT.Length; i++)
			{
				if (cacheCOT[i] != null && cacheCOT[i].Number == number && ((NinjaScriptBase)cacheCOT[i]).EqualsInput(input))
				{
					return cacheCOT[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<COT>(new COT
		{
			Number = number
		}, input, ref cacheCOT);
	}

	public CurrentDayOHL CurrentDayOHL()
	{
		return CurrentDayOHL(((NinjaScriptBase)this).Input);
	}

	public CurrentDayOHL CurrentDayOHL(ISeries<double> input)
	{
		if (cacheCurrentDayOHL != null)
		{
			for (int i = 0; i < cacheCurrentDayOHL.Length; i++)
			{
				if (cacheCurrentDayOHL[i] != null && ((NinjaScriptBase)cacheCurrentDayOHL[i]).EqualsInput(input))
				{
					return cacheCurrentDayOHL[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<CurrentDayOHL>(new CurrentDayOHL(), input, ref cacheCurrentDayOHL);
	}

	public Darvas Darvas()
	{
		return Darvas(((NinjaScriptBase)this).Input);
	}

	public Darvas Darvas(ISeries<double> input)
	{
		if (cacheDarvas != null)
		{
			for (int i = 0; i < cacheDarvas.Length; i++)
			{
				if (cacheDarvas[i] != null && ((NinjaScriptBase)cacheDarvas[i]).EqualsInput(input))
				{
					return cacheDarvas[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<Darvas>(new Darvas(), input, ref cacheDarvas);
	}

	public DEMA DEMA(int period)
	{
		return DEMA(((NinjaScriptBase)this).Input, period);
	}

	public DEMA DEMA(ISeries<double> input, int period)
	{
		if (cacheDEMA != null)
		{
			for (int i = 0; i < cacheDEMA.Length; i++)
			{
				if (cacheDEMA[i] != null && cacheDEMA[i].Period == period && ((NinjaScriptBase)cacheDEMA[i]).EqualsInput(input))
				{
					return cacheDEMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<DEMA>(new DEMA
		{
			Period = period
		}, input, ref cacheDEMA);
	}

	public DisparityIndex DisparityIndex(int period)
	{
		return DisparityIndex(((NinjaScriptBase)this).Input, period);
	}

	public DisparityIndex DisparityIndex(ISeries<double> input, int period)
	{
		if (cacheDisparityIndex != null)
		{
			for (int i = 0; i < cacheDisparityIndex.Length; i++)
			{
				if (cacheDisparityIndex[i] != null && cacheDisparityIndex[i].Period == period && ((NinjaScriptBase)cacheDisparityIndex[i]).EqualsInput(input))
				{
					return cacheDisparityIndex[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<DisparityIndex>(new DisparityIndex
		{
			Period = period
		}, input, ref cacheDisparityIndex);
	}

	public DM DM(int period)
	{
		return DM(((NinjaScriptBase)this).Input, period);
	}

	public DM DM(ISeries<double> input, int period)
	{
		if (cacheDM != null)
		{
			for (int i = 0; i < cacheDM.Length; i++)
			{
				if (cacheDM[i] != null && cacheDM[i].Period == period && ((NinjaScriptBase)cacheDM[i]).EqualsInput(input))
				{
					return cacheDM[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<DM>(new DM
		{
			Period = period
		}, input, ref cacheDM);
	}

	public DMI DMI(int period)
	{
		return DMI(((NinjaScriptBase)this).Input, period);
	}

	public DMI DMI(ISeries<double> input, int period)
	{
		if (cacheDMI != null)
		{
			for (int i = 0; i < cacheDMI.Length; i++)
			{
				if (cacheDMI[i] != null && cacheDMI[i].Period == period && ((NinjaScriptBase)cacheDMI[i]).EqualsInput(input))
				{
					return cacheDMI[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<DMI>(new DMI
		{
			Period = period
		}, input, ref cacheDMI);
	}

	public DMIndex DMIndex(int smooth)
	{
		return DMIndex(((NinjaScriptBase)this).Input, smooth);
	}

	public DMIndex DMIndex(ISeries<double> input, int smooth)
	{
		if (cacheDMIndex != null)
		{
			for (int i = 0; i < cacheDMIndex.Length; i++)
			{
				if (cacheDMIndex[i] != null && cacheDMIndex[i].Smooth == smooth && ((NinjaScriptBase)cacheDMIndex[i]).EqualsInput(input))
				{
					return cacheDMIndex[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<DMIndex>(new DMIndex
		{
			Smooth = smooth
		}, input, ref cacheDMIndex);
	}

	public DonchianChannel DonchianChannel(int period)
	{
		return DonchianChannel(((NinjaScriptBase)this).Input, period);
	}

	public DonchianChannel DonchianChannel(ISeries<double> input, int period)
	{
		if (cacheDonchianChannel != null)
		{
			for (int i = 0; i < cacheDonchianChannel.Length; i++)
			{
				if (cacheDonchianChannel[i] != null && cacheDonchianChannel[i].Period == period && ((NinjaScriptBase)cacheDonchianChannel[i]).EqualsInput(input))
				{
					return cacheDonchianChannel[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<DonchianChannel>(new DonchianChannel
		{
			Period = period
		}, input, ref cacheDonchianChannel);
	}

	public DoubleStochastics DoubleStochastics(int period)
	{
		return DoubleStochastics(((NinjaScriptBase)this).Input, period);
	}

	public DoubleStochastics DoubleStochastics(ISeries<double> input, int period)
	{
		if (cacheDoubleStochastics != null)
		{
			for (int i = 0; i < cacheDoubleStochastics.Length; i++)
			{
				if (cacheDoubleStochastics[i] != null && cacheDoubleStochastics[i].Period == period && ((NinjaScriptBase)cacheDoubleStochastics[i]).EqualsInput(input))
				{
					return cacheDoubleStochastics[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<DoubleStochastics>(new DoubleStochastics
		{
			Period = period
		}, input, ref cacheDoubleStochastics);
	}

	public EaseOfMovement EaseOfMovement(int smoothing, int volumeDivisor)
	{
		return EaseOfMovement(((NinjaScriptBase)this).Input, smoothing, volumeDivisor);
	}

	public EaseOfMovement EaseOfMovement(ISeries<double> input, int smoothing, int volumeDivisor)
	{
		if (cacheEaseOfMovement != null)
		{
			for (int i = 0; i < cacheEaseOfMovement.Length; i++)
			{
				if (cacheEaseOfMovement[i] != null && cacheEaseOfMovement[i].Smoothing == smoothing && cacheEaseOfMovement[i].VolumeDivisor == volumeDivisor && ((NinjaScriptBase)cacheEaseOfMovement[i]).EqualsInput(input))
				{
					return cacheEaseOfMovement[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<EaseOfMovement>(new EaseOfMovement
		{
			Smoothing = smoothing,
			VolumeDivisor = volumeDivisor
		}, input, ref cacheEaseOfMovement);
	}

	public EMA EMA(int period)
	{
		return EMA(((NinjaScriptBase)this).Input, period);
	}

	public EMA EMA(ISeries<double> input, int period)
	{
		if (cacheEMA != null)
		{
			for (int i = 0; i < cacheEMA.Length; i++)
			{
				if (cacheEMA[i] != null && cacheEMA[i].Period == period && ((NinjaScriptBase)cacheEMA[i]).EqualsInput(input))
				{
					return cacheEMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<EMA>(new EMA
		{
			Period = period
		}, input, ref cacheEMA);
	}

	public FibonacciPivots FibonacciPivots(PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		return FibonacciPivots(((NinjaScriptBase)this).Input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
	}

	public FibonacciPivots FibonacciPivots(ISeries<double> input, PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		if (cacheFibonacciPivots != null)
		{
			for (int i = 0; i < cacheFibonacciPivots.Length; i++)
			{
				if (cacheFibonacciPivots[i] != null && cacheFibonacciPivots[i].PivotRangeType == pivotRangeType && cacheFibonacciPivots[i].PriorDayHlc == priorDayHlc && cacheFibonacciPivots[i].UserDefinedClose == userDefinedClose && cacheFibonacciPivots[i].UserDefinedHigh == userDefinedHigh && cacheFibonacciPivots[i].UserDefinedLow == userDefinedLow && cacheFibonacciPivots[i].Width == width && ((NinjaScriptBase)cacheFibonacciPivots[i]).EqualsInput(input))
				{
					return cacheFibonacciPivots[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<FibonacciPivots>(new FibonacciPivots
		{
			PivotRangeType = pivotRangeType,
			PriorDayHlc = priorDayHlc,
			UserDefinedClose = userDefinedClose,
			UserDefinedHigh = userDefinedHigh,
			UserDefinedLow = userDefinedLow,
			Width = width
		}, input, ref cacheFibonacciPivots);
	}

	public FisherTransform FisherTransform(int period)
	{
		return FisherTransform(((NinjaScriptBase)this).Input, period);
	}

	public FisherTransform FisherTransform(ISeries<double> input, int period)
	{
		if (cacheFisherTransform != null)
		{
			for (int i = 0; i < cacheFisherTransform.Length; i++)
			{
				if (cacheFisherTransform[i] != null && cacheFisherTransform[i].Period == period && ((NinjaScriptBase)cacheFisherTransform[i]).EqualsInput(input))
				{
					return cacheFisherTransform[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<FisherTransform>(new FisherTransform
		{
			Period = period
		}, input, ref cacheFisherTransform);
	}

	public FOSC FOSC(int period)
	{
		return FOSC(((NinjaScriptBase)this).Input, period);
	}

	public FOSC FOSC(ISeries<double> input, int period)
	{
		if (cacheFOSC != null)
		{
			for (int i = 0; i < cacheFOSC.Length; i++)
			{
				if (cacheFOSC[i] != null && cacheFOSC[i].Period == period && ((NinjaScriptBase)cacheFOSC[i]).EqualsInput(input))
				{
					return cacheFOSC[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<FOSC>(new FOSC
		{
			Period = period
		}, input, ref cacheFOSC);
	}

	public FVG FVG()
	{
		return FVG(((NinjaScriptBase)this).Input);
	}

	public FVG FVG(ISeries<double> input)
	{
		if (cacheFVG != null)
		{
			for (int i = 0; i < cacheFVG.Length; i++)
			{
				if (cacheFVG[i] != null && ((NinjaScriptBase)cacheFVG[i]).EqualsInput(input))
				{
					return cacheFVG[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<FVG>(new FVG(), input, ref cacheFVG);
	}

	public HMA HMA(int period)
	{
		return HMA(((NinjaScriptBase)this).Input, period);
	}

	public HMA HMA(ISeries<double> input, int period)
	{
		if (cacheHMA != null)
		{
			for (int i = 0; i < cacheHMA.Length; i++)
			{
				if (cacheHMA[i] != null && cacheHMA[i].Period == period && ((NinjaScriptBase)cacheHMA[i]).EqualsInput(input))
				{
					return cacheHMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<HMA>(new HMA
		{
			Period = period
		}, input, ref cacheHMA);
	}

	public IchimokuCloud IchimokuCloud(int conversionPeriod, int basePeriod, int leadingSpanBPeriod, int spanDisplacement, int laggingDisplacement)
	{
		return IchimokuCloud(((NinjaScriptBase)this).Input, conversionPeriod, basePeriod, leadingSpanBPeriod, spanDisplacement, laggingDisplacement);
	}

	public IchimokuCloud IchimokuCloud(ISeries<double> input, int conversionPeriod, int basePeriod, int leadingSpanBPeriod, int spanDisplacement, int laggingDisplacement)
	{
		if (cacheIchimokuCloud != null)
		{
			for (int i = 0; i < cacheIchimokuCloud.Length; i++)
			{
				if (cacheIchimokuCloud[i] != null && cacheIchimokuCloud[i].ConversionPeriod == conversionPeriod && cacheIchimokuCloud[i].BasePeriod == basePeriod && cacheIchimokuCloud[i].LeadingSpanBPeriod == leadingSpanBPeriod && cacheIchimokuCloud[i].SpanDisplacement == spanDisplacement && cacheIchimokuCloud[i].LaggingDisplacement == laggingDisplacement && ((NinjaScriptBase)cacheIchimokuCloud[i]).EqualsInput(input))
				{
					return cacheIchimokuCloud[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<IchimokuCloud>(new IchimokuCloud
		{
			ConversionPeriod = conversionPeriod,
			BasePeriod = basePeriod,
			LeadingSpanBPeriod = leadingSpanBPeriod,
			SpanDisplacement = spanDisplacement,
			LaggingDisplacement = laggingDisplacement
		}, input, ref cacheIchimokuCloud);
	}

	public KAMA KAMA(int fast, int period, int slow)
	{
		return KAMA(((NinjaScriptBase)this).Input, fast, period, slow);
	}

	public KAMA KAMA(ISeries<double> input, int fast, int period, int slow)
	{
		if (cacheKAMA != null)
		{
			for (int i = 0; i < cacheKAMA.Length; i++)
			{
				if (cacheKAMA[i] != null && cacheKAMA[i].Fast == fast && cacheKAMA[i].Period == period && cacheKAMA[i].Slow == slow && ((NinjaScriptBase)cacheKAMA[i]).EqualsInput(input))
				{
					return cacheKAMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<KAMA>(new KAMA
		{
			Fast = fast,
			Period = period,
			Slow = slow
		}, input, ref cacheKAMA);
	}

	public KeltnerChannel KeltnerChannel(double offsetMultiplier, int period)
	{
		return KeltnerChannel(((NinjaScriptBase)this).Input, offsetMultiplier, period);
	}

	public KeltnerChannel KeltnerChannel(ISeries<double> input, double offsetMultiplier, int period)
	{
		if (cacheKeltnerChannel != null)
		{
			for (int i = 0; i < cacheKeltnerChannel.Length; i++)
			{
				if (cacheKeltnerChannel[i] != null && cacheKeltnerChannel[i].OffsetMultiplier == offsetMultiplier && cacheKeltnerChannel[i].Period == period && ((NinjaScriptBase)cacheKeltnerChannel[i]).EqualsInput(input))
				{
					return cacheKeltnerChannel[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<KeltnerChannel>(new KeltnerChannel
		{
			OffsetMultiplier = offsetMultiplier,
			Period = period
		}, input, ref cacheKeltnerChannel);
	}

	public KeyReversalDown KeyReversalDown(int period)
	{
		return KeyReversalDown(((NinjaScriptBase)this).Input, period);
	}

	public KeyReversalDown KeyReversalDown(ISeries<double> input, int period)
	{
		if (cacheKeyReversalDown != null)
		{
			for (int i = 0; i < cacheKeyReversalDown.Length; i++)
			{
				if (cacheKeyReversalDown[i] != null && cacheKeyReversalDown[i].Period == period && ((NinjaScriptBase)cacheKeyReversalDown[i]).EqualsInput(input))
				{
					return cacheKeyReversalDown[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<KeyReversalDown>(new KeyReversalDown
		{
			Period = period
		}, input, ref cacheKeyReversalDown);
	}

	public KeyReversalUp KeyReversalUp(int period)
	{
		return KeyReversalUp(((NinjaScriptBase)this).Input, period);
	}

	public KeyReversalUp KeyReversalUp(ISeries<double> input, int period)
	{
		if (cacheKeyReversalUp != null)
		{
			for (int i = 0; i < cacheKeyReversalUp.Length; i++)
			{
				if (cacheKeyReversalUp[i] != null && cacheKeyReversalUp[i].Period == period && ((NinjaScriptBase)cacheKeyReversalUp[i]).EqualsInput(input))
				{
					return cacheKeyReversalUp[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<KeyReversalUp>(new KeyReversalUp
		{
			Period = period
		}, input, ref cacheKeyReversalUp);
	}

	public LinReg LinReg(int period)
	{
		return LinReg(((NinjaScriptBase)this).Input, period);
	}

	public LinReg LinReg(ISeries<double> input, int period)
	{
		if (cacheLinReg != null)
		{
			for (int i = 0; i < cacheLinReg.Length; i++)
			{
				if (cacheLinReg[i] != null && cacheLinReg[i].Period == period && ((NinjaScriptBase)cacheLinReg[i]).EqualsInput(input))
				{
					return cacheLinReg[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<LinReg>(new LinReg
		{
			Period = period
		}, input, ref cacheLinReg);
	}

	public LinRegIntercept LinRegIntercept(int period)
	{
		return LinRegIntercept(((NinjaScriptBase)this).Input, period);
	}

	public LinRegIntercept LinRegIntercept(ISeries<double> input, int period)
	{
		if (cacheLinRegIntercept != null)
		{
			for (int i = 0; i < cacheLinRegIntercept.Length; i++)
			{
				if (cacheLinRegIntercept[i] != null && cacheLinRegIntercept[i].Period == period && ((NinjaScriptBase)cacheLinRegIntercept[i]).EqualsInput(input))
				{
					return cacheLinRegIntercept[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<LinRegIntercept>(new LinRegIntercept
		{
			Period = period
		}, input, ref cacheLinRegIntercept);
	}

	public LinRegSlope LinRegSlope(int period)
	{
		return LinRegSlope(((NinjaScriptBase)this).Input, period);
	}

	public LinRegSlope LinRegSlope(ISeries<double> input, int period)
	{
		if (cacheLinRegSlope != null)
		{
			for (int i = 0; i < cacheLinRegSlope.Length; i++)
			{
				if (cacheLinRegSlope[i] != null && cacheLinRegSlope[i].Period == period && ((NinjaScriptBase)cacheLinRegSlope[i]).EqualsInput(input))
				{
					return cacheLinRegSlope[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<LinRegSlope>(new LinRegSlope
		{
			Period = period
		}, input, ref cacheLinRegSlope);
	}

	public MACD MACD(int fast, int slow, int smooth)
	{
		return MACD(((NinjaScriptBase)this).Input, fast, slow, smooth);
	}

	public MACD MACD(ISeries<double> input, int fast, int slow, int smooth)
	{
		if (cacheMACD != null)
		{
			for (int i = 0; i < cacheMACD.Length; i++)
			{
				if (cacheMACD[i] != null && cacheMACD[i].Fast == fast && cacheMACD[i].Slow == slow && cacheMACD[i].Smooth == smooth && ((NinjaScriptBase)cacheMACD[i]).EqualsInput(input))
				{
					return cacheMACD[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<MACD>(new MACD
		{
			Fast = fast,
			Slow = slow,
			Smooth = smooth
		}, input, ref cacheMACD);
	}

	public MAEnvelopes MAEnvelopes(double envelopePercentage, int mAType, int period)
	{
		return MAEnvelopes(((NinjaScriptBase)this).Input, envelopePercentage, mAType, period);
	}

	public MAEnvelopes MAEnvelopes(ISeries<double> input, double envelopePercentage, int mAType, int period)
	{
		if (cacheMAEnvelopes != null)
		{
			for (int i = 0; i < cacheMAEnvelopes.Length; i++)
			{
				if (cacheMAEnvelopes[i] != null && cacheMAEnvelopes[i].EnvelopePercentage == envelopePercentage && cacheMAEnvelopes[i].MAType == mAType && cacheMAEnvelopes[i].Period == period && ((NinjaScriptBase)cacheMAEnvelopes[i]).EqualsInput(input))
				{
					return cacheMAEnvelopes[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<MAEnvelopes>(new MAEnvelopes
		{
			EnvelopePercentage = envelopePercentage,
			MAType = mAType,
			Period = period
		}, input, ref cacheMAEnvelopes);
	}

	public MAMA MAMA(double fastLimit, double slowLimit)
	{
		return MAMA(((NinjaScriptBase)this).Input, fastLimit, slowLimit);
	}

	public MAMA MAMA(ISeries<double> input, double fastLimit, double slowLimit)
	{
		if (cacheMAMA != null)
		{
			for (int i = 0; i < cacheMAMA.Length; i++)
			{
				if (cacheMAMA[i] != null && cacheMAMA[i].FastLimit == fastLimit && cacheMAMA[i].SlowLimit == slowLimit && ((NinjaScriptBase)cacheMAMA[i]).EqualsInput(input))
				{
					return cacheMAMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<MAMA>(new MAMA
		{
			FastLimit = fastLimit,
			SlowLimit = slowLimit
		}, input, ref cacheMAMA);
	}

	public MAX MAX(int period)
	{
		return MAX(((NinjaScriptBase)this).Input, period);
	}

	public MAX MAX(ISeries<double> input, int period)
	{
		if (cacheMAX != null)
		{
			for (int i = 0; i < cacheMAX.Length; i++)
			{
				if (cacheMAX[i] != null && cacheMAX[i].Period == period && ((NinjaScriptBase)cacheMAX[i]).EqualsInput(input))
				{
					return cacheMAX[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<MAX>(new MAX
		{
			Period = period
		}, input, ref cacheMAX);
	}

	public McClellanOscillator McClellanOscillator(int fastPeriod, int slowPeriod)
	{
		return McClellanOscillator(((NinjaScriptBase)this).Input, fastPeriod, slowPeriod);
	}

	public McClellanOscillator McClellanOscillator(ISeries<double> input, int fastPeriod, int slowPeriod)
	{
		if (cacheMcClellanOscillator != null)
		{
			for (int i = 0; i < cacheMcClellanOscillator.Length; i++)
			{
				if (cacheMcClellanOscillator[i] != null && cacheMcClellanOscillator[i].FastPeriod == fastPeriod && cacheMcClellanOscillator[i].SlowPeriod == slowPeriod && ((NinjaScriptBase)cacheMcClellanOscillator[i]).EqualsInput(input))
				{
					return cacheMcClellanOscillator[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<McClellanOscillator>(new McClellanOscillator
		{
			FastPeriod = fastPeriod,
			SlowPeriod = slowPeriod
		}, input, ref cacheMcClellanOscillator);
	}

	public MFI MFI(int period)
	{
		return MFI(((NinjaScriptBase)this).Input, period);
	}

	public MFI MFI(ISeries<double> input, int period)
	{
		if (cacheMFI != null)
		{
			for (int i = 0; i < cacheMFI.Length; i++)
			{
				if (cacheMFI[i] != null && cacheMFI[i].Period == period && ((NinjaScriptBase)cacheMFI[i]).EqualsInput(input))
				{
					return cacheMFI[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<MFI>(new MFI
		{
			Period = period
		}, input, ref cacheMFI);
	}

	public MIN MIN(int period)
	{
		return MIN(((NinjaScriptBase)this).Input, period);
	}

	public MIN MIN(ISeries<double> input, int period)
	{
		if (cacheMIN != null)
		{
			for (int i = 0; i < cacheMIN.Length; i++)
			{
				if (cacheMIN[i] != null && cacheMIN[i].Period == period && ((NinjaScriptBase)cacheMIN[i]).EqualsInput(input))
				{
					return cacheMIN[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<MIN>(new MIN
		{
			Period = period
		}, input, ref cacheMIN);
	}

	public Momentum Momentum(int period)
	{
		return Momentum(((NinjaScriptBase)this).Input, period);
	}

	public Momentum Momentum(ISeries<double> input, int period)
	{
		if (cacheMomentum != null)
		{
			for (int i = 0; i < cacheMomentum.Length; i++)
			{
				if (cacheMomentum[i] != null && cacheMomentum[i].Period == period && ((NinjaScriptBase)cacheMomentum[i]).EqualsInput(input))
				{
					return cacheMomentum[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<Momentum>(new Momentum
		{
			Period = period
		}, input, ref cacheMomentum);
	}

	public MoneyFlowOscillator MoneyFlowOscillator(int period)
	{
		return MoneyFlowOscillator(((NinjaScriptBase)this).Input, period);
	}

	public MoneyFlowOscillator MoneyFlowOscillator(ISeries<double> input, int period)
	{
		if (cacheMoneyFlowOscillator != null)
		{
			for (int i = 0; i < cacheMoneyFlowOscillator.Length; i++)
			{
				if (cacheMoneyFlowOscillator[i] != null && cacheMoneyFlowOscillator[i].Period == period && ((NinjaScriptBase)cacheMoneyFlowOscillator[i]).EqualsInput(input))
				{
					return cacheMoneyFlowOscillator[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<MoneyFlowOscillator>(new MoneyFlowOscillator
		{
			Period = period
		}, input, ref cacheMoneyFlowOscillator);
	}

	public MovingAverageRibbon MovingAverageRibbon(RibbonMAType movingAverage, int basePeriod, int incrementalPeriod)
	{
		return MovingAverageRibbon(((NinjaScriptBase)this).Input, movingAverage, basePeriod, incrementalPeriod);
	}

	public MovingAverageRibbon MovingAverageRibbon(ISeries<double> input, RibbonMAType movingAverage, int basePeriod, int incrementalPeriod)
	{
		if (cacheMovingAverageRibbon != null)
		{
			for (int i = 0; i < cacheMovingAverageRibbon.Length; i++)
			{
				if (cacheMovingAverageRibbon[i] != null && cacheMovingAverageRibbon[i].MovingAverage == movingAverage && cacheMovingAverageRibbon[i].BasePeriod == basePeriod && cacheMovingAverageRibbon[i].IncrementalPeriod == incrementalPeriod && ((NinjaScriptBase)cacheMovingAverageRibbon[i]).EqualsInput(input))
				{
					return cacheMovingAverageRibbon[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<MovingAverageRibbon>(new MovingAverageRibbon
		{
			MovingAverage = movingAverage,
			BasePeriod = basePeriod,
			IncrementalPeriod = incrementalPeriod
		}, input, ref cacheMovingAverageRibbon);
	}

	public NBarsDown NBarsDown(int barCount, bool barDown, bool lowerHigh, bool lowerLow)
	{
		return NBarsDown(((NinjaScriptBase)this).Input, barCount, barDown, lowerHigh, lowerLow);
	}

	public NBarsDown NBarsDown(ISeries<double> input, int barCount, bool barDown, bool lowerHigh, bool lowerLow)
	{
		if (cacheNBarsDown != null)
		{
			for (int i = 0; i < cacheNBarsDown.Length; i++)
			{
				if (cacheNBarsDown[i] != null && cacheNBarsDown[i].BarCount == barCount && cacheNBarsDown[i].BarDown == barDown && cacheNBarsDown[i].LowerHigh == lowerHigh && cacheNBarsDown[i].LowerLow == lowerLow && ((NinjaScriptBase)cacheNBarsDown[i]).EqualsInput(input))
				{
					return cacheNBarsDown[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<NBarsDown>(new NBarsDown
		{
			BarCount = barCount,
			BarDown = barDown,
			LowerHigh = lowerHigh,
			LowerLow = lowerLow
		}, input, ref cacheNBarsDown);
	}

	public NBarsUp NBarsUp(int barCount, bool barUp, bool higherHigh, bool higherLow)
	{
		return NBarsUp(((NinjaScriptBase)this).Input, barCount, barUp, higherHigh, higherLow);
	}

	public NBarsUp NBarsUp(ISeries<double> input, int barCount, bool barUp, bool higherHigh, bool higherLow)
	{
		if (cacheNBarsUp != null)
		{
			for (int i = 0; i < cacheNBarsUp.Length; i++)
			{
				if (cacheNBarsUp[i] != null && cacheNBarsUp[i].BarCount == barCount && cacheNBarsUp[i].BarUp == barUp && cacheNBarsUp[i].HigherHigh == higherHigh && cacheNBarsUp[i].HigherLow == higherLow && ((NinjaScriptBase)cacheNBarsUp[i]).EqualsInput(input))
				{
					return cacheNBarsUp[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<NBarsUp>(new NBarsUp
		{
			BarCount = barCount,
			BarUp = barUp,
			HigherHigh = higherHigh,
			HigherLow = higherLow
		}, input, ref cacheNBarsUp);
	}

	public NetChangeDisplay NetChangeDisplay(PerformanceUnit unit, NetChangePosition location)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return NetChangeDisplay(((NinjaScriptBase)this).Input, unit, location);
	}

	public NetChangeDisplay NetChangeDisplay(ISeries<double> input, PerformanceUnit unit, NetChangePosition location)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (cacheNetChangeDisplay != null)
		{
			for (int i = 0; i < cacheNetChangeDisplay.Length; i++)
			{
				if (cacheNetChangeDisplay[i] != null && cacheNetChangeDisplay[i].Unit == unit && cacheNetChangeDisplay[i].Location == location && ((NinjaScriptBase)cacheNetChangeDisplay[i]).EqualsInput(input))
				{
					return cacheNetChangeDisplay[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<NetChangeDisplay>(new NetChangeDisplay
		{
			Unit = unit,
			Location = location
		}, input, ref cacheNetChangeDisplay);
	}

	public OBV OBV()
	{
		return OBV(((NinjaScriptBase)this).Input);
	}

	public OBV OBV(ISeries<double> input)
	{
		if (cacheOBV != null)
		{
			for (int i = 0; i < cacheOBV.Length; i++)
			{
				if (cacheOBV[i] != null && ((NinjaScriptBase)cacheOBV[i]).EqualsInput(input))
				{
					return cacheOBV[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OBV>(new OBV(), input, ref cacheOBV);
	}

	public ParabolicSAR ParabolicSAR(double acceleration, double accelerationMax, double accelerationStep)
	{
		return ParabolicSAR(((NinjaScriptBase)this).Input, acceleration, accelerationMax, accelerationStep);
	}

	public ParabolicSAR ParabolicSAR(ISeries<double> input, double acceleration, double accelerationMax, double accelerationStep)
	{
		if (cacheParabolicSAR != null)
		{
			for (int i = 0; i < cacheParabolicSAR.Length; i++)
			{
				if (cacheParabolicSAR[i] != null && cacheParabolicSAR[i].Acceleration == acceleration && cacheParabolicSAR[i].AccelerationMax == accelerationMax && cacheParabolicSAR[i].AccelerationStep == accelerationStep && ((NinjaScriptBase)cacheParabolicSAR[i]).EqualsInput(input))
				{
					return cacheParabolicSAR[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ParabolicSAR>(new ParabolicSAR
		{
			Acceleration = acceleration,
			AccelerationMax = accelerationMax,
			AccelerationStep = accelerationStep
		}, input, ref cacheParabolicSAR);
	}

	public PFE PFE(int period, int smooth)
	{
		return PFE(((NinjaScriptBase)this).Input, period, smooth);
	}

	public PFE PFE(ISeries<double> input, int period, int smooth)
	{
		if (cachePFE != null)
		{
			for (int i = 0; i < cachePFE.Length; i++)
			{
				if (cachePFE[i] != null && cachePFE[i].Period == period && cachePFE[i].Smooth == smooth && ((NinjaScriptBase)cachePFE[i]).EqualsInput(input))
				{
					return cachePFE[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PFE>(new PFE
		{
			Period = period,
			Smooth = smooth
		}, input, ref cachePFE);
	}

	public Pivots Pivots(PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		return Pivots(((NinjaScriptBase)this).Input, pivotRangeType, priorDayHlc, userDefinedClose, userDefinedHigh, userDefinedLow, width);
	}

	public Pivots Pivots(ISeries<double> input, PivotRange pivotRangeType, HLCCalculationMode priorDayHlc, double userDefinedClose, double userDefinedHigh, double userDefinedLow, int width)
	{
		if (cachePivots != null)
		{
			for (int i = 0; i < cachePivots.Length; i++)
			{
				if (cachePivots[i] != null && cachePivots[i].PivotRangeType == pivotRangeType && cachePivots[i].PriorDayHlc == priorDayHlc && cachePivots[i].UserDefinedClose == userDefinedClose && cachePivots[i].UserDefinedHigh == userDefinedHigh && cachePivots[i].UserDefinedLow == userDefinedLow && cachePivots[i].Width == width && ((NinjaScriptBase)cachePivots[i]).EqualsInput(input))
				{
					return cachePivots[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<Pivots>(new Pivots
		{
			PivotRangeType = pivotRangeType,
			PriorDayHlc = priorDayHlc,
			UserDefinedClose = userDefinedClose,
			UserDefinedHigh = userDefinedHigh,
			UserDefinedLow = userDefinedLow,
			Width = width
		}, input, ref cachePivots);
	}

	public PPO PPO(int fast, int slow, int smooth)
	{
		return PPO(((NinjaScriptBase)this).Input, fast, slow, smooth);
	}

	public PPO PPO(ISeries<double> input, int fast, int slow, int smooth)
	{
		if (cachePPO != null)
		{
			for (int i = 0; i < cachePPO.Length; i++)
			{
				if (cachePPO[i] != null && cachePPO[i].Fast == fast && cachePPO[i].Slow == slow && cachePPO[i].Smooth == smooth && ((NinjaScriptBase)cachePPO[i]).EqualsInput(input))
				{
					return cachePPO[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PPO>(new PPO
		{
			Fast = fast,
			Slow = slow,
			Smooth = smooth
		}, input, ref cachePPO);
	}

	public PriceLine PriceLine(bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
	{
		return PriceLine(((NinjaScriptBase)this).Input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
	}

	public PriceLine PriceLine(ISeries<double> input, bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
	{
		if (cachePriceLine != null)
		{
			for (int i = 0; i < cachePriceLine.Length; i++)
			{
				if (cachePriceLine[i] != null && cachePriceLine[i].ShowAskLine == showAskLine && cachePriceLine[i].ShowBidLine == showBidLine && cachePriceLine[i].ShowLastLine == showLastLine && cachePriceLine[i].AskLineLength == askLineLength && cachePriceLine[i].BidLineLength == bidLineLength && cachePriceLine[i].LastLineLength == lastLineLength && ((NinjaScriptBase)cachePriceLine[i]).EqualsInput(input))
				{
					return cachePriceLine[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PriceLine>(new PriceLine
		{
			ShowAskLine = showAskLine,
			ShowBidLine = showBidLine,
			ShowLastLine = showLastLine,
			AskLineLength = askLineLength,
			BidLineLength = bidLineLength,
			LastLineLength = lastLineLength
		}, input, ref cachePriceLine);
	}

	public PriceOscillator PriceOscillator(int fast, int slow, int smooth)
	{
		return PriceOscillator(((NinjaScriptBase)this).Input, fast, slow, smooth);
	}

	public PriceOscillator PriceOscillator(ISeries<double> input, int fast, int slow, int smooth)
	{
		if (cachePriceOscillator != null)
		{
			for (int i = 0; i < cachePriceOscillator.Length; i++)
			{
				if (cachePriceOscillator[i] != null && cachePriceOscillator[i].Fast == fast && cachePriceOscillator[i].Slow == slow && cachePriceOscillator[i].Smooth == smooth && ((NinjaScriptBase)cachePriceOscillator[i]).EqualsInput(input))
				{
					return cachePriceOscillator[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PriceOscillator>(new PriceOscillator
		{
			Fast = fast,
			Slow = slow,
			Smooth = smooth
		}, input, ref cachePriceOscillator);
	}

	public PriorDayOHLC PriorDayOHLC()
	{
		return PriorDayOHLC(((NinjaScriptBase)this).Input);
	}

	public PriorDayOHLC PriorDayOHLC(ISeries<double> input)
	{
		if (cachePriorDayOHLC != null)
		{
			for (int i = 0; i < cachePriorDayOHLC.Length; i++)
			{
				if (cachePriorDayOHLC[i] != null && ((NinjaScriptBase)cachePriorDayOHLC[i]).EqualsInput(input))
				{
					return cachePriorDayOHLC[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PriorDayOHLC>(new PriorDayOHLC(), input, ref cachePriorDayOHLC);
	}

	public PsychologicalLine PsychologicalLine(int period)
	{
		return PsychologicalLine(((NinjaScriptBase)this).Input, period);
	}

	public PsychologicalLine PsychologicalLine(ISeries<double> input, int period)
	{
		if (cachePsychologicalLine != null)
		{
			for (int i = 0; i < cachePsychologicalLine.Length; i++)
			{
				if (cachePsychologicalLine[i] != null && cachePsychologicalLine[i].Period == period && ((NinjaScriptBase)cachePsychologicalLine[i]).EqualsInput(input))
				{
					return cachePsychologicalLine[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PsychologicalLine>(new PsychologicalLine
		{
			Period = period
		}, input, ref cachePsychologicalLine);
	}

	public Range Range()
	{
		return Range(((NinjaScriptBase)this).Input);
	}

	public Range Range(ISeries<double> input)
	{
		if (cacheRange != null)
		{
			for (int i = 0; i < cacheRange.Length; i++)
			{
				if (cacheRange[i] != null && ((NinjaScriptBase)cacheRange[i]).EqualsInput(input))
				{
					return cacheRange[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<Range>(new Range(), input, ref cacheRange);
	}

	public RangeCounter RangeCounter(bool countDown)
	{
		return RangeCounter(((NinjaScriptBase)this).Input, countDown);
	}

	public RangeCounter RangeCounter(bool countDown, TextPositionFine textPositionFine)
	{
		return RangeCounter(((NinjaScriptBase)this).Input, countDown, textPositionFine);
	}

	public RangeCounter RangeCounter(ISeries<double> input, bool countDown)
	{
		return RangeCounter(input, countDown, TextPositionFine.BottomRight);
	}

	public RangeCounter RangeCounter(ISeries<double> input, bool countDown, TextPositionFine textPositionFine)
	{
		if (cacheRangeCounter != null)
		{
			for (int i = 0; i < cacheRangeCounter.Length; i++)
			{
				if (cacheRangeCounter[i] != null && cacheRangeCounter[i].CountDown == countDown && cacheRangeCounter[i].TextPositionFine == textPositionFine && ((NinjaScriptBase)cacheRangeCounter[i]).EqualsInput(input))
				{
					return cacheRangeCounter[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<RangeCounter>(new RangeCounter
		{
			CountDown = countDown
		}, input, ref cacheRangeCounter);
	}

	public RegressionChannel RegressionChannel(int period, double width)
	{
		return RegressionChannel(((NinjaScriptBase)this).Input, period, width);
	}

	public RegressionChannel RegressionChannel(ISeries<double> input, int period, double width)
	{
		if (cacheRegressionChannel != null)
		{
			for (int i = 0; i < cacheRegressionChannel.Length; i++)
			{
				if (cacheRegressionChannel[i] != null && cacheRegressionChannel[i].Period == period && cacheRegressionChannel[i].Width == width && ((NinjaScriptBase)cacheRegressionChannel[i]).EqualsInput(input))
				{
					return cacheRegressionChannel[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<RegressionChannel>(new RegressionChannel
		{
			Period = period,
			Width = width
		}, input, ref cacheRegressionChannel);
	}

	public RelativeVigorIndex RelativeVigorIndex(int period)
	{
		return RelativeVigorIndex(((NinjaScriptBase)this).Input, period);
	}

	public RelativeVigorIndex RelativeVigorIndex(ISeries<double> input, int period)
	{
		if (cacheRelativeVigorIndex != null)
		{
			for (int i = 0; i < cacheRelativeVigorIndex.Length; i++)
			{
				if (cacheRelativeVigorIndex[i] != null && cacheRelativeVigorIndex[i].Period == period && ((NinjaScriptBase)cacheRelativeVigorIndex[i]).EqualsInput(input))
				{
					return cacheRelativeVigorIndex[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<RelativeVigorIndex>(new RelativeVigorIndex
		{
			Period = period
		}, input, ref cacheRelativeVigorIndex);
	}

	public RIND RIND(int periodQ, int smooth)
	{
		return RIND(((NinjaScriptBase)this).Input, periodQ, smooth);
	}

	public RIND RIND(ISeries<double> input, int periodQ, int smooth)
	{
		if (cacheRIND != null)
		{
			for (int i = 0; i < cacheRIND.Length; i++)
			{
				if (cacheRIND[i] != null && cacheRIND[i].PeriodQ == periodQ && cacheRIND[i].Smooth == smooth && ((NinjaScriptBase)cacheRIND[i]).EqualsInput(input))
				{
					return cacheRIND[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<RIND>(new RIND
		{
			PeriodQ = periodQ,
			Smooth = smooth
		}, input, ref cacheRIND);
	}

	public ROC ROC(int period)
	{
		return ROC(((NinjaScriptBase)this).Input, period);
	}

	public ROC ROC(ISeries<double> input, int period)
	{
		if (cacheROC != null)
		{
			for (int i = 0; i < cacheROC.Length; i++)
			{
				if (cacheROC[i] != null && cacheROC[i].Period == period && ((NinjaScriptBase)cacheROC[i]).EqualsInput(input))
				{
					return cacheROC[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ROC>(new ROC
		{
			Period = period
		}, input, ref cacheROC);
	}

	public RSI RSI(int period, int smooth)
	{
		return RSI(((NinjaScriptBase)this).Input, period, smooth);
	}

	public RSI RSI(ISeries<double> input, int period, int smooth)
	{
		if (cacheRSI != null)
		{
			for (int i = 0; i < cacheRSI.Length; i++)
			{
				if (cacheRSI[i] != null && cacheRSI[i].Period == period && cacheRSI[i].Smooth == smooth && ((NinjaScriptBase)cacheRSI[i]).EqualsInput(input))
				{
					return cacheRSI[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<RSI>(new RSI
		{
			Period = period,
			Smooth = smooth
		}, input, ref cacheRSI);
	}

	public RSquared RSquared(int period)
	{
		return RSquared(((NinjaScriptBase)this).Input, period);
	}

	public RSquared RSquared(ISeries<double> input, int period)
	{
		if (cacheRSquared != null)
		{
			for (int i = 0; i < cacheRSquared.Length; i++)
			{
				if (cacheRSquared[i] != null && cacheRSquared[i].Period == period && ((NinjaScriptBase)cacheRSquared[i]).EqualsInput(input))
				{
					return cacheRSquared[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<RSquared>(new RSquared
		{
			Period = period
		}, input, ref cacheRSquared);
	}

	public RSS RSS(int eMA1, int eMA2, int length)
	{
		return RSS(((NinjaScriptBase)this).Input, eMA1, eMA2, length);
	}

	public RSS RSS(ISeries<double> input, int eMA1, int eMA2, int length)
	{
		if (cacheRSS != null)
		{
			for (int i = 0; i < cacheRSS.Length; i++)
			{
				if (cacheRSS[i] != null && cacheRSS[i].EMA1 == eMA1 && cacheRSS[i].EMA2 == eMA2 && cacheRSS[i].Length == length && ((NinjaScriptBase)cacheRSS[i]).EqualsInput(input))
				{
					return cacheRSS[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<RSS>(new RSS
		{
			EMA1 = eMA1,
			EMA2 = eMA2,
			Length = length
		}, input, ref cacheRSS);
	}

	public RVI RVI(int period)
	{
		return RVI(((NinjaScriptBase)this).Input, period);
	}

	public RVI RVI(ISeries<double> input, int period)
	{
		if (cacheRVI != null)
		{
			for (int i = 0; i < cacheRVI.Length; i++)
			{
				if (cacheRVI[i] != null && cacheRVI[i].Period == period && ((NinjaScriptBase)cacheRVI[i]).EqualsInput(input))
				{
					return cacheRVI[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<RVI>(new RVI
		{
			Period = period
		}, input, ref cacheRVI);
	}

	public SampleCustomRender SampleCustomRender()
	{
		return SampleCustomRender(((NinjaScriptBase)this).Input);
	}

	public SampleCustomRender SampleCustomRender(ISeries<double> input)
	{
		if (cacheSampleCustomRender != null)
		{
			for (int i = 0; i < cacheSampleCustomRender.Length; i++)
			{
				if (cacheSampleCustomRender[i] != null && ((NinjaScriptBase)cacheSampleCustomRender[i]).EqualsInput(input))
				{
					return cacheSampleCustomRender[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<SampleCustomRender>(new SampleCustomRender(), input, ref cacheSampleCustomRender);
	}

	public SMA SMA(int period)
	{
		return SMA(((NinjaScriptBase)this).Input, period);
	}

	public SMA SMA(ISeries<double> input, int period)
	{
		if (cacheSMA != null)
		{
			for (int i = 0; i < cacheSMA.Length; i++)
			{
				if (cacheSMA[i] != null && cacheSMA[i].Period == period && ((NinjaScriptBase)cacheSMA[i]).EqualsInput(input))
				{
					return cacheSMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<SMA>(new SMA
		{
			Period = period
		}, input, ref cacheSMA);
	}

	public StdDev StdDev(int period)
	{
		return StdDev(((NinjaScriptBase)this).Input, period);
	}

	public StdDev StdDev(ISeries<double> input, int period)
	{
		if (cacheStdDev != null)
		{
			for (int i = 0; i < cacheStdDev.Length; i++)
			{
				if (cacheStdDev[i] != null && cacheStdDev[i].Period == period && ((NinjaScriptBase)cacheStdDev[i]).EqualsInput(input))
				{
					return cacheStdDev[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<StdDev>(new StdDev
		{
			Period = period
		}, input, ref cacheStdDev);
	}

	public StdError StdError(int period)
	{
		return StdError(((NinjaScriptBase)this).Input, period);
	}

	public StdError StdError(ISeries<double> input, int period)
	{
		if (cacheStdError != null)
		{
			for (int i = 0; i < cacheStdError.Length; i++)
			{
				if (cacheStdError[i] != null && cacheStdError[i].Period == period && ((NinjaScriptBase)cacheStdError[i]).EqualsInput(input))
				{
					return cacheStdError[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<StdError>(new StdError
		{
			Period = period
		}, input, ref cacheStdError);
	}

	public Stochastics Stochastics(int periodD, int periodK, int smooth)
	{
		return Stochastics(((NinjaScriptBase)this).Input, periodD, periodK, smooth);
	}

	public Stochastics Stochastics(ISeries<double> input, int periodD, int periodK, int smooth)
	{
		if (cacheStochastics != null)
		{
			for (int i = 0; i < cacheStochastics.Length; i++)
			{
				if (cacheStochastics[i] != null && cacheStochastics[i].PeriodD == periodD && cacheStochastics[i].PeriodK == periodK && cacheStochastics[i].Smooth == smooth && ((NinjaScriptBase)cacheStochastics[i]).EqualsInput(input))
				{
					return cacheStochastics[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<Stochastics>(new Stochastics
		{
			PeriodD = periodD,
			PeriodK = periodK,
			Smooth = smooth
		}, input, ref cacheStochastics);
	}

	public StochasticsFast StochasticsFast(int periodD, int periodK)
	{
		return StochasticsFast(((NinjaScriptBase)this).Input, periodD, periodK);
	}

	public StochasticsFast StochasticsFast(ISeries<double> input, int periodD, int periodK)
	{
		if (cacheStochasticsFast != null)
		{
			for (int i = 0; i < cacheStochasticsFast.Length; i++)
			{
				if (cacheStochasticsFast[i] != null && cacheStochasticsFast[i].PeriodD == periodD && cacheStochasticsFast[i].PeriodK == periodK && ((NinjaScriptBase)cacheStochasticsFast[i]).EqualsInput(input))
				{
					return cacheStochasticsFast[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<StochasticsFast>(new StochasticsFast
		{
			PeriodD = periodD,
			PeriodK = periodK
		}, input, ref cacheStochasticsFast);
	}

	public StochRSI StochRSI(int period)
	{
		return StochRSI(((NinjaScriptBase)this).Input, period);
	}

	public StochRSI StochRSI(ISeries<double> input, int period)
	{
		if (cacheStochRSI != null)
		{
			for (int i = 0; i < cacheStochRSI.Length; i++)
			{
				if (cacheStochRSI[i] != null && cacheStochRSI[i].Period == period && ((NinjaScriptBase)cacheStochRSI[i]).EqualsInput(input))
				{
					return cacheStochRSI[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<StochRSI>(new StochRSI
		{
			Period = period
		}, input, ref cacheStochRSI);
	}

	public SUM SUM(int period)
	{
		return SUM(((NinjaScriptBase)this).Input, period);
	}

	public SUM SUM(ISeries<double> input, int period)
	{
		if (cacheSUM != null)
		{
			for (int i = 0; i < cacheSUM.Length; i++)
			{
				if (cacheSUM[i] != null && cacheSUM[i].Period == period && ((NinjaScriptBase)cacheSUM[i]).EqualsInput(input))
				{
					return cacheSUM[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<SUM>(new SUM
		{
			Period = period
		}, input, ref cacheSUM);
	}

	public Swing Swing(int strength)
	{
		return Swing(((NinjaScriptBase)this).Input, strength);
	}

	public Swing Swing(ISeries<double> input, int strength)
	{
		if (cacheSwing != null)
		{
			for (int i = 0; i < cacheSwing.Length; i++)
			{
				if (cacheSwing[i] != null && cacheSwing[i].Strength == strength && ((NinjaScriptBase)cacheSwing[i]).EqualsInput(input))
				{
					return cacheSwing[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<Swing>(new Swing
		{
			Strength = strength
		}, input, ref cacheSwing);
	}

	public T3 T3(int period, int tCount, double vFactor)
	{
		return T3(((NinjaScriptBase)this).Input, period, tCount, vFactor);
	}

	public T3 T3(ISeries<double> input, int period, int tCount, double vFactor)
	{
		if (cacheT3 != null)
		{
			for (int i = 0; i < cacheT3.Length; i++)
			{
				if (cacheT3[i] != null && cacheT3[i].Period == period && cacheT3[i].TCount == tCount && cacheT3[i].VFactor == vFactor && ((NinjaScriptBase)cacheT3[i]).EqualsInput(input))
				{
					return cacheT3[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<T3>(new T3
		{
			Period = period,
			TCount = tCount,
			VFactor = vFactor
		}, input, ref cacheT3);
	}

	public TEMA TEMA(int period)
	{
		return TEMA(((NinjaScriptBase)this).Input, period);
	}

	public TEMA TEMA(ISeries<double> input, int period)
	{
		if (cacheTEMA != null)
		{
			for (int i = 0; i < cacheTEMA.Length; i++)
			{
				if (cacheTEMA[i] != null && cacheTEMA[i].Period == period && ((NinjaScriptBase)cacheTEMA[i]).EqualsInput(input))
				{
					return cacheTEMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<TEMA>(new TEMA
		{
			Period = period
		}, input, ref cacheTEMA);
	}

	public TickCounter TickCounter(bool countDown, bool showPercent)
	{
		return TickCounter(((NinjaScriptBase)this).Input, countDown, showPercent, TextPositionFine.BottomRight);
	}

	public TickCounter TickCounter(bool countDown, bool showPercent, TextPositionFine textPositionFine)
	{
		return TickCounter(((NinjaScriptBase)this).Input, countDown, showPercent, textPositionFine);
	}

	public TickCounter TickCounter(ISeries<double> input, bool countDown, bool showPercent)
	{
		return TickCounter(input, countDown, showPercent, TextPositionFine.BottomRight);
	}

	public TickCounter TickCounter(ISeries<double> input, bool countDown, bool showPercent, TextPositionFine textPositionFine)
	{
		if (cacheTickCounter != null)
		{
			for (int i = 0; i < cacheTickCounter.Length; i++)
			{
				if (cacheTickCounter[i] != null && cacheTickCounter[i].CountDown == countDown && cacheTickCounter[i].ShowPercent == showPercent && cacheTickCounter[i].TextPositionFine == textPositionFine && ((NinjaScriptBase)cacheTickCounter[i]).EqualsInput(input))
				{
					return cacheTickCounter[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<TickCounter>(new TickCounter
		{
			CountDown = countDown,
			ShowPercent = showPercent
		}, input, ref cacheTickCounter);
	}

	public TMA TMA(int period)
	{
		return TMA(((NinjaScriptBase)this).Input, period);
	}

	public TMA TMA(ISeries<double> input, int period)
	{
		if (cacheTMA != null)
		{
			for (int i = 0; i < cacheTMA.Length; i++)
			{
				if (cacheTMA[i] != null && cacheTMA[i].Period == period && ((NinjaScriptBase)cacheTMA[i]).EqualsInput(input))
				{
					return cacheTMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<TMA>(new TMA
		{
			Period = period
		}, input, ref cacheTMA);
	}

	public TrendLines TrendLines(int strength, int numberOfTrendLines, int oldTrendsOpacity, bool alertOnBreak)
	{
		return TrendLines(((NinjaScriptBase)this).Input, strength, numberOfTrendLines, oldTrendsOpacity, alertOnBreak);
	}

	public TrendLines TrendLines(ISeries<double> input, int strength, int numberOfTrendLines, int oldTrendsOpacity, bool alertOnBreak)
	{
		if (cacheTrendLines != null)
		{
			for (int i = 0; i < cacheTrendLines.Length; i++)
			{
				if (cacheTrendLines[i] != null && cacheTrendLines[i].Strength == strength && cacheTrendLines[i].NumberOfTrendLines == numberOfTrendLines && cacheTrendLines[i].OldTrendsOpacity == oldTrendsOpacity && cacheTrendLines[i].AlertOnBreak == alertOnBreak && ((NinjaScriptBase)cacheTrendLines[i]).EqualsInput(input))
				{
					return cacheTrendLines[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<TrendLines>(new TrendLines
		{
			Strength = strength,
			NumberOfTrendLines = numberOfTrendLines,
			OldTrendsOpacity = oldTrendsOpacity,
			AlertOnBreak = alertOnBreak
		}, input, ref cacheTrendLines);
	}

	public TRIX TRIX(int period, int signalPeriod)
	{
		return TRIX(((NinjaScriptBase)this).Input, period, signalPeriod);
	}

	public TRIX TRIX(ISeries<double> input, int period, int signalPeriod)
	{
		if (cacheTRIX != null)
		{
			for (int i = 0; i < cacheTRIX.Length; i++)
			{
				if (cacheTRIX[i] != null && cacheTRIX[i].Period == period && cacheTRIX[i].SignalPeriod == signalPeriod && ((NinjaScriptBase)cacheTRIX[i]).EqualsInput(input))
				{
					return cacheTRIX[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<TRIX>(new TRIX
		{
			Period = period,
			SignalPeriod = signalPeriod
		}, input, ref cacheTRIX);
	}

	public TSF TSF(int forecast, int period)
	{
		return TSF(((NinjaScriptBase)this).Input, forecast, period);
	}

	public TSF TSF(ISeries<double> input, int forecast, int period)
	{
		if (cacheTSF != null)
		{
			for (int i = 0; i < cacheTSF.Length; i++)
			{
				if (cacheTSF[i] != null && cacheTSF[i].Forecast == forecast && cacheTSF[i].Period == period && ((NinjaScriptBase)cacheTSF[i]).EqualsInput(input))
				{
					return cacheTSF[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<TSF>(new TSF
		{
			Forecast = forecast,
			Period = period
		}, input, ref cacheTSF);
	}

	public TSI TSI(int fast, int slow)
	{
		return TSI(((NinjaScriptBase)this).Input, fast, slow);
	}

	public TSI TSI(ISeries<double> input, int fast, int slow)
	{
		if (cacheTSI != null)
		{
			for (int i = 0; i < cacheTSI.Length; i++)
			{
				if (cacheTSI[i] != null && cacheTSI[i].Fast == fast && cacheTSI[i].Slow == slow && ((NinjaScriptBase)cacheTSI[i]).EqualsInput(input))
				{
					return cacheTSI[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<TSI>(new TSI
		{
			Fast = fast,
			Slow = slow
		}, input, ref cacheTSI);
	}

	public UltimateOscillator UltimateOscillator(int fast, int intermediate, int slow)
	{
		return UltimateOscillator(((NinjaScriptBase)this).Input, fast, intermediate, slow);
	}

	public UltimateOscillator UltimateOscillator(ISeries<double> input, int fast, int intermediate, int slow)
	{
		if (cacheUltimateOscillator != null)
		{
			for (int i = 0; i < cacheUltimateOscillator.Length; i++)
			{
				if (cacheUltimateOscillator[i] != null && cacheUltimateOscillator[i].Fast == fast && cacheUltimateOscillator[i].Intermediate == intermediate && cacheUltimateOscillator[i].Slow == slow && ((NinjaScriptBase)cacheUltimateOscillator[i]).EqualsInput(input))
				{
					return cacheUltimateOscillator[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<UltimateOscillator>(new UltimateOscillator
		{
			Fast = fast,
			Intermediate = intermediate,
			Slow = slow
		}, input, ref cacheUltimateOscillator);
	}

	public VMA VMA(int period, int volatilityPeriod)
	{
		return VMA(((NinjaScriptBase)this).Input, period, volatilityPeriod);
	}

	public VMA VMA(ISeries<double> input, int period, int volatilityPeriod)
	{
		if (cacheVMA != null)
		{
			for (int i = 0; i < cacheVMA.Length; i++)
			{
				if (cacheVMA[i] != null && cacheVMA[i].Period == period && cacheVMA[i].VolatilityPeriod == volatilityPeriod && ((NinjaScriptBase)cacheVMA[i]).EqualsInput(input))
				{
					return cacheVMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<VMA>(new VMA
		{
			Period = period,
			VolatilityPeriod = volatilityPeriod
		}, input, ref cacheVMA);
	}

	public VOL VOL()
	{
		return VOL(((NinjaScriptBase)this).Input);
	}

	public VOL VOL(ISeries<double> input)
	{
		if (cacheVOL != null)
		{
			for (int i = 0; i < cacheVOL.Length; i++)
			{
				if (cacheVOL[i] != null && ((NinjaScriptBase)cacheVOL[i]).EqualsInput(input))
				{
					return cacheVOL[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<VOL>(new VOL(), input, ref cacheVOL);
	}

	public VOLMA VOLMA(int period)
	{
		return VOLMA(((NinjaScriptBase)this).Input, period);
	}

	public VOLMA VOLMA(ISeries<double> input, int period)
	{
		if (cacheVOLMA != null)
		{
			for (int i = 0; i < cacheVOLMA.Length; i++)
			{
				if (cacheVOLMA[i] != null && cacheVOLMA[i].Period == period && ((NinjaScriptBase)cacheVOLMA[i]).EqualsInput(input))
				{
					return cacheVOLMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<VOLMA>(new VOLMA
		{
			Period = period
		}, input, ref cacheVOLMA);
	}

	public VolumeCounter VolumeCounter(bool countDown, bool showPercent)
	{
		return VolumeCounter(((NinjaScriptBase)this).Input, countDown, showPercent, TextPositionFine.BottomRight);
	}

	public VolumeCounter VolumeCounter(bool countDown, bool showPercent, TextPositionFine textPositionFine)
	{
		return VolumeCounter(((NinjaScriptBase)this).Input, countDown, showPercent, textPositionFine);
	}

	public VolumeCounter VolumeCounter(ISeries<double> input, bool countDown, bool showPercent)
	{
		return VolumeCounter(input, countDown, showPercent, TextPositionFine.BottomRight);
	}

	public VolumeCounter VolumeCounter(ISeries<double> input, bool countDown, bool showPercent, TextPositionFine textPositionFine)
	{
		if (cacheVolumeCounter != null)
		{
			for (int i = 0; i < cacheVolumeCounter.Length; i++)
			{
				if (cacheVolumeCounter[i] != null && cacheVolumeCounter[i].CountDown == countDown && cacheVolumeCounter[i].ShowPercent == showPercent && cacheVolumeCounter[i].TextPositionFine == textPositionFine && ((NinjaScriptBase)cacheVolumeCounter[i]).EqualsInput(input))
				{
					return cacheVolumeCounter[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<VolumeCounter>(new VolumeCounter
		{
			CountDown = countDown,
			ShowPercent = showPercent
		}, input, ref cacheVolumeCounter);
	}

	public VolumeOscillator VolumeOscillator(int fast, int slow)
	{
		return VolumeOscillator(((NinjaScriptBase)this).Input, fast, slow);
	}

	public VolumeOscillator VolumeOscillator(ISeries<double> input, int fast, int slow)
	{
		if (cacheVolumeOscillator != null)
		{
			for (int i = 0; i < cacheVolumeOscillator.Length; i++)
			{
				if (cacheVolumeOscillator[i] != null && cacheVolumeOscillator[i].Fast == fast && cacheVolumeOscillator[i].Slow == slow && ((NinjaScriptBase)cacheVolumeOscillator[i]).EqualsInput(input))
				{
					return cacheVolumeOscillator[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<VolumeOscillator>(new VolumeOscillator
		{
			Fast = fast,
			Slow = slow
		}, input, ref cacheVolumeOscillator);
	}

	public VolumeProfile VolumeProfile()
	{
		return VolumeProfile(((NinjaScriptBase)this).Input);
	}

	public VolumeProfile VolumeProfile(ISeries<double> input)
	{
		if (cacheVolumeProfile != null)
		{
			for (int i = 0; i < cacheVolumeProfile.Length; i++)
			{
				if (cacheVolumeProfile[i] != null && ((NinjaScriptBase)cacheVolumeProfile[i]).EqualsInput(input))
				{
					return cacheVolumeProfile[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<VolumeProfile>(new VolumeProfile(), input, ref cacheVolumeProfile);
	}

	public VolumeUpDown VolumeUpDown()
	{
		return VolumeUpDown(((NinjaScriptBase)this).Input);
	}

	public VolumeUpDown VolumeUpDown(ISeries<double> input)
	{
		if (cacheVolumeUpDown != null)
		{
			for (int i = 0; i < cacheVolumeUpDown.Length; i++)
			{
				if (cacheVolumeUpDown[i] != null && ((NinjaScriptBase)cacheVolumeUpDown[i]).EqualsInput(input))
				{
					return cacheVolumeUpDown[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<VolumeUpDown>(new VolumeUpDown(), input, ref cacheVolumeUpDown);
	}

	public VolumeZones VolumeZones()
	{
		return VolumeZones(((NinjaScriptBase)this).Input);
	}

	public VolumeZones VolumeZones(ISeries<double> input)
	{
		if (cacheVolumeZones != null)
		{
			for (int i = 0; i < cacheVolumeZones.Length; i++)
			{
				if (cacheVolumeZones[i] != null && ((NinjaScriptBase)cacheVolumeZones[i]).EqualsInput(input))
				{
					return cacheVolumeZones[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<VolumeZones>(new VolumeZones(), input, ref cacheVolumeZones);
	}

	public Vortex Vortex(int period)
	{
		return Vortex(((NinjaScriptBase)this).Input, period);
	}

	public Vortex Vortex(ISeries<double> input, int period)
	{
		if (cacheVortex != null)
		{
			for (int i = 0; i < cacheVortex.Length; i++)
			{
				if (cacheVortex[i] != null && cacheVortex[i].Period == period && ((NinjaScriptBase)cacheVortex[i]).EqualsInput(input))
				{
					return cacheVortex[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<Vortex>(new Vortex
		{
			Period = period
		}, input, ref cacheVortex);
	}

	public VROC VROC(int period, int smooth)
	{
		return VROC(((NinjaScriptBase)this).Input, period, smooth);
	}

	public VROC VROC(ISeries<double> input, int period, int smooth)
	{
		if (cacheVROC != null)
		{
			for (int i = 0; i < cacheVROC.Length; i++)
			{
				if (cacheVROC[i] != null && cacheVROC[i].Period == period && cacheVROC[i].Smooth == smooth && ((NinjaScriptBase)cacheVROC[i]).EqualsInput(input))
				{
					return cacheVROC[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<VROC>(new VROC
		{
			Period = period,
			Smooth = smooth
		}, input, ref cacheVROC);
	}

	public VWMA VWMA(int period)
	{
		return VWMA(((NinjaScriptBase)this).Input, period);
	}

	public VWMA VWMA(ISeries<double> input, int period)
	{
		if (cacheVWMA != null)
		{
			for (int i = 0; i < cacheVWMA.Length; i++)
			{
				if (cacheVWMA[i] != null && cacheVWMA[i].Period == period && ((NinjaScriptBase)cacheVWMA[i]).EqualsInput(input))
				{
					return cacheVWMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<VWMA>(new VWMA
		{
			Period = period
		}, input, ref cacheVWMA);
	}

	public WilliamsR WilliamsR(int period)
	{
		return WilliamsR(((NinjaScriptBase)this).Input, period);
	}

	public WilliamsR WilliamsR(ISeries<double> input, int period)
	{
		if (cacheWilliamsR != null)
		{
			for (int i = 0; i < cacheWilliamsR.Length; i++)
			{
				if (cacheWilliamsR[i] != null && cacheWilliamsR[i].Period == period && ((NinjaScriptBase)cacheWilliamsR[i]).EqualsInput(input))
				{
					return cacheWilliamsR[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WilliamsR>(new WilliamsR
		{
			Period = period
		}, input, ref cacheWilliamsR);
	}

	public WMA WMA(int period)
	{
		return WMA(((NinjaScriptBase)this).Input, period);
	}

	public WMA WMA(ISeries<double> input, int period)
	{
		if (cacheWMA != null)
		{
			for (int i = 0; i < cacheWMA.Length; i++)
			{
				if (cacheWMA[i] != null && cacheWMA[i].Period == period && ((NinjaScriptBase)cacheWMA[i]).EqualsInput(input))
				{
					return cacheWMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WMA>(new WMA
		{
			Period = period
		}, input, ref cacheWMA);
	}

	public ZigZag ZigZag(DeviationType deviationType, double deviationValue, bool useHighLow)
	{
		return ZigZag(((NinjaScriptBase)this).Input, deviationType, deviationValue, useHighLow);
	}

	public ZigZag ZigZag(ISeries<double> input, DeviationType deviationType, double deviationValue, bool useHighLow)
	{
		if (cacheZigZag != null)
		{
			for (int i = 0; i < cacheZigZag.Length; i++)
			{
				if (cacheZigZag[i] != null && cacheZigZag[i].DeviationType == deviationType && cacheZigZag[i].DeviationValue == deviationValue && cacheZigZag[i].UseHighLow == useHighLow && ((NinjaScriptBase)cacheZigZag[i]).EqualsInput(input))
				{
					return cacheZigZag[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ZigZag>(new ZigZag
		{
			DeviationType = deviationType,
			DeviationValue = deviationValue,
			UseHighLow = useHighLow
		}, input, ref cacheZigZag);
	}

	public ZLEMA ZLEMA(int period)
	{
		return ZLEMA(((NinjaScriptBase)this).Input, period);
	}

	public ZLEMA ZLEMA(ISeries<double> input, int period)
	{
		if (cacheZLEMA != null)
		{
			for (int i = 0; i < cacheZLEMA.Length; i++)
			{
				if (cacheZLEMA[i] != null && cacheZLEMA[i].Period == period && ((NinjaScriptBase)cacheZLEMA[i]).EqualsInput(input))
				{
					return cacheZLEMA[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<ZLEMA>(new ZLEMA
		{
			Period = period
		}, input, ref cacheZLEMA);
	}

	public AutoLegProfile AutoLegProfile(int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
	{
		return AutoLegProfile(((NinjaScriptBase)this).Input, reversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, tickCompression, deltaTickCompression, legsToDisplay, volumeProfileWidth, deltaProfileWidth, pastVolumeWidth, pastDeltaWidth, rightOffset, profileSeparation, profileBarSpacing, mergeOverlapPercent, mirrorPastProfiles, showVolume, showDelta, valueAreaPercent, showCurrentLegBox, showVWAP, showDeltaLabels, deltaLabelMinHeight, deltaLabelFontSize, showDeltaLabelBackground);
	}

	public AutoLegProfile AutoLegProfile(ISeries<double> input, int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
	{
		if (cacheAutoLegProfile != null)
		{
			for (int i = 0; i < cacheAutoLegProfile.Length; i++)
			{
				if (cacheAutoLegProfile[i] != null && cacheAutoLegProfile[i].ReversalTicks == reversalTicks && cacheAutoLegProfile[i].MinimumLegTicks == minimumLegTicks && cacheAutoLegProfile[i].MinimumBarsPerLeg == minimumBarsPerLeg && cacheAutoLegProfile[i].MinimumDurationMinutes == minimumDurationMinutes && cacheAutoLegProfile[i].TickCompression == tickCompression && cacheAutoLegProfile[i].DeltaTickCompression == deltaTickCompression && cacheAutoLegProfile[i].LegsToDisplay == legsToDisplay && cacheAutoLegProfile[i].VolumeProfileWidth == volumeProfileWidth && cacheAutoLegProfile[i].DeltaProfileWidth == deltaProfileWidth && cacheAutoLegProfile[i].PastVolumeWidth == pastVolumeWidth && cacheAutoLegProfile[i].PastDeltaWidth == pastDeltaWidth && cacheAutoLegProfile[i].RightOffset == rightOffset && cacheAutoLegProfile[i].ProfileSeparation == profileSeparation && cacheAutoLegProfile[i].ProfileBarSpacing == profileBarSpacing && cacheAutoLegProfile[i].MergeOverlapPercent == mergeOverlapPercent && cacheAutoLegProfile[i].MirrorPastProfiles == mirrorPastProfiles && cacheAutoLegProfile[i].ShowVolume == showVolume && cacheAutoLegProfile[i].ShowDelta == showDelta && cacheAutoLegProfile[i].ValueAreaPercent == valueAreaPercent && cacheAutoLegProfile[i].ShowCurrentLegBox == showCurrentLegBox && cacheAutoLegProfile[i].ShowVWAP == showVWAP && cacheAutoLegProfile[i].ShowDeltaLabels == showDeltaLabels && cacheAutoLegProfile[i].DeltaLabelMinHeight == deltaLabelMinHeight && cacheAutoLegProfile[i].DeltaLabelFontSize == deltaLabelFontSize && cacheAutoLegProfile[i].ShowDeltaLabelBackground == showDeltaLabelBackground && ((NinjaScriptBase)cacheAutoLegProfile[i]).EqualsInput(input))
				{
					return cacheAutoLegProfile[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<AutoLegProfile>(new AutoLegProfile
		{
			ReversalTicks = reversalTicks,
			MinimumLegTicks = minimumLegTicks,
			MinimumBarsPerLeg = minimumBarsPerLeg,
			MinimumDurationMinutes = minimumDurationMinutes,
			TickCompression = tickCompression,
			DeltaTickCompression = deltaTickCompression,
			LegsToDisplay = legsToDisplay,
			VolumeProfileWidth = volumeProfileWidth,
			DeltaProfileWidth = deltaProfileWidth,
			PastVolumeWidth = pastVolumeWidth,
			PastDeltaWidth = pastDeltaWidth,
			RightOffset = rightOffset,
			ProfileSeparation = profileSeparation,
			ProfileBarSpacing = profileBarSpacing,
			MergeOverlapPercent = mergeOverlapPercent,
			MirrorPastProfiles = mirrorPastProfiles,
			ShowVolume = showVolume,
			ShowDelta = showDelta,
			ValueAreaPercent = valueAreaPercent,
			ShowCurrentLegBox = showCurrentLegBox,
			ShowVWAP = showVWAP,
			ShowDeltaLabels = showDeltaLabels,
			DeltaLabelMinHeight = deltaLabelMinHeight,
			DeltaLabelFontSize = deltaLabelFontSize,
			ShowDeltaLabelBackground = showDeltaLabelBackground
		}, input, ref cacheAutoLegProfile);
	}

	public AutoLegProfileNT AutoLegProfileNT(int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
	{
		return AutoLegProfileNT(((NinjaScriptBase)this).Input, reversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, tickCompression, deltaTickCompression, legsToDisplay, volumeProfileWidth, deltaProfileWidth, pastVolumeWidth, pastDeltaWidth, rightOffset, profileSeparation, profileBarSpacing, mergeOverlapPercent, mirrorPastProfiles, showVolume, showDelta, valueAreaPercent, showCurrentLegBox, showVWAP, showDeltaLabels, deltaLabelMinHeight, deltaLabelFontSize, showDeltaLabelBackground);
	}

	public AutoLegProfileNT AutoLegProfileNT(ISeries<double> input, int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
	{
		if (cacheAutoLegProfileNT != null)
		{
			for (int i = 0; i < cacheAutoLegProfileNT.Length; i++)
			{
				if (cacheAutoLegProfileNT[i] != null && cacheAutoLegProfileNT[i].ReversalTicks == reversalTicks && cacheAutoLegProfileNT[i].MinimumLegTicks == minimumLegTicks && cacheAutoLegProfileNT[i].MinimumBarsPerLeg == minimumBarsPerLeg && cacheAutoLegProfileNT[i].MinimumDurationMinutes == minimumDurationMinutes && cacheAutoLegProfileNT[i].TickCompression == tickCompression && cacheAutoLegProfileNT[i].DeltaTickCompression == deltaTickCompression && cacheAutoLegProfileNT[i].LegsToDisplay == legsToDisplay && cacheAutoLegProfileNT[i].VolumeProfileWidth == volumeProfileWidth && cacheAutoLegProfileNT[i].DeltaProfileWidth == deltaProfileWidth && cacheAutoLegProfileNT[i].PastVolumeWidth == pastVolumeWidth && cacheAutoLegProfileNT[i].PastDeltaWidth == pastDeltaWidth && cacheAutoLegProfileNT[i].RightOffset == rightOffset && cacheAutoLegProfileNT[i].ProfileSeparation == profileSeparation && cacheAutoLegProfileNT[i].ProfileBarSpacing == profileBarSpacing && cacheAutoLegProfileNT[i].MergeOverlapPercent == mergeOverlapPercent && cacheAutoLegProfileNT[i].MirrorPastProfiles == mirrorPastProfiles && cacheAutoLegProfileNT[i].ShowVolume == showVolume && cacheAutoLegProfileNT[i].ShowDelta == showDelta && cacheAutoLegProfileNT[i].ValueAreaPercent == valueAreaPercent && cacheAutoLegProfileNT[i].ShowCurrentLegBox == showCurrentLegBox && cacheAutoLegProfileNT[i].ShowVWAP == showVWAP && cacheAutoLegProfileNT[i].ShowDeltaLabels == showDeltaLabels && cacheAutoLegProfileNT[i].DeltaLabelMinHeight == deltaLabelMinHeight && cacheAutoLegProfileNT[i].DeltaLabelFontSize == deltaLabelFontSize && cacheAutoLegProfileNT[i].ShowDeltaLabelBackground == showDeltaLabelBackground && ((NinjaScriptBase)cacheAutoLegProfileNT[i]).EqualsInput(input))
				{
					return cacheAutoLegProfileNT[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<AutoLegProfileNT>(new AutoLegProfileNT
		{
			ReversalTicks = reversalTicks,
			MinimumLegTicks = minimumLegTicks,
			MinimumBarsPerLeg = minimumBarsPerLeg,
			MinimumDurationMinutes = minimumDurationMinutes,
			TickCompression = tickCompression,
			DeltaTickCompression = deltaTickCompression,
			LegsToDisplay = legsToDisplay,
			VolumeProfileWidth = volumeProfileWidth,
			DeltaProfileWidth = deltaProfileWidth,
			PastVolumeWidth = pastVolumeWidth,
			PastDeltaWidth = pastDeltaWidth,
			RightOffset = rightOffset,
			ProfileSeparation = profileSeparation,
			ProfileBarSpacing = profileBarSpacing,
			MergeOverlapPercent = mergeOverlapPercent,
			MirrorPastProfiles = mirrorPastProfiles,
			ShowVolume = showVolume,
			ShowDelta = showDelta,
			ValueAreaPercent = valueAreaPercent,
			ShowCurrentLegBox = showCurrentLegBox,
			ShowVWAP = showVWAP,
			ShowDeltaLabels = showDeltaLabels,
			DeltaLabelMinHeight = deltaLabelMinHeight,
			DeltaLabelFontSize = deltaLabelFontSize,
			ShowDeltaLabelBackground = showDeltaLabelBackground
		}, input, ref cacheAutoLegProfileNT);
	}

	public AutoLegProfileNT2 AutoLegProfileNT2(int reversalTicks, int pastReversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, float volumeOpacity, float deltaOpacity)
	{
		return AutoLegProfileNT2(((NinjaScriptBase)this).Input, reversalTicks, pastReversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, volumeOpacity, deltaOpacity);
	}

	public AutoLegProfileNT2 AutoLegProfileNT2(ISeries<double> input, int reversalTicks, int pastReversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, float volumeOpacity, float deltaOpacity)
	{
		if (cacheAutoLegProfileNT2 != null)
		{
			for (int i = 0; i < cacheAutoLegProfileNT2.Length; i++)
			{
				if (cacheAutoLegProfileNT2[i] != null && cacheAutoLegProfileNT2[i].ReversalTicks == reversalTicks && cacheAutoLegProfileNT2[i].PastReversalTicks == pastReversalTicks && cacheAutoLegProfileNT2[i].MinimumLegTicks == minimumLegTicks && cacheAutoLegProfileNT2[i].MinimumBarsPerLeg == minimumBarsPerLeg && cacheAutoLegProfileNT2[i].MinimumDurationMinutes == minimumDurationMinutes && cacheAutoLegProfileNT2[i].LegsToDisplay == legsToDisplay && cacheAutoLegProfileNT2[i].VolumeTickCompression == volumeTickCompression && cacheAutoLegProfileNT2[i].DeltaTickCompression == deltaTickCompression && cacheAutoLegProfileNT2[i].VolumeProfileWidthPx == volumeProfileWidthPx && cacheAutoLegProfileNT2[i].DeltaProfileWidthPx == deltaProfileWidthPx && cacheAutoLegProfileNT2[i].PastVolumeWidthPx == pastVolumeWidthPx && cacheAutoLegProfileNT2[i].PastDeltaWidthPx == pastDeltaWidthPx && cacheAutoLegProfileNT2[i].RightOffsetPx == rightOffsetPx && cacheAutoLegProfileNT2[i].ProfileSeparationPx == profileSeparationPx && cacheAutoLegProfileNT2[i].ProfileBarSpacingPx == profileBarSpacingPx && cacheAutoLegProfileNT2[i].ShowVolume == showVolume && cacheAutoLegProfileNT2[i].ShowDelta == showDelta && cacheAutoLegProfileNT2[i].ShowPastDelta == showPastDelta && cacheAutoLegProfileNT2[i].ShowCurrentLegBox == showCurrentLegBox && cacheAutoLegProfileNT2[i].DeltaLabelFontSize == deltaLabelFontSize && cacheAutoLegProfileNT2[i].ShowDeltaLabelBackground == showDeltaLabelBackground && cacheAutoLegProfileNT2[i].VolumeOpacity == volumeOpacity && cacheAutoLegProfileNT2[i].DeltaOpacity == deltaOpacity && ((NinjaScriptBase)cacheAutoLegProfileNT2[i]).EqualsInput(input))
				{
					return cacheAutoLegProfileNT2[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<AutoLegProfileNT2>(new AutoLegProfileNT2
		{
			ReversalTicks = reversalTicks,
			PastReversalTicks = pastReversalTicks,
			MinimumLegTicks = minimumLegTicks,
			MinimumBarsPerLeg = minimumBarsPerLeg,
			MinimumDurationMinutes = minimumDurationMinutes,
			LegsToDisplay = legsToDisplay,
			VolumeTickCompression = volumeTickCompression,
			DeltaTickCompression = deltaTickCompression,
			VolumeProfileWidthPx = volumeProfileWidthPx,
			DeltaProfileWidthPx = deltaProfileWidthPx,
			PastVolumeWidthPx = pastVolumeWidthPx,
			PastDeltaWidthPx = pastDeltaWidthPx,
			RightOffsetPx = rightOffsetPx,
			ProfileSeparationPx = profileSeparationPx,
			ProfileBarSpacingPx = profileBarSpacingPx,
			ShowVolume = showVolume,
			ShowDelta = showDelta,
			ShowPastDelta = showPastDelta,
			ShowCurrentLegBox = showCurrentLegBox,
			DeltaLabelFontSize = deltaLabelFontSize,
			ShowDeltaLabelBackground = showDeltaLabelBackground,
			VolumeOpacity = volumeOpacity,
			DeltaOpacity = deltaOpacity
		}, input, ref cacheAutoLegProfileNT2);
	}

	public BarTimes BarTimes(TimeSelector timeUnits)
	{
		return BarTimes(((NinjaScriptBase)this).Input, timeUnits);
	}

	public BarTimes BarTimes(ISeries<double> input, TimeSelector timeUnits)
	{
		if (cacheBarTimes != null)
		{
			for (int i = 0; i < cacheBarTimes.Length; i++)
			{
				if (cacheBarTimes[i] != null && cacheBarTimes[i].TimeUnits == timeUnits && ((NinjaScriptBase)cacheBarTimes[i]).EqualsInput(input))
				{
					return cacheBarTimes[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<BarTimes>(new BarTimes
		{
			TimeUnits = timeUnits
		}, input, ref cacheBarTimes);
	}

	public FastCandleHighlight FastCandleHighlight(HighlightingMode mode, Brush highlightColor, int maxSeconds, int averagePeriod, int percentageThreshold)
	{
		return FastCandleHighlight(((NinjaScriptBase)this).Input, mode, highlightColor, maxSeconds, averagePeriod, percentageThreshold);
	}

	public FastCandleHighlight FastCandleHighlight(ISeries<double> input, HighlightingMode mode, Brush highlightColor, int maxSeconds, int averagePeriod, int percentageThreshold)
	{
		if (cacheFastCandleHighlight != null)
		{
			for (int i = 0; i < cacheFastCandleHighlight.Length; i++)
			{
				if (cacheFastCandleHighlight[i] != null && cacheFastCandleHighlight[i].Mode == mode && cacheFastCandleHighlight[i].HighlightColor == highlightColor && cacheFastCandleHighlight[i].MaxSeconds == maxSeconds && cacheFastCandleHighlight[i].AveragePeriod == averagePeriod && cacheFastCandleHighlight[i].PercentageThreshold == percentageThreshold && ((NinjaScriptBase)cacheFastCandleHighlight[i]).EqualsInput(input))
				{
					return cacheFastCandleHighlight[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<FastCandleHighlight>(new FastCandleHighlight
		{
			Mode = mode,
			HighlightColor = highlightColor,
			MaxSeconds = maxSeconds,
			AveragePeriod = averagePeriod,
			PercentageThreshold = percentageThreshold
		}, input, ref cacheFastCandleHighlight);
	}

	public LegToLegDeltaProfile LegToLegDeltaProfile(ProfileModes profileMode, LegRotationMode rotationMode, double rotationPoints, int maxProfileWidthPx, long minAbsDeltaToShow, int rebuildLookbackBarsCap, bool autoDeltaText, int autoFontMin, int autoFontMax, int fontSize, int minRowHeightPx, Brush positiveBrush, Brush negativeBrush, Brush textBrush, float deltaOpacity, bool showSpine, int spineWidthPx, int rightMarginPx, double rightReservedPercent, int xOffsetPx, bool showVolumeProfile, VolumeProfileLayer volumeLayer, int volumeProfileWidthPx, float volumeOpacity, Brush volumeBrush, int sideBySideGapPx)
	{
		return LegToLegDeltaProfile(((NinjaScriptBase)this).Input, profileMode, rotationMode, rotationPoints, maxProfileWidthPx, minAbsDeltaToShow, rebuildLookbackBarsCap, autoDeltaText, autoFontMin, autoFontMax, fontSize, minRowHeightPx, positiveBrush, negativeBrush, textBrush, deltaOpacity, showSpine, spineWidthPx, rightMarginPx, rightReservedPercent, xOffsetPx, showVolumeProfile, volumeLayer, volumeProfileWidthPx, volumeOpacity, volumeBrush, sideBySideGapPx);
	}

	public LegToLegDeltaProfile LegToLegDeltaProfile(ISeries<double> input, ProfileModes profileMode, LegRotationMode rotationMode, double rotationPoints, int maxProfileWidthPx, long minAbsDeltaToShow, int rebuildLookbackBarsCap, bool autoDeltaText, int autoFontMin, int autoFontMax, int fontSize, int minRowHeightPx, Brush positiveBrush, Brush negativeBrush, Brush textBrush, float deltaOpacity, bool showSpine, int spineWidthPx, int rightMarginPx, double rightReservedPercent, int xOffsetPx, bool showVolumeProfile, VolumeProfileLayer volumeLayer, int volumeProfileWidthPx, float volumeOpacity, Brush volumeBrush, int sideBySideGapPx)
	{
		if (cacheLegToLegDeltaProfile != null)
		{
			for (int i = 0; i < cacheLegToLegDeltaProfile.Length; i++)
			{
				if (cacheLegToLegDeltaProfile[i] != null && cacheLegToLegDeltaProfile[i].ProfileMode == profileMode && cacheLegToLegDeltaProfile[i].RotationMode == rotationMode && cacheLegToLegDeltaProfile[i].RotationPoints == rotationPoints && cacheLegToLegDeltaProfile[i].MaxProfileWidthPx == maxProfileWidthPx && cacheLegToLegDeltaProfile[i].MinAbsDeltaToShow == minAbsDeltaToShow && cacheLegToLegDeltaProfile[i].RebuildLookbackBarsCap == rebuildLookbackBarsCap && cacheLegToLegDeltaProfile[i].AutoDeltaText == autoDeltaText && cacheLegToLegDeltaProfile[i].AutoFontMin == autoFontMin && cacheLegToLegDeltaProfile[i].AutoFontMax == autoFontMax && cacheLegToLegDeltaProfile[i].FontSize == fontSize && cacheLegToLegDeltaProfile[i].MinRowHeightPx == minRowHeightPx && cacheLegToLegDeltaProfile[i].PositiveBrush == positiveBrush && cacheLegToLegDeltaProfile[i].NegativeBrush == negativeBrush && cacheLegToLegDeltaProfile[i].TextBrush == textBrush && cacheLegToLegDeltaProfile[i].DeltaOpacity == deltaOpacity && cacheLegToLegDeltaProfile[i].ShowSpine == showSpine && cacheLegToLegDeltaProfile[i].SpineWidthPx == spineWidthPx && cacheLegToLegDeltaProfile[i].RightMarginPx == rightMarginPx && cacheLegToLegDeltaProfile[i].RightReservedPercent == rightReservedPercent && cacheLegToLegDeltaProfile[i].XOffsetPx == xOffsetPx && cacheLegToLegDeltaProfile[i].ShowVolumeProfile == showVolumeProfile && cacheLegToLegDeltaProfile[i].VolumeLayer == volumeLayer && cacheLegToLegDeltaProfile[i].VolumeProfileWidthPx == volumeProfileWidthPx && cacheLegToLegDeltaProfile[i].VolumeOpacity == volumeOpacity && cacheLegToLegDeltaProfile[i].VolumeBrush == volumeBrush && cacheLegToLegDeltaProfile[i].SideBySideGapPx == sideBySideGapPx && ((NinjaScriptBase)cacheLegToLegDeltaProfile[i]).EqualsInput(input))
				{
					return cacheLegToLegDeltaProfile[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<LegToLegDeltaProfile>(new LegToLegDeltaProfile
		{
			ProfileMode = profileMode,
			RotationMode = rotationMode,
			RotationPoints = rotationPoints,
			MaxProfileWidthPx = maxProfileWidthPx,
			MinAbsDeltaToShow = minAbsDeltaToShow,
			RebuildLookbackBarsCap = rebuildLookbackBarsCap,
			AutoDeltaText = autoDeltaText,
			AutoFontMin = autoFontMin,
			AutoFontMax = autoFontMax,
			FontSize = fontSize,
			MinRowHeightPx = minRowHeightPx,
			PositiveBrush = positiveBrush,
			NegativeBrush = negativeBrush,
			TextBrush = textBrush,
			DeltaOpacity = deltaOpacity,
			ShowSpine = showSpine,
			SpineWidthPx = spineWidthPx,
			RightMarginPx = rightMarginPx,
			RightReservedPercent = rightReservedPercent,
			XOffsetPx = xOffsetPx,
			ShowVolumeProfile = showVolumeProfile,
			VolumeLayer = volumeLayer,
			VolumeProfileWidthPx = volumeProfileWidthPx,
			VolumeOpacity = volumeOpacity,
			VolumeBrush = volumeBrush,
			SideBySideGapPx = sideBySideGapPx
		}, input, ref cacheLegToLegDeltaProfile);
	}

	public OrcaAbsorptionCandles OrcaAbsorptionCandles(bool showHistoricalColor)
	{
		return OrcaAbsorptionCandles(((NinjaScriptBase)this).Input, showHistoricalColor);
	}

	public OrcaAbsorptionCandles OrcaAbsorptionCandles(ISeries<double> input, bool showHistoricalColor)
	{
		if (cacheOrcaAbsorptionCandles != null)
		{
			for (int i = 0; i < cacheOrcaAbsorptionCandles.Length; i++)
			{
				if (cacheOrcaAbsorptionCandles[i] != null && cacheOrcaAbsorptionCandles[i].ShowHistoricalColor == showHistoricalColor && ((NinjaScriptBase)cacheOrcaAbsorptionCandles[i]).EqualsInput(input))
				{
					return cacheOrcaAbsorptionCandles[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaAbsorptionCandles>(new OrcaAbsorptionCandles
		{
			ShowHistoricalColor = showHistoricalColor
		}, input, ref cacheOrcaAbsorptionCandles);
	}

	public OrcaAnchoredVWAPs OrcaAnchoredVWAPs(int developingTicks, int standardTicks, int htfTicks, bool useAtrReversal, int atrPeriod, double devAtrMultiplier, double stdAtrMultiplier, double htfAtrMultiplier, bool showStdBands, bool showHtfBands, bool showAllBands, bool showStdDev1, bool showStdDev2, bool showStdDev3, double stdDevMultiplier1, double stdDevMultiplier2, double stdDevMultiplier3, int fillOpacityStdCore1, int fillOpacityStd12, int fillOpacityStd23, int fillOpacityHtfCore1, int fillOpacityHtf12, int fillOpacityHtf23)
	{
		return OrcaAnchoredVWAPs(((NinjaScriptBase)this).Input, developingTicks, standardTicks, htfTicks, useAtrReversal, atrPeriod, devAtrMultiplier, stdAtrMultiplier, htfAtrMultiplier, showStdBands, showHtfBands, showAllBands, showStdDev1, showStdDev2, showStdDev3, stdDevMultiplier1, stdDevMultiplier2, stdDevMultiplier3, fillOpacityStdCore1, fillOpacityStd12, fillOpacityStd23, fillOpacityHtfCore1, fillOpacityHtf12, fillOpacityHtf23);
	}

	public OrcaAnchoredVWAPs OrcaAnchoredVWAPs(ISeries<double> input, int developingTicks, int standardTicks, int htfTicks, bool useAtrReversal, int atrPeriod, double devAtrMultiplier, double stdAtrMultiplier, double htfAtrMultiplier, bool showStdBands, bool showHtfBands, bool showAllBands, bool showStdDev1, bool showStdDev2, bool showStdDev3, double stdDevMultiplier1, double stdDevMultiplier2, double stdDevMultiplier3, int fillOpacityStdCore1, int fillOpacityStd12, int fillOpacityStd23, int fillOpacityHtfCore1, int fillOpacityHtf12, int fillOpacityHtf23)
	{
		if (cacheOrcaAnchoredVWAPs != null)
		{
			for (int i = 0; i < cacheOrcaAnchoredVWAPs.Length; i++)
			{
				if (cacheOrcaAnchoredVWAPs[i] != null && cacheOrcaAnchoredVWAPs[i].DevelopingTicks == developingTicks && cacheOrcaAnchoredVWAPs[i].StandardTicks == standardTicks && cacheOrcaAnchoredVWAPs[i].HtfTicks == htfTicks && cacheOrcaAnchoredVWAPs[i].UseAtrReversal == useAtrReversal && cacheOrcaAnchoredVWAPs[i].AtrPeriod == atrPeriod && cacheOrcaAnchoredVWAPs[i].DevAtrMultiplier == devAtrMultiplier && cacheOrcaAnchoredVWAPs[i].StdAtrMultiplier == stdAtrMultiplier && cacheOrcaAnchoredVWAPs[i].HtfAtrMultiplier == htfAtrMultiplier && cacheOrcaAnchoredVWAPs[i].ShowStdBands == showStdBands && cacheOrcaAnchoredVWAPs[i].ShowHtfBands == showHtfBands && cacheOrcaAnchoredVWAPs[i].ShowAllBands == showAllBands && cacheOrcaAnchoredVWAPs[i].ShowStdDev1 == showStdDev1 && cacheOrcaAnchoredVWAPs[i].ShowStdDev2 == showStdDev2 && cacheOrcaAnchoredVWAPs[i].ShowStdDev3 == showStdDev3 && cacheOrcaAnchoredVWAPs[i].StdDevMultiplier1 == stdDevMultiplier1 && cacheOrcaAnchoredVWAPs[i].StdDevMultiplier2 == stdDevMultiplier2 && cacheOrcaAnchoredVWAPs[i].StdDevMultiplier3 == stdDevMultiplier3 && cacheOrcaAnchoredVWAPs[i].FillOpacityStdCore1 == fillOpacityStdCore1 && cacheOrcaAnchoredVWAPs[i].FillOpacityStd12 == fillOpacityStd12 && cacheOrcaAnchoredVWAPs[i].FillOpacityStd23 == fillOpacityStd23 && cacheOrcaAnchoredVWAPs[i].FillOpacityHtfCore1 == fillOpacityHtfCore1 && cacheOrcaAnchoredVWAPs[i].FillOpacityHtf12 == fillOpacityHtf12 && cacheOrcaAnchoredVWAPs[i].FillOpacityHtf23 == fillOpacityHtf23 && ((NinjaScriptBase)cacheOrcaAnchoredVWAPs[i]).EqualsInput(input))
				{
					return cacheOrcaAnchoredVWAPs[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaAnchoredVWAPs>(new OrcaAnchoredVWAPs
		{
			DevelopingTicks = developingTicks,
			StandardTicks = standardTicks,
			HtfTicks = htfTicks,
			UseAtrReversal = useAtrReversal,
			AtrPeriod = atrPeriod,
			DevAtrMultiplier = devAtrMultiplier,
			StdAtrMultiplier = stdAtrMultiplier,
			HtfAtrMultiplier = htfAtrMultiplier,
			ShowStdBands = showStdBands,
			ShowHtfBands = showHtfBands,
			ShowAllBands = showAllBands,
			ShowStdDev1 = showStdDev1,
			ShowStdDev2 = showStdDev2,
			ShowStdDev3 = showStdDev3,
			StdDevMultiplier1 = stdDevMultiplier1,
			StdDevMultiplier2 = stdDevMultiplier2,
			StdDevMultiplier3 = stdDevMultiplier3,
			FillOpacityStdCore1 = fillOpacityStdCore1,
			FillOpacityStd12 = fillOpacityStd12,
			FillOpacityStd23 = fillOpacityStd23,
			FillOpacityHtfCore1 = fillOpacityHtfCore1,
			FillOpacityHtf12 = fillOpacityHtf12,
			FillOpacityHtf23 = fillOpacityHtf23
		}, input, ref cacheOrcaAnchoredVWAPs);
	}

	public OrcaCandleVolumeProfile OrcaCandleVolumeProfile(int tickCompression, int candleWidthPx, int profileWidthPx, bool dynamicProfileWidth, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showPOC, bool showDelta, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
	{
		return OrcaCandleVolumeProfile(((NinjaScriptBase)this).Input, tickCompression, candleWidthPx, profileWidthPx, dynamicProfileWidth, candleProfileGapPx, profileBarSpacingPx, wickWidthPx, showPOC, showDelta, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
	}

	public OrcaCandleVolumeProfile OrcaCandleVolumeProfile(ISeries<double> input, int tickCompression, int candleWidthPx, int profileWidthPx, bool dynamicProfileWidth, int candleProfileGapPx, int profileBarSpacingPx, int wickWidthPx, bool showPOC, bool showDelta, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
	{
		if (cacheOrcaCandleVolumeProfile != null)
		{
			for (int i = 0; i < cacheOrcaCandleVolumeProfile.Length; i++)
			{
				if (cacheOrcaCandleVolumeProfile[i] != null && cacheOrcaCandleVolumeProfile[i].TickCompression == tickCompression && cacheOrcaCandleVolumeProfile[i].CandleWidthPx == candleWidthPx && cacheOrcaCandleVolumeProfile[i].ProfileWidthPx == profileWidthPx && cacheOrcaCandleVolumeProfile[i].DynamicProfileWidth == dynamicProfileWidth && cacheOrcaCandleVolumeProfile[i].CandleProfileGapPx == candleProfileGapPx && cacheOrcaCandleVolumeProfile[i].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaCandleVolumeProfile[i].WickWidthPx == wickWidthPx && cacheOrcaCandleVolumeProfile[i].ShowPOC == showPOC && cacheOrcaCandleVolumeProfile[i].ShowDelta == showDelta && cacheOrcaCandleVolumeProfile[i].UseGradient == useGradient && cacheOrcaCandleVolumeProfile[i].GradientSteps == gradientSteps && cacheOrcaCandleVolumeProfile[i].ShowValueArea == showValueArea && cacheOrcaCandleVolumeProfile[i].ShowVAColor == showVAColor && cacheOrcaCandleVolumeProfile[i].ShowVALines == showVALines && cacheOrcaCandleVolumeProfile[i].ValueAreaPercent == valueAreaPercent && cacheOrcaCandleVolumeProfile[i].VALineThickness == vALineThickness && cacheOrcaCandleVolumeProfile[i].VALineStyle == vALineStyle && cacheOrcaCandleVolumeProfile[i].MinBrightness == minBrightness && cacheOrcaCandleVolumeProfile[i].VolumeOpacity == volumeOpacity && cacheOrcaCandleVolumeProfile[i].DeltaOpacity == deltaOpacity && ((NinjaScriptBase)cacheOrcaCandleVolumeProfile[i]).EqualsInput(input))
				{
					return cacheOrcaCandleVolumeProfile[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaCandleVolumeProfile>(new OrcaCandleVolumeProfile
		{
			TickCompression = tickCompression,
			CandleWidthPx = candleWidthPx,
			ProfileWidthPx = profileWidthPx,
			DynamicProfileWidth = dynamicProfileWidth,
			CandleProfileGapPx = candleProfileGapPx,
			ProfileBarSpacingPx = profileBarSpacingPx,
			WickWidthPx = wickWidthPx,
			ShowPOC = showPOC,
			ShowDelta = showDelta,
			UseGradient = useGradient,
			GradientSteps = gradientSteps,
			ShowValueArea = showValueArea,
			ShowVAColor = showVAColor,
			ShowVALines = showVALines,
			ValueAreaPercent = valueAreaPercent,
			VALineThickness = vALineThickness,
			VALineStyle = vALineStyle,
			MinBrightness = minBrightness,
			VolumeOpacity = volumeOpacity,
			DeltaOpacity = deltaOpacity
		}, input, ref cacheOrcaCandleVolumeProfile);
	}

	public OrcaCumulativeDelta OrcaCumulativeDelta()
	{
		return OrcaCumulativeDelta(((NinjaScriptBase)this).Input);
	}

	public OrcaCumulativeDelta OrcaCumulativeDelta(ISeries<double> input)
	{
		if (cacheOrcaCumulativeDelta != null)
		{
			for (int i = 0; i < cacheOrcaCumulativeDelta.Length; i++)
			{
				if (cacheOrcaCumulativeDelta[i] != null && ((NinjaScriptBase)cacheOrcaCumulativeDelta[i]).EqualsInput(input))
				{
					return cacheOrcaCumulativeDelta[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaCumulativeDelta>(new OrcaCumulativeDelta(), input, ref cacheOrcaCumulativeDelta);
	}

	public OrcaExecutionLines OrcaExecutionLines(bool showExecutionLines, bool showLabels, bool showMarkers, bool showIndividualLines, bool showIndividualMarkers, bool showMAEMFE, bool enableShotClock, int shotClockSeconds, bool loadTodayHistory, bool loadSqliteHistory, double riskAmount, int lineWidth, int labelFontSize)
	{
		return OrcaExecutionLines(((NinjaScriptBase)this).Input, showExecutionLines, showLabels, showMarkers, showIndividualLines, showIndividualMarkers, showMAEMFE, enableShotClock, shotClockSeconds, loadTodayHistory, loadSqliteHistory, riskAmount, lineWidth, labelFontSize);
	}

	public OrcaExecutionLines OrcaExecutionLines(ISeries<double> input, bool showExecutionLines, bool showLabels, bool showMarkers, bool showIndividualLines, bool showIndividualMarkers, bool showMAEMFE, bool enableShotClock, int shotClockSeconds, bool loadTodayHistory, bool loadSqliteHistory, double riskAmount, int lineWidth, int labelFontSize)
	{
		if (cacheOrcaExecutionLines != null)
		{
			for (int i = 0; i < cacheOrcaExecutionLines.Length; i++)
			{
				if (cacheOrcaExecutionLines[i] != null && cacheOrcaExecutionLines[i].ShowExecutionLines == showExecutionLines && cacheOrcaExecutionLines[i].ShowLabels == showLabels && cacheOrcaExecutionLines[i].ShowMarkers == showMarkers && cacheOrcaExecutionLines[i].ShowIndividualLines == showIndividualLines && cacheOrcaExecutionLines[i].ShowIndividualMarkers == showIndividualMarkers && cacheOrcaExecutionLines[i].ShowMAEMFE == showMAEMFE && cacheOrcaExecutionLines[i].EnableShotClock == enableShotClock && cacheOrcaExecutionLines[i].ShotClockSeconds == shotClockSeconds && cacheOrcaExecutionLines[i].LoadTodayHistory == loadTodayHistory && cacheOrcaExecutionLines[i].LoadSqliteHistory == loadSqliteHistory && cacheOrcaExecutionLines[i].RiskAmount == riskAmount && cacheOrcaExecutionLines[i].LineWidth == lineWidth && cacheOrcaExecutionLines[i].LabelFontSize == labelFontSize && ((NinjaScriptBase)cacheOrcaExecutionLines[i]).EqualsInput(input))
				{
					return cacheOrcaExecutionLines[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaExecutionLines>(new OrcaExecutionLines
		{
			ShowExecutionLines = showExecutionLines,
			ShowLabels = showLabels,
			ShowMarkers = showMarkers,
			ShowIndividualLines = showIndividualLines,
			ShowIndividualMarkers = showIndividualMarkers,
			ShowMAEMFE = showMAEMFE,
			EnableShotClock = enableShotClock,
			ShotClockSeconds = shotClockSeconds,
			LoadTodayHistory = loadTodayHistory,
			LoadSqliteHistory = loadSqliteHistory,
			RiskAmount = riskAmount,
			LineWidth = lineWidth,
			LabelFontSize = labelFontSize
		}, input, ref cacheOrcaExecutionLines);
	}

	public OrcaLegtoLegProfile OrcaLegtoLegProfile(int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
	{
		return OrcaLegtoLegProfile(((NinjaScriptBase)this).Input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
	}

	public OrcaLegtoLegProfile OrcaLegtoLegProfile(ISeries<double> input, int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
	{
		if (cacheOrcaLegtoLegProfile != null)
		{
			for (int i = 0; i < cacheOrcaLegtoLegProfile.Length; i++)
			{
				if (cacheOrcaLegtoLegProfile[i] != null && cacheOrcaLegtoLegProfile[i].ReversalTicks == reversalTicks && cacheOrcaLegtoLegProfile[i].PastReversalTicks == pastReversalTicks && cacheOrcaLegtoLegProfile[i].UseAtrReversal == useAtrReversal && cacheOrcaLegtoLegProfile[i].AtrPeriod == atrPeriod && cacheOrcaLegtoLegProfile[i].AtrMultiplier == atrMultiplier && cacheOrcaLegtoLegProfile[i].PastAtrMultiplier == pastAtrMultiplier && cacheOrcaLegtoLegProfile[i].MinimumLegTicks == minimumLegTicks && cacheOrcaLegtoLegProfile[i].MinimumBarsPerLeg == minimumBarsPerLeg && cacheOrcaLegtoLegProfile[i].MinimumDurationMinutes == minimumDurationMinutes && cacheOrcaLegtoLegProfile[i].LegsToDisplay == legsToDisplay && cacheOrcaLegtoLegProfile[i].UseDynamicAggregation == useDynamicAggregation && cacheOrcaLegtoLegProfile[i].VolumeTickCompression == volumeTickCompression && cacheOrcaLegtoLegProfile[i].DeltaTickCompression == deltaTickCompression && cacheOrcaLegtoLegProfile[i].VolumeProfileWidthPx == volumeProfileWidthPx && cacheOrcaLegtoLegProfile[i].DeltaProfileWidthPx == deltaProfileWidthPx && cacheOrcaLegtoLegProfile[i].PastVolumeWidthPx == pastVolumeWidthPx && cacheOrcaLegtoLegProfile[i].PastDeltaWidthPx == pastDeltaWidthPx && cacheOrcaLegtoLegProfile[i].RightOffsetPx == rightOffsetPx && cacheOrcaLegtoLegProfile[i].ProfileSeparationPx == profileSeparationPx && cacheOrcaLegtoLegProfile[i].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaLegtoLegProfile[i].ShowVolume == showVolume && cacheOrcaLegtoLegProfile[i].ShowDelta == showDelta && cacheOrcaLegtoLegProfile[i].ShowPastDelta == showPastDelta && cacheOrcaLegtoLegProfile[i].ShowCurrentLegBox == showCurrentLegBox && cacheOrcaLegtoLegProfile[i].DeltaLabelFontSize == deltaLabelFontSize && cacheOrcaLegtoLegProfile[i].ShowDeltaLabelBackground == showDeltaLabelBackground && cacheOrcaLegtoLegProfile[i].ShowPOC == showPOC && cacheOrcaLegtoLegProfile[i].UseGradient == useGradient && cacheOrcaLegtoLegProfile[i].GradientSteps == gradientSteps && cacheOrcaLegtoLegProfile[i].ShowValueArea == showValueArea && cacheOrcaLegtoLegProfile[i].ShowVAColor == showVAColor && cacheOrcaLegtoLegProfile[i].ShowVALines == showVALines && cacheOrcaLegtoLegProfile[i].ValueAreaPercent == valueAreaPercent && cacheOrcaLegtoLegProfile[i].VALineThickness == vALineThickness && cacheOrcaLegtoLegProfile[i].VALineStyle == vALineStyle && cacheOrcaLegtoLegProfile[i].MinBrightness == minBrightness && cacheOrcaLegtoLegProfile[i].VolumeOpacity == volumeOpacity && cacheOrcaLegtoLegProfile[i].DeltaOpacity == deltaOpacity && ((NinjaScriptBase)cacheOrcaLegtoLegProfile[i]).EqualsInput(input))
				{
					return cacheOrcaLegtoLegProfile[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaLegtoLegProfile>(new OrcaLegtoLegProfile
		{
			ReversalTicks = reversalTicks,
			PastReversalTicks = pastReversalTicks,
			UseAtrReversal = useAtrReversal,
			AtrPeriod = atrPeriod,
			AtrMultiplier = atrMultiplier,
			PastAtrMultiplier = pastAtrMultiplier,
			MinimumLegTicks = minimumLegTicks,
			MinimumBarsPerLeg = minimumBarsPerLeg,
			MinimumDurationMinutes = minimumDurationMinutes,
			LegsToDisplay = legsToDisplay,
			UseDynamicAggregation = useDynamicAggregation,
			VolumeTickCompression = volumeTickCompression,
			DeltaTickCompression = deltaTickCompression,
			VolumeProfileWidthPx = volumeProfileWidthPx,
			DeltaProfileWidthPx = deltaProfileWidthPx,
			PastVolumeWidthPx = pastVolumeWidthPx,
			PastDeltaWidthPx = pastDeltaWidthPx,
			RightOffsetPx = rightOffsetPx,
			ProfileSeparationPx = profileSeparationPx,
			ProfileBarSpacingPx = profileBarSpacingPx,
			ShowVolume = showVolume,
			ShowDelta = showDelta,
			ShowPastDelta = showPastDelta,
			ShowCurrentLegBox = showCurrentLegBox,
			DeltaLabelFontSize = deltaLabelFontSize,
			ShowDeltaLabelBackground = showDeltaLabelBackground,
			ShowPOC = showPOC,
			UseGradient = useGradient,
			GradientSteps = gradientSteps,
			ShowValueArea = showValueArea,
			ShowVAColor = showVAColor,
			ShowVALines = showVALines,
			ValueAreaPercent = valueAreaPercent,
			VALineThickness = vALineThickness,
			VALineStyle = vALineStyle,
			MinBrightness = minBrightness,
			VolumeOpacity = volumeOpacity,
			DeltaOpacity = deltaOpacity
		}, input, ref cacheOrcaLegtoLegProfile);
	}

	public OrcaRollingProfiles OrcaRollingProfiles(RollingProfilePeriod period, ProfileOperatingMode mode, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int deltaTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
	{
		return OrcaRollingProfiles(((NinjaScriptBase)this).Input, period, mode, minutesPerDay, rthStartTime, rthEndTime, volumeTickCompression, deltaTickCompression, profileWidthPx, deltaWidthPx, rightOffsetPx, profileBarSpacingPx, showVolume, showDelta, showPOC, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, volumeOpacity, deltaOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
	}

	public OrcaRollingProfiles OrcaRollingProfiles(ISeries<double> input, RollingProfilePeriod period, ProfileOperatingMode mode, int minutesPerDay, TimeSpan rthStartTime, TimeSpan rthEndTime, int volumeTickCompression, int deltaTickCompression, int profileWidthPx, int deltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPOC, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, float volumeOpacity, float deltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
	{
		if (cacheOrcaRollingProfiles != null)
		{
			for (int i = 0; i < cacheOrcaRollingProfiles.Length; i++)
			{
				if (cacheOrcaRollingProfiles[i] != null && cacheOrcaRollingProfiles[i].Period == period && cacheOrcaRollingProfiles[i].Mode == mode && cacheOrcaRollingProfiles[i].MinutesPerDay == minutesPerDay && cacheOrcaRollingProfiles[i].RthStartTime == rthStartTime && cacheOrcaRollingProfiles[i].RthEndTime == rthEndTime && cacheOrcaRollingProfiles[i].VolumeTickCompression == volumeTickCompression && cacheOrcaRollingProfiles[i].DeltaTickCompression == deltaTickCompression && cacheOrcaRollingProfiles[i].ProfileWidthPx == profileWidthPx && cacheOrcaRollingProfiles[i].DeltaWidthPx == deltaWidthPx && cacheOrcaRollingProfiles[i].RightOffsetPx == rightOffsetPx && cacheOrcaRollingProfiles[i].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaRollingProfiles[i].ShowVolume == showVolume && cacheOrcaRollingProfiles[i].ShowDelta == showDelta && cacheOrcaRollingProfiles[i].ShowPOC == showPOC && cacheOrcaRollingProfiles[i].UseGradient == useGradient && cacheOrcaRollingProfiles[i].GradientSteps == gradientSteps && cacheOrcaRollingProfiles[i].MinBrightness == minBrightness && cacheOrcaRollingProfiles[i].ShowValueArea == showValueArea && cacheOrcaRollingProfiles[i].ShowVAColor == showVAColor && cacheOrcaRollingProfiles[i].ShowVALines == showVALines && cacheOrcaRollingProfiles[i].ValueAreaPercent == valueAreaPercent && cacheOrcaRollingProfiles[i].VALineThickness == vALineThickness && cacheOrcaRollingProfiles[i].VolumeOpacity == volumeOpacity && cacheOrcaRollingProfiles[i].DeltaOpacity == deltaOpacity && cacheOrcaRollingProfiles[i].ShowDeltaText == showDeltaText && cacheOrcaRollingProfiles[i].DeltaTextMinThreshold == deltaTextMinThreshold && cacheOrcaRollingProfiles[i].DeltaTextFontSize == deltaTextFontSize && ((NinjaScriptBase)cacheOrcaRollingProfiles[i]).EqualsInput(input))
				{
					return cacheOrcaRollingProfiles[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaRollingProfiles>(new OrcaRollingProfiles
		{
			Period = period,
			Mode = mode,
			MinutesPerDay = minutesPerDay,
			RthStartTime = rthStartTime,
			RthEndTime = rthEndTime,
			VolumeTickCompression = volumeTickCompression,
			DeltaTickCompression = deltaTickCompression,
			ProfileWidthPx = profileWidthPx,
			DeltaWidthPx = deltaWidthPx,
			RightOffsetPx = rightOffsetPx,
			ProfileBarSpacingPx = profileBarSpacingPx,
			ShowVolume = showVolume,
			ShowDelta = showDelta,
			ShowPOC = showPOC,
			UseGradient = useGradient,
			GradientSteps = gradientSteps,
			MinBrightness = minBrightness,
			ShowValueArea = showValueArea,
			ShowVAColor = showVAColor,
			ShowVALines = showVALines,
			ValueAreaPercent = valueAreaPercent,
			VALineThickness = vALineThickness,
			VolumeOpacity = volumeOpacity,
			DeltaOpacity = deltaOpacity,
			ShowDeltaText = showDeltaText,
			DeltaTextMinThreshold = deltaTextMinThreshold,
			DeltaTextFontSize = deltaTextFontSize
		}, input, ref cacheOrcaRollingProfiles);
	}

	public OrcaStepProfile OrcaStepProfile(StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
	{
		return OrcaStepProfile(((NinjaScriptBase)this).Input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, rTHOnly, rTHStart, rTHEnd, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
	}

	public OrcaStepProfile OrcaStepProfile(ISeries<double> input, StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
	{
		if (cacheOrcaStepProfile != null)
		{
			for (int i = 0; i < cacheOrcaStepProfile.Length; i++)
			{
				if (cacheOrcaStepProfile[i] != null && cacheOrcaStepProfile[i].StepInterval == stepInterval && cacheOrcaStepProfile[i].VolumeTickCompression == volumeTickCompression && cacheOrcaStepProfile[i].DeltaTickCompression == deltaTickCompression && cacheOrcaStepProfile[i].UseDynamicAggregation == useDynamicAggregation && cacheOrcaStepProfile[i].DynamicAggregationMultiplier == dynamicAggregationMultiplier && cacheOrcaStepProfile[i].RTHOnly == rTHOnly && cacheOrcaStepProfile[i].RTHStart == rTHStart && cacheOrcaStepProfile[i].RTHEnd == rTHEnd && cacheOrcaStepProfile[i].HistoricalProfileWidthPx == historicalProfileWidthPx && cacheOrcaStepProfile[i].ActiveProfileWidthPx == activeProfileWidthPx && cacheOrcaStepProfile[i].ActiveDeltaWidthPx == activeDeltaWidthPx && cacheOrcaStepProfile[i].HistoricalDeltaWidthPx == historicalDeltaWidthPx && cacheOrcaStepProfile[i].RightOffsetPx == rightOffsetPx && cacheOrcaStepProfile[i].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaStepProfile[i].MirrorProfiles == mirrorProfiles && cacheOrcaStepProfile[i].ShowActiveVolume == showActiveVolume && cacheOrcaStepProfile[i].ShowHistoricalVolume == showHistoricalVolume && cacheOrcaStepProfile[i].ShowActiveDelta == showActiveDelta && cacheOrcaStepProfile[i].ShowHistoricalDelta == showHistoricalDelta && cacheOrcaStepProfile[i].ShowPOC == showPOC && cacheOrcaStepProfile[i].ShowBlockSeparators == showBlockSeparators && cacheOrcaStepProfile[i].UseGradient == useGradient && cacheOrcaStepProfile[i].GradientSteps == gradientSteps && cacheOrcaStepProfile[i].MinBrightness == minBrightness && cacheOrcaStepProfile[i].ShowValueArea == showValueArea && cacheOrcaStepProfile[i].ShowVAColor == showVAColor && cacheOrcaStepProfile[i].ShowVALines == showVALines && cacheOrcaStepProfile[i].ValueAreaPercent == valueAreaPercent && cacheOrcaStepProfile[i].VALineThickness == vALineThickness && cacheOrcaStepProfile[i].VALineStyle == vALineStyle && cacheOrcaStepProfile[i].ActiveVolumeOpacity == activeVolumeOpacity && cacheOrcaStepProfile[i].HistoricalVolumeOpacity == historicalVolumeOpacity && cacheOrcaStepProfile[i].ActiveDeltaOpacity == activeDeltaOpacity && cacheOrcaStepProfile[i].HistoricalDeltaOpacity == historicalDeltaOpacity && cacheOrcaStepProfile[i].ShowDeltaText == showDeltaText && cacheOrcaStepProfile[i].DeltaTextMinThreshold == deltaTextMinThreshold && cacheOrcaStepProfile[i].DeltaTextFontSize == deltaTextFontSize && ((NinjaScriptBase)cacheOrcaStepProfile[i]).EqualsInput(input))
				{
					return cacheOrcaStepProfile[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaStepProfile>(new OrcaStepProfile
		{
			StepInterval = stepInterval,
			VolumeTickCompression = volumeTickCompression,
			DeltaTickCompression = deltaTickCompression,
			UseDynamicAggregation = useDynamicAggregation,
			DynamicAggregationMultiplier = dynamicAggregationMultiplier,
			RTHOnly = rTHOnly,
			RTHStart = rTHStart,
			RTHEnd = rTHEnd,
			HistoricalProfileWidthPx = historicalProfileWidthPx,
			ActiveProfileWidthPx = activeProfileWidthPx,
			ActiveDeltaWidthPx = activeDeltaWidthPx,
			HistoricalDeltaWidthPx = historicalDeltaWidthPx,
			RightOffsetPx = rightOffsetPx,
			ProfileBarSpacingPx = profileBarSpacingPx,
			MirrorProfiles = mirrorProfiles,
			ShowActiveVolume = showActiveVolume,
			ShowHistoricalVolume = showHistoricalVolume,
			ShowActiveDelta = showActiveDelta,
			ShowHistoricalDelta = showHistoricalDelta,
			ShowPOC = showPOC,
			ShowBlockSeparators = showBlockSeparators,
			UseGradient = useGradient,
			GradientSteps = gradientSteps,
			MinBrightness = minBrightness,
			ShowValueArea = showValueArea,
			ShowVAColor = showVAColor,
			ShowVALines = showVALines,
			ValueAreaPercent = valueAreaPercent,
			VALineThickness = vALineThickness,
			VALineStyle = vALineStyle,
			ActiveVolumeOpacity = activeVolumeOpacity,
			HistoricalVolumeOpacity = historicalVolumeOpacity,
			ActiveDeltaOpacity = activeDeltaOpacity,
			HistoricalDeltaOpacity = historicalDeltaOpacity,
			ShowDeltaText = showDeltaText,
			DeltaTextMinThreshold = deltaTextMinThreshold,
			DeltaTextFontSize = deltaTextFontSize
		}, input, ref cacheOrcaStepProfile);
	}

	public OrcaTickDirectionIndex OrcaTickDirectionIndex()
	{
		return OrcaTickDirectionIndex(((NinjaScriptBase)this).Input);
	}

	public OrcaTickDirectionIndex OrcaTickDirectionIndex(ISeries<double> input)
	{
		if (cacheOrcaTickDirectionIndex != null)
		{
			for (int i = 0; i < cacheOrcaTickDirectionIndex.Length; i++)
			{
				if (cacheOrcaTickDirectionIndex[i] != null && ((NinjaScriptBase)cacheOrcaTickDirectionIndex[i]).EqualsInput(input))
				{
					return cacheOrcaTickDirectionIndex[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaTickDirectionIndex>(new OrcaTickDirectionIndex(), input, ref cacheOrcaTickDirectionIndex);
	}

	public OrcaTimeStatistics OrcaTimeStatistics()
	{
		return OrcaTimeStatistics(((NinjaScriptBase)this).Input);
	}

	public OrcaTimeStatistics OrcaTimeStatistics(ISeries<double> input)
	{
		if (cacheOrcaTimeStatistics != null)
		{
			for (int i = 0; i < cacheOrcaTimeStatistics.Length; i++)
			{
				if (cacheOrcaTimeStatistics[i] != null && ((NinjaScriptBase)cacheOrcaTimeStatistics[i]).EqualsInput(input))
				{
					return cacheOrcaTimeStatistics[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaTimeStatistics>(new OrcaTimeStatistics(), input, ref cacheOrcaTimeStatistics);
	}

	public OrcaTimeVWAPs OrcaTimeVWAPs(bool globexShowVWAP, TimeSpan globexStartTime, bool globexShowDev1, double globexDev1Mult, bool globexShowDev2, double globexDev2Mult, bool globexShowDev3, double globexDev3Mult, int globexFillOpacityCore, int globexFillOpacity12, int globexFillOpacity23, bool rthShowVWAP, TimeSpan rthStartTime, bool rthShowDev1, double rthDev1Mult, bool rthShowDev2, double rthDev2Mult, bool rthShowDev3, double rthDev3Mult, int rthFillOpacityCore, int rthFillOpacity12, int rthFillOpacity23, bool rollingShowVWAP, RollingVwapPeriod rollingPeriod, int minutesPerDay, bool rollingShowDev1, double rollingDev1Mult, bool rollingShowDev2, double rollingDev2Mult, bool rollingShowDev3, double rollingDev3Mult, int rollingFillOpacityCore, int rollingFillOpacity12, int rollingFillOpacity23, bool weeklyShowVWAP, TimeSpan weeklyStartTime, bool weeklyShowDev1, double weeklyDev1Mult, bool weeklyShowDev2, double weeklyDev2Mult, bool weeklyShowDev3, double weeklyDev3Mult, int weeklyFillOpacityCore, int weeklyFillOpacity12, int weeklyFillOpacity23)
	{
		return OrcaTimeVWAPs(((NinjaScriptBase)this).Input, globexShowVWAP, globexStartTime, globexShowDev1, globexDev1Mult, globexShowDev2, globexDev2Mult, globexShowDev3, globexDev3Mult, globexFillOpacityCore, globexFillOpacity12, globexFillOpacity23, rthShowVWAP, rthStartTime, rthShowDev1, rthDev1Mult, rthShowDev2, rthDev2Mult, rthShowDev3, rthDev3Mult, rthFillOpacityCore, rthFillOpacity12, rthFillOpacity23, rollingShowVWAP, rollingPeriod, minutesPerDay, rollingShowDev1, rollingDev1Mult, rollingShowDev2, rollingDev2Mult, rollingShowDev3, rollingDev3Mult, rollingFillOpacityCore, rollingFillOpacity12, rollingFillOpacity23, weeklyShowVWAP, weeklyStartTime, weeklyShowDev1, weeklyDev1Mult, weeklyShowDev2, weeklyDev2Mult, weeklyShowDev3, weeklyDev3Mult, weeklyFillOpacityCore, weeklyFillOpacity12, weeklyFillOpacity23);
	}

	public OrcaTimeVWAPs OrcaTimeVWAPs(ISeries<double> input, bool globexShowVWAP, TimeSpan globexStartTime, bool globexShowDev1, double globexDev1Mult, bool globexShowDev2, double globexDev2Mult, bool globexShowDev3, double globexDev3Mult, int globexFillOpacityCore, int globexFillOpacity12, int globexFillOpacity23, bool rthShowVWAP, TimeSpan rthStartTime, bool rthShowDev1, double rthDev1Mult, bool rthShowDev2, double rthDev2Mult, bool rthShowDev3, double rthDev3Mult, int rthFillOpacityCore, int rthFillOpacity12, int rthFillOpacity23, bool rollingShowVWAP, RollingVwapPeriod rollingPeriod, int minutesPerDay, bool rollingShowDev1, double rollingDev1Mult, bool rollingShowDev2, double rollingDev2Mult, bool rollingShowDev3, double rollingDev3Mult, int rollingFillOpacityCore, int rollingFillOpacity12, int rollingFillOpacity23, bool weeklyShowVWAP, TimeSpan weeklyStartTime, bool weeklyShowDev1, double weeklyDev1Mult, bool weeklyShowDev2, double weeklyDev2Mult, bool weeklyShowDev3, double weeklyDev3Mult, int weeklyFillOpacityCore, int weeklyFillOpacity12, int weeklyFillOpacity23)
	{
		if (cacheOrcaTimeVWAPs != null)
		{
			for (int i = 0; i < cacheOrcaTimeVWAPs.Length; i++)
			{
				if (cacheOrcaTimeVWAPs[i] != null && cacheOrcaTimeVWAPs[i].GlobexShowVWAP == globexShowVWAP && cacheOrcaTimeVWAPs[i].GlobexStartTime == globexStartTime && cacheOrcaTimeVWAPs[i].GlobexShowDev1 == globexShowDev1 && cacheOrcaTimeVWAPs[i].GlobexDev1Mult == globexDev1Mult && cacheOrcaTimeVWAPs[i].GlobexShowDev2 == globexShowDev2 && cacheOrcaTimeVWAPs[i].GlobexDev2Mult == globexDev2Mult && cacheOrcaTimeVWAPs[i].GlobexShowDev3 == globexShowDev3 && cacheOrcaTimeVWAPs[i].GlobexDev3Mult == globexDev3Mult && cacheOrcaTimeVWAPs[i].GlobexFillOpacityCore == globexFillOpacityCore && cacheOrcaTimeVWAPs[i].GlobexFillOpacity12 == globexFillOpacity12 && cacheOrcaTimeVWAPs[i].GlobexFillOpacity23 == globexFillOpacity23 && cacheOrcaTimeVWAPs[i].RthShowVWAP == rthShowVWAP && cacheOrcaTimeVWAPs[i].RthStartTime == rthStartTime && cacheOrcaTimeVWAPs[i].RthShowDev1 == rthShowDev1 && cacheOrcaTimeVWAPs[i].RthDev1Mult == rthDev1Mult && cacheOrcaTimeVWAPs[i].RthShowDev2 == rthShowDev2 && cacheOrcaTimeVWAPs[i].RthDev2Mult == rthDev2Mult && cacheOrcaTimeVWAPs[i].RthShowDev3 == rthShowDev3 && cacheOrcaTimeVWAPs[i].RthDev3Mult == rthDev3Mult && cacheOrcaTimeVWAPs[i].RthFillOpacityCore == rthFillOpacityCore && cacheOrcaTimeVWAPs[i].RthFillOpacity12 == rthFillOpacity12 && cacheOrcaTimeVWAPs[i].RthFillOpacity23 == rthFillOpacity23 && cacheOrcaTimeVWAPs[i].RollingShowVWAP == rollingShowVWAP && cacheOrcaTimeVWAPs[i].RollingPeriod == rollingPeriod && cacheOrcaTimeVWAPs[i].MinutesPerDay == minutesPerDay && cacheOrcaTimeVWAPs[i].RollingShowDev1 == rollingShowDev1 && cacheOrcaTimeVWAPs[i].RollingDev1Mult == rollingDev1Mult && cacheOrcaTimeVWAPs[i].RollingShowDev2 == rollingShowDev2 && cacheOrcaTimeVWAPs[i].RollingDev2Mult == rollingDev2Mult && cacheOrcaTimeVWAPs[i].RollingShowDev3 == rollingShowDev3 && cacheOrcaTimeVWAPs[i].RollingDev3Mult == rollingDev3Mult && cacheOrcaTimeVWAPs[i].RollingFillOpacityCore == rollingFillOpacityCore && cacheOrcaTimeVWAPs[i].RollingFillOpacity12 == rollingFillOpacity12 && cacheOrcaTimeVWAPs[i].RollingFillOpacity23 == rollingFillOpacity23 && cacheOrcaTimeVWAPs[i].WeeklyShowVWAP == weeklyShowVWAP && cacheOrcaTimeVWAPs[i].WeeklyStartTime == weeklyStartTime && cacheOrcaTimeVWAPs[i].WeeklyShowDev1 == weeklyShowDev1 && cacheOrcaTimeVWAPs[i].WeeklyDev1Mult == weeklyDev1Mult && cacheOrcaTimeVWAPs[i].WeeklyShowDev2 == weeklyShowDev2 && cacheOrcaTimeVWAPs[i].WeeklyDev2Mult == weeklyDev2Mult && cacheOrcaTimeVWAPs[i].WeeklyShowDev3 == weeklyShowDev3 && cacheOrcaTimeVWAPs[i].WeeklyDev3Mult == weeklyDev3Mult && cacheOrcaTimeVWAPs[i].WeeklyFillOpacityCore == weeklyFillOpacityCore && cacheOrcaTimeVWAPs[i].WeeklyFillOpacity12 == weeklyFillOpacity12 && cacheOrcaTimeVWAPs[i].WeeklyFillOpacity23 == weeklyFillOpacity23 && ((NinjaScriptBase)cacheOrcaTimeVWAPs[i]).EqualsInput(input))
				{
					return cacheOrcaTimeVWAPs[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaTimeVWAPs>(new OrcaTimeVWAPs
		{
			GlobexShowVWAP = globexShowVWAP,
			GlobexStartTime = globexStartTime,
			GlobexShowDev1 = globexShowDev1,
			GlobexDev1Mult = globexDev1Mult,
			GlobexShowDev2 = globexShowDev2,
			GlobexDev2Mult = globexDev2Mult,
			GlobexShowDev3 = globexShowDev3,
			GlobexDev3Mult = globexDev3Mult,
			GlobexFillOpacityCore = globexFillOpacityCore,
			GlobexFillOpacity12 = globexFillOpacity12,
			GlobexFillOpacity23 = globexFillOpacity23,
			RthShowVWAP = rthShowVWAP,
			RthStartTime = rthStartTime,
			RthShowDev1 = rthShowDev1,
			RthDev1Mult = rthDev1Mult,
			RthShowDev2 = rthShowDev2,
			RthDev2Mult = rthDev2Mult,
			RthShowDev3 = rthShowDev3,
			RthDev3Mult = rthDev3Mult,
			RthFillOpacityCore = rthFillOpacityCore,
			RthFillOpacity12 = rthFillOpacity12,
			RthFillOpacity23 = rthFillOpacity23,
			RollingShowVWAP = rollingShowVWAP,
			RollingPeriod = rollingPeriod,
			MinutesPerDay = minutesPerDay,
			RollingShowDev1 = rollingShowDev1,
			RollingDev1Mult = rollingDev1Mult,
			RollingShowDev2 = rollingShowDev2,
			RollingDev2Mult = rollingDev2Mult,
			RollingShowDev3 = rollingShowDev3,
			RollingDev3Mult = rollingDev3Mult,
			RollingFillOpacityCore = rollingFillOpacityCore,
			RollingFillOpacity12 = rollingFillOpacity12,
			RollingFillOpacity23 = rollingFillOpacity23,
			WeeklyShowVWAP = weeklyShowVWAP,
			WeeklyStartTime = weeklyStartTime,
			WeeklyShowDev1 = weeklyShowDev1,
			WeeklyDev1Mult = weeklyDev1Mult,
			WeeklyShowDev2 = weeklyShowDev2,
			WeeklyDev2Mult = weeklyDev2Mult,
			WeeklyShowDev3 = weeklyShowDev3,
			WeeklyDev3Mult = weeklyDev3Mult,
			WeeklyFillOpacityCore = weeklyFillOpacityCore,
			WeeklyFillOpacity12 = weeklyFillOpacity12,
			WeeklyFillOpacity23 = weeklyFillOpacity23
		}, input, ref cacheOrcaTimeVWAPs);
	}

	public OrcaVisualOrders OrcaVisualOrders(int tagOffsetRight, int orderLabelOffsetRight)
	{
		return OrcaVisualOrders(((NinjaScriptBase)this).Input, tagOffsetRight, orderLabelOffsetRight);
	}

	public OrcaVisualOrders OrcaVisualOrders(ISeries<double> input, int tagOffsetRight, int orderLabelOffsetRight)
	{
		if (cacheOrcaVisualOrders != null)
		{
			for (int i = 0; i < cacheOrcaVisualOrders.Length; i++)
			{
				if (cacheOrcaVisualOrders[i] != null && cacheOrcaVisualOrders[i].TagOffsetRight == tagOffsetRight && cacheOrcaVisualOrders[i].OrderLabelOffsetRight == orderLabelOffsetRight && ((NinjaScriptBase)cacheOrcaVisualOrders[i]).EqualsInput(input))
				{
					return cacheOrcaVisualOrders[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrcaVisualOrders>(new OrcaVisualOrders
		{
			TagOffsetRight = tagOffsetRight,
			OrderLabelOffsetRight = orderLabelOffsetRight
		}, input, ref cacheOrcaVisualOrders);
	}

	public PassiveFlowSuite PassiveFlowSuite()
	{
		return PassiveFlowSuite(((NinjaScriptBase)this).Input);
	}

	public PassiveFlowSuite PassiveFlowSuite(ISeries<double> input)
	{
		if (cachePassiveFlowSuite != null)
		{
			for (int i = 0; i < cachePassiveFlowSuite.Length; i++)
			{
				if (cachePassiveFlowSuite[i] != null && ((NinjaScriptBase)cachePassiveFlowSuite[i]).EqualsInput(input))
				{
					return cachePassiveFlowSuite[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PassiveFlowSuite>(new PassiveFlowSuite(), input, ref cachePassiveFlowSuite);
	}

	public PAX30OpeningRange PAX30OpeningRange(string oRBStartSerialize, string oRBEndPlotSerialize, int textvertPixels, int textHorzOffset, int fontSize, bool boldFont, string labelPrefix, Brush highLineColor, Brush lowLineColor, Brush midLineColor, int mainLineWidth, int midLineWidth, int levelsLineWidth, bool showMid)
	{
		return PAX30OpeningRange(((NinjaScriptBase)this).Input, oRBStartSerialize, oRBEndPlotSerialize, textvertPixels, textHorzOffset, fontSize, boldFont, labelPrefix, highLineColor, lowLineColor, midLineColor, mainLineWidth, midLineWidth, levelsLineWidth, showMid);
	}

	public PAX30OpeningRange PAX30OpeningRange(ISeries<double> input, string oRBStartSerialize, string oRBEndPlotSerialize, int textvertPixels, int textHorzOffset, int fontSize, bool boldFont, string labelPrefix, Brush highLineColor, Brush lowLineColor, Brush midLineColor, int mainLineWidth, int midLineWidth, int levelsLineWidth, bool showMid)
	{
		if (cachePAX30OpeningRange != null)
		{
			for (int i = 0; i < cachePAX30OpeningRange.Length; i++)
			{
				if (cachePAX30OpeningRange[i] != null && cachePAX30OpeningRange[i].ORBStartSerialize == oRBStartSerialize && cachePAX30OpeningRange[i].ORBEndPlotSerialize == oRBEndPlotSerialize && cachePAX30OpeningRange[i].TextvertPixels == textvertPixels && cachePAX30OpeningRange[i].TextHorzOffset == textHorzOffset && cachePAX30OpeningRange[i].FontSize == fontSize && cachePAX30OpeningRange[i].BoldFont == boldFont && cachePAX30OpeningRange[i].LabelPrefix == labelPrefix && cachePAX30OpeningRange[i].HighLineColor == highLineColor && cachePAX30OpeningRange[i].LowLineColor == lowLineColor && cachePAX30OpeningRange[i].MidLineColor == midLineColor && cachePAX30OpeningRange[i].MainLineWidth == mainLineWidth && cachePAX30OpeningRange[i].MidLineWidth == midLineWidth && cachePAX30OpeningRange[i].LevelsLineWidth == levelsLineWidth && cachePAX30OpeningRange[i].ShowMid == showMid && ((NinjaScriptBase)cachePAX30OpeningRange[i]).EqualsInput(input))
				{
					return cachePAX30OpeningRange[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<PAX30OpeningRange>(new PAX30OpeningRange
		{
			ORBStartSerialize = oRBStartSerialize,
			ORBEndPlotSerialize = oRBEndPlotSerialize,
			TextvertPixels = textvertPixels,
			TextHorzOffset = textHorzOffset,
			FontSize = fontSize,
			BoldFont = boldFont,
			LabelPrefix = labelPrefix,
			HighLineColor = highLineColor,
			LowLineColor = lowLineColor,
			MidLineColor = midLineColor,
			MainLineWidth = mainLineWidth,
			MidLineWidth = midLineWidth,
			LevelsLineWidth = levelsLineWidth,
			ShowMid = showMid
		}, input, ref cachePAX30OpeningRange);
	}

	public VWAP VWAP()
	{
		return VWAP(((NinjaScriptBase)this).Input);
	}

	public VWAP VWAP(ISeries<double> input)
	{
		if (cacheVWAP != null)
		{
			for (int i = 0; i < cacheVWAP.Length; i++)
			{
				if (cacheVWAP[i] != null && ((NinjaScriptBase)cacheVWAP[i]).EqualsInput(input))
				{
					return cacheVWAP[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<VWAP>(new VWAP(), input, ref cacheVWAP);
	}

	public TickRefresh TickRefresh(int refreshTimeInterval)
	{
		return TickRefresh(((NinjaScriptBase)this).Input, refreshTimeInterval);
	}

	public TickRefresh TickRefresh(ISeries<double> input, int refreshTimeInterval)
	{
		if (cacheTickRefresh != null)
		{
			for (int i = 0; i < cacheTickRefresh.Length; i++)
			{
				if (cacheTickRefresh[i] != null && cacheTickRefresh[i].RefreshTimeInterval == refreshTimeInterval && ((NinjaScriptBase)cacheTickRefresh[i]).EqualsInput(input))
				{
					return cacheTickRefresh[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<TickRefresh>(new TickRefresh
		{
			RefreshTimeInterval = refreshTimeInterval
		}, input, ref cacheTickRefresh);
	}

	public WoodiesCCI WoodiesCCI(int chopIndicatorWidth, int neutralBars, int period, int periodEma, int periodLinReg, int periodTurbo, int sideWinderLimit0, int sideWinderLimit1, int sideWinderWidth)
	{
		return WoodiesCCI(((NinjaScriptBase)this).Input, chopIndicatorWidth, neutralBars, period, periodEma, periodLinReg, periodTurbo, sideWinderLimit0, sideWinderLimit1, sideWinderWidth);
	}

	public WoodiesCCI WoodiesCCI(ISeries<double> input, int chopIndicatorWidth, int neutralBars, int period, int periodEma, int periodLinReg, int periodTurbo, int sideWinderLimit0, int sideWinderLimit1, int sideWinderWidth)
	{
		if (cacheWoodiesCCI != null)
		{
			for (int i = 0; i < cacheWoodiesCCI.Length; i++)
			{
				if (cacheWoodiesCCI[i] != null && cacheWoodiesCCI[i].ChopIndicatorWidth == chopIndicatorWidth && cacheWoodiesCCI[i].NeutralBars == neutralBars && cacheWoodiesCCI[i].Period == period && cacheWoodiesCCI[i].PeriodEma == periodEma && cacheWoodiesCCI[i].PeriodLinReg == periodLinReg && cacheWoodiesCCI[i].PeriodTurbo == periodTurbo && cacheWoodiesCCI[i].SideWinderLimit0 == sideWinderLimit0 && cacheWoodiesCCI[i].SideWinderLimit1 == sideWinderLimit1 && cacheWoodiesCCI[i].SideWinderWidth == sideWinderWidth && ((NinjaScriptBase)cacheWoodiesCCI[i]).EqualsInput(input))
				{
					return cacheWoodiesCCI[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WoodiesCCI>(new WoodiesCCI
		{
			ChopIndicatorWidth = chopIndicatorWidth,
			NeutralBars = neutralBars,
			Period = period,
			PeriodEma = periodEma,
			PeriodLinReg = periodLinReg,
			PeriodTurbo = periodTurbo,
			SideWinderLimit0 = sideWinderLimit0,
			SideWinderLimit1 = sideWinderLimit1,
			SideWinderWidth = sideWinderWidth
		}, input, ref cacheWoodiesCCI);
	}

	public WoodiesPivots WoodiesPivots(HLCCalculationModeWoodie priorDayHlc, int width)
	{
		return WoodiesPivots(((NinjaScriptBase)this).Input, priorDayHlc, width);
	}

	public WoodiesPivots WoodiesPivots(ISeries<double> input, HLCCalculationModeWoodie priorDayHlc, int width)
	{
		if (cacheWoodiesPivots != null)
		{
			for (int i = 0; i < cacheWoodiesPivots.Length; i++)
			{
				if (cacheWoodiesPivots[i] != null && cacheWoodiesPivots[i].PriorDayHlc == priorDayHlc && cacheWoodiesPivots[i].Width == width && ((NinjaScriptBase)cacheWoodiesPivots[i]).EqualsInput(input))
				{
					return cacheWoodiesPivots[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WoodiesPivots>(new WoodiesPivots
		{
			PriorDayHlc = priorDayHlc,
			Width = width
		}, input, ref cacheWoodiesPivots);
	}

	public WisemanAlligator WisemanAlligator(int jawPeriod, int teethPeriod, int lipsPeriod, int jawOffset, int teethOffset, int lipsOffset)
	{
		return WisemanAlligator(((NinjaScriptBase)this).Input, jawPeriod, teethPeriod, lipsPeriod, jawOffset, teethOffset, lipsOffset);
	}

	public WisemanAlligator WisemanAlligator(ISeries<double> input, int jawPeriod, int teethPeriod, int lipsPeriod, int jawOffset, int teethOffset, int lipsOffset)
	{
		if (cacheWisemanAlligator != null)
		{
			for (int i = 0; i < cacheWisemanAlligator.Length; i++)
			{
				if (cacheWisemanAlligator[i] != null && cacheWisemanAlligator[i].JawPeriod == jawPeriod && cacheWisemanAlligator[i].TeethPeriod == teethPeriod && cacheWisemanAlligator[i].LipsPeriod == lipsPeriod && cacheWisemanAlligator[i].JawOffset == jawOffset && cacheWisemanAlligator[i].TeethOffset == teethOffset && cacheWisemanAlligator[i].LipsOffset == lipsOffset && ((NinjaScriptBase)cacheWisemanAlligator[i]).EqualsInput(input))
				{
					return cacheWisemanAlligator[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WisemanAlligator>(new WisemanAlligator
		{
			JawPeriod = jawPeriod,
			TeethPeriod = teethPeriod,
			LipsPeriod = lipsPeriod,
			JawOffset = jawOffset,
			TeethOffset = teethOffset,
			LipsOffset = lipsOffset
		}, input, ref cacheWisemanAlligator);
	}

	public WisemanAwesomeOscillator WisemanAwesomeOscillator()
	{
		return WisemanAwesomeOscillator(((NinjaScriptBase)this).Input);
	}

	public WisemanAwesomeOscillator WisemanAwesomeOscillator(ISeries<double> input)
	{
		if (cacheWisemanAwesomeOscillator != null)
		{
			for (int i = 0; i < cacheWisemanAwesomeOscillator.Length; i++)
			{
				if (cacheWisemanAwesomeOscillator[i] != null && ((NinjaScriptBase)cacheWisemanAwesomeOscillator[i]).EqualsInput(input))
				{
					return cacheWisemanAwesomeOscillator[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WisemanAwesomeOscillator>(new WisemanAwesomeOscillator(), input, ref cacheWisemanAwesomeOscillator);
	}

	public WisemanFractal WisemanFractal(int strength, int triangleOffset)
	{
		return WisemanFractal(((NinjaScriptBase)this).Input, strength, triangleOffset);
	}

	public WisemanFractal WisemanFractal(ISeries<double> input, int strength, int triangleOffset)
	{
		if (cacheWisemanFractal != null)
		{
			for (int i = 0; i < cacheWisemanFractal.Length; i++)
			{
				if (cacheWisemanFractal[i] != null && cacheWisemanFractal[i].Strength == strength && cacheWisemanFractal[i].TriangleOffset == triangleOffset && ((NinjaScriptBase)cacheWisemanFractal[i]).EqualsInput(input))
				{
					return cacheWisemanFractal[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<WisemanFractal>(new WisemanFractal
		{
			Strength = strength,
			TriangleOffset = triangleOffset
		}, input, ref cacheWisemanFractal);
	}

	public OrderFlowCumulativeDelta OrderFlowCumulativeDelta(CumulativeDeltaType deltaType, CumulativeDeltaPeriod period, int sizeFilter)
	{
		return OrderFlowCumulativeDelta(((NinjaScriptBase)this).Input, deltaType, period, sizeFilter);
	}

	public OrderFlowCumulativeDelta OrderFlowCumulativeDelta(ISeries<double> input, CumulativeDeltaType deltaType, CumulativeDeltaPeriod period, int sizeFilter)
	{
		if (cacheOrderFlowCumulativeDelta != null)
		{
			for (int i = 0; i < cacheOrderFlowCumulativeDelta.Length; i++)
			{
				if (cacheOrderFlowCumulativeDelta[i] != null && cacheOrderFlowCumulativeDelta[i].DeltaType == deltaType && cacheOrderFlowCumulativeDelta[i].Period == period && cacheOrderFlowCumulativeDelta[i].SizeFilter == sizeFilter && ((NinjaScriptBase)cacheOrderFlowCumulativeDelta[i]).EqualsInput(input))
				{
					return cacheOrderFlowCumulativeDelta[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrderFlowCumulativeDelta>(new OrderFlowCumulativeDelta
		{
			DeltaType = deltaType,
			Period = period,
			SizeFilter = sizeFilter
		}, input, ref cacheOrderFlowCumulativeDelta);
	}

	public OrderFlowMarketDepthMap OrderFlowMarketDepthMap(BaseVolumeRange baseRange, int maxRange, int minRange, OpacityDistribution opacityDistribution, int depthMargin, bool extendLastKnown, bool showBidAskLine)
	{
		return OrderFlowMarketDepthMap(((NinjaScriptBase)this).Input, baseRange, maxRange, minRange, opacityDistribution, depthMargin, extendLastKnown, showBidAskLine);
	}

	public OrderFlowMarketDepthMap OrderFlowMarketDepthMap(ISeries<double> input, BaseVolumeRange baseRange, int maxRange, int minRange, OpacityDistribution opacityDistribution, int depthMargin, bool extendLastKnown, bool showBidAskLine)
	{
		if (cacheOrderFlowMarketDepthMap != null)
		{
			for (int i = 0; i < cacheOrderFlowMarketDepthMap.Length; i++)
			{
				if (cacheOrderFlowMarketDepthMap[i] != null && cacheOrderFlowMarketDepthMap[i].BaseRange == baseRange && cacheOrderFlowMarketDepthMap[i].MaxRange == maxRange && cacheOrderFlowMarketDepthMap[i].MinRange == minRange && cacheOrderFlowMarketDepthMap[i].OpacityDistribution == opacityDistribution && cacheOrderFlowMarketDepthMap[i].DepthMargin == depthMargin && cacheOrderFlowMarketDepthMap[i].ExtendLastKnown == extendLastKnown && cacheOrderFlowMarketDepthMap[i].ShowBidAskLine == showBidAskLine && ((NinjaScriptBase)cacheOrderFlowMarketDepthMap[i]).EqualsInput(input))
				{
					return cacheOrderFlowMarketDepthMap[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrderFlowMarketDepthMap>(new OrderFlowMarketDepthMap
		{
			BaseRange = baseRange,
			MaxRange = maxRange,
			MinRange = minRange,
			OpacityDistribution = opacityDistribution,
			DepthMargin = depthMargin,
			ExtendLastKnown = extendLastKnown,
			ShowBidAskLine = showBidAskLine
		}, input, ref cacheOrderFlowMarketDepthMap);
	}

	public OrderFlowVWAP OrderFlowVWAP(VWAPResolution resolution, TradingHours tradingHoursInstance, VWAPStandardDeviations numStandardDeviations, double sD1Multiplier, double sD2Multiplier, double sD3Multiplier)
	{
		return OrderFlowVWAP(((NinjaScriptBase)this).Input, resolution, tradingHoursInstance, numStandardDeviations, sD1Multiplier, sD2Multiplier, sD3Multiplier);
	}

	public OrderFlowVWAP OrderFlowVWAP(ISeries<double> input, VWAPResolution resolution, TradingHours tradingHoursInstance, VWAPStandardDeviations numStandardDeviations, double sD1Multiplier, double sD2Multiplier, double sD3Multiplier)
	{
		if (cacheOrderFlowVWAP != null)
		{
			for (int i = 0; i < cacheOrderFlowVWAP.Length; i++)
			{
				if (cacheOrderFlowVWAP[i] != null && cacheOrderFlowVWAP[i].Resolution == resolution && cacheOrderFlowVWAP[i].TradingHoursInstance == tradingHoursInstance && cacheOrderFlowVWAP[i].NumStandardDeviations == numStandardDeviations && cacheOrderFlowVWAP[i].SD1Multiplier == sD1Multiplier && cacheOrderFlowVWAP[i].SD2Multiplier == sD2Multiplier && cacheOrderFlowVWAP[i].SD3Multiplier == sD3Multiplier && ((NinjaScriptBase)cacheOrderFlowVWAP[i]).EqualsInput(input))
				{
					return cacheOrderFlowVWAP[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrderFlowVWAP>(new OrderFlowVWAP
		{
			Resolution = resolution,
			TradingHoursInstance = tradingHoursInstance,
			NumStandardDeviations = numStandardDeviations,
			SD1Multiplier = sD1Multiplier,
			SD2Multiplier = sD2Multiplier,
			SD3Multiplier = sD3Multiplier
		}, input, ref cacheOrderFlowVWAP);
	}

	public OrderFlowTradeDetector OrderFlowTradeDetector(TradeDetectorBaseLargeVolumeOn baseLargeVolumeOn, int minimumVolumeForMarker, int maximumMarkerSize, TradeDetectorSizeBase baseMarkerSizeOn, bool hoverValues)
	{
		return OrderFlowTradeDetector(((NinjaScriptBase)this).Input, baseLargeVolumeOn, minimumVolumeForMarker, maximumMarkerSize, baseMarkerSizeOn, hoverValues);
	}

	public OrderFlowTradeDetector OrderFlowTradeDetector(ISeries<double> input, TradeDetectorBaseLargeVolumeOn baseLargeVolumeOn, int minimumVolumeForMarker, int maximumMarkerSize, TradeDetectorSizeBase baseMarkerSizeOn, bool hoverValues)
	{
		if (cacheOrderFlowTradeDetector != null)
		{
			for (int i = 0; i < cacheOrderFlowTradeDetector.Length; i++)
			{
				if (cacheOrderFlowTradeDetector[i] != null && cacheOrderFlowTradeDetector[i].BaseLargeVolumeOn == baseLargeVolumeOn && cacheOrderFlowTradeDetector[i].MinimumVolumeForMarker == minimumVolumeForMarker && cacheOrderFlowTradeDetector[i].MaximumMarkerSize == maximumMarkerSize && cacheOrderFlowTradeDetector[i].BaseMarkerSizeOn == baseMarkerSizeOn && cacheOrderFlowTradeDetector[i].HoverValues == hoverValues && ((NinjaScriptBase)cacheOrderFlowTradeDetector[i]).EqualsInput(input))
				{
					return cacheOrderFlowTradeDetector[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrderFlowTradeDetector>(new OrderFlowTradeDetector
		{
			BaseLargeVolumeOn = baseLargeVolumeOn,
			MinimumVolumeForMarker = minimumVolumeForMarker,
			MaximumMarkerSize = maximumMarkerSize,
			BaseMarkerSizeOn = baseMarkerSizeOn,
			HoverValues = hoverValues
		}, input, ref cacheOrderFlowTradeDetector);
	}

	public OrderFlowVolumeProfile OrderFlowVolumeProfile(MarketProfileType profileType, MarketProfilePeriod profilePeriod, int sessions, TradingHours tradingHoursInstance, MarketProfileResolution resolution, int valueAreaPercent, int initialBalanceMinutes)
	{
		return OrderFlowVolumeProfile(((NinjaScriptBase)this).Input, profileType, profilePeriod, sessions, tradingHoursInstance, resolution, valueAreaPercent, initialBalanceMinutes);
	}

	public OrderFlowVolumeProfile OrderFlowVolumeProfile(ISeries<double> input, MarketProfileType profileType, MarketProfilePeriod profilePeriod, int sessions, TradingHours tradingHoursInstance, MarketProfileResolution resolution, int valueAreaPercent, int initialBalanceMinutes)
	{
		if (cacheOrderFlowVP != null)
		{
			for (int i = 0; i < cacheOrderFlowVP.Length; i++)
			{
				if (cacheOrderFlowVP[i] != null && cacheOrderFlowVP[i].ProfileType == profileType && cacheOrderFlowVP[i].TradingHoursInstance == tradingHoursInstance && cacheOrderFlowVP[i].ProfilePeriod == profilePeriod && cacheOrderFlowVP[i].ProfilePeriodValue == sessions && cacheOrderFlowVP[i].Resolution == resolution && cacheOrderFlowVP[i].ValueArea == valueAreaPercent && cacheOrderFlowVP[i].InitialBalanceMinutes == initialBalanceMinutes && ((NinjaScriptBase)cacheOrderFlowVP[i]).EqualsInput(input))
				{
					return cacheOrderFlowVP[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OrderFlowVolumeProfile>(new OrderFlowVolumeProfile
		{
			ProfileType = profileType,
			ProfilePeriod = profilePeriod,
			ProfilePeriodValue = sessions,
			TradingHoursInstance = tradingHoursInstance,
			Resolution = resolution,
			ValueArea = valueAreaPercent,
			InitialBalanceMinutes = initialBalanceMinutes
		}, input, ref cacheOrderFlowVP);
	}

	public OTMDeltaBarFree OTMDeltaBarFree()
	{
		return OTMDeltaBarFree(((NinjaScriptBase)this).Input);
	}

	public OTMDeltaBarFree OTMDeltaBarFree(ISeries<double> input)
	{
		if (cacheOTMDeltaBarFree != null)
		{
			for (int i = 0; i < cacheOTMDeltaBarFree.Length; i++)
			{
				if (((NinjaScriptBase)cacheOTMDeltaBarFree[i]).EqualsInput(input))
				{
					return cacheOTMDeltaBarFree[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<OTMDeltaBarFree>(new OTMDeltaBarFree(), input, ref cacheOTMDeltaBarFree);
	}

	public T3000_LagDetector T3000_LagDetector()
	{
		return T3000_LagDetector(((NinjaScriptBase)this).Input);
	}

	public T3000_MGI_Daily T3000_MGI_Daily()
	{
		return T3000_MGI_Daily(((NinjaScriptBase)this).Input);
	}

	public T3000_MGI_Monthly T3000_MGI_Monthly()
	{
		return T3000_MGI_Monthly(((NinjaScriptBase)this).Input);
	}

	public T3000_MGI_Statistics T3000_MGI_Statistics()
	{
		return T3000_MGI_Statistics(((NinjaScriptBase)this).Input);
	}

	public T3000_MGI_Weekly T3000_MGI_Weekly()
	{
		return T3000_MGI_Weekly(((NinjaScriptBase)this).Input);
	}

	public T3000_LagDetector T3000_LagDetector(ISeries<double> input)
	{
		if (cacheT3000_LagDetector != null)
		{
			for (int i = 0; i < cacheT3000_LagDetector.Length; i++)
			{
				if (((NinjaScriptBase)cacheT3000_LagDetector[i]).EqualsInput(input))
				{
					return cacheT3000_LagDetector[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<T3000_LagDetector>(new T3000_LagDetector(), input, ref cacheT3000_LagDetector);
	}

	public T3000_MGI_Daily T3000_MGI_Daily(ISeries<double> input)
	{
		if (cacheT3000_MGI_Daily != null)
		{
			for (int i = 0; i < cacheT3000_MGI_Daily.Length; i++)
			{
				if (((NinjaScriptBase)cacheT3000_MGI_Daily[i]).EqualsInput(input))
				{
					return cacheT3000_MGI_Daily[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<T3000_MGI_Daily>(new T3000_MGI_Daily(), input, ref cacheT3000_MGI_Daily);
	}

	public T3000_MGI_Monthly T3000_MGI_Monthly(ISeries<double> input)
	{
		if (cacheT3000_MGI_Monthly != null)
		{
			for (int i = 0; i < cacheT3000_MGI_Monthly.Length; i++)
			{
				if (((NinjaScriptBase)cacheT3000_MGI_Monthly[i]).EqualsInput(input))
				{
					return cacheT3000_MGI_Monthly[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<T3000_MGI_Monthly>(new T3000_MGI_Monthly(), input, ref cacheT3000_MGI_Monthly);
	}

	public T3000_MGI_Statistics T3000_MGI_Statistics(ISeries<double> input)
	{
		if (cacheT3000_MGI_Statistics != null)
		{
			for (int i = 0; i < cacheT3000_MGI_Statistics.Length; i++)
			{
				if (((NinjaScriptBase)cacheT3000_MGI_Statistics[i]).EqualsInput(input))
				{
					return cacheT3000_MGI_Statistics[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<T3000_MGI_Statistics>(new T3000_MGI_Statistics(), input, ref cacheT3000_MGI_Statistics);
	}

	public T3000_MGI_Weekly T3000_MGI_Weekly(ISeries<double> input)
	{
		if (cacheT3000_MGI_Weekly != null)
		{
			for (int i = 0; i < cacheT3000_MGI_Weekly.Length; i++)
			{
				if (((NinjaScriptBase)cacheT3000_MGI_Weekly[i]).EqualsInput(input))
				{
					return cacheT3000_MGI_Weekly[i];
				}
			}
		}
		return ((IndicatorBase)this).CacheIndicator<T3000_MGI_Weekly>(new T3000_MGI_Weekly(), input, ref cacheT3000_MGI_Weekly);
	}
}

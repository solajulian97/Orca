using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.StrategyGenerator;

namespace NinjaTrader.NinjaScript.Optimizers;

public class StrategyGenerator : Optimizer
{
	private int oldKeepBestResults = -1;

	private ChartPattern[] selectedCandleStickPattern;

	private Type[] selectedIndicatorTypes;

	public static ChartPattern[] AvailableCandleStickPattern = Enum.GetValues(typeof(ChartPattern)).Cast<ChartPattern>().ToArray();

	internal static Dictionary<Type, Tuple<double, double>> AvailableIndicators = new Dictionary<Type, Tuple<double, double>>
	{
		{
			typeof(ADL),
			null
		},
		{
			typeof(ADX),
			new Tuple<double, double>(0.0, 100.0)
		},
		{
			typeof(ADXR),
			new Tuple<double, double>(0.0, 100.0)
		},
		{
			typeof(APZ),
			null
		},
		{
			typeof(Aroon),
			new Tuple<double, double>(0.0, 100.0)
		},
		{
			typeof(AroonOscillator),
			new Tuple<double, double>(-100.0, 100.0)
		},
		{
			typeof(ATR),
			null
		},
		{
			typeof(Bollinger),
			null
		},
		{
			typeof(BOP),
			null
		},
		{
			typeof(CCI),
			null
		},
		{
			typeof(ChaikinMoneyFlow),
			new Tuple<double, double>(-1.0, 1.0)
		},
		{
			typeof(ChaikinOscillator),
			null
		},
		{
			typeof(ChaikinVolatility),
			null
		},
		{
			typeof(CMO),
			new Tuple<double, double>(-100.0, 100.0)
		},
		{
			typeof(DM),
			new Tuple<double, double>(-100.0, 100.0)
		},
		{
			typeof(DMI),
			new Tuple<double, double>(-100.0, 100.0)
		},
		{
			typeof(EMA),
			null
		},
		{
			typeof(FisherTransform),
			null
		},
		{
			typeof(FOSC),
			null
		},
		{
			typeof(HMA),
			null
		},
		{
			typeof(KAMA),
			null
		},
		{
			typeof(KeltnerChannel),
			null
		},
		{
			typeof(KeyReversalDown),
			new Tuple<double, double>(0.0, 1.0)
		},
		{
			typeof(KeyReversalUp),
			new Tuple<double, double>(0.0, 1.0)
		},
		{
			typeof(LinReg),
			null
		},
		{
			typeof(LinRegIntercept),
			null
		},
		{
			typeof(LinRegSlope),
			null
		},
		{
			typeof(MACD),
			null
		},
		{
			typeof(MAEnvelopes),
			null
		},
		{
			typeof(MAMA),
			null
		},
		{
			typeof(MFI),
			new Tuple<double, double>(0.0, 100.0)
		},
		{
			typeof(Momentum),
			null
		},
		{
			typeof(MoneyFlowOscillator),
			new Tuple<double, double>(-1.0, 1.0)
		},
		{
			typeof(MovingAverageRibbon),
			null
		},
		{
			typeof(NBarsDown),
			new Tuple<double, double>(0.0, 1.0)
		},
		{
			typeof(NBarsUp),
			new Tuple<double, double>(0.0, 1.0)
		},
		{
			typeof(OBV),
			null
		},
		{
			typeof(ParabolicSAR),
			null
		},
		{
			typeof(PFE),
			new Tuple<double, double>(-100.0, 100.0)
		},
		{
			typeof(Pivots),
			null
		},
		{
			typeof(PPO),
			null
		},
		{
			typeof(PriceOscillator),
			null
		},
		{
			typeof(Range),
			null
		},
		{
			typeof(RelativeVigorIndex),
			null
		},
		{
			typeof(RIND),
			null
		},
		{
			typeof(ROC),
			null
		},
		{
			typeof(RSI),
			new Tuple<double, double>(0.0, 100.0)
		},
		{
			typeof(RSquared),
			new Tuple<double, double>(0.0, 1.0)
		},
		{
			typeof(RSS),
			new Tuple<double, double>(0.0, 100.0)
		},
		{
			typeof(RVI),
			new Tuple<double, double>(0.0, 100.0)
		},
		{
			typeof(SMA),
			null
		},
		{
			typeof(StdDev),
			null
		},
		{
			typeof(StdError),
			null
		},
		{
			typeof(Stochastics),
			new Tuple<double, double>(0.0, 100.0)
		},
		{
			typeof(StochasticsFast),
			new Tuple<double, double>(0.0, 100.0)
		},
		{
			typeof(StochRSI),
			new Tuple<double, double>(0.0, 100.0)
		},
		{
			typeof(Swing),
			null
		},
		{
			typeof(T3),
			null
		},
		{
			typeof(TEMA),
			null
		},
		{
			typeof(TMA),
			null
		},
		{
			typeof(TRIX),
			null
		},
		{
			typeof(TSF),
			null
		},
		{
			typeof(TSI),
			new Tuple<double, double>(-100.0, 100.0)
		},
		{
			typeof(UltimateOscillator),
			new Tuple<double, double>(0.0, 100.0)
		},
		{
			typeof(VMA),
			null
		},
		{
			typeof(VOL),
			null
		},
		{
			typeof(VOLMA),
			null
		},
		{
			typeof(VolumeOscillator),
			null
		},
		{
			typeof(VROC),
			null
		},
		{
			typeof(VWMA),
			null
		},
		{
			typeof(WilliamsR),
			new Tuple<double, double>(-100.0, 0.0)
		},
		{
			typeof(WMA),
			null
		},
		{
			typeof(ZLEMA),
			null
		}
	};

	public override bool IsStrategyGenerator => true;

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorProperties", Name = "NinjaScriptGeneticOptimizerGenerations", Order = 40)]
	[Range(1, int.MaxValue)]
	public int Generations { get; set; }

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorProperties", Name = "NinjaScriptGeneticOptimizerGenerationSize", Order = 50)]
	[Range(1, int.MaxValue)]
	public int GenerationSize { get; set; }

	public bool OptimizeEntries
	{
		get
		{
			if (!UseCandleStickPatternForEntries && !UseDayOfWeekForEntries && !UseIndicatorsForEntries)
			{
				return UseSessionTimeForEntries;
			}
			return true;
		}
	}

	public bool OptimizeExits
	{
		get
		{
			if (!UseCandleStickPatternForExits && !UseDayOfWeekForExits && !UseIndicatorsForExits && !UseParabolicStopForExits && !UseSessionTimeForExits && !UseStopTargetsForExits)
			{
				return UseSessionCloseForExits;
			}
			return true;
		}
	}

	[PropertyEditor("NinjaTrader.Gui.Tools.AvailableCandleStickPatternListEditor")]
	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorProperties", Name = "NinjaScriptStrategyGeneratorUseCandleStickPattern", Order = 10, Prompt = "NinjaScriptStrategyGeneratorCandleStickPatternPrompt")]
	public ChartPattern[] SelectedCandleStickPattern
	{
		get
		{
			return selectedCandleStickPattern ?? (selectedCandleStickPattern = AvailableCandleStickPattern);
		}
		set
		{
			selectedCandleStickPattern = value;
		}
	}

	[PropertyEditor("NinjaTrader.Gui.Tools.AvailableIndicatorsListEditor")]
	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorProperties", Name = "NinjaScriptStrategyGeneratorUseIndicators", Order = 0, Prompt = "NinjaScriptStrategyGeneratorIndicatorsPrompt")]
	public Type[] SelectedIndicatorTypes
	{
		get
		{
			if (selectedIndicatorTypes == null)
			{
				selectedIndicatorTypes = AvailableIndicators.Keys.ToArray();
			}
			return selectedIndicatorTypes;
		}
		set
		{
			selectedIndicatorTypes = value;
		}
	}

	/// <summary>
	/// Abort if for N generations there was no improvement on the average performance of the 'best results to keep'. Set to '0' to disable
	/// </summary>
	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorProperties", Name = "NinjaScriptGeneticOptimizerThresholdGenerations", Order = 60)]
	[Range(0, int.MaxValue)]
	public int ThresholdGenerations { get; set; }

	[Browsable(false)]
	public bool UseCandleStickPatternForEntries { get; set; }

	[Browsable(false)]
	public bool UseCandleStickPatternForExits { get; set; }

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorEntries", Name = "NinjaScriptStrategyGeneratorDayOfWeek", Order = 20)]
	public bool UseDayOfWeekForEntries { get; set; }

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorExits", Name = "NinjaScriptStrategyGeneratorDayOfWeek", Order = 2)]
	public bool UseDayOfWeekForExits { get; set; }

	[Browsable(false)]
	public bool UseIndicatorsForEntries { get; set; }

	[Browsable(false)]
	public bool UseIndicatorsForExits { get; set; }

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorExits", Name = "NinjaScriptStrategyGeneratorUseParabolicStop", Order = 4)]
	public bool UseParabolicStopForExits { get; set; }

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorExits", Name = "NinjaScriptStrategyGeneratorUseSessionClose", Order = 6)]
	public bool UseSessionCloseForExits { get; set; }

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorEntries", Name = "NinjaScriptStrategyGeneratorUseSessionTime", Order = 30)]
	public bool UseSessionTimeForEntries { get; set; }

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorExits", Name = "NinjaScriptStrategyGeneratorUseSessionTime", Order = 3)]
	public bool UseSessionTimeForExits { get; set; }

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptStrategyGeneratorExits", Name = "NinjaScriptStrategyGeneratorUseStopTargets", Order = 5)]
	public bool UseStopTargetsForExits { get; set; }

	public override void CopyTo(NinjaScript ninjaScript)
	{
		((NinjaScript)this).CopyTo(ninjaScript);
		if (ninjaScript is StrategyGenerator strategyGenerator)
		{
			strategyGenerator.oldKeepBestResults = oldKeepBestResults;
		}
	}

	private List<SystemPerformance> GetUniqueResults()
	{
		SystemPerformance[] results = ((Optimizer)this).Results;
		foreach (SystemPerformance val in results)
		{
			if (val.ParameterValues != null)
			{
				if (val.LongTrades.TradesCount == 0 && val.ShortTrades.TradesCount != 0 && val.ParameterValues[0] is GeneratedStrategyLogic generatedStrategyLogic)
				{
					generatedStrategyLogic.EnterLongCondition = null;
					generatedStrategyLogic.ExitLongCondition = null;
					generatedStrategyLogic.SessionMinutesForLongEntries = -1;
					generatedStrategyLogic.SessionMinutesForLongExits = -1;
					generatedStrategyLogic.SessionMinutesOffsetForLongEntries = -1;
					generatedStrategyLogic.SessionMinutesOffsetForLongExits = -1;
				}
				if (val.ShortTrades.TradesCount == 0 && val.LongTrades.TradesCount != 0 && val.ParameterValues[0] is GeneratedStrategyLogic generatedStrategyLogic2)
				{
					generatedStrategyLogic2.EnterShortCondition = null;
					generatedStrategyLogic2.ExitShortCondition = null;
					generatedStrategyLogic2.SessionMinutesForShortEntries = -1;
					generatedStrategyLogic2.SessionMinutesForShortExits = -1;
					generatedStrategyLogic2.SessionMinutesOffsetForShortEntries = -1;
					generatedStrategyLogic2.SessionMinutesOffsetForShortExits = -1;
				}
			}
		}
		List<SystemPerformance> list = new List<SystemPerformance>();
		results = ((Optimizer)this).Results;
		foreach (SystemPerformance result in results)
		{
			List<SystemPerformance> list2 = ((Optimizer)this).Results.Where((SystemPerformance r) => result.ParameterValues != null && MathExtentions.ApproxCompare(result.PerformanceValue, r.PerformanceValue) == 0 && MathExtentions.ApproxCompare(result.AllTrades.TradesPerformance.Percent.CumProfit, r.AllTrades.TradesPerformance.Percent.CumProfit) == 0 && result.AllTrades.LosingTrades.TradesCount == r.AllTrades.LosingTrades.TradesCount && result.AllTrades.WinningTrades.TradesCount == r.AllTrades.WinningTrades.TradesCount).ToList();
			if (list2.Count != 0)
			{
				list2.Sort((SystemPerformance a, SystemPerformance b) => (a.ParameterValues[0] as GeneratedStrategyLogic).NumNodes.CompareTo((b.ParameterValues[0] as GeneratedStrategyLogic).NumNodes));
				if (!list.Contains(list2[0]))
				{
					list.Add(list2[0]);
				}
			}
		}
		return list;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Invalid comparison between Unknown and I4
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			Generations = 50;
			GenerationSize = 100;
			((NinjaScript)this).Name = Resource.NinjaScriptStrategyGenerator;
			ThresholdGenerations = 10;
			UseCandleStickPatternForEntries = true;
			UseCandleStickPatternForExits = true;
			UseDayOfWeekForEntries = false;
			UseDayOfWeekForExits = false;
			UseIndicatorsForEntries = true;
			UseIndicatorsForExits = true;
			UseParabolicStopForExits = true;
			UseSessionCloseForExits = false;
			UseSessionTimeForEntries = false;
			UseSessionTimeForExits = false;
			UseStopTargetsForExits = true;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			if (oldKeepBestResults < 0)
			{
				oldKeepBestResults = ((Optimizer)this).KeepBestResults;
			}
			((Optimizer)this).KeepBestResults = GenerationSize;
			((Optimizer)this).NumberOfIterations = Generations * GenerationSize;
			if (SelectedCandleStickPattern.Length == 0)
			{
				UseCandleStickPatternForEntries = false;
				UseCandleStickPatternForExits = false;
			}
			if (SelectedIndicatorTypes.Length == 0)
			{
				UseIndicatorsForEntries = false;
				UseIndicatorsForExits = false;
			}
		}
		else if ((int)((NinjaScript)this).State == 8 && oldKeepBestResults > 0)
		{
			((Optimizer)this).KeepBestResults = oldKeepBestResults;
			oldKeepBestResults = -1;
		}
	}

	protected override void OnOptimize()
	{
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_0537: Unknown result type (might be due to invalid IL or missing references)
		//IL_053e: Expected O, but got Unknown
		if (!OptimizeEntries && !OptimizeExits)
		{
			throw new ArgumentException(Resource.NinjaScriptStrategyGeneratorEntriesOrExits);
		}
		List<SystemPerformance> list = new List<SystemPerformance>();
		double num = double.MinValue;
		int num2 = 0;
		List<GeneratedStrategyLogic> list2 = new List<GeneratedStrategyLogic>();
		Random random = new Random();
		bool flag = true;
		int num3 = (int)((double)GenerationSize * 0.2);
		int num4 = Math.Max(0, Math.Min(GenerationSize - num3, (int)((double)GenerationSize * 0.2)));
		int num5 = Math.Max(0, Math.Min(GenerationSize - num3 - num4, (int)((double)GenerationSize * 0.2)));
		int num6 = Math.Max(0, Math.Min(GenerationSize - num3 - num4 - num5, (int)((double)GenerationSize * 0.2)));
		List<string> list3 = new List<string>();
		((Optimizer)this).Strategies[0].IncludeTradeHistoryInBacktest = true;
		((Optimizer)this).Strategies[0].IsInstantiatedOnEachOptimizationIteration = true;
		((Optimizer)this).Strategies[0].SupportsOptimizationGraph = false;
		int num7 = 0;
		while (true)
		{
			if (num7 == 0)
			{
				for (int i = 0; i < GenerationSize; i++)
				{
					list2.Add(new GeneratedStrategyLogic
					{
						StrategyGenerator = this
					}.NewRandom(random));
				}
			}
			else
			{
				List<SystemPerformance> list4 = new List<SystemPerformance>();
				SystemPerformance systemPerformance = ((Optimizer)this).Strategies[0].SystemPerformance;
				double num8 = 0.0;
				List<SystemPerformance> uniqueResults = GetUniqueResults();
				if (!((Optimizer)this).Strategies[0].IsAggregated)
				{
					uniqueResults.Sort((SystemPerformance a, SystemPerformance b) => b.PerformanceValue.CompareTo(a.PerformanceValue));
					if (((Optimizer)this).Progress != null)
					{
						for (int num9 = 0; num9 < Math.Min(oldKeepBestResults, uniqueResults.Count); num9++)
						{
							num8 += uniqueResults[num9].PerformanceValue;
						}
						((Optimizer)this).Progress.Message = string.Format(Resource.NinjaScriptStrategyGeneratorPeformance, ((NinjaScriptBase)((Optimizer)this).Strategies[0]).Instrument.FullName, (num8 / (double)Math.Min(oldKeepBestResults, uniqueResults.Count)).ToString("#.00", Globals.GeneralOptions.CurrentCulture));
					}
				}
				foreach (SystemPerformance item2 in uniqueResults)
				{
					if (num7 < Generations - 1)
					{
						double num10 = 1.645;
						List<double> list5 = new List<double>();
						foreach (SystemPerformance item3 in new MonteCarlo
						{
							NumberOfTrades = ((Collection<Trade>)(object)item2.AllTrades).Count
						}.Run((ICollection<Trade>)item2.AllTrades, (IProgress)null))
						{
							((Optimizer)this).Strategies[0].SystemPerformance = item3;
							((Optimizer)this).Strategies[0].OptimizationFitness.CalculatePerformanceValue(((Optimizer)this).Strategies[0]);
							list5.Add(((Optimizer)this).Strategies[0].OptimizationFitness.Value);
						}
						if (list5.Count == 0)
						{
							continue;
						}
						double mean = list5.Sum() / (double)list5.Count;
						double num11 = Math.Sqrt(list5.Sum((double r) => (r - mean) * (r - mean)) / (double)list5.Count);
						item2.PerformanceValue = mean - num10 * num11 / Math.Sqrt(((Collection<Trade>)(object)item2.AllTrades).Count);
						if (MathExtentions.ApproxCompare(item2.PerformanceValue, (item2.ParameterValues[0] as GeneratedStrategyLogic).PriorPerformance) < 0)
						{
							(item2.ParameterValues[0] as GeneratedStrategyLogic).TryLinearMutation = false;
						}
					}
					(item2.ParameterValues[0] as GeneratedStrategyLogic).PriorPerformance = item2.PerformanceValue;
					list4.Add(item2);
				}
				list4.Sort((SystemPerformance a, SystemPerformance b) => b.PerformanceValue.CompareTo(a.PerformanceValue));
				((Optimizer)this).Strategies[0].SystemPerformance = systemPerformance;
				bool flag2 = num7 >= Generations;
				double num12 = 0.0;
				for (int num13 = 0; num13 < Math.Min(num3, list4.Count); num13++)
				{
					num12 += list4[num13].PerformanceValue;
				}
				if (list4.Count == 0 || num12 / (double)Math.Min(num3, list4.Count) < num)
				{
					if (ThresholdGenerations > 0 && ++num2 > ThresholdGenerations - 1)
					{
						NinjaScript.Log(string.Format(Resource.NinjaScriptStrategyGeneratorTerminated, num7, ThresholdGenerations), (LogLevel)1);
						flag2 = true;
					}
				}
				else if (list4.Count > 0)
				{
					num = num12 / (double)Math.Min(num3, list4.Count);
					num2 = 0;
					list.Clear();
					foreach (SystemPerformance item4 in uniqueResults)
					{
						SystemPerformance val = new SystemPerformance(false);
						item4.CopyPerformance(val);
						val.ParameterValues[0] = ((GeneratedStrategyLogicBase)(item4.ParameterValues[0] as GeneratedStrategyLogic)).Clone();
						list.Add(val);
					}
				}
				if (flag2)
				{
					((Optimizer)this).Results = list.ToArray();
					break;
				}
				flag = list4.Count < ((Optimizer)this).Results.Length;
				List<GeneratedStrategyLogic> list6 = new List<GeneratedStrategyLogic>();
				for (int num14 = 0; num14 < Math.Min(num3, list4.Count); num14++)
				{
					list6.Add(list4[num14].ParameterValues[0] as GeneratedStrategyLogic);
				}
				long[] stableIds = list6.Select((GeneratedStrategyLogic p) => p.Id).ToArray();
				list6.AddRange((from p in list4
					where !stableIds.Contains((p.ParameterValues[0] as GeneratedStrategyLogic).Id)
					select p.ParameterValues[0] as GeneratedStrategyLogic).ToArray());
				while (list6.Count < GenerationSize)
				{
					GeneratedStrategyLogic item;
					while (list3.Contains(((GeneratedStrategyLogicBase)(item = new GeneratedStrategyLogic
					{
						StrategyGenerator = this
					}.NewRandom(random))).ToString((StrategyBase)null)))
					{
					}
					list6.Add(item);
				}
				list2 = list6;
				((Optimizer)this).Reset((!flag) ? num3 : 0);
				for (int num15 = 0; num15 < list2.Count; num15++)
				{
					GeneratedStrategyLogic generatedStrategyLogic;
					if (num15 < num3)
					{
						if (flag)
						{
							list2[num15] = ((GeneratedStrategyLogicBase)list2[num15]).Clone() as GeneratedStrategyLogic;
						}
						generatedStrategyLogic = list2[num15];
					}
					else if (num15 < num3 + num4)
					{
						int num16 = 0;
						while (list3.Contains(((GeneratedStrategyLogicBase)(generatedStrategyLogic = list2[num15 - num3].NewMutation(random))).ToString((StrategyBase)null)))
						{
							if (num16 == 10)
							{
								while (list3.Contains(((GeneratedStrategyLogicBase)(generatedStrategyLogic = new GeneratedStrategyLogic
								{
									StrategyGenerator = this
								}.NewRandom(random))).ToString((StrategyBase)null)))
								{
								}
								break;
							}
							list2[num15 - num3].TryLinearMutation = false;
							num16++;
						}
					}
					else if (num15 < num3 + num4 + num5)
					{
						int num17 = 0;
						while (list3.Contains(((GeneratedStrategyLogicBase)(generatedStrategyLogic = list2[num15].NewMutation(random))).ToString((StrategyBase)null)))
						{
							if (num17 == 10)
							{
								while (list3.Contains(((GeneratedStrategyLogicBase)(generatedStrategyLogic = new GeneratedStrategyLogic
								{
									StrategyGenerator = this
								}.NewRandom(random))).ToString((StrategyBase)null)))
								{
								}
								break;
							}
							list2[num15].TryLinearMutation = false;
							num17++;
						}
					}
					else if (num15 < num3 + num4 + num5 + num6)
					{
						int num18 = 0;
						while (true)
						{
							GeneratedStrategyLogic generatedStrategyLogic2 = list2[random.Next(num3)];
							if (list2[num15].IsLong == generatedStrategyLogic2.IsLong && list2[num15].IsShort == generatedStrategyLogic2.IsShort)
							{
								int num19 = 0;
								while (list3.Contains(((GeneratedStrategyLogicBase)(generatedStrategyLogic = list2[num15].NewCrossOver(generatedStrategyLogic2, random))).ToString((StrategyBase)null)))
								{
									if (num19 >= GenerationSize)
									{
										while (list3.Contains(((GeneratedStrategyLogicBase)(generatedStrategyLogic = new GeneratedStrategyLogic
										{
											StrategyGenerator = this
										}.NewRandom(random))).ToString((StrategyBase)null)))
										{
										}
										break;
									}
									num19++;
								}
								break;
							}
							if (num18 >= num3)
							{
								while (list3.Contains(((GeneratedStrategyLogicBase)(generatedStrategyLogic = new GeneratedStrategyLogic
								{
									StrategyGenerator = this
								}.NewRandom(random))).ToString((StrategyBase)null)))
								{
								}
								break;
							}
							num18++;
						}
					}
					else
					{
						while (list3.Contains(((GeneratedStrategyLogicBase)(generatedStrategyLogic = new GeneratedStrategyLogic
						{
							StrategyGenerator = this
						}.NewRandom(random))).ToString((StrategyBase)null)))
						{
						}
					}
					list3.Add(((GeneratedStrategyLogicBase)generatedStrategyLogic).ToString((StrategyBase)null));
					list2[num15] = generatedStrategyLogic;
				}
			}
			for (int num20 = 0; num20 < list2.Count; num20++)
			{
				if (!flag && num7 > 0 && num20 < Math.Min(oldKeepBestResults, num3))
				{
					IProgress progress = ((Optimizer)this).Progress;
					if (progress != null)
					{
						progress.PerformStep();
					}
				}
				else
				{
					((Optimizer)this).Strategies[0].GeneratedStrategyLogic = (GeneratedStrategyLogicBase)(object)list2[num20];
					((Optimizer)this).RunIteration();
				}
			}
			((Optimizer)this).WaitForIterationsCompleted();
			IProgress progress2 = ((Optimizer)this).Progress;
			if (progress2 != null && progress2.IsAborted)
			{
				((Optimizer)this).Results = list.ToArray();
				break;
			}
			num7++;
		}
		List<SystemPerformance> uniqueResults2 = GetUniqueResults();
		((Optimizer)this).KeepBestResults = oldKeepBestResults;
		((Optimizer)this).Results = (SystemPerformance[])(object)new SystemPerformance[Math.Min(((Optimizer)this).KeepBestResults, uniqueResults2.Count)];
		uniqueResults2.Sort((SystemPerformance a, SystemPerformance b) => b.PerformanceValue.CompareTo(a.PerformanceValue));
		Array.Copy(uniqueResults2.ToArray(), ((Optimizer)this).Results, Math.Min(((Optimizer)this).KeepBestResults, uniqueResults2.Count));
	}
}

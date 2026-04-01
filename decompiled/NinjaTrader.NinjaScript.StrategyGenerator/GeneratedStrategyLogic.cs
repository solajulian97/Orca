using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Optimizers;

namespace NinjaTrader.NinjaScript.StrategyGenerator;

public sealed class GeneratedStrategyLogic : GeneratedStrategyLogicBase
{
	private CandleStickPatternLogic candleStickPatternLogic;

	private static readonly int daysOfWeekCount = Enum.GetValues(typeof(DayOfWeek)).Length;

	private DateTime endTimeForLongEntries;

	private DateTime endTimeForLongExits;

	private DateTime endTimeForShortEntries;

	private DateTime endTimeForShortExits;

	private int isInitialized;

	private static long lastId = -1L;

	private readonly int minutesStep = 15;

	private int numNodes = -1;

	private int r0 = -1;

	private int r1 = -1;

	private int r2 = -1;

	private int r3 = -1;

	private SessionIterator sessionIterator;

	private DateTime startTimeForLongEntries;

	private DateTime startTimeForLongExits;

	private DateTime startTimeForShortEntries;

	private DateTime startTimeForShortExits;

	private readonly double stopTargetPercentStep = 0.0025;

	private static readonly object syncRoot = new object();

	internal static readonly int NumConditions = 7;

	internal static readonly int NumLogicalOperators = Enum.GetValues(typeof(LogicalOperator)).Length;

	public List<string> ChartIndicators { get; set; }

	public bool[] EnterOnDayOfWeek { get; set; }

	internal IExpression EnterLongCondition { get; set; }

	internal IExpression EnterShortCondition { get; set; }

	internal IExpression ExitLongCondition { get; set; }

	public bool[] ExitOnDayOfWeek { get; set; }

	internal IExpression ExitShortCondition { get; set; }

	internal bool? ExitOnSessionClose { get; set; }

	public bool HasCandleStickPatternExpression
	{
		get
		{
			if (EnterLongCondition?.GetExpressions().FirstOrDefault((IExpression e) => e is CandleStickPatternExpression) == null && ExitLongCondition?.GetExpressions().FirstOrDefault((IExpression e) => e is CandleStickPatternExpression) == null && EnterShortCondition?.GetExpressions().FirstOrDefault((IExpression e) => e is CandleStickPatternExpression) == null)
			{
				return ExitShortCondition?.GetExpressions().FirstOrDefault((IExpression e) => e is CandleStickPatternExpression) != null;
			}
			return true;
		}
	}

	public long Id { get; private set; }

	public bool IsConsistent
	{
		get
		{
			if (((double.IsNaN(StopLossPercent) && double.IsNaN(TrailStopPercent)) || !double.IsNaN(ProfitTargetPercent)) && ((double.IsNaN(ParabolicStopPercent) && ExitShortCondition == null) || double.IsNaN(ProfitTargetPercent)) && ((SessionMinutesForLongEntries == -1 && SessionMinutesOffsetForLongEntries == -1) || (SessionMinutesForLongEntries >= -1 && SessionMinutesOffsetForLongEntries >= -1)) && ((SessionMinutesForLongExits == -1 && SessionMinutesOffsetForLongExits == -1) || (SessionMinutesForLongExits >= -1 && SessionMinutesOffsetForLongExits >= -1)) && ((SessionMinutesForShortEntries == -1 && SessionMinutesOffsetForShortEntries == -1) || (SessionMinutesForShortEntries >= -1 && SessionMinutesOffsetForShortEntries >= -1)) && ((SessionMinutesForShortExits == -1 && SessionMinutesOffsetForShortExits == -1) || (SessionMinutesForShortExits >= -1 && SessionMinutesOffsetForShortExits >= -1)))
			{
				return TrendStrength > 0;
			}
			return false;
		}
	}

	public bool IsLong
	{
		get
		{
			if (EnterLongCondition == null)
			{
				return SessionMinutesOffsetForLongEntries >= 0;
			}
			return true;
		}
	}

	public bool IsShort
	{
		get
		{
			if (EnterShortCondition == null)
			{
				return SessionMinutesOffsetForShortEntries >= 0;
			}
			return true;
		}
	}

	internal int NumNodes
	{
		get
		{
			if (numNodes >= 0)
			{
				return numNodes;
			}
			lock (syncRoot)
			{
				if (numNodes >= 0)
				{
					return numNodes;
				}
				numNodes = 0;
				numNodes += ((EnterLongCondition != null) ? EnterLongCondition.GetExpressions().Count : 0);
				numNodes += ((EnterShortCondition != null) ? EnterShortCondition.GetExpressions().Count : 0);
				numNodes += ((ExitLongCondition != null) ? ExitLongCondition.GetExpressions().Count : 0);
				numNodes += ((ExitShortCondition != null) ? ExitShortCondition.GetExpressions().Count : 0);
				numNodes += ((!double.IsNaN(ParabolicStopPercent)) ? 1 : 0);
				numNodes += ((!double.IsNaN(ProfitTargetPercent)) ? 1 : 0);
				numNodes += ((!double.IsNaN(StopLossPercent)) ? 1 : 0);
				numNodes += ((!double.IsNaN(TrailStopPercent)) ? 1 : 0);
				numNodes += ((SessionMinutesOffsetForLongEntries >= 0) ? 1 : 0);
				numNodes += ((SessionMinutesOffsetForLongExits >= 0) ? 1 : 0);
				numNodes += ((SessionMinutesOffsetForShortEntries >= 0) ? 1 : 0);
				numNodes += ((SessionMinutesOffsetForShortExits >= 0) ? 1 : 0);
				return numNodes;
			}
		}
	}

	public double ParabolicStopPercent { get; set; }

	public double PriorPerformance { get; set; }

	public double ProfitTargetPercent { get; set; }

	public int SessionMinutesForLongEntries { get; set; }

	public int SessionMinutesForLongExits { get; set; }

	public int SessionMinutesForShortEntries { get; set; }

	public int SessionMinutesForShortExits { get; set; }

	public int SessionMinutesOffsetForLongEntries { get; set; }

	public int SessionMinutesOffsetForLongExits { get; set; }

	public int SessionMinutesOffsetForShortEntries { get; set; }

	public int SessionMinutesOffsetForShortExits { get; set; }

	public double StopLossPercent { get; set; }

	public double TrailStopPercent { get; set; }

	public int TrendStrength { get; set; }

	/// <summary>
	/// The expression tree has some properties which could be mutated linearly like 'oldValue = 10, newValue = 10 +- 1'
	/// In case we had a 'linear' mutation which yielded better results than the prior generation, then we wanted to try 'more of the same'.
	/// This implies that prior random triggers (see .r0/1/2/3...) needed to be re-applied and not be calculated again.
	/// </summary>
	public bool TryLinearMutation { get; set; }

	public NinjaTrader.NinjaScript.Optimizers.StrategyGenerator StrategyGenerator { get; set; }

	/// <summary>
	/// Create a clone.
	/// </summary>
	/// <returns></returns>
	public override object Clone()
	{
		return new GeneratedStrategyLogic
		{
			EnterLongCondition = (IExpression)(EnterLongCondition?.Clone()),
			EnterOnDayOfWeek = EnterOnDayOfWeek,
			EnterShortCondition = (IExpression)(EnterShortCondition?.Clone()),
			ExitLongCondition = (IExpression)(ExitLongCondition?.Clone()),
			ExitShortCondition = (IExpression)(ExitShortCondition?.Clone()),
			ExitOnDayOfWeek = ExitOnDayOfWeek,
			ExitOnSessionClose = ExitOnSessionClose,
			Id = Id,
			ParabolicStopPercent = ParabolicStopPercent,
			ProfitTargetPercent = ProfitTargetPercent,
			r0 = r0,
			r1 = r1,
			r2 = r2,
			r3 = r3,
			SessionMinutesForLongEntries = SessionMinutesForLongEntries,
			SessionMinutesForLongExits = SessionMinutesForLongExits,
			SessionMinutesForShortEntries = SessionMinutesForShortEntries,
			SessionMinutesForShortExits = SessionMinutesForShortExits,
			SessionMinutesOffsetForLongEntries = SessionMinutesOffsetForLongEntries,
			SessionMinutesOffsetForLongExits = SessionMinutesOffsetForLongExits,
			SessionMinutesOffsetForShortEntries = SessionMinutesOffsetForShortEntries,
			SessionMinutesOffsetForShortExits = SessionMinutesOffsetForShortExits,
			StopLossPercent = StopLossPercent,
			TrailStopPercent = TrailStopPercent,
			TrendStrength = TrendStrength,
			TryLinearMutation = TryLinearMutation,
			StrategyGenerator = StrategyGenerator
		};
	}

	/// <summary>
	/// Populate an instance from XML
	/// </summary>
	/// <param name="element"></param>
	public override void FromXml(XElement element)
	{
		EnterLongCondition = ((element.Element("EnterLongCondition") == null) ? null : ((element.Element("EnterLongCondition").Elements().First()
			.Name == "IndicatorExpression") ? IndicatorExpression.FromXml(element.Element("EnterLongCondition").Elements().First()) : ((element.Element("EnterLongCondition").Elements().First()
			.Name == "CandleStickPatternExpression") ? CandleStickPatternExpression.FromXml(element.Element("EnterLongCondition").Elements().First()) : LogicalExpression.FromXml(element.Element("EnterLongCondition").Elements().First()))));
		EnterShortCondition = ((element.Element("EnterShortCondition") == null) ? null : ((element.Element("EnterShortCondition").Elements().First()
			.Name == "IndicatorExpression") ? IndicatorExpression.FromXml(element.Element("EnterShortCondition").Elements().First()) : ((element.Element("EnterShortCondition").Elements().First()
			.Name == "CandleStickPatternExpression") ? CandleStickPatternExpression.FromXml(element.Element("EnterShortCondition").Elements().First()) : LogicalExpression.FromXml(element.Element("EnterShortCondition").Elements().First()))));
		ExitLongCondition = ((element.Element("ExitLongCondition") == null) ? null : ((element.Element("ExitLongCondition").Elements().First()
			.Name == "IndicatorExpression") ? IndicatorExpression.FromXml(element.Element("ExitLongCondition").Elements().First()) : ((element.Element("ExitLongCondition").Elements().First()
			.Name == "CandleStickPatternExpression") ? CandleStickPatternExpression.FromXml(element.Element("ExitLongCondition").Elements().First()) : LogicalExpression.FromXml(element.Element("ExitLongCondition").Elements().First()))));
		ExitShortCondition = ((element.Element("ExitShortCondition") == null) ? null : ((element.Element("ExitShortCondition").Elements().First()
			.Name == "IndicatorExpression") ? IndicatorExpression.FromXml(element.Element("ExitShortCondition").Elements().First()) : ((element.Element("ExitShortCondition").Elements().First()
			.Name == "CandleStickPatternExpression") ? CandleStickPatternExpression.FromXml(element.Element("ExitShortCondition").Elements().First()) : LogicalExpression.FromXml(element.Element("ExitShortCondition").Elements().First()))));
		if (element.Element("EnterOnDayOfWeek") != null)
		{
			EnterOnDayOfWeek = new bool[daysOfWeekCount];
			for (int i = 0; i < element.Element("EnterOnDayOfWeek").Value.Length; i++)
			{
				EnterOnDayOfWeek[i] = element.Element("EnterOnDayOfWeek").Value[i] == '1';
			}
		}
		if (element.Element("ExitOnDayOfWeek") != null)
		{
			ExitOnDayOfWeek = new bool[daysOfWeekCount];
			for (int j = 0; j < element.Element("ExitOnDayOfWeek").Value.Length; j++)
			{
				ExitOnDayOfWeek[j] = element.Element("ExitOnDayOfWeek").Value[j] == '1';
			}
		}
		if (element.Element("ExitOnSessionClose") != null)
		{
			ExitOnSessionClose = bool.Parse(element.Element("ExitOnSessionClose").Value);
		}
		ParabolicStopPercent = double.Parse(element.Element("ParabolicStopPercent").Value, CultureInfo.InvariantCulture);
		ProfitTargetPercent = double.Parse(element.Element("ProfitTargetPercent").Value, CultureInfo.InvariantCulture);
		SessionMinutesForLongEntries = int.Parse(element.Element("SessionMinutesForLongEntries").Value, CultureInfo.InvariantCulture);
		SessionMinutesForLongExits = int.Parse(element.Element("SessionMinutesForLongExits").Value, CultureInfo.InvariantCulture);
		SessionMinutesForShortEntries = int.Parse(element.Element("SessionMinutesForShortEntries").Value, CultureInfo.InvariantCulture);
		SessionMinutesForShortExits = int.Parse(element.Element("SessionMinutesForShortExits").Value, CultureInfo.InvariantCulture);
		SessionMinutesOffsetForLongEntries = int.Parse(element.Element("SessionMinutesOffsetForLongEntries").Value, CultureInfo.InvariantCulture);
		SessionMinutesOffsetForLongExits = int.Parse(element.Element("SessionMinutesOffsetForLongExits").Value, CultureInfo.InvariantCulture);
		SessionMinutesOffsetForShortEntries = int.Parse(element.Element("SessionMinutesOffsetForShortEntries").Value, CultureInfo.InvariantCulture);
		SessionMinutesOffsetForShortExits = int.Parse(element.Element("SessionMinutesOffsetForShortExits").Value, CultureInfo.InvariantCulture);
		StopLossPercent = double.Parse(element.Element("StopLossPercent").Value, CultureInfo.InvariantCulture);
		TrailStopPercent = double.Parse(element.Element("TrailStopPercent").Value, CultureInfo.InvariantCulture);
		TrendStrength = int.Parse(element.Element("TrendStrength").Value, CultureInfo.InvariantCulture);
	}

	public CandleStickPatternLogic GetCandleStickPatternLogic(StrategyBase strategy)
	{
		if (candleStickPatternLogic != null)
		{
			return candleStickPatternLogic;
		}
		lock (syncRoot)
		{
			if (candleStickPatternLogic == null)
			{
				candleStickPatternLogic = new CandleStickPatternLogic((NinjaScriptBase)(object)strategy, TrendStrength);
			}
		}
		return candleStickPatternLogic;
	}

	internal GeneratedStrategyLogic NewCrossOver(GeneratedStrategyLogic fitter, Random random)
	{
		int num = random.Next(3);
		GeneratedStrategyLogic obj = new GeneratedStrategyLogic
		{
			EnterLongCondition = ((num == 0) ? (fitter.EnterLongCondition?.Clone() as IExpression) : (EnterLongCondition?.Clone() as IExpression)),
			EnterShortCondition = ((num == 0) ? (fitter.EnterShortCondition?.Clone() as IExpression) : (EnterShortCondition?.Clone() as IExpression)),
			EnterOnDayOfWeek = ((num == 0) ? fitter.EnterOnDayOfWeek : EnterOnDayOfWeek),
			ExitLongCondition = ((num == 1) ? (fitter.ExitLongCondition?.Clone() as IExpression) : (ExitLongCondition?.Clone() as IExpression)),
			ExitShortCondition = ((num == 1) ? (fitter.ExitShortCondition?.Clone() as IExpression) : (ExitShortCondition?.Clone() as IExpression)),
			ExitOnDayOfWeek = ((num == 1) ? fitter.ExitOnDayOfWeek : ExitOnDayOfWeek),
			ExitOnSessionClose = ((num == 1) ? fitter.ExitOnSessionClose : ExitOnSessionClose),
			ParabolicStopPercent = ((num == 1) ? fitter.ParabolicStopPercent : ParabolicStopPercent),
			ProfitTargetPercent = ((num == 1) ? fitter.ProfitTargetPercent : ProfitTargetPercent),
			SessionMinutesForLongEntries = ((num == 0) ? fitter.SessionMinutesForLongEntries : SessionMinutesForLongEntries),
			SessionMinutesForLongExits = ((num == 1) ? fitter.SessionMinutesForLongExits : SessionMinutesForLongExits),
			SessionMinutesForShortEntries = ((num == 0) ? fitter.SessionMinutesForShortEntries : SessionMinutesForShortEntries),
			SessionMinutesForShortExits = ((num == 1) ? fitter.SessionMinutesForShortExits : SessionMinutesForShortExits),
			SessionMinutesOffsetForLongEntries = ((num == 0) ? fitter.SessionMinutesOffsetForLongEntries : SessionMinutesOffsetForLongEntries),
			SessionMinutesOffsetForLongExits = ((num == 1) ? fitter.SessionMinutesOffsetForLongExits : SessionMinutesOffsetForLongExits),
			SessionMinutesOffsetForShortEntries = ((num == 0) ? fitter.SessionMinutesOffsetForShortEntries : SessionMinutesOffsetForShortEntries),
			SessionMinutesOffsetForShortExits = ((num == 1) ? fitter.SessionMinutesOffsetForShortExits : SessionMinutesOffsetForShortExits),
			StopLossPercent = ((num == 1) ? fitter.StopLossPercent : StopLossPercent),
			TrailStopPercent = ((num == 1) ? fitter.TrailStopPercent : TrailStopPercent),
			TrendStrength = ((num == 2) ? fitter.TrendStrength : TrendStrength),
			StrategyGenerator = StrategyGenerator
		};
		if (!obj.IsConsistent)
		{
			throw new InvalidOperationException("NewCrossOver");
		}
		return obj;
	}

	internal GeneratedStrategyLogic NewMutation(Random random)
	{
		if (!TryLinearMutation)
		{
			r0 = random.Next(7);
			r1 = random.Next(2);
			r2 = random.Next(2);
		}
		GeneratedStrategyLogic generatedStrategyLogic = new GeneratedStrategyLogic
		{
			PriorPerformance = PriorPerformance,
			TryLinearMutation = TryLinearMutation,
			StrategyGenerator = StrategyGenerator
		};
		if (r0 == 0 && r1 == 0 && EnterLongCondition != null)
		{
			List<IExpression> expressions = EnterLongCondition.GetExpressions();
			if (!generatedStrategyLogic.TryLinearMutation)
			{
				r3 = random.Next(expressions.Count);
			}
			generatedStrategyLogic.EnterLongCondition = EnterLongCondition.NewMutation(generatedStrategyLogic, random, expressions[r3]);
		}
		else
		{
			generatedStrategyLogic.EnterLongCondition = EnterLongCondition?.Clone() as IExpression;
		}
		if (r0 == 0 && r1 == 1 && EnterShortCondition != null)
		{
			List<IExpression> expressions = EnterShortCondition.GetExpressions();
			if (!generatedStrategyLogic.TryLinearMutation)
			{
				r3 = random.Next(expressions.Count);
			}
			generatedStrategyLogic.EnterShortCondition = EnterShortCondition.NewMutation(generatedStrategyLogic, random, expressions[r3]);
		}
		else
		{
			generatedStrategyLogic.EnterShortCondition = EnterShortCondition?.Clone() as IExpression;
		}
		if (r0 == 1 && r1 == 0 && ExitLongCondition != null)
		{
			List<IExpression> expressions = ExitLongCondition.GetExpressions();
			if (!generatedStrategyLogic.TryLinearMutation)
			{
				r3 = random.Next(expressions.Count);
			}
			generatedStrategyLogic.ExitLongCondition = ExitLongCondition.NewMutation(generatedStrategyLogic, random, expressions[r3]);
		}
		else
		{
			generatedStrategyLogic.ExitLongCondition = ExitLongCondition?.Clone() as IExpression;
		}
		if (r0 == 1 && r1 == 1 && ExitShortCondition != null)
		{
			List<IExpression> expressions = ExitShortCondition.GetExpressions();
			if (!generatedStrategyLogic.TryLinearMutation)
			{
				r3 = random.Next(expressions.Count);
			}
			generatedStrategyLogic.ExitShortCondition = ExitShortCondition.NewMutation(generatedStrategyLogic, random, expressions[r3]);
		}
		else
		{
			generatedStrategyLogic.ExitShortCondition = ExitShortCondition?.Clone() as IExpression;
		}
		if (r0 == 1 && !double.IsNaN(ParabolicStopPercent))
		{
			generatedStrategyLogic.TryLinearMutation = true;
			generatedStrategyLogic.ParabolicStopPercent = Math.Max(stopTargetPercentStep, ParabolicStopPercent + (double)((r2 == 0) ? 1 : (-1)) * stopTargetPercentStep);
		}
		else
		{
			generatedStrategyLogic.ParabolicStopPercent = ParabolicStopPercent;
		}
		if (r0 == 1 && !double.IsNaN(ProfitTargetPercent))
		{
			generatedStrategyLogic.TryLinearMutation = true;
			generatedStrategyLogic.ProfitTargetPercent = Math.Max(stopTargetPercentStep, ProfitTargetPercent + (double)((r2 == 0) ? 1 : (-1)) * stopTargetPercentStep);
		}
		else
		{
			generatedStrategyLogic.ProfitTargetPercent = ProfitTargetPercent;
		}
		if (r0 == 1 && !double.IsNaN(StopLossPercent))
		{
			generatedStrategyLogic.TryLinearMutation = true;
			generatedStrategyLogic.StopLossPercent = Math.Max(stopTargetPercentStep, StopLossPercent + (double)((r2 == 0) ? 1 : (-1)) * stopTargetPercentStep);
		}
		else
		{
			generatedStrategyLogic.StopLossPercent = StopLossPercent;
		}
		if (r0 == 1 && !double.IsNaN(TrailStopPercent))
		{
			generatedStrategyLogic.TryLinearMutation = true;
			generatedStrategyLogic.TrailStopPercent = Math.Max(stopTargetPercentStep, TrailStopPercent + (double)((r2 == 0) ? 1 : (-1)) * stopTargetPercentStep);
		}
		else
		{
			generatedStrategyLogic.TrailStopPercent = TrailStopPercent;
		}
		if (r0 == 2)
		{
			generatedStrategyLogic.TryLinearMutation = true;
			generatedStrategyLogic.TrendStrength = Math.Max(2, 2 + ((r2 == 0) ? 1 : (-1)));
		}
		else
		{
			generatedStrategyLogic.TrendStrength = TrendStrength;
		}
		if (r0 == 3 && StrategyGenerator.UseSessionCloseForExits)
		{
			generatedStrategyLogic.ExitOnSessionClose = !ExitOnSessionClose;
		}
		else
		{
			generatedStrategyLogic.ExitOnSessionClose = ExitOnSessionClose;
		}
		if (r0 == 4 && StrategyGenerator.UseDayOfWeekForEntries)
		{
			int num = random.Next(daysOfWeekCount);
			generatedStrategyLogic.EnterOnDayOfWeek = EnterOnDayOfWeek.ToArray();
			generatedStrategyLogic.EnterOnDayOfWeek[num] = !EnterOnDayOfWeek[num];
		}
		else
		{
			generatedStrategyLogic.EnterOnDayOfWeek = EnterOnDayOfWeek;
		}
		if (r0 == 4 && StrategyGenerator.UseDayOfWeekForExits)
		{
			int num2 = random.Next(daysOfWeekCount);
			generatedStrategyLogic.ExitOnDayOfWeek = ExitOnDayOfWeek.ToArray();
			generatedStrategyLogic.ExitOnDayOfWeek[num2] = !ExitOnDayOfWeek[num2];
		}
		else
		{
			generatedStrategyLogic.ExitOnDayOfWeek = ExitOnDayOfWeek;
		}
		if (r0 == 6)
		{
			if (!generatedStrategyLogic.TryLinearMutation)
			{
				r3 = random.Next(4);
			}
			generatedStrategyLogic.TryLinearMutation = true;
			switch (r3)
			{
			case 0:
				generatedStrategyLogic.SessionMinutesForLongEntries = ((SessionMinutesForLongEntries == -1) ? (-1) : Math.Max(1, Math.Min(9 * minutesStep, minutesStep + ((r2 == 0) ? 1 : (-1)) * minutesStep)));
				generatedStrategyLogic.SessionMinutesForShortEntries = ((SessionMinutesForShortEntries == -1) ? (-1) : Math.Max(1, Math.Min(9 * minutesStep, minutesStep + ((r2 == 0) ? 1 : (-1)) * minutesStep)));
				break;
			case 1:
				generatedStrategyLogic.SessionMinutesForLongExits = ((SessionMinutesForLongExits == -1) ? (-1) : Math.Max(1, Math.Min(9 * minutesStep, minutesStep + ((r2 == 0) ? 1 : (-1)) * minutesStep)));
				generatedStrategyLogic.SessionMinutesForShortExits = ((SessionMinutesForShortExits == -1) ? (-1) : Math.Max(1, Math.Min(9 * minutesStep, minutesStep + ((r2 == 0) ? 1 : (-1)) * minutesStep)));
				break;
			case 2:
				generatedStrategyLogic.SessionMinutesOffsetForLongEntries = ((SessionMinutesOffsetForLongEntries == -1) ? (-1) : Math.Max(0, Math.Min(5 * minutesStep, minutesStep + ((r2 == 0) ? 1 : (-1)) * minutesStep)));
				generatedStrategyLogic.SessionMinutesOffsetForShortEntries = ((SessionMinutesOffsetForShortEntries == -1) ? (-1) : Math.Max(0, Math.Min(5 * minutesStep, minutesStep + ((r2 == 0) ? 1 : (-1)) * minutesStep)));
				break;
			case 3:
				generatedStrategyLogic.SessionMinutesOffsetForLongExits = ((SessionMinutesOffsetForLongExits == -1) ? (-1) : Math.Max(0, Math.Min(5 * minutesStep, minutesStep + ((r2 == 0) ? 1 : (-1)) * minutesStep)));
				generatedStrategyLogic.SessionMinutesOffsetForShortExits = ((SessionMinutesOffsetForShortExits == -1) ? (-1) : Math.Max(0, Math.Min(5 * minutesStep, minutesStep + ((r2 == 0) ? 1 : (-1)) * minutesStep)));
				break;
			}
		}
		else
		{
			generatedStrategyLogic.SessionMinutesForLongEntries = SessionMinutesForLongEntries;
			generatedStrategyLogic.SessionMinutesForLongExits = SessionMinutesForLongExits;
			generatedStrategyLogic.SessionMinutesForShortEntries = SessionMinutesForShortEntries;
			generatedStrategyLogic.SessionMinutesForShortExits = SessionMinutesForShortExits;
			generatedStrategyLogic.SessionMinutesOffsetForLongEntries = SessionMinutesOffsetForLongEntries;
			generatedStrategyLogic.SessionMinutesOffsetForLongExits = SessionMinutesOffsetForLongExits;
			generatedStrategyLogic.SessionMinutesOffsetForShortEntries = SessionMinutesOffsetForShortEntries;
			generatedStrategyLogic.SessionMinutesOffsetForShortExits = SessionMinutesOffsetForShortExits;
		}
		if (!generatedStrategyLogic.IsConsistent)
		{
			throw new InvalidOperationException("NewMutation");
		}
		return generatedStrategyLogic;
	}

	internal GeneratedStrategyLogic NewRandom(Random random)
	{
		int num = (StrategyGenerator.OptimizeEntries ? random.Next(3) : (-1));
		int num2 = ((!StrategyGenerator.OptimizeExits || (!StrategyGenerator.UseCandleStickPatternForExits && !StrategyGenerator.UseIndicatorsForExits && !StrategyGenerator.UseParabolicStopForExits && !StrategyGenerator.UseStopTargetsForExits)) ? (-1) : random.Next(((StrategyGenerator.UseCandleStickPatternForExits || StrategyGenerator.UseIndicatorsForExits) ? 1 : 0) + (StrategyGenerator.UseParabolicStopForExits ? 1 : 0) + (StrategyGenerator.UseStopTargetsForExits ? 2 : 0)));
		if (num2 >= 0 && !StrategyGenerator.UseCandleStickPatternForExits && !StrategyGenerator.UseIndicatorsForExits)
		{
			num2++;
		}
		if (num2 >= 1 && !StrategyGenerator.UseParabolicStopForExits)
		{
			num2++;
		}
		int num3 = (StrategyGenerator.UseSessionTimeForEntries ? random.Next(2) : 0);
		int num4 = (StrategyGenerator.UseSessionTimeForExits ? random.Next(2) : 0);
		double num5 = stopTargetPercentStep * (double)(1 + random.Next(8));
		GeneratedStrategyLogic generatedStrategyLogic = new GeneratedStrategyLogic();
		GeneratedStrategyLogic generatedStrategyLogic2 = generatedStrategyLogic;
		bool flag = (uint)num <= 1u;
		generatedStrategyLogic2.EnterLongCondition = (flag ? RandomExpression(random, true) : null);
		GeneratedStrategyLogic generatedStrategyLogic3 = generatedStrategyLogic;
		bool flag2 = ((num == 0 || num == 2) ? true : false);
		generatedStrategyLogic3.EnterShortCondition = (flag2 ? RandomExpression(random, true) : null);
		generatedStrategyLogic.EnterOnDayOfWeek = (StrategyGenerator.UseDayOfWeekForEntries ? new bool[daysOfWeekCount] : null);
		GeneratedStrategyLogic generatedStrategyLogic4 = generatedStrategyLogic;
		bool flag3 = (uint)num <= 1u;
		generatedStrategyLogic4.ExitLongCondition = ((flag3 && num2 == 0) ? RandomExpression(random, false) : null);
		GeneratedStrategyLogic generatedStrategyLogic5 = generatedStrategyLogic;
		bool flag4 = ((num == 0 || num == 2) ? true : false);
		generatedStrategyLogic5.ExitShortCondition = ((flag4 && num2 == 0) ? RandomExpression(random, false) : null);
		generatedStrategyLogic.ExitOnDayOfWeek = (StrategyGenerator.UseDayOfWeekForExits ? new bool[daysOfWeekCount] : null);
		generatedStrategyLogic.ExitOnSessionClose = (StrategyGenerator.UseSessionCloseForExits ? new bool?(random.Next(2) == 0) : ((bool?)null));
		generatedStrategyLogic.ParabolicStopPercent = ((num2 == 1) ? num5 : double.NaN);
		generatedStrategyLogic.ProfitTargetPercent = ((num2 != 0 && num2 != 1) ? (2.0 * num5) : double.NaN);
		GeneratedStrategyLogic generatedStrategyLogic6 = generatedStrategyLogic;
		bool flag5 = (uint)num <= 1u;
		generatedStrategyLogic6.SessionMinutesForLongEntries = ((flag5 && num3 == 1) ? (minutesStep * (1 + random.Next(8))) : (-1));
		GeneratedStrategyLogic generatedStrategyLogic7 = generatedStrategyLogic;
		bool flag6 = (uint)num <= 1u;
		generatedStrategyLogic7.SessionMinutesForLongExits = ((flag6 && num4 == 1) ? (minutesStep * (1 + random.Next(8))) : (-1));
		GeneratedStrategyLogic generatedStrategyLogic8 = generatedStrategyLogic;
		bool flag7 = ((num == 0 || num == 2) ? true : false);
		generatedStrategyLogic8.SessionMinutesForShortEntries = ((flag7 && num3 == 1) ? (minutesStep * (1 + random.Next(8))) : (-1));
		GeneratedStrategyLogic generatedStrategyLogic9 = generatedStrategyLogic;
		bool flag8 = ((num == 0 || num == 2) ? true : false);
		generatedStrategyLogic9.SessionMinutesForShortExits = ((flag8 && num4 == 1) ? (minutesStep * (1 + random.Next(8))) : (-1));
		GeneratedStrategyLogic generatedStrategyLogic10 = generatedStrategyLogic;
		bool flag9 = (uint)num <= 1u;
		generatedStrategyLogic10.SessionMinutesOffsetForLongEntries = ((flag9 && num3 == 1) ? (minutesStep * random.Next(5)) : (-1));
		GeneratedStrategyLogic generatedStrategyLogic11 = generatedStrategyLogic;
		bool flag10 = (uint)num <= 1u;
		generatedStrategyLogic11.SessionMinutesOffsetForLongExits = ((flag10 && num4 == 1) ? (minutesStep * random.Next(5)) : (-1));
		GeneratedStrategyLogic generatedStrategyLogic12 = generatedStrategyLogic;
		bool flag11 = ((num == 0 || num == 2) ? true : false);
		generatedStrategyLogic12.SessionMinutesOffsetForShortEntries = ((flag11 && num3 == 1) ? (minutesStep * random.Next(5)) : (-1));
		GeneratedStrategyLogic generatedStrategyLogic13 = generatedStrategyLogic;
		bool flag12 = ((num == 0 || num == 2) ? true : false);
		generatedStrategyLogic13.SessionMinutesOffsetForShortExits = ((flag12 && num4 == 1) ? (minutesStep * random.Next(5)) : (-1));
		generatedStrategyLogic.StopLossPercent = ((num2 == 2) ? num5 : double.NaN);
		generatedStrategyLogic.TrailStopPercent = ((num2 == 3) ? num5 : double.NaN);
		generatedStrategyLogic.TrendStrength = 2 + random.Next(9);
		generatedStrategyLogic.StrategyGenerator = StrategyGenerator;
		GeneratedStrategyLogic generatedStrategyLogic14 = generatedStrategyLogic;
		if (generatedStrategyLogic14.EnterOnDayOfWeek != null)
		{
			for (int i = 1; i < generatedStrategyLogic14.EnterOnDayOfWeek.Length; i++)
			{
				generatedStrategyLogic14.EnterOnDayOfWeek[i] = random.Next(2) == 0;
			}
		}
		if (generatedStrategyLogic14.ExitOnDayOfWeek != null)
		{
			for (int j = 1; j < generatedStrategyLogic14.ExitOnDayOfWeek.Length; j++)
			{
				generatedStrategyLogic14.ExitOnDayOfWeek[j] = random.Next(2) == 0;
			}
		}
		if (!generatedStrategyLogic14.IsConsistent)
		{
			throw new InvalidOperationException("NewRandom");
		}
		return generatedStrategyLogic14;
	}

	/// <summary>
	/// Called on every OnBarUpdate. Implement your custom logic here.
	/// </summary>
	/// <param name="strategy"></param>
	public override void OnBarUpdate(StrategyBase strategy)
	{
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Expected O, but got Unknown
		if (((NinjaScriptBase)strategy).CurrentBars[0] < strategy.BarsRequiredToTrade)
		{
			return;
		}
		if (Interlocked.CompareExchange(ref isInitialized, 1, 0) == 0)
		{
			EnterLongCondition?.Initialize(strategy);
			EnterShortCondition?.Initialize(strategy);
			ExitLongCondition?.Initialize(strategy);
			ExitShortCondition?.Initialize(strategy);
			if (!double.IsNaN(ParabolicStopPercent))
			{
				strategy.SetParabolicStop((CalculationMode)1, ParabolicStopPercent);
			}
			if (!double.IsNaN(ProfitTargetPercent))
			{
				strategy.SetProfitTarget((CalculationMode)1, ProfitTargetPercent);
			}
			if (!double.IsNaN(StopLossPercent))
			{
				strategy.SetStopLoss((CalculationMode)1, StopLossPercent);
			}
			if (!double.IsNaN(TrailStopPercent))
			{
				strategy.SetTrailStop((CalculationMode)1, TrailStopPercent);
			}
		}
		if ((sessionIterator == null && (SessionMinutesOffsetForLongEntries >= 0 || SessionMinutesOffsetForShortEntries >= 0 || SessionMinutesOffsetForLongExits >= 0 || SessionMinutesOffsetForShortExits >= 0)) || (sessionIterator != null && ((NinjaScriptBase)strategy).BarsArray[0].IsFirstBarOfSession))
		{
			if (sessionIterator == null)
			{
				sessionIterator = new SessionIterator(((NinjaScriptBase)strategy).BarsArray[0]);
				sessionIterator.GetNextSession(((NinjaScriptBase)strategy).Times[0][0], true);
			}
			else if (((NinjaScriptBase)strategy).BarsArray[0].IsFirstBarOfSession)
			{
				sessionIterator.GetNextSession(((NinjaScriptBase)strategy).Times[0][0], true);
			}
			if (SessionMinutesOffsetForLongEntries >= 0)
			{
				startTimeForLongEntries = sessionIterator.ActualSessionBegin.AddMinutes(SessionMinutesOffsetForLongEntries);
				endTimeForLongEntries = startTimeForLongEntries.AddMinutes(SessionMinutesForLongEntries);
			}
			if (SessionMinutesOffsetForShortEntries >= 0)
			{
				startTimeForShortEntries = sessionIterator.ActualSessionBegin.AddMinutes(SessionMinutesOffsetForShortEntries);
				endTimeForShortEntries = startTimeForShortEntries.AddMinutes(SessionMinutesForShortEntries);
			}
			if (SessionMinutesOffsetForLongExits >= 0)
			{
				startTimeForLongExits = sessionIterator.ActualSessionEnd.AddMinutes(-(SessionMinutesOffsetForLongExits + SessionMinutesForLongExits));
				endTimeForLongExits = startTimeForLongExits.AddMinutes(SessionMinutesForLongExits);
			}
			if (SessionMinutesOffsetForShortExits >= 0)
			{
				startTimeForShortExits = sessionIterator.ActualSessionEnd.AddMinutes(-(SessionMinutesOffsetForShortExits + SessionMinutesForShortExits));
				endTimeForShortExits = startTimeForShortExits.AddMinutes(SessionMinutesForShortExits);
			}
		}
		if ((EnterLongCondition != null || SessionMinutesOffsetForLongEntries >= 0) && (EnterLongCondition == null || EnterLongCondition.Evaluate(this, strategy)) && (SessionMinutesOffsetForLongEntries == -1 || (startTimeForLongEntries < ((NinjaScriptBase)strategy).Times[0][0] && ((NinjaScriptBase)strategy).Times[0][0] <= endTimeForLongEntries)) && (EnterOnDayOfWeek == null || EnterOnDayOfWeek[(int)((NinjaScriptBase)strategy).Times[0][0].DayOfWeek]))
		{
			if (!(strategy is IGeneratedStrategy))
			{
				strategy.EnterLong();
			}
			else
			{
				(strategy as IGeneratedStrategy).OnEnterLong();
			}
		}
		if ((ExitLongCondition != null || SessionMinutesOffsetForLongExits >= 0) && (ExitLongCondition == null || ExitLongCondition.Evaluate(this, strategy)) && (SessionMinutesOffsetForLongExits == -1 || (startTimeForLongExits < ((NinjaScriptBase)strategy).Times[0][0] && ((NinjaScriptBase)strategy).Times[0][0] <= endTimeForLongExits)) && (ExitOnDayOfWeek == null || ExitOnDayOfWeek[(int)((NinjaScriptBase)strategy).Times[0][0].DayOfWeek]))
		{
			if (!(strategy is IGeneratedStrategy))
			{
				strategy.ExitLong();
			}
			else
			{
				(strategy as IGeneratedStrategy).OnExitLong();
			}
		}
		if ((EnterShortCondition != null || SessionMinutesOffsetForShortEntries >= 0) && (EnterShortCondition == null || EnterShortCondition.Evaluate(this, strategy)) && (SessionMinutesOffsetForShortEntries == -1 || (startTimeForShortEntries < ((NinjaScriptBase)strategy).Times[0][0] && ((NinjaScriptBase)strategy).Times[0][0] <= endTimeForShortEntries)) && (EnterOnDayOfWeek == null || EnterOnDayOfWeek[(int)((NinjaScriptBase)strategy).Times[0][0].DayOfWeek]))
		{
			if (!(strategy is IGeneratedStrategy))
			{
				strategy.EnterShort();
			}
			else
			{
				(strategy as IGeneratedStrategy).OnEnterShort();
			}
		}
		if ((ExitShortCondition != null || SessionMinutesOffsetForShortExits >= 0) && (ExitShortCondition == null || ExitShortCondition.Evaluate(this, strategy)) && (SessionMinutesOffsetForShortExits == -1 || (startTimeForShortExits < ((NinjaScriptBase)strategy).Times[0][0] && ((NinjaScriptBase)strategy).Times[0][0] <= endTimeForShortExits)) && (ExitOnDayOfWeek == null || ExitOnDayOfWeek[(int)((NinjaScriptBase)strategy).Times[0][0].DayOfWeek]))
		{
			if (!(strategy is IGeneratedStrategy))
			{
				strategy.ExitShort();
			}
			else
			{
				(strategy as IGeneratedStrategy).OnExitShort();
			}
		}
	}

	/// <summary>
	/// Called on every OnStateChange. Implement your custom logic here.
	/// </summary>
	/// <param name="strategy"></param>
	public override void OnStateChange(StrategyBase strategy)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)strategy).State == 2 && ExitOnSessionClose.HasValue)
		{
			strategy.IsExitOnSessionCloseStrategy = ExitOnSessionClose.Value;
		}
	}

	internal IExpression RandomExpression(Random random, bool? isEntry = null)
	{
		bool flag = StrategyGenerator.SelectedCandleStickPattern.Length != 0 && (!isEntry.HasValue || (isEntry == true && StrategyGenerator.UseCandleStickPatternForEntries) || (isEntry == false && StrategyGenerator.UseCandleStickPatternForExits));
		bool flag2 = StrategyGenerator.SelectedIndicatorTypes.Length != 0 && (!isEntry.HasValue || (isEntry == true && StrategyGenerator.UseIndicatorsForEntries) || (isEntry == false && StrategyGenerator.UseIndicatorsForExits));
		int num = random.Next(1 + (flag ? 2 : 0) + (flag2 ? 2 : 0));
		if (!flag && !flag2)
		{
			return null;
		}
		if (num == 0)
		{
			return new LogicalExpression
			{
				Left = RandomExpression(random, isEntry),
				Operator = (LogicalOperator)random.Next(NumLogicalOperators),
				Right = RandomExpression(random, isEntry)
			};
		}
		if (flag && num <= 2)
		{
			return new CandleStickPatternExpression
			{
				Pattern = RandomCandleStickPattern(random)
			};
		}
		IndicatorExpression indicatorExpression = new IndicatorExpression
		{
			CompareFactor = (double)random.Next(101) / 100.0,
			Condition = (Condition)random.Next(NumConditions),
			Left = RandomIndicator(random),
			LeftBarsAgo = 0,
			Right = RandomIndicator(random),
			RightBarsAgo = 0,
			UsePriceToCompare = (random.Next(2) == 0)
		};
		if (NinjaTrader.NinjaScript.Optimizers.StrategyGenerator.AvailableIndicators.TryGetValue(((object)indicatorExpression.Left).GetType(), out var value) && value != null)
		{
			indicatorExpression.MinCompare = value.Item1;
			indicatorExpression.MaxCompare = value.Item2;
		}
		if (((NinjaScriptBase)indicatorExpression.Left).IsOverlay)
		{
			while (!((NinjaScriptBase)indicatorExpression.Right).IsOverlay)
			{
				try
				{
					((NinjaScript)indicatorExpression.Right).SetState((State)9);
				}
				catch
				{
				}
				indicatorExpression.Right = RandomIndicator(random);
			}
		}
		else
		{
			while (((NinjaScriptBase)indicatorExpression.Right).IsOverlay)
			{
				try
				{
					((NinjaScript)indicatorExpression.Right).SetState((State)9);
				}
				catch
				{
				}
				indicatorExpression.Right = RandomIndicator(random);
			}
		}
		try
		{
			((NinjaScript)indicatorExpression.Left).SetState((State)2);
		}
		catch (Exception ex)
		{
			Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
			{
				((NinjaScriptBase)indicatorExpression.Left).Name,
				(ex.InnerException != null) ? ex.InnerException.ToString() : ex.ToString()
			}, (LogLevel)3, (LogCategories)4);
			((NinjaScript)indicatorExpression.Left).SetState((State)9);
			return null;
		}
		try
		{
			((NinjaScript)indicatorExpression.Right).SetState((State)2);
			return indicatorExpression;
		}
		catch (Exception ex2)
		{
			Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
			{
				((NinjaScriptBase)indicatorExpression.Right).Name,
				(ex2.InnerException != null) ? ex2.InnerException.ToString() : ex2.ToString()
			}, (LogLevel)3, (LogCategories)4);
			((NinjaScript)indicatorExpression.Right).SetState((State)9);
			return null;
		}
	}

	internal ChartPattern RandomCandleStickPattern(Random random)
	{
		return StrategyGenerator.SelectedCandleStickPattern[random.Next(StrategyGenerator.SelectedCandleStickPattern.Length - 1)];
	}

	internal IndicatorBase RandomIndicator(Random random)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		Type type = null;
		while (true)
		{
			try
			{
				type = StrategyGenerator.SelectedIndicatorTypes[random.Next(StrategyGenerator.SelectedIndicatorTypes.Length - 1)];
				IndicatorBase val = (IndicatorBase)type.Assembly.CreateInstance(type.FullName ?? "");
				if (val != null)
				{
					((NinjaScript)val).SetState((State)2);
				}
				if ((val != null && !((NinjaScript)val).VerifyVendorLicense()) || (val != null && ((NinjaScriptBase)val).BarsPeriods.Length > 1) || (val != null && ((NinjaScriptBase)val).Values.Length == 0))
				{
					try
					{
						((NinjaScript)val).SetState((State)9);
					}
					catch
					{
					}
					continue;
				}
				if (val != null)
				{
					((NinjaScriptBase)val).SelectedValueSeries = random.Next(((NinjaScriptBase)val).Values.Length);
				}
				return val;
			}
			catch (Exception ex)
			{
				Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
				{
					type.FullName,
					(ex.InnerException != null) ? ex.InnerException.ToString() : ex.ToString()
				}, (LogLevel)3, (LogCategories)4);
			}
		}
	}

	/// <summary>
	/// Create a hard coded version of the strategy.
	/// </summary>
	/// <param name="templateStrategy">Optional template strategy</param>
	/// <returns>The strategy code</returns>
	public override string ToString(StrategyBase templateStrategy = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		EnterLongCondition?.PrintAddChartIndicator(this, stringBuilder, 4);
		EnterShortCondition?.PrintAddChartIndicator(this, stringBuilder, 4);
		ExitLongCondition?.PrintAddChartIndicator(this, stringBuilder, 4);
		ExitShortCondition?.PrintAddChartIndicator(this, stringBuilder, 4);
		string value = stringBuilder.ToString();
		stringBuilder = new StringBuilder();
		stringBuilder.Append("//" + Environment.NewLine);
		stringBuilder.Append("// Copyright (C) 2025, NinjaTrader LLC <www.ninjatrader.com>." + Environment.NewLine);
		stringBuilder.Append("// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release." + Environment.NewLine);
		stringBuilder.Append("//" + Environment.NewLine);
		stringBuilder.Append("#region Using declarations" + Environment.NewLine);
		stringBuilder.Append("using System;" + Environment.NewLine);
		stringBuilder.Append("using System.Collections.Generic;" + Environment.NewLine);
		stringBuilder.Append("using System.ComponentModel;" + Environment.NewLine);
		stringBuilder.Append("using System.ComponentModel.DataAnnotations;" + Environment.NewLine);
		stringBuilder.Append("using System.Linq;" + Environment.NewLine);
		stringBuilder.Append("using System.Text;" + Environment.NewLine);
		stringBuilder.Append("using System.Threading.Tasks;" + Environment.NewLine);
		stringBuilder.Append("using System.Windows;" + Environment.NewLine);
		stringBuilder.Append("using System.Windows.Input;" + Environment.NewLine);
		stringBuilder.Append("using System.Windows.Media;" + Environment.NewLine);
		stringBuilder.Append("using System.Xml.Serialization;" + Environment.NewLine);
		stringBuilder.Append("using NinjaTrader.Cbi;" + Environment.NewLine);
		stringBuilder.Append("using NinjaTrader.Gui;" + Environment.NewLine);
		stringBuilder.Append("using NinjaTrader.Gui.Chart;" + Environment.NewLine);
		stringBuilder.Append("using NinjaTrader.Gui.SuperDom;" + Environment.NewLine);
		stringBuilder.Append("using NinjaTrader.Data;" + Environment.NewLine);
		stringBuilder.Append("using NinjaTrader.NinjaScript;" + Environment.NewLine);
		stringBuilder.Append("using NinjaTrader.Core.FloatingPoint;" + Environment.NewLine);
		stringBuilder.Append("using NinjaTrader.NinjaScript.Indicators;" + Environment.NewLine);
		stringBuilder.Append("using NinjaTrader.NinjaScript.DrawingTools;" + Environment.NewLine);
		stringBuilder.Append("#endregion" + Environment.NewLine + Environment.NewLine);
		stringBuilder.Append("// This namespace holds strategies in this folder and is required. Do not change it." + Environment.NewLine);
		stringBuilder.Append("namespace NinjaTrader.NinjaScript.Strategies" + Environment.NewLine);
		stringBuilder.Append("{" + Environment.NewLine);
		stringBuilder.Indent(1);
		StringBuilder stringBuilder2 = stringBuilder;
		bool flag = ((templateStrategy is IGeneratedStrategy || templateStrategy != null) ? true : false);
		stringBuilder2.Append("public class " + (flag ? ((NinjaScriptBase)templateStrategy).Name : "GeneratedStrategy") + " : " + ((templateStrategy is IGeneratedStrategy) ? ((NinjaScriptBase)templateStrategy).Name : "Strategy") + Environment.NewLine);
		stringBuilder.Indent(1);
		stringBuilder.Append("{" + Environment.NewLine);
		if (HasCandleStickPatternExpression)
		{
			stringBuilder.Indent(2);
			stringBuilder.Append("private Indicators.CandleStickPatternLogic candleStickPatternLogic;" + Environment.NewLine);
		}
		if (SessionMinutesOffsetForLongEntries >= 0)
		{
			stringBuilder.Indent(2);
			stringBuilder.Append("private DateTime\t\t\t\t\t\t\t\tendTimeForLongEntries;" + Environment.NewLine);
		}
		if (SessionMinutesOffsetForLongExits >= 0)
		{
			stringBuilder.Indent(2);
			stringBuilder.Append("private DateTime\t\t\t\t\t\t\t\tendTimeForLongExits;" + Environment.NewLine);
		}
		if (SessionMinutesOffsetForShortEntries >= 0)
		{
			stringBuilder.Indent(2);
			stringBuilder.Append("private DateTime\t\t\t\t\t\t\t\tendTimeForShortEntries;" + Environment.NewLine);
		}
		if (SessionMinutesOffsetForShortExits >= 0)
		{
			stringBuilder.Indent(2);
			stringBuilder.Append("private DateTime\t\t\t\t\t\t\t\tendTimeForShortExits;" + Environment.NewLine);
		}
		if (SessionMinutesOffsetForLongEntries >= 0 || SessionMinutesOffsetForShortEntries >= 0 || SessionMinutesOffsetForLongExits >= 0 || SessionMinutesOffsetForShortExits >= 0)
		{
			stringBuilder.Indent(2);
			stringBuilder.Append("private Data.SessionIterator\t\t\t\t\tsessionIterator;" + Environment.NewLine);
		}
		if (SessionMinutesOffsetForLongEntries >= 0)
		{
			stringBuilder.Indent(2);
			stringBuilder.Append("private DateTime\t\t\t\t\t\t\t\tstartTimeForLongEntries;" + Environment.NewLine);
		}
		if (SessionMinutesOffsetForLongExits >= 0)
		{
			stringBuilder.Indent(2);
			stringBuilder.Append("private DateTime\t\t\t\t\t\t\t\tstartTimeForLongExits;" + Environment.NewLine);
		}
		if (SessionMinutesOffsetForShortEntries >= 0)
		{
			stringBuilder.Indent(2);
			stringBuilder.Append("private DateTime\t\t\t\t\t\t\t\tstartTimeForShortEntries;" + Environment.NewLine);
		}
		if (SessionMinutesOffsetForShortExits >= 0)
		{
			stringBuilder.Indent(2);
			stringBuilder.Append("private DateTime\t\t\t\t\t\t\t\tstartTimeForShortExits;" + Environment.NewLine);
		}
		stringBuilder.Indent(2);
		stringBuilder.Append(Environment.NewLine);
		stringBuilder.Indent(2);
		stringBuilder.Append("protected override void OnStateChange()" + Environment.NewLine);
		stringBuilder.Indent(2);
		stringBuilder.Append("{" + Environment.NewLine);
		stringBuilder.Indent(3);
		stringBuilder.Append("base.OnStateChange();" + Environment.NewLine + Environment.NewLine);
		stringBuilder.Indent(3);
		stringBuilder.Append("if (State == State.SetDefaults)" + Environment.NewLine);
		stringBuilder.Indent(3);
		stringBuilder.Append("{" + Environment.NewLine);
		stringBuilder.Indent(4);
		stringBuilder.Append("IncludeTradeHistoryInBacktest             = false;" + Environment.NewLine);
		if (ExitOnSessionClose.HasValue)
		{
			stringBuilder.Indent(4);
			stringBuilder.Append("IsExitOnSessionCloseStrategy              = " + ExitOnSessionClose.Value.ToString().ToLower() + ";" + Environment.NewLine);
		}
		stringBuilder.Indent(4);
		stringBuilder.Append("IsInstantiatedOnEachOptimizationIteration = true;" + Environment.NewLine);
		stringBuilder.Indent(4);
		stringBuilder.Append("MaximumBarsLookBack                       = MaximumBarsLookBack.TwoHundredFiftySix;" + Environment.NewLine);
		if (templateStrategy != null)
		{
			stringBuilder.Indent(4);
			stringBuilder.Append("Name                                      = \"" + ((NinjaScriptBase)templateStrategy).Name + "\";" + Environment.NewLine);
		}
		stringBuilder.Indent(4);
		stringBuilder.Append("SupportsOptimizationGraph                 = false;" + Environment.NewLine);
		stringBuilder.Indent(3);
		stringBuilder.Append("}" + Environment.NewLine);
		stringBuilder.Indent(3);
		stringBuilder.Append("else if (State == State.Configure)" + Environment.NewLine);
		stringBuilder.Indent(3);
		stringBuilder.Append("{" + Environment.NewLine);
		if (HasCandleStickPatternExpression)
		{
			stringBuilder.Indent(4);
			stringBuilder.Append("candleStickPatternLogic = new CandleStickPatternLogic(this, " + TrendStrength + ");" + Environment.NewLine);
		}
		if (!double.IsNaN(ParabolicStopPercent))
		{
			stringBuilder.Indent(4);
			stringBuilder.Append("SetParabolicStop(CalculationMode.Percent, " + ParabolicStopPercent.ToString(CultureInfo.InvariantCulture) + ");" + Environment.NewLine);
		}
		if (!double.IsNaN(ProfitTargetPercent))
		{
			stringBuilder.Indent(4);
			stringBuilder.Append("SetProfitTarget(CalculationMode.Percent, " + ProfitTargetPercent.ToString(CultureInfo.InvariantCulture) + ");" + Environment.NewLine);
		}
		if (!double.IsNaN(StopLossPercent))
		{
			stringBuilder.Indent(4);
			stringBuilder.Append("SetStopLoss(CalculationMode.Percent, " + StopLossPercent.ToString(CultureInfo.InvariantCulture) + ");" + Environment.NewLine);
		}
		if (!double.IsNaN(TrailStopPercent))
		{
			stringBuilder.Indent(4);
			stringBuilder.Append("SetTrailStop(CalculationMode.Percent, " + TrailStopPercent.ToString(CultureInfo.InvariantCulture) + ");" + Environment.NewLine);
		}
		stringBuilder.Indent(3);
		stringBuilder.Append("}" + Environment.NewLine);
		if (!string.IsNullOrEmpty(value))
		{
			stringBuilder.Indent(3);
			stringBuilder.Append("else if (State == State.DataLoaded)" + Environment.NewLine);
			stringBuilder.Indent(3);
			stringBuilder.Append("{" + Environment.NewLine);
			stringBuilder.Append(value);
			stringBuilder.Indent(3);
			stringBuilder.Append("}" + Environment.NewLine);
		}
		stringBuilder.Indent(2);
		stringBuilder.Append("}" + Environment.NewLine + Environment.NewLine);
		stringBuilder.Indent(2);
		stringBuilder.Append("protected override void OnBarUpdate()" + Environment.NewLine);
		stringBuilder.Indent(2);
		stringBuilder.Append("{" + Environment.NewLine);
		stringBuilder.Indent(3);
		stringBuilder.Append("base.OnBarUpdate();" + Environment.NewLine + Environment.NewLine);
		stringBuilder.Indent(3);
		stringBuilder.Append("if (CurrentBars[0] < BarsRequiredToTrade)" + Environment.NewLine);
		stringBuilder.Indent(4);
		stringBuilder.Append("return;" + Environment.NewLine + Environment.NewLine);
		if (SessionMinutesOffsetForLongEntries >= 0 || SessionMinutesOffsetForShortEntries >= 0 || SessionMinutesOffsetForLongExits >= 0 || SessionMinutesOffsetForShortExits >= 0)
		{
			stringBuilder.Indent(3);
			stringBuilder.Append("if (sessionIterator == null || BarsArray[0].IsFirstBarOfSession)" + Environment.NewLine);
			stringBuilder.Indent(3);
			stringBuilder.Append("{" + Environment.NewLine);
			stringBuilder.Indent(4);
			stringBuilder.Append("if (sessionIterator == null)" + Environment.NewLine);
			stringBuilder.Indent(4);
			stringBuilder.Append("{" + Environment.NewLine);
			stringBuilder.Indent(5);
			stringBuilder.Append("sessionIterator = new Data.SessionIterator(BarsArray[0]);" + Environment.NewLine);
			stringBuilder.Indent(5);
			stringBuilder.Append("sessionIterator.GetNextSession(Times[0][0], true);" + Environment.NewLine);
			stringBuilder.Indent(4);
			stringBuilder.Append("}" + Environment.NewLine);
			stringBuilder.Indent(4);
			stringBuilder.Append("else if (BarsArray[0].IsFirstBarOfSession)" + Environment.NewLine);
			stringBuilder.Indent(5);
			stringBuilder.Append("sessionIterator.GetNextSession(Times[0][0], true);" + Environment.NewLine + Environment.NewLine);
			if (SessionMinutesOffsetForLongEntries >= 0)
			{
				stringBuilder.Indent(4);
				stringBuilder.Append("startTimeForLongEntries   = sessionIterator.ActualSessionBegin.AddMinutes(" + SessionMinutesOffsetForLongEntries + ");" + Environment.NewLine);
				stringBuilder.Indent(4);
				stringBuilder.Append("endTimeForLongEntries     = startTimeForLongEntries.AddMinutes(" + SessionMinutesForLongEntries + ");" + Environment.NewLine);
			}
			if (SessionMinutesOffsetForShortEntries >= 0)
			{
				stringBuilder.Indent(4);
				stringBuilder.Append("startTimeForShortEntries  = sessionIterator.ActualSessionBegin.AddMinutes(" + SessionMinutesOffsetForShortEntries + ");" + Environment.NewLine);
				stringBuilder.Indent(4);
				stringBuilder.Append("endTimeForShortEntries    = startTimeForShortEntries.AddMinutes(" + SessionMinutesForShortEntries + ");" + Environment.NewLine);
			}
			if (SessionMinutesOffsetForLongExits >= 0)
			{
				stringBuilder.Indent(4);
				stringBuilder.Append("startTimeForLongExits     = sessionIterator.ActualSessionEnd.AddMinutes(-" + (SessionMinutesOffsetForLongExits + SessionMinutesForLongExits) + ");" + Environment.NewLine);
				stringBuilder.Indent(4);
				stringBuilder.Append("endTimeForLongExits       = startTimeForLongExits.AddMinutes(" + SessionMinutesForLongExits + ");" + Environment.NewLine);
			}
			if (SessionMinutesOffsetForShortExits >= 0)
			{
				stringBuilder.Indent(4);
				stringBuilder.Append("startTimeForShortExits    = sessionIterator.ActualSessionEnd.AddMinutes(-" + (SessionMinutesOffsetForShortExits + SessionMinutesForShortExits) + ");" + Environment.NewLine);
				stringBuilder.Indent(4);
				stringBuilder.Append("endTimeForShortExits      = startTimeForShortExits.AddMinutes(" + SessionMinutesForShortExits + ");" + Environment.NewLine);
			}
			stringBuilder.Indent(3);
			stringBuilder.Append("}" + Environment.NewLine + Environment.NewLine);
		}
		bool flag2 = false;
		if (EnterLongCondition != null || SessionMinutesOffsetForLongEntries >= 0)
		{
			stringBuilder.Indent(3);
			stringBuilder.Append("if (");
			EnterLongCondition?.Print(stringBuilder, 4);
			if (SessionMinutesOffsetForLongEntries >= 0)
			{
				if (EnterLongCondition != null)
				{
					stringBuilder.Append(Environment.NewLine);
					stringBuilder.Indent(4);
					stringBuilder.Append("&& ");
				}
				stringBuilder.Append("startTimeForLongEntries < Times[0][0] && Times[0][0] <= endTimeForLongEntries");
			}
			if (EnterOnDayOfWeek != null)
			{
				bool flag3 = true;
				for (int i = 0; i < EnterOnDayOfWeek.Length; i++)
				{
					if (EnterOnDayOfWeek[i])
					{
						if (flag3)
						{
							stringBuilder.Append(Environment.NewLine);
							stringBuilder.Indent(4);
							stringBuilder.Append("&& (");
							flag3 = false;
						}
						else
						{
							stringBuilder.Append(" || ");
						}
						stringBuilder.Append("Times[0][0].DayOfWeek == DayOfWeek." + Enum.GetValues(typeof(DayOfWeek)).GetValue(i));
					}
				}
				if (!flag3)
				{
					stringBuilder.Append(")");
				}
			}
			stringBuilder.Append(")" + Environment.NewLine);
			stringBuilder.Indent(4);
			stringBuilder.Append(((templateStrategy is IGeneratedStrategy) ? "OnEnterLong();" : "EnterLong();") + Environment.NewLine);
			flag2 = true;
		}
		if (ExitLongCondition != null || SessionMinutesOffsetForLongExits >= 0)
		{
			if (flag2)
			{
				stringBuilder.Append(Environment.NewLine);
			}
			stringBuilder.Indent(3);
			stringBuilder.Append("if (");
			ExitLongCondition?.Print(stringBuilder, 4);
			if (SessionMinutesOffsetForLongExits >= 0)
			{
				if (ExitLongCondition != null)
				{
					stringBuilder.Append(Environment.NewLine);
					stringBuilder.Indent(4);
					stringBuilder.Append("&& ");
				}
				stringBuilder.Append("startTimeForLongExits < Times[0][0] && Times[0][0] <= endTimeForLongExits");
			}
			if (ExitOnDayOfWeek != null)
			{
				bool flag4 = true;
				for (int j = 0; j < ExitOnDayOfWeek.Length; j++)
				{
					if (ExitOnDayOfWeek[j])
					{
						if (flag4)
						{
							stringBuilder.Append(Environment.NewLine);
							stringBuilder.Indent(4);
							stringBuilder.Append("&& (");
							flag4 = false;
						}
						else
						{
							stringBuilder.Append(" || ");
						}
						stringBuilder.Append("Times[0][0].DayOfWeek == DayOfWeek." + Enum.GetValues(typeof(DayOfWeek)).GetValue(j));
					}
				}
				if (!flag4)
				{
					stringBuilder.Append(")");
				}
			}
			stringBuilder.Append(")" + Environment.NewLine);
			stringBuilder.Indent(4);
			stringBuilder.Append(((templateStrategy is IGeneratedStrategy) ? "OnExitLong();" : "ExitLong();") + Environment.NewLine);
			flag2 = true;
		}
		if (EnterShortCondition != null || SessionMinutesOffsetForShortEntries >= 0)
		{
			if (flag2)
			{
				stringBuilder.Append(Environment.NewLine);
			}
			stringBuilder.Indent(3);
			stringBuilder.Append("if (");
			EnterShortCondition?.Print(stringBuilder, 4);
			if (SessionMinutesOffsetForShortEntries >= 0)
			{
				if (EnterShortCondition != null)
				{
					stringBuilder.Append(Environment.NewLine);
					stringBuilder.Indent(4);
					stringBuilder.Append("&& ");
				}
				stringBuilder.Append("startTimeForShortEntries < Times[0][0] && Times[0][0] <= endTimeForShortEntries");
			}
			if (EnterOnDayOfWeek != null)
			{
				bool flag5 = true;
				for (int k = 0; k < EnterOnDayOfWeek.Length; k++)
				{
					if (EnterOnDayOfWeek[k])
					{
						if (flag5)
						{
							stringBuilder.Append(Environment.NewLine);
							stringBuilder.Indent(4);
							stringBuilder.Append("&& (");
							flag5 = false;
						}
						else
						{
							stringBuilder.Append(" || ");
						}
						stringBuilder.Append("Times[0][0].DayOfWeek == DayOfWeek." + Enum.GetValues(typeof(DayOfWeek)).GetValue(k));
					}
				}
				if (!flag5)
				{
					stringBuilder.Append(")");
				}
			}
			stringBuilder.Append(")" + Environment.NewLine);
			stringBuilder.Indent(4);
			stringBuilder.Append(((templateStrategy is IGeneratedStrategy) ? "OnEnterShort();" : "EnterShort();") + Environment.NewLine);
			flag2 = true;
		}
		if (ExitShortCondition != null || SessionMinutesOffsetForShortExits >= 0)
		{
			if (flag2)
			{
				stringBuilder.Append(Environment.NewLine);
			}
			stringBuilder.Indent(3);
			stringBuilder.Append("if (");
			ExitShortCondition?.Print(stringBuilder, 4);
			if (SessionMinutesOffsetForShortExits >= 0)
			{
				if (ExitShortCondition != null)
				{
					stringBuilder.Append(Environment.NewLine);
					stringBuilder.Indent(4);
					stringBuilder.Append("&& ");
				}
				stringBuilder.Append("startTimeForShortExits < Times[0][0] && Times[0][0] <= endTimeForShortExits");
			}
			if (ExitOnDayOfWeek != null)
			{
				bool flag6 = true;
				for (int l = 0; l < ExitOnDayOfWeek.Length; l++)
				{
					if (ExitOnDayOfWeek[l])
					{
						if (flag6)
						{
							stringBuilder.Append(Environment.NewLine);
							stringBuilder.Indent(4);
							stringBuilder.Append("&& (");
							flag6 = false;
						}
						else
						{
							stringBuilder.Append(" || ");
						}
						stringBuilder.Append("Times[0][0].DayOfWeek == DayOfWeek." + Enum.GetValues(typeof(DayOfWeek)).GetValue(l));
					}
				}
				if (!flag6)
				{
					stringBuilder.Append(")");
				}
			}
			stringBuilder.Append(")" + Environment.NewLine);
			stringBuilder.Indent(4);
			stringBuilder.Append(((templateStrategy is IGeneratedStrategy) ? "OnExitShort();" : "ExitShort();") + Environment.NewLine);
		}
		stringBuilder.Indent(2);
		stringBuilder.Append("}" + Environment.NewLine);
		stringBuilder.Indent(1);
		stringBuilder.Append("}" + Environment.NewLine);
		stringBuilder.Append("}" + Environment.NewLine);
		return stringBuilder.ToString();
	}

	/// <summary>
	/// Serialize to XML
	/// </summary>
	/// <returns></returns>
	public override XElement ToXml()
	{
		XElement xElement = new XElement(((object)this).GetType().Name);
		xElement.Add(new XElement("ClassName", ((object)this).GetType().FullName));
		if (EnterLongCondition != null)
		{
			xElement.Add(new XElement("EnterLongCondition", EnterLongCondition.ToXml()));
		}
		if (EnterShortCondition != null)
		{
			xElement.Add(new XElement("EnterShortCondition", EnterShortCondition.ToXml()));
		}
		if (ExitLongCondition != null)
		{
			xElement.Add(new XElement("ExitLongCondition", ExitLongCondition.ToXml()));
		}
		if (ExitShortCondition != null)
		{
			xElement.Add(new XElement("ExitShortCondition", ExitShortCondition.ToXml()));
		}
		if (EnterOnDayOfWeek != null)
		{
			xElement.Add(new XElement("EnterOnDayOfWeek", EnterOnDayOfWeek.Select((bool e) => (!e) ? "0" : "1")));
		}
		if (ExitOnDayOfWeek != null)
		{
			xElement.Add(new XElement("ExitOnDayOfWeek", ExitOnDayOfWeek.Select((bool e) => (!e) ? "0" : "1")));
		}
		if (ExitOnSessionClose.HasValue)
		{
			xElement.Add(new XElement("ExitOnSessionClose", ExitOnSessionClose.Value.ToString(CultureInfo.InvariantCulture)));
		}
		xElement.Add(new XElement("IsLong", IsLong.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("IsShort", IsShort.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("ParabolicStopPercent", ParabolicStopPercent.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("ProfitTargetPercent", ProfitTargetPercent.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("SessionMinutesForLongEntries", SessionMinutesForLongEntries.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("SessionMinutesForLongExits", SessionMinutesForLongExits.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("SessionMinutesForShortEntries", SessionMinutesForShortEntries.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("SessionMinutesForShortExits", SessionMinutesForShortExits.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("SessionMinutesOffsetForLongEntries", SessionMinutesOffsetForLongEntries.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("SessionMinutesOffsetForLongExits", SessionMinutesOffsetForLongExits.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("SessionMinutesOffsetForShortEntries", SessionMinutesOffsetForShortEntries.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("SessionMinutesOffsetForShortExits", SessionMinutesOffsetForShortExits.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("StopLossPercent", StopLossPercent.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("TrailStopPercent", TrailStopPercent.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("TrendStrength", TrendStrength.ToString(CultureInfo.InvariantCulture)));
		return xElement;
	}

	/// <summary>
	/// Constructor with no parameters is mandatory for any subclass of .GeneratedStrategyLogicBase
	/// </summary>
	public GeneratedStrategyLogic()
	{
		ChartIndicators = new List<string>();
		EnterOnDayOfWeek = null;
		ExitOnDayOfWeek = null;
		ExitOnSessionClose = false;
		Id = Interlocked.Increment(ref lastId);
		ParabolicStopPercent = double.NaN;
		PriorPerformance = double.MinValue;
		ProfitTargetPercent = double.NaN;
		SessionMinutesForLongEntries = -1;
		SessionMinutesForLongExits = -1;
		SessionMinutesForShortEntries = -1;
		SessionMinutesForShortExits = -1;
		SessionMinutesOffsetForLongEntries = -1;
		SessionMinutesOffsetForLongExits = -1;
		SessionMinutesOffsetForShortEntries = -1;
		SessionMinutesOffsetForShortExits = -1;
		StopLossPercent = double.NaN;
		TrailStopPercent = double.NaN;
		TrendStrength = 4;
	}
}

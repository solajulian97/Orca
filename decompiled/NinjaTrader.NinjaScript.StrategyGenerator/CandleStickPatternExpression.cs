using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace NinjaTrader.NinjaScript.StrategyGenerator;

internal class CandleStickPatternExpression : IExpression, ICloneable
{
	public ChartPattern Pattern { get; set; } = ChartPattern.MorningStar;

	public object Clone()
	{
		return new CandleStickPatternExpression
		{
			Pattern = Pattern
		};
	}

	public bool Evaluate(GeneratedStrategyLogic u, StrategyBase s)
	{
		return u.GetCandleStickPatternLogic(s).Evaluate(Pattern);
	}

	public static IExpression FromXml(XElement element)
	{
		return new CandleStickPatternExpression
		{
			Pattern = (ChartPattern)Enum.Parse(typeof(ChartPattern), element.Element("Pattern").Value)
		};
	}

	public List<IExpression> GetExpressions()
	{
		return new List<IExpression>(new IExpression[1] { this });
	}

	public void Initialize(StrategyBase strategy)
	{
	}

	public IExpression NewMutation(GeneratedStrategyLogic logic, Random random, IExpression toMutate)
	{
		return new CandleStickPatternExpression
		{
			Pattern = logic.RandomCandleStickPattern(random)
		};
	}

	public void Print(StringBuilder s, int indentationLevel)
	{
		s.Append($"candleStickPatternLogic.Evaluate(ChartPattern.{Pattern})");
	}

	public void PrintAddChartIndicator(GeneratedStrategyLogic logic, StringBuilder s, int indentationLevel)
	{
		string text = $"AddChartIndicator(CandlestickPattern(ChartPattern.{Pattern}, {logic.TrendStrength}));{Environment.NewLine}";
		if (!logic.ChartIndicators.Contains(text))
		{
			logic.ChartIndicators.Add(text);
			s.Indent(indentationLevel);
			s.Append(text);
		}
	}

	public XElement ToXml()
	{
		XElement xElement = new XElement(GetType().Name);
		xElement.Add(new XElement("Pattern", Pattern.ToString()));
		return xElement;
	}
}

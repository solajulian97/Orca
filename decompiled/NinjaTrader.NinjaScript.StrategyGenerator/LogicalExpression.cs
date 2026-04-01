using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NinjaTrader.NinjaScript.StrategyGenerator;

internal class LogicalExpression : IExpression, ICloneable
{
	private int r0 = -1;

	private int r1 = -1;

	public IExpression Left { get; set; }

	public LogicalOperator Operator { get; set; }

	public IExpression Right { get; set; }

	public object Clone()
	{
		return new LogicalExpression
		{
			Left = (IExpression)Left.Clone(),
			Operator = Operator,
			Right = (IExpression)Right.Clone()
		};
	}

	public bool Evaluate(GeneratedStrategyLogic logic, StrategyBase strategy)
	{
		return Operator switch
		{
			LogicalOperator.And => Left.Evaluate(logic, strategy) && Right.Evaluate(logic, strategy), 
			LogicalOperator.Not => !Left.Evaluate(logic, strategy), 
			LogicalOperator.Or => Left.Evaluate(logic, strategy) || Right.Evaluate(logic, strategy), 
			_ => false, 
		};
	}

	public static IExpression FromXml(XElement element)
	{
		return new LogicalExpression
		{
			Left = ((element.Element("Left").Elements().First()
				.Name == "IndicatorExpression") ? IndicatorExpression.FromXml(element.Element("Left").Elements().First()) : ((element.Element("Left").Elements().First()
				.Name == "CandleStickPatternExpression") ? CandleStickPatternExpression.FromXml(element.Element("Left").Elements().First()) : FromXml(element.Element("Left").Elements().First()))),
			Operator = (LogicalOperator)Enum.Parse(typeof(LogicalOperator), element.Element("Operator").Value),
			Right = ((element.Element("Right").Elements().First()
				.Name == "IndicatorExpression") ? IndicatorExpression.FromXml(element.Element("Right").Elements().First()) : ((element.Element("Right").Elements().First()
				.Name == "CandleStickPatternExpression") ? CandleStickPatternExpression.FromXml(element.Element("Right").Elements().First()) : FromXml(element.Element("Right").Elements().First())))
		};
	}

	public List<IExpression> GetExpressions()
	{
		List<IExpression> list = new List<IExpression>();
		list.Add(this);
		list.AddRange(Left.GetExpressions());
		list.AddRange(Right.GetExpressions());
		return list;
	}

	public void Initialize(StrategyBase strategy)
	{
		Left.Initialize(strategy);
		Right.Initialize(strategy);
	}

	public IExpression NewMutation(GeneratedStrategyLogic logic, Random random, IExpression toMutate)
	{
		if (!logic.TryLinearMutation)
		{
			r0 = random.Next(10);
			r1 = random.Next(GeneratedStrategyLogic.NumLogicalOperators);
		}
		LogicalExpression obj = new LogicalExpression
		{
			Operator = ((toMutate == this && r0 < 6) ? ((LogicalOperator)r1) : Operator)
		};
		IExpression left;
		if (toMutate == this)
		{
			int num = r0;
			if (num >= 6 && num < 8)
			{
				left = logic.RandomExpression(random);
				goto IL_0081;
			}
		}
		left = Left.NewMutation(logic, random, toMutate);
		goto IL_0081;
		IL_0081:
		obj.Left = left;
		obj.Right = ((toMutate == this && r0 >= 8) ? logic.RandomExpression(random) : Right.NewMutation(logic, random, toMutate));
		return obj;
	}

	public void Print(StringBuilder s, int indentationLevel)
	{
		switch (Operator)
		{
		case LogicalOperator.And:
			s.Append('(');
			Left.Print(s, indentationLevel + 1);
			s.Append(Environment.NewLine);
			s.Indent(indentationLevel);
			s.Append("&& ");
			Right.Print(s, indentationLevel + 1);
			s.Append(')');
			break;
		case LogicalOperator.Not:
			s.Append("!(");
			Left.Print(s, indentationLevel + 1);
			s.Append(')');
			break;
		case LogicalOperator.Or:
			s.Append('(');
			Left.Print(s, indentationLevel + 1);
			s.Append(Environment.NewLine);
			s.Indent(indentationLevel);
			s.Append("|| ");
			Right.Print(s, indentationLevel + 1);
			s.Append(')');
			break;
		}
	}

	public void PrintAddChartIndicator(GeneratedStrategyLogic logic, StringBuilder stringBuilder, int indentationLevel)
	{
		Left.PrintAddChartIndicator(logic, stringBuilder, indentationLevel);
		Right.PrintAddChartIndicator(logic, stringBuilder, indentationLevel);
	}

	public XElement ToXml()
	{
		XElement xElement = new XElement(GetType().Name);
		xElement.Add(new XElement("Left", Left.ToXml()));
		xElement.Add(new XElement("Operator", Operator.ToString()));
		xElement.Add(new XElement("Right", Right.ToXml()));
		return xElement;
	}
}

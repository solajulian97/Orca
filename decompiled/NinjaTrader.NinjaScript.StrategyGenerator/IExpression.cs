using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace NinjaTrader.NinjaScript.StrategyGenerator;

internal interface IExpression : ICloneable
{
	bool Evaluate(GeneratedStrategyLogic logic, StrategyBase strategy);

	List<IExpression> GetExpressions();

	void Initialize(StrategyBase strategy);

	IExpression NewMutation(GeneratedStrategyLogic logic, Random random, IExpression toMutate);

	void Print(StringBuilder stringBuilder, int indentationLevel);

	void PrintAddChartIndicator(GeneratedStrategyLogic logic, StringBuilder stringBuilder, int indentationLevel);

	XElement ToXml();
}

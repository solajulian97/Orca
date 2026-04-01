using System.Text;

namespace NinjaTrader.NinjaScript.StrategyGenerator;

internal static class Extensions
{
	internal static void Indent(this StringBuilder stringBuilder, int tabLevels)
	{
		for (int i = 0; i < tabLevels; i++)
		{
			stringBuilder.Append('\t');
		}
	}
}

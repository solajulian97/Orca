using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Optimizers;

public class DefaultOptimizer : Optimizer
{
	private int[] enumIndexes;

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptOptimizerDefault;
			((Optimizer)this).SupportsMultiObjectiveOptimization = true;
		}
		else if ((int)((NinjaScript)this).State == 2 && ((Optimizer)this).Strategies.Count > 0)
		{
			enumIndexes = new int[((Optimizer)this).Strategies[0].OptimizationParameters.Count];
			((Optimizer)this).NumberOfIterations = Optimizer.GetParametersCombinationsCount(((Optimizer)this).Strategies[0]);
		}
	}

	protected override void OnOptimize()
	{
		Iterate(0);
	}

	/// <summary>
	/// This methods iterates the parameters recursively. The actual back test is performed as the last parameter is iterated.
	/// </summary>
	/// <param name="index"></param>
	private void Iterate(int index)
	{
		if (((Optimizer)this).Strategies[0].OptimizationParameters.Count == 0)
		{
			return;
		}
		Parameter val = ((Optimizer)this).Strategies[0].OptimizationParameters[index];
		int num = 0;
		while (true)
		{
			if (((Optimizer)this).IsAborted)
			{
				return;
			}
			if (val.ParameterType == typeof(int))
			{
				if ((double)(int)val.Min + (double)num * val.Increment > (double)(int)val.Max + val.Increment / 1000000.0)
				{
					return;
				}
				val.Value = (double)(int)val.Min + (double)num * val.Increment;
			}
			else if (val.ParameterType == typeof(double))
			{
				if ((double)val.Min + (double)num * val.Increment > (double)val.Max + val.Increment / 1000000.0)
				{
					return;
				}
				val.Value = (double)val.Min + (double)num * val.Increment;
			}
			else if (val.ParameterType == typeof(bool))
			{
				switch (num)
				{
				case 0:
					val.Value = val.Min;
					break;
				case 1:
					if ((bool)val.Min != (bool)val.Max)
					{
						val.Value = !(bool)val.Value;
						break;
					}
					return;
				default:
					return;
				}
			}
			else if (val.ParameterType.IsEnum)
			{
				if (enumIndexes[index] >= val.EnumValues.Length)
				{
					break;
				}
				val.Value = val.EnumValues[enumIndexes[index]++];
			}
			if (index == ((Optimizer)this).Strategies[0].OptimizationParameters.Count - 1)
			{
				((Optimizer)this).RunIteration();
			}
			else
			{
				Iterate(index + 1);
			}
			num++;
		}
		enumIndexes[index] = 0;
	}
}

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Optimizers;

public class GeneticOptimizer : Optimizer
{
	public class Generation
	{
		public double AveragePerformance { get; set; }

		public int ChildrenCount { get; set; }

		public int GenerationNumber { get; set; }

		public bool IsStable { get; set; }

		public bool IsReset { get; set; }

		public double MaxPerformance { get; set; }

		public double MinPerformance { get; set; }

		public double MutantCount { get; set; }

		public double PercentImprovement { get; set; }

		public int ParentCount { get; set; }

		public int PopulationCount { get; set; }

		public int RandomCount { get; set; }

		public double StabilityScore { get; set; }

		public double TotalPerformance { get; set; }

		public void AnalyzeInput(Individual[] individuals)
		{
			for (int i = 0; i < individuals.GetUpperBound(0) + 1; i++)
			{
				switch (individuals[i].Type)
				{
				case Individual.IndividualType.Child:
					ChildrenCount++;
					break;
				case Individual.IndividualType.Mutant:
					ChildrenCount++;
					MutantCount++;
					break;
				case Individual.IndividualType.Parent:
					ParentCount++;
					break;
				case Individual.IndividualType.Random:
					RandomCount++;
					break;
				case Individual.IndividualType.Unknown:
					continue;
				}
				PopulationCount++;
			}
		}

		public Generation(int generationNum)
		{
			GenerationNumber = generationNum;
		}

		public override string ToString()
		{
			return $"Generation# = {GenerationNumber} Average performance = {AveragePerformance}, %Improvement = {PercentImprovement}, Stability={StabilityScore}, Perf={TotalPerformance}";
		}
	}

	public class Individual : ICloneable
	{
		public enum IndividualType
		{
			Child,
			Mutant,
			Parent,
			Random,
			Unknown
		}

		public IndividualType Type { get; set; }

		public Parameter[] Parameters { get; set; }

		public double PerformanceValue { get; set; }

		public double Weight { get; set; }

		public string IndividualName
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < Parameters.GetUpperBound(0) + 1; i++)
				{
					if (i == 0)
					{
						stringBuilder.Append(Parameters[i].Value);
					}
					else
					{
						stringBuilder.AppendFormat("|{0}", Parameters[i].Value);
					}
				}
				return stringBuilder.ToString();
			}
		}

		public object Clone()
		{
			return MemberwiseClone();
		}

		public Individual(int parameters)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			Parameters = (Parameter[])(object)new Parameter[parameters];
			for (int i = 0; i < parameters; i++)
			{
				Parameters[i] = new Parameter();
			}
			PerformanceValue = 0.0;
			Weight = 0.0;
			Type = IndividualType.Unknown;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < Parameters.Length; i++)
			{
				stringBuilder.Append((i == 0) ? "[" : ((i < Parameters.Length) ? ";" : string.Empty));
				stringBuilder.Append(Parameters[i]);
				if (i == Parameters.Length - 1)
				{
					stringBuilder.Append("]");
				}
			}
			return $"{IndividualName} Type={Type} Weight={Weight} Parameters={stringBuilder} PerformanceValue={PerformanceValue}";
		}
	}

	private double averagePerformance;

	private Individual[] bestResultsGeneration;

	private int crossoverIndividuals;

	private double crossoverRate;

	private Individual[] currentGeneration;

	private double maxGenPerformance;

	private double minGenPerformance;

	private double mutationRate;

	private double mutationStrength;

	private Random random;

	private int resetIndividuals;

	private double resetSize;

	private int stabilityIndividuals;

	private double stabilityScore;

	private double stabilitySize;

	private double totalPerformance;

	private int trueBestResults;

	private int trueGenerations;

	private int trueGenerationSize;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptGeneticOptimizerConvergenceThreshold")]
	[Range(0, int.MaxValue)]
	public int ConvergenceThreshold { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptGeneticOptimizerCrossoverRatePercent")]
	[Range(0, 100)]
	public double CrossoverRatePecent
	{
		get
		{
			return crossoverRate * 100.0;
		}
		set
		{
			crossoverRate = Math.Max(0.0, Math.Min(1.0, value / 100.0));
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptGeneticOptimizerGenerations")]
	[Range(1, int.MaxValue)]
	public int Generations { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptGeneticOptimizerGenerationSize")]
	[Range(1, int.MaxValue)]
	public int GenerationSize { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptGeneticOptimizerMinimumPerformance")]
	[Range(0, int.MaxValue)]
	public double MinimumPerformance { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptGeneticOptimizerMutationRatePercent")]
	[Range(0, 100)]
	public double MutationRatePercent
	{
		get
		{
			return mutationRate * 100.0;
		}
		set
		{
			mutationRate = Math.Max(0.0, Math.Min(1.0, value / 100.0));
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptGeneticOptimizerMutationStrengthPercent")]
	[Range(0, 100)]
	public double MutationStrengthPercent
	{
		get
		{
			return mutationStrength * 100.0;
		}
		set
		{
			mutationStrength = Math.Max(0.0, Math.Min(1.0, value / 100.0));
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptGeneticOptimizerResetSizePercent")]
	[Range(0, 100)]
	public double ResetSizePercent
	{
		get
		{
			return resetSize * 100.0;
		}
		set
		{
			resetSize = Math.Max(0.0, Math.Min(1.0, value / 100.0));
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptGeneticOptimizerStabilitySizePercent")]
	[Range(0, 100)]
	public double StabilitySizePercent
	{
		get
		{
			return stabilitySize * 100.0;
		}
		set
		{
			stabilitySize = Math.Max(0.0, Math.Min(1.0, value / 100.0));
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			ConvergenceThreshold = 20;
			CrossoverRatePecent = 80.0;
			Generations = 5;
			GenerationSize = 25;
			MinimumPerformance = 0.0;
			((NinjaScript)this).Name = Resource.NinjaScriptOptimizerGenetic;
			MutationRatePercent = 2.0;
			MutationStrengthPercent = 25.0;
			ResetSizePercent = 3.0;
			StabilitySizePercent = 4.0;
			random = new Random(Globals.Now.Millisecond);
			stabilityScore = 0.0;
		}
		else
		{
			if ((int)((NinjaScript)this).State != 2)
			{
				return;
			}
			int num = Generations * GenerationSize;
			long parametersCombinationsCount = Optimizer.GetParametersCombinationsCount(((Optimizer)this).Strategies[0]);
			((Optimizer)this).NumberOfIterations = Math.Min(num, parametersCombinationsCount);
			trueBestResults = (int)Math.Min(((Optimizer)this).KeepBestResults, ((Optimizer)this).NumberOfIterations);
			if (parametersCombinationsCount < num)
			{
				trueGenerationSize = (int)Math.Min(GenerationSize, parametersCombinationsCount);
				int num2 = 0;
				int num3 = 0;
				while (num2 < Generations && num3 < parametersCombinationsCount)
				{
					num2++;
					num3 += trueGenerationSize;
				}
				trueGenerations = num2;
			}
			else
			{
				trueGenerationSize = GenerationSize;
				trueGenerations = Generations;
			}
			crossoverIndividuals = Math.Min((int)((double)trueGenerationSize * crossoverRate), trueGenerationSize);
			stabilityIndividuals = Math.Max(1, (int)((double)trueBestResults * stabilitySize));
			resetIndividuals = Math.Max(1, (int)((double)trueBestResults * resetSize));
		}
	}

	protected override void OnOptimize()
	{
		if (((Optimizer)this).Strategies[0].OptimizationParameters.Count == 0)
		{
			return;
		}
		bestResultsGeneration = CreateHolder(trueBestResults);
		Generation[] array = new Generation[trueGenerations];
		for (int i = 0; i < trueGenerations; i++)
		{
			array[i] = new Generation(i);
		}
		double num = 0.0;
		double num2 = 0.0;
		bool flag = false;
		for (int j = 0; j < trueGenerations; j++)
		{
			currentGeneration = CreateHolder(trueGenerationSize);
			((Optimizer)this).Reset(0);
			int num3;
			if (j == 0)
			{
				num3 = CreateRandomIndividuals(0, trueGenerationSize);
			}
			else if ((MathExtentions.ApproxCompare(MinimumPerformance, 0.0) == 0 || maxGenPerformance <= MinimumPerformance) && MathExtentions.ApproxCompare(averagePerformance, 0.0) != 0)
			{
				if (flag)
				{
					array[j - 1].IsStable = true;
					array[j].IsReset = true;
					num3 = AddSurvivors(resetIndividuals);
					if (num3 < trueGenerationSize - 1)
					{
						num3 = CreateRandomIndividuals(num3, trueGenerationSize);
					}
					num2 = 0.0;
				}
				else
				{
					num3 = CreateCrossoverIndividuals(0, crossoverIndividuals);
					num3 = AddSurvivorsCheckDuplicate(num3, Math.Min(trueGenerationSize - 1, num3 + trueBestResults));
					num3 = CreateRandomIndividuals(num3, trueGenerationSize);
				}
			}
			else
			{
				if (MathExtentions.ApproxCompare(array[j - 1].AveragePerformance, 0.0) != 0)
				{
					break;
				}
				num3 = CreateRandomIndividuals(0, trueGenerationSize);
			}
			array[j].AnalyzeInput(currentGeneration);
			SystemPerformance[] results = OptimizeIndividuals(currentGeneration, num3 - 1);
			CreateGeneration(results);
			if (MathExtentions.ApproxCompare(RankPopulation(), 1.0) != 0 || MathExtentions.ApproxCompare(averagePerformance, 0.0) == 0)
			{
				flag = MathExtentions.ApproxCompare(num2, stabilityScore) == 0;
				array[j].AveragePerformance = averagePerformance;
				array[j].MaxPerformance = maxGenPerformance;
				array[j].MinPerformance = minGenPerformance;
				array[j].StabilityScore = stabilityScore;
				array[j].TotalPerformance = totalPerformance;
				if (MathExtentions.ApproxCompare(num, 0.0) != 0)
				{
					array[j].PercentImprovement = (array[j].TotalPerformance - num) / num;
				}
				num2 = stabilityScore;
				num = totalPerformance;
				continue;
			}
			break;
		}
	}

	private int AddSurvivors(int addCount)
	{
		int num = Math.Min(addCount, trueGenerationSize);
		for (int i = 0; i < num; i++)
		{
			if (bestResultsGeneration[i].Type != Individual.IndividualType.Unknown)
			{
				CopyIndividual(bestResultsGeneration[i], currentGeneration[i]);
			}
		}
		return num;
	}

	private int AddSurvivorsCheckDuplicate(int nextGenIdx, int addCount)
	{
		int num = Math.Min(nextGenIdx + addCount, trueGenerationSize);
		int num2 = 0;
		while (nextGenIdx < num && num2 < trueBestResults)
		{
			if (bestResultsGeneration[num2].Type != Individual.IndividualType.Unknown && !ContainsDuplicate(currentGeneration, bestResultsGeneration[num2], nextGenIdx))
			{
				CopyIndividual(bestResultsGeneration[num2], currentGeneration[nextGenIdx]);
				nextGenIdx++;
			}
			num2++;
		}
		return nextGenIdx;
	}

	private static void CopyIndividual(Individual from, Individual to)
	{
		to.Type = from.Type;
		to.PerformanceValue = from.PerformanceValue;
		to.Weight = from.Weight;
		for (int i = 0; i < to.Parameters.GetUpperBound(0) + 1; i++)
		{
			from.Parameters[i].CopyTo(to.Parameters[i]);
		}
	}

	private int CreateCrossoverIndividuals(int nextGenIdx, int addCount)
	{
		int num = 0;
		int num2 = Math.Min(nextGenIdx + addCount, trueGenerationSize);
		int num3 = nextGenIdx - 1;
		while (num < ConvergenceThreshold && nextGenIdx < num2)
		{
			int num4 = RouletteSelection(bestResultsGeneration);
			int num5 = RouletteSelection(bestResultsGeneration, num4);
			int num6 = Math.Min(nextGenIdx + 1, currentGeneration.Length - 1);
			Individual individual = new Individual(((Optimizer)this).Strategies[0].OptimizationParameters.Count);
			Individual individual2 = new Individual(((Optimizer)this).Strategies[0].OptimizationParameters.Count);
			Crossover(bestResultsGeneration[num4], bestResultsGeneration[num5], individual, individual2);
			individual.Type = Individual.IndividualType.Child;
			individual2.Type = Individual.IndividualType.Child;
			MutateIndividual(individual);
			MutateIndividual(individual2);
			if (!ContainsDuplicate(bestResultsGeneration, individual, trueBestResults) && !ContainsDuplicate(currentGeneration, individual, currentGeneration.Length))
			{
				currentGeneration[nextGenIdx] = individual;
				if (!ContainsDuplicate(bestResultsGeneration, individual2, nextGenIdx) && !ContainsDuplicate(currentGeneration, individual2, currentGeneration.Length))
				{
					currentGeneration[num6] = individual2;
					nextGenIdx += 2;
					num3 = num6;
				}
				else
				{
					num3 = nextGenIdx;
					nextGenIdx++;
				}
			}
			else if (!ContainsDuplicate(bestResultsGeneration, individual2, trueBestResults) && !ContainsDuplicate(currentGeneration, individual2, currentGeneration.Length))
			{
				currentGeneration[nextGenIdx] = individual2;
				num3 = nextGenIdx;
				nextGenIdx++;
			}
			else
			{
				num++;
			}
		}
		return num3 + 1;
	}

	private void CreateGeneration(SystemPerformance[] results)
	{
		stabilityScore = 0.0;
		totalPerformance = 0.0;
		averagePerformance = 0.0;
		for (int i = 0; i < results.Length; i++)
		{
			if (results[i].ParameterValues == null)
			{
				continue;
			}
			for (int j = 0; j < ((Optimizer)this).Strategies[0].OptimizationParameters.Count; j++)
			{
				Parameter val = ((Optimizer)this).Strategies[0].OptimizationParameters[j];
				if (val.ParameterType == typeof(int))
				{
					val.Value = (int)results[i].ParameterValues[j];
				}
				else if (val.ParameterType == typeof(double))
				{
					val.Value = (double)results[i].ParameterValues[j];
				}
				else if (val.ParameterType == typeof(bool))
				{
					val.Value = (bool)results[i].ParameterValues[j];
				}
				else if (val.ParameterType.IsEnum)
				{
					val.Value = results[i].ParameterValues[j];
				}
				val.CopyTo(bestResultsGeneration[i].Parameters[j]);
			}
			bestResultsGeneration[i].PerformanceValue = results[i].PerformanceValue;
			if (maxGenPerformance < results[i].PerformanceValue)
			{
				maxGenPerformance = results[i].PerformanceValue;
			}
			if (MathExtentions.ApproxCompare(minGenPerformance, 0.0) == 0 || minGenPerformance > results[i].PerformanceValue)
			{
				minGenPerformance = results[i].PerformanceValue;
			}
			if (i < stabilityIndividuals)
			{
				stabilityScore += results[i].PerformanceValue;
			}
			totalPerformance += results[i].PerformanceValue;
			bestResultsGeneration[i].Type = Individual.IndividualType.Parent;
		}
		averagePerformance = totalPerformance / (double)trueBestResults;
	}

	private Individual[] CreateHolder(int size)
	{
		Individual[] array = new Individual[size];
		for (int i = 0; i < size; i++)
		{
			array[i] = new Individual(((Optimizer)this).Strategies[0].OptimizationParameters.Count);
		}
		return array;
	}

	private int CreateRandomIndividuals(int nextGenIdx, int addCount)
	{
		int num = 0;
		int num2 = Math.Min(nextGenIdx + addCount, trueGenerationSize);
		while (num < ConvergenceThreshold && nextGenIdx < num2)
		{
			Individual individual = new Individual(((Optimizer)this).Strategies[0].OptimizationParameters.Count);
			for (int i = 0; i < ((Optimizer)this).Strategies[0].OptimizationParameters.Count; i++)
			{
				Parameter val = ((Optimizer)this).Strategies[0].OptimizationParameters[i];
				if (val.ParameterType == typeof(int))
				{
					int num3 = Math.Max(1, (int)val.Increment);
					int maxValue = ((int)val.Max - (int)val.Min) / num3 + 1;
					int num4 = random.Next(0, maxValue);
					val.Value = (int)val.Min + num4 * num3;
				}
				else if (val.ParameterType == typeof(double))
				{
					double num5 = (double)val.Min;
					double num6 = (double)val.Max;
					double num7 = Math.Max(1E-08, val.Increment);
					int num8 = (int)((num6 - num5) / num7);
					int num9 = random.Next(0, num8 + 1);
					val.Value = (double)val.Min + (double)num9 * num7;
				}
				else if (val.ParameterType == typeof(bool))
				{
					if ((bool)val.Min != (bool)val.Max)
					{
						val.Value = random.Next(0, 2) == 1;
					}
					else
					{
						val.Value = val.Min;
					}
				}
				else if (val.ParameterType.IsEnum)
				{
					val.Value = val.EnumValues[random.Next(0, val.EnumValues.Length)];
				}
				val.CopyTo(individual.Parameters[i]);
			}
			if (!ContainsDuplicate(currentGeneration, individual, nextGenIdx + 1))
			{
				individual.Type = Individual.IndividualType.Random;
				currentGeneration[nextGenIdx] = individual;
				nextGenIdx++;
			}
			else
			{
				num++;
			}
		}
		return nextGenIdx;
	}

	private void Crossover(Individual parent1, Individual parent2, Individual child1, Individual child2)
	{
		int num = random.Next(((Optimizer)this).Strategies[0].OptimizationParameters.Count);
		for (int i = 0; i < ((Optimizer)this).Strategies[0].OptimizationParameters.Count; i++)
		{
			if (i < num)
			{
				parent1.Parameters[i].CopyTo(child1.Parameters[i]);
				parent2.Parameters[i].CopyTo(child2.Parameters[i]);
			}
			else
			{
				parent2.Parameters[i].CopyTo(child1.Parameters[i]);
				parent1.Parameters[i].CopyTo(child2.Parameters[i]);
			}
		}
	}

	private static bool ContainsDuplicate(Individual[] individuals, Individual individual, int idx)
	{
		bool result = false;
		for (int num = Math.Min(idx, individuals.Length) - 1; num >= 0; num--)
		{
			if (!(individuals[num].IndividualName != individual.IndividualName))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public void MutateIndividual(Individual individual)
	{
		if (!(mutationRate >= random.NextDouble()))
		{
			return;
		}
		for (int i = 0; i < ((Optimizer)this).Strategies[0].OptimizationParameters.Count; i++)
		{
			Parameter val = individual.Parameters[i];
			if (val.ParameterType == typeof(int))
			{
				val.Value = Math.Min((int)val.Max, Math.Max((int)val.Min, (int)val.Value + (int)val.Increment * (random.Next(0, 3) - 1)));
			}
			else if (val.ParameterType == typeof(double))
			{
				double num = Math.Max((double)val.Min, (double)val.Value * (1.0 - mutationStrength));
				double num2 = Math.Min((double)val.Max, (double)val.Value * (1.0 + mutationStrength));
				val.Value = Math.Min(num2, Math.Max(num, random.NextDouble() * (num2 - num) + num));
			}
			else if (val.ParameterType == typeof(bool))
			{
				if ((bool)val.Min != (bool)val.Max)
				{
					val.Value = random.Next(0, 2) == 1;
				}
				else
				{
					val.Value = val.Min;
				}
			}
			else if (val.ParameterType.IsEnum)
			{
				val.Value = val.EnumValues[random.Next(0, val.EnumValues.Length)];
			}
			val.CopyTo(individual.Parameters[i]);
		}
		individual.Type = Individual.IndividualType.Mutant;
	}

	private SystemPerformance[] OptimizeIndividuals(Individual[] nextGen, int idx)
	{
		for (int i = 0; i <= idx; i++)
		{
			for (int j = 0; j < ((Optimizer)this).Strategies[0].OptimizationParameters.Count; j++)
			{
				((Optimizer)this).Strategies[0].OptimizationParameters[j].Value = nextGen[i].Parameters[j].Value;
			}
			((Optimizer)this).RunIteration();
		}
		((Optimizer)this).WaitForIterationsCompleted();
		return ((Optimizer)this).Results;
	}

	private double RankPopulation()
	{
		double performanceValue = bestResultsGeneration[trueBestResults - 1].PerformanceValue;
		double performanceValue2 = bestResultsGeneration[0].PerformanceValue;
		double num = 0.0;
		double num2 = ((MathExtentions.ApproxCompare(performanceValue2, performanceValue) == 0) ? 1.0 : (performanceValue2 - performanceValue));
		for (int i = 0; i < trueBestResults; i++)
		{
			bestResultsGeneration[i].Weight = (bestResultsGeneration[i].PerformanceValue - performanceValue) / num2;
			num += bestResultsGeneration[i].Weight;
		}
		if (MathExtentions.ApproxCompare(num, 0.0) == 0)
		{
			num = 1.0;
		}
		bestResultsGeneration[trueBestResults - 1].Weight = 0.0;
		for (int num3 = trueBestResults - 2; num3 >= 0; num3--)
		{
			bestResultsGeneration[num3].Weight = bestResultsGeneration[num3 + 1].Weight + bestResultsGeneration[num3].Weight / num;
		}
		return num;
	}

	private int RouletteSelection(Individual[] individuals, int self)
	{
		int num = 0;
		int num2;
		do
		{
			num2 = RouletteSelection(individuals);
		}
		while (num2 == self && num++ < 5);
		return num2;
	}

	private int RouletteSelection(Individual[] individuals)
	{
		double num = random.NextDouble();
		int num2 = -1;
		int num3 = 0;
		for (int i = 0; i < individuals.Length && individuals[i].Type != Individual.IndividualType.Unknown; i++)
		{
			num3 = i;
		}
		int num4 = 0;
		int num5 = num3 / 2;
		while (num2 == -1 && num4 <= num3)
		{
			if (num < individuals[num5].Weight)
			{
				num4 = num5;
			}
			else
			{
				num3 = num5;
			}
			num5 = (num4 + num3) / 2;
			if (num3 - num4 == 1)
			{
				num2 = num3;
			}
		}
		return num2;
	}
}

using System;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.PerformanceMetrics;

public class SampleCumProfit : PerformanceMetric
{
	private Currency denomination = (Currency)(-1);

	[Display(ResourceType = typeof(Resource), Description = "SampleCumProfitDescription", Name = "SampleCumProfit", Order = 0)]
	public double[] Values { get; private set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.PerformanceMetricSampleCumProfit;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			Values = new double[5];
		}
		else if ((int)((NinjaScript)this).State == 3)
		{
			Array.Clear(Values, 0, Values.Length);
		}
	}

	protected override void OnAddTrade(Trade trade)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if ((int)denomination == -1)
		{
			denomination = trade.Exit.Account.Denomination;
		}
		Values[0] += trade.ProfitCurrency;
		Values[1] = (1.0 + Values[1]) * (1.0 + trade.ProfitPercent) - 1.0;
		Values[2] += trade.ProfitPips;
		Values[3] += trade.ProfitPoints;
		Values[4] += trade.ProfitTicks;
	}

	protected override void OnCopyTo(PerformanceMetricBase target)
	{
		if (target is SampleCumProfit sampleCumProfit)
		{
			Array.Copy(Values, sampleCumProfit.Values, Values.Length);
		}
	}

	protected override void OnMergePerformanceMetric(PerformanceMetricBase target)
	{
		if (target is SampleCumProfit sampleCumProfit && ((PerformanceMetricBase)this).TradesPerformance.TradesCount + ((PerformanceMetricBase)sampleCumProfit).TradesPerformance.TradesCount > 0)
		{
			for (int i = 0; i < Values.Length; i++)
			{
				sampleCumProfit.Values[i] = (sampleCumProfit.Values[i] * (double)((PerformanceMetricBase)sampleCumProfit).TradesPerformance.TradesCount + Values[i] * (double)((PerformanceMetricBase)this).TradesPerformance.TradesCount) / (double)(((PerformanceMetricBase)this).TradesPerformance.TradesCount + ((PerformanceMetricBase)sampleCumProfit).TradesPerformance.TradesCount);
			}
		}
	}

	public override string Format(object value, PerformanceUnit unit, string propertyName)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected I4, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (value is double[] array && array.Length == 5)
		{
			switch ((int)unit)
			{
			case 0:
				return Globals.FormatCurrency(array[0], denomination);
			case 1:
				return array[1].ToString("P");
			case 2:
				return Math.Round(array[2]).ToString(Globals.GeneralOptions.CurrentCulture);
			case 3:
				return Math.Round(array[3]).ToString(Globals.GeneralOptions.CurrentCulture);
			case 4:
				return Math.Round(array[4]).ToString(Globals.GeneralOptions.CurrentCulture);
			}
		}
		return value.ToString();
	}
}

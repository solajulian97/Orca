using System;

namespace NinjaTrader.NinjaScript.Indicators;

public class OrcaVwapSession
{
	public double SumVol;

	public double SumPriceVol;

	public double SumPrice2Vol;

	public double Vwap
	{
		get
		{
			if (!(SumVol > 0.0))
			{
				return 0.0;
			}
			return SumPriceVol / SumVol;
		}
	}

	public double MathVariance
	{
		get
		{
			if (!(SumVol > 0.0))
			{
				return 0.0;
			}
			return Math.Max(0.0, SumPrice2Vol / SumVol - Vwap * Vwap);
		}
	}

	public double StdDev => Math.Sqrt(MathVariance);

	public void Add(double price, double vol)
	{
		SumVol += vol;
		SumPriceVol += price * vol;
		SumPrice2Vol += price * price * vol;
	}

	public void Reset()
	{
		SumVol = 0.0;
		SumPriceVol = 0.0;
		SumPrice2Vol = 0.0;
	}
}

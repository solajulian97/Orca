#region Using declarations
using System;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
#endregion

namespace NinjaTrader.NinjaScript.AddOns
{
	internal sealed class OrcaCompletedBarAtrSnapshot
	{
		public double Value { get; set; }
		public int Period { get; set; }
		public int CompletedBarIndex { get; set; }
		public int CompletedBarCount { get; set; }
		public DateTime CompletedBarTime { get; set; }
	}

	internal sealed class OrcaCompletedBarAtrCalculator
	{
		private Bars sourceBars;
		private int period;
		private int lastCompletedIndex = -1;
		private DateTime lastCompletedTime = DateTime.MinValue;
		private double lastCompletedHigh = double.NaN;
		private double lastCompletedLow = double.NaN;
		private double lastCompletedClose = double.NaN;
		private double atr = double.NaN;

		public void Reset()
		{
			sourceBars = null;
			period = 0;
			lastCompletedIndex = -1;
			lastCompletedTime = DateTime.MinValue;
			lastCompletedHigh = double.NaN;
			lastCompletedLow = double.NaN;
			lastCompletedClose = double.NaN;
			atr = double.NaN;
		}

		public bool TryUpdate(Bars bars, int requestedPeriod, out OrcaCompletedBarAtrSnapshot snapshot)
		{
			snapshot = null;
			if (bars == null || requestedPeriod < 1 || bars.Count < requestedPeriod + 1)
				return false;

			int completedIndex = bars.Count - 2;
			if (completedIndex < requestedPeriod - 1)
				return false;

			try {
				DateTime completedTime = bars.GetTime(completedIndex);
				double completedHigh = bars.GetHigh(completedIndex);
				double completedLow = bars.GetLow(completedIndex);
				double completedClose = bars.GetClose(completedIndex);
				bool sourceChanged = !object.ReferenceEquals(sourceBars, bars) || period != requestedPeriod;
				bool historyRewound = completedIndex < lastCompletedIndex;
				bool currentCompletedBarChanged = completedIndex == lastCompletedIndex
					&& (completedTime != lastCompletedTime
						|| !NearlyEqual(completedHigh, lastCompletedHigh)
						|| !NearlyEqual(completedLow, lastCompletedLow)
						|| !NearlyEqual(completedClose, lastCompletedClose));

				if (sourceChanged || historyRewound || currentCompletedBarChanged || lastCompletedIndex < 0) {
					Rebuild(bars, requestedPeriod, completedIndex);
				} else if (completedIndex > lastCompletedIndex) {
					for (int index = lastCompletedIndex + 1; index <= completedIndex; index++)
						ApplyBar(bars, index, requestedPeriod);
					lastCompletedIndex = completedIndex;
				}

				lastCompletedTime = completedTime;
				lastCompletedHigh = completedHigh;
				lastCompletedLow = completedLow;
				lastCompletedClose = completedClose;
				if (double.IsNaN(atr) || double.IsInfinity(atr) || atr <= 0)
					return false;

				snapshot = new OrcaCompletedBarAtrSnapshot {
					Value = atr,
					Period = requestedPeriod,
					CompletedBarIndex = completedIndex,
					CompletedBarCount = completedIndex + 1,
					CompletedBarTime = completedTime
				};
				return true;
			} catch {
				Reset();
				return false;
			}
		}

		private void Rebuild(Bars bars, int requestedPeriod, int completedIndex)
		{
			sourceBars = bars;
			period = requestedPeriod;
			lastCompletedIndex = -1;
			atr = double.NaN;
			for (int index = 0; index <= completedIndex; index++)
				ApplyBar(bars, index, requestedPeriod);
			lastCompletedIndex = completedIndex;
		}

		private void ApplyBar(Bars bars, int index, int requestedPeriod)
		{
			double high = bars.GetHigh(index);
			double low = bars.GetLow(index);
			double trueRange = high - low;
			if (index > 0) {
				double previousClose = bars.GetClose(index - 1);
				trueRange = Math.Max(trueRange, Math.Max(Math.Abs(high - previousClose), Math.Abs(low - previousClose)));
			}

			if (index == 0 || double.IsNaN(atr)) {
				atr = trueRange;
				return;
			}

			int divisor = Math.Min(index + 1, requestedPeriod);
			atr = ((divisor - 1) * atr + trueRange) / divisor;
		}

		private static bool NearlyEqual(double left, double right)
		{
			if (double.IsNaN(left) || double.IsNaN(right))
				return false;
			return Math.Abs(left - right) <= 0.0000000001;
		}
	}

	internal sealed class OrcaAtrSizingResult
	{
		public double AtrValue { get; set; }
		public double Multiplier { get; set; }
		public double RawStopPoints { get; set; }
		public int StopTicks { get; set; }
		public double StopPoints { get; set; }
		public double RiskPerContract { get; set; }
		public double RiskBudget { get; set; }
		public int UncappedQuantity { get; set; }
		public int Quantity { get; set; }
		public int MaxQuantity { get; set; }
		public double PlannedRisk { get; set; }
		public bool IsCapped { get; set; }
		public bool IsBelowMinimum { get; set; }
	}

	internal static class OrcaRiskSizingMath
	{
		public static bool TryCalculateAtr(
			double atrValue,
			double multiplier,
			double riskBudget,
			Instrument executionInstrument,
			int maxQuantity,
			out OrcaAtrSizingResult result)
		{
			result = null;
			if (!IsPositiveFinite(atrValue) || !IsPositiveFinite(multiplier))
				return false;
			return TryCalculateStop(
				atrValue * multiplier,
				atrValue,
				multiplier,
				riskBudget,
				executionInstrument,
				maxQuantity,
				out result);
		}

		public static bool TryCalculateStop(
			double requestedStopPoints,
			double atrValue,
			double multiplier,
			double riskBudget,
			Instrument executionInstrument,
			int maxQuantity,
			out OrcaAtrSizingResult result)
		{
			result = null;
			double tickSize = executionInstrument?.MasterInstrument?.TickSize ?? 0;
			double pointValue = executionInstrument?.MasterInstrument?.PointValue ?? 0;
			if (!IsPositiveFinite(requestedStopPoints)
				|| !IsPositiveFinite(riskBudget)
				|| !IsPositiveFinite(tickSize)
				|| !IsPositiveFinite(pointValue))
				return false;

			int boundedMax = Math.Max(1, maxQuantity);
			int stopTicks = Math.Max(1, (int)Math.Ceiling((requestedStopPoints / tickSize) - 0.0000000001));
			double stopPoints = stopTicks * tickSize;
			double riskPerContract = stopPoints * pointValue;
			if (!IsPositiveFinite(riskPerContract))
				return false;

			int uncappedQuantity = Math.Max(0, (int)Math.Floor((riskBudget / riskPerContract) + 0.0000000001));
			int quantity = Math.Min(uncappedQuantity, boundedMax);
			result = new OrcaAtrSizingResult {
				AtrValue = atrValue,
				Multiplier = multiplier,
				RawStopPoints = requestedStopPoints,
				StopTicks = stopTicks,
				StopPoints = stopPoints,
				RiskPerContract = riskPerContract,
				RiskBudget = riskBudget,
				UncappedQuantity = uncappedQuantity,
				Quantity = quantity,
				MaxQuantity = boundedMax,
				PlannedRisk = quantity * riskPerContract,
				IsCapped = uncappedQuantity > boundedMax,
				IsBelowMinimum = uncappedQuantity < 1
			};
			return true;
		}

		private static bool IsPositiveFinite(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
		}
	}
}

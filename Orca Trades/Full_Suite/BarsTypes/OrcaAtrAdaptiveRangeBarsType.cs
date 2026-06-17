#region Using declarations
using System;
using System.ComponentModel;
using NinjaTrader.Data;
#endregion

#pragma warning disable 0612

namespace NinjaTrader.NinjaScript.BarsTypes
{
	public class OrcaAtrAdaptiveRangeBarsType : BarsType
	{
		private const int OrcaBarsPeriodTypeId = 50501;
		private const int DefaultAtrLength = 14;
		private const int DefaultAtrSourceSeconds = 60;
		private const int DefaultRangeMultiplierPercent = 100;
		private const int DefaultStartupRangeTicks = 80;
		private const int MinimumRangeTicks = 1;

		private static readonly BarsPeriodType OrcaBarsPeriodType = (BarsPeriodType) OrcaBarsPeriodTypeId;

		private double activeRangePrice;
		private int activeRangeTicks;

		private DateTime atrBarEnd = Core.Globals.MinDate;
		private double atrClose;
		private double atrHigh;
		private double atrLow;
		private int atrSamples;
		private double atrSeedSum;
		private double latestAtr;
		private DateTime lastDataPointTime = Core.Globals.MinDate;
		private double previousAtrClose = double.NaN;

		public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
		{
			period.BaseBarsPeriodType = BarsPeriodType.Second;
			period.BaseBarsPeriodValue = DefaultAtrSourceSeconds;
		}

		public override void ApplyDefaultValue(BarsPeriod period)
		{
			period.Value = DefaultAtrLength;
			period.Value2 = DefaultRangeMultiplierPercent;
		}

		public override string ChartLabel(DateTime time)
		{
			return time.ToString("T", Core.Globals.GeneralOptions.CurrentCulture);
		}

		public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
		{
			return 5;
		}

		public override double GetPercentComplete(Bars bars, DateTime now)
		{
			if (bars.Count == 0 || activeRangePrice <= 0)
				return 0;

			double currentRange = bars.GetHigh(bars.Count - 1) - bars.GetLow(bars.Count - 1);
			return Math.Max(0, Math.Min(1, currentRange / activeRangePrice));
		}

		protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
		{
			SessionIterator = SessionIterator ?? new SessionIterator(bars);

			bool isNewSession = SessionIterator.IsNewSession(time, isBar);
			if (isNewSession)
				SessionIterator.GetNextSession(time, isBar);

			if (bars.IsResetOnNewTradingDay && isNewSession)
				ResetAtrSource(false);

			ProcessAtrSource(bars, close, time, isBar);

			if (bars.Count == 0 || ShouldStartFreshBar(bars, isNewSession, time))
			{
				LockNextRange(bars);
				AddFreshAnchorBar(bars, close, time, volume);
				bars.LastPrice = close;
				lastDataPointTime = time;
				return;
			}

			if (activeRangeTicks < MinimumRangeTicks || activeRangePrice <= 0)
				LockNextRange(bars);

			ProcessAdaptiveRangeBar(bars, close, time, volume);
			bars.LastPrice = close;
			lastDataPointTime = time;
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca ATR Adaptive Range";
				BarsPeriod = new BarsPeriod
				{
					BarsPeriodType = OrcaBarsPeriodType,
					BarsPeriodTypeName = "Orca ATR Adaptive Range",
					BaseBarsPeriodType = BarsPeriodType.Second,
					BaseBarsPeriodValue = DefaultAtrSourceSeconds,
					Value = DefaultAtrLength,
					Value2 = DefaultRangeMultiplierPercent
				};
				BuiltFrom = BarsPeriodType.Tick;
				DaysToLoad = 5;
				WeeksToLoad = 1;
				IsIntraday = true;
				IsTimeBased = false;
			}
			else if (State == State.Configure)
			{
				NormalizeBarsPeriod(BarsPeriod);

				double multiplier = GetRangeMultiplier(BarsPeriod);
				string marketDataSuffix = BarsPeriod.MarketDataType != MarketDataType.Last ? string.Format(" - {0}", BarsPeriod.MarketDataType) : string.Empty;
				Name = string.Format(
					Core.Globals.GeneralOptions.CurrentCulture,
					"Orca ATR Adaptive Range ({0} ATR, {1}, x{2:0.##}){3}",
					BarsPeriod.Value,
					GetAtrSourceLabel(BarsPeriod),
					multiplier,
					marketDataSuffix);

				RemoveProperty("PointAndFigurePriceType");
				RemoveProperty("ReversalType");
				RemoveProperty("VolumetricDeltaType");
				RemoveProperty("BaseBarsPeriodType");

				SetPropertyName("Value", "ATR Length");
				SetPropertyName("BaseBarsPeriodValue", "ATR Source Seconds");
				SetPropertyName("Value2", "Range Multiplier x100");
				SetPropertyOrder("Value", 1);
				SetPropertyOrder("BaseBarsPeriodValue", 2);
				SetPropertyOrder("Value2", 3);
			}
		}

		private void CompleteAtrSourceBar(Bars bars)
		{
			int length = GetAtrLength(bars.BarsPeriod);
			double trueRange = atrHigh - atrLow;

			if (!double.IsNaN(previousAtrClose))
				trueRange = Math.Max(trueRange, Math.Max(Math.Abs(atrHigh - previousAtrClose), Math.Abs(atrLow - previousAtrClose)));

			if (atrSamples < length)
			{
				atrSamples++;
				atrSeedSum += trueRange;
				latestAtr = atrSeedSum / atrSamples;
			}
			else
				latestAtr = (latestAtr * (length - 1) + trueRange) / length;

			previousAtrClose = atrClose;
		}

		private int ComputeRangeTicks(Bars bars)
		{
			double tickSize = bars.Instrument.MasterInstrument.TickSize;
			if (tickSize <= 0)
				return MinimumRangeTicks;

			double atrTicks = latestAtr > 0 ? latestAtr / tickSize : DefaultStartupRangeTicks;
			int rangeTicks = (int) Math.Round(atrTicks * GetRangeMultiplier(bars.BarsPeriod), MidpointRounding.AwayFromZero);

			return Math.Max(MinimumRangeTicks, rangeTicks);
		}

		private void AddFreshAnchorBar(Bars bars, double close, DateTime time, long volume)
		{
			AddBar(bars, close, close, close, close, time, volume);
		}

		private int GetAtrLength(BarsPeriod period)
		{
			return Math.Max(1, period.Value);
		}

		private string GetAtrSourceLabel(BarsPeriod period)
		{
			return string.Format(
				Core.Globals.GeneralOptions.CurrentCulture,
				"{0}s",
				Math.Max(1, period.BaseBarsPeriodValue));
		}

		private double GetAtrSourcePeriodSeconds(BarsPeriod period)
		{
			return Math.Max(1, period.BaseBarsPeriodValue);
		}

		private DateTime GetAtrSourceBarEnd(Bars bars, DateTime time, bool isBar)
		{
			double sourceSeconds = GetAtrSourcePeriodSeconds(bars.BarsPeriod);
			DateTime sessionBegin = SessionIterator.ActualSessionBegin;
			DateTime sessionEnd = SessionIterator.ActualSessionEnd;

			if (sessionBegin == Core.Globals.MinDate)
				return time;

			double elapsedSeconds = Math.Max(0, time.Subtract(sessionBegin).TotalSeconds);
			double periodCount = isBar
				? Math.Ceiling(Math.Ceiling(elapsedSeconds) / sourceSeconds)
				: 1 + Math.Floor(Math.Floor(elapsedSeconds) / sourceSeconds);

			DateTime barEnd = sessionBegin.AddSeconds(periodCount * sourceSeconds);
			if (bars.TradingHours.Sessions.Count > 0 && barEnd > sessionEnd)
				barEnd = sessionEnd;

			return barEnd;
		}

		private double GetRangeMultiplier(BarsPeriod period)
		{
			return Math.Max(1, period.Value2) / 100.0;
		}

		private void LockNextRange(Bars bars)
		{
			activeRangeTicks = ComputeRangeTicks(bars);
			activeRangePrice = bars.Instrument.MasterInstrument.RoundToTickSize(activeRangeTicks * bars.Instrument.MasterInstrument.TickSize);
		}

		private void NormalizeBarsPeriod(BarsPeriod period)
		{
			period.BarsPeriodType = OrcaBarsPeriodType;
			period.BarsPeriodTypeSerialize = OrcaBarsPeriodTypeId;
			period.BarsPeriodTypeName = "Orca ATR Adaptive Range";

			if (period.BaseBarsPeriodType == BarsPeriodType.Minute)
				period.BaseBarsPeriodValue = Math.Max(1, period.BaseBarsPeriodValue) * 60;
			else if (period.BaseBarsPeriodType != BarsPeriodType.Second)
				period.BaseBarsPeriodValue = DefaultAtrSourceSeconds;

			period.BaseBarsPeriodType = BarsPeriodType.Second;

			if (period.Value < 1)
				period.Value = DefaultAtrLength;
			if (period.Value2 < 1)
				period.Value2 = DefaultRangeMultiplierPercent;
			if (period.BaseBarsPeriodValue < 1)
				period.BaseBarsPeriodValue = DefaultAtrSourceSeconds;
		}

		private void ProcessAdaptiveRangeBar(Bars bars, double close, DateTime time, long volume)
		{
			double barClose = bars.GetClose(bars.Count - 1);
			double barHigh = bars.GetHigh(bars.Count - 1);
			double barLow = bars.GetLow(bars.Count - 1);
			double tickSize = bars.Instrument.MasterInstrument.TickSize;

			if (bars.Instrument.MasterInstrument.Compare(close, barLow + activeRangePrice) > 0)
			{
				double closedClose = bars.Instrument.MasterInstrument.RoundToTickSize(barLow + activeRangePrice);
				if (bars.Instrument.MasterInstrument.Compare(closedClose, barClose) > 0)
					UpdateBar(bars, closedClose, barLow, closedClose, time, 0);

				double newBarOpen = bars.Instrument.MasterInstrument.RoundToTickSize(closedClose + tickSize);
				while (bars.Instrument.MasterInstrument.Compare(close, closedClose) > 0)
				{
					LockNextRange(bars);

					double nextBoundary = bars.Instrument.MasterInstrument.RoundToTickSize(newBarOpen + activeRangePrice);
					bool hasMore = bars.Instrument.MasterInstrument.Compare(close, nextBoundary) > 0;
					double newClose = hasMore ? nextBoundary : close;

					AddBar(bars, newBarOpen, Math.Max(newBarOpen, newClose), Math.Min(newBarOpen, newClose), newClose, time, hasMore ? 0 : volume);

					closedClose = newClose;
					newBarOpen = bars.Instrument.MasterInstrument.RoundToTickSize(closedClose + tickSize);
				}
			}
			else if (bars.Instrument.MasterInstrument.Compare(barHigh - activeRangePrice, close) > 0)
			{
				double closedClose = bars.Instrument.MasterInstrument.RoundToTickSize(barHigh - activeRangePrice);
				if (bars.Instrument.MasterInstrument.Compare(barClose, closedClose) > 0)
					UpdateBar(bars, barHigh, closedClose, closedClose, time, 0);

				double newBarOpen = bars.Instrument.MasterInstrument.RoundToTickSize(closedClose - tickSize);
				while (bars.Instrument.MasterInstrument.Compare(closedClose, close) > 0)
				{
					LockNextRange(bars);

					double nextBoundary = bars.Instrument.MasterInstrument.RoundToTickSize(newBarOpen - activeRangePrice);
					bool hasMore = bars.Instrument.MasterInstrument.Compare(nextBoundary, close) > 0;
					double newClose = hasMore ? nextBoundary : close;

					AddBar(bars, newBarOpen, Math.Max(newBarOpen, newClose), Math.Min(newBarOpen, newClose), newClose, time, hasMore ? 0 : volume);

					closedClose = newClose;
					newBarOpen = bars.Instrument.MasterInstrument.RoundToTickSize(closedClose - tickSize);
				}
			}
			else
				UpdateBar(bars, close > barHigh ? close : barHigh, close < barLow ? close : barLow, close, time, volume);
		}

		private void ProcessAtrSource(Bars bars, double close, DateTime time, bool isBar)
		{
			DateTime sourceBarEnd = GetAtrSourceBarEnd(bars, time, isBar);

			if (atrBarEnd == Core.Globals.MinDate)
			{
				StartAtrSourceBar(sourceBarEnd, close);
				return;
			}

			if (sourceBarEnd > atrBarEnd)
			{
				CompleteAtrSourceBar(bars);
				StartAtrSourceBar(sourceBarEnd, close);
				return;
			}

			atrHigh = Math.Max(atrHigh, close);
			atrLow = Math.Min(atrLow, close);
			atrClose = close;
		}

		private void RemoveProperty(string propertyName)
		{
			PropertyDescriptor property = Properties.Find(propertyName, true);
			if (property != null)
				Properties.Remove(property);
		}

		private void ResetAtrSource(bool resetAtrEstimate)
		{
			atrBarEnd = Core.Globals.MinDate;
			atrHigh = 0;
			atrLow = 0;
			atrClose = 0;
			previousAtrClose = double.NaN;

			if (!resetAtrEstimate)
				return;

			atrSamples = 0;
			atrSeedSum = 0;
			latestAtr = 0;
		}

		private bool ShouldStartFreshBar(Bars bars, bool isNewSession, DateTime time)
		{
			if (bars.IsResetOnNewTradingDay && isNewSession)
				return true;

			if (isNewSession)
				return true;

			if (lastDataPointTime == Core.Globals.MinDate || time <= lastDataPointTime)
				return false;

			double sourceMinutes = GetAtrSourcePeriodSeconds(bars.BarsPeriod) / 60.0;
			double gapMinutes = time.Subtract(lastDataPointTime).TotalMinutes;
			double gapThresholdMinutes = Math.Max(5, sourceMinutes * 2);

			return gapMinutes > gapThresholdMinutes;
		}

		private void StartAtrSourceBar(DateTime sourceBarEnd, double price)
		{
			atrBarEnd = sourceBarEnd;
			atrHigh = price;
			atrLow = price;
			atrClose = price;
		}
	}
}
#pragma warning restore 0612

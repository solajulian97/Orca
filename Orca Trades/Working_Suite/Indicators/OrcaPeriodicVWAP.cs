#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum OrcaPeriodicVwapWindowBasis
	{
		Periodic = 0,
		Session = 1
	}

	public enum OrcaPeriodicVwapInterval
	{
		Minutes5 = 5,
		Minutes15 = 15,
		Minutes30 = 30,
		Hour1 = 60,
		Hours4 = 240,
		Daily = 1440
	}

	public enum OrcaPeriodicVwapSessionConfiguration
	{
		OvernightAndRth = 0,
		AsiaLondonRth = 1
	}

	public sealed class OrcaPeriodicVwapIntervalConverter : EnumConverter
	{
		public OrcaPeriodicVwapIntervalConverter() : base(typeof(OrcaPeriodicVwapInterval)) { }

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is OrcaPeriodicVwapInterval)
			{
				switch ((OrcaPeriodicVwapInterval)value)
				{
					case OrcaPeriodicVwapInterval.Minutes5: return "5 Minutes";
					case OrcaPeriodicVwapInterval.Minutes15: return "15 Minutes";
					case OrcaPeriodicVwapInterval.Minutes30: return "30 Minutes";
					case OrcaPeriodicVwapInterval.Hour1: return "1 Hour";
					case OrcaPeriodicVwapInterval.Hours4: return "4 Hours";
					case OrcaPeriodicVwapInterval.Daily: return "Daily";
				}
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	public sealed class OrcaPeriodicVwapSessionConfigurationConverter : EnumConverter
	{
		public OrcaPeriodicVwapSessionConfigurationConverter() : base(typeof(OrcaPeriodicVwapSessionConfiguration)) { }

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value is OrcaPeriodicVwapSessionConfiguration)
				return (OrcaPeriodicVwapSessionConfiguration)value == OrcaPeriodicVwapSessionConfiguration.AsiaLondonRth
					? "Asia / London / RTH"
					: "Overnight / RTH";

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	[TypeConverter(typeof(OrcaPeriodicVwapTypeConverter))]
	public class OrcaPeriodicVWAP : Indicator
	{
		private const int VwapPlot = 0;
		private const int Dev1UpperPlot = 1;
		private const int Dev1LowerPlot = 2;
		private const int Dev2UpperPlot = 3;
		private const int Dev2LowerPlot = 4;
		private const int Dev3UpperPlot = 5;
		private const int Dev3LowerPlot = 6;

		private sealed class VwapAccumulator
		{
			public double SumVolume;
			public double SumPriceVolume;
			public double SumPriceSquaredVolume;

			public void Add(double price, double volume)
			{
				SumVolume += volume;
				SumPriceVolume += price * volume;
				SumPriceSquaredVolume += price * price * volume;
			}

			public void Reset()
			{
				SumVolume = 0;
				SumPriceVolume = 0;
				SumPriceSquaredVolume = 0;
			}

			public double Vwap
			{
				get { return SumVolume > 0 ? SumPriceVolume / SumVolume : 0; }
			}

			public double Variance
			{
				get { return SumVolume > 0 ? Math.Max(0, SumPriceSquaredVolume / SumVolume - Vwap * Vwap) : 0; }
			}

			public double StandardDeviation
			{
				get { return Math.Sqrt(Variance); }
			}
		}

		private sealed class WindowRecord
		{
			public long Sequence;
			public DateTime StartTime;
			public DateTime EndTime;
			public int StartBarIndex;
			public int EndBarIndex;
			public bool HasVolume;
			public string TagBase;
		}

		private sealed class BarSample
		{
			public long WindowSequence;
			public double Vwap;
			public double StandardDeviation;
			public bool IsValid;
		}

		private readonly string instanceTagPrefix = "OPVWAP-" + Guid.NewGuid().ToString("N") + "-";
		private VwapAccumulator accumulator;
		private WindowRecord activeWindow;
		private List<WindowRecord> completedWindows;
		private Dictionary<int, BarSample> samplesByPrimaryBar;
		private HashSet<int> dirtyPrimaryBars;
		private List<int> publishIndexes;
		private List<WindowRecord> completedRegionRedrawQueue;
		private List<WindowRecord> regionRemovalQueue;
		private Stroke vwapLineStyle;
		private Stroke deviation1UpperLineStyle;
		private Stroke deviation1LowerLineStyle;
		private Stroke deviation2UpperLineStyle;
		private Stroke deviation2LowerLineStyle;
		private Stroke deviation3UpperLineStyle;
		private Stroke deviation3LowerLineStyle;
		private long nextWindowSequence;
		private int lastActiveRegionEndBar;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca Periodic VWAP";
				Description = "Independent periodic or session VWAP windows with population-deviation bands and bounded historical fills.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				BarsRequiredToPlot = 0;
				ArePlotsConfigurable = false;

				WindowBasis = OrcaPeriodicVwapWindowBasis.Periodic;
				PeriodicInterval = OrcaPeriodicVwapInterval.Minutes30;
				SessionConfiguration = OrcaPeriodicVwapSessionConfiguration.OvernightAndRth;

				ShowVwap = true;
				ShowDeviationBands = true;
				ShowDeviation1 = true;
				Deviation1Multiplier = 1.0;
				ShowDeviation2 = true;
				Deviation2Multiplier = 2.0;
				ShowDeviation3 = true;
				Deviation3Multiplier = 3.0;

				VwapToDeviation1FillBrush = Brushes.DodgerBlue;
				VwapToDeviation1FillOpacity = 0;
				Deviation1To2FillBrush = Brushes.DodgerBlue;
				Deviation1To2FillOpacity = 0;
				Deviation2To3FillBrush = Brushes.DodgerBlue;
				Deviation2To3FillOpacity = 0;

				ShowHistoricalWindows = true;
				MaxHistoricalWindows = 100;

				vwapLineStyle = new Stroke(Brushes.DodgerBlue, DashStyleHelper.Solid, 2);
				deviation1UpperLineStyle = new Stroke(Brushes.DodgerBlue, DashStyleHelper.Dash, 1);
				deviation1LowerLineStyle = new Stroke(Brushes.DodgerBlue, DashStyleHelper.Dash, 1);
				deviation2UpperLineStyle = new Stroke(Brushes.DodgerBlue, DashStyleHelper.Dot, 1);
				deviation2LowerLineStyle = new Stroke(Brushes.DodgerBlue, DashStyleHelper.Dot, 1);
				deviation3UpperLineStyle = new Stroke(Brushes.DodgerBlue, DashStyleHelper.DashDot, 1);
				deviation3LowerLineStyle = new Stroke(Brushes.DodgerBlue, DashStyleHelper.DashDot, 1);

				AddPlot(new Stroke(vwapLineStyle), PlotStyle.Line, "VWAP");
				AddPlot(new Stroke(deviation1UpperLineStyle), PlotStyle.Line, "Deviation 1 Upper");
				AddPlot(new Stroke(deviation1LowerLineStyle), PlotStyle.Line, "Deviation 1 Lower");
				AddPlot(new Stroke(deviation2UpperLineStyle), PlotStyle.Line, "Deviation 2 Upper");
				AddPlot(new Stroke(deviation2LowerLineStyle), PlotStyle.Line, "Deviation 2 Lower");
				AddPlot(new Stroke(deviation3UpperLineStyle), PlotStyle.Line, "Deviation 3 Upper");
				AddPlot(new Stroke(deviation3LowerLineStyle), PlotStyle.Line, "Deviation 3 Lower");
			}
			else if (State == State.Configure)
			{
				ApplyConfiguredPlotStyles();
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				accumulator = new VwapAccumulator();
				completedWindows = new List<WindowRecord>(128);
				samplesByPrimaryBar = new Dictionary<int, BarSample>(4096);
				dirtyPrimaryBars = new HashSet<int>();
				publishIndexes = new List<int>(128);
				completedRegionRedrawQueue = new List<WindowRecord>(8);
				regionRemovalQueue = new List<WindowRecord>(8);
				nextWindowSequence = 1;
				lastActiveRegionEndBar = -1;
			}
			else if (State == State.Terminated)
			{
				RemoveAllWindowRegions();
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				ProcessTrade(Times[1][0], Closes[1][0], Volumes[1][0]);
				return;
			}

			if (BarsInProgress != 0 || CurrentBar < 0)
				return;

			PublishDirtySamples(CurrentBar);
			ProcessPendingRegionChanges(CurrentBar);
			DrawActiveWindowRegions(CurrentBar);
		}

		private void ProcessTrade(DateTime tradeTime, double price, double volume)
		{
			if (tradeTime == DateTime.MinValue
				|| double.IsNaN(price)
				|| double.IsInfinity(price)
				|| double.IsNaN(volume)
				|| double.IsInfinity(volume)
				|| volume <= 0)
				return;

			int primaryBarIndex = BarsArray[0].GetBar(tradeTime);
			if (primaryBarIndex < 0)
				return;

			DateTime windowStart;
			DateTime windowEnd;
			if (!TryResolveWindow(tradeTime, out windowStart, out windowEnd))
			{
				if (activeWindow != null && tradeTime >= activeWindow.EndTime)
				{
					FinalizeActiveWindow(primaryBarIndex);
					accumulator.Reset();
				}

				SetInvalidSample(primaryBarIndex);
				return;
			}

			if (activeWindow == null || activeWindow.StartTime != windowStart || activeWindow.EndTime != windowEnd)
			{
				if (activeWindow != null)
					FinalizeActiveWindow(primaryBarIndex);

				StartWindow(windowStart, windowEnd, primaryBarIndex);
			}

			accumulator.Add(price, volume);
			activeWindow.HasVolume = true;
			if (primaryBarIndex < activeWindow.StartBarIndex)
				activeWindow.StartBarIndex = primaryBarIndex;
			if (primaryBarIndex > activeWindow.EndBarIndex)
				activeWindow.EndBarIndex = primaryBarIndex;

			SetValidSample(primaryBarIndex, activeWindow.Sequence, accumulator.Vwap, accumulator.StandardDeviation);
		}

		private void StartWindow(DateTime startTime, DateTime endTime, int primaryBarIndex)
		{
			accumulator.Reset();
			activeWindow = new WindowRecord
			{
				Sequence = nextWindowSequence++,
				StartTime = startTime,
				EndTime = endTime,
				StartBarIndex = primaryBarIndex,
				EndBarIndex = primaryBarIndex,
				TagBase = instanceTagPrefix + startTime.Ticks.ToString(CultureInfo.InvariantCulture)
			};
			lastActiveRegionEndBar = -1;
		}

		private void FinalizeActiveWindow(int incomingPrimaryBarIndex)
		{
			WindowRecord completed = activeWindow;
			activeWindow = null;
			lastActiveRegionEndBar = -1;
			if (completed == null)
				return;

			InsertBoundaryGap(completed, incomingPrimaryBarIndex);
			if (!completed.HasVolume || completed.EndBarIndex < completed.StartBarIndex)
			{
				QueueRegionRemoval(completed);
				return;
			}

			if (ShowHistoricalWindows)
			{
				completedRegionRedrawQueue.Add(completed);
				completedWindows.Add(completed);
				PruneCompletedWindowsAtBoundary();
			}
			else
			{
				ClearWindowSamples(completed);
				QueueRegionRemoval(completed);
				ClearAllCompletedWindowsAtBoundary();
			}
		}

		private void InsertBoundaryGap(WindowRecord completed, int incomingPrimaryBarIndex)
		{
			if (completed == null || samplesByPrimaryBar == null)
				return;

			int gapBarIndex = completed.EndBarIndex;
			if (incomingPrimaryBarIndex <= completed.EndBarIndex)
				gapBarIndex--;
			BarSample ownedSample;
			while (gapBarIndex >= completed.StartBarIndex)
			{
				if (samplesByPrimaryBar.TryGetValue(gapBarIndex, out ownedSample)
					&& ownedSample != null
					&& ownedSample.WindowSequence == completed.Sequence)
				{
					SetInvalidSample(gapBarIndex);
					completed.EndBarIndex = gapBarIndex - 1;
					return;
				}

				gapBarIndex--;
			}
		}

		private void PruneCompletedWindowsAtBoundary()
		{
			int keepCount = Math.Max(1, MaxHistoricalWindows);
			while (completedWindows.Count > keepCount)
			{
				WindowRecord expired = completedWindows[0];
				completedWindows.RemoveAt(0);
				ClearWindowSamples(expired);
				QueueRegionRemoval(expired);
			}
		}

		private void ClearAllCompletedWindowsAtBoundary()
		{
			for (int index = 0; index < completedWindows.Count; index++)
			{
				ClearWindowSamples(completedWindows[index]);
				QueueRegionRemoval(completedWindows[index]);
			}
			completedWindows.Clear();
		}

		private void ClearWindowSamples(WindowRecord window)
		{
			if (window == null || samplesByPrimaryBar == null)
				return;

			for (int barIndex = Math.Max(0, window.StartBarIndex); barIndex <= window.EndBarIndex; barIndex++)
			{
				BarSample sample;
				if (!samplesByPrimaryBar.TryGetValue(barIndex, out sample)
					|| sample == null
					|| sample.WindowSequence != window.Sequence)
					continue;

				samplesByPrimaryBar.Remove(barIndex);
				dirtyPrimaryBars.Add(barIndex);
			}
		}

		private void SetValidSample(int primaryBarIndex, long windowSequence, double vwap, double standardDeviation)
		{
			BarSample sample;
			if (!samplesByPrimaryBar.TryGetValue(primaryBarIndex, out sample) || sample == null)
			{
				sample = new BarSample();
				samplesByPrimaryBar[primaryBarIndex] = sample;
			}

			sample.WindowSequence = windowSequence;
			sample.Vwap = vwap;
			sample.StandardDeviation = standardDeviation;
			sample.IsValid = true;
			dirtyPrimaryBars.Add(primaryBarIndex);
		}

		private void SetInvalidSample(int primaryBarIndex)
		{
			if (primaryBarIndex < 0)
				return;

			BarSample sample;
			if (!samplesByPrimaryBar.TryGetValue(primaryBarIndex, out sample) || sample == null)
			{
				sample = new BarSample();
				samplesByPrimaryBar[primaryBarIndex] = sample;
			}

			sample.WindowSequence = 0;
			sample.IsValid = false;
			dirtyPrimaryBars.Add(primaryBarIndex);
		}

		private void PublishDirtySamples(int currentPrimaryBar)
		{
			if (dirtyPrimaryBars.Count == 0)
				return;

			publishIndexes.Clear();
			foreach (int primaryBarIndex in dirtyPrimaryBars)
				if (primaryBarIndex >= 0 && primaryBarIndex <= currentPrimaryBar)
					publishIndexes.Add(primaryBarIndex);

			for (int index = 0; index < publishIndexes.Count; index++)
			{
				int primaryBarIndex = publishIndexes[index];
				int barsAgo = currentPrimaryBar - primaryBarIndex;
				BarSample sample;
				if (!samplesByPrimaryBar.TryGetValue(primaryBarIndex, out sample) || sample == null || !sample.IsValid)
					ResetPlots(barsAgo);
				else
					WritePlots(sample, barsAgo);

				dirtyPrimaryBars.Remove(primaryBarIndex);
			}
		}

		private void WritePlots(BarSample sample, int barsAgo)
		{
			if (ShowVwap)
				Values[VwapPlot][barsAgo] = sample.Vwap;
			else
				Values[VwapPlot].Reset(barsAgo);

			if (!ShowDeviationBands)
			{
				ResetDeviationPlots(barsAgo);
				return;
			}

			WriteDeviationPair(Dev1UpperPlot, Dev1LowerPlot, ShowDeviation1, sample, Deviation1Multiplier, barsAgo);
			WriteDeviationPair(Dev2UpperPlot, Dev2LowerPlot, ShowDeviation2, sample, Deviation2Multiplier, barsAgo);
			WriteDeviationPair(Dev3UpperPlot, Dev3LowerPlot, ShowDeviation3, sample, Deviation3Multiplier, barsAgo);
		}

		private void WriteDeviationPair(int upperPlot, int lowerPlot, bool show, BarSample sample, double multiplier, int barsAgo)
		{
			if (!show)
			{
				Values[upperPlot].Reset(barsAgo);
				Values[lowerPlot].Reset(barsAgo);
				return;
			}

			Values[upperPlot][barsAgo] = sample.Vwap + sample.StandardDeviation * multiplier;
			Values[lowerPlot][barsAgo] = sample.Vwap - sample.StandardDeviation * multiplier;
		}

		private void ResetPlots(int barsAgo)
		{
			for (int plotIndex = 0; plotIndex < Values.Length; plotIndex++)
				Values[plotIndex].Reset(barsAgo);
		}

		private void ResetDeviationPlots(int barsAgo)
		{
			for (int plotIndex = Dev1UpperPlot; plotIndex <= Dev3LowerPlot; plotIndex++)
				Values[plotIndex].Reset(barsAgo);
		}

		private void ApplyConfiguredPlotStyles()
		{
			ApplyPlotStyle(VwapPlot, vwapLineStyle);
			ApplyPlotStyle(Dev1UpperPlot, deviation1UpperLineStyle);
			ApplyPlotStyle(Dev1LowerPlot, deviation1LowerLineStyle);
			ApplyPlotStyle(Dev2UpperPlot, deviation2UpperLineStyle);
			ApplyPlotStyle(Dev2LowerPlot, deviation2LowerLineStyle);
			ApplyPlotStyle(Dev3UpperPlot, deviation3UpperLineStyle);
			ApplyPlotStyle(Dev3LowerPlot, deviation3LowerLineStyle);
		}

		private void ApplyPlotStyle(int plotIndex, Stroke style)
		{
			if (style == null || Plots == null || plotIndex < 0 || plotIndex >= Plots.Length)
				return;

			Plots[plotIndex].Brush = style.Brush;
			Plots[plotIndex].DashStyleHelper = style.DashStyleHelper;
			Plots[plotIndex].Width = style.Width;
			Plots[plotIndex].Opacity = style.Opacity;
		}

		private bool TryResolveWindow(DateTime time, out DateTime startTime, out DateTime endTime)
		{
			if (WindowBasis == OrcaPeriodicVwapWindowBasis.Session)
				return TryResolveSessionWindow(time, out startTime, out endTime);

			int intervalMinutes = (int)PeriodicInterval;
			if (intervalMinutes == 240 || intervalMinutes == 1440)
			{
				DateTime tradingDayStart = GetTradingDayStart(time);
				int elapsedMinutes = (int)(time - tradingDayStart).TotalMinutes;
				startTime = tradingDayStart.AddMinutes((elapsedMinutes / intervalMinutes) * intervalMinutes);
			}
			else
			{
				int totalMinutes = time.Hour * 60 + time.Minute;
				int alignedMinutes = (totalMinutes / intervalMinutes) * intervalMinutes;
				startTime = time.Date.AddMinutes(alignedMinutes);
			}

			endTime = startTime.AddMinutes(intervalMinutes);
			return true;
		}

		private bool TryResolveSessionWindow(DateTime time, out DateTime startTime, out DateTime endTime)
		{
			DateTime tradingDayStart = GetTradingDayStart(time);
			DateTime threeAm = tradingDayStart.AddHours(9);
			DateTime rthStart = tradingDayStart.AddHours(15.5);
			DateTime rthEnd = tradingDayStart.AddHours(23);

			if (SessionConfiguration == OrcaPeriodicVwapSessionConfiguration.AsiaLondonRth)
			{
				if (time >= tradingDayStart && time < threeAm)
				{
					startTime = tradingDayStart;
					endTime = threeAm;
					return true;
				}

				if (time >= threeAm && time < rthStart)
				{
					startTime = threeAm;
					endTime = rthStart;
					return true;
				}
			}
			else if (time >= tradingDayStart && time < rthStart)
			{
				startTime = tradingDayStart;
				endTime = rthStart;
				return true;
			}

			if (time >= rthStart && time < rthEnd)
			{
				startTime = rthStart;
				endTime = rthEnd;
				return true;
			}

			startTime = DateTime.MinValue;
			endTime = DateTime.MinValue;
			return false;
		}

		private static DateTime GetTradingDayStart(DateTime time)
		{
			DateTime tradingDayStart = time.Date.AddHours(18);
			return time < tradingDayStart ? tradingDayStart.AddDays(-1) : tradingDayStart;
		}

		private void ProcessPendingRegionChanges(int currentPrimaryBar)
		{
			for (int index = 0; index < regionRemovalQueue.Count; index++)
				RemoveWindowRegions(regionRemovalQueue[index]);
			regionRemovalQueue.Clear();

			for (int index = 0; index < completedRegionRedrawQueue.Count; index++)
				DrawWindowRegions(completedRegionRedrawQueue[index], currentPrimaryBar);
			completedRegionRedrawQueue.Clear();
		}

		private void DrawActiveWindowRegions(int currentPrimaryBar)
		{
			if (activeWindow == null || !activeWindow.HasVolume || activeWindow.EndBarIndex == lastActiveRegionEndBar)
				return;

			DrawWindowRegions(activeWindow, currentPrimaryBar);
			lastActiveRegionEndBar = activeWindow.EndBarIndex;
		}

		private void DrawWindowRegions(WindowRecord window, int currentPrimaryBar)
		{
			if (window == null || !window.HasVolume || window.EndBarIndex < window.StartBarIndex)
				return;

			int boundedEndBar = Math.Min(window.EndBarIndex, currentPrimaryBar);
			if (boundedEndBar < window.StartBarIndex)
				return;

			int startBarsAgo = currentPrimaryBar - window.StartBarIndex;
			int endBarsAgo = currentPrimaryBar - boundedEndBar;

			DrawOrRemoveRegion(window.TagBase + "-CoreU", startBarsAgo, endBarsAgo, VwapPlot, Dev1UpperPlot,
				ShowVwap && ShowDeviationBands && ShowDeviation1, VwapToDeviation1FillBrush, VwapToDeviation1FillOpacity);
			DrawOrRemoveRegion(window.TagBase + "-CoreL", startBarsAgo, endBarsAgo, VwapPlot, Dev1LowerPlot,
				ShowVwap && ShowDeviationBands && ShowDeviation1, VwapToDeviation1FillBrush, VwapToDeviation1FillOpacity);
			DrawOrRemoveRegion(window.TagBase + "-12U", startBarsAgo, endBarsAgo, Dev1UpperPlot, Dev2UpperPlot,
				ShowDeviationBands && ShowDeviation1 && ShowDeviation2, Deviation1To2FillBrush, Deviation1To2FillOpacity);
			DrawOrRemoveRegion(window.TagBase + "-12L", startBarsAgo, endBarsAgo, Dev1LowerPlot, Dev2LowerPlot,
				ShowDeviationBands && ShowDeviation1 && ShowDeviation2, Deviation1To2FillBrush, Deviation1To2FillOpacity);
			DrawOrRemoveRegion(window.TagBase + "-23U", startBarsAgo, endBarsAgo, Dev2UpperPlot, Dev3UpperPlot,
				ShowDeviationBands && ShowDeviation2 && ShowDeviation3, Deviation2To3FillBrush, Deviation2To3FillOpacity);
			DrawOrRemoveRegion(window.TagBase + "-23L", startBarsAgo, endBarsAgo, Dev2LowerPlot, Dev3LowerPlot,
				ShowDeviationBands && ShowDeviation2 && ShowDeviation3, Deviation2To3FillBrush, Deviation2To3FillOpacity);
		}

		private void DrawOrRemoveRegion(string tag, int startBarsAgo, int endBarsAgo, int firstPlot, int secondPlot,
			bool enabled, Brush brush, int opacity)
		{
			if (!enabled || opacity <= 0 || brush == null)
			{
				try { RemoveDrawObject(tag); }
				catch { }
				return;
			}

			Draw.Region(this, tag, startBarsAgo, endBarsAgo, Values[firstPlot], Values[secondPlot], null, brush, opacity);
		}

		private void QueueRegionRemoval(WindowRecord window)
		{
			if (window != null)
				regionRemovalQueue.Add(window);
		}

		private void RemoveWindowRegions(WindowRecord window)
		{
			if (window == null || string.IsNullOrEmpty(window.TagBase))
				return;

			string[] suffixes = { "-CoreU", "-CoreL", "-12U", "-12L", "-23U", "-23L" };
			for (int index = 0; index < suffixes.Length; index++)
			{
				try { RemoveDrawObject(window.TagBase + suffixes[index]); }
				catch { }
			}
		}

		private void RemoveAllWindowRegions()
		{
			if (activeWindow != null)
				RemoveWindowRegions(activeWindow);
			if (completedWindows != null)
				for (int index = 0; index < completedWindows.Count; index++)
					RemoveWindowRegions(completedWindows[index]);
			if (completedRegionRedrawQueue != null)
				for (int index = 0; index < completedRegionRedrawQueue.Count; index++)
					RemoveWindowRegions(completedRegionRedrawQueue[index]);
		}

		#region Properties
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Window Basis", Description = "Builds independently resetting VWAPs by aligned period or fixed market session.", GroupName = "1. Window Configuration", Order = 0)]
		public OrcaPeriodicVwapWindowBasis WindowBasis { get; set; }

		[NinjaScriptProperty]
		[TypeConverter(typeof(OrcaPeriodicVwapIntervalConverter))]
		[Display(Name = "Periodic Interval", Description = "Clock-aligned period. Four-hour and Daily windows are anchored to 6:00 PM.", GroupName = "1. Window Configuration", Order = 1)]
		public OrcaPeriodicVwapInterval PeriodicInterval { get; set; }

		[NinjaScriptProperty]
		[TypeConverter(typeof(OrcaPeriodicVwapSessionConfigurationConverter))]
		[Display(Name = "Session Configuration", Description = "Fixed Eastern-aligned session windows. The 5:00 PM-6:00 PM maintenance period is excluded.", GroupName = "1. Window Configuration", Order = 2)]
		public OrcaPeriodicVwapSessionConfiguration SessionConfiguration { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Show VWAP", GroupName = "2. VWAP and Bands", Order = 0)]
		public bool ShowVwap { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Show Deviation Bands", GroupName = "2. VWAP and Bands", Order = 1)]
		public bool ShowDeviationBands { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Show Deviation 1", GroupName = "2. VWAP and Bands", Order = 2)]
		public bool ShowDeviation1 { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, 100.0)]
		[Display(Name = "Deviation 1 Multiplier", GroupName = "2. VWAP and Bands", Order = 3)]
		public double Deviation1Multiplier { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Show Deviation 2", GroupName = "2. VWAP and Bands", Order = 4)]
		public bool ShowDeviation2 { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, 100.0)]
		[Display(Name = "Deviation 2 Multiplier", GroupName = "2. VWAP and Bands", Order = 5)]
		public double Deviation2Multiplier { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Show Deviation 3", GroupName = "2. VWAP and Bands", Order = 6)]
		public bool ShowDeviation3 { get; set; }

		[NinjaScriptProperty]
		[Range(0.01, 100.0)]
		[Display(Name = "Deviation 3 Multiplier", GroupName = "2. VWAP and Bands", Order = 7)]
		public double Deviation3Multiplier { get; set; }

		[Display(Name = "VWAP Line", Description = "Color, dash style, and width of the VWAP line.", GroupName = "3. Plot Styling", Order = 0)]
		public Stroke VwapLineStyle
		{
			get { return vwapLineStyle; }
			set { vwapLineStyle = value; ApplyPlotStyle(VwapPlot, value); }
		}

		[Display(Name = "Deviation 1 Upper", Description = "Color, dash style, and width of the upper first-deviation line.", GroupName = "3. Plot Styling", Order = 1)]
		public Stroke Deviation1UpperLineStyle
		{
			get { return deviation1UpperLineStyle; }
			set { deviation1UpperLineStyle = value; ApplyPlotStyle(Dev1UpperPlot, value); }
		}

		[Display(Name = "Deviation 1 Lower", Description = "Color, dash style, and width of the lower first-deviation line.", GroupName = "3. Plot Styling", Order = 2)]
		public Stroke Deviation1LowerLineStyle
		{
			get { return deviation1LowerLineStyle; }
			set { deviation1LowerLineStyle = value; ApplyPlotStyle(Dev1LowerPlot, value); }
		}

		[Display(Name = "Deviation 2 Upper", Description = "Color, dash style, and width of the upper second-deviation line.", GroupName = "3. Plot Styling", Order = 3)]
		public Stroke Deviation2UpperLineStyle
		{
			get { return deviation2UpperLineStyle; }
			set { deviation2UpperLineStyle = value; ApplyPlotStyle(Dev2UpperPlot, value); }
		}

		[Display(Name = "Deviation 2 Lower", Description = "Color, dash style, and width of the lower second-deviation line.", GroupName = "3. Plot Styling", Order = 4)]
		public Stroke Deviation2LowerLineStyle
		{
			get { return deviation2LowerLineStyle; }
			set { deviation2LowerLineStyle = value; ApplyPlotStyle(Dev2LowerPlot, value); }
		}

		[Display(Name = "Deviation 3 Upper", Description = "Color, dash style, and width of the upper third-deviation line.", GroupName = "3. Plot Styling", Order = 5)]
		public Stroke Deviation3UpperLineStyle
		{
			get { return deviation3UpperLineStyle; }
			set { deviation3UpperLineStyle = value; ApplyPlotStyle(Dev3UpperPlot, value); }
		}

		[Display(Name = "Deviation 3 Lower", Description = "Color, dash style, and width of the lower third-deviation line.", GroupName = "3. Plot Styling", Order = 6)]
		public Stroke Deviation3LowerLineStyle
		{
			get { return deviation3LowerLineStyle; }
			set { deviation3LowerLineStyle = value; ApplyPlotStyle(Dev3LowerPlot, value); }
		}

		[XmlIgnore]
		[Display(Name = "VWAP to Deviation 1 Fill", GroupName = "4. Region Fills", Order = 0)]
		public Brush VwapToDeviation1FillBrush { get; set; }

		[Browsable(false)]
		public string VwapToDeviation1FillBrushSerializable
		{
			get { return Serialize.BrushToString(VwapToDeviation1FillBrush); }
			set { VwapToDeviation1FillBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "VWAP to Deviation 1 Opacity", Description = "Zero disables this fill.", GroupName = "4. Region Fills", Order = 1)]
		public int VwapToDeviation1FillOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Deviation 1 to 2 Fill", GroupName = "4. Region Fills", Order = 2)]
		public Brush Deviation1To2FillBrush { get; set; }

		[Browsable(false)]
		public string Deviation1To2FillBrushSerializable
		{
			get { return Serialize.BrushToString(Deviation1To2FillBrush); }
			set { Deviation1To2FillBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Deviation 1 to 2 Opacity", Description = "Zero disables this fill.", GroupName = "4. Region Fills", Order = 3)]
		public int Deviation1To2FillOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Deviation 2 to 3 Fill", GroupName = "4. Region Fills", Order = 4)]
		public Brush Deviation2To3FillBrush { get; set; }

		[Browsable(false)]
		public string Deviation2To3FillBrushSerializable
		{
			get { return Serialize.BrushToString(Deviation2To3FillBrush); }
			set { Deviation2To3FillBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Deviation 2 to 3 Opacity", Description = "Zero disables this fill.", GroupName = "4. Region Fills", Order = 5)]
		public int Deviation2To3FillOpacity { get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Show Historical Windows", GroupName = "5. Display", Order = 0)]
		public bool ShowHistoricalWindows { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "Max Historical Windows", Description = "Completed windows retained and pruned only when a new boundary is processed.", GroupName = "5. Display", Order = 1)]
		public int MaxHistoricalWindows { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Vwap { get { return Values[VwapPlot]; } }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Deviation1Upper { get { return Values[Dev1UpperPlot]; } }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Deviation1Lower { get { return Values[Dev1LowerPlot]; } }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Deviation2Upper { get { return Values[Dev2UpperPlot]; } }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Deviation2Lower { get { return Values[Dev2LowerPlot]; } }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Deviation3Upper { get { return Values[Dev3UpperPlot]; } }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Deviation3Lower { get { return Values[Dev3LowerPlot]; } }
		#endregion
	}

	public sealed class OrcaPeriodicVwapTypeConverter : IndicatorBaseConverter
	{
		public override bool GetPropertiesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			PropertyDescriptorCollection properties = base.GetPropertiesSupported(context)
				? base.GetProperties(context, value, attributes)
				: TypeDescriptor.GetProperties(value, attributes);

			OrcaPeriodicVWAP indicator = value as OrcaPeriodicVWAP;
			if (indicator == null || properties == null)
				return properties;

			HashSet<string> hidden = new HashSet<string>();
			hidden.Add("BarsPeriod");
			hidden.Add("InputPlot");
			hidden.Add("Plots");
			hidden.Add("SelectedValueSeries");

			if (indicator.WindowBasis == OrcaPeriodicVwapWindowBasis.Periodic)
				hidden.Add(nameof(indicator.SessionConfiguration));
			else
				hidden.Add(nameof(indicator.PeriodicInterval));

			if (!indicator.ShowVwap)
				hidden.Add(nameof(indicator.VwapLineStyle));

			if (!indicator.ShowDeviationBands)
			{
				hidden.Add(nameof(indicator.ShowDeviation1));
				hidden.Add(nameof(indicator.Deviation1Multiplier));
				hidden.Add(nameof(indicator.ShowDeviation2));
				hidden.Add(nameof(indicator.Deviation2Multiplier));
				hidden.Add(nameof(indicator.ShowDeviation3));
				hidden.Add(nameof(indicator.Deviation3Multiplier));
				hidden.Add(nameof(indicator.Deviation1UpperLineStyle));
				hidden.Add(nameof(indicator.Deviation1LowerLineStyle));
				hidden.Add(nameof(indicator.Deviation2UpperLineStyle));
				hidden.Add(nameof(indicator.Deviation2LowerLineStyle));
				hidden.Add(nameof(indicator.Deviation3UpperLineStyle));
				hidden.Add(nameof(indicator.Deviation3LowerLineStyle));
				HideCoreFill(indicator, hidden);
				HideDeviation12Fill(indicator, hidden);
				HideDeviation23Fill(indicator, hidden);
			}
			else
			{
				if (!indicator.ShowDeviation1)
				{
					hidden.Add(nameof(indicator.Deviation1Multiplier));
					hidden.Add(nameof(indicator.Deviation1UpperLineStyle));
					hidden.Add(nameof(indicator.Deviation1LowerLineStyle));
				}
				if (!indicator.ShowDeviation2)
				{
					hidden.Add(nameof(indicator.Deviation2Multiplier));
					hidden.Add(nameof(indicator.Deviation2UpperLineStyle));
					hidden.Add(nameof(indicator.Deviation2LowerLineStyle));
				}
				if (!indicator.ShowDeviation3)
				{
					hidden.Add(nameof(indicator.Deviation3Multiplier));
					hidden.Add(nameof(indicator.Deviation3UpperLineStyle));
					hidden.Add(nameof(indicator.Deviation3LowerLineStyle));
				}

				if (!indicator.ShowVwap || !indicator.ShowDeviation1)
					HideCoreFill(indicator, hidden);
				if (!indicator.ShowDeviation1 || !indicator.ShowDeviation2)
					HideDeviation12Fill(indicator, hidden);
				if (!indicator.ShowDeviation2 || !indicator.ShowDeviation3)
					HideDeviation23Fill(indicator, hidden);
			}

			if (!indicator.ShowHistoricalWindows)
				hidden.Add(nameof(indicator.MaxHistoricalWindows));

			PropertyDescriptorCollection adjusted = new PropertyDescriptorCollection(null);
			foreach (PropertyDescriptor descriptor in properties)
			{
				adjusted.Add(hidden.Contains(descriptor.Name)
					? new PropertyDescriptorExtended(descriptor, _ => value, null, new Attribute[] { new BrowsableAttribute(false) })
					: descriptor);
			}

			return adjusted;
		}

		private static void HideCoreFill(OrcaPeriodicVWAP indicator, HashSet<string> hidden)
		{
			hidden.Add(nameof(indicator.VwapToDeviation1FillBrush));
			hidden.Add(nameof(indicator.VwapToDeviation1FillOpacity));
		}

		private static void HideDeviation12Fill(OrcaPeriodicVWAP indicator, HashSet<string> hidden)
		{
			hidden.Add(nameof(indicator.Deviation1To2FillBrush));
			hidden.Add(nameof(indicator.Deviation1To2FillOpacity));
		}

		private static void HideDeviation23Fill(OrcaPeriodicVWAP indicator, HashSet<string> hidden)
		{
			hidden.Add(nameof(indicator.Deviation2To3FillBrush));
			hidden.Add(nameof(indicator.Deviation2To3FillOpacity));
		}
	}
}

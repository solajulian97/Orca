#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Serialization;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.Core.FloatingPoint;

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

using WpfBrush  = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColors  = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public enum StepIntervalType
	{
		Minutes15 = 15,
		Minutes30 = 30,
		Hour1     = 60,
		Hours4    = 240,
		Daily     = 1440
	}

	public enum StepVALineStyleEnum
	{
		Solid   = 0,
		Dash    = 1,
		Dot     = 2,
		DashDot = 3
	}

	public class OrcaStepProfile : Indicator
	{
		#region Inner Types
		private class StepBlock
		{
			public object   SyncObj = new object();
			public DateTime StartTime;
			public DateTime EndTime;
			public int      StartBarIndex;
			public int      EndBarIndex = -1;      // -1 = active/in-progress
			public double   HighPrice;
			public double   LowPrice;
			public Dictionary<double, long> VolByPrice   = new Dictionary<double, long>();
			public Dictionary<double, long> DeltaByPrice = new Dictionary<double, long>();
			public bool IsVACalculated = false;
			public double POCPrice = double.NaN;
			public double VAHPrice = double.NaN;
			public double VALPrice = double.NaN;
			public long MaxVol = 0;
		}
		#endregion

		#region Fields
		private List<StepBlock> stepBlocks;
		private DateTime        previousBarTime = DateTime.MinValue;

		// Bid/Ask cache for delta classification
		private double lastBid  = double.NaN;
		private double lastAsk  = double.NaN;
		private double prevLast = double.NaN;

		// SharpDX rendering resources
		private SolidColorBrush volBrushDx;
		private SolidColorBrush histVolBrushDx;
		private SolidColorBrush pocBrushDx;
		private SolidColorBrush posDeltaBrushDx;
		private SolidColorBrush negDeltaBrushDx;
		private SolidColorBrush histPosDeltaBrushDx;
		private SolidColorBrush histNegDeltaBrushDx;
		private SolidColorBrush blockSepBrushDx;

		// Volume gradient palette (outside VA)
		private SolidColorBrush[] volGradientBrushes;
		private SolidColorBrush[] histVolGradientBrushes;
		private int lastBuiltGradientSteps = -1;

		// Value Area gradient palette (inside VA)
		private SolidColorBrush   vaVolBrushDx;
		private SolidColorBrush   histVaVolBrushDx;
		private SolidColorBrush[] vaGradientBrushes;
		private SolidColorBrush[] histVaGradientBrushes;
		private int lastBuiltVAGradientSteps = -1;

		// VA line resources
		private SolidColorBrush vaLineBrushDx;
		private StrokeStyle     vaLineStrokeDx;

		// Text resources
		private SolidColorBrush deltaTextBrushDx;
		private TextFormat      deltaTextFormatDx;
		private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();
		private int lastDynamicDeltaComp = -1;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name        = "OrcaStepProfile";
				Description = "Time-based volume profiles at fixed intervals with dual volume/delta histograms, gradient, POC, and Value Area.";
				Calculate   = Calculate.OnPriceChange;
				IsOverlay   = true;

				// Data
				StepInterval           = StepIntervalType.Minutes30;
				VolumeTickCompression  = 4;
				DeltaTickCompression   = 10;
				UseDynamicAggregation  = false;
				DynamicAggregationMultiplier = 1.0;
				RTHOnly                = false;
				RTHStart               = DateTime.Parse("09:30:00", System.Globalization.CultureInfo.InvariantCulture);
				RTHEnd                 = DateTime.Parse("16:00:00", System.Globalization.CultureInfo.InvariantCulture);

				// Layout
				HistoricalProfileWidthPx = 100;
				ActiveProfileWidthPx     = 150;
				HistoricalDeltaWidthPx   = 60;
				ActiveDeltaWidthPx       = 60;
				RightOffsetPx            = 60;
				ProfileBarSpacingPx      = 0;
				MirrorProfiles           = false;

				// Visibility
				ShowActiveVolume    = true;
				ShowHistoricalVolume= true;
				ShowActiveDelta     = true;
				ShowHistoricalDelta = true;
				ShowPOC             = true;
				ShowBlockSeparators = true;

				// Gradient
				UseGradient   = true;
				GradientSteps = 16;
				MinBrightness = 0.20f;

				// Value Area
				ShowValueArea    = true;
				ShowVAColor      = true;
				ShowVALines      = true;
				ValueAreaPercent = 70;
				VALineThickness  = 1.5f;
				VALineStyle      = StepVALineStyleEnum.Dash;

				// Colors — profile
				VolumeBrush             = WpfBrushes.RoyalBlue;
				ActiveVolumeOpacity     = 0.85f;
				HistoricalVolumeOpacity = 0.50f;
				POCBrush                = WpfBrushes.DodgerBlue;

				// Colors — Value Area
				VABrush     = WpfBrushes.CornflowerBlue;
				VALineBrush = WpfBrushes.White;

				// Colors — delta
				PositiveDeltaBrush     = WpfBrushes.Lime;
				NegativeDeltaBrush     = WpfBrushes.Red;
				ActiveDeltaOpacity     = 0.85f;
				HistoricalDeltaOpacity = 0.50f;

				// Colors — separators
				BlockSeparatorBrush = WpfBrushes.DimGray;

				// Delta Text
				ShowDeltaText = true;
				DeltaTextMinThreshold = 10;
				DeltaTextBrush = WpfBrushes.White;
				DeltaTextFontSize = 11f;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				stepBlocks      = new List<StepBlock>(256);
				previousBarTime = DateTime.MinValue;
				textWidthCache.Clear();
			}
			else if (State == State.Terminated)
			{
				DisposeDx();
			}
		}

		#region Dispose
		private void DisposeDx()
		{
			try
			{
				volBrushDx?.Dispose();
				pocBrushDx?.Dispose();
				posDeltaBrushDx?.Dispose();
				negDeltaBrushDx?.Dispose();
				blockSepBrushDx?.Dispose();
				vaVolBrushDx?.Dispose();
				vaLineBrushDx?.Dispose();
				vaLineStrokeDx?.Dispose();

				if (volGradientBrushes != null)
					for (int i = 0; i < volGradientBrushes.Length; i++)
						volGradientBrushes[i]?.Dispose();

				if (vaGradientBrushes != null)
					for (int i = 0; i < vaGradientBrushes.Length; i++)
						vaGradientBrushes[i]?.Dispose();
			}
			catch { }
			finally
			{
				volBrushDx         = null;
				histVolBrushDx     = null;
				pocBrushDx         = null;
				posDeltaBrushDx    = null;
				negDeltaBrushDx    = null;
				histPosDeltaBrushDx = null;
				histNegDeltaBrushDx = null;
				blockSepBrushDx    = null;
				vaVolBrushDx       = null;
				histVaVolBrushDx   = null;
				vaLineBrushDx      = null;
				vaLineStrokeDx     = null;
				volGradientBrushes = null;
				histVolGradientBrushes = null;
				vaGradientBrushes  = null;
				histVaGradientBrushes = null;
				deltaTextBrushDx   = null;
				deltaTextFormatDx  = null;
				lastBuiltGradientSteps   = -1;
				lastBuiltVAGradientSteps = -1;
				textWidthCache.Clear();
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
		}
		#endregion

		#region Market Data / Block Boundary / Tick Processing
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Bid)
				lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask)
				lastAsk = e.Price;
		}

		protected override void OnBarUpdate()
		{
			// We only care about the tick series for building the profile and handling boundaries.
			// Primary series (BarsInProgress == 0) is only used for `CurrentBar` index matching for rendering.
			
			if (BarsInProgress == 1)
			{
				DateTime tickTime = Time[0];

				if (RTHOnly)
				{
					TimeSpan tod = tickTime.TimeOfDay;
					TimeSpan startTod = RTHStart.TimeOfDay;
					TimeSpan endTod = RTHEnd.TimeOfDay;

					bool inRTH = false;
					if (startTod < endTod)
						inRTH = (tod >= startTod && tod < endTod);
					else
						inRTH = (tod >= startTod || tod < endTod);

					if (!inRTH) return;
				}

				// Check for block boundary crossing
				if (stepBlocks.Count == 0)
				{
					// First block
					StartNewBlock(tickTime, BarsArray[0].CurrentBar);
				}
				else
				{
					StepBlock active = stepBlocks[stepBlocks.Count - 1];

					bool crossed = tickTime >= active.EndTime;

					if (crossed)
					{
						// Finalize the previous block.
						// The previous block ends exactly 1 index before the primary bar this boundary-crossing tick belongs to.
						
						// If the primary series hasn't formally closed its bar yet (its end time is <= active.EndTime),
						// the current primary bar belongs to the old profile block, and the new block will start on CurrentBar + 1.
						// If the primary series HAS advanced (end time > active.EndTime), it belongs to the new profile,
						// so the old block ended at CurrentBar - 1.
						int currentPrimaryEndIdx = BarsArray[0].CurrentBar;
						int newBlockStartIdx = currentPrimaryEndIdx;
						
						if (currentPrimaryEndIdx >= 0)
						{
							DateTime currentPrimaryVal = BarsArray[0].GetTime(currentPrimaryEndIdx);
							if (currentPrimaryVal <= active.EndTime)
							{
								newBlockStartIdx = currentPrimaryEndIdx + 1;
							}
						}

						// Close out the old block
						active.EndBarIndex = newBlockStartIdx - 1;
						if (active.EndBarIndex < active.StartBarIndex)
							active.EndBarIndex = active.StartBarIndex;

						// Determine new block start time
						DateTime newStart = GetAlignedBlockStart(tickTime, (int)StepInterval);

						StartNewBlock(newStart, newBlockStartIdx);
					}
				}

				// Keep active block's end bar index updated to the current primary bar
				if (stepBlocks.Count > 0)
				{
					StepBlock active = stepBlocks[stepBlocks.Count - 1];
					if (active.EndBarIndex < 0 || active.EndBarIndex < BarsArray[0].CurrentBar)
						active.EndBarIndex = BarsArray[0].CurrentBar;
				}

				previousBarTime = tickTime;

				// Process the tick into the active block
				ProcessTickIntoActiveBlock();

				// Removed ForceRefresh() to fix UI Thread lagging
			}
		}

		private DateTime GetAlignedBlockStart(DateTime time, int intervalMinutes)
		{
			if (intervalMinutes >= 1440)
			{
				// Daily: reset at 6 PM (18:00)
				// If the tick is before 18:00, it belongs to the session that started yesterday at 18:00
				// If the tick is >= 18:00, it belongs to the session starting today at 18:00
				if (time.Hour < 18)
					return time.Date.AddDays(-1).AddHours(18);
				else
					return time.Date.AddHours(18);
			}

			// Align to interval boundaries from midnight
			int totalMins = time.Hour * 60 + time.Minute;
			int blockStart = (totalMins / intervalMinutes) * intervalMinutes;
			return time.Date.AddMinutes(blockStart);
		}

		private DateTime GetAlignedBlockEnd(DateTime blockStart, int intervalMinutes)
		{
			if (intervalMinutes >= 1440)
			{
				// Daily: Ends at 18:00 the next day
				return blockStart.AddDays(1);
			}
			return blockStart.AddMinutes(intervalMinutes);
		}

		private void StartNewBlock(DateTime startTime, int startBarIndex)
		{
			int intervalMins = (int)StepInterval;
			DateTime alignedStart = GetAlignedBlockStart(startTime, intervalMins);

			var block = new StepBlock
			{
				StartTime     = alignedStart,
				EndTime       = GetAlignedBlockEnd(alignedStart, intervalMins),
				StartBarIndex = startBarIndex,
				EndBarIndex   = -1,
				HighPrice     = double.MinValue,
				LowPrice      = double.MaxValue
			};

			stepBlocks.Add(block);
		}

		private void ProcessTickIntoActiveBlock()
		{
			if (stepBlocks == null || stepBlocks.Count == 0) return;

			StepBlock active = stepBlocks[stepBlocks.Count - 1];

			double last = Close[0];
			long   vol  = (long)Volume[0];
			if (vol <= 0) return;

			// Update high/low
			if (last > active.HighPrice) active.HighPrice = last;
			if (last < active.LowPrice)  active.LowPrice  = last;

			// --- VOLUME ---
			double volComp        = VolumeTickCompression * TickSize;
			double volBucketPrice = Math.Floor(last / volComp + 0.000001) * volComp;

			lock (active.SyncObj)
			{
				if (active.VolByPrice.TryGetValue(volBucketPrice, out long vExisting))
					active.VolByPrice[volBucketPrice] = vExisting + vol;
				else
					active.VolByPrice[volBucketPrice] = vol;
			}

			// --- DELTA ---
			long signed = 0;
			if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
			{
				if (last >= lastAsk)       signed = +vol;
				else if (last <= lastBid)  signed = -vol;
				else if (!double.IsNaN(prevLast))
					signed = (last > prevLast) ? +vol : (last < prevLast ? -vol : 0);
			}
			else if (!double.IsNaN(prevLast))
			{
				signed = (last > prevLast) ? +vol : (last < prevLast ? -vol : 0);
			}
			prevLast = last;

			if (signed != 0)
			{
				// Delta uses volume compression for storage; grouped to DeltaTickCompression at render time
				double deltaComp        = VolumeTickCompression * TickSize;
				double deltaBucketPrice = Math.Floor(last / deltaComp + 0.000001) * deltaComp;

				lock (active.SyncObj)
				{
					if (active.DeltaByPrice.TryGetValue(deltaBucketPrice, out long dExisting))
						active.DeltaByPrice[deltaBucketPrice] = dExisting + signed;
					else
						active.DeltaByPrice[deltaBucketPrice] = signed;
				}
			}
		}
		#endregion

		#region Value Area Calculation
		private bool CalcValueArea(Dictionary<double, long> volMap, double pocPrice, out double vahPrice, out double valPrice)
		{
			vahPrice = pocPrice;
			valPrice = pocPrice;

			if (volMap.Count <= 1) return false;

			var sortedPrices = new List<double>(volMap.Keys);
			sortedPrices.Sort();

			long totalVol = 0;
			foreach (var kv in volMap) totalVol += kv.Value;
			if (totalVol <= 0) return false;

			double targetVol = totalVol * (ValueAreaPercent / 100.0);

			int pocIdx = sortedPrices.IndexOf(pocPrice);
			if (pocIdx < 0) return false;

			long accumulatedVol = volMap[pocPrice];
			int lo = pocIdx;
			int hi = pocIdx;

			while (accumulatedVol < targetVol && (lo > 0 || hi < sortedPrices.Count - 1))
			{
				long volBelow = (lo > 0) ? volMap[sortedPrices[lo - 1]] : 0;
				long volAbove = (hi < sortedPrices.Count - 1) ? volMap[sortedPrices[hi + 1]] : 0;

				if (lo <= 0)
				{
					hi++;
					accumulatedVol += volAbove;
				}
				else if (hi >= sortedPrices.Count - 1)
				{
					lo--;
					accumulatedVol += volBelow;
				}
				else if (volAbove >= volBelow)
				{
					hi++;
					accumulatedVol += volAbove;
				}
				else
				{
					lo--;
					accumulatedVol += volBelow;
				}
			}

			valPrice = sortedPrices[lo];
			vahPrice = sortedPrices[hi];
			return true;
		}
		#endregion

		#region Rendering
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);

			if (stepBlocks == null || stepBlocks.Count == 0 || ChartBars == null) return;

			EnsureDxResources();

			int dynamicDeltaComp = DeltaTickCompression;
			if (UseDynamicAggregation)
			{
				var panel = chartControl.ChartPanels[chartScale.PanelIndex];
				double visibleTicks = (chartScale.MaxValue - chartScale.MinValue) / TickSize;
				double ticksPerPixel = visibleTicks / Math.Max(1, panel.H);
				double desiredTicks = ticksPerPixel * (DeltaTextFontSize + 4) * DynamicAggregationMultiplier;

				if (desiredTicks <= 1) dynamicDeltaComp = 1;
				else if (desiredTicks <= 2) dynamicDeltaComp = 2;
				else if (desiredTicks <= 4) dynamicDeltaComp = 4;
				else if (desiredTicks <= 5) dynamicDeltaComp = 5;
				else if (desiredTicks <= 8) dynamicDeltaComp = 8;
				else if (desiredTicks <= 10) dynamicDeltaComp = 10;
				else if (desiredTicks <= 15) dynamicDeltaComp = 15;
				else if (desiredTicks <= 20) dynamicDeltaComp = 20;
				else if (desiredTicks <= 25) dynamicDeltaComp = 25;
				else if (desiredTicks <= 30) dynamicDeltaComp = 30;
				else if (desiredTicks <= 40) dynamicDeltaComp = 40;
				else if (desiredTicks <= 50) dynamicDeltaComp = 50;
				else if (desiredTicks <= 100) dynamicDeltaComp = (int)(Math.Round(desiredTicks / 20.0) * 20);
				else dynamicDeltaComp = (int)(Math.Round(desiredTicks / 50.0) * 50);

				if (lastDynamicDeltaComp > 0 && Math.Abs(dynamicDeltaComp - lastDynamicDeltaComp) < Math.Max(2, dynamicDeltaComp * 0.15))
				{
					dynamicDeltaComp = lastDynamicDeltaComp;
				}
				else
				{
					lastDynamicDeltaComp = dynamicDeltaComp;
				}
			}

			int fromIdx = ChartBars.FromIndex;
			int toIdx   = ChartBars.ToIndex;

			float panelTop    = ChartPanel.Y;
			float panelBottom = ChartPanel.Y + ChartPanel.H;

			// Render historical (completed) blocks
			for (int i = 0; i < stepBlocks.Count - 1; i++)
			{
				StepBlock block = stepBlocks[i];

				// Skip blocks entirely outside the visible bar range
				int blockEnd = block.EndBarIndex >= 0 ? block.EndBarIndex : (BarsArray[0].Count - 1);
				if (blockEnd < fromIdx || block.StartBarIndex > toIdx)
					continue;

				float spineX = chartControl.GetXByBarIndex(ChartBars, Math.Max(block.StartBarIndex, fromIdx));

				if (ShowHistoricalVolume && block.VolByPrice.Count > 0)
				{
					DrawBlockProfile(chartControl, chartScale, block, spineX, HistoricalProfileWidthPx,
						HistoricalDeltaWidthPx, panelTop, panelBottom, facingRight: true, isActiveProfile: false);
				}

				if (ShowHistoricalDelta && block.DeltaByPrice.Count > 0)
				{
					DrawBlockDelta(chartControl, chartScale, block, spineX, HistoricalProfileWidthPx,
						HistoricalDeltaWidthPx, panelTop, panelBottom, facingRight: true, isActiveProfile: false, dynamicDeltaComp);
				}

				// Block separator line
				if (ShowBlockSeparators && blockSepBrushDx != null)
				{
					RenderTarget.DrawLine(
						new Vector2(spineX, panelTop),
						new Vector2(spineX, panelBottom),
						blockSepBrushDx, 1f);
				}
			}

			// Render active (last) block
			if (stepBlocks.Count > 0)
			{
				StepBlock active = stepBlocks[stepBlocks.Count - 1];

				if (ShowActiveVolume && active.VolByPrice.Count > 0)
				{
					// Spine at right chart edge
					float activeSpineX = chartControl.CanvasRight - RightOffsetPx;

					DrawBlockProfile(chartControl, chartScale, active, activeSpineX, ActiveProfileWidthPx,
						ActiveDeltaWidthPx, panelTop, panelBottom, facingRight: false, isActiveProfile: true);
				}

				if (ShowActiveDelta && active.DeltaByPrice.Count > 0)
				{
					float activeSpineX = chartControl.CanvasRight - RightOffsetPx;
					DrawBlockDelta(chartControl, chartScale, active, activeSpineX, ActiveProfileWidthPx,
						ActiveDeltaWidthPx, panelTop, panelBottom, facingRight: false, isActiveProfile: true, dynamicDeltaComp);
				}

				// Draw separator at active block's start bar
				if (ShowBlockSeparators && blockSepBrushDx != null && active.StartBarIndex >= fromIdx && active.StartBarIndex <= toIdx)
				{
					float sepX = chartControl.GetXByBarIndex(ChartBars, active.StartBarIndex);
					RenderTarget.DrawLine(
						new Vector2(sepX, panelTop),
						new Vector2(sepX, panelBottom),
						blockSepBrushDx, 1f);
				}
			}
		}

		private void DrawBlockProfile(ChartControl chartControl, ChartScale chartScale,
			StepBlock block, float baseSpineX, int profileWidthPx, int deltaWidthPx,
			float panelTop, float panelBottom, bool facingRight, bool isActiveProfile)
		{
			Dictionary<double, long> volMap;
			lock (block.SyncObj)
			{
				if (block.VolByPrice.Count == 0) return;
				volMap = isActiveProfile ? new Dictionary<double, long>(block.VolByPrice) : block.VolByPrice;
			}

			// Find max volume and POC
			long   maxVol   = 0;
			double pocPrice = double.NaN;
			double vahPrice = double.NaN, valPrice = double.NaN;
			bool haveVA = false;

			if (!block.IsVACalculated || isActiveProfile)
			{
				foreach (var kvp in volMap)
				{
					if (kvp.Value > maxVol)
					{
						maxVol   = kvp.Value;
						pocPrice = kvp.Key;
					}
				}
				if (maxVol > 0 && ShowValueArea && (ShowVAColor || ShowVALines))
				{
					haveVA = CalcValueArea(volMap, pocPrice, out vahPrice, out valPrice);
				}
				
				if (!isActiveProfile)
				{
					block.MaxVol = maxVol;
					block.POCPrice = pocPrice;
					block.VAHPrice = vahPrice;
					block.VALPrice = valPrice;
					block.IsVACalculated = true;
				}
			}
			else
			{
				maxVol = block.MaxVol;
				pocPrice = block.POCPrice;
				vahPrice = block.VAHPrice;
				valPrice = block.VALPrice;
				haveVA = !double.IsNaN(vahPrice);
			}

			if (maxVol <= 0) return;

			double volCompHeight = VolumeTickCompression * TickSize;

			// --- VOLUME PROFILE ---
			if ((isActiveProfile && ShowActiveVolume) || (!isActiveProfile && ShowHistoricalVolume))
			{
				foreach (var kvp in volMap)
				{
					double price = kvp.Key;
					long   vol   = kvp.Value;

					int yTop = chartScale.GetYByValue(price + volCompHeight);
					int yBot = chartScale.GetYByValue(price);

					if (yBot < panelTop - 20 || yTop > panelBottom + 20) continue;

					int   rowHeight = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
					float drawY     = Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f;

					float barWidth = (float)(profileWidthPx * (vol / (double)maxVol));
					if (barWidth < 0.5f) continue;

					RectangleF rect;
					if (facingRight)
					{
						rect = new RectangleF(baseSpineX, drawY, barWidth, rowHeight);
					}
					else
					{
						if (MirrorProfiles)
						{
							// If mirroring the active right profile:
							// The axis is shifted explicitly to `baseSpineX - profileWidthPx`.
							// Volume draws facing RIGHT from this axis (so it sits on the right side).
							float shiftedAxisX = baseSpineX - profileWidthPx;
							rect = new RectangleF(shiftedAxisX, drawY, barWidth, rowHeight);
						}
						else
						{
							// Standard active right profile: Volume draws LEFT from the extreme right spine
							rect = new RectangleF(baseSpineX - barWidth, drawY, barWidth, rowHeight);
						}
					}

					// Determine if this row is inside the Value Area
					bool insideVA = haveVA && price >= valPrice - TickSize * 0.01 && price <= vahPrice + TickSize * 0.01;

					// Choose brush: POC > Gradient/Flat
					SolidColorBrush brush;

					if (ShowPOC && Math.Abs(price - pocPrice) < TickSize * 0.01)
					{
						brush = pocBrushDx;
					}
					else if (UseGradient)
					{
						var palette = (ShowValueArea && ShowVAColor && insideVA)
							? (isActiveProfile ? vaGradientBrushes : histVaGradientBrushes)
							: (isActiveProfile ? volGradientBrushes : histVolGradientBrushes);

						if (palette != null)
						{
							double ratio = vol / (double)maxVol;
							int steps = palette.Length;
							int gradIdx = (int)(ratio * (steps - 1));
							if (gradIdx < 0) gradIdx = 0;
							if (gradIdx >= steps) gradIdx = steps - 1;
							brush = palette[gradIdx];
						}
						else
						{
							brush = isActiveProfile ? volBrushDx : histVolBrushDx;
						}
					}
					else
					{
						if (ShowValueArea && ShowVAColor && insideVA)
							brush = isActiveProfile ? vaVolBrushDx : histVaVolBrushDx;
						else
							brush = isActiveProfile ? volBrushDx : histVolBrushDx;
					}

					RenderTarget.FillRectangle(rect, brush);
				}

				// --- Draw VA boundary lines ---
				if (haveVA && ShowValueArea && ShowVALines && vaLineBrushDx != null)
				{
					float lineLeft, lineRight;
					if (facingRight)
					{
						int usedDeltaWidth = isActiveProfile ? ActiveDeltaWidthPx : HistoricalDeltaWidthPx;
						lineLeft  = MirrorProfiles ? baseSpineX - usedDeltaWidth - 2 : baseSpineX - 2;
						lineRight = baseSpineX + profileWidthPx + 2;
					}
					else
					{
						if (MirrorProfiles)
						{
							int usedDeltaWidth = isActiveProfile ? ActiveDeltaWidthPx : HistoricalDeltaWidthPx;
							float shiftedAxisX = baseSpineX - profileWidthPx;
							lineLeft  = shiftedAxisX - usedDeltaWidth - 2;
							lineRight = shiftedAxisX + profileWidthPx + 2;
						}
						else
						{
							lineLeft  = baseSpineX - profileWidthPx - 2;
							lineRight = baseSpineX + 2;
						}
					}

					// VAH line
					float yVAH = chartScale.GetYByValue(vahPrice + volCompHeight);
					if (yVAH >= panelTop - 5 && yVAH <= panelBottom + 5)
					{
						RenderTarget.DrawLine(
							new Vector2(lineLeft, yVAH),
							new Vector2(lineRight, yVAH),
							vaLineBrushDx, VALineThickness, vaLineStrokeDx);
					}

					// VAL line
					float yVAL = chartScale.GetYByValue(valPrice);
					if (yVAL >= panelTop - 5 && yVAL <= panelBottom + 5)
					{
						RenderTarget.DrawLine(
							new Vector2(lineLeft, yVAL),
							new Vector2(lineRight, yVAL),
							vaLineBrushDx, VALineThickness, vaLineStrokeDx);
					}
				}
			}

		}

		private void DrawBlockDelta(ChartControl chartControl, ChartScale chartScale,
			StepBlock block, float baseSpineX, int profileWidthPx, int deltaWidthPx,
			float panelTop, float panelBottom, bool facingRight, bool isActiveProfile, int deltaCompTicks)
		{
				Dictionary<double, long> deltaMap;
				lock (block.SyncObj)
				{
					if (block.DeltaByPrice.Count == 0) return;
					deltaMap = isActiveProfile ? new Dictionary<double, long>(block.DeltaByPrice) : block.DeltaByPrice;
				}

				// Group delta into deltaCompTicks-sized buckets at render time
				double deltaComp = deltaCompTicks * TickSize;
				var groupedDelta = new Dictionary<double, long>();
				foreach (var kvp in deltaMap)
				{
					double bucketPrice = Math.Floor(kvp.Key / deltaComp + 0.000001) * deltaComp;
					if (groupedDelta.TryGetValue(bucketPrice, out long existing))
						groupedDelta[bucketPrice] = existing + kvp.Value;
					else
						groupedDelta[bucketPrice] = kvp.Value;
				}

				long maxAbsDelta = 0;
				foreach (var kvp in groupedDelta)
				{
					long absVal = Math.Abs(kvp.Value);
					if (absVal > maxAbsDelta) maxAbsDelta = absVal;
				}

				if (maxAbsDelta > 0)
				{
					foreach (var kvp in groupedDelta)
					{
						int yTop = chartScale.GetYByValue(kvp.Key + deltaComp);
						int yBot = chartScale.GetYByValue(kvp.Key);

						if (yBot < panelTop - 20 || yTop > panelBottom + 20) continue;

						int   height = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
						float drawY  = Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f;

						float w = (float)(deltaWidthPx * (Math.Abs(kvp.Value) / (double)maxAbsDelta));
						if (w < 0.5f && !(ShowDeltaText && deltaTextFormatDx != null && deltaTextBrushDx != null)) continue;
						
						w = Math.Max(w, 0.5f); // Ensure at least a minimal width if text needs to be drawn but rect is tiny
						
						SolidColorBrush deltaBrush;
						if (isActiveProfile)
							deltaBrush = kvp.Value >= 0 ? posDeltaBrushDx : negDeltaBrushDx;
						else
							deltaBrush = kvp.Value >= 0 ? histPosDeltaBrushDx : histNegDeltaBrushDx;

						RectangleF rect;
						if (facingRight)
						{
							if (MirrorProfiles)
							{
								// Historical profile, mirrored: Delta draws LEFT from baseSpineX
								rect = new RectangleF(baseSpineX - w, drawY, w, height);
							}
							else
							{
								// Historical profile, standard: Delta draws RIGHT from baseSpineX (overlapping volume)
								rect = new RectangleF(baseSpineX, drawY, w, height);
							}
						}
						else
						{
							if (MirrorProfiles)
							{
								// Active profile, mirrored: Axis is shifted to (baseSpineX - profileWidthPx).
								// Delta draws LEFT from shifted axis.
								float shiftedAxisX = baseSpineX - profileWidthPx;
								rect = new RectangleF(shiftedAxisX - w, drawY, w, height);
							}
							else
							{
								// Active profile, standard: Delta draws LEFT from baseSpineX (overlapping volume at right edge)
								rect = new RectangleF(baseSpineX - w, drawY, w, height);
							}
						}

						// Draw the background rect
						if (w >= 0.5f)
						{
							RenderTarget.FillRectangle(rect, deltaBrush);
						}

						if (ShowDeltaText && deltaTextFormatDx != null && deltaTextBrushDx != null)
						{
							long absVol = Math.Abs(kvp.Value);
							// Only attempt to draw text if the row height is at least 6 pixels, and volume meets threshold
							if (rect.Height >= 6 && absVol >= DeltaTextMinThreshold)
							{
								string text = kvp.Value > 0 ? $"+{kvp.Value}" : kvp.Value.ToString();
								
								// Rather than strictly hiding the text if it doesn't fit in the tiny rect,
								// we probably want to just draw it within the max Delta Width area.
								RectangleF textRect = rect;
								// Subtract 4 pixels from the allocatable width so the text doesn't touch the very edge (spine axis)
								textRect.Width = Math.Max(1, deltaWidthPx - 4); 
								
								// If left-facing, we need to shift the left edge so it aligns right at the spine properly
								if (facingRight)
								{
									if (MirrorProfiles && !isActiveProfile)
									{
										// Historical profile, mirrored: Delta facing left from baseSpineX
										textRect.X = baseSpineX - deltaWidthPx;
									}
									// Standard historical face right, so X = baseSpineX is fine
								}
								else
								{
									if (MirrorProfiles && isActiveProfile)
									{
										// Active profile, mirrored: Delta facing left from shifted axis
										float shiftedAxisX = baseSpineX - profileWidthPx;
										textRect.X = shiftedAxisX - deltaWidthPx;
									}
									else
									{
										// Active profile, standard: Delta facing left from baseSpineX
										textRect.X = baseSpineX - deltaWidthPx;
									}
								}

								// Draw text securely bypassing TextLayout COM object creation
								RenderTarget.DrawText(text, deltaTextFormatDx, textRect, deltaTextBrushDx);
							}
						}
					}
				}
		}

		#region DX Resources
		private void EnsureDxResources()
		{
			if (volBrushDx == null)
				volBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VolumeBrush, ActiveVolumeOpacity));
			if (histVolBrushDx == null)
				histVolBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VolumeBrush, HistoricalVolumeOpacity));
				
			if (pocBrushDx == null)
				pocBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(POCBrush, 1f));
				
			if (posDeltaBrushDx == null)
				posDeltaBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(PositiveDeltaBrush, ActiveDeltaOpacity));
			if (negDeltaBrushDx == null)
				negDeltaBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(NegativeDeltaBrush, ActiveDeltaOpacity));
				
			if (histPosDeltaBrushDx == null)
				histPosDeltaBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(PositiveDeltaBrush, HistoricalDeltaOpacity));
			if (histNegDeltaBrushDx == null)
				histNegDeltaBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(NegativeDeltaBrush, HistoricalDeltaOpacity));
				
			if (blockSepBrushDx == null)
				blockSepBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(BlockSeparatorBrush, 0.4f));

			// VA flat brush
			if (vaVolBrushDx == null)
				vaVolBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VABrush, ActiveVolumeOpacity));
			if (histVaVolBrushDx == null)
				histVaVolBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VABrush, HistoricalVolumeOpacity));

			// VA line brush + stroke
			if (vaLineBrushDx == null)
				vaLineBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(VALineBrush, 1f));
			if (vaLineStrokeDx == null)
			{
				DashStyle ds;
				switch (VALineStyle)
				{
					case StepVALineStyleEnum.Solid:   ds = DashStyle.Solid;   break;
					case StepVALineStyleEnum.Dot:     ds = DashStyle.Dot;     break;
					case StepVALineStyleEnum.DashDot: ds = DashStyle.DashDot; break;
					default:                          ds = DashStyle.Dash;    break;
				}
				vaLineStrokeDx = new StrokeStyle(RenderTarget.Factory,
					new StrokeStyleProperties { DashStyle = ds });
			}

			// Delta text
			if (deltaTextBrushDx == null)
				deltaTextBrushDx = new SolidColorBrush(RenderTarget, ToDxColor(DeltaTextBrush, 1f));
			
			if (deltaTextFormatDx == null)
			{
				deltaTextFormatDx = new TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory,
					"Segoe UI",
					FontWeight.Bold,
					FontStyle.Normal,
					FontStretch.Normal,
					DeltaTextFontSize)
				{
					TextAlignment = TextAlignment.Trailing,
					ParagraphAlignment = ParagraphAlignment.Center
				};
			}

			// Build gradient palettes
			int steps = Math.Max(2, GradientSteps);

			if (UseGradient && (volGradientBrushes == null || histVolGradientBrushes == null || lastBuiltGradientSteps != steps))
			{
				if (volGradientBrushes != null)
					for (int i = 0; i < volGradientBrushes.Length; i++)
						volGradientBrushes[i]?.Dispose();
				if (histVolGradientBrushes != null)
					for (int i = 0; i < histVolGradientBrushes.Length; i++)
						histVolGradientBrushes[i]?.Dispose();

				volGradientBrushes     = BuildGradientPalette(VolumeBrush, steps, ActiveVolumeOpacity);
				histVolGradientBrushes = BuildGradientPalette(VolumeBrush, steps, HistoricalVolumeOpacity);
				
				lastBuiltGradientSteps = steps;
			}

			if (UseGradient && ShowValueArea && ShowVAColor && (vaGradientBrushes == null || histVaGradientBrushes == null || lastBuiltVAGradientSteps != steps))
			{
				if (vaGradientBrushes != null)
					for (int i = 0; i < vaGradientBrushes.Length; i++)
						vaGradientBrushes[i]?.Dispose();
				if (histVaGradientBrushes != null)
					for (int i = 0; i < histVaGradientBrushes.Length; i++)
						histVaGradientBrushes[i]?.Dispose();

				vaGradientBrushes     = BuildGradientPalette(VABrush, steps, ActiveVolumeOpacity);
				histVaGradientBrushes = BuildGradientPalette(VABrush, steps, HistoricalVolumeOpacity);
				
				lastBuiltVAGradientSteps = steps;
			}
		}

		private SolidColorBrush[] BuildGradientPalette(WpfBrush baseBrush, int steps, float opacity)
		{
			var baseColor = BrushToMediaColor(baseBrush);
			var palette = new SolidColorBrush[steps];

			for (int i = 0; i < steps; i++)
			{
				float t = i / (float)(steps - 1);
				float brightness = MinBrightness + t * (1f - MinBrightness);

				var c = new Color4(
					(baseColor.R / 255f) * brightness,
					(baseColor.G / 255f) * brightness,
					(baseColor.B / 255f) * brightness,
					(baseColor.A / 255f) * opacity);

				palette[i] = new SolidColorBrush(RenderTarget, c);
			}

			return palette;
		}

		private static System.Windows.Media.Color BrushToMediaColor(WpfBrush b)
		{
			return (b as WpfSolidColorBrush)?.Color ?? WpfColors.White;
		}

		private Color4 ToDxColor(WpfBrush b, float alphaMult)
		{
			var c = BrushToMediaColor(b);
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * alphaMult);
		}
		#endregion
		#endregion

		#region Properties

		// --- Data ---
		[NinjaScriptProperty]
		[Display(Name = "Step Interval", GroupName = "Data", Order = 0)]
		public StepIntervalType StepInterval { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Volume Tick Compression", GroupName = "Data", Order = 1)]
		public int VolumeTickCompression { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Delta Tick Compression", GroupName = "Data", Order = 2)]
		public int DeltaTickCompression { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Dynamic Aggregation", Description = "Auto adjust delta compression upon zoom", GroupName = "Data", Order = 3)]
		public bool UseDynamicAggregation { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name = "Dynamic Aggregation Multiplier", Description = "Lower value = more granular blocks", GroupName = "Data", Order = 4)]
		public double DynamicAggregationMultiplier { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "RTH Only", GroupName = "Data", Order = 4)]
		public bool RTHOnly { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "RTH Start Time", GroupName = "Data", Order = 5)]
		public DateTime RTHStart { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name = "RTH End Time", GroupName = "Data", Order = 6)]
		public DateTime RTHEnd { get; set; }

		// --- Layout ---
		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Historical Profile Width (px)", GroupName = "Layout", Order = 10)]
		public int HistoricalProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Active Profile Width (px)", GroupName = "Layout", Order = 11)]
		public int ActiveProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Active Delta Width (px)", GroupName = "Layout", Order = 12)]
		public int ActiveDeltaWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(10, 500)]
		[Display(Name = "Historical Delta Width (px)", GroupName = "Layout", Order = 13)]
		public int HistoricalDeltaWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 500)]
		[Display(Name = "Right Offset (px)", GroupName = "Layout", Order = 14)]
		public int RightOffsetPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 10)]
		[Display(Name = "Profile Bar Spacing (px)", GroupName = "Layout", Order = 15)]
		public int ProfileBarSpacingPx { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Mirror Profiles", Description = "Draw Delta and Volume profiles in opposite directions from the central axis", GroupName = "Layout", Order = 16)]
		public bool MirrorProfiles { get; set; }

		// --- Visibility ---
		[NinjaScriptProperty]
		[Display(Name = "Show Active Volume", GroupName = "Visibility", Order = 20)]
		public bool ShowActiveVolume { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Hist Volume", GroupName = "Visibility", Order = 21)]
		public bool ShowHistoricalVolume { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Active Delta", GroupName = "Visibility", Order = 22)]
		public bool ShowActiveDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Historical Delta", GroupName = "Visibility", Order = 23)]
		public bool ShowHistoricalDelta { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show POC", GroupName = "Visibility", Order = 24)]
		public bool ShowPOC { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Block Separators", GroupName = "Visibility", Order = 25)]
		public bool ShowBlockSeparators { get; set; }

		// --- Gradient ---
		[NinjaScriptProperty]
		[Display(Name = "Use Gradient", GroupName = "Gradient", Order = 30)]
		public bool UseGradient { get; set; }

		[NinjaScriptProperty]
		[Range(2, 64)]
		[Display(Name = "Gradient Steps", GroupName = "Gradient", Order = 31)]
		public int GradientSteps { get; set; }

		[NinjaScriptProperty]
		[Range(0.05, 1.0)]
		[Display(Name = "Min Brightness", GroupName = "Gradient", Order = 32)]
		public float MinBrightness { get; set; }

		// --- Value Area ---
		[NinjaScriptProperty]
		[Display(Name = "Show Value Area", GroupName = "Value Area", Order = 40)]
		public bool ShowValueArea { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VA Color Mode", Description = "Color rows inside the Value Area differently", GroupName = "Value Area", Order = 41)]
		public bool ShowVAColor { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VA Boundary Lines", Description = "Draw lines at VAH and VAL", GroupName = "Value Area", Order = 42)]
		public bool ShowVALines { get; set; }

		[NinjaScriptProperty]
		[Range(50, 95)]
		[Display(Name = "VA Percent", GroupName = "Value Area", Order = 43)]
		public int ValueAreaPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.5, 6.0)]
		[Display(Name = "VA Line Thickness", GroupName = "Value Area", Order = 44)]
		public float VALineThickness { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "VA Line Style", GroupName = "Value Area", Order = 45)]
		public StepVALineStyleEnum VALineStyle { get; set; }

		[XmlIgnore]
		[Display(Name = "VA Color", GroupName = "Value Area", Order = 46)]
		public WpfBrush VABrush { get; set; }
		[Browsable(false)]
		public string VABrushSerialize
		{ get { return Serialize.BrushToString(VABrush); } set { VABrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "VA Line Color", GroupName = "Value Area", Order = 47)]
		public WpfBrush VALineBrush { get; set; }
		[Browsable(false)]
		public string VALineBrushSerialize
		{ get { return Serialize.BrushToString(VALineBrush); } set { VALineBrush = Serialize.StringToBrush(value); } }

		// --- Colors: Profile ---
		[XmlIgnore]
		[Display(Name = "Volume Color", GroupName = "Colors", Order = 50)]
		public WpfBrush VolumeBrush { get; set; }
		[Browsable(false)]
		public string VolumeBrushSerialize
		{ get { return Serialize.BrushToString(VolumeBrush); } set { VolumeBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Active Volume Opacity", GroupName = "Colors", Order = 51)]
		public float ActiveVolumeOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Hist Volume Opacity", GroupName = "Colors", Order = 52)]
		public float HistoricalVolumeOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "POC Color", GroupName = "Colors", Order = 53)]
		public WpfBrush POCBrush { get; set; }
		[Browsable(false)]
		public string POCBrushSerialize
		{ get { return Serialize.BrushToString(POCBrush); } set { POCBrush = Serialize.StringToBrush(value); } }

		// --- Colors: Delta ---
		[XmlIgnore]
		[Display(Name = "Positive Delta", GroupName = "Colors", Order = 53)]
		public WpfBrush PositiveDeltaBrush { get; set; }
		[Browsable(false)]
		public string PositiveDeltaBrushSerialize
		{ get { return Serialize.BrushToString(PositiveDeltaBrush); } set { PositiveDeltaBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Negative Delta", GroupName = "Colors", Order = 54)]
		public WpfBrush NegativeDeltaBrush { get; set; }
		[Browsable(false)]
		public string NegativeDeltaBrushSerialize
		{ get { return Serialize.BrushToString(NegativeDeltaBrush); } set { NegativeDeltaBrush = Serialize.StringToBrush(value); } }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Active Delta Opacity", GroupName = "Colors", Order = 56)]
		public float ActiveDeltaOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Hist Delta Opacity", GroupName = "Colors", Order = 57)]
		public float HistoricalDeltaOpacity { get; set; }

		// --- Colors: Separators ---
		[XmlIgnore]
		[Display(Name = "Block Separator Color", GroupName = "Colors", Order = 58)]
		public WpfBrush BlockSeparatorBrush { get; set; }
		[Browsable(false)]
		public string BlockSeparatorBrushSerialize
		{ get { return Serialize.BrushToString(BlockSeparatorBrush); } set { BlockSeparatorBrush = Serialize.StringToBrush(value); } }

		// --- Delta Text ---
		[NinjaScriptProperty]
		[Display(Name = "Show Delta Text", GroupName = "Delta Text", Order = 60)]
		public bool ShowDeltaText { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1000000)]
		[Display(Name = "Minimum Threshold", Description = "Minimum delta value (absolute) required to show text", GroupName = "Delta Text", Order = 61)]
		public int DeltaTextMinThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(6, 48)]
		[Display(Name = "Text Font Size", GroupName = "Delta Text", Order = 62)]
		public float DeltaTextFontSize { get; set; }

		[XmlIgnore]
		[Display(Name = "Text Color", GroupName = "Delta Text", Order = 63)]
		public WpfBrush DeltaTextBrush { get; set; }
		[Browsable(false)]
		public string DeltaTextBrushSerialize
		{ get { return Serialize.BrushToString(DeltaTextBrush); } set { DeltaTextBrush = Serialize.StringToBrush(value); } }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaStepProfile[] cacheOrcaStepProfile;
		public OrcaStepProfile OrcaStepProfile(StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			return OrcaStepProfile(Input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, rTHOnly, rTHStart, rTHEnd, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
		}

		public OrcaStepProfile OrcaStepProfile(ISeries<double> input, StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			if (cacheOrcaStepProfile != null)
				for (int idx = 0; idx < cacheOrcaStepProfile.Length; idx++)
					if (cacheOrcaStepProfile[idx] != null && cacheOrcaStepProfile[idx].StepInterval == stepInterval && cacheOrcaStepProfile[idx].VolumeTickCompression == volumeTickCompression && cacheOrcaStepProfile[idx].DeltaTickCompression == deltaTickCompression && cacheOrcaStepProfile[idx].UseDynamicAggregation == useDynamicAggregation && cacheOrcaStepProfile[idx].DynamicAggregationMultiplier == dynamicAggregationMultiplier && cacheOrcaStepProfile[idx].RTHOnly == rTHOnly && cacheOrcaStepProfile[idx].RTHStart == rTHStart && cacheOrcaStepProfile[idx].RTHEnd == rTHEnd && cacheOrcaStepProfile[idx].HistoricalProfileWidthPx == historicalProfileWidthPx && cacheOrcaStepProfile[idx].ActiveProfileWidthPx == activeProfileWidthPx && cacheOrcaStepProfile[idx].ActiveDeltaWidthPx == activeDeltaWidthPx && cacheOrcaStepProfile[idx].HistoricalDeltaWidthPx == historicalDeltaWidthPx && cacheOrcaStepProfile[idx].RightOffsetPx == rightOffsetPx && cacheOrcaStepProfile[idx].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaStepProfile[idx].MirrorProfiles == mirrorProfiles && cacheOrcaStepProfile[idx].ShowActiveVolume == showActiveVolume && cacheOrcaStepProfile[idx].ShowHistoricalVolume == showHistoricalVolume && cacheOrcaStepProfile[idx].ShowActiveDelta == showActiveDelta && cacheOrcaStepProfile[idx].ShowHistoricalDelta == showHistoricalDelta && cacheOrcaStepProfile[idx].ShowPOC == showPOC && cacheOrcaStepProfile[idx].ShowBlockSeparators == showBlockSeparators && cacheOrcaStepProfile[idx].UseGradient == useGradient && cacheOrcaStepProfile[idx].GradientSteps == gradientSteps && cacheOrcaStepProfile[idx].MinBrightness == minBrightness && cacheOrcaStepProfile[idx].ShowValueArea == showValueArea && cacheOrcaStepProfile[idx].ShowVAColor == showVAColor && cacheOrcaStepProfile[idx].ShowVALines == showVALines && cacheOrcaStepProfile[idx].ValueAreaPercent == valueAreaPercent && cacheOrcaStepProfile[idx].VALineThickness == vALineThickness && cacheOrcaStepProfile[idx].VALineStyle == vALineStyle && cacheOrcaStepProfile[idx].ActiveVolumeOpacity == activeVolumeOpacity && cacheOrcaStepProfile[idx].HistoricalVolumeOpacity == historicalVolumeOpacity && cacheOrcaStepProfile[idx].ActiveDeltaOpacity == activeDeltaOpacity && cacheOrcaStepProfile[idx].HistoricalDeltaOpacity == historicalDeltaOpacity && cacheOrcaStepProfile[idx].ShowDeltaText == showDeltaText && cacheOrcaStepProfile[idx].DeltaTextMinThreshold == deltaTextMinThreshold && cacheOrcaStepProfile[idx].DeltaTextFontSize == deltaTextFontSize && cacheOrcaStepProfile[idx].EqualsInput(input))
						return cacheOrcaStepProfile[idx];
			return CacheIndicator<OrcaStepProfile>(new OrcaStepProfile(){ StepInterval = stepInterval, VolumeTickCompression = volumeTickCompression, DeltaTickCompression = deltaTickCompression, UseDynamicAggregation = useDynamicAggregation, DynamicAggregationMultiplier = dynamicAggregationMultiplier, RTHOnly = rTHOnly, RTHStart = rTHStart, RTHEnd = rTHEnd, HistoricalProfileWidthPx = historicalProfileWidthPx, ActiveProfileWidthPx = activeProfileWidthPx, ActiveDeltaWidthPx = activeDeltaWidthPx, HistoricalDeltaWidthPx = historicalDeltaWidthPx, RightOffsetPx = rightOffsetPx, ProfileBarSpacingPx = profileBarSpacingPx, MirrorProfiles = mirrorProfiles, ShowActiveVolume = showActiveVolume, ShowHistoricalVolume = showHistoricalVolume, ShowActiveDelta = showActiveDelta, ShowHistoricalDelta = showHistoricalDelta, ShowPOC = showPOC, ShowBlockSeparators = showBlockSeparators, UseGradient = useGradient, GradientSteps = gradientSteps, MinBrightness = minBrightness, ShowValueArea = showValueArea, ShowVAColor = showVAColor, ShowVALines = showVALines, ValueAreaPercent = valueAreaPercent, VALineThickness = vALineThickness, VALineStyle = vALineStyle, ActiveVolumeOpacity = activeVolumeOpacity, HistoricalVolumeOpacity = historicalVolumeOpacity, ActiveDeltaOpacity = activeDeltaOpacity, HistoricalDeltaOpacity = historicalDeltaOpacity, ShowDeltaText = showDeltaText, DeltaTextMinThreshold = deltaTextMinThreshold, DeltaTextFontSize = deltaTextFontSize }, input, ref cacheOrcaStepProfile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaStepProfile OrcaStepProfile(StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			return indicator.OrcaStepProfile(Input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, rTHOnly, rTHStart, rTHEnd, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
		}

		public Indicators.OrcaStepProfile OrcaStepProfile(ISeries<double> input , StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			return indicator.OrcaStepProfile(input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, rTHOnly, rTHStart, rTHEnd, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaStepProfile OrcaStepProfile(StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			return indicator.OrcaStepProfile(Input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, rTHOnly, rTHStart, rTHEnd, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
		}

		public Indicators.OrcaStepProfile OrcaStepProfile(ISeries<double> input , StepIntervalType stepInterval, int volumeTickCompression, int deltaTickCompression, bool useDynamicAggregation, double dynamicAggregationMultiplier, bool rTHOnly, DateTime rTHStart, DateTime rTHEnd, int historicalProfileWidthPx, int activeProfileWidthPx, int activeDeltaWidthPx, int historicalDeltaWidthPx, int rightOffsetPx, int profileBarSpacingPx, bool mirrorProfiles, bool showActiveVolume, bool showHistoricalVolume, bool showActiveDelta, bool showHistoricalDelta, bool showPOC, bool showBlockSeparators, bool useGradient, int gradientSteps, float minBrightness, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, StepVALineStyleEnum vALineStyle, float activeVolumeOpacity, float historicalVolumeOpacity, float activeDeltaOpacity, float historicalDeltaOpacity, bool showDeltaText, int deltaTextMinThreshold, float deltaTextFontSize)
		{
			return indicator.OrcaStepProfile(input, stepInterval, volumeTickCompression, deltaTickCompression, useDynamicAggregation, dynamicAggregationMultiplier, rTHOnly, rTHStart, rTHEnd, historicalProfileWidthPx, activeProfileWidthPx, activeDeltaWidthPx, historicalDeltaWidthPx, rightOffsetPx, profileBarSpacingPx, mirrorProfiles, showActiveVolume, showHistoricalVolume, showActiveDelta, showHistoricalDelta, showPOC, showBlockSeparators, useGradient, gradientSteps, minBrightness, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, activeVolumeOpacity, historicalVolumeOpacity, activeDeltaOpacity, historicalDeltaOpacity, showDeltaText, deltaTextMinThreshold, deltaTextFontSize);
		}
	}
}

#endregion

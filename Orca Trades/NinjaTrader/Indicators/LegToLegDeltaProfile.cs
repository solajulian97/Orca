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

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

// IMPORTANT: alias WPF brushes to avoid conflict with SharpDX.Direct2D1.Brush
using WpfBrush = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColors = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public enum ProfileModes
	{
		LegToLeg = 0,
		SessionCurrentDay = 1
	}

	public enum VolumeProfileLayer
	{
		BehindDelta = 0,
		InFrontOfDelta = 1,
		LeftOfDelta = 2,
		RightOfDelta = 3
	}

	public enum LegRotationMode
	{
		DistanceFromExtreme = 0,
		TrueSwingAlternate = 1
	}

	internal enum LegDirection
	{
		Unknown = 0,
		Up = 1,
		Down = -1
	}

	public class LegToLegDeltaProfile : Indicator
	{
		// Per primary bar maps
		private List<Dictionary<double, long>> barDeltaMaps;
		private List<Dictionary<double, long>> barVolMaps;

		// Active (current) maps
		private readonly Dictionary<double, long> legDeltaByPrice = new Dictionary<double, long>();
		private readonly Dictionary<double, long> sessionDeltaByPrice = new Dictionary<double, long>();

		private readonly Dictionary<double, long> legVolByPrice = new Dictionary<double, long>();
		private readonly Dictionary<double, long> sessionVolByPrice = new Dictionary<double, long>();

		// Start bar index for current session (primary series)
		private int sessionStartBar = -1;

		// Start bar index for current "leg profile" (primary series)
		private int legStartBar = -1;

		// Rotation tracking (for LegToLeg mode)
		private double legHigh = double.NaN;
		private double legLow  = double.NaN;
		private int    legHighBar = -1;
		private int    legLowBar  = -1;
		private LegDirection legDir = LegDirection.Unknown;

		// Bid/Ask cache (Tick Replay)
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;

		// Tick-rule fallback
		private double prevLast = double.NaN;

		// Rendering resources
		private TextFormat textFormat;
		private SolidColorBrush posBrushDx, negBrushDx, textBrushDx, spineBrushDx, volBrushDx;

		// Brush change detection so settings update without requiring render-target changes
		private string lastPosBrushSer, lastNegBrushSer, lastTextBrushSer, lastVolBrushSer;
		private float  lastDeltaOpacity = -1f;
		private float  lastVolOpacity = -1f;
		private int    lastFontSize = -1;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name        = "LegToLegDeltaProfile";
				Description = "Rotation-based leg delta profile OR current-session delta profile, with optional tick-based volume profile layer (behind/infront/side-by-side).";
				Calculate   = Calculate.OnEachTick;
				IsOverlay   = true;

				ProfileMode = ProfileModes.LegToLeg;

				// Rotation threshold in POINTS (price units). Example NQ: 65
				RotationPoints = 65;
				RotationMode = LegRotationMode.TrueSwingAlternate;

				// Delta rendering
				MaxProfileWidthPx      = 70;
				MinAbsDeltaToShow      = 1;
				RebuildLookbackBarsCap = 2000;

				// Auto text behavior (recommended ON) =====
				AutoDeltaText = true;
				AutoFontMin = 6;
				AutoFontMax = 18;

				// If you disable AutoDeltaText, these are used:
				FontSize = 10;

				// Auto scaling to avoid overlap when zoomed out (DELTA ONLY)
				MinRowHeightPx = 10;

				ShowSpine    = false;
				SpineWidthPx = 2;
				RightMarginPx = 0;

				DeltaOpacity = 0.85f;

				RightReservedPercent = 0.10;
				XOffsetPx = 0;

				// NT color pickers (Brushes) - DELTA
				PositiveBrush = WpfBrushes.Blue;
				NegativeBrush = WpfBrushes.Red;
				TextBrush     = WpfBrushes.LightGray;

				// Optional VOLUME profile
				ShowVolumeProfile    = true;
				VolumeLayer          = VolumeProfileLayer.RightOfDelta;
				VolumeProfileWidthPx = 70;
				VolumeOpacity        = 0.25f;
				VolumeBrush          = WpfBrushes.Gray;

				SideBySideGapPx = 4;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				barDeltaMaps = new List<Dictionary<double, long>>(4096);
				barVolMaps   = new List<Dictionary<double, long>>(4096);

				legDeltaByPrice.Clear();
				sessionDeltaByPrice.Clear();
				legVolByPrice.Clear();
				sessionVolByPrice.Clear();

				sessionStartBar = -1;
				legStartBar = -1;

				legHigh = double.NaN;
				legLow  = double.NaN;
				legHighBar = -1;
				legLowBar  = -1;
				legDir  = LegDirection.Unknown;

				lastPosBrushSer = lastNegBrushSer = lastTextBrushSer = lastVolBrushSer = null;
				lastDeltaOpacity = -1f;
				lastVolOpacity = -1f;
				lastFontSize = -1;
			}
			else if (State == State.Terminated)
			{
				DisposeDx();
			}
		}

		private void DisposeDx()
		{
			try
			{
				textFormat?.Dispose();
				posBrushDx?.Dispose();
				negBrushDx?.Dispose();
				textBrushDx?.Dispose();
				spineBrushDx?.Dispose();
				volBrushDx?.Dispose();
			}
			catch { }
			finally
			{
				textFormat = null;
				posBrushDx = null;
				negBrushDx = null;
				textBrushDx = null;
				spineBrushDx = null;
				volBrushDx = null;
			}
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Bid)
				lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask)
				lastAsk = e.Price;
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				ProcessTickIntoPrimaryBar();
				return;
			}

			if (CurrentBar < 1)
				return;

			EnsureBarMaps(CurrentBar);

			if (Bars.IsFirstBarOfSession)
			{
				sessionStartBar = CurrentBar;
				RebuildSessionFrom(sessionStartBar);

				legStartBar = sessionStartBar;
				RebuildLegFromStart(legStartBar);

				legHigh = High[0];
				legLow  = Low[0];
				legHighBar = CurrentBar;
				legLowBar  = CurrentBar;
				legDir  = LegDirection.Unknown;
			}

			if (sessionStartBar < 0)
			{
				sessionStartBar = 0;
				RebuildSessionFrom(sessionStartBar);
			}

			if (legStartBar < 0)
			{
				legStartBar = sessionStartBar >= 0 ? sessionStartBar : 0;
				RebuildLegFromStart(legStartBar);

				legHigh = High[0];
				legLow  = Low[0];
				legHighBar = CurrentBar;
				legLowBar  = CurrentBar;
				legDir  = LegDirection.Unknown;
			}

			if (ProfileMode == ProfileModes.LegToLeg)
			{
				// Update extremes of current leg + remember where they occurred
				if (double.IsNaN(legHigh) || double.IsNaN(legLow))
				{
					legHigh = High[0];
					legLow  = Low[0];
					legHighBar = CurrentBar;
					legLowBar  = CurrentBar;
				}
				else
				{
					if (High[0] >= legHigh)
					{
						legHigh = High[0];
						legHighBar = CurrentBar;
					}
					if (Low[0] <= legLow)
					{
						legLow = Low[0];
						legLowBar = CurrentBar;
					}
				}

				if (RotationPoints > 0)
				{
					double last = Close[0];

					if (RotationMode == LegRotationMode.DistanceFromExtreme)
					{
						bool rotateDown = last <= (legHigh - RotationPoints);
						bool rotateUp   = last >= (legLow  + RotationPoints);

						if (rotateDown || rotateUp)
							StartNewLegAtCurrentBar(rotateUp ? LegDirection.Up : LegDirection.Down);
					}
					else // TrueSwingAlternate (start new leg at extreme bar)
					{
						if (legDir == LegDirection.Unknown)
						{
							if (last >= (legLow + RotationPoints))
								legDir = LegDirection.Up;
							else if (last <= (legHigh - RotationPoints))
								legDir = LegDirection.Down;
						}

						if (legDir == LegDirection.Up)
						{
							if (last <= (legHigh - RotationPoints))
								StartNewLegAtBar(legHighBar, LegDirection.Down, last);
						}
						else if (legDir == LegDirection.Down)
						{
							if (last >= (legLow + RotationPoints))
								StartNewLegAtBar(legLowBar, LegDirection.Up, last);
						}
					}
				}
			}
		}

		private void StartNewLegAtCurrentBar(LegDirection newDir)
		{
			legStartBar = CurrentBar;

			legHigh = High[0];
			legLow  = Low[0];
			legHighBar = CurrentBar;
			legLowBar  = CurrentBar;

			legDir = (RotationMode == LegRotationMode.TrueSwingAlternate) ? newDir : LegDirection.Unknown;

			RebuildLegFromStart(legStartBar);
		}

		private void StartNewLegAtBar(int startBar, LegDirection newDir, double lastPrice)
		{
			legStartBar = Math.Max(0, startBar);

			RebuildLegFromStart(legStartBar);

			legDir = newDir;

			// Reset extremes for the new leg so next reversal works correctly
			if (newDir == LegDirection.Down)
			{
				// anchor from swing high
				legLow = lastPrice;
				legLowBar = CurrentBar;
				// keep legHigh/legHighBar as swing high already known
			}
			else if (newDir == LegDirection.Up)
			{
				// anchor from swing low
				legHigh = lastPrice;
				legHighBar = CurrentBar;
				// keep legLow/legLowBar as swing low already known
			}
			else
			{
				legHigh = High[0];
				legLow  = Low[0];
				legHighBar = CurrentBar;
				legLowBar  = CurrentBar;
			}
		}

		private void EnsureBarMaps(int primaryBarIndex)
		{
			while (barDeltaMaps.Count <= primaryBarIndex)
				barDeltaMaps.Add(new Dictionary<double, long>());

			while (barVolMaps.Count <= primaryBarIndex)
				barVolMaps.Add(new Dictionary<double, long>());
		}

		private void ProcessTickIntoPrimaryBar()
		{
			int primaryIndex = BarsArray[0].GetBar(Time[0]);
			if (primaryIndex < 0) return;

			EnsureBarMaps(primaryIndex);

			double last = Close[0];
			long vol    = (long)Volume[0];
			if (vol <= 0) return;

			double p = Instrument.MasterInstrument.RoundToTickSize(last);

			// ---------- VOLUME ----------
			var vmap = barVolMaps[primaryIndex];
			if (vmap.TryGetValue(p, out long vExisting))
				vmap[p] = vExisting + vol;
			else
				vmap[p] = vol;

			int legStart = legStartBar >= 0
				? legStartBar
				: Math.Max(0, (BarsArray[0].Count - 1) - RebuildLookbackBarsCap);

			if (primaryIndex >= legStart)
			{
				if (legVolByPrice.TryGetValue(p, out long lv))
					legVolByPrice[p] = lv + vol;
				else
					legVolByPrice[p] = vol;
			}

			if (sessionStartBar >= 0 && primaryIndex >= sessionStartBar)
			{
				if (sessionVolByPrice.TryGetValue(p, out long sv))
					sessionVolByPrice[p] = sv + vol;
				else
					sessionVolByPrice[p] = vol;
			}

			// ---------- DELTA ----------
			long signed = 0;

			if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
			{
				if (last >= lastAsk)
					signed = +vol;
				else if (last <= lastBid)
					signed = -vol;
				else
				{
					if (!double.IsNaN(prevLast))
						signed = (last > prevLast) ? +vol : (last < prevLast ? -vol : 0);
				}
			}
			else
			{
				if (!double.IsNaN(prevLast))
					signed = (last > prevLast) ? +vol : (last < prevLast ? -vol : 0);
			}

			prevLast = last;
			if (signed == 0) return;

			var dmap = barDeltaMaps[primaryIndex];
			if (dmap.TryGetValue(p, out long dExisting))
				dmap[p] = dExisting + signed;
			else
				dmap[p] = signed;

			if (primaryIndex >= legStart)
			{
				if (legDeltaByPrice.TryGetValue(p, out long ld))
					legDeltaByPrice[p] = ld + signed;
				else
					legDeltaByPrice[p] = signed;
			}

			if (sessionStartBar >= 0 && primaryIndex >= sessionStartBar)
			{
				if (sessionDeltaByPrice.TryGetValue(p, out long sd))
					sessionDeltaByPrice[p] = sd + signed;
				else
					sessionDeltaByPrice[p] = signed;
			}
		}

		private void RebuildLegFromStart(int startBar)
		{
			legDeltaByPrice.Clear();
			legVolByPrice.Clear();

			int end   = BarsArray[0].Count - 1;
			int start = Math.Max(0, startBar);

			if (end - start > RebuildLookbackBarsCap)
				start = end - RebuildLookbackBarsCap;

			for (int b = start; b <= end; b++)
			{
				if (b < 0) continue;
				if (b >= barDeltaMaps.Count) continue;
				if (b >= barVolMaps.Count) continue;

				foreach (var kv in barDeltaMaps[b])
				{
					if (legDeltaByPrice.TryGetValue(kv.Key, out long existing))
						legDeltaByPrice[kv.Key] = existing + kv.Value;
					else
						legDeltaByPrice[kv.Key] = kv.Value;
				}

				foreach (var kv in barVolMaps[b])
				{
					if (legVolByPrice.TryGetValue(kv.Key, out long existing))
						legVolByPrice[kv.Key] = existing + kv.Value;
					else
						legVolByPrice[kv.Key] = kv.Value;
				}
			}
		}

		private void RebuildSessionFrom(int startBar)
		{
			sessionDeltaByPrice.Clear();
			sessionVolByPrice.Clear();

			int end   = BarsArray[0].Count - 1;
			int start = Math.Max(0, startBar);

			if (end - start > RebuildLookbackBarsCap)
				start = end - RebuildLookbackBarsCap;

			for (int b = start; b <= end; b++)
			{
				if (b < 0) continue;
				if (b >= barDeltaMaps.Count) continue;
				if (b >= barVolMaps.Count) continue;

				foreach (var kv in barDeltaMaps[b])
				{
					if (sessionDeltaByPrice.TryGetValue(kv.Key, out long existing))
						sessionDeltaByPrice[kv.Key] = existing + kv.Value;
					else
						sessionDeltaByPrice[kv.Key] = kv.Value;
				}

				foreach (var kv in barVolMaps[b])
				{
					if (sessionVolByPrice.TryGetValue(kv.Key, out long existing))
						sessionVolByPrice[kv.Key] = existing + kv.Value;
					else
						sessionVolByPrice[kv.Key] = kv.Value;
				}
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);

			var activeDelta = (ProfileMode == ProfileModes.SessionCurrentDay) ? sessionDeltaByPrice : legDeltaByPrice;
			var activeVol   = (ProfileMode == ProfileModes.SessionCurrentDay) ? sessionVolByPrice   : legVolByPrice;

			bool haveDelta = activeDelta != null && activeDelta.Count > 0;
			bool haveVol   = ShowVolumeProfile && activeVol != null && activeVol.Count > 0;

			if (!haveDelta && !haveVol)
				return;

			EnsureDxResources(); // this now handles auto font too

			// Base spine position
			float reservedPx = (float)(ChartPanel.W * RightReservedPercent);
			float spineXBase = chartControl.CanvasRight - RightMarginPx - reservedPx - XOffsetPx;

			float panelTop    = ChartPanel.Y;
			float panelBottom = ChartPanel.Y + ChartPanel.H;

			if (ShowSpine)
			{
				var spineRect = new RectangleF(spineXBase - SpineWidthPx, panelTop, SpineWidthPx, panelBottom - panelTop);
				RenderTarget.FillRectangle(spineRect, spineBrushDx);
			}

			// Determine X anchors for Delta and Volume
			float deltaSpineX = spineXBase;
			float volSpineX   = spineXBase;

			if (haveVol && (VolumeLayer == VolumeProfileLayer.LeftOfDelta || VolumeLayer == VolumeProfileLayer.RightOfDelta))
			{
				if (VolumeLayer == VolumeProfileLayer.LeftOfDelta)
				{
					deltaSpineX = spineXBase;
					volSpineX   = spineXBase - MaxProfileWidthPx - SideBySideGapPx;
				}
				else // RightOfDelta
				{
					volSpineX   = spineXBase;
					deltaSpineX = spineXBase - VolumeProfileWidthPx - SideBySideGapPx;
				}
			}

			if (haveVol && VolumeLayer == VolumeProfileLayer.BehindDelta)
				RenderVolumeProfile(chartScale, volSpineX, activeVol);

			if (haveDelta)
				RenderDeltaProfile(chartScale, deltaSpineX, activeDelta);

			if (haveVol && VolumeLayer == VolumeProfileLayer.InFrontOfDelta)
				RenderVolumeProfile(chartScale, volSpineX, activeVol);

			if (haveVol && (VolumeLayer == VolumeProfileLayer.LeftOfDelta || VolumeLayer == VolumeProfileLayer.RightOfDelta))
			{
				RenderVolumeProfile(chartScale, volSpineX, activeVol);
				if (haveDelta)
					RenderDeltaProfile(chartScale, deltaSpineX, activeDelta);
			}
		}

		private void RenderDeltaProfile(ChartScale chartScale, float spineX, Dictionary<double, long> activeMap)
		{
			// Determine grouping for this zoom (delta only)
			float tickPx = Math.Abs(chartScale.GetYByValue(TickSize) - chartScale.GetYByValue(0));
			int groupTicks = 1;

			if (tickPx > 0 && tickPx < MinRowHeightPx)
				groupTicks = (int)Math.Ceiling(MinRowHeightPx / tickPx);

			long maxAbsBin = ComputeMaxAbsBin(activeMap, groupTicks);
			if (maxAbsBin < MinAbsDeltaToShow)
				return;

			double maxPrice = Instrument.MasterInstrument.RoundToTickSize(activeMap.Keys.Max());
			double minPrice = Instrument.MasterInstrument.RoundToTickSize(activeMap.Keys.Min());

			float approxRowHeightPx = Math.Abs(chartScale.GetYByValue(maxPrice) - chartScale.GetYByValue(maxPrice - TickSize * groupTicks));
			int effectiveFont = GetEffectiveFontSizeFromRowHeight(approxRowHeightPx);

			EnsureTextFormat(effectiveFont);

			double pTop = maxPrice;
			while (pTop >= minPrice - TickSize * 0.5)
			{
				long binDelta = 0;

				for (int i = 0; i < groupTicks; i++)
				{
					double p = pTop - (TickSize * i);
					if (activeMap.TryGetValue(p, out long d))
						binDelta += d;
				}

				long absD = Math.Abs(binDelta);
				if (absD >= MinAbsDeltaToShow)
				{
					float yTop = chartScale.GetYByValue(pTop);
					float yBot = chartScale.GetYByValue(pTop - TickSize * groupTicks);
					float height = Math.Abs(yBot - yTop);
					if (height < 2f) height = 2f;

					float width = (float)(MaxProfileWidthPx * (absD / (double)maxAbsBin));

					if (width > 0.5f)
					{
						float top = Math.Min(yTop, yBot);
						var rect = new RectangleF(spineX - width, top, width, height);

						RenderTarget.FillRectangle(rect, binDelta >= 0 ? posBrushDx : negBrushDx);

						if (effectiveFont > 0)
						{
							string txt = binDelta.ToString();

							if (height >= (effectiveFont + 2))
							{
								float textWidth = MeasureTextWidth(txt);

								if (rect.Width >= textWidth + 6f)
									RenderTarget.DrawText(txt, textFormat, rect, textBrushDx);
							}
						}
					}
				}

				pTop -= TickSize * groupTicks;
			}
		}

		private long ComputeMaxAbsBin(Dictionary<double, long> activeMap, int groupTicks)
		{
			if (activeMap == null || activeMap.Count == 0)
				return 0;

			double maxPrice = Instrument.MasterInstrument.RoundToTickSize(activeMap.Keys.Max());
			double minPrice = Instrument.MasterInstrument.RoundToTickSize(activeMap.Keys.Min());

			long maxAbsBin = 0;

			double pTop = maxPrice;
			while (pTop >= minPrice - TickSize * 0.5)
			{
				long binDelta = 0;

				for (int i = 0; i < groupTicks; i++)
				{
					double p = pTop - (TickSize * i);
					if (activeMap.TryGetValue(p, out long d))
						binDelta += d;
				}

				long absD = Math.Abs(binDelta);
				if (absD > maxAbsBin)
					maxAbsBin = absD;

				pTop -= TickSize * groupTicks;
			}

			return maxAbsBin;
		}

		private int GetEffectiveFontSizeFromRowHeight(float rowHeightPx)
		{
			if (!AutoDeltaText)
				return FontSize;

			// If rows are tiny, hide text entirely
			if (rowHeightPx < 6f)
				return 0;

			// heuristic: text should be ~70% of row height
			int f = (int)Math.Floor(rowHeightPx * 0.70f);

			if (f < AutoFontMin)
				f = AutoFontMin;

			if (f > AutoFontMax)
				f = AutoFontMax;

			if (rowHeightPx < (f + 2))
				return 0;

			return f;
		}

		private void EnsureTextFormat(int effectiveFont)
		{
			if (effectiveFont <= 0)
				return;

			if (textFormat == null || lastFontSize != effectiveFont)
			{
				textFormat?.Dispose();
				textFormat = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", effectiveFont)
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Center,
					ParagraphAlignment = ParagraphAlignment.Center
				};
				lastFontSize = effectiveFont;
			}
		}

		private void RenderVolumeProfile(ChartScale chartScale, float spineX, Dictionary<double, long> volMap)
		{
			long maxVol = 0;
			foreach (var kv in volMap)
				maxVol = Math.Max(maxVol, kv.Value);

			if (maxVol <= 0)
				return;

			double maxPrice = Instrument.MasterInstrument.RoundToTickSize(volMap.Keys.Max());
			double minPrice = Instrument.MasterInstrument.RoundToTickSize(volMap.Keys.Min());

			double pTop = maxPrice;
			while (pTop >= minPrice - TickSize * 0.5)
			{
				if (volMap.TryGetValue(pTop, out long v) && v > 0)
				{
					float yTop = chartScale.GetYByValue(pTop);
					float yBot = chartScale.GetYByValue(pTop - TickSize);
					float height = Math.Abs(yBot - yTop);
					if (height < 1f) height = 1f;

					float width = (float)(VolumeProfileWidthPx * (v / (double)maxVol));

					if (width > 0.5f)
					{
						float top = Math.Min(yTop, yBot);
						var rect = new RectangleF(spineX - width, top, width, height);
						RenderTarget.FillRectangle(rect, volBrushDx);
					}
				}

				pTop -= TickSize;
			}
		}

		private void EnsureDxResources()
		{
			string posSer = SafeBrushSerialize(PositiveBrush);
			string negSer = SafeBrushSerialize(NegativeBrush);
			string txtSer = SafeBrushSerialize(TextBrush);
			string volSer = SafeBrushSerialize(VolumeBrush);

			bool needsRebuild =
				posBrushDx == null || negBrushDx == null || textBrushDx == null || spineBrushDx == null || volBrushDx == null
				|| lastPosBrushSer != posSer
				|| lastNegBrushSer != negSer
				|| lastTextBrushSer != txtSer
				|| lastVolBrushSer != volSer
				|| Math.Abs(lastDeltaOpacity - DeltaOpacity) > 0.0001f
				|| Math.Abs(lastVolOpacity - VolumeOpacity) > 0.0001f;

			if (!needsRebuild)
				return;

			try
			{
				posBrushDx?.Dispose();
				negBrushDx?.Dispose();
				textBrushDx?.Dispose();
				spineBrushDx?.Dispose();
				volBrushDx?.Dispose();
			}
			catch { }

			posBrushDx   = new SolidColorBrush(RenderTarget, ToDx(PositiveBrush, DeltaOpacity));
			negBrushDx   = new SolidColorBrush(RenderTarget, ToDx(NegativeBrush, DeltaOpacity));
			textBrushDx  = new SolidColorBrush(RenderTarget, ToDx(TextBrush, 1f));
			spineBrushDx = new SolidColorBrush(RenderTarget, new Color4(1f, 1f, 1f, 0.25f));
			volBrushDx   = new SolidColorBrush(RenderTarget, ToDx(VolumeBrush, VolumeOpacity));

			lastPosBrushSer = posSer;
			lastNegBrushSer = negSer;
			lastTextBrushSer = txtSer;
			lastVolBrushSer = volSer;
			lastDeltaOpacity = DeltaOpacity;
			lastVolOpacity = VolumeOpacity;
		}

		private string SafeBrushSerialize(WpfBrush b)
		{
			try { return Serialize.BrushToString(b); }
			catch { return b?.ToString() ?? ""; }
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
		}

		private float MeasureTextWidth(string text)
		{
			if (textFormat == null)
				return 0f;

			using (var layout = new TextLayout(
				Core.Globals.DirectWriteFactory,
				text,
				textFormat,
				1000,
				100))
			{
				return layout.Metrics.Width;
			}
		}

		private static System.Windows.Media.Color BrushToMediaColor(WpfBrush b)
		{
			if (b is WpfSolidColorBrush sb)
				return sb.Color;
			return WpfColors.White;
		}

		private Color4 ToDx(WpfBrush b, float alphaMult)
		{
			var c = BrushToMediaColor(b ?? WpfBrushes.White);
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * alphaMult);
		}

		#region Properties

		[NinjaScriptProperty]
		[Display(Name = "Profile Mode", Order = 0, GroupName = "Mode")]
		public ProfileModes ProfileMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Rotation Mode", Order=0, GroupName="Leg Detection",
			Description="DistanceFromExtreme = new profile when price is X away from leg high/low. TrueSwingAlternate = only on reversal X, alternates direction (new leg starts at swing extreme bar).")]
		public LegRotationMode RotationMode { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 5000.0)]
		[Display(Name = "Rotation (Points)", Order = 1, GroupName = "Leg Detection",
			Description = "Start a new leg profile when price rotates by this many POINTS. Set to 0 to disable.")]
		public double RotationPoints { get; set; }

		// ===== Delta rendering =====
		[NinjaScriptProperty]
		[Range(20, 600)]
		[Display(Name = "Max Delta Width (px)", Order = 2, GroupName = "Delta Rendering")]
		public int MaxProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 1000000)]
		[Display(Name = "Min Abs Delta To Show", Order = 3, GroupName = "Delta Rendering")]
		public long MinAbsDeltaToShow { get; set; }

		[NinjaScriptProperty]
		[Range(200, 50000)]
		[Display(Name = "Rebuild Lookback Cap (bars)", Order = 4, GroupName = "Performance")]
		public int RebuildLookbackBarsCap { get; set; }

		// ===== Auto text settings =====
		[NinjaScriptProperty]
		[Display(Name="Auto Delta Text", Order=5, GroupName="Delta Rendering",
			Description="When enabled, font size (and whether text is shown) is automatically derived from current row height / zoom.")]
		public bool AutoDeltaText { get; set; }

		[NinjaScriptProperty]
		[Range(6, 30)]
		[Display(Name="Auto Font Min", Order=6, GroupName="Delta Rendering")]
		public int AutoFontMin { get; set; }

		[NinjaScriptProperty]
		[Range(6, 40)]
		[Display(Name="Auto Font Max", Order=7, GroupName="Delta Rendering")]
		public int AutoFontMax { get; set; }

		[NinjaScriptProperty]
		[Range(8, 28)]
		[Display(Name="Manual Font Size", Order=8, GroupName="Delta Rendering",
			Description="Used only when Auto Delta Text = false.")]
		public int FontSize { get; set; }

		[NinjaScriptProperty]
		[Range(2, 30)]
		[Display(Name="Min Row Height (px)", Order=9, GroupName="Delta Rendering",
			Description="Auto-groups ticks per row when zoomed out to avoid overlap (DELTA only).")]
		public int MinRowHeightPx { get; set; }

		// ===== Delta Colors =====
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Positive Brush", Order = 10, GroupName = "Delta Colors")]
		public WpfBrush PositiveBrush { get; set; }

		[Browsable(false)]
		public string PositiveBrushSerialize
		{
			get { return Serialize.BrushToString(PositiveBrush); }
			set { PositiveBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Negative Brush", Order = 11, GroupName = "Delta Colors")]
		public WpfBrush NegativeBrush { get; set; }

		[Browsable(false)]
		public string NegativeBrushSerialize
		{
			get { return Serialize.BrushToString(NegativeBrush); }
			set { NegativeBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Text Brush", Order = 12, GroupName = "Delta Colors")]
		public WpfBrush TextBrush { get; set; }

		[Browsable(false)]
		public string TextBrushSerialize
		{
			get { return Serialize.BrushToString(TextBrush); }
			set { TextBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0.1, 1.0)]
		[Display(Name = "Delta Opacity", Order = 13, GroupName = "Delta Rendering")]
		public float DeltaOpacity { get; set; }

		// ===== Shared rendering / positioning =====
		[NinjaScriptProperty]
		[Display(Name = "Show Spine", Order = 0, GroupName = "Rendering")]
		public bool ShowSpine { get; set; }

		[NinjaScriptProperty]
		[Range(1, 6)]
		[Display(Name = "Spine Width (px)", Order = 1, GroupName = "Rendering")]
		public int SpineWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(0, 120)]
		[Display(Name = "Right Margin (px)", Order = 2, GroupName = "Rendering")]
		public int RightMarginPx { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 0.8)]
		[Display(Name = "Right Reserved %", Order = 3, GroupName = "Rendering")]
		public double RightReservedPercent { get; set; }

		[NinjaScriptProperty]
		[Range(-500, 500)]
		[Display(Name = "X Offset (px)", Order = 4, GroupName = "Rendering")]
		public int XOffsetPx { get; set; }

		// ===== Volume Profile Overlay =====
		[NinjaScriptProperty]
		[Display(Name = "Show Volume Profile", Order = 0, GroupName = "Volume Profile")]
		public bool ShowVolumeProfile { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Volume Placement", Order = 1, GroupName = "Volume Profile")]
		public VolumeProfileLayer VolumeLayer { get; set; }

		[NinjaScriptProperty]
		[Range(20, 600)]
		[Display(Name = "Volume Width (px)", Order = 2, GroupName = "Volume Profile")]
		public int VolumeProfileWidthPx { get; set; }

		[NinjaScriptProperty]
		[Range(0.05, 1.0)]
		[Display(Name = "Volume Opacity", Order = 3, GroupName = "Volume Profile")]
		public float VolumeOpacity { get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Volume Brush", Order = 4, GroupName = "Volume Profile")]
		public WpfBrush VolumeBrush { get; set; }

		[Browsable(false)]
		public string VolumeBrushSerialize
		{
			get { return Serialize.BrushToString(VolumeBrush); }
			set { VolumeBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name = "Side-by-Side Gap (px)", Order = 5, GroupName = "Volume Profile")]
		public int SideBySideGapPx { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private LegToLegDeltaProfile[] cacheLegToLegDeltaProfile;
		public LegToLegDeltaProfile LegToLegDeltaProfile(ProfileModes profileMode, LegRotationMode rotationMode, double rotationPoints, int maxProfileWidthPx, long minAbsDeltaToShow, int rebuildLookbackBarsCap, bool autoDeltaText, int autoFontMin, int autoFontMax, int fontSize, int minRowHeightPx, WpfBrush positiveBrush, WpfBrush negativeBrush, WpfBrush textBrush, float deltaOpacity, bool showSpine, int spineWidthPx, int rightMarginPx, double rightReservedPercent, int xOffsetPx, bool showVolumeProfile, VolumeProfileLayer volumeLayer, int volumeProfileWidthPx, float volumeOpacity, WpfBrush volumeBrush, int sideBySideGapPx)
		{
			return LegToLegDeltaProfile(Input, profileMode, rotationMode, rotationPoints, maxProfileWidthPx, minAbsDeltaToShow, rebuildLookbackBarsCap, autoDeltaText, autoFontMin, autoFontMax, fontSize, minRowHeightPx, positiveBrush, negativeBrush, textBrush, deltaOpacity, showSpine, spineWidthPx, rightMarginPx, rightReservedPercent, xOffsetPx, showVolumeProfile, volumeLayer, volumeProfileWidthPx, volumeOpacity, volumeBrush, sideBySideGapPx);
		}

		public LegToLegDeltaProfile LegToLegDeltaProfile(ISeries<double> input, ProfileModes profileMode, LegRotationMode rotationMode, double rotationPoints, int maxProfileWidthPx, long minAbsDeltaToShow, int rebuildLookbackBarsCap, bool autoDeltaText, int autoFontMin, int autoFontMax, int fontSize, int minRowHeightPx, WpfBrush positiveBrush, WpfBrush negativeBrush, WpfBrush textBrush, float deltaOpacity, bool showSpine, int spineWidthPx, int rightMarginPx, double rightReservedPercent, int xOffsetPx, bool showVolumeProfile, VolumeProfileLayer volumeLayer, int volumeProfileWidthPx, float volumeOpacity, WpfBrush volumeBrush, int sideBySideGapPx)
		{
			if (cacheLegToLegDeltaProfile != null)
				for (int idx = 0; idx < cacheLegToLegDeltaProfile.Length; idx++)
					if (cacheLegToLegDeltaProfile[idx] != null && cacheLegToLegDeltaProfile[idx].ProfileMode == profileMode && cacheLegToLegDeltaProfile[idx].RotationMode == rotationMode && cacheLegToLegDeltaProfile[idx].RotationPoints == rotationPoints && cacheLegToLegDeltaProfile[idx].MaxProfileWidthPx == maxProfileWidthPx && cacheLegToLegDeltaProfile[idx].MinAbsDeltaToShow == minAbsDeltaToShow && cacheLegToLegDeltaProfile[idx].RebuildLookbackBarsCap == rebuildLookbackBarsCap && cacheLegToLegDeltaProfile[idx].AutoDeltaText == autoDeltaText && cacheLegToLegDeltaProfile[idx].AutoFontMin == autoFontMin && cacheLegToLegDeltaProfile[idx].AutoFontMax == autoFontMax && cacheLegToLegDeltaProfile[idx].FontSize == fontSize && cacheLegToLegDeltaProfile[idx].MinRowHeightPx == minRowHeightPx && cacheLegToLegDeltaProfile[idx].PositiveBrush == positiveBrush && cacheLegToLegDeltaProfile[idx].NegativeBrush == negativeBrush && cacheLegToLegDeltaProfile[idx].TextBrush == textBrush && cacheLegToLegDeltaProfile[idx].DeltaOpacity == deltaOpacity && cacheLegToLegDeltaProfile[idx].ShowSpine == showSpine && cacheLegToLegDeltaProfile[idx].SpineWidthPx == spineWidthPx && cacheLegToLegDeltaProfile[idx].RightMarginPx == rightMarginPx && cacheLegToLegDeltaProfile[idx].RightReservedPercent == rightReservedPercent && cacheLegToLegDeltaProfile[idx].XOffsetPx == xOffsetPx && cacheLegToLegDeltaProfile[idx].ShowVolumeProfile == showVolumeProfile && cacheLegToLegDeltaProfile[idx].VolumeLayer == volumeLayer && cacheLegToLegDeltaProfile[idx].VolumeProfileWidthPx == volumeProfileWidthPx && cacheLegToLegDeltaProfile[idx].VolumeOpacity == volumeOpacity && cacheLegToLegDeltaProfile[idx].VolumeBrush == volumeBrush && cacheLegToLegDeltaProfile[idx].SideBySideGapPx == sideBySideGapPx && cacheLegToLegDeltaProfile[idx].EqualsInput(input))
						return cacheLegToLegDeltaProfile[idx];
			return CacheIndicator<LegToLegDeltaProfile>(new LegToLegDeltaProfile(){ ProfileMode = profileMode, RotationMode = rotationMode, RotationPoints = rotationPoints, MaxProfileWidthPx = maxProfileWidthPx, MinAbsDeltaToShow = minAbsDeltaToShow, RebuildLookbackBarsCap = rebuildLookbackBarsCap, AutoDeltaText = autoDeltaText, AutoFontMin = autoFontMin, AutoFontMax = autoFontMax, FontSize = fontSize, MinRowHeightPx = minRowHeightPx, PositiveBrush = positiveBrush, NegativeBrush = negativeBrush, TextBrush = textBrush, DeltaOpacity = deltaOpacity, ShowSpine = showSpine, SpineWidthPx = spineWidthPx, RightMarginPx = rightMarginPx, RightReservedPercent = rightReservedPercent, XOffsetPx = xOffsetPx, ShowVolumeProfile = showVolumeProfile, VolumeLayer = volumeLayer, VolumeProfileWidthPx = volumeProfileWidthPx, VolumeOpacity = volumeOpacity, VolumeBrush = volumeBrush, SideBySideGapPx = sideBySideGapPx }, input, ref cacheLegToLegDeltaProfile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.LegToLegDeltaProfile LegToLegDeltaProfile(ProfileModes profileMode, LegRotationMode rotationMode, double rotationPoints, int maxProfileWidthPx, long minAbsDeltaToShow, int rebuildLookbackBarsCap, bool autoDeltaText, int autoFontMin, int autoFontMax, int fontSize, int minRowHeightPx, WpfBrush positiveBrush, WpfBrush negativeBrush, WpfBrush textBrush, float deltaOpacity, bool showSpine, int spineWidthPx, int rightMarginPx, double rightReservedPercent, int xOffsetPx, bool showVolumeProfile, VolumeProfileLayer volumeLayer, int volumeProfileWidthPx, float volumeOpacity, WpfBrush volumeBrush, int sideBySideGapPx)
		{
			return indicator.LegToLegDeltaProfile(Input, profileMode, rotationMode, rotationPoints, maxProfileWidthPx, minAbsDeltaToShow, rebuildLookbackBarsCap, autoDeltaText, autoFontMin, autoFontMax, fontSize, minRowHeightPx, positiveBrush, negativeBrush, textBrush, deltaOpacity, showSpine, spineWidthPx, rightMarginPx, rightReservedPercent, xOffsetPx, showVolumeProfile, volumeLayer, volumeProfileWidthPx, volumeOpacity, volumeBrush, sideBySideGapPx);
		}

		public Indicators.LegToLegDeltaProfile LegToLegDeltaProfile(ISeries<double> input , ProfileModes profileMode, LegRotationMode rotationMode, double rotationPoints, int maxProfileWidthPx, long minAbsDeltaToShow, int rebuildLookbackBarsCap, bool autoDeltaText, int autoFontMin, int autoFontMax, int fontSize, int minRowHeightPx, WpfBrush positiveBrush, WpfBrush negativeBrush, WpfBrush textBrush, float deltaOpacity, bool showSpine, int spineWidthPx, int rightMarginPx, double rightReservedPercent, int xOffsetPx, bool showVolumeProfile, VolumeProfileLayer volumeLayer, int volumeProfileWidthPx, float volumeOpacity, WpfBrush volumeBrush, int sideBySideGapPx)
		{
			return indicator.LegToLegDeltaProfile(input, profileMode, rotationMode, rotationPoints, maxProfileWidthPx, minAbsDeltaToShow, rebuildLookbackBarsCap, autoDeltaText, autoFontMin, autoFontMax, fontSize, minRowHeightPx, positiveBrush, negativeBrush, textBrush, deltaOpacity, showSpine, spineWidthPx, rightMarginPx, rightReservedPercent, xOffsetPx, showVolumeProfile, volumeLayer, volumeProfileWidthPx, volumeOpacity, volumeBrush, sideBySideGapPx);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.LegToLegDeltaProfile LegToLegDeltaProfile(ProfileModes profileMode, LegRotationMode rotationMode, double rotationPoints, int maxProfileWidthPx, long minAbsDeltaToShow, int rebuildLookbackBarsCap, bool autoDeltaText, int autoFontMin, int autoFontMax, int fontSize, int minRowHeightPx, WpfBrush positiveBrush, WpfBrush negativeBrush, WpfBrush textBrush, float deltaOpacity, bool showSpine, int spineWidthPx, int rightMarginPx, double rightReservedPercent, int xOffsetPx, bool showVolumeProfile, VolumeProfileLayer volumeLayer, int volumeProfileWidthPx, float volumeOpacity, WpfBrush volumeBrush, int sideBySideGapPx)
		{
			return indicator.LegToLegDeltaProfile(Input, profileMode, rotationMode, rotationPoints, maxProfileWidthPx, minAbsDeltaToShow, rebuildLookbackBarsCap, autoDeltaText, autoFontMin, autoFontMax, fontSize, minRowHeightPx, positiveBrush, negativeBrush, textBrush, deltaOpacity, showSpine, spineWidthPx, rightMarginPx, rightReservedPercent, xOffsetPx, showVolumeProfile, volumeLayer, volumeProfileWidthPx, volumeOpacity, volumeBrush, sideBySideGapPx);
		}

		public Indicators.LegToLegDeltaProfile LegToLegDeltaProfile(ISeries<double> input , ProfileModes profileMode, LegRotationMode rotationMode, double rotationPoints, int maxProfileWidthPx, long minAbsDeltaToShow, int rebuildLookbackBarsCap, bool autoDeltaText, int autoFontMin, int autoFontMax, int fontSize, int minRowHeightPx, WpfBrush positiveBrush, WpfBrush negativeBrush, WpfBrush textBrush, float deltaOpacity, bool showSpine, int spineWidthPx, int rightMarginPx, double rightReservedPercent, int xOffsetPx, bool showVolumeProfile, VolumeProfileLayer volumeLayer, int volumeProfileWidthPx, float volumeOpacity, WpfBrush volumeBrush, int sideBySideGapPx)
		{
			return indicator.LegToLegDeltaProfile(input, profileMode, rotationMode, rotationPoints, maxProfileWidthPx, minAbsDeltaToShow, rebuildLookbackBarsCap, autoDeltaText, autoFontMin, autoFontMax, fontSize, minRowHeightPx, positiveBrush, negativeBrush, textBrush, deltaOpacity, showSpine, spineWidthPx, rightMarginPx, rightReservedPercent, xOffsetPx, showVolumeProfile, volumeLayer, volumeProfileWidthPx, volumeOpacity, volumeBrush, sideBySideGapPx);
		}
	}
}

#endregion

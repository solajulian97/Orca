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

using WpfBrush = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfColors = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	internal enum OrcaLegDirection { Unknown = 0, Up = 1, Down = -1 }

	public class OrcaLegtoLegProfile : Indicator
	{
		private class PriceLeg
		{
			public object SyncObj = new object();
			public int StartIndex;
			public int EndIndex;
			public DateTime StartTime;
			public DateTime EndTime;
			public double HighPrice;
			public double LowPrice;
			public OrcaLegDirection Direction;
			public Dictionary<double, long> VolByPrice = new Dictionary<double, long>();
			public Dictionary<double, long> DeltaByPrice = new Dictionary<double, long>();
			public bool IsVACalculated = false;
			public double POCPrice = double.NaN;
			public double VAHPrice = double.NaN;
			public double VALPrice = double.NaN;
			public long MaxVol = 0;
			public int LastVolComp = -1;
		}

		private LegTracker currentTracker;
		private LegTracker pastTracker;
		private ATR atrIndicator;
		private int lastDynamicDeltaComp = -1;
		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private double prevLast = double.NaN;

		private TextFormat textFormat;
		private SolidColorBrush posBrushDx, negBrushDx, textBrushDx, volBrushDx, labelBgBrushDx, legBoxBrushDx;
		private SolidColorBrush pocBrushDx, vaVolBrushDx, vaLineBrushDx;
		private StrokeStyle vaLineStrokeDx;
		private SolidColorBrush[] volGradientBrushes;
		private SolidColorBrush[] vaGradientBrushes;
		private int lastBuiltGradientSteps = -1;
		private int lastBuiltVAGradientSteps = -1;
		private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "OrcaLegtoLegProfile";
				Description = "Rotation-based leg delta/volume profile with Value Area, POC and gradient support.";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;

				ReversalTicks = 20;
				PastReversalTicks = 40;
				UseAtrReversal = false;
				AtrPeriod = 14;
				AtrMultiplier = 1.0;
				PastAtrMultiplier = 2.0;
				MinimumBarsPerLeg = 1;
				MinimumDurationMinutes = 0;
				LegsToDisplay = 3;
				UseDynamicAggregation = false;
				DynamicAggregationMultiplier = 1.0;
				VolumeTickCompression = 6;
				DeltaTickCompression = 6;
				VolumeProfileWidthPx = 150;
				DeltaProfileWidthPx = 100;
				PastVolumeWidthPx = 60;
				PastDeltaWidthPx = 40;
				RightOffsetPx = 60;
				ProfileSeparationPx = 20;
				ProfileBarSpacingPx = 0;
				ShowVolume = true;
				ShowDelta = true;
				ShowPastDelta = true;
				ShowCurrentLegBox = false;
				MirrorProfile = false;

				DeltaLabelFontSize = 10;
				ShowDeltaLabelBackground = true;
				VolumeOpacity = 0.6f;
				DeltaOpacity = 0.85f;

				PositiveBrush = WpfBrushes.Lime;
				NegativeBrush = WpfBrushes.Red;
				VolumeBrush = WpfBrushes.RoyalBlue;
				TextBrush = WpfBrushes.White;
				LabelBgBrush = WpfBrushes.Black;
				LegBoxBrush = WpfBrushes.Yellow;

				ShowPOC = true;
				POCBrush = WpfBrushes.DodgerBlue;
				UseGradient = true;
				GradientSteps = 16;
				MinBrightness = 0.20f;

				ShowValueArea = true;
				ShowVAColor = true;
				ShowVALines = true;
				ValueAreaPercent = 70;
				VALineThickness = 1.5f;
				VALineStyle = VALineStyleEnum.Dash;
				VABrush = WpfBrushes.CornflowerBlue;
				VALineBrush = WpfBrushes.White;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
				currentTracker = new LegTracker(this, ReversalTicks, AtrMultiplier);
				pastTracker = new LegTracker(this, PastReversalTicks > 0 ? PastReversalTicks : ReversalTicks, PastAtrMultiplier);
			}
			else if (State == State.DataLoaded)
			{
				if (UseAtrReversal)
				{
					atrIndicator = ATR(AtrPeriod);
				}
			}
			else if (State == State.Terminated) DisposeDx();
		}

		private void DisposeDx()
		{
			try
			{
				textFormat?.Dispose();
				posBrushDx?.Dispose();
				negBrushDx?.Dispose();
				textBrushDx?.Dispose();
				volBrushDx?.Dispose();
				labelBgBrushDx?.Dispose();
				legBoxBrushDx?.Dispose();
				pocBrushDx?.Dispose();
				vaVolBrushDx?.Dispose();
				vaLineBrushDx?.Dispose();
				vaLineStrokeDx?.Dispose();
				if (volGradientBrushes != null) foreach (var b in volGradientBrushes) b?.Dispose();
				if (vaGradientBrushes != null) foreach (var b in vaGradientBrushes) b?.Dispose();
			}
			catch { }
			finally
			{
				textFormat = null; posBrushDx = null; negBrushDx = null; textBrushDx = null;
				volBrushDx = null; labelBgBrushDx = null; legBoxBrushDx = null;
				pocBrushDx = null; vaVolBrushDx = null; vaLineBrushDx = null; vaLineStrokeDx = null;
				volGradientBrushes = null; vaGradientBrushes = null;
				lastBuiltGradientSteps = -1; lastBuiltVAGradientSteps = -1;
				textWidthCache.Clear();
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
		}

		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Bid) lastBid = e.Price;
			else if (e.MarketDataType == MarketDataType.Ask) lastAsk = e.Price;
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				if (CurrentBar < 1) return;
				double last = Close[0];
				long vol = (long)Volume[0];
				DateTime time = Time[0];
				int primaryBarIndex = Math.Max(0, BarsArray[0].Count - 1);

				long signedVol = 0;
				if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
				{
					if (last >= lastAsk) signedVol = +vol;
					else if (last <= lastBid) signedVol = -vol;
					else if (!double.IsNaN(prevLast)) signedVol = (last > prevLast) ? +vol : (last < prevLast ? -vol : 0);
				}
				else if (!double.IsNaN(prevLast)) signedVol = (last > prevLast) ? +vol : (last < prevLast ? -vol : 0);
				prevLast = last;

				if (vol > 0)
				{
					currentTracker.ProcessBarUpdate(last, vol, signedVol, time, primaryBarIndex);
					pastTracker.ProcessBarUpdate(last, vol, signedVol, time, primaryBarIndex);
					// Removed ForceRefresh() to fix UI Thread lagging
				}
			}
			else if (BarsInProgress == 0)
			{
				if (currentTracker?.CurrentLeg != null && CurrentBar > 0) currentTracker.CurrentLeg.EndIndex = CurrentBar;
				if (pastTracker?.CurrentLeg != null && CurrentBar > 0) pastTracker.CurrentLeg.EndIndex = CurrentBar;
			}
		}

		private class LegTracker
		{
			private OrcaLegtoLegProfile parent;
			public int TickReversalThreshold;
			public List<PriceLeg> CompletedLegs = new List<PriceLeg>();
			public PriceLeg CurrentLeg;

			private double currentExtremePrice = double.NaN;
			private int currentExtremeBar = -1;
			private DateTime currentExtremeTime;
			private OrcaLegDirection legDir = OrcaLegDirection.Unknown;

			private struct TickRecord { public double Price; public long Volume; public long SignedVolume; public DateTime Time; }
			private List<TickRecord> ticksSinceExtreme = new List<TickRecord>();

			public double AtrMultiplier;

			public LegTracker(OrcaLegtoLegProfile indicator, int reversalTicks, double atrMultiplier)
			{
				parent = indicator;
				TickReversalThreshold = reversalTicks;
				AtrMultiplier = atrMultiplier;
			}

			public void ProcessBarUpdate(double last, long vol, long signedVol, DateTime time, int primaryBarIndex)
			{
				if (CurrentLeg == null)
				{
					StartNewLegAtCurrentTick(OrcaLegDirection.Up, last, time, primaryBarIndex);
					return;
				}

				bool newExtremeFound = false;
				if (legDir == OrcaLegDirection.Up || legDir == OrcaLegDirection.Unknown)
				{
					if (double.IsNaN(currentExtremePrice) || last >= currentExtremePrice)
					{
						currentExtremePrice = last; currentExtremeTime = time; newExtremeFound = true;
					}
				}
				if (legDir == OrcaLegDirection.Down || legDir == OrcaLegDirection.Unknown)
				{
					if (double.IsNaN(currentExtremePrice) || last <= currentExtremePrice)
					{
						currentExtremePrice = last; currentExtremeTime = time; newExtremeFound = true;
					}
				}

				if (newExtremeFound) ticksSinceExtreme.Clear();
				ticksSinceExtreme.Add(new TickRecord { Price = last, Volume = vol, SignedVolume = signedVol, Time = time });

				if (!newExtremeFound)
				{
					double reversalThreshold = (TickReversalThreshold * parent.TickSize);
					if (parent.UseAtrReversal && parent.atrIndicator != null && parent.CurrentBars[0] >= parent.AtrPeriod)
					{
						try
						{
							double atrVal = parent.atrIndicator[0];
							if (atrVal > 0)
							{
								reversalThreshold = atrVal * AtrMultiplier;
							}
						}
						catch { }
					}

					reversalThreshold = Math.Max(reversalThreshold, parent.TickSize);

					bool durationMet = parent.MinimumDurationMinutes == 0 || (time - CurrentLeg.StartTime).TotalMinutes >= parent.MinimumDurationMinutes;
					if (durationMet)
					{
						if (legDir == OrcaLegDirection.Up && (currentExtremePrice - last) >= reversalThreshold)
						{
							HandleReversalTick(OrcaLegDirection.Down, last, time, primaryBarIndex); return;
						}
						else if (legDir == OrcaLegDirection.Down && (last - currentExtremePrice) >= reversalThreshold)
						{
							HandleReversalTick(OrcaLegDirection.Up, last, time, primaryBarIndex); return;
						}
					}
				}

				ProcessTickToLeg(CurrentLeg, last, vol, signedVol, time);
				CurrentLeg.HighPrice = Math.Max(CurrentLeg.HighPrice, last);
				CurrentLeg.LowPrice = Math.Min(CurrentLeg.LowPrice, last);
				CurrentLeg.EndTime = time;
			}

			private void HandleReversalTick(OrcaLegDirection newDir, double currentTickPrice, DateTime time, int primaryBarIndex)
			{
				if (Math.Abs(CurrentLeg.HighPrice - CurrentLeg.LowPrice) / parent.TickSize >= parent.MinimumLegTicks)
				{
					CompletedLegs.Add(CurrentLeg);
					if (CompletedLegs.Count > parent.LegsToDisplay) CompletedLegs.RemoveAt(0);
				}

				legDir = newDir;
				CurrentLeg = new PriceLeg { StartIndex = currentExtremeBar > -1 ? currentExtremeBar : primaryBarIndex, StartTime = currentExtremeTime, EndIndex = primaryBarIndex, EndTime = time, HighPrice = currentExtremePrice, LowPrice = currentExtremePrice, Direction = newDir };

				foreach (var t in ticksSinceExtreme)
				{
					ProcessTickToLeg(CurrentLeg, t.Price, t.Volume, t.SignedVolume, t.Time);
					CurrentLeg.HighPrice = Math.Max(CurrentLeg.HighPrice, t.Price);
					CurrentLeg.LowPrice = Math.Min(CurrentLeg.LowPrice, t.Price);
					CurrentLeg.EndTime = t.Time;
				}
				ticksSinceExtreme.Clear();
				currentExtremePrice = currentTickPrice; currentExtremeBar = primaryBarIndex; currentExtremeTime = time;
			}

			private void StartNewLegAtCurrentTick(OrcaLegDirection dir, double last, DateTime time, int primaryBarIndex)
			{
				legDir = dir; currentExtremePrice = last; currentExtremeBar = primaryBarIndex; currentExtremeTime = time;
				CurrentLeg = new PriceLeg { StartIndex = primaryBarIndex, StartTime = time, EndIndex = primaryBarIndex, EndTime = time, HighPrice = last, LowPrice = last, Direction = dir };
			}

			private void ProcessTickToLeg(PriceLeg targetLeg, double price, long vol, long signedVol, DateTime time)
			{
				if (targetLeg == null || vol <= 0) return;
				double volComp = parent.VolumeTickCompression * parent.TickSize;
				double roundedVolPrice = Math.Floor(price / volComp + 0.000001) * volComp;
				lock (targetLeg.SyncObj) targetLeg.VolByPrice[roundedVolPrice] = targetLeg.VolByPrice.TryGetValue(roundedVolPrice, out long v) ? v + vol : vol;

				if (signedVol == 0) return;
				int baseTicks = parent.UseDynamicAggregation ? 1 : parent.DeltaTickCompression;
				double deltaComp = baseTicks * parent.TickSize;
				double roundedDeltaPrice = Math.Floor(price / deltaComp + 0.000001) * deltaComp;
				lock (targetLeg.SyncObj) targetLeg.DeltaByPrice[roundedDeltaPrice] = targetLeg.DeltaByPrice.TryGetValue(roundedDeltaPrice, out long d) ? d + signedVol : signedVol;
			}
		}

		private bool CalcValueArea(Dictionary<double, long> volMap, double pocPrice, out double vahPrice, out double valPrice)
		{
			vahPrice = pocPrice; valPrice = pocPrice;
			if (volMap.Count <= 1) return false;

			var sortedPrices = new List<double>(volMap.Keys);
			sortedPrices.Sort();

			long totalVol = volMap.Values.Sum();
			if (totalVol <= 0) return false;

			double targetVol = totalVol * (ValueAreaPercent / 100.0);
			int pocIdx = sortedPrices.IndexOf(pocPrice);
			if (pocIdx < 0) return false;

			long accumulatedVol = volMap[pocPrice];
			int lo = pocIdx, hi = pocIdx;

			while (accumulatedVol < targetVol && (lo > 0 || hi < sortedPrices.Count - 1))
			{
				long volBelow = (lo > 0) ? volMap[sortedPrices[lo - 1]] : 0;
				long volAbove = (hi < sortedPrices.Count - 1) ? volMap[sortedPrices[hi + 1]] : 0;
				if (lo <= 0) { hi++; accumulatedVol += volAbove; }
				else if (hi >= sortedPrices.Count - 1) { lo--; accumulatedVol += volBelow; }
				else if (volAbove >= volBelow) { hi++; accumulatedVol += volAbove; }
				else { lo--; accumulatedVol += volBelow; }
			}

			valPrice = sortedPrices[lo];
			vahPrice = sortedPrices[hi];
			return true;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (currentTracker?.CurrentLeg == null) return;
			EnsureDxResources();
			var panel = chartControl.ChartPanels[chartScale.PanelIndex];

			int dynamicVolComp = VolumeTickCompression;
			int dynamicDeltaComp = DeltaTickCompression;
			if (UseDynamicAggregation)
			{
				double visibleTicks = (chartScale.MaxValue - chartScale.MinValue) / TickSize;
				double ticksPerPixel = visibleTicks / Math.Max(1, panel.H);
				double desiredTicks = ticksPerPixel * (DeltaLabelFontSize + 4) * DynamicAggregationMultiplier;

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
				else if (desiredTicks <= 100) dynamicDeltaComp = (int)(Math.Round(desiredTicks / 20.0) * 20); // 60, 80, 100
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

			float rightmostEdge = chartControl.CanvasRight - RightOffsetPx - VolumeProfileWidthPx;
			DrawLegProfiles(chartControl, chartScale, panel, currentTracker.CurrentLeg, rightmostEdge, VolumeProfileWidthPx, DeltaProfileWidthPx, true, false, dynamicVolComp, dynamicDeltaComp);

			if (LegsToDisplay > 0 && pastTracker?.CompletedLegs.Count > 0)
			{
				for (int i = pastTracker.CompletedLegs.Count - 1; i >= 0; i--)
				{
					var leg = pastTracker.CompletedLegs[i];
					float originX = chartControl.GetXByTime(leg.StartTime);
					DrawLegProfiles(chartControl, chartScale, panel, leg, originX, PastVolumeWidthPx, PastDeltaWidthPx, false, !ShowPastDelta, dynamicVolComp, dynamicDeltaComp);
				}
			}
		}

		private void DrawLegProfiles(ChartControl chartControl, ChartScale chartScale, NinjaTrader.Gui.Chart.ChartPanel panel, PriceLeg leg, float originX, int vWidth, int dWidth, bool isCurrent, bool forceHideDelta, int volCompTicks, int deltaCompTicks)
		{
			// Snapshot gradient palettes up front to avoid a null-swap race with OnRenderTargetChanged() mid-render
			var localVolGrad = volGradientBrushes;
			var localVAGrad  = vaGradientBrushes;
			if (ShowCurrentLegBox && isCurrent)
			{
				int topY = chartScale.GetYByValue(leg.HighPrice), bottomY = chartScale.GetYByValue(leg.LowPrice);
				RenderTarget.DrawRectangle(new RectangleF(originX - dWidth - 5, topY - 5, vWidth + dWidth + 10, (bottomY - topY) + 10), legBoxBrushDx, 1f);
			}

			float spineX = MirrorProfile ? originX + vWidth : originX;
			if (ShowVolume && leg.VolByPrice.Count > 0)
			{
				Dictionary<double, long> targetVolMap;
				lock (leg.SyncObj) targetVolMap = isCurrent ? new Dictionary<double, long>(leg.VolByPrice) : leg.VolByPrice;
				long maxVol = 0; double pocPrice = double.NaN;
				bool haveVA = false; double vahPrice = double.NaN, valPrice = double.NaN;

				if (!leg.IsVACalculated || isCurrent)
				{
					foreach (var kvp in targetVolMap) { if (kvp.Value > maxVol) { maxVol = kvp.Value; pocPrice = kvp.Key; } }
					if (maxVol > 0 && ShowValueArea && (ShowVAColor || ShowVALines))
						haveVA = CalcValueArea(targetVolMap, pocPrice, out vahPrice, out valPrice);
					
					if (!isCurrent)
					{
						leg.MaxVol = maxVol; leg.POCPrice = pocPrice;
						leg.VAHPrice = vahPrice; leg.VALPrice = valPrice;
						leg.IsVACalculated = true;
					}
				}
				else
				{
					maxVol = leg.MaxVol; pocPrice = leg.POCPrice;
					vahPrice = leg.VAHPrice; valPrice = leg.VALPrice;
					haveVA = !double.IsNaN(vahPrice);
				}

				if (maxVol > 0)
				{
					foreach (var kvp in targetVolMap)
					{
						int yTop = chartScale.GetYByValue(kvp.Key);
						if (yTop < panel.Y - 50 || yTop > panel.Y + panel.H + 50) continue;
						int yBot = chartScale.GetYByValue(kvp.Key - (volCompTicks * TickSize));
						int height = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
						float w = (float)(vWidth * (kvp.Value / (double)maxVol));
						if (w > 0.5f)
						{
							bool insideVA = haveVA && kvp.Key >= valPrice - TickSize * 0.01 && kvp.Key <= vahPrice + TickSize * 0.01;
							SolidColorBrush brush = volBrushDx;
							if (ShowPOC && Math.Abs(kvp.Key - pocPrice) < TickSize * 0.01) brush = pocBrushDx;
							else if (UseGradient)
							{
								var palette = (ShowValueArea && ShowVAColor && insideVA && localVAGrad != null) ? localVAGrad : localVolGrad;
								if (palette != null)
								{
									int gradIdx = Math.Min(palette.Length - 1, Math.Max(0, (int)((kvp.Value / (double)maxVol) * (palette.Length - 1))));
									brush = palette[gradIdx];
								}
							}
							else brush = (ShowValueArea && ShowVAColor && insideVA) ? vaVolBrushDx : volBrushDx;

							float barX = MirrorProfile ? spineX - w : spineX;
							RenderTarget.FillRectangle(new RectangleF(barX, Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f, w, height), brush);
						}
					}
					if (haveVA && ShowValueArea && ShowVALines && vaLineBrushDx != null)
					{
						float lineLeft = MirrorProfile ? spineX - vWidth - 2 : spineX - 2;
						float lineRight = MirrorProfile ? spineX + 2 : spineX + vWidth + 2;
						float yVAH = chartScale.GetYByValue(vahPrice);
						float yVAL = chartScale.GetYByValue(valPrice);
						if (yVAH >= panel.Y - 5 && yVAH <= panel.Y + panel.H + 5)
							RenderTarget.DrawLine(new Vector2(lineLeft, yVAH), new Vector2(lineRight, yVAH), vaLineBrushDx, VALineThickness, vaLineStrokeDx);
						if (yVAL >= panel.Y - 5 && yVAL <= panel.Y + panel.H + 5)
							RenderTarget.DrawLine(new Vector2(lineLeft, yVAL), new Vector2(lineRight, yVAL), vaLineBrushDx, VALineThickness, vaLineStrokeDx);
					}
				}
			}

			if (ShowDelta && !forceHideDelta && leg.DeltaByPrice.Count > 0)
			{
				double deltaComp = deltaCompTicks * TickSize;
				var groupedDelta = new Dictionary<double, long>();
				Dictionary<double, long> targetDeltaMap;
				lock (leg.SyncObj) targetDeltaMap = isCurrent ? new Dictionary<double, long>(leg.DeltaByPrice) : leg.DeltaByPrice;
				foreach (var kvp in targetDeltaMap)
				{
					double bPrice = Math.Floor(kvp.Key / deltaComp + 0.000001) * deltaComp;
					groupedDelta[bPrice] = groupedDelta.TryGetValue(bPrice, out long ext) ? ext + kvp.Value : kvp.Value;
				}
				long maxAbsDelta = groupedDelta.Values.Select(v => Math.Abs(v)).DefaultIfEmpty(0).Max();
				if (maxAbsDelta > 0)
				{
					foreach (var kvp in groupedDelta)
					{
						int yTop = chartScale.GetYByValue(kvp.Key + deltaComp);
						if (yTop < panel.Y - 50 || yTop > panel.Y + panel.H + 50) continue;
						int yBot = chartScale.GetYByValue(kvp.Key), height = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
						float drawY = Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f, w = (float)(dWidth * (Math.Abs(kvp.Value) / (double)maxAbsDelta));
						if (w > 0.5f)
						{
							float barX = MirrorProfile ? spineX : spineX - w;
							RenderTarget.FillRectangle(new RectangleF(barX, drawY, w, height), kvp.Value >= 0 ? posBrushDx : negBrushDx);
							if (height >= DeltaLabelFontSize + 2)
							{
								string lbl = kvp.Value.ToString("+#;-#;0");
								float textWidth = MeasureTextWidth(lbl), tX = MirrorProfile ? (spineX + 2) : (spineX - textWidth - 2), tY = drawY + (height / 2f) - (DeltaLabelFontSize / 2f);
								if (ShowDeltaLabelBackground) RenderTarget.FillRectangle(new RectangleF(tX - 1, tY - 1, textWidth + 2, DeltaLabelFontSize + 2), labelBgBrushDx);
								RenderTarget.DrawText(lbl, textFormat, new RectangleF(tX, tY, textWidth, DeltaLabelFontSize + 2), textBrushDx);
							}
						}
					}
				}
			}
		}

		private void EnsureDxResources()
		{
			if (posBrushDx == null) posBrushDx = new SolidColorBrush(RenderTarget, ToDx(PositiveBrush, DeltaOpacity));
			if (negBrushDx == null) negBrushDx = new SolidColorBrush(RenderTarget, ToDx(NegativeBrush, DeltaOpacity));
			if (textBrushDx == null) textBrushDx = new SolidColorBrush(RenderTarget, ToDx(TextBrush, 1f));
			if (volBrushDx == null) volBrushDx = new SolidColorBrush(RenderTarget, ToDx(VolumeBrush, VolumeOpacity));
			if (labelBgBrushDx == null) labelBgBrushDx = new SolidColorBrush(RenderTarget, ToDx(LabelBgBrush, 1f));
			if (legBoxBrushDx == null) legBoxBrushDx = new SolidColorBrush(RenderTarget, ToDx(LegBoxBrush, 1f));
			if (pocBrushDx == null) pocBrushDx = new SolidColorBrush(RenderTarget, ToDx(POCBrush, 1f));
			if (vaVolBrushDx == null) vaVolBrushDx = new SolidColorBrush(RenderTarget, ToDx(VABrush, VolumeOpacity));
			if (vaLineBrushDx == null) vaLineBrushDx = new SolidColorBrush(RenderTarget, ToDx(VALineBrush, 1f));
			if (vaLineStrokeDx == null)
			{
				DashStyle ds = VALineStyle == VALineStyleEnum.Solid ? DashStyle.Solid :
							   VALineStyle == VALineStyleEnum.Dot ? DashStyle.Dot :
							   VALineStyle == VALineStyleEnum.DashDot ? DashStyle.DashDot : DashStyle.Dash;
				vaLineStrokeDx = new StrokeStyle(RenderTarget.Factory, new StrokeStyleProperties { DashStyle = ds });
			}
			if (textFormat == null) textFormat = new TextFormat(Core.Globals.DirectWriteFactory, "Segoe UI", FontWeight.Bold, FontStyle.Normal, (float)DeltaLabelFontSize) { TextAlignment = SharpDX.DirectWrite.TextAlignment.Center, ParagraphAlignment = ParagraphAlignment.Center };
			
			int steps = Math.Max(2, GradientSteps);
			if (UseGradient && (volGradientBrushes == null || lastBuiltGradientSteps != steps))
			{
				if (volGradientBrushes != null) foreach (var b in volGradientBrushes) b?.Dispose();
				volGradientBrushes = BuildGradientPalette(VolumeBrush, steps);
				lastBuiltGradientSteps = steps;
			}
			if (UseGradient && ShowValueArea && ShowVAColor && (vaGradientBrushes == null || lastBuiltVAGradientSteps != steps))
			{
				if (vaGradientBrushes != null) foreach (var b in vaGradientBrushes) b?.Dispose();
				vaGradientBrushes = BuildGradientPalette(VABrush, steps);
				lastBuiltVAGradientSteps = steps;
			}
		}

		private SolidColorBrush[] BuildGradientPalette(WpfBrush baseBrush, int steps)
		{
			var baseColor = (baseBrush as WpfSolidColorBrush)?.Color ?? WpfColors.White;
			var palette = new SolidColorBrush[steps];
			for (int i = 0; i < steps; i++)
			{
				float t = i / (float)(steps - 1), brightness = MinBrightness + t * (1f - MinBrightness);
				palette[i] = new SolidColorBrush(RenderTarget, new Color4((baseColor.R / 255f) * brightness, (baseColor.G / 255f) * brightness, (baseColor.B / 255f) * brightness, (baseColor.A / 255f) * VolumeOpacity));
			}
			return palette;
		}

		private float MeasureTextWidth(string text)
		{ 
			if (textFormat == null) return 0f; 
			if (textWidthCache.TryGetValue(text, out float width)) return width;
			using (var l = new TextLayout(Core.Globals.DirectWriteFactory, text, textFormat, 1000, 100))
			{
				width = l.Metrics.Width;
				textWidthCache[text] = width;
				return width;
			}
		}
		private Color4 ToDx(WpfBrush b, float alphaMult) { var c = (b as WpfSolidColorBrush)?.Color ?? WpfColors.White; return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * alphaMult); }

		#region Properties
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Current Reversal Ticks", GroupName="Leg Detection", Order=0)] public int ReversalTicks { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Past Reversal Ticks", GroupName="Leg Detection", Order=1)] public int PastReversalTicks { get; set; }
		[NinjaScriptProperty] [Display(Name="Use ATR Reversal", GroupName="Leg Detection", Order=2)] public bool UseAtrReversal { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="ATR Period", GroupName="Leg Detection", Order=3)] public int AtrPeriod { get; set; }
		[NinjaScriptProperty] [Range(0.1, double.MaxValue)] [Display(Name="Current ATR Multiplier", GroupName="Leg Detection", Order=4)] public double AtrMultiplier { get; set; }
		[NinjaScriptProperty] [Range(0.1, double.MaxValue)] [Display(Name="Past ATR Multiplier", GroupName="Leg Detection", Order=5)] public double PastAtrMultiplier { get; set; }
		[NinjaScriptProperty] [Range(0, int.MaxValue)] [Display(Name="Min Leg Ticks", GroupName="Leg Detection", Order=6)] public int MinimumLegTicks { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Min Bars Per Leg", GroupName="Leg Detection", Order=7)] public int MinimumBarsPerLeg { get; set; }
		[NinjaScriptProperty] [Range(0, 1440)] [Display(Name="Min Duration (Min)", GroupName="Leg Detection", Order=8)] public int MinimumDurationMinutes { get; set; }
		[NinjaScriptProperty] [Range(0, 50)] [Display(Name="Legs To Display", GroupName="Layout", Order=4)] public int LegsToDisplay { get; set; }
		[NinjaScriptProperty] [Display(Name="Use Dynamic Aggregation", Description="Automatically adjust profile compression upon zoom", GroupName="Layout", Order=5)] public bool UseDynamicAggregation { get; set; }
		[NinjaScriptProperty] [Range(0.1, 10.0)] [Display(Name="Dynamic Aggregation Multiplier", Description="Lower value = more granular blocks (fewer aggregated ticks)", GroupName="Layout", Order=6)] public double DynamicAggregationMultiplier { get; set; }
		[NinjaScriptProperty] [Range(1, 100)] [Display(Name="Vol Compression (Ticks)", GroupName="Layout", Order=6)] public int VolumeTickCompression { get; set; }
		[NinjaScriptProperty] [Range(1, 100)] [Display(Name="Delta Compression (Ticks)", GroupName="Layout", Order=7)] public int DeltaTickCompression { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Vol Width", GroupName="Layout", Order=7)] public int VolumeProfileWidthPx { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Delta Width", GroupName="Layout", Order=8)] public int DeltaProfileWidthPx { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Past Vol Width", GroupName="Layout", Order=9)] public int PastVolumeWidthPx { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Past Delta Width", GroupName="Layout", Order=10)] public int PastDeltaWidthPx { get; set; }
		[NinjaScriptProperty] [Range(-500, 500)] [Display(Name="Right Offset (px)", GroupName="Layout", Order=11)] public int RightOffsetPx { get; set; }
		[NinjaScriptProperty] [Range(0, 500)] [Display(Name="Separation", GroupName="Layout", Order=12)] public int ProfileSeparationPx { get; set; }
		[NinjaScriptProperty] [Range(0, 10)] [Display(Name="Profile Bar Spacing", GroupName="Layout", Order=13)] public int ProfileBarSpacingPx { get; set; }
		[NinjaScriptProperty] [Display(Name="Mirror Profile", Description="Flip the profile so the spine is on the right and bars point left", GroupName="Layout", Order=14)] public bool MirrorProfile { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Volume", GroupName="Visibility", Order=14)] public bool ShowVolume { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Delta", GroupName="Visibility", Order=15)] public bool ShowDelta { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Past Delta", GroupName="Visibility", Order=16)] public bool ShowPastDelta { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Current Leg Box", GroupName="Visibility", Order=17)] public bool ShowCurrentLegBox { get; set; }
		[NinjaScriptProperty] [Range(5, 50)] [Display(Name="Delta Label Font Size", GroupName="Visibility", Order=18)] public int DeltaLabelFontSize { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Delta Lbl BG", GroupName="Visibility", Order=19)] public bool ShowDeltaLabelBackground { get; set; }
		[NinjaScriptProperty] [Display(Name="Show POC", GroupName="Volume Profile", Order=20)] public bool ShowPOC { get; set; }
		[NinjaScriptProperty] [Display(Name="Use Gradient", GroupName="Volume Profile", Order=21)] public bool UseGradient { get; set; }
		[NinjaScriptProperty] [Range(2, 64)] [Display(Name="Gradient Steps", GroupName="Volume Profile", Order=22)] public int GradientSteps { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Value Area", GroupName="Value Area", Order=23)] public bool ShowValueArea { get; set; }
		[NinjaScriptProperty] [Display(Name="VA Color Mode", GroupName="Value Area", Order=24)] public bool ShowVAColor { get; set; }
		[NinjaScriptProperty] [Display(Name="VA Boundary Lines", GroupName="Value Area", Order=25)] public bool ShowVALines { get; set; }
		[NinjaScriptProperty] [Range(50, 95)] [Display(Name="VA Percent", GroupName="Value Area", Order=26)] public int ValueAreaPercent { get; set; }
		[NinjaScriptProperty] [Range(0.5, 6.0)] [Display(Name="VA Line Thickness", GroupName="Value Area", Order=27)] public float VALineThickness { get; set; }
		[NinjaScriptProperty] [Display(Name="VA Line Style", GroupName="Value Area", Order=28)] public VALineStyleEnum VALineStyle { get; set; }
		[NinjaScriptProperty] [Range(0.05, 1.0)] [Display(Name="Min Brightness", GroupName="Colors", Order=30)] public float MinBrightness { get; set; }
		[NinjaScriptProperty] [Range(0.1, 1.0)] [Display(Name="Volume Opacity", GroupName="Colors", Order=31)] public float VolumeOpacity { get; set; }
		[NinjaScriptProperty] [Range(0.1, 1.0)] [Display(Name="Delta Opacity", GroupName="Colors", Order=32)] public float DeltaOpacity { get; set; }
		[XmlIgnore] [Display(Name="Pos Delta Color", GroupName="Colors", Order=33)] public WpfBrush PositiveBrush { get; set; }
		[Browsable(false)] public string PositiveBrushSerialize { get { return Serialize.BrushToString(PositiveBrush); } set { PositiveBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Neg Delta Color", GroupName="Colors", Order=34)] public WpfBrush NegativeBrush { get; set; }
		[Browsable(false)] public string NegativeBrushSerialize { get { return Serialize.BrushToString(NegativeBrush); } set { NegativeBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Vol Color", GroupName="Colors", Order=35)] public WpfBrush VolumeBrush { get; set; }
		[Browsable(false)] public string VolumeBrushSerialize { get { return Serialize.BrushToString(VolumeBrush); } set { VolumeBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Text Color", GroupName="Colors", Order=36)] public WpfBrush TextBrush { get; set; }
		[Browsable(false)] public string TextBrushSerialize { get { return Serialize.BrushToString(TextBrush); } set { TextBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Label BG Color", GroupName="Colors", Order=37)] public WpfBrush LabelBgBrush { get; set; }
		[Browsable(false)] public string LabelBgBrushSerialize { get { return Serialize.BrushToString(LabelBgBrush); } set { LabelBgBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="Leg Box Color", GroupName="Colors", Order=38)] public WpfBrush LegBoxBrush { get; set; }
		[Browsable(false)] public string LegBoxBrushSerialize { get { return Serialize.BrushToString(LegBoxBrush); } set { LegBoxBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="POC Color", GroupName="Colors", Order=39)] public WpfBrush POCBrush { get; set; }
		[Browsable(false)] public string POCBrushSerialize { get { return Serialize.BrushToString(POCBrush); } set { POCBrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="VA Color", GroupName="Colors", Order=40)] public WpfBrush VABrush { get; set; }
		[Browsable(false)] public string VABrushSerialize { get { return Serialize.BrushToString(VABrush); } set { VABrush = Serialize.StringToBrush(value); } }
		[XmlIgnore] [Display(Name="VA Line Color", GroupName="Colors", Order=41)] public WpfBrush VALineBrush { get; set; }
		[Browsable(false)] public string VALineBrushSerialize { get { return Serialize.BrushToString(VALineBrush); } set { VALineBrush = Serialize.StringToBrush(value); } }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaLegtoLegProfile[] cacheOrcaLegtoLegProfile;
		public OrcaLegtoLegProfile OrcaLegtoLegProfile(int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			return OrcaLegtoLegProfile(Input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, dynamicAggregationMultiplier, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, mirrorProfile, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
		}

		public OrcaLegtoLegProfile OrcaLegtoLegProfile(ISeries<double> input, int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			if (cacheOrcaLegtoLegProfile != null)
				for (int idx = 0; idx < cacheOrcaLegtoLegProfile.Length; idx++)
					if (cacheOrcaLegtoLegProfile[idx] != null && cacheOrcaLegtoLegProfile[idx].ReversalTicks == reversalTicks && cacheOrcaLegtoLegProfile[idx].PastReversalTicks == pastReversalTicks && cacheOrcaLegtoLegProfile[idx].UseAtrReversal == useAtrReversal && cacheOrcaLegtoLegProfile[idx].AtrPeriod == atrPeriod && cacheOrcaLegtoLegProfile[idx].AtrMultiplier == atrMultiplier && cacheOrcaLegtoLegProfile[idx].PastAtrMultiplier == pastAtrMultiplier && cacheOrcaLegtoLegProfile[idx].MinimumLegTicks == minimumLegTicks && cacheOrcaLegtoLegProfile[idx].MinimumBarsPerLeg == minimumBarsPerLeg && cacheOrcaLegtoLegProfile[idx].MinimumDurationMinutes == minimumDurationMinutes && cacheOrcaLegtoLegProfile[idx].LegsToDisplay == legsToDisplay && cacheOrcaLegtoLegProfile[idx].UseDynamicAggregation == useDynamicAggregation && cacheOrcaLegtoLegProfile[idx].DynamicAggregationMultiplier == dynamicAggregationMultiplier && cacheOrcaLegtoLegProfile[idx].VolumeTickCompression == volumeTickCompression && cacheOrcaLegtoLegProfile[idx].DeltaTickCompression == deltaTickCompression && cacheOrcaLegtoLegProfile[idx].VolumeProfileWidthPx == volumeProfileWidthPx && cacheOrcaLegtoLegProfile[idx].DeltaProfileWidthPx == deltaProfileWidthPx && cacheOrcaLegtoLegProfile[idx].PastVolumeWidthPx == pastVolumeWidthPx && cacheOrcaLegtoLegProfile[idx].PastDeltaWidthPx == pastDeltaWidthPx && cacheOrcaLegtoLegProfile[idx].RightOffsetPx == rightOffsetPx && cacheOrcaLegtoLegProfile[idx].ProfileSeparationPx == profileSeparationPx && cacheOrcaLegtoLegProfile[idx].ProfileBarSpacingPx == profileBarSpacingPx && cacheOrcaLegtoLegProfile[idx].MirrorProfile == mirrorProfile && cacheOrcaLegtoLegProfile[idx].ShowVolume == showVolume && cacheOrcaLegtoLegProfile[idx].ShowDelta == showDelta && cacheOrcaLegtoLegProfile[idx].ShowPastDelta == showPastDelta && cacheOrcaLegtoLegProfile[idx].ShowCurrentLegBox == showCurrentLegBox && cacheOrcaLegtoLegProfile[idx].DeltaLabelFontSize == deltaLabelFontSize && cacheOrcaLegtoLegProfile[idx].ShowDeltaLabelBackground == showDeltaLabelBackground && cacheOrcaLegtoLegProfile[idx].ShowPOC == showPOC && cacheOrcaLegtoLegProfile[idx].UseGradient == useGradient && cacheOrcaLegtoLegProfile[idx].GradientSteps == gradientSteps && cacheOrcaLegtoLegProfile[idx].ShowValueArea == showValueArea && cacheOrcaLegtoLegProfile[idx].ShowVAColor == showVAColor && cacheOrcaLegtoLegProfile[idx].ShowVALines == showVALines && cacheOrcaLegtoLegProfile[idx].ValueAreaPercent == valueAreaPercent && cacheOrcaLegtoLegProfile[idx].VALineThickness == vALineThickness && cacheOrcaLegtoLegProfile[idx].VALineStyle == vALineStyle && cacheOrcaLegtoLegProfile[idx].MinBrightness == minBrightness && cacheOrcaLegtoLegProfile[idx].VolumeOpacity == volumeOpacity && cacheOrcaLegtoLegProfile[idx].DeltaOpacity == deltaOpacity && cacheOrcaLegtoLegProfile[idx].EqualsInput(input))
						return cacheOrcaLegtoLegProfile[idx];
			return CacheIndicator<OrcaLegtoLegProfile>(new OrcaLegtoLegProfile(){ ReversalTicks = reversalTicks, PastReversalTicks = pastReversalTicks, UseAtrReversal = useAtrReversal, AtrPeriod = atrPeriod, AtrMultiplier = atrMultiplier, PastAtrMultiplier = pastAtrMultiplier, MinimumLegTicks = minimumLegTicks, MinimumBarsPerLeg = minimumBarsPerLeg, MinimumDurationMinutes = minimumDurationMinutes, LegsToDisplay = legsToDisplay, UseDynamicAggregation = useDynamicAggregation, DynamicAggregationMultiplier = dynamicAggregationMultiplier, VolumeTickCompression = volumeTickCompression, DeltaTickCompression = deltaTickCompression, VolumeProfileWidthPx = volumeProfileWidthPx, DeltaProfileWidthPx = deltaProfileWidthPx, PastVolumeWidthPx = pastVolumeWidthPx, PastDeltaWidthPx = pastDeltaWidthPx, RightOffsetPx = rightOffsetPx, ProfileSeparationPx = profileSeparationPx, ProfileBarSpacingPx = profileBarSpacingPx, MirrorProfile = mirrorProfile, ShowVolume = showVolume, ShowDelta = showDelta, ShowPastDelta = showPastDelta, ShowCurrentLegBox = showCurrentLegBox, DeltaLabelFontSize = deltaLabelFontSize, ShowDeltaLabelBackground = showDeltaLabelBackground, ShowPOC = showPOC, UseGradient = useGradient, GradientSteps = gradientSteps, ShowValueArea = showValueArea, ShowVAColor = showVAColor, ShowVALines = showVALines, ValueAreaPercent = valueAreaPercent, VALineThickness = vALineThickness, VALineStyle = vALineStyle, MinBrightness = minBrightness, VolumeOpacity = volumeOpacity, DeltaOpacity = deltaOpacity }, input, ref cacheOrcaLegtoLegProfile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaLegtoLegProfile OrcaLegtoLegProfile(int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			return indicator.OrcaLegtoLegProfile(Input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, dynamicAggregationMultiplier, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, mirrorProfile, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
		}

		public Indicators.OrcaLegtoLegProfile OrcaLegtoLegProfile(ISeries<double> input , int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			return indicator.OrcaLegtoLegProfile(input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, dynamicAggregationMultiplier, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, mirrorProfile, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaLegtoLegProfile OrcaLegtoLegProfile(int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			return indicator.OrcaLegtoLegProfile(Input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, dynamicAggregationMultiplier, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, mirrorProfile, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
		}

		public Indicators.OrcaLegtoLegProfile OrcaLegtoLegProfile(ISeries<double> input , int reversalTicks, int pastReversalTicks, bool useAtrReversal, int atrPeriod, double atrMultiplier, double pastAtrMultiplier, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, bool useDynamicAggregation, double dynamicAggregationMultiplier, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool mirrorProfile, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, bool showPOC, bool useGradient, int gradientSteps, bool showValueArea, bool showVAColor, bool showVALines, int valueAreaPercent, float vALineThickness, VALineStyleEnum vALineStyle, float minBrightness, float volumeOpacity, float deltaOpacity)
		{
			return indicator.OrcaLegtoLegProfile(input, reversalTicks, pastReversalTicks, useAtrReversal, atrPeriod, atrMultiplier, pastAtrMultiplier, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, useDynamicAggregation, dynamicAggregationMultiplier, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, mirrorProfile, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, showPOC, useGradient, gradientSteps, showValueArea, showVAColor, showVALines, valueAreaPercent, vALineThickness, vALineStyle, minBrightness, volumeOpacity, deltaOpacity);
		}
	}
}

#endregion

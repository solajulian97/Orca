# region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
# endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class AutoLegProfileNT : Indicator
	{
		#region Fields
		private List<PriceLeg> completedLegs;
		private PriceLeg currentLeg;
		
		private double currentExtremePrice;
		private int currentExtremeIndex; 
		private DateTime currentExtremeTime;
		private bool isUpLeg;
		
		private SharpDX.Direct2D1.Brush volumeBrush;
		private SharpDX.Direct2D1.Brush valueAreaBrush;
		private SharpDX.Direct2D1.Brush positiveDeltaBrush;
		private SharpDX.Direct2D1.Brush negativeDeltaBrush;
		private SharpDX.Direct2D1.Brush vwapBrush;
		private SharpDX.Direct2D1.Brush textBrush;
		private SharpDX.Direct2D1.Brush labelBackgroundBrush;
		private TextFormat textFormat;
		private double lastBarVolume;
		#endregion

		#region Classes
		private class PriceLeg
		{
			public int StartIndex;
			public int EndIndex;
			public DateTime StartTime;
			public DateTime EndTime;
			public double StartPrice;
			public double EndPrice;
			public double HighPrice;
			public double LowPrice;
			public bool IsUpLeg;
			
			public Dictionary<double, LevelData> VolumeProfileData = new Dictionary<double, LevelData>();
			public Dictionary<double, LevelData> DeltaProfileData = new Dictionary<double, LevelData>();
			public List<Tuple<DateTime, double>> VwapPoints = new List<Tuple<DateTime, double>>();
			public DateTime LastVwapBarTime = DateTime.MinValue;

			public double TotalVolume;
			public double TotalPV;
			public double CurrentVwapValue;    
			
			public double LegTotalVolume; 
			public double MaxVolume;
			public double MaxDeltaAbs;

			public HashSet<double> CachedValueAreaLevels = new HashSet<double>();
			public double LastVAUpdateVolume = 0;

			public void ResetData()
			{
				VolumeProfileData.Clear();
				DeltaProfileData.Clear();
				VwapPoints.Clear();
				CachedValueAreaLevels.Clear();
				LastVAUpdateVolume = 0;
				LastVwapBarTime = DateTime.MinValue;
				CurrentVwapValue = 0;
				TotalVolume = 0;
				TotalPV = 0;
				LegTotalVolume = 0;
				MaxVolume = 0;
				MaxDeltaAbs = 0;
			}
		}

		private class LevelData
		{
			public double Volume;
			public double BuyVolume;
			public double SellVolume;
			public double Delta { get { return BuyVolume - SellVolume; } }
		}
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Automatically creates volume/delta profiles for each price leg";
				Name										= "AutoLegProfileNT";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
				
				// Parameters
				ReversalTicks								= 20;
				MinimumLegTicks								= 20;
				MinimumBarsPerLeg                           = 1;
				MinimumDurationMinutes                      = 0;
				TickCompression								= 4;
				DeltaTickCompression						= 10;
				LegsToDisplay								= 10;
				VolumeProfileWidth							= 150;
				DeltaProfileWidth							= 100;
				PastVolumeWidth								= 60;
				PastDeltaWidth								= 40;
				RightOffset									= 60;
				ProfileSeparation							= 20;
				ProfileBarSpacing                           = 0;
				MergeOverlapPercent							= 80;
				MirrorPastProfiles							= true;
				ShowVolume									= true;
				ShowDelta									= true;
				ValueAreaPercent							= 70;
				ShowCurrentLegBox                           = false;
				ShowVWAP									= true;
				ShowDeltaLabels								= true;
				DeltaLabelMinHeight							= 12;
				DeltaLabelFontSize							= 12;
				ShowDeltaLabelBackground                    = true;
				
				// Colors
				PositiveDeltaColor							= Brushes.Lime;
				NegativeDeltaColor							= Brushes.Red;
				VolumeColor									= Brushes.RoyalBlue;
				ValueAreaColor								= Brushes.Gray;
				VWAPColor									= Brushes.Magenta;
				TextColor									= Brushes.White;
				DeltaLabelBackgroundColor                   = Brushes.Black;
			}
			else if (State == State.Configure)
			{
				completedLegs = new List<PriceLeg>();
			}
			else if (State == State.Terminated)
			{
				DisposeResources();
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeResources();
		}

		private void DisposeResources()
		{
			if (volumeBrush != null) volumeBrush.Dispose();
			if (valueAreaBrush != null) valueAreaBrush.Dispose();
			if (positiveDeltaBrush != null) positiveDeltaBrush.Dispose();
			if (negativeDeltaBrush != null) negativeDeltaBrush.Dispose();
			if (vwapBrush != null) vwapBrush.Dispose();
			if (textBrush != null) textBrush.Dispose();
			if (labelBackgroundBrush != null) labelBackgroundBrush.Dispose();
			if (textFormat != null) textFormat.Dispose();
			
			volumeBrush = null;
			valueAreaBrush = null;
			positiveDeltaBrush = null;
			negativeDeltaBrush = null;
			vwapBrush = null;
			textBrush = null;
			labelBackgroundBrush = null;
			textFormat = null;
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) return;

			double high = High[0];
			double low = Low[0];
			double close = Close[0];
			double volume = Volume[0];
			double open = Open[0];
			double tickVolume = (CurrentBar == currentLeg?.EndIndex) ? (volume - lastBarVolume) : volume;
			lastBarVolume = volume;
			
			DateTime time = Time[0];

			// Initialize
			if (currentLeg == null)
			{
				isUpLeg = true;
				currentExtremePrice = high;
				currentExtremeIndex = CurrentBar;
				currentExtremeTime = time;
				
				currentLeg = new PriceLeg
				{
					StartIndex = CurrentBar,
					StartTime = time,
					StartPrice = close,
					HighPrice = high,
					LowPrice = low,
					IsUpLeg = true,
					EndPrice = close,
					EndTime = time,
					EndIndex = CurrentBar
				};
			}

			// Extreme Check
			bool newExtremeFound = false;
			if (isUpLeg)
			{
				if (high >= currentExtremePrice)
				{
					currentExtremePrice = high;
					currentExtremeIndex = CurrentBar;
					currentExtremeTime = time;
					newExtremeFound = true;
				}
			}
			else
			{
				if (low <= currentExtremePrice)
				{
					currentExtremePrice = low;
					currentExtremeIndex = CurrentBar;
					currentExtremeTime = time;
					newExtremeFound = true;
				}
			}

			// Reversal Check
			double reversalThreshold = ReversalTicks * TickSize;
			bool reversalDetected = false;

			if (!newExtremeFound)
			{
				int barsInLeg = CurrentBar - currentLeg.StartIndex;
				bool constraintsMet = barsInLeg >= MinimumBarsPerLeg;
				
				if (MinimumDurationMinutes > 0)
				{
					constraintsMet = constraintsMet && (time - currentLeg.StartTime).TotalMinutes >= MinimumDurationMinutes;
				}

				if (constraintsMet)
				{
					if (isUpLeg && (currentExtremePrice - low) >= reversalThreshold)
					{
						reversalDetected = true;
						HandleReversal(false);
					}
					else if (!isUpLeg && (high - currentExtremePrice) >= reversalThreshold)
					{
						reversalDetected = true;
						HandleReversal(true);
					}
				}
			}

			// Accumulate data
			if (!reversalDetected && tickVolume > 0)
			{
				if (State == State.Historical && !IsFirstTickOfBar)
				{
					// For historical data without Tick Replay, distribute volume across the bar
					AccumulateBar(currentLeg, high, low, open, close, tickVolume);
				}
				else
				{
					// For real-time data or Tick Replay
					AccumulateTick(currentLeg, close, tickVolume); 
				}

				currentLeg.HighPrice = Math.Max(currentLeg.HighPrice, high);
				currentLeg.LowPrice = Math.Min(currentLeg.LowPrice, low);
				currentLeg.EndIndex = CurrentBar;
				currentLeg.EndTime = time;
				currentLeg.EndPrice = close;
				
				UpdateVWAP(currentLeg, close, tickVolume, time);
				UpdateLegStats(currentLeg);

				// Only update Value Area periodically (e.g. on new bar or significant volume change) to save CPU
				if (IsFirstTickOfBar || (currentLeg.LegTotalVolume - currentLeg.LastVAUpdateVolume) > 1000)
				{
					UpdateValueArea(currentLeg);
				}
			}
		}

		private void UpdateValueArea(PriceLeg leg)
		{
			leg.CachedValueAreaLevels.Clear();
			if (leg.LegTotalVolume == 0) return;

			double targetVol = leg.LegTotalVolume * (ValueAreaPercent / 100.0);
			double accVol = 0;
			var levels = leg.VolumeProfileData.OrderByDescending(k => k.Value.Volume).ToList();
			
			foreach (var lev in levels) 
			{ 
				if (accVol < targetVol) 
				{ 
					leg.CachedValueAreaLevels.Add(lev.Key); 
					accVol += lev.Value.Volume; 
				} 
				else break;
			}
			leg.LastVAUpdateVolume = leg.LegTotalVolume;
		}

		private void HandleReversal(bool toUpLeg)
		{
			PriceLeg oldLeg = currentLeg;
			oldLeg.EndIndex = currentExtremeIndex;
			oldLeg.EndTime = currentExtremeTime;

			if (Math.Abs(oldLeg.HighPrice - oldLeg.LowPrice) / TickSize >= MinimumLegTicks)
			{
				bool merged = false;
				if (completedLegs.Count > 0 && MergeOverlapPercent > 0)
				{
					PriceLeg lastLeg = completedLegs.Last();
					double overlapLow = Math.Max(lastLeg.LowPrice, oldLeg.LowPrice);
					double overlapHigh = Math.Min(lastLeg.HighPrice, oldLeg.HighPrice);
					double overlapHeight = Math.Max(0, overlapHigh - overlapLow);
					double minHeight = Math.Min(lastLeg.HighPrice - lastLeg.LowPrice, oldLeg.HighPrice - oldLeg.LowPrice);
					
					if (minHeight > 0 && (overlapHeight / minHeight) * 100.0 >= MergeOverlapPercent)
					{
						MergeLegs(lastLeg, oldLeg);
						merged = true;
					}
				}

				if (!merged)
				{
					completedLegs.Add(oldLeg);
					if (completedLegs.Count > LegsToDisplay) completedLegs.RemoveAt(0);
				}
			}

			// Start New Leg
			isUpLeg = toUpLeg;
			currentLeg = new PriceLeg
			{
				StartIndex = CurrentBar,
				StartTime = Time[0],
				StartPrice = Close[0],
				IsUpLeg = toUpLeg,
				HighPrice = toUpLeg ? High[0] : currentExtremePrice,
				LowPrice = toUpLeg ? currentExtremePrice : Low[0],
				EndIndex = CurrentBar,
				EndTime = Time[0],
				EndPrice = Close[0]
			};
			
			currentExtremePrice = toUpLeg ? High[0] : Low[0];
			currentExtremeIndex = CurrentBar;
			currentExtremeTime = Time[0];
			lastBarVolume = 0; // Reset for new leg context
		}

		private void MergeLegs(PriceLeg dest, PriceLeg src)
		{
			dest.HighPrice = Math.Max(dest.HighPrice, src.HighPrice);
			dest.LowPrice = Math.Min(dest.LowPrice, src.LowPrice);
			dest.EndTime = src.EndTime;
			dest.EndIndex = src.EndIndex;

			foreach (var kvp in src.VolumeProfileData)
			{
				if (!dest.VolumeProfileData.ContainsKey(kvp.Key)) dest.VolumeProfileData[kvp.Key] = new LevelData();
				dest.VolumeProfileData[kvp.Key].Volume += kvp.Value.Volume;
				dest.VolumeProfileData[kvp.Key].BuyVolume += kvp.Value.BuyVolume;
				dest.VolumeProfileData[kvp.Key].SellVolume += kvp.Value.SellVolume;
			}
			
			foreach (var kvp in src.DeltaProfileData)
			{
				if (!dest.DeltaProfileData.ContainsKey(kvp.Key)) dest.DeltaProfileData[kvp.Key] = new LevelData();
				dest.DeltaProfileData[kvp.Key].BuyVolume += kvp.Value.BuyVolume;
				dest.DeltaProfileData[kvp.Key].SellVolume += kvp.Value.SellVolume;
			}

			dest.VwapPoints.AddRange(src.VwapPoints);
			dest.TotalVolume += src.TotalVolume;
			dest.TotalPV += src.TotalPV;
			dest.CurrentVwapValue = src.CurrentVwapValue;
			UpdateLegStats(dest);
			UpdateValueArea(dest);
		}

		private void AccumulateBar(PriceLeg leg, double high, double low, double open, double close, double volume)
		{
			// Volume Profile Base
			double vComp = TickCompression * TickSize;
			double rangeStartV = Math.Floor(low / vComp) * vComp;
			double rangeEndV = Math.Ceiling(high / vComp) * vComp;

			for (double price = rangeStartV; price <= rangeEndV; price += vComp)
			{
				double rounded = Math.Floor(price / vComp + 0.000001) * vComp;
				if (!leg.VolumeProfileData.ContainsKey(rounded)) leg.VolumeProfileData[rounded] = new LevelData();
				
				double levelVolume = volume / ((rangeEndV - rangeStartV) / vComp + 1);
				leg.VolumeProfileData[rounded].Volume += levelVolume;

				double barRange = high - low;
				if (barRange > 0)
				{
					double closePosition = (close - low) / barRange;
					leg.VolumeProfileData[rounded].BuyVolume += levelVolume * closePosition;
					leg.VolumeProfileData[rounded].SellVolume += levelVolume * (1 - closePosition);
				}
				else
				{
					leg.VolumeProfileData[rounded].BuyVolume += levelVolume * 0.5;
					leg.VolumeProfileData[rounded].SellVolume += levelVolume * 0.5;
				}
			}

			// Delta Profile Base
			double dComp = DeltaTickCompression * TickSize;
			double rangeStartD = Math.Floor(low / dComp) * dComp;
			double rangeEndD = Math.Ceiling(high / dComp) * dComp;

			for (double price = rangeStartD; price <= rangeEndD; price += dComp)
			{
				double rounded = Math.Floor(price / dComp + 0.000001) * dComp;
				if (!leg.DeltaProfileData.ContainsKey(rounded)) leg.DeltaProfileData[rounded] = new LevelData();
				
				double levelVolume = volume / ((rangeEndD - rangeStartD) / dComp + 1);
				
				// Accurately shape Delta based on the Bar's Open and Close
				if (close > open)
				{
					// Up Bar: More Buy Volume
					double buyRatio = 0.5 + Math.Max(0, (close - low) / (high - low) * 0.4); 
					leg.DeltaProfileData[rounded].BuyVolume += levelVolume * buyRatio;
					leg.DeltaProfileData[rounded].SellVolume += levelVolume * (1 - buyRatio);
				}
				else if (close < open)
				{
					// Down Bar: More Sell Volume
					double sellRatio = 0.5 + Math.Max(0, (high - close) / (high - low) * 0.4);
					leg.DeltaProfileData[rounded].SellVolume += levelVolume * sellRatio;
					leg.DeltaProfileData[rounded].BuyVolume += levelVolume * (1 - sellRatio);
				}
				else
				{
					// Doji: Neutral
					leg.DeltaProfileData[rounded].BuyVolume += levelVolume * 0.5;
					leg.DeltaProfileData[rounded].SellVolume += levelVolume * 0.5;
				}
			}
		}

		private void AccumulateTick(PriceLeg leg, double price, double volume)
		{
			// Volume Profile
			double comp = TickCompression * TickSize;
			double rounded = Math.Round(price / comp) * comp;
			if (!leg.VolumeProfileData.ContainsKey(rounded)) leg.VolumeProfileData[rounded] = new LevelData();
			leg.VolumeProfileData[rounded].Volume += volume;

			// Delta Profile (Simplified for NT8 Tick-by-Tick)
			double dComp = DeltaTickCompression * TickSize;
			double dRounded = Math.Round(price / dComp) * dComp;
			if (!leg.DeltaProfileData.ContainsKey(dRounded)) leg.DeltaProfileData[dRounded] = new LevelData();
			
			// Aggressor logic (simplistic NT check)
			if (CurrentBar > 0) {
				if (price.ApproxCompare(Close[1]) >= 0) { leg.VolumeProfileData[rounded].BuyVolume += volume; leg.DeltaProfileData[dRounded].BuyVolume += volume; }
				else { leg.VolumeProfileData[rounded].SellVolume += volume; leg.DeltaProfileData[dRounded].SellVolume += volume; }
			}
			else {
				leg.VolumeProfileData[rounded].BuyVolume += volume * 0.5;
				leg.VolumeProfileData[rounded].SellVolume += volume * 0.5;
			}
		}

		private void UpdateVWAP(PriceLeg leg, double price, double volume, DateTime time)
		{
			if (!ShowVWAP) return;
			leg.TotalVolume += volume;
			leg.TotalPV += (price * volume);
			if (leg.TotalVolume > 0)
			{
				leg.CurrentVwapValue = leg.TotalPV / leg.TotalVolume;
				if (time > leg.LastVwapBarTime)
				{
					leg.VwapPoints.Add(new Tuple<DateTime, double>(time, leg.CurrentVwapValue));
					leg.LastVwapBarTime = time;
				}
				else if (leg.VwapPoints.Count > 0)
				{
					leg.VwapPoints[leg.VwapPoints.Count - 1] = new Tuple<DateTime, double>(time, leg.CurrentVwapValue);
				}
			}
		}

		private void UpdateLegStats(PriceLeg leg)
		{
			leg.MaxVolume = 0; leg.LegTotalVolume = 0; leg.MaxDeltaAbs = 0;
			foreach (var val in leg.VolumeProfileData.Values)
			{
				leg.LegTotalVolume += val.Volume;
				if (val.Volume > leg.MaxVolume) leg.MaxVolume = val.Volume;
			}
			foreach (var val in leg.DeltaProfileData.Values)
			{
				double absDelta = Math.Abs(val.Delta);
				if (absDelta > leg.MaxDeltaAbs) leg.MaxDeltaAbs = absDelta;
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (completedLegs == null) return;
			
			// We need a specific ChartPanel to determine height limits
			NinjaTrader.Gui.Chart.ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];

			// Quick bounds check - is the chart even drawing near these?
			double minX = chartControl.GetXByTime(chartControl.FirstTimePainted);
			double maxX = chartControl.GetXByTime(chartControl.LastTimePainted);

			// Brushes setup
			if (volumeBrush == null) volumeBrush = VolumeColor.ToDxBrush(RenderTarget);
			if (valueAreaBrush == null) valueAreaBrush = ValueAreaColor.ToDxBrush(RenderTarget);
			if (positiveDeltaBrush == null) positiveDeltaBrush = PositiveDeltaColor.ToDxBrush(RenderTarget);
			if (negativeDeltaBrush == null) negativeDeltaBrush = NegativeDeltaColor.ToDxBrush(RenderTarget);
			if (vwapBrush == null) vwapBrush = VWAPColor.ToDxBrush(RenderTarget);
			if (textBrush == null) textBrush = TextColor.ToDxBrush(RenderTarget);
			if (labelBackgroundBrush == null) labelBackgroundBrush = DeltaLabelBackgroundColor.ToDxBrush(RenderTarget);
			if (textFormat == null) textFormat = new TextFormat(new SharpDX.DirectWrite.Factory(), "Arial", (float)DeltaLabelFontSize);

			foreach (var leg in completedLegs) 
			{
				int legOriginX = chartControl.GetXByTime(leg.StartTime);
				if (legOriginX + PastVolumeWidth + PastDeltaWidth + ProfileSeparation > minX && legOriginX - PastVolumeWidth - PastDeltaWidth - ProfileSeparation < maxX)
				{
					DrawLeg(leg, false, chartControl, chartScale, panel);
				}
			}
			if (currentLeg != null) DrawLeg(currentLeg, true, chartControl, chartScale, panel);
		}

		private void DrawLeg(PriceLeg leg, bool isCurrent, ChartControl chartControl, ChartScale chartScale, NinjaTrader.Gui.Chart.ChartPanel panel)
		{
			if (leg.VolumeProfileData.Count == 0 && leg.DeltaProfileData.Count == 0) return;

			int volWidth = isCurrent ? VolumeProfileWidth : PastVolumeWidth;
			int dWidth = isCurrent ? DeltaProfileWidth : PastDeltaWidth;
			
			int originX = isCurrent ? chartControl.CanvasRight - RightOffset : chartControl.GetXByTime(leg.StartTime);
			int vDir = isCurrent ? -1 : 1;
			int dDir = isCurrent ? -1 : (MirrorPastProfiles ? -1 : 1);
			int vOrigin = originX;
			int dOrigin = isCurrent ? (originX - volWidth - ProfileSeparation) : (MirrorPastProfiles ? originX : originX + volWidth + ProfileSeparation);

			// Draw Volume
			if (ShowVolume)
			{
				// Note: Uses CachedValueAreaLevels instead of sorting on every draw call
				foreach (var kvp in leg.VolumeProfileData)
				{
					int y = chartScale.GetYByValue(kvp.Key);
					// Out of bounds render check to save CPU
					if (y < panel.Y - 50 || y > panel.Y + panel.H + 50) continue; 

					int nextY = chartScale.GetYByValue(kvp.Key + (TickCompression * TickSize));
					int tickHeight = Math.Max(2, Math.Abs(nextY - y));
					int h = Math.Max(1, tickHeight - ProfileBarSpacing);
					int w = (int)((kvp.Value.Volume / leg.MaxVolume) * volWidth);
					int x = vDir == 1 ? vOrigin : vOrigin - w;
					RenderTarget.FillRectangle(new RectangleF(x, y - tickHeight + ProfileBarSpacing / 2, w, h), leg.CachedValueAreaLevels.Contains(kvp.Key) ? valueAreaBrush : volumeBrush);
				}
			}

			// Draw Delta
			if (ShowDelta && leg.MaxDeltaAbs > 0)
			{
				foreach (var kvp in leg.DeltaProfileData)
				{
					int y = chartScale.GetYByValue(kvp.Key);
					if (y < panel.Y - 50 || y > panel.Y + panel.H + 50) continue;

					int nextY = chartScale.GetYByValue(kvp.Key + (DeltaTickCompression * TickSize));
					int tickHeight = Math.Max(2, Math.Abs(nextY - y));
					int h = Math.Max(1, tickHeight - ProfileBarSpacing);
					int w = (int)((Math.Abs(kvp.Value.Delta) / leg.MaxDeltaAbs) * dWidth);
					if (Math.Abs(kvp.Value.Delta) > 0 && w == 0) w = 1;

					int x = dDir == 1 ? dOrigin : dOrigin - w;
					float drawY = y - tickHeight + ProfileBarSpacing / 2;
					RenderTarget.FillRectangle(new RectangleF(x, drawY, w, h), kvp.Value.Delta >= 0 ? positiveDeltaBrush : negativeDeltaBrush);

					if (ShowDeltaLabels && h >= DeltaLabelMinHeight && Math.Abs(kvp.Value.Delta) > 0)
					{
						string lbl = kvp.Value.Delta.ToString("+#;-#;0");
						
						// Simplistic Text metric estimation to avoid expensive TextLayout creations on hundreds of nodes
						float textWidth = lbl.Length * (DeltaLabelFontSize * 0.6f); 
						float textHeight = DeltaLabelFontSize + 2;
						
						float tx = isCurrent ? dOrigin + 2 : (dDir == 1 ? x + w + 2 : x - textWidth - 2);
						float ty = drawY + (h / 2f) - (textHeight / 2f);

						if (ShowDeltaLabelBackground)
						{
							RenderTarget.FillRectangle(new RectangleF(tx - 1, ty - 1, textWidth + 2, textHeight + 2), labelBackgroundBrush);
						}

						// Draw string directly instead of Layout
						RenderTarget.DrawText(lbl, textFormat, new RectangleF(tx, ty, 200, textHeight), textBrush);
					}
				}
			}

			// Draw VWAP
			if (ShowVWAP && leg.VwapPoints.Count > 1)
			{
				for (int i = 0; i < leg.VwapPoints.Count - 1; i++)
				{
					float x1 = chartControl.GetXByTime(leg.VwapPoints[i].Item1);
					float y1 = chartScale.GetYByValue(leg.VwapPoints[i].Item2);
					float x2 = chartControl.GetXByTime(leg.VwapPoints[i+1].Item1);
					float y2 = chartScale.GetYByValue(leg.VwapPoints[i+1].Item2);
					RenderTarget.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), vwapBrush, 2f);
				}
			}

			// Draw Current Leg Box
			if (isCurrent && ShowCurrentLegBox)
			{
				int topY = chartScale.GetYByValue(leg.HighPrice);
				int bottomY = chartScale.GetYByValue(leg.LowPrice);
				int totalWidth = volWidth + ProfileSeparation + dWidth;
				
				using (SharpDX.Direct2D1.Brush legBoxBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.Yellow))
				{
					RenderTarget.DrawRectangle(new RectangleF(originX - totalWidth, topY, totalWidth, bottomY - topY), legBoxBrush, 2f);
				}
			}
		}

		#region Properties
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Reversal Ticks", GroupName="Parameters", Order=0)]
		public int ReversalTicks { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Min Leg Ticks", GroupName="Parameters", Order=1)]
		public int MinimumLegTicks { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Min Bars Per Leg", GroupName="Parameters", Order=2)]
		public int MinimumBarsPerLeg { get; set; }
		[NinjaScriptProperty] [Range(0, 1440)] [Display(Name="Min Duration (Min)", GroupName="Parameters", Order=3)]
		public int MinimumDurationMinutes { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Vol Compression", GroupName="Parameters", Order=4)]
		public int TickCompression { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Delta Compression", GroupName="Parameters", Order=5)]
		public int DeltaTickCompression { get; set; }
		[NinjaScriptProperty] [Range(1, 50)] [Display(Name="Legs To Display", GroupName="Parameters", Order=6)]
		public int LegsToDisplay { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Vol Width", GroupName="Layout", Order=7)]
		public int VolumeProfileWidth { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Delta Width", GroupName="Layout", Order=8)]
		public int DeltaProfileWidth { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Past Vol Width", GroupName="Layout", Order=9)]
		public int PastVolumeWidth { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Past Delta Width", GroupName="Layout", Order=10)]
		public int PastDeltaWidth { get; set; }
		[NinjaScriptProperty] [Range(0, 500)] [Display(Name="Right Offset", GroupName="Layout", Order=11)]
		public int RightOffset { get; set; }
		[NinjaScriptProperty] [Range(0, 500)] [Display(Name="Separation", GroupName="Layout", Order=12)]
		public int ProfileSeparation { get; set; }
		[NinjaScriptProperty] [Range(0, 10)] [Display(Name="Profile Bar Spacing", GroupName="Layout", Order=13)]
		public int ProfileBarSpacing { get; set; }
		[NinjaScriptProperty] [Range(0, 100)] [Display(Name="Merge Overlap %", GroupName="Logic", Order=14)]
		public int MergeOverlapPercent { get; set; }
		[NinjaScriptProperty] [Display(Name="Mirror Past Profiles", GroupName="Layout", Order=15)]
		public bool MirrorPastProfiles { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Volume", GroupName="Visibility", Order=16)]
		public bool ShowVolume { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Delta", GroupName="Visibility", Order=17)]
		public bool ShowDelta { get; set; }
		[NinjaScriptProperty] [Range(1, 100)] [Display(Name="Value Area %", GroupName="Logic", Order=18)]
		public int ValueAreaPercent { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Current Leg Box", GroupName="Visibility", Order=19)]
		public bool ShowCurrentLegBox { get; set; }
		[NinjaScriptProperty] [Display(Name="Show VWAP", GroupName="Visibility", Order=20)]
		public bool ShowVWAP { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Delta Labels", GroupName="Visibility", Order=21)]
		public bool ShowDeltaLabels { get; set; }
		[NinjaScriptProperty] [Range(5, 50)] [Display(Name="Label Min Height", GroupName="Layout", Order=22)]
		public int DeltaLabelMinHeight { get; set; }
		[NinjaScriptProperty] [Range(5, 24)] [Display(Name="Delta Label Font Size", GroupName="Layout", Order=23)]
		public int DeltaLabelFontSize { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Delta Lbl BG", GroupName="Visibility", Order=24)]
		public bool ShowDeltaLabelBackground { get; set; }

		[XmlIgnore] [Display(Name="Pos Delta Color", GroupName="Colors", Order=20)]
		public System.Windows.Media.Brush PositiveDeltaColor { get; set; }
		[Browsable(false)] public string PositiveDeltaColorSerializable { get { return Serialize.BrushToString(PositiveDeltaColor); } set { PositiveDeltaColor = Serialize.StringToBrush(value); } }
		
		[XmlIgnore] [Display(Name="Neg Delta Color", GroupName="Colors", Order=21)]
		public System.Windows.Media.Brush NegativeDeltaColor { get; set; }
		[Browsable(false)] public string NegativeDeltaColorSerializable { get { return Serialize.BrushToString(NegativeDeltaColor); } set { NegativeDeltaColor = Serialize.StringToBrush(value); } }

		[XmlIgnore] [Display(Name="Vol Color", GroupName="Colors", Order=22)]
		public System.Windows.Media.Brush VolumeColor { get; set; }
		[Browsable(false)] public string VolumeColorSerializable { get { return Serialize.BrushToString(VolumeColor); } set { VolumeColor = Serialize.StringToBrush(value); } }

		[XmlIgnore] [Display(Name="VA Color", GroupName="Colors", Order=23)]
		public System.Windows.Media.Brush ValueAreaColor { get; set; }
		[Browsable(false)] public string ValueAreaColorSerializable { get { return Serialize.BrushToString(ValueAreaColor); } set { ValueAreaColor = Serialize.StringToBrush(value); } }

		[XmlIgnore] [Display(Name="VWAP Color", GroupName="Colors", Order=29)]
		public System.Windows.Media.Brush VWAPColor { get; set; }
		[Browsable(false)] public string VWAPColorSerializable { get { return Serialize.BrushToString(VWAPColor); } set { VWAPColor = Serialize.StringToBrush(value); } }

		[XmlIgnore] [Display(Name="Text Color", GroupName="Colors", Order=30)]
		public System.Windows.Media.Brush TextColor { get; set; }
		[Browsable(false)] public string TextColorSerializable { get { return Serialize.BrushToString(TextColor); } set { TextColor = Serialize.StringToBrush(value); } }

		[XmlIgnore] [Display(Name="Label BG Color", GroupName="Colors", Order=31)]
		public System.Windows.Media.Brush DeltaLabelBackgroundColor { get; set; }
		[Browsable(false)] public string DeltaLabelBackgroundColorSerializable { get { return Serialize.BrushToString(DeltaLabelBackgroundColor); } set { DeltaLabelBackgroundColor = Serialize.StringToBrush(value); } }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private AutoLegProfileNT[] cacheAutoLegProfileNT;
		public AutoLegProfileNT AutoLegProfileNT(int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
		{
			return AutoLegProfileNT(Input, reversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, tickCompression, deltaTickCompression, legsToDisplay, volumeProfileWidth, deltaProfileWidth, pastVolumeWidth, pastDeltaWidth, rightOffset, profileSeparation, profileBarSpacing, mergeOverlapPercent, mirrorPastProfiles, showVolume, showDelta, valueAreaPercent, showCurrentLegBox, showVWAP, showDeltaLabels, deltaLabelMinHeight, deltaLabelFontSize, showDeltaLabelBackground);
		}

		public AutoLegProfileNT AutoLegProfileNT(ISeries<double> input, int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
		{
			if (cacheAutoLegProfileNT != null)
				for (int idx = 0; idx < cacheAutoLegProfileNT.Length; idx++)
					if (cacheAutoLegProfileNT[idx] != null && cacheAutoLegProfileNT[idx].ReversalTicks == reversalTicks && cacheAutoLegProfileNT[idx].MinimumLegTicks == minimumLegTicks && cacheAutoLegProfileNT[idx].MinimumBarsPerLeg == minimumBarsPerLeg && cacheAutoLegProfileNT[idx].MinimumDurationMinutes == minimumDurationMinutes && cacheAutoLegProfileNT[idx].TickCompression == tickCompression && cacheAutoLegProfileNT[idx].DeltaTickCompression == deltaTickCompression && cacheAutoLegProfileNT[idx].LegsToDisplay == legsToDisplay && cacheAutoLegProfileNT[idx].VolumeProfileWidth == volumeProfileWidth && cacheAutoLegProfileNT[idx].DeltaProfileWidth == deltaProfileWidth && cacheAutoLegProfileNT[idx].PastVolumeWidth == pastVolumeWidth && cacheAutoLegProfileNT[idx].PastDeltaWidth == pastDeltaWidth && cacheAutoLegProfileNT[idx].RightOffset == rightOffset && cacheAutoLegProfileNT[idx].ProfileSeparation == profileSeparation && cacheAutoLegProfileNT[idx].ProfileBarSpacing == profileBarSpacing && cacheAutoLegProfileNT[idx].MergeOverlapPercent == mergeOverlapPercent && cacheAutoLegProfileNT[idx].MirrorPastProfiles == mirrorPastProfiles && cacheAutoLegProfileNT[idx].ShowVolume == showVolume && cacheAutoLegProfileNT[idx].ShowDelta == showDelta && cacheAutoLegProfileNT[idx].ValueAreaPercent == valueAreaPercent && cacheAutoLegProfileNT[idx].ShowCurrentLegBox == showCurrentLegBox && cacheAutoLegProfileNT[idx].ShowVWAP == showVWAP && cacheAutoLegProfileNT[idx].ShowDeltaLabels == showDeltaLabels && cacheAutoLegProfileNT[idx].DeltaLabelMinHeight == deltaLabelMinHeight && cacheAutoLegProfileNT[idx].DeltaLabelFontSize == deltaLabelFontSize && cacheAutoLegProfileNT[idx].ShowDeltaLabelBackground == showDeltaLabelBackground && cacheAutoLegProfileNT[idx].EqualsInput(input))
						return cacheAutoLegProfileNT[idx];
			return CacheIndicator<AutoLegProfileNT>(new AutoLegProfileNT(){ ReversalTicks = reversalTicks, MinimumLegTicks = minimumLegTicks, MinimumBarsPerLeg = minimumBarsPerLeg, MinimumDurationMinutes = minimumDurationMinutes, TickCompression = tickCompression, DeltaTickCompression = deltaTickCompression, LegsToDisplay = legsToDisplay, VolumeProfileWidth = volumeProfileWidth, DeltaProfileWidth = deltaProfileWidth, PastVolumeWidth = pastVolumeWidth, PastDeltaWidth = pastDeltaWidth, RightOffset = rightOffset, ProfileSeparation = profileSeparation, ProfileBarSpacing = profileBarSpacing, MergeOverlapPercent = mergeOverlapPercent, MirrorPastProfiles = mirrorPastProfiles, ShowVolume = showVolume, ShowDelta = showDelta, ValueAreaPercent = valueAreaPercent, ShowCurrentLegBox = showCurrentLegBox, ShowVWAP = showVWAP, ShowDeltaLabels = showDeltaLabels, DeltaLabelMinHeight = deltaLabelMinHeight, DeltaLabelFontSize = deltaLabelFontSize, ShowDeltaLabelBackground = showDeltaLabelBackground }, input, ref cacheAutoLegProfileNT);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.AutoLegProfileNT AutoLegProfileNT(int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
		{
			return indicator.AutoLegProfileNT(Input, reversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, tickCompression, deltaTickCompression, legsToDisplay, volumeProfileWidth, deltaProfileWidth, pastVolumeWidth, pastDeltaWidth, rightOffset, profileSeparation, profileBarSpacing, mergeOverlapPercent, mirrorPastProfiles, showVolume, showDelta, valueAreaPercent, showCurrentLegBox, showVWAP, showDeltaLabels, deltaLabelMinHeight, deltaLabelFontSize, showDeltaLabelBackground);
		}

		public Indicators.AutoLegProfileNT AutoLegProfileNT(ISeries<double> input , int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
		{
			return indicator.AutoLegProfileNT(input, reversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, tickCompression, deltaTickCompression, legsToDisplay, volumeProfileWidth, deltaProfileWidth, pastVolumeWidth, pastDeltaWidth, rightOffset, profileSeparation, profileBarSpacing, mergeOverlapPercent, mirrorPastProfiles, showVolume, showDelta, valueAreaPercent, showCurrentLegBox, showVWAP, showDeltaLabels, deltaLabelMinHeight, deltaLabelFontSize, showDeltaLabelBackground);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.AutoLegProfileNT AutoLegProfileNT(int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
		{
			return indicator.AutoLegProfileNT(Input, reversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, tickCompression, deltaTickCompression, legsToDisplay, volumeProfileWidth, deltaProfileWidth, pastVolumeWidth, pastDeltaWidth, rightOffset, profileSeparation, profileBarSpacing, mergeOverlapPercent, mirrorPastProfiles, showVolume, showDelta, valueAreaPercent, showCurrentLegBox, showVWAP, showDeltaLabels, deltaLabelMinHeight, deltaLabelFontSize, showDeltaLabelBackground);
		}

		public Indicators.AutoLegProfileNT AutoLegProfileNT(ISeries<double> input , int reversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int tickCompression, int deltaTickCompression, int legsToDisplay, int volumeProfileWidth, int deltaProfileWidth, int pastVolumeWidth, int pastDeltaWidth, int rightOffset, int profileSeparation, int profileBarSpacing, int mergeOverlapPercent, bool mirrorPastProfiles, bool showVolume, bool showDelta, int valueAreaPercent, bool showCurrentLegBox, bool showVWAP, bool showDeltaLabels, int deltaLabelMinHeight, int deltaLabelFontSize, bool showDeltaLabelBackground)
		{
			return indicator.AutoLegProfileNT(input, reversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, tickCompression, deltaTickCompression, legsToDisplay, volumeProfileWidth, deltaProfileWidth, pastVolumeWidth, pastDeltaWidth, rightOffset, profileSeparation, profileBarSpacing, mergeOverlapPercent, mirrorPastProfiles, showVolume, showDelta, valueAreaPercent, showCurrentLegBox, showVWAP, showDeltaLabels, deltaLabelMinHeight, deltaLabelFontSize, showDeltaLabelBackground);
		}
	}
}

#endregion

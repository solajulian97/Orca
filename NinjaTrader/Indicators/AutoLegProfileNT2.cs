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
	internal enum AutoLegDirection
	{
		Unknown = 0,
		Up = 1,
		Down = -1
	}

	public class AutoLegProfileNT2 : Indicator
	{
		// Data Structures
		private class PriceLeg
		{
			public int StartIndex;
			public int EndIndex;
			public DateTime StartTime;
			public DateTime EndTime;
			public double HighPrice;
			public double LowPrice;
			public AutoLegDirection Direction;
			
			// Stores aggregated ticks based on VolumeTickCompression / DeltaTickCompression
			public Dictionary<double, long> VolByPrice = new Dictionary<double, long>();
			public Dictionary<double, long> DeltaByPrice = new Dictionary<double, long>();
		}

		private LegTracker currentTracker;
		private LegTracker pastTracker;


		private double lastBid = double.NaN;
		private double lastAsk = double.NaN;
		private double prevLast = double.NaN;

		// Rendering resources
		private TextFormat textFormat;
		private SolidColorBrush posBrushDx, negBrushDx, textBrushDx, volBrushDx, labelBgBrushDx, legBoxBrushDx;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "AutoLegProfileNT 2.0";
				Description = "Rotation-based leg delta/volume profile strictly rendering on the right scale edge with past leg support.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;

				// Detection Parameters
				ReversalTicks = 20;
				PastReversalTicks = 40;
				MinimumBarsPerLeg = 1;
				MinimumDurationMinutes = 0;

				// Rendering Parameters
				LegsToDisplay = 3;
				VolumeTickCompression = 4;
				DeltaTickCompression = 10;
				VolumeProfileWidthPx = 150;
				DeltaProfileWidthPx = 100;
				PastVolumeWidthPx = 60;
				PastDeltaWidthPx = 40;
				RightOffsetPx = 60;
				ProfileSeparationPx = 20;
				ProfileBarSpacingPx = 0;
				
				// Visibility Parameters
				ShowVolume = true;
				ShowDelta = true;
				ShowPastDelta = true;
				ShowCurrentLegBox = false;

				// Styling Parameters
				DeltaLabelFontSize = 10;
				ShowDeltaLabelBackground = true;
				VolumeOpacity = 0.6f;
				DeltaOpacity = 0.85f;

				// Brushes (WPF to DX bridging)
				PositiveBrush = WpfBrushes.Lime;
				NegativeBrush = WpfBrushes.Red;
				VolumeBrush = WpfBrushes.RoyalBlue;
				TextBrush = WpfBrushes.White;
				LabelBgBrush = WpfBrushes.Black;
				LegBoxBrush = WpfBrushes.Yellow;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
				currentTracker = new LegTracker(this, ReversalTicks);
				pastTracker = new LegTracker(this, PastReversalTicks > 0 ? PastReversalTicks : ReversalTicks);
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
				volBrushDx?.Dispose();
				labelBgBrushDx?.Dispose();
				legBoxBrushDx?.Dispose();
			}
			catch { }
			finally
			{
				textFormat = null;
				posBrushDx = null;
				negBrushDx = null;
				textBrushDx = null;
				volBrushDx = null;
				labelBgBrushDx = null;
				legBoxBrushDx = null;
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDx();
			base.OnRenderTargetChanged();
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
				if (CurrentBar < 1) return;

				double last = Close[0];
				long vol = (long)Volume[0];
				DateTime time = Time[0];
				
				int primaryBarIndex = BarsArray[0].Count - 1;
				if (primaryBarIndex < 0) primaryBarIndex = 0;

				long signedVol = 0;
				if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0 && lastBid > 0 && lastAsk >= lastBid)
				{
					if (last >= lastAsk) signedVol = +vol;
					else if (last <= lastBid) signedVol = -vol;
					else if (!double.IsNaN(prevLast)) signedVol = (last > prevLast) ? +vol : (last < prevLast ? -vol : 0);
				}
				else if (!double.IsNaN(prevLast))
				{
					signedVol = (last > prevLast) ? +vol : (last < prevLast ? -vol : 0);
				}
				prevLast = last;

				if (vol <= 0) return;

				currentTracker.ProcessBarUpdate(last, vol, signedVol, time, primaryBarIndex);
				pastTracker.ProcessBarUpdate(last, vol, signedVol, time, primaryBarIndex);

				// Force chart repaint on every tick so profiles update in real time
				if (State == State.Realtime)
					ForceRefresh();
			}
			else if (BarsInProgress == 0) // Primary Series
			{
				if (currentTracker != null && currentTracker.CurrentLeg != null && CurrentBar > 0)
					currentTracker.CurrentLeg.EndIndex = CurrentBar;
				if (pastTracker != null && pastTracker.CurrentLeg != null && CurrentBar > 0)
					pastTracker.CurrentLeg.EndIndex = CurrentBar;
			}
		}

		private class LegTracker
		{
			private AutoLegProfileNT2 parent;
			public int TickReversalThreshold;
			
			public List<PriceLeg> CompletedLegs = new List<PriceLeg>();
			public PriceLeg CurrentLeg;

			private double currentExtremePrice = double.NaN;
			private int currentExtremeBar = -1;
			private DateTime currentExtremeTime;
			private AutoLegDirection legDir = AutoLegDirection.Unknown;

			// Buffer of ticks since the last swing extreme — replayed into new leg on reversal
			private struct TickRecord
			{
				public double Price;
				public long Volume;
				public long SignedVolume;
				public DateTime Time;
			}
			private List<TickRecord> ticksSinceExtreme = new List<TickRecord>();

			public LegTracker(AutoLegProfileNT2 indicator, int reversalTicks)
			{
				parent = indicator;
				TickReversalThreshold = reversalTicks;
			}

			public void ProcessBarUpdate(double last, long vol, long signedVol, DateTime time, int primaryBarIndex)
			{
				if (CurrentLeg == null)
				{
					StartNewLegAtCurrentTick(AutoLegDirection.Up, last, time, primaryBarIndex);
					return;
				}

				// Track extremes
				bool newExtremeFound = false;

				if (legDir == AutoLegDirection.Up || legDir == AutoLegDirection.Unknown)
				{
					if (double.IsNaN(currentExtremePrice) || last >= currentExtremePrice)
					{
						currentExtremePrice = last;
						currentExtremeTime = time;
						newExtremeFound = true;
					}
				}
				if (legDir == AutoLegDirection.Down || legDir == AutoLegDirection.Unknown)
				{
					if (double.IsNaN(currentExtremePrice) || last <= currentExtremePrice)
					{
						currentExtremePrice = last;
						currentExtremeTime = time;
						newExtremeFound = true;
					}
				}

				// If new extreme found, reset the post-extreme buffer
				if (newExtremeFound)
				{
					ticksSinceExtreme.Clear();
				}

				// Buffer this tick (used to seed new leg on reversal)
				ticksSinceExtreme.Add(new TickRecord { Price = last, Volume = vol, SignedVolume = signedVol, Time = time });

				// Check for reversal (only when NOT a new extreme)
				if (!newExtremeFound)
				{
					double reversalThreshold = TickReversalThreshold * parent.TickSize;
					bool durationMet = parent.MinimumDurationMinutes == 0 || (time - CurrentLeg.StartTime).TotalMinutes >= parent.MinimumDurationMinutes;
					
					if (durationMet)
					{
						if (legDir == AutoLegDirection.Up && (currentExtremePrice - last) >= reversalThreshold)
						{
							HandleReversalTick(AutoLegDirection.Down, last, time, primaryBarIndex);
							return; // New leg was created and seeded — skip the ProcessTickToLeg below
						}
						else if (legDir == AutoLegDirection.Down && (last - currentExtremePrice) >= reversalThreshold)
						{
							HandleReversalTick(AutoLegDirection.Up, last, time, primaryBarIndex);
							return; // New leg was created and seeded — skip the ProcessTickToLeg below
						}
					}
				}

				// ALWAYS process every tick into the current leg immediately (no buffering)
				ProcessTickToLeg(CurrentLeg, last, vol, signedVol, time);
				CurrentLeg.HighPrice = Math.Max(CurrentLeg.HighPrice, last);
				CurrentLeg.LowPrice = Math.Min(CurrentLeg.LowPrice, last);
				CurrentLeg.EndTime = time;
			}

			private void HandleReversalTick(AutoLegDirection newDir, double currentTickPrice, DateTime time, int primaryBarIndex)
			{
				if (Math.Abs(CurrentLeg.HighPrice - CurrentLeg.LowPrice) / parent.TickSize >= parent.MinimumLegTicks)
				{
					CompletedLegs.Add(CurrentLeg);
					if (CompletedLegs.Count > parent.LegsToDisplay)
					{
						CompletedLegs.RemoveAt(0);
					}
				}

				legDir = newDir;
				CurrentLeg = new PriceLeg
				{
					StartIndex = currentExtremeBar > -1 ? currentExtremeBar : primaryBarIndex,
					StartTime = currentExtremeTime,
					EndIndex = primaryBarIndex,
					EndTime = time,
					HighPrice = currentExtremePrice,
					LowPrice = currentExtremePrice,
					Direction = newDir
				};

				// Replay all buffered ticks since the swing extreme into the new leg
				// This fills in the data from the swing point to now
				foreach (var t in ticksSinceExtreme)
				{
					ProcessTickToLeg(CurrentLeg, t.Price, t.Volume, t.SignedVolume, t.Time);
					CurrentLeg.HighPrice = Math.Max(CurrentLeg.HighPrice, t.Price);
					CurrentLeg.LowPrice = Math.Min(CurrentLeg.LowPrice, t.Price);
					CurrentLeg.EndTime = t.Time;
				}
				
				ticksSinceExtreme.Clear();
				currentExtremePrice = currentTickPrice;
				currentExtremeBar = primaryBarIndex;
				currentExtremeTime = time;
			}

			private void StartNewLegAtCurrentTick(AutoLegDirection dir, double last, DateTime time, int primaryBarIndex)
			{
				legDir = dir;
				currentExtremePrice = last;
				currentExtremeBar = primaryBarIndex;
				currentExtremeTime = time;

				CurrentLeg = new PriceLeg
				{
					StartIndex = primaryBarIndex,
					StartTime = time,
					EndIndex = primaryBarIndex,
					EndTime = time,
					HighPrice = last,
					LowPrice = last,
					Direction = dir
				};
			}



			private void ProcessTickToLeg(PriceLeg targetLeg, double price, long vol, long signedVol, DateTime time)
			{
				if (targetLeg == null || vol <= 0) return;

				double volComp = parent.VolumeTickCompression * parent.TickSize;
				double roundedVolPrice = Math.Floor(price / volComp + 0.000001) * volComp;

				if (targetLeg.VolByPrice.TryGetValue(roundedVolPrice, out long vExisting))
					targetLeg.VolByPrice[roundedVolPrice] = vExisting + vol;
				else
					targetLeg.VolByPrice[roundedVolPrice] = vol;

				if (signedVol == 0) return;

				// Delta - use same compression as volume so levels align
				double deltaComp = parent.VolumeTickCompression * parent.TickSize;
				double roundedDeltaPrice = Math.Floor(price / deltaComp + 0.000001) * deltaComp;

				if (targetLeg.DeltaByPrice.TryGetValue(roundedDeltaPrice, out long dExisting))
					targetLeg.DeltaByPrice[roundedDeltaPrice] = dExisting + signedVol;
				else
					targetLeg.DeltaByPrice[roundedDeltaPrice] = signedVol;
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (currentTracker == null || currentTracker.CurrentLeg == null) return;

			EnsureDxResources();
			
			NinjaTrader.Gui.Chart.ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];

			// Anchored to right price scale
			float rightmostEdge = chartControl.CanvasRight - RightOffsetPx - VolumeProfileWidthPx;
			
			// Current Leg (Anchored Right)
			DrawLegProfiles(chartControl, chartScale, panel, currentTracker.CurrentLeg, rightmostEdge, VolumeProfileWidthPx, DeltaProfileWidthPx, true, false);

			// Historical Legs (Anchored to their origin time) from pastTracker
			if (LegsToDisplay > 0 && pastTracker != null && pastTracker.CompletedLegs.Count > 0)
			{
				for (int i = pastTracker.CompletedLegs.Count - 1; i >= 0; i--)
				{
					PriceLeg leg = pastTracker.CompletedLegs[i];
					float originX = chartControl.GetXByTime(leg.StartTime);
					bool hideDelta = !ShowPastDelta;
					
					DrawLegProfiles(chartControl, chartScale, panel, leg, originX, PastVolumeWidthPx, PastDeltaWidthPx, false, hideDelta);
				}
			}
		}

		private void DrawLegProfiles(ChartControl chartControl, ChartScale chartScale, NinjaTrader.Gui.Chart.ChartPanel panel, PriceLeg leg, float originX, int vWidth, int dWidth, bool isCurrent, bool forceHideDelta)
		{
			if (ShowCurrentLegBox && isCurrent)
			{
				int topY = chartScale.GetYByValue(leg.HighPrice);
				int bottomY = chartScale.GetYByValue(leg.LowPrice);
				int totalWidth = vWidth + dWidth;
				float boxX = originX - dWidth;
				RenderTarget.DrawRectangle(new RectangleF(boxX - 5, topY - 5, totalWidth + 10, (bottomY - topY) + 10), legBoxBrushDx, 1f);
			}

			// Shared Spine
			float spineX = originX;

			// Draw Volume Overlay Layers Back to Front
			if (ShowVolume)
			{
				long maxVol = leg.VolByPrice.Values.DefaultIfEmpty(0).Max();
				if (maxVol > 0)
				{
					foreach (var kvp in leg.VolByPrice)
					{
						int yTop = chartScale.GetYByValue(kvp.Key);
						// Cull OOB
						if (yTop < panel.Y - 50 || yTop > panel.Y + panel.H + 50) continue;

						int yBot = chartScale.GetYByValue(kvp.Key - (VolumeTickCompression * TickSize));
						int height = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);

						float w = (float)(vWidth * (kvp.Value / (double)maxVol));
						if (w > 0.5f)
						{
							// Volume draws to the right of the spine
							RenderTarget.FillRectangle(new RectangleF(spineX, Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f, w, height), volBrushDx);
						}
					}
				}
			}

			// Draw Delta Overlay Layers (aggregate fine-grained delta rows into DeltaTickCompression buckets)
			if (ShowDelta && !forceHideDelta && leg.DeltaByPrice.Count > 0)
			{
				// Build grouped delta buckets from the fine-grained data
				double deltaComp = DeltaTickCompression * TickSize;
				var groupedDelta = new Dictionary<double, long>();
				foreach (var kvp in leg.DeltaByPrice)
				{
					double bucketPrice = Math.Floor(kvp.Key / deltaComp + 0.000001) * deltaComp;
					if (groupedDelta.TryGetValue(bucketPrice, out long existing))
						groupedDelta[bucketPrice] = existing + kvp.Value;
					else
						groupedDelta[bucketPrice] = kvp.Value;
				}

				long maxAbsDelta = groupedDelta.Values.Select(v => Math.Abs(v)).DefaultIfEmpty(0).Max();
				if (maxAbsDelta > 0)
				{
					foreach (var kvp in groupedDelta)
					{
						int yTop = chartScale.GetYByValue(kvp.Key + deltaComp);
						if (yTop < panel.Y - 50 || yTop > panel.Y + panel.H + 50) continue;

						int yBot = chartScale.GetYByValue(kvp.Key);
						int height = Math.Max(1, Math.Abs(yBot - yTop) - ProfileBarSpacingPx);
						float drawY = Math.Min(yTop, yBot) + ProfileBarSpacingPx / 2f;

						float w = (float)(dWidth * (Math.Abs(kvp.Value) / (double)maxAbsDelta));
						if (w > 0.5f)
						{
							// Delta draws to the left of the spine
							RectangleF rect = new RectangleF(spineX - w, drawY, w, height);
							
							RenderTarget.FillRectangle(rect, kvp.Value >= 0 ? posBrushDx : negBrushDx);

							if (height >= (DeltaLabelFontSize + 2))
							{
								string lbl = kvp.Value.ToString("+#;-#;0");
								float textWidth = MeasureTextWidth(lbl);
								
								float tX = spineX - textWidth - 2;
								float tY = drawY + (height / 2f) - (DeltaLabelFontSize / 2f);

								if (ShowDeltaLabelBackground)
								{
									RenderTarget.FillRectangle(new RectangleF(tX - 1, tY - 1, textWidth + 2, DeltaLabelFontSize + 2), labelBgBrushDx);
								}

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
			if (textFormat == null) 
			{
				textFormat = new TextFormat(Core.Globals.DirectWriteFactory, "Arial", DeltaLabelFontSize)
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Center,
					ParagraphAlignment = ParagraphAlignment.Center
				};
			}
		}

		private float MeasureTextWidth(string text)
		{
			if (textFormat == null) return 0f;
			using (var layout = new TextLayout(Core.Globals.DirectWriteFactory, text, textFormat, 1000, 100))
			{
				return layout.Metrics.Width;
			}
		}

		private static System.Windows.Media.Color BrushToMediaColor(WpfBrush b)
		{
			return (b as WpfSolidColorBrush)?.Color ?? WpfColors.White;
		}

		private Color4 ToDx(WpfBrush b, float alphaMult)
		{
			var c = BrushToMediaColor(b);
			return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (c.A / 255f) * alphaMult);
		}

		#region Properties
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Current Reversal Ticks", GroupName="Leg Detection", Order=0)]
		public int ReversalTicks { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Past Reversal Ticks", GroupName="Leg Detection", Order=1)]
		public int PastReversalTicks { get; set; }

		[NinjaScriptProperty] [Range(0, int.MaxValue)] [Display(Name="Min Leg Ticks", GroupName="Leg Detection", Order=2)]
		public int MinimumLegTicks { get; set; }

		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Min Bars Per Leg", GroupName="Leg Detection", Order=2)]
		public int MinimumBarsPerLeg { get; set; }

		[NinjaScriptProperty] [Range(0, 1440)] [Display(Name="Min Duration (Min)", GroupName="Leg Detection", Order=3)]
		public int MinimumDurationMinutes { get; set; }

		[NinjaScriptProperty] [Range(0, 50)] [Display(Name="Legs To Display", GroupName="Layout", Order=4)]
		public int LegsToDisplay { get; set; }

		[NinjaScriptProperty] [Range(1, 100)] [Display(Name="Vol Compression (Ticks)", GroupName="Layout", Order=5)]
		public int VolumeTickCompression { get; set; }

		[NinjaScriptProperty] [Range(1, 100)] [Display(Name="Delta Compression (Ticks)", GroupName="Layout", Order=6)]
		public int DeltaTickCompression { get; set; }

		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Vol Width", GroupName="Layout", Order=7)]
		public int VolumeProfileWidthPx { get; set; }

		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Delta Width", GroupName="Layout", Order=8)]
		public int DeltaProfileWidthPx { get; set; }

		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Past Vol Width", GroupName="Layout", Order=9)]
		public int PastVolumeWidthPx { get; set; }

		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Past Delta Width", GroupName="Layout", Order=10)]
		public int PastDeltaWidthPx { get; set; }

		[NinjaScriptProperty] [Range(-500, 500)] [Display(Name="Right Offset (px)", GroupName="Layout", Order=11)]
		public int RightOffsetPx { get; set; }

		[NinjaScriptProperty] [Range(0, 500)] [Display(Name="Separation", GroupName="Layout", Order=12)]
		public int ProfileSeparationPx { get; set; }

		[NinjaScriptProperty] [Range(0, 10)] [Display(Name="Profile Bar Spacing", GroupName="Layout", Order=13)]
		public int ProfileBarSpacingPx { get; set; }

		[NinjaScriptProperty] [Display(Name="Show Volume", GroupName="Visibility", Order=14)]
		public bool ShowVolume { get; set; }

		[NinjaScriptProperty] [Display(Name="Show Delta", GroupName="Visibility", Order=15)]
		public bool ShowDelta { get; set; }

		[NinjaScriptProperty] [Display(Name="Show Past Delta", GroupName="Visibility", Order=16)]
		public bool ShowPastDelta { get; set; }

		[NinjaScriptProperty] [Display(Name="Show Current Leg Box", GroupName="Visibility", Order=17)]
		public bool ShowCurrentLegBox { get; set; }

		[NinjaScriptProperty] [Range(5, 50)] [Display(Name="Delta Label Font Size", GroupName="Visibility", Order=18)]
		public int DeltaLabelFontSize { get; set; }

		[NinjaScriptProperty] [Display(Name="Show Delta Lbl BG", GroupName="Visibility", Order=19)]
		public bool ShowDeltaLabelBackground { get; set; }

		[NinjaScriptProperty] [Range(0.1, 1.0)] [Display(Name="Volume Opacity", GroupName="Colors", Order=22)]
		public float VolumeOpacity { get; set; }

		[NinjaScriptProperty] [Range(0.1, 1.0)] [Display(Name="Delta Opacity", GroupName="Colors", Order=23)]
		public float DeltaOpacity { get; set; }

		[XmlIgnore] [Display(Name="Pos Delta Color", GroupName="Colors", Order=24)]
		public WpfBrush PositiveBrush { get; set; }
		[Browsable(false)] public string PositiveBrushSerialize { get { return Serialize.BrushToString(PositiveBrush); } set { PositiveBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore] [Display(Name="Neg Delta Color", GroupName="Colors", Order=25)]
		public WpfBrush NegativeBrush { get; set; }
		[Browsable(false)] public string NegativeBrushSerialize { get { return Serialize.BrushToString(NegativeBrush); } set { NegativeBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore] [Display(Name="Vol Color", GroupName="Colors", Order=26)]
		public WpfBrush VolumeBrush { get; set; }
		[Browsable(false)] public string VolumeBrushSerialize { get { return Serialize.BrushToString(VolumeBrush); } set { VolumeBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore] [Display(Name="Text Color", GroupName="Colors", Order=27)]
		public WpfBrush TextBrush { get; set; }
		[Browsable(false)] public string TextBrushSerialize { get { return Serialize.BrushToString(TextBrush); } set { TextBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore] [Display(Name="Label BG Color", GroupName="Colors", Order=28)]
		public WpfBrush LabelBgBrush { get; set; }
		[Browsable(false)] public string LabelBgBrushSerialize { get { return Serialize.BrushToString(LabelBgBrush); } set { LabelBgBrush = Serialize.StringToBrush(value); } }

		[XmlIgnore] [Display(Name="Leg Box Color", GroupName="Colors", Order=29)]
		public WpfBrush LegBoxBrush { get; set; }
		[Browsable(false)] public string LegBoxBrushSerialize { get { return Serialize.BrushToString(LegBoxBrush); } set { LegBoxBrush = Serialize.StringToBrush(value); } }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private AutoLegProfileNT2[] cacheAutoLegProfileNT2;
		public AutoLegProfileNT2 AutoLegProfileNT2(int reversalTicks, int pastReversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, float volumeOpacity, float deltaOpacity)
		{
			return AutoLegProfileNT2(Input, reversalTicks, pastReversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, volumeOpacity, deltaOpacity);
		}

		public AutoLegProfileNT2 AutoLegProfileNT2(ISeries<double> input, int reversalTicks, int pastReversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, float volumeOpacity, float deltaOpacity)
		{
			if (cacheAutoLegProfileNT2 != null)
				for (int idx = 0; idx < cacheAutoLegProfileNT2.Length; idx++)
					if (cacheAutoLegProfileNT2[idx] != null && cacheAutoLegProfileNT2[idx].ReversalTicks == reversalTicks && cacheAutoLegProfileNT2[idx].PastReversalTicks == pastReversalTicks && cacheAutoLegProfileNT2[idx].MinimumLegTicks == minimumLegTicks && cacheAutoLegProfileNT2[idx].MinimumBarsPerLeg == minimumBarsPerLeg && cacheAutoLegProfileNT2[idx].MinimumDurationMinutes == minimumDurationMinutes && cacheAutoLegProfileNT2[idx].LegsToDisplay == legsToDisplay && cacheAutoLegProfileNT2[idx].VolumeTickCompression == volumeTickCompression && cacheAutoLegProfileNT2[idx].DeltaTickCompression == deltaTickCompression && cacheAutoLegProfileNT2[idx].VolumeProfileWidthPx == volumeProfileWidthPx && cacheAutoLegProfileNT2[idx].DeltaProfileWidthPx == deltaProfileWidthPx && cacheAutoLegProfileNT2[idx].PastVolumeWidthPx == pastVolumeWidthPx && cacheAutoLegProfileNT2[idx].PastDeltaWidthPx == pastDeltaWidthPx && cacheAutoLegProfileNT2[idx].RightOffsetPx == rightOffsetPx && cacheAutoLegProfileNT2[idx].ProfileSeparationPx == profileSeparationPx && cacheAutoLegProfileNT2[idx].ProfileBarSpacingPx == profileBarSpacingPx && cacheAutoLegProfileNT2[idx].ShowVolume == showVolume && cacheAutoLegProfileNT2[idx].ShowDelta == showDelta && cacheAutoLegProfileNT2[idx].ShowPastDelta == showPastDelta && cacheAutoLegProfileNT2[idx].ShowCurrentLegBox == showCurrentLegBox && cacheAutoLegProfileNT2[idx].DeltaLabelFontSize == deltaLabelFontSize && cacheAutoLegProfileNT2[idx].ShowDeltaLabelBackground == showDeltaLabelBackground && cacheAutoLegProfileNT2[idx].VolumeOpacity == volumeOpacity && cacheAutoLegProfileNT2[idx].DeltaOpacity == deltaOpacity && cacheAutoLegProfileNT2[idx].EqualsInput(input))
						return cacheAutoLegProfileNT2[idx];
			return CacheIndicator<AutoLegProfileNT2>(new AutoLegProfileNT2(){ ReversalTicks = reversalTicks, PastReversalTicks = pastReversalTicks, MinimumLegTicks = minimumLegTicks, MinimumBarsPerLeg = minimumBarsPerLeg, MinimumDurationMinutes = minimumDurationMinutes, LegsToDisplay = legsToDisplay, VolumeTickCompression = volumeTickCompression, DeltaTickCompression = deltaTickCompression, VolumeProfileWidthPx = volumeProfileWidthPx, DeltaProfileWidthPx = deltaProfileWidthPx, PastVolumeWidthPx = pastVolumeWidthPx, PastDeltaWidthPx = pastDeltaWidthPx, RightOffsetPx = rightOffsetPx, ProfileSeparationPx = profileSeparationPx, ProfileBarSpacingPx = profileBarSpacingPx, ShowVolume = showVolume, ShowDelta = showDelta, ShowPastDelta = showPastDelta, ShowCurrentLegBox = showCurrentLegBox, DeltaLabelFontSize = deltaLabelFontSize, ShowDeltaLabelBackground = showDeltaLabelBackground, VolumeOpacity = volumeOpacity, DeltaOpacity = deltaOpacity }, input, ref cacheAutoLegProfileNT2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.AutoLegProfileNT2 AutoLegProfileNT2(int reversalTicks, int pastReversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, float volumeOpacity, float deltaOpacity)
		{
			return indicator.AutoLegProfileNT2(Input, reversalTicks, pastReversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, volumeOpacity, deltaOpacity);
		}

		public Indicators.AutoLegProfileNT2 AutoLegProfileNT2(ISeries<double> input , int reversalTicks, int pastReversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, float volumeOpacity, float deltaOpacity)
		{
			return indicator.AutoLegProfileNT2(input, reversalTicks, pastReversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, volumeOpacity, deltaOpacity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.AutoLegProfileNT2 AutoLegProfileNT2(int reversalTicks, int pastReversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, float volumeOpacity, float deltaOpacity)
		{
			return indicator.AutoLegProfileNT2(Input, reversalTicks, pastReversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, volumeOpacity, deltaOpacity);
		}

		public Indicators.AutoLegProfileNT2 AutoLegProfileNT2(ISeries<double> input , int reversalTicks, int pastReversalTicks, int minimumLegTicks, int minimumBarsPerLeg, int minimumDurationMinutes, int legsToDisplay, int volumeTickCompression, int deltaTickCompression, int volumeProfileWidthPx, int deltaProfileWidthPx, int pastVolumeWidthPx, int pastDeltaWidthPx, int rightOffsetPx, int profileSeparationPx, int profileBarSpacingPx, bool showVolume, bool showDelta, bool showPastDelta, bool showCurrentLegBox, int deltaLabelFontSize, bool showDeltaLabelBackground, float volumeOpacity, float deltaOpacity)
		{
			return indicator.AutoLegProfileNT2(input, reversalTicks, pastReversalTicks, minimumLegTicks, minimumBarsPerLeg, minimumDurationMinutes, legsToDisplay, volumeTickCompression, deltaTickCompression, volumeProfileWidthPx, deltaProfileWidthPx, pastVolumeWidthPx, pastDeltaWidthPx, rightOffsetPx, profileSeparationPx, profileBarSpacingPx, showVolume, showDelta, showPastDelta, showCurrentLegBox, deltaLabelFontSize, showDeltaLabelBackground, volumeOpacity, deltaOpacity);
		}
	}
}

#endregion

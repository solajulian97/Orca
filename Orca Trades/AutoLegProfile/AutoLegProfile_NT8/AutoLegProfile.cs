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
	public class AutoLegProfile : Indicator
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

			public void ResetData()
			{
				VolumeProfileData.Clear();
				DeltaProfileData.Clear();
				VwapPoints.Clear();
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
				Name										= "AutoLegProfile";
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
				TickCompression								= 4;
				DeltaTickCompression						= 10;
				LegsToDisplay								= 10;
				VolumeProfileWidth							= 150;
				DeltaProfileWidth							= 100;
				PastVolumeWidth								= 60;
				PastDeltaWidth								= 40;
				RightOffset									= 60;
				ProfileSeparation							= 20;
				MergeOverlapPercent							= 80;
				MirrorPastProfiles							= true;
				ShowVolume									= true;
				ShowDelta									= true;
				ValueAreaPercent							= 70;
				ShowVWAP									= true;
				ShowDeltaLabels								= true;
				DeltaLabelMinHeight							= 12;
				
				// Colors
				PositiveDeltaColor							= Brushes.Lime;
				NegativeDeltaColor							= Brushes.Red;
				VolumeColor									= Brushes.RoyalBlue;
				ValueAreaColor								= Brushes.Gray;
				VWAPColor									= Brushes.Magenta;
				TextColor									= Brushes.White;
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
			if (textFormat != null) textFormat.Dispose();
			
			volumeBrush = null;
			valueAreaBrush = null;
			positiveDeltaBrush = null;
			negativeDeltaBrush = null;
			vwapBrush = null;
			textBrush = null;
			textFormat = null;
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) return;

			double high = High[0];
			double low = Low[0];
			double close = Close[0];
			double volume = Volume[0];
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

			// Accumulate data
			if (!reversalDetected && tickVolume > 0)
			{
				AccumulateTick(currentLeg, close, tickVolume); 
				currentLeg.HighPrice = Math.Max(currentLeg.HighPrice, high);
				currentLeg.LowPrice = Math.Min(currentLeg.LowPrice, low);
				currentLeg.EndIndex = CurrentBar;
				currentLeg.EndTime = time;
				currentLeg.EndPrice = close;
				
				UpdateVWAP(currentLeg, close, tickVolume, time);
				UpdateLegStats(currentLeg);
			}
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
		}

		private void AccumulateTick(PriceLeg leg, double price, double volume)
		{
			// Volume Profile
			double comp = TickCompression * TickSize;
			double rounded = Math.Round(price / comp) * comp;
			if (!leg.VolumeProfileData.ContainsKey(rounded)) leg.VolumeProfileData[rounded] = new LevelData();
			leg.VolumeProfileData[rounded].Volume += volume;

			// Delta Profile - use same compression as volume so levels align
			double dComp = TickCompression * TickSize;
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

			// Brushes setup
			if (volumeBrush == null) volumeBrush = VolumeColor.ToDxBrush(RenderTarget);
			if (valueAreaBrush == null) valueAreaBrush = ValueAreaColor.ToDxBrush(RenderTarget);
			if (positiveDeltaBrush == null) positiveDeltaBrush = PositiveDeltaColor.ToDxBrush(RenderTarget);
			if (negativeDeltaBrush == null) negativeDeltaBrush = NegativeDeltaColor.ToDxBrush(RenderTarget);
			if (vwapBrush == null) vwapBrush = VWAPColor.ToDxBrush(RenderTarget);
			if (textBrush == null) textBrush = TextColor.ToDxBrush(RenderTarget);
			if (textFormat == null) textFormat = new TextFormat(new SharpDX.DirectWrite.Factory(), "Arial", 12f);

			foreach (var leg in completedLegs) DrawLeg(leg, false, chartControl, chartScale);
			if (currentLeg != null) DrawLeg(currentLeg, true, chartControl, chartScale);
		}

		private void DrawLeg(PriceLeg leg, bool isCurrent, ChartControl chartControl, ChartScale chartScale)
		{
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
				double targetVol = leg.LegTotalVolume * (ValueAreaPercent / 100.0);
				double accVol = 0;
				var levels = leg.VolumeProfileData.OrderByDescending(k => k.Value.Volume).ToList();
				HashSet<double> vaLevels = new HashSet<double>();
				foreach (var lev in levels) { if (accVol < targetVol) { vaLevels.Add(lev.Key); accVol += lev.Value.Volume; } }

				foreach (var kvp in leg.VolumeProfileData)
				{
					int y = chartScale.GetYByValue(kvp.Key);
					int nextY = chartScale.GetYByValue(kvp.Key + (TickCompression * TickSize));
					int h = Math.Max(2, Math.Abs(nextY - y));
					int w = (int)((kvp.Value.Volume / leg.MaxVolume) * volWidth);
					int x = vDir == 1 ? vOrigin : vOrigin - w;
					RenderTarget.FillRectangle(new RectangleF(x, y - h, w, h), vaLevels.Contains(kvp.Key) ? valueAreaBrush : volumeBrush);
				}
			}

			// Draw Delta
			if (ShowDelta && leg.MaxDeltaAbs > 0)
			{
				foreach (var kvp in leg.DeltaProfileData)
				{
					int y = chartScale.GetYByValue(kvp.Key);
					int nextY = chartScale.GetYByValue(kvp.Key + (TickCompression * TickSize));
					int h = Math.Max(2, Math.Abs(nextY - y));
					int w = (int)((Math.Abs(kvp.Value.Delta) / leg.MaxDeltaAbs) * dWidth);
					int x = dDir == 1 ? dOrigin : dOrigin - w;
					RenderTarget.FillRectangle(new RectangleF(x, y - h, w, h), kvp.Value.Delta >= 0 ? positiveDeltaBrush : negativeDeltaBrush);

					if (ShowDeltaLabels && h >= DeltaLabelMinHeight && Math.Abs(kvp.Value.Delta) > 0)
					{
						string lbl = kvp.Value.Delta.ToString("+#;-#;0");
						float tx = isCurrent ? dOrigin + 2 : (dDir == 1 ? x + w + 2 : x - 30);
						RenderTarget.DrawText(lbl, textFormat, new RectangleF(tx, y - h, 50, h), textBrush);
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
		}

		#region Properties
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Reversal Ticks", GroupName="Parameters", Order=0)]
		public int ReversalTicks { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Min Leg Ticks", GroupName="Parameters", Order=1)]
		public int MinimumLegTicks { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Vol Compression", GroupName="Parameters", Order=2)]
		public int TickCompression { get; set; }
		[NinjaScriptProperty] [Range(1, int.MaxValue)] [Display(Name="Delta Compression", GroupName="Parameters", Order=3)]
		public int DeltaTickCompression { get; set; }
		[NinjaScriptProperty] [Range(1, 50)] [Display(Name="Legs To Display", GroupName="Parameters", Order=4)]
		public int LegsToDisplay { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Vol Width", GroupName="Layout", Order=5)]
		public int VolumeProfileWidth { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Delta Width", GroupName="Layout", Order=6)]
		public int DeltaProfileWidth { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Past Vol Width", GroupName="Layout", Order=7)]
		public int PastVolumeWidth { get; set; }
		[NinjaScriptProperty] [Range(10, 500)] [Display(Name="Past Delta Width", GroupName="Layout", Order=8)]
		public int PastDeltaWidth { get; set; }
		[NinjaScriptProperty] [Range(0, 500)] [Display(Name="Right Offset", GroupName="Layout", Order=9)]
		public int RightOffset { get; set; }
		[NinjaScriptProperty] [Range(0, 500)] [Display(Name="Separation", GroupName="Layout", Order=10)]
		public int ProfileSeparation { get; set; }
		[NinjaScriptProperty] [Range(0, 100)] [Display(Name="Merge Overlap %", GroupName="Logic", Order=11)]
		public int MergeOverlapPercent { get; set; }
		[NinjaScriptProperty] [Display(Name="Mirror Past Profiles", GroupName="Layout", Order=12)]
		public bool MirrorPastProfiles { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Volume", GroupName="Visibility", Order=13)]
		public bool ShowVolume { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Delta", GroupName="Visibility", Order=14)]
		public bool ShowDelta { get; set; }
		[NinjaScriptProperty] [Range(1, 100)] [Display(Name="Value Area %", GroupName="Logic", Order=15)]
		public int ValueAreaPercent { get; set; }
		[NinjaScriptProperty] [Display(Name="Show VWAP", GroupName="Visibility", Order=16)]
		public bool ShowVWAP { get; set; }
		[NinjaScriptProperty] [Display(Name="Show Delta Labels", GroupName="Visibility", Order=17)]
		public bool ShowDeltaLabels { get; set; }
		[NinjaScriptProperty] [Range(5, 50)] [Display(Name="Label Min Height", GroupName="Layout", Order=18)]
		public int DeltaLabelMinHeight { get; set; }

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

		[XmlIgnore] [Display(Name="VWAP Color", GroupName="Colors", Order=24)]
		public System.Windows.Media.Brush VWAPColor { get; set; }
		[Browsable(false)] public string VWAPColorSerializable { get { return Serialize.BrushToString(VWAPColor); } set { VWAPColor = Serialize.StringToBrush(value); } }

		[XmlIgnore] [Display(Name="Text Color", GroupName="Colors", Order=25)]
		public System.Windows.Media.Brush TextColor { get; set; }
		[Browsable(false)] public string TextColorSerializable { get { return Serialize.BrushToString(TextColor); } set { TextColor = Serialize.StringToBrush(value); } }
		#endregion
	}
}

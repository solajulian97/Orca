using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators;

public class AutoLegProfileNT2 : Indicator
{
	private class PriceLeg
	{
		public int StartIndex;

		public int EndIndex;

		public DateTime StartTime;

		public DateTime EndTime;

		public double HighPrice;

		public double LowPrice;

		public AutoLegDirection Direction;

		public Dictionary<double, long> VolByPrice = new Dictionary<double, long>();

		public Dictionary<double, long> DeltaByPrice = new Dictionary<double, long>();
	}

	private class LegTracker
	{
		private struct TickRecord
		{
			public double Price;

			public long Volume;

			public long SignedVolume;

			public DateTime Time;
		}

		private AutoLegProfileNT2 parent;

		public int TickReversalThreshold;

		public List<PriceLeg> CompletedLegs = new List<PriceLeg>();

		public PriceLeg CurrentLeg;

		private double currentExtremePrice = double.NaN;

		private int currentExtremeBar = -1;

		private DateTime currentExtremeTime;

		private AutoLegDirection legDir;

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
			bool flag = false;
			if ((legDir == AutoLegDirection.Up || legDir == AutoLegDirection.Unknown) && (double.IsNaN(currentExtremePrice) || last >= currentExtremePrice))
			{
				currentExtremePrice = last;
				currentExtremeTime = time;
				flag = true;
			}
			if ((legDir == AutoLegDirection.Down || legDir == AutoLegDirection.Unknown) && (double.IsNaN(currentExtremePrice) || last <= currentExtremePrice))
			{
				currentExtremePrice = last;
				currentExtremeTime = time;
				flag = true;
			}
			if (flag)
			{
				ticksSinceExtreme.Clear();
			}
			ticksSinceExtreme.Add(new TickRecord
			{
				Price = last,
				Volume = vol,
				SignedVolume = signedVol,
				Time = time
			});
			if (!flag)
			{
				double num = (double)TickReversalThreshold * ((NinjaScriptBase)parent).TickSize;
				if (parent.MinimumDurationMinutes == 0 || (time - CurrentLeg.StartTime).TotalMinutes >= (double)parent.MinimumDurationMinutes)
				{
					if (legDir == AutoLegDirection.Up && currentExtremePrice - last >= num)
					{
						HandleReversalTick(AutoLegDirection.Down, last, time, primaryBarIndex);
						return;
					}
					if (legDir == AutoLegDirection.Down && last - currentExtremePrice >= num)
					{
						HandleReversalTick(AutoLegDirection.Up, last, time, primaryBarIndex);
						return;
					}
				}
			}
			ProcessTickToLeg(CurrentLeg, last, vol, signedVol, time);
			CurrentLeg.HighPrice = Math.Max(CurrentLeg.HighPrice, last);
			CurrentLeg.LowPrice = Math.Min(CurrentLeg.LowPrice, last);
			CurrentLeg.EndTime = time;
		}

		private void HandleReversalTick(AutoLegDirection newDir, double currentTickPrice, DateTime time, int primaryBarIndex)
		{
			if (Math.Abs(CurrentLeg.HighPrice - CurrentLeg.LowPrice) / ((NinjaScriptBase)parent).TickSize >= (double)parent.MinimumLegTicks)
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
				StartIndex = ((currentExtremeBar > -1) ? currentExtremeBar : primaryBarIndex),
				StartTime = currentExtremeTime,
				EndIndex = primaryBarIndex,
				EndTime = time,
				HighPrice = currentExtremePrice,
				LowPrice = currentExtremePrice,
				Direction = newDir
			};
			foreach (TickRecord item in ticksSinceExtreme)
			{
				ProcessTickToLeg(CurrentLeg, item.Price, item.Volume, item.SignedVolume, item.Time);
				CurrentLeg.HighPrice = Math.Max(CurrentLeg.HighPrice, item.Price);
				CurrentLeg.LowPrice = Math.Min(CurrentLeg.LowPrice, item.Price);
				CurrentLeg.EndTime = item.Time;
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
			if (targetLeg == null || vol <= 0)
			{
				return;
			}
			double num = (double)parent.VolumeTickCompression * ((NinjaScriptBase)parent).TickSize;
			double key = Math.Floor(price / num + 1E-06) * num;
			if (targetLeg.VolByPrice.TryGetValue(key, out var value))
			{
				targetLeg.VolByPrice[key] = value + vol;
			}
			else
			{
				targetLeg.VolByPrice[key] = vol;
			}
			if (signedVol != 0L)
			{
				double num2 = (double)parent.VolumeTickCompression * ((NinjaScriptBase)parent).TickSize;
				double key2 = Math.Floor(price / num2 + 1E-06) * num2;
				if (targetLeg.DeltaByPrice.TryGetValue(key2, out var value2))
				{
					targetLeg.DeltaByPrice[key2] = value2 + signedVol;
				}
				else
				{
					targetLeg.DeltaByPrice[key2] = signedVol;
				}
			}
		}
	}

	private LegTracker currentTracker;

	private LegTracker pastTracker;

	private double lastBid = double.NaN;

	private double lastAsk = double.NaN;

	private double prevLast = double.NaN;

	private TextFormat textFormat;

	private SolidColorBrush posBrushDx;

	private SolidColorBrush negBrushDx;

	private SolidColorBrush textBrushDx;

	private SolidColorBrush volBrushDx;

	private SolidColorBrush labelBgBrushDx;

	private SolidColorBrush legBoxBrushDx;

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Current Reversal Ticks", GroupName = "Leg Detection", Order = 0)]
	public int ReversalTicks { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Past Reversal Ticks", GroupName = "Leg Detection", Order = 1)]
	public int PastReversalTicks { get; set; }

	[NinjaScriptProperty]
	[Range(0, int.MaxValue)]
	[Display(Name = "Min Leg Ticks", GroupName = "Leg Detection", Order = 2)]
	public int MinimumLegTicks { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Min Bars Per Leg", GroupName = "Leg Detection", Order = 2)]
	public int MinimumBarsPerLeg { get; set; }

	[NinjaScriptProperty]
	[Range(0, 1440)]
	[Display(Name = "Min Duration (Min)", GroupName = "Leg Detection", Order = 3)]
	public int MinimumDurationMinutes { get; set; }

	[NinjaScriptProperty]
	[Range(0, 50)]
	[Display(Name = "Legs To Display", GroupName = "Layout", Order = 4)]
	public int LegsToDisplay { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Vol Compression (Ticks)", GroupName = "Layout", Order = 5)]
	public int VolumeTickCompression { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Delta Compression (Ticks)", GroupName = "Layout", Order = 6)]
	public int DeltaTickCompression { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Vol Width", GroupName = "Layout", Order = 7)]
	public int VolumeProfileWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Delta Width", GroupName = "Layout", Order = 8)]
	public int DeltaProfileWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Past Vol Width", GroupName = "Layout", Order = 9)]
	public int PastVolumeWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Past Delta Width", GroupName = "Layout", Order = 10)]
	public int PastDeltaWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(-500, 500)]
	[Display(Name = "Right Offset (px)", GroupName = "Layout", Order = 11)]
	public int RightOffsetPx { get; set; }

	[NinjaScriptProperty]
	[Range(0, 500)]
	[Display(Name = "Separation", GroupName = "Layout", Order = 12)]
	public int ProfileSeparationPx { get; set; }

	[NinjaScriptProperty]
	[Range(0, 10)]
	[Display(Name = "Profile Bar Spacing", GroupName = "Layout", Order = 13)]
	public int ProfileBarSpacingPx { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Volume", GroupName = "Visibility", Order = 14)]
	public bool ShowVolume { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Delta", GroupName = "Visibility", Order = 15)]
	public bool ShowDelta { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Past Delta", GroupName = "Visibility", Order = 16)]
	public bool ShowPastDelta { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Current Leg Box", GroupName = "Visibility", Order = 17)]
	public bool ShowCurrentLegBox { get; set; }

	[NinjaScriptProperty]
	[Range(5, 50)]
	[Display(Name = "Delta Label Font Size", GroupName = "Visibility", Order = 18)]
	public int DeltaLabelFontSize { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Delta Lbl BG", GroupName = "Visibility", Order = 19)]
	public bool ShowDeltaLabelBackground { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, 1.0)]
	[Display(Name = "Volume Opacity", GroupName = "Colors", Order = 22)]
	public float VolumeOpacity { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, 1.0)]
	[Display(Name = "Delta Opacity", GroupName = "Colors", Order = 23)]
	public float DeltaOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Pos Delta Color", GroupName = "Colors", Order = 24)]
	public Brush PositiveBrush { get; set; }

	[Browsable(false)]
	public string PositiveBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(PositiveBrush);
		}
		set
		{
			PositiveBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Neg Delta Color", GroupName = "Colors", Order = 25)]
	public Brush NegativeBrush { get; set; }

	[Browsable(false)]
	public string NegativeBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(NegativeBrush);
		}
		set
		{
			NegativeBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Vol Color", GroupName = "Colors", Order = 26)]
	public Brush VolumeBrush { get; set; }

	[Browsable(false)]
	public string VolumeBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(VolumeBrush);
		}
		set
		{
			VolumeBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Text Color", GroupName = "Colors", Order = 27)]
	public Brush TextBrush { get; set; }

	[Browsable(false)]
	public string TextBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(TextBrush);
		}
		set
		{
			TextBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Label BG Color", GroupName = "Colors", Order = 28)]
	public Brush LabelBgBrush { get; set; }

	[Browsable(false)]
	public string LabelBgBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(LabelBgBrush);
		}
		set
		{
			LabelBgBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Leg Box Color", GroupName = "Colors", Order = 29)]
	public Brush LegBoxBrush { get; set; }

	[Browsable(false)]
	public string LegBoxBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(LegBoxBrush);
		}
		set
		{
			LegBoxBrush = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Invalid comparison between Unknown and I4
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = "AutoLegProfileNT 2.0";
			((NinjaScript)this).Description = "Rotation-based leg delta/volume profile strictly rendering on the right scale edge with past leg support.";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = true;
			ReversalTicks = 20;
			PastReversalTicks = 40;
			MinimumBarsPerLeg = 1;
			MinimumDurationMinutes = 0;
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
			ShowVolume = true;
			ShowDelta = true;
			ShowPastDelta = true;
			ShowCurrentLegBox = false;
			DeltaLabelFontSize = 10;
			ShowDeltaLabelBackground = true;
			VolumeOpacity = 0.6f;
			DeltaOpacity = 0.85f;
			PositiveBrush = Brushes.Lime;
			NegativeBrush = Brushes.Red;
			VolumeBrush = Brushes.RoyalBlue;
			TextBrush = Brushes.White;
			LabelBgBrush = Brushes.Black;
			LegBoxBrush = Brushes.Yellow;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
			currentTracker = new LegTracker(this, ReversalTicks);
			pastTracker = new LegTracker(this, (PastReversalTicks > 0) ? PastReversalTicks : ReversalTicks);
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			DisposeDx();
		}
	}

	private void DisposeDx()
	{
		try
		{
			TextFormat obj = textFormat;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			SolidColorBrush obj2 = posBrushDx;
			if (obj2 != null)
			{
				((DisposeBase)obj2).Dispose();
			}
			SolidColorBrush obj3 = negBrushDx;
			if (obj3 != null)
			{
				((DisposeBase)obj3).Dispose();
			}
			SolidColorBrush obj4 = textBrushDx;
			if (obj4 != null)
			{
				((DisposeBase)obj4).Dispose();
			}
			SolidColorBrush obj5 = volBrushDx;
			if (obj5 != null)
			{
				((DisposeBase)obj5).Dispose();
			}
			SolidColorBrush obj6 = labelBgBrushDx;
			if (obj6 != null)
			{
				((DisposeBase)obj6).Dispose();
			}
			SolidColorBrush obj7 = legBoxBrushDx;
			if (obj7 != null)
			{
				((DisposeBase)obj7).Dispose();
			}
		}
		catch
		{
		}
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
		((IndicatorRenderBase)this).OnRenderTargetChanged();
	}

	protected override void OnMarketData(MarketDataEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if ((int)e.MarketDataType == 1)
		{
			lastBid = e.Price;
		}
		else if ((int)e.MarketDataType == 0)
		{
			lastAsk = e.Price;
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).BarsInProgress == 1)
		{
			if (((NinjaScriptBase)this).CurrentBar < 1)
			{
				return;
			}
			double num = ((NinjaScriptBase)this).Close[0];
			long num2 = (long)((NinjaScriptBase)this).Volume[0];
			DateTime time = ((NinjaScriptBase)this).Time[0];
			int num3 = ((NinjaScriptBase)this).BarsArray[0].Count - 1;
			if (num3 < 0)
			{
				num3 = 0;
			}
			long signedVol = 0L;
			if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0.0 && lastBid > 0.0 && lastAsk >= lastBid)
			{
				if (num >= lastAsk)
				{
					signedVol = num2;
				}
				else if (num <= lastBid)
				{
					signedVol = -num2;
				}
				else if (!double.IsNaN(prevLast))
				{
					signedVol = ((num > prevLast) ? num2 : ((num < prevLast) ? (-num2) : 0));
				}
			}
			else if (!double.IsNaN(prevLast))
			{
				signedVol = ((num > prevLast) ? num2 : ((num < prevLast) ? (-num2) : 0));
			}
			prevLast = num;
			if (num2 > 0)
			{
				currentTracker.ProcessBarUpdate(num, num2, signedVol, time, num3);
				pastTracker.ProcessBarUpdate(num, num2, signedVol, time, num3);
				if ((int)((NinjaScript)this).State == 7)
				{
					((IndicatorRenderBase)this).ForceRefresh();
				}
			}
		}
		else if (((NinjaScriptBase)this).BarsInProgress == 0)
		{
			if (currentTracker != null && currentTracker.CurrentLeg != null && ((NinjaScriptBase)this).CurrentBar > 0)
			{
				currentTracker.CurrentLeg.EndIndex = ((NinjaScriptBase)this).CurrentBar;
			}
			if (pastTracker != null && pastTracker.CurrentLeg != null && ((NinjaScriptBase)this).CurrentBar > 0)
			{
				pastTracker.CurrentLeg.EndIndex = ((NinjaScriptBase)this).CurrentBar;
			}
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		if (currentTracker == null || currentTracker.CurrentLeg == null)
		{
			return;
		}
		EnsureDxResources();
		ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
		float originX = chartControl.CanvasRight - RightOffsetPx - VolumeProfileWidthPx;
		DrawLegProfiles(chartControl, chartScale, panel, currentTracker.CurrentLeg, originX, VolumeProfileWidthPx, DeltaProfileWidthPx, isCurrent: true, forceHideDelta: false);
		if (LegsToDisplay > 0 && pastTracker != null && pastTracker.CompletedLegs.Count > 0)
		{
			for (int num = pastTracker.CompletedLegs.Count - 1; num >= 0; num--)
			{
				PriceLeg priceLeg = pastTracker.CompletedLegs[num];
				float originX2 = chartControl.GetXByTime(priceLeg.StartTime);
				bool forceHideDelta = !ShowPastDelta;
				DrawLegProfiles(chartControl, chartScale, panel, priceLeg, originX2, PastVolumeWidthPx, PastDeltaWidthPx, isCurrent: false, forceHideDelta);
			}
		}
	}

	private void DrawLegProfiles(ChartControl chartControl, ChartScale chartScale, ChartPanel panel, PriceLeg leg, float originX, int vWidth, int dWidth, bool isCurrent, bool forceHideDelta)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_042f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0402: Unknown result type (might be due to invalid IL or missing references)
		if (ShowCurrentLegBox && isCurrent)
		{
			int yByValue = chartScale.GetYByValue(leg.HighPrice);
			int yByValue2 = chartScale.GetYByValue(leg.LowPrice);
			int num = vWidth + dWidth;
			float num2 = originX - (float)dWidth;
			((IndicatorRenderBase)this).RenderTarget.DrawRectangle(new RectangleF(num2 - 5f, (float)(yByValue - 5), (float)(num + 10), (float)(yByValue2 - yByValue + 10)), (Brush)(object)legBoxBrushDx, 1f);
		}
		if (ShowVolume)
		{
			long num3 = leg.VolByPrice.Values.DefaultIfEmpty(0L).Max();
			if (num3 > 0)
			{
				foreach (KeyValuePair<double, long> item in leg.VolByPrice)
				{
					int yByValue3 = chartScale.GetYByValue(item.Key);
					if (yByValue3 >= panel.Y - 50 && yByValue3 <= panel.Y + panel.H + 50)
					{
						int yByValue4 = chartScale.GetYByValue(item.Key - (double)VolumeTickCompression * ((NinjaScriptBase)this).TickSize);
						int num4 = Math.Max(1, Math.Abs(yByValue4 - yByValue3) - ProfileBarSpacingPx);
						float num5 = (float)((double)vWidth * ((double)item.Value / (double)num3));
						if (num5 > 0.5f)
						{
							((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(originX, (float)Math.Min(yByValue3, yByValue4) + (float)ProfileBarSpacingPx / 2f, num5, (float)num4), (Brush)(object)volBrushDx);
						}
					}
				}
			}
		}
		if (!ShowDelta || forceHideDelta || leg.DeltaByPrice.Count <= 0)
		{
			return;
		}
		double num6 = (double)DeltaTickCompression * ((NinjaScriptBase)this).TickSize;
		Dictionary<double, long> dictionary = new Dictionary<double, long>();
		foreach (KeyValuePair<double, long> item2 in leg.DeltaByPrice)
		{
			double key = Math.Floor(item2.Key / num6 + 1E-06) * num6;
			if (dictionary.TryGetValue(key, out var value))
			{
				dictionary[key] = value + item2.Value;
			}
			else
			{
				dictionary[key] = item2.Value;
			}
		}
		long num7 = dictionary.Values.Select((long v) => Math.Abs(v)).DefaultIfEmpty(0L).Max();
		if (num7 <= 0)
		{
			return;
		}
		RectangleF val = default(RectangleF);
		foreach (KeyValuePair<double, long> item3 in dictionary)
		{
			int yByValue5 = chartScale.GetYByValue(item3.Key + num6);
			if (yByValue5 < panel.Y - 50 || yByValue5 > panel.Y + panel.H + 50)
			{
				continue;
			}
			int yByValue6 = chartScale.GetYByValue(item3.Key);
			int num8 = Math.Max(1, Math.Abs(yByValue6 - yByValue5) - ProfileBarSpacingPx);
			float num9 = (float)Math.Min(yByValue5, yByValue6) + (float)ProfileBarSpacingPx / 2f;
			float num10 = (float)((double)dWidth * ((double)Math.Abs(item3.Value) / (double)num7));
			if (!(num10 > 0.5f))
			{
				continue;
			}
			((RectangleF)(ref val))._002Ector(originX - num10, num9, num10, (float)num8);
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(val, (Brush)(object)((item3.Value >= 0) ? posBrushDx : negBrushDx));
			if (num8 >= DeltaLabelFontSize + 2)
			{
				string text = item3.Value.ToString("+#;-#;0");
				float num11 = MeasureTextWidth(text);
				float num12 = originX - num11 - 2f;
				float num13 = num9 + (float)num8 / 2f - (float)DeltaLabelFontSize / 2f;
				if (ShowDeltaLabelBackground)
				{
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(num12 - 1f, num13 - 1f, num11 + 2f, (float)(DeltaLabelFontSize + 2)), (Brush)(object)labelBgBrushDx);
				}
				((IndicatorRenderBase)this).RenderTarget.DrawText(text, textFormat, new RectangleF(num12, num13, num11, (float)(DeltaLabelFontSize + 2)), (Brush)(object)textBrushDx);
			}
		}
	}

	private void EnsureDxResources()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Expected O, but got Unknown
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected O, but got Unknown
		if (posBrushDx == null)
		{
			posBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(PositiveBrush, DeltaOpacity));
		}
		if (negBrushDx == null)
		{
			negBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(NegativeBrush, DeltaOpacity));
		}
		if (textBrushDx == null)
		{
			textBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(TextBrush, 1f));
		}
		if (volBrushDx == null)
		{
			volBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(VolumeBrush, VolumeOpacity));
		}
		if (labelBgBrushDx == null)
		{
			labelBgBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(LabelBgBrush, 1f));
		}
		if (legBoxBrushDx == null)
		{
			legBoxBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(LegBoxBrush, 1f));
		}
		if (textFormat == null)
		{
			textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", (float)DeltaLabelFontSize)
			{
				TextAlignment = (TextAlignment)2,
				ParagraphAlignment = (ParagraphAlignment)2
			};
		}
	}

	private float MeasureTextWidth(string text)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (textFormat == null)
		{
			return 0f;
		}
		TextLayout val = new TextLayout(Globals.DirectWriteFactory, text, textFormat, 1000f, 100f);
		try
		{
			return val.Metrics.Width;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static Color BrushToMediaColor(Brush b)
	{
		return (b as SolidColorBrush)?.Color ?? Colors.White;
	}

	private Color4 ToDx(Brush b, float alphaMult)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		Color color = BrushToMediaColor(b);
		return new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, (float)(int)color.A / 255f * alphaMult);
	}
}

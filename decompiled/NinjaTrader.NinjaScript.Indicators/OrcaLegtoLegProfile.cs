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

		public bool IsVACalculated;

		public double POCPrice = double.NaN;

		public double VAHPrice = double.NaN;

		public double VALPrice = double.NaN;

		public long MaxVol;

		public int LastVolComp = -1;
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

		private OrcaLegtoLegProfile parent;

		public int TickReversalThreshold;

		public List<PriceLeg> CompletedLegs = new List<PriceLeg>();

		public PriceLeg CurrentLeg;

		private double currentExtremePrice = double.NaN;

		private int currentExtremeBar = -1;

		private DateTime currentExtremeTime;

		private OrcaLegDirection legDir;

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
			bool flag = false;
			if ((legDir == OrcaLegDirection.Up || legDir == OrcaLegDirection.Unknown) && (double.IsNaN(currentExtremePrice) || last >= currentExtremePrice))
			{
				currentExtremePrice = last;
				currentExtremeTime = time;
				flag = true;
			}
			if ((legDir == OrcaLegDirection.Down || legDir == OrcaLegDirection.Unknown) && (double.IsNaN(currentExtremePrice) || last <= currentExtremePrice))
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
				double val = (double)TickReversalThreshold * ((NinjaScriptBase)parent).TickSize;
				if (parent.UseAtrReversal && parent.atrIndicator != null && ((NinjaScriptBase)parent).CurrentBars[0] >= parent.AtrPeriod)
				{
					try
					{
						double num = ((NinjaScriptBase)parent.atrIndicator)[0];
						if (num > 0.0)
						{
							val = num * AtrMultiplier;
						}
					}
					catch
					{
					}
				}
				val = Math.Max(val, ((NinjaScriptBase)parent).TickSize);
				if (parent.MinimumDurationMinutes == 0 || (time - CurrentLeg.StartTime).TotalMinutes >= (double)parent.MinimumDurationMinutes)
				{
					if (legDir == OrcaLegDirection.Up && currentExtremePrice - last >= val)
					{
						HandleReversalTick(OrcaLegDirection.Down, last, time, primaryBarIndex);
						return;
					}
					if (legDir == OrcaLegDirection.Down && last - currentExtremePrice >= val)
					{
						HandleReversalTick(OrcaLegDirection.Up, last, time, primaryBarIndex);
						return;
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

		private void StartNewLegAtCurrentTick(OrcaLegDirection dir, double last, DateTime time, int primaryBarIndex)
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
			lock (targetLeg.SyncObj)
			{
				targetLeg.VolByPrice[key] = (targetLeg.VolByPrice.TryGetValue(key, out var value) ? (value + vol) : vol);
			}
			if (signedVol == 0L)
			{
				return;
			}
			double num2 = (double)(parent.UseDynamicAggregation ? 1 : parent.DeltaTickCompression) * ((NinjaScriptBase)parent).TickSize;
			double key2 = Math.Floor(price / num2 + 1E-06) * num2;
			lock (targetLeg.SyncObj)
			{
				targetLeg.DeltaByPrice[key2] = (targetLeg.DeltaByPrice.TryGetValue(key2, out var value2) ? (value2 + signedVol) : signedVol);
			}
		}
	}

	private LegTracker currentTracker;

	private LegTracker pastTracker;

	private ATR atrIndicator;

	private int lastDynamicDeltaComp = -1;

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

	private SolidColorBrush pocBrushDx;

	private SolidColorBrush vaVolBrushDx;

	private SolidColorBrush vaLineBrushDx;

	private StrokeStyle vaLineStrokeDx;

	private SolidColorBrush[] volGradientBrushes;

	private SolidColorBrush[] vaGradientBrushes;

	private int lastBuiltGradientSteps = -1;

	private int lastBuiltVAGradientSteps = -1;

	private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Current Reversal Ticks", GroupName = "Leg Detection", Order = 0)]
	public int ReversalTicks { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Past Reversal Ticks", GroupName = "Leg Detection", Order = 1)]
	public int PastReversalTicks { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Use ATR Reversal", GroupName = "Leg Detection", Order = 2)]
	public bool UseAtrReversal { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "ATR Period", GroupName = "Leg Detection", Order = 3)]
	public int AtrPeriod { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, double.MaxValue)]
	[Display(Name = "Current ATR Multiplier", GroupName = "Leg Detection", Order = 4)]
	public double AtrMultiplier { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, double.MaxValue)]
	[Display(Name = "Past ATR Multiplier", GroupName = "Leg Detection", Order = 5)]
	public double PastAtrMultiplier { get; set; }

	[NinjaScriptProperty]
	[Range(0, int.MaxValue)]
	[Display(Name = "Min Leg Ticks", GroupName = "Leg Detection", Order = 6)]
	public int MinimumLegTicks { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Min Bars Per Leg", GroupName = "Leg Detection", Order = 7)]
	public int MinimumBarsPerLeg { get; set; }

	[NinjaScriptProperty]
	[Range(0, 1440)]
	[Display(Name = "Min Duration (Min)", GroupName = "Leg Detection", Order = 8)]
	public int MinimumDurationMinutes { get; set; }

	[NinjaScriptProperty]
	[Range(0, 50)]
	[Display(Name = "Legs To Display", GroupName = "Layout", Order = 4)]
	public int LegsToDisplay { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Use Dynamic Aggregation", Description = "Automatically adjust profile compression upon zoom", GroupName = "Layout", Order = 5)]
	public bool UseDynamicAggregation { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, 10.0)]
	[Display(Name = "Dynamic Aggregation Multiplier", Description = "Lower value = more granular blocks (fewer aggregated ticks)", GroupName = "Layout", Order = 6)]
	public double DynamicAggregationMultiplier { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Vol Compression (Ticks)", GroupName = "Layout", Order = 6)]
	public int VolumeTickCompression { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Delta Compression (Ticks)", GroupName = "Layout", Order = 7)]
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
	[Display(Name = "Show POC", GroupName = "Volume Profile", Order = 20)]
	public bool ShowPOC { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Use Gradient", GroupName = "Volume Profile", Order = 21)]
	public bool UseGradient { get; set; }

	[NinjaScriptProperty]
	[Range(2, 64)]
	[Display(Name = "Gradient Steps", GroupName = "Volume Profile", Order = 22)]
	public int GradientSteps { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Value Area", GroupName = "Value Area", Order = 23)]
	public bool ShowValueArea { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VA Color Mode", GroupName = "Value Area", Order = 24)]
	public bool ShowVAColor { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VA Boundary Lines", GroupName = "Value Area", Order = 25)]
	public bool ShowVALines { get; set; }

	[NinjaScriptProperty]
	[Range(50, 95)]
	[Display(Name = "VA Percent", GroupName = "Value Area", Order = 26)]
	public int ValueAreaPercent { get; set; }

	[NinjaScriptProperty]
	[Range(0.5, 6.0)]
	[Display(Name = "VA Line Thickness", GroupName = "Value Area", Order = 27)]
	public float VALineThickness { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "VA Line Style", GroupName = "Value Area", Order = 28)]
	public VALineStyleEnum VALineStyle { get; set; }

	[NinjaScriptProperty]
	[Range(0.05, 1.0)]
	[Display(Name = "Min Brightness", GroupName = "Colors", Order = 30)]
	public float MinBrightness { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, 1.0)]
	[Display(Name = "Volume Opacity", GroupName = "Colors", Order = 31)]
	public float VolumeOpacity { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, 1.0)]
	[Display(Name = "Delta Opacity", GroupName = "Colors", Order = 32)]
	public float DeltaOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "Pos Delta Color", GroupName = "Colors", Order = 33)]
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
	[Display(Name = "Neg Delta Color", GroupName = "Colors", Order = 34)]
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
	[Display(Name = "Vol Color", GroupName = "Colors", Order = 35)]
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
	[Display(Name = "Text Color", GroupName = "Colors", Order = 36)]
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
	[Display(Name = "Label BG Color", GroupName = "Colors", Order = 37)]
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
	[Display(Name = "Leg Box Color", GroupName = "Colors", Order = 38)]
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

	[XmlIgnore]
	[Display(Name = "POC Color", GroupName = "Colors", Order = 39)]
	public Brush POCBrush { get; set; }

	[Browsable(false)]
	public string POCBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(POCBrush);
		}
		set
		{
			POCBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "VA Color", GroupName = "Colors", Order = 40)]
	public Brush VABrush { get; set; }

	[Browsable(false)]
	public string VABrushSerialize
	{
		get
		{
			return Serialize.BrushToString(VABrush);
		}
		set
		{
			VABrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "VA Line Color", GroupName = "Colors", Order = 41)]
	public Brush VALineBrush { get; set; }

	[Browsable(false)]
	public string VALineBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(VALineBrush);
		}
		set
		{
			VALineBrush = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Invalid comparison between Unknown and I4
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Invalid comparison between Unknown and I4
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = "OrcaLegtoLegProfile";
			((NinjaScript)this).Description = "Rotation-based leg delta/volume profile with Value Area, POC and gradient support.";
			((NinjaScriptBase)this).Calculate = (Calculate)2;
			((NinjaScriptBase)this).IsOverlay = true;
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
			ShowPOC = true;
			POCBrush = Brushes.DodgerBlue;
			UseGradient = true;
			GradientSteps = 16;
			MinBrightness = 0.2f;
			ShowValueArea = true;
			ShowVAColor = true;
			ShowVALines = true;
			ValueAreaPercent = 70;
			VALineThickness = 1.5f;
			VALineStyle = VALineStyleEnum.Dash;
			VABrush = Brushes.CornflowerBlue;
			VALineBrush = Brushes.White;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
			currentTracker = new LegTracker(this, ReversalTicks, AtrMultiplier);
			pastTracker = new LegTracker(this, (PastReversalTicks > 0) ? PastReversalTicks : ReversalTicks, PastAtrMultiplier);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			if (UseAtrReversal)
			{
				atrIndicator = ATR(AtrPeriod);
			}
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
			SolidColorBrush obj8 = pocBrushDx;
			if (obj8 != null)
			{
				((DisposeBase)obj8).Dispose();
			}
			SolidColorBrush obj9 = vaVolBrushDx;
			if (obj9 != null)
			{
				((DisposeBase)obj9).Dispose();
			}
			SolidColorBrush obj10 = vaLineBrushDx;
			if (obj10 != null)
			{
				((DisposeBase)obj10).Dispose();
			}
			StrokeStyle obj11 = vaLineStrokeDx;
			if (obj11 != null)
			{
				((DisposeBase)obj11).Dispose();
			}
			SolidColorBrush[] array;
			if (volGradientBrushes != null)
			{
				array = volGradientBrushes;
				foreach (SolidColorBrush obj12 in array)
				{
					if (obj12 != null)
					{
						((DisposeBase)obj12).Dispose();
					}
				}
			}
			if (vaGradientBrushes == null)
			{
				return;
			}
			array = vaGradientBrushes;
			foreach (SolidColorBrush obj13 in array)
			{
				if (obj13 != null)
				{
					((DisposeBase)obj13).Dispose();
				}
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
			pocBrushDx = null;
			vaVolBrushDx = null;
			vaLineBrushDx = null;
			vaLineStrokeDx = null;
			volGradientBrushes = null;
			vaGradientBrushes = null;
			lastBuiltGradientSteps = -1;
			lastBuiltVAGradientSteps = -1;
			textWidthCache.Clear();
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
		if (((NinjaScriptBase)this).BarsInProgress == 1)
		{
			if (((NinjaScriptBase)this).CurrentBar < 1)
			{
				return;
			}
			double num = ((NinjaScriptBase)this).Close[0];
			long num2 = (long)((NinjaScriptBase)this).Volume[0];
			DateTime time = ((NinjaScriptBase)this).Time[0];
			int primaryBarIndex = Math.Max(0, ((NinjaScriptBase)this).BarsArray[0].Count - 1);
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
				currentTracker.ProcessBarUpdate(num, num2, signedVol, time, primaryBarIndex);
				pastTracker.ProcessBarUpdate(num, num2, signedVol, time, primaryBarIndex);
			}
		}
		else if (((NinjaScriptBase)this).BarsInProgress == 0)
		{
			if (currentTracker?.CurrentLeg != null && ((NinjaScriptBase)this).CurrentBar > 0)
			{
				currentTracker.CurrentLeg.EndIndex = ((NinjaScriptBase)this).CurrentBar;
			}
			if (pastTracker?.CurrentLeg != null && ((NinjaScriptBase)this).CurrentBar > 0)
			{
				pastTracker.CurrentLeg.EndIndex = ((NinjaScriptBase)this).CurrentBar;
			}
		}
	}

	private bool CalcValueArea(Dictionary<double, long> volMap, double pocPrice, out double vahPrice, out double valPrice)
	{
		vahPrice = pocPrice;
		valPrice = pocPrice;
		if (volMap.Count <= 1)
		{
			return false;
		}
		List<double> list = new List<double>(volMap.Keys);
		list.Sort();
		long num = volMap.Values.Sum();
		if (num <= 0)
		{
			return false;
		}
		double num2 = (double)num * ((double)ValueAreaPercent / 100.0);
		int num3 = list.IndexOf(pocPrice);
		if (num3 < 0)
		{
			return false;
		}
		long num4 = volMap[pocPrice];
		int num5 = num3;
		int num6 = num3;
		while ((double)num4 < num2 && (num5 > 0 || num6 < list.Count - 1))
		{
			long num7 = ((num5 > 0) ? volMap[list[num5 - 1]] : 0);
			long num8 = ((num6 < list.Count - 1) ? volMap[list[num6 + 1]] : 0);
			if (num5 <= 0)
			{
				num6++;
				num4 += num8;
			}
			else if (num6 >= list.Count - 1)
			{
				num5--;
				num4 += num7;
			}
			else if (num8 >= num7)
			{
				num6++;
				num4 += num8;
			}
			else
			{
				num5--;
				num4 += num7;
			}
		}
		valPrice = list[num5];
		vahPrice = list[num6];
		return true;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		if (currentTracker?.CurrentLeg == null)
		{
			return;
		}
		EnsureDxResources();
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		int volumeTickCompression = VolumeTickCompression;
		int num = DeltaTickCompression;
		if (UseDynamicAggregation)
		{
			double num2 = (chartScale.MaxValue - chartScale.MinValue) / ((NinjaScriptBase)this).TickSize / (double)Math.Max(1, val.H) * (double)(DeltaLabelFontSize + 4) * DynamicAggregationMultiplier;
			num = ((num2 <= 1.0) ? 1 : ((num2 <= 2.0) ? 2 : ((num2 <= 4.0) ? 4 : ((num2 <= 5.0) ? 5 : ((num2 <= 8.0) ? 8 : ((num2 <= 10.0) ? 10 : ((num2 <= 15.0) ? 15 : ((num2 <= 20.0) ? 20 : ((num2 <= 25.0) ? 25 : ((num2 <= 30.0) ? 30 : ((num2 <= 40.0) ? 40 : ((num2 <= 50.0) ? 50 : ((!(num2 <= 100.0)) ? ((int)(Math.Round(num2 / 50.0) * 50.0)) : ((int)(Math.Round(num2 / 20.0) * 20.0)))))))))))))));
			if (lastDynamicDeltaComp > 0 && (double)Math.Abs(num - lastDynamicDeltaComp) < Math.Max(2.0, (double)num * 0.15))
			{
				num = lastDynamicDeltaComp;
			}
			else
			{
				lastDynamicDeltaComp = num;
			}
		}
		float originX = chartControl.CanvasRight - RightOffsetPx - VolumeProfileWidthPx;
		DrawLegProfiles(chartControl, chartScale, val, currentTracker.CurrentLeg, originX, VolumeProfileWidthPx, DeltaProfileWidthPx, isCurrent: true, forceHideDelta: false, volumeTickCompression, num);
		if (LegsToDisplay <= 0)
		{
			return;
		}
		LegTracker legTracker = pastTracker;
		if (legTracker != null && legTracker.CompletedLegs.Count > 0)
		{
			for (int num3 = pastTracker.CompletedLegs.Count - 1; num3 >= 0; num3--)
			{
				PriceLeg priceLeg = pastTracker.CompletedLegs[num3];
				float originX2 = chartControl.GetXByTime(priceLeg.StartTime);
				DrawLegProfiles(chartControl, chartScale, val, priceLeg, originX2, PastVolumeWidthPx, PastDeltaWidthPx, isCurrent: false, !ShowPastDelta, volumeTickCompression, num);
			}
		}
	}

	private void DrawLegProfiles(ChartControl chartControl, ChartScale chartScale, ChartPanel panel, PriceLeg leg, float originX, int vWidth, int dWidth, bool isCurrent, bool forceHideDelta, int volCompTicks, int deltaCompTicks)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0450: Unknown result type (might be due to invalid IL or missing references)
		//IL_0462: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0778: Unknown result type (might be due to invalid IL or missing references)
		if (ShowCurrentLegBox && isCurrent)
		{
			int yByValue = chartScale.GetYByValue(leg.HighPrice);
			int yByValue2 = chartScale.GetYByValue(leg.LowPrice);
			((IndicatorRenderBase)this).RenderTarget.DrawRectangle(new RectangleF(originX - (float)dWidth - 5f, (float)(yByValue - 5), (float)(vWidth + dWidth + 10), (float)(yByValue2 - yByValue + 10)), (Brush)(object)legBoxBrushDx, 1f);
		}
		if (ShowVolume && leg.VolByPrice.Count > 0)
		{
			Dictionary<double, long> dictionary;
			lock (leg.SyncObj)
			{
				dictionary = (isCurrent ? new Dictionary<double, long>(leg.VolByPrice) : leg.VolByPrice);
			}
			long num = 0L;
			double num2 = double.NaN;
			bool flag = false;
			double vahPrice = double.NaN;
			double valPrice = double.NaN;
			if (!leg.IsVACalculated || isCurrent)
			{
				foreach (KeyValuePair<double, long> item in dictionary)
				{
					if (item.Value > num)
					{
						num = item.Value;
						num2 = item.Key;
					}
				}
				if (num > 0 && ShowValueArea && (ShowVAColor || ShowVALines))
				{
					flag = CalcValueArea(dictionary, num2, out vahPrice, out valPrice);
				}
				if (!isCurrent)
				{
					leg.MaxVol = num;
					leg.POCPrice = num2;
					leg.VAHPrice = vahPrice;
					leg.VALPrice = valPrice;
					leg.IsVACalculated = true;
				}
			}
			else
			{
				num = leg.MaxVol;
				num2 = leg.POCPrice;
				vahPrice = leg.VAHPrice;
				valPrice = leg.VALPrice;
				flag = !double.IsNaN(vahPrice);
			}
			if (num > 0)
			{
				foreach (KeyValuePair<double, long> item2 in dictionary)
				{
					int yByValue3 = chartScale.GetYByValue(item2.Key);
					if (yByValue3 < panel.Y - 50 || yByValue3 > panel.Y + panel.H + 50)
					{
						continue;
					}
					int yByValue4 = chartScale.GetYByValue(item2.Key - (double)volCompTicks * ((NinjaScriptBase)this).TickSize);
					int num3 = Math.Max(1, Math.Abs(yByValue4 - yByValue3) - ProfileBarSpacingPx);
					float num4 = (float)((double)vWidth * ((double)item2.Value / (double)num));
					if (!(num4 > 0.5f))
					{
						continue;
					}
					bool flag2 = flag && item2.Key >= valPrice - ((NinjaScriptBase)this).TickSize * 0.01 && item2.Key <= vahPrice + ((NinjaScriptBase)this).TickSize * 0.01;
					SolidColorBrush val = volBrushDx;
					if (ShowPOC && Math.Abs(item2.Key - num2) < ((NinjaScriptBase)this).TickSize * 0.01)
					{
						val = pocBrushDx;
					}
					else if (UseGradient)
					{
						SolidColorBrush[] array = ((ShowValueArea && ShowVAColor && flag2 && vaGradientBrushes != null) ? vaGradientBrushes : volGradientBrushes);
						if (array != null)
						{
							int num5 = Math.Min(array.Length - 1, Math.Max(0, (int)((double)item2.Value / (double)num * (double)(array.Length - 1))));
							val = array[num5];
						}
					}
					else
					{
						val = ((ShowValueArea && ShowVAColor && flag2) ? vaVolBrushDx : volBrushDx);
					}
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(originX, (float)Math.Min(yByValue3, yByValue4) + (float)ProfileBarSpacingPx / 2f, num4, (float)num3), (Brush)(object)val);
				}
				if (flag && ShowValueArea && ShowVALines && vaLineBrushDx != null)
				{
					float num6 = chartScale.GetYByValue(vahPrice);
					float num7 = chartScale.GetYByValue(valPrice - (double)volCompTicks * ((NinjaScriptBase)this).TickSize);
					if (num6 >= (float)(panel.Y - 5) && num6 <= (float)(panel.Y + panel.H + 5))
					{
						((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(originX - 2f, num6), new Vector2(originX + (float)vWidth + 2f, num6), (Brush)(object)vaLineBrushDx, VALineThickness, vaLineStrokeDx);
					}
					if (num7 >= (float)(panel.Y - 5) && num7 <= (float)(panel.Y + panel.H + 5))
					{
						((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(originX - 2f, num7), new Vector2(originX + (float)vWidth + 2f, num7), (Brush)(object)vaLineBrushDx, VALineThickness, vaLineStrokeDx);
					}
				}
			}
		}
		if (!ShowDelta || forceHideDelta || leg.DeltaByPrice.Count <= 0)
		{
			return;
		}
		double num8 = (double)deltaCompTicks * ((NinjaScriptBase)this).TickSize;
		Dictionary<double, long> dictionary2 = new Dictionary<double, long>();
		Dictionary<double, long> dictionary3;
		lock (leg.SyncObj)
		{
			dictionary3 = (isCurrent ? new Dictionary<double, long>(leg.DeltaByPrice) : leg.DeltaByPrice);
		}
		foreach (KeyValuePair<double, long> item3 in dictionary3)
		{
			double key = Math.Floor(item3.Key / num8 + 1E-06) * num8;
			dictionary2[key] = (dictionary2.TryGetValue(key, out var value) ? (value + item3.Value) : item3.Value);
		}
		long num9 = dictionary2.Values.Select((long v) => Math.Abs(v)).DefaultIfEmpty(0L).Max();
		if (num9 <= 0)
		{
			return;
		}
		foreach (KeyValuePair<double, long> item4 in dictionary2)
		{
			int yByValue5 = chartScale.GetYByValue(item4.Key + num8);
			if (yByValue5 < panel.Y - 50 || yByValue5 > panel.Y + panel.H + 50)
			{
				continue;
			}
			int yByValue6 = chartScale.GetYByValue(item4.Key);
			int num10 = Math.Max(1, Math.Abs(yByValue6 - yByValue5) - ProfileBarSpacingPx);
			float num11 = (float)Math.Min(yByValue5, yByValue6) + (float)ProfileBarSpacingPx / 2f;
			float num12 = (float)((double)dWidth * ((double)Math.Abs(item4.Value) / (double)num9));
			if (!(num12 > 0.5f))
			{
				continue;
			}
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(originX - num12, num11, num12, (float)num10), (Brush)(object)((item4.Value >= 0) ? posBrushDx : negBrushDx));
			if (num10 >= DeltaLabelFontSize + 2)
			{
				string text = item4.Value.ToString("+#;-#;0");
				float num13 = MeasureTextWidth(text);
				float num14 = originX - num13 - 2f;
				float num15 = num11 + (float)num10 / 2f - (float)DeltaLabelFontSize / 2f;
				if (ShowDeltaLabelBackground)
				{
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(num14 - 1f, num15 - 1f, num13 + 2f, (float)(DeltaLabelFontSize + 2)), (Brush)(object)labelBgBrushDx);
				}
				((IndicatorRenderBase)this).RenderTarget.DrawText(text, textFormat, new RectangleF(num14, num15, num13, (float)(DeltaLabelFontSize + 2)), (Brush)(object)textBrushDx);
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
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Expected O, but got Unknown
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Expected O, but got Unknown
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Expected O, but got Unknown
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Expected O, but got Unknown
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
		if (pocBrushDx == null)
		{
			pocBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(POCBrush, 1f));
		}
		if (vaVolBrushDx == null)
		{
			vaVolBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(VABrush, VolumeOpacity));
		}
		if (vaLineBrushDx == null)
		{
			vaLineBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDx(VALineBrush, 1f));
		}
		if (vaLineStrokeDx == null)
		{
			DashStyle dashStyle = (DashStyle)((VALineStyle != VALineStyleEnum.Solid) ? ((VALineStyle == VALineStyleEnum.Dot) ? 2 : ((VALineStyle != VALineStyleEnum.DashDot) ? 1 : 3)) : 0);
			vaLineStrokeDx = new StrokeStyle(((Resource)((IndicatorRenderBase)this).RenderTarget).Factory, new StrokeStyleProperties
			{
				DashStyle = dashStyle
			});
		}
		if (textFormat == null)
		{
			textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", (float)DeltaLabelFontSize)
			{
				TextAlignment = (TextAlignment)2,
				ParagraphAlignment = (ParagraphAlignment)2
			};
		}
		int num = Math.Max(2, GradientSteps);
		if (UseGradient && (volGradientBrushes == null || lastBuiltGradientSteps != num))
		{
			if (volGradientBrushes != null)
			{
				SolidColorBrush[] array = volGradientBrushes;
				foreach (SolidColorBrush obj in array)
				{
					if (obj != null)
					{
						((DisposeBase)obj).Dispose();
					}
				}
			}
			volGradientBrushes = BuildGradientPalette(VolumeBrush, num);
			lastBuiltGradientSteps = num;
		}
		if (!UseGradient || !ShowValueArea || !ShowVAColor || (vaGradientBrushes != null && lastBuiltVAGradientSteps == num))
		{
			return;
		}
		if (vaGradientBrushes != null)
		{
			SolidColorBrush[] array = vaGradientBrushes;
			foreach (SolidColorBrush obj2 in array)
			{
				if (obj2 != null)
				{
					((DisposeBase)obj2).Dispose();
				}
			}
		}
		vaGradientBrushes = BuildGradientPalette(VABrush, num);
		lastBuiltVAGradientSteps = num;
	}

	private SolidColorBrush[] BuildGradientPalette(Brush baseBrush, int steps)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Expected O, but got Unknown
		Color color = (baseBrush as SolidColorBrush)?.Color ?? Colors.White;
		SolidColorBrush[] array = (SolidColorBrush[])(object)new SolidColorBrush[steps];
		for (int i = 0; i < steps; i++)
		{
			float num = (float)i / (float)(steps - 1);
			float num2 = MinBrightness + num * (1f - MinBrightness);
			array[i] = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, new Color4((float)(int)color.R / 255f * num2, (float)(int)color.G / 255f * num2, (float)(int)color.B / 255f * num2, (float)(int)color.A / 255f * VolumeOpacity));
		}
		return array;
	}

	private float MeasureTextWidth(string text)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (textFormat == null)
		{
			return 0f;
		}
		if (textWidthCache.TryGetValue(text, out var value))
		{
			return value;
		}
		TextLayout val = new TextLayout(Globals.DirectWriteFactory, text, textFormat, 1000f, 100f);
		try
		{
			value = val.Metrics.Width;
			textWidthCache[text] = value;
			return value;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private Color4 ToDx(Brush b, float alphaMult)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		Color color = (b as SolidColorBrush)?.Color ?? Colors.White;
		return new Color4((float)(int)color.R / 255f, (float)(int)color.G / 255f, (float)(int)color.B / 255f, (float)(int)color.A / 255f * alphaMult);
	}
}

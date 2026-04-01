using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

public class OrcaRollingProfiles : Indicator
{
	private Queue<OrcaProfileBucket> rollingHistory;

	private OrcaProfileBucket developingBucket;

	private OrcaProfileBucket totalProfile;

	private DateTime currentMinuteToken;

	private double lastBid = double.NaN;

	private double lastAsk = double.NaN;

	private double prevLast = double.NaN;

	private SolidColorBrush volBrushDx;

	private SolidColorBrush pocBrushDx;

	private SolidColorBrush posDeltaBrushDx;

	private SolidColorBrush negDeltaBrushDx;

	private SolidColorBrush[] volGradientBrushes;

	private int lastBuiltGradientSteps = -1;

	private SolidColorBrush vaVolBrushDx;

	private SolidColorBrush[] vaGradientBrushes;

	private int lastBuiltVAGradientSteps = -1;

	private SolidColorBrush vaLineBrushDx;

	private StrokeStyle vaLineStrokeDx;

	private SolidColorBrush deltaTextBrushDx;

	private TextFormat deltaTextFormatDx;

	private Dictionary<string, float> textWidthCache = new Dictionary<string, float>();

	[NinjaScriptProperty]
	[Display(Name = "1. Rolling Period", Order = 1, GroupName = "1. Data")]
	public RollingProfilePeriod Period { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "2. Operating Mode", Order = 2, GroupName = "1. Data")]
	public ProfileOperatingMode Mode { get; set; }

	[NinjaScriptProperty]
	[Range(1, 3000)]
	[Display(Name = "3. Minutes In Trading Day", Order = 3, GroupName = "1. Data", Description = "Multiplier for multiday logic.")]
	public int MinutesPerDay { get; set; }

	[NinjaScriptProperty]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
	[Display(Name = "4. RTH Start Time", Order = 4, GroupName = "1. Data")]
	public TimeSpan RthStartTime { get; set; }

	[NinjaScriptProperty]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
	[Display(Name = "5. RTH End Time", Order = 5, GroupName = "1. Data")]
	public TimeSpan RthEndTime { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Volume Tick Compression", Order = 6, GroupName = "1. Data")]
	public int VolumeTickCompression { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Delta Tick Compression", Order = 7, GroupName = "1. Data")]
	public int DeltaTickCompression { get; set; }

	[NinjaScriptProperty]
	[Range(10, 1000)]
	[Display(Name = "Profile Width (px)", Order = 1, GroupName = "2. Layout")]
	public int ProfileWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(10, 500)]
	[Display(Name = "Delta Width (px)", Order = 2, GroupName = "2. Layout")]
	public int DeltaWidthPx { get; set; }

	[NinjaScriptProperty]
	[Range(0, 1000)]
	[Display(Name = "Right Canvas Offset (px)", Order = 3, GroupName = "2. Layout")]
	public int RightOffsetPx { get; set; }

	[NinjaScriptProperty]
	[Range(0, 10)]
	[Display(Name = "Bar Spacing (px)", Order = 4, GroupName = "2. Layout")]
	public int ProfileBarSpacingPx { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Volume", Order = 1, GroupName = "3. Visibility")]
	public bool ShowVolume { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Delta", Order = 2, GroupName = "3. Visibility")]
	public bool ShowDelta { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show POC", Order = 3, GroupName = "3. Visibility")]
	public bool ShowPOC { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Use Gradient", Order = 1, GroupName = "4. Gradient")]
	public bool UseGradient { get; set; }

	[NinjaScriptProperty]
	[Range(2, 64)]
	[Display(Name = "Gradient Steps", Order = 2, GroupName = "4. Gradient")]
	public int GradientSteps { get; set; }

	[NinjaScriptProperty]
	[Range(0.009999999776482582, 1.0)]
	[Display(Name = "Minimum Brightness", Order = 3, GroupName = "4. Gradient")]
	public float MinBrightness { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Value Area", Order = 1, GroupName = "5. Value Area")]
	public bool ShowValueArea { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show VA Color", Order = 2, GroupName = "5. Value Area")]
	public bool ShowVAColor { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show VA Lines", Order = 3, GroupName = "5. Value Area")]
	public bool ShowVALines { get; set; }

	[NinjaScriptProperty]
	[Range(1, 100)]
	[Display(Name = "Value Area Percent", Order = 4, GroupName = "5. Value Area")]
	public int ValueAreaPercent { get; set; }

	[NinjaScriptProperty]
	[Range(0.10000000149011612, 10.0)]
	[Display(Name = "VA Line Thickness", Order = 5, GroupName = "5. Value Area")]
	public float VALineThickness { get; set; }

	[XmlIgnore]
	[Display(Name = "Volume Background", Order = 1, GroupName = "6. Colors")]
	public Brush VolumeBrush { get; set; }

	[Browsable(false)]
	public string VolumeBrushSerializable
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

	[NinjaScriptProperty]
	[Range(0.10000000149011612, 1.0)]
	[Display(Name = "Volume Opacity", Order = 2, GroupName = "6. Colors")]
	public float VolumeOpacity { get; set; }

	[XmlIgnore]
	[Display(Name = "POC Color", Order = 3, GroupName = "6. Colors")]
	public Brush POCBrush { get; set; }

	[Browsable(false)]
	public string POCBrushSerializable
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
	[Display(Name = "Value Area Background", Order = 4, GroupName = "6. Colors")]
	public Brush VABrush { get; set; }

	[Browsable(false)]
	public string VABrushSerializable
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
	[Display(Name = "Value Area Lines", Order = 5, GroupName = "6. Colors")]
	public Brush VALineBrush { get; set; }

	[Browsable(false)]
	public string VALineBrushSerializable
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

	[XmlIgnore]
	[Display(Name = "Positive Delta", Order = 6, GroupName = "6. Colors")]
	public Brush PositiveDeltaBrush { get; set; }

	[Browsable(false)]
	public string PositiveDeltaBrushSerializable
	{
		get
		{
			return Serialize.BrushToString(PositiveDeltaBrush);
		}
		set
		{
			PositiveDeltaBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "Negative Delta", Order = 7, GroupName = "6. Colors")]
	public Brush NegativeDeltaBrush { get; set; }

	[Browsable(false)]
	public string NegativeDeltaBrushSerializable
	{
		get
		{
			return Serialize.BrushToString(NegativeDeltaBrush);
		}
		set
		{
			NegativeDeltaBrush = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0.10000000149011612, 1.0)]
	[Display(Name = "Delta Opacity", Order = 8, GroupName = "6. Colors")]
	public float DeltaOpacity { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Text", Order = 1, GroupName = "7. Delta Text")]
	public bool ShowDeltaText { get; set; }

	[NinjaScriptProperty]
	[Range(0, 1000000)]
	[Display(Name = "Min Threshold", Order = 2, GroupName = "7. Delta Text")]
	public int DeltaTextMinThreshold { get; set; }

	[XmlIgnore]
	[Display(Name = "Text Color", Order = 3, GroupName = "7. Delta Text")]
	public Brush DeltaTextBrush { get; set; }

	[Browsable(false)]
	public string DeltaTextBrushSerializable
	{
		get
		{
			return Serialize.BrushToString(DeltaTextBrush);
		}
		set
		{
			DeltaTextBrush = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(6.0, 36.0)]
	[Display(Name = "Font Size", Order = 4, GroupName = "7. Delta Text")]
	public float DeltaTextFontSize { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Invalid comparison between Unknown and I4
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Invalid comparison between Unknown and I4
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = "Orca Rolling Profiles";
			((NinjaScript)this).Description = "Dynamically rolling O(1) volume profiles representing right-aligned active depth.";
			((NinjaScriptBase)this).Calculate = (Calculate)2;
			((NinjaScriptBase)this).IsOverlay = true;
			Period = RollingProfilePeriod.Day1;
			Mode = ProfileOperatingMode.FullSession;
			MinutesPerDay = 1380;
			RthStartTime = new TimeSpan(9, 30, 0);
			RthEndTime = new TimeSpan(16, 0, 0);
			VolumeTickCompression = 4;
			DeltaTickCompression = 10;
			ProfileWidthPx = 150;
			DeltaWidthPx = 60;
			RightOffsetPx = 60;
			ProfileBarSpacingPx = 0;
			ShowVolume = true;
			ShowDelta = true;
			ShowPOC = true;
			UseGradient = true;
			GradientSteps = 16;
			MinBrightness = 0.2f;
			ShowValueArea = true;
			ShowVAColor = true;
			ShowVALines = true;
			ValueAreaPercent = 70;
			VALineThickness = 1.5f;
			VolumeBrush = Brushes.RoyalBlue;
			VolumeOpacity = 0.85f;
			POCBrush = Brushes.DodgerBlue;
			VABrush = Brushes.CornflowerBlue;
			VALineBrush = Brushes.White;
			PositiveDeltaBrush = Brushes.Lime;
			NegativeDeltaBrush = Brushes.Red;
			DeltaOpacity = 0.85f;
			ShowDeltaText = true;
			DeltaTextMinThreshold = 10;
			DeltaTextBrush = Brushes.White;
			DeltaTextFontSize = 11f;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			rollingHistory = new Queue<OrcaProfileBucket>(30000);
			developingBucket = new OrcaProfileBucket();
			totalProfile = new OrcaProfileBucket();
			currentMinuteToken = DateTime.MinValue;
			textWidthCache.Clear();
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
			SolidColorBrush obj = volBrushDx;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			SolidColorBrush obj2 = pocBrushDx;
			if (obj2 != null)
			{
				((DisposeBase)obj2).Dispose();
			}
			SolidColorBrush obj3 = posDeltaBrushDx;
			if (obj3 != null)
			{
				((DisposeBase)obj3).Dispose();
			}
			SolidColorBrush obj4 = negDeltaBrushDx;
			if (obj4 != null)
			{
				((DisposeBase)obj4).Dispose();
			}
			SolidColorBrush obj5 = vaVolBrushDx;
			if (obj5 != null)
			{
				((DisposeBase)obj5).Dispose();
			}
			SolidColorBrush obj6 = vaLineBrushDx;
			if (obj6 != null)
			{
				((DisposeBase)obj6).Dispose();
			}
			StrokeStyle obj7 = vaLineStrokeDx;
			if (obj7 != null)
			{
				((DisposeBase)obj7).Dispose();
			}
			if (volGradientBrushes != null)
			{
				for (int i = 0; i < volGradientBrushes.Length; i++)
				{
					SolidColorBrush obj8 = volGradientBrushes[i];
					if (obj8 != null)
					{
						((DisposeBase)obj8).Dispose();
					}
				}
			}
			if (vaGradientBrushes == null)
			{
				return;
			}
			for (int j = 0; j < vaGradientBrushes.Length; j++)
			{
				SolidColorBrush obj9 = vaGradientBrushes[j];
				if (obj9 != null)
				{
					((DisposeBase)obj9).Dispose();
				}
			}
		}
		catch
		{
		}
		finally
		{
			volBrushDx = null;
			pocBrushDx = null;
			posDeltaBrushDx = null;
			negDeltaBrushDx = null;
			vaVolBrushDx = null;
			vaLineBrushDx = null;
			vaLineStrokeDx = null;
			volGradientBrushes = null;
			vaGradientBrushes = null;
			deltaTextBrushDx = null;
			deltaTextFormatDx = null;
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
		if (((NinjaScriptBase)this).BarsInProgress != 1)
		{
			return;
		}
		DateTime dateTime = ((NinjaScriptBase)this).Time[0];
		if (Mode == ProfileOperatingMode.RthOnly && (dateTime.TimeOfDay < RthStartTime || dateTime.TimeOfDay > RthEndTime))
		{
			return;
		}
		DateTime dateTime2 = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
		if (dateTime2 > currentMinuteToken)
		{
			if (currentMinuteToken != DateTime.MinValue)
			{
				rollingHistory.Enqueue(developingBucket);
				int num = ((Mode == ProfileOperatingMode.RthOnly) ? ((int)(RthEndTime - RthStartTime).TotalMinutes) : MinutesPerDay);
				int num2 = ((Period == RollingProfilePeriod.Day1) ? num : ((Period == RollingProfilePeriod.Days2) ? (num * 2) : ((Period == RollingProfilePeriod.Days5) ? (num * 5) : ((Period == RollingProfilePeriod.Days10) ? (num * 10) : ((Period != RollingProfilePeriod.Days20) ? ((int)Period) : (num * 20))))));
				int num3 = (int)(dateTime2 - currentMinuteToken).TotalMinutes;
				if (num3 > 1 && num3 <= 720)
				{
					int num4 = Math.Min(num3 - 1, num2);
					for (int i = 0; i < num4; i++)
					{
						rollingHistory.Enqueue(new OrcaProfileBucket());
					}
				}
				developingBucket = new OrcaProfileBucket();
				while (rollingHistory.Count >= num2)
				{
					OrcaProfileBucket orcaProfileBucket = rollingHistory.Dequeue();
					foreach (KeyValuePair<double, long> item in orcaProfileBucket.VolByPrice)
					{
						if (totalProfile.VolByPrice.TryGetValue(item.Key, out var value))
						{
							long num5 = value - item.Value;
							if (num5 <= 0)
							{
								totalProfile.VolByPrice.Remove(item.Key);
							}
							else
							{
								totalProfile.VolByPrice[item.Key] = num5;
							}
						}
					}
					foreach (KeyValuePair<double, long> item2 in orcaProfileBucket.DeltaByPrice)
					{
						if (totalProfile.DeltaByPrice.TryGetValue(item2.Key, out var value2))
						{
							long num6 = value2 - item2.Value;
							if (num6 == 0L)
							{
								totalProfile.DeltaByPrice.Remove(item2.Key);
							}
							else
							{
								totalProfile.DeltaByPrice[item2.Key] = num6;
							}
						}
					}
				}
			}
			currentMinuteToken = dateTime2;
		}
		else if (dateTime2 < currentMinuteToken)
		{
			rollingHistory.Clear();
			developingBucket = new OrcaProfileBucket();
			totalProfile = new OrcaProfileBucket();
			currentMinuteToken = dateTime2;
		}
		double num7 = ((NinjaScriptBase)this).Close[0];
		long num8 = (long)((NinjaScriptBase)this).Volume[0];
		if (num8 <= 0)
		{
			return;
		}
		double num9 = (double)VolumeTickCompression * ((NinjaScriptBase)this).TickSize;
		double key = Math.Floor(num7 / num9 + 1E-06) * num9;
		if (developingBucket.VolByPrice.TryGetValue(key, out var value3))
		{
			developingBucket.VolByPrice[key] = value3 + num8;
		}
		else
		{
			developingBucket.VolByPrice[key] = num8;
		}
		if (totalProfile.VolByPrice.TryGetValue(key, out var value4))
		{
			totalProfile.VolByPrice[key] = value4 + num8;
		}
		else
		{
			totalProfile.VolByPrice[key] = num8;
		}
		long num10 = 0L;
		if (!double.IsNaN(lastAsk) && !double.IsNaN(lastBid) && lastAsk > 0.0 && lastBid > 0.0 && lastAsk >= lastBid)
		{
			if (num7 >= lastAsk)
			{
				num10 = num8;
			}
			else if (num7 <= lastBid)
			{
				num10 = -num8;
			}
			else if (!double.IsNaN(prevLast))
			{
				num10 = ((num7 > prevLast) ? num8 : ((num7 < prevLast) ? (-num8) : 0));
			}
		}
		else if (!double.IsNaN(prevLast))
		{
			num10 = ((num7 > prevLast) ? num8 : ((num7 < prevLast) ? (-num8) : 0));
		}
		prevLast = num7;
		if (num10 != 0L)
		{
			double num11 = (double)DeltaTickCompression * ((NinjaScriptBase)this).TickSize;
			double key2 = Math.Floor(num7 / num11 + 1E-06) * num11;
			if (developingBucket.DeltaByPrice.TryGetValue(key2, out var value5))
			{
				developingBucket.DeltaByPrice[key2] = value5 + num10;
			}
			else
			{
				developingBucket.DeltaByPrice[key2] = num10;
			}
			if (totalProfile.DeltaByPrice.TryGetValue(key2, out var value6))
			{
				totalProfile.DeltaByPrice[key2] = value6 + num10;
			}
			else
			{
				totalProfile.DeltaByPrice[key2] = num10;
			}
		}
	}

	private void EnsureDxResources()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Expected O, but got Unknown
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Expected O, but got Unknown
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Expected O, but got Unknown
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Expected O, but got Unknown
		if (((IndicatorRenderBase)this).RenderTarget != null)
		{
			if (volBrushDx == null)
			{
				volBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(VolumeBrush, VolumeOpacity));
			}
			if (pocBrushDx == null)
			{
				pocBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(POCBrush, VolumeOpacity));
			}
			if (posDeltaBrushDx == null)
			{
				posDeltaBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(PositiveDeltaBrush, DeltaOpacity));
			}
			if (negDeltaBrushDx == null)
			{
				negDeltaBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(NegativeDeltaBrush, DeltaOpacity));
			}
			if (vaVolBrushDx == null)
			{
				vaVolBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(VABrush, VolumeOpacity));
			}
			if (vaLineBrushDx == null)
			{
				vaLineBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(VALineBrush, 1f));
			}
			if (vaLineStrokeDx == null)
			{
				StrokeStyleProperties val = new StrokeStyleProperties
				{
					DashStyle = (DashStyle)1
				};
				vaLineStrokeDx = new StrokeStyle(((Resource)((IndicatorRenderBase)this).RenderTarget).Factory, val);
			}
			if (deltaTextBrushDx == null)
			{
				deltaTextBrushDx = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, ToDxColor(DeltaTextBrush, 1f));
			}
			if (deltaTextFormatDx == null)
			{
				deltaTextFormatDx = new TextFormat(Globals.DirectWriteFactory, "Arial", (FontWeight)700, (FontStyle)0, DeltaTextFontSize)
				{
					TextAlignment = (TextAlignment)2,
					ParagraphAlignment = (ParagraphAlignment)2
				};
			}
			if (UseGradient && (volGradientBrushes == null || lastBuiltGradientSteps != GradientSteps))
			{
				BuildGradient(VolumeBrush, VolumeOpacity, ref volGradientBrushes);
				lastBuiltGradientSteps = GradientSteps;
			}
			if (UseGradient && ShowValueArea && ShowVAColor && (vaGradientBrushes == null || lastBuiltVAGradientSteps != GradientSteps))
			{
				BuildGradient(VABrush, VolumeOpacity, ref vaGradientBrushes);
				lastBuiltVAGradientSteps = GradientSteps;
			}
		}
	}

	private void BuildGradient(Brush baseWpfBrush, float opacity, ref SolidColorBrush[] palette)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		if (((IndicatorRenderBase)this).RenderTarget == null)
		{
			return;
		}
		if (palette != null)
		{
			for (int i = 0; i < palette.Length; i++)
			{
				SolidColorBrush obj = palette[i];
				if (obj != null)
				{
					((DisposeBase)obj).Dispose();
				}
			}
		}
		palette = (SolidColorBrush[])(object)new SolidColorBrush[GradientSteps];
		Color4 val = ToDxColor(baseWpfBrush, opacity);
		float red = val.Red;
		float green = val.Green;
		float blue = val.Blue;
		Color4 val2 = default(Color4);
		for (int j = 0; j < GradientSteps; j++)
		{
			float num = ((GradientSteps > 1) ? ((float)j / (float)(GradientSteps - 1)) : 1f);
			float num2 = MinBrightness + (1f - MinBrightness) * num;
			((Color4)(ref val2))._002Ector(red * num2, green * num2, blue * num2, opacity);
			try
			{
				palette[j] = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, val2);
			}
			catch
			{
				palette = null;
				break;
			}
		}
	}

	private Color4 ToDxColor(Brush wpfBrush, float opacity)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (wpfBrush is SolidColorBrush { Color: var color } solidColorBrush)
		{
			float num = (float)(int)color.A / 255f * opacity;
			float num2 = (float)(int)solidColorBrush.Color.R / 255f;
			float num3 = (float)(int)solidColorBrush.Color.G / 255f;
			float num4 = (float)(int)solidColorBrush.Color.B / 255f;
			return new Color4(num2, num3, num4, num);
		}
		return new Color4(1f, 1f, 1f, opacity);
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
		long num = 0L;
		foreach (KeyValuePair<double, long> item in volMap)
		{
			num += item.Value;
		}
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
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0404: Unknown result type (might be due to invalid IL or missing references)
		//IL_063d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0740: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c8: Expected O, but got Unknown
		//IL_06ca: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
			EnsureDxResources();
			if (totalProfile == null || totalProfile.VolByPrice.Count == 0)
			{
				return;
			}
			float num = ((IndicatorRenderBase)this).ChartPanel.Y;
			float num2 = ((IndicatorRenderBase)this).ChartPanel.Y + ((IndicatorRenderBase)this).ChartPanel.H;
			float num3 = ((IndicatorRenderBase)this).ChartPanel.X + ((IndicatorRenderBase)this).ChartPanel.W - RightOffsetPx;
			long num4 = 0L;
			double num5 = double.NaN;
			double vahPrice = double.NaN;
			double valPrice = double.NaN;
			bool flag = false;
			foreach (KeyValuePair<double, long> item in totalProfile.VolByPrice)
			{
				if (item.Value > num4)
				{
					num4 = item.Value;
					num5 = item.Key;
				}
			}
			if (num4 > 0 && ShowValueArea && (ShowVAColor || ShowVALines))
			{
				flag = CalcValueArea(totalProfile.VolByPrice, num5, out vahPrice, out valPrice);
			}
			if (num4 <= 0)
			{
				return;
			}
			double num6 = (double)VolumeTickCompression * ((NinjaScriptBase)this).TickSize;
			if (ShowVolume)
			{
				RectangleF val = default(RectangleF);
				foreach (KeyValuePair<double, long> item2 in totalProfile.VolByPrice)
				{
					double key = item2.Key;
					long value = item2.Value;
					int yByValue = chartScale.GetYByValue(key + num6);
					int yByValue2 = chartScale.GetYByValue(key);
					if ((float)yByValue2 < num - 20f || (float)yByValue > num2 + 20f)
					{
						continue;
					}
					int num7 = Math.Max(1, Math.Abs(yByValue2 - yByValue) - ProfileBarSpacingPx);
					float num8 = (float)Math.Min(yByValue, yByValue2) + (float)ProfileBarSpacingPx / 2f;
					float num9 = (float)((double)ProfileWidthPx * ((double)value / (double)num4));
					if (num9 < 0.5f)
					{
						continue;
					}
					((RectangleF)(ref val))._002Ector(num3 - num9, num8, num9, (float)num7);
					bool flag2 = flag && key >= valPrice - ((NinjaScriptBase)this).TickSize * 0.01 && key <= vahPrice + ((NinjaScriptBase)this).TickSize * 0.01;
					SolidColorBrush val2;
					if (ShowPOC && Math.Abs(key - num5) < ((NinjaScriptBase)this).TickSize * 0.01)
					{
						val2 = pocBrushDx;
					}
					else if (!UseGradient)
					{
						val2 = ((!(ShowValueArea && ShowVAColor && flag2)) ? volBrushDx : vaVolBrushDx);
					}
					else
					{
						SolidColorBrush[] array = ((ShowValueArea && ShowVAColor && flag2) ? vaGradientBrushes : volGradientBrushes);
						if (array != null)
						{
							double num10 = (double)value / (double)num4;
							int num11 = array.Length;
							int num12 = (int)(num10 * (double)(num11 - 1));
							if (num12 < 0)
							{
								num12 = 0;
							}
							if (num12 >= num11)
							{
								num12 = num11 - 1;
							}
							val2 = array[num12];
						}
						else
						{
							val2 = volBrushDx;
						}
					}
					((IndicatorRenderBase)this).RenderTarget.FillRectangle(val, (Brush)(object)val2);
				}
				if (flag && ShowValueArea && ShowVALines && vaLineBrushDx != null)
				{
					float num13 = num3 - (float)ProfileWidthPx - 2f;
					float num14 = num3 + 2f;
					float num15 = chartScale.GetYByValue(vahPrice + num6);
					if (num15 >= num - 5f && num15 <= num2 + 5f)
					{
						((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num13, num15), new Vector2(num14, num15), (Brush)(object)vaLineBrushDx, VALineThickness, vaLineStrokeDx);
					}
					float num16 = chartScale.GetYByValue(valPrice);
					if (num16 >= num - 5f && num16 <= num2 + 5f)
					{
						((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num13, num16), new Vector2(num14, num16), (Brush)(object)vaLineBrushDx, VALineThickness, vaLineStrokeDx);
					}
				}
			}
			if (!ShowDelta || totalProfile.DeltaByPrice.Count <= 0)
			{
				return;
			}
			double num17 = (double)DeltaTickCompression * ((NinjaScriptBase)this).TickSize;
			Dictionary<double, long> dictionary = new Dictionary<double, long>();
			foreach (KeyValuePair<double, long> item3 in totalProfile.DeltaByPrice)
			{
				double key2 = Math.Floor(item3.Key / num17 + 1E-06) * num17;
				if (dictionary.TryGetValue(key2, out var value2))
				{
					dictionary[key2] = value2 + item3.Value;
				}
				else
				{
					dictionary[key2] = item3.Value;
				}
			}
			long num18 = 0L;
			foreach (KeyValuePair<double, long> item4 in dictionary)
			{
				long num19 = Math.Abs(item4.Value);
				if (num19 > num18)
				{
					num18 = num19;
				}
			}
			if (num18 <= 0)
			{
				return;
			}
			RectangleF val4 = default(RectangleF);
			RectangleF val6 = default(RectangleF);
			foreach (KeyValuePair<double, long> item5 in dictionary)
			{
				int yByValue3 = chartScale.GetYByValue(item5.Key + num17);
				int yByValue4 = chartScale.GetYByValue(item5.Key);
				if ((float)yByValue4 < num - 20f || (float)yByValue3 > num2 + 20f)
				{
					continue;
				}
				int num20 = Math.Max(1, Math.Abs(yByValue4 - yByValue3) - ProfileBarSpacingPx);
				float num21 = (float)Math.Min(yByValue3, yByValue4) + (float)ProfileBarSpacingPx / 2f;
				float num22 = (float)((double)DeltaWidthPx * ((double)Math.Abs(item5.Value) / (double)num18));
				if (num22 < 0.5f && (!ShowDeltaText || deltaTextFormatDx == null || deltaTextBrushDx == null))
				{
					continue;
				}
				num22 = Math.Max(num22, 0.5f);
				SolidColorBrush val3 = ((item5.Value >= 0) ? posDeltaBrushDx : negDeltaBrushDx);
				((RectangleF)(ref val4))._002Ector(num3, num21, num22, (float)num20);
				((IndicatorRenderBase)this).RenderTarget.FillRectangle(val4, (Brush)(object)val3);
				if (!ShowDeltaText || Math.Abs(item5.Value) < DeltaTextMinThreshold || deltaTextFormatDx == null || deltaTextBrushDx == null || num20 < 8)
				{
					continue;
				}
				string text = item5.Value.ToString();
				if (!textWidthCache.TryGetValue(text, out var value3))
				{
					TextLayout val5 = new TextLayout(Globals.DirectWriteFactory, text, deltaTextFormatDx, 1000f, 1000f);
					try
					{
						value3 = val5.Metrics.Width + 2f;
						if (textWidthCache.Count > 1000)
						{
							textWidthCache.Clear();
						}
						textWidthCache[text] = value3;
					}
					finally
					{
						((IDisposable)val5)?.Dispose();
					}
				}
				float num23 = num3 + num22 + 2f;
				((RectangleF)(ref val6))._002Ector(num23, num21, value3, (float)num20);
				((IndicatorRenderBase)this).RenderTarget.DrawText(text, deltaTextFormatDx, val6, (Brush)(object)deltaTextBrushDx);
			}
		}
		catch (Exception)
		{
			DisposeDx();
		}
	}
}

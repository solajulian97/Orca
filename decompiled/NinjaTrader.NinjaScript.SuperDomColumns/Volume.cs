using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.SuperDom;

namespace NinjaTrader.NinjaScript.SuperDomColumns;

public class Volume : SuperDomColumn
{
	private readonly object barsSync = new object();

	private bool clearLoadingSent;

	private FontFamily fontFamily;

	private FontStyle fontStyle;

	private FontWeight fontWeight;

	private Pen gridPen;

	private double halfPenWidth;

	private bool heightUpdateNeeded;

	private int lastMaxIndex = -1;

	private long maxVolume;

	private bool mouseEventsSubscribed;

	private double textHeight;

	private Point textPosition = new Point(4.0, 0.0);

	private long totalBuyVolume;

	private long totalLastVolume;

	private long totalSellVolume;

	private Typeface typeFace;

	[XmlIgnore]
	[Browsable(false)]
	public ConcurrentDictionary<double, long> Buys { get; set; }

	[XmlIgnore]
	[Browsable(false)]
	public ConcurrentDictionary<double, long> LastVolumes { get; set; }

	[XmlIgnore]
	[Browsable(false)]
	public ConcurrentDictionary<double, long> Sells { get; set; }

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptColumnBaseBackground", GroupName = "PropertyCategoryVisual", Order = 130)]
	public Brush BackColor { get; set; }

	[Browsable(false)]
	public string BackColorSerialize
	{
		get
		{
			return Serialize.BrushToString(BackColor, (object)"brushPriceColumnBackground");
		}
		set
		{
			BackColor = Serialize.StringToBrush(value, (object)"brushPriceColumnBackground");
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptBarColor", GroupName = "PropertyCategoryVisual", Order = 110)]
	public Brush BarColor { get; set; }

	[Browsable(false)]
	public string BarColorSerialize
	{
		get
		{
			return Serialize.BrushToString(BarColor);
		}
		set
		{
			BarColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptBuyColor", GroupName = "PropertyCategoryVisual", Order = 120)]
	public Brush BuyColor { get; set; }

	[Browsable(false)]
	public string BuyColorSerialize
	{
		get
		{
			return Serialize.BrushToString(BuyColor);
		}
		set
		{
			BuyColor = Serialize.StringToBrush(value);
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDisplayText", GroupName = "PropertyCategoryVisual", Order = 175)]
	public bool DisplayText { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDisplayType", GroupName = "NinjaScriptSetup", Order = 150)]
	public DisplayType DisplayType { get; set; }

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptColumnBaseForeground", GroupName = "PropertyCategoryVisual", Order = 140)]
	public Brush ForeColor { get; set; }

	[Browsable(false)]
	public string ForeColorSerialize
	{
		get
		{
			return Serialize.BrushToString(ForeColor, (object)"brushVolumeColumnForeground");
		}
		set
		{
			ForeColor = Serialize.StringToBrush(value, (object)"brushVolumeColumnForeground");
		}
	}

	[XmlIgnore]
	[Browsable(false)]
	public Brush ImmutableBarColor { get; set; }

	[Browsable(false)]
	public string ImmutableBarColorSerialize
	{
		get
		{
			return Serialize.BrushToString(ImmutableBarColor, (object)"CustomVolume.ImmutableBarColor");
		}
		set
		{
			ImmutableBarColor = Serialize.StringToBrush(value, (object)"CustomVolume.ImmutableBarColor");
		}
	}

	[XmlIgnore]
	[Browsable(false)]
	public Brush ImmutableForeColor { get; set; }

	[Browsable(false)]
	public string ImmutableForeColorSerialize
	{
		get
		{
			return Serialize.BrushToString(ImmutableForeColor, (object)"CustomVolume.ImmutableForeColor");
		}
		set
		{
			ImmutableForeColor = Serialize.StringToBrush(value, (object)"CustomVolume.ImmutableForeColor");
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptSellColor", GroupName = "PropertyCategoryVisual", Order = 170)]
	public Brush SellColor { get; set; }

	[Browsable(false)]
	public string SellColorSerialize
	{
		get
		{
			return Serialize.BrushToString(SellColor);
		}
		set
		{
			SellColor = Serialize.StringToBrush(value);
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "IndicatorSuperDomBaseTradingHoursTemplate", GroupName = "NinjaScriptTimeFrame", Order = 60)]
	[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
	[RefreshProperties(RefreshProperties.All)]
	[TypeConverter(typeof(TradingHoursDataConverter))]
	[XmlIgnore]
	public TradingHours TradingHoursInstance
	{
		get
		{
			if (TradingHoursSerializable.Length > 0)
			{
				TradingHours val = TradingHours.All.FirstOrDefault((TradingHours t) => t.Name == TradingHoursSerializable);
				if (val != null)
				{
					return val;
				}
			}
			return TradingHours.UseInstrumentSettingsInstance;
		}
		set
		{
			TradingHoursSerializable = ((value == TradingHours.UseInstrumentSettingsInstance) ? string.Empty : value.Name);
		}
	}

	[Browsable(false)]
	public string TradingHoursSerializable { get; set; } = string.Empty;

	[Display(ResourceType = typeof(Resource), Name = "GuiType", GroupName = "NinjaScriptSetup", Order = 180)]
	public VolumeType VolumeType { get; set; }

	private void OnBarsUpdate(object sender, BarsUpdateEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State != 3)
		{
			return;
		}
		SuperDomViewModel superDom = ((SuperDomColumn)this).SuperDom;
		if (superDom == null || !superDom.IsConnected)
		{
			return;
		}
		if (((SuperDomColumn)this).SuperDom.IsReloading)
		{
			((SuperDomColumn)this).OnPropertyChanged("OnBarsUpdate");
			return;
		}
		lock (barsSync)
		{
			int maxIndex = e.MaxIndex;
			for (int i = lastMaxIndex + 1; i <= maxIndex; i++)
			{
				if (e.BarsSeries.GetIsFirstBarOfSession(i))
				{
					maxVolume = 0L;
					totalBuyVolume = 0L;
					totalLastVolume = 0L;
					totalSellVolume = 0L;
					Sells.Clear();
					Buys.Clear();
					LastVolumes.Clear();
				}
				double ask = e.BarsSeries.GetAsk(i);
				double bid = e.BarsSeries.GetBid(i);
				double close = e.BarsSeries.GetClose(i);
				long volume = e.BarsSeries.GetVolume(i);
				if (ask > double.MinValue && close >= ask)
				{
					Buys.AddOrUpdate(close, volume, (double _, long oldVolume) => oldVolume + volume);
					totalBuyVolume += volume;
				}
				else if (bid > double.MinValue && close <= bid)
				{
					Sells.AddOrUpdate(close, volume, (double _, long oldVolume) => oldVolume + volume);
					totalSellVolume += volume;
				}
				long newVolume;
				LastVolumes.AddOrUpdate(close, newVolume = volume, (double _, long oldVolume) => newVolume = oldVolume + volume);
				totalLastVolume += volume;
				if (newVolume > maxVolume)
				{
					maxVolume = newVolume;
				}
			}
			lastMaxIndex = e.MaxIndex;
			if (!clearLoadingSent)
			{
				((SuperDomColumn)this).SuperDom.Dispatcher.InvokeAsync(delegate
				{
					((SuperDomColumn)this).SuperDom.ClearLoadingString();
				});
				clearLoadingSent = true;
			}
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		((SuperDomColumn)this).OnPropertyChanged("OnMouseLeave");
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		((SuperDomColumn)this).OnPropertyChanged("OnMouseEnter");
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		((SuperDomColumn)this).OnPropertyChanged("OnMouseMove");
	}

	protected override void OnRender(DrawingContext dc, double renderWidth)
	{
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_037e: Invalid comparison between Unknown and I4
		//IL_05e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ed: Invalid comparison between Unknown and I4
		if (gridPen == null && ((SuperDomColumn)this).UiWrapper != null)
		{
			CompositionTarget compositionTarget = PresentationSource.FromVisual(((SuperDomColumn)this).UiWrapper)?.CompositionTarget;
			if (compositionTarget != null)
			{
				double num = 1.0 / compositionTarget.TransformToDevice.M11;
				gridPen = new Pen(Application.Current.TryFindResource("BorderThinBrush") as Brush, 1.0 * num);
				halfPenWidth = gridPen.Thickness * 0.5;
			}
		}
		if (!object.Equals(fontFamily, ((SuperDomColumn)this).SuperDom.Font.Family) || (((SuperDomColumn)this).SuperDom.Font.Italic && fontStyle != FontStyles.Italic) || (!((SuperDomColumn)this).SuperDom.Font.Italic && fontStyle == FontStyles.Italic) || (((SuperDomColumn)this).SuperDom.Font.Bold && fontWeight != FontWeights.Bold) || (!((SuperDomColumn)this).SuperDom.Font.Bold && fontWeight == FontWeights.Bold))
		{
			fontFamily = ((SuperDomColumn)this).SuperDom.Font.Family;
			fontStyle = (((SuperDomColumn)this).SuperDom.Font.Italic ? FontStyles.Italic : FontStyles.Normal);
			fontWeight = (((SuperDomColumn)this).SuperDom.Font.Bold ? FontWeights.Bold : FontWeights.Normal);
			typeFace = new Typeface(fontFamily, fontStyle, fontWeight, FontStretches.Normal);
			heightUpdateNeeded = true;
		}
		Pen pen = gridPen;
		double num2 = ((pen != null) ? (0.0 - pen.Thickness) : 0.0);
		double pixelsPerDip = VisualTreeHelper.GetDpi(((SuperDomColumn)this).UiWrapper).PixelsPerDip;
		lock (((SuperDomColumn)this).SuperDom.Rows)
		{
			foreach (PriceRow row in ((SuperDomColumn)this).SuperDom.Rows)
			{
				if (!(renderWidth - halfPenWidth >= 0.0))
				{
					continue;
				}
				Rect rectangle = new Rect(0.0 - halfPenWidth, num2, renderWidth - halfPenWidth, ((SuperDomColumn)this).SuperDom.ActualRowHeight);
				GuidelineSet guidelineSet = new GuidelineSet();
				guidelineSet.GuidelinesX.Add(rectangle.Left + halfPenWidth);
				guidelineSet.GuidelinesX.Add(rectangle.Right + halfPenWidth);
				guidelineSet.GuidelinesY.Add(rectangle.Top + halfPenWidth);
				guidelineSet.GuidelinesY.Add(rectangle.Bottom + halfPenWidth);
				dc.PushGuidelineSet(guidelineSet);
				dc.DrawRectangle(BackColor, null, rectangle);
				dc.DrawLine(gridPen, new Point(0.0 - gridPen.Thickness, rectangle.Bottom), new Point(renderWidth - halfPenWidth, rectangle.Bottom));
				dc.DrawLine(gridPen, new Point(rectangle.Right, num2), new Point(rectangle.Right, rectangle.Bottom));
				if (((SuperDomColumn)this).SuperDom.IsConnected && !((SuperDomColumn)this).SuperDom.IsReloading && (int)((NinjaScript)this).State == 3)
				{
					long value = 0L;
					long value2 = 0L;
					long num3 = 0L;
					if (VolumeType == VolumeType.Standard)
					{
						if (!LastVolumes.TryGetValue(row.Price, out value2))
						{
							num2 += ((SuperDomColumn)this).SuperDom.ActualRowHeight;
							continue;
						}
						num3 = totalLastVolume;
					}
					else if (VolumeType == VolumeType.BuySell)
					{
						long value3;
						bool num4 = Sells.TryGetValue(row.Price, out value3);
						bool flag = Buys.TryGetValue(row.Price, out value);
						if (!(num4 || flag))
						{
							num2 += ((SuperDomColumn)this).SuperDom.ActualRowHeight;
							continue;
						}
						value2 = value3 + value;
						num3 = totalBuyVolume + totalSellVolume;
					}
					double num5 = renderWidth * ((double)value2 / (double)maxVolume);
					if (num5 - gridPen.Thickness >= 0.0)
					{
						if (VolumeType == VolumeType.Standard)
						{
							dc.DrawRectangle(BarColor, null, new Rect(0.0, num2 + halfPenWidth, (Math.Abs(num5 - renderWidth) < 1E-09) ? (num5 - gridPen.Thickness * 1.5) : (num5 - halfPenWidth), rectangle.Height - gridPen.Thickness));
						}
						else if (VolumeType == VolumeType.BuySell)
						{
							double num6 = num5 * ((double)value / (double)value2);
							if (num5 - halfPenWidth >= 0.0)
							{
								dc.DrawRectangle(SellColor, null, new Rect(0.0, num2 + halfPenWidth, (Math.Abs(num5 - renderWidth) < 1E-09) ? (num5 - gridPen.Thickness * 1.5) : (num5 - halfPenWidth), rectangle.Height - gridPen.Thickness));
							}
							if (num6 - halfPenWidth >= 0.0)
							{
								dc.DrawRectangle(BuyColor, null, new Rect(0.0, num2 + halfPenWidth, num6 - halfPenWidth, rectangle.Height - gridPen.Thickness));
							}
						}
					}
					if (value2 > 0)
					{
						string textToFormat = string.Empty;
						if (DisplayType == DisplayType.Volume)
						{
							textToFormat = (((int)((SuperDomColumn)this).SuperDom.Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume(value2).ToString(Globals.GeneralOptions.CurrentCulture) : value2.ToString(Globals.GeneralOptions.CurrentCulture));
						}
						else if (DisplayType == DisplayType.Percent)
						{
							textToFormat = ((double)value2 / (double)num3).ToString("P1", Globals.GeneralOptions.CurrentCulture);
						}
						if (renderWidth - 6.0 > 0.0 && (DisplayText || rectangle.Contains(Mouse.GetPosition(((SuperDomColumn)this).UiWrapper))))
						{
							FormattedText formattedText = new FormattedText(textToFormat, Globals.GeneralOptions.CurrentCulture, FlowDirection.LeftToRight, typeFace, ((SuperDomColumn)this).SuperDom.Font.Size, ForeColor, pixelsPerDip)
							{
								MaxLineCount = 1,
								MaxTextWidth = renderWidth - 6.0,
								Trimming = TextTrimming.CharacterEllipsis
							};
							if (heightUpdateNeeded)
							{
								textHeight = formattedText.Height;
								heightUpdateNeeded = false;
							}
							textPosition.Y = num2 + (((SuperDomColumn)this).SuperDom.ActualRowHeight - textHeight) / 2.0;
							dc.DrawText(formattedText, textPosition);
						}
					}
					num2 += ((SuperDomColumn)this).SuperDom.ActualRowHeight;
				}
				else
				{
					num2 += ((SuperDomColumn)this).SuperDom.ActualRowHeight;
				}
				dc.Pop();
			}
		}
	}

	public override void OnRestoreValues()
	{
		bool flag = false;
		SolidColorBrush solidColorBrush = Application.Current.FindResource("immutableBrushVolumeColumnForeground") as SolidColorBrush;
		if ((ForeColor as SolidColorBrush).Color == (ImmutableForeColor as SolidColorBrush).Color && (ImmutableForeColor as SolidColorBrush).Color != solidColorBrush.Color)
		{
			ForeColor = solidColorBrush;
			ImmutableForeColor = solidColorBrush;
			flag = true;
		}
		SolidColorBrush solidColorBrush2 = Application.Current.FindResource("immutableBrushVolumeColumnBackground") as SolidColorBrush;
		if ((BarColor as SolidColorBrush).Color == (ImmutableBarColor as SolidColorBrush).Color && (ImmutableBarColor as SolidColorBrush).Color != solidColorBrush2.Color)
		{
			BarColor = solidColorBrush2;
			ImmutableBarColor = solidColorBrush2;
			flag = true;
		}
		if (flag)
		{
			((SuperDomColumn)this).OnPropertyChanged("OnRestoreValues");
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Invalid comparison between Unknown and I4
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Invalid comparison between Unknown and I4
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Invalid comparison between Unknown and I4
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Expected O, but got Unknown
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((SuperDomColumn)this).Name = Resource.NinjaScriptSuperDomColumnVolume;
			((NinjaScript)this).Description = Resource.NinjaScriptSuperDomColumnDescriptionVolume;
			Buys = new ConcurrentDictionary<double, long>();
			BackColor = Application.Current.TryFindResource("brushPriceColumnBackground") as Brush;
			BarColor = Application.Current.TryFindResource("brushVolumeColumnBackground") as Brush;
			BuyColor = Brushes.DarkCyan;
			((SuperDomColumn)this).DefaultWidth = 160.0;
			((SuperDomColumn)this).PreviousWidth = -1.0;
			DisplayText = false;
			DisplayType = DisplayType.Volume;
			ForeColor = Application.Current.TryFindResource("brushVolumeColumnForeground") as Brush;
			ImmutableBarColor = Application.Current.TryFindResource("immutableBrushVolumeColumnBackground") as Brush;
			ImmutableForeColor = Application.Current.TryFindResource("immutableBrushVolumeColumnForeground") as Brush;
			((SuperDomColumn)this).IsDataSeriesRequired = true;
			LastVolumes = new ConcurrentDictionary<double, long>();
			SellColor = Brushes.Crimson;
			Sells = new ConcurrentDictionary<double, long>();
			VolumeType = VolumeType.Standard;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			if (((SuperDomColumn)this).UiWrapper != null)
			{
				CompositionTarget compositionTarget = PresentationSource.FromVisual(((SuperDomColumn)this).UiWrapper)?.CompositionTarget;
				if (compositionTarget != null)
				{
					double num = 1.0 / compositionTarget.TransformToDevice.M11;
					gridPen = new Pen(Application.Current.TryFindResource("BorderThinBrush") as Brush, 1.0 * num);
					halfPenWidth = gridPen.Thickness * 0.5;
				}
			}
			if (((SuperDomColumn)this).SuperDom.Instrument == null || !((SuperDomColumn)this).SuperDom.IsConnected)
			{
				return;
			}
			BarsPeriod barsPeriod = new BarsPeriod
			{
				MarketDataType = (MarketDataType)2,
				BarsPeriodType = (BarsPeriodType)0,
				Value = 1
			};
			((SuperDomColumn)this).SuperDom.Dispatcher.InvokeAsync(delegate
			{
				((SuperDomColumn)this).SuperDom.SetLoadingString();
			});
			clearLoadingSent = false;
			if (((SuperDomColumn)this).BarsRequest != null)
			{
				((SuperDomColumn)this).BarsRequest.Update -= OnBarsUpdate;
				((SuperDomColumn)this).BarsRequest = null;
			}
			((SuperDomColumn)this).BarsRequest = new BarsRequest(((SuperDomColumn)this).SuperDom.Instrument, (Connection.PlaybackConnection != null) ? Connection.PlaybackConnection.Now : Globals.Now, (Connection.PlaybackConnection != null) ? Connection.PlaybackConnection.Now : Globals.Now)
			{
				BarsPeriod = barsPeriod,
				TradingHours = ((TradingHoursSerializable.Length == 0 || TradingHours.Get(TradingHoursSerializable) == null) ? ((SuperDomColumn)this).SuperDom.Instrument.MasterInstrument.TradingHours : TradingHours.Get(TradingHoursSerializable))
			};
			((SuperDomColumn)this).BarsRequest.Update += OnBarsUpdate;
			((SuperDomColumn)this).BarsRequest.Request((Action<BarsRequest, ErrorCode, string>)delegate(BarsRequest request, ErrorCode errorCode, string _)
			{
				//IL_0316: Unknown result type (might be due to invalid IL or missing references)
				//IL_031d: Invalid comparison between Unknown and I4
				//IL_0053: Unknown result type (might be due to invalid IL or missing references)
				//IL_0059: Invalid comparison between Unknown and I4
				//IL_005c: Unknown result type (might be due to invalid IL or missing references)
				//IL_005e: Invalid comparison between Unknown and I4
				//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
				//IL_0061: Unknown result type (might be due to invalid IL or missing references)
				//IL_0067: Invalid comparison between Unknown and I4
				//IL_0113: Unknown result type (might be due to invalid IL or missing references)
				//IL_0119: Expected O, but got Unknown
				if (request == ((SuperDomColumn)this).BarsRequest)
				{
					lastMaxIndex = 0;
					maxVolume = 0L;
					totalBuyVolume = 0L;
					totalLastVolume = 0L;
					totalSellVolume = 0L;
					Sells.Clear();
					Buys.Clear();
					LastVolumes.Clear();
					if ((int)((NinjaScript)this).State < 8)
					{
						if ((int)errorCode == 8)
						{
							if ((int)((NinjaScript)this).State <= 8 && ((SuperDomColumn)this).SuperDom != null && !clearLoadingSent)
							{
								((SuperDomColumn)this).SuperDom.Dispatcher.InvokeAsync(delegate
								{
									((SuperDomColumn)this).SuperDom.ClearLoadingString();
								});
								clearLoadingSent = true;
							}
							request.Update -= OnBarsUpdate;
							request.Dispose();
						}
						else if ((int)errorCode != 0)
						{
							request.Update -= OnBarsUpdate;
							request.Dispose();
							if (((SuperDomColumn)this).SuperDom != null && !clearLoadingSent)
							{
								((SuperDomColumn)this).SuperDom.Dispatcher.InvokeAsync(delegate
								{
									((SuperDomColumn)this).SuperDom.ClearLoadingString();
								});
								clearLoadingSent = true;
							}
						}
						else
						{
							try
							{
								SessionIterator val = new SessionIterator(request.Bars);
								bool flag = request.Bars.BarsType.IncludesEndTimeStamp(false);
								if (val.IsInSession((Connection.PlaybackConnection != null) ? Connection.PlaybackConnection.Now : Globals.Now, flag, request.Bars.BarsType.IsIntraday))
								{
									for (int num2 = 0; num2 < request.Bars.Count; num2++)
									{
										DateTime time = request.Bars.BarsSeries.GetTime(num2);
										if ((!flag || !(time <= val.ActualSessionBegin)) && (flag || !(time < val.ActualSessionBegin)))
										{
											double ask = request.Bars.BarsSeries.GetAsk(num2);
											double bid = request.Bars.BarsSeries.GetBid(num2);
											double close = request.Bars.BarsSeries.GetClose(num2);
											long volume = request.Bars.BarsSeries.GetVolume(num2);
											if (ask > double.MinValue && close >= ask)
											{
												Buys.AddOrUpdate(close, volume, (double num3, long oldVolume) => oldVolume + volume);
												totalBuyVolume += volume;
											}
											else if (bid > double.MinValue && close <= bid)
											{
												Sells.AddOrUpdate(close, volume, (double num3, long oldVolume) => oldVolume + volume);
												totalSellVolume += volume;
											}
											long newVolume;
											LastVolumes.AddOrUpdate(close, newVolume = volume, (double num3, long oldVolume) => newVolume = oldVolume + volume);
											totalLastVolume += volume;
											if (newVolume > maxVolume)
											{
												maxVolume = newVolume;
											}
										}
									}
									lastMaxIndex = request.Bars.Count - 1;
									((SuperDomColumn)this).OnPropertyChanged("OnStateChange");
								}
							}
							catch
							{
								if ((int)((NinjaScript)this).State != 9)
								{
									throw;
								}
							}
							if (((SuperDomColumn)this).SuperDom != null && !clearLoadingSent)
							{
								((SuperDomColumn)this).SuperDom.Dispatcher.InvokeAsync(delegate
								{
									((SuperDomColumn)this).SuperDom.ClearLoadingString();
								});
								clearLoadingSent = true;
							}
						}
					}
				}
			});
		}
		else if ((int)((NinjaScript)this).State == 3)
		{
			if (!DisplayText)
			{
				WeakEventManager<Panel, MouseEventArgs>.AddHandler(((SuperDomColumn)this).UiWrapper, "MouseMove", OnMouseMove);
				WeakEventManager<Panel, MouseEventArgs>.AddHandler(((SuperDomColumn)this).UiWrapper, "MouseEnter", OnMouseEnter);
				WeakEventManager<Panel, MouseEventArgs>.AddHandler(((SuperDomColumn)this).UiWrapper, "MouseLeave", OnMouseLeave);
				mouseEventsSubscribed = true;
			}
		}
		else
		{
			if ((int)((NinjaScript)this).State != 8)
			{
				return;
			}
			if (((SuperDomColumn)this).BarsRequest != null)
			{
				((SuperDomColumn)this).BarsRequest.Update -= OnBarsUpdate;
				((SuperDomColumn)this).BarsRequest.Dispose();
			}
			((SuperDomColumn)this).BarsRequest = null;
			if (((SuperDomColumn)this).SuperDom != null && !clearLoadingSent)
			{
				((SuperDomColumn)this).SuperDom.Dispatcher.InvokeAsync(delegate
				{
					((SuperDomColumn)this).SuperDom.ClearLoadingString();
				});
				clearLoadingSent = true;
			}
			if (!DisplayText && mouseEventsSubscribed)
			{
				WeakEventManager<Panel, MouseEventArgs>.RemoveHandler(((SuperDomColumn)this).UiWrapper, "MouseMove", OnMouseMove);
				WeakEventManager<Panel, MouseEventArgs>.RemoveHandler(((SuperDomColumn)this).UiWrapper, "MouseEnter", OnMouseEnter);
				WeakEventManager<Panel, MouseEventArgs>.RemoveHandler(((SuperDomColumn)this).UiWrapper, "MouseLeave", OnMouseLeave);
				mouseEventsSubscribed = false;
			}
			lastMaxIndex = 0;
			maxVolume = 0L;
			totalBuyVolume = 0L;
			totalLastVolume = 0L;
			totalSellVolume = 0L;
			Sells.Clear();
			Buys.Clear();
			LastVolumes.Clear();
		}
	}
}

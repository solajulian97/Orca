using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.SuperDom;

namespace NinjaTrader.NinjaScript.SuperDomColumns;

public class Recent : SuperDomColumn
{
	private ConcurrentDictionary<double, double> askPriceValues;

	private ConcurrentDictionary<double, double> bidPriceValues;

	private FontFamily fontFamily;

	private FontStyle fontStyle;

	private FontWeight fontWeight;

	private Pen gridPen;

	private double halfPenWidth;

	private bool heightUpdateNeeded;

	private double mostRecentLast;

	private double previousAsk = double.MinValue;

	private double previousBid = double.MinValue;

	private ConcurrentDictionary<double, Timer> resetAskTimers;

	private ConcurrentDictionary<double, Timer> resetBidTimers;

	private double textHeight;

	private Typeface typeFace;

	private bool? wasAskMostRecentlyFilled;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptRecentColumnDiplay", GroupName = "NinjaScriptSetup", Order = 100)]
	public RecentDisplayType DisplayType { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptRecentColumnResetWhen", GroupName = "NinjaScriptSetup", Order = 110)]
	public RecentResetWhen ResetWhen { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptRecentColumnResetTolerance", GroupName = "NinjaScriptSetup", Order = 115)]
	[Range(1, int.MaxValue)]
	public int ResetTolerance { get; set; }

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptRecentColumnAskBackground", GroupName = "PropertyCategoryVisual", Order = 105)]
	public Brush AskBackColor { get; set; }

	[Browsable(false)]
	public string AskBackgroundBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(AskBackColor, (object)"brushAskPriceColumnBackground");
		}
		set
		{
			AskBackColor = Serialize.StringToBrush(value, (object)"brushAskPriceColumnBackground");
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptRecentColumnAskForeground", GroupName = "PropertyCategoryVisual", Order = 111)]
	public Brush AskForeColor { get; set; }

	[Browsable(false)]
	public string AskForeColorSerialize
	{
		get
		{
			return Serialize.BrushToString(AskForeColor);
		}
		set
		{
			AskForeColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptRecentColumnBidBackground", GroupName = "PropertyCategoryVisual", Order = 116)]
	public Brush BidBackColor { get; set; }

	[Browsable(false)]
	public string BidBackgroundBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(BidBackColor, (object)"brushBidPriceColumnBackground");
		}
		set
		{
			BidBackColor = Serialize.StringToBrush(value, (object)"brushBidPriceColumnBackground");
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptRecentColumnBidForeground", GroupName = "PropertyCategoryVisual", Order = 121)]
	public Brush BidForeColor { get; set; }

	[Browsable(false)]
	public string BidForeColorSerialize
	{
		get
		{
			return Serialize.BrushToString(BidForeColor);
		}
		set
		{
			BidForeColor = Serialize.StringToBrush(value);
		}
	}

	public override void OnColumnLabelClicked(object sender, MouseButtonEventArgs e)
	{
		foreach (KeyValuePair<double, Timer> resetAskTimer in resetAskTimers)
		{
			resetAskTimer.Value.Dispose();
		}
		resetAskTimers.Clear();
		foreach (KeyValuePair<double, Timer> resetBidTimer in resetBidTimers)
		{
			resetBidTimer.Value.Dispose();
		}
		resetBidTimers.Clear();
		askPriceValues.Clear();
		bidPriceValues.Clear();
		((SuperDomColumn)this).OnPropertyChanged("OnColumnLabelClicked");
	}

	protected override void OnMarketData(MarketDataEventArgs marketData)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State != 3)
		{
			return;
		}
		if ((int)marketData.MarketDataType == 2)
		{
			double currentAsk = ((SuperDomColumn)this).SuperDom.CurrentAsk;
			double currentBid = ((SuperDomColumn)this).SuperDom.CurrentBid;
			if (MathExtentions.ApproxCompare(marketData.Price, currentAsk) == 0)
			{
				askPriceValues.AddOrUpdate(marketData.Price, marketData.Volume, (double _, double v) => (ResetWhen == RecentResetWhen.PriceReturns) ? ((MathExtentions.ApproxCompare(currentAsk, mostRecentLast) != 0) ? ((double)marketData.Volume) : (v + (double)marketData.Volume)) : (v + (double)marketData.Volume));
				mostRecentLast = marketData.Price;
				wasAskMostRecentlyFilled = true;
			}
			else if (MathExtentions.ApproxCompare(marketData.Price, currentBid) == 0)
			{
				bidPriceValues.AddOrUpdate(marketData.Price, marketData.Volume, (double _, double v) => (ResetWhen == RecentResetWhen.PriceReturns) ? ((MathExtentions.ApproxCompare(currentBid, mostRecentLast) != 0) ? ((double)marketData.Volume) : (v + (double)marketData.Volume)) : (v + (double)marketData.Volume));
				mostRecentLast = marketData.Price;
				wasAskMostRecentlyFilled = false;
			}
			else if (wasAskMostRecentlyFilled == true)
			{
				askPriceValues.AddOrUpdate(currentAsk, marketData.Volume, (double _, double v) => (ResetWhen == RecentResetWhen.PriceReturns) ? ((MathExtentions.ApproxCompare(currentAsk, mostRecentLast) != 0) ? ((double)marketData.Volume) : (v + (double)marketData.Volume)) : (v + (double)marketData.Volume));
				mostRecentLast = currentAsk;
				wasAskMostRecentlyFilled = true;
			}
			else if (wasAskMostRecentlyFilled == false)
			{
				bidPriceValues.AddOrUpdate(currentBid, marketData.Volume, (double _, double v) => (ResetWhen == RecentResetWhen.PriceReturns) ? ((MathExtentions.ApproxCompare(currentBid, mostRecentLast) != 0) ? ((double)marketData.Volume) : (v + (double)marketData.Volume)) : (v + (double)marketData.Volume));
				mostRecentLast = currentBid;
				wasAskMostRecentlyFilled = false;
			}
		}
		if (ResetWhen == RecentResetWhen.PriceReturns)
		{
			return;
		}
		if ((int)marketData.MarketDataType == 0)
		{
			if (resetAskTimers.TryRemove(marketData.Price, out var value))
			{
				value.Dispose();
			}
			if (MathExtentions.ApproxCompare(marketData.Price, previousAsk) != 0)
			{
				if (previousAsk > double.MinValue)
				{
					resetAskTimers[previousAsk] = new Timer(ResetAsk, previousAsk, ResetTolerance, -1);
				}
				previousAsk = marketData.Price;
			}
		}
		else
		{
			if ((int)marketData.MarketDataType != 1)
			{
				return;
			}
			if (resetBidTimers.TryRemove(marketData.Price, out var value2))
			{
				value2.Dispose();
			}
			if (MathExtentions.ApproxCompare(marketData.Price, previousBid) != 0)
			{
				if (previousBid > double.MinValue)
				{
					resetBidTimers[previousBid] = new Timer(ResetBid, previousBid, ResetTolerance, -1);
				}
				previousBid = marketData.Price;
			}
		}
	}

	protected override void OnRender(DrawingContext dc, double renderWidth)
	{
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_043e: Invalid comparison between Unknown and I4
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
				if (DisplayType == RecentDisplayType.BidAsk)
				{
					Rect rectangle2 = new Rect(0.0 - halfPenWidth, num2, renderWidth / 2.0 - halfPenWidth, ((SuperDomColumn)this).SuperDom.ActualRowHeight);
					Rect rectangle3 = new Rect(renderWidth / 2.0 - halfPenWidth, num2, renderWidth / 2.0 - halfPenWidth, ((SuperDomColumn)this).SuperDom.ActualRowHeight);
					dc.DrawRectangle(BidBackColor, null, rectangle2);
					dc.DrawRectangle(AskBackColor, null, rectangle3);
				}
				else if (DisplayType == RecentDisplayType.Ask)
				{
					dc.DrawRectangle(AskBackColor, null, rectangle);
				}
				else if (DisplayType == RecentDisplayType.Bid)
				{
					dc.DrawRectangle(BidBackColor, null, rectangle);
				}
				Pen pen2 = gridPen;
				Pen pen3 = gridPen;
				dc.DrawLine(pen2, new Point((pen3 != null) ? (0.0 - pen3.Thickness) : 0.0, rectangle.Bottom), new Point(renderWidth - halfPenWidth, rectangle.Bottom));
				dc.DrawLine(gridPen, new Point(rectangle.Right, num2), new Point(rectangle.Right, rectangle.Bottom));
				if (((SuperDomColumn)this).SuperDom.IsConnected && !((SuperDomColumn)this).SuperDom.IsReloading && (int)((NinjaScript)this).State == 3)
				{
					RecentDisplayType displayType = DisplayType;
					bool flag = (uint)(displayType - 1) <= 1u;
					if (flag && bidPriceValues.TryGetValue(row.Price, out var value))
					{
						fontFamily = ((SuperDomColumn)this).SuperDom.Font.Family;
						typeFace = new Typeface(fontFamily, ((SuperDomColumn)this).SuperDom.Font.Italic ? FontStyles.Italic : FontStyles.Normal, ((SuperDomColumn)this).SuperDom.Font.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
						if (renderWidth - 6.0 > 0.0)
						{
							FormattedText formattedText = new FormattedText((value > 0.0) ? value.ToString(Globals.GeneralOptions.CurrentCulture) : string.Empty, Globals.GeneralOptions.CurrentCulture, FlowDirection.LeftToRight, typeFace, ((SuperDomColumn)this).SuperDom.Font.Size, BidForeColor, pixelsPerDip)
							{
								MaxLineCount = 1,
								MaxTextWidth = renderWidth / 2.0 - 6.0,
								Trimming = TextTrimming.CharacterEllipsis
							};
							if (heightUpdateNeeded)
							{
								textHeight = formattedText.Height;
								heightUpdateNeeded = false;
							}
							dc.DrawText(formattedText, new Point(4.0, num2 + (((SuperDomColumn)this).SuperDom.ActualRowHeight - textHeight) / 2.0));
						}
					}
					displayType = DisplayType;
					flag = ((displayType == RecentDisplayType.Ask || displayType == RecentDisplayType.BidAsk) ? true : false);
					if (flag && askPriceValues.TryGetValue(row.Price, out var value2))
					{
						fontFamily = ((SuperDomColumn)this).SuperDom.Font.Family;
						typeFace = new Typeface(fontFamily, ((SuperDomColumn)this).SuperDom.Font.Italic ? FontStyles.Italic : FontStyles.Normal, ((SuperDomColumn)this).SuperDom.Font.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
						if (renderWidth - 6.0 > 0.0)
						{
							FormattedText formattedText2 = new FormattedText((value2 > 0.0) ? value2.ToString(Globals.GeneralOptions.CurrentCulture) : string.Empty, Globals.GeneralOptions.CurrentCulture, FlowDirection.LeftToRight, typeFace, ((SuperDomColumn)this).SuperDom.Font.Size, AskForeColor, pixelsPerDip)
							{
								MaxLineCount = 1,
								MaxTextWidth = renderWidth / 2.0 - 6.0,
								Trimming = TextTrimming.CharacterEllipsis
							};
							if (heightUpdateNeeded)
							{
								textHeight = formattedText2.Height;
								heightUpdateNeeded = false;
							}
							dc.DrawText(formattedText2, new Point(renderWidth / 2.0 + 4.0, num2 + (((SuperDomColumn)this).SuperDom.ActualRowHeight - textHeight) / 2.0));
						}
					}
				}
				dc.Pop();
				num2 += ((SuperDomColumn)this).SuperDom.ActualRowHeight;
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Invalid comparison between Unknown and I4
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((SuperDomColumn)this).Name = Resource.NinjaScriptSuperDomColumnRecentLabel;
			((NinjaScript)this).Description = Resource.NinjaScriptSuperDomColumnRecentDescription;
			((SuperDomColumn)this).DefaultWidth = 100.0;
			((SuperDomColumn)this).PreviousWidth = -1.0;
			((SuperDomColumn)this).IsDataSeriesRequired = false;
			AskBackColor = Application.Current.TryFindResource("brushPriceColumnBackground") as Brush;
			AskForeColor = Application.Current.TryFindResource("brushVolumeColumnForeground") as SolidColorBrush;
			BidBackColor = Application.Current.TryFindResource("brushPriceColumnBackground") as Brush;
			BidForeColor = Application.Current.TryFindResource("brushVolumeColumnForeground") as SolidColorBrush;
			askPriceValues = new ConcurrentDictionary<double, double>();
			bidPriceValues = new ConcurrentDictionary<double, double>();
			DisplayType = RecentDisplayType.BidAsk;
			resetAskTimers = new ConcurrentDictionary<double, Timer>();
			resetBidTimers = new ConcurrentDictionary<double, Timer>();
			ResetWhen = RecentResetWhen.BidAskChange;
			ResetTolerance = 2500;
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
		}
		else
		{
			if ((int)((NinjaScript)this).State != 8)
			{
				return;
			}
			foreach (KeyValuePair<double, Timer> resetAskTimer in resetAskTimers)
			{
				resetAskTimer.Value.Dispose();
			}
			resetAskTimers.Clear();
			foreach (KeyValuePair<double, Timer> resetBidTimer in resetBidTimers)
			{
				resetBidTimer.Value.Dispose();
			}
			resetBidTimers.Clear();
		}
	}

	private void ResetAsk(object price)
	{
		double key = (double)price;
		if (resetAskTimers.TryRemove(key, out var value))
		{
			value.Dispose();
		}
		askPriceValues[key] = 0.0;
		((SuperDomColumn)this).OnPropertyChanged("ResetAsk");
	}

	private void ResetBid(object price)
	{
		double key = (double)price;
		if (resetBidTimers.TryRemove(key, out var value))
		{
			value.Dispose();
		}
		bidPriceValues[key] = 0.0;
		((SuperDomColumn)this).OnPropertyChanged("ResetBid");
	}
}

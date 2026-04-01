using System;
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

public class PullingStacking : SuperDomColumn
{
	[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
	public enum PullingStackingDisplayType
	{
		Ask,
		Bid,
		BidAsk
	}

	[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
	public enum PullingStackingResetWhen
	{
		NoMoreData,
		BidAskChange
	}

	private Dictionary<double, Tuple<long, long>> askPriceDepthMap;

	private Dictionary<double, Tuple<long, long>> bidPriceDepthMap;

	private readonly object collectionSync = new object();

	private FontFamily fontFamily;

	private FontStyle fontStyle;

	private FontWeight fontWeight;

	private Pen gridPen;

	private double halfPenWidth;

	private bool heightUpdateNeeded;

	private double previousAsk = double.MinValue;

	private double previousBid = double.MinValue;

	private Timer resetTimer;

	private double textHeight;

	private Typeface typeFace;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptRecentColumnDiplay", GroupName = "NinjaScriptSetup", Order = 100)]
	public PullingStackingDisplayType DisplayType { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptRecentColumnResetWhen", GroupName = "NinjaScriptSetup", Order = 110)]
	public PullingStackingResetWhen ResetWhen { get; set; }

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
		lock (collectionSync)
		{
			askPriceDepthMap.Clear();
			bidPriceDepthMap.Clear();
			if (((SuperDomColumn)this).SuperDom.MarketDepth == null)
			{
				return;
			}
			lock (((SuperDomColumn)this).SuperDom.MarketDepth.Instrument.SyncMarketDepth)
			{
				for (int i = 0; i < ((SuperDomColumn)this).SuperDom.MarketDepth.Asks.Count; i++)
				{
					askPriceDepthMap.Add(((MarketDepthRow)((SuperDomColumn)this).SuperDom.MarketDepth.Asks[i]).Price, Tuple.Create(((MarketDepthRow)((SuperDomColumn)this).SuperDom.MarketDepth.Asks[i]).Volume, 0L));
				}
				for (int j = 0; j < ((SuperDomColumn)this).SuperDom.MarketDepth.Bids.Count; j++)
				{
					bidPriceDepthMap.Add(((MarketDepthRow)((SuperDomColumn)this).SuperDom.MarketDepth.Bids[j]).Price, Tuple.Create(((MarketDepthRow)((SuperDomColumn)this).SuperDom.MarketDepth.Bids[j]).Volume, 0L));
				}
			}
			((SuperDomColumn)this).OnPropertyChanged("OnColumnLabelClicked");
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketData)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Invalid comparison between Unknown and I4
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		if ((int)marketData.MarketDataType == 2)
		{
			lock (collectionSync)
			{
				if (askPriceDepthMap.TryGetValue(marketData.Price, out var value))
				{
					askPriceDepthMap[marketData.Price] = Tuple.Create(value.Item1 - marketData.Volume, value.Item2 - marketData.Volume);
				}
				if (bidPriceDepthMap.TryGetValue(marketData.Price, out value))
				{
					bidPriceDepthMap[marketData.Price] = Tuple.Create(value.Item1 - marketData.Volume, value.Item2 - marketData.Volume);
				}
				return;
			}
		}
		if (ResetWhen != PullingStackingResetWhen.BidAskChange)
		{
			return;
		}
		if ((int)marketData.MarketDataType == 0 && (previousAsk <= double.MinValue || MathExtentions.ApproxCompare(previousAsk, marketData.Price) != 0))
		{
			if (previousAsk > double.MinValue)
			{
				resetTimer?.Dispose();
				resetTimer = new Timer(ResetTimerCallback, Tuple.Create<double, MarketDataType>(marketData.Price, marketData.MarketDataType), ResetTolerance, -1);
			}
			previousAsk = marketData.Price;
		}
		else if ((int)marketData.MarketDataType == 1 && (previousBid <= double.MinValue || MathExtentions.ApproxCompare(previousBid, marketData.Price) != 0))
		{
			if (previousBid > double.MinValue)
			{
				resetTimer?.Dispose();
				resetTimer = new Timer(ResetTimerCallback, Tuple.Create<double, MarketDataType>(marketData.Price, marketData.MarketDataType), ResetTolerance, -1);
			}
			previousBid = marketData.Price;
		}
	}

	protected override void OnMarketDepth(MarketDepthEventArgs marketDepth)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Invalid comparison between Unknown and I4
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Invalid comparison between Unknown and I4
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Invalid comparison between Unknown and I4
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Invalid comparison between Unknown and I4
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Invalid comparison between Unknown and I4
		if (marketDepth.IsReset)
		{
			lock (collectionSync)
			{
				askPriceDepthMap.Clear();
				bidPriceDepthMap.Clear();
				((SuperDomColumn)this).OnPropertyChanged("OnMarketDepth");
				return;
			}
		}
		if (marketDepth.Position >= ((SuperDomColumn)this).SuperDom.DepthLevels)
		{
			return;
		}
		lock (collectionSync)
		{
			if ((int)marketDepth.MarketDataType == 0)
			{
				if ((int)marketDepth.Operation == 0)
				{
					askPriceDepthMap[marketDepth.Price] = Tuple.Create(marketDepth.Volume, 0L);
				}
				else if ((int)marketDepth.Operation == 1)
				{
					if (askPriceDepthMap.TryGetValue(marketDepth.Price, out var value))
					{
						askPriceDepthMap[marketDepth.Price] = Tuple.Create(value.Item1, marketDepth.Volume - value.Item1);
					}
					else
					{
						askPriceDepthMap[marketDepth.Price] = Tuple.Create(marketDepth.Volume, 0L);
					}
				}
				else if ((int)marketDepth.Operation == 2 && ResetWhen == PullingStackingResetWhen.NoMoreData)
				{
					askPriceDepthMap.Remove(marketDepth.Price);
				}
			}
			else
			{
				if ((int)marketDepth.MarketDataType != 1)
				{
					return;
				}
				if ((int)marketDepth.Operation == 0)
				{
					bidPriceDepthMap[marketDepth.Price] = Tuple.Create(marketDepth.Volume, 0L);
				}
				else if ((int)marketDepth.Operation == 1)
				{
					if (bidPriceDepthMap.TryGetValue(marketDepth.Price, out var value2))
					{
						bidPriceDepthMap[marketDepth.Price] = Tuple.Create(value2.Item1, marketDepth.Volume - value2.Item1);
					}
					else
					{
						bidPriceDepthMap[marketDepth.Price] = Tuple.Create(marketDepth.Volume, 0L);
					}
				}
				else if ((int)marketDepth.Operation == 2 && ResetWhen == PullingStackingResetWhen.NoMoreData)
				{
					bidPriceDepthMap.Remove(marketDepth.Price);
				}
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
				if (DisplayType == PullingStackingDisplayType.BidAsk)
				{
					Rect rectangle2 = new Rect(0.0 - halfPenWidth, num2, renderWidth / 2.0 - halfPenWidth, ((SuperDomColumn)this).SuperDom.ActualRowHeight);
					Rect rectangle3 = new Rect(renderWidth / 2.0 - halfPenWidth, num2, renderWidth / 2.0 - halfPenWidth, ((SuperDomColumn)this).SuperDom.ActualRowHeight);
					dc.DrawRectangle(BidBackColor, null, rectangle2);
					dc.DrawRectangle(AskBackColor, null, rectangle3);
				}
				else if (DisplayType == PullingStackingDisplayType.Ask)
				{
					dc.DrawRectangle(AskBackColor, null, rectangle);
				}
				else if (DisplayType == PullingStackingDisplayType.Bid)
				{
					dc.DrawRectangle(BidBackColor, null, rectangle);
				}
				Pen pen2 = gridPen;
				Pen pen3 = gridPen;
				dc.DrawLine(pen2, new Point((pen3 != null) ? (0.0 - pen3.Thickness) : 0.0, rectangle.Bottom), new Point(renderWidth - halfPenWidth, rectangle.Bottom));
				dc.DrawLine(gridPen, new Point(rectangle.Right, num2), new Point(rectangle.Right, rectangle.Bottom));
				if (((SuperDomColumn)this).SuperDom.IsConnected && !((SuperDomColumn)this).SuperDom.IsReloading && (int)((NinjaScript)this).State == 3)
				{
					lock (collectionSync)
					{
						if ((DisplayType == PullingStackingDisplayType.BidAsk || DisplayType == PullingStackingDisplayType.Bid) && bidPriceDepthMap.TryGetValue(row.Price, out var value) && row.BidVolume > 0)
						{
							fontFamily = ((SuperDomColumn)this).SuperDom.Font.Family;
							typeFace = new Typeface(fontFamily, ((SuperDomColumn)this).SuperDom.Font.Italic ? FontStyles.Italic : FontStyles.Normal, ((SuperDomColumn)this).SuperDom.Font.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
							if (renderWidth - 6.0 > 0.0)
							{
								FormattedText formattedText = new FormattedText(value.Item2.ToString(Globals.GeneralOptions.CurrentCulture), Globals.GeneralOptions.CurrentCulture, FlowDirection.LeftToRight, typeFace, ((SuperDomColumn)this).SuperDom.Font.Size, BidForeColor, pixelsPerDip)
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
						if ((DisplayType == PullingStackingDisplayType.BidAsk || DisplayType == PullingStackingDisplayType.Ask) && askPriceDepthMap.TryGetValue(row.Price, out var value2) && row.AskVolume > 0)
						{
							fontFamily = ((SuperDomColumn)this).SuperDom.Font.Family;
							typeFace = new Typeface(fontFamily, ((SuperDomColumn)this).SuperDom.Font.Italic ? FontStyles.Italic : FontStyles.Normal, ((SuperDomColumn)this).SuperDom.Font.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
							if (renderWidth - 6.0 > 0.0)
							{
								FormattedText formattedText2 = new FormattedText(value2.Item2.ToString(Globals.GeneralOptions.CurrentCulture), Globals.GeneralOptions.CurrentCulture, FlowDirection.LeftToRight, typeFace, ((SuperDomColumn)this).SuperDom.Font.Size, AskForeColor, pixelsPerDip)
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
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Invalid comparison between Unknown and I4
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((SuperDomColumn)this).Name = Resource.NinjaScriptSuperDomColumnPullingStackingLabel;
			((NinjaScript)this).Description = Resource.NinjaScriptSuperdomColumnPullingStackingDescription;
			((SuperDomColumn)this).DefaultWidth = 100.0;
			((SuperDomColumn)this).PreviousWidth = -1.0;
			((SuperDomColumn)this).IsDataSeriesRequired = false;
			AskBackColor = Application.Current.TryFindResource("brushPriceColumnBackground") as Brush;
			AskForeColor = Application.Current.TryFindResource("brushVolumeColumnForeground") as SolidColorBrush;
			BidBackColor = Application.Current.TryFindResource("brushPriceColumnBackground") as Brush;
			BidForeColor = Application.Current.TryFindResource("brushVolumeColumnForeground") as SolidColorBrush;
			askPriceDepthMap = new Dictionary<double, Tuple<long, long>>();
			bidPriceDepthMap = new Dictionary<double, Tuple<long, long>>();
			DisplayType = PullingStackingDisplayType.BidAsk;
			ResetWhen = PullingStackingResetWhen.BidAskChange;
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
			_ = ((NinjaScript)this).State;
			_ = 8;
		}
	}

	private void ResetTimerCallback(object state)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Invalid comparison between Unknown and I4
		lock (collectionSync)
		{
			if (((SuperDomColumn)this).SuperDom.Instrument == null || ((SuperDomColumn)this).SuperDom.Instrument.MarketData.Ask == null || ((SuperDomColumn)this).SuperDom.Instrument.MarketData.Bid == null)
			{
				askPriceDepthMap.Clear();
				bidPriceDepthMap.Clear();
				return;
			}
			Tuple<double, MarketDataType> tuple = (Tuple<double, MarketDataType>)state;
			if ((int)tuple.Item2 == 0)
			{
				if (MathExtentions.ApproxCompare(tuple.Item1, ((SuperDomColumn)this).SuperDom.Instrument.MarketData.Ask.Price) == 0)
				{
					return;
				}
			}
			else if ((int)tuple.Item2 == 1 && MathExtentions.ApproxCompare(tuple.Item1, ((SuperDomColumn)this).SuperDom.Instrument.MarketData.Bid.Price) == 0)
			{
				return;
			}
			askPriceDepthMap.Clear();
			bidPriceDepthMap.Clear();
			if (((SuperDomColumn)this).SuperDom.MarketDepth == null)
			{
				return;
			}
			lock (((SuperDomColumn)this).SuperDom.MarketDepth.Instrument.SyncMarketDepth)
			{
				for (int i = 0; i < ((SuperDomColumn)this).SuperDom.MarketDepth.Asks.Count; i++)
				{
					askPriceDepthMap[((MarketDepthRow)((SuperDomColumn)this).SuperDom.MarketDepth.Asks[i]).Price] = Tuple.Create(((MarketDepthRow)((SuperDomColumn)this).SuperDom.MarketDepth.Asks[i]).Volume, 0L);
				}
				for (int j = 0; j < ((SuperDomColumn)this).SuperDom.MarketDepth.Bids.Count; j++)
				{
					bidPriceDepthMap[((MarketDepthRow)((SuperDomColumn)this).SuperDom.MarketDepth.Bids[j]).Price] = Tuple.Create(((MarketDepthRow)((SuperDomColumn)this).SuperDom.MarketDepth.Bids[j]).Volume, 0L);
				}
			}
		}
	}
}

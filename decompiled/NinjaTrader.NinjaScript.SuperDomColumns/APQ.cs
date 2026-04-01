using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.SuperDom;

namespace NinjaTrader.NinjaScript.SuperDomColumns;

public class APQ : SuperDomColumn
{
	private bool eventsSubscribed;

	private FontFamily fontFamily;

	private Pen gridPen;

	private double halfPenWidth;

	private bool justConnected;

	private ConcurrentDictionary<double, long> priceApqValues;

	private ConcurrentDictionary<double, ConcurrentDictionary<Order, long>> priceToOrderApqMap;

	private Typeface typeFace;

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptColumnBaseBackground", GroupName = "PropertyCategoryVisual", Order = 105)]
	public Brush BackColor { get; set; }

	[Browsable(false)]
	public string BackgroundBrushSerialize
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
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptColumnBaseForeground", GroupName = "PropertyCategoryVisual", Order = 111)]
	public Brush ForeColor { get; set; }

	[Browsable(false)]
	public string ForeColorSerialize
	{
		get
		{
			return Serialize.BrushToString(ForeColor);
		}
		set
		{
			ForeColor = Serialize.StringToBrush(value);
		}
	}

	private void GetInitialOrderApq(Order order, OrderState orderState, double limitPrice)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Invalid comparison between Unknown and I4
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Invalid comparison between Unknown and I4
		if (!order.IsLimit)
		{
			return;
		}
		if ((int)((SuperDomColumn)this).SuperDom.AtmStrategySelectionMode == 0 && ((SuperDomColumn)this).SuperDom.AtmStrategy != null)
		{
			AtmStrategy val;
			lock (order.Account.Strategies)
			{
				StrategyBase obj = order.Account.Strategies.FirstOrDefault(delegate(StrategyBase s)
				{
					lock (s.Orders)
					{
						return s.Orders.FirstOrDefault((Order o1) => o1 == order) != null;
					}
				});
				val = (AtmStrategy)(object)((obj is AtmStrategy) ? obj : null);
			}
			if (((SuperDomColumn)this).SuperDom.AtmStrategy != val)
			{
				return;
			}
		}
		double price = limitPrice;
		if (!Order.IsTerminalState(orderState))
		{
			OrderAction orderAction;
			if (priceToOrderApqMap.TryGetValue(price, out var value))
			{
				if (value.TryGetValue(order, out var value2) || order.IsSimulatedStop)
				{
					return;
				}
				orderAction = order.OrderAction;
				if ((int)orderAction <= 1)
				{
					MarketDepthRow val2;
					lock (((SuperDomColumn)this).SuperDom.MarketDepth.Instrument.SyncMarketDepth)
					{
						val2 = (MarketDepthRow)(object)((SuperDomColumn)this).SuperDom.MarketDepth.Bids.FirstOrDefault((LadderRow b) => Math.Abs(((MarketDepthRow)b).Price - price) < 1E-15);
					}
					if (val2 != null)
					{
						value2 = val2.Volume + 1;
					}
					value.TryAdd(order, value2);
				}
				else
				{
					MarketDepthRow val3;
					lock (((SuperDomColumn)this).SuperDom.MarketDepth.Instrument.SyncMarketDepth)
					{
						val3 = (MarketDepthRow)(object)((SuperDomColumn)this).SuperDom.MarketDepth.Asks.FirstOrDefault((LadderRow b) => Math.Abs(((MarketDepthRow)b).Price - price) < 1E-15);
					}
					if (val3 != null)
					{
						value2 = val3.Volume + 1;
					}
					value.TryAdd(order, value2);
				}
				RemoveOrder(order, price);
				UpdateApqValuesForScreen();
				return;
			}
			long value3 = 0L;
			if (order.IsSimulatedStop)
			{
				return;
			}
			orderAction = order.OrderAction;
			if ((int)orderAction <= 1)
			{
				MarketDepthRow val4;
				lock (((SuperDomColumn)this).SuperDom.MarketDepth.Instrument.SyncMarketDepth)
				{
					val4 = (MarketDepthRow)(object)((SuperDomColumn)this).SuperDom.MarketDepth.Bids.FirstOrDefault((LadderRow b) => Math.Abs(((MarketDepthRow)b).Price - price) < 1E-15);
				}
				if (val4 != null)
				{
					value3 = val4.Volume + 1;
				}
			}
			else
			{
				MarketDepthRow val5;
				lock (((SuperDomColumn)this).SuperDom.MarketDepth.Instrument.SyncMarketDepth)
				{
					val5 = (MarketDepthRow)(object)((SuperDomColumn)this).SuperDom.MarketDepth.Asks.FirstOrDefault((LadderRow b) => Math.Abs(((MarketDepthRow)b).Price - price) < 1E-15);
				}
				if (val5 != null)
				{
					value3 = val5.Volume + 1;
				}
			}
			value = new ConcurrentDictionary<Order, long>();
			if (value.TryAdd(order, value3) && priceToOrderApqMap.TryAdd(price, value))
			{
				RemoveOrder(order, price);
				UpdateApqValuesForScreen();
			}
		}
		else if (!order.IsMarket)
		{
			RemoveOrder(order);
			UpdateApqValuesForScreen();
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketUpdate)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		if ((int)marketUpdate.MarketDataType != 2)
		{
			return;
		}
		MarketDataEventArgs mu = marketUpdate;
		if (justConnected)
		{
			if (((SuperDomColumn)this).SuperDom.Account == null)
			{
				return;
			}
			lock (((SuperDomColumn)this).SuperDom.Account.Orders)
			{
				foreach (Order order in ((SuperDomColumn)this).SuperDom.Account.Orders)
				{
					GetInitialOrderApq(order, order.OrderState, order.LimitPrice);
				}
			}
			justConnected = false;
		}
		else
		{
			if (!priceApqValues.TryGetValue(mu.Price, out var value))
			{
				return;
			}
			lock (((SuperDomColumn)this).SuperDom.Rows)
			{
				PriceRow val = ((SuperDomColumn)this).SuperDom.Rows.FirstOrDefault((PriceRow r) => Math.Abs(r.Price - mu.Price) < 1E-15);
				if (val == null)
				{
					return;
				}
				long value3;
				if (val.BuyOrders.Count > 0)
				{
					long num = value - mu.Volume;
					if (value != 0L && num < value)
					{
						priceApqValues.TryUpdate(mu.Price, (value <= 1) ? value : ((num >= 1) ? num : 1), value);
						if (priceToOrderApqMap.TryGetValue(mu.Price, out var value2))
						{
							foreach (Order key in value2.Keys)
							{
								if (num < value2[key])
								{
									value2.TryUpdate(key, num, value2[key]);
								}
							}
						}
					}
				}
				else if (val.SellOrders.Count == 0)
				{
					priceApqValues.TryRemove(mu.Price, out value3);
				}
				if (val.SellOrders.Count > 0)
				{
					long num2 = value - mu.Volume;
					if (value == 0L || num2 >= value)
					{
						return;
					}
					priceApqValues.TryUpdate(mu.Price, (value <= 1) ? value : ((num2 >= 1) ? num2 : 1), value);
					if (!priceToOrderApqMap.TryGetValue(mu.Price, out var value4))
					{
						return;
					}
					{
						foreach (Order key2 in value4.Keys)
						{
							if (num2 < value4[key2])
							{
								value4.TryUpdate(key2, num2, value4[key2]);
							}
						}
						return;
					}
				}
				if (val.BuyOrders.Count == 0)
				{
					priceApqValues.TryRemove(mu.Price, out value3);
				}
			}
		}
	}

	private void OnMarketDepthUpdate(object sender, MarketDepthEventArgs e)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (justConnected)
		{
			if (((SuperDomColumn)this).SuperDom.Account == null)
			{
				return;
			}
			lock (((SuperDomColumn)this).SuperDom.Account.Orders)
			{
				foreach (Order order in ((SuperDomColumn)this).SuperDom.Account.Orders)
				{
					GetInitialOrderApq(order, order.OrderState, order.LimitPrice);
				}
			}
			justConnected = false;
			return;
		}
		lock (((SuperDomColumn)this).SuperDom.Rows)
		{
			if (((SuperDomColumn)this).SuperDom.MarketDepth == null)
			{
				return;
			}
			long value2;
			if (((SuperDomColumn)this).SuperDom.MarketDepth.Bids != null)
			{
				foreach (LadderRow bid in ((SuperDomColumn)this).SuperDom.MarketDepth.Bids)
				{
					PriceRow val = ((SuperDomColumn)this).SuperDom.Rows.FirstOrDefault((PriceRow r) => Math.Abs(r.Price - ((MarketDepthRow)bid).Price) < 1E-15);
					if (val == null || (priceApqValues.TryGetValue(((MarketDepthRow)bid).Price, out var currentApqValue) && ((MarketDepthRow)bid).Volume >= currentApqValue - 1) || ((MarketDepthRow)bid).Volume < 1)
					{
						continue;
					}
					if (val.BuyOrders.Count((Order o) => o.IsLimit) > 0)
					{
						long num = priceApqValues.AddOrUpdate(((MarketDepthRow)bid).Price, ((MarketDepthRow)bid).Volume + 1, delegate(double _, long oldValue)
						{
							if (oldValue <= 1)
							{
								return oldValue;
							}
							return (oldValue - (currentApqValue - ((MarketDepthRow)bid).Volume) < 1) ? 1 : (oldValue - (currentApqValue - ((MarketDepthRow)bid).Volume));
						});
						if (!priceToOrderApqMap.TryGetValue(((MarketDepthRow)bid).Price, out var value))
						{
							continue;
						}
						foreach (Order key in value.Keys)
						{
							if (num < value[key])
							{
								value.TryUpdate(key, num, value[key]);
							}
						}
					}
					else
					{
						priceApqValues.TryRemove(((MarketDepthRow)bid).Price, out value2);
					}
				}
			}
			if (((SuperDomColumn)this).SuperDom.MarketDepth.Asks == null)
			{
				return;
			}
			foreach (LadderRow ask in ((SuperDomColumn)this).SuperDom.MarketDepth.Asks)
			{
				PriceRow val2 = ((SuperDomColumn)this).SuperDom.Rows.FirstOrDefault((PriceRow r) => Math.Abs(r.Price - ((MarketDepthRow)ask).Price) < 1E-15);
				if (val2 == null || (priceApqValues.TryGetValue(((MarketDepthRow)ask).Price, out var currentApqValue2) && ((MarketDepthRow)ask).Volume >= currentApqValue2 - 1) || ((MarketDepthRow)ask).Volume < 1)
				{
					continue;
				}
				if (val2.SellOrders.Count((Order o) => o.IsLimit) > 0)
				{
					long num2 = priceApqValues.AddOrUpdate(((MarketDepthRow)ask).Price, ((MarketDepthRow)ask).Volume + 1, delegate(double _, long oldValue)
					{
						if (oldValue <= 1)
						{
							return oldValue;
						}
						return (oldValue - (currentApqValue2 - ((MarketDepthRow)ask).Volume) < 1) ? 1 : (oldValue - (currentApqValue2 - ((MarketDepthRow)ask).Volume));
					});
					if (!priceToOrderApqMap.TryGetValue(((MarketDepthRow)ask).Price, out var value3))
					{
						continue;
					}
					foreach (Order key2 in value3.Keys)
					{
						if (num2 < value3[key2])
						{
							value3.TryUpdate(key2, num2, value3[key2]);
						}
					}
				}
				else
				{
					priceApqValues.TryRemove(((MarketDepthRow)ask).Price, out value2);
				}
			}
		}
	}

	protected override void OnOrderUpdate(OrderEventArgs orderUpdate)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		if (orderUpdate.Order.Instrument == ((SuperDomColumn)this).SuperDom.Instrument && orderUpdate.Order.Account == ((SuperDomColumn)this).SuperDom.Account && !justConnected)
		{
			GetInitialOrderApq(orderUpdate.Order, orderUpdate.OrderState, orderUpdate.LimitPrice);
		}
	}

	protected override void OnRender(DrawingContext dc, double renderWidth)
	{
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
				Pen pen2 = gridPen;
				Pen pen3 = gridPen;
				dc.DrawLine(pen2, new Point((pen3 != null) ? (0.0 - pen3.Thickness) : 0.0, rectangle.Bottom), new Point(renderWidth - halfPenWidth, rectangle.Bottom));
				dc.DrawLine(gridPen, new Point(rectangle.Right, num2), new Point(rectangle.Right, rectangle.Bottom));
				if (priceApqValues.TryGetValue(row.Price, out var value) && value > 0)
				{
					fontFamily = ((SuperDomColumn)this).SuperDom.Font.Family;
					typeFace = new Typeface(fontFamily, ((SuperDomColumn)this).SuperDom.Font.Italic ? FontStyles.Italic : FontStyles.Normal, ((SuperDomColumn)this).SuperDom.Font.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
					if (renderWidth - 6.0 > 0.0)
					{
						FormattedText formattedText = new FormattedText(value.ToString(Globals.GeneralOptions.CurrentCulture), Globals.GeneralOptions.CurrentCulture, FlowDirection.LeftToRight, typeFace, ((SuperDomColumn)this).SuperDom.Font.Size, ForeColor, pixelsPerDip)
						{
							MaxLineCount = 1,
							MaxTextWidth = renderWidth - 6.0,
							Trimming = TextTrimming.CharacterEllipsis
						};
						dc.DrawText(formattedText, new Point(4.0, num2 + (((SuperDomColumn)this).SuperDom.ActualRowHeight - formattedText.Height) / 2.0));
					}
				}
				dc.Pop();
				num2 += ((SuperDomColumn)this).SuperDom.ActualRowHeight;
			}
		}
	}

	private void OnSelectedAccountChanged(object sender, EventArgs e)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		ResetApqCollections();
		lock (((SuperDomColumn)this).SuperDom.Rows)
		{
			foreach (PriceRow row in ((SuperDomColumn)this).SuperDom.Rows)
			{
				foreach (Order buyOrder in row.BuyOrders)
				{
					GetInitialOrderApq(buyOrder, buyOrder.OrderState, buyOrder.LimitPrice);
				}
				foreach (Order sellOrder in row.SellOrders)
				{
					GetInitialOrderApq(sellOrder, sellOrder.OrderState, sellOrder.LimitPrice);
				}
			}
		}
	}

	private void OnSelectedAtmStrategyChanged(object sender, EventArgs e)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((SuperDomColumn)this).SuperDom.AtmStrategySelectionMode != 0)
		{
			return;
		}
		ResetApqCollections();
		lock (((SuperDomColumn)this).SuperDom.Rows)
		{
			foreach (PriceRow row in ((SuperDomColumn)this).SuperDom.Rows)
			{
				foreach (Order buyOrder in row.BuyOrders)
				{
					GetInitialOrderApq(buyOrder, buyOrder.OrderState, buyOrder.LimitPrice);
				}
				foreach (Order sellOrder in row.SellOrders)
				{
					GetInitialOrderApq(sellOrder, sellOrder.OrderState, sellOrder.LimitPrice);
				}
			}
		}
	}

	private void OnSelectedAtmStrategySelectionModeChanged(object sender, EventArgs e)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		ResetApqCollections();
		lock (((SuperDomColumn)this).SuperDom.Rows)
		{
			foreach (PriceRow row in ((SuperDomColumn)this).SuperDom.Rows)
			{
				foreach (Order buyOrder in row.BuyOrders)
				{
					GetInitialOrderApq(buyOrder, buyOrder.OrderState, buyOrder.LimitPrice);
				}
				foreach (Order sellOrder in row.SellOrders)
				{
					GetInitialOrderApq(sellOrder, sellOrder.OrderState, sellOrder.LimitPrice);
				}
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Invalid comparison between Unknown and I4
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((SuperDomColumn)this).Name = Resource.NinjaScriptSuperDomColumnApq;
			((NinjaScript)this).Description = Resource.NinjaScriptSuperDomColumnDescriptionApq;
			((SuperDomColumn)this).DefaultWidth = 100.0;
			((SuperDomColumn)this).PreviousWidth = -1.0;
			((SuperDomColumn)this).IsDataSeriesRequired = false;
			BackColor = Application.Current.TryFindResource("brushPriceColumnBackground") as Brush;
			ForeColor = Application.Current.TryFindResource("FontControlBrush") as SolidColorBrush;
			priceApqValues = new ConcurrentDictionary<double, long>();
			priceToOrderApqMap = new ConcurrentDictionary<double, ConcurrentDictionary<Order, long>>();
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
		else if ((int)((NinjaScript)this).State == 3)
		{
			if (!eventsSubscribed && ((SuperDomColumn)this).SuperDom.MarketDepth != null)
			{
				WeakEventManager<MarketDepth<LadderRow>, MarketDepthEventArgs>.AddHandler(((SuperDomColumn)this).SuperDom.MarketDepth, "Update", OnMarketDepthUpdate);
				WeakEventManager<SuperDomViewModel, EventArgs>.AddHandler(((SuperDomColumn)this).SuperDom, "SelectedAccountChanged", OnSelectedAccountChanged);
				WeakEventManager<SuperDomViewModel, EventArgs>.AddHandler(((SuperDomColumn)this).SuperDom, "SelectedAtmStrategyChanged", OnSelectedAtmStrategyChanged);
				WeakEventManager<SuperDomViewModel, EventArgs>.AddHandler(((SuperDomColumn)this).SuperDom, "SelectedAtmStrategySelectionModeChanged", OnSelectedAtmStrategySelectionModeChanged);
				eventsSubscribed = true;
			}
			justConnected = true;
		}
		else if ((int)((NinjaScript)this).State == 8 && ((SuperDomColumn)this).SuperDom != null && ((SuperDomColumn)this).SuperDom.MarketDepth != null && eventsSubscribed)
		{
			WeakEventManager<MarketDepth<LadderRow>, MarketDepthEventArgs>.RemoveHandler(((SuperDomColumn)this).SuperDom.MarketDepth, "Update", OnMarketDepthUpdate);
			WeakEventManager<SuperDomViewModel, EventArgs>.RemoveHandler(((SuperDomColumn)this).SuperDom, "SelectedAccountChanged", OnSelectedAccountChanged);
			WeakEventManager<SuperDomViewModel, EventArgs>.RemoveHandler(((SuperDomColumn)this).SuperDom, "SelectedAtmStrategyChanged", OnSelectedAtmStrategyChanged);
			WeakEventManager<SuperDomViewModel, EventArgs>.RemoveHandler(((SuperDomColumn)this).SuperDom, "SelectedAtmStrategySelectionModeChanged", OnSelectedAtmStrategySelectionModeChanged);
			eventsSubscribed = false;
		}
	}

	private void RemoveOrder(Order order, double excludePrice = double.MinValue)
	{
		try
		{
			KeyValuePair<double, ConcurrentDictionary<Order, long>>[] array = priceToOrderApqMap.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				KeyValuePair<double, ConcurrentDictionary<Order, long>> keyValuePair = array[i];
				long value;
				if (excludePrice > double.MinValue)
				{
					if (Math.Abs(keyValuePair.Key - excludePrice) > 1E-16)
					{
						keyValuePair.Value.TryRemove(order, out value);
					}
				}
				else
				{
					keyValuePair.Value.TryRemove(order, out value);
				}
			}
		}
		catch (Exception ex)
		{
			((NinjaScript)this).LogAndPrint(typeof(Resource), "SuperDomColumnException", new object[3]
			{
				((SuperDomColumn)this).Name,
				"RemoveOrder",
				ex.Message
			}, (LogLevel)3);
		}
	}

	private void ResetApqCollections()
	{
		priceApqValues.Clear();
		priceToOrderApqMap.Clear();
	}

	private void UpdateApqValuesForScreen()
	{
		try
		{
			KeyValuePair<double, ConcurrentDictionary<Order, long>>[] array = priceToOrderApqMap.ToArray();
			foreach (KeyValuePair<double, ConcurrentDictionary<Order, long>> keyValuePair in array)
			{
				double key = keyValuePair.Key;
				long value;
				if (priceToOrderApqMap.TryGetValue(key, out var ordersThisPrice))
				{
					List<Order> list = ordersThisPrice.Select((KeyValuePair<Order, long> o) => o.Key).ToList();
					if (list.Count > 0)
					{
						list.Sort((Order a, Order b) => b.Time.CompareTo(a.Time));
						Order oldest = list[list.Count - 1];
						priceApqValues.AddOrUpdate(key, ordersThisPrice[oldest], (double _, long _) => ordersThisPrice[oldest]);
					}
					else
					{
						priceApqValues.TryRemove(key, out value);
						priceToOrderApqMap.TryRemove(key, out ordersThisPrice);
					}
				}
				else
				{
					priceApqValues.TryRemove(key, out value);
				}
			}
		}
		catch (Exception ex)
		{
			((NinjaScript)this).LogAndPrint(typeof(Resource), "SuperDomColumnException", new object[3]
			{
				((SuperDomColumn)this).Name,
				"UpdateApqValuesForScreen",
				ex.Message
			}, (LogLevel)3);
		}
	}
}

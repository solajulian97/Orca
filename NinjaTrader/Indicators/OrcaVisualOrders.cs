using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators;

public class OrcaVisualOrders : Indicator
{
	private bool isDraggingTP;

	private bool isDraggingSL;

	private double currentDragPrice;

	private Rect rectTP;

	private Rect rectSL;

	private bool showTPButton;

	private bool showSLButton;

	private double activeEntryPrice;

	private int activeQuantity;

	private MarketPosition activeSide = (MarketPosition)2;

	private string activeOcoId = "";

	private ChartScale lastChartScale;

	private ChartPanel lastChartPanel;

	[NinjaScriptProperty]
	[Range(50, 2000)]
	[Display(Name = "Tag Offset From Right", GroupName = "1. Styling", Order = 0)]
	public int TagOffsetRight { get; set; }

	[NinjaScriptProperty]
	[Range(50, 2000)]
	[Display(Name = "Order Label Offset Right", GroupName = "1. Styling", Order = 1)]
	public int OrderLabelOffsetRight { get; set; }

	[XmlIgnore]
	[Display(Name = "TP Button Color", GroupName = "1. Styling", Order = 2)]
	public Brush ButtonColorTP { get; set; }

	[Browsable(false)]
	public string ButtonColorTPSerializable
	{
		get
		{
			return Serialize.BrushToString(ButtonColorTP);
		}
		set
		{
			ButtonColorTP = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(Name = "SL Button Color", GroupName = "1. Styling", Order = 1)]
	public Brush ButtonColorSL { get; set; }

	[Browsable(false)]
	public string ButtonColorSLSerializable
	{
		get
		{
			return Serialize.BrushToString(ButtonColorSL);
		}
		set
		{
			ButtonColorSL = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Invalid comparison between Unknown and I4
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Invalid comparison between Unknown and I4
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Interactive drag-to-create TP and SL directly from the chart";
			((NinjaScriptBase)this).Name = "OrcaVisualOrders";
			((NinjaScriptBase)this).Calculate = (Calculate)2;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = false;
			((IndicatorBase)this).DrawVerticalGridLines = false;
			((IndicatorBase)this).PaintPriceMarkers = false;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			ButtonColorTP = Brushes.LimeGreen;
			ButtonColorSL = Brushes.Salmon;
			TagOffsetRight = 600;
			OrderLabelOffsetRight = 600;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			isDraggingTP = false;
			isDraggingSL = false;
		}
		else if ((int)((NinjaScript)this).State == 5)
		{
			if (((IndicatorRenderBase)this).ChartControl == null)
			{
				return;
			}
			((DispatcherObject)(object)((IndicatorRenderBase)this).ChartControl).Dispatcher.InvokeAsync(delegate
			{
				try
				{
					((UIElement)(object)((IndicatorRenderBase)this).ChartControl).MouseLeftButtonDown += ChartControl_MouseLeftButtonDown;
					((UIElement)(object)((IndicatorRenderBase)this).ChartControl).MouseMove += ChartControl_MouseMove;
					((UIElement)(object)((IndicatorRenderBase)this).ChartControl).MouseLeftButtonUp += ChartControl_MouseLeftButtonUp;
				}
				catch
				{
				}
			});
		}
		else
		{
			if ((int)((NinjaScript)this).State != 8 || ((IndicatorRenderBase)this).ChartControl == null)
			{
				return;
			}
			((DispatcherObject)(object)((IndicatorRenderBase)this).ChartControl).Dispatcher.InvokeAsync(delegate
			{
				try
				{
					((UIElement)(object)((IndicatorRenderBase)this).ChartControl).MouseLeftButtonDown -= ChartControl_MouseLeftButtonDown;
					((UIElement)(object)((IndicatorRenderBase)this).ChartControl).MouseMove -= ChartControl_MouseMove;
					((UIElement)(object)((IndicatorRenderBase)this).ChartControl).MouseLeftButtonUp -= ChartControl_MouseLeftButtonUp;
				}
				catch
				{
				}
			});
		}
	}

	private Account GetActiveAccount()
	{
		if (((IndicatorRenderBase)this).ChartControl != null)
		{
			try
			{
				Account act = null;
				((DispatcherObject)(object)((IndicatorRenderBase)this).ChartControl).Dispatcher.Invoke(delegate
				{
					Window window = Window.GetWindow((DependencyObject)(object)((IndicatorRenderBase)this).ChartControl);
					Chart val = (Chart)(object)((window is Chart) ? window : null);
					if (val != null && val.ChartTrader != null)
					{
						act = val.ChartTrader.Account;
					}
				});
				if (act != null)
				{
					return act;
				}
			}
			catch
			{
			}
		}
		return Account.All.FirstOrDefault((Account a) => a.Positions.Any((Position p) => p.Instrument == ((NinjaScriptBase)this).Instrument)) ?? Account.All.FirstOrDefault((Account a) => a.Name == "Sim101");
	}

	private void ChartControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (((IndicatorRenderBase)this).ChartControl != null && lastChartPanel != null && lastChartScale != null)
		{
			Point position = e.GetPosition((IInputElement)lastChartPanel);
			double tickSize = ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize;
			if (showTPButton && rectTP.Contains(position))
			{
				isDraggingTP = true;
				currentDragPrice = Math.Round(lastChartScale.GetValueByY((float)position.Y) / tickSize) * tickSize;
				e.Handled = true;
				((IndicatorRenderBase)this).ForceRefresh();
			}
			else if (showSLButton && rectSL.Contains(position))
			{
				isDraggingSL = true;
				currentDragPrice = Math.Round(lastChartScale.GetValueByY((float)position.Y) / tickSize) * tickSize;
				e.Handled = true;
				((IndicatorRenderBase)this).ForceRefresh();
			}
		}
	}

	private void ChartControl_MouseMove(object sender, MouseEventArgs e)
	{
		if ((isDraggingTP || isDraggingSL) && lastChartPanel != null && lastChartScale != null)
		{
			Point position = e.GetPosition((IInputElement)lastChartPanel);
			double tickSize = ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize;
			currentDragPrice = Math.Round(lastChartScale.GetValueByY((float)position.Y) / tickSize) * tickSize;
			((IndicatorRenderBase)this).ForceRefresh();
		}
	}

	private void ChartControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if ((isDraggingTP || isDraggingSL) && lastChartPanel != null && lastChartScale != null)
		{
			Point position = e.GetPosition((IInputElement)lastChartPanel);
			double tickSize = ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize;
			double price = Math.Round(lastChartScale.GetValueByY((float)position.Y) / tickSize) * tickSize;
			SubmitDraggedOrder(isDraggingTP, price);
			isDraggingTP = false;
			isDraggingSL = false;
			e.Handled = true;
			((IndicatorRenderBase)this).ForceRefresh();
		}
	}

	private void SubmitDraggedOrder(bool isTP, double price)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		Account activeAccount = GetActiveAccount();
		if (activeAccount == null || activeQuantity <= 0 || (int)activeSide == 2)
		{
			return;
		}
		OrderAction val = (OrderAction)(((int)activeSide != 0) ? 1 : 2);
		if (isTP)
		{
			if (((int)activeSide == 0) ? (price > activeEntryPrice) : (price < activeEntryPrice))
			{
				activeAccount.Submit((IEnumerable<Order>)(object)new Order[1] { activeAccount.CreateOrder(((NinjaScriptBase)this).Instrument, val, (OrderType)0, (OrderEntry)1, (TimeInForce)0, activeQuantity, price, 0.0, activeOcoId, "Target", DateTime.MaxValue, (CustomOrder)null) });
			}
		}
		else if (((int)activeSide == 0) ? (price < activeEntryPrice) : (price > activeEntryPrice))
		{
			activeAccount.Submit((IEnumerable<Order>)(object)new Order[1] { activeAccount.CreateOrder(((NinjaScriptBase)this).Instrument, val, (OrderType)4, (OrderEntry)1, (TimeInForce)0, activeQuantity, 0.0, price, activeOcoId, "Stop", DateTime.MaxValue, (CustomOrder)null) });
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Invalid comparison between Unknown and I4
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Invalid comparison between Unknown and I4
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Invalid comparison between Unknown and I4
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Invalid comparison between Unknown and I4
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Invalid comparison between Unknown and I4
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Invalid comparison between Unknown and I4
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Invalid comparison between Unknown and I4
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Invalid comparison between Unknown and I4
		//IL_042f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Invalid comparison between Unknown and I4
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cb: Expected O, but got Unknown
		//IL_05d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0606: Unknown result type (might be due to invalid IL or missing references)
		//IL_060b: Unknown result type (might be due to invalid IL or missing references)
		//IL_060d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0617: Expected O, but got Unknown
		//IL_04b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04df: Expected O, but got Unknown
		//IL_069a: Unknown result type (might be due to invalid IL or missing references)
		//IL_069f: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a8: Expected O, but got Unknown
		//IL_04f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f9: Expected O, but got Unknown
		//IL_06bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c2: Expected O, but got Unknown
		//IL_04ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0501: Unknown result type (might be due to invalid IL or missing references)
		//IL_0506: Unknown result type (might be due to invalid IL or missing references)
		//IL_050d: Expected O, but got Unknown
		//IL_06dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0527: Unknown result type (might be due to invalid IL or missing references)
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		lastChartScale = chartScale;
		lastChartPanel = ((IndicatorRenderBase)this).ChartPanel;
		if (((NinjaScriptBase)this).Bars == null || ((NinjaScriptBase)this).Bars.Instrument == null)
		{
			return;
		}
		Account activeAccount = GetActiveAccount();
		if (activeAccount == null)
		{
			return;
		}
		Position val = activeAccount.Positions.FirstOrDefault((Position p) => p.Instrument == ((NinjaScriptBase)this).Instrument);
		if (val == null || (int)val.MarketPosition == 2)
		{
			showTPButton = false;
			showSLButton = false;
			return;
		}
		activeEntryPrice = val.AveragePrice;
		activeQuantity = val.Quantity;
		activeSide = val.MarketPosition;
		bool flag = false;
		bool flag2 = false;
		activeOcoId = "";
		foreach (Order order in activeAccount.Orders)
		{
			if (order.Instrument == ((NinjaScriptBase)this).Instrument && ((int)order.OrderState == 10 || (int)order.OrderState == 0))
			{
				if ((int)order.OrderType == 4 || (int)order.OrderType == 3)
				{
					flag = true;
				}
				if ((int)order.OrderType == 0)
				{
					flag2 = true;
				}
				if (!string.IsNullOrEmpty(order.Oco) && order.Oco.StartsWith("OrcaOCO_"))
				{
					activeOcoId = order.Oco;
				}
			}
		}
		if (string.IsNullOrEmpty(activeOcoId))
		{
			activeOcoId = "OrcaOCO_" + Guid.NewGuid().ToString("N");
		}
		showTPButton = !flag2;
		showSLButton = !flag;
		double num = chartScale.GetYByValue(activeEntryPrice);
		double num2 = chartControl.CanvasRight;
		double num3 = num2 - (double)TagOffsetRight;
		if (showTPButton && !isDraggingTP)
		{
			rectTP = new Rect(num3, num - 22.0, 36.0, 18.0);
			DrawButton("TP", rectTP, ButtonColorTP);
		}
		else
		{
			rectTP = Rect.Empty;
		}
		if (showSLButton && !isDraggingSL)
		{
			rectSL = new Rect(num3 + 40.0, num - 22.0, 36.0, 18.0);
			DrawButton("SL", rectSL, ButtonColorSL);
		}
		else
		{
			rectSL = Rect.Empty;
		}
		double tickSize = ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize;
		double pointValue = ((NinjaScriptBase)this).Instrument.MasterInstrument.PointValue;
		Dictionary<string, Tuple<bool, double, int>> dictionary = new Dictionary<string, Tuple<bool, double, int>>();
		foreach (Order order2 in activeAccount.Orders)
		{
			if (order2.Instrument != ((NinjaScriptBase)this).Instrument || ((int)order2.OrderState != 10 && (int)order2.OrderState != 0) || ((int)order2.OrderType != 0 && (int)order2.OrderType != 4 && (int)order2.OrderType != 3))
			{
				continue;
			}
			int num4 = order2.Quantity - order2.Filled;
			if (num4 > 0)
			{
				bool flag3 = (int)order2.OrderType == 0;
				double item = (flag3 ? order2.LimitPrice : order2.StopPrice);
				string key = (flag3 ? "L_" : "S_") + item;
				if (dictionary.ContainsKey(key))
				{
					dictionary[key] = new Tuple<bool, double, int>(flag3, item, dictionary[key].Item3 + num4);
				}
				else
				{
					dictionary[key] = new Tuple<bool, double, int>(flag3, item, num4);
				}
			}
		}
		foreach (KeyValuePair<string, Tuple<bool, double, int>> item5 in dictionary)
		{
			bool item2 = item5.Value.Item1;
			double item3 = item5.Value.Item2;
			int item4 = item5.Value.Item3;
			double num5 = chartScale.GetYByValue(item3);
			double num6 = Math.Round(Math.Abs(item3 - activeEntryPrice) / tickSize);
			double num7 = num6 * tickSize * pointValue * (double)item4;
			bool flag4 = ((int)activeSide == 0 && item3 > activeEntryPrice) || ((int)activeSide == 1 && item3 < activeEntryPrice);
			string text = string.Concat((item2 && flag4) ? "Profit: $" : "Risk: $", str2: $" ({Math.Round(num6 * tickSize, 2)} pts)", str1: num7.ToString("N2"));
			Color val2 = dxColorFromWpf((item2 && flag4) ? ButtonColorTP : ButtonColorSL);
			TextFormat val3 = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", (FontWeight)700, (FontStyle)0, 11f)
			{
				TextAlignment = (TextAlignment)0
			};
			try
			{
				TextLayout val4 = new TextLayout(Globals.DirectWriteFactory, text, val3, 300f, 20f);
				try
				{
					SolidColorBrush val5 = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, Color.op_Implicit(val2));
					try
					{
						((IndicatorRenderBase)this).RenderTarget.DrawTextLayout(new Vector2((float)num2 - (float)OrderLabelOffsetRight, (float)num5 - 18f), val4, (Brush)(object)val5);
					}
					finally
					{
						((IDisposable)val5)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val4)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val3)?.Dispose();
			}
		}
		if (!isDraggingTP && !isDraggingSL)
		{
			return;
		}
		double num8 = chartScale.GetYByValue(currentDragPrice);
		Color val6 = dxColorFromWpf(isDraggingTP ? ButtonColorTP : ButtonColorSL);
		SolidColorBrush val7 = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, Color.op_Implicit(val6));
		try
		{
			((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(0f, (float)num8), new Vector2((float)num2, (float)num8), (Brush)(object)val7, 1.5f, new StrokeStyle(((Resource)((IndicatorRenderBase)this).RenderTarget).Factory, new StrokeStyleProperties
			{
				DashStyle = (DashStyle)1
			}));
			double num9 = Math.Round(Math.Abs(currentDragPrice - activeEntryPrice) / tickSize);
			double num10 = num9 * tickSize * pointValue * (double)activeQuantity;
			double num11 = Math.Round(num9 * tickSize, 2);
			string text2 = (isDraggingTP ? "Profit: $" : "Risk: $") + num10.ToString("N2") + $" ({num11} pts)";
			TextFormat val8 = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", (FontWeight)700, (FontStyle)0, 12f)
			{
				TextAlignment = (TextAlignment)0
			};
			try
			{
				TextLayout val9 = new TextLayout(Globals.DirectWriteFactory, text2, val8, 300f, 20f);
				try
				{
					((IndicatorRenderBase)this).RenderTarget.DrawTextLayout(new Vector2((float)num2 - (float)OrderLabelOffsetRight, (float)num8 - 20f), val9, (Brush)(object)val7);
				}
				finally
				{
					((IDisposable)val9)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val8)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val7)?.Dispose();
		}
	}

	private void DrawButton(string text, Rect rect, Brush wpfColor)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Expected O, but got Unknown
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Expected O, but got Unknown
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		Color val = dxColorFromWpf(wpfColor);
		SolidColorBrush val2 = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, Color.op_Implicit(val));
		try
		{
			RectangleF rect2 = default(RectangleF);
			((RectangleF)(ref rect2))._002Ector((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
			RoundedRectangle val3 = new RoundedRectangle
			{
				Rect = rect2,
				RadiusX = 4f,
				RadiusY = 4f
			};
			((IndicatorRenderBase)this).RenderTarget.FillRoundedRectangle(val3, (Brush)(object)val2);
			TextFormat val4 = new TextFormat(Globals.DirectWriteFactory, "Segoe UI", (FontWeight)700, (FontStyle)0, 11f)
			{
				TextAlignment = (TextAlignment)2
			};
			try
			{
				TextLayout val5 = new TextLayout(Globals.DirectWriteFactory, text, val4, ((RectangleF)(ref rect2)).Width, ((RectangleF)(ref rect2)).Height);
				try
				{
					SolidColorBrush val6 = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, Color.op_Implicit(Color.White));
					try
					{
						((IndicatorRenderBase)this).RenderTarget.DrawTextLayout(new Vector2(((RectangleF)(ref rect2)).Left, ((RectangleF)(ref rect2)).Top + 2f), val5, (Brush)(object)val6);
					}
					finally
					{
						((IDisposable)val6)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val5)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val4)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
	}

	private Color dxColorFromWpf(Brush wpfBrush)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (wpfBrush is SolidColorBrush { Color: var color } solidColorBrush)
		{
			return new Color(color.R, solidColorBrush.Color.G, solidColorBrush.Color.B, solidColorBrush.Color.A);
		}
		return Color.Gray;
	}
}

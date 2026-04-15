#region Using declarations
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
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum VisualOrderAlignment
	{
		RightEdge,
		Center,
		LeftEdge,
		RightmostBar
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
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
		private MarketPosition activeSide = MarketPosition.Flat;
		private string activeOcoId = "";
		private ChartScale lastChartScale;
		private ChartPanel lastChartPanel;

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Button Opacity %", GroupName = "1. Styling (Aesthetics)", Order = 2)]
		public int ButtonOpacity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Label Opacity %", GroupName = "1. Styling (Aesthetics)", Order = 3)]
		public int LabelOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Button Text Color", GroupName = "1. Styling (Aesthetics)", Order = 4)]
		public System.Windows.Media.Brush ButtonTextColor { get; set; }

		[Browsable(false)]
		public string ButtonTextColorSerializable
		{
			get { return Serialize.BrushToString(ButtonTextColor); }
			set { ButtonTextColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Label Text Color", GroupName = "1. Styling (Aesthetics)", Order = 5)]
		public System.Windows.Media.Brush LabelTextColor { get; set; }

		[Browsable(false)]
		public string LabelTextColorSerializable
		{
			get { return Serialize.BrushToString(LabelTextColor); }
			set { LabelTextColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "TP Color", GroupName = "1. Styling (Aesthetics)", Order = 0)]
		public System.Windows.Media.Brush ButtonColorTP { get; set; }

		[Browsable(false)]
		public string ButtonColorTPSerializable
		{
			get { return Serialize.BrushToString(ButtonColorTP); }
			set { ButtonColorTP = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "SL Color", GroupName = "1. Styling (Aesthetics)", Order = 1)]
		public System.Windows.Media.Brush ButtonColorSL { get; set; }

		[Browsable(false)]
		public string ButtonColorSLSerializable
		{
			get { return Serialize.BrushToString(ButtonColorSL); }
			set { ButtonColorSL = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(-2000, 2000)]
		[Display(Name = "Drag Button Offset Right", GroupName = "2. Styling (Layout)", Order = 0)]
		public int TagOffsetRight { get; set; }

		[NinjaScriptProperty]
		[Range(-2000, 2000)]
		[Display(Name = "Trade Label Offset Right", GroupName = "2. Styling (Layout)", Order = 1)]
		public int OrderLabelOffsetRight { get; set; }

		[NinjaScriptProperty]
		[Range(-100, 100)]
		[Display(Name = "Label Vertical Offset", GroupName = "2. Styling (Layout)", Order = 2)]
		public int LabelVerticalOffset { get; set; }

		[NinjaScriptProperty]
		[Range(-500, 500)]
		[Display(Name = "Drag Button Vertical Offset", GroupName = "2. Styling (Layout)", Order = 3)]
		public int DragButtonVerticalOffset { get; set; }

		[NinjaScriptProperty]
		[Range(10, 100)]
		[Display(Name = "Drag Button Width", GroupName = "2. Styling (Layout)", Order = 4)]
		public int DragButtonWidth { get; set; }

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name = "Drag Button Gap", GroupName = "2. Styling (Layout)", Order = 5)]
		public int DragButtonGap { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Label Anchor Point", Description = "Binds the offset logic geometrically to match native anchor setups", GroupName = "2. Styling (Layout)", Order = 4)]
		public VisualOrderAlignment LabelAlignment { get; set; }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Interactive drag-to-create TP and SL directly from the chart";
				Name = "OrcaVisualOrders";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = false;
				DrawVerticalGridLines = false;
				PaintPriceMarkers = false;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				var bc = new System.Windows.Media.BrushConverter();
				ButtonColorTP = (System.Windows.Media.Brush)bc.ConvertFrom("#FF44CC44");
				ButtonColorSL = (System.Windows.Media.Brush)bc.ConvertFrom("#FFCC4444");
				ButtonTextColor = System.Windows.Media.Brushes.Black;
				LabelTextColor = System.Windows.Media.Brushes.Black;
				ButtonOpacity = 60;
				LabelOpacity = 80;
				TagOffsetRight = 600;
				OrderLabelOffsetRight = 480;
				LabelVerticalOffset = 0;
				DragButtonVerticalOffset = 0;
				DragButtonWidth = 40;
				DragButtonGap = 4;
				LabelAlignment = VisualOrderAlignment.RightEdge;
			}
			else if (State == State.DataLoaded)
			{
				isDraggingTP = false;
				isDraggingSL = false;
			}
			else if (State == State.Historical)
			{
				if (ChartControl == null) return;
				ChartControl.Dispatcher.InvokeAsync(() =>
				{
					ChartControl.MouseLeftButtonDown += ChartControl_MouseLeftButtonDown;
					ChartControl.MouseMove += ChartControl_MouseMove;
					ChartControl.MouseLeftButtonUp += ChartControl_MouseLeftButtonUp;
				});
			}
			else if (State == State.Terminated)
			{
				if (ChartControl == null) return;
				ChartControl.Dispatcher.InvokeAsync(() =>
				{
					ChartControl.MouseLeftButtonDown -= ChartControl_MouseLeftButtonDown;
					ChartControl.MouseMove -= ChartControl_MouseMove;
					ChartControl.MouseLeftButtonUp -= ChartControl_MouseLeftButtonUp;
				});
			}
		}

		private Account GetActiveAccount()
		{
			if (ChartControl != null)
			{
				Account act = null;
				ChartControl.Dispatcher.Invoke(() =>
				{
					Window window = Window.GetWindow(ChartControl);
					Chart chart = window as Chart;
					if (chart != null && chart.ChartTrader != null) act = chart.ChartTrader.Account;
				});
				if (act != null) return act;
			}
			return Account.All.FirstOrDefault(a => a.Positions.Any(p => p.Instrument == Instrument)) ?? Account.All.FirstOrDefault(a => a.Name == "Sim101");
		}

		private void ChartControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (ChartControl != null && lastChartPanel != null && lastChartScale != null)
			{
				System.Windows.Point position = e.GetPosition(lastChartPanel);
				bool isTP = showTPButton && rectTP.Contains(position);
				bool isSL = showSLButton && rectSL.Contains(position);
				
				if (isTP || isSL)
				{
					isDraggingTP = isTP; isDraggingSL = isSL;
					currentDragPrice = Math.Round(lastChartScale.GetValueByY((float)position.Y) / Instrument.MasterInstrument.TickSize) * Instrument.MasterInstrument.TickSize;
					ChartControl.CaptureMouse();
					e.Handled = true;
					ChartControl.InvalidateVisual();
				}
			}
		}

		private void ChartControl_MouseMove(object sender, MouseEventArgs e)
		{
			if ((isDraggingTP || isDraggingSL) && lastChartPanel != null && lastChartScale != null)
			{
				System.Windows.Point position = e.GetPosition(lastChartPanel);
				double tickSize = Instrument.MasterInstrument.TickSize;
				currentDragPrice = Math.Round(lastChartScale.GetValueByY((float)position.Y) / tickSize) * tickSize;
				ChartControl.InvalidateVisual();
			}
		}

		private void ChartControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if ((isDraggingTP || isDraggingSL) && lastChartPanel != null && lastChartScale != null)
			{
				SubmitDraggedOrder(isDraggingTP, currentDragPrice);
				isDraggingTP = false; isDraggingSL = false;
				ChartControl.ReleaseMouseCapture();
				e.Handled = true;
				ChartControl.InvalidateVisual();
			}
		}

		private void SubmitDraggedOrder(bool isTP, double price)
		{
			Account activeAccount = GetActiveAccount();
			if (activeAccount == null || activeQuantity <= 0 || activeSide == MarketPosition.Flat) return;
			OrderAction action = (activeSide == MarketPosition.Long) ? OrderAction.Sell : OrderAction.Buy;
			if (isTP)
			{
				if (activeSide == MarketPosition.Long ? (price > activeEntryPrice) : (price < activeEntryPrice))
					activeAccount.Submit(new[] { activeAccount.CreateOrder(Instrument, action, OrderType.Limit, OrderEntry.Manual, TimeInForce.Day, activeQuantity, price, 0.0, activeOcoId, "Target", DateTime.MaxValue, null) });
			}
			else if (activeSide == MarketPosition.Long ? (price < activeEntryPrice) : (price > activeEntryPrice))
				activeAccount.Submit(new[] { activeAccount.CreateOrder(Instrument, action, OrderType.StopMarket, OrderEntry.Manual, TimeInForce.Day, activeQuantity, 0.0, price, activeOcoId, "Stop", DateTime.MaxValue, null) });
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			lastChartScale = chartScale;
			lastChartPanel = ChartPanel;
			if (Bars == null || Instrument == null) return;
			Account activeAccount = GetActiveAccount();
			if (activeAccount == null) return;

			Position pos = activeAccount.Positions.FirstOrDefault(p => p.Instrument == Instrument);
			if (pos == null || pos.MarketPosition == MarketPosition.Flat) { showTPButton = false; showSLButton = false; return; }

			activeEntryPrice = pos.AveragePrice;
			activeQuantity = pos.Quantity;
			activeSide = pos.MarketPosition;
			bool hasStop = false, hasLimit = false;
			activeOcoId = "";

			foreach (Order order in activeAccount.Orders)
			{
				if (order.Instrument == Instrument && (order.OrderState == OrderState.Working || order.OrderState == OrderState.Accepted))
				{
					if (order.OrderType == OrderType.Limit) hasLimit = true;
					if (order.OrderType == OrderType.StopMarket || order.OrderType == OrderType.StopLimit)
					{
						// Only treat this stop as covering the SL if it is on the CLOSING side of the current position.
						// A Sell stop when Long = true SL. A Sell stop when Short = adding to position, not an SL.
						bool isClosingStop = (activeSide == MarketPosition.Long  && (order.OrderAction == OrderAction.Sell || order.OrderAction == OrderAction.SellShort))
						                  || (activeSide == MarketPosition.Short && (order.OrderAction == OrderAction.Buy  || order.OrderAction == OrderAction.BuyToCover));
						if (isClosingStop) hasStop = true;
					}
					if (!string.IsNullOrEmpty(order.Oco) && order.Oco.StartsWith("OrcaOCO_")) activeOcoId = order.Oco;
				}
			}
			if (string.IsNullOrEmpty(activeOcoId)) activeOcoId = "OrcaOCO_" + Guid.NewGuid().ToString("N");
			showTPButton = !hasLimit;
			showSLButton = !hasStop;

			double yEntry = chartScale.GetYByValue(activeEntryPrice);
			
			double rightX = chartControl.CanvasRight;
			if (LabelAlignment == VisualOrderAlignment.Center) 
				rightX = chartControl.CanvasLeft + ((chartControl.CanvasRight - chartControl.CanvasLeft) / 2.0);
			else if (LabelAlignment == VisualOrderAlignment.LeftEdge)
				rightX = chartControl.CanvasLeft;
			else if (LabelAlignment == VisualOrderAlignment.RightmostBar && ChartBars != null && ChartBars.Count > 0)
				rightX = chartControl.GetXByBarIndex(ChartBars, ChartBars.Count - 1);
				
			double buttonX = rightX - TagOffsetRight;

			if (showTPButton && !isDraggingTP) { rectTP = new Rect(buttonX, yEntry - 10.5 + DragButtonVerticalOffset, DragButtonWidth, 21.0); DrawButton("TP", rectTP, ButtonColorTP); } else rectTP = Rect.Empty;
			if (showSLButton && !isDraggingSL) { rectSL = new Rect(buttonX + DragButtonWidth + DragButtonGap, yEntry - 10.5 + DragButtonVerticalOffset, DragButtonWidth, 21.0); DrawButton("SL", rectSL, ButtonColorSL); } else rectSL = Rect.Empty;

			double tickSize = Instrument.MasterInstrument.TickSize;
			double pointValue = Instrument.MasterInstrument.PointValue;
			var groups = new Dictionary<string, Tuple<bool, double, int>>();
			// Tracks which group keys are add-to-position stops (determined by order action, not price)
			var addingStopKeys = new HashSet<string>();

			foreach (Order o in activeAccount.Orders)
			{
				if (o.Instrument != Instrument || (o.OrderState != OrderState.Working && o.OrderState != OrderState.Accepted)) continue;
				int rem = o.Quantity - o.Filled;
				if (rem > 0 && (o.OrderType == OrderType.Limit || o.OrderType == OrderType.StopMarket || o.OrderType == OrderType.StopLimit))
				{
					bool isL = o.OrderType == OrderType.Limit;
					double p = isL ? o.LimitPrice : o.StopPrice;
					string key = (isL ? "L_" : "S_") + p;
					if (groups.ContainsKey(key)) groups[key] = new Tuple<bool, double, int>(isL, p, groups[key].Item3 + rem);
					else groups[key] = new Tuple<bool, double, int>(isL, p, rem);

					// A stop is "adding to position" only if its order action is on the SAME side as the current position:
					// Sell stop when Short = scale in. Buy stop when Long = scale in.
					// Anything else (Buy stop when Short, Sell stop when Long) is a closing stop.
					if (!isL)
					{
						bool isAddingOrder = (activeSide == MarketPosition.Short && (o.OrderAction == OrderAction.Sell || o.OrderAction == OrderAction.SellShort))
						                  || (activeSide == MarketPosition.Long  && (o.OrderAction == OrderAction.Buy  || o.OrderAction == OrderAction.BuyToCover));
						if (isAddingOrder) addingStopKeys.Add(key);
					}
				}
			}

			foreach (var g in groups)
			{
				bool isLim = g.Value.Item1; double pr = g.Value.Item2; int qty = g.Value.Item3;
				float y = chartScale.GetYByValue(pr);
				double pts = Math.Round(Math.Abs(pr - activeEntryPrice), 2);
				double val = (Math.Abs(pr - activeEntryPrice) / tickSize) * tickSize * pointValue * qty;
				// isProf: true if this order is in profitable territory relative to the entry
				bool isProf = (activeSide == MarketPosition.Long && pr > activeEntryPrice) || (activeSide == MarketPosition.Short && pr < activeEntryPrice);
				// isAddingStop: determined by order action recorded during the scan — NOT price position
				bool isAddingStop = !isLim && addingStopKeys.Contains(g.Key);

				if (isAddingStop)
				{
					// Scale-in stop: neutral ADD label
					string addTxt = string.Format("ADD {0} @ {1:F2}", qty, pr);
					DrawPill(addTxt, new Vector2((float)rightX - OrderLabelOffsetRight, y + LabelVerticalOffset), ButtonColorTP);
				}
				else
				{
					// Closing order (limit TP, stop loss, or stop in profit)
					// Show PROFIT in green when in profitable territory, RISK in red when in loss territory
					double rMultiple = val / 500.0;
					string label = isProf ? "PROFIT" : "RISK";
					string txt = string.Format("{0}: ${1:N0} | {2:F2}pts | {3:F1}R", label, val, pts, rMultiple);
					DrawPill(txt, new Vector2((float)rightX - OrderLabelOffsetRight, y + LabelVerticalOffset), isProf ? ButtonColorTP : ButtonColorSL);
				}
			}

			if (isDraggingTP || isDraggingSL)
			{
				float yDrag = chartScale.GetYByValue(currentDragPrice);
				var clr = ToDxColor(isDraggingTP ? ButtonColorTP : ButtonColorSL);
				using (var br = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, clr))
				using (var stroke = new SharpDX.Direct2D1.StrokeStyle(RenderTarget.Factory, new SharpDX.Direct2D1.StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dash }))
				{
					RenderTarget.DrawLine(new Vector2(0f, yDrag), new Vector2((float)rightX, yDrag), br, 1.5f, stroke);
					double ps = Math.Round(Math.Abs(currentDragPrice - activeEntryPrice), 2);
					double v = (Math.Abs(currentDragPrice - activeEntryPrice) / tickSize) * tickSize * pointValue * activeQuantity;
					double rM = v / 500.0;
					string dTxt = string.Format("{0}: ${1:N0} | {2:F2}pts | {3:F1}R", (isDraggingTP ? "TARGET" : "STOP"), v, ps, rM);
					DrawPill(dTxt, new Vector2((float)rightX - OrderLabelOffsetRight, yDrag), (isDraggingTP ? ButtonColorTP : ButtonColorSL));
				}
			}
		}

		private void DrawButton(string text, Rect rect, System.Windows.Media.Brush wpfColor)
		{
			var clr = ToDxColor(wpfColor);
			var fillClr = new SharpDX.Color4(clr.Red, clr.Green, clr.Blue, (float)ButtonOpacity / 100f);
			var borderClr = clr;

			using (var fillBr = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, fillClr))
			using (var borderBr = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, borderClr))
			using (var textBr = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(ButtonTextColor)))
			using (var fmt = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, 11f))
			{
				var dxRect = new RectangleF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
				var rounded = new RoundedRectangle { Rect = dxRect, RadiusX = 4f, RadiusY = 4f };
				RenderTarget.FillRoundedRectangle(rounded, fillBr);
				RenderTarget.DrawRoundedRectangle(rounded, borderBr, 1.5f);
				fmt.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				using (var l = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, fmt, dxRect.Width, dxRect.Height))
				{
					RenderTarget.DrawTextLayout(new Vector2(dxRect.Left, dxRect.Top + 3.5f), l, textBr);
				}
			}
		}

		private void DrawPill(string text, Vector2 pos, System.Windows.Media.Brush wpfColor)
		{
			var clr = ToDxColor(wpfColor);
			var fillClr = new SharpDX.Color4(clr.Red, clr.Green, clr.Blue, (float)LabelOpacity / 100f);
			var borderClr = clr;

			using (var fillBr = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, fillClr))
			using (var borderBr = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, borderClr))
			using (var textBr = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(LabelTextColor)))
			using (var fmt = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, 12f))
			using (var l = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, fmt, 400f, 1000f))
			{
				float w = l.Metrics.Width + 12f;
				float h = l.Metrics.Height + 4f;
				var rect = new RectangleF(pos.X - w, pos.Y - h/2f, w, h);
				var rounded = new RoundedRectangle { Rect = rect, RadiusX = 4f, RadiusY = 4f };
				RenderTarget.FillRoundedRectangle(rounded, fillBr);
				RenderTarget.DrawRoundedRectangle(rounded, borderBr, 1.5f);
				RenderTarget.DrawTextLayout(new Vector2(rect.Left + 6f, rect.Top + 2f), l, textBr);
			}
		}

		private SharpDX.Color4 ToDxColor(System.Windows.Media.Brush b)
		{
			var scb = b as System.Windows.Media.SolidColorBrush;
			if (scb == null) return SharpDX.Color.Gray;
			return new SharpDX.Color4(scb.Color.R / 255f, scb.Color.G / 255f, scb.Color.B / 255f, scb.Color.A / 255f);
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrcaVisualOrders[] cacheOrcaVisualOrders;
		public OrcaVisualOrders OrcaVisualOrders(int buttonOpacity, int labelOpacity, int tagOffsetRight, int orderLabelOffsetRight, int labelVerticalOffset)
		{
			return OrcaVisualOrders(Input, buttonOpacity, labelOpacity, tagOffsetRight, orderLabelOffsetRight, labelVerticalOffset);
		}

		public OrcaVisualOrders OrcaVisualOrders(ISeries<double> input, int buttonOpacity, int labelOpacity, int tagOffsetRight, int orderLabelOffsetRight, int labelVerticalOffset)
		{
			if (cacheOrcaVisualOrders != null)
				for (int idx = 0; idx < cacheOrcaVisualOrders.Length; idx++)
					if (cacheOrcaVisualOrders[idx] != null && cacheOrcaVisualOrders[idx].ButtonOpacity == buttonOpacity && cacheOrcaVisualOrders[idx].LabelOpacity == labelOpacity && cacheOrcaVisualOrders[idx].TagOffsetRight == tagOffsetRight && cacheOrcaVisualOrders[idx].OrderLabelOffsetRight == orderLabelOffsetRight && cacheOrcaVisualOrders[idx].LabelVerticalOffset == labelVerticalOffset && cacheOrcaVisualOrders[idx].EqualsInput(input))
						return cacheOrcaVisualOrders[idx];
			return CacheIndicator<OrcaVisualOrders>(new OrcaVisualOrders(){ ButtonOpacity = buttonOpacity, LabelOpacity = labelOpacity, TagOffsetRight = tagOffsetRight, OrderLabelOffsetRight = orderLabelOffsetRight, LabelVerticalOffset = labelVerticalOffset }, input, ref cacheOrcaVisualOrders);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrcaVisualOrders OrcaVisualOrders(int buttonOpacity, int labelOpacity, int tagOffsetRight, int orderLabelOffsetRight, int labelVerticalOffset)
		{
			return indicator.OrcaVisualOrders(Input, buttonOpacity, labelOpacity, tagOffsetRight, orderLabelOffsetRight, labelVerticalOffset);
		}

		public Indicators.OrcaVisualOrders OrcaVisualOrders(ISeries<double> input , int buttonOpacity, int labelOpacity, int tagOffsetRight, int orderLabelOffsetRight, int labelVerticalOffset)
		{
			return indicator.OrcaVisualOrders(input, buttonOpacity, labelOpacity, tagOffsetRight, orderLabelOffsetRight, labelVerticalOffset);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrcaVisualOrders OrcaVisualOrders(int buttonOpacity, int labelOpacity, int tagOffsetRight, int orderLabelOffsetRight, int labelVerticalOffset)
		{
			return indicator.OrcaVisualOrders(Input, buttonOpacity, labelOpacity, tagOffsetRight, orderLabelOffsetRight, labelVerticalOffset);
		}

		public Indicators.OrcaVisualOrders OrcaVisualOrders(ISeries<double> input , int buttonOpacity, int labelOpacity, int tagOffsetRight, int orderLabelOffsetRight, int labelVerticalOffset)
		{
			return indicator.OrcaVisualOrders(input, buttonOpacity, labelOpacity, tagOffsetRight, orderLabelOffsetRight, labelVerticalOffset);
		}
	}
}

#endregion

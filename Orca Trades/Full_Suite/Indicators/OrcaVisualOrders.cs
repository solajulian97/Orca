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
		[Range(-2000, 2000)]
		[Display(Name = "Tag Offset From Right", GroupName = "1. Styling", Order = 0)]
		public int TagOffsetRight { get; set; }

		[NinjaScriptProperty]
		[Range(-2000, 2000)]
		[Display(Name = "Order Label Offset Right", GroupName = "1. Styling", Order = 1)]
		public int OrderLabelOffsetRight { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Label Anchor Point", Description = "Binds the offset logic geometrically to match native anchor setups", GroupName = "1. Styling", Order = 2)]
		public VisualOrderAlignment LabelAlignment { get; set; }

		[NinjaScriptProperty]
		[Range(-500, 500)]
		[Display(Name = "Drag Button Vertical Offset", GroupName = "1. Styling", Order = 3)]
		public int DragButtonVerticalOffset { get; set; }

		[NinjaScriptProperty]
		[Range(10, 100)]
		[Display(Name = "Drag Button Width", GroupName = "1. Styling", Order = 4)]
		public int DragButtonWidth { get; set; }

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name = "Drag Button Gap", GroupName = "1. Styling", Order = 5)]
		public int DragButtonGap { get; set; }

		[XmlIgnore]
		[Display(Name = "TP Button Color", GroupName = "1. Styling", Order = 2)]
		public System.Windows.Media.Brush ButtonColorTP { get; set; }

		[Browsable(false)]
		public string ButtonColorTPSerializable
		{
			get { return Serialize.BrushToString(ButtonColorTP); }
			set { ButtonColorTP = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "SL Button Color", GroupName = "1. Styling", Order = 1)]
		public System.Windows.Media.Brush ButtonColorSL { get; set; }

		[Browsable(false)]
		public string ButtonColorSLSerializable
		{
			get { return Serialize.BrushToString(ButtonColorSL); }
			set { ButtonColorSL = Serialize.StringToBrush(value); }
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Interactive drag-to-create TP and SL directly from the chart";
				Name = "OrcaVisualOrders";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = false;
				DrawVerticalGridLines = false;
				PaintPriceMarkers = false;
				ScaleJustification = ScaleJustification.Right;
				IsSuspendedWhileInactive = true;
				ButtonColorTP = System.Windows.Media.Brushes.LimeGreen;
				ButtonColorSL = System.Windows.Media.Brushes.Salmon;
				TagOffsetRight = 600;
				OrderLabelOffsetRight = 600;
				DragButtonVerticalOffset = 0;
				DragButtonWidth = 36;
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
				double tickSize = Instrument.MasterInstrument.TickSize;
				if (showTPButton && rectTP.Contains(position))
				{
					isDraggingTP = true;
					currentDragPrice = Math.Round(lastChartScale.GetValueByY((float)position.Y) / tickSize) * tickSize;
					e.Handled = true;
					ForceRefresh();
				}
				else if (showSLButton && rectSL.Contains(position))
				{
					isDraggingSL = true;
					currentDragPrice = Math.Round(lastChartScale.GetValueByY((float)position.Y) / tickSize) * tickSize;
					e.Handled = true;
					ForceRefresh();
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
				ForceRefresh();
			}
		}

		private void ChartControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if ((isDraggingTP || isDraggingSL) && lastChartPanel != null && lastChartScale != null)
			{
				System.Windows.Point position = e.GetPosition(lastChartPanel);
				double tickSize = Instrument.MasterInstrument.TickSize;
				double price = Math.Round(lastChartScale.GetValueByY((float)position.Y) / tickSize) * tickSize;
				SubmitDraggedOrder(isDraggingTP, price);
				isDraggingTP = false; isDraggingSL = false;
				e.Handled = true;
				ForceRefresh();
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
					if (order.OrderType == OrderType.StopMarket || order.OrderType == OrderType.StopLimit) hasStop = true;
					if (order.OrderType == OrderType.Limit) hasLimit = true;
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

			if (showTPButton && !isDraggingTP) { rectTP = new Rect(buttonX, yEntry - 22.0 + DragButtonVerticalOffset, DragButtonWidth, 18.0); DrawButton("TP", rectTP, ButtonColorTP); } else rectTP = Rect.Empty;
			if (showSLButton && !isDraggingSL) { rectSL = new Rect(buttonX + DragButtonWidth + DragButtonGap, yEntry - 22.0 + DragButtonVerticalOffset, DragButtonWidth, 18.0); DrawButton("SL", rectSL, ButtonColorSL); } else rectSL = Rect.Empty;

			double tickSize = Instrument.MasterInstrument.TickSize;
			double pointValue = Instrument.MasterInstrument.PointValue;
			var groups = new Dictionary<string, Tuple<bool, double, int>>();

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
				}
			}

			foreach (var g in groups)
			{
				bool isLim = g.Value.Item1; double pr = g.Value.Item2; int qty = g.Value.Item3;
				float y = chartScale.GetYByValue(pr);
				double ticks = Math.Round(Math.Abs(pr - activeEntryPrice) / tickSize);
				double val = ticks * tickSize * pointValue * qty;
				bool isProf = (activeSide == MarketPosition.Long && pr > activeEntryPrice) || (activeSide == MarketPosition.Short && pr < activeEntryPrice);
				string txt = string.Format("{0}: ${1:N2} ({2} pts)", (isLim && isProf ? "Profit" : "Risk"), val, Math.Round(ticks * tickSize, 2));

				var dxClr = ToDxColor((isLim && isProf ? ButtonColorTP : ButtonColorSL));
				using (var fmt = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, 11f))
				using (var layout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, txt, fmt, 300f, 20f))
				using (var br = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, dxClr))
				{
					fmt.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
					RenderTarget.DrawTextLayout(new Vector2((float)rightX - OrderLabelOffsetRight, y - 18f), layout, br);
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
					double ts = Math.Round(Math.Abs(currentDragPrice - activeEntryPrice) / tickSize);
					string dTxt = string.Format("{0}: ${1:N2} ({2} pts)", (isDraggingTP ? "Profit" : "Risk"), ts * tickSize * pointValue * activeQuantity, Math.Round(ts * tickSize, 2));
					using (var f = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, 12f))
					using (var l = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, dTxt, f, 300f, 20f))
					{
						f.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
						RenderTarget.DrawTextLayout(new Vector2((float)rightX - OrderLabelOffsetRight, yDrag - 20f), l, br);
					}
				}
			}
		}

		private void DrawButton(string text, Rect rect, System.Windows.Media.Brush wpfColor)
		{
			var clr = ToDxColor(wpfColor);
			using (var br = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, clr))
			using (var whiteBr = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.White))
			using (var fmt = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, 11f))
			{
				var dxRect = new RectangleF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
				var rounded = new RoundedRectangle { Rect = dxRect, RadiusX = 4f, RadiusY = 4f };
				RenderTarget.FillRoundedRectangle(rounded, br);
				fmt.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
				using (var l = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, fmt, dxRect.Width, dxRect.Height))
				{
					RenderTarget.DrawTextLayout(new Vector2(dxRect.Left, dxRect.Top + 2f), l, whiteBr);
				}
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

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
using NinjaTrader.NinjaScript.AddOns;
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
		private System.Windows.Controls.Canvas visualOverlayCanvas;

		private class VisualOverlayItem
		{
			public string Text;
			public string NativeText;
			public string Background;
			public string Foreground;
			public double Left;
			public double Top;
			public double Width;
			public double Height;
			public double NativeLeftX;
			public double RightX;
			public double MaxRightX;
			public double LineY;
			public double Opacity;
			public bool IsButton;
			public bool IsTp;
			public bool IsOrderLabel;
			public bool PlaceAboveLine;
		}

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
				ButtonColorSL = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FFFF7F7F");
				TagOffsetRight = 0;
				OrderLabelOffsetRight = 0;
				DragButtonVerticalOffset = 0;
				DragButtonWidth = 32;
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
				SetZOrder(10000);
				ChartControl.Dispatcher.InvokeAsync(() =>
				{
					EnsureVisualOverlayOnUi();
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
					if (Mouse.Captured == ChartControl || Mouse.Captured == visualOverlayCanvas)
						Mouse.Capture(null);
					RemoveVisualOverlayOnUi();
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
					Mouse.Capture(ChartControl);
					e.Handled = true;
					RefreshChartDuringDrag();
				}
				else if (showSLButton && rectSL.Contains(position))
				{
					isDraggingSL = true;
					currentDragPrice = Math.Round(lastChartScale.GetValueByY((float)position.Y) / tickSize) * tickSize;
					Mouse.Capture(ChartControl);
					e.Handled = true;
					RefreshChartDuringDrag();
				}
			}
		}

		private void ChartControl_MouseMove(object sender, MouseEventArgs e)
		{
			if ((isDraggingTP || isDraggingSL) && lastChartPanel != null && lastChartScale != null)
			{
				UpdateDragPrice(e);
				e.Handled = true;
			}
		}

		private void ChartControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (isDraggingTP || isDraggingSL)
			{
				FinishDrag(e);
			}
		}

		private void SubmitDraggedOrder(bool isTP, double price)
		{
			Account activeAccount = GetActiveAccount();
			if (activeAccount == null || activeQuantity <= 0 || activeSide == MarketPosition.Flat) return;
			OrderAction action = (activeSide == MarketPosition.Long) ? OrderAction.Sell : OrderAction.BuyToCover;
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
			if (activeAccount == null) { QueueOverlayUpdate(new List<VisualOverlayItem>()); return; }
			OrcaExecutionRouterSettings visual = OrcaExecutionRouter.GetSettings();
			System.Windows.Media.Brush tpBrush = GetRouterBrush(visual.BuyColor, ButtonColorTP ?? System.Windows.Media.Brushes.LimeGreen);
			System.Windows.Media.Brush slBrush = GetRouterBrush(visual.SellColor, ButtonColorSL ?? System.Windows.Media.Brushes.Salmon);
			var overlayItems = new List<VisualOverlayItem>();

			Position pos = activeAccount.Positions.FirstOrDefault(p => p.Instrument == Instrument);
			if (pos == null || pos.MarketPosition == MarketPosition.Flat) { showTPButton = false; showSLButton = false; QueueOverlayUpdate(overlayItems); return; }

			activeEntryPrice = pos.AveragePrice;
			activeQuantity = Math.Abs(pos.Quantity);
			activeSide = pos.MarketPosition;
			bool hasStop = false, hasLimit = false;
			activeOcoId = "";

			foreach (Order order in activeAccount.Orders)
			{
				if (order.Instrument == Instrument && (order.OrderState == OrderState.Working || order.OrderState == OrderState.Accepted))
				{
					if (!IsReducingOrder(pos, order)) continue;
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
				
			int tagOffset = NormalizeLegacyOffset(TagOffsetRight);
			int labelOffset = NormalizeLegacyOffset(OrderLabelOffsetRight);
			double labelRightX = rightX - visual.LabelRightPadding - labelOffset;
			double buttonWidth = DragButtonWidth <= 0 ? 32 : DragButtonWidth;
			double buttonX = labelRightX - (buttonWidth * 2) - DragButtonGap - 8 - tagOffset;
			double buttonY = activeSide == MarketPosition.Long ? yEntry - 18.0 + DragButtonVerticalOffset : yEntry - 18.0 + DragButtonVerticalOffset;

			if (showTPButton && !isDraggingTP)
			{
				rectTP = new Rect(buttonX, buttonY, buttonWidth, 18.0);
				overlayItems.Add(new VisualOverlayItem { IsButton = true, IsTp = true, Text = "TP", Background = visual.BuyColor, Left = rectTP.X, Top = rectTP.Y, Width = rectTP.Width, Height = rectTP.Height });
			}
			else rectTP = Rect.Empty;
			if (showSLButton && !isDraggingSL)
			{
				rectSL = new Rect(buttonX + buttonWidth + DragButtonGap, buttonY, buttonWidth, 18.0);
				overlayItems.Add(new VisualOverlayItem { IsButton = true, IsTp = false, Text = "SL", Background = visual.SellColor, Left = rectSL.X, Top = rectSL.Y, Width = rectSL.Width, Height = rectSL.Height });
			}
			else rectSL = Rect.Empty;

			double tickSize = Instrument.MasterInstrument.TickSize;
			double pointValue = Instrument.MasterInstrument.PointValue;
			var groups = new Dictionary<string, Tuple<bool, double, int>>();

			foreach (Order o in activeAccount.Orders)
			{
				if (o.Instrument != Instrument || (o.OrderState != OrderState.Working && o.OrderState != OrderState.Accepted)) continue;
				if (!IsReducingOrder(pos, o)) continue;
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
				double points = ticks * tickSize;
				double val = ticks * tickSize * pointValue * qty;
				bool isProf = (activeSide == MarketPosition.Long && pr > activeEntryPrice) || (activeSide == MarketPosition.Short && pr < activeEntryPrice);
				string action = activeSide == MarketPosition.Long ? "Sell" : "Buy";
				string type = isLim ? "LMT" : "STP";
				string txt = string.Format("{0} {1} | {2}", isProf ? "Profit" : "Risk", val.ToString("C0"), FormatPoints(points));
				string nativeText = string.Format("{0} {1} {2}", qty, action, type);
				bool labelAboveLine = pr < activeEntryPrice;
				overlayItems.Add(new VisualOverlayItem { IsOrderLabel = true, Text = txt, NativeText = nativeText, Background = isProf ? visual.BuyColor : visual.SellColor, Foreground = visual.TextColor, NativeLeftX = rightX - labelOffset, RightX = labelRightX, MaxRightX = chartControl.CanvasRight - visual.LabelRightPadding, LineY = y, PlaceAboveLine = labelAboveLine, Opacity = visual.LabelBackgroundOpacity });
			}

			if (isDraggingTP || isDraggingSL)
			{
				float yDrag = chartScale.GetYByValue(currentDragPrice);
				System.Windows.Media.Brush dragBrush = isDraggingTP ? tpBrush : slBrush;
				var clr = ToDxColor(dragBrush, 0.95);
				using (var br = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, clr))
				using (var stroke = new SharpDX.Direct2D1.StrokeStyle(RenderTarget.Factory, new SharpDX.Direct2D1.StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dash }))
				{
					RenderTarget.DrawLine(new Vector2(0f, yDrag), new Vector2((float)rightX, yDrag), br, (float)Math.Max(1.5, visual.LineThickness), stroke);
					double ts = Math.Round(Math.Abs(currentDragPrice - activeEntryPrice) / tickSize);
					double points = ts * tickSize;
					string dTxt = string.Format("{0} @ {1:F2} | {2} {3} | {4}", isDraggingTP ? "TP" : "SL", currentDragPrice, isDraggingTP ? "Profit" : "Risk", (points * pointValue * activeQuantity).ToString("C0"), FormatPoints(points));
					overlayItems.Add(new VisualOverlayItem { Text = dTxt, Background = isDraggingTP ? visual.BuyColor : visual.SellColor, Foreground = visual.TextColor, RightX = labelRightX, LineY = yDrag, PlaceAboveLine = currentDragPrice < activeEntryPrice, Opacity = visual.LabelBackgroundOpacity });
				}
			}
			QueueOverlayUpdate(overlayItems);
		}

		private void RefreshChartDuringDrag()
		{
			if (ChartControl != null) ChartControl.InvalidateVisual();
			ForceRefresh();
		}

		private void EnsureVisualOverlayOnUi()
		{
			if (visualOverlayCanvas != null || ChartControl == null) return;
			System.Windows.DependencyObject parentObj = ChartControl;
			System.Windows.Controls.Panel parentPanel = null;
			while (parentObj != null)
			{
				parentObj = System.Windows.Media.VisualTreeHelper.GetParent(parentObj);
				parentPanel = parentObj as System.Windows.Controls.Panel;
				if (parentPanel != null) break;
			}
			if (parentPanel == null) return;

			visualOverlayCanvas = new System.Windows.Controls.Canvas
			{
				ClipToBounds = true,
				IsHitTestVisible = true,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};
			System.Windows.Controls.Panel.SetZIndex(visualOverlayCanvas, 10000);
			var grid = parentPanel as System.Windows.Controls.Grid;
			if (grid != null)
			{
				System.Windows.Controls.Grid.SetRow(visualOverlayCanvas, System.Windows.Controls.Grid.GetRow(ChartControl));
				System.Windows.Controls.Grid.SetColumn(visualOverlayCanvas, System.Windows.Controls.Grid.GetColumn(ChartControl));
				System.Windows.Controls.Grid.SetRowSpan(visualOverlayCanvas, System.Windows.Controls.Grid.GetRowSpan(ChartControl));
				System.Windows.Controls.Grid.SetColumnSpan(visualOverlayCanvas, System.Windows.Controls.Grid.GetColumnSpan(ChartControl));
			}
			visualOverlayCanvas.MouseMove += VisualOverlayCanvas_MouseMove;
			visualOverlayCanvas.MouseLeftButtonUp += VisualOverlayCanvas_MouseLeftButtonUp;
			parentPanel.Children.Add(visualOverlayCanvas);
		}

		private void RemoveVisualOverlayOnUi()
		{
			if (visualOverlayCanvas == null) return;
			visualOverlayCanvas.MouseMove -= VisualOverlayCanvas_MouseMove;
			visualOverlayCanvas.MouseLeftButtonUp -= VisualOverlayCanvas_MouseLeftButtonUp;
			(System.Windows.Media.VisualTreeHelper.GetParent(visualOverlayCanvas) as System.Windows.Controls.Panel)?.Children.Remove(visualOverlayCanvas);
			visualOverlayCanvas = null;
		}

		private void QueueOverlayUpdate(List<VisualOverlayItem> items)
		{
			if (ChartControl == null) return;
			var snapshot = items == null ? new List<VisualOverlayItem>() : items.ToList();
			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				EnsureVisualOverlayOnUi();
				if (visualOverlayCanvas == null) return;
				visualOverlayCanvas.Width = ChartControl.ActualWidth;
				visualOverlayCanvas.Height = ChartControl.ActualHeight;
				visualOverlayCanvas.Children.Clear();
				OrcaExecutionRouterSettings visual = OrcaExecutionRouter.GetSettings();
				foreach (var item in snapshot.Where(i => i.IsButton)) AddOverlayButton(item, visual);
				foreach (var item in snapshot.Where(i => !i.IsButton)) AddOverlayPill(item, visual);
			});
		}

		private void AddOverlayButton(VisualOverlayItem item, OrcaExecutionRouterSettings visual)
		{
			var text = new System.Windows.Controls.TextBlock
			{
				Text = item.Text,
				Foreground = System.Windows.Media.Brushes.White,
				FontFamily = GetOverlayFont(visual),
				FontWeight = GetOverlayFontWeight(visual),
				FontSize = visual.FontSize,
				TextAlignment = System.Windows.TextAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				IsHitTestVisible = false
			};
			var button = new System.Windows.Controls.Border
			{
				Background = GetOverlayBackgroundBrush(item.Background, item.IsTp ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Salmon, visual.CoverButtonBackgroundOpacity),
				CornerRadius = new CornerRadius(4),
				Width = item.Width,
				Height = item.Height,
				Child = text,
				Cursor = Cursors.SizeNS
			};
			button.MouseLeftButtonDown += (s, e) =>
			{
				StartVisualProtectionDrag(item.IsTp);
				e.Handled = true;
			};
			visualOverlayCanvas.Children.Add(button);
			System.Windows.Controls.Canvas.SetLeft(button, item.Left);
			System.Windows.Controls.Canvas.SetTop(button, item.Top);
			System.Windows.Controls.Panel.SetZIndex(button, 10002);
		}

		private void AddOverlayPill(VisualOverlayItem item, OrcaExecutionRouterSettings visual)
		{
			var text = new System.Windows.Controls.TextBlock
			{
				Text = item.Text,
				Foreground = GetRouterBrush(item.Foreground, System.Windows.Media.Brushes.Black),
				FontFamily = GetOverlayFont(visual),
				FontWeight = GetOverlayFontWeight(visual),
				FontSize = visual.FontSize,
				VerticalAlignment = VerticalAlignment.Center,
				IsHitTestVisible = false
			};
			var pill = new System.Windows.Controls.Border
			{
				Background = GetOverlayBackgroundBrush(item.Background, System.Windows.Media.Brushes.LimeGreen, item.Opacity),
				CornerRadius = new CornerRadius(4),
				Padding = new Thickness(4, 2, 4, 2),
				Child = text,
				IsHitTestVisible = false
			};
			pill.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			double left;
			if (item.IsOrderLabel && LabelAlignment == VisualOrderAlignment.Center)
			{
				var native = new System.Windows.Controls.TextBlock { Text = item.NativeText, FontFamily = GetOverlayFont(visual), FontWeight = GetOverlayFontWeight(visual), FontSize = visual.FontSize };
				native.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				double nativeWidth = Math.Max(82, native.DesiredSize.Width + 46);
				left = item.NativeLeftX + nativeWidth + 12;
				if (item.MaxRightX > 0) left = Math.Min(left, item.MaxRightX - pill.DesiredSize.Width);
			}
			else
				left = item.RightX - pill.DesiredSize.Width;
			double top = item.PlaceAboveLine ? item.LineY - pill.DesiredSize.Height - 8 : item.LineY + 8;
			visualOverlayCanvas.Children.Add(pill);
			System.Windows.Controls.Canvas.SetLeft(pill, Math.Max(0, left));
			System.Windows.Controls.Canvas.SetTop(pill, top);
			System.Windows.Controls.Panel.SetZIndex(pill, 10003);
		}

		private void StartVisualProtectionDrag(bool isTp)
		{
			isDraggingTP = isTp;
			isDraggingSL = !isTp;
			currentDragPrice = activeEntryPrice;
			if (visualOverlayCanvas != null) Mouse.Capture(visualOverlayCanvas);
			else if (ChartControl != null) Mouse.Capture(ChartControl);
			RefreshChartDuringDrag();
		}

		private void VisualOverlayCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (!isDraggingTP && !isDraggingSL) return;
			UpdateDragPrice(e);
			e.Handled = true;
		}

		private void VisualOverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (!isDraggingTP && !isDraggingSL) return;
			FinishDrag(e);
		}

		private void UpdateDragPrice(MouseEventArgs e)
		{
			if (lastChartPanel == null || lastChartScale == null) return;
			System.Windows.Point position = e.GetPosition(lastChartPanel);
			double tickSize = Instrument.MasterInstrument.TickSize;
			currentDragPrice = Math.Round(lastChartScale.GetValueByY((float)position.Y) / tickSize) * tickSize;
			RefreshChartDuringDrag();
		}

		private void FinishDrag(MouseButtonEventArgs e)
		{
			if (lastChartPanel == null || lastChartScale == null)
			{
				isDraggingTP = false; isDraggingSL = false;
				Mouse.Capture(null);
				e.Handled = true;
				RefreshChartDuringDrag();
				return;
			}
			System.Windows.Point position = e.GetPosition(lastChartPanel);
			double tickSize = Instrument.MasterInstrument.TickSize;
			double price = Math.Round(lastChartScale.GetValueByY((float)position.Y) / tickSize) * tickSize;
			bool submitTP = isDraggingTP;
			try { SubmitDraggedOrder(submitTP, price); }
			finally
			{
				isDraggingTP = false; isDraggingSL = false;
				Mouse.Capture(null);
				e.Handled = true;
				RefreshChartDuringDrag();
			}
		}

		private System.Windows.Media.FontFamily GetOverlayFont(OrcaExecutionRouterSettings visual)
		{
			try { return new System.Windows.Media.FontFamily(string.IsNullOrWhiteSpace(visual.FontFamily) ? "Segoe UI" : visual.FontFamily); } catch { return new System.Windows.Media.FontFamily("Segoe UI"); }
		}

		private System.Windows.FontWeight GetOverlayFontWeight(OrcaExecutionRouterSettings visual)
		{
			string weight = string.IsNullOrWhiteSpace(visual.FontWeight) ? "SemiBold" : visual.FontWeight.Replace(" ", "");
			if (string.Equals(weight, "Normal", StringComparison.OrdinalIgnoreCase)) return System.Windows.FontWeights.Normal;
			if (string.Equals(weight, "Medium", StringComparison.OrdinalIgnoreCase)) return System.Windows.FontWeights.Medium;
			if (string.Equals(weight, "Bold", StringComparison.OrdinalIgnoreCase)) return System.Windows.FontWeights.Bold;
			return System.Windows.FontWeights.SemiBold;
		}

		private System.Windows.Media.Brush GetOverlayBackgroundBrush(string brushText, System.Windows.Media.Brush fallback, double opacity)
		{
			System.Windows.Media.Brush brush = GetRouterBrush(brushText, fallback);
			try
			{
				System.Windows.Media.Brush clone = brush.Clone();
				clone.Opacity = GetOpacity(opacity, 0.95);
				return clone;
			}
			catch { return brush; }
		}

		private double GetOpacity(double opacity, double fallback)
		{
			if (double.IsNaN(opacity) || double.IsInfinity(opacity)) return fallback;
			if (opacity > 1 && opacity <= 100) opacity /= 100.0;
			return Math.Max(0, Math.Min(1, opacity));
		}

		private void DrawButton(string text, Rect rect, System.Windows.Media.Brush wpfColor, OrcaExecutionRouterSettings visual)
		{
			var clr = ToDxColor(wpfColor, visual.CoverButtonBackgroundOpacity);
			using (var br = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, clr))
			using (var whiteBr = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.White))
			using (var fmt = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, GetRouterFontFamily(visual), GetRouterFontWeight(visual), SharpDX.DirectWrite.FontStyle.Normal, (float)visual.FontSize))
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

		private void DrawOrderPill(string text, string nativeText, float nativeLeftX, float rightX, float maxRightX, float lineY, bool placeAboveLine, System.Windows.Media.Brush background, System.Windows.Media.Brush foreground, OrcaExecutionRouterSettings visual, double opacity)
		{
			if (LabelAlignment != VisualOrderAlignment.Center)
			{
				DrawOffsetPill(text, rightX, lineY, placeAboveLine, background, foreground, visual, opacity);
				return;
			}

			using (var fmt = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, GetRouterFontFamily(visual), GetRouterFontWeight(visual), SharpDX.DirectWrite.FontStyle.Normal, (float)visual.FontSize))
			using (var layout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, fmt, 600f, 24f))
			using (var nativeLayout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, nativeText, fmt, 240f, 24f))
			using (var bg = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(background, opacity)))
			using (var fg = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(foreground)))
			{
				float width = Math.Max(16f, layout.Metrics.Width + 8f);
				float height = Math.Max(18f, layout.Metrics.Height + 4f);
				float nativeWidth = Math.Max(82f, nativeLayout.Metrics.Width + 46f);
				float left = nativeLeftX + nativeWidth + 12f;
				if (maxRightX > 0)
					left = Math.Min(left, maxRightX - width);
				float top = placeAboveLine ? lineY - height - 8f : lineY + 8f;
				var rect = new RectangleF(left, top, width, height);
				var rounded = new RoundedRectangle { Rect = rect, RadiusX = 4f, RadiusY = 4f };
				RenderTarget.FillRoundedRectangle(rounded, bg);
				fmt.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
				RenderTarget.DrawTextLayout(new Vector2(left + 4f, top + ((height - layout.Metrics.Height) / 2f)), layout, fg);
			}
		}

		private void DrawOffsetPill(string text, float rightX, float lineY, bool placeAboveLine, System.Windows.Media.Brush background, System.Windows.Media.Brush foreground, OrcaExecutionRouterSettings visual, double opacity)
		{
			using (var fmt = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, GetRouterFontFamily(visual), GetRouterFontWeight(visual), SharpDX.DirectWrite.FontStyle.Normal, (float)visual.FontSize))
			using (var layout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, fmt, 600f, 24f))
			using (var bg = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(background, opacity)))
			using (var fg = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(foreground)))
			{
				float width = Math.Max(16f, layout.Metrics.Width + 8f);
				float height = Math.Max(18f, layout.Metrics.Height + 4f);
				float left = rightX - width;
				float top = placeAboveLine ? lineY - height - 8f : lineY + 8f;
				var rect = new RectangleF(left, top, width, height);
				var rounded = new RoundedRectangle { Rect = rect, RadiusX = 4f, RadiusY = 4f };
				RenderTarget.FillRoundedRectangle(rounded, bg);
				fmt.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
				RenderTarget.DrawTextLayout(new Vector2(left + 4f, top + ((height - layout.Metrics.Height) / 2f)), layout, fg);
			}
		}

		private void DrawPill(string text, float rightX, float centerY, System.Windows.Media.Brush background, System.Windows.Media.Brush foreground, OrcaExecutionRouterSettings visual, double opacity)
		{
			using (var fmt = new SharpDX.DirectWrite.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, GetRouterFontFamily(visual), GetRouterFontWeight(visual), SharpDX.DirectWrite.FontStyle.Normal, (float)visual.FontSize))
			using (var layout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, fmt, 600f, 24f))
			using (var bg = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(background, opacity)))
			using (var fg = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDxColor(foreground)))
			{
				float width = Math.Max(16f, layout.Metrics.Width + 8f);
				float height = Math.Max(18f, layout.Metrics.Height + 4f);
				float left = rightX - width;
				float top = centerY - (height / 2f);
				var rect = new RectangleF(left, top, width, height);
				var rounded = new RoundedRectangle { Rect = rect, RadiusX = 4f, RadiusY = 4f };
				RenderTarget.FillRoundedRectangle(rounded, bg);
				fmt.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
				RenderTarget.DrawTextLayout(new Vector2(left + 4f, top + ((height - layout.Metrics.Height) / 2f)), layout, fg);
			}
		}

		private bool IsReducingOrder(Position pos, Order order)
		{
			if (pos == null || order == null || pos.MarketPosition == MarketPosition.Flat) return false;
			if (pos.MarketPosition == MarketPosition.Long) return order.OrderAction == OrderAction.Sell;
			return order.OrderAction == OrderAction.Buy || order.OrderAction == OrderAction.BuyToCover;
		}

		private int NormalizeLegacyOffset(int value)
		{
			return value == 600 ? 0 : value;
		}

		private System.Windows.Media.Brush GetRouterBrush(string text, System.Windows.Media.Brush fallback)
		{
			try { return (System.Windows.Media.Brush)new BrushConverter().ConvertFrom(text); } catch { return fallback; }
		}

		private string GetRouterFontFamily(OrcaExecutionRouterSettings visual)
		{
			return string.IsNullOrWhiteSpace(visual.FontFamily) ? "Segoe UI" : visual.FontFamily;
		}

		private SharpDX.DirectWrite.FontWeight GetRouterFontWeight(OrcaExecutionRouterSettings visual)
		{
			string weight = string.IsNullOrWhiteSpace(visual.FontWeight) ? "SemiBold" : visual.FontWeight.Replace(" ", "");
			if (string.Equals(weight, "Normal", StringComparison.OrdinalIgnoreCase)) return SharpDX.DirectWrite.FontWeight.Normal;
			if (string.Equals(weight, "Medium", StringComparison.OrdinalIgnoreCase)) return SharpDX.DirectWrite.FontWeight.Medium;
			if (string.Equals(weight, "Bold", StringComparison.OrdinalIgnoreCase)) return SharpDX.DirectWrite.FontWeight.Bold;
			return SharpDX.DirectWrite.FontWeight.SemiBold;
		}

		private string FormatPoints(double points)
		{
			return Math.Round(points, 2).ToString("0.##");
		}

		private SharpDX.Color4 ToDxColor(System.Windows.Media.Brush b)
		{
			return ToDxColor(b, 1.0);
		}

		private SharpDX.Color4 ToDxColor(System.Windows.Media.Brush b, double opacity)
		{
			var scb = b as System.Windows.Media.SolidColorBrush;
			if (scb == null) return SharpDX.Color.Gray;
			float alpha = (float)Math.Max(0, Math.Min(1, opacity));
			return new SharpDX.Color4(scb.Color.R / 255f, scb.Color.G / 255f, scb.Color.B / 255f, (scb.Color.A / 255f) * alpha);
		}
	}
}

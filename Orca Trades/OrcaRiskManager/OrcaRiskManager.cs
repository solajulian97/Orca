using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;
using System.Windows.Forms;

namespace Quantower.Indicators
{
    public class OrcaRiskManagerV12 : Indicator
    {
        #region Parameters
        [InputParameter("Risk Amount $", 10)]
        public double RiskAmount = 100;

        [InputParameter("Panel Position", 20, variants: new object[] {
            "Top Left", PanelPosition.TopLeft,
            "Top Right", PanelPosition.TopRight,
            "Bottom Left", PanelPosition.BottomLeft,
            "Bottom Right", PanelPosition.BottomRight,
            "Top Bar", PanelPosition.TopBar,
            "Bottom Bar", PanelPosition.BottomBar
        })]
        public PanelPosition Position = PanelPosition.TopRight;

        [InputParameter("Account Name (Optional)", 30)]
        public string AccountName = "";

        [InputParameter("Manual Tick Value (0 = Auto)", 40)]
        public double ManualTickValue = 0;

        public enum PanelPosition { TopLeft, TopRight, BottomLeft, BottomRight, TopBar, BottomBar }
        #endregion

        #region Fields
        private double entryPrice;
        private double slPrice;
        private double tpPrice;
        private double calculatedQty;
        private double rrRatio;
        private Rectangle panelRect;
        private Rectangle buyButtonRect;
        private Rectangle sellButtonRect;
        private double currentPrice;
        private bool isInitialized = false;
        private string dragTarget = null;
        private bool isBuyHovered = false;
        private bool isSellHovered = false;
        private Point lastMousePos;
        private double lastCalculatedRiskAmount; // Field to detect parameter changes
        #endregion

        public OrcaRiskManagerV12()
        {
            Name = "Orca Risk Manager V12";
            Description = "Premium Risk Management (V12)";
            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            InitializeData();
            if (CurrentChart != null)
            {
                // Always unhook first to prevent duplicate event handlers on hot-reload
                CurrentChart.MouseDown -= Chart_MouseDown;
                CurrentChart.MouseMove -= Chart_MouseMove;
                CurrentChart.MouseUp -= Chart_MouseUp;

                CurrentChart.MouseDown += Chart_MouseDown;
                CurrentChart.MouseMove += Chart_MouseMove;
                CurrentChart.MouseUp += Chart_MouseUp;
            }
        }

        private void InitializeData()
        {
            if (Symbol == null) return;
            UpdatePrice();
            if (currentPrice > 0 && !isInitialized)
            {
                entryPrice = currentPrice;
                slPrice = currentPrice - (100 * Symbol.TickSize);
                tpPrice = currentPrice + (200 * Symbol.TickSize);
                CalculateRisk();
                isInitialized = true;
            }
        }

        private void UpdatePrice()
        {
            if (Symbol == null) return;
            currentPrice = Symbol.Last;
            if (currentPrice <= 0) currentPrice = Symbol.Bid;
            if (currentPrice <= 0) currentPrice = Symbol.Ask;
            if (currentPrice <= 0 && this.Count > 0) currentPrice = this.Close();
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (!isInitialized) InitializeData();

            // Detect if user changed the RiskAmount parameter in the platform settings
            if (RiskAmount != lastCalculatedRiskAmount)
            {
                lastCalculatedRiskAmount = RiskAmount;
                CalculateRisk();
            }

            if (args.Reason == UpdateReason.NewBar || args.Reason == UpdateReason.NewTick)
            {
                UpdatePrice();
            }
        }

        private void CalculateRisk()
        {
            try
            {
                double diff = Math.Abs(entryPrice - slPrice);
                if (diff > 0 && Symbol.TickSize > 0)
                {
                    double tickValue = GetTickValue(); 
                    double slTicks = diff / Symbol.TickSize;
                    if (slTicks > 0 && tickValue > 0)
                    {
                        calculatedQty = RiskAmount / (slTicks * tickValue);
                        double step = Symbol.LotStep > 0 ? Symbol.LotStep : Symbol.MinLot;
                        calculatedQty = Math.Floor(calculatedQty / (step > 0 ? step : 1)) * step;
                        calculatedQty = Math.Max(Symbol.MinLot, Math.Min(calculatedQty, Symbol.MaxLot));
                    }
                    else calculatedQty = 0;
                    double tpDiff = Math.Abs(entryPrice - tpPrice);
                    rrRatio = diff > 0 ? (tpDiff / diff) : 0;
                }
                else calculatedQty = 0;
            }
            catch { }
        }

        private void Chart_MouseDown(object sender, EventArgs e)
        {
            try
            {
                dynamic mouse = e;
                int mouseY = (int)mouse.Y;
                int mouseX = (int)mouse.X;
                Point mousePos = new Point(mouseX, mouseY);
                var btn = (TradingPlatform.BusinessLayer.Native.NativeMouseButtons)mouse.Button;

                // Shift + Middle Click -> Teleport closest line and start drag
                if (System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Shift && 
                    btn == TradingPlatform.BusinessLayer.Native.NativeMouseButtons.Middle)
                {
                    double targetPrice = CurrentChart.MainWindow.CoordinatesConverter.GetPrice(mouseY);
                    targetPrice = Math.Round(targetPrice / Symbol.TickSize) * Symbol.TickSize;

                    double dE = Math.Abs(targetPrice - entryPrice);
                    double dS = Math.Abs(targetPrice - slPrice);
                    double dT = Math.Abs(targetPrice - tpPrice);

                    if (dE <= dS && dE <= dT) { entryPrice = targetPrice; dragTarget = "ENTRY"; }
                    else if (dS <= dE && dS <= dT) { slPrice = targetPrice; dragTarget = "SL"; }
                    else { tpPrice = targetPrice; dragTarget = "TP"; }

                    CalculateRisk();
                    // We don't return here so that mouse focus is maintained if needed
                }

                if (btn == TradingPlatform.BusinessLayer.Native.NativeMouseButtons.Left)
                {
                    if (buyButtonRect.Contains(mousePos)) { PlaceSmartOrder(Side.Buy); return; }
                    if (sellButtonRect.Contains(mousePos)) { PlaceSmartOrder(Side.Sell); return; }
                }
                
                // Check Line Drags
                int entryY = (int)CurrentChart.MainWindow.CoordinatesConverter.GetChartY(entryPrice);
                int slY = (int)CurrentChart.MainWindow.CoordinatesConverter.GetChartY(slPrice);
                int tpY = (int)CurrentChart.MainWindow.CoordinatesConverter.GetChartY(tpPrice);

                if (Math.Abs(mouseY - entryY) < 10) dragTarget = "ENTRY";
                else if (Math.Abs(mouseY - slY) < 10) dragTarget = "SL";
                else if (Math.Abs(mouseY - tpY) < 10) dragTarget = "TP";
            }
            catch { }
        }

        private void Chart_MouseMove(object sender, EventArgs e)
        {
            try
            {
                dynamic mouse = e;
                int mouseX = (int)mouse.X;
                int mouseY = (int)mouse.Y;
                lastMousePos = new Point(mouseX, mouseY);
                isBuyHovered = buyButtonRect.Contains(lastMousePos);
                isSellHovered = sellButtonRect.Contains(lastMousePos);
                if (dragTarget != null)
                {
                    double newPrice = CurrentChart.MainWindow.CoordinatesConverter.GetPrice(mouseY);
                    // Snap to tick
                    newPrice = Math.Round(newPrice / Symbol.TickSize) * Symbol.TickSize;

                    if (dragTarget == "ENTRY") entryPrice = newPrice;
                    else if (dragTarget == "SL") slPrice = newPrice;
                    else if (dragTarget == "TP") tpPrice = newPrice;

                    CalculateRisk();
                }
            }
            catch { }
        }

        private void Chart_MouseUp(object sender, EventArgs e) { dragTarget = null; }

        private void PlaceSmartOrder(Side side)
        {
            try
            {
                if (calculatedQty < Symbol.MinLot) return;
                
                // --- ACCOUNT SELECTION LOGIC V12 ---
                // 1. Try Manual Input name
                // 2. Try the ACCOUNT SELECTED IN THE CHART TOOLBAR (CurrentChart.Account)
                // 3. Fallback to FirstOrDefault
                Account account = null;
                if (!string.IsNullOrEmpty(AccountName))
                    account = Core.Accounts.FirstOrDefault(a => a.Name.Contains(AccountName));
                
                if (account == null && CurrentChart != null)
                {
                    try {
                        // Priority: Use the account selected in the Quick Trading Toolbar
                        account = CurrentChart.Account;
                    } catch { }
                }

                if (account == null)
                    account = Core.Accounts.FirstOrDefault();

                if (account == null) return;
                
                bool isLimit = (side == Side.Buy) ? (entryPrice < currentPrice) : (entryPrice > currentPrice);
                var request = new PlaceOrderRequestParameters
                {
                    Account = account, Symbol = Symbol, Side = side, Quantity = calculatedQty,
                    Price = entryPrice, TimeInForce = TimeInForce.Day, Comment = "Orca Risk Manager V12"
                };
                dynamic dynRequest = request;
                if (isLimit) { try { dynRequest.OrderTypeId = OrderType.Limit; } catch { dynRequest.OrderType = OrderType.Limit; } }
                else { try { dynRequest.OrderTypeId = OrderType.Stop; } catch { dynRequest.OrderType = OrderType.Stop; } request.TriggerPrice = entryPrice; }
                request.StopLoss = SlTpHolder.CreateSL(Math.Round(Math.Abs(entryPrice - slPrice) / Symbol.TickSize), PriceMeasurement.Offset);
                request.TakeProfit = SlTpHolder.CreateTP(Math.Round(Math.Abs(entryPrice - tpPrice) / Symbol.TickSize), PriceMeasurement.Offset);
                Core.Instance.PlaceOrder(request);
            }
            catch { }
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            // Ensure risk is always recalculated for display
            CalculateRisk();

            Graphics g = args.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            DrawVirtualLine(g, entryPrice, "ENTRY", Color.DeepSkyBlue, args.Rectangle);
            DrawVirtualLine(g, slPrice, "STOP LOSS", Color.Crimson, args.Rectangle);
            DrawVirtualLine(g, tpPrice, "TAKE PROFIT", Color.LimeGreen, args.Rectangle);
            DrawModernPanel(g, args.Rectangle);
        }

        private void DrawVirtualLine(Graphics g, double price, string label, Color color, Rectangle rect)
        {
            float y = (float)CurrentChart.MainWindow.CoordinatesConverter.GetChartY(price);
            if (y < rect.Top || y > rect.Bottom) return;
            using (Pen pen = new Pen(color, 2))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                g.DrawLine(pen, rect.Left, y, rect.Right, y);
                using (Font f = new Font("Segoe UI", 7, FontStyle.Bold))
                using (SolidBrush b = new SolidBrush(color))
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(180, 20, 20, 20)))
                {
                    string txt = $"{label}: {price:F2}";
                    SizeF size = g.MeasureString(txt, f);
                    g.FillRectangle(bg, rect.Right - size.Width - 50, y - size.Height - 2, size.Width + 4, size.Height + 2);
                    g.DrawString(txt, f, b, rect.Right - size.Width - 48, y - size.Height);
                }
            }
        }

        private void DrawModernPanel(Graphics g, Rectangle chartRect)
        {
            int width = (Position == PanelPosition.TopBar || Position == PanelPosition.BottomBar) ? 500 : 240;
            int height = (Position == PanelPosition.TopBar || Position == PanelPosition.BottomBar) ? 50 : 150;
            int padding = 10;
            int x = padding, y = padding;
            switch (Position)
            {
                case PanelPosition.TopRight: x = chartRect.Width - width - padding; break;
                case PanelPosition.BottomLeft: y = chartRect.Height - height - padding; break;
                case PanelPosition.BottomRight: x = chartRect.Width - width - padding; y = chartRect.Height - height - padding; break;
                case PanelPosition.TopBar: x = (chartRect.Width - width) / 2; y = padding + 20; break;
                case PanelPosition.BottomBar: x = (chartRect.Width - width) / 2; y = chartRect.Height - height - padding - 20; break;
            }
            panelRect = new Rectangle(x, y, width, height);
            using (var path = GetRoundedRect(panelRect, 10))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(220, 20, 22, 28)))
            using (Pen border = new Pen(Color.FromArgb(100, 255, 255, 255), 1))
            {
                g.FillPath(bg, path);
                g.DrawPath(border, path);
            }
            if (height < 60) DrawBarContent(g, x, y, width, height);
            else DrawBoxContent(g, x, y, width, height);
        }

        private void DrawBarContent(Graphics g, int x, int y, int w, int h)
        {
            using (Font fLabel = new Font("Segoe UI", 8))
            using (Font fVal = new Font("Segoe UI", 10, FontStyle.Bold))
            using (SolidBrush bWhite = new SolidBrush(Color.White))
            using (SolidBrush bGray = new SolidBrush(Color.Gray))
            using (SolidBrush bAccent = new SolidBrush(Color.DeepSkyBlue))
            {
                g.DrawString("ORCA", fVal, bAccent, x + 15, y + 15);
                int xOff = x + 80;
                g.DrawString("RISK", fLabel, bGray, xOff, y + 8);
                g.DrawString($"${RiskAmount:F0}", fVal, bWhite, xOff, y + 22); xOff += 60;
                g.DrawString("SIZE", fLabel, bGray, xOff, y + 8);
                g.DrawString($"{calculatedQty}", fVal, new SolidBrush(Color.YellowGreen), xOff, y + 22); xOff += 70;
                g.DrawString("ENTRY", fLabel, bGray, xOff, y + 8);
                g.DrawString($"{entryPrice:F2}", fVal, bWhite, xOff, y + 22); xOff += 90;
                g.DrawString("R/R", fLabel, bGray, xOff, y + 8);
                g.DrawString($"{rrRatio:F1}:1", fVal, bWhite, xOff, y + 22);
                buyButtonRect = new Rectangle(xOff + 60, y + 10, 60, 30);
                sellButtonRect = new Rectangle(xOff + 130, y + 10, 60, 30);
                DrawButton(g, buyButtonRect, "BUY", Color.FromArgb(40, 160, 60), isBuyHovered);
                DrawButton(g, sellButtonRect, "SELL", Color.FromArgb(180, 40, 40), isSellHovered);
            }
        }

        private void DrawBoxContent(Graphics g, int x, int y, int w, int h)
        {
            using (Font fHead = new Font("Segoe UI", 9, FontStyle.Bold))
            using (Font fLabel = new Font("Segoe UI", 9))
            using (Font fVal = new Font("Segoe UI", 11, FontStyle.Bold))
            using (SolidBrush bWhite = new SolidBrush(Color.White))
            using (SolidBrush bGray = new SolidBrush(Color.Gray))
            using (SolidBrush bAccent = new SolidBrush(Color.DeepSkyBlue))
            {
                g.DrawString("ORCA RISK MANAGER V12", fHead, bAccent, x + 15, y + 10);
                
                // --- DEBUG LINE (Subtle) ---
                try {
                   dynamic s = Symbol;
                   string curAcc = (CurrentChart != null && CurrentChart.Account != null) ? CurrentChart.Account.Name : "None";
                   string debug = $"TS:{Symbol.TickSize} PV:{s.PointValue} ACC:{curAcc}";
                   g.DrawString(debug, new Font("Arial", 6), Brushes.DimGray, x + 15, y + 25);
                } catch {}

                g.DrawString("Risk Amount", fLabel, bGray, x + 15, y + 40);
                g.DrawString($"${RiskAmount:F0}", fVal, bWhite, x + 15, y + 55);
                g.DrawString("Position Size", fLabel, bGray, x + 120, y + 40);
                g.DrawString($"{calculatedQty}", fVal, new SolidBrush(Color.YellowGreen), x + 120, y + 55);
                g.DrawString("R/R Ratio", fLabel, bGray, x + 15, y + 75);
                g.DrawString($"{rrRatio:F1}:1", fVal, bWhite, x + 15, y + 90);
                buyButtonRect = new Rectangle(x + 15, y + 110, (w-40)/2, 30);
                sellButtonRect = new Rectangle(x + 25 + (w-40)/2, y + 110, (w-40)/2, 30);
                DrawButton(g, buyButtonRect, "BUY", Color.FromArgb(40, 160, 60), isBuyHovered);
                DrawButton(g, sellButtonRect, "SELL", Color.FromArgb(180, 40, 40), isSellHovered);
            }
        }

        private void DrawButton(Graphics g, Rectangle r, string t, Color c, bool h)
        {
            using (var p = GetRoundedRect(r, 5))
            using (SolidBrush b = new SolidBrush(h ? Color.FromArgb(Math.Min(255, c.R + 40), Math.Min(255, c.G + 40), Math.Min(255, c.B + 40)) : c))
            {
                g.FillPath(b, p);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(t, new Font("Segoe UI", 9, FontStyle.Bold), Brushes.White, r, sf);
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(Rectangle b, int r)
        {
            var p = new System.Drawing.Drawing2D.GraphicsPath();
            int d = r * 2;
            p.AddArc(b.X, b.Y, d, d, 180, 90);
            p.AddArc(b.Right - d, b.Y, d, d, 270, 90);
            p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            p.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private double GetTickValue()
        {
            if (Symbol == null) return 1.0;
            if (ManualTickValue > 0) return ManualTickValue;
            
            string sym = Symbol.Name.ToUpper();
            string desc = Symbol.Description != null ? Symbol.Description.ToUpper() : "";

            // --- PRIORITY 1: KNOWN MICRO INSTRUMENTS ---
            // Micro Nasdaq (MNQ) is ALWAYS 0.50 per 0.25 tick.
            if (sym.Contains("MNQ") || desc.Contains("MNQ") || desc.Contains("MICRO NASDAQ")) return 0.5;
            
            // Micro S&P (MES) is ALWAYS 1.25 per 0.25 tick.
            if (sym.Contains("MES") || desc.Contains("MES") || desc.Contains("MICRO S&P")) return 1.25;

            // Micro Gold (MGC) is ALWAYS 1.0 per 0.1 tick
            if (sym.Contains("MGC") || desc.Contains("MICRO GOLD")) return 1.0;

            // --- PRIORITY 2: CALCULATION ---
            // Theoretically: PointValue (value of 1.0 price move) * TickSize (minimum price move)
            dynamic s = Symbol;
            try 
            {
                double pv = (double)s.PointValue;
                double ts = (double)Symbol.TickSize;
                if (pv > 0 && ts > 0) return pv * ts;

                // --- PRIORITY 3: BROKER PROPERTIES (Often unreliable for Micros) ---
                if (s.StepPrice > 0) return (double)s.StepPrice;
                if (s.TickValue > 0) return (double)s.TickValue;
                if (s.TickPrice > 0) return (double)s.TickPrice;
            }
            catch { }
            
            return 1.0;
        }
    }
}

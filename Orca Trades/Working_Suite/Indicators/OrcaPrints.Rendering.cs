#region Using declarations
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Input;

using NinjaTrader.Gui.Chart;

using DxEllipse = SharpDX.Direct2D1.Ellipse;
using DxSolidColorBrush = SharpDX.Direct2D1.SolidColorBrush;
using DxTextFormat = SharpDX.DirectWrite.TextFormat;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class OrcaPrints
	{
		private Dictionary<int, DxSolidColorBrush> dxBrushCache;
		private DxTextFormat tooltipTextFormat;
		private ChartPanel lastChartPanel;
		private System.Windows.Point lastMousePoint;
		private bool hasMousePoint;

		private struct OrcaPrintRenderItem
		{
			public PrintEvent Event;
			public float X;

			public OrcaPrintRenderItem(PrintEvent printEvent, float x)
			{
				Event = printEvent;
				X = x;
			}
		}

		private void InitializeOrcaPrintsRendering()
		{
			dxBrushCache = new Dictionary<int, DxSolidColorBrush>();
			hasMousePoint = false;
		}

		private void AttachOrcaPrintsMouseHandlers()
		{
			if (ChartControl == null)
				return;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				if (ChartControl == null)
					return;

				ChartControl.MouseMove -= ChartControl_MouseMove;
				ChartControl.MouseLeave -= ChartControl_MouseLeave;
				ChartControl.MouseMove += ChartControl_MouseMove;
				ChartControl.MouseLeave += ChartControl_MouseLeave;
			});
		}

		private void DetachOrcaPrintsMouseHandlers()
		{
			if (ChartControl == null)
				return;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				if (ChartControl == null)
					return;

				ChartControl.MouseMove -= ChartControl_MouseMove;
				ChartControl.MouseLeave -= ChartControl_MouseLeave;
			});
		}

		private void ChartControl_MouseMove(object sender, MouseEventArgs e)
		{
			if (lastChartPanel == null || ChartControl == null)
				return;

			System.Windows.Point position = e.GetPosition(lastChartPanel);
			lastMousePoint = position;
			hasMousePoint = true;
			ChartControl.InvalidateVisual();
		}

		private void ChartControl_MouseLeave(object sender, MouseEventArgs e)
		{
			hasMousePoint = false;
			if (ChartControl != null)
				ChartControl.InvalidateVisual();
		}

		public override void OnRenderTargetChanged()
		{
			DisposeDxBrushCache();
			base.OnRenderTargetChanged();
		}

		private void DisposeDxBrushCache()
		{
			if (dxBrushCache != null)
			{
				foreach (KeyValuePair<int, DxSolidColorBrush> kvp in dxBrushCache)
				{
					if (kvp.Value != null)
						kvp.Value.Dispose();
				}
				dxBrushCache.Clear();
			}

			if (tooltipTextFormat != null)
			{
				tooltipTextFormat.Dispose();
				tooltipTextFormat = null;
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);

			if (RenderTarget == null || chartControl == null || chartScale == null || ChartPanel == null)
				return;

			lastChartPanel = ChartPanel;

			List<PrintEvent> snapshot = CopyPrintEventsSnapshot();
			if (snapshot.Count == 0)
				return;

			float left = ChartPanel.X;
			float right = ChartPanel.X + ChartPanel.W;
			float top = ChartPanel.Y;
			float bottom = ChartPanel.Y + ChartPanel.H;
			float padding = Math.Max(MaxDotSize, MinDotSize) * 1.5f;

			List<OrcaPrintRenderItem> visibleItems = new List<OrcaPrintRenderItem>();
			long maxVisibleVolume = 0;
			long minVisibleVolume = long.MaxValue;

			for (int i = 0; i < snapshot.Count; i++)
			{
				PrintEvent printEvent = snapshot[i];
				if (printEvent == null)
					continue;
				if (printEvent.Price < chartScale.MinValue || printEvent.Price > chartScale.MaxValue)
					continue;

				float x = GetPrintX(chartControl, printEvent);
				if (x < left - padding || x > right + padding)
					continue;

				visibleItems.Add(new OrcaPrintRenderItem(printEvent, x));
				if (printEvent.Volume > maxVisibleVolume)
					maxVisibleVolume = printEvent.Volume;
				if (printEvent.Volume < minVisibleVolume)
					minVisibleVolume = printEvent.Volume;
			}

			if (visibleItems.Count == 0 || maxVisibleVolume <= 0)
				return;

			if (minVisibleVolume == long.MaxValue || minVisibleVolume <= 0)
				minVisibleVolume = 1;

			PrintEvent hoveredEvent = null;
			float hoveredX = 0.0f;
			float hoveredY = 0.0f;
			float hoveredRadius = 0.0f;
			double closestHitDistance = double.MaxValue;
			bool mouseInPanel = hasMousePoint
				&& lastMousePoint.X >= left
				&& lastMousePoint.X <= right
				&& lastMousePoint.Y >= top
				&& lastMousePoint.Y <= bottom;

			for (int i = 0; i < visibleItems.Count; i++)
			{
				PrintEvent printEvent = visibleItems[i].Event;
				float x = visibleItems[i].X;
				float y = chartScale.GetYByValue(printEvent.Price);
				if (y < top - padding || y > bottom + padding)
					continue;

				double volumeRank = CalculateVisibleVolumeRank(printEvent.Volume, minVisibleVolume, maxVisibleVolume);
				float diameter = CalculateDotDiameter(printEvent.Volume, minVisibleVolume, maxVisibleVolume);
				float radius = diameter * 0.5f;
				if (radius <= 0)
					continue;

				if (mouseInPanel)
				{
					double dx = lastMousePoint.X - x;
					double dy = lastMousePoint.Y - y;
					double distance = Math.Sqrt(dx * dx + dy * dy);
					double hitRadius = Math.Max(radius + 4.0, 8.0);
					if (distance <= hitRadius && distance < closestHitDistance)
					{
						closestHitDistance = distance;
						hoveredEvent = printEvent;
						hoveredX = x;
						hoveredY = y;
						hoveredRadius = radius;
					}
				}

				int fillArgb = GetEventArgb(printEvent, volumeRank, 1.0);
				DxSolidColorBrush fillBrush = GetDxBrush(fillArgb);
				if (fillBrush == null)
					continue;

				bool drawPriceLevelSquare = ShapeMode == NinjaTrader.NinjaScript.Indicators.ShapeMode.DistinguishClusters && printEvent.IsPriceLevel;
				DxEllipse ellipse = new DxEllipse(new SharpDX.Vector2(x, y), radius, radius);
				SharpDX.RectangleF square = new SharpDX.RectangleF(x - radius, y - radius, diameter, diameter);

				if (drawPriceLevelSquare)
					RenderTarget.FillRectangle(square, fillBrush);
				else
					RenderTarget.FillEllipse(ellipse, fillBrush);

				if (BorderEnabled)
				{
					int borderArgb = GetBrushArgb(BorderColor, 1.0, GetAlphaFactor());
					DxSolidColorBrush borderBrush = GetDxBrush(borderArgb);
					if (borderBrush != null)
					{
						if (drawPriceLevelSquare)
							RenderTarget.DrawRectangle(square, borderBrush, 1.0f);
						else
							RenderTarget.DrawEllipse(ellipse, borderBrush, 1.0f);
					}
				}

				if (ShapeMode == NinjaTrader.NinjaScript.Indicators.ShapeMode.DistinguishClusters && printEvent.IsCluster)
				{
					int ringArgb = GetEventArgb(printEvent, volumeRank, 0.70);
					DxSolidColorBrush ringBrush = GetDxBrush(ringArgb);
					if (ringBrush != null)
					{
						float ringRadius = radius * 1.3f;
						DxEllipse ring = new DxEllipse(new SharpDX.Vector2(x, y), ringRadius, ringRadius);
						RenderTarget.DrawEllipse(ring, ringBrush, 1.5f);
					}
				}
			}

			if (hoveredEvent != null)
				DrawPrintTooltip(hoveredEvent, hoveredX, hoveredY, hoveredRadius, left, right, top, bottom);
		}

		private void DrawPrintTooltip(PrintEvent printEvent, float x, float y, float radius, float panelLeft, float panelRight, float panelTop, float panelBottom)
		{
			if (RenderTarget == null || printEvent == null)
				return;

			EnsureTooltipTextFormat();
			if (tooltipTextFormat == null)
				return;

			string text = BuildTooltipText(printEvent);
			int lineCount = CountTooltipLines(text);
			float width = printEvent.IsCluster || printEvent.IsPriceLevel ? 250.0f : 170.0f;
			float height = Math.Max(38.0f, 18.0f + lineCount * 15.0f);
			float tipX = x + radius + 10.0f;
			float tipY = y - height - radius - 6.0f;

			if (tipX + width > panelRight)
				tipX = x - radius - width - 10.0f;
			if (tipX < panelLeft)
				tipX = panelLeft + 4.0f;
			if (tipY < panelTop)
				tipY = y + radius + 8.0f;
			if (tipY + height > panelBottom)
				tipY = panelBottom - height - 4.0f;

			SharpDX.RectangleF backgroundRect = new SharpDX.RectangleF(tipX, tipY, width, height);
			SharpDX.RectangleF textRect = new SharpDX.RectangleF(tipX + 7.0f, tipY + 5.0f, width - 14.0f, height - 10.0f);
			DxSolidColorBrush backgroundBrush = GetDxBrush(unchecked((int)0xE61C1C1C));
			DxSolidColorBrush borderBrush = GetDxBrush(unchecked((int)0xF0FFFFFF));
			DxSolidColorBrush textBrush = GetDxBrush(unchecked((int)0xFFFFFFFF));

			if (backgroundBrush != null)
				RenderTarget.FillRectangle(backgroundRect, backgroundBrush);
			if (borderBrush != null)
				RenderTarget.DrawRectangle(backgroundRect, borderBrush, 1.0f);
			if (textBrush != null)
				RenderTarget.DrawText(text, tooltipTextFormat, textRect, textBrush);
		}

		private float GetPrintX(ChartControl chartControl, PrintEvent printEvent)
		{
			float x = chartControl.GetXByTime(printEvent.Time);

			if (HorizontalAnchor != OrcaPrintHorizontalAnchor.ExactPrintTime && Bars != null && ChartBars != null)
			{
				try
				{
					int barIndex = Bars.GetBar(printEvent.Time);
					if (barIndex >= 0 && barIndex < Bars.Count)
					{
						x = chartControl.GetXByBarIndex(ChartBars, barIndex);
						if (HorizontalAnchor == OrcaPrintHorizontalAnchor.OrcaCandleVolumeProfileLeft || HorizontalAnchor == OrcaPrintHorizontalAnchor.OrcaCandleVolumeProfileCenter)
						{
							float profileX;
							if (TryGetOrcaCandleVolumeProfileX(chartControl, barIndex, x, HorizontalAnchor == OrcaPrintHorizontalAnchor.OrcaCandleVolumeProfileCenter, out profileX))
								x = profileX;
						}
					}
				}
				catch { }
			}

			return x + HorizontalOffsetPx;
		}

		private bool TryGetOrcaCandleVolumeProfileX(ChartControl chartControl, int barIndex, float barCenterX, bool centerProfile, out float x)
		{
			x = barCenterX;
			OrcaCandleVolumeProfile profile = null;

			try
			{
				if (chartControl != null && chartControl.Indicators != null)
				{
					foreach (object indicator in chartControl.Indicators)
					{
						profile = indicator as OrcaCandleVolumeProfile;
						if (profile != null)
							break;
					}
				}
			}
			catch { profile = null; }

			if (profile == null)
				return false;

			float halfCandle = Math.Max(0.0f, profile.CandleWidthPx * 0.5f);
			float profileLeft = barCenterX + halfCandle + Math.Max(0.0f, profile.CandleProfileGapPx);
			x = profileLeft;

			if (!centerProfile)
				return true;

			float profileWidth = Math.Max(2.0f, profile.ProfileWidthPx);
			if (profile.DynamicProfileWidth && ChartBars != null)
			{
				try
				{
					float nextBarCenterX;
					if (barIndex + 1 < ChartBars.Count)
						nextBarCenterX = chartControl.GetXByBarIndex(ChartBars, barIndex + 1);
					else if (barIndex > 0)
						nextBarCenterX = barCenterX + (barCenterX - chartControl.GetXByBarIndex(ChartBars, barIndex - 1));
					else
						nextBarCenterX = barCenterX + profile.ProfileWidthPx;

					float nextCandleLeft = nextBarCenterX - halfCandle;
					float availableWidth = nextCandleLeft - profileLeft;
					profileWidth = Math.Max(2.0f, availableWidth - 1.0f);
				}
				catch { }
			}

			x = profileLeft + profileWidth * 0.5f;
			return true;
		}

		private void EnsureTooltipTextFormat()
		{
			if (tooltipTextFormat != null)
				return;

			try
			{
				tooltipTextFormat = new DxTextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", 12.0f)
				{
					TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading,
					ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Near
				};
			}
			catch
			{
				tooltipTextFormat = null;
			}
		}

		private string BuildTooltipText(PrintEvent printEvent)
		{
			string side = printEvent.Side == AggressorSide.Buy ? "Buy" : "Sell";
			PriceLevelEvent priceLevel = printEvent as PriceLevelEvent;
			if (priceLevel != null)
			{
				return "Price level accumulation" + Environment.NewLine
					+ side + " dominant  " + priceLevel.DominantPercent.ToString("0") + "%" + Environment.NewLine
					+ "S: " + priceLevel.SellVolume.ToString("N0") + "  B: " + priceLevel.BuyVolume.ToString("N0") + "  V: " + priceLevel.Volume.ToString("N0") + Environment.NewLine
					+ priceLevel.ChildCount.ToString("N0") + " prints at " + priceLevel.Price.ToString("0.00");
			}

			ClusterEvent cluster = printEvent as ClusterEvent;
			if (cluster == null)
			{
				return "Single print" + Environment.NewLine
					+ side + " " + printEvent.Volume.ToString("N0") + " contracts" + Environment.NewLine
					+ "Price " + printEvent.Price.ToString("0.00");
			}

			string confidence = ParentConfidenceMode == NinjaTrader.NinjaScript.Indicators.ParentConfidenceMode.Off
				? "off"
				: (cluster.ParentConfidenceScore * 100.0).ToString("0") + "%";

			return "Cluster print" + Environment.NewLine
				+ side + " " + cluster.TotalVolume.ToString("N0") + " contracts / " + cluster.ChildCount.ToString("N0") + " prints" + Environment.NewLine
				+ "VWAP " + cluster.VwapPrice.ToString("0.00") + "  Range " + ((cluster.MaxPrice - cluster.MinPrice) / TickSize).ToString("0.0") + " ticks" + Environment.NewLine
				+ "Parent confidence " + confidence;
		}

		private int CountTooltipLines(string text)
		{
			if (string.IsNullOrEmpty(text))
				return 0;

			int lines = 1;
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '\n')
					lines++;
			}
			return lines;
		}

		private float CalculateDotDiameter(long volume, long minVisibleVolume, long maxVisibleVolume)
		{
			float minSize = Math.Max(1, Math.Min(MinDotSize, MaxDotSize));
			float maxSize = Math.Max(minSize, Math.Max(MinDotSize, MaxDotSize));
			double ratio;

			if (DotSizeScale == NinjaTrader.NinjaScript.Indicators.DotSizeScale.Linear)
			{
				ratio = maxVisibleVolume > 0 ? (double)volume / (double)maxVisibleVolume : 0.0;
			}
			else
			{
				if (maxVisibleVolume <= minVisibleVolume || minVisibleVolume <= 0 || volume <= 0)
					ratio = 1.0;
				else
				{
					double numerator = Math.Log((double)volume / (double)minVisibleVolume);
					double denominator = Math.Log((double)maxVisibleVolume / (double)minVisibleVolume);
					ratio = denominator > 0.0000001 ? numerator / denominator : 1.0;
				}
			}

			ratio = Clamp01(ratio);
			return (float)(minSize + ratio * (maxSize - minSize));
		}

		private double CalculateVisibleVolumeRank(long volume, long minVisibleVolume, long maxVisibleVolume)
		{
			if (maxVisibleVolume <= minVisibleVolume)
				return 1.0;

			double rank = (double)(volume - minVisibleVolume) / (double)(maxVisibleVolume - minVisibleVolume);
			return Clamp01(rank);
		}

		private int GetEventArgb(PrintEvent printEvent, double volumeRank, double brightnessMultiplier)
		{
			WpfBrush buyBrush = printEvent.IsPriceLevel ? PriceLevelBuyColor : BuyAggressorColor;
			WpfBrush sellBrush = printEvent.IsPriceLevel ? PriceLevelSellColor : SellAggressorColor;
			WpfBrush brush = printEvent.Side == AggressorSide.Buy ? buyBrush : sellBrush;
			double intensity = 1.0;
			if (UseVariableIntensity)
			{
				double minIntensity = Clamp01(MinIntensityPct / 100.0);
				double rank = Clamp01(volumeRank);
				PriceLevelEvent priceLevel = printEvent as PriceLevelEvent;
				if (priceLevel != null)
					rank = Clamp01((priceLevel.DominantPercent - 50.0) / 50.0);
				intensity = minIntensity + (1.0 - minIntensity) * rank;
			}

			intensity = Clamp01(intensity * brightnessMultiplier);
			return GetBrushArgb(brush, intensity, GetAlphaFactor());
		}

		private double GetAlphaFactor()
		{
			return Clamp01((100.0 - TransparencyPct) / 100.0);
		}

		private int GetBrushArgb(WpfBrush brush, double intensity, double alphaFactor)
		{
			WpfColor color = WpfColors.White;
			WpfSolidColorBrush solid = brush as WpfSolidColorBrush;
			if (solid != null)
				color = solid.Color;

			byte a = (byte)Math.Round(color.A * Clamp01(alphaFactor));
			byte r = (byte)Math.Round(color.R * Clamp01(intensity));
			byte g = (byte)Math.Round(color.G * Clamp01(intensity));
			byte b = (byte)Math.Round(color.B * Clamp01(intensity));

			return unchecked((int)((a << 24) | (r << 16) | (g << 8) | b));
		}

		private DxSolidColorBrush GetDxBrush(int argb)
		{
			if (RenderTarget == null)
				return null;

			if (dxBrushCache == null)
				dxBrushCache = new Dictionary<int, DxSolidColorBrush>();

			DxSolidColorBrush brush;
			if (dxBrushCache.TryGetValue(argb, out brush) && brush != null)
				return brush;

			float a = ((argb >> 24) & 0xFF) / 255.0f;
			float r = ((argb >> 16) & 0xFF) / 255.0f;
			float g = ((argb >> 8) & 0xFF) / 255.0f;
			float b = (argb & 0xFF) / 255.0f;
			brush = new DxSolidColorBrush(RenderTarget, new SharpDX.Color4(r, g, b, a));
			dxBrushCache[argb] = brush;
			return brush;
		}
	}
}

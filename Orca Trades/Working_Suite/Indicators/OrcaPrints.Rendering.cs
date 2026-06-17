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
		private IntPtr dxBrushCacheRenderTarget = IntPtr.Zero;
		private Dictionary<int, DxSolidColorBrush> dxBrushCache;
		private DxTextFormat tooltipTextFormat;
		private ChartPanel lastChartPanel;
		private System.Windows.Point lastMousePoint;
		private bool hasMousePoint;
		private bool detailProfileLayoutActive;

		private struct OrcaPrintRenderItem
		{
			public PrintEvent Event;
			public int BarIndex;
			public float BaseX;
			public float X;
			public float Y;
			public float Diameter;
			public float Radius;
			public float LayoutLeft;
			public float LayoutRight;
			public bool HasLayoutRange;
			public bool LayoutFromLeft;

			public OrcaPrintRenderItem(PrintEvent printEvent, int barIndex, float x, float layoutLeft, float layoutRight, bool hasLayoutRange, bool layoutFromLeft)
			{
				Event = printEvent;
				BarIndex = barIndex;
				BaseX = x;
				X = x;
				Y = 0.0f;
				Diameter = 0.0f;
				Radius = 0.0f;
				LayoutLeft = layoutLeft;
				LayoutRight = layoutRight;
				HasLayoutRange = hasLayoutRange;
				LayoutFromLeft = layoutFromLeft;
			}
		}

		private struct VolumeRange
		{
			public long Min;
			public long Max;

			public void Include(long volume)
			{
				if (volume <= 0)
					return;

				if (Min <= 0 || volume < Min)
					Min = volume;
				if (volume > Max)
					Max = volume;
			}
		}

		private void InitializeOrcaPrintsRendering()
		{
			dxBrushCache = new Dictionary<int, DxSolidColorBrush>();
			hasMousePoint = false;
			detailProfileLayoutActive = true;
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

			System.Windows.Point position = e.GetPosition(ChartControl);
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
			dxBrushCacheRenderTarget = IntPtr.Zero;

			if (tooltipTextFormat != null)
			{
				tooltipTextFormat.Dispose();
				tooltipTextFormat = null;
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			RefreshSharedProfileRegistrationIfNeeded();

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
			float padding = GetMaxConfiguredDotSize() * 1.5f;
			bool useDetailProfileLayout = ResolveDetailProfileLayout(chartControl);
			bool useProfileLaneLayout = useDetailProfileLayout || IsProfileHorizontalAnchor();

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

				float layoutLeft;
				float layoutRight;
				int barIndex;
				bool hasLayoutRange;
				bool layoutFromLeft;
				float x = GetPrintX(chartControl, printEvent, useDetailProfileLayout, out barIndex, out layoutLeft, out layoutRight, out hasLayoutRange, out layoutFromLeft);
				if (x < left - padding || x > right + padding)
					continue;

				visibleItems.Add(new OrcaPrintRenderItem(printEvent, barIndex, x, layoutLeft, layoutRight, hasLayoutRange, layoutFromLeft));
				if (printEvent.Volume > maxVisibleVolume)
					maxVisibleVolume = printEvent.Volume;
				if (printEvent.Volume < minVisibleVolume)
					minVisibleVolume = printEvent.Volume;
			}

			if (visibleItems.Count == 0 || maxVisibleVolume <= 0)
				return;

			if (minVisibleVolume == long.MaxValue || minVisibleVolume <= 0)
				minVisibleVolume = 1;

			PreparePrintRenderLayout(visibleItems, chartScale, left, right, top, bottom, padding, useProfileLaneLayout);

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
				OrcaPrintRenderItem item = visibleItems[i];
				PrintEvent printEvent = item.Event;
				float x = item.X;
				float y = item.Y;
				if (y < top - padding || y > bottom + padding)
					continue;

				double volumeRank = CalculateVisibleVolumeRank(printEvent.Volume, minVisibleVolume, maxVisibleVolume);
				float diameter = item.Diameter;
				float radius = item.Radius;
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

		private void PreparePrintRenderLayout(List<OrcaPrintRenderItem> visibleItems, ChartScale chartScale, float panelLeft, float panelRight, float panelTop, float panelBottom, float padding, bool applySameLevelLayout)
		{
			if (visibleItems == null || chartScale == null)
				return;

			VolumeRange singleRange;
			VolumeRange priceLevelRange;
			VolumeRange clusterRange;
			GetVisibleVolumeRanges(visibleItems, out singleRange, out priceLevelRange, out clusterRange);

			for (int i = 0; i < visibleItems.Count; i++)
			{
				OrcaPrintRenderItem item = visibleItems[i];
				if (item.Event != null)
				{
					item.Y = chartScale.GetYByValue(item.Event.Price);
					VolumeRange range = GetVolumeRangeForEvent(item.Event, singleRange, priceLevelRange, clusterRange);
					item.Diameter = CalculateDotDiameter(item.Event, range.Min, range.Max);
					item.Radius = item.Diameter * 0.5f;
					if (item.HasLayoutRange && item.LayoutFromLeft)
						item.X = GetLeftAnchoredLayoutX(item);
				}
				visibleItems[i] = item;
			}

			if (applySameLevelLayout)
				ApplySameLevelHorizontalLayout(visibleItems, panelLeft, panelRight, panelTop, panelBottom, padding);
		}

		private void GetVisibleVolumeRanges(List<OrcaPrintRenderItem> visibleItems, out VolumeRange singleRange, out VolumeRange priceLevelRange, out VolumeRange clusterRange)
		{
			singleRange = new VolumeRange();
			priceLevelRange = new VolumeRange();
			clusterRange = new VolumeRange();

			if (visibleItems == null)
				return;

			for (int i = 0; i < visibleItems.Count; i++)
			{
				PrintEvent printEvent = visibleItems[i].Event;
				if (printEvent == null)
					continue;

				if (printEvent.IsCluster)
					clusterRange.Include(printEvent.Volume);
				else if (printEvent.IsPriceLevel)
					priceLevelRange.Include(printEvent.Volume);
				else
					singleRange.Include(printEvent.Volume);
			}
		}

		private VolumeRange GetVolumeRangeForEvent(PrintEvent printEvent, VolumeRange singleRange, VolumeRange priceLevelRange, VolumeRange clusterRange)
		{
			if (printEvent != null && printEvent.IsCluster)
				return clusterRange;
			if (printEvent != null && printEvent.IsPriceLevel)
				return priceLevelRange;
			return singleRange;
		}

		private void ApplySameLevelHorizontalLayout(List<OrcaPrintRenderItem> visibleItems, float panelLeft, float panelRight, float panelTop, float panelBottom, float padding)
		{
			if (visibleItems == null || visibleItems.Count < 2)
				return;

			Dictionary<string, List<int>> indicesByLevelAndCandle = new Dictionary<string, List<int>>();
			for (int i = 0; i < visibleItems.Count; i++)
			{
				OrcaPrintRenderItem item = visibleItems[i];
				if (item.Event == null || item.Radius <= 0)
					continue;
				if (item.Y < panelTop - padding || item.Y > panelBottom + padding)
					continue;

				string layoutKey = GetSameLevelLayoutKey(item);
				List<int> indices;
				if (!indicesByLevelAndCandle.TryGetValue(layoutKey, out indices))
				{
					indices = new List<int>();
					indicesByLevelAndCandle[layoutKey] = indices;
				}
				indices.Add(i);
			}

			foreach (KeyValuePair<string, List<int>> kvp in indicesByLevelAndCandle)
			{
				List<int> indices = kvp.Value;
				if (indices == null || indices.Count < 2)
					continue;

				indices.Sort((a, b) => visibleItems[a].BaseX.CompareTo(visibleItems[b].BaseX));

				int runStart = 0;
				while (runStart < indices.Count)
				{
					int runEnd = runStart;
					float runMaxDiameter = visibleItems[indices[runStart]].Diameter;

					while (runEnd + 1 < indices.Count)
					{
						OrcaPrintRenderItem current = visibleItems[indices[runEnd]];
						OrcaPrintRenderItem next = visibleItems[indices[runEnd + 1]];
						float spacingThreshold = Math.Max(Math.Max(runMaxDiameter, next.Diameter) + 4.0f, 12.0f);
						if (next.BaseX - current.BaseX > spacingThreshold)
							break;

						runEnd++;
						runMaxDiameter = Math.Max(runMaxDiameter, next.Diameter);
					}

					LayoutSameLevelRun(visibleItems, indices, runStart, runEnd, panelLeft, panelRight);
					runStart = runEnd + 1;
				}
			}
		}

		private string GetSameLevelLayoutKey(OrcaPrintRenderItem item)
		{
			long priceKey = item.Event != null ? GetPriceLevelLayoutKey(item.Event.Price) : 0;
			return priceKey.ToString() + ":" + item.BarIndex.ToString();
		}

		private void LayoutSameLevelRun(List<OrcaPrintRenderItem> visibleItems, List<int> sortedIndices, int runStart, int runEnd, float panelLeft, float panelRight)
		{
			int count = runEnd - runStart + 1;
			if (visibleItems == null || sortedIndices == null || count < 2)
				return;

			float maxDiameter = 0.0f;
			float firstBaseX = visibleItems[sortedIndices[runStart]].BaseX;
			float lastBaseX = visibleItems[sortedIndices[runEnd]].BaseX;
			for (int i = runStart; i <= runEnd; i++)
				maxDiameter = Math.Max(maxDiameter, visibleItems[sortedIndices[i]].Diameter);

			float layoutLeft;
			float layoutRight;
			bool hasRunRange = TryGetRunHorizontalRange(visibleItems, sortedIndices, runStart, runEnd, out layoutLeft, out layoutRight);
			if (!hasRunRange)
			{
				layoutLeft = panelLeft;
				layoutRight = panelRight;
			}

			float spacing = CalculateSameLevelSpacing(maxDiameter, count, layoutRight - layoutLeft);
			float centerX = (firstBaseX + lastBaseX) * 0.5f;
			bool leftAnchorRun = hasRunRange && IsRunLeftAnchored(visibleItems, sortedIndices, runStart, runEnd);
			float firstX = leftAnchorRun
				? layoutLeft + maxDiameter * 0.5f
				: centerX - spacing * (count - 1) * 0.5f;
			float groupWidth = spacing * (count - 1) + maxDiameter;
			float layoutWidth = Math.Max(0.0f, layoutRight - layoutLeft);

			if (layoutWidth > 0.0f && groupWidth <= layoutWidth)
			{
				float groupLeft = firstX - maxDiameter * 0.5f;
				float groupRight = firstX + spacing * (count - 1) + maxDiameter * 0.5f;
				if (groupLeft < layoutLeft)
					firstX += layoutLeft - groupLeft;
				else if (groupRight > layoutRight)
					firstX -= groupRight - layoutRight;
			}
			else if (layoutWidth > maxDiameter && count > 1)
			{
				spacing = Math.Max(0.0f, (layoutWidth - maxDiameter) / (count - 1));
				firstX = layoutLeft + maxDiameter * 0.5f;
			}

			for (int i = 0; i < count; i++)
			{
				int itemIndex = sortedIndices[runStart + i];
				OrcaPrintRenderItem item = visibleItems[itemIndex];
				item.X = firstX + spacing * i;
				visibleItems[itemIndex] = item;
			}
		}

		private float GetLeftAnchoredLayoutX(OrcaPrintRenderItem item)
		{
			float x = item.LayoutLeft + item.Radius;
			if (item.LayoutRight > item.LayoutLeft + item.Radius)
				x = Math.Min(x, item.LayoutRight - item.Radius);
			return x;
		}

		private bool IsRunLeftAnchored(List<OrcaPrintRenderItem> visibleItems, List<int> sortedIndices, int runStart, int runEnd)
		{
			for (int i = runStart; i <= runEnd; i++)
			{
				OrcaPrintRenderItem item = visibleItems[sortedIndices[i]];
				if (!item.LayoutFromLeft)
					return false;
			}

			return true;
		}

		private bool TryGetRunHorizontalRange(List<OrcaPrintRenderItem> visibleItems, List<int> sortedIndices, int runStart, int runEnd, out float layoutLeft, out float layoutRight)
		{
			layoutLeft = 0.0f;
			layoutRight = 0.0f;
			bool hasRange = false;

			for (int i = runStart; i <= runEnd; i++)
			{
				OrcaPrintRenderItem item = visibleItems[sortedIndices[i]];
				if (!item.HasLayoutRange || item.LayoutRight <= item.LayoutLeft)
					return false;

				if (!hasRange)
				{
					layoutLeft = item.LayoutLeft;
					layoutRight = item.LayoutRight;
					hasRange = true;
				}
				else
				{
					layoutLeft = Math.Max(layoutLeft, item.LayoutLeft);
					layoutRight = Math.Min(layoutRight, item.LayoutRight);
				}
			}

			return hasRange && layoutRight > layoutLeft;
		}

		private float CalculateSameLevelSpacing(float maxDiameter, int count, float availableWidth)
		{
			float baseSpacing = Math.Max(maxDiameter + 2.0f, 10.0f);
			if (count < 2 || availableWidth <= 0.0f)
				return baseSpacing;

			float fitSpacing = (availableWidth - maxDiameter) / (count - 1);
			if (fitSpacing <= 0.0f)
				return Math.Max(maxDiameter * 0.55f, 6.0f);

			float compactSpacing = Math.Max(maxDiameter * 0.75f, 8.0f);
			return Math.Min(baseSpacing, Math.Max(compactSpacing, fitSpacing));
		}

		private long GetPriceLevelLayoutKey(double price)
		{
			if (TickSize > 0)
				return (long)Math.Round(price / TickSize);

			return (long)Math.Round(price * 100000000.0);
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

		private float GetPrintX(ChartControl chartControl, PrintEvent printEvent, bool allowAutoProfileLayout, out int barIndex, out float layoutLeft, out float layoutRight, out bool hasLayoutRange, out bool layoutFromLeft)
		{
			barIndex = -1;
			layoutLeft = 0.0f;
			layoutRight = 0.0f;
			hasLayoutRange = false;
			layoutFromLeft = false;

			float x = chartControl.GetXByTime(printEvent.Time);

			if (Bars != null && ChartBars != null)
			{
				try
				{
					barIndex = Bars.GetBar(printEvent.Time);
					if (barIndex >= 0 && barIndex < Bars.Count)
					{
						float barCenterX = chartControl.GetXByBarIndex(ChartBars, barIndex);
						float profileLeft;
						float profileRight;
						float profileCenter;
						bool hasProfileLayout = TryGetOrcaCandleVolumeProfileLayout(chartControl, barIndex, barCenterX, out profileLeft, out profileRight, out profileCenter);
						bool useProfileLayout = hasProfileLayout
							&& ((HorizontalAnchor == OrcaPrintHorizontalAnchor.ExactPrintTime && allowAutoProfileLayout)
								|| HorizontalAnchor == OrcaPrintHorizontalAnchor.OrcaCandleVolumeProfileLeft
								|| HorizontalAnchor == OrcaPrintHorizontalAnchor.OrcaCandleVolumeProfileCenter);

						if (HorizontalAnchor != OrcaPrintHorizontalAnchor.ExactPrintTime)
							x = barCenterX;

						if (useProfileLayout)
						{
							layoutLeft = profileLeft;
							layoutRight = profileRight;
							hasLayoutRange = true;
							layoutFromLeft = HorizontalAnchor == OrcaPrintHorizontalAnchor.ExactPrintTime
								|| HorizontalAnchor == OrcaPrintHorizontalAnchor.OrcaCandleVolumeProfileLeft;

							if (HorizontalAnchor == OrcaPrintHorizontalAnchor.OrcaCandleVolumeProfileLeft)
								x = profileLeft;
							else if (HorizontalAnchor == OrcaPrintHorizontalAnchor.ExactPrintTime)
								x = profileLeft;
							else
								x = profileCenter;
						}
					}
				}
				catch { }
			}

			if (hasLayoutRange)
			{
				layoutLeft += HorizontalOffsetPx;
				layoutRight += HorizontalOffsetPx;
			}

			return x + HorizontalOffsetPx;
		}

		private bool ResolveDetailProfileLayout(ChartControl chartControl)
		{
			if (!AutoCompactLayout)
			{
				detailProfileLayoutActive = true;
				return true;
			}

			float spacing = GetAverageVisibleBarSpacing(chartControl);
			if (spacing <= 0.0f)
				return detailProfileLayoutActive;

			int compactBelow = Math.Max(1, CompactLayoutEnterSpacingPx);
			int detailAbove = Math.Max(compactBelow + 1, DetailLayoutEnterSpacingPx);

			if (detailProfileLayoutActive)
			{
				if (spacing <= compactBelow)
					detailProfileLayoutActive = false;
			}
			else if (spacing >= detailAbove)
			{
				detailProfileLayoutActive = true;
			}

			return detailProfileLayoutActive;
		}

		private bool IsProfileHorizontalAnchor()
		{
			return HorizontalAnchor == OrcaPrintHorizontalAnchor.OrcaCandleVolumeProfileLeft
				|| HorizontalAnchor == OrcaPrintHorizontalAnchor.OrcaCandleVolumeProfileCenter;
		}

		private bool TryGetOrcaCandleVolumeProfileLayout(ChartControl chartControl, int barIndex, float barCenterX, out float profileLeft, out float profileRight, out float profileCenter)
		{
			profileLeft = barCenterX;
			profileRight = barCenterX;
			profileCenter = barCenterX;
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

			float averageBarSpacing = GetVisibleBarSpacing(chartControl, barIndex);
			if (profile.AutoHideProfilesWhenCompressed && averageBarSpacing > 0.0f && averageBarSpacing < profile.MinBarSpacingToShowProfilesPx)
				return false;

			float halfCandle = Math.Max(0.0f, profile.CandleWidthPx * 0.5f);
			float widthScale = (float)Math.Max(0.1, Math.Min(1.0, profile.ProfileWidthScale));
			float profileWidth = Math.Max(2.0f, profile.ProfileWidthPx * widthScale);
			profileLeft = barCenterX + halfCandle + Math.Max(0.0f, profile.CandleProfileGapPx);

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
					profileWidth = Math.Max(2.0f, (availableWidth - 1.0f) * widthScale);
				}
				catch { }
			}

			profileRight = profileLeft + profileWidth;
			profileCenter = profileLeft + profileWidth * 0.5f;
			return true;
		}

		private float GetVisibleBarSpacing(ChartControl chartControl, int barIndex)
		{
			if (chartControl == null || ChartBars == null)
				return 0.0f;

			try
			{
				if (barIndex + 1 < ChartBars.Count)
					return Math.Abs(chartControl.GetXByBarIndex(ChartBars, barIndex + 1) - chartControl.GetXByBarIndex(ChartBars, barIndex));
				if (barIndex > 0)
					return Math.Abs(chartControl.GetXByBarIndex(ChartBars, barIndex) - chartControl.GetXByBarIndex(ChartBars, barIndex - 1));
			}
			catch { }

			return 0.0f;
		}

		private float GetAverageVisibleBarSpacing(ChartControl chartControl)
		{
			if (chartControl == null || ChartBars == null)
				return 0.0f;

			try
			{
				int fromIndex = Math.Max(0, ChartBars.FromIndex);
				int toIndex = Math.Min(ChartBars.ToIndex, ChartBars.Count - 1);
				if (toIndex <= fromIndex)
					return GetVisibleBarSpacing(chartControl, fromIndex);

				float totalSpacing = 0.0f;
				int sampleCount = 0;
				int step = Math.Max(1, (toIndex - fromIndex) / 24);

				for (int barIndex = fromIndex; barIndex + step <= toIndex; barIndex += step)
				{
					float x1 = chartControl.GetXByBarIndex(ChartBars, barIndex);
					float x2 = chartControl.GetXByBarIndex(ChartBars, barIndex + step);
					totalSpacing += Math.Abs(x2 - x1) / step;
					sampleCount++;
				}

				return sampleCount > 0 ? totalSpacing / sampleCount : 0.0f;
			}
			catch { }

			return 0.0f;
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

		private float CalculateDotDiameter(PrintEvent printEvent, long minVisibleVolume, long maxVisibleVolume)
		{
			double ratio = CalculateDotSizeRatio(printEvent != null ? printEvent.Volume : 0, minVisibleVolume, maxVisibleVolume);
			float minSize;
			float maxSize;
			GetDotSizeRange(printEvent, out minSize, out maxSize);

			return (float)(minSize + ratio * (maxSize - minSize));
		}

		private double CalculateDotSizeRatio(long volume, long minVisibleVolume, long maxVisibleVolume)
		{
			if (DotSizeScale == NinjaTrader.NinjaScript.Indicators.DotSizeScale.Linear)
			{
				return Clamp01(maxVisibleVolume > 0 ? (double)volume / (double)maxVisibleVolume : 0.0);
			}
			else
			{
				if (maxVisibleVolume <= minVisibleVolume || minVisibleVolume <= 0 || volume <= 0)
					return 1.0;

				double numerator = Math.Log((double)volume / (double)minVisibleVolume);
				double denominator = Math.Log((double)maxVisibleVolume / (double)minVisibleVolume);
				return Clamp01(denominator > 0.0000001 ? numerator / denominator : 1.0);
			}
		}

		private void GetDotSizeRange(PrintEvent printEvent, out float minSize, out float maxSize)
		{
			int configuredMin = SinglePrintMinDotSize;
			int configuredMax = SinglePrintMaxDotSize;

			if (printEvent != null && printEvent.IsCluster)
			{
				configuredMin = ClusterMinDotSize;
				configuredMax = ClusterMaxDotSize;
			}
			else if (printEvent != null && printEvent.IsPriceLevel)
			{
				configuredMin = PriceLevelMinDotSize;
				configuredMax = PriceLevelMaxDotSize;
			}

			minSize = Math.Max(1.0f, Math.Min(configuredMin, configuredMax));
			maxSize = Math.Max(minSize, Math.Max(configuredMin, configuredMax));
		}

		private float GetMaxConfiguredDotSize()
		{
			return Math.Max(1.0f, Math.Max(Math.Max(MaxDotSize, MinDotSize), Math.Max(Math.Max(SinglePrintMaxDotSize, PriceLevelMaxDotSize), ClusterMaxDotSize)));
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

			IntPtr currentTarget = RenderTarget.NativePointer;
			if (dxBrushCacheRenderTarget != IntPtr.Zero && dxBrushCacheRenderTarget != currentTarget)
				DisposeDxBrushCache();

			if (dxBrushCache == null)
				dxBrushCache = new Dictionary<int, DxSolidColorBrush>();
			dxBrushCacheRenderTarget = currentTarget;

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

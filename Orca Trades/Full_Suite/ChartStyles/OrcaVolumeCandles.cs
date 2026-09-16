#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
#endregion

namespace NinjaTrader.NinjaScript.ChartStyles
{
	/// <summary>
	/// Orca volume candles: TradingView-like equivolume candlesticks.
	/// Body width follows relative bar volume through a shaping exponent, with a
	/// minimum-width floor so low-volume bars keep a small consistent gap instead of
	/// collapsing to hairlines, and a maximum-width ceiling so high-volume bars pop
	/// while keeping a hairline gap. Normalization matches NT Equivolume (visible range max).
	/// </summary>
	public class OrcaVolumeCandles : ChartStyle
	{
		private const int OrcaVolumeCandlesStyleTypeId = 137;

		private object icon;

		public override object Icon => icon ?? (icon = Icons.ChartChartStyle);

		#region Properties

		[NinjaScriptProperty]
		[Range(0.1, 2.0)]
		[Display(Name = "Width Exponent", Order = 10, GroupName = "Volume Width",
			Description = "Shapes volume-to-width mapping. Below 1 boosts thin/mid-volume bars (TV-like packing); above 1 exaggerates high-volume bars.")]
		public double WidthExponent { get; set; }

		[NinjaScriptProperty]
		[Range(5, 90)]
		[Display(Name = "Min Body Width %", Order = 11, GroupName = "Volume Width",
			Description = "Minimum candle body width as percent of bar slot. Keeps a small consistent gap on low-volume bars.")]
		public int MinBodyWidthPercent { get; set; }

		[NinjaScriptProperty]
		[Range(10, 100)]
		[Display(Name = "Max Body Width %", Order = 12, GroupName = "Volume Width",
			Description = "Maximum candle body width as percent of bar slot. High-volume bars nearly fill the slot with a hairline gap.")]
		public int MaxBodyWidthPercent { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Wick Matches Body", Order = 20, GroupName = "Colors",
			Description = "If true, wicks use the candle body (up/down) color; otherwise the Wick color.")]
		public bool WickMatchesBody { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Border Matches Wick", Order = 21, GroupName = "Colors",
			Description = "If true, the body border uses the wick color; otherwise the Border color.")]
		public bool BorderMatchesWick { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 0.5)]
		[Display(Name = "Wick Width Scale", Order = 30, GroupName = "Wicks",
			Description = "Wick line width as a fraction of body width (clamped to 1-3px). 0 forces a 1px wick.")]
		public double WickWidthScale { get; set; }

		#endregion

		public override int GetBarPaintWidth(int barWidth)
		{
			return 1 + 2 * (barWidth - 1) + 2 * (int)Math.Round(Stroke.Width);
		}

		public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
		{
			if (chartControl == null || chartScale == null || chartBars == null)
				return;

			Bars bars = chartBars.Bars;
			if (bars == null || bars.Count == 0)
				return;

			int fromIdx = Math.Max(0, chartBars.FromIndex);
			int toIdx = Math.Min(chartBars.ToIndex, bars.Count - 1);
			if (toIdx < fromIdx)
				return;

			float paintSlot = GetBarPaintWidth(BarWidthUI);
			if (paintSlot < 1f)
				paintSlot = 1f;

			double referenceVolume = 0;
			for (int i = fromIdx; i <= toIdx; i++)
			{
				double volume = bars.GetVolume(i);
				if (volume > referenceVolume)
					referenceVolume = volume;
			}
			if (referenceVolume <= 0)
				referenceVolume = 1;

			double exponent = Math.Min(2.0, Math.Max(0.1, WidthExponent));
			double minFraction = Math.Min(1.0, Math.Max(0.01, MinBodyWidthPercent / 100.0));
			double maxFraction = Math.Min(1.0, Math.Max(0.01, MaxBodyWidthPercent / 100.0));
			if (maxFraction < minFraction)
				maxFraction = minFraction;
			double wickScale = Math.Min(0.5, Math.Max(0.0, WickWidthScale));

			Vector2 lineStart = new Vector2();
			Vector2 lineEnd = new Vector2();
			RectangleF bodyRect = new RectangleF();

			for (int i = fromIdx; i <= toIdx; i++)
			{
				int barCenterX = chartControl.GetXByBarIndex(chartBars, i);

				float slot = paintSlot;
				if (i > fromIdx)
					slot = Math.Min(slot, Math.Abs(barCenterX - chartControl.GetXByBarIndex(chartBars, i - 1)));
				else if (i < toIdx)
					slot = Math.Min(slot, Math.Abs(barCenterX - chartControl.GetXByBarIndex(chartBars, i + 1)));
				if (slot < 1f)
					slot = 1f;

				double normalized = bars.GetVolume(i) / referenceVolume;
				if (normalized < 0)
					normalized = 0;
				else if (normalized > 1)
					normalized = 1;

				double widthFraction = minFraction + (maxFraction - minFraction) * Math.Pow(normalized, exponent);
				float bodyWidth = (float)(slot * widthFraction);
				if (bodyWidth < 1f)
					bodyWidth = 1f;
				else if (bodyWidth > slot)
					bodyWidth = slot;

				float wickWidth = wickScale <= 0
					? 1f
					: Math.Min(3f, Math.Max(1f, (float)Math.Round(bodyWidth * wickScale)));

				Brush barOverrideBrush = chartControl.GetBarOverrideBrush(chartBars, i);
				Brush candleOutlineOverrideBrush = chartControl.GetCandleOutlineOverrideBrush(chartBars, i);

				double close = bars.GetClose(i);
				double high = bars.GetHigh(i);
				double low = bars.GetLow(i);
				double open = bars.GetOpen(i);

				int closeY = chartScale.GetYByValue(close);
				int highY = chartScale.GetYByValue(high);
				int lowY = chartScale.GetYByValue(low);
				int openY = chartScale.GetYByValue(open);

				// Body: up/down brushes. Wick: own color, or body when WickMatchesBody.
				// Border: own color, or wick color when BorderMatchesWick.
				// Candle outline overrides (e.g. Absorption) still win for border + wick.
				Brush bodyBrush = barOverrideBrush ?? (close >= open ? UpBrushDX : DownBrushDX);
				Brush wickBrush = candleOutlineOverrideBrush ?? (WickMatchesBody ? bodyBrush : Stroke2.BrushDX);
				Brush outlineBrush = candleOutlineOverrideBrush ?? (BorderMatchesWick ? wickBrush : Stroke.BrushDX);

				if (Math.Abs(openY - closeY) < 1)
				{
					lineStart.X = barCenterX - bodyWidth * 0.5f;
					lineStart.Y = closeY;
					lineEnd.X = barCenterX + bodyWidth * 0.5f;
					lineEnd.Y = closeY;
					if (!(outlineBrush is SolidColorBrush))
						TransformBrush(outlineBrush, new RectangleF(lineStart.X, lineStart.Y - Stroke.Width, bodyWidth, Stroke.Width));
					RenderTarget.DrawLine(lineStart, lineEnd, outlineBrush, Stroke.Width, Stroke.StrokeStyle);
				}
				else
				{
					bodyRect.X = barCenterX - bodyWidth * 0.5f + 0.5f;
					bodyRect.Y = Math.Min(closeY, openY);
					bodyRect.Width = bodyWidth - 1f;
					bodyRect.Height = Math.Max(closeY, openY) - Math.Min(closeY, openY);
					if (!(bodyBrush is SolidColorBrush))
						TransformBrush(bodyBrush, bodyRect);
					RenderTarget.FillRectangle(bodyRect, bodyBrush);
					if (!(outlineBrush is SolidColorBrush))
						TransformBrush(outlineBrush, bodyRect);
					RenderTarget.DrawRectangle(bodyRect, outlineBrush, Stroke.Width, Stroke.StrokeStyle);
				}

				if (high > Math.Max(open, close))
				{
					lineStart.X = barCenterX;
					lineStart.Y = highY;
					lineEnd.X = barCenterX;
					lineEnd.Y = open > close ? openY : closeY;
					if (!(wickBrush is SolidColorBrush))
						TransformBrush(wickBrush, new RectangleF(lineStart.X - wickWidth, lineStart.Y, wickWidth, lineEnd.Y - lineStart.Y));
					RenderTarget.DrawLine(lineStart, lineEnd, wickBrush, wickWidth, Stroke2.StrokeStyle);
				}

				if (low < Math.Min(open, close))
				{
					lineStart.X = barCenterX;
					lineStart.Y = lowY;
					lineEnd.X = barCenterX;
					lineEnd.Y = open < close ? openY : closeY;
					if (!(wickBrush is SolidColorBrush))
						TransformBrush(wickBrush, new RectangleF(lineEnd.X - wickWidth, lineEnd.Y, wickWidth, lineStart.Y - lineEnd.Y));
					RenderTarget.DrawLine(lineStart, lineEnd, wickBrush, wickWidth, Stroke2.StrokeStyle);
				}
			}
		}

		public override object Clone()
		{
			OrcaVolumeCandles clone = base.Clone() as OrcaVolumeCandles;
			if (clone != null)
			{
				clone.WidthExponent = WidthExponent;
				clone.MinBodyWidthPercent = MinBodyWidthPercent;
				clone.MaxBodyWidthPercent = MaxBodyWidthPercent;
				clone.WickMatchesBody = WickMatchesBody;
				clone.BorderMatchesWick = BorderMatchesWick;
				clone.WickWidthScale = WickWidthScale;
			}
			return clone ?? new OrcaVolumeCandles();
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca Volume Candles";
				ChartStyleType = (ChartStyleType)OrcaVolumeCandlesStyleTypeId;
				WidthExponent = 0.5;
				MinBodyWidthPercent = 35;
				MaxBodyWidthPercent = 96;
				WickMatchesBody = true;
				BorderMatchesWick = false;
				WickWidthScale = 0.1;
			}
			else if (State == State.Configure)
			{
				// Body up/down, Border (Stroke), Wick (Stroke2) — separate color picks.
				SetPropertyName("BarWidth", "Bar width");
				SetPropertyName("UpBrush", "Body up");
				SetPropertyName("DownBrush", "Body down");
				SetPropertyName("Stroke", "Border");
				SetPropertyName("Stroke2", "Wick");
				SetPropertyOrder("BarWidth", 1);
				SetPropertyOrder("UpBrush", 2);
				SetPropertyOrder("DownBrush", 3);
				SetPropertyOrder("Stroke", 4);
				SetPropertyOrder("Stroke2", 5);
			}
		}
	}
}

#region Using declarations
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Xml.Serialization;
using DxBrush = SharpDX.Direct2D1.Brush;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
#endregion

namespace NinjaTrader.NinjaScript.ChartStyles
{
	public enum OrcaVolumeNormalizationMode
	{
		[Description("Visible Range")]
		VisibleRange,
		[Description("Rolling Lookback")]
		RollingLookback
	}

	public enum OrcaVolumeScalingMethod
	{
		Linear,
		[Description("Square Root")]
		SquareRoot,
		Logarithmic
	}

	public enum OrcaVolumeColorAssignment
	{
		[Description("Open vs. Close")]
		OpenVsClose,
		[Description("Previous Close")]
		PreviousClose
	}

	public abstract class OrcaVolumeEnumDescriptionConverter : EnumConverter
	{
		private readonly Type enumType;

		protected OrcaVolumeEnumDescriptionConverter(Type type) : base(type)
		{
			enumType = type;
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string text = value as string;
			if (!string.IsNullOrWhiteSpace(text))
			{
				foreach (object enumValue in Enum.GetValues(enumType))
				{
					if (string.Equals(text, GetDescription(enumValue), StringComparison.OrdinalIgnoreCase)
						|| string.Equals(text, enumValue.ToString(), StringComparison.OrdinalIgnoreCase))
						return enumValue;
				}
			}

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string) && value != null && enumType.IsInstanceOfType(value))
				return GetDescription(value);

			return base.ConvertTo(context, culture, value, destinationType);
		}

		private string GetDescription(object value)
		{
			FieldInfo field = enumType.GetField(value.ToString());
			DescriptionAttribute description = field == null
				? null
				: Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
			return description == null ? value.ToString() : description.Description;
		}
	}

	public sealed class OrcaVolumeNormalizationModeConverter : OrcaVolumeEnumDescriptionConverter
	{
		public OrcaVolumeNormalizationModeConverter() : base(typeof(OrcaVolumeNormalizationMode)) { }
	}

	public sealed class OrcaVolumeScalingMethodConverter : OrcaVolumeEnumDescriptionConverter
	{
		public OrcaVolumeScalingMethodConverter() : base(typeof(OrcaVolumeScalingMethod)) { }
	}

	public sealed class OrcaVolumeColorAssignmentConverter : OrcaVolumeEnumDescriptionConverter
	{
		public OrcaVolumeColorAssignmentConverter() : base(typeof(OrcaVolumeColorAssignment)) { }
	}

	/// <summary>
	/// Paints the chart's existing OHLCV bars as centered candlesticks whose body width is normalized by volume.
	/// This ChartStyle never changes bar construction, timestamps, horizontal spacing, or chart-scale values.
	/// </summary>
	public class OrcaVolumeCandles : ChartStyle
	{
		// 0x4F564331 spells "OVC1" and is intentionally well above NinjaTrader's reserved range.
		public const int ChartStyleTypeIdentifier = 0x4F564331;

		private object icon;

		private MediaBrush dojiBrush;
		private MediaBrush outlineBrush;
		private MediaBrush wickBrush;
		private DxBrush dojiBrushDX;
		private DxBrush outlineBrushDX;
		private DxBrush wickBrushDX;

		// Reused render scratch buffers. They grow only when a larger visible/calculation range is requested.
		private double[] volumeBuffer;
		private bool[] volumeValidBuffer;
		private double[] sortedVolumeBuffer;
		private int[] volumeRankBuffer;
		private int[] fenwickTree;
		private double[] widthFactorBuffer;
		private float[] centerBuffer;
		private float[] paintWidthBuffer;

		public override object Icon => icon ??= NinjaTrader.Gui.Tools.Icons.ChartEquivolume;

		public override int GetBarPaintWidth(int barWidth)
		{
			int coreWidth = 1 + 2 * Math.Max(0, barWidth - 1);
			int outlineAllowance = ShowOutlines ? 2 * Math.Max(1, OutlineWidth) : 0;
			return Math.Max(1, coreWidth + outlineAllowance);
		}

		public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
		{
			if (RenderTarget == null || chartControl == null || chartScale == null || chartBars == null || chartBars.Bars == null)
				return;

			Bars bars = chartBars.Bars;
			int fromIndex = Math.Max(0, chartBars.FromIndex);
			int toIndex = Math.Min(chartBars.ToIndex, bars.Count - 1);
			if (fromIndex > toIndex)
				return;

			int visibleCount = toIndex - fromIndex + 1;
			int lookback = Math.Max(1, LookbackBars);
			int calculationStart = NormalizationMode == OrcaVolumeNormalizationMode.RollingLookback
				? Math.Max(0, fromIndex - lookback + 1)
				: fromIndex;
			int calculationCount = toIndex - calculationStart + 1;

			EnsureBufferCapacity(calculationCount, visibleCount);
			for (int offset = 0; offset < calculationCount; offset++)
			{
				double volume;
				bool isValid = TryReadVolume(bars, calculationStart + offset, out volume);
				volumeBuffer[offset] = volume;
				volumeValidBuffer[offset] = isValid;
			}

			if (NormalizationMode == OrcaVolumeNormalizationMode.RollingLookback)
				BuildRollingWidthFactors(calculationStart, fromIndex, calculationCount, visibleCount, lookback);
			else
				BuildVisibleRangeWidthFactors(calculationCount, visibleCount);

			for (int visibleOffset = 0; visibleOffset < visibleCount; visibleOffset++)
				centerBuffer[visibleOffset] = chartControl.GetXByBarIndex(chartBars, fromIndex + visibleOffset);

			BuildPaintWidths(visibleCount);

			DxBrush upBrushDX = UpBrushDX;
			DxBrush downBrushDX = DownBrushDX;
			DxBrush neutralBrushDX = GetOwnedDxBrush(ref dojiBrushDX, DojiBrush);
			DxBrush candleOutlineBrushDX = ShowOutlines ? GetOwnedDxBrush(ref outlineBrushDX, OutlineBrush) : null;
			DxBrush candleWickBrushDX = ShowWicks ? GetOwnedDxBrush(ref wickBrushDX, WickBrush) : null;
			if (upBrushDX == null || downBrushDX == null || neutralBrushDX == null)
				return;

			AntialiasMode previousAntialiasMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = AntialiasMode.Aliased;

			try
			{
				Vector2 point0 = new Vector2();
				Vector2 point1 = new Vector2();

				for (int visibleOffset = 0; visibleOffset < visibleCount; visibleOffset++)
				{
					int index = fromIndex + visibleOffset;
					double openValue;
					double highValue;
					double lowValue;
					double closeValue;

					try
					{
						openValue = bars.GetOpen(index);
						highValue = bars.GetHigh(index);
						lowValue = bars.GetLow(index);
						closeValue = bars.GetClose(index);
					}
					catch
					{
						continue;
					}

					if (!IsFinite(openValue) || !IsFinite(highValue) || !IsFinite(lowValue) || !IsFinite(closeValue))
						continue;

					int openY = chartScale.GetYByValue(openValue);
					int highY = chartScale.GetYByValue(highValue);
					int lowY = chartScale.GetYByValue(lowValue);
					int closeY = chartScale.GetYByValue(closeValue);
					float x = centerBuffer[visibleOffset];
					float paintWidth = Math.Max(1f, paintWidthBuffer[visibleOffset]);
					bool isDoji = ComparePrices(bars, closeValue, openValue) == 0;
					int colorDirection = GetColorDirection(bars, index, openValue, closeValue);

					DxBrush overriddenBarBrush = chartControl.GetBarOverrideBrush(chartBars, index);
					DxBrush overriddenOutlineBrush = chartControl.GetCandleOutlineOverrideBrush(chartBars, index);
					DxBrush directionBrush = colorDirection > 0 ? upBrushDX : colorDirection < 0 ? downBrushDX : neutralBrushDX;

					if (isDoji)
					{
						float dojiWidth = paintWidth;
						float dojiThickness = ShowOutlines ? Math.Max(1f, OutlineWidth) : 1f;
						point0.X = x - dojiWidth * 0.5f;
						point0.Y = closeY;
						point1.X = x + dojiWidth * 0.5f;
						point1.Y = closeY;
						DxBrush dojiLineBrush = overriddenOutlineBrush ?? directionBrush;
						TransformIfNeeded(dojiLineBrush, new RectangleF(point0.X, closeY - dojiThickness * 0.5f, dojiWidth, dojiThickness));
						RenderTarget.DrawLine(point0, point1, dojiLineBrush, dojiThickness);
					}
					else
					{
						float outlineAllowance = ShowOutlines ? Math.Min(Math.Max(1f, OutlineWidth), Math.Max(0f, paintWidth - 1f)) : 0f;
						float bodyWidth = Math.Max(1f, paintWidth - outlineAllowance);
						float bodyHeight = Math.Max(1f, Math.Abs(openY - closeY));
						RectangleF bodyRectangle = new RectangleF(
							x - bodyWidth * 0.5f,
							Math.Min(openY, closeY),
							bodyWidth,
							bodyHeight);

						DxBrush bodyBrush = overriddenBarBrush ?? directionBrush;
						TransformIfNeeded(bodyBrush, bodyRectangle);
						FillRectangleWithOpacity(bodyRectangle, bodyBrush);

						if (ShowOutlines)
						{
							DxBrush borderBrush = overriddenOutlineBrush ?? candleOutlineBrushDX;
							if (borderBrush != null)
							{
								TransformIfNeeded(borderBrush, bodyRectangle);
								RenderTarget.DrawRectangle(bodyRectangle, borderBrush, Math.Max(1f, OutlineWidth));
							}
						}
					}

					if (!ShowWicks)
						continue;

					DxBrush activeWickBrush = overriddenOutlineBrush ?? candleWickBrushDX;
					if (activeWickBrush == null)
						continue;

					float wickWidth = Math.Max(1f, WickWidth);
					if (ComparePrices(bars, highValue, Math.Max(openValue, closeValue)) > 0)
					{
						point0.X = x;
						point0.Y = highY;
						point1.X = x;
						point1.Y = openValue > closeValue ? openY : closeY;
						TransformIfNeeded(activeWickBrush, new RectangleF(x - wickWidth * 0.5f, Math.Min(point0.Y, point1.Y), wickWidth, Math.Max(1f, Math.Abs(point1.Y - point0.Y))));
						RenderTarget.DrawLine(point0, point1, activeWickBrush, wickWidth);
					}

					if (ComparePrices(bars, lowValue, Math.Min(openValue, closeValue)) < 0)
					{
						point0.X = x;
						point0.Y = lowY;
						point1.X = x;
						point1.Y = openValue < closeValue ? openY : closeY;
						TransformIfNeeded(activeWickBrush, new RectangleF(x - wickWidth * 0.5f, Math.Min(point0.Y, point1.Y), wickWidth, Math.Max(1f, Math.Abs(point1.Y - point0.Y))));
						RenderTarget.DrawLine(point0, point1, activeWickBrush, wickWidth);
					}
				}
			}
			finally
			{
				RenderTarget.AntialiasMode = previousAntialiasMode;
			}
		}

		public override void OnRenderTargetChanged()
		{
			DisposeOwnedDxResources();
			base.OnRenderTargetChanged();
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Orca Volume Candles";
				ChartStyleType = (NinjaTrader.Gui.Chart.ChartStyleType)ChartStyleTypeIdentifier;
				BarWidth = 5;

				NormalizationMode = OrcaVolumeNormalizationMode.VisibleRange;
				LookbackBars = 100;
				MinimumWidthPercent = 15;
				MaximumWidthPercent = 90;
				WidthSensitivity = 1.0;
				ScalingMethod = OrcaVolumeScalingMethod.Linear;
				OutlierClipping = true;
				UpperPercentile = 95;
				MinimumGapPixels = 1;

				ColorAssignment = OrcaVolumeColorAssignment.OpenVsClose;
				UpBodyBrush = Core.Globals.GeneralOptions.BrushUpPrimary ?? MediaBrushes.LimeGreen;
				DownBodyBrush = Core.Globals.GeneralOptions.BrushDownPrimary ?? MediaBrushes.Red;
				MediaBrush chartStroke = GetDefaultChartStrokeBrush();
				DojiBrush = chartStroke;
				OutlineBrush = chartStroke;
				WickBrush = chartStroke;
				BodyOpacity = 100;
				OutlineWidth = 1;
				WickWidth = 1;
				ShowOutlines = true;
				ShowWicks = true;
			}
			else if (State == State.Configure)
			{
				SetPropertyName(nameof(BarWidth), "Base Bar Width");
				SetPropertyOrder(nameof(BarWidth), 0);
				RemoveInheritedProperty(nameof(UpBrush));
				RemoveInheritedProperty(nameof(DownBrush));
				RemoveInheritedProperty(nameof(Stroke));
				RemoveInheritedProperty(nameof(Stroke2));
			}
			else if (State == State.Terminated)
			{
				DisposeOwnedDxResources();
			}
		}

		public override object Clone()
		{
			object clone = base.Clone();

			// A compile can return a valid clone from the newly loaded assembly that cannot be cast here.
			if (clone is not OrcaVolumeCandles ret)
				return clone ?? new OrcaVolumeCandles();

			// base.Clone() is shallow for private scratch/resource fields; detach without disposing the source instance.
			ret.dojiBrushDX = null;
			ret.outlineBrushDX = null;
			ret.wickBrushDX = null;
			ret.volumeBuffer = null;
			ret.volumeValidBuffer = null;
			ret.sortedVolumeBuffer = null;
			ret.volumeRankBuffer = null;
			ret.fenwickTree = null;
			ret.widthFactorBuffer = null;
			ret.centerBuffer = null;
			ret.paintWidthBuffer = null;

			ret.UpBodyBrush = UpBodyBrush?.Clone();
			ret.DownBodyBrush = DownBodyBrush?.Clone();
			ret.DojiBrush = DojiBrush?.Clone();
			ret.OutlineBrush = OutlineBrush?.Clone();
			ret.WickBrush = WickBrush?.Clone();
			return ret;
		}

		private void BuildVisibleRangeWidthFactors(int calculationCount, int visibleCount)
		{
			int validCount = 0;
			for (int i = 0; i < calculationCount; i++)
			{
				if (volumeValidBuffer[i])
					sortedVolumeBuffer[validCount++] = volumeBuffer[i];
			}

			if (validCount == 0)
			{
				Array.Clear(widthFactorBuffer, 0, visibleCount);
				return;
			}

			Array.Sort(sortedVolumeBuffer, 0, validCount);
			double minimum = sortedVolumeBuffer[0];
			double upperReference = OutlierClipping
				? GetPercentileFromSortedBuffer(validCount, UpperPercentile * 0.01)
				: sortedVolumeBuffer[validCount - 1];
			bool equalRange = !IsFinite(upperReference) || upperReference <= minimum;

			for (int visibleOffset = 0; visibleOffset < visibleCount; visibleOffset++)
			{
				if (!volumeValidBuffer[visibleOffset])
				{
					widthFactorBuffer[visibleOffset] = 0;
					continue;
				}

				widthFactorBuffer[visibleOffset] = equalRange
					? 0.5
					: TransformNormalizedVolume(NormalizeVolume(volumeBuffer[visibleOffset], minimum, upperReference));
			}
		}

		private void BuildRollingWidthFactors(int calculationStart, int fromIndex, int calculationCount, int visibleCount, int lookback)
		{
			int validCount = 0;
			for (int i = 0; i < calculationCount; i++)
			{
				if (volumeValidBuffer[i])
					sortedVolumeBuffer[validCount++] = volumeBuffer[i];
			}

			if (validCount == 0)
			{
				Array.Clear(widthFactorBuffer, 0, visibleCount);
				return;
			}

			Array.Sort(sortedVolumeBuffer, 0, validCount);
			int uniqueCount = 0;
			for (int i = 0; i < validCount; i++)
			{
				if (uniqueCount == 0 || sortedVolumeBuffer[i] != sortedVolumeBuffer[uniqueCount - 1])
					sortedVolumeBuffer[uniqueCount++] = sortedVolumeBuffer[i];
			}

			for (int i = 0; i < calculationCount; i++)
			{
				volumeRankBuffer[i] = volumeValidBuffer[i]
					? Array.BinarySearch(sortedVolumeBuffer, 0, uniqueCount, volumeBuffer[i])
					: -1;
			}

			Array.Clear(fenwickTree, 0, uniqueCount + 1);
			int windowValidCount = 0;
			int firstVisibleOffset = fromIndex - calculationStart;

			for (int calculationOffset = 0; calculationOffset < calculationCount; calculationOffset++)
			{
				int addedRank = volumeRankBuffer[calculationOffset];
				if (addedRank >= 0)
				{
					FenwickAdd(addedRank + 1, 1, uniqueCount);
					windowValidCount++;
				}

				int expiredOffset = calculationOffset - lookback;
				if (expiredOffset >= 0)
				{
					int expiredRank = volumeRankBuffer[expiredOffset];
					if (expiredRank >= 0)
					{
						FenwickAdd(expiredRank + 1, -1, uniqueCount);
						windowValidCount--;
					}
				}

				if (calculationOffset < firstVisibleOffset)
					continue;

				int visibleOffset = calculationOffset - firstVisibleOffset;
				if (visibleOffset >= visibleCount)
					break;

				if (addedRank < 0 || windowValidCount <= 0)
				{
					widthFactorBuffer[visibleOffset] = 0;
					continue;
				}

				double minimum = sortedVolumeBuffer[FenwickFindByOrder(1, uniqueCount)];
				double upperReference = OutlierClipping
					? GetPercentileFromFenwick(windowValidCount, uniqueCount, UpperPercentile * 0.01)
					: sortedVolumeBuffer[FenwickFindByOrder(windowValidCount, uniqueCount)];

				widthFactorBuffer[visibleOffset] = upperReference <= minimum
					? 0.5
					: TransformNormalizedVolume(NormalizeVolume(volumeBuffer[calculationOffset], minimum, upperReference));
			}
		}

		private void BuildPaintWidths(int visibleCount)
		{
			double minimumPercent = Math.Min(MinimumWidthPercent, MaximumWidthPercent);
			double maximumPercent = Math.Max(MinimumWidthPercent, MaximumWidthPercent);
			float configuredMaximumWidth = Math.Max(1f, GetBarPaintWidth(BarWidthUI));

			for (int i = 0; i < visibleCount; i++)
			{
				float leftSpacing = i > 0 ? Math.Abs(centerBuffer[i] - centerBuffer[i - 1]) : float.PositiveInfinity;
				float rightSpacing = i + 1 < visibleCount ? Math.Abs(centerBuffer[i + 1] - centerBuffer[i]) : float.PositiveInfinity;
				float localSpacing = Math.Min(leftSpacing, rightSpacing);
				if (float.IsInfinity(localSpacing) || float.IsNaN(localSpacing))
					localSpacing = configuredMaximumWidth + MinimumGapPixels;

				float availableSlot = Math.Max(1f, localSpacing - Math.Max(0, MinimumGapPixels));
				availableSlot = Math.Min(availableSlot, configuredMaximumWidth);

				double factor = Clamp01(widthFactorBuffer[i]);
				double widthPercent = minimumPercent + (maximumPercent - minimumPercent) * factor;
				float requestedWidth = (float)(availableSlot * widthPercent * 0.01);
				float roundedWidth = (float)Math.Round(Math.Max(1f, requestedWidth), MidpointRounding.AwayFromZero);
				paintWidthBuffer[i] = Math.Max(1f, Math.Min(availableSlot, roundedWidth));
			}
		}

		private double GetPercentileFromSortedBuffer(int count, double percentile)
		{
			if (count <= 1)
				return sortedVolumeBuffer[0];

			double position = (count - 1) * Clamp01(percentile);
			int lower = (int)Math.Floor(position);
			int upper = (int)Math.Ceiling(position);
			double fraction = position - lower;
			return sortedVolumeBuffer[lower] + (sortedVolumeBuffer[upper] - sortedVolumeBuffer[lower]) * fraction;
		}

		private double GetPercentileFromFenwick(int count, int uniqueCount, double percentile)
		{
			if (count <= 1)
				return sortedVolumeBuffer[FenwickFindByOrder(1, uniqueCount)];

			double position = (count - 1) * Clamp01(percentile);
			int lowerOrder = (int)Math.Floor(position) + 1;
			int upperOrder = (int)Math.Ceiling(position) + 1;
			double fraction = position - Math.Floor(position);
			double lower = sortedVolumeBuffer[FenwickFindByOrder(lowerOrder, uniqueCount)];
			double upper = sortedVolumeBuffer[FenwickFindByOrder(upperOrder, uniqueCount)];
			return lower + (upper - lower) * fraction;
		}

		private void FenwickAdd(int index, int delta, int size)
		{
			for (int i = index; i <= size; i += i & -i)
				fenwickTree[i] += delta;
		}

		private int FenwickFindByOrder(int order, int size)
		{
			int index = 0;
			int bit = 1;
			while ((bit << 1) <= size)
				bit <<= 1;

			for (; bit != 0; bit >>= 1)
			{
				int next = index + bit;
				if (next <= size && fenwickTree[next] < order)
				{
					index = next;
					order -= fenwickTree[next];
				}
			}

			return Math.Min(size - 1, index);
		}

		private double NormalizeVolume(double volume, double minimum, double upperReference)
		{
			if (!IsFinite(volume) || !IsFinite(minimum) || !IsFinite(upperReference) || upperReference <= minimum)
				return 0.5;

			double clipped = Math.Min(Math.Max(volume, minimum), upperReference);
			return Clamp01((clipped - minimum) / (upperReference - minimum));
		}

		private double TransformNormalizedVolume(double normalized)
		{
			double value = Clamp01(normalized);
			switch (ScalingMethod)
			{
				case OrcaVolumeScalingMethod.SquareRoot:
					value = Math.Sqrt(value);
					break;
				case OrcaVolumeScalingMethod.Logarithmic:
					value = Math.Log(1.0 + 9.0 * value) / Math.Log(10.0);
					break;
			}

			if (value <= 0)
				return 0;
			if (value >= 1)
				return 1;

			double sensitivity = Math.Max(0.1, WidthSensitivity);
			return Clamp01(Math.Pow(value, 1.0 / sensitivity));
		}

		private int GetColorDirection(Bars bars, int index, double openValue, double closeValue)
		{
			if (ColorAssignment == OrcaVolumeColorAssignment.PreviousClose && index > 0)
			{
				try
				{
					double previousClose = bars.GetClose(index - 1);
					if (IsFinite(previousClose))
						return ComparePrices(bars, closeValue, previousClose);
				}
				catch
				{
					// Fall through to Open vs. Close when the preceding bar is not yet available.
				}
			}

			return ComparePrices(bars, closeValue, openValue);
		}

		private int ComparePrices(Bars bars, double first, double second)
		{
			try
			{
				if (bars.Instrument != null && bars.Instrument.MasterInstrument != null)
					return bars.Instrument.MasterInstrument.Compare(first, second);
			}
			catch
			{
			}

			double difference = first - second;
			return Math.Abs(difference) < 1E-10 ? 0 : difference > 0 ? 1 : -1;
		}

		private bool TryReadVolume(Bars bars, int index, out double volume)
		{
			volume = 0;
			try
			{
				double candidate = bars.GetVolume(index);
				if (!IsFinite(candidate))
					return false;

				volume = Math.Max(0, candidate);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private void FillRectangleWithOpacity(RectangleF rectangle, DxBrush brush)
		{
			if (brush == null)
				return;

			float originalOpacity = brush.Opacity;
			try
			{
				brush.Opacity = originalOpacity * Math.Max(0f, Math.Min(1f, BodyOpacity * 0.01f));
				RenderTarget.FillRectangle(rectangle, brush);
			}
			finally
			{
				brush.Opacity = originalOpacity;
			}
		}

		private void TransformIfNeeded(DxBrush brush, RectangleF bounds)
		{
			if (brush != null && brush is not SolidColorBrush)
				TransformBrush(brush, bounds);
		}

		private DxBrush GetOwnedDxBrush(ref DxBrush dxBrush, MediaBrush mediaBrush)
		{
			if (RenderTarget == null || mediaBrush == null)
				return null;

			if (dxBrush == null || dxBrush.IsDisposed)
				dxBrush = NinjaTrader.Gui.DxExtensions.ToDxBrush(mediaBrush, RenderTarget);
			return dxBrush;
		}

		private void EnsureBufferCapacity(int calculationCount, int visibleCount)
		{
			int calculationCapacity = GetExpandedCapacity(volumeBuffer == null ? 0 : volumeBuffer.Length, calculationCount);
			if (volumeBuffer == null || volumeBuffer.Length < calculationCount)
			{
				volumeBuffer = new double[calculationCapacity];
				volumeValidBuffer = new bool[calculationCapacity];
				sortedVolumeBuffer = new double[calculationCapacity];
				volumeRankBuffer = new int[calculationCapacity];
				fenwickTree = new int[calculationCapacity + 1];
			}

			int visibleCapacity = GetExpandedCapacity(widthFactorBuffer == null ? 0 : widthFactorBuffer.Length, visibleCount);
			if (widthFactorBuffer == null || widthFactorBuffer.Length < visibleCount)
			{
				widthFactorBuffer = new double[visibleCapacity];
				centerBuffer = new float[visibleCapacity];
				paintWidthBuffer = new float[visibleCapacity];
			}
		}

		private int GetExpandedCapacity(int current, int required)
		{
			int capacity = Math.Max(16, current);
			while (capacity < required && capacity <= int.MaxValue / 2)
				capacity *= 2;
			return Math.Max(required, capacity);
		}

		private void RemoveInheritedProperty(string propertyName)
		{
			PropertyDescriptor property = Properties.Find(propertyName, true);
			if (property != null)
				Properties.Remove(property);
		}

		private void DisposeOwnedDxResources()
		{
			DisposeDxBrush(ref dojiBrushDX);
			DisposeDxBrush(ref outlineBrushDX);
			DisposeDxBrush(ref wickBrushDX);
		}

		private void DisposeDxBrush(ref DxBrush brush)
		{
			if (brush != null)
			{
				brush.Dispose();
				brush = null;
			}
		}

		private MediaBrush PrepareBrush(MediaBrush value, MediaBrush fallback)
		{
			MediaBrush prepared = value ?? fallback;
			if (prepared != null && prepared.CanFreeze && !prepared.IsFrozen)
				prepared.Freeze();
			return prepared;
		}

		private MediaBrush GetDefaultChartStrokeBrush()
		{
			try
			{
				System.Windows.Media.Pen pen = Application.Current?.TryFindResource("ChartControl.Stroke") as System.Windows.Media.Pen;
				return pen?.Brush ?? MediaBrushes.DimGray;
			}
			catch
			{
				return MediaBrushes.DimGray;
			}
		}

		private static double Clamp01(double value)
		{
			if (!IsFinite(value) || value <= 0)
				return 0;
			return value >= 1 ? 1 : value;
		}

		private static bool IsFinite(double value)
		{
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}

		#region Volume Width properties
		[Display(Name = "Normalization Mode", GroupName = "Volume Width", Order = 0)]
		[TypeConverter(typeof(OrcaVolumeNormalizationModeConverter))]
		public OrcaVolumeNormalizationMode NormalizationMode { get; set; }

		[Range(1, 10000)]
		[Display(Name = "Lookback Bars", GroupName = "Volume Width", Order = 1)]
		public int LookbackBars { get; set; }

		[Range(1.0, 100.0)]
		[Display(Name = "Minimum Width Percent", GroupName = "Volume Width", Order = 2)]
		public double MinimumWidthPercent { get; set; }

		[Range(1.0, 100.0)]
		[Display(Name = "Maximum Width Percent", GroupName = "Volume Width", Order = 3)]
		public double MaximumWidthPercent { get; set; }

		[Range(0.1, 5.0)]
		[Display(Name = "Width Sensitivity", GroupName = "Volume Width", Order = 4)]
		public double WidthSensitivity { get; set; }

		[Display(Name = "Scaling Method", GroupName = "Volume Width", Order = 5)]
		[TypeConverter(typeof(OrcaVolumeScalingMethodConverter))]
		public OrcaVolumeScalingMethod ScalingMethod { get; set; }

		[Display(Name = "Outlier Clipping", GroupName = "Volume Width", Order = 6)]
		public bool OutlierClipping { get; set; }

		[Range(50.0, 100.0)]
		[Display(Name = "Upper Percentile", GroupName = "Volume Width", Order = 7)]
		public double UpperPercentile { get; set; }

		[Range(0, 20)]
		[Display(Name = "Minimum Gap Pixels", GroupName = "Volume Width", Order = 8)]
		public int MinimumGapPixels { get; set; }
		#endregion

		#region Appearance properties
		[Display(Name = "Color Assignment", GroupName = "Appearance", Order = 0)]
		[TypeConverter(typeof(OrcaVolumeColorAssignmentConverter))]
		public OrcaVolumeColorAssignment ColorAssignment { get; set; }

		[XmlIgnore]
		[Display(Name = "Up body brush", GroupName = "Appearance", Order = 1)]
		public MediaBrush UpBodyBrush
		{
			get { return UpBrush; }
			set { UpBrush = PrepareBrush(value, Core.Globals.GeneralOptions.BrushUpPrimary ?? MediaBrushes.LimeGreen); }
		}

		[Browsable(false)]
		public string UpBodyBrushSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(UpBodyBrush); }
			set { UpBodyBrush = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Down body brush", GroupName = "Appearance", Order = 2)]
		public MediaBrush DownBodyBrush
		{
			get { return DownBrush; }
			set { DownBrush = PrepareBrush(value, Core.Globals.GeneralOptions.BrushDownPrimary ?? MediaBrushes.Red); }
		}

		[Browsable(false)]
		public string DownBodyBrushSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(DownBodyBrush); }
			set { DownBodyBrush = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Doji brush", GroupName = "Appearance", Order = 3)]
		public MediaBrush DojiBrush
		{
			get { return dojiBrush ?? MediaBrushes.DimGray; }
			set
			{
				DisposeDxBrush(ref dojiBrushDX);
				dojiBrush = PrepareBrush(value, MediaBrushes.DimGray);
			}
		}

		[Browsable(false)]
		public string DojiBrushSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(DojiBrush); }
			set { DojiBrush = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Outline brush", GroupName = "Appearance", Order = 4)]
		public MediaBrush OutlineBrush
		{
			get { return outlineBrush ?? MediaBrushes.DimGray; }
			set
			{
				DisposeDxBrush(ref outlineBrushDX);
				outlineBrush = PrepareBrush(value, MediaBrushes.DimGray);
			}
		}

		[Browsable(false)]
		public string OutlineBrushSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(OutlineBrush); }
			set { OutlineBrush = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Wick brush", GroupName = "Appearance", Order = 5)]
		public MediaBrush WickBrush
		{
			get { return wickBrush ?? MediaBrushes.DimGray; }
			set
			{
				DisposeDxBrush(ref wickBrushDX);
				wickBrush = PrepareBrush(value, MediaBrushes.DimGray);
			}
		}

		[Browsable(false)]
		public string WickBrushSerializable
		{
			get { return NinjaTrader.Gui.Serialize.BrushToString(WickBrush); }
			set { WickBrush = NinjaTrader.Gui.Serialize.StringToBrush(value); }
		}

		[Range(0, 100)]
		[Display(Name = "Body opacity", GroupName = "Appearance", Order = 6)]
		public int BodyOpacity { get; set; }

		[Range(1, 10)]
		[Display(Name = "Outline width", GroupName = "Appearance", Order = 7)]
		public int OutlineWidth { get; set; }

		[Range(1, 10)]
		[Display(Name = "Wick width", GroupName = "Appearance", Order = 8)]
		public int WickWidth { get; set; }

		[Display(Name = "Show outlines", GroupName = "Appearance", Order = 9)]
		public bool ShowOutlines { get; set; }

		[Display(Name = "Show wicks", GroupName = "Appearance", Order = 10)]
		public bool ShowWicks { get; set; }
		#endregion
	}
}

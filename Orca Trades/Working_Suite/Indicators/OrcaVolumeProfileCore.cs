using System;
using System.Collections.Generic;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript
{
	public struct OrcaVolumeProfileRow
	{
		public double LowPrice;
		public double HighPrice;
		public double Volume;
		public double UpVolume;
		public double DownVolume;

		public void Reset(double lowPrice, double highPrice)
		{
			LowPrice = lowPrice;
			HighPrice = highPrice;
			Volume = 0;
			UpVolume = 0;
			DownVolume = 0;
		}
	}

	public class OrcaVolumeProfileResult
	{
		public OrcaVolumeProfileRow[] Rows;
		public int RowCount;
		public double LowPrice;
		public double HighPrice;
		public double RowHeight;
		public double TotalVolume;
		public double MaxVolume;
		public double MaxDelta;
		public int PocIndex = -1;
		public int VahIndex = -1;
		public int ValIndex = -1;
		public double PocPrice = double.NaN;
		public double VahPrice = double.NaN;
		public double ValPrice = double.NaN;
		public bool HasValueArea;

		public bool HasProfile
		{
			get { return RowCount > 0 && MaxVolume > 0 && PocIndex >= 0; }
		}

		public void EnsureCapacity(int rowCount)
		{
			if (Rows == null || Rows.Length < rowCount)
				Rows = new OrcaVolumeProfileRow[rowCount];
		}

		public void Clear()
		{
			RowCount = 0;
			LowPrice = 0;
			HighPrice = 0;
			RowHeight = 0;
			TotalVolume = 0;
			MaxVolume = 0;
			MaxDelta = 0;
			PocIndex = -1;
			VahIndex = -1;
			ValIndex = -1;
			PocPrice = double.NaN;
			VahPrice = double.NaN;
			ValPrice = double.NaN;
			HasValueArea = false;
		}
	}

	public static class OrcaVolumeProfileCore
	{
		private const double Epsilon = 1E-10;
		private const double BucketEpsilon = 1E-06;

		public static bool BuildVisibleRangeFromBars(Bars bars, int fromIndex, int toIndex, int requestedRowCount, double valueAreaPercent, double tickSize, OrcaVolumeProfileResult result)
		{
			return BuildVisibleRangeFromBars(bars, fromIndex, toIndex, requestedRowCount, 1, false, valueAreaPercent, tickSize, result);
		}

		public static bool BuildVisibleRangeFromBars(Bars bars, int fromIndex, int toIndex, int requestedRowCount, int requestedTicksPerRow, bool useTicksPerRow, double valueAreaPercent, double tickSize, OrcaVolumeProfileResult result)
		{
			if (result == null)
				return false;

			result.Clear();
			if (bars == null || bars.Count <= 0)
				return false;

			int firstBar = Math.Max(0, fromIndex);
			int lastBar = Math.Min(toIndex, bars.Count - 1);
			if (firstBar > lastBar)
				return false;

			double minPrice = double.MaxValue;
			double maxPrice = double.MinValue;
			bool foundPrice = false;

			for (int barIndex = firstBar; barIndex <= lastBar; barIndex++)
			{
				double high = bars.GetHigh(barIndex);
				double low = bars.GetLow(barIndex);
				if (double.IsNaN(high) || double.IsNaN(low))
					continue;

				if (high < low)
				{
					double tmp = high;
					high = low;
					low = tmp;
				}

				if (low < minPrice) minPrice = low;
				if (high > maxPrice) maxPrice = high;
				foundPrice = true;
			}

			if (!foundPrice)
				return false;

			double safeTickSize = tickSize > 0 && !double.IsNaN(tickSize) && !double.IsInfinity(tickSize) ? tickSize : 0.01;
			int rowCount = Math.Max(1, requestedRowCount);
			double rowHeight = 0;
			double profileLow;
			double profileHigh;
			if (useTicksPerRow)
			{
				rowHeight = Math.Max(1, requestedTicksPerRow) * safeTickSize;
				profileLow = Math.Floor((minPrice / rowHeight) + BucketEpsilon) * rowHeight;
				profileHigh = Math.Ceiling((maxPrice / rowHeight) - BucketEpsilon) * rowHeight;
				if (profileHigh <= profileLow)
					profileHigh = profileLow + rowHeight;
				rowCount = Math.Max(1, (int)Math.Ceiling((profileHigh - profileLow) / rowHeight - BucketEpsilon));
			}
			else
			{
				profileLow = Math.Floor((minPrice / safeTickSize) + BucketEpsilon) * safeTickSize;
				profileHigh = Math.Ceiling((maxPrice / safeTickSize) - BucketEpsilon) * safeTickSize;
				if (profileHigh <= profileLow)
					profileHigh = profileLow + safeTickSize;
				rowHeight = (profileHigh - profileLow) / rowCount;
			}

			result.EnsureCapacity(rowCount);
			result.RowCount = rowCount;
			result.LowPrice = profileLow;
			result.HighPrice = profileHigh;
			result.RowHeight = rowHeight;
			if (result.RowHeight <= 0 || double.IsNaN(result.RowHeight) || double.IsInfinity(result.RowHeight))
			{
				result.Clear();
				return false;
			}

			for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
			{
				double rowLow = profileLow + (rowIndex * result.RowHeight);
				double rowHigh = rowIndex == rowCount - 1 ? profileHigh : rowLow + result.RowHeight;
				result.Rows[rowIndex].Reset(rowLow, rowHigh);
			}

			for (int barIndex = firstBar; barIndex <= lastBar; barIndex++)
			{
				double high = bars.GetHigh(barIndex);
				double low = bars.GetLow(barIndex);
				double open = bars.GetOpen(barIndex);
				double close = bars.GetClose(barIndex);
				double volume = bars.GetVolume(barIndex);

				if (volume <= 0 || double.IsNaN(volume) || double.IsInfinity(volume) || double.IsNaN(high) || double.IsNaN(low))
					continue;

				if (high < low)
				{
					double tmp = high;
					high = low;
					low = tmp;
				}

				double barLow = Math.Max(low, profileLow);
				double barHigh = Math.Min(high, profileHigh);
				bool upBar = double.IsNaN(open) || double.IsNaN(close) || close >= open;

				if (barHigh <= barLow + Epsilon)
				{
					AddVolumeToRow(result, GetRowIndex(result, close >= profileLow && close <= profileHigh ? close : barLow), volume, upBar);
					continue;
				}

				double barRange = barHigh - barLow;
				int firstRow = GetRowIndex(result, barLow);
				int lastRow = GetRowIndex(result, barHigh - (result.RowHeight * 1E-09));
				double distributed = 0;

				for (int rowIndex = firstRow; rowIndex <= lastRow; rowIndex++)
				{
					double overlapLow = Math.Max(barLow, result.Rows[rowIndex].LowPrice);
					double overlapHigh = Math.Min(barHigh, result.Rows[rowIndex].HighPrice);
					double overlap = overlapHigh - overlapLow;
					if (overlap <= Epsilon)
						continue;

					double rowVolume = volume * (overlap / barRange);
					AddVolumeToRow(result, rowIndex, rowVolume, upBar);
					distributed += rowVolume;
				}

				if (distributed <= Epsilon)
					AddVolumeToRow(result, GetRowIndex(result, (barLow + barHigh) * 0.5), volume, upBar);
			}

			FinalizeProfile(result, valueAreaPercent);
			return result.HasProfile;
		}

		public static bool BuildVisibleRangeFromPriceMaps(IList<Dictionary<double, long>> volumeByBar, IList<Dictionary<double, long>> upVolumeByBar, IList<Dictionary<double, long>> downVolumeByBar, int fromIndex, int toIndex, int requestedRowCount, int requestedTicksPerRow, bool useTicksPerRow, double valueAreaPercent, double tickSize, OrcaVolumeProfileResult result)
		{
			if (result == null)
				return false;

			result.Clear();
			if (volumeByBar == null || volumeByBar.Count <= 0)
				return false;

			int firstBar = Math.Max(0, fromIndex);
			int lastBar = Math.Min(toIndex, volumeByBar.Count - 1);
			if (firstBar > lastBar)
				return false;

			double minPrice = double.MaxValue;
			double maxPrice = double.MinValue;
			bool foundPrice = false;

			for (int barIndex = firstBar; barIndex <= lastBar; barIndex++)
			{
				Dictionary<double, long> volumeMap = GetMap(volumeByBar, barIndex);
				if (volumeMap == null || volumeMap.Count <= 0)
					continue;

				foreach (KeyValuePair<double, long> kvp in volumeMap)
				{
					if (kvp.Value <= 0 || double.IsNaN(kvp.Key) || double.IsInfinity(kvp.Key))
						continue;

					if (kvp.Key < minPrice) minPrice = kvp.Key;
					if (kvp.Key > maxPrice) maxPrice = kvp.Key;
					foundPrice = true;
				}
			}

			if (!foundPrice)
				return false;

			double safeTickSize = tickSize > 0 && !double.IsNaN(tickSize) && !double.IsInfinity(tickSize) ? tickSize : 0.01;
			int rowCount = Math.Max(1, requestedRowCount);
			double rowHeight;
			double profileLow;
			double profileHigh;

			if (useTicksPerRow)
			{
				rowHeight = Math.Max(1, requestedTicksPerRow) * safeTickSize;
				profileLow = Math.Floor((minPrice / rowHeight) + BucketEpsilon) * rowHeight;
				profileHigh = (Math.Floor((maxPrice / rowHeight) + BucketEpsilon) + 1) * rowHeight;
				if (profileHigh <= profileLow)
					profileHigh = profileLow + rowHeight;
				rowCount = Math.Max(1, (int)Math.Ceiling((profileHigh - profileLow) / rowHeight - BucketEpsilon));
			}
			else
			{
				profileLow = Math.Floor((minPrice / safeTickSize) + BucketEpsilon) * safeTickSize;
				profileHigh = (Math.Floor((maxPrice / safeTickSize) + BucketEpsilon) + 1) * safeTickSize;
				if (profileHigh <= profileLow)
					profileHigh = profileLow + safeTickSize;
				rowHeight = (profileHigh - profileLow) / rowCount;
			}

			result.EnsureCapacity(rowCount);
			result.RowCount = rowCount;
			result.LowPrice = profileLow;
			result.HighPrice = profileHigh;
			result.RowHeight = rowHeight;
			if (result.RowHeight <= 0 || double.IsNaN(result.RowHeight) || double.IsInfinity(result.RowHeight))
			{
				result.Clear();
				return false;
			}

			for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
			{
				double rowLow = profileLow + (rowIndex * result.RowHeight);
				double rowHigh = rowIndex == rowCount - 1 ? profileHigh : rowLow + result.RowHeight;
				result.Rows[rowIndex].Reset(rowLow, rowHigh);
			}

			for (int barIndex = firstBar; barIndex <= lastBar; barIndex++)
			{
				Dictionary<double, long> volumeMap = GetMap(volumeByBar, barIndex);
				if (volumeMap == null || volumeMap.Count <= 0)
					continue;

				Dictionary<double, long> upMap = GetMap(upVolumeByBar, barIndex);
				Dictionary<double, long> downMap = GetMap(downVolumeByBar, barIndex);
				foreach (KeyValuePair<double, long> kvp in volumeMap)
				{
					double price = kvp.Key;
					double volume = kvp.Value;
					if (volume <= 0 || double.IsNaN(price) || double.IsInfinity(price))
						continue;

					double upVolume = 0;
					double downVolume = 0;
					long mapValue;
					if (upMap != null && upMap.TryGetValue(price, out mapValue))
						upVolume = mapValue;
					if (downMap != null && downMap.TryGetValue(price, out mapValue))
						downVolume = mapValue;

					AddExactVolumeToRow(result, GetRowIndex(result, price), volume, upVolume, downVolume);
				}
			}

			FinalizeProfile(result, valueAreaPercent);
			return result.HasProfile;
		}

		public static bool TryCalculateValueArea(OrcaVolumeProfileResult result, double valueAreaPercent)
		{
			if (result == null || result.RowCount <= 0 || result.PocIndex < 0 || result.TotalVolume <= 0)
				return false;

			double targetVolume = result.TotalVolume * (Clamp(valueAreaPercent, 0, 100) / 100.0);
			int lowIndex = result.PocIndex;
			int highIndex = result.PocIndex;
			double accumulated = result.Rows[result.PocIndex].Volume;

			while (accumulated < targetVolume)
			{
				int nextLow = FindNextVolumeRow(result, lowIndex - 1, -1);
				int nextHigh = FindNextVolumeRow(result, highIndex + 1, 1);
				if (nextLow < 0 && nextHigh < 0)
					break;

				double lowVolume = nextLow >= 0 ? result.Rows[nextLow].Volume : 0;
				double highVolume = nextHigh >= 0 ? result.Rows[nextHigh].Volume : 0;

				if (nextLow < 0)
				{
					highIndex = nextHigh;
					accumulated += highVolume;
				}
				else if (nextHigh < 0)
				{
					lowIndex = nextLow;
					accumulated += lowVolume;
				}
				else if (highVolume >= lowVolume)
				{
					highIndex = nextHigh;
					accumulated += highVolume;
				}
				else
				{
					lowIndex = nextLow;
					accumulated += lowVolume;
				}
			}

			result.ValIndex = lowIndex;
			result.VahIndex = highIndex;
			result.ValPrice = result.Rows[lowIndex].LowPrice;
			result.VahPrice = result.Rows[highIndex].HighPrice;
			result.HasValueArea = true;
			return true;
		}

		public static bool TryCalculateValueArea(IDictionary<double, long> volumeByPrice, double pocPrice, double valueAreaPercent, out double vahPrice, out double valPrice)
		{
			vahPrice = pocPrice;
			valPrice = pocPrice;
			if (volumeByPrice == null || volumeByPrice.Count <= 0 || !volumeByPrice.ContainsKey(pocPrice))
				return false;

			List<double> prices = new List<double>(volumeByPrice.Keys);
			prices.Sort();

			long totalVolume = 0;
			foreach (long volume in volumeByPrice.Values)
				totalVolume += volume;
			if (totalVolume <= 0)
				return false;

			double targetVolume = totalVolume * (Clamp(valueAreaPercent, 0, 100) / 100.0);
			int lowIndex = prices.IndexOf(pocPrice);
			if (lowIndex < 0)
				return false;

			int highIndex = lowIndex;
			long accumulated = volumeByPrice[pocPrice];

			while (accumulated < targetVolume && (lowIndex > 0 || highIndex < prices.Count - 1))
			{
				long lowVolume = lowIndex > 0 ? volumeByPrice[prices[lowIndex - 1]] : 0;
				long highVolume = highIndex < prices.Count - 1 ? volumeByPrice[prices[highIndex + 1]] : 0;

				if (lowIndex <= 0)
				{
					highIndex++;
					accumulated += highVolume;
				}
				else if (highIndex >= prices.Count - 1)
				{
					lowIndex--;
					accumulated += lowVolume;
				}
				else if (highVolume >= lowVolume)
				{
					highIndex++;
					accumulated += highVolume;
				}
				else
				{
					lowIndex--;
					accumulated += lowVolume;
				}
			}

			valPrice = prices[lowIndex];
			vahPrice = prices[highIndex];
			return true;
		}

		private static void AddVolumeToRow(OrcaVolumeProfileResult result, int rowIndex, double volume, bool upBar)
		{
			if (rowIndex < 0 || rowIndex >= result.RowCount || volume <= 0)
				return;

			result.Rows[rowIndex].Volume += volume;
			if (upBar)
				result.Rows[rowIndex].UpVolume += volume;
			else
				result.Rows[rowIndex].DownVolume += volume;
			result.TotalVolume += volume;
		}

		private static void AddExactVolumeToRow(OrcaVolumeProfileResult result, int rowIndex, double volume, double upVolume, double downVolume)
		{
			if (rowIndex < 0 || rowIndex >= result.RowCount || volume <= 0)
				return;

			upVolume = Math.Max(0, upVolume);
			downVolume = Math.Max(0, downVolume);
			double classifiedVolume = upVolume + downVolume;

			if (classifiedVolume > Epsilon && classifiedVolume < volume - Epsilon)
			{
				double remainder = volume - classifiedVolume;
				if (upVolume >= downVolume)
					upVolume += remainder;
				else
					downVolume += remainder;
			}
			else if (classifiedVolume > volume + Epsilon)
			{
				double scale = volume / classifiedVolume;
				upVolume *= scale;
				downVolume *= scale;
			}

			result.Rows[rowIndex].Volume += volume;
			result.Rows[rowIndex].UpVolume += upVolume;
			result.Rows[rowIndex].DownVolume += downVolume;
			result.TotalVolume += volume;
		}

		private static Dictionary<double, long> GetMap(IList<Dictionary<double, long>> maps, int index)
		{
			if (maps == null || index < 0 || index >= maps.Count)
				return null;
			return maps[index];
		}

		private static int GetRowIndex(OrcaVolumeProfileResult result, double price)
		{
			if (price <= result.LowPrice)
				return 0;
			if (price >= result.HighPrice)
				return result.RowCount - 1;

			int index = (int)((price - result.LowPrice) / result.RowHeight);
			if (index < 0) return 0;
			if (index >= result.RowCount) return result.RowCount - 1;
			return index;
		}

		private static void FinalizeProfile(OrcaVolumeProfileResult result, double valueAreaPercent)
		{
			result.MaxVolume = 0;
			result.MaxDelta = 0;
			result.PocIndex = -1;
			for (int rowIndex = 0; rowIndex < result.RowCount; rowIndex++)
			{
				double volume = result.Rows[rowIndex].Volume;
				double delta = Math.Abs(result.Rows[rowIndex].UpVolume - result.Rows[rowIndex].DownVolume);
				if (delta > result.MaxDelta)
					result.MaxDelta = delta;
				if (volume > result.MaxVolume)
				{
					result.MaxVolume = volume;
					result.PocIndex = rowIndex;
				}
			}

			if (result.PocIndex < 0 || result.MaxVolume <= 0)
				return;

			result.PocPrice = (result.Rows[result.PocIndex].LowPrice + result.Rows[result.PocIndex].HighPrice) * 0.5;
			TryCalculateValueArea(result, valueAreaPercent);
		}

		private static int FindNextVolumeRow(OrcaVolumeProfileResult result, int startIndex, int direction)
		{
			for (int index = startIndex; index >= 0 && index < result.RowCount; index += direction)
			{
				if (result.Rows[index].Volume > 0)
					return index;
			}
			return -1;
		}

		private static double Clamp(double value, double min, double max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;

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

	public sealed class OrcaProfileDataSnapshot
	{
		public List<Dictionary<double, long>> VolumeByBar;
		public List<Dictionary<double, long>> UpVolumeByBar;
		public List<Dictionary<double, long>> DownVolumeByBar;
		public int FromIndex;
		public int ToIndex;
		public int Revision;
		public string SourceName;
		public bool HasAnyVolume;
	}

	public sealed class OrcaProfileDataSource
	{
		public Guid SourceId;
		public string Key;
		public string SourceName;
		public object SyncRoot;
		public IList<Dictionary<double, long>> VolumeByBar;
		public IList<Dictionary<double, long>> UpVolumeByBar;
		public IList<Dictionary<double, long>> DownVolumeByBar;
		public Func<int> RevisionProvider;
		public Func<DateTime> LastUpdatedUtcProvider;
		public Func<int> CoverageProvider;
	}

	public enum OrcaOrderFlowSourceMode
	{
		Internal,
		SharedProvider,
		SharedHistoricalInternalRealtime
	}

	public sealed class OrcaOrderFlowBucket
	{
		public DateTime Time;
		public double Price = double.NaN;
		public long Volume;
		public long AskVolume;
		public long BidVolume;
		public long Delta;
		public long MaxDelta;
		public long MinDelta;
		public long BidAskClassifiedVolume;
		public long FallbackClassifiedVolume;
		public long UnclassifiedVolume;
		public long RunningDelta;

		public void Add(double price, long volume, long signedVolume, bool usedBidAsk, bool usedFallback)
		{
			if (volume <= 0)
				return;

			if (!double.IsNaN(price) && !double.IsInfinity(price))
			{
				if (double.IsNaN(Price))
					Price = price;
				else if (Math.Abs(Price - price) > 1E-10)
					Price = double.NaN;
			}

			Volume += volume;
			if (signedVolume > 0)
				AskVolume += volume;
			else if (signedVolume < 0)
				BidVolume += volume;

			if (usedBidAsk)
				BidAskClassifiedVolume += volume;
			else if (usedFallback)
				FallbackClassifiedVolume += volume;
			else
				UnclassifiedVolume += volume;

			RunningDelta += signedVolume;
			Delta += signedVolume;
			if (RunningDelta > MaxDelta)
				MaxDelta = RunningDelta;
			if (RunningDelta < MinDelta)
				MinDelta = RunningDelta;
		}

		public OrcaOrderFlowBucket Clone()
		{
			return new OrcaOrderFlowBucket
			{
				Time = Time,
				Price = Price,
				Volume = Volume,
				AskVolume = AskVolume,
				BidVolume = BidVolume,
				Delta = Delta,
				MaxDelta = MaxDelta,
				MinDelta = MinDelta,
				BidAskClassifiedVolume = BidAskClassifiedVolume,
				FallbackClassifiedVolume = FallbackClassifiedVolume,
				UnclassifiedVolume = UnclassifiedVolume,
				RunningDelta = RunningDelta
			};
		}
	}

	public sealed class OrcaOrderFlowDataSnapshot
	{
		public List<OrcaOrderFlowBucket> Buckets;
		public DateTime FromTime;
		public DateTime ToTime;
		public int Revision;
		public string SourceName;
		public int BucketSeconds;
		public bool HasAnyOrderFlow;
		public long Volume;
		public long BidAskClassifiedVolume;
		public long FallbackClassifiedVolume;
		public long UnclassifiedVolume;
	}

	public sealed class OrcaOrderFlowDataSource
	{
		public Guid SourceId;
		public string Key;
		public string SourceName;
		public object SyncRoot;
		public IList<OrcaOrderFlowBucket> Buckets;
		public Func<int> RevisionProvider;
		public Func<DateTime> LastUpdatedUtcProvider;
		public Func<int> BucketSecondsProvider;
	}

	public static class OrcaProfileDataCache
	{
		private static readonly object CacheSync = new object();
		private static readonly Dictionary<string, List<OrcaProfileDataSource>> SourcesByKey = new Dictionary<string, List<OrcaProfileDataSource>>();
		private static readonly Dictionary<string, List<OrcaOrderFlowDataSource>> OrderFlowSourcesByKey = new Dictionary<string, List<OrcaOrderFlowDataSource>>();

		public static string BuildKey(Bars bars)
		{
			if (bars == null)
				return string.Empty;

			string instrumentName = bars.Instrument != null ? bars.Instrument.FullName : string.Empty;
			string barsPeriod = bars.BarsPeriod != null ? bars.BarsPeriod.ToString() : string.Empty;
			return instrumentName + "|" + barsPeriod;
		}

		public static string BuildKey(Bars bars, ChartControl chartControl)
		{
			string baseKey = BuildKey(bars);
			if (string.IsNullOrEmpty(baseKey) || chartControl == null)
				return baseKey;

			return baseKey + "|chart:" + chartControl.GetHashCode().ToString();
		}

		public static string BuildInstrumentKey(Bars bars)
		{
			if (bars == null)
				return string.Empty;

			return bars.Instrument != null ? bars.Instrument.FullName : string.Empty;
		}

		public static void RegisterSource(OrcaProfileDataSource source)
		{
			if (source == null || source.SourceId == Guid.Empty || string.IsNullOrEmpty(source.Key))
				return;

			lock (CacheSync)
			{
				List<OrcaProfileDataSource> sources;
				if (!SourcesByKey.TryGetValue(source.Key, out sources))
				{
					sources = new List<OrcaProfileDataSource>();
					SourcesByKey[source.Key] = sources;
				}

				for (int index = sources.Count - 1; index >= 0; index--)
				{
					if (sources[index] != null && sources[index].SourceId == source.SourceId)
						sources.RemoveAt(index);
				}

				sources.Add(source);
			}
		}

		public static void RegisterOrderFlowSource(OrcaOrderFlowDataSource source)
		{
			if (source == null || source.SourceId == Guid.Empty || string.IsNullOrEmpty(source.Key))
				return;

			lock (CacheSync)
			{
				List<OrcaOrderFlowDataSource> sources;
				if (!OrderFlowSourcesByKey.TryGetValue(source.Key, out sources))
				{
					sources = new List<OrcaOrderFlowDataSource>();
					OrderFlowSourcesByKey[source.Key] = sources;
				}

				for (int index = sources.Count - 1; index >= 0; index--)
				{
					if (sources[index] != null && sources[index].SourceId == source.SourceId)
						sources.RemoveAt(index);
				}

				sources.Add(source);
			}
		}

		public static void UnregisterSource(Guid sourceId)
		{
			if (sourceId == Guid.Empty)
				return;

			lock (CacheSync)
			{
				List<string> emptyKeys = null;
				foreach (KeyValuePair<string, List<OrcaProfileDataSource>> kvp in SourcesByKey)
				{
					List<OrcaProfileDataSource> sources = kvp.Value;
					for (int index = sources.Count - 1; index >= 0; index--)
					{
						if (sources[index] == null || sources[index].SourceId == sourceId)
							sources.RemoveAt(index);
					}

					if (sources.Count == 0)
					{
						if (emptyKeys == null)
							emptyKeys = new List<string>();
						emptyKeys.Add(kvp.Key);
					}
				}

				if (emptyKeys != null)
				{
					foreach (string key in emptyKeys)
						SourcesByKey.Remove(key);
				}

				emptyKeys = null;
				foreach (KeyValuePair<string, List<OrcaOrderFlowDataSource>> kvp in OrderFlowSourcesByKey)
				{
					List<OrcaOrderFlowDataSource> sources = kvp.Value;
					for (int index = sources.Count - 1; index >= 0; index--)
					{
						if (sources[index] == null || sources[index].SourceId == sourceId)
							sources.RemoveAt(index);
					}

					if (sources.Count == 0)
					{
						if (emptyKeys == null)
							emptyKeys = new List<string>();
						emptyKeys.Add(kvp.Key);
					}
				}

				if (emptyKeys != null)
				{
					foreach (string key in emptyKeys)
						OrderFlowSourcesByKey.Remove(key);
				}
			}
		}

		public static bool TrySnapshot(string key, int fromIndex, int toIndex, out OrcaProfileDataSnapshot snapshot)
		{
			snapshot = null;
			OrcaProfileDataSource source = GetBestSource(key);
			if (source == null || source.VolumeByBar == null)
				return false;

			object syncRoot = source.SyncRoot ?? source;
			lock (syncRoot)
			{
				int firstBar = Math.Max(0, fromIndex);
				int lastBar = Math.Max(firstBar, toIndex);
				int count = Math.Max(0, lastBar - firstBar + 1);
				OrcaProfileDataSnapshot working = new OrcaProfileDataSnapshot
				{
					FromIndex = firstBar,
					ToIndex = count - 1,
					Revision = GetRevision(source),
					SourceName = source.SourceName,
					VolumeByBar = new List<Dictionary<double, long>>(count),
					UpVolumeByBar = new List<Dictionary<double, long>>(count),
					DownVolumeByBar = new List<Dictionary<double, long>>(count)
				};

				for (int barIndex = firstBar; barIndex <= lastBar; barIndex++)
				{
					Dictionary<double, long> volumeMap = CopyMapAt(source.VolumeByBar, barIndex);
					if (volumeMap != null && volumeMap.Count > 0)
						working.HasAnyVolume = true;

					working.VolumeByBar.Add(volumeMap);
					working.UpVolumeByBar.Add(CopyMapAt(source.UpVolumeByBar, barIndex));
					working.DownVolumeByBar.Add(CopyMapAt(source.DownVolumeByBar, barIndex));
				}

				snapshot = working;
				return working.HasAnyVolume;
			}
		}

		public static bool TrySnapshotOrderFlow(string key, DateTime fromTime, DateTime toTime, out OrcaOrderFlowDataSnapshot snapshot)
		{
			snapshot = null;
			OrcaOrderFlowDataSource source = GetBestOrderFlowSource(key);
			if (source == null || source.Buckets == null)
				return false;

			if (toTime < fromTime)
			{
				DateTime tmp = fromTime;
				fromTime = toTime;
				toTime = tmp;
			}

			object syncRoot = source.SyncRoot ?? source;
			lock (syncRoot)
			{
				OrcaOrderFlowDataSnapshot working = new OrcaOrderFlowDataSnapshot
				{
					FromTime = fromTime,
					ToTime = toTime,
					Revision = GetRevision(source),
					SourceName = source.SourceName,
					BucketSeconds = GetBucketSeconds(source),
					Buckets = new List<OrcaOrderFlowBucket>()
				};

				for (int index = 0; index < source.Buckets.Count; index++)
				{
					OrcaOrderFlowBucket bucket = source.Buckets[index];
					if (bucket == null || bucket.Time < fromTime || bucket.Time > toTime)
						continue;

					working.Buckets.Add(bucket.Clone());
					working.Volume += bucket.Volume;
					working.BidAskClassifiedVolume += bucket.BidAskClassifiedVolume;
					working.FallbackClassifiedVolume += bucket.FallbackClassifiedVolume;
					working.UnclassifiedVolume += bucket.UnclassifiedVolume;
					if (bucket.Volume > 0 || bucket.Delta != 0)
						working.HasAnyOrderFlow = true;
				}

				snapshot = working;
				return working.HasAnyOrderFlow;
			}
		}

		public static bool TrySnapshotOrderFlowSinceIndex(string key, Guid expectedSourceId, int startIndex, DateTime fromTime, out OrcaOrderFlowDataSnapshot snapshot, out Guid sourceId, out int nextIndex, out int totalBucketCount)
		{
			return TrySnapshotOrderFlowSinceIndex(key, expectedSourceId, startIndex, fromTime, int.MaxValue, out snapshot, out sourceId, out nextIndex, out totalBucketCount);
		}

		public static bool TrySnapshotOrderFlowSinceIndex(string key, Guid expectedSourceId, int startIndex, DateTime fromTime, int maxBuckets, out OrcaOrderFlowDataSnapshot snapshot, out Guid sourceId, out int nextIndex, out int totalBucketCount)
		{
			snapshot = null;
			sourceId = Guid.Empty;
			nextIndex = 0;
			totalBucketCount = 0;

			OrcaOrderFlowDataSource source = GetBestOrderFlowSource(key);
			if (source == null || source.Buckets == null)
				return false;

			object syncRoot = source.SyncRoot ?? source;
			lock (syncRoot)
			{
				sourceId = source.SourceId;
				totalBucketCount = source.Buckets.Count;
				int firstIndex = startIndex;
				if (expectedSourceId == Guid.Empty || expectedSourceId != source.SourceId || firstIndex < 0 || firstIndex > totalBucketCount)
					firstIndex = FindFirstBucketIndexAtOrAfter(source.Buckets, fromTime);
				if (firstIndex < 0)
					firstIndex = 0;
				if (firstIndex > totalBucketCount)
					firstIndex = totalBucketCount;

				int batchLimit = maxBuckets <= 0 ? int.MaxValue : maxBuckets;
				int endIndex = Math.Min(totalBucketCount, firstIndex + batchLimit);
				OrcaOrderFlowDataSnapshot working = new OrcaOrderFlowDataSnapshot
				{
					FromTime = fromTime,
					ToTime = DateTime.MinValue,
					Revision = GetRevision(source),
					SourceName = source.SourceName,
					BucketSeconds = GetBucketSeconds(source),
					Buckets = new List<OrcaOrderFlowBucket>()
				};

				for (int index = firstIndex; index < endIndex; index++)
				{
					OrcaOrderFlowBucket bucket = source.Buckets[index];
					if (bucket == null)
						continue;

					working.Buckets.Add(bucket.Clone());
					working.Volume += bucket.Volume;
					working.BidAskClassifiedVolume += bucket.BidAskClassifiedVolume;
					working.FallbackClassifiedVolume += bucket.FallbackClassifiedVolume;
					working.UnclassifiedVolume += bucket.UnclassifiedVolume;
					working.ToTime = bucket.Time;
					if (bucket.Volume > 0 || bucket.Delta != 0)
						working.HasAnyOrderFlow = true;
				}

				nextIndex = endIndex;
				snapshot = working;
				return working.HasAnyOrderFlow;
			}
		}

		public static bool TrySnapshotOrderFlowPriceMaps(string key, DateTime fromTime, DateTime toTime, out OrcaProfileDataSnapshot snapshot, out int bucketSeconds, out string sourceName)
		{
			snapshot = null;
			bucketSeconds = -1;
			sourceName = null;

			OrcaOrderFlowDataSnapshot orderFlowSnapshot;
			if (!TrySnapshotOrderFlow(key, fromTime, toTime, out orderFlowSnapshot))
			{
				OrcaOrderFlowDataSource source = GetBestOrderFlowSource(key);
				if (source != null)
				{
					bucketSeconds = GetBucketSeconds(source);
					sourceName = source.SourceName;
				}
				return false;
			}

			bucketSeconds = orderFlowSnapshot.BucketSeconds;
			sourceName = orderFlowSnapshot.SourceName;
			if (bucketSeconds != 0 || orderFlowSnapshot.Buckets == null || orderFlowSnapshot.Buckets.Count == 0)
				return false;

			Dictionary<double, long> volumeMap = new Dictionary<double, long>();
			Dictionary<double, long> upVolumeMap = new Dictionary<double, long>();
			Dictionary<double, long> downVolumeMap = new Dictionary<double, long>();

			for (int index = 0; index < orderFlowSnapshot.Buckets.Count; index++)
			{
				OrcaOrderFlowBucket bucket = orderFlowSnapshot.Buckets[index];
				if (bucket == null || bucket.Volume <= 0 || double.IsNaN(bucket.Price) || double.IsInfinity(bucket.Price))
					continue;

				AddToMap(volumeMap, bucket.Price, bucket.Volume);

				long askVolume = bucket.AskVolume;
				long bidVolume = bucket.BidVolume;
				if (askVolume <= 0 && bidVolume <= 0)
				{
					if (bucket.Delta > 0)
						askVolume = bucket.Volume;
					else if (bucket.Delta < 0)
						bidVolume = bucket.Volume;
				}

				if (askVolume > 0)
					AddToMap(upVolumeMap, bucket.Price, askVolume);
				if (bidVolume > 0)
					AddToMap(downVolumeMap, bucket.Price, bidVolume);
			}

			OrcaProfileDataSnapshot working = new OrcaProfileDataSnapshot
			{
				FromIndex = 0,
				ToIndex = 0,
				Revision = orderFlowSnapshot.Revision,
				SourceName = orderFlowSnapshot.SourceName,
				VolumeByBar = new List<Dictionary<double, long>>(1),
				UpVolumeByBar = new List<Dictionary<double, long>>(1),
				DownVolumeByBar = new List<Dictionary<double, long>>(1),
				HasAnyVolume = volumeMap.Count > 0
			};
			working.VolumeByBar.Add(volumeMap);
			working.UpVolumeByBar.Add(upVolumeMap);
			working.DownVolumeByBar.Add(downVolumeMap);

			snapshot = working;
			return working.HasAnyVolume;
		}

		public static bool HasSource(string key)
		{
			return GetBestSource(key) != null;
		}

		public static string DescribeProfileSources()
		{
			lock (CacheSync)
			{
				if (SourcesByKey.Count == 0)
					return "none";

				StringBuilder builder = new StringBuilder();
				foreach (KeyValuePair<string, List<OrcaProfileDataSource>> kvp in SourcesByKey)
				{
					if (builder.Length > 0)
						builder.Append("; ");

					builder.Append(kvp.Key);
					builder.Append("=");

					List<OrcaProfileDataSource> sources = kvp.Value;
					if (sources == null || sources.Count == 0)
					{
						builder.Append("0 sources");
						continue;
					}

					builder.Append(sources.Count);
					builder.Append(sources.Count == 1 ? " source" : " sources");

					OrcaProfileDataSource source = GetBestSource(kvp.Key);
					if (source != null)
					{
						builder.Append(" best=");
						builder.Append(source.SourceName ?? "unnamed");
						builder.Append(" rev=");
						builder.Append(GetRevision(source));
						builder.Append(" coverage=");
						builder.Append(GetProfileCoverage(source));
					}
				}

				return builder.ToString();
			}
		}

		public static bool HasOrderFlowSource(string key)
		{
			return GetBestOrderFlowSource(key) != null;
		}

		public static bool TryGetOrderFlowStatus(string key, out int revision, out DateTime lastUpdatedUtc, out string sourceName)
		{
			revision = 0;
			lastUpdatedUtc = DateTime.MinValue;
			sourceName = null;

			OrcaOrderFlowDataSource source = GetBestOrderFlowSource(key);
			if (source == null)
				return false;

			revision = GetRevision(source);
			lastUpdatedUtc = GetLastUpdatedUtc(source);
			sourceName = source.SourceName;
			return true;
		}

		public static bool TryGetOrderFlowStatus(string key, out int revision, out DateTime lastUpdatedUtc, out string sourceName, out int bucketSeconds, out int bucketCount)
		{
			revision = 0;
			lastUpdatedUtc = DateTime.MinValue;
			sourceName = null;
			bucketSeconds = -1;
			bucketCount = 0;

			OrcaOrderFlowDataSource source = GetBestOrderFlowSource(key);
			if (source == null)
				return false;

			revision = GetRevision(source);
			lastUpdatedUtc = GetLastUpdatedUtc(source);
			sourceName = source.SourceName;
			bucketSeconds = GetBucketSeconds(source);
			bucketCount = GetBucketCount(source);
			return true;
		}

		public static bool TryGetOrderFlowStatus(string key, out int revision, out DateTime lastUpdatedUtc, out string sourceName, out int bucketSeconds, out int bucketCount, out DateTime firstBucketTime, out DateTime lastBucketTime)
		{
			revision = 0;
			lastUpdatedUtc = DateTime.MinValue;
			sourceName = null;
			bucketSeconds = -1;
			bucketCount = 0;
			firstBucketTime = DateTime.MinValue;
			lastBucketTime = DateTime.MinValue;

			OrcaOrderFlowDataSource source = GetBestOrderFlowSource(key);
			if (source == null)
				return false;

			revision = GetRevision(source);
			lastUpdatedUtc = GetLastUpdatedUtc(source);
			sourceName = source.SourceName;
			bucketSeconds = GetBucketSeconds(source);
			bucketCount = GetBucketCount(source);
			firstBucketTime = GetFirstBucketTime(source);
			lastBucketTime = GetLastBucketTime(source);
			return true;
		}

		public static string DescribeOrderFlowSources()
		{
			lock (CacheSync)
			{
				if (OrderFlowSourcesByKey.Count == 0)
					return "none";

				StringBuilder builder = new StringBuilder();
				foreach (KeyValuePair<string, List<OrcaOrderFlowDataSource>> kvp in OrderFlowSourcesByKey)
				{
					if (builder.Length > 0)
						builder.Append("; ");

					builder.Append(kvp.Key);
					builder.Append("=");

					List<OrcaOrderFlowDataSource> sources = kvp.Value;
					if (sources == null || sources.Count == 0)
					{
						builder.Append("0 sources");
						continue;
					}

					builder.Append(sources.Count);
					builder.Append(sources.Count == 1 ? " source" : " sources");

					OrcaOrderFlowDataSource source = GetBestOrderFlowSource(kvp.Key);
					if (source != null)
					{
						builder.Append(" best=");
						builder.Append(source.SourceName ?? "unnamed");
						builder.Append(" rev=");
						builder.Append(GetRevision(source));
						builder.Append(" buckets=");
						builder.Append(source.Buckets != null ? source.Buckets.Count : 0);
						builder.Append(" bucketSeconds=");
						builder.Append(GetBucketSeconds(source));
						if (source.Buckets != null && source.Buckets.Count > 0)
						{
							OrcaOrderFlowBucket lastBucket = source.Buckets[source.Buckets.Count - 1];
							if (lastBucket != null)
							{
								builder.Append(" lastBucket=");
								builder.Append(lastBucket.Time.ToString("HH:mm:ss"));
							}
						}
					}
				}

				return builder.ToString();
			}
		}

		private static OrcaProfileDataSource GetBestSource(string key)
		{
			if (string.IsNullOrEmpty(key))
				return null;

			lock (CacheSync)
			{
				List<OrcaProfileDataSource> sources;
				if (!SourcesByKey.TryGetValue(key, out sources) || sources == null || sources.Count == 0)
					return null;

				OrcaProfileDataSource best = null;
				for (int index = sources.Count - 1; index >= 0; index--)
				{
					OrcaProfileDataSource source = sources[index];
					if (source == null || source.VolumeByBar == null)
					{
						sources.RemoveAt(index);
						continue;
					}

					if (IsBetterProfileDataSource(source, best))
						best = source;
				}

				return best;
			}
		}

		private static bool IsBetterProfileDataSource(OrcaProfileDataSource candidate, OrcaProfileDataSource currentBest)
		{
			if (candidate == null || candidate.VolumeByBar == null)
				return false;
			if (currentBest == null || currentBest.VolumeByBar == null)
				return true;

			int candidateCoverage = GetProfileCoverage(candidate);
			int bestCoverage = GetProfileCoverage(currentBest);
			if (candidateCoverage >= bestCoverage + 10)
				return true;
			if (bestCoverage >= candidateCoverage + 10)
				return false;

			DateTime candidateUpdated = GetLastUpdatedUtc(candidate);
			DateTime bestUpdated = GetLastUpdatedUtc(currentBest);
			if (candidateUpdated != bestUpdated)
				return candidateUpdated > bestUpdated;

			return GetRevision(candidate) >= GetRevision(currentBest);
		}

		private static OrcaOrderFlowDataSource GetBestOrderFlowSource(string key)
		{
			if (string.IsNullOrEmpty(key))
				return null;

			lock (CacheSync)
			{
				List<OrcaOrderFlowDataSource> sources;
				if (!OrderFlowSourcesByKey.TryGetValue(key, out sources) || sources == null || sources.Count == 0)
					return null;

				OrcaOrderFlowDataSource best = null;
				for (int index = sources.Count - 1; index >= 0; index--)
				{
					OrcaOrderFlowDataSource source = sources[index];
					if (source == null || source.Buckets == null)
					{
						sources.RemoveAt(index);
						continue;
					}

					if (IsBetterOrderFlowSource(source, best))
						best = source;
				}

				return best;
			}
		}

		private static bool IsBetterOrderFlowSource(OrcaOrderFlowDataSource candidate, OrcaOrderFlowDataSource currentBest)
		{
			if (candidate == null || candidate.Buckets == null)
				return false;
			if (currentBest == null || currentBest.Buckets == null)
				return true;

			int candidateBuckets = GetBucketCount(candidate);
			int bestBuckets = GetBucketCount(currentBest);

			// A freshly-added non-Tick-Replay provider can update more recently but only contain live
			// ticks. Prefer the provider with substantially richer history so consumers do not lose
			// backfill when another chart accidentally registers a weaker same-instrument source.
			if (candidateBuckets >= bestBuckets + 1000)
				return true;
			if (bestBuckets >= candidateBuckets + 1000)
				return false;

			DateTime candidateFirst = GetFirstBucketTime(candidate);
			DateTime bestFirst = GetFirstBucketTime(currentBest);
			if (candidateFirst != DateTime.MinValue && bestFirst != DateTime.MinValue)
			{
				if (candidateFirst < bestFirst)
					return true;
				if (bestFirst < candidateFirst)
					return false;
			}

			DateTime candidateLast = GetLastBucketTime(candidate);
			DateTime bestLast = GetLastBucketTime(currentBest);
			if (candidateLast != bestLast)
				return candidateLast > bestLast;

			DateTime candidateUpdated = GetLastUpdatedUtc(candidate);
			DateTime bestUpdated = GetLastUpdatedUtc(currentBest);
			if (candidateUpdated != bestUpdated)
				return candidateUpdated > bestUpdated;

			return GetRevision(candidate) >= GetRevision(currentBest);
		}

		private static int GetBucketCount(OrcaOrderFlowDataSource source)
		{
			return source != null && source.Buckets != null ? source.Buckets.Count : 0;
		}

		private static DateTime GetFirstBucketTime(OrcaOrderFlowDataSource source)
		{
			if (source == null || source.Buckets == null || source.Buckets.Count == 0 || source.Buckets[0] == null)
				return DateTime.MinValue;
			return source.Buckets[0].Time;
		}

		private static DateTime GetLastBucketTime(OrcaOrderFlowDataSource source)
		{
			if (source == null || source.Buckets == null || source.Buckets.Count == 0)
				return DateTime.MinValue;

			OrcaOrderFlowBucket bucket = source.Buckets[source.Buckets.Count - 1];
			return bucket != null ? bucket.Time : DateTime.MinValue;
		}

		private static int FindFirstBucketIndexAtOrAfter(IList<OrcaOrderFlowBucket> buckets, DateTime fromTime)
		{
			if (buckets == null || buckets.Count == 0 || fromTime == DateTime.MinValue)
				return 0;

			int low = 0;
			int high = buckets.Count - 1;
			int result = buckets.Count;
			while (low <= high)
			{
				int mid = low + ((high - low) / 2);
				OrcaOrderFlowBucket bucket = buckets[mid];
				DateTime time = bucket != null ? bucket.Time : DateTime.MinValue;
				if (time >= fromTime)
				{
					result = mid;
					high = mid - 1;
				}
				else
				{
					low = mid + 1;
				}
			}

			return result;
		}

		private static int GetRevision(OrcaProfileDataSource source)
		{
			if (source == null || source.RevisionProvider == null)
				return 0;
			return source.RevisionProvider();
		}

		private static int GetProfileCoverage(OrcaProfileDataSource source)
		{
			if (source == null)
				return 0;

			if (source.CoverageProvider != null)
			{
				try
				{
					return Math.Max(0, source.CoverageProvider());
				}
				catch { }
			}

			IList<Dictionary<double, long>> maps = source.VolumeByBar;
			if (maps == null)
				return 0;

			int coverage = 0;
			for (int index = 0; index < maps.Count; index++)
			{
				Dictionary<double, long> map = maps[index];
				if (map != null && map.Count > 0)
					coverage++;
			}

			return coverage;
		}

		private static int GetRevision(OrcaOrderFlowDataSource source)
		{
			if (source == null || source.RevisionProvider == null)
				return 0;
			return source.RevisionProvider();
		}

		private static int GetBucketSeconds(OrcaOrderFlowDataSource source)
		{
			if (source == null || source.BucketSecondsProvider == null)
				return -1;
			return source.BucketSecondsProvider();
		}

		private static DateTime GetLastUpdatedUtc(OrcaProfileDataSource source)
		{
			if (source == null || source.LastUpdatedUtcProvider == null)
				return DateTime.MinValue;
			return source.LastUpdatedUtcProvider();
		}

		private static DateTime GetLastUpdatedUtc(OrcaOrderFlowDataSource source)
		{
			if (source == null || source.LastUpdatedUtcProvider == null)
				return DateTime.MinValue;
			return source.LastUpdatedUtcProvider();
		}

		private static void AddToMap(Dictionary<double, long> map, double price, long volume)
		{
			if (map == null || volume <= 0 || double.IsNaN(price) || double.IsInfinity(price))
				return;

			long existing;
			if (map.TryGetValue(price, out existing))
				map[price] = existing + volume;
			else
				map[price] = volume;
		}

		private static Dictionary<double, long> CopyMapAt(IList<Dictionary<double, long>> maps, int index)
		{
			if (maps == null || index < 0 || index >= maps.Count)
				return null;

			Dictionary<double, long> source = maps[index];
			return source != null && source.Count > 0 ? new Dictionary<double, long>(source) : null;
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

		public static bool BuildFixedRangeFromBars(Bars bars, int fromIndex, int toIndex, double rangeLowPrice, double rangeHighPrice, int requestedRowCount, int requestedTicksPerRow, bool useTicksPerRow, double valueAreaPercent, double tickSize, OrcaVolumeProfileResult result)
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

			double rangeLow;
			double rangeHigh;
			double safeTickSize;
			if (!TryNormalizeFixedRange(rangeLowPrice, rangeHighPrice, tickSize, out rangeLow, out rangeHigh, out safeTickSize))
				return false;

			if (!InitializeFixedRangeRows(result, rangeLow, rangeHigh, requestedRowCount, requestedTicksPerRow, useTicksPerRow, safeTickSize))
				return false;

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

				if (high < rangeLow - Epsilon || low > rangeHigh + Epsilon)
					continue;

				double barLow = Math.Max(low, rangeLow);
				double barHigh = Math.Min(high, rangeHigh);
				bool upBar = double.IsNaN(open) || double.IsNaN(close) || close >= open;

				if (barHigh <= barLow + Epsilon)
				{
					double referencePrice = close >= rangeLow && close <= rangeHigh ? close : barLow;
					AddVolumeToRow(result, GetRowIndex(result, referencePrice), volume, upBar);
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

		public static bool BuildFixedRangeFromPriceMaps(IList<Dictionary<double, long>> volumeByBar, IList<Dictionary<double, long>> upVolumeByBar, IList<Dictionary<double, long>> downVolumeByBar, int fromIndex, int toIndex, double rangeLowPrice, double rangeHighPrice, int requestedRowCount, int requestedTicksPerRow, bool useTicksPerRow, double valueAreaPercent, double tickSize, OrcaVolumeProfileResult result)
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

			double rangeLow;
			double rangeHigh;
			double safeTickSize;
			if (!TryNormalizeFixedRange(rangeLowPrice, rangeHighPrice, tickSize, out rangeLow, out rangeHigh, out safeTickSize))
				return false;

			if (!InitializeFixedRangeRows(result, rangeLow, rangeHigh, requestedRowCount, requestedTicksPerRow, useTicksPerRow, safeTickSize))
				return false;

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
					if (price < rangeLow - BucketEpsilon || price > rangeHigh + BucketEpsilon)
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

		private static bool TryNormalizeFixedRange(double rangeLowPrice, double rangeHighPrice, double tickSize, out double rangeLow, out double rangeHigh, out double safeTickSize)
		{
			rangeLow = Math.Min(rangeLowPrice, rangeHighPrice);
			rangeHigh = Math.Max(rangeLowPrice, rangeHighPrice);
			safeTickSize = tickSize > 0 && !double.IsNaN(tickSize) && !double.IsInfinity(tickSize) ? tickSize : 0.01;

			if (double.IsNaN(rangeLow) || double.IsInfinity(rangeLow) || double.IsNaN(rangeHigh) || double.IsInfinity(rangeHigh))
				return false;

			if (rangeHigh <= rangeLow + Epsilon)
				rangeHigh = rangeLow + safeTickSize;

			return rangeHigh > rangeLow;
		}

		private static bool InitializeFixedRangeRows(OrcaVolumeProfileResult result, double rangeLow, double rangeHigh, int requestedRowCount, int requestedTicksPerRow, bool useTicksPerRow, double safeTickSize)
		{
			int rowCount = Math.Max(1, requestedRowCount);
			double rowHeight;
			double profileLow;
			double profileHigh;

			if (useTicksPerRow)
			{
				rowHeight = Math.Max(1, requestedTicksPerRow) * safeTickSize;
				profileLow = Math.Floor((rangeLow / rowHeight) + BucketEpsilon) * rowHeight;
				profileHigh = Math.Ceiling((rangeHigh / rowHeight) - BucketEpsilon) * rowHeight;
				if (profileHigh <= profileLow)
					profileHigh = profileLow + rowHeight;
				rowCount = Math.Max(1, (int)Math.Ceiling((profileHigh - profileLow) / rowHeight - BucketEpsilon));
			}
			else
			{
				profileLow = rangeLow;
				profileHigh = rangeHigh;
				rowHeight = (profileHigh - profileLow) / rowCount;
			}

			if (rowHeight <= 0 || double.IsNaN(rowHeight) || double.IsInfinity(rowHeight))
				return false;

			result.EnsureCapacity(rowCount);
			result.RowCount = rowCount;
			result.LowPrice = profileLow;
			result.HighPrice = profileHigh;
			result.RowHeight = rowHeight;

			for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
			{
				double rowLow = profileLow + (rowIndex * result.RowHeight);
				double rowHigh = rowIndex == rowCount - 1 ? profileHigh : rowLow + result.RowHeight;
				result.Rows[rowIndex].Reset(rowLow, rowHigh);
			}

			return true;
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

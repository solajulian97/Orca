#region Using declarations
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using NinjaTrader.Gui.Chart;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum OrcaReplayLifecycle
	{
		Idle = 0,
		Selecting = 1,
		Preparing = 2,
		Paused = 3,
		Playing = 4,
		Complete = 5,
		ReturningLive = 6
	}

	public enum OrcaReplayMode
	{
		Bar = 0,
		Tick = 1
	}

	[Flags]
	public enum OrcaReplayChartStyleSupport
	{
		None = 0,
		Candlestick = 1,
		Ohlc = 2,
		Line = 4,
		OrcaVolumeCandles = 8,
		AllV1 = Candlestick | Ohlc | Line | OrcaVolumeCandles
	}

	public sealed class OrcaReplayCapabilities
	{
		public bool SupportsBarReplay { get; private set; }
		public bool SupportsTickReplay { get; private set; }
		public bool PublishesReplayRenderSnapshot { get; private set; }
		public bool SupportsCheckpoints { get; private set; }
		public OrcaReplayChartStyleSupport SupportedChartStyles { get; private set; }

		public OrcaReplayCapabilities(bool supportsBarReplay, bool supportsTickReplay,
			bool publishesReplayRenderSnapshot, bool supportsCheckpoints,
			OrcaReplayChartStyleSupport supportedChartStyles)
		{
			SupportsBarReplay = supportsBarReplay;
			SupportsTickReplay = supportsTickReplay;
			PublishesReplayRenderSnapshot = publishesReplayRenderSnapshot;
			SupportsCheckpoints = supportsCheckpoints;
			SupportedChartStyles = supportedChartStyles;
		}
	}

	public sealed class OrcaReplayTradeEvent
	{
		public DateTime Time { get; private set; }
		public double Price { get; private set; }
		public long Volume { get; private set; }
		public double Bid { get; private set; }
		public double Ask { get; private set; }
		public long Sequence { get; private set; }
		public int PrimaryBarIndex { get; private set; }

		public OrcaReplayTradeEvent(DateTime time, double price, long volume, double bid,
			double ask, long sequence, int primaryBarIndex)
		{
			Time = time;
			Price = price;
			Volume = volume;
			Bid = bid;
			Ask = ask;
			Sequence = sequence;
			PrimaryBarIndex = primaryBarIndex;
		}
	}

	public static class OrcaReplayEventIdentity
	{
		/// <summary>
		/// Used once at the Historical-to-Realtime boundary. Sequence and quote context are
		/// intentionally excluded because NinjaTrader may republish the same Last event with
		/// a new callback sequence after transition.
		/// </summary>
		public static bool IsSameBoundaryTrade(OrcaReplayTradeEvent historical, OrcaReplayTradeEvent live)
		{
			return historical != null && live != null
				&& historical.Time == live.Time
				&& historical.Price.Equals(live.Price)
				&& historical.Volume == live.Volume
				&& historical.PrimaryBarIndex == live.PrimaryBarIndex;
		}
	}

	/// <summary>
	/// Deterministic bid/ask classification with tick-rule fallback. Participants own one
	/// instance per replay model so no classifier state is shared with the live model.
	/// </summary>
	public sealed class OrcaReplayTradeClassifier
	{
		private double previousPrice = double.NaN;
		private int lastDirection;

		public void Reset()
		{
			previousPrice = double.NaN;
			lastDirection = 0;
		}

		public long Classify(OrcaReplayTradeEvent tradeEvent)
		{
			return Classify(tradeEvent, true);
		}

		public long Classify(OrcaReplayTradeEvent tradeEvent, bool useBidAsk)
		{
			if (tradeEvent == null || tradeEvent.Volume <= 0) return 0;
			double price = tradeEvent.Price;
			long volume = tradeEvent.Volume;
			double bid = tradeEvent.Bid;
			double ask = tradeEvent.Ask;
			long signed = 0;
			bool haveQuote = useBidAsk && !double.IsNaN(ask) && !double.IsNaN(bid)
				&& ask > 0 && bid > 0 && ask >= bid;
			if (haveQuote)
			{
				if (price >= ask) signed = volume;
				else if (price <= bid) signed = -volume;
				else signed = ClassifyByTickRule(price, volume);
			}
			else signed = ClassifyByTickRule(price, volume);

			previousPrice = price;
			if (signed > 0) lastDirection = 1;
			else if (signed < 0) lastDirection = -1;
			return signed;
		}

		private long ClassifyByTickRule(double price, long volume)
		{
			if (double.IsNaN(previousPrice)) return 0;
			if (price > previousPrice) return volume;
			if (price < previousPrice) return -volume;
			return lastDirection * volume;
		}
	}

	public sealed class OrcaReplayCheckpoint
	{
		public DateTime Time { get; private set; }
		public long Sequence { get; private set; }
		public int PrimaryBarIndex { get; private set; }
		public string ParticipantId { get; private set; }
		public object State { get; private set; }

		public OrcaReplayCheckpoint(DateTime time, long sequence, int primaryBarIndex,
			string participantId, object state)
		{
			Time = time;
			Sequence = sequence;
			PrimaryBarIndex = primaryBarIndex;
			ParticipantId = participantId ?? string.Empty;
			State = state;
		}
	}

	public sealed class OrcaReplayContext
	{
		public ChartControl ChartControl { get; private set; }
		public OrcaReplayMode Mode { get; private set; }
		public DateTime StartTime { get; private set; }
		public DateTime CurrentTime { get; private set; }
		public int PrimaryBarIndex { get; private set; }
		public long Sequence { get; private set; }
		public CancellationToken CancellationToken { get; private set; }

		public OrcaReplayContext(ChartControl chartControl, OrcaReplayMode mode,
			DateTime startTime, DateTime currentTime, int primaryBarIndex, long sequence,
			CancellationToken cancellationToken)
		{
			ChartControl = chartControl;
			Mode = mode;
			StartTime = startTime;
			CurrentTime = currentTime;
			PrimaryBarIndex = primaryBarIndex;
			Sequence = sequence;
			CancellationToken = cancellationToken;
		}
	}

	public interface IOrcaReplayParticipant
	{
		string ReplayParticipantId { get; }
		OrcaReplayCapabilities ReplayCapabilities { get; }
		OrcaReplayCheckpoint CaptureReplayCheckpoint(OrcaReplayContext context);
		void PrepareReplay(OrcaReplayContext context, OrcaReplayCheckpoint checkpoint);
		void ApplyReplayEvent(OrcaReplayContext context, OrcaReplayTradeEvent tradeEvent);
		void ApplyReplayBar(OrcaReplayContext context, int primaryBarIndex);
		void PublishReplaySnapshot(OrcaReplayContext context);
		void RestoreLiveState();
	}

	/// <summary>
	/// Lightweight adapter state for indicators whose completed historical bars are causal
	/// and whose future price-panel rendering is covered by the coordinator mask. The only
	/// mutable replay value is an atomic visible-bar horizon; live indicator state is never
	/// copied or modified. Indicators with a separate panel can clamp rendering to MaxBarIndex.
	/// </summary>
	public sealed class OrcaReplayBarHorizon
	{
		private static readonly OrcaReplayCapabilities capabilities = new OrcaReplayCapabilities(
			true, false, true, true, OrcaReplayChartStyleSupport.AllV1);
		private readonly string participantId;
		private int maxBarIndex = -1;

		public OrcaReplayBarHorizon(string participantId)
		{
			this.participantId = participantId ?? string.Empty;
		}

		public string ParticipantId { get { return participantId; } }
		public OrcaReplayCapabilities Capabilities { get { return capabilities; } }
		public int MaxBarIndex { get { return Volatile.Read(ref maxBarIndex); } }

		public OrcaReplayCheckpoint Capture(OrcaReplayContext context)
		{
			if (context == null) throw new ArgumentNullException("context");
			return new OrcaReplayCheckpoint(context.CurrentTime, context.Sequence,
				context.PrimaryBarIndex, participantId, context.PrimaryBarIndex);
		}

		public void Prepare(OrcaReplayContext context, OrcaReplayCheckpoint checkpoint)
		{
			if (context == null) throw new ArgumentNullException("context");
			int selectedBar = context.PrimaryBarIndex;
			if (checkpoint != null && checkpoint.State is int)
				selectedBar = (int)checkpoint.State;
			Interlocked.Exchange(ref maxBarIndex, selectedBar);
		}

		public void ApplyBar(int primaryBarIndex)
		{
			Interlocked.Exchange(ref maxBarIndex, primaryBarIndex);
		}

		public void Restore()
		{
			Interlocked.Exchange(ref maxBarIndex, -1);
		}
	}

	/// <summary>
	/// Immutable state consumed by render and chart interaction paths. Publishing a new
	/// instance is atomic; no replay model work, cache access, or locking is required in OnRender.
	/// </summary>
	public sealed class OrcaReplayViewSnapshot
	{
		public OrcaReplayLifecycle Lifecycle { get; private set; }
		public OrcaReplayMode Mode { get; private set; }
		public DateTime SelectionTime { get; private set; }
		public DateTime CurrentTime { get; private set; }
		public int SelectionBarIndex { get; private set; }
		public int CurrentBarIndex { get; private set; }
		public long CurrentSequence { get; private set; }
		public double Speed { get; private set; }
		public bool TickModeAvailable { get; private set; }
		public string Status { get; private set; }

		public bool IsReplayActive
		{
			get
			{
				return Lifecycle == OrcaReplayLifecycle.Preparing
					|| Lifecycle == OrcaReplayLifecycle.Paused
					|| Lifecycle == OrcaReplayLifecycle.Playing
					|| Lifecycle == OrcaReplayLifecycle.Complete;
			}
		}

		public OrcaReplayViewSnapshot(OrcaReplayLifecycle lifecycle, OrcaReplayMode mode,
			DateTime selectionTime, DateTime currentTime, int selectionBarIndex,
			int currentBarIndex, long currentSequence, double speed,
			bool tickModeAvailable, string status)
		{
			Lifecycle = lifecycle;
			Mode = mode;
			SelectionTime = selectionTime;
			CurrentTime = currentTime;
			SelectionBarIndex = selectionBarIndex;
			CurrentBarIndex = currentBarIndex;
			CurrentSequence = currentSequence;
			Speed = speed;
			TickModeAvailable = tickModeAvailable;
			Status = status ?? string.Empty;
		}
	}

	/// <summary>
	/// Chart-local replay registry and shared order-entry interlock. It intentionally has no
	/// account-global state, so an unrelated live chart remains usable during replay.
	/// </summary>
	public static class OrcaReplayCore
	{
		private sealed class ChartSession
		{
			public object Owner;
			public volatile OrcaReplayViewSnapshot Snapshot;
			public readonly Dictionary<string, IOrcaReplayParticipant> Participants
				= new Dictionary<string, IOrcaReplayParticipant>(StringComparer.OrdinalIgnoreCase);
		}

		private static readonly object Sync = new object();
		private static readonly ConcurrentDictionary<ChartControl, ChartSession> Sessions
			= new ConcurrentDictionary<ChartControl, ChartSession>();

		public static bool TryRegisterCoordinator(ChartControl chartControl, object owner)
		{
			if (chartControl == null || owner == null) return false;
			lock (Sync)
			{
				ChartSession existing;
				if (Sessions.TryGetValue(chartControl, out existing))
				{
					if (existing.Owner == null)
					{
						existing.Owner = owner;
						existing.Snapshot = CreateIdleSnapshot("Capture active");
						return true;
					}
					return ReferenceEquals(existing.Owner, owner);
				}

				Sessions[chartControl] = new ChartSession
				{
					Owner = owner,
					Snapshot = CreateIdleSnapshot("Capture active")
				};
				return true;
			}
		}

		public static void UnregisterCoordinator(ChartControl chartControl, object owner)
		{
			if (chartControl == null || owner == null) return;
			List<IOrcaReplayParticipant> participants = null;
			lock (Sync)
			{
				ChartSession session;
				if (!Sessions.TryGetValue(chartControl, out session) || !ReferenceEquals(session.Owner, owner))
					return;
				participants = session.Participants.Values.Distinct().ToList();
				session.Owner = null;
				session.Snapshot = CreateIdleSnapshot("Coordinator unavailable");
				if (session.Participants.Count == 0)
				{
					ChartSession removed;
					Sessions.TryRemove(chartControl, out removed);
				}
			}

			foreach (IOrcaReplayParticipant participant in participants)
			{
				try { participant.RestoreLiveState(); }
				catch { }
			}
		}

		public static bool RegisterParticipant(ChartControl chartControl, IOrcaReplayParticipant participant)
		{
			if (chartControl == null || participant == null || string.IsNullOrWhiteSpace(participant.ReplayParticipantId))
				return false;
			lock (Sync)
			{
				ChartSession session;
				if (!Sessions.TryGetValue(chartControl, out session))
				{
					session = new ChartSession { Owner = null, Snapshot = CreateIdleSnapshot("Coordinator unavailable") };
					Sessions[chartControl] = session;
				}
				session.Participants[participant.ReplayParticipantId] = participant;
				return true;
			}
		}

		public static void UnregisterParticipant(ChartControl chartControl, IOrcaReplayParticipant participant)
		{
			if (chartControl == null || participant == null) return;
			lock (Sync)
			{
				ChartSession session;
				if (!Sessions.TryGetValue(chartControl, out session)) return;
				IOrcaReplayParticipant current;
				if (session.Participants.TryGetValue(participant.ReplayParticipantId, out current)
					&& ReferenceEquals(current, participant))
					session.Participants.Remove(participant.ReplayParticipantId);
				if (session.Owner == null && session.Participants.Count == 0)
				{
					ChartSession removed;
					Sessions.TryRemove(chartControl, out removed);
				}
			}
		}

		public static ReadOnlyCollection<IOrcaReplayParticipant> GetParticipants(ChartControl chartControl)
		{
			lock (Sync)
			{
				ChartSession session;
				if (!Sessions.TryGetValue(chartControl, out session))
					return new List<IOrcaReplayParticipant>().AsReadOnly();
				return session.Participants.Values.Distinct().ToList().AsReadOnly();
			}
		}

		public static void PublishSnapshot(ChartControl chartControl, object owner, OrcaReplayViewSnapshot snapshot)
		{
			if (chartControl == null || owner == null || snapshot == null) return;
			ChartSession session;
			if (Sessions.TryGetValue(chartControl, out session) && ReferenceEquals(session.Owner, owner))
				session.Snapshot = snapshot;
		}

		public static bool TryGetSnapshot(ChartControl chartControl, out OrcaReplayViewSnapshot snapshot)
		{
			snapshot = null;
			if (chartControl == null) return false;
			ChartSession session;
			if (!Sessions.TryGetValue(chartControl, out session)) return false;
			snapshot = session.Snapshot;
			return snapshot != null;
		}

		public static bool IsChartLocked(ChartControl chartControl)
		{
			OrcaReplayViewSnapshot snapshot;
			return TryGetSnapshot(chartControl, out snapshot) && snapshot.IsReplayActive;
		}

		public static bool IsReplayActive(ChartControl chartControl)
		{
			return IsChartLocked(chartControl);
		}

		private static OrcaReplayViewSnapshot CreateIdleSnapshot(string status)
		{
			return new OrcaReplayViewSnapshot(OrcaReplayLifecycle.Idle, OrcaReplayMode.Bar,
				DateTime.MinValue, DateTime.MinValue, -1, -1, 0, 1.0, false, status);
		}
	}
}

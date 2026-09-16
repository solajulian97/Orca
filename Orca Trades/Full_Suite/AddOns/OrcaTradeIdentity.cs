// Canonical, source-linked identity contract. No NinjaTrader, database or market-data dependencies.
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Orca.SharedIdentity
{
    internal sealed class Allocation
    {
        public string ExecutionId;
        public int SignedQuantity;
    }

    internal sealed class Cycle
    {
        public string Account;
        public string Instrument;
        public bool CompleteHistory;
        public string Reason;
        public readonly List<Allocation> Allocations = new List<Allocation>();
        public string Uid { get { return Contract.Build(this); } }
    }

    internal static class Contract
    {
        public static bool Validate(Cycle cycle, out string reason)
        {
            reason = "Missing provenance";
            if (cycle == null) return false;
            if (!cycle.CompleteHistory) { reason = cycle.Reason ?? "History continuity unverified"; return false; }
            if (string.IsNullOrWhiteSpace(cycle.Account) || string.IsNullOrWhiteSpace(cycle.Instrument))
            { reason = "Missing account or full contract"; return false; }
            if (cycle.Allocations.Count < 2) { reason = "Missing execution allocations"; return false; }
            long net = 0;
            int side = 0;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < cycle.Allocations.Count; i++)
            {
                var fill = cycle.Allocations[i];
                if (fill == null || string.IsNullOrWhiteSpace(fill.ExecutionId) || fill.SignedQuantity == 0 || !ids.Add(fill.ExecutionId))
                { reason = "Missing, duplicate or invalid execution allocation"; return false; }
                if (i == 0) side = Math.Sign(fill.SignedQuantity);
                net += fill.SignedQuantity;
                if ((net != 0 && Math.Sign(net) != side) || (net == 0 && i != cycle.Allocations.Count - 1))
                { reason = "Not one flat-to-flat cycle; split reversal quantities"; return false; }
            }
            if (net != 0) { reason = "Open cycle"; return false; }
            reason = "Complete";
            return true;
        }

        public static string Build(Cycle cycle)
        {
            string reason;
            if (!Validate(cycle, out reason)) return null;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write("orca.trade.v1"); writer.Write(cycle.Account); writer.Write(cycle.Instrument);
                    writer.Write(cycle.Allocations.Count);
                    foreach (var fill in cycle.Allocations) { writer.Write(fill.ExecutionId); writer.Write(fill.SignedQuantity); }
                }
                using (var sha = SHA256.Create())
                    return "orca.trade.v1:" + BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }

        // Small fixed schema avoids adding a JSON/assembly dependency to NinjaScript.
        public static string Json(Cycle cycle)
        {
            if (cycle == null) return null;
            var json = new StringBuilder("{\"Version\":1,\"Account\":");
            json.Append(Quote(cycle.Account)).Append(",\"Instrument\":").Append(Quote(cycle.Instrument));
            json.Append(",\"CompleteHistory\":").Append(cycle.CompleteHistory ? "true" : "false");
            json.Append(",\"Reason\":").Append(Quote(cycle.Reason)).Append(",\"Allocations\":[");
            for (int i = 0; i < cycle.Allocations.Count; i++)
            {
                if (i > 0) json.Append(',');
                var fill = cycle.Allocations[i];
                json.Append("{\"ExecutionId\":").Append(Quote(fill.ExecutionId)).Append(",\"SignedQuantity\":")
                    .Append(fill.SignedQuantity.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('}');
            }
            return json.Append("]}").ToString();
        }

        private static string Quote(string value)
        {
            if (value == null) return "null";
            var text = new StringBuilder("\"");
            foreach (char c in value)
            {
                if (c == '"' || c == '\\') text.Append('\\').Append(c);
                else if (c < 32 || char.IsSurrogate(c)) text.Append("\\u").Append(((int)c).ToString("x4"));
                else text.Append(c);
            }
            return text.Append('"').ToString();
        }
    }

    // One account/full-contract observer. Caller serializes access and deduplicates deliveries.
    // First cycle is unverified: only an observed execution ending flat establishes a boundary.
    internal sealed class Tracker
    {
        private Cycle cycle;
        private long net;
        private bool flatObserved;
        private DateTime lastTime;
        private string account;
        private string instrument;
        private readonly HashSet<string> cycleIds = new HashSet<string>(StringComparer.Ordinal);

        public void Invalidate(string reason)
        {
            flatObserved = false;
            if (cycle != null) { cycle.CompleteHistory = false; cycle.Reason = reason; }
        }

        public Cycle Fill(string accountName, string fullContract, string id, int signedQuantity, DateTime time, int? positionAfter)
        {
            if (account != null && (account != accountName || instrument != fullContract))
            {
                cycle = null; net = 0; flatObserved = false; lastTime = default(DateTime);
            }
            account = accountName; instrument = fullContract;
            if (signedQuantity == 0 || signedQuantity == int.MinValue)
            { Invalidate("Invalid quantity"); return null; }
            long next = net + signedQuantity;
            bool reliable = positionAfter.HasValue && Math.Abs((long)positionAfter.Value) == Math.Abs(next) &&
                time != default(DateTime) && (lastTime == default(DateTime) || time >= lastTime) && !string.IsNullOrWhiteSpace(id);
            if (!reliable) Invalidate("Missing ID, time/order or execution-position continuity");
            lastTime = time;
            int sign = Math.Sign(signedQuantity);
            int quantity = Math.Abs(signedQuantity);
            int closing = net != 0 && Math.Sign(net) != sign ? (int)Math.Min(Math.Abs(net), quantity) : 0;
            Cycle completed = null;
            if (closing > 0)
            {
                Append(id, sign * closing);
                net += sign * closing;
                if (net == 0)
                {
                    completed = cycle;
                    cycle = null;
                    // A virtual reversal boundary is trusted only if the closing cycle was trusted.
                    flatObserved = reliable && (positionAfter == 0 || (completed != null && completed.CompleteHistory));
                }
            }
            int opening = quantity - closing;
            if (opening > 0)
            {
                if (net == 0)
                {
                    cycleIds.Clear();
                    cycle = new Cycle { Account = accountName, Instrument = fullContract,
                        CompleteHistory = flatObserved && reliable,
                        Reason = flatObserved && reliable ? "Observed flat boundary and continuous execution positions" : "Initial flat boundary or continuity unverified" };
                }
                Append(id, sign * opening);
                net += sign * opening;
            }
            return completed;
        }

        private void Append(string id, int quantity)
        {
            if (cycle == null) return;
            if (cycle.Allocations.Count >= 100000) { Invalidate("Identity allocation limit exceeded"); return; }
            if (!string.IsNullOrEmpty(id) && !cycleIds.Add(id)) Invalidate("Duplicate ID inside cycle");
            cycle.Allocations.Add(new Allocation { ExecutionId = id, SignedQuantity = quantity });
        }
    }
}

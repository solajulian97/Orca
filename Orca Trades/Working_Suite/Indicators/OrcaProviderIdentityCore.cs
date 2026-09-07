using System;
using System.Globalization;

namespace NinjaTrader.NinjaScript.Indicators
{
    public enum OrcaProviderEnvironment { LiveFeed, MarketReplay, HistoricalOnly }

    // Immutable description supplied by the owning adapter, never inferred from a
    // display name. Keep platform objects/accounts/charts out of shared identities.
    public sealed class OrcaProviderSourceIdentity
    {
        public readonly OrcaProviderStreamKey Key;

        public OrcaProviderSourceIdentity(string fullContract, OrcaProviderEnvironment environment,
            Guid connectionEpoch, string sessionDefinitionSnapshot, string timezoneRulesSnapshot,
            bool resetClassificationOnSessionBreak, Guid classifierOrigin,
            OrcaProviderClassificationPolicy classificationPolicy, string quoteSemantics)
        {
            if (!Enum.IsDefined(typeof(OrcaProviderEnvironment), environment))
                throw new ArgumentOutOfRangeException("environment");
            if (connectionEpoch == Guid.Empty) throw new ArgumentException("An explicit connection/replay epoch is required.", "connectionEpoch");
            if (classifierOrigin == Guid.Empty) throw new ArgumentException("An explicit classifier initialization scope is required.", "classifierOrigin");
            Require(fullContract, "fullContract");
            Require(sessionDefinitionSnapshot, "sessionDefinitionSnapshot");
            Require(timezoneRulesSnapshot, "timezoneRulesSnapshot");
            Require(quoteSemantics, "quoteSemantics");
            string policy;
            switch (classificationPolicy)
            {
                case OrcaProviderClassificationPolicy.TickDirection: policy = "tickdirection:v1"; break;
                case OrcaProviderClassificationPolicy.TradeQuoteThenTickDirection: policy = "bidask-fallback:v1"; break;
                default: throw new ArgumentOutOfRangeException("classificationPolicy");
            }
            // Length-prefix all arbitrary strings. Separators alone can alias user-defined
            // session/timezone descriptions. Versioned encoding is ordinal and culture-free.
            string data = "source-v1:" + ((int)environment).ToString(CultureInfo.InvariantCulture)
                + ":" + connectionEpoch.ToString("N") + ":" + classifierOrigin.ToString("N")
                + ":" + Part(quoteSemantics);
            string session = "session-v1:" + Part(sessionDefinitionSnapshot) + Part(timezoneRulesSnapshot)
                + (resetClassificationOnSessionBreak ? "reset" : "continuous");
            Key = new OrcaProviderStreamKey(fullContract, data, session, "UTC", policy);
        }

        private static void Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim())
                throw new ArgumentException("A nonempty canonical identity snapshot is required.", name);
        }
        private static string Part(string value)
        { return value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value; }
    }
}

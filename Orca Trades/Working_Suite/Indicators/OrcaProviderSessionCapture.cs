using System;
using System.Globalization;
using System.Linq;
using System.Text;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.Indicators
{
    // Call only during owner-controlled initialization with resolved, stable metadata.
    // Retains strings only, not mutable TradingHours or chart references.
    public sealed class OrcaProviderSessionCapture
    {
        public readonly string Definition;
        public readonly string EventTimezone;
        private OrcaProviderSessionCapture(string definition, string eventTimezone)
        { Definition = definition; EventTimezone = eventTimezone; }

        public static OrcaProviderSessionCapture Capture(TradingHours hours, TimeZoneInfo eventTimezone)
        {
            if (hours == null || eventTimezone == null || hours.TimeZoneInfo == null)
                throw new ArgumentException("Resolved session and event timezone metadata required.");
            if (hours.Name == TradingHours.UseInstrumentSettings || hours.Name == TradingHours.UseDataSeriesSettings)
                throw new ArgumentException("Resolve the effective Trading Hours template before capture.");
            string first = Encode(hours);
            string second = Encode(hours);
            if (!string.Equals(first, second, StringComparison.Ordinal))
                throw new InvalidOperationException("Trading Hours changed during capture; reload the source.");
            // .NET serialization preserves offset, transition and adjustment rules.
            // Display names are included conservatively: false non-sharing is preferable to aliasing.
            return new OrcaProviderSessionCapture(first, eventTimezone.ToSerializedString());
        }

        private static string Encode(TradingHours hours)
        {
            if (hours.Sessions == null || hours.Sessions.Count == 0 || hours.Holidays == null || hours.PartialHolidays == null)
                throw new ArgumentException("Incomplete Trading Hours definition.");
            var result = new StringBuilder("nt-session-v1:");
            Add(result, hours.TimeZoneInfo.ToSerializedString());
            Add(result, hours.Sessions.Count);
            foreach (var session in hours.Sessions) AddSession(result, session);
            Add(result, hours.Holidays.Count);
            foreach (var holiday in hours.Holidays.OrderBy(p => p.Key.Ticks))
            {
                AddDate(result, holiday.Key);
                Add(result, holiday.Value ?? "");
            }
            Add(result, hours.PartialHolidays.Count);
            foreach (var holiday in hours.PartialHolidays.OrderBy(p => p.Key.Ticks))
            {
                var partial = holiday.Value;
                if (partial == null || partial.Sessions == null) throw new ArgumentException("Incomplete partial holiday.");
                AddDate(result, holiday.Key); AddDate(result, partial.Date);
                Add(result, partial.Description ?? "");
                Add(result, partial.IsEarlyEnd); Add(result, partial.IsLateBegin);
                Add(result, partial.Constraint != null);
                if (partial.Constraint != null) AddSession(result, partial.Constraint);
                Add(result, partial.Sessions.Count);
                foreach (var session in partial.Sessions) AddSession(result, session);
            }
            return result.ToString();
        }
        private static void AddSession(StringBuilder result, Session session)
        {
            if (session == null) throw new ArgumentException("Missing session definition.");
            Add(result, (int)session.BeginDay); Add(result, session.BeginTime);
            Add(result, (int)session.EndDay); Add(result, session.EndTime); Add(result, (int)session.TradingDay);
        }
        private static void AddDate(StringBuilder result, DateTime date)
        { Add(result, date.Ticks); Add(result, (int)date.Kind); }
        private static void Add(StringBuilder result, object value)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            result.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text);
        }
    }
}

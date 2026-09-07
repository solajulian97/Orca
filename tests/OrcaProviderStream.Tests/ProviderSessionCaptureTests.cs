using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.Indicators;

// Shape-only fixtures. The separate installed-platform semantic check verifies API binding.
namespace NinjaTrader.Data
{
    public class Session { public DayOfWeek BeginDay, EndDay, TradingDay; public int BeginTime, EndTime; }
    public class PartialHoliday
    {
        public Session Constraint; public DateTime Date; public string Description;
        public bool IsEarlyEnd, IsLateBegin; public Collection<Session> Sessions = new Collection<Session>();
    }
    public class TradingHours
    {
        public static string UseInstrumentSettings = "<instrument>", UseDataSeriesSettings = "<series>";
        public string Name = "Template"; public TimeZoneInfo TimeZoneInfo = TimeZoneInfo.Utc;
        public Collection<Session> Sessions = new Collection<Session>();
        public Dictionary<DateTime, string> Holidays = new Dictionary<DateTime, string>();
        public Dictionary<DateTime, PartialHoliday> PartialHolidays = new Dictionary<DateTime, PartialHoliday>();
    }
}
static class ProviderSessionCaptureTests
{
    static TradingHours Hours()
    {
        var hours = new TradingHours();
        hours.Sessions.Add(new Session { BeginDay = DayOfWeek.Monday, EndDay = DayOfWeek.Monday,
            TradingDay = DayOfWeek.Monday, BeginTime = 90000, EndTime = 170000 });
        return hours;
    }
    static string Capture(TradingHours hours) { return OrcaProviderSessionCapture.Capture(hours, TimeZoneInfo.Utc).Definition; }
    public static void Run(Action<bool, string> check)
    {
        var original = Capture(Hours());
        check(original == Capture(Hours()), "equivalent session capture stable");
        var renamed = Hours(); renamed.Name = "Renamed template";
        check(original == Capture(renamed), "template label alone is not session semantics");
        var changed = Hours(); changed.Sessions[0].EndTime--;
        check(original != Capture(changed), "same-name schedule edit changes identity");
        changed = Hours(); changed.Sessions[0].TradingDay = DayOfWeek.Tuesday;
        check(original != Capture(changed), "trading-day assignment participates");
        changed = Hours(); changed.Holidays.Add(new DateTime(2026, 12, 25), "Closed");
        check(original != Capture(changed), "full holiday participates");
        var reverse = Hours(); reverse.Holidays.Add(new DateTime(2026, 12, 25), "Closed");
        reverse.Holidays.Add(new DateTime(2026, 1, 1), "New Year");
        changed.Holidays.Clear(); changed.Holidays.Add(new DateTime(2026, 1, 1), "New Year"); changed.Holidays.Add(new DateTime(2026, 12, 25), "Closed");
        check(Capture(changed) == Capture(reverse), "holiday dictionary order independent");
        changed = Hours(); var partial = new PartialHoliday { Date = new DateTime(2026, 12, 24), IsEarlyEnd = true };
        changed.PartialHolidays.Add(partial.Date, partial);
        string before = Capture(changed);
        check(original != before, "partial holiday participates");
        partial.Constraint = new Session { EndTime = 120000 };
        check(before != Capture(changed), "partial holiday constraint participates");
        before = Capture(changed); partial.Sessions.Add(new Session { BeginTime = 90000, EndTime = 120000 });
        check(before != Capture(changed), "partial holiday session list participates");
        var frozen = OrcaProviderSessionCapture.Capture(changed, TimeZoneInfo.Utc);
        before = frozen.Definition; partial.Sessions[0].EndTime++;
        check(before == frozen.Definition && before != Capture(changed), "capture retains no mutable session objects");
        var shifted = TimeZoneInfo.CreateCustomTimeZone("test-offset", TimeSpan.FromHours(1), "test", "test");
        changed = Hours(); changed.TimeZoneInfo = shifted;
        check(original != Capture(changed), "session timezone rules participate");
        check(OrcaProviderSessionCapture.Capture(Hours(), shifted).EventTimezone != OrcaProviderSessionCapture.Capture(Hours(), TimeZoneInfo.Utc).EventTimezone, "event timezone independent from exchange timezone");
        Action<Action> rejects = action => { bool rejected = false; try { action(); } catch (ArgumentException) { rejected = true; } check(rejected, "incomplete session capture fails closed"); };
        rejects(() => Capture(null));
        changed = Hours(); changed.Name = TradingHours.UseInstrumentSettings; rejects(() => Capture(changed));
        changed = Hours(); changed.Sessions.Clear(); rejects(() => Capture(changed));
        rejects(() => OrcaProviderSessionCapture.Capture(Hours(), null));
    }
}

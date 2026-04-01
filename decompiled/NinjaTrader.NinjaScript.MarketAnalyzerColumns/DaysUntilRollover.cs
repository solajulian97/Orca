using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class DaysUntilRollover : MarketAnalyzerColumn
{
	private SessionIterator sessionIterator;

	private DispatcherTimer timer;

	private void CalculateDays()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		DateTime dateTime = ((Connection.PlaybackConnection != null) ? Connection.PlaybackConnection.Now : Globals.Now);
		if (sessionIterator == null)
		{
			sessionIterator = new SessionIterator(((NinjaScriptBase)this).Instrument.MasterInstrument.TradingHours);
		}
		if (!(dateTime > sessionIterator.ActualTradingDayEndLocal))
		{
			return;
		}
		sessionIterator.GetNextSession(dateTime, false);
		lock (((NinjaScriptBase)this).Instrument.MasterInstrument.RolloverCollection)
		{
			foreach (Rollover item in (Collection<Rollover>)(object)((NinjaScriptBase)this).Instrument.MasterInstrument.RolloverCollection)
			{
				if (item.ContractMonth == ((NinjaScriptBase)this).Instrument.Expiry)
				{
					((MarketAnalyzerColumnBase)this).CurrentValue = ((NinjaScriptBase)this).Instrument.MasterInstrument.GetNextRolloverDate(item.Date).Subtract(sessionIterator.ActualTradingDayExchange).TotalDays;
					break;
				}
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Invalid comparison between Unknown and I4
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionDaysUntilRollover;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameDaysUntilRollover;
			base.IsDataSeriesRequired = false;
		}
		else if ((int)((NinjaScript)this).State == 7)
		{
			((NinjaScriptBase)this).Dispatcher.InvokeAsync(delegate
			{
				timer = new DispatcherTimer
				{
					Interval = new TimeSpan(0, 0, 1),
					IsEnabled = true
				};
				timer.Tick += delegate
				{
					CalculateDays();
				};
			});
		}
		else if ((int)((NinjaScript)this).State == 8 && timer != null)
		{
			timer.IsEnabled = false;
			timer = null;
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		if (marketDataUpdate.IsReset)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = double.MinValue;
			sessionIterator = null;
		}
	}
}

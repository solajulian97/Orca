using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class RealizedProfitLoss : MarketAnalyzerColumn
{
	private readonly List<Execution> executions = new List<Execution>();

	private Currency accountDenomination = (Currency)7;

	[NinjaScriptProperty]
	[TypeConverter(typeof(AccountDisplayNameConverter))]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptColumnBaseAccount", GroupName = "NinjaScriptSetup", Order = 0)]
	public string AccountName { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			AccountName = MarketAnalyzerColumnBase.DefaultAccountName;
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionRealizedProfitLoss;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameRealizedProfitLoss;
			base.IsDataSeriesRequired = false;
			((MarketAnalyzerColumnBase)this).ShowInTotalRow = true;
		}
	}

	protected override void OnAccountItemUpdate(AccountItemEventArgs accountItemUpdate)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Invalid comparison between Unknown and I4
		if (AccountName != MarketAnalyzerColumnBase.DefaultAccountName || accountItemUpdate.Account.DisplayName != AccountName || (int)accountItemUpdate.AccountItem != 18)
		{
			return;
		}
		accountDenomination = accountItemUpdate.Account.Denomination;
		executions.Clear();
		foreach (Execution item in accountItemUpdate.Account.Executions.ToList())
		{
			if (item.Instrument == ((NinjaScriptBase)this).Instrument || ((int)item.Instrument.MasterInstrument.InstrumentType == 1 && item.Instrument.FullName == ((NinjaScriptBase)this).Instrument.FullName))
			{
				executions.Add(item);
			}
		}
		((MarketAnalyzerColumnBase)this).CurrentValue = SystemPerformance.Calculate((ICollection<Execution>)executions).AllTrades.TradesPerformance.Currency.CumProfit;
	}

	protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Invalid comparison between Unknown and I4
		if ((int)connectionStatusUpdate.Status != 3 && (int)connectionStatusUpdate.PreviousStatus != 2)
		{
			return;
		}
		Account val;
		lock (connectionStatusUpdate.Connection.Accounts)
		{
			val = connectionStatusUpdate.Connection.Accounts.FirstOrDefault((Account o) => o.DisplayName == AccountName);
		}
		if (val == null)
		{
			return;
		}
		accountDenomination = val.Denomination;
		executions.Clear();
		foreach (Execution item in val.Executions.ToList())
		{
			if (item.Instrument == ((NinjaScriptBase)this).Instrument || ((int)item.Instrument.MasterInstrument.InstrumentType == 1 && item.Instrument.FullName == ((NinjaScriptBase)this).Instrument.FullName))
			{
				executions.Add(item);
			}
		}
		((MarketAnalyzerColumnBase)this).CurrentValue = SystemPerformance.Calculate((ICollection<Execution>)executions).AllTrades.TradesPerformance.Currency.CumProfit;
	}

	protected override void OnExecutionUpdate(ExecutionEventArgs executionUpdate)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Invalid comparison between Unknown and I4
		if (!(executionUpdate.Execution.Account.DisplayName != AccountName) && ((int)executionUpdate.Execution.Instrument.MasterInstrument.InstrumentType == 1 || executionUpdate.Execution.Instrument == ((NinjaScriptBase)this).Instrument) && ((int)executionUpdate.Execution.Instrument.MasterInstrument.InstrumentType != 1 || !(executionUpdate.Execution.Instrument.FullName != ((NinjaScriptBase)this).Instrument.FullName)))
		{
			executions.Add(executionUpdate.Execution);
			((MarketAnalyzerColumnBase)this).CurrentValue = SystemPerformance.Calculate((ICollection<Execution>)executions).AllTrades.TradesPerformance.Currency.CumProfit;
		}
	}

	public override string Format(double value)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (((MarketAnalyzerColumnBase)this).CellConditions.Count == 0)
		{
			((MarketAnalyzerColumnBase)this).ForeColor = ((value >= 0.0) ? Application.Current.TryFindResource("MAGridForeground") : Application.Current.TryFindResource("StrategyAnalyzerNegativeValueBrush")) as Brush;
		}
		Currency val = accountDenomination;
		return Globals.FormatCurrency(value, val);
	}
}

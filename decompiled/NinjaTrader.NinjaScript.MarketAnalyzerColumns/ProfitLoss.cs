using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class ProfitLoss : MarketAnalyzerColumn
{
	private Currency accountDenomination = (Currency)7;

	private readonly List<Execution> executions = new List<Execution>();

	private Position position;

	private double realizedPL;

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
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionProfitLoss;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameProfitLoss;
			base.IsDataSeriesRequired = false;
			((MarketAnalyzerColumnBase)this).ShowInTotalRow = true;
		}
	}

	protected override void OnAccountItemUpdate(AccountItemEventArgs accountItemUpdate)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		if (AccountName != MarketAnalyzerColumnBase.DefaultAccountName || accountItemUpdate.Account.Name != AccountName || (int)accountItemUpdate.AccountItem != 23)
		{
			return;
		}
		lock (accountItemUpdate.Account.Positions)
		{
			position = accountItemUpdate.Account.Positions.FirstOrDefault((Position o) => o.Instrument.FullName == ((NinjaScriptBase)this).Instrument.FullName);
		}
		executions.Clear();
		foreach (Execution item in accountItemUpdate.Account.Executions.ToList())
		{
			if (item.Instrument == ((NinjaScriptBase)this).Instrument)
			{
				executions.Add(item);
			}
		}
		realizedPL = SystemPerformance.Calculate((ICollection<Execution>)executions).AllTrades.TradesPerformance.Currency.CumProfit;
		((MarketAnalyzerColumnBase)this).CurrentValue = realizedPL + ((position == null) ? 0.0 : position.GetUnrealizedProfitLoss((PerformanceUnit)0, double.MinValue));
	}

	protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Invalid comparison between Unknown and I4
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		if ((int)connectionStatusUpdate.Status == 3 || (int)connectionStatusUpdate.PreviousStatus == 4)
		{
			Account val;
			lock (connectionStatusUpdate.Connection.Accounts)
			{
				val = connectionStatusUpdate.Connection.Accounts.FirstOrDefault((Account o) => o.DisplayName == AccountName);
			}
			if (val != null)
			{
				lock (val.Positions)
				{
					position = val.Positions.FirstOrDefault((Position o) => o.Instrument.FullName == ((NinjaScriptBase)this).Instrument.FullName);
				}
				accountDenomination = val.Denomination;
				executions.Clear();
				foreach (Execution execution in val.Executions)
				{
					if (execution.Instrument == ((NinjaScriptBase)this).Instrument)
					{
						executions.Add(execution);
					}
				}
				realizedPL = SystemPerformance.Calculate((ICollection<Execution>)executions).AllTrades.TradesPerformance.Currency.CumProfit;
			}
		}
		else if ((int)connectionStatusUpdate.Status == 0 && (int)connectionStatusUpdate.PreviousStatus == 1 && position != null && position.Account.Connection == connectionStatusUpdate.Connection)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = 0.0;
			position = null;
			realizedPL = 0.0;
		}
		((MarketAnalyzerColumnBase)this).CurrentValue = realizedPL + ((position == null) ? 0.0 : position.GetUnrealizedProfitLoss((PerformanceUnit)0, double.MinValue));
	}

	protected override void OnExecutionUpdate(ExecutionEventArgs executionUpdate)
	{
		if (!(executionUpdate.Execution.Account.DisplayName != AccountName) && executionUpdate.Execution.Instrument == ((NinjaScriptBase)this).Instrument)
		{
			executions.Add(executionUpdate.Execution);
			realizedPL = SystemPerformance.Calculate((ICollection<Execution>)executions).AllTrades.TradesPerformance.Currency.CumProfit;
			((MarketAnalyzerColumnBase)this).CurrentValue = realizedPL + ((position == null) ? 0.0 : position.GetUnrealizedProfitLoss((PerformanceUnit)0, double.MinValue));
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		((MarketAnalyzerColumnBase)this).CurrentValue = realizedPL + ((position == null) ? 0.0 : position.GetUnrealizedProfitLoss((PerformanceUnit)0, double.MinValue));
	}

	protected override void OnPositionUpdate(PositionEventArgs positionUpdate)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Invalid comparison between Unknown and I4
		if (!(positionUpdate.Position.Account.DisplayName != AccountName) && positionUpdate.Position.Instrument == ((NinjaScriptBase)this).Instrument)
		{
			position = (((int)positionUpdate.Operation == 2) ? null : positionUpdate.Position);
			((MarketAnalyzerColumnBase)this).CurrentValue = realizedPL + ((position == null) ? 0.0 : position.GetUnrealizedProfitLoss((PerformanceUnit)0, double.MinValue));
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

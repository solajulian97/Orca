using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class TradedContracts : MarketAnalyzerColumn
{
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
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionTradedContracts;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameTradedContracts;
			base.IsDataSeriesRequired = false;
			((MarketAnalyzerColumnBase)this).FormatDecimals = 0;
			((MarketAnalyzerColumnBase)this).ShowInTotalRow = true;
		}
	}

	protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Invalid comparison between Unknown and I4
		if ((int)connectionStatusUpdate.Status == 3 && (int)connectionStatusUpdate.PreviousStatus == 4)
		{
			lock (connectionStatusUpdate.Connection.Accounts)
			{
				Account val = connectionStatusUpdate.Connection.Accounts.FirstOrDefault((Account o) => o.DisplayName == AccountName);
				if (val != null)
				{
					((MarketAnalyzerColumnBase)this).CurrentValue = 0.0;
					{
						foreach (Execution execution in val.Executions)
						{
							if (execution.Instrument == ((NinjaScriptBase)this).Instrument)
							{
								((MarketAnalyzerColumnBase)this).CurrentValue = ((MarketAnalyzerColumnBase)this).CurrentValue + (double)execution.Quantity;
							}
						}
						return;
					}
				}
				return;
			}
		}
		if ((int)connectionStatusUpdate.Status != 0 || (int)connectionStatusUpdate.PreviousStatus != 1)
		{
			return;
		}
		lock (connectionStatusUpdate.Connection.Accounts)
		{
			if (connectionStatusUpdate.Connection.Accounts.FirstOrDefault((Account o) => o.DisplayName == AccountName) != null)
			{
				((MarketAnalyzerColumnBase)this).CurrentValue = 0.0;
			}
		}
	}

	protected override void OnExecutionUpdate(ExecutionEventArgs executionUpdate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)executionUpdate.Operation == 0 && executionUpdate.Execution.Instrument == ((NinjaScriptBase)this).Instrument && executionUpdate.Execution.Account.DisplayName == AccountName)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = ((MarketAnalyzerColumnBase)this).CurrentValue + (double)executionUpdate.Execution.Quantity;
		}
	}
}

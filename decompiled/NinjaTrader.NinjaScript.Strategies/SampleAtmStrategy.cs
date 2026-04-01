using System;
using NinjaTrader.Cbi;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Strategies;

public class SampleAtmStrategy : Strategy
{
	private string atmStrategyId = string.Empty;

	private string orderId = string.Empty;

	private bool isAtmStrategyCreated;

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptStrategyDescriptionSampleATMStrategy;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptStrategyNameSampleATMStrategy;
			((StrategyBase)this).IsInstantiatedOnEachOptimizationIteration = false;
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Invalid comparison between Unknown and I4
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Invalid comparison between Unknown and I4
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		if (((NinjaScriptBase)this).CurrentBar < ((StrategyBase)this).BarsRequiredToTrade || (int)((NinjaScript)this).State == 5)
		{
			return;
		}
		if (orderId.Length == 0 && atmStrategyId.Length == 0 && ((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0])
		{
			isAtmStrategyCreated = false;
			atmStrategyId = ((StrategyBase)this).GetAtmStrategyUniqueId();
			orderId = ((StrategyBase)this).GetAtmStrategyUniqueId();
			((StrategyBase)this).AtmStrategyCreate((OrderAction)0, (OrderType)0, ((NinjaScriptBase)this).Low[0], 0.0, (TimeInForce)0, orderId, "AtmStrategyTemplate", atmStrategyId, (Action<ErrorCode, string>)delegate(ErrorCode atmCallbackErrorCode, string atmCallBackId)
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				if ((int)atmCallbackErrorCode == 0 && atmCallBackId == atmStrategyId)
				{
					isAtmStrategyCreated = true;
				}
			});
		}
		if (!isAtmStrategyCreated)
		{
			return;
		}
		if (orderId.Length > 0)
		{
			string[] atmStrategyEntryOrderStatus = ((StrategyBase)this).GetAtmStrategyEntryOrderStatus(orderId);
			if (atmStrategyEntryOrderStatus.GetLength(0) > 0)
			{
				((NinjaScript)this).Print((object)("The entry order average fill price is: " + atmStrategyEntryOrderStatus[0]));
				((NinjaScript)this).Print((object)("The entry order filled amount is: " + atmStrategyEntryOrderStatus[1]));
				((NinjaScript)this).Print((object)("The entry order order state is: " + atmStrategyEntryOrderStatus[2]));
				if (atmStrategyEntryOrderStatus[2] == "Filled" || atmStrategyEntryOrderStatus[2] == "Cancelled" || atmStrategyEntryOrderStatus[2] == "Rejected")
				{
					orderId = string.Empty;
				}
			}
		}
		else if (atmStrategyId.Length > 0 && (int)((StrategyBase)this).GetAtmStrategyMarketPosition(atmStrategyId) == 2)
		{
			atmStrategyId = string.Empty;
		}
		if (atmStrategyId.Length > 0)
		{
			if ((int)((StrategyBase)this).GetAtmStrategyMarketPosition(atmStrategyId) != 2)
			{
				((StrategyBase)this).AtmStrategyChangeStopTarget(0.0, ((NinjaScriptBase)this).Low[0] - 3.0 * ((NinjaScriptBase)this).TickSize, "STOP1", atmStrategyId);
			}
			((NinjaScript)this).Print((object)$"The current ATM Strategy market position is: {((StrategyBase)this).GetAtmStrategyMarketPosition(atmStrategyId)}");
			((NinjaScript)this).Print((object)$"The current ATM Strategy position quantity is: {((StrategyBase)this).GetAtmStrategyPositionQuantity(atmStrategyId)}");
			((NinjaScript)this).Print((object)$"The current ATM Strategy average price is: {((StrategyBase)this).GetAtmStrategyPositionAveragePrice(atmStrategyId)}");
			((NinjaScript)this).Print((object)$"The current ATM Strategy Unrealized PnL is: {((StrategyBase)this).GetAtmStrategyUnrealizedProfitLoss(atmStrategyId)}");
		}
	}
}

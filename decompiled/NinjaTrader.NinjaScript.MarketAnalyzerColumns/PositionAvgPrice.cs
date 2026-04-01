using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class PositionAvgPrice : MarketAnalyzerColumn
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
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionPositionAvgPrice;
			((MarketAnalyzerColumnBase)this).FormatDecimals = 5;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNamePositionAvgPrice;
			base.IsDataSeriesRequired = false;
		}
	}

	protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
	{
		((MarketAnalyzerColumnBase)this).BackColor = null;
		((MarketAnalyzerColumnBase)this).CurrentValue = 0.0;
		Account val = null;
		Position val2 = null;
		lock (Account.All)
		{
			val = Account.All.FirstOrDefault((Account o) => o.DisplayName == AccountName);
		}
		if (val != null)
		{
			lock (val.Positions)
			{
				val2 = val.Positions.FirstOrDefault((Position o) => o.Instrument.FullName == ((NinjaScriptBase)this).Instrument.FullName);
			}
		}
		if (val2 != null)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = val2.AveragePrice;
		}
	}

	protected override void OnPositionUpdate(PositionEventArgs positionUpdate)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		if (positionUpdate.Position.Instrument == ((NinjaScriptBase)this).Instrument && positionUpdate.Position.Account.DisplayName == AccountName)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = (((int)positionUpdate.Operation == 2) ? 0.0 : positionUpdate.AveragePrice);
		}
	}
}

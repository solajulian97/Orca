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

public class UnrealizedProfitLoss : MarketAnalyzerColumn
{
	private Currency accountDenomination = (Currency)7;

	private Position position;

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
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionUnrealizedProfitLoss;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNameUnrealizedProfitLoss;
			base.IsDataSeriesRequired = false;
			((MarketAnalyzerColumnBase)this).ShowInTotalRow = true;
		}
	}

	protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Invalid comparison between Unknown and I4
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if ((int)connectionStatusUpdate.Status == 3 && (int)connectionStatusUpdate.PreviousStatus == 4)
		{
			Account val = null;
			lock (connectionStatusUpdate.Connection.Accounts)
			{
				val = connectionStatusUpdate.Connection.Accounts.FirstOrDefault((Account o) => o.DisplayName == AccountName);
			}
			if (val == null)
			{
				return;
			}
			accountDenomination = val.Denomination;
			lock (val.Positions)
			{
				position = val.Positions.FirstOrDefault((Position o) => o.Instrument.FullName == ((NinjaScriptBase)this).Instrument.FullName);
				return;
			}
		}
		if ((int)connectionStatusUpdate.Status == 0 && (int)connectionStatusUpdate.PreviousStatus == 1 && position != null && position.Account.Connection == connectionStatusUpdate.Connection)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = 0.0;
			position = null;
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		((MarketAnalyzerColumnBase)this).CurrentValue = ((position == null) ? 0.0 : position.GetUnrealizedProfitLoss((PerformanceUnit)0, double.MinValue));
	}

	protected override void OnPositionUpdate(PositionEventArgs positionUpdate)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		if (positionUpdate.Position.Account.DisplayName == AccountName && positionUpdate.Position.Instrument == ((NinjaScriptBase)this).Instrument)
		{
			position = (((int)positionUpdate.Operation == 2) ? null : positionUpdate.Position);
			((MarketAnalyzerColumnBase)this).CurrentValue = ((position == null) ? 0.0 : position.GetUnrealizedProfitLoss((PerformanceUnit)0, double.MinValue));
			accountDenomination = positionUpdate.Position.Account.Denomination;
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

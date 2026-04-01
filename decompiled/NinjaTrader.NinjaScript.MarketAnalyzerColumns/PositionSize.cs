using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class PositionSize : MarketAnalyzerColumn
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
			((NinjaScript)this).Description = Resource.NinjaScriptMarketAnalyzerColumnDescriptionPositionSize;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptMarketAnalyzerColumnNamePositionSize;
			base.IsDataSeriesRequired = false;
			((MarketAnalyzerColumnBase)this).FormatDecimals = 0;
		}
	}

	protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
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
			((MarketAnalyzerColumnBase)this).CurrentValue = (((int)val2.MarketPosition == 0) ? 1 : (-1)) * val2.Quantity;
		}
	}

	protected override void OnPositionUpdate(PositionEventArgs positionUpdate)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (positionUpdate.Position.Instrument == ((NinjaScriptBase)this).Instrument && positionUpdate.Position.Account.DisplayName == AccountName)
		{
			((MarketAnalyzerColumnBase)this).CurrentValue = (((int)positionUpdate.Operation != 2) ? ((((int)positionUpdate.Position.MarketPosition == 0) ? 1 : (-1)) * positionUpdate.Position.Quantity) : 0);
		}
	}

	public override string Format(double value)
	{
		if (((MarketAnalyzerColumnBase)this).CellConditions.Count == 0)
		{
			((MarketAnalyzerColumnBase)this).BackColor = ((value == 0.0) ? null : ((value > 0.0) ? (Application.Current.FindResource("LongBackground") as Brush) : (Application.Current.FindResource("ShortBackground") as Brush)));
		}
		if (value != 0.0)
		{
			return Globals.FormatQuantity((long)Math.Abs(value), false);
		}
		return string.Empty;
	}
}

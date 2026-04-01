using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Displays net change on the chart.
/// </summary>
public class NetChangeDisplay : Indicator
{
	private Account account;

	private double currentValue;

	private double lastValue;

	[XmlIgnore]
	[Browsable(false)]
	public double NetChange => currentValue;

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Unit", GroupName = "NinjaScriptParameters", Order = 0)]
	public PerformanceUnit Unit { get; set; }

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "PositiveColor", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1810)]
	public Brush PositiveBrush { get; set; }

	[Browsable(false)]
	public string PositiveBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(PositiveBrush);
		}
		set
		{
			PositiveBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NegativeColor", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1820)]
	public Brush NegativeBrush { get; set; }

	[Browsable(false)]
	public string NegativeBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(NegativeBrush);
		}
		set
		{
			NegativeBrush = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Location", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1830)]
	public NetChangePosition Location { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "Font", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1800)]
	public SimpleFont Font { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionNetChangeDisplay;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameNetChangeDisplay;
			((NinjaScriptBase)this).Calculate = (Calculate)2;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Unit = (PerformanceUnit)1;
			PositiveBrush = Brushes.LimeGreen;
			NegativeBrush = Brushes.Red;
			Location = NetChangePosition.TopRight;
			Font = new SimpleFont("Arial", 18);
		}
	}

	private TextPosition GetTextPosition(NetChangePosition ncp)
	{
		return ncp switch
		{
			NetChangePosition.BottomLeft => TextPosition.BottomLeft, 
			NetChangePosition.BottomRight => TextPosition.BottomRight, 
			NetChangePosition.TopLeft => TextPosition.TopLeft, 
			NetChangePosition.TopRight => TextPosition.TopRight, 
			_ => TextPosition.TopRight, 
		};
	}

	protected override void OnBarUpdate()
	{
	}

	protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Invalid comparison between Unknown and I4
		if ((int)connectionStatusUpdate.PriceStatus == 3 && (int)connectionStatusUpdate.PreviousStatus == 4 && connectionStatusUpdate.Connection.Accounts.Count > 0 && account == null)
		{
			account = connectionStatusUpdate.Connection.Accounts[0];
		}
		else if ((int)connectionStatusUpdate.Status == 0 && (int)connectionStatusUpdate.PreviousStatus == 1 && account != null && account.Connection == connectionStatusUpdate.Connection)
		{
			account = null;
		}
	}

	protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Invalid comparison between Unknown and I4
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Expected I4, but got Unknown
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Invalid comparison between Unknown and I4
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Invalid comparison between Unknown and I4
		if (marketDataUpdate.IsReset)
		{
			currentValue = double.MinValue;
			if (Math.Abs(lastValue - currentValue) > 1E-18)
			{
				Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", FormatValue(currentValue), GetTextPosition(Location), (currentValue >= 0.0) ? PositiveBrush : NegativeBrush, Font, Brushes.Transparent, Brushes.Transparent, 0);
				lastValue = currentValue;
			}
		}
		else
		{
			if ((int)marketDataUpdate.MarketDataType != 2 || marketDataUpdate.Instrument.MarketData.LastClose == null)
			{
				return;
			}
			double num = 0.0;
			if (account != null)
			{
				bool flag = default(bool);
				num = marketDataUpdate.Instrument.GetConversionRate((MarketDataType)1, account.Denomination, ref flag);
			}
			PerformanceUnit unit = Unit;
			double num2;
			switch ((int)unit)
			{
			case 1:
				num2 = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / marketDataUpdate.Instrument.MarketData.LastClose.Price;
				break;
			case 2:
				num2 = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize * (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 4) ? 0.1 : 1.0);
				break;
			case 4:
				num2 = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / ((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize;
				break;
			case 0:
			{
				double num3 = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) * ((NinjaScriptBase)this).Instrument.MasterInstrument.PointValue * num;
				int num4;
				if ((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType != 4)
				{
					num4 = 1;
				}
				else
				{
					Account obj = account;
					num4 = ((obj != null) ? obj.ForexLotSize : 1000);
				}
				num2 = num3 * (double)num4;
				break;
			}
			case 3:
				num2 = marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price;
				break;
			default:
				num2 = currentValue;
				break;
			}
			currentValue = num2;
			if (Math.Abs(lastValue - currentValue) > 1E-18)
			{
				Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", FormatValue(currentValue), GetTextPosition(Location), (currentValue >= 0.0) ? PositiveBrush : NegativeBrush, Font, Brushes.Transparent, Brushes.Transparent, 0);
				lastValue = currentValue;
			}
		}
	}

	public string FormatValue(double value)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected I4, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		if (value <= double.MinValue)
		{
			return string.Empty;
		}
		PerformanceUnit unit = Unit;
		switch ((int)unit)
		{
		case 0:
		{
			Account obj = account;
			Currency val = ((obj != null) ? obj.Denomination : ((NinjaScriptBase)this).Instrument.MasterInstrument.Currency);
			return Globals.FormatCurrency(value, val);
		}
		case 3:
			return value.ToString(Globals.GetTickFormatString(((NinjaScriptBase)this).Instrument.MasterInstrument.TickSize), Globals.GeneralOptions.CurrentCulture);
		case 1:
			return value.ToString("P", Globals.GeneralOptions.CurrentCulture);
		case 2:
		{
			CultureInfo cultureInfo = Globals.GeneralOptions.CurrentCulture.Clone() as CultureInfo;
			if (cultureInfo != null)
			{
				cultureInfo.NumberFormat.NumberDecimalSeparator = "'";
			}
			return (Math.Round(value * 10.0) / 10.0).ToString("0.0", cultureInfo);
		}
		case 4:
			return Math.Round(value).ToString(Globals.GeneralOptions.CurrentCulture);
		default:
			return "0";
		}
	}
}

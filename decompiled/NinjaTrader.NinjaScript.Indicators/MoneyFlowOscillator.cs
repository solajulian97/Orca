using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Money Flow Oscillator measures the amount of money flow volume over a specific period. A move into positive territory indicates buying pressure while a move into negative territory indicates selling pressure.
/// </summary>
public class MoneyFlowOscillator : Indicator
{
	private Series<double> mfv;

	private double dvs;

	private double mltp;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> MoneyFlow => ((NinjaScriptBase)this).Values[0];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionMoneyFlowOscillator;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameMoneyFlowOscillator;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 20;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorMoneyFlowLine);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			mfv = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Invalid comparison between Unknown and I4
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).CurrentBar > 0)
		{
			dvs = ((((NinjaScriptBase)this).High[0] - ((NinjaScriptBase)this).Low[1] + (((NinjaScriptBase)this).High[1] - ((NinjaScriptBase)this).Low[0]) == 0.0) ? 1E-05 : (((NinjaScriptBase)this).High[0] - ((NinjaScriptBase)this).Low[1] + (((NinjaScriptBase)this).High[1] - ((NinjaScriptBase)this).Low[0])));
			mltp = Math.Round((((NinjaScriptBase)this).High[0] < ((NinjaScriptBase)this).Low[1]) ? (-1.0) : ((((NinjaScriptBase)this).Low[0] > ((NinjaScriptBase)this).High[1]) ? 1.0 : ((((NinjaScriptBase)this).High[0] - ((NinjaScriptBase)this).Low[1] - (((NinjaScriptBase)this).High[1] - ((NinjaScriptBase)this).Low[0])) / dvs)), 2);
			mfv[0] = mltp * (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume((long)((NinjaScriptBase)this).Volume[0]) : ((NinjaScriptBase)this).Volume[0]);
			if (((NinjaScriptBase)this).CurrentBar >= Period)
			{
				double num = ((NinjaScriptBase)SUM((ISeries<double>)(object)((NinjaScriptBase)this).Volume, Period))[0];
				if ((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7)
				{
					num = Globals.ToCryptocurrencyVolume((long)num);
				}
				((NinjaScriptBase)this).Values[0][0] = Math.Round(((NinjaScriptBase)SUM((ISeries<double>)(object)mfv, Period))[0] / num, 3);
			}
		}
		else
		{
			mfv[0] = 0.0;
		}
	}
}

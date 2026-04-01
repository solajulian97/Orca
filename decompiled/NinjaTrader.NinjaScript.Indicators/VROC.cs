using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The VROC (Volume Rate-of-Change) shows whether or not a volume trend is
/// developing in either an up or down direction. It is similar to the ROC
/// indicator, but is applied to volume instead.
/// </summary>
public class VROC : Indicator
{
	private Series<double> smaVolume;

	private SMA sma;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Smooth { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Invalid comparison between Unknown and I4
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Invalid comparison between Unknown and I4
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionVROC;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameVROC;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			Period = 14;
			Smooth = 3;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.NinjaScriptIndicatorNameVROC);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 0.0, Resource.NinjaScriptIndicatorZeroLine);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			smaVolume = new Series<double>((NinjaScriptBase)(object)this);
			sma = SMA((ISeries<double>)(object)smaVolume, Smooth);
		}
		else if ((int)((NinjaScript)this).State == 5 && (int)((NinjaScriptBase)this).Calculate == 2)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), TextPosition.BottomRight);
			NinjaScript.Log(string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), (LogLevel)3);
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Invalid comparison between Unknown and I4
		double num = ((NinjaScriptBase)this).Volume[Math.Min(((NinjaScriptBase)this).CurrentBar, Period - 1)];
		double num2 = ((NinjaScriptBase)this).Volume[0];
		if ((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7)
		{
			num = Globals.ToCryptocurrencyVolume((long)num);
			num2 = Globals.ToCryptocurrencyVolume((long)num2);
		}
		smaVolume[0] = 100.0 * num2 / ((num == 0.0) ? 1.0 : num) - 100.0;
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)sma)[0];
	}
}

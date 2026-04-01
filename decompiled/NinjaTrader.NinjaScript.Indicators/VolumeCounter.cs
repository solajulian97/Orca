using System.ComponentModel.DataAnnotations;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

public class VolumeCounter : Indicator
{
	private double volume;

	private bool isVolume;

	private bool isVolumeBase;

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "CountDown", GroupName = "NinjaScriptParameters", Order = 0)]
	public bool CountDown { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "ShowPercent", GroupName = "NinjaScriptParameters", Order = 0)]
	public bool ShowPercent { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "GuiPropertyNameTextPosition", GroupName = "PropertyCategoryVisual", Order = 70)]
	public TextPositionFine TextPositionFine { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Invalid comparison between Unknown and I4
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Invalid comparison between Unknown and I4
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Invalid comparison between Unknown and I4
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Invalid comparison between Unknown and I4
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionVolumeCounter;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameVolumeCounter;
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			CountDown = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((IndicatorBase)this).IsChartOnly = true;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			ShowPercent = true;
			TextPositionFine = TextPositionFine.BottomRight;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			isVolume = (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 1;
			isVolumeBase = ((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 9 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 16 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 14) && (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType == 1;
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		volume = (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume((long)((NinjaScriptBase)this).Volume[0]) : ((NinjaScriptBase)this).Volume[0]);
		double num = ((!ShowPercent) ? (CountDown ? ((double)(isVolumeBase ? ((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodValue : ((NinjaScriptBase)this).BarsPeriod.Value) - volume) : volume) : (CountDown ? ((1.0 - ((NinjaScriptBase)this).Bars.PercentComplete) * 100.0) : (((NinjaScriptBase)this).Bars.PercentComplete * 100.0)));
		string text = ((isVolume || isVolumeBase) ? ((CountDown ? (Resource.VolumeCounterVolumeRemaining + num) : (Resource.VolumeCounterVolumeCount + num)) + (ShowPercent ? "%" : "")) : Resource.VolumeCounterBarError);
		Draw.TextFixedFine((NinjaScriptBase)(object)this, "NinjaScriptInfo", text, TextPositionFine);
	}
}

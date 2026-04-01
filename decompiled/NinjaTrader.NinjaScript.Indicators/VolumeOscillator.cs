using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Volume Oscillator measures volume by calculating the difference of a fast and
/// a slow moving average of volume. The Volume Oscillator can provide insight into the
/// strength or weakness of a price trend. A positive value suggests there is enough
/// market support to continue driving price activity in the direction of the current
/// trend. A negative value suggests there is a lack of support, that prices may begin
/// to become stagnant or reverse.
/// </summary>
public class VolumeOscillator : Indicator
{
	private SMA smaFast;

	private SMA smaSlow;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Fast", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Fast { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Slow", GroupName = "NinjaScriptParameters", Order = 1)]
	public int Slow { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Invalid comparison between Unknown and I4
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionVolumeOscillator;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameVolumeOscillator;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			Fast = 12;
			Slow = 26;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, 2f), (PlotStyle)0, Resource.NinjaScriptIndicatorNameVolumeOscillator);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			smaFast = SMA((ISeries<double>)(object)((NinjaScriptBase)this).Volume, Fast);
			smaSlow = SMA((ISeries<double>)(object)((NinjaScriptBase)this).Volume, Slow);
		}
		else if ((int)((NinjaScript)this).State == 5 && (int)((NinjaScriptBase)this).Calculate == 2)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), TextPosition.BottomRight);
			NinjaScript.Log(string.Format(Resource.NinjaScriptOnPriceChangeError, ((NinjaScriptBase)this).Name), (LogLevel)3);
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		double num = ((NinjaScriptBase)smaFast)[0] - ((NinjaScriptBase)smaSlow)[0];
		if ((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7)
		{
			num = Globals.ToCryptocurrencyVolume((long)num);
		}
		((NinjaScriptBase)this).Value[0] = num;
	}
}

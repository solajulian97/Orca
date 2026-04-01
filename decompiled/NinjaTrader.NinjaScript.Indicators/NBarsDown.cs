using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// This indicator returns 1 when we have n of consecutive bars down, otherwise returns 0.
/// A down bar is defined as a bar where the close is below the open and the bars makes a
/// lower high and a lower low. You can adjust the specific requirements with the indicator options.
/// </summary>
public class NBarsDown : Indicator
{
	[Range(2, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "BarCount", GroupName = "NinjaScriptParameters", Order = 0)]
	public int BarCount { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "BarDown", GroupName = "NinjaScriptParameters", Order = 1)]
	public bool BarDown { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "LowerHigh", GroupName = "NinjaScriptParameters", Order = 2)]
	public bool LowerHigh { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "LowerLow", GroupName = "NinjaScriptParameters", Order = 3)]
	public bool LowerLow { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionNBarsDown;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameNBarsDown;
			BarCount = 3;
			BarDown = true;
			LowerHigh = true;
			LowerLow = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Crimson, 2f), (PlotStyle)0, Resource.NBarsDownTrigger);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar < BarCount)
		{
			((NinjaScriptBase)this).Value[0] = 0.0;
			return;
		}
		bool flag = false;
		for (int i = 0; i < BarCount + 1; i++)
		{
			if (i == BarCount)
			{
				flag = true;
				break;
			}
			if (!(((NinjaScriptBase)this).Close[i] < ((NinjaScriptBase)this).Close[i + 1]) || (BarDown && !(((NinjaScriptBase)this).Close[i] < ((NinjaScriptBase)this).Open[i])) || (LowerHigh && !(((NinjaScriptBase)this).High[i] < ((NinjaScriptBase)this).High[i + 1])) || (LowerLow && !(((NinjaScriptBase)this).Low[i] < ((NinjaScriptBase)this).Low[i + 1])))
			{
				break;
			}
		}
		((NinjaScriptBase)this).Value[0] = (flag ? 1 : 0);
	}
}

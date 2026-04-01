using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

public class PsychologicalLine : Indicator
{
	private double prevUpBars;

	private int saveCurrentBar;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionPsychologicalLine;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNamePsychologicalLine;
			((NinjaScriptBase)this).IsOverlay = false;
			Period = 10;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorNamePsychologicalLine);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, 75.0, Resource.NinjaScriptIndicatorOverBoughtLine);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkCyan, 25.0, Resource.NinjaScriptIndicatorOverSoldLine);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar > saveCurrentBar)
		{
			prevUpBars = prevUpBars + (double)((((NinjaScriptBase)this).Close[1] > ((NinjaScriptBase)this).Open[1]) ? 1 : 0) - (double)((((NinjaScriptBase)this).CurrentBar > Period - 1 && ((NinjaScriptBase)this).Close[Period] > ((NinjaScriptBase)this).Open[Period]) ? 1 : 0);
		}
		else if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported && saveCurrentBar < ((NinjaScriptBase)this).CurrentBar)
		{
			prevUpBars = 0.0;
			for (int num = Math.Min(((NinjaScriptBase)this).CurrentBar, Period - 1); num > 0; num--)
			{
				if (((NinjaScriptBase)this).Close[num] > ((NinjaScriptBase)this).Open[num])
				{
					prevUpBars += 1.0;
				}
			}
		}
		((NinjaScriptBase)this).Value[0] = (prevUpBars + (double)((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0]) ? 1 : 0)) / (double)Math.Min(((NinjaScriptBase)this).CurrentBar + 1, Period) * 100.0;
		saveCurrentBar = ((NinjaScriptBase)this).CurrentBar;
	}
}

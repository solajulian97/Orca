using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Minimum shows the minimum of the last n bars.
/// </summary>
public class MIN : Indicator
{
	private int lastBar;

	private double lastMin;

	private double runningMin;

	private int runningBar;

	private int thisBar;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionMIN;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameMIN;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameMIN);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			lastBar = 0;
			lastMin = 0.0;
			runningMin = 0.0;
			runningBar = 0;
			thisBar = 0;
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			runningMin = ((NinjaScriptBase)this).Input[0];
			lastMin = ((NinjaScriptBase)this).Input[0];
			runningBar = 0;
			lastBar = 0;
			thisBar = 0;
			((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)this).Input[0];
			return;
		}
		if (((NinjaScriptBase)this).CurrentBar - runningBar >= Period || ((NinjaScriptBase)this).CurrentBar < thisBar)
		{
			runningMin = double.MaxValue;
			for (int num = Math.Min(((NinjaScriptBase)this).CurrentBar, Period - 1); num > 0; num--)
			{
				if (((NinjaScriptBase)this).Input[num] <= runningMin)
				{
					runningMin = ((NinjaScriptBase)this).Input[num];
					runningBar = ((NinjaScriptBase)this).CurrentBar - num;
				}
			}
		}
		if (thisBar != ((NinjaScriptBase)this).CurrentBar)
		{
			lastMin = runningMin;
			lastBar = runningBar;
			thisBar = ((NinjaScriptBase)this).CurrentBar;
		}
		if (((NinjaScriptBase)this).Input[0] <= lastMin)
		{
			runningMin = ((NinjaScriptBase)this).Input[0];
			runningBar = ((NinjaScriptBase)this).CurrentBar;
		}
		else
		{
			runningMin = lastMin;
			runningBar = lastBar;
		}
		((NinjaScriptBase)this).Value[0] = runningMin;
	}
}

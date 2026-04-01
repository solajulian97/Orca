using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Returns a value of 1 when the current close is less than the prior close after penetrating the highest high of the last n bars.
/// </summary>
public class KeyReversalDown : Indicator
{
	private MAX max;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionKeyReversalDown;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameKeyReversalDown;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 1;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.KeyReversalPlot0);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			max = MAX(((NinjaScriptBase)this).High, Period);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar >= Period + 1)
		{
			((NinjaScriptBase)this).Value[0] = ((((NinjaScriptBase)this).High[0] > ((NinjaScriptBase)max)[1] && ((NinjaScriptBase)this).Close[0] < ((NinjaScriptBase)this).Close[1]) ? 1 : 0);
		}
	}
}

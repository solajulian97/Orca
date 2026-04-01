using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// T3 Moving Average
/// </summary>
public class T3 : Indicator
{
	private ArrayList seriesCollection;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "TCount", GroupName = "NinjaScriptParameters", Order = 1)]
	public int TCount { get; set; }

	[Range(0, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "VFactor", GroupName = "NinjaScriptParameters", Order = 2)]
	public double VFactor { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionT3;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameT3;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			Period = 14;
			TCount = 3;
			VFactor = 0.7;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorNameT3);
		}
	}

	protected override void OnBarUpdate()
	{
		if (TCount == 1)
		{
			CalculateGd(((NinjaScriptBase)this).Inputs[0], ((NinjaScriptBase)this).Values[0]);
			return;
		}
		if (seriesCollection == null)
		{
			seriesCollection = new ArrayList();
			for (int i = 0; i < TCount - 1; i++)
			{
				seriesCollection.Add(new Series<double>((NinjaScriptBase)(object)this));
			}
		}
		CalculateGd(((NinjaScriptBase)this).Inputs[0], (Series<double>)seriesCollection[0]);
		for (int j = 0; j <= seriesCollection.Count - 2; j++)
		{
			CalculateGd((ISeries<double>)(object)(Series<double>)seriesCollection[j], (Series<double>)seriesCollection[j + 1]);
		}
		CalculateGd((ISeries<double>)(object)(Series<double>)seriesCollection[seriesCollection.Count - 1], ((NinjaScriptBase)this).Values[0]);
	}

	private void CalculateGd(ISeries<double> input, Series<double> output)
	{
		output[0] = ((NinjaScriptBase)EMA(input, Period))[0] * (1.0 + VFactor) - ((NinjaScriptBase)EMA((ISeries<double>)(object)EMA(input, Period), Period))[0] * VFactor;
	}
}

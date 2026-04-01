using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

public class Correlation : Indicator
{
	private double avg0;

	private double avg1;

	private SessionIterator sessionIterator;

	private SessionIterator SessionIterator
	{
		get
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Expected O, but got Unknown
			if (sessionIterator == null && ((NinjaScriptBase)this).BarsArray.Length == 2 && ((NinjaScriptBase)this).BarsArray[1] != null)
			{
				sessionIterator = new SessionIterator(((NinjaScriptBase)this).BarsArray[1]);
			}
			return sessionIterator;
		}
	}

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptIndicatorCount", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptMarketAnalyzerColumnNameInstrument", GroupName = "NinjaScriptParameters", Order = 0)]
	[PropertyEditor("NinjaTrader.Gui.Tools.UppercaseTextEditor")]
	public string CorrelationSeries { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Invalid comparison between Unknown and I4
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionCorrelation;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameCorrelation;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = false;
			Period = 10;
			CorrelationSeries = string.Empty;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Goldenrod, 1f), (PlotStyle)6, Resource.NinjaScriptIndicatorNameCorrelation);
		}
		else if ((int)((NinjaScript)this).State == 2 && !string.IsNullOrWhiteSpace(CorrelationSeries))
		{
			((NinjaScriptBase)this).AddDataSeries(CorrelationSeries);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).BarsInProgress == 1)
		{
			avg1 = ((NinjaScriptBase)SMA((ISeries<double>)(object)((NinjaScriptBase)this).BarsArray[1], Period))[0];
		}
		else if (SessionIterator != null && ((NinjaScriptBase)this).CurrentBars[0] >= Period && ((NinjaScriptBase)this).CurrentBars[1] >= Period && (!((NinjaScriptBase)this).Bars.BarsType.IsIntraday || SessionIterator.IsInSession(((NinjaScriptBase)this).Times[0][0], true, true)))
		{
			avg0 = ((NinjaScriptBase)SMA((ISeries<double>)(object)((NinjaScriptBase)this).BarsArray[0], Period))[0];
			double num = 0.0;
			double num2 = 0.0;
			double num3 = 0.0;
			for (int i = 0; i < Period; i++)
			{
				num += (avg0 - ((NinjaScriptBase)this).Inputs[0][i]) * (avg1 - ((NinjaScriptBase)this).Inputs[1][i]);
				num2 += (avg0 - ((NinjaScriptBase)this).Inputs[0][i]) * (avg0 - ((NinjaScriptBase)this).Inputs[0][i]);
				num3 += (avg1 - ((NinjaScriptBase)this).Inputs[1][i]) * (avg1 - ((NinjaScriptBase)this).Inputs[1][i]);
			}
			double num4 = Math.Sqrt(num2) * Math.Sqrt(num3);
			((NinjaScriptBase)this).Value[0] = ((MathExtentions.ApproxCompare(num4, 0.0) == 0) ? 0.0 : (num / num4));
		}
	}
}

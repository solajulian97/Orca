using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Aroon Indicator was developed by Tushar Chande. Its comprised of two plots one
/// measuring the number of periods since the most recent x-period high (Aroon Up) and the
/// other measuring the number of periods since the most recent x-period low (Aroon Down).
/// </summary>
public class Aroon : Indicator
{
	private double runningMax;

	private int runningMaxBar;

	private double runningMin;

	private int runningMinBar;

	private int saveCurrentBar;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Down => ((NinjaScriptBase)this).Values[1];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Period { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Up => ((NinjaScriptBase)this).Values[0];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionAroon;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameAroon;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Period = 14;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DarkCyan, Resource.NinjaScriptIndicatorUp);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.SlateBlue, Resource.NinjaScriptIndicatorDown);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 30.0, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddLine((Brush)Brushes.DarkGray, 70.0, Resource.NinjaScriptIndicatorUpper);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			runningMax = 0.0;
			runningMaxBar = 0;
			runningMin = 0.0;
			runningMinBar = 0;
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((NinjaScriptBase)this).High[0];
		double num2 = ((NinjaScriptBase)this).Low[0];
		if (((NinjaScriptBase)this).CurrentBar == 0)
		{
			Down[0] = 0.0;
			Up[0] = 0.0;
			runningMax = num;
			runningMin = num2;
			runningMaxBar = 0;
			runningMinBar = 0;
			return;
		}
		int num3 = Math.Min(Period, ((NinjaScriptBase)this).CurrentBar);
		if (((NinjaScriptBase)this).CurrentBar - runningMaxBar >= Period || ((NinjaScriptBase)this).CurrentBar < saveCurrentBar)
		{
			runningMax = double.MinValue;
			for (int num4 = num3; num4 > 0; num4--)
			{
				if (((NinjaScriptBase)this).High[num4] >= runningMax)
				{
					runningMax = ((NinjaScriptBase)this).High[num4];
					runningMaxBar = ((NinjaScriptBase)this).CurrentBar - num4;
				}
			}
		}
		if (((NinjaScriptBase)this).CurrentBar - runningMinBar >= Period || ((NinjaScriptBase)this).CurrentBar < saveCurrentBar)
		{
			runningMin = double.MaxValue;
			for (int num5 = num3; num5 > 0; num5--)
			{
				if (((NinjaScriptBase)this).Low[num5] <= runningMin)
				{
					runningMin = ((NinjaScriptBase)this).Low[num5];
					runningMinBar = ((NinjaScriptBase)this).CurrentBar - num5;
				}
			}
		}
		if (num >= runningMax)
		{
			runningMax = num;
			runningMaxBar = ((NinjaScriptBase)this).CurrentBar;
		}
		if (num2 <= runningMin)
		{
			runningMin = num2;
			runningMinBar = ((NinjaScriptBase)this).CurrentBar;
		}
		saveCurrentBar = ((NinjaScriptBase)this).CurrentBar;
		Up[0] = 100.0 * ((double)(num3 - (((NinjaScriptBase)this).CurrentBar - runningMaxBar)) / (double)num3);
		Down[0] = 100.0 * ((double)(num3 - (((NinjaScriptBase)this).CurrentBar - runningMinBar)) / (double)num3);
	}
}

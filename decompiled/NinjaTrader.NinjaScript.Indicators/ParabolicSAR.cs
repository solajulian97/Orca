using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Parabolic SAR according to Stocks and Commodities magazine V 11:11 (477-479).
/// </summary>
public class ParabolicSAR : Indicator
{
	private double af;

	private bool afIncreased;

	private bool longPosition;

	private int prevBar;

	private double prevSar;

	private int reverseBar;

	private double reverseValue;

	private double todaySar;

	private double xp;

	private Series<double> afSeries;

	private Series<bool> afIncreasedSeries;

	private Series<bool> longPositionSeries;

	private Series<int> prevBarSeries;

	private Series<double> prevSarSeries;

	private Series<int> reverseBarSeries;

	private Series<double> reverseValueSeries;

	private Series<double> todaySarSeries;

	private Series<double> xpSeries;

	private ISeries<double> high;

	private ISeries<double> low;

	[Range(0.0, double.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Acceleration", GroupName = "NinjaScriptParameters", Order = 0)]
	public double Acceleration { get; set; }

	[Range(0.001, double.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "AccelerationMax", GroupName = "NinjaScriptParameters", Order = 1)]
	public double AccelerationMax { get; set; }

	[Range(0.001, double.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "AccelerationStep", GroupName = "NinjaScriptParameters", Order = 2)]
	public double AccelerationStep { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Invalid comparison between Unknown and I4
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionParabolicSAR;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameParabolicSAR;
			Acceleration = 0.02;
			AccelerationStep = 0.02;
			AccelerationMax = 0.2;
			((NinjaScriptBase)this).Calculate = (Calculate)2;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Goldenrod, 2f), (PlotStyle)3, Resource.NinjaScriptIndicatorNameParabolicSAR);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			xp = 0.0;
			af = 0.0;
			todaySar = 0.0;
			prevSar = 0.0;
			reverseBar = 0;
			reverseValue = 0.0;
			prevBar = 0;
			afIncreased = false;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported)
			{
				afSeries = new Series<double>((NinjaScriptBase)(object)this);
				afIncreasedSeries = new Series<bool>((NinjaScriptBase)(object)this);
				longPositionSeries = new Series<bool>((NinjaScriptBase)(object)this);
				prevBarSeries = new Series<int>((NinjaScriptBase)(object)this);
				prevSarSeries = new Series<double>((NinjaScriptBase)(object)this);
				reverseBarSeries = new Series<int>((NinjaScriptBase)(object)this);
				reverseValueSeries = new Series<double>((NinjaScriptBase)(object)this);
				todaySarSeries = new Series<double>((NinjaScriptBase)(object)this);
				xpSeries = new Series<double>((NinjaScriptBase)(object)this);
			}
			high = ((((NinjaScriptBase)this).Input is NinjaScriptBase) ? ((NinjaScriptBase)this).Input : ((NinjaScriptBase)this).High);
			low = ((((NinjaScriptBase)this).Input is NinjaScriptBase) ? ((NinjaScriptBase)this).Input : ((NinjaScriptBase)this).Low);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar < 3)
		{
			return;
		}
		if (((NinjaScriptBase)this).CurrentBar == 3)
		{
			longPosition = high[0] > high[1];
			xp = (longPosition ? ((NinjaScriptBase)MAX(high, ((NinjaScriptBase)this).CurrentBar))[0] : ((NinjaScriptBase)MIN(low, ((NinjaScriptBase)this).CurrentBar))[0]);
			af = Acceleration;
			((NinjaScriptBase)this).Value[0] = xp + (double)((!longPosition) ? 1 : (-1)) * ((((NinjaScriptBase)MAX(high, ((NinjaScriptBase)this).CurrentBar))[0] - ((NinjaScriptBase)MIN(low, ((NinjaScriptBase)this).CurrentBar))[0]) * af);
			return;
		}
		if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported && ((NinjaScriptBase)this).CurrentBar < prevBar)
		{
			af = afSeries[0];
			afIncreased = afIncreasedSeries[0];
			longPosition = longPositionSeries[0];
			prevBar = prevBarSeries[0];
			prevSar = prevSarSeries[0];
			reverseBar = reverseBarSeries[0];
			reverseValue = reverseValueSeries[0];
			todaySar = todaySarSeries[0];
			xp = xpSeries[0];
		}
		if (afIncreased && prevBar != ((NinjaScriptBase)this).CurrentBar)
		{
			afIncreased = false;
		}
		if (reverseBar != ((NinjaScriptBase)this).CurrentBar)
		{
			todaySar = TodaySar(((NinjaScriptBase)this).Value[1] + af * (xp - ((NinjaScriptBase)this).Value[1]));
			for (int i = 1; i <= 2; i++)
			{
				if (longPosition)
				{
					if (todaySar > low[i])
					{
						todaySar = low[i];
					}
				}
				else if (todaySar < high[i])
				{
					todaySar = high[i];
				}
			}
			if (longPosition)
			{
				if (prevBar != ((NinjaScriptBase)this).CurrentBar || low[0] < prevSar)
				{
					((NinjaScriptBase)this).Value[0] = todaySar;
					prevSar = todaySar;
				}
				else
				{
					((NinjaScriptBase)this).Value[0] = prevSar;
				}
				if (high[0] > xp)
				{
					xp = high[0];
					AfIncrease();
				}
			}
			else if (!longPosition)
			{
				if (prevBar != ((NinjaScriptBase)this).CurrentBar || high[0] > prevSar)
				{
					((NinjaScriptBase)this).Value[0] = todaySar;
					prevSar = todaySar;
				}
				else
				{
					((NinjaScriptBase)this).Value[0] = prevSar;
				}
				if (low[0] < xp)
				{
					xp = low[0];
					AfIncrease();
				}
			}
		}
		else
		{
			if (longPosition && high[0] > xp)
			{
				xp = high[0];
			}
			else if (!longPosition && low[0] < xp)
			{
				xp = low[0];
			}
			((NinjaScriptBase)this).Value[0] = prevSar;
			todaySar = TodaySar(longPosition ? Math.Min(reverseValue, low[0]) : Math.Max(reverseValue, high[0]));
		}
		prevBar = ((NinjaScriptBase)this).CurrentBar;
		if ((longPosition && (low[0] < todaySar || low[1] < todaySar)) || (!longPosition && (high[0] > todaySar || high[1] > todaySar)))
		{
			((NinjaScriptBase)this).Value[0] = Reverse();
		}
		if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported)
		{
			afSeries[0] = af;
			afIncreasedSeries[0] = afIncreased;
			longPositionSeries[0] = longPosition;
			prevBarSeries[0] = prevBar;
			prevSarSeries[0] = prevSar;
			reverseBarSeries[0] = reverseBar;
			reverseValueSeries[0] = reverseValue;
			todaySarSeries[0] = todaySar;
			xpSeries[0] = xp;
		}
	}

	private void AfIncrease()
	{
		if (!afIncreased)
		{
			af = Math.Min(AccelerationMax, af + AccelerationStep);
			afIncreased = true;
		}
	}

	private double TodaySar(double tSar)
	{
		if (longPosition)
		{
			double num = Math.Min(Math.Min(tSar, low[0]), low[1]);
			if (low[0] > num)
			{
				tSar = num;
			}
		}
		else
		{
			double num2 = Math.Max(Math.Max(tSar, high[0]), high[1]);
			if (high[0] < num2)
			{
				tSar = num2;
			}
		}
		return tSar;
	}

	private double Reverse()
	{
		double result = xp;
		if ((longPosition && prevSar > low[0]) || (!longPosition && prevSar < high[0]) || prevBar != ((NinjaScriptBase)this).CurrentBar)
		{
			longPosition = !longPosition;
			reverseBar = ((NinjaScriptBase)this).CurrentBar;
			reverseValue = xp;
			af = Acceleration;
			xp = (longPosition ? high[0] : low[0]);
			prevSar = result;
		}
		else
		{
			result = prevSar;
		}
		return result;
	}
}

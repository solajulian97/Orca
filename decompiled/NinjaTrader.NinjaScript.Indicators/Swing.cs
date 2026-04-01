using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// The Swing indicator plots lines that represents the swing high and low points.
/// </summary>
public class Swing : Indicator
{
	private int constant;

	private double currentSwingHigh;

	private double currentSwingLow;

	private ArrayList lastHighCache;

	private double lastSwingHighValue;

	private ArrayList lastLowCache;

	private double lastSwingLowValue;

	private int saveCurrentBar;

	private Series<double> swingHighSeries;

	private Series<double> swingHighSwings;

	private Series<double> swingLowSeries;

	private Series<double> swingLowSwings;

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Strength", GroupName = "NinjaScriptParameters", Order = 0)]
	public int Strength { get; set; }

	/// <summary>
	/// Gets the high swings.
	/// </summary>
	[Browsable(false)]
	[XmlIgnore]
	public Series<double> SwingHigh
	{
		get
		{
			((NinjaScriptBase)this).Update();
			return swingHighSeries;
		}
	}

	private Series<double> SwingHighPlot
	{
		get
		{
			((NinjaScriptBase)this).Update();
			return ((NinjaScriptBase)this).Values[0];
		}
	}

	/// <summary>
	/// Gets the low swings.
	/// </summary>
	[Browsable(false)]
	[XmlIgnore]
	public Series<double> SwingLow
	{
		get
		{
			((NinjaScriptBase)this).Update();
			return swingLowSeries;
		}
	}

	private Series<double> SwingLowPlot
	{
		get
		{
			((NinjaScriptBase)this).Update();
			return ((NinjaScriptBase)this).Values[1];
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Invalid comparison between Unknown and I4
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionSwing;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameSwing;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).PaintPriceMarkers = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			Strength = 5;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DarkCyan, 2f), (PlotStyle)3, Resource.SwingHigh);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Goldenrod, 2f), (PlotStyle)3, Resource.SwingLow);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			currentSwingHigh = 0.0;
			currentSwingLow = 0.0;
			lastSwingHighValue = 0.0;
			lastSwingLowValue = 0.0;
			saveCurrentBar = -1;
			constant = 2 * Strength + 1;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			lastHighCache = new ArrayList();
			lastLowCache = new ArrayList();
			swingHighSeries = new Series<double>((NinjaScriptBase)(object)this);
			swingHighSwings = new Series<double>((NinjaScriptBase)(object)this);
			swingLowSeries = new Series<double>((NinjaScriptBase)(object)this);
			swingLowSwings = new Series<double>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = ((!(((NinjaScriptBase)this).Input is PriceSeries) && !(((NinjaScriptBase)this).Input is Bars)) ? ((NinjaScriptBase)this).Input[0] : ((NinjaScriptBase)this).High[0]);
		double num2 = ((!(((NinjaScriptBase)this).Input is PriceSeries) && !(((NinjaScriptBase)this).Input is Bars)) ? ((NinjaScriptBase)this).Input[0] : ((NinjaScriptBase)this).Low[0]);
		double num3 = ((!(((NinjaScriptBase)this).Input is PriceSeries) && !(((NinjaScriptBase)this).Input is Bars)) ? ((NinjaScriptBase)this).Input[0] : ((NinjaScriptBase)this).Close[0]);
		if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported && ((NinjaScriptBase)this).CurrentBar < saveCurrentBar)
		{
			currentSwingHigh = (SwingHighPlot.IsValidDataPoint(0) ? SwingHighPlot[0] : 0.0);
			currentSwingLow = (SwingLowPlot.IsValidDataPoint(0) ? SwingLowPlot[0] : 0.0);
			lastSwingHighValue = swingHighSeries[0];
			lastSwingLowValue = swingLowSeries[0];
			swingHighSeries[Strength] = 0.0;
			swingLowSeries[Strength] = 0.0;
			lastHighCache.Clear();
			lastLowCache.Clear();
			for (int num4 = Math.Min(((NinjaScriptBase)this).CurrentBar, constant) - 1; num4 >= 0; num4--)
			{
				ArrayList arrayList = lastHighCache;
				ISeries<double> input = ((NinjaScriptBase)this).Input;
				bool flag = ((input is PriceSeries || input is Bars) ? true : false);
				arrayList.Add((!flag) ? ((NinjaScriptBase)this).Input[num4] : ((NinjaScriptBase)this).High[num4]);
				ArrayList arrayList2 = lastLowCache;
				input = ((NinjaScriptBase)this).Input;
				flag = ((input is PriceSeries || input is Bars) ? true : false);
				arrayList2.Add((!flag) ? ((NinjaScriptBase)this).Input[num4] : ((NinjaScriptBase)this).Low[num4]);
			}
			saveCurrentBar = ((NinjaScriptBase)this).CurrentBar;
		}
		else if (saveCurrentBar != ((NinjaScriptBase)this).CurrentBar)
		{
			swingHighSwings[0] = 0.0;
			swingLowSwings[0] = 0.0;
			swingHighSeries[0] = 0.0;
			swingLowSeries[0] = 0.0;
			lastHighCache.Add(num);
			if (lastHighCache.Count > constant)
			{
				lastHighCache.RemoveAt(0);
			}
			lastLowCache.Add(num2);
			if (lastLowCache.Count > constant)
			{
				lastLowCache.RemoveAt(0);
			}
			if (lastHighCache.Count == constant)
			{
				bool flag2 = true;
				double num5 = (double)lastHighCache[Strength];
				for (int i = 0; i < Strength; i++)
				{
					if (MathExtentions.ApproxCompare((double)lastHighCache[i], num5) >= 0)
					{
						flag2 = false;
					}
				}
				for (int j = Strength + 1; j < lastHighCache.Count; j++)
				{
					if (MathExtentions.ApproxCompare((double)lastHighCache[j], num5) > 0)
					{
						flag2 = false;
					}
				}
				swingHighSwings[Strength] = (flag2 ? num5 : 0.0);
				if (flag2)
				{
					lastSwingHighValue = num5;
				}
				if (flag2)
				{
					currentSwingHigh = num5;
					for (int k = 0; k <= Strength; k++)
					{
						SwingHighPlot[k] = currentSwingHigh;
					}
				}
				else if (num > currentSwingHigh || MathExtentions.ApproxCompare(currentSwingHigh, 0.0) == 0)
				{
					currentSwingHigh = 0.0;
					SwingHighPlot[0] = num3;
					SwingHighPlot.Reset();
				}
				else
				{
					SwingHighPlot[0] = currentSwingHigh;
				}
				if (flag2)
				{
					for (int l = 0; l <= Strength; l++)
					{
						swingHighSeries[l] = lastSwingHighValue;
					}
				}
				else
				{
					swingHighSeries[0] = lastSwingHighValue;
				}
			}
			if (lastLowCache.Count == constant)
			{
				bool flag3 = true;
				double num6 = (double)lastLowCache[Strength];
				for (int m = 0; m < Strength; m++)
				{
					if (MathExtentions.ApproxCompare((double)lastLowCache[m], num6) <= 0)
					{
						flag3 = false;
					}
				}
				for (int n = Strength + 1; n < lastLowCache.Count; n++)
				{
					if (MathExtentions.ApproxCompare((double)lastLowCache[n], num6) < 0)
					{
						flag3 = false;
					}
				}
				swingLowSwings[Strength] = (flag3 ? num6 : 0.0);
				if (flag3)
				{
					lastSwingLowValue = num6;
				}
				if (flag3)
				{
					currentSwingLow = num6;
					for (int num7 = 0; num7 <= Strength; num7++)
					{
						SwingLowPlot[num7] = currentSwingLow;
					}
				}
				else if (num2 < currentSwingLow || MathExtentions.ApproxCompare(currentSwingLow, 0.0) == 0)
				{
					currentSwingLow = double.MaxValue;
					SwingLowPlot[0] = num3;
					SwingLowPlot.Reset();
				}
				else
				{
					SwingLowPlot[0] = currentSwingLow;
				}
				if (flag3)
				{
					for (int num8 = 0; num8 <= Strength; num8++)
					{
						swingLowSeries[num8] = lastSwingLowValue;
					}
				}
				else
				{
					swingLowSeries[0] = lastSwingLowValue;
				}
			}
			saveCurrentBar = ((NinjaScriptBase)this).CurrentBar;
		}
		else
		{
			if (((NinjaScriptBase)this).CurrentBar < constant - 1)
			{
				return;
			}
			if (lastHighCache.Count == constant && MathExtentions.ApproxCompare(num, (double)lastHighCache[lastHighCache.Count - 1]) > 0)
			{
				lastHighCache[lastHighCache.Count - 1] = num;
			}
			if (lastLowCache.Count == constant && MathExtentions.ApproxCompare(num2, (double)lastLowCache[lastLowCache.Count - 1]) < 0)
			{
				lastLowCache[lastLowCache.Count - 1] = num2;
			}
			if (num > currentSwingHigh && swingHighSwings[Strength] > 0.0)
			{
				swingHighSwings[Strength] = 0.0;
				for (int num9 = 0; num9 <= Strength; num9++)
				{
					SwingHighPlot[num9] = num3;
					SwingHighPlot.Reset(num9);
					currentSwingHigh = 0.0;
				}
			}
			else if (num > currentSwingHigh && MathExtentions.ApproxCompare(currentSwingHigh, 0.0) != 0)
			{
				SwingHighPlot[0] = num3;
				SwingHighPlot.Reset();
				currentSwingHigh = 0.0;
			}
			else if (num <= currentSwingHigh)
			{
				SwingHighPlot[0] = currentSwingHigh;
			}
			if (num2 < currentSwingLow && swingLowSwings[Strength] > 0.0)
			{
				swingLowSwings[Strength] = 0.0;
				for (int num10 = 0; num10 <= Strength; num10++)
				{
					SwingLowPlot[num10] = num3;
					SwingLowPlot.Reset(num10);
					currentSwingLow = double.MaxValue;
				}
			}
			else if (num2 < currentSwingLow && MathExtentions.ApproxCompare(currentSwingLow, double.MaxValue) != 0)
			{
				SwingLowPlot.Reset();
				currentSwingLow = double.MaxValue;
			}
			else if (num2 >= currentSwingLow)
			{
				SwingLowPlot[0] = currentSwingLow;
			}
		}
	}

	/// <summary>
	/// Returns the number of bars ago a swing low occurred. Returns a value of -1 if a swing low is not found within the look back period.
	/// </summary>
	/// <param name="barsAgo"></param>
	/// <param name="instance"></param>
	/// <param name="lookBackPeriod"></param>
	/// <returns></returns>
	public int SwingLowBar(int barsAgo, int instance, int lookBackPeriod)
	{
		if (instance < 1)
		{
			throw new Exception(string.Format(Resource.SwingSwingLowBarInstanceGreaterEqual, ((object)this).GetType().Name, instance));
		}
		if (barsAgo < 0)
		{
			throw new Exception(string.Format(Resource.SwingSwingLowBarBarsAgoGreaterEqual, ((object)this).GetType().Name, barsAgo));
		}
		if (barsAgo >= ((NinjaScriptBase)this).Count)
		{
			throw new Exception(string.Format(Resource.SwingSwingLowBarBarsAgoOutOfRange, ((object)this).GetType().Name, ((NinjaScriptBase)this).Count - 1, barsAgo));
		}
		((NinjaScriptBase)this).Update();
		for (int num = ((NinjaScriptBase)this).CurrentBar - barsAgo - Strength; num >= ((NinjaScriptBase)this).CurrentBar - barsAgo - Strength - lookBackPeriod; num--)
		{
			if (num < 0)
			{
				return -1;
			}
			if (num < swingLowSwings.Count && !swingLowSwings.GetValueAt(num).Equals(0.0))
			{
				if (instance == 1)
				{
					return ((NinjaScriptBase)this).CurrentBar - num;
				}
				instance--;
			}
		}
		return -1;
	}

	/// <summary>
	/// Returns the number of bars ago a swing high occurred. Returns a value of -1 if a swing high is not found within the look back period.
	/// </summary>
	/// <param name="barsAgo"></param>
	/// <param name="instance"></param>
	/// <param name="lookBackPeriod"></param>
	/// <returns></returns>
	public int SwingHighBar(int barsAgo, int instance, int lookBackPeriod)
	{
		if (instance < 1)
		{
			throw new Exception(string.Format(Resource.SwingSwingHighBarInstanceGreaterEqual, ((object)this).GetType().Name, instance));
		}
		if (barsAgo < 0)
		{
			throw new Exception(string.Format(Resource.SwingSwingHighBarBarsAgoGreaterEqual, ((object)this).GetType().Name, barsAgo));
		}
		if (barsAgo >= ((NinjaScriptBase)this).Count)
		{
			throw new Exception(string.Format(Resource.SwingSwingHighBarBarsAgoOutOfRange, ((object)this).GetType().Name, ((NinjaScriptBase)this).Count - 1, barsAgo));
		}
		((NinjaScriptBase)this).Update();
		for (int num = ((NinjaScriptBase)this).CurrentBar - barsAgo - Strength; num >= ((NinjaScriptBase)this).CurrentBar - barsAgo - Strength - lookBackPeriod; num--)
		{
			if (num < 0)
			{
				return -1;
			}
			if (num < swingHighSwings.Count && !swingHighSwings.GetValueAt(num).Equals(0.0))
			{
				if (instance <= 1)
				{
					return ((NinjaScriptBase)this).CurrentBar - num;
				}
				instance--;
			}
		}
		return -1;
	}
}

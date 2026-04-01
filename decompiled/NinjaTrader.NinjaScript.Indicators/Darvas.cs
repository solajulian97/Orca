using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

public class Darvas : Indicator
{
	private double boxBottom = double.MaxValue;

	private double boxTop = double.MinValue;

	private bool buySignal;

	private double currentBarHigh = double.MinValue;

	private double currentBarLow = double.MaxValue;

	private bool isRealtime;

	private int savedCurrentBar = -1;

	private bool sellSignal;

	private int startBarActBox;

	private int state;

	private int prevCurrentBar = -1;

	private Series<double> boxBottomSeries;

	private Series<double> boxTopSeries;

	private Series<double> currentBarHighSeries;

	private Series<double> currentBarLowSeries;

	private Series<int> startBarActBoxSeries;

	private Series<int> stateSeries;

	[Browsable(false)]
	[XmlIgnore]
	public bool BuySignal
	{
		get
		{
			((NinjaScriptBase)this).Update();
			return buySignal;
		}
		set
		{
			buySignal = value;
		}
	}

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Lower => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public bool SellSignal
	{
		get
		{
			((NinjaScriptBase)this).Update();
			return sellSignal;
		}
		set
		{
			sellSignal = value;
		}
	}

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Upper => ((NinjaScriptBase)this).Values[1];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionDarvas;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameDarvas;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Crimson, 2f), (PlotStyle)7, Resource.NinjaScriptIndicatorLower);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DarkCyan, 2f), (PlotStyle)7, Resource.NinjaScriptIndicatorUpper);
		}
		else if ((int)((NinjaScript)this).State == 4 && ((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported)
		{
			boxBottomSeries = new Series<double>((NinjaScriptBase)(object)this);
			boxTopSeries = new Series<double>((NinjaScriptBase)(object)this);
			currentBarHighSeries = new Series<double>((NinjaScriptBase)(object)this);
			currentBarLowSeries = new Series<double>((NinjaScriptBase)(object)this);
			startBarActBoxSeries = new Series<int>((NinjaScriptBase)(object)this);
			stateSeries = new Series<int>((NinjaScriptBase)(object)this);
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Invalid comparison between Unknown and I4
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Invalid comparison between Unknown and I4
		BuySignal = false;
		SellSignal = false;
		if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported && ((NinjaScriptBase)this).CurrentBar < prevCurrentBar)
		{
			boxBottom = boxBottomSeries[0];
			boxTop = boxTopSeries[0];
			currentBarHigh = currentBarHighSeries[0];
			currentBarLow = currentBarLowSeries[0];
			startBarActBox = startBarActBoxSeries[0];
			state = stateSeries[0];
		}
		if (savedCurrentBar == -1)
		{
			currentBarHigh = ((NinjaScriptBase)this).High[0];
			currentBarLow = ((NinjaScriptBase)this).Low[0];
			state = GetNextState();
			savedCurrentBar = ((NinjaScriptBase)this).CurrentBar;
		}
		else if (savedCurrentBar != ((NinjaScriptBase)this).CurrentBar)
		{
			currentBarHigh = ((isRealtime && (int)((NinjaScriptBase)this).Calculate == 1) ? ((NinjaScriptBase)this).High[1] : ((NinjaScriptBase)this).High[0]);
			currentBarLow = ((isRealtime && (int)((NinjaScriptBase)this).Calculate == 1) ? ((NinjaScriptBase)this).Low[1] : ((NinjaScriptBase)this).Low[0]);
			if ((state == 5 && currentBarHigh > boxTop) || (state == 5 && currentBarLow < boxBottom))
			{
				if (state == 5 && currentBarHigh > boxTop)
				{
					BuySignal = true;
				}
				else
				{
					SellSignal = true;
				}
				state = 0;
				startBarActBox = ((NinjaScriptBase)this).CurrentBar;
			}
			state = GetNextState();
			if (boxBottom >= double.MaxValue)
			{
				for (int num = ((NinjaScriptBase)this).CurrentBar - startBarActBox; num >= 0; num--)
				{
					Upper[num] = boxTop;
				}
			}
			else
			{
				for (int num2 = ((NinjaScriptBase)this).CurrentBar - startBarActBox; num2 >= 0; num2--)
				{
					Upper[num2] = boxTop;
					Lower[num2] = boxBottom;
				}
			}
		}
		else
		{
			isRealtime = true;
			if ((state == 5 && currentBarHigh > boxTop) || (state == 5 && currentBarLow < boxBottom))
			{
				if (state == 5 && currentBarHigh > boxTop)
				{
					BuySignal = true;
				}
				else
				{
					SellSignal = true;
				}
				startBarActBox = ((NinjaScriptBase)this).CurrentBar + 1;
				state = 0;
			}
			if (boxBottom >= double.MaxValue)
			{
				Upper[0] = boxTop;
			}
			else
			{
				Upper[0] = boxTop;
				Lower[0] = boxBottom;
			}
		}
		if (((NinjaScriptBase)this).BarsArray[0].BarsType.IsRemoveLastBarSupported)
		{
			boxBottomSeries[0] = boxBottom;
			boxTopSeries[0] = boxTop;
			currentBarHighSeries[0] = currentBarHigh;
			currentBarLowSeries[0] = currentBarLow;
			startBarActBoxSeries[0] = startBarActBox;
			stateSeries[0] = state;
			prevCurrentBar = ((NinjaScriptBase)this).CurrentBar;
		}
	}

	private int GetNextState()
	{
		switch (state)
		{
		case 0:
			boxTop = currentBarHigh;
			boxBottom = double.MaxValue;
			return 1;
		case 1:
			if (boxTop > currentBarHigh)
			{
				return 2;
			}
			boxTop = currentBarHigh;
			return 1;
		case 2:
			if (boxTop > currentBarHigh)
			{
				boxBottom = currentBarLow;
				return 3;
			}
			boxTop = currentBarHigh;
			return 1;
		case 3:
			if (boxTop > currentBarHigh)
			{
				if (boxBottom < currentBarLow)
				{
					return 4;
				}
				boxBottom = currentBarLow;
				return 3;
			}
			boxTop = currentBarHigh;
			boxBottom = double.MaxValue;
			return 1;
		case 4:
			if (boxTop > currentBarHigh)
			{
				if (boxBottom < currentBarLow)
				{
					return 5;
				}
				boxBottom = currentBarLow;
				return 3;
			}
			boxTop = currentBarHigh;
			boxBottom = double.MaxValue;
			return 1;
		case 5:
			return 5;
		default:
			return state;
		}
	}
}

using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

public class BlockVolume : Indicator
{
	private double blockValue;

	private int lastCurrentBar;

	private bool hasCarriedOverTransitionTick;

	[Range(1E-08, double.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "BlockTradeSize", GroupName = "NinjaScriptParameters", Order = 0)]
	public double BlockSize { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptIndicatorCount", GroupName = "NinjaScriptParameters", Order = 0)]
	public CountType CountType { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Invalid comparison between Unknown and I4
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionBlockVolume;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameBlockVolume;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = false;
			CountType = CountType.Volume;
			BlockSize = 80.0;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DarkRed, 2f), (PlotStyle)0, Resource.NinjaScriptIndicatorNameBlockVolume);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddDataSeries((BarsPeriodType)0, 1);
		}
	}

	private void CalculateBlockVolume(bool forceCurrentBar)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Invalid comparison between Unknown and I4
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Invalid comparison between Unknown and I4
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Invalid comparison between Unknown and I4
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		bool flag = (int)((NinjaScript)this).State == 7 && ((NinjaScriptBase)this).BarsArray[1].Count - 1 - ((NinjaScriptBase)this).CurrentBars[1] > 1;
		int num = (((int)((NinjaScript)this).State == 5 || flag || (int)((NinjaScriptBase)this).Calculate > 0 || forceCurrentBar) ? ((NinjaScriptBase)this).CurrentBars[1] : Math.Min(((NinjaScriptBase)this).CurrentBars[1] + 1, ((NinjaScriptBase)this).BarsArray[1].Count - 1));
		if ((((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume(((NinjaScriptBase)this).BarsArray[1].GetVolume(num)) : ((double)((NinjaScriptBase)this).BarsArray[1].GetVolume(num))) >= BlockSize)
		{
			if (!flag && hasCarriedOverTransitionTick && !forceCurrentBar && (int)((NinjaScriptBase)this).Calculate == 0)
			{
				CalculateBlockVolume(forceCurrentBar: true);
			}
			hasCarriedOverTransitionTick = flag;
			blockValue += ((CountType != CountType.Volume) ? 1.0 : (((int)((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType == 7) ? Globals.ToCryptocurrencyVolume(((NinjaScriptBase)this).BarsArray[1].GetVolume(num)) : ((double)((NinjaScriptBase)this).BarsArray[1].GetVolume(num))));
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Invalid comparison between Unknown and I4
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Invalid comparison between Unknown and I4
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).BarsInProgress == 0)
		{
			if (lastCurrentBar <= ((NinjaScriptBase)this).CurrentBars[0])
			{
				int num = ((NinjaScriptBase)this).BarsArray[1].Count - 1 - ((NinjaScriptBase)this).CurrentBars[1];
				if (lastCurrentBar < ((NinjaScriptBase)this).CurrentBars[0] && (int)((NinjaScriptBase)this).Calculate != 0 && ((int)((NinjaScript)this).State == 7 || ((NinjaScriptBase)this).BarsArray[0].IsTickReplay))
				{
					if (((NinjaScriptBase)this).CurrentBars[0] > 0)
					{
						((NinjaScriptBase)this).Value[1] = blockValue;
					}
					if (((NinjaScriptBase)this).BarsArray[0].IsTickReplay || ((int)((NinjaScript)this).State == 7 && num == 0))
					{
						blockValue = 0.0;
					}
				}
				((NinjaScriptBase)this).Value[0] = blockValue;
				if ((int)((NinjaScriptBase)this).Calculate == 0 || (lastCurrentBar < ((NinjaScriptBase)this).CurrentBars[0] && ((NinjaScriptBase)this).BarsArray[0].BarsType.IsIntraday && (((int)((NinjaScript)this).State == 5 && ((NinjaScriptBase)this).BarsArray[0].Count - 1 - ((NinjaScriptBase)this).CurrentBars[0] > 0) || ((int)((NinjaScript)this).State == 7 && num > 0))))
				{
					blockValue = 0.0;
				}
			}
			lastCurrentBar = ((lastCurrentBar < ((NinjaScriptBase)this).CurrentBars[0]) ? ((NinjaScriptBase)this).CurrentBars[0] : lastCurrentBar);
		}
		else
		{
			if (((NinjaScriptBase)this).BarsArray[1].IsFirstBarOfSession && ((int)((NinjaScriptBase)this).Calculate != 0 || ((NinjaScriptBase)this).BarsArray[0].BarsType.IsIntraday))
			{
				blockValue = 0.0;
			}
			CalculateBlockVolume(forceCurrentBar: false);
		}
	}
}

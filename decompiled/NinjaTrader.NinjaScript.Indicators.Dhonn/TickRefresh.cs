using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Threading;
using NinjaTrader.Gui.NinjaScript;

namespace NinjaTrader.NinjaScript.Indicators.Dhonn;

public class TickRefresh : Indicator
{
	private long oldTimeFrame;

	private bool isRefreshing;

	[NinjaScriptProperty]
	[Range(0, int.MaxValue)]
	[Display(Name = "Refresh Time Interval in Milliseconds", Description = "Number of milliseconds between chart refreshes.\n\n0 will refresh the chart on every tick or price change.\nUnder \"Set up - Calculate\" choose between updates on each tick or price change.\n\nExamples:\n0 = no delay, update immediately on every tick or price change.\n1 = maximum of 1000 updates per second.\n10 = maximum of 100 updates per second.\n20 = maximum of 50 updates per second.\n100 = maximum of 10 updates per second.", Order = 1, GroupName = "Parameters")]
	public int RefreshTimeInterval { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Invalid comparison between Unknown and I4
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Refresh the chart on every tick or specified time interval.";
			((NinjaScriptBase)this).Name = "TickRefresh";
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).IsChartOnly = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			RefreshTimeInterval = 10;
		}
		else if ((int)((NinjaScript)this).State != 2)
		{
			_ = ((NinjaScript)this).State;
			_ = 8;
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		if (isRefreshing || ((IndicatorRenderBase)this).ChartControl == null || (int)((NinjaScript)this).State == 5)
		{
			return;
		}
		if (RefreshTimeInterval > 0)
		{
			long num = DateTime.Now.Ticks / (10000 * RefreshTimeInterval);
			if (num == oldTimeFrame)
			{
				return;
			}
			oldTimeFrame = num;
		}
		isRefreshing = true;
		((DispatcherObject)(object)((IndicatorRenderBase)this).ChartControl).Dispatcher.InvokeAsync(delegate
		{
			((IndicatorRenderBase)this).ChartControl.InvalidateVisual();
			isRefreshing = false;
		});
	}
}

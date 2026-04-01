using System;
using System.Windows;
using NinjaTrader.Custom;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Text Fixed IDrawingTool.
/// </summary>
public class TextFixedBase : Text
{
	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
	}

	protected int PaddingMultiplier(ChartControl chartControl, ChartPanel panel, bool top)
	{
		if (top)
		{
			if (chartControl.ChartPanels.IndexOf(panel) != 0 || !chartControl.IsScrollArrowVisible)
			{
				return 1;
			}
			return 4;
		}
		if (chartControl.ChartPanels.IndexOf(panel) != chartControl.ChartPanels.Count - 1)
		{
			return 1;
		}
		return 2;
	}

	protected override Rect GetCurrentRect(Rect layoutRect, double outlinePadding)
	{
		return new Rect(layoutRect.X - outlinePadding, layoutRect.Y - outlinePadding, layoutRect.Width + outlinePadding * 2.0, layoutRect.Height + outlinePadding * 2.0);
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		return true;
	}

	protected override void OnStateChange()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		base.OnStateChange();
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolTextFixed;
			base.Anchor.IsBrowsable = false;
			((DrawingTool)this).ZOrderType = (DrawingToolZOrder)2;
			((DrawingTool)this).IgnoresUserInput = true;
			((DrawingTool)this).DisplayOnChartsMenus = false;
		}
	}
}

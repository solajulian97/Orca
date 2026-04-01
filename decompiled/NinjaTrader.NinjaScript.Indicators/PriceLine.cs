using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Displays ask, bid, and/or last lines on the chart.
/// </summary>
public class PriceLine : Indicator
{
	[XmlIgnore]
	[Browsable(false)]
	public double AskLine => ((NinjaScriptBase)this).Values[0].GetValueAt(((NinjaScriptBase)this).Values[0].Count - 1);

	[XmlIgnore]
	[Browsable(false)]
	public double BidLine => ((NinjaScriptBase)this).Values[1].GetValueAt(((NinjaScriptBase)this).Values[1].Count - 1);

	[XmlIgnore]
	[Browsable(false)]
	public double LastLine => ((NinjaScriptBase)this).Values[2].GetValueAt(((NinjaScriptBase)this).Values[2].Count - 1);

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "ShowAskLine", GroupName = "NinjaScriptParameters", Order = 0)]
	public bool ShowAskLine { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "ShowBidLine", GroupName = "NinjaScriptParameters", Order = 1)]
	public bool ShowBidLine { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "ShowLastLine", GroupName = "NinjaScriptParameters", Order = 2)]
	public bool ShowLastLine { get; set; }

	[Range(1, 100)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "AskLineLength", GroupName = "NinjaScriptParameters", Order = 3)]
	public int AskLineLength { get; set; }

	[Range(1, 100)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "BidLineLength", GroupName = "NinjaScriptParameters", Order = 4)]
	public int BidLineLength { get; set; }

	[Range(1, 100)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "LastLineLength", GroupName = "NinjaScriptParameters", Order = 5)]
	public int LastLineLength { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "AskLineStroke", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1800)]
	public Stroke AskStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "BidLineStroke", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1810)]
	public Stroke BidStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "LastLineStroke", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1820)]
	public Stroke LastStroke { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Invalid comparison between Unknown and I4
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionPriceLine;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNamePriceLine;
			((NinjaScriptBase)this).Calculate = (Calculate)2;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).ShowTransparentPlotsInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			ShowAskLine = false;
			ShowBidLine = false;
			ShowLastLine = true;
			AskLineLength = 100;
			BidLineLength = 100;
			LastLineLength = 100;
			AskStroke = new Stroke((Brush)Brushes.DarkGreen, (DashStyleHelper)1, 1f);
			BidStroke = new Stroke((Brush)Brushes.Blue, (DashStyleHelper)1, 1f);
			LastStroke = new Stroke((Brush)Brushes.Yellow, (DashStyleHelper)1, 1f);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).AddPlot(ShowAskLine ? AskStroke.Brush : Brushes.Transparent, Resource.PriceLinePlotAsk);
			((NinjaScriptBase)this).AddPlot(ShowBidLine ? BidStroke.Brush : Brushes.Transparent, Resource.PriceLinePlotBid);
			((NinjaScriptBase)this).AddPlot(ShowLastLine ? LastStroke.Brush : Brushes.Transparent, Resource.PriceLinePlotLast);
		}
	}

	protected override void OnBarUpdate()
	{
	}

	public override void OnCalculateMinMax()
	{
		double num = double.MaxValue;
		double num2 = double.MinValue;
		if (((NinjaScriptBase)this).Values[0].Count > 0 && ShowAskLine && ((NinjaScriptBase)this).Values[0].IsValidDataPointAt(((NinjaScriptBase)this).Values[0].Count - 1))
		{
			double valueAt = ((NinjaScriptBase)this).Values[0].GetValueAt(((NinjaScriptBase)this).Values[0].Count - 1);
			num = Math.Min(num, valueAt);
			num2 = Math.Max(num2, valueAt);
		}
		if (((NinjaScriptBase)this).Values[1].Count > 0 && ShowBidLine && ((NinjaScriptBase)this).Values[1].IsValidDataPointAt(((NinjaScriptBase)this).Values[1].Count - 1))
		{
			double valueAt2 = ((NinjaScriptBase)this).Values[1].GetValueAt(((NinjaScriptBase)this).Values[1].Count - 1);
			num = Math.Min(num, valueAt2);
			num2 = Math.Max(num2, valueAt2);
		}
		if (((NinjaScriptBase)this).Values[2].Count > 0 && ShowLastLine && ((NinjaScriptBase)this).Values[2].IsValidDataPointAt(((NinjaScriptBase)this).Values[2].Count - 1))
		{
			double valueAt3 = ((NinjaScriptBase)this).Values[2].GetValueAt(((NinjaScriptBase)this).Values[2].Count - 1);
			num = Math.Min(num, valueAt3);
			num2 = Math.Max(num2, valueAt3);
		}
		((IndicatorRenderBase)this).MinValue = num;
		((IndicatorRenderBase)this).MaxValue = num2;
	}

	protected override void OnMarketData(MarketDataEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)e.MarketDataType == 2 && ((NinjaScriptBase)this).CurrentBar >= 0)
		{
			((NinjaScriptBase)this).Values[0][0] = e.Ask;
			((NinjaScriptBase)this).Values[1][0] = e.Bid;
			((NinjaScriptBase)this).Values[2][0] = e.Price;
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		if (((NinjaScriptBase)this).BarsArray[0] != null && ((IndicatorRenderBase)this).ChartBars != null)
		{
			ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
			float num = val.X + val.W;
			if (((NinjaScriptBase)this).Values[0].Count > 0 && ShowAskLine && ((NinjaScriptBase)this).Values[0].IsValidDataPointAt(((NinjaScriptBase)this).Values[0].Count - 1))
			{
				float num2 = Convert.ToSingle((double)val.X + (double)val.W * (1.0 - (double)AskLineLength / 100.0));
				float num3 = chartScale.GetYByValue(((NinjaScriptBase)this).Values[0].GetValueAt(((NinjaScriptBase)this).Values[0].Count - 1));
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num2, num3), new Vector2(num, num3), AskStroke.BrushDX, AskStroke.Width, AskStroke.StrokeStyle);
			}
			if (((NinjaScriptBase)this).Values[1].Count > 0 && ShowBidLine && ((NinjaScriptBase)this).Values[1].IsValidDataPointAt(((NinjaScriptBase)this).Values[1].Count - 1))
			{
				float num4 = Convert.ToSingle((double)val.X + (double)val.W * (1.0 - (double)BidLineLength / 100.0));
				float num5 = chartScale.GetYByValue(((NinjaScriptBase)this).Values[1].GetValueAt(((NinjaScriptBase)this).Values[1].Count - 1));
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num4, num5), new Vector2(num, num5), BidStroke.BrushDX, BidStroke.Width, BidStroke.StrokeStyle);
			}
			if (((NinjaScriptBase)this).Values[2].Count > 0 && ShowLastLine && ((NinjaScriptBase)this).Values[2].IsValidDataPointAt(((NinjaScriptBase)this).Values[2].Count - 1))
			{
				float num6 = Convert.ToSingle((double)val.X + (double)val.W * (1.0 - (double)LastLineLength / 100.0));
				float num7 = chartScale.GetYByValue(((NinjaScriptBase)this).Values[2].GetValueAt(((NinjaScriptBase)this).Values[2].Count - 1));
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num6, num7), new Vector2(num, num7), LastStroke.BrushDX, LastStroke.Width, LastStroke.StrokeStyle);
			}
		}
	}

	public override void OnRenderTargetChanged()
	{
		AskStroke.RenderTarget = ((IndicatorRenderBase)this).RenderTarget;
		BidStroke.RenderTarget = ((IndicatorRenderBase)this).RenderTarget;
		LastStroke.RenderTarget = ((IndicatorRenderBase)this).RenderTarget;
	}
}

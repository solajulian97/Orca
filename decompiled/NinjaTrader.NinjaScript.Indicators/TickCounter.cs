using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

public class TickCounter : Indicator
{
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "CountDown", Order = 1, GroupName = "NinjaScriptParameters")]
	public bool CountDown { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "ShowPercent", Order = 2, GroupName = "NinjaScriptParameters")]
	public bool ShowPercent { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "GuiPropertyNameTextPosition", GroupName = "PropertyCategoryVisual", Order = 70)]
	public TextPositionFine TextPositionFine { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionTickCounter;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameTickCounter;
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			CountDown = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((IndicatorBase)this).IsChartOnly = true;
			((NinjaScriptBase)this).IsOverlay = true;
			ShowPercent = false;
			TextPositionFine = TextPositionFine.BottomRight;
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Invalid comparison between Unknown and I4
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Invalid comparison between Unknown and I4
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Invalid comparison between Unknown and I4
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Invalid comparison between Unknown and I4
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Invalid comparison between Unknown and I4
		double num = (((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 0) ? ((NinjaScriptBase)this).BarsPeriod.Value : ((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodValue);
		double num2 = ((!ShowPercent) ? (CountDown ? (num - (double)((NinjaScriptBase)this).Bars.TickCount) : ((double)((NinjaScriptBase)this).Bars.TickCount)) : (CountDown ? (1.0 - ((NinjaScriptBase)this).Bars.PercentComplete) : ((NinjaScriptBase)this).Bars.PercentComplete));
		string text = (ShowPercent ? num2.ToString("P0") : num2.ToString(CultureInfo.InvariantCulture));
		bool flag = (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 0;
		if (!flag)
		{
			BarsPeriodType barsPeriodType = ((NinjaScriptBase)this).BarsPeriod.BarsPeriodType;
			bool flag2 = (((int)barsPeriodType == 9 || (int)barsPeriodType == 14 || (int)barsPeriodType == 16) ? true : false);
			flag = flag2 && (int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType == 0;
		}
		string text2 = ((!flag) ? Resource.TickCounterBarError : (CountDown ? (Resource.TickCounterTicksRemaining + text) : (Resource.TickCounterTickCount + text)));
		Draw.TextFixedFine((NinjaScriptBase)(object)this, "NinjaScriptInfo", text2, TextPositionFine, ((IndicatorRenderBase)this).ChartControl.Properties.ChartText, ((IndicatorRenderBase)this).ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
	}
}

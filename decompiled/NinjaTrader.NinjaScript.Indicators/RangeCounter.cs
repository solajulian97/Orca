using System;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Custom;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

public class RangeCounter : Indicator
{
	private bool isAdvancedType;

	private string rangeString;

	private bool supportsRange;

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "CountDown", Order = 1, GroupName = "NinjaScriptParameters")]
	public bool CountDown { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "GuiPropertyNameTextPosition", GroupName = "PropertyCategoryVisual", Order = 70)]
	public TextPositionFine TextPositionFine { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Invalid comparison between Unknown and I4
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Invalid comparison between Unknown and I4
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Invalid comparison between Unknown and I4
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Invalid comparison between Unknown and I4
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Invalid comparison between Unknown and I4
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Invalid comparison between Unknown and I4
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionRangeCounter;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameRangeCounter;
			((NinjaScriptBase)this).Calculate = (Calculate)2;
			CountDown = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsChartOnly = true;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			TextPositionFine = TextPositionFine.BottomRight;
		}
		else if ((int)((NinjaScript)this).State == 5)
		{
			isAdvancedType = (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 9 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 16 || (int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 14;
			bool flag = ((object)((NinjaScriptBase)this).BarsPeriod).ToString().IndexOf("Range", StringComparison.Ordinal) >= 0 || ((object)((NinjaScriptBase)this).BarsPeriod).ToString().IndexOf(Resource.BarsPeriodTypeNameRange, StringComparison.Ordinal) >= 0;
			if ((int)((NinjaScriptBase)this).BarsPeriod.BarsPeriodType == 2 || ((int)((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodType == 2 && isAdvancedType) || ((int)((NinjaScriptBase)this).BarsArray[0].BarsType.BuiltFrom == 0 && flag))
			{
				supportsRange = true;
			}
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Invalid comparison between Unknown and I4
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).BarsArray != null && ((NinjaScriptBase)this).BarsArray.Length != 0)
		{
			if (supportsRange)
			{
				double valueAt = ((NinjaScriptBase)this).High.GetValueAt(((NinjaScriptBase)this).Bars.Count - 1 - (((int)((NinjaScriptBase)this).Calculate == 0) ? 1 : 0));
				double valueAt2 = ((NinjaScriptBase)this).Low.GetValueAt(((NinjaScriptBase)this).Bars.Count - 1 - (((int)((NinjaScriptBase)this).Calculate == 0) ? 1 : 0));
				double valueAt3 = ((NinjaScriptBase)this).Close.GetValueAt(((NinjaScriptBase)this).Bars.Count - 1 - (((int)((NinjaScriptBase)this).Calculate == 0) ? 1 : 0));
				int num = (int)Math.Round(Math.Max(valueAt3 - valueAt2, valueAt - valueAt3) / ((NinjaScriptBase)this).Bars.Instrument.MasterInstrument.TickSize);
				double num2 = (CountDown ? ((isAdvancedType ? ((NinjaScriptBase)this).BarsPeriod.BaseBarsPeriodValue : ((NinjaScriptBase)this).BarsPeriod.Value) - num) : num);
				rangeString = (CountDown ? string.Format(Resource.RangeCounterRemaing, num2) : string.Format(Resource.RangerCounterCount, num2));
			}
			else
			{
				rangeString = Resource.RangeCounterBarError;
			}
			Draw.TextFixedFine((NinjaScriptBase)(object)this, "NinjaScriptInfo", rangeString, TextPositionFine);
		}
	}
}

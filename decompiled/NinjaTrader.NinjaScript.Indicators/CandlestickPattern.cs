using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

public class CandlestickPattern : Indicator
{
	private Brush downBrush = Brushes.DimGray;

	private CandleStickPatternLogic logic;

	private int numPatternsFound;

	private readonly TextPosition textBoxPosition = TextPosition.BottomRight;

	private Brush textBrush = Brushes.DimGray;

	private Brush upBrush = Brushes.DimGray;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> PatternFound => ((NinjaScriptBase)this).Values[0];

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "SelectPattern", Description = "SelectPatternDescription", GroupName = "NinjaScriptGeneral", Order = 1)]
	public ChartPattern Pattern { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "SendAlerts", Description = "SendAlertsDescription", GroupName = "NinjaScriptGeneral", Order = 2)]
	public bool ShowAlerts { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "ShowPatternCount", Description = "ShowPatternCountDescription", GroupName = "NinjaScriptGeneral", Order = 3)]
	public bool ShowPatternCount { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "TextFont", Description = "TextFontDescription", GroupName = "NinjaScriptGeneral", Order = 4)]
	public SimpleFont TextFont { get; set; }

	[NinjaScriptProperty]
	[Range(0, int.MaxValue)]
	[Display(ResourceType = typeof(Resource), Name = "TrendStrength", Description = "TrendStrengthDescription", GroupName = "NinjaScriptGeneral", Order = 5)]
	public int TrendStrength { get; set; }

	private void DrawText(string text, int barsAgo, double price, int yOffset)
	{
		string tag = text + ((NinjaScriptBase)this).CurrentBar;
		int num = ++numPatternsFound;
		Draw.Text((NinjaScriptBase)(object)this, tag, isAutoScale: false, text + " # " + num, barsAgo, price, yOffset, textBrush, TextFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Invalid comparison between Unknown and I4
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Invalid comparison between Unknown and I4
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionCandlestickPattern;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameCandlestickPattern;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((NinjaScriptBase)this).IsAutoScale = false;
			((IndicatorBase)this).PaintPriceMarkers = false;
			Pattern = ChartPattern.MorningStar;
			ShowAlerts = true;
			ShowPatternCount = true;
			TrendStrength = 4;
			TextFont = new SimpleFont
			{
				Size = 14.0
			};
			downBrush = Brushes.DimGray;
			upBrush = Brushes.DimGray;
			textBrush = Brushes.DimGray;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Transparent, Resource.CandlestickPatternFound);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((NinjaScriptBase)this).Calculate = (Calculate)0;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			logic = new CandleStickPatternLogic((NinjaScriptBase)(object)this, TrendStrength);
		}
		else if ((int)((NinjaScript)this).State == 5)
		{
			if (((IndicatorRenderBase)this).ChartControl != null)
			{
				downBrush = ((IndicatorRenderBase)this).ChartControl.Properties.AxisPen.Brush;
				textBrush = ((IndicatorRenderBase)this).ChartControl.Properties.ChartText;
			}
			if (downBrush == upBrush)
			{
				upBrush = Brushes.Transparent;
			}
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0ce1: Unknown result type (might be due to invalid IL or missing references)
		PatternFound[0] = (logic.Evaluate(Pattern) ? 1 : 0);
		if (Math.Abs(PatternFound[0] - 1.0) < 1E-13)
		{
			bool flag = false;
			string text = string.Empty;
			switch (Pattern)
			{
			case ChartPattern.BearishBeltHold:
				text = "Bearish Belt Hold";
				flag = true;
				break;
			case ChartPattern.BearishEngulfing:
				text = "Bearish Engulfing";
				flag = true;
				break;
			case ChartPattern.BearishHarami:
				text = "Bearish Harami";
				flag = true;
				break;
			case ChartPattern.BearishHaramiCross:
				text = "Bearish Harami Cross";
				flag = true;
				break;
			case ChartPattern.BullishBeltHold:
				text = "Bullish Belt Hold";
				break;
			case ChartPattern.BullishEngulfing:
				text = "Bullish Engulfing";
				break;
			case ChartPattern.BullishHarami:
				text = "Bullish Harami";
				break;
			case ChartPattern.BullishHaramiCross:
				text = "Bullish Harami Cross";
				break;
			}
			if (!string.IsNullOrEmpty(text))
			{
				((NinjaScriptBase)this).BarBrushes[1] = (flag ? upBrush : downBrush);
				((NinjaScriptBase)this).BarBrushes[0] = (flag ? downBrush : upBrush);
				((NinjaScriptBase)this).CandleOutlineBrushes[1] = ((Pattern == ChartPattern.BearishBeltHold) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[1]);
				((NinjaScriptBase)this).CandleOutlineBrushes[0] = ((!flag) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[0]);
				DrawText(text, 0, flag ? Math.Max(((NinjaScriptBase)this).High[0], ((NinjaScriptBase)this).High[1]) : Math.Min(((NinjaScriptBase)this).Low[0], ((NinjaScriptBase)this).Low[1]), flag ? 40 : 10);
			}
			switch (Pattern)
			{
			case ChartPattern.DarkCloudCover:
				((NinjaScriptBase)this).BarBrushes[1] = upBrush;
				((NinjaScriptBase)this).BarBrushes[0] = downBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[1] = downBrush;
				DrawText("Dark Cloud Cover", 1, Math.Max(((NinjaScriptBase)this).High[0], ((NinjaScriptBase)this).High[1]), 50);
				break;
			case ChartPattern.Doji:
			{
				((NinjaScriptBase)this).BarBrushes[0] = upBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[0] = downBrush;
				int num = ((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Close[Math.Min(1, ((NinjaScriptBase)this).CurrentBar)]) ? 20 : (-20));
				DrawText("Doji", 0, (num > 0) ? ((NinjaScriptBase)this).High[0] : ((NinjaScriptBase)this).Low[0], num);
				break;
			}
			case ChartPattern.DownsideTasukiGap:
				((NinjaScriptBase)this).BarBrushes[2] = downBrush;
				((NinjaScriptBase)this).BarBrushes[1] = downBrush;
				((NinjaScriptBase)this).BarBrushes[0] = upBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[0] = downBrush;
				DrawText("Downside Tasuki Gap", 1, ((NinjaScriptBase)MAX(((NinjaScriptBase)this).High, 3))[0], 10);
				break;
			case ChartPattern.EveningStar:
				((NinjaScriptBase)this).BarBrushes[2] = ((((NinjaScriptBase)this).Close[2] > ((NinjaScriptBase)this).Open[2]) ? upBrush : downBrush);
				((NinjaScriptBase)this).BarBrushes[1] = ((((NinjaScriptBase)this).Close[1] > ((NinjaScriptBase)this).Open[1]) ? upBrush : downBrush);
				((NinjaScriptBase)this).BarBrushes[0] = ((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0]) ? upBrush : downBrush);
				((NinjaScriptBase)this).CandleOutlineBrushes[2] = ((((NinjaScriptBase)this).Close[2] > ((NinjaScriptBase)this).Open[2]) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[2]);
				((NinjaScriptBase)this).CandleOutlineBrushes[1] = ((((NinjaScriptBase)this).Close[1] > ((NinjaScriptBase)this).Open[1]) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[1]);
				((NinjaScriptBase)this).CandleOutlineBrushes[0] = ((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0]) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[0]);
				DrawText("Evening Star", 1, ((NinjaScriptBase)MAX(((NinjaScriptBase)this).High, 3))[0], 40);
				break;
			case ChartPattern.FallingThreeMethods:
			{
				((NinjaScriptBase)this).BarBrushes[4] = downBrush;
				((NinjaScriptBase)this).BarBrushes[0] = downBrush;
				for (int j = 1; j < 4; j++)
				{
					((NinjaScriptBase)this).BarBrushes[j] = ((((NinjaScriptBase)this).Close[j] > ((NinjaScriptBase)this).Open[j]) ? upBrush : downBrush);
					((NinjaScriptBase)this).CandleOutlineBrushes[j] = ((((NinjaScriptBase)this).Close[j] > ((NinjaScriptBase)this).Open[j]) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[j]);
				}
				DrawText("Falling Three Methods", 2, Math.Max(((NinjaScriptBase)this).High[0], ((NinjaScriptBase)this).High[4]), 40);
				break;
			}
			case ChartPattern.Hammer:
				((NinjaScriptBase)this).BarBrushes[0] = ((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0]) ? upBrush : downBrush);
				((NinjaScriptBase)this).CandleOutlineBrushes[0] = ((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0]) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[0]);
				DrawText("Hammer", 0, ((NinjaScriptBase)this).Low[0], -20);
				break;
			case ChartPattern.HangingMan:
				((NinjaScriptBase)this).BarBrushes[0] = ((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0]) ? upBrush : downBrush);
				((NinjaScriptBase)this).CandleOutlineBrushes[0] = ((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0]) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[0]);
				DrawText("Hanging Man", 0, ((NinjaScriptBase)this).Low[0], -20);
				break;
			case ChartPattern.InvertedHammer:
				((NinjaScriptBase)this).BarBrushes[0] = ((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0]) ? upBrush : downBrush);
				((NinjaScriptBase)this).CandleOutlineBrushes[0] = ((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0]) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[0]);
				DrawText("Inverted Hammer", 0, ((NinjaScriptBase)this).Low[0] - 2.0 * ((NinjaScriptBase)this).TickSize, 20);
				break;
			case ChartPattern.MorningStar:
				((NinjaScriptBase)this).BarBrushes[2] = ((((NinjaScriptBase)this).Close[2] > ((NinjaScriptBase)this).Open[2]) ? upBrush : downBrush);
				((NinjaScriptBase)this).BarBrushes[1] = ((((NinjaScriptBase)this).Close[1] > ((NinjaScriptBase)this).Open[1]) ? upBrush : downBrush);
				((NinjaScriptBase)this).BarBrushes[0] = ((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0]) ? upBrush : downBrush);
				((NinjaScriptBase)this).CandleOutlineBrushes[2] = ((((NinjaScriptBase)this).Close[2] > ((NinjaScriptBase)this).Open[2]) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[2]);
				((NinjaScriptBase)this).CandleOutlineBrushes[1] = ((((NinjaScriptBase)this).Close[1] > ((NinjaScriptBase)this).Open[1]) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[1]);
				((NinjaScriptBase)this).CandleOutlineBrushes[0] = ((((NinjaScriptBase)this).Close[0] > ((NinjaScriptBase)this).Open[0]) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[0]);
				DrawText("Morning Star", 1, ((NinjaScriptBase)MIN(((NinjaScriptBase)this).Low, 3))[0], -20);
				break;
			case ChartPattern.PiercingLine:
				((NinjaScriptBase)this).BarBrushes[1] = upBrush;
				((NinjaScriptBase)this).BarBrushes[0] = downBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[1] = downBrush;
				DrawText("Piercing Line", 1, ((NinjaScriptBase)this).Low[0], -10);
				break;
			case ChartPattern.RisingThreeMethods:
			{
				((NinjaScriptBase)this).BarBrushes[4] = upBrush;
				((NinjaScriptBase)this).BarBrushes[0] = upBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[4] = downBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[0] = downBrush;
				for (int i = 1; i < 4; i++)
				{
					((NinjaScriptBase)this).BarBrushes[i] = ((((NinjaScriptBase)this).Close[i] > ((NinjaScriptBase)this).Open[i]) ? upBrush : downBrush);
					((NinjaScriptBase)this).CandleOutlineBrushes[i] = ((((NinjaScriptBase)this).Close[i] > ((NinjaScriptBase)this).Open[i]) ? downBrush : ((NinjaScriptBase)this).CandleOutlineBrushes[i]);
				}
				DrawText("Rising Three Methods", 2, ((NinjaScriptBase)MIN(((NinjaScriptBase)this).Low, 5))[0], -10);
				break;
			}
			case ChartPattern.ShootingStar:
				((NinjaScriptBase)this).BarBrushes[0] = downBrush;
				DrawText("Shooting Star", 0, ((NinjaScriptBase)this).High[0], 30);
				break;
			case ChartPattern.StickSandwich:
				((NinjaScriptBase)this).BarBrushes[2] = downBrush;
				((NinjaScriptBase)this).BarBrushes[1] = upBrush;
				((NinjaScriptBase)this).BarBrushes[0] = downBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[1] = downBrush;
				DrawText("Stick Sandwich", 1, ((NinjaScriptBase)MAX(((NinjaScriptBase)this).High, 3))[0], 50);
				break;
			case ChartPattern.ThreeBlackCrows:
				((NinjaScriptBase)this).BarBrushes[2] = downBrush;
				((NinjaScriptBase)this).BarBrushes[1] = downBrush;
				((NinjaScriptBase)this).BarBrushes[0] = downBrush;
				DrawText("Three Black Crows", 1, ((NinjaScriptBase)MAX(((NinjaScriptBase)this).High, 3))[0], 50);
				break;
			case ChartPattern.ThreeWhiteSoldiers:
				((NinjaScriptBase)this).BarBrushes[2] = upBrush;
				((NinjaScriptBase)this).BarBrushes[1] = upBrush;
				((NinjaScriptBase)this).BarBrushes[0] = upBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[2] = downBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[1] = downBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[0] = downBrush;
				DrawText("Three White Soldiers", 1, ((NinjaScriptBase)this).Low[2], -10);
				break;
			case ChartPattern.UpsideGapTwoCrows:
				((NinjaScriptBase)this).BarBrushes[2] = upBrush;
				((NinjaScriptBase)this).BarBrushes[1] = downBrush;
				((NinjaScriptBase)this).BarBrushes[0] = downBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[2] = downBrush;
				DrawText("Upside Gap Two Crows", 1, Math.Max(((NinjaScriptBase)this).High[0], ((NinjaScriptBase)this).High[1]), 10);
				break;
			case ChartPattern.UpsideTasukiGap:
				((NinjaScriptBase)this).BarBrushes[2] = upBrush;
				((NinjaScriptBase)this).BarBrushes[1] = upBrush;
				((NinjaScriptBase)this).BarBrushes[0] = downBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[2] = downBrush;
				((NinjaScriptBase)this).CandleOutlineBrushes[1] = downBrush;
				DrawText("Upide Tasuki Gap", 1, ((NinjaScriptBase)MIN(((NinjaScriptBase)this).Low, 3))[0], -20);
				break;
			}
			if (ShowAlerts)
			{
				((NinjaScriptBase)this).Alert("myAlert", (Priority)2, $"Pattern(s) found: {numPatternsFound} {Pattern} on {((NinjaScriptBase)this).Instrument.FullName} {((NinjaScriptBase)this).BarsPeriod.Value} {((NinjaScriptBase)this).BarsPeriod.BarsPeriodType} Chart", "Alert3.wav", 10, (Brush)Brushes.Transparent, textBrush);
			}
		}
		if (ShowPatternCount)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "Count", $"{numPatternsFound} {Pattern}\n patterns found", textBoxPosition, textBrush, TextFont, Brushes.Transparent, Brushes.Transparent, 0);
		}
	}

	public override string ToString()
	{
		return $"{((NinjaScriptBase)this).Name}({Pattern})";
	}
}

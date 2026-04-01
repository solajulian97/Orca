using System;
using NinjaTrader.Cbi;

namespace NinjaTrader.NinjaScript.Indicators;

public class CandleStickPatternLogic
{
	private bool isInDownTrend;

	private bool isInUpTrend;

	private MAX max;

	private MIN min;

	private readonly NinjaScriptBase ninjaScript;

	private readonly bool[] prior = new bool[2];

	private Swing swing;

	private readonly int trendStrength;

	public CandleStickPatternLogic(NinjaScriptBase ninjaScript, int trendStrength)
	{
		this.ninjaScript = ninjaScript;
		this.trendStrength = trendStrength;
	}

	public bool Evaluate(ChartPattern pattern)
	{
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0471: Unknown result type (might be due to invalid IL or missing references)
		if (ninjaScript.CurrentBar < trendStrength || ninjaScript.CurrentBar < 2)
		{
			return false;
		}
		if (max == null && trendStrength > 0 && (pattern == ChartPattern.HangingMan || pattern == ChartPattern.InvertedHammer))
		{
			max = new MAX
			{
				Period = trendStrength
			};
			try
			{
				((NinjaScript)max).SetState((State)2);
			}
			catch (Exception ex)
			{
				Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
				{
					((NinjaScriptBase)max).Name,
					(ex.InnerException != null) ? ex.InnerException.ToString() : ex.ToString()
				}, (LogLevel)3, (LogCategories)4);
				((NinjaScript)max).SetState((State)9);
			}
			((NinjaScriptBase)max).Parent = ninjaScript;
			((NinjaScriptBase)max).SetInput(ninjaScript.High);
			lock (ninjaScript.NinjaScripts)
			{
				ninjaScript.NinjaScripts.Add((NinjaScriptBase)(object)max);
			}
			try
			{
				((NinjaScript)max).SetState(((NinjaScript)ninjaScript).State);
			}
			catch (Exception ex2)
			{
				Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
				{
					((NinjaScriptBase)max).Name,
					(ex2.InnerException != null) ? ex2.InnerException.ToString() : ex2.ToString()
				}, (LogLevel)3, (LogCategories)4);
				((NinjaScript)max).SetState((State)9);
				return false;
			}
		}
		if (min == null && trendStrength > 0 && pattern == ChartPattern.Hammer)
		{
			min = new MIN
			{
				Period = trendStrength
			};
			try
			{
				((NinjaScript)min).SetState((State)2);
			}
			catch (Exception ex3)
			{
				Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
				{
					((NinjaScriptBase)min).Name,
					(ex3.InnerException != null) ? ex3.InnerException.ToString() : ex3.ToString()
				}, (LogLevel)3, (LogCategories)4);
				((NinjaScript)min).SetState((State)9);
			}
			((NinjaScriptBase)min).Parent = ninjaScript;
			((NinjaScriptBase)min).SetInput(ninjaScript.Low);
			lock (ninjaScript.NinjaScripts)
			{
				ninjaScript.NinjaScripts.Add((NinjaScriptBase)(object)min);
			}
			try
			{
				((NinjaScript)min).SetState(((NinjaScript)ninjaScript).State);
			}
			catch (Exception ex4)
			{
				Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
				{
					((NinjaScriptBase)min).Name,
					(ex4.InnerException != null) ? ex4.InnerException.ToString() : ex4.ToString()
				}, (LogLevel)3, (LogCategories)4);
				((NinjaScript)min).SetState((State)9);
				return false;
			}
		}
		if (pattern != ChartPattern.Doji && pattern != ChartPattern.DownsideTasukiGap && pattern != ChartPattern.EveningStar && pattern != ChartPattern.FallingThreeMethods && pattern != ChartPattern.MorningStar && pattern != ChartPattern.RisingThreeMethods && pattern != ChartPattern.StickSandwich && pattern != ChartPattern.UpsideTasukiGap)
		{
			if (trendStrength == 0)
			{
				isInDownTrend = true;
				isInUpTrend = true;
			}
			else
			{
				if (swing == null)
				{
					swing = new Swing
					{
						Strength = trendStrength
					};
					try
					{
						((NinjaScript)swing).SetState((State)2);
					}
					catch (Exception ex5)
					{
						Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
						{
							((NinjaScriptBase)swing).Name,
							(ex5.InnerException != null) ? ex5.InnerException.ToString() : ex5.ToString()
						}, (LogLevel)3, (LogCategories)4);
						((NinjaScript)swing).SetState((State)9);
					}
					((NinjaScriptBase)swing).Parent = ninjaScript;
					((NinjaScriptBase)swing).SetInput(ninjaScript.Input);
					lock (ninjaScript.NinjaScripts)
					{
						ninjaScript.NinjaScripts.Add((NinjaScriptBase)(object)swing);
					}
					try
					{
						((NinjaScript)swing).SetState(((NinjaScript)ninjaScript).State);
					}
					catch (Exception ex6)
					{
						Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
						{
							((NinjaScriptBase)swing).Name,
							(ex6.InnerException != null) ? ex6.InnerException.ToString() : ex6.ToString()
						}, (LogLevel)3, (LogCategories)4);
						((NinjaScript)swing).SetState((State)9);
						return false;
					}
				}
				int num = 0;
				int num2 = 0;
				int num3 = 1;
				while (ninjaScript.Low[num2] <= ninjaScript.Low[num])
				{
					num = swing.SwingLowBar(0, num3 + 1, ninjaScript.CurrentBar);
					num2 = swing.SwingLowBar(0, num3, ninjaScript.CurrentBar);
					if (num < 0 || num2 < 0)
					{
						break;
					}
					num3++;
				}
				int num4 = 0;
				int num5 = 0;
				int num6 = 1;
				while (ninjaScript.High[num5] >= ninjaScript.High[num4])
				{
					num4 = swing.SwingHighBar(0, num6 + 1, ninjaScript.CurrentBar);
					num5 = swing.SwingHighBar(0, num6, ninjaScript.CurrentBar);
					if (num4 < 0 || num5 < 0)
					{
						break;
					}
					num6++;
				}
				if (num > 0 && num2 > 0 && num < num4)
				{
					isInDownTrend = false;
					isInUpTrend = true;
				}
				else if (num4 > 0 && num5 > 0 && num > num4)
				{
					isInDownTrend = true;
					isInUpTrend = false;
				}
				else
				{
					isInDownTrend = false;
					isInUpTrend = false;
				}
			}
		}
		bool flag = false;
		NinjaScriptBase val = ninjaScript;
		if (!prior[0] && !prior[1])
		{
			switch (pattern)
			{
			case ChartPattern.BearishBeltHold:
				flag = isInUpTrend && val.Close[1] > val.Open[1] && val.Open[0] > val.Close[1] + 5.0 * val.TickSize && val.Open[0] == val.High[0] && val.Close[0] < val.Open[0];
				break;
			case ChartPattern.BearishEngulfing:
				flag = isInUpTrend && val.Close[1] > val.Open[1] && val.Close[0] < val.Open[0] && val.Open[0] > val.Close[1] && val.Close[0] < val.Open[1];
				break;
			case ChartPattern.BearishHarami:
				flag = isInUpTrend && val.Close[0] < val.Open[0] && val.Close[1] > val.Open[1] && val.Low[0] >= val.Open[1] && val.High[0] <= val.Close[1];
				break;
			case ChartPattern.BearishHaramiCross:
				flag = isInUpTrend && val.High[0] <= val.Close[1] && val.Low[0] >= val.Open[1] && val.Open[0] <= val.Close[1] && val.Close[0] >= val.Open[1] && ((val.Close[0] >= val.Open[0] && val.Close[0] <= val.Open[0] + val.TickSize) || (val.Close[0] <= val.Open[0] && val.Close[0] >= val.Open[0] - val.TickSize));
				break;
			case ChartPattern.BullishBeltHold:
				flag = isInDownTrend && val.Close[1] < val.Open[1] && val.Open[0] < val.Close[1] - 5.0 * val.TickSize && val.Open[0] == val.Low[0] && val.Close[0] > val.Open[0];
				break;
			case ChartPattern.BullishEngulfing:
				flag = isInDownTrend && val.Close[1] < val.Open[1] && val.Close[0] > val.Open[0] && val.Close[0] > val.Open[1] && val.Open[0] < val.Close[1];
				break;
			case ChartPattern.BullishHarami:
				flag = isInDownTrend && val.Close[0] > val.Open[0] && val.Close[1] < val.Open[1] && val.Low[0] >= val.Close[1] && val.High[0] <= val.Open[1];
				break;
			case ChartPattern.BullishHaramiCross:
				flag = isInDownTrend && val.High[0] <= val.Open[1] && val.Low[0] >= val.Close[1] && val.Open[0] >= val.Close[1] && val.Close[0] <= val.Open[1] && ((val.Close[0] >= val.Open[0] && val.Close[0] <= val.Open[0] + val.TickSize) || (val.Close[0] <= val.Open[0] && val.Close[0] >= val.Open[0] - val.TickSize));
				break;
			case ChartPattern.DarkCloudCover:
				flag = isInUpTrend && val.Open[0] > val.High[1] && val.Close[1] > val.Open[1] && val.Close[0] < val.Open[0] && val.Close[0] <= val.Close[1] - (val.Close[1] - val.Open[1]) / 2.0 && val.Close[0] >= val.Open[1];
				break;
			case ChartPattern.Doji:
				flag = Math.Abs(val.Close[0] - val.Open[0]) <= (val.High[0] - val.Low[0]) * 0.07;
				break;
			case ChartPattern.DownsideTasukiGap:
				flag = val.Close[2] < val.Open[2] && val.Close[1] < val.Open[1] && val.Close[0] > val.Open[0] && val.High[1] < val.Low[2] && val.Open[0] > val.Close[1] && val.Open[0] < val.Open[1] && val.Close[0] > val.Open[1] && val.Close[0] < val.Close[2];
				break;
			case ChartPattern.EveningStar:
				flag = val.Close[2] > val.Open[2] && val.Close[1] > val.Close[2] && val.Open[0] < Math.Abs((val.Close[1] - val.Open[1]) / 2.0) + val.Open[1] && val.Close[0] < val.Open[0];
				break;
			case ChartPattern.FallingThreeMethods:
				flag = val.CurrentBar > 5 && val.Close[4] < val.Open[4] && val.Close[0] < val.Open[0] && val.Close[0] < val.Low[4] && val.High[3] < val.High[4] && val.Low[3] > val.Low[4] && val.High[2] < val.High[4] && val.Low[2] > val.Low[4] && val.High[1] < val.High[4] && val.Low[1] > val.Low[4];
				break;
			case ChartPattern.Hammer:
				flag = isInDownTrend && (min == null || ((NinjaScriptBase)min)[0] == val.Low[0]) && val.Low[0] < val.Open[0] - 5.0 * val.TickSize && Math.Abs(val.Open[0] - val.Close[0]) < 0.1 * (val.High[0] - val.Low[0]) && val.High[0] - val.Close[0] < 0.25 * (val.High[0] - val.Low[0]);
				break;
			case ChartPattern.HangingMan:
				flag = isInUpTrend && (max == null || ((NinjaScriptBase)max)[0] == val.High[0]) && val.Low[0] < val.Open[0] - 5.0 * val.TickSize && Math.Abs(val.Open[0] - val.Close[0]) < 0.1 * (val.High[0] - val.Low[0]) && val.High[0] - val.Close[0] < 0.25 * (val.High[0] - val.Low[0]);
				break;
			case ChartPattern.InvertedHammer:
				flag = isInUpTrend && (max == null || ((NinjaScriptBase)max)[0] == val.High[0]) && val.High[0] > val.Open[0] + 5.0 * val.TickSize && Math.Abs(val.Open[0] - val.Close[0]) < 0.1 * (val.High[0] - val.Low[0]) && val.Close[0] - val.Low[0] < 0.25 * (val.High[0] - val.Low[0]);
				break;
			case ChartPattern.MorningStar:
				flag = val.Close[2] < val.Open[2] && val.Close[1] < val.Close[2] && val.Open[0] > Math.Abs((val.Close[1] - val.Open[1]) / 2.0) + val.Open[1] && val.Close[0] > val.Open[0];
				break;
			case ChartPattern.PiercingLine:
				flag = isInDownTrend && val.Open[0] < val.Low[1] && val.Close[1] < val.Open[1] && val.Close[0] > val.Open[0] && val.Close[0] >= val.Close[1] + (val.Open[1] - val.Close[1]) / 2.0 && val.Close[0] <= val.Open[1];
				break;
			case ChartPattern.RisingThreeMethods:
				flag = val.CurrentBar > 5 && val.Close[4] > val.Open[4] && val.Close[0] > val.Open[0] && val.Close[0] > val.High[4] && val.High[3] < val.High[4] && val.Low[3] > val.Low[4] && val.High[2] < val.High[4] && val.Low[2] > val.Low[4] && val.High[1] < val.High[4] && val.Low[1] > val.Low[4];
				break;
			case ChartPattern.ShootingStar:
				flag = isInUpTrend && val.High[0] > val.Open[0] && val.High[0] - val.Open[0] >= 2.0 * (val.Open[0] - val.Close[0]) && val.Close[0] < val.Open[0] && val.Close[0] - val.Low[0] <= 2.0 * val.TickSize;
				break;
			case ChartPattern.StickSandwich:
				flag = val.Close[2] == val.Close[0] && val.Close[2] < val.Open[2] && val.Close[1] > val.Open[1] && val.Close[0] < val.Open[0];
				break;
			case ChartPattern.ThreeBlackCrows:
				flag = isInUpTrend && val.Close[0] < val.Open[0] && val.Close[1] < val.Open[1] && val.Close[2] < val.Open[2] && val.Close[0] < val.Close[1] && val.Close[1] < val.Close[2] && val.Open[0] < val.Open[1] && val.Open[0] > val.Close[1] && val.Open[1] < val.Open[2] && val.Open[1] > val.Close[2];
				break;
			case ChartPattern.ThreeWhiteSoldiers:
				flag = isInDownTrend && val.Close[0] > val.Open[0] && val.Close[1] > val.Open[1] && val.Close[2] > val.Open[2] && val.Close[0] > val.Close[1] && val.Close[1] > val.Close[2] && val.Open[0] < val.Close[1] && val.Open[0] > val.Open[1] && val.Open[1] < val.Close[2] && val.Open[1] > val.Open[2];
				break;
			case ChartPattern.UpsideGapTwoCrows:
				flag = isInUpTrend && val.Close[2] > val.Open[2] && val.Close[1] < val.Open[1] && val.Close[0] < val.Open[0] && val.Low[1] > val.High[2] && val.Close[0] > val.High[2] && val.Close[0] < val.Close[1] && val.Open[0] > val.Open[1];
				break;
			case ChartPattern.UpsideTasukiGap:
				flag = val.Close[2] > val.Open[2] && val.Close[1] > val.Open[1] && val.Close[0] < val.Open[0] && val.Low[1] > val.High[2] && val.Open[0] < val.Close[1] && val.Open[0] > val.Open[1] && val.Close[0] < val.Open[1] && val.Close[0] > val.Close[2];
				break;
			}
		}
		prior[val.CurrentBars[0] % 2] = flag;
		return flag;
	}
}

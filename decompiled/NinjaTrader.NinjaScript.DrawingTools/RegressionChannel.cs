using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Regression Channel IDrawingTool.
/// </summary>
[TypeConverter("NinjaTrader.NinjaScript.DrawingTools.RegressionChannelTypeConverter")]
public class RegressionChannel : DrawingTool
{
	[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
	public enum RegressionChannelType
	{
		Segment,
		StandardDeviation
	}

	private ChartControl cControl;

	private ChartScale cScale;

	private ChartAnchor editingAnchor;

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[2] { StartAnchor, EndAnchor };

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRegressionChannelType", GroupName = "NinjaScriptGeneral", Order = 2)]
	[RefreshProperties(RefreshProperties.All)]
	public RegressionChannelType ChannelType { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRegressionChannelStandardDeviationExtendLeft", GroupName = "NinjaScriptLines")]
	public bool ExtendLeft { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRegressionChannelStandardDeviationExtendRight", GroupName = "NinjaScriptLines")]
	public bool ExtendRight { get; set; }

	[Display(Order = 2)]
	public ChartAnchor EndAnchor { get; set; }

	public override object Icon => Icons.DrawRegressionChannel;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRegressionChannelLowerChannel", GroupName = "NinjaScriptLines", Order = 3)]
	public Stroke LowerChannelStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRegressionChannelRegressionChannel", GroupName = "NinjaScriptLines", Order = 2)]
	public Stroke RegressionStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRegressionChannelPriceType", GroupName = "NinjaScriptGeneral", Order = 1)]
	public PriceType PriceType { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRegressionChannelStandardDeviationUpperDistance", GroupName = "NinjaScriptGeneral", Order = 3)]
	public double StandardDeviationUpperDistance { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRegressionChannelStandardDeviationLowerDistance", GroupName = "NinjaScriptGeneral", Order = 4)]
	public double StandardDeviationLowerDistance { get; set; }

	[Display(Order = 1)]
	public ChartAnchor StartAnchor { get; set; }

	public override bool SupportsAlerts => true;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRegressionChannelUpperChannel", GroupName = "NinjaScriptLines", Order = 1)]
	public Stroke UpperChannelStroke { get; set; }

	public override void AddPastedOffset(ChartPanel panel, ChartScale chartScale)
	{
	}

	public double[] CalculateRegressionPriceValues(Bars baseBars, int startIndex, int endIndex)
	{
		double barPrice;
		double barPrice2;
		double barPrice3;
		double barPrice4;
		double barPrice5;
		double barPrice6;
		if (startIndex == endIndex)
		{
			barPrice = GetBarPrice(baseBars, endIndex);
			barPrice2 = GetBarPrice(baseBars, endIndex);
			barPrice3 = GetBarPrice(baseBars, endIndex);
			barPrice4 = GetBarPrice(baseBars, endIndex);
			barPrice5 = GetBarPrice(baseBars, endIndex);
			barPrice6 = GetBarPrice(baseBars, endIndex);
			return new double[6] { barPrice3, barPrice4, barPrice, barPrice2, barPrice5, barPrice6 };
		}
		int num = ((startIndex < endIndex) ? startIndex : endIndex);
		int num2 = Math.Abs(endIndex - startIndex) + 1;
		double num3 = (double)(num2 * (num2 - 1)) * 0.5;
		double num4 = num3 * num3 - (double)(num2 * num2) * ((double)num2 - 1.0) * (2.0 * (double)num2 - 1.0) / 6.0;
		double num5 = 0.0;
		double num6 = 0.0;
		for (int i = 0; i < num2; i++)
		{
			int num7 = num + i;
			if (num7 < baseBars.Count)
			{
				double barPrice7 = GetBarPrice(baseBars, num7);
				num5 += (double)i * barPrice7;
				num6 += barPrice7;
			}
		}
		double num8 = ((double)num2 * num5 - num3 * num6) / num4;
		double num9 = (num6 - num8 * num3) / (double)num2;
		double num10 = 0.0;
		for (int j = 0; j < num2; j++)
		{
			int num11 = num + j;
			if (num11 < baseBars.Count)
			{
				double num12 = Math.Abs(GetBarPrice(baseBars, num11) - (num9 + num8 * ((double)num2 - 1.0 - (double)j)));
				num10 += num12;
			}
		}
		double num13 = num10 / (double)num2;
		num10 = 0.0;
		for (int k = 0; k < num2; k++)
		{
			int num14 = num + k;
			if (num14 < baseBars.Count)
			{
				double num15 = Math.Abs(GetBarPrice(baseBars, num14) - (num9 + num8 * ((double)num2 - 1.0 - (double)k)));
				num10 += (num15 - num13) * (num15 - num13);
			}
		}
		double num16 = Math.Sqrt(num10 / (double)num2);
		barPrice = num9 + num8 * ((double)num2 - 1.0);
		barPrice2 = num9;
		barPrice3 = barPrice + num16 * StandardDeviationUpperDistance;
		barPrice4 = num9 + num16 * StandardDeviationUpperDistance;
		barPrice5 = barPrice - num16 * StandardDeviationLowerDistance;
		barPrice6 = num9 - num16 * StandardDeviationLowerDistance;
		if (startIndex > endIndex)
		{
			barPrice = num9;
			barPrice2 = num9 - num8 * (-1.0 * (double)num2 + 1.0);
			barPrice3 = num9 - num16 * (0.0 - StandardDeviationUpperDistance);
			barPrice4 = barPrice2 - num16 * (0.0 - StandardDeviationUpperDistance);
			barPrice5 = barPrice + num16 * (0.0 - StandardDeviationLowerDistance);
			barPrice6 = barPrice2 + num16 * (0.0 - StandardDeviationLowerDistance);
		}
		if (ChannelType == RegressionChannelType.Segment)
		{
			int num17 = int.MinValue;
			int num18 = int.MaxValue;
			double num19 = double.MinValue;
			double num20 = double.MaxValue;
			for (int l = 0; l < num2; l++)
			{
				int num21 = num + l;
				if (num19 < baseBars.GetHigh(num21))
				{
					num19 = baseBars.GetHigh(num21);
					num17 = num21;
				}
				if (num20 > baseBars.GetLow(num21))
				{
					num20 = baseBars.GetLow(num21);
					num18 = num21;
				}
			}
			double num22 = num19 - (num9 + num8 * (double)(endIndex - num17));
			double num23 = num9 + num8 * (double)(endIndex - num18) - num20;
			barPrice3 = barPrice + num22;
			barPrice4 = barPrice2 + num22;
			barPrice5 = barPrice - num23;
			barPrice6 = barPrice2 - num23;
			if (startIndex > endIndex)
			{
				num17 = int.MinValue;
				num18 = int.MaxValue;
				num19 = double.MinValue;
				num20 = double.MaxValue;
				for (int m = 0; m < num2; m++)
				{
					int num24 = endIndex + m;
					if (num19 < baseBars.GetHigh(num24))
					{
						num19 = baseBars.GetHigh(num24);
						num17 = num24;
					}
					if (num20 > baseBars.GetLow(num24))
					{
						num20 = baseBars.GetLow(num24);
						num18 = num24;
					}
				}
				num22 = num19 - (num9 + num8 * (double)(startIndex - num17));
				num23 = num9 + num8 * (double)(startIndex - num18) - num20;
				barPrice3 = barPrice + Math.Abs(num22);
				barPrice4 = barPrice2 + Math.Abs(num22);
				barPrice5 = barPrice - Math.Abs(num23);
				barPrice6 = barPrice2 - Math.Abs(num23);
			}
		}
		return new double[6] { barPrice3, barPrice4, barPrice, barPrice2, barPrice5, barPrice6 };
	}

	private Point[] CreateRegressionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		if (chartControl.BarsArray.Count == 0)
		{
			return null;
		}
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Bars bars = ((DrawingTool)this).GetAttachedToChartBars().Bars;
		int bar = bars.GetBar(StartAnchor.Time);
		int bar2 = bars.GetBar(EndAnchor.Time);
		double[] array = CalculateRegressionPriceValues(bars, bar, bar2);
		return new Point[6]
		{
			new Point(point.X, chartScale.GetYByValue(array[0])),
			new Point(point2.X, chartScale.GetYByValue(array[1])),
			new Point(point.X, chartScale.GetYByValue(array[2])),
			new Point(point2.X, chartScale.GetYByValue(array[3])),
			new Point(point.X, chartScale.GetYByValue(array[4])),
			new Point(point2.X, chartScale.GetYByValue(array[5]))
		};
	}

	public double GetBarPrice(Bars barObject, int barIndex)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected I4, but got Unknown
		if (barObject == null || !barObject.IsValidDataPointAt(barIndex))
		{
			return double.MinValue;
		}
		PriceType priceType = PriceType;
		return (priceType - 1) switch
		{
			0 => barObject.GetHigh(barIndex), 
			1 => barObject.GetLow(barIndex), 
			3 => barObject.GetOpen(barIndex), 
			2 => (barObject.GetHigh(barIndex) + barObject.GetLow(barIndex)) / 2.0, 
			4 => (barObject.GetHigh(barIndex) + barObject.GetLow(barIndex) + barObject.GetClose(barIndex)) / 3.0, 
			5 => (barObject.GetHigh(barIndex) + barObject.GetLow(barIndex) + barObject.GetClose(barIndex) * 2.0) / 4.0, 
			_ => barObject.GetClose(barIndex), 
		};
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		yield return new AlertConditionItem
		{
			ShouldOnlyDisplayName = true,
			Name = Resource.NinjaScriptDrawingToolRegressionChannelUpperChannel
		};
		yield return new AlertConditionItem
		{
			ShouldOnlyDisplayName = true,
			Name = Resource.NinjaScriptDrawingToolRegressionChannel
		};
		yield return new AlertConditionItem
		{
			ShouldOnlyDisplayName = true,
			Name = Resource.NinjaScriptDrawingToolRegressionChannelLowerChannel
		};
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		switch ((int)drawingState)
		{
		case 0:
			return Cursors.Pen;
		case 3:
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.No;
		case 1:
			if (((DrawingTool)this).IsLocked)
			{
				return Cursors.No;
			}
			if (editingAnchor != StartAnchor)
			{
				return Cursors.SizeNWSE;
			}
			return Cursors.SizeNESW;
		default:
		{
			ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 10.0, point);
			if (closestAnchor != null)
			{
				if (((DrawingTool)this).IsLocked)
				{
					return Cursors.Arrow;
				}
				if (closestAnchor != StartAnchor)
				{
					return Cursors.SizeNESW;
				}
				return Cursors.SizeNWSE;
			}
			Point point2 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point3 = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point4 = point2;
			if (ExtendLeft)
			{
				point3 = ((DrawingTool)this).GetExtendedPoint(chartControl, chartPanel, chartScale, EndAnchor, StartAnchor);
			}
			if (ExtendRight)
			{
				point4 = ((DrawingTool)this).GetExtendedPoint(chartControl, chartPanel, chartScale, StartAnchor, EndAnchor);
			}
			Vector vector = point4 - point3;
			if (MathHelper.IsPointAlongVector(point, point3, vector, 10.0))
			{
				if (!((DrawingTool)this).IsLocked)
				{
					return Cursors.SizeAll;
				}
				return Cursors.Arrow;
			}
			return null;
		}
		}
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point3 = new Point((point.X + point2.X) / 2.0, (point.Y + point2.Y) / 2.0);
		return new Point[3] { point, point3, point2 };
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Expected I4, but got Unknown
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Invalid comparison between Unknown and I4
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Invalid comparison between Unknown and I4
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Invalid comparison between Unknown and I4
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Invalid comparison between Unknown and I4
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Invalid comparison between Unknown and I4
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Invalid comparison between Unknown and I4
		Point[] array = CreateRegressionPoints(chartControl, chartScale);
		if (array == null)
		{
			return false;
		}
		Point point;
		Point point2;
		if (((ConditionItem)conditionItem).Name == Resource.NinjaScriptDrawingToolRegressionChannelUpperChannel)
		{
			point = array[0];
			point2 = array[1];
		}
		else if (((ConditionItem)conditionItem).Name == Resource.NinjaScriptDrawingToolRegressionChannel)
		{
			point = array[2];
			point2 = array[3];
		}
		else
		{
			point = array[4];
			point2 = array[5];
		}
		Point point3 = point;
		Point point4 = point2;
		if (ExtendLeft)
		{
			point3 = ((DrawingTool)this).GetExtendedPoint(chartControl, chartControl.ChartPanels[((DrawingTool)this).PanelIndex], chartScale, EndAnchor, StartAnchor);
		}
		if (ExtendRight)
		{
			point4 = ((DrawingTool)this).GetExtendedPoint(chartControl, chartControl.ChartPanels[((DrawingTool)this).PanelIndex], chartScale, StartAnchor, EndAnchor);
		}
		double num = chartControl.GetXByTime(values[0].Time);
		double y = chartScale.GetYByValue(values[0].Value);
		double val = double.MaxValue;
		double num2 = double.MinValue;
		Point[] array2 = new Point[2] { point, point2 };
		for (int i = 0; i < array2.Length; i++)
		{
			Point point5 = array2[i];
			val = Math.Min(val, point5.X);
			num2 = Math.Max(num2, point5.X);
		}
		if (num2 < num)
		{
			return false;
		}
		Point leftPoint = ((point3.X < point4.X) ? point : point4);
		Point rightPoint = ((point4.X > point3.X) ? point4 : point3);
		Point point6 = new Point(num, y);
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(leftPoint, rightPoint, point6);
		Condition val2 = condition;
		switch ((int)val2)
		{
		case 3:
			return (int)pointLineLocation == 0;
		case 4:
			if ((int)pointLineLocation == 0 || (int)pointLineLocation == 2)
			{
				return true;
			}
			return false;
		case 5:
			return (int)pointLineLocation == 1;
		case 6:
			if (pointLineLocation - 1 <= 1)
			{
				return true;
			}
			return false;
		case 2:
			return (int)pointLineLocation == 2;
		case 7:
			return (int)pointLineLocation != 2;
		case 0:
		case 1:
			return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate);
		default:
			return false;
		}
		bool Predicate(ChartAlertValue v)
		{
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Invalid comparison between Unknown and I4
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Invalid comparison between Unknown and I4
			double x = chartControl.GetXByTime(v.Time);
			double y2 = chartScale.GetYByValue(v.Value);
			Point point7 = new Point(x, y2);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(leftPoint, rightPoint, point7);
			if ((int)condition == 0)
			{
				return (int)pointLineLocation2 == 0;
			}
			return (int)pointLineLocation2 == 1;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		Point[] array = CreateRegressionPoints(chartControl, chartScale);
		if (array == null)
		{
			return false;
		}
		Point point = array[2];
		Point point2 = array[3];
		if (point.X > point2.X)
		{
			Point point3 = point2;
			point2 = point;
			point = point3;
		}
		Point point4 = point;
		Point point5 = point2;
		ChartAnchor val = ((StartAnchor.Time < EndAnchor.Time) ? StartAnchor : EndAnchor);
		ChartAnchor val2 = ((StartAnchor.Time < EndAnchor.Time) ? EndAnchor : StartAnchor);
		if (ExtendLeft)
		{
			point4 = ((DrawingTool)this).GetExtendedPoint(chartControl, chartControl.ChartPanels[((DrawingTool)this).PanelIndex], chartScale, val2, val);
		}
		if (ExtendRight)
		{
			point5 = ((DrawingTool)this).GetExtendedPoint(chartControl, chartControl.ChartPanels[((DrawingTool)this).PanelIndex], chartScale, val, val2);
		}
		DateTime timeByX = chartControl.GetTimeByX((int)point4.X);
		DateTime timeByX2 = chartControl.GetTimeByX((int)point5.X);
		if (timeByX >= firstTimeOnChart && timeByX <= lastTimeOnChart)
		{
			return true;
		}
		if (timeByX2 >= firstTimeOnChart && timeByX2 <= lastTimeOnChart)
		{
			return true;
		}
		if (!(timeByX2 < firstTimeOnChart) || !(timeByX > lastTimeOnChart))
		{
			if (timeByX < firstTimeOnChart)
			{
				return timeByX2 > lastTimeOnChart;
			}
			return false;
		}
		return true;
	}

	public override void OnBarsChanged()
	{
		if (cControl != null && cScale != null)
		{
			SetAnchorsToRegression(cControl, cScale);
		}
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (((NinjaScript)this).IsVisible)
		{
			((ChartObject)this).MinValue = ((StartAnchor.Price > EndAnchor.Price) ? StartAnchor.Price : EndAnchor.Price);
			((ChartObject)this).MaxValue = ((StartAnchor.Price > EndAnchor.Price) ? EndAnchor.Price : StartAnchor.Price);
		}
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState == 2)
			{
				Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
				editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 10.0, point);
				if (editingAnchor != null)
				{
					editingAnchor.IsEditing = true;
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) == null)
				{
					((ChartObject)this).IsSelected = false;
				}
				else
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
			}
			return;
		}
		if (StartAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(StartAnchor);
			dataPoint.CopyDataValues(EndAnchor);
			StartAnchor.IsEditing = false;
		}
		else if (EndAnchor.IsEditing)
		{
			EndAnchor.IsEditing = false;
		}
		if (!StartAnchor.IsEditing && !EndAnchor.IsEditing)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
		SetAnchorsToRegression(chartControl, chartScale);
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Invalid comparison between Unknown and I4
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Invalid comparison between Unknown and I4
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Expected O, but got Unknown
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Expected O, but got Unknown
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		Bars bars = ((DrawingTool)this).GetAttachedToChartBars().Bars;
		DateTime time = bars.GetTime(0);
		BarsPeriodType barsPeriodType = bars.BarsPeriod.BarsPeriodType;
		bool flag = barsPeriodType - 5 <= 3;
		DateTime dateTime = (flag ? bars.GetSessionEndTime(bars.Count - 1) : bars.GetTime(bars.Count - 1));
		ChartAnchor val = ((StartAnchor.Time < EndAnchor.Time) ? StartAnchor : EndAnchor);
		ChartAnchor val2 = ((StartAnchor.Time < EndAnchor.Time) ? EndAnchor : StartAnchor);
		DateTime time2 = dataPoint.Time;
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			ChartAnchor val3 = ((DrawingTool)this).Anchors.FirstOrDefault((ChartAnchor a) => a.IsEditing);
			if (val3 != null && time2 >= time && time2 <= dateTime)
			{
				dataPoint.CopyDataValues(val3);
				SetAnchorsToRegression(chartControl, chartScale);
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 1 && editingAnchor != null)
		{
			if (time2 >= time && time2 <= dateTime)
			{
				dataPoint.CopyDataValues(editingAnchor);
				SetAnchorsToRegression(chartControl, chartScale);
			}
		}
		else
		{
			if ((int)((DrawingTool)this).DrawingState != 3)
			{
				return;
			}
			ChartAnchor val4 = new ChartAnchor();
			val.CopyTo(val4);
			ChartAnchor val5 = new ChartAnchor();
			val2.CopyTo(val5);
			val4.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			val5.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			if (!(val4.Time >= time) || !(val5.Time <= dateTime))
			{
				return;
			}
			foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
			{
				anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			SetAnchorsToRegression(chartControl, chartScale);
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState == 1 || (int)((DrawingTool)this).DrawingState == 3)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
		}
		if (editingAnchor != null)
		{
			editingAnchor.IsEditing = false;
		}
		editingAnchor = null;
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		cControl = chartControl;
		cScale = chartScale;
		Point[] array = CreateRegressionPoints(chartControl, chartScale);
		if (array == null)
		{
			return;
		}
		RegressionStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		UpperChannelStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		LowerChannelStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		Point[] array2 = array.ToArray();
		for (int i = 0; i < array.Length; i += 2)
		{
			if (ExtendLeft)
			{
				array2[i] = ((DrawingTool)this).GetExtendedPoint(array[i + 1], array[i]);
			}
			if (ExtendRight)
			{
				array2[i + 1] = ((DrawingTool)this).GetExtendedPoint(array[i], array[i + 1]);
			}
		}
		Vector2 val = DxExtensions.ToVector2(array2[0]);
		Vector2 val2 = DxExtensions.ToVector2(array2[1]);
		Vector2 val3 = DxExtensions.ToVector2(array2[2]);
		Vector2 val4 = DxExtensions.ToVector2(array2[3]);
		Vector2 val5 = DxExtensions.ToVector2(array2[4]);
		Vector2 val6 = DxExtensions.ToVector2(array2[5]);
		Brush val7 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : UpperChannelStroke.BrushDX);
		RegressionStroke.RenderTarget.DrawLine(val, val2, val7, UpperChannelStroke.Width, UpperChannelStroke.StrokeStyle);
		val7 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : RegressionStroke.BrushDX);
		RegressionStroke.RenderTarget.DrawLine(val3, val4, val7, RegressionStroke.Width, RegressionStroke.StrokeStyle);
		val7 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : LowerChannelStroke.BrushDX);
		LowerChannelStroke.RenderTarget.DrawLine(val5, val6, val7, LowerChannelStroke.Width, LowerChannelStroke.StrokeStyle);
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			UpperChannelStroke = new Stroke((Brush)Brushes.CornflowerBlue, (DashStyleHelper)0, 2f);
			RegressionStroke = new Stroke((Brush)Brushes.DarkGray, (DashStyleHelper)0, 2f);
			LowerChannelStroke = new Stroke((Brush)Brushes.CornflowerBlue, (DashStyleHelper)0, 2f);
			((NinjaScript)this).Description = Resource.NinjaScriptDrawingToolRegressionChannel;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolRegressionChannel;
			ChannelType = RegressionChannelType.StandardDeviation;
			PriceType = (PriceType)0;
			StandardDeviationUpperDistance = 2.0;
			StandardDeviationLowerDistance = 2.0;
			StartAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this,
				DisplayName = Resource.NinjaScriptDrawingToolAnchorStart
			};
			EndAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this,
				DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd
			};
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	private void SetAnchorsToRegression(ChartControl chartControl, ChartScale chartScale)
	{
		Point[] array = CreateRegressionPoints(chartControl, chartScale);
		StartAnchor.UpdateYFromDevicePoint(array[2], chartScale);
		EndAnchor.UpdateYFromDevicePoint(array[3], chartScale);
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Gann Fan IDrawingTool.
/// </summary>
public class GannFan : GannAngleContainer
{
	[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
	public enum GannFanDirection
	{
		UpLeft,
		UpRight,
		DownLeft,
		DownRight
	}

	public ChartAnchor Anchor { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolGannFanFanDirection", GroupName = "NinjaScriptGeneral", Order = 3)]
	public GannFanDirection FanDirection { get; set; }

	public override object Icon => Icons.DrawGanFan;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolGannFanDisplayText", GroupName = "NinjaScriptGeneral", Order = 2)]
	public bool IsTextDisplayed { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolGannFanPointsPerBar", GroupName = "NinjaScriptGeneral", Order = 4)]
	public double PointsPerBar { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolPriceLevelsOpacity", GroupName = "NinjaScriptGeneral")]
	public int PriceLevelOpacity { get; set; }

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[1] { Anchor };

	public override bool SupportsAlerts => true;

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (((NinjaScript)this).IsVisible && !Anchor.IsEditing)
		{
			double minValue = (((ChartObject)this).MaxValue = Anchor.Price);
			((ChartObject)this).MinValue = minValue;
		}
	}

	public Point CalculateExtendedDataPoint(ChartPanel panel, ChartScale scale, int startX, double startPrice, Vector slope)
	{
		bool flag = slope.X > 0.0;
		bool flag2 = slope.Y > 0.0;
		double num = Math.Abs((double)(flag ? (panel.W - startX) : (panel.X + startX)) / slope.X) * slope.Y;
		double num2 = startPrice + num;
		double num3 = (flag2 ? panel.MaxValue : panel.MinValue);
		if (flag2 ? (num2 > num3) : (num3 > num2))
		{
			double num4 = Math.Abs(Math.Abs(num3 - startPrice) / slope.Y) * slope.X;
			return new Point((double)startX + num4, scale.GetYByValue(num3));
		}
		return new Point(flag ? panel.W : 0, scale.GetYByValue(num2));
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Invalid comparison between Unknown and I4
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Invalid comparison between Unknown and I4
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return Cursors.Pen;
		}
		if ((int)((DrawingTool)this).DrawingState == 3)
		{
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.No;
		}
		Point point2 = Anchor.GetPoint(chartControl, chartPanel, chartScale, true);
		Vector vector = point - point2;
		if ((int)((DrawingTool)this).DrawingState == 1 || vector.Length <= 10.0)
		{
			if (((DrawingTool)this).IsLocked)
			{
				if ((int)((DrawingTool)this).DrawingState != 1)
				{
					return Cursors.Arrow;
				}
				return Cursors.No;
			}
			return Cursors.SizeNESW;
		}
		foreach (Point gannEndPoint in GetGannEndPoints(chartControl, chartScale))
		{
			Vector vector2 = gannEndPoint - point2;
			if (MathHelper.IsPointAlongVector(point, point2, vector2, 10.0))
			{
				if (((DrawingTool)this).IsLocked)
				{
					return ((int)((DrawingTool)this).DrawingState == 1) ? Cursors.No : Cursors.Arrow;
				}
				return Cursors.SizeAll;
			}
		}
		return null;
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		if (base.GannAngles == null)
		{
			yield break;
		}
		foreach (GannAngle gannAngle in base.GannAngles)
		{
			yield return new AlertConditionItem
			{
				Name = gannAngle.Name,
				Tag = gannAngle,
				ShouldOnlyDisplayName = true
			};
		}
	}

	private IEnumerable<Point> GetGannEndPoints(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point anchorPoint = Anchor.GetPoint(chartControl, val, chartScale, true);
		foreach (GannAngle item in base.GannAngles.Where((GannAngle ga) => ga.IsVisible))
		{
			double deltaX = item.RatioX * (double)chartControl.Properties.BarDistance;
			double deltaPrice = item.RatioY * PointsPerBar;
			Point gannStepPoint = GetGannStepPoint(chartScale, anchorPoint.X, Anchor.Price, deltaX, deltaPrice);
			Point extendedPoint = ((DrawingTool)this).GetExtendedPoint(anchorPoint, gannStepPoint);
			yield return new Point(Math.Max(extendedPoint.X, 1.0), Math.Max(extendedPoint.Y, 1.0));
		}
	}

	private Point GetGannStepPoint(ChartScale scale, double startX, double startPrice, double deltaX, double deltaPrice)
	{
		double x;
		double num;
		switch (FanDirection)
		{
		case GannFanDirection.DownLeft:
			x = startX - deltaX;
			num = startPrice - deltaPrice;
			break;
		case GannFanDirection.DownRight:
			x = startX + deltaX;
			num = startPrice - deltaPrice;
			break;
		case GannFanDirection.UpLeft:
			x = startX - deltaX;
			num = startPrice + deltaPrice;
			break;
		default:
			x = startX + deltaX;
			num = startPrice + deltaPrice;
			break;
		}
		return new Point(x, scale.GetYByValue(num));
	}

	private Vector GetGannStepDataVector(double deltaX, double deltaPrice)
	{
		return FanDirection switch
		{
			GannFanDirection.DownLeft => new Vector(0.0 - deltaX, 0.0 - deltaPrice), 
			GannFanDirection.DownRight => new Vector(Math.Abs(deltaX), 0.0 - deltaPrice), 
			GannFanDirection.UpLeft => new Vector(0.0 - deltaX, Math.Abs(deltaPrice)), 
			_ => new Vector(Math.Abs(deltaX), Math.Abs(deltaPrice)), 
		};
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = Anchor.GetPoint(chartControl, val, chartScale, true);
		return new Point[1] { point };
	}

	public override IEnumerable<Condition> GetValidAlertConditions()
	{
		Condition[] array = new Condition[8];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		return (IEnumerable<Condition>)(object)array;
	}

	public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Invalid comparison between Unknown and I4
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Invalid comparison between Unknown and I4
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Expected I4, but got Unknown
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Invalid comparison between Unknown and I4
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Invalid comparison between Unknown and I4
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Invalid comparison between Unknown and I4
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Invalid comparison between Unknown and I4
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Invalid comparison between Unknown and I4
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Invalid comparison between Unknown and I4
		if (!(conditionItem.Tag is GannAngle gannAngle))
		{
			return false;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point anchorPoint = Anchor.GetPoint(chartControl, val, chartScale, true);
		double deltaX = gannAngle.RatioX * (double)chartControl.Properties.BarDistance;
		double deltaPrice = chartScale.GetPixelsForDistance(gannAngle.RatioY * chartControl.Instrument.MasterInstrument.TickSize);
		Point gannStepPoint = GetGannStepPoint(chartScale, anchorPoint.X, Anchor.Price, deltaX, deltaPrice);
		Point extendedEndPoint = ((DrawingTool)this).GetExtendedPoint(anchorPoint, gannStepPoint);
		if ((int)values[0].ValueType == 11)
		{
			int xByTime = chartControl.GetXByTime(values[0].Time);
			if (!(gannStepPoint.X >= (double)xByTime))
			{
				return gannStepPoint.X >= (double)xByTime;
			}
			return true;
		}
		double num = chartControl.GetXByTime(values[0].Time);
		double num2 = chartScale.GetYByValue(values[0].Value);
		Point point = new Point(num, num2);
		if (extendedEndPoint.X < num)
		{
			return false;
		}
		if (gannStepPoint.X > num2)
		{
			return false;
		}
		Condition val2 = condition;
		if ((int)val2 <= 1)
		{
			return MathHelper.DidPredicateCross((IList<ChartAlertValue>)values, (Predicate<ChartAlertValue>)Predicate);
		}
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(anchorPoint, extendedEndPoint, point);
		val2 = condition;
		return (val2 - 2) switch
		{
			1 => (int)pointLineLocation == 0, 
			2 => ((int)pointLineLocation == 0 || (int)pointLineLocation == 2) ? true : false, 
			3 => (int)pointLineLocation == 1, 
			4 => pointLineLocation - 1 <= 1, 
			0 => (int)pointLineLocation == 2, 
			5 => (int)pointLineLocation != 2, 
			_ => false, 
		};
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
			double y = chartScale.GetYByValue(v.Value);
			Point point2 = new Point(x, y);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(anchorPoint, extendedEndPoint, point2);
			if ((int)condition == 0)
			{
				return (int)pointLineLocation2 == 0;
			}
			return (int)pointLineLocation2 == 1;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		if (Anchor.Time >= firstTimeOnChart && Anchor.Time <= lastTimeOnChart)
		{
			return true;
		}
		bool flag = Anchor.Time > lastTimeOnChart;
		if (flag)
		{
			GannFanDirection fanDirection = FanDirection;
			bool flag2 = ((fanDirection == GannFanDirection.UpLeft || fanDirection == GannFanDirection.DownLeft) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return true;
		}
		flag = Anchor.Time < firstTimeOnChart;
		if (flag)
		{
			GannFanDirection fanDirection = FanDirection;
			bool flag2 = ((fanDirection == GannFanDirection.UpRight || fanDirection == GannFanDirection.DownRight) ? true : false);
			flag = flag2;
		}
		return flag;
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
				if (((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 10.0, point) == Anchor)
				{
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartControl.ChartPanels[((DrawingTool)this).PanelIndex], chartScale, point) == Cursors.SizeAll)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
				else
				{
					((ChartObject)this).IsSelected = false;
				}
			}
		}
		else
		{
			if (PointsPerBar < 0.0)
			{
				PointsPerBar = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize;
			}
			dataPoint.CopyDataValues(Anchor);
			Anchor.IsEditing = false;
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		if (!((DrawingTool)this).IsLocked || (int)((DrawingTool)this).DrawingState == 0)
		{
			if ((int)((DrawingTool)this).DrawingState == 1)
			{
				dataPoint.CopyDataValues(Anchor);
			}
			else if ((int)((DrawingTool)this).DrawingState == 3)
			{
				Anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		((DrawingTool)this).DrawingState = (DrawingState)2;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		State state = ((NinjaScript)this).State;
		if ((int)state != 1)
		{
			if ((int)state != 2)
			{
				if ((int)state == 8)
				{
					((DrawingTool)this).Dispose();
				}
			}
			else if (base.GannAngles.Count == 0)
			{
				Brush[] array = new Brush[9]
				{
					Brushes.Red,
					Brushes.MediumOrchid,
					Brushes.DarkSlateBlue,
					Brushes.SteelBlue,
					Brushes.Gray,
					Brushes.MediumAquamarine,
					Brushes.Khaki,
					Brushes.Coral,
					Brushes.Red
				};
				for (int i = 0; i < 9; i++)
				{
					int num = ((i == 8) ? 8 : ((i <= 4) ? 1 : (i - 3)));
					int num2 = ((i == 0) ? 8 : ((i > 4) ? 1 : (5 - i)));
					base.GannAngles.Add(new GannAngle(num, num2, array[i % 8]));
				}
			}
		}
		else
		{
			((NinjaScript)this).Description = Resource.NinjaScriptDrawingToolGannFan;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolGannFan;
			Anchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchor,
				IsEditing = true
			};
			FanDirection = GannFanDirection.UpRight;
			PriceLevelOpacity = 5;
			IsTextDisplayed = true;
			PointsPerBar = -1.0;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Expected O, but got Unknown
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cb: Expected O, but got Unknown
		//IL_04cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0505: Unknown result type (might be due to invalid IL or missing references)
		//IL_048e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0525: Unknown result type (might be due to invalid IL or missing references)
		//IL_0540: Unknown result type (might be due to invalid IL or missing references)
		//IL_0651: Unknown result type (might be due to invalid IL or missing references)
		//IL_0656: Unknown result type (might be due to invalid IL or missing references)
		//IL_065b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0663: Unknown result type (might be due to invalid IL or missing references)
		//IL_0677: Unknown result type (might be due to invalid IL or missing references)
		//IL_0696: Unknown result type (might be due to invalid IL or missing references)
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = Anchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = new Point(0.0, 0.0);
		Brush val2 = null;
		foreach (GannAngle item in from ga in base.GannAngles
			where ga.IsVisible && ga.Stroke != null
			orderby ga.RatioX / ga.RatioY
			select ga)
		{
			item.Stroke.RenderTarget = ((ChartObject)this).RenderTarget;
			double deltaX = item.RatioX * (double)chartControl.Properties.BarDistance;
			double deltaPrice = item.RatioY * PointsPerBar;
			Vector gannStepDataVector = GetGannStepDataVector(deltaX, deltaPrice);
			Point point3 = CalculateExtendedDataPoint(val, chartScale, Convert.ToInt32(point.X), Anchor.Price, gannStepDataVector);
			double y = ((MathExtentions.ApproxCompare((double)(item.Stroke.Width % 2f), 0.0) == 0) ? 0.5 : 0.0);
			Vector vector = new Vector(0.0, y);
			Brush val3 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : item.Stroke.BrushDX);
			((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point + vector), DxExtensions.ToVector2(point3 + vector), val3, item.Stroke.Width, item.Stroke.StrokeStyle);
			if (val2 != null)
			{
				float opacity = val2.Opacity;
				val2.Opacity = (float)PriceLevelOpacity / 100f;
				PathGeometry val4 = new PathGeometry(Globals.D2DFactory);
				GeometrySink val5 = val4.Open();
				((SimplifiedGeometrySink)val5).BeginFigure(DxExtensions.ToVector2(point2), (FigureBegin)0);
				if (Math.Abs(point2.Y - point3.Y) > 0.1 && Math.Abs(point2.X - point3.X) > 0.1)
				{
					double y2;
					double x;
					if (point2.Y <= (double)((ChartObject)this).ChartPanel.Y || point2.Y >= (double)(((ChartObject)this).ChartPanel.Y + ((ChartObject)this).ChartPanel.H))
					{
						GannFanDirection fanDirection = FanDirection;
						if ((uint)fanDirection <= 1u)
						{
							y2 = point3.Y;
							x = point2.X;
						}
						else
						{
							y2 = point2.Y;
							x = point3.X;
						}
					}
					else
					{
						GannFanDirection fanDirection = FanDirection;
						if ((uint)fanDirection <= 1u)
						{
							y2 = point2.Y;
							x = point3.X;
						}
						else
						{
							y2 = point3.Y;
							x = point2.X;
						}
					}
					val5.AddLine(new Vector2((float)x, (float)y2));
				}
				val5.AddLine(DxExtensions.ToVector2(point3));
				val5.AddLine(DxExtensions.ToVector2(point + vector));
				val5.AddLine(DxExtensions.ToVector2(point2));
				((SimplifiedGeometrySink)val5).EndFigure((FigureEnd)1);
				((SimplifiedGeometrySink)val5).Close();
				((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)val4, val2);
				((DisposeBase)val4).Dispose();
				val2.Opacity = opacity;
			}
			point2 = point3 + vector;
			val2 = val3;
		}
		if (!IsTextDisplayed || ((ChartObject)this).IsInHitTest)
		{
			return;
		}
		foreach (GannAngle item2 in from ga in base.GannAngles
			where ga.IsVisible && ga.Stroke != null
			orderby ga.RatioX / ga.RatioY
			select ga)
		{
			item2.Stroke.RenderTarget = ((ChartObject)this).RenderTarget;
			double deltaX2 = item2.RatioX * (double)chartControl.Properties.BarDistance;
			double deltaPrice2 = item2.RatioY * PointsPerBar;
			Vector gannStepDataVector2 = GetGannStepDataVector(deltaX2, deltaPrice2);
			Point point4 = CalculateExtendedDataPoint(val, chartScale, Convert.ToInt32(point.X), Anchor.Price, gannStepDataVector2);
			if (!IsTextDisplayed || ((ChartObject)this).IsInHitTest)
			{
				continue;
			}
			TextFormat val6 = ((SimpleFont)(((object)chartControl.Properties.LabelFont) ?? ((object)new SimpleFont()))).ToDirectWriteTextFormat();
			val6.TextAlignment = (TextAlignment)0;
			val6.WordWrapping = (WordWrapping)1;
			TextLayout val7 = new TextLayout(Globals.DirectWriteFactory, item2.Name, val6, 100f, val6.FontSize);
			float height = val7.Metrics.Height;
			Point point5 = new Point(point4.X, point4.Y);
			if (point5.X > (double)((float)(val.X + val.W) - val7.Metrics.Width))
			{
				point5.X = (float)(val.X + val.W) - val7.Metrics.Width;
				point5.Y += val7.Metrics.Width;
			}
			if (gannStepDataVector2.Y > 0.0)
			{
				if (point5.Y < (double)val.Y + (double)height * 0.5)
				{
					point5.Y = (double)val.Y + (double)height * 0.5;
				}
			}
			else if (point5.Y > (double)(val.Y + val.H) - (double)height * 1.5)
			{
				point5.Y = (double)(val.Y + val.H) - (double)height * 1.5;
			}
			float num = 2f + ((Application.Current.FindResource("FontModalTitleMargin") as float?) ?? 3f);
			GannFanDirection fanDirection = FanDirection;
			bool flag = ((fanDirection == GannFanDirection.UpLeft || fanDirection == GannFanDirection.DownLeft) ? true : false);
			float num2 = (flag ? num : (-2f * num));
			Matrix3x2 transform = Matrix3x2.Translation(new Vector2((float)point5.X, (float)point5.Y));
			((ChartObject)this).RenderTarget.Transform = transform;
			((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2(num2 + num, num), val7, item2.Stroke.BrushDX, (DrawTextOptions)1);
			((ChartObject)this).RenderTarget.Transform = Matrix3x2.Identity;
			((DisposeBase)val6).Dispose();
			((DisposeBase)val7).Dispose();
		}
	}
}

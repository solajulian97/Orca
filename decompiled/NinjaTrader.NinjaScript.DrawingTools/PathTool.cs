using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Path IDrawingTool.
/// </summary>
public class PathTool : PathToolSegmentContainer
{
	[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
	public enum PathToolCapMode
	{
		Arrow,
		Line
	}

	private PathGeometry arrowPathGeometry;

	private const double cursorSensitivity = 15.0;

	private DispatcherTimer doubleClickTimer;

	private ChartAnchor editingAnchor;

	[Browsable(false)]
	[SkipOnCopyTo(true)]
	[ExcludeFromTemplate]
	public List<ChartAnchor> ChartAnchors { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 0)]
	public Stroke OutlineStroke { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolPathBegin", GroupName = "NinjaScriptGeneral", Order = 1)]
	public PathToolCapMode PathBegin { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolPathEnd", GroupName = "NinjaScriptGeneral", Order = 2)]
	public PathToolCapMode PathEnd { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolPathShowCount", GroupName = "NinjaScriptGeneral", Order = 3)]
	public bool ShowCount { get; set; }

	[Display(Order = 0)]
	[SkipOnCopyTo(true)]
	[ExcludeFromTemplate]
	public ChartAnchor StartAnchor
	{
		get
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Expected O, but got Unknown
			if (ChartAnchors == null || ChartAnchors.Count == 0)
			{
				return new ChartAnchor
				{
					DisplayName = Resource.NinjaScriptDrawingToolAnchorStart,
					IsEditing = true,
					DrawingTool = (IDrawingTool)(object)this
				};
			}
			return ChartAnchors[0];
		}
		set
		{
			if (ChartAnchors != null)
			{
				if (ChartAnchors.Count == 0)
				{
					ChartAnchors.Add(value);
				}
				else
				{
					ChartAnchors[0] = value;
				}
			}
		}
	}

	public override IEnumerable<ChartAnchor> Anchors
	{
		get
		{
			if (ChartAnchors == null || ChartAnchors.Count == 0)
			{
				return (IEnumerable<ChartAnchor>)(object)new ChartAnchor[1] { StartAnchor };
			}
			return ChartAnchors.ToArray();
		}
	}

	public override object Icon => Icons.DrawPath;

	public override bool SupportsAlerts => true;

	public override void CopyTo(NinjaScript ninjaScript)
	{
		base.CopyTo(ninjaScript);
		if (ninjaScript is PathTool pathTool)
		{
			if (ChartAnchors == null)
			{
				return;
			}
			pathTool.ChartAnchors.Clear();
			{
				foreach (ChartAnchor chartAnchor in ChartAnchors)
				{
					List<ChartAnchor> chartAnchors = pathTool.ChartAnchors;
					object obj = chartAnchor.Clone();
					chartAnchors.Add((ChartAnchor)((obj is ChartAnchor) ? obj : null));
				}
				return;
			}
		}
		PropertyInfo property = ((object)ninjaScript).GetType().GetProperty("ChartAnchors");
		if (property == null || !(property.GetValue(ninjaScript) is IList list))
		{
			return;
		}
		list.Clear();
		foreach (ChartAnchor chartAnchor2 in ChartAnchors)
		{
			try
			{
				object obj2 = chartAnchor2.Clone();
				ChartAnchor val = (ChartAnchor)((obj2 is ChartAnchor) ? obj2 : null);
				if (val != null)
				{
					list.Add(val);
				}
			}
			catch
			{
			}
		}
	}

	private PathGeometry CreatePathGeometry(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, double pixelAdjust)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		List<Vector2> list = new List<Vector2>();
		Vector vector = new Vector(pixelAdjust, pixelAdjust);
		for (int i = 0; i < ChartAnchors.Count; i++)
		{
			Point point = ChartAnchors[i].GetPoint(chartControl, chartPanel, chartScale, true);
			list.Add(DxExtensions.ToVector2(point + vector));
			if (i + 1 < ChartAnchors.Count)
			{
				Point point2 = ChartAnchors[i + 1].GetPoint(chartControl, chartPanel, chartScale, true);
				list.Add(DxExtensions.ToVector2(point2 + vector));
			}
		}
		PathGeometry val = new PathGeometry(Globals.D2DFactory);
		GeometrySink obj = val.Open();
		((SimplifiedGeometrySink)obj).BeginFigure(list[0], (FigureBegin)0);
		((SimplifiedGeometrySink)obj).AddLines(list.ToArray());
		((SimplifiedGeometrySink)obj).EndFigure((FigureEnd)0);
		((SimplifiedGeometrySink)obj).Close();
		return val;
	}

	private void DoubleClickTimerTick(object sender, EventArgs e)
	{
		doubleClickTimer.Stop();
	}

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		if (ChartAnchors == null || ChartAnchors.Count == 0)
		{
			yield break;
		}
		foreach (PathToolSegment pathToolSegment in base.PathToolSegments)
		{
			yield return new AlertConditionItem
			{
				Name = pathToolSegment.Name,
				ShouldOnlyDisplayName = true,
				Tag = pathToolSegment
			};
		}
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
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
		if ((int)((DrawingTool)this).DrawingState == 1 && ((DrawingTool)this).IsLocked)
		{
			return Cursors.No;
		}
		if (((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point) == null)
		{
			Point[] pathAnchorPoints = GetPathAnchorPoints(chartControl, chartScale);
			if (pathAnchorPoints.Length != 0 && (pathAnchorPoints.Last() - point).Length <= 15.0)
			{
				if (!((DrawingTool)this).IsLocked)
				{
					return Cursors.SizeAll;
				}
				return Cursors.Arrow;
			}
			for (int i = 0; i < ChartAnchors.Count; i++)
			{
				Point point2 = ChartAnchors[i].GetPoint(chartControl, chartPanel, chartScale, true);
				if (i + 1 < ChartAnchors.Count)
				{
					Point point3 = ChartAnchors[i + 1].GetPoint(chartControl, chartPanel, chartScale, true);
					if (MathHelper.IsPointAlongVector(point, point2, point3 - point2, 15.0))
					{
						if (!((DrawingTool)this).IsLocked)
						{
							return Cursors.SizeAll;
						}
						return Cursors.Arrow;
					}
					continue;
				}
				Point point4 = ChartAnchors[0].GetPoint(chartControl, chartPanel, chartScale, true);
				if (MathHelper.IsPointAlongVector(point, point4, point2 - point4, 15.0))
				{
					if (!((DrawingTool)this).IsLocked)
					{
						return Cursors.SizeAll;
					}
					return Cursors.Arrow;
				}
			}
			return null;
		}
		if (!((DrawingTool)this).IsLocked)
		{
			return Cursors.SizeNESW;
		}
		return null;
	}

	[DllImport("user32.dll")]
	private static extern uint GetDoubleClickTime();

	private Point[] GetPathAnchorPoints(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point[] array = new Point[ChartAnchors.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ChartAnchors[i].GetPoint(chartControl, val, chartScale, true);
		}
		return array;
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		return GetPathAnchorPoints(chartControl, chartScale);
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
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Invalid comparison between Unknown and I4
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Expected I4, but got Unknown
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Invalid comparison between Unknown and I4
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Invalid comparison between Unknown and I4
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Invalid comparison between Unknown and I4
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Invalid comparison between Unknown and I4
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Invalid comparison between Unknown and I4
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Invalid comparison between Unknown and I4
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Invalid comparison between Unknown and I4
		if (!(conditionItem.Tag is PathToolSegment pathToolSegment))
		{
			return false;
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		Point point = pathToolSegment.StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = pathToolSegment.EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = double.MaxValue;
		double num2 = double.MinValue;
		Point[] array = new Point[2] { point, point2 };
		for (int i = 0; i < array.Length; i++)
		{
			Point point3 = array[i];
			num = Math.Min(num, point3.X);
			num2 = Math.Max(num2, point3.X);
		}
		double num3 = (((int)values[0].ValueType == 12) ? num : ((double)chartControl.GetXByTime(values[0].Time)));
		double y = chartScale.GetYByValue(values[0].Value);
		if (num2 < num3)
		{
			return false;
		}
		if (num > num3)
		{
			return false;
		}
		Point leftPoint = ((point.X < point2.X) ? point : point2);
		Point rightPoint = ((point2.X > point.X) ? point2 : point);
		Point point4 = new Point(num3, y);
		PointLineLocation pointLineLocation = MathHelper.GetPointLineLocation(leftPoint, rightPoint, point4);
		Condition val2 = condition;
		switch ((int)val2)
		{
		case 3:
			return (int)pointLineLocation == 0;
		case 4:
			if ((int)pointLineLocation != 0)
			{
				return (int)pointLineLocation == 2;
			}
			return true;
		case 5:
			return (int)pointLineLocation == 1;
		case 6:
			if ((int)pointLineLocation != 1)
			{
				return (int)pointLineLocation == 2;
			}
			return true;
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
			Point point5 = new Point(x, y2);
			PointLineLocation pointLineLocation2 = MathHelper.GetPointLineLocation(leftPoint, rightPoint, point5);
			if ((int)condition == 0)
			{
				return (int)pointLineLocation2 == 0;
			}
			return (int)pointLineLocation2 == 1;
		}
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		float num = float.MaxValue;
		float num2 = float.MinValue;
		ChartPanel chartPanel = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		foreach (Point item in ChartAnchors.Select((ChartAnchor a) => a.GetPoint(chartControl, chartPanel, chartScale, true)))
		{
			num = (float)Math.Min(num, item.X);
			num2 = (float)Math.Max(num2, item.X);
		}
		DateTime timeByX = chartControl.GetTimeByX((int)num);
		DateTime timeByX2 = chartControl.GetTimeByX((int)num2);
		if (timeByX <= lastTimeOnChart)
		{
			return timeByX2 >= firstTimeOnChart;
		}
		return false;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (!((NinjaScript)this).IsVisible || !ChartAnchors.Any((ChartAnchor a) => !a.IsEditing))
		{
			return;
		}
		foreach (ChartAnchor chartAnchor in ChartAnchors)
		{
			((ChartObject)this).MinValue = Math.Min(((ChartObject)this).MinValue, chartAnchor.Price);
			((ChartObject)this).MaxValue = Math.Max(((ChartObject)this).MaxValue, chartAnchor.Price);
		}
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Expected O, but got Unknown
		Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if ((int)drawingState != 0)
		{
			if ((int)drawingState == 2)
			{
				editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
				if (editingAnchor != null)
				{
					editingAnchor.IsEditing = true;
					((DrawingTool)this).DrawingState = (DrawingState)1;
				}
				else if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) != null)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
			}
			return;
		}
		if (ChartAnchors.Count == 0)
		{
			ChartAnchors.Add(new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchor,
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			});
		}
		foreach (ChartAnchor chartAnchor in ChartAnchors)
		{
			if (chartAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(chartAnchor);
				chartAnchor.IsEditing = false;
			}
		}
		ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
		if (ChartAnchors.Count > 1 && doubleClickTimer.IsEnabled && closestAnchor != null && closestAnchor != ChartAnchors[ChartAnchors.Count - 1])
		{
			ChartAnchors.Remove(ChartAnchors[ChartAnchors.Count - 1]);
			base.PathToolSegments.Remove(base.PathToolSegments[base.PathToolSegments.Count - 1]);
			doubleClickTimer.Stop();
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
			return;
		}
		ChartAnchor val = new ChartAnchor
		{
			DisplayName = Resource.NinjaScriptDrawingToolAnchor,
			IsEditing = true,
			DrawingTool = (IDrawingTool)(object)this
		};
		dataPoint.CopyDataValues(val);
		ChartAnchors.Add(val);
		if (ChartAnchors.Count > 1)
		{
			base.PathToolSegments.Add(new PathToolSegment(ChartAnchors[ChartAnchors.Count - 2], ChartAnchors[ChartAnchors.Count - 1], $"{Resource.NinjaScriptDrawingToolPathSegment} {base.PathToolSegments.Count + 1}"));
			if (!doubleClickTimer.IsEnabled)
			{
				doubleClickTimer.Start();
			}
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected I4, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		switch ((int)drawingState)
		{
		case 0:
		{
			foreach (ChartAnchor chartAnchor in ChartAnchors)
			{
				if (chartAnchor.IsEditing)
				{
					dataPoint.CopyDataValues(chartAnchor);
				}
			}
			break;
		}
		case 1:
			if (editingAnchor != null)
			{
				dataPoint.CopyDataValues(editingAnchor);
			}
			break;
		case 3:
		{
			foreach (ChartAnchor chartAnchor2 in ChartAnchors)
			{
				chartAnchor2.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			break;
		}
		case 2:
			break;
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			if (editingAnchor != null)
			{
				editingAnchor.IsEditing = false;
				editingAnchor = null;
			}
			((DrawingTool)this).DrawingState = (DrawingState)2;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0503: Expected O, but got Unknown
		//IL_0537: Unknown result type (might be due to invalid IL or missing references)
		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
		//IL_057d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Expected O, but got Unknown
		//IL_047d: Unknown result type (might be due to invalid IL or missing references)
		//IL_049e: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c3: Unknown result type (might be due to invalid IL or missing references)
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		Stroke outlineStroke = OutlineStroke;
		outlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		double num = ((outlineStroke.Width % 2f == 0f) ? 0.5 : 0.0);
		Vector vector = new Vector(num, num);
		PathGeometry val2 = CreatePathGeometry(chartControl, val, chartScale, num);
		Brush val3 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
		((ChartObject)this).RenderTarget.DrawGeometry((Geometry)(object)val2, val3, outlineStroke.Width, outlineStroke.StrokeStyle);
		((DisposeBase)val2).Dispose();
		if (PathBegin == PathToolCapMode.Arrow || PathEnd == PathToolCapMode.Arrow)
		{
			Point[] pathAnchorPoints = GetPathAnchorPoints(chartControl, chartScale);
			if (pathAnchorPoints.Length > 1)
			{
				if (arrowPathGeometry == null)
				{
					arrowPathGeometry = new PathGeometry(Globals.D2DFactory);
					GeometrySink obj = arrowPathGeometry.Open();
					float num2 = 6f;
					Vector2 val4 = default(Vector2);
					((Vector2)(ref val4))._002Ector(0f, outlineStroke.Width * 0.5f);
					((SimplifiedGeometrySink)obj).BeginFigure(val4, (FigureBegin)0);
					obj.AddLine(new Vector2(num2, 0f - num2));
					obj.AddLine(new Vector2(0f - num2, 0f - num2));
					obj.AddLine(val4);
					((SimplifiedGeometrySink)obj).EndFigure((FigureEnd)1);
					((SimplifiedGeometrySink)obj).Close();
				}
				if (PathBegin == PathToolCapMode.Arrow)
				{
					Vector vector2 = pathAnchorPoints[0] - pathAnchorPoints[1];
					vector2.Normalize();
					Vector2 val5 = DxExtensions.ToVector2(pathAnchorPoints[0] + vector);
					float num3 = 0f - (float)Math.Atan2(vector2.X, vector2.Y);
					Vector vector3 = vector2 * 5.0;
					Vector2 val6 = default(Vector2);
					((Vector2)(ref val6))._002Ector((float)((double)val5.X + vector3.X), (float)((double)val5.Y + vector3.Y));
					Matrix3x2 transform = Matrix3x2.Rotation(num3, Vector2.Zero) * Matrix3x2.Scaling((float)Math.Max(1.0, (double)outlineStroke.Width * 0.45) + 0.25f) * Matrix3x2.Translation(val6);
					((ChartObject)this).RenderTarget.Transform = transform;
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)arrowPathGeometry, val3);
					((ChartObject)this).RenderTarget.Transform = Matrix3x2.Identity;
				}
				if (PathEnd == PathToolCapMode.Arrow)
				{
					Vector vector4 = pathAnchorPoints[pathAnchorPoints.Length - 1] - pathAnchorPoints[pathAnchorPoints.Length - 2];
					vector4.Normalize();
					Vector2 val7 = DxExtensions.ToVector2(pathAnchorPoints[pathAnchorPoints.Length - 1] + vector);
					float num4 = 0f - (float)Math.Atan2(vector4.X, vector4.Y);
					Vector vector5 = vector4 * 5.0;
					Vector2 val8 = default(Vector2);
					((Vector2)(ref val8))._002Ector((float)((double)val7.X + vector5.X), (float)((double)val7.Y + vector5.Y));
					Matrix3x2 transform2 = Matrix3x2.Rotation(num4, Vector2.Zero) * Matrix3x2.Scaling((float)Math.Max(1.0, (double)outlineStroke.Width * 0.45) + 0.25f) * Matrix3x2.Translation(val8);
					((ChartObject)this).RenderTarget.Transform = transform2;
					((ChartObject)this).RenderTarget.FillGeometry((Geometry)(object)arrowPathGeometry, val3);
					((ChartObject)this).RenderTarget.Transform = Matrix3x2.Identity;
				}
			}
		}
		if (!ShowCount)
		{
			return;
		}
		TextFormat val9 = ((SimpleFont)(((object)chartControl.Properties.LabelFont) ?? ((object)new SimpleFont()))).ToDirectWriteTextFormat();
		val9.TextAlignment = (TextAlignment)0;
		val9.WordWrapping = (WordWrapping)1;
		for (int i = 1; i < ChartAnchors.Count; i++)
		{
			Point point = ChartAnchors[i - 1].GetPoint(chartControl, val, chartScale, true);
			Point point2 = ChartAnchors[i].GetPoint(chartControl, val, chartScale, true);
			if (i + 1 < ChartAnchors.Count)
			{
				Point point3 = ChartAnchors[i + 1].GetPoint(chartControl, val, chartScale, true);
				Vector vector6 = point - point2;
				vector6.Normalize();
				Vector vector7 = point3 - point2;
				vector7.Normalize();
				Vector vector8 = vector6 + vector7;
				vector8.Normalize();
				TextLayout val10 = new TextLayout(Globals.DirectWriteFactory, i.ToString(), val9, 250f, val9.FontSize);
				Point point4 = point2 - vector8 * val9.FontSize;
				point4.X -= val10.Metrics.Width / 2f;
				point4.Y -= val10.Metrics.Height / 2f;
				((ChartObject)this).RenderTarget.DrawTextLayout(DxExtensions.ToVector2(point4 + vector), val10, outlineStroke.BrushDX, (DrawTextOptions)1);
				((DisposeBase)val10).Dispose();
			}
			else
			{
				TextLayout val11 = new TextLayout(Globals.DirectWriteFactory, i.ToString(), val9, 250f, val9.FontSize);
				Vector vector9 = point - point2;
				vector9.Normalize();
				Point point5 = point2 - vector9 * val9.FontSize;
				point5.X -= val11.Metrics.Width / 2f;
				point5.Y -= val11.Metrics.Height / 2f;
				((ChartObject)this).RenderTarget.DrawTextLayout(DxExtensions.ToVector2(point5 + vector), val11, outlineStroke.BrushDX, (DrawTextOptions)1);
				((DisposeBase)val11).Dispose();
			}
		}
		((DisposeBase)val9).Dispose();
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((DrawingTool)this).DrawingState = (DrawingState)0;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolPath;
			OutlineStroke = new Stroke((Brush)Brushes.CornflowerBlue, (DashStyleHelper)0, 2f, 100);
			ChartAnchors = new List<ChartAnchor>();
			PathBegin = PathToolCapMode.Line;
			PathEnd = PathToolCapMode.Line;
			ShowCount = false;
		}
		else if ((int)((NinjaScript)this).State == 3 && doubleClickTimer == null)
		{
			doubleClickTimer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, (int)GetDoubleClickTime()), DispatcherPriority.Background, DoubleClickTimerTick, Dispatcher.CurrentDispatcher);
		}
	}
}

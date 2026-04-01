using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
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
/// Represents an interface that exposes information regarding a Ruler IDrawingTool.
/// </summary>
public class Ruler : DrawingTool
{
	private const int cursorSensitivity = 15;

	private ChartAnchor editingAnchor;

	private bool isTextCreated;

	private const float textMargin = 3f;

	private TextFormat textFormat;

	private TextLayout textLayout;

	private Brush textBrush;

	private readonly DeviceBrush textDeviceBrush = new DeviceBrush();

	private readonly DeviceBrush textBackgroundDeviceBrush = new DeviceBrush();

	private string yValueString;

	private string timeText;

	private ValueUnit yValueDisplayUnit;

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[3] { StartAnchor, EndAnchor, TextAnchor };

	[Display(Order = 1)]
	public ChartAnchor StartAnchor { get; set; }

	[Display(Order = 2)]
	public ChartAnchor EndAnchor { get; set; }

	[Display(Order = 3)]
	public ChartAnchor TextAnchor { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAnchor", GroupName = "NinjaScriptGeneral", Order = 2)]
	public Stroke LineColor { get; set; }

	private bool ShouldDrawText
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			if ((int)((DrawingTool)this).DrawingState != 3)
			{
				ChartAnchor endAnchor = EndAnchor;
				if (endAnchor == null || endAnchor.IsEditing)
				{
					endAnchor = TextAnchor;
					if (endAnchor != null)
					{
						return !endAnchor.IsEditing;
					}
					return false;
				}
			}
			return true;
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolText", GroupName = "NinjaScriptGeneral", Order = 1)]
	public Brush TextColor
	{
		get
		{
			return textBrush;
		}
		set
		{
			textBrush = value;
			textDeviceBrush.Brush = value;
		}
	}

	[Browsable(false)]
	public string TextColorSerialize
	{
		get
		{
			return Serialize.BrushToString(TextColor);
		}
		set
		{
			TextColor = Serialize.StringToBrush(value);
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolRulerYValueDisplayUnit", GroupName = "NinjaScriptGeneral", Order = 3)]
	public ValueUnit YValueDisplayUnit
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return yValueDisplayUnit;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			yValueDisplayUnit = value;
			isTextCreated = false;
		}
	}

	public override object Icon => Icons.DrawRuler;

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		try
		{
			TextLayout obj = textLayout;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			textFormat = null;
			textDeviceBrush.RenderTarget = null;
			textBackgroundDeviceBrush.RenderTarget = null;
		}
		catch
		{
		}
		finally
		{
			LineColor = null;
		}
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
			if (editingAnchor == TextAnchor)
			{
				return Cursors.SizeNESW;
			}
			if (editingAnchor != StartAnchor)
			{
				return Cursors.SizeNWSE;
			}
			return Cursors.SizeNESW;
		default:
		{
			ChartAnchor closestAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
			if (closestAnchor != null)
			{
				if (((DrawingTool)this).IsLocked)
				{
					return Cursors.Arrow;
				}
				if (closestAnchor != StartAnchor)
				{
					return Cursors.SizeNWSE;
				}
				return Cursors.SizeNESW;
			}
			Point point2 = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point3 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point4 = TextAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Vector vector = point3 - point2;
			Vector vector2 = point4 - point3;
			UpdateTextLayout(chartControl, ((ChartObject)this).ChartPanel, chartScale);
			Point point5 = new Point(point4.X - (double)textLayout.MaxWidth - 3.0, point4.Y);
			Point point6 = new Point(point5.X, point4.Y - (double)textLayout.MaxHeight - 6.0);
			Point point7 = new Point(point4.X, point4.Y - (double)textLayout.MaxHeight - 6.0);
			Vector vector3 = point5 - point4;
			Vector vector4 = point6 - point5;
			Vector vector5 = point7 - point6;
			Vector vector6 = point4 - point7;
			if (MathHelper.IsPointAlongVector(point, point2, vector, 15.0) || MathHelper.IsPointAlongVector(point, point3, vector2, 15.0) || MathHelper.IsPointAlongVector(point, point4, vector3, 15.0) || MathHelper.IsPointAlongVector(point, point5, vector4, 15.0) || MathHelper.IsPointAlongVector(point, point6, vector5, 15.0) || MathHelper.IsPointAlongVector(point, point7, vector6, 15.0))
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

	public sealed override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		if (ShouldDrawText)
		{
			Point point3 = TextAnchor.GetPoint(chartControl, val, chartScale, true);
			return new Point[3] { point, point3, point2 };
		}
		return new Point[2] { point, point2 };
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		DateTime dateTime = Globals.MaxDate;
		DateTime dateTime2 = Globals.MinDate;
		foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
		{
			if (anchor.Time < dateTime)
			{
				dateTime = anchor.Time;
			}
			if (anchor.Time > dateTime2)
			{
				dateTime2 = anchor.Time;
			}
		}
		if (!(dateTime <= lastTimeOnChart))
		{
			if (dateTime <= firstTimeOnChart)
			{
				return dateTime2 >= firstTimeOnChart;
			}
			return false;
		}
		return true;
	}

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (((NinjaScript)this).IsVisible)
		{
			((ChartObject)this).MinValue = ((DrawingTool)this).Anchors.Select((ChartAnchor a) => a.Price).Min();
			((ChartObject)this).MaxValue = ((DrawingTool)this).Anchors.Select((ChartAnchor a) => a.Price).Max();
		}
	}

	public override void OnBarsChanged()
	{
		isTextCreated = false;
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
			if ((int)drawingState != 2)
			{
				return;
			}
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			editingAnchor = ((DrawingTool)this).GetClosestAnchor(chartControl, chartPanel, chartScale, 15.0, point);
			if (editingAnchor != null)
			{
				editingAnchor.IsEditing = true;
				((DrawingTool)this).DrawingState = (DrawingState)1;
			}
			else if (editingAnchor == null || ((DrawingTool)this).IsLocked)
			{
				if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) != null)
				{
					((DrawingTool)this).DrawingState = (DrawingState)3;
				}
				else
				{
					((ChartObject)this).IsSelected = false;
				}
			}
			return;
		}
		if (StartAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(StartAnchor);
			dataPoint.CopyDataValues(EndAnchor);
			dataPoint.CopyDataValues(TextAnchor);
			StartAnchor.IsEditing = false;
		}
		else if (EndAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(EndAnchor);
			EndAnchor.IsEditing = false;
			dataPoint.CopyDataValues(TextAnchor);
		}
		else if (TextAnchor.IsEditing)
		{
			dataPoint.CopyDataValues(TextAnchor);
			TextAnchor.IsEditing = false;
		}
		if (!StartAnchor.IsEditing && !EndAnchor.IsEditing && !TextAnchor.IsEditing)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Invalid comparison between Unknown and I4
		if (((DrawingTool)this).IsLocked && (int)((DrawingTool)this).DrawingState != 0)
		{
			return;
		}
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (EndAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(EndAnchor);
				dataPoint.CopyDataValues(TextAnchor);
				isTextCreated = false;
			}
			else if (TextAnchor.IsEditing)
			{
				dataPoint.CopyDataValues(TextAnchor);
			}
		}
		else if ((int)((DrawingTool)this).DrawingState == 1 && editingAnchor != null)
		{
			dataPoint.CopyDataValues(editingAnchor);
			if (editingAnchor == StartAnchor || editingAnchor == EndAnchor)
			{
				isTextCreated = false;
			}
		}
		else
		{
			if ((int)((DrawingTool)this).DrawingState != 3)
			{
				return;
			}
			foreach (ChartAnchor anchor in ((DrawingTool)this).Anchors)
			{
				anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
			}
			TextLayout obj = textLayout;
			if (obj != null)
			{
				((DisposeBase)obj).Dispose();
			}
			textLayout = null;
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState != 0)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
			if (editingAnchor != null)
			{
				editingAnchor.IsEditing = false;
			}
			editingAnchor = null;
		}
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Expected O, but got Unknown
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		LineColor.RenderTarget = ((ChartObject)this).RenderTarget;
		((ChartObject)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = StartAnchor.GetPoint(chartControl, val, chartScale, true);
		Point point2 = EndAnchor.GetPoint(chartControl, val, chartScale, true);
		double num = ((MathExtentions.ApproxCompare(LineColor.Width % 2f, 0f) == 0) ? 0.5 : 0.0);
		Vector vector = new Vector(num, num);
		Vector2 val2 = DxExtensions.ToVector2(point2 + vector);
		Brush val3 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : LineColor.BrushDX);
		((ChartObject)this).RenderTarget.DrawLine(DxExtensions.ToVector2(point + vector), val2, val3, LineColor.Width, LineColor.StrokeStyle);
		if (ShouldDrawText)
		{
			UpdateTextLayout(chartControl, ((ChartObject)this).ChartPanel, chartScale);
			textDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			textBackgroundDeviceBrush.Brush = Application.Current.FindResource("ChartControl.DataBoxBackground") as Brush;
			textBackgroundDeviceBrush.RenderTarget = ((ChartObject)this).RenderTarget;
			object obj = Application.Current.FindResource("BorderThinBrush") as Brush;
			double value = (Application.Current.FindResource("BorderThinThickness") as double?) ?? 1.0;
			if (obj == null)
			{
				obj = LineColor.Brush;
			}
			Stroke val4 = new Stroke((Brush)obj, (DashStyleHelper)0, Convert.ToSingle(value))
			{
				RenderTarget = ((ChartObject)this).RenderTarget
			};
			Point point3 = TextAnchor.GetPoint(chartControl, val, chartScale, true);
			Vector2 val5 = DxExtensions.ToVector2(point3 + vector);
			((ChartObject)this).RenderTarget.DrawLine(val2, val5, LineColor.BrushDX, LineColor.Width, LineColor.StrokeStyle);
			float num2 = (float)(num / 2.0);
			RectangleF val6 = default(RectangleF);
			((RectangleF)(ref val6))._002Ector((float)(point3.X - (double)textLayout.MaxWidth - 3.0 + (double)num2), (float)(point3.Y - (double)textLayout.MaxHeight - 3.0 + (double)num2), textLayout.MaxWidth + 6f, textLayout.MaxHeight + 3f);
			if (textBackgroundDeviceBrush.BrushDX != null && !((ChartObject)this).IsInHitTest)
			{
				((ChartObject)this).RenderTarget.FillRectangle(val6, textBackgroundDeviceBrush.BrushDX);
			}
			((ChartObject)this).RenderTarget.DrawRectangle(val6, val4.BrushDX, val4.Width, val4.StrokeStyle);
			if (textDeviceBrush.BrushDX != null && !((ChartObject)this).IsInHitTest)
			{
				((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2((float)((double)(((RectangleF)(ref val6)).X + 3f) + num), (float)((double)(((RectangleF)(ref val6)).Y + 3f) + num)), textLayout, textDeviceBrush.BrushDX);
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Invalid comparison between Unknown and I4
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolRuler;
			((DrawingTool)this).DrawingState = (DrawingState)0;
			StartAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			EndAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			TextAnchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this
			};
			StartAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorStart;
			EndAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorEnd;
			TextAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchorText;
			LineColor = new Stroke((Brush)Brushes.DarkGray, (DashStyleHelper)0, 1f, 50);
			TextColor = (Application.Current.FindResource("ChartControl.DataBoxForeground") as Brush) ?? Brushes.CornflowerBlue;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}

	private void UpdateTextLayout(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected I4, but got Unknown
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Invalid comparison between Unknown and I4
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Invalid comparison between Unknown and I4
		//IL_048f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Expected O, but got Unknown
		//IL_04a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
		TextLayout val;
		if (isTextCreated)
		{
			val = textLayout;
			if (val != null && !((DisposeBase)val).IsDisposed)
			{
				return;
			}
		}
		TextFormat val2 = textFormat;
		if (val2 != null && !((DisposeBase)val2).IsDisposed)
		{
			((DisposeBase)textFormat).Dispose();
		}
		val = textLayout;
		if (val != null && !((DisposeBase)val).IsDisposed)
		{
			((DisposeBase)textLayout).Dispose();
		}
		ChartBars attachedToChartBars = ((DrawingTool)this).GetAttachedToChartBars();
		if (attachedToChartBars != null)
		{
			double num = ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(EndAnchor.Price - StartAnchor.Price);
			double num2 = num / ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize;
			ValueUnit val3 = YValueDisplayUnit;
			switch ((int)val3)
			{
			case 0:
				yValueString = attachedToChartBars.Bars.Instrument.MasterInstrument.FormatPrice(num, true);
				break;
			case 3:
				yValueString = (((int)((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.InstrumentType == 4) ? Globals.FormatCurrency((double)((int)num2 * Account.All[0].ForexLotSize) * (((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize * ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.PointValue)) : Globals.FormatCurrency((double)(int)num2 * (((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.TickSize * ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.PointValue)));
				break;
			case 1:
				yValueString = (num / ((DrawingTool)this).AttachedTo.Instrument.MasterInstrument.RoundToTickSize(StartAnchor.Price)).ToString("P", Globals.GeneralOptions.CurrentCulture);
				break;
			case 2:
				yValueString = num2.ToString("F0");
				break;
			case 4:
			{
				double num3 = Math.Abs(num2 / 10.0);
				char c = char.Parse(Globals.GeneralOptions.CurrentCulture.NumberFormat.NumberDecimalSeparator);
				yValueString = ((int.Parse(num3.ToString("F1").Split(c)[1]) > 0) ? num3.ToString("F1").Replace(c, '\'') : num3.ToString("F0"));
				break;
			}
			}
			TimeSpan timeSpan = EndAnchor.Time - StartAnchor.Time;
			timeSpan = new TimeSpan(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
			bool flag = Math.Abs(timeSpan.TotalHours) >= 24.0;
			if ((int)attachedToChartBars.Bars.BarsPeriod.BarsPeriodType == 5)
			{
				int num4 = Math.Abs(timeSpan.Days);
				timeText = ((num4 > 1) ? $"{Math.Abs(timeSpan.Days)} {Resource.Days}" : $"{Math.Abs(timeSpan.Days)} {Resource.Day}");
			}
			else
			{
				timeText = (flag ? $"{string.Format(Resource.NinjaScriptDrawingToolRulerDaysFormat, Math.Abs(timeSpan.Days))}\n{timeSpan.Subtract(new TimeSpan(timeSpan.Days, 0, 0, 0)).Duration(),25}" : timeSpan.Duration().ToString());
			}
			Point point = StartAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			Point point2 = EndAnchor.GetPoint(chartControl, chartPanel, chartScale, true);
			int barIdxByX = attachedToChartBars.GetBarIdxByX(chartControl, (int)point.X);
			int num5 = attachedToChartBars.GetBarIdxByX(chartControl, (int)point2.X) - barIdxByX;
			SimpleFont val4 = (SimpleFont)(((object)chartControl.Properties.LabelFont) ?? ((object)new SimpleFont()));
			textFormat = val4.ToDirectWriteTextFormat();
			textFormat.TextAlignment = (TextAlignment)0;
			textFormat.WordWrapping = (WordWrapping)1;
			string text = $"{((DrawingTool)this).AttachedTo.DisplayName}\n{Resource.NinjaScriptDrawingToolRulerNumberBarsText,-11}{num5,-11}\n{Resource.NinjaScriptDrawingToolRulerTimeText,-11}{timeText,-11}\n{Resource.NinjaScriptDrawingToolRulerYValueText,-10}{yValueString,-10}";
			textLayout = new TextLayout(Globals.DirectWriteFactory, text, textFormat, 600f, 600f);
			textLayout.MaxWidth = textLayout.Metrics.Width;
			textLayout.MaxHeight = textLayout.Metrics.Height;
			isTextCreated = true;
		}
	}
}

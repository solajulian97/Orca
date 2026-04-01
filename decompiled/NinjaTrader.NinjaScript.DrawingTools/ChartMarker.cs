using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;

namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class ChartMarker : DrawingTool
{
	private Brush areaBrush;

	[CLSCompliant(false)]
	protected readonly DeviceBrush AreaDeviceBrush = new DeviceBrush();

	private Brush outlineBrush;

	[CLSCompliant(false)]
	protected readonly DeviceBrush OutlineDeviceBrush = new DeviceBrush();

	public ChartAnchor Anchor { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
	[XmlIgnore]
	public Brush AreaBrush
	{
		get
		{
			return areaBrush;
		}
		set
		{
			areaBrush = value;
			AreaDeviceBrush.Brush = value;
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesSize", GroupName = "GuiGeneral", Order = 2)]
	public ChartMarkerSize Size { get; set; } = ChartMarkerSize.Medium;

	[Browsable(false)]
	public string AreaBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(AreaBrush);
		}
		set
		{
			AreaBrush = Serialize.StringToBrush(value);
		}
	}

	protected double BarWidth
	{
		get
		{
			if (((DrawingTool)this).AttachedTo != null)
			{
				IChartObject chartObject = ((DrawingTool)this).AttachedTo.ChartObject;
				ChartBars val = (ChartBars)(object)((chartObject is ChartBars) ? chartObject : null);
				if (val == null)
				{
					IChartObject chartObject2 = ((DrawingTool)this).AttachedTo.ChartObject;
					IChartBars val2 = (IChartBars)(object)((chartObject2 is IChartBars) ? chartObject2 : null);
					if (val2 != null)
					{
						val = val2.ChartBars;
					}
				}
				if (((val != null) ? val.Properties.ChartStyle : null) != null)
				{
					return val.Properties.ChartStyle.BarWidth;
				}
			}
			return MinimumSize;
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesOutlineBrush", GroupName = "NinjaScriptGeneral", Order = 3)]
	[XmlIgnore]
	public Brush OutlineBrush
	{
		get
		{
			return outlineBrush;
		}
		set
		{
			outlineBrush = value;
			OutlineDeviceBrush.Brush = value;
		}
	}

	[Browsable(false)]
	public string OutlineBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(OutlineBrush);
		}
		set
		{
			OutlineBrush = Serialize.StringToBrush(value);
		}
	}

	protected static float MinimumSize => 5f;

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[1] { Anchor };

	public override void OnCalculateMinMax()
	{
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (((NinjaScript)this).IsVisible)
		{
			((ChartObject)this).MinValue = Anchor.Price;
			((ChartObject)this).MaxValue = Anchor.Price;
		}
	}

	protected float GetSizeMultiplier()
	{
		return Size switch
		{
			ChartMarkerSize.ExtraLarge => 2f, 
			ChartMarkerSize.Large => 1.5f, 
			ChartMarkerSize.Small => 0.75f, 
			ChartMarkerSize.ExtraSmall => 0.5f, 
			_ => 1f, 
		};
	}

	protected override void Dispose(bool disposing)
	{
		AreaDeviceBrush.RenderTarget = null;
		OutlineDeviceBrush.RenderTarget = null;
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
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
		if (!((point - point2).Length <= GetSelectionSensitivity()))
		{
			return null;
		}
		if (!((DrawingTool)this).IsLocked)
		{
			return Cursors.SizeAll;
		}
		return Cursors.Arrow;
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		if (Anchor.IsEditing)
		{
			return Array.Empty<Point>();
		}
		ChartPanel val = chartControl.ChartPanels[chartScale.PanelIndex];
		Point point = Anchor.GetPoint(chartControl, val, chartScale, true);
		return new Point[1] { point };
	}

	private double GetSelectionSensitivity()
	{
		return Math.Max(15.0, 10.0 * (BarWidth / 5.0));
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return false;
		}
		if (!((ChartObject)this).IsAutoScale && (Anchor.Price < chartScale.MinValue || Anchor.Price > chartScale.MaxValue))
		{
			return false;
		}
		if (Anchor.Time >= firstTimeOnChart)
		{
			return Anchor.Time <= lastTimeOnChart;
		}
		return false;
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
				if (((DrawingTool)this).GetCursor(chartControl, chartPanel, chartScale, point) != null)
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
			dataPoint.CopyDataValues(Anchor);
			Anchor.IsEditing = false;
			((DrawingTool)this).DrawingState = (DrawingState)2;
			((ChartObject)this).IsSelected = false;
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 3 && (!((DrawingTool)this).IsLocked || (int)((DrawingTool)this).DrawingState == 0))
		{
			dataPoint.CopyDataValues(Anchor);
		}
	}

	public override void OnMouseUp(ChartControl control, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		DrawingState drawingState = ((DrawingTool)this).DrawingState;
		if (((int)drawingState == 1 || (int)drawingState == 3) ? true : false)
		{
			((DrawingTool)this).DrawingState = (DrawingState)2;
		}
	}
}

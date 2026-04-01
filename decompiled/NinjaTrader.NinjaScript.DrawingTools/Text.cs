using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
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
/// Represents an interface that exposes information regarding a Text IDrawingTool.
/// </summary>
public class Text : DrawingTool
{
	private Brush areaBrush;

	private DeviceBrush areaBrushDevice = new DeviceBrush();

	private int areaOpacity;

	private TextAlignment alignment;

	[CLSCompliant(false)]
	protected TextLayout cachedTextLayout;

	private SimpleFont font;

	private Rect layoutRect;

	private bool needsLayoutUpdate;

	private readonly float outlinePadding = GetPadding();

	private Brush textBrush;

	private DeviceBrush textBrushDevice = new DeviceBrush();

	private string text;

	public override object Icon => Icons.DrawText;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextAlignment", GroupName = "NinjaScriptGeneral", Order = 7)]
	public TextAlignment Alignment
	{
		get
		{
			return alignment;
		}
		set
		{
			if (alignment != value)
			{
				alignment = value;
				needsLayoutUpdate = true;
			}
		}
	}

	[XmlIgnore]
	[Browsable(false)]
	public bool UseChartTextBrush { get; set; }

	[Browsable(false)]
	public bool UseChartTextBrushSerialize
	{
		get
		{
			if (UseChartTextBrush)
			{
				if (LastBrush != null && TextBrush != null)
				{
					return LastBrush.ToString() == TextBrush.ToString();
				}
				return true;
			}
			return false;
		}
		set
		{
			UseChartTextBrush = value;
		}
	}

	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool ManuallyDrawn { get; set; }

	[XmlIgnore]
	[Browsable(false)]
	public Brush LastBrush { get; set; }

	public ChartAnchor Anchor { get; set; }

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[1] { Anchor };

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
	public Brush AreaBrush
	{
		get
		{
			return areaBrush;
		}
		set
		{
			areaBrush = value;
			Brush brush = areaBrush;
			if (brush != null && brush.CanFreeze)
			{
				areaBrush.Freeze();
			}
		}
	}

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

	/// <summary>
	/// Opacity in percent value (0 to 100)
	/// </summary>
	[Range(0, 100)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 2)]
	public int AreaOpacity
	{
		get
		{
			return areaOpacity;
		}
		set
		{
			areaOpacity = Math.Max(0, Math.Min(100, value));
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextFont", GroupName = "NinjaScriptGeneral", Order = 4)]
	public SimpleFont Font
	{
		get
		{
			return font;
		}
		set
		{
			font = value;
			needsLayoutUpdate = true;
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 3)]
	public Stroke OutlineStroke { get; set; }

	[ExcludeFromTemplate]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolText", GroupName = "NinjaScriptGeneral", Order = 5)]
	[PropertyEditor("NinjaTrader.Gui.Tools.MultilineEditor")]
	public string DisplayText
	{
		get
		{
			return text;
		}
		set
		{
			if (!(text == value))
			{
				text = value;
				needsLayoutUpdate = true;
			}
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
	public Brush TextBrush
	{
		get
		{
			return textBrush;
		}
		set
		{
			textBrush = value;
			Brush brush = textBrush;
			if (brush != null && brush.CanFreeze)
			{
				textBrush.Freeze();
			}
		}
	}

	[Browsable(false)]
	public string TextBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(TextBrush);
		}
		set
		{
			TextBrush = Serialize.StringToBrush(value);
		}
	}

	/// <summary>
	///  set this to offset the text up/down by a certain number of pixels
	/// </summary>
	[Browsable(false)]
	public int YPixelOffset { get; set; }

	protected override void Dispose(bool disposing)
	{
		((DrawingTool)this).Dispose(disposing);
		TextLayout obj = cachedTextLayout;
		if (obj != null)
		{
			((DisposeBase)obj).Dispose();
		}
		if (textBrushDevice != null)
		{
			textBrushDevice.RenderTarget = null;
		}
		if (areaBrushDevice != null)
		{
			areaBrushDevice.RenderTarget = null;
		}
		cachedTextLayout = null;
		textBrushDevice = null;
		areaBrushDevice = null;
	}

	private void DrawText(ChartControl chartControl)
	{
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		if (Font == null || string.IsNullOrEmpty(DisplayText))
		{
			return;
		}
		Rect currentRect = GetCurrentRect(layoutRect, outlinePadding);
		RectangleF val = default(RectangleF);
		((RectangleF)(ref val))._002Ector((float)currentRect.X, (float)currentRect.Y, (float)currentRect.Width, (float)currentRect.Height);
		Stroke outlineStroke = OutlineStroke;
		textBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		areaBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		outlineStroke.RenderTarget = ((ChartObject)this).RenderTarget;
		Brush val2;
		if (AreaBrush != null)
		{
			if (!(AreaBrush is SolidColorBrush solidColorBrush) || !(areaBrushDevice.Brush is SolidColorBrush solidColorBrush2) || solidColorBrush2.Color != solidColorBrush.Color || Math.Abs(solidColorBrush2.Opacity - (double)areaOpacity / 100.0) > 0.1)
			{
				Brush brush = AreaBrush.Clone();
				brush.Opacity = (double)areaOpacity / 100.0;
				areaBrushDevice.Brush = brush;
			}
			areaBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
			val2 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : areaBrushDevice.BrushDX);
			((ChartObject)this).RenderTarget.FillRectangle(val, val2);
		}
		else
		{
			areaBrushDevice.RenderTarget = null;
		}
		if (outlineStroke.StrokeStyle != null && (outlineStroke.Brush != null || !BrushExtensions.IsTransparent(outlineStroke.Brush)))
		{
			val2 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX);
			if (val2 != null)
			{
				((ChartObject)this).RenderTarget.DrawRectangle(val, val2, outlineStroke.Width, outlineStroke.StrokeStyle);
			}
		}
		textBrushDevice.RenderTarget = ((ChartObject)this).RenderTarget;
		if (!(TextBrush is SolidColorBrush solidColorBrush3) || !(textBrushDevice.Brush is SolidColorBrush solidColorBrush4) || solidColorBrush4.Color != solidColorBrush3.Color || Math.Abs(solidColorBrush4.Opacity - solidColorBrush3.Opacity) > 0.1)
		{
			textBrushDevice.Brush = TextBrush;
		}
		val2 = (((ChartObject)this).IsInHitTest ? chartControl.SelectionBrush : textBrushDevice.BrushDX);
		((ChartObject)this).RenderTarget.DrawTextLayout(new Vector2(((RectangleF)(ref val)).X + outlinePadding, ((RectangleF)(ref val)).Y + outlinePadding), cachedTextLayout, val2, (DrawTextOptions)1);
	}

	public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			if (chartControl.GetTextEntryBox().Visibility == Visibility.Visible)
			{
				return null;
			}
			return Cursors.IBeam;
		}
		if ((int)((DrawingTool)this).DrawingState == 3)
		{
			if (!((DrawingTool)this).IsLocked)
			{
				return Cursors.SizeAll;
			}
			return Cursors.No;
		}
		if (!GetCurrentRect(layoutRect, outlinePadding).IntersectsWith(new Rect(point.X, point.Y, 4.0, 4.0)))
		{
			return null;
		}
		if (!((DrawingTool)this).IsLocked)
		{
			return Cursors.SizeAll;
		}
		return Cursors.Arrow;
	}

	protected virtual Rect GetCurrentRect(Rect pLayoutRect, double pOutlinePadding)
	{
		if (ManuallyDrawn)
		{
			return new Rect(pLayoutRect.X - pOutlinePadding, pLayoutRect.Y - pOutlinePadding, pLayoutRect.Width + pOutlinePadding * 2.0, pLayoutRect.Height + pOutlinePadding * 2.0);
		}
		return new Rect(pLayoutRect.X - pOutlinePadding, pLayoutRect.Y - pLayoutRect.Height / 2.0 - pOutlinePadding, pLayoutRect.Width + pOutlinePadding * 2.0, pLayoutRect.Height + pOutlinePadding * 2.0);
	}

	private static float GetPadding()
	{
		return (Application.Current.FindResource("FontModalTitleMargin") as float?) ?? 3f;
	}

	protected virtual Point GetTextDrawingPosition(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
	{
		Point point = Anchor.GetPoint(chartControl, chartPanel, chartScale, true);
		if (cachedTextLayout == null)
		{
			return point;
		}
		return Alignment switch
		{
			TextAlignment.Center => new Point(point.X - (double)(cachedTextLayout.MaxWidth / 2f), point.Y), 
			TextAlignment.Right => new Point(point.X - (double)cachedTextLayout.MaxWidth, point.Y), 
			TextAlignment.Left => new Point(point.X + (double)outlinePadding, point.Y), 
			_ => point, 
		};
	}

	public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0 || layoutRect == default(Rect) || chartControl.GetTextEntryBox().Visibility == Visibility.Visible)
		{
			return Array.Empty<Point>();
		}
		Rect currentRect = GetCurrentRect(layoutRect, outlinePadding);
		return new Point[4] { currentRect.TopLeft, currentRect.TopRight, currentRect.BottomLeft, currentRect.BottomRight };
	}

	public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return true;
		}
		float num = (float)chartControl.GetXByTime(Anchor.Time) + ((cachedTextLayout == null) ? 0f : cachedTextLayout.Metrics.Width);
		DateTime timeByX = chartControl.GetTimeByX((int)num);
		if (Anchor.Time > lastTimeOnChart || timeByX < firstTimeOnChart)
		{
			return false;
		}
		if (((ChartObject)this).IsAutoScale)
		{
			return true;
		}
		if (needsLayoutUpdate || cachedTextLayout == null)
		{
			return true;
		}
		float num2 = chartScale.GetYByValue(Anchor.Price);
		float height = cachedTextLayout.Metrics.Height;
		if (!(chartScale.GetValueByY(num2 + height) > chartScale.MaxValue))
		{
			return !(Anchor.Price < chartScale.MinValue);
		}
		return false;
	}

	public override void OnCalculateMinMax()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		((ChartObject)this).MinValue = double.MaxValue;
		((ChartObject)this).MaxValue = double.MinValue;
		if (((NinjaScript)this).IsVisible && (int)((DrawingTool)this).DrawingState != 0)
		{
			((ChartObject)this).MinValue = Anchor.Price;
			((ChartObject)this).MaxValue = Anchor.Price;
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Invalid comparison between Unknown and I4
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolText;
			Alignment = TextAlignment.Left;
			Anchor = new ChartAnchor
			{
				IsEditing = true,
				DrawingTool = (IDrawingTool)(object)this,
				DisplayName = Resource.NinjaScriptDrawingToolAnchor
			};
			Font = new SimpleFont
			{
				Size = 14.0
			};
			OutlineStroke = new Stroke((Brush)Brushes.Transparent, 2f);
			TextBrush = textBrush;
			AreaBrush = Brushes.Transparent;
			AreaOpacity = 100;
			YPixelOffset = 0;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			TextBrush = null;
			textBrush = null;
			((DrawingTool)this).Dispose();
		}
	}

	public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		TextBox tb;
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			dataPoint.CopyDataValues(Anchor);
			Anchor.IsEditing = false;
			Point mouseDownPoint = chartControl.MouseDownPoint;
			DisplayText = string.Empty;
			tb = chartControl.GetTextEntryBox();
			tb.Text = string.Empty;
			tb.AcceptsReturn = true;
			tb.AcceptsTab = true;
			tb.Background = new SolidColorBrush(Color.FromArgb(4, 0, 0, 0));
			tb.BorderBrush = chartControl.Properties.AxisPen.Brush;
			tb.FontFamily = Font.Family;
			tb.FontSize = Font.Size;
			tb.FontStyle = (Font.Italic ? FontStyles.Italic : FontStyles.Normal);
			tb.FontWeight = (Font.Bold ? FontWeights.Bold : FontWeights.Normal);
			tb.Foreground = TextBrush ?? chartControl.Properties.ChartText;
			tb.Style = Application.Current.FindResource("TextBoxNoEffects") as Style;
			tb.Margin = new Thickness(mouseDownPoint.X, mouseDownPoint.Y, 0.0, 0.0);
			if (TextBrush == null)
			{
				UseChartTextBrush = true;
			}
			tb.PreviewKeyDown += OnTbOnPreviewKeyDown;
			((UIElement)(object)chartControl).PreviewMouseDown += OnTbPreviewMouseDown;
			tb.IsVisibleChanged += OnTbOnIsVisibleChanged;
			ManuallyDrawn = true;
			tb.Visibility = Visibility.Visible;
			tb.Focus();
		}
		else
		{
			Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale, true);
			if (GetCurrentRect(layoutRect, outlinePadding).IntersectsWith(new Rect(point.X, point.Y, 2.0, 2.0)))
			{
				Anchor.IsEditing = true;
				((DrawingTool)this).DrawingState = (DrawingState)3;
			}
			else
			{
				((ChartObject)this).IsSelected = false;
			}
		}
		void OnTbOnIsVisibleChanged(object _, DependencyPropertyChangedEventArgs __)
		{
			if (tb.Visibility != Visibility.Visible)
			{
				tb.PreviewKeyDown -= OnTbOnPreviewKeyDown;
				tb.PreviewMouseDown -= OnTbPreviewMouseDown;
				tb.IsVisibleChanged -= OnTbOnIsVisibleChanged;
				DisplayText = tb.Text;
				((DrawingTool)this).DrawingState = (DrawingState)2;
				((ChartObject)this).IsSelected = false;
				chartControl.InvalidateVisual();
				if (chartControl.IsStayInDrawMode)
				{
					chartControl.TryStartDrawing(((object)this).GetType().FullName);
				}
				if (((DrawingTool)this).IsGlobalDrawingTool)
				{
					GlobalDrawingToolManager.RaiseGlobalDrawingObjectChanged(chartControl, (Operation)1, (DrawingTool)(object)this);
				}
			}
		}
		void OnTbOnPreviewKeyDown(object _, KeyEventArgs args)
		{
			Key key = args.Key;
			if ((key == Key.Tab || key == Key.Return) ? true : false)
			{
				tb.Visibility = Visibility.Collapsed;
				args.Handled = true;
			}
			else if (args.Key == Key.System && args.SystemKey == Key.Return)
			{
				int caretIndex = tb.CaretIndex;
				string text = tb.Text.Substring(0, caretIndex);
				string text2 = tb.Text.Substring(caretIndex);
				tb.Text = text + Environment.NewLine + text2;
				tb.CaretIndex = caretIndex + Environment.NewLine.Length;
				args.Handled = true;
			}
		}
		void OnTbPreviewMouseDown(object _, MouseButtonEventArgs __)
		{
			if (!tb.IsMouseDirectlyOver)
			{
				tb.Visibility = Visibility.Collapsed;
			}
		}
	}

	public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		bool flag = !((DrawingTool)this).IsLocked;
		if (flag)
		{
			DrawingState drawingState = ((DrawingTool)this).DrawingState;
			bool flag2 = (((int)drawingState == 1 || (int)drawingState == 3) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			Anchor.MoveAnchor(((DrawingTool)this).InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, (DrawingTool)(object)this);
		}
	}

	public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
	{
		((DrawingTool)this).DrawingState = (DrawingState)2;
	}

	public override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((DrawingTool)this).DrawingState == 0)
		{
			return;
		}
		if (UseChartTextBrush)
		{
			if (LastBrush != TextBrush && LastBrush != chartControl.Properties.ChartText && LastBrush != null)
			{
				LastBrush = TextBrush;
				UseChartTextBrush = false;
			}
			else
			{
				TextBrush = chartControl.Properties.ChartText;
				LastBrush = TextBrush;
			}
		}
		ChartPanel val = chartControl.ChartPanels[((DrawingTool)this).PanelIndex];
		UpdateTextLayout(val.W);
		Point textDrawingPosition = GetTextDrawingPosition(chartControl, val, chartScale);
		float num = (float)textDrawingPosition.X;
		float num2 = (float)textDrawingPosition.Y;
		num2 -= (float)YPixelOffset;
		layoutRect = new Rect(num, num2, cachedTextLayout.MaxWidth, cachedTextLayout.MaxHeight);
		DrawText(chartControl);
	}

	private void UpdateTextLayout(float maxWidth)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		if (needsLayoutUpdate)
		{
			needsLayoutUpdate = false;
			cachedTextLayout = null;
			if (Font != null)
			{
				TextFormat val = Font.ToDirectWriteTextFormat();
				cachedTextLayout = new TextLayout(Globals.DirectWriteFactory, DisplayText ?? string.Empty, val, maxWidth, val.FontSize);
				cachedTextLayout.MaxWidth = cachedTextLayout.Metrics.Width;
				cachedTextLayout.MaxHeight = cachedTextLayout.Metrics.Height;
				((TextFormat)cachedTextLayout).TextAlignment = (TextAlignment)((Alignment == TextAlignment.Center) ? 2 : ((Alignment == TextAlignment.Right) ? 1 : 0));
				needsLayoutUpdate = false;
				((DisposeBase)val).Dispose();
			}
		}
	}
}

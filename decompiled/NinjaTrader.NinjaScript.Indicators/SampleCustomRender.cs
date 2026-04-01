using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators;

public class SampleCustomRender : Indicator
{
	private Brush areaBrush;

	private int areaOpacity;

	private SMA mySma;

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral")]
	public Brush AreaBrush
	{
		get
		{
			return areaBrush;
		}
		set
		{
			areaBrush = value;
			if (areaBrush != null)
			{
				if (areaBrush.IsFrozen)
				{
					areaBrush = areaBrush.Clone();
				}
				areaBrush.Opacity = (double)areaOpacity / 100.0;
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

	[Range(0, 100)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral")]
	public int AreaOpacity
	{
		get
		{
			return areaOpacity;
		}
		set
		{
			areaOpacity = Math.Max(0, Math.Min(100, value));
			if (areaBrush != null)
			{
				Brush brush = areaBrush.Clone();
				brush.Opacity = (double)areaOpacity / 100.0;
				brush.Freeze();
				areaBrush = brush;
			}
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "SmallAreaColor", GroupName = "NinjaScriptGeneral")]
	public Brush SmallAreaBrush { get; set; }

	[Browsable(false)]
	public string SmallAreaBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(SmallAreaBrush);
		}
		set
		{
			SmallAreaBrush = Serialize.StringToBrush(value);
		}
	}

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> TestPlot => ((NinjaScriptBase)this).Values[0];

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "TextColor", GroupName = "NinjaScriptGeneral")]
	public Brush TextBrush { get; set; }

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

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Invalid comparison between Unknown and I4
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionSampleCustomRender;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameSampleCustomRender;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsChartOnly = true;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			AreaBrush = Brushes.DodgerBlue;
			TextBrush = Brushes.DodgerBlue;
			SmallAreaBrush = Brushes.Crimson;
			AreaOpacity = 20;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Crimson, Resource.NinjaScriptIndicatorNameSampleCustomRender);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			mySma = SMA(20);
		}
		else if ((int)((NinjaScript)this).State == 5)
		{
			((IndicatorRenderBase)this).SetZOrder(-1);
		}
	}

	protected override void OnBarUpdate()
	{
		((NinjaScriptBase)this).Value[0] = ((NinjaScriptBase)mySma)[0];
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Expected O, but got Unknown
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Expected O, but got Unknown
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Expected O, but got Unknown
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Expected O, but got Unknown
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Expected O, but got Unknown
		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Expected O, but got Unknown
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = default(Vector2);
		((Vector2)(ref val))._002Ector((float)((IndicatorRenderBase)this).ChartPanel.X, (float)((IndicatorRenderBase)this).ChartPanel.Y);
		Vector2 val2 = default(Vector2);
		((Vector2)(ref val2))._002Ector((float)(((IndicatorRenderBase)this).ChartPanel.X + ((IndicatorRenderBase)this).ChartPanel.W), (float)(((IndicatorRenderBase)this).ChartPanel.Y + ((IndicatorRenderBase)this).ChartPanel.H));
		Vector2 val3 = DxExtensions.ToVector2(new Point(((IndicatorRenderBase)this).ChartPanel.X, ((IndicatorRenderBase)this).ChartPanel.Y + ((IndicatorRenderBase)this).ChartPanel.H));
		Vector2 val4 = DxExtensions.ToVector2(new Point(((IndicatorRenderBase)this).ChartPanel.X + ((IndicatorRenderBase)this).ChartPanel.W, ((IndicatorRenderBase)this).ChartPanel.Y));
		float num = val2.X - val.X;
		float num2 = val2.Y - val.Y;
		Vector2 val5 = (val + val2) / 2f;
		if (!((IndicatorRenderBase)this).IsInHitTest)
		{
			Brush val6 = DxExtensions.ToDxBrush(areaBrush, ((IndicatorRenderBase)this).RenderTarget);
			Brush val7 = DxExtensions.ToDxBrush(SmallAreaBrush, ((IndicatorRenderBase)this).RenderTarget);
			Brush val8 = DxExtensions.ToDxBrush(TextBrush, ((IndicatorRenderBase)this).RenderTarget);
			SolidColorBrush val9 = new SolidColorBrush(((IndicatorRenderBase)this).RenderTarget, Color.op_Implicit(Color.DodgerBlue));
			AntialiasMode antialiasMode = ((IndicatorRenderBase)this).RenderTarget.AntialiasMode;
			((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)1;
			((IndicatorRenderBase)this).RenderTarget.DrawLine(val, val2, val6, 4f);
			((IndicatorRenderBase)this).RenderTarget.AntialiasMode = (AntialiasMode)0;
			((IndicatorRenderBase)this).RenderTarget.DrawLine(val3, val4, val6, 4f);
			RectangleF val10 = default(RectangleF);
			((RectangleF)(ref val10))._002Ector(val.X, val.Y, num, num2);
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(val10, val6);
			((IndicatorRenderBase)this).RenderTarget.DrawRectangle(val10, (Brush)(object)val9, 2f);
			int num3 = ChartingExtensions.ConvertToVerticalPixels(100.0, ((IndicatorRenderBase)this).ChartControl.PresentationSource);
			int num4 = ChartingExtensions.ConvertToHorizontalPixels(100.0, ((IndicatorRenderBase)this).ChartControl.PresentationSource);
			Ellipse val11 = default(Ellipse);
			((Ellipse)(ref val11))._002Ector(val5, (float)num4, (float)num3);
			GradientStop[] array = (GradientStop[])(object)new GradientStop[2];
			array[0].Color = Color.op_Implicit(Color.Goldenrod);
			array[0].Position = 0f;
			array[1].Color = Color.op_Implicit(Color.SeaGreen);
			array[1].Position = 1f;
			GradientStopCollection val12 = new GradientStopCollection(((IndicatorRenderBase)this).RenderTarget, array);
			RadialGradientBrushProperties val13 = new RadialGradientBrushProperties
			{
				GradientOriginOffset = new Vector2(0f, 0f),
				Center = val11.Point,
				RadiusX = val11.RadiusY,
				RadiusY = val11.RadiusY
			};
			RadialGradientBrush val14 = new RadialGradientBrush(((IndicatorRenderBase)this).RenderTarget, val13, val12);
			((IndicatorRenderBase)this).RenderTarget.FillEllipse(val11, (Brush)(object)val14);
			TextFormat val15 = ((SimpleFont)(((object)chartControl.Properties.LabelFont) ?? ((object)new SimpleFont("Arial", 12)))).ToDirectWriteTextFormat();
			Vector2 val16 = default(Vector2);
			((Vector2)(ref val16))._002Ector((float)(((IndicatorRenderBase)this).ChartPanel.X + 10), (float)(((IndicatorRenderBase)this).ChartPanel.Y + 20));
			TextLayout val17 = new TextLayout(Globals.DirectWriteFactory, Resource.SampleCustomPlotUpperLeftCorner, val15, (float)(((IndicatorRenderBase)this).ChartPanel.X + ((IndicatorRenderBase)this).ChartPanel.W), val15.FontSize);
			((IndicatorRenderBase)this).RenderTarget.DrawTextLayout(val16, val17, val8, (DrawTextOptions)1);
			TextFormat val18 = new TextFormat(Globals.DirectWriteFactory, "Century Gothic", (FontWeight)700, (FontStyle)2, 32f);
			TextLayout val19 = new TextLayout(Globals.DirectWriteFactory, Resource.SampleCustomPlotLowerRightCorner, val18, 400f, val15.FontSize);
			Vector2 val20 = default(Vector2);
			((Vector2)(ref val20))._002Ector((float)((IndicatorRenderBase)this).ChartPanel.W - val19.Metrics.Width - 5f, (float)((IndicatorRenderBase)this).ChartPanel.Y + ((float)((IndicatorRenderBase)this).ChartPanel.H - val19.Metrics.Height));
			RectangleF val21 = default(RectangleF);
			((RectangleF)(ref val21))._002Ector(val20.X, val20.Y, val19.Metrics.Width, val19.Metrics.Height);
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(val21, val7);
			((IndicatorRenderBase)this).RenderTarget.DrawRectangle(val21, val7, 2f);
			((IndicatorRenderBase)this).RenderTarget.DrawTextLayout(val20, val19, val8, (DrawTextOptions)1);
			((IndicatorRenderBase)this).RenderTarget.AntialiasMode = antialiasMode;
			((DisposeBase)val6).Dispose();
			((DisposeBase)val9).Dispose();
			((DisposeBase)val12).Dispose();
			((DisposeBase)val14).Dispose();
			((DisposeBase)val7).Dispose();
			((DisposeBase)val8).Dispose();
			((DisposeBase)val15).Dispose();
			((DisposeBase)val18).Dispose();
			((DisposeBase)val17).Dispose();
			((DisposeBase)val19).Dispose();
		}
	}
}

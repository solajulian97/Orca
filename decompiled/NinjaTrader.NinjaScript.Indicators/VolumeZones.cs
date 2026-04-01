using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.Indicators;

public class VolumeZones : Indicator
{
	internal struct VolumeInfo
	{
		public double up;

		public double down;

		public double total;
	}

	private VolumeInfo[] volumeInfo = new VolumeInfo[20];

	private int barCount;

	private int barSpacing;

	[Range(2, 20)]
	[Display(ResourceType = typeof(Resource), Name = "BarCount", Order = 1, GroupName = "NinjaScriptParameters")]
	public int BarCount
	{
		get
		{
			return barCount;
		}
		set
		{
			barCount = value;
			if (value > volumeInfo.Length)
			{
				volumeInfo = new VolumeInfo[value];
			}
		}
	}

	[Range(0, 5)]
	[Display(ResourceType = typeof(Resource), Name = "BarSpacing", Order = 2, GroupName = "NinjaScriptParameters")]
	public int BarSpacing
	{
		get
		{
			return barSpacing;
		}
		set
		{
			barSpacing = Math.Max(0, value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "DownBarColor", Order = 3, GroupName = "NinjaScriptParameters")]
	public Brush BarDownBrush { get; set; }

	[Browsable(false)]
	public string BarColorDownSerialize
	{
		get
		{
			return Serialize.BrushToString(BarDownBrush);
		}
		set
		{
			BarDownBrush = Serialize.StringToBrush(value);
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "DrawLines", Order = 4, GroupName = "NinjaScriptParameters")]
	public bool DrawLines { get; set; }

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "LineColor", Order = 5, GroupName = "NinjaScriptParameters")]
	public Brush LineBrush { get; set; }

	[Browsable(false)]
	public string LineBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(LineBrush);
		}
		set
		{
			LineBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "UpBarColor", Order = 6, GroupName = "NinjaScriptParameters")]
	public Brush BarUpBrush { get; set; }

	[Browsable(false)]
	public string BarColorUpSerialize
	{
		get
		{
			return Serialize.BrushToString(BarUpBrush);
		}
		set
		{
			BarUpBrush = Serialize.StringToBrush(value);
		}
	}

	[Range(10, 100)]
	[Display(ResourceType = typeof(Resource), Name = "Opacity", Order = 7, GroupName = "NinjaScriptParameters")]
	public double Opacity { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionVolumeZones;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameVolumesZones;
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((IndicatorBase)this).IsChartOnly = true;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).PaintPriceMarkers = false;
			DrawLines = false;
			Opacity = 50.0;
			BarCount = 10;
			BarSpacing = 1;
			BarDownBrush = Brushes.Crimson;
			BarUpBrush = Brushes.DarkCyan;
			LineBrush = Brushes.DarkGray;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((IndicatorRenderBase)this).ZOrder = -1;
		}
	}

	protected override void OnBarUpdate()
	{
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_046b: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_050c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Expected I4, but got Unknown
		if (((IndicatorRenderBase)this).IsInHitTest)
		{
			return;
		}
		int toIndex = ((IndicatorRenderBase)this).ChartBars.ToIndex;
		int fromIndex = ((IndicatorRenderBase)this).ChartBars.FromIndex;
		double num = 0.0;
		double num2 = double.MaxValue;
		Brush val = DxExtensions.ToDxBrush(BarDownBrush, ((IndicatorRenderBase)this).RenderTarget);
		Brush val2 = DxExtensions.ToDxBrush(LineBrush, ((IndicatorRenderBase)this).RenderTarget);
		Brush val3 = DxExtensions.ToDxBrush(BarUpBrush, ((IndicatorRenderBase)this).RenderTarget);
		val.Opacity = (float)(Opacity / 100.0);
		val3.Opacity = (float)(Opacity / 100.0);
		for (int i = fromIndex; i <= toIndex && i >= 0; i++)
		{
			num = Math.Max(num, ((NinjaScriptBase)this).Bars.GetHigh(i));
			num2 = Math.Min(num2, ((NinjaScriptBase)this).Bars.GetLow(i));
		}
		int num3 = BarCount;
		double num4 = (num - num2) / (double)num3;
		double num5 = 0.0;
		for (int j = 0; j < num3; j++)
		{
			double num6 = num2 + num4 * (double)(j + 1);
			double num7 = num2 + num4 * (double)j;
			double num8 = 0.0;
			double num9 = 0.0;
			for (int k = fromIndex; k <= toIndex; k++)
			{
				ISeries<double> obj = ((NinjaScriptBase)this).Inputs[0];
				ISeries<double> obj2 = ((obj is PriceSeries) ? obj : null);
				double num10 = ((obj2 != null) ? new PriceType?(((PriceSeries)obj2).PriceType) : ((PriceType?)null)) switch
				{
					(PriceType)4L => ((NinjaScriptBase)this).Bars.GetOpen(k), 
					(PriceType)0L => ((NinjaScriptBase)this).Bars.GetClose(k), 
					(PriceType)1L => ((NinjaScriptBase)this).Bars.GetHigh(k), 
					(PriceType)2L => ((NinjaScriptBase)this).Bars.GetLow(k), 
					(PriceType)3L => (((NinjaScriptBase)this).Bars.GetHigh(k) + ((NinjaScriptBase)this).Bars.GetLow(k)) / 2.0, 
					(PriceType)5L => (((NinjaScriptBase)this).Bars.GetHigh(k) + ((NinjaScriptBase)this).Bars.GetLow(k) + ((NinjaScriptBase)this).Bars.GetClose(k)) / 3.0, 
					(PriceType)6L => (((NinjaScriptBase)this).Bars.GetHigh(k) + ((NinjaScriptBase)this).Bars.GetLow(k) + 2.0 * ((NinjaScriptBase)this).Bars.GetClose(k)) / 4.0, 
					_ => ((NinjaScriptBase)this).Bars.GetClose(k), 
				};
				if (num10 >= num7 && num10 < num6)
				{
					if (((NinjaScriptBase)this).Bars.GetOpen(k) < ((NinjaScriptBase)this).Bars.GetClose(k))
					{
						num8 += (double)((NinjaScriptBase)this).Bars.GetVolume(k);
					}
					else
					{
						num9 += (double)((NinjaScriptBase)this).Bars.GetVolume(k);
					}
				}
			}
			volumeInfo[j].up = num8;
			volumeInfo[j].down = num9;
			volumeInfo[j].total = num8 + num9;
			num5 = Math.Max(num5, volumeInfo[j].total);
		}
		RectangleF val4 = default(RectangleF);
		RectangleF val5 = default(RectangleF);
		for (int l = 0; l < Math.Min(num3, toIndex - fromIndex + 1); l++)
		{
			double num11 = num2 + num4 * (double)(l + 1);
			double num12 = num2 + num4 * (double)l;
			int num13 = Convert.ToInt32(chartScale.GetYByValue(num11)) + BarSpacing;
			int num14 = Convert.ToInt32(chartScale.GetYByValue(num12));
			int num15 = (int)(chartScale.Height / 2.0 * (volumeInfo[l].up / num5));
			int num16 = (int)(chartScale.Height / 2.0 * (volumeInfo[l].down / num5));
			((RectangleF)(ref val4))._002Ector((float)((IndicatorRenderBase)this).ChartPanel.X, (float)num13, (float)num15, (float)Math.Abs(num13 - num14));
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(val4, val3);
			((IndicatorRenderBase)this).RenderTarget.DrawRectangle(val4, val3);
			((RectangleF)(ref val5))._002Ector((float)(((IndicatorRenderBase)this).ChartPanel.X + num15), (float)num13, (float)num16, (float)Math.Abs(num13 - num14));
			((IndicatorRenderBase)this).RenderTarget.DrawRectangle(val5, val);
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(val5, val);
			if (DrawLines)
			{
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2((float)((IndicatorRenderBase)this).ChartPanel.X, (float)num14), new Vector2((float)(((IndicatorRenderBase)this).ChartPanel.X + ((IndicatorRenderBase)this).ChartPanel.W), (float)num14), val2);
				if (l == num3 - 1)
				{
					((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2((float)((IndicatorRenderBase)this).ChartPanel.X, (float)num13), new Vector2((float)(((IndicatorRenderBase)this).ChartPanel.X + ((IndicatorRenderBase)this).ChartPanel.W), (float)num13), val2);
				}
			}
		}
		((DisposeBase)val2).Dispose();
		((DisposeBase)val).Dispose();
		((DisposeBase)val3).Dispose();
	}
}

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.DirectWrite;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Commitment of traders indicator
/// </summary>
[TypeConverter("NinjaTrader.NinjaScript.Indicators.COTTypeConverter")]
public class COT : Indicator
{
	private bool backCalculated;

	private CotReport[] reports;

	private TextFormat textFormat;

	private SimpleFont font;

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Cot1 => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Cot2 => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Cot3 => ((NinjaScriptBase)this).Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Cot4 => ((NinjaScriptBase)this).Values[3];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Cot5 => ((NinjaScriptBase)this).Values[4];

	[Range(1, 5)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "NumberOfCotPlots", GroupName = "NinjaScriptParameters", Order = 0)]
	[TypeConverter(typeof(RangeEnumConverter))]
	[RefreshProperties(RefreshProperties.All)]
	public int Number { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "COT1", GroupName = "NinjaScriptParameters", Order = 1)]
	[XmlIgnore]
	public CotReport CotReport1 { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "COT2", GroupName = "NinjaScriptParameters", Order = 2)]
	[XmlIgnore]
	public CotReport CotReport2 { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "COT3", GroupName = "NinjaScriptParameters", Order = 3)]
	[XmlIgnore]
	public CotReport CotReport3 { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "COT4", GroupName = "NinjaScriptParameters", Order = 4)]
	[XmlIgnore]
	public CotReport CotReport4 { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "COT5", GroupName = "NinjaScriptParameters", Order = 5)]
	[XmlIgnore]
	public CotReport CotReport5 { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "LegendLocation", GroupName = "NinjaScriptParameters", Order = 6)]
	public LegendLocation LegendLocation { get; set; }

	[Browsable(false)]
	public int Cot1Serialize
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected I4, but got Unknown
			return CotReport1.ReportType * 100 + CotReport1.Field;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected O, but got Unknown
			CotReport1 = new CotReport
			{
				ReportType = (CotReportType)(value / 100),
				Field = (CotReportField)(value % 100)
			};
		}
	}

	[Browsable(false)]
	public int Cot2Serialize
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected I4, but got Unknown
			return CotReport2.ReportType * 100 + CotReport2.Field;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected O, but got Unknown
			CotReport2 = new CotReport
			{
				ReportType = (CotReportType)(value / 100),
				Field = (CotReportField)(value % 100)
			};
		}
	}

	[Browsable(false)]
	public int Cot3Serialize
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected I4, but got Unknown
			return CotReport3.ReportType * 100 + CotReport3.Field;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected O, but got Unknown
			CotReport3 = new CotReport
			{
				ReportType = (CotReportType)(value / 100),
				Field = (CotReportField)(value % 100)
			};
		}
	}

	[Browsable(false)]
	public int Cot4Serialize
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected I4, but got Unknown
			return CotReport4.ReportType * 100 + CotReport4.Field;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected O, but got Unknown
			CotReport4 = new CotReport
			{
				ReportType = (CotReportType)(value / 100),
				Field = (CotReportField)(value % 100)
			};
		}
	}

	[Browsable(false)]
	public int Cot5Serialize
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected I4, but got Unknown
			return CotReport5.ReportType * 100 + CotReport5.Field;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected O, but got Unknown
			CotReport5 = new CotReport
			{
				ReportType = (CotReportType)(value / 100),
				Field = (CotReportField)(value % 100)
			};
		}
	}

	private Vector2 GetPosition(TextLayout textLayout, int pos)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		return (Vector2)(LegendLocation switch
		{
			LegendLocation.TopLeft => new Vector2(8f, (float)(((IndicatorRenderBase)this).ChartPanel.Y + 12) + (float)(pos + 1) * textLayout.Metrics.Height), 
			LegendLocation.TopRight => new Vector2((float)(((IndicatorRenderBase)this).ChartPanel.W - 8) - textLayout.Metrics.Width, (float)(((IndicatorRenderBase)this).ChartPanel.Y + 12) + (float)(pos + 1) * textLayout.Metrics.Height), 
			LegendLocation.BottomLeft => new Vector2(8f, (float)((IndicatorRenderBase)this).ChartPanel.Y + (float)((IndicatorRenderBase)this).ChartPanel.H - (float)(Number + 1 - pos) * textLayout.Metrics.Height), 
			LegendLocation.BottomRight => new Vector2((float)(((IndicatorRenderBase)this).ChartPanel.W - 8) - textLayout.Metrics.Width, (float)((IndicatorRenderBase)this).ChartPanel.Y + (float)((IndicatorRenderBase)this).ChartPanel.H - (float)(Number + 1 - pos) * textLayout.Metrics.Height), 
			_ => new Vector2(-1f, -1f), 
		});
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionCOT;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameCOT;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Number = 3;
			LegendLocation = LegendLocation.TopLeft;
			CotReport1 = new CotReport
			{
				ReportType = (CotReportType)4,
				Field = (CotReportField)55
			};
			CotReport2 = new CotReport
			{
				ReportType = (CotReportType)4,
				Field = (CotReportField)58
			};
			CotReport3 = new CotReport
			{
				ReportType = (CotReportType)4,
				Field = (CotReportField)42
			};
			CotReport4 = new CotReport
			{
				ReportType = (CotReportType)4,
				Field = (CotReportField)0
			};
			CotReport5 = new CotReport
			{
				ReportType = (CotReportType)4,
				Field = (CotReportField)61
			};
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.CornflowerBlue, Resource.COT1);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Red, Resource.COT2);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.LimeGreen, Resource.COT3);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.Goldenrod, Resource.COT4);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.BlueViolet, Resource.COT5);
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			reports = (CotReport[])(object)new CotReport[5] { CotReport1, CotReport2, CotReport3, CotReport4, CotReport5 };
			((NinjaScriptBase)this).BarsRequiredToPlot = 0;
		}
	}

	protected override void OnBarUpdate()
	{
		if (CotData.GetCotReportNames(((NinjaScriptBase)this).Instrument.MasterInstrument.Name).Count == 0)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "Error", Resource.CotDataError, TextPosition.BottomRight);
			return;
		}
		if (!Globals.MarketDataOptions.DownloadCotData)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "Warning", Resource.CotDataWarning, TextPosition.BottomRight);
		}
		if (CotData.IsDownloadingData)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "Warning", Resource.CotDataStillDownloading, TextPosition.BottomRight);
			return;
		}
		for (int i = 0; i < Number; i++)
		{
			if (!backCalculated && ((NinjaScriptBase)this).CurrentBar > 0)
			{
				for (int num = ((NinjaScriptBase)this).CurrentBar - 1; num >= 0; num--)
				{
					double num2 = reports[i].Calculate(((NinjaScriptBase)this).Instrument.MasterInstrument.Name, ((NinjaScriptBase)this).Time[num]);
					if (!double.IsNaN(num2))
					{
						((NinjaScriptBase)this).Values[i][num] = num2;
					}
				}
			}
			double num3 = reports[i].Calculate(((NinjaScriptBase)this).Instrument.MasterInstrument.Name, ((NinjaScriptBase)this).Time[0]);
			if (!double.IsNaN(num3))
			{
				((NinjaScriptBase)this).Values[i][0] = num3;
			}
		}
		backCalculated = true;
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		((IndicatorRenderBase)this).OnRender(chartControl, chartScale);
		if (!backCalculated || LegendLocation == LegendLocation.Disabled)
		{
			return;
		}
		if (font == null || !((object)font).Equals((object)chartControl.Properties.LabelFont) || textFormat == null || ((DisposeBase)textFormat).IsDisposed)
		{
			TextFormat val = textFormat;
			if (val != null && !((DisposeBase)val).IsDisposed)
			{
				((DisposeBase)textFormat).Dispose();
			}
			font = chartControl.Properties.LabelFont;
			textFormat = font.ToDirectWriteTextFormat();
		}
		for (int i = 0; i < Number; i++)
		{
			TextLayout val2 = new TextLayout(Globals.DirectWriteFactory, Globals.ToLocalizedObject((object)reports[i].Field, Globals.GeneralOptions.CurrentUICulture), textFormat, (float)((IndicatorRenderBase)this).ChartPanel.W, textFormat.FontSize);
			((IndicatorRenderBase)this).RenderTarget.DrawTextLayout(GetPosition(val2, i), val2, ((Stroke)((NinjaScriptBase)this).Plots[i]).BrushDX);
			((DisposeBase)val2).Dispose();
		}
	}
}

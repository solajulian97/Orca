using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

public class OrcaAnchoredVWAPs : Indicator
{
	private VwapTracker devTracker;

	private VwapTracker stdTracker;

	private VwapTracker htfTracker;

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Developing Reversal Ticks", Description = "Tick threshold for the Developing VWAP pivot", Order = 1, GroupName = "Parameters")]
	public int DevelopingTicks { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "Standard Reversal Ticks", Description = "Tick threshold for the Standard VWAP pivot", Order = 2, GroupName = "Parameters")]
	public int StandardTicks { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "HTF Reversal Ticks", Description = "Tick threshold for the HTF VWAP pivot", Order = 3, GroupName = "Parameters")]
	public int HtfTicks { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Use ATR Reversal", Description = "Toggle ATR-based Reversal", Order = 4, GroupName = "Parameters")]
	public bool UseAtrReversal { get; set; }

	[NinjaScriptProperty]
	[Range(1, int.MaxValue)]
	[Display(Name = "ATR Period", Description = "Lookback period for ATR", Order = 5, GroupName = "Parameters")]
	public int AtrPeriod { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, double.MaxValue)]
	[Display(Name = "Dev ATR Multiplier", Description = "ATR Multiplier for Developing VWAP", Order = 6, GroupName = "Parameters")]
	public double DevAtrMultiplier { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, double.MaxValue)]
	[Display(Name = "Std ATR Multiplier", Description = "ATR Multiplier for Standard VWAP", Order = 7, GroupName = "Parameters")]
	public double StdAtrMultiplier { get; set; }

	[NinjaScriptProperty]
	[Range(0.1, double.MaxValue)]
	[Display(Name = "HTF ATR Multiplier", Description = "ATR Multiplier for HTF VWAP", Order = 8, GroupName = "Parameters")]
	public double HtfAtrMultiplier { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Standard VWAP Bands", Order = 1, GroupName = "Standard Deviation")]
	public bool ShowStdBands { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show HTF VWAP Bands", Order = 2, GroupName = "Standard Deviation")]
	public bool ShowHtfBands { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show All Bands (Override Filter)", Order = 3, GroupName = "Standard Deviation")]
	public bool ShowAllBands { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Std Dev 1", Order = 4, GroupName = "Standard Deviation")]
	public bool ShowStdDev1 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Std Dev 2", Order = 5, GroupName = "Standard Deviation")]
	public bool ShowStdDev2 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Std Dev 3", Order = 6, GroupName = "Standard Deviation")]
	public bool ShowStdDev3 { get; set; }

	[NinjaScriptProperty]
	[Range(0.01, double.MaxValue)]
	[Display(Name = "Std Dev Multiplier 1", Order = 7, GroupName = "Standard Deviation")]
	public double StdDevMultiplier1 { get; set; }

	[NinjaScriptProperty]
	[Range(0.01, double.MaxValue)]
	[Display(Name = "Std Dev Multiplier 2", Order = 8, GroupName = "Standard Deviation")]
	public double StdDevMultiplier2 { get; set; }

	[NinjaScriptProperty]
	[Range(0.01, double.MaxValue)]
	[Display(Name = "Std Dev Multiplier 3", Order = 9, GroupName = "Standard Deviation")]
	public double StdDevMultiplier3 { get; set; }

	[XmlIgnore]
	[Display(Name = "Fill Color Core-1 (Std)", Order = 7, GroupName = "Standard Deviation Regions")]
	public Brush FillColorStdCore1 { get; set; }

	[Browsable(false)]
	public string FillColorStdCore1Serializable
	{
		get
		{
			return Serialize.BrushToString(FillColorStdCore1);
		}
		set
		{
			FillColorStdCore1 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Fill Opacity Core-1 (Std)", Order = 8, GroupName = "Standard Deviation Regions")]
	public int FillOpacityStdCore1 { get; set; }

	[XmlIgnore]
	[Display(Name = "Fill Color 1-2 (Std)", Order = 9, GroupName = "Standard Deviation Regions")]
	public Brush FillColorStd12 { get; set; }

	[Browsable(false)]
	public string FillColorStd12Serializable
	{
		get
		{
			return Serialize.BrushToString(FillColorStd12);
		}
		set
		{
			FillColorStd12 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Fill Opacity 1-2 (Std)", Order = 10, GroupName = "Standard Deviation Regions")]
	public int FillOpacityStd12 { get; set; }

	[XmlIgnore]
	[Display(Name = "Fill Color 2-3 (Std)", Order = 11, GroupName = "Standard Deviation Regions")]
	public Brush FillColorStd23 { get; set; }

	[Browsable(false)]
	public string FillColorStd23Serializable
	{
		get
		{
			return Serialize.BrushToString(FillColorStd23);
		}
		set
		{
			FillColorStd23 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Fill Opacity 2-3 (Std)", Order = 12, GroupName = "Standard Deviation Regions")]
	public int FillOpacityStd23 { get; set; }

	[XmlIgnore]
	[Display(Name = "Fill Color Core-1 (HTF)", Order = 13, GroupName = "Standard Deviation Regions")]
	public Brush FillColorHtfCore1 { get; set; }

	[Browsable(false)]
	public string FillColorHtfCore1Serializable
	{
		get
		{
			return Serialize.BrushToString(FillColorHtfCore1);
		}
		set
		{
			FillColorHtfCore1 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Fill Opacity Core-1 (HTF)", Order = 14, GroupName = "Standard Deviation Regions")]
	public int FillOpacityHtfCore1 { get; set; }

	[XmlIgnore]
	[Display(Name = "Fill Color 1-2 (HTF)", Order = 15, GroupName = "Standard Deviation Regions")]
	public Brush FillColorHtf12 { get; set; }

	[Browsable(false)]
	public string FillColorHtf12Serializable
	{
		get
		{
			return Serialize.BrushToString(FillColorHtf12);
		}
		set
		{
			FillColorHtf12 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Fill Opacity 1-2 (HTF)", Order = 16, GroupName = "Standard Deviation Regions")]
	public int FillOpacityHtf12 { get; set; }

	[XmlIgnore]
	[Display(Name = "Fill Color 2-3 (HTF)", Order = 17, GroupName = "Standard Deviation Regions")]
	public Brush FillColorHtf23 { get; set; }

	[Browsable(false)]
	public string FillColorHtf23Serializable
	{
		get
		{
			return Serialize.BrushToString(FillColorHtf23);
		}
		set
		{
			FillColorHtf23 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Fill Opacity 2-3 (HTF)", Order = 18, GroupName = "Standard Deviation Regions")]
	public int FillOpacityHtf23 { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> DevVWAP => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> StdVWAP => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> HtfVWAP => ((NinjaScriptBase)this).Values[2];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Invalid comparison between Unknown and I4
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Expected O, but got Unknown
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Expected O, but got Unknown
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Expected O, but got Unknown
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Expected O, but got Unknown
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Expected O, but got Unknown
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Expected O, but got Unknown
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Expected O, but got Unknown
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Expected O, but got Unknown
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Expected O, but got Unknown
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Expected O, but got Unknown
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Expected O, but got Unknown
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Expected O, but got Unknown
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Expected O, but got Unknown
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Expected O, but got Unknown
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "Anchored VWAP indicator with 3 reversal thresholds, standard deviation bands, and region fills.";
			((NinjaScriptBase)this).Name = "Orca Anchored VWAPs";
			((NinjaScriptBase)this).Calculate = (Calculate)0;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			DevelopingTicks = 120;
			StandardTicks = 250;
			HtfTicks = 500;
			UseAtrReversal = false;
			AtrPeriod = 14;
			DevAtrMultiplier = 1.0;
			StdAtrMultiplier = 2.0;
			HtfAtrMultiplier = 3.0;
			ShowStdBands = true;
			ShowHtfBands = true;
			ShowAllBands = false;
			ShowStdDev1 = true;
			ShowStdDev2 = true;
			ShowStdDev3 = true;
			StdDevMultiplier1 = 1.0;
			StdDevMultiplier2 = 2.0;
			StdDevMultiplier3 = 3.0;
			FillColorStdCore1 = Brushes.LightBlue;
			FillOpacityStdCore1 = 20;
			FillColorStd12 = Brushes.DodgerBlue;
			FillOpacityStd12 = 15;
			FillColorStd23 = Brushes.RoyalBlue;
			FillOpacityStd23 = 10;
			FillColorHtfCore1 = Brushes.PaleTurquoise;
			FillOpacityHtfCore1 = 20;
			FillColorHtf12 = Brushes.DarkCyan;
			FillOpacityHtf12 = 15;
			FillColorHtf23 = Brushes.Teal;
			FillOpacityHtf23 = 10;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Gray, 2f), (PlotStyle)6, "Dev VWAP");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Magenta, 2f), (PlotStyle)6, "Std VWAP");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Cyan, 2f), (PlotStyle)6, "HTF VWAP");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Magenta, (DashStyleHelper)1, 1f), (PlotStyle)6, "Std VWAP Upper 1");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Magenta, (DashStyleHelper)1, 1f), (PlotStyle)6, "Std VWAP Upper 2");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Magenta, (DashStyleHelper)1, 1f), (PlotStyle)6, "Std VWAP Upper 3");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Magenta, (DashStyleHelper)1, 1f), (PlotStyle)6, "Std VWAP Lower 1");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Magenta, (DashStyleHelper)1, 1f), (PlotStyle)6, "Std VWAP Lower 2");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Magenta, (DashStyleHelper)1, 1f), (PlotStyle)6, "Std VWAP Lower 3");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Cyan, (DashStyleHelper)1, 1f), (PlotStyle)6, "HTF VWAP Upper 1");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Cyan, (DashStyleHelper)1, 1f), (PlotStyle)6, "HTF VWAP Upper 2");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Cyan, (DashStyleHelper)1, 1f), (PlotStyle)6, "HTF VWAP Upper 3");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Cyan, (DashStyleHelper)1, 1f), (PlotStyle)6, "HTF VWAP Lower 1");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Cyan, (DashStyleHelper)1, 1f), (PlotStyle)6, "HTF VWAP Lower 2");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Cyan, (DashStyleHelper)1, 1f), (PlotStyle)6, "HTF VWAP Lower 3");
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			devTracker = new VwapTracker(((NinjaScriptBase)this).TickSize, DevelopingTicks);
			stdTracker = new VwapTracker(((NinjaScriptBase)this).TickSize, StandardTicks);
			htfTracker = new VwapTracker(((NinjaScriptBase)this).TickSize, HtfTicks);
		}
	}

	protected override void OnBarUpdate()
	{
		if (((NinjaScriptBase)this).CurrentBar >= 1)
		{
			if (UseAtrReversal)
			{
				double num = ((NinjaScriptBase)ATR(AtrPeriod))[0];
				devTracker.ReversalTicks = Math.Max(1.0, num * DevAtrMultiplier / ((NinjaScriptBase)this).TickSize);
				stdTracker.ReversalTicks = Math.Max(1.0, num * StdAtrMultiplier / ((NinjaScriptBase)this).TickSize);
				htfTracker.ReversalTicks = Math.Max(1.0, num * HtfAtrMultiplier / ((NinjaScriptBase)this).TickSize);
			}
			else
			{
				devTracker.ReversalTicks = DevelopingTicks;
				stdTracker.ReversalTicks = StandardTicks;
				htfTracker.ReversalTicks = HtfTicks;
			}
			devTracker.Process(((NinjaScriptBase)this).High[0], ((NinjaScriptBase)this).Low[0], ((NinjaScriptBase)this).Close[0], ((NinjaScriptBase)this).Volume[0], ((NinjaScriptBase)this).CurrentBar);
			stdTracker.Process(((NinjaScriptBase)this).High[0], ((NinjaScriptBase)this).Low[0], ((NinjaScriptBase)this).Close[0], ((NinjaScriptBase)this).Volume[0], ((NinjaScriptBase)this).CurrentBar);
			htfTracker.Process(((NinjaScriptBase)this).High[0], ((NinjaScriptBase)this).Low[0], ((NinjaScriptBase)this).Close[0], ((NinjaScriptBase)this).Volume[0], ((NinjaScriptBase)this).CurrentBar);
			bool visible = devTracker.IsActive && devTracker.ActiveAnchorBar > stdTracker.ActiveAnchorBar;
			bool flag = stdTracker.IsActive && (!htfTracker.IsActive || stdTracker.ActiveAnchorBar > htfTracker.ActiveAnchorBar);
			bool isActive = htfTracker.IsActive;
			DrawVwap(0, -1, -1, -1, -1, -1, -1, devTracker, visible, showBands: false);
			DrawVwap(1, 3, 4, 5, 6, 7, 8, stdTracker, flag, ShowStdBands);
			DrawVwap(2, 9, 10, 11, 12, 13, 14, htfTracker, isActive, ShowHtfBands);
			DrawRegions(flag, isActive);
		}
	}

	private void DrawRegions(bool drawStd, bool drawHtf)
	{
		if (ShowStdBands && drawStd && stdTracker.ActiveAnchorBar >= 0 && stdTracker.ActiveAnchorBar <= ((NinjaScriptBase)this).CurrentBar)
		{
			int startBarsAgo = ((NinjaScriptBase)this).CurrentBar - stdTracker.ActiveAnchorBar;
			bool num = ShowAllBands || stdTracker.Direction == -1;
			bool flag = ShowAllBands || stdTracker.Direction == 1;
			if (num)
			{
				if (ShowStdDev1)
				{
					Draw.Region((NinjaScriptBase)(object)this, "StdUpperCore_1", startBarsAgo, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[1], (ISeries<double>)(object)((NinjaScriptBase)this).Values[3], null, FillColorStdCore1, FillOpacityStdCore1);
				}
				if (ShowStdDev1 && ShowStdDev2)
				{
					Draw.Region((NinjaScriptBase)(object)this, "StdUpper1_2", startBarsAgo, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[3], (ISeries<double>)(object)((NinjaScriptBase)this).Values[4], null, FillColorStd12, FillOpacityStd12);
				}
				if (ShowStdDev2 && ShowStdDev3)
				{
					Draw.Region((NinjaScriptBase)(object)this, "StdUpper2_3", startBarsAgo, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[4], (ISeries<double>)(object)((NinjaScriptBase)this).Values[5], null, FillColorStd23, FillOpacityStd23);
				}
			}
			else
			{
				((IndicatorRenderBase)this).RemoveDrawObject("StdUpperCore_1");
				((IndicatorRenderBase)this).RemoveDrawObject("StdUpper1_2");
				((IndicatorRenderBase)this).RemoveDrawObject("StdUpper2_3");
			}
			if (flag)
			{
				if (ShowStdDev1)
				{
					Draw.Region((NinjaScriptBase)(object)this, "StdLowerCore_1", startBarsAgo, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[1], (ISeries<double>)(object)((NinjaScriptBase)this).Values[6], null, FillColorStdCore1, FillOpacityStdCore1);
				}
				if (ShowStdDev1 && ShowStdDev2)
				{
					Draw.Region((NinjaScriptBase)(object)this, "StdLower1_2", startBarsAgo, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[6], (ISeries<double>)(object)((NinjaScriptBase)this).Values[7], null, FillColorStd12, FillOpacityStd12);
				}
				if (ShowStdDev2 && ShowStdDev3)
				{
					Draw.Region((NinjaScriptBase)(object)this, "StdLower2_3", startBarsAgo, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[7], (ISeries<double>)(object)((NinjaScriptBase)this).Values[8], null, FillColorStd23, FillOpacityStd23);
				}
			}
			else
			{
				((IndicatorRenderBase)this).RemoveDrawObject("StdLowerCore_1");
				((IndicatorRenderBase)this).RemoveDrawObject("StdLower1_2");
				((IndicatorRenderBase)this).RemoveDrawObject("StdLower2_3");
			}
		}
		else if (!drawStd)
		{
			((IndicatorRenderBase)this).RemoveDrawObject("StdUpperCore_1");
			((IndicatorRenderBase)this).RemoveDrawObject("StdLowerCore_1");
			((IndicatorRenderBase)this).RemoveDrawObject("StdUpper1_2");
			((IndicatorRenderBase)this).RemoveDrawObject("StdLower1_2");
			((IndicatorRenderBase)this).RemoveDrawObject("StdUpper2_3");
			((IndicatorRenderBase)this).RemoveDrawObject("StdLower2_3");
		}
		if (ShowHtfBands && drawHtf && htfTracker.ActiveAnchorBar >= 0 && htfTracker.ActiveAnchorBar <= ((NinjaScriptBase)this).CurrentBar)
		{
			int startBarsAgo2 = ((NinjaScriptBase)this).CurrentBar - htfTracker.ActiveAnchorBar;
			bool num2 = ShowAllBands || htfTracker.Direction == -1;
			bool flag2 = ShowAllBands || htfTracker.Direction == 1;
			if (num2)
			{
				if (ShowStdDev1)
				{
					Draw.Region((NinjaScriptBase)(object)this, "HtfUpperCore_1", startBarsAgo2, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[2], (ISeries<double>)(object)((NinjaScriptBase)this).Values[9], null, FillColorHtfCore1, FillOpacityHtfCore1);
				}
				if (ShowStdDev1 && ShowStdDev2)
				{
					Draw.Region((NinjaScriptBase)(object)this, "HtfUpper1_2", startBarsAgo2, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[9], (ISeries<double>)(object)((NinjaScriptBase)this).Values[10], null, FillColorHtf12, FillOpacityHtf12);
				}
				if (ShowStdDev2 && ShowStdDev3)
				{
					Draw.Region((NinjaScriptBase)(object)this, "HtfUpper2_3", startBarsAgo2, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[10], (ISeries<double>)(object)((NinjaScriptBase)this).Values[11], null, FillColorHtf23, FillOpacityHtf23);
				}
			}
			else
			{
				((IndicatorRenderBase)this).RemoveDrawObject("HtfUpperCore_1");
				((IndicatorRenderBase)this).RemoveDrawObject("HtfUpper1_2");
				((IndicatorRenderBase)this).RemoveDrawObject("HtfUpper2_3");
			}
			if (flag2)
			{
				if (ShowStdDev1)
				{
					Draw.Region((NinjaScriptBase)(object)this, "HtfLowerCore_1", startBarsAgo2, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[2], (ISeries<double>)(object)((NinjaScriptBase)this).Values[12], null, FillColorHtfCore1, FillOpacityHtfCore1);
				}
				if (ShowStdDev1 && ShowStdDev2)
				{
					Draw.Region((NinjaScriptBase)(object)this, "HtfLower1_2", startBarsAgo2, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[12], (ISeries<double>)(object)((NinjaScriptBase)this).Values[13], null, FillColorHtf12, FillOpacityHtf12);
				}
				if (ShowStdDev2 && ShowStdDev3)
				{
					Draw.Region((NinjaScriptBase)(object)this, "HtfLower2_3", startBarsAgo2, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[13], (ISeries<double>)(object)((NinjaScriptBase)this).Values[14], null, FillColorHtf23, FillOpacityHtf23);
				}
			}
			else
			{
				((IndicatorRenderBase)this).RemoveDrawObject("HtfLowerCore_1");
				((IndicatorRenderBase)this).RemoveDrawObject("HtfLower1_2");
				((IndicatorRenderBase)this).RemoveDrawObject("HtfLower2_3");
			}
		}
		else if (!drawHtf)
		{
			((IndicatorRenderBase)this).RemoveDrawObject("HtfUpperCore_1");
			((IndicatorRenderBase)this).RemoveDrawObject("HtfLowerCore_1");
			((IndicatorRenderBase)this).RemoveDrawObject("HtfUpper1_2");
			((IndicatorRenderBase)this).RemoveDrawObject("HtfLower1_2");
			((IndicatorRenderBase)this).RemoveDrawObject("HtfUpper2_3");
			((IndicatorRenderBase)this).RemoveDrawObject("HtfLower2_3");
		}
	}

	private void DrawVwap(int coreIdx, int u1, int u2, int u3, int l1, int l2, int l3, VwapTracker tracker, bool visible, bool showBands)
	{
		if (!visible || !tracker.IsActive || tracker.ActiveAnchorBar < 0 || ((NinjaScriptBase)this).CurrentBar < tracker.ActiveAnchorBar)
		{
			((NinjaScriptBase)this).Values[coreIdx].Reset();
			if (u1 >= 0)
			{
				((NinjaScriptBase)this).Values[u1].Reset();
				((NinjaScriptBase)this).Values[u2].Reset();
				((NinjaScriptBase)this).Values[u3].Reset();
				((NinjaScriptBase)this).Values[l1].Reset();
				((NinjaScriptBase)this).Values[l2].Reset();
				((NinjaScriptBase)this).Values[l3].Reset();
			}
			return;
		}
		if (tracker.JustReversed)
		{
			for (int i = 0; i <= ((NinjaScriptBase)this).CurrentBar; i++)
			{
				((NinjaScriptBase)this).Values[coreIdx].Reset(i);
				if (u1 >= 0)
				{
					((NinjaScriptBase)this).Values[u1].Reset(i);
					((NinjaScriptBase)this).Values[u2].Reset(i);
					((NinjaScriptBase)this).Values[u3].Reset(i);
					((NinjaScriptBase)this).Values[l1].Reset(i);
					((NinjaScriptBase)this).Values[l2].Reset(i);
					((NinjaScriptBase)this).Values[l3].Reset(i);
				}
			}
			double num = 0.0;
			double num2 = 0.0;
			double num3 = 0.0;
			for (int num4 = ((NinjaScriptBase)this).CurrentBar - tracker.ActiveAnchorBar; num4 > 0; num4--)
			{
				double num5 = (((NinjaScriptBase)this).High[num4] + ((NinjaScriptBase)this).Low[num4] + ((NinjaScriptBase)this).Close[num4]) / 3.0;
				bool flag = num4 == ((NinjaScriptBase)this).CurrentBar - tracker.ActiveAnchorBar;
				num += ((NinjaScriptBase)this).Volume[num4];
				num2 += num5 * ((NinjaScriptBase)this).Volume[num4];
				num3 += num5 * num5 * ((NinjaScriptBase)this).Volume[num4];
				double num6 = 0.0;
				if (flag)
				{
					num6 = tracker.AnchorPrice;
				}
				else if (num > 0.0)
				{
					num6 = num2 / num;
				}
				((NinjaScriptBase)this).Values[coreIdx][num4] = num6;
				if (u1 >= 0)
				{
					double stdDev = 0.0;
					if (flag)
					{
						stdDev = 0.0;
					}
					else if (num > 0.0)
					{
						stdDev = Math.Sqrt(Math.Max(0.0, num3 / num - num6 * num6));
					}
					SetBands(num4, num6, stdDev, tracker.Direction, u1, u2, u3, l1, l2, l3, showBands);
				}
			}
			tracker.PriorCumVol = num;
			tracker.PriorCumPV = num2;
			tracker.PriorCumP2V = num3;
			tracker.LastBarSeen = ((NinjaScriptBase)this).CurrentBar;
			bool flag2 = ((NinjaScriptBase)this).CurrentBar - tracker.ActiveAnchorBar == 0;
			double num7 = (((NinjaScriptBase)this).High[0] + ((NinjaScriptBase)this).Low[0] + ((NinjaScriptBase)this).Close[0]) / 3.0;
			double num8 = num + ((NinjaScriptBase)this).Volume[0];
			double num9 = num2 + num7 * ((NinjaScriptBase)this).Volume[0];
			double num10 = num3 + num7 * num7 * ((NinjaScriptBase)this).Volume[0];
			double num11 = 0.0;
			if (flag2)
			{
				num11 = tracker.AnchorPrice;
			}
			else if (num8 > 0.0)
			{
				num11 = num9 / num8;
			}
			((NinjaScriptBase)this).Values[coreIdx][0] = num11;
			if (u1 >= 0)
			{
				double stdDev2 = 0.0;
				if (flag2)
				{
					stdDev2 = 0.0;
				}
				else if (num8 > 0.0)
				{
					stdDev2 = Math.Sqrt(Math.Max(0.0, num10 / num8 - num11 * num11));
				}
				SetBands(0, num11, stdDev2, tracker.Direction, u1, u2, u3, l1, l2, l3, showBands);
			}
		}
		else
		{
			bool flag3 = ((NinjaScriptBase)this).CurrentBar == tracker.ActiveAnchorBar;
			if (tracker.LastBarSeen != ((NinjaScriptBase)this).CurrentBar && tracker.LastBarSeen >= 0)
			{
				double num12 = (((NinjaScriptBase)this).High[1] + ((NinjaScriptBase)this).Low[1] + ((NinjaScriptBase)this).Close[1]) / 3.0;
				tracker.PriorCumVol += ((NinjaScriptBase)this).Volume[1];
				tracker.PriorCumPV += num12 * ((NinjaScriptBase)this).Volume[1];
				tracker.PriorCumP2V += num12 * num12 * ((NinjaScriptBase)this).Volume[1];
			}
			tracker.LastBarSeen = ((NinjaScriptBase)this).CurrentBar;
			double num13 = (((NinjaScriptBase)this).High[0] + ((NinjaScriptBase)this).Low[0] + ((NinjaScriptBase)this).Close[0]) / 3.0;
			double num14 = tracker.PriorCumVol + ((NinjaScriptBase)this).Volume[0];
			double num15 = tracker.PriorCumPV + num13 * ((NinjaScriptBase)this).Volume[0];
			double num16 = tracker.PriorCumP2V + num13 * num13 * ((NinjaScriptBase)this).Volume[0];
			double num17 = 0.0;
			if (flag3)
			{
				num17 = tracker.AnchorPrice;
			}
			else if (num14 > 0.0)
			{
				num17 = num15 / num14;
			}
			((NinjaScriptBase)this).Values[coreIdx][0] = num17;
			if (u1 >= 0)
			{
				double stdDev3 = 0.0;
				if (flag3)
				{
					stdDev3 = 0.0;
				}
				else if (num14 > 0.0)
				{
					double val = num16 / num14 - num17 * num17;
					stdDev3 = Math.Sqrt(Math.Max(0.0, val));
				}
				SetBands(0, num17, stdDev3, tracker.Direction, u1, u2, u3, l1, l2, l3, showBands);
			}
		}
		int num18 = ((NinjaScriptBase)this).CurrentBar - tracker.ActiveAnchorBar + 1;
		if (num18 >= 0 && num18 <= ((NinjaScriptBase)this).CurrentBar)
		{
			((NinjaScriptBase)this).Values[coreIdx].Reset(num18);
			if (u1 >= 0)
			{
				((NinjaScriptBase)this).Values[u1].Reset(num18);
				((NinjaScriptBase)this).Values[u2].Reset(num18);
				((NinjaScriptBase)this).Values[u3].Reset(num18);
				((NinjaScriptBase)this).Values[l1].Reset(num18);
				((NinjaScriptBase)this).Values[l2].Reset(num18);
				((NinjaScriptBase)this).Values[l3].Reset(num18);
			}
		}
	}

	private void SetBands(int barsAgo, double vwap, double stdDev, int anchorDirection, int u1, int u2, int u3, int l1, int l2, int l3, bool showBands)
	{
		if (!showBands)
		{
			((NinjaScriptBase)this).Values[u1].Reset(barsAgo);
			((NinjaScriptBase)this).Values[u2].Reset(barsAgo);
			((NinjaScriptBase)this).Values[u3].Reset(barsAgo);
			((NinjaScriptBase)this).Values[l1].Reset(barsAgo);
			((NinjaScriptBase)this).Values[l2].Reset(barsAgo);
			((NinjaScriptBase)this).Values[l3].Reset(barsAgo);
			return;
		}
		bool num = ShowAllBands || anchorDirection == -1;
		bool flag = ShowAllBands || anchorDirection == 1;
		if (num)
		{
			if (ShowStdDev1)
			{
				((NinjaScriptBase)this).Values[u1][barsAgo] = vwap + StdDevMultiplier1 * stdDev;
			}
			else
			{
				((NinjaScriptBase)this).Values[u1].Reset(barsAgo);
			}
			if (ShowStdDev2)
			{
				((NinjaScriptBase)this).Values[u2][barsAgo] = vwap + StdDevMultiplier2 * stdDev;
			}
			else
			{
				((NinjaScriptBase)this).Values[u2].Reset(barsAgo);
			}
			if (ShowStdDev3)
			{
				((NinjaScriptBase)this).Values[u3][barsAgo] = vwap + StdDevMultiplier3 * stdDev;
			}
			else
			{
				((NinjaScriptBase)this).Values[u3].Reset(barsAgo);
			}
		}
		else
		{
			((NinjaScriptBase)this).Values[u1].Reset(barsAgo);
			((NinjaScriptBase)this).Values[u2].Reset(barsAgo);
			((NinjaScriptBase)this).Values[u3].Reset(barsAgo);
		}
		if (flag)
		{
			if (ShowStdDev1)
			{
				((NinjaScriptBase)this).Values[l1][barsAgo] = vwap - StdDevMultiplier1 * stdDev;
			}
			else
			{
				((NinjaScriptBase)this).Values[l1].Reset(barsAgo);
			}
			if (ShowStdDev2)
			{
				((NinjaScriptBase)this).Values[l2][barsAgo] = vwap - StdDevMultiplier2 * stdDev;
			}
			else
			{
				((NinjaScriptBase)this).Values[l2].Reset(barsAgo);
			}
			if (ShowStdDev3)
			{
				((NinjaScriptBase)this).Values[l3][barsAgo] = vwap - StdDevMultiplier3 * stdDev;
			}
			else
			{
				((NinjaScriptBase)this).Values[l3].Reset(barsAgo);
			}
		}
		else
		{
			((NinjaScriptBase)this).Values[l1].Reset(barsAgo);
			((NinjaScriptBase)this).Values[l2].Reset(barsAgo);
			((NinjaScriptBase)this).Values[l3].Reset(barsAgo);
		}
	}
}

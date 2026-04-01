using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

public class OrcaTimeVWAPs : Indicator
{
	private OrcaVwapSession globexSession;

	private OrcaVwapSession rthSession;

	private OrcaVwapSession weeklySession;

	private Queue<OrcaVwapBucket> rollingHistory;

	private OrcaVwapBucket rollingDeveloping;

	private OrcaVwapSession rollingTotal;

	private DateTime currentMinuteToken;

	private double lastBarVolume;

	private int lastBarIndex;

	[NinjaScriptProperty]
	[Display(Name = "1. Show VWAP", Order = 1, GroupName = "1. Globex VWAP")]
	public bool GlobexShowVWAP { get; set; }

	[NinjaScriptProperty]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
	[Display(Name = "2. Start Time", Order = 2, GroupName = "1. Globex VWAP")]
	public TimeSpan GlobexStartTime { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 1", Order = 3, GroupName = "1. Globex VWAP")]
	public bool GlobexShowDev1 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 1 Multiplier", Order = 4, GroupName = "1. Globex VWAP")]
	public double GlobexDev1Mult { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 2", Order = 5, GroupName = "1. Globex VWAP")]
	public bool GlobexShowDev2 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 2 Multiplier", Order = 6, GroupName = "1. Globex VWAP")]
	public double GlobexDev2Mult { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 3", Order = 7, GroupName = "1. Globex VWAP")]
	public bool GlobexShowDev3 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 3 Multiplier", Order = 8, GroupName = "1. Globex VWAP")]
	public double GlobexDev3Mult { get; set; }

	[XmlIgnore]
	[Display(Name = "Core-Dev1 Fill Color", Order = 9, GroupName = "1. Globex VWAP")]
	public Brush GlobexFillColorCore { get; set; }

	[Browsable(false)]
	public string GlobexFillColorCoreSerializable
	{
		get
		{
			return Serialize.BrushToString(GlobexFillColorCore);
		}
		set
		{
			GlobexFillColorCore = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Core-Dev1 Fill Opacity (0 = Off)", Order = 10, GroupName = "1. Globex VWAP")]
	public int GlobexFillOpacityCore { get; set; }

	[XmlIgnore]
	[Display(Name = "Dev1-Dev2 Fill Color", Order = 11, GroupName = "1. Globex VWAP")]
	public Brush GlobexFillColor12 { get; set; }

	[Browsable(false)]
	public string GlobexFillColor12Serializable
	{
		get
		{
			return Serialize.BrushToString(GlobexFillColor12);
		}
		set
		{
			GlobexFillColor12 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Dev1-Dev2 Fill Opacity (0 = Off)", Order = 12, GroupName = "1. Globex VWAP")]
	public int GlobexFillOpacity12 { get; set; }

	[XmlIgnore]
	[Display(Name = "Dev2-Dev3 Fill Color", Order = 13, GroupName = "1. Globex VWAP")]
	public Brush GlobexFillColor23 { get; set; }

	[Browsable(false)]
	public string GlobexFillColor23Serializable
	{
		get
		{
			return Serialize.BrushToString(GlobexFillColor23);
		}
		set
		{
			GlobexFillColor23 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Dev2-Dev3 Fill Opacity (0 = Off)", Order = 14, GroupName = "1. Globex VWAP")]
	public int GlobexFillOpacity23 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "1. Show VWAP", Order = 1, GroupName = "2. RTH VWAP")]
	public bool RthShowVWAP { get; set; }

	[NinjaScriptProperty]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
	[Display(Name = "2. Start Time", Order = 2, GroupName = "2. RTH VWAP")]
	public TimeSpan RthStartTime { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 1", Order = 3, GroupName = "2. RTH VWAP")]
	public bool RthShowDev1 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 1 Multiplier", Order = 4, GroupName = "2. RTH VWAP")]
	public double RthDev1Mult { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 2", Order = 5, GroupName = "2. RTH VWAP")]
	public bool RthShowDev2 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 2 Multiplier", Order = 6, GroupName = "2. RTH VWAP")]
	public double RthDev2Mult { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 3", Order = 7, GroupName = "2. RTH VWAP")]
	public bool RthShowDev3 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 3 Multiplier", Order = 8, GroupName = "2. RTH VWAP")]
	public double RthDev3Mult { get; set; }

	[XmlIgnore]
	[Display(Name = "Core-Dev1 Fill Color", Order = 9, GroupName = "2. RTH VWAP")]
	public Brush RthFillColorCore { get; set; }

	[Browsable(false)]
	public string RthFillColorCoreSerializable
	{
		get
		{
			return Serialize.BrushToString(RthFillColorCore);
		}
		set
		{
			RthFillColorCore = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Core-Dev1 Fill Opacity (0 = Off)", Order = 10, GroupName = "2. RTH VWAP")]
	public int RthFillOpacityCore { get; set; }

	[XmlIgnore]
	[Display(Name = "Dev1-Dev2 Fill Color", Order = 11, GroupName = "2. RTH VWAP")]
	public Brush RthFillColor12 { get; set; }

	[Browsable(false)]
	public string RthFillColor12Serializable
	{
		get
		{
			return Serialize.BrushToString(RthFillColor12);
		}
		set
		{
			RthFillColor12 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Dev1-Dev2 Fill Opacity (0 = Off)", Order = 12, GroupName = "2. RTH VWAP")]
	public int RthFillOpacity12 { get; set; }

	[XmlIgnore]
	[Display(Name = "Dev2-Dev3 Fill Color", Order = 13, GroupName = "2. RTH VWAP")]
	public Brush RthFillColor23 { get; set; }

	[Browsable(false)]
	public string RthFillColor23Serializable
	{
		get
		{
			return Serialize.BrushToString(RthFillColor23);
		}
		set
		{
			RthFillColor23 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Dev2-Dev3 Fill Opacity (0 = Off)", Order = 14, GroupName = "2. RTH VWAP")]
	public int RthFillOpacity23 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "1. Show VWAP", Order = 1, GroupName = "3. Rolling VWAP")]
	public bool RollingShowVWAP { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "2. Rolling Period", Order = 2, GroupName = "3. Rolling VWAP")]
	public RollingVwapPeriod RollingPeriod { get; set; }

	[NinjaScriptProperty]
	[Range(1, 3000)]
	[Display(Name = "3. Minutes In Trading Day", Order = 3, GroupName = "3. Rolling VWAP", Description = "1380 for Globex, 390 for RTH. Used to calculate Day1, Day5, Day20.")]
	public int MinutesPerDay { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 1", Order = 4, GroupName = "3. Rolling VWAP")]
	public bool RollingShowDev1 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 1 Multiplier", Order = 5, GroupName = "3. Rolling VWAP")]
	public double RollingDev1Mult { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 2", Order = 6, GroupName = "3. Rolling VWAP")]
	public bool RollingShowDev2 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 2 Multiplier", Order = 7, GroupName = "3. Rolling VWAP")]
	public double RollingDev2Mult { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 3", Order = 8, GroupName = "3. Rolling VWAP")]
	public bool RollingShowDev3 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 3 Multiplier", Order = 9, GroupName = "3. Rolling VWAP")]
	public double RollingDev3Mult { get; set; }

	[XmlIgnore]
	[Display(Name = "Core-Dev1 Fill Color", Order = 10, GroupName = "3. Rolling VWAP")]
	public Brush RollingFillColorCore { get; set; }

	[Browsable(false)]
	public string RollingFillColorCoreSerializable
	{
		get
		{
			return Serialize.BrushToString(RollingFillColorCore);
		}
		set
		{
			RollingFillColorCore = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Core-Dev1 Fill Opacity (0 = Off)", Order = 11, GroupName = "3. Rolling VWAP")]
	public int RollingFillOpacityCore { get; set; }

	[XmlIgnore]
	[Display(Name = "Dev1-Dev2 Fill Color", Order = 12, GroupName = "3. Rolling VWAP")]
	public Brush RollingFillColor12 { get; set; }

	[Browsable(false)]
	public string RollingFillColor12Serializable
	{
		get
		{
			return Serialize.BrushToString(RollingFillColor12);
		}
		set
		{
			RollingFillColor12 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Dev1-Dev2 Fill Opacity (0 = Off)", Order = 13, GroupName = "3. Rolling VWAP")]
	public int RollingFillOpacity12 { get; set; }

	[XmlIgnore]
	[Display(Name = "Dev2-Dev3 Fill Color", Order = 14, GroupName = "3. Rolling VWAP")]
	public Brush RollingFillColor23 { get; set; }

	[Browsable(false)]
	public string RollingFillColor23Serializable
	{
		get
		{
			return Serialize.BrushToString(RollingFillColor23);
		}
		set
		{
			RollingFillColor23 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Dev2-Dev3 Fill Opacity (0 = Off)", Order = 15, GroupName = "3. Rolling VWAP")]
	public int RollingFillOpacity23 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "1. Show VWAP", Order = 1, GroupName = "4. Weekly VWAP")]
	public bool WeeklyShowVWAP { get; set; }

	[NinjaScriptProperty]
	[PropertyEditor("NinjaTrader.Gui.Tools.TimeSpanEditorKey")]
	[Display(Name = "2. Start Time", Order = 2, GroupName = "4. Weekly VWAP")]
	public TimeSpan WeeklyStartTime { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 1", Order = 3, GroupName = "4. Weekly VWAP")]
	public bool WeeklyShowDev1 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 1 Multiplier", Order = 4, GroupName = "4. Weekly VWAP")]
	public double WeeklyDev1Mult { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 2", Order = 5, GroupName = "4. Weekly VWAP")]
	public bool WeeklyShowDev2 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 2 Multiplier", Order = 6, GroupName = "4. Weekly VWAP")]
	public double WeeklyDev2Mult { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Show Dev 3", Order = 7, GroupName = "4. Weekly VWAP")]
	public bool WeeklyShowDev3 { get; set; }

	[NinjaScriptProperty]
	[Display(Name = "Dev 3 Multiplier", Order = 8, GroupName = "4. Weekly VWAP")]
	public double WeeklyDev3Mult { get; set; }

	[XmlIgnore]
	[Display(Name = "Core-Dev1 Fill Color", Order = 9, GroupName = "4. Weekly VWAP")]
	public Brush WeeklyFillColorCore { get; set; }

	[Browsable(false)]
	public string WeeklyFillColorCoreSerializable
	{
		get
		{
			return Serialize.BrushToString(WeeklyFillColorCore);
		}
		set
		{
			WeeklyFillColorCore = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Core-Dev1 Fill Opacity (0 = Off)", Order = 10, GroupName = "4. Weekly VWAP")]
	public int WeeklyFillOpacityCore { get; set; }

	[XmlIgnore]
	[Display(Name = "Dev1-Dev2 Fill Color", Order = 11, GroupName = "4. Weekly VWAP")]
	public Brush WeeklyFillColor12 { get; set; }

	[Browsable(false)]
	public string WeeklyFillColor12Serializable
	{
		get
		{
			return Serialize.BrushToString(WeeklyFillColor12);
		}
		set
		{
			WeeklyFillColor12 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Dev1-Dev2 Fill Opacity (0 = Off)", Order = 12, GroupName = "4. Weekly VWAP")]
	public int WeeklyFillOpacity12 { get; set; }

	[XmlIgnore]
	[Display(Name = "Dev2-Dev3 Fill Color", Order = 13, GroupName = "4. Weekly VWAP")]
	public Brush WeeklyFillColor23 { get; set; }

	[Browsable(false)]
	public string WeeklyFillColor23Serializable
	{
		get
		{
			return Serialize.BrushToString(WeeklyFillColor23);
		}
		set
		{
			WeeklyFillColor23 = Serialize.StringToBrush(value);
		}
	}

	[NinjaScriptProperty]
	[Range(0, 100)]
	[Display(Name = "Dev2-Dev3 Fill Opacity (0 = Off)", Order = 14, GroupName = "4. Weekly VWAP")]
	public int WeeklyFillOpacity23 { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_05b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b9: Invalid comparison between Unknown and I4
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Expected O, but got Unknown
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Expected O, but got Unknown
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Expected O, but got Unknown
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Expected O, but got Unknown
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Expected O, but got Unknown
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Expected O, but got Unknown
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Expected O, but got Unknown
		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Expected O, but got Unknown
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Expected O, but got Unknown
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Expected O, but got Unknown
		//IL_03c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Expected O, but got Unknown
		//IL_03e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Expected O, but got Unknown
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Expected O, but got Unknown
		//IL_0419: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Expected O, but got Unknown
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_0445: Expected O, but got Unknown
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Expected O, but got Unknown
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_047d: Expected O, but got Unknown
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Expected O, but got Unknown
		//IL_04a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b5: Expected O, but got Unknown
		//IL_04c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Expected O, but got Unknown
		//IL_04dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ed: Expected O, but got Unknown
		//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0509: Expected O, but got Unknown
		//IL_0515: Unknown result type (might be due to invalid IL or missing references)
		//IL_0525: Expected O, but got Unknown
		//IL_0531: Unknown result type (might be due to invalid IL or missing references)
		//IL_0541: Expected O, but got Unknown
		//IL_054d: Unknown result type (might be due to invalid IL or missing references)
		//IL_055d: Expected O, but got Unknown
		//IL_0569: Unknown result type (might be due to invalid IL or missing references)
		//IL_0579: Expected O, but got Unknown
		//IL_0585: Unknown result type (might be due to invalid IL or missing references)
		//IL_0595: Expected O, but got Unknown
		//IL_05a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b1: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = "All-in-one Anchored Time and Rolling VWAP with Deviation bands.";
			((NinjaScriptBase)this).Name = "Orca Time VWAPs";
			((NinjaScriptBase)this).Calculate = (Calculate)2;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = true;
			((IndicatorBase)this).DrawOnPricePanel = true;
			((IndicatorBase)this).DrawHorizontalGridLines = true;
			((IndicatorBase)this).DrawVerticalGridLines = true;
			((IndicatorBase)this).PaintPriceMarkers = true;
			((NinjaScriptBase)this).ScaleJustification = (ScaleJustification)1;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			GlobexShowVWAP = true;
			GlobexStartTime = new TimeSpan(18, 0, 0);
			GlobexShowDev1 = true;
			GlobexDev1Mult = 1.0;
			GlobexShowDev2 = true;
			GlobexDev2Mult = 2.0;
			GlobexShowDev3 = true;
			GlobexDev3Mult = 3.0;
			GlobexFillColorCore = Brushes.DodgerBlue;
			GlobexFillOpacityCore = 0;
			GlobexFillColor12 = Brushes.DodgerBlue;
			GlobexFillOpacity12 = 0;
			GlobexFillColor23 = Brushes.DodgerBlue;
			GlobexFillOpacity23 = 0;
			RthShowVWAP = true;
			RthStartTime = new TimeSpan(9, 30, 0);
			RthShowDev1 = true;
			RthDev1Mult = 1.0;
			RthShowDev2 = true;
			RthDev2Mult = 2.0;
			RthShowDev3 = true;
			RthDev3Mult = 3.0;
			RthFillColorCore = Brushes.Orange;
			RthFillOpacityCore = 0;
			RthFillColor12 = Brushes.Orange;
			RthFillOpacity12 = 0;
			RthFillColor23 = Brushes.Orange;
			RthFillOpacity23 = 0;
			RollingShowVWAP = true;
			RollingPeriod = RollingVwapPeriod.Day1;
			MinutesPerDay = 1380;
			RollingShowDev1 = true;
			RollingDev1Mult = 1.0;
			RollingShowDev2 = true;
			RollingDev2Mult = 2.0;
			RollingShowDev3 = true;
			RollingDev3Mult = 3.0;
			RollingFillColorCore = Brushes.LimeGreen;
			RollingFillOpacityCore = 0;
			RollingFillColor12 = Brushes.LimeGreen;
			RollingFillOpacity12 = 0;
			RollingFillColor23 = Brushes.LimeGreen;
			RollingFillOpacity23 = 0;
			WeeklyShowVWAP = true;
			WeeklyStartTime = new TimeSpan(18, 0, 0);
			WeeklyShowDev1 = true;
			WeeklyDev1Mult = 1.0;
			WeeklyShowDev2 = true;
			WeeklyDev2Mult = 2.0;
			WeeklyShowDev3 = true;
			WeeklyDev3Mult = 3.0;
			WeeklyFillColorCore = Brushes.Plum;
			WeeklyFillOpacityCore = 0;
			WeeklyFillColor12 = Brushes.Plum;
			WeeklyFillOpacity12 = 0;
			WeeklyFillColor23 = Brushes.Plum;
			WeeklyFillOpacity23 = 0;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, (DashStyleHelper)0, 2f), (PlotStyle)6, "Globex VWAP");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, (DashStyleHelper)1, 1f), (PlotStyle)6, "Globex Dev 1 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, (DashStyleHelper)1, 1f), (PlotStyle)6, "Globex Dev 1 Lower");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, (DashStyleHelper)4, 1f), (PlotStyle)6, "Globex Dev 2 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, (DashStyleHelper)4, 1f), (PlotStyle)6, "Globex Dev 2 Lower");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, (DashStyleHelper)2, 1f), (PlotStyle)6, "Globex Dev 3 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, (DashStyleHelper)2, 1f), (PlotStyle)6, "Globex Dev 3 Lower");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Orange, (DashStyleHelper)0, 2f), (PlotStyle)6, "Rth VWAP");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Orange, (DashStyleHelper)1, 1f), (PlotStyle)6, "Rth Dev 1 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Orange, (DashStyleHelper)1, 1f), (PlotStyle)6, "Rth Dev 1 Lower");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Orange, (DashStyleHelper)4, 1f), (PlotStyle)6, "Rth Dev 2 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Orange, (DashStyleHelper)4, 1f), (PlotStyle)6, "Rth Dev 2 Lower");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Orange, (DashStyleHelper)2, 1f), (PlotStyle)6, "Rth Dev 3 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Orange, (DashStyleHelper)2, 1f), (PlotStyle)6, "Rth Dev 3 Lower");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.LimeGreen, (DashStyleHelper)0, 2f), (PlotStyle)6, "Rolling VWAP");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.LimeGreen, (DashStyleHelper)1, 1f), (PlotStyle)6, "Rolling Dev 1 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.LimeGreen, (DashStyleHelper)1, 1f), (PlotStyle)6, "Rolling Dev 1 Lower");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.LimeGreen, (DashStyleHelper)4, 1f), (PlotStyle)6, "Rolling Dev 2 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.LimeGreen, (DashStyleHelper)4, 1f), (PlotStyle)6, "Rolling Dev 2 Lower");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.LimeGreen, (DashStyleHelper)2, 1f), (PlotStyle)6, "Rolling Dev 3 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.LimeGreen, (DashStyleHelper)2, 1f), (PlotStyle)6, "Rolling Dev 3 Lower");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Plum, (DashStyleHelper)0, 2f), (PlotStyle)6, "Weekly VWAP");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Plum, (DashStyleHelper)1, 1f), (PlotStyle)6, "Weekly Dev 1 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Plum, (DashStyleHelper)1, 1f), (PlotStyle)6, "Weekly Dev 1 Lower");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Plum, (DashStyleHelper)4, 1f), (PlotStyle)6, "Weekly Dev 2 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Plum, (DashStyleHelper)4, 1f), (PlotStyle)6, "Weekly Dev 2 Lower");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Plum, (DashStyleHelper)2, 1f), (PlotStyle)6, "Weekly Dev 3 Upper");
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Plum, (DashStyleHelper)2, 1f), (PlotStyle)6, "Weekly Dev 3 Lower");
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			globexSession = new OrcaVwapSession();
			rthSession = new OrcaVwapSession();
			weeklySession = new OrcaVwapSession();
			rollingHistory = new Queue<OrcaVwapBucket>();
			rollingDeveloping = new OrcaVwapBucket();
			rollingTotal = new OrcaVwapSession();
			currentMinuteToken = DateTime.MinValue;
			lastBarVolume = 0.0;
			lastBarIndex = -1;
		}
	}

	private bool CrossedTime(DateTime start, DateTime end, TimeSpan target)
	{
		if (start >= end)
		{
			return false;
		}
		DateTime dateTime = ((!(end.TimeOfDay >= target)) ? (end.Date.AddDays(-1.0) + target) : (end.Date + target));
		if (start < dateTime)
		{
			return end >= dateTime;
		}
		return false;
	}

	private bool CrossedWeekly(DateTime start, DateTime end, TimeSpan target)
	{
		if (start >= end)
		{
			return false;
		}
		int num = (int)end.DayOfWeek;
		if (num < 0)
		{
			num += 7;
		}
		DateTime dateTime = end.Date.AddDays(-num);
		if (end.DayOfWeek == DayOfWeek.Sunday && end.TimeOfDay < target)
		{
			dateTime = dateTime.AddDays(-7.0);
		}
		DateTime dateTime2 = dateTime.Date + target;
		if (start < dateTime2)
		{
			return end >= dateTime2;
		}
		return false;
	}

	protected override void OnBarUpdate()
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).CurrentBar < 0)
		{
			return;
		}
		double num = ((NinjaScriptBase)this).Volume[0];
		double num2 = 0.0;
		num2 = ((((NinjaScriptBase)this).CurrentBar == lastBarIndex) ? (num - lastBarVolume) : num);
		lastBarVolume = num;
		lastBarIndex = ((NinjaScriptBase)this).CurrentBar;
		if (num2 <= 0.0)
		{
			return;
		}
		double num3 = (((int)((NinjaScript)this).State == 5) ? ((NinjaScriptBase)this).Typical[0] : ((NinjaScriptBase)this).Close[0]);
		DateTime dateTime = ((NinjaScriptBase)this).Time[0];
		DateTime start = ((((NinjaScriptBase)this).CurrentBar > 0) ? ((NinjaScriptBase)this).Time[1] : dateTime);
		if (GlobexShowVWAP)
		{
			if (CrossedTime(start, dateTime, GlobexStartTime))
			{
				globexSession.Reset();
			}
			globexSession.Add(num3, num2);
			if (globexSession.SumVol > 0.0)
			{
				double vwap = globexSession.Vwap;
				double stdDev = globexSession.StdDev;
				((NinjaScriptBase)this).Values[0][0] = vwap;
				if (GlobexShowDev1)
				{
					((NinjaScriptBase)this).Values[1][0] = vwap + stdDev * GlobexDev1Mult;
					((NinjaScriptBase)this).Values[2][0] = vwap - stdDev * GlobexDev1Mult;
				}
				if (GlobexShowDev2)
				{
					((NinjaScriptBase)this).Values[3][0] = vwap + stdDev * GlobexDev2Mult;
					((NinjaScriptBase)this).Values[4][0] = vwap - stdDev * GlobexDev2Mult;
				}
				if (GlobexShowDev3)
				{
					((NinjaScriptBase)this).Values[5][0] = vwap + stdDev * GlobexDev3Mult;
					((NinjaScriptBase)this).Values[6][0] = vwap - stdDev * GlobexDev3Mult;
				}
				if (GlobexFillOpacityCore > 0)
				{
					Draw.Region((NinjaScriptBase)(object)this, "GlobexR_CoreU", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[0], (ISeries<double>)(object)((NinjaScriptBase)this).Values[1], null, GlobexFillColorCore, GlobexFillOpacityCore);
					Draw.Region((NinjaScriptBase)(object)this, "GlobexR_CoreD", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[0], (ISeries<double>)(object)((NinjaScriptBase)this).Values[2], null, GlobexFillColorCore, GlobexFillOpacityCore);
				}
				if (GlobexFillOpacity12 > 0)
				{
					Draw.Region((NinjaScriptBase)(object)this, "GlobexR_12U", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[1], (ISeries<double>)(object)((NinjaScriptBase)this).Values[3], null, GlobexFillColor12, GlobexFillOpacity12);
					Draw.Region((NinjaScriptBase)(object)this, "GlobexR_12D", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[2], (ISeries<double>)(object)((NinjaScriptBase)this).Values[4], null, GlobexFillColor12, GlobexFillOpacity12);
				}
				if (GlobexFillOpacity23 > 0)
				{
					Draw.Region((NinjaScriptBase)(object)this, "GlobexR_23U", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[3], (ISeries<double>)(object)((NinjaScriptBase)this).Values[5], null, GlobexFillColor23, GlobexFillOpacity23);
					Draw.Region((NinjaScriptBase)(object)this, "GlobexR_23D", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[4], (ISeries<double>)(object)((NinjaScriptBase)this).Values[6], null, GlobexFillColor23, GlobexFillOpacity23);
				}
			}
		}
		if (RthShowVWAP)
		{
			if (CrossedTime(start, dateTime, RthStartTime))
			{
				rthSession.Reset();
			}
			rthSession.Add(num3, num2);
			if (rthSession.SumVol > 0.0)
			{
				double vwap2 = rthSession.Vwap;
				double stdDev2 = rthSession.StdDev;
				((NinjaScriptBase)this).Values[7][0] = vwap2;
				if (RthShowDev1)
				{
					((NinjaScriptBase)this).Values[8][0] = vwap2 + stdDev2 * RthDev1Mult;
					((NinjaScriptBase)this).Values[9][0] = vwap2 - stdDev2 * RthDev1Mult;
				}
				if (RthShowDev2)
				{
					((NinjaScriptBase)this).Values[10][0] = vwap2 + stdDev2 * RthDev2Mult;
					((NinjaScriptBase)this).Values[11][0] = vwap2 - stdDev2 * RthDev2Mult;
				}
				if (RthShowDev3)
				{
					((NinjaScriptBase)this).Values[12][0] = vwap2 + stdDev2 * RthDev3Mult;
					((NinjaScriptBase)this).Values[13][0] = vwap2 - stdDev2 * RthDev3Mult;
				}
				if (RthFillOpacityCore > 0)
				{
					Draw.Region((NinjaScriptBase)(object)this, "RthR_CoreU", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[7], (ISeries<double>)(object)((NinjaScriptBase)this).Values[8], null, RthFillColorCore, RthFillOpacityCore);
					Draw.Region((NinjaScriptBase)(object)this, "RthR_CoreD", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[7], (ISeries<double>)(object)((NinjaScriptBase)this).Values[9], null, RthFillColorCore, RthFillOpacityCore);
				}
				if (RthFillOpacity12 > 0)
				{
					Draw.Region((NinjaScriptBase)(object)this, "RthR_12U", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[8], (ISeries<double>)(object)((NinjaScriptBase)this).Values[10], null, RthFillColor12, RthFillOpacity12);
					Draw.Region((NinjaScriptBase)(object)this, "RthR_12D", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[9], (ISeries<double>)(object)((NinjaScriptBase)this).Values[11], null, RthFillColor12, RthFillOpacity12);
				}
				if (RthFillOpacity23 > 0)
				{
					Draw.Region((NinjaScriptBase)(object)this, "RthR_23U", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[10], (ISeries<double>)(object)((NinjaScriptBase)this).Values[12], null, RthFillColor23, RthFillOpacity23);
					Draw.Region((NinjaScriptBase)(object)this, "RthR_23D", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[11], (ISeries<double>)(object)((NinjaScriptBase)this).Values[13], null, RthFillColor23, RthFillOpacity23);
				}
			}
		}
		if (WeeklyShowVWAP)
		{
			if (CrossedWeekly(start, dateTime, WeeklyStartTime))
			{
				weeklySession.Reset();
			}
			weeklySession.Add(num3, num2);
			if (weeklySession.SumVol > 0.0)
			{
				double vwap3 = weeklySession.Vwap;
				double stdDev3 = weeklySession.StdDev;
				((NinjaScriptBase)this).Values[21][0] = vwap3;
				if (WeeklyShowDev1)
				{
					((NinjaScriptBase)this).Values[22][0] = vwap3 + stdDev3 * WeeklyDev1Mult;
					((NinjaScriptBase)this).Values[23][0] = vwap3 - stdDev3 * WeeklyDev1Mult;
				}
				if (WeeklyShowDev2)
				{
					((NinjaScriptBase)this).Values[24][0] = vwap3 + stdDev3 * WeeklyDev2Mult;
					((NinjaScriptBase)this).Values[25][0] = vwap3 - stdDev3 * WeeklyDev2Mult;
				}
				if (WeeklyShowDev3)
				{
					((NinjaScriptBase)this).Values[26][0] = vwap3 + stdDev3 * WeeklyDev3Mult;
					((NinjaScriptBase)this).Values[27][0] = vwap3 - stdDev3 * WeeklyDev3Mult;
				}
				if (WeeklyFillOpacityCore > 0)
				{
					Draw.Region((NinjaScriptBase)(object)this, "WeeklyR_CoreU", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[21], (ISeries<double>)(object)((NinjaScriptBase)this).Values[22], null, WeeklyFillColorCore, WeeklyFillOpacityCore);
					Draw.Region((NinjaScriptBase)(object)this, "WeeklyR_CoreD", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[21], (ISeries<double>)(object)((NinjaScriptBase)this).Values[23], null, WeeklyFillColorCore, WeeklyFillOpacityCore);
				}
				if (WeeklyFillOpacity12 > 0)
				{
					Draw.Region((NinjaScriptBase)(object)this, "WeeklyR_12U", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[22], (ISeries<double>)(object)((NinjaScriptBase)this).Values[24], null, WeeklyFillColor12, WeeklyFillOpacity12);
					Draw.Region((NinjaScriptBase)(object)this, "WeeklyR_12D", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[23], (ISeries<double>)(object)((NinjaScriptBase)this).Values[25], null, WeeklyFillColor12, WeeklyFillOpacity12);
				}
				if (WeeklyFillOpacity23 > 0)
				{
					Draw.Region((NinjaScriptBase)(object)this, "WeeklyR_23U", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[24], (ISeries<double>)(object)((NinjaScriptBase)this).Values[26], null, WeeklyFillColor23, WeeklyFillOpacity23);
					Draw.Region((NinjaScriptBase)(object)this, "WeeklyR_23D", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[25], (ISeries<double>)(object)((NinjaScriptBase)this).Values[27], null, WeeklyFillColor23, WeeklyFillOpacity23);
				}
			}
		}
		if (!RollingShowVWAP)
		{
			return;
		}
		DateTime dateTime2 = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
		if (dateTime2 > currentMinuteToken)
		{
			if (currentMinuteToken != DateTime.MinValue)
			{
				rollingHistory.Enqueue(rollingDeveloping);
				int num4 = ((RollingPeriod == RollingVwapPeriod.Day1) ? MinutesPerDay : ((RollingPeriod == RollingVwapPeriod.Day5) ? (MinutesPerDay * 5) : ((RollingPeriod != RollingVwapPeriod.Day20) ? ((int)RollingPeriod) : (MinutesPerDay * 20))));
				int num5 = (int)(dateTime2 - currentMinuteToken).TotalMinutes;
				if (num5 > 1 && num5 <= 720)
				{
					int num6 = Math.Min(num5 - 1, num4);
					for (int i = 0; i < num6; i++)
					{
						rollingHistory.Enqueue(new OrcaVwapBucket());
					}
				}
				rollingDeveloping = new OrcaVwapBucket();
				while (rollingHistory.Count >= num4)
				{
					rollingHistory.Dequeue();
				}
				rollingTotal.Reset();
				foreach (OrcaVwapBucket item in rollingHistory)
				{
					rollingTotal.SumVol += item.SumVol;
					rollingTotal.SumPriceVol += item.SumPriceVol;
					rollingTotal.SumPrice2Vol += item.SumPrice2Vol;
				}
			}
			currentMinuteToken = dateTime2;
		}
		else if (dateTime2 < currentMinuteToken)
		{
			rollingHistory.Clear();
			rollingDeveloping = new OrcaVwapBucket();
			rollingTotal.Reset();
			currentMinuteToken = dateTime2;
		}
		rollingDeveloping.SumVol += num2;
		rollingDeveloping.SumPriceVol += num3 * num2;
		rollingDeveloping.SumPrice2Vol += num3 * num3 * num2;
		double num7 = rollingTotal.SumVol + rollingDeveloping.SumVol;
		if (num7 > 0.0)
		{
			double num8 = rollingTotal.SumPriceVol + rollingDeveloping.SumPriceVol;
			double num9 = rollingTotal.SumPrice2Vol + rollingDeveloping.SumPrice2Vol;
			double num10 = num8 / num7;
			double num11 = Math.Sqrt(Math.Max(0.0, num9 / num7 - num10 * num10));
			((NinjaScriptBase)this).Values[14][0] = num10;
			if (RollingShowDev1)
			{
				((NinjaScriptBase)this).Values[15][0] = num10 + num11 * RollingDev1Mult;
				((NinjaScriptBase)this).Values[16][0] = num10 - num11 * RollingDev1Mult;
			}
			if (RollingShowDev2)
			{
				((NinjaScriptBase)this).Values[17][0] = num10 + num11 * RollingDev2Mult;
				((NinjaScriptBase)this).Values[18][0] = num10 - num11 * RollingDev2Mult;
			}
			if (RollingShowDev3)
			{
				((NinjaScriptBase)this).Values[19][0] = num10 + num11 * RollingDev3Mult;
				((NinjaScriptBase)this).Values[20][0] = num10 - num11 * RollingDev3Mult;
			}
			if (RollingFillOpacityCore > 0)
			{
				Draw.Region((NinjaScriptBase)(object)this, "RollR_CoreU", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[14], (ISeries<double>)(object)((NinjaScriptBase)this).Values[15], null, RollingFillColorCore, RollingFillOpacityCore);
				Draw.Region((NinjaScriptBase)(object)this, "RollR_CoreD", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[14], (ISeries<double>)(object)((NinjaScriptBase)this).Values[16], null, RollingFillColorCore, RollingFillOpacityCore);
			}
			if (RollingFillOpacity12 > 0)
			{
				Draw.Region((NinjaScriptBase)(object)this, "RollR_12U", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[15], (ISeries<double>)(object)((NinjaScriptBase)this).Values[17], null, RollingFillColor12, RollingFillOpacity12);
				Draw.Region((NinjaScriptBase)(object)this, "RollR_12D", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[16], (ISeries<double>)(object)((NinjaScriptBase)this).Values[18], null, RollingFillColor12, RollingFillOpacity12);
			}
			if (RollingFillOpacity23 > 0)
			{
				Draw.Region((NinjaScriptBase)(object)this, "RollR_23U", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[17], (ISeries<double>)(object)((NinjaScriptBase)this).Values[19], null, RollingFillColor23, RollingFillOpacity23);
				Draw.Region((NinjaScriptBase)(object)this, "RollR_23D", ((NinjaScriptBase)this).CurrentBar, 0, (ISeries<double>)(object)((NinjaScriptBase)this).Values[18], (ISeries<double>)(object)((NinjaScriptBase)this).Values[20], null, RollingFillColor23, RollingFillOpacity23);
			}
		}
	}
}

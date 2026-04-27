#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Windows.Media;
using System.Xml.Serialization;

using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;

using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class OrcaPrints : Indicator
	{
		private List<OrcaPrintTick> tickBuffer;
		private List<PrintEvent> printEvents;
		private Dictionary<string, DateTime> clusterCooldowns;
		private ReaderWriterLockSlim printLock;
		private double currentBid = double.NaN;
		private double currentAsk = double.NaN;
		private int lastSessionResetBar = -1;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "OrcaPrints";
				Description = "Detects standalone large prints and clustered aggressive participation. Tick Replay must be enabled on the data series for accurate historical replay.";
				Calculate = Calculate.OnEachTick;
				IsOverlay = true;
				DisplayInDataBox = false;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = false;
				IsSuspendedWhileInactive = true;

				MinTradeSize = 5;
				ResetOnNewSession = true;

				EnableSinglePrints = true;
				SinglePrintMinSize = 100;

				EnableClusters = true;
				ClusterTimeWindowSec = 3.0;
				ClusterMinVolume = 500;
				ClusterMaxPriceTicks = 4;
				MinAggressorPercent = 70;
				ClusterCooldownSec = 1.0;

				ParentConfidenceMode = NinjaTrader.NinjaScript.Indicators.ParentConfidenceMode.Score;
				MinParentConfidence = 60;
				WeightAggressorConsistency = 0.30;
				WeightSizeUniformity = 0.25;
				WeightPriceTightness = 0.20;
				WeightTimingRegularity = 0.25;

				MinDotSize = 6;
				MaxDotSize = 30;
				DotSizeScale = NinjaTrader.NinjaScript.Indicators.DotSizeScale.Logarithmic;
				BuyAggressorColor = WpfBrushes.LimeGreen;
				SellAggressorColor = WpfBrushes.OrangeRed;
				UseVariableIntensity = true;
				MinIntensityPct = 35;
				BorderEnabled = true;
				BorderColor = WpfBrushes.Black;
				TransparencyPct = 20;
				ShapeMode = NinjaTrader.NinjaScript.Indicators.ShapeMode.DistinguishClusters;
			}
			else if (State == State.DataLoaded)
			{
				InitializeOrcaPrintsEngine();
				InitializeOrcaPrintsRendering();
			}
			else if (State == State.Historical)
			{
				AttachOrcaPrintsMouseHandlers();
			}
			else if (State == State.Terminated)
			{
				DetachOrcaPrintsMouseHandlers();
				TerminateOrcaPrintsEngine();
				DisposeDxBrushCache();
			}
		}

		protected override void OnBarUpdate()
		{
			if (!ResetOnNewSession || CurrentBar < 0 || Bars == null)
				return;

			if (Bars.IsFirstBarOfSession && lastSessionResetBar != CurrentBar)
			{
				lastSessionResetBar = CurrentBar;
				ClearOrcaPrintsState();
			}
		}

		#region 01. General
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Min Trade Size", Order = 1, GroupName = "01. General")]
		public int MinTradeSize { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Reset On New Session", Order = 2, GroupName = "01. General")]
		public bool ResetOnNewSession { get; set; }
		#endregion

		#region 02. Single Prints
		[NinjaScriptProperty]
		[Display(Name = "Enable Single Prints", Order = 1, GroupName = "02. Single Prints")]
		public bool EnableSinglePrints { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Single Print Min Size", Order = 2, GroupName = "02. Single Prints")]
		public int SinglePrintMinSize { get; set; }
		#endregion

		#region 03. Clusters
		[NinjaScriptProperty]
		[Display(Name = "Enable Clusters", Order = 1, GroupName = "03. Clusters")]
		public bool EnableClusters { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 60.0)]
		[Display(Name = "Cluster Time Window Sec", Order = 2, GroupName = "03. Clusters")]
		public double ClusterTimeWindowSec { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000000000)]
		[Display(Name = "Cluster Min Volume", Order = 3, GroupName = "03. Clusters")]
		public long ClusterMinVolume { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "Cluster Max Price Ticks", Order = 4, GroupName = "03. Clusters")]
		public int ClusterMaxPriceTicks { get; set; }

		[NinjaScriptProperty]
		[Range(50, 100)]
		[Display(Name = "Min Aggressor Percent", Order = 5, GroupName = "03. Clusters")]
		public int MinAggressorPercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 60.0)]
		[Display(Name = "Cluster Cooldown Sec", Order = 6, GroupName = "03. Clusters")]
		public double ClusterCooldownSec { get; set; }
		#endregion

		#region 04. Parent Confidence
		[NinjaScriptProperty]
		[Display(Name = "Parent Confidence Mode", Order = 1, GroupName = "04. Parent Confidence")]
		public ParentConfidenceMode ParentConfidenceMode { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Min Parent Confidence", Order = 2, GroupName = "04. Parent Confidence")]
		public int MinParentConfidence { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Weight Aggressor Consistency", Order = 3, GroupName = "04. Parent Confidence")]
		public double WeightAggressorConsistency { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Weight Size Uniformity", Order = 4, GroupName = "04. Parent Confidence")]
		public double WeightSizeUniformity { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Weight Price Tightness", Order = 5, GroupName = "04. Parent Confidence")]
		public double WeightPriceTightness { get; set; }

		[NinjaScriptProperty]
		[Range(0.0, 1.0)]
		[Display(Name = "Weight Timing Regularity", Order = 6, GroupName = "04. Parent Confidence")]
		public double WeightTimingRegularity { get; set; }
		#endregion

		#region 05. Rendering
		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "Min Dot Size", Order = 1, GroupName = "05. Rendering")]
		public int MinDotSize { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Max Dot Size", Order = 2, GroupName = "05. Rendering")]
		public int MaxDotSize { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Dot Size Scale", Order = 3, GroupName = "05. Rendering")]
		public DotSizeScale DotSizeScale { get; set; }

		[XmlIgnore]
		[Display(Name = "Buy Aggressor Color", Order = 4, GroupName = "05. Rendering")]
		public WpfBrush BuyAggressorColor { get; set; }

		[Browsable(false)]
		public string BuyAggressorColorSerializable
		{
			get { return Serialize.BrushToString(BuyAggressorColor); }
			set { BuyAggressorColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Sell Aggressor Color", Order = 5, GroupName = "05. Rendering")]
		public WpfBrush SellAggressorColor { get; set; }

		[Browsable(false)]
		public string SellAggressorColorSerializable
		{
			get { return Serialize.BrushToString(SellAggressorColor); }
			set { SellAggressorColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Display(Name = "Use Variable Intensity", Order = 6, GroupName = "05. Rendering")]
		public bool UseVariableIntensity { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Min Intensity Pct", Order = 7, GroupName = "05. Rendering")]
		public int MinIntensityPct { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Border Enabled", Order = 8, GroupName = "05. Rendering")]
		public bool BorderEnabled { get; set; }

		[XmlIgnore]
		[Display(Name = "Border Color", Order = 9, GroupName = "05. Rendering")]
		public WpfBrush BorderColor { get; set; }

		[Browsable(false)]
		public string BorderColorSerializable
		{
			get { return Serialize.BrushToString(BorderColor); }
			set { BorderColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 99)]
		[Display(Name = "Transparency Pct", Order = 10, GroupName = "05. Rendering")]
		public int TransparencyPct { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Shape Mode", Order = 11, GroupName = "05. Rendering")]
		public ShapeMode ShapeMode { get; set; }
		#endregion
	}
}

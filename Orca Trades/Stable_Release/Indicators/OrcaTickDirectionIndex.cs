#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
#endregion

namespace NinjaTrader.NinjaScript
{
	public enum TDIDisplayMode
	{
		CumulativeLine,
		BarHistogram,
		RatioLine
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrcaTickDirectionIndex : Indicator
	{
		private double	prevLast;
		private double	runningTickDelta;
		private int		lastPrimaryBarProcessed;

		private List<double>	barTickDelta;
		private List<double>	barCumTickDelta;
		private List<double>	barCumOpen;
		private List<double>	barCumHigh;
		private List<double>	barCumLow;
		private List<double>	barUptickVol;
		private List<double>	barDowntickVol;
		private List<double>	barUnchangedVol;
		private List<int>		barUnchangedCount;
		private List<bool>		barHasData;
		private List<bool>		barCumFirstTick;

		private SharpDX.Direct2D1.Brush	dxUpBrush;
		private SharpDX.Direct2D1.Brush	dxDownBrush;
		private SharpDX.Direct2D1.Brush	dxUpBorderBrush;
		private SharpDX.Direct2D1.Brush	dxDownBorderBrush;
		private SharpDX.Direct2D1.Brush	dxZeroBrush;
		private SharpDX.Direct2D1.Brush	dxNeutralBrush;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name						= "OrcaTickDirectionIndex";
				Description					= "Tick Direction Index (Rewarded Effort Index) — tracks volume classified by tick direction.";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= false;
				DrawOnPricePanel			= false;
				DisplayInDataBox			= true;
				IsSuspendedWhileInactive	= true;
				BarsRequiredToPlot			= 0;
				Mode				= TDIDisplayMode.BarHistogram;
				ResetOnSession		= true;
				ColorUp				= Brushes.DodgerBlue;
				ColorDown			= Brushes.Tomato;
				ColorUpBorder		= Brushes.DodgerBlue;
				ColorDownBorder		= Brushes.Tomato;
				NeutralColor		= Brushes.Gray;
				BarOpacity			= 0.5;
				BarWidthPercent		= 90;
				ZeroLineColor		= Brushes.DimGray;
				ZeroLineWidth		= 1;
				AddPlot(new Stroke(Brushes.DimGray, 1), PlotStyle.Line, "TickDelta");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Tick, 1);
			}
			else if (State == State.DataLoaded)
			{
				barTickDelta		= new List<double>(4096);
				barCumTickDelta		= new List<double>(4096);
				barCumOpen			= new List<double>(4096);
				barCumHigh			= new List<double>(4096);
				barCumLow			= new List<double>(4096);
				barUptickVol		= new List<double>(4096);
				barDowntickVol		= new List<double>(4096);
				barUnchangedVol		= new List<double>(4096);
				barUnchangedCount	= new List<int>(4096);
				barHasData			= new List<bool>(4096);
				barCumFirstTick		= new List<bool>(4096);
				prevLast			= double.NaN;
				runningTickDelta	= 0;
				lastPrimaryBarProcessed = -1;
			}
			else if (State == State.Terminated)
			{
				DisposeDxResources();
			}
		}

		private void EnsureBarLists(int idx)
		{
			while (barTickDelta.Count <= idx)
			{
				barTickDelta.Add(0);
				barCumTickDelta.Add(0);
				barCumOpen.Add(0);
				barCumHigh.Add(0);
				barCumLow.Add(0);
				barUptickVol.Add(0);
				barDowntickVol.Add(0);
				barUnchangedVol.Add(0);
				barUnchangedCount.Add(0);
				barHasData.Add(false);
				barCumFirstTick.Add(false);
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 1)
			{
				double price = Close[0];
				long   vol   = (long)Volume[0];
				if (vol <= 0) return;
				int primaryIdx = BarsArray[0].GetBar(Time[0]);
				if (primaryIdx < 0) return;
				EnsureBarLists(primaryIdx);
				if (primaryIdx != lastPrimaryBarProcessed) lastPrimaryBarProcessed = primaryIdx;

				long signed = 0;
				if (!double.IsNaN(prevLast))
				{
					if (price > prevLast) signed = +vol;
					else if (price < prevLast) signed = -vol;
				}
				prevLast = price;

				if (signed == 0)
				{
					barUnchangedVol[primaryIdx]   += vol;
					barUnchangedCount[primaryIdx] += 1;
					barHasData[primaryIdx] = true;
					return;
				}

				if (signed > 0) barUptickVol[primaryIdx] += vol;
				else barDowntickVol[primaryIdx] += vol;

				barTickDelta[primaryIdx] += signed;
				runningTickDelta += signed;
				barCumTickDelta[primaryIdx] = runningTickDelta;

				if (!barCumFirstTick[primaryIdx])
				{
					barCumOpen[primaryIdx] = runningTickDelta;
					barCumHigh[primaryIdx] = runningTickDelta;
					barCumLow[primaryIdx]  = runningTickDelta;
					barCumFirstTick[primaryIdx] = true;
				}
				else
				{
					if (runningTickDelta > barCumHigh[primaryIdx]) barCumHigh[primaryIdx] = runningTickDelta;
					if (runningTickDelta < barCumLow[primaryIdx]) barCumLow[primaryIdx] = runningTickDelta;
				}
				barHasData[primaryIdx] = true;
				return;
			}

			if (BarsInProgress != 0 || CurrentBar < 0) return;
			EnsureBarLists(CurrentBar);
			if (ResetOnSession && Bars.IsFirstBarOfSession) { runningTickDelta = 0; prevLast = double.NaN; }

			if (CurrentBar < barHasData.Count && barHasData[CurrentBar])
			{
				switch (Mode)
				{
					case TDIDisplayMode.CumulativeLine: Value[0] = barCumTickDelta[CurrentBar]; break;
					case TDIDisplayMode.BarHistogram: Value[0] = barTickDelta[CurrentBar]; break;
					case TDIDisplayMode.RatioLine:
						double up = barUptickVol[CurrentBar];
						double down = barDowntickVol[CurrentBar];
						Value[0] = (up + down) > 0 ? up / (up + down) : 0.5;
						break;
				}
			}
			else Value[0] = double.NaN;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (chartControl == null || chartScale == null || barTickDelta == null) return;
			int fromIdx = ChartBars.FromIndex;
			int toIdx   = ChartBars.ToIndex;
			if (fromIdx < 0 || toIdx < 0 || fromIdx > toIdx) return;

			EnsureDxResources();
			if (dxUpBrush == null) return;

			SharpDX.Direct2D1.AntialiasMode oldMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;

			float panelX = ChartPanel.X;
			float panelW = ChartPanel.W;
			float panelY = ChartPanel.Y;
			float panelH = ChartPanel.H;

			double refValue = (Mode == TDIDisplayMode.RatioLine) ? 0.5 : 0.0;
			float refY = chartScale.GetYByValue(refValue);
			if (refY >= panelY && refY <= panelY + panelH)
				RenderTarget.DrawLine(new Vector2(panelX, refY), new Vector2(panelX + panelW, refY), dxZeroBrush, ZeroLineWidth);

			if (Mode == TDIDisplayMode.BarHistogram || Mode == TDIDisplayMode.CumulativeLine)
			{
				for (int barIdx = fromIdx; barIdx <= toIdx; barIdx++)
				{
					if (barIdx < 0 || barIdx >= barTickDelta.Count || !barHasData[barIdx]) continue;
					float barX = chartControl.GetXByBarIndex(ChartBars, barIdx);
					float barSpacing = (barIdx < toIdx) ? (chartControl.GetXByBarIndex(ChartBars, barIdx + 1) - barX) : ((barIdx > fromIdx) ? (barX - chartControl.GetXByBarIndex(ChartBars, barIdx - 1)) : (float)chartControl.BarWidth);
					float halfW = Math.Max(1f, (float)(barSpacing * BarWidthPercent / 100.0 / 2.0));

					if (Mode == TDIDisplayMode.BarHistogram)
					{
						double val = barTickDelta[barIdx];
						if (val == 0) continue;
						float yVal = chartScale.GetYByValue(val);
						float yZero = chartScale.GetYByValue(0);
						SharpDX.Direct2D1.Brush fillBrush = val > 0 ? dxUpBrush : dxDownBrush;
						float bTop = Math.Min(yVal, yZero);
						float bH = Math.Max(1f, Math.Max(yVal, yZero) - bTop);
						var barRect = new RectangleF(barX - halfW, bTop, halfW * 2, bH);
						RenderTarget.FillRectangle(barRect, fillBrush);
						RenderTarget.DrawRectangle(barRect, fillBrush, 1f);
					}
					else
					{
						if (!barCumFirstTick[barIdx]) continue;
						float yOpen = chartScale.GetYByValue(barCumOpen[barIdx]);
						float yHigh = chartScale.GetYByValue(barCumHigh[barIdx]);
						float yLow = chartScale.GetYByValue(barCumLow[barIdx]);
						float yClose = chartScale.GetYByValue(barCumTickDelta[barIdx]);
						bool isUp = barCumTickDelta[barIdx] >= barCumOpen[barIdx];
						SharpDX.Direct2D1.Brush fillBrush = isUp ? dxUpBrush : dxDownBrush;
						SharpDX.Direct2D1.Brush borderBrush = isUp ? dxUpBorderBrush : dxDownBorderBrush;
						float bTop = Math.Min(yOpen, yClose);
						float bH = Math.Max(1f, Math.Max(yOpen, yClose) - bTop);
						if (yHigh < bTop) RenderTarget.DrawLine(new Vector2(barX, yHigh), new Vector2(barX, bTop), borderBrush, 1f);
						if (yLow > bTop + bH) RenderTarget.DrawLine(new Vector2(barX, bTop + bH), new Vector2(barX, yLow), borderBrush, 1f);
						var bodyRect = new RectangleF(barX - halfW, bTop, halfW * 2, bH);
						RenderTarget.FillRectangle(bodyRect, fillBrush);
						RenderTarget.DrawRectangle(bodyRect, borderBrush, 1f);
					}
				}
			}
			RenderTarget.AntialiasMode = oldMode;
		}

		private void EnsureDxResources()
		{
			if (RenderTarget == null || dxUpBrush != null) return;
			float opacity = (float)Math.Max(0.0, Math.Min(1.0, BarOpacity));
			dxUpBrush = CreateSolidBrush(ColorUp, opacity);
			dxDownBrush = CreateSolidBrush(ColorDown, opacity);
			dxUpBorderBrush = CreateSolidBrush(ColorUpBorder, 1.0f);
			dxDownBorderBrush = CreateSolidBrush(ColorDownBorder, 1.0f);
			dxZeroBrush = CreateSolidBrush(ZeroLineColor, 1.0f);
			dxNeutralBrush = CreateSolidBrush(NeutralColor, 1.0f);
		}

		private SharpDX.Direct2D1.Brush CreateSolidBrush(System.Windows.Media.Brush wpfBrush, float opacity)
		{
			var color = (wpfBrush as System.Windows.Media.SolidColorBrush)?.Color ?? System.Windows.Media.Colors.White;
			return new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(color.R / 255f, color.G / 255f, color.B / 255f, (color.A / 255f) * opacity));
		}

		private void DisposeDxResources()
		{
			if (dxUpBrush != null) { dxUpBrush.Dispose(); dxUpBrush = null; }
			if (dxDownBrush != null) { dxDownBrush.Dispose(); dxDownBrush = null; }
			if (dxUpBorderBrush != null) { dxUpBorderBrush.Dispose(); dxUpBorderBrush = null; }
			if (dxDownBorderBrush != null) { dxDownBorderBrush.Dispose(); dxDownBorderBrush = null; }
			if (dxZeroBrush != null) { dxZeroBrush.Dispose(); dxZeroBrush = null; }
			if (dxNeutralBrush != null) { dxNeutralBrush.Dispose(); dxNeutralBrush = null; }
		}

		public override void OnRenderTargetChanged() { DisposeDxResources(); base.OnRenderTargetChanged(); }

		[Display(Name = "Display Mode", Order = 0, GroupName = "Parameters")]
		public TDIDisplayMode Mode { get; set; }

		[Display(Name = "Reset on Session", Order = 1, GroupName = "Parameters")]
		public bool ResetOnSession { get; set; }

		[XmlIgnore]
		[Display(Name = "Color Up", Order = 1, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorUp { get; set; }
		[Browsable(false)]
		public string ColorUpSerialize { get { return Serialize.BrushToString(ColorUp); } set { ColorUp = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Color Down", Order = 2, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorDown { get; set; }
		[Browsable(false)]
		public string ColorDownSerialize { get { return Serialize.BrushToString(ColorDown); } set { ColorDown = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Color Up Border", Order = 3, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorUpBorder { get; set; }
		[Browsable(false)]
		public string ColorUpBorderSerialize { get { return Serialize.BrushToString(ColorUpBorder); } set { ColorUpBorder = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Color Down Border", Order = 4, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush ColorDownBorder { get; set; }
		[Browsable(false)]
		public string ColorDownBorderSerialize { get { return Serialize.BrushToString(ColorDownBorder); } set { ColorDownBorder = Serialize.StringToBrush(value); } }

		[XmlIgnore]
		[Display(Name = "Neutral Color", Order = 5, GroupName = "Visual Parameters")]
		public System.Windows.Media.Brush NeutralColor { get; set; }
		[Browsable(false)]
		public string NeutralColorSerialize { get { return Serialize.BrushToString(NeutralColor); } set { NeutralColor = Serialize.StringToBrush(value); } }

		[Range(0.0, 1.0)]
		[Display(Name = "Bar Opacity", Order = 6, GroupName = "Visual Parameters")]
		public double BarOpacity { get; set; }

		[Range(1, 100)]
		[Display(Name = "Bar Width %", Order = 7, GroupName = "Visual Parameters")]
		public int BarWidthPercent { get; set; }

		[XmlIgnore]
		[Display(Name = "Zero Line Color", Order = 1, GroupName = "Reference Levels")]
		public System.Windows.Media.Brush ZeroLineColor { get; set; }
		[Browsable(false)]
		public string ZeroLineColorSerialize { get { return Serialize.BrushToString(ZeroLineColor); } set { ZeroLineColor = Serialize.StringToBrush(value); } }

		[Range(1, 5)]
		[Display(Name = "Zero Line Width", Order = 2, GroupName = "Reference Levels")]
		public int ZeroLineWidth { get; set; }
	}
}

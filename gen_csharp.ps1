$csharp_code = @"
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;

public enum RollingVwapPeriod 
{
	Min15 = 15,
	Min30 = 30,
	Hour1 = 60,
	Hour2 = 120,
	Hour4 = 240,
	Hour8 = 480,
	Day1 = 1440,
	Day5 = 7200,
	Day20 = 28800
}

namespace NinjaTrader.NinjaScript.Indicators
{

	public class OrcaVwapSession 
	{
		public double SumVol;
		public double SumPriceVol;
		public double SumPrice2Vol;

		public void Add(double price, double vol) 
		{
			SumVol += vol;
			SumPriceVol += price * vol;
			SumPrice2Vol += price * price * vol;
		}

		public void Reset() 
		{
			SumVol = 0;
			SumPriceVol = 0;
			SumPrice2Vol = 0;
		}

		public double Vwap => SumVol > 0 ? SumPriceVol / SumVol : 0;
		public double MathVariance => SumVol > 0 ? Math.Max(0, (SumPrice2Vol / SumVol) - (Vwap * Vwap)) : 0;
		public double StdDev => Math.Sqrt(MathVariance);
	}

	public class OrcaVwapBucket 
	{
		public double SumVol;
		public double SumPriceVol;
		public double SumPrice2Vol;
	}

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

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "All-in-one Anchored Time and Rolling VWAP with Deviation bands.";
				Name = "Orca Time VWAPs";
				Calculate = Calculate.OnPriceChange;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive = true;

"@

# Now use powershell to build the repetitious parts
$groups = @(
	@{ Prefix="Globex"; Name="1. Globex VWAP"; Time="18:00:00"; Color="DodgerBlue"; HasTime=$true },
	@{ Prefix="Rth"; Name="2. RTH VWAP"; Time="09:30:00"; Color="Orange"; HasTime=$true },
	@{ Prefix="Rolling"; Name="3. Rolling VWAP"; Time=""; Color="LimeGreen"; HasTime=$false },
	@{ Prefix="Weekly"; Name="4. Weekly VWAP"; Time="18:00:00"; Color="Plum"; HasTime=$true }
)

foreach ($g in $groups) {
	$p = $g.Prefix
	$csharp_code += "`r`n`t`t`t`t// $($g.Prefix) Defaults`r`n"
	$csharp_code += "`t`t`t`t$( $p )ShowVWAP = true;`r`n"
	if ($g.HasTime) {
		$timeParts = $g.Time.Split(":")
		$csharp_code += "`t`t`t`t$( $p )StartTime = new TimeSpan($($timeParts[0]), $($timeParts[1]), $($timeParts[2]));`r`n"
	} elseif ($p -eq "Rolling") {
		$csharp_code += "`t`t`t`tRollingPeriod = RollingVwapPeriod.Day1;`r`n"
	}

	$csharp_code += "`t`t`t`t$( $p )ShowDev1 = true;`r`n"
	$csharp_code += "`t`t`t`t$( $p )Dev1Mult = 1.0;`r`n"
	$csharp_code += "`t`t`t`t$( $p )ShowDev2 = true;`r`n"
	$csharp_code += "`t`t`t`t$( $p )Dev2Mult = 2.0;`r`n"
	$csharp_code += "`t`t`t`t$( $p )ShowDev3 = true;`r`n"
	$csharp_code += "`t`t`t`t$( $p )Dev3Mult = 3.0;`r`n"

	$csharp_code += "`t`t`t`t$( $p )FillColorCore = Brushes.$($g.Color);`r`n"
	$csharp_code += "`t`t`t`t$( $p )FillOpacityCore = 0;`r`n"
	$csharp_code += "`t`t`t`t$( $p )FillColor12 = Brushes.$($g.Color);`r`n"
	$csharp_code += "`t`t`t`t$( $p )FillOpacity12 = 0;`r`n"
	$csharp_code += "`t`t`t`t$( $p )FillColor23 = Brushes.$($g.Color);`r`n"
	$csharp_code += "`t`t`t`t$( $p )FillOpacity23 = 0;`r`n"
}

$csharp_code += "`r`n`t`t`t`t// === Plots ===`r`n"

for ($i=0; $i -lt $groups.Length; $i++) {
	$g = $groups[$i]
	$p = $g.Prefix
	$idx = $i * 7
	$csharp_code += "`t`t`t`t// $idx - $($idx+6): $p`r`n"
	$csharp_code += "`t`t`t`tAddPlot(new Stroke(Brushes.$($g.Color), DashStyleHelper.Solid, 2), PlotStyle.Line, `"$p VWAP`");`r`n"
	$csharp_code += "`t`t`t`tAddPlot(new Stroke(Brushes.$($g.Color), DashStyleHelper.Dash, 1), PlotStyle.Line, `"$p Dev 1 Upper`");`r`n"
	$csharp_code += "`t`t`t`tAddPlot(new Stroke(Brushes.$($g.Color), DashStyleHelper.Dash, 1), PlotStyle.Line, `"$p Dev 1 Lower`");`r`n"
	$csharp_code += "`t`t`t`tAddPlot(new Stroke(Brushes.$($g.Color), DashStyleHelper.Dot, 1), PlotStyle.Line, `"$p Dev 2 Upper`");`r`n"
	$csharp_code += "`t`t`t`tAddPlot(new Stroke(Brushes.$($g.Color), DashStyleHelper.Dot, 1), PlotStyle.Line, `"$p Dev 2 Lower`");`r`n"
	$csharp_code += "`t`t`t`tAddPlot(new Stroke(Brushes.$($g.Color), DashStyleHelper.DashDot, 1), PlotStyle.Line, `"$p Dev 3 Upper`");`r`n"
	$csharp_code += "`t`t`t`tAddPlot(new Stroke(Brushes.$($g.Color), DashStyleHelper.DashDot, 1), PlotStyle.Line, `"$p Dev 3 Lower`");`r`n"
}

$csharp_code += @"
			}
			else if (State == State.Configure)
			{
				globexSession = new OrcaVwapSession();
				rthSession = new OrcaVwapSession();
				weeklySession = new OrcaVwapSession();
				rollingHistory = new Queue<OrcaVwapBucket>();
				rollingDeveloping = new OrcaVwapBucket();
				rollingTotal = new OrcaVwapSession();
				currentMinuteToken = DateTime.MinValue;
				lastBarVolume = 0;
				lastBarIndex = -1;
			}
		}

		private bool CrossedTime(DateTime start, DateTime end, TimeSpan target)
		{
			if (start >= end) return false;
			
			DateTime targetTime;
			if (end.TimeOfDay >= target) 
			{
				targetTime = end.Date + target;
			}
			else 
			{
				targetTime = end.Date.AddDays(-1) + target;
			}
			
			return start < targetTime && end >= targetTime;
		}

		private bool CrossedWeekly(DateTime start, DateTime end, TimeSpan target)
		{
			if (start >= end) return false;
			
			// Get Sunday of the end date's week (or previous week if we are early Sunday)
			int diff = (int)end.DayOfWeek - (int)DayOfWeek.Sunday;
			if (diff < 0) diff += 7;
			DateTime sunday = end.Date.AddDays(-diff);
			
			// If we are before the target time on Sunday, the weekly anchor is the PREVIOUS Sunday
			if (end.DayOfWeek == DayOfWeek.Sunday && end.TimeOfDay < target)
			{
				sunday = sunday.AddDays(-7);
			}
			
			DateTime targetTime = sunday.Date + target;
			return start < targetTime && end >= targetTime;
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 0) return;

			double currentVolume = Volume[0];
			double tickVol = 0;

			if (CurrentBar != lastBarIndex) 
			{
				tickVol = currentVolume; // New bar starts handling the entire accumulated volume
			}
			else 
			{
				tickVol = currentVolume - lastBarVolume; // Intra-bar tick volume
			}
			
			lastBarVolume = currentVolume;
			lastBarIndex = CurrentBar;

			if (tickVol <= 0) return;

			double price = (State == State.Historical) ? Typical[0] : Close[0];
			DateTime time0 = Time[0];
			DateTime time1 = (CurrentBar > 0) ? Time[1] : time0;

"@

$timeSects = @(
	@{ Prefix="Globex"; Offset=0 },
	@{ Prefix="Rth"; Offset=7 },
	@{ Prefix="Weekly"; Offset=21 }
)

foreach ($g in $timeSects) {
	$p = $g.Prefix
	$sess = $p.ToLower() + "Session"
	$method = if ($p -eq "Weekly") { "CrossedWeekly" } else { "CrossedTime" }
	$offset = $g.Offset

	$csharp_code += "`r`n`t`t`t// --- $p VWAP ---`r`n"
	$csharp_code += "`t`t`tif ($( $p )ShowVWAP)`r`n`t`t`t{`r`n"
	$csharp_code += "`t`t`t`tif ($method(time1, time0, $( $p )StartTime)) $sess.Reset();`r`n"
	$csharp_code += "`t`t`t`t$sess.Add(price, tickVol);`r`n"
	$csharp_code += "`t`t`t`tif ($sess.SumVol > 0)`r`n`t`t`t`t{`r`n"
	$csharp_code += "`t`t`t`t`tdouble vwap = $sess.Vwap;`r`n"
	$csharp_code += "`t`t`t`t`tdouble sd = $sess.StdDev;`r`n"
	$csharp_code += "`t`t`t`t`tValues[$offset][0] = vwap;`r`n"
	$csharp_code += "`t`t`t`t`tif ($( $p )ShowDev1) { Values[$($offset+1)][0] = vwap + sd * $( $p )Dev1Mult; Values[$($offset+2)][0] = vwap - sd * $( $p )Dev1Mult; }`r`n"
	$csharp_code += "`t`t`t`t`tif ($( $p )ShowDev2) { Values[$($offset+3)][0] = vwap + sd * $( $p )Dev2Mult; Values[$($offset+4)][0] = vwap - sd * $( $p )Dev2Mult; }`r`n"
	$csharp_code += "`t`t`t`t`tif ($( $p )ShowDev3) { Values[$($offset+5)][0] = vwap + sd * $( $p )Dev3Mult; Values[$($offset+6)][0] = vwap - sd * $( $p )Dev3Mult; }`r`n"

	$csharp_code += "`t`t`t`t`t// Draw Regions`r`n"
	$csharp_code += "`t`t`t`t`tif ($( $p )FillOpacityCore > 0) {`r`n"
	$csharp_code += "`t`t`t`t`t`tDraw.Region(this, `"$($p)R_CoreU`", CurrentBar, 0, Values[$offset], Values[$($offset+1)], null, $( $p )FillColorCore, $( $p )FillOpacityCore);`r`n"
	$csharp_code += "`t`t`t`t`t`tDraw.Region(this, `"$($p)R_CoreD`", CurrentBar, 0, Values[$offset], Values[$($offset+2)], null, $( $p )FillColorCore, $( $p )FillOpacityCore);`r`n"
	$csharp_code += "`t`t`t`t`t}`r`n"

	$csharp_code += "`t`t`t`t`tif ($( $p )FillOpacity12 > 0) {`r`n"
	$csharp_code += "`t`t`t`t`t`tDraw.Region(this, `"$($p)R_12U`", CurrentBar, 0, Values[$($offset+1)], Values[$($offset+3)], null, $( $p )FillColor12, $( $p )FillOpacity12);`r`n"
	$csharp_code += "`t`t`t`t`t`tDraw.Region(this, `"$($p)R_12D`", CurrentBar, 0, Values[$($offset+2)], Values[$($offset+4)], null, $( $p )FillColor12, $( $p )FillOpacity12);`r`n"
	$csharp_code += "`t`t`t`t`t}`r`n"

	$csharp_code += "`t`t`t`t`tif ($( $p )FillOpacity23 > 0) {`r`n"
	$csharp_code += "`t`t`t`t`t`tDraw.Region(this, `"$($p)R_23U`", CurrentBar, 0, Values[$($offset+3)], Values[$($offset+5)], null, $( $p )FillColor23, $( $p )FillOpacity23);`r`n"
	$csharp_code += "`t`t`t`t`t`tDraw.Region(this, `"$($p)R_23D`", CurrentBar, 0, Values[$($offset+4)], Values[$($offset+6)], null, $( $p )FillColor23, $( $p )FillOpacity23);`r`n"
	$csharp_code += "`t`t`t`t`t}`r`n"
	
	$csharp_code += "`t`t`t`t}`r`n"
	$csharp_code += "`t`t`t}`r`n"
}

$csharp_code += @"
			// --- 3. ROLLING VWAP ---
			if (RollingShowVWAP)
			{
				DateTime minuteToken = new DateTime(time0.Year, time0.Month, time0.Day, time0.Hour, time0.Minute, 0);
				if (minuteToken > currentMinuteToken) 
				{
					if (currentMinuteToken != DateTime.MinValue) 
					{
						rollingHistory.Enqueue(rollingDeveloping);
						
						int maxBuckets = (int)RollingPeriod;
						int missedMinutes = (int)(minuteToken - currentMinuteToken).TotalMinutes;

						if (missedMinutes > 1 && missedMinutes <= 720) 
						{
							int emptyBuckets = Math.Min(missedMinutes - 1, maxBuckets);
							for (int i = 0; i < emptyBuckets; i++) 
							{
								rollingHistory.Enqueue(new OrcaVwapBucket());
							}
						}

						rollingDeveloping = new OrcaVwapBucket();
						
						while (rollingHistory.Count >= maxBuckets) rollingHistory.Dequeue();

						rollingTotal.Reset();
						foreach(var b in rollingHistory) 
						{
							rollingTotal.SumVol += b.SumVol;
							rollingTotal.SumPriceVol += b.SumPriceVol;
							rollingTotal.SumPrice2Vol += b.SumPrice2Vol;
						}
					}
					currentMinuteToken = minuteToken;
				}
				else if (minuteToken < currentMinuteToken)
				{
					// Defensive reset if data reloads or time unexpectedly jumps backward
					rollingHistory.Clear();
					rollingDeveloping = new OrcaVwapBucket();
					rollingTotal.Reset();
					currentMinuteToken = minuteToken;
				}

				rollingDeveloping.SumVol += tickVol;
				rollingDeveloping.SumPriceVol += price * tickVol;
				rollingDeveloping.SumPrice2Vol += price * price * tickVol;

				double currentSumVol = rollingTotal.SumVol + rollingDeveloping.SumVol;
				if (currentSumVol > 0) 
				{
					double currentSumPriceVol = rollingTotal.SumPriceVol + rollingDeveloping.SumPriceVol;
					double currentSumPrice2Vol = rollingTotal.SumPrice2Vol + rollingDeveloping.SumPrice2Vol;

					double vwap = currentSumPriceVol / currentSumVol;
					double variance = Math.Max(0, (currentSumPrice2Vol / currentSumVol) - (vwap * vwap));
					double sd = Math.Sqrt(variance);

					Values[14][0] = vwap;
					if (RollingShowDev1) { Values[15][0] = vwap + sd * RollingDev1Mult; Values[16][0] = vwap - sd * RollingDev1Mult; }
					if (RollingShowDev2) { Values[17][0] = vwap + sd * RollingDev2Mult; Values[18][0] = vwap - sd * RollingDev2Mult; }
					if (RollingShowDev3) { Values[19][0] = vwap + sd * RollingDev3Mult; Values[20][0] = vwap - sd * RollingDev3Mult; }

					if (RollingFillOpacityCore > 0) {
						Draw.Region(this, "RollR_CoreU", CurrentBar, 0, Values[14], Values[15], null, RollingFillColorCore, RollingFillOpacityCore);
						Draw.Region(this, "RollR_CoreD", CurrentBar, 0, Values[14], Values[16], null, RollingFillColorCore, RollingFillOpacityCore);
					}
					if (RollingFillOpacity12 > 0) {
						Draw.Region(this, "RollR_12U", CurrentBar, 0, Values[15], Values[17], null, RollingFillColor12, RollingFillOpacity12);
						Draw.Region(this, "RollR_12D", CurrentBar, 0, Values[16], Values[18], null, RollingFillColor12, RollingFillOpacity12);
					}
					if (RollingFillOpacity23 > 0) {
						Draw.Region(this, "RollR_23U", CurrentBar, 0, Values[17], Values[19], null, RollingFillColor23, RollingFillOpacity23);
						Draw.Region(this, "RollR_23D", CurrentBar, 0, Values[18], Values[20], null, RollingFillColor23, RollingFillOpacity23);
					}
				}
			}
		}

		#region Properties
"@

foreach ($g in $groups) {
	$p = $g.Prefix
	$name = $g.Name
	$csharp_code += "`r`n`t`t// ------------------ $name ------------------`r`n"
	$csharp_code += "`t`t[NinjaScriptProperty]`r`n"
	$csharp_code += "`t`t[Display(Name=`"1. Show VWAP`", Order=1, GroupName=`"$name`")]`r`n"
	$csharp_code += "`t`tpublic bool $( $p )ShowVWAP { get; set; }`r`n`r`n"

	$order = 2
	if ($g.HasTime) {
		$csharp_code += "`t`t[NinjaScriptProperty]`r`n"
		$csharp_code += "`t`t[PropertyEditor(`"NinjaTrader.Gui.Tools.TimeSpanEditorKey`")]`r`n"
		$csharp_code += "`t`t[Display(Name=`"2. Start Time`", Order=$order, GroupName=`"$name`")]`r`n"
		$csharp_code += "`t`tpublic TimeSpan $( $p )StartTime { get; set; }`r`n`r`n"
	} elseif ($p -eq "Rolling") {
		$csharp_code += "`t`t[NinjaScriptProperty]`r`n"
		$csharp_code += "`t`t[Display(Name=`"2. Rolling Period`", Order=$order, GroupName=`"$name`")]`r`n"
		$csharp_code += "`t`tpublic RollingVwapPeriod RollingPeriod { get; set; }`r`n`r`n"
	}

	for ($lvl = 1; $lvl -le 3; $lvl++) {
		$csharp_code += "`t`t[NinjaScriptProperty]`r`n"
		$csharp_code += "`t`t[Display(Name=`"Show Dev $lvl`", Order=$($order + 1), GroupName=`"$name`")]`r`n"
		$csharp_code += "`t`tpublic bool $( $p )ShowDev$lvl { get; set; }`r`n`r`n"

		$csharp_code += "`t`t[NinjaScriptProperty]`r`n"
		$csharp_code += "`t`t[Display(Name=`"Dev $lvl Multiplier`", Order=$($order + 2), GroupName=`"$name`")]`r`n"
		$csharp_code += "`t`tpublic double $( $p )Dev$lvl`Mult { get; set; }`r`n`r`n"
		$order += 2
	}

	$fills = @(
		@{ Label="Core-Dev1"; Suffix="Core" },
		@{ Label="Dev1-Dev2"; Suffix="12" },
		@{ Label="Dev2-Dev3"; Suffix="23" }
	)

	foreach ($f in $fills) {
		$label = $f.Label
		$suffix = $f.Suffix
		
		$csharp_code += "`t`t[XmlIgnore]`r`n"
		$csharp_code += "`t`t[Display(Name=`"$label Fill Color`", Order=$($order + 1), GroupName=`"$name`")]`r`n"
		$csharp_code += "`t`tpublic Brush $( $p )FillColor$suffix { get; set; }`r`n"
		
		$csharp_code += "`t`t[Browsable(false)]`r`n"
		$csharp_code += "`t`tpublic string $( $p )FillColor$suffix`Serializable`r`n"
		$csharp_code += "`t`t{`r`n"
		$csharp_code += "`t`t`tget { return Serialize.BrushToString($( $p )FillColor$suffix); }`r`n"
		$csharp_code += "`t`t`tset { $( $p )FillColor$suffix = Serialize.StringToBrush(value); }`r`n"
		$csharp_code += "`t`t}`r`n"

		$csharp_code += "`t`t[NinjaScriptProperty]`r`n"
		$csharp_code += "`t`t[Range(0, 100)]`r`n"
		$csharp_code += "`t`t[Display(Name=`"$label Fill Opacity (0 = Off)`", Order=$($order + 2), GroupName=`"$name`")]`r`n"
		$csharp_code += "`t`tpublic int $( $p )FillOpacity$suffix { get; set; }`r`n`r`n"
		$order += 2
	}
}

$csharp_code += @"
		#endregion
	}
}
"@

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText("c:\Users\Owner\.gemini\antigravity\scratch\Orca\Orca Time VWAPs\OrcaTimeVWAPs.cs", $csharp_code, $utf8NoBom)

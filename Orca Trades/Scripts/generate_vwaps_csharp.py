import sys

csharp_code = """using System;
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
\tMin15 = 15,
\tMin30 = 30,
\tHour1 = 60,
\tHour2 = 120,
\tHour4 = 240,
\tHour8 = 480,
\tDay1 = 1440,
\tDay5 = 7200,
\tDay20 = 28800
}

namespace NinjaTrader.NinjaScript.Indicators
{

\tpublic class OrcaVwapSession 
\t{
\t\tpublic double SumVol;
\t\tpublic double SumPriceVol;
\t\tpublic double SumPrice2Vol;

\t\tpublic void Add(double price, double vol) 
\t\t{
\t\t\tSumVol += vol;
\t\t\tSumPriceVol += price * vol;
\t\t\tSumPrice2Vol += price * price * vol;
\t\t}

\t\tpublic void Reset() 
\t\t{
\t\t\tSumVol = 0;
\t\t\tSumPriceVol = 0;
\t\t\tSumPrice2Vol = 0;
\t\t}

\t\tpublic double Vwap => SumVol > 0 ? SumPriceVol / SumVol : 0;
\t\tpublic double MathVariance => SumVol > 0 ? Math.Max(0, (SumPrice2Vol / SumVol) - (Vwap * Vwap)) : 0;
\t\tpublic double StdDev => Math.Sqrt(MathVariance);
\t}

\tpublic class OrcaVwapBucket 
\t{
\t\tpublic double SumVol;
\t\tpublic double SumPriceVol;
\t\tpublic double SumPrice2Vol;
\t}

\tpublic class OrcaTimeVWAPs : Indicator
\t{
\t\tprivate OrcaVwapSession globexSession;
\t\tprivate OrcaVwapSession rthSession;
\t\tprivate OrcaVwapSession weeklySession;

\t\t// Rolling Queue State
\t\tprivate Queue<OrcaVwapBucket> rollingHistory;
\t\tprivate OrcaVwapBucket rollingDeveloping;
\t\tprivate OrcaVwapSession rollingTotal;
\t\tprivate DateTime currentMinuteToken;

\t\tprivate double lastBarVolume;
\t\tprivate int lastBarIndex;

\t\tprotected override void OnStateChange()
\t\t{
\t\t\tif (State == State.SetDefaults)
\t\t\t{
\t\t\t\tDescription = "All-in-one Anchored Time and Rolling VWAP with Deviation bands.";
\t\t\t\tName = "Orca Time VWAPs";
\t\t\t\tCalculate = Calculate.OnPriceChange;
\t\t\t\tIsOverlay = true;
\t\t\t\tDisplayInDataBox = true;
\t\t\t\tDrawOnPricePanel = true;
\t\t\t\tDrawHorizontalGridLines = true;
\t\t\t\tDrawVerticalGridLines = true;
\t\t\t\tPaintPriceMarkers = true;
\t\t\t\tScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
\t\t\t\tIsSuspendedWhileInactive = true;
\t\t\t\t
"""

groups = [
    ("Globex", "1. Globex VWAP", "18:00:00", "DodgerBlue", True),
    ("RTH", "2. RTH VWAP", "09:30:00", "Orange", True),
    ("Rolling", "3. Rolling VWAP", "", "LimeGreen", False),
    ("Weekly", "4. Weekly VWAP", "18:00:00", "Plum", True)
]

for g, gname, t, c, has_time in groups:
    csharp_code += f"\n\t\t\t\t// {g} Defaults\n"
    csharp_code += f"\t\t\t\t{g}ShowVWAP = true;\n"
    if has_time:
        h, m, s = [int(x) for x in t.split(':')]
        csharp_code += f"\t\t\t\t{g}StartTime = new TimeSpan({h}, {m}, {s});\n"
    elif g == "Rolling":
        csharp_code += f"\t\t\t\tRollingPeriod = RollingVwapPeriod.Day1;\n"
        
    csharp_code += f"\t\t\t\t{g}ShowDev1 = true;\n"
    csharp_code += f"\t\t\t\t{g}Dev1Mult = 1.0;\n"
    csharp_code += f"\t\t\t\t{g}ShowDev2 = true;\n"
    csharp_code += f"\t\t\t\t{g}Dev2Mult = 2.0;\n"
    csharp_code += f"\t\t\t\t{g}ShowDev3 = true;\n"
    csharp_code += f"\t\t\t\t{g}Dev3Mult = 3.0;\n"
    
    csharp_code += f"\t\t\t\t{g}FillColorCore = Brushes.{c};\n"
    csharp_code += f"\t\t\t\t{g}FillOpacityCore = 0;\n"
    csharp_code += f"\t\t\t\t{g}FillColor12 = Brushes.{c};\n"
    csharp_code += f"\t\t\t\t{g}FillOpacity12 = 0;\n"
    csharp_code += f"\t\t\t\t{g}FillColor23 = Brushes.{c};\n"
    csharp_code += f"\t\t\t\t{g}FillOpacity23 = 0;\n"


csharp_code += "\n\t\t\t\t// === Plots ===\n"

for i, (g, gname, t, c, has_time) in enumerate(groups):
    idx = i * 7
    csharp_code += f"\t\t\t\t// {idx} - {idx+6}: {g}\n"
    csharp_code += f"\t\t\t\tAddPlot(new Stroke(Brushes.{c}, DashStyleHelper.Solid, 2), PlotStyle.Line, \"{g} VWAP\");\n"
    csharp_code += f"\t\t\t\tAddPlot(new Stroke(Brushes.{c}, DashStyleHelper.Dash, 1), PlotStyle.Line, \"{g} Dev 1 Upper\");\n"
    csharp_code += f"\t\t\t\tAddPlot(new Stroke(Brushes.{c}, DashStyleHelper.Dash, 1), PlotStyle.Line, \"{g} Dev 1 Lower\");\n"
    csharp_code += f"\t\t\t\tAddPlot(new Stroke(Brushes.{c}, DashStyleHelper.Dot, 1), PlotStyle.Line, \"{g} Dev 2 Upper\");\n"
    csharp_code += f"\t\t\t\tAddPlot(new Stroke(Brushes.{c}, DashStyleHelper.Dot, 1), PlotStyle.Line, \"{g} Dev 2 Lower\");\n"
    csharp_code += f"\t\t\t\tAddPlot(new Stroke(Brushes.{c}, DashStyleHelper.DashDot, 1), PlotStyle.Line, \"{g} Dev 3 Upper\");\n"
    csharp_code += f"\t\t\t\tAddPlot(new Stroke(Brushes.{c}, DashStyleHelper.DashDot, 1), PlotStyle.Line, \"{g} Dev 3 Lower\");\n"

csharp_code += """
\t\t\t}
\t\t\telse if (State == State.Configure)
\t\t\t{
\t\t\t\tglobexSession = new OrcaVwapSession();
\t\t\t\trthSession = new OrcaVwapSession();
\t\t\t\tweeklySession = new OrcaVwapSession();
\t\t\t\trollingHistory = new Queue<OrcaVwapBucket>();
\t\t\t\trollingDeveloping = new OrcaVwapBucket();
\t\t\t\trollingTotal = new OrcaVwapSession();
\t\t\t\tcurrentMinuteToken = DateTime.MinValue;
\t\t\t\tlastBarVolume = 0;
\t\t\t\tlastBarIndex = -1;
\t\t\t}
\t\t}

\t\tprivate bool CrossedTime(DateTime start, DateTime end, TimeSpan target)
\t\t{
\t\t\tif (start >= end) return false;
\t\t\t
\t\t\tDateTime targetTime;
\t\t\tif (end.TimeOfDay >= target) 
\t\t\t{
\t\t\t\ttargetTime = end.Date + target;
\t\t\t}
\t\t\telse 
\t\t\t{
\t\t\t\ttargetTime = end.Date.AddDays(-1) + target;
\t\t\t}
\t\t\t
\t\t\treturn start < targetTime && end >= targetTime;
\t\t}

\t\tprivate bool CrossedWeekly(DateTime start, DateTime end, TimeSpan target)
\t\t{
\t\t\tif (start >= end) return false;
\t\t\t
\t\t\t// Get Sunday of the end date's week (or previous week if we are early Sunday)
\t\t\tint diff = (int)end.DayOfWeek - (int)DayOfWeek.Sunday;
\t\t\tif (diff < 0) diff += 7;
\t\t\tDateTime sunday = end.Date.AddDays(-diff);
\t\t\t
\t\t\t// If we are before the target time on Sunday, the weekly anchor is the PREVIOUS Sunday
\t\t\tif (end.DayOfWeek == DayOfWeek.Sunday && end.TimeOfDay < target)
\t\t\t{
\t\t\t\tsunday = sunday.AddDays(-7);
\t\t\t}
\t\t\t
\t\t\tDateTime targetTime = sunday + target;
\t\t\treturn start < targetTime && end >= targetTime;
\t\t}

\t\tprotected override void OnBarUpdate()
\t\t{
\t\t\tif (CurrentBar < 0) return;

\t\t\tdouble currentVolume = Volume[0];
\t\t\tdouble tickVol = 0;

\t\t\tif (CurrentBar != lastBarIndex) 
\t\t\t{
\t\t\t\ttickVol = currentVolume; // New bar starts
\t\t\t}
\t\t\telse 
\t\t\t{
\t\t\t\ttickVol = currentVolume - lastBarVolume; // Intra-bar
\t\t\t}
\t\t\t
\t\t\tlastBarVolume = currentVolume;
\t\t\tlastBarIndex = CurrentBar;

\t\t\tif (tickVol <= 0) return;

\t\t\tdouble price = (State == State.Historical) ? Typical[0] : Close[0];
\t\t\tDateTime time0 = Time[0];
\t\t\tDateTime time1 = (CurrentBar > 0) ? Time[1] : time0;

"""

for i, g in enumerate(["Globex", "RTH", "Weekly"]):
    sess = g.lower() + "Session"
    method = "CrossedWeekly" if g == "Weekly" else "CrossedTime"
    offset = i * 7
    if g == "Weekly":
        offset = 21 # 0, 7, 21
    csharp_code += f"\t\t\t// --- {g} VWAP ---\n"
    csharp_code += f"\t\t\tif ({g}ShowVWAP)\n"
    csharp_code += "\t\t\t{\n"
    csharp_code += f"\t\t\t\tif ({method}(time1, time0, {g}StartTime)) {sess}.Reset();\n"
    csharp_code += f"\t\t\t\t{sess}.Add(price, tickVol);\n"
    csharp_code += f"\t\t\t\tif ({sess}.SumVol > 0)\n"
    csharp_code += "\t\t\t\t{\n"
    csharp_code += f"\t\t\t\t\tdouble vwap = {sess}.Vwap;\n"
    csharp_code += f"\t\t\t\t\tdouble sd = {sess}.StdDev;\n"
    csharp_code += f"\t\t\t\t\tValues[{offset}][0] = vwap;\n"
    csharp_code += f"\t\t\t\t\tif ({g}ShowDev1) {{ Values[{offset+1}][0] = vwap + sd * {g}Dev1Mult; Values[{offset+2}][0] = vwap - sd * {g}Dev1Mult; }}\n"
    csharp_code += f"\t\t\t\t\tif ({g}ShowDev2) {{ Values[{offset+3}][0] = vwap + sd * {g}Dev2Mult; Values[{offset+4}][0] = vwap - sd * {g}Dev2Mult; }}\n"
    csharp_code += f"\t\t\t\t\tif ({g}ShowDev3) {{ Values[{offset+5}][0] = vwap + sd * {g}Dev3Mult; Values[{offset+6}][0] = vwap - sd * {g}Dev3Mult; }}\n"
    
    csharp_code += f"\t\t\t\t\t// Draw Regions\n"
    csharp_code += f"\t\t\t\t\tif ({g}FillOpacityCore > 0) {{\n"
    csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_CoreU\", CurrentBar, 0, Values[{offset}], Values[{offset+1}], null, {g}FillColorCore, {g}FillOpacityCore);\n"
    csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_CoreD\", CurrentBar, 0, Values[{offset}], Values[{offset+2}], null, {g}FillColorCore, {g}FillOpacityCore);\n"
    csharp_code += f"\t\t\t\t\t}}\n"
    
    csharp_code += f"\t\t\t\t\tif ({g}FillOpacity12 > 0) {{\n"
    csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_12U\", CurrentBar, 0, Values[{offset+1}], Values[{offset+3}], null, {g}FillColor12, {g}FillOpacity12);\n"
    csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_12D\", CurrentBar, 0, Values[{offset+2}], Values[{offset+4}], null, {g}FillColor12, {g}FillOpacity12);\n"
    csharp_code += f"\t\t\t\t\t}}\n"
    
    csharp_code += f"\t\t\t\t\tif ({g}FillOpacity23 > 0) {{\n"
    csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_23U\", CurrentBar, 0, Values[{offset+3}], Values[{offset+5}], null, {g}FillColor23, {g}FillOpacity23);\n"
    csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_23D\", CurrentBar, 0, Values[{offset+4}], Values[{offset+6}], null, {g}FillColor23, {g}FillOpacity23);\n"
    csharp_code += f"\t\t\t\t\t}}\n"
    csharp_code += "\t\t\t\t}\n"
    csharp_code += "\t\t\t}\n"

# Rolling VWAP logic
g = "Rolling"
offset = 14
csharp_code += f"""
\t\t\t// --- 3. ROLLING VWAP ---
\t\t\tif (RollingShowVWAP)
\t\t\t{{
\t\t\t\tDateTime minuteToken = new DateTime(time0.Year, time0.Month, time0.Day, time0.Hour, time0.Minute, 0);
\t\t\t\tif (minuteToken > currentMinuteToken) 
\t\t\t\t{{
\t\t\t\t\tif (currentMinuteToken != DateTime.MinValue) 
\t\t\t\t\t{{
\t\t\t\t\t\trollingHistory.Enqueue(rollingDeveloping);
\t\t\t\t\t\t
\t\t\t\t\t\tint maxBuckets = (int)RollingPeriod;
\t\t\t\t\t\tint missedMinutes = (int)(minuteToken - currentMinuteToken).TotalMinutes;

\t\t\t\t\t\tif (missedMinutes > 1 && missedMinutes <= 720) 
\t\t\t\t\t\t{{
\t\t\t\t\t\t\tint emptyBuckets = Math.Min(missedMinutes - 1, maxBuckets);
\t\t\t\t\t\t\tfor (int i = 0; i < emptyBuckets; i++) 
\t\t\t\t\t\t\t{{
\t\t\t\t\t\t\t\trollingHistory.Enqueue(new OrcaVwapBucket());
\t\t\t\t\t\t\t}}
\t\t\t\t\t\t}}

\t\t\t\t\t\trollingDeveloping = new OrcaVwapBucket();
\t\t\t\t\t\t
\t\t\t\t\t\twhile (rollingHistory.Count >= maxBuckets) rollingHistory.Dequeue();

\t\t\t\t\t\trollingTotal.Reset();
\t\t\t\t\t\tforeach(var b in rollingHistory) 
\t\t\t\t\t\t{{
\t\t\t\t\t\t\trollingTotal.SumVol += b.SumVol;
\t\t\t\t\t\t\trollingTotal.SumPriceVol += b.SumPriceVol;
\t\t\t\t\t\t\trollingTotal.SumPrice2Vol += b.SumPrice2Vol;
\t\t\t\t\t\t}}
\t\t\t\t\t}}
\t\t\t\t\tcurrentMinuteToken = minuteToken;
\t\t\t\t}}
\t\t\t\telse if (minuteToken < currentMinuteToken)
\t\t\t\t{{
\t\t\t\t\trollingHistory.Clear();
\t\t\t\t\trollingDeveloping = new OrcaVwapBucket();
\t\t\t\t\trollingTotal.Reset();
\t\t\t\t\tcurrentMinuteToken = minuteToken;
\t\t\t\t}}

\t\t\t\trollingDeveloping.SumVol += tickVol;
\t\t\t\trollingDeveloping.SumPriceVol += price * tickVol;
\t\t\t\trollingDeveloping.SumPrice2Vol += price * price * tickVol;

\t\t\t\tdouble currentSumVol = rollingTotal.SumVol + rollingDeveloping.SumVol;
\t\t\t\tif (currentSumVol > 0) 
\t\t\t\t{{
\t\t\t\t\tdouble currentSumPriceVol = rollingTotal.SumPriceVol + rollingDeveloping.SumPriceVol;
\t\t\t\t\tdouble currentSumPrice2Vol = rollingTotal.SumPrice2Vol + rollingDeveloping.SumPrice2Vol;

\t\t\t\t\tdouble vwap = currentSumPriceVol / currentSumVol;
\t\t\t\t\tdouble variance = Math.Max(0, (currentSumPrice2Vol / currentSumVol) - (vwap * vwap));
\t\t\t\t\tdouble sd = Math.Sqrt(variance);

\t\t\t\t\tValues[{offset}][0] = vwap;
"""
csharp_code += f"\t\t\t\t\tif ({g}ShowDev1) {{ Values[{offset+1}][0] = vwap + sd * {g}Dev1Mult; Values[{offset+2}][0] = vwap - sd * {g}Dev1Mult; }}\n"
csharp_code += f"\t\t\t\t\tif ({g}ShowDev2) {{ Values[{offset+3}][0] = vwap + sd * {g}Dev2Mult; Values[{offset+4}][0] = vwap - sd * {g}Dev2Mult; }}\n"
csharp_code += f"\t\t\t\t\tif ({g}ShowDev3) {{ Values[{offset+5}][0] = vwap + sd * {g}Dev3Mult; Values[{offset+6}][0] = vwap - sd * {g}Dev3Mult; }}\n"

csharp_code += f"\t\t\t\t\t// Draw Regions\n"
csharp_code += f"\t\t\t\t\tif ({g}FillOpacityCore > 0) {{\n"
csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_CoreU\", CurrentBar, 0, Values[{offset}], Values[{offset+1}], null, {g}FillColorCore, {g}FillOpacityCore);\n"
csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_CoreD\", CurrentBar, 0, Values[{offset}], Values[{offset+2}], null, {g}FillColorCore, {g}FillOpacityCore);\n"
csharp_code += f"\t\t\t\t\t}}\n"

csharp_code += f"\t\t\t\t\tif ({g}FillOpacity12 > 0) {{\n"
csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_12U\", CurrentBar, 0, Values[{offset+1}], Values[{offset+3}], null, {g}FillColor12, {g}FillOpacity12);\n"
csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_12D\", CurrentBar, 0, Values[{offset+2}], Values[{offset+4}], null, {g}FillColor12, {g}FillOpacity12);\n"
csharp_code += f"\t\t\t\t\t}}\n"

csharp_code += f"\t\t\t\t\tif ({g}FillOpacity23 > 0) {{\n"
csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_23U\", CurrentBar, 0, Values[{offset+3}], Values[{offset+5}], null, {g}FillColor23, {g}FillOpacity23);\n"
csharp_code += f"\t\t\t\t\t\tDraw.Region(this, \"{g}R_23D\", CurrentBar, 0, Values[{offset+4}], Values[{offset+6}], null, {g}FillColor23, {g}FillOpacity23);\n"
csharp_code += f"\t\t\t\t\t}}\n"

csharp_code += "\t\t\t\t}\n\t\t\t}\n\t\t}\n"

# Properties section
csharp_code += "\n\t\t#region Properties\n"

for g, gname, t, c, has_time in groups:
    csharp_code += f"\n\t\t// ------------------ {gname} ------------------\n"
    csharp_code += f"\t\t[NinjaScriptProperty]\n"
    csharp_code += f"\t\t[Display(Name=\"1. Show VWAP\", Order=1, GroupName=\"{gname}\")]\n"
    csharp_code += f"\t\tpublic bool {g}ShowVWAP {{ get; set; }}\n\n"
    
    order = 2
    if has_time:
        csharp_code += f"\t\t[NinjaScriptProperty]\n"
        csharp_code += f"\t\t[PropertyEditor(\"NinjaTrader.Gui.Tools.TimeSpanEditorKey\")]\n"
        csharp_code += f"\t\t[Display(Name=\"2. Start Time\", Order={order}, GroupName=\"{gname}\")]\n"
        csharp_code += f"\t\tpublic TimeSpan {g}StartTime {{ get; set; }}\n"
    elif g == "Rolling":
        csharp_code += f"\t\t[NinjaScriptProperty]\n"
        csharp_code += f"\t\t[Display(Name=\"2. Rolling Period\", Order={order}, GroupName=\"{gname}\")]\n"
        csharp_code += f"\t\tpublic RollingVwapPeriod RollingPeriod {{ get; set; }}\n"
    
    for lvl in range(1, 4):
        csharp_code += f"\t\t[NinjaScriptProperty]\n"
        csharp_code += f"\t\t[Display(Name=\"Show Dev {lvl}\", Order={order+1}, GroupName=\"{gname}\")]\n"
        csharp_code += f"\t\tpublic bool {g}ShowDev{lvl} {{ get; set; }}\n"
        
        csharp_code += f"\t\t[NinjaScriptProperty]\n"
        csharp_code += f"\t\t[Display(Name=\"Dev {lvl} Multiplier\", Order={order+2}, GroupName=\"{gname}\")]\n"
        csharp_code += f"\t\tpublic double {g}Dev{lvl}Mult {{ get; set; }}\n"
        order += 2
        
    for label, suffix in [("Core-Dev1 Fill", "Core"), ("Dev1-Dev2 Fill", "12"), ("Dev2-Dev3 Fill", "23")]:
        csharp_code += f"\t\t[XmlIgnore]\n"
        csharp_code += f"\t\t[Display(Name=\"{label} Color\", Order={order+1}, GroupName=\"{gname}\")]\n"
        csharp_code += f"\t\tpublic Brush {g}FillColor{suffix} {{ get; set; }}\n"
        csharp_code += f"\t\t[Browsable(false)]\n"
        csharp_code += f"\t\tpublic string {g}FillColor{suffix}Serializable\n"
        csharp_code += "\t\t{\n"
        csharp_code += f"\t\t\tget {{ return Serialize.BrushToString({g}FillColor{suffix}); }}\n"
        csharp_code += f"\t\t\tset {{ {g}FillColor{suffix} = Serialize.StringToBrush(value); }}\n"
        csharp_code += "\t\t}\n"
        csharp_code += f"\t\t[NinjaScriptProperty]\n"
        csharp_code += f"\t\t[Range(0, 100)]\n"
        csharp_code += f"\t\t[Display(Name=\"{label} Opacity (0 = Off)\", Order={order+2}, GroupName=\"{gname}\")]\n"
        csharp_code += f"\t\tpublic int {g}FillOpacity{suffix} {{ get; set; }}\n"
        order += 2

csharp_code += "\t\t#endregion\n\t}\n}\n"

with open("c:\\Users\\Owner\\.gemini\\antigravity\\scratch\\Orca\\Orca Time VWAPs\\OrcaTimeVWAPs.cs", "w", encoding="utf-8") as f:
    f.write(csharp_code)
"""

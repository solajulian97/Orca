using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Plots lines at user  defined values.
/// </summary>
public class ConstantLines : Indicator
{
	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Line1 => ((NinjaScriptBase)this).Values[0];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Line2 => ((NinjaScriptBase)this).Values[1];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Line3 => ((NinjaScriptBase)this).Values[2];

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Line4 => ((NinjaScriptBase)this).Values[3];

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Line1Value", GroupName = "NinjaScriptParameters", Order = 0)]
	public double Line1Value { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Line2Value", GroupName = "NinjaScriptParameters", Order = 1)]
	public double Line2Value { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Line3Value", GroupName = "NinjaScriptParameters", Order = 2)]
	public double Line3Value { get; set; }

	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Line4Value", GroupName = "NinjaScriptParameters", Order = 3)]
	public double Line4Value { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected O, but got Unknown
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionConstantLines;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameConstantLines;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			Line1Value = 0.0;
			Line2Value = 0.0;
			Line3Value = 0.0;
			Line4Value = 0.0;
			((NinjaScriptBase)this).IsAutoScale = false;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsChartOnly = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue), (PlotStyle)5, Resource.ConstantLines1);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DarkCyan), (PlotStyle)5, Resource.ConstantLines2);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.SlateBlue), (PlotStyle)5, Resource.ConstantLines3);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.Goldenrod), (PlotStyle)5, Resource.ConstantLines4);
		}
	}

	protected override void OnBarUpdate()
	{
		if (Line1Value != 0.0)
		{
			Line1[0] = Line1Value;
		}
		if (Line2Value != 0.0)
		{
			Line2[0] = Line2Value;
		}
		if (Line3Value != 0.0)
		{
			Line3[0] = Line3Value;
		}
		if (Line4Value != 0.0)
		{
			Line4[0] = Line4Value;
		}
	}
}

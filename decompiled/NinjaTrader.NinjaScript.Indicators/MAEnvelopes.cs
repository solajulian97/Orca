using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

/// <summary>
/// Moving Average Envelopes
/// </summary>
public class MAEnvelopes : Indicator
{
	private EMA ema;

	private HMA hma;

	private SMA sma;

	private TEMA tema;

	private TMA tma;

	private WMA wma;

	private int period;

	[Range(0.01, 2147483647.0)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "EnvelopePercentage", GroupName = "NinjaScriptParameters", Order = 0)]
	public double EnvelopePercentage { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Lower => ((NinjaScriptBase)this).Values[2];

	[Range(1, 6)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "MAType", GroupName = "NinjaScriptParameters", Order = 1)]
	[RefreshProperties(RefreshProperties.All)]
	[TypeConverter(typeof(MovingAverageEnumConverter))]
	[PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
	public int MAType { get; set; }

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Middle => ((NinjaScriptBase)this).Values[1];

	[Range(1, int.MaxValue)]
	[NinjaScriptProperty]
	[Display(ResourceType = typeof(Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 2)]
	public int Period
	{
		get
		{
			if (MAType == 2)
			{
				return Math.Max(2, period);
			}
			return period;
		}
		set
		{
			period = ((MAType != 2) ? value : Math.Max(2, value));
		}
	}

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> Upper => ((NinjaScriptBase)this).Values[0];

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionMAEnvelopes;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameMAEnvelopes;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			((NinjaScriptBase)this).IsOverlay = true;
			MAType = 3;
			Period = 14;
			EnvelopePercentage = 1.5;
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorUpper);
			((NinjaScriptBase)this).AddPlot(new Stroke((Brush)Brushes.DodgerBlue, (DashStyleHelper)1, 1f), (PlotStyle)6, Resource.NinjaScriptIndicatorMiddle);
			((NinjaScriptBase)this).AddPlot((Brush)Brushes.DodgerBlue, Resource.NinjaScriptIndicatorLower);
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			ema = EMA(((NinjaScriptBase)this).Inputs[0], Period);
			hma = HMA(((NinjaScriptBase)this).Inputs[0], Math.Max(2, Period));
			sma = SMA(((NinjaScriptBase)this).Inputs[0], Period);
			tma = TMA(((NinjaScriptBase)this).Inputs[0], Period);
			tema = TEMA(((NinjaScriptBase)this).Inputs[0], Period);
			wma = WMA(((NinjaScriptBase)this).Inputs[0], Period);
		}
	}

	protected override void OnBarUpdate()
	{
		double num = 0.0;
		switch (MAType)
		{
		case 1:
			num = (Middle[0] = ((NinjaScriptBase)ema)[0]);
			break;
		case 2:
			num = (Middle[0] = ((NinjaScriptBase)hma)[0]);
			break;
		case 3:
			num = (Middle[0] = ((NinjaScriptBase)sma)[0]);
			break;
		case 4:
			num = (Middle[0] = ((NinjaScriptBase)tma)[0]);
			break;
		case 5:
			num = (Middle[0] = ((NinjaScriptBase)tema)[0]);
			break;
		case 6:
			num = (Middle[0] = ((NinjaScriptBase)wma)[0]);
			break;
		}
		Upper[0] = num + num * EnvelopePercentage / 100.0;
		Lower[0] = num - num * EnvelopePercentage / 100.0;
	}
}

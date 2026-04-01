using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.SuperDom;

namespace NinjaTrader.NinjaScript.SuperDomColumns;

public class ProfitLoss : SuperDomColumn
{
	private FontFamily fontFamily;

	private CultureInfo forexCulture;

	private Pen gridPen;

	private double halfPenWidth;

	private Typeface typeFace;

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptColumnBaseBackground", GroupName = "PropertyCategoryVisual", Order = 105)]
	public Brush BackColor { get; set; }

	[Browsable(false)]
	public string BackColorSerialize
	{
		get
		{
			return Serialize.BrushToString(BackColor, (object)"brushPriceColumnBackground");
		}
		set
		{
			BackColor = Serialize.StringToBrush(value, (object)"brushPriceColumnBackground");
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptNegativeBackgroundColor", GroupName = "PropertyCategoryVisual", Order = 110)]
	public Brush NegativeBackColor { get; set; }

	[Browsable(false)]
	public string NegativeBackColorSerialize
	{
		get
		{
			return Serialize.BrushToString(NegativeBackColor);
		}
		set
		{
			NegativeBackColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptNegativeForegroundColor", GroupName = "PropertyCategoryVisual", Order = 120)]
	public Brush NegativeForeColor { get; set; }

	[Browsable(false)]
	public string NegativeForeColorSerialize
	{
		get
		{
			return Serialize.BrushToString(NegativeForeColor);
		}
		set
		{
			NegativeForeColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptPositiveBackgroundColor", GroupName = "PropertyCategoryVisual", Order = 130)]
	public Brush PositiveBackColor { get; set; }

	[Browsable(false)]
	public string PositiveBackColorSerialize
	{
		get
		{
			return Serialize.BrushToString(PositiveBackColor);
		}
		set
		{
			PositiveBackColor = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptPositiveForegroundColor", GroupName = "PropertyCategoryVisual", Order = 140)]
	public Brush PositiveForeColor { get; set; }

	[Browsable(false)]
	public string PositiveForeColorSerialize
	{
		get
		{
			return Serialize.BrushToString(PositiveForeColor);
		}
		set
		{
			PositiveForeColor = Serialize.StringToBrush(value);
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDisplayUnit", GroupName = "NinjaScriptSetup", Order = 100)]
	public PerformanceUnit PnlDisplayUnit { get; set; }

	protected override void OnRender(DrawingContext dc, double renderWidth)
	{
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Invalid comparison between Unknown and I4
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Expected I4, but got Unknown
		if (gridPen == null && ((SuperDomColumn)this).UiWrapper != null)
		{
			CompositionTarget compositionTarget = PresentationSource.FromVisual(((SuperDomColumn)this).UiWrapper)?.CompositionTarget;
			if (compositionTarget != null)
			{
				double num = 1.0 / compositionTarget.TransformToDevice.M11;
				gridPen = new Pen(Application.Current.TryFindResource("BorderThinBrush") as Brush, 1.0 * num);
				halfPenWidth = gridPen.Thickness * 0.5;
			}
		}
		if (gridPen == null)
		{
			return;
		}
		double num2 = 0.0 - gridPen.Thickness;
		double pixelsPerDip = VisualTreeHelper.GetDpi(((SuperDomColumn)this).UiWrapper).PixelsPerDip;
		lock (((SuperDomColumn)this).SuperDom.Rows)
		{
			foreach (PriceRow row in ((SuperDomColumn)this).SuperDom.Rows)
			{
				if (!(renderWidth - halfPenWidth >= 0.0))
				{
					continue;
				}
				Rect rectangle = new Rect(0.0 - halfPenWidth, num2, renderWidth - halfPenWidth, ((SuperDomColumn)this).SuperDom.ActualRowHeight);
				GuidelineSet guidelineSet = new GuidelineSet();
				guidelineSet.GuidelinesX.Add(rectangle.Left + halfPenWidth);
				guidelineSet.GuidelinesX.Add(rectangle.Right + halfPenWidth);
				guidelineSet.GuidelinesY.Add(rectangle.Top + halfPenWidth);
				guidelineSet.GuidelinesY.Add(rectangle.Bottom + halfPenWidth);
				dc.PushGuidelineSet(guidelineSet);
				if (((SuperDomColumn)this).SuperDom.IsConnected && ((SuperDomColumn)this).SuperDom.Position != null && (int)((SuperDomColumn)this).SuperDom.Position.MarketPosition != 2)
				{
					double unrealizedProfitLoss = ((SuperDomColumn)this).SuperDom.Position.GetUnrealizedProfitLoss(PnlDisplayUnit, row.Price);
					string textToFormat = string.Empty;
					PerformanceUnit pnlDisplayUnit = PnlDisplayUnit;
					switch ((int)pnlDisplayUnit)
					{
					case 0:
						textToFormat = Globals.FormatCurrency(unrealizedProfitLoss, ((SuperDomColumn)this).SuperDom.Position);
						break;
					case 1:
						textToFormat = unrealizedProfitLoss.ToString("P", Globals.GeneralOptions.CurrentCulture);
						break;
					case 2:
						textToFormat = (Math.Round(unrealizedProfitLoss * 10.0) / 10.0).ToString("0.0", forexCulture);
						break;
					case 3:
						textToFormat = ((SuperDomColumn)this).SuperDom.Position.Instrument.MasterInstrument.RoundToTickSize(unrealizedProfitLoss).ToString("0.#######", Globals.GeneralOptions.CurrentCulture);
						break;
					case 4:
						textToFormat = Math.Round(unrealizedProfitLoss).ToString(Globals.GeneralOptions.CurrentCulture);
						break;
					}
					dc.DrawRectangle((unrealizedProfitLoss > 0.0) ? PositiveBackColor : NegativeBackColor, null, rectangle);
					dc.DrawLine(gridPen, new Point(0.0 - gridPen.Thickness, rectangle.Bottom), new Point(renderWidth - halfPenWidth, rectangle.Bottom));
					dc.DrawLine(gridPen, new Point(rectangle.Right, num2), new Point(rectangle.Right, rectangle.Bottom));
					fontFamily = ((SuperDomColumn)this).SuperDom.Font.Family;
					typeFace = new Typeface(fontFamily, ((SuperDomColumn)this).SuperDom.Font.Italic ? FontStyles.Italic : FontStyles.Normal, ((SuperDomColumn)this).SuperDom.Font.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
					if (renderWidth - 6.0 > 0.0)
					{
						FormattedText formattedText = new FormattedText(textToFormat, Globals.GeneralOptions.CurrentCulture, FlowDirection.LeftToRight, typeFace, ((SuperDomColumn)this).SuperDom.Font.Size, (((SuperDomColumn)this).SuperDom.Position.Instrument.MasterInstrument.RoundToTickSize(unrealizedProfitLoss) > 0.0) ? PositiveForeColor : NegativeForeColor, pixelsPerDip)
						{
							MaxLineCount = 1,
							MaxTextWidth = renderWidth - 6.0,
							Trimming = TextTrimming.CharacterEllipsis
						};
						dc.DrawText(formattedText, new Point(4.0, num2 + (((SuperDomColumn)this).SuperDom.ActualRowHeight - formattedText.Height) / 2.0));
					}
				}
				else
				{
					dc.DrawRectangle(BackColor, null, rectangle);
					dc.DrawLine(gridPen, new Point(0.0 - gridPen.Thickness, rectangle.Bottom), new Point(renderWidth - halfPenWidth, rectangle.Bottom));
					dc.DrawLine(gridPen, new Point(rectangle.Right, num2), new Point(rectangle.Right, rectangle.Bottom));
				}
				dc.Pop();
				num2 += ((SuperDomColumn)this).SuperDom.ActualRowHeight;
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((SuperDomColumn)this).Name = Resource.NinjaScriptSuperDomColumnProfitAndLoss;
			((NinjaScript)this).Description = Resource.NinjaScriptSuperDomColumnDescriptionPnl;
			((SuperDomColumn)this).DefaultWidth = 100.0;
			((SuperDomColumn)this).PreviousWidth = -1.0;
			((SuperDomColumn)this).IsDataSeriesRequired = false;
			BackColor = Application.Current.TryFindResource("brushPriceColumnBackground") as Brush;
			NegativeBackColor = Brushes.Crimson;
			NegativeForeColor = Application.Current.TryFindResource("FontControlBrush") as Brush;
			PositiveBackColor = Brushes.SeaGreen;
			PositiveForeColor = Application.Current.TryFindResource("FontControlBrush") as Brush;
			PnlDisplayUnit = (PerformanceUnit)0;
			forexCulture = Globals.GeneralOptions.CurrentCulture.Clone() as CultureInfo;
			if (forexCulture != null)
			{
				forexCulture.NumberFormat.NumberDecimalSeparator = "'";
			}
		}
		else if ((int)((NinjaScript)this).State == 2 && ((SuperDomColumn)this).UiWrapper != null)
		{
			CompositionTarget compositionTarget = PresentationSource.FromVisual(((SuperDomColumn)this).UiWrapper)?.CompositionTarget;
			if (compositionTarget != null)
			{
				double num = 1.0 / compositionTarget.TransformToDevice.M11;
				gridPen = new Pen(Application.Current.TryFindResource("BorderThinBrush") as Brush, 1.0 * num);
				halfPenWidth = gridPen.Thickness * 0.5;
			}
		}
	}
}

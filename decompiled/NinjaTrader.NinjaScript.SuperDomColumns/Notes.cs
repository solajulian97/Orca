using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.SuperDom;

namespace NinjaTrader.NinjaScript.SuperDomColumns;

public class Notes : SuperDomColumn
{
	private double columnWidth;

	private double currentEditingPrice = -1.0;

	private FontFamily fontFamily;

	private double gridHeight;

	private int gridIndex;

	private Pen gridPen;

	private double halfPenWidth;

	private TextBox tbNotes;

	private Typeface typeFace;

	private CommandBinding displayTextBoxCommandBinding;

	private MouseBinding doubleClickMouseBinding;

	public static ICommand DisplayTextBox = new RoutedCommand("DisplayTextBox", typeof(Notes));

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptColumnBaseBackground", GroupName = "PropertyCategoryVisual", Order = 110)]
	public Brush BackColor { get; set; }

	[Browsable(false)]
	public string BackBrushSerialize
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
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptColumnBaseForeground", GroupName = "PropertyCategoryVisual", Order = 111)]
	public Brush ForeColor { get; set; }

	[Browsable(false)]
	public string ForeColorSerialize
	{
		get
		{
			return Serialize.BrushToString(ForeColor);
		}
		set
		{
			ForeColor = Serialize.StringToBrush(value);
		}
	}

	[Browsable(false)]
	public List<string> NotesSerializable { get; set; }

	[XmlIgnore]
	[Browsable(false)]
	public ConcurrentDictionary<double, string> PriceStringValues { get; set; }

	public static void DisplayTextBoxExecuted(object sender, ExecutedRoutedEventArgs e)
	{
		if (!(e.Parameter is Notes notes))
		{
			return;
		}
		Point position = Mouse.GetPosition(e.Source as IInputElement);
		if (notes.gridHeight > 0.0 && ((SuperDomColumn)notes).SuperDom.IsConnected)
		{
			if (notes.tbNotes.Visibility == Visibility.Visible)
			{
				notes.SetAndSaveNote();
				notes.tbNotes.Text = string.Empty;
			}
			notes.gridIndex = (int)Math.Floor(position.Y / ((SuperDomColumn)notes).SuperDom.ActualRowHeight);
			notes.currentEditingPrice = ((SuperDomColumn)notes).SuperDom.Rows[notes.gridIndex].Price;
			double top = (double)notes.gridIndex * ((SuperDomColumn)notes).SuperDom.ActualRowHeight;
			notes.tbNotes.Height = ((SuperDomColumn)notes).SuperDom.ActualRowHeight;
			notes.tbNotes.Margin = new Thickness(0.0, top, 0.0, 0.0);
			notes.tbNotes.Text = notes.PriceStringValues[notes.currentEditingPrice];
			notes.tbNotes.Width = notes.columnWidth;
			notes.tbNotes.Visibility = Visibility.Visible;
			notes.tbNotes.SetValue(Panel.ZIndexProperty, 100);
			notes.tbNotes.BringIntoView();
			notes.tbNotes.Focus();
			((SuperDomColumn)notes).OnPropertyChanged("DisplayTextBoxExecuted");
		}
	}

	public override void CopyCustomData(SuperDomColumn newInstance)
	{
		if (newInstance is Notes notes)
		{
			notes.PriceStringValues = new ConcurrentDictionary<double, string>(PriceStringValues);
		}
	}

	protected override void OnRender(DrawingContext dc, double renderWidth)
	{
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
		columnWidth = renderWidth;
		gridHeight = 0.0 - gridPen.Thickness;
		double num2 = 0.0 - gridPen.Thickness;
		double pixelsPerDip = VisualTreeHelper.GetDpi(((SuperDomColumn)this).UiWrapper).PixelsPerDip;
		if (((SuperDomColumn)this).SuperDom.IsConnected)
		{
			if (tbNotes.Visibility == Visibility.Visible && ((SuperDomColumn)this).SuperDom.Rows.All((PriceRow r) => Math.Abs(r.Price - currentEditingPrice) > 1E-15))
			{
				tbNotes.Visibility = Visibility.Hidden;
			}
			if (tbNotes.Visibility == Visibility.Hidden && ((SuperDomColumn)this).SuperDom.Rows.Any((PriceRow r) => Math.Abs(r.Price - currentEditingPrice) < 1E-15))
			{
				tbNotes.Visibility = Visibility.Visible;
			}
		}
		lock (((SuperDomColumn)this).SuperDom.Rows)
		{
			foreach (PriceRow row in ((SuperDomColumn)this).SuperDom.Rows)
			{
				PriceStringValues.AddOrUpdate(row.Price, string.Empty, (double _, string oldValue) => oldValue);
				if (tbNotes.Visibility == Visibility.Visible && Math.Abs(row.Price - currentEditingPrice) < 1E-15 && ((SuperDomColumn)this).SuperDom.Rows.IndexOf(row) != gridIndex)
				{
					gridIndex = ((SuperDomColumn)this).SuperDom.Rows.IndexOf(row);
					double top = (double)gridIndex * ((SuperDomColumn)this).SuperDom.ActualRowHeight;
					tbNotes.Margin = new Thickness(0.0, top, 0.0, 0.0);
				}
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
				dc.DrawRectangle(BackColor, null, rectangle);
				dc.DrawLine(gridPen, new Point(0.0 - gridPen.Thickness, rectangle.Bottom), new Point(renderWidth - halfPenWidth, rectangle.Bottom));
				dc.DrawLine(gridPen, new Point(rectangle.Right, num2), new Point(rectangle.Right, rectangle.Bottom));
				if (PriceStringValues.TryGetValue(row.Price, out var value) && !string.IsNullOrEmpty(PriceStringValues[row.Price]))
				{
					fontFamily = ((SuperDomColumn)this).SuperDom.Font.Family;
					typeFace = new Typeface(fontFamily, ((SuperDomColumn)this).SuperDom.Font.Italic ? FontStyles.Italic : FontStyles.Normal, ((SuperDomColumn)this).SuperDom.Font.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
					if (renderWidth - 6.0 > 0.0)
					{
						FormattedText formattedText = new FormattedText(value, Globals.GeneralOptions.CurrentCulture, FlowDirection.LeftToRight, typeFace, ((SuperDomColumn)this).SuperDom.Font.Size, ForeColor, pixelsPerDip)
						{
							MaxLineCount = 1,
							MaxTextWidth = renderWidth - 6.0,
							Trimming = TextTrimming.CharacterEllipsis
						};
						dc.DrawText(formattedText, new Point(4.0, num2 + (((SuperDomColumn)this).SuperDom.ActualRowHeight - formattedText.Height) / 2.0));
					}
				}
				dc.Pop();
				num2 += ((SuperDomColumn)this).SuperDom.ActualRowHeight;
				gridHeight += ((SuperDomColumn)this).SuperDom.ActualRowHeight;
			}
		}
	}

	public override void OnRestoreValues()
	{
		bool flag = false;
		if (NotesSerializable != null)
		{
			foreach (string item in NotesSerializable)
			{
				string[] noteVal = item.Split(';');
				if (double.TryParse(noteVal[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
				{
					PriceStringValues.AddOrUpdate(result, noteVal[1], (double _, string _) => noteVal[1]);
					flag = true;
				}
			}
		}
		if (flag)
		{
			((SuperDomColumn)this).OnPropertyChanged("OnRestoreValues");
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Invalid comparison between Unknown and I4
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((SuperDomColumn)this).Name = Resource.NinjaScriptSuperDomColumnNotes;
			((NinjaScript)this).Description = Resource.NinjaScriptSuperDomColumnDescriptionNotes;
			((SuperDomColumn)this).DefaultWidth = 160.0;
			((SuperDomColumn)this).PreviousWidth = -1.0;
			((SuperDomColumn)this).IsDataSeriesRequired = false;
			BackColor = Application.Current.TryFindResource("brushPriceColumnBackground") as Brush;
			ForeColor = Application.Current.TryFindResource("FontControlBrush") as Brush;
			NotesSerializable = new List<string>();
			PriceStringValues = new ConcurrentDictionary<double, string>();
			return;
		}
		if ((int)((NinjaScript)this).State == 2)
		{
			if (((SuperDomColumn)this).UiWrapper != null)
			{
				CompositionTarget compositionTarget = PresentationSource.FromVisual(((SuperDomColumn)this).UiWrapper)?.CompositionTarget;
				if (compositionTarget != null)
				{
					double num = 1.0 / compositionTarget.TransformToDevice.M11;
					gridPen = new Pen(Application.Current.TryFindResource("BorderThinBrush") as Brush, 1.0 * num);
					halfPenWidth = gridPen.Thickness * 0.5;
				}
			}
			tbNotes = new TextBox
			{
				Margin = new Thickness(0.0),
				VerticalAlignment = VerticalAlignment.Top,
				Visibility = Visibility.Hidden
			};
			((SuperDomColumn)this).SetBindings();
			tbNotes.LostKeyboardFocus += delegate
			{
				if (Math.Abs(currentEditingPrice - -1.0) > 1E-15 && tbNotes.Visibility == Visibility.Visible)
				{
					SetAndSaveNote();
					tbNotes.Text = string.Empty;
					currentEditingPrice = -1.0;
					tbNotes.Visibility = Visibility.Hidden;
					((SuperDomColumn)this).OnPropertyChanged("OnStateChange");
				}
			};
			tbNotes.KeyDown += delegate(object _, KeyEventArgs args)
			{
				Key key = args.Key;
				if ((key == Key.Tab || key == Key.Return) ? true : false)
				{
					SetAndSaveNote();
					tbNotes.Text = string.Empty;
					currentEditingPrice = -1.0;
					tbNotes.Visibility = Visibility.Hidden;
					((SuperDomColumn)this).OnPropertyChanged("OnStateChange");
				}
				else if (args.Key == Key.Escape)
				{
					currentEditingPrice = -1.0;
					tbNotes.Visibility = Visibility.Hidden;
					((SuperDomColumn)this).OnPropertyChanged("OnStateChange");
				}
			};
			return;
		}
		if ((int)((NinjaScript)this).State == 3)
		{
			foreach (PriceRow row in ((SuperDomColumn)this).SuperDom.Rows)
			{
				PriceStringValues.AddOrUpdate(row.Price, string.Empty, (double _, string oldValue) => oldValue);
			}
			return;
		}
		if ((int)((NinjaScript)this).State == 8 && ((SuperDomColumn)this).UiWrapper != null)
		{
			((SuperDomColumn)this).UiWrapper.Children.Remove(tbNotes);
			((SuperDomColumn)this).UiWrapper.InputBindings.Remove(doubleClickMouseBinding);
			((SuperDomColumn)this).UiWrapper.CommandBindings.Remove(displayTextBoxCommandBinding);
		}
	}

	public override void SetBindings()
	{
		doubleClickMouseBinding = new MouseBinding(DisplayTextBox, new MouseGesture(MouseAction.LeftDoubleClick))
		{
			CommandParameter = this
		};
		displayTextBoxCommandBinding = new CommandBinding(DisplayTextBox, DisplayTextBoxExecuted);
		if (((SuperDomColumn)this).UiWrapper != null)
		{
			((SuperDomColumn)this).UiWrapper.InputBindings.Add(doubleClickMouseBinding);
			((SuperDomColumn)this).UiWrapper.CommandBindings.Add(displayTextBoxCommandBinding);
			((SuperDomColumn)this).UiWrapper.Children.Add(tbNotes);
		}
	}

	private void SetAndSaveNote()
	{
		string text = PriceStringValues.AddOrUpdate(currentEditingPrice, tbNotes.Text, (double _, string _) => tbNotes.Text);
		lock (NotesSerializable)
		{
			if (NotesSerializable.Any((string n) => n.StartsWith(currentEditingPrice.ToString("N2", CultureInfo.InvariantCulture))))
			{
				int index = NotesSerializable.IndexOf(NotesSerializable.SingleOrDefault((string n) => n.StartsWith(currentEditingPrice.ToString("N2", CultureInfo.InvariantCulture))));
				NotesSerializable[index] = currentEditingPrice.ToString("N2", CultureInfo.InvariantCulture) + ";" + text;
			}
			else
			{
				NotesSerializable.Add(currentEditingPrice.ToString("N2", CultureInfo.InvariantCulture) + ";" + tbNotes.Text);
			}
		}
	}
}

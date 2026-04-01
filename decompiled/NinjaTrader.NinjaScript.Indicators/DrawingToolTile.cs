using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

[TypeConverter("NinjaTrader.NinjaScript.Indicators.DrawingToolIndicatorTypeConverter")]
[CategoryOrder(typeof(Resource), "NinjaScriptParameters", 1)]
[CategoryOrder(typeof(Resource), "PropertyCategoryDataSeries", 2)]
[CategoryOrder(typeof(Resource), "NinjaScriptSetup", 3)]
[CategoryOrder(typeof(Resource), "NinjaScriptDrawingTools", 4)]
[CategoryOrder(typeof(Resource), "NinjaScriptIndicatorVisualGroup", 5)]
[CategoryExpanded(typeof(Resource), "NinjaScriptDrawingTools", false)]
public class DrawingToolTile : Indicator
{
	private Border b;

	private Grid grid;

	private Thickness margin;

	private bool subscribedToSize;

	private Point startPoint;

	[Browsable(false)]
	public double Top { get; set; }

	[Browsable(false)]
	public double Left { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptIsVisibleOnlyFocused", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 499)]
	public bool IsVisibleOnlyFocused { get; set; }

	public XElement SelectedTypes { get; set; }

	[Range(1, int.MaxValue)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptNumberOfRows", GroupName = "NinjaScriptParameters", Order = 0)]
	public int NumberOfRows { get; set; }

	protected override void OnBarUpdate()
	{
		if (subscribedToSize || ((IndicatorRenderBase)this).ChartPanel == null)
		{
			return;
		}
		subscribedToSize = true;
		((FrameworkElement)(object)((IndicatorRenderBase)this).ChartPanel).SizeChanged += delegate
		{
			if (grid != null && ((IndicatorRenderBase)this).ChartPanel != null && (grid.Margin.Left + grid.ActualWidth > ((FrameworkElement)(object)((IndicatorRenderBase)this).ChartPanel).ActualWidth || grid.Margin.Top + grid.ActualHeight > ((FrameworkElement)(object)((IndicatorRenderBase)this).ChartPanel).ActualHeight))
			{
				double left = Math.Max(0.0, Math.Min(grid.Margin.Left, ((FrameworkElement)(object)((IndicatorRenderBase)this).ChartPanel).ActualWidth - grid.ActualWidth));
				double top = Math.Max(0.0, Math.Min(grid.Margin.Top, ((FrameworkElement)(object)((IndicatorRenderBase)this).ChartPanel).ActualHeight - grid.ActualHeight));
				grid.Margin = new Thickness(left, top, 0.0, 0.0);
				Left = left;
				Top = top;
			}
		};
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScriptBase)this).Name = Resource.DrawingToolIndicatorName;
			((NinjaScript)this).Description = Resource.DrawingToolIndicatorDescription;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).IsChartOnly = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			((IndicatorBase)this).PaintPriceMarkers = false;
			((IndicatorBase)this).IsSuspendedWhileInactive = true;
			SelectedTypes = new XElement("SelectedTypes");
			Type[] array = new Type[10]
			{
				typeof(NinjaTrader.NinjaScript.DrawingTools.Ellipse),
				typeof(ExtendedLine),
				typeof(FibonacciExtensions),
				typeof(FibonacciRetracements),
				typeof(HorizontalLine),
				typeof(NinjaTrader.NinjaScript.DrawingTools.Line),
				typeof(Ray),
				typeof(NinjaTrader.NinjaScript.DrawingTools.Rectangle),
				typeof(Text),
				typeof(VerticalLine)
			};
			for (int i = 0; i < array.Length; i++)
			{
				XElement xElement = new XElement(array[i].FullName ?? "");
				xElement.Add(new XAttribute("Assembly", "NinjaTrader.Custom"));
				SelectedTypes.Add(xElement);
			}
			Left = 5.0;
			Top = -1.0;
			NumberOfRows = 5;
		}
		else
		{
			if ((int)((NinjaScript)this).State != 5 || !((NinjaScript)this).IsVisible || ((IndicatorRenderBase)this).ChartControl == null)
			{
				return;
			}
			if (Top < 0.0)
			{
				Top = 25.0;
			}
			((DispatcherObject)(object)((IndicatorRenderBase)this).ChartControl).Dispatcher.InvokeAsync(delegate
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0007: Invalid comparison between Unknown and I4
				if ((int)((NinjaScript)this).State < 8)
				{
					((IndicatorRenderBase)this).UserControlCollection.Add(CreateControl());
				}
			});
		}
	}

	private FrameworkElement CreateControl()
	{
		if (this.grid != null)
		{
			return this.grid;
		}
		this.grid = new Grid
		{
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(Left, Top, 0.0, 0.0)
		};
		this.grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = default(GridLength)
		});
		this.grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = default(GridLength)
		});
		this.grid.RowDefinitions.Add(new RowDefinition
		{
			Height = default(GridLength)
		});
		Brush background = (Application.Current.FindResource("BackgroundMainWindow") as Brush) ?? Brushes.White;
		Brush brush = (Application.Current.FindResource("BorderThinBrush") as Brush) ?? Brushes.Black;
		Grid grid = new Grid();
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(2.0, GridUnitType.Star)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(2.0, GridUnitType.Star)
		});
		for (int i = 0; i < grid.RowDefinitions.Count; i++)
		{
			System.Windows.Shapes.Ellipse element = new System.Windows.Shapes.Ellipse
			{
				Width = 3.0,
				Height = 3.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Fill = brush
			};
			Grid.SetRow(element, i);
			grid.Children.Add(element);
		}
		b = new Border
		{
			VerticalAlignment = VerticalAlignment.Top,
			BorderThickness = new Thickness(0.0, 1.0, 1.0, 1.0),
			BorderBrush = brush,
			Background = background,
			Width = 12.0,
			Height = 24.0,
			Cursor = Cursors.Hand,
			Child = grid
		};
		b.MouseDown += delegate(object _, MouseButtonEventArgs e)
		{
			startPoint = e.GetPosition((IInputElement)((IndicatorRenderBase)this).ChartPanel);
			margin = this.grid.Margin;
			if (e.ClickCount > 1)
			{
				b.ReleaseMouseCapture();
				((IndicatorRenderBase)this).ChartControl.OnIndicatorsHotKey((object)this, (KeyEventArgs)null);
			}
			else
			{
				b.CaptureMouse();
			}
		};
		b.MouseUp += delegate
		{
			b.ReleaseMouseCapture();
		};
		b.MouseMove += delegate(object _, MouseEventArgs e)
		{
			if (b.IsMouseCaptured && this.grid != null && ((IndicatorRenderBase)this).ChartPanel != null)
			{
				Point position = e.GetPosition((IInputElement)((IndicatorRenderBase)this).ChartPanel);
				if (margin.Left + (position.X - startPoint.X) < 0.0 || margin.Left + (position.X - startPoint.X) > ((FrameworkElement)(object)((IndicatorRenderBase)this).ChartPanel).ActualWidth - this.grid.ActualWidth || margin.Top + (position.Y - startPoint.Y) < 0.0 || margin.Top + (position.Y - startPoint.Y) > ((FrameworkElement)(object)((IndicatorRenderBase)this).ChartPanel).ActualHeight - this.grid.ActualHeight)
				{
					((IndicatorRenderBase)this).ChartControl.InitDragDrop((IChartObject)(object)this);
				}
				else
				{
					this.grid.Margin = new Thickness
					{
						Left = Math.Max(0.0, Math.Min(margin.Left + (position.X - startPoint.X), ((FrameworkElement)(object)((IndicatorRenderBase)this).ChartPanel).ActualWidth - this.grid.ActualWidth)),
						Top = Math.Max(0.0, Math.Min(margin.Top + (position.Y - startPoint.Y), ((FrameworkElement)(object)((IndicatorRenderBase)this).ChartPanel).ActualHeight - this.grid.ActualHeight))
					};
					Left = this.grid.Margin.Left;
					Top = this.grid.Margin.Top;
				}
			}
		};
		Grid.SetColumn(b, 1);
		this.grid.Children.Add(b);
		Grid grid2 = new Grid();
		List<XElement> list = SortElements(XElement.Parse(SelectedTypes.ToString()));
		int num = 0;
		int num2 = 0;
		FontFamily fontFamily = Application.Current.Resources["IconsFamily"] as FontFamily;
		Style style = Application.Current.Resources["LinkButtonStyle"] as Style;
		while (num2 < list.Count)
		{
			if (grid2.ColumnDefinitions.Count <= num)
			{
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
			}
			for (int num3 = 0; num3 < NumberOfRows; num3++)
			{
				if (num2 >= list.Count)
				{
					break;
				}
				if (grid2.RowDefinitions.Count <= num3)
				{
					grid2.RowDefinitions.Add(new RowDefinition
					{
						Height = new GridLength(1.0, GridUnitType.Auto)
					});
				}
				XElement xElement = list[num2];
				try
				{
					object obj = Globals.AssemblyRegistry[xElement.Attribute("Assembly").Value].CreateInstance(xElement.Name.ToString());
					DrawingTool dt = (DrawingTool)((obj is DrawingTool) ? obj : null);
					if (dt != null && dt.DisplayOnChartsMenus)
					{
						Button button = new Button
						{
							Content = (dt.Icon ?? Icons.DrawPencil),
							ToolTip = ((NinjaScript)dt).DisplayName,
							Style = style,
							FontFamily = fontFamily,
							FontSize = 16.0,
							FontStyle = FontStyles.Normal,
							Margin = new Thickness(3.0),
							Padding = new Thickness(3.0)
						};
						Grid.SetRow(button, num3);
						Grid.SetColumn(button, num);
						button.Click += delegate
						{
							ChartControl chartControl = ((IndicatorRenderBase)this).ChartControl;
							if (chartControl != null)
							{
								chartControl.TryStartDrawing(((object)dt).GetType().FullName);
							}
						};
						grid2.Children.Add(button);
						num2++;
					}
					else
					{
						list.RemoveAt(num3);
						num3--;
					}
				}
				catch (Exception ex)
				{
					list.RemoveAt(num3);
					num3--;
					Log.Process(typeof(Resource), "NinjaScriptTileError", new object[2]
					{
						xElement.Name.ToString(),
						ex
					}, (LogLevel)3, (LogCategories)16);
				}
			}
			num++;
		}
		Border element2 = new Border
		{
			Cursor = Cursors.Arrow,
			Background = (Application.Current.FindResource("BackgroundMainWindow") as Brush),
			BorderThickness = new Thickness((double)(Application.Current.FindResource("BorderThinThickness") ?? ((object)1))),
			BorderBrush = (Application.Current.FindResource("BorderThinBrush") as Brush),
			Child = grid2
		};
		this.grid.Children.Add(element2);
		if (IsVisibleOnlyFocused)
		{
			Binding binding = new Binding("IsActive")
			{
				Source = ((IndicatorRenderBase)this).ChartControl.OwnerChart,
				Converter = (Application.Current.FindResource("BoolToVisConverter") as IValueConverter)
			};
			this.grid.SetBinding(UIElement.VisibilityProperty, binding);
		}
		return this.grid;
	}

	public override void CopyTo(NinjaScript ninjaScript)
	{
		if (ninjaScript is DrawingToolTile drawingToolTile)
		{
			drawingToolTile.Left = Left;
			drawingToolTile.Top = Top;
		}
		((IndicatorRenderBase)this).CopyTo(ninjaScript);
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
	}

	private List<XElement> SortElements(XElement elements)
	{
		string[] obj = new string[35]
		{
			typeof(Ruler).FullName,
			typeof(RiskReward).FullName,
			typeof(RegionHighlightX).FullName,
			typeof(RegionHighlightY).FullName,
			typeof(NinjaTrader.NinjaScript.DrawingTools.Line).FullName,
			typeof(Ray).FullName,
			typeof(ExtendedLine).FullName,
			typeof(ArrowLine).FullName,
			typeof(HorizontalLine).FullName,
			typeof(VerticalLine).FullName,
			typeof(PathTool).FullName,
			typeof(FibonacciRetracements).FullName,
			typeof(FibonacciExtensions).FullName,
			typeof(FibonacciTimeExtensions).FullName,
			typeof(FibonacciCircle).FullName,
			typeof(AndrewsPitchfork).FullName,
			typeof(GannFan).FullName,
			typeof(NinjaTrader.NinjaScript.DrawingTools.RegressionChannel).FullName,
			typeof(TrendChannel).FullName,
			typeof(TimeCycles).FullName,
			typeof(NinjaTrader.NinjaScript.DrawingTools.Ellipse).FullName,
			typeof(NinjaTrader.NinjaScript.DrawingTools.Rectangle).FullName,
			typeof(Triangle).FullName,
			typeof(NinjaTrader.NinjaScript.DrawingTools.Polygon).FullName,
			"NinjaTrader.NinjaScript.DrawingTools.OrderFlowVolumeProfile",
			"NinjaTrader.NinjaScript.DrawingTools.OrderFlowVWAPDrawingTool",
			typeof(Arc).FullName,
			typeof(Text).FullName,
			typeof(ArrowUp).FullName,
			typeof(ArrowDown).FullName,
			typeof(Diamond).FullName,
			typeof(Dot).FullName,
			typeof(Square).FullName,
			typeof(TriangleUp).FullName,
			typeof(TriangleDown).FullName
		};
		List<XElement> list = new List<XElement>();
		string[] array = obj;
		foreach (string text in array)
		{
			XElement xElement = elements.Element(text);
			if (xElement != null)
			{
				list.Add(XElement.Parse(xElement.ToString()));
				xElement.Remove();
			}
		}
		list.AddRange(elements.Elements());
		return list;
	}
}

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.DrawingTools;

[CategoryDefaultExpanded(true)]
[XmlInclude(typeof(GannAngle))]
[XmlInclude(typeof(TrendLevel))]
[TypeConverter("NinjaTrader.NinjaScript.DrawingTools.PriceLevelTypeConverter")]
public class PriceLevel : NotifyPropertyChangedBase, IStrokeProvider, ICloneable
{
	private double val;

	private string name;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolsPriceLevelIsVisible", GroupName = "NinjaScriptGeneral")]
	public bool IsVisible { get; set; }

	[XmlIgnore]
	[Browsable(false)]
	public bool IsValueVisible { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolsPriceLevelLineStroke", GroupName = "NinjaScriptGeneral")]
	public Stroke Stroke { get; set; }

	[XmlIgnore]
	[Browsable(false)]
	public object Tag { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolsPriceLevelValue", GroupName = "NinjaScriptGeneral")]
	public double Value
	{
		get
		{
			return val;
		}
		set
		{
			val = value;
			if (ValueFormatFunc != null)
			{
				Name = ValueFormatFunc(value);
			}
		}
	}

	[XmlIgnore]
	[Browsable(false)]
	public Func<double, string> ValueFormatFunc { get; set; }

	[Browsable(false)]
	public string Name
	{
		get
		{
			return name;
		}
		set
		{
			if (!(name == value))
			{
				name = value;
				((NotifyPropertyChangedBase)this).OnPropertyChanged("Name");
			}
		}
	}

	public virtual object Clone()
	{
		PriceLevel priceLevel = new PriceLevel();
		CopyTo(priceLevel);
		return priceLevel;
	}

	public object AssemblyClone(Type t)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		object obj = t.Assembly.CreateInstance(t.FullName ?? "");
		PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.CanWrite)
			{
				if (propertyInfo.PropertyType == typeof(Stroke))
				{
					Stroke val = new Stroke();
					Stroke.CopyTo(val);
					propertyInfo.SetValue(obj, val, null);
				}
				else
				{
					propertyInfo.SetValue(obj, ((object)this).GetType().GetProperty(propertyInfo.Name)?.GetValue(this), null);
				}
			}
		}
		return obj;
	}

	public virtual void CopyTo(PriceLevel other)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		other.IsVisible = IsVisible;
		other.IsValueVisible = IsValueVisible;
		other.Name = Name;
		if (Stroke != null)
		{
			other.Stroke = new Stroke();
			Stroke.CopyTo(other.Stroke);
		}
		else
		{
			other.Stroke = null;
		}
		other.Tag = Tag;
		other.Value = Value;
		other.ValueFormatFunc = ValueFormatFunc;
	}

	public double GetPrice(double startPrice, double totalPriceRange, bool isInverted)
	{
		if (!isInverted)
		{
			return startPrice + Value / 100.0 * totalPriceRange;
		}
		return startPrice + (1.0 - Value / 100.0) * totalPriceRange;
	}

	public float GetY(ChartScale chartScale, double startPrice, double totalPriceRange, bool isInverted)
	{
		float num = chartScale.GetYByValue(GetPrice(startPrice, totalPriceRange, isInverted));
		float num2 = (((double)Math.Abs(num % 1f) > 0.9) ? 0f : 0.5f);
		return num - num2;
	}

	public PriceLevel()
		: this(0.0, Brushes.DimGray, 2f)
	{
	}

	public PriceLevel(double value, Brush brush)
		: this(value, brush, 2f)
	{
	}

	public PriceLevel(double value, Brush brush, float strokeWidth)
		: this(value, brush, strokeWidth, (DashStyleHelper)0, 100)
	{
	}

	public PriceLevel(double value, Brush brush, float strokeWidth, DashStyleHelper dashStyle, int opacity)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		ValueFormatFunc = (double v) => (v / 100.0).ToString("P", Globals.GeneralOptions.CurrentCulture);
		Value = value;
		IsVisible = true;
		Stroke = new Stroke(brush, dashStyle, strokeWidth, opacity);
		IsValueVisible = true;
	}
}

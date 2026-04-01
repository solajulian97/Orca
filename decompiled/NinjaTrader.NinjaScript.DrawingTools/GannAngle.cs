using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.DrawingTools;

[TypeConverter("NinjaTrader.NinjaScript.DrawingTools.GannAngleTypeConverter")]
public class GannAngle : NotifyPropertyChangedBase, IStrokeProvider, ICloneable
{
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolsGannAngleRatioX", GroupName = "NinjaScriptGeneral")]
	public double RatioX { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolsGannAngleRatioY", GroupName = "NinjaScriptGeneral")]
	public double RatioY { get; set; }

	[Browsable(false)]
	public string Name => RatioX.ToString("0", Globals.GeneralOptions.CurrentCulture) + "x" + RatioY.ToString("0", Globals.GeneralOptions.CurrentCulture);

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

	public virtual object Clone()
	{
		GannAngle gannAngle = new GannAngle();
		CopyTo(gannAngle);
		return gannAngle;
	}

	public virtual void CopyTo(GannAngle other)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		other.IsVisible = IsVisible;
		other.IsValueVisible = IsValueVisible;
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
		other.RatioX = RatioX;
		other.RatioY = RatioY;
	}

	public GannAngle()
		: this(1.0, 1.0, Brushes.Gray)
	{
	}

	public GannAngle(double ratioX, double ratioY, Brush strokeBrush)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		IsValueVisible = false;
		RatioX = ratioX;
		RatioY = ratioY;
		Stroke = new Stroke(strokeBrush, 2f);
		IsVisible = true;
	}
}

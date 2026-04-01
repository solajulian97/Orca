using System;
using System.ComponentModel;
using NinjaTrader.Core;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

public class DrawingToolIndicatorTypeConverter : TypeConverter
{
	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
	{
		PropertyDescriptorCollection properties = ((component is IndicatorBase) ? TypeDescriptor.GetConverter(typeof(IndicatorBase)) : TypeDescriptor.GetConverter(typeof(DrawingTool))).GetProperties(context, component, attrs);
		if (properties == null)
		{
			return null;
		}
		PropertyDescriptorCollection propertyDescriptorCollection = new PropertyDescriptorCollection(null);
		foreach (PropertyDescriptor item in properties)
		{
			if (!item.IsBrowsable || item.IsReadOnly)
			{
				continue;
			}
			bool flag;
			switch (item.Name)
			{
			case "IsAutoScale":
			case "DisplayInDataBox":
			case "MaximumBarsLookBack":
			case "Calculate":
			case "PaintPriceMarkers":
			case "Displacement":
			case "ScaleJustification":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				continue;
			}
			if (item.Name == "SelectedTypes")
			{
				int num = 1;
				Type[] derivedTypes = Globals.AssemblyRegistry.GetDerivedTypes(typeof(DrawingTool));
				foreach (Type type in derivedTypes)
				{
					object obj = type.Assembly.CreateInstance(type.FullName ?? "");
					DrawingTool val = (DrawingTool)((obj is DrawingTool) ? obj : null);
					if (val != null && val.DisplayOnChartsMenus)
					{
						DrawingToolPropertyDescriptor value = new DrawingToolPropertyDescriptor(type, ((NinjaScript)val).Name, num);
						propertyDescriptorCollection.Add(value);
						num++;
					}
				}
			}
			else
			{
				propertyDescriptorCollection.Add(item);
			}
		}
		return propertyDescriptorCollection;
	}
}

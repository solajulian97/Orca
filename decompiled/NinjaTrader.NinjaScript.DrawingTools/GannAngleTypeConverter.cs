using System;
using System.ComponentModel;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class GannAngleTypeConverter : TypeConverter
{
	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
	{
		PropertyDescriptorCollection propertyDescriptorCollection = (base.GetPropertiesSupported(context) ? base.GetProperties(context, component, attrs) : TypeDescriptor.GetProperties(component, attrs));
		if (!(component is GannAngle gannAngle) || propertyDescriptorCollection == null)
		{
			return null;
		}
		PropertyDescriptorCollection propertyDescriptorCollection2 = new PropertyDescriptorCollection(null);
		foreach (PropertyDescriptor item in propertyDescriptorCollection)
		{
			if ((item.Name != "Value" || gannAngle.IsValueVisible) && item.IsBrowsable)
			{
				propertyDescriptorCollection2.Add(item);
			}
		}
		return propertyDescriptorCollection2;
	}

	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}

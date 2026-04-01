using System;
using System.ComponentModel;
using System.Linq;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class PriceLevelTypeConverter : TypeConverter
{
	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
	{
		PropertyDescriptorCollection propertyDescriptorCollection = (base.GetPropertiesSupported(context) ? base.GetProperties(context, component, attrs) : TypeDescriptor.GetProperties(component, attrs));
		PriceLevel priceLevel = component as PriceLevel;
		if (priceLevel == null || propertyDescriptorCollection == null)
		{
			return null;
		}
		PropertyDescriptorCollection propertyDescriptorCollection2 = new PropertyDescriptorCollection(null);
		foreach (PropertyDescriptor item in from PropertyDescriptor property in propertyDescriptorCollection
			where (property.Name != "Value" || priceLevel.IsValueVisible) && property.IsBrowsable
			select property)
		{
			propertyDescriptorCollection2.Add(item);
		}
		return propertyDescriptorCollection2;
	}

	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}

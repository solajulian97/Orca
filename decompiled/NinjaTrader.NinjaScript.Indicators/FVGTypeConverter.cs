using System;
using System.ComponentModel;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

public class FVGTypeConverter : IndicatorBaseConverter
{
	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
	{
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		PropertyDescriptorCollection propertyDescriptorCollection = (((IndicatorBaseConverter)this).GetPropertiesSupported(context) ? ((IndicatorBaseConverter)this).GetProperties(context, value, attributes) : TypeDescriptor.GetProperties(value, attributes));
		if (((FVG)value).ExtendUntil == 3)
		{
			return propertyDescriptorCollection;
		}
		PropertyDescriptorCollection propertyDescriptorCollection2 = new PropertyDescriptorCollection(null);
		if (propertyDescriptorCollection != null)
		{
			foreach (PropertyDescriptor item in propertyDescriptorCollection)
			{
				propertyDescriptorCollection2.Add((item.Name == "BarsToExtend") ? ((PropertyDescriptor)new PropertyDescriptorExtended(item, (Func<object, object>)((object _) => value), (string)null, new Attribute[1]
				{
					new BrowsableAttribute(browsable: false)
				})) : item);
			}
		}
		return propertyDescriptorCollection2;
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

public class COTTypeConverter : IndicatorBaseConverter
{
	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
	{
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		PropertyDescriptorCollection propertyDescriptorCollection = (((IndicatorBaseConverter)this).GetPropertiesSupported(context) ? ((IndicatorBaseConverter)this).GetProperties(context, value, attributes) : TypeDescriptor.GetProperties(value, attributes));
		int number = ((COT)value).Number;
		if (number == 5)
		{
			return propertyDescriptorCollection;
		}
		PropertyDescriptorCollection propertyDescriptorCollection2 = new PropertyDescriptorCollection(null);
		List<string> list = new List<string>();
		for (int i = number + 1; i <= 5; i++)
		{
			list.Add("CotReport" + i);
			list.Add("Plot" + (i - 1));
		}
		if (propertyDescriptorCollection != null)
		{
			foreach (PropertyDescriptor item in propertyDescriptorCollection)
			{
				propertyDescriptorCollection2.Add(list.Contains(item.Name) ? ((PropertyDescriptor)new PropertyDescriptorExtended(item, (Func<object, object>)((object _) => value), (string)null, new Attribute[1]
				{
					new BrowsableAttribute(browsable: false)
				})) : item);
			}
		}
		return propertyDescriptorCollection2;
	}
}

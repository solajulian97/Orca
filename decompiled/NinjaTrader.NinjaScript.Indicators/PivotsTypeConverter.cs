using System;
using System.ComponentModel;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

public class PivotsTypeConverter : IndicatorBaseConverter
{
	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
	{
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		PropertyDescriptorCollection propertyDescriptorCollection = (((IndicatorBaseConverter)this).GetPropertiesSupported(context) ? ((IndicatorBaseConverter)this).GetProperties(context, value, attributes) : TypeDescriptor.GetProperties(value, attributes));
		if (((Pivots)value).PriorDayHlc == HLCCalculationMode.UserDefinedValues)
		{
			return propertyDescriptorCollection;
		}
		PropertyDescriptorCollection propertyDescriptorCollection2 = new PropertyDescriptorCollection(null);
		if (propertyDescriptorCollection != null)
		{
			foreach (PropertyDescriptor item in propertyDescriptorCollection)
			{
				PropertyDescriptorCollection propertyDescriptorCollection3 = propertyDescriptorCollection2;
				bool flag;
				switch (item.Name)
				{
				case "UserDefinedClose":
				case "UserDefinedHigh":
				case "UserDefinedLow":
					flag = true;
					break;
				default:
					flag = false;
					break;
				}
				propertyDescriptorCollection3.Add(flag ? ((PropertyDescriptor)new PropertyDescriptorExtended(item, (Func<object, object>)((object _) => value), (string)null, new Attribute[1]
				{
					new BrowsableAttribute(browsable: false)
				})) : item);
			}
		}
		return propertyDescriptorCollection2;
	}
}

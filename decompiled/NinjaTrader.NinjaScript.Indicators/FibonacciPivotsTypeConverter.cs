using System;
using System.ComponentModel;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.Indicators;

public class FibonacciPivotsTypeConverter : IndicatorBaseConverter
{
	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
	{
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Expected O, but got Unknown
		PropertyDescriptorCollection propertyDescriptorCollection = (((IndicatorBaseConverter)this).GetPropertiesSupported(context) ? ((IndicatorBaseConverter)this).GetProperties(context, value, attributes) : TypeDescriptor.GetProperties(value, attributes));
		if (((FibonacciPivots)value).PriorDayHlc == HLCCalculationMode.UserDefinedValues)
		{
			return propertyDescriptorCollection;
		}
		PropertyDescriptorCollection propertyDescriptorCollection2 = new PropertyDescriptorCollection(null);
		foreach (PropertyDescriptor item in propertyDescriptorCollection)
		{
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
			if (flag)
			{
				propertyDescriptorCollection2.Add((PropertyDescriptor)new PropertyDescriptorExtended(item, (Func<object, object>)((object _) => value), (string)null, new Attribute[1]
				{
					new BrowsableAttribute(browsable: false)
				}));
			}
			else
			{
				propertyDescriptorCollection2.Add(item);
			}
		}
		return propertyDescriptorCollection2;
	}
}

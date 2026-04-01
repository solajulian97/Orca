using System;
using System.ComponentModel;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class TSTrendConverter : IndicatorBaseConverter
{
	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
	{
		TSTrend obj = component as TSTrend;
		PropertyDescriptorCollection propertyDescriptorCollection = (((IndicatorBaseConverter)this).GetPropertiesSupported(context) ? ((IndicatorBaseConverter)this).GetProperties(context, component, attrs) : TypeDescriptor.GetProperties(component, attrs));
		if (obj == null || propertyDescriptorCollection == null)
		{
			return propertyDescriptorCollection;
		}
		PropertyDescriptorCollection propertyDescriptorCollection2 = new PropertyDescriptorCollection(null);
		foreach (PropertyDescriptor item in propertyDescriptorCollection)
		{
			if (!(item.Name != "AboveAsk") || !(item.Name != "AtAsk") || !(item.Name != "AtBid") || !(item.Name != "BarWidth") || !(item.Name != "BelowBid") || !(item.Name != "Between") || !(item.Name != "Name"))
			{
				propertyDescriptorCollection2.Add(item);
			}
		}
		return propertyDescriptorCollection2;
	}
}

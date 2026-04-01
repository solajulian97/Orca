using System;
using System.ComponentModel;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class ChartNetChangeConverter : IndicatorBaseConverter
{
	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
	{
		PropertyDescriptorCollection propertyDescriptorCollection = (((IndicatorBaseConverter)this).GetPropertiesSupported(context) ? ((IndicatorBaseConverter)this).GetProperties(context, component, attrs) : TypeDescriptor.GetProperties(component, attrs));
		if (!(component is ChartNetChange) || propertyDescriptorCollection == null)
		{
			return propertyDescriptorCollection;
		}
		PropertyDescriptorCollection propertyDescriptorCollection2 = new PropertyDescriptorCollection(null);
		foreach (PropertyDescriptor item in propertyDescriptorCollection)
		{
			if (!(item.Name != "DownArea") || !(item.Name != "DownOutline") || !(item.Name != "IsVisible") || !(item.Name != "Name") || !(item.Name != "Opacity") || !(item.Name != "UpArea") || !(item.Name != "UpOutline"))
			{
				propertyDescriptorCollection2.Add(item);
			}
		}
		return propertyDescriptorCollection2;
	}
}

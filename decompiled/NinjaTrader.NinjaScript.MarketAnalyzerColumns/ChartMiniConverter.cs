using System;
using System.ComponentModel;

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns;

public class ChartMiniConverter : IndicatorBaseConverter
{
	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
	{
		ChartMini obj = component as ChartMini;
		PropertyDescriptorCollection propertyDescriptorCollection = (((IndicatorBaseConverter)this).GetPropertiesSupported(context) ? ((IndicatorBaseConverter)this).GetProperties(context, component, attrs) : TypeDescriptor.GetProperties(component, attrs));
		if (obj == null || propertyDescriptorCollection == null)
		{
			return propertyDescriptorCollection;
		}
		PropertyDescriptorCollection propertyDescriptorCollection2 = new PropertyDescriptorCollection(null);
		foreach (PropertyDescriptor item in propertyDescriptorCollection)
		{
			if (!(item.Name != "Color") || !(item.Name != "Opacity") || !(item.Name != "OutlineBrush") || !(item.Name != "Span") || !(item.Name != "Name") || !(item.Name != "IsVisible"))
			{
				propertyDescriptorCollection2.Add(item);
			}
		}
		return propertyDescriptorCollection2;
	}
}

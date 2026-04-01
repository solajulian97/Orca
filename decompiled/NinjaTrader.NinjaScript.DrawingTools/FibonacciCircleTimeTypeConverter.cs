using System;
using System.ComponentModel;
using NinjaTrader.Gui.DrawingTools;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class FibonacciCircleTimeTypeConverter : DrawingToolPropertiesConverter
{
	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
	{
		PropertyDescriptorCollection propertyDescriptorCollection = (((DrawingToolPropertiesConverter)this).GetPropertiesSupported(context) ? ((DrawingToolPropertiesConverter)this).GetProperties(context, value, attributes) : TypeDescriptor.GetProperties(value, attributes));
		PropertyDescriptorCollection propertyDescriptorCollection2 = new PropertyDescriptorCollection(null);
		if (propertyDescriptorCollection != null)
		{
			foreach (PropertyDescriptor item in propertyDescriptorCollection)
			{
				if (item.Name != "IsExtendedLinesRight" && item.Name != "IsExtendedLinesLeft" && item.Name != "TextLocation")
				{
					propertyDescriptorCollection2.Add(item);
				}
			}
		}
		return propertyDescriptorCollection2;
	}
}

using System;
using System.ComponentModel;
using NinjaTrader.Gui;
using NinjaTrader.Gui.DrawingTools;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class RegressionChannelTypeConverter : DrawingToolPropertiesConverter
{
	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
	{
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Expected O, but got Unknown
		PropertyDescriptorCollection propertyDescriptorCollection = (((DrawingToolPropertiesConverter)this).GetPropertiesSupported(context) ? ((DrawingToolPropertiesConverter)this).GetProperties(context, value, attributes) : TypeDescriptor.GetProperties(value, attributes));
		if (((RegressionChannel)value).ChannelType == RegressionChannel.RegressionChannelType.StandardDeviation)
		{
			return propertyDescriptorCollection;
		}
		PropertyDescriptorCollection propertyDescriptorCollection2 = new PropertyDescriptorCollection(null);
		if (propertyDescriptorCollection != null)
		{
			foreach (PropertyDescriptor item in propertyDescriptorCollection)
			{
				string name = item.Name;
				if ((name == "StandardDeviationUpperDistance" || name == "StandardDeviationLowerDistance") ? true : false)
				{
					propertyDescriptorCollection2.Add((PropertyDescriptor)new PropertyDescriptorExtended(item, (Func<object, object>)((object _) => value), (string)null, new Attribute[1]
					{
						new ReadOnlyAttribute(isReadOnly: true)
					}));
				}
				else
				{
					propertyDescriptorCollection2.Add(item);
				}
			}
		}
		return propertyDescriptorCollection2;
	}
}

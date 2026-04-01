using System;
using System.ComponentModel;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.ShareServices;

public class MailTypeConverter : TypeConverter
{
	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object component, Attribute[] attrs)
	{
		Mail mail = component as Mail;
		PropertyDescriptorCollection propertyDescriptorCollection = new PropertyDescriptorCollection(null);
		TypeConverter converter = TypeDescriptor.GetConverter(typeof(ShareService));
		if (mail == null || !((ShareService)mail).UseOAuth)
		{
			return converter.GetProperties(context, component, attrs);
		}
		PropertyDescriptorCollection properties = converter.GetProperties(context, component, attrs);
		if (properties != null)
		{
			foreach (PropertyDescriptor item in properties)
			{
				bool flag;
				switch (item.Name)
				{
				case "Password":
				case "Port":
				case "Server":
				case "UseSSL":
				case "UserName":
				case "FromMailAddress":
					flag = true;
					break;
				default:
					flag = false;
					break;
				}
				if (!flag && (!(item.Name == "SenderDisplayName") || !string.Equals(mail?.SelectedPreconfiguredSetting, Resource.ShareMailPreconfiguredOutlook)))
				{
					propertyDescriptorCollection.Add(item);
				}
			}
		}
		return propertyDescriptorCollection;
	}

	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}

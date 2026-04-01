using System.Collections.Generic;
using System.ComponentModel;
using NinjaTrader.Core;

namespace NinjaTrader.NinjaScript.ShareServices;

public class TextMessageEmailConverter : StringConverter
{
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		List<string> list = new List<string>();
		lock (Globals.GeneralOptions.ShareServices)
		{
			foreach (ShareService shareService in Globals.GeneralOptions.ShareServices)
			{
				if (((object)shareService).GetType().Name == "Mail")
				{
					list.Add(shareService.Name);
				}
			}
		}
		return new StandardValuesCollection(list);
	}

	public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
	{
		return true;
	}

	public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}

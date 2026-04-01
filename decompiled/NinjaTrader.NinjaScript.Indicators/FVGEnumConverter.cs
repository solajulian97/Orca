using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

public class FVGEnumConverter : TypeConverter
{
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		return new StandardValuesCollection(new List<string>
		{
			Resource.FVGFilled,
			Resource.FVGPartiallyFilled,
			Resource.FVGBarsSpecified
		});
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		string text = value?.ToString();
		if (text == null)
		{
			goto IL_0032;
		}
		int num;
		if (text == Resource.FVGFilled)
		{
			num = 1;
		}
		else
		{
			if (!(text == Resource.FVGPartiallyFilled))
			{
				goto IL_0032;
			}
			num = 2;
		}
		goto IL_0034;
		IL_0034:
		return num;
		IL_0032:
		num = 3;
		goto IL_0034;
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		string text = value.ToString();
		if (!(text == "1"))
		{
			if (text == "2")
			{
				return Resource.FVGPartiallyFilled;
			}
			return Resource.FVGBarsSpecified;
		}
		return Resource.FVGFilled;
	}

	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		return true;
	}

	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		return true;
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

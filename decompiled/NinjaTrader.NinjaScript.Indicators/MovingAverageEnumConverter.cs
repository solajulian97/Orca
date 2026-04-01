using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace NinjaTrader.NinjaScript.Indicators;

public class MovingAverageEnumConverter : TypeConverter
{
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		return new StandardValuesCollection(new List<string> { "EMA", "HMA", "SMA", "TMA", "TEMA", "WMA" });
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		int num = 3;
		switch (value?.ToString())
		{
		case "EMA":
			num = 1;
			break;
		case "HMA":
			num = 2;
			break;
		case "SMA":
			num = 3;
			break;
		case "TMA":
			num = 4;
			break;
		case "TEMA":
			num = 5;
			break;
		case "WMA":
			num = 6;
			break;
		}
		return num;
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		string result = "SMA";
		switch (value.ToString())
		{
		case "1":
			result = "EMA";
			break;
		case "2":
			result = "HMA";
			break;
		case "3":
			result = "SMA";
			break;
		case "4":
			result = "TMA";
			break;
		case "5":
			result = "TEMA";
			break;
		case "6":
			result = "WMA";
			break;
		}
		return result;
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

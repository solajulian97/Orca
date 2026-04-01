using System;
using Infralution.Localization.Wpf;

namespace NinjaTrader.Custom;

internal class ResourceEnumConverter : ResourceEnumConverter
{
	public ResourceEnumConverter(Type type)
		: base(type, Resource.ResourceManager)
	{
	}
}

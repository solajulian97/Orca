using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using NinjaTrader.Core;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.Indicators;

public class DrawingToolPropertyDescriptor : PropertyDescriptor
{
	private readonly int order;

	private readonly Type type;

	public override AttributeCollection Attributes => new AttributeCollection(new DisplayAttribute
	{
		Name = DisplayName,
		GroupName = Resource.NinjaScriptDrawingTools,
		Order = order
	});

	public override Type ComponentType => typeof(DrawingToolTile);

	public override string DisplayName { get; }

	public override bool IsReadOnly => false;

	public override string Name { get; }

	public override Type PropertyType => typeof(bool);

	public DrawingToolPropertyDescriptor(Type type, string displayName, int order)
		: base(type.FullName ?? "", null)
	{
		Name = type.FullName ?? "";
		DisplayName = displayName;
		this.order = order;
		this.type = type;
	}

	public override bool CanResetValue(object component)
	{
		return true;
	}

	public override bool ShouldSerializeValue(object component)
	{
		return true;
	}

	public override object GetValue(object component)
	{
		return (component as DrawingToolTile)?.SelectedTypes.Element(Name) != null;
	}

	public override void ResetValue(object component)
	{
	}

	public override void SetValue(object component, object value)
	{
		if (component is DrawingToolTile drawingToolTile)
		{
			bool flag = (bool)value;
			if (flag && drawingToolTile.SelectedTypes.Element(Name) == null)
			{
				XElement xElement = new XElement(Name);
				xElement.Add(new XAttribute("Assembly", Globals.AssemblyRegistry.IsNinjaTraderCustomAssembly(type) ? "NinjaTrader.Custom" : type.Assembly.GetName().Name));
				drawingToolTile.SelectedTypes.Add(xElement);
			}
			else if (!flag && drawingToolTile.SelectedTypes.Element(Name) != null)
			{
				drawingToolTile.SelectedTypes.Element(Name)?.Remove();
			}
		}
	}
}

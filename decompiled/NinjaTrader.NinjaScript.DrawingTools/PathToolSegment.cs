using System;
using System.ComponentModel;
using System.Reflection;

namespace NinjaTrader.NinjaScript.DrawingTools;

public class PathToolSegment : ICloneable
{
	[Browsable(false)]
	public ChartAnchor EndAnchor { get; set; }

	[Browsable(false)]
	public string Name { get; set; }

	[Browsable(false)]
	public ChartAnchor StartAnchor { get; set; }

	public object AssemblyClone(Type t)
	{
		object obj = t.Assembly.CreateInstance(t.FullName ?? "");
		PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.CanWrite)
			{
				propertyInfo.SetValue(obj, GetType().GetProperty(propertyInfo.Name)?.GetValue(this), null);
			}
		}
		return obj;
	}

	public virtual object Clone()
	{
		PathToolSegment pathToolSegment = new PathToolSegment();
		CopyTo(pathToolSegment);
		return pathToolSegment;
	}

	public virtual void CopyTo(PathToolSegment other)
	{
		StartAnchor.CopyDataValues(other.StartAnchor);
		EndAnchor.CopyDataValues(other.EndAnchor);
		other.Name = Name;
	}

	public PathToolSegment()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		StartAnchor = new ChartAnchor();
		EndAnchor = new ChartAnchor();
		Name = string.Empty;
	}

	public PathToolSegment(ChartAnchor startAnchor, ChartAnchor endAnchor, string name)
	{
		StartAnchor = startAnchor;
		EndAnchor = endAnchor;
		Name = name;
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class PathToolSegmentContainer : DrawingTool
{
	[Browsable(false)]
	[SkipOnCopyTo(true)]
	public List<PathToolSegment> PathToolSegments { get; set; } = new List<PathToolSegment>();

	public override void CopyTo(NinjaScript ninjaScript)
	{
		((DrawingTool)this).CopyTo(ninjaScript);
		PropertyInfo property = ((object)ninjaScript).GetType().GetProperty("PathToolSegments");
		if (property == null || !(property.GetValue(ninjaScript) is IList list))
		{
			return;
		}
		list.Clear();
		foreach (PathToolSegment pathToolSegment in PathToolSegments)
		{
			try
			{
				object obj = pathToolSegment.AssemblyClone(((object)ninjaScript).GetType().Assembly.GetType(typeof(PathToolSegment).FullName ?? ""));
				if (obj != null)
				{
					list.Add(obj);
				}
			}
			catch (ArgumentException)
			{
				object value = pathToolSegment.Clone();
				list.Add(value);
			}
			catch
			{
			}
		}
	}
}

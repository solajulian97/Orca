using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class GannAngleContainer : DrawingTool
{
	[PropertyEditor("NinjaTrader.Gui.Tools.CollectionEditor")]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolsGannAngles", Prompt = "NinjaScriptDrawingToolsGannAnglesPrompt", GroupName = "NinjaScriptGeneral", Order = 99)]
	[SkipOnCopyTo(true)]
	public List<GannAngle> GannAngles { get; set; } = new List<GannAngle>();

	public override void CopyTo(NinjaScript ninjaScript)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		((DrawingTool)this).CopyTo(ninjaScript);
		PropertyInfo property = ((object)ninjaScript).GetType().GetProperty("GannAngles");
		if (property == null || !(property.GetValue(ninjaScript) is IList list))
		{
			return;
		}
		list.Clear();
		foreach (GannAngle gannAngle in GannAngles)
		{
			try
			{
				object obj = gannAngle.AssemblyClone(Globals.AssemblyRegistry.GetType(typeof(GannAngle).FullName));
				if (obj != null)
				{
					list.Add(obj);
				}
			}
			catch (ArgumentException)
			{
				object obj2 = gannAngle.Clone();
				IStrokeProvider val = (IStrokeProvider)((obj2 is IStrokeProvider) ? obj2 : null);
				if (val != null)
				{
					Stroke stroke = val.Stroke;
					val.Stroke = new Stroke();
					stroke.CopyTo(val.Stroke);
				}
				list.Add(obj2);
			}
			catch
			{
			}
		}
	}
}

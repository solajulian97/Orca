using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class PriceLevelContainer : DrawingTool
{
	[PropertyEditor("NinjaTrader.Gui.Tools.CollectionEditor")]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolsPriceLevels", Prompt = "NinjaScriptDrawingToolsPriceLevelsPrompt", GroupName = "NinjaScriptLines", Order = 99)]
	[SkipOnCopyTo(true)]
	public List<PriceLevel> PriceLevels { get; set; } = new List<PriceLevel>();

	public override void CopyTo(NinjaScript ninjaScript)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		((DrawingTool)this).CopyTo(ninjaScript);
		PropertyInfo property = ((object)ninjaScript).GetType().GetProperty("PriceLevels");
		if (property == null || !(property.GetValue(ninjaScript) is IList list))
		{
			return;
		}
		list.Clear();
		foreach (PriceLevel priceLevel in PriceLevels)
		{
			try
			{
				object obj = priceLevel.AssemblyClone(Globals.AssemblyRegistry.GetType(typeof(PriceLevel).FullName));
				if (obj != null)
				{
					list.Add(obj);
				}
			}
			catch (ArgumentException)
			{
				object obj2 = priceLevel.Clone();
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

	public void SetAllPriceLevelsRenderTarget()
	{
		if (PriceLevels == null)
		{
			return;
		}
		foreach (PriceLevel item in PriceLevels.Where((PriceLevel pl) => pl.Stroke != null))
		{
			item.Stroke.RenderTarget = ((ChartObject)this).RenderTarget;
		}
	}
}

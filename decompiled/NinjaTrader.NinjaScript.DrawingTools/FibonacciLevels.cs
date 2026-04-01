using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class FibonacciLevels : PriceLevelContainer
{
	protected const int CursorSensitivity = 15;

	private int priceLevelOpacity;

	protected ChartAnchor editingAnchor;

	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolFibonacciLevelsBaseAnchorLineStroke", GroupName = "NinjaScriptLines", Order = 1)]
	public Stroke AnchorLineStroke { get; set; }

	[Display(Order = 1)]
	public ChartAnchor StartAnchor { get; set; }

	[Display(Order = 2)]
	public ChartAnchor EndAnchor { get; set; }

	[Range(0, 100)]
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolPriceLevelsOpacity", GroupName = "NinjaScriptGeneral")]
	public int PriceLevelOpacity
	{
		get
		{
			return priceLevelOpacity;
		}
		set
		{
			priceLevelOpacity = Math.Max(0, Math.Min(100, value));
		}
	}

	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[2] { StartAnchor, EndAnchor };

	public override bool SupportsAlerts => true;

	public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
	{
		if (base.PriceLevels == null || base.PriceLevels.Count == 0)
		{
			yield break;
		}
		foreach (PriceLevel priceLevel in base.PriceLevels)
		{
			yield return new AlertConditionItem
			{
				Name = priceLevel.Name,
				ShouldOnlyDisplayName = true,
				Tag = priceLevel
			};
		}
	}
}

using System.Collections.Generic;
using NinjaTrader.Custom;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Vertical Line IDrawingTool.
/// </summary>
public class VerticalLine : Line
{
	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[1] { base.StartAnchor };

	public override object Icon => Icons.DrawVertLineTool;

	public override bool SupportsAlerts => false;

	protected override void OnStateChange()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		base.OnStateChange();
		if ((int)((NinjaScript)this).State == 1)
		{
			base.EndAnchor.IsBrowsable = false;
			base.LineType = ChartLineType.VerticalLine;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolVerticalLine;
			base.StartAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchor;
			base.StartAnchor.IsYPropertyVisible = false;
		}
	}
}

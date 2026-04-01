using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Triangle Down IDrawingTool.
/// </summary>
public class TriangleDown : TriangleBase
{
	public override object Icon => Icons.DrawTriangleDown;

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		if ((int)((NinjaScript)this).State == 1)
		{
			base.Anchor = new ChartAnchor
			{
				DisplayName = Resource.NinjaScriptDrawingToolAnchor,
				IsEditing = true
			};
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolsChartTriangleDownMarkerName;
			base.AreaBrush = Brushes.Crimson;
			base.OutlineBrush = Brushes.DarkGray;
			base.IsUpTriangle = false;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			((DrawingTool)this).Dispose();
		}
	}
}

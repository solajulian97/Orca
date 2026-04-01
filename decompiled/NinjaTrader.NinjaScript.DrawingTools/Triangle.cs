using NinjaTrader.Custom;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Triangle IDrawingTool.
/// </summary>
public class Triangle : ShapeBase
{
	public override object Icon => Icons.DrawTriangle;

	protected override void OnStateChange()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		base.OnStateChange();
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolTriangle;
			base.ShapeType = ChartShapeType.Triangle;
			base.MiddleAnchor.IsBrowsable = true;
		}
	}
}

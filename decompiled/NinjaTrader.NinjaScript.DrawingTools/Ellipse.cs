using NinjaTrader.Custom;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding an Ellipse IDrawingTool.
/// </summary>
public class Ellipse : ShapeBase
{
	public override object Icon => Icons.DrawElipse;

	protected override void OnStateChange()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		base.OnStateChange();
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolEllipse;
			base.ShapeType = ChartShapeType.Ellipse;
		}
	}
}

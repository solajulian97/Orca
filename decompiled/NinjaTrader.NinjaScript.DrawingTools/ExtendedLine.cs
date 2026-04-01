using NinjaTrader.Custom;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding an Extended Line IDrawingTool.
/// </summary>
public class ExtendedLine : Line
{
	public override object Icon => Icons.DrawExtendedLineTo;

	protected override void OnStateChange()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		base.OnStateChange();
		if ((int)((NinjaScript)this).State == 1)
		{
			base.LineType = ChartLineType.ExtendedLine;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolExtendedLine;
		}
	}
}

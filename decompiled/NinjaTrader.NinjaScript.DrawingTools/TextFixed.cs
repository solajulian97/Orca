using System.ComponentModel.DataAnnotations;
using System.Windows;
using NinjaTrader.Custom;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Text Fixed IDrawingTool.
/// </summary>
public class TextFixed : TextFixedBase
{
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptDrawingToolTextFixedTextPosition", GroupName = "NinjaScriptGeneral")]
	public TextPosition TextPosition { get; set; }

	protected override Point GetTextDrawingPosition(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (cachedTextLayout == null)
		{
			return new Point(-1.0, -1.0);
		}
		float num = 0f;
		float num2 = 0f;
		float width = cachedTextLayout.Metrics.Width;
		float height = cachedTextLayout.Metrics.Height;
		switch (TextPosition)
		{
		case TextPosition.BottomLeft:
			num = (float)chartPanel.X + 10.5f;
			num2 = (float)(chartPanel.Y + chartPanel.H) - height - 10.5f * (float)PaddingMultiplier(chartControl, chartPanel, top: false);
			break;
		case TextPosition.BottomRight:
			num = (float)(chartPanel.X + chartPanel.W) - 10.5f - width;
			num2 = (float)(chartPanel.Y + chartPanel.H) - height - 10.5f;
			break;
		case TextPosition.Center:
			num = (float)chartPanel.X + (float)chartPanel.W / 2f - width / 2f;
			num2 = (float)chartPanel.Y + (float)chartPanel.H / 2f - height / 2f;
			break;
		case TextPosition.TopLeft:
			num = (float)chartPanel.X + 10.5f;
			num2 = (float)chartPanel.Y + 21f;
			break;
		case TextPosition.TopRight:
			num = (float)(chartPanel.X + chartPanel.W) - 10.5f - width;
			num2 = chartPanel.Y + (int)(10.5f * (float)PaddingMultiplier(chartControl, chartPanel, top: true));
			break;
		}
		return new Point(num, num2);
	}
}

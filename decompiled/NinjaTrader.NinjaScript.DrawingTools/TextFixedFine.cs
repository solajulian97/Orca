using System.ComponentModel.DataAnnotations;
using System.Windows;
using NinjaTrader.Custom;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Text Fixed IDrawingTool.
/// </summary>
public class TextFixedFine : TextFixedBase
{
	[Display(ResourceType = typeof(Resource), Name = "NinjaScriptTextPosition", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 70)]
	public TextPositionFine TextPositionFine { get; set; }

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
		switch (TextPositionFine)
		{
		case TextPositionFine.BottomLeft:
			num = (float)chartPanel.X + 10.5f;
			num2 = (float)(chartPanel.Y + chartPanel.H) - height - 10.5f * (float)PaddingMultiplier(chartControl, chartPanel, top: false);
			break;
		case TextPositionFine.BottomMiddle:
			num = (float)(chartPanel.X + chartPanel.W / 2) - width / 2f;
			num2 = (float)(chartPanel.Y + chartPanel.H) - height - 10.5f;
			break;
		case TextPositionFine.BottomRight:
			num = (float)(chartPanel.X + chartPanel.W) - 10.5f - width;
			num2 = (float)(chartPanel.Y + chartPanel.H) - height - 10.5f;
			break;
		case TextPositionFine.MiddleLeft:
			num = (float)chartPanel.X + 10.5f;
			num2 = (float)(chartPanel.Y + chartPanel.H / 2) - height / 2f;
			break;
		case TextPositionFine.MiddleRight:
			num = (float)(chartPanel.X + chartPanel.W) - 10.5f - width;
			num2 = (float)(chartPanel.Y + chartPanel.H / 2) - height / 2f;
			break;
		case TextPositionFine.TopLeft:
			num = (float)chartPanel.X + 10.5f;
			num2 = (float)chartPanel.Y + 21f;
			break;
		case TextPositionFine.TopMiddle:
			num = (float)(chartPanel.X + chartPanel.W / 2) - width / 2f;
			num2 = (float)chartPanel.Y + 10.5f;
			break;
		case TextPositionFine.TopRight:
			num = (float)(chartPanel.X + chartPanel.W) - 10.5f - width;
			num2 = chartPanel.Y + (int)(10.5f * (float)PaddingMultiplier(chartControl, chartPanel, top: true));
			break;
		}
		return new Point(num, num2);
	}
}

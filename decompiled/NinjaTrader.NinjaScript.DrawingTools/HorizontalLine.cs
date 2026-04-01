using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.DrawingTools;

/// <summary>
/// Represents an interface that exposes information regarding a Horizontal Line IDrawingTool.
/// </summary>
public class HorizontalLine : Line
{
	public override IEnumerable<ChartAnchor> Anchors => (IEnumerable<ChartAnchor>)(object)new ChartAnchor[1] { base.StartAnchor };

	public override object Icon => Icons.DrawHorizLineTool;

	[Display(ResourceType = typeof(Resource), GroupName = "NinjaScriptGeneral", Name = "NinjaScriptDrawingToolPriceMarker", Order = 1000)]
	public bool IsPriceMarkerVisible { get; set; }

	protected override void OnStateChange()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		base.OnStateChange();
		if ((int)((NinjaScript)this).State == 1)
		{
			base.EndAnchor.IsBrowsable = false;
			base.LineType = ChartLineType.HorizontalLine;
			((NinjaScript)this).Name = Resource.NinjaScriptDrawingToolHorizontalLine;
			base.StartAnchor.DisplayName = Resource.NinjaScriptDrawingToolAnchor;
			base.StartAnchor.IsXPropertiesVisible = false;
		}
	}

	public override bool GetPriceMarkersSupported()
	{
		return IsPriceMarkerVisible;
	}

	public override Dictionary<double, Brush> GetPriceMarkers(ChartControl chartControl, ChartPanel chartPanel)
	{
		if (IsPriceMarkerVisible)
		{
			return new Dictionary<double, Brush> { 
			{
				base.StartAnchor.Price,
				base.Stroke.Brush
			} };
		}
		return null;
	}
}

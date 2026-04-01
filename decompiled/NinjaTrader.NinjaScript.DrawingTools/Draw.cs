using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.DrawingTools;

public static class Draw
{
	private static readonly Brush defaultRegionBrush = Brushes.Goldenrod;

	private const int defaultRegionOpacity = 25;

	private static AndrewsPitchfork AndrewsPitchforkCore(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, DateTime anchor1Time, double anchor1Y, int anchor2BarsAgo, DateTime anchor2Time, double anchor2Y, int anchor3BarsAgo, DateTime anchor3Time, double anchor3Y, Brush brush, DashStyleHelper dashStyle, int width, bool isGlobal, string templateName)
	{
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Expected O, but got Unknown
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty", "tag");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			char globalDrawingToolTagPrefix = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix;
			tag = globalDrawingToolTagPrefix + tag;
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(AndrewsPitchfork), tag, templateName) is AndrewsPitchfork andrewsPitchfork))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)andrewsPitchfork, tag, isAutoScale, owner, isGlobal);
		ChartAnchor val = DrawingTool.CreateChartAnchor(owner, anchor1BarsAgo, anchor1Time, anchor1Y);
		ChartAnchor val2 = DrawingTool.CreateChartAnchor(owner, anchor2BarsAgo, anchor2Time, anchor2Y);
		ChartAnchor obj = DrawingTool.CreateChartAnchor(owner, anchor3BarsAgo, anchor3Time, anchor3Y);
		val.CopyDataValues(andrewsPitchfork.StartAnchor);
		val2.CopyDataValues(andrewsPitchfork.EndAnchor);
		obj.CopyDataValues(andrewsPitchfork.ExtensionAnchor);
		if (string.IsNullOrEmpty(templateName) || brush != null)
		{
			andrewsPitchfork.AnchorLineStroke.Width = width;
			andrewsPitchfork.RetracementLineStroke = new Stroke(brush, dashStyle, (float)width)
			{
				RenderTarget = andrewsPitchfork.RetracementLineStroke.RenderTarget
			};
		}
		((NinjaScript)andrewsPitchfork).SetState((State)3);
		return andrewsPitchfork;
	}

	/// <summary>
	/// Draws an Andrew's Pitchfork.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1BarsAgo">The number of bars ago (x value) of the 1st anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2BarsAgo">The number of bars ago (x axis coordinate) to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3BarsAgo">The number of bars ago (x axis coordinate) to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static AndrewsPitchfork AndrewsPitchfork(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y, int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		return AndrewsPitchforkCore(owner, tag, isAutoScale, anchor1BarsAgo, Globals.MinDate, anchor1Y, anchor2BarsAgo, Globals.MinDate, anchor2Y, anchor3BarsAgo, Globals.MinDate, anchor3Y, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an Andrew's Pitchfork.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1Time">The time at which to draw the first anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2Time">The time at which to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3Time">The time at which to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static AndrewsPitchfork AndrewsPitchfork(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time, double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		return AndrewsPitchforkCore(owner, tag, isAutoScale, int.MinValue, anchor1Time, anchor1Y, int.MinValue, anchor2Time, anchor2Y, int.MinValue, anchor3Time, anchor3Y, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an Andrew's Pitchfork.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1BarsAgo">The number of bars ago (x value) of the 1st anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2BarsAgo">The number of bars ago (x axis coordinate) to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3BarsAgo">The number of bars ago (x axis coordinate) to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static AndrewsPitchfork AndrewsPitchfork(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y, int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y, bool isGlobal, string templateName)
	{
		return AndrewsPitchforkCore(owner, tag, isAutoScale, anchor1BarsAgo, Globals.MinDate, anchor1Y, anchor2BarsAgo, Globals.MinDate, anchor2Y, anchor3BarsAgo, Globals.MinDate, anchor3Y, null, (DashStyleHelper)0, 0, isGlobal, templateName);
	}

	/// <summary>
	/// Draws an Andrew's Pitchfork.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1Time">The time at which to draw the first anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2Time">The time at which to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3Time">The time at which to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="isGlobal">if set to <c>true</c> [is global].</param>
	/// <param name="templateName">Name of the template.</param>
	/// <returns></returns>
	public static AndrewsPitchfork AndrewsPitchfork(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time, double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y, bool isGlobal, string templateName)
	{
		return AndrewsPitchforkCore(owner, tag, isAutoScale, int.MinValue, anchor1Time, anchor1Y, int.MinValue, anchor2Time, anchor2Y, int.MinValue, anchor3Time, anchor3Y, null, (DashStyleHelper)0, 0, isGlobal, templateName);
	}

	private static Arc ArcCore(NinjaScriptBase owner, bool isAutoScale, string tag, int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool isGlobal, string templateName)
	{
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (startTime == Globals.MinDate && endTime == Globals.MinDate && startBarsAgo == int.MinValue && endBarsAgo == int.MinValue)
		{
			throw new ArgumentException("bad start/end date/time");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty", "tag");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			char globalDrawingToolTagPrefix = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix;
			tag = globalDrawingToolTagPrefix + tag;
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(Arc), tag, templateName) is Arc arc))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)arc, tag, isAutoScale, owner, isGlobal);
		ChartAnchor val = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
		ChartAnchor obj = DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, endY);
		val.CopyDataValues(arc.StartAnchor);
		obj.CopyDataValues(arc.EndAnchor);
		if (brush != null)
		{
			arc.Stroke = new Stroke(brush, dashStyle, (float)width, 50)
			{
				RenderTarget = arc.Stroke.RenderTarget
			};
			arc.ArcStroke = new Stroke(brush, dashStyle, (float)width)
			{
				RenderTarget = arc.ArcStroke.RenderTarget
			};
		}
		((NinjaScript)arc).SetState((State)3);
		return arc;
	}

	/// <summary>
	/// Draws an arc.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Arc Arc(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush)
	{
		return ArcCore(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, (DashStyleHelper)0, 1, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an arc.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Arc Arc(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush)
	{
		return ArcCore(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, (DashStyleHelper)0, 1, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an arc.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static Arc Arc(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return ArcCore(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an arc.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static Arc Arc(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return ArcCore(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an arc.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Arc Arc(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<Arc>(owner, drawOnPricePanel, (Func<Arc>)(() => ArcCore(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an arc.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Arc Arc(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<Arc>(owner, drawOnPricePanel, (Func<Arc>)(() => ArcCore(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, dashStyle, width, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an arc.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Arc Arc(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, bool isGlobal, string templateName)
	{
		return ArcCore(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, null, (DashStyleHelper)0, 1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws an arc.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Arc Arc(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, bool isGlobal, string templateName)
	{
		return ArcCore(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, null, (DashStyleHelper)0, 1, isGlobal, templateName);
	}

	private static T ChartMarkerCore<T>(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, DateTime time, double yVal, Brush brush, bool isGlobal, string templateName) where T : ChartMarker
	{
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (time == Globals.MinDate && barsAgo == int.MinValue)
		{
			throw new ArgumentException("bad start/end date/time");
		}
		if (MathExtentions.ApproxCompare(yVal, double.MinValue) == 0 || MathExtentions.ApproxCompare(yVal, double.MaxValue) == 0)
		{
			throw new ArgumentException("bad Y value");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			tag = $"{GlobalDrawingToolManager.GlobalDrawingToolTagPrefix}{tag}";
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(T), tag, templateName) is T val))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)val, tag, isAutoScale, owner, isGlobal);
		DrawingTool.CreateChartAnchor(owner, barsAgo, time, yVal).CopyDataValues(val.Anchor);
		val.Anchor.IsEditing = false;
		if (brush != null)
		{
			val.AreaBrush = brush;
		}
		((NinjaScript)val).SetState((State)3);
		return val;
	}

	/// <summary>
	/// Draws an arrow pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static ArrowDown ArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush)
	{
		return ChartMarkerCore<ArrowDown>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an arrow pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static ArrowDown ArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush)
	{
		return ChartMarkerCore<ArrowDown>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an arrow pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static ArrowDown ArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<ArrowDown>(owner, drawOnPricePanel, (Func<ArrowDown>)(() => ChartMarkerCore<ArrowDown>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an arrow pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static ArrowDown ArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<ArrowDown>(owner, drawOnPricePanel, (Func<ArrowDown>)(() => ChartMarkerCore<ArrowDown>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an arrow pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static ArrowDown ArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<ArrowDown>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws an arrow pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static ArrowDown ArrowDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<ArrowDown>(owner, tag, isAutoScale, int.MinValue, time, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws an arrow pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static ArrowUp ArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush)
	{
		return ChartMarkerCore<ArrowUp>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an arrow pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static ArrowUp ArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush)
	{
		return ChartMarkerCore<ArrowUp>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an arrow pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static ArrowUp ArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<ArrowUp>(owner, drawOnPricePanel, (Func<ArrowUp>)(() => ChartMarkerCore<ArrowUp>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an arrow pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static ArrowUp ArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<ArrowUp>(owner, drawOnPricePanel, (Func<ArrowUp>)(() => ChartMarkerCore<ArrowUp>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an arrow pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static ArrowUp ArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<ArrowUp>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws an arrow pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static ArrowUp ArrowUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<ArrowUp>(owner, tag, isAutoScale, int.MinValue, time, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a diamond.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Diamond Diamond(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush)
	{
		return ChartMarkerCore<Diamond>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a diamond.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Diamond Diamond(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush)
	{
		return ChartMarkerCore<Diamond>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a diamond.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Diamond Diamond(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Diamond>(owner, drawOnPricePanel, (Func<Diamond>)(() => ChartMarkerCore<Diamond>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a diamond.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Diamond Diamond(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Diamond>(owner, drawOnPricePanel, (Func<Diamond>)(() => ChartMarkerCore<Diamond>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a diamond.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Diamond Diamond(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<Diamond>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a diamond.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Diamond Diamond(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<Diamond>(owner, tag, isAutoScale, int.MinValue, time, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a dot.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Dot Dot(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush)
	{
		return ChartMarkerCore<Dot>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a dot.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Dot Dot(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush)
	{
		return ChartMarkerCore<Dot>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a dot.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Dot Dot(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Dot>(owner, drawOnPricePanel, (Func<Dot>)(() => ChartMarkerCore<Dot>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a dot.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Dot Dot(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Dot>(owner, drawOnPricePanel, (Func<Dot>)(() => ChartMarkerCore<Dot>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a dot.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Dot Dot(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<Dot>(owner, tag, isAutoScale, int.MinValue, time, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a dot.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Dot Dot(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<Dot>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a square.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Square Square(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush)
	{
		return ChartMarkerCore<Square>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a square.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Square Square(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush)
	{
		return ChartMarkerCore<Square>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a square.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Square Square(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Square>(owner, drawOnPricePanel, (Func<Square>)(() => ChartMarkerCore<Square>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a square.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Square Square(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Square>(owner, drawOnPricePanel, (Func<Square>)(() => ChartMarkerCore<Square>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a square.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Square Square(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<Square>(owner, tag, isAutoScale, int.MinValue, time, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a square.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Square Square(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<Square>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a triangle pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static TriangleDown TriangleDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush)
	{
		return ChartMarkerCore<TriangleDown>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a triangle pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static TriangleDown TriangleDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush)
	{
		return ChartMarkerCore<TriangleDown>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a triangle pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static TriangleDown TriangleDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<TriangleDown>(owner, drawOnPricePanel, (Func<TriangleDown>)(() => ChartMarkerCore<TriangleDown>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a triangle pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static TriangleDown TriangleDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<TriangleDown>(owner, drawOnPricePanel, (Func<TriangleDown>)(() => ChartMarkerCore<TriangleDown>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a triangle pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static TriangleDown TriangleDown(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<TriangleDown>(owner, tag, isAutoScale, int.MinValue, time, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a triangle pointing down.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static TriangleDown TriangleDown(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<TriangleDown>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a triangle pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static TriangleUp TriangleUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush)
	{
		return ChartMarkerCore<TriangleUp>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a triangle pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static TriangleUp TriangleUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush)
	{
		return ChartMarkerCore<TriangleUp>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a triangle pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static TriangleUp TriangleUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<TriangleUp>(owner, drawOnPricePanel, (Func<TriangleUp>)(() => ChartMarkerCore<TriangleUp>(owner, tag, isAutoScale, int.MinValue, time, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a triangle pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static TriangleUp TriangleUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<TriangleUp>(owner, drawOnPricePanel, (Func<TriangleUp>)(() => ChartMarkerCore<TriangleUp>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, brush, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a triangle pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static TriangleUp TriangleUp(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<TriangleUp>(owner, tag, isAutoScale, int.MinValue, time, y, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a triangle pointing up.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static TriangleUp TriangleUp(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
	{
		return ChartMarkerCore<TriangleUp>(owner, tag, isAutoScale, barsAgo, Globals.MinDate, y, null, isGlobal, templateName);
	}

	private static T FibonacciCore<T>(NinjaScriptBase owner, bool isAutoScale, string tag, int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY, bool isGlobal, string templateName) where T : FibonacciLevels
	{
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (startTime == Globals.MinDate && endTime == Globals.MinDate && startBarsAgo == int.MinValue && endBarsAgo == int.MinValue)
		{
			throw new ArgumentException("bad start/end date/time");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			tag = $"{GlobalDrawingToolManager.GlobalDrawingToolTagPrefix}{tag}";
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(T), tag, templateName) is T val))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)val, tag, isAutoScale, owner, isGlobal);
		ChartAnchor val2 = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
		ChartAnchor obj = DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, endY);
		val2.CopyDataValues(val.StartAnchor);
		obj.CopyDataValues(val.EndAnchor);
		((NinjaScript)val).SetState((State)3);
		return val;
	}

	private static FibonacciExtensions FibonacciExtensionsCore(NinjaScriptBase owner, bool isAutoScale, string tag, int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY, int extensionBarsAgo, DateTime extensionTime, double extensionY, bool isGlobal, string templateName)
	{
		FibonacciExtensions fibonacciExtensions = FibonacciCore<FibonacciExtensions>(owner, isAutoScale, tag, startBarsAgo, startTime, startY, endBarsAgo, endTime, endY, isGlobal, templateName);
		DrawingTool.CreateChartAnchor(owner, extensionBarsAgo, extensionTime, extensionY).CopyDataValues(fibonacciExtensions.ExtensionAnchor);
		return fibonacciExtensions;
	}

	/// <summary>
	/// Draws a fibonacci circle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <returns></returns>
	public static FibonacciCircle FibonacciCircle(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY)
	{
		return FibonacciCore<FibonacciCircle>(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a fibonacci circle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <returns></returns>
	public static FibonacciCircle FibonacciCircle(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY)
	{
		return FibonacciCore<FibonacciCircle>(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a fibonacci circle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static FibonacciCircle FibonacciCircle(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, bool isGlobal, string templateName)
	{
		return FibonacciCore<FibonacciCircle>(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a fibonacci circle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static FibonacciCircle FibonacciCircle(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, bool isGlobal, string templateName)
	{
		return FibonacciCore<FibonacciCircle>(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a fibonacci extension.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="extensionBarsAgo">The extension bars ago.</param>
	/// <param name="extensionY">The y value of the 3rd anchor point</param>
	/// <returns></returns>
	public static FibonacciExtensions FibonacciExtensions(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, int extensionBarsAgo, double extensionY)
	{
		return FibonacciExtensionsCore(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, extensionBarsAgo, Globals.MinDate, extensionY, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a fibonacci extension.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="extensionTime">The time of the 3rd anchor point</param>
	/// <param name="extensionY">The y value of the 3rd anchor point</param>
	/// <returns></returns>
	public static FibonacciExtensions FibonacciExtensions(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, DateTime extensionTime, double extensionY)
	{
		return FibonacciExtensionsCore(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, int.MinValue, extensionTime, extensionY, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a fibonacci extension.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="extensionTime">The time of the 3rd anchor point</param>
	/// <param name="extensionY">The y value of the 3rd anchor point</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static FibonacciExtensions FibonacciExtensions(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, DateTime extensionTime, double extensionY, bool isGlobal, string templateName)
	{
		return FibonacciExtensionsCore(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, int.MinValue, extensionTime, extensionY, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a fibonacci extension.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="extensionBarsAgo">The extension bars ago.</param>
	/// <param name="extensionY">The y value of the 3rd anchor point</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static FibonacciExtensions FibonacciExtensions(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, int extensionBarsAgo, double extensionY, bool isGlobal, string templateName)
	{
		return FibonacciExtensionsCore(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, extensionBarsAgo, Globals.MinDate, extensionY, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a fibonacci retracement.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <returns></returns>
	public static FibonacciRetracements FibonacciRetracements(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY)
	{
		return FibonacciCore<FibonacciRetracements>(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a fibonacci retracement.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <returns></returns>
	public static FibonacciRetracements FibonacciRetracements(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY)
	{
		return FibonacciCore<FibonacciRetracements>(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a fibonacci retracement.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static FibonacciRetracements FibonacciRetracements(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, bool isGlobal, string templateName)
	{
		return FibonacciCore<FibonacciRetracements>(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a fibonacci retracement.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static FibonacciRetracements FibonacciRetracements(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, bool isGlobal, string templateName)
	{
		return FibonacciCore<FibonacciRetracements>(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a fibonacci time extension.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <returns></returns>
	public static FibonacciTimeExtensions FibonacciTimeExtensions(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY)
	{
		return FibonacciCore<FibonacciTimeExtensions>(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a fibonacci time extension.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <returns></returns>
	public static FibonacciTimeExtensions FibonacciTimeExtensions(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY)
	{
		return FibonacciCore<FibonacciTimeExtensions>(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a fibonacci time extension.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static FibonacciTimeExtensions FibonacciTimeExtensions(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, bool isGlobal, string templateName)
	{
		return FibonacciCore<FibonacciTimeExtensions>(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a fibonacci time extension.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static FibonacciTimeExtensions FibonacciTimeExtensions(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, bool isGlobal, string templateName)
	{
		return FibonacciCore<FibonacciTimeExtensions>(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, isGlobal, templateName);
	}

	private static GannFan GannFanCore(NinjaScriptBase owner, bool isAutoScale, string tag, int barsAgo, DateTime time, double y, bool isGlobal, string templateName)
	{
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (time == Globals.MinDate && barsAgo == int.MinValue)
		{
			throw new ArgumentException("bad start/end date/time");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty");
		}
		if (isGlobal && tag[0] != '@')
		{
			tag = "@" + tag;
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(GannFan), tag, templateName) is GannFan gannFan))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)gannFan, tag, isAutoScale, owner, isGlobal);
		DrawingTool.CreateChartAnchor(owner, barsAgo, time, y).CopyDataValues(gannFan.Anchor);
		((NinjaScript)gannFan).SetState((State)3);
		return gannFan;
	}

	/// <summary>
	/// Draws a Gann Fan.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <returns></returns>
	public static GannFan GannFan(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y)
	{
		return GannFanCore(owner, isAutoScale, tag, barsAgo, Globals.MinDate, y, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a Gann Fan.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <returns></returns>
	public static GannFan GannFan(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y)
	{
		return GannFanCore(owner, isAutoScale, tag, int.MinValue, time, y, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a Gann Fan.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static GannFan GannFan(NinjaScriptBase owner, string tag, bool isAutoScale, int barsAgo, double y, bool isGlobal, string templateName)
	{
		return GannFanCore(owner, isAutoScale, tag, barsAgo, Globals.MinDate, y, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a Gann Fan.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static GannFan GannFan(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime time, double y, bool isGlobal, string templateName)
	{
		return GannFanCore(owner, isAutoScale, tag, int.MinValue, time, y, isGlobal, templateName);
	}

	private static T DrawLineTypeCore<T>(NinjaScriptBase owner, bool isAutoScale, string tag, int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool isGlobal, string templateName) where T : Line
	{
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Expected O, but got Unknown
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty", "tag");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			tag = $"{GlobalDrawingToolManager.GlobalDrawingToolTagPrefix}{tag}";
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(T), tag, templateName) is T val))
		{
			return null;
		}
		if (val is VerticalLine)
		{
			if (startTime == Globals.MinDate && startBarsAgo == int.MinValue)
			{
				throw new ArgumentException("missing vertical line time / bars ago");
			}
		}
		else if (val is HorizontalLine)
		{
			if (MathExtentions.ApproxCompare(startY, double.MinValue) == 0)
			{
				throw new ArgumentException("missing horizontal line Y");
			}
		}
		else
		{
			if (startTime == Globals.MinDate && endTime == Globals.MinDate && startBarsAgo == int.MinValue && endBarsAgo == int.MinValue)
			{
				throw new ArgumentException("bad start/end date/time");
			}
			if (startTime < Globals.MinDate)
			{
				string[] obj = new string[5]
				{
					((object)val)?.ToString(),
					" startTime must be greater than the minimum Date of ",
					null,
					null,
					null
				};
				DateTime minDate = Globals.MinDate;
				obj[2] = minDate.ToString();
				obj[3] = " but was ";
				obj[4] = startTime.ToString();
				throw new ArgumentException(string.Concat(obj));
			}
			if (endTime < Globals.MinDate)
			{
				string[] obj2 = new string[5]
				{
					((object)val)?.ToString(),
					" endTime must be greater than the minimum Date of ",
					null,
					null,
					null
				};
				DateTime minDate = Globals.MinDate;
				obj2[2] = minDate.ToString();
				obj2[3] = " but was ";
				obj2[4] = endTime.ToString();
				throw new ArgumentException(string.Concat(obj2));
			}
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)val, tag, isAutoScale, owner, isGlobal);
		if ((val is HorizontalLine || val is VerticalLine) ? true : false)
		{
			ChartAnchor val2 = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
			val2.CopyDataValues(val.StartAnchor);
		}
		else
		{
			ChartAnchor val2 = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
			ChartAnchor obj3 = DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, endY);
			val2.CopyDataValues(val.StartAnchor);
			obj3.CopyDataValues(val.EndAnchor);
		}
		if (brush != null)
		{
			val.Stroke = new Stroke(brush, dashStyle, (float)width)
			{
				RenderTarget = val.Stroke.RenderTarget
			};
		}
		((NinjaScript)val).SetState((State)3);
		return val;
	}

	private static ArrowLine ArrowLineCore(NinjaScriptBase owner, bool isAutoScale, string tag, int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool isGlobal, string templateName)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return DrawLineTypeCore<ArrowLine>(owner, isAutoScale, tag, startBarsAgo, startTime, startY, endBarsAgo, endTime, endY, brush, dashStyle, width, isGlobal, templateName);
	}

	/// <summary>
	/// Draws an arrow line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static ArrowLine ArrowLine(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush)
	{
		return ArrowLineCore(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, (DashStyleHelper)0, 1, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an arrow line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static ArrowLine ArrowLine(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush)
	{
		return ArrowLineCore(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, (DashStyleHelper)0, 1, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an arrow line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static ArrowLine ArrowLine(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		return ArrowLineCore(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an arrow line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static ArrowLine ArrowLine(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<ArrowLine>(owner, drawOnPricePanel, (Func<ArrowLine>)(() => ArrowLineCore(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an arrow line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static ArrowLine ArrowLine(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<ArrowLine>(owner, drawOnPricePanel, (Func<ArrowLine>)(() => ArrowLineCore(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, dashStyle, width, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an arrow line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static ArrowLine ArrowLine(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, bool isGlobal, string templateName)
	{
		return ArrowLineCore(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, null, (DashStyleHelper)0, 1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws an arrow line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static ArrowLine ArrowLine(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, bool isGlobal, string templateName)
	{
		return ArrowLineCore(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, null, (DashStyleHelper)0, 1, isGlobal, templateName);
	}

	private static ExtendedLine ExtendedLineCore(NinjaScriptBase owner, bool isAutoScale, string tag, int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool isGlobal, string templateName)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return DrawLineTypeCore<ExtendedLine>(owner, isAutoScale, tag, startBarsAgo, startTime, startY, endBarsAgo, endTime, endY, brush, dashStyle, width, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush)
	{
		return ExtendedLineCore(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, (DashStyleHelper)0, 1, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush)
	{
		return ExtendedLineCore(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, (DashStyleHelper)0, 1, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		return ExtendedLineCore(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		return ExtendedLineCore(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<ExtendedLine>(owner, drawOnPricePanel, (Func<ExtendedLine>)(() => ExtendedLineCore(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<ExtendedLine>(owner, drawOnPricePanel, (Func<ExtendedLine>)(() => ExtendedLineCore(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, dashStyle, width, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, bool isGlobal, string templateName)
	{
		return ExtendedLineCore(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, null, (DashStyleHelper)0, 1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, bool isGlobal, string templateName)
	{
		return ExtendedLineCore(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, null, (DashStyleHelper)0, 1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return ExtendedLineCore(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return ExtendedLineCore(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<ExtendedLine>(owner, drawOnPricePanel, (Func<ExtendedLine>)(() => ExtendedLineCore(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a line with infinite end points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static ExtendedLine ExtendedLine(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<ExtendedLine>(owner, drawOnPricePanel, (Func<ExtendedLine>)(() => ExtendedLineCore(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, dashStyle, width, isGlobal: false, null)));
	}

	private static HorizontalLine HorizontalLineCore(NinjaScriptBase owner, bool isAutoScale, string tag, double y, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		return DrawLineTypeCore<HorizontalLine>(owner, isAutoScale, tag, 0, Globals.MinDate, y, 0, Globals.MinDate, y, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a horizontal line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static HorizontalLine HorizontalLine(NinjaScriptBase owner, string tag, double y, Brush brush)
	{
		return HorizontalLineCore(owner, isAutoScale: false, tag, y, brush, (DashStyleHelper)0, 1);
	}

	/// <summary>
	/// Draws a horizontal line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static HorizontalLine HorizontalLine(NinjaScriptBase owner, string tag, double y, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return HorizontalLineCore(owner, isAutoScale: false, tag, y, brush, dashStyle, width);
	}

	/// <summary>
	/// Draws a horizontal line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static HorizontalLine HorizontalLine(NinjaScriptBase owner, string tag, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<HorizontalLine>(owner, drawOnPricePanel, (Func<HorizontalLine>)(() => HorizontalLineCore(owner, isAutoScale: false, tag, y, brush, (DashStyleHelper)0, 1)));
	}

	/// <summary>
	/// Draws a horizontal line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static HorizontalLine HorizontalLine(NinjaScriptBase owner, string tag, double y, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<HorizontalLine>(owner, drawOnPricePanel, (Func<HorizontalLine>)(() => HorizontalLineCore(owner, isAutoScale: false, tag, y, brush, dashStyle, width)));
	}

	/// <summary>
	/// Draws a horizontal line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static HorizontalLine HorizontalLine(NinjaScriptBase owner, string tag, double y, bool isGlobal, string templateName)
	{
		return DrawLineTypeCore<HorizontalLine>(owner, isAutoScale: false, tag, int.MinValue, Globals.MinDate, y, int.MinValue, Globals.MinDate, y, null, (DashStyleHelper)0, 1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a horizontal line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static HorizontalLine HorizontalLine(NinjaScriptBase owner, string tag, bool isAutoScale, double y, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return HorizontalLineCore(owner, isAutoScale, tag, y, brush, dashStyle, width);
	}

	/// <summary>
	/// Draws a horizontal line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoscale">if set to <c>true</c> [is autoscale].</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static HorizontalLine HorizontalLine(NinjaScriptBase owner, string tag, bool isAutoscale, double y, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<HorizontalLine>(owner, drawOnPricePanel, (Func<HorizontalLine>)(() => HorizontalLineCore(owner, isAutoscale, tag, y, brush, (DashStyleHelper)0, 1)));
	}

	private static Line Line(NinjaScriptBase owner, bool isAutoScale, string tag, int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return DrawLineTypeCore<Line>(owner, isAutoScale, tag, startBarsAgo, startTime, startY, endBarsAgo, endTime, endY, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a line between two points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Line Line(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush)
	{
		return Line(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, (DashStyleHelper)0, 1);
	}

	/// <summary>
	/// Draws a line between two points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static Line Line(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return Line(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width);
	}

	/// <summary>
	/// Draws a line between two points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static Line Line(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return Line(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, dashStyle, width);
	}

	/// <summary>
	/// Draws a line between two points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Line Line(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<Line>(owner, drawOnPricePanel, (Func<Line>)(() => Line(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width)));
	}

	/// <summary>
	/// Draws a line between two points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Line Line(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<Line>(owner, drawOnPricePanel, (Func<Line>)(() => Line(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, dashStyle, width)));
	}

	/// <summary>
	/// Draws a line between two points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Line Line(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, string templateName)
	{
		return DrawLineTypeCore<Line>(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, null, (DashStyleHelper)1, 0, isGlobal: false, templateName);
	}

	/// <summary>
	/// Draws a line between two points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Line Line(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, string templateName)
	{
		return DrawLineTypeCore<Line>(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, null, (DashStyleHelper)1, 0, isGlobal: false, templateName);
	}

	/// <summary>
	/// Draws a line between two points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Line Line(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, bool isGlobal, string templateName)
	{
		return DrawLineTypeCore<Line>(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, null, (DashStyleHelper)0, 0, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a line between two points.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Line Line(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, bool isGlobal, string templateName)
	{
		return DrawLineTypeCore<Line>(owner, isAutoScale, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, null, (DashStyleHelper)0, 0, isGlobal, templateName);
	}

	private static VerticalLine VerticalLineCore(NinjaScriptBase owner, bool isAutoScale, string tag, int barsAgo, DateTime time, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		return DrawLineTypeCore<VerticalLine>(owner, isAutoScale, tag, barsAgo, time, double.MinValue, int.MinValue, Globals.MinDate, double.MinValue, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a vertical line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static VerticalLine VerticalLine(NinjaScriptBase owner, string tag, DateTime time, Brush brush)
	{
		return VerticalLineCore(owner, isAutoScale: false, tag, int.MinValue, time, brush, (DashStyleHelper)0, 1);
	}

	/// <summary>
	/// Draws a vertical line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static VerticalLine VerticalLine(NinjaScriptBase owner, string tag, DateTime time, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		return VerticalLineCore(owner, isAutoScale: false, tag, int.MinValue, time, brush, dashStyle, width);
	}

	/// <summary>
	/// Draws a vertical line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static VerticalLine VerticalLine(NinjaScriptBase owner, string tag, int barsAgo, Brush brush)
	{
		return VerticalLineCore(owner, isAutoScale: false, tag, barsAgo, Globals.MinDate, brush, (DashStyleHelper)0, 1);
	}

	/// <summary>
	/// Draws a vertical line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static VerticalLine VerticalLine(NinjaScriptBase owner, string tag, int barsAgo, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		return VerticalLineCore(owner, isAutoScale: false, tag, barsAgo, Globals.MinDate, brush, dashStyle, width);
	}

	/// <summary>
	/// Draws a vertical line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static VerticalLine VerticalLine(NinjaScriptBase owner, string tag, DateTime time, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<VerticalLine>(owner, drawOnPricePanel, (Func<VerticalLine>)(() => VerticalLineCore(owner, isAutoScale: false, tag, int.MinValue, time, brush, dashStyle, width)));
	}

	/// <summary>
	/// Draws a vertical line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static VerticalLine VerticalLine(NinjaScriptBase owner, string tag, int barsAgo, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<VerticalLine>(owner, drawOnPricePanel, (Func<VerticalLine>)(() => VerticalLineCore(owner, isAutoScale: false, tag, barsAgo, Globals.MinDate, brush, dashStyle, width)));
	}

	/// <summary>
	/// Draws a vertical line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static VerticalLine VerticalLine(NinjaScriptBase owner, string tag, int barsAgo, bool isGlobal, string templateName)
	{
		return DrawLineTypeCore<VerticalLine>(owner, isAutoScale: false, tag, barsAgo, Globals.MinDate, double.MinValue, int.MinValue, Globals.MinDate, double.MinValue, null, (DashStyleHelper)0, 1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a vertical line.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static VerticalLine VerticalLine(NinjaScriptBase owner, string tag, DateTime time, bool isGlobal, string templateName)
	{
		return DrawLineTypeCore<VerticalLine>(owner, isAutoScale: false, tag, int.MinValue, time, double.MinValue, int.MinValue, Globals.MinDate, double.MinValue, null, (DashStyleHelper)0, 1, isGlobal, templateName);
	}

	private static Ray RayCore(NinjaScriptBase owner, bool isAutoScale, string tag, int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return DrawLineTypeCore<Ray>(owner, isAutoScale, tag, startBarsAgo, startTime, startY, endBarsAgo, endTime, endY, brush, dashStyle, width, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a line which has an infinite end point in one direction.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Ray Ray(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush)
	{
		return RayCore(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, (DashStyleHelper)0, 1);
	}

	/// <summary>
	/// Draws a line which has an infinite end point in one direction.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static Ray Ray(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return RayCore(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width);
	}

	/// <summary>
	/// Draws a line which has an infinite end point in one direction.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Ray Ray(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush)
	{
		return RayCore(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, (DashStyleHelper)0, 1);
	}

	/// <summary>
	/// Draws a line which has an infinite end point in one direction.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <returns></returns>
	public static Ray Ray(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		return RayCore(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, dashStyle, width);
	}

	/// <summary>
	/// Draws a line which has an infinite end point in one direction.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Ray Ray(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<Ray>(owner, drawOnPricePanel, (Func<Ray>)(() => RayCore(owner, isAutoScale, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, brush, dashStyle, width)));
	}

	/// <summary>
	/// Draws a line which has an infinite end point in one direction.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object.</param>
	/// <param name="width">The width of the draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Ray Ray(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, DashStyleHelper dashStyle, int width, bool drawOnPricePanel)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		return DrawingTool.DrawToggledPricePanel<Ray>(owner, drawOnPricePanel, (Func<Ray>)(() => RayCore(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, brush, dashStyle, width)));
	}

	/// <summary>
	/// Draws a line which has an infinite end point in one direction.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Ray Ray(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, bool isGlobal, string templateName)
	{
		return DrawLineTypeCore<Ray>(owner, isAutoScale: false, tag, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, null, (DashStyleHelper)0, 1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a line which has an infinite end point in one direction.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Ray Ray(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, bool isGlobal, string templateName)
	{
		return DrawLineTypeCore<Ray>(owner, isAutoScale: false, tag, int.MinValue, startTime, startY, int.MinValue, endTime, endY, null, (DashStyleHelper)0, 1, isGlobal, templateName);
	}

	private static PathTool PathCore(NinjaScriptBase owner, string tag, bool isAutoScale, List<ChartAnchor> chartAnchors, Brush brush, DashStyleHelper dashStyle, bool isGlobal, string templateName)
	{
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected O, but got Unknown
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty", "tag");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			char globalDrawingToolTagPrefix = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix;
			tag = globalDrawingToolTagPrefix + tag;
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(PathTool), tag, templateName) is PathTool pathTool))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)pathTool, tag, isAutoScale, owner, isGlobal);
		if (chartAnchors != null)
		{
			pathTool.ChartAnchors = chartAnchors;
			for (int i = 1; i < chartAnchors.Count; i++)
			{
				pathTool.PathToolSegments.Add(new PathToolSegment(chartAnchors[i - 1], chartAnchors[i], $"{Resource.NinjaScriptDrawingToolPathSegment} {i}"));
			}
		}
		if (brush != null)
		{
			pathTool.OutlineStroke = new Stroke(brush, dashStyle, 2f);
		}
		((NinjaScript)pathTool).SetState((State)3);
		return pathTool;
	}

	private static PathTool PathBasic(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, DateTime anchor1Time, double anchor1Y, int anchor2BarsAgo, DateTime anchor2Time, double anchor2Y, int anchor3BarsAgo, DateTime anchor3Time, double anchor3Y, int anchor4BarsAgo, DateTime anchor4Time, double anchor4Y, int anchor5BarsAgo, DateTime anchor5Time, double anchor5Y)
	{
		List<ChartAnchor> list = new List<ChartAnchor>
		{
			DrawingTool.CreateChartAnchor(owner, anchor1BarsAgo, anchor1Time, anchor1Y),
			DrawingTool.CreateChartAnchor(owner, anchor2BarsAgo, anchor2Time, anchor2Y),
			DrawingTool.CreateChartAnchor(owner, anchor3BarsAgo, anchor3Time, anchor3Y)
		};
		if (anchor4BarsAgo != int.MinValue || anchor4Time != DateTime.MinValue)
		{
			list.Add(DrawingTool.CreateChartAnchor(owner, anchor4BarsAgo, anchor4Time, anchor4Y));
		}
		if (anchor5BarsAgo != int.MinValue || anchor5Time != DateTime.MinValue)
		{
			list.Add(DrawingTool.CreateChartAnchor(owner, anchor5BarsAgo, anchor5Time, anchor5Y));
		}
		return PathCore(owner, tag, isAutoScale, list, null, (DashStyleHelper)0, isGlobal: false, string.Empty);
	}

	/// <summary>
	/// Draws a Path.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1BarsAgo">The number of bars ago (x axis coordinate) to draw the first anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2BarsAgo">The number of bars ago (x axis coordinate) to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3BarsAgo">The number of bars ago (x axis coordinate) to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <returns></returns>
	public static PathTool PathTool(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y, int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y)
	{
		return PathBasic(owner, tag, isAutoScale, anchor1BarsAgo, DateTime.MinValue, anchor1Y, anchor2BarsAgo, DateTime.MinValue, anchor2Y, anchor3BarsAgo, DateTime.MinValue, anchor3Y, int.MinValue, DateTime.MinValue, double.MinValue, int.MinValue, DateTime.MinValue, double.MinValue);
	}

	/// <summary>
	/// Draws a Path.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1Time">The time at which to draw the first anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2Time">The time at which to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3Time">The time at which to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <returns></returns>
	public static PathTool PathTool(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time, double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y)
	{
		return PathBasic(owner, tag, isAutoScale, int.MinValue, anchor1Time, anchor1Y, int.MinValue, anchor2Time, anchor2Y, int.MinValue, anchor3Time, anchor3Y, int.MinValue, DateTime.MinValue, double.MinValue, int.MinValue, DateTime.MinValue, double.MinValue);
	}

	/// <summary>
	/// Draws a Path.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1BarsAgo">The number of bars ago (x axis coordinate) to draw the first anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2BarsAgo">The number of bars ago (x axis coordinate) to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3BarsAgo">The number of bars ago (x axis coordinate) to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="anchor4BarsAgo">The number of bars ago (x axis coordinate) to draw the fourth anchor point</param>
	/// <param name="anchor4Y">The y value coordinate of the fourth anchor point</param>
	/// <returns></returns>
	public static PathTool PathTool(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y, int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y, int anchor4BarsAgo, double anchor4Y)
	{
		return PathBasic(owner, tag, isAutoScale, anchor1BarsAgo, DateTime.MinValue, anchor1Y, anchor2BarsAgo, DateTime.MinValue, anchor2Y, anchor3BarsAgo, DateTime.MinValue, anchor3Y, anchor4BarsAgo, DateTime.MinValue, anchor4Y, int.MinValue, DateTime.MinValue, double.MinValue);
	}

	/// <summary>
	/// Draws a Path.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1Time">The time at which to draw the first anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2Time">The time at which to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3Time">The time at which to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="anchor4Time">The time at which to draw the fourth anchor point</param>
	/// <param name="anchor4Y">The y value coordinate of the fourth anchor point</param>
	/// <returns></returns>
	public static PathTool PathTool(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time, double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y, DateTime anchor4Time, double anchor4Y)
	{
		return PathBasic(owner, tag, isAutoScale, int.MinValue, anchor1Time, anchor1Y, int.MinValue, anchor2Time, anchor2Y, int.MinValue, anchor3Time, anchor3Y, int.MinValue, anchor4Time, anchor4Y, int.MinValue, DateTime.MinValue, double.MinValue);
	}

	/// <summary>
	/// Draws a Path.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1BarsAgo">The number of bars ago (x axis coordinate) to draw the first anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2BarsAgo">The number of bars ago (x axis coordinate) to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3BarsAgo">The number of bars ago (x axis coordinate) to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="anchor4BarsAgo">The number of bars ago (x axis coordinate) to draw the fourth anchor point</param>
	/// <param name="anchor4Y">The y value coordinate of the fourth anchor point</param>
	/// <param name="anchor5BarsAgo">The number of bars ago (x axis coordinate) to draw the fifth anchor point</param>
	/// <param name="anchor5Y">The y value coordinate of the fifth anchor point</param>
	/// <returns></returns>
	public static PathTool PathTool(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y, int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y, int anchor4BarsAgo, double anchor4Y, int anchor5BarsAgo, double anchor5Y)
	{
		return PathBasic(owner, tag, isAutoScale, anchor1BarsAgo, DateTime.MinValue, anchor1Y, anchor2BarsAgo, DateTime.MinValue, anchor2Y, anchor3BarsAgo, DateTime.MinValue, anchor3Y, anchor4BarsAgo, DateTime.MinValue, anchor4Y, anchor5BarsAgo, DateTime.MinValue, anchor5Y);
	}

	/// <summary>
	/// Draws a Path.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1Time">The time at which to draw the first anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2Time">The time at which to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3Time">The time at which to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="anchor4Time">The time at which to draw the fourth anchor point</param>
	/// <param name="anchor4Y">The y value coordinate of the fourth anchor point</param>
	/// <param name="anchor5Time">The time at which to draw the fifth anchor point</param>
	/// <param name="anchor5Y">The y value coordinate of the fifth anchor point</param>
	/// <returns></returns>
	public static PathTool PathTool(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time, double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y, DateTime anchor4Time, double anchor4Y, DateTime anchor5Time, double anchor5Y)
	{
		return PathBasic(owner, tag, isAutoScale, int.MinValue, anchor1Time, anchor1Y, int.MinValue, anchor2Time, anchor2Y, int.MinValue, anchor3Time, anchor3Y, int.MinValue, anchor4Time, anchor4Y, int.MinValue, anchor5Time, anchor5Y);
	}

	/// <summary>
	/// Draws a ath.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="chartAnchors">A List of ChartAnchor objects defining the path</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object</param>
	/// <returns></returns>
	public static PathTool PathTool(NinjaScriptBase owner, string tag, bool isAutoScale, List<ChartAnchor> chartAnchors, Brush brush, DashStyleHelper dashStyle)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return PathCore(owner, tag, isAutoScale, chartAnchors, brush, dashStyle, isGlobal: false, string.Empty);
	}

	/// <summary>
	/// Draws a Path.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="chartAnchors">A List of ChartAnchor objects defining the path</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static PathTool PathTool(NinjaScriptBase owner, string tag, bool isAutoScale, List<ChartAnchor> chartAnchors, bool isGlobal, string templateName)
	{
		return PathCore(owner, tag, isAutoScale, chartAnchors, null, (DashStyleHelper)0, isGlobal, templateName);
	}

	private static Polygon PolygonCore(NinjaScriptBase owner, string tag, bool isAutoScale, List<ChartAnchor> chartAnchors, Brush brush, DashStyleHelper dashStyle, Brush areaBrush, int areaOpacity, bool isGlobal, string templateName)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected O, but got Unknown
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty", "tag");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			char globalDrawingToolTagPrefix = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix;
			tag = globalDrawingToolTagPrefix + tag;
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(Polygon), tag, templateName) is Polygon polygon))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)polygon, tag, isAutoScale, owner, isGlobal);
		if (chartAnchors != null)
		{
			polygon.ChartAnchors = chartAnchors;
		}
		if (brush != null)
		{
			polygon.OutlineStroke = new Stroke(brush, dashStyle, 2f);
		}
		if (areaBrush != null)
		{
			polygon.AreaBrush = areaBrush;
		}
		if (areaOpacity > -1)
		{
			polygon.AreaOpacity = areaOpacity;
		}
		((NinjaScript)polygon).SetState((State)3);
		return polygon;
	}

	private static Polygon PolygonBasic(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, DateTime anchor1Time, double anchor1Y, int anchor2BarsAgo, DateTime anchor2Time, double anchor2Y, int anchor3BarsAgo, DateTime anchor3Time, double anchor3Y, int anchor4BarsAgo, DateTime anchor4Time, double anchor4Y, int anchor5BarsAgo, DateTime anchor5Time, double anchor5Y, int anchor6BarsAgo, DateTime anchor6Time, double anchor6Y)
	{
		List<ChartAnchor> list = new List<ChartAnchor>
		{
			DrawingTool.CreateChartAnchor(owner, anchor1BarsAgo, anchor1Time, anchor1Y),
			DrawingTool.CreateChartAnchor(owner, anchor2BarsAgo, anchor2Time, anchor2Y),
			DrawingTool.CreateChartAnchor(owner, anchor3BarsAgo, anchor3Time, anchor3Y),
			DrawingTool.CreateChartAnchor(owner, anchor4BarsAgo, anchor4Time, anchor4Y)
		};
		if (anchor5BarsAgo != int.MinValue || anchor5Time != DateTime.MinValue)
		{
			list.Add(DrawingTool.CreateChartAnchor(owner, anchor5BarsAgo, anchor5Time, anchor5Y));
		}
		if (anchor6BarsAgo != int.MinValue || anchor6Time != DateTime.MinValue)
		{
			list.Add(DrawingTool.CreateChartAnchor(owner, anchor6BarsAgo, anchor6Time, anchor6Y));
		}
		return PolygonCore(owner, tag, isAutoScale, list, null, (DashStyleHelper)0, null, int.MinValue, isGlobal: false, string.Empty);
	}

	/// <summary>
	/// Draws a Polygon.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1BarsAgo">The number of bars ago (x value) of the 1st anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2BarsAgo">The number of bars ago (x axis coordinate) to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3BarsAgo">The number of bars ago (x axis coordinate) to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="anchor4BarsAgo">The number of bars ago (x axis coordinate) to draw the fourth anchor point</param>
	/// <param name="anchor4Y">The y value coordinate of the fourth anchor point</param>
	/// <returns></returns>
	public static Polygon Polygon(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y, int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y, int anchor4BarsAgo, double anchor4Y)
	{
		return PolygonBasic(owner, tag, isAutoScale, anchor1BarsAgo, DateTime.MinValue, anchor1Y, anchor2BarsAgo, DateTime.MinValue, anchor2Y, anchor3BarsAgo, DateTime.MinValue, anchor3Y, anchor4BarsAgo, DateTime.MinValue, anchor4Y, int.MinValue, DateTime.MinValue, double.MinValue, int.MinValue, DateTime.MinValue, double.MinValue);
	}

	/// <summary>
	/// Draws a Polygon.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1Time">The time at which to draw the first anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2Time">The time at which to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3Time">The time at which to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="anchor4Time">The time at which to draw the fourth anchor point</param>
	/// <param name="anchor4Y">The y value coordinate of the fourth anchor point</param>
	/// <returns></returns>
	public static Polygon Polygon(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time, double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y, DateTime anchor4Time, double anchor4Y)
	{
		return PolygonBasic(owner, tag, isAutoScale, int.MinValue, anchor1Time, anchor1Y, int.MinValue, anchor2Time, anchor2Y, int.MinValue, anchor3Time, anchor3Y, int.MinValue, anchor4Time, anchor4Y, int.MinValue, DateTime.MinValue, double.MinValue, int.MinValue, DateTime.MinValue, double.MinValue);
	}

	/// <summary>
	/// Draws a Polygon.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1BarsAgo">The number of bars ago (x value) of the 1st anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2BarsAgo">The number of bars ago (x axis coordinate) to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3BarsAgo">The number of bars ago (x axis coordinate) to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="anchor4BarsAgo">The number of bars ago (x axis coordinate) to draw the fourth anchor point</param>
	/// <param name="anchor4Y">The y value coordinate of the fourth anchor point</param>
	/// <param name="anchor5BarsAgo">The number of bars ago (x axis coordinate) to draw the fifth anchor point</param>
	/// <param name="anchor5Y">The y value coordinate of the fifth anchor point</param>
	/// <returns></returns>
	public static Polygon Polygon(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y, int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y, int anchor4BarsAgo, double anchor4Y, int anchor5BarsAgo, double anchor5Y)
	{
		return PolygonBasic(owner, tag, isAutoScale, anchor1BarsAgo, DateTime.MinValue, anchor1Y, anchor2BarsAgo, DateTime.MinValue, anchor2Y, anchor3BarsAgo, DateTime.MinValue, anchor3Y, anchor4BarsAgo, DateTime.MinValue, anchor4Y, anchor5BarsAgo, DateTime.MinValue, anchor5Y, int.MinValue, DateTime.MinValue, -2147483648.0);
	}

	/// <summary>
	/// Draws a Polygon.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1Time">The time at which to draw the first anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2Time">The time at which to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3Time">The time at which to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="anchor4Time">The time at which to draw the fourth anchor point</param>
	/// <param name="anchor4Y">The y value coordinate of the fourth anchor point</param>
	/// <param name="anchor5Time">The time at which to draw the fifth anchor point</param>
	/// <param name="anchor5Y">The y value coordinate of the fifth anchor point</param>
	/// <returns></returns>
	public static Polygon Polygon(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time, double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y, DateTime anchor4Time, double anchor4Y, DateTime anchor5Time, double anchor5Y)
	{
		return PolygonBasic(owner, tag, isAutoScale, int.MinValue, anchor1Time, anchor1Y, int.MinValue, anchor2Time, anchor2Y, int.MinValue, anchor3Time, anchor3Y, int.MinValue, anchor4Time, anchor4Y, int.MinValue, anchor5Time, anchor5Y, int.MinValue, DateTime.MinValue, double.MinValue);
	}

	/// <summary>
	/// Draws a Polygon.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1BarsAgo">The number of bars ago (x value) of the 1st anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2BarsAgo">The number of bars ago (x axis coordinate) to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3BarsAgo">The number of bars ago (x axis coordinate) to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="anchor4BarsAgo">The number of bars ago (x axis coordinate) to draw the fourth anchor point</param>
	/// <param name="anchor4Y">The y value coordinate of the fourth anchor point</param>
	/// <param name="anchor5BarsAgo">The number of bars ago (x axis coordinate) to draw the fifth anchor point</param>
	/// <param name="anchor5Y">The y value coordinate of the fifth anchor point</param>
	/// <param name="anchor6BarsAgo">The number of bars ago (x axis coordinate) to draw the sixth anchor point</param>
	/// <param name="anchor6Y">The y value coordinate of the sixth anchor point</param>
	/// <returns></returns>
	public static Polygon Polygon(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y, int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y, int anchor4BarsAgo, double anchor4Y, int anchor5BarsAgo, double anchor5Y, int anchor6BarsAgo, double anchor6Y)
	{
		return PolygonBasic(owner, tag, isAutoScale, anchor1BarsAgo, DateTime.MinValue, anchor1Y, anchor2BarsAgo, DateTime.MinValue, anchor2Y, anchor3BarsAgo, DateTime.MinValue, anchor3Y, anchor4BarsAgo, DateTime.MinValue, anchor4Y, anchor5BarsAgo, DateTime.MinValue, anchor5Y, anchor6BarsAgo, DateTime.MinValue, anchor6Y);
	}

	/// <summary>
	/// Draws a Polygon.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1Time">The time at which to draw the first anchor point</param>
	/// <param name="anchor1Y">The y value coordinate of the first anchor point</param>
	/// <param name="anchor2Time">The time at which to draw the second anchor point</param>
	/// <param name="anchor2Y">The y value coordinate of the second anchor point</param>
	/// <param name="anchor3Time">The time at which to draw the third anchor point</param>
	/// <param name="anchor3Y">The y value coordinate of the third anchor point</param>
	/// <param name="anchor4Time">The time at which to draw the fourth anchor point</param>
	/// <param name="anchor4Y">The y value coordinate of the fourth anchor point</param>
	/// <param name="anchor5Time">The time at which to draw the fifth anchor point</param>
	/// <param name="anchor5Y">The y value coordinate of the fifth anchor point</param>
	/// <param name="anchor6Time">The time at which to draw the sixth anchor point</param>
	/// <param name="anchor6Y">The y value coordinate of the sixth anchor point</param>
	/// <returns></returns>
	public static Polygon Polygon(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time, double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y, DateTime anchor4Time, double anchor4Y, DateTime anchor5Time, double anchor5Y, DateTime anchor6Time, double anchor6Y)
	{
		return PolygonBasic(owner, tag, isAutoScale, int.MinValue, anchor1Time, anchor1Y, int.MinValue, anchor2Time, anchor2Y, int.MinValue, anchor3Time, anchor3Y, int.MinValue, anchor4Time, anchor4Y, int.MinValue, anchor5Time, anchor5Y, int.MinValue, anchor6Time, anchor6Y);
	}

	/// <summary>
	/// Draws a Polygon.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="chartAnchors">A List of ChartAnchor objects defining the polygon</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="dashStyle">The dash style used for the lines of the object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static Polygon Polygon(NinjaScriptBase owner, string tag, bool isAutoScale, List<ChartAnchor> chartAnchors, Brush brush, DashStyleHelper dashStyle, Brush areaBrush, int areaOpacity)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return PolygonCore(owner, tag, isAutoScale, chartAnchors, brush, dashStyle, areaBrush, areaOpacity, isGlobal: false, string.Empty);
	}

	/// <summary>
	/// Draws a Polygon.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="chartAnchors">A List of ChartAnchor objects defining the polygon</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Polygon Polygon(NinjaScriptBase owner, string tag, bool isAutoScale, List<ChartAnchor> chartAnchors, bool isGlobal, string templateName)
	{
		return PolygonCore(owner, tag, isAutoScale, chartAnchors, null, (DashStyleHelper)0, null, int.MinValue, isGlobal, templateName);
	}

	private static Region Region(NinjaScriptBase owner, string tag, int startBarsAgo, DateTime startTime, int endBarsAgo, DateTime endTime, ISeries<double> series1, ISeries<double> series2, double price, Brush outlineBrush, Brush areaBrush, int areaOpacity, int displacement)
	{
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(Region), tag, (string)null) is Region region))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)region, tag, false, owner, false);
		ChartAnchor val = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, 0.0);
		ChartAnchor obj = DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, 0.0);
		val.CopyDataValues(region.StartAnchor);
		obj.CopyDataValues(region.EndAnchor);
		if (series1 == null && series2 == null)
		{
			throw new ArgumentException("At least one series is required");
		}
		region.Series1 = series1;
		region.Series2 = series2;
		region.Price = price;
		region.Displacement = displacement;
		region.AreaBrush = areaBrush;
		region.AreaOpacity = areaOpacity;
		region.OutlineStroke = ((outlineBrush == null) ? ((Stroke)null) : new Stroke(outlineBrush));
		((NinjaScript)region).SetState((State)3);
		((DrawingTool)region).DrawingState = (DrawingState)2;
		((ChartObject)region).IsSelected = false;
		return region;
	}

	/// <summary>
	/// Draws a region on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="series">Any Series double type object such as an indicator, Close, High, Low etc.. The value of the object will represent a y value</param>
	/// <param name="price">Any double value</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="displacement">An optional parameter which will offset the barsAgo value for the Series double value used to match the desired Displacement.  Default value is 0</param>
	/// <returns></returns>
	public static Region Region(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, ISeries<double> series, double price, Brush areaBrush, int areaOpacity, int displacement = 0)
	{
		return Region(owner, tag, startBarsAgo, Globals.MinDate, endBarsAgo, Globals.MinDate, series, null, price, null, areaBrush, areaOpacity, displacement);
	}

	/// <summary>
	/// Draws a region on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="series1">Any Series double type object such as an indicator, Close, High, Low etc.. The value of the object will represent a y value.</param>
	/// <param name="series2">Any Series double type object such as an indicator, Close, High, Low etc.. The value of the object will represent a y value.</param>
	/// <param name="outlineBrush">The brush used to color the region outline of draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="displacement">An optional parameter which will offset the barsAgo value for the Series double value used to match the desired Displacement.  Default value is 0</param>
	/// <returns></returns>
	public static Region Region(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, ISeries<double> series1, ISeries<double> series2, Brush outlineBrush, Brush areaBrush, int areaOpacity, int displacement = 0)
	{
		return Region(owner, tag, startBarsAgo, Globals.MinDate, endBarsAgo, Globals.MinDate, series1, series2, 0.0, outlineBrush, areaBrush, areaOpacity, displacement);
	}

	/// <summary>
	/// Draws a region on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="series">Any Series double type object such as an indicator, Close, High, Low etc.. The value of the object will represent a y value</param>
	/// <param name="price">Any double value</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static Region Region(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, ISeries<double> series, double price, Brush areaBrush, int areaOpacity)
	{
		return Region(owner, tag, int.MinValue, startTime, int.MinValue, endTime, series, null, price, null, areaBrush, areaOpacity, 0);
	}

	/// <summary>
	/// Draws a region on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="series1">Any Series double type object such as an indicator, Close, High, Low etc.. The value of the object will represent a y value.</param>
	/// <param name="series2">Any Series double type object such as an indicator, Close, High, Low etc.. The value of the object will represent a y value.</param>
	/// <param name="outlineBrush">The brush used to color the region outline of draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static Region Region(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, ISeries<double> series1, ISeries<double> series2, Brush outlineBrush, Brush areaBrush, int areaOpacity)
	{
		return Region(owner, tag, int.MinValue, startTime, int.MinValue, endTime, series1, series2, 0.0, outlineBrush, areaBrush, areaOpacity, 0);
	}

	private static T RegionHighlightCore<T>(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY, Brush brush, Brush areaBrush, int areaOpacity, bool isGlobal, string templateName) where T : RegionHighlightBase
	{
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Expected O, but got Unknown
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty", "tag");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			char globalDrawingToolTagPrefix = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix;
			tag = globalDrawingToolTagPrefix + tag;
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(T), tag, templateName) is T val))
		{
			return null;
		}
		RegionHighlightMode regionHighlightMode = (val.Mode = ((!(typeof(T) == typeof(RegionHighlightX))) ? RegionHighlightMode.Price : RegionHighlightMode.Time));
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)val, tag, isAutoScale, owner, isGlobal);
		ChartAnchor val2;
		ChartAnchor val3;
		if (regionHighlightMode == RegionHighlightMode.Time)
		{
			val2 = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
			val3 = DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, endY);
		}
		else
		{
			val2 = DrawingTool.CreateChartAnchor(owner, 0, owner.Time[0], startY);
			val3 = DrawingTool.CreateChartAnchor(owner, 0, owner.Time[0], endY);
		}
		val2.CopyDataValues(val.StartAnchor);
		val3.CopyDataValues(val.EndAnchor);
		if (val.AreaBrush != null && areaBrush != null)
		{
			val.AreaBrush = areaBrush.Clone();
		}
		if (areaOpacity >= 0)
		{
			val.AreaOpacity = areaOpacity;
		}
		if (brush != null)
		{
			val.OutlineStroke = new Stroke(brush);
		}
		((NinjaScript)val).SetState((State)3);
		return val;
	}

	/// <summary>
	/// Draws a region highlight x on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	[CLSCompliant(false)]
	public static RegionHighlightX RegionHighlightX(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, Brush brush)
	{
		return RegionHighlightCore<RegionHighlightX>(owner, tag, isAutoScale: false, int.MinValue, startTime, 0.0, int.MinValue, endTime, 0.0, brush, defaultRegionBrush, 25, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a region highlight x on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	[CLSCompliant(false)]
	public static RegionHighlightX RegionHighlightX(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, Brush brush)
	{
		return RegionHighlightCore<RegionHighlightX>(owner, tag, isAutoScale: false, startBarsAgo, Globals.MinDate, 0.0, endBarsAgo, Globals.MinDate, 0.0, brush, defaultRegionBrush, 25, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a region highlight x on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	[CLSCompliant(false)]
	public static RegionHighlightX RegionHighlightX(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, Brush brush, Brush areaBrush, int areaOpacity)
	{
		return RegionHighlightCore<RegionHighlightX>(owner, tag, isAutoScale: false, int.MinValue, startTime, 0.0, int.MinValue, endTime, 0.0, brush, areaBrush, areaOpacity, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a region highlight x on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	[CLSCompliant(false)]
	public static RegionHighlightX RegionHighlightX(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, Brush brush, Brush areaBrush, int areaOpacity)
	{
		return RegionHighlightCore<RegionHighlightX>(owner, tag, isAutoScale: false, startBarsAgo, Globals.MinDate, 0.0, endBarsAgo, Globals.MinDate, 0.0, brush, areaBrush, areaOpacity, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a region highlight x on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	[CLSCompliant(false)]
	public static RegionHighlightX RegionHighlightX(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, bool isGlobal, string templateName)
	{
		return RegionHighlightCore<RegionHighlightX>(owner, tag, isAutoScale: false, int.MinValue, startTime, 0.0, int.MinValue, endTime, 0.0, null, null, -1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a region highlight x on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	[CLSCompliant(false)]
	public static RegionHighlightX RegionHighlightX(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, bool isGlobal, string templateName)
	{
		return RegionHighlightCore<RegionHighlightX>(owner, tag, isAutoScale: false, startBarsAgo, Globals.MinDate, 0.0, endBarsAgo, Globals.MinDate, 0.0, null, null, -1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a region highlight y on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	[CLSCompliant(false)]
	public static RegionHighlightY RegionHighlightY(NinjaScriptBase owner, string tag, double startY, double endY, Brush brush)
	{
		return RegionHighlightCore<RegionHighlightY>(owner, tag, isAutoScale: false, 0, Globals.MinDate, startY, 0, Globals.MinDate, endY, brush, defaultRegionBrush, 25, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a region highlight y on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	[CLSCompliant(false)]
	public static RegionHighlightY RegionHighlightY(NinjaScriptBase owner, string tag, bool isAutoScale, double startY, double endY, Brush brush, Brush areaBrush, int areaOpacity)
	{
		return RegionHighlightCore<RegionHighlightY>(owner, tag, isAutoScale, 0, Globals.MinDate, startY, 0, Globals.MinDate, endY, brush, areaBrush, areaOpacity, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a region highlight y on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	[CLSCompliant(false)]
	public static RegionHighlightY RegionHighlightY(NinjaScriptBase owner, string tag, double startY, double endY, bool isGlobal, string templateName)
	{
		return RegionHighlightCore<RegionHighlightY>(owner, tag, isAutoScale: false, 0, Globals.MinDate, startY, 0, Globals.MinDate, endY, null, null, -1, isGlobal, templateName);
	}

	private static RegressionChannel RegressionChannelCore(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, DateTime startTime, int endBarsAgo, DateTime endTime, Brush upperBrush, DashStyleHelper upperDashStyle, float? upperWidth, Brush middleBrush, DashStyleHelper middleDashStyle, float? middleWidth, Brush lowerBrush, DashStyleHelper lowerDashStyle, float? lowerWidth, bool isGlobal, string templateName)
	{
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Expected O, but got Unknown
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Expected O, but got Unknown
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Expected O, but got Unknown
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty", "tag");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			char globalDrawingToolTagPrefix = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix;
			tag = globalDrawingToolTagPrefix + tag;
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(RegressionChannel), tag, templateName) is RegressionChannel regressionChannel))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)regressionChannel, tag, isAutoScale, owner, isGlobal);
		int currentBar = DrawingTool.GetCurrentBar(owner);
		double[] array;
		if (startBarsAgo > int.MinValue && endBarsAgo > int.MinValue)
		{
			array = regressionChannel.CalculateRegressionPriceValues(owner.BarsArray[0], currentBar - startBarsAgo, currentBar - endBarsAgo);
		}
		else
		{
			if (!(startTime > Globals.MinDate) || !(endTime > Globals.MinDate))
			{
				throw new ArgumentException("Bad start / end values");
			}
			array = regressionChannel.CalculateRegressionPriceValues(owner.BarsArray[0], owner.BarsArray[0].GetBar(startTime), owner.BarsArray[0].GetBar(endTime));
		}
		ChartAnchor obj = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, array[2]);
		ChartAnchor val = DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, array[3]);
		double slotIndex = (val.SlotIndex = double.MinValue);
		obj.SlotIndex = slotIndex;
		obj.CopyDataValues(regressionChannel.StartAnchor);
		val.CopyDataValues(regressionChannel.EndAnchor);
		if (string.IsNullOrEmpty(templateName))
		{
			Brush obj2 = middleBrush ?? upperBrush;
			Brush brush = lowerBrush ?? upperBrush;
			Stroke val2 = new Stroke(upperBrush)
			{
				DashStyleHelper = upperDashStyle
			};
			if (upperWidth.HasValue)
			{
				val2.Width = upperWidth.Value;
			}
			Stroke val3 = new Stroke(obj2)
			{
				DashStyleHelper = middleDashStyle
			};
			if (middleWidth.HasValue)
			{
				val3.Width = middleWidth.Value;
			}
			Stroke val4 = new Stroke(brush)
			{
				DashStyleHelper = lowerDashStyle
			};
			if (lowerWidth.HasValue)
			{
				val4.Width = lowerWidth.Value;
			}
			val2.CopyTo(regressionChannel.UpperChannelStroke);
			val3.CopyTo(regressionChannel.RegressionStroke);
			val4.CopyTo(regressionChannel.LowerChannelStroke);
		}
		((NinjaScript)regressionChannel).SetState((State)3);
		return regressionChannel;
	}

	/// <summary>
	/// Draws a regression channel.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static RegressionChannel RegressionChannel(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, Brush brush)
	{
		return RegressionChannelCore(owner, tag, isAutoScale: false, startBarsAgo, Globals.MinDate, endBarsAgo, Globals.MinDate, brush, (DashStyleHelper)0, null, null, (DashStyleHelper)0, null, null, (DashStyleHelper)0, null, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a regression channel.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static RegressionChannel RegressionChannel(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, Brush brush)
	{
		return RegressionChannelCore(owner, tag, isAutoScale: false, int.MinValue, startTime, int.MinValue, endTime, brush, (DashStyleHelper)0, null, null, (DashStyleHelper)0, null, null, (DashStyleHelper)0, null, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a regression channel.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="upperBrush">The brush for the upper line</param>
	/// <param name="upperDashStyle">The DashStyle for the upper line</param>
	/// <param name="upperWidth">Width of the upper line.</param>
	/// <param name="middleBrush">The brush for the middle line</param>
	/// <param name="middleDashStyle">The DashStyle for the middle line</param>
	/// <param name="middleWidth">Width of the middle line.</param>
	/// <param name="lowerBrush">The brush for the lower line</param>
	/// <param name="lowerDashStyle">The DashStyle for the lower line</param>
	/// <param name="lowerWidth">Width of the lower line.</param>
	/// <returns></returns>
	public static RegressionChannel RegressionChannel(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, int endBarsAgo, Brush upperBrush, DashStyleHelper upperDashStyle, int upperWidth, Brush middleBrush, DashStyleHelper middleDashStyle, int middleWidth, Brush lowerBrush, DashStyleHelper lowerDashStyle, int lowerWidth)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		return RegressionChannelCore(owner, tag, isAutoScale, startBarsAgo, Globals.MinDate, endBarsAgo, Globals.MinDate, upperBrush, upperDashStyle, upperWidth, middleBrush, middleDashStyle, middleWidth, lowerBrush, lowerDashStyle, lowerWidth, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a regression channel.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="upperBrush">The brush for the upper line</param>
	/// <param name="upperDashStyle">The DashStyle for the upper line</param>
	/// <param name="upperWidth">Width of the upper line.</param>
	/// <param name="middleBrush">The brush for the middle line</param>
	/// <param name="middleDashStyle">The DashStyle for the middle line</param>
	/// <param name="middleWidth">Width of the middle line.</param>
	/// <param name="lowerBrush">The brush for the lower line</param>
	/// <param name="lowerDashStyle">The DashStyle for the lower line</param>
	/// <param name="lowerWidth">Width of the lower line.</param>
	/// <returns></returns>
	public static RegressionChannel RegressionChannel(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, DateTime endTime, Brush upperBrush, DashStyleHelper upperDashStyle, int upperWidth, Brush middleBrush, DashStyleHelper middleDashStyle, int middleWidth, Brush lowerBrush, DashStyleHelper lowerDashStyle, int lowerWidth)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		return RegressionChannelCore(owner, tag, isAutoScale, int.MinValue, startTime, int.MinValue, endTime, upperBrush, upperDashStyle, upperWidth, middleBrush, middleDashStyle, middleWidth, lowerBrush, lowerDashStyle, lowerWidth, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a regression channel.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static RegressionChannel RegressionChannel(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, bool isGlobal, string templateName)
	{
		return RegressionChannelCore(owner, tag, isAutoScale: false, startBarsAgo, Globals.MinDate, endBarsAgo, Globals.MinDate, null, (DashStyleHelper)0, null, null, (DashStyleHelper)0, null, null, (DashStyleHelper)0, null, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a regression channel.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static RegressionChannel RegressionChannel(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, bool isGlobal, string templateName)
	{
		return RegressionChannelCore(owner, tag, isAutoScale: false, int.MinValue, startTime, int.MinValue, endTime, null, (DashStyleHelper)0, null, null, (DashStyleHelper)0, null, null, (DashStyleHelper)0, null, isGlobal, templateName);
	}

	private static RiskReward RiskRewardCore(NinjaScriptBase owner, string tag, bool isAutoScale, int entryBarsAgo, DateTime entryTime, double entryY, int stopBarsAgo, DateTime stopTime, double stopY, int targetBarsAgo, DateTime targetTime, double targetY, double ratio, bool isStop, bool isGlobal, string templateName)
	{
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (entryBarsAgo == int.MinValue && entryTime == Globals.MinDate)
		{
			throw new ArgumentException("entry value required");
		}
		if (stopBarsAgo == int.MinValue && stopTime == Globals.MinDate && targetBarsAgo == int.MinValue && targetTime == Globals.MinDate)
		{
			throw new ArgumentException("a stop or target value is required");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			tag = $"{GlobalDrawingToolManager.GlobalDrawingToolTagPrefix}{tag}";
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(RiskReward), tag, templateName) is RiskReward riskReward))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)riskReward, tag, isAutoScale, owner, isGlobal);
		ChartAnchor val = DrawingTool.CreateChartAnchor(owner, entryBarsAgo, entryTime, entryY);
		riskReward.Ratio = ratio;
		if (isStop)
		{
			ChartAnchor obj = DrawingTool.CreateChartAnchor(owner, stopBarsAgo, stopTime, stopY);
			val.CopyDataValues(riskReward.EntryAnchor);
			val.CopyDataValues(riskReward.RewardAnchor);
			obj.CopyDataValues(riskReward.RiskAnchor);
			riskReward.SetReward();
		}
		else
		{
			ChartAnchor obj2 = DrawingTool.CreateChartAnchor(owner, targetBarsAgo, targetTime, targetY);
			val.CopyDataValues(riskReward.EntryAnchor);
			val.CopyDataValues(riskReward.RiskAnchor);
			obj2.CopyDataValues(riskReward.RewardAnchor);
			riskReward.SetRisk();
		}
		((NinjaScript)riskReward).SetState((State)3);
		return riskReward;
	}

	/// <summary>
	/// Draws a risk/reward on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="entryTime">The time where the draw object's entry will be drawn</param>
	/// <param name="entryY">The y value coordinate where the draw object's entry price will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="ratio">An int value determining the calculated ratio between the risk or reward based on the entry point</param>
	/// <param name="isStop">A bool value, when true will use the endTime/endBarsAgo and endY to set the stop, and will automatically calculate the target based off the ratio value.</param>
	/// <returns></returns>
	public static RiskReward RiskReward(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime entryTime, double entryY, DateTime endTime, double endY, double ratio, bool isStop)
	{
		if (!isStop)
		{
			return RiskRewardCore(owner, tag, isAutoScale, int.MinValue, entryTime, entryY, 0, Globals.MinDate, 0.0, int.MinValue, endTime, endY, ratio, isStop: false, isGlobal: false, null);
		}
		return RiskRewardCore(owner, tag, isAutoScale, int.MinValue, entryTime, entryY, int.MinValue, endTime, endY, 0, Globals.MinDate, 0.0, ratio, isStop: true, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a risk/reward on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="entryBarsAgo">The starting bar (x axis coordinate) where the draw object's entry will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="entryY">The y value coordinate where the draw object's entry price will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="ratio">An int value determining the calculated ratio between the risk or reward based on the entry point</param>
	/// <param name="isStop">A bool value, when true will use the endTime/endBarsAgo and endY to set the stop, and will automatically calculate the target based off the ratio value.</param>
	/// <returns></returns>
	public static RiskReward RiskReward(NinjaScriptBase owner, string tag, bool isAutoScale, int entryBarsAgo, double entryY, int endBarsAgo, double endY, double ratio, bool isStop)
	{
		if (!isStop)
		{
			return RiskRewardCore(owner, tag, isAutoScale, entryBarsAgo, Globals.MinDate, entryY, 0, Globals.MinDate, 0.0, endBarsAgo, Globals.MinDate, endY, ratio, isStop: false, isGlobal: false, null);
		}
		return RiskRewardCore(owner, tag, isAutoScale, entryBarsAgo, Globals.MinDate, entryY, endBarsAgo, Globals.MinDate, endY, 0, Globals.MinDate, 0.0, ratio, isStop: true, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a risk/reward on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="entryTime">The time where the draw object's entry will be drawn</param>
	/// <param name="entryY">The y value coordinate where the draw object's entry price will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="ratio">An int value determining the calculated ratio between the risk or reward based on the entry point</param>
	/// <param name="isStop">A bool value, when true will use the endTime/endBarsAgo and endY to set the stop, and will automatically calculate the target based off the ratio value.</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static RiskReward RiskReward(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime entryTime, double entryY, DateTime endTime, double endY, double ratio, bool isStop, bool isGlobal, string templateName)
	{
		if (!isStop)
		{
			return RiskRewardCore(owner, tag, isAutoScale, int.MinValue, entryTime, entryY, 0, Globals.MinDate, 0.0, int.MinValue, endTime, endY, ratio, isStop: false, isGlobal, templateName);
		}
		return RiskRewardCore(owner, tag, isAutoScale, int.MinValue, entryTime, entryY, int.MinValue, endTime, endY, 0, Globals.MinDate, 0.0, ratio, isStop: true, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a risk/reward on a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="entryBarsAgo">The starting bar (x axis coordinate) where the draw object's entry will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="entryY">The y value coordinate where the draw object's entry price will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="ratio">An int value determining the calculated ratio between the risk or reward based on the entry point</param>
	/// <param name="isStop">A bool value, when true will use the endTime/endBarsAgo and endY to set the stop, and will automatically calculate the target based off the ratio value.</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static RiskReward RiskReward(NinjaScriptBase owner, string tag, bool isAutoScale, int entryBarsAgo, double entryY, int endBarsAgo, double endY, double ratio, bool isStop, bool isGlobal, string templateName)
	{
		if (!isStop)
		{
			return RiskRewardCore(owner, tag, isAutoScale, entryBarsAgo, Globals.MinDate, entryY, 0, Globals.MinDate, 0.0, endBarsAgo, Globals.MinDate, endY, ratio, isStop: false, isGlobal, templateName);
		}
		return RiskRewardCore(owner, tag, isAutoScale, entryBarsAgo, Globals.MinDate, entryY, endBarsAgo, Globals.MinDate, endY, 0, Globals.MinDate, 0.0, ratio, isStop: true, isGlobal, templateName);
	}

	private static Ruler RulerCore(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY, int textBarsAgo, DateTime textTime, double textY, bool isGlobal, string templateName)
	{
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (startTime == Globals.MinDate && endTime == Globals.MinDate && startBarsAgo == int.MinValue && endBarsAgo == int.MinValue)
		{
			throw new ArgumentException("bad start/end date/time");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty", "tag");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			char globalDrawingToolTagPrefix = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix;
			tag = globalDrawingToolTagPrefix + tag;
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(Ruler), tag, templateName) is Ruler ruler))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)ruler, tag, isAutoScale, owner, isGlobal);
		ChartAnchor val = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
		ChartAnchor val2 = DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, endY);
		ChartAnchor obj = DrawingTool.CreateChartAnchor(owner, textBarsAgo, textTime, textY);
		val.CopyDataValues(ruler.StartAnchor);
		val2.CopyDataValues(ruler.EndAnchor);
		obj.CopyDataValues(ruler.TextAnchor);
		((NinjaScript)ruler).SetState((State)3);
		return ruler;
	}

	/// <summary>
	/// Draws a ruler.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="textBarsAgo">The number of bars ago (x value) of the 3rd anchor point</param>
	/// <param name="textY">The y value of the 3rd anchor point</param>
	/// <returns></returns>
	public static Ruler Ruler(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, int textBarsAgo, double textY)
	{
		return RulerCore(owner, tag, isAutoScale, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, textBarsAgo, Globals.MinDate, textY, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a ruler.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="textTime">The time of the 3rd anchor point</param>
	/// <param name="textY">The y value of the 3rd anchor point</param>
	/// <returns></returns>
	public static Ruler Ruler(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, DateTime textTime, double textY)
	{
		return RulerCore(owner, tag, isAutoScale, int.MinValue, startTime, startY, int.MinValue, endTime, endY, int.MinValue, textTime, textY, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a ruler.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="textBarsAgo">The number of bars ago (x value) of the 3rd anchor point</param>
	/// <param name="textY">The y value of the 3rd anchor point</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Ruler Ruler(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, int textBarsAgo, double textY, bool isGlobal, string templateName)
	{
		return RulerCore(owner, tag, isAutoScale, startBarsAgo, Globals.MinDate, startY, endBarsAgo, Globals.MinDate, endY, textBarsAgo, Globals.MinDate, textY, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a ruler.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="textTime">The time of the 3rd anchor point</param>
	/// <param name="textY">The y value of the 3rd anchor point</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Ruler Ruler(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, DateTime textTime, double textY, bool isGlobal, string templateName)
	{
		return RulerCore(owner, tag, isAutoScale, int.MinValue, startTime, startY, int.MinValue, endTime, endY, int.MinValue, textTime, textY, isGlobal, templateName);
	}

	private static T ShapeCore<T>(NinjaScriptBase owner, bool isAutoScale, string tag, int startBarsAgo, int endBarsAgo, DateTime startTime, DateTime endTime, double startY, double endY, Brush brush, Brush areaBrush, int areaOpacity, bool isGlobal, string templateName) where T : ShapeBase
	{
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Expected O, but got Unknown
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			char globalDrawingToolTagPrefix = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix;
			tag = globalDrawingToolTagPrefix + tag;
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(T), tag, templateName) is T val))
		{
			return null;
		}
		if (startTime < Globals.MinDate)
		{
			string[] obj = new string[5]
			{
				((object)val)?.ToString(),
				" startTime must be greater than the minimum Date of ",
				null,
				null,
				null
			};
			DateTime minDate = Globals.MinDate;
			obj[2] = minDate.ToString();
			obj[3] = " but was ";
			obj[4] = startTime.ToString();
			throw new ArgumentException(string.Concat(obj));
		}
		if (endTime < Globals.MinDate)
		{
			string[] obj2 = new string[5]
			{
				((object)val)?.ToString(),
				" endTime must be greater than the minimum Date of ",
				null,
				null,
				null
			};
			DateTime minDate = Globals.MinDate;
			obj2[2] = minDate.ToString();
			obj2[3] = " but was ";
			obj2[4] = endTime.ToString();
			throw new ArgumentException(string.Concat(obj2));
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)val, tag, isAutoScale, owner, isGlobal);
		ChartAnchor val2 = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
		ChartAnchor obj3 = DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, endY);
		val2.CopyDataValues(val.StartAnchor);
		obj3.CopyDataValues(val.EndAnchor);
		if (brush != null)
		{
			val.OutlineStroke = new Stroke(brush, (DashStyleHelper)0, 2f)
			{
				RenderTarget = val.OutlineStroke.RenderTarget
			};
		}
		if (areaOpacity >= 0)
		{
			val.AreaOpacity = areaOpacity;
		}
		if (areaBrush != null)
		{
			val.AreaBrush = areaBrush.Clone();
			if (val.AreaBrush.CanFreeze)
			{
				val.AreaBrush.Freeze();
			}
		}
		((NinjaScript)val).SetState((State)3);
		return val;
	}

	private static Triangle TriangleCore(NinjaScriptBase owner, bool isAutoScale, string tag, int startBarsAgo, int midBarsAgo, int endBarsAgo, DateTime startTime, DateTime midTime, DateTime endTime, double startY, double midY, double endY, Brush color, Brush areaColor, int areaOpacity, bool isGlobal, string templateName)
	{
		Triangle triangle = ShapeCore<Triangle>(owner, isAutoScale, tag, startBarsAgo, endBarsAgo, startTime, endTime, startY, endY, color, areaColor, areaOpacity, isGlobal, templateName);
		DrawingTool.CreateChartAnchor(owner, midBarsAgo, midTime, midY).CopyDataValues(triangle.MiddleAnchor);
		return triangle;
	}

	/// <summary>
	/// Draws an ellipse.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Ellipse Ellipse(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush)
	{
		return ShapeCore<Ellipse>(owner, isAutoScale: false, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, startY, endY, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an ellipse.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static Ellipse Ellipse(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, Brush areaBrush, int areaOpacity)
	{
		return ShapeCore<Ellipse>(owner, isAutoScale, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, startY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an ellipse.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Ellipse Ellipse(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush)
	{
		return ShapeCore<Ellipse>(owner, isAutoScale: false, tag, int.MinValue, int.MinValue, startTime, endTime, startY, endY, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an ellipse.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static Ellipse Ellipse(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, Brush areaBrush, int areaOpacity)
	{
		return ShapeCore<Ellipse>(owner, isAutoScale, tag, int.MinValue, int.MinValue, startTime, endTime, startY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null);
	}

	/// <summary>
	/// Draws an ellipse.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Ellipse Ellipse(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Ellipse>(owner, drawOnPricePanel, (Func<Ellipse>)(() => ShapeCore<Ellipse>(owner, isAutoScale: false, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, startY, endY, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an ellipse.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Ellipse Ellipse(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, Brush areaBrush, int areaOpacity, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Ellipse>(owner, drawOnPricePanel, (Func<Ellipse>)(() => ShapeCore<Ellipse>(owner, isAutoScale, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, startY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an ellipse.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Ellipse Ellipse(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Ellipse>(owner, drawOnPricePanel, (Func<Ellipse>)(() => ShapeCore<Ellipse>(owner, isAutoScale: false, tag, int.MinValue, int.MinValue, startTime, endTime, startY, endY, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an ellipse.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Ellipse Ellipse(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, Brush areaBrush, int areaOpacity, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Ellipse>(owner, drawOnPricePanel, (Func<Ellipse>)(() => ShapeCore<Ellipse>(owner, isAutoScale, tag, int.MinValue, int.MinValue, startTime, endTime, startY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws an ellipse.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Ellipse Ellipse(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, bool isGlobal, string templateName)
	{
		return ShapeCore<Ellipse>(owner, isAutoScale: false, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, startY, endY, null, null, -1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws an ellipse.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Ellipse Ellipse(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, bool isGlobal, string templateName)
	{
		return ShapeCore<Ellipse>(owner, isAutoScale: false, tag, int.MinValue, int.MinValue, startTime, endTime, startY, endY, null, null, -1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a rectangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Rectangle Rectangle(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush)
	{
		return ShapeCore<Rectangle>(owner, isAutoScale: false, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, startY, endY, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a rectangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Rectangle Rectangle(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush)
	{
		return ShapeCore<Rectangle>(owner, isAutoScale: false, tag, int.MinValue, int.MinValue, startTime, endTime, startY, endY, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a rectangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static Rectangle Rectangle(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, Brush areaBrush, int areaOpacity)
	{
		return ShapeCore<Rectangle>(owner, isAutoScale, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, startY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a rectangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static Rectangle Rectangle(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, Brush areaBrush, int areaOpacity)
	{
		return ShapeCore<Rectangle>(owner, isAutoScale, tag, int.MinValue, int.MinValue, startTime, endTime, startY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a rectangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Rectangle Rectangle(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Rectangle>(owner, drawOnPricePanel, (Func<Rectangle>)(() => ShapeCore<Rectangle>(owner, isAutoScale: false, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, startY, endY, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a rectangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Rectangle Rectangle(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo, double endY, Brush brush, Brush areaBrush, int areaOpacity, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Rectangle>(owner, drawOnPricePanel, (Func<Rectangle>)(() => ShapeCore<Rectangle>(owner, isAutoScale, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, startY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a rectangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Rectangle Rectangle(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime endTime, double endY, Brush brush, Brush areaBrush, int areaOpacity, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Rectangle>(owner, drawOnPricePanel, (Func<Rectangle>)(() => ShapeCore<Rectangle>(owner, isAutoScale, tag, int.MinValue, int.MinValue, startTime, endTime, startY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a rectangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Rectangle Rectangle(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int endBarsAgo, double endY, bool isGlobal, string templateName)
	{
		return ShapeCore<Rectangle>(owner, isAutoScale: false, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, startY, endY, null, null, -1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a rectangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Rectangle Rectangle(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime endTime, double endY, bool isGlobal, string templateName)
	{
		return ShapeCore<Rectangle>(owner, isAutoScale: false, tag, int.MinValue, int.MinValue, startTime, endTime, startY, endY, null, null, -1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a triangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="middleBarsAgo">The number of bars ago (x value) of the 2nd anchor point</param>
	/// <param name="middleY">The y value of the 2nd anchor point</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Triangle Triangle(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int middleBarsAgo, double middleY, int endBarsAgo, double endY, Brush brush)
	{
		return TriangleCore(owner, isAutoScale: false, tag, startBarsAgo, middleBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, Globals.MinDate, startY, middleY, endY, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a triangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="middleTime">The middle time.</param>
	/// <param name="middleY">The y value of the 2nd anchor point</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static Triangle Triangle(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime middleTime, double middleY, DateTime endTime, double endY, Brush brush)
	{
		return TriangleCore(owner, isAutoScale: false, tag, int.MinValue, int.MinValue, int.MinValue, startTime, middleTime, endTime, startY, middleY, endY, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a triangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="middleBarsAgo">The number of bars ago (x value) of the 2nd anchor point</param>
	/// <param name="middleY">The y value of the 2nd anchor point</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static Triangle Triangle(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int middleBarsAgo, double middleY, int endBarsAgo, double endY, Brush brush, Brush areaBrush, int areaOpacity)
	{
		return TriangleCore(owner, isAutoScale, tag, startBarsAgo, middleBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, Globals.MinDate, startY, middleY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a triangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="midTime">The time of the 2nd anchor point</param>
	/// <param name="middleY">The y value of the 2nd anchor point</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static Triangle Triangle(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime midTime, double middleY, DateTime endTime, double endY, Brush brush, Brush areaBrush, int areaOpacity)
	{
		return TriangleCore(owner, isAutoScale, tag, int.MinValue, int.MinValue, int.MinValue, startTime, midTime, endTime, startY, middleY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a triangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="middleBarsAgo">The number of bars ago (x value) of the 2nd anchor point</param>
	/// <param name="middleY">The y value of the 2nd anchor point</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Triangle Triangle(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int middleBarsAgo, double middleY, int endBarsAgo, double endY, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Triangle>(owner, drawOnPricePanel, (Func<Triangle>)(() => TriangleCore(owner, isAutoScale: false, tag, startBarsAgo, middleBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, Globals.MinDate, startY, middleY, endY, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a triangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="middleBarsAgo">The number of bars ago (x value) of the 2nd anchor point</param>
	/// <param name="middleY">The y value of the 2nd anchor point</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Triangle Triangle(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int middleBarsAgo, double middleY, int endBarsAgo, double endY, Brush brush, Brush areaBrush, int areaOpacity, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Triangle>(owner, drawOnPricePanel, (Func<Triangle>)(() => TriangleCore(owner, isAutoScale, tag, startBarsAgo, middleBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, Globals.MinDate, startY, middleY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a triangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="midTime">The time of the 2nd anchor point</param>
	/// <param name="middleY">The y value of the 2nd anchor point</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static Triangle Triangle(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime startTime, double startY, DateTime midTime, double middleY, DateTime endTime, double endY, Brush brush, Brush areaBrush, int areaOpacity, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<Triangle>(owner, drawOnPricePanel, (Func<Triangle>)(() => TriangleCore(owner, isAutoScale, tag, int.MinValue, int.MinValue, int.MinValue, startTime, midTime, endTime, startY, middleY, endY, brush, areaBrush, areaOpacity, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws a triangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="middleBarsAgo">The number of bars ago (x value) of the 2nd anchor point</param>
	/// <param name="middleY">The y value of the 2nd anchor point</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Triangle Triangle(NinjaScriptBase owner, string tag, int startBarsAgo, double startY, int middleBarsAgo, double middleY, int endBarsAgo, double endY, bool isGlobal, string templateName)
	{
		return TriangleCore(owner, isAutoScale: false, tag, startBarsAgo, middleBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, Globals.MinDate, startY, middleY, endY, null, null, -1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a triangle.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="startY">The starting y value coordinate where the draw object will be drawn</param>
	/// <param name="middleTime">The middle time.</param>
	/// <param name="middleY">The y value of the 2nd anchor point</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="endY">The end y value coordinate where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Triangle Triangle(NinjaScriptBase owner, string tag, DateTime startTime, double startY, DateTime middleTime, double middleY, DateTime endTime, double endY, bool isGlobal, string templateName)
	{
		return TriangleCore(owner, isAutoScale: false, tag, int.MinValue, int.MinValue, int.MinValue, startTime, middleTime, endTime, startY, middleY, endY, null, null, -1, isGlobal, templateName);
	}

	private static Text TextCore(NinjaScriptBase owner, string tag, bool autoScale, string text, int barsAgo, DateTime time, double y, int? yPixelOffset, Brush textBrush, TextAlignment? textAlignment, SimpleFont font, Brush outlineBrush, Brush areaBrush, int? areaOpacity, bool isGlobal, string templateName, DashStyleHelper outlineDashStyle, int outlineWidth)
	{
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Expected O, but got Unknown
		if (barsAgo == int.MinValue && time == Globals.MinDate)
		{
			throw new ArgumentException("Text: Bad barsAgo/time parameters");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty", "tag");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			char globalDrawingToolTagPrefix = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix;
			tag = globalDrawingToolTagPrefix + tag;
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(Text), tag, templateName) is Text text2))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)text2, tag, autoScale, owner, isGlobal);
		DrawingTool.CreateChartAnchor(owner, barsAgo, time, y).CopyDataValues(text2.Anchor);
		((NinjaScript)text2).SetState((State)3);
		text2.DisplayText = text;
		if (textBrush != null)
		{
			text2.TextBrush = textBrush;
		}
		text2.UseChartTextBrush = text2.TextBrush == null;
		if (textAlignment.HasValue)
		{
			text2.Alignment = textAlignment.Value;
		}
		else if (string.IsNullOrEmpty(templateName))
		{
			text2.Alignment = TextAlignment.Center;
		}
		if (outlineBrush != null)
		{
			text2.OutlineStroke = new Stroke(outlineBrush, outlineDashStyle, (float)outlineWidth)
			{
				RenderTarget = text2.OutlineStroke.RenderTarget
			};
		}
		if (areaBrush != null)
		{
			text2.AreaBrush = areaBrush;
		}
		if (areaOpacity.HasValue)
		{
			text2.AreaOpacity = areaOpacity.Value;
		}
		if (font != null)
		{
			object obj = font.Clone();
			text2.Font = (SimpleFont)((obj is SimpleFont) ? obj : null);
		}
		if (yPixelOffset.HasValue)
		{
			text2.YPixelOffset = yPixelOffset.Value;
		}
		text2.ManuallyDrawn = false;
		return text2;
	}

	/// <summary>
	/// Draws text.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <returns></returns>
	public static Text Text(NinjaScriptBase owner, string tag, string text, int barsAgo, double y)
	{
		return TextCore(owner, tag, autoScale: false, text, barsAgo, Globals.MinDate, y, null, null, TextAlignment.Center, null, null, null, null, isGlobal: false, null, (DashStyleHelper)0, 0);
	}

	/// <summary>
	/// Draws text.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="textBrush">The brush used to color the text of the draw object</param>
	/// <returns></returns>
	public static Text Text(NinjaScriptBase owner, string tag, string text, int barsAgo, double y, Brush textBrush)
	{
		return TextCore(owner, tag, autoScale: false, text, barsAgo, Globals.MinDate, y, null, textBrush, TextAlignment.Center, null, null, null, null, isGlobal: false, null, (DashStyleHelper)0, 0);
	}

	/// <summary>
	/// Draws text.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Text Text(NinjaScriptBase owner, string tag, string text, int barsAgo, double y, bool isGlobal, string templateName)
	{
		return TextCore(owner, tag, autoScale: false, text, barsAgo, Globals.MinDate, y, null, null, null, null, null, null, null, isGlobal, templateName, (DashStyleHelper)0, 0);
	}

	/// <summary>
	/// Draws text.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="yPixelOffset">The offset value in pixels from within the text box area</param>
	/// <param name="textBrush">The brush used to color the text of the draw object</param>
	/// <param name="font">A SimpleFont object</param>
	/// <param name="alignment">The TextAlignment for the textbox</param>
	/// <param name="outlineBrush">The brush used to color the region outline of draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static Text Text(NinjaScriptBase owner, string tag, bool isAutoScale, string text, int barsAgo, double y, int yPixelOffset, Brush textBrush, SimpleFont font, TextAlignment alignment, Brush outlineBrush, Brush areaBrush, int areaOpacity)
	{
		return TextCore(owner, tag, isAutoScale, text, barsAgo, Globals.MinDate, y, yPixelOffset, textBrush, alignment, font, outlineBrush, areaBrush, areaOpacity, isGlobal: false, null, (DashStyleHelper)0, 2);
	}

	/// <summary>
	/// Draws text.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="yPixelOffset">The offset value in pixels from within the text box area</param>
	/// <param name="textBrush">The brush used to color the text of the draw object</param>
	/// <param name="font">A SimpleFont object</param>
	/// <param name="alignment">The TextAlignment for the textbox</param>
	/// <param name="outlineBrush">The brush used to color the region outline of draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static Text Text(NinjaScriptBase owner, string tag, bool isAutoScale, string text, DateTime time, double y, int yPixelOffset, Brush textBrush, SimpleFont font, TextAlignment alignment, Brush outlineBrush, Brush areaBrush, int areaOpacity)
	{
		return TextCore(owner, tag, isAutoScale, text, int.MinValue, time, y, yPixelOffset, textBrush, alignment, font, outlineBrush, areaBrush, areaOpacity, isGlobal: false, null, (DashStyleHelper)0, 2);
	}

	/// <summary>
	/// Draws text.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="barsAgo">The bar the object will be drawn at. A value of 10 would be 10 bars ago</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="yPixelOffset">The offset value in pixels from within the text box area</param>
	/// <param name="textBrush">The brush used to color the text of the draw object</param>
	/// <param name="font">A SimpleFont object</param>
	/// <param name="alignment">The TextAlignment for the textbox</param>
	/// <param name="outlineBrush">The brush used to color the region outline of draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="outlineDashStyle">The outline dash style.</param>
	/// <param name="outlineWidth">Width of the outline.</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Text Text(NinjaScriptBase owner, string tag, bool isAutoScale, string text, int barsAgo, double y, int yPixelOffset, Brush textBrush, SimpleFont font, TextAlignment alignment, Brush outlineBrush, Brush areaBrush, int areaOpacity, DashStyleHelper outlineDashStyle, int outlineWidth, bool isGlobal, string templateName)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		return TextCore(owner, tag, isAutoScale, text, barsAgo, Globals.MinDate, y, yPixelOffset, textBrush, alignment, font, outlineBrush, areaBrush, areaOpacity, isGlobal, templateName, outlineDashStyle, outlineWidth);
	}

	/// <summary>
	/// Draws text.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="time"> The time the object will be drawn at.</param>
	/// <param name="y">The y value or Price for the object</param>
	/// <param name="yPixelOffset">The offset value in pixels from within the text box area</param>
	/// <param name="textBrush">The brush used to color the text of the draw object</param>
	/// <param name="font">A SimpleFont object</param>
	/// <param name="alignment">The TextAlignment for the textbox</param>
	/// <param name="outlineBrush">The brush used to color the region outline of draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="outlineDashStyle">The outline dash style.</param>
	/// <param name="outlineWidth">Width of the outline.</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static Text Text(NinjaScriptBase owner, string tag, bool isAutoScale, string text, DateTime time, double y, int yPixelOffset, Brush textBrush, SimpleFont font, TextAlignment alignment, Brush outlineBrush, Brush areaBrush, int areaOpacity, DashStyleHelper outlineDashStyle, int outlineWidth, bool isGlobal, string templateName)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		return TextCore(owner, tag, isAutoScale, text, int.MinValue, time, y, yPixelOffset, textBrush, alignment, font, outlineBrush, areaBrush, areaOpacity, isGlobal, templateName, outlineDashStyle, outlineWidth);
	}

	/// <summary>
	/// Texts the fixed core.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="textPosition">The TextPosition of the text</param>
	/// <param name="textBrush">The brush used to color the text of the draw object</param>
	/// <param name="font">A SimpleFont object</param>
	/// <param name="outlineBrush">The brush used to color the region outline of draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <param name="outlineDashStyle">The outline dash style.</param>
	/// <param name="outlineWidth">Width of the outline.</param>
	/// <returns></returns>
	private static TextFixed TextFixedCore(NinjaScriptBase owner, string tag, string text, TextPosition textPosition, Brush textBrush, SimpleFont font, Brush outlineBrush, Brush areaBrush, int? areaOpacity, bool isGlobal, string templateName, DashStyleHelper outlineDashStyle, int outlineWidth)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(TextFixed), tag, templateName) is TextFixed textFixed))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)textFixed, tag, false, owner, isGlobal);
		((NinjaScript)textFixed).SetState((State)3);
		textFixed.DisplayText = text;
		textFixed.TextPosition = textPosition;
		if (textBrush != null)
		{
			textFixed.TextBrush = textBrush;
		}
		textFixed.UseChartTextBrush = textFixed.TextBrush == null;
		if (outlineBrush != null)
		{
			textFixed.OutlineStroke = new Stroke(outlineBrush, outlineDashStyle, (float)outlineWidth)
			{
				RenderTarget = textFixed.OutlineStroke.RenderTarget
			};
		}
		if (areaBrush != null)
		{
			textFixed.AreaBrush = areaBrush;
		}
		if (areaOpacity.HasValue)
		{
			textFixed.AreaOpacity = areaOpacity.Value;
		}
		if (font != null)
		{
			textFixed.Font = font;
		}
		return textFixed;
	}

	/// <summary>
	/// Texts the fixed core with fine TextPosition.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="textPositionFine">The TextPositionFine of the text</param>
	/// <param name="textBrush">The brush used to color the text of the draw object</param>
	/// <param name="font">A SimpleFont object</param>
	/// <param name="outlineBrush">The brush used to color the region outline of draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <param name="outlineDashStyle">The outline dash style.</param>
	/// <param name="outlineWidth">Width of the outline.</param>
	/// <returns></returns>
	private static TextFixedFine TextFixedFineCore(NinjaScriptBase owner, string tag, string text, TextPositionFine textPositionFine, Brush textBrush, SimpleFont font, Brush outlineBrush, Brush areaBrush, int? areaOpacity, bool isGlobal, string templateName, DashStyleHelper outlineDashStyle, int outlineWidth)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(TextFixedFine), tag, templateName) is TextFixedFine textFixedFine))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)textFixedFine, tag, false, owner, isGlobal);
		((NinjaScript)textFixedFine).SetState((State)3);
		textFixedFine.DisplayText = text;
		textFixedFine.TextPositionFine = textPositionFine;
		if (textBrush != null)
		{
			textFixedFine.TextBrush = textBrush;
		}
		textFixedFine.UseChartTextBrush = textFixedFine.TextBrush == null;
		if (outlineBrush != null)
		{
			textFixedFine.OutlineStroke = new Stroke(outlineBrush, outlineDashStyle, (float)outlineWidth)
			{
				RenderTarget = textFixedFine.OutlineStroke.RenderTarget
			};
		}
		if (areaBrush != null)
		{
			textFixedFine.AreaBrush = areaBrush;
		}
		if (areaOpacity.HasValue)
		{
			textFixedFine.AreaOpacity = areaOpacity.Value;
		}
		if (font != null)
		{
			textFixedFine.Font = font;
		}
		return textFixedFine;
	}

	/// <summary>
	/// Draws text in one of 5 available pre-defined fixed locations on panel 1 (price panel) of a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="textPosition">The TextPosition of the text</param>
	/// <param name="textBrush">The brush used to color the text of the draw object</param>
	/// <param name="font">A SimpleFont object</param>
	/// <param name="outlineBrush">The brush used to color the region outline of draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static TextFixed TextFixed(NinjaScriptBase owner, string tag, string text, TextPosition textPosition, Brush textBrush, SimpleFont font, Brush outlineBrush, Brush areaBrush, int areaOpacity)
	{
		return TextFixedCore(owner, tag, text, textPosition, textBrush, font, outlineBrush, areaBrush, areaOpacity, isGlobal: false, null, (DashStyleHelper)0, 2);
	}

	/// <summary>
	/// Draws text in one of 5 available pre-defined fixed locations on panel 1 (price panel) of a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="textPosition">The TextPosition of the text</param>
	/// <param name="textBrush">The brush used to color the text of the draw object</param>
	/// <param name="font">A SimpleFont object</param>
	/// <param name="outlineBrush">The brush used to color the region outline of draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="outlineDashStyle">The outline dash style.</param>
	/// <param name="outlineWidth">Width of the outline.</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static TextFixed TextFixed(NinjaScriptBase owner, string tag, string text, TextPosition textPosition, Brush textBrush, SimpleFont font, Brush outlineBrush, Brush areaBrush, int areaOpacity, DashStyleHelper outlineDashStyle, int outlineWidth, bool isGlobal, string templateName)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		return TextFixedCore(owner, tag, text, textPosition, textBrush, font, outlineBrush, areaBrush, areaOpacity, isGlobal, templateName, outlineDashStyle, outlineWidth);
	}

	/// <summary>
	/// Draws text in one of 5 available pre-defined fixed locations on panel 1 (price panel) of a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="textPosition">The TextPosition of the text</param>
	/// <returns></returns>
	public static TextFixed TextFixed(NinjaScriptBase owner, string tag, string text, TextPosition textPosition)
	{
		return TextFixedCore(owner, tag, text, textPosition, null, null, null, null, null, isGlobal: false, null, (DashStyleHelper)0, 0);
	}

	/// <summary>
	/// Draws text in one of 5 available pre-defined fixed locations on panel 1 (price panel) of a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="textPosition">The TextPosition of the text</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static TextFixed TextFixed(NinjaScriptBase owner, string tag, string text, TextPosition textPosition, bool isGlobal, string templateName)
	{
		return TextFixedCore(owner, tag, text, textPosition, null, null, null, null, null, isGlobal, templateName, (DashStyleHelper)0, 0);
	}

	/// <summary>
	/// Draws text in one of 5 available pre-defined fixed locations on panel 1 (price panel) of a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="textPositionFine">The TextPosition of the text</param>
	/// <returns></returns>
	public static TextFixedFine TextFixedFine(NinjaScriptBase owner, string tag, string text, TextPositionFine textPositionFine)
	{
		return TextFixedFineCore(owner, tag, text, textPositionFine, null, null, null, null, null, isGlobal: false, null, (DashStyleHelper)0, 0);
	}

	/// <summary>
	/// Draws text in one of 8 available pre-defined fixed locations on panel 1 (price panel) of a chart.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="text">The text you wish to draw</param>
	/// <param name="textPositionFine">The TextPositionFine of the text</param>
	/// <param name="textBrush">The brush used to color the text of the draw object</param>
	/// <param name="font">A SimpleFont object</param>
	/// <param name="outlineBrush">The brush used to color the region outline of draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static TextFixedFine TextFixedFine(NinjaScriptBase owner, string tag, string text, TextPositionFine textPositionFine, Brush textBrush, SimpleFont font, Brush outlineBrush, Brush areaBrush, int areaOpacity)
	{
		return TextFixedFineCore(owner, tag, text, textPositionFine, textBrush, font, outlineBrush, areaBrush, areaOpacity, isGlobal: false, null, (DashStyleHelper)0, 2);
	}

	private static TimeCycles TimeCyclesCore(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, DateTime startTime, DateTime endTime, Brush brush, Brush areaBrush, int areaOpacity, bool isGlobal, string templateName)
	{
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Expected O, but got Unknown
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			char globalDrawingToolTagPrefix = GlobalDrawingToolManager.GlobalDrawingToolTagPrefix;
			tag = globalDrawingToolTagPrefix + tag;
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(TimeCycles), tag, templateName) is TimeCycles timeCycles))
		{
			return null;
		}
		if (startTime < Globals.MinDate)
		{
			throw new ArgumentException($"{timeCycles} startTime must be greater than the minimum Date but was {startTime}");
		}
		if (endTime < Globals.MinDate)
		{
			throw new ArgumentException($"{timeCycles} endTime must be greater than the minimum Date but was {endTime}");
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)timeCycles, tag, false, owner, isGlobal);
		ChartAnchor val = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, 0.0);
		ChartAnchor obj = DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, 0.0);
		val.CopyDataValues(timeCycles.StartAnchor);
		obj.CopyDataValues(timeCycles.EndAnchor);
		if (brush != null)
		{
			timeCycles.OutlineStroke = new Stroke(brush, (DashStyleHelper)0, 2f)
			{
				RenderTarget = timeCycles.OutlineStroke.RenderTarget
			};
		}
		if (areaOpacity >= 0)
		{
			timeCycles.AreaOpacity = areaOpacity;
		}
		if (areaBrush != null)
		{
			timeCycles.AreaBrush = areaBrush.Clone();
			if (timeCycles.AreaBrush.CanFreeze)
			{
				timeCycles.AreaBrush.Freeze();
			}
		}
		((NinjaScript)timeCycles).SetState((State)3);
		return timeCycles;
	}

	/// <summary>
	/// Draws time cycles.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static TimeCycles TimeCycles(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, Brush brush)
	{
		return TimeCyclesCore(owner, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null);
	}

	/// <summary>
	/// Draws time cycles.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static TimeCycles TimeCycles(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, Brush brush, Brush areaBrush, int areaOpacity)
	{
		return TimeCyclesCore(owner, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, brush, areaBrush, areaOpacity, isGlobal: false, null);
	}

	/// <summary>
	/// Draws time cycles.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <returns></returns>
	public static TimeCycles TimeCycles(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, Brush brush)
	{
		return TimeCyclesCore(owner, tag, int.MinValue, int.MinValue, startTime, endTime, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null);
	}

	/// <summary>
	/// Draws time cycles.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <returns></returns>
	public static TimeCycles TimeCycles(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, Brush brush, Brush areaBrush, int areaOpacity)
	{
		return TimeCyclesCore(owner, tag, int.MinValue, int.MinValue, startTime, endTime, brush, areaBrush, areaOpacity, isGlobal: false, null);
	}

	/// <summary>
	/// Draws time cycles.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static TimeCycles TimeCycles(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<TimeCycles>(owner, drawOnPricePanel, (Func<TimeCycles>)(() => TimeCyclesCore(owner, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws time cycles.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static TimeCycles TimeCycles(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, Brush brush, Brush areaBrush, int areaOpacity, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<TimeCycles>(owner, drawOnPricePanel, (Func<TimeCycles>)(() => TimeCyclesCore(owner, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, brush, areaBrush, areaOpacity, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws time cycles.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static TimeCycles TimeCycles(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, Brush brush, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<TimeCycles>(owner, drawOnPricePanel, (Func<TimeCycles>)(() => TimeCyclesCore(owner, tag, int.MinValue, int.MinValue, startTime, endTime, brush, Brushes.CornflowerBlue, 40, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws time cycles.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="brush">The brush used to color draw object</param>
	/// <param name="areaBrush">The brush used to color the fill region area of the draw object</param>
	/// <param name="areaOpacity"> Sets the level of transparency for the fill color. Valid values between 0 - 100. (0 = completely transparent, 100 = no opacity)</param>
	/// <param name="drawOnPricePanel">Determines if the draw-object should be on the price panel or a separate panel</param>
	/// <returns></returns>
	public static TimeCycles TimeCycles(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, Brush brush, Brush areaBrush, int areaOpacity, bool drawOnPricePanel)
	{
		return DrawingTool.DrawToggledPricePanel<TimeCycles>(owner, drawOnPricePanel, (Func<TimeCycles>)(() => TimeCyclesCore(owner, tag, int.MinValue, int.MinValue, startTime, endTime, brush, areaBrush, areaOpacity, isGlobal: false, null)));
	}

	/// <summary>
	/// Draws time cycles.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startBarsAgo">The starting bar (x axis coordinate) where the draw object will be drawn. For example, a value of 10 would paint the draw object 10 bars back.</param>
	/// <param name="endBarsAgo">The end bar (x axis coordinate) where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static TimeCycles TimeCycles(NinjaScriptBase owner, string tag, int startBarsAgo, int endBarsAgo, bool isGlobal, string templateName)
	{
		return TimeCyclesCore(owner, tag, startBarsAgo, endBarsAgo, Globals.MinDate, Globals.MinDate, null, null, -1, isGlobal, templateName);
	}

	/// <summary>
	/// Draws time cycles.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="startTime">The starting time where the draw object will be drawn.</param>
	/// <param name="endTime">The end time where the draw object will terminate</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static TimeCycles TimeCycles(NinjaScriptBase owner, string tag, DateTime startTime, DateTime endTime, bool isGlobal, string templateName)
	{
		return TimeCyclesCore(owner, tag, int.MinValue, int.MinValue, startTime, endTime, null, null, -1, isGlobal, templateName);
	}

	private static TrendChannel TrendChannelCore(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, DateTime anchor1Time, double anchor1Y, int anchor2BarsAgo, DateTime anchor2Time, double anchor2Y, int anchor3BarsAgo, DateTime anchor3Time, double anchor3Y, bool isGlobal, string templateName)
	{
		if (owner == null)
		{
			throw new ArgumentException("owner");
		}
		if (string.IsNullOrWhiteSpace(tag))
		{
			throw new ArgumentException("tag cant be null or empty");
		}
		if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
		{
			tag = $"{GlobalDrawingToolManager.GlobalDrawingToolTagPrefix}{tag}";
		}
		if (!(DrawingTool.GetByTagOrNew(owner, typeof(TrendChannel), tag, templateName) is TrendChannel trendChannel))
		{
			return null;
		}
		DrawingTool.SetDrawingToolCommonValues((IDrawingTool)(object)trendChannel, tag, isAutoScale, owner, isGlobal);
		ChartAnchor val = DrawingTool.CreateChartAnchor(owner, anchor1BarsAgo, anchor1Time, anchor1Y);
		ChartAnchor val2 = DrawingTool.CreateChartAnchor(owner, anchor2BarsAgo, anchor2Time, anchor2Y);
		ChartAnchor obj = DrawingTool.CreateChartAnchor(owner, anchor3BarsAgo, anchor3Time, anchor3Y);
		val.CopyDataValues(trendChannel.TrendStartAnchor);
		val2.CopyDataValues(trendChannel.TrendEndAnchor);
		obj.CopyDataValues(trendChannel.ParallelStartAnchor);
		((NinjaScript)trendChannel).SetState((State)3);
		return trendChannel;
	}

	/// <summary>
	/// Draws a trend channel.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1BarsAgo">The number of bars ago (x value) of the 1st anchor point</param>
	/// <param name="anchor1Y">The y value of the 1st anchor point</param>
	/// <param name="anchor2BarsAgo">The number of bars ago (x value) of the 2nd anchor point</param>
	/// <param name="anchor2Y">The y value of the 2nd anchor point</param>
	/// <param name="anchor3BarsAgo">The number of bars ago (x value) of the 3rd anchor point</param>
	/// <param name="anchor3Y">The y value of the 3rd anchor point</param>
	/// <returns></returns>
	public static TrendChannel TrendChannel(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y, int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y)
	{
		return TrendChannelCore(owner, tag, isAutoScale, anchor1BarsAgo, Globals.MinDate, anchor1Y, anchor2BarsAgo, Globals.MinDate, anchor2Y, anchor3BarsAgo, Globals.MinDate, anchor3Y, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a trend channel.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1Time">The time of the 1st anchor point</param>
	/// <param name="anchor1Y">The y value of the 1st anchor point</param>
	/// <param name="anchor2Time">The time of the 2nd anchor point</param>
	/// <param name="anchor2Y">The y value of the 2nd anchor point</param>
	/// <param name="anchor3Time">The time of the 3rd anchor point</param>
	/// <param name="anchor3Y">The y value of the 3rd anchor point</param>
	/// <returns></returns>
	public static TrendChannel TrendChannel(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time, double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y)
	{
		return TrendChannelCore(owner, tag, isAutoScale, int.MinValue, anchor1Time, anchor1Y, int.MinValue, anchor2Time, anchor2Y, int.MinValue, anchor3Time, anchor3Y, isGlobal: false, null);
	}

	/// <summary>
	/// Draws a trend channel.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1BarsAgo">The number of bars ago (x value) of the 1st anchor point</param>
	/// <param name="anchor1Y">The y value of the 1st anchor point</param>
	/// <param name="anchor2BarsAgo">The number of bars ago (x value) of the 2nd anchor point</param>
	/// <param name="anchor2Y">The y value of the 2nd anchor point</param>
	/// <param name="anchor3BarsAgo">The number of bars ago (x value) of the 3rd anchor point</param>
	/// <param name="anchor3Y">The y value of the 3rd anchor point</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static TrendChannel TrendChannel(NinjaScriptBase owner, string tag, bool isAutoScale, int anchor1BarsAgo, double anchor1Y, int anchor2BarsAgo, double anchor2Y, int anchor3BarsAgo, double anchor3Y, bool isGlobal, string templateName)
	{
		return TrendChannelCore(owner, tag, isAutoScale, anchor1BarsAgo, Globals.MinDate, anchor1Y, anchor2BarsAgo, Globals.MinDate, anchor2Y, anchor3BarsAgo, Globals.MinDate, anchor3Y, isGlobal, templateName);
	}

	/// <summary>
	/// Draws a trend channel.
	/// </summary>
	/// <param name="owner">The hosting NinjaScript object which is calling the draw method</param>
	/// <param name="tag">A user defined unique id used to reference the draw object</param>
	/// <param name="isAutoScale">Determines if the draw object will be included in the y-axis scale</param>
	/// <param name="anchor1Time">The time of the 1st anchor point</param>
	/// <param name="anchor1Y">The y value of the 1st anchor point</param>
	/// <param name="anchor2Time">The time of the 2nd anchor point</param>
	/// <param name="anchor2Y">The y value of the 2nd anchor point</param>
	/// <param name="anchor3Time">The time of the 3rd anchor point</param>
	/// <param name="anchor3Y">The y value of the 3rd anchor point</param>
	/// <param name="isGlobal">Determines if the draw object will be global across all charts which match the instrument</param>
	/// <param name="templateName">The name of the drawing tool template the object will use to determine various visual properties</param>
	/// <returns></returns>
	public static TrendChannel TrendChannel(NinjaScriptBase owner, string tag, bool isAutoScale, DateTime anchor1Time, double anchor1Y, DateTime anchor2Time, double anchor2Y, DateTime anchor3Time, double anchor3Y, bool isGlobal, string templateName)
	{
		return TrendChannelCore(owner, tag, isAutoScale, int.MinValue, anchor1Time, anchor1Y, int.MinValue, anchor2Time, anchor2Y, int.MinValue, anchor3Time, anchor3Y, isGlobal, templateName);
	}
}

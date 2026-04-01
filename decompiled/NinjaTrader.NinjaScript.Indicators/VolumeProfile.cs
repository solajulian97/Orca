using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;

namespace NinjaTrader.NinjaScript.Indicators;

public class VolumeProfile : Indicator
{
	internal class VolumeInfoItem
	{
		public double up;

		public double down;

		public double neutral;
	}

	private double alpha = 50.0;

	private double askPrice;

	private readonly int barSpacing = 1;

	private double bidPrice;

	private DateTime cacheSessionEnd = Globals.MinDate;

	private DateTime currentDate = Globals.MinDate;

	private bool drawLines;

	private readonly List<int> newSessionBarIdx = new List<int>();

	private DateTime sessionDateTmp = Globals.MinDate;

	private SessionIterator sessionIterator;

	private int startIndexOf;

	private SessionIterator storedSession;

	private readonly List<Dictionary<double, VolumeInfoItem>> sortedDicList = new List<Dictionary<double, VolumeInfoItem>>();

	private Dictionary<double, VolumeInfoItem> cacheDictionary = new Dictionary<double, VolumeInfoItem>();

	[Range(0.0, double.MaxValue)]
	[Display(ResourceType = typeof(Resource), Name = "Opacity", Order = 0, GroupName = "NinjaScriptParameters")]
	public double Opacity
	{
		get
		{
			return alpha;
		}
		set
		{
			alpha = Math.Max(1.0, value);
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "DrawLines", Order = 1, GroupName = "NinjaScriptParameters")]
	public bool DrawLines
	{
		get
		{
			return drawLines;
		}
		set
		{
			drawLines = value;
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "LineColor", Order = 2, GroupName = "NinjaScriptParameters")]
	public Brush LineBrush { get; set; }

	[Browsable(false)]
	public string LineBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(LineBrush);
		}
		set
		{
			LineBrush = Serialize.StringToBrush(value);
		}
	}

	private SessionIterator SessionIterator
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Expected O, but got Unknown
			//IL_001d: Expected O, but got Unknown
			SessionIterator obj = sessionIterator;
			if (obj == null)
			{
				SessionIterator val = new SessionIterator(((NinjaScriptBase)this).Bars);
				SessionIterator val2 = val;
				sessionIterator = val;
				obj = val2;
			}
			return obj;
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "VolumeDownColor", Order = 3, GroupName = "NinjaScriptParameters")]
	public Brush VolumeDownBrush { get; set; }

	[Browsable(false)]
	public string VolumeDownBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(VolumeDownBrush);
		}
		set
		{
			VolumeDownBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "VolumeNeutralColor", Order = 4, GroupName = "NinjaScriptParameters")]
	public Brush VolumeNeutralBrush { get; set; }

	[Browsable(false)]
	public string VolumeNeutralBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(VolumeNeutralBrush);
		}
		set
		{
			VolumeNeutralBrush = Serialize.StringToBrush(value);
		}
	}

	[XmlIgnore]
	[Display(ResourceType = typeof(Resource), Name = "VolumeUpColor", Order = 5, GroupName = "NinjaScriptParameters")]
	public Brush VolumeUpBrush { get; set; }

	[Browsable(false)]
	public string VolumeUpBrushSerialize
	{
		get
		{
			return Serialize.BrushToString(VolumeUpBrush);
		}
		set
		{
			VolumeUpBrush = Serialize.StringToBrush(value);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Invalid comparison between Unknown and I4
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Invalid comparison between Unknown and I4
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Invalid comparison between Unknown and I4
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionVolumeProfile;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameVolumeProfile;
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			DrawLines = false;
			((IndicatorBase)this).IsChartOnly = true;
			((NinjaScriptBase)this).IsOverlay = true;
			((IndicatorBase)this).DrawOnPricePanel = false;
			LineBrush = Brushes.DarkGray;
			VolumeDownBrush = Brushes.Crimson;
			VolumeNeutralBrush = Brushes.DarkGray;
			VolumeUpBrush = Brushes.DarkCyan;
		}
		else if ((int)((NinjaScript)this).State == 2)
		{
			((IndicatorRenderBase)this).ZOrder = -1;
		}
		else if ((int)((NinjaScript)this).State == 4)
		{
			storedSession = new SessionIterator(((NinjaScriptBase)this).Bars);
		}
		else if ((int)((NinjaScript)this).State == 5 && (int)((NinjaScriptBase)this).Calculate != 1)
		{
			Draw.TextFixed((NinjaScriptBase)(object)this, "NinjaScriptInfo", string.Format(Resource.NinjaScriptOnBarCloseError, ((NinjaScriptBase)this).Name), TextPosition.BottomRight);
		}
	}

	protected override void OnBarUpdate()
	{
	}

	private DateTime GetLastBarSessionDate(DateTime time)
	{
		if (time <= cacheSessionEnd)
		{
			return sessionDateTmp;
		}
		if (!((NinjaScriptBase)this).Bars.BarsType.IsIntraday)
		{
			return sessionDateTmp;
		}
		storedSession.GetNextSession(time, true);
		cacheSessionEnd = storedSession.ActualSessionEnd;
		sessionDateTmp = TimeZoneInfo.ConvertTime(cacheSessionEnd.AddSeconds(-1.0), Globals.GeneralOptions.TimeZoneInfo, ((NinjaScriptBase)this).Bars.TradingHours.TimeZoneInfo);
		if (newSessionBarIdx.Count == 0 || (newSessionBarIdx.Count > 0 && ((NinjaScriptBase)this).CurrentBar > newSessionBarIdx[newSessionBarIdx.Count - 1]))
		{
			newSessionBarIdx.Add(((NinjaScriptBase)this).CurrentBar);
		}
		return sessionDateTmp;
	}

	protected override void OnMarketData(MarketDataEventArgs e)
	{
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Invalid comparison between Unknown and I4
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Invalid comparison between Unknown and I4
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Invalid comparison between Unknown and I4
		if (((NinjaScriptBase)this).Bars.Count <= 0)
		{
			return;
		}
		DateTime lastBarSessionDate = GetLastBarSessionDate(((NinjaScriptBase)this).Time[0]);
		if (lastBarSessionDate != currentDate)
		{
			cacheDictionary = new Dictionary<double, VolumeInfoItem>();
			sortedDicList.Add(cacheDictionary);
		}
		currentDate = lastBarSessionDate;
		if (((NinjaScriptBase)this).Bars.IsTickReplay)
		{
			if ((int)e.MarketDataType == 2)
			{
				double price = e.Price;
				long volume = e.Volume;
				if (!cacheDictionary.ContainsKey(price))
				{
					cacheDictionary.Add(price, new VolumeInfoItem());
				}
				VolumeInfoItem volumeInfoItem = cacheDictionary[price];
				if (price >= e.Ask)
				{
					volumeInfoItem.up += volume;
				}
				else if (price <= e.Bid)
				{
					volumeInfoItem.down += volume;
				}
				else
				{
					volumeInfoItem.neutral += volume;
				}
			}
		}
		else if ((int)e.MarketDataType == 0)
		{
			askPrice = e.Price;
		}
		else if ((int)e.MarketDataType == 1)
		{
			bidPrice = e.Price;
		}
		else if ((int)e.MarketDataType == 2 && ((IndicatorRenderBase)this).ChartControl != null && askPrice != 0.0 && bidPrice != 0.0 && (((NinjaScriptBase)this).Bars == null || SessionIterator.IsInSession(Globals.Now, true, true)))
		{
			double price = e.Price;
			long volume = e.Volume;
			if (!cacheDictionary.ContainsKey(price))
			{
				cacheDictionary.Add(price, new VolumeInfoItem());
			}
			VolumeInfoItem volumeInfoItem = cacheDictionary[price];
			if (price >= askPrice)
			{
				volumeInfoItem.up += volume;
			}
			else if (price <= bidPrice)
			{
				volumeInfoItem.down += volume;
			}
			else
			{
				volumeInfoItem.neutral += volume;
			}
		}
	}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Unknown result type (might be due to invalid IL or missing references)
		//IL_044a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0469: Unknown result type (might be due to invalid IL or missing references)
		if (((NinjaScriptBase)this).Bars == null || ((NinjaScriptBase)this).Bars.Instrument == null || ((IndicatorRenderBase)this).IsInHitTest)
		{
			return;
		}
		int val = -1;
		double tickSize = ((NinjaScriptBase)this).Bars.Instrument.MasterInstrument.TickSize;
		double num = 0.0;
		Brush val2 = DxExtensions.ToDxBrush(VolumeUpBrush, ((IndicatorRenderBase)this).RenderTarget);
		Brush val3 = DxExtensions.ToDxBrush(VolumeDownBrush, ((IndicatorRenderBase)this).RenderTarget);
		Brush val4 = DxExtensions.ToDxBrush(VolumeNeutralBrush, ((IndicatorRenderBase)this).RenderTarget);
		Brush val5 = DxExtensions.ToDxBrush(LineBrush, ((IndicatorRenderBase)this).RenderTarget);
		val2.Opacity = (float)(alpha / 100.0);
		val3.Opacity = (float)(alpha / 100.0);
		val4.Opacity = (float)(alpha / 100.0);
		for (int num2 = newSessionBarIdx.Count - 1; num2 > 0; num2--)
		{
			if (newSessionBarIdx[num2] <= ((IndicatorRenderBase)this).ChartBars.ToIndex)
			{
				startIndexOf = num2;
				val = newSessionBarIdx[num2];
				break;
			}
		}
		if (sortedDicList.Count < 1 && cacheDictionary.Keys.Count > 0)
		{
			sortedDicList.Add(cacheDictionary);
		}
		foreach (Dictionary<double, VolumeInfoItem> sortedDic in sortedDicList)
		{
			foreach (KeyValuePair<double, VolumeInfoItem> item in sortedDic)
			{
				double key = item.Key;
				if (!((NinjaScriptBase)this).Bars.BarsType.IsIntraday || (!(key > chartScale.MaxValue) && !(key < chartScale.MinValue)))
				{
					VolumeInfoItem value = item.Value;
					double val6 = value.up + value.down + value.neutral;
					num = Math.Max(num, val6);
				}
			}
		}
		if (MathExtentions.ApproxCompare(num, 0.0) == 0)
		{
			return;
		}
		int num3 = 0;
		foreach (KeyValuePair<double, VolumeInfoItem> item2 in sortedDicList[startIndexOf])
		{
			num3++;
			VolumeInfoItem value2 = item2.Value;
			double num4 = item2.Key - tickSize / 2.0;
			float num5 = chartScale.GetYByValue(num4);
			float num6 = chartScale.GetYByValue(num4 + tickSize);
			float num7 = Math.Max(1f, Math.Abs(num6 - num5) - (float)barSpacing);
			int num8 = (int)((double)((IndicatorRenderBase)this).ChartPanel.W / 2.0 * (value2.up / num));
			int num9 = (int)((double)((IndicatorRenderBase)this).ChartPanel.W / 2.0 * (value2.neutral / num));
			int num10 = (int)((double)((IndicatorRenderBase)this).ChartPanel.W / 2.0 * (value2.down / num));
			float num11 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, (!((NinjaScriptBase)this).Bars.IsTickReplay) ? ((IndicatorRenderBase)this).ChartBars.FromIndex : Math.Max(((IndicatorRenderBase)this).ChartBars.FromIndex, val));
			float num12 = chartControl.GetXByBarIndex(((IndicatorRenderBase)this).ChartBars, (!((NinjaScriptBase)this).Bars.IsTickReplay) ? ((IndicatorRenderBase)this).ChartBars.FromIndex : (Math.Max(1, Math.Max(((IndicatorRenderBase)this).ChartBars.FromIndex, val)) - 1));
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(num12, num6, (float)num8, num7), val2);
			num12 += (float)num8;
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(num12, num6, (float)num9, num7), val4);
			num12 += (float)num9;
			((IndicatorRenderBase)this).RenderTarget.FillRectangle(new RectangleF(num12, num6, (float)num10, num7), val3);
			if (drawLines)
			{
				((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num11, num5), new Vector2((float)(((IndicatorRenderBase)this).ChartPanel.X + ((IndicatorRenderBase)this).ChartPanel.W), num5), val5);
				if (num3 == sortedDicList[startIndexOf].Count)
				{
					((IndicatorRenderBase)this).RenderTarget.DrawLine(new Vector2(num11, num6), new Vector2((float)(((IndicatorRenderBase)this).ChartPanel.X + ((IndicatorRenderBase)this).ChartPanel.W), num6), val5);
				}
			}
		}
		((DisposeBase)val5).Dispose();
		((DisposeBase)val2).Dispose();
		((DisposeBase)val3).Dispose();
		((DisposeBase)val4).Dispose();
	}
}

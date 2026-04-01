using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators;

public class BarTimer : Indicator
{
	private string timeLeft = string.Empty;

	private DateTime now = Globals.Now;

	private bool connected;

	private bool hasRealtimeData;

	private SessionIterator sessionIterator;

	private DispatcherTimer timer;

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

	private DateTime Now
	{
		get
		{
			now = ((Connection.PlaybackConnection != null) ? Connection.PlaybackConnection.Now : Globals.Now);
			if (now.Millisecond > 0)
			{
				DateTime minDate = Globals.MinDate;
				now = minDate.AddSeconds((long)Math.Floor(now.Subtract(Globals.MinDate).TotalSeconds));
			}
			return now;
		}
	}

	[Display(ResourceType = typeof(Resource), Name = "GuiPropertyNameTextPosition", GroupName = "PropertyCategoryVisual", Order = 70)]
	public TextPositionFine TextPositionFine { get; set; }

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((NinjaScript)this).Description = Resource.NinjaScriptIndicatorDescriptionBarTimer;
			((NinjaScriptBase)this).Name = Resource.NinjaScriptIndicatorNameBarTimer;
			((NinjaScriptBase)this).Calculate = (Calculate)1;
			((IndicatorBase)this).DrawOnPricePanel = false;
			((IndicatorBase)this).IsChartOnly = true;
			((NinjaScriptBase)this).IsOverlay = true;
			((NinjaScriptBase)this).DisplayInDataBox = false;
			TextPositionFine = TextPositionFine.BottomRight;
		}
		else if ((int)((NinjaScript)this).State == 7)
		{
			if (timer != null || !((NinjaScript)this).IsVisible)
			{
				return;
			}
			if (((NinjaScriptBase)this).Bars.BarsType.IsTimeBased && ((NinjaScriptBase)this).Bars.BarsType.IsIntraday)
			{
				lock (Connection.Connections)
				{
					if (Connection.Connections.ToList().FirstOrDefault((Connection c) => (int)c.Status == 3 && c.InstrumentTypes.Contains(((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType)) == null)
					{
						Draw.TextFixedFine((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.BarTimerDisconnectedError, TextPositionFine, ((IndicatorRenderBase)this).ChartControl.Properties.ChartText, ((IndicatorRenderBase)this).ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
					}
					else
					{
						Draw.TextFixedFine((NinjaScriptBase)(object)this, "NinjaScriptInfo", (!SessionIterator.IsInSession(Now, false, true)) ? Resource.BarTimerSessionTimeError : Resource.BarTimerWaitingOnDataError, TextPositionFine, ((IndicatorRenderBase)this).ChartControl.Properties.ChartText, ((IndicatorRenderBase)this).ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
					}
					return;
				}
			}
			Draw.TextFixedFine((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.BarTimerTimeBasedError, TextPositionFine, ((IndicatorRenderBase)this).ChartControl.Properties.ChartText, ((IndicatorRenderBase)this).ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
		}
		else if ((int)((NinjaScript)this).State == 8 && timer != null)
		{
			timer.IsEnabled = false;
			timer = null;
		}
	}

	protected override void OnBarUpdate()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 7)
		{
			hasRealtimeData = true;
			connected = true;
		}
	}

	protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if ((int)connectionStatusUpdate.PriceStatus == 3 && connectionStatusUpdate.Connection.InstrumentTypes.Contains(((NinjaScriptBase)this).Instrument.MasterInstrument.InstrumentType) && ((NinjaScriptBase)this).Bars.BarsType.IsTimeBased && ((NinjaScriptBase)this).Bars.BarsType.IsIntraday)
		{
			connected = true;
			if (DisplayTime() && timer == null)
			{
				((DispatcherObject)(object)((IndicatorRenderBase)this).ChartControl).Dispatcher.InvokeAsync(delegate
				{
					timer = new DispatcherTimer
					{
						Interval = new TimeSpan(0, 0, 1),
						IsEnabled = true
					};
					timer.Tick += OnTimerTick;
				});
			}
		}
		else if ((int)connectionStatusUpdate.PriceStatus == 0)
		{
			connected = false;
		}
	}

	private bool DisplayTime()
	{
		if (((IndicatorRenderBase)this).ChartControl != null)
		{
			Bars bars = ((NinjaScriptBase)this).Bars;
			if (((bars != null) ? bars.Instrument.MarketData : null) != null)
			{
				return ((NinjaScript)this).IsVisible;
			}
		}
		return false;
	}

	private void OnTimerTick(object sender, EventArgs e)
	{
		((IndicatorRenderBase)this).ForceRefresh();
		if (!DisplayTime())
		{
			return;
		}
		DispatcherTimer dispatcherTimer = timer;
		if (dispatcherTimer != null && !dispatcherTimer.IsEnabled)
		{
			timer.IsEnabled = true;
		}
		if (connected)
		{
			if (SessionIterator.IsInSession(Now, false, true))
			{
				if (hasRealtimeData)
				{
					TimeSpan timeSpan = ((NinjaScriptBase)this).Bars.GetTime(((NinjaScriptBase)this).Bars.Count - 1).Subtract(Now);
					timeLeft = ((timeSpan.Ticks < 0) ? "00:00:00" : (timeSpan.Hours.ToString("00") + ":" + timeSpan.Minutes.ToString("00") + ":" + timeSpan.Seconds.ToString("00")));
					Draw.TextFixedFine((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.BarTimerTimeRemaining + timeLeft, TextPositionFine, ((IndicatorRenderBase)this).ChartControl.Properties.ChartText, ((IndicatorRenderBase)this).ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
				}
				else
				{
					Draw.TextFixedFine((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.BarTimerWaitingOnDataError, TextPositionFine, ((IndicatorRenderBase)this).ChartControl.Properties.ChartText, ((IndicatorRenderBase)this).ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
				}
			}
			else
			{
				Draw.TextFixedFine((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.BarTimerSessionTimeError, TextPositionFine, ((IndicatorRenderBase)this).ChartControl.Properties.ChartText, ((IndicatorRenderBase)this).ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
			}
		}
		else
		{
			Draw.TextFixedFine((NinjaScriptBase)(object)this, "NinjaScriptInfo", Resource.BarTimerDisconnectedError, TextPositionFine, ((IndicatorRenderBase)this).ChartControl.Properties.ChartText, ((IndicatorRenderBase)this).ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
			if (timer != null)
			{
				timer.IsEnabled = false;
			}
		}
	}
}

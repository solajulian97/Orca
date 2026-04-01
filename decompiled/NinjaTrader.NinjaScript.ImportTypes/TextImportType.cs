using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.ImportTypes;

public class TextImportType : ImportType
{
	private readonly char[] quotes = new char[1] { '"' };

	private CultureInfo cultureInfo;

	private int currentInstrumentIdx = -1;

	private bool firstLine = true;

	private bool hasBidAsk;

	private bool hasSubSecond;

	private bool isCryptoCurrency;

	private StreamReader reader;

	private Regex regex;

	private string separator = string.Empty;

	public bool EndOfBarTimestamps { get; set; }

	public string[] FileNames { get; set; }

	protected override void Dispose(bool isDisposing)
	{
		reader?.Dispose();
		reader = null;
	}

	protected override void OnNextDataPoint()
	{
		//IL_0724: Unknown result type (might be due to invalid IL or missing references)
		//IL_0761: Unknown result type (might be due to invalid IL or missing references)
		//IL_079e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Invalid comparison between Unknown and I4
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Invalid comparison between Unknown and I4
		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0858: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c8: Unknown result type (might be due to invalid IL or missing references)
		if (reader == null)
		{
			return;
		}
		MatchCollection matchCollection;
		string text2;
		while (true)
		{
			((ImportType)this).DataPointString = reader.ReadLine();
			if (((ImportType)this).DataPointString == null)
			{
				reader.Close();
				reader = null;
				return;
			}
			((ImportType)this).DataPointString = ((ImportType)this).DataPointString.Trim();
			if (((ImportType)this).DataPointString.Length == 0)
			{
				continue;
			}
			if (firstLine)
			{
				separator = string.Empty;
				string[] array = new string[2] { ";", "," };
				foreach (string text in array)
				{
					if (new Regex(string.Format(CultureInfo.InvariantCulture, "{0}\"?[^{1}]+\"?{2}", text, text, text)).Match(((ImportType)this).DataPointString).Success)
					{
						separator = text;
						break;
					}
				}
				if (separator.Length == 0)
				{
					Log.Process(typeof(Resource), "ImportTypeNinjaTraderFieldSeparatorNotIdentified", new object[1] { ((ImportType)this).Instrument.FullName }, (LogLevel)3, (LogCategories)4);
					reader.Close();
					reader = null;
					throw new InvalidOperationException();
				}
				regex = new Regex(string.Format(CultureInfo.InvariantCulture, "\"?[^{0}]+\"?", separator));
				firstLine = false;
			}
			matchCollection = regex.Matches(((ImportType)this).DataPointString);
			if (matchCollection.Count != 0)
			{
				text2 = matchCollection[0].Value.Trim(quotes).Trim().Replace("-", string.Empty)
					.Replace(":", string.Empty)
					.Replace(" ", string.Empty);
				if (text2.Length != 0 && char.IsDigit(text2[0]))
				{
					break;
				}
			}
		}
		if (matchCollection.Count != 3 && matchCollection.Count != 5 && matchCollection.Count != 6)
		{
			Log.Process(typeof(Resource), "ImportTypeNinjaTraderUnexpectedFieldNumber", new object[2]
			{
				((ImportType)this).Instrument.FullName,
				((ImportType)this).NumberOfDataPoints
			}, (LogLevel)3, (LogCategories)4);
			reader.Close();
			reader = null;
			throw new InvalidOperationException();
		}
		if (((ImportType)this).NumberOfDataPoints == 0)
		{
			if (matchCollection.Count >= 6 && text2.Length == 8)
			{
				((ImportType)this).BarsPeriodType = (BarsPeriodType)5;
			}
			else if (matchCollection.Count >= 6 && (text2.Length == 12 || text2.Length == 14))
			{
				((ImportType)this).BarsPeriodType = (BarsPeriodType)4;
			}
			else if (matchCollection.Count == 3 && text2.Length == 14)
			{
				((ImportType)this).BarsPeriodType = (BarsPeriodType)0;
				hasBidAsk = false;
				hasSubSecond = false;
			}
			else if (matchCollection.Count == 3 && text2.Length == 21)
			{
				((ImportType)this).BarsPeriodType = (BarsPeriodType)0;
				hasBidAsk = false;
				hasSubSecond = true;
			}
			else if (matchCollection.Count == 5 && text2.Length == 14)
			{
				((ImportType)this).BarsPeriodType = (BarsPeriodType)0;
				hasBidAsk = true;
				hasSubSecond = false;
			}
			else if (matchCollection.Count == 5 && text2.Length == 21)
			{
				((ImportType)this).BarsPeriodType = (BarsPeriodType)0;
				hasBidAsk = true;
				hasSubSecond = true;
			}
		}
		((ImportType)this).Time = Globals.MinDate;
		try
		{
			if ((int)((ImportType)this).BarsPeriodType == 5)
			{
				((ImportType)this).Time = new DateTime(Convert.ToInt32(text2.Substring(0, 4), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(4, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(6, 2), CultureInfo.InvariantCulture));
			}
			else if ((int)((ImportType)this).BarsPeriodType == 4)
			{
				((ImportType)this).Time = new DateTime(Convert.ToInt32(text2.Substring(0, 4), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(4, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(6, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(8, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(10, 2), CultureInfo.InvariantCulture), 0);
				if (!EndOfBarTimestamps)
				{
					((ImportType)this).Time = ((ImportType)this).Time.AddMinutes(1.0);
				}
			}
			else if ((int)((ImportType)this).BarsPeriodType == 0 && !hasSubSecond)
			{
				((ImportType)this).Time = new DateTime(Convert.ToInt32(text2.Substring(0, 4), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(4, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(6, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(8, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(10, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(12, 2), CultureInfo.InvariantCulture));
			}
			else if ((int)((ImportType)this).BarsPeriodType == 0 && hasSubSecond)
			{
				((ImportType)this).Time = new DateTime(new DateTime(Convert.ToInt32(text2.Substring(0, 4), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(4, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(6, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(8, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(10, 2), CultureInfo.InvariantCulture), Convert.ToInt32(text2.Substring(12, 2), CultureInfo.InvariantCulture)).Ticks + Convert.ToInt32(text2.Substring(14, 7), CultureInfo.InvariantCulture));
			}
		}
		catch (Exception ex)
		{
			Log.Process(typeof(Resource), "ImportTypeNinjaTraderDateTimeFormatError", new object[4]
			{
				((ImportType)this).Instrument.FullName,
				((ImportType)this).NumberOfDataPoints,
				ex.Message,
				((ImportType)this).DataPointString
			}, (LogLevel)3, (LogCategories)4);
			reader.Close();
			reader = null;
			throw new InvalidOperationException();
		}
		if (cultureInfo == null)
		{
			List<CultureInfo> list = new List<CultureInfo>();
			try
			{
				CultureInfo item = new CultureInfo("en-US");
				list.Add(item);
			}
			catch
			{
			}
			try
			{
				CultureInfo item = (CultureInfo)CultureInfo.CurrentCulture.Clone();
				list.Add(item);
			}
			catch
			{
			}
			try
			{
				CultureInfo item = new CultureInfo("de-DE");
				list.Add(item);
			}
			catch
			{
			}
			foreach (CultureInfo item2 in list)
			{
				item2.NumberFormat.NumberGroupSeparator = string.Empty;
				try
				{
					((ImportType)this).Open = Convert.ToDouble(matchCollection[1].Value.Trim(quotes).Trim(), item2);
					cultureInfo = item2;
				}
				catch
				{
					continue;
				}
				break;
			}
			if (cultureInfo == null)
			{
				Log.Process(typeof(Resource), "ImportTypeNinjaTraderNumericPriceFormatError", new object[1] { ((ImportType)this).Instrument.FullName }, (LogLevel)3, (LogCategories)4);
				try
				{
					reader.Close();
				}
				catch
				{
				}
				reader = null;
				throw new InvalidOperationException();
			}
		}
		try
		{
			((ImportType)this).Open = Convert.ToDouble(matchCollection[1].Value.Trim(quotes).Trim(), cultureInfo);
			((ImportType)this).High = (((int)((ImportType)this).BarsPeriodType == 0) ? ((ImportType)this).Open : Convert.ToDouble(matchCollection[2].Value.Trim(quotes).Trim(), cultureInfo));
			((ImportType)this).Low = (((int)((ImportType)this).BarsPeriodType == 0) ? ((ImportType)this).Open : Convert.ToDouble(matchCollection[3].Value.Trim(quotes).Trim(), cultureInfo));
			((ImportType)this).Close = (((int)((ImportType)this).BarsPeriodType == 0) ? ((ImportType)this).Open : Convert.ToDouble(matchCollection[4].Value.Trim(quotes).Trim(), cultureInfo));
			if (hasBidAsk)
			{
				((ImportType)this).Bid = Convert.ToDouble(matchCollection[2].Value.Trim(quotes).Trim(), cultureInfo);
				((ImportType)this).Ask = Convert.ToDouble(matchCollection[3].Value.Trim(quotes).Trim(), cultureInfo);
			}
			else
			{
				double bid = (((ImportType)this).Ask = double.MinValue);
				((ImportType)this).Bid = bid;
			}
			if ((int)((ImportType)this).BarsPeriodType != 0)
			{
				((ImportType)this).Volume = (isCryptoCurrency ? Globals.FromCryptocurrencyVolume(Convert.ToDouble(matchCollection[5].Value.Trim(quotes).Trim(), cultureInfo)) : Convert.ToInt64(matchCollection[5].Value.Trim(quotes).Trim(), cultureInfo));
			}
			else if ((int)((ImportType)this).BarsPeriodType == 0 && !hasBidAsk)
			{
				((ImportType)this).Volume = (isCryptoCurrency ? Globals.FromCryptocurrencyVolume(Convert.ToDouble(matchCollection[2].Value.Trim(quotes).Trim(), cultureInfo)) : Convert.ToInt64(matchCollection[2].Value.Trim(quotes).Trim(), cultureInfo));
			}
			else
			{
				((ImportType)this).Volume = (isCryptoCurrency ? Globals.FromCryptocurrencyVolume(Convert.ToDouble(matchCollection[4].Value.Trim(quotes).Trim(), cultureInfo)) : Convert.ToInt64(matchCollection[4].Value.Trim(quotes).Trim(), cultureInfo));
			}
			((ImportType)this).HasValidDataPoint = true;
		}
		catch (Exception ex2)
		{
			Log.Process(typeof(Resource), "ImportTypeNinjaTraderFormatError", new object[4]
			{
				((ImportType)this).Instrument.FullName,
				((ImportType)this).NumberOfDataPoints,
				ex2.Message,
				((ImportType)this).DataPointString
			}, (LogLevel)3, (LogCategories)4);
			reader.Close();
			reader = null;
			throw new InvalidOperationException();
		}
	}

	protected override void OnNextInstrument()
	{
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Invalid comparison between Unknown and I4
		if (FileNames == null)
		{
			return;
		}
		while (((ImportType)this).Instrument == null && currentInstrumentIdx + 1 < FileNames.Length)
		{
			FileInfo fileInfo = new FileInfo(FileNames[++currentInstrumentIdx]);
			string text = fileInfo.Name.ToLowerInvariant().Replace(".ask.", ".").Replace(".bid.", ".")
				.Replace(".last.", ".");
			((ImportType)this).Instrument = Instrument.GetInstrument((fileInfo.Extension.Length == 4 && text.Length > fileInfo.Extension.Length) ? text.Substring(0, text.Length - fileInfo.Extension.Length).ToUpperInvariant() : text.ToUpperInvariant(), true);
			if (((ImportType)this).Instrument == null)
			{
				Log.Process(typeof(Resource), "ImportTypeNinjaTraderInstrumentNotSupported", new object[1] { FileNames[currentInstrumentIdx] }, (LogLevel)3, (LogCategories)4);
				continue;
			}
			isCryptoCurrency = (int)((ImportType)this).Instrument.MasterInstrument.InstrumentType == 7;
			try
			{
				reader = new StreamReader(FileNames[currentInstrumentIdx]);
			}
			catch (Exception ex)
			{
				Log.Process(typeof(Resource), "ImportTypeNinjaTraderUnableReadData", new object[2]
				{
					FileNames[currentInstrumentIdx],
					ex.Message
				}, (LogLevel)3, (LogCategories)4);
				((ImportType)this).Instrument = null;
				continue;
			}
			cultureInfo = null;
			firstLine = true;
			((ImportType)this).HasValidInstrument = true;
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		State state = ((NinjaScript)this).State;
		if ((int)state != 1)
		{
			if ((int)state != 2)
			{
				if ((int)state == 8)
				{
					((ImportType)this).Dispose(true);
				}
			}
			else if (FileNames == null)
			{
				OpenFileDialog openFileDialog = new OpenFileDialog
				{
					FileName = Resource.FileName,
					Filter = Resource.FileFilterAnyWinForms,
					InitialDirectory = RecentFolders.GetRecentFolder("HistoryImport", Environment.GetFolderPath(Environment.SpecialFolder.Personal)),
					Multiselect = true,
					Title = Resource.Load
				};
				if (openFileDialog.ShowDialog() != true)
				{
					((NinjaScript)this).SetState((State)8);
					return;
				}
				if (openFileDialog.FileNames.Length == 0)
				{
					((NinjaScript)this).SetState((State)8);
					return;
				}
				RecentFolders.SetRecentFolder("HistoryImport", Path.GetDirectoryName(openFileDialog.FileNames[0]));
				FileNames = openFileDialog.FileNames;
			}
		}
		else
		{
			EndOfBarTimestamps = true;
			((NinjaScript)this).Name = Resource.ImportTypeNinjaTraderEndOfBar;
		}
	}
}

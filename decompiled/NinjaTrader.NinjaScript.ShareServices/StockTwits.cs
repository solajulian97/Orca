using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.ShareServices;

public class StockTwits : ShareService
{
	public enum Sentiment
	{
		Neutral,
		Bearish,
		Bullish
	}

	private object icon;

	public override object Icon => icon ?? (icon = Application.Current.TryFindResource("ShareIconStockTwits"));

	[Encrypt]
	[Browsable(false)]
	public string OAuthToken { get; set; }

	[Browsable(false)]
	[ShareField]
	[Display(ResourceType = typeof(Resource), Name = "StockTwitsSentiment", Description = "StockTwitsSentimentDescription")]
	public Sentiment StockTwitsSentiment { get; set; }

	[ReadOnly(true)]
	[Display(ResourceType = typeof(Resource), Name = "ShareServiceUserName", GroupName = "ShareServiceParameters", Order = 1)]
	public string UserName { get; set; }

	/// <summary>
	/// This MUST be overridden for any custom service properties to be copied over when instances of the service are created
	/// </summary>
	/// <param name="ninjaScript"></param>
	public override void CopyTo(NinjaScript ninjaScript)
	{
		((ShareService)this).CopyTo(ninjaScript);
		PropertyInfo[] properties = ((object)ninjaScript).GetType().GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.Name == "OAuth_Token")
			{
				propertyInfo.SetValue(ninjaScript, OAuthToken);
			}
			else if (propertyInfo.Name == "StockTwitsSentiment")
			{
				propertyInfo.SetValue(ninjaScript, StockTwitsSentiment);
			}
			else if (propertyInfo.Name == "UserName")
			{
				propertyInfo.SetValue(ninjaScript, UserName);
			}
		}
	}

	public override Task OnAuthorizeAccount()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_0048: Expected O, but got Unknown
		NTWindow val = new NTWindow
		{
			Caption = Resource.GuiAuthorize,
			IsModal = true
		};
		((FrameworkElement)val).Height = 650.0;
		((FrameworkElement)val).Width = 900.0;
		NTWindow authWin = val;
		Window webHost = new Window
		{
			ResizeMode = ResizeMode.NoResize,
			ShowInTaskbar = false,
			WindowStyle = WindowStyle.None
		};
		WebBrowser webBrowser = new WebBrowser
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		Grid grid = new Grid();
		grid.Children.Add(webBrowser);
		webHost.Content = grid;
		Grid placementGrid = new Grid();
		((ContentControl)(object)authWin).Content = placementGrid;
		((Window)(object)authWin).LocationChanged += delegate
		{
			OnSizeLocationChanged(placementGrid, webHost);
		};
		placementGrid.SizeChanged += delegate
		{
			OnSizeLocationChanged(placementGrid, webHost);
		};
		string oauthToken = string.Empty;
		HideScriptErrors(webBrowser);
		webBrowser.Navigating += async delegate(object _, NavigatingCancelEventArgs e)
		{
			if (e.Uri.Host == "www.ninjatrader.com")
			{
				if (e.Uri.Fragment.StartsWith("#access_token"))
				{
					string[] array = e.Uri.Fragment.TrimStart('#').Split('&');
					for (int i = 0; i < array.Length; i++)
					{
						string[] array2 = array[i].Split('=');
						if (array2[0] == "access_token")
						{
							oauthToken = array2[1];
						}
					}
					OAuthToken = oauthToken;
					string requestUri = "https://api.stocktwits.com/api/2/account/verify.json?access_token=" + OAuthToken;
					using (HttpClient client = new HttpClient())
					{
						string input = await new StreamReader((await client.GetAsync(requestUri)).Content.ReadAsStreamAsync().Result).ReadToEndAsync();
						if (!(new JavaScriptSerializer().DeserializeObject(input) is Dictionary<string, object> dictionary))
						{
							((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareStockTwitsNoAccount", (object[])null, (LogLevel)3);
							authWin.DialogResult = false;
							((Window)(object)authWin).Close();
							return;
						}
						if (!dictionary.TryGetValue("user", out var value))
						{
							((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareStockTwitsNoAccount", (object[])null, (LogLevel)3);
							authWin.DialogResult = false;
							((Window)(object)authWin).Close();
							return;
						}
						if (!(value is Dictionary<string, object> dictionary2))
						{
							((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareStockTwitsNoAccount", (object[])null, (LogLevel)3);
							authWin.DialogResult = false;
							((Window)(object)authWin).Close();
							return;
						}
						if (!dictionary2.TryGetValue("username", out var value2))
						{
							((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareStockTwitsNoAccount", (object[])null, (LogLevel)3);
							authWin.DialogResult = false;
							((Window)(object)authWin).Close();
							return;
						}
						UserName = value2 as string;
					}
					authWin.DialogResult = true;
					((Window)(object)authWin).Close();
				}
				else if (e.Uri.Fragment.StartsWith("#error"))
				{
					authWin.DialogResult = false;
					((Window)(object)authWin).Close();
				}
			}
		};
		((Window)(object)authWin).Closing += delegate
		{
			webHost.Close();
		};
		string uriString = "https://api.stocktwits.com/api/2/oauth/authorize?client_id=5cd7b6bdb6575757&redirect_uri=http://www.ninjatrader.com&response_type=token&scope=publish_messages";
		webBrowser.Navigate(new Uri(uriString));
		webHost.Visibility = Visibility.Visible;
		webHost.Topmost = true;
		authWin.ShowDialog();
		if (authWin.DialogResult != true || string.IsNullOrEmpty(OAuthToken) || string.IsNullOrEmpty(UserName))
		{
			return Task.FromResult(0);
		}
		((ShareService)this).IsConfigured = true;
		return Task.FromResult(0);
	}

	public static void HideScriptErrors(WebBrowser wb)
	{
		try
		{
			FieldInfo field = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null)
			{
				return;
			}
			object value = field.GetValue(wb);
			if (value == null)
			{
				wb.Loaded += delegate
				{
					HideScriptErrors(wb);
				};
			}
			else
			{
				value.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, value, new object[1] { true });
			}
		}
		catch
		{
		}
	}

	public override async Task OnShare(string text, string imageFilePath)
	{
		string text2 = text.Normalize();
		string text3 = "https://api.stocktwits.com/api/2/messages/create.json?access_token=" + OAuthToken;
		if (string.IsNullOrEmpty(imageFilePath))
		{
			using (HttpClient client = new HttpClient())
			{
				string s = "body=" + Globals.UrlEncode(text2) + "&sentiment=" + StockTwitsSentiment.ToString().ToLower();
				HttpContent content = new ByteArrayContent(new ASCIIEncoding().GetBytes(s));
				HttpResponseMessage stockTwitsResponse = await client.PostAsync(text3, content);
				string result = await new StreamReader(stockTwitsResponse.Content.ReadAsStreamAsync().Result).ReadToEndAsync();
				if (!stockTwitsResponse.IsSuccessStatusCode)
				{
					LogErrorResponse(result, stockTwitsResponse);
					return;
				}
				((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareStockTwitsSentSuccessfully", new object[1] { ((ShareService)this).Name }, (LogLevel)1);
			}
			return;
		}
		if (!File.Exists(imageFilePath))
		{
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareImageNoLongerExists", new object[1] { imageFilePath }, (LogLevel)3);
			((NinjaScript)this).SetState((State)9);
			return;
		}
		using HttpClient client = new HttpClient();
		using MultipartFormDataContent formData = new MultipartFormDataContent();
		string requestUri = text3 + "&body=" + Globals.UrlEncode(text2) + "&sentiment=" + StockTwitsSentiment.ToString().ToLower();
		HttpContent httpContent = new ByteArrayContent(File.ReadAllBytes(imageFilePath));
		httpContent.Headers.Add("Content-Type", "image/png");
		formData.Add(httpContent, "chart", "photo.png");
		HttpResponseMessage stockTwitsResponse = await client.PostAsync(requestUri, formData);
		string result2 = await new StreamReader(stockTwitsResponse.Content.ReadAsStreamAsync().Result).ReadToEndAsync();
		if (!stockTwitsResponse.IsSuccessStatusCode)
		{
			LogErrorResponse(result2, stockTwitsResponse);
			return;
		}
		((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareStockTwitsSentSuccessfully", new object[1] { ((ShareService)this).Name }, (LogLevel)1);
	}

	public override async Task OnShare(string text, string imageFilePath, object[] args)
	{
		if (args != null && args.Length > 0)
		{
			try
			{
				Sentiment stockTwitsSentiment = (Sentiment)args[0];
				StockTwitsSentiment = stockTwitsSentiment;
			}
			catch (Exception ex)
			{
				((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareArgsException", new object[1] { ex.Message }, (LogLevel)3);
				return;
			}
		}
		await ((ShareService)this).OnShare(text, imageFilePath);
	}

	private void LogErrorResponse(string result, HttpResponseMessage stockTwitsResponse)
	{
		switch (stockTwitsResponse.StatusCode)
		{
		case HttpStatusCode.BadRequest:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareBadRequestError", new object[1] { result }, (LogLevel)3);
			break;
		case HttpStatusCode.Unauthorized:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareNotAuthorized", new object[1] { result }, (LogLevel)3);
			break;
		case HttpStatusCode.Forbidden:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareForbidden", new object[1] { result }, (LogLevel)3);
			break;
		case (HttpStatusCode)429:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareTooManyRequests", new object[1] { result }, (LogLevel)3);
			break;
		case HttpStatusCode.InternalServerError:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareInternalServerError", new object[1] { result }, (LogLevel)3);
			break;
		case HttpStatusCode.ServiceUnavailable:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareBadGatewayError", new object[1] { result }, (LogLevel)3);
			break;
		case HttpStatusCode.GatewayTimeout:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareGatewayTimeoutError", new object[1] { result }, (LogLevel)3);
			break;
		default:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareNonSuccessCode", new object[2] { stockTwitsResponse.StatusCode, result }, (LogLevel)3);
			break;
		}
	}

	private static void OnSizeLocationChanged(FrameworkElement placementTarget, Window webHost)
	{
		if (webHost.Visibility == Visibility.Visible)
		{
			webHost.Show();
		}
		webHost.Owner = Window.GetWindow(placementTarget);
		Point point = placementTarget.PointToScreen(new Point(0.0, 0.0));
		PresentationSource presentationSource = PresentationSource.FromVisual(webHost);
		if (presentationSource != null && presentationSource.CompositionTarget != null)
		{
			Point point2 = presentationSource.CompositionTarget.TransformFromDevice.Transform(point);
			webHost.Left = point2.X;
			webHost.Top = point2.Y;
		}
		webHost.Width = placementTarget.ActualWidth;
		webHost.Height = placementTarget.ActualHeight;
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Invalid comparison between Unknown and I4
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((ShareService)this).CharacterLimit = 1000;
			((ShareService)this).IsConfigured = false;
			((ShareService)this).IsDefault = false;
			((ShareService)this).IsImageAttachmentSupported = true;
			((ShareService)this).Name = Resource.StockTwitsServiceName;
			((ShareService)this).Signature = string.Empty;
			((ShareService)this).UseOAuth = true;
			UserName = string.Empty;
			StockTwitsSentiment = Sentiment.Neutral;
		}
		else if ((int)((NinjaScript)this).State == 3)
		{
			((ShareService)this).CharactersReservedPerMedia = 40;
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			StockTwitsSentiment = Sentiment.Neutral;
		}
	}
}

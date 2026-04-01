using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;

namespace NinjaTrader.NinjaScript.ShareServices;

public class Twitter : ShareService
{
	private object icon;

	private const string oauthConsumerKey = "jq8MT2wxw93NVhcLrdjg";

	public override object Icon => icon ?? (icon = Application.Current.TryFindResource("ShareIconTwitter"));

	[Encrypt]
	[Browsable(false)]
	public string OAuthToken { get; set; }

	[Encrypt]
	[Browsable(false)]
	public string OAuthTokenSecret { get; set; }

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
			if (propertyInfo.Name == "OAuthToken")
			{
				propertyInfo.SetValue(ninjaScript, OAuthToken);
			}
			else if (propertyInfo.Name == "OAuthTokenSecret")
			{
				propertyInfo.SetValue(ninjaScript, OAuthTokenSecret);
			}
			else if (propertyInfo.Name == "UserName")
			{
				propertyInfo.SetValue(ninjaScript, UserName);
			}
		}
	}

	private void LogErrorResponse(string result, HttpResponseMessage twitterResponse)
	{
		switch (twitterResponse.StatusCode)
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
		case HttpStatusCode.BadGateway:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareBadGatewayError", new object[1] { result }, (LogLevel)3);
			break;
		case HttpStatusCode.ServiceUnavailable:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareBadGatewayError", new object[1] { result }, (LogLevel)3);
			break;
		case HttpStatusCode.GatewayTimeout:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareGatewayTimeoutError", new object[1] { result }, (LogLevel)3);
			break;
		default:
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareNonSuccessCode", new object[2] { twitterResponse.StatusCode, result }, (LogLevel)3);
			break;
		}
	}

	public override async Task OnAuthorizeAccount()
	{
		string text = "https://api.twitter.com/oauth/request_token";
		string oauthCallback = "http://127.0.0.1:2943";
		string text2 = Convert.ToInt64((TimeZoneInfo.ConvertTime(Globals.Now, Globals.GeneralOptions.TimeZoneInfo, TimeZoneInfo.Utc) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds, CultureInfo.CurrentCulture).ToString(CultureInfo.CurrentCulture);
		string text3 = Convert.ToBase64String(new ASCIIEncoding().GetBytes(Globals.Now.Ticks.ToString()));
		string oauthSignatureMethod = "HMAC-SHA1";
		string oauthVersion = "1.0";
		OrderedDictionary sigParameters = new OrderedDictionary
		{
			{
				"oauth_callback=",
				Globals.UrlEncode(oauthCallback) + "&"
			},
			{
				"oauth_consumer_key=",
				Globals.UrlEncode("jq8MT2wxw93NVhcLrdjg") + "&"
			},
			{
				"oauth_nonce=",
				Globals.UrlEncode(text3) + "&"
			},
			{
				"oauth_signature_method=",
				Globals.UrlEncode(oauthSignatureMethod) + "&"
			},
			{
				"oauth_timestamp=",
				Globals.UrlEncode(text2) + "&"
			},
			{
				"oauth_version=",
				Globals.UrlEncode(oauthVersion)
			}
		};
		string twitterSignature = Globals.GetTwitterSignature(text, "POST", sigParameters);
		string value = "OAuth oauth_callback=\"" + Globals.UrlEncode(oauthCallback) + "\",oauth_consumer_key=\"" + Globals.UrlEncode("jq8MT2wxw93NVhcLrdjg") + "\",oauth_nonce=\"" + Globals.UrlEncode(text3) + "\",oauth_signature_method=\"" + Globals.UrlEncode(oauthSignatureMethod) + "\",oauth_timestamp=\"" + Globals.UrlEncode(text2) + "\",oauth_version=\"" + Globals.UrlEncode(oauthVersion) + "\",oauth_signature=\"" + Globals.UrlEncode(twitterSignature) + "\"";
		string result;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(text);
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentLength = 0L;
			httpWebRequest.ContentType = "application/x-www-form-urlencoded";
			httpWebRequest.ServicePoint.Expect100Continue = false;
			httpWebRequest.Headers.Add("Authorization", value);
			using HttpWebResponse s = (HttpWebResponse)httpWebRequest.GetResponse();
			using StreamReader reader = new StreamReader(s.GetResponseStream());
			result = await reader.ReadToEndAsync();
		}
		catch (WebException ex)
		{
			using StreamReader reader = new StreamReader(ex.Response.GetResponseStream());
			await reader.ReadToEndAsync();
			((ShareService)this).IsConfigured = false;
			((NinjaScript)this).SetState((State)9);
			return;
		}
		string oauthToken = string.Empty;
		string oauthVerifier = string.Empty;
		if (!string.IsNullOrEmpty(result))
		{
			string[] array = result.Split('&');
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('=');
				if (array2[0] == "oauth_token")
				{
					oauthToken = array2[1];
				}
			}
		}
		Process.Start("https://api.twitter.com/oauth/authorize?oauth_token=" + oauthToken);
		string authString;
		using (HttpListener listener = new HttpListener())
		{
			listener.Prefixes.Add(oauthCallback + "/");
			listener.Start();
			HttpListenerContext obj = await listener.GetContextAsync();
			HttpListenerRequest request = obj.Request;
			authString = request.RawUrl;
			HttpListenerResponse response = obj.Response;
			string twitterAuthHeader = Resource.TwitterAuthHeader;
			string text4 = string.Format(Resource.TwitterAuthText1, Globals.ProductName);
			string text5 = string.Format(Resource.TwitterAuthText2, Globals.ProductName);
			string text6 = string.Format(CultureInfo.InvariantCulture, Resource.AuthDisclosureText1, Globals.Now.Year);
			string authDisclosureText = Resource.AuthDisclosureText2;
			string s2 = $"<!DOCTYPE html>\r\n\t\t\t\t\t\t\t\t<html class=\"no-js\" style=\"height:100%\">\r\n\t\t\t\t\t\t\t\t\t<head>\r\n\t\t\t\t\t\t\t\t\t\t<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">\r\n\t\t\t\t\t\t\t\t\t\t<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\r\n\t\t\t\t\t\t\t\t\t\t<title>NinjaTrader</title>\r\n\t\t\t\t\t\t\t\t\t\t<meta name=\"description\" content=\"\">\r\n\t\t\t\t\t\t\t\t\t\t<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\r\n\t\t\t\t\t\t\t\t\t\t<style type=\"text/css\">\r\n\t\t\t\t\t\t\t\ta,body,div,footer,h1,header,html,img,p,span,sup{{margin:0;padding:0;border:0;font:inherit;font-size:100%;vertical-align:baseline}}html{{line-height:1}}footer,header{{display:block}}*{{-moz-box-sizing:border-box;-webkit-box-sizing:border-box;box-sizing:border-box}}:focus{{outline:0;border:none}}a:active,a:focus{{border:0;outline:0}}body{{background-color:#fff;overflow-x:hidden}}sup{{vertical-align:text-top;font-size:70%}}h1{{font-size:50px;font-size:3.125rem;line-height:50px;line-height:3.125rem;color:#4d4d4d;text-align:center;font-family:ProximaNova-Bold,Helvetica,Arial,sans-serif!important;margin-bottom:16px;margin-bottom:1rem}}@media (min-width:300px) and (max-width:600px){{h1{{font-size:40px;font-size:2.5rem;line-height:40px;line-height:2.5rem;color:#4d4d4d;text-align:center;font-family:ProximaNova-Bold,Helvetica,Arial,sans-serif!important;margin-bottom:16px;margin-bottom:1rem}}}}p{{font-size:24px;font-size:1.5rem;line-height:32px;line-height:2rem;color:#4d4d4d;text-align:center;font-family:ProximaNova-Regular,Helvetica,Arial,sans-serif!important;margin-bottom:30px;margin-bottom:1.875rem}}@media (min-width:300px) and (max-width:600px){{p{{font-size:16px;font-size:1rem;line-height:18px;line-height:1.125rem;color:#4d4d4d;text-align:center;font-family:ProximaNova-Regular,Helvetica,Arial,sans-serif!important;margin-bottom:8px;margin-bottom:.5rem}}}}@media (min-width:300px) and (max-width:600px){{p{{margin-bottom:1rem}}}}a{{font-size:24px;font-size:1.5rem;line-height:32px;line-height:2rem;color:#4d4d4d;text-align:center;font-family:ProximaNova-Regular,Helvetica,Arial,sans-serif!important;text-decoration:none;-webkit-transition:all 350ms ease;-moz-transition:all 350ms ease;-ms-transition:all 350ms ease;-o-transition:all 350ms ease;transition:all 350ms ease}}@media (min-width:300px) and (max-width:600px){{a{{font-size:16px;line-height:18px}}}}.t-left{{text-align:left}}.t-base{{font-size:16px!important;line-height:20px!important}}.c-red{{color:#a41e23}}.b-black{{background-color:#231f20!important}}img.inline{{max-height:100%;max-width:100%;vertical-align:bottom}}@media (min-width:300px) and (max-width:600px){{img.inline{{max-width:90%;max-height:100%;height:auto;vertical-align:bottom;margin:0 5%}}}}.l-row{{width:100%}}.l-block{{max-width:71.3em;padding-left:1em;padding-right:1em;margin-left:auto;margin-right:auto;padding:30px;padding:1.875rem;height:100%}}.l-block:after{{content:\"\";display:table;clear:both}}@media (min-width:300px) and (max-width:600px){{.l-block{{padding:.75rem}}}}.l-four{{width:32.39832%;float:left;margin-right:1.40252%;display:inline}}@media (min-width:300px) and (max-width:600px){{.l-four{{width:100%;float:left;margin-right:1.40252%;display:inline;margin-right:0}}}}.l-twelve{{width:100%;float:left;margin-right:1.40252%;display:inline;margin-right:0}}@media (min-width:300px) and (max-width:600px){{.l-twelve{{width:100%;float:left;margin-right:1.40252%;display:inline;margin-right:0}}}}.l-nmt{{margin-top:0!important}}.l-nmb{{margin-bottom:0!important}}.l-mb1{{margin-bottom:16px!important}}.l-np{{padding:0}}.l-nav{{padding:25px 0;padding:1.5625rem 0}}@media (min-width:300px) and (max-width:600px){{.l-nav{{padding:8px 0;padding:.5rem 0}}}}footer{{width:100%;float:left;margin-right:1.40252%;display:inline;margin-right:0}}footer .l-row{{background:#6e6c6d url(data:image/webp;base64,UklGRi4AAABXRUJQVlA4TCEAAAAvCcAOAA9wF/jPwj8mfv7jAQQCFOH/ZgM6ENH/CUD9mQAA) left top repeat-x}}footer .l-block{{padding:96px 0 16px 0;padding:6rem 0 1rem 0}}@media (min-width:300px) and (max-width:600px){{footer .l-block{{padding:96px 16px 16px 16px;padding:6rem 1rem 1rem 1rem}}}}@media (min-width:601px) and (max-width:1024px){{footer .l-block{{padding:96px 16px 16px 16px;padding:6rem 1rem 1rem 1rem}}}}footer .l-block p{{font-size:16px;font-size:1rem;line-height:20px;line-height:1.25rem;color:#d0d2d3;text-align:left;font-family:ProximaNova-Regular,Helvetica,Arial,sans-serif!important}}@media (min-width:300px) and (max-width:600px){{footer .l-block{{padding:6rem 1rem}}}}#l-nav-block{{-js-display:flex;display:flex;align-items:center;height:30px}}@media (min-width:601px) and (max-width:1024px){{#l-nav-block{{display:block;height:auto}}}}@media (min-width:300px) and (max-width:600px){{#l-nav-block{{display:block;padding-bottom:.5rem;height:auto}}}}body{{-webkit-backface-visibility:hidden}}body{{display:flex;flex-direction:column;min-height:100vh}}footer{{margin:auto auto 0 auto}}\r\n\t\t\t\t\t\t\t\t\t\t</style>\r\n\t\t\t\t\t\t\t\t\t</head>\r\n\t\t\t\t\t\t\t\t\t<body>\r\n\t\t\t\t\t\t\t\t\t\t<header>\r\n\t\t\t\t\t\t\t\t\t\t\t<div class=\"l-row b-black\">\r\n\t\t\t\t\t\t\t\t\t\t\t\t<div id=\"l-nav-block\" class=\"l-block l-np\">\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t&nbsp;\r\n\t\t\t\t\t\t\t\t\t\t\t\t</div>\r\n\t\t\t\t\t\t\t\t\t\t\t</div>\r\n\t\t\t\t\t\t\t\t\t\t\t<div class=\"l-row white\">\r\n\t\t\t\t\t\t\t\t\t\t\t\t<div class=\"l-block l-nav\">\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t<span class=\"l-four\">\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t<img src=\"data:image/webp;base64,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\" alt=\"NinjaTrader\" class=\"inline\">\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t</span>\r\n\t\t\t\t\t\t\t\t\t\t\t\t</div>\r\n\t\t\t\t\t\t\t\t\t\t\t</div>\r\n\t\t\t\t\t\t\t\t\t\t</header>\r\n\t\t\t\t\t\t\t\t\t\t<div class=\"l-row content\">\r\n\t\t\t\t\t\t\t\t\t\t\t<div class=\"l-block\">\r\n\t\t\t\t\t\t\t\t\t\t\t\t<h1 class=\"l-twelve t-left\">{twitterAuthHeader}</h1>\r\n\t\t\t\t\t\t\t\t\t\t\t\t<p class=\"l-twelve l-mb1 t-base t-left\">{text4}</p>\r\n\t\t\t\t\t\t\t\t\t\t\t\t<p class=\"l-twelve l-mb1 t-base t-left\">{text5}</p>\r\n\t\t\t\t\t\t\t\t\t\t\t</div>\r\n\t\t\t\t\t\t\t\t\t\t</div>\r\n\t\t\t\t\t\t\t\t\t\t<footer>\r\n\t\t\t\t\t\t\t\t\t\t\t<div class=\"l-row\">\r\n\t\t\t\t\t\t\t\t\t\t\t\t<div class=\"l-block\" style=\"padding-top:5rem!important;\">\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t<p class=\"l-twelve l-mb1 l-nmt\">{text6}</p>\r\n\t\t\t\t\t\t\t\t\t\t\t\t\t<p id=\"disc\" class=\"l-twelve l-nmb\">{authDisclosureText}</p>\r\n\t\t\t\t\t\t\t\t\t\t\t\t</div>\r\n\t\t\t\t\t\t\t\t\t\t\t</div>\r\n\t\t\t\t\t\t\t\t\t\t</footer>\r\n\t\t\t\t\t\t\t\t\t</body>\r\n\t\t\t\t\t\t\t\t</html>";
			byte[] bytes = Encoding.UTF8.GetBytes(s2);
			response.ContentLength64 = bytes.Length;
			using (Stream output = response.OutputStream)
			{
				await output.WriteAsync(bytes, 0, bytes.Length);
			}
			listener.Close();
		}
		bool flag = false;
		if (!string.IsNullOrEmpty(authString) && authString.StartsWith("/?oauth_token"))
		{
			string[] array = authString.TrimStart('/', '?').Split('&');
			for (int i = 0; i < array.Length; i++)
			{
				string[] array3 = array[i].Split('=');
				if (array3[0] == "oauth_token")
				{
					oauthToken = array3[1];
				}
				else if (array3[0] == "oauth_verifier")
				{
					oauthVerifier = array3[1];
				}
			}
			flag = true;
		}
		if (flag)
		{
			string text7 = "https://api.twitter.com/oauth/access_token";
			text2 = Convert.ToInt64((TimeZoneInfo.ConvertTime(Globals.Now, Globals.GeneralOptions.TimeZoneInfo, TimeZoneInfo.Utc) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds, CultureInfo.CurrentCulture).ToString(CultureInfo.CurrentCulture);
			text3 = Convert.ToBase64String(new ASCIIEncoding().GetBytes(Globals.Now.Ticks.ToString()));
			sigParameters.Clear();
			sigParameters.Add("oauth_consumer_key=", Globals.UrlEncode("jq8MT2wxw93NVhcLrdjg") + "&");
			sigParameters.Add("oauth_nonce=", Globals.UrlEncode(text3) + "&");
			sigParameters.Add("oauth_signature_method=", Globals.UrlEncode(oauthSignatureMethod) + "&");
			sigParameters.Add("oauth_timestamp=", Globals.UrlEncode(text2) + "&");
			sigParameters.Add("oauth_token=", Globals.UrlEncode(oauthToken) + "&");
			sigParameters.Add("oauth_verifier=", Globals.UrlEncode(oauthVerifier) + "&");
			sigParameters.Add("oauth_version=", Globals.UrlEncode(oauthVersion));
			twitterSignature = Globals.GetTwitterSignature(text7, "POST", sigParameters);
			value = "OAuth oauth_consumer_key=\"" + Globals.UrlEncode("jq8MT2wxw93NVhcLrdjg") + "\",oauth_nonce=\"" + Globals.UrlEncode(text3) + "\",oauth_signature_method=\"" + Globals.UrlEncode(oauthSignatureMethod) + "\",oauth_timestamp=\"" + Globals.UrlEncode(text2) + "\",oauth_token=\"" + Globals.UrlEncode(oauthToken) + "\",oauth_verifier=\"" + Globals.UrlEncode(oauthVerifier) + "\",oauth_version=\"" + Globals.UrlEncode(oauthVersion) + "\",oauth_signature=\"" + Globals.UrlEncode(twitterSignature) + "\"";
			try
			{
				HttpWebRequest httpWebRequest2 = (HttpWebRequest)WebRequest.Create(text7 + "?oauth_verifier=" + Globals.UrlEncode(oauthVerifier));
				httpWebRequest2.Method = "POST";
				httpWebRequest2.ContentLength = 0L;
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded";
				httpWebRequest2.ServicePoint.Expect100Continue = false;
				httpWebRequest2.Headers.Add("Authorization", value);
				using HttpWebResponse s = (HttpWebResponse)httpWebRequest2.GetResponse();
				using StreamReader reader = new StreamReader(s.GetResponseStream());
				result = await reader.ReadToEndAsync();
				if (!string.IsNullOrEmpty(result))
				{
					string[] array = result.Split('&');
					for (int i = 0; i < array.Length; i++)
					{
						string[] array4 = array[i].Split('=');
						if (array4[0] == "oauth_token")
						{
							OAuthToken = array4[1];
						}
						else if (array4[0] == "oauth_token_secret")
						{
							OAuthTokenSecret = array4[1];
						}
						else if (array4[0] == "screen_name")
						{
							UserName = array4[1];
						}
					}
				}
			}
			catch (WebException ex2)
			{
				using StreamReader reader = new StreamReader(ex2.Response.GetResponseStream());
				await reader.ReadToEndAsync();
				((ShareService)this).IsConfigured = false;
				((NinjaScript)this).SetState((State)9);
				return;
			}
			((ShareService)this).IsConfigured = !string.IsNullOrEmpty(OAuthToken) && !string.IsNullOrEmpty(OAuthTokenSecret) && !string.IsNullOrEmpty(UserName);
		}
		else
		{
			((ShareService)this).IsConfigured = false;
		}
	}

	public override async Task OnShare(string text, string imageFilePath)
	{
		if ((int)((NinjaScript)this).State != 3)
		{
			throw new InvalidOperationException("Not a valid state to perform this action. State=" + ((object)((NinjaScript)this).State/*cast due to .constrained prefix*/).ToString());
		}
		string text2 = text.Normalize();
		if (string.IsNullOrEmpty(imageFilePath))
		{
			string text3 = Convert.ToInt64((TimeZoneInfo.ConvertTime(Globals.Now, Globals.GeneralOptions.TimeZoneInfo, TimeZoneInfo.Utc) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds, CultureInfo.CurrentCulture).ToString(CultureInfo.CurrentCulture);
			string text4 = Convert.ToBase64String(new ASCIIEncoding().GetBytes(Globals.Now.Ticks.ToString()));
			OrderedDictionary orderedDictionary = new OrderedDictionary
			{
				{
					"oauth_consumer_key=",
					Globals.UrlEncode("jq8MT2wxw93NVhcLrdjg") + "&"
				},
				{
					"oauth_nonce=",
					Globals.UrlEncode(text4) + "&"
				},
				{
					"oauth_signature_method=",
					Globals.UrlEncode("HMAC-SHA1") + "&"
				},
				{
					"oauth_timestamp=",
					Globals.UrlEncode(text3) + "&"
				},
				{
					"oauth_token=",
					Globals.UrlEncode(OAuthToken) + "&"
				},
				{
					"oauth_version=",
					Globals.UrlEncode("1.0") + "&"
				},
				{
					"status=",
					Globals.UrlEncode(text2)
				}
			};
			string twitterSignature = Globals.GetTwitterSignature("https://api.twitter.com/1.1/statuses/update.json", "POST", OAuthTokenSecret, orderedDictionary);
			string parameter = "oauth_consumer_key=\"" + Globals.UrlEncode("jq8MT2wxw93NVhcLrdjg") + "\",oauth_nonce=\"" + Globals.UrlEncode(text4) + "\",oauth_signature_method=\"" + Globals.UrlEncode("HMAC-SHA1") + "\",oauth_timestamp=\"" + Globals.UrlEncode(text3) + "\",oauth_token=\"" + Globals.UrlEncode(OAuthToken) + "\",oauth_version=\"" + Globals.UrlEncode("1.0") + "\",oauth_signature=\"" + Globals.UrlEncode(twitterSignature) + "\"";
			try
			{
				using HttpClient client = new HttpClient();
				string s = "status=" + Globals.UrlEncode(text2);
				HttpContent httpContent = new ByteArrayContent(new ASCIIEncoding().GetBytes(s));
				httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", parameter);
				client.DefaultRequestHeaders.ExpectContinue = false;
				HttpResponseMessage twitterResponse = await client.PostAsync("https://api.twitter.com/1.1/statuses/update.json", httpContent);
				string result = await new StreamReader(twitterResponse.Content.ReadAsStreamAsync().Result).ReadToEndAsync();
				if (!twitterResponse.IsSuccessStatusCode)
				{
					LogErrorResponse(result, twitterResponse);
					return;
				}
				((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareTwitterSentSuccessfully", new object[1] { ((ShareService)this).Name }, (LogLevel)1);
			}
			catch (WebException ex)
			{
				using StreamReader reader = new StreamReader(ex.Response.GetResponseStream());
				await reader.ReadToEndAsync();
				((NinjaScript)this).SetState((State)9);
			}
			return;
		}
		string text5 = "https://api.twitter.com/1.1/statuses/update_with_media.json";
		if (!File.Exists(imageFilePath))
		{
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareImageNoLongerExists", new object[1] { imageFilePath }, (LogLevel)3);
			((NinjaScript)this).SetState((State)9);
			return;
		}
		byte[] content = File.ReadAllBytes(imageFilePath);
		string text6 = Convert.ToInt64((TimeZoneInfo.ConvertTime(Globals.Now, Globals.GeneralOptions.TimeZoneInfo, TimeZoneInfo.Utc) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds, CultureInfo.CurrentCulture).ToString(CultureInfo.CurrentCulture);
		string text7 = Convert.ToBase64String(new ASCIIEncoding().GetBytes(Globals.Now.Ticks.ToString()));
		string text8 = "HMAC-SHA1";
		string text9 = "1.0";
		OrderedDictionary orderedDictionary2 = new OrderedDictionary
		{
			{
				"oauth_consumer_key=",
				Globals.UrlEncode("jq8MT2wxw93NVhcLrdjg") + "&"
			},
			{
				"oauth_nonce=",
				Globals.UrlEncode(text7) + "&"
			},
			{
				"oauth_signature_method=",
				Globals.UrlEncode(text8) + "&"
			},
			{
				"oauth_timestamp=",
				Globals.UrlEncode(text6) + "&"
			},
			{
				"oauth_token=",
				Globals.UrlEncode(OAuthToken) + "&"
			},
			{
				"oauth_version=",
				Globals.UrlEncode(text9)
			}
		};
		string twitterSignature2 = Globals.GetTwitterSignature(text5, "POST", OAuthTokenSecret, orderedDictionary2);
		string parameter2 = "oauth_consumer_key=\"" + Globals.UrlEncode("jq8MT2wxw93NVhcLrdjg") + "\",oauth_nonce=\"" + Globals.UrlEncode(text7) + "\",oauth_signature_method=\"" + Globals.UrlEncode(text8) + "\",oauth_timestamp=\"" + Globals.UrlEncode(text6) + "\",oauth_token=\"" + Globals.UrlEncode(OAuthToken) + "\",oauth_version=\"" + Globals.UrlEncode(text9) + "\",oauth_signature=\"" + Globals.UrlEncode(twitterSignature2) + "\"";
		try
		{
			HttpContent content2 = new StringContent(text2);
			HttpContent content3 = new ByteArrayContent(content);
			using HttpClient client = new HttpClient();
			using MultipartFormDataContent formData = new MultipartFormDataContent
			{
				{ content2, "status" },
				{ content3, "media" }
			};
			client.DefaultRequestHeaders.Connection.Add("Keep-Alive");
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", parameter2);
			client.DefaultRequestHeaders.ExpectContinue = false;
			HttpResponseMessage twitterResponse = await client.PostAsync(text5, formData);
			string result2 = await new StreamReader(twitterResponse.Content.ReadAsStreamAsync().Result).ReadToEndAsync();
			if (!twitterResponse.IsSuccessStatusCode)
			{
				LogErrorResponse(result2, twitterResponse);
				return;
			}
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareTwitterSentSuccessfully", new object[1] { ((ShareService)this).Name }, (LogLevel)1);
		}
		catch (WebException ex2)
		{
			using StreamReader reader = new StreamReader(ex2.Response.GetResponseStream());
			await reader.ReadToEndAsync();
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareWebException", new object[2] { ex2.Status, ex2.Message }, (LogLevel)3);
		}
		catch (Exception ex3)
		{
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareServiceSignature", new object[1] { ex3.Message }, (LogLevel)3);
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((ShareService)this).CharacterLimit = 280;
			((ShareService)this).CharactersReservedPerMedia = 0;
			((ShareService)this).IsConfigured = false;
			((ShareService)this).IsDefault = false;
			((ShareService)this).IsImageAttachmentSupported = true;
			((ShareService)this).Name = Resource.TwitterServiceName;
			((ShareService)this).Signature = Resource.TwitterSignature;
			((ShareService)this).UseOAuth = true;
			UserName = string.Empty;
		}
	}
}

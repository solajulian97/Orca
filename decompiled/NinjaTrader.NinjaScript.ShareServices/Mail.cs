using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;

namespace NinjaTrader.NinjaScript.ShareServices;

[TypeConverter("NinjaTrader.NinjaScript.ShareServices.MailTypeConverter")]
public class Mail : ShareService, IPreconfiguredProvider
{
	private object icon;

	public override object Icon => icon ?? (icon = Application.Current.TryFindResource("ShareIconEmail"));

	[XmlIgnore]
	public List<string> PreconfiguredNames { get; set; }

	public string SelectedPreconfiguredSetting { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "MailServiceMailAddress", GroupName = "ShareServiceParameters", Order = 40)]
	[Required]
	public string FromMailAddress { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "MailServiceSenderDisplayName", GroupName = "ShareServiceParameters", Order = 45)]
	public string SenderDisplayName { get; set; }

	[Browsable(false)]
	public bool IsBodyHtml { get; set; }

	[Encrypt]
	[Browsable(false)]
	public string OAuthToken { get; set; }

	[Encrypt]
	[PasswordPropertyText(true)]
	[Required]
	[Display(ResourceType = typeof(Resource), Name = "ShareServicePassword", GroupName = "ShareServiceParameters", Order = 60)]
	public string Password { get; set; }

	[Range(1, int.MaxValue)]
	[Display(ResourceType = typeof(Resource), Name = "MailServicePort", GroupName = "ShareServiceParameters", Order = 20)]
	[Required]
	public int Port { get; set; }

	[Encrypt]
	[Browsable(false)]
	public string RefreshToken { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "MailServiceServer", GroupName = "ShareServiceParameters", Order = 10)]
	[Required]
	public string Server { get; set; }

	[ShareField]
	[Display(ResourceType = typeof(Resource), Name = "MailSubject", Description = "MailSubjectDescription", Order = 100)]
	[Browsable(false)]
	[XmlIgnore]
	public string Subject { get; set; }

	[ShareField]
	[Display(ResourceType = typeof(Resource), Name = "MailToAddress", Description = "MailToAddressDescription", Order = 0)]
	[Browsable(false)]
	[XmlIgnore]
	public string ToMailAddress { get; set; }

	[ShareField]
	[Display(ResourceType = typeof(Resource), Name = "MailCcAddress", Description = "MailCcAddressDescription", Order = 1)]
	[Browsable(false)]
	[XmlIgnore]
	public string CcMailAddress { get; set; }

	[Encrypt]
	[Display(ResourceType = typeof(Resource), Name = "ShareServiceUserName", GroupName = "ShareServiceParameters", Order = 50)]
	[Required]
	public string UserName { get; set; }

	[Encrypt]
	[Display(ResourceType = typeof(Resource), Name = "MailServiceSSL", GroupName = "ShareServiceParameters", Order = 30)]
	public bool UseSSL { get; set; }

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
			if (propertyInfo.Name == "FromMailAddress")
			{
				propertyInfo.SetValue(ninjaScript, FromMailAddress);
			}
			if (propertyInfo.Name == "SenderDisplayName")
			{
				propertyInfo.SetValue(ninjaScript, SenderDisplayName);
			}
			else if (propertyInfo.Name == "IsBodyHtml")
			{
				propertyInfo.SetValue(ninjaScript, IsBodyHtml);
			}
			else if (propertyInfo.Name == "Password")
			{
				propertyInfo.SetValue(ninjaScript, Password);
			}
			else if (propertyInfo.Name == "Port")
			{
				propertyInfo.SetValue(ninjaScript, Port);
			}
			else if (propertyInfo.Name == "Server")
			{
				propertyInfo.SetValue(ninjaScript, Server);
			}
			else if (propertyInfo.Name == "Subject")
			{
				propertyInfo.SetValue(ninjaScript, Subject);
			}
			else if (propertyInfo.Name == "ToMailAddress")
			{
				propertyInfo.SetValue(ninjaScript, ToMailAddress);
			}
			else if (propertyInfo.Name == "CcMailAddress")
			{
				propertyInfo.SetValue(ninjaScript, CcMailAddress);
			}
			else if (propertyInfo.Name == "UserName")
			{
				propertyInfo.SetValue(ninjaScript, UserName);
			}
			else if (propertyInfo.Name == "UseSSL")
			{
				propertyInfo.SetValue(ninjaScript, UseSSL);
			}
			else if (propertyInfo.Name == "OAuthToken")
			{
				propertyInfo.SetValue(ninjaScript, OAuthToken);
			}
			else if (propertyInfo.Name == "RefreshToken")
			{
				propertyInfo.SetValue(ninjaScript, RefreshToken);
			}
		}
	}

	public override async Task OnAuthorizeAccount()
	{
		Tuple<string, string, string> tuple = null;
		if (string.Equals(SelectedPreconfiguredSetting, Resource.ShareMailPreconfiguredGmail))
		{
			tuple = await GoogleOAuthHelper.Authorize((string)null);
		}
		else if (string.Equals(SelectedPreconfiguredSetting, Resource.ShareMailPreconfiguredOutlook))
		{
			tuple = await MicrosoftOAuthHelper.SignInAndGetAuthCredentialsAsync(true);
		}
		if (tuple == null || string.IsNullOrWhiteSpace(tuple.Item1) || string.IsNullOrWhiteSpace(tuple.Item2) || string.IsNullOrWhiteSpace(tuple.Item3))
		{
			await Task.FromResult(0);
			return;
		}
		FromMailAddress = tuple.Item3;
		OAuthToken = tuple.Item1;
		RefreshToken = tuple.Item2;
		((ShareService)this).IsConfigured = true;
		await Task.FromResult(0);
	}

	public override async Task OnShare(string text, string imageFilePath)
	{
		if (((ShareService)this).IsConfigured && string.Equals(SelectedPreconfiguredSetting, Resource.ShareMailPreconfiguredGmail))
		{
			if (!(await Globals.SendGMail(FromMailAddress, SenderDisplayName, ToMailAddress.Split(',', ';'), CcMailAddress.Split(',', ';'), text, Subject, imageFilePath, OAuthToken)))
			{
				OAuthToken = (await GoogleOAuthHelper.Authorize(RefreshToken)).Item1;
				if (!(await Globals.SendGMail(FromMailAddress, SenderDisplayName, ToMailAddress.Split(',', ';'), CcMailAddress.Split(',', ';'), text, Subject, imageFilePath, OAuthToken)))
				{
					await ((ShareService)this).OnAuthorizeAccount();
					if (await Globals.SendGMail(FromMailAddress, SenderDisplayName, ToMailAddress.Split(',', ';'), CcMailAddress.Split(',', ';'), text, Subject, imageFilePath, OAuthToken))
					{
						((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareMailSentSuccessfully", new object[1] { ((ShareService)this).Name }, (LogLevel)1);
					}
				}
			}
			else
			{
				((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareMailSentSuccessfully", new object[1] { ((ShareService)this).Name }, (LogLevel)1);
			}
			return;
		}
		if (((ShareService)this).IsConfigured && string.Equals(SelectedPreconfiguredSetting, Resource.ShareMailPreconfiguredOutlook))
		{
			if (!(await Globals.SendOutlookMailAsync(ToMailAddress.Split(',', ';'), CcMailAddress.Split(',', ';'), text, Subject, imageFilePath, OAuthToken)))
			{
				OAuthToken = (await MicrosoftOAuthHelper.SignInAndGetAuthCredentialsAsync(false)).Item1;
				if (await Globals.SendOutlookMailAsync(ToMailAddress.Split(',', ';'), CcMailAddress.Split(',', ';'), text, Subject, imageFilePath, OAuthToken))
				{
					((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareMailSentSuccessfully", new object[1] { ((ShareService)this).Name }, (LogLevel)1);
				}
			}
			else
			{
				((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareMailSentSuccessfully", new object[1] { ((ShareService)this).Name }, (LogLevel)1);
			}
			return;
		}
		string text2 = NinjaScript.Decrypt(Password);
		string text3 = NinjaScript.Decrypt(UserName);
		if (Server.Trim().Length == 0 || Port == 0 || text3.Trim().Length == 0 || text2.Trim().Length == 0)
		{
			Log.Process(typeof(Resource), "CoreGlobalsSendMail", (object[])null, (LogLevel)3, (LogCategories)4);
			return;
		}
		try
		{
			if (await Globals.SendMailToServer(FromMailAddress, ((NinjaScript)this).DisplayName, ToMailAddress.Split(',', ';'), CcMailAddress.Split(',', ';'), text, Subject, imageFilePath, Server, Port, text3, text2, IsBodyHtml))
			{
				((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareMailSentSuccessfully", new object[1] { ((ShareService)this).Name }, (LogLevel)1);
			}
		}
		catch (Exception ex)
		{
			Exception innerException = ex.InnerException;
			string text4 = ex.Message;
			while (innerException != null)
			{
				text4 = text4 + " " + innerException.Message;
				innerException = innerException.InnerException;
			}
			NinjaScript.Log(string.Format(Resource.ShareMailException, text4), (LogLevel)3);
		}
		finally
		{
			Subject = string.Empty;
			ToMailAddress = string.Empty;
		}
	}

	public override async Task OnShare(string text, string imageFilePath, object[] args)
	{
		if (args != null && args.Length > 1)
		{
			try
			{
				ToMailAddress = args[0].ToString();
				Subject = args[1].ToString();
			}
			catch (Exception ex)
			{
				((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareArgsException", new object[1] { ex.Message }, (LogLevel)3);
				return;
			}
		}
		await ((ShareService)this).OnShare(text, imageFilePath);
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			((ShareService)this).CharacterLimit = int.MaxValue;
			((ShareService)this).CharactersReservedPerMedia = int.MaxValue;
			((ShareService)this).IsConfigured = true;
			((ShareService)this).IsDefault = false;
			((ShareService)this).IsImageAttachmentSupported = true;
			((ShareService)this).Name = Resource.MailServiceName;
			((ShareService)this).Signature = Resource.EmailSignature;
			((ShareService)this).UseOAuth = false;
			CcMailAddress = string.Empty;
			FromMailAddress = string.Empty;
			IsBodyHtml = false;
			Port = 25;
			SenderDisplayName = string.Empty;
			Server = string.Empty;
			Subject = string.Empty;
			ToMailAddress = string.Empty;
			UserName = string.Empty;
			PreconfiguredNames = new List<string>
			{
				Resource.ShareMailPreconfiguredManual,
				Resource.ShareMailPreconfiguredAol,
				Resource.ShareMailPreconfiguredComcast,
				Resource.ShareMailPreconfiguredGmail,
				Resource.ShareMailPreconfiguredICloud,
				Resource.ShareMailPreconfiguredOutlook,
				Resource.ShareMailPreconfiguredYahoo
			};
			SelectedPreconfiguredSetting = PreconfiguredNames[0];
		}
		else if ((int)((NinjaScript)this).State == 8)
		{
			CcMailAddress = string.Empty;
			Subject = string.Empty;
			ToMailAddress = string.Empty;
		}
	}

	public void ApplyPreconfiguredSettings(string name)
	{
		if (name == Resource.ShareMailPreconfiguredAol)
		{
			Port = 587;
			Server = "smtp.aol.com";
			UseSSL = true;
			((ShareService)this).UseOAuth = false;
			((ShareService)this).IsConfigured = true;
		}
		else if (name == Resource.ShareMailPreconfiguredComcast)
		{
			Port = 587;
			Server = "smtp.comcast.net";
			UseSSL = true;
			((ShareService)this).UseOAuth = false;
			((ShareService)this).IsConfigured = true;
		}
		else if (name == Resource.ShareMailPreconfiguredGmail)
		{
			((ShareService)this).UseOAuth = true;
			((ShareService)this).IsConfigured = false;
		}
		else if (name == Resource.ShareMailPreconfiguredICloud)
		{
			Port = 587;
			Server = "smtp.mail.me.com";
			UseSSL = true;
			((ShareService)this).UseOAuth = false;
			((ShareService)this).IsConfigured = true;
		}
		else if (name == Resource.ShareMailPreconfiguredOutlook)
		{
			((ShareService)this).UseOAuth = true;
			((ShareService)this).IsConfigured = false;
		}
		else if (name == Resource.ShareMailPreconfiguredYahoo)
		{
			Port = 587;
			Server = "smtp.mail.yahoo.com";
			UseSSL = true;
			((ShareService)this).UseOAuth = false;
			((ShareService)this).IsConfigured = true;
		}
		else
		{
			((ShareService)this).UseOAuth = false;
			((ShareService)this).IsConfigured = true;
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.ShareServices;

public class TextMessage : ShareService, IPreconfiguredProvider
{
	private object icon;

	public override object Icon => icon ?? (icon = Application.Current.TryFindResource("ShareIconSMS"));

	[XmlIgnore]
	public List<string> PreconfiguredNames { get; set; }

	public string SelectedPreconfiguredSetting { get; set; }

	[Required(ErrorMessageResourceName = "ShareTextMessageEmailRequired", ErrorMessageResourceType = typeof(Resource))]
	[TypeConverter(typeof(TextMessageEmailConverter))]
	[Display(ResourceType = typeof(Resource), Name = "ShareTextMessageEmail", GroupName = "ShareServiceParameters", Order = 5)]
	public string Email { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "ShareTextMessageMmsAddress", GroupName = "ShareServiceParameters", Order = 30)]
	public string MmsAddress { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "ShareTextMessagePhoneNumber", GroupName = "ShareServiceParameters", Order = 10)]
	[Range(0.0, 999999999999999.0)]
	public long PhoneNumber { get; set; }

	[Display(ResourceType = typeof(Resource), Name = "ShareTextMessageSmsAddress", GroupName = "ShareServiceParameters", Order = 20)]
	public string SmsAddress { get; set; }

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
			if (propertyInfo.Name == "Email")
			{
				propertyInfo.SetValue(ninjaScript, Email);
			}
			else if (propertyInfo.Name == "MmsAddress")
			{
				propertyInfo.SetValue(ninjaScript, MmsAddress);
			}
			else if (propertyInfo.Name == "PhoneNumber")
			{
				propertyInfo.SetValue(ninjaScript, PhoneNumber);
			}
			else if (propertyInfo.Name == "SmsAddress")
			{
				propertyInfo.SetValue(ninjaScript, SmsAddress);
			}
		}
	}

	public override async Task OnShare(string text, string imageFilePath)
	{
		ShareService val;
		lock (Globals.GeneralOptions.ShareServices)
		{
			val = Globals.GeneralOptions.ShareServices.FirstOrDefault((ShareService s) => ((object)s).GetType().Name == "Mail" && s.Name == Email);
		}
		if (val == null)
		{
			((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareTextMessageUnknownEmailService", new object[1] { Email }, (LogLevel)3);
			return;
		}
		string text2 = string.Empty;
		if (!string.IsNullOrEmpty(SmsAddress) && string.IsNullOrEmpty(MmsAddress))
		{
			text2 = PhoneNumber.ToString(CultureInfo.InvariantCulture) + SmsAddress;
		}
		else if (string.IsNullOrEmpty(SmsAddress) && !string.IsNullOrEmpty(MmsAddress))
		{
			text2 = PhoneNumber.ToString(CultureInfo.InvariantCulture) + MmsAddress;
		}
		else if (!string.IsNullOrEmpty(SmsAddress) && !string.IsNullOrEmpty(MmsAddress))
		{
			text2 = ((!string.IsNullOrEmpty(imageFilePath)) ? (PhoneNumber.ToString(CultureInfo.InvariantCulture) + MmsAddress) : (PhoneNumber.ToString(CultureInfo.InvariantCulture) + SmsAddress));
		}
		object obj = ((NinjaScript)val).Clone();
		ShareService liveClone = (ShareService)((obj is ShareService) ? obj : null);
		try
		{
			if (liveClone != null)
			{
				((NinjaScript)liveClone).SetState((State)3);
				await liveClone.OnShare(text, imageFilePath, new object[2]
				{
					text2,
					string.Empty
				});
				((NinjaScript)this).LogAndPrint(typeof(Resource), "ShareTextMessageSentSuccessfully", new object[1] { ((ShareService)this).Name }, (LogLevel)1);
			}
		}
		catch (Exception ex)
		{
			TextMessage textMessage = this;
			Type typeFromHandle = typeof(Resource);
			object[] array = new object[2];
			array[0] = ((liveClone != null) ? liveClone.Name : null);
			array[1] = ex.Message;
			((NinjaScript)textMessage).LogAndPrint(typeFromHandle, "ShareTextMessageErrorOnShare", array, (LogLevel)3);
		}
		finally
		{
			if (liveClone != null)
			{
				((NinjaScript)liveClone).SetState((State)9);
			}
		}
	}

	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State != 1)
		{
			return;
		}
		((ShareService)this).CharacterLimit = int.MaxValue;
		((ShareService)this).CharactersReservedPerMedia = int.MaxValue;
		((ShareService)this).IsConfigured = true;
		((ShareService)this).IsDefault = false;
		((ShareService)this).IsImageAttachmentSupported = true;
		((ShareService)this).Name = Resource.ShareTextMessageName;
		((ShareService)this).Signature = string.Empty;
		((ShareService)this).UseOAuth = false;
		ShareService val;
		lock (Globals.GeneralOptions.ShareServices)
		{
			val = Globals.GeneralOptions.ShareServices.FirstOrDefault((ShareService s) => ((object)s).GetType().Name == "Mail" && s.IsDefault);
		}
		Email = ((val != null) ? val.Name : string.Empty);
		MmsAddress = string.Empty;
		PhoneNumber = 8005551234L;
		SmsAddress = string.Empty;
		PreconfiguredNames = new List<string>
		{
			Resource.ShareTextMessagePreconfiguredManual,
			Resource.ShareTextMessagePreconfiguredVerizon,
			Resource.ShareTextMessagePreconfiguredTMobile,
			Resource.ShareTextMessagePreconfiguredSprint
		};
		SelectedPreconfiguredSetting = PreconfiguredNames[0];
	}

	public void ApplyPreconfiguredSettings(string name)
	{
		if (name == Resource.ShareTextMessagePreconfiguredVerizon)
		{
			SmsAddress = "@vtext.com";
			MmsAddress = "@vzwpix.com";
		}
		else if (name == Resource.ShareTextMessagePreconfiguredTMobile)
		{
			SmsAddress = "@tmomail.net";
			MmsAddress = "@tmomail.net";
		}
		else if (name == Resource.ShareTextMessagePreconfiguredSprint)
		{
			SmsAddress = "@messaging.sprintpcs.com";
			MmsAddress = "@pm.sprint.com";
		}
	}
}

using NinjaTrader.Custom;

namespace NinjaTrader.NinjaScript.ImportTypes;

public class TextImportTypeBeginningOfBar : TextImportType
{
	protected override void OnStateChange()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((NinjaScript)this).State == 1)
		{
			base.EndOfBarTimestamps = false;
			((NinjaScript)this).Name = Resource.ImportTypeNinjaTraderBeginningOfBar;
		}
		else
		{
			base.OnStateChange();
		}
	}
}

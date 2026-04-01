using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom;
using NinjaTrader.NinjaScript.Optimizers;

namespace NinjaTrader.NinjaScript.StrategyGenerator;

internal class IndicatorExpression : IExpression, ICloneable
{
	private int r0 = -1;

	private int r1 = -1;

	private int r2 = -1;

	private int r3 = -1;

	private int r4 = -1;

	/// <summary>
	/// Between 0..1
	/// </summary>
	public double CompareFactor { get; set; }

	private double CompareValue => MinCompare + (MaxCompare - MinCompare) * CompareFactor;

	public Condition Condition { get; set; }

	public IndicatorBase Left { get; set; }

	public int LeftBarsAgo { get; set; }

	public double MaxCompare { get; set; } = double.NaN;

	public double MinCompare { get; set; } = double.NaN;

	public IndicatorBase Right { get; set; }

	public int RightBarsAgo { get; set; }

	public bool UsePriceToCompare { get; set; }

	public object Clone()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		IndicatorBase val = (IndicatorBase)((NinjaScript)Left).Clone();
		IndicatorBase val2 = (IndicatorBase)((NinjaScript)Right).Clone();
		try
		{
			((NinjaScript)val).SetState((State)2);
		}
		catch (Exception ex)
		{
			Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
			{
				((NinjaScriptBase)Left).Name,
				(ex.InnerException != null) ? ex.InnerException.ToString() : ex.ToString()
			}, (LogLevel)3, (LogCategories)4);
			((NinjaScript)val).SetState((State)9);
		}
		try
		{
			((NinjaScript)val2).SetState((State)2);
		}
		catch (Exception ex2)
		{
			Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
			{
				((NinjaScriptBase)Right).Name,
				(ex2.InnerException != null) ? ex2.InnerException.ToString() : ex2.ToString()
			}, (LogLevel)3, (LogCategories)4);
			((NinjaScript)val2).SetState((State)9);
		}
		((NinjaScriptBase)val).SelectedValueSeries = ((NinjaScriptBase)Left).SelectedValueSeries;
		((NinjaScriptBase)val2).SelectedValueSeries = ((NinjaScriptBase)Right).SelectedValueSeries;
		return new IndicatorExpression
		{
			CompareFactor = CompareFactor,
			Condition = Condition,
			Left = val,
			LeftBarsAgo = LeftBarsAgo,
			MaxCompare = MaxCompare,
			MinCompare = MinCompare,
			r0 = r0,
			r1 = r1,
			r2 = r2,
			r3 = r3,
			r4 = r4,
			Right = val2,
			RightBarsAgo = RightBarsAgo,
			UsePriceToCompare = UsePriceToCompare
		};
	}

	public bool Evaluate(GeneratedStrategyLogic logic, StrategyBase strategy)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected I4, but got Unknown
		try
		{
			Condition condition = Condition;
			switch ((int)condition)
			{
			case 0:
			{
				int num = -1;
				int val = 1;
				if (((NinjaScriptBase)Left).IsOverlay)
				{
					ISeries<double> obj;
					if (!UsePriceToCompare)
					{
						ISeries<double> val2 = (ISeries<double>)(object)((NinjaScriptBase)Right).Values[((NinjaScriptBase)Right).SelectedValueSeries];
						obj = val2;
					}
					else
					{
						obj = ((NinjaScriptBase)Left).Close;
					}
					ISeries<double> val3 = obj;
					int num2 = Math.Min(val3.Count - 1, Math.Min(val, ((NinjaScriptBase)Left).Count - 1));
					for (int i = 0; i <= num2; i++)
					{
						if (num < 0 && ((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][i] > val3[i])
						{
							num = i;
						}
						else if (num >= 0 && ((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][i] <= val3[i])
						{
							return true;
						}
					}
				}
				else
				{
					int num3 = Math.Min(val, ((NinjaScriptBase)Left).Count - 1);
					for (int j = 0; j <= num3; j++)
					{
						if (num < 0 && ((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][j] > CompareValue)
						{
							num = j;
						}
						else if (num >= 0 && ((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][j] <= CompareValue)
						{
							return true;
						}
					}
				}
				return false;
			}
			case 1:
			{
				int num4 = -1;
				int val4 = 1;
				if (((NinjaScriptBase)Left).IsOverlay)
				{
					ISeries<double> obj2;
					if (!UsePriceToCompare)
					{
						ISeries<double> val2 = (ISeries<double>)(object)((NinjaScriptBase)Right).Values[((NinjaScriptBase)Right).SelectedValueSeries];
						obj2 = val2;
					}
					else
					{
						obj2 = ((NinjaScriptBase)Left).Close;
					}
					ISeries<double> val5 = obj2;
					int num5 = Math.Min(val5.Count - 1, Math.Min(val4, ((NinjaScriptBase)Left).Count - 1));
					for (int k = 0; k <= num5; k++)
					{
						if (num4 < 0 && ((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][k] < val5[k])
						{
							num4 = k;
						}
						else if (num4 >= 0 && ((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][k] >= val5[k])
						{
							return true;
						}
					}
				}
				else
				{
					int num6 = Math.Min(val4, ((NinjaScriptBase)Left).Count - 1);
					for (int l = 0; l <= num6; l++)
					{
						if (num4 < 0 && ((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][l] < CompareValue)
						{
							num4 = l;
						}
						else if (num4 >= 0 && ((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][l] >= CompareValue)
						{
							return true;
						}
					}
				}
				return false;
			}
			case 2:
				return MathExtentions.ApproxCompare(((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][LeftBarsAgo], (!((NinjaScriptBase)Left).IsOverlay) ? CompareValue : (UsePriceToCompare ? ((NinjaScriptBase)Left).Close[RightBarsAgo] : ((NinjaScriptBase)Right).Values[((NinjaScriptBase)Right).SelectedValueSeries][RightBarsAgo])) == 0;
			case 3:
				return MathExtentions.ApproxCompare(((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][LeftBarsAgo], (!((NinjaScriptBase)Left).IsOverlay) ? CompareValue : (UsePriceToCompare ? ((NinjaScriptBase)Left).Close[RightBarsAgo] : ((NinjaScriptBase)Right).Values[((NinjaScriptBase)Right).SelectedValueSeries][RightBarsAgo])) > 0;
			case 4:
				return MathExtentions.ApproxCompare(((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][LeftBarsAgo], (!((NinjaScriptBase)Left).IsOverlay) ? CompareValue : (UsePriceToCompare ? ((NinjaScriptBase)Left).Close[RightBarsAgo] : ((NinjaScriptBase)Right).Values[((NinjaScriptBase)Right).SelectedValueSeries][RightBarsAgo])) >= 0;
			case 5:
				return MathExtentions.ApproxCompare(((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][LeftBarsAgo], (!((NinjaScriptBase)Left).IsOverlay) ? CompareValue : (UsePriceToCompare ? ((NinjaScriptBase)Left).Close[RightBarsAgo] : ((NinjaScriptBase)Right).Values[((NinjaScriptBase)Right).SelectedValueSeries][RightBarsAgo])) < 0;
			case 6:
				return MathExtentions.ApproxCompare(((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][LeftBarsAgo], (!((NinjaScriptBase)Left).IsOverlay) ? CompareValue : (UsePriceToCompare ? ((NinjaScriptBase)Left).Close[RightBarsAgo] : ((NinjaScriptBase)Right).Values[((NinjaScriptBase)Right).SelectedValueSeries][RightBarsAgo])) <= 0;
			case 7:
				return MathExtentions.ApproxCompare(((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries][LeftBarsAgo], (!((NinjaScriptBase)Left).IsOverlay) ? CompareValue : (UsePriceToCompare ? ((NinjaScriptBase)Left).Close[RightBarsAgo] : ((NinjaScriptBase)Right).Values[((NinjaScriptBase)Right).SelectedValueSeries][RightBarsAgo])) != 0;
			default:
				return false;
			}
		}
		catch
		{
			StringBuilder stringBuilder = new StringBuilder();
			Print(stringBuilder, 1);
			Log.Process(typeof(Resource), "NinjaScriptStrategyGeneratorIndicatorException", new object[2]
			{
				Environment.NewLine,
				stringBuilder.ToString()
			}, (LogLevel)3, (LogCategories)4);
			throw;
		}
	}

	public static IExpression FromXml(XElement element)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		IndicatorExpression indicatorExpression = new IndicatorExpression
		{
			CompareFactor = double.Parse(element.Element("CompareFactor").Value, CultureInfo.InvariantCulture),
			Condition = (Condition)Enum.Parse(typeof(Condition), element.Element("Condition").Value),
			LeftBarsAgo = int.Parse(element.Element("LeftBarsAgo").Value),
			MaxCompare = double.Parse(element.Element("MaxCompare").Value, CultureInfo.InvariantCulture),
			MinCompare = double.Parse(element.Element("MinCompare").Value, CultureInfo.InvariantCulture),
			RightBarsAgo = int.Parse(element.Element("RightBarsAgo").Value),
			UsePriceToCompare = bool.Parse(element.Element("UsePriceToCompare").Value)
		};
		if (element.Element("LeftType") != null)
		{
			object obj = new XmlSerializer(Globals.AssemblyRegistry.GetType(element.Element("LeftType").Value)).Deserialize(element.Element("Left").FirstNode.CreateReader());
			indicatorExpression.Left = (IndicatorBase)((obj is IndicatorBase) ? obj : null);
		}
		if (element.Element("RightType") != null)
		{
			object obj2 = new XmlSerializer(Globals.AssemblyRegistry.GetType(element.Element("RightType").Value)).Deserialize(element.Element("Right").FirstNode.CreateReader());
			indicatorExpression.Right = (IndicatorBase)((obj2 is IndicatorBase) ? obj2 : null);
		}
		return indicatorExpression;
	}

	public List<IExpression> GetExpressions()
	{
		return new List<IExpression>(new IExpression[1] { this });
	}

	public void Initialize(StrategyBase strategy)
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		((NinjaScriptBase)Left).Parent = (NinjaScriptBase)(object)strategy;
		((NinjaScriptBase)Right).Parent = (NinjaScriptBase)(object)strategy;
		((NinjaScriptBase)Left).SetInput(((NinjaScriptBase)strategy).Input);
		((NinjaScriptBase)Right).SetInput(((NinjaScriptBase)strategy).Input);
		lock (((NinjaScriptBase)strategy).NinjaScripts)
		{
			((NinjaScriptBase)strategy).NinjaScripts.Add((NinjaScriptBase)(object)Left);
			((NinjaScriptBase)strategy).NinjaScripts.Add((NinjaScriptBase)(object)Right);
		}
		try
		{
			((NinjaScript)Left).SetState(((NinjaScript)strategy).State);
		}
		catch (Exception ex)
		{
			Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
			{
				((NinjaScriptBase)Left).Name,
				(ex.InnerException != null) ? ex.InnerException.ToString() : ex.ToString()
			}, (LogLevel)3, (LogCategories)4);
			((NinjaScript)Left).SetState((State)9);
			return;
		}
		try
		{
			((NinjaScript)Right).SetState(((NinjaScript)strategy).State);
		}
		catch (Exception ex2)
		{
			Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
			{
				((NinjaScriptBase)Right).Name,
				(ex2.InnerException != null) ? ex2.InnerException.ToString() : ex2.ToString()
			}, (LogLevel)3, (LogCategories)4);
			((NinjaScript)Right).SetState((State)9);
			return;
		}
		if (!((NinjaScriptBase)Left).IsOverlay && double.IsNaN(MaxCompare))
		{
			((NinjaScriptBase)Left).Update(((NinjaScriptBase)Left).BarsArray[0].Count - 1, 0);
			MaxCompare = double.MinValue;
			MinCompare = double.MaxValue;
			for (int i = 0; i < ((NinjaScriptBase)Left).BarsArray[0].Count; i++)
			{
				MaxCompare = Math.Max(MaxCompare, ((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries].GetValueAt(i));
				MinCompare = Math.Min(MinCompare, ((NinjaScriptBase)Left).Values[((NinjaScriptBase)Left).SelectedValueSeries].GetValueAt(i));
			}
			MaxCompare = RoundToNearestDecimal(MaxCompare, up: true);
			MinCompare = RoundToNearestDecimal(MinCompare, up: false);
		}
	}

	public IExpression NewMutation(GeneratedStrategyLogic logic, Random random, IExpression toMutate)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Expected O, but got Unknown
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Invalid comparison between Unknown and I4
		if (!logic.TryLinearMutation)
		{
			r0 = random.Next(50);
			r2 = random.Next(2);
			r3 = random.Next(GeneratedStrategyLogic.NumConditions);
		}
		IndicatorExpression obj = new IndicatorExpression
		{
			CompareFactor = CompareFactor
		};
		int num2;
		if (toMutate == this)
		{
			int num = r0;
			if (num >= 0 && num < 2)
			{
				num2 = r3;
				goto IL_0067;
			}
		}
		num2 = (int)Condition;
		goto IL_0067;
		IL_0099:
		object left;
		obj.Left = (IndicatorBase)left;
		obj.LeftBarsAgo = LeftBarsAgo;
		obj.MaxCompare = double.NaN;
		obj.MinCompare = double.NaN;
		object right;
		if (toMutate == this)
		{
			int num = r0;
			if (num >= 4 && num < 6)
			{
				right = logic.RandomIndicator(random);
				goto IL_00f5;
			}
		}
		right = (object)(IndicatorBase)((NinjaScript)Right).Clone();
		goto IL_00f5;
		IL_00f5:
		obj.Right = (IndicatorBase)right;
		obj.RightBarsAgo = RightBarsAgo;
		IndicatorExpression indicatorExpression = obj;
		if (NinjaTrader.NinjaScript.Optimizers.StrategyGenerator.AvailableIndicators.TryGetValue(((object)indicatorExpression.Left).GetType(), out var value) && value != null)
		{
			indicatorExpression.MinCompare = value.Item1;
			indicatorExpression.MaxCompare = value.Item2;
		}
		if (((NinjaScriptBase)indicatorExpression.Left).IsOverlay)
		{
			while (!((NinjaScriptBase)indicatorExpression.Right).IsOverlay)
			{
				try
				{
					((NinjaScript)indicatorExpression.Right).SetState((State)9);
				}
				catch
				{
				}
				indicatorExpression.Right = logic.RandomIndicator(random);
			}
		}
		else
		{
			while (((NinjaScriptBase)indicatorExpression.Right).IsOverlay)
			{
				try
				{
					((NinjaScript)indicatorExpression.Right).SetState((State)9);
				}
				catch
				{
				}
				indicatorExpression.Right = logic.RandomIndicator(random);
			}
		}
		try
		{
			((NinjaScript)indicatorExpression.Left).SetState((State)2);
		}
		catch (Exception ex)
		{
			Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
			{
				((NinjaScriptBase)indicatorExpression.Left).Name,
				(ex.InnerException != null) ? ex.InnerException.ToString() : ex.ToString()
			}, (LogLevel)3, (LogCategories)4);
			((NinjaScript)indicatorExpression.Left).SetState((State)9);
			return indicatorExpression;
		}
		try
		{
			((NinjaScript)indicatorExpression.Right).SetState((State)2);
		}
		catch (Exception ex2)
		{
			Log.Process(typeof(Resource), "CbiUnableToCreateInstance2", new object[2]
			{
				((NinjaScriptBase)indicatorExpression.Right).Name,
				(ex2.InnerException != null) ? ex2.InnerException.ToString() : ex2.ToString()
			}, (LogLevel)3, (LogCategories)4);
			((NinjaScript)indicatorExpression.Left).SetState((State)9);
			((NinjaScript)indicatorExpression.Right).SetState((State)9);
			return indicatorExpression;
		}
		if (toMutate == this)
		{
			int num = r0;
			if (num >= 2 && num < 4)
			{
				indicatorExpression.MaxCompare = double.NaN;
				indicatorExpression.MinCompare = double.NaN;
			}
			else
			{
				num = r0;
				if (num >= 4 && num < 6)
				{
					if (!double.IsNaN(indicatorExpression.MaxCompare))
					{
						indicatorExpression.CompareFactor = Math.Min(1.0, Math.Max(0.0, indicatorExpression.CompareFactor + ((r2 == 0) ? 0.1 : (-0.1))));
					}
					indicatorExpression.UsePriceToCompare = r2 == 0;
				}
				else
				{
					num = r0;
					if (num >= 10 && num < 30)
					{
						IndicatorBase val = ((r0 < 20) ? indicatorExpression.Left : indicatorExpression.Right);
						List<PropertyInfo> list = (from p in ((object)val).GetType().GetProperties()
							where Attribute.GetCustomAttribute(p, typeof(RangeAttribute), inherit: false) != null && Attribute.GetCustomAttribute(p, typeof(NinjaScriptPropertyAttribute), inherit: false) != null
							select p).ToList();
						if (list.Count == 0 || (int)((NinjaScript)val).State == 9)
						{
							return indicatorExpression;
						}
						if (!logic.TryLinearMutation)
						{
							r1 = random.Next(list.Count);
						}
						logic.TryLinearMutation = true;
						PropertyInfo propertyInfo = list[r1];
						double num3;
						try
						{
							num3 = (double)Convert.ChangeType(propertyInfo.GetValue(val, null), typeof(double));
						}
						catch (Exception ex3)
						{
							((NinjaScript)val).LogAndPrint(typeof(Resource), "DataGetPropertyValueException", new object[3]
							{
								propertyInfo.Name,
								((NinjaScriptBase)val).Name,
								NinjaScriptBase.GetExceptionMessage(ex3)
							}, (LogLevel)3);
							((NinjaScript)val).SetState((State)9);
							return indicatorExpression;
						}
						RangeAttribute obj4 = Attribute.GetCustomAttribute(propertyInfo, typeof(RangeAttribute), inherit: false) as RangeAttribute;
						double num4 = (double)Convert.ChangeType(obj4.Maximum, typeof(double));
						double val2 = (double)Convert.ChangeType(obj4.Minimum, typeof(double));
						if (propertyInfo.PropertyType == typeof(int))
						{
							num4 = 246.0;
							num3 += (double)((r2 == 0) ? 1 : (-1));
						}
						else
						{
							num4 = 50.0;
							num3 *= ((r2 == 0) ? 1.25 : 0.75);
						}
						try
						{
							num3 = Math.Max(val2, Math.Min(num4, num3));
							propertyInfo.SetValue(val, Convert.ChangeType(num3, propertyInfo.PropertyType));
						}
						catch (Exception ex4)
						{
							((NinjaScript)val).LogAndPrint(typeof(Resource), "DataGetPropertyValueException", new object[3]
							{
								propertyInfo.Name,
								((NinjaScriptBase)val).Name,
								NinjaScriptBase.GetExceptionMessage(ex4)
							}, (LogLevel)3);
							((NinjaScript)val).SetState((State)9);
							return indicatorExpression;
						}
					}
					else
					{
						num = r0;
						if (num >= 6 && num < 10)
						{
							if (!logic.TryLinearMutation)
							{
								r4 = random.Next(((NinjaScriptBase)((r0 < 8) ? indicatorExpression.Left : indicatorExpression.Right)).Values.Length);
							}
							((NinjaScriptBase)((r0 < 8) ? indicatorExpression.Left : indicatorExpression.Right)).SelectedValueSeries = r4;
							indicatorExpression.MaxCompare = double.NaN;
							indicatorExpression.MinCompare = double.NaN;
						}
						else
						{
							num = r0;
							if (num >= 30 && num < 40)
							{
								indicatorExpression.LeftBarsAgo = Math.Max(0, indicatorExpression.LeftBarsAgo + ((r2 == 0) ? 1 : (-1)));
								logic.TryLinearMutation = true;
							}
							else
							{
								num = r0;
								if (num >= 40 && num < 50)
								{
									indicatorExpression.RightBarsAgo = Math.Max(0, indicatorExpression.RightBarsAgo + ((r2 == 0) ? 1 : (-1)));
									logic.TryLinearMutation = true;
								}
							}
						}
					}
				}
			}
		}
		return indicatorExpression;
		IL_0067:
		obj.Condition = (Condition)num2;
		if (toMutate == this)
		{
			int num = r0;
			if (num >= 2 && num < 4)
			{
				left = logic.RandomIndicator(random);
				goto IL_0099;
			}
		}
		left = (object)(IndicatorBase)((NinjaScript)Left).Clone();
		goto IL_0099;
	}

	public void Print(StringBuilder s, int indentationLevel)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected I4, but got Unknown
		Condition condition = Condition;
		switch ((int)condition)
		{
		case 0:
			s.Append("CrossAbove(");
			s.Append(Left.GetDisplayName(true, true, false));
			if (((NinjaScriptBase)Left).SelectedValueSeries != 0)
			{
				s.Append(".Values[" + ((NinjaScriptBase)Left).SelectedValueSeries + "]");
			}
			s.Append(", ");
			s.Append((!((NinjaScriptBase)Left).IsOverlay) ? CompareValue.ToString(CultureInfo.InvariantCulture) : (UsePriceToCompare ? "Close" : (Right.GetDisplayName(true, true, false) + ((((NinjaScriptBase)Right).SelectedValueSeries != 0) ? (".Values[" + ((NinjaScriptBase)Right).SelectedValueSeries + "]") : string.Empty))));
			s.Append(", 1)");
			break;
		case 1:
			s.Append("CrossBelow(");
			s.Append(Left.GetDisplayName(true, true, false));
			if (((NinjaScriptBase)Left).SelectedValueSeries != 0)
			{
				s.Append(".Values[" + ((NinjaScriptBase)Left).SelectedValueSeries + "]");
			}
			s.Append(", ");
			s.Append((!((NinjaScriptBase)Left).IsOverlay) ? CompareValue.ToString(CultureInfo.InvariantCulture) : (UsePriceToCompare ? "Close" : (Right.GetDisplayName(true, true, false) + ((((NinjaScriptBase)Right).SelectedValueSeries != 0) ? (".Values[" + ((NinjaScriptBase)Right).SelectedValueSeries + "]") : string.Empty))));
			s.Append(", 1)");
			break;
		case 2:
			s.Append(Left.GetDisplayName(true, true, false));
			if (((NinjaScriptBase)Left).SelectedValueSeries != 0)
			{
				s.Append(".Values[" + ((NinjaScriptBase)Left).SelectedValueSeries + "]");
			}
			s.Append("[" + LeftBarsAgo + "].ApproxCompare(");
			s.Append(((NinjaScriptBase)Left).IsOverlay ? ((UsePriceToCompare ? "Close" : (Right.GetDisplayName(true, true, false) + ((((NinjaScriptBase)Right).SelectedValueSeries != 0) ? (".Values[" + ((NinjaScriptBase)Right).SelectedValueSeries + "]") : string.Empty))) + "[" + RightBarsAgo + "]") : CompareValue.ToString(CultureInfo.InvariantCulture));
			s.Append(") == 0");
			break;
		case 3:
			s.Append(Left.GetDisplayName(true, true, false));
			if (((NinjaScriptBase)Left).SelectedValueSeries != 0)
			{
				s.Append(".Values[" + ((NinjaScriptBase)Left).SelectedValueSeries + "]");
			}
			s.Append("[" + LeftBarsAgo + "].ApproxCompare(");
			s.Append(((NinjaScriptBase)Left).IsOverlay ? ((UsePriceToCompare ? "Close" : (Right.GetDisplayName(true, true, false) + ((((NinjaScriptBase)Right).SelectedValueSeries != 0) ? (".Values[" + ((NinjaScriptBase)Right).SelectedValueSeries + "]") : string.Empty))) + "[" + RightBarsAgo + "]") : CompareValue.ToString(CultureInfo.InvariantCulture));
			s.Append(") > 0");
			break;
		case 4:
			s.Append(Left.GetDisplayName(true, true, false));
			if (((NinjaScriptBase)Left).SelectedValueSeries != 0)
			{
				s.Append(".Values[" + ((NinjaScriptBase)Left).SelectedValueSeries + "]");
			}
			s.Append("[" + LeftBarsAgo + "].ApproxCompare(");
			s.Append(((NinjaScriptBase)Left).IsOverlay ? ((UsePriceToCompare ? "Close" : (Right.GetDisplayName(true, true, false) + ((((NinjaScriptBase)Right).SelectedValueSeries != 0) ? (".Values[" + ((NinjaScriptBase)Right).SelectedValueSeries + "]") : string.Empty))) + "[" + RightBarsAgo + "]") : CompareValue.ToString(CultureInfo.InvariantCulture));
			s.Append(") >= 0");
			break;
		case 5:
			s.Append(Left.GetDisplayName(true, true, false));
			if (((NinjaScriptBase)Left).SelectedValueSeries != 0)
			{
				s.Append(".Values[" + ((NinjaScriptBase)Left).SelectedValueSeries + "]");
			}
			s.Append("[" + LeftBarsAgo + "].ApproxCompare(");
			s.Append(((NinjaScriptBase)Left).IsOverlay ? ((UsePriceToCompare ? "Close" : (Right.GetDisplayName(true, true, false) + ((((NinjaScriptBase)Right).SelectedValueSeries != 0) ? (".Values[" + ((NinjaScriptBase)Right).SelectedValueSeries + "]") : string.Empty))) + "[" + RightBarsAgo + "]") : CompareValue.ToString(CultureInfo.InvariantCulture));
			s.Append(") < 0");
			break;
		case 6:
			s.Append(Left.GetDisplayName(true, true, false));
			if (((NinjaScriptBase)Left).SelectedValueSeries != 0)
			{
				s.Append(".Values[" + ((NinjaScriptBase)Left).SelectedValueSeries + "]");
			}
			s.Append("[" + LeftBarsAgo + "].ApproxCompare(");
			s.Append(((NinjaScriptBase)Left).IsOverlay ? ((UsePriceToCompare ? "Close" : (Right.GetDisplayName(true, true, false) + ((((NinjaScriptBase)Right).SelectedValueSeries != 0) ? (".Values[" + ((NinjaScriptBase)Right).SelectedValueSeries + "]") : string.Empty))) + "[" + RightBarsAgo + "]") : CompareValue.ToString(CultureInfo.InvariantCulture));
			s.Append(") <= 0");
			break;
		case 7:
			s.Append(Left.GetDisplayName(true, true, false));
			if (((NinjaScriptBase)Left).SelectedValueSeries != 0)
			{
				s.Append(".Values[" + ((NinjaScriptBase)Left).SelectedValueSeries + "]");
			}
			s.Append("[" + LeftBarsAgo + "].ApproxCompare(");
			s.Append(((NinjaScriptBase)Left).IsOverlay ? ((UsePriceToCompare ? "Close" : (Right.GetDisplayName(true, true, false) + ((((NinjaScriptBase)Right).SelectedValueSeries != 0) ? (".Values[" + ((NinjaScriptBase)Right).SelectedValueSeries + "]") : string.Empty))) + "[" + RightBarsAgo + "]") : CompareValue.ToString(CultureInfo.InvariantCulture));
			s.Append(") != 0");
			break;
		}
	}

	public void PrintAddChartIndicator(GeneratedStrategyLogic logic, StringBuilder s, int indentationLevel)
	{
		string text = "AddChartIndicator(" + Left.GetDisplayName(true, true, false) + ");" + Environment.NewLine;
		if (!logic.ChartIndicators.Contains(text))
		{
			logic.ChartIndicators.Add(text);
			s.Indent(indentationLevel);
			s.Append(text);
		}
		if (!UsePriceToCompare)
		{
			text = "AddChartIndicator(" + Right.GetDisplayName(true, true, false) + ");" + Environment.NewLine;
			if (!logic.ChartIndicators.Contains(text))
			{
				logic.ChartIndicators.Add(text);
				s.Indent(indentationLevel);
				s.Append(text);
			}
		}
	}

	private double RoundToNearestDecimal(double value, bool up)
	{
		if ((value == double.MinValue || double.IsNaN(value)) ? true : false)
		{
			return value;
		}
		bool flag = MathExtentions.ApproxCompare(value, 0.0) >= 0;
		double num = 1E-10 * (double)((MathExtentions.ApproxCompare(value, 0.0) >= 0) ? 1 : (-1));
		while (true)
		{
			for (int i = 1; i <= 10; i++)
			{
				if ((up && flag && MathExtentions.ApproxCompare(num * (double)i, value) >= 0) || (!up && flag && MathExtentions.ApproxCompare(num * (double)(i + 1), value) >= 0) || (up && !flag && MathExtentions.ApproxCompare(num * (double)(i + 1), value) <= 0) || (!up && !flag && MathExtentions.ApproxCompare(num * (double)i, value) <= 0))
				{
					return num * (double)i;
				}
			}
			num *= 10.0;
		}
	}

	public XElement ToXml()
	{
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		XElement xElement = new XElement(GetType().Name);
		if (Left != null)
		{
			using StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
			new XmlSerializer(((object)Left).GetType()).Serialize(stringWriter, Left);
			xElement.Add(new XElement("LeftType", ((object)Left).GetType().FullName));
			xElement.Add(new XElement("Left", XElement.Parse(stringWriter.ToString())));
		}
		if (Right != null)
		{
			using StringWriter stringWriter2 = new StringWriter(CultureInfo.InvariantCulture);
			new XmlSerializer(((object)Right).GetType()).Serialize(stringWriter2, Right);
			xElement.Add(new XElement("RightType", ((object)Right).GetType().FullName));
			xElement.Add(new XElement("Right", XElement.Parse(stringWriter2.ToString())));
		}
		xElement.Add(new XElement("CompareFactor", CompareFactor.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("Condition", ((object)Condition/*cast due to .constrained prefix*/).ToString()));
		xElement.Add(new XElement("LeftBarsAgo", LeftBarsAgo.ToString()));
		xElement.Add(new XElement("MaxCompare", MaxCompare.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("MinCompare", MinCompare.ToString(CultureInfo.InvariantCulture)));
		xElement.Add(new XElement("RightBarsAgo", RightBarsAgo.ToString()));
		xElement.Add(new XElement("UsePriceToCompare", UsePriceToCompare.ToString()));
		return xElement;
	}
}

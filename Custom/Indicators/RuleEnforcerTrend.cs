#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom.RuleEnforcer;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Intraday uptrend detector tuned for 3-minute charts.
	/// Blocks shorts when price holds above a fast EMA with rising slope,
	/// and optionally above session VWAP.
	/// </summary>
	public class RuleEnforcerTrend : Indicator
	{
		public const string VoteSourceId = "RuleEnforcerTrend";

		private EMA trendEma;
		private double cumulativeVolume;
		private double cumulativeTypicalVolume;
		private bool lastVoteShortAllowed = true;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Intraday uptrend filter for 3-minute charts. Blocks shorts when price is above a rising fast EMA and (optionally) session VWAP.";
				Name						= "Rule Enforcer Trend";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= true;
				DisplayInDataBox			= true;
				DrawOnPricePanel			= true;
				IsSuspendedWhileInactive	= true;

				// ~63 minutes of structure on a 3-minute chart
				EmaPeriod					= 21;
				RequireRisingEma			= true;
				UseVwapFilter				= true;
				ShowEma						= true;

				AddPlot(Brushes.DodgerBlue, "Trend EMA");
			}
			else if (State == State.DataLoaded)
			{
				trendEma = EMA(EmaPeriod);
			}
			else if (State == State.Terminated)
				RuleEnforcerState.RemoveSource(Instrument.FullName, VoteSourceId);
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToPlot())
				return;

			if (ShowEma)
				Values[0][0] = trendEma[0];
			else
				Values[0][0] = double.NaN;

			bool uptrend = IsUptrend();
			bool sourceAllowsShorts = !uptrend;

			if (sourceAllowsShorts != lastVoteShortAllowed)
			{
				lastVoteShortAllowed = sourceAllowsShorts;
				RuleEnforcerState.SetSourceVote(Instrument.FullName, VoteSourceId, sourceAllowsShorts);
			}

		}

		private int BarsRequiredToPlot() => Math.Max(EmaPeriod + 1, 2);

		private double SessionVwap()
		{
			if (Bars.IsFirstBarOfSession)
			{
				cumulativeVolume = 0;
				cumulativeTypicalVolume = 0;
			}

			double typicalPrice = (High[0] + Low[0] + Close[0]) / 3.0;
			cumulativeTypicalVolume += typicalPrice * Volume[0];
			cumulativeVolume += Volume[0];

			return cumulativeVolume > 0 ? cumulativeTypicalVolume / cumulativeVolume : Close[0];
		}

		private bool IsUptrend()
		{
			if (Close[0] <= trendEma[0])
				return false;

			if (RequireRisingEma && trendEma[0] <= trendEma[1])
				return false;

			if (UseVwapFilter && Close[0] <= SessionVwap())
				return false;

			return true;
		}

		#region Properties

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "EMA Period", Description = "Default 21 suits 3-minute intraday (~1 hour of bars).", Order = 1, GroupName = "Trend")]
		public int EmaPeriod { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require Rising EMA", Description = "EMA must slope up on the current bar.", Order = 2, GroupName = "Trend")]
		public bool RequireRisingEma { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use VWAP Filter", Description = "Also require price above session VWAP before blocking shorts.", Order = 3, GroupName = "Trend")]
		public bool UseVwapFilter { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show EMA", Order = 1, GroupName = "Display")]
		public bool ShowEma { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RuleEnforcerTrend[] cacheRuleEnforcerTrend;
		public RuleEnforcerTrend RuleEnforcerTrend(int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma)
		{
			return RuleEnforcerTrend(Input, emaPeriod, requireRisingEma, useVwapFilter, showEma);
		}

		public RuleEnforcerTrend RuleEnforcerTrend(ISeries<double> input, int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma)
		{
			if (cacheRuleEnforcerTrend != null)
				for (int idx = 0; idx < cacheRuleEnforcerTrend.Length; idx++)
					if (cacheRuleEnforcerTrend[idx] != null && cacheRuleEnforcerTrend[idx].EmaPeriod == emaPeriod && cacheRuleEnforcerTrend[idx].RequireRisingEma == requireRisingEma && cacheRuleEnforcerTrend[idx].UseVwapFilter == useVwapFilter && cacheRuleEnforcerTrend[idx].ShowEma == showEma && cacheRuleEnforcerTrend[idx].EqualsInput(input))
						return cacheRuleEnforcerTrend[idx];
			return CacheIndicator<RuleEnforcerTrend>(new RuleEnforcerTrend(){ EmaPeriod = emaPeriod, RequireRisingEma = requireRisingEma, UseVwapFilter = useVwapFilter, ShowEma = showEma }, input, ref cacheRuleEnforcerTrend);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RuleEnforcerTrend RuleEnforcerTrend(int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma)
		{
			return indicator.RuleEnforcerTrend(Input, emaPeriod, requireRisingEma, useVwapFilter, showEma);
		}

		public Indicators.RuleEnforcerTrend RuleEnforcerTrend(ISeries<double> input , int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma)
		{
			return indicator.RuleEnforcerTrend(input, emaPeriod, requireRisingEma, useVwapFilter, showEma);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RuleEnforcerTrend RuleEnforcerTrend(int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma)
		{
			return indicator.RuleEnforcerTrend(Input, emaPeriod, requireRisingEma, useVwapFilter, showEma);
		}

		public Indicators.RuleEnforcerTrend RuleEnforcerTrend(ISeries<double> input , int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma)
		{
			return indicator.RuleEnforcerTrend(input, emaPeriod, requireRisingEma, useVwapFilter, showEma);
		}
	}
}

#endregion
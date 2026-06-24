#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Custom.RuleEnforcer;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
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
		private EMA trendEma;
		private double cumulativeVolume;
		private double cumulativeTypicalVolume;
		private bool lastShortAllowed = true;

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
				ShowStatusLabel				= true;
			}
			else if (State == State.DataLoaded)
			{
				trendEma = EMA(EmaPeriod);
				if (ShowEma)
					AddChartIndicator(trendEma);
			}
			else if (State == State.Terminated)
				RuleEnforcerState.Clear(Instrument.FullName);
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToPlot())
				return;

			bool uptrend = IsUptrend();
			bool shortAllowed = !uptrend;

			if (shortAllowed != lastShortAllowed)
			{
				lastShortAllowed = shortAllowed;
				RuleEnforcerState.SetShortAllowed(Instrument.FullName, shortAllowed);
			}

			if (ShowStatusLabel && IsFirstTickOfBar)
			{
				string status = shortAllowed
					? "Shorts: ALLOWED"
					: string.Format("Shorts: BLOCKED ({0} {1} uptrend)", EmaPeriod, UseVwapFilter ? "EMA + VWAP" : "EMA");
				Brush color = shortAllowed ? Brushes.LimeGreen : Brushes.OrangeRed;
				Draw.TextFixed(this, "RuleEnforcerStatus", status, TextPosition.TopRight, color,
					new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.Transparent, 0);
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

		[NinjaScriptProperty]
		[Display(Name = "Show Status Label", Order = 2, GroupName = "Display")]
		public bool ShowStatusLabel { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RuleEnforcerTrend[] cacheRuleEnforcerTrend;
		public RuleEnforcerTrend RuleEnforcerTrend(int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma, bool showStatusLabel)
		{
			return RuleEnforcerTrend(Input, emaPeriod, requireRisingEma, useVwapFilter, showEma, showStatusLabel);
		}

		public RuleEnforcerTrend RuleEnforcerTrend(ISeries<double> input, int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma, bool showStatusLabel)
		{
			if (cacheRuleEnforcerTrend != null)
				for (int idx = 0; idx < cacheRuleEnforcerTrend.Length; idx++)
					if (cacheRuleEnforcerTrend[idx] != null && cacheRuleEnforcerTrend[idx].EmaPeriod == emaPeriod && cacheRuleEnforcerTrend[idx].RequireRisingEma == requireRisingEma && cacheRuleEnforcerTrend[idx].UseVwapFilter == useVwapFilter && cacheRuleEnforcerTrend[idx].ShowEma == showEma && cacheRuleEnforcerTrend[idx].ShowStatusLabel == showStatusLabel && cacheRuleEnforcerTrend[idx].EqualsInput(input))
						return cacheRuleEnforcerTrend[idx];
			return CacheIndicator<RuleEnforcerTrend>(new RuleEnforcerTrend(){ EmaPeriod = emaPeriod, RequireRisingEma = requireRisingEma, UseVwapFilter = useVwapFilter, ShowEma = showEma, ShowStatusLabel = showStatusLabel }, input, ref cacheRuleEnforcerTrend);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RuleEnforcerTrend RuleEnforcerTrend(int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma, bool showStatusLabel)
		{
			return indicator.RuleEnforcerTrend(Input, emaPeriod, requireRisingEma, useVwapFilter, showEma, showStatusLabel);
		}

		public Indicators.RuleEnforcerTrend RuleEnforcerTrend(ISeries<double> input , int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma, bool showStatusLabel)
		{
			return indicator.RuleEnforcerTrend(input, emaPeriod, requireRisingEma, useVwapFilter, showEma, showStatusLabel);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RuleEnforcerTrend RuleEnforcerTrend(int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma, bool showStatusLabel)
		{
			return indicator.RuleEnforcerTrend(Input, emaPeriod, requireRisingEma, useVwapFilter, showEma, showStatusLabel);
		}

		public Indicators.RuleEnforcerTrend RuleEnforcerTrend(ISeries<double> input , int emaPeriod, bool requireRisingEma, bool useVwapFilter, bool showEma, bool showStatusLabel)
		{
			return indicator.RuleEnforcerTrend(input, emaPeriod, requireRisingEma, useVwapFilter, showEma, showStatusLabel);
		}
	}
}

#endregion
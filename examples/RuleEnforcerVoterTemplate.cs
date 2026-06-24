//
// NT8 Rule Enforcer — voter template
//
// Copy patterns from this file into your existing indicators.
// This file is NOT meant to be compiled as-is in NinjaTrader unless you
// rename the class and import it under Custom\Indicators\.
//
// Grok sessions: see GROK-ADD-VOTING-TO-INDICATOR.md for full instructions.
//

#region Using declarations
using NinjaTrader.Custom.RuleEnforcer;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Template showing how to vote in Rule Enforcer.
	/// Replace ExampleSignalLogic() with your own conditions.
	/// </summary>
	public class RuleEnforcerVoterTemplate : Indicator
	{
		// REQUIRED: unique id — use your real indicator class name
		private const string VoteSourceId = "RuleEnforcerVoterTemplate";

		private EMA ema21;
		private bool lastVoteShortAllowed = true;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Template for Rule Enforcer voting integration.";
				Name						= "Rule Enforcer Voter Template";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= true;
			}
			else if (State == State.DataLoaded)
			{
				ema21 = EMA(21);
			}
			else if (State == State.Terminated)
			{
				// REQUIRED: withdraw vote when indicator is removed from chart
				RuleEnforcerState.RemoveSource(Instrument.FullName, VoteSourceId);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 21)
				return;

			// true  = this source permits shorts
			// false = this source blocks shorts
			bool shortAllowed = ExampleSignalLogic();

			if (shortAllowed != lastVoteShortAllowed)
			{
				lastVoteShortAllowed = shortAllowed;
				RuleEnforcerState.SetSourceVote(Instrument.FullName, VoteSourceId, shortAllowed);
			}
		}

		private bool ExampleSignalLogic()
		{
			// EXAMPLE: block shorts when price is above a rising 21 EMA (uptrend)
			bool uptrend = Close[0] > ema21[0] && ema21[0] > ema21[1];
			return !uptrend;
		}
	}
}

// --- SNIPPET: paste into an EXISTING indicator (minimal diff) ---
//
// 1) Add at top:
//    using NinjaTrader.Custom.RuleEnforcer;
//
// 2) Add inside class:
//    private const string VoteSourceId = "YourIndicatorClassName";
//    private bool lastVoteShortAllowed = true;
//
// 3) Add to OnStateChange → State.Terminated:
//    RuleEnforcerState.RemoveSource(Instrument.FullName, VoteSourceId);
//
// 4) Add at end of OnBarUpdate (after your signal logic):
//    bool shortAllowed = !yourBlockCondition;
//    if (shortAllowed != lastVoteShortAllowed)
//    {
//        lastVoteShortAllowed = shortAllowed;
//        RuleEnforcerState.SetSourceVote(Instrument.FullName, VoteSourceId, shortAllowed);
//    }
//
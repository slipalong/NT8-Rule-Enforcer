#region Using declarations
using System;
using System.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Code;
using NinjaTrader.Custom.RuleEnforcer;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.AddOns
{
	/// <summary>
	/// Cancels short-entry orders when RuleEnforcerState blocks shorts.
	/// Loads automatically with NinjaTrader; no chart attachment required.
	/// </summary>
	public class RuleEnforcerOrderGuard : AddOnBase
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "Rule Enforcer Order Guard";
			}
			else if (State == State.Active)
			{
				foreach (Account account in Account.All)
					account.OrderUpdate += OnAccountOrderUpdate;
			}
			else if (State == State.Terminated)
			{
				foreach (Account account in Account.All)
					account.OrderUpdate -= OnAccountOrderUpdate;
			}
		}

		private void OnAccountOrderUpdate(object sender, OrderEventArgs e)
		{
			Order order = e?.Order;
			if (order == null || order.Instrument == null)
				return;

			// Only intercept orders as they are initialized, before they reach the exchange.
			if (order.OrderState != OrderState.Initialized)
				return;

			if (RuleEnforcerState.IsShortAllowed(order.Instrument.FullName))
				return;

			if (!IsBlockedShortEntry(order))
				return;

			try
			{
				order.Account.Cancel(new[] { order });
				Output.Process(string.Format("Rule Enforcer: cancelled short entry on {0} ({1})",
					order.Instrument.FullName, order.OrderAction), PrintTo.OutputTab1);
			}
			catch (Exception ex)
			{
				Output.Process(string.Format("Rule Enforcer: failed to cancel order on {0}: {1}",
					order.Instrument.FullName, ex.Message), PrintTo.OutputTab1);
			}
		}

		private static bool IsBlockedShortEntry(Order order)
		{
			if (order.OrderAction == OrderAction.SellShort)
				return true;

			// Chart Trader sell from flat on some accounts submits Sell instead of SellShort.
			if (order.OrderAction != OrderAction.Sell)
				return false;

			Position position = order.Account.Positions
				.FirstOrDefault(p => p.Instrument == order.Instrument);

			return position == null || position.MarketPosition == MarketPosition.Flat;
		}
	}
}
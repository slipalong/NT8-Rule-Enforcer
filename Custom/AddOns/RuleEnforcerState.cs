#region Using declarations
using System;
using System.Collections.Generic;
#endregion

namespace NinjaTrader.Custom.RuleEnforcer
{
	/// <summary>
	/// Shared state between the trend indicator, Chart Trader UI enforcer, and order guard AddOn.
	/// Keyed by instrument full name so multiple charts can run independently.
	/// </summary>
	public static class RuleEnforcerState
	{
		private static readonly object Sync = new object();
		private static readonly Dictionary<string, bool> ShortAllowedByInstrument = new Dictionary<string, bool>();

		/// <summary>
		/// Fired when short permission changes for an instrument.
		/// Args: instrument full name, whether shorts are allowed.
		/// </summary>
		public static event Action<string, bool> ShortAllowedChanged;

		public static void SetShortAllowed(string instrumentFullName, bool allowed)
		{
			if (string.IsNullOrWhiteSpace(instrumentFullName))
				return;

			bool changed = false;

			lock (Sync)
			{
				if (!ShortAllowedByInstrument.TryGetValue(instrumentFullName, out bool current) || current != allowed)
				{
					ShortAllowedByInstrument[instrumentFullName] = allowed;
					changed = true;
				}
			}

			if (changed)
				ShortAllowedChanged?.Invoke(instrumentFullName, allowed);
		}

		public static bool IsShortAllowed(string instrumentFullName)
		{
			if (string.IsNullOrWhiteSpace(instrumentFullName))
				return true;

			lock (Sync)
				return !ShortAllowedByInstrument.TryGetValue(instrumentFullName, out bool allowed) || allowed;
		}

		public static void Clear(string instrumentFullName)
		{
			if (string.IsNullOrWhiteSpace(instrumentFullName))
				return;

			lock (Sync)
				ShortAllowedByInstrument.Remove(instrumentFullName);
		}
	}
}
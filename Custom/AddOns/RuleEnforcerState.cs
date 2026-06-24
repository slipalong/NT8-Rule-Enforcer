#region Using declarations
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace NinjaTrader.Custom.RuleEnforcer
{
	/// <summary>
	/// Shared voting state between indicators, Chart Trader UI, and the order guard AddOn.
	/// Multiple indicators can vote independently per instrument; shorts are allowed only when ALL votes allow.
	/// </summary>
	public static class RuleEnforcerState
	{
		private static readonly object Sync = new object();
		private static readonly Dictionary<string, Dictionary<string, bool>> VotesByInstrument =
			new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

		/// <summary>
		/// Fired when the aggregated short permission changes for an instrument.
		/// Args: instrument full name, whether shorts are allowed.
		/// </summary>
		public static event Action<string, bool> ShortAllowedChanged;

		/// <summary>
		/// Registers or updates one indicator's vote for an instrument.
		/// shortAllowed = true means this source permits shorts; false means this source blocks shorts.
		/// </summary>
		public static void SetSourceVote(string instrumentFullName, string sourceId, bool shortAllowed)
		{
			if (string.IsNullOrWhiteSpace(instrumentFullName) || string.IsNullOrWhiteSpace(sourceId))
				return;

			bool aggregateChanged = false;
			bool newAggregate = true;

			lock (Sync)
			{
				if (!VotesByInstrument.TryGetValue(instrumentFullName, out Dictionary<string, bool> votes))
				{
					votes = new Dictionary<string, bool>(StringComparer.Ordinal);
					VotesByInstrument[instrumentFullName] = votes;
				}

				bool oldAggregate = ComputeAggregateLocked(instrumentFullName);

				if (!votes.TryGetValue(sourceId, out bool current) || current != shortAllowed)
					votes[sourceId] = shortAllowed;

				newAggregate = ComputeAggregateLocked(instrumentFullName);
				aggregateChanged = oldAggregate != newAggregate;
			}

			if (aggregateChanged)
				ShortAllowedChanged?.Invoke(instrumentFullName, newAggregate);
		}

		/// <summary>
		/// Removes one source's vote. Use in State.Terminated when an indicator is removed from a chart.
		/// </summary>
		public static void RemoveSource(string instrumentFullName, string sourceId)
		{
			if (string.IsNullOrWhiteSpace(instrumentFullName) || string.IsNullOrWhiteSpace(sourceId))
				return;

			bool aggregateChanged = false;
			bool newAggregate = true;

			lock (Sync)
			{
				if (!VotesByInstrument.TryGetValue(instrumentFullName, out Dictionary<string, bool> votes))
					return;

				if (!votes.ContainsKey(sourceId))
					return;

				bool oldAggregate = ComputeAggregateLocked(instrumentFullName);

				votes.Remove(sourceId);

				if (votes.Count == 0)
					VotesByInstrument.Remove(instrumentFullName);

				newAggregate = ComputeAggregateLocked(instrumentFullName);
				aggregateChanged = oldAggregate != newAggregate;
			}

			if (aggregateChanged)
				ShortAllowedChanged?.Invoke(instrumentFullName, newAggregate);
		}

		/// <summary>
		/// Returns true when no source is blocking shorts for this instrument.
		/// If no votes are registered, shorts are allowed.
		/// </summary>
		public static bool IsShortAllowed(string instrumentFullName)
		{
			if (string.IsNullOrWhiteSpace(instrumentFullName))
				return true;

			lock (Sync)
				return ComputeAggregateLocked(instrumentFullName);
		}

		/// <summary>
		/// Returns the source ids currently blocking shorts for an instrument.
		/// </summary>
		public static string[] GetBlockingSources(string instrumentFullName)
		{
			if (string.IsNullOrWhiteSpace(instrumentFullName))
				return Array.Empty<string>();

			lock (Sync)
			{
				if (!VotesByInstrument.TryGetValue(instrumentFullName, out Dictionary<string, bool> votes))
					return Array.Empty<string>();

				return votes.Where(v => !v.Value).Select(v => v.Key).ToArray();
			}
		}

		/// <summary>
		/// Removes all votes for an instrument.
		/// </summary>
		public static void Clear(string instrumentFullName)
		{
			if (string.IsNullOrWhiteSpace(instrumentFullName))
				return;

			bool aggregateChanged = false;

			lock (Sync)
			{
				if (!VotesByInstrument.ContainsKey(instrumentFullName))
					return;

				bool oldAggregate = ComputeAggregateLocked(instrumentFullName);
				VotesByInstrument.Remove(instrumentFullName);
				aggregateChanged = !oldAggregate;
			}

			if (aggregateChanged)
				ShortAllowedChanged?.Invoke(instrumentFullName, true);
		}

		private static bool ComputeAggregateLocked(string instrumentFullName)
		{
			if (!VotesByInstrument.TryGetValue(instrumentFullName, out Dictionary<string, bool> votes) || votes.Count == 0)
				return true;

			foreach (bool shortAllowed in votes.Values)
			{
				if (!shortAllowed)
					return false;
			}

			return true;
		}
	}
}
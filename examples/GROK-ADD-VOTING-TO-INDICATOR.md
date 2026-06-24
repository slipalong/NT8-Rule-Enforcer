# Grok Task: Add NT8 Rule Enforcer Voting to an Existing Indicator

**Paste this entire file into a Grok session** when you want to modify an existing NinjaTrader 8 indicator so it participates in the Rule Enforcer short-block voting system.

---

## Your task

Modify the user's existing NinjaScript indicator to **vote** on whether short entries are allowed. Do **not** rewrite unrelated logic. Make the **smallest possible diff**.

The indicator must integrate with `RuleEnforcerState` from the NT8 Rule Enforcer project:

- Repo: https://github.com/slipalong/NT8-Rule-Enforcer
- State file: `Custom/AddOns/RuleEnforcerState.cs`
- Namespace: `NinjaTrader.Custom.RuleEnforcer`

---

## How voting works

Multiple indicators can vote per instrument. Shorts are **blocked** if **any** source votes `shortAllowed = false`.

| Vote value | Meaning |
|------------|---------|
| `shortAllowed = true` | This indicator **permits** shorts |
| `shortAllowed = false` | This indicator **blocks** shorts |

The Chart Trader UI and Order Guard read the **aggregate** via `IsShortAllowed()` — you do not need to touch those components.

---

## API (use only these methods)

```csharp
using NinjaTrader.Custom.RuleEnforcer;

// Register or update this indicator's vote
RuleEnforcerState.SetSourceVote(Instrument.FullName, sourceId, shortAllowed);

// Remove vote when indicator is removed from chart (REQUIRED in State.Terminated)
RuleEnforcerState.RemoveSource(Instrument.FullName, sourceId);

// Optional: debug which sources are blocking
string[] blockers = RuleEnforcerState.GetBlockingSources(Instrument.FullName);
```

---

## Integration checklist

Apply these changes to the **existing** indicator:

### 1. Add using directive (top of file)

```csharp
using NinjaTrader.Custom.RuleEnforcer;
```

### 2. Add a unique source id constant (inside the class)

Use the indicator class name. Must be unique across all voters.

```csharp
private const string VoteSourceId = "YourIndicatorClassName";
```

### 3. Add a field to avoid redundant votes (optional but recommended)

```csharp
private bool lastVoteShortAllowed = true;
```

### 4. Register `RemoveSource` in `OnStateChange`

```csharp
else if (State == State.Terminated)
{
    RuleEnforcerState.RemoveSource(Instrument.FullName, VoteSourceId);
}
```

If `State.Terminated` already has logic, **append** the `RemoveSource` call — do not remove existing cleanup.

### 5. Vote in `OnBarUpdate` (or wherever the signal is computed)

After your existing logic computes whether shorts should be allowed:

```csharp
bool shortAllowed = /* true = permit shorts, false = block shorts */;

if (shortAllowed != lastVoteShortAllowed)
{
    lastVoteShortAllowed = shortAllowed;
    RuleEnforcerState.SetSourceVote(Instrument.FullName, VoteSourceId, shortAllowed);
}
```

**Important:** Map your signal correctly:

- If your indicator detects an **uptrend** and you want to **block shorts** → `shortAllowed = false`
- If your indicator says conditions are OK to short → `shortAllowed = true`

Example mapping:

```csharp
bool uptrend = Close[0] > ema[0];
bool shortAllowed = !uptrend;   // block shorts during uptrend
```

### 6. Guard for insufficient bars

Do not vote until your indicator has enough data:

```csharp
if (CurrentBar < BarsRequiredToPlot())
    return;
```

On first valid vote, set `lastVoteShortAllowed` to the opposite of the initial vote before calling `SetSourceVote`, or call `SetSourceVote` once on the first valid bar regardless of `lastVoteShortAllowed`.

---

## Full minimal template (new indicator)

If building from scratch, use this pattern:

```csharp
#region Using declarations
using NinjaTrader.Custom.RuleEnforcer;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class MyExistingIndicator : Indicator
    {
        private const string VoteSourceId = "MyExistingIndicator";
        private bool lastVoteShortAllowed = true;

        // ... existing fields ...

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                // ... existing defaults ...
            }
            else if (State == State.DataLoaded)
            {
                // ... existing data loaded logic ...
            }
            else if (State == State.Terminated)
            {
                RuleEnforcerState.RemoveSource(Instrument.FullName, VoteSourceId);
                // ... existing termination logic ...
            }
        }

        protected override void OnBarUpdate()
        {
            // ... existing early returns ...

            // EXAMPLE: block shorts when close is above 50 SMA
            bool shortAllowed = Close[0] < SMA(50)[0];

            if (shortAllowed != lastVoteShortAllowed)
            {
                lastVoteShortAllowed = shortAllowed;
                RuleEnforcerState.SetSourceVote(Instrument.FullName, VoteSourceId, shortAllowed);
            }

            // ... rest of existing OnBarUpdate ...
        }
    }
}
```

---

## Rules — do not break these

1. **`VoteSourceId` must be unique** — never reuse `"RuleEnforcerTrend"` or another indicator's id.
2. **Always call `RemoveSource` in `State.Terminated`** — ghost votes keep blocking after indicator removal.
3. **Key is `Instrument.FullName`** — votes apply per contract (e.g. `ES 06-26`), shared across charts of the same instrument.
4. **Do not call `Clear()` from a voter** — that wipes all sources; use `RemoveSource` for your id only.
5. **Do not modify `RuleEnforcerUI` or `RuleEnforcerOrderGuard`** — voters only call `SetSourceVote` / `RemoveSource`.
6. **Preserve existing indicator behavior** — only add voting; do not refactor unrelated code.

---

## Common mapping patterns

| User intent | `shortAllowed` value |
|-------------|----------------------|
| Block shorts in uptrend | `shortAllowed = !isUptrend` |
| Block shorts above VWAP | `shortAllowed = Close[0] < vwap` |
| Block shorts when RSI > 70 | `shortAllowed = RSI(14)[0] <= 70` |
| Block shorts during session window | `shortAllowed = !IsWithinBlockWindow()` |
| Only allow shorts in downtrend | `shortAllowed = isDowntrend` |

---

## Prerequisites on the user's machine

1. `RuleEnforcerState.cs` installed in `Documents\NinjaTrader 8\bin\Custom\AddOns\`
2. NinjaScript compiled (F5) with no errors
3. `RuleEnforcerUI` on chart for grey buttons (optional, cosmetic)
4. `RuleEnforcerOrderGuard` AddOn active (restart NT8 after first install) for order cancellation

---

## What to return

When modifying the user's indicator, return:

1. The **complete updated `.cs` file** (or a clear diff)
2. The **`VoteSourceId`** you chose
3. A one-sentence description of **when shorts are blocked** vs allowed
4. Reminder to **recompile (F5)** in NinjaTrader

---

## Reference implementation

See `RuleEnforcerTrend.cs` in the repo for a production voter:

- Source id: `"RuleEnforcerTrend"`
- Blocks shorts (`shortAllowed = false`) when: close > EMA(21), EMA rising, and close > session VWAP
- Removes vote in `State.Terminated`
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NinjaTrader.Custom.RuleEnforcer;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Greys out Chart Trader sell buttons and shows aggregate voter status when RuleEnforcerState blocks shorts.
	/// Requires at least one voting indicator (or any component that calls RuleEnforcerState.SetSourceVote).
	/// </summary>
	public class RuleEnforcerUI : Indicator
	{
		private static readonly string[] SellButtonAutomationIds =
		{
			"ChartTraderControlQuickSellMarketButton",
			"ChartTraderControlQuickSellLimitButton",
			"ChartTraderControlQuickSellStopMarketButton",
			"ChartTraderControlQuickSellStopLimitButton"
		};

		private Chart chartWindow;
		private Grid chartTraderGrid;
		private Grid chartTraderButtonsGrid;
		private readonly List<Button> sellButtons = new List<Button>();
		private bool controlsActive;
		private bool lastAppliedShortAllowed = true;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Disables Chart Trader sell buttons when shorts are blocked.";
				Name						= "Rule Enforcer UI";
				Calculate					= Calculate.OnEachTick;
				IsOverlay					= true;
				DisplayInDataBox			= false;
				PaintPriceMarkers			= false;
				IsSuspendedWhileInactive	= false;
				ShowStatusLabel				= true;
			}
			else if (State == State.Historical)
			{
				if (ChartControl != null)
				{
					ChartControl.Dispatcher.InvokeAsync(() =>
					{
						CreateWpfControls();
						ApplyShortPermission(RuleEnforcerState.IsShortAllowed(Instrument.FullName));
					});
				}
			}
			else if (State == State.DataLoaded)
			{
				RuleEnforcerState.ShortAllowedChanged += OnShortAllowedChanged;
			}
			else if (State == State.Terminated)
			{
				RuleEnforcerState.ShortAllowedChanged -= OnShortAllowedChanged;
				RemoveDrawObject("RuleEnforcerStatus");

				if (ChartControl != null)
				{
					ChartControl.Dispatcher.InvokeAsync(() =>
					{
						DisposeWpfControls();
					});
				}
			}
		}

		protected override void OnBarUpdate()
		{
			// Fallback poll in case a voter hasn't fired yet this session.
			bool shortAllowed = RuleEnforcerState.IsShortAllowed(Instrument.FullName);
			if (shortAllowed != lastAppliedShortAllowed)
				ApplyShortPermission(shortAllowed);
			else if (ShowStatusLabel && IsFirstTickOfBar)
				UpdateStatusLabel();
		}

		private void OnShortAllowedChanged(string instrumentFullName, bool shortAllowed)
		{
			if (instrumentFullName != Instrument.FullName)
				return;

			ApplyShortPermission(shortAllowed);
		}

		private void ApplyShortPermission(bool shortAllowed)
		{
			lastAppliedShortAllowed = shortAllowed;
			UpdateStatusLabel();

			if (ChartControl == null)
				return;

			ChartControl.Dispatcher.InvokeAsync(() =>
			{
				if (!controlsActive)
					return;

				foreach (Button button in sellButtons)
				{
					if (button != null)
						button.IsEnabled = shortAllowed;
				}
			});
		}

		private void UpdateStatusLabel()
		{
			if (!ShowStatusLabel)
			{
				RemoveDrawObject("RuleEnforcerStatus");
				return;
			}

			bool shortAllowed = RuleEnforcerState.IsShortAllowed(Instrument.FullName);
			string status = shortAllowed
				? "Shorts: ALLOWED"
				: FormatBlockedStatus();
			Brush color = shortAllowed ? Brushes.LimeGreen : Brushes.OrangeRed;

			Draw.TextFixed(this, "RuleEnforcerStatus", status, TextPosition.TopRight, color,
				new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.Transparent, 0);
		}

		private string FormatBlockedStatus()
		{
			string[] blockers = RuleEnforcerState.GetBlockingSources(Instrument.FullName);
			string blockerText = blockers.Length > 0 ? string.Join(", ", blockers) : "unknown";
			return string.Format("Shorts: BLOCKED ({0})", blockerText);
		}

		private void CreateWpfControls()
		{
			chartWindow = Window.GetWindow(ChartControl.Parent) as Chart;
			if (chartWindow == null)
				return;

			ChartTrader chartTrader = chartWindow.FindFirst("ChartWindowChartTraderControl") as ChartTrader;
			if (chartTrader == null)
				return;

			chartTraderGrid = chartTrader.Content as Grid;
			if (chartTraderGrid == null)
				return;

			chartTraderButtonsGrid = chartTraderGrid.Children[0] as Grid;
			sellButtons.Clear();

			foreach (string automationId in SellButtonAutomationIds)
			{
				Button button = chartTraderGrid.FindFirst(automationId) as Button;
				if (button == null && chartTraderButtonsGrid != null)
					button = chartTraderButtonsGrid.FindFirst(automationId) as Button;

				if (button != null && !sellButtons.Contains(button))
					sellButtons.Add(button);
			}

			if (sellButtons.Count == 0)
				Print("Rule Enforcer UI: no Chart Trader sell buttons found. Enable Chart Trader on this chart.");

			if (TabSelected())
				controlsActive = true;

			chartWindow.MainTabControl.SelectionChanged += TabChangedHandler;
		}

		private void DisposeWpfControls()
		{
			if (chartWindow != null)
				chartWindow.MainTabControl.SelectionChanged -= TabChangedHandler;

			// Re-enable buttons on removal so Chart Trader isn't left disabled.
			foreach (Button button in sellButtons)
			{
				if (button != null)
					button.IsEnabled = true;
			}

			sellButtons.Clear();
			controlsActive = false;
			chartTraderButtonsGrid = null;
			chartTraderGrid = null;
			chartWindow = null;
		}

		private bool TabSelected()
		{
			if (ChartControl == null || chartWindow == null || chartWindow.MainTabControl == null)
				return false;

			TabItem tabItem = chartWindow.MainTabControl.Items[chartWindow.MainTabControl.SelectedIndex] as TabItem;
			if (tabItem?.Content is ChartTab chartTab)
				return ChartControl.ChartTab == chartTab;

			return false;
		}

		private void TabChangedHandler(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0)
				return;

			controlsActive = TabSelected();
			ApplyShortPermission(RuleEnforcerState.IsShortAllowed(Instrument.FullName));
		}

		#region Properties

		[NinjaScriptProperty]
		[Display(Name = "Show Status Label", Description = "Shows aggregate short permission and blocking voter names.", Order = 1, GroupName = "Display")]
		public bool ShowStatusLabel { get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RuleEnforcerUI[] cacheRuleEnforcerUI;
		public RuleEnforcerUI RuleEnforcerUI(bool showStatusLabel)
		{
			return RuleEnforcerUI(Input, showStatusLabel);
		}

		public RuleEnforcerUI RuleEnforcerUI(ISeries<double> input, bool showStatusLabel)
		{
			if (cacheRuleEnforcerUI != null)
				for (int idx = 0; idx < cacheRuleEnforcerUI.Length; idx++)
					if (cacheRuleEnforcerUI[idx] != null && cacheRuleEnforcerUI[idx].ShowStatusLabel == showStatusLabel && cacheRuleEnforcerUI[idx].EqualsInput(input))
						return cacheRuleEnforcerUI[idx];
			return CacheIndicator<RuleEnforcerUI>(new RuleEnforcerUI(){ ShowStatusLabel = showStatusLabel }, input, ref cacheRuleEnforcerUI);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RuleEnforcerUI RuleEnforcerUI(bool showStatusLabel)
		{
			return indicator.RuleEnforcerUI(Input, showStatusLabel);
		}

		public Indicators.RuleEnforcerUI RuleEnforcerUI(ISeries<double> input , bool showStatusLabel)
		{
			return indicator.RuleEnforcerUI(input, showStatusLabel);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RuleEnforcerUI RuleEnforcerUI(bool showStatusLabel)
		{
			return indicator.RuleEnforcerUI(Input, showStatusLabel);
		}

		public Indicators.RuleEnforcerUI RuleEnforcerUI(ISeries<double> input , bool showStatusLabel)
		{
			return indicator.RuleEnforcerUI(input, showStatusLabel);
		}
	}
}

#endregion
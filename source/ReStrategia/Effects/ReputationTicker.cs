using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Strategies.Effects;
using ContractConfigurator;

namespace ReStrategia
{
    /// <summary>
    /// Strategy for adding periodic reputation.
    /// </summary>
    public class ReputationTicker : StrategyEffect, IMultipleEffects
    {
        /// <summary>
        /// Separate MonoBehaviour for checking, as the strategy system only gets update calls in flight.
        /// </summary>
        [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
        public class ReputationUpdater : MonoBehaviour
        {
            public static ReputationUpdater Instance;
            public List<ReputationTicker> effects = new List<ReputationTicker>();

            void Start()
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }

            public void Register(ReputationTicker effect)
            {
                effects.AddUnique(effect);
            }

            public void Unregister(ReputationTicker effect)
            {
                effects.Remove(effect);
            }

            public void Update()
            {
                foreach (ReputationTicker effect in effects)
                {
                    effect.DoCheck();
                }
            }
        }

        float reputation;
        float reputationLimit;
        float funds;
        Duration period;

        double lastCheck = 0.0;
        float reputationGiven = 0.0f;

        public ReputationTicker(Strategy parent)
            : base(parent)
        {
        }

        public IEnumerable<string> EffectText()
        {
            yield return "Costs " + funds.ToString("N0") + " Funds every " + period + ".";
            yield return "Gives " + reputation.ToString("N1") + " Reputation (to a maximum of " + reputationLimit.ToString("N1") + ") every " + period + ".";

            string clawbackString = "Reputation given is lost when the strategy is deactivated";
            if (Parent.IsActive)
            {
                clawbackString += " (currently would lose " + reputationGiven.ToString("N1") + " Reputation)";
            }
            yield return clawbackString + ".";
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                ReputationUpdater.Instance.Register(this);

                if (lastCheck == 0.0)
                {
                    lastCheck = Planetarium.GetUniversalTime();
                }
            }
        }

        protected override void OnUnregister()
        {
            ReputationUpdater.Instance.Unregister(this);

            // Check for a deactivation
            if (!Parent.IsActive && reputationGiven > 0.01)
            {
                float clawbackAmount = 0.0f;

                // Check for an upgrade/downgrade
                IEnumerable<ReputationTicker> activeEffects = StrategySystem.Instance.Strategies.Where(s => s.IsActive && s != Parent).SelectMany(s => s.Effects).OfType<ReputationTicker>();
                if (activeEffects.Any())
                {
                    // Found another active effect, should only be one
                    ReputationTicker otherEffect = activeEffects.First();

                    // Set the current reputation given
                    otherEffect.reputationGiven = Math.Min(reputationGiven, otherEffect.reputationLimit);

                    if (reputationGiven > otherEffect.reputationLimit)
                    {
                        clawbackAmount = otherEffect.reputationLimit - reputationGiven;
                    }
                }
                else
                {
                    clawbackAmount = -reputationGiven;
                }

                // Clawback of reputation
                if (clawbackAmount != 0.0f)
                {
                    Reputation.Instance.addReputation_discrete(clawbackAmount, TransactionReasons.Strategies);
                    CurrencyPopup.Instance.AddPopup(Currency.Reputation, clawbackAmount, TransactionReasons.Strategies, Parent.Config.Title + " cancellation", false);
                }
            }
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            reputation = ConfigNodeUtil.ParseValue<float>(node, "reputation");
            reputationLimit = ConfigNodeUtil.ParseValue<float>(node, "reputationLimit");
            funds = ConfigNodeUtil.ParseValue<float>(node, "funds");
            period = ConfigNodeUtil.ParseValue<Duration>(node, "period");
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.AddValue("lastCheck", lastCheck);
            node.AddValue("reputationGiven", reputationGiven);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            lastCheck = ConfigNodeUtil.ParseValue<float>(node, "lastCheck");
            reputationGiven = ConfigNodeUtil.ParseValue<float>(node, "reputationGiven");
        }
        
        public void DoCheck()
        {
            // Check if it's time to give reputation
            if (lastCheck + period.Value <= Planetarium.GetUniversalTime())
            {
                lastCheck += period.Value;

                // Give reputation
                float currentReputation = Reputation.Instance.reputation;
                if (currentReputation < reputationLimit)
                {
                    Reputation.Instance.AddReputation(reputation, TransactionReasons.Strategies);
                    if (Reputation.Instance.reputation > reputationLimit)
                    {
                        Reputation.Instance.addReputation_discrete(Reputation.Instance.reputation - reputationLimit, TransactionReasons.Strategies);
                    }
                    reputationGiven += Reputation.Instance.reputation - currentReputation;
                    CurrencyPopup.Instance.AddPopup(Currency.Reputation, reputation, TransactionReasons.Strategies, Parent.Config.Title, false);
                }

                // Take money (even if rep wasn't given)
                Funding.Instance.AddFunds(-funds, TransactionReasons.Strategies);
            }
        }
    }
}

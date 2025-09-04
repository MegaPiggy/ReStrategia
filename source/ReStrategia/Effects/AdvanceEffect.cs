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
    /// Gives an advance
    /// </summary>
    public class AdvanceEffect : StrategyEffect
    {
        Currency currency;
        double advance;

        bool isActive = false;

        public AdvanceEffect(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            return (advance > 0.0 ? "+" : "") + advance.ToString("N0") + " " + currency + " on strategy activation.";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);

            currency = ConfigNodeUtil.ParseValue<Currency>(node, "currency");
            advance = ConfigNodeUtil.ParseValue<double>(node, "advance");
        }

        public override bool CanActivate(ref string reason)
        {
            isActive = false;
            return base.CanActivate(ref reason);
        }

        protected override void OnRegister()
        {
            if (!isActive && Parent.IsActive)
            {
                if (currency == Currency.Funds)
                {
                    Funding.Instance.AddFunds(advance, TransactionReasons.StrategySetup);
                }
                else if (currency == Currency.Reputation)
                {
                    Reputation.Instance.AddReputation((float)advance, TransactionReasons.StrategySetup);
                }
                else if (currency == Currency.Science)
                {
                    ResearchAndDevelopment.Instance.AddScience((float)advance, TransactionReasons.StrategySetup);
                }

                isActive = true;
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.AddValue("isActive", isActive);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            isActive = ConfigNodeUtil.ParseValue<bool>(node, "isActive");
        }
    }
}

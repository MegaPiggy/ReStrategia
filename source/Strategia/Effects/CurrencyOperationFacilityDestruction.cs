using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Strategies;
using Strategies.Effects;
using ContractConfigurator;

namespace Strategia
{
    /// <summary>
    /// Special CurrencyOperation that gives a modifier for facility destruction.
    /// </summary>
    public class CurrencyOperationFacilityDestruction : StrategyEffect
    {
        Currency currency;
        float amount;
        string effectDescription;

        public CurrencyOperationFacilityDestruction(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            return (amount > 0.0 ? "+" : "") + amount.ToString("F1") + " " + currency + " " + effectDescription;
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);

            currency = ConfigNodeUtil.ParseValue<Currency>(node, "currency");
            effectDescription = ConfigNodeUtil.ParseValue<string>(node, "effectDescription");
            amount = ConfigNodeUtil.ParseValue<float>(node, "amount");
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                GameEvents.OnKSCStructureCollapsing.Add(new EventData<DestructibleBuilding>.OnEvent(OnKSCStructureCollapsing));
            }
        }

        protected override void OnUnregister()
        {
            GameEvents.OnKSCStructureCollapsing.Remove(new EventData<DestructibleBuilding>.OnEvent(OnKSCStructureCollapsing));
        }

        void OnKSCStructureCollapsing(DestructibleBuilding building)
        {
            Debug.Log("[Strategia] OnKSCStructureCollapsing: " + building);

            if (currency == Currency.Funds)
            {
                Funding.Instance.AddFunds(amount, TransactionReasons.Strategies);
            }
            else if (currency == Currency.Reputation)
            {
                Reputation.Instance.AddReputation(amount, TransactionReasons.Strategies);
            }
            else if (currency == Currency.Science)
            {
                ResearchAndDevelopment.Instance.AddScience(amount, TransactionReasons.Strategies);
            }

            CurrencyPopup.Instance.AddPopup(currency, amount, TransactionReasons.StructureCollapse, Parent.Config.Title, building.FxTarget, true);
        }
    }
}

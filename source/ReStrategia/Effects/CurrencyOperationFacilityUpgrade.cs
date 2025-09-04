using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Strategies.Effects;

namespace ReStrategia
{
    /// <summary>
    /// Special CurrencyOperation only for facility upgrades
    /// </summary>
    public class CurrencyOperationFacilityUpgrade : CurrencyOperation
    {
        float fundsDelta;
        float reputationDelta;
        float scienceDelta;

        public CurrencyOperationFacilityUpgrade(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            node.AddValue("effectDescription", "on facility upgrades");
            node.AddValue("AffectReasons", "StructureConstruction");

            base.OnLoadFromConfig(node);
        }

        protected override void OnRegister()
        {
            base.OnRegister();

            if (Parent.IsActive)
            {
                GameEvents.Modifiers.OnCurrencyModified.Add(new EventData<CurrencyModifierQuery>.OnEvent(OnCurrencyModified));
            }
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            GameEvents.Modifiers.OnCurrencyModified.Remove(new EventData<CurrencyModifierQuery>.OnEvent(OnCurrencyModified));
        }

        protected override void OnEffectQuery(CurrencyModifierQuery qry)
        {
            if (qry.reason != TransactionReasons.StructureConstruction)
            {
                return;
            }

            fundsDelta = qry.GetEffectDelta(Currency.Funds);
            reputationDelta = qry.GetEffectDelta(Currency.Reputation);
            scienceDelta = qry.GetEffectDelta(Currency.Science);

            base.OnEffectQuery(qry);

            // Calculate any changes
            fundsDelta = qry.GetEffectDelta(Currency.Funds) - fundsDelta;
            reputationDelta = qry.GetEffectDelta(Currency.Reputation) - reputationDelta;
            scienceDelta = qry.GetEffectDelta(Currency.Science) - scienceDelta;
        }

        void OnCurrencyModified(CurrencyModifierQuery qry)
        {
            if (qry.reason == TransactionReasons.StructureConstruction)
            {
                // Check for changes
                if (Math.Abs(fundsDelta) > 0.01)
                {
                    CurrencyPopup.Instance.AddFacilityPopup(Currency.Funds, fundsDelta, qry.reason, Parent.Config.Title, true);
                }
                if (Math.Abs(reputationDelta) > 0.01)
                {
                    CurrencyPopup.Instance.AddFacilityPopup(Currency.Reputation, reputationDelta, qry.reason, Parent.Config.Title, true);
                }
                if (Math.Abs(scienceDelta) > 0.01)
                {
                    CurrencyPopup.Instance.AddFacilityPopup(Currency.Science, scienceDelta, qry.reason, Parent.Config.Title, true);
                }
            }
        }
    }
}

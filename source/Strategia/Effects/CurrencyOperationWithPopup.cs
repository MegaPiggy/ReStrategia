using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Strategies.Effects;

namespace Strategia
{
    /// <summary>
    /// Special CurrencyOperation with a currency popup.
    /// </summary>
    public class CurrencyOperationWithPopup : CurrencyOperation
    {
        float fundsDelta;
        float reputationDelta;
        float scienceDelta;

        public CurrencyOperationWithPopup(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
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
            fundsDelta = 0.0f;
            reputationDelta = 0.0f;
            scienceDelta = 0.0f;

            // Check if it's non-zero
            if (Math.Abs(qry.GetInput(Currency.Funds)) < 0.01 && Math.Abs(qry.GetInput(Currency.Science)) < 0.01 && Math.Abs(qry.GetInput(Currency.Reputation)) < 0.01)
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

        private void OnCurrencyModified(CurrencyModifierQuery qry)
        {
            // Check for changes
            if (Math.Abs(fundsDelta) > 0.01)
            {
                CurrencyPopup.Instance.AddPopup(Currency.Funds, fundsDelta, qry.reason, Parent.Config.Title, true);
            }
            if (Math.Abs(reputationDelta) > 0.01)
            {
                CurrencyPopup.Instance.AddPopup(Currency.Reputation, reputationDelta, qry.reason, Parent.Config.Title, true);
            }
            if (Math.Abs(scienceDelta) > 0.01)
            {
                CurrencyPopup.Instance.AddPopup(Currency.Science, scienceDelta, qry.reason, Parent.Config.Title, true);
            }
        }
    }
}

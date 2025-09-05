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

namespace ReStrategia
{
    /// <summary>
    /// Special CurrencyOperation that gives a modifier for specific trait.
    /// </summary>
    public class CurrencyOperationByTrait : StrategyEffect
    {
        List<Currency> currencies;
        string effectDescription;
        List<TransactionReasons> affectReasons;
        List<float> multipliers;
        string trait;

        private Vessel cachedVessel;
        private float cacheTime;

        public CurrencyOperationByTrait(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            float multiplier = Parent.GetLeveledListItem(multipliers);
            string multiplierStr = ToPercentage(multiplier);

            string currencyStr = currencies.Count() > 1 ? "" : (currencies.First() + " ");

            return multiplierStr + " " + currencyStr + effectDescription +
                " when " + StringUtil.ATrait(trait) + " is present.";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);

            currencies = ConfigNodeUtil.ParseValue<List<Currency>>(node, "currency");
            effectDescription = ConfigNodeUtil.ParseValue<string>(node, "effectDescription");
            affectReasons = ConfigNodeUtil.ParseValue<List<TransactionReasons>>(node, "AffectReason");
            multipliers = ConfigNodeUtil.ParseValue<List<float>>(node, "multiplier");
            trait = ConfigNodeUtil.ParseValue<string>(node, "trait");
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                GameEvents.Modifiers.OnCurrencyModifierQuery.Add(new EventData<CurrencyModifierQuery>.OnEvent(OnEffectQuery));
                GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel, bool>.OnEvent(OnVesselRecovered));
                GameEvents.onGameSceneLoadRequested.Add(new EventData<GameScenes>.OnEvent(OnGameSceneLoadRequested));
            }
        }

        protected override void OnUnregister()
        {
            GameEvents.Modifiers.OnCurrencyModifierQuery.Remove(new EventData<CurrencyModifierQuery>.OnEvent(OnEffectQuery));
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel, bool>.OnEvent(OnVesselRecovered));
            GameEvents.onGameSceneLoadRequested.Remove(new EventData<GameScenes>.OnEvent(OnGameSceneLoadRequested));
        }

        private void OnGameSceneLoadRequested(GameScenes scene)
        {
            cachedVessel = null;
        }

        private void OnVesselRecovered(ProtoVessel vessel, bool quick)
        {
            cachedVessel = vessel.vesselRef;
            cacheTime = Time.fixedTime;
        }

        private void OnEffectQuery(CurrencyModifierQuery qry)
        {
            // Check the reason is a match
            if (!affectReasons.Contains(qry.reason))
            {
                return;
            }

            // Check if it's non-zero
            float total = 0.0f;
            foreach (Currency currency in currencies)
            {
                total += Math.Abs(qry.GetInput(currency));
            }
            if (total < 0.01f)
            {
                return;
            }

            // Figure out the vessel to look at
            Vessel vessel = null;
            if (FlightGlobals.ActiveVessel != null)
            {
                vessel = FlightGlobals.ActiveVessel;
            }
            else if (cachedVessel != null && cacheTime < Time.fixedTime + 5.0f)
            {
                vessel = cachedVessel;
            }

            // Check for matching crew
            if (vessel != null)
            {
                bool crewFound = false;
                foreach (ProtoCrewMember pcm in VesselUtil.GetVesselCrew(vessel))
                {
                    if (pcm.experienceTrait.Config.Name == trait)
                    {
                        crewFound = true;
                        break;
                    }
                }
                if (!crewFound)
                {
                    return;
                }
            }

            float multiplier = Parent.GetLeveledListItem(multipliers);
            foreach (Currency currency in currencies)
            {
                qry.AddDelta(currency, multiplier * qry.GetInput(currency) - qry.GetInput(currency));
            }
        }
    }
}

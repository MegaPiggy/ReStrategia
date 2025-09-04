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
    /// Strategy for awarding a bonus 
    /// </summary>
    public class OrbitWeightBonus : StrategyEffect, IMultipleEffects
    {
        double funds;
        float science;
        float reputation;
        double mass;

        Vessel validVessel = null;

        public OrbitWeightBonus(Strategy parent)
            : base(parent)
        {
        }

        public IEnumerable<string> EffectText()
        {
            if (funds > 0.0)
            {
                yield return funds.ToString("N0") + " funds when reaching orbit with a vessel with a mass of at least " + mass.ToString("N1") + " tons.";
            }
            if (reputation > 0.0)
            {
                yield return reputation.ToString("N0") + " reputation when reaching orbit with a vessel with a mass of at least " + mass.ToString("N1") + " tons.";
            }
            if (science > 0.0)
            {
                yield return science.ToString("N0") + " science when reaching orbit with a vessel with a mass of at least " + mass.ToString("N1") + " tons.";
            }
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            funds = ConfigNodeUtil.ParseValue<double>(node, "funds", 0.0);
            science = ConfigNodeUtil.ParseValue<float>(node, "science", 0.0f);
            reputation = ConfigNodeUtil.ParseValue<float>(node, "reputation", 0.0f);
            mass = ConfigNodeUtil.ParseValue<double>(node, "mass");
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                GameEvents.onVesselSituationChange.Add(new EventData<GameEvents.HostedFromToAction<Vessel, Vessel.Situations>>.OnEvent(OnVesselSituationChange));
            }
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselSituationChange.Remove(new EventData<GameEvents.HostedFromToAction<Vessel, Vessel.Situations>>.OnEvent(OnVesselSituationChange));
        }

        protected override void OnLoad(ConfigNode node)
        {
            validVessel = ConfigNodeUtil.ParseValue<Vessel>(node, "validVessel", null);
        }

        protected override void OnSave(ConfigNode node)
        {
            if (validVessel != null)
            {
                node.AddValue("validVessel", validVessel.id);
            }
        }

        private void OnVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> hfta)
        {
            if (hfta.from == Vessel.Situations.PRELAUNCH || (hfta.from == Vessel.Situations.LANDED && hfta.host.mainBody.isHomeWorld))
            {
                validVessel = hfta.host;
            }

            if (hfta.to == Vessel.Situations.ORBITING)
            {
                if (validVessel == hfta.host)
                {
                    HandleVessel(hfta.host);
                }
                validVessel = null;
            }
        }

        private void HandleVessel(Vessel vessel)
        {
            Debug.Log("[ReStrategia] OrbitWeightBonus.HandleVessel");

            // Check weight limit
            if (vessel.totalMass < mass)
            {
                return;
            }

            // Add the funds (or whatever)
            if (funds > 0.0)
            {
                Funding.Instance.AddFunds(funds, TransactionReasons.Strategies);
                CurrencyPopup.Instance.AddPopup(Currency.Funds, funds, TransactionReasons.Strategies, Parent.Config.Title, false);
            }
            else if (reputation > 0.0f)
            {
                Reputation.Instance.AddReputation(reputation, TransactionReasons.Strategies);
                CurrencyPopup.Instance.AddPopup(Currency.Reputation, reputation, TransactionReasons.Strategies, Parent.Config.Title, false);
            }
            else if (science > 0.0f)
            {
                ResearchAndDevelopment.Instance.AddScience(science, TransactionReasons.Strategies);
                CurrencyPopup.Instance.AddPopup(Currency.Science, science, TransactionReasons.Strategies, Parent.Config.Title, false);
            }
        }
    }
}

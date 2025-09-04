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
using ContractConfigurator.Util;

namespace ReStrategia
{
    /// <summary>
    /// Strategy effect for boosting science for certain conditions. 
    /// </summary>
    public class ScienceBooster : StrategyEffect, IMultipleEffects
    {
        float KSCScienceMultiplier;
        float nonKSCScienceMultiplier;

        public ScienceBooster(Strategy parent)
            : base(parent)
        {
        }

        public IEnumerable<string> EffectText()
        {
            if (KSCScienceMultiplier > 0.0)
            {
                yield return ToPercentage(KSCScienceMultiplier, "N0") + " bonus to KSC Science.";
            }
            if (nonKSCScienceMultiplier > 0.0)
            {
                CelestialBody home = FlightGlobals.Bodies.First(cb => cb.isHomeWorld);
                yield return ToPercentage(nonKSCScienceMultiplier, "N0") + " bonus to " + home.CleanDisplayName(true) + " Science.";
            }
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            KSCScienceMultiplier = ConfigNodeUtil.ParseValue<float>(node, "KSCScienceMultiplier", 0.0f);
            nonKSCScienceMultiplier = ConfigNodeUtil.ParseValue<float>(node, "nonKSCScienceMultiplier", 0.0f);
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                GameEvents.OnScienceRecieved.Add(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
            }
        }

        protected override void OnUnregister()
        {
            GameEvents.OnScienceRecieved.Remove(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
        }

        private void OnScienceReceived(float amount, ScienceSubject subject, ProtoVessel vessel, bool reverseEngineered)
        {
            // Check that the science is for home
            CelestialBody body = Science.GetCelestialBody(subject);
            if (body == null || !body.isHomeWorld)
            {
                return;
            }

            Biome biome = Science.GetBiome(subject);
            bool isKSC = biome != null && biome.IsKSC();
            if (KSCScienceMultiplier > 0.0f && isKSC)
            {
                float delta = KSCScienceMultiplier * amount - amount;
                ResearchAndDevelopment.Instance.AddScience(delta, TransactionReasons.Strategies);
                CurrencyPopup.Instance.AddPopup(Currency.Science, delta, TransactionReasons.Strategies, Parent.Config.Title, true);
            }
            else if (nonKSCScienceMultiplier > 0.0f && !isKSC)
            {
                float delta = nonKSCScienceMultiplier * amount - amount;
                ResearchAndDevelopment.Instance.AddScience(delta, TransactionReasons.Strategies);
                CurrencyPopup.Instance.AddPopup(Currency.Science, delta, TransactionReasons.Strategies, Parent.Config.Title, true);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Strategies;
using Strategies.Effects;
using ContractConfigurator;

namespace ReStrategia
{
    public class ContractWeightBooster : StrategyEffect
    {
        private List<CelestialBody> bodies;
        private bool bonusGiven = false;
        private int weight;

        public ContractWeightBooster(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            return "Increases likelihood of receiving contracts for " + CelestialBodyUtil.BodyList(bodies.Where(CelestialBodyUtil.IsNotBarycenter), "and") + ".";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);

            bodies = ConfigNodeUtil.ParseValue<List<CelestialBody>>(node, "bodies");
            weight = ConfigNodeUtil.ParseValue<int>(node, "weight");
        }

        protected override void OnRegister()
        {
            // Check for an activation
            if (Parent.IsActive && !bonusGiven)
            {
                bonusGiven = true;
                foreach (CelestialBody body in bodies)
                {
                    Contracts.ContractSystem.WeightAdjustment(body.name, weight);
                }
            }
        }

        protected override void OnUnregister()
        {
            // Check for a deactivation
            if (!Parent.IsActive)
            {
                foreach (CelestialBody body in bodies)
                {
                    Contracts.ContractSystem.WeightAdjustment(body.name, -weight);
                }
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.AddValue("bonusGiven", bonusGiven);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            bonusGiven = ConfigNodeUtil.ParseValue<bool>(node, "bonusGiven");
        }
    }
}

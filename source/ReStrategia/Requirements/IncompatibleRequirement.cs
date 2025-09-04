using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Strategies;
using Strategies.Effects;
using ContractConfigurator;

namespace ReStrategia
{
    public class IncompatibleRequirement : StrategyEffect, IRequirementEffect
    {
        string strategy;
        public IncompatibleRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            strategy = ConfigNodeUtil.ParseValue<string>(node, "strategy");
        }

        public string RequirementText()
        {
            return strategy  + " cannot be active";
        }

        public bool RequirementMet(out string unmetReason)
        {
            Strategy conflict = StrategySystem.Instance.Strategies.FirstOrDefault(s => s.Title.StartsWith(strategy) && s.IsActive);
            unmetReason = conflict != null ? (conflict.Title + " is active") : null;
            return conflict == null;
        }
    }
}

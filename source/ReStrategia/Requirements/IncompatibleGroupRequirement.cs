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
    public class IncompatibleGroupRequirement : StrategyEffect, IRequirementEffect
    {
        string group;
        string text;
        public IncompatibleGroupRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            group = ConfigNodeUtil.ParseValue<string>(node, "group");
            text = ConfigNodeUtil.ParseValue<string>(node, "text");
        }

        public string RequirementText()
        {
            return text;
        }

        public bool RequirementMet(out string unmetReason)
        {
            Strategy conflict = StrategySystem.Instance.Strategies.Where(s => s.Title != Parent.Title && s.GroupTags.First() == group && s.IsActive).FirstOrDefault();
            unmetReason = conflict != null ? (conflict.Title + " is active") : null;
            return conflict == null;
        }
    }
}

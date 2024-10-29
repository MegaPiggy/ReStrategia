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
using ContractConfigurator.Util;

namespace Strategia
{
    public class ResearchBodiesRequirement : StrategyEffect, IRequirementEffect
    {
        private IEnumerable<CelestialBody> bodies;
        private string id;
        public bool invert;

        public ResearchBodiesRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            id = ConfigNodeUtil.ParseValue<string>(node, "id", "");
            if (!string.IsNullOrEmpty(id))
            {
                bodies = CelestialBodyUtil.GetBodiesForStrategy(id);
            }
            else if (node.HasValue("body"))
            {
                bodies = ConfigNodeUtil.ParseValue<List<CelestialBody>>(node, "body");
            }
            else
            {
                bodies = FlightGlobals.Bodies.Where(cb => cb.isHomeWorld);
            }
            invert = ConfigNodeUtil.ParseValue<bool?>(node, "invert", (bool?)false).Value;
        }

        public string RequirementText()
        {
            return "Must " + (invert ? "not " : "") + "have researched " + CelestialBodyUtil.BodyList(bodies, "and");
        }

        public bool RequirementMet(out string unmetReason)
        {
            unmetReason = null;
            return invert ^ Check();
        }

        protected bool Check()
        {
            if (ContractConfigurator.Util.Version.VerifyResearchBodiesVersion())
            {
                LoggingUtil.LogVerbose(this, "ResearchBodies check for strategy " + Parent.Config.Title);

                // Check each body that the contract references
                Dictionary<CelestialBody, RBWrapper.CelestialBodyInfo> bodyInfoDict = RBWrapper.RBactualAPI.CelestialBodies;
                foreach (CelestialBody body in bodies)
                {
                    if (bodyInfoDict.ContainsKey(body) && !body.isHomeWorld)
                    {
                        RBWrapper.CelestialBodyInfo bodyInfo = bodyInfoDict[body];
                        LoggingUtil.LogVerbose(this, "    check body {0} = {1}", body, bodyInfo.isResearched);
                        if (!bodyInfo.isResearched)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}

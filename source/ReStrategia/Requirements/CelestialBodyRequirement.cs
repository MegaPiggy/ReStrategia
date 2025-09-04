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
    public abstract class CelestialBodyRequirement : StrategyEffect, IRequirementEffect
    {
        private IEnumerable<CelestialBody> bodies;
        private string id;
        public bool invert;

        public CelestialBodyRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            id = ConfigNodeUtil.ParseValue<string>(node, "id", "");
            if (!string.IsNullOrEmpty(id))
            {
                bodies = CelestialBodyUtil.GetDistinctBodiesForStrategy(id);
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
            return "Must " + (invert ? "not " : "") + "have " + Verbed() + " " + CelestialBodyUtil.BodyList(bodies, "or");
        }

        public bool RequirementMet(out string unmetReason)
        {
            unmetReason = null;
            return invert ^ ProgressTracking.Instance.celestialBodyNodes.Where(node => bodies.Contains(node.Body)).Any(cbs => Check(cbs));
        }

        protected abstract bool Check(CelestialBodySubtree cbs);
        protected abstract string Verbed();
    }

    public class ReachedBodyRequirement : CelestialBodyRequirement
    {
        public ReachedBodyRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.IsReached;
        }

        protected override string Verbed()
        {
            return "reached";
        }
    }

    public class OrbitBodyRequirement : CelestialBodyRequirement
    {
        public OrbitBodyRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.orbit.IsReached;
        }

        protected override string Verbed()
        {
            return "orbited";
        }
    }

    public class LandedBodyRequirement : CelestialBodyRequirement
    {
        public LandedBodyRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.landing.IsReached;
        }

        protected override string Verbed()
        {
            return "landed on";
        }
    }

    public class ReturnFromOrbitRequirement : CelestialBodyRequirement
    {
        public ReturnFromOrbitRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.returnFromOrbit.IsReached;
        }

        protected override string Verbed()
        {
            return "returned from orbit of";
        }
    }

    public class ReturnFromSurfaceRequirement : CelestialBodyRequirement
    {
        public ReturnFromSurfaceRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.returnFromSurface.IsReached;
        }

        protected override string Verbed()
        {
            return "returned from the surface of";
        }
    }

    public class ReachedBodyMannedRequirement : CelestialBodyRequirement
    {
        public ReachedBodyMannedRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.flyBy.IsReached && cbs.flyBy.IsCompleteManned;
        }

        protected override string Verbed()
        {
            return "performed a crewed fly-by of";
        }
    }

    public class OrbitBodyMannedRequirement : CelestialBodyRequirement
    {
        public OrbitBodyMannedRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.orbit.IsReached && cbs.orbit.IsCompleteManned;
        }

        protected override string Verbed()
        {
            return "orbited with a crew around";
        }
    }

    public class LandedBodyMannedRequirement : CelestialBodyRequirement
    {
        public LandedBodyMannedRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.landing.IsReached && cbs.landing.IsCompleteManned;
        }

        protected override string Verbed()
        {
            return "landed a crew on";
        }
    }

    public class ReturnFromOrbitMannedRequirement : CelestialBodyRequirement
    {
        public ReturnFromOrbitMannedRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            return cbs.returnFromOrbit.IsReached && cbs.returnFromOrbit.IsCompleteManned;
        }

        protected override string Verbed()
        {
            return "returned a crew from orbit of";
        }
    }

    public class ReturnFromSurfaceMannedRequirement : CelestialBodyRequirement
    {
        public ReturnFromSurfaceMannedRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs)
        {
            if (cbs.returnFromSurface.IsReached && cbs.returnFromSurface.IsCompleteManned)
            {
                return true;
            }

            // Check if a Kerbal has returned from the surface, and consider that good enough
            return HighLogic.CurrentGame.CrewRoster.Crew.Any(pcm => pcm.careerLog.HasEntry(FlightLog.EntryType.Land, cbs.Body.name));
        }

        protected override string Verbed()
        {
            return "returned a crew from the surface of";
        }
    }
}

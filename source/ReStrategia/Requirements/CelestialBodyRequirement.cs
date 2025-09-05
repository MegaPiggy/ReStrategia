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
            return "Must " + (invert ? "not " : "") + "have " + Verbed() + " " + CelestialBodyUtil.BodyList(bodies.Where(CelestialBodyUtil.IsNotBarycenter), "or");
        }

        public bool RequirementMet(out string unmetReason)
        {
            unmetReason = null;
            foreach (var node in ProgressTracking.Instance.celestialBodyNodes.Where(n => bodies.Contains(n.Body)))
            {
                if (Check(node, ref unmetReason))
                {
                    if (DefaultInvertedUnmetReason && invert && string.IsNullOrEmpty(unmetReason))
                    {
                        unmetReason = $"Have {Verbed()} {node.Body.name}";
                    }
                    return invert ? false : true;
                }
            }

            if (DefaultUnmetReason && !invert && string.IsNullOrEmpty(unmetReason))
            {
                unmetReason = $"Haven't {Verbed()} {CelestialBodyUtil.BodyList(bodies, "or")}";
            }
            return invert;
        }

        protected virtual bool DefaultUnmetReason => false;
        protected virtual bool DefaultInvertedUnmetReason => true;

        protected abstract bool Check(CelestialBodySubtree cbs, ref string unmetReason);
        protected abstract string Verbed();
    }

    public class ReachedBodyRequirement : CelestialBodyRequirement
    {
        public ReachedBodyRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override bool Check(CelestialBodySubtree cbs, ref string unmetReason)
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

        protected override bool Check(CelestialBodySubtree cbs, ref string unmetReason)
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

        protected override bool Check(CelestialBodySubtree cbs, ref string unmetReason)
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

        protected override bool Check(CelestialBodySubtree cbs, ref string unmetReason)
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

        protected override bool Check(CelestialBodySubtree cbs, ref string unmetReason)
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

        protected override bool Check(CelestialBodySubtree cbs, ref string unmetReason)
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

        protected override bool Check(CelestialBodySubtree cbs, ref string unmetReason)
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

        protected override bool Check(CelestialBodySubtree cbs, ref string unmetReason)
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

        protected override bool Check(CelestialBodySubtree cbs, ref string unmetReason)
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

        protected override bool Check(CelestialBodySubtree cbs, ref string unmetReason)
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

    public class VesselEnrouteRequirement : CelestialBodyRequirement
    {
        // Use 2.5 billion meters as the distance threshold (about 50 Duna SOIs)
        const double distanceLimit = 2500000000;

        public bool? manned;

        public VesselEnrouteRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);
            manned = ConfigNodeUtil.ParseValue<bool?>(node, "manned", null);
        }

        protected string MannedString => manned == null ? "" : manned.Value ? "crewed " : "uncrewed ";

        protected override bool Check(CelestialBodySubtree cbs, ref string unmetReason)
        {
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                // Crew check
                if (manned != null)
                {
                    if (manned.Value && vessel.GetCrewCount() == 0) continue;
                    if (!manned.Value && vessel.GetCrewCount() > 0) continue;
                }

                if (VesselIsEnroute(cbs.Body, vessel))
                {
                    if (invert)
                    {
                        unmetReason = $"{vessel.vesselName} is en route to {cbs.Body.CleanDisplayName(true)}"; // TODO: change so it doesn't get the name of sigma barycenters
                    }
                    return true;
                }
            }

            if (!invert)
            {
                string mannedStr = MannedString;
                unmetReason = $"No {mannedStr}vessels are en route to {cbs.Body.CleanDisplayName(true)}";
            }
            return false;
        }

        protected override string Verbed()
        {
            string mannedStr = MannedString;
            return (invert ? "any " + mannedStr + "vessels" : "a " + mannedStr + "vessel") + " en route to";
        }

        protected bool VesselIsEnroute(CelestialBody body, Vessel vessel)
        {
            // Only check when in orbit of a system root
            if (!CelestialBodyUtil.IsSystemRoot(vessel.mainBody))
            {
                return false;
            }

            // Ignore escaping or other silly things
            if (vessel.situation != Vessel.Situations.ORBITING)
            {
                return false;
            }

            // Asteroids?  No...
            if (vessel.vesselType == VesselType.SpaceObject || vessel.vesselType == VesselType.Debris)
            {
                return false;
            }

            // Check the orbit
            Orbit vesselOrbit = vessel.loaded ? vessel.orbit : vessel.protoVessel.orbitSnapShot.Load();
            Orbit bodyOrbit = body.orbit;
            double minUT = Planetarium.GetUniversalTime();
            double maxUT = minUT + vesselOrbit.period;
            double UT = (maxUT - minUT) / 2.0;
            int iterations = 0;
            double distance = Orbit.SolveClosestApproach(vesselOrbit, bodyOrbit, ref UT, (maxUT - minUT) * 0.3, 0.0, minUT, maxUT, 0.1, 50, ref iterations);

            return distance > 0 && distance < distanceLimit;
        }
    }
}

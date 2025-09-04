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
    public class VesselEnrouteRequirement : StrategyEffect, IRequirementEffect
    {
        // Use 2.5 billion meters as the distance threshold (about 50 Duna SOIs)
        const double distanceLimit = 2500000000;

        CelestialBody body;
        public bool invert;
        public bool? manned;

        public VesselEnrouteRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            body = ConfigNodeUtil.ParseValue<CelestialBody>(node, "body");
            invert = ConfigNodeUtil.ParseValue<bool?>(node, "invert", (bool?)false).Value;
            manned = ConfigNodeUtil.ParseValue<bool?>(node, "manned", null);
        }

        public string RequirementText()
        {
            string mannedStr = manned == null ? "" : manned.Value ? "crewed " : "uncrewed ";
            return "Must " + (invert ? "not have any " + mannedStr + "vessels" : "have a " + mannedStr + "vessel") + " en route to " + body.CleanDisplayName(true);
        }

        public bool RequirementMet(out string unmetReason)
        {
            unmetReason = null;

            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (manned != null)
                {
                    if (manned.Value && vessel.GetCrewCount() == 0 ||
                        !manned.Value && vessel.GetCrewCount() > 0)
                    {
                        continue;
                    }
                }

                bool enRoute = VesselIsEnroute(vessel);
                if (enRoute && invert)
                {
                    unmetReason = vessel.vesselName + " is en route to " + body.CleanDisplayName(true);
                    return false;
                }
                else if (enRoute && !invert)
                {
                    return true;
                }
            }

            if (invert)
            {
                return true;
            }
            else
            {
                unmetReason = "No vessels are en route to " + body.CleanDisplayName(true);
                return false;
            }
        }

        protected bool VesselIsEnroute(Vessel vessel)
        {
            // Only check when in orbit of the sun
            if (vessel.mainBody != FlightGlobals.Bodies[0])
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

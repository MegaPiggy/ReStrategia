using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Strategies.Effects;

namespace Strategia
{
    /// <summary>
    /// Strategy for giving an extra experience level for media stars.
    /// </summary>
    public class MediaStar : StrategyEffect
    {
        public const string MEDIA_STAR_XP = "MediaStar";

        /// <summary>
        /// Static initializer to hack the kerbal experience/flight log system to add our entries.
        /// </summary>
        static MediaStar()
        {
            Debug.Log("[Strategia] Setting up Media Star Experience");

            KerbalRoster.AddExperienceType(MEDIA_STAR_XP, "Media star from", 3.5f);
        }

        public MediaStar(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            return "Kerbals returning from the surface of other bodies get bonus experience";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel, bool>.OnEvent(OnVesselRecovered));
            }
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel, bool>.OnEvent(OnVesselRecovered));
        }

        private void OnVesselRecovered(ProtoVessel vessel, bool quick)
        {
            foreach (ProtoCrewMember pcm in VesselUtil.GetVesselCrew(vessel.vesselRef))
            {
                // Award the media star XP for each planet landed on
                foreach (string target in pcm.flightLog.Entries.
                    Where(fle => fle.type == FlightLog.EntryType.Land.ToString()).
                    Select(fle => fle.target).ToList())
                {
                    pcm.flightLog.AddEntry(MEDIA_STAR_XP, target);
                }
            }
        }
    }
}

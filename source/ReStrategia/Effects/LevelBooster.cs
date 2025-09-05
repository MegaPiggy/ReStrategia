using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using KSP.UI.Screens.Flight;
using Strategies;
using Strategies.Effects;
using ContractConfigurator;

namespace ReStrategia
{
    /// <summary>
    /// Strategy for increasing the a Kerbal's level while the strategy is active.
    /// </summary>
    public class LevelBooster : StrategyEffect
    {
        List<int> levels;
        ProtoCrewMember.Gender? gender;
        string trait;

        public LevelBooster(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            string genderStr = gender != null ? gender.Value.ToString().ToLower() + " " : "";
            string astronautStr = string.IsNullOrEmpty(trait) ? "astronauts" : (trait.ToLower() + "s");

            int level = Parent.GetLeveledListItem<int>(levels);
            return "All " + genderStr + astronautStr + " perform as if they had " + level + " more level" + (level == 1 ? "" : "s") + " of experience, up to a maximum of 5.";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            levels = ConfigNodeUtil.ParseValue<List<int>>(node, "level");
            gender = ConfigNodeUtil.ParseValue<ProtoCrewMember.Gender?>(node, "gender", null);
            trait = ConfigNodeUtil.ParseValue<string>(node, "trait");
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChange));
                GameEvents.onFlightReady.Add(new EventVoid.OnEvent(OnFlightReady));
                GameEvents.onKerbalLevelUp.Add(new EventData<ProtoCrewMember>.OnEvent(OnKerbalLevelUp));
            }
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onFlightReady.Remove(new EventVoid.OnEvent(OnFlightReady));
            GameEvents.onKerbalLevelUp.Remove(new EventData<ProtoCrewMember>.OnEvent(OnKerbalLevelUp));
        }

        private void OnFlightReady()
        {
            HandleVessel(FlightGlobals.ActiveVessel);
        }

        private void OnVesselChange(Vessel vessel)
        {
            HandleVessel(vessel);
        }

        private void OnKerbalLevelUp(ProtoCrewMember pcm)
        {
            // If in flight, assume a level up means that our crew is to be handled by this strategy
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                HandleCrew(pcm);
            }
        }

        private void HandleVessel(Vessel vessel)
        {
            // Update the level for all crew in the vessel
            foreach (ProtoCrewMember pcm in VesselUtil.GetVesselCrew(vessel))
            {
                HandleCrew(pcm);
            }
        }

        private void HandleCrew(ProtoCrewMember pcm)
        {
            // Get the level
            int level = Parent.GetLeveledListItem<int>(levels);

            if (string.IsNullOrEmpty(trait) || pcm.experienceTrait.Config.Name == trait &&
                gender == null || pcm.gender == gender)
            {
                // Crew portraits break down if they have to display more than five stars, complicating EVA immensely.
                // To prevent this, we have to limit the total level to 5.
                pcm.experienceLevel = Math.Min(KerbalRoster.CalculateExperienceLevel(pcm.experience) + level, 5);

                // Force an update of the portrait, if present
                if (pcm.KerbalRef != null)
                {
                    KerbalPortraitGallery.Instance.UpdatePortrait(pcm.KerbalRef);
                }
            }
        }
    }
}

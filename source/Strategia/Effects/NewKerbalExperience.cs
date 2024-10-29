using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using KSP.UI.Screens;
using Strategies;
using Strategies.Effects;
using ContractConfigurator;

namespace Strategia
{
    /// <summary>
    /// Strategy for giving experience to new hires.
    /// </summary>
    public class NewKerbalExperience : StrategyEffect
    {
        public const string SPECIAL_XP = "SpecialTraining";

        /// <summary>
        /// Static initializer to hack the kerbal experience/flight log system to add our entries.
        /// </summary>
        static NewKerbalExperience()
        {
            Debug.Log("Strategia: Setting up Kerbal Experience");
            KerbalRoster.AddExperienceType(SPECIAL_XP + "1", "Special training", 0.0f, 2.0f);
            KerbalRoster.AddExperienceType(SPECIAL_XP + "2", "Special training", 0.0f, 8.0f);
            KerbalRoster.AddExperienceType(SPECIAL_XP + "3", "Special training", 0.0f, 16.0f);
            KerbalRoster.AddExperienceType(SPECIAL_XP + "4", "Special training", 0.0f, 32.0f);
            KerbalRoster.AddExperienceType(SPECIAL_XP + "5", "Special training", 0.0f, 64.0f);
        }

        int level;
        ProtoCrewMember.Gender? gender;
        string trait;

        public NewKerbalExperience(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            string genderStr = gender != null ? gender.Value.ToString().ToLower() + " " : "";
            string astronautStr = string.IsNullOrEmpty(trait) ? "astronauts" : (trait.ToLower() + "s");

            return "Hired " + genderStr + astronautStr + " start at level " + level + ".";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            level = ConfigNodeUtil.ParseValue<int>(node, "level", 0);
            gender = ConfigNodeUtil.ParseValue<ProtoCrewMember.Gender?>(node, "gender", null);
            trait = ConfigNodeUtil.ParseValue<string>(node, "trait", (string)null);
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                GameEvents.onKerbalTypeChange.Add(new EventData<ProtoCrewMember, ProtoCrewMember.KerbalType, ProtoCrewMember.KerbalType>.OnEvent(OnKerbalTypeChange));
            }
        }

        protected override void OnUnregister()
        {
            GameEvents.onKerbalTypeChange.Remove(new EventData<ProtoCrewMember, ProtoCrewMember.KerbalType, ProtoCrewMember.KerbalType>.OnEvent(OnKerbalTypeChange));
        }

        private void OnKerbalTypeChange(ProtoCrewMember pcm, ProtoCrewMember.KerbalType oldType, ProtoCrewMember.KerbalType newType)
        {
            if (oldType == ProtoCrewMember.KerbalType.Applicant && newType == ProtoCrewMember.KerbalType.Crew)
            {
                // Check for correct trait
                if (!string.IsNullOrEmpty(trait) && pcm.experienceTrait.Config.Name != trait)
                {
                    return;
                }

                // Check for correct gender
                if (gender != null && pcm.gender != gender.Value)
                {
                    return;
                }

                CelestialBody homeworld = FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).FirstOrDefault();

                Debug.Log("Strategia: Awarding experience to " + pcm.name);

                // Find existing entries
                int currentValue = 2;
                foreach (FlightLog.Entry entry in pcm.careerLog.Entries.Concat(pcm.flightLog.Entries).Where(e => e.type.Contains(SPECIAL_XP)))
                {
                    // Get the entry with the largest value
                    int entryValue = Convert.ToInt32(entry.type.Substring(SPECIAL_XP.Length, entry.type.Length - SPECIAL_XP.Length));
                    currentValue = Math.Max(currentValue, entryValue);
                }

                // Get the experience level
                int value = level;
                string type = SPECIAL_XP + value.ToString();

                // Do the awarding
                pcm.flightLog.AddEntry(type, homeworld.name);
                pcm.ArchiveFlightLog();

                // Force the astronaut complex GUI to refresh so we actually see the experience
                AstronautComplex ac = UnityEngine.Object.FindObjectOfType<AstronautComplex>();
                if (ac != null)
                {
                    MethodInfo updateListMethod = typeof(AstronautComplex).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                        Where(mi => mi.Name == "CreateAvailableList").First();
                    updateListMethod.Invoke(ac, new object[] { });

                    MethodInfo addToListMethod = typeof(AstronautComplex).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                        Where(mi => mi.Name == "AddItem_Available").First();
                    addToListMethod.Invoke(ac, new object[] { pcm });
                }
            }
        }
    }
}

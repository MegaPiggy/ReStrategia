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
    public class MinimumFacilityLevelRequirement : StrategyEffect, IRequirementEffect
    {
        SpaceCenterFacility facility;
        static Dictionary<SpaceCenterFacility, string> facilityNames = new Dictionary<SpaceCenterFacility, string>();
        int level;

        static MinimumFacilityLevelRequirement()
        {
            facilityNames[SpaceCenterFacility.Administration] = "Administration Facility";
            facilityNames[SpaceCenterFacility.AstronautComplex] = "Astronaut Complex";
            facilityNames[SpaceCenterFacility.LaunchPad] = "Launch Pad";
            facilityNames[SpaceCenterFacility.MissionControl] = "Mission Control";
            facilityNames[SpaceCenterFacility.ResearchAndDevelopment] = "Research and Development";
            facilityNames[SpaceCenterFacility.Runway] = "Runway";
            facilityNames[SpaceCenterFacility.SpaceplaneHangar] = "Spaceplane Hangar";
            facilityNames[SpaceCenterFacility.TrackingStation] = "Tracking Station";
            facilityNames[SpaceCenterFacility.VehicleAssemblyBuilding] = "Vehicle Assembly Building";
        }

        public MinimumFacilityLevelRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            facility = ConfigNodeUtil.ParseValue<SpaceCenterFacility>(node, "facility", SpaceCenterFacility.Administration);
            level = ConfigNodeUtil.ParseValue<int>(node, "level");
        }

        public string RequirementText()
        {
            return level > 1 ? (facilityNames[facility] + " must be at least level " + level) : null;
        }

        public bool RequirementMet(out string unmetReason)
        {
            int currentLevel = (int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(facility) *
                ScenarioUpgradeableFacilities.GetFacilityLevelCount(facility)) + 1;
            unmetReason = "Current level: " + currentLevel;
            return currentLevel >= level;
        }
    }
}

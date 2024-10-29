using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using CompoundParts;
using Contracts;
using Strategies;
using Strategies.Effects;
using ContractConfigurator;

namespace Strategia
{
    /// <summary>
    /// Strategy effect that improves vessel values.
    /// </summary>
    public class VesselValueImprover : StrategyEffect
    {
        enum Attribute
        {
            ISP,
            ParachuteDrag,
            StrutStrength,
            ReactionWheelTorque,
        }

        string trait;
        List<float> multipliers;
        Attribute attribute;

        private Dictionary<string, float> originalValues = new Dictionary<string, float>();
        private static Dictionary<Attribute, string> attributeTitles = new Dictionary<Attribute, string>();

        static VesselValueImprover()
        {
            attributeTitles[Attribute.ISP] = "Engine ISP";
            attributeTitles[Attribute.ParachuteDrag] = "Parachute effectiveness";
            attributeTitles[Attribute.StrutStrength] = "Strut strength";
            attributeTitles[Attribute.ReactionWheelTorque] = "Reaction wheel torque";

        }

        public VesselValueImprover(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            float multiplier = Parent.GetLeveledListItem(multipliers);
            string multiplierStr = ToPercentage(multiplier, "F1");

            return attributeTitles[attribute] + " increased by " + multiplierStr + " when " + StringUtil.ATrait(trait) + " is on board.";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);
            multipliers = ConfigNodeUtil.ParseValue<List<float>>(node, "multiplier");
            trait = ConfigNodeUtil.ParseValue<string>(node, "trait");
            attribute = ConfigNodeUtil.ParseValue<Attribute>(node, "attribute");
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChange));
                GameEvents.onFlightReady.Add(new EventVoid.OnEvent(OnFlightReady));
                GameEvents.onPartAttach.Add(new EventData<GameEvents.HostTargetAction<Part, Part>>.OnEvent(OnPartAttach));
                GameEvents.onPartJointBreak.Add(new EventData<PartJoint, float>.OnEvent(OnPartJointBreak));
                GameEvents.onCrewTransferred.Add(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
                GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(OnVesselWasModified));
            }
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onFlightReady.Remove(new EventVoid.OnEvent(OnFlightReady));
            GameEvents.onPartAttach.Remove(new EventData<GameEvents.HostTargetAction<Part, Part>>.OnEvent(OnPartAttach));
            GameEvents.onPartJointBreak.Remove(new EventData<PartJoint, float>.OnEvent(OnPartJointBreak));
            GameEvents.onCrewTransferred.Remove(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
            GameEvents.onVesselWasModified.Remove(new EventData<Vessel>.OnEvent(OnVesselWasModified));
        }

        private void OnFlightReady()
        {
            HandleVessel(FlightGlobals.ActiveVessel);
        }

        private void OnVesselChange(Vessel vessel)
        {
            HandleVessel(vessel);
        }

        private void OnPartAttach(GameEvents.HostTargetAction<Part, Part> hta)
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                HandleVessel(hta.host.vessel);
            }
        }

        private void OnPartJointBreak(PartJoint p, float force)
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                HandleVessel(p.Parent.vessel);
            }
        }

        private void OnVesselWasModified(Vessel vessel)
        {
            HandleVessel(vessel);
        }

        private void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> a)
        {
            // Check both vessels
            HandleVessel(a.from.vessel);
            HandleVessel(a.to.vessel);
        }

        private void HandleVessel(Vessel vessel)
        {
            if (vessel == null)
            {
                return;
            }

            Debug.Log("Strategia: VesselValueImprover.HandleVessel");

            // Check for our trait
            bool needsIncrease = false;
            foreach (ProtoCrewMember pcm in VesselUtil.GetVesselCrew(vessel))
            {
                if (pcm.experienceTrait.Config.Name == trait)
                {
                    needsIncrease = true;
                    break;
                }
            }

            // Find all relevant parts
            foreach (Part p in vessel.parts)
            {
                foreach (PartModule m in p.Modules)
                {
                    switch (attribute)
                    {
                        case Attribute.ISP:
                            ModuleEngines engine = m as ModuleEngines;
                            if (engine != null)
                            {
                                FloatCurve curve = engine.atmosphereCurve;
                                ConfigNode node = new ConfigNode();
                                curve.Save(node);

                                // Find and adjust the vacuum ISP
                                ConfigNode newNode = new ConfigNode();
                                foreach (ConfigNode.Value pair in node.values)
                                {
                                    string[] values = pair.value.Split(new char[] { ' ' });
                                    if (values[0] == "0")
                                    {
                                        float value = float.Parse(values[1]);
                                        float oldValue = value;
                                        SetValue(p.partInfo.name + "|" + engine.engineID, needsIncrease, ref value);
                                        values[1] = value.ToString("F1");
                                        newNode.AddValue(pair.name, string.Join(" ", values));
                                        Debug.Log("Setting ISP of " + p + " from " + oldValue + " to " + value);
                                    }
                                    else
                                    {
                                        newNode.AddValue(pair.name, pair.value);
                                    }
                                }
                                curve.Load(newNode);
                                engine.realIsp = curve.Evaluate(0);
                            }
                            break;
                        case Attribute.ParachuteDrag:
                            ModuleParachute parachute = m as ModuleParachute;
                            if (parachute != null)
                            {
                                SetValue(p.persistentId.ToString(), needsIncrease, ref parachute.fullyDeployedDrag);
                            }
                            break;
                        case Attribute.StrutStrength:
                            CModuleStrut strut = m as CModuleStrut;
                            if (strut != null)
                            {
                                SetValue(p.persistentId.ToString() + "_linear", needsIncrease, ref strut.linearStrength);
                                SetValue(p.persistentId.ToString() + "_angular", needsIncrease, ref strut.angularStrength);
                            }
                            break;
                        case Attribute.ReactionWheelTorque:
                            ModuleReactionWheel reactionWheel = m as ModuleReactionWheel;
                            if (reactionWheel != null)
                            {
                                SetValue(p.persistentId.ToString() + "_pitch", needsIncrease, ref reactionWheel.PitchTorque);
                                SetValue(p.persistentId.ToString() + "_yaw", needsIncrease, ref reactionWheel.YawTorque);
                                SetValue(p.persistentId.ToString() + "_roll", needsIncrease, ref reactionWheel.RollTorque);
                            }
                            break;
                    }
                }
            }
        }

        private void SetValue(string name, bool increaseRequired, ref float value)
        {
            // Multiplier to use
            float multiplier = Parent.GetLeveledListItem(multipliers);

            // Cache the original value
            if (!originalValues.ContainsKey(name))
            {
                originalValues[name] = value;
            }
            value = originalValues[name] * (increaseRequired ? multiplier : 1.0f);
            Debug.Log("Strategia.VesselValueImprover.SetValue]: " + name + ":" + originalValues[name].ToString() + " x " + (increaseRequired ? multiplier : 1.0f).ToString() + " = " + value.ToString() );
        }
    }
}

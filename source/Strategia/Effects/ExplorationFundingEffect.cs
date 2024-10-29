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
using ContractConfigurator.Util;

namespace Strategia
{
    /// <summary>
    /// Rewards for exploring biomes.
    /// </summary>
    public class ExplorationFundingEffect : StrategyEffect
    {
        enum ExplorationType
        {
            Biome,
            CelestialBody,
        }

        static string ExplorationTypeNamePlural(ExplorationType type)
        {
            switch (type)
            {
                case ExplorationType.Biome:
                    return "biomes";
                case ExplorationType.CelestialBody:
                    return "celestial bodies";
            }
            return "";
        }

        ExplorationType explorationType;
        double rewardFunds;

        public ExplorationFundingEffect(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            return (rewardFunds > 0.0 ? "+" : "") + rewardFunds.ToString("N0") + " funds when science transmitted/recovered from new " +
                ExplorationTypeNamePlural(explorationType) + ".";
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);

            rewardFunds = ConfigNodeUtil.ParseValue<double>(node, "rewardFunds");
            explorationType = ConfigNodeUtil.ParseValue<ExplorationType>(node, "explorationType");
        }

        protected override void OnRegister()
        {
            base.OnRegister();

            if (Parent.IsActive)
            {
                GameEvents.OnScienceRecieved.Add(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
            }
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            GameEvents.OnScienceRecieved.Remove(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
        }

        private void OnScienceReceived(float science, ScienceSubject subject, ProtoVessel vessel, bool reverseEngineered)
        {
            // If this is set, it means that the vessel recovery dialog is figuring out the science that was received
            if (reverseEngineered)
            {
                return;
            }

            Biome biome = Science.GetBiome(subject);
            if (biome == null || biome.IsKSC())
            {
                return;
            }

            IEnumerable<ScienceSubject> subjects = ResearchAndDevelopment.GetSubjects().Where(ss => ss.id.Contains(biome.body.name));
            if (explorationType == ExplorationType.Biome)
            {
                subjects = subjects.Where(ss => ss.id.Contains(biome.biome));
            }
            float totalScience = subjects.Sum(ss => ss.science) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;

            // The values will be the same if this is the first for the biome
            if (Math.Abs(totalScience - science) < 0.001)
            {
                Funding.Instance.AddFunds(rewardFunds, TransactionReasons.Strategies);

                string title = "Rewards from strategy '" + Parent.Title + "'";
                string header = "Science from new " + ExplorationTypeNamePlural(explorationType) + ":\n";
                string rewardMessage = "    " + (explorationType == ExplorationType.Biome ? biome.ToString() : biome.body.name) +
                    ": <color=#B4D455><sprite=\"CurrencySpriteAsset\" name=\"Funds\" tint=1> " + rewardFunds.ToString("N0") + "</color>\n";

                MessageSystem.Message message = MessageSystem.Instance.FindMessages(m => m.messageTitle == title).FirstOrDefault();
                if (message == null)
                {
                    MessageSystem.Instance.AddMessage(new MessageSystem.Message(title,
                        header + rewardMessage, MessageSystemButton.MessageButtonColor.GREEN, MessageSystemButton.ButtonIcons.ACHIEVE));
                }
                else
                {
                    // Section doesn't exist
                    if (!message.message.Contains(header))
                    {
                        message.message += "\n" + header;
                        message.message += rewardMessage;
                    }
                    // Section is second (last)
                    else if (message.message.Contains("\n\n" + header))
                    {
                        message.message += rewardMessage;
                    }
                    // Section is first
                    else
                    {
                        message.message = message.message.Replace("\n\n", "\n" + rewardMessage);
                    }

                    message.IsRead = false;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSP.UI.Screens;
using Contracts;
using Strategies;
using ContractConfigurator;

namespace ReStrategia
{
    public class ContractEffect : StrategyEffect, IObjectiveEffect, IOnDeactivateEffect, ICanDeactivateEffect
    {
        /// <summary>
        /// Separate MonoBehaviour for checking, as the strategy system only gets update calls in flight.
        /// </summary>
        [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
        public class ContractChecker : MonoBehaviour
        {
            public static ContractChecker Instance;
            public List<ContractEffect> effects = new List<ContractEffect>();

            void Start()
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }

            public void Register(ContractEffect effect)
            {
                effects.AddUnique(effect);
            }

            public void Unregister(ContractEffect effect)
            {
                effects.Remove(effect);
            }

            public void Update()
            {
                foreach (ContractEffect effect in effects)
                {
                    // Assign the contract
                    if (effect.contract == null)
                    {
                        effect.contract = ContractSystem.Instance.GetCurrentActiveContracts<ConfiguredContract>().
                            FirstOrDefault(c => c.contractType != null && c.contractType.name == effect.contractType);
                    }
                }
            }
        }

        public CelestialBody targetBody;
        public List<CelestialBody> bodies;
        public double rewardFunds { get; private set; }
        public float rewardScience { get; private set; }
        public float rewardReputation { get; private set; }
        public double failureFunds { get; private set; }
        public float failureScience { get; private set; }
        public float failureReputation { get; private set; }
        public double advanceFunds { get; private set; }
        public float advanceScience { get; private set; }
        public float advanceReputation { get; private set; }
        public string synopsis;
        public string completedMessage;
        public string failureMessage;

        public string contractType;

        public Contract contract;

        bool normalDeactivation = false;

        public ContractEffect(Strategy parent)
            : base(parent)
        {
        }

        public IEnumerable<string> ObjectiveText()
        {
            yield return synopsis;

            if (failureFunds + advanceFunds > 0.0)
            {
                yield return "A penalty of " + (failureFunds + advanceFunds).ToString("N0") + " funds will be applied if cancelled before the objective is completed.";
            }
            if (failureReputation > 0.0)
            {
                yield return "A penalty of " + failureReputation.ToString("N0") + " reputation will be applied if cancelled before the objective is completed.";
            }
            if (failureScience > 0.0)
            {
                yield return "A penalty of " + failureScience.ToString("N0") + " science will be applied if cancelled before the objective is completed.";
            }
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            contractType = ConfigNodeUtil.ParseValue<string>(node, "contractType");
            targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", FlightGlobals.Bodies.First(cb => cb.isHomeWorld));
            bodies = ConfigNodeUtil.ParseValue<List<CelestialBody>>(node, "bodies", CelestialBodyUtil.GetDistinctBodiesForStrategy(Parent.Config.Name).ToList());
            rewardFunds = ConfigNodeUtil.ParseValue<double>(node, "rewardFunds", 0.0);
            rewardScience = ConfigNodeUtil.ParseValue<float>(node, "rewardScience", 0.0f);
            rewardReputation = ConfigNodeUtil.ParseValue<float>(node, "rewardReputation", 0.0f);
            failureFunds = ConfigNodeUtil.ParseValue<double>(node, "failureFunds", 0.0);
            failureScience = ConfigNodeUtil.ParseValue<float>(node, "failureScience", 0.0f);
            failureReputation = ConfigNodeUtil.ParseValue<float>(node, "failureReputation", 0.0f);
            advanceFunds = ConfigNodeUtil.ParseValue<double>(node, "advanceFunds", 0.0);
            advanceScience = ConfigNodeUtil.ParseValue<float>(node, "advanceScience", 0.0f);
            advanceReputation = ConfigNodeUtil.ParseValue<float>(node, "advanceReputation", 0.0f);
            synopsis = ConfigNodeUtil.ParseValue<string>(node, "synopsis");
            completedMessage = ConfigNodeUtil.ParseValue<string>(node, "completedMessage");
            failureMessage = ConfigNodeUtil.ParseValue<string>(node, "failureMessage");
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                ContractChecker.Instance.Register(this);
                GameEvents.Contract.onCompleted.Add(new EventData<Contract>.OnEvent(OnContractCompleted));
                GameEvents.Contract.onFailed.Add(new EventData<Contract>.OnEvent(OnContractFailed));

                // Force contracts to generate immediately in case we need the associated contract
                ContractPreLoader.Instance.ResetGenerationFailure();
            }
        }

        protected override void OnUnregister()
        {
            if (!Parent.IsActive && contract != null)
            {
                contract.Cancel();
                contract = null;
            }

            ContractChecker.Instance.Unregister(this);
            GameEvents.Contract.onCompleted.Remove(new EventData<Contract>.OnEvent(OnContractCompleted));
            GameEvents.Contract.onFailed.Remove(new EventData<Contract>.OnEvent(OnContractFailed));
        }

        protected void OnContractCompleted(Contract c)
        {
            if (c == contract)
            {
                normalDeactivation = true;
                (Parent as StrategiaStrategy).ForceDeactivate();
            }
        }

        protected void OnContractFailed(Contract c)
        {
            if (c == contract)
            {
                normalDeactivation = true;
                MessageSystem.Instance.AddMessage(new MessageSystem.Message("Failed to complete strategy '" + Parent.Title + "'",
                    failureMessage, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.FAIL));
                (Parent as StrategiaStrategy).ForceDeactivate();
            }
        }

        public bool CanDeactivate(ref string reason)
        {
            if (failureFunds + advanceFunds > 0.0 && Funding.Instance.Funds < failureFunds + advanceFunds)
            {
                reason = "Not enough funds to pay cancellation penalty (" + (failureFunds + advanceFunds).ToString("N0") + " required).";
                return false;
            }
            if (failureScience > 0.0 && ResearchAndDevelopment.Instance.Science < failureScience)
            {
                reason = "Not enough science to pay cancellation penalty (" + failureScience.ToString("N0") + " required).";
                return false;
            }

            return true;
        }

        public void OnDeactivate()
        {
            if (!normalDeactivation)
            {
                if (failureFunds  > 0.0)
                {
                    Funding.Instance.AddFunds(-failureFunds, TransactionReasons.Strategies);
                }
                if (failureReputation > 0.0)
                {
                    Reputation.Instance.AddReputation(-failureReputation, TransactionReasons.Strategies);
                }
                if (failureScience > 0.0)
                {
                    ResearchAndDevelopment.Instance.AddScience(-failureScience, TransactionReasons.Strategies);
                }
            }
        }
    }
}

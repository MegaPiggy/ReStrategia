using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Strategies;
using KSP.UI.Screens;

namespace ReStrategia
{
    public class StrategiaStrategy : Strategy
    {
        private float deactivateCall;
        private bool forcedDeactivation = false;
        public static StrategiaStrategy lastActivationRequest = null;

        protected override string GetText()
        {
            return base.GetText();
        }

        protected override string GetEffectText()
        {
            string result = GenerateEffectText();
            result += GenerateCostText();
            result += GenerateObjectiveText();
            result += GenerateRequirementText();

            return result;
        }

        protected virtual string GenerateEffectText()
        {
            // Write out the effect text
            string result = "<b><color=#feb200>Effects:</color></b>\n\n";
            foreach (StrategyEffect effect in Effects)
            {
                IMultipleEffects multiEffect = effect as IMultipleEffects;
                if (multiEffect != null)
                {
                    foreach (string effectText in (multiEffect.EffectText()))
                    {
                        if (!string.IsNullOrEmpty(effectText))
                        {
                            result += "<color=#BEC2AE>* " + effectText + "</color>\n";
                        }
                    }
                }
                else
                {
                    string effectText = effect.Description;
                    if (!string.IsNullOrEmpty(effectText))
                    {
                        result += "<color=#BEC2AE>* " + effectText + "</color>\n";
                    }
                }
            }

            return result;
        }

        protected virtual string GenerateCostText()
        {
            string result = "";

            // Write out the cost line
            string costLine = "";
            if (InitialCostFunds != 0)
            {
                costLine += "<color=#B4D455><sprite=\"CurrencySpriteAsset\" name=\"Funds\" tint=1> " + InitialCostFunds.ToString("N0") + "    </color>";
            }
            if (InitialCostScience != 0)
            {
                costLine += "<color=#6DCFF6><sprite=\"CurrencySpriteAsset\" name=\"Science\" tint=1> " + InitialCostScience.ToString("N0") + "    </color>";
            }
            if (InitialCostReputation != 0)
            {
                costLine += "<color=#E0D503><sprite=\"CurrencySpriteAsset\" name=\"Reputation\" tint=1> " + InitialCostReputation.ToString("N0") + "    </color>";
            }
            if (!string.IsNullOrEmpty(costLine))
            {
                result += "\n<b><color=#EDED8B>Setup Cost:</color></b> " + costLine + "\n";
            }

            return result;
        }

        protected virtual string GenerateObjectiveText()
        {
            string result = "";

            // Write out objectives
            bool first = true;
            foreach (StrategyEffect effect in Effects)
            {
                IObjectiveEffect objectiveEffect = effect as IObjectiveEffect;
                if (objectiveEffect != null)
                {
                    if (first)
                    {
                        result += "\n<b><color=#feb200>Objectives:</color></b>\n\n";
                        first = false;
                    }

                    foreach (string objectiveText in objectiveEffect.ObjectiveText())
                    {
                        if (!string.IsNullOrEmpty(objectiveText))
                        {
                            result += "<color=#BEC2AE>* " + objectiveText + "</color>\n";
                        }
                    }
                }
            }

            // Calculate objective rewards/penalties
            double advanceFunds = 0.0;
            float advanceScience = 0.0f;
            float advanceReputation = 0.0f;
            double rewardFunds = 0.0;
            float rewardScience = 0.0f;
            float rewardReputation = 0.0f;
            double failureFunds = 0.0;
            float failureScience = 0.0f;
            float failureReputation = 0.0f;
            foreach (StrategyEffect effect in Effects)
            {
                IObjectiveEffect objectiveEffect = effect as IObjectiveEffect;
                if (objectiveEffect != null)
                {
                    advanceFunds += objectiveEffect.advanceFunds;
                    advanceScience += objectiveEffect.advanceScience;
                    advanceReputation += objectiveEffect.advanceReputation;
                    rewardFunds += objectiveEffect.rewardFunds;
                    rewardScience += objectiveEffect.rewardScience;
                    rewardReputation += objectiveEffect.rewardReputation;
                    failureFunds += objectiveEffect.failureFunds + objectiveEffect.advanceFunds;
                    failureScience += objectiveEffect.failureScience;
                    failureReputation += objectiveEffect.failureReputation;
                }
            }

            // Write out objective advances
            first = true;
            if (advanceFunds > 0 || advanceScience > 0 || advanceReputation > 0)
            {
                if (first)
                {
                    result += "\n";
                    first = false;
                }

                result += "<b><color=#8BED8B>Advances: </color></b>";
                if (advanceFunds > 0)
                {
                    result += "<color=#B4D455><sprite=\"CurrencySpriteAsset\" name=\"Funds\" tint=1> " + advanceFunds.ToString("N0") + "    </color>";
                }
                if (advanceScience > 0)
                {
                    result += "<color=#6DCFF6><sprite=\"CurrencySpriteAsset\" name=\"Science\" tint=1> " + advanceScience.ToString("N0") + "    </color>";
                }
                if (advanceReputation > 0)
                {
                    result += "<color=#E0D503><sprite=\"CurrencySpriteAsset\" name=\"Reputation\" tint=1> " + advanceReputation.ToString("N0") + "    </color>";
                }
                result += "\n";
            }

            if (rewardFunds > 0 || rewardScience > 0 || rewardReputation > 0)
            {
                if (first)
                {
                    result += "\n";
                    first = false;
                }

                result += "<b><color=#8BED8B>Rewards: </color></b>";
                if (rewardFunds > 0)
                {
                    result += "<color=#B4D455><sprite=\"CurrencySpriteAsset\" name=\"Funds\" tint=1> " + rewardFunds.ToString("N0") + "    </color>";
                }
                if (rewardScience > 0)
                {
                    result += "<color=#6DCFF6><sprite=\"CurrencySpriteAsset\" name=\"Science\" tint=1> " + rewardScience.ToString("N0") + "    </color>";
                }
                if (rewardReputation > 0)
                {
                    result += "<color=#E0D503><sprite=\"CurrencySpriteAsset\" name=\"Reputation\" tint=1> " + rewardReputation.ToString("N0") + "    </color>";
                }
                result += "\n";
            }

            // Write out objective penalties
            if (failureFunds > 0 || failureScience > 0 || failureReputation > 0)
            {
                if (first)
                {
                    result += "\n";
                    first = false;
                }

                result += "<b><color=#ED0B0B>Penalties: </color></b>";
                if (failureFunds > 0)
                {
                    result += "<color=#B4D455><sprite=\"CurrencySpriteAsset\" name=\"Funds\" tint=1> -" + failureFunds.ToString("N0") + "    </color>";
                }
                if (failureScience > 0)
                {
                    result += "<color=#6DCFF6><sprite=\"CurrencySpriteAsset\" name=\"Science\" tint=1> -" + failureScience.ToString("N0") + "    </color>";
                }
                if (failureReputation > 0)
                {
                    result += "<color=#E0D503><sprite=\"CurrencySpriteAsset\" name=\"Reputation\" tint=1> -" + failureReputation.ToString("N0") + "    </color>";
                }
                result += "\n";
            }

            return result;
        }

        protected virtual string GenerateRequirementText()
        {
            string result = "";

            // Write out stock-based requirements
            result += "\n<b><color=#feb200>Requirements:</color></b>\n\n";
            if (InitialCostFunds > 0)
            {
                double currentFunds = Funding.Instance.Funds;
                bool fundsMet = Math.Round(currentFunds) >= Math.Round(InitialCostFunds);
                string text = "At least " + InitialCostFunds.ToString("N0") + " funds";
                if (!fundsMet)
                {
                    text += " (Current funds: " + Math.Round(currentFunds).ToString("N0") + ")";
                }
                result += RequirementText(text, fundsMet);
            }
            if (InitialCostScience > 0)
            {
                double currentScience = ResearchAndDevelopment.Instance.Science;
                bool scienceMet = Math.Round(currentScience) >= Math.Round(InitialCostScience);
                string text = "At least " + InitialCostScience.ToString("N0") + " science";
                if (!scienceMet)
                {
                    text += " (Current science: " + Math.Round(currentScience).ToString("N0") + ")";
                }
                result += RequirementText(text, scienceMet);
            }
            if (RequiredReputation > -1000 || InitialCostReputation > 0)
            {
                float reputationNeeded = Math.Max(RequiredReputation, InitialCostReputation > 0 ? InitialCostReputation : -1000);
                float currentReputation = Reputation.Instance.reputation;
                bool repMet = Math.Round(currentReputation) >= Math.Round(reputationNeeded);
                string text = "At least " + reputationNeeded.ToString("N0") + " reputation";
                if (!repMet)
                {
                    text += " (Current reputation: " + Math.Round(currentReputation).ToString("N0") + ")";
                }
                result += RequirementText(text, repMet);
            }

            // Requirements from strategies
            foreach (StrategyEffect effect in Effects)
            {
                IRequirementEffect requirementEffect = effect as IRequirementEffect;
                if (requirementEffect != null)
                {
                    string unmetReason;
                    bool requirementMet = requirementEffect.RequirementMet(out unmetReason);
                    string text = requirementEffect.RequirementText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        if (!requirementMet && !string.IsNullOrEmpty(unmetReason))
                        {
                            text += " (" + unmetReason + ")";
                        }
                        result += RequirementText(text, requirementMet);
                    }
                }
            }

            return result;
        }

        public string RequirementText(string requirement, bool met)
        {
            string color = met ? "#8BED8B" : "#FF7512";
            return "<color=" + color + ">* " + requirement + "</color>\n";
        }

        protected override bool CanActivate(ref string reason)
        {
            lastActivationRequest = this;

            // Force the active strategies text to be updated next tick
            AdminResizer.Instance.ticks = 0;

            // If we are at max strategies, only allow activation if it would be an upgrade
            IEnumerable<Strategy> activeStrategies = StrategySystem.Instance.Strategies.Where(s => s.IsActive);
            if (activeStrategies.Count() >= Administration.Instance.MaxActiveStrategies)
            {
                UpgradeableStrategy upgradeable = this as UpgradeableStrategy;
                if (upgradeable != null && activeStrategies.OfType<UpgradeableStrategy>().Any(s => s.Name == upgradeable.Name))
                {
                    return true;
                }
                else
                {
                    reason = "The Administration Building cannot support more than " + Administration.Instance.MaxActiveStrategies + " active strategies at this level.";
                    return false;
                }
            }

            // Special requirements
            foreach (StrategyEffect effect in Effects)
            {
                IRequirementEffect requirement = effect as IRequirementEffect;
                if (requirement != null)
                {
                    string unmetReason;
                    if (!requirement.RequirementMet(out unmetReason))
                    {
                        reason = requirement.RequirementText();
                        if (!string.IsNullOrEmpty(unmetReason))
                        {
                            reason += " (" + unmetReason + ")";
                        }

                        return false;
                    }
                }
            }

            return base.CanActivate(ref reason);
        }

        public void ForceDeactivate()
        {
            forcedDeactivation = true;
            Deactivate();
            forcedDeactivation = false;
        }

        protected override bool CanDeactivate(ref string reason)
        {
            // Force the active strategies text to be updated next tick
            AdminResizer.Instance.ticks = 0;

            deactivateCall = Time.time;
            if (forcedDeactivation)
            {
                return true;
            }

            foreach (StrategyEffect effect in Effects)
            {
                ICanDeactivateEffect canDeactivateEffect = effect as ICanDeactivateEffect;
                if (canDeactivateEffect != null)
                {
                    if (!canDeactivateEffect.CanDeactivate(ref reason))
                    {
                        return false;
                    }
                }
            }

            return base.CanDeactivate(ref reason);
        }

        public int MinimumFacilityLevel()
        {
            if (FactorSliderDefault > 0.70)
            {
                return 3;
            }
            if (FactorSliderDefault > 0.40)
            {
                return 2;
            }

            return 1;
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            if (!IsActive && deactivateCall == Time.time)
            {
                foreach (StrategyEffect effect in Effects)
                {
                    IOnDeactivateEffect onDeactivateEffect = effect as IOnDeactivateEffect;
                    if (onDeactivateEffect != null)
                    {
                        onDeactivateEffect.OnDeactivate();
                    }
                }
            }
        }
    }
}

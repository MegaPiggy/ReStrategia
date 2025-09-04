using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Strategies.Effects;
using ContractConfigurator;

namespace ReStrategia
{
    /// <summary>
    /// Allows overriding of career parameters.
    /// </summary>
    public class CareerParameterOverride : CurrencyOperation
    {
        enum Parameter
        {
            RepLossDeclined,
        }

        Parameter parameter;
        float value;
        float? originalValue = null;

        public CareerParameterOverride(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            switch (parameter)
            {
                case Parameter.RepLossDeclined:
                    return value == 0.0 ? "Removes reputation loss on contract decline." :
                        "Sets reputation loss on contract decline to " + value.ToString("N0") + ".";
                default:
                    break;
            }

            return null;
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);

            parameter = ConfigNodeUtil.ParseValue<Parameter>(node, "parameter");
            value = ConfigNodeUtil.ParseValue<float>(node, "value");
        }

        protected override void OnRegister()
        {
            if (Parent.IsActive)
            {
                switch (parameter)
                {
                    case Parameter.RepLossDeclined:
                        if (originalValue != null)
                        {
                            originalValue = HighLogic.CurrentGame.Parameters.Career.RepLossDeclined;
                        }
                        HighLogic.CurrentGame.Parameters.Career.RepLossDeclined = value;
                        break;
                    default:
                        break;
                }
            }
        }

        protected override void OnUnregister()
        {
            // Check for a deactivation
            if (!Parent.IsActive)
            {
                switch (parameter)
                {
                    case Parameter.RepLossDeclined:
                        if (originalValue != null)
                        {
                            HighLogic.CurrentGame.Parameters.Career.RepLossDeclined = originalValue.Value;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (originalValue != null)
            {
                node.AddValue("originalValue", originalValue);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            originalValue = ConfigNodeUtil.ParseValue<float?>(node, "originalValue", null);
        }
    }
}

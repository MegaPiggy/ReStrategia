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
    /// Special CurrencyOperation that overrides the text.
    /// </summary>
    public class CurrencyOperationTextOverride : CurrencyOperationWithPopup
    {
        string description;

        public CurrencyOperationTextOverride(Strategy parent)
            : base(parent)
        {
        }

        protected override string GetDescription()
        {
            return description;
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);

            description = ConfigNodeUtil.ParseValue<string>(node, "description");
        }
    }
}

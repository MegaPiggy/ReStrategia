using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Strategies.Effects;

namespace ReStrategia
{
    /// <summary>
    /// Special CurrencyOperation that only acts on non-zero values.
    /// </summary>
    public class CurrencyOperationNonZero : CurrencyOperation
    {
        public CurrencyOperationNonZero(Strategy parent)
            : base(parent)
        {
        }

        public CurrencyOperationNonZero(Strategy parent, float minValue, float maxValue, Currency currency, CurrencyOperation.Operator op, TransactionReasons AffectReasons, string description)
            : base(parent, minValue, maxValue, currency, op, AffectReasons, description)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            base.OnLoadFromConfig(node);
        }

        protected override void OnEffectQuery(CurrencyModifierQuery qry)
        {
            // Check if it's non-zero
            if (Math.Abs(qry.GetInput(Currency.Funds)) < 0.01 && Math.Abs(qry.GetInput(Currency.Science)) < 0.01 && Math.Abs(qry.GetInput(Currency.Reputation)) < 0.01)
            {
                return;
            }

            base.OnEffectQuery(qry);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;
using Strategies.Effects;

namespace ReStrategia
{
    public interface ICanDeactivateEffect
    {
        bool CanDeactivate(ref string reason);
    }
}

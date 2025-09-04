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
    public interface IObjectiveEffect
    {
        IEnumerable<string> ObjectiveText();

        double rewardFunds { get; }
        float rewardScience { get; }
        float rewardReputation { get; }
        double failureFunds { get; }
        float failureScience { get; }
        float failureReputation { get; }
        double advanceFunds { get; }
        float advanceScience { get; }
        float advanceReputation { get; }
    }
}

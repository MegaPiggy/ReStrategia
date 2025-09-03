using KopernicusExpansion.Wormholes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Strategia
{
    internal class KopernicusExpansionWrapper
    {
        public static bool APIReady { get; internal set; }

        internal static bool Init()
        {
            try
            {
                Debug.Log(typeof(WormholeComponent).Name);
                APIReady = true;
            }
            catch
            {
                APIReady = false;
                return false;
            }
            return true;
        }

        internal static bool IsWormhole(CelestialBody cb)
        {
            return cb.GetComponent<KopernicusExpansion.Wormholes.WormholeComponent>() != null;
        }
    }
}

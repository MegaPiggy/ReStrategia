using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Strategia
{
    internal class KopernicusWrapper
    {
        public static bool APIReady { get; internal set; }

        internal static bool Init()
        {
            try
            {
                Kopernicus.Utility.Clamp(0, 0, 0);
                APIReady = true;
            }
            catch
            {
                APIReady = false;
                return false;
            }
            return true;
        }

        internal static bool IsInvisible(CelestialBody cb)
        {
            var sc = cb.GetComponent<Kopernicus.Components.StorageComponent>();
            if (sc != null)
            {
                return sc.Has("invisibleScaledSpace") && sc.Get<bool>("invisibleScaledSpace");
            }
            return false;
        }
    }
}

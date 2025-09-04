using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReStrategia
{
    internal class SingularityWrapper
    {
        public static bool APIReady { get; internal set; }

        internal static bool Init()
        {
            try
            {
                var singularity = Singularity.Singularity.Instance;
                APIReady = true;
            }
            catch
            {
                APIReady = false;
                return false;
            }
            return true;
        }

        internal static bool IsSingularity(CelestialBody cb)
        {
            return cb.scaledBody.GetComponent<Singularity.SingularityObject>() != null;
        }
    }
}

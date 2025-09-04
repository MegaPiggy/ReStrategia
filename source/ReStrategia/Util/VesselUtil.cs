using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;

namespace ReStrategia
{
    public static class VesselUtil
    {
        /// <summary>
        /// Gets the vessel crew and works for EVAs as well
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static IEnumerable<ProtoCrewMember> GetVesselCrew(Vessel v)
        {
            if (v == null)
            {
                yield break;
            }

            // EVA vessel
            if (v.vesselType == VesselType.EVA)
            {
                if (v.parts == null)
                {
                    yield break;
                }

                foreach (Part p in v.parts)
                {
                    foreach (ProtoCrewMember pcm in p.protoModuleCrew)
                    {
                        yield return pcm;
                    }
                }
            }
            else
            {
                // Vessel with crew
                foreach (ProtoCrewMember pcm in v.GetVesselCrew())
                {
                    yield return pcm;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;

namespace Strategia
{
    public static class CelestialBodyUtil
    {
        private const double BARYCENTER_THRESHOLD = 100;

        public static IEnumerable<CelestialBody> GetBodiesForStrategy(string id)
        {
            CelestialBody home = FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).Single();

            if (id == "KerbinProgram")
            {
                yield return home;
            }
            else if (id == "MoonProgram")
            {
                foreach (CelestialBody child in home.orbitingBodies)
                {
                    yield return child;
                }

                // Special case for mods where Kerbin is a Gas Giant's moon
                if (home.referenceBody != FlightGlobals.Bodies[0])
                {
                    foreach (CelestialBody child in home.referenceBody.orbitingBodies.Where(cb => cb != home))
                    {
                        yield return child;
                    }
                }
            }
            else if (id == "PlanetaryProgram")
            {
                foreach (CelestialBody body in FlightGlobals.Bodies[0].orbitingBodies)
                {
                    if (body != home)
                    {
                        if (body.Radius > BARYCENTER_THRESHOLD)
                        {
                            if (body.pqsController != null && body.hasSolidSurface)
                            {
                                yield return body;
                            }
                        }
                    }
                }
            }
            else if (id == "GasGiantProgram")
            {
                foreach (CelestialBody body in FlightGlobals.Bodies[0].orbitingBodies)
                {
                    if ((body.pqsController == null || !body.hasSolidSurface) && !body.orbitingBodies.Contains(home) && body.orbitingBodies.Count() >= 2 && body.Radius > BARYCENTER_THRESHOLD)
                    {
                        yield return body;
                    }
                }
            }
            else if (id == "ImpactorProbes")
            {
                foreach (CelestialBody body in FlightGlobals.Bodies[0].orbitingBodies)
                {
                    if (body != home)
                    {
                        if (body.pqsController != null && body.hasSolidSurface)
                        {
                            yield return body;
                        }

                        foreach (CelestialBody childBody in body.orbitingBodies)
                        {
                            if (childBody.pqsController != null && body.hasSolidSurface)
                            {
                                yield return childBody;
                            }
                        }
                    }
                }
            }
            else if (id == "FlyByProbes")
            {
                foreach (CelestialBody body in FlightGlobals.Bodies[0].orbitingBodies)
                {
                    if (body != home && !body.orbitingBodies.Contains(home))
                    {
                        yield return body;
                    }
                }
            }
            else
            {
                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    yield return body;
                }
            }
        }

        public static string BodyList(IEnumerable<CelestialBody> bodies, string conjunction)
        {
            CelestialBody first = bodies.First();
            CelestialBody last = bodies.Last();
            string result = first.CleanDisplayName();
            foreach (CelestialBody body in bodies.Where(cb => cb != first && cb != last))
            {
                result += ", " + body.CleanDisplayName(true);
            }
            if (last != first)
            {
                result += " " + conjunction + " " + last.CleanDisplayName(true);
            }
            return result;
        }

    }
}

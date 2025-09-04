using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;
using Kopernicus.Configuration;
using Kopernicus.UI;

namespace ReStrategia
{
    public static class CelestialBodyUtil
    {
        private const double BARYCENTER_THRESHOLD = 100;

        private enum CelestialBodyType
        {
            NOT_APPLICABLE,
            STAR,
            GAS_GIANT,
            TERRESTRIAL,
            MOON,
            BARYCENTER,
            SINGULARITY,
            WORMHOLE
        }

        private static CelestialBodyType BodyType(CelestialBody cb)
        {
            if (cb == null)
            {
                return CelestialBodyType.NOT_APPLICABLE;
            }

            if (IsBarycenter(cb))
            {
                return CelestialBodyType.BARYCENTER;
            }

            if (IsSingularity(cb))
            {
                return CelestialBodyType.SINGULARITY;
            }

            if (IsWormhole(cb))
            {
                return CelestialBodyType.WORMHOLE;
            }

            if (cb.isStar)
            {
                return CelestialBodyType.STAR;
            }

            // Add a special case for barycenters (Sigma binary)
            var isTerrestrial = IsTerrestrial(cb);
            var referenceType = BodyType(cb.referenceBody);
            if (referenceType is CelestialBodyType.STAR or CelestialBodyType.SINGULARITY or CelestialBodyType.BARYCENTER)
            {
                // For barycenters, gas giants and the biggest terrestrial are planets, the rest are moons.
                if (referenceType is CelestialBodyType.BARYCENTER)
                {
                    for (int i = cb.referenceBody.orbitingBodies.Count; --i >= 0;)
                    {
                        if (cb.referenceBody.orbitingBodies[i].Mass > cb.Mass && isTerrestrial)
                        {
                            return CelestialBodyType.MOON;
                        }
                    }
                }

                if (isTerrestrial)
                {
                    return CelestialBodyType.TERRESTRIAL;
                }
            }

            if (isTerrestrial)
            {
                return CelestialBodyType.MOON;
            }

            if (IsGasGiant(cb))
            {
                return CelestialBodyType.GAS_GIANT;
            }

            return CelestialBodyType.NOT_APPLICABLE;
        }

        private static bool IsTerrestrial(CelestialBody cb)
        {
            return cb.pqsController != null && cb.hasSolidSurface;
        }

        private static bool IsGasGiant(CelestialBody cb)
        {
            return (cb.pqsController == null || !cb.hasSolidSurface);
        }

        private static bool IsGasGiantWithManyMoons(CelestialBody cb)
        {
            return IsGasGiant(cb) && cb.orbitingBodies.Count() >= 2;
        }

        private static bool IsSingularity(CelestialBody cb)
        {
            return Version.VerifySingularityVersion() && SingularityWrapper.IsSingularity(cb);
        }

        private static bool IsBarycenter(CelestialBody cb)
        {
            return cb.Radius <= BARYCENTER_THRESHOLD || Version.VerifyKopernicusVersion() && KopernicusWrapper.IsInvisible(cb);
        }

        private static bool IsWormhole(CelestialBody cb)
        {
            return Version.VerifyKopernicusExpansionVersion() && KopernicusExpansionWrapper.IsWormhole(cb);
        }

        private static bool IsDirectRoot(CelestialBody cb)
        {
            return BodyType(cb) is CelestialBodyType.STAR or CelestialBodyType.SINGULARITY;
        }

        private static bool IsRoot(CelestialBody cb)
        {
            return BodyType(cb) is CelestialBodyType.STAR or CelestialBodyType.SINGULARITY or CelestialBodyType.BARYCENTER;
        }

        private static bool IsPlanet(CelestialBody cb)
        {
            return BodyType(cb) is CelestialBodyType.TERRESTRIAL or CelestialBodyType.GAS_GIANT;
        }

        /// <summary>
        /// The Sun plus any stars/singularities/barycenters orbiting the Sun,
        /// and recursively any orbiting those.
        /// </summary>
        private static IEnumerable<CelestialBody> GetSystemRoots(CelestialBody root)
        {
            if (root == null) yield break;

            yield return root;

            foreach (var cb in root.orbitingBodies)
            {
                if (BodyType(cb) is CelestialBodyType.STAR or CelestialBodyType.SINGULARITY or CelestialBodyType.BARYCENTER)
                {
                    // recursively include any deeper stars/singularities/barycenters
                    foreach (var nested in GetSystemRoots(cb))
                        yield return nested;
                }
            }
        }

        /// <summary>
        /// Children of a root that count as terrestrial planets.
        /// </summary>
        private static IEnumerable<CelestialBody> GetTerrestrialPlanetsUnderRoot(CelestialBody root)
        {
            if (root == null) yield break;
            foreach (var child in root.orbitingBodies)
            {
                if (BodyType(child) is CelestialBodyType.TERRESTRIAL)
                    yield return child;
            }
        }

        /// <summary>
        /// Children of a root that count as gas giant planets.
        /// </summary>
        private static IEnumerable<CelestialBody> GetGasGiantPlanetsUnderRoot(CelestialBody root)
        {
            if (root == null) yield break;
            foreach (var child in root.orbitingBodies)
            {
                if (BodyType(child) is CelestialBodyType.GAS_GIANT)
                    yield return child;
            }
        }

        /// <summary>
        /// Children of a root that count as planets (terrestrial or gas giant).
        /// </summary>
        private static IEnumerable<CelestialBody> GetPlanetsUnderRoot(CelestialBody root)
        {
            if (root == null) yield break;
            foreach (var child in root.orbitingBodies)
            {
                if (IsPlanet(child))
                    yield return child;
            }
        }

        /// <summary>
        /// Solid-surface moons of a given planet.
        /// </summary>
        private static IEnumerable<CelestialBody> GetSolidMoons(CelestialBody planet)
        {
            if (planet == null) yield break;
            foreach (var m in planet.orbitingBodies)
            {
                if (IsTerrestrial(m))
                    yield return m;
            }
        }

        public static bool loggedRoots = false;
        public static Dictionary<string, bool> loggedIDs = new Dictionary<string, bool>();

        public static IEnumerable<CelestialBody> GetBodiesForStrategy(string id)
        {
            CelestialBody sun = FlightGlobals.Bodies[0];
            CelestialBody home = FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).Single();

            var roots = GetSystemRoots(sun).ToList();

            if (!loggedRoots)
            {
                loggedRoots = true;
                UnityEngine.Debug.Log("[ReStrategia] System Roots");
                foreach (CelestialBody child in roots)
                    UnityEngine.Debug.Log("[ReStrategia] \"" + child.name + "\"");
            }

            if (id == "KerbinProgram")
            {
                yield return home;
                yield break;
            }
            else if (id == "MoonProgram")
            {
                // Moons of home
                foreach (CelestialBody child in home.orbitingBodies)
                    yield return child;

                // Special case for mods where Kerbin is a Gas Giant's moon
                if (!IsDirectRoot(home.referenceBody))
                {
                    foreach (CelestialBody child in home.referenceBody.orbitingBodies.Where(cb => cb != home))
                        yield return child;
                }
                yield break;
            }

            // Build a de-duped bag for everything else.
            var bag = new HashSet<CelestialBody>();

            if (id == "PlanetaryProgram")
            {
                foreach (var root in roots)
                {
                    foreach (var body in GetTerrestrialPlanetsUnderRoot(root)
                             .Where(cb => cb != home && !cb.orbitingBodies.Contains(home)))
                    {
                        bag.Add(body);
                    }
                }
            }
            else if (id == "GasGiantProgram")
            {
                foreach (var root in roots)
                {
                    foreach (var body in GetGasGiantPlanetsUnderRoot(root)
                             .Where(cb => cb != home && !cb.orbitingBodies.Contains(home)))
                    {
                        bag.Add(body);
                    }
                }
            }
            else if (id == "ImpactorProbes")
            {
                foreach (var root in roots)
                {
                    foreach (var planet in GetPlanetsUnderRoot(root)
                             .Where(cb => cb != home))
                    {
                        // Add if terrestrial
                        if (BodyType(planet) == CelestialBodyType.TERRESTRIAL) bag.Add(planet);

                        // Add solid-surface moons regardless of planet
                        foreach (var moon in GetSolidMoons(planet))
                            bag.Add(moon);
                    }
                }
            }
            else if (id == "FlyByProbes")
            {
                // All planets (terrestrial & gas giants) orbiting any system root, excluding home
                foreach (var root in roots)
                {
                    foreach (var body in GetPlanetsUnderRoot(root)
                             .Where(cb => cb != home && !cb.orbitingBodies.Contains(home)))
                    {
                        bag.Add(body);
                    }
                }
            }
            else
            {
                foreach (CelestialBody body in FlightGlobals.Bodies)
                    bag.Add(body);
            }

            if (!loggedIDs.ContainsKey(id))
            {
                loggedIDs.Add(id, true);
                foreach (var b in bag)
                    UnityEngine.Debug.Log("[ReStrategia] \"" + b.name + "\"");
            }
            foreach (var b in bag)
                yield return b;
        }

        public static string BodyList(IEnumerable<CelestialBody> bodies, string conjunction)
        {
            if (!bodies.Any()) return string.Empty;
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

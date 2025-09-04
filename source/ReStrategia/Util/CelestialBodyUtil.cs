using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;
using Kopernicus.Configuration;
using Kopernicus.UI;
using Expansions.Missions;

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
            if (cb == null || IsHidden(cb))
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
            var isTerrestrial = IsSolid(cb);
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

            if (IsNotSolid(cb))
            {
                return CelestialBodyType.GAS_GIANT;
            }

            return CelestialBodyType.NOT_APPLICABLE;
        }

        private static bool IsSolid(CelestialBody cb)
        {
            return cb.pqsController != null && cb.hasSolidSurface && !IsWormhole(cb);
        }

        private static bool IsNotSolid(CelestialBody cb)
        {
            return (cb.pqsController == null || !cb.hasSolidSurface);
        }

        private static bool IsHidden(CelestialBody cb)
        {
            return Version.VerifyKopernicusVersion() && KopernicusWrapper.IsRnDHidden(cb);
        }

        private static bool IsSingularity(CelestialBody cb)
        {
            return Version.VerifySingularityVersion() && SingularityWrapper.IsSingularity(cb);
        }

        /// <summary>
        /// Detects if this barycenter follows the SigmaBinary pattern:
        /// - Has a NOT_APPLICABLE child in orbitingBodies
        /// - Only one real body directly under the barycenter (the primary)
        /// </summary>
        public static bool IsSigmaBinary(CelestialBody bary)
        {
            if (!IsPlanetaryBarycenter(bary)) return false;

            // Must contain at least one NOT_APPLICABLE body
            bool hasNotApplicable = bary.orbitingBodies.Any(cb => BodyType(cb) == CelestialBodyType.NOT_APPLICABLE);
            int realChildren = bary.orbitingBodies.Count(cb => BodyType(cb) != CelestialBodyType.NOT_APPLICABLE && !IsBarycenter(cb));

            return hasNotApplicable && realChildren == 1;
        }

        public static bool IsBarycenter(CelestialBody cb)
        {
            return Version.VerifyKopernicusVersion() && (KopernicusWrapper.IsInvisible(cb) || KopernicusWrapper.IsRnDSkip(cb));
        }

        private static bool IsWormhole(CelestialBody cb)
        {
            return Version.VerifyKopernicusExpansionVersion() && KopernicusExpansionWrapper.IsWormhole(cb);
        }

        private static bool IsStellarObject(CelestialBody cb)
        {
            return BodyType(cb) is CelestialBodyType.STAR or CelestialBodyType.SINGULARITY;
        }

        private static bool IsTerrestrial(CelestialBody cb)
        {
            return BodyType(cb) is CelestialBodyType.TERRESTRIAL;
        }

        private static bool IsGasGiant(CelestialBody cb)
        {
            return BodyType(cb) is CelestialBodyType.GAS_GIANT;
        }

        private static bool IsPlanet(CelestialBody cb)
        {
            return BodyType(cb) is CelestialBodyType.TERRESTRIAL or CelestialBodyType.GAS_GIANT;
        }

        /// <summary>
        /// True if this barycenter is grouping stars/singularities (system-level).
        /// False if it's a planetary barycenter (should be processed with planets).
        /// </summary>
        private static bool IsStellarBarycenter(CelestialBody cb)
        {
            if (!IsBarycenter(cb)) return false;

            // If any of its orbiting bodies is a star or singularity, treat as stellar.
            return cb.orbitingBodies.Any(IsStellarObject);
        }

        private static bool IsPlanetaryBarycenter(CelestialBody cb)
        {
            if (!IsBarycenter(cb)) return false;

            // If any orbiting body is a star/singularity, it's not planetary
            return !cb.orbitingBodies.Any(IsStellarObject);
        }

        /// <summary>
        /// From a planetary barycenter, return its non-stellar, non-barycenter children
        /// ordered by Mass descending. Typically the first two are primary/secondary.
        /// </summary>
        private static List<CelestialBody> GetBarycenterComponents(CelestialBody bary)
        {
            if (bary == null || !IsPlanetaryBarycenter(bary)) return new List<CelestialBody>();

            if (IsSigmaBinary(bary))
            {
                var sigmaBinaryComponents = new List<CelestialBody>();
                // Primary is the only real child of the barycenter
                var primary = bary.orbitingBodies.FirstOrDefault(cb =>
                    BodyType(cb) != CelestialBodyType.NOT_APPLICABLE && !IsBarycenter(cb));
                sigmaBinaryComponents.Add(primary);
                // Look inside primary’s orbitingBodies for all the rest
                sigmaBinaryComponents.AddRange(primary.orbitingBodies);
                return sigmaBinaryComponents
                    .Where(cb => !IsStellarObject(cb) && !IsBarycenter(cb))
                    .OrderByDescending(cb => cb.Mass)
                    .ToList();
            }

            return bary.orbitingBodies
                .Where(cb => !IsStellarObject(cb) && !IsBarycenter(cb))
                .OrderByDescending(cb => cb.Mass)
                .ToList();
        }

        public static IEnumerable<CelestialBody> GetBarycenterPrimaryAndSecondary(CelestialBody bary)
        {
            if (bary == null) return new List<CelestialBody>();

            var comps = GetBarycenterComponents(bary);
            return comps.Count > 0 ? comps.Take(2) : new List<CelestialBody> { bary };
        }

        /// <summary> Primary of a planetary barycenter (by mass). </summary>
        public static CelestialBody GetBarycenterPrimary(CelestialBody bary)
        {
            if (bary == null) return null;

            var comps = GetBarycenterComponents(bary);
            return comps.Count > 0 ? comps[0] : null;
        }

        /// <summary> Secondary of a planetary barycenter (by mass). </summary>
        public static CelestialBody GetBarycenterSecondary(CelestialBody bary)
        {
            if (bary == null) return null;

            var comps = GetBarycenterComponents(bary);
            return comps.Count > 1 ? comps[1] : null;
        }


        /// <summary>
        /// True if the body should be considered a "moon-like" child of parent for strategy lists,
        /// honoring the solidsOnly filter. Excludes stellar objects and barycenters.
        /// </summary>
        private static bool IsMoonLike(CelestialBody child, bool solidsOnly)
        {
            if (child == null) return false;
            if (IsStellarObject(child) || IsBarycenter(child)) return false;
            if (solidsOnly) return IsSolid(child);
            // include any non-stellar, non-barycenter (terrestrial or gas giant)
            return BodyType(child) is not CelestialBodyType.NOT_APPLICABLE and not CelestialBodyType.WORMHOLE;
        }

        /// <summary> Moons that orbit the given parent directly (filtered). </summary>
        private static IEnumerable<CelestialBody> GetDirectMoons(CelestialBody parent, bool solidsOnly)
        {
            if (parent == null) yield break;
            foreach (var m in parent.orbitingBodies)
            {
                // Normal moon
                if (IsMoonLike(m, solidsOnly))
                {
                    yield return m;
                }
                // Special case: barycenter orbiting a planet
                else if (IsPlanetaryBarycenter(m))
                {
                    if (!solidsOnly) yield return m;
                    // Include barycenter’s primary/secondary
                    foreach (var body in GetBarycenterPrimaryAndSecondary(m))
                    {
                        if (solidsOnly && IsSolid(m)) yield return body;
                    }
                }
            }
        }

        /// <summary> Moons of the planetary barycenter itself (exclude its primary/secondary). </summary>
        private static IEnumerable<CelestialBody> GetBarycenterMoons(CelestialBody bary, bool solidsOnly)
        {
            if (bary == null || !IsPlanetaryBarycenter(bary)) yield break;

            var primary = GetBarycenterPrimary(bary);
            var secondary = GetBarycenterSecondary(bary);

            foreach (var m in bary.orbitingBodies)
            {
                if (m == primary || m == secondary) continue;
                if (IsMoonLike(m, solidsOnly))
                    yield return m;
            }
        }

        /// <summary> Moons of the primary component of a planetary barycenter. </summary>
        private static IEnumerable<CelestialBody> GetPrimaryMoons(CelestialBody bary, bool solidsOnly)
        {
            var primary = GetBarycenterPrimary(bary);
            var directMoons = GetDirectMoons(primary, solidsOnly);
            if (IsSigmaBinary(bary))
            {
                var secondary = GetBarycenterSecondary(bary);
                return directMoons.Where(cb => cb != secondary);
            }
            return directMoons;
        }

        /// <summary> Moons of the secondary component of a planetary barycenter. </summary>
        private static IEnumerable<CelestialBody> GetSecondaryMoons(CelestialBody bary, bool solidsOnly)
        {
            var secondary = GetBarycenterSecondary(bary);
            return GetDirectMoons(secondary, solidsOnly);
        }

        public static bool IsSystemRoot(CelestialBody cb)
        {
            return IsStellarObject(cb) || IsStellarBarycenter(cb);
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
                if (IsSystemRoot(cb))
                {
                    // recursively include any deeper stars/singularities/barycenters
                    foreach (var nested in GetSystemRoots(cb))
                        yield return nested;
                }
            }
        }

        /// <summary>
        /// Children of a root that count as planets (terrestrial, gas giant, or planetary barycenter).
        /// </summary>
        private static IEnumerable<CelestialBody> GetPlanetsUnderRoot(CelestialBody root)
        {
            if (root == null) yield break;

            foreach (var child in root.orbitingBodies)
            {
                if (IsPlanet(child) || IsPlanetaryBarycenter(child))
                    yield return child;
            }
        }

        /// <summary>
        /// All “planet nodes” under a root: regular planets, plus planetary barycenters
        /// whose PRIMARY matches the requested predicate (e.g., terrestrial-only or gas-giant-only).
        /// </summary>
        private static IEnumerable<CelestialBody> GetPlanetNodesUnderRoot(
            CelestialBody root,
            Func<CelestialBody, bool> primaryPredicate)
        {
            if (root == null) yield break;

            foreach (var child in root.orbitingBodies)
            {
                if (IsPlanetaryBarycenter(child))
                {
                    var primary = GetBarycenterPrimary(child);
                    if (primary != null && primaryPredicate(primary))
                        yield return child; // return the barycenter node
                }
                else if (IsPlanet(child) && primaryPredicate(child))
                {
                    yield return child; // standalone planet node
                }
            }
        }

        /// <summary> Children of a root that count as terrestrial planet nodes. </summary>
        private static IEnumerable<CelestialBody> GetTerrestrialPlanetsUnderRoot(CelestialBody root)
        {
            return GetPlanetNodesUnderRoot(
                root,
                IsTerrestrial
            );
        }

        /// <summary> Children of a root that count as gas giant planet nodes. </summary>
        private static IEnumerable<CelestialBody> GetGasGiantPlanetsUnderRoot(CelestialBody root)
        {
            return GetPlanetNodesUnderRoot(
                root,
                IsGasGiant
            );
        }

        /// <summary>
        /// Return everything “under” a node suitable for mission lists.
        /// - If node is a regular planet: returns the planet (if noPrimary is false) and its moons (filtered by solidsOnly).
        /// - If node is a planetary barycenter: returns primary (if noPrimary is false), secondary,
        ///   plus moons of (barycenter, primary, secondary), filtered by solidsOnly.
        /// </summary>
        public static IEnumerable<CelestialBody> GetBodiesUnderNode(CelestialBody node, bool solidsOnly = false, bool noBarycenter = true, bool noPrimary = true)
        {
            if (node == null) yield break;

            // Planetary barycenter aggregation
            if (IsPlanetaryBarycenter(node))
            {
                var primary = GetBarycenterPrimary(node);
                var secondary = GetBarycenterSecondary(node);

                if (!noBarycenter && !solidsOnly) yield return node;
                if (!noPrimary && primary != null && (!solidsOnly || IsSolid(primary))) yield return primary;
                if (secondary != null && (!solidsOnly || IsSolid(secondary))) yield return secondary;

                foreach (var m in GetBarycenterMoons(node, solidsOnly)) yield return m;
                foreach (var m in GetPrimaryMoons(node, solidsOnly)) yield return m;
                foreach (var m in GetSecondaryMoons(node, solidsOnly)) yield return m;
                yield break;
            }

            // Regular planet aggregation
            if (IsPlanet(node))
            {
                if (!noPrimary && (!solidsOnly || IsSolid(node))) yield return node;
                foreach (var m in GetDirectMoons(node, solidsOnly)) yield return m;
                yield break;
            }

            yield break;
        }

        private static bool IsNotHomeWorld(CelestialBody body, CelestialBody home)
        {
            return body != home && !home.orbitingBodies.Contains(body) && !body.orbitingBodies.Contains(home);
        }

        public static IEnumerable<CelestialBody> GetSolidBodies(bool allowHomeMoons = false)
        {
            CelestialBody sun = FlightGlobals.Bodies[0];
            CelestialBody home = FlightGlobals.Bodies.Single(cb => cb.isHomeWorld);

            var roots = GetSystemRoots(sun).ToList();
            foreach (var root in roots)
            {
                foreach (var node in GetPlanetsUnderRoot(root)
                         .Where(cb => allowHomeMoons ? cb != home : IsNotHomeWorld(cb, home)))
                {
                    foreach (var body in GetBodiesUnderNode(node, solidsOnly: true, noPrimary: false))
                        yield return body;
                }
            }
        }

        public static bool loggedRoots = false;
        public static Dictionary<string, bool> loggedIDs = new Dictionary<string, bool>();

        public static IEnumerable<CelestialBody> GetDistinctBodiesForStrategy(string id)
        {
            return GetBodiesForStrategy(id).Distinct();
        }

        public static IEnumerable<CelestialBody> GetBodiesForStrategy(string id)
        {
            CelestialBody sun = FlightGlobals.Bodies[0];
            CelestialBody home = FlightGlobals.Bodies.Single(cb => cb.isHomeWorld);

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
                if (!IsStellarObject(home.referenceBody))
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
                // all terrestrial planets (standalones + barycenters w/ terrestrial primary)
                foreach (var root in roots)
                {
                    foreach (var body in GetTerrestrialPlanetsUnderRoot(root)
                         .Where(cb => IsNotHomeWorld(cb, home)))
                    {
                        bag.Add(body);
                    }
                }
            }
            else if (id == "GasGiantProgram")
            {
                // all gas giants with many solid moons (+ barycenters w/ gas giant primary)
                foreach (var root in roots)
                {
                    foreach (var body in GetGasGiantPlanetsUnderRoot(root)
                         .Where(cb => IsNotHomeWorld(cb, home)))
                    {
                        // check moon count
                        var moons = GetBodiesUnderNode(body, solidsOnly: true).ToList();
                        if (moons.Count >= 1) // at least 1 moon
                            bag.Add(body);
                    }
                }
            }
            else if (id == "ImpactorProbes")
            {
                // all solid bodies (planets, moons, barycenter components, etc.)
                foreach (var root in roots)
                {
                    foreach (var node in GetPlanetsUnderRoot(root)
                         .Where(cb => cb != home)) // allow home moons
                    {
                        foreach (var body in GetBodiesUnderNode(node, solidsOnly: true, noPrimary: false))
                            bag.Add(body);
                    }
                }
            }
            else if (id == "FlyByProbes")
            {
                // All planets (terrestrial & gas giants) orbiting any system root, excluding home
                foreach (var root in roots)
                {
                    foreach (var body in GetPlanetsUnderRoot(root)
                         .Where(cb => IsNotHomeWorld(cb, home)))
                    {
                        bag.Add(body);
                    }
                }
            }
            else
            {
                foreach (CelestialBody body in FlightGlobals.Bodies.Where(cb => BodyType(cb) != CelestialBodyType.NOT_APPLICABLE))
                    bag.Add(body);
            }

            if (!loggedIDs.ContainsKey(id))
            {
                loggedIDs.Add(id, true);
                UnityEngine.Debug.Log("[ReStrategia] Logging ID: " + id);
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

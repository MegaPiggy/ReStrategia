using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using ContractConfigurator;

namespace ReStrategia
{
    /// <summary>
    /// Utility class with version checking functionality.
    /// </summary>
    public static class Version
    {
        public static bool SingularityCheckDone = false;
        public static Assembly SingularityAssembly;
        public static bool KopernicusCheckDone = false;
        public static Assembly KopernicusAssembly;
        public static bool KopernicusExpansionCheckDone = false;
        public static Assembly KopernicusExpansionAssembly;

        private static readonly HashSet<string> MissingAssembliesLogged = new();

        public static Assembly GetAssembly(string assemblyName)
        {
            return AssemblyLoader.loadedAssemblies.SingleOrDefault(a => a.assembly.GetName().Name == assemblyName).assembly;
        }


        /// <summary>
        /// Verify the loaded assembly meets a minimum version number.
        /// </summary>
        /// <param name="name">Assembly name</param>
        /// <param name="version">Minium version</param>
        /// <param name="silent">Silent mode</param>
        /// <returns>The assembly if the version check was successful.  If not, logs and error and returns null.</returns>
        public static Assembly VerifyAssemblyVersion(string name, string version, bool silent = false)
        {
            return VerifyAssemblyVersion(
                name,
                version,
                GetInformationalVersion,
                "informational",
                silent
            );
        }

        /// <summary>
        /// Verify the loaded assembly meets a minimum version number.
        /// </summary>
        /// <param name="name">Assembly name</param>
        /// <param name="version">Minium version</param>
        /// <param name="silent">Silent mode</param>
        /// <returns>The assembly if the version check was successful.  If not, logs and error and returns null.</returns>
        public static Assembly VerifyAssemblyFileVersion(string name, string version, bool silent = false)
        {
            return VerifyAssemblyVersion(
                name,
                version,
                GetFileVersion,
                "file",
                silent
            );
        }

        /// <summary>
        /// Verify the loaded assembly meets a minimum version number.
        /// </summary>
        /// <param name="name">Assembly name</param>
        /// <param name="version">Minium version</param>
        /// <param name="silent">Silent mode</param>
        /// <returns>The assembly if the version check was successful.  If not, logs and error and returns null.</returns>
        private static Assembly VerifyAssemblyVersion(
            string name,
            string version,
            Func<Assembly, string> versionGetter,
            string versionType,
            bool silent
        )
        {
            // Logic courtesy of DMagic
            var assemblies = AssemblyLoader.loadedAssemblies
                .Where(a => a.assembly.GetName().Name == name)
                .ToList();

            var assembly = assemblies.FirstOrDefault();

            if (assembly == null)
            {
                if (MissingAssembliesLogged.Add(name))
                {
                    if (silent)
                    {
                        LogUtil.LogDebug(
                            "Couldn't find assembly for '{0}'!",
                            name
                        );
                    }
                    else
                    {
                        LogUtil.LogError(
                            "Couldn't find assembly for '{0}'!",
                            name
                        );
                    }
                }

                return null;
            }

            if (assemblies.Count > 1)
            {
                LogUtil.LogWarning(
                    StringBuilderCache.Format("Multiple assemblies with name '{0}' found!", name)
                );
            }

            string receivedStr = versionGetter(assembly.assembly);

            if (string.IsNullOrEmpty(receivedStr) || receivedStr == " ")
            {
                receivedStr = assembly.assembly.GetName().Version.ToString();
            }

            System.Version expected = ParseVersion(version);
            System.Version received = ParseVersion(receivedStr);

            if (received >= expected)
            {
                LogUtil.LogDebug(
                    "Version check for '{0}' passed using {1} version. Minimum required is {2}, version found was {3}",
                    name,
                    versionType,
                    version,
                    receivedStr
                );

                return assembly.assembly;
            }

            if (silent)
            {
                LogUtil.LogDebug(
                    "Version check for '{0}' failed using {1} version! Minimum required is {2}, version found was {3}",
                    name,
                    versionType,
                    version,
                    receivedStr
                );
            }
            else
            {
                LogUtil.LogError(
                    "Version check for '{0}' failed using {1} version! Minimum required is {2}, version found was {3}",
                    name,
                    versionType,
                    version,
                    receivedStr
                );
            }

            return null;
        }

        private static string GetInformationalVersion(Assembly assembly)
        {
            var infoVersion = Attribute.GetCustomAttribute(
                assembly,
                typeof(AssemblyInformationalVersionAttribute)
            ) as AssemblyInformationalVersionAttribute;

            return infoVersion?.InformationalVersion;
        }

        private static string GetFileVersion(Assembly assembly)
        {
            var fileVersion = Attribute.GetCustomAttribute(
                assembly,
                typeof(AssemblyFileVersionAttribute)
            ) as AssemblyFileVersionAttribute;

            if (!string.IsNullOrEmpty(fileVersion?.Version))
            {
                return fileVersion.Version;
            }

            return FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
        }

        public static System.Version ParseVersion(string version)
        {
            Match m = Regex.Match(version, @"^[vV]?(\d+)(.(\d+)(.(\d+)(.(\d+))?)?)?");
            int major = m.Groups[1].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[1].Value);
            int minor = m.Groups[3].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[3].Value);
            int build = m.Groups[5].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[5].Value);
            int revision = m.Groups[7].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[7].Value);

            return new System.Version(major, minor, build, revision);
        }

        /// <summary>
        /// Verifies that the Kopernicus version the player has is compatible.
        /// </summary>
        /// <returns>Whether the check passed.</returns>
        public static bool VerifySingularityVersion()
        {
            string minVersion = "0.991";
            if (SingularityAssembly == null || !SingularityCheckDone)
            {
                SingularityAssembly = Version.VerifyAssemblyVersion("Singularity", minVersion, true);
                SingularityCheckDone = true;
            }

            // Check the wrapper is initalized, while we're here
            if (SingularityAssembly != null && !SingularityWrapper.APIReady)
            {
                // Initialize the wrapper
                bool init = SingularityWrapper.Init();
                if (init)
                {
                    LogUtil.LogInfo("Successfully initialized Singularity wrapper.");
                }
                else
                {
                    LogUtil.LogDebug("Couldn't initialize Singularity wrapper.");
                }
            }

            return SingularityAssembly != null;
        }

        /// <summary>
        /// Verifies that the Kopernicus version the player has is compatible.
        /// </summary>
        /// <returns>Whether the check passed.</returns>
        public static bool VerifyKopernicusVersion()
        {
            string minVersion = "1.0.227";
            if (KopernicusAssembly == null || !KopernicusCheckDone)
            {
                KopernicusAssembly = Version.VerifyAssemblyFileVersion("Kopernicus", minVersion, true);
                KopernicusCheckDone = true;
            }

            // Check the wrapper is initalized, while we're here
            if (KopernicusAssembly != null && !KopernicusWrapper.APIReady)
            {
                // Initialize the wrapper
                bool init = KopernicusWrapper.Init();
                if (init)
                {
                    LogUtil.LogInfo("Successfully initialized Kopernicus wrapper.");
                }
                else
                {
                    LogUtil.LogDebug("Couldn't initialize Kopernicus wrapper.");
                }
            }

            return KopernicusAssembly != null;
        }

        /// <summary>
        /// Verifies that the KopernicusExpansion version the player has is compatible.
        /// </summary>
        /// <returns>Whether the check passed.</returns>
        public static bool VerifyKopernicusExpansionVersion()
        {
            string minVersion = "1.0";
            if (KopernicusExpansionAssembly == null && !KopernicusExpansionCheckDone)
            {
                KopernicusExpansionAssembly = Version.VerifyAssemblyVersion("KEX-Wormholes", minVersion, true);
                KopernicusExpansionCheckDone = true;
            }

            // Check the wrapper is initalized, while we're here
            if (KopernicusExpansionAssembly != null && !KopernicusExpansionWrapper.APIReady)
            {
                // Initialize the wrapper
                bool init = KopernicusExpansionWrapper.Init();
                if (init)
                {
                    LogUtil.LogInfo("Successfully initialized Kopernicus Expansion wrapper.");
                }
                else
                {
                    LogUtil.LogDebug("Couldn't initialize Kopernicus Expansion wrapper.");
                }
            }

            return KopernicusExpansionAssembly != null;
        }
    }
}

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

namespace Strategia
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
            // Logic courtesy of DMagic
            var assemblies = AssemblyLoader.loadedAssemblies.Where(a => a.assembly.GetName().Name == name);
            var assembly = assemblies.FirstOrDefault();
            if (assembly != null)
            {
                if (assemblies.Count() > 1)
                {
                    LoggingUtil.LogWarning(typeof(Version), StringBuilderCache.Format("Multiple assemblies with name '{0}' found!", name));
                }

                string receivedStr;

                // First try the informational version
                var ainfoV = Attribute.GetCustomAttribute(assembly.assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                if (ainfoV != null)
                {
                    receivedStr = ainfoV.InformationalVersion;
                }
                // If that fails, use the product version
                else
                {
                    receivedStr = FileVersionInfo.GetVersionInfo(assembly.assembly.Location).ProductVersion;
                }
                // If that still fails, fall back on AssemblyVersion
                if (string.IsNullOrEmpty(receivedStr) || receivedStr == " ")
                {
                    receivedStr = assembly.assembly.GetName().Version.ToString();
                }

                System.Version expected = ParseVersion(version);
                System.Version received = ParseVersion(receivedStr);

                if (received >= expected)
                {
                    LoggingUtil.LogVerbose(typeof(Version), "Version check for '{0}' passed.  Minimum required is {1}, version found was {2}", name, version, receivedStr);
                    return assembly.assembly;
                }
                else
                {
                    LoggingUtil.Log(silent ? LoggingUtil.LogLevel.DEBUG : LoggingUtil.LogLevel.ERROR, typeof(Version), "Version check for '{0}' failed!  Minimum required is {1}, version found was {2}", name, version, receivedStr);
                    return null;
                }
            }
            else
            {
                LoggingUtil.Log(silent ? LoggingUtil.LogLevel.VERBOSE : LoggingUtil.LogLevel.ERROR, typeof(Version), "Couldn't find assembly for '{0}'!", name);
                return null;
            }
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
                SingularityAssembly = Version.VerifyAssemblyVersion("Singularity", minVersion);
                SingularityCheckDone = true;
            }

            // Check the wrapper is initalized, while we're here
            if (SingularityAssembly != null && !SingularityWrapper.APIReady)
            {
                // Initialize the wrapper
                bool init = SingularityWrapper.Init();
                if (init)
                {
                    LoggingUtil.LogInfo(typeof(Version), "Successfully initialized Singularity wrapper.");
                }
                else
                {
                    LoggingUtil.LogDebug(typeof(Version), "Couldn't initialize Singularity wrapper.");
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
            string minVersion = "1.12.227";
            if (KopernicusAssembly == null || !KopernicusCheckDone)
            {
                KopernicusAssembly = Version.VerifyAssemblyVersion("Kopernicus", minVersion);
                KopernicusCheckDone = true;
            }

            // Check the wrapper is initalized, while we're here
            if (KopernicusAssembly != null && !KopernicusWrapper.APIReady)
            {
                // Initialize the wrapper
                bool init = KopernicusWrapper.Init();
                if (init)
                {
                    LoggingUtil.LogInfo(typeof(Version), "Successfully initialized Kopernicus wrapper.");
                }
                else
                {
                    LoggingUtil.LogDebug(typeof(Version), "Couldn't initialize Kopernicus wrapper.");
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
                    LoggingUtil.LogInfo(typeof(Version), "Successfully initialized Kopernicus Expansion wrapper.");
                }
                else
                {
                    LoggingUtil.LogDebug(typeof(Version), "Couldn't initialize Kopernicus Expansion wrapper.");
                }
            }

            return KopernicusExpansionAssembly != null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;
using Kopernicus.Configuration;

namespace ReStrategia
{
    /// <summary>
    /// Special MonoBehaviour for expanding special config files into strategies.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ConfigExpander : MonoBehaviour
    {
        Dictionary<string, int> names = new Dictionary<string, int>();

        public void Awake()
        {
            Debug.Log("[ReStrategia] Expanding configuration");
            DoDependencyCheck();
            DoLoad();
            DontDestroyOnLoad(this);
        }

        public void ModuleManagerPostLoad()
        {
            StartCoroutine(LoadCoroutine());
        }

        public void DoDependencyCheck()
        {
            if (Version.VerifyAssemblyVersion("CustomBarnKit", "1.0.0") == null)
            {
                var ainfoV = Attribute.GetCustomAttribute(GetType().Assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                string title = "ReStrategia " + ainfoV.InformationalVersion + " Message";
                string message = "ReStrategia requires Custom Barn Kit to function properly.  ReStrategia is currently disabled, and will automatically re-enable itself when Custom Barn Kit is installed.";
                DialogGUIButton dialogOption = new DialogGUIButton("Okay", new Callback(DoNothing), true);
                PopupDialog.SpawnPopupDialog(new MultiOptionDialog("StrategiaMsg", message, title, UISkinManager.GetSkin("default"), dialogOption), false, UISkinManager.GetSkin("default"));
            }
        }

        private void DoNothing() { }

        public void DoLoad()
        {
            IEnumerator<YieldInstruction> enumerator = LoadCoroutine();
            while (enumerator.MoveNext()) { }
        }

        public IEnumerator<YieldInstruction> LoadCoroutine()
        {
            // Do Celestial Body expansion
            foreach (UrlDir.UrlConfig config in GameDatabase.Instance.GetConfigs("STRATEGY_BODY_EXPAND"))
            {
                ConfigNode node = config.config;
                Debug.Log("[ReStrategia] Expanding " + node.GetValue("id"));
                foreach (CelestialBody body in CelestialBodyUtil.GetDistinctBodiesForStrategy(node.GetValue("id")))
                {
                    try
                    {
                        // Duplicate the node
                        ConfigNode newStrategy = ExpandNode(node, body);
                        newStrategy.name = "STRATEGY";

                        // Name must be unique
                        string name = node.GetValue("name");
                        int current;
                        names.TryGetValue(name, out current);
                        names[name] = current + 1;
                        name = name + current;
                        newStrategy.SetValue("name", name);

                        // Duplicate effect nodes
                        foreach (ConfigNode effect in node.GetNodes("EFFECT"))
                        {
                            ConfigNode newEffect = ExpandNode(effect, body);
                            newStrategy.AddNode(newEffect);
                        }

                        // Add the cloned strategy to the config file
                        Debug.Log("[ReStrategia] Generated strategy '" + newStrategy.GetValue("title") + "'");
                        config.parent.configs.Add(new UrlDir.UrlConfig(config.parent, newStrategy));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[ReStrategia] Failed to generate strategy for body '" + body.name + "'\n" + e);
                    }

                    yield return null;
                }
            }

            // Do level-based expansion
            foreach (UrlDir.UrlConfig config in GameDatabase.Instance.GetConfigs("STRATEGY_LEVEL_EXPAND"))
            {
                ConfigNode node = config.config;
                Debug.Log("[ReStrategia] Expanding " + node.GetValue("name"));

                int count = ConfigNodeUtil.ParseValue<int>(node, "factorSliderSteps");
                for (int level = 1; level <= count; level++)
                {
                    try
                    {
                        // Duplicate the node
                        ConfigNode newStrategy = ExpandNode(node, level);
                        if (newStrategy == null)
                        {
                            continue;
                        }
                        newStrategy.name = "STRATEGY";

                        // Name must be unique
                        newStrategy.SetValue("name", newStrategy.GetValue("name") + level);

                        // Set the title
                        newStrategy.SetValue("title", newStrategy.GetValue("title") + " " + StringUtil.IntegerToRoman(level));

                        // Set the group tag
                        newStrategy.SetValue("groupTag", newStrategy.GetValue("groupTag") + StringUtil.IntegerToRoman(level));

                        // Set the icon
                        newStrategy.SetValue("icon", newStrategy.GetValue("icon") + level);

                        if (newStrategy.HasValue("requiredReputation"))
                        {
                            float requiredReputation = ConfigNodeUtil.ParseValue<float>(newStrategy, "requiredReputation");
                            newStrategy.SetValue("requiredReputationMin", requiredReputation.ToString(), true);
                            newStrategy.SetValue("requiredReputationMax", requiredReputation.ToString(), true);
                        }

                        // Duplicate effect nodes
                        foreach (ConfigNode effect in node.GetNodes("EFFECT"))
                        {
                            ConfigNode newEffect = ExpandNode(effect, level);
                            if (newEffect != null)
                            {
                                newStrategy.AddNode(newEffect);
                            }
                        }

                        // Add the cloned strategy to the config file
                        Debug.Log("[ReStrategia] Generated strategy '" + newStrategy.GetValue("title") + "'");
                        config.parent.configs.Add(new UrlDir.UrlConfig(config.parent, newStrategy));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[ReStrategia] Failed to generate strategy for level " + level + "\n" + e);
                    }

                    yield return null;
                }
            }
        }

        public ConfigNode ExpandNode(ConfigNode node, int level)
        {
            // Handle min/max level
            int minLevel = ConfigNodeUtil.ParseValue<int>(node, "minLevel", 1);
            int maxLevel = ConfigNodeUtil.ParseValue<int>(node, "maxLevel", 3);
            if (level < minLevel || level > maxLevel)
            {
                return null;
            }

            ConfigNode newNode = new ConfigNode(node.name);

            foreach (ConfigNode.Value pair in node.values)
            {
                newNode.AddValue(pair.name, FormatString(pair.value));
            }

            foreach (ConfigNode overrideNode in node.GetNodes())
            {
                if (overrideNode.name == "EFFECT")
                {
                    continue;
                }

                if (overrideNode.HasValue(level.ToString()))
                {
                    if (newNode.HasValue(overrideNode.name))
                    {
                        newNode.RemoveValue(overrideNode.name);
                    }
                    if (overrideNode.HasValue(level.ToString()))
                    {
                        newNode.AddValue(overrideNode.name, FormatString(overrideNode.GetValue(level.ToString())));
                    }
                }
            }

            return newNode;
        }

        public ConfigNode ExpandNode(ConfigNode node, CelestialBody body)
        {
            ConfigNode newNode = new ConfigNode(node.name);
            CelestialBody displayBody = CelestialBodyUtil.IsSigmaBinary(body) ? CelestialBodyUtil.GetBarycenterPrimary(body) : body;

            foreach (ConfigNode.Value pair in node.values)
            {
                string value = pair.value;
                if (node.HasNode(pair.name))
                {
                    ConfigNode overrideNode = node.GetNode(pair.name);
                    if (overrideNode.HasValue(displayBody.name))
                    {
                        value = overrideNode.GetValue(displayBody.name);
                    }
                }

                if (value.StartsWith("@"))
                {
                    foreach (string listValue in ExpandList(value, body))
                    {
                        newNode.AddValue(pair.name, listValue);
                    }
                }
                else
                {
                    newNode.AddValue(pair.name, FormatString(FormatBodyString(value, body)));
                }
            }

            return newNode;
        }
        
        public IEnumerable<string> ExpandList(string list, CelestialBody body)
        {
            if (list == "@bodies")
            {
                foreach (CelestialBody cb in CelestialBodyUtil.GetBodiesUnderNode(body, solidsOnly: false, noBarycenter: false, noPrimary: false))
                {
                    yield return cb.name;
                }
            }
            else if (list == "@primarySecondary")
            {
                if (CelestialBodyUtil.IsBarycenter(body))
                {
                    foreach (CelestialBody cb in CelestialBodyUtil.GetBarycenterPrimaryAndSecondary(body))
                    {
                        yield return cb.name;
                    }
                }
                else
                    yield return body.name;
            }
            else if (list == "@solidMoons")
            {
                foreach (CelestialBody cb in CelestialBodyUtil.GetBodiesUnderNode(body, solidsOnly: true, noBarycenter: true, noPrimary: true))
                {
                    yield return cb.name;
                }
            }
            else
            {
                throw new Exception("Unhandled tag: " + list);
            }
        }

        public string FormatBodyString(string input, CelestialBody body)
        {
            CelestialBody displayBody  = body.GetDisplayBody();
            CelestialBody primary = body.GetPrimaryBody();
            string result = input.
                Replace("$body", displayBody.name).
                Replace("$culledName", displayBody.GetCulledName()).
                Replace("$primaryBody", primary.name).
                Replace("$theBody", primary.CleanDisplayName()).
                Replace("$primaryAndSecondary", body.GetPrimaryAndSecondaryList("and")).
                Replace("$primaryOrSecondary", body.GetPrimaryAndSecondaryList("or"));

            if (result.Contains("$theBodies"))
            {
                result = result.Replace("$theBodies", CelestialBodyUtil.BodyList(CelestialBodyUtil.GetBodiesUnderNode(body, solidsOnly: false, noBarycenter: true, noPrimary: false), "and"));
            }

            var hasChildBodies = result.Contains("$childBodies");
            var hasChildBodyCount = result.Contains("$childBodyCount");
            var hasGasGiantMoons = result.Contains("$gasGiantMoons");
            if (hasChildBodies || hasChildBodyCount || hasGasGiantMoons)
            {
                var childBodies = CelestialBodyUtil.GetBodiesUnderNode(body, solidsOnly: true, noBarycenter: true, noPrimary: true);
                if (hasChildBodies)
                {
                    result = result.Replace("$childBodies", CelestialBodyUtil.BodyList(childBodies, "and"));
                }
                var childBodyCount = childBodies.Count();
                if (hasChildBodyCount)
                {
                    if (childBodyCount >= 1)
                        result = result.Replace("$childBodyCount", StringUtil.IntegerToRoman(childBodyCount));
                    else
                        result = result.Replace(" $childBodyCount", "");
                }
                if (hasGasGiantMoons)
                {
                    if (childBodyCount >= 1)
                        result = result.Replace("$gasGiantMoons", "each of " + primary.CleanDisplayName() + "'s moons");
                    else
                        result = result.Replace("$gasGiantMoons", primary.CleanDisplayName() + "'s moon");
                }
            }

            return result;
        }

        public string FormatString(string input)
        {
            string result = input.
                Replace("$homeWorld", FlightGlobals.Bodies.First(cb => cb.isHomeWorld).name);
            return result;
        }
    }
}

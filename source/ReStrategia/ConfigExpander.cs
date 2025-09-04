using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;

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
            if (ContractConfigurator.Util.Version.VerifyAssemblyVersion("CustomBarnKit", "1.0.0") == null)
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

            foreach (ConfigNode.Value pair in node.values)
            {
                string value = pair.value;
                if (node.HasNode(pair.name))
                {
                    ConfigNode overrideNode = node.GetNode(pair.name);
                    if (overrideNode.HasValue(body.name))
                    {
                        value = overrideNode.GetValue(body.name);
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
                yield return body.name;
                foreach (CelestialBody child in body.orbitingBodies)
                {
                    yield return child.name;
                }
            }
            else
            {
                throw new Exception("Unhandled tag: " + list);
            }
        }

        public string FormatBodyString(string input, CelestialBody body)
        {
            string result = input.
                Replace("$body", body.name).
                Replace("$theBody", body.CleanDisplayName());

            if (result.Contains("$theBodies"))
            {
                result = result.Replace("$theBodies", CelestialBodyUtil.BodyList(Enumerable.Repeat(body, 1).Union(body.orbitingBodies), "and"));
            }
            if (result.Contains("$childBodies"))
            {
                result = result.Replace("$childBodies", CelestialBodyUtil.BodyList(body.orbitingBodies, "and"));
            }
            if (result.Contains("$childBodyCount"))
            {
                var childBodyCount = body.orbitingBodies.Count();
                if (childBodyCount == 0) 
                    result = result.Replace(" $childBodyCount", "");
                else
                    result = result.Replace("$childBodyCount", StringUtil.IntegerToRoman(childBodyCount));
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

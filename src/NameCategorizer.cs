using System;
using System.Collections.Generic;

namespace VesselCategorizer
{
    /// <summary>
    /// Categorizer utility that suggests a ship category based on the ship's name, as controlled by
    /// rules in config.  For example, if the config has an rule like, "Probe = explorer", then a vessel
    /// with "explorer" in its name would get assigned the "Probe" category, regardless of what
    /// parts it contains.
    /// 
    /// Note that it's possible for more than one rule to match a ship's name. For example, you
    /// could have a rule "Station = foo" and another rule "Base = bar", in which case a ship
    /// named "Foobar" would match both rules.  In such cases, order matters within the config
    /// file:  first one wins.  That is, whichever matching rule comes first within the config
    /// file will determine the vessel type.
    /// 
    /// </summary>
    public static class NameCategorizer
    {
        /// <summary>
        /// Used by the Loader class in this mod to load the config that drives this categorizer.
        /// </summary>
        public const string CONFIG_NODE_NAME = "NamingRules";

        private static List<KeyValuePair<string, VesselType>> _namingRules;

        /// <summary>
        /// Try to categorize the vessel based on its name.  Returns true if it did so; false
        /// if the name was unrecognized (i.e. no match found) and therefore type was left alone.
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns></returns>
        public static bool TryCategorize(Vessel vessel)
        {
            string canonicalName = Canonicalize(vessel.vesselName);
            for (int i = 0; i < _namingRules.Count; ++i)
            {
                KeyValuePair<string, VesselType> rule = _namingRules[i];
                if (canonicalName.Contains(rule.Key))
                {
                    // Got a match!
                    Logging.Log("Setting type of " + vessel.vesselName + " to " + rule.Value + " (name matches '" + rule.Key + "')");
                    vessel.vesselType = rule.Value;
                    return true;
                }
            }

            // Nope, no match found.
            return false;
        }

        /// <summary>
        /// Config-loader method.  This is called from the Loader class in this mod, once, at
        /// KSP startup.  It loads all necessary config from the relevant node, named above.
        /// </summary>
        /// <param name="config"></param>
        public static void LoadConfig(ConfigNode config)
        {
            Logging.Log("Loading naming rules");
            _namingRules = new List<KeyValuePair<string, VesselType>>();
            for (int i = 0; i < config.values.Count; ++i)
            {
                ConfigNode.Value rule = config.values[i];
                VesselType vesselType;
                try
                {
                    vesselType = (VesselType)Enum.Parse(typeof(VesselType), rule.name);
                }
                catch (ArgumentException)
                {
                    Logging.Error(CONFIG_NODE_NAME + " config: Invalid vessel type '" + rule.name + "' specified, skipping");
                    continue;
                }
                string matchString = Canonicalize(rule.value);
                Logging.Log(rule.name + " = '" + matchString + "'");
                _namingRules.Add(new KeyValuePair<string, VesselType>(matchString, vesselType));
            }
        }

        /// <summary>
        /// Canonicalize a string to facilitate matching.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string Canonicalize(string input)
        {
            return input.ToLower();
        }
    }
}

using UnityEngine;

namespace VesselCategorizer
{
    /// <summary>
    /// Runs once when the game hits the main menu on startup. Loads custom VesselCategorizer
    /// config used in various places.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class Loader : MonoBehaviour
    {
        // The master config node for this mod.
        private const string MASTER_NODE_NAME = "VesselCategorizer";

        /// <summary>
        /// Delegate that subnodes provide to allow their config to be loaded.
        /// </summary>
        /// <param name="node"></param>
        private delegate void ConfigLoader(ConfigNode node);

        /// <summary>
        /// Here when the script starts up.
        /// </summary>
        public void Start()
        {
            Logging.Log("Starting up");
            UrlDir.UrlConfig[] configs = GameDatabase.Instance.GetConfigs(MASTER_NODE_NAME);
            if (configs.Length < 1)
            {
                Logging.Error("Can't find main " + MASTER_NODE_NAME + " config node! Some features will be inoperable.");
                return;
            }
            ConfigNode masterNode = configs[0].config;
            ProcessMasterNode(masterNode);
        }

        /// <summary>
        /// Process the main IndicatorLights config node.
        /// </summary>
        /// <param name="masterNode"></param>
        private static void ProcessMasterNode(ConfigNode masterNode)
        {
            TryProcessChildNode(masterNode, NameCategorizer.CONFIG_NODE_NAME, NameCategorizer.LoadConfig);
            // FUTURE: any additional config nodes would be handled by adding a reference here
        }

        /// <summary>
        /// Looks for a child with the specified name, and delegates to it if found.
        /// </summary>
        /// <param name="masterNode"></param>
        /// <param name="childName"></param>
        /// <param name="loader"></param>
        private static void TryProcessChildNode(ConfigNode masterNode, string childName, ConfigLoader loader)
        {
            ConfigNode child = masterNode.nodes.GetNode(childName);
            if (child == null)
            {
                Logging.Warn("Child node " + childName + " of master config node " + MASTER_NODE_NAME + " not found, skipping");
            }
            else
            {
                loader(child);
            }
        }
    }
}

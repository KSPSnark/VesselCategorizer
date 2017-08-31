using UnityEngine;

namespace VesselCategorizer
{
    /// <summary>
    /// Categorizes (i.e. chooses a vessel type for) each vessel upon launch, in a custom
    /// way defined in config.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class VesselCategorizer : MonoBehaviour
    {
        public void Awake()
        {
            GameEvents.onVesselChange.Add(OnVesselChange);
        }

        public void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(OnVesselChange);
        }

        /// <summary>
        /// Here when switching to a different vessel, loading, or launching.
        /// </summary>
        /// <param name="vessel"></param>
        private void OnVesselChange(Vessel vessel)
        {
            // We only care about the specific case where we're launching a new ship
            if ((vessel != null) && (vessel.situation == Vessel.Situations.PRELAUNCH))
            {
                OnVesselLaunch(vessel);
            }
        }

        /// <summary>
        /// Here when a new vessel is launched to the launchpad. This is where we set the vessel type.
        /// </summary>
        /// <param name="vessel"></param>
        private void OnVesselLaunch(Vessel vessel)
        {
            // First, see if the user manually assigned a vessel type in the editor.
            if (ModuleVesselCategorizer.TryCategorize(vessel)) return;

            // Nope. Well, can we assign a vessel type based on the name?
            if (NameCategorizer.TryCategorize(vessel)) return;

            // Still nope.  Okay, all out of ideas, we'll just leave it alone.
            Logging.Log("No changes made to vessel type for " + vessel.vesselName);
        }
    }
}

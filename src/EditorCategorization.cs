using System.Collections.Generic;
using UnityEngine;

namespace VesselCategorizer
{
    /// <summary>
    /// Editor logic to support ModuleVesselCategorizer. We need this so that when the user
    /// adds new parts to a ship *after* setting a vessel type, the new parts will have
    /// their settings properly initialized to match the rest of the ship. (This only becomes
    /// relevant when adding more than one command pod to a ship.)
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class EditorCategorization : MonoBehaviour
    {
        private readonly List<ModuleVesselCategorizer> toInitialize = new List<ModuleVesselCategorizer>();

        public void Awake()
        {
            GameEvents.onEditorPodPicked.Add(OnEditorPodPicked);
            GameEvents.onEditorPartPlaced.Add(OnEditorPartPlaced);
        }

        public void OnDestroy()
        {
            GameEvents.onEditorPodPicked.Remove(OnEditorPodPicked);
            GameEvents.onEditorPartPlaced.Remove(OnEditorPartPlaced);
        }

        /// <summary>
        /// Here when the initial root part is picked to start a new ship.
        /// </summary>
        /// <param name="part"></param>
        private void OnEditorPodPicked(Part part)
        {
            toInitialize.Clear();
            CollectToInitializerList(part, toInitialize);
            Initialize(toInitialize);
        }

        /// <summary>
        /// Here when a part is placed on the ship. Note that only the *single* part
        /// actually placed by the user fires this event. If the part has children,
        /// or symmetry counterparts, then those other parts don't get the notification.
        /// 
        /// Apparently the method also gets called-- with a null part-- any time you
        /// have a part that's not attached to the ship (e.g. you've picked it but not
        /// placed it) and you delete it.  So it needs to handle that case.
        /// </summary>
        /// <param name="part"></param>
        private void OnEditorPartPlaced(Part part)
        {
            if (part == null) return;

            // Collect a list of modules to initialize from the part and all its children.
            toInitialize.Clear();        
            CollectToInitializerList(part, toInitialize);

            // Also any symmetry counterparts it may have, and *their* children.
            if (part.symmetryCounterparts != null)
            {
                for (int i = 0; i < part.symmetryCounterparts.Count; ++i)
                {
                    Part counterpart = part.symmetryCounterparts[i];
                    if (!ReferenceEquals(this, counterpart))
                    {
                        CollectToInitializerList(counterpart, toInitialize);
                    }
                }
            }

            // Now we've got a full list of ModuleVesselCategorizer instances from
            // *all* the newly added parts, so we can initialize them.
            Initialize(toInitialize);
        }

        /// <summary>
        /// Initialize all modules in the supplied list so that they have the same
        /// selection value as what's already on the current ship construct, if there is one.
        /// </summary>
        /// <param name="toInitialize"></param>
        private static void Initialize(List<ModuleVesselCategorizer> toInitialize)
        {
            if (toInitialize.Count < 1) return; // nothing to do
            try
            {
                ModuleVesselCategorizer existing = FindFirstNotInList(toInitialize);
                if (existing == null) return;
                for (int i = 0; i < toInitialize.Count; ++i)
                {
                    toInitialize[i].vesselTypeSelection = existing.vesselTypeSelection;
                }
            }
            finally
            {
                toInitialize.Clear();
            }
        }

        internal static ShipConstruct CurrentShipConstruct
        {
            get
            {
                return (EditorLogic.fetch == null) ? null : EditorLogic.fetch.ship;
            }
        }

        /// <summary>
        /// Scan the provided part and all its children recursively, looking for any of them that have
        /// a ModuleVesselCategorizer.  Add any found modules to the list.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="toInitialize"></param>
        private static void CollectToInitializerList(Part root, List<ModuleVesselCategorizer> toInitialize)
        {
            if (root == null) return;
            ModuleVesselCategorizer module = ModuleVesselCategorizer.FindFirst(root);
            if (module != null) toInitialize.Add(module);
            if (root.children == null) return;
            for (int i = 0; i < root.children.Count; ++i)
            {
                CollectToInitializerList(root.children[i], toInitialize);
            }
        }

        /// <summary>
        /// Finds the first ModuleVesselCategorizer on the current ship construct, aside
        /// from any module in the supplied list. Returns that module, or null if none exists.
        /// </summary>
        /// <param name="excluded"></param>
        /// <returns></returns>
        private static ModuleVesselCategorizer FindFirstNotInList(List<ModuleVesselCategorizer> excluded)
        {
            ShipConstruct ship = CurrentShipConstruct;
            if (ship == null) return null;
            for (int i = 0; i < ship.Parts.Count; ++i)
            {
                Part part = ship.Parts[i];
                ModuleVesselCategorizer module = ModuleVesselCategorizer.FindFirst(part);
                if (module == null) continue;
                if (excluded.Contains(module)) continue;
                return module;
            }
            return null; // nope, there isn't one
        }
    }
}
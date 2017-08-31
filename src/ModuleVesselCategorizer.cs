using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace VesselCategorizer
{
    /// <summary>
    /// Allows selecting vessel type on a part that has it.
    /// </summary>
    class ModuleVesselCategorizer : PartModule
    {
        // is "Default", then each allowable vessel type
        private static readonly string[] _optionText;

        // is each allowable vessel type (count is 1 shorter than _optionText)
        private static readonly VesselType[] _vesselTypes;

        [KSPField(guiActiveEditor = true, isPersistant = true, guiName = "#autoLOC_900677")]
        [UI_Cycle(affectSymCounterparts = UI_Scene.Editor, stateNames = new string[]{ "Default" }, scene = UI_Scene.Editor)]
        public int vesselTypeSelection = 0;
        private BaseField SelectionField {  get { return Fields["vesselTypeSelection"];  } }

        /// <summary>
        /// Used to prevent sending selection-changed notifications when the change is being
        /// done by another VesselCategorizerModule.
        /// </summary>
        private bool triggerIsSuppressed = false;

        static ModuleVesselCategorizer()
        {
            List<string> texts = new List<string>();
            List<VesselType> types = new List<VesselType>();
            texts.Add("Default");
            foreach (VesselType vesselType in Enum.GetValues(typeof(VesselType)))
            {
                switch (vesselType)
                {
                    case global::VesselType.Debris:
                    case global::VesselType.SpaceObject:
                    case global::VesselType.Unknown:
                    case global::VesselType.EVA:
                    case global::VesselType.Flag:
                        // keep these out of the list
                        break;
                    default:

                        texts.Add(GetLocalizedDescriptionOf(vesselType));
                        types.Add(vesselType);
                        break;
                }
            }
            _optionText = texts.ToArray();
            _vesselTypes = types.ToArray();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            SelectionField.uiControlEditor.onFieldChanged = OnSelectionChanged;
            UI_Cycle cycle = SelectionField.uiControlEditor as UI_Cycle;
            cycle.stateNames = _optionText;
        }

        /// <summary>
        /// Try to categorize the specified vessel based on any VesselCategorizerModule
        /// it contains. Returns true if the vessel type was set, false if it was left alone
        /// because either there's no VesselCategorizerModule, or else because it was
        /// set to "default".
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns></returns>
        public static bool TryCategorize(Vessel vessel)
        {
            if (vessel == null) return false;
            if (vessel.Parts == null) return false;
            int vesselTypeSelection = -1;
            List<ModuleVesselCategorizer> toRemove = new List<ModuleVesselCategorizer>();
            for (int partIndex = 0; partIndex < vessel.Parts.Count; ++partIndex)
            {
                Part part = vessel.Parts[partIndex];
                toRemove.Clear();
                for (int moduleIndex = 0; moduleIndex < part.Modules.Count; ++moduleIndex)
                {
                    ModuleVesselCategorizer module = part.Modules[moduleIndex] as ModuleVesselCategorizer;
                    if (module == null) continue;
                    if (vesselTypeSelection < 0) vesselTypeSelection = module.vesselTypeSelection;
                    toRemove.Add(module);
                }
                for (int i = 0; i < toRemove.Count; ++i)
                {
                    // We remove all the modules from the ship because they've served their purpose.
                    // We just extract the selected vessel type, once
                    part.Modules.Remove(toRemove[i]);
                }
            } // for each part on the vessel
            if (vesselTypeSelection < 1) return false; // nothing found, or else selection was "default"
            int typeIndex = vesselTypeSelection - 1;
            if (typeIndex >= _vesselTypes.Length) return false; // wtf? should never happen
            VesselType assignedType = _vesselTypes[typeIndex];

            Logging.Log("Setting type of " + vessel.vesselName + " to " + assignedType + " (manual user selection in editor)");
            vessel.vesselType = assignedType;
            return true;
        }

        /// <summary>
        /// Finds the first ModuleVesselCategorizer on the part, or null if none.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public static ModuleVesselCategorizer FindFirst(Part part)
        {
            if (part == null) return null;
            for (int i = 0; i < part.Modules.Count; ++i)
            {
                ModuleVesselCategorizer module = part.Modules[i] as ModuleVesselCategorizer;
                if (module != null) return module;
            }
            return null;
        }

        /// <summary>
        /// Here when the value of vesselTypeSelection changes. It updates all
        /// the other VesselCategorizerModules on the ship  to have the same value
        /// as itself.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        private void OnSelectionChanged(BaseField field, object value)
        {
            if (triggerIsSuppressed) return;
            ShipConstruct construct = EditorCategorization.CurrentShipConstruct;
            if (construct == null) return;
            for (int i = 0; i < construct.Count; ++i)
            {
                Part part = construct.parts[i];
                if (part == null) continue;
                for (int j = 0; j < part.Modules.Count; ++j)
                {
                    ModuleVesselCategorizer module = part.Modules[j] as ModuleVesselCategorizer;
                    if (module == null) continue;
                    if (ReferenceEquals(module, this)) continue;
                    module.triggerIsSuppressed = true;
                    module.vesselTypeSelection = vesselTypeSelection;
                    module.triggerIsSuppressed = false;
                }
            }
        }

        private static string GetLocalizedDescriptionOf(VesselType vesselType)
        {
            FieldInfo info = typeof(VesselType).GetField(vesselType.ToString());
            if (info == null) return vesselType.ToString();
            DescriptionAttribute[] attributes = (DescriptionAttribute[])info.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if ((attributes == null) || (attributes.Length < 1)) return vesselType.ToString();
            return attributes[0].Description;
        }
    }
}

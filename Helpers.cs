using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BattleTech;

namespace ArmorRepair
{
    class Helpers
    {

        /* REPAIR PRIORITIES
         * Set priority order of chassis locations for repairs (key 0 = highest priority)
         * 
         * These are ordered so that structure or armor repair work orders target the most important locations to the player first.
         * This is just a gameplay / usability tweak to allow them to cancel a work order before it completes, but still have key locations like the head, CT and torsos repaired etc.
         */
        public static Dictionary<int, ChassisLocations> repairPriorities = new Dictionary<int, ChassisLocations>
        {
            { 0, ChassisLocations.CenterTorso },
            { 1, ChassisLocations.Head },
            { 2, ChassisLocations.LeftTorso },
            { 3, ChassisLocations.RightTorso },
            { 4, ChassisLocations.LeftLeg },
            { 5, ChassisLocations.RightLeg },
            { 6, ChassisLocations.LeftArm },
            { 7, ChassisLocations.RightArm }
        };

        // Create a new parent / base work order of the generic MechLab type
        public static WorkOrderEntry_MechLab CreateBaseMechLabOrder(SimGameState __instance, MechDef mech)
        {
            string mechGUID = mech.GUID;
            string mechName = "Unknown";
            mechName = mech.Description.Name;

            Logger.LogDebug("Creating base MechLab work order with params - " +
                WorkOrderType.MechLabGeneric.ToString() +
                " | WO String: MechLab-BaseWorkOrder" +
                " | WO Description: " + string.Format("Modify 'Mech - {0}", mechName) +
                " | Mech GUID: " + mechGUID +
                " | Cost: 0" +
                " | Toast Description: " + string.Format(__instance.Constants.Story.GeneralMechWorkOrderCompletedText, mechName)
            );

            return new WorkOrderEntry_MechLab(
                    WorkOrderType.MechLabGeneric,
                    "MechLab-BaseWorkOrder",
                    string.Format("Modify 'Mech - {0}", mechName),
                    mechGUID,
                    0,
                    string.Format(__instance.Constants.Story.GeneralMechWorkOrderCompletedText, mechName)
            );
        }

        // Evaluates whether a given mech needs any armor repaired
        public static bool CheckArmorDamage(MechDef mech)
        {
            // Default to not requesting any armor repair
            bool mechNeedsArmor = false;

            // Using the repair priority for loop here as it is faster and simpler than foreach'ing over ChassisLocations and filtering out ones that don't have armor
            for (int index = 0; index < repairPriorities.Count; index++)
            {
                // Set current ChassisLocation
                ChassisLocations thisLoc = repairPriorities.ElementAt(index).Value;
                // Get current mech location loadout from the looped chassis definitions
                LocationLoadoutDef thisLocLoadout = mech.GetLocationLoadoutDef(thisLoc);
                // Friendly name for this location
                string thisLocName = thisLoc.ToString();

                // Work out difference of armor lost for each location - default to 0
                int armorDifference = 0;

                // Consider rear armour in difference calculation if this is a RT, CT or LT
                if (thisLocLoadout == mech.CenterTorso || thisLocLoadout == mech.RightTorso || thisLocLoadout == mech.LeftTorso)
                {
                    armorDifference = (int)Mathf.Abs(thisLocLoadout.CurrentArmor - thisLocLoadout.AssignedArmor) + (int)Mathf.Abs(thisLocLoadout.CurrentRearArmor - thisLocLoadout.AssignedRearArmor);
                }
                else
                {
                    armorDifference = (int)Mathf.Abs(thisLocLoadout.CurrentArmor - thisLocLoadout.AssignedArmor);
                }

                // If any difference betwen the location's current and assigned armor is detected, flag this mech for armor repair
                if (armorDifference > 0)
                {
                    Logger.LogDebug(mech.Name + " requires armor repair based on armor loss from: " + thisLocName);
                    mechNeedsArmor = true;
                    break; // Stop evaluating other locations once a repair requirement is determined on any location (no point checking further)
                }
            }

            if (!mechNeedsArmor)
            {
                Logger.LogInfo(mech.Name + " does not require armor repairs.");
            }

            return mechNeedsArmor;
        }

        // Evaluates whether a given mech needs any structure repaired
        public static bool CheckStructureDamage(MechDef mech)
        {

            // Default to not requesting any structure repair
            bool mechNeedsRepair = false;

            // Using the repair priority for loop here as it is faster and simpler than foreach'ing over ChassisLocations and filtering out ones that don't have armor
            for (int index = 0; index < repairPriorities.Count; index++)
            {
                // Set current ChassisLocation
                ChassisLocations thisLoc = repairPriorities.ElementAt(index).Value;
                // Get current mech location loadout from the looped chassis definitions
                LocationLoadoutDef thisLocLoadout = mech.GetLocationLoadoutDef(thisLoc);
                // Friendly name for this location
                string thisLocName = thisLoc.ToString();

                // Work out difference of armor lost for each location - default to 0
                int structureDifference = 0;
                float currentStructure = thisLocLoadout.CurrentInternalStructure;
                float definedStructure = mech.GetChassisLocationDef(thisLoc).InternalStructure;
                structureDifference = (int)Mathf.Abs(currentStructure - definedStructure);

                // If any difference betwen the location's current and assigned armor is detected, flag this mech for armor repair
                if (structureDifference > 0)
                {
                    Logger.LogDebug(mech.Name + " requires structure repair based on damage to: " + thisLocName);
                    mechNeedsRepair = true;
                    break; // Stop evaluating other locations once a repair requirement is determined
                }

            }

            return mechNeedsRepair;

        }


    }
}

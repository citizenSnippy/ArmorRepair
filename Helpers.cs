using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BattleTech;
using System;

namespace ArmorRepair
{
    class Helpers
    {
        /* Submits a Mech Lab Work Order to our temporary queue, it will be held there until the player decides whether to let Yang run the repairs or not.*/
        public static void SubmitTempWorkOrder(SimGameState simGame, WorkOrderEntry_MechLab newMechLabWorkOrder, MechDef mech)
        {
            try
            {
                Logger.LogDebug("Adding base MechLab WO to temporary queue.");
                Globals.tempMechLabQueue.Add(newMechLabWorkOrder);
                Logger.LogDebug("Number of work orders in temp queue: " + Globals.tempMechLabQueue.Count.ToString());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        /* Submits a Mech Lab Work Order to the game's Mech Lab queue to actually be processed */
        public static void SubmitWorkOrder(SimGameState simGame, WorkOrderEntry_MechLab newMechLabWorkOrder)
        {
            try
            {
                Logger.LogDebug("Begin SubmitWorkOrder(): ");

                // Now that all WO subentries are added, insert the base MechLab work order to the game's Mech Lab Work Order Queue as the highest priority (index 0)
                simGame.MechLabQueue.Insert(0, newMechLabWorkOrder);
                // Call this to properly Initialize the base Mech Lab WO and make it visible in the Mech Lab queue UI / timeline etc.
                simGame.InitializeMechLabEntry(newMechLabWorkOrder, newMechLabWorkOrder.GetCBillCost());
                // Force an update of the Mech Lab queue, false is to tell it a day isn't passing.
                simGame.UpdateMechLabWorkQueue(false);

                // Simple cost debugging for the log
                foreach (WorkOrderEntry subEntries in newMechLabWorkOrder.SubEntries)
                {
                    Logger.LogDebug(subEntries.Description + " Repair Tech Costs: " + subEntries.GetCost());
                }

                Logger.LogInfo(simGame.GetMechByID(newMechLabWorkOrder.MechID).Name + " Repair Costs are - techCost: " + newMechLabWorkOrder.GetCost() + " | CBill Cost: " + newMechLabWorkOrder.GetCBillCost());
                Logger.LogInfo("=====================================================================");

                // Deduct the total CBill costs of the WO from player inventory. For some reason this isn't done automatically via the HBS WO system.
                simGame.AddFunds(-newMechLabWorkOrder.GetCBillCost(), "ArmorRepair", true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            
        }

        // Create a new parent / base work order of the generic MechLab type
        public static WorkOrderEntry_MechLab CreateBaseMechLabOrder(SimGameState __instance, MechDef mech)
        {
            try
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
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return null;
            }
            
        }

        // Evaluates whether a given mech needs any armor repaired
        public static bool CheckArmorDamage(MechDef mech)
        {
            // Default to not requesting any armor repair
            bool mechNeedsArmor = false;

            // Using the repair priority for loop here as it is faster and simpler than foreach'ing over ChassisLocations and filtering out ones that don't have armor
            for (int index = 0; index < Globals.repairPriorities.Count; index++)
            {
                // Set current ChassisLocation
                ChassisLocations thisLoc = Globals.repairPriorities.ElementAt(index).Value;
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
            for (int index = 0; index < Globals.repairPriorities.Count; index++)
            {
                // Set current ChassisLocation
                ChassisLocations thisLoc = Globals.repairPriorities.ElementAt(index).Value;
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

        // Evaluates whether a given mech has any destroyed components
        public static bool CheckDestroyedComponents(MechDef mech)
        {
            // Default to not requesting any structure repair
            bool destroyedComponents = false;

            foreach (MechComponentRef mechComponentRef in mech.Inventory)
            {
                if (mechComponentRef.DamageLevel == ComponentDamageLevel.Destroyed)
                {
                    Logger.LogDebug(mech.Name + " has destroyed components: " + mechComponentRef.ComponentDefID);
                    destroyedComponents = true;
                    break; // Stop evaluating other components if a destroyed one has already been found
                }
            }

            return destroyedComponents;
        }

        // Evaluates whether a given mech has any damaged components
        public static bool CheckDamagedComponents(MechDef mech)
        {
            try
            {
                // Default to not requesting any structure repair
                bool damagedComponents = false;

                foreach (MechComponentRef mechComponentRef in mech.Inventory)
                {
                    if (mechComponentRef.DamageLevel == ComponentDamageLevel.Penalized)
                    {
                        Logger.LogDebug(mech.Name + " has damaged components: " + mechComponentRef.ComponentDefID);
                        damagedComponents = true;
                        break; // Stop evaluating other components if a damaged one has already been found
                    }
                }

                return damagedComponents;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return false;
            }
        }

    }
}

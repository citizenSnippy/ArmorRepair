using System;
using System.Collections.Generic;
using BattleTech;
using Harmony;
using UnityEngine;
using System.Linq;

namespace ArmorRepair
{
    // Prefix on RestoreMechPostCombat to create a new modify armor work order from the armor loss difference of each mech at the end of combat
    [HarmonyPatch(typeof(SimGameState), "RestoreMechPostCombat")]
    public static class SimGameState_RestoreMechPostCombat_Patch
    {

        public static bool Prefix(SimGameState __instance, MechDef mech)
        {
            try
            {
                // Start of analysing a mech for armor repair
                Logger.LogInfo("Analysing Mech: " + mech.Name);
                Logger.LogInfo("============================================");

                // Base generic MechLab WO for a mech that requires armor or structure repair - each individual locational subentry WO has to be added to this base WO later
                WorkOrderEntry_MechLab newMechLabWorkOrder = null;

                /* STRUCTURE DAMAGE CHECKS
                 * ------------------------
                 * Check if the given mech needs any structure repaired and that EnableStructureRepair is true in the mod settings
                 * 
                 */
                if(ArmorRepair.ModSettings.EnableStructureRepair)
                { 
                    if (Helpers.CheckStructureDamage(mech))
                    {
                        Logger.LogDebug("SimGameConstant: StructureRepairTechPoints: " + __instance.Constants.MechLab.StructureRepairTechPoints);
                        Logger.LogDebug("SimGameConstant: StructureRepairCost: " + __instance.Constants.MechLab.StructureRepairCost);

                        // Loop over the ChassisLocations for repair in their highest -> lowest priority order from the dictionary defined in Helpers
                        for (int index = 0; index < Helpers.repairPriorities.Count; index++)
                        {
                            // Set current looped ChassisLocation
                            ChassisLocations thisLoc = Helpers.repairPriorities.ElementAt(index).Value;
                            // Get current mech's loadout definition from the looped chassis location
                            LocationLoadoutDef thisLocLoadout = mech.GetLocationLoadoutDef(thisLoc);
                            // Friendly name for this location
                            string thisLocName = thisLoc.ToString();

                            Logger.LogDebug("Analysing location: " + thisLocName);

                            // Check if a new base MechLab order needs to be created or not
                            if (newMechLabWorkOrder == null)
                            {
                                // Create new base work order of the generic MechLab type if it doesn't already exist
                                newMechLabWorkOrder = Helpers.CreateBaseMechLabOrder(__instance, mech);
                            }

                            float currentStructure = thisLocLoadout.CurrentInternalStructure;
                            float definedStructure = mech.GetChassisLocationDef(thisLoc).InternalStructure;

                            // Only create work orders for repairing structure if this location has taken damage in combat
                            if (currentStructure != definedStructure)
                            {
                                // Work out difference of structure lost for each location - default to 0
                                int structureDifference = 0;
                                structureDifference = (int)Mathf.Abs(currentStructure - definedStructure);

                                Logger.LogInfo("Total structure difference for " + thisLocName + " is " + structureDifference);

                                Logger.LogInfo("Creating MechRepair work order entry for " + thisLocName);
                                Logger.LogDebug("Calling CreateMechRepairWorkOrder with params - GUID: " +
                                    mech.GUID.ToString() +
                                    " | Location: " + thisLocName +
                                    " | structureDifference: " + structureDifference
                                );

                                WorkOrderEntry_RepairMechStructure newRepairWorkOrder = __instance.CreateMechRepairWorkOrder(
                                    mech.GUID,
                                    thisLocLoadout.Location,
                                    structureDifference
                                );

                                Logger.LogDebug("Adding WO subentry to repair missing " + thisLocName + " structure.");
                                newMechLabWorkOrder.AddSubEntry(newRepairWorkOrder);

                            }
                            else
                            {
                                Logger.LogDebug("Structure repair not required for: " + thisLocName);
                            }
                        }
                    }
                }


                /* ARMOR DAMAGE CHECKS
                 * -------------------
                 * Check if the given mech needs any structure repaired
                 * 
                 */
                if (Helpers.CheckArmorDamage(mech))
                {

                    Logger.LogDebug("SimGameConstant: ArmorInstallTechPoints: " + __instance.Constants.MechLab.ArmorInstallTechPoints);
                    Logger.LogDebug("SimGameConstant: ArmorInstallCost: " + __instance.Constants.MechLab.ArmorInstallCost);

                    // Loop over the ChassisLocations for repair in their highest -> lowest priority order from the dictionary defined in Helpers
                    for (int index = 0; index < Helpers.repairPriorities.Count; index++)
                    {
                        // Set current ChassisLocation
                        ChassisLocations thisLoc = Helpers.repairPriorities.ElementAt(index).Value;
                        // Get current mech's loadout from the looped chassis location
                        LocationLoadoutDef thisLocLoadout = mech.GetLocationLoadoutDef(thisLoc);
                        // Friendly name for this location
                        string thisLocName = thisLoc.ToString();

                        Logger.LogDebug("Analysing location: " + thisLocName);

                        // Check if a new base MechLab order needs to be created
                        if (newMechLabWorkOrder == null)
                        {
                            // Create new base work order of the generic MechLab type if it doesn't already exist
                            newMechLabWorkOrder = Helpers.CreateBaseMechLabOrder(__instance, mech);
                        }

                        // Only create work orders for repairing armor if this location has taken armor damage in combat
                        if (thisLocLoadout.CurrentArmor != thisLocLoadout.AssignedArmor)
                        {
                            // Work out difference of armor lost for each location - default to 0
                            int armorDifference = 0;

                            // Consider rear armour in difference calculation if this is a RT, CT or LT
                            if (thisLocLoadout == mech.CenterTorso || thisLocLoadout == mech.RightTorso || thisLocLoadout == mech.LeftTorso)
                            {
                                Logger.LogDebug("Location also has rear armor.");
                                armorDifference = (int)Mathf.Abs(thisLocLoadout.CurrentArmor - thisLocLoadout.AssignedArmor) + (int)Mathf.Abs(thisLocLoadout.CurrentRearArmor - thisLocLoadout.AssignedRearArmor);
                            }
                            else
                            {
                                armorDifference = (int)Mathf.Abs(thisLocLoadout.CurrentArmor - thisLocLoadout.AssignedArmor);
                            }

                            Logger.LogInfo("Total armor difference for " + thisLocName + " is " + armorDifference);
                            Logger.LogInfo("Creating ModifyMechArmor work order entry for " + thisLocName);
                            Logger.LogDebug("Calling ModifyMechArmor WO with params - GUID: " +
                                mech.GUID.ToString() +
                                " | Location: " + thisLocName +
                                " | armorDifference: " + armorDifference +
                                " | AssignedArmor: " + thisLocLoadout.AssignedArmor +
                                " | AssignedRearArmor: " + thisLocLoadout.AssignedRearArmor
                            );
                            WorkOrderEntry_ModifyMechArmor newArmorWorkOrder = __instance.CreateMechArmorModifyWorkOrder(
                                mech.GUID,
                                thisLocLoadout.Location,
                                armorDifference,
                                (int)(thisLocLoadout.AssignedArmor),
                                (int)(thisLocLoadout.AssignedRearArmor)
                            );

                            /* IMPORTANT!
                                * This has turned out to be required as CurrentArmor appears to be reset to AssignedArmor from somewhere unknown in the game after battle
                                * So if we don't reset AssignedArmor now, player can cancel the work order to get a free armor reset anyway!
                                * 
                                * NOTE: CeilToInt (or similar rounding) is vital to prevent fractions of armor from causing Mech tonnage / validation issues for the player
                                */
                            Logger.LogDebug("Forcing assignment of Assigned Armor: " + thisLocLoadout.AssignedArmor + " To Current Armor (CeilToInt): " + Mathf.CeilToInt(thisLocLoadout.CurrentArmor));
                            thisLocLoadout.AssignedArmor = Mathf.CeilToInt(thisLocLoadout.CurrentArmor);
                            thisLocLoadout.AssignedRearArmor = Mathf.CeilToInt(thisLocLoadout.CurrentRearArmor);

                            Logger.LogInfo("Adding WO subentry to install missing " + thisLocName + " armor.");
                            newMechLabWorkOrder.AddSubEntry(newArmorWorkOrder);

                        }
                        else
                        {
                            Logger.LogDebug("Armor repair not required for: " + thisLocName);
                        }

                    }
                }


                /* WORK ORDER SUBMISSION
                 * ---------------------
                 * Submit the complete work order for the mech, which will include any repair armor / structure subentries for each location
                 * 
                 */
                if (newMechLabWorkOrder != null)
                {
                    if (newMechLabWorkOrder.SubEntryCount > 0)
                    {

                        Logger.LogDebug("Adding base MechLab WO to queue.");

                        // Now that all WO subentries are added, add the base MechLab work order to the game's Mech Lab Work Order Queue
                        __instance.MechLabQueue.Add(newMechLabWorkOrder);
                        // Call this to properly Initialize the base Mech Lab WO and make it visible in the Mech Lab queue UI / timeline etc.
                        __instance.InitializeMechLabEntry(newMechLabWorkOrder, newMechLabWorkOrder.GetCBillCost());
                        // This call below is also required to push it to the queue, but is now included in the above InitializeMechLabEntry method
                        //      _instance.RoomManager.AddWorkQueueEntry(newMechLabWorkOrder);

                        // Force an update of the Mech Lab queue, false is to tell it a day isn't passing.
                        __instance.UpdateMechLabWorkQueue(false);

                        // Simple cost debugging for the log
                        foreach (WorkOrderEntry subEntries in newMechLabWorkOrder.SubEntries)
                        {
                            Logger.LogDebug(subEntries.Description + " Repair Tech Costs: " + subEntries.GetCost());
                        }
                        Logger.LogInfo(mech.Name + " Repair Costs are - techCost: " + newMechLabWorkOrder.GetCost() + " | CBill Cost: " + newMechLabWorkOrder.GetCBillCost());
                        Logger.LogInfo("=====================================================================");

                        // Deduct the total CBill costs of the WO from player inventory. For some reason this isn't done automatically via the HBS WO system.
                        __instance.AddFunds(-newMechLabWorkOrder.GetCBillCost(), "ArmorRepair", true);
                    }
                    else
                    {
                        Logger.LogInfo(mech.Name + " did not require repairs.");
                    }
                }

                // Lifted from original RestoreMechPostCombat method - resets any non-functional mech components back to functional
                foreach (MechComponentRef mechComponentRef in mech.Inventory)
                {
                    if (mechComponentRef.DamageLevel == ComponentDamageLevel.NonFunctional)
                    {
                        Logger.LogDebug("Resetting non-functional mech component: " + mechComponentRef.ToString());
                        mechComponentRef.DamageLevel = ComponentDamageLevel.Functional;
                    }
                }

                return false; // Finally, prevent firing the original method
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return true; // Allow original method to fire if there is an exception
            }
        }
    }



    /* Patch CreateMechArmorModifyWorkOrder so we can apply tonnage modifiers to armor work orders in the game
     *  The intent of this is to make light mechs cheaper to repair armor on compared with heavy/assault mechs. It can be disabled in the mod.json settings.
     */
    [HarmonyPatch(typeof(SimGameState), "CreateMechArmorModifyWorkOrder")]
    public static class SimGameState_CreateMechArmorModifyWorkOrder_Patch
    {
        private static void Postfix(ref SimGameState __instance, ref string mechSimGameUID, ref ChassisLocations location, ref int armorDiff, ref int frontArmor, ref int rearArmor, ref WorkOrderEntry_ModifyMechArmor __result)
        {
            string id = string.Format("MechLab - ModifyArmor - {0}", __instance.GenerateSimGameUID());

            try
            {
                float mechTonnageModifier = 1f;
                int techCost = 0;
                int cbillCost = 0;

                foreach (MechDef mechDef in __instance.ActiveMechs.Values)
                {
                    if (mechDef.GUID == mechSimGameUID)
                    {
                        
                        // If ScaleArmorCostByTonnage is enabled, make the mech tonnage work as a percentage tech cost reduction (95 tons = 0.95 or "95%" of the cost, 50 tons = 0.05 or "50%" of the cost etc)
                        if (ArmorRepair.ModSettings.ScaleArmorCostByTonnage)
                        {
                            mechTonnageModifier = mechDef.Chassis.Tonnage * 0.01f;
                        }
                        float locationTechCost = ((armorDiff * mechTonnageModifier) * __instance.Constants.MechLab.ArmorInstallTechPoints);
                        float locationCbillCost = ((armorDiff * mechTonnageModifier) * __instance.Constants.MechLab.ArmorInstallCost);
                        techCost = Mathf.CeilToInt(locationTechCost);
                        cbillCost = Mathf.CeilToInt(locationCbillCost);

                        Logger.LogDebug("Armor WO SubEntry Costing: ");
                        Logger.LogDebug("***************************************");
                        Logger.LogDebug("location: " + location.ToString());
                        Logger.LogDebug("armorDifference: " + armorDiff);
                        Logger.LogDebug("mechTonnage: " + mechDef.Chassis.Tonnage);
                        Logger.LogDebug("mechTonnageModifier: " + mechTonnageModifier);
                        Logger.LogDebug("techCost: " + techCost);
                        Logger.LogDebug("cbillCost: " + cbillCost);
                        Logger.LogDebug("***************************************");
                    }
                }

                __result = new WorkOrderEntry_ModifyMechArmor(id, string.Format("Modify Armor - {0}", location.ToString()), mechSimGameUID, techCost, location, frontArmor, rearArmor, cbillCost, string.Empty);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

        }
    }

    /* Patch CreateMechRepairWorkOrder so we can apply tonnage modifiers to structure repair work orders in the game
     *  The intent of this is to make light mechs cheaper to repair structure on compared with heavy/assault mechs. It can be disabled in the mod.json settings.
     *  
     */
    [HarmonyPatch(typeof(SimGameState), "CreateMechRepairWorkOrder")]
    public static class SimGameState_CreateMechRepairWorkOrder_Patch
    {

        private static void Postfix(ref SimGameState __instance, ref string mechSimGameUID, ref ChassisLocations location, ref int structureCount, ref WorkOrderEntry_RepairMechStructure __result)
        {
            try
            {

                float mechTonnageModifier = 1f;
                // Original method code, this is still needed to work out zero structure modifiers 
                string id = string.Format("MechLab - RepairMech - {0}", __instance.GenerateSimGameUID());
                bool flag = false;
                float num = 1f;
                float num2 = 1f;

                foreach (MechDef mechDef in __instance.ActiveMechs.Values)
                {
                    if (mechDef.GUID == mechSimGameUID)
                    {
                        // If ScaleStructureCostByTonnage is enabled, make the mech tonnage work as a percentage tech cost reduction (95 tons = 0.95 or "95%" of the cost, 50 tons = 0.05 or "50%" of the cost etc)
                        if (ArmorRepair.ModSettings.ScaleStructureCostByTonnage)
                        {
                            mechTonnageModifier = mechDef.Chassis.Tonnage * 0.01f;
                        }

                        if (mechDef.GetChassisLocationDef(location).InternalStructure == (float)structureCount)
                        {
                            flag = true;
                        }

                        break;
                    }
                }
                if (flag)
                {
                    num = __instance.Constants.MechLab.ZeroStructureCBillModifier;
                    num2 = __instance.Constants.MechLab.ZeroStructureTechPointModifier;
                }

                int techCost = Mathf.CeilToInt((__instance.Constants.MechLab.StructureRepairTechPoints * (float)structureCount * num2) * mechTonnageModifier);
                int cbillCost = Mathf.CeilToInt((float)((__instance.Constants.MechLab.StructureRepairCost * structureCount) * num) * mechTonnageModifier);

                Logger.LogDebug("Structure WO Subentry Costing:");
                Logger.LogDebug("***************************************");
                Logger.LogDebug("location: " + location.ToString());
                Logger.LogDebug("structureCount: " + structureCount);
                Logger.LogDebug("mechTonnageModifier: " + mechTonnageModifier);
                Logger.LogDebug("techCost: " + techCost);
                Logger.LogDebug("cBill cost: " + cbillCost);
                Logger.LogDebug("***************************************");

                __result = new WorkOrderEntry_RepairMechStructure(id, string.Format("Repair 'Mech - {0}", location.ToString()), mechSimGameUID, techCost, location, structureCount, cbillCost, string.Empty);

            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }


    /* [FIX] Patch WorkOrderEntry_ModifyMechArmor so we can apply our armor tech cost modifier 
     *  
     *  techCostModifier is used to reduce the overall tech cost for armor work orders
     *      PROBLEM: 
     *      HBS exposed ArmorInstallTechCost in SimGameConstants but it's an int, rather than a float.
     *      
     *      By default this means when we set it to even the lowest possible int value (1), armor install tech costs are still calculated at unitsLost * ArmorInstallTechCost = techCost. 
     *      This results in even the lowest integer (1) setting causing armor units to be over 3x more expensive than the default cost of structure units! It's too much even late game with lots of mechtechs.
     *      
     *      WORKAROUND:
     *      The workaround for this is to modify the game's calculated techCost for armor Work Oroders by * 0.01f, reducing it by a factor of 100 as a base.
     *      This effectively turns the SimGameConstants integer into a usable float without having to modify shit tons of references or doing anything too messy. 
     *      
     *      The player can then modify the ultimate tech cost for armor by setting the SimGameConstants.ArmorInstallTechCost integer as normal. 
     *      An ArmorInstallTechCost setting of 10 in SimGameConstants will now be equivalent to a StructureRepairTechCost of 0.1 (vanilla default for structure units is 0.3 for balancing illustration)
    */
    [HarmonyPatch(typeof(WorkOrderEntry_ModifyMechArmor), new Type[]
        {
            typeof(string),
            typeof(string),
            typeof(string),
            typeof(int),
            typeof(ChassisLocations),
            typeof(int),
            typeof(int),
            typeof(int),
            typeof(string)
        })]
    public static class WorkOrderEntry_ModifyMechArmor_Patch
    {
        private static void Prefix(ref int cbillCost, ref int techCost, int desiredFrontArmor, int desiredRearArmor)
        {
            try
            {

                float techCostModifier = 0.01f; // Modify int based armor techCosts to a pseudo float
                float num = techCost * techCostModifier;
                techCost = Mathf.CeilToInt(num);
                cbillCost = Mathf.CeilToInt(cbillCost);

                Logger.LogDebug("Armor WO Costing: ");
                Logger.LogDebug("*********************");
                Logger.LogDebug("techCost: " + techCost);
                Logger.LogDebug("cBillCost: " + cbillCost);
                Logger.LogDebug("*********************");

            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }


    /* [FIX] Patch into ML_RepairMech to prevent structure repair work orders from resetting armor
     *      HBS hardcoded structure repairs to reset armor because reasons
     */
    [HarmonyPatch(typeof(SimGameState), "ML_RepairMech")]
    public static class SimGameState_ML_RepairMech_Patch
    {
        public static bool Prefix(SimGameState __instance, WorkOrderEntry_RepairMechStructure order)
        {
            if (order.IsMechLabComplete)
            {
                return true;
            }
            else
            {
                MechDef mechByID = __instance.GetMechByID(order.MechLabParent.MechID);
                LocationLoadoutDef locationLoadoutDef = mechByID.GetLocationLoadoutDef(order.Location);
                locationLoadoutDef.CurrentInternalStructure = mechByID.GetChassisLocationDef(order.Location).InternalStructure;
                // Original method resets currentArmor to assignedArmor here for some reason! Removed them from this override
                Logger.LogDebug("ALERT: Intercepted armor reset from ML_RepairMech and prevented it.");
                mechByID.RefreshBattleValue();
                order.SetMechLabComplete(true);

                return false; // Prevent original method from firing
            }
        }
    }

    /* UI WARNING ON DAMAGED COMPONENTS
     * Attempting to flag up warning in Mech Bay / Mech Lab when a mech has destroyed components.
     * 
     * This isn't a problem in vanilla, but now we are auto repairing armour and structure and can't auto repair components (e.g. they might not be in stock)
     * we now need to flag up the player that there is a problem with the mech when it has destroyed components.
     * 
     * TODO: Probably need to find a smarter way of doing this than using the Underweight Tonnage warning.
     */
    [HarmonyPatch(typeof(MechValidationRules), "ValidateMechTonnage")]
    public static class MechValidationRules_ValidateMechTonnage_Patch
    {
        public static void Postfix(MechDef mechDef, ref Dictionary<MechValidationType, List<string>> errorMessages)
        {
            try
            {
                for (int i = 0; i < mechDef.Inventory.Length; i++)
                {
                    MechComponentRef mechComponentRef = mechDef.Inventory[i];
                    if (mechComponentRef.DamageLevel == ComponentDamageLevel.Destroyed)
                    {
                        Logger.LogDebug("Flagging damaged component warning: " + mechDef.Name);
                        errorMessages[MechValidationType.Underweight].Add(string.Format("DAMAGED COMPONENT: 'Mech has damaged components", new object[0]));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }



    /* TESTING / DEBUGGING
     * Testing and debugging patches
     * 
     */

    // Testing to see if this is the override we need to stop mech's auto regaining their armor even if the WO is cancelled
    [HarmonyPatch(typeof(SimGameState), "ML_ModifyArmor")]
    public static class SimGameState_ML_ModifyArmor_Patch
    {
        private static bool Prefix(SimGameState __instance, WorkOrderEntry_ModifyMechArmor order)
        {
            MechDef mechByID = __instance.GetMechByID(order.MechLabParent.MechID);
            LocationLoadoutDef locationLoadoutDef = mechByID.GetLocationLoadoutDef(order.Location);

            Logger.LogDebug("ML_ModifyArmor was called with params: ");
            Logger.LogDebug("************************************** ");
            Logger.LogDebug("mechByID: " + mechByID.Description.Name);
            Logger.LogDebug("CurrentArmor: " + locationLoadoutDef.CurrentArmor + " = Desired: " + (float)order.DesiredFrontArmor);
            Logger.LogDebug("CurrentRearArmor: " + locationLoadoutDef.CurrentRearArmor + " = Desired: " + (float)order.DesiredRearArmor);
            Logger.LogDebug("AssignedArmor: " + locationLoadoutDef.AssignedArmor + " = Desired: " + (float)order.DesiredFrontArmor);
            Logger.LogDebug("AssignedRearArmor: " + locationLoadoutDef.AssignedRearArmor + " = Desired: " + (float)order.DesiredRearArmor);
            Logger.LogDebug("************************************** ");

            return true;
        }
    }

    // Just to debug Structure WO final costs
    [HarmonyPatch(typeof(WorkOrderEntry_RepairMechStructure), new Type[]
        {
            typeof(string),
            typeof(string),
            typeof(string),
            typeof(int),
            typeof(ChassisLocations),
            typeof(int),
            typeof(int),
            typeof(string)
        })]
    public static class WorkOrderEntry_RepairMechStructure_Patch
    {
        private static void Prefix(ref int cbillCost, ref int techCost, int structureAmount)
        {
            try
            {
                Logger.LogDebug("Structure WO Costing: ");
                Logger.LogDebug("*********************");
                Logger.LogDebug("techCost: " + techCost);
                Logger.LogDebug("cBillCost: " + cbillCost);
                Logger.LogDebug("*********************");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }

    /* ARMOR RESET PROBLEM DEBUGGING 
    [HarmonyPatch(typeof(SimGameState), "RestoreArmorIfUndamaged")]
    public static class SimGameState_RestoreArmorIfUndamaged_Patch
    {
        public static bool Prefix(LocationLoadoutDef loadoutDef, LocationDef chassisLocationDef)
        {
            Logger.LogDebug("ALERT: The game is still calling SimGameState.RestoreArmorIfUndamaged from somewhere!");
            return false;
        }
    }

    [HarmonyPatch(typeof(MechDef), "ResetArmorToAssigned")]
    public static class MechDef_ResetArmorToAssigned_Patch
    {
        public static bool ResetArmorToAssigned()
        {
            Logger.LogDebug("ALERT: The game is still calling MechDef.ResetArmorToDefined from somewhere!");
            return false;
        }
    }
    */


}

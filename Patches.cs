using System;
using System.Collections.Generic;
using Harmony;
using BattleTech;
using BattleTech.UI;
using UnityEngine;
using System.Linq;

namespace ArmorRepair
{

    // Ensure our temp Mech Lab queue is always cleared before processing another mission/contract completion
    [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
    public static class SimGameState_ResolveCompleteContract_Patch
    {

        // Just for safety, ensure the temp queue in this mod is completely clear before we run any processing
        public static bool Prefix(SimGameState __instance)
        {
            try
            {
                Globals.tempMechLabQueue.Clear();
            }
            catch(Exception ex)
            {
                Logger.LogError(ex);
                return true; 
            }

            return true; // Allow original method to fire
        }

        // Run after completion of contracts and queue up any orders in the temp queue into the game's Mech Lab queue 
        public static void Postfix(SimGameState __instance)
        {
            try
            {
                // If there are any work orders in the temporary queue, prompt the player
                if(Globals.tempMechLabQueue.Count > 0)
                {
                    Logger.LogDebug("Processing temp Mech Lab queue orders.");     
                    
                    int cbills = 0;
                    int techCost = 0;
                    int mechRepairCount = 0;
                    int skipMechCount = 0;
                    string mechRepairCountDisplayed = String.Empty;
                    string skipMechCountDisplayed = String.Empty;
                    string skipMechMessage = String.Empty;
                    string finalMessage = String.Empty;

                    // If player has disabled auto repairing mechs with destroyed components, check for them and remove them from the temp queue before continuing
                    if (!ArmorRepair.ModSettings.AutoRepairMechsWithDestroyedComponents)
                    {
                        for (int index = 0; index < Globals.tempMechLabQueue.Count; index++)
                        {
                            WorkOrderEntry_MechLab order = Globals.tempMechLabQueue[index];

                            Logger.LogDebug("Checking for destroyed components.");
                            bool destroyedComponents = false;
                            MechDef mech = __instance.GetMechByID(order.MechID);
                            destroyedComponents = Helpers.CheckDestroyedComponents(mech);

                            if (destroyedComponents)
                            {
                                // Remove this work order from the temp mech lab queue if the mech has destroyed components and move to next iteration
                                Logger.LogDebug("Removing " + mech.Name + " order from temp queue due to destroyed components and mod settings.");
                                Globals.tempMechLabQueue.Remove(order);
                                destroyedComponents = false;
                                skipMechCount++;
                                index++;

                            }
                        }
                    }

                    Logger.LogDebug("Temp Queue has " + Globals.tempMechLabQueue.Count + " entries.");

                    // Calculate summary of total repair costs from the temp work order queue
                    for (int index = 0; index < Globals.tempMechLabQueue.Count; index++)
                    {
                        WorkOrderEntry_MechLab order = Globals.tempMechLabQueue[index];
                        MechDef mech = __instance.GetMechByID(order.MechID);
                        Logger.LogDebug("Adding " + mech.Name + " to RepairCount.");
                        cbills += order.GetCBillCost();
                        techCost += order.GetCost();
                        mechRepairCount++;
                    }

                    mechRepairCount = Mathf.Clamp(mechRepairCount, 0, 4);
                    Logger.LogDebug("Temp Queue has " + Globals.tempMechLabQueue.Count + " work order entries.");

                    // If Yang's Auto Repair prompt is enabled, build a message prompt dialog for the player
                    if (ArmorRepair.ModSettings.EnableAutoRepairPrompt)
                    {

                        // Calculate a friendly techCost of the work order in days, based on number of current mechtechs in the player's game.
                        if (techCost != 0 && __instance.MechTechSkill != 0)
                        {                
                            techCost = Mathf.CeilToInt((float)techCost / (float)__instance.MechTechSkill);
                        }
                        else
                        {
                            techCost = 1; // Safety in case of weird div/0
                        }

                        // Generate a quick friendly description of how many mechs were damaged in battle
                        switch (mechRepairCount)
                        {
                            case 0: { Logger.LogDebug("mechRepairCount was 0."); break; }
                            case 1: { mechRepairCountDisplayed = "one of our 'Mechs was"; break; }
                            case 2: { mechRepairCountDisplayed = "a couple of the 'Mechs were"; break; }
                            case 3: { mechRepairCountDisplayed = "three of our 'Mechs were"; break; }
                            case 4: { mechRepairCountDisplayed = "our whole lance was"; break; }
                        }
                        // Generate a friendly description of how many mechs were damaged but had components destroyed
                        switch (skipMechCount)
                        {
                            case 0: { Logger.LogDebug("skipMechCount was 0."); break; }
                            case 1: { skipMechCountDisplayed = "one of the 'Mechs is damaged but has"; break; }
                            case 2: { skipMechCountDisplayed = "two of the 'Mechs are damaged but have"; break; }
                            case 3: { skipMechCountDisplayed = "three of the 'Mechs are damaged but have "; break; }
                            case 4: { skipMechCountDisplayed = "the whole lance is damaged but has"; break; }
                        }

                        // Check if there are any mechs to process
                        if (mechRepairCount > 0 || skipMechCount > 0)
                        {
                            Logger.LogDebug("mechRepairCount is " + mechRepairCount + " skipMechCount is " + skipMechCount);

                            // Setup the notification for mechs with damaged components that we might want to skip
                            if (skipMechCount > 0 && mechRepairCount == 0)
                            {
                                skipMechMessage = String.Format("{0} destroyed components. I'll leave the repairs for you to review.", skipMechCountDisplayed);
                            }
                            else 
                            {
                                skipMechMessage = String.Format("{0} destroyed components, so I'll leave those repairs to you.", skipMechCountDisplayed);
                            }
                            
                            Logger.LogDebug("Firing Yang's UI notification.");
                            SimGameInterruptManager notificationQueue = __instance.GetInterruptQueue();

                            // If all of the mechs needing repairs have damaged components and should be skipped from auto-repair, change the message notification structure to make more sense (e.g. just have an OK button)
                            if(skipMechCount > 0 && mechRepairCount == 0)
                            {
                                finalMessage = String.Format(
                                    "Boss, {0} \n\n", 
                                    skipMechMessage
                                );

                                // Queue Notification
                                notificationQueue.QueuePauseNotification(
                                    "'Mech Repairs Needed",
                                    finalMessage,
                                    __instance.GetCrewPortrait(SimGameCrew.Crew_Yang),
                                    string.Empty,
                                    delegate
                                    {
                                        Logger.LogDebug("[PROMPT] All damaged mechs had destroyed components and won't be queued for repair.");
                                    },
                                    "OK"
                                );
                            }
                            else
                            {
                                if(skipMechCount > 0)
                                {
                                    finalMessage = String.Format(
                                        "Boss, {0} damaged. It'll cost <color=#DE6729>{1}{2:n0}</color> and {3} days for these repairs. Want my crew to get started?\n\nAlso, {4}\n\n",
                                        mechRepairCountDisplayed,
                                        '¢', cbills.ToString(),
                                        techCost.ToString(),
                                        skipMechMessage
                                    );
                                }
                                else
                                {
                                    finalMessage = String.Format(
                                        "Boss, {0} damaged on the last engagement. It'll cost <color=#DE6729>{1}{2:n0}</color> and {3} days for the repairs.\n\nWant my crew to get started?",
                                        mechRepairCountDisplayed,
                                        '¢', cbills.ToString(),
                                        techCost.ToString()
                                    );
                                }
                                

                                // Queue up Yang's notification
                                notificationQueue.QueuePauseNotification(
                                    "'Mech Repairs Needed",
                                    finalMessage,
                                    __instance.GetCrewPortrait(SimGameCrew.Crew_Yang),
                                    string.Empty,
                                    delegate
                                    {
                                        Logger.LogDebug("[PROMPT] Moving work orders from temp queue to Mech Lab queue: " + Globals.tempMechLabQueue.Count + " work orders");
                                        foreach (WorkOrderEntry_MechLab workOrder in Globals.tempMechLabQueue.ToList())
                                        {
                                                Logger.LogInfo("[PROMPT] Moving work order from temp queue to Mech Lab queue: " + workOrder.Description + " - " + workOrder.GetCBillCost());
                                                Helpers.SubmitWorkOrder(__instance, workOrder);
                                                Globals.tempMechLabQueue.Remove(workOrder);
                                        }
                                    },
                                    "Yes",
                                    delegate
                                    {
                                        Logger.LogInfo("[PROMPT] Discarding work orders from temp queue: " + Globals.tempMechLabQueue.Count + " work orders");
                                        foreach (WorkOrderEntry_MechLab workOrder in Globals.tempMechLabQueue.ToList())
                                        {
                                            Logger.LogInfo("[PROMPT] Discarding work order from temp queue: " + workOrder.Description + " - " + workOrder.GetCBillCost());
                                            Globals.tempMechLabQueue.Remove(workOrder);
                                        }
                                    },
                                    "No"
                                );
                            }
                        }
                    }
                    else // If Auto Repair prompt is not enabled, just proceed with queuing the remaining temp queue work orders and don't notify the player
                    {
                        foreach (WorkOrderEntry_MechLab workOrder in Globals.tempMechLabQueue.ToList())
                        {
                            Logger.LogInfo("[AUTO] Moving work order from temp queue to Mech Lab queue: " + workOrder.Description + " - " + workOrder.GetCBillCost());
                            Helpers.SubmitWorkOrder(__instance, workOrder);
                            Globals.tempMechLabQueue.Remove(workOrder);
                        }          
                    }
                }    
            }
            catch (Exception ex)
            {
                Globals.tempMechLabQueue.Clear();
                Logger.LogError(ex);
            }
        }
    }


    /* Prefix on RestoreMechPostCombat to create a new modify armor work order from the armor loss difference of each mech at the end of combat
     * 
     *  If successful we prevent firing the original method as this is required to stop mech armor being blindly reset at the end of a contract.
     */
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
                        for (int index = 0; index < Globals.repairPriorities.Count; index++)
                        {
                            // Set current looped ChassisLocation
                            ChassisLocations thisLoc = Globals.repairPriorities.ElementAt(index).Value;
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

                /* COMPONENT DAMAGE CHECKS
                 * -----------------------
                 * Check if the given mech needs any critted components repaired
                 * 
                 * NOTE: Not yet working. Repair components are added to work order but not actually repaired after WO completes. Noticed there is another queue involved on SGS.WorkOrderComponents we need to debug.
                 * Currently throws "SimGameState [ERROR] ML_RepairComponent MechBay - RepairComponent - SGRef_490 had an invalid mechComponentID Ammo_AmmunitionBox_Generic_AC5, skipping" in SimGame logger on WO completion.
                if (Helpers.CheckDamagedComponents(mech))
                {
                    for (int index = 0; index < mech.Inventory.Length; index++)
                    {
                        MechComponentRef mechComponentRef = mech.Inventory[index];

                        // Penalized = Critted Component
                        if (mechComponentRef.DamageLevel == ComponentDamageLevel.Penalized)
                        {
                            // Check if a new base MechLab order needs to be created or not
                            if (newMechLabWorkOrder == null)
                            {
                                // Create new base work order of the generic MechLab type if it doesn't already exist
                                newMechLabWorkOrder = Helpers.CreateBaseMechLabOrder(__instance, mech);
                            }

                            // Create a new component repair work order for this component
                            Logger.LogInfo("Creating Component Repair work order entry for " + mechComponentRef.ComponentDefID);
                            WorkOrderEntry_RepairComponent newComponentRepairOrder = __instance.CreateComponentRepairWorkOrder(mechComponentRef, false);

                            // Attach as a child to the base Mech Lab order.
                            Logger.LogDebug("Adding WO subentry to repair component " + mechComponentRef.ComponentDefID);
                            newMechLabWorkOrder.AddSubEntry(newComponentRepairOrder);
                        }
                    }
                }
                */


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
                    for (int index = 0; index < Globals.repairPriorities.Count; index++)
                    {
                        // Set current ChassisLocation
                        ChassisLocations thisLoc = Globals.repairPriorities.ElementAt(index).Value;
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
                        // Submit work order to our temporary queue for internal processing
                        Helpers.SubmitTempWorkOrder(
                            __instance, 
                            newMechLabWorkOrder,
                            mech
                        );
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
     *  HBS hardcoded structure repairs to reset armor because reasons
     *  
     *  This must prevent ML_RepairMech from firing as it's the only way we can stop blind armor resets when mech structure is repaired
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

    /* [FIX] UI WARNING ON DESTROYED COMPONENTS
     * Attempting to flag up warning in Mech Bay / Mech Lab when a mech has destroyed components.
     * 
     * This isn't a problem in vanilla, but now we are auto repairing armour and structure and can't auto repair components easily (e.g. they might not be in stock)
     * we now need to flag up the player that there is a problem with the mech when it has destroyed components.
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
                        Logger.LogDebug("Flagging destroyed component warning: " + mechDef.Name);
                        errorMessages[MechValidationType.Underweight].Add(string.Format("DESTROYED COMPONENT: 'Mech has destroyed components", new object[0]));
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


    /* [FIX] SUPPRESS YANG REPAIRS WARNING
     * If the player has enabled Yang's notification about mech repairs in this mod, suppress the default in-game warning from spamming the player about repairs twice
     */ 
    [HarmonyPatch(typeof(SimGameState), "ShowMechRepairsNeededNotif")]
    public static class SimGameState_ShowMechRepairsNeededNotif_Patch
    {
        public static bool Prefx(SimGameState __instance)
        {
            if(ArmorRepair.ModSettings.enableAutoRepairPrompt)
            {
                __instance.CompanyStats.Set<int>("COMPANY_NotificationViewed_BattleMechRepairsNeeded", __instance.DaysPassed);
                return false; // Suppress original method
            }
            else
            {
                return true; // Do nothing if the player isn't using our Yang prompt functionality.
            }
            
        }
    }


    /* TESTING / DEBUGGING
     * Testing and debugging patches
     * 
     */

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

    /* ML_ModifyArmor executes when a work order item for modifying armor is completed, and physically sets the desired amor on the mech. 
     *  It's not needed at this time 
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
    }*/

}

using System;
using System.Collections.Generic;
using BattleTech;

namespace ArmorRepair
{

    class Globals
    {
        /* TEMPMECHLABQUEUE
         * Temporary queue to hold post battle work orders until player confirms they want them processed
         * 
         */ 
        public static List<WorkOrderEntry_MechLab> tempMechLabQueue = new List<WorkOrderEntry_MechLab>();

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
    }
}

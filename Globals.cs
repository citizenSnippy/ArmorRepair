using System;
using System.Collections.Generic;
using BattleTech;

namespace ArmorRepair
{

    class Globals
    {
        // Temporary queue to hold post battle work orders until player confirms they want them processed
        public static List<WorkOrderEntry_MechLab> tempMechLabQueue = new List<WorkOrderEntry_MechLab>();

    }
}

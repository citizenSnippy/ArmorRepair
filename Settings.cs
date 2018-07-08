namespace ArmorRepair
{
    public class Settings
    {
        #region logging
        public bool debug = false;
        public bool Debug => debug;
        #endregion logging

        #region game
        public bool enableStructureRepair = true;
        public bool EnableStructureRepair => enableStructureRepair;
        public bool scaleStructureCostByTonnage = true;
        public bool ScaleStructureCostByTonnage => scaleStructureCostByTonnage;
        public bool scaleArmorCostByTonnage = true;
        public bool ScaleArmorCostByTonnage => scaleArmorCostByTonnage;
        public bool enableAutoRepairPrompt = true;
        public bool EnableAutoRepairPrompt => enableAutoRepairPrompt;
        public bool autoRepairMechsWithDamagedComponents = true;
        public bool AutoRepairMechsWithDamagedComponents => autoRepairMechsWithDamagedComponents;
        #endregion game
    }
}

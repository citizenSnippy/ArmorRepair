using System;
using System.Reflection;
using Harmony;
using Newtonsoft.Json;

namespace ArmorRepair
{
   
    public class ArmorRepair
    {
        // Mod Settings
        public static Settings ModSettings;
        public static string ModDirectory;

        public static void Init(string modDirectory, string settingsJSON)
        {

            new Logger();

            Logger.LogInfo("Mod Initialising...");

            var harmony = HarmonyInstance.Create("io.github.citizenSnippy.ArmorRepair");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            // Serialise settings from mod.json
            ModDirectory = modDirectory;
            try
            {
                ModSettings = JsonConvert.DeserializeObject<Settings>(settingsJSON);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                ModSettings = new Settings();
            }

            Logger.LogDebug("Mod Directory: " + ModDirectory);
            Logger.LogDebug("Mod Settings Debug: " + ModSettings.Debug);
            Logger.LogDebug("Mod Settings StructureRepair: " + ModSettings.EnableStructureRepair);
            Logger.LogDebug("Mod Settings StructureScaling: " + ModSettings.ScaleStructureCostByTonnage);
            Logger.LogDebug("Mod Settings ArmorScaling: " + ModSettings.ScaleArmorCostByTonnage);
            Logger.LogDebug("Mod Settings EnableAutoRepairPrompt: " + ModSettings.EnableAutoRepairPrompt);
            Logger.LogDebug("Mod Settings AutoRepairMechsWithDamagedComponents: " + ModSettings.AutoRepairMechsWithDamagedComponents);
            Logger.LogInfo("Mod Initialised.");

        }

    }

}

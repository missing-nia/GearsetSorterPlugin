using System;
using Dalamud.Configuration;
using Dalamud.Plugin;
using GearsetSorterPlugin.Enum;

namespace GearsetSorterPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        //TODO: sorting from least to greatest or greatest to least (item level)
        // As well as sorting alphabetically both ways

        public int Version { get; set; } = 1;

        public int PrimarySort { get; set; } = 0;

        public int SecondarySort { get; set; } = 1;

        public byte[] ClassJobSortOrder { get; set; } = { (byte)GearsetClassJob.PLD, (byte)GearsetClassJob.WAR, (byte)GearsetClassJob.DRK, (byte)GearsetClassJob.GNB,
                                                          (byte)GearsetClassJob.WHM, (byte)GearsetClassJob.SCH, (byte)GearsetClassJob.AST, (byte)GearsetClassJob.SGE,
                                                          (byte)GearsetClassJob.MNK, (byte)GearsetClassJob.DRG, (byte)GearsetClassJob.NIN, (byte)GearsetClassJob.SAM,
                                                          (byte)GearsetClassJob.RPR, (byte)GearsetClassJob.BRD, (byte)GearsetClassJob.MCH, (byte)GearsetClassJob.DNC,
                                                          (byte)GearsetClassJob.BLM, (byte)GearsetClassJob.SMN, (byte)GearsetClassJob.RDM, (byte)GearsetClassJob.BLU,
                                                          (byte)GearsetClassJob.GLD, (byte)GearsetClassJob.MRD, (byte)GearsetClassJob.CNJ, (byte)GearsetClassJob.PUG,
                                                          (byte)GearsetClassJob.LNC, (byte)GearsetClassJob.ROG, (byte)GearsetClassJob.ARC, (byte)GearsetClassJob.THM,
                                                          (byte)GearsetClassJob.ACN, (byte)GearsetClassJob.CRP, (byte)GearsetClassJob.BSM, (byte)GearsetClassJob.ARM,
                                                          (byte)GearsetClassJob.GSM, (byte)GearsetClassJob.LTW, (byte)GearsetClassJob.WVR, (byte)GearsetClassJob.ALC,
                                                          (byte)GearsetClassJob.CUL, (byte)GearsetClassJob.MIN, (byte)GearsetClassJob.BTN, (byte)GearsetClassJob.FSH };

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}

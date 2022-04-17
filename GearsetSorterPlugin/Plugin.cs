using System.IO;

using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using GearsetSorterPlugin.Enum;

namespace GearsetSorterPlugin
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Gearset Sorter";

        private const string commandName = "/pgearset";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        protected SigScanner mSigScanner;
        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            SigScanner sigScanner)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            mSigScanner = sigScanner;

            // Testing ClassJob sorting
            byte[] sortOrder = { (byte)GearsetClassJob.PLD, (byte)GearsetClassJob.WAR, (byte)GearsetClassJob.DRK, (byte)GearsetClassJob.GNB,
                                 (byte)GearsetClassJob.WHM, (byte)GearsetClassJob.SCH, (byte)GearsetClassJob.AST, (byte)GearsetClassJob.SGE,
                                 (byte)GearsetClassJob.BLM, (byte)GearsetClassJob.SMN, (byte)GearsetClassJob.RDM, (byte)GearsetClassJob.BRD,
                                 (byte)GearsetClassJob.MCH, (byte)GearsetClassJob.DNC, (byte)GearsetClassJob.MNK, (byte)GearsetClassJob.DRG,
                                 (byte)GearsetClassJob.NIN, (byte)GearsetClassJob.SAM, (byte)GearsetClassJob.RPR, (byte)GearsetClassJob.GLD,
                                 (byte)GearsetClassJob.MRD, (byte)GearsetClassJob.CNJ, (byte)GearsetClassJob.THM, (byte)GearsetClassJob.ACN,
                                 (byte)GearsetClassJob.ARC, (byte)GearsetClassJob.PUG, (byte)GearsetClassJob.LNC, (byte)GearsetClassJob.ROG, 
                                 (byte)GearsetClassJob.BLU, (byte)GearsetClassJob.CRP, (byte)GearsetClassJob.BSM, (byte)GearsetClassJob.ARM,
                                 (byte)GearsetClassJob.GSM, (byte)GearsetClassJob.LTW, (byte)GearsetClassJob.WVR, (byte)GearsetClassJob.ALC,
                                 (byte)GearsetClassJob.CUL, (byte)GearsetClassJob.MIN, (byte)GearsetClassJob.BTN, (byte)GearsetClassJob.FSH };

            FileManager.Init(sigScanner);
            GearsetSort.Init(sortOrder);

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);
            this.PluginUi = new PluginUI(this.Configuration, goatImage);

            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(commandName);
            FileManager.Uninit();
            GearsetSort.Uninit();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            //this.PluginUi.Visible = true;

            // Sorting Tests
            // Gearsets can hold 100 gearsets so go from 0 to 99
            GearsetSort.GearsetSortType sortTypePrimary = GearsetSort.GearsetSortType.Name;
            GearsetSort.GearsetSortType sortTypeSecondary = GearsetSort.GearsetSortType.ClassJob;

            string[] argsArray = args.Split(' ');

            if (argsArray[0] == "Name")
            {
                sortTypePrimary = GearsetSort.GearsetSortType.Name;
            }
            else if (argsArray[0] == "ClassJob")
            {
                sortTypePrimary = GearsetSort.GearsetSortType.ClassJob;
            }    
            else if (argsArray[0] == "ItemLevel")
            {
                sortTypePrimary = GearsetSort.GearsetSortType.ItemLevel;
            }

            if (argsArray[1] == "Name")
            {
                sortTypeSecondary = GearsetSort.GearsetSortType.Name;
            }
            else if (argsArray[1] == "ClassJob")
            {
                sortTypeSecondary = GearsetSort.GearsetSortType.ClassJob;
            }
            else if (argsArray[1] == "ItemLevel")
            {
                sortTypeSecondary = GearsetSort.GearsetSortType.ItemLevel;
            }

            GearsetSort.Sort(0, 99, sortTypePrimary, sortTypeSecondary);

            // Write to GEARSET.DAT and HOTBAR.DAT
            unsafe
            {
                FileManager.WriteFile((byte*)RaptureGearsetModule.Instance());
                FileManager.WriteFile((byte*)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetRaptureHotbarModule());
            }
        }

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}

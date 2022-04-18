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

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            FileManager.Init(sigScanner);
            GearsetSort.Init(this.Configuration.ClassJobSortOrder);

            this.PluginUi = new PluginUI(this.Configuration);

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
            // Sorting Tests
            // Gearsets can hold 100 gearsets so go from 0 to 99
            GearsetSortType sortTypePrimary = (GearsetSortType)this.Configuration.PrimarySort;
            GearsetSortType sortTypeSecondary = (GearsetSortType)this.Configuration.SecondarySort;

            /*string[] argsArray = args.Split(' ');

            if (argsArray[0] == "Name")
            {
                sortTypePrimary = GearsetSortType.Name;
            }
            else if (argsArray[0] == "ClassJob")
            {
                sortTypePrimary = GearsetSortType.ClassJob;
            }    
            else if (argsArray[0] == "ItemLevel")
            {
                sortTypePrimary = GearsetSortType.ItemLevel;
            }

            if (argsArray[1] == "Name")
            {
                sortTypeSecondary = GearsetSortType.Name;
            }
            else if (argsArray[1] == "ClassJob")
            {
                sortTypeSecondary = GearsetSortType.ClassJob;
            }
            else if (argsArray[1] == "ItemLevel")
            {
                sortTypeSecondary = GearsetSortType.ItemLevel;
            }*/

            // Update the sort order before sorting (just in case)
            GearsetSort.setClassJobSortOrder(this.Configuration.ClassJobSortOrder);
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

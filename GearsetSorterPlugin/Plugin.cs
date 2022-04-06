using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.IO;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace GearsetSorterPlugin
{
    public unsafe class Plugin : IDalamudPlugin
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

            MemManager.Init(sigScanner);
            GearsetSort.Init();

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
            MemManager.Uninit();
            GearsetSort.Uninit();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            //this.PluginUi.Visible = true;

            // Sorting Tests
            // Gearsets can hold 100 gearsets so go from 0 to 99
            GearsetSort.Sort(0, 99);

            // Write to GEARSET.DAT and HOTBAR.DAT
            MemManager.WriteFile((byte*)RaptureGearsetModule.Instance());
            MemManager.WriteFile((byte*)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetRaptureHotbarModule());
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

using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.IO;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule;

namespace GearsetSorter
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
        private RaptureGearsetModule *mGearsetModule;

        // Delegates
        // Not completely sure what char is intended for but it essentially functions
        // As a way to write to the file even if the proper flag isn't checked in memory
        private delegate char FileWriteDelegate(byte* pModule, char forceWrite);

        private static FileWriteDelegate mdGEARSETSave;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            SigScanner sigScanner)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            mSigScanner = sigScanner;

            //TODO: Actually do the stuff lol
            //Instance the GearsetModule and write the address to log for debugging
            mGearsetModule = RaptureGearsetModule.Instance();
            PluginLog.LogInformation($"GEARSET.DAT: {new IntPtr(mGearsetModule).ToString("x")}");

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
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            //this.PluginUi.Visible = true;

            // Test write functionality
            try
            {
                // Get Address of UserFileEvent_vf12
                IntPtr fpGetFileWriteAddress = mSigScanner.ScanText("40 57 48 81 EC 50 02");
                if (fpGetFileWriteAddress != IntPtr.Zero)
                {
                    mdGEARSETSave = Marshal.GetDelegateForFunctionPointer<FileWriteDelegate>(fpGetFileWriteAddress);
                }

                //Write to the file
                mdGEARSETSave.Invoke((byte*)mGearsetModule, (char)0x01);
            }
            catch (Exception e)
            {
                throw new Exception($"Error in \"OnCommand()\" while searching for required function signatures; Raw exception as follows:\r\n{e}");
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

using System;
using System.Runtime.InteropServices;

using Dalamud.Game;

namespace GearsetSorterPlugin
{
    public static class MemManager
    {
        public static void Init(SigScanner sigScanner)
        {
            // Check for valid SigScanner object
            if (sigScanner == null)
            {
                throw new Exception("Error in \"MemManager.Init()\": SigScanner is null!");
            }

            // Initialize delegate for file writing
            try
            {
                // Get Address of UserFileEvent_vf12
                IntPtr fpGetFileWriteAddress = sigScanner.ScanText("40 57 48 81 EC 50 02");
                if (fpGetFileWriteAddress != IntPtr.Zero)
                {
                    mdSaveFileDAT = Marshal.GetDelegateForFunctionPointer<FileWriteDelegate>(fpGetFileWriteAddress);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error in \"MemManager.Init()\" while searching for required function signatures; Raw exception as follows:\r\n{e}");
            }
        }

        public static void Uninit()
        {
            mdSaveFileDAT = null;
        }

        public unsafe static void WriteFile(byte* pModule)
        {
            if (mdSaveFileDAT == null)
            {
                throw new Exception("Error in \"MemManager.WriteFile()\": mdSaveFileDAT is not initialized!");
            }

            // Write to the file
            mdSaveFileDAT.Invoke(pModule, (char)0x01);
        }

        // Delegates
        // Not completely sure what char is intended for but it essentially functions
        // As a way to write to the file even if the proper flag isn't checked in memory
        private unsafe delegate char FileWriteDelegate(byte* pModule, char forceWrite);

        private static FileWriteDelegate? mdSaveFileDAT;
    }
}

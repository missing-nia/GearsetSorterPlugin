using System;
using System.Runtime.InteropServices;

using Dalamud.Game;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;

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

        // Quicksort
        public unsafe static void GearsetSort(RaptureGearsetModule* pGearsetModule, int lo, int hi)
        {
            if (lo < hi)
            {
                int pi = GearsetPartition(pGearsetModule, lo, hi);

                GearsetSort(pGearsetModule, lo, pi - 1);
                GearsetSort(pGearsetModule, pi + 1, hi);
            }
        }

        public unsafe static int GearsetPartition(RaptureGearsetModule* pGearsetModule, int lo, int hi)
        {
            // Get the current active gearset in memory
            byte *currentGearset = (byte*)pGearsetModule + mCurrentGearsetOffset;
            //PluginLog.LogInformation($"CurrentGearset: 0x{*currentGearset}");

            String pivotName = System.Text.Encoding.UTF8.GetString(pGearsetModule->Gearset[hi]->Name, mGearsetEntryNameSize);
            RaptureGearsetModule.GearsetFlag pivotFlag = pGearsetModule->Gearset[hi]->Flags;

            int i = lo - 1;
            for (int j = lo; j <= hi - 1; ++j)
            {
                String curName = System.Text.Encoding.UTF8.GetString(pGearsetModule->Gearset[j]->Name, mGearsetEntryNameSize);
                RaptureGearsetModule.GearsetFlag curFlag = pGearsetModule->Gearset[j]->Flags;

                // Sorting by unicode value so this should essentially be alphabetical
                // So this is a bit of a headache but essentially sets that dont exist need
                // To be sorted to the back so that our sorted list wont have gaps in it
                // But because deleted sets wont actually be wiped clean we need to make sure
                // That we put them back there regardless
                if (curFlag.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
                {
                    if (!pivotFlag.HasFlag(RaptureGearsetModule.GearsetFlag.Exists) || String.Compare(curName, pivotName, StringComparison.Ordinal) < 0)
                    {
                        ++i;

                        // Don't swap if we're referencing the same place in memory for both
                        if (i != j)
                        {
                            GearsetSwap(currentGearset, pGearsetModule->Gearset[i], pGearsetModule->Gearset[j]);
                        }
                    }
                }
            }
            GearsetSwap(currentGearset, pGearsetModule->Gearset[i + 1], pGearsetModule->Gearset[hi]);
            return (i + 1);
        }

        public unsafe static void GearsetSwap(byte* currentGearset, RaptureGearsetModule.GearsetEntry* pGearsetA, RaptureGearsetModule.GearsetEntry* pGearsetB)
        {
            if (pGearsetA == null || pGearsetB == null)
            {
                throw new Exception("Error in \"MemManager.GearsetSwap()\": Null GearsetEntry pointer.");
            }

            int gearsetEntrySize = sizeof(RaptureGearsetModule.GearsetEntry);

            // If one of these is the current active gearset we need to update the ID since we're swapping positions
            if (pGearsetA->ID == *currentGearset)
            {
                *currentGearset = pGearsetB->ID;
            }
            else if (pGearsetB->ID == *currentGearset)
            {
                *currentGearset = pGearsetA->ID;
            }

            // Update the hotbars before swapping
            GearsetUpdateHotbars(pGearsetA, pGearsetB);

            // Update gearset number before swapping
            byte tempID = pGearsetA->ID;
            pGearsetA->ID = pGearsetB->ID;
            pGearsetB->ID = tempID;

            // Swap in memory
            byte[] tempGearsetA = new byte[gearsetEntrySize];
            Marshal.Copy((IntPtr)pGearsetA, tempGearsetA, 0, gearsetEntrySize);

            byte[] tempGearsetB = new byte[gearsetEntrySize];
            Marshal.Copy((IntPtr)pGearsetB, tempGearsetB, 0, gearsetEntrySize);

            Marshal.Copy(tempGearsetA, 0, (IntPtr)pGearsetB, gearsetEntrySize);
            Marshal.Copy(tempGearsetB, 0, (IntPtr)pGearsetA, gearsetEntrySize);
        }

        // Update the hotbar IDs every time we swap things around, both in the active set and the saved sets
        public unsafe static void GearsetUpdateHotbars(RaptureGearsetModule.GearsetEntry* pGearsetA, RaptureGearsetModule.GearsetEntry* pGearsetB)
        {
            RaptureHotbarModule* pHotbarModule = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureHotbarModule();

            // Iterate through all the saved hotbars and update
            // These are what are written to the file
            for (int i = 0; i < 60; ++i)
            {
                // Each of these should be a set of hotbars for any given job (10 hotbars + 8 crossbars)
                SavedHotBars.SavedHotBarClassJob* pClassJobHotbars = pHotbarModule->SavedClassJob[i];
                for (int j = 0; j < 18; ++j)
                {
                    // Each of these should be a hotbar for the given job (16 slots for crossbars)
                    SavedHotBars.SavedHotBarClassJobBars.SavedHotBarClassJobBar* pHotbar = pClassJobHotbars->Bar[j];
                    for (int k = 0; k < 16; ++k)
                    {
                        // Each of these should be a hotbar slot although in practice many should be empty or unusable
                        SavedHotBars.SavedHotBarClassJobSlots.SavedHotBarClassJobSlot* pHotbarSlot = pHotbar->Slot[k];

                        // Make sure this is a gearset slot
                        if (pHotbarSlot->Type == HotbarSlotType.GearSet)
                        {
                            // Check if this slot is for either of the swapping gearsets
                            if (pHotbarSlot->ID == pGearsetA->ID)
                            {
                                pHotbarSlot->ID = pGearsetB->ID;
                            }
                            else if (pHotbarSlot->ID == pGearsetB->ID)
                            {
                                pHotbarSlot->ID = pGearsetA->ID;
                            }
                        }
                    }
                }
            }

            // Iterate through the actively loaded hotbars and update
            // These are not written to the file ever but instead are loaded 
            // Versions of the SavedHotBars that the player can actually use
            for (int i = 0; i < 18; ++i )
            {
                // Each of these should be a currently loaded hotbar in memory (16 slots for crossbars)
                HotBar* pHotbar = pHotbarModule->HotBar[i];
                for (int j = 0; j < 16; ++j)
                {
                    // Each of these should be a hotbar slot although in practice many should be empty or unusable
                    HotBarSlot* pHotbarSlot = pHotbar->Slot[j];

                    // Make sure this is a gearset slot
                    if (pHotbarSlot->CommandType == HotbarSlotType.GearSet)
                    {
                        // Check if this slot is for either of the swapping gearsets
                        if (pHotbarSlot->CommandId == pGearsetA->ID)
                        {
                            pHotbarSlot->Set(HotbarSlotType.GearSet, pGearsetB->ID);
                        }
                        else if (pHotbarSlot->CommandId == pGearsetB->ID)
                        {
                            pHotbarSlot->Set(HotbarSlotType.GearSet, pGearsetA->ID);
                        }
                    }
                }
            }
        }

        // Magic Numbers
        private static readonly ushort mCurrentGearsetOffset = 0xAF74; // Offset in memory for the currently active gearset

        //private static readonly byte mNoActiveGearset = 0xFF; // Assigned value for when the player does not have a stored gearset active
        private static readonly byte mGearsetEntryNameSize = 0x2F; // Size of the Name field in GearsetEntry (I dont really like this but idk how else to get this cleanly)

        // Delegates
        // Not completely sure what char is intended for but it essentially functions
        // As a way to write to the file even if the proper flag isn't checked in memory
        private unsafe delegate char FileWriteDelegate(byte* pModule, char forceWrite);

        private static FileWriteDelegate? mdSaveFileDAT;

        // GearsetEntry.ClassJob attribute values
        // TODO: implement role sorting + a user config to customize role order (default is by implemenation order so it's fucked lmao)
        public enum GearsetClassJob : byte
        {
            GLD = 0x01,
            PUG = 0x02,
            MRD = 0x03,
            LNC = 0x04,
            ARC = 0x05,
            CNJ = 0x06,
            THM = 0x07,
            CRP = 0x08,
            BSM = 0x09,
            ARM = 0x0A,
            GSM = 0x0B,
            LTW = 0x0C,
            WVR = 0x0D,
            ALC = 0x0E,
            CUL = 0x0F,
            MIN = 0x10,
            BTN = 0x11,
            FSH = 0x12,
            PLD = 0x13,
            MNK = 0x14,
            WAR = 0x15,
            DRG = 0x16,
            BRD = 0x17,
            WHM = 0x18,
            BLM = 0x19,
            ACN = 0x1A,
            SMN = 0x1B,
            SCH = 0x1C,
            ROG = 0x1D,
            NIN = 0x1E,
            MCH = 0x1F,
            DRK = 0x20,
            AST = 0x21,
            SAM = 0x22,
            RDM = 0x23,
            BLU = 0x24,
            GNB = 0x25,
            DNC = 0x26,
            RPR = 0x27,
            SGE = 0x28
        }
    }
}

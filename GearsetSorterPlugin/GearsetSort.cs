using System;
using System.Runtime.InteropServices;

using Dalamud.Game;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace GearsetSorterPlugin
{
    public unsafe static class GearsetSort 
    {
        public static void Init()
        {
            mpGearsetModule = RaptureGearsetModule.Instance();
            mpHotbarModule = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureHotbarModule();

            // Get the current active gearset in memory
            mpCurrentGearset = (byte*)mpGearsetModule + mCurrentGearsetOffset;
            PluginLog.LogInformation($"CurrentGearset: 0x{*mpCurrentGearset}");
        }

        public static void Uninit()
        {
            mpGearsetModule = null;
            mpHotbarModule = null;
            mpCurrentGearset = null;
        }

        // Quicksort
        public static void Sort(int lo, int hi)
        {
            if (lo < hi)
            {
                int pi = Partition(lo, hi);

                Sort(lo, pi - 1);
                Sort(pi + 1, hi);
            }
        }

        private static int Partition(int lo, int hi)
        {
            String pivotName = System.Text.Encoding.UTF8.GetString(mpGearsetModule->Gearset[hi]->Name, mGearsetEntryNameSize);
            RaptureGearsetModule.GearsetFlag pivotFlag = mpGearsetModule->Gearset[hi]->Flags;

            int i = lo - 1;
            for (int j = lo; j <= hi - 1; ++j)
            {
                String curName = System.Text.Encoding.UTF8.GetString(mpGearsetModule->Gearset[j]->Name, mGearsetEntryNameSize);
                RaptureGearsetModule.GearsetFlag curFlag = mpGearsetModule->Gearset[j]->Flags;

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
                            Swap(mpGearsetModule->Gearset[i], mpGearsetModule->Gearset[j]);
                        }
                    }
                }
            }
            Swap(mpGearsetModule->Gearset[i + 1], mpGearsetModule->Gearset[hi]);
            return (i + 1);
        }

        private static void Swap(RaptureGearsetModule.GearsetEntry* pGearsetA, RaptureGearsetModule.GearsetEntry* pGearsetB)
        {
            if (pGearsetA == null || pGearsetB == null)
            {
                throw new Exception("Error in \"MemManager.GearsetSwap()\": Null GearsetEntry pointer.");
            }

            int gearsetEntrySize = sizeof(RaptureGearsetModule.GearsetEntry);

            // If one of these is the current active gearset we need to update the ID since we're swapping positions
            if (pGearsetA->ID == *mpCurrentGearset)
            {
                *mpCurrentGearset = pGearsetB->ID;
            }
            else if (pGearsetB->ID == *mpCurrentGearset)
            {
                *mpCurrentGearset = pGearsetA->ID;
            }

            // Update the hotbars before swapping
            UpdateHotbars(pGearsetA, pGearsetB);

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
        private static void UpdateHotbars(RaptureGearsetModule.GearsetEntry* pGearsetA, RaptureGearsetModule.GearsetEntry* pGearsetB)
        {
            // Iterate through all the saved hotbars and update
            // These are what are written to the file
            for (int i = 0; i < 60; ++i)
            {
                // Each of these should be a set of hotbars for any given job (10 hotbars + 8 crossbars)
                SavedHotBars.SavedHotBarClassJob* pClassJobHotbars = mpHotbarModule->SavedClassJob[i];
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
            for (int i = 0; i < 18; ++i)
            {
                // Each of these should be a currently loaded hotbar in memory (16 slots for crossbars)
                HotBar* pHotbar = mpHotbarModule->HotBar[i];
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

        private static RaptureGearsetModule* mpGearsetModule;
        private static RaptureHotbarModule* mpHotbarModule;

        private static byte* mpCurrentGearset;
    }
}

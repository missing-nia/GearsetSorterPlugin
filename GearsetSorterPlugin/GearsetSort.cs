using System;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using GearsetSorterPlugin.Enum;

namespace GearsetSorterPlugin
{
    public unsafe static class GearsetSort 
    {
        public static void Init(byte[] sortOrder)
        {
            mpGearsetModule = RaptureGearsetModule.Instance();
            mpHotbarModule = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureHotbarModule();

            // Get the current active gearset in memory
            mpCurrentGearset = (byte*)mpGearsetModule + mCurrentGearsetOffset;
            PluginLog.LogInformation($"CurrentGearset: 0x{*mpCurrentGearset}");

            mClassJobSortOrder = sortOrder;
        }

        public static void Uninit()
        {
            mpGearsetModule = null;
            mpHotbarModule = null;
            mpCurrentGearset = null;
            mClassJobSortOrder = null;
            mReverseItemLevelSort = false;
            mReverseNameSort = false;
        }
        public static void setClassJobSortOrder(byte[] sortOrder)
        {
            mClassJobSortOrder = sortOrder;
        }
        public static void setItemLevelSort(bool reverseItemLevelSort)
        {
            mReverseItemLevelSort = reverseItemLevelSort;
        }

        public static void setNameSort(bool reverseNameSort)
        {
            mReverseNameSort = reverseNameSort;
        }

        // Quicksort
        public static void Sort(int lo, int hi, GearsetSortType sortTypePrimary, GearsetSortType sortTypeSecondary)
        {
            if (lo < hi)
            {
                int pi = Partition(lo, hi, sortTypePrimary, sortTypeSecondary);

                Sort(lo, pi - 1, sortTypePrimary, sortTypeSecondary);
                Sort(pi + 1, hi, sortTypePrimary, sortTypeSecondary);
            }
        }

        private static int Partition(int lo, int hi, GearsetSortType sortTypePrimary, GearsetSortType sortTypeSecondary)
        {
            RaptureGearsetModule.GearsetFlag pivotFlag = mpGearsetModule->Gearset[hi]->Flags;

            int i = lo - 1;
            for (int j = lo; j <= hi - 1; ++j)
            {
                RaptureGearsetModule.GearsetFlag curFlag = mpGearsetModule->Gearset[j]->Flags;

                // Sorting by unicode value so this should essentially be alphabetical
                // So this is a bit of a headache but essentially sets that dont exist need
                // To be sorted to the back so that our sorted list wont have gaps in it
                // But because deleted sets wont actually be wiped clean we need to make sure
                // That we put them back there regardless
                if (curFlag.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
                {
                    // First try to sort using the primary sort type
                    int res = GearsetCompare(hi, j, sortTypePrimary);
                    bool shouldSwap = ShouldSwap(res, sortTypePrimary);
                    if (res == 0)
                    {
                        // Primary sort values are the same so sort using the secondary sort
                        res = GearsetCompare(hi, j, sortTypeSecondary);
                        shouldSwap = ShouldSwap(res, sortTypeSecondary);
                    }

                    // Only sort if the pivot doesn't exist or if the current set should be earlier in the list
                    if (!pivotFlag.HasFlag(RaptureGearsetModule.GearsetFlag.Exists) || shouldSwap)
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

        // Helper function to identify if the current gearsets should be swapped
        // This helps facilitate easier use of reverse item level and name sorting
        private static bool ShouldSwap(int res, GearsetSortType sortType)
        {
            // Check if the sort needs to be reversed
            if ((sortType == GearsetSortType.Name && mReverseNameSort) ||
                (sortType == GearsetSortType.ItemLevel && mReverseItemLevelSort))
            {
                if (res > 0)
                {
                    return true;
                }    
            }
            else if (res < 0)
            {
                return true;
            }
            return false;
        }

        private static int GearsetCompare(int pivot, int cur, GearsetSortType sortType)
        {
            if (sortType == GearsetSortType.Name)
            {
                // Sorting by Name
                String pivotName = System.Text.Encoding.UTF8.GetString(mpGearsetModule->Gearset[pivot]->Name, mGearsetEntryNameSize);
                String curName = System.Text.Encoding.UTF8.GetString(mpGearsetModule->Gearset[cur]->Name, mGearsetEntryNameSize);

                return String.Compare(curName, pivotName, StringComparison.Ordinal);
            }   
            else if (sortType == GearsetSortType.ClassJob)
            {
                // Sorting By Class/Job
                byte pivotClassJob = mpGearsetModule->Gearset[pivot]->ClassJob;
                byte curClassJob = mpGearsetModule->Gearset[cur]->ClassJob;

                return ClassJobComparator(pivotClassJob, curClassJob);
            }
            else if (sortType == GearsetSortType.ItemLevel)
            {
                // Sorting by Item Level
                short pivotItemLevel = mpGearsetModule->Gearset[pivot]->ItemLevel;
                short curItemLevel = mpGearsetModule->Gearset[cur]->ItemLevel;

                return curItemLevel.CompareTo(pivotItemLevel);
            }
            return 1;
        }

        private static int ClassJobComparator(byte pivotClassJob, byte curClassJob)
        {
            if (mClassJobSortOrder == null)
            {
                throw new Exception("Error in \"GearsetSort.ShouldSwap()\": mJobClassSortOrder is not initialized!");
            }

            // Check where in the priority pivot and cur are
            int pivotClassJobPos = Array.IndexOf(mClassJobSortOrder, pivotClassJob);
            int curClassJobPos = Array.IndexOf(mClassJobSortOrder, curClassJob);

            if (curClassJobPos == pivotClassJobPos)
            {
                return 0;
            }
            else if (curClassJobPos < pivotClassJobPos)
            {
                return -1;
            }
            return 1;
        }

        private static void Swap(RaptureGearsetModule.GearsetEntry* pGearsetA, RaptureGearsetModule.GearsetEntry* pGearsetB)
        {
            if (pGearsetA == null || pGearsetB == null)
            {
                throw new Exception("Error in \"GearsetSort.Swap()\": Null GearsetEntry pointer.");
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

        // Sort order for JobClass
        private static byte[] ?mClassJobSortOrder;
        private static bool mReverseItemLevelSort;
        private static bool mReverseNameSort;
    }
}

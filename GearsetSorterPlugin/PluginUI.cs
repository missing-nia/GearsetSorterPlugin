using ImGuiNET;
using System;
using GearsetSorterPlugin.Enum;

namespace GearsetSorterPlugin
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration mConfiguration;

        private bool mSettingsVisible = false;
        public bool SettingsVisible
        {
            get { return mSettingsVisible; }
            set { mSettingsVisible = value; }
        }

        // passing in the image here just for simplicity
        public PluginUI(Configuration configuration)
        {
            mConfiguration = configuration;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            DrawSettingsWindow();
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            if (ImGui.Begin("Gearset Settings", ref mSettingsVisible,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // Drop down list for primary sort
                var primarySort = mConfiguration.PrimarySort;
                if (ImGui.BeginCombo("Primary Sort", GearsetSortTypeString((GearsetSortType)primarySort)))
                {
                    ImGui.Separator();

                    // List all of the available sorting types
                    for (var sortType = 0; sortType < System.Enum.GetNames(typeof(GearsetSortType)).Length; ++sortType)
                    {
                        if (!ImGui.Selectable(GearsetSortTypeString((GearsetSortType)sortType)))
                        {
                            continue;
                        }

                        mConfiguration.PrimarySort = sortType;
                        if (mConfiguration.PrimarySort == mConfiguration.SecondarySort)
                        {
                            // Change the secondary sort if the primary sort is changed to the same thing
                            switch ((GearsetSortType)mConfiguration.PrimarySort)
                            {
                                case GearsetSortType.Name:
                                    mConfiguration.SecondarySort = (int)GearsetSortType.ClassJob;
                                    break;
                                case GearsetSortType.ClassJob:
                                    mConfiguration.SecondarySort = (int)GearsetSortType.ItemLevel;
                                    break;
                                case GearsetSortType.ItemLevel:
                                    mConfiguration.SecondarySort = (int)GearsetSortType.Name;
                                    break;
                            }
                        }
                        mConfiguration.Save();
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                HelpMarker(
                    "Gearsets will be sorted using this first\n\n" +
                    "Name will sort gearsets alphabetically\n" +
                    "Class/Job will sort gearsets by a customizable class/job order\n" +
                    "Item Level will sort gearsets by item level (ilvl)");

                // Drop down list for secondary sort
                var secondarySort = mConfiguration.SecondarySort;
                if (ImGui.BeginCombo("Secondary Sort", GearsetSortTypeString((GearsetSortType)secondarySort)))
                {
                    ImGui.Separator();

                    // List all of the available sorting types
                    for (var sortType = 0; sortType < System.Enum.GetNames(typeof(GearsetSortType)).Length; ++sortType)
                    {
                        // Don't draw a sort type if it's already the primary sorting type
                        if (sortType != mConfiguration.PrimarySort)
                        {
                            if (!ImGui.Selectable(GearsetSortTypeString((GearsetSortType)sortType)))
                            {
                                continue;
                            }

                            mConfiguration.SecondarySort = sortType;
                            mConfiguration.Save();
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                HelpMarker(
                    "Gearsets will be sorted using this in the case that first sort values are equal (i.e. sorted by \"Name\" and both gearsets are named \"Paladin\")\n\n" +
                    "Name will sort gearsets alphabetically\n" +
                    "Class/Job will sort gearsets by a customizable class/job order\n" +
                    "Item Level will sort gearsets by item level (ilvl)");

                // Reverse item level sorting order
                var sortItemLevelReverse = mConfiguration.SortItemLevelReverse;
                if (ImGui.Checkbox("Sort Item Level From Greatest to Least", ref sortItemLevelReverse))
                {
                    mConfiguration.SortItemLevelReverse = sortItemLevelReverse;
                    mConfiguration.Save();
                }
                ImGui.SameLine();
                HelpMarker(
                    "Item level sorting by default sorts from least to greatest (i.e. 1,2,3)\n\n" +
                    "When this setting is enabled item level sorting\n" +
                    "will instead sort in reverse (i.e. 3,2,1)");

                // Reverse name sorting order
                var sortNameReverse = mConfiguration.SortNameReverse;
                if (ImGui.Checkbox("Sort Name in Reverse", ref sortNameReverse))
                {
                    mConfiguration.SortNameReverse = sortNameReverse;
                    mConfiguration.Save();
                }
                ImGui.SameLine();
                HelpMarker(
                    "Name sorting by default sorts alphabetically (i.e. a,b,c)\n\n" +
                    "When this setting is enabled name sorting\n" +
                    "will instead sort in reverse (i.e. c,b,a)");

                // Sort order that will be used for ClassJob sorting
                // TODO: render this in its own window (very large maybe work on that?) 
                // TODO: add job icons and maybe color coding (it's really hard to find what you're looking for quickly)
                if (ImGui.CollapsingHeader("Class/Job Sort Order"))
                {
                    if (ImGui.TreeNode("Class/Job sorting order"))
                    {
                        var sortOrder = mConfiguration.ClassJobSortOrder;
                        for (var i = 0; i < sortOrder.Length; ++i)
                        {
                            var classJob = sortOrder[i];
                            var classJobString = GearsetClassJobString((GearsetClassJob)classJob);
                            ImGui.Selectable(classJobString);

                            if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                            {
                                // Update sort order when elements are dragged around
                                var next = i + (ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Y < 0.0f ? -1 : 1);
                                if (next >= 0 && next < sortOrder.Length)
                                {
                                    sortOrder[i] = sortOrder[next];
                                    sortOrder[next] = classJob;
                                    mConfiguration.ClassJobSortOrder = sortOrder;
                                    mConfiguration.Save();

                                    ImGui.ResetMouseDragDelta();
                                }
                            }
                        }
                        ImGui.TreePop();
                    }
                }
            }
            ImGui.End();
        }

        // Adapted from https://github.com/ocornut/imgui/blob/master/imgui_demo.cpp
        // Hover over help text for difference config settings
        private void HelpMarker(string desc)
        {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize()* 35.0f);
                ImGui.TextUnformatted(desc);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        // Conversion from GearsetSortType to UI strings
        public static string GearsetSortTypeString(GearsetSortType sortType) =>
            sortType switch
            {
                GearsetSortType.Name => "Name",
                GearsetSortType.ClassJob => "Class/Job",
                GearsetSortType.ItemLevel => "Item Level",
                _ => throw new System.ArgumentException(message: "invalid enum value", paramName: nameof(sortType)),
            };

        // Conversion from GearsetClassJob to UI strings
        public static string GearsetClassJobString(GearsetClassJob classJob) =>
            classJob switch
            {
                GearsetClassJob.GLD => "GLD",
                GearsetClassJob.PUG => "PUG",
                GearsetClassJob.MRD => "MRD",
                GearsetClassJob.LNC => "LNC",
                GearsetClassJob.ARC => "ARC",
                GearsetClassJob.CNJ => "CNJ",
                GearsetClassJob.THM => "THM",
                GearsetClassJob.CRP => "CRP",
                GearsetClassJob.BSM => "BSM",
                GearsetClassJob.ARM => "ARM",
                GearsetClassJob.GSM => "GSM",
                GearsetClassJob.LTW => "LTW",
                GearsetClassJob.WVR => "WVR",
                GearsetClassJob.ALC => "ALC",
                GearsetClassJob.CUL => "CUL",
                GearsetClassJob.MIN => "MIN",
                GearsetClassJob.BTN => "BTN",
                GearsetClassJob.FSH => "FSH",
                GearsetClassJob.PLD => "PLD",
                GearsetClassJob.MNK => "MNK",
                GearsetClassJob.WAR => "WAR",
                GearsetClassJob.DRG => "DRG",
                GearsetClassJob.BRD => "BRD",
                GearsetClassJob.WHM => "WHM",
                GearsetClassJob.BLM => "BLM",
                GearsetClassJob.ACN => "ACN",
                GearsetClassJob.SMN => "SMN",
                GearsetClassJob.SCH => "SCH",
                GearsetClassJob.ROG => "ROG",
                GearsetClassJob.NIN => "NIN",
                GearsetClassJob.MCH => "MCH",
                GearsetClassJob.DRK => "DRK",
                GearsetClassJob.AST => "AST",
                GearsetClassJob.SAM => "SAM",
                GearsetClassJob.RDM => "RDM",
                GearsetClassJob.BLU => "BLU",
                GearsetClassJob.GNB => "GNB",
                GearsetClassJob.DNC => "DNC",
                GearsetClassJob.RPR => "RPR",
                GearsetClassJob.SGE => "SGE",
                _ => throw new System.ArgumentException(message: "invalid enum value", paramName: nameof(classJob)),
            };
    }
}

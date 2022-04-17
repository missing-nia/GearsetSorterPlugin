using ImGuiNET;
using System;
using System.Numerics;

namespace GearsetSorterPlugin
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration mConfiguration;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool mVisible = false;
        public bool Visible
        {
            get { return mVisible; }
            set { mVisible = value; }
        }

        private bool mSettingsVisible = false;
        public bool SettingsVisible
        {
            get { return mSettingsVisible; }
            set { mSettingsVisible = value; }
        }

        // passing in the image here just for simplicity
        public PluginUI(Configuration configuration, ImGuiScene.TextureWrap goatImage)
        {
            mConfiguration = configuration;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawMainWindow();
            DrawSettingsWindow();
        }

        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("My Amazing Window", ref mVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text($"The random config bool is {mConfiguration.SomePropertyToBeSavedAndWithADefault}");

                if (ImGui.Button("Show Settings"))
                {
                    SettingsVisible = true;
                }

                ImGui.Spacing();

                ImGui.Indent(55);
                ImGui.Unindent(55);
            }
            ImGui.End();
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
                // can't ref a property, so use a local copy
                var configValue = mConfiguration.SomePropertyToBeSavedAndWithADefault;
                if (ImGui.Checkbox("Random Config Bool", ref configValue))
                {
                    mConfiguration.SomePropertyToBeSavedAndWithADefault = configValue;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    mConfiguration.Save();
                }
            }
            ImGui.End();
        }
    }
}

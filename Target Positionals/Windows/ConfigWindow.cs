using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using SamplePlugin;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin plugin;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("Directional Config Window###ConfigWindow")
    {
        this.plugin = plugin;
        Configuration = plugin.Configuration;

        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(300, 150);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (Configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        var cardinals = Configuration.DrawCardinals; //Draw Cardinals
        var interCardinals = Configuration.DrawInterCardinals; //Draw InterCardinals
        var alwaysDraw = Configuration.AlwaysDrawDirections; // Always draw regardless of target type
        if (ImGui.Checkbox("Cardinals", ref cardinals))
        {
            Configuration.DrawCardinals = cardinals;
            ;
            Configuration.Save();
        }
        if (ImGui.Checkbox("InterCardinals", ref interCardinals))
            {
            Configuration.DrawInterCardinals = interCardinals;
            Configuration.Save();
        }

        if (ImGui.Checkbox("Show on Non-Enemies", ref alwaysDraw))
        {
            Configuration.AlwaysDrawDirections = alwaysDraw;
            Configuration.Save();
        }
    }
}

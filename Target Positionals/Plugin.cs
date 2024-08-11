using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState;
using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Runtime.CompilerServices;

namespace SamplePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;

        private const string CommandName = "/targetcompass?";
        public Configuration Configuration { get; init; }
        public readonly WindowSystem WindowSystem = new("Directionals");
        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        public string Name => "Positional Direction Compass";

        public Plugin(IDalamudPluginInterface pluginInterface)
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this);
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
            PluginInterface = pluginInterface;

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Draws the compass directionals around the target for easier callouts"
            });
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.Draw -= ToggleConfigUI;
            PluginInterface.UiBuilder.Draw -= ToggleMainUI;
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            CommandManager.RemoveHandler(CommandName);
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
            if (ClientState.LocalPlayer == null)
            {
                return;
            }

            var target = ClientState.LocalPlayer.TargetObject; //The target object based on what the player has clicked
            if (target != null)
            {
                if (Configuration.AlwaysDrawDirections == false && target is IBattleNpc battleNpc && IsHostile(battleNpc))
                {
                    if (Configuration.DrawCardinals)
                    {
                        DrawCardinals(target);
                    }
                    if (Configuration.DrawInterCardinals)
                    {
                        DrawIntercardinals(target);
                    }
                }
                else
                {
                    if (Configuration.AlwaysDrawDirections)
                    {
                        if (target is IBattleNpc)
                            if (Configuration.DrawCardinals)
                            {
                                DrawCardinals(target);
                            }
                        if (Configuration.DrawInterCardinals)
                        {
                            DrawIntercardinals(target);
                        }
                    }
                }
            }
        }

        private void DrawText(string text, float x, float y, uint color)
        {
            var drawList = ImGui.GetForegroundDrawList();
            drawList.AddText(new Vector2(x, y), color, text);
        }

        private bool IsHostile(IBattleNpc npc)
        {
            return npc.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.Hostile);
        }

        private void DrawCardinals(IGameObject target)
        {
            var position = target.Position; //Targets position
            var radius = target.HitboxRadius; //Targets hitbox radius
            var color = 0xFF0000FF; // Red color

            // Get target's forward direction
            var forward = new Vector3((float)Math.Sin(target.Rotation), 0, (float)Math.Cos(target.Rotation)); //Cheating to get the forward direction
            var right = new Vector3((float)Math.Sin(target.Rotation + MathF.PI / 2), 0, (float)Math.Cos(target.Rotation + MathF.PI / 2));

            // North
            var northPos = position + forward * radius;
            var northScreenPos = WorldToScreen(northPos);
            DrawText("N", northScreenPos.X, northScreenPos.Y, color);

            // South
            var southPos = position - forward * radius;
            var southScreenPos = WorldToScreen(southPos);
            DrawText("S", southScreenPos.X, southScreenPos.Y, color);

            // East
            var eastPos = position + right * radius;
            var eastScreenPos = WorldToScreen(eastPos);
            DrawText("E", eastScreenPos.X, eastScreenPos.Y, color);

            // West
            var westPos = position - right * radius;
            var westScreenPos = WorldToScreen(westPos);
            DrawText("W", westScreenPos.X, westScreenPos.Y, color);
        }

        private void DrawIntercardinals(IGameObject target)
        {
            var position = target.Position;
            var radius = target.HitboxRadius;
            var color = 0xFF0000FF; // Red color

            // Get target's forward direction
            var forward = new Vector3((float)Math.Sin(target.Rotation), 0, (float)Math.Cos(target.Rotation));
            var right = new Vector3((float)Math.Sin(target.Rotation + MathF.PI / 2), 0, (float)Math.Cos(target.Rotation + MathF.PI / 2));

            // Northeast
            var northeastPos = position + forward * (radius * MathF.Sqrt(0.5f)) + right * (radius * MathF.Sqrt(0.5f));
            var northeastScreenPos = WorldToScreen(northeastPos);
            DrawText("NE", northeastScreenPos.X, northeastScreenPos.Y, color);

            // Southeast
            var southeastPos = position - forward * (radius * MathF.Sqrt(0.5f)) + right * (radius * MathF.Sqrt(0.5f));
            var southeastScreenPos = WorldToScreen(southeastPos);
            DrawText("SE", southeastScreenPos.X, southeastScreenPos.Y, color);

            // Southwest
            var southwestPos = position - forward * (radius * MathF.Sqrt(0.5f)) - right * (radius * MathF.Sqrt(0.5f));
            var southwestScreenPos = WorldToScreen(southwestPos);
            DrawText("SW", southwestScreenPos.X, southwestScreenPos.Y, color);

            // Northwest
            var northwestPos = position + forward * (radius * MathF.Sqrt(0.5f)) - right * (radius * MathF.Sqrt(0.5f));
            var northwestScreenPos = WorldToScreen(northwestPos);
            DrawText("NW", northwestScreenPos.X, northwestScreenPos.Y, color);
        }

        private Vector2 WorldToScreen(Vector3 worldPos)
        {
            GameGui.WorldToScreen(worldPos, out Vector2 screenPos);
            return screenPos;
        }

        private void OnCommand(string command, string args)
        {
            Boolean enabled = false;
            if (!enabled)
            {
                ConfigWindow.Toggle(); //Config Window Open
                enabled = true;
            } else if (enabled) {
                ConfigWindow.Toggle(); //Config Window Closed
            }
        }

        public void ToggleConfigUI() => ConfigWindow.Toggle();
        public void ToggleMainUI() => MainWindow.Toggle();
    }
}

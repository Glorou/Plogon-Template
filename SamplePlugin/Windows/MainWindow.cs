using System;
using System.Globalization;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using Pictomancy;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using SamplePlugin.Apricot;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private string GoatImagePath;
    private Plugin Plugin;

    private string addressStr;
    private nint address = 0;
    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string goatImagePath)
        : base("My Amazing Window##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        GoatImagePath = goatImagePath;
        Plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        
        // Do not use .Text() or any other formatted function like TextWrapped(), or SetTooltip().
        // These expect formatting parameter if any part of the text contains a "%", which we can't
        // provide through our bindings, leading to a Crash to Desktop.
        // Replacements can be found in the ImGuiHelpers Class

        ImGui.Spacing();
        if (ImGui.Button("Show VFX"))
        {
            Plugin.VfxManager.AddVfx(new Vfx("test", "fish_kemi00f", Plugin.ObjectTable.LocalPlayer!), 60000);
        }
        

        if (PictoService.VfxRenderer.Address != null)
        {
            ImGui.SameLine();
            unsafe
            {
                ImGui.TextUnformatted($"{(nint)PictoService.VfxRenderer.Address.data:X}");
                ImGui.TextUnformatted($"{Plugin.debugAddress:X}");
            }
            ImGui.SameLine();
            if (ImGui.Button("Copy Address"))
            {
                unsafe
                {
                    ImGui.SetClipboardText($"{(nint)PictoService.VfxRenderer.Address.data:X}");
                }
            }
        }


        // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
        // ImRaii takes care of this after the scope ends.
        // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        using (var child = ImRaii.Child("SomeChildWithAScrollbar", Vector2.Zero, true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                //ImGui.TextUnformatted($"{Plugin.AudioAsString()}");
                unsafe
                {
                    var core = Plugin._hooks.core;
                    if (core != null && address != 0)
                    {
                        ImGui.Text($"Apricot: ");
                        ImGui.SameLine();
                        var coreStr = $"{(nint)core:X}";
                        ImGui.InputText("##CorePtr", ref coreStr, 256);


                        var ptr = (VfxObject*)address;  //11:33:20.249 | INF | [VFXEditor] New Static: vfx/common/eff/fish_kemi00f.avfx 

                        
                        var resInst = ptr->ResourceInstance;
                        if (resInst == null) return;
		
                        ImGui.Text($"Resource Instance: ");
                        ImGui.SameLine();
                        var resStr = $"{(nint)resInst:X}";
                        ImGui.InputText("##ResInstPtr", ref resStr, 256);
		
                        ImGui.Text($"Apricot Handle: {resInst->Handle.Id:X} {resInst->Handle.Index:X}");
		
                        var inst = core->Data->GetIndex(resInst->Handle.Index)->Instance;
                        if (inst == null) return;
		
                        ImGui.Text($"Apricot Instance: ");
                        ImGui.SameLine();
                        var instStr = $"{(nint)inst:X}";
                        ImGui.InputText("##InstPtr", ref instStr, 256);

                        var doc = inst->Document;
                        if (doc == null) return;
		
                        ImGui.Text($"{doc->ParticleCount}");

                        if (doc->Particles != null) {
                            for (var i = 0; i < doc->ParticleCount; i++) {
                                var particle = doc->Particles + i;
                                ImGui.Text($"{(nint)particle:X}");

                                using var _ = ImRaii.PushIndent();

                                var col = particle->Rgb;
                                if (col == null) continue;

                                var keys = col->GetKeyCount();
                                ImGui.Text($"Keys: {keys}");

                                for (var k = 0; k < keys; k++) {
                                    var key = col->GetKey(k);
                                    ImGui.ColorEdit3($"##ColorEdit_{i}_{k}", ref key->Color, ImGuiColorEditFlags.Float);
                                }
                            }
                        }
                    }
                }
                // Example for other services that Dalamud provides.
                // ClientState provides a wrapper filled with information about the local player object and client.

                var localPlayer = Plugin.ClientState.LocalPlayer;
                if (localPlayer == null)
                {
                    ImGui.TextUnformatted("Our local player is currently not loaded.");
                    return;
                }

                if (!localPlayer.ClassJob.IsValid)
                {
                    ImGui.TextUnformatted("Our current job is currently not valid.");
                    return;
                }

                // ExtractText() should be the preferred method to read Lumina SeStrings,
                // as ToString does not provide the actual text values, instead gives an encoded macro string.
                ImGui.TextUnformatted($"Our current job is ({localPlayer.ClassJob.RowId}) \"{localPlayer.ClassJob.Value.Abbreviation.ExtractText()}\"");

                // Example for quarrying Lumina directly, getting the name of our current area.
                var territoryId = Plugin.ClientState.TerritoryType;
                if (Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territoryRow))
                {
                    ImGui.TextUnformatted($"We are currently in ({territoryId}) \"{territoryRow.PlaceName.Value.Name.ExtractText()}\"");
                }
                else
                {
                    ImGui.TextUnformatted("Invalid territory.");
                }
            }
        }
    }
}

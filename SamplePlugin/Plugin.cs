using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using SamplePlugin.Windows;
using Pictomancy;
using SamplePlugin.Apricot;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;

namespace SamplePlugin;

public unsafe sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    [PluginService]
    internal static IFramework Framework { get; private set; } = null!;

    [PluginService]
    internal static IObjectTable ObjectTable { get; private set; } = null!;

    [PluginService]
    internal static IGameInteropProvider InteropProvider { get; set; } = null!;

    internal static VfxManager VfxManager { get; private set; } = null!;

    private const string CommandName = "/pmycommand";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("SamplePlugin");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    

    public VfxHooks _hooks;
    public ApricotCore* core;

    public static uint debugAddress = 0;

    public byte* AudioStream;

    public unsafe class AudioHooks : IDisposable
    {
        private delegate ulong GetStreamPointer(byte* a1, ulong a2, nint a3);
        public byte* AudioPointer { get; private set; }
    
        [Signature("E8 ?? ?? ?? ?? 0F B6 0D ?? ?? ?? ?? 85 C0 41 8B DC", DetourName = nameof(PointerCapture))]
        private Hook<GetStreamPointer>? _getStreamPointer;

        private ulong PointerCapture(byte* thisPtr, ulong a2, nint a3)
        {
            Framework.RunOnFrameworkThread(() =>
            {
                AudioPointer = (thisPtr + (*(int*)(thisPtr + 0x100) * 2) * 0x8 + 0x38);
            });
            var temp = _getStreamPointer!.Original(thisPtr, a2, a3);
            return temp;
        }

        public AudioHooks()
        {
            InteropProvider.InitializeFromAttributes(this);
            
            _getStreamPointer?.Enable();
        }

        public void Dispose()
        {
            _getStreamPointer?.Dispose();
        }
    }


    public unsafe class VfxHooks : IDisposable
    {
        
        //public unsafe delegate ApricotCore* GetApricotCoreDelegate();

        [Signature("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 85 C0 48 8B 6C 24", DetourName = nameof(UnbindVfx))]
        private Hook<VfxSetupDelegate>? _vfxSetup;

        public ApricotCore* core = null!;

        public delegate nint VfxSetupDelegate(ApricotCore* core, nint unk1, byte* filePath, byte* vfxFileResource, uint fileSize, ResourceHandle* resourceHandle, uint unk2);

        public VfxHooks()
        {
            InteropProvider.InitializeFromAttributes(this);
            _vfxSetup?.Enable();
        }

        private unsafe nint UnbindVfx(ApricotCore* core, nint unk1, byte* filePath, byte* vfxFileResource, uint fileSize, ResourceHandle* resourceHandle, uint unk2)
        {
            this.core = core;
            
            var temp = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(filePath);
            var path = Encoding.UTF8.GetString(temp);
            if (string.Equals(path, "vfx/common/eff/fish_kemi00f.avfx"))
            {
                vfxFileResource[0x1074 + 0] = 0xff;
                vfxFileResource[0x1074 + 1] = 0xff;
                vfxFileResource[0x1074 + 2] = 0xff;
                vfxFileResource[0x1074 + 3] = 0xff;
                
            }
            byte* pointer = (byte*)_vfxSetup?.Original(core, unk1, filePath, vfxFileResource, fileSize, resourceHandle, unk2);
            return (IntPtr)pointer;
        }
        
        public void Dispose()
        {
            _vfxSetup?.Disable();
            _vfxSetup?.Dispose();
            //_getStreamPointer?.Dispose();
        }
    }
    public unsafe Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        
        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        VfxManager = new VfxManager(Framework);
        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        _hooks = new VfxHooks();
        PictoService.Initialize(PluginInterface);
    }

    public unsafe String AudioAsString()
    {
        var temp = String.Empty;
        
        Framework.RunOnFrameworkThread(() =>
        {
            for (var i = 0; i < 3840; i++)
            {
                //temp += _hooks.AudioPointer[i].ToString();
            }
        });
        return temp;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        VfxManager.Dispose();
        ConfigWindow.Dispose();
        MainWindow.Dispose();
        _hooks?.Dispose();
        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}

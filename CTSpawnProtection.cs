using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace CTSpawnProtection;

[MinimumApiVersion(43)]
public class CTSpawnProtection : BasePlugin, IPluginConfig<CTSpawnProtectionConfig>
{
    public override string ModuleName => "[CT] Spawn protection.";
    public override string ModuleAuthor => "livevilog";
    public override string ModuleVersion => "1.0.0";

    public CTSpawnProtectionConfig Config { get; set; } = new ();
    
    private readonly Dictionary<CCSPlayerController, bool> _protectedList = new ();
    private readonly Dictionary<CCSPlayerController, DateTime> _spawnTimings = new ();
    
    private Timer? _clearProtectionTimer;
    
    private int _protectionDuration = 0;

    public override void Load(bool hotReload)
    {
        if (hotReload)
        {
            InitializeLists();
        }
        
        _clearProtectionTimer = new Timer(.5f, ClearProtectionTimer, TimerFlags.REPEAT);
        Timers.Add(_clearProtectionTimer);
    }

    public override void Unload(bool hotReload)
    {
        _clearProtectionTimer!.Kill();
        Timers.Remove(_clearProtectionTimer);
        _clearProtectionTimer = null;
        
        _spawnTimings.Clear();
        _protectedList.Clear();
    }

    public void OnConfigParsed(CTSpawnProtectionConfig config)
    {
        this.Config = config;
        _protectionDuration = config.ProtectionDuration;
    }

    private void InitializeLists()
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            _protectedList[controller] = false;
            _spawnTimings[controller] = DateTime.Now;
        });
    }

    private void ClearProtectionTimer()
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            if (!_spawnTimings.TryGetValue(controller, out var time)) 
                return;

            if (_protectedList[controller] && (DateTime.Now - time).Seconds >= _protectionDuration)
            {
                _protectedList[controller] = false;
                controller.PrintToChat($" {Config.ProtectionEndMessage}");
            }
        });
    }

    #region Events 
    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo _)
    {
        _protectedList[@event.Userid] = true;
        _spawnTimings[@event.Userid] = DateTime.Now;
        
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo _)
    {
        if (!_protectedList[@event.Userid]) return HookResult.Continue;
        
        @event.Userid.PlayerPawn.Value.Health = Config.PreventiveHealth;
        
        return HookResult.Changed;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        _protectedList.Remove(@event.Userid);
        
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerConnectFull(EventPlayerDisconnect @event, GameEventInfo _)
    {
        _protectedList[@event.Userid] = false;
        
        return HookResult.Continue;
    }
    #endregion
    
    [ConsoleCommand("css_ctspawnprotection_duration", "Change the duration of the spawn protection.")]
    [CommandHelper(1, "<duration in seconds>", CommandUsage.SERVER_ONLY)]
    public void UpdateDurationCommand(CCSPlayerController? caller, CommandInfo info)
    {
        if (!int.TryParse(info.GetArg(1), out var total) || total == 0)
        {
            info.ReplyToCommand("[CTSpawnProtection] Invalid duration.");
            return;
        }

        _protectionDuration = total;
        
        info.ReplyToCommand($"[CTSpawnProtection] Protection duration changed to {total} seconds.");
    }
}
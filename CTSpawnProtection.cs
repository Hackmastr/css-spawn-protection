using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace CTSpawnProtection;

[MinimumApiVersion(110)]
public class CTSpawnProtection : BasePlugin, IPluginConfig<CTSpawnProtectionConfig>
{
    public override string ModuleName => "[CT] Spawn protection.";
    public override string ModuleAuthor => "livevilog";
    public override string ModuleVersion => "1.2.0";

    public CTSpawnProtectionConfig Config { get; set; } = new ();
    
    private readonly Dictionary<uint, bool> _protectedList = new ();
    private readonly Dictionary<uint, DateTime> _spawnTimings = new ();
    
    private Timer? _clearProtectionTimer;
    
    private int _protectionDuration;

    public override void Load(bool hotReload)
    {
        if (Config.Version != 3)
        {
            Console.WriteLine("[CTSpawnProtection] Please update your config to version 3.");
        }
        
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        
        if (hotReload)
        {
            InitializeLists();
        }
        
        _clearProtectionTimer = new Timer(.5f, ClearProtectionTimer, TimerFlags.REPEAT);
        Timers.Add(_clearProtectionTimer);

        if (Config.EnableRoundEndProtection)
        {
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        }
    }

    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
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

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            _protectedList[controller.Index] = true;
            _spawnTimings[controller.Index] = DateTime.Now;
        });
        
        return HookResult.Continue;
    }

    private void InitializeLists()
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            _protectedList[controller.Index] = false;
            _spawnTimings[controller.Index] = DateTime.Now;
        });
    }

    private void ClearProtectionTimer()
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            if (!_spawnTimings.TryGetValue(controller.Index, out var time)) 
                return;

            if (_protectedList[controller.Index] && (DateTime.Now - time).Seconds >= _protectionDuration)
            {
                _protectedList[controller.Index] = false;
                controller.PrintToChat($" {Config.ProtectionEndMessage}");
            }
        });
    }

    #region Events 
    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo _)
    {
        _protectedList[@event.Userid.Index] = true;
        _spawnTimings[@event.Userid.Index] = DateTime.Now;
        
        return HookResult.Continue;
    }

    private HookResult OnTakeDamage(DynamicHook hook)    
    {
        var entindex = hook.GetParam<CEntityInstance>(0).Index;
       
        if (entindex == 0)
            return HookResult.Continue;

        var pawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>((int)entindex);
        
        if (pawn?.OriginalController?.Value is not { } player)
            return HookResult.Continue;

        if (_protectedList.TryGetValue(player.Index, out var value) && value)
            hook.GetParam<CTakeDamageInfo>(1).Damage = 0;
        
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        _protectedList.Remove(@event.Userid.Index);
        _spawnTimings.Remove(@event.Userid.Index);
        
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerConnectFull(EventPlayerDisconnect @event, GameEventInfo _)
    {
        _protectedList[@event.Userid.Index] = false;
        _spawnTimings[@event.Userid.Index] = DateTime.Now;
        
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
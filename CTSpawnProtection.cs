using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace CTSpawnProtection;

public class CTSpawnProtection : BasePlugin, IPluginConfig<CTSpawnProtectionConfig>
{
    public override string ModuleName => "[CT] Spawn protection.";
    public override string ModuleAuthor => "livevilog";
    public override string ModuleVersion => "0.3.0";

    public CTSpawnProtectionConfig Config { get; set; } = new ();
    
    private readonly Dictionary<CCSPlayerController, bool> _protectedList = new ();
    private readonly Dictionary<CCSPlayerController, DateTime> _spawnTimings = new ();
    
    private Timer? _clearProtectionTimer;

    public override void Load(bool hotReload)
    {
        if (hotReload)
        {
            InitializeLists();
        }
        
        _clearProtectionTimer = new Timer(.5f, ClearProtectionTimer, TimerFlags.REPEAT);
        Timers.Add(_clearProtectionTimer);
    }

    private void InitializeLists()
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            _protectedList[controller] = false;
            _spawnTimings[controller] = DateTime.Now;
        });
    }

    public override void Unload(bool hotReload)
    {
        _clearProtectionTimer!.Kill();
        Timers.Remove(_clearProtectionTimer);
        _clearProtectionTimer = null;
        
        _spawnTimings.Clear();
        _protectedList.Clear();
    }

    private void ClearProtectionTimer()
    {
        Utilities.GetPlayers().ForEach(controller =>
        {
            if (!_spawnTimings.TryGetValue(controller, out var time)) 
                return;
            
            if ((DateTime.Now - time).Seconds > Config.ProtectionDuration && _protectedList[controller])
            {
                _protectedList[controller] = false;
                controller.PrintToChat("You are no longer spawn protected.");
            }
        });
    }

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

    public void OnConfigParsed(CTSpawnProtectionConfig config)
    {
        this.Config = config;
    }
}
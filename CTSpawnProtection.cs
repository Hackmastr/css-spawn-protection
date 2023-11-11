using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace CTSpawnProtection;

public class CTSpawnProtection : BasePlugin
{
    public override string ModuleName => "[CT] Spawn protection.";
    public override string ModuleAuthor => "livevilog";
    public override string ModuleVersion => "0.2.0";

    private const int PreventiveHealth = 100; // Cuanta vida necesitamos para mantener el jugador vivo
    private const int ProtectionDuration = 15 + 1; // Duracion protection spawn en segundos + 1
    
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
            
            if ((DateTime.Now - time).Seconds > ProtectionDuration && _protectedList[controller])
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
        
        @event.Userid.PlayerPawn.Value.Health = PreventiveHealth;
        
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
}
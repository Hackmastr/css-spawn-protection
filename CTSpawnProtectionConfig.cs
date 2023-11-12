using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace CTSpawnProtection;

public class CTSpawnProtectionConfig : BasePluginConfig
{
    public override int Version { get; set; } = 1;

    [JsonPropertyName("PreventiveHealth")]
    public int PreventiveHealth { get; set; } = 100;
    
    [JsonPropertyName("ProtectionDuration")]
    public int ProtectionDuration { get; set; } = 15;
}


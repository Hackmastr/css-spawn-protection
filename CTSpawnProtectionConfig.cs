using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace CTSpawnProtection;

public class CTSpawnProtectionConfig : BasePluginConfig
{
    public override int Version { get; set; } = 3;

    [JsonPropertyName("PreventiveHealth")]
    public int PreventiveHealth { get; set; } = 100;
    
    [JsonPropertyName("ProtectionDuration")]
    public int ProtectionDuration { get; set; } = 15;

    [JsonPropertyName("ProtectionEndMessage")]
    public string ProtectionEndMessage { get; set; } = 
        "\v[CTSpawnProtection]\u0001 Your spawn protection has ended.";

    [JsonPropertyName("EnableRoundEndProtection")]
    public bool EnableRoundEndProtection { get; set; } = false;
}


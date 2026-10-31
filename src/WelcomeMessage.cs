using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Logging;

namespace WelcomeMessage;

public class WelcomeMessageConfig : BasePluginConfig
{
    // Relative paths are resolved from the plugin folder
    [JsonPropertyName("GeoIpDatabase")] public string GeoIpDatabase { get; set; } = "GeoLite2-City.mmdb";

    // Language used for country/city names from the GeoIP database (en, de, es, fr, ja, pt-BR, ru, zh-CN)
    [JsonPropertyName("GeoIpLanguage")] public string GeoIpLanguage { get; set; } = "en";

    // Private "Welcome" message to the player who joined
    [JsonPropertyName("ShowWelcome")] public bool ShowWelcome { get; set; } = true;

    // "JoinAnnounce" message to everyone on the server
    [JsonPropertyName("ShowJoinAnnounce")] public bool ShowJoinAnnounce { get; set; } = true;

    // Seconds to wait after the player is fully connected, so the message isn't lost while the client loads
    [JsonPropertyName("Delay")] public float Delay { get; set; } = 2.0f;
}

public class WelcomeMessage : BasePlugin, IPluginConfig<WelcomeMessageConfig>
{
    public override string ModuleName => "WelcomeMessage";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Hardix";
    public override string ModuleDescription => "Welcome message with country and city (GeoIP) on connect";

    public WelcomeMessageConfig Config { get; set; } = new();

    private DatabaseReader? _geoIp;
    private bool _hasCity;

    // {tag} -> chat color code, built from every public static member of ChatColors ({green}, {red}, {lime}, ...)
    private static readonly Dictionary<string, string> ColorTags = typeof(ChatColors)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (f.Name, Value: f.GetValue(null)))
        .Concat(typeof(ChatColors)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(p => (p.Name, Value: p.GetValue(null))))
        .Where(c => c.Value is char or string)
        .GroupBy(c => c.Name.ToLowerInvariant())
        .ToDictionary(g => $"{{{g.Key}}}", g => g.First().Value!.ToString()!, StringComparer.OrdinalIgnoreCase);

    public void OnConfigParsed(WelcomeMessageConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        // Configured file first, then whichever GeoLite2 database is in the plugin folder
        string? path = new[] { Config.GeoIpDatabase, "GeoLite2-City.mmdb", "GeoLite2-Country.mmdb" }
            .Select(p => Path.IsPathRooted(p) ? p : Path.Combine(ModuleDirectory, p))
            .FirstOrDefault(File.Exists);

        if (path != null)
        {
            _geoIp = new DatabaseReader(path, new[] { Config.GeoIpLanguage, "en" });
            _hasCity = _geoIp.Metadata.DatabaseType.Contains("City", StringComparison.OrdinalIgnoreCase);
            Logger.LogInformation("Loaded GeoIP database {Path} ({Type})", path, _geoIp.Metadata.DatabaseType);
        }
        else
        {
            Logger.LogWarning("GeoIP database not found in {Dir}, country and city will show as unknown", ModuleDirectory);
        }

        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    }

    public override void Unload(bool hotReload)
    {
        _geoIp?.Dispose();
        _geoIp = null;
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return HookResult.Continue;

        var (country, countryCode, city) = Lookup(player.IpAddress);
        string name = player.PlayerName;
        int slot = player.Slot;

        AddTimer(Config.Delay, () =>
        {
            var target = Utilities.GetPlayerFromSlot(slot);
            if (target == null || !target.IsValid)
                return;

            var placeholders = new Dictionary<string, string>
            {
                ["{player}"] = name,
                ["{country}"] = country ?? "",
                ["{countrycode}"] = countryCode ?? "",
                ["{city}"] = city ?? "",
            };

            if (Config.ShowWelcome)
                target.PrintToChat(FormatMessage(target, "Welcome", placeholders));

            if (Config.ShowJoinAnnounce)
            {
                foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
                    p.PrintToChat(FormatMessage(p, "JoinAnnounce", placeholders));
            }
        });

        return HookResult.Continue;
    }

    private (string? Country, string? CountryCode, string? City) Lookup(string? ipAddress)
    {
        if (_geoIp == null || string.IsNullOrEmpty(ipAddress))
            return (null, null, null);

        // IpAddress is "ip:port"
        string ip = ipAddress.Split(':')[0];
        if (!IPAddress.TryParse(ip, out var address))
            return (null, null, null);

        // A Country database has no city, so {city} falls back to "Unknown"
        if (_hasCity)
        {
            return _geoIp.TryCity(address, out var city) && city != null
                ? (city.Country.Name, city.Country.IsoCode, city.City.Name)
                : (null, null, null);
        }

        return _geoIp.TryCountry(address, out var country) && country != null
            ? (country.Country.Name, country.Country.IsoCode, null)
            : (null, null, null);
    }

    private string FormatMessage(CCSPlayerController player, string key, Dictionary<string, string> placeholders)
    {
        string prefix = Localizer.ForPlayer(player, "Prefix");
        string message = Localizer.ForPlayer(player, key);
        string full = string.IsNullOrWhiteSpace(prefix) ? message : $"{prefix} {message}";

        // Colors first, so a player name containing "{red}" is not turned into a color
        full = ReplaceColorTags(full);

        string unknown = Localizer.ForPlayer(player, "Unknown");
        foreach (var (tag, value) in placeholders)
            full = full.Replace(tag, string.IsNullOrEmpty(value) ? unknown : value, StringComparison.OrdinalIgnoreCase);

        // Leading space is required, otherwise CS2 ignores a color code at the very start of the message.
        return $" {full}";
    }

    private static string ReplaceColorTags(string input)
    {
        foreach (var (tag, color) in ColorTags)
            input = input.Replace(tag, color, StringComparison.OrdinalIgnoreCase);

        return input;
    }
}

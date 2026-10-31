# CS2 WelcomeMessage

A CounterStrikeSharp plugin that greets players when they join, showing their country and city using MaxMind GeoIP.

```
[Welcome] Welcome to the server, Hardix!
[Welcome] Hardix connected from Berlin, Germany
```

## Installation

1. Build: `dotnet build src -c Release`
2. Copy the contents of `src/bin/Release/net10.0/` to
   `addons/counterstrikesharp/plugins/WelcomeMessage/` (keep the `lang` folder and the `MaxMind.*.dll` files next to `WelcomeMessage.dll`).
3. Download **GeoLite2-City.mmdb** (free account at https://www.maxmind.com/en/geolite2/signup) and put it in
   `addons/counterstrikesharp/plugins/WelcomeMessage/`.

`GeoLite2-Country.mmdb` also works, but it has no city data, so `{city}` shows as `Unknown`. If the configured file
isn't found, the plugin uses whichever of `GeoLite2-City.mmdb` or `GeoLite2-Country.mmdb` is in the plugin folder.

If the database is missing, the plugin still runs and country and city show as `Unknown`. Players on LAN or private IPs also show as `Unknown`.

## Config

Created on first load at `addons/counterstrikesharp/configs/plugins/WelcomeMessage/WelcomeMessage.json`:

```json
{
  "GeoIpDatabase": "GeoLite2-City.mmdb",
  "GeoIpLanguage": "en",
  "ShowWelcome": true,
  "ShowJoinAnnounce": true,
  "Delay": 2.0
}
```

| Option | Description |
|--------|-------------|
| `GeoIpDatabase` | Path to the `.mmdb` file. Relative paths start from the plugin folder. |
| `GeoIpLanguage` | Language for country and city names: `en` `de` `es` `fr` `ja` `pt-BR` `ru` `zh-CN`. Falls back to `en`. |
| `ShowWelcome` | Sends the `Welcome` message privately to the player who joined. |
| `ShowJoinAnnounce` | Sends the `JoinAnnounce` message to everyone. |
| `Delay` | Seconds to wait after the player has fully connected before sending. |

## Messages

Edit `lang/en.json` (or add another language file such as `lang/de.json`, which is picked per player from their language):

```json
{
  "Prefix": "{green}[Welcome]{default}",
  "Welcome": "{default}Welcome to the server, {lime}{player}{default}!",
  "JoinAnnounce": "{lime}{player} {default}connected from {gold}{city}{default}, {gold}{country}",
  "Unknown": "Unknown"
}
```

`Prefix` is added in front of every message automatically. Leave it as `""` to disable it.

### Placeholders

| Placeholder | Example |
|-------------|---------|
| `{player}` | `Hardix` |
| `{country}` | `Germany` |
| `{countrycode}` | `DE` |
| `{city}` | `Berlin` |

When a value isn't found, the `Unknown` text is used instead.

### Colors

`{default}` `{white}` `{darkred}` `{green}` `{lightyellow}` `{lightblue}` `{olive}` `{lime}` `{red}`
`{lightpurple}` `{purple}` `{grey}` `{yellow}` `{gold}` `{silver}` `{blue}` `{darkblue}` `{bluegrey}`
`{magenta}` `{lightred}` `{orange}`

GeoLite2 data is created by MaxMind, available from https://www.maxmind.com.

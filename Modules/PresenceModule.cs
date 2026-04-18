using NetCord;
using NetCord.Gateway;

namespace OriginsBot.Modules
{
    public static class PresenceModule
    {
        private static HashSet<ulong> _guildIds = [];

        public static void Init(GatewayClient client)
        {
            client.Ready += async ev =>
            {
                _guildIds = [.. ev.GuildIds];
                await UpdatePresenceAsync(client, _guildIds.Count);
            };

            client.GuildCreate += async ev =>
            {
                if (ev.GuildId == 0ul)
                    return;

                _guildIds.Add(ev.GuildId);
                await UpdatePresenceAsync(client, _guildIds.Count);
            };
            
            client.GuildDelete += async ev =>
            {
                if (ev.GuildId == 0ul)
                    return;

                _guildIds.Remove(ev.GuildId);
                await UpdatePresenceAsync(client, _guildIds.Count);
            };
        }

        private static ValueTask UpdatePresenceAsync(GatewayClient client, int guildCount)
        {
            return client.UpdatePresenceAsync(new(UserStatusType.Idle)
            {
                Activities = [new($"🍞 in {guildCount} guilds", UserActivityType.Listening)]
            });
        }
    }
}
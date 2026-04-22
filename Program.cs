using NetCord;
using NetCord.Gateway;
using NetCord.Logging;
using OriginsBot.Modules;
using OriginsBot.Services;

AppDomain.CurrentDomain.UnhandledException += async (sender, e) =>
{
    Console.WriteLine($"FATAL Error! Unhandled Exception:\n{e.ExceptionObject}");
    while (true)
        Console.ReadLine();
};

if (!OriginDataService.TryReload(out _))
    await Task.Delay(-1);

PermissionService.Reload();


GatewayClient client = new(new BotToken(File.ReadAllText("token.txt")), new()
{
    Intents = GatewayIntents.Guilds,
    Logger = new ConsoleLogger(),
});

PresenceModule.Init(client);
await new CommandModule(client).RegisterCommandsAsync();

await client.StartAsync();
await Task.Delay(-1);
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using OriginsBot.Commands;
using OriginsBot.Interactions;

namespace OriginsBot.Modules
{
    public sealed class CommandModule
    {
        private const string GuildFileName = "guild.txt";
        private static readonly string GuildFile = Path.Combine(Directory.GetCurrentDirectory(), GuildFileName);

        private readonly GatewayClient _client;

        private readonly ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext> _globalCommandService;
        private HashSet<ulong> _globalCommandIds = [];

        private readonly ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext> _guildCommandService;
        private HashSet<ulong> _guildCommandIds = [];

        private readonly ComponentInteractionService<ButtonInteractionContext> _buttonInteractionService;
        private readonly ComponentInteractionService<ModalInteractionContext> _modalInteractionService;


        public CommandModule(GatewayClient client)
        {
            _client = client;

            _globalCommandService = new();

            _guildCommandService = new();
            _guildCommandService.AddModule<GlobalCommands>();
            _guildCommandService.AddModule<GuildCommands>();

            _buttonInteractionService = new();
            _buttonInteractionService.AddModule<CreatorsPagination>();
            _buttonInteractionService.AddModule<PacksPagination>();
            _buttonInteractionService.AddModule<OriginsPagination>();

            _modalInteractionService = new();
            _modalInteractionService.AddModule<ReorderModal>();


            client.InteractionCreate += async interaction =>
            {
                switch (interaction)
                {
                    case ApplicationCommandInteraction commandInteraction:
                        {
                            ApplicationCommandContext context = new(commandInteraction, client);
                            IExecutionResult? result;

                            CommandUsed(context);

                            if (_globalCommandIds.Contains(commandInteraction.Data.Id))
                                result = await _globalCommandService.ExecuteAsync(context);
                            else if (_guildCommandIds.Contains(commandInteraction.Data.Id))
                                result = await _guildCommandService.ExecuteAsync(context);
                            else
                                return;

                            if (result is not IFailResult failResult)
                                return;

                            try
                            {
                                await interaction.SendResponseAsync(InteractionCallback.Message(failResult.Message));
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(ex);
                            }

                            return;
                        }
                    case AutocompleteInteraction autocompleteInteraction:
                        {
                            AutocompleteInteractionContext context = new(autocompleteInteraction, client);
                            IExecutionResult? result;

                            if (_globalCommandIds.Contains(autocompleteInteraction.Data.Id))
                                result = await _globalCommandService.ExecuteAutocompleteAsync(context);
                            else if (_guildCommandIds.Contains(autocompleteInteraction.Data.Id))
                                result = await _guildCommandService.ExecuteAutocompleteAsync(context);
                            else
                                return;

                            if (result is not IFailResult failResult)
                                return;

                            Console.Error.WriteLine($"Autocomplete failed: {failResult.Message}");
                            return;
                        }
                    case ButtonInteraction buttonInteraction:
                        {
                            ButtonInteractionContext context = new(buttonInteraction, client);

                            var result = await _buttonInteractionService.ExecuteAsync(context);

                            if (result is not IFailResult failResult)
                                return;

                            Console.Error.WriteLine($"Button interact failed: {failResult.Message}");

                            return;
                        }
                    case ModalInteraction modalInteraction:
                        {
                            ModalInteractionContext context = new(modalInteraction, client);

                            var result = await _modalInteractionService.ExecuteAsync(context);

                            if (result is not IFailResult failResult)
                                return;

                            Console.Error.WriteLine($"Modal interact failed: {failResult.Message}");

                            return;
                        }
                }
            };
        }

        public async ValueTask RegisterCommandsAsync()
        {
            ulong guildId = ulong.Parse(File.ReadAllText(GuildFile));

            var globalCommandTask = _globalCommandService.RegisterCommandsAsync(_client.Rest, _client.Id);
            var guildCommandTask = _guildCommandService.RegisterCommandsAsync(_client.Rest, _client.Id, guildId);

            await Task.WhenAll(
                globalCommandTask,
                guildCommandTask
            );
            
            _globalCommandIds = [.. globalCommandTask.Result.Select(cmd => cmd.Id)];
            _guildCommandIds = [.. guildCommandTask.Result.Select(cmd => cmd.Id)];
        }


        public static void CommandUsed(ApplicationCommandContext context)
        {
            if (context.Interaction is SlashCommandInteraction interaction)
            {
                string userName = (context.User is GuildUser guildUser ? guildUser.Nickname ?? context.User.GlobalName : context.User.GlobalName) ?? context.User.Id.ToString();
                string commandName = context.Interaction.Data.Name;
                string channelName = context.Channel is INamedChannel namedChannel ? namedChannel.Name : context.Channel.Id.ToString();
                string args = string.Join(',', interaction.Data.Options.Select(opt => opt.Value ?? "null"));

                if (string.IsNullOrEmpty(args))
                    Console.WriteLine($"{DateTime.Now,-11:h:mm:ss tt} '{userName}' used '{commandName}' in '{channelName}'.");
                else
                    Console.WriteLine($"{DateTime.Now,-11:h:mm:ss tt} '{userName}' used '{commandName}' in '{channelName}' with args: {args}");
            }
        }
    }
}
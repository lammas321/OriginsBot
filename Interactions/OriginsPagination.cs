using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using OriginsBot.Services;

namespace OriginsBot.Interactions
{
    public sealed class OriginsPagination : ComponentInteractionModule<ButtonInteractionContext>
    {
        public const int PerPage = 20;

        public static int Count
            => OriginDataService.Data.OrderedOriginIds.Count;

        public static int MaxPage
            => Math.Max(0, (Count - 1) / PerPage);


        public static async Task SendMessage(ApplicationCommandContext context)
        {
            await context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Embeds = [GetEmbed(0)],
                Components = [GetButtons(0)],
            }));
        }

        private static InteractionCallbackProperties<MessageOptions> ModifyMessage(uint unsignedPage)
        {
            int page = (int)Math.Min(MaxPage, unsignedPage);

            return InteractionCallback.ModifyMessage(message =>
            {
                message.Embeds = [GetEmbed(page)];
                message.Components = [GetButtons(page)];
            });
        }


        private static EmbedProperties GetEmbed(int page)
            => new()
            {
                Title = $"Origins ({Count})",
                Description = $"- {string.Join("\n- ", OriginDataService.Data.OrderedOriginIds.Skip(PerPage * page).Take(PerPage).Select(originId => OriginDataService.Data.UniqueOriginNames[originId]))}",
                Color = new Color(0, 150, 255),
                Footer = new()
                {
                    Text = $"Page: {page + 1}/{MaxPage + 1}",
                },
            };

        private static ActionRowProperties GetButtons(int page)
#pragma warning disable IDE0028 // Simplify collection initialization
            => new([
                new ButtonProperties("origins_start", "<<", ButtonStyle.Primary) { Disabled = page == 0 },
                new ButtonProperties($"origins_back:{page}", "<", ButtonStyle.Primary) { Disabled = page == 0},
                new ButtonProperties($"origins_next:{page}", ">", ButtonStyle.Primary) { Disabled = page == MaxPage },
                new ButtonProperties("origins_end", ">>", ButtonStyle.Primary) { Disabled = page == MaxPage },
            ]);
#pragma warning restore IDE0028 // Simplify collection initialization


        [ComponentInteraction("origins_start")]
        public static InteractionCallbackProperties<MessageOptions> Start()
            => ModifyMessage(uint.MinValue);

        [ComponentInteraction("origins_back")]
        public static InteractionCallbackProperties<MessageOptions> Back(uint page)
            => ModifyMessage(page - 1);
        
        [ComponentInteraction("origins_next")]
        public static InteractionCallbackProperties<MessageOptions> Next(uint page)
            => ModifyMessage(page + 1);

        [ComponentInteraction("origins_end")]
        public static InteractionCallbackProperties<MessageOptions> End()
            => ModifyMessage(uint.MaxValue);
    }
}
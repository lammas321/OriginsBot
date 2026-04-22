using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using OriginsBot.Extensions;
using OriginsBot.Interactions;
using OriginsBot.Services;

namespace OriginsBot.Commands
{
    public sealed class GlobalCommands : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SlashCommand("info", "See information about the bot.")]
        public async Task Info()
        {
            EmbedProperties embed = new()
            {
                Title = "Information",
                Description = $"Created by <@394347954900697108>\nOriginally created by <@855948446540496896>\nOrigins BE Invite: https://discord.gg/Mypr3MqpcP",
                Color = new Color(0, 150, 255),
            };

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Embeds = [embed],
            }));
        }


        [SlashCommand("creators", "See a list of all registered origin creators.")]
        public async Task Creators()
        {
            await CreatorsPagination.SendMessage(Context);
        }


        [SlashCommand("packs", "See a list of all registered origin packs.")]
        public async Task Packs()
        {
            await PacksPagination.SendMessage(Context);
        }


        [SlashCommand("origins", "See a list of all registered origins.")]
        public async Task Origins()
        {
            await OriginsPagination.SendMessage(Context);
        }
        

        [SlashCommand("creator", "See information about a registered origin creator.")]
        public async Task Creator(
            [SlashCommandParameter(Name = "creator", AutocompleteProviderType = typeof(CreatorProvider))] string creatorString)
        {
            if (!OriginDataService.Data.TryParseCreatorId(creatorString, out ulong creatorId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{creatorString}' is not a registered origin creator.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (!OriginDataService.Data.CreatorIds.TryGetValue(creatorId, out CreatorData? creator))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"The data for '{creatorId}' is invalid:```\n{(OriginDataService.Data.InvalidCreatorIds.TryGetValue(creatorId, out Exception? ex) ? ex.Message : "Failed to retrieve exception.")}```",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            EmbedProperties embed = new()
            {
                Title = creator.Name,
                Description = creator.Description,
                Color = creator.Color,
                Footer = new()
                {
                    Text = $"id: {creator.Id}",
                },
            };

            embed.AddFields([
                .. creator.Info.Select(info => new EmbedFieldProperties
                {
                    Name = info.Item1,
                    Value = info.Item2,
                    Inline = false,
                }),
                new()
                {
                    Name = "Packs",
                    Value = string.Join(", ", creator.OrderedPacks.Select(pack => pack.Name)),
                    Inline = false,
                },
            ]);

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Embeds = embed.MakeCompliant(),
            }));
        }


        [SlashCommand("pack", "See information about a registered origin pack.")]
        public async Task Pack(
            [SlashCommandParameter(Name = "pack", AutocompleteProviderType = typeof(PackProvider))] string packString)
        {
            if (!OriginDataService.Data.TryParsePackId(packString, out PackId packId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{packString}' is not a registered origin pack.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (!OriginDataService.Data.PackIds.TryGetValue(packId, out PackData? pack))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"The data for '{packId.Value}' is invalid:```\n{(OriginDataService.Data.InvalidPackIds.TryGetValue(packId, out Exception? ex) ? ex.Message : "Failed to retrieve exception.")}```",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            EmbedProperties embed = new()
            {
                Title = $"{pack.Name} v{pack.Version}",
                Description = pack.Description,
                Color = pack.Color,
                Footer = new()
                {
                    Text = $"id: {pack.Id.Value}",
                },
            };

            embed.AddFields(
                new EmbedFieldProperties
                {
                    Name = pack.CreatorIds.Count == 1 ? "Creator" : "Creators",
                    Value = string.Join(", ", pack.OrderedCreatorIds.Select(creatorId => OriginDataService.Data.UniqueCreatorNames.TryGetValue(creatorId, out string? uniqueName) ? uniqueName : $"<@{creatorId}>")),
                    Inline = false,
                }
            );

            if (pack.Requirements.Count != 0)
                embed.AddFields(new EmbedFieldProperties
                {
                    Name = "Requirements",
                    Value = string.Join(", ", pack.Requirements),
                    Inline = false,
                });

            embed.AddFields([
                .. pack.Info.Select(info => new EmbedFieldProperties
                {
                    Name = info.Item1,
                    Value = info.Item2,
                    Inline = false,
                }),
                new()
                {
                    Name = "Origins",
                    Value = string.Join(", ", pack.OrderedOrigins.Select(origin => origin.Name)),
                    Inline = false,
                },
            ]);

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Embeds = embed.MakeCompliant(),
            }));
        }


        [SlashCommand("origin", "See information about a registered origin.")]
        public async Task Origin(
            [SlashCommandParameter(Name = "origin", AutocompleteProviderType = typeof(OriginProvider))] string originString)
        {
            if (!OriginDataService.Data.TryParseOriginId(originString, out OriginId originId) ||
                !OriginDataService.Data.OriginIds.TryGetValue(originId, out OriginData? origin))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{originString}' is not a registered origin.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            
            EmbedProperties embed = new()
            {
                Title = origin.Name,
                Description = $"{OriginDataService.Impacts[origin.Impact]}\n\n{origin.Description.Replace(" * ", "\\*")}",
                Color = origin.Color,
                Footer = new EmbedFooterProperties
                {
                    Text = $"id: {origin.Id.GetFullId()}",
                },
            };

            embed.AddFields([
                .. origin.Powers.Select(power => new EmbedFieldProperties
                {
                    Name = power.Name,
                    Value = power.Description,
                    Inline = false,
                })
            ]);

            if (origin.Credit != null)
                embed.AddFields(new EmbedFieldProperties
                {
                    Name = "Credit",
                    Value = origin.Credit,
                    Inline = false,
                });
            
            InteractionMessageProperties response = new()
            {
                Embeds = embed.MakeCompliant(),
            };


            string iconPath = Path.Combine(OriginDataService.PacksDirectory, origin.Id.PackId.Value, OriginDataService.PackIconsDirectoryName, $"{origin.Id.Value}.png");
            if (File.Exists(iconPath))
            {
                using FileStream iconStream = File.OpenRead(iconPath);

                response.Attachments = [new("icon.png", iconStream)];

                foreach (EmbedProperties e in response.Embeds)
                    e.Thumbnail = new EmbedThumbnailProperties("attachment://icon.png");

                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(response));
                return;
            }

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(response));
        }
    }
}
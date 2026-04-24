using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using OriginsBot.JsonModels;
using OriginsBot.Services;
using System.IO.Compression;
using System.Text.Json;

namespace OriginsBot.Commands
{
    public sealed class GuildCommands : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SlashCommand("get_creator", "Origin Creators Only: Retrieve a registered origin creator's creator.json file.")]
        public async Task GetCreator(
            [SlashCommandParameter(Name = "creator", AutocompleteProviderType = typeof(AccessibleCreatorProvider))] string creatorString)
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(Context);
            if (userPermissions < UserPermissions.Creator)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to use this.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (!OriginDataService.Data.TryParseCreatorId(creatorString, out ulong creatorId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{creatorString}' is not a registered origin creator.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (creatorId != Context.User.Id && userPermissions < UserPermissions.Admin)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to get this creator.json.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (!OriginDataService.Data.CreatorIds.TryGetValue(creatorId, out CreatorData? creator))
            {
                using FileStream creatorJsonFile = File.OpenRead(Path.Combine(OriginDataService.CreatorsDirectory, creatorId.ToString(), OriginDataService.CreatorJsonFileName));

                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"The data for '{creatorId}' is invalid:```\n{(OriginDataService.Data.InvalidCreatorIds.TryGetValue(creatorId, out Exception? ex) ? ex.Message : "Failed to retrieve exception.")}```",
                    Attachments = [new("creator.json", creatorJsonFile)],
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            JsonCreator jsonCreator = new()
            {
                Name = creator.Name,
                Description = creator.Description,
                Info = [
                    .. creator.Info.Select(info => new JsonInfo
                    {
                        Name = info.Item1,
                        Value = info.Item2
                    })
                ],
                Color = [creator.Color.Red, creator.Color.Green, creator.Color.Blue],
            };

            MemoryStream stream = new();
            JsonSerializer.Serialize(stream, jsonCreator, OriginDataService.JsonOptions);
            stream.Position = 0;

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Attachments = [new("creator.json", stream)],
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("get_pack", "Origin Creators Only: Retrieve a registered origin pack's pack.json file.")]
        public async Task GetPack(
            [SlashCommandParameter(Name = "pack", AutocompleteProviderType = typeof(AccessiblePackProvider))] string packString)
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(Context);
            if (userPermissions < UserPermissions.Creator)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to use this.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

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
                InteractionMessageProperties message = new()
                {
                    Content = $"The data for '{packId.Value}' is invalid:```\n{(OriginDataService.Data.InvalidPackIds.TryGetValue(packId, out Exception? ex) ? ex.Message : "Failed to retrieve exception.")}```",
                    Flags = MessageFlags.Ephemeral,
                };

                if (userPermissions < UserPermissions.Admin)
                {
                    await Context.Interaction.SendResponseAsync(InteractionCallback.Message(message));
                    return;
                }

                using FileStream packJsonFile = File.OpenRead(Path.Combine(OriginDataService.PacksDirectory, packId.Value, OriginDataService.PackJsonFileName));
                using FileStream langFile = File.OpenRead(Path.Combine(OriginDataService.PacksDirectory, packId.Value, OriginDataService.PackLangFileName));
                message.Attachments = [
                    new(OriginDataService.PackJsonFileName, packJsonFile),
                    new(OriginDataService.PackLangFileName, langFile),
                ];

                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(message));
                return;
            }

            if (!pack.CreatorIds.Contains(Context.User.Id) && userPermissions < UserPermissions.Admin)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to get this pack.json.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            JsonPack jsonPack = new()
            {
                Name = pack.Name,
                Description = pack.Description,
                CreatorIds = [.. pack.CreatorIds],
                Version = pack.Version,
                Requirements = [.. pack.Requirements],
                Info = [.. pack.Info.Select(info => new JsonInfo
                {
                    Name = info.Item1,
                    Value = info.Item2
                })],
                Color = [pack.Color.Red, pack.Color.Green, pack.Color.Blue],

                Origins = [.. pack.OriginIds
                .Select(originId => new JsonOrigin
                {
                    Id = originId.Value,
                    Impact = OriginDataService.Data.OriginIds[originId].Impact,
                    PowerIds = [.. OriginDataService.Data.OriginIds[originId].Powers.Select(power => power.Id)],
                    Color = [OriginDataService.Data.OriginIds[originId].Color.Red, OriginDataService.Data.OriginIds[originId].Color.Green, OriginDataService.Data.OriginIds[originId].Color.Blue],
                })],
            };

            MemoryStream stream = new();
            JsonSerializer.Serialize(stream, jsonPack, OriginDataService.JsonOptions);
            stream.Position = 0;

            using FileStream langStream = File.OpenRead(Path.Combine(OriginDataService.PacksDirectory, packId.Value, OriginDataService.PackLangFileName));

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Attachments = [
                    new(OriginDataService.PackJsonFileName, stream),
                    new(OriginDataService.PackLangFileName, langStream),
                ],
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("creator_template", "Origin Creators Only: Retrieve a template creator.json file.")]
        public async Task CreatorTemplate()
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(Context);
            if (userPermissions < UserPermissions.Creator)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to use this.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            JsonCreator jsonCreator = new()
            {
                Name = "Your Display Name",
                Description = "Describe yourself here.",
                Info = [
                    new() { Name = "Key", Value = "Some value" },
                    new() { Name = "Key 2", Value = "Some second value" },
                    new() { Name = "Key N", Value = "Some N-th value" },
                ],
                Color = [255, 255, 255],
            };

            MemoryStream stream = new();
            JsonSerializer.Serialize(stream, jsonCreator, OriginDataService.JsonOptions);
            stream.Position = 0;

            EmbedProperties embed = new()
            {
                Title = "Creator Template",
                Description = "Attached is a template for a creator.json file.",
                Fields = [
                    new EmbedFieldProperties
                    {
                        Name = "Name",
                        Value = "The name that will be displayed to others through the bot.",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Description",
                        Value = "The description others will see when looking at your creator profile.",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Info",
                        Value = "Tidbits of information about you, could be used to advertise your socials.",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Color",
                        Value = "The RGB color of your creator profile.",
                        Inline = false,
                    },
                ],
                Color = new Color(0, 150, 255),
            };

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Embeds = [embed],
                Attachments = [new("creator.json", stream)],
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("pack_template", "Origin Creators Only: Retrieve a template pack.json file.")]
        public async Task PackTemplate()
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(Context);
            if (userPermissions < UserPermissions.Creator)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to use this.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            JsonPack jsonPack = new()
            {
                Name = "Pack's Display Name",
                Description = "Describe your pack here.",
                CreatorIds = [0ul, 12345ul],
                Version = "2.1.9",
                Requirements = ["Origins v1.4.14"],
                Info = [
                    new() { Name = "Key", Value = "Some value" },
                    new() { Name = "Key 2", Value = "Some second value" },
                    new() { Name = "Key N", Value = "Some N-th value" },
                ],
                Color = [255, 255, 255],
                Origins = [
                    new JsonOrigin
                    {
                        Id = "originId1",
                        Impact = 3,
                        PowerIds = ["powerId1", "powerId2", "powerId3"],
                        Color = [255, 255, 255],
                    },
                    new JsonOrigin
                    {
                        Id = "originId2",
                        Impact = 1,
                        PowerIds = ["powerId2"],
                        Color = [255, 255, 255],
                    },
                    new JsonOrigin
                    {
                        Id = "originId3",
                        Impact = 2,
                        PowerIds = ["powerId2", "powerId3"],
                        Color = [255, 255, 255],
                    },
                ],
            };

            MemoryStream stream = new();
            JsonSerializer.Serialize(stream, jsonPack, OriginDataService.JsonOptions);
            stream.Position = 0;

            EmbedProperties embed = new()
            {
                Title = "Pack Template",
                Description = "Attached is a template for a pack.json file.",
                Fields = [
                    new EmbedFieldProperties
                    {
                        Name = "Name",
                        Value = "The name that will be displayed to others through the bot.",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Description",
                        Value = "The description others will see when looking at the pack's page.",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Creator Ids",
                        Value = "The Discord User Ids of creators that may modify this pack, in order of authority greatest to least.\n**ONLY** add secure alts or other creators you trust.",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Version",
                        Value = "The version of the pack.",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Requirements",
                        Value = "Anything that's required for this pack to work normally.",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Info",
                        Value = "Tidbits of information regarding the pack.",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Color",
                        Value = "The RGB color of the pack's page.",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Origins",
                        Value = "The definitions for the origins in the pack.",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Origin Id",
                        Value = "The id of the origin.\nCheck the keys of your en_US.lang for the right origin ids.\n> origins:origin.{packId}.{originId}.name",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Origin Impact",
                        Value = "The numerical impact level of the origin:\n> 0=None, 1=Low, 2=Medium, 3=High, 4=Major",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Origin Power Ids",
                        Value = "The ids of the powers the origin has.\nCheck the keys of your en_US.lang for the right power ids.\n> origins:power.{powerId}.name",
                        Inline = false,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "Origin Color",
                        Value = "The RGB color of the origin's page.",
                        Inline = false,
                    },
                ],
                Color = new Color(0, 150, 255),
            };

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Embeds = [embed],
                Attachments = [new(OriginDataService.PackJsonFileName, stream)],
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("new_creator", "Admins Only: Register a new origin creator.", DefaultGuildPermissions = Permissions.Administrator)]
        public async Task NewCreator(
            [SlashCommandParameter(Name = "creator")] GuildUser creatorUser,
            [SlashCommandParameter(Name = "creator_json")] Attachment? creatorJsonAttachment = null)
        {
            ulong creatorId = creatorUser.Id;

            if (OriginDataService.Data.CreatorIds.ContainsKey(creatorId) ||
                OriginDataService.Data.InvalidCreatorIds.ContainsKey(creatorId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{creatorId}' is already a registered origin creator.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            JsonCreator? jsonCreator = null;
            if (creatorJsonAttachment != null)
            {
                using HttpClient http = new();
                using Stream stream = await http.GetStreamAsync(creatorJsonAttachment.Url);

                jsonCreator = JsonSerializer.Deserialize<JsonCreator>(stream, OriginDataService.JsonOptions);
                if (jsonCreator == null)
                {
                    await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                    {
                        Content = "Failed to deserialize the provided creator.json.",
                        Flags = MessageFlags.Ephemeral,
                    }));
                    return;
                }
            }
            else
                jsonCreator = new()
                {
                    Name = creatorUser.Nickname ?? creatorUser.GlobalName ?? $"Unknown ({creatorId})",
                    Description = "Undefined",
                    Info = [],
                    Color = [255, 255, 255],
                };


            if (!OriginDataService.TryAddCreator(creatorId, jsonCreator, out Exception? ex))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Someone tell <@394347954900697108> there is a problem with my AI.\nSomething is fundamentally broken (could be logic or data) and most likely can only be fixed manually.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (ex != null)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"Failed to register '{creatorId}' as a new origin creator:```\n{ex.Message}```",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Content = $"Registered '{creatorId}' as a new origin creator.",
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("transfer_creator", "Admins Only: Change a registered origin creator's id, does not update any registered origin packs.", DefaultGuildPermissions = Permissions.Administrator)]
        public async Task TransferCreator(
            [SlashCommandParameter(Name = "old_creator", AutocompleteProviderType = typeof(CreatorProvider))] string oldCreatorString,
            [SlashCommandParameter(Name = "new_creator")] GuildUser newCreatorUser)
        {
            if (!OriginDataService.Data.TryParseCreatorId(oldCreatorString, out ulong oldCreatorId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{oldCreatorString}' is not a registered origin creator.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            ulong newCreatorId = newCreatorUser.Id;

            if (OriginDataService.Data.CreatorIds.ContainsKey(newCreatorId) ||
                OriginDataService.Data.InvalidCreatorIds.ContainsKey(newCreatorId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{newCreatorId}' is already a registered origin creator.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            if (!OriginDataService.TryMoveCreator(oldCreatorId, newCreatorId, out Exception? ex))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Someone tell <@394347954900697108> there is a problem with my AI.\nSomething is fundamentally broken (could be logic or data) and most likely can only be fixed manually.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (ex != null)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"Failed to change the registered origin creator id '{oldCreatorId}' into '{newCreatorId}':```\n{ex.Message}```",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Content = $"Changed the registered origin creator id '{oldCreatorId}' into '{newCreatorId}'.",
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("update_creator", "Origin Creators Only: Update a registered origin creator.")]
        public async Task UpdateCreator(
            [SlashCommandParameter(Name = "creator", AutocompleteProviderType = typeof(AccessibleCreatorProvider))] string creatorString,
            [SlashCommandParameter(Name = "creator_json")] Attachment creatorJsonAttachment)
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(Context);
            if (userPermissions < UserPermissions.Creator)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to use this.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (!OriginDataService.Data.TryParseCreatorId(creatorString, out ulong creatorId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{creatorString}' is not a registered origin creator.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (creatorId != Context.User.Id && userPermissions < UserPermissions.Admin)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to update this registered origin creator.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            using HttpClient http = new();
            using Stream stream = await http.GetStreamAsync(creatorJsonAttachment.Url);

            JsonCreator? jsonCreator = JsonSerializer.Deserialize<JsonCreator>(stream, OriginDataService.JsonOptions);
            if (jsonCreator == null)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Failed to deserialize the provided creator.json.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            if (!OriginDataService.TryUpdateCreator(creatorId, jsonCreator, out Exception? ex))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Someone tell <@394347954900697108> there is a problem with my AI.\nSomething is fundamentally broken (could be logic or data) and most likely can only be fixed manually.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (ex != null)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"Failed to update the registered origin creator '{creatorId}':```\n{ex.Message}```",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Content = $"Updated the registered origin creator '{creatorId}'.",
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("remove_creator", "Admins Only: Remove a registered origin creator.", DefaultGuildPermissions = Permissions.Administrator)]
        public async Task RemoveCreator(
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


            if (!OriginDataService.TryRemoveCreator(creatorId, out Exception? ex))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Someone tell <@394347954900697108> there is a problem with my AI.\nSomething is fundamentally broken (could be logic or data) and most likely can only be fixed manually.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (ex != null)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"Failed to remove the registered origin creator '{creatorId}':```\n{ex.Message}```",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Content = $"Removed the registered origin creator '{creatorId}'.",
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("new_pack", "Origin Creators Only: Register a new origin pack.")]
        public async Task NewPack(
            [SlashCommandParameter(Name = "pack_id_string")] string packIdString,
            [SlashCommandParameter(Name = "pack_json")] Attachment? packJsonAttachment = null,
            [SlashCommandParameter(Name = "lang")] Attachment? langAttachment = null)
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(Context);
            if (userPermissions < UserPermissions.Creator)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to use this.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (!PackId.IsValid(packIdString))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Invalid pack id, must be at most 32 characters and only contain lowercase letters, digits, and underscores.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            PackId packId = new(packIdString);

            if (OriginDataService.Data.PackIds.ContainsKey(packId) ||
                OriginDataService.Data.InvalidPackIds.ContainsKey(packId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{packId.Value}' is already a registered origin pack.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            JsonPack? jsonPack = null;
            if (packJsonAttachment != null)
            {
                using HttpClient http = new();
                using Stream stream = await http.GetStreamAsync(packJsonAttachment.Url);

                jsonPack = JsonSerializer.Deserialize<JsonPack>(stream, OriginDataService.JsonOptions);
                if (jsonPack == null)
                {
                    await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                    {
                        Content = "Failed to deserialize the provided pack.json.",
                        Flags = MessageFlags.Ephemeral,
                    }));
                    return;
                }
            }
            else
                jsonPack = new()
                {
                    Name = $"{Context.User.Id}'s Origin Pack",
                    Description = "Describe your pack here.",
                    CreatorIds = [Context.User.Id],
                    Version = "0.1.0",
                    Requirements = [],
                    Info = [
                        new() { Name = "Hey!", Value = "You gotta update this :D" },
                    ],
                    Color = [255, 255, 255],
                    Origins = [],
                };

            string? fullLang = null;
            if (langAttachment != null)
            {
                using HttpClient http = new();
                fullLang = await http.GetStringAsync(langAttachment.Url);
            }
            else
                fullLang = string.Empty;


            if (!OriginDataService.TryAddPack(packId, jsonPack, fullLang, out Exception? ex))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Someone tell <@394347954900697108> there is a problem with my AI.\nSomething is fundamentally broken (could be logic or data) and most likely can only be fixed manually.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (ex != null)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"Failed to register '{packId.Value}' as a new origin pack:```\n{ex.Message}```",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Content = $"Registered '{packId.Value}' as a new origin pack.",
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("rename_pack", "Origin Creators Only: Change a registered origin pack's id.")]
        public async Task RenamePack(
            [SlashCommandParameter(Name = "old_pack", AutocompleteProviderType = typeof(AccessiblePackProvider))] string oldPackString,
            [SlashCommandParameter(Name = "new_pack_id_string")] string newPackIdString)
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(Context);
            if (userPermissions < UserPermissions.Creator)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to use this.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }
            
            if (!OriginDataService.Data.TryParsePackId(oldPackString, out PackId oldPackId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{oldPackString}' is not a registered origin pack.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (
                (!OriginDataService.Data.PackIds.TryGetValue(oldPackId, out PackData? pack) && userPermissions < UserPermissions.Admin) ||
                (pack != null && userPermissions < UserPermissions.Admin && !pack.CreatorIds.Contains(Context.User.Id)))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to rename this registered origin pack.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (!PackId.IsValid(newPackIdString))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Invalid new pack id, must be at most 32 characters and only contain lowercase letters, digits, and underscores.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            PackId newPackId = new(newPackIdString);

            if (OriginDataService.Data.PackIds.ContainsKey(newPackId) ||
                OriginDataService.Data.InvalidPackIds.ContainsKey(newPackId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{newPackId.Value}' is already a registered origin pack.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            if (!OriginDataService.TryMovePack(oldPackId, newPackId, out Exception? ex))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Someone tell <@394347954900697108> there is a problem with my AI.\nSomething is fundamentally broken (could be logic or data) and most likely can only be fixed manually.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (ex != null)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"Failed to change the registered origin pack '{oldPackId.Value}' into {newPackId.Value}:```\n{ex.Message}```",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Content = $"Registered '{newPackId.Value}' as a new origin pack.",
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("update_pack", "Origin Creators Only: Update a registered origin pack.")]
        public async Task UpdatePack(
            [SlashCommandParameter(Name = "pack", AutocompleteProviderType = typeof(PackProvider))] string packString,
            [SlashCommandParameter(Name = "pack_json")] Attachment? packJsonAttachment = null,
            [SlashCommandParameter(Name = "lang")] Attachment? langAttachment = null)
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(Context);
            if (userPermissions < UserPermissions.Creator)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to use this.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }
            
            if (!OriginDataService.Data.TryParsePackId(packString, out PackId packId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{packString}' is not a registered origin pack.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (
                (!OriginDataService.Data.PackIds.TryGetValue(packId, out PackData? pack) && userPermissions < UserPermissions.Admin) ||
                (pack != null && userPermissions < UserPermissions.Admin && !pack.CreatorIds.Contains(Context.User.Id)))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to update this registered origin pack.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (packJsonAttachment == null && langAttachment == null)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You should provide at least a pack.json or en_US.lang file.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            JsonPack? jsonPack = null;
            if (packJsonAttachment != null)
            {
                using HttpClient http = new();
                using Stream stream = await http.GetStreamAsync(packJsonAttachment.Url);

                jsonPack = JsonSerializer.Deserialize<JsonPack>(stream, OriginDataService.JsonOptions);
                if (jsonPack == null)
                {
                    await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                    {
                        Content = "Failed to deserialize the provided pack.json.",
                        Flags = MessageFlags.Ephemeral,
                    }));
                    return;
                }

                if (userPermissions == UserPermissions.Creator)
                {
                    if (pack == null)
                    {
                        await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                        {
                            Content = "You don't have permission to update this registered origin pack.",
                            Flags = MessageFlags.Ephemeral,
                        }));
                        return;
                    }

                    ulong[] creatorIds = [.. pack.OrderedCreatorIds];
                    if (creatorIds[0] != Context.User.Id && !creatorIds.SequenceEqual(jsonPack.CreatorIds))
                    {
                        await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                        {
                            Content = "You don't have permission to update the creator ids of this registered origin pack.",
                            Flags = MessageFlags.Ephemeral,
                        }));
                        return;
                    }
                }
            }

            string? fullLang = null;
            if (langAttachment != null)
            {
                using HttpClient http = new();
                fullLang = await http.GetStringAsync(langAttachment.Url);
            }


            if (!OriginDataService.TryUpdatePack(packId, jsonPack, fullLang, out Exception? ex))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Someone tell <@394347954900697108> there is a problem with my AI.\nSomething is fundamentally broken (could be logic or data) and most likely can only be fixed manually.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (ex != null)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"Failed to update the registered origin pack '{packId.Value}':```\n{ex.Message}```",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Content = $"Updated the registered origin pack '{packId.Value}'.",
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("remove_pack", "Origin Creators Only: Remove a registered origin pack.")]
        public async Task RemovePack(
            [SlashCommandParameter(Name = "pack", AutocompleteProviderType = typeof(PackProvider))] string packString)
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(Context);
            if (userPermissions < UserPermissions.Creator)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to use this.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (!OriginDataService.Data.TryParsePackId(packString, out PackId packId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{packString}' is not a registered origin pack.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (
                (!OriginDataService.Data.PackIds.TryGetValue(packId, out PackData? pack) && userPermissions < UserPermissions.Admin) ||
                (pack != null && userPermissions < UserPermissions.Admin && !pack.CreatorIds.Contains(Context.User.Id)))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to remove this registered origin pack.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            if (!OriginDataService.TryRemovePack(packId, out Exception? ex))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Someone tell <@394347954900697108> there is a problem with my AI.\nSomething is fundamentally broken (could be logic or data) and most likely can only be fixed manually.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (ex != null)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"Failed to remove the registered origin pack '{packId.Value}':```\n{ex.Message}```",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Content = $"Removed the registered origin pack '{packId.Value}'.",
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("update_icons", "Origin Creators Only: Update a registered origin pack's icons.")]
        public async Task UpdateIcons(
            [SlashCommandParameter(Name = "pack", AutocompleteProviderType = typeof(PackProvider))] string packString,
            [SlashCommandParameter(Name = "icons_zip")] Attachment iconsZipAttachment)
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(Context);
            if (userPermissions < UserPermissions.Creator)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to use this.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (!OriginDataService.Data.TryParsePackId(packString, out PackId packId))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"'{packString}' is not a registered origin pack.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (
                (!OriginDataService.Data.PackIds.TryGetValue(packId, out PackData? pack) && userPermissions < UserPermissions.Admin) ||
                (pack != null && userPermissions < UserPermissions.Admin && !pack.CreatorIds.Contains(Context.User.Id)))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "You don't have permission to update this registered origin pack's icons.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            using HttpClient http = new();
            using Stream stream = await http.GetStreamAsync(iconsZipAttachment.Url);
            using ZipArchive archive = new(stream, ZipArchiveMode.Read);

            OriginDataService.UpdateIcons(packId, archive);

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Content = $"Updated the icons of the registered origin pack '{packId.Value}'.",
                Flags = MessageFlags.Ephemeral,
            }));
        }


        [SlashCommand("reorder", "Admins Only: Reorder creators and packs.")]
        public async Task Reorder()
        {
            string orderedCreators = string.Join('\n', File.ReadAllLines(OriginDataService.OrderedCreatorsFile));
            string orderedPacks = string.Join('\n', File.ReadAllLines(OriginDataService.OrderedPacksFile));

            await Context.Interaction.SendResponseAsync(InteractionCallback.Modal(new("reorder_modal", "Reorder Creators and Packs")
            {
                new LabelProperties(
                    "Creators:",
                    new TextInputProperties("creators", TextInputStyle.Paragraph)
                    {
                        Value = orderedCreators,
                        Placeholder = orderedCreators,
                        Required = true,
                    }
                ),
                new LabelProperties(
                    "Packs:",
                    new TextInputProperties("packs", TextInputStyle.Paragraph)
                    {
                        Value = orderedPacks,
                        Placeholder = orderedPacks,
                        Required = true,
                    }
                ),
            }));
        }


        [SlashCommand("reload_data", "Admins Only: Forces a data reload from disk.", DefaultGuildPermissions = Permissions.Administrator)]
        public async Task ReloadData()
        {
            if (!OriginDataService.TryReload(out Exception? exception))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = $"Something went seriously wrong, failed to reload data.\n- {exception!.Message}",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            if (OriginDataService.Data.InvalidCreatorIds.Count != 0 || OriginDataService.Data.InvalidPackIds.Count != 0)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Something went wrong when loading at least one origin creator or pack, but the reload concluded successfully.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Content = "Reloaded data successfully.",
                Flags = MessageFlags.Ephemeral,
            }));
        }
    }
}
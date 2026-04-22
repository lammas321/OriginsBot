using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using OriginsBot.Services;

namespace OriginsBot.Commands
{
    public sealed class CreatorProvider : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
            => ValueTask.FromResult<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(
                OriginDataService.Data.OrderedCreatorIds.Where(creatorId => Match(creatorId, option)).Select(AsChoice).Take(25));

        private static bool Match(ulong creatorId, ApplicationCommandInteractionDataOption option)
            => OriginDataService.Data.CreatorIds.TryGetValue(creatorId, out CreatorData? creator) && creator.Name.Contains(option.Value!, StringComparison.InvariantCultureIgnoreCase);

        private static ApplicationCommandOptionChoiceProperties AsChoice(ulong creatorId)
            => new(OriginDataService.Data.UniqueCreatorNames[creatorId], creatorId.ToString());
    }

    public sealed class PackProvider : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
            => ValueTask.FromResult<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(
                OriginDataService.Data.OrderedPackIds.Where(packId => Match(packId, option)).Select(AsChoice).Take(25));

        private static bool Match(PackId packId, ApplicationCommandInteractionDataOption option)
            => OriginDataService.Data.PackIds.TryGetValue(packId, out PackData? pack) && pack.Name.Contains(option.Value!, StringComparison.InvariantCultureIgnoreCase);

        private static ApplicationCommandOptionChoiceProperties AsChoice(PackId packId)
            => new(OriginDataService.Data.UniquePackNames[packId], packId.Value);
    }

    public sealed class OriginProvider : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
            => ValueTask.FromResult<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(
                OriginDataService.Data.OrderedOriginIds.Where(originId => Match(originId, option)).Select(AsChoice).Take(25));

        private static bool Match(OriginId originId, ApplicationCommandInteractionDataOption option)
            => OriginDataService.Data.OriginIds[originId].Name.Contains(option.Value!, StringComparison.InvariantCultureIgnoreCase);

        private static ApplicationCommandOptionChoiceProperties AsChoice(OriginId originId)
            => new(OriginDataService.Data.UniqueOriginNames[originId], originId.GetFullId());
    }


    public sealed class AccessibleCreatorProvider : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
            => ValueTask.FromResult<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(
                OriginDataService.Data.OrderedCreatorIds.Where(creatorId => Match(creatorId, option, context)).Select(AsChoice).Take(25));

        private static bool Match(ulong creatorId, ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(context);
            if (userPermissions < UserPermissions.Creator)
                return false;

            if (userPermissions >= UserPermissions.Admin)
            {
                string name = OriginDataService.Data.CreatorIds.TryGetValue(creatorId, out CreatorData? creator) ? creator.Name : creatorId.ToString();
                return name.Contains(option.Value!, StringComparison.InvariantCultureIgnoreCase);
            }

            return creatorId == context.User.Id;
        }

        private static ApplicationCommandOptionChoiceProperties AsChoice(ulong creatorId)
            => new(OriginDataService.Data.UniqueCreatorNames.TryGetValue(creatorId, out string? name) ? name : $"<@{creatorId}>", creatorId.ToString());
    }

    public sealed class AccessiblePackProvider : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
            => ValueTask.FromResult<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(
                OriginDataService.Data.OrderedPackIds.Where(packId => Match(packId, option, context)).Select(AsChoice).Take(25));

        private static bool Match(PackId packId, ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
        {
            UserPermissions userPermissions = PermissionService.GetUserPermissions(context);
            if (userPermissions < UserPermissions.Creator)
                return false;

            PackData? pack;
            if (userPermissions >= UserPermissions.Admin)
            {
                string name = OriginDataService.Data.PackIds.TryGetValue(packId, out pack) ? pack.Name : packId.Value;
                return name.Contains(option.Value!, StringComparison.InvariantCultureIgnoreCase);
            }

            return OriginDataService.Data.PackIds.TryGetValue(packId, out pack) &&
                pack.CreatorIds.Contains(context.User.Id) &&
                pack.Name.Contains(option.Value!, StringComparison.InvariantCultureIgnoreCase);
        }

        private static ApplicationCommandOptionChoiceProperties AsChoice(PackId packId)
            => new(OriginDataService.Data.UniquePackNames.TryGetValue(packId, out string? name) ? name : packId.Value, packId.Value);
    }
}
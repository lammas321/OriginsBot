using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using OriginsBot.Services;

namespace OriginsBot.Commands
{
    public sealed class ReorderModal : ComponentInteractionModule<ModalInteractionContext>
    {
        [ComponentInteraction("reorder_modal")]
        public async Task Reorder()
        {
            if (Context.Components[0] is not Label creatorsLabel ||
                creatorsLabel.Component is not TextInput creatorsInput ||
                Context.Components[1] is not Label packsLabel ||
                packsLabel.Component is not TextInput packsInput)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Something is wrong with the modal.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            string[] creators = [.. creatorsInput.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(creatorIdString => ulong.TryParse(creatorIdString, out ulong creatorId) && OriginDataService.Data.OrderedCreatorIds.Contains(creatorId))];

            string[] packs = [.. packsInput.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(packIdString => PackId.IsValid(packIdString) && OriginDataService.Data.OrderedPackIds.Contains(new(packIdString)))];

            if (creators.Length == 0 || packs.Length == 0)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "At least one creator and one pack must be ordered.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }


            File.WriteAllText(OriginDataService.OrderedCreatorsFile, string.Join('\n', creators));
            File.WriteAllText(OriginDataService.OrderedPacksFile, string.Join('\n', packs));


            if (!OriginDataService.TryReload(out _))
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
                {
                    Content = "Someone tell <@394347954900697108> there is a problem with my AI.\nSomething is fundamentally broken (could be logic or data) and most likely can only be fixed manually.",
                    Flags = MessageFlags.Ephemeral,
                }));
                return;
            }

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new()
            {
                Content = "Reordered creators and packs successfully.",
                Flags = MessageFlags.Ephemeral,
            }));
        }
    }
}
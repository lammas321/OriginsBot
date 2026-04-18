using NetCord;
using NetCord.Gateway;
using NetCord.Services;

namespace OriginsBot.Services
{
    public static class PermissionService
    {
        private const string CreatorRoleFileName = "creator_role.txt";
        private static readonly string CreatorRoleFile = Path.Combine(OriginDataService.CreatorsDirectory, CreatorRoleFileName);


        private static ulong _creatorRoleId;

        public static void Reload()
        {
            if (ulong.TryParse(File.ReadAllText(CreatorRoleFile), out ulong creatorRoleId))
                _creatorRoleId = creatorRoleId;
        }


        public static UserPermissions GetUserPermissions<T>(T context) where T : IGuildContext, IUserContext
            => GetUserPermissions(context.User, context.Guild);

        public static UserPermissions GetUserPermissions(User user, Guild? guild)
        {
            if (user is not PartialGuildUser guildUser || guild == null)
                return UserPermissions.None;

            if ((guildUser.GetPermissions(guild) & Permissions.Administrator) == Permissions.Administrator)
                return UserPermissions.Admin;

            if (guildUser.RoleIds.Contains(_creatorRoleId))
                return UserPermissions.Creator;

            return UserPermissions.None;
        }
    }
    
    public enum UserPermissions
    {
        None,
        Creator,
        Admin
    }
}
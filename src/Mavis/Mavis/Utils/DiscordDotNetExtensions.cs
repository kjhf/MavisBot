using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mavis.Utils
{
  internal static class DiscordDotNetExtensions
  {
    public static SocketGuildChannel? GetGuildChannel(this SocketSlashCommand command)
      => command.Channel as SocketGuildChannel;

    public static SocketGuild? GetGuild(this SocketSlashCommand command)
      => command.GetGuildChannel()?.Guild;

    public static SocketGuildUser? GetGuildUser(this SocketSlashCommand command)
      => command.User as SocketGuildUser;

    public static async Task RemoveRolesAsync(this SocketGuild _, IEnumerable<IRole> roles, RequestOptions? options = null)
    {
      foreach (var role in roles)
        await role.DeleteAsync(options);
    }
  }
}
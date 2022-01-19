using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  /// <summary>
  /// This handler describes a command with multiple names and responses...
  /// </summary>
  public interface IMavisMultipleCommand
  {
    public const string UserNameReplaceString = "%username%";
    public const string MentionUserNameReplaceString = "%mentionusername%";
    public const string CommandDetailReplaceString = "%commanddetail%";
    public const string EscapedDetailReplaceString = "%escapeddetail%";
    public const string UsernameOrDetailReplaceString = "%usernameordetail%";
    public const string BotNameReplaceString = "%botname%";
    public const string DevelopmentServerReplaceString = "%developmentserver%";

    /// <summary>The name of the command type/category. For debugging and categorization purposes.</summary>
    public string CommandTypeName { get; }

    /// <summary>A list of names this multi command responds to.</summary>
    public IList<string> Names { get; }

    /// <summary>Build all the properties should include everything in Names.</summary>
    public IList<ApplicationCommandProperties> BuildCommands();

    /// <summary>Execute the multi-command with the given command.</summary>
    public Task Execute(DiscordSocketClient _client, SocketSlashCommand command, string name);
  }
}
using Discord;
using Discord.WebSocket;
using NLog;
using System.Linq;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  public class AvatarCommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public string Name => "avatar";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Privately get a user or server's avatar.")
          .AddOption("arg", ApplicationCommandOptionType.String, "The avatar to get. Omit arg to get yours, or write a server id, user id, or user mention.", isRequired: false)
          .Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      ulong requestedId;
      string? detail = command.Data.Options.FirstOrDefault()?.Value?.ToString();
      log.Trace($"Processing {Name} with arg {detail}.");

      if (string.IsNullOrWhiteSpace(detail))
      {
        requestedId = command.User.Id;
      }
      else
      {
        // Parsing a mention?
        if (detail.EndsWith(">"))
        {
          if (detail.StartsWith("<@!"))
          {
            detail = detail.Substring("<@!".Length, detail.Length - 3); // Minus 3 for <@nnnnn>
          }
          else if (detail.StartsWith("<@"))
          {
            detail = detail.Substring("<@".Length, detail.Length - 3);
          }
        }

        // Id?
        if (!ulong.TryParse(detail, out requestedId))
        {
          // No, fail.
          await command.RespondAsync(text: $"❌ No results or unable to parse {detail}.", ephemeral: true).ConfigureAwait(false);
          return;
        }
      }

      // From the id, determine if it's a user or server.
      // Is it a server?
      var candidateServer = client.GetGuild(requestedId);
      if (candidateServer != null)
      {
        await command.RespondAsync(text: $"{candidateServer.IconUrl}", ephemeral: true);
      }
      else
      {
        // Is it a user?
        IUser candidateUser = client.GetUser(requestedId);
        if (candidateUser != null)
        {
          await command.RespondAsync(text: $"{candidateUser.GetAvatarUrl(ImageFormat.Auto, 2048)}", ephemeral: true).ConfigureAwait(false);
        }
      }
    }
  }
}
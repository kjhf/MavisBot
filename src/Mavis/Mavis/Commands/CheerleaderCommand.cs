using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  public class CheerleaderCommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private static readonly ReadOnlyCollection<string> cheerleaderSymbols = new(new[] { "🙆", "🙆🏻", "🙆🏼", "🙆🏽", "🙆🏾", "🙆🏿", "🙆‍♂️", "🙆🏻‍♂️", "🙆🏼‍♂️", "🙆🏽‍♂️", "🙆🏾‍♂️", "🙆🏿‍♂️", "🙆‍♀️", "🙆🏻‍♀️", "🙆🏼‍♀️", "🙆🏽‍♀️", "🙆🏾‍♀️", "🙆🏿‍♀️" });
    private static readonly Random rand = new();

    public string Name => "cheerleader";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Have cheerleaders say a phrase.")
          .AddOption("phrase", ApplicationCommandOptionType.String, "The phrase to cheer!", isRequired: true)
          .Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      StringBuilder sb = new StringBuilder();
      string commandDetail = command.Data.Options.First().Value.ToString()!.StripAccents();
      log.Trace($"Processing {Name} with arg {commandDetail}.");

      int length = commandDetail.Length;

      if (length > 80)
      {
        await command.RespondAsync(text: "⛔ Your mesage is too long.", ephemeral: true);
      }
      else
      {
        sb.AppendLine();
        foreach (char c in commandDetail)
        {
          if (char.IsLetterOrDigit(c))
          {
            sb.Append(":regional_indicator_").Append(char.ToLowerInvariant(c)).Append(": ");
          }
          else if (c == ' ')
          {
            sb.Append(":blue_heart: ");
          }
          else if (c == '!')
          {
            sb.Append(":grey_exclamation: ");
          }
          else if (c == '?')
          {
            sb.Append(":grey_question: ");
          }
          else if (c == '\'')
          {
            sb.Append(":arrow_down_small: ");
          }
          else
          {
            sb.Append(c);
          }
        }

        sb.AppendLine();

        foreach (char _ in commandDetail)
        {
          sb.Append(cheerleaderSymbols[rand.Next(0, cheerleaderSymbols.Count)]).Append(' ');
        }
      }

      // Respond with the cheerleader message
      await command.RespondAsync(text: sb.ToString(), ephemeral: false);
    }
  }
}
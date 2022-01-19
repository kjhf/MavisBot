using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  record ResponseListSubCommand
  {
    public readonly string[][] choices;
    public readonly bool isPrivate;
    public readonly string format;
    public readonly SlashCommandBuilder subCommand;

    public ResponseListSubCommand(SlashCommandBuilder subCommand, bool isPrivate, string format, string[][] choices)
    {
      this.format = format;
      this.choices = choices;
      this.isPrivate = isPrivate;
      this.subCommand = subCommand;
    }
  }

  public class ResponseListCommand : IMavisMultipleCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private static readonly Random rand = new();
    private readonly Dictionary<string, ResponseListSubCommand> subCommands = new();

    public string CommandTypeName => "ResponseList";
    public IList<string> Names => subCommands.Keys.ToArray();

    public ResponseListCommand()
    {
      List<ResponseListSubCommand> toAdd = new();

      toAdd.Add(
      new ResponseListSubCommand(subCommand: new SlashCommandBuilder().WithName("card").WithDescription("Draw a random playing card"),
        isPrivate: false, format: "{0} of {1}", choices: new[]
        {
          new[] { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" },
          new[] { "Hearts", "Diamonds", "Clubs", "Spades" },
        })
      );
      subCommands = toAdd.ToDictionary(pair => pair.subCommand.Name, pair => pair);
    }

    public IList<ApplicationCommandProperties> BuildCommands()
    {
      return subCommands.Select(pair => pair.Value.subCommand.Build()).ToArray();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command, string name)
    {
      log.Trace($"Processing {nameof(ResponsesCommand)} {name}.");
      var subCommand = subCommands[name];

      var choice = new StringBuilder(subCommand.format);

      for (int i = 0; i < subCommand.choices.Length; i++)
      {
        choice.Replace($"{{{i}}}", subCommand.choices[i][rand.Next(subCommand.choices[i].Length)]);
      }

      string result = ReplaceVariables(client, command, choice.ToString());

      // Respond with the message
      await command.RespondAsync(text: result, ephemeral: subCommand.isPrivate).ConfigureAwait(false);
    }

    private static string ReplaceVariables(DiscordSocketClient client, SocketSlashCommand command, string choice)
    {
      string commandDetail = string.Join(" ", command.Data.Options.Select(option => option.Value));
      return choice
       .Replace(IMavisMultipleCommand.MentionUserNameReplaceString, "<@" + command.User.Id + ">")
       .Replace(IMavisMultipleCommand.UserNameReplaceString, command.User.Username)
       .Replace(IMavisMultipleCommand.CommandDetailReplaceString, commandDetail)
       .Replace(IMavisMultipleCommand.EscapedDetailReplaceString, Uri.EscapeDataString(commandDetail).Replace("+", "%2B"))
       .Replace(IMavisMultipleCommand.UsernameOrDetailReplaceString, string.IsNullOrWhiteSpace(commandDetail) ? command.User.Username : commandDetail)
       .Replace(IMavisMultipleCommand.BotNameReplaceString, client.CurrentUser.Username)
       .Replace(IMavisMultipleCommand.DevelopmentServerReplaceString, Constants.DevelopmentServerLink)
       .Replace("\\r\\n", Environment.NewLine)
       .Replace("\\n", Environment.NewLine);
    }
  }
}
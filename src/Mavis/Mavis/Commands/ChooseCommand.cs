using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  public class ChooseCommand : IMavisCommand
  {
    private static readonly Random random = new();
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    public string Name => "choose";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      var builder = new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Randomly choose between all given options.")
          ;
      for (int i = 0; i < DiscordLimits.NUMBER_OF_FIELDS_LIMIT; i++)
      {
        string n = (i + 1).ToString();
        builder.AddOption("choice-" + n, ApplicationCommandOptionType.String, "Choice " + n, isRequired: false);
      }
      return builder.Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      Dictionary<string, string> commandParams = command.Data.Options.ToDictionary(kv => kv.Name, kv => kv.Value?.ToString() ?? "");
      log.Trace($"Processing {Name} with params: {string.Join(", ", commandParams.Select(kv => kv.Key + "=" + kv.Value))} ");
      var choice = random.Choice(commandParams.Values.ToImmutableArray());
      await command.RespondAsync(choice);
    }
  }
}
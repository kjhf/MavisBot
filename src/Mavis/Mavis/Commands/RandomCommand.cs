using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  public class RandomCommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private static readonly Random rand = new();

    public string Name => "random";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Random number generator")
          .AddOption("param1", ApplicationCommandOptionType.String, "The maximum bound (or 0-10 if not specified) -OR- the minimum bound if you specify param2", isRequired: false)
          .AddOption("param2", ApplicationCommandOptionType.String, "The maximum bound", isRequired: false)
          .Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      StringBuilder output = new StringBuilder();
      string[] commandParams = command.Data.Options.Select(c => c.Value.ToString() ?? "").ToArray();
      log.Trace($"Processing {Name}.");

      long min = 0;
      long max;
      if (commandParams.Length == 0)
      {
        max = 10;
      }
      else if (commandParams.Length == 1)
      {
        if (!long.TryParse(commandParams[0], NumberStyles.Any, CultureInfo.InvariantCulture, out max))
        {
          if (commandParams[0] == "∞" || commandParams[0].StartsWith("inf"))
          {
            max = long.MaxValue;
          }
          else if (commandParams[0] == "-∞" || commandParams[0].StartsWith("-inf"))
          {
            max = long.MinValue;
          }
          else
          {
            output.AppendLine($"❌ I don't understand your parameter: {commandParams[0]}.");
            max = 10;
          }
        }
      }
      else
      {
        if (!long.TryParse(commandParams[0], NumberStyles.Any, CultureInfo.InvariantCulture, out min))
        {
          if (commandParams[0] == "∞" || commandParams[0].StartsWith("inf"))
          {
            min = long.MaxValue;
          }
          else if (commandParams[0] == "-∞" || commandParams[0].StartsWith("-inf"))
          {
            min = long.MinValue;
          }
          else
          {
            output.AppendLine($"❌ I don't understand your parameter: {commandParams[0]}.");
            min = 0;
          }
        }

        if (!long.TryParse(commandParams[1], NumberStyles.Any, CultureInfo.InvariantCulture, out max))
        {
          if (commandParams[1] == "∞" || commandParams[1].StartsWith("inf"))
          {
            max = long.MaxValue;
          }
          else if (commandParams[1] == "-∞" || commandParams[1].StartsWith("-inf"))
          {
            max = long.MinValue;
          }
          else
          {
            output.AppendLine($"❌ I don't understand your parameter: {commandParams[1]}.");
            max = 10;
          }
        }
      }
      if (min > max)
      {
        // Swap
        long temp = max;
        max = min;
        min = temp;
      }

      long num = rand.NextLong(min, max == long.MaxValue ? long.MaxValue : max + 1);
      output.Append(min).Append(" -> ").Append(max).Append(": ").Append(num).AppendLine();

      ulong range = (ulong)(max - min);
      if (range == 0) range = 1;

      long normNum = (num - min);
      float percentage = (((float)normNum) / range) * 360;
      if (percentage > 360) { percentage = 360; }
      var drawingColour = Imaging.ImageManipulator.FromAHSB(255, percentage, 0.8f, 0.5f);
      var responseColor = new Color(drawingColour.R, drawingColour.G, drawingColour.B);

      await command.RespondAsync(text: output.ToString(), ephemeral: true).ConfigureAwait(false);
    }
  }
}
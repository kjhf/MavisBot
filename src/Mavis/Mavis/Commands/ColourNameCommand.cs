using Discord;
using Discord.WebSocket;
using Mavis.Imaging;
using Mavis.Utils;
using NLog;
using System;
using System.DrawingCore;
using System.Linq;
using System.Threading.Tasks;
using DiscordColor = Discord.Color;
using DrawingColor = System.DrawingCore.Color;

namespace Mavis.Commands
{
  internal class ColourNameCommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public string Name => "colour-name";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Evaluate a hex code for its nearest colour name.")
          .AddOption("code", ApplicationCommandOptionType.String, "The code as a 3-, 6-, or 8- digit colour code.", isRequired: true)
          .Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      string? input = command.Data.Options.FirstOrDefault()?.Value?.ToString();
      log.Trace($"Processing {Name} with arg {input}.");

      if (string.IsNullOrWhiteSpace(input))
      {
        const string err = "❌ Nothing specified?";
        await command.RespondAsync(err, ephemeral: true).ConfigureAwait(false);
        return;
      }

      var inputColour = ImageManipulator.FromString(input);
      if (inputColour == null)
      {
        const string message = "❌ I don't understand your input.";
        await command.RespondAsync(message, ephemeral: true).ConfigureAwait(false);
      }
      else
      {
        DrawingColor color = inputColour.Value;
        DiscordColor discordColor = color.ToDiscordColor();
        ConsoleColor nearestConsoleColor = color.ToConsoleColor();
        KnownColor nearestKnownColor = color.ToNearestKnownColor();

        string colourString = nearestKnownColor.ToString().ToTitleCase();
        bool isExact = (nearestKnownColor.ToString().Equals(nearestConsoleColor.ToString()));
        string message = isExact ? $"✓ It's {colourString}" : $"(~) It's near to {colourString}";
        await command.RespondAsync(text: message, embed: new MavisEmbedBuilder(message, discordColor).BuildFirst(), ephemeral: false).ConfigureAwait(false);
      }
    }
  }
}
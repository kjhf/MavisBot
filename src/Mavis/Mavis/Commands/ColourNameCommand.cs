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

      DiscordColor discordColor;
      string message;

      if (ImageManipulator.FindHexColour(input, out string? foundColour))
      {
        DrawingColor color = ImageManipulator.FromHexCode(foundColour);
        discordColor = new DiscordColor(color.R, color.G, color.B);

        ConsoleColor nearestConsoleColor = color.ToConsoleColor();
        KnownColor nearestKnownColor = color.ToNearestKnownColor();
        message = (nearestKnownColor.ToString().Equals(nearestConsoleColor.ToString())) ? $"✓ It's {nearestKnownColor.ToString().ToTitleCase()}" : $"(~) It's near to {nearestKnownColor.ToString().ToTitleCase()}";
        await command.RespondAsync(text: message, embed: new MavisEmbedBuilder(message, discordColor).BuildFirst(), ephemeral: false).ConfigureAwait(false);
      }
      else if (Enum.TryParse(input, true, out KnownColor knownColor))
      {
        var color = DrawingColor.FromKnownColor(knownColor);
        discordColor = new DiscordColor(color.R, color.G, color.B);
        message = $"✓ {knownColor}";
        await command.RespondAsync(text: message, embed: new MavisEmbedBuilder(message, discordColor).BuildFirst(), ephemeral: false).ConfigureAwait(false);
      }
      else
      {
        const string err = "❌ I don't understand your input.";
        await command.RespondAsync(err, ephemeral: true).ConfigureAwait(false);
        return;
      }
    }
  }
}
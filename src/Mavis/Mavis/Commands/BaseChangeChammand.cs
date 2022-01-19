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
  public class BaseChangeCommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public string Name => "basechange";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Changes number(s) or string from one base to another.")
          .AddOption("input", ApplicationCommandOptionType.String, "The number(s) or string to convert.", isRequired: true)
          .AddOption(new SlashCommandOptionBuilder().WithName("from").WithType(ApplicationCommandOptionType.Integer).WithDescription("The base that the number or string has been specified in.").WithRequired(true).AddChoice("2", 2).AddChoice("8", 8).AddChoice("10", 10).AddChoice("16", 16).AddChoice("64", 64))
          .AddOption(new SlashCommandOptionBuilder().WithName("to").WithType(ApplicationCommandOptionType.Integer).WithDescription("The base that the number or string should be converted to.").WithRequired(true).AddChoice("2", 2).AddChoice("8", 8).AddChoice("10", 10).AddChoice("16", 16).AddChoice("64", 64))
          .Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      StringBuilder sb = new();
      var commandParams = command.Data.Options.ToArray();
      if (commandParams.Length != 3)
      {
        const string error = "Expected 3 parameters from command.";
        log.Error(error);
        throw new ArgumentException(error, nameof(command));
      }

      string input = commandParams[0].Value.ToString()!;
      int fromBase = int.Parse(commandParams[1].Value.ToString() ?? "-1");
      int toBase = int.Parse(commandParams[2].Value.ToString() ?? "-1");
      Color? embedColour = null;

      log.Trace($"Processing {Name} with args {input} {fromBase} {toBase}.");

      // Remove decorators and replace commas with spaces
      input = input.Replace("0x", "").Replace("0b", "").Replace("#", "").Replace(",", " ").Trim();

      // Special case hex to decimal in cases of 6 or 8 characters.
      bool is6Chars = input.Length == 6;
      bool is8Chars = input.Length == 8;
      if (fromBase == 16 && (is6Chars || is8Chars))
      {
        // Try and convert the source hex number into a colour.
        if (uint.TryParse(input, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb))
        {
          // Remove alpha channel (N.B. this usually makes it transparent as a = 0, but Discord fixes this to fully opaque)
          argb &= 0x00FFFFFF;
          embedColour = new Color(argb);
        }

        input += " ";
        input += input.Substring(0, 2);
        input += " ";
        input += input.Substring(2, 2);
        input += " ";
        input += input.Substring(4, 2);
        if (is8Chars)
        {
          input += " ";
          input += input.Substring(6, 2);
        }
      }

      string[] inputWithSpaces = input.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

      if (fromBase == 10)
      {
        // Try and convert the source decimal number(s) into a colour.
        if (inputWithSpaces.Length == 1)
        {
          if (uint.TryParse(inputWithSpaces[0], out uint argb))
          {
            // Remove alpha channel (N.B. this usually makes it transparent as a = 0, but Discord fixes this to fully opaque)
            argb &= 0x00FFFFFF;
            embedColour = new Color(argb);
          }
        }
        else if (inputWithSpaces.Length == 3 || commandParams.Length == 4)
        {
          // Using -n here as alpha could be included at the front.
          bool canParseColour = (byte.TryParse(inputWithSpaces[^3], out byte r));
          canParseColour &= (byte.TryParse(inputWithSpaces[^2], out byte g));
          canParseColour &= (byte.TryParse(inputWithSpaces[^1], out byte b));

          if (canParseColour)
          {
            embedColour = new Color(r, g, b);
          }
        }
      }

      foreach (string col in inputWithSpaces)
      {
        try
        {
          switch (fromBase)
          {
            case 2:
            case 8:
            case 10:
            case 16:
            {
              long part = Convert.ToInt64(col, fromBase);
              byte[] parts = BitConverter.GetBytes(part);
              if (toBase == 64)
              {
                sb.Append(Convert.ToBase64String(parts)).Append(' ');
              }
              else if (toBase == 16)
              {
                sb.Append(part.ToString("X2")).Append(' ');
              }
              else
              {
                sb.Append(Convert.ToString(part, toBase)).Append(' ');
              }
              break;
            }

            case 64:
            {
              byte[] parts = Convert.FromBase64String(col);
              long part = BitConverter.ToInt64(parts, 0);

              if (toBase == 64)
              {
                sb.Append(Convert.ToBase64String(parts)).Append(' ');
              }
              else if (toBase == 16)
              {
                sb.Append(part.ToString("X2")).Append(' ');
              }
              else
              {
                sb.Append(Convert.ToString(part, toBase)).Append(' ');
              }
              break;
            }
          }
        }
        catch (FormatException)
        {
          sb.Append("❌ ").Append("I didn't understand your input").Append(": ").Append(col).AppendLine(".");
        }
        catch (Exception ex)
        {
          sb.Append("❌ ").Append("Oops, something went wrong..").Append(": ").Append(col).Append(' ').AppendLine(ex.Message);
        }
      }

      // If multiple things to translate, also provide the answer without spaces.
      if (inputWithSpaces.Length > 2)
      {
        string outputWithoutSpace = sb.ToString().Replace(" ", "");
        sb.AppendLine();
        switch (toBase)
        {
          case 2: sb.Append("0b "); break;
          case 8: sb.Append("0o "); break;
          case 16: sb.Append("0x "); break;
        }
        sb.AppendLine(outputWithoutSpace);
      }

      string output = sb.ToString();
      await command.RespondAsync(embed: EmbedUtility.ToEmbed(output, embedColour).Build(), ephemeral: true).ConfigureAwait(false);
    }
  }
}
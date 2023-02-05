using Discord;
using Discord.WebSocket;
using Mavis.Imaging;
using Mavis.Utils;
using NLog;
using System;
using System.DrawingCore;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Image = System.DrawingCore.Image;

namespace Mavis.Commands
{
  public class ASCIICommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public string Name => "ascii";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Converts an image into ASCII art.")
          .AddOption("url", ApplicationCommandOptionType.String, "Specify a url (in future when Discord implements attachments in /commands, this will be optional).", isRequired: true)
          .Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      log.Trace($"Processing {Name}.");

      string? url = command.Data.Options.FirstOrDefault()?.Value?.ToString();
      log.Trace($"{(url == null ? "URL not specified." : "URL was specified: " + url)}.");

      if (url == null)
      {
        const string err = "❌ No URL specified, and I can't read attachments yet. ";
        await command.RespondAsync(err, ephemeral: true).ConfigureAwait(false);
        return;
      }

      await command.RespondAsync(text: "<a:typing:897094396502224916> Just a sec!", ephemeral: true);
      _ = Task.Run(async () =>
      {
        try
        {
          // Check if the url is a file
          if (await WebHelper.IsImageUrlAsync(url))
          {
            // It is, download and perform meme
            var tuple = await WebHelper.DownloadFile(url).ConfigureAwait(false);
            if (tuple.Item2 != null)
            {
              var (success, message) = DoBuildAsciiImage(tuple.Item2);
              await command.FollowupAsync(message, ephemeral: !success).ConfigureAwait(false);
            }
            else
            {
              // We failed, return a response indicating the failure.
              string err = "⛔ " + tuple.Item1.ReasonPhrase;
              await command.FollowupAsync(err, ephemeral: true).ConfigureAwait(false);
            }
          }
          else
          {
            const string err = "❌ Not a recognised image URL. ";
            await command.FollowupAsync(err, ephemeral: true).ConfigureAwait(false);
          }
        }
        catch (HttpRequestException ex)
        {
          log.Debug($"HttpRequestException exception downloading file: {url}. {ex.Message}");
          log.Trace(ex);

          string err = "⛔ Bad http response: " + ex.Message;
          await command.FollowupAsync(err, ephemeral: true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          log.Debug($"Exception downloading or handling meme file: {url}. {ex.Message}");
          log.Trace(ex);
        }

        try
        {
          await command.DeleteOriginalResponseAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          log.Debug($"Exception deleting the original response to {command.Data.Name}. {ex.Message}");
          log.Trace(ex);
        }
      }).ConfigureAwait(false);
    }

    private (bool, string) DoBuildAsciiImage(byte[] file)
    {
      try
      {
        const int MaximumNumberOfCharacters = DiscordLimits.MESSAGE_TEXT_LIMIT;
        const int MaximumWidth = 130;
        const int MaximumHeight = 60;

        using Image original = Image.FromStream(new MemoryStream(file));
        using Bitmap bmp = new Bitmap(original);
        float width = original.Width;
        float height = original.Height;

        // width + 1 for new line
        while (((((int)width + 1) * (int)height) > MaximumNumberOfCharacters) || (width > MaximumWidth) || (height > MaximumHeight))
        {
          width *= 0.9995f;
          height *= 0.9995f;

          if (width < 1)
          {
            width = 1;
          }
          if (height < 1)
          {
            height = 1;
          }
        }

        using Bitmap newCanvas = new Bitmap((int)width, (int)height);
        using (Graphics g = Graphics.FromImage(newCanvas))
        {
          g.Clear(System.DrawingCore.Color.White);
          g.DrawImage(original, 0, 0, (int)width, (int)height);
          g.Save();
        }
        return (true, $"```{ImageManipulator.GreyscaleImageToASCII(newCanvas)}```");
      }
      catch (OutOfMemoryException)
      {
        return (false, "❌ The URL specified is not a file or the file is too big (< 4MB please!)");
      }
      catch (IOException iox)
      {
        // e.g. path error
        log.Info(iox);
        return (false, "❗ " + iox.Message);
      }
      catch (Exception sysEx)
      {
        // everything else
        log.Error(sysEx);
        return (false, "❗ " + sysEx.Message);
      }
    }
  }
}
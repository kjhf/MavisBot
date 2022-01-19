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
  internal class ObaboCommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public string Name => "obabo";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Splits an image in half and mirrors one side onto another.")
          .AddOption(new SlashCommandOptionBuilder().WithName("mirroring-mode").WithType(ApplicationCommandOptionType.Integer).WithDescription("How to mirror the image.").WithRequired(true)
            .AddChoice("Left onto Right (\"obabo\")", (int)MirrorType.LeftOntoRight)
            .AddChoice("Right onto Left (\"amama\")", (int)MirrorType.RightOntoLeft)
            .AddChoice("Top onto Bottom (\"hooh\")", (int)MirrorType.TopOntoBottom)
            .AddChoice("Bottom onto Top (\"uoou\")", (int)MirrorType.BottomOntoTop)
           )
          .AddOption("url", ApplicationCommandOptionType.String, "Specify a url (in future when Discord implements attachments in /commands, this will be optional).", isRequired: true)
          .Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      log.Trace($"Processing {nameof(ObaboCommand)}.");

      if (!Enum.TryParse(command.Data.Options.FirstOrDefault()?.Value?.ToString(), out MirrorType mirror))
      {
        const string err = "❌ Nothing specified?";
        await command.RespondAsync(err, ephemeral: true).ConfigureAwait(false);
        return;
      }

      string? url = command.Data.Options.Skip(1).FirstOrDefault()?.Value?.ToString();
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
            if (WebHelper.IsImageUrl(url))
            {
              // It is, download and perform obabo
              var tuple = await WebHelper.DownloadFile(url).ConfigureAwait(false);
              if (tuple.Item2 != null)
              {
                string fileOrError = DoBuildObaboImage(tuple.Item2, mirror);
                if (File.Exists(fileOrError))
                {
                  await command.FollowupWithFileAsync(
                    filePath: fileOrError,
                    fileName: Path.GetFileName(fileOrError),
                    text: $"Here you go, one Obabo meme!",
                    ephemeral: false
                  );
                }
                else
                {
                  await command.FollowupAsync(fileOrError, ephemeral: true).ConfigureAwait(false);
                }
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
            log.Debug($"Exception deleting the original response to {command.Data.Name} Obabo. {ex.Message}");
            log.Trace(ex);
          }
        }).ConfigureAwait(false);
    }

    private string DoBuildObaboImage(byte[] file, MirrorType mirrorType)
    {
      try
      {
        using Image original = Image.FromStream(new MemoryStream(file));
        using Bitmap bmp = new Bitmap(original);
        ImageManipulator.PerformObabo(bmp, mirrorType);
        string filePath = Path.GetTempFileName() + ".png";
        bmp.Save(filePath);
        return filePath;
      }
      catch (OutOfMemoryException)
      {
        return "❌ The URL specified is not a file or the file is too big (< 4MB please!)";
      }
      catch (IOException iox)
      {
        // e.g. path error
        log.Info(iox);
        return "❗ " + iox.Message;
      }
      catch (Exception sysEx)
      {
        // everything else
        log.Error(sysEx);
        return "❗ " + sysEx.Message;
      }
    }
  }
}
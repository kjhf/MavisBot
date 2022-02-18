using Discord;
using Discord.WebSocket;
using Mavis.Imaging;
using Mavis.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Image = System.DrawingCore.Image;

namespace Mavis.Commands
{
  record MemeDetails
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    public readonly ulong id;

    public Point topLeft, topRight, bottomLeft;
    public static string TemplateDir => Path.Combine(Path.GetTempPath(), Constants.ProgramName);
    public string TemplatePath => Path.Combine(TemplateDir, id.ToString() + ".png");
    public bool Cached => File.Exists(TemplatePath);

    static MemeDetails()
    {
      Directory.CreateDirectory(TemplateDir);
    }
    private MemeDetails(ulong id)
    {
      this.id = id;
    }

    /// <summary>
    /// Load a meme by its id.
    /// </summary>
    public static async Task<MemeDetails?> CreateMemeDetails(ulong id, IAttachment? file)
    {
      var details = new MemeDetails(id);

      if ((file is null) && (!details.Cached))
      {
        throw new ArgumentNullException(nameof(file), "File must be specified if the meme is not in the cache.");
      }
      else if ((file is not null) && (!details.Cached))
      {
        await WebHelper.DownloadAndWriteFile(file.Url, details.TemplatePath).ConfigureAwait(false);
      }

      int lowX = int.MaxValue, lowY = int.MaxValue;
      int highX = int.MinValue, highY = int.MinValue;

      using (Bitmap template = new Bitmap(details.TemplatePath))
      {
        for (int x = 0; x < template.Width; x++)
        {
          for (int y = 0; y < template.Height; y++)
          {
            if (template.GetPixel(x, y).A == 0)
            {
              if (x < lowX)
              {
                lowX = x;
              }
              if (x > highX)
              {
                highX = x;
              }
              if (y < lowY)
              {
                lowY = y;
              }
              if (y > highY)
              {
                highY = y;
              }
            }
          }
        }
      }

      details.topLeft = new Point(lowX, lowY);
      details.topRight = new Point(highX, lowY);
      details.bottomLeft = new Point(lowX, highY);

      if (
        details.topLeft.Y < details.bottomLeft.Y
        && details.topRight.Y < details.bottomLeft.Y
        && details.bottomLeft.X < details.topRight.X
        && details.topLeft.X < details.topRight.X)
      {
        return details;
      }
      else
      {
        log.Warn($"Failed to load meme {details.TemplatePath} as the points aren't correct.");
        return null;
      }
    }
  }

  internal class MemeCommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, MemeDetails> memeTemplates = new();
    private DiscordSocketClient? _client;

    private string AvailableMemes => string.Join(", ", memeTemplates.Keys.OrderBy(c => c));

    public string Name => "meme";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      this._client = client;
      client.MessageReceived += Client_MessageReceived;

      Task.Run(async () =>
      {
        log.Trace("Loading memes...");
        if (_client is not null)
        {
          ITextChannel? memeChannel = null;
          string? memeChannelStr = Environment.GetEnvironmentVariable("MEME_TEMPLATE_CHANNEL");

          if (memeChannelStr != null)
          {
            if (ulong.TryParse(memeChannelStr, out ulong memeChannelULong))
            {
              memeChannel = (ITextChannel)await this._client.GetChannelAsync(memeChannelULong);
              log.Debug($"✔ Meme channel found: {memeChannelStr}");
              log.Trace("Downloading memes from template channel...");
              var messages = (await memeChannel.GetMessagesAsync().FlattenAsync().ConfigureAwait(false)).ToArray();
              log.Trace("... " + messages.Length + " memes to load.");
              foreach (var message in messages)
              {
                await LoadMeme(message).ConfigureAwait(false);
              }
            }
            else
            {
              log.Error($"Meme channel was found in the env variable MEME_TEMPLATE_CHANNEL but is not an id: {memeChannelStr}");
            }
          }
          else
          {
            log.Warn("Meme channel was not found in the env variable MEME_TEMPLATE_CHANNEL.");
          }
        }
        log.Trace("...Finished loading memes.");
      });

      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Make a meme.")
          .AddOption("arg", ApplicationCommandOptionType.String, "The meme template to use. Write `list` to view your options if you don't know.", isRequired: true)
          .AddOption("url", ApplicationCommandOptionType.String, "Specify a url (in future when Discord implements attachments in /commands, this will be optional).", isRequired: true)
          .Build();
    }

    private async Task Client_MessageReceived(SocketMessage arg)
    {
      if (arg.Channel.Id == ulong.Parse(Environment.GetEnvironmentVariable("MEME_TEMPLATE_CHANNEL") ?? "0"))
      {
        // Add template
        await LoadMeme(arg).ConfigureAwait(false);
      }
    }

    private async Task LoadMeme(IMessage msg)
    {
      string name = msg.Content.ToLower();
      if (string.IsNullOrEmpty(name))
      {
        log.Warn("Cannot load meme, there's no name for it (content is null or blank). Message id: " + msg.Id);
        return;
      }

      IAttachment? file = msg.Attachments.FirstOrDefault();
      if (file is null)
      {
        log.Warn("Cannot load meme, there's no attachments on it: " + name);
      }
      else
      {
        var meme = await MemeDetails.CreateMemeDetails(msg.Id, file).ConfigureAwait(false);
        if (meme is not null)
        {
          memeTemplates.Add(name, meme);
          log.Debug("Successfully loaded " + name + " meme.");
        }
        else
        {
          log.Debug("Did not load " + name + " meme.");
        }
      }
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      log.Trace($"Processing {nameof(MemeCommand)}.");

      string? meme = command.Data.Options.FirstOrDefault()?.Value?.ToString()?.ToLower();
      if (string.IsNullOrWhiteSpace(meme))
      {
        const string err = "❌ Nothing specified?";
        await command.RespondAsync(err, ephemeral: true).ConfigureAwait(false);
        return;
      }
      else if (meme.Equals("list", StringComparison.OrdinalIgnoreCase))
      {
        var list = AvailableMemes;
        string display = string.IsNullOrEmpty(list) ? "(nothing loaded)" : list;
        await command.RespondAsync(text: display, ephemeral: true).ConfigureAwait(false);
        return;
      }

      if (!memeTemplates.ContainsKey(meme))
      {
        const string err = "❌ I don't know that meme: ";
        var list = AvailableMemes;
        string display = string.IsNullOrEmpty(list) ? "(nothing loaded)" : list;
        await command.RespondAsync(err + AvailableMemes, ephemeral: true).ConfigureAwait(false);
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
              // It is, download and perform meme
              var tuple = await WebHelper.DownloadFile(url).ConfigureAwait(false);
              if (tuple.Item2 != null)
              {
                string fileOrError = DoBuildMemeImage(tuple.Item2, memeTemplates[meme]);
                if (File.Exists(fileOrError))
                {
                  await command.FollowupWithFileAsync(
                    filePath: fileOrError,
                    fileName: Path.GetFileName(fileOrError),
                    text: $"Here you go, one {meme} meme!",
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
            log.Debug($"Exception deleting the original response to {command.Data.Name} {meme}. {ex.Message}");
            log.Trace(ex);
          }
        }).ConfigureAwait(false);
    }

    private string DoBuildMemeImage(byte[] file, MemeDetails memeDetails)
    {
      try
      {
        using Image i = Image.FromStream(new MemoryStream(file));
        using Bitmap original = new Bitmap(i);
        Point[] destinationPoints = { memeDetails.topLeft, memeDetails.topRight, memeDetails.bottomLeft };
        return ImageManipulator.PerformMeme(original, memeDetails.TemplatePath, destinationPoints);
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
using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  internal class TranslateCommand : IMavisCommand
  {
    private static readonly HashSet<string> translateCodes = new HashSet<string>() { "af", "ga", "sq", "it", "ar", "ja", "az", "kn", "eu", "ko", "bn", "la", "be", "lv", "bg", "lt", "ca", "mk", "zh-CN", "ms", "zh-TW", "mt", "hr", "no", "cs", "fa", "da", "pl", "nl", "pt", "en", "ro", "eo", "ru", "et", "sr", "tl", "sk", "fi", "sl", "fr", "es", "gl", "sw", "ka", "sv", "de", "ta", "el", "te", "gu", "th", "ht", "tr", "iw", "uk", "hi", "ur", "hu", "vi", "is", "cy", "id", "yi" };
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public string Name => "translate";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Translate text.")
          .AddOption("to", ApplicationCommandOptionType.String, "The target language as a two letter code.", isRequired: true)
          .AddOption("text", ApplicationCommandOptionType.String, "The text to translate.", isRequired: true)
          .Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      const string sourceLanguage = "auto";
      string? targetLanguage = command.Data.Options.FirstOrDefault()?.Value?.ToString();
      string? toTranslate = command.Data.Options.LastOrDefault()?.Value?.ToString();
      log.Trace($"Processing {Name} with arg {toTranslate}.");

      // Verify command.
      if (targetLanguage == null || !translateCodes.Contains(targetLanguage))
      {
        string err = $"❌ I don't recognise that language: {targetLanguage}";
        await command.RespondAsync(err, ephemeral: true).ConfigureAwait(false);
        return;
      }

      if (string.IsNullOrWhiteSpace(toTranslate))
      {
        const string err = "❌ Nothing to translate?";
        await command.RespondAsync(err, ephemeral: true).ConfigureAwait(false);
        return;
      }

      string query = Uri.EscapeDataString(toTranslate);
      string request =
        "https://translate.googleapis.com/translate_a/single?client=gtx&dt=t"
        + $"&ie=UTF-8"
        + $"&oe=UTF-8"
        + $"&sl={sourceLanguage}"
        + $"&tl={targetLanguage}"
        + $"&q={query}";
      log.Trace(request);

      JContainer? json;
      string? message = null;

      try
      {
        json = (JContainer?)await JSONHelper.GetJsonAsync(request).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        json = null;
        message = ("⛔ Couldn't reach the server: " + ex.Message);
      }

      if (json != null)
      {
        try
        {
          dynamic? outer = json[0];
          if (outer?.Count > 0)
          {
            StringBuilder translation = new();
            for (int i = 0; i < outer.Count; i++)
            {
              string? translatedLine = outer[i][0]?.ToString();
              if (translatedLine != null)
              {
                translation.AppendLine(translatedLine);
              }
            }

            if (translation.Length <= 0)
            {
              message = "⚠ Didn't get any results from the server.";
            }
            else
            {
              message = translation.ToString();
            }
          }
          else
          {
            message = "⚠ Didn't get any results from the server.";
          }
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
        {
          message = "⛔ Server sent something I didn't understand: " + ex.Message;

          // Extra information for debugging.
          log.Error(ex);
          log.Debug("Unable to process the translation response: " + ex.Message);
          log.Debug("Request: " + request);
          log.Debug("Response: " + json);
        }
      }

      if (!string.IsNullOrWhiteSpace(message))
      {
        await command.RespondAsync(text: message, ephemeral: false).ConfigureAwait(false);
      }
    }
  }
}
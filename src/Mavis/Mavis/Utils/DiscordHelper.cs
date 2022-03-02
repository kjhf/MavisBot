using Discord;
using SplatTagCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mavis.Utils
{
  public static class DiscordHelper
  {
    /// <summary>
    /// Get the string would/did trigger this slash command.
    /// </summary>
    /// <param name="interaction"></param>
    public static string AsCommandString(this ISlashCommandInteraction interaction)
    => $"/{interaction.Data.Name} {string.Join(" ", interaction.Data.Options.Select(option => $"{option.Name}:{option.Value}"))}";

    /// <summary>
    /// Sends message(s) to this message channel. If the message exceeds the character limit, a new message is started.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="text">The message to be sent.</param>
    /// <param name="isTTS">Determines whether the message should be read aloud by Discord or not.</param>
    /// <returns>A task that represents an asynchronous send operation for delivering the message. The task result contains the sent message.</returns>
    public static Task<IUserMessage[]> SendMessagesUnrolled(this IMessageChannel channel, string text, bool isTTS = false)
    {
      return Task.Run(async () =>
      {
        var messages = new List<IUserMessage>();
        while (text.Length > DiscordLimits.MESSAGE_TEXT_LIMIT)
        {
          text = text.Substring(0, DiscordLimits.MESSAGE_TEXT_LIMIT);
          messages.Add(await channel.SendMessageAsync(text, isTTS));
        }
        return messages.ToArray();
      });
    }

    /// <summary>
    /// Wrap the string in backticks.
    /// If the string contains an `, it is wrapped in ```.
    /// Otherwise, only one ` is used either side.
    /// </summary>
    public static string WrapInBackticks(this string str)
    {
      return str.Contains('`') ? $"```{str}```" : $"`{str}`";
    }

    /// <summary>
    /// Wrap the string in backticks if and only if it requires it.
    /// If the string contains an `, it is wrapped in ```.
    /// If the string contains an _ or *, or starts or ends with a space, it is wrapped in `.
    /// </summary>
    public static string SafeBackticks(this string str)
    {
      if (str.Length > 0)
      {
        if (str.Contains('`'))
        {
          return $"```{str}```";
        }
        else if (str.Contains('_') || str.Contains('*') || str[0] == ' ' || str[^1] == ' ')
        {
          return $"`{str}`";
        }
      }
      return str;
    }

    /// <summary>
    /// Append three backticks if they are unclosed.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string CloseBackticksIfUnclosed(this string str)
    {
      if ((str.Count("```") % 2) == 1)
      {
        str += "```";
      }
      return str;
    }

    /// <summary>
    /// Get the local timestamp tag representing a datetime.
    /// </summary>
    public static string GetLocalTimeInMarkdown(this DateTime dateTime)
    {
      return $"<t:{(dateTime - new DateTime(1970, 1, 1)).TotalSeconds}>";
    }

    /// <summary>
    /// Get the local timestamp tag representing the source's start time.
    /// </summary>
    public static string GetStartAsLocalTimeInMarkdown(this Source source)
    {
      return GetLocalTimeInMarkdown(source.Start);
    }

    /// <summary>
    /// Return a markdown link with the truncated source date if available,
    /// otherwise return its truncated_name only.
    /// </summary>
    public static string GetLinkedDateDisplay(this Source source)
    {
      var link = source.BattlefyUri;
      string text;
      if (source.Start != Builtins.UnknownDateTime)
      {
        text = source.Start.ToString("MMM yyyy");
      }
      else
      {
        text = source.StrippedTournamentName;
      }
      return string.IsNullOrEmpty(link) ? text : $"[{text}]({link})";
    }

    /// <summary>
    /// Return a markdown link with the truncated source name (date-name) if available,
    /// otherwise return its truncated_name only.
    /// </summary>
    public static string GetLinkedNameDisplay(this Source source)
    {
      var link = source.BattlefyUri;
      string text = source.StrippedTournamentName;
      return string.IsNullOrEmpty(link) ? text : $"[{text}]({link})";
    }

    public static IEmote ToEmote(this string react)
    {
      return string.IsNullOrWhiteSpace(react)
          ? throw new ArgumentNullException(nameof(react), "react cannot be null or whitespace.")
          : (react.StartsWith("<a:") || react.StartsWith("<:"))
          ? Emote.Parse(react)
          : new Emoji(react);
    }
  }
}
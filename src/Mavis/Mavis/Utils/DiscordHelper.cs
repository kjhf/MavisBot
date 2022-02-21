using Discord;
using SplatTagCore;
using System;
using System.Collections.Generic;

namespace Mavis.Utils
{
  public static class DiscordHelper
  {
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
      if (str.Contains('`'))
      {
        return $"```{str}```";
      }
      else if (str.Contains('_') || str.Contains('*') || str.StartsWith(' ') || str.EndsWith(' '))
      {
        return $"`{str}`";
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

    public static IEmote? ToEmote(this string react)
    {
      if (string.IsNullOrWhiteSpace(react)) return null;
      if (react.StartsWith("<a:") || react.StartsWith("<:"))
      {
        return Emote.Parse(react);
      }
      else
      {
        return new Emoji(react);
      }
    }
  }
}
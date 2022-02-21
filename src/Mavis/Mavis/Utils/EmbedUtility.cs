using Discord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mavis.Utils
{
  public static class EmbedUtility
  {
    /// <summary>
    /// Convert string to embed with an optional title and rgb colour
    /// </summary>
    /// <remarks>Forwarding method</remarks>
    public static EmbedBuilder ToEmbed(string str, int r, int g, int b, string? title = null)
    {
      return ToEmbed(str, new Color(r, g, b), title);
    }

    /// <summary>
    /// Convert string to embed with an optional title and colour
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public static EmbedBuilder ToEmbed(string? str = null, Color? color = null, string? title = null, string? imageURL = null)
    {
      if (str == null && imageURL == null)
      {
        throw new ArgumentNullException(nameof(str), $"{nameof(str)} and {nameof(imageURL)} cannot both be null.");
      }

      var builder = new EmbedBuilder();

      if (str != null)
      {
        builder = builder.WithDescription(str);
      }
      else if (imageURL != null)
      {
        builder = builder.WithDescription(imageURL);
      }

      if (color != null)
      {
        builder = builder.WithColor((Color)color);
      }

      if (imageURL != null)
      {
        builder = builder.WithImageUrl(imageURL);
      }

      if (title != null)
      {
        builder = builder.WithAuthor(author =>
        {
          author.WithName(title);
        });
      }
      return builder;
    }

    /// <summary>
    /// Append a list to the builder as strings that might append multiple fields.
    /// If an individual string would overflow the field, it will be truncated to fit.
    /// </summary>
    /// <param name="builder">The embed builder to append to.</param>
    /// <param name="fieldHeader">The title of the field.A count will be automatically added.</param>
    /// <param name="fieldValues">The list of strings to add to the field(s).</param>
    /// <param name="separator">The separator between strings. Newline by default.</param>
    /// <param name="maxUnrolls">The maximum number of fields to add. Will max out at <see cref="DiscordLimits.NUMBER_OF_FIELDS_LIMIT"/>.</param>
    public static EmbedBuilder AddUnrolledList(this EmbedBuilder builder, string fieldHeader, IReadOnlyList<string> fieldValues, string separator = "\n", int maxUnrolls = DiscordLimits.NUMBER_OF_FIELDS_LIMIT)
    {
      maxUnrolls = Math.Min(maxUnrolls, DiscordLimits.NUMBER_OF_FIELDS_LIMIT);
      if (fieldValues.Count > 0)
      {
        for (int batch = 0; batch < maxUnrolls; batch++)
        {
          var valuesLength = fieldValues.Count;
          string thisBatchMessage = string.Empty;
          int j = 0;
          for (j = 0; j < valuesLength; j++)
          {
            // Check if we'd overrun the field by adding this message.
            var singleToAdd = fieldValues[j] + separator;
            if (thisBatchMessage.Length + singleToAdd.Length >= DiscordLimits.FIELD_VALUE_LIMIT)
            {
              // Check if we're going to loop indefinitely
              if (j == 0)
              {
                // Bite the bullet and truncate-add, otherwise we'd get stuck
                thisBatchMessage += fieldValues[j].Truncate(DiscordLimits.FIELD_VALUE_LIMIT, "…" + separator);
              }
              break;
            }
            else
            {
              thisBatchMessage += fieldValues[j] + separator;
            }
          }
          fieldValues = fieldValues.Skip(Math.Min(valuesLength, j + 1)).ToArray();
          var noMore = fieldValues.Count <= 0;
          var onlyField = noMore && batch == 0;
          var header = onlyField ? $"{fieldHeader}:" : $"{fieldHeader} ({batch + 1}):";
          builder.AddField(
            name: header,
            value: thisBatchMessage.Truncate(DiscordLimits.FIELD_VALUE_LIMIT),
            inline: false
          );

          if (noMore)
          {
            break;
          }
        }
      }
      return builder;
    }
  }
}
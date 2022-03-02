using Discord;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mavis.Utils
{
  internal class MavisEmbedBuilder
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private readonly EmbedBuilder builder = new();
    private const int FIELD_VALUE_LIMIT_WITH_BACKTICK_CHECK = DiscordLimits.FIELD_VALUE_LIMIT - 3;

    /// <summary>
    /// Default/parameterless constructor.
    /// </summary>
    public MavisEmbedBuilder()
    {
      builder.WithDescription("");  // The description MUST be set
    }

    /// <summary>
    /// Construct the builder with a message (description), and optional colour.
    /// If <paramref name="messageIsImageUrl"/> is true, the ImageUrl is set to the message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="colour"></param>
    /// <param name="messageIsImageUrl"></param>
    public MavisEmbedBuilder(string message, Color? colour = null, bool messageIsImageUrl = false)
    {
      WithDescription(message);
      WithColor(colour);

      if (messageIsImageUrl)
      {
        WithImageUrl(message);
      }
    }

    /// <summary>
    /// Append a list to the builder as strings that might append multiple fields, but only if the list is not null or empty.
    /// If an individual string would overflow the field, it will be truncated to fit.
    /// </summary>
    /// <param name="fieldHeader">The title of the field. A count will be automatically added.</param>
    /// <param name="fieldValues">The list of strings to add to the field(s).</param>
    /// <param name="separator">The separator between strings. Newline by default.</param>
    /// <param name="maxUnrolls">The maximum number of fields to add. Will max out at <see cref="DiscordLimits.NUMBER_OF_FIELDS_LIMIT"/>.</param>
    public MavisEmbedBuilder ConditionallyAddUnrolledList(string fieldHeader, IReadOnlyList<string>? fieldValues, string separator = "\n", int maxUnrolls = DiscordLimits.NUMBER_OF_FIELDS_LIMIT)
    {
      if (fieldValues?.Count > 0)
      {
        this.AddUnrolledList(
          fieldHeader: fieldHeader,
          fieldValues: fieldValues,
          separator: separator,
          maxUnrolls: maxUnrolls
        );
      }
      return this;
    }

    /// <summary>
    /// Append a list to the builder as strings that might append multiple fields.
    /// If an individual string would overflow the field, it will be truncated to fit.
    /// </summary>
    /// <param name="fieldHeader">The title of the field. A count will be automatically added.</param>
    /// <param name="fieldValues">The list of strings to add to the field(s).</param>
    /// <param name="separator">The separator between strings. Newline by default.</param>
    /// <param name="maxUnrolls">The maximum number of fields to add. Will max out at <see cref="DiscordLimits.NUMBER_OF_FIELDS_LIMIT"/>.</param>
    public MavisEmbedBuilder AddUnrolledList(string fieldHeader, IReadOnlyList<string> fieldValues, string separator = "\n", int maxUnrolls = DiscordLimits.NUMBER_OF_FIELDS_LIMIT)
    {
      maxUnrolls = Math.Min(maxUnrolls, DiscordLimits.NUMBER_OF_FIELDS_LIMIT);
      if (fieldValues.Count > 0)
      {
        for (int batch = 0; batch < maxUnrolls; batch++)
        {
          var valuesLength = fieldValues.Count;
          string thisBatchMessage = string.Empty;
          int j;
          for (j = 0; j < valuesLength; j++)
          {
            // Check if we'd overrun the field by adding this message.
            var singleToAdd = fieldValues[j] + separator;
            if (thisBatchMessage.Length + singleToAdd.Length >= FIELD_VALUE_LIMIT_WITH_BACKTICK_CHECK)
            {
              // Check if we're going to loop indefinitely
              if (j == 0)
              {
                // Bite the bullet and truncate-add, otherwise we'd get stuck
                thisBatchMessage += fieldValues[j].Truncate(FIELD_VALUE_LIMIT_WITH_BACKTICK_CHECK, "…" + separator);
              }
              break;
            }
            else
            {
              thisBatchMessage += fieldValues[j] + separator;
            }
          }
          fieldValues = fieldValues.Skip(Math.Min(valuesLength, j + 1)).ToArray();
          var noMore = fieldValues.Count == 0;
          var onlyField = noMore && batch == 0;
          var header = onlyField ? $"{fieldHeader}:" : $"{fieldHeader} ({batch + 1}):";
          AddField(
            name: header,
            value: thisBatchMessage,
            inline: false
          );

          if (noMore)
          {
            break;
          }
        }
      }
      return this;
    }

    /// <summary>
    /// Adds an Discord.Embed field with the provided name and value.
    /// Titles and values here are truncated to fit in the <see cref="DiscordLimits.FIELD_NAME_LIMIT"/> and <see cref="DiscordLimits.FIELD_VALUE_LIMIT"/>.
    /// </summary>
    /// <param name="name">The title of the field.</param>
    /// <param name="value">The value of the field.</param>
    /// <param name="inline">Indicates whether the field is in-line or not.</param>
    /// <param name="defaultName">If the <paramref name="name"/> is null or empty, uses the default name instead.
    /// <param name="defaultValue">If the <paramref name="value"/> is null or empty, uses the default value instead.
    /// <param name="truncationString">If the name or value is truncated, this string is the indicator for this.
    /// This is as Discord does not allow field titles to be empty.</param>
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder AddField(
      string name,
      string value,
      bool inline = false,
      string defaultName = "Unnamed",
      string defaultValue = "(Nothing more to say)",
      string truncationString = "…")
    {
      builder.AddField(
        name.Or(defaultName).Truncate(DiscordLimits.FIELD_NAME_LIMIT, truncationString),
        value.Or(defaultValue).Truncate(FIELD_VALUE_LIMIT_WITH_BACKTICK_CHECK, truncationString).CloseBackticksIfUnclosed(),
        inline);
      return this;
    }

    /// <summary>
    /// Builds the Discord.Embed into a Rich Embed ready to be sent. This will build the embeds but returns the first only.
    /// This ensures the discord field limits are adhered to.
    /// </summary>
    /// <returns>The built embed object.</returns>
    /// <exception cref="InvalidOperationException">Any URL must include its protocols (i.e http:// or https://).</exception>
    public Embed BuildFirst()
    {
      return SmartBuild()[0];
    }

    /// <summary>
    /// Builds the Discord.Embed into one or more Rich Embeds ready to be sent.
    /// </summary>
    /// <returns>The built embed objects.</returns>
    /// <exception cref="InvalidOperationException">Any URL must include its protocols (i.e http:// or https://).</exception>
    public List<Embed> SmartBuild()
    {
      List<Embed> result = new();

      const int MAX_MESSAGES_TO_UNROLL = 10;
      var removedFields = new List<EmbedFieldBuilder>();
      int message = 1;

      // this.WithCurrentTimestamp(); // We don't need this :3
      var builder = this.builder;
      var colour = this.builder.Color.GetValueOrDefault();
      while (message < MAX_MESSAGES_TO_UNROLL)
      {
        // While the message is not in limits, or the removed fields has one field only (as the footer cannot be alone)
        while (builder.Length > DiscordLimits.TOTAL_CHARACTER_LIMIT
          || builder.Fields.Count > DiscordLimits.NUMBER_OF_FIELDS_LIMIT
          || removedFields.Count == 1)
        {
          log.Debug($"Looping builder: builder.Length={builder.Length}, builder.Fields.Count={builder.Fields.Count}, removedFields.Count={removedFields.Count}");
          // Take LIFO
          int index = builder.Fields.Count - 1;
          var removed = builder.Fields[index];
          builder.Fields.RemoveAt(index);
          removedFields.Add(removed);
        }

        // Don't think we should use the builder instance here - should smart build be a static/extension?
        if (builder.Length > 0)
        {
          result.Add(builder.Build());
          log.Debug($"Embed built: builder.Title={builder.Title} of length {builder.Length}");
        }

        if (removedFields.Count > 0)
        {
          message++;
          removedFields.Reverse();
          builder = new EmbedBuilder().WithTitle("Page " + message).WithColor(colour).WithDescription("");
          foreach (var field in removedFields)
          {
            builder.Fields.Add(field);
          }
          removedFields.Clear();
        }
        else
        {
          break;
        }
      }
      return result;
    }

    /// <summary>
    /// Sets the author field of an Discord.Embed with the provided name, icon URL, and URL.
    /// </summary>
    /// <param name="name">The title of the author field.</param>
    /// <param name="iconUrl">The icon URL of the author field.</param>
    /// <param name="url">The URL of the author field.</param>
    /// <param name="defaultName">If the <paramref name="name"/> is null or empty, uses the default name instead.
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder WithAuthor(string name, string? iconUrl = null, string? url = null, string defaultName = "Unnamed")
    {
      builder.WithAuthor(
        name.Or(defaultName).Truncate(DiscordLimits.AUTHOR_NAME_LIMIT),
        iconUrl,
        url);
      return this;
    }

    /// <summary>
    /// Sets the sidebar colour of an Discord.Embed.
    /// </summary>
    /// <param name="colour">The colour to be set.</param>
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder WithColor(Color colour)
    {
      builder.WithColor(colour);
      return this;
    }

    /// <summary>
    /// Sets the sidebar colour of an Discord.Embed.
    /// </summary>
    /// <param name="colour">The colour to be set.</param>
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder WithColor(Color? colour)
    {
      if (colour.HasValue)
      {
        builder.WithColor(colour.Value);
      }
      return this;
    }

    /// <summary>
    /// Adds embed colour based on the provided RGB System.Int32 value.
    /// </summary>
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder WithColor(int r, int g, int b)
    {
      builder.WithColor(r, g, b);
      return this;
    }

    /// <summary>
    /// Sets the timestamp of an Discord.Embed to the current time.
    /// </summary>
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder WithCurrentTimestamp()
    {
      builder.WithCurrentTimestamp();
      return this;
    }

    /// <summary>
    /// Sets the description of an Discord.Embed.
    /// </summary>
    /// <param name="description">The description to be set.</param>
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder WithDescription(string description)
    {
      builder.WithDescription(
        description.Truncate(DiscordLimits.DESCRIPTION_LIMIT)
      );
      return this;
    }

    /// <summary>
    /// Sets the footer field of an Discord.Embed with the provided name, icon URL.
    /// </summary>
    /// <param name="text">The title of the footer field.</param>
    /// <param name="iconUrl">The icon URL of the footer field.</param>
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder WithFooter(string text, string? iconUrl = null)
    {
      builder.WithFooter(
        text.Truncate(DiscordLimits.FOOTER_TEXT_LIMIT),
        iconUrl);
      return this;
    }

    /// <summary>
    /// Sets the image URL of an Discord.Embed.
    /// </summary>
    /// <param name="imageUrl">The image URL to be set.</param>
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder WithImageUrl(string imageUrl)
    {
      builder.WithImageUrl(imageUrl);
      return this;
    }

    /// <summary>
    /// Sets the thumbnail URL of an Discord.Embed.
    /// </summary>
    /// <param name="thumbnailUrl">The thumbnail URL to be set.</param>
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder WithThumbnailUrl(string thumbnailUrl)
    {
      builder.WithThumbnailUrl(thumbnailUrl);
      return this;
    }

    /// <summary>
    /// Sets the title of an Discord.Embed.
    /// </summary>
    /// <param name="title">The title to be set.</param>
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder WithTitle(string title)
    {
      builder.WithTitle(
        title.Truncate(DiscordLimits.TITLE_LIMIT)
      );
      return this;
    }

    /// <summary>
    /// Sets the URL of an Discord.Embed.
    /// </summary>
    /// <param name="url">The URL to be set.</param>
    /// <returns>The current builder.</returns>
    public MavisEmbedBuilder WithUrl(string url)
    {
      builder.WithUrl(url);
      return this;
    }
  }
}
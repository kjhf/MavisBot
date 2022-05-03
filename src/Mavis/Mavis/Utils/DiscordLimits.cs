using Discord;

namespace Mavis.Utils
{
  /// <summary>
  /// See https://discord.com/developers/docs/resources/channel#embed-limits-limits
  /// </summary>
  public static class DiscordLimits
  {
    public const int TITLE_LIMIT = EmbedBuilder.MaxTitleLength;
    public const int DESCRIPTION_LIMIT = EmbedBuilder.MaxDescriptionLength;
    public const int NUMBER_OF_FIELDS_LIMIT = EmbedBuilder.MaxFieldCount;
    public const int FIELD_NAME_LIMIT = EmbedFieldBuilder.MaxFieldNameLength;
    public const int FIELD_VALUE_LIMIT = EmbedFieldBuilder.MaxFieldValueLength;
    public const int FOOTER_TEXT_LIMIT = EmbedFooterBuilder.MaxFooterTextLength;
    public const int AUTHOR_NAME_LIMIT = EmbedAuthorBuilder.MaxAuthorNameLength;
    public const int MESSAGE_TEXT_LIMIT = 2000;
    public const int TOTAL_CHARACTER_LIMIT = EmbedBuilder.MaxEmbedLength;

    public const int MAX_EMBED_RESULTS = 20;
  }
}
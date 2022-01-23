using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavis.Utils
{
  /// <summary>
  /// See https://discord.com/developers/docs/resources/channel#embed-limits-limits
  /// </summary>
  public static class DiscordLimits
  {
    public const int TITLE_LIMIT = 256;
    public const int DESCRIPTION_LIMIT = 4096;
    public const int NUMBER_OF_FIELDS_LIMIT = 25;
    public const int FIELD_NAME_LIMIT = 256;
    public const int FIELD_VALUE_LIMIT = 1024;
    public const int FOOTER_TEXT_LIMIT = 2048;
    public const int AUTHOR_NAME_LIMIT = 256;
    public const int MESSAGE_TEXT_LIMIT = 2000;
    public const int TOTAL_CHARACTER_LIMIT = 6000;

    public const int MAX_EMBED_RESULTS = 20;
  }
}
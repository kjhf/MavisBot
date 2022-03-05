using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavis.Utils
{
  public static class StringExtensions
  {
    /// <summary>
    /// Make the first letter in specified string upper-case.
    /// </summary>
    /// <param name="str">String to capitalize.</param>
    /// <returns>Capitalized string.</returns>
    public static string Capitalize(this string str)
    {
      if (char.IsUpper(str[0]))
      {
        return str;
      }

      return char.ToUpper(str[0]) + str.Substring(1);
    }

    /// <summary>
    /// String comparison check on Contains overload
    /// </summary>
    /// <param name="source"></param>
    /// <param name="toCheck"></param>
    /// <param name="comp"></param>
    /// <returns></returns>
    public static bool Contains(this string source, string toCheck, StringComparison comp)
    {
      return source.IndexOf(toCheck, comp) >= 0;
    }

    /// <summary>
    /// Get the indexes of the character in the given string.
    /// </summary>
    public static IReadOnlyList<int> IndexOfAll(this string source, char toFind)
    {
      List<int> result = new();
      for (int i = 0; i < source.Length; i++)
      {
        char c = source[i];
        if (c.Equals(toFind))
        {
          result.Add(i);
        }
      }
      return result;
    }

    /// <summary>
    /// Get whether this string contains or nearly contains another string.
    /// </summary>
    /// <param name="s">Object string</param>
    /// <param name="other">Other string</param>
    /// <param name="comp">The comparison type</param>
    /// <param name="stringsToIgnore">Characters/Strings that are ignored as part of the comparison</param>
    public static bool ContainsIgnore(this string s, string other, StringComparison comp = StringComparison.OrdinalIgnoreCase, params string[] stringsToIgnore)
    {
      foreach (string c in stringsToIgnore)
      {
        s = s.Replace(c, "");
        other = other.Replace(c, "");
      }
      return s.Contains(other, comp);
    }

    /// <summary>
    /// Get whether this string equals or nearly equals another string:
    /// Case insensitive, ignores underscores, spaces, and hyphens.
    /// </summary>
    /// <param name="s">Object string</param>
    /// <param name="other">Other string</param>
    public static bool CloseEquals(this string s, string other)
    {
      return s.Replace(" ", "").Replace("_", "").Replace("-", "").Equals(other.Replace(" ", "").Replace("_", "").Replace("-", "").Replace(".", ""), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Escape characters in a string with the specified escape character(s).
    /// </summary>
    /// <param name="s">The string to escape</param>
    /// <param name="charactersToEscape">The characters that must be escaped</param>
    /// <param name="charactersToPrepend">The character to use for an escape</param>
    /// <returns>The escaped string</returns>
    public static string EscapeCharacters(this string s, string charactersToEscape = "\\", string charactersToPrepend = "\\")
    {
      foreach (var c in charactersToEscape)
      {
        s = s.Replace(c.ToString(), $"{charactersToPrepend}{c}");
      }
      return s;
    }

    public static char FlipCharacter(this char c)
    {
      switch (c)
      {
        case 'a': return 'ɐ';
        case 'ɐ': return 'a';
        case 'b': return 'q';
        case 'c': return 'ɔ';
        case 'ɔ': return 'c';
        case 'd': return 'p';
        case 'e': return 'ǝ';
        case 'ǝ': return 'e';
        case 'f': return 'ɟ';
        case 'ɟ': return 'f';
        case 'g': return 'b';
        case 'h': return 'ɥ';
        case 'ɥ': return 'h';
        case 'i': return 'ᴉ';
        case 'ᴉ': return 'i';
        case 'j': return 'ſ';
        case 'ſ': return 'j';
        case 'k': return 'ʞ';
        case 'ʞ': return 'k';
        case 'l': return 'ן';
        case 'ן': return 'l';
        case 'm': return 'ɯ';
        case 'ɯ': return 'm';
        case 'n': return 'u';
        case 'o': return 'o';
        case 'p': return 'd';
        case 'q': return 'b';
        case 'r': return 'ɹ';
        case 'ɹ': return 'r';
        case 's': return 's';
        case 't': return 'ʇ';
        case 'ʇ': return 't';
        case 'u': return 'n';
        case 'v': return 'ʌ';
        case 'w': return 'ʍ';
        case 'ʍ': return 'w';
        case 'x': return 'x';
        case 'y': return 'ʎ';
        case 'ʎ': return 'y';
        case 'z': return 'z';

        case 'A': return '∀';
        case '∀': return 'A';
        case 'B': return 'ᗺ';
        case 'ᗺ': return 'B';
        case 'C': return 'Ɔ';
        case 'Ɔ': return 'C';
        case 'D': return 'ᗡ';
        case 'ᗡ': return 'D';
        case 'E': return 'Ǝ';
        case 'Ǝ': return 'E';
        case 'F': return 'Ⅎ';
        case 'Ⅎ': return 'F';
        case 'G': return 'פ';
        case 'פ': return 'G';
        case 'H': return 'H';
        case 'I': return 'I';
        case 'J': return 'ſ';
        case 'K': return 'ʞ';
        case 'L': return '˥';
        case '˥': return 'L';
        case 'M': return 'W';
        case 'N': return 'N';
        case 'O': return 'O';
        case 'P': return 'Ԁ';
        case 'Ԁ': return 'P';
        case 'Q': return 'Ò';
        case 'Ó': return 'Q';
        case 'R': return 'ᴚ';
        case 'ᴚ': return 'R';
        case 'S': return 'S';
        case 'T': return '⊥';
        case '⊥': return 'T';
        case 'U': return '∩';
        case '∩': return 'U';
        case 'V': return 'Λ';
        case 'Λ': return 'V';
        case 'W': return 'M';
        case 'X': return 'X';
        case 'Y': return '⩑';
        case '⩑': return 'Y';
        case 'Z': return 'Z';

        case '0': return '0';
        case '1': return '1';
        case '2': return '2';
        case '3': return 'Ɛ';
        case 'Ɛ': return '3';
        case '4': return 'h';
        case '5': return '5';
        case '6': return '9';
        case '7': return 'L';
        case '8': return '8';
        case '9': return '6';

        case '`': return ',';
        case '¬': return '¬';
        case '¦': return '¦';
        case '!': return '¡';
        case '¡': return '!';
        case '"': return '"';
        case '£': return '3';
        case '$': return '$';
        case '%': return '%';
        case '^': return 'v';
        case '&': return '&';
        case '*': return '.';
        case '(': return ')';
        case ')': return '(';
        case '-': return '-';
        case '=': return '=';
        case '_': return '‾';
        case '+': return '+';
        case '\\': return '\\';
        case '|': return '|';
        case '[': return ']';
        case ']': return '[';
        case '{': return '}';
        case '}': return '{';
        case ':': return ':';
        case ';': return '؛';
        case '؛': return ';';
        case '@': return '@';
        case '\'': return ',';
        case '#': return '#';
        case '~': return '~';
        case '<': return '>';
        case '>': return '<';
        case ',': return '`';
        case '.': return '˙';
        case '/': return '/';
        case '?': return '¿';
        case '¿': return '?';

        case 'ʖ': return 'Ç';
        case 'Ç': return 'ʖ';
        case 'ç': return 'ʖ';
        default: return c;
      }
    }

    public static string FlipString(this string input)
    {
      var flip = new StringBuilder(input.Length);
      for (int i = (input.Length - 1); i >= 0; i--)
      {
        flip.Append(FlipCharacter(input[i]));
      }
      return (flip.ToString());
    }

    /// <summary>
    /// Removes any character in a string not matching the predicate.
    /// Case sensitive.
    /// </summary>
    /// <param name="s">Object string</param>
    /// <param name="pred">Predicate determining characters to keep</param>
    public static string KeepAny(this string s, Predicate<char> pred)
    {
      var sb = new StringBuilder();
      foreach (char c in s)
      {
        if (pred(c))
        {
          sb.Append(c);
        }
      }
      return sb.ToString();
    }

    /// <summary>
    /// Make a string camelCase
    /// </summary>
    /// <param name="str">Object string</param>
    public static string ToCamelCase(this string str)
    {
      if (string.IsNullOrWhiteSpace(str))
      {
        return str;
      }

      var sb = new StringBuilder();
      var words = str.Split(' ');

      for (int i = 0; i < words.Length; i++)
      {
        var word = words[i];
        if (string.IsNullOrWhiteSpace(word))
        {
          continue;
        }

        if (i == 0)
        {
          // Make the first letter of the word lower-case.
          sb.Append(word.Substring(0, 1).ToLower());
        }
        else
        {
          // Make the first letter of the word upper-case.
          sb.Append(word.Substring(0, 1).ToUpper());
        }

        // And keep the casing for the other letters.
        sb.Append(word.Substring(1));
      }

      return sb.ToString();
    }

    /// <summary>
    /// Make a string kebab-case
    /// </summary>
    public static string ToKebabCase(this string str)
    {
      if (string.IsNullOrEmpty(str)) return str;
      if (str.Length < 2) return str.ToLowerInvariant();

      var sb = new StringBuilder();
      sb.Append(char.ToLowerInvariant(str[0]));
      for (int i = 1; i < str.Length; i++)
      {
        // If capital, prepend a hyphen.
        if (char.IsUpper(str[i]))
        {
          sb.Append('-');
        }

        // If space, replace with a hyphen, otherwise add the character (lower-case).
        if (char.IsWhiteSpace(str[i]))
        {
          sb.Append('-');
        }
        else
        {
          sb.Append(char.ToLowerInvariant(str[i]));
        }
      }
      return sb.ToString().Trim();
    }

    /// <summary>
    /// Make a string MACRO_CASE
    /// </summary>
    public static string ToMacroCase(this string str)
    {
      if (string.IsNullOrEmpty(str)) return str;
      if (str.Length < 2) return str.ToUpperInvariant();

      var sb = new StringBuilder();
      sb.Append(char.ToUpperInvariant(str[0]));
      for (int i = 1; i < str.Length; i++)
      {
        // If capital, prepend an underscore.
        if (char.IsUpper(str[i]))
        {
          sb.Append('_');
        }

        // If space, replace with an underscore, otherwise add the character (upper-case).
        if (char.IsWhiteSpace(str[i]))
        {
          sb.Append('_');
        }
        else
        {
          sb.Append(char.ToUpperInvariant(str[i]));
        }
      }
      return sb.ToString().Trim();
    }

    /// <summary>
    /// Make a string PascalCase
    /// </summary>
    /// <param name="str">Object string</param>
    public static string ToPascalCase(this string str)
    {
      if (string.IsNullOrWhiteSpace(str))
      {
        return str;
      }

      var sb = new StringBuilder();
      foreach (var word in str.Split(' '))
      {
        if (string.IsNullOrWhiteSpace(word))
        {
          continue;
        }

        // Make the first letter of the word upper-case.
        sb.Append(word.Substring(0, 1).ToUpper());

        // And keep the casing for the other letters.
        sb.Append(word.Substring(1));
      }
      return sb.ToString();
    }

    /// <summary>
    /// Make a string snake_case
    /// </summary>
    public static string ToSnakeCase(this string str)
    {
      if (string.IsNullOrEmpty(str)) return str;
      if (str.Length < 2) return str.ToLowerInvariant();

      var sb = new StringBuilder();
      sb.Append(char.ToLowerInvariant(str[0]));
      for (int i = 1; i < str.Length; i++)
      {
        // If capital, prepend a hyphen.
        if (char.IsUpper(str[i]))
        {
          sb.Append('_');
        }

        // If space, replace with a hyphen, otherwise add the character (lower-case).
        if (char.IsWhiteSpace(str[i]))
        {
          sb.Append('_');
        }
        else
        {
          sb.Append(char.ToLowerInvariant(str[i]));
        }
      }
      return sb.ToString().Trim();
    }

    /// <summary>
    /// Make A String Title Case
    /// </summary>
    public static string ToTitleCase(this string str)
    {
      if (string.IsNullOrEmpty(str)) return str;
      if (str.Length < 2) return str.ToUpper();

      var sb = new StringBuilder();
      sb.Append(char.ToUpper(str[0]));
      for (int i = 1; i < str.Length; i++)
      {
        // If capital, prepend a space.
        if (char.IsUpper(str[i]))
        {
          sb.Append(' ');
        }
        sb.Append(str[i]);
      }
      return sb.ToString().Trim();
    }

    /// <summary>
    /// Removes any of the specified characters.
    /// Case sensitive.
    /// </summary>
    /// <param name="s">Object string</param>
    /// <param name="charsToRemove">Characters to remove</param>
    public static string RemoveAny(this string s, params char[] charsToRemove)
    {
      foreach (char c in charsToRemove)
      {
        s = s.Replace(c.ToString(), "");
      }
      return s;
    }

    /// <summary>
    /// Removes any character in a string matching the predicate.
    /// Case sensitive.
    /// </summary>
    /// <param name="s">Object string</param>
    /// <param name="pred">Predicate determining characters to remove</param>
    public static string RemoveAny(this string s, Predicate<char> pred)
    {
      var sb = new StringBuilder();
      foreach (char c in s)
      {
        if (!pred(c))
        {
          sb.Append(c);
        }
      }
      return sb.ToString();
    }

    /// <summary>
    /// Replaces a value in a string with a new value. Extends the <see cref="string.Replace(string, string)"/> functionality
    /// by adding a <see cref="StringComparison"/> parameter.
    /// </summary>
    /// <param name="s"></param>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    public static string Replace(this string s, string oldValue, string newValue, StringComparison comparison)
    {
      // Null checks
      if (s == null)
      {
        throw new ArgumentNullException(nameof(s));
      }

      if (oldValue == null)
      {
        throw new ArgumentNullException(nameof(oldValue));
      }

      if (newValue == null)
      {
        throw new ArgumentNullException(nameof(newValue));
      }

      // Trivial cases
      if (string.Equals(oldValue, newValue, comparison))
      {
        // Skip.
        return s;
      }

      int index = s.IndexOf(oldValue, comparison);
      if (index == -1)
      {
        // Match not found, skip.
        return s;
      }

      // Replacement
      var sb = new StringBuilder();
      int previousIndex = 0;
      while (index != -1)
      {
        sb.Append(s.Substring(previousIndex, index - previousIndex));
        sb.Append(newValue);
        index += oldValue.Length;

        previousIndex = index;
        index = s.IndexOf(oldValue, index, comparison);
      }
      sb.Append(s.Substring(previousIndex));

      return sb.ToString();
    }

    /// <summary>
    /// Returns a string array that contains the substrings in this string that are
    /// delimited by elements of a specified string. A parameter specifies
    /// whether to return empty array elements.
    /// </summary>
    /// <param name="data">Source string to split</param>
    /// <param name="delimiter">String that delimits the data string.</param>
    /// <param name="options">
    /// System.StringSplitOptions.RemoveEmptyEntries to omit empty
    /// array elements from the array returned; or System.StringSplitOptions.None
    /// to include empty array elements in the array returned.
    /// </param>
    /// <returns>
    /// An array whose elements contain the substrings in this string.
    /// </returns>
    /// <exception cref="System.ArgumentException">options is not one of the System.StringSplitOptions values.</exception>
    public static string[] Split(this string data, string delimiter, StringSplitOptions options = StringSplitOptions.None)
    {
      return data.Split(new string[] { delimiter }, options);
    }

    public static string StripAccents(this string s)
    {
      StringBuilder sb = new StringBuilder(s);
      sb
        .Replace('ç', 'c')
        .Replace('Ç', 'C')
        .Replace('é', 'e')
        .Replace('É', 'e')
        .Replace('â', 'a')
        .Replace('Â', 'a')
        .Replace('ê', 'e')
        .Replace('Ê', 'e')
        .Replace('î', 'i')
        .Replace('Î', 'i')
        .Replace('ô', 'o')
        .Replace('Ô', 'o')
        .Replace('û', 'u')
        .Replace('Û', 'u')
        .Replace('à', 'a')
        .Replace('À', 'a')
        .Replace('è', 'e')
        .Replace('È', 'e')
        .Replace('ù', 'u')
        .Replace('Ù', 'u')
        .Replace('ë', 'e')
        .Replace('Ë', 'e')
        .Replace('ï', 'i')
        .Replace('Ï', 'i')
        .Replace('ü', 'u')
        .Replace('Ü', 'u')

        ;
      return sb.ToString();
    }

    /// <summary>
    /// Centres a string given a total length.
    /// </summary>
    /// <param name="theString">The string to centre</param>
    /// <param name="totalLength">Total length</param>
    /// <param name="paddingChar">Character to pad with, space by default.</param>
    /// <returns></returns>
    public static string CentreString(this string theString, int totalLength, char paddingChar = ' ')
    {
      return theString.PadLeft(((totalLength - theString.Length) / 2) + theString.Length, paddingChar).PadRight(totalLength, paddingChar);
    }

    /// <summary>
    /// Truncates a string up to maximum length
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maximumLength">The maximum length of the string (inclusive)</param>
    /// <param name="truncationIndicator">String to append if truncation has occurred, e.g. ... or …</param>
    /// <returns></returns>
    public static string Truncate(this string value, int maximumLength, string truncationIndicator = "…")
    {
      if (value == null)
      {
        throw new ArgumentNullException(nameof(value));
      }

      if (truncationIndicator.Length > maximumLength)
      {
        throw new ArgumentException("Truncation length cannot be greater than the maximum length of the string.", nameof(maximumLength));
      }

      // If we don't need to do anything...
      if ((string.IsNullOrEmpty(value)) || (value.Length <= maximumLength))
      {
        // Skip.
        return value;
      }

      // Substring up to the maximum length and add on the truncation indicator.
      return value.Substring(0, maximumLength - truncationIndicator.Length) + truncationIndicator;
    }

    /// <summary>
    /// Returns a default string if the prior string is null or empty.
    /// </summary>
    [return: NotNullIfNotNull("@default")]
    [return: NotNullIfNotNull("str")]
    public static string? Or(this string? str, string @default)
      => string.IsNullOrEmpty(str) ? @default : str;

    /// <summary>
    /// Returns the instance string that is prefixed with <paramref name="prefix"/> and suffixed with <paramref name="suffix"/>, if the instance string is not null or empty.
    /// Otherwise, returns <paramref name="default"/>.
    /// </summary>
    [return: NotNullIfNotNull("@default")]
    [return: NotNullIfNotNull("str")]
    public static string? ConditionalString(this string? str, string prefix = "", string suffix = "", string? @default = "")
      => string.IsNullOrEmpty(str) ? @default : prefix + str + suffix;

    /// <summary>
    /// Append a plural suffix to a string if the <paramref name="collection"/>'s length is not 1.
    /// </summary>
    public static string Plural<T>(this string str, IReadOnlyCollection<T> collection, string suffix = "s")
      => Plural(str, collection.Count, suffix);

    /// <summary>
    /// Append a plural suffix to a string if the <paramref name="count"/> is not 1.
    /// </summary>
    public static string Plural(this string str, int count, string suffix = "s")
      => count == 1 ? str : str + suffix;
  }
}
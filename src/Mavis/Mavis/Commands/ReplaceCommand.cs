using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  record ReplaceSubCommand
  {
    public readonly Dictionary<string, string> map;
    public readonly bool ignoreCase;
    public readonly bool reverse;
    public readonly bool isPrivate;
    public readonly SlashCommandBuilder subCommand;

    public ReplaceSubCommand(SlashCommandBuilder subCommand, bool ignoreCase, bool reverse, bool isPrivate, Dictionary<string, string> map)
    {
      this.map = map;
      this.isPrivate = isPrivate;
      this.ignoreCase = ignoreCase;
      this.reverse = reverse;
      this.subCommand = subCommand;
    }
  }

  public class ReplaceCommand : IMavisMultipleCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, ReplaceSubCommand> subCommands = new();

    public ReplaceCommand()
    {
      List<ReplaceSubCommand> toAdd = new();
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("black-bubble").WithDescription("Replaces letters with black bubble letters").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: true, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "🅐" },
            { "b", "🅑" },
            { "c", "🅒" },
            { "d", "🅓" },
            { "e", "🅔" },
            { "f", "🅕" },
            { "g", "🅖" },
            { "h", "🅗" },
            { "i", "🅘" },
            { "j", "🅙" },
            { "k", "🅚" },
            { "l", "🅛" },
            { "m", "🅜" },
            { "n", "🅝" },
            { "o", "🅞" },
            { "p", "🅟" },
            { "q", "🅠" },
            { "r", "🅡" },
            { "s", "🅢" },
            { "t", "🅣" },
            { "u", "🅤" },
            { "v", "🅥" },
            { "w", "🅦" },
            { "x", "🅧" },
            { "y", "🅨" },
            { "z", "🅩" },
            { "0", "🄌" },
            { "1", "➊" },
            { "2", "➋" },
            { "3", "➌" },
            { "4", "➍" },
            { "5", "➎" },
            { "6", "➏" },
            { "7", "➐" },
            { "8", "➑" },
            { "9", "➒" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("black-square").WithDescription("Replaces letters with black square letters").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: true, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "🅰" },
            { "b", "🅱" },
            { "c", "🅲" },
            { "d", "🅳" },
            { "e", "🅴" },
            { "f", "🅵" },
            { "g", "🅶" },
            { "h", "🅷" },
            { "i", "🅸" },
            { "j", "🅹" },
            { "k", "🅺" },
            { "l", "🅻" },
            { "m", "🅼" },
            { "n", "🅽" },
            { "o", "🅾" },
            { "p", "🅿" },
            { "q", "🆀" },
            { "r", "🆁" },
            { "s", "🆂" },
            { "t", "🆃" },
            { "u", "🆄" },
            { "v", "🆅" },
            { "w", "🆆" },
            { "x", "🆇" },
            { "y", "🆈" },
            { "z", "🆉" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("braille").WithDescription("Replaces letters with Braille markings").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: true, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "⠁" },
            { "b", "⠃" },
            { "c", "⠉" },
            { "d", "⠙" },
            { "e", "⠑" },
            { "f", "⠋" },
            { "g", "⠛" },
            { "h", "⠓" },
            { "i", "⠊" },
            { "j", "⠚" },
            { "k", "⠅" },
            { "l", "⠇" },
            { "m", "⠍" },
            { "n", "⠝" },
            { "o", "⠕" },
            { "p", "⠏" },
            { "q", "⠟" },
            { "r", "⠗" },
            { "s", "⠎" },
            { "t", "⠞" },
            { "u", "⠥" },
            { "v", "⠧" },
            { "w", "⠺" },
            { "x", "⠭" },
            { "y", "⠽" },
            { "z", "⠵" },
            { "0", "⠼⠚" },
            { "1", "⠼⠁" },
            { "2", "⠼⠃" },
            { "3", "⠼⠉" },
            { "4", "⠼⠙" },
            { "5", "⠼⠑" },
            { "6", "⠼⠋" },
            { "7", "⠼⠛" },
            { "8", "⠼⠓" },
            { "9", "⠼⠊" },
            { "A", "⠠⠁" },
            { "B", "⠠⠃" },
            { "C", "⠠⠉" },
            { "D", "⠠⠙" },
            { "E", "⠠⠑" },
            { "F", "⠠⠋" },
            { "G", "⠠⠛" },
            { "H", "⠠⠓" },
            { "I", "⠠⠊" },
            { "J", "⠠⠚" },
            { "K", "⠠⠅" },
            { "L", "⠠⠇" },
            { "M", "⠠⠍" },
            { "N", "⠠⠝" },
            { "O", "⠠⠕" },
            { "P", "⠠⠏" },
            { "Q", "⠠⠟" },
            { "R", "⠠⠗" },
            { "S", "⠠⠎" },
            { "T", "⠠⠞" },
            { "U", "⠠⠥" },
            { "V", "⠠⠧" },
            { "W", "⠠⠺" },
            { "X", "⠠⠭" },
            { "Y", "⠠⠽" },
            { "Z", "⠠⠵" },
            { ",", "⠂" },
            { ";", "⠆" },
            { ":", "⠒" },
            { ".", "⠲" },
            { "?", "⠦" },
            { "!", "⠖" },
            { "'", "⠄" },
            { "\"", "⠄⠶" },
            { "(", "⠐⠣" },
            { ")", "⠐⠜" },
            { "/", "⠸⠌" },
            { "\\", "⠸⠡" },
            { "-", "⠤" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("bubble").WithDescription("Replaces letters with bubble letters").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: true, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "ⓐ" },
            { "b", "ⓑ" },
            { "c", "ⓒ" },
            { "d", "ⓓ" },
            { "e", "ⓔ" },
            { "f", "ⓕ" },
            { "g", "ⓖ" },
            { "h", "ⓗ" },
            { "i", "ⓘ" },
            { "j", "ⓙ" },
            { "k", "ⓚ" },
            { "l", "ⓛ" },
            { "m", "ⓜ" },
            { "n", "ⓝ" },
            { "o", "ⓞ" },
            { "p", "ⓟ" },
            { "q", "ⓠ" },
            { "r", "ⓡ" },
            { "s", "ⓢ" },
            { "t", "ⓣ" },
            { "u", "ⓤ" },
            { "v", "ⓥ" },
            { "w", "ⓦ" },
            { "x", "ⓧ" },
            { "y", "ⓨ" },
            { "z", "ⓩ " },
            { "0", "Ⓞ" },
            { "1", "①" },
            { "2", "②" },
            { "3", "③" },
            { "4", "④" },
            { "5", "⑤" },
            { "6", "⑥" },
            { "7", "⑦" },
            { "8", "⑧" },
            { "9", "⑨" },
            { ".", "⊙" },
            { "*", "⊛" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("double-struck").WithDescription("Replaces letters with double-struck letters").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: false, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "𝕒" },
            { "b", "𝕓" },
            { "c", "𝕔" },
            { "d", "𝕕" },
            { "e", "𝕖" },
            { "f", "𝕗" },
            { "g", "𝕘" },
            { "h", "𝕙" },
            { "i", "𝕚" },
            { "j", "𝕛" },
            { "k", "𝕜" },
            { "l", "𝕝" },
            { "m", "𝕞" },
            { "n", "𝕟" },
            { "o", "𝕠" },
            { "p", "𝕡" },
            { "q", "𝕢" },
            { "r", "𝕣" },
            { "s", "𝕤" },
            { "t", "𝕥" },
            { "u", "𝕦" },
            { "v", "𝕧" },
            { "w", "𝕨" },
            { "x", "𝕩" },
            { "y", "𝕪" },
            { "z", "𝕫" },
            { "0", "𝟘" },
            { "1", "𝟙" },
            { "2", "𝟚" },
            { "3", "𝟛" },
            { "4", "𝟜" },
            { "5", "𝟝" },
            { "6", "𝟞" },
            { "7", "𝟟" },
            { "8", "𝟠" },
            { "9", "𝟡" },
            { "A", "𝔸" },
            { "B", "𝔹" },
            { "C", "ℂ" },
            { "D", "𝔻" },
            { "E", "𝔼" },
            { "F", "𝔽" },
            { "G", "𝔾" },
            { "H", "ℍ" },
            { "I", "𝕀" },
            { "J", "𝕁" },
            { "K", "𝕂" },
            { "L", "𝕃" },
            { "M", "𝕄" },
            { "N", "ℕ" },
            { "O", "𝕆" },
            { "P", "ℙ" },
            { "Q", "ℚ" },
            { "R", "ℝ" },
            { "S", "𝕊" },
            { "T", "𝕋" },
            { "U", "𝕌" },
            { "V", "𝕍" },
            { "W", "𝕎" },
            { "X", "𝕏" },
            { "Y", "𝕐" },
            { "Z", "ℤ" },
            { "[", "『" },
            { "]", "』" },
            { "?", "❔" },
            { "!", "❕" },
            { ".", "⚬" },
            { "{", "⦃" },
            { "}", "⦄" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("fancy").WithDescription("Replaces letters with fancy letters").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: false, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "𝓪" },
            { "b", "𝓫" },
            { "c", "𝓬" },
            { "d", "𝓭" },
            { "e", "𝓮" },
            { "f", "𝓯" },
            { "g", "𝓰" },
            { "h", "𝓱" },
            { "i", "𝓲" },
            { "j", "𝓳" },
            { "k", "𝓴" },
            { "l", "𝓵" },
            { "m", "𝓶" },
            { "n", "𝓷" },
            { "o", "𝓸" },
            { "p", "𝓹" },
            { "q", "𝓺" },
            { "r", "𝓻" },
            { "s", "𝓼" },
            { "t", "𝓽" },
            { "u", "𝓾" },
            { "v", "𝓿" },
            { "w", "𝔀" },
            { "x", "𝔁" },
            { "y", "𝔂" },
            { "z", "𝔃" },
            { "A", "𝓐" },
            { "B", "𝓑" },
            { "C", "𝓒" },
            { "D", "𝓓" },
            { "E", "𝓔" },
            { "F", "𝓕" },
            { "G", "𝓖" },
            { "H", "𝓗" },
            { "I", "𝓘" },
            { "J", "𝓙" },
            { "K", "𝓚" },
            { "L", "𝓛" },
            { "M", "𝓜" },
            { "N", "𝓝" },
            { "O", "𝓞" },
            { "P", "𝓟" },
            { "Q", "𝓠" },
            { "R", "𝓡" },
            { "S", "𝓢" },
            { "T", "𝓣" },
            { "U", "𝓤" },
            { "V", "𝓥" },
            { "W", "𝓦" },
            { "X", "𝓧" },
            { "Y", "𝓨" },
            { "Z", "𝓩" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("flip").WithDescription("Replaces letters with flipped letters").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: false, reverse: true, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "ɐ" },
            { "ɐ", "a" },
            { "b", "q" },
            { "c", "ɔ" },
            { "ɔ", "c" },
            { "d", "p" },
            { "e", "ǝ" },
            { "ǝ", "e" },
            { "f", "ɟ" },
            { "ɟ", "f" },
            { "g", "b" },
            { "h", "ɥ" },
            { "ɥ", "h" },
            { "i", "ᴉ" },
            { "ᴉ", "i" },
            { "j", "ſ" },
            { "ſ", "j" },
            { "k", "ʞ" },
            { "ʞ", "k" },
            { "l", "ן" },
            { "ן", "l" },
            { "m", "ɯ" },
            { "ɯ", "m" },
            { "n", "u" },
            { "o", "o" },
            { "p", "d" },
            { "q", "b" },
            { "r", "ɹ" },
            { "ɹ", "r" },
            { "s", "s" },
            { "t", "ʇ" },
            { "ʇ", "t" },
            { "u", "n" },
            { "v", "ʌ" },
            { "w", "ʍ" },
            { "ʍ", "w" },
            { "x", "x" },
            { "y", "ʎ" },
            { "ʎ", "y" },
            { "z", "z" },
            { "A", "∀" },
            { "∀", "A" },
            { "B", "ᗺ" },
            { "ᗺ", "B" },
            { "C", "Ɔ" },
            { "Ɔ", "C" },
            { "D", "ᗡ" },
            { "ᗡ", "D" },
            { "E", "Ǝ" },
            { "Ǝ", "E" },
            { "F", "Ⅎ" },
            { "Ⅎ", "F" },
            { "G", "פ" },
            { "פ", "G" },
            { "H", "H" },
            { "I", "I" },
            { "J", "ſ" },
            { "K", "ʞ" },
            { "L", "˥" },
            { "˥", "L" },
            { "M", "W" },
            { "N", "N" },
            { "O", "O" },
            { "P", "Ԁ" },
            { "Ԁ", "P" },
            { "Q", "Ò" },
            { "Ó", "Q" },
            { "R", "ᴚ" },
            { "ᴚ", "R" },
            { "S", "S" },
            { "T", "⊥" },
            { "⊥", "T" },
            { "U", "∩" },
            { "∩", "U" },
            { "V", "Λ" },
            { "Λ", "V" },
            { "W", "M" },
            { "X", "X" },
            { "Y", "⩑" },
            { "⩑", "Y" },
            { "Z", "Z" },
            { "0", "0" },
            { "1", "1" },
            { "2", "2" },
            { "3", "Ɛ" },
            { "Ɛ", "3" },
            { "4", "h" },
            { "5", "5" },
            { "6", "9" },
            { "7", "L" },
            { "8", "8" },
            { "9", "6" },
            { "`", "," },
            { "¬", "¬" },
            { "¦", "¦" },
            { "!", "¡" },
            { "¡", "!" },
            { "\"", "\"" },
            { "£", "3" },
            { "$", "$" },
            { "%", "%" },
            { "^", "v" },
            { "&", "&" },
            { "*", "." },
            { "(", ")" },
            { ")", "(" },
            { "-", "-" },
            { "=", "=" },
            { "_", "‾" },
            { "+", "+" },
            { "\\", "\\" },
            { "|", "|" },
            { "[", "]" },
            { "]", "[" },
            { "{", "}" },
            { "}", "{" },
            { ":", ":" },
            { ";", "؛" },
            { "؛", "\'" },
            { "@", "@" },
            { "'", "," },
            { "#", "#" },
            { "~", "~" },
            { "<", ">" },
            { ">", "<" },
            { ",", "`" },
            { ".", "˙" },
            { "/", "/" },
            { "?", "¿" },
            { "¿", "?" },
            { "ʖ", "Ç" },
            { "Ç", "ʖ" },
            { "ç", "ʖ" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("full-width").WithDescription("Replaces letters with full-width letters").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: false, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "ａ" },
            { "b", "ｂ" },
            { "c", "ｃ" },
            { "d", "ｄ" },
            { "e", "ｅ" },
            { "f", "ｆ" },
            { "g", "ｇ" },
            { "h", "ｈ" },
            { "i", "ｉ" },
            { "j", "ｊ" },
            { "k", "ｋ" },
            { "l", "ｌ" },
            { "m", "ｍ" },
            { "n", "ｎ" },
            { "o", "ｏ" },
            { "p", "ｐ" },
            { "q", "ｑ" },
            { "r", "ｒ" },
            { "s", "ｓ" },
            { "t", "ｔ" },
            { "u", "ｕ" },
            { "v", "ｖ" },
            { "w", "ｗ" },
            { "x", "ｘ" },
            { "y", "ｙ" },
            { "z", "ｚ" },
            { "A", "Ａ" },
            { "B", "Ｂ" },
            { "C", "Ｃ" },
            { "D", "Ｄ" },
            { "E", "Ｅ" },
            { "F", "Ｆ" },
            { "G", "Ｇ" },
            { "H", "Ｈ" },
            { "I", "Ｉ" },
            { "J", "Ｊ" },
            { "K", "Ｋ" },
            { "L", "Ｌ" },
            { "M", "Ｍ" },
            { "N", "Ｎ" },
            { "O", "Ｏ" },
            { "P", "Ｐ" },
            { "Q", "Ｑ" },
            { "R", "Ｒ" },
            { "S", "Ｓ" },
            { "T", "Ｔ" },
            { "U", "Ｕ" },
            { "V", "Ｖ" },
            { "W", "Ｗ" },
            { "X", "Ｘ" },
            { "Y", "Ｙ" },
            { "Z", "Ｚ" },
            { "?", "？" },
            { "0", "０" },
            { "1", "１" },
            { "2", "２" },
            { "3", "３" },
            { "4", "４" },
            { "5", "５" },
            { "6", "６" },
            { "7", "７" },
            { "8", "８" },
            { "9", "９" },
            { "!", "！" },
            { "*", "＊" },
            { ".", "." },
            { ",", "," },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("gothic").WithDescription("Replaces letters with gothic letters").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: false, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "𝔞" },
            { "b", "𝔟" },
            { "c", "𝔠" },
            { "d", "𝔡" },
            { "e", "𝔢" },
            { "f", "𝔣" },
            { "g", "𝔤" },
            { "h", "𝔥" },
            { "i", "𝔦" },
            { "j", "𝔧" },
            { "k", "𝔨" },
            { "l", "𝔩" },
            { "m", "𝔪" },
            { "n", "𝔫" },
            { "o", "𝔬" },
            { "p", "𝔭" },
            { "q", "𝔮" },
            { "r", "𝔯" },
            { "s", "𝔰" },
            { "t", "𝔱" },
            { "u", "𝔲" },
            { "v", "𝔳" },
            { "w", "𝔴" },
            { "x", "𝔵" },
            { "y", "𝔶" },
            { "z", "𝔷" },
            { "A", "𝕬" },
            { "B", "𝕭" },
            { "C", "𝕮" },
            { "D", "𝕯" },
            { "E", "𝕰" },
            { "F", "𝕱" },
            { "G", "𝕲" },
            { "H", "𝕳" },
            { "I", "𝕴" },
            { "J", "𝕵" },
            { "K", "𝕶" },
            { "L", "𝕷" },
            { "M", "𝕸" },
            { "N", "𝔑" },
            { "O", "𝕺" },
            { "P", "𝕻" },
            { "Q", "𝕼" },
            { "R", "𝕽" },
            { "S", "𝕾" },
            { "T", "𝕿" },
            { "U", "𝖀" },
            { "V", "𝖁" },
            { "W", "𝖂" },
            { "X", "𝖃" },
            { "Y", "𝖄" },
            { "Z", "𝖅" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("l33t").WithDescription("Replaces letters with l33tspeek").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: true, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "4" },
            { "b", "8" },
            { "c", "<" },
            { "d", "|>" },
            { "e", "3" },
            { "f", "|=" },
            { "g", "9" },
            { "h", "4" },
            { "i", "1" },
            { "j", "1" },
            { "k", "|<" },
            { "l", "|ˍ" },
            { "m", "⁄\\⁄\\" },
            { "n", "|\\|" },
            { "o", "0" },
            { "p", "ρ" },
            { "q", "9" },
            { "r", "|2" },
            { "s", "5" },
            { "t", "7" },
            { "u", "|ˍ|" },
            { "v", "\\⁄" },
            { "w", "\\⁄\\⁄" },
            { "x", "><" },
            { "y", "'⁄" },
            { "z", "̄⁄ˍ" },
            { "?", ":grey_question:" },
            { "!", ":grey_exclamation:" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("square").WithDescription("Replaces letters with square letters").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: true, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "🄰" },
            { "b", "🄱" },
            { "c", "🄲" },
            { "d", "🄳" },
            { "e", "🄴" },
            { "f", "🄵" },
            { "g", "🄶" },
            { "h", "🄷" },
            { "i", "🄸" },
            { "j", "🄹" },
            { "k", "🄺" },
            { "l", "🄻" },
            { "m", "🄼" },
            { "n", "🄽" },
            { "o", "🄾" },
            { "p", "🄿" },
            { "q", "🅀" },
            { "r", "🅁" },
            { "s", "🅂" },
            { "t", "🅃" },
            { "u", "🅄" },
            { "v", "🅅" },
            { "w", "🅆" },
            { "x", "🅇" },
            { "y", "🅈" },
            { "z", "🅉" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("text-to-emoji").WithDescription("Replaces text with a regional indicator emoji").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: true, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "🇦 " },
            { "b", "🇧 " },
            { "c", "🇨 " },
            { "d", "🇩 " },
            { "e", "🇪 " },
            { "f", "🇫 " },
            { "g", "🇬 " },
            { "h", "🇭 " },
            { "i", "🇮 " },
            { "j", "🇯 " },
            { "k", "🇰 " },
            { "l", "🇱 " },
            { "m", "🇲 " },
            { "n", "🇳 " },
            { "o", "🇴 " },
            { "p", "🇵 " },
            { "q", "🇶 " },
            { "r", "🇷 " },
            { "s", "🇸 " },
            { "t", "🇹 " },
            { "u", "🇺 " },
            { "v", "🇻 " },
            { "w", "🇼 " },
            { "x", "🇽 " },
            { "y", "🇾 " },
            { "z", "🇿 " },
            { "?", ":grey_question: " },
            { "!", ":grey_exclamation: " },
            { "\u0020", "\u0020\u0020\u0020" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("tiny").WithDescription("Replaces text with tiny letters").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: false, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "a", "ᵃ" },
            { "b", "ᵇ" },
            { "c", "ᶜ" },
            { "d", "ᵈ" },
            { "e", "ᵉ" },
            { "f", "ᶠ" },
            { "g", "ᵍ" },
            { "h", "ʰ" },
            { "i", "ᶦ" },
            { "j", "ʲ" },
            { "k", "ᵏ" },
            { "l", "ᶫ" },
            { "m", "ᵐ" },
            { "n", "ᶰ" },
            { "o", "ᵒ" },
            { "p", "ᵖ" },
            { "q", "ɋ" },
            { "r", "ʳ" },
            { "s", "ˢ" },
            { "t", "ᵗ" },
            { "u", "ᵘ" },
            { "v", "ᵛ" },
            { "w", "ʷ" },
            { "x", "ˣ" },
            { "y", "ʸ" },
            { "z", "ᶻ" },
            { "0", "⁰" },
            { "1", "¹" },
            { "2", "²" },
            { "3", "³" },
            { "4", "⁴" },
            { "5", "⁵" },
            { "6", "⁶" },
            { "7", "⁷" },
            { "8", "⁸" },
            { "9", "⁹" },
            { "A", "ᴬ" },
            { "B", "ᴮ" },
            { "C", "ᶜ" },
            { "D", "ᴰ" },
            { "E", "ᴱ" },
            { "F", "ᵳ" },
            { "G", "ᴳ" },
            { "H", "ᴴ" },
            { "I", "ᴵ" },
            { "J", "ᴶ" },
            { "K", "ᴷ" },
            { "L", "ᴸ" },
            { "M", "ᴹ" },
            { "N", "ᴺ" },
            { "O", "ᴼ" },
            { "P", "ᴾ" },
            { "Q", "ᑫ" },
            { "R", "ᴿ" },
            { "S", "s" },
            { "T", "ᵀ" },
            { "U", "ᵁ" },
            { "V", "v" },
            { "W", "ᵂ" },
            { "X", "ᵡ" },
            { "Y", "ᵞ" },
            { "Z", "ᶻ" },
            { "!", "﹗" },
            { "?", "﹖" },
            { "*", "﹡" },
            { ".", "⋅" },
            { "¡", "ꜞ" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("uwu").WithDescription("Replaces l and r with w").AddOption("text", ApplicationCommandOptionType.String, "Text to transform", true),
          ignoreCase: false, reverse: false, isPrivate: false, map:
          new Dictionary<string, string>
          {
            { "l", "w" },
            { "L", "W" },
            { "r", "w" },
            { "R", "W" },
            { "u", "wu" },
            { "U", "WU" },
          })
        );
      toAdd.Add(
        new ReplaceSubCommand(subCommand: new SlashCommandBuilder().WithName("reverse").WithDescription("Reverses text").AddOption("text", ApplicationCommandOptionType.String, "Text to reverse", true),
          ignoreCase: true, reverse: true, isPrivate: false, map: new Dictionary<string, string>())
        );

      subCommands = toAdd.ToDictionary(pair => pair.subCommand.Name, pair => pair);
    }

    public string CommandTypeName => "replace";
    public IList<string> Names => subCommands.Keys.ToArray();

    public IList<ApplicationCommandProperties> BuildCommands()
    {
      return subCommands.Select(pair => pair.Value.subCommand.Build()).ToArray();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command, string name)
    {
      log.Trace($"Processing {nameof(ReplaceCommand)} {name}.");
      var subCommand = subCommands[name];
      string message = ReplaceVariables(client, command);

      // Now substitute the replacements from the table.
      StringBuilder sb = new StringBuilder();
      foreach (char c in message)
      {
        string letter = c.ToString();
        foreach (var replacementPair in subCommand.map)
        {
          letter = letter.Replace(replacementPair.Key, replacementPair.Value, subCommand.ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

          // If a replacement was made.
          if (letter != c.ToString())
          {
            break;
          }
        }
        sb.Append(letter);
      }

      string output = subCommand.reverse ? new string(sb.ToString().Reverse().ToArray()) : sb.ToString();
      await command.RespondAsync(text: output, ephemeral: subCommand.isPrivate);
    }

    private static string ReplaceVariables(DiscordSocketClient client, SocketSlashCommand command)
    {
      string commandDetail = string.Join(" ", command.Data.Options.Select(option => option.Value)).StripAccents();
      return commandDetail
       .Replace(IMavisMultipleCommand.MentionUserNameReplaceString, "<@" + command.User.Id + ">")
       .Replace(IMavisMultipleCommand.UserNameReplaceString, command.User.Username)
       .Replace(IMavisMultipleCommand.CommandDetailReplaceString, commandDetail)
       .Replace(IMavisMultipleCommand.EscapedDetailReplaceString, Uri.EscapeDataString(commandDetail).Replace("+", "%2B"))
       .Replace(IMavisMultipleCommand.UsernameOrDetailReplaceString, string.IsNullOrWhiteSpace(commandDetail) ? command.User.Username : commandDetail)
       .Replace(IMavisMultipleCommand.BotNameReplaceString, client.CurrentUser.Username)
       .Replace(IMavisMultipleCommand.DevelopmentServerReplaceString, Constants.DevelopmentServerLink)
       .Replace("\\r\\n", Environment.NewLine)
       .Replace("\\n", Environment.NewLine);
    }
  }
}
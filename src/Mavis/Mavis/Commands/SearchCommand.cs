using Discord;
using Discord.WebSocket;
using Mavis.Utils;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavis.Commands
{
  public class SearchCommand : IMavisCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private static readonly Random rand = new();

    public string Name => "search";

    public ApplicationCommandProperties BuildCommand(DiscordSocketClient client)
    {
      /* Some options commented out because Discord has a 25 choice limit >.< */
      return new SlashCommandBuilder()
          .WithName(Name)
          .WithDescription("Privately get a link to an internet search engine or wiki page.")
          .AddOption(new SlashCommandOptionBuilder().WithName("engine").WithType(ApplicationCommandOptionType.String).WithDescription("The engine to search with.").WithRequired(true)
            .AddChoice("Bing", "Bing")
            .AddChoice("Duck Duck Go", "DuckDuckGo")
            .AddChoice("Google", "Google")
            .AddChoice("Google Reverse Image", "GoogleRIM")
            //.AddChoice("LMGTFY", "LMGTFY")
            .AddChoice("Urban Dictionary", "UrbanDictionary")
            .AddChoice("Wolfram|Alpha", "Wolfram")

            //.AddChoice("Wiki: ARMS", "ARMS")
            .AddChoice("Wiki: Bulbapedia", "Bulbapedia")
            //.AddChoice("Wiki: Dragon Quest", "DragonQuest")
            .AddChoice("Wiki: Fire Emblem", "FireEmblem")
            .AddChoice("Wiki: F-Zero", "FZero")
            //.AddChoice("Wiki: Golden Sun", "GoldenSun")
            //.AddChoice("Wiki: Hard Drop", "HardDrop")
            .AddChoice("Wiki: Icaruspedia", "Icaruspedia")
            .AddChoice("Wiki: Inkipedia", "Inkipedia")
            .AddChoice("Wiki: Kingdom Hearts", "KingdomHearts")
            //.AddChoice("Wiki: Lylat", "Lylat")
            .AddChoice("Wiki: Metroid", "Metroid")
            .AddChoice("Wiki: Minecraft", "Minecraft")
            //.AddChoice("Wiki: NIWA", "NIWA")
            .AddChoice("Wiki: Nookipedia", "Nookipedia")
            .AddChoice("Wiki: Pikipedia", "Pikipedia")
            //.AddChoice("Wiki: PikminFanon", "PikminFanon")
            .AddChoice("Wiki: Smash", "Smash")
            .AddChoice("Wiki: Smogon", "Smogon")
            //.AddChoice("Wiki: Starfy", "Starfy")
            .AddChoice("Wiki: Strategy Wiki", "StrategyWiki")
            .AddChoice("Wiki: Super Mario", "SuperMario")
            .AddChoice("Wiki: TF2", "TF2")
            //.AddChoice("Wiki: Wars", "Wars")
            //.AddChoice("Wiki: Wikibound", "Wikibound")
            .AddChoice("Wiki: Wikipedia", "Wikipedia")
            .AddChoice("Wiki: Wikirby", "Wikirby")
            .AddChoice("Wiki: Wiktionary", "Wiktionary")
            .AddChoice("Wiki: Zelda", "Zelda")
           )
          .AddOption(new SlashCommandOptionBuilder().WithName("query").WithType(ApplicationCommandOptionType.String).WithDescription("Optional query.").WithRequired(false))
          .Build();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
    {
      log.Trace($"Processing {nameof(SearchCommand)}.");

      string choice;
      switch (command.Data.Options.First().Value.ToString())
      {
        case "Bing": choice = "https://www.bing.com/search?q=%escapeddetail%&go=Submit"; break;
        case "DuckDuckGo": choice = "https://duckduckgo.com/?q=%escapeddetail%"; break;
        case "Google": choice = "https://www.google.com/#sclient=psy&q=%escapeddetail%"; break;
        case "GoogleRIM": choice = "https://www.google.com/searchbyimage?site=search&sa=X&image_url=%escapeddetail%"; break;
        case "LMGTFY": choice = "https://lmgtfy.com/?q=%escapeddetail%"; break;
        case "UrbanDictionary": choice = "https://www.urbandictionary.com/define.php?term=%escapeddetail%"; break;
        case "Wolfram": choice = "https://www.wolframalpha.com/input/?i=%escapeddetail%"; break;

        case "ARMS": choice = "https://armswiki.org/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Bulbapedia": choice = "https://bulbapedia.bulbagarden.net/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "DragonQuest": choice = "https://dragon-quest.org/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "FireEmblem": choice = "https://www.fireemblemwiki.org/w/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "FZero": choice = "https://mutecity.org/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "GoldenSun": choice = "https://www.goldensunwiki.net/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "HardDrop": choice = "https://harddrop.com/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Icaruspedia": choice = "https://www.kidicaruswiki.org/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Inkipedia": choice = "https://splatoonwiki.org/w/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "KingdomHearts": choice = "https://www.khwiki.net/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Lylat": choice = "https://starfoxwiki.info/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Metroid": choice = "https://www.metroidwiki.org/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Minecraft": choice = "https://minecraft.fandom.com/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "NIWA": choice = "https://www.niwanetwork.org/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Nookipedia": choice = "https://nookipedia.com/w/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Pikipedia": choice = "https://www.pikminwiki.com/w/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "PikminFanon": choice = "https://wwwwww.pikminfanon.com/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Smash": choice = "https://www.ssbwiki.com/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Smogon": choice = "https://www.google.com/search?q=site%3Awww.smogon.com+%escapeddetail%"; break;
        case "Starfy": choice = "https://www.starfywiki.org/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "StrategyWiki": choice = "https://www.strategywiki.org/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "SuperMario": choice = "https://www.mariowiki.com/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "TF2": choice = "https://wiki.teamfortress.com/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Wars": choice = "https://warswiki.org/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Wikibound": choice = "https://www.wikibound.info/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Wikipedia": choice = "https://en.wikipedia.org/w/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Wikirby": choice = "https://wikirby.com/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Wiktionary": choice = "https://en.wiktionary.org/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        case "Zelda": choice = "https://zelda.fandom.com/wiki/?search=%escapeddetail%&title=Special:Search&go=Go"; break;
        default: choice = "Unknown option."; log.Error("Unimplemented option: " + command.Data.Options.First().Value.ToString()); break;
      }
      string result = ReplaceVariables(client, command, choice);
      await command.RespondAsync(text: result, ephemeral: true);
    }

    private static string ReplaceVariables(DiscordSocketClient _, SocketSlashCommand command, string choice)
    {
      string detail = string.Join(" ", command.Data.Options.Skip(1).Select(v => v.Value.ToString()));
      return choice
       .Replace(IMavisMultipleCommand.CommandDetailReplaceString, detail)
       .Replace(IMavisMultipleCommand.EscapedDetailReplaceString, Uri.EscapeDataString(detail).Replace("+", "%2B"))
       .Replace("\\r\\n", Environment.NewLine)
       .Replace("\\n", Environment.NewLine);
    }
  }
}
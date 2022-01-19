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
  record ResponseSubCommand
  {
    public readonly string[] responses;
    public readonly bool isPrivate;
    public readonly SlashCommandBuilder subCommand;

    public ResponseSubCommand(SlashCommandBuilder subCommand, bool isPrivate, string[] responses)
    {
      this.responses = responses;
      this.isPrivate = isPrivate;
      this.subCommand = subCommand;
    }
  }

  public class ResponsesCommand : IMavisMultipleCommand
  {
    private static readonly Logger log = LogManager.GetCurrentClassLogger();
    private static readonly Random rand = new();
    private readonly Dictionary<string, ResponseSubCommand> subCommands = new();

    public string CommandTypeName => "Responses";
    public IList<string> Names => subCommands.Keys.ToArray();

    public ResponsesCommand()
    {
      List<ResponseSubCommand> toAdd = new()
      {
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("8ball").WithDescription("Ask the 8 ball a question").AddOption("query", ApplicationCommandOptionType.String, "Optional question", false),
          isPrivate: false, responses: new[]
          {
            "🎱 As I see it, yes.",
            "🎱 It is certain.",
            "🎱 It is decidedly so.",
            "🎱 Most likely.",
            "🎱 Outlook good.",
            "🎱 Signs point to yes.",
            "🎱 Without a doubt.",
            "🎱 Yes.",
            "🎱 Yes – definitely.",
            "🎱 You may rely on it.",
            "🎱 Reply hazy, try again.",
            "🎱 Ask again later.",
            "🎱 Better not tell you now.",
            "🎱 Cannot predict now.",
            "🎱 Concentrate and ask again.",
            "🎱 Don't count on it.",
            "🎱 My reply is no.",
            "🎱 My sources say no.",
            "🎱 Outlook not so good.",
            "🎱 Very doubtful.",
          }),
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("42ball").WithDescription("Ask the 42 ball a question").AddOption("query", ApplicationCommandOptionType.String, "Optional question", false),
          isPrivate: false, responses: new[]
          {
            ":four::two:",
            "42.",
            "_**42**_",
            "(4*10)+2",
            "④②",
            "４２",
            "⁴²",
            "⁴²",
            "42\r\n2",
          }),
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("sarcastic-8ball").WithDescription("Ask the sarcastic-8 ball a question").AddOption("query", ApplicationCommandOptionType.String, "Optional question", false),
          isPrivate: false, responses: new[]
          {
            "😏 As if.",
            "😏 I don't care.",
            "😏 Dumb question. Ask another.",
            "😏 Forget about it.",
            "😏 Get a clue.",
            "😏 In your dreams.",
            "😏 Not.",
            "😏 Not a chance.",
            "😏 Obviously.",
            "😏 Oh please.",
            "😏 Sure.",
            "😏 That's ridiculous.",
            "😏 Well maybe.",
            "😏 What do you think?",
            "😏 Whatever.",
            "😏 Who cares?",
            "😏 Yeah, and I'm the pope.",
            "😏 Yeah right.",
            "😏 You wish.",
            "😏 You've got to be kidding.",
          }),
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("yoda-8ball").WithDescription("Ask the yoda-8 ball a question").AddOption("query", ApplicationCommandOptionType.String, "Optional question", false),
          isPrivate: false, responses: new[]
          {
            "💭 Yes, I sense this is.",
            "💭 The answer you seek is yes.",
            "💭 Simple question you ask. Yes, I answer.",
            "💭 Difficult question you ask. Yes, I answer.",
            "💭 Use the force. Teach you it will.",
            "💭 Search your feelings. Answer this question it will.",
            "💭 Many questions you ask.",
            "💭 This even Yoda does not know.",
            "💭 Answers you seek can be found in the Force.",
            "💭 No, I sense this.",
            "💭 The answer you seek is no.",
            "💭 Simple question you ask. No, I answer.",
            "💭 Difficult question you ask. No, I answer.",
          }),
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("bed").WithDescription("GO TO BED"),
          isPrivate: false, responses: new[] { "→ 🛏" }),
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("dancegif").WithDescription("Post the dancing robot"),
          isPrivate: false, responses: new[] { "http://transhuman.neocities.org/midibot.gif" }),
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("disapprove").WithDescription("Post your disapproval"),
           isPrivate: false, responses: new[] { "ಠ_ಠ", "ಠ__ಠ", "ಠ~ಠ" }),
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("doot").WithDescription("Post your doots"),
           isPrivate: false, responses: new[] { "🎺 Doot 🎺", "🎺 Doot Doot 🎺", "🎺 Toot Toot 🎺", "📯 Pfffammpt 📯" }),
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("invite").WithDescription("Get an invite link for Mavis"),
           isPrivate: false, responses: new[] { Constants.BotInviteLink }),
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("just-right").WithDescription("When it's just right"),
           isPrivate: false, responses: new[]
           {
             "✋😔👌",
             "https://i.kym-cdn.com/entries/icons/original/000/019/698/d96.jpg",
             "https://images7.memedroid.com/images/UPLOADED804/5a6342bbec8df.jpeg",
             "https://pics.me.me/Facebook-When-you-hit-88mph-just-right-61e62a.png",
             "https://images7.memedroid.com/images/UPLOADED177/5702cf6aea70f.jpeg",
             "https://i.imgflip.com/1utxbf.jpg",
             "http://41.media.tumblr.com/c7a0cf68f8aa446eb263bde292ccdaeb/tumblr_o3ogq8GE7E1sr6y44o1_r1_500.jpg",
             "https://memeguy.com/photos/images/the-customer-ordered-a-pizza-just-right-210756.jpg",
             "https://i.imgur.com/mTvGsBol.png",
             "https://i.kym-cdn.com/photos/images/original/001/073/767/c96.jpg",
             "https://media.tenor.com/images/aec958c19350bcb199e44234ccc03e7d/tenor.gif",
             "http://i.imgur.com/lM1E6na.jpg",
             "https://orig00.deviantart.net/3b0e/f/2016/041/2/1/when_the_backstab_is_just_right_by_myopicmoose-d9ravgw.jpg",
             "https://i.kym-cdn.com/photos/images/original/001/104/603/5cf.png",
             "https://pics.astrologymemes.com/when-the-high-noon-is-just-right-2653946.png",
             "https://i.kym-cdn.com/photos/images/original/001/091/136/8f4.png",
             "https://img.fireden.net/v/image/1454/20/1454202004090.png",
             "https://assets.rbl.ms/13362349/980x.png",
             "https://pm1.narvii.com/6673/a150ec100ec35f9828881b044801b14071906a78_hq.jpg",
           }),
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("lenny").WithDescription("Post your lenny face"),
           isPrivate: false, responses: new[] { "( ͡° ͜ʖ ͡°)", "( ﾟ ͜ʖ ﾟ)", "( ͡º ͜ʖ͡º)" }),
        new ResponseSubCommand(subCommand: new SlashCommandBuilder().WithName("woomy").WithDescription("Woomy!"),
           isPrivate: false, responses: new[] { "Woomy! くコ:彡", "くコ:彡 ~~", "WOOMY! > 🦑 🐙 < NGYES!", "MAMENMI! > 🦑 🐙 < SQWAYY!", "Ngyes! くコ:彡", "~~ 彡:C> Woomy!", "~~ 彡:C> Ngyes!", }) };

      subCommands = toAdd.ToDictionary(pair => pair.subCommand.Name, pair => pair);
    }

    public IList<ApplicationCommandProperties> BuildCommands()
    {
      return subCommands.Select(pair => pair.Value.subCommand.Build()).ToArray();
    }

    public async Task Execute(DiscordSocketClient client, SocketSlashCommand command, string name)
    {
      log.Trace($"Processing {nameof(ResponsesCommand)} {name}.");
      var subCommand = subCommands[name];
      string choice = subCommand.responses[rand.Next(subCommand.responses.Length)];
      string result = ReplaceVariables(client, command, choice);

      // Respond with the Responses message
      await command.RespondAsync(text: result, ephemeral: subCommand.isPrivate);
    }

    private static string ReplaceVariables(DiscordSocketClient client, SocketSlashCommand command, string choice)
    {
      string commandDetail = string.Join(" ", command.Data.Options.Select(option => option.Value));
      return choice
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
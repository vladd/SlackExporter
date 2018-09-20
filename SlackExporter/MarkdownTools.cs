using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SlackExporter
{
    // some documentation: https://api.slack.com/docs/messages#formatting_messages
    static class MarkdownTools
    {
        static Regex userref = new Regex("<@([a-zA-Z0-9]+)>", RegexOptions.Compiled);
        static Regex chanref = new Regex("<#([a-zA-Z0-9]+)>", RegexOptions.Compiled);
        static Regex emojiref = new Regex(@":([a-zA-Z0-9_]+):", RegexOptions.Compiled);
        static Regex pre = new Regex(@"```(.*?)```", RegexOptions.Compiled);
        static Regex code = new Regex(@"`(.*?)`", RegexOptions.Compiled);
        static Regex url = new Regex(@"<(https?:.*?)>", RegexOptions.Compiled);
        static Regex italics = new Regex(@"\b_([^_]*?)_\b", RegexOptions.Compiled);
        static Regex bold = new Regex(@"\B\*\b([^\*]*?)\b\*\B", RegexOptions.Compiled);
        static Regex strike = new Regex(@"\B~\b([^~]*?)\b~\B", RegexOptions.Compiled);

        public static string BeautifyMessage(string text)
        {
            text = userref.Replace(text, match => Program.usersById[match.Groups[1].Value].displayName);
            text = chanref.Replace(text, match => Program.channelsById[match.Groups[1].Value].name);
            text = emojiref.Replace(text, match =>
            {
                var emoji = Program.EmojiTools.Get(match.Groups[1].Value);
                if (match.Index == 0 && match.Length == text.Length)
                    return $"<span class=\"bigemoji\">{emoji}</span>";
                else
                    return emoji;
            });
            text = text.Replace("\n", "<br>");
            text = pre.Replace(text, match => $"<pre class=\"blockpre\">{match.Groups[1].Value}</pre>");
            text = code.Replace(text, match => $"<code class=\"softcode\">{match.Groups[1].Value}</code>");
            text = url.Replace(text, match =>
            {
                var link = match.Groups[1].Value;
                var display = link;
                if (link.Contains('|'))
                {
                    var parts = link.Split('|');
                    if (parts.Length != 2)
                        throw new FormatException("Cannot parse link");
                    link = parts[0];
                    display = parts[1];
                }
                return $"<a href=\"{link}\">{display}</a>";
            });
            text = italics.Replace(text, match => $"<em>{match.Groups[1].Value}</em>");
            text = bold.Replace(text, match => $"<strong>{match.Groups[1].Value}</strong>");
            text = strike.Replace(text, match => $"<strike>{match.Groups[1].Value}</strike>");
            return text;
        }
    }
}

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
            var r1 = userref.Replace(text, match => Program.usersById[match.Groups[1].Value].displayName);
            var r2 = chanref.Replace(r1, match => Program.channelsById[match.Groups[1].Value].name);
            var r3 = emojiref.Replace(r2, match =>
            {
                var emoji = Program.EmojiTools.Get(match.Groups[1].Value);
                if (match.Index == 0 && match.Length == r2.Length)
                    return $"<span class=\"bigemoji\">{emoji}</span>";
                else
                    return emoji;
            });
            var r4 = r3.Replace("\n", "<br>");
            var r5 = pre.Replace(r4, match => $"<pre>{match.Groups[1].Value}</pre>");
            var r6 = code.Replace(r5, match => $"<code>{match.Groups[1].Value}</code>");
            var r7 = url.Replace(r6, match =>
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
            var r8 = italics.Replace(r7, match => $"<em>{match.Groups[1].Value}</em>");
            var r9 = bold.Replace(r8, match => $"<strong>{match.Groups[1].Value}</strong>");
            var r10 = strike.Replace(r9, match => $"<strike>{match.Groups[1].Value}</strike>");
            return r10;
        }
    }
}

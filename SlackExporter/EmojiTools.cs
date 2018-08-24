using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SlackExporter
{
    // TODO: find a complete emoji name list, compatible with Slack
    // per https://stackoverflow.com/a/39654939/276994, it must be https://raw.githubusercontent.com/iamcal/emoji-data/master/emoji.json?
    // or maybe https://www.webfx.com/tools/emoji-cheat-sheet/?
    class EmojiTools
    {
        EmojiList1 t1;
        EmojiList2 t2;

        public EmojiTools(string p1, string p2)
        {
            t1 = new EmojiList1(p1);
            t2 = new EmojiList2(p2);
        }

        public string Get(string desc) =>
            t1.TryGet(desc) ?? t2.TryGet(desc) ?? throw new ArgumentException("Unknown emoji");
    }

    // https://gist.githubusercontent.com/oliveratgithub/0bf11a9aff0d6da7b46f1490f86a71eb/raw/ac8dde8a374066bcbcf44a8296fc0522c7392244/emojis.json
    class EmojiList1
    {
        class EmojiEntry
        {
            public string annotation;
            public string unicode;
            public string[] shortcodes;
        }

        Dictionary<string, string> DescToEmoji;

        public EmojiList1(string datapath)
        {
            using (var reader = File.OpenText(datapath))
            {
                var list = new JsonSerializer().Deserialize<List<EmojiEntry>>(new JsonTextReader(reader));
                DescToEmoji = new Dictionary<string, string>();
                foreach (var l in list)
                {
                    DescToEmoji[l.annotation] = l.unicode;
                    if (l.shortcodes != null)
                        foreach (var sc in l.shortcodes)
                            DescToEmoji[sc] = l.unicode;
                }
            }
        }

        public string Get(string desc)
        {
            if (DescToEmoji.TryGetValue(desc.Replace('_', ' '), out var emoji))
                return emoji;
            throw new ArgumentException("Unknown emoji");
        }

        public string TryGet(string desc)
        {
            if (DescToEmoji.TryGetValue(desc.Replace('_', ' '), out var emoji))
                return emoji;
            return null;
        }
    }

    // https://raw.githubusercontent.com/milesj/emojibase/master/packages/data/en/compact.json
    class EmojiList2
    {
        class Shell
        {
            public List<EmojiEntry> emojis;
        }

        class EmojiEntry
        {
            public string emoji;
            public string name;
            public string shortname;
        }

        Dictionary<string, string> DescToEmoji;

        public EmojiList2(string datapath)
        {
            using (var reader = File.OpenText(datapath))
            {
                var list = new JsonSerializer().Deserialize<Shell>(new JsonTextReader(reader)).emojis;
                DescToEmoji = list.Where(entry => !string.IsNullOrEmpty(entry.shortname))
                                  .ToDictionary(entry => entry.shortname.Trim(':'), entry => entry.emoji);
                DescToEmoji = new Dictionary<string, string>();
                foreach (var l in list)
                {
                    DescToEmoji[l.name] = l.emoji;
                    DescToEmoji[l.shortname.Trim(':')] = l.emoji;
                }
            }
        }

        public string Get(string desc)
        {
            if (DescToEmoji.TryGetValue(desc, out var emoji))
                return emoji;
            throw new ArgumentException("Unknown emoji");
        }

        public string TryGet(string desc)
        {
            if (DescToEmoji.TryGetValue(desc, out var emoji))
                return emoji;
            return null;
        }
    }
}

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
    // https://raw.githubusercontent.com/iamcal/emoji-data/master/emoji.json
    class EmojiTools
    {
        class EmojiEntry
        {
            public string unified;
            public string[] short_names;
        }

        Dictionary<string, string> NameToEmoji;

        public EmojiTools(string datapath)
        {
            using (var reader = File.OpenText(datapath))
            {
                var list = new JsonSerializer().Deserialize<List<EmojiEntry>>(new JsonTextReader(reader));
                NameToEmoji = new Dictionary<string, string>();
                foreach (var l in list)
                {
                    string unicode = ConvertUnified(l.unified);
                    foreach (var sc in l.short_names)
                        NameToEmoji[sc] = unicode;
                }
            }
        }

        static string ConvertUnified(string unified) => string.Concat(unified.Split('-').Select(ConvertSingle));
        static string ConvertSingle(string unified) => char.ConvertFromUtf32(Convert.ToInt32(unified, 16));

        public string Get(string name)
        {
            if (NameToEmoji.TryGetValue(name, out var emoji))
                return emoji;
            throw new ArgumentException("Unknown emoji");
        }

        public string TryGet(string name)
        {
            if (NameToEmoji.TryGetValue(name, out var emoji))
                return emoji;
            return null;
        }
    }
}

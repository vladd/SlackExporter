using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SlackExporter
{
    class Program
    {
        public static Dictionary<string, User> usersById;
        public static Dictionary<string, Channel> channelsById;

        static public Uri GenerateUri(string remoteUri, string nameHint, bool needFull)
        {
            if (remoteUri == null)
                return null;
            var path = remoteCache.Cache(remoteUri, nameHint);
            var fullUri = new Uri(path, UriKind.Absolute);
            if (needFull)
                return fullUri;
            else
                return baseUri.MakeRelativeUri(fullUri);
        }

        static string baseDir;
        static Uri baseUri;
        static string selfDir;

        public static EmojiTools EmojiTools { get; private set; }
        static RemoteCache remoteCache;

        public static TimeZoneInfo TargetTimeZone { get; private set; }

        // args:
        //  [0] = work directory
        //  [1] = target time zone ID
        static void Main(string[] args)
        {
            var firstArg = args.FirstOrDefault();
            if (firstArg == "-?" || firstArg == "/?")
            {
                Console.WriteLine("Usage: SlackExporter [work directory] [target time zone]");
                Console.WriteLine("If work directory is not specified, current is assumed");
                Console.WriteLine("If time zone is not specified, local is assumed");
                Console.WriteLine("Work directory must contain an extracted Slack archive");
                return;
            }
            baseDir = (args.Length > 0) ? args[0] : Environment.CurrentDirectory;
            baseUri = new Uri(baseDir, UriKind.Absolute);
            selfDir = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);

            TargetTimeZone = (args.Length > 1) ?
                TimeZoneInfo.FindSystemTimeZoneById(args[1]) :
                TimeZoneInfo.Local;

            EmojiTools = new EmojiTools(Path.Combine(selfDir, "Emoji", "emoji.json"));
            remoteCache = new RemoteCache(Path.Combine(baseDir, "cache.registry"), Path.Combine(baseDir, "Cache"));

            var dataImporter = new DataImporter();
            usersById = dataImporter.GetUsers(Path.Combine(baseDir, @"users.json")).ToDictionary(user => user.id);

            List<Channel> channels = dataImporter.GetChannels(Path.Combine(baseDir, @"channels.json"));
            channelsById = channels.ToDictionary(chan => chan.id);

            File.Copy(Path.Combine(selfDir, "Styles.css"), Path.Combine(baseDir, "Styles.css"), overwrite: true);

            foreach (var channel in channels)
            {
                var channelDir = Path.Combine(baseDir, channel.name);
                var channelsFiles = Directory.EnumerateFiles(channelDir, "*.json");

                var sortedRootMessages = new DataImporter().GetRootMessages(channelsFiles);

                OutputMessageList(Path.Combine(baseDir, channel.name + ".html"), sortedRootMessages);
            }
        }

        static void OutputMessageList(string file, IEnumerable<Message> messages)
        {
            var template = File.ReadAllLines(Path.Combine(selfDir, "Template.txt"));

            using (var of = File.CreateText(file))
            {
                // preamble
                foreach (var l in template.TakeWhile(l => l != "[CONTENT]"))
                    of.WriteLine(l);

                // content
                Message.RenderMessageList(messages, of.WriteLine);

                // closing
                foreach (var l in template.SkipWhile(l => l != "[CONTENT]").Skip(1))
                    of.WriteLine(l);
            }
        }
    }
}

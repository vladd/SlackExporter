using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SlackExporter
{
    class DataImporter
    {
        static string GetUriExtension(Uri uri) => Path.GetExtension(uri.AbsolutePath);

        public List<User> GetUsers(string file)
        {
            using (var reader = File.OpenText(file))
            {
                var usersJson = JArray.ReadFrom(new JsonTextReader(reader));
                return usersJson.Select(ParseUser).ToList();
            }
        }

        User ParseUser(JToken token)
        {
            var id = (string)token["id"];
            var profile = token["profile"];
            var displayName = (string)profile["display_name"];
            if (string.IsNullOrEmpty(displayName))
                displayName = (string)profile["real_name"];
            var avatarString = (string)profile["image_72"];
            Uri avatar = null;
            if (avatarString != null)
            {
                var ext = GetUriExtension(new Uri(avatarString));
                avatar = Program.GenerateUri(avatarString, "avatar." + displayName + ext, needFull: false);
            }
            return new User() { id = id, displayName = displayName, avatar = avatar };
        }

        public List<Channel> GetChannels(string file)
        {
            using (var reader = File.OpenText(file))
            {
                var channelsJson = JArray.ReadFrom(new JsonTextReader(reader));
                return channelsJson.Select(ParseChannel).ToList();
            }
        }

        Channel ParseChannel(JToken token)
        {
            var id = (string)token["id"];
            var name = (string)token["name"];
            var purpose = (string)token["purpose"]["value"];
            return new Channel() { id = id, name = name, purpose = purpose };
        }

        public List<Message> GetRootMessages(IEnumerable<string> files)
        {
            var allMessages = files.SelectMany(ParseMessageFile).Where(msg => msg != null).ToList();

            var tsToMsg = allMessages.ToDictionary(msg => msg.timestamp);

            // set parents
            foreach (var message in allMessages)
            {
                if (message.childTimestamps != null)
                {
                    message.children = message.childTimestamps.Select(ts => tsToMsg[ts]).ToList();
                    foreach (var child in message.children)
                        child.parent = message;
                }
            }

            var sortedRootMessages = allMessages.Where(msg => msg.parent == null).OrderBy(msg => msg.timestamp).ToList();
            return sortedRootMessages;
        }

        List<Message> ParseMessageFile(string file)
        {
            using (var reader = File.OpenText(file))
            {
                var messagesJson = JArray.ReadFrom(new JsonTextReader(reader));
                return messagesJson.Select(ParseMessage).ToList();
            }
        }

        Message ParseMessage(JToken token)
        {
            var subtypeRaw = token["subtype"];
            var subtype = subtypeRaw == null ? null : (string)subtypeRaw;
            Message message;
            switch (subtype)
            {
            case "file_comment":
                return null; // TODO: add file comment processing too 
            case null:
                var upload = token["upload"];
                if (upload != null && (bool)upload)
                    message = new UploadMessage() { files = GatherFiles(token["files"]).ToList() };
                else
                    message = new TextMessage();
                break;
            case "channel_join":
                message = new JoinMessage();
                break;
            case "channel_purpose":
                message = new ChannelPurposeMessage() { purpose = (string)token["purpose"] };
                break;
            case "channel_name":
                message = new ChannelNameMessage() { oldName = (string)token["old_name"], name = (string)token["name"] };
                break;
            case "file_share":
                var fileContent = ParseFileContent(token["file"]);
                message = new PreviewableFileMessage() { file = fileContent };
                break;
            default:
                throw new NotImplementedException("Unknown message type: " + subtype);
            }

            var userId = (string)token["user"];
            message.author = Program.usersById[userId];
            message.text = (string)token["text"];
            message.timestamp = ExtractTimestamp(token["ts"]);

            var threadToken = token["thread_ts"];
            if (threadToken != null)
                message.threadTimestamp = ExtractTimestamp(threadToken);

            var repliesToken = token["replies"];
            if (repliesToken != null)
                message.childTimestamps = ((JArray)repliesToken).Select(t => ExtractTimestamp(t["ts"])).ToList();

            if (token["attachments"] != null)
                message.attachments = ((JArray)token["attachments"]).Select(ParseAttachment).ToList();

            return message;
        }

        IEnumerable<FileContent> GatherFiles(JToken files)
        {
            if (!(files is JArray fileArray))
                throw new NotSupportedException("Unsupported upload type, expected array of files");
            return fileArray.Select(ParseFileContent);
        }

        FileContent ParseFileContent(JToken file)
        {
            var mode = (string)file["mode"];
            var name = (string)file["name"];
            switch (mode)
            {
            case "hosted":
                var initialComment = file["initial_comment"];
                var mimetype = (string)file["mimetype"];
                var filetype = (string)file["filetype"];
                var thumbName = Path.GetFileNameWithoutExtension(name);
                var thumbExt = Path.GetExtension(name);
                var thumbSuggestion = $"{thumbName}.thumb{thumbExt}";
                FileContent content;
                if (mimetype.StartsWith("image/"))
                {
                    var thumbLink = (string)file["thumb_480"] ?? (string)file["thumb_360"] ?? (string)file["thumb_64"];
                    content = new ThumbnailedFileContent()
                    {
                        //origSize = new Size() { w = (int)file["original_w"], h = (int)file["original_h"] },
                        thumb = Program.GenerateUri(thumbLink, thumbSuggestion, needFull: false)
                    };
                }
                else if (mimetype == "application/pdf")
                {
                    content = new ThumbnailedFileContent()
                    {
                        thumb = Program.GenerateUri((string)file["thumb_pdf"], thumbSuggestion, needFull: false)
                    };
                }
                else if (mimetype.StartsWith("video/"))
                {
                    content = new VideoFileContent();
                }
                else
                {
                    throw new NotSupportedException("Unsupported file mime type: " + mimetype);
                }
                content.name = name;
                content.link = Program.GenerateUri((string)file["url_private"], name, needFull: false);
                content.comment = initialComment == null ? null : (string)initialComment["comment"];
                content.mimeType = mimetype;
                content.fileType = filetype;
                return content;
            case "snippet":
                string snippet;
                if ((int)file["lines_more"] != 0)
                {
                    var snippetUri = (string)file["url_private"];
                    if (string.IsNullOrEmpty(name))
                        name = "snippet";
                    snippet = new WebClient() { Encoding = Encoding.UTF8 }.DownloadString(Program.GenerateUri(snippetUri, name, needFull: true));
                }
                else
                {
                    snippet = (string)file["preview"];
                }
                return new SnippetFileContent { snippet = snippet };
            default:
                throw new NotImplementedException("Unknown message type: share/" + mode);
            }
        }

        Attachment ParseAttachment(JToken token)
        {
            Attachment attachment;
            if (token["video_html"] is JToken videoToken)
            {
                Size size = new Size() { w = 480, h = 360 };
                if (token["video_html_width"] is JToken width && token["video_html_height"] is JToken height)
                    size = new Size() { w = (int)width, h = (int)height };
                attachment = new RemoteVideoAttachment() { VideoHtml = (string)videoToken, VideoSize = size };
            }
            else if (token["text"] is JToken textToken)
            {
                attachment = new SiteLinkAttachment() { Text = (string)textToken };
            }
            else
            {
                attachment = new SimpleAttachment();
            }

            attachment.Title = (string)token["title"];
            var titleLinkString = (string)token["title_link"];
            if (titleLinkString != null)
                attachment.TitleLink = new Uri(titleLinkString);

            attachment.ServiceName = (string)token["service_name"];
            if (token["service_icon"] is JToken serviceIconToken)
            {
                var serviceIcon = (string)serviceIconToken;
                attachment.ServiceIcon = Program.GenerateUri(serviceIcon, $"{attachment.ServiceName}.icon{GetUriExtension(new Uri(serviceIcon))}", needFull: false);
            }
            if (token["service_url"] is JToken serviceUrlToken)
                attachment.ServiceUrl = new Uri((string)serviceUrlToken);

            attachment.FromUrl = new Uri((string)token["from_url"]);

            if (token["thumb_url"] is JToken thumbToken)
            {
                var thumb = (string)thumbToken;
                attachment.Thumb = Program.GenerateUri(thumb, $"{attachment.ServiceName}.thumb{GetUriExtension(new Uri(thumb))}", needFull: false);
            }

            if (token["author_name"] is JToken nameToken)
                attachment.AuthorName = (string)nameToken;
            if (token["author_link"] is JToken nameLinkToken)
                attachment.AuthorLink = new Uri((string)nameLinkToken);

            return attachment;
        }

        static ExactDateTimeOffset ExtractTimestamp(JToken token) => new ExactDateTimeOffset((double)token);
    }
}

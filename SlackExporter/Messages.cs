using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SlackExporter
{
    abstract class Message
    {
        public ExactDateTimeOffset timestamp, threadTimestamp;
        public List<ExactDateTimeOffset> childTimestamps;
        public User author;
        public string text;
        public List<Attachment> attachments;
        public List<Message> children;
        public Message parent;

        protected string InContainer(string inner) => "<div class=\"container\">" + inner + "</div>";
        protected string InMessage(string inner) => "<div class=\"message\">" + inner + "</div>";

        protected string GetAvatarHtml() => $"<img class=\"avatar\" src=\"{author.avatar.ToString()}\"/>";
        protected string GetMessageHtml() =>
            InMessage(GetUsernameHtml() +
                      GetTimeHtml() +
                      GetMessageTextHtml() +
                      GetAdditions() +
                      GetAttachments() +
                      GetThread());
        protected string GetUsernameHtml() => $"<div class=\"username\">{WebUtility.HtmlEncode(author.displayName)}</div>";
        protected string GetTimeHtml() => $"<div class=\"time\">{timestamp}</div>";
        protected virtual string GetMessageTextHtml() => $"<div class=\"msg{GetMessageClasses()}\">{GetMessageContent()}</div>";
        protected abstract string GetMessageContent();
        protected virtual string GetMessageClasses() => "";
        protected virtual string GetAdditions() => "";

        protected string GetAttachments()
        {
            if (attachments == null)
                return "";
            return StringifyAttachList(attachments);
        }

        static string StringifyAttachList(IEnumerable<Attachment> attachments) =>
            string.Join("\n", attachments.Select(a => $"<div class=\"attachcontainer\">{a.CreateHtml()}</div>"));

        protected string GetThread()
        {
            if (children == null)
                return "";
            return
                $"<div class=\"threadcontainer\">\n  " +
                StringifyMessageList(children) +
                $"\n</div>";
        }

        static string StringifyMessageList(IEnumerable<Message> messages)
        {
            var sb = new StringBuilder();
            RenderMessageList(messages, s => sb.AppendLine(s));
            return sb.ToString();
        }

        static public void RenderMessageList(IEnumerable<Message> messages, Action<string> shipout)
        {
            bool prevText = false;
            User prevUser = null;
            foreach (var message in messages)
            {
                if (message is TextMessage tm)
                {
                    if (prevText && prevUser == tm.author)
                        shipout(tm.CreateHtmlShort());
                    else
                        shipout(message.CreateHtml());
                    prevText = true;
                }
                else
                {
                    shipout(message.CreateHtml());
                    prevText = false;
                }
                prevUser = message.author;
            }
        }

        public string CreateHtml() => InContainer(GetAvatarHtml() + GetMessageHtml());
    }

    class TextMessage : Message
    {
        protected override string GetMessageContent() => MarkdownTools.BeautifyMessage(text);

        const string messageTemplateShort =
            "<div class=\"container\">" +
                "<div class=\"imgplaceholder\"><div class=\"hiddentime\">{0}</div></div>" +
                "<div class=\"message\">" +
                    "<div class=\"simplemsg\">{1}</div>" +
                    "{2}" +
                "</div>" +
            "</div>";

        public string CreateHtmlShort() =>
            string.Format(messageTemplateShort,
                          timestamp.ToTimeString(),
                          MarkdownTools.BeautifyMessage(text),
                          GetAttachments() + GetThread());
    }

    abstract class FileMessage : Message
    {
        public Uri link;
        //public Size origSize;
        public string comment;
        public string mimeType, fileType;
        public string name;
    }

    class PreviewableFileMessage : FileMessage
    {
        public Uri thumb;

        protected override string GetMessageClasses() => " sysmsg";
        protected override string GetMessageContent() => $"загрузил файл: «{name}»";
        protected override string GetAdditions() => GetPreviewHtml();

        string GetPreviewHtml() => $"<a href=\"{link.ToString()}\" target=\"_blank\"><img src=\"{thumb.ToString()}\" style=\"max-width: 480px;\"></a>";
    }

    class ImageMessage : PreviewableFileMessage { }

    class AppMessage : PreviewableFileMessage { }

    class VideoMessage : FileMessage
    {
        protected override string GetMessageClasses() => " sysmsg";
        protected override string GetMessageContent() => $"загрузил файл: «{name}»";
        protected override string GetAdditions() => $"<video class=\"video\" src=\"{link.ToString()}\" controls></video>";
    }

    class SnippetMessage : Message
    {
        public string snippet;

        protected override string GetMessageTextHtml() => "";
        protected override string GetAdditions() => $"<pre class=\"prettyprint linenums\">{snippet}</pre>";
        protected override string GetMessageContent() => "";
    }

    abstract class SystemMessage : Message
    {
        protected override string GetMessageClasses() => " sysmsg";
    }

    class JoinMessage : SystemMessage
    {
        protected override string GetMessageContent() => "присоединился к каналу";
    }

    class ChannelPurposeMessage : SystemMessage
    {
        public string purpose;
        protected override string GetMessageContent() => $"установил предназначение канала: «{purpose}»";
    }

    class ChannelNameMessage : SystemMessage
    {
        public string oldName;
        public string name;

        protected override string GetMessageContent() => $"назвал канал «{name}»";
    }
}

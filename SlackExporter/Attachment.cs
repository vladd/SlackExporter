using System;
using System.Net;

namespace SlackExporter
{
    // TODO: check the documentation here: https://api.slack.com/docs/message-attachments
    abstract class Attachment
    {
        public string Title;
        public Uri TitleLink;
        public string ServiceName;
        public Uri ServiceIcon;
        public Uri ServiceUrl;
        public string AuthorName;
        public Uri AuthorLink;
        public Uri FromUrl;
        public Uri Thumb; // optional

        public string CreateHtml() => GetAuthorLineHtml() + GetTitleHtml() + GetContentHtml();

        string GetAuthorLineHtml()
        {
            var serviceHtml = GetServiceHtml();
            var authorHtml = GetAuthorHtml();
            var inlay = (serviceHtml != "" && authorHtml != "") ? "<span> | </span>" : "";
            var content = serviceHtml + inlay + authorHtml;
            return $"<div class=\"attachauthor\">{content}</div>";
        }

        string GetServiceHtml()
        {
            var icon = (ServiceIcon != null) ? $"<img class=\"serviceicon\" src=\"{ServiceIcon}\">" : "";
            var name = (ServiceName != null) ? $"<span class=\"servicename\">{ServiceName}</span>" : "";
            var content = icon + name;
            if (ServiceUrl != null)
                content = $"<a href=\"{ServiceUrl}\" target=\"_blank\" rel=\"noopener noreferrer\">{content}</a>";
            return content;
        }

        string GetAuthorHtml()
        {
            var name = (AuthorName != null) ? $"<span class=\"authorname\">{AuthorName}</span>" : "";
            if (AuthorLink != null)
                return $"<a href=\"{AuthorLink}\" target=\"_blank\" rel=\"noopener noreferrer\">{name}</a>";
            return name;
        }

        string GetTitleHtml()
        {
            var content = (TitleLink != null) ? $"<a href=\"{TitleLink}\" target=\"_blank\" rel=\"noopener noreferrer\">{Title}</a>" : Title;
            return $"<div class=\"attachtitle\">{content}</div>";
        }

        protected abstract string GetContentHtml();
    }

    class RemoteVideoAttachment : Attachment
    {
        public string VideoHtml;
        public Size VideoSize;
        protected override string GetContentHtml() =>
            "<div class=\"attachcontent attachvideocontent\" onclick=\"video_activate(this)\" " +
                $"data-video-content=\"{WebUtility.HtmlEncode(VideoHtml)}\"" +
                $"style=\"width: {VideoSize.w}px; height: {VideoSize.h}px;\">" +
              $"<img src=\"{Thumb}\" class=\"videothumb\">" +
            "</div>";
    }

    class SiteLinkAttachment : Attachment
    {
        public string Text;
        protected override string GetContentHtml() => $"<div class=\"attachcontent\">{Text}</div>";
    }

    class SimpleAttachment : Attachment
    {
        protected override string GetContentHtml() => "";
    }
}

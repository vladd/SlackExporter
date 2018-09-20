using System;

namespace SlackExporter
{
    abstract class FileContent
    {
        public Uri link;
        //public Size origSize;
        public string comment;
        public string mimeType, fileType;
        public string name;
        public Uri thumb;

        public abstract string CreateHtml();
    }

    class ThumbnailedFileContent : FileContent
    {
        public override string CreateHtml() => $"<a href=\"{link}\" target=\"_blank\"><img src=\"{thumb}\" style=\"max-width: 480px;\"></a>";
    }

    class VideoFileContent : FileContent
    {
        public override string CreateHtml() => $"<video class=\"video\" src=\"{link}\" controls></video>";
    }

    class SnippetFileContent : FileContent
    {
        public string snippet;
        public override string CreateHtml() => $"<pre class=\"prettyprint linenums\">{snippet}</pre>";
    }
}

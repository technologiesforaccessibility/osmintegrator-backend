using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using OsmIntegrator.DomainUseCases;
using OsmIntegrator.Interfaces;

namespace OsmIntegrator.Presenters
{

    public class CreateChangeFileWebPresenter : IPresentResponse<StreamContent>
    {
        public StreamContent Content { get; private set; }

        public void Present(AUseCaseResponse result)
        {
            var r = (CreateChangeFileResponse)result;
            var zipFileMemoryStream = new MemoryStream();

            using (ZipArchive archive = new ZipArchive(zipFileMemoryStream, ZipArchiveMode.Update, leaveOpen: true))
            {
                var entry = archive.CreateEntry("osmchange.osc");
                var commentEntry = archive.CreateEntry("osmchange.comment");

                using (var commentEntryStream = commentEntry.Open())
                {
                        r.CommentStream.CopyTo(commentEntryStream);
                }
                using (var entryStream = entry.Open())
                {
                    r.XmlStream.CopyTo(entryStream);
                }
            }
            zipFileMemoryStream.Seek(0, SeekOrigin.Begin);
            Content = new StreamContent(zipFileMemoryStream);

        }
    }
}
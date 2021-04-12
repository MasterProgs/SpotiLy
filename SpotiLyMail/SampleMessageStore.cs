using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace SpotiLyMail
{
    public class SampleMessageStore : MessageStore
    {
        readonly TextWriter _writer;

        public SampleMessageStore(TextWriter writer)
        {
            _writer = writer;
        }

        public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            await using var stream = new MemoryStream();

            var position = buffer.GetPosition(0);
            while (buffer.TryGet(ref position, out var memory))
            {
                stream.Write(memory.Span);
            }

            stream.Position = 0;

            var message = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);
            var dest = string.Join(',', message.To.Mailboxes.Select(x => x.GetAddress(true)));

            _writer.WriteLine("From={0}", string.Join(',', message.From.Mailboxes.Select(x => x.GetAddress(true))));
            _writer.WriteLine("To={0}", string.Join(',', message.To.Mailboxes.Select(x => x.GetAddress(true))));

            File.WriteAllText(Directory.GetCurrentDirectory() + "/mails/" + dest + DateTime.Now.ToFileTime().ToString(), $"{message.Subject}\n\n{message.HtmlBody}\n{message.TextBody}");
            return SmtpResponse.Ok;
        }
    }
}
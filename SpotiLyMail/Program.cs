using SmtpServer;
using SmtpServer.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpotiLyMail
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var options = new SmtpServerOptionsBuilder().ServerName("SmtpServer SampleApp").Port(25).Build();

            var serviceProvider = new ServiceProvider();
            serviceProvider.Add(new SampleMessageStore(Console.Out));

            var smtpServer = new SmtpServer.SmtpServer(options, serviceProvider);
            await smtpServer.StartAsync(CancellationToken.None);
        }
    }
}
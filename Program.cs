using System;
using System.Threading;
using FluentFTP;

namespace FtpExtractor
{
    class Program
    {
        private static readonly CancellationTokenSource GlobalCts = new CancellationTokenSource();
        static void Main(string[] args)
        {
            //hook ctrl-c
            Console.CancelKeyPress += (sender, e) =>
            {
                GlobalCts.Cancel();
                Console.WriteLine("Abort");
                e.Cancel = true;
            };

            try
            {
                var opts = new Options(args);
                FtpTrace.LogToConsole = opts.Verbose;

                switch (opts.Action)
                {
                    case Options.EAction.DownloadFile:
                        FtpActions.DownloadFile(opts.GetFtpParams(), opts.SourcePath, opts.DestinationPath, opts.Resume, opts.Retries, opts.Delay, (long)opts.StartOffset, GlobalCts.Token);
                        break;
                    case Options.EAction.GetDirectoryList: 
                        FtpActions.PrintDirectoryListing(opts.GetFtpParams(), opts.SourcePath);
                        break;
                    case Options.EAction.ShowHelp:
                        Console.WriteLine(opts.GetHelpMessage());
                        break;
                    case Options.EAction.UploadFile:
                        FtpActions.UploadFile(opts.GetFtpParams(), opts.DestinationPath, opts.SourcePath, GlobalCts.Token);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                while (e.InnerException != null)
                {
                    e = e.InnerException;
                    Console.WriteLine("Inner Exception: " + e.Message);
                }
                Console.WriteLine("Try --help or -a help for more information");
            }
        }
    }
}

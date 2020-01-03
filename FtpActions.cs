using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace FtpExtractor
{
    public static class FtpActions
    {
        public static void UploadFile(Options.FtpParams ftpParams, string serverPath, string localPath, CancellationToken uploadingCts)
        {
            if (!File.Exists(localPath))
            {
                throw new FileNotFoundException($"File {localPath} not exist!");
            }

            var uplFileStream = File.OpenRead(localPath);
            var ftpClient = PrepareFtpClient(ftpParams);
            ftpClient.UploadAsync(uplFileStream, serverPath, progress: new FtpProgressHandler(uplFileStream), token: uploadingCts).Wait(uploadingCts);
        }

        public static void DownloadFile(Options.FtpParams ftpParams, string serverPath, string localPath, bool resume, int retries, int delay, long offset, CancellationToken downloadingCts)
        {
            //prepare file
            var dataStream = (Stream) null;
            if (resume)
            {
                if (File.Exists(localPath))
                {
                    dataStream = File.Open(localPath, FileMode.Open);
                    offset = dataStream.Length;
                    dataStream.Position = dataStream.Length;
                    Console.WriteLine("Resuming from position " + offset);
                }
                else
                {
                    throw new FileNotFoundException($"File {localPath} not exist!");
                }
            }
            else
            {
                dataStream = File.Create(localPath);
            }

            //downloading
            var origStreamStart = dataStream.Position;
            var alreadyRetries = 0;
            while (true)
            {
                try
                {
                    var ftpClient = PrepareFtpClient(ftpParams);
                    DownloadToStream(ftpClient, dataStream, offset, serverPath, downloadingCts);
                    dataStream.Close();
                    break;
                }
                catch (Exception e)
                {
                    var receivedBytesCount = dataStream.Position - origStreamStart;
                    Console.WriteLine($"Received total {receivedBytesCount} bytes");
                    if (retries == -1 || retries > alreadyRetries)
                    {
                        offset += receivedBytesCount;
                        Task.Delay(delay * 100, downloadingCts).Wait(downloadingCts);
                        Console.WriteLine("Retry");
                        alreadyRetries++;
                    }
                    else
                    {
                        dataStream.Close();
                        throw;
                    }
                }
            }
        }

        private static FtpClient PrepareFtpClient(Options.FtpParams ftpParams)
        {
            var ftpClient = new FtpClient(ftpParams.Address, ftpParams.Port,
                new NetworkCredential(ftpParams.Username, ftpParams.Password))
            {
                Encoding = ftpParams.Encoding,
                UploadDataType = FtpDataType.Binary,
                EncryptionMode = ftpParams.UseSsl ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None,
                SslProtocols = ftpParams.UseSsl ? SslProtocols.Tls : SslProtocols.None,
                ValidateAnyCertificate = ftpParams.IgnoreCert
            };
            ftpClient.Connect();
            return ftpClient;
        }

        private static void DownloadToStream(FtpClient ftpClient, Stream dataStream, long offset, string serverPath, CancellationToken downloadingCts)
        {
            var origStreamStart = dataStream.Position;
            try
            {
                ftpClient.DownloadAsync(dataStream, serverPath, offset, new FtpProgressHandler(dataStream), downloadingCts)
                    .Wait(downloadingCts);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                Console.WriteLine($"Received total {dataStream.Position - origStreamStart} bytes");
            }
        }

        public static void PrintDirectoryListing(Options.FtpParams ftpParams, string serverPath)
        {
            var ftpClient = PrepareFtpClient(ftpParams);

            var listing = ftpClient.GetListing(serverPath);
            
            var directories = listing.Where(l => l.Type == FtpFileSystemObjectType.Directory);
            var files = listing.Where(l => l.Type == FtpFileSystemObjectType.File);
            var links = listing.Where(l => l.Type == FtpFileSystemObjectType.Link);

            //Console.WriteLine("\tDirectories");
            foreach (var dir in directories)
            {
                Console.WriteLine($"{dir.Name}/");
            }

            //Console.WriteLine("\tLinks");
            foreach (var link in links)
            {
                Console.WriteLine($"{link.Name}");
            }

            //Console.WriteLine("\tFiles");
            foreach (var file in files)
            {
                Console.WriteLine($"{file.Size.ToString().PadRight(16)}  {file.Name}");
            }
        }

        
    }

}

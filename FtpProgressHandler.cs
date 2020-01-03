using System;
using System.IO;
using FluentFTP;

namespace FtpExtractor
{
    public class FtpProgressHandler : IProgress<FtpProgress>
    {
        private readonly Stream _dataStream;
        private int _counter = 0;

        private DateTime _prevTime = DateTime.Now;

        public FtpProgressHandler(Stream dataStream) => _dataStream = dataStream;
        public void Report(FtpProgress progress)
        {
            if (DateTime.Now.Subtract(_prevTime).TotalMilliseconds >= 200 || progress.Progress >= 100)
            {
                _prevTime = DateTime.Now;
                _counter = 0;
                ClearPrint($"eta: {progress.ETA:dd\\.hh\\:mm\\:ss}\t speed: {progress.TransferSpeedToString()}\t %: {progress.Progress:F}\t Pos: {_dataStream.Position}");
            }
            _counter++;
        }

        //TODO: need fix
        public static void ClearPrint(string text)
        {
            Console.WriteLine(text);
        }
    }
}
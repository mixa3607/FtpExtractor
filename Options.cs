using System;
using System.Collections.Generic;
using System.Text;
using Mono.Options;

namespace FtpExtractor
{
    public class Options
    {
        public struct FtpParams
        {
            public Encoding Encoding;
            public string Username;
            public string Password;
            public int Port;
            public string Address;
            public bool UseSsl;
            public bool IgnoreCert;
        }

        private static EAction ParseAction(string sAction)
        {
            if (StringCommands.ContainsKey(sAction))
                return StringCommands[sAction];
            throw new KeyNotFoundException($"Action with key \"{sAction}\" not found!");
        }

        public enum EAction : byte
        {
            //None,
            ShowHelp,
            GetDirectoryList,
            GetFileInfo,
            DownloadFile,
            UploadFile
        }

        private OptionSet _optionSet;
        private static readonly Dictionary<string, EAction> StringCommands = new Dictionary<string, EAction>
        {
            {"d", EAction.DownloadFile},
            {"download", EAction.DownloadFile},
            {"dwn", EAction.DownloadFile},

            {"u", EAction.UploadFile},
            {"upload", EAction.UploadFile},
            {"upl", EAction.UploadFile},

            {"ls", EAction.GetDirectoryList},
            {"list", EAction.GetDirectoryList},

            //{"i", EAction.GetFileInfo},
            //{"info", EAction.GetFileInfo},

            {"help", EAction.ShowHelp},
            {"h", EAction.ShowHelp},
            {"hlp", EAction.ShowHelp},
        };

        
        public EAction Action { get; private set; } = EAction.DownloadFile;
        public string SourcePath { get; private set; } = null;
        public string DestinationPath { get; private set; } = null;
        public int Retries { get; private set; } = 0; //-1 mean unlimited
        public int Delay { get; private set; } = 10; //delay in sec between retries
        public bool Resume { get; private set; } = false;
        public ulong StartOffset { get; private set; } = 0L;
        //public ulong BytesCount { get; private set; } = 0L; //0 mean to end
        public string FtpUsername { get; private set; } = "Anonymous";
        public string FtpPassword { get; private set; } = "";
        public bool UseSsl { get; private set; } = false;
        public bool IgnoreCert { get; private set; } = false;
        public Encoding FtpEncoding { get; private set; } = Encoding.UTF8;
        public bool Verbose { get; private set; } = false;

        public string FtpAddress { get; private set; } = null;
        public int FtpPort { get; private set; } = 21;

        

        public Options(string[] args) => Parse(args);

        private void Parse(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _optionSet = new OptionSet()
            {
                {"user=", $"Set username. Default: \"{FtpUsername}\"", u => FtpUsername = u},
                {"pass=", $"Set password. Default: \"{FtpPassword}\"", p => FtpPassword = p},
                {"ssl", "Use ssl", f => UseSsl = true},
                //{"count=", "Download N bytes", (ulong c) => BytesCount = c},
                {"offset=", $"Start download from N byte. Default: \"{StartOffset}\"", (ulong o) => StartOffset = o},
                {"c|resume", "Continue download", f => Resume = true},
                {"p|delay=", $"Delay between retries. Default: \"{Delay}\"", (int p) => Delay = p},
                {"r|retries=", $"Retries count. -1 eq infinity. Default: \"{Retries}\"", (int t) => Retries = t},
                {"d|destination=", "Destination path", d => DestinationPath = d},
                {"s|source=", "Source path", s => SourcePath = s},
                {"a|action=", "Select action. Default: \"download\"", c => Action = ParseAction(c)},
                {"h|help", "Show this message", f => Action = EAction.ShowHelp},
                {"e|encoding=", $"Set encoding. Default: \"{FtpEncoding.BodyName}\"", s => FtpEncoding = Encoding.GetEncoding(s)},
                {"v|verbose", "More debug info", f=> Verbose = true },
                {"I|ignore-cert", "Don't check ssl cert", f=> IgnoreCert = true },
            };
            try
            {
                _optionSet.Parse(args);
                ValidateOptions(this);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public FtpParams GetFtpParams()
        {
            return new FtpParams()
            {
                Address = FtpAddress,
                Encoding = FtpEncoding,
                Password = FtpPassword,
                Port = FtpPort,
                Username = FtpUsername,
                UseSsl = UseSsl,
                IgnoreCert = IgnoreCert
            };
        }

        private static void ValidateOptions(Options options)
        {
            switch (options.Action)
            {
                case EAction.DownloadFile:
                    ParseServerPath(options);
                    if (string.IsNullOrWhiteSpace(options.DestinationPath))
                        options.DestinationPath = options.SourcePath.Substring(1);
                    break;
                case EAction.UploadFile:
                    ParseServerPath(options, false);
                    if (string.IsNullOrWhiteSpace(options.SourcePath))
                        throw new FormatException("Source file path must be not empty");
                    break;
                case EAction.GetFileInfo:
                case EAction.GetDirectoryList:
                    ParseServerPath(options);
                    break;
            }
        }

        private static void ParseServerPath(Options options, bool parseSource = true)
        {
            var serverPath = parseSource ? options.SourcePath : options.DestinationPath;
            var (addr, port, path) = ParseServerPath(serverPath);

            if (string.IsNullOrWhiteSpace(addr))
                throw new FormatException("server address is empty");

            options.FtpAddress = addr;

            if (port != null)
                options.FtpPort = (int)port;

            if (parseSource)
                options.SourcePath = path;
            else
                options.DestinationPath = path;
        }

        private static (string addr, int? port, string path) ParseServerPath(string serverPath)
        {
            try
            {
                var parts = serverPath.Split('/', 2);

                var path = "/" + parts[1];
                var addr = parts[0];
                var port = (int?) null;

                if (addr.Contains(':'))
                {
                    parts = addr.Split(':');
                    addr = parts[0];
                    port = int.Parse(parts[1]);
                }

                return (addr, port, path);
            }
            catch (Exception e)
            {
                throw new FormatException("Wrong server url!", e);
            }
        }

        public string GetHelpMessage() => GetHelpMessage(_optionSet);

        private static string GetHelpMessage(OptionSet optionSet)
        {
            var helpBuilder = new StringBuilder();

            helpBuilder.Append("Available keys: \n");
            foreach (var option in optionSet)
            {
                var len = 0;
                foreach (var optName in option.GetNames())
                {
                    var str = (len > 0 ? "|" : "  ") + (optName.Length == 1? "-" : "--")  + optName;
                    len += str.Length;
                    helpBuilder.Append(str);
                }

                helpBuilder.Append($"{"".PadRight(20 - len)}   {option.Description}\n");
            }

            helpBuilder.Append("\nAvailable actions: \n");
            foreach (var stringCommand in StringCommands)
            {
                helpBuilder.Append($"  {stringCommand.Key.PadRight(10)}  {stringCommand.Value.ToString()}\n");
            }

            return helpBuilder.ToString();
        }
    }
}
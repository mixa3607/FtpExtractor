# FtpExtractor
Programm mainly for download files from unstable servers with non-standard encoding. Also support upload and listing
```
$ ./FtpExtractor --help
Available keys:
  --user               Set username. Default: "Anonymous"
  --pass               Set password. Default: ""
  --ssl                Use ssl
  --offset             Start download from N byte. Default: "0"
  -c|--resume          Continue download
  -p|--delay           Delay between retries. Default: "10"
  -r|--retries         Retries count. -1 eq infinity. Default: "0"
  -d|--destination     Destination path
  -s|--source          Source path
  -a|--action          Select action. Default: "download"
  -h|--help            Show this message
  -e|--encoding        Set encoding. Default: "utf-8"
  -v|--verbose         More debug info
  -I|--ignore-cert     Don't check ssl cert

Available actions:
  d           DownloadFile
  download    DownloadFile
  dwn         DownloadFile
  u           UploadFile
  upload      UploadFile
  upl         UploadFile
  ls          GetDirectoryList
  list        GetDirectoryList
  help        ShowHelp
  h           ShowHelp
  hlp         ShowHelp
```

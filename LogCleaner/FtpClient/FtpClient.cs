using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace LogCleaner.FtpClient
{
  internal class FtpClient
  {

    private readonly string _userName;
    private readonly string _password;
    private readonly int? _requestTimeout;
    private readonly int? _readWriteTimeout;
    public string Server { get;}

    public FtpClient(string server, int? requestTimeout = null, int? readWriteTimeout = null)
    {
        Server = server;
        _userName = "anonymous";
        _password = "anonymous";
        _requestTimeout =  requestTimeout;
        _readWriteTimeout = readWriteTimeout;
    }

    public IEnumerable<String> ListFiles(string path)
    {
      var listFiles = new List<String>();
      var request = GetRequest(path, WebRequestMethods.Ftp.ListDirectory);

      using (var response = (FtpWebResponse)request.GetResponse())
      {
        using (var stream = response.GetResponseStream())
        {
          using (var reader = new StreamReader(stream, true))
          {
            while (!reader.EndOfStream)
            {
              listFiles.Add(reader.ReadLine());
            }
          }
        }
      }
      return listFiles;
    }

    public async Task<IEnumerable<string>> ListFilesAsync(string path)
    {
      var listFiles = new List<string>();
      var request = GetRequest(path, WebRequestMethods.Ftp.ListDirectory);

      using (var response = (FtpWebResponse)await request.GetResponseAsync())
      {
        using (var stream = response.GetResponseStream())
        {
          using (var reader = new StreamReader(stream, true))
          {
            while (!reader.EndOfStream)
            {
              listFiles.Add(await reader.ReadLineAsync());
            }
          }
          
        }
      }
      return listFiles;
    }

    public FtpStatusCode DownloadFile(string source, string target)
    {
      var request = GetRequest(source, WebRequestMethods.Ftp.DownloadFile);

      using (var response = (FtpWebResponse)request.GetResponse())
      {
        using (var stream = response.GetResponseStream())
        {
          using (var fs = new FileStream(target, FileMode.Create, FileAccess.Write))
          {
            stream.CopyTo(fs, 4096);
          }
          return response.StatusCode;
        }
      }
    }

    public async Task<FtpStatusCode> DownloadFileAsync(string source, string target)
    {
      var request = GetRequest(source, WebRequestMethods.Ftp.DownloadFile);

      using (var response = (FtpWebResponse)await request.GetResponseAsync())
      {
        using (var stream = response.GetResponseStream())
        {
          using (var fs = new FileStream(target, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.Asynchronous ))
          {
            await stream.CopyToAsync(fs, 4096);
          }
        }
      }
      return await GetStatusCodeAsync(request);
    }

    public async Task<FtpStatusCode> DownloadFileAsync(string source,  Stream trgStream)
    {
      var request = GetRequest(source, WebRequestMethods.Ftp.DownloadFile);

      using (var response = (FtpWebResponse)await request.GetResponseAsync())
      {
        using (var stream = response.GetResponseStream())
        {
          await stream.CopyToAsync(trgStream, 4096);
        }
      }
      return await GetStatusCodeAsync(request);
    }

    public FtpStatusCode DeleteFile(string targetPath)
    {
      var request = GetRequest(targetPath, WebRequestMethods.Ftp.DeleteFile);
      using (var response = (FtpWebResponse)request.GetResponse())
      {
        return response.StatusCode;
      }
    }

    public async Task<FtpStatusCode> DeleteFileAsync(string targetPath)
    {
      var request = GetRequest(targetPath, WebRequestMethods.Ftp.DeleteFile);
      return await GetStatusCodeAsync(request);
    }

    private FtpStatusCode GetStatusCode(WebRequest request)
    {
      using (var response = (FtpWebResponse)request.GetResponse())
      {
        return response.StatusCode;
      }
    }

    private static async Task<FtpStatusCode> GetStatusCodeAsync(WebRequest request)
    {
      using (var response = (FtpWebResponse)await request.GetResponseAsync())
      {
        return response.StatusCode;
      }
    }


    private Uri GetServerUri(string path)
    {
      return new Uri($"ftp://{Server}{'/'}{path}");
    }

    private FtpWebRequest GetRequest(string path, string method)
    {
      var uri = GetServerUri(path);

      var request = (FtpWebRequest)WebRequest.Create(uri);
      request.Timeout = _requestTimeout ?? request.Timeout ;
      request.ReadWriteTimeout = _readWriteTimeout ?? request.ReadWriteTimeout;
      request.Method = method;
      request.Credentials = new NetworkCredential(_userName, _password);

      return request;
    }

  }
}

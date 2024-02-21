using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FtpSiteManager
{
    /// <summary>
    /// FTP操作类，用于执行FTP相关操作
    /// </summary>
    public class FtpHelper
    {
        /// <summary>
        /// FTP服务器地址
        /// </summary>
        private string ftpServer;

        /// <summary>
        /// 用户名
        /// </summary>
        private string userName;

        /// <summary>
        /// 密码
        /// </summary>
        private string passWord;

        /// <summary>
        /// FTPHelper类的构造函数
        /// </summary>
        /// <param name="ftpServer">FTP服务器地址</param>
        /// <param name="userName">用户名</param>
        /// <param name="passWord">密码</param>
        public FtpHelper(string ftpServer, string userName, string passWord)
        {
            this.ftpServer = ftpServer;
            this.userName = userName;
            this.passWord = passWord;
        }
        /// <summary>
        /// 查询FTP服务器上所有文件和目录
        /// </summary>
        /// <param name="directoryPath">要查询的目录路径</param>
        public List<FileInfo> FtpQueryAll(string directoryPath = "")
        {
            List<FileInfo> list = new List<FileInfo>();
            ExecuteFtpOperation(() =>
         {
             FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + directoryPath);
             request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
             request.Credentials = new NetworkCredential(userName, passWord);
             request.Timeout = 5000;

             using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
             {
                 //Console.WriteLine("状态: {0}", response.StatusDescription);

                 using (var responseStream = response.GetResponseStream())
                 {
                     if (responseStream != null)
                     {
                         using (var reader = new StreamReader(responseStream))
                         {
                             string line = reader.ReadLine();
                             while (!string.IsNullOrEmpty(line))
                             {
                                 list.AddRange(ParseFTPFileList(line));
                                 line = reader.ReadLine();
                             }
                         }
                     }
                 }
             }
         });
            return list;
        }

        /// <summary>
        /// 解析FTP服务器返回的文件列表信息，将其转换为FileInfo对象列表
        /// </summary>
        /// <param name="ftpFileList">FTP服务器返回的文件列表信息</param>
        /// <returns>包含文件信息的FileInfo对象列表</returns>
        public List<FileInfo> ParseFTPFileList(string ftpFileList)
        {
            // 解析FTP返回的文件列表信息并返回FileInfo对象列表
            List<FileInfo> filesInfo = new List<FileInfo>();

            // 按行分割FTP文件列表信息
            string[] lines = ftpFileList.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // 按空格分割行信息
                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 4)
                {

                    string lastModifiedDateStr = parts[0] + " " + parts[1];
                    string format = "MM-dd-yy hh:mmtt"; // 指定日期时间的确切格式
                    DateTime lastModifiedDate;
                    DateTime.TryParseExact(lastModifiedDateStr, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out lastModifiedDate);

                    // 提取文件大小信息
                    string fileSizeStr = parts[2];
                    long fileSize;
                    string fileType = "Folder";
                    string fileName = string.Join(" ", parts, 3, parts.Length - 3);
                    if (fileSizeStr.Contains("DIR"))
                    {

                        fileSize = 0;
                    }
                    else
                    {
                        fileType = Path.GetExtension(fileName);
                        fileSize = Convert.ToInt64(fileSizeStr);
                    }


                    FileInfo fileInfo = new FileInfo(lastModifiedDate, fileSize, fileType, fileName);

                    filesInfo.Add(fileInfo);
                }
            }

            return filesInfo;
        }


        /// <summary>
        /// 从FTP服务器下载文件到本地
        /// </summary>
        /// <param name="remoteFilePaths">要下载的远程文件路径数组</param>
        /// <param name="localDirectory">本地目录路径</param>
        public void FtpDownloadFile(string[] remoteFilePaths, string localDirectory)
        {
            ExecuteFtpOperation(() =>
            {
                // 检查本地路径是否存在，如果不存在则创建
                if (!Directory.Exists(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                }
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(userName, passWord);

                    foreach (var remoteFilePath in remoteFilePaths)
                    {
                        string fileName = remoteFilePath.Substring(remoteFilePath.LastIndexOf("/") + 1);
                        string localFilePath = Path.Combine(localDirectory, fileName);

                        try
                        {
                            client.DownloadFile(ftpServer + remoteFilePath, localFilePath);
                            Console.WriteLine($"文件 '{fileName}' 下载成功。");
                        }
                        catch (WebException ex)
                        {
                            Console.WriteLine($"下载文件 '{fileName}' 时出错: {ex.Message}");
                            // Handle the exception as needed
                        }
                    }
                }
            });
        }


        /// <summary>
        /// 上传文件到FTP服务器
        /// </summary>
        /// <param name="filePath">要上传的文件路径</param>
        /// <param name="directoryPath">目标文件夹路径</param>
        public void FtpUploadFile(string filePath, string directoryPath = "")
        {
            ExecuteFtpOperation(() =>
            {
                if (!FtpDirectoryExists(directoryPath))
                {
                    FtpCreateDirectory(directoryPath);
                }
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(userName, passWord); ;
                    client.UploadFile(ftpServer + directoryPath + "/" + Path.GetFileName(filePath), WebRequestMethods.Ftp.UploadFile, filePath);
                    Console.WriteLine("文件上传成功。");
                }
            });
        }
        public void FtpUploadFolder(string localFolderPath, string ftpDirectoryPath)
        {
            if (!Directory.Exists(localFolderPath))
            {
                Console.WriteLine("本地文件夹不存在。");
                return;
            }

            // 获取文件夹名称
            string folderName = new DirectoryInfo(localFolderPath).Name;

            string rootPath = (string.IsNullOrEmpty(ftpDirectoryPath) ? "" : ftpDirectoryPath + "/");

            if (!FtpDirectoryExists(rootPath + folderName))
            {
                // 在 FTP 服务器上创建目标文件夹
                FtpCreateDirectory(rootPath + folderName);
            }
        

            // 获取文件夹中的所有文件
            string[] files = Directory.GetFiles(localFolderPath);

            // 逐个上传文件
            foreach (string file in files)
            {
                FtpUploadFile(file, rootPath + folderName);
            }

            // 获取文件夹中的所有子文件夹
            string[] subDirectories = Directory.GetDirectories(localFolderPath);

            // 逐个上传子文件夹
            foreach (string subDirectory in subDirectories)
            {
                FtpUploadFolder(subDirectory, rootPath + folderName);
            }

            Console.WriteLine($"{localFolderPath} 文件夹上传成功。");
        }

        /// <summary>
        /// 上传多个文件夹到FTP服务器
        /// </summary>
        /// <param name="filePath">要上传的文件夹路径</param>
        /// <param name="ftpDirectoryPath">目标文件夹路径</param>
        public void FtpUploadFolders(string[] localDirectories, string ftpDirectoryPath = "")
        {
            foreach (string localDirectory in localDirectories)
            {
                FtpUploadFolder(localDirectory, ftpDirectoryPath);
            }
        }


      


        /// <summary>
        /// 判断FTP服务器上是否存在指定文件夹
        /// </summary>
        /// <param name="directoryPath">要检查的文件夹路径</param>
        /// <returns>文件夹是否存在的布尔值</returns>
        public bool FtpDirectoryExists(string directoryPath)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + directoryPath);
            request.Credentials = new NetworkCredential(userName, passWord);
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                response.Close();
                return true;
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return false;
                }
                throw;
            }
        }

        /// <summary>
        /// 在FTP服务器上创建文件夹
        /// </summary>
        /// <param name="folderName">要创建的文件夹名称</param>
        public void FtpCreateDirectory(string folderName)
        {
            ExecuteFtpOperation(() =>
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + folderName);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Credentials = new NetworkCredential(userName, passWord);

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Console.WriteLine("文件夹创建成功。");
                }
            });
        }

        /// <summary>
        /// 移动FTP服务器上的多个文件
        /// </summary>
        /// <param name="sourceDirectoryPath">源文件目录路径</param>
        /// <param name="destinationDirectoryPath">目标文件目录路径</param>
        /// <param name="fileNames">要移动的文件名数组</param>
        public void FtpMoveFiles(string sourceDirectoryPath, string destinationDirectoryPath, string[] fileNames)
        {
            ExecuteFtpOperation(() =>
            {
                foreach (var fileName in fileNames)
                {
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + sourceDirectoryPath + "/" + fileName);
                    request.Method = WebRequestMethods.Ftp.Rename;
                    request.Credentials = new NetworkCredential(userName, passWord);
                    request.RenameTo = destinationDirectoryPath + "/" + fileName;

                    using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                    {
                        Console.WriteLine($"文件 '{fileName}' 移动成功。");
                    }
                }
            });
        }


        /// <summary>
        /// 执行FTP操作的方法
        /// </summary>
        /// <param name="action">要执行的操作</param>
        private void ExecuteFtpOperation(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (WebException ex)
            {
                HandleFtpException(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生错误: {0}", ex.Message);
            }
        }


        // 处理FTP异常的方法
        private void HandleFtpException(WebException ex)
        {
            Console.WriteLine("发生错误: {0}", ex.Message);
            if (ex.Status == WebExceptionStatus.Timeout)
            {
                Console.WriteLine("连接超时。");
            }
        }



        /// <summary>
        /// 验证服务器证书的方法
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="certificate">服务器证书</param>
        /// <param name="chain">证书链</param>
        /// <param name="sslPolicyErrors">SSL策略错误</param>
        /// <returns>是否验证通过</returns>
        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //return true;
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                return false;
            }
            else if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                System.Security.Policy.Zone z = System.Security.Policy.Zone.CreateFromUrl(((FtpWebRequest)sender).RequestUri.ToString());
                if (z.SecurityZone == System.Security.SecurityZone.Internet)
                {
                    return true;
                }
                return false;
            }
            return true;
        }

    }
}

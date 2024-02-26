using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FtpSiteManager
{
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
        /// 执行FTP操作的方法
        /// </summary>
        /// <param name="action">要执行的操作</param>
        private int ExecuteFtpOperation(Action action)
        {
            try
            {
                action.Invoke();
                return 1;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    Console.WriteLine("连接超时。");
                }
                else
                {
                    Console.WriteLine("发生错误 WebException: {0}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生错误: {0}", ex.Message);
            }
            return 0;
        }

        #region 文件查询

        /// <summary>
        /// 递归查询FTP服务器上所有文件和目录
        /// </summary>
        /// <param name="ftpDirectoryPath">要查询的目录路径</param>
        public virtual List<FileInformation> RecursiveQueryAll(string ftpDirectoryPath = "")
        {
            List<FileInformation> list = new List<FileInformation>();
            ExecuteFtpOperation(() =>
            {
                List<FileInformation> currentList = QueryAll(ftpDirectoryPath);
                list.AddRange(currentList);

                foreach (var fileInfo in currentList)
                {
                    if (fileInfo.FileType == "Folder")
                    {
                        // 如果是文件夹，递归查询
                        List<FileInformation> subList = RecursiveQueryAll(ftpDirectoryPath + "/" + fileInfo.FileName);
                        list.AddRange(subList);
                    }
                }
            });
            return list;
        }

        /// <summary>
        /// 查询FTP服务器上指定路径下的文件和文件夹
        /// </summary>
        /// <param name="ftpDirectoryPath">要查询的目录路径</param>
        public virtual List<FileInformation> QueryAll(string ftpDirectoryPath = "")
        {
            List<FileInformation> list = new List<FileInformation>();
            ExecuteFtpOperation(() =>
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + ftpDirectoryPath);
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
        public virtual List<FileInformation> ParseFTPFileList(string ftpFileList)
        {
            // 解析FTP返回的文件列表信息并返回FileInfo对象列表
            List<FileInformation> filesInfo = new List<FileInformation>();

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


                    FileInformation fileInfo = new FileInformation(lastModifiedDate, fileSize, fileType, fileName);

                    filesInfo.Add(fileInfo);
                }
            }

            return filesInfo;
        }
        #endregion

        #region 判断FTP服务器上是否存在指定文件夹 && 在FTP服务器上创建文件夹 &&删除FTP服务器上的空文件夹
        /// <summary>
        /// 判断FTP服务器上是否存在指定文件夹
        /// </summary>
        /// <param name="directoryPath">要检查的文件夹路径</param>
        /// <returns>文件夹是否存在的布尔值</returns>
        public virtual bool FtpDirectoryExists(string directoryPath)
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
        public virtual void FtpCreateDirectory(string folderName)
        {
            ExecuteFtpOperation(() =>
            {
                string[] pathArray = folderName.Split('/');
                var tempPath = "";
                foreach (var path in pathArray)
                {
                    if (path == "")
                    {
                        continue;
                    }
                    tempPath += path + "/";
                    if (FtpDirectoryExists(tempPath))
                    {
                        continue;
                    }
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + tempPath);
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    request.Credentials = new NetworkCredential(userName, passWord);

                    using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                    {
                        Console.WriteLine($"文件夹{path}创建成功。");
                    }
                }

            });
        }
        /// <summary>
        /// 删除FTP服务器上的空文件夹
        /// </summary>
        /// <param name="ftpFolderPath">FTP服务器上的空文件夹路径</param>
        public virtual int FtpDeleteFolder(string ftpFolderPath)
        {
            return ExecuteFtpOperation(() =>
             {
                 if (string.IsNullOrEmpty(ftpFolderPath))
                 {
                     return;
                 }
                 // 连接到 FTP 服务器
                 FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + ftpFolderPath);
                 request.Credentials = new NetworkCredential(userName, passWord);
                 request.Method = WebRequestMethods.Ftp.RemoveDirectory;
                 using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                 {
                     Console.WriteLine($"文件夹{new DirectoryInfo(ftpFolderPath).Name}删除成功！");
                 }
             });
        }
        #endregion

        #region 文件、文件夹删除
        /// <summary>
        /// 删除FTP服务器指定路径下的多个文件
        /// </summary>
        /// <param name="directoryPath">要删除的文件夹路径</param>
        /// <param name="fileNames">要删除的文件名数组（多文件,号分隔）</param>
        public virtual int FtpDeleteFiles(string directoryPath, string fileNames)
        {
            return ExecuteFtpOperation(() =>
            {
                foreach (var fileName in fileNames.Split(','))
                {
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + directoryPath + "/" + fileName);
                    request.Method = WebRequestMethods.Ftp.DeleteFile;
                    request.Credentials = new NetworkCredential(userName, passWord);

                    using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                    {
                        Console.WriteLine($"文件 '{fileName}' 删除成功。");
                    }
                }
            });
        }

        /// <summary>
        /// 递归删除FTP服务器上的文件夹及其内容
        /// </summary>
        /// <param name="directoryPath">要删除的文件夹路径</param>
        public virtual int FtpDeleteFolders(string directoryPath)
        {
            return ExecuteFtpOperation(() =>
            {

                if (!FtpDirectoryExists(directoryPath))
                {
                    Console.WriteLine($"{directoryPath} 不存在！");
                    return;
                }

                // 获取文件夹内所有文件和子文件夹
                var fileList = QueryAll(directoryPath);
                foreach (var fileInfo in fileList)
                {
                    // 删除文件
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + directoryPath + "/" + fileInfo.FileName);
                    request.Method = WebRequestMethods.Ftp.DeleteFile;
                    request.Credentials = new NetworkCredential(userName, passWord);

                    // 如果是文件夹，递归删除
                    if (fileInfo.FileType == "Folder")
                    {
                        FtpDeleteFolders(directoryPath + "/" + fileInfo.FileName);
                    }
                    else
                    {
                        using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                        {
                            Console.WriteLine($"文件 '{fileInfo.FileName}' 删除成功。");
                        }
                    }



                }

                // 删除空文件夹
                FtpDeleteFolder(directoryPath);
            });
        }


        #endregion

        #region  文件移动

        /// <summary>
        /// 移动FTP服务器上的多个文件
        /// </summary>
        /// <param name="sourceDirectoryPath">源文件目录路径</param>
        /// <param name="destinationDirectoryPath">目标文件目录路径</param>
        /// <param name="fileNames">要移动的文件名数组(,分隔)</param>
        public virtual int FtpMoveFiles(string sourceDirectoryPath, string destinationDirectoryPath, string fileNames)
        {
            return ExecuteFtpOperation(() =>
             {
                 if (!FtpDirectoryExists(sourceDirectoryPath))
                 {
                     throw new Exception($"{sourceDirectoryPath} 目录不存在！");                   
                 }
                 if (!FtpDirectoryExists(destinationDirectoryPath))
                 {
                     FtpCreateDirectory(destinationDirectoryPath);
                 }
                 foreach (var fileName in fileNames.Split(','))
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
        /// 移动整个文件夹到目标位置
        /// </summary>
        /// <param name="sourceDirectoryPath">源文件夹路径</param>
        /// <param name="destinationDirectoryPath">目标文件夹路径</param>
        public virtual int FtpMoveFolder(string sourceDirectoryPath, string destinationDirectoryPath)
        {
            return ExecuteFtpOperation(() =>
             {
                 if (!FtpDirectoryExists(sourceDirectoryPath))
                 {
                     Console.WriteLine($"{sourceDirectoryPath} 目录不存在！");
                     return;
                 }
                 //destinationDirectoryPath = destinationDirectoryPath + "/" + new DirectoryInfo(sourceDirectoryPath).Name;//解决移动后源文件夹丢失的问题
                 // 创建目标文件夹
                 if (!FtpDirectoryExists(destinationDirectoryPath))
                 {
                     FtpCreateDirectory(destinationDirectoryPath);
                 }


                 // 获取源文件夹内所有文件和子文件夹
                 var fileList = QueryAll(sourceDirectoryPath);
                 foreach (var fileInfo in fileList)
                 {
                     // 构建源文件和目标文件的完整路径
                     string sourcePath = sourceDirectoryPath + "/" + fileInfo.FileName;
                     string destinationPath = destinationDirectoryPath + "/" + fileInfo.FileName;

                     // 如果是文件夹，递归移动
                     if (fileInfo.FileType == "Folder")
                     {
                         FtpMoveFolder(sourcePath, destinationPath);
                     }
                     else
                     {
                         // 创建源文件的FTP请求
                         FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + sourcePath);
                         request.Method = WebRequestMethods.Ftp.Rename; // 使用重命名操作实现移动
                         request.Credentials = new NetworkCredential(userName, passWord);
                         request.RenameTo = destinationPath; // 设置重命名目标路径

                         // 发起请求并获取响应
                         using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                         {
                             Console.WriteLine($"文件 '{fileInfo.FileName}' 移动成功。");
                         }
                     }
                 }
                 if (!string.IsNullOrEmpty(sourceDirectoryPath))
                 {
                     // 删除源文件夹
                     FtpDeleteFolder(sourceDirectoryPath);
                 }

             });
        }

        #endregion

        #region 文件上传【文件批量上传&&文件夹上传】
        /// <summary>
        /// 上传文件到FTP服务器
        /// </summary>
        /// <param name="filePath">要上传的文件路径</param>
        /// <param name="directoryPath">目标文件夹路径（默认不传是根目录）</param>
        public virtual int FtpUploadFile(string filePath, string directoryPath = "")
        {
            return ExecuteFtpOperation(() =>
             {
                 if (!FtpDirectoryExists(directoryPath))
                 {
                     FtpCreateDirectory(directoryPath);
                 }
                 using (WebClient client = new WebClient())
                 {
                     client.Credentials = new NetworkCredential(userName, passWord);
                     client.UploadFile(ftpServer + directoryPath + "/" + Path.GetFileName(filePath), WebRequestMethods.Ftp.UploadFile, filePath);
                     Console.WriteLine($"{filePath}文件上传成功。");
                 }
             });

        }
        /// <summary>
        /// 递归上传文件夹到FTP服务器
        /// </summary>
        /// <param name="localFolderPath">本地文件夹路径</param>
        /// <param name="ftpDirectoryPath">FTP服务器目标文件夹路径</param>
        public virtual int FtpUploadFolder(string localFolderPath, string ftpDirectoryPath)
        {
            return ExecuteFtpOperation(() =>
            {
                // 检查本地文件夹是否存在
                if (!Directory.Exists(localFolderPath))
                {
                    throw new Exception("本地文件夹不存在。");
                    return;
                }

                // 获取文件夹名称
                string folderName = new DirectoryInfo(localFolderPath).Name;

                // 构建FTP服务器上的目标路径
                string rootPath = (string.IsNullOrEmpty(ftpDirectoryPath) ? "" : ftpDirectoryPath + "/");

                // 如果目标文件夹在FTP服务器上不存在，则创建
                if (!FtpDirectoryExists(rootPath + folderName))
                {
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

                // 逐个处理子文件夹
                foreach (string subDirectory in subDirectories)
                {
                    // 递归上传子文件夹
                    FtpUploadFolder(subDirectory, rootPath + folderName);
                }

                Console.WriteLine($"{localFolderPath} 文件夹上传成功。");
            });
        }

        /// <summary>
        /// 上传多个文件夹到FTP服务器
        /// </summary>
        /// <param name="filePath">要上传的文件夹路径(多文件夹用,号分隔)</param>
        /// <param name="ftpDirectoryPath">目标文件夹路径</param>
        public virtual int FtpUploadFolders(string localDirectories, string ftpDirectoryPath = "")
        {
            foreach (string localDirectory in localDirectories.Split(','))
            {
                FtpUploadFolder(localDirectory, ftpDirectoryPath);
            }
            return 1;
        }
        #endregion

        #region 文件下载
        /// <summary>
        /// 从FTP服务器下载文件到本地
        /// </summary>
        /// <param name="remoteFilePaths">要下载的远程文件路径(多文件.号分隔)</param>
        /// <param name="localDirectory">本地目录路径</param>
        public virtual int FtpDownloadFile(string remoteFilePaths, string localDirectory)
        {
            return ExecuteFtpOperation(() =>
             {
                // 检查本地路径是否存在，如果不存在则创建
                if (!Directory.Exists(localDirectory))
                 {
                     Directory.CreateDirectory(localDirectory);
                 }
                 using (WebClient client = new WebClient())
                 {
                     client.Credentials = new NetworkCredential(userName, passWord);

                     foreach (var remoteFilePath in remoteFilePaths.Split(','))
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
                             throw new Exception($"下载文件 '{fileName}' 时出错: {ex.Message}");

                         }
                     }
                 }
             });
        }

        /// <summary>
        /// 递归从FTP服务器下载文件夹到本地
        /// </summary>
        /// <param name="remoteDirectoryPath">要下载的远程文件夹路径</param>
        /// <param name="localDirectory">本地目录路径</param>
        public virtual int FtpDownloadFolder(string remoteDirectoryPath, string localDirectory)
        {
            return ExecuteFtpOperation(() =>
             {
                // 检查本地路径是否存在，如果不存在则创建
                if (!Directory.Exists(localDirectory))
                 {
                     Directory.CreateDirectory(localDirectory);
                 }
                // 获取远程文件夹内所有文件和子文件夹
                var fileList = QueryAll(remoteDirectoryPath);

                 foreach (var fileInfo in fileList)
                 {
                     string remotePath = remoteDirectoryPath + "/" + fileInfo.FileName;
                     string localPath = Path.Combine(localDirectory, fileInfo.FileName);

                     if (fileInfo.FileType == "Folder")
                     {
                        // 如果是文件夹，递归下载
                        string newLocalDirectory = Path.Combine(localDirectory, fileInfo.FileName);
                         Directory.CreateDirectory(newLocalDirectory);
                         FtpDownloadFolder(remotePath, newLocalDirectory);
                     }
                     else
                     {
                        // 如果是文件，下载到本地
                        using (WebClient client = new WebClient())
                         {
                             client.Credentials = new NetworkCredential(userName, passWord);
                             client.DownloadFile(ftpServer + remotePath, localPath);
                             Console.WriteLine($"文件 '{fileInfo.FileName}' 下载成功。");
                         }
                     }
                 }
             });
        }

        #endregion
    }

    /// <summary>
    /// 表示调用结果的类
    /// </summary>
    public class InvokeResult
    {
       
        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public dynamic Data { get; set; }

       
    }

}

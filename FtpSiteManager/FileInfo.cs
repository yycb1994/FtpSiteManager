
namespace FtpSiteManager
{
    using System;

    public class FileInfo
    {
        public DateTime LastModifiedDate { get; set; }
        public long FileSize { get; set; }
        public string FileType { get; set; }
        public string FileName { get; set; }

        public FileInfo(DateTime lastModifiedDate, long fileSize, string fileType, string fileName)
        {
            LastModifiedDate = lastModifiedDate;
            FileSize = fileSize;
            this.FileType = fileType;
            FileName = fileName;
            Console.WriteLine($"最后修改时间:{lastModifiedDate}，文件类型：{fileType}，文件大小：{fileSize}，文件名称：{fileName}");
        }

     
    }
  
}

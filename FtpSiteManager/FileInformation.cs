
namespace FtpSiteManager
{
    using System;

    /// <summary>
    /// 文件信息类
    /// </summary>
    public class FileInformation
    {
        /// <summary>
        /// 获取或设置最后修改日期
        /// </summary>
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// 获取或设置文件大小
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 获取或设置文件类型
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// 获取或设置文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 文件信息类的构造函数
        /// </summary>
        /// <param name="lastModifiedDate">最后修改日期</param>
        /// <param name="fileSize">文件大小</param>
        /// <param name="fileType">文件类型</param>
        /// <param name="fileName">文件名</param>
        public FileInformation(DateTime lastModifiedDate, long fileSize, string fileType, string fileName)
        {
            LastModifiedDate = lastModifiedDate;
            FileSize = fileSize;
            this.FileType = fileType;
            FileName = fileName;
            //Console.WriteLine($"最后修改时间:{lastModifiedDate}，文件类型：{fileType}，文件大小：{fileSize}，文件名称：{fileName}");
        }
    }


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FtpSiteManager
{
    public class FtpTest : FtpOperation
    {
        public FtpTest(string ftpServer, string userName, string passWord) : base(ftpServer, userName, passWord)
        {

        }
       
    }

    class Program
    {
        static void Main(string[] args)
        {
           


            // FTP 服务器地址
            string ftpServer = "ftp://127.0.0.1/";
            // FTP 服务器用户名
            string userName = "Administrator";
            // FTP 服务器密码
            string password = "123";

            FtpTest ftp = new FtpTest(ftpServer, userName, password);
            //ftp.QueryAll("/Template"); //查询
            ftp.FtpDeleteFolders("");//删除所有
            ftp.FtpUploadFolder("e:\\Template", "");//将文件夹的内容上传到根目录
            ftp.FtpUploadFolder(@"D:\GitCode\Blog.Core", "/gitCode/Blog.Core");//将本地文件夹的内容上传到指定目录
            var data = ftp.RecursiveQueryAll("");//查询所有文件信息
            ftp.FtpMoveFolder("/CoaTemplate", "/1/CoaTemplate");//文件夹移动
            ftp.FtpDownloadFolder("/1", "d:\\1\\");    //将ftp服务器的指定文件夹下载到本地目录

            //ftp.FtpMoveFiles("/1/CoaTemplate", "/CoaTemplate",new string[] { "明细.xlsx" });
        }
    }
}

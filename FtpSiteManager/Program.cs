using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FtpSiteManager
{
    class Program
    {
        static void Main(string[] args)
        {
            //Test Code
            /*
            string[] path = {
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Upload\FtpUploadFileConfig.json" ,
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Upload\FtpUploadFolderConfig.json" ,
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Upload\FtpUploadFoldersConfig.json" ,
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Query\QueryAllConfig.json" ,
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Query\RecursiveQueryAllConfig.json" ,
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Download\FtpDownloadFileConfig.json" ,
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Download\FtpDownloadFolderConfig.json" ,
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Move\FtpMoveFilesConfig.json" ,
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Move\FtpMoveFolderConfig.json" ,
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Delete\FtpDeleteFilesConfig.json" ,
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Delete\FtpDeleteFoldersConfig.json" ,
                @"D:\YcbCode\FtpSiteManager\FtpSiteManager\FtpInvokeConfig\Delete\FtpDeleteAllConfig.json" ,
            };

            foreach (var item in path)
            {
                FtpInvoke ftpInvoke = new FtpInvoke(item);
                var data = ftpInvoke.CallFtp();
            }*/
            FtpInvoke ftpInvoke = new FtpInvoke(args[0]);
            var data = ftpInvoke.CallFtp();
            Console.WriteLine(JsonConvert.SerializeObject(data));
        }
    }
}

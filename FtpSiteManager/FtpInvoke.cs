using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FtpSiteManager
{
    /// <summary>
    /// 表示用于调用FTP操作的类。
    /// </summary>
    public class FtpInvoke
    {
        private FtpInvokeParameter FtpInvokeParameter; // FTP调用参数
        private FtpHelper ftpHelper; // FTP操作辅助类

        /// <summary>
        /// FtpInvoke 类的构造函数。
        /// </summary>
        /// <param name="configPath">配置文件路径。</param>
        public FtpInvoke(string configPath)
        {
            // 从配置文件中读取JSON文本
            string jsonText = File.ReadAllText(configPath, Encoding.GetEncoding("GBK"));
            // 反序列化JSON文本为FtpInvokeParameter对象
            FtpInvokeParameter = JsonConvert.DeserializeObject<FtpInvokeParameter>(jsonText);
            // 初始化ftpHelper实例，使用FTP服务器地址、端口、用户名和密码
            ftpHelper = new FtpHelper(FtpInvokeParameter.FtpServer + ":" + FtpInvokeParameter.Port + "/", FtpInvokeParameter.UserName, FtpInvokeParameter.PassWord);
        }

        /// <summary>
        /// 调用FTP方法。
        /// </summary>
        /// <returns>调用结果。</returns>
        public InvokeResult CallFtp()
        {
            InvokeResult invokeResult = new InvokeResult(); // 创建一个调用结果对象
            try
            {
                MethodInfo methodInfo = typeof(FtpHelper).GetMethod(FtpInvokeParameter.MethodName); // 获取FTPHelper类中指定方法的MethodInfo对象
                object obj = null; // 用于存储方法调用的返回值

                // 根据方法参数数量调用对应的方法重载
                switch (methodInfo.GetParameters().Length)
                {
                    case 1:
                        obj = methodInfo.Invoke(ftpHelper, new object[] { FtpInvokeParameter.Parameter1 });
                        break;
                    case 2:
                        obj = methodInfo.Invoke(ftpHelper, new object[] { FtpInvokeParameter.Parameter1, FtpInvokeParameter.Parameter2 });
                        break;
                    case 3:
                        obj = methodInfo.Invoke(ftpHelper, new object[] { FtpInvokeParameter.Parameter1, FtpInvokeParameter.Parameter2, FtpInvokeParameter.Parameter3 });
                        break;
                    default:
                        obj = methodInfo.Invoke(ftpHelper, null);
                        break;
                }

                // 将方法调用的结果存储到InvokeResult对象的Data属性中
                invokeResult.Data = obj;
            }
            catch (Exception ex)
            {
                invokeResult.Message = ex.Message;
            }

            return invokeResult; // 返回调用结果
        }
    }

    /// <summary>
    /// 表示FTP调用所需的参数。
    /// </summary>
    public class FtpInvokeParameter
    {
        public string FtpServer { get; set; } // FTP服务器地址
        public string Port { get; set; } // FTP端口
        public string UserName { get; set; } // FTP用户名
        public string PassWord { get; set; } // FTP密码
        public string MethodName { get; set; } // 要调用的FTP操作方法名
        public dynamic Parameter1 { get; set; } // 方法参数1
        public dynamic Parameter2 { get; set; } // 方法参数2
        public dynamic Parameter3 { get; set; } // 方法参数3
    }

}

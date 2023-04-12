using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using System.Net.NetworkInformation;
using Server;

namespace HttpServer
{
    internal class HttpServer
    {
        private HttpListener listener;
        private string baseAddress;
        private int port;
        List<string> ipAddressList;
        public bool ServerRunning { get; set; }
        public HttpServer()
        {
            listener = new HttpListener();
            GetUnusedPort(ref port);
            baseAddress = "http://*:" + port  + "/";
            ipAddressList = GetLocalIPAddress();
        }

        /// <summary>
        /// 获取一个未使用的端口号
        /// </summary>
        /// <param name="port">返回结果</param>
        /// <param name="startPort">开始端口号</param>
        /// <returns>找到空闲的端口返回true，如果所有端口都被占用，则返回false</returns>
        private static bool GetUnusedPort(ref int port, int startPort = 1000)
        {
            var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            for(int i = startPort; i <= 65535; i++)
            {
                if(listeners.Any(x=>x.Port == i))
                {
                    continue;
                }
                port = i;
                return true;
            }
            return false;
        }


        /// <summary>
        /// 获取所有网络接口的地址列表
        /// </summary>
        /// <returns></returns>
        public static List<string> GetLocalIPAddress()
        {
            // 获取本地计算机上的网络接口
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            List<String> ipList = new List<string>();
            foreach (NetworkInterface ni in interfaces)
            {
                //未连接的接口
                if(ni.OperationalStatus == OperationalStatus.Down)
                {
                    continue;
                }
                // 排除非以太网、无线网络、虚拟网络等接口
                //if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                //    ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                //    ni.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet ||
                //    ni.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx ||
                //    ni.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
                //    ni.NetworkInterfaceType == NetworkInterfaceType.Ppp ||
                //    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                //    ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                //    ni.NetworkInterfaceType == NetworkInterfaceType.GenericModem ||
                //    ni.NetworkInterfaceType == NetworkInterfaceType.Wman ||
                //    ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit ||
                //    ni.NetworkInterfaceType == NetworkInterfaceType.Slip)
                //{
                //    continue;
                //}
                
                // 获取IP属性集合
                IPInterfaceProperties ipProperties = ni.GetIPProperties();

                // 获取IPv4地址集合
                var ips = ipProperties.UnicastAddresses;

                foreach (var ip in ips)
                {
                    // 获取IPv4地址
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        ipList.Add(ip.Address.ToString());
                    }
                }
            }

            return ipList;
        }


        /// <summary>
        /// 开始服务器
        /// </summary>
        public async void Start()
        {
            listener.Prefixes.Add(baseAddress);
            listener.Start();
            this.ServerRunning = true;
            //Dictionary<string, string> dictParam = new Dictionary<string, string>()
            //{
            //    {"host", Environment.MachineName},
            //    {"ip", string.Join(",",ipAddressList)},
            //    {"port", port.ToString()}
            //};
            //FormUrlEncodedContent content = new FormUrlEncodedContent(dictParam!);

            Logger.AddLog($"服务器已运行，端口号：{port}");
            while (true)
            {

                HttpListenerContext context = await listener.GetContextAsync();
                
                var response = context.Response;
                //System.Diagnostics.Trace.WriteLine("当前 response 对象地址：" + response.GetHashCode().ToString("X"));
                //向 Headers 里面添加，不然会提示跨域请求无效之类的错误
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                //下面通过 Header 向客户端返回文件名，如果不设置此Header，则客户端无法读取到文件名
                response.Headers.Add("Access-Control-Expose-Headers:Content-Disposition");

                //获取请求的URL地址
                var requestUrl = context.Request.Url;

                Logger.AddLog("收到请求：" + requestUrl);

                if(requestUrl?.AbsolutePath == "/path/get/computer") //获取所有驱动器列表
                {
                    DriveItem[] driveItems = GetDriveInfo();
                    var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(driveItems));
                    response.ContentLength64 = buffer.LongLength;
                    response.ContentType = "application/json";
                    var output = response.OutputStream;
                    await output.WriteAsync(buffer.AsMemory(0, buffer.Length));
                    output.Close();
                }
                else if(requestUrl?.AbsolutePath == "/path/get")
                {
                    if (requestUrl.Query.StartsWith("?target="))
                    {
                        var target = HttpUtility.UrlDecode(requestUrl.Query.Substring(8));
                        //如果目标是一个文件，那么直接将该文件写入流
                        if (File.Exists(target))
                        {
                            byte[] fileBytes = File.ReadAllBytes(target);
                            
                            response.ContentType = "application/octet-stream";
                            //如果文件名中有中文或其他特殊字符，这个方法将会出现错误，所以要使用HttpUtility.UrlEncode()方法
                            //向客户端返回文件名
                            response.AddHeader("Content-Disposition", "attachment;filename=" + 
                                HttpUtility.UrlEncode(Path.GetFileName(target)));

                            //写入响应流
                            response.ContentLength64 = fileBytes.LongLength;
                            response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                            response.OutputStream.Close();
                            response.Close();
                        }
                        else if(Directory.Exists(target)) //文件夹
                        {
                            FolderViewResult r = new FolderViewResult();
                            r.Dir = target;
                            r.Items = new List<FileItem>();
                            try
                            {
                                string[] fs = Directory.GetFileSystemEntries(target);
                                for (int i = 0; i < fs.Length; i++)
                                {
                                    FileItem fi = new FileItem();
                                    fi.IsFile = File.Exists(fs[i]);
                                    if (fi.IsFile)
                                    {
                                        fi.Size = GetFileSize(fs[i], out char c);
                                        fi.Unit = c;
                                    }
                                    fi.Name = fs[i];
                                    r.Items.Add(fi);
                                }
                            }
                            catch(Exception ex)
                            {
                                r.Code = 500;
                                r.Message = ex.Message;
                            }

                            var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(r));
                            //发送响应
                            response.ContentLength64 = buffer.LongLength;
                            response.ContentType = "text/html";
                            response.StatusCode = 200;
                            var output = response.OutputStream;
                            await output.WriteAsync(buffer.AsMemory(0, buffer.Length));
                            output.Close();
                        }
                        else
                        {
                            //OperationResult r = new OperationResult();
                            //r.Code = 404;
                            //r.Message = "没有找到指定的文件：" + target;
                            response.StatusCode = 404;
                            response.Close();
                        }
                    }
                }
                else if(requestUrl?.AbsolutePath == "/action/delete")
                {
                    if (requestUrl.Query.StartsWith("?file="))
                    {
                        OpResult res = new OpResult();
                        var target = HttpUtility.UrlDecode(requestUrl.Query.Substring(6));
                        if (File.Exists(target))
                        {
                            try
                            {
                                File.Delete(target);
                            }
                            catch(Exception ex)
                            {
                                res.Code = 500;
                                res.Message = ex.Message;
                            }
                        }
                        else
                        {
                            res.Code = 501;
                            res.Message = $"文件{target}不存在。";
                        }

                        var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(res));
                        response.ContentLength64 = buffer.LongLength;
                        response.ContentType = "application/json";
                        var output = response.OutputStream;
                        await output.WriteAsync(buffer.AsMemory(0, buffer.Length));
                        output.Close();
                    }
                    else
                    {
                        response.StatusCode = 600;
                    }
                }
                else if(requestUrl?.AbsolutePath == "/action/up") //返回到上一级目录
                {
                    FolderViewResult res = new FolderViewResult();
                    if (requestUrl.Query.StartsWith("?current="))
                    {
                        var current = HttpUtility.UrlDecode(requestUrl.Query.Substring(9));
                        if (string.IsNullOrEmpty(current))
                        {
                            res.Code = 501;
                            res.Message = "当前路径为空";
                        }
                        else
                        {
                            DirectoryInfo? parent = Directory.GetParent(current);
                            if (parent == null)
                            {
                                res.Code = 501;
                                res.Message = "当前正处于根目录中";
                            }
                            else
                            {
                                res.Dir = parent.FullName;
                                res.Items = new List<FileItem>();
                                try
                                {
                                    string[] fs = Directory.GetFileSystemEntries(parent.FullName);
                                    for (int i = 0; i < fs.Length; i++)
                                    {
                                        FileItem fi = new FileItem();
                                        fi.IsFile = File.Exists(fs[i]);
                                        if (fi.IsFile)
                                        {
                                            fi.Size = GetFileSize(fs[i], out char c);
                                            fi.Unit = c;
                                        }
                                        fi.Name = fs[i];
                                        res.Items.Add(fi);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    res.Code = 500;
                                    res.Message = ex.Message;
                                }
                            }
                        }
                        var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(res));
                        //发送响应
                        response.ContentLength64 = buffer.LongLength;
                        response.ContentType = "application/json";
                        response.StatusCode = 200;
                        var output = response.OutputStream;
                        await output.WriteAsync(buffer.AsMemory(0, buffer.Length));
                        output.Close();
                    }
                    else
                    {
                        
                    }
                }
            }
        }

        /// <summary>
        /// 判断是否是微信图片
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool IsWeChatImage(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            if(ext == ".dat" && fileName.Contains("WeChat Files"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取所有驱动器列表
        /// </summary>
        /// <returns></returns>
        private DriveItem[] GetDriveInfo()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            DriveItem[] items = new DriveItem[drives.Length];
            for(int i = 0; i < drives.Length; i++)
            {
                DriveItem item = new DriveItem();
                if (!drives[i].IsReady)
                {
                    item.Online = false;
                    item.Name = drives[i].Name;
                    item.Type = Enum.GetName(typeof(DriveType), drives[i].DriveType) ?? "";
                    items[i] = item;
                    continue;
                }
                item.Online = true;
                item.Free = drives[i].AvailableFreeSpace;
                item.Size = drives[i].TotalSize;
                item.Type = Enum.GetName(typeof(DriveType), drives[i].DriveType) ?? "";
                item.Label = drives[i].VolumeLabel;
                item.Format = drives[i].DriveFormat;
                item.Name = drives[i].Name;
                items[i] = item;
            }
            return items;
        }


        /// <summary>
        /// 获取指定文件的大小
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="unit">大小单位</param>
        /// <returns></returns>
        private static double GetFileSize(string fileName, out char unit)
        {
            FileInfo fs = new FileInfo(fileName);
            double size;
            if(fs.Length < 1024)
            {
                unit = 'B';
                size = fs.Length;
            }
            else if(fs.Length >= 1024 && fs.Length < (1048576))
            {
                size = Math.Round(fs.Length / 1024.0, 2);
                unit = 'K';
            }
            else
            {
                unit = 'M';
                size = Math.Round(fs.Length / 1048576.0, 2);
            }
            return size;
        }
    }


    /// <summary>
    /// 文件夹操作结果
    /// </summary>
    struct FolderViewResult
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 当前文件夹路径
        /// </summary>
        public string Dir { get; set; }
        /// <summary>
        /// 文件列表
        /// </summary>
        public List<FileItem> Items { get; set; }
    }

    /// <summary>
    /// 通用操作结果
    /// </summary>
    struct OpResult
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 驱动器信息
    /// </summary>
    struct DriveItem
    {
        /// <summary>
        /// 驱动器名称。比如：C:\
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 驱动器格式，比如：NTFS
        /// </summary>
        public string Format { get; set; }
        /// <summary>
        /// 卷标
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// 类型。比如：Network，Fixed
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 总容量
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// 剩余空间
        /// </summary>
        public long Free { get; set; }
        /// <summary>
        /// 是否在线
        /// </summary>
        public bool Online { get; set; }

    }

    /// <summary>
    /// 文件信息
    /// </summary>
    struct FileItem
    {
        public bool IsFile { get; set; }
        public string Name { get; set; }
        public double Size { get; set; }
        public char Unit { get; set; }
    }
}

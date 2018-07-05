using Gecko;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Jack.JSUnitTest
{
    public class JsRunner:IDisposable
    {
        static string ServerUrl;
        static string WebRoot;
        /// <summary>
        /// 启动web服务器
        /// </summary>
        /// <param name="webFolder">web服务器指向的文件夹，一般为测试工程所在解决方案的文件夹路径</param>
        /// <returns>返回服务器url</returns>
        public static string StartVirtualWebServer(string webFolder)
        {
            if (ServerUrl != null)
                throw new Exception("不要重复启动服务器");

            WebRoot = webFolder;
            Xpcom.Initialize("Firefox");
            int port = 0;
            for (int i = 901; i < 1200; i++)
            {
                try
                {
                    Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect("127.0.0.1", i);
                    socket.Dispose();
                }
                catch
                {
                    port = i;
                    break;
                }
            }
            Way.Lib.ScriptRemoting.ScriptRemotingServer.RegisterHandler(new VisitHandler());
            Way.Lib.ScriptRemoting.ScriptRemotingServer.Start(port, webFolder, 0);
            ServerUrl = $"http://localhost:{port}/";
            return ServerUrl;
        }

        /// <summary>
        /// 停止web服务器
        /// </summary>
        public static void StopVirtualWebServer()
        {
            Way.Lib.ScriptRemoting.ScriptRemotingServer.Stop();
        }

        static List<JsFileReference> ReferenceConfigs = new List<JsFileReference>();
        static List<string> GlobalJSFiles = new List<string>();
        static List<Type> SimulatorTypes = new List<Type>();
        bool _runJsOnWindowLoad = false;

        /// <summary>
        /// 需要放置在页面上的html代码
        /// </summary>
        public string Html { get; set; }

        /// <summary>
        /// 设置js文件依赖
        /// </summary>
        /// <param name="jsFile">js文件路径</param>
        /// <param name="references">所依赖的js文件路径</param>
        public static void SetJsReference(string jsFile,IEnumerable<string> references)
        {
            ReferenceConfigs.Add(new JsFileReference() {
                JsFile = jsFile,
                References = references,
            });
        }
        /// <summary>
        /// 添加全局js文件
        /// </summary>
        /// <param name="jsFile"></param>
        public static void RegisterGlobalJsFile(string jsFile)
        {
            GlobalJSFiles.Add(jsFile);
        }

        /// <summary>
        /// 注册模拟器
        /// </summary>
        /// <param name="simulatorType">模拟器类型，必须实现ISimulator接口</param>
        public static void RegisterSimulator(Type simulatorType)
        {
            if (simulatorType.GetInterface(typeof(ISimulator).FullName) == null)
                throw new Exception("simulatorType必须实现ISimulator接口");
            SimulatorTypes.Add(simulatorType);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="runJsOnWindowLoad">是否在window.onload后，再运行js代码</param>
        public JsRunner(bool runJsOnWindowLoad = false)
        {
            if(ServerUrl == null)
            {
                throw new Exception("请先使用静态函数JsRunner.StartVirtualWebServer开启一个服务器");
            }
            _runJsOnWindowLoad = runJsOnWindowLoad;
            JsFiles.AddRange(GlobalJSFiles);
        }
        ~JsRunner()
        {
            Dispose();
        }
        List<string> JsFiles = new List<string>();
        /// <summary>
        /// 添加js文件
        /// </summary>
        /// <param name="jsPath">js文件路径</param>
        public void AddJsFile(string jsPath)
        {
            putJsFileContentToList(jsPath);
           
        }

        void putJsFileContentToList(string jsPath)
        {
            if (JsFiles.Contains(jsPath) )
            {
                return;
            }
            //查找该js是否引用其他js
            var arr = ReferenceConfigs.Where(m => string.Equals(m.JsFile, jsPath, StringComparison.CurrentCultureIgnoreCase));
            foreach( var item in arr )
            {
                foreach( var path in item.References )
                {
                    putJsFileContentToList(path);
                }
            }
            JsFiles.Add(jsPath);
        }

       

        /// <summary>
        /// 运行js代码
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="jsCode">一段js代码。如：return data.name;</param>
        /// <param name="data">传到js里面的对象，js可以通过data.*直接使用此参数</param>
        /// <returns></returns>
        public T Run<T>(string jsCode, object data)
        {
            var gecko = new GeckoWebBrowser();
            gecko.CreateControl();

            //加入apicloud.db模拟器
            List<ISimulator> simulators = new List<ISimulator>();
            foreach( var t in SimulatorTypes)
            {
                var obj = (ISimulator)Activator.CreateInstance(t);
                obj.Init(gecko);
                simulators.Add(obj);
            }  

            bool loadFinished = false;

            gecko.NavigationError += (s, e) =>
            {
               
            };
            gecko.NSSError += (s, e) =>
            {

            };
            gecko.DocumentCompleted += (s, e) => {
                loadFinished = true;
            };
            using (System.IO.MemoryStream ms = new MemoryStream())
            {
                System.IO.StreamWriter sw = new StreamWriter(ms, Encoding.UTF8);

                sw.WriteLine("<!DOCTYPE html>");
                sw.WriteLine("<html>");


                sw.WriteLine("<body>");

                foreach (var simulator in simulators)
                {
                    simulator.OnWritingScript(sw);
                }
                foreach (var path in JsFiles)
                {
                    if (path[1] == ':')
                    {
                        if (File.Exists(path) == false)
                            throw new Exception($"文件{path}不存在");
                        sw.WriteLine("<script src=\"file:///" + path + "\" type=\"text/javascript\"></script>");
                    }
                    else
                    {
                        sw.WriteLine("<script src=\"" + path + "\" type=\"text/javascript\"></script>");
                    }
                }

                if(!string.IsNullOrEmpty(this.Html ))

                {
                    sw.WriteLine(this.Html);
                }

                string result;

                if (_runJsOnWindowLoad)
                {
                    sw.WriteLine("</body>");
                    sw.WriteLine("</html>");
                    sw.Dispose();

                    var pageid = Guid.NewGuid().ToString("N");
                    VisitHandler.HtmlContents.TryAdd(pageid, ms.ToArray());
                    ms.Dispose();
                    gecko.Navigate($"{ServerUrl}Jack.JSUnitTest?id={pageid}");

                    while (!loadFinished)
                    {
                        System.Threading.Thread.Sleep(10);
                        System.Windows.Forms.Application.DoEvents();
                    }

                    var jsContext = new AutoJSContext(gecko.Window);


                    var js = @"
(function(d){
     var result = (function(data){

try{
        " + jsCode + @"
    }
catch(e)
    {
        if(typeof e === 'string')
            return { ______err : e , line:0 };
        else
        {
            var msg = e.message;
            if(e.name)
                msg = e.name + ',' + msg;
            return { ______err :msg , line:e.lineNumber };
         }
    }
})(d);
     return JSON.stringify(result);

    
})(" + (data == null ? "null" : Newtonsoft.Json.JsonConvert.SerializeObject(data)) + @");
";


                    jsContext.EvaluateScript(js, out result);
                    jsContext.Dispose();
                }
                else
                {
                    var guidName = "v" + Guid.NewGuid().ToString("N");
                    var js = @"
<script lang='ja'>
var " + guidName + @" = (function(data){

try{
        " + jsCode + @"
    }
catch(e)
    {
        if(typeof e === 'string')
            return { ______err : e , line:0 };
        else
        {
            var msg = e.message;
            if(e.name)
                msg = e.name + ',' + msg;
            return { ______err :msg , line:e.lineNumber };
         }
    }
})(" + Newtonsoft.Json.JsonConvert.SerializeObject(data) + @");
" + guidName + @"=JSON.stringify(" + guidName + @");
</script>
";

                    sw.WriteLine(js);
                    sw.WriteLine("</body>");
                    sw.WriteLine("</html>");
                    sw.Dispose();

                    var pageid = Guid.NewGuid().ToString("N");
                    VisitHandler.HtmlContents.TryAdd(pageid, ms.ToArray());
                    ms.Dispose();
                    gecko.Navigate($"{ServerUrl}Jack.JSUnitTest?id={pageid}");

                    while (!loadFinished)
                    {
                        System.Threading.Thread.Sleep(10);
                        System.Windows.Forms.Application.DoEvents();
                    }

                    var jsContext = new AutoJSContext(gecko.Window);
                    js = guidName;
                    jsContext.EvaluateScript(js, out result);
                    jsContext.Dispose();
                }
                gecko.Dispose();

                if (result.StartsWith("{\"______err\":"))
                {
                    var errObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(result);
                    string errMsg = errObj.Value<string>("______err");
                    //int lineNumber = errObj.Value<int>("line") - 3;
                    throw new Exception(errMsg);
                }
                if (result.StartsWith("\"") == false && typeof(T) == typeof(string))
                    return (T)(object)result;
                if (result == "undefined")
                    return default(T);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(result);
            }
        }

        /// <summary>
        /// 读取js文件内容
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        string readJsFile(string filePath)
        {
            using (System.IO.FileStream fs = System.IO.File.OpenRead(filePath))
            {
                var data = new byte[fs.Length];
                var isUtf8 = IsUTF8(fs);
                fs.Position = 0;
                fs.Read(data, 0, data.Length);
                if (isUtf8)
                { 
                    return Encoding.UTF8.GetString(data);
                }
                else
                {
                    return Encoding.GetEncoding("gb2312").GetString(data);
                }
            }
        }

        /// <summary>
        /// 判断流是否是utf-8编码
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        static bool IsUTF8(Stream stream)
        {
            bool IsUTF8 = true;

            while (stream.Position < stream.Length)
            {
                byte b = (byte)stream.ReadByte();
                if (b < 0x80) // (10000000): 值小于0x80的为ASCII字符    
                {

                }
                else if (b < (0xC0)) // (11000000): 值介于0x80与0xC0之间的为无效UTF-8字符    
                {
                    IsUTF8 = false;
                    break;
                }
                else if (b < (0xE0)) // (11100000): 此范围内为2字节UTF-8字符    
                {
                    if (stream.Position >= stream.Length - 1)
                    {
                        break;
                    }
                    byte nextByte = (byte)stream.ReadByte();
                    if ((nextByte & (0xC0)) != 0x80)
                    {
                        IsUTF8 = false;
                        break;
                    }
                }
                else if (b < (0xF0)) // (11110000): 此范围内为3字节UTF-8字符    
                {
                    if (stream.Position >= stream.Length - 2)
                    {
                        break;
                    }

                    byte nextByte1 = (byte)stream.ReadByte();
                    byte nextByte2 = (byte)stream.ReadByte();
                    if ((nextByte1 & (0xC0)) != 0x80 || (nextByte2 & (0xC0)) != 0x80)
                    {
                        IsUTF8 = false;
                        break;
                    }
                }
                else
                {
                    IsUTF8 = false;
                    break;
                }
            }

            return IsUTF8;

        }

        public void Dispose()
        {
            JsFiles.Clear();
        }
    }

    class JsFileReference
    {
        /// <summary>
        /// js文件
        /// </summary>
        public string JsFile;
        /// <summary>
        /// 所依赖的js文件
        /// </summary>
        public IEnumerable<string> References;
        
    }

}

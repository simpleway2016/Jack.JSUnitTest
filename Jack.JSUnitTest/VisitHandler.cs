using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Way.Lib.ScriptRemoting;

namespace Jack.JSUnitTest
{
    class VisitHandler : Way.Lib.ScriptRemoting.ICustomHttpHandler
    {
        public static ConcurrentDictionary<string, byte[]> HtmlContents = new ConcurrentDictionary<string, byte[]>();
        public void Handle(string originalUrl, HttpConnectInformation connectInfo, ref bool handled)
        {
            if(originalUrl.EndsWith("Jack.JSUnitTest"))
            {
                handled = true;
                string id = connectInfo.Request.Query["id"];
                byte[] content;
                HtmlContents.TryRemove(id, out content);
 
                connectInfo.Response.ContentLength = content.Length;
                connectInfo.Response.Write(content);
            }
        }
    }
}

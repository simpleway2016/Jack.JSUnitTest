using Gecko;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jack.JSUnitTest
{
    public interface ISimulator
    {
        /// <summary>
        /// 初始化模拟器
        /// </summary>
        /// <param name="gecko"></param>
        void Init(GeckoWebBrowser gecko);

        /// <summary>
        /// 往页面输出html页面时触发
        /// </summary>
        /// <param name="sw"></param>
        void OnWritingScript(System.IO.StreamWriter sw);
    }
}

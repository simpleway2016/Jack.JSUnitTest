using System;
using System.Collections.Generic;
using Jack.JSUnitTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        static UnitTest1()
        {

        }

        [TestMethod]
        public void TestMethod1()
        {
            var jsCode = @"
return { a : data.value + 2 , b : getTime() };
";
            using (var runner = new JsRunner())
            {
                runner.AddJsFile(AppDomain.CurrentDomain.BaseDirectory + @"\..\..\JavaScript1.js");
                var value = runner.Run<Dictionary<string,string>>(jsCode, new { value = 3 });
                if (value["a"] != "5")
                    throw new Exception("结果错误");
            }
        }
    }
}

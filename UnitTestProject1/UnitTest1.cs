using System;
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
function test(v)
{
    return v + 2;
}
return test(data.value);
";
            using (var runner = new JsRunner())
            {
                var value = runner.Run<int>(jsCode, new { value = 3 });
                if (value != 5)
                    throw new Exception("结果错误");
            }
        }
    }
}

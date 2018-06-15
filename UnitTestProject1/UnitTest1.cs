﻿using System;
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
return data.value + 2;
";
            using (var runner = new JsRunner())
            {
                //添加依赖的js文件
                runner.AddJsFile(AppDomain.CurrentDomain.BaseDirectory + @"\..\..\JavaScript1.js");

                var value = runner.Run<int>(jsCode, new { value = 3 });
                if (value != 5)
                    throw new Exception("结果错误");
            }
        }
    }
}

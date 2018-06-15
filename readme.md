```code
单元测试例子：

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
```
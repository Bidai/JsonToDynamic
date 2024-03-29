# JsonToDynamic
Json格式字符串与C#对象相互转换

## 示例代码：
```C#
namespace JsonToDynamic
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Json格式转换测试\n");

            dynamic v = JsonParser.FromJson("{\"value\":314,\"array\":[\"123\",true]}");
            double d = v["value"];
            bool t = v["array"][1];

            Console.WriteLine("v [\"value\"] = " + d);
            Console.WriteLine("v [\"array\"] [1] = " + t);
            Console.WriteLine("v = " + JsonParser.ToJson(v));

            Console.WriteLine("ToJson(new Dictionary<int,int>{{1,2}}) -> " + JsonParser. ToJson(new Dictionary<int, int> { { 1, 2 } }));
            Console.WriteLine("ToJson(FromJson(\"{1:2}\")) -> " + JsonParser.ToJson(JsonParser.FromJson("{1:2}")));
            Console.Read();
        }
    }
}
```

### 输出结果

```
Json格式转换测试

v ["value"] = 314
v ["array"] [1] = True
v = {"value":314,"array":["123",ture]}
ToJson(new Dictionary<int,int>{{1,2}}) -> {1:2}
ToJson(FromJson("{1:2}")) -> {"1":2}
```

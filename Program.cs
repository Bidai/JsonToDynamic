using System;

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

            Console.Read();
        }
    }
}

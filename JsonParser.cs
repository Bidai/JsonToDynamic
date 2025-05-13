using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace JsonToDynamic
{
    public static class JsonParser
    {
        /// <summary>
        /// 将Json字符串解析为C#中的对象
        /// </summary>
        /// <param name="str">Json格式的字符串</param> 
        /// <returns>返回一个Dictionary<string,dynamic>或者dynamic[]对象</returns>
        static public dynamic FromJson(this string str)
        {
            return FromJson(str, 0, str.Length);
        }

        /// <summary>
        /// 将Json字符串解析为C#中的对象
        /// </summary>
        /// <param name="str">Json格式的字符串</param>
        /// <param name="startIndex">字符串中Json格式的起始位置</param>
        /// <param name="length">字符串中Json字符串的长度</param>
        /// <returns>返回一个Dictionary<string,dynamic>或者dynamic[]对象</returns>
        static public dynamic FromJson(this string str, int startIndex, int length)
        {
            if (startIndex >= str.Length || startIndex < 0) return null;
            if (length < 0 || length + startIndex > str.Length) return null;
            //忽略开头的空白字符
            while (length > 0 && char.IsWhiteSpace(str[startIndex]))
            {
                startIndex++;
                length--;
            }
            //如果全是空白字符，则返回null
            if (length == 0)
                return null;
            //对于最外层为 [ ] 的Json格式字符串，将返回数组
            if (str[startIndex] == '[')
            {
                return ArrayFromJson(str, startIndex, ref length);
            }
            //对于最外层为 { } 的Json格式字符串，将返回字典
            if (str[startIndex]=='{')return DictionaryFromJson(str, startIndex, ref length);
            //字符串
            if (str[startIndex] == '"') {
                StringBuilder tmp = null;
                int end = startIndex + length;
                //记录字符串起始
                int lastIndex = startIndex + 1;
                int i;
                for (i = lastIndex; i < end; ++i)
                {
                    //双引号内部
                    char ch = str[i];
                    //存在转义
                    if (ch == '\\')
                    {
                        var strtmp = str.Substring(lastIndex, i - lastIndex);
                        tmp = new StringBuilder();
                        i = JsonStringConvert(ref tmp, str, i, end);
                        return strtmp + tmp.ToString();
                    }
                    //遇到双引号
                    else if (ch == '\"')
                    {
                        return str.Substring(lastIndex, i - lastIndex);
                    }
                }
                return str.Substring(lastIndex, length - 1);
            }
            return DynamicParse(str.Substring(startIndex, length));
        }
        /// <summary>
        /// Json格式中的字符串值识别
        /// </summary>
        /// <param name="tmp">StringBuilder对象，用以输出识别结果</param>
        /// <param name="str">被识别的字符串</param>
        /// <param name="startIndex">识别字符串的起始索引位置</param>
        /// <param name="end">识别字符串的终止索引位置（该位置不识别）</param>
        /// <returns>返回end</returns>
        static public int JsonStringConvert(ref StringBuilder tmp, string str, int startIndex, int end)
        {
            tmp.Clear();
            //标记是否在转义符号‘\’的后面
            bool cvt = false;
            for (int i = startIndex; i < end; ++i)
            {
                char ch = str[i];
                if (cvt)
                {//转义识别
                    switch (ch)
                    {
                        case 'b':
                            tmp.Append('\b');
                            break;
                        case 'r':
                            tmp.Append('\r');
                            break;
                        case 'n':
                            tmp.Append('\n');
                            break;
                        case 't':
                            tmp.Append('\t');
                            break;
                        case 'f':
                            tmp.Append('\f');
                            break;
                        case 'x'://非标准
                            tmp.Append((char)int.Parse(str.Substring(i + 1, 2), System.Globalization.NumberStyles.HexNumber));
                            i += 2;
                            break;
                        case 'u':
                            tmp.Append((char)int.Parse(str.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber));
                            i += 4;
                            break;
                        default:
                            tmp.Append(ch);
                            break;
                    }
                    cvt = false;
                }
                else
                {
                    if (ch == '\\')
                    {
                        cvt = true;
                    }
                    else if (ch == '\"')
                    {
                        return i;
                    }
                    else
                    {
                        tmp.Append(ch);
                    }
                }
            }
            return end;
        }
        /// <summary>
        /// 将Json字符串解析为Dictionary<string, dynamic>对象。
        /// </summary>
        /// <param name="str">被解析的字符串，要求其中指定的Json内容最外层是 { } </param>
        /// <param name="startIndex">被解析字符串中Json内容的起始索引位置</param>
        /// <param name="length">被解析字符串中Json内容的长度</param>
        /// <returns>返回一个Dictionary<string,dynamic>对象</returns>
        static public Dictionary<string, dynamic> DictionaryFromJson(string str, int startIndex, ref int length)
        {
            StringBuilder tmp = new StringBuilder();
            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            int end = startIndex + length;
            //记录字典的key
            string key = null;
            //临时字符串，用以记录值,
            string strtmp = null;
            //记录是否在引号内
            bool inq = false;
            //记录是否是字符串
            bool isstr = false;
            //记录子成员
            dynamic sub = null;
            //记录字符串起始
            int lastIndex = startIndex + 1;
            int i;
            for (i = lastIndex; i < end; ++i)
            {
                char ch = str[i];
                //双引号内部
                if (inq)
                {
                    //存在转义
                    if (ch == '\\')
                    {
                        strtmp = str.Substring(lastIndex, i - lastIndex);
                        i = JsonStringConvert(ref tmp, str, i, end);
                        strtmp += tmp.ToString();
                        inq = false;
                    }
                    //遇到双引号
                    else if (ch == '\"')
                    {
                        if (strtmp == null)
                            strtmp = str.Substring(lastIndex, i - lastIndex);
                        //状态切换为双引号外部
                        inq = false;
                    }
                }
                else//双引号外部
                {
                    //字符串起始
                    if (ch == '\"')
                    {
                        //状态切换到双引号内部
                        inq = true;
                        //判断为字符串变量
                        isstr = true;
                        //记录字符串起始位置
                        lastIndex = i + 1;
                        //清空临时字符串
                        strtmp = null;
                    }
                    //遇到 : 则确定前面的内容为key
                    else if (ch == ':')
                    {
                        if (strtmp == null)
                            key = str.Substring(lastIndex, i - lastIndex).Trim();
                        else
                            key = strtmp;
                        strtmp = null;
                        isstr = false;
                        lastIndex = i + 1;
                    }
                    //遇到 [ ，则按内部数组进行解析
                    else if (ch == '[')
                    {
                        length = end - i;
                        sub = ArrayFromJson(str, i, ref length);
                        i += length - 1;
                    }
                    //遇到 { ，则按内部字典进行解析
                    else if (ch == '{')
                    {
                        length = end - i;
                        sub = DictionaryFromJson(str, i, ref length);
                        i += length - 1;
                    }
                    //遇到 , 或者 } ，则对解析进行一个小结
                    else if (ch == ',' || ch == '}')
                    {
                        //如果解析到子节点
                        if (sub != null)
                        {
                            result.Add(key, sub);
                            sub = null;
                        }
                        //否则
                        else
                        {
                            //如果遇到字符串变量
                            if (isstr)
                            {
                                result.Add(key, strtmp);
                                isstr = false;
                            }
                            else
                            {
                                string t = str.Substring(lastIndex, i - lastIndex);
                                //根据内容识别为null、bool类型、double值
                                result.Add(key, DynamicParse(t));
                            }
                        }
                        //遇到 } 时完成解析
                        if (ch == '}')
                        {
                            ++i;
                            break;
                        }
                    }
                }
            }
            length = i - startIndex;
            return result;
        }

        /// <summary>
        /// 将Json字符串解析为dynamic[]对象。
        /// </summary>
        /// <param name="str">被解析的字符串，要求其中指定的Json内容最外层是 [ ] </param>
        /// <param name="startIndex">被解析字符串中Json内容的起始索引位置</param>
        /// <param name="length">被解析字符串中Json内容的长度</param>
        /// <returns>返回一个dynamic[]对象</returns>
        static public dynamic[] ArrayFromJson(string str, int startIndex, ref int length)
        {
            int end = startIndex + length;
            StringBuilder tmp = new StringBuilder();
            //记录多个成员
            List<dynamic> result = new List<dynamic>();
            //临时字符串，用以记录值
            string strtmp = null;
            //记录是否在引号内
            bool inq = false;
            //记录是否为字符串
            bool isstr = false;
            //记录子成员
            dynamic sub = null;
            //记录字符串起始
            int lastIndex = startIndex + 1;
            int i;
            for (i = lastIndex; i < end; ++i)
            {
                char ch = str[i];
                //双引号内部
                if (inq)
                {
                    //遇到转义
                    if (ch == '\\')
                    {
                        strtmp = str.Substring(lastIndex, i - lastIndex);
                        i = JsonStringConvert(ref tmp, str, i, end);
                        strtmp += tmp.ToString();
                        inq = false;
                    }
                    //遇到引号
                    else if (ch == '\"')
                    {
                        if (strtmp == null) strtmp = str.Substring(lastIndex, i - lastIndex);
                        inq = false;
                    }
                }
                //双引号外部
                else
                {
                    //遇到双引号
                    if (ch == '\"')
                    {
                        inq = true;
                        isstr = true;
                        lastIndex = i + 1;//记录字符串起始位置
                        strtmp = null;
                    }
                    //遇到 [
                    else if (ch == '[')
                    {
                        length = end - i;
                        sub = ArrayFromJson(str, i, ref length);
                        i += length - 1;
                    }
                    //遇到 { 
                    else if (ch == '{')
                    {
                        length = end - i;
                        sub = DictionaryFromJson(str, i, ref length);
                        i += length - 1;
                    }
                    //遇到 , 或者 ] 
                    else if (ch == ',' || ch == ']')
                    {
                        if (sub != null)
                        {
                            result.Add(sub);
                            sub = null;
                        }
                        else
                        {
                            if (isstr)
                            {
                                result.Add(strtmp);
                                isstr = false;
                            }
                            else
                            {
                                string t = str.Substring(lastIndex, i - lastIndex);
                                //根据内容识别为null、bool类型、double类型对象
                                if (!string.IsNullOrEmpty(t)) result.Add(DynamicParse(t));
                            }
                        }
                        if (ch == ']')
                        {
                            ++i;
                            break;
                        }
                        lastIndex = i + 1;
                    }
                }
            }
            length = i - startIndex;
            return result.ToArray();
        }
        /// <summary>
        /// Dictionary类型对象转换为Json格式字符串。仅针对从Json字符串生成的动态对象
        /// </summary>
        /// <param name="dic">Dictionary类型的对象</param>
        /// <returns>返回Json格式的字符串</returns>
        static string DictionaryToJson(IDictionary dic)
        {
            if (dic == null) return null;
            StringBuilder result = null;
            foreach (dynamic a in dic)
            {
                if (result == null)
                {
                    result = new StringBuilder("{");
                }
                else
                    result.Append(',');
                result.Append(ToJson(a.Key));
                result.Append(":");
                result.Append(ToJson(a.Value));
            }
            if (result == null) return "{}";
            result.Append('}');
            return result.ToString();
        }
        /// <summary>
        /// C#对象转换为Json格式字符串。注意，非基础数据类型将序列化为ToString()所得字符串
        /// </summary>
        /// <param name="d">对象</param>
        /// <returns>返回Json格式的字符串</returns>
        static public string ToJson(this object d)
        {
            if (d == null) return "null";
            var arr = d as Array;
            if (arr != null)
            {
                return ArrayToJson(arr);
            }
            var dic = d as IDictionary;
            if (dic != null)
            {
                return DictionaryToJson(dic);
            }
            var lst = d as IList;
            if (lst != null)
            {
                dynamic[] dy = new dynamic[lst.Count];
                int i = 0;
                foreach (var a in lst)
                    dy[i++] = a;
                return ArrayToJson(dy);
            }
            Type type = d.GetType();
            if (type == typeof(string))
            {
                return "\"" + JsonEscape(d.ToString()) + "\"";
            }
            if (type == typeof(bool))
                return (bool)d == true ? "true" : "false";
            if (type == typeof(int) || type == typeof(double) || type == typeof(float) || type == typeof(long) || type == typeof(uint) || type == typeof(ulong) || type == typeof(short) || type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(decimal) || type == typeof(BigInteger))
            {
                return d.ToString();
            }
            return "\"" + JsonEscape(d.ToString()) + "\"";
        }
        static public string ArrayToJson(Array arr)
        {
            if (arr == null) return null;
            if (arr.Length == 0) return "[]";
            StringBuilder result = new StringBuilder("[");
            int i = 0;
            for (; i < arr.Length - 1; ++i)
            {
                result.Append(ToJson(arr.GetValue(i)));
                result.Append(',');
            }
            result.Append(ToJson(arr.GetValue(i)));
            result.Append(']');
            return result.ToString();
        }
        /// <summary>
        /// 进行简单的转义处理。只处理 \ 和 "
        /// </summary>
        /// <param name="str">被转义字符串</param>
        /// <returns>转义处理后的字符串</returns>
        static string JsonEscape(string str)
        {
            StringBuilder result = new StringBuilder();
            int lastidx = 0;
            //转义字符串
            string addString = null;
            for (int i = 0; i < str.Length; ++i)
            {
                //根据字符设置转义字符串，这里仅处理四种转义
                switch (str[i])
                {
                    case '\\':
                        addString = "\\\\";
                        break;
                    case '\"':
                        addString = "\\\"";
                        break;
                    case '\r':
                        addString = "\\r";
                        break;
                    case '\n':
                        addString = "\\n";
                        break;
                    case '\t':
                        addString = "\t";//不转义
                        break;
                    case '\f':
                        addString = "\f";//不转义
                        break;
                    case '\b':
                        addString = "\b";//不转义
                        break;
                    default:
                        if (str[i] > 0x7f || str[i] < 0x20)
                        {
                            addString = "\\u" + ((int)str[i]).ToString("x4");
                        }
                        else addString = str[i].ToString();
                        break;
                }
                //当addString非空时，说明有转义内容需要添加
                if (addString != null)
                {
                    if (lastidx < i)
                    {
                        result.Append(str, lastidx, i - lastidx);
                    }
                    lastidx = i + 1;
                    result.Append(addString);
                    addString = null;
                }
            }
            if (lastidx == 0)//不曾遇到需要转义的字符
                return str;
            if (lastidx < str.Length)
                return result.ToString() + str.Substring(lastidx);
            return result.ToString();
        }
        /// <summary>
        /// 识别Json中的值
        /// </summary>
        /// <param name="str">Json字符串中表示非字符串的值的部分</param>
        /// <returns>返回null、bool类型或者double类型对象</returns>
        static dynamic DynamicParse(string str)
        {
            string s = str.Trim().ToLower();
            if (s == "null")
                return null;
            if (s == "true")
                return true;
            if (s == "false")
                return false;
            if (s.Length > 0)
            {
                if (s.Contains('.') || s.Contains('e'))
                {
                    double dbl = 0;
                    if (double.TryParse(s, out dbl)) return dbl;
                }
                else
                {
                    if (s.Length <= 19)
                    {
                        int i = 0;
                        if (int.TryParse(s, out i)) return i;
                        long l = 0;
                        if (long.TryParse(s, out l)) return l;
                        ulong ul = 0;
                        if (ulong.TryParse(s, out ul)) return ul;
                    }
                    BigInteger bi = new BigInteger();
                    if (BigInteger.TryParse(s, out bi)) return bi;

                }
            }
            //throw new InvalidCastException("无法识别的字符串："+str);
            return str;//识别为原字符串
        }

    }
}

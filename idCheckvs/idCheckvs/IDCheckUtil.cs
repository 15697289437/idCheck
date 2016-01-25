using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Web;

namespace idCheckvs
{
    public class IDCheckUtil
    {
        /// <summary>
        /// 获取Token
        /// </summary>
        public static string Token
        {
            get
            {
                DateTime diffDateTime = Convert.ToDateTime(HttpContext.Current.Application["TokenTime"]);
                //如果超时获取新的
                if (HttpContext.Current.Application["Token"] == null || string.Equals(HttpContext.Current.Application["Token"].ToString(), "") || diffDateTime.AddHours(1) < DateTime.Now)
                {
                    HttpContext.Current.Application["TokenTime"] = DateTime.Now;
                    string tk = getToken();
                    HttpContext.Current.Application["Token"] = tk;
                    return tk;
                }
                return HttpContext.Current.Application["Token"].ToString();
            }
        }

        /// <summary>
        /// 请求地址的回调参数
        /// </summary>
        public static string CallBackUrl
        {
            get
            {
                return HttpContext.Current.Application["CallBackUrl"].ToString();
            }
        }

        private const string APIURI = "http://api10.g315.net:8511/wndc/tkn/queryaccount.do";

        /// <summary>
        /// 账户
        /// </summary>
        private const string USER = "xly_zz";

        /// <summary>
        /// 密码
        /// </summary>
        private const string PWD = "zz123456";

        /// <summary>
        /// 获取token
        /// </summary>
        /// <returns></returns>
        private static string getToken()
        {
            
            string param=string.Format("?action=login&token=&msg={0}{1}", USER, md5(PWD, 16));
            log("1.获取Token:"+param );
            string res = req.SendRequest(APIURI, param);
            log("1.Token返回信息:" + res);
            Token tk = JsonConvert.DeserializeObject<Token>(res);
            if (tk.statcode == "100")
            {
                HttpContext.Current.Application["CallBackUrl"] = tk.callbackUrl;
                return tk.msg;
            }
            return "";
            //return string.Format("action=login&token=&msg={0}{1}", USER, md5(PWD, 16));
        }

        /// <summary>
        /// 验证身份证
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="errmsg"></param>
        /// <returns></returns>
        public static bool IdCheck(string id, string name, ref string errmsg)
        {
            ReqIDCheck ric = new ReqIDCheck();
            ric.ywlx = "open account";
            ric.sessionId = "";
            ric.gmsfhm = id;
            ric.xm = name;
            ric.fsd = "不验证";
            ric.account = USER;
            ric.xp = "";
            ric.action = "qsfz";
            ric.cid = DateTime.Now.Ticks.ToString();
            string json = JsonConvert.SerializeObject(ric);
            string param = "?action=qsfz&token=" + Token + "&mid=1&msg={\"" + ric.gmsfhm + "\":" + json + "}";
            log("2.验证用户请求:" + param);
            string res = req.SendRequest(CallBackUrl, param);
            log("2.验证返回信息:" + res);
            IDinfo ri = null;
            JObject jo = JObject.Parse(res);
            if (jo != null)
            {
                JObject joc = JObject.Parse(jo["msg"].ToString());
                if (joc != null){
                    ri = JsonConvert.DeserializeObject<IDinfo>(joc[ric.gmsfhm].ToString());
                }
            }
            bool isCheck = false;
            switch (ri.statcode)
            {
                case "1100":
                    errmsg = "验证成功";
                    log("2.验证结果:" + ric.gmsfhm+" 验证成功.");
                    isCheck = true;
                    break;

                case "1101":
                    errmsg = "身份证号码不一致";
                    log("2.验证结果:" + ric.gmsfhm+" 身份证号码不一致.");
                    isCheck = false;
                    break;

                case "1102":
                    errmsg = "姓名不一致";
                    log("2.验证结果:" + ric.gmsfhm+" 姓名不一致.");
                    isCheck = false;
                    break;

                default:
                    errmsg = "异常错误";
                    break;
            }

            return isCheck;
        }

        private static string md5(string str, int code)
        {
            if (code == 16) //16位MD5加密（取32位加密的9~25字符）
            {
                return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(str, "MD5").ToUpper().Substring(8, 16);
            }
            else//32位加密
            {
                return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(str, "MD5").ToUpper();
            }
        }

        private static void log(string msg)
        {
            string path = HttpContext.Current.Server.MapPath("log" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
            FileInfo f = new FileInfo(path);
            StreamWriter w = null;
            if (!File.Exists(path))
            {
                w = f.CreateText();
            }
            else
            {
                w = f.AppendText();
            }
            w.WriteLine("{0}--{1}", DateTime.Now.ToLocalTime(), msg);
            w.WriteLine("\n\r\t");
            w.Close();
        }
    }

    /// <summary>
    /// 响应返回基类
    /// </summary>
    internal class ResBase
    {
        public string statcode { get; set; }
        public string callbackUrl { get; set; }
        public string sessionId { get; set; }
        public string state { get; set; }
    }

    /// <summary>
    /// 服务器返回token的model
    /// </summary>
    internal class Token : ResBase
    {
        public string msg { get; set; }
        public string cid { get; set; }
    }

    /// <summary>
    /// 请求验证身份证信息
    /// </summary>
    internal class ResID : ResBase
    {
        //public IDInfokey msg { get; set; }
    }

    /// <summary>
    /// 相应身份证详细信息
    /// </summary>
    internal class IDinfo
    {
        public string callbackUrl { get; set; }
        public string cid { get; set; }
        public string gmsfhm { get; set; }
        public string msg { get; set; }
        public string resultGmsfhm { get; set; }
        public string resultXm { get; set; }
        public string resultxp { get; set; }
        public string sessionId { get; set; }
        public string sid { get; set; }
        public string statcode { get; set; }
        public string state { get; set; }
        public string xm { get; set; }
    }

    /// <summary>
    /// id请求类
    /// </summary>
    internal class ReqIDCheck
    {
        /// <summary>
        /// 业务类型
        /// </summary>
        public string ywlx { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public string sessionId { get; set; }

        /// <summary>
        /// 身份证
        /// </summary>
        public string gmsfhm { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string xm { get; set; }

        /// <summary>
        /// 发生地 不验证
        /// </summary>
        public string fsd { get; set; }

        /// <summary>
        /// 分配的帐号
        /// </summary>
        public string account { get; set; }

        /// <summary>
        /// 相片 为空
        /// </summary>
        public string xp { get; set; }

        /// <summary>
        /// 固定值qsfz
        /// </summary>
        public string action { get; set; }

        /// <summary>
        /// 唯一的序号
        /// </summary>
        public string cid { get; set; }
    }

    internal class req
    {
        #region 通讯函数

        /// <summary>
        /// 通讯函数
        /// </summary>
        /// <param name="url">请求Url</param>
        /// <param name="para">请求参数</param>
        /// <param name="method">请求方式GET/POST</param>
        /// <returns></returns>
        public static string SendRequest(string url, string para, string method)
        {
            string strResult = "";
            if (url == null || url == "")
                return null;
            if (method == null || method == "")
                method = "GET";
            // GET方式
            if (method.ToUpper() == "GET")
            {
                try
                {
                    System.Net.WebRequest wrq = System.Net.WebRequest.Create(url + para);
                    wrq.Method = "GET";
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                    System.Net.WebResponse wrp = wrq.GetResponse();
                    System.IO.StreamReader sr = new System.IO.StreamReader(wrp.GetResponseStream(), System.Text.Encoding.GetEncoding("gb2312"));
                    strResult = sr.ReadToEnd();
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            // POST方式
            if (method.ToUpper() == "POST")
            {
                if (para.Length > 0 && para.IndexOf('?') == 0)
                {
                    para = para.Substring(1);
                }
                WebRequest req = WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                StringBuilder UrlEncoded = new StringBuilder();
                Char[] reserved = { '?', '=', '&' };
                byte[] SomeBytes = null;
                if (para != null)
                {
                    int i = 0, j;
                    while (i < para.Length)
                    {
                        j = para.IndexOfAny(reserved, i);
                        if (j == -1)
                        {
                            UrlEncoded.Append(HttpUtility.UrlEncode(para.Substring(i, para.Length - i), System.Text.Encoding.GetEncoding("gb2312")));
                            break;
                        }
                        UrlEncoded.Append(HttpUtility.UrlEncode(para.Substring(i, j - i), System.Text.Encoding.GetEncoding("gb2312")));
                        UrlEncoded.Append(para.Substring(j, 1));
                        i = j + 1;
                    }
                    SomeBytes = Encoding.Default.GetBytes(UrlEncoded.ToString());
                    req.ContentLength = SomeBytes.Length;
                    Stream newStream = req.GetRequestStream();
                    newStream.Write(SomeBytes, 0, SomeBytes.Length);
                    newStream.Close();
                }
                else
                {
                    req.ContentLength = 0;
                }
                try
                {
                    WebResponse result = req.GetResponse();
                    Stream ReceiveStream = result.GetResponseStream();
                    Byte[] read = new Byte[512];
                    int bytes = ReceiveStream.Read(read, 0, 512);
                    while (bytes > 0)
                    {
                        // 注意：
                        // 下面假定响应使用 UTF-8 作为编码方式。
                        // 如果内容以 ANSI 代码页形式（例如，932）发送，则使用类似下面的语句：
                        // Encoding encode = System.Text.Encoding.GetEncoding("shift-jis");
                        Encoding encode = System.Text.Encoding.GetEncoding("gb2312");
                        strResult += encode.GetString(read, 0, bytes);
                        bytes = ReceiveStream.Read(read, 0, 512);
                    }
                    return strResult;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            return strResult;
        }

        #endregion 通讯函数

        #region 简化通讯函数

        /// <summary>
        /// GET方式通讯
        /// </summary>
        /// <param name="url"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public static string SendRequest(string url, string para)
        {
            return SendRequest(url, para, "GET");
        }

        #endregion 简化通讯函数
    }
}
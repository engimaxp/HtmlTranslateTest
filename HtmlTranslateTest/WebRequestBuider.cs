using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace HtmlTranslateTest
{
    public enum RequestMethod
    {
        POST,
        GET
    }

    /// <summary>
    /// Builder类
    /// </summary>
    public class WebRequestBuider
    {
        /// <summary>
        /// 返回未赋值builder
        /// </summary>
        /// <returns></returns>
        public static WebRequestBuider build()
        {
            return new WebRequestBuider();
        }
        public static WebRequestBuider buildJsonPost()
        {
            return new WebRequestBuider()
            {
                _contentType = "application/json",
                _method = RequestMethod.POST
            };
        }

        private int _timeout { get; set; }
        private RequestMethod _method { get; set; }
        private string _contentType { get; set; }
        private string _url { get; set; }
        private WebProxy _proxy { get; set; }
        public WebRequestBuider setTimeOut(int timeout)
        {
            _timeout = timeout;
            return this;
        }
        public WebRequestBuider setMethod(RequestMethod method)
        {
            _method = method;
            return this;
        }
        public WebRequestBuider setContentType(string contentType)
        {
            _contentType = contentType;
            return this;
        }
        public WebRequestBuider setUrl(string url)
        {
            _url = url;
            return this;
        }

        public WebRequestBuider setProxy(string proxyUrl)
        {
            if (string.IsNullOrEmpty(proxyUrl)) return this;
            var proyPara = proxyUrl.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (proyPara.Length < 2) return this;
            int port = 0;
            if (!int.TryParse(proyPara[1], out port)) return this;
            _proxy = new WebProxy(proyPara[0], port) { UseDefaultCredentials = false };
            return this;
        }

        /// <summary>
        /// 创建WebRequestInstance
        /// </summary>
        /// <returns></returns>
        public WebRequestInstance CreateRequest()
        {
            if (string.IsNullOrEmpty(_url))
            {
                return null;
            }
            var request = new WebRequestInstance();

            if (_timeout <= 0)
            {
                _timeout = 1000;
            }
            request.TimeOut = _timeout;
            request.Method = _method;

            if (string.IsNullOrEmpty(_contentType))
            {
                _contentType = "application/json";
            }
            request.ContentType = _contentType;

            request.Url = _url;

            if (_proxy != null)
            {
                request.Proxy = _proxy;
            }

            return request;
        }
    }
    /// <summary>
    /// WebRequest实例
    /// </summary>
    public class WebRequestInstance
    {
        /// <summary>
        /// 超时时间
        /// </summary>
        public int TimeOut { private get; set; }
        /// <summary>
        /// 请求方法
        /// </summary>
        public RequestMethod Method { private get; set; }
        /// <summary>
        /// 内容类型
        /// </summary>
        public string ContentType { private get; set; }
        /// <summary>
        /// 请求地址
        /// </summary>
        public string Url { private get; set; }
        /// <summary>
        /// 错误
        /// </summary>
        public string Error { get; private set; }
        /// <summary>
        /// 请求状态
        /// </summary>
        public string Status { get; private set; }
        public WebProxy Proxy { private get; set; }
        internal WebRequestInstance()
        {
        }
        public override string ToString()
        {
            return string.Format("TimeOut: {0}, Method: {1}, ContentType: {2}, Url: {3},Proxy:{4}", TimeOut, Method, ContentType, Url, Proxy);
        }

        public TResponse PostGetEntity<TResponse>(object entityRequest)
        {
            try
            {
                var response = PostHttp(JsonConvert.SerializeObject(entityRequest));
                if (string.IsNullOrEmpty(response)) return default(TResponse);
                return JsonConvert.DeserializeObject<TResponse>(response);
            }
            catch (Exception exception)
            {
                Error = exception.ToString();
                return default(TResponse);
            }
        }

        /// <summary>
        /// Post http 
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public string Http(string jsonString)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamWriter responseStream = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = Method.ToString();
                request.ContentType = ContentType;
                //request.ContentLength = Encoding.UTF8.GetByteCount(jsonString);
                request.Timeout = this.TimeOut;
                if (Proxy != null)
                {
                    request.Proxy = Proxy;
                }
                if (Method == RequestMethod.POST)
                {
                    responseStream = new StreamWriter(request.GetRequestStream());
                    responseStream.Write(jsonString);
                    responseStream.Close();
                }
                response = (HttpWebResponse)request.GetResponse();
                this.Status = response.StatusDescription;
                var stream = response.GetResponseStream();
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
                else
                {
                    Error = string.Format("PostHttp,Class:{0}|Json:{1}|ex:{2}", this, jsonString, "返回流为空");
                    return String.Empty;
                }
            }
            catch (Exception ex)
            {
                Error = string.Format("PostHttp,Class:{0}|Json:{1}|ex:{2}", this, jsonString, ex);
                return string.Empty;
            }
            finally
            {
                //必须收尾动作
                if (request != null)
                {
                    request.Abort();
                    GC.SuppressFinalize(request);
                }
                if (responseStream != null)
                {
                    responseStream.Close();
                }

                if (response != null)
                {
                    response.Close();
                }
            }
        }
        /// <summary>
        /// Post http 
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        private string PostHttp(string jsonString)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamWriter responseStream = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = Method.ToString();
                request.ContentType = ContentType;
                //request.ContentLength = Encoding.UTF8.GetByteCount(jsonString);
                request.Timeout = this.TimeOut;
                if (Proxy != null)
                {
                    request.Proxy = Proxy;
                }
                if (Method == RequestMethod.POST)
                {
                    responseStream = new StreamWriter(request.GetRequestStream());
                    responseStream.Write(jsonString);
                    responseStream.Close();
                }
                response = (HttpWebResponse)request.GetResponse();
                this.Status = response.StatusDescription;
                var stream = response.GetResponseStream();
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
                else
                {
                    Error = string.Format("PostHttp,Class:{0}|Json:{1}|ex:{2}", this, jsonString, "返回流为空");
                    return String.Empty;
                }
            }
            catch (Exception ex)
            {
                Error = string.Format("PostHttp,Class:{0}|Json:{1}|ex:{2}", this, jsonString, ex);
                return string.Empty;
            }
            finally
            {
                //必须收尾动作
                if (request != null)
                {
                    request.Abort();
                    GC.SuppressFinalize(request);
                }
                if (responseStream != null)
                {
                    responseStream.Close();
                }

                if (response != null)
                {
                    response.Close();
                }
            }
        }
    }
}

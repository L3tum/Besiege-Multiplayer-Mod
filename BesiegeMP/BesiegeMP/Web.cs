using System;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace BesiegeMP
{
    static class Web
    {
        public static T GetAsyncJSON<T>(String url)
        {
            WebRequest request = WebRequest.Create(url);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            WebResponse response = request.GetResponse();
            return serializer.Deserialize<T>(new StreamReader(response.GetResponseStream()).ReadToEnd());
        }
    }
}

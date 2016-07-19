using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace BesiegeMP
{
    static class Web
    {

        public static T GetAsyncJSON<T>(String url)
        {
            WebRequest request = WebRequest.Create(url);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return (T)serializer.Deserialize<T>(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());
        }
    }
}

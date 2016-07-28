using System;

namespace BesiegeMP
{
    static class Util
    {
        public static String LocalIPAddress()
        {
            string url = "http://icanhazip.com/";
            System.Net.WebRequest req = System.Net.WebRequest.Create(url);
            System.Net.WebResponse resp = req.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
            return sr.ReadToEnd().Trim();
        }
    }
}

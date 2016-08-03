using System;
using BesiegeMP.CrapForWeb;

namespace BesiegeMP
{
    static class Util
    {
        public static void LocalIPAddress()
        {
            string url = "http://icanhazip.com/";
            System.Net.WebRequest req = System.Net.WebRequest.Create(url);
            System.Net.WebResponse resp = req.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
            Settings.adress = sr.ReadToEnd().Trim();
            if (Settings.getLocation)
            {
                Settings.Location = Web.GetAsyncJSON<Region>("http://ip-api.com/json/" + Settings.adress).country;
            }
        }
    }
}

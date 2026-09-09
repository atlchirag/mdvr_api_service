using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace mdvr_api_service
{
    internal class LoginAPIvariable
    {
       public string Jsession { get; set; }
       
    }

    internal class TrackingAPIvariable
    {
        public List<data> status { get; set; }
        public string result { get; set; }
        

    }

    public class data
    {
   
        public string id { get; set; }

        public string vid { get; set; }
    
        public string mlng { get; set; }    
        
        public string mlat { get; set; }

        public string sp { get; set; }
   
        public string ol { get; set; }
   
        public string hx { get; set; }
   
        public string yl { get; set; }
     
        public string dn { get; set; }
        public string gt { get; set; }
        public int s1 { get; set; }

        // NEW: mileage in meters (from getDeviceStatus → lc)
        public string lc { get; set; }   // keep as string to avoid parsing issues
    }

    internal class PlaybackResponse
    {
        public int result { get; set; }
        public Pagination pagination { get; set; }
        public List<data> infos { get; set; }
    }
    internal class Pagination
    {
        public int currentPage { get; set; }
        public int totalPages { get; set; }
    }

}

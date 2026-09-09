using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mdvr_api_service
{
    internal class apiVariables
    {
        public apiVariables[] storeListobj
        {
            get;
            set;
        }
        public int createdBy { get; set; }
        public int TripId { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
            

        public string miles { get; set; }
        public string speed { get; set; }

        internal class VehicleLatestResponse
        {
            public int result { get; set; }
            public List<VehicleLatestInfo> infos { get; set; }
        }

        internal class VehicleLatestInfo
        {
            public string vi { get; set; }
            public long tm { get; set; }
            public double? jd { get; set; }   // was double → make it double?
            public double? wd { get; set; }   // was double → make it double?
            public string pos { get; set; }
        }


        //internal class VehicleLatestInfo
        //{
        //    public string vi { get; set; } // Plate Number
        //    public long tm { get; set; }   // milliseconds
        //    public double jd { get; set; } // Lng
        //    public double wd { get; set; } // Lat
        //    public string pos { get; set; } // Address (unused)
        //}
    }
}

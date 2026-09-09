using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using System.Configuration.Install;
using System.Threading;
using System.Globalization;
using static System.Collections.Specialized.BitVector32;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Security;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using mdvr_api_service;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace mdvr_api_service

{
    partial class Program : ServiceBase
    {
        
        static void Main(string[] args)
        {
          
            //  General.my_SendNotification("this \n is \n test", 20340);

            //string qee = $"insert into alert_notification_log values('26716','{msg}','{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")}','{Lat}','{Long}')";

            // General.my_SendNotification("check test ", 20340);

            //StartService();

            if (args.Length > 0)
            {
                for (int ii = 0; ii < args.Length; ii++)
                {
                    switch (args[ii].ToUpper())
                    {
                        case "/I":
                            InstallService();
                            return;
                        case "/U":
                            UninstallService();
                            return;
                        default:
                            break;
                    }
                }
            }
            else
            {
                System.ServiceProcess.ServiceBase.Run(new Program());
            }

        }


        public Program()
        {
            InitializeComponent();
        }


        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string loggerMessage = "An unhandled exception has occured in method " + new StackTrace(((Exception)e.ExceptionObject), true).GetFrame(0).GetMethod().Name + " at line no." + new StackTrace(((Exception)e.ExceptionObject), true).GetFrame(0).GetFileLineNumber() + " : " + Environment.NewLine +
                                 "Exception is: " + ((Exception)e.ExceptionObject).Message;
            General.WriteToLogFile(loggerMessage, AppDomain.CurrentDomain.BaseDirectory, "UnhandledException.txt");

        }
        protected override void OnStart(string[] args)
        {

            base.OnStart(args);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Thread t = new Thread(new ThreadStart(StartService));
            t.Start();
            //Thread.Sleep(1000*20);

        }



        private static async void StartService()
        {
            General.WriteToLogFile("started", AppDomain.CurrentDomain.BaseDirectory, "started.txt");
            apiMethod apiMethod = new apiMethod();
            General gen = new General();
            Dictionary<string,string> devices= new Dictionary<string, string>();
            string tablename = $"tbl_telemetry_{DateTime.Now.ToString("MMM")}{DateTime.Now.ToString("yy")}";
           

            while (true)
            {
                try
                {
                    devices = gen.getDevices();
                    foreach (KeyValuePair<string, string> device in devices)
                    {
                        Thread thread = new Thread(async () =>
                        {
                            await inserttrackingdata(gen, apiMethod, device.Key, tablename, device.Value);
                        });

                        thread.Start();
                    }
                    Thread.Sleep(20 * 1000);
                }
                catch (Exception ex)
                {

                }
                

            }
 

        }

        public static async Task inserttrackingdata(General gen, apiMethod api, string device, string tablename, string userid)
        {
            try
            {
                string username, password, Jsession;
                string device_number, veh_reg;
                string longitude, latitude, speed, online_status, direction, fuel;
                string gps_validity = "A";
                string gps_time, sys_proc_time;
                int s1;

                // 1) creds + login
                (username, password) = gen.getuser(userid);
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    General.WriteToLogFile($"Empty creds for user={userid}", AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                    return;
                }
                Jsession = await api.consumeLoginAPI(username, password);
                if (string.IsNullOrWhiteSpace(Jsession)) return;


                if (device == "DL05AZ3232") device = "000001234567";

                // 2) live status
                var json = await api.consumeLiveTrackingApi(Jsession, device);
                if (string.IsNullOrWhiteSpace(json))
                {
                    General.WriteToLogFile($"Empty API response for device={device}", AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                    return;
                }

                var obj = JsonConvert.DeserializeObject<TrackingAPIvariable>(json, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                if (obj == null || obj.status == null || obj.status.Count == 0)
                {
                    General.WriteToLogFile($"No status in API response for device={device}. Payload={json}", AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                    return;
                }
                
                var r = obj.status[0];

                //ignition value and signal value
                s1 = r.s1;
                int accState = (r.s1 >> 1) & 1; //s1:1st bit
                int signal = (s1 >> 10) & 0b111; //s1: 11th, 12th, 13th bits for the signal strength
                int signal_strength;
                if(signal == 0)
                {
                    signal_strength = 0; // No signal
                }
                else
                {
                    signal_strength = 100 / signal; // Convert to percentage
                }
                // 3) map (safe)
                device_number = r.id ?? device;
                veh_reg = r.vid ?? "";
                longitude = string.IsNullOrWhiteSpace(r.mlng) ? "0" : r.mlng;
                latitude = string.IsNullOrWhiteSpace(r.mlat) ? "0" : r.mlat;

                int sp10; if (!int.TryParse(r.sp ?? "0", out sp10)) sp10 = 0;
                speed = (sp10 / 10).ToString();

                online_status = r.ol ?? "0";
                direction = r.hx ?? "0";
                //string tel_odometer_m = string.IsNullOrWhiteSpace(r.lc) ? "0" : r.lc;
                // naya: meters → km (string to double), phir Invariant string
                double odoMeters = 0;
                double.TryParse(string.IsNullOrWhiteSpace(r.lc) ? "0" : r.lc,
                                NumberStyles.Any, CultureInfo.InvariantCulture, out odoMeters);
                string tel_odometer_km = (odoMeters / 1000.0).ToString(CultureInfo.InvariantCulture);
                fuel = r.yl ?? "0";

                // 4) resolve service id once
                // 4) resolve service id once
                string service_id = gen.service_id(device, gen);

                // ========== GAP CHECK (3 minutes) ==========
                DateTime? lastSysProcLocal = gen.GetLastSysProcTime(tablename, service_id);   // IST from DB
                DateTime nowLocal = DateTime.Now;

                bool needBackfill = false;
                DateTime backfillStartLocal = DateTime.MinValue;
                DateTime backfillEndLocal = nowLocal;        // end = service start/resume time (IST)

                if (lastSysProcLocal.HasValue)
                {
                    var gap = nowLocal - lastSysProcLocal.Value;
                    if (gap > TimeSpan.FromMinutes(3))          // ← your new rule
                    {
                        needBackfill = true;
                        backfillStartLocal = lastSysProcLocal.Value.AddSeconds(1);  // exclusive
                                                                                    // optional: small overlap safety
                        backfillEndLocal = nowLocal.AddSeconds(-5);
                        if (backfillEndLocal <= backfillStartLocal)
                            needBackfill = false;
                    }
                }

                // OL (online) check — yahi par inject karein
                bool isOnline = (r.ol ?? "0") == "1";
                string online= r.ol ?? "0";

                // Agar gap > 3 min hai aur device OFFLINE hai ⇒ LIVE ko SKIP karo, PB bhi mat chalao abhi
                if (needBackfill && !isOnline)
                {
                    General.WriteToLogFile(
                        $"Pending backfill but device offline. Dev={device}, last={lastSysProcLocal:yyyy-MM-dd HH:mm:ss}, now={nowLocal:yyyy-MM-dd HH:mm:ss}",
                        AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                    return; // ← iss device ke liye is cycle me kuchh mat karo. Next cycles me online hote hi PB chalega.
                }

                // Agar gap > 3 min aur device ONLINE ⇒ pehle PB chalao, phir live
                if (needBackfill && isOnline)
                {
                    await BackfillPlayback(gen, api, Jsession, device, tablename, service_id, backfillStartLocal, backfillEndLocal);
                }


                // ================== GAP CHECK & BACKFILL END ==================


                // 5) time (aapka rule: sys_proc_time = NOW local; gps_time = same line)
                sys_proc_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss'.0'", CultureInfo.InvariantCulture);


                // keep your original gps_time logic EXACTLY
                gps_time = (Convert.ToDateTime(sys_proc_time).AddHours(-5).AddMinutes(-30))
                            .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                if (longitude == "0" || latitude == "0") gps_validity = "V";

                // 6) INSERT (same query/columns)
                string query = $"INSERT INTO {tablename} " +
                               "(sys_service_id, sys_proc_time, gps_validity, gps_time, " +
                               " gps_latitude, gps_longitude, gps_orientation, gps_speed, tel_odometer, i2, signal_strength,i1) " +
                               $"VALUES ('{service_id}', '{sys_proc_time}', '{gps_validity}', '{gps_time}', " +
                               $" '{latitude}', '{longitude}', '{direction}', '{speed}', '{tel_odometer_km}', '{accState}', '{signal_strength}', '{online}')";

                gen.DML(query);
            }
            catch (Exception ex)
            {
                General.WriteToLogFile($"inserttrackingdata ex: {ex.Message}", AppDomain.CurrentDomain.BaseDirectory, "ingest_error.txt");
            }
        }


        private static async Task BackfillPlayback(
    General gen,
    apiMethod api,
    string jsession,
    string device,           // devIdno/IMEI
    string tablename,
    string service_id,
    DateTime beginLocal,     // IST window
    DateTime endLocal        // IST window
)
        {
            try
            {
                // IST → CST (+2:30)
                DateTime beginChina = beginLocal.AddHours(2).AddMinutes(30);
                DateTime endChina = endLocal.AddHours(2).AddMinutes(30);

                int page = 1, totalPages = 1;
                do
                {
                    string beginStr = beginChina.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    string endStr = endChina.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                    var raw = await api.consumePlaybackTrackApi(jsession, device, beginStr, endStr, page, 50);
                    if (string.IsNullOrWhiteSpace(raw)) break;

                    var pb = JsonConvert.DeserializeObject<PlaybackResponse>(raw,
                                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    if (pb?.infos == null || pb.infos.Count == 0) break;

                    foreach (var p in pb.infos)
                    {
                        string plat = string.IsNullOrWhiteSpace(p.mlat) ? "0" : p.mlat;
                        string plng = string.IsNullOrWhiteSpace(p.mlng) ? "0" : p.mlng;
                        string phx = string.IsNullOrWhiteSpace(p.hx) ? "0" : p.hx;
                        //ignition value
                        int s1 = p.s1;
                        int accState = (s1 >> 1) & 1;
                        //signal value
                        int signal = (s1 >> 10) & 0b111;
                        int signal_strength;
                        if (signal == 0)
                        {
                            signal_strength = 0; // No signal
                        }
                        else
                        {
                            signal_strength = 100 / signal; // Convert to percentage
                        }
                        //mainpower
                        string online = p.ol ?? "0";
                        // speed: /10
                        int sp10; if (!int.TryParse(p.sp ?? "0", out sp10)) sp10 = 0;
                        string speedpb = (sp10 / 10).ToString();

                        // odometer: meters → km
                        double m = 0; double.TryParse(p.lc ?? "0",
                                      NumberStyles.Any, CultureInfo.InvariantCulture, out m);
                        string km = (m / 1000.0).ToString(CultureInfo.InvariantCulture);

                        // p.gt is CST → IST for storage
                        DateTime ptChina;
                        if (!DateTime.TryParseExact(p.gt ?? "",
                                new[] { "yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-dd HH:mm:ss.F", "yyyy-MM-dd HH:mm:ss" },
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out ptChina))
                            ptChina = DateTime.UtcNow.AddHours(8);

                        DateTime ptLocal = ptChina.AddHours(-2).AddMinutes(-30);

                        string sys_proc_time_pb = ptLocal.ToString("yyyy-MM-dd HH:mm:ss'.0'", CultureInfo.InvariantCulture);
                        string gps_time_pb = ptLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        string gps_validity_pb = (plat == "0" || plng == "0") ? "V" : "A";

                        // (Optional) skip duplicates
                        // if (gen.TelemetryExists(tablename, service_id, ptLocal)) continue;

                        string q = $"INSERT INTO {tablename} " +
                                   "(sys_service_id, sys_proc_time, gps_validity, gps_time, " +
                                   " gps_latitude, gps_longitude, gps_orientation, gps_speed, tel_odometer,i2, signal_strength,i1) " +
                                   $"VALUES ('{service_id}', '{sys_proc_time_pb}', '{gps_validity_pb}', '{gps_time_pb}', " +
                                   $" '{plat}', '{plng}', '{phx}', '{speedpb}', '{km}', '{accState}', '{signal_strength}', '{online}')";

                        gen.DML(q);
                    }

                    totalPages = (pb?.pagination?.totalPages > 0) ? pb.pagination.totalPages : page;
                    page++;
                }
                while (page <= totalPages);
            }
            catch (Exception ex)
            {
                General.WriteToLogFile($"BackfillPlayback ex: {ex.Message}", AppDomain.CurrentDomain.BaseDirectory, "ingest_error.txt");
            }
        }




        //public static async Task inserttrackingdata(General gen, apiMethod api,string device,string tablename,string userid)
        //{
        //    try
        //    {


        //        string username;
        //        string password;
        //        string device_number;
        //        string veh_reg;
        //        string Jsession;
        //        string longitude;
        //        string latitude;
        //        string speed;
        //        string online_status;
        //        string direction;
        //        string fuel;
        //        string driver_name;
        //        string gps_validity = "A";
        //        string gps_time;
        //        string sys_proc_time;
        //        List<data> d = new List<data>();
        //        (username, password) = gen.getuser(userid);
        //        Jsession = await api.consumeLoginAPI(username, password);
        //        if (device== "DL05AZ3232")
        //        {
        //            device = "000001234567";
        //        }
        //        var livetrackingdata = await api.consumeLiveTrackingApi(Jsession, device);
        //        var livetrackingdata_json = JsonConvert.DeserializeObject<TrackingAPIvariable>(livetrackingdata);
        //        d = livetrackingdata_json.status;
        //        device_number = d[0].id;
        //        veh_reg = d[0].vid;
        //        longitude = d[0].mlng;
        //        latitude = d[0].mlat;
        //        speed = (Convert.ToInt32(d[0].sp)/10).ToString();
        //        online_status = d[0].ol;
        //        direction = d[0].hx;
        //        string tel_odometer_m = d[0].lc;  // mileage in meters (from getDeviceStatus → lc)
        //        fuel = d[0].yl;
        //        // current China time (UTC+8)
        //        sys_proc_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss'.0'");
        //        //gps_time = (Convert.ToDateTime(sys_proc_time).AddHours(-5).AddMinutes(-30)).ToString();
        //        gps_time = (Convert.ToDateTime(sys_proc_time).AddHours(-5).AddMinutes(-30)).ToString("yyyy-MM-dd HH:mm:ss");
        //        //gps_time = (Convert.ToDateTime(sys_proc_time).AddHours(-5).AddMinutes(-30)).ToString();
        //        gps_time = (Convert.ToDateTime(sys_proc_time).AddHours(-5).AddMinutes(-30)).ToString("yyyy-MM-dd HH:mm:ss");
        //        if (longitude == "0" || latitude == "0")
        //        {
        //            gps_validity = "V";
        //        }


        //        string service_id = gen.service_id(device, gen);
        //        //string query = $"insert into {tablename} (sys_service_id,sys_proc_time,gps_validity,gps_time,gps_latitude,gps_longitude,gps_orientation,gps_speed,tel_fuel)";
        //        //query += $"values ('{service_id}','{sys_proc_time}','{gps_validity}','{gps_time.ToString()}','{latitude}','{longitude}','{direction}','{speed}','{fuel}')";
        //        string query = $"insert into {tablename} (sys_service_id,sys_proc_time,gps_validity,gps_time,gps_latitude,gps_longitude,gps_orientation,gps_speed,tel_odometer)";
        //        query += $"values ('{service_id}','{sys_proc_time}','{gps_validity}','{gps_time.ToString()}','{latitude}','{longitude}','{direction}','{speed}','{tel_odometer_m}')";

        //        //Console.WriteLine(query);
        //        gen.DML(query);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}






        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
        }

        private static void InstallService()
        {
            if (IsServiceInstalled())
            {
                UninstallService();
            }

            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static bool IsServiceInstalled()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == "MdvrApiService");
        }

        private static void UninstallService()
        {

            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
        }



    }
}

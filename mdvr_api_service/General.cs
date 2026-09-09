using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace mdvr_api_service
{
    class General
    {
        //public static readonly string connectionString = "Data Source=DESKTOP-PN7P82O ;Initial Catalog=atltracking;Trusted_Connection=True";
        //public static readonly string connectionString = "Data Source=192.168.23.131,15433;Initial Catalog=atltracking;User ID=newtrack;Password=55hD&44m7E3jnd; Max Pool Size=32767;";
        public static readonly string connectionString = "Data Source=103.108.12.184,15433;Initial Catalog=atltracking;User ID=newtrack;Password=55hD&44m7E3jnd; Max Pool Size=32767;";
          
        //public static readonly string connectionString = "Data Source=45.113.189.23;Initial Catalog=atltracking;User ID=newtrack;Password=55hD&44m7E3jnd; Max Pool Size=32767;";
        //public static readonly string connectionString = "Data Source=192.168.23.131,15433;Initial Catalog=atltracking;User ID=newtrack;Password=55hD&44m7E3jnd; Max Pool Size=32767;";
        public Dictionary<string,string> getDevices()
        {
            Dictionary<string,string> keyValuePairs= new Dictionary<string,string>();
            //List<string> devices= new List<string>();
            DataTable dt = new DataTable();
            //string query = $"select imei,sys_user_id from tbl_devices where id in (select sys_device_id from tbl_services where is_mdvr = 1)";

            //string query = $"select tbl_devices.imei,tbl_devices.sys_user_id from tbl_devices inner join tbl_services on tbl_devices.id = tbl_services.sys_device_id where tbl_devices.id in (select sys_device_id from tbl_services where is_mdvr = 1);";
            string query = $"select tbl_devices.imei,tbl_devices.sys_user_id from tbl_devices inner join tbl_services on tbl_devices.id = tbl_services.sys_device_id where tbl_devices.id in (99616);";
            //Console.WriteLine(query);
            dt = SelectQuery(query);
            foreach (DataRow dr in dt.Rows)
            {
                //devices.Add(dr["imei"].ToString());
                keyValuePairs.Add(dr["imei"].ToString(), dr["sys_user_id"].ToString());
            }
            return keyValuePairs;
        }
        public (string username, string password) getuser(string user_id)
        {
            try
            {
                var dt = SelectQuery(
                    $"select sys_username, sys_password from tbl_users where id = {user_id}"
                );

                if (dt == null)
                {
                    General.WriteToLogFile(
                        $"getuser: SelectQuery returned NULL for user_id={user_id}",
                        AppDomain.CurrentDomain.BaseDirectory,
                        "dbselect.txt"
                    );
                    return ("", "");
                }

                if (dt.Rows.Count == 0)
                {
                    General.WriteToLogFile(
                        $"getuser: No rows for user_id={user_id}",
                        AppDomain.CurrentDomain.BaseDirectory,
                        "dbselect.txt"
                    );
                    return ("", "");
                }

                // safer: reference by column name
                string u = dt.Rows[0]["sys_username"]?.ToString() ?? "";
                string p = dt.Rows[0]["sys_password"]?.ToString() ?? "";

                return (u, p);
            }
            catch (Exception ex)
            {
                General.WriteToLogFile(
                    $"getuser ex for user_id={user_id}: {ex.Message}",
                    AppDomain.CurrentDomain.BaseDirectory,
                    "dbselect.txt"
                );
                return ("", "");
            }
        }

        //public (string username, string password) getuser(string user_id)
        //{
        //    List<string> devices = new List<string>();
        //    DataTable dt = new DataTable();
        //    string query = $"select sys_username,sys_password from tbl_users where id = {user_id}";
        //    dt = SelectQuery(query);

        //    return (dt.Rows[0][0].ToString(),dt.Rows[0][1].ToString());
        //}
        public void DML(string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }
                        //  connection.Open();
                        cmd.CommandTimeout = 500;
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            General.WriteToLogFile(ex.Message + ex.InnerException + ex.Source + $"{query}", AppDomain.CurrentDomain.BaseDirectory, "db1.txt");

                        }
                        finally { connection.Close(); }
                        //  Console.WriteLine("data inserted");
                    }
                }
            }
            catch (SqlException e)
            {

                General.WriteToLogFile(e.Message + e.InnerException + e.Source, AppDomain.CurrentDomain.BaseDirectory, "dbinsert.txt");
            }
        }

        public  DataTable SelectQuery(string query)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        SqlCommand command = new SqlCommand(query, conn);
                        //command.CommandTimeout = 5000000;
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        adapter.Dispose();
                        return dt;
                    }
                    catch (Exception e)
                    {

                        General.WriteToLogFile(e.Message + e.InnerException + e.Source, AppDomain.CurrentDomain.BaseDirectory, "dbselect.txt");
                        return null;
                    }
                    finally
                    {
                        conn.Dispose();

                    }
                }
            }
            catch (Exception e)
            {

                General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "db2.txt");
                return null;

            }
        }

        public delegate T RetryOpenDelegate<T>();

        public static T RetryOpen<T>(RetryOpenDelegate<T> action)
        {

            while (true)
            {

                try
                {

                    return action();

                }

                catch (IOException)
                {

                    System.Threading.Thread.Sleep(50);

                }

            }

        }
        public static void WriteToLogFile(string msg, string folderPath, string logFile, bool isAppend = true, bool isDateTime = true)
        {



            string dir = folderPath + "\\" + DateTime.Now.ToString("ddMMyyyy");

            if (!Directory.Exists(dir))

                Directory.CreateDirectory(dir);

            if (!Directory.Exists(dir + "\\ReceivedData"))
                Directory.CreateDirectory(dir + "\\ReceivedData");

            if (!Directory.Exists(dir + "\\WrongProtocol"))
                Directory.CreateDirectory(dir + "\\WrongProtocol");


            string LogFile = dir + "\\" + logFile;


            TextWriter tw = null;

            try
            {

                // create a writer and open the file

                tw = RetryOpen<StreamWriter>(delegate ()
                {

                    return new StreamWriter(LogFile, isAppend);



                });


                // write a line of text to the file
                if (isDateTime)
                    tw.WriteLine(DateTime.Now + Environment.NewLine + msg);
                else
                    tw.WriteLine(msg);

            }

            catch (Exception ex)
            {
            }

            finally
            {

                // close the stream

                if (tw != null)
                {

                    tw.Close();

                    tw.Dispose();

                }

            }

        }

        public string service_id (string imei,General gen)
        {
            if(imei == "000001234567")
            {
                imei = "DL05AZ3232";
            }
            string query = $"select id from tbl_services where sys_device_id in (select id from tbl_devices where imei = '{imei}')";
            //string query = $"select id from tbl_services where veh_reg= '{imei}'";
            DataTable dt = new DataTable();
            dt = gen.SelectQuery(query);

            return $"{dt.Rows[0][0].ToString()}";
        }

        public DateTime? GetLastSysProcTime(string tableName, string serviceId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var sql = $"SELECT MAX(sys_proc_time) FROM {tableName} WHERE sys_service_id = @sid";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@sid", serviceId);
                        var o = cmd.ExecuteScalar();
                        if (o == null || o == DBNull.Value) return null;
                        return Convert.ToDateTime(o);              // ← IST stored in DB
                    }
                }
            }
            catch { return null; }
        }



        // (optional) duplicate protection – exact timestamp + coords par dubara insert na ho
        public bool TelemetryExists(string tableName, string serviceId, DateTime sysProcTime)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var sql = $@"
                SELECT 1 FROM {tableName}
                WHERE sys_service_id = @sid AND sys_proc_time = @t";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@sid", serviceId);
                        cmd.Parameters.AddWithValue("@t", sysProcTime);
                        var o = cmd.ExecuteScalar();
                        return o != null;
                    }
                }
            }
            catch { return false; }
        }


    }
}

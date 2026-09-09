using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Security.Policy;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using static mdvr_api_service.apiVariables;
using System.Globalization;
using System.Threading;

namespace mdvr_api_service
{
    public  class apiMethod
    {

        //public  async task<string> consumeloginapi(string username,string password)
        //{
        //    //general.writetologfile("insideapi", appdomain.currentdomain.basedirectory, "apiresponse.txt");
        //    try
        //    {
        //        httpclienthandler handler = new httpclienthandler();
        //        handler.servercertificatecustomvalidationcallback = (sender, cert, chain, sslpolicyerrors) => true;

        //        var client = new httpclient(handler);
        //        httpresponsemessage response_from_api = await client.getasync($"https://cctv.abctraq.com:9443/standardapiaction_login.action?account={username}&password={password}");

        //        if (response_from_api.issuccessstatuscode)
        //        {
        //            var getjsession_included_data = await response_from_api.content.readasstringasync();
        //            var getjsession_included_data_json = jsonconvert.deserializeobject<loginapivariable>(getjsession_included_data);
        //            string jsession = getjsession_included_data_json.jsession;
        //            return jsession;

        //        }
        //        else
        //        {
        //            general.writetologfile(response_from_api.tostring(), appdomain.currentdomain.basedirectory, "apiresponse.txt");
        //            return "";
        //        }
        //    }
        //    catch (exception ex)
        //    {
        //        general.writetologfile(ex.message, appdomain.currentdomain.basedirectory, $"{username}.txt");
        //        return "";
        //    }

        //}


        public async Task<string> consumeLoginAPI(string username, string password)
        {
            try
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (var client = new HttpClient(handler))
                {
                    var url = "https://cctv.trackofy.com/StandardApiAction_login.action"
                              + $"?account={WebUtility.UrlEncode(username)}"
                              + $"&password={WebUtility.UrlEncode(password)}";

                    var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        var raw = await resp.Content.ReadAsStringAsync();
                        General.WriteToLogFile(resp.ToString() + " " + raw, AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                        return "";
                    }

                    var json = await resp.Content.ReadAsStringAsync();

                    // be defensive about key name
                    var jo = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var jsession = (jo["jsession"] ?? jo["Jsession"] ?? jo["JSESSION"] ?? jo["JSESSIONID"])?.ToString() ?? "";
                    return jsession;
                }
            }
            catch (Exception ex)
            {
                General.WriteToLogFile(ex.Message, AppDomain.CurrentDomain.BaseDirectory, username + ".txt");
                return "";
            }
        }


        //public async Task<string> consumeLiveTrackingApi(string JSession, string device_number)
        //{
        //    //General.WriteToLogFile("insideApi", AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
        //    try
        //    {
        //        HttpClientHandler handler = new HttpClientHandler();
        //        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        //        var client = new HttpClient(handler);
        //        HttpResponseMessage response_from_api = await client.GetAsync($"https://cctv.abctraq.com:9443/StandardApiAction_getDeviceStatus.action?jsession={JSession}&devIdno={device_number}&toMap=1&driver=0&language=zh");

        //        if (response_from_api.IsSuccessStatusCode)
        //        {
        //            var getJsession_included_data = await response_from_api.Content.ReadAsStringAsync();
        //            //var getJsession_included_data_json = JsonConvert.DeserializeObject<TrackingAPIvariable>(getJsession_included_data);
        //            return getJsession_included_data.ToString();

        //        }
        //        else
        //        {
        //            General.WriteToLogFile(response_from_api.ToString(), AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
        //            return "";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        General.WriteToLogFile($"{ex.Message} : {device_number}", AppDomain.CurrentDomain.BaseDirectory, $"{device_number}.txt");
        //        return "";
        //    }

        //}

        //public async Task<string> consumeLiveTrackingApi(string JSession, string device_number)
        //{
        //    try
        //    {
        //        var handler = new HttpClientHandler();
        //        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

        //        using (var client = new HttpClient(handler))
        //        {
        //            client.Timeout = TimeSpan.FromSeconds(20);

        //            var url = "https://cctv.trackofy.com/StandardApiAction_vehicleStatus.action"
        //                      + $"?jsession={WebUtility.UrlEncode(JSession)}"
        //                      + $"&vehIdno={WebUtility.UrlEncode(device_number)}"
        //                      + $"&devIdno={WebUtility.UrlEncode(device_number)}"
        //                      + "&toMap=1&geoaddress=0&pageRecords=50&currentPage=1";

        //            var resp = await client.GetAsync(url);
        //            var raw = await resp.Content.ReadAsStringAsync();

        //            if (!resp.IsSuccessStatusCode)
        //            {
        //                General.WriteToLogFile("HTTP " + (int)resp.StatusCode + ": " + raw, AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
        //                return "";
        //            }

        //            var latest = JsonConvert.DeserializeObject<VehicleLatestResponse>(raw);
        //            var mapped = MapLatestToOld(latest);
        //            return JsonConvert.SerializeObject(mapped);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        General.WriteToLogFile(ex.Message + " : " + device_number, AppDomain.CurrentDomain.BaseDirectory, device_number + ".txt");
        //        return "";
        //    }
        //}

        public async Task<string> consumeLiveTrackingApi(string JSession, string device_number_or_plate)
        {
            // simple retry with per-try timeout
            const int maxAttempts = 3;
            TimeSpan perTryTimeout = TimeSpan.FromSeconds(45); // was 20s

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using (var client = new HttpClient(handler, disposeHandler: attempt == maxAttempts))
                using (var cts = new CancellationTokenSource(perTryTimeout))
                {
                    try
                    {
                        client.DefaultRequestHeaders.ConnectionClose = false; // keepalive
                                                                              // NOTE: don’t set client.Timeout; we use CTS per request

                        var url = "https://cctv.trackofy.com/StandardApiAction_getDeviceStatus.action"
                                  + $"?jsession={WebUtility.UrlEncode(JSession)}"
                                  + $"&devIdno={WebUtility.UrlEncode(device_number_or_plate)}"
                                  + $"&vehiIdno={WebUtility.UrlEncode(device_number_or_plate)}"
                                  + "&toMap=1&driver=0&language=en";

                        var resp = await client.GetAsync(url, cts.Token).ConfigureAwait(false);
                        var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(raw))
                        {
                            General.WriteToLogFile(
                                $"Live try#{attempt} HTTP {(int)resp.StatusCode}. Empty/Bad body. URL={url} BODY={raw}",
                                AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                            // retry on 5xx or empty; break on 4xx
                            if ((int)resp.StatusCode >= 500 && attempt < maxAttempts) continue;
                            return "";
                        }

                        // optional sanity: log non-zero result
                        try
                        {
                            var jo = Newtonsoft.Json.Linq.JObject.Parse(raw);
                            if ((int?)jo["result"] != 0)
                                General.WriteToLogFile(
                                    $"getDeviceStatus non-zero result (try#{attempt}): {raw}",
                                    AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                        }
                        catch { /* ignore parse issues */ }

                        return raw; // success
                    }
                    catch (TaskCanceledException tex) // timeout or canceled
                    {
                        General.WriteToLogFile(
                            $"Live try#{attempt} TIMEOUT after {perTryTimeout.TotalSeconds}s: {tex.Message}",
                            AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                        if (attempt == maxAttempts) return "";
                        await Task.Delay(800).ConfigureAwait(false);
                    }
                    catch (HttpRequestException hex)
                    {
                        General.WriteToLogFile(
                            $"Live try#{attempt} HttpRequestException: {hex.Message}",
                            AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                        if (attempt == maxAttempts) return "";
                        await Task.Delay(800).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        General.WriteToLogFile(
                            $"Live try#{attempt} Unexpected: {ex.Message}",
                            AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                        return "";
                    }
                }
            }

            return "";
        }


        public async Task<string> consumePlaybackTrackApi(
    string jsession, string devIdno, string beginTime, string endTime,
    int currentPage, int pageRecords)
        {
            const int maxAttempts = 2;
            TimeSpan perTryTimeout = TimeSpan.FromSeconds(45);

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (s, c, ch, e) => true;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using (var client = new HttpClient(handler, disposeHandler: attempt == maxAttempts))
                using (var cts = new CancellationTokenSource(perTryTimeout))
                {
                    try
                    {
                        var url = "https://cctv.trackofy.com/StandardApiAction_queryTrackDetail.action"
                                  + $"?jsession={WebUtility.UrlEncode(jsession)}"
                                  + $"&devIdno={WebUtility.UrlEncode(devIdno)}"
                                  + $"&begintime={WebUtility.UrlEncode(beginTime)}"
                                  + $"&endtime={WebUtility.UrlEncode(endTime)}"
                                  + "&distance=0&parkTime=0"
                                  + $"&currentPage={currentPage}"
                                  + $"&pageRecords={pageRecords}"
                                  + "&toMap=1";

                        var resp = await client.GetAsync(url, cts.Token).ConfigureAwait(false);
                        var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (!resp.IsSuccessStatusCode)
                        {
                            General.WriteToLogFile(
                                $"PB try#{attempt} HTTP {(int)resp.StatusCode}. URL={url} BODY={raw}",
                                AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                            if ((int)resp.StatusCode >= 500 && attempt < maxAttempts) continue;
                            return "";
                        }

                        return raw;
                    }
                    catch (TaskCanceledException)
                    {
                        General.WriteToLogFile(
                            $"PB try#{attempt} TIMEOUT after {perTryTimeout.TotalSeconds}s",
                            AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                        if (attempt == maxAttempts) return "";
                        await Task.Delay(800).ConfigureAwait(false);
                    }
                    catch (HttpRequestException hex)
                    {
                        General.WriteToLogFile(
                            $"PB try#{attempt} HttpRequestException: {hex.Message}",
                            AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                        if (attempt == maxAttempts) return "";
                        await Task.Delay(800).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        General.WriteToLogFile(
                            $"PB try#{attempt} Unexpected: {ex.Message}",
                            AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
                        return "";
                    }
                }
            }

            return "";
        }




        //public async Task<string> consumePlayBackTrackingApi(string username, string password)
        //{
        //    //General.WriteToLogFile("insideApi", AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
        //    try
        //    {
        //        HttpClientHandler handler = new HttpClientHandler();
        //        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        //        var client = new HttpClient(handler);
        //        //HttpResponseMessage response_from_api = await client.GetAsync($"https://cctv.abctraq.com:9443/StandardApiAction_login.action?account={username}&password={password}");
        //        HttpResponseMessage response_from_api = await client.GetAsync($"https://cctv.abctraq.com:9443/StandardApiAction_queryTrackDetail.action?account={username}&password={password}");

        //        if (response_from_api.IsSuccessStatusCode)
        //        {
        //            var getJsession_included_data = await response_from_api.Content.ReadAsStringAsync();
        //            var getJsession_included_data_json = JsonConvert.DeserializeObject<TrackingAPIvariable>(getJsession_included_data);
        //            //string jsession = getJsession_included_data_json.Jsession;
        //            return "";

        //        }
        //        else
        //        {
        //            General.WriteToLogFile(response_from_api.ToString(), AppDomain.CurrentDomain.BaseDirectory, "apiResponse.txt");
        //            return "";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        General.WriteToLogFile(ex.Message, AppDomain.CurrentDomain.BaseDirectory, $"{username}.txt");
        //        return "";
        //    }

        //}

        private static TrackingAPIvariable MapLatestToOld(VehicleLatestResponse src)
        {
            var list = new List<data>();
            if (src != null && src.infos != null)
            {
                foreach (var x in src.infos)
                {
                    // ms -> "yyyy-MM-dd HH:mm:ss" (UTC)
                    var ts = DateTimeOffset.FromUnixTimeMilliseconds(x.tm)
                                           .UtcDateTime
                                           .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                    list.Add(new data
                    {
                        id = x.vi ?? "",
                        vid = x.vi ?? "",
                        //mlng = x.jd.ToString(CultureInfo.InvariantCulture),
                        //mlat = x.wd.ToString(CultureInfo.InvariantCulture),
                        mlng = (x.jd ?? 0).ToString(CultureInfo.InvariantCulture),
                        mlat = (x.wd ?? 0).ToString(CultureInfo.InvariantCulture),


                        sp = "0",
                        ol = "1",
                        hx = "0",
                        yl = "0",

                        gt = ts
                    });
                }
            }

            return new TrackingAPIvariable
            {
                result = (src != null ? src.result : 1).ToString(),
                status = list
            };
        }



    }


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OData.Client
{
    static class HttpHelpers
    {
        public static async Task Patch(HttpMessageInvoker invoker, string url, object value)
        {
            string jsonValue = Serialize(value);
#if DEBUG
            StringBuilder sbLog = new();
            AppendRequestInfo(sbLog, invoker, "PATCH", url);
            AppendBodyInfo(sbLog, jsonValue);
#endif
            HttpResponseMessage responseMessage = await invoker.SendAsync(
               new HttpRequestMessage(
                   HttpMethod.Patch,
                   url
               )
               { 
                   Content = new StringContent(jsonValue, Encoding.UTF8, "application/json")
               }, default);

            var json = await responseMessage.Content.ReadAsStringAsync();
#if DEBUG
            AppendResponseInfo(sbLog, json);
            Console.WriteLine(sbLog);
#endif
            ThrowErrorIfNotOk(responseMessage, json);
        }

        public static async Task Post(HttpMessageInvoker invoker, string url, object value)
        {
            string jsonValue = Serialize(value);
#if DEBUG
            StringBuilder sbLog = new();
            AppendRequestInfo(sbLog, invoker, "POST", url);
            AppendBodyInfo(sbLog, jsonValue);
#endif
            HttpResponseMessage responseMessage = await invoker.SendAsync(
               new HttpRequestMessage(
                   HttpMethod.Post,
                   url
               )
               {
                   Content = new StringContent(jsonValue, Encoding.UTF8, "application/json")
               }, default);

            var json = await responseMessage.Content.ReadAsStringAsync();
#if DEBUG
            AppendResponseInfo(sbLog, json);
            Console.WriteLine(sbLog);
#endif
            ThrowErrorIfNotOk(responseMessage, json);
        }

        static string Serialize<TValue>(TValue value)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            return JsonSerializer.Serialize(value, options);
        }

        public static async Task Delete(HttpMessageInvoker invoker, string url)
        {
#if DEBUG
            StringBuilder sbLog = new();
            AppendRequestInfo(sbLog, invoker, "DELETE", url);
#endif
            HttpResponseMessage responseMessage = await invoker.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Delete,
                    url
                ), default);

            var json = await responseMessage.Content.ReadAsStringAsync();

#if DEBUG
            AppendResponseInfo(sbLog, json);
            Console.WriteLine(sbLog);
#endif
            ThrowErrorIfNotOk(responseMessage, json);
        }

        public static async Task<TResult> Get<TResult>(HttpMessageInvoker invoker, string url)
        {
#if DEBUG
            StringBuilder sbLog = new();
            AppendRequestInfo(sbLog, invoker, "GET", url);
#endif
            HttpResponseMessage responseMessage = await invoker.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url
                ), default);

            var json = await responseMessage.Content.ReadAsStringAsync();

#if DEBUG
            AppendResponseInfo(sbLog, json);
            Console.WriteLine(sbLog);
#endif
            ThrowErrorIfNotOk(responseMessage, json);

            var jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<TResult>(json, jsonSerializerOptions);

            return result;
        }

     
        static void AppendRequestInfo(StringBuilder sbLog, HttpMessageInvoker invoker, string method, string url)
        {
            sbLog.AppendLine();
            sbLog.AppendLine("########################");
            sbLog.AppendLine("# ODataClient");
            sbLog.AppendLine("#");
            sbLog.AppendLine("# TOKEN: " + (invoker as HttpClient).DefaultRequestHeaders.Authorization?.Parameter);
            sbLog.AppendLine("#");
            sbLog.AppendLine($"# {method}");
            sbLog.AppendLine("#");
            sbLog.AppendLine("# " + url.Replace("?", "\n  ?").Replace("&", "\n  &"));
            sbLog.AppendLine("#");
        }

        static void AppendBodyInfo(StringBuilder sbLog, string json)
        {
            sbLog.AppendLine();
            sbLog.AppendLine("# BODY ");
            sbLog.AppendLine("#");
            sbLog.AppendLine(json);
            sbLog.AppendLine("#");
        }

        static void AppendResponseInfo(StringBuilder sbLog, string responseText)
        {
            int limit = 40000;
            string formattedResponseText;
            try
            {
                formattedResponseText = string.Join("",
                        JsonSerializer
                        .Serialize(JsonSerializer.Deserialize<Dictionary<string, object>>(responseText), new JsonSerializerOptions { WriteIndented = true })
                        .Take(limit)
                        );
            }
            catch
            {
                formattedResponseText = responseText;
            }
            sbLog.Append("# " + formattedResponseText.Replace("\n", "\n# "));
            if (formattedResponseText.Length >= limit)
            {
                sbLog.AppendLine("# ...");
            }
            sbLog.AppendLine();
            sbLog.AppendLine("###########");
            sbLog.AppendLine();
        }

        static void ThrowErrorIfNotOk(HttpResponseMessage responseMessage, string json)
        {
            if (responseMessage.StatusCode != HttpStatusCode.OK && responseMessage.StatusCode != HttpStatusCode.NoContent)
            {
                throw new HttpRequestException(responseMessage.StatusCode.ToString() + ": " + json);
            }
        }

    }
}

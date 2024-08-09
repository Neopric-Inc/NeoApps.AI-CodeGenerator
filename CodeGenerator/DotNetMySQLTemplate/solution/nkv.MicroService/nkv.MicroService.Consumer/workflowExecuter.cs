using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using nkv.MicroService.Utility;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace nkv.MicroService.Consumer
{
    public class workflowExecuter
    {
        //To Fetch Data From AppSettings.json making configuration 
        private IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
        private async Task<HttpResponseMessage> GenerateToken()
        {
            // Create an instance of HttpClient
            using (var httpClient = new HttpClient())
            {
                // Set the base URL
                httpClient.BaseAddress = new Uri(configuration.GetSection("ConnectionStrings")["TokenAPI"]);

                // Create the request body as a JSON string
                var requestBody = new AuthenticateModel
                {
                    Username = configuration.GetSection("AppSettings")["DefaultTokenUsername"],
                    Password = configuration.GetSection("AppSettings")["DefaultTokenPassword"]
                };
                string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);

                // Set the Content-Type header
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // Create the HTTP POST request and set the payload
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Send the request and get the response
                var response = await httpClient.PostAsync("Token", content);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                else
                {
                    return response;
                }
            }
        }
        public async Task<APIResponse> TriggerWorkflowAsync(string nodeRedURL, Object obj, string action, string className, string token)
        {

            using (var httpClient = new HttpClient())
            {
                //var res = await Task.Run(async () => await GenerateToken());

                try
                {
                    //Parsing Result into APIResponse and TokenResponse to get Token as String
                    //string result = await res.Content.ReadAsStringAsync();
                    //APIResponse response_object = JsonConvert.DeserializeObject<APIResponse>(result);
                    //TokenResponse tokenDetail = JsonConvert.DeserializeObject<TokenResponse>(response_object.Document.ToString());
                    //var token = tokenDetail.AccessToken;

                    //Removing Model From className 
                    className = className.Substring(0, className.Length - "Model".Length);

                    //Converting Object into JSON and Perparing it to pass in payload
                    var payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
                    var payloadContent = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                    // Set the Authorization header with the token
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // Set the Content-Type header
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = await httpClient.PostAsync($"{nodeRedURL}/{className}/{action}", payloadContent);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Workflow triggered successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to trigger workflow. Error: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    return new APIResponse(ResponseCode.ERROR, "Exception", ex.Message);
                }
                return new APIResponse(ResponseCode.SUCCESS, "Workflow Triggered Successfully");


            }
        }

        public async Task<APIResponse> TriggerWorkflowAsync(string nodeRedURL, int id, string action, string className, string token)
        {

            using (var httpClient = new HttpClient())
            {
                // var res = await Task.Run(async () => await GenerateToken());
                // if (res.IsSuccessStatusCode)
                // {
                try
                {
                    //Parsing Result into APIResponse and TokenResponse to get Token as String
                    // string result = await res.Content.ReadAsStringAsync();
                    // APIResponse response_object = JsonConvert.DeserializeObject<APIResponse>(result);
                    // TokenResponse tokenDetail = JsonConvert.DeserializeObject<TokenResponse>(response_object.Document.ToString());
                    // var token = tokenDetail.AccessToken;

                    //Removing Model From className 
                    className = className.Substring(0, className.Length - "Model".Length);

                    //Converting Object into JSON and Perparing it to pass in payload
                    var payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(id);
                    var payloadContent = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                    // Set the Authorization header with the token
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // Set the Content-Type header
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = await httpClient.PostAsync($"{nodeRedURL}/{className}/{action}", payloadContent);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Workflow triggered successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to trigger workflow. Error: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    return new APIResponse(ResponseCode.ERROR, "Exception", ex.Message);
                }
                return new APIResponse(ResponseCode.SUCCESS, "Workflow Triggered Successfully");
                // }
                // else
                // {
                //     return new APIResponse(ResponseCode.ERROR, "Failed to Create the Token", res);
                // }
            }
        }

        public async Task TriggerWorkflowAsync(string nodeRedURL, string action, string className)
        {
            using (var httpClient = new HttpClient())
            {
                className = className.Substring(0, className.Length - "Model".Length);
                var importUrlPost = $"{nodeRedURL}/{className}/{action}";
                //code for importing flow from Database
                //var importContent = new StringContent(workflowJson, Encoding.UTF8, "application/json");
                //var importResponse = await httpClient.PostAsync(importUrlPost, importContent);
                var response = await httpClient.PostAsync(importUrlPost, null);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Workflow triggered successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to trigger workflow. Error: {response.StatusCode}");
                }
            }
        }
    }
}

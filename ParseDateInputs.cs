using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using DateOverlapCalc.OpenAI.Davinci;
using static DateOverlapCalc.OpenAI.Davinci.OpenAIBody;

namespace DateOverlapCalc
{
    public static class ParseDateInputs
    {
        public static bool isDavinci = false;

        public static async Task<string> GetAIResponse(string input, string key)
        {
            // Create the request
            OpenAIBody InputBody = new OpenAIBody(input);
            var json = JsonConvert.SerializeObject(InputBody);

            var OpenAIClient = new HttpClient(key);

            string response = await OpenAIClient.GetCompletion(input);

            return response;
        }

        [FunctionName("ParseDateInputs")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log
        )
        {
            string OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            string OPENAI_ORG = Environment.GetEnvironmentVariable("OPENAI_ORG");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string response = "No Data";
            if (isDavinci)
            {
                string prompt =
                    "Convert the following list into JSON. The json should be in this format: { \"projects\": [ { \"name\": \"project 1\", \"start\": \"December 2010\", \"end\": \"june 2012\" }, \"maxMonths\"?: 810 }";
                string fullPrompt = $"{prompt}. {data}. JSON:";

                // Create the request
                response = await GetAIResponse(fullPrompt, OPENAI_API_KEY);
            }
            else
            {
                string SystemPrompt =
                    "You are a ChatBot created to help parse CVs and extract dates. You also need to help determine if projects qualify for certain roles. When given a list of projects, you might be asked to convert it into JSON, this is the expected format: { \"projects\": [ { \"name\": \"project 1\", \"start\": \"December 2010\", \"end\": \"june 2012\" }, \"maxMonths\"?: 810 }";
                string UserPrompt = $"Parse this into JSON: {data}";
            }

            return new OkObjectResult(response);
        }
    }
}

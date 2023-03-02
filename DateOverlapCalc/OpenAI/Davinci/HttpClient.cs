using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace DateOverlapCalc.OpenAI.Davinci
{
    struct OpenAIUsage
    {
        public short prompt_tokens;
        public short completion_tokens;
        public short total_tokens;
    }

    struct OpenAIChoice
    {
        public string text;
        public int index;
        public string logprobs;
        public string finish_reason;
    }

    class OpenAICompletionResponse
    {
        public string id;
        public string @object;
        public string created;
        public string model;
        public OpenAIChoice[] choices;
        public OpenAIUsage usage;

        public OpenAICompletionResponse(string id, string @object, string created, string model, OpenAIChoice[] choices, OpenAIUsage usage)
        {
            this.id = id;
            this.@object = @object;
            this.created = created;
            this.model = model;
            this.choices = choices;
            this.usage = usage;
        }
    }

    class OpenAIBody
    {
        [JsonInclude]
        public string model = Model.Davinci3;
        public string prompt;
        public string suffix;
        public short max_tokens = 16;
        public float temperature = 1f;
        public float top_p = 1f;
        public short n = 1;
        public float frequency_penalty = 0f;
        public float presence_penalty = 0f;

        public OpenAIBody(string prompt)
        {
            this.prompt = prompt;
            temperature = 0.9f;
            max_tokens = 1000;
        }

        public class HttpClient : System.Net.Http.HttpClient
        {
            private static readonly string url = Url.Completion;
            private System.Net.Http.HttpClient client;
            public HttpClient(string key)
            {
                client = new System.Net.Http.HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            public new async Task<string> SendAsync(HttpRequestMessage req)
            {
                using (HttpResponseMessage responsePost = await client.SendAsync(req))
                {
                    responsePost.EnsureSuccessStatusCode();

                    return await responsePost.Content.ReadAsStringAsync();
                }
            }

            public async Task<string> GetCompletion(string prompt)
            {
                OpenAIBody InputBody = new OpenAIBody(prompt);
                var json = JsonConvert.SerializeObject(InputBody);

                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                string response = await SendAsync(req);

                var response1 = JsonConvert.DeserializeObject<OpenAICompletionResponse>(response);

                return response1.choices[0].text;
            }
        }
    }
}

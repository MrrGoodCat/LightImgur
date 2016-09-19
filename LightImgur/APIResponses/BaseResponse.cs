using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LightImgur.APIResponses
{
    public class BaseResponse
    {
        public class BaseData
        {
            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("method")]
            public string Method { get; set; }

            [JsonProperty("parameters")]
            public string Parameters { get; set; }

            [JsonProperty("request")]
            public string Request { get; set; }
        }
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }
    }
}

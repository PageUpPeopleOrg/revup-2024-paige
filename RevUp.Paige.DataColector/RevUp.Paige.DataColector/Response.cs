using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevUp.Paige.DataColector
{
    public class Response
    {
        [JsonProperty("articles")]
        public IList<Article> Articles { get; set; }
    }
}

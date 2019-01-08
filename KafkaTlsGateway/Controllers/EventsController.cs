using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace KafkaTlsGateway.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly Producer producer;
        private readonly Random seed = new Random();

        public ValuesController(Producer producer)
        {
            this.producer = producer;
        }
        
        [HttpPost]
        public async Task Post([FromBody] Event e)
        {
            var key = (long) (seed.NextDouble() * 100000);
            await producer.PublishNext(BitConverter.GetBytes(key),
                Encoding.UTF8.GetBytes(e.Value));
        }
    }
}
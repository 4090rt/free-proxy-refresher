using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ModelData.JsonPtoxyTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestShadowsocks
{
    public class TestCircleShadow
    {
        private readonly ILogger<TestCircleShadow> _logger;
        private readonly TestShadowTunnelCreate _testShadowTunnelCreate;

        public TestCircleShadow(ILogger<TestCircleShadow> logger, TestShadowTunnelCreate testShadowTunnelCreate)
        {
            _logger = logger;
            _testShadowTunnelCreate = testShadowTunnelCreate;
        }

        public async Task<TesteProxyModelCunt> CircleMethod(CancellationToken cancellationToken)
        { 
            
        }
    }
}

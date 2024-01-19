using System.Net;
using System.Threading.Tasks;
using TinCan;
using TinCan.Documents;
using Xunit;

namespace Float.TinCan.LocalLRSServer.Tests
{
    class SimpleDelegate : ILRSServerDelegate
    {
        public AgentProfileDocument AgentProfileDocumentForProfileId(string profileId)
        {
            return new AgentProfileDocument();
        }

        public string GetAccessConrolAllowOrigin()
        {
            return null;
        }

        public void AlterAgentProfileResponse(HttpListenerRequest request, ref HttpListenerResponse response, ref AgentProfileDocument profileDocument)
        {
            return;
        }
    }

    public class LRSServerTests
    {
        [Fact]
        public void TestInit()
        {
            _ = new LRSServer();
            _ = new LRSServer(serverDelegate: new SimpleDelegate());
        }

        [Fact]
        public async Task TestResponse()
        {
            var lrs = new RemoteLRS("http://127.0.0.1:8122", "username", "password");
            var server = new LRSServer(port: 8122);
            server.Start();

            var response = await lrs.SaveStatement(new Statement());
            Assert.NotNull(response);
        }
    }
}

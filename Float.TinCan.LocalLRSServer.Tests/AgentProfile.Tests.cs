using System;
using System.Text;
using System.Threading.Tasks;
using TinCan;
using TinCan.Documents;
using Xunit;

namespace Float.TinCan.LocalLRSServer.Tests
{
    public class TestAgentProfile
    {
        class StubServerDelegate : ILRSServerDelegate
        {
            readonly Agent agent;

            internal StubServerDelegate(Agent agent)
            {
                this.agent = agent;
            }

            public AgentProfileDocument AgentProfileDocumentForProfileId(string profileId)
            {
                return new AgentProfileDocument
                {
                    agent = agent,
                    id = profileId,
                    content = Encoding.UTF8.GetBytes("this is a test string")
                };
            }

            public string GetAccessConrolAllowOrigin()
            {
                return null;
            }
        }

        readonly LRSServer localLRS;
        readonly RemoteLRS remoteLRS;
        readonly StubServerDelegate stubDelegate;

        readonly Agent testAgent = new Agent
        {
            name = "Test User",
            mbox = "test@user.com",
            account = new AgentAccount
            {
                homePage = new Uri("http://www.example.com"),
                name = "Example.com"
            }
        };

        static ushort port = 8981;

        public TestAgentProfile()
        {
            var urlString = "http://127.0.0.1";
            stubDelegate = new StubServerDelegate(testAgent);
            localLRS = new LRSServer(urlString, port, stubDelegate);
            localLRS.Start();
            remoteLRS = new RemoteLRS($"{urlString}:{port}/", "username", "password");
            port++;
        }

        [Fact]
        public async Task TestCanSaveAgentProfile()
        {
            var didReceive = false;
            var document = new AgentProfileDocument();

            localLRS.AgentProfileDocumentReceived += (lrs, args) =>
            {
                didReceive = true;
                document = args.AgentProfileDocument;
            };

            var doc = new AgentProfileDocument
            {
                id = "test_id",
                timestamp = new DateTime(),
                contentType = "text/html",
                content = Encoding.UTF8.GetBytes("this is test content"),
                agent = testAgent
            };

            var response = await remoteLRS.SaveAgentProfile(doc);

            Assert.NotNull(response);
            Assert.True(response.success);
            Assert.True(didReceive);
            Assert.Equal("test_id", document.id);

            // BUG: remoteLRS.SaveAgentProfile
            // does not decode mbox so
            // we cannot test for agent equality
            Assert.NotNull(document.agent);
            Assert.NotNull(document.content);
        }

        [Fact]
        public async Task TestCanGetAgentProfile()
        {
            var doc = new AgentProfileDocument
            {
                id = "test_id",
                timestamp = new DateTime(),
                contentType = "text/html",
                content = Encoding.UTF8.GetBytes("this is test content"),
                agent = testAgent
            };

            await remoteLRS.SaveAgentProfile(doc);

            var response = await remoteLRS.RetrieveAgentProfile("test_id", testAgent);

            var result = Encoding.UTF8.GetString(response.content.content);

            Assert.NotNull(response);
            Assert.NotNull(response.content);
            Assert.Null(response.errMsg);
            Assert.Null(response.Error);
            Assert.Null(response.httpException);
            Assert.True(response.success);
            Assert.Equal("application/json", response.content.contentType);
            Assert.Null(response.content.etag);
            Assert.Null(response.content.id);
            Assert.NotEmpty(response.content.content);

            // there's actually a bug in RemoteLRS: the agent and id are not written to the response
            // Assert.Equal(testAgent, response.content.agent);
            // Assert.Equal("test_id", response.content.id);

            Assert.Equal("this is a test string", result);
        }

        ~TestAgentProfile()
        {
            localLRS.Close();
        }
    }
}

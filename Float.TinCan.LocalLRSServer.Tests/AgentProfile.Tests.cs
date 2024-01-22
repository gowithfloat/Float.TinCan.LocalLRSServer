using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TinCan;
using TinCan.Documents;
using Xunit;

namespace Float.TinCan.LocalLRSServer.Tests
{
    public sealed class TestAgentProfile: IDisposable
    {
        class StubServerDelegate : ILRSServerDelegate
        {
            readonly Agent agent;
            AgentProfileDocument tempProfileDocument;

            internal StubServerDelegate(Agent agent)
            {
                this.agent = agent;
            }

            public AgentProfileDocument AgentProfileDocumentForProfileId(string profileId)
            {
                if (tempProfileDocument != null)
                {
                    return tempProfileDocument;
                }

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

            public void AlterAgentProfileResponse(HttpListenerRequest request, ref HttpListenerResponse response, ref AgentProfileDocument profileDocument)
            {
                if (request.HttpMethod.ToString() != "GET")
                {
                    tempProfileDocument = new AgentProfileDocument
                    {
                        agent = profileDocument.agent,
                        content = profileDocument.id != "testing_save" ? profileDocument.content : Encoding.UTF8.GetBytes("this is transformed test content"),
                        contentType = profileDocument.contentType,
                        id = profileDocument.id,
                        timestamp = profileDocument.timestamp,
                        etag = profileDocument.etag
                    };
                }
                return;
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
            var doc = new AgentProfileDocument
            {
                id = "testing_save",
                timestamp = new DateTime(),
                contentType = "text/html",
                content = Encoding.UTF8.GetBytes("this is test content"),
                agent = testAgent
            };

            var saveResponse = await remoteLRS.SaveAgentProfile(doc);
            Assert.True(saveResponse.success);

            var response = await remoteLRS.RetrieveAgentProfile("test_id", testAgent);

            var result = Encoding.UTF8.GetString(response.content.content);
            Assert.NotNull(response);
            Assert.True(response.success);

            // BUG: remoteLRS.SaveAgentProfile
            // does not decode mbox so
            // we cannot test for agent equality
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
            Assert.Equal("this is transformed test content", result);
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

            Assert.Equal("this is test content", result);
        }

        ~TestAgentProfile()
        {
            localLRS.Close();
        }

        public void Dispose() => localLRS.Dispose();
    }
}

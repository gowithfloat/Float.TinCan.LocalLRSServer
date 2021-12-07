using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Float.TinCan.LocalLRSServer.Tests
{
    class SimpleHttpServer : HttpServer
    {
        internal SimpleHttpServer(string address = "http://127.0.0.1", ushort port = 8080, string suffix = "/") : base(address, port, suffix)
        {
        }

        protected override void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.OutputStream.Close();
        }
    }

    public class HttpServerTests
    {
        [Fact]
        public void TestInit()
        {
            Assert.Throws<ArgumentException>(() => new SimpleHttpServer(null));
            Assert.Throws<ArgumentException>(() => new SimpleHttpServer(string.Empty));
            Assert.Throws<ArgumentException>(() => new SimpleHttpServer(" "));
            Assert.Throws<ArgumentException>(() => new SimpleHttpServer("aaaa"));
            Assert.Throws<ArgumentException>(() => new SimpleHttpServer("float://www.test.com"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimpleHttpServer(port: 1000));
            Assert.Throws<ArgumentException>(() => new SimpleHttpServer(suffix: null));
            Assert.Throws<ArgumentException>(() => new SimpleHttpServer(suffix: string.Empty));
            Assert.Throws<ArgumentException>(() => new SimpleHttpServer(suffix: " "));
            Assert.Throws<ArgumentException>(() => new SimpleHttpServer(suffix: "invalid"));

            var server1 = new SimpleHttpServer();
            var server2 = new SimpleHttpServer(suffix: "valid/");
            var server3 = new SimpleHttpServer("http://198.162.3.100");
            var server4 = new SimpleHttpServer(port: 8922);
            var server5 = new SimpleHttpServer(port: 65000);
            var server6 = new SimpleHttpServer(suffix: "/test/");
        }

        [Fact]
        public void TestLifecycle()
        {
            var server = new SimpleHttpServer();
            Assert.False(server.IsListening);

            server.Start();
            Assert.True(server.IsListening);

            server.Stop();
            Assert.False(server.IsListening);

            server.Start();
            Assert.True(server.IsListening);

            server.Close();
            Assert.False(server.IsListening);
            Assert.Throws<ObjectDisposedException>(() => server.Start());
        }

        [Fact]
        public async Task TestRequest()
        {
            var server = new SimpleHttpServer();
            server.Start();

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri("http://127.0.0.1:8080")
                };

                var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            server.Close();
        }

        [Fact]
        public void TestUrl()
        {
            var server1 = new SimpleHttpServer();
            var server2 = new SimpleHttpServer(suffix: "invalid/");
            var server3 = new SimpleHttpServer("http://198.162.3.100");
            var server4 = new SimpleHttpServer(port: 8922);
            var server5 = new SimpleHttpServer(port: 65000);
            var server6 = new SimpleHttpServer(suffix: "/test/");

            Assert.Equal(server1.Url.AbsoluteUri, new Uri("http://127.0.0.1:8080/").AbsoluteUri);
            Assert.Throws<UriFormatException>(() => server2.Url);
            Assert.Equal(server3.Url.AbsoluteUri, new Uri("http://198.162.3.100:8080/").AbsoluteUri);
            Assert.Equal(server4.Url.AbsoluteUri, new Uri("http://127.0.0.1:8922/").AbsoluteUri);
            Assert.Equal(server5.Url.AbsoluteUri, new Uri("http://127.0.0.1:65000/").AbsoluteUri);
            Assert.Equal(server6.Url.AbsoluteUri, new Uri("http://127.0.0.1:8080/test/").AbsoluteUri);
        }
    }
}

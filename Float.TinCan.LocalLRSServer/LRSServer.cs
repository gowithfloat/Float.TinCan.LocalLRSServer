using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TinCan;
using TinCan.Documents;
using TinCan.Json;

namespace Float.TinCan.LocalLRSServer
{
    /// <summary>
    /// A local HTTP server that handles LRS statements.
    /// </summary>
    public class LRSServer : HttpServer
    {
        readonly ILRSServerDelegate serverDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="LRSServer"/> class.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="port">The port to listen to requests on.</param>
        /// <param name="serverDelegate">A delegate to craft server responses. Optional.</param>
        public LRSServer(string address = "http://127.0.0.1", ushort port = 8080, ILRSServerDelegate serverDelegate = null) : base(address, port)
        {
            this.serverDelegate = serverDelegate;
        }

        /// <summary>
        /// A delegate prototype for all State Resource request and responses.
        /// </summary>
        /// <param name="request">The HTTP request from the web view.</param>
        /// <param name="response">The response that will be sent back to the web view.</param>
        public delegate void StateRequest(HttpListenerRequest request, HttpListenerResponse response);

        /// <summary>
        /// Event to be raised when the local LRS receives activity and verb information.
        /// </summary>
        public event EventHandler<StatementEventArgs> StatementReceived;

        /// <summary>
        /// Event to be raised when the local LRS receives agent profile document information.
        /// </summary>
        public event EventHandler<AgentProfileDocumentEventArgs> AgentProfileDocumentReceived;

        /// <summary>
        /// Gets or sets delegate method for State Get requests.
        /// </summary>
        /// <value>The delegate method for state get requests.</value>
        public StateRequest StateGetRequest { get; set; }

        /// <summary>
        /// Gets or sets delegate method for State Put requests.
        /// </summary>
        /// <value>The delegate method for state put requests.</value>
        public StateRequest StatePutRequest { get; set; }

        /// <summary>
        /// Gets or sets delegate method for State Push requests.
        /// </summary>
        /// <value>The delegate method for state post requests.</value>
        public StateRequest StatePostRequest { get; set; }

        /// <summary>
        /// Gets or sets delegate method for State Delete requests.
        /// </summary>
        /// <value>The delegate method for state delete requests.</value>
        public StateRequest StateDeleteRequest { get; set; }

        /// <summary>
        /// Gets the encoded URL of this endpoint.
        /// </summary>
        /// <value>The encoded URL.</value>
#pragma warning disable CA1056 // Uri properties should not be strings
        public string EncodedUrl => WebUtility.UrlEncode(Prefix);
#pragma warning restore CA1056 // Uri properties should not be strings

        /// <summary>
        /// Handles the request. Stores locally or sends to the server if needed.
        /// </summary>
        /// <param name="request">The HTTP request from the web view.</param>
        /// <param name="response">The response that will be sent back to the web view.</param>
        protected override void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (!string.IsNullOrEmpty(serverDelegate?.GetAccessConrolAllowOrigin()))
            {
                response.AddHeader("Access-Control-Allow-Origin", serverDelegate?.GetAccessConrolAllowOrigin());
            }
            else
            {
                response.AddHeader("Access-Control-Allow-Origin", "file://");
            }

            if (request.HttpMethod == "OPTIONS")
            {
                HandlePreflightRequest(request, response);
            }
            else
            {
                switch (request.Url.LocalPath)
                {
                    case "/statements":
                        HandleStatementsRequest(request, response);
                        break;
                    case "/activities/state":
                        HandleStateRequest(request, response);
                        break;
                    case "/agents/profile":
                        HandleAgentProfileRequest(request, response);
                        break;
                    default:
                        // todo: we should not always respond "no content" in other cases
                        SendResponse(response, HttpStatusCode.NoContent);
                        break;
                }
            }
        }

        /// <summary>
        /// Handles adding parameters to the preflight request.
        /// </summary>
        /// <param name="request">The HTTP request from the client.</param>
        /// <param name="response">The HTTP response to add headers to.</param>
        protected virtual void HandlePreflightRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, DELETE, PUT");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, Origin, Accept, User-Agent, X-Experience-API-Version, If-Match, If-None-Match");
            SendResponse(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// Handles requests that are specifically related to xAPI statements.
        /// </summary>
        /// <param name="request">The HTTP request to get payload data from.</param>
        /// <param name="response">The HTTP response to write a statement ID to.</param>
        void HandleStatementsRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.HttpMethod == "GET")
            {
                // TODO: someday implement this
                SendResponse(response, HttpStatusCode.NotImplemented);
                return;
            }

            var bytes = new byte[request.ContentLength64];
            request.InputStream.Read(bytes, 0, (int)request.ContentLength64);
            var payload = Encoding.UTF8.GetString(bytes);

            var statements = new List<Statement>();

            try
            {
                var payloadObjects = JsonConvert.DeserializeObject(payload);

                if (payloadObjects is JArray arr)
                {
                    foreach (var eachObject in arr)
                    {
                        statements.Add(new Statement(eachObject as JObject));
                    }
                }
                else if (payloadObjects is JObject obj)
                {
                    statements.Add(new Statement(obj));
                }
                else
                {
                    SendResponse(response, HttpStatusCode.BadRequest);
                }

                foreach (var eachStatement in statements)
                {
                    eachStatement.Stamp();

                    // if we found a valid verb and activity in the request, create an event
                    var args = new StatementEventArgs(eachStatement);
                    RaiseStatementEvent(args);
                }

                if (request.HttpMethod == "PUT")
                {
                    response.StatusCode = (int)HttpStatusCode.NoContent;
                    response.OutputStream.Close();
                }
                else if (request.HttpMethod == "POST")
                {
                    var statementIds = statements.Select((arg) => arg.id.Value.ToString());
                    var statementsJson = JsonConvert.SerializeObject(statementIds);
                    WriteToStream(response, statementsJson, ContentType.Json, HttpStatusCode.OK);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                SendResponse(response, HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Handles requests that are specifically related to xAPI State Resources.
        /// </summary>
        /// <param name="request">The HTTP request to get payload data from.</param>
        /// <param name="response">The HTTP response to write the response to.</param>
        void HandleStateRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            switch (request.HttpMethod)
            {
                case "GET":
                    StateGetRequest?.Invoke(request, response);
                    break;
                case "POST":
                    StatePostRequest?.Invoke(request, response);
                    break;
                case "PUT":
                    StatePutRequest?.Invoke(request, response);
                    break;
                case "DELETE":
                    StateDeleteRequest?.Invoke(request, response);
                    break;
            }
        }

        /// <summary>
        /// Handles requests related to xAPI agent profile resource requests.
        /// </summary>
        /// <param name="request">The HTTP request to get query params from.</param>
        /// <param name="response">The HTTP response, which will get a No Content response.</param>
        void HandleAgentProfileRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            var method = new HttpMethod(request.HttpMethod).ToString();
            var profileIdString = request.QueryString["profileId"];
            AgentProfileDocument profileDocument = null;
            try
            {
                switch (method)
                {
                    case string m when HttpMethod.Get.ToString() == method:
                        if (string.IsNullOrWhiteSpace(profileIdString))
                        {
                            // todo: return all available IDs
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                            throw new NotImplementedException("GET requests for all documents are not yet implemented");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                        }

                        if (serverDelegate != null)
                        {
                            profileDocument = serverDelegate.AgentProfileDocumentForProfileId(profileIdString);
                        }

                        WriteToStream(response, profileDocument.content, ContentType.Json, HttpStatusCode.OK);
                        return;
                    case string m when HttpMethod.Post.ToString() == method:
                    case string m1 when HttpMethod.Put.ToString() == method:
                        profileDocument = new AgentProfileDocument
                        {
                            id = WebUtility.UrlDecode(profileIdString),
                        };

                        var agentString = WebUtility.UrlDecode(request.QueryString["agent"]);

                        if (agentString != null)
                        {
                            profileDocument.agent = new Agent(new StringOfJSON(agentString));
                        }

                        Encoding encoding = request.ContentEncoding;
                        var bytes = new byte[request.ContentLength64];
                        request.InputStream.Read(bytes, 0, (int)request.ContentLength64);
                        profileDocument.content = bytes;
                        profileDocument.contentType = request.ContentType;
                        RaiseAgentProfileDocumentEvent(new AgentProfileDocumentEventArgs(profileDocument));

                        if (serverDelegate != null)
                        {
                            serverDelegate.AlterAgentProfileResponse(request, ref response, ref profileDocument);
                        }

                        SendResponse(response, HttpStatusCode.NoContent);
                        return;
                    case string m when HttpMethod.Delete.ToString() == method:
                        if (serverDelegate != null)
                        {
                            serverDelegate.AlterAgentProfileResponse(request, ref response, ref profileDocument);
                            SendResponse(response, HttpStatusCode.NoContent);
                        }
                        else
                        {
                            // todo: implement delete requests
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                            throw new NotImplementedException("DELETE requests are not yet implemented");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                        }

                        return;
                    default:
                        throw new InvalidOperationException($"Only GET, DELETE, PUT, and POST are supported for {nameof(HandleAgentProfileRequest)}. Received {method}");
                }
            }

#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                SendResponse(response, HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Method to handle statement events.
        /// </summary>
        /// <param name="args">Statement event arguments, containing statement data.</param>
        void RaiseStatementEvent(StatementEventArgs args)
        {
            StatementReceived?.Invoke(this, args);
        }

        /// <summary>
        /// Method to handle agent profile document events.
        /// </summary>
        /// <param name="args">Agent profile document event arguments, containing document data.</param>
        void RaiseAgentProfileDocumentEvent(AgentProfileDocumentEventArgs args)
        {
            AgentProfileDocumentReceived?.Invoke(this, args);
        }
    }
}

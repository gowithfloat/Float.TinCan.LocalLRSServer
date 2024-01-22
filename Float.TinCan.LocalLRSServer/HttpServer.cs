using System;
using System.Net;
using System.Text;

namespace Float.TinCan.LocalLRSServer
{
    /// <summary>
    /// Receives requests and serves content locally.
    /// </summary>
    public abstract class HttpServer : IDisposable
    {
        /// <summary>
        /// The minimum user port per ICANN.
        /// </summary>
        const ushort MinimumPort = 1024;

        /// <summary>
        /// Internal HTTP listener object which we control to handle HTTP requests.
        /// </summary>
        readonly HttpListener listener;

        /// <summary>
        /// Whether or not we've disposed the HTTP listener.
        /// </summary>
        bool disposed;

        /// <summary>
        /// Flag indicating whether or not Start() has been called on this instance.
        /// </summary>
        bool started;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServer"/> class.
        /// Create a new local HTTP server to listen for requests.
        /// Handle these requests in your implementing subclasses.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="port">The port to listen to requests on.</param>
        /// <param name="suffix">HTTP listeners may have a sub-path.</param>
        protected HttpServer(string address = "http://127.0.0.1", ushort port = 8080, string suffix = "/")
        {
            if (!HttpListener.IsSupported)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new Exception("HttpListener not supported on this platform.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            if (string.IsNullOrWhiteSpace(address))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Address parameter is required.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            if (port < MinimumPort)
            {
                throw new ArgumentOutOfRangeException($"Ports below {MinimumPort} are reserved.");
            }

            if (string.IsNullOrWhiteSpace(suffix))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Suffix parameter must be a valid string");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            if (!suffix.EndsWith("/", StringComparison.Ordinal))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Server suffix must end with '/'.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            listener = new HttpListener();

            Prefix = $"{address}:{port}{suffix}";

            // we can listen to multiple prefixes, but only need the one
            listener.Prefixes.Add(Prefix);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="HttpServer"/> is listening for connections.
        /// </summary>
        /// <value><c>true</c> if is listening; otherwise, <c>false</c>.</value>
        public bool IsListening => listener.IsListening;

        /// <summary>
        /// Gets a URI of the local LRS server.
        /// </summary>
        /// <value>URI of the local LRS server.</value>
        public Uri Url => new Uri(Prefix);

        /// <summary>
        /// Gets the active prefix.
        /// </summary>
        /// <value>The active prefix.</value>
        protected string Prefix { get; }

        /// <summary>
        /// Begin listening for requests.
        /// </summary>
        public void Start()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("listener");
            }

            if (started)
            {
                return;
            }

            listener.Start();

            // set up the initial context
            StartContext();

            started = true;
        }

        /// <summary>
        /// Stop listening for requests.
        /// Use <c>Start</c> to start listening for requests again.
        /// </summary>
        public void Stop()
        {
            if (disposed)
            {
                return;
            }

            started = false;

            if (listener != null && listener.IsListening)
            {
                listener.Stop();
            }
        }

        /// <summary>
        /// Permanantly closes the connection.
        /// </summary>
        public void Close()
        {
            if (disposed)
            {
                return;
            }

            Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Convenience function to write string content to a response output stream.
        /// Also updates the content length on the response.
        /// Optionally, you can specify the type of content you're providing, and the response status code.
        /// </summary>
        /// <param name="response">The response to write to.</param>
        /// <param name="content">The content to write.</param>
        /// <param name="contentType">Optional. The type of content that will be written.</param>
        /// <param name="statusCode">Optional. The status code to associate with the response.</param>
        protected static void WriteToStream(HttpListenerResponse response, string content, ContentType contentType = null, HttpStatusCode? statusCode = null)
        {
            WriteToStream(response, Encoding.UTF8.GetBytes(content), contentType, statusCode);
        }

        /// <summary>
        /// Slightly less convenient function to write bytes to a response output stream.
        /// </summary>
        /// <param name="response">The response to write to.</param>
        /// <param name="contentBytes">The content bytes to write.</param>
        /// <param name="contentType">Optional. The type of content that will be written.</param>
        /// <param name="statusCode">Optional. The status code to associate with the response.</param>
        protected static void WriteToStream(HttpListenerResponse response, byte[] contentBytes, ContentType contentType = null, HttpStatusCode? statusCode = null)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (contentBytes == null && statusCode != HttpStatusCode.NoContent)
            {
                throw new ArgumentNullException(nameof(contentBytes));
            }

            if (contentType != null)
            {
                response.ContentType = contentType.ToString();
            }

            if (statusCode != null)
            {
                response.StatusCode = (int)statusCode;
            }

            if (contentBytes != null)
            {
                var contentLength = contentBytes.Length;
                response.ContentLength64 = contentLength;
                response.OutputStream.Write(contentBytes, 0, contentLength);
            }

            response.OutputStream.Close();
        }

        /// <summary>
        /// Sends the response with an optional status code, then closes the output stream.
        /// </summary>
        /// <param name="response">The HTTPResponse to send.</param>
        /// <param name="statusCode">Optional. The status code to associate with the response.</param>
        protected static void SendResponse(HttpListenerResponse response, HttpStatusCode? statusCode = null)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (statusCode != null)
            {
                response.StatusCode = (int)statusCode;
            }

            response.OutputStream.Close();
        }

        /// <summary>
        /// Override this method to handle HTTP requests in your class.
        /// Response data should be written to the given response object.
        /// NOTE: You must close the response stream.
        /// </summary>
        /// <param name="request">The HTTP request from the client.</param>
        /// <param name="response">An object to hold data to return to the client.</param>
        protected abstract void HandleRequest(HttpListenerRequest request, HttpListenerResponse response);

        /// <summary>
        /// Release all managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">If we called disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                listener.Abort();
                listener.Close();
            }

            disposed = true;
        }

        /// <summary>
        /// Internal method to handle request events.
        /// </summary>
        /// <param name="result">The async result object.</param>
        void OnRequest(IAsyncResult result)
        {
            // When we close the listener, OnRequest gets called one last time.
            // However, this gets called after the listener is disposed, which can
            // cause an exception. So, if we've closed the listener, bail.
            if (disposed)
            {
                return;
            }

            var context = listener.EndGetContext(result);

            // set up a context for the next request
            StartContext();

            HandleRequest(context.Request, context.Response);
        }

        /// <summary>
        /// Internal method to start a new context that will be used for the next HTTP request.
        /// </summary>
        void StartContext()
        {
            listener.BeginGetContext(new AsyncCallback(OnRequest), listener);
        }

        /// <summary>
        /// Effectively an enum for response content type values.
        /// This is equivalent to a string-backed enum.
        /// </summary>
        protected sealed class ContentType
        {
            /// <summary>
            /// Response content will be HTML.
            /// </summary>
            public static readonly ContentType Html = new ContentType("text/html");

            /// <summary>
            /// Response content will be JSON.
            /// </summary>
            public static readonly ContentType Json = new ContentType("application/json");

            /// <summary>
            /// Internal storage for the raw value passed in the initializer.
            /// </summary>
            readonly string rawValue;

            /// <summary>
            /// Initializes a new instance of the <see cref="ContentType"/> class.
            /// </summary>
            /// <param name="rawValue">The raw value that will be returned by toString.</param>
            ContentType(string rawValue)
            {
                this.rawValue = rawValue;
            }

            /// <summary>
            /// Returns a <see cref="string"/> that represents the current <see cref="ContentType"/>.
            /// </summary>
            /// <returns>A <see cref="string"/> that represents the current <see cref="ContentType"/>.</returns>
            public override string ToString()
            {
                return rawValue;
            }
        }
    }
}

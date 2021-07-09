using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TusDotNetClient
{
    /// <summary>
    /// Represents a response from a request made to a Tus enabled server.
    /// </summary>
    public class TusHttpResponse
    {
        /// <summary>
        /// Get the HTTP status code from the Tus server.
        /// </summary>
        public HttpStatusCode StatusCode { get; }
        /// <summary>
        /// Get the HTTP headers from the response.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; }
        /// <summary>
        /// Get the content of the HTTP response as bytes.
        /// </summary>
        public byte[] ResponseBytes { get; }
        /// <summary>
        /// Get the content of the HTTP response as a <see cref="string"/>.
        /// </summary>
        public string ResponseString => Encoding.UTF8.GetString(ResponseBytes);

        /// <summary>
        /// Create an instance of a <see cref="TusHttpResponse"/>.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="headers">The HTTP headers of the response.</param>
        /// <param name="responseBytes">The content of the response.</param>
        public TusHttpResponse(
            HttpStatusCode statusCode,
            IDictionary<string, string> headers = null,
            byte[] responseBytes = null)
        {
            StatusCode = statusCode;
            Headers = headers is null
                ? new Dictionary<string, string>(0)
                : new Dictionary<string, string>(headers);
            ResponseBytes = responseBytes;
        }
    }
}
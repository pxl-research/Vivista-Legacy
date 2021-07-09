using System;
using System.Collections.Generic;
using System.Threading;

namespace TusDotNetClient
{
    /// <summary>
    /// HTTP methods supported by <see cref="TusDotNetClient"/>
    /// </summary>
    public enum RequestMethod
    {
        Get,
        Post,
        Head,
        Patch,
        Options,
        Delete
    }

    /// <summary>
    /// A class representing a request to be sent to a Tus enabled server.
    /// </summary>
    public class TusHttpRequest
    {
        private readonly Dictionary<string, string> _headers;
        
        /// <summary>
        /// Occurs when progress sending the request is made.
        /// </summary>
        public event ProgressDelegate UploadProgressed;

        /// <summary>
        /// Occurs when progress receiving the response is made.
        /// </summary>
        public event ProgressDelegate DownloadProgressed;

        /// <summary>
        /// Get the URL the request is being made against.
        /// </summary>
        public Uri Url { get; }
        /// <summary>
        /// Get the HTTP method of the request.
        /// </summary>
        public string Method { get; }
        /// <summary>
        /// Get a read-only collection of the headers of the request.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers => _headers;
        /// <summary>
        /// Get the raw bytes of the content of the request.
        /// </summary>
        public ArraySegment<byte> BodyBytes { get; }
        /// <summary>
        /// Get the cancellation token used to cancel the request.
        /// </summary>
        public CancellationToken CancelToken { get; }

        /// <summary>
        /// Create a new request to be made against a Tus enabled server.
        /// </summary>
        /// <param name="url">The URL to make the request against.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="additionalHeaders">A <see cref="Dictionary{TKey,TValue}"/> of user specified headers to add to the request.</param>
        /// <param name="bodyBytes">A byte array of the content of the request.</param>
        /// <param name="cancelToken">A <see cref="CancellationToken"/> to cancel the request with.</param>
        public TusHttpRequest(
            string url,
            RequestMethod method,
            IDictionary<string, string> additionalHeaders = null,
            ArraySegment<byte> bodyBytes = default,
            CancellationToken? cancelToken = null)
        {
            Url = new Uri(url);
            Method = method.ToString().ToUpperInvariant();
            BodyBytes = bodyBytes;
            CancelToken = cancelToken ?? CancellationToken.None;

            _headers = additionalHeaders is null
                ? new Dictionary<string, string>(1)
                : new Dictionary<string, string>(additionalHeaders); 
            _headers.Add(TusHeaderNames.TusResumable, "1.0.0");
        }

        /// <summary>
        /// Add an HTTP header to request.
        /// </summary>
        /// <param name="key">The name of the HTTP header.</param>
        /// <param name="value">The value of the HTTP header.</param>
        public void AddHeader(string key, string value) => _headers.Add(key, value);

        /// <summary>
        /// Invoke an <see cref="UploadProgressed"/> event.
        /// </summary>
        /// <param name="bytesTransferred">The number of bytes uploaded so far.</param>
        /// <param name="bytesTotal">The total number of bytes to be uploaded.</param>
        public void OnUploadProgressed(long bytesTransferred, long bytesTotal) =>
            UploadProgressed?.Invoke(bytesTransferred, bytesTotal);

        /// <summary>
        /// Invoke an <see cref="DownloadProgressed"/> event.
        /// </summary>
        /// <param name="bytesTransferred">The number of bytes downloaded so far.</param>
        /// <param name="bytesTotal">The total number of bytes to be downloaded.</param>
        public void OnDownloadProgressed(long bytesTransferred, long bytesTotal) =>
            DownloadProgressed?.Invoke(bytesTransferred, bytesTotal);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace TusDotNetClient
{
    /// <summary>
    /// Represents an exception that might occur when using <see cref="TusClient"/>.
    /// </summary>
    public class TusException : WebException
    {
        /// <summary>
        /// Get the content, if any, of the failed operation.
        /// </summary>
        public string ResponseContent { get; }

        /// <summary>
        /// Get the HTTP status code, if any, of the failed operation.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Get the description of the HTTP status code.
        /// </summary>
        public string StatusDescription { get; }

        /// <summary>
        /// Get the original <see cref="WebException"/> that occured.
        /// </summary>
        public WebException OriginalException { get; }

        /// <summary>
        /// Create a new instance of <see cref="TusException"/> based on an <see cref="OperationCanceledException"/>.
        /// </summary>
        /// <param name="ex">An <see cref="OperationCanceledException"/> to base the <see cref="TusException"/> on.</param>
        public TusException(OperationCanceledException ex)
            : base(ex.Message, ex, WebExceptionStatus.RequestCanceled, null)
        {
            OriginalException = null;
        }

        /// <summary>
        /// Create a new instance of <see cref="TusException"/> based on another <see cref="TusException"/>.
        /// </summary>
        /// <param name="ex">The <see cref="TusException"/> to base the new <see cref="TusException"/> on.</param>
        /// <param name="message">Text to prefix the <see cref="Exception.Message"/> with.</param>
        public TusException(TusException ex, string message)
            : base($"{message}{ex.Message}", ex, ex.Status, ex.Response)
        {
            OriginalException = ex;

            StatusCode = ex.StatusCode;
            StatusDescription = ex.StatusDescription;
            ResponseContent = ex.ResponseContent;
        }

        /// <summary>
        /// Create a new instance of <see cref="TusException"/> based on a <see cref="WebException"/>.
        /// </summary>
        /// <param name="ex">The <see cref="WebException"/> to base the new <see cref="TusException"/> on.</param>
        /// <param name="message">Text to prefix the <see cref="Exception.Message"/> with.</param>
        public TusException(WebException ex, string message = "")
            : base($"{message}{ex.Message}", ex, ex.Status, ex.Response)
        {
            OriginalException = ex;

            if (ex.Response is HttpWebResponse webResponse &&
                webResponse.GetResponseStream() is Stream responseStream)
            {
                using (var reader = new StreamReader(responseStream))
                {
                    StatusCode = webResponse.StatusCode;
                    StatusDescription = webResponse.StatusDescription;
                    ResponseContent = reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Get a <see cref="string"/> containing all relevant information about the <see cref="TusException"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> containing information about the exception.</returns>
        public override string ToString()
        {
            var bits = new List<string>
            {
                Message
            };

            if (Response is WebResponse response)
            {
                bits.Add($"URL:{response.ResponseUri}");
            }

            if (StatusCode != HttpStatusCode.OK)
            {
                bits.Add($"{StatusCode}:{StatusDescription}");
            }

            if (!string.IsNullOrEmpty(ResponseContent))
            {
                bits.Add(ResponseContent);
            }

            return string.Join(Environment.NewLine, bits);
        }
    }
}
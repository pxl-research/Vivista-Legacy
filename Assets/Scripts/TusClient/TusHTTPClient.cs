using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TusDotNetClient
{
	/// <summary>
	/// A class to execute requests against a Tus enabled server.
	/// </summary>
	public class TusHttpClient
	{
		/// <summary>
		/// Perform a request to the Tus server.
		/// </summary>
		/// <param name="request">The <see cref="TusHttpRequest"/> to execute.</param>
		/// <returns>A <see cref="TusHttpResponse"/> with the response data.</returns>
		/// <exception cref="TusException">Throws when the request fails.</exception>
		public static async Task<TusHttpResponse> PerformRequestAsync(TusHttpRequest request, IWebProxy proxy = null)
		{
			var segment = request.BodyBytes;

			try
			{
				var webRequest = WebRequest.CreateHttp(request.Url);
				webRequest.AutomaticDecompression = DecompressionMethods.GZip;

				webRequest.Timeout = Timeout.Infinite;
				webRequest.ReadWriteTimeout = Timeout.Infinite;
				webRequest.Method = request.Method;
				webRequest.KeepAlive = false;

				webRequest.Proxy = proxy;

				try
				{
					webRequest.ServicePoint.Expect100Continue = false;
				}
				catch (PlatformNotSupportedException)
				{
					//expected on .net core 2.0 with systemproxy
					//fixed by https://github.com/dotnet/corefx/commit/a9e01da6f1b3a0dfbc36d17823a2264e2ec47050
					//should work in .net core 2.2
				}

				//SEND
				var buffer = new byte[4096];

				var totalBytesWritten = 0L;

				webRequest.AllowWriteStreamBuffering = false;
				webRequest.ContentLength = segment.Count;

				foreach (var header in request.Headers)
				{
					switch (header.Key)
					{
						case TusHeaderNames.ContentLength:
							webRequest.ContentLength = long.Parse(header.Value);
							break;
						case TusHeaderNames.ContentType:
							webRequest.ContentType = header.Value;
							break;
						default:
							webRequest.Headers.Add(header.Key, header.Value);
							break;
					}
				}

				if (request.BodyBytes.Count > 0)
				{
					var inputStream = new MemoryStream(request.BodyBytes.Array, request.BodyBytes.Offset,
						request.BodyBytes.Count);

					using (var requestStream = webRequest.GetRequestStream())
					{
						inputStream.Seek(0, SeekOrigin.Begin);

						var bytesWritten = await inputStream.ReadAsync(buffer, 0, buffer.Length, request.CancelToken).ConfigureAwait(false);

						request.OnUploadProgressed(0, segment.Count);

						while (bytesWritten > 0)
						{
							totalBytesWritten += bytesWritten;

							request.OnUploadProgressed(totalBytesWritten, segment.Count);

							await requestStream.WriteAsync(buffer, 0, bytesWritten, request.CancelToken).ConfigureAwait(false);

							bytesWritten = await inputStream.ReadAsync(buffer, 0, buffer.Length, request.CancelToken).ConfigureAwait(false);
						}
					}
				}

				var response = (HttpWebResponse)await webRequest.GetResponseAsync().ConfigureAwait(false);

				//contentLength=0 for gzipped responses due to .net bug
				long contentLength = Math.Max(response.ContentLength, 0);

				buffer = new byte[16 * 1024];

				var outputStream = new MemoryStream();

				using (var responseStream = response.GetResponseStream())
				{
					if (responseStream != null)
					{
						var bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, request.CancelToken).ConfigureAwait(false);

						request.OnDownloadProgressed(0, contentLength);

						var totalBytesRead = 0L;
						while (bytesRead > 0)
						{
							totalBytesRead += bytesRead;

							request.OnDownloadProgressed(totalBytesRead, contentLength);

							await outputStream.WriteAsync(buffer, 0, bytesRead, request.CancelToken).ConfigureAwait(false);

							bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, request.CancelToken).ConfigureAwait(false);
						}
					}
				}

				return new TusHttpResponse(
					response.StatusCode,
					response.Headers.AllKeys.ToDictionary(headerName => headerName, headerName => response.Headers.Get(headerName)),
					outputStream.ToArray());
			}
			catch (OperationCanceledException cancelEx)
			{
				throw new TusException(cancelEx);
			}
			catch (WebException ex)
			{
				var response = (HttpWebResponse)ex.Response;

				var result = new TusHttpResponse(
					response.StatusCode,
					response.Headers.AllKeys.ToDictionary(headerName => headerName, headerName => response.Headers.Get(headerName)),
					Encoding.UTF8.GetBytes(response.StatusDescription));

				return result;
			}
		}
	}
}
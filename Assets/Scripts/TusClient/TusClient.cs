using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TusDotNetClient
{
	/// <summary>
	/// Represents the different hashing algorithm implementations supported by <see cref="TusClient"/>
	/// </summary>
	public enum HashingImplementation
	{
		Sha1Managed,
		SHA1CryptoServiceProvider,
	}

	/// <summary>
	/// A class to perform actions against a Tus enabled server.
	/// </summary>
	public class TusClient
	{
		/// <summary>
		/// Get or set the hashing algorithm implementation to be used for checksum calculation.
		/// </summary>
		public static HashingImplementation HashingImplementation { get; set; } = HashingImplementation.Sha1Managed;

		/// <summary>
		/// A mutable dictionary of headers which will be included with all requests.
		/// </summary>
		public Dictionary<string, string> AdditionalHeaders { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Create a file at the Tus server.
		/// </summary>
		/// <param name="url">URL to the creation endpoint of the Tus server.</param>
		/// <param name="fileInfo">The file which will be uploaded.</param>
		/// <param name="metadata">Metadata to be stored alongside the file.</param>
		/// <returns>The URL to the created file.</returns>
		/// <exception cref="Exception">Throws if the response doesn't contain the required information.</exception>
		public async Task<(int, string)> CreateAsync(string url, FileInfo fileInfo, params (string key, string value)[] metadata)
		{
			if (!metadata.Any(m => m.key == "filename"))
			{
				metadata = metadata.Concat(new[] { ("filename", fileInfo.Name) }).ToArray();
			}

			return await CreateAsync(url, fileInfo.Length, metadata);
		}

		/// <summary>
		/// Create a file at the Tus server.
		/// </summary>
		/// <param name="url">URL to the creation endpoint of the Tus server.</param>
		/// <param name="uploadLength">The byte size of the file which will be uploaded.</param>
		/// <param name="metadata">Metadata to be stored alongside the file.</param>
		/// <returns>The URL to the created file.</returns>
		/// <exception cref="Exception">Throws if the response doesn't contain the required information.</exception>
		public async Task<(int, string)> CreateAsync(string url, long uploadLength, params (string key, string value)[] metadata)
		{
			var requestUri = new Uri(url);

			var request = new TusHttpRequest(url, RequestMethod.Post, AdditionalHeaders);

			request.AddHeader(TusHeaderNames.UploadLength, uploadLength.ToString());
			request.AddHeader(TusHeaderNames.ContentLength, "0");

			if (metadata.Length > 0)
			{
				request.AddHeader(TusHeaderNames.UploadMetadata, string.Join(",", metadata
					.Select(md => $"{md.key.Replace(" ", "").Replace(",", "")} {Convert.ToBase64String(Encoding.UTF8.GetBytes(md.value))}")));
			}

			var response = await TusHttpClient.PerformRequestAsync(request).ConfigureAwait(false);

			if (response.StatusCode != HttpStatusCode.Created)
			{
				return ((int)response.StatusCode, response.ResponseString);
			}

			if (!response.Headers.ContainsKey("Location"))
			{
				throw new Exception("Location Header Missing");
			}

			if (!Uri.TryCreate(response.Headers["Location"], UriKind.RelativeOrAbsolute, out var locationUri))
			{
				throw new Exception("Invalid Location Header");
			}

			if (!locationUri.IsAbsoluteUri)
			{
				locationUri = new Uri(requestUri, locationUri);
			}

			return (200, locationUri.ToString());
		}

		/// <summary>
		/// Upload a file to the Tus server.
		/// </summary>
		/// <param name="url">URL to a previously created file.</param>
		/// <param name="file">The file to upload.</param>
		/// <param name="chunkSize">The size, in megabytes, of each file chunk when uploading.</param>
		/// <param name="cancellationToken">A cancellation token to cancel the operation with.</param>
		/// <returns>A <see cref="TusOperation{T}"/> which represents the upload operation.</returns>
		public TusOperation<List<TusHttpResponse>> UploadAsync(string url, FileInfo file, double chunkSize = 5.0, CancellationToken cancellationToken = default)
		{
			FileStream fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkSizeToMB(chunkSize), true);

			return UploadAsync(url, fileStream, chunkSize, cancellationToken);
		}

		/// <summary>
		/// Upload a file to the Tus server.
		/// </summary>
		/// <param name="url">URL to a previously created file.</param>
		/// <param name="fileStream">A file stream of the file to upload. The stream will be closed automatically.</param>
		/// <param name="chunkSize">The size, in megabytes, of each file chunk when uploading.</param>
		/// <param name="cancellationToken">A cancellation token to cancel the operation with.</param>
		/// <returns>A <see cref="TusOperation{T}"/> which represents the upload operation.</returns>
		public TusOperation<List<TusHttpResponse>> UploadAsync(string url, Stream fileStream, double chunkSize = 5.0, CancellationToken cancellationToken = default)
		{
			return new TusOperation<List<TusHttpResponse>>(
				async reportProgress =>
				{
					try
					{
						var offset = await GetFileOffset(url).ConfigureAwait(false);

						var sha = HashingImplementation == HashingImplementation.Sha1Managed
							? (SHA1)new SHA1Managed()
							: new SHA1CryptoServiceProvider();

						var uploadChunkSize = ChunkSizeToMB(chunkSize);

						if (offset == fileStream.Length)
						{
							reportProgress(fileStream.Length, fileStream.Length);
						}

						var buffer = new byte[uploadChunkSize];

						void OnProgress(long written, long total) => reportProgress(offset + written, fileStream.Length);

						List<TusHttpResponse> responses = new List<TusHttpResponse>();

						while (offset < fileStream.Length)
						{
							fileStream.Seek(offset, SeekOrigin.Begin);

							var bytesRead = await fileStream.ReadAsync(buffer, 0, uploadChunkSize);
							var segment = new ArraySegment<byte>(buffer, 0, bytesRead);
							var sha1Hash = sha.ComputeHash(buffer, 0, bytesRead);

							var request = new TusHttpRequest(url, RequestMethod.Patch, AdditionalHeaders, segment, cancellationToken);
							request.AddHeader(TusHeaderNames.UploadOffset, offset.ToString());
							request.AddHeader(TusHeaderNames.UploadChecksum, $"sha1 {Convert.ToBase64String(sha1Hash)}");
							request.AddHeader(TusHeaderNames.ContentType, "application/offset+octet-stream");

							try
							{
								request.UploadProgressed += OnProgress;
								var response = await TusHttpClient.PerformRequestAsync(request).ConfigureAwait(false);
								responses.Add(response);
								request.UploadProgressed -= OnProgress;

								if (response.StatusCode != HttpStatusCode.NoContent)
								{
									throw new Exception("WriteFileInServer failed. " + response.ResponseString);
								}

								offset = long.Parse(response.Headers[TusHeaderNames.UploadOffset]);

								//reportProgress(offset, fileStream.Length);
							}
							catch (IOException ex)
							{
								if (ex.InnerException is SocketException socketException)
								{
									if (socketException.SocketErrorCode == SocketError.ConnectionReset)
									{
										// retry by continuing the while loop
										// but get new offset from server to prevent Conflict error
										offset = await GetFileOffset(url).ConfigureAwait(false);
									}
									else
									{
										throw;
									}
								}
								else
								{
									throw;
								}
							}
						}

						return responses;
					}
					finally
					{
						fileStream.Dispose();
					}
				});
		}

		/// <summary>
		/// Download a file from the Tus server.
		/// </summary>
		/// <param name="url">The URL of a file at the Tus server.</param>
		/// <param name="cancellationToken">A cancellation token to cancel the operation with.</param>
		/// <returns>A <see cref="TusOperation{T}"/> which represents the download operation.</returns>
		public TusOperation<TusHttpResponse> DownloadAsync(string url, CancellationToken cancellationToken = default) =>
			new TusOperation<TusHttpResponse>(
				async reportProgress =>
				{
					var request = new TusHttpRequest(url, RequestMethod.Get, AdditionalHeaders, cancelToken: cancellationToken);

					request.DownloadProgressed += reportProgress;

					var response = await TusHttpClient.PerformRequestAsync(request).ConfigureAwait(false);

					request.DownloadProgressed -= reportProgress;

					return response;
				});

		/// <summary>
		/// Send a HEAD request to the Tus server.
		/// </summary>
		/// <param name="url">The endpoint to post the HEAD request to.</param>
		/// <returns>The response from the Tus server.</returns>
		public async Task<TusHttpResponse> HeadAsync(string url)
		{
			var request = new TusHttpRequest(url, RequestMethod.Head, AdditionalHeaders);

			try
			{
				return await TusHttpClient.PerformRequestAsync(request).ConfigureAwait(false);
			}
			catch (TusException ex)
			{
				return new TusHttpResponse(ex.StatusCode);
			}
		}

		/// <summary>
		/// Get information about the Tus server.
		/// </summary>
		/// <param name="url">The URL of the Tus enabled endpoint.</param>
		/// <returns>A <see cref="TusServerInfo"/> containing information about the Tus server.</returns>
		/// <exception cref="Exception">Throws if request fails.</exception>
		public async Task<TusServerInfo> GetServerInfo(string url)
		{
			var request = new TusHttpRequest(url, RequestMethod.Options, AdditionalHeaders);

			var response = await TusHttpClient.PerformRequestAsync(request).ConfigureAwait(false);

			// Spec says NoContent but tusd gives OK because of browser bugs
			if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
			{
				throw new Exception("getServerInfo failed. " + response.ResponseString);
			}

			response.Headers.TryGetValue(TusHeaderNames.TusResumable, out var version);
			response.Headers.TryGetValue(TusHeaderNames.TusVersion, out var supportedVersions);
			response.Headers.TryGetValue(TusHeaderNames.TusExtension, out var extensions);
			response.Headers.TryGetValue(TusHeaderNames.TusMaxSize, out var maxSizeString);
			response.Headers.TryGetValue(TusHeaderNames.TusChecksumAlgorithm, out var checksumAlgorithms);
			long.TryParse(maxSizeString, out var maxSize);
			return new TusServerInfo(version, supportedVersions, extensions, maxSize, checksumAlgorithms);
		}

		/// <summary>
		/// Delete a file from the Tus server.
		/// </summary>
		/// <param name="url">The URL of the file at the Tus server.</param>
		/// <returns>A <see cref="bool"/> indicating whether the file is deleted.</returns>
		public async Task<bool> Delete(string url)
		{
			var request = new TusHttpRequest(url, RequestMethod.Delete, AdditionalHeaders);

			var response = await TusHttpClient.PerformRequestAsync(request).ConfigureAwait(false);

			return response.StatusCode == HttpStatusCode.NoContent ||
				   response.StatusCode == HttpStatusCode.NotFound ||
				   response.StatusCode == HttpStatusCode.Gone;
		}

		private async Task<long> GetFileOffset(string url)
		{
			var request = new TusHttpRequest(url, RequestMethod.Head, AdditionalHeaders);

			var response = await TusHttpClient.PerformRequestAsync(request).ConfigureAwait(false);

			if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
			{
				throw new Exception("GetFileOffset failed. " + response.ResponseString);
			}

			if (!response.Headers.ContainsKey(TusHeaderNames.UploadOffset))
			{
				throw new Exception("Offset Header Missing");
			}

			return long.Parse(response.Headers[TusHeaderNames.UploadOffset]);
		}

		private static int ChunkSizeToMB(double chunkSize)
		{
			return (int)Math.Ceiling(chunkSize * 1024.0 * 1024.0);
		}
	}
}
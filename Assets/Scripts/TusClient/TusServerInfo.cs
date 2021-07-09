using System.Linq;

namespace TusDotNetClient
{
    /// <summary>
    /// Represents information about a Tus enabled server.
    /// </summary>
    public class TusServerInfo
    {
        /// <summary>
        /// Get the version of the Tus protocol used by the Tus server.
        /// </summary>
        public string Version { get; }
        /// <summary>
        /// Get a list of versions of the protocol the Tus server supports.
        /// </summary>
        public string[] SupportedVersions { get; }
        /// <summary>
        /// Get the protocol extensions supported by the Tus server.
        /// </summary>
        public string[] Extensions { get; }
        /// <summary>
        /// Get the maximum total size of a single file supported by the Tus server. 
        /// </summary>
        public long MaxSize { get; }
        /// <summary>
        /// Get the checksum algorithms supported by the Tus server.
        /// </summary>
        public string[] SupportedChecksumAlgorithms { get; }

        /// <summary>
        /// Get whether the <c>termination</c> protocol extension is supported by the Tus server.
        /// </summary>
        public bool SupportsDelete => Extensions.Contains("termination");

        /// <summary>
        /// Create a new instance of <see cref="TusServerInfo"/>.
        /// </summary>
        /// <param name="version">The protocol version used by the Tus server.</param>
        /// <param name="supportedVersions">The versions of the protocol supported by the Tus server separated by commas.</param>
        /// <param name="extensions">The protocol extensions supported by the Tus server separated by commas.</param>
        /// <param name="maxSize">The maximum total size of a single file supported by the Tus server.</param>
        /// <param name="checksumAlgorithms">The checksum algorithms supported by the Tus server separated by the Tus server.</param>
        public TusServerInfo(
            string version,
            string supportedVersions,
            string extensions,
            long? maxSize,
            string checksumAlgorithms)
        {
            Version = version ?? "";
            SupportedVersions = (supportedVersions ?? "").Trim().Split(',').ToArray();
            Extensions = (extensions ?? "").Trim().Split(',').ToArray();
            MaxSize = maxSize ?? 0;
            SupportedChecksumAlgorithms = (checksumAlgorithms ?? "").Trim().Split(',').ToArray();
        }
    }
}
namespace TusDotNetClient
{
    /// <summary>
    /// A collection of the header names used by the Tus protocol.
    /// </summary>
    public static class TusHeaderNames
    {
        public const string TusResumable = "Tus-Resumable";
        public const string TusVersion = "Tus-Version";
        public const string TusExtension = "Tus-Extension";
        public const string TusMaxSize = "Tus-Max-Size";
        public const string TusChecksumAlgorithm = "Tus-Checksum-Algorithm";
        public const string UploadLength = "Upload-Length";
        public const string UploadOffset = "Upload-Offset";
        public const string UploadMetadata = "Upload-Metadata";
        public const string UploadChecksum = "Upload-Checksum";
        public const string ContentLength = "Content-Length";
        public const string ContentType = "Content-Type";
    }
}
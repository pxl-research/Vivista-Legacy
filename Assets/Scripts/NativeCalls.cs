using System;
using System.Runtime.InteropServices;
using System.Text;

public static class NativeCalls
{
	public static string GetDriveName(string path)
	{
#if UNITY_STANDALONE_WIN
		var buffer = new StringBuilder(261);
		if (GetVolumeInformation(path, buffer, buffer.Capacity, out uint volSer, out uint macCompLen, out var flags, null, 0))
		{
			return buffer.Length == 0 ? path : buffer.ToString();
		}
		else
		{
			return path;
		}
#else 
		return "";
#endif
	}

	[DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetVolumeInformation(
		  string rootPathName,
		  StringBuilder volumeNameBuffer,
		  int volumeNameSize,
		  out uint volumeSerialNumber,
		  out uint maximumComponentLength,
		  out FileSystemFeature fileSystemFlags,
		  StringBuilder fileSystemNameBuffer,
		  int nFileSystemNameSize);

	[Flags]
	private enum FileSystemFeature : uint
	{
		CasePreservedNames = 2,
		CaseSensitiveSearch = 1,
		DaxVolume = 0x20000000,
		FileCompression = 0x10,
		NamedStreams = 0x40000,
		PersistentACLS = 8,
		ReadOnlyVolume = 0x80000,
		SequentialWriteOnce = 0x100000,
		SupportsEncryption = 0x20000,
		SupportsExtendedAttributes = 0x00800000,
		SupportsHardLinks = 0x00400000,
		SupportsObjectIDs = 0x10000,
		SupportsOpenByFileId = 0x01000000,
		SupportsReparsePoints = 0x80,
		SupportsSparseFiles = 0x40,
		SupportsTransactions = 0x200000,
		SupportsUsnJournal = 0x02000000,
		UnicodeOnDisk = 4,
		VolumeIsCompressed = 0x8000,
		VolumeQuotas = 0x20
	}
}

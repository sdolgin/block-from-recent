using System.Text;
using BlockFromRecent.Core;

namespace BlockFromRecent.Tests;

public class ShortcutResolverTests
{
    // --- Valid local path .lnk ---

    [Fact]
    public void ResolveTargetFromBytes_ValidLocalPath_ReturnsPath()
    {
        string expected = @"C:\Users\test\document.txt";
        byte[] data = BuildLocalPathLnk(expected);

        string? result = ShortcutResolver.ResolveTargetFromBytes(data);

        Assert.Equal(expected, result);
    }

    // --- Valid network path .lnk ---

    [Fact]
    public void ResolveTargetFromBytes_ValidNetworkPath_ReturnsPath()
    {
        byte[] data = BuildNetworkPathLnk(@"\\server\share", "docs");

        string? result = ShortcutResolver.ResolveTargetFromBytes(data);

        // Path.Combine joins with backslash
        Assert.Equal(@"\\server\share\docs", result);
    }

    [Fact]
    public void ResolveTargetFromBytes_NetworkPathNoSuffix_ReturnsShareOnly()
    {
        byte[] data = BuildNetworkPathLnk(@"\\server\share", suffix: null);

        string? result = ShortcutResolver.ResolveTargetFromBytes(data);

        Assert.Equal(@"\\server\share", result);
    }

    // --- Truncated / corrupt data ---

    [Fact]
    public void ResolveTargetFromBytes_WrongHeaderSize_ReturnsNull()
    {
        byte[] data = new byte[0x4C];
        // Write an incorrect header size (0xFF instead of 0x4C)
        BitConverter.GetBytes((uint)0xFF).CopyTo(data, 0);

        Assert.Null(ShortcutResolver.ResolveTargetFromBytes(data));
    }

    [Fact]
    public void ResolveTargetFromBytes_TruncatedLinkInfo_ReturnsNull()
    {
        // Valid header but no room for LinkInfo
        byte[] data = new byte[0x4C];
        BitConverter.GetBytes((uint)0x4C).CopyTo(data, 0);
        // Set HAS_LINK_INFO flag
        BitConverter.GetBytes((uint)0x02).CopyTo(data, 0x14);
        // Data ends right at header — ParseLinkInfo will fail bounds check

        Assert.Null(ShortcutResolver.ResolveTargetFromBytes(data));
    }

    // --- Data smaller than header size ---

    [Fact]
    public void ResolveTargetFromBytes_TooSmall_ReturnsNull()
    {
        byte[] data = new byte[10];

        Assert.Null(ShortcutResolver.ResolveTargetFromBytes(data));
    }

    [Fact]
    public void ResolveTargetFromBytes_EmptyArray_ReturnsNull()
    {
        Assert.Null(ShortcutResolver.ResolveTargetFromBytes(Array.Empty<byte>()));
    }

    // --- No HAS_LINK_INFO flag ---

    [Fact]
    public void ResolveTargetFromBytes_NoLinkInfoFlag_ReturnsNull()
    {
        byte[] data = new byte[0x4C];
        BitConverter.GetBytes((uint)0x4C).CopyTo(data, 0);
        // linkFlags = 0 (no HAS_LINK_INFO)
        BitConverter.GetBytes((uint)0x00).CopyTo(data, 0x14);

        Assert.Null(ShortcutResolver.ResolveTargetFromBytes(data));
    }

    // --- Helpers to construct .lnk binary data ---

    /// <summary>
    /// Builds minimal .lnk binary data with a local ANSI path via the LinkInfo structure.
    /// Uses LinkInfoHeaderSize = 0x1C (no Unicode fields) so the ANSI fallback path is used.
    /// </summary>
    private static byte[] BuildLocalPathLnk(string localPath)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // --- Shell Link Header (76 bytes) ---
        bw.Write((uint)0x4C);           // HeaderSize
        bw.Write(new byte[16]);          // LinkCLSID (unused for parsing)
        bw.Write((uint)0x02);           // LinkFlags = HAS_LINK_INFO
        bw.Write(new byte[76 - 4 - 16 - 4]); // Pad rest of header to 76 bytes

        // --- LinkInfo ---
        int linkInfoStart = (int)ms.Position;

        // LinkInfo Header (0x1C = 28 bytes, basic without Unicode offsets)
        const uint linkInfoHeaderSize = 0x1C;
        const uint volumeIdOffset = linkInfoHeaderSize; // VolumeID right after header

        // VolumeID: minimal (16 bytes + 1 null for volume label = 17 bytes)
        const uint volumeIdSize = 0x11;
        uint localBasePathOffset = volumeIdOffset + volumeIdSize;

        byte[] pathBytes = Encoding.Default.GetBytes(localPath);
        uint commonPathSuffixOffset = localBasePathOffset + (uint)pathBytes.Length + 1; // +1 for null

        uint linkInfoSize = commonPathSuffixOffset + 1; // +1 for null suffix byte

        bw.Write(linkInfoSize);              // [0x00] LinkInfoSize
        bw.Write(linkInfoHeaderSize);        // [0x04] LinkInfoHeaderSize
        bw.Write((uint)0x01);               // [0x08] LinkInfoFlags = VolumeIDAndLocalBasePath
        bw.Write(volumeIdOffset);            // [0x0C] VolumeIDOffset
        bw.Write(localBasePathOffset);       // [0x10] LocalBasePathOffset
        bw.Write((uint)0);                  // [0x14] CommonNetworkRelativeLinkOffset
        bw.Write(commonPathSuffixOffset);    // [0x18] CommonPathSuffixOffset

        // VolumeID (17 bytes)
        bw.Write((uint)volumeIdSize);        // VolumeIDSize
        bw.Write((uint)3);                  // DriveType = DRIVE_FIXED
        bw.Write((uint)0);                  // DriveSerialNumber
        bw.Write((uint)0x10);               // VolumeLabelOffset (offset 16 within VolumeID)
        bw.Write((byte)0);                  // Volume label (empty, null-terminated)

        // Local base path (ANSI, null-terminated)
        bw.Write(pathBytes);
        bw.Write((byte)0);

        // Common path suffix (empty, null-terminated)
        bw.Write((byte)0);

        return ms.ToArray();
    }

    /// <summary>
    /// Builds minimal .lnk binary data with a network path via the LinkInfo structure.
    /// </summary>
    private static byte[] BuildNetworkPathLnk(string netName, string? suffix = null)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // --- Shell Link Header (76 bytes) ---
        bw.Write((uint)0x4C);           // HeaderSize
        bw.Write(new byte[16]);          // LinkCLSID
        bw.Write((uint)0x02);           // LinkFlags = HAS_LINK_INFO
        bw.Write(new byte[76 - 4 - 16 - 4]); // Pad rest of header

        // --- LinkInfo ---
        const uint linkInfoHeaderSize = 0x1C;

        // We'll build the CommonNetworkRelativeLink block first to know its size
        byte[] netNameBytes = Encoding.Default.GetBytes(netName);
        const uint cnrlHeaderSize = 0x14; // 5 uint32 fields
        uint cnrlSize = cnrlHeaderSize + (uint)netNameBytes.Length + 1; // +1 for null

        // Layout within LinkInfo:
        // [0x00-0x1B] Header (28 bytes)
        // [0x1C ...] CommonNetworkRelativeLink
        // [after CNRL] CommonPathSuffix
        uint commonNetworkOffset = linkInfoHeaderSize;
        uint commonPathSuffixOffset = commonNetworkOffset + cnrlSize;

        byte[] suffixBytes = suffix != null ? Encoding.Default.GetBytes(suffix) : Array.Empty<byte>();
        uint linkInfoSize = commonPathSuffixOffset + (uint)suffixBytes.Length + 1; // +1 null

        bw.Write(linkInfoSize);              // [0x00] LinkInfoSize
        bw.Write(linkInfoHeaderSize);        // [0x04] LinkInfoHeaderSize
        bw.Write((uint)0x02);               // [0x08] LinkInfoFlags = CommonNetworkRelativeLinkAndPathSuffix
        bw.Write((uint)0);                  // [0x0C] VolumeIDOffset (unused)
        bw.Write((uint)0);                  // [0x10] LocalBasePathOffset (unused)
        bw.Write(commonNetworkOffset);       // [0x14] CommonNetworkRelativeLinkOffset
        bw.Write(commonPathSuffixOffset);    // [0x18] CommonPathSuffixOffset

        // CommonNetworkRelativeLink
        bw.Write(cnrlSize);                  // [0x00] CommonNetworkRelativeLinkSize
        bw.Write((uint)0x01);               // [0x04] CommonNetworkRelativeLinkFlags
        bw.Write(cnrlHeaderSize);            // [0x08] NetNameOffset (name starts right after header)
        bw.Write((uint)0);                  // [0x0C] DeviceNameOffset
        bw.Write((uint)0);                  // [0x10] NetworkProviderType

        // Net name (ANSI, null-terminated)
        bw.Write(netNameBytes);
        bw.Write((byte)0);

        // Common path suffix (ANSI, null-terminated)
        if (suffixBytes.Length > 0)
            bw.Write(suffixBytes);
        bw.Write((byte)0);

        return ms.ToArray();
    }
}

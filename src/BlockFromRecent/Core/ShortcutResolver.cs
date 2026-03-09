using System.Text;

namespace BlockFromRecent.Core;

/// <summary>
/// Parses .lnk (Shell Link) files using the MS-SHLLINK binary format specification.
/// Pure managed .NET — no COM interop required.
/// Reference: https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-shllink
/// </summary>
public static class ShortcutResolver
{
    private const uint LNK_HEADER_SIZE = 0x0000004C;
    private const uint HAS_LINK_TARGET_ID_LIST = 0x00000001;
    private const uint HAS_LINK_INFO = 0x00000002;

    public static string? ResolveTarget(string lnkPath)
    {
        try
        {
            byte[] data = File.ReadAllBytes(lnkPath);
            if (data.Length < LNK_HEADER_SIZE)
                return null;

            // Validate header size
            uint headerSize = BitConverter.ToUInt32(data, 0);
            if (headerSize != LNK_HEADER_SIZE)
                return null;

            uint linkFlags = BitConverter.ToUInt32(data, 0x14);
            int offset = (int)LNK_HEADER_SIZE;

            // Skip LinkTargetIDList if present
            if ((linkFlags & HAS_LINK_TARGET_ID_LIST) != 0)
            {
                if (offset + 2 > data.Length) return null;
                ushort idListSize = BitConverter.ToUInt16(data, offset);
                offset += 2 + idListSize;
            }

            // Parse LinkInfo if present
            if ((linkFlags & HAS_LINK_INFO) != 0)
            {
                return ParseLinkInfo(data, offset);
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Warn($"ResolveTarget failed for {Path.GetFileName(lnkPath)}: {ex.Message}");
            return null;
        }
    }

    private static string? ParseLinkInfo(byte[] data, int offset)
    {
        if (offset + 12 > data.Length) return null;

        uint linkInfoSize = BitConverter.ToUInt32(data, offset);
        if (linkInfoSize < 12 || offset + linkInfoSize > data.Length) return null;

        uint linkInfoHeaderSize = BitConverter.ToUInt32(data, offset + 4);
        uint linkInfoFlags = BitConverter.ToUInt32(data, offset + 8);

        // VolumeIDAndLocalBasePath flag
        bool hasLocalBasePath = (linkInfoFlags & 0x00000001) != 0;
        // CommonNetworkRelativeLinkAndPathSuffix flag
        bool hasNetworkPath = (linkInfoFlags & 0x00000002) != 0;

        // Try Unicode local base path first (available if header >= 0x24)
        if (hasLocalBasePath && linkInfoHeaderSize >= 0x24)
        {
            if (offset + 0x1C + 4 <= data.Length)
            {
                uint localBasePathOffsetUnicode = BitConverter.ToUInt32(data, offset + 0x1C);
                if (localBasePathOffsetUnicode > 0 && offset + localBasePathOffsetUnicode < data.Length)
                {
                    string? unicodePath = ReadUnicodeStringZ(data, offset + (int)localBasePathOffsetUnicode);
                    if (!string.IsNullOrWhiteSpace(unicodePath))
                        return unicodePath;
                }
            }
        }

        // Fall back to ANSI local base path
        if (hasLocalBasePath)
        {
            if (offset + 0x10 + 4 <= data.Length)
            {
                uint localBasePathOffset = BitConverter.ToUInt32(data, offset + 0x10);
                if (localBasePathOffset > 0 && offset + localBasePathOffset < data.Length)
                {
                    string? ansiPath = ReadAnsiStringZ(data, offset + (int)localBasePathOffset);
                    if (!string.IsNullOrWhiteSpace(ansiPath))
                        return ansiPath;
                }
            }
        }

        // Try network relative link
        if (hasNetworkPath)
        {
            if (offset + 0x14 + 4 <= data.Length)
            {
                uint commonNetworkOffset = BitConverter.ToUInt32(data, offset + 0x14);
                if (commonNetworkOffset > 0 && offset + commonNetworkOffset < data.Length)
                {
                    string? netPath = ParseCommonNetworkRelativeLink(data, offset + (int)commonNetworkOffset);
                    if (!string.IsNullOrWhiteSpace(netPath))
                    {
                        // Append common path suffix if available
                        string? suffix = TryReadPathSuffix(data, offset, linkInfoHeaderSize, hasLocalBasePath);
                        return suffix != null ? Path.Combine(netPath, suffix) : netPath;
                    }
                }
            }
        }

        return null;
    }

    private static string? ParseCommonNetworkRelativeLink(byte[] data, int offset)
    {
        if (offset + 0x14 > data.Length) return null;

        uint netNameOffset = BitConverter.ToUInt32(data, offset + 0x08);
        if (netNameOffset > 0 && offset + netNameOffset < data.Length)
        {
            return ReadAnsiStringZ(data, offset + (int)netNameOffset);
        }

        return null;
    }

    private static string? TryReadPathSuffix(byte[] data, int offset, uint headerSize, bool hasLocalBasePath)
    {
        // CommonPathSuffix is at offset 0x18 in LinkInfo (ANSI)
        int suffixFieldOffset = hasLocalBasePath ? 0x18 : 0x14;
        if (offset + suffixFieldOffset + 4 > data.Length) return null;

        uint suffixOffset = BitConverter.ToUInt32(data, offset + suffixFieldOffset);
        if (suffixOffset > 0 && offset + suffixOffset < data.Length)
        {
            return ReadAnsiStringZ(data, offset + (int)suffixOffset);
        }

        return null;
    }

    private static string? ReadAnsiStringZ(byte[] data, int offset)
    {
        int end = offset;
        while (end < data.Length && data[end] != 0) end++;
        if (end == offset) return null;
        return Encoding.Default.GetString(data, offset, end - offset);
    }

    private static string? ReadUnicodeStringZ(byte[] data, int offset)
    {
        int end = offset;
        while (end + 1 < data.Length && (data[end] != 0 || data[end + 1] != 0)) end += 2;
        if (end == offset) return null;
        return Encoding.Unicode.GetString(data, offset, end - offset);
    }
}

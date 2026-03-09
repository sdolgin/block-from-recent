using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace BlockFromRecent.Core;

public static class ShortcutResolver
{
    /// <summary>
    /// Resolves a .lnk shortcut file to its target path.
    /// Returns null if the shortcut cannot be resolved.
    /// </summary>
    public static string? ResolveTarget(string lnkPath)
    {
        try
        {
            var link = (IShellLink)new ShellLink();
            var file = (IPersistFile)link;
            file.Load(lnkPath, 0);

            // Don't resolve the link — just read the stored path (faster, no UI)
            var sb = new StringBuilder(260);
            var data = new WIN32_FIND_DATAW();
            link.GetPath(sb, sb.Capacity, out data, SLGP_RAWPATH);

            string target = sb.ToString();
            return string.IsNullOrWhiteSpace(target) ? null : target;
        }
        catch
        {
            return null;
        }
    }

    private const int SLGP_RAWPATH = 0x4;

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLink
    {
        void GetPath(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder pszFile,
            int cch,
            out WIN32_FIND_DATAW pfd,
            int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder pszName,
            int cch);
        void SetDescription(
            [MarshalAs(UnmanagedType.LPStr)] string pszName);
        void GetWorkingDirectory(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder pszDir,
            int cch);
        void SetWorkingDirectory(
            [MarshalAs(UnmanagedType.LPStr)] string pszDir);
        void GetArguments(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder pszArgs,
            int cch);
        void SetArguments(
            [MarshalAs(UnmanagedType.LPStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation(
            [Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder pszIconPath,
            int cch,
            out int piIcon);
        void SetIconLocation(
            [MarshalAs(UnmanagedType.LPStr)] string pszIconPath,
            int iIcon);
        void SetRelativePath(
            [MarshalAs(UnmanagedType.LPStr)] string pszPathRel,
            int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath(
            [MarshalAs(UnmanagedType.LPStr)] string pszFile);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public FILETIME ftCreationTime;
        public FILETIME ftLastAccessTime;
        public FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }
}

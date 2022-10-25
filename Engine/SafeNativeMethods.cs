// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    [SuppressUnmanagedCodeSecurityAttribute]
    internal class SafeNativeMethods {
        //Code adapted from Stack Exchange network post https://stackoverflow.com/questions/26514954/registration-free-com-interop-deactivating-activation-context-in-finalizer-thro
        //Authored by https://stackoverflow.com/users/3742925/aurora
        //Answered by https://stackoverflow.com/users/505088/david-heffernan

        private const uint ACTCTX_FLAG_RESOURCE_NAME_VALID = 0x008;
        private const UInt16 ISOLATIONAWARE_MANIFEST_RESOURCE_ID = 2;
        [DllImport("Kernel32.dll")]
        private extern static IntPtr CreateActCtx(ref ACTCTX actctx);
        [DllImport("Kernel32.dll")]
        private extern static bool ActivateActCtx(IntPtr hActCtx, out IntPtr lpCookie);
        [DllImport("Kernel32.dll")]
        private extern static bool DeactivateActCtx(uint dwFlags, IntPtr lpCookie);
        [DllImport("Kernel32.dll")]
        private extern static bool ReleaseActCtx(IntPtr hActCtx);

        private struct ACTCTX {
            public int cbSize;
            public uint dwFlags;
            public string lpSource;
            public ushort wProcessorArchitecture;
            public ushort wLangId;
            public string lpAssemblyDirectory;
            public UInt16 lpResourceName;
            public string lpApplicationName;
            public IntPtr hModule;
        }

        [ThreadStatic]
        private static IntPtr m_cookie;
        [ThreadStatic]
        private static IntPtr m_hActCtx;
        internal static bool DestroyActivationContext() {
            if (m_cookie != IntPtr.Zero) {
                if (!DeactivateActCtx(0, m_cookie))
                    return false;
                m_cookie = IntPtr.Zero;
                if (!ReleaseActCtx(m_hActCtx))
                    return false;
                m_hActCtx = IntPtr.Zero;
            }

            return true;
        }

        internal static bool EstablishActivationContext() {
            ACTCTX info = new() {
                cbSize = Marshal.SizeOf(typeof(ACTCTX)),
                dwFlags = ACTCTX_FLAG_RESOURCE_NAME_VALID,
                lpSource = System.Reflection.Assembly.GetExecutingAssembly().Location,
                lpResourceName = ISOLATIONAWARE_MANIFEST_RESOURCE_ID
            };
            m_hActCtx = CreateActCtx(ref info);
            if (m_hActCtx == new IntPtr(-1)) return false;
            m_cookie = IntPtr.Zero;
            if (!ActivateActCtx(m_hActCtx, out m_cookie)) return false;
            return true;
        }

        [DllImport("dbghelp.dll", CharSet = CharSet.Unicode)]
        public static extern bool SymFindFileInPath(IntPtr hProcess,
            [MarshalAs(UnmanagedType.LPWStr)] string SearchPath,
            [MarshalAs(UnmanagedType.LPWStr)] string FileName,
            IntPtr id,
            Int32 two,
            Int32 three,
            Int32 flags,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder filePath,
            IntPtr callback,
            IntPtr context);
    }
}

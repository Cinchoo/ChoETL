using System;
using System.IO;
using System.Text;
using System.Security;
using System.ComponentModel;
using System.Security.Permissions;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

namespace ChoETL
{
#if !NETSTANDARD2_0
    [Serializable, SuppressUnmanagedCodeSecurity, HostProtection(SecurityAction.LinkDemand, SharedState = true)]
#else
    [Serializable, SuppressUnmanagedCodeSecurity]
#endif
    public class ChoWin32Exception : ExternalException, ISerializable
    {
        private const string Kernel32DllName = "kernel32.dll";

#if !NETSTANDARD2_0
        [DllImport(Kernel32DllName, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int FormatMessage(int dwFlags, HandleRef lpSource, int dwMessageId, int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr arguments);
#endif

        #region ChoIntSecurity Class

#if !NETSTANDARD2_0
        [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
        private static class ChoIntSecurity
        {
            // Fields
            public static readonly CodeAccessPermission FullReflection = new ReflectionPermission(PermissionState.Unrestricted);
            public static readonly CodeAccessPermission UnmanagedCode = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);

            // Methods
            public static string UnsafeGetFullPath(string fileName)
            {
                string fullPath = fileName;
                new FileIOPermission(PermissionState.None) { AllFiles = FileIOPermissionAccess.PathDiscovery }.Assert();
                try
                {
                    fullPath = Path.GetFullPath(fileName);
                }
                finally
                {
                    CodeAccessPermission.RevertAssert();
                }
                return fullPath;
            }
        }
#endif
        #endregion ChoIntSecurity Class

        // Fields
        private readonly int nativeErrorCode;

        // Methods
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public ChoWin32Exception()
            : this(Marshal.GetLastWin32Error())
        {
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public ChoWin32Exception(int error)
            : this(error, GetErrorMessage(error), true)
        {
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public ChoWin32Exception(string message)
            : this(Marshal.GetLastWin32Error(), GetErrorMessage(Marshal.GetLastWin32Error(), message), true)
        {
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public ChoWin32Exception(int error, string message)
            : base(GetErrorMessage(error, message))
        {
            this.nativeErrorCode = error;
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public ChoWin32Exception(string message, Exception innerException)
            : base(GetErrorMessage(Marshal.GetLastWin32Error(), message), innerException)
        {
            this.nativeErrorCode = Marshal.GetLastWin32Error();
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        private ChoWin32Exception(int error, string message, bool dummy)
            : base(message)
        {
            this.nativeErrorCode = error;
        }

        protected ChoWin32Exception(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
#if !NETSTANDARD2_0
            ChoIntSecurity.UnmanagedCode.Demand();
#endif
            this.nativeErrorCode = info.GetInt32("NativeErrorCode");
        }

        private static string GetErrorMessage(int error)
        {
            return GetErrorMessage(error, null);
        }

        private static string GetErrorMessage(int error, string customErrMsg)
        {
            string lastErrMsg = null;
            if (customErrMsg != null)
                customErrMsg = customErrMsg.Trim();

            try
            {
                StringBuilder lpBuffer = new StringBuilder(0x100);
#if !NETSTANDARD2_0
                if (FormatMessage(0x3200, Win32Common.NullHandleRef, error, 0, lpBuffer, lpBuffer.Capacity + 1, IntPtr.Zero) == 0)
#endif
                    lastErrMsg = "Unknown error (0x" + Convert.ToString(error, 0x10) + ")";

                int length = lpBuffer.Length;
                while (length > 0)
                {
                    char ch = lpBuffer[length - 1];
                    if ((ch > ' ') && (ch != '.'))
                    {
                        break;
                    }
                    length--;
                }
                string errMsg = lpBuffer.ToString(0, length);
                lastErrMsg = errMsg.IsNullOrWhiteSpace() ? lastErrMsg : errMsg;
            }
            finally
            {
                if (!String.IsNullOrEmpty(customErrMsg))
                {
                    if (customErrMsg.EndsWith("."))
                        lastErrMsg = String.Format("{0} {1}.", customErrMsg, lastErrMsg);
                    else
                        lastErrMsg = String.Format("{0}. {1}.", customErrMsg, lastErrMsg);
                }
            }

            return lastErrMsg;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("NativeErrorCode", this.nativeErrorCode);
            base.GetObjectData(info, context);
        }

        // Properties
        public int NativeErrorCode
        {
            get
            {
                return this.nativeErrorCode;
            }
        }
    }
    public static class Win32Common
    {
        public static readonly HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        public const int ANYSIZE_ARRAY = 1;
        public const uint TOKEN_QUERY = 0x0008;
        public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        public const int ERROR_ACCESS_DENIED = 5;

        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint FILE_SHARE_DELETE = 0x00000004;

        public const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
        public const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
        public const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        public const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
        public const uint FILE_ATTRIBUTE_DEVICE = 0x00000040;
        public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        public const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;
        public const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200;
        public const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
        public const uint FILE_ATTRIBUTE_COMPRESSED = 0x00000800;
        public const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
        public const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
        public const uint FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;

        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint GENERIC_EXECUTE = 0x20000000;
        public const uint GENERIC_ALL = 0x10000000;
    }
}

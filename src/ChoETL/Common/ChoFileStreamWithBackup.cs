namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;

    #endregion

    /// <summary>
    /// Exposes a Stream around a file, supporting both synchronous and asynchronous read and write operations
    /// as well as backup feature
    /// </summary>
    public sealed class ChoFileStreamWithBackup : FileStream
    {
        #region Instance Data Memebers (Private)

        long _maxFileSize;
        int _maxFileCount;
        string _fileDir;
        string _fileBase;
        string _fileExt;
        int _fileDecimals;
        bool _allowSplitMsg;
        int _nextFileIndex;
        bool _cyclic;
        bool _autoBackup;
        DateTime _lastBackupTime;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="maxFileSize"></param>
        /// <param name="maxFileCount"></param>
        /// <param name="mode"></param>
        /// <param name="cyclic"></param>
        /// <param name="autoBackup"></param>
        /// <param name="allowSplitMsg"></param>
        public ChoFileStreamWithBackup(string fileName, long maxFileSize, int maxFileCount, FileMode mode, bool cyclic, bool autoBackup, bool allowSplitMsg)
            : base(fileName, BaseFileMode(mode), FileAccess.Write)
        {
            Init(fileName, maxFileSize, maxFileCount, mode, cyclic, autoBackup, allowSplitMsg);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="maxFileSize"></param>
        /// <param name="maxFileCount"></param>
        /// <param name="mode"></param>
        /// <param name="share"></param>
        /// <param name="cyclic"></param>
        /// <param name="autoBackup"></param>
        /// <param name="allowSplitMsg"></param>
        public ChoFileStreamWithBackup(string fileName, long maxFileSize, int maxFileCount, FileMode mode, FileShare share, bool cyclic, bool autoBackup, bool allowSplitMsg)
            : base(fileName, BaseFileMode(mode), FileAccess.Write, share)
        {
            Init(fileName, maxFileSize, maxFileCount, mode, _cyclic, autoBackup, allowSplitMsg);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="maxFileSize"></param>
        /// <param name="maxFileCount"></param>
        /// <param name="mode"></param>
        /// <param name="share"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cyclic"></param>
        /// <param name="autoBackup"></param>
        /// <param name="allowSplitMsg"></param>
        public ChoFileStreamWithBackup(string fileName, long maxFileSize, int maxFileCount, FileMode mode, FileShare share, int bufferSize, bool cyclic, bool autoBackup, bool allowSplitMsg)
            : base(fileName, BaseFileMode(mode), FileAccess.Write, share, bufferSize)
        {
            Init(fileName, maxFileSize, maxFileCount, mode, _cyclic, autoBackup, allowSplitMsg);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="maxFileSize"></param>
        /// <param name="maxFileCount"></param>
        /// <param name="mode"></param>
        /// <param name="share"></param>
        /// <param name="bufferSize"></param>
        /// <param name="isAsync"></param>
        /// <param name="cyclic"></param>
        /// <param name="autoBackup"></param>
        /// <param name="allowSplitMsg"></param>
        public ChoFileStreamWithBackup(string fileName, long maxFileSize, int maxFileCount, FileMode mode, FileShare share, int bufferSize, bool isAsync, bool cyclic, bool autoBackup, bool allowSplitMsg)
            : base(fileName, BaseFileMode(mode), FileAccess.Write, share, bufferSize, isAsync)
        {
            Init(fileName, maxFileSize, maxFileCount, mode, _cyclic, autoBackup, allowSplitMsg);
        }

        #endregion

        #region FileStream Overrides Properties

        /// <summary>
        /// Disable the CanRead propeties
        /// </summary>
        public override bool CanRead { get { return false; } }

        #endregion

        #region Instance Properties (Private)

        /// <summary>
        /// Determine when to backup the log file
        /// </summary>
        bool BackupNow
        {
            get
            {
                if (!_autoBackup) return false;
                if (_lastBackupTime.CompareTo(DateTime.Today) < 0) return true;

                return false;
            }
        }

        #endregion

        #region FileStream Overrides functions (Public)

        /// <summary>
        /// Writes a block of bytes to this stream using data from a buffer.
        /// </summary>
        /// <param name="array">The array to which bytes are written.</param>
        /// <param name="offset">The byte offset in array at which to begin writing.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        public override void Write(byte[] array, int offset, int count)
        {
            if (!_cyclic)
                base.Write(array, offset, count);
            else
            {
                //Calculate the length of the message to be written to log file
                int actualCount = Math.Min(count, array.GetLength(0));

                //If the message can fit in to current log file
                if (Position + actualCount <= _maxFileSize)
                {
                    if (BackupNow)
                    {
                        BackupAndResetStream();
                        BackupAllFiles();
                    }

                    base.Write(array, offset, count);
                }
                else
                {
                    //If we can split message between log file
                    if (_allowSplitMsg)
                    {
                        int partialCount = (int)(Math.Max(_maxFileSize, Position) - Position);
                        base.Write(array, offset, partialCount);

                        offset += partialCount;
                        count = actualCount - partialCount;
                    }
                    else
                    {
                        if (count > _maxFileSize)
                            throw new ArgumentOutOfRangeException("Buffer size exceeds maximum file length");
                    }

                    BackupAndResetStream();
                    Write(array, offset, count);
                }
            }
        }

        #endregion

        #region Instance Member Functions (Private)

        /// <summary>
        /// Initialize the object memeber. Called by Constructors
        /// </summary>
        /// <param name="fileName">Log file name</param>
        /// <param name="maxFileSize">Max file size</param>
        /// <param name="maxFileCount">Max file count</param>
        /// <param name="mode">Log file mode</param>
        /// <param name="cyclic"></param>
        /// <param name="autoBackup">Auto backup flag</param>
        /// <param name="allowSplitMsg">Auto backup flag</param>
        void Init(string fileName, long maxFileSize, int maxFileCount, FileMode mode, bool cyclic, bool autoBackup, bool allowSplitMsg)
        {
            _lastBackupTime = DateTime.Today;

            if (maxFileSize <= 0)
                throw new ArgumentOutOfRangeException("Invalid maximum file length.");
            if (maxFileCount <= 0)
                throw new ArgumentOutOfRangeException("Invalid maximum file count.");

            _maxFileSize = maxFileSize;
            _maxFileCount = maxFileCount;
            _allowSplitMsg = allowSplitMsg;
            _autoBackup = autoBackup;
            _cyclic = cyclic;

            //check and set autobackup to true, if cyclic option is on
            if (cyclic) _autoBackup = true;

            string fullPath = ChoPath.GetFullPath(fileName);
            _fileDir = Path.GetDirectoryName(fullPath);
            _fileBase = Path.GetFileNameWithoutExtension(fullPath);
            _fileExt = Path.GetExtension(fullPath);

            //Calculate file decimals
            _fileDecimals = 1;
            int decimalBase = 10;

            while (decimalBase < _maxFileCount)
            {
                ++_fileDecimals;
                decimalBase *= 10;
            }

            switch (mode)
            {
                case FileMode.Create:
                case FileMode.CreateNew:
                case FileMode.Truncate:
                    // Delete old files
                    for (int fileIndex = 0; fileIndex < _maxFileCount; ++fileIndex)
                    {
                        string file = GetBackupFileName(fileIndex);
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                    break;

                default:
                    // Position file pointer to the last backup file
                    for (int fileIndex = 0; fileIndex < _maxFileCount; ++fileIndex)
                    {
                        if (File.Exists(GetBackupFileName(fileIndex)))
                            _nextFileIndex = fileIndex + 1;
                        else
                            break;
                    }

                    //if the file count reaches the max file count, back those up
                    if (_nextFileIndex == _maxFileCount)
                        BackupAllFiles();
                    else
                    {
                        Seek(0, SeekOrigin.End);

                        if (_autoBackup && Position > 0)
                        {
                            BackupAndResetStream();
                            BackupAllFiles();
                        }
                    }

                    Seek(0, SeekOrigin.End);
                    break;
            }
        }

        /// <summary>
        /// Backup the log file and reset the the stream
        /// </summary>
        void BackupAndResetStream()
        {
            Flush();
            File.Copy(Name, GetBackupFileName(_nextFileIndex), true);
            SetLength(0);

            ++_nextFileIndex;
            if (_nextFileIndex >= _maxFileCount)
                BackupAllFiles();
        }

        /// <summary>
        /// Create backup directory and move all log file to there
        /// </summary>
        void BackupAllFiles()
        {
            if (!_autoBackup) return;

            bool backupDirEmpty = true;
            string backupDirName = GetBackupDirName();
            string sourceFileName;

            for (int fileIndex = 0; fileIndex < _maxFileCount; ++fileIndex)
            {
                sourceFileName = GetBackupFileName(fileIndex);
                if (File.Exists(sourceFileName))
                {
                    string strPath = Path.Combine(backupDirName, Path.GetFileName(sourceFileName));// GetBackupFileNameOnly(fileIndex));
                    Directory.CreateDirectory(backupDirName);
                    if (File.Exists(strPath)) File.Delete(strPath);
                    File.Move(sourceFileName, strPath);
                    backupDirEmpty = false;
                }
                else
                    break;
            }

            if (backupDirEmpty) Directory.Delete(backupDirName);

            _nextFileIndex = 0;
            _lastBackupTime = DateTime.Today;
        }

        /// <summary>
        /// Returns available backup directory name
        /// </summary>
        /// <returns></returns>
        string GetBackupDirName()
        {
            string baseBackupDir = Path.Combine(_fileDir, _lastBackupTime.ToString("MM-dd-yyyy"));
            string backupDir = baseBackupDir;
            int index = 0;

            while (Directory.Exists(backupDir))
            {
                index++;
                backupDir = String.Format("{0}.{1}", baseBackupDir, index.ToString("D2"));
            }

            Directory.CreateDirectory(backupDir);
            return backupDir;
        }

        /// <summary>
        /// Generate backup log file name
        /// </summary>
        /// <param name="index">Index used to create unique log file name</param>
        /// <returns></returns>
        string GetBackupFileName(int index)
        {
            StringBuilder format = new StringBuilder();
            format.AppendFormat("D{0}", _fileDecimals);

            if (_fileExt.Length > 0)
                return Path.Combine(_fileDir,
                    String.Format("{0}.{1}{2}", _fileBase, index.ToString(format.ToString()), _fileExt));
            else
                return Path.Combine(_fileDir,
                    String.Format("{0}.{1}", _fileBase, index.ToString(format.ToString())));
        }

        /// <summary>
        /// Generate backup log file name
        /// </summary>
        /// <param name="index">Index used to create unique log file name</param>
        /// <returns></returns>
        string GetBackupFileNameOnly(int index)
        {
            StringBuilder format = new StringBuilder();
            format.AppendFormat("D{0}", _fileDecimals);

            if (_fileExt.Length > 0)
                return String.Format("{0}.{1}{2}", _fileBase, index.ToString(format.ToString()), _fileExt);
            else
                return String.Format("{0}.{1}", _fileBase, index.ToString(format.ToString()));
        }

        #endregion

        #region Shared Member functions (Private)

        static FileMode BaseFileMode(FileMode mode)
        {
            return mode == FileMode.Append ? FileMode.OpenOrCreate : mode;
        }

        #endregion

        #region Instance Properties (Public)

        public bool AutoBackup
        {
            get { return _autoBackup; }
            set { _autoBackup = value; }
        }

        public bool Cyclic
        {
            get { return _cyclic; }
            set
            {
                _cyclic = value;
                if (_cyclic) _autoBackup = true;
            }
        }

        #endregion Instance Properties (Public)
    }
}

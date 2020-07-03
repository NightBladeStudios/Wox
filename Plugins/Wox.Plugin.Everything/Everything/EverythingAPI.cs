namespace Wox.Plugin.Everything.Everything
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using Exceptions;

    public sealed class EverythingApi
    {
        public enum RequestFlag
        {
            FileName = 0x00000001,
            Path = 0x00000002,
            FullPathAndFileName = 0x00000004,
            Extension = 0x00000008,
            Size = 0x00000010,
            DateCreated = 0x00000020,
            DateModified = 0x00000040,
            DateAccessed = 0x00000080,
            Attributes = 0x00000100,
            FileListFileName = 0x00000200,
            RunCount = 0x00000400,
            DateRun = 0x00000800,
            DateRecentlyChanged = 0x00001000,
            HighlightedFileName = 0x00002000,
            HighlightedPath = 0x00004000,
            HighlightedFullPathAndFileName = 0x00008000
        }

        public enum StateCode
        {
            OK,
            MemoryError,
            IPCError,
            RegisterClassExError,
            CreateWindowError,
            CreateThreadError,
            InvalidIndexError,
            InvalidCallError
        }

        /// <summary>
        /// Gets or sets a value indicating whether [match path].
        /// </summary>
        /// <value><c>true</c> if [match path]; otherwise, <c>false</c>.</value>
        public bool MatchPath
        {
            get => EverythingApiDllImport.Everything_GetMatchPath();
            set => EverythingApiDllImport.Everything_SetMatchPath(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [match case].
        /// </summary>
        /// <value><c>true</c> if [match case]; otherwise, <c>false</c>.</value>
        public bool MatchCase
        {
            get => EverythingApiDllImport.Everything_GetMatchCase();
            set => EverythingApiDllImport.Everything_SetMatchCase(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [match whole word].
        /// </summary>
        /// <value><c>true</c> if [match whole word]; otherwise, <c>false</c>.</value>
        public bool MatchWholeWord
        {
            get => EverythingApiDllImport.Everything_GetMatchWholeWord();
            set => EverythingApiDllImport.Everything_SetMatchWholeWord(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [enable regex].
        /// </summary>
        /// <value><c>true</c> if [enable regex]; otherwise, <c>false</c>.</value>
        public bool EnableRegex
        {
            get => EverythingApiDllImport.Everything_GetRegex();
            set => EverythingApiDllImport.Everything_SetRegex(value);
        }

        [DllImport("kernel32.dll")]
        private static extern int LoadLibrary(string name);

        #region Public

        /// <summary>
        /// Searches the specified key word and reset the everything API afterwards
        /// </summary>
        /// <param name="keyWord">The key word.</param>
        /// <param name="token">when cancelled the current search will stop and exit (and would not reset)</param>
        /// <param name="offset">The offset.</param>
        /// <param name="maxCount">The max count.</param>
        /// <returns></returns>
        public List<SearchResult> Search(string keyWord, CancellationToken token, int maxCount)
        {
            var results = new List<SearchResult>();

            if (string.IsNullOrEmpty(keyWord))
                throw new ArgumentNullException(nameof(keyWord));
            if (maxCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxCount));

            if (token.IsCancellationRequested) return results;
            if (keyWord.StartsWith("@"))
            {
                EverythingApiDllImport.Everything_SetRegex(true);
                keyWord = keyWord.Substring(1);
            }
            else
            {
                EverythingApiDllImport.Everything_SetRegex(false);
            }

            if (token.IsCancellationRequested) return results;
            EverythingApiDllImport.Everything_SetRequestFlags(RequestFlag.HighlightedFileName | RequestFlag.HighlightedFullPathAndFileName);
            if (token.IsCancellationRequested) return results;
            EverythingApiDllImport.Everything_SetOffset(0);
            if (token.IsCancellationRequested) return results;
            EverythingApiDllImport.Everything_SetMax(maxCount);
            if (token.IsCancellationRequested) return results;
            EverythingApiDllImport.Everything_SetSearchW(keyWord);

            if (token.IsCancellationRequested) return results;
            if (!EverythingApiDllImport.Everything_QueryW(true))
            {
                CheckAndThrowExceptionOnError();
                return results;
            }

            if (token.IsCancellationRequested) return results;
            var count = EverythingApiDllImport.Everything_GetNumResults();
            for (var idx = 0; idx < count; ++idx)
            {
                if (token.IsCancellationRequested) return results;
                // https://www.voidtools.com/forum/viewtopic.php?t=8169
                var fileNameHighlighted = Marshal.PtrToStringUni(EverythingApiDllImport.Everything_GetResultHighlightedFileNameW(idx));
                var fullPathHighlighted = Marshal.PtrToStringUni(EverythingApiDllImport.Everything_GetResultHighlightedFullPathAndFileNameW(idx));
                if ((fileNameHighlighted == null) | (fullPathHighlighted == null)) CheckAndThrowExceptionOnError();
                if (token.IsCancellationRequested) return results;
                ConvertHighlightFormat(fileNameHighlighted, out var fileNameHighlightData, out var fileName);
                if (token.IsCancellationRequested) return results;
                ConvertHighlightFormat(fullPathHighlighted, out var fullPathHighlightData, out var fullPath);

                var result = new SearchResult
                {
                    FileName = fileName,
                    FileNameHighlightData = fileNameHighlightData,
                    FullPath = fullPath,
                    FullPathHighlightData = fullPathHighlightData
                };

                if (token.IsCancellationRequested) return results;
                if (EverythingApiDllImport.Everything_IsFolderResult(idx))
                    result.Type = ResultType.Folder;
                else
                    result.Type = ResultType.File;

                results.Add(result);
            }

            return results;
        }

        public void Load(string sdkPath)
        {
            LoadLibrary(sdkPath);
        }

        #endregion

        #region Private

        private static void ConvertHighlightFormat(string contentHighlighted, out List<int> highlightData, out string fn)
        {
            highlightData = new List<int>();
            var content = new StringBuilder();
            var flag = false;
            var contentArray = contentHighlighted.ToCharArray();
            var count = 0;
            for (var i = 0; i < contentArray.Length; i++)
            {
                var current = contentHighlighted[i];
                if (current == '*')
                {
                    flag = !flag;
                    count = count + 1;
                }
                else
                {
                    if (flag) highlightData.Add(i - count);
                    content.Append(current);
                }
            }

            fn = content.ToString();
        }

        private static void CheckAndThrowExceptionOnError()
        {
            switch (EverythingApiDllImport.Everything_GetLastError())
            {
                case StateCode.CreateThreadError:
                    throw new CreateThreadException();
                case StateCode.CreateWindowError:
                    throw new CreateWindowException();
                case StateCode.InvalidCallError:
                    throw new InvalidCallException();
                case StateCode.InvalidIndexError:
                    throw new InvalidIndexException();
                case StateCode.IPCError:
                    throw new IPCErrorException();
                case StateCode.MemoryError:
                    throw new MemoryErrorException();
                case StateCode.RegisterClassExError:
                    throw new RegisterClassExException();
            }
        }

        #endregion
    }
}
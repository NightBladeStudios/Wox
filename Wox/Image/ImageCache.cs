namespace Wox.Image
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using Helper;
    using Infrastructure;
    using Infrastructure.Logger;
    using JetBrains.Annotations;
    using NLog;

    internal class CacheEntry
    {
        internal DateTime ExpiredDate;
        internal ImageSource Image;
        internal string Key;

        public CacheEntry(string key, ImageSource image, DateTime expiredTime)
        {
            Key = key;
            Image = image;
            ExpiredDate = expiredTime;
        }
    }

    internal class UpdateCallbackEntry
    {
        internal Func<string, ImageSource> ImageFactory;
        internal string Key;
        internal Action<ImageSource> UpdateImageCallback;

        public UpdateCallbackEntry(string key, Func<string, ImageSource> imageFactory, Action<ImageSource> updateImageCallback)
        {
            Key = key;
            ImageFactory = imageFactory;
            UpdateImageCallback = updateImageCallback;
        }
    }

    internal class ImageCache
    {
        private const int _cacheLimit = 500;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly SortedSet<CacheEntry> _cacheSorted;
        private readonly TimeSpan _checkInterval = new TimeSpan(1, 0, 0);
        private readonly TimeSpan _expiredTime = new TimeSpan(24, 0, 0);

        private readonly Timer timer;
        private readonly BlockingCollection<CacheEntry> _cacheQueue;
        private readonly BlockingCollection<UpdateCallbackEntry> _updateQueue;

        public ImageCache()
        {
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            _cacheSorted = new SortedSet<CacheEntry>(new CacheEntryComparer());
            _cacheQueue = new BlockingCollection<CacheEntry>();
            _updateQueue = new BlockingCollection<UpdateCallbackEntry>();

            timer = new Timer(ExpirationCheck, null, _checkInterval, _checkInterval);
            Task.Run(() =>
            {
                while (true)
                {
                    var entry = _cacheQueue.Take();
                    var currentCount = _cache.Count;
                    if (currentCount > _cacheLimit)
                    {
                        var min = _cacheSorted.Min;
                        _cacheSorted.Remove(min);
                        var removeResult = _cache.TryRemove(min.Key, out _);
                        Logger.WoxDebug($"remove exceed: <{removeResult}> entry: <{min.Key}>");
                    }
                    else
                    {
                        _cacheSorted.Remove(entry);
                    }

                    _cacheSorted.Add(entry);
                }
            }).ContinueWith(ErrorReporting.UnhandledExceptionHandleTask, TaskContinuationOptions.OnlyOnFaulted);
            Task.Run(() =>
            {
                while (true)
                {
                    var entry = _updateQueue.Take();
                    var addEntry = Add(entry.Key, entry.ImageFactory);
                    entry.UpdateImageCallback(addEntry.Image);
                }
            }).ContinueWith(ErrorReporting.UnhandledExceptionHandleTask, TaskContinuationOptions.OnlyOnFaulted);
        }

        #region Public

        /// <summary>
        /// Not thread safe, should be only called from ui thread
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ImageSource GetOrAdd([NotNull] string key, Func<string, ImageSource> imageFactory)
        {
            key.RequireNonNull();
            CacheEntry entry;
            var getResult = _cache.TryGetValue(key, out entry);
            if (!getResult)
            {
                entry = Add(key, imageFactory);
                return entry.Image;
            }

            UpdateDate(entry);
            return entry.Image;
        }

        public ImageSource GetOrAdd([NotNull] string key, ImageSource defaultImage, Func<string, ImageSource> imageFactory, Action<ImageSource> updateImageCallback)
        {
            key.RequireNonNull();
            CacheEntry getEntry;
            var getResult = _cache.TryGetValue(key, out getEntry);
            if (!getResult)
            {
                _updateQueue.Add(new UpdateCallbackEntry(key, imageFactory, updateImageCallback));
                return defaultImage;
            }

            UpdateDate(getEntry);
            return getEntry.Image;
        }

        #endregion

        #region Private

        private void ExpirationCheck(object state)
        {
            try
            {
                var now = DateTime.Now;
                Logger.WoxDebug($"ExpirationCheck start {now}");
                var pairs = _cache.Where(pair => now > pair.Value.ExpiredDate).ToList();

                foreach (var pair in pairs)
                {
                    var success = _cache.TryRemove(pair.Key, out var entry);
                    Logger.WoxDebug($"remove expired: <{success}> entry: <{pair.Key}>");
                }
            }
            catch (Exception e)
            {
                e.Data.Add(nameof(state), state);
                Logger.WoxError($"error check image cache with state: {state}", e);
            }
        }

        private CacheEntry Add(string key, Func<string, ImageSource> imageFactory)
        {
            CacheEntry entry;
            var image = imageFactory(key);
            entry = new CacheEntry(key, image, DateTime.Now + _expiredTime);
            _cache[key] = entry;
            _cacheQueue.Add(entry);
            return entry;
        }

        private void UpdateDate(CacheEntry entry)
        {
            entry.ExpiredDate = DateTime.Now + _expiredTime;
            _cacheQueue.Add(entry);
        }

        #endregion
    }

    internal class CacheEntryComparer : IComparer<CacheEntry>
    {
        #region Public

        public int Compare(CacheEntry x, CacheEntry y)
        {
            return x.ExpiredDate.CompareTo(y.ExpiredDate);
        }

        #endregion
    }
}
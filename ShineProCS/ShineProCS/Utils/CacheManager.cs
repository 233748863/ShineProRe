using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ShineProCS.Utils
{
    /// <summary>
    /// 缓存管理器
    /// 提供线程安全的全局缓存支持，具备自动清理机制
    /// 
    /// 【缓存设计说明】
    /// 1. 使用 ConcurrentDictionary 确保线程安全
    /// 2. 支持设置过期时间（TTL）
    /// 3. 模拟 Python 版本的统一缓存管理
    /// </summary>
    public class CacheManager
    {
        private static readonly Lazy<CacheManager> _instance = new Lazy<CacheManager>(() => new CacheManager());
        public static CacheManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();

        private class CacheItem
        {
            public object Value { get; set; }
            public DateTime Expiry { get; set; }
            public bool IsPermanent { get; set; }

            public CacheItem(object value, TimeSpan ttl)
            {
                Value = value;
                Expiry = DateTime.Now.Add(ttl);
                IsPermanent = ttl == TimeSpan.MaxValue;
            }
        }

        private CacheManager() { }

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="ttl">生存时间（默认 1 小时）</param>
        public void Set(string key, object value, TimeSpan? ttl = null)
        {
            var timeToLive = ttl ?? TimeSpan.FromHours(1);
            var item = new CacheItem(value, timeToLive);
            _cache.AddOrUpdate(key, item, (k, old) => item);
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        public T? Get<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (item.IsPermanent || item.Expiry > DateTime.Now)
                {
                    return item.Value as T;
                }
                
                // 已过期，移除
                _cache.TryRemove(key, out _);
            }
            return null;
        }

        /// <summary>
        /// 移除缓存
        /// </summary>
        public void Remove(string key)
        {
            _cache.TryRemove(key, out _);
        }

        /// <summary>
        /// 清理所有过期缓存
        /// </summary>
        public void CleanupExpired()
        {
            var now = DateTime.Now;
            foreach (var kvp in _cache)
            {
                if (!kvp.Value.IsPermanent && kvp.Value.Expiry < now)
                {
                    _cache.TryRemove(kvp.Key, out _);
                }
            }
        }

        /// <summary>
        /// 获取缓存统计
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }
    }
}

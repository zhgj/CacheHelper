using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Midgard.Common
{
    /// <summary>
    /// 缓存管理
    /// </summary>
    public class CacheManager
    {
        private volatile static CacheManager helper = null;
        private static readonly object lockHelper = new object();
        private CacheManager()
        {
            cache = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();
        }
        /// <summary>
        /// 取得单例
        /// </summary>
        /// <returns></returns>
        public static CacheManager GetInstance()
        {
            if (helper == null)
            {
                lock (lockHelper)
                {
                    if (helper == null)
                        helper = new CacheManager();
                }
            }
            return helper;
        }
        private ConcurrentDictionary<string, ConcurrentDictionary<string, object>> cache = null;

        /// <summary>
        /// 获取区块下的缓存
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public ConcurrentDictionary<string, object> GetRegion(string region)
        {
            if (cache.ContainsKey(region))
                return cache[region];
            else
                return new ConcurrentDictionary<string, object>();
        }

        /// <summary>
        /// 新增缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="region"></param>
        public object AddOrUpdate(string key, object value, string region)
        {
            var Region = GetRegion(region);
            if (Region.Count > 0)
            {
                if (Region.ContainsKey(key))
                {
                    //var json = JsonConvert.SerializeObject(cache, LogManager.Get().IsDebugEnabled ? Formatting.Indented : Formatting.None);
                    //LogManager.Get().ErrorFormat("cache333: {0}", json);
                    Region[key] = value;
                    return value;
                }
                else
                {
                    var va = Region.AddOrUpdate(key, value, (k, v) => value);
                    var di = cache.AddOrUpdate(region, Region, (k, v) => Region);
                    //LogManager.Get().Error("va2:" + JsonConvert.SerializeObject(va));
                    //LogManager.Get().Error("di2:" + JsonConvert.SerializeObject(di));
                    //var json = JsonConvert.SerializeObject(cache, LogManager.Get().IsDebugEnabled ? Formatting.Indented : Formatting.None);
                    //LogManager.Get().ErrorFormat("cache222: {0}", json);
                    return Region[key];
                }
            }
            else
            {
                var dict = new ConcurrentDictionary<string, object>();
                var va = dict.AddOrUpdate(key, value, (k, v) => value);
                var di = cache.AddOrUpdate(region, dict, (k, v) => dict);
                //LogManager.Get().Error("va:" + JsonConvert.SerializeObject(va));
                //LogManager.Get().Error("di:" + JsonConvert.SerializeObject(di));
                //var json = JsonConvert.SerializeObject(cache, LogManager.Get().IsDebugEnabled ? Formatting.Indented : Formatting.None);
                //LogManager.Get().ErrorFormat("cache111: {0}", json);
                return value;
            }
        }

        /// <summary>
        /// 删除缓存 找不到就删除失败返回false
        /// </summary>
        /// <param name="key"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public bool Remove(string key, string region)
        {
            object obj = null;
            var Region = GetRegion(region);
            if (Region.ContainsKey(key))
                return Region.TryRemove(key, out obj);
            else
                return false;
        }

        /// <summary>
        /// 获取某个缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public object Get(string key, string region)
        {
            var Region = GetRegion(region);
            if (Region.ContainsKey(key))
                return Region[key];
            else
                return null;
        }

        /// <summary>
        /// 通过区块前缀获取
        /// </summary>
        /// <param name="regionPrefix"></param>
        /// <returns></returns>
        public IEnumerable<ConcurrentDictionary<string, object>> GetRegionByPrefix(string regionPrefix)
        {
            List<ConcurrentDictionary<string, object>> result = new List<ConcurrentDictionary<string, object>>();
            foreach (var key in cache.Keys)
            {
                if (key.StartsWith(regionPrefix))
                {
                    result.Add(cache[key]);
                }
            }
            return result;
        }

        /// <summary>
        /// 清空区域
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public bool ClearRegion(string region)
        {
            ConcurrentDictionary<string, object> obj = null;
            if (cache.ContainsKey(region))
                return cache.TryRemove(region, out obj);
            else
                return false;
        }
    }
}

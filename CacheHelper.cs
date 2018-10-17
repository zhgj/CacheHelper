using System;
using System.Collections.Concurrent;
using System.Timers;

namespace Common
{
    /// <summary>
    /// 缓存管理
    /// </summary>
    public class CacheHelper
    {
        private static readonly CacheHelper helper = new CacheHelper();

        static CacheHelper()
        {
        }
        /// <summary>
        /// 帮助类实例
        /// </summary>
        public static CacheHelper Instance
        {
            get
            {
                return helper;
            }
        }

        private ConcurrentDictionary<string, ConcurrentDictionary<string, object>> cache = null;

        private ConcurrentDictionary<string,DateTime> expire = null;

        private Timer timer = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        private CacheHelper()
        {
            cache = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();
            expire = new ConcurrentDictionary<string, DateTime>();
            timer = new Timer(1000);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private static int inTimer = 0;

        /// <summary>
        /// System.Timers.Timer的回调方法
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (System.Threading.Interlocked.Exchange(ref inTimer, 1) == 0)
            {
                Remove();
                System.Threading.Interlocked.Exchange(ref inTimer, 0);
            }
        }

        /// <summary>
        /// 检测循环删除过期缓存
        /// </summary>
        /// <returns></returns>
        private bool Remove()
        {
            try
            {
                foreach (var expireTime in expire.Keys)
                {
                    string[] kv = expireTime.Split(new string[] { "***" }, StringSplitOptions.None);
                    string region = kv[0];
                    string key = kv[1];
                    DateTime dt = expire[expireTime];
                    if (dt < DateTime.Now)
                        Remove(key, region);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断缓存是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public bool Exists(string key, string region)
        {
            if (cache.ContainsKey(region))
            {
                var Region = cache[key];
                return Region.ContainsKey(key);
            }
            else
                return false;
        }

        /// <summary>
        /// 新增缓存 已存在就失败返回false
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="region"></param>
        public bool Add(string key, object value, string region)
        {
            var Region = GetRegion(region);
            if (Region != null)
            {
                if (Region.ContainsKey(key))
                    return false;
                else
                {
                    Region.TryAdd(key, value);
                    cache.TryAdd(region, Region);
                    return true;
                }
            }
            else
            {
                var dict = new ConcurrentDictionary<string, object>();
                dict.TryAdd(key, value);
                cache.TryAdd(region, dict);
                return true;
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
            if (Region != null && Region.ContainsKey(key))
                return Region.TryRemove(key, out obj);
            else
                return false;
        }

        /// <summary>
        /// 更新缓存 没找到就是失败返回false
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public bool Update(string key, object value, string region)
        {
            var Region = GetRegion(region);
            if (Region != null && Region.ContainsKey(key))
            {
                Region[key] = value;
                return true;
            }
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
            if (Region != null && Region.ContainsKey(key))
                return Region[key];
            else
                return null;
        }

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
                return null;
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

        /// <summary>
        /// 设置缓存过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="region"></param>
        /// <param name="expireTime"></param>
        /// <returns></returns>
        public bool Expire(string key, string region, TimeSpan expireTime)
        {
            try
            {
                string k = region + "***" + key;
                DateTime dt = DateTime.Now + expireTime;
                if (expire.ContainsKey(k))
                    expire[k] = dt;
                else
                    expire.TryAdd(k, dt);
                return true;
            }
            catch { return false; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Newtonsoft.Json;
using System.IO;

namespace CacheTest {
	public class Cache {

		/// <summary>
		/// Cache Name
		/// </summary>
		public string CacheName { get; set; }

		/// <summary>
		/// Cache Directory
		/// </summary>
		public string CacheDirectory => Path.Combine(Config.AppDataDirectory, "AsuvrilStorage");

		/// <summary>
		/// Cache file save path
		/// </summary>
		public string SavePath => Path.Combine(CacheDirectory, $"{CacheName}.json");

		private List<CacheEntity> JsonContent { get; set; } = null;

		/// <summary>
		/// 创建缓存并读取，如果缓存已存在则直接读取
		/// </summary>
		/// <param name="CacheName">缓存名</param>
		public Cache(string CacheName = "Public") {
			this.CacheName = CacheName;

			if (!Directory.Exists(CacheDirectory))
			{
				Directory.CreateDirectory(CacheDirectory);
			}
			if (!File.Exists(SavePath))
			{
				File.Create(SavePath).Close();
			}

			var TextContent = File.ReadAllText(SavePath);
			if (!string.IsNullOrEmpty(TextContent))
			{
				JsonContent = JsonConvert.DeserializeObject<List<CacheEntity>>(TextContent);
			}
			else
			{
				JsonContent = new List<CacheEntity>();
			}

			SaveBeforClose();
		}

		/// <summary>
		/// Write cache content to file when closing
		/// </summary>
		private void SaveBeforClose() {
			AppDomain.CurrentDomain.ProcessExit += (sender, args) => this.Save();
			AppDomain.CurrentDomain.DomainUnload += (sender, args) => this.Save();
		}

		/// <summary>
		/// 保存缓存。将缓存中的所有数据存入Json文件中
		/// </summary>
		/// <returns></returns>
		public bool Save() {
			try
			{
				var TextContent = JsonConvert.SerializeObject(JsonContent);
				File.WriteAllText(SavePath, TextContent);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Cache Save Exception：{ex.Message}");
				return false;
			}

		}

		/// <summary>
		/// 缓存数据。
		/// 数据名已存在但值类型不同时，如果ForceSave为true时，直接覆盖，否则抛出异常。
		/// 数据名不存在则创建。
		/// </summary>
		/// <param name="key">数据名</param>
		/// <param name="value">数据值</param>
		/// <param name="ForceSave">数据值类型不同时是否强制覆盖</param>
		/// <param name="EffectiveDuration">增加有效时长，单位为秒</param>
		/// <exception cref="数据类型不同异常。当数据名已存在，数据值类型不同，且ForceSave为False时触发"></exception>
		public void Set(string key, object value, bool ForceSave = true, int EffectiveDuration = 0) {

			var vs = JsonConvert.SerializeObject(value);
			var JsonTarget = JsonContent.Find(j => j.Key == key);

			if (JsonTarget == null)
			{
				JsonTarget = new CacheEntity();
				JsonTarget.Key = key;
				JsonTarget.Value = vs;
				JsonTarget.Type = value.GetType();
				if (EffectiveDuration != 0)
				{
					if (JsonTarget.ExpireDate == default(DateTime) || JsonTarget.ExpireDate != default(DateTime) && DateTime.Now > JsonTarget.ExpireDate)
					{
						JsonTarget.ExpireDate = DateTime.Now.AddSeconds(EffectiveDuration);
					}
					else
					{
						JsonTarget.ExpireDate = JsonTarget.ExpireDate.AddSeconds(EffectiveDuration);
					}
				}

				JsonContent.Add(JsonTarget);

			}
			else
			{
				var a = JsonTarget.Type == value.GetType();
				if (JsonTarget.Type == value.GetType() || ForceSave)
				{
					JsonTarget.Value = vs;
					JsonTarget.Type = value.GetType();
					if (EffectiveDuration != 0)
					{
						if (JsonTarget.ExpireDate == default(DateTime) || JsonTarget.ExpireDate != default(DateTime) && DateTime.Now > JsonTarget.ExpireDate)
						{
							JsonTarget.ExpireDate = DateTime.Now.AddSeconds(EffectiveDuration);
						}
						else
						{
							JsonTarget.ExpireDate = JsonTarget.ExpireDate.AddSeconds(EffectiveDuration);
						}
					}
				}
				else
				{
					throw new Exception($"[数类型据]名为{key}数据的值类型为{JsonTarget.Type},而当前值类型为{value.GetType()}，请考虑在使用Set()方法时将ForceSave设置为true");
				}
			}
		}

		/// <summary>
		/// 获取数据值，如果数据名不存在，则返回要转换的类型默认值
		/// </summary>
		/// <typeparam name="T">要转换的类型</typeparam>
		/// <param name="key">数据名</param>
		/// <returns></returns>
		public T Get<T>(string key) {
			var JsonTarget = JsonContent.Find(j => j.Key == key && j.ExpireDate > DateTime.Now);
			if (JsonTarget != null)
			{
				var type = JsonTarget.Type;
				var value = JsonConvert.DeserializeObject<T>(JsonTarget.Value);
				return value;
			}
			return default(T);
		}

		/// <summary>
		/// 获取数据值，如果数据名不存在，则返回null
		/// </summary>
		/// <param name = "key" > 数据名 </ param >
		/// < returns ></ returns >
		public object Get(string key) {
			var js = JsonContent.Find(j => j.Key == key && j.ExpireDate > DateTime.Now)?.Value;
			return JsonConvert.DeserializeObject(js);
		}

		public Type GetType(string key) {
			return JsonContent.Find(j => j.Key == key)?.Type;
		}

		/// <summary>
		/// 判断数据名是否存在
		/// </summary>
		/// <param name="key">数据名</param>
		/// <returns></returns>
		public bool Exists(string key) {
			return JsonContent.Any(j => j.Key == key);
		}

		/// <summary>
		/// 删除数据名，如果数据名不存在，则直接返回true
		/// </summary>
		/// <param name="key">数据名</param>
		/// <returns ></returns>
		public bool Delete(string key) {
			var JsonTarget = JsonContent.Find(j => j.Key == key);
			if (JsonTarget == null)
			{
				return true;
			}
			return JsonContent.Remove(JsonTarget);
		}

		/// <summary>
		/// 清除缓存中的所有数据
		/// </summary>
		public void Clear() {
			JsonContent = new List<CacheEntity>();
		}

		class CacheEntity {
			/// <summary>
			/// 数据名
			/// </summary>
			public string Key { get; set; }

			/// <summary>
			/// 数据值
			/// </summary>
			public string Value { get; set; }

			/// <summary>
			/// 到期时间
			/// </summary>
			public DateTime ExpireDate { get; set; } = default(DateTime);

			/// <summary>
			/// 数据类型
			/// </summary>
			public Type Type { get; set; }
		}
	}

}

using System.Collections.Generic;
using JsonLD.Util;
using Newtonsoft.Json.Linq;

namespace JsonLD.Util
{
	public class Obj
	{
		/// <summary>
		/// Used to make getting values from maps embedded in maps embedded in maps
		/// easier TODO: roll out the loops for efficiency
		/// </summary>
		/// <param name="map"></param>
		/// <param name="keys"></param>
		/// <returns></returns>
		public static object Get(JToken map, params string[] keys)
		{
			foreach (string key in keys)
			{
				map = ((IDictionary<string, JToken>)map)[key];
				// make sure we don't crash if we get a null somewhere down the line
				if (map == null)
				{
					return map;
				}
			}
			return map;
		}

		public static object Put(object map, string key1, object value)
		{
			((IDictionary<string, object>)map)[key1] = value;
			return map;
		}

		public static object Put(object map, string key1, string key2, object value)
		{
			((IDictionary<string, object>)((IDictionary<string, object>)map)[key1])[key2] = value;
			return map;
		}

		public static object Put(object map, string key1, string key2, string key3, object
			 value)
		{
			((IDictionary<string, object>)((IDictionary<string, object>)((IDictionary<string, 
				object>)map)[key1])[key2])[key3] = value;
			return map;
		}

		public static object Put(object map, string key1, string key2, string key3, string
			 key4, object value)
		{
			((IDictionary<string, object>)((IDictionary<string, object>)((IDictionary<string, 
				object>)((IDictionary<string, object>)map)[key1])[key2])[key3])[key4] = value;
			return map;
		}

		public static bool Contains(object map, params string[] keys)
		{
			foreach (string key in keys)
			{
				map = ((IDictionary<string, JToken>)map)[key];
				if (map == null)
				{
					return false;
				}
			}
			return true;
		}

		public static object Remove(object map, string k1, string k2)
		{
			return JsonLD.Collections.Remove(((IDictionary<string, object>)((IDictionary<string
				, object>)map)[k1]), k2);
		}

		/// <summary>A null-safe equals check using v1.equals(v2) if they are both not null.</summary>
		/// <remarks>A null-safe equals check using v1.equals(v2) if they are both not null.</remarks>
		/// <param name="v1">The source object for the equals check.</param>
		/// <param name="v2">
		/// The object to be checked for equality using the first objects
		/// equals method.
		/// </param>
		/// <returns>
		/// True if the objects were both null. True if both objects were not
		/// null and v1.equals(v2). False otherwise.
		/// </returns>
		public static bool Equals(object v1, object v2)
		{
			return v1 == null ? v2 == null : v1.Equals(v2);
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;

namespace JsonLD.Infrastructure.Text.Tests
{
    internal static class DictionaryExtensions
    {
        public static T Required<T>(this Dictionary<string, object> dictionary, string propertyName) => Extract<T>(dictionary, propertyName, true);

        public static T Optional<T>(this Dictionary<string, object> dictionary, string propertyName) => Extract<T>(dictionary, propertyName, false);

        private static T Extract<T>(Dictionary<string, object> dictionary, string propertyName, bool isRequired)
        {
            if (!dictionary.ContainsKey(propertyName))
            {
                if (isRequired)
                {
                    var message = $"Expected top-level property {propertyName} but only found {string.Join(", ", dictionary.Keys)}";
                    throw new Exception(message);
                } 
                else
                {
                    return default;
                }
            }
            var value = dictionary[propertyName];
            if (typeof(T).IsGenericType) {
                var genericType = typeof(T).GetGenericTypeDefinition();
                if (genericType == typeof(List<>))
                {
                    var list = Activator.CreateInstance<T>();
                    var addMethod = typeof(T).GetMethod("Add");
                    foreach(var item in (IEnumerable)value)
                    {
                        addMethod.Invoke(list, new[] { item });
                    }
                    return list;
                }
            }            
            return (T)value;
        }
    }
}
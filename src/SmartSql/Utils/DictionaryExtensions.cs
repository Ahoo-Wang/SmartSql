using System;
using System.Collections.Generic;
using System.Text;
using SmartSql.Exceptions;

namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static bool Value<TKey, TValue, TTypedValue>(this IDictionary<TKey, TValue> dic, TKey key, out TTypedValue value)
        {
            value = default;
            if (dic == null) { return false; }
            if (!dic.TryGetValue(key, out var val))
            {
                return false;
            }

            object objVal = val;
            var typedValType = typeof(TTypedValue);
            if (typedValType.IsEnum && objVal is string)
            {
                value = (TTypedValue)Enum.Parse(typedValType, objVal.ToString());
            }
            else if (typeof(IConvertible).IsAssignableFrom(typedValType))
            {
                value = (TTypedValue)Convert.ChangeType(objVal, typedValType);
            }
            else
            {
                value = (TTypedValue)objVal;
            }
            return true;
        }
        public static void EnsureValue<TKey, TValue, TTypedValue>(this IDictionary<TKey, TValue> dic, TKey key, out TTypedValue value)
        {
            if (!dic.Value(key, out value))
            {
                throw new SmartSqlException($"Can not find Parameter:{key}!");
            }
        }
        public static void AddRange<TKey, TValue>(this IDictionary<TKey,TValue> dic, IDictionary<TKey,TValue> collection)
        {
            foreach(var  item in collection)
            {
                dic.Add(item);
            }
        }

    }
}

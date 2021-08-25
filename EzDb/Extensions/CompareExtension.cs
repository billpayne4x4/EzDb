using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MPSHouse.EzDb.Attributes;

namespace MPSHouse.EzDb.Extensions
{
    public static class CompareExtension
    {
        public static List<string> Log;
        public static IEnumerable<string> Compare<TFrom, TTo>(this TFrom from, TTo to, string parent = "", bool ignoreId = false, bool ignoreCreatedAndUpdated = false)
        {
            Type tTo = typeof(TTo);
            Type tFrom = typeof(TFrom);
            List<string> results = new List<string>();
            if (string.IsNullOrEmpty(parent)) parent = tFrom.Name;
            Log = Log ?? new List<string>();

            PropertyInfo[] fromProperties = tFrom.GetProperties();
            PropertyInfo[] toProperties = tTo.GetProperties();

            foreach (PropertyInfo fromProperty in fromProperties)
            {
                foreach (PropertyInfo toProperty in toProperties)
                {
                    if(fromProperty.GetCustomAttributes(typeof(EzDbIgnoreAttribute)).Count() > 0 || toProperty.GetCustomAttributes(typeof(EzDbIgnoreAttribute)).Count() > 0) continue;
                    Type tFromProperty = fromProperty.PropertyType;
                    Type tToProperty = toProperty.PropertyType;

                    if(fromProperty.Name == "Table2" && toProperty.Name == "Table2")
                    {
                        
                    }

                    // Cathes:
                    // HashId ((type)Id and Id) or (Id and (type)Id)
                    // HashId ((same name - (from)typeof(string))Id  and (same name - (to)typeof(long or long?))Id)
                    // HashId ((same name - (from)typeof(long or long?))Id and (same name - (to)typeof(string))Id)
                    if ((!ignoreId && ((fromProperty.Name == $"{tTo.Name}Id" && toProperty.Name == "Id") || (fromProperty.Name == "Id" && toProperty.Name == $"{tFrom.Name}Id"))) ||
                        ((fromProperty.Name == toProperty.Name) && (fromProperty.Name.EndsWith("Id") && toProperty.Name.EndsWith("Id"))) &&
                            ((fromProperty.PropertyType == typeof(string) && (toProperty.PropertyType == typeof(long?) || toProperty.PropertyType == typeof(long))) ||
                            (toProperty.PropertyType == typeof(string)) || (toProperty.PropertyType == typeof(long?) || toProperty.PropertyType == typeof(long))))
                    {
                        long? fromId = null;
                        long? toId = null;

                        // From
                        if (!(from is null))
                        {
                            if (fromProperty.PropertyType == typeof(string)) fromId = ((string)fromProperty.GetValue(from))?.HashIdDecode();
                            else if (fromProperty.PropertyType == typeof(long)) fromId = (long)fromProperty.GetValue(from);
                            else if (fromProperty.PropertyType == typeof(long?)) fromId = ((long?)fromProperty.GetValue(from));
                        }

                        // To
                        if (!(to is null))
                        {
                            if (toProperty.PropertyType == typeof(string)) toId = ((string)toProperty.GetValue(to))?.HashIdDecode();
                            else if (toProperty.PropertyType == typeof(long)) toId = (long)toProperty.GetValue(to);
                            else if (toProperty.PropertyType == typeof(long?)) toId = ((long?)toProperty.GetValue(to));
                        }

                        string equality = toId == fromId ? "==" : "!=";
                        string fromValue = fromId.HasValue ? fromId.Value.ToString() : "null";
                        string toValue = toId.HasValue ? toId.Value.ToString() : "null";
                        if (fromProperty.PropertyType != typeof(string))
                        {
                            if (fromId != toId)
                            {
                                results.Add($"{parent}.{fromProperty.Name} ({fromValue} {equality} {toValue})");
                            }
                            Log.Add($"{parent}.{fromProperty.Name} ({fromValue} {equality} {toValue})");
                        }
                        else
                        {
                            if (toId != fromId)
                            {
                                results.Add($"{parent}.{fromProperty.Name} ({toValue} {equality} {fromValue})");
                                Log.Add($"{parent}.{fromProperty.Name} ({toValue} {equality} {fromValue})");
                            }
                        }
                    }
                    else if (toProperty.Name != fromProperty.Name) continue;
                    else if (toProperty.PropertyType.IsClass &&
                        !toProperty.PropertyType.Assembly.FullName.StartsWith("System") &&
                        !toProperty.PropertyType.Assembly.FullName.StartsWith("Microsoft"))
                    {
                        var fromValue = from is null ? null : fromProperty.GetValue(from);
                        var toValue = to is null ? null : toProperty.GetValue(to);
                        if (fromValue is null && toValue is null)
                            Log.Add($"{parent}.{fromProperty.Name} (null == null))");
                        else if (fromValue is null || toValue is null)
                        {
                            fromValue = fromValue is null ? "null" : fromValue.ToString();
                            toValue = toValue is null ? "null" : toValue.ToString();
                            results.Add($"{parent}.{fromProperty.Name} ({fromValue} != {toValue}))");
                            Log.Add($"{parent}.{fromProperty.Name} ({fromValue} != {toValue}))");
                        }
                        else
                        {
                            results.AddRange((IEnumerable<string>)typeof(CompareExtension).GetTypeInfo()
                                .DeclaredMethods.Where(w => w.Name == "Compare")
                                .SingleOrDefault()?
                                .MakeGenericMethod(fromProperty.PropertyType, toProperty.PropertyType)
                                .Invoke(null, new object[] { fromProperty.GetValue(from), toProperty.GetValue(to), $"{parent}.{fromProperty.Name}", ignoreId, ignoreCreatedAndUpdated }));
                        }
                    }
                    else if (toProperty.PropertyType.Name == "IEnumerable`1" && !(to is null || from is null || toProperty is null || fromProperty is null || toProperty.GetValue(to) is null || fromProperty.GetValue(from) is null)) // TODO: I added a condition to and from must not be null, I need to add a compare of null and not null (Which would fail of cause :p)
                    {
                        var fromValue = from is null ? null : fromProperty.GetValue(from);
                        var toValue = to is null ? null : toProperty.GetValue(to);
                        if (fromValue is null && toValue is null)
                            Log.Add($"{parent}.{fromProperty.Name} (null == null))");
                        else if (fromValue is null || toValue is null)
                        {
                            fromValue = fromValue is null ? "null" : fromValue.ToString();
                            toValue = toValue is null ? "null" : toValue.ToString();
                            results.Add($"{parent}.{fromProperty.Name} ({fromValue} != {toValue}))");
                            Log.Add($"{parent}.{fromProperty.Name} ({fromValue} != {toValue}))");
                        }
                        else
                        {
                            results.AddRange((IEnumerable<string>)typeof(CompareExtension).GetTypeInfo()
                                .DeclaredMethods.Where(w => w.Name == "HandleCompareIEnumerable")
                                .SingleOrDefault()?
                                .MakeGenericMethod(fromProperty.PropertyType.GenericTypeArguments[0], toProperty.PropertyType.GenericTypeArguments[0])
                                .Invoke(null, new object[] { fromProperty.GetValue(from), toProperty.GetValue(to), parent, ignoreId, ignoreCreatedAndUpdated }));
                        }
                    }
                    else if (!toProperty.PropertyType.AssemblyQualifiedName.StartsWith("System.Collections") && !(from is null) &&
                                !(ignoreCreatedAndUpdated &&
                                    ((toProperty.Name == "Created" && toProperty.Name == "Created") ||
                                    (toProperty.Name == "Updated" && toProperty.Name == "Updated"))))
                    {
                        string f = fromProperty.GetValue(from)?.ToString();
                        string t = toProperty.GetValue(to)?.ToString();
                        if (fromProperty.GetValue(from)?.ToString() != toProperty.GetValue(to)?.ToString())
                        {
                            string log = $"{parent}.{fromProperty.Name} ({fromProperty.GetValue(from)?.ToString()} != {toProperty.GetValue(to)?.ToString()})";
                            results.Add(log);
                            Log.Add(log);
                        }
                        else
                        {
                            Log.Add($"{parent}.{fromProperty.Name} ({fromProperty.GetValue(from)?.ToString()} == {toProperty.GetValue(to)?.ToString()})");
                        }
                    }
                }
            }
            return results;
        }

        public static IEnumerable<string> HandleCompareIEnumerable<TFrom, TTo>(object oFromItems, object oToItems, string parent = "", bool ignoreId = false, bool ignoreCreatedAndUpdated = false)
        {
            List<string> results = new List<string>();

            TFrom[] fromItems = ((IEnumerable<TFrom>)oFromItems).ToArray();
            TTo[] toItems = ((IEnumerable<TTo>)oToItems).ToArray();

            if (fromItems.Count() != toItems.Count())
            {
                results.Add($"{parent} Item count does not match. (From: {fromItems.Count()} | To: {toItems.Count()})");
                return results;
            }

            for (int iItemIndex = 0; iItemIndex < fromItems.Count(); iItemIndex++)
            {
                TFrom fromItem = fromItems[iItemIndex];
                TTo toItem = toItems[iItemIndex];
                results.AddRange((IEnumerable<string>)typeof(CompareExtension).GetTypeInfo()
                    .DeclaredMethods.Where(w => w.Name == "Compare")
                    .SingleOrDefault()?
                    .MakeGenericMethod(typeof(TFrom), typeof(TTo))
                    .Invoke(null, new object[] { fromItem, toItem, $"{parent}.{fromItem.GetType().Name}[{iItemIndex}]", ignoreId, ignoreCreatedAndUpdated }));
            }
            return results;
        }
    }
}
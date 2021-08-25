using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MPSHouse.EzDb.Attributes;
using MPSHouse.EzDb.Extensions;

namespace MPSHouse.EzDb.Extensions
{
    public static class CopyExtension
    {
        public static TTo Copy<TFrom, TTo>(this TFrom from, bool updateUpdated = false, bool updateCreated = false, TTo to = null)
            where TFrom : class
            where TTo : class
        {
            int iPropInfo1 = 0;
            int iPropInfo2 = 0;

            Type tTo = typeof(TTo);
            Type tFrom = typeof(TFrom);
            to = to ?? (TTo)Activator.CreateInstance(tTo);

            PropertyInfo[] fromProperties = tFrom.GetProperties();
            PropertyInfo[] toProperties = tTo.GetProperties();

            PropertyInfo fromProperty;
            PropertyInfo toProperty;

            for (iPropInfo1 = 0; iPropInfo1 < fromProperties.Length; iPropInfo1++)
            {
                fromProperty = fromProperties[iPropInfo1];
                for (iPropInfo2 = 0; iPropInfo2 < toProperties.Length; iPropInfo2++)
                {
                    toProperty = toProperties[iPropInfo2];
                    
                    if(fromProperty.GetCustomAttributes(typeof(EzDbIgnoreAttribute)).Count() > 0 || toProperty.GetCustomAttributes(typeof(EzDbIgnoreAttribute)).Count() > 0) continue;
                    
                    // HashId ((same name - (from)typeof(string))Id  and (same name - (to)typeof(long or long?))Id)
                    if(((fromProperty.Name == toProperty.Name) && (fromProperty.Name.EndsWith("Id") && toProperty.Name.EndsWith("Id"))) && 
                            (fromProperty.PropertyType == typeof(string) && (toProperty.PropertyType == typeof(long?) || toProperty.PropertyType == typeof(long))) && !(from is null))
                    {
                        string hashedId = (string)fromProperty.GetValue(from);
                        if (!(hashedId is null))
                        {
                            toProperty.SetValue(to, hashedId.HashIdDecode());
                        }
                        else
                            toProperty.SetValue(to, null);
                    }
                    // HashId ((same name - (from)typeof(long or long?))Id  and (same name - (to)typeof(string))Id)
                    else if(((fromProperty.Name == toProperty.Name) && (fromProperty.Name.EndsWith("Id") && toProperty.Name.EndsWith("Id"))) && 
                            ((fromProperty.PropertyType == typeof(long?) || fromProperty.PropertyType == typeof(long)) && toProperty.PropertyType == typeof(string)) && !(from is null))
                    {
                        long? nullableLongFrom = (long?)fromProperty.GetValue(from);
                        if(!nullableLongFrom.HasValue)
                            toProperty.SetValue(to, null);
                        else
                            toProperty.SetValue(to, nullableLongFrom.Value.HashIdEncode());
                    }
                    // HashId ((type)Id  and Id)
                    else if (fromProperty.Name == $"{tTo.Name}Id" && toProperty.Name == "Id" && !(from is null))
                    {
                        string hashedId = (string)fromProperty.GetValue(from);
                        if (!(hashedId is null))
                        {
                            toProperty.SetValue(to, hashedId.HashIdDecode());
                        }
                        else
                            toProperty.SetValue(to, null);
                    }
                    else if (fromProperty.Name == "Id" && toProperty.Name == $"{tFrom.Name}Id")
                    {
                        toProperty.SetValue(to, ((long)fromProperty.GetValue(from)).HashIdEncode());
                    }
                    else if (fromProperty.Name == toProperty.Name && !(from is null) && !(fromProperty is null)) // TODO: I added a condition to and from must not be null, I need to add a compare of null and not null (Which would fail of cause :p))
                    {
                        string temp = toProperty.PropertyType.Assembly.FullName;
                        var t = toProperty.PropertyType;
                        object fromValue = fromProperty.GetValue(from);
                        bool b = t.IsArray;
                        if (!(fromValue is null) &&
                            toProperty.PropertyType.IsClass &&
                            !toProperty.PropertyType.Assembly.FullName.StartsWith("System") &&
                            !toProperty.PropertyType.Assembly.FullName.StartsWith("Microsoft"))
                        {
                            toProperty.SetValue(to, typeof(CopyExtension).GetTypeInfo()
                                .GetMethod("Copy")?
                                .MakeGenericMethod(fromProperty.PropertyType, toProperty.PropertyType)
                                .Invoke(null, new object[] { fromValue, updateUpdated, updateCreated, toProperty.GetValue(to) }));
                        }
                        else if (!(fromValue is null) && toProperty.PropertyType.Name == "IEnumerable`1")
                        {
                            toProperty = (PropertyInfo)typeof(CopyExtension).GetTypeInfo()
                                .DeclaredMethods.Where(w => w.Name == "HandleCopyIEnumerable")
                                .SingleOrDefault()?
                                .MakeGenericMethod(fromProperty.PropertyType.GetGenericArguments()[0], toProperty.PropertyType.GetGenericArguments()[0], tTo)
                                .Invoke(null, new object[] { fromValue, toProperty.GetValue(to), to, toProperty, updateUpdated, updateCreated });

                        }
                        else if (!toProperty.PropertyType.AssemblyQualifiedName.StartsWith("System.Collections"))
                        {
                            toProperty.SetValue(to, fromValue);
                        }
                        break;
                    }
                }
            }

            if (updateUpdated && toProperties.Count(c => c.Name == "Updated") == 1)
                toProperties.SingleOrDefault(w => w.Name == "Updated")?.SetValue(to, DateTime.UtcNow);
            if (updateCreated && toProperties.Count(c => c.Name == "Created") == 1)
                toProperties.SingleOrDefault(w => w.Name == "Created").SetValue(to, DateTime.UtcNow);

            return to;
        }

        private static PropertyInfo HandleCopyIEnumerable<TFrom, TTo, TToParent>(object items, IEnumerable<TTo> toEnumerable, TToParent toParent, PropertyInfo toProperty, bool updateUpdated = false, bool updateCreated = false)
            where TFrom : class
            where TTo : class
            where TToParent : class
        {
            List<TFrom> fromItems = new List<TFrom>((IEnumerable<TFrom>)items);
            List<TTo> toItems = toEnumerable is null ? (List<TTo>)Activator.CreateInstance(typeof(List<TTo>)) : new List<TTo>(toEnumerable);
            for (int i = 0; i < fromItems.Count; i++)
            {
                TTo to = (TTo)typeof(CopyExtension).GetTypeInfo()
                        .GetMethod("Copy")
                        .MakeGenericMethod(typeof(TFrom), typeof(TTo))
                        .Invoke(null, new object[] { fromItems[i], updateUpdated, updateCreated, fromItems.Count > toItems.Count ? (TTo)null : toItems[i] });

                if (fromItems.Count > toItems.Count)
                    toItems.Add(to);
                else
                    toItems[i] = to;
            }

            toProperty.SetValue(toParent, toItems);
            return toProperty;
        }
    }
}
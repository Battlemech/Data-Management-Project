﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Mime;
using DMP.Databases.VS;
using DMP.Utility;

namespace DMP.Databases
{
    public partial class Database
    {
        public readonly string Id;
        private readonly Dictionary<string, ValueStorage> _values = new Dictionary<string, ValueStorage>();

        public Database(string id, bool isPersistent = false, bool isSynchronised = false)
        {
            Id = id;
            IsPersistent = isPersistent;
        }

        public ValueStorage<T> Get<T>(string id)
        {
            lock (_values)
            {
                //retrieve valueStorage
                if (_values.TryGetValue(id, out ValueStorage valueStorage))
                {
                    //return valueStorage if it is of expected type
                    if (valueStorage is ValueStorage<T> expected) return expected;

                    throw new ArgumentException($"Saved value has type {valueStorage.GetEnclosedType()}, not {typeof(T)}");
                }
                
                //try loading persistent value
                T obj;
                lock (_serializedData)
                {
                    obj = (_serializedData.TryGetValue(id, out byte[] bytes))
                        ? Serialization.Deserialize<T>(bytes)
                        : default;
                }
                
                //create valueStorage
                ValueStorage<T> newVs = new ValueStorage<T>(this, id, obj);
                
                _values.Add(id, newVs);
                return newVs;
            }
        }
    }
}
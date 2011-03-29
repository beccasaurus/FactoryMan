using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;

/// <summary>
/// FactoryMan namespace ...
/// </summary>
namespace FactoryMan {
 
    /// <summary>
    /// Factory class ...
    /// </summary>
    public class Factory {

        /// <summary>
        /// static method ...
        /// </summary>
        /// <param name="objectType">it is cool</param>
        /// <param name="anonymousType">also cool</param>
        /// <returns>some cool shit!</returns>
        public static Factory Define(Type objectType, object anonymousType) {
            return new Factory(objectType, anonymousType);
        }

        // Gives you the option of using Factory.Null as anonymous type values instead of "(object) null"
        public static object Null = (object) null;

        public static string         CreateMethod;
        public static Action<object> CreateAction;

        string _name;
        internal Dictionary<string, object>               _properties     = new Dictionary<string, object>();
        internal Dictionary<string, Func<object, object>> _funcProperties = new Dictionary<string, Func<object, object>>();
        internal IDictionary _typedFuncProperties;

        public Factory() { }

        public Factory(Type objectType) {
            ObjectType = objectType;
        }

        public Factory(Type objectType, string name) {
            ObjectType = objectType;
            Name       = name;
        }

        public Factory(Type objectType, object anonymousType) {
            ObjectType = objectType;
            Add(anonymousType);
        }

        public virtual Factory SetCreateMethod(string method) {
            InstanceCreateMethod = method;
            return this;
        }
        public virtual Factory SetCreateAction(Action<object> action) {
            InstanceCreateAction = action;
            return this;
        }

        public virtual Type ObjectType { get; set; }
        public virtual Action<object> InstanceCreateAction { get; set; }
        public virtual string InstanceCreateMethod { get; set; }

        public virtual string Name {
            get {
                if (_name == null)
                    if (ObjectType != null)
                        _name = ObjectType.Name;
                return _name;
            }
            set { _name = value;  }
        }

        public virtual int Count {
            get {
                var count = _properties.Count + _funcProperties.Count;
                if (_typedFuncProperties != null)
                    count += _typedFuncProperties.Count;
                return count;
            }
        }

        public virtual object this[string propertyName] {
            get {
                if (_properties.ContainsKey(propertyName))
                    return _properties[propertyName];
                else if (_funcProperties.ContainsKey(propertyName))
                    return _funcProperties[propertyName];
                else
                    return null;
            }
        }

        public virtual Factory Add(object anonymousType) {
            foreach (var property in anonymousType.ToDictionary())
                Add(property.Key, property.Value);
            return this;
        }

        public virtual Factory Add(string propertyName, object propertyValue) {
            _properties.Add(propertyName, propertyValue);
            return this;
        }

        public virtual Factory Add(string propertyName, Func<object, object> propertyValue) {
            _funcProperties.Add(propertyName, propertyValue);
            return this;
        }

        public virtual Func<object, object> Func(string propertyName) {
            return _funcProperties[propertyName];
        }

        public virtual IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
            foreach (var keyValuePair in _properties)
                yield return keyValuePair;
            foreach (var keyValuePair in _funcProperties)
                yield return new KeyValuePair<string, object>(keyValuePair.Key, (object)keyValuePair.Value);
        }

        public virtual Dictionary<string, object> Properties {
            get {
                var properties = new Dictionary<string, object>();
                foreach (var property in this)
                    properties.Add(property.Key, property.Value);
                return properties;
            }
        }

        public virtual object Build() {
            return Build(null);
        }
        public virtual object Build(object overrides) {
            return Build(overrides.ToDictionary());
        }
        public virtual object Build(IDictionary<string,object> overrides) {
            var instance   = Activator.CreateInstance(ObjectType);
            var properties = Properties;

            if (overrides != null)
                foreach (var property in overrides)
                    properties[property.Key] = property.Value;

            foreach (var property in properties)
                if (property.Value != null && property.Value.GetType().FullName.StartsWith("System.Func")) { // call Invoke() if it's a Func
                    var value = property.Value.GetType().GetMethod("Invoke").Invoke(property.Value, new object[] { instance });
                    ObjectType.GetProperty(property.Key).SetValue(instance, value, new object[] { });
                } else
                    ObjectType.GetProperty(property.Key).SetValue(instance, property.Value, new object[] { });

            return instance;
        }

        // Alias Gen() to Create()
        public virtual object Gen()         { return Create();  }
        public virtual object Gen(object o) { return Create(o); }

        public virtual object Create() {
            var instance = Build();
            RunCreateAction(instance);
            return instance;
        }
        public virtual object Create(object overrides) {
            return Create(overrides.ToDictionary());
        }
        public virtual object Create(IDictionary<string,object> overrides) {
            var instance = Build(overrides);
            RunCreateAction(instance);
            return instance;
        }

        internal void RunCreateAction(object instance) {
            if (InstanceCreateAction != null)
                InstanceCreateAction.Invoke(instance);
            else if (InstanceCreateMethod != null)
                ObjectType.GetMethod(InstanceCreateMethod).Invoke(instance, new object[] { });
            else if (CreateAction != null)
                CreateAction.Invoke(instance);
            else if (CreateMethod != null)
                ObjectType.GetMethod(CreateMethod).Invoke(instance, new object[] { });
            else
                throw new Exception("Don't know how to Create().  Please set CreateAction or CreateMethod.");
        }
    }
}

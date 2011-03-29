using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FactoryMan.Generic {

    public class Factory<T> : FactoryMan.Factory {
        public static Factory<T> Define(object anonymousType) {
            return new Factory<T>(anonymousType);
        }

        new Dictionary<string, Func<T, object>> _funcProperties = new Dictionary<string, Func<T, object>>();
        new public Action<T> CreateAction { get; set; }

        new public Action<T> InstanceCreateAction { get; set; }

        public Factory() {
            base._typedFuncProperties = _funcProperties;
        }

        public Factory(string name) {
            base._typedFuncProperties = _funcProperties;
            Name = name;
        }

        public Factory(object anonymousType) {
            base._typedFuncProperties = _funcProperties;
            Add(anonymousType);
        }

        public virtual Factory<T> SetCreateMethod(string method) {
            InstanceCreateMethod = method;
            return this;
        }
        public virtual Factory<T> SetCreateAction(Action<T> action) {
            InstanceCreateAction = action;
            return this;
        }

        public override Type ObjectType { get { return typeof(T); } }

        public new Factory<T> Add(object anonymousType) {
            foreach (var property in anonymousType.ToDictionary())
                Add(property.Key, property.Value);
            return this;
        }

        public new Factory<T> Add(string propertyName, object propertyValue) {
            _properties.Add(propertyName, propertyValue);
            return this;
        }

        public Factory<T> Add(string propertyName, Func<T, object> propertyValue) {
            _funcProperties.Add(propertyName, propertyValue);
            return this;
        }

        public override object this[string propertyName] {
            get {
                var baseResult = base[propertyName];
                if (baseResult != null)
                    return baseResult;
                else
                    return _funcProperties[propertyName];
            }
        }

        public new Func<T, object> Func(string propertyName) {
            return _funcProperties[propertyName];
        }

        public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
            foreach (var keyValuePair in _properties)
                yield return keyValuePair;
            foreach (var keyValuePair in _funcProperties)
                yield return new KeyValuePair<string, object>(keyValuePair.Key, (object)keyValuePair.Value);
        }

        public new T Build() {
            return Build(null);
        }
        public new T Build(object overrides) {
            return Build(overrides.ToDictionary());
        }
        public new T Build(IDictionary<string,object> overrides) {
            var instance = Activator.CreateInstance<T>();
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
        public new T Gen()         { return Create(); }
        public new T Gen(object o) { return Create(o); }

        public new T Create() {
            var instance = Build();
            RunCreateAction(instance);
            return instance;
        }
        public new T Create(object overrides) {
            return Create(overrides.ToDictionary());
        }
        public new T Create(IDictionary<string,object> overrides) {
            var instance = Build(overrides);
            RunCreateAction(instance);
            return instance;
        }

        void RunCreateAction(T instance) {
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

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
        new public Action<T> GenerateAction { get; set; }

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
            var instance = Activator.CreateInstance<T>();
            var properties = Properties;

            if (overrides != null)
                foreach (var property in overrides.ToDictionary())
                    properties[property.Key] = property.Value;

            foreach (var property in properties)
                if (property.Value.GetType().FullName.StartsWith("System.Func")) { // call Invoke() if it's a Func
                    var value = property.Value.GetType().GetMethod("Invoke").Invoke(property.Value, new object[] { instance });
                    ObjectType.GetProperty(property.Key).SetValue(instance, value, new object[] { });
                } else
                    ObjectType.GetProperty(property.Key).SetValue(instance, property.Value, new object[] { });

            return instance;
        }

        // Alias Gen() to Generate()
        public new T Gen()         { return Generate(); }
        public new T Gen(object o) { return Generate(o); }

        public new T Generate() {
            var instance = Build();
            RunGenerateAction(instance);
            return instance;
        }

        public new T Generate(object overrides) {
            var instance = Build(overrides);
            RunGenerateAction(instance);
            return instance;
        }

        void RunGenerateAction(T instance) {
            try {
                base.RunGenerateAction(instance);
            } catch (Exception ex) {
                if (ex.Message.Contains("Don't know how to Generate") && GenerateAction != null)
                    GenerateAction(instance);
                else
                    throw ex;
            }
        }
    }
}
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using System.ComponentModel;
using System.Globalization;

namespace Maze
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class SettingAttribute : Attribute
    {
        public string Name { get; }

        public SettingAttribute(string name) => Name = name;
    }

    public class Vector3Converter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || destinationType == typeof(Vector3);
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || sourceType == typeof(Vector3);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (!CanConvertFrom(null, value.GetType()))
                throw new NotSupportedException();

            var vector = (Vector3)value;

            return $"({vector.X} {vector.Y} {vector.Z})";
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (!CanConvertTo(null, destinationType))
                throw new NotSupportedException();

            return Extensions.ParseToVector3((string)value);
        }
    }

    public class SettingsManager : IUpdateable, IDisposable
    {
        private readonly Dictionary<string, MemberInfo> _fields;
        private readonly FileSystemWatcher _watcher;
        private bool _update;
        private readonly Dictionary<Type, List<object>> _objects;
        private readonly FileStream _stream;

        public SettingsManager()
        {
            _fields = new Dictionary<string, MemberInfo>();

            foreach (var type in typeof(SettingsManager).Module.GetTypes())
            {
                foreach (var field in type.GetRuntimeFields())
                {
                    var attribute = Attribute.GetCustomAttribute(field, typeof(SettingAttribute)) as SettingAttribute;
                    if (attribute is not null)
                        _fields.Add(attribute.Name, field);
                }

                foreach (var property in type.GetRuntimeProperties())
                {
                    var attribute = Attribute.GetCustomAttribute(property, typeof(SettingAttribute)) as SettingAttribute;
                    if (attribute is not null)
                        _fields.Add(attribute.Name, property);
                }
            }

            _watcher = new FileSystemWatcher(@"..\..\..\", "settings.json")
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
            };
            _watcher.Changed += (sender, info) => { _update = true; };

            _stream = new FileStream(@"..\..\..\settings.json", FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            _objects = new Dictionary<Type, List<object>>();

            TypeDescriptor.AddAttributes(typeof(Vector3), new TypeConverterAttribute(typeof(Vector3Converter)));
        }

        public void Subscribe(object @object)
        {
            if (_objects.ContainsKey(@object.GetType()) && !_objects[@object.GetType()].Contains(@object))
                _objects[@object.GetType()].Add(@object);
            else
            {
                _objects.Add(@object.GetType(), new List<object>());
                _objects[@object.GetType()].Add(@object);
            }
        }

        public bool Unsubscribe(object @object)
        {
            if (_objects.ContainsKey(@object.GetType()))
                return _objects[@object.GetType()].Remove(@object);
            return false;
        }

        void IUpdateable.Begin() => _update = true;
        void IUpdateable.End() => Dispose();
        bool IUpdateable.Update(GameTime time)
        {
            if (_update)
            {
                Span<byte> bytes = stackalloc byte[(int)_stream.Length];
                _stream.Read(bytes);
                _stream.Position = 0;

                var reader = new Utf8JsonReader(bytes);
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var value = System.Text.Encoding.UTF8.GetString(reader.ValueSpan);

                        if (!_fields.ContainsKey(value))
                            continue;

                        var member = _fields[value];
                        if (!_objects.ContainsKey(member.DeclaringType))
                            continue;

                        foreach (var obj in _objects[member.DeclaringType])
                        {
                            reader.Read();

                            switch (member)
                            {
                                case FieldInfo field:
                                    {
                                        var converter = TypeDescriptor.GetConverter(field.FieldType);
                                        field.SetValue(obj, converter.ConvertTo(reader.GetString(), field.FieldType));
                                        break;
                                    }
                                case PropertyInfo property:
                                    {
                                        var converter = TypeDescriptor.GetConverter(property.PropertyType);
                                        property.SetValue(obj, converter.ConvertTo(reader.GetString(), property.PropertyType));
                                        break;
                                    }
                            }
                        }
                    }
                }

                _update = false;
            }

            return false;
        }

        ~SettingsManager() => Dispose();

        public void Dispose()
        {
            _watcher.Dispose();
            _stream.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
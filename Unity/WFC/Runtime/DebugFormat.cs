using Codice.CM.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace steph.Unity.WFC.Runtime
{
    /// <summary>
    /// General reflection-based "debug formatter", similar to Rust's {:?} / derive(Debug).
    /// Call DebugFormat.Debug(obj) to get a readable dump of any object's fields,
    /// including nested/polymorphic types like TileData subclasses.
    /// </summary>
    public static class DebugFormat
    {
        public static string Debug(object obj, int maxDepth = 4)
        {
            var visited = new HashSet<object>(RefEqualityComparer.Instance);
            return Format(obj, maxDepth, visited);
        }

        private static string Format(object obj, int depth, HashSet<object> visited)
        {
            if (obj == null) return "null";

            Type type = obj.GetType();

            if (obj is string str) return $"\"{str}\"";
            if (type.IsPrimitive || obj is decimal) return obj.ToString();
            if (type.IsEnum) return obj.ToString();

            // Unity objects tend to have huge/unsafe reflection surfaces (native handles etc.)
            // so just fall back to their own ToString().
            if (obj is UnityEngine.Object unityObj) return unityObj.ToString();

            if (obj is IEnumerable enumerable)
            {
                if (depth <= 0) return $"{type.Name}[...]";
                var items = enumerable.Cast<object>().Select(i => Format(i, depth - 1, visited));
                return $"[{string.Join(", ", items)}]";
            }

            if (!type.IsValueType)
            {
                if (!visited.Add(obj)) return $"{type.Name} {{ <cycle> }}";
            }

            if (depth <= 0) return $"{type.Name} {{ .. }}";

            var fields = type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => !f.IsDefined(typeof(CompilerGeneratedAttribute), false) || IsAutoPropertyBackingField(f));

            var sb = new StringBuilder();
            sb.Append(type.Name).Append(" { ");

            bool first = true;
            foreach (var field in fields)
            {
                object value;
                try { value = field.GetValue(obj); }
                catch { value = "<unreadable>"; }

                if (!first) sb.Append(", ");
                first = false;

                sb.Append(CleanFieldName(field.Name)).Append(": ").Append(Format(value, depth - 1, visited));
            }

            sb.Append(" }");
            return sb.ToString();
        }

        private static bool IsAutoPropertyBackingField(FieldInfo f) =>
            f.Name.EndsWith("k__BackingField", StringComparison.Ordinal);

        private static string CleanFieldName(string name)
        {
            // "<Name>k__BackingField" -> "Name"
            if (name.StartsWith("<", StringComparison.Ordinal))
            {
                int end = name.IndexOf('>');
                if (end > 0) return name.Substring(1, end - 1);
            }
            return name;
        }
    }

    /// <summary>
    /// Reference-identity comparer for use in HashSet/Dictionary, since Unity's
    /// runtime doesn't expose System.Collections.Generic.ReferenceEqualityComparer.
    /// </summary>
    public sealed class RefEqualityComparer : IEqualityComparer<object>
    {
        public static readonly RefEqualityComparer Instance = new();

        bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
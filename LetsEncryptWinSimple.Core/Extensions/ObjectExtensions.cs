using System;
using System.Collections.Generic;
using System.Reflection;
using LetsEncryptWinSimple.Core.Extensions.ArrayExtensions;

namespace LetsEncryptWinSimple.Core.Extensions
{
    //From https://github.com/Burtsev-Alexey/net-object-deep-copy
    public static class ObjectExtensions
    {
        private static readonly MethodInfo CloneMethod = typeof (object).GetMethod("MemberwiseClone",
            BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPrimitive(this Type type)
        {
            if (type == typeof (string))
                return true;
            return type.IsValueType & type.IsPrimitive;
        }

        public static object Copy(this object originalObject)
        {
            return InternalCopy(originalObject, new Dictionary<object, object>(new ReferenceEqualityComparer()));
        }

        private static object InternalCopy(object originalObject, IDictionary<object, object> visited)
        {
            if (originalObject == null)
                return null;
            var typeToReflect = originalObject.GetType();
            if (IsPrimitive(typeToReflect))
                return originalObject;
            if (visited.ContainsKey(originalObject))
                return visited[originalObject];
            if (typeof (Delegate).IsAssignableFrom(typeToReflect))
                return null;
            var cloneObject = CloneMethod.Invoke(originalObject, null);
            if (typeToReflect.IsArray)
            {
                var arrayType = typeToReflect.GetElementType();
                if (IsPrimitive(arrayType) == false)
                {
                    var clonedArray = (Array) cloneObject;
                    clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
                }
            }
            visited.Add(originalObject, cloneObject);
            CopyFields(originalObject, visited, cloneObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
            return cloneObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields(object originalObject,
            IDictionary<object, object> visited, object cloneObject, Type typeToReflect)
        {
            if (typeToReflect.BaseType == null)
                return;

            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
            CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType,
                BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
        }

        private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject,
            IReflect typeToReflect, BindingFlags bindingFlags =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy,
            Func<FieldInfo, bool> filter = null)
        {
            foreach (var fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                var originalFieldValue = fieldInfo.GetValue(originalObject);
                var clonedFieldValue = InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
            }
        }

        public static T Copy<T>(this T original)
        {
            return (T) Copy((object) original);
        }
    }

    public class ReferenceEqualityComparer : EqualityComparer<object>
    {
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public override int GetHashCode(object obj)
        {
            return obj == null ? 0 : obj.GetHashCode();
        }
    }

    namespace ArrayExtensions
    {
        public static class ArrayExtensions
        {
            public static void ForEach(this Array array, Action<Array, int[]> action)
            {
                if (array.LongLength == 0) return;
                var walker = new ArrayTraverse(array);

                do action(array, walker.Position);
                while (walker.Step());
            }
        }

        internal class ArrayTraverse
        {
            public int[] Position;
            private readonly int[] _maxLengths;

            public ArrayTraverse(Array array)
            {
                _maxLengths = new int[array.Rank];
                for (var i = 0; i < array.Rank; ++i)
                    _maxLengths[i] = array.GetLength(i) - 1;

                Position = new int[array.Rank];
            }

            public bool Step()
            {
                for (var i = 0; i < Position.Length; ++i)
                {
                    if (Position[i] >= _maxLengths[i])
                        continue;

                    Position[i]++;
                    for (var j = 0; j < i; j++)
                        Position[j] = 0;

                    return true;
                }
                return false;
            }
        }
    }
}
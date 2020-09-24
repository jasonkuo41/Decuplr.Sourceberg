using System;
using System.Collections.Generic;
using System.Linq;

namespace Decuplr {
    internal static class ReflectionTypeExtensions {

        /// <summary>
        /// Get's all the parent (declaring type) of this type, doesn't include itself
        /// </summary>
        public static IEnumerable<Type> GetDeclaringTypes(this Type type) {
            return UnwrapParentType(type).Reverse();

            static IEnumerable<Type> UnwrapParentType(Type type) {
                var parent = type.DeclaringType;
                while (parent is { }) {
                    yield return parent;
                    parent = parent.DeclaringType;
                }
            }
        }

        public static IReadOnlyList<int> GetArities(this Type type) {
            var typeWithParents = GetAllDeclaringTypesWithSelf(type);
            var layouts = new int[typeWithParents.Count];
            var currentSum = 0;
            for (var i = 0; i < typeWithParents.Count; ++i) {
                var length = typeWithParents[i].GetGenericArguments().Length - currentSum;
                layouts[i] = length;
                currentSum += length;
            }
            return layouts;

            static IReadOnlyList<Type> GetAllDeclaringTypesWithSelf(Type type) {
                var list = type.GetDeclaringTypes().ToList();
                list.Add(type);
                return list;
            }
        }

        public static bool ImplementsOrInherits(this Type source, Type target) {
            if (target.IsInterface)
                return source.GetInterfaces().Any(x => x.Equals(target));
            return source.IsSubclassOf(target);
        }

        public static bool ImplementsOrInherits<T>(this Type source) => source.ImplementsOrInherits(typeof(T));

    }
}
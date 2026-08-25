using System;
using System.Reflection;

namespace YShared.Console
{
    public readonly struct MethodSignature : IEquatable<MethodSignature>
    {
        public readonly string Name;
        public readonly Type[] ParameterTypes;
        private readonly int _hashCode;

        public MethodSignature(string name, Type[] parameterTypes)
        {
            Name = name;
            ParameterTypes = parameterTypes;

            unchecked
            {
                int hash = name.GetHashCode();

                foreach (Type type in parameterTypes)
                    hash = (hash * 31) + type.GetHashCode();

                _hashCode = hash;
            }
        }

        public bool Equals(MethodSignature other)
        {
            if (_hashCode != other._hashCode)
                return false;

            if (!string.Equals(Name, other.Name, StringComparison.Ordinal))
                return false;

            if (ParameterTypes.Length != other.ParameterTypes.Length)
                return false;

            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                if (ParameterTypes[i] != other.ParameterTypes[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj) => obj is MethodSignature other && Equals(other);

        public override int GetHashCode() => _hashCode;

        public static MethodSignature Create(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();

            Type[] types = new Type[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                types[i] = parameters[i].ParameterType;

            return new MethodSignature(method.Name, types);
        }
    }
}
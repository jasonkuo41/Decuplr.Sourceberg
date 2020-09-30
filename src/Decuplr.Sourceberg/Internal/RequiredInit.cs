using System;
using System.Threading;

namespace Decuplr.Sourceberg.Internal {
    /// <summary>
    /// Requires a field to be initialized afterwards for the value to be correctly consumed 
    /// </summary>
    /// <typeparam name="T">The type to be initialized with.</typeparam>
    internal struct RequiredInit<T> where T : class {

        private readonly string _name;
        private T? _required;

        public bool IsInitialized => _required is null;

        public T Value 
        {
            get => _required ?? throw new InvalidOperationException($"Value '{_name}' (Type: {typeof(T)}) was not initialized and thus cannot be used");
            set => Interlocked.Exchange(ref _required, value.ThrowIfNull(nameof(value)));
        }

        public RequiredInit(string name) {
            _name = name;
            _required = null;
        }

        private RequiredInit(T item) {
            _name = string.Empty;
            _required = item;
        }

        public static implicit operator T (RequiredInit<T> item) => item.Value;
        public static implicit operator RequiredInit<T>(T item) => new RequiredInit<T>(item.ThrowIfNull(nameof(item)));
    }
}

using SimpleRelm.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.RelmInternal.Helpers.Operations
{
    internal class FieldLoaderRegistry : IEnumerable<IRelmFieldLoader>
    {
        private readonly Dictionary<string, IRelmFieldLoader> _fieldDataLoaders = new Dictionary<string, IRelmFieldLoader>();

        public IEnumerator<IRelmFieldLoader> GetEnumerator()
        {
            return _fieldDataLoaders.Values?.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Determines if a field loader has been registered for the given field name.
        /// </summary>
        /// <param name="fieldName">The field name to search for.</param>
        /// <returns>Whether the provided field has a field loader.</returns>
        public bool HasFieldLoader(string fieldName)
        {
            return _fieldDataLoaders.ContainsKey(fieldName);
        }

        /// <summary>
        /// Register a field loader for the given field name.
        /// </summary>
        /// <param name="fieldName">The field name to register the loader for. Passing a null with a previously registered field name removes that field loader from the
        /// field loader list.</param>
        /// <param name="loader">The loader implementation to register, or null to remove.</param>
        /// <returns>The field loader that was successfully registered, or null if one was removed.</returns>
        /// <exception cref="ArgumentException">Throws when passing a null loader with an invalid field name.</exception>
        public IRelmFieldLoader RegisterFieldLoader(string fieldName, IRelmFieldLoader loader)
        {
            if (loader == null)
            {
                if (_fieldDataLoaders.ContainsKey(fieldName))
                    _fieldDataLoaders.Remove(fieldName);
                else
                    throw new ArgumentException($"The field {fieldName} does not have a data loader set");

                return null;
            }
            else
            {
                if (!_fieldDataLoaders.ContainsKey(fieldName))
                    _fieldDataLoaders.Add(fieldName, loader);
                else
                    _fieldDataLoaders[fieldName] = loader;

                return _fieldDataLoaders[fieldName];
            }
        }

        /// <summary>
        /// Get the field loader for the given field name, if it exists.
        /// </summary>
        /// <param name="fieldName">The field name to get the loader for.</param>
        /// <returns>The loader for the provided field name, or null if none exists.</returns>
        public IRelmFieldLoader GetFieldLoader(string fieldName)
        {
            _fieldDataLoaders.TryGetValue(fieldName, out var loader);

            return loader;
        }
    }
}

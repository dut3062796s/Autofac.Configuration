﻿using Autofac.Configuration.Util;
using Autofac.Core;
using Microsoft.Framework.ConfigurationModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Autofac.Configuration.Core
{
    /// <summary>
    /// Extension methods for working with <see cref="IConfiguration"/>.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Reads the default assembly information from configuration and
        /// parses the assembly name into an assembly.
        /// </summary>
        /// <param name="configuration">
        /// An <see cref="IConfiguration"/> from which the default assembly
        /// should be read.
        /// </param>
        /// <returns>
        /// An <see cref="Assembly"/> if the default assembly is specified on
        /// <paramref name="configuration"/>; or <see langword="null"/> if not.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="configuration"/> is <see langword="null"/>.
        /// </exception>
        public static Assembly DefaultAssembly(this IConfiguration configuration)
        {
            return configuration.GetAssembly("defaultAssembly");
        }

        /// <summary>
        /// Reads an assembly name from configuration and parses the assembly name into an assembly.
        /// </summary>
        /// <param name="configuration">
        /// An <see cref="IConfiguration"/> from which the default assembly
        /// should be read.
        /// </param>
        /// <param name="key">
        /// The <see cref="String"/> key in configuration where the assembly name
        /// is specified.
        /// </param>
        /// <returns>
        /// An <see cref="Assembly"/> if the assembly is specified on
        /// <paramref name="configuration"/>; or <see langword="null"/> if not.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="configuration"/> or <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="key"/> is empty or whitespace.
        /// </exception>
        public static Assembly GetAssembly(this IConfiguration configuration, string key)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (String.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, ConfigurationResources.ArgumentMayNotBeEmpty, "configuration key"), "key");
            }

            var assemblyName = configuration.Get(key);
            if (String.IsNullOrWhiteSpace(assemblyName))
            {
                return null;
            }

            return Assembly.Load(new AssemblyName(assemblyName));
        }

        public static IEnumerable<Parameter> GetParameters(this IConfiguration configuration, string key)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (String.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, ConfigurationResources.ArgumentMayNotBeEmpty, "configuration key"), "key");
            }

            foreach (var parameterElement in configuration.GetSubKeys(key).Select(kvp => kvp.Value))
            {
                // TODO: Try list, then dictionary, then value - look at CoerceValue in ParameterElement
                // TODO: Get a copy of the parameter value in the closure.
                object parameterValue = null;
                var parameterName = parameterElement.Get("name");
                yield return new ResolvedParameter(
                    (pi, c) => pi.Name == parameterName,
                    (pi, c) => TypeManipulation.ChangeToCompatibleType(parameterValue, pi.ParameterType, pi));
            }
        }

        public static object ConvertToList(IConfiguration listConfiguration, Type destinationType)
        {
            var instantiatableType = GetInstantiableListType(destinationType);
            if(instantiatableType == null)
            {
                return null;
            }

            var genericItemType = instantiatableType.GetGenericArguments()[0];
            var collection = (IList)Activator.CreateInstance(instantiatableType);
            foreach (var item in listConfiguration.GetSubKeys("item").Select(kvp => kvp.Value).SelectMany(c => c.Get("value")))
            {
                collection.Add(TypeManipulation.ChangeToCompatibleType(item, genericItemType));
            }
            return collection;
        }

        private static Type GetInstantiableListType(Type destinationType)
        {
            if (typeof(IEnumerable).IsAssignableFrom(destinationType))
            {
                var generics = destinationType.IsConstructedGenericType ? destinationType.GetGenericArguments() : new[] { typeof(object) };
                if (generics.Length != 1)
                {
                    return null;
                }

                var listType = typeof(List<>).MakeGenericType(generics);

                if (destinationType.IsAssignableFrom(listType))
                {
                    return listType;
                }
            }

            return null;
        }


        public static IEnumerable<Parameter> GetProperties(this IConfiguration configuration, string key)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (String.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, ConfigurationResources.ArgumentMayNotBeEmpty, "configuration key"), "key");
            }

            foreach (var propertyElement in configuration.GetSubKeys(key).Select(kvp => kvp.Value))
            {
                // TODO: Try list, then dictionary, then value - look at CoerceValue in ParameterElement
                // TODO: Get a copy of the parameter value in the closure.
                object parameterValue = null;
                var parameterName = propertyElement.Get("name");
                yield return new ResolvedParameter(
                    (pi, c) =>
                    {
                        PropertyInfo prop;
                        return pi.TryGetDeclaringProperty(out prop) &&
                            prop.Name == parameterName;
                    },
                    (pi, c) =>
                    {
                        PropertyInfo prop = null;
                        pi.TryGetDeclaringProperty(out prop);
                        return TypeManipulation.ChangeToCompatibleType(parameterValue, pi.ParameterType, prop);
                    });
            }
        }
    }
}
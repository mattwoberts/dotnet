﻿namespace StackExchange.Profiling.EntityFramework6
{
    using System;
    using System.Collections.Concurrent;
    using System.Data.Entity;
    using System.Data.Entity.Core.Common;
    using System.Reflection;

    using StackExchange.Profiling.Data;

    /// <summary>
    /// Provides helper methods to help with initializing the MiniProfiler for Entity Framework 6.
    /// </summary>
    public static class MiniProfilerEF6
    {
        /// <summary>
        /// A cache so we don't have to do reflection every time someone asks for the MiniProfiler implementation for a DB Provider.
        /// </summary>
        private static readonly ConcurrentDictionary<DbProviderServices,DbProviderServices> ProviderCache = new ConcurrentDictionary<DbProviderServices, DbProviderServices>(); 

        /// <summary>
        /// Registers the WrapProviderService method with the Entity Framework 6 DbConfiguration as a replacement service for DbProviderServices.
        /// </summary>
        public static void Initialize()
        {
            DbConfiguration.Loaded += (_, a) => a.ReplaceService<DbProviderServices>(
                (services, o) => WrapProviderService(services));
        }

        /// <summary>
        /// Wraps the provided DbProviderServices class in a MiniProfiler profiled DbService and returns the wrapped service.
        /// </summary>
        /// <param name="services">The DbProviderServices service to wrap.</param>
        /// <returns>A wrapped version of the DbProviderService service.</returns>
        private static DbProviderServices WrapProviderService(DbProviderServices services)
        {
            // first let's see if our type is already wrapped.
            var serviceType = services.GetType();
            while (serviceType != null)
            {
                if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(EFProfiledDbProviderServices<>))
                {
                    return services;
                }

                serviceType = serviceType.BaseType;
            }

            // Then let's check our cache.
            if (ProviderCache.ContainsKey(services))
            {
                return ProviderCache[services];
            }

            // Let's wrap it.
            var genericType = typeof(EFProfiledDbProviderServices<>);
            Type[] typeArgs = { services.GetType() };
            var createdType = genericType.MakeGenericType(typeArgs);
            var instanceProperty = createdType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceProperty.GetValue(null) as DbProviderServices;
            ProviderCache[services] = instance;
            return instance;
        }
    }
}
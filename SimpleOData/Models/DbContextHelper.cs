using Microsoft.AspNet.OData.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleOData.Models
{
    /// <summary>
    /// DbContext management handy functions
    /// </summary>
    public static class DbContextHelper
    {
        /// <summary>
        /// Build simple EdmModel from provided DbContext
        /// </summary>
        /// <param name="ctx">Instance of DbContext</param>
        /// <returns>EdmModel</returns>
        public static IEdmModel BuildEdmModel(DbContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException("ctx");

            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();

            foreach (var dbSet in ctx.GetType().GetProperties())
            {
                // Only looking for DbSet<TEntity> properties
                if (dbSet.PropertyType.IsGenericType)
                {
                    System.Type setType = dbSet.PropertyType.GenericTypeArguments[0];

                    var entityType = builder.AddEntityType(setType);

                    builder.AddEntitySet(setType.Name, entityType);
                }
            }

            return builder.GetEdmModel();
        }

        /// <summary>
        /// Initialize object properties using seed as a starting point.
        /// Following property types supported:
        /// - string
        /// - int
        /// - DateTimeOffset?
        /// Exception will be thrown for unsupported property types
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="obj">Object instance</param>
        /// <param name="seed">Seed value</param>
        /// <returns>Original obj with updated properties</returns>
        public static T InitObject<T>(T obj, int seed)
        {
            if (obj == null) throw new ArgumentNullException("obj");

            foreach (var prop in obj.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(obj, prop.Name + seed);
                }
                else
                if (prop.PropertyType == typeof(int))
                {
                    prop.SetValue(obj, seed);
                }
                else
                if (prop.PropertyType == typeof(DateTimeOffset?))
                {
                    prop.SetValue(obj, new DateTimeOffset(new DateTime(seed, 1, 1).Date));
                }
                else
                    throw new Exception("Unknown type " + prop.PropertyType);
            }

            return obj;
        }

        /// <summary>
        /// Insert test records into provided DbSet 
        /// </summary>
        /// <typeparam name="T">Type of the desired DbSet entity</typeparam>
        /// <param name="context">Context instance</param>
        /// <param name="seedCount">Number of records to seed</param>
        /// <returns>Task</returns>
        public static async Task PopulateTestDbSetAsync<T>(DbContext context, int seedCount)
        {
            if (context == null) throw new ArgumentNullException("context");

            for (int ind = 1; ind <= seedCount; ind++)
            {
                T obj = (T)Activator.CreateInstance(typeof(T));
                await context.AddAsync(InitObject(obj, ind));
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Insert records from url into provided DbSet 
        /// </summary>
        /// <typeparam name="T">Type of the desired DbSet entity</typeparam>
        /// <param name="context">Context instance</param>
        /// <param name="url">url which returns mock values</param>
        public static async Task PopulateDbAsync<T>(DbContext context, string url)
            where T : class
        {
            if (context == null) throw new ArgumentNullException("context");

            var removeItems = await context.Set<T>().ToArrayAsync();

            context.RemoveRange(removeItems);

            using (HttpClient client = new HttpClient())
            using (Stream stream = await client.GetStreamAsync(url))
            using (StreamReader sr = new StreamReader(stream))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                JsonSerializer serializer = new JsonSerializer();

                var items = serializer.Deserialize<T[]>(reader);

                foreach (var item in items)
                    await context.AddAsync(item);
            }

            await context.SaveChangesAsync();
        }
    }
}

using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SimpleOData.Controllers
{
    /// <summary>
    /// Ideas from http://blog.scottlogic.com/2015/12/01/generalizing-odata.html    
    /// </summary>
    /// <typeparam name="TKey">Type of key field of the Entity object.</typeparam>
    /// <typeparam name="TEntity">Entity object MUST have *single* key of type TKey.</typeparam>
    /// <typeparam name="TContext">DbContext were Entity object belongs to</typeparam>
    [ThrottleActionFilterAttribute]
    public class GenericODataCRUDController<TKey, TEntity, TContext> : ODataController
        where TEntity : class
        where TContext : DbContext
    {
        #region Error messages
        public const string errorDuplicateKey = "Duplicate key ";
        public const string errorKeyMismatch = "Uri key not matching with key extracted from body";

        #endregion

        #region Protected members

        protected readonly TContext _ctx;
        protected DbSet<TEntity> _table;

        protected TKey GetKey(TEntity entity)
        {
            var keyName = ctx.Model
                .FindEntityType(typeof(TEntity))
                .FindPrimaryKey()
                .Properties
                .Select(x => x.Name)
                .Single();

            return (TKey)entity.GetType().GetProperty(keyName).GetValue(entity, null);
        }

        protected DbSet<TEntity> TableForT()
        {
            return ctx.Set<TEntity>();
        }

        protected bool Exists(TKey key)
        {
            var entity = table.Find(key);

            return (entity != null);
        }

        #endregion

        #region Public members 

        public DbSet<TEntity> table => _table ?? (_table = TableForT());
        public TContext ctx => _ctx;

        public GenericODataCRUDController(TContext context)
        {
            _ctx = context;
        }

        #endregion

        #region CRUD

        // E.g. GET http://localhost/Products
        [EnableQuery]
        public IQueryable<TEntity> Get()
        {
            return table.AsNoTracking();
        }

        // E.g. GET http://localhost/Products/1
        [EnableQuery]
        public IActionResult Get([FromODataUri] TKey key)
        {
            var result = table.Find(key);

            if (result != null)
                return Ok(result);
            else
                return NotFound();
        }

        // E.g. POST http://localhost/Products
        public async Task<IActionResult> Post([FromBody] TEntity obj)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            table.Add(obj);

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (ArgumentException)
            {
                TKey key = GetKey(obj);
                if (Exists(key))
                {
                    return BadRequest(new { message = errorDuplicateKey + key.ToString() });
                }
                else
                {
                    throw;
                }
            }

            return Created(obj);
        }

        // E.g. PATCH http://localhost/Products/1
        public async Task<IActionResult> Patch([FromODataUri] TKey key, Delta<TEntity> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await table.FindAsync(key);
            if (entity == null)
            {
                return NotFound();
            }

            delta.Patch(entity);

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Exists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(entity);
        }

        // E.g. PUT http://localhost/Products/1
        public async Task<IActionResult> Put([FromODataUri] TKey key, [FromBody] TEntity obj)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            TKey objKey = GetKey(obj);

            if (!key.Equals(objKey))
            {
                return BadRequest(new { message = errorKeyMismatch });
            }

            ctx.Entry(obj).State = EntityState.Modified;

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Exists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(obj);
        }

        // E.g. DELETE http://localhost/Products/1
        public async Task<IActionResult> Delete([FromODataUri] TKey key)
        {
            var entity = await table.FindAsync(key);

            if (entity == null)
            {
                return NotFound();
            }

            table.Remove(entity);

            await ctx.SaveChangesAsync();

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        #endregion
    }
}

using SimpleOData.Models;

namespace SimpleOData.Controllers
{
    /// <summary>
    /// Implements Person OData Controller
    /// Can be easilly created using reflections if needed
    /// </summary>
    public class PersonController : GenericODataCRUDController<int, Person, PeopleContext>
    {
        public PersonController(PeopleContext ctx) : base(ctx) { }
    }
}
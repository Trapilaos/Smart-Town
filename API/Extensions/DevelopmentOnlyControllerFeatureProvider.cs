using API.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace API.Extensions
{
    /// <summary>
    /// Removes controllers that must only exist in the Development environment (e.g. BuggyController).
    /// </summary>
    public class DevelopmentOnlyControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private static readonly Type[] DevelopmentOnly = { typeof(BuggyController) };

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var type in DevelopmentOnly)
            {
                var existing = feature.Controllers.FirstOrDefault(t => t.AsType() == type);
                if (existing != null) feature.Controllers.Remove(existing);
            }
        }
    }
}

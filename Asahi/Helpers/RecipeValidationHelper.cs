using Asahi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asahi.Helpers
{
    public static class RecipeValidationHelper
    {
        public static Dictionary<string, Type> GetExpectedTypes()
        {
            return typeof(DefaultRecipeValuesModel).GetProperties().ToDictionary(p => p.Name, p => p.PropertyType);
        }
    }
}

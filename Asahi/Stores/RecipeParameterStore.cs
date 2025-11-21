using Asahi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asahi.Stores
{
    public class RecipeParameterStore
    {
        public Dictionary<string, object> CurrentParameters { get; private set; } = new();

        public event Action? ParametersUpdated;

        public void SetParameters(IEnumerable<RecipeParameterModel> parameters)
        {
            CurrentParameters = parameters.ToDictionary(
                p => p.ParameterName,
                p => string.IsNullOrWhiteSpace(p.NewValue) ? p.Value : p.ParsedValue
                );
            ParametersUpdated?.Invoke();
        }
    }
}

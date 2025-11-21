using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asahi.Stores
{
    public class RecipeStore
    {
        public event Action? RecipeChanged;

        public void OnRecipeChanged()
        {
            RecipeChanged?.Invoke();
        }

        private string? currentRecipe;

        public string CurrentRecipe
        {
            get
            {
                return currentRecipe;
            }
            set
            {
                currentRecipe = value;
                OnRecipeChanged();
            }
        }
    }
}

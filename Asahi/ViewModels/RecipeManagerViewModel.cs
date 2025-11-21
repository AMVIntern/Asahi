using Asahi.Base;
using Asahi.Helpers;
using Asahi.Models;
using Asahi.Stores;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Asahi.Stores;
using System.Collections.ObjectModel;
using System.IO;
using Asahi.DataServices;
using System.Windows;
using Newtonsoft.Json;
using System.Diagnostics;
using Asahi.Helpers;

namespace Asahi.ViewModels
{
    public partial class RecipeManagerViewModel : ViewModelBase, IDisposable
    {
        private readonly JSONDataService _jsonDataService;
        private readonly RecipeStore _recipeStore;
        private readonly DefaultRecipeValuesModel _defaultRecipeValuesModel;
        private readonly RecipeParameterStore _recipeParameterStore;
        private readonly ModalStore _modalStore;
        private string _currentLoadedRecipe = "";
        private Dictionary<string, Type> _expectedTypes;
        private AppConfigModel _appConfig;
        public ObservableCollection<RecipeParameterModel> RecipeParameters { get; set; } = new ObservableCollection<RecipeParameterModel>();

        [ObservableProperty]
        public string tempRecipeSelected;

        [ObservableProperty]
        public string currentRecipe;

        [ObservableProperty]
        public ObservableCollection<string> recipesCollection = new ObservableCollection<string>();

        [ObservableProperty]
        public bool isDefaultRecipeLoaded;

        public NavigationBarViewModel? NavigationBarViewModel { get; set; }
        public DefaultRecipeValuesModel RecipeValues => _defaultRecipeValuesModel;
        public RecipeManagerViewModel(JSONDataService jsonDataService, RecipeStore recipeStore, DefaultRecipeValuesModel defaultRecipeValuesModel, RecipeParameterStore parameterStore, ModalStore modalStore)
        {
            AppLogger.Info("Recipe Manager Initializing..");
            _jsonDataService = jsonDataService;
            _recipeStore = recipeStore;
            _defaultRecipeValuesModel = defaultRecipeValuesModel;
            _recipeParameterStore = parameterStore;
            _modalStore = modalStore;
            _expectedTypes = RecipeValidationHelper.GetExpectedTypes();
            _recipeParameterStore.ParametersUpdated += OnRecipeParametersUpdated;
            AppLogger.Info("Recipe Manager Initialized!");
        }

        public async Task InitializeAsync()
        {
            await EnsureDefaultRecipeExistsAsync();
            await UpdateRecipeCollectionAsync();

            _appConfig = await _jsonDataService.LoadAppConfigAsync();

            if (RecipesCollection.Contains(_appConfig.CurrentRecipe))
            {
                await LoadRecipeValuesAsync(_appConfig.CurrentRecipe);
            }
            else
            {
                await LoadRecipeValuesAsync("Default");
                _appConfig.CurrentRecipe = "Default";
                await _jsonDataService.SaveAppConfigAsync(_appConfig);
            }

            CurrentRecipe = _recipeStore.CurrentRecipe = _appConfig.CurrentRecipe;
            _recipeStore.RecipeChanged += RecipeStore_RecipeChanged;
        }

        private async Task EnsureDefaultRecipeExistsAsync()
        {
            string defaultRecipePath = Path.Combine(_jsonDataService._recipesFolderPath, "Default.json");
            if (!File.Exists(defaultRecipePath))
            {
                Dictionary<string, object> defaultValues = new();

                var properties = typeof(DefaultRecipeValuesModel).GetProperties();

                foreach (var prop in properties)
                {
                    string name = prop.Name;
                    object value = prop.GetValue(_defaultRecipeValuesModel);

                    defaultValues[name] = value;
                }

                try
                {
                    await _jsonDataService.SaveRecipeAsync("Default", defaultValues);
                    AppLogger.Info("Default recipe has been created dynamically.");
                }
                catch (IOException ex)
                {
                    _modalStore.ShowModal(new MessageModalViewModel("Invalid Input!", "Some values are invalid. Please fix them before saving.", _modalStore));
                    AppLogger.Error("Some values in the recipe were invalid.", ex);
                }
            }
        }

        private void RecipeStore_RecipeChanged()
        {
            CurrentRecipe = _recipeStore.CurrentRecipe;
        }

        [RelayCommand]
        public async Task SaveRecipe()
        {
            if (string.IsNullOrEmpty(_currentLoadedRecipe))
            {
                _modalStore.ShowModal(new MessageModalViewModel("Error!", "No recipe is currently loaded.", _modalStore));
                AppLogger.Info("No recipe was loaded and save button was clicked.");
                return;
            }

            if (RecipeParameters.Any(p => !p.IsValid))
            {
                _modalStore.ShowModal(new MessageModalViewModel("Invalid Input!", "Some values are invalid. Please fix them before saving.", _modalStore));
                AppLogger.Info("Some values in the recipe were invalid.");
                return;
            }

            bool hasChanges = RecipeParameters.Any(p => !string.IsNullOrWhiteSpace(p.NewValue) && p.NewValue != p.Value);

            if (!hasChanges)
            {
                _modalStore.ShowModal(new MessageModalViewModel("Nothing Changed!", "No changes to save.", _modalStore));
                AppLogger.Info("There were no changes to save.");
                return;
            }

            var updatedParameters = await _jsonDataService.LoadRecipeAsync(_currentLoadedRecipe) ?? new Dictionary<string, object>();

            foreach (var param in RecipeParameters)
            {

                if (string.IsNullOrWhiteSpace(param.NewValue))
                {
                    updatedParameters[param.ParameterName] = param.Value;
                }
                else
                {
                    updatedParameters[param.ParameterName] = param.ParsedValue;
                }
            }

            try
            {
                await _jsonDataService.SaveRecipeAsync(_currentLoadedRecipe, updatedParameters);
                _modalStore.ShowModal(new MessageModalViewModel("Recipe Saved!", $"Parameters saved for recipe: {_currentLoadedRecipe}", _modalStore));
                AppLogger.Info($"Parameters saved for recipe: {_currentLoadedRecipe}");
            }
            catch (IOException ex)
            {
                _modalStore.ShowModal(new MessageModalViewModel("Save Error!", $"Failed to save recipe: {ex.Message}", _modalStore));
                AppLogger.Error($"Failed to save recipe: ", ex);
            }

            await LoadRecipeValuesAsync(_currentLoadedRecipe);
        }

        [RelayCommand]
        public async Task LoadRecipe()
        {
            if (string.IsNullOrEmpty(TempRecipeSelected))
            {
                _modalStore.ShowModal(new MessageModalViewModel("No Recipe Selected!", message: "No recipe selected for loading.", _modalStore));
                AppLogger.Info("No recipe selected.");
                return;
            }

            await LoadRecipeValuesAsync(TempRecipeSelected);
            _recipeStore.CurrentRecipe = TempRecipeSelected;
            _appConfig.CurrentRecipe = TempRecipeSelected;
            await _jsonDataService.SaveAppConfigAsync(_appConfig);
            _modalStore.ShowModal(new MessageModalViewModel("Recipe Loaded!", message: $"{TempRecipeSelected} has been loaded.", _modalStore));
            AppLogger.Info($"{TempRecipeSelected} has been loaded.");
        }

        [RelayCommand]
        public async Task DeleteRecipe()
        {
            if (string.IsNullOrEmpty(TempRecipeSelected))
            {
                _modalStore.ShowModal(new MessageModalViewModel("No Recipe Selected!", message: "No recipe selected for deletion.", _modalStore));
                AppLogger.Info("No recipe selected for deletion.");
                return;
            }

            if (TempRecipeSelected.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                _modalStore.ShowModal(new MessageModalViewModel("Cannot Delete!", "You cannot delete the default recipe.", _modalStore));
                AppLogger.Info("User tried to delete the default recipe.");
                return;
            }

            string filePath = Path.Combine(_jsonDataService._recipesFolderPath, TempRecipeSelected + ".json");

            if (File.Exists(filePath))
            {
                string recipeToDelete = TempRecipeSelected;
                bool confirm = await _modalStore.ShowConfirmationAsync("Confirm Delete?", $"Are you sure you want to delete {TempRecipeSelected}?");
                if (confirm != true)
                {
                    return;
                }

                File.Delete(filePath);
                _modalStore.ShowModal(new MessageModalViewModel("Recipe Deleted!", $"{TempRecipeSelected} has been deleted.", _modalStore));
                AppLogger.Info($"Deleted recipe: {TempRecipeSelected}");

                if (_currentLoadedRecipe.Equals(recipeToDelete, StringComparison.OrdinalIgnoreCase))
                {
                    await LoadRecipeValuesAsync("Default");
                    _appConfig.CurrentRecipe = "Default";
                    await _jsonDataService.SaveAppConfigAsync(_appConfig);
                    _recipeStore.CurrentRecipe = "Default";
                    _modalStore.ShowModal(new MessageModalViewModel("Recipe Deleted!", $"{TempRecipeSelected} has been deleted and Default has been loaded.", _modalStore));
                }

                RecipesCollection.Remove(TempRecipeSelected);

            }
            else
            {
                AppLogger.Info($"Recipe {TempRecipeSelected} does not exist.");
            }
        }

        [RelayCommand]
        public async Task AddRecipe()
        {
            if (string.IsNullOrWhiteSpace(TempRecipeSelected))
            {
                _modalStore.ShowModal(new MessageModalViewModel("No Recipe Selected!", message: "Please highlight a recipe to use as template.", _modalStore));
                return;
            }

            var result = await _modalStore.ShowTextInputAsync("New Recipe!", "Enter the name of the new recipe: ");

            if (result.WasConfirmed == false)
            {
                return;
            }

            string newRecipeName = result.Value;

            if (string.IsNullOrWhiteSpace(newRecipeName))
            {
                _modalStore.ShowModal(new MessageModalViewModel("Invalid Name!", message: "Recipe name cannot be empty.", _modalStore));
                AppLogger.Info("Recipe name was empty.");
                return;
            }

            var templateData = await _jsonDataService.LoadRecipeAsync(TempRecipeSelected);
            if (templateData == null || templateData.Count == 0)
            {
                _modalStore.ShowModal(new MessageModalViewModel("Load Error!", message: "The selected recipe could not be loaded.", _modalStore));
                AppLogger.Info($"The selected recipe {TempRecipeSelected} could not be laoded.");
                return;
            }

            var newParams = templateData.Select(kvp => new RecipeParameterModel
            {
                ParameterName = kvp.Key,
                Value = kvp.Value,
                NewValue = ""
            });

            try
            {
                await _jsonDataService.CreateRecipeAsync(newRecipeName, newParams);
                await UpdateRecipeCollectionAsync();
                _modalStore.ShowModal(new MessageModalViewModel("Recipe Created!", $"{newRecipeName} has been created.", _modalStore));
                AppLogger.Info($"{newRecipeName} has been created.");
            }
            catch (IOException ex)
            {
                _modalStore.ShowModal(new MessageModalViewModel("Recipe Error!", message: "A recipe with that name already exists.", _modalStore));
                AppLogger.Error("A recipe with the entered name already exists.", ex);
                return;
            }
        }

        public async Task UpdateRecipeCollectionAsync()
        {
            var recipes = await Task.Run(() => _jsonDataService.GetAllRecipes());
            var expectedKeys = _expectedTypes.Keys.ToHashSet();
            var validRecipes = new List<string>();
            var invalidRecipes = new List<string>();

            foreach (var recipe in recipes)
            {
                var recipeData = await _jsonDataService.LoadRecipeAsync(recipe);

                if (recipeData == null)
                {
                    continue;
                }

                bool isValid = true;

                var recipeKeys = recipeData.Keys.ToHashSet();

                if (recipeKeys.SetEquals(expectedKeys) == false)
                {
                    isValid = false;
                }
                else
                {
                    foreach (var kvp in recipeData)
                    {
                        if (_expectedTypes.TryGetValue(kvp.Key, out var expectedType))
                        {
                            try
                            {
                                Convert.ChangeType(kvp.Value, expectedType);
                            }
                            catch
                            {
                                isValid = false;
                                break;
                            }
                        }
                    }
                }

                if (isValid)
                {
                    validRecipes.Add(recipe);
                }
                else
                {
                    if (recipe == "Default")
                    {
                        await RegenerateDefaultRecipeAsync();
                        validRecipes.Add("Default");
                    }
                    else
                    {
                        invalidRecipes.Add(recipe);
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                RecipesCollection.Clear();
                foreach (var recipe in validRecipes)
                {
                    RecipesCollection.Add(recipe);
                    AppLogger.Info($"Recipe Loaded: {recipe}");
                }
            });

            if (invalidRecipes.Count > 0)
            {
                string message = "The following recipes contain values with incorrect types and have been skipped:\n\n" + string.Join("\n", invalidRecipes);
                ModalHelper.ShowDeferred(_modalStore, "Invalid Recipes Skipped!", message);
            }
        }

        private async Task RegenerateDefaultRecipeAsync()
        {
            string defaultRecipePath = Path.Combine(_jsonDataService._recipesFolderPath, "Default.json");
            File.Delete(defaultRecipePath);
            await EnsureDefaultRecipeExistsAsync();
        }

        private async Task LoadRecipeValuesAsync(string recipeName)
        {
            var recipeData = await _jsonDataService.LoadRecipeAsync(recipeName);

            if (recipeData != null)
            {
                _currentLoadedRecipe = recipeName;
                //IsDefaultRecipeLoaded = string.Equals(recipeName, "Default", StringComparison.OrdinalIgnoreCase);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    RecipeParameters.Clear();
                    foreach (var entry in recipeData)
                    {
                        object typedValue = entry.Value;
                        if (_expectedTypes.TryGetValue(entry.Key, out var expectedType))
                        {
                            try
                            {
                                typedValue = Convert.ChangeType(entry.Value, expectedType);
                            }
                            catch
                            {
                                Debug.WriteLine($"Failed to convert {entry.Key} to {expectedType}.");
                            }

                            RecipeParameters.Add(new RecipeParameterModel
                            {
                                ParameterName = entry.Key,
                                Value = typedValue,
                                NewValue = ""
                            });
                        }
                    }
                });
            }

            _recipeParameterStore.SetParameters(RecipeParameters);
        }

        private void OnRecipeParametersUpdated()
        {
            LoadRecipeValues();
        }
        public void LoadRecipeValues()
        {
            try
            {
                var currentParameters = _recipeParameterStore.CurrentParameters;

                if (currentParameters != null && currentParameters.Count > 0)
                {
                    string json = JsonConvert.SerializeObject(currentParameters);
                    var recipeValues = JsonConvert.DeserializeObject<DefaultRecipeValuesModel>(json);

                    if (recipeValues != null)
                    {
                        // Update the existing instance properties
                        _defaultRecipeValuesModel.ShieldRegionRow1 = recipeValues.ShieldRegionRow1;
                        _defaultRecipeValuesModel.ShieldRegionCol1 = recipeValues.ShieldRegionCol1;
                        _defaultRecipeValuesModel.ShieldRegionRow2 = recipeValues.ShieldRegionRow2;
                        _defaultRecipeValuesModel.ShieldRegionCol2 = recipeValues.ShieldRegionCol2;
                        _defaultRecipeValuesModel.ShieldRegionScaleMult = recipeValues.ShieldRegionScaleMult;
                        _defaultRecipeValuesModel.ShieldRegionScaleAdd = recipeValues.ShieldRegionScaleAdd;

                        AppLogger.Info($"Successfully loaded recipe parameters from RecipeParameterStore.");
                    }
                }
                else
                {
                    AppLogger.Info($"RecipeParameterStore is empty. Using default values.");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load recipe parameters from RecipeParameterStore: {ex.Message}. Using default values.", ex);
            }
        }
        public void Dispose()
        {
            _recipeStore.RecipeChanged -= RecipeStore_RecipeChanged;
            _recipeParameterStore.ParametersUpdated -= OnRecipeParametersUpdated;
        }
    }
}

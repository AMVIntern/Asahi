using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asahi.Models
{
    public class RecipeParameterModel : INotifyPropertyChanged
    {
        public string ParameterName { get; set; }

        private object _value;
        public object Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(DisplayValue));
                OnPropertyChanged(nameof(ValueType));
            }

        }

        private string _newValue = "";
        public string NewValue
        {
            get { return _newValue; }
            set
            {
                _newValue = value;
                OnPropertyChanged(nameof(NewValue));
                ValidateNewValue();
            }
        }

        public string DisplayValue => Value?.ToString() ?? "";

        public Type ValueType => Value?.GetType() ?? typeof(string);

        public bool IsValid { get; private set; } = true;

        public object ParsedValue { get; private set; }

        private void ValidateNewValue()
        {
            if (string.IsNullOrWhiteSpace(NewValue))
            {
                IsValid = true;
                ParsedValue = Value;
            }
            else
            {
                try
                {
                    ParsedValue = Convert.ChangeType(NewValue, ValueType);
                    IsValid = true;
                }
                catch
                {
                    ParsedValue = null;
                    IsValid = false;
                }
            }
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(ParsedValue));
        }

        public string HintText
        {
            get
            {
                if (Value is int)
                    return "Enter an integer";
                if (Value is double)
                    return "Enter a decimal";
                if (Value is bool)
                    return "Enter true or false";
                if (Value is string)
                    return "Enter a string";

                return "Enter value";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

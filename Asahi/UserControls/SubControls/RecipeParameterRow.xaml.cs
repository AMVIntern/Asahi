using Asahi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Asahi.UserControls.SubControls
{
    /// <summary>
    /// Interaction logic for RecipeParameterRow.xaml
    /// </summary>
    public partial class RecipeParameterRow : UserControl, IDisposable
    {
        public string ParameterName
        {
            get { return (string)GetValue(ParameterNameProperty); }
            set { SetValue(ParameterNameProperty, value); }
        }

        public static readonly DependencyProperty ParameterNameProperty =
            DependencyProperty.Register(nameof(ParameterName), typeof(string), typeof(RecipeParameterRow), new PropertyMetadata(string.Empty));

        public string CurrentValue
        {
            get { return (string)GetValue(CurrentValueProperty); }
            set { SetValue(CurrentValueProperty, value); }
        }

        public static readonly DependencyProperty CurrentValueProperty =
            DependencyProperty.Register(nameof(CurrentValue), typeof(string), typeof(RecipeParameterRow), new PropertyMetadata(string.Empty));

        public string NewValue
        {
            get { return (string)GetValue(NewValueProperty); }
            set { SetValue(NewValueProperty, value); }
        }

        public static readonly DependencyProperty NewValueProperty =
            DependencyProperty.Register(nameof(NewValue), typeof(string), typeof(RecipeParameterRow), new PropertyMetadata(string.Empty));

        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            set { SetValue(IsValidProperty, value); }
        }

        public static readonly DependencyProperty IsValidProperty =
            DependencyProperty.Register(nameof(IsValid), typeof(string), typeof(RecipeParameterRow), new PropertyMetadata(string.Empty));

        public RecipeParameterRow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is RecipeParameterModel model)
            {
                SetBinding(IsValidProperty, new Binding(nameof(model.IsValid)));
                SetBinding(NewValueProperty, new Binding(nameof(model.NewValue)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            }
        }

        public void Dispose()
        {
            DataContextChanged -= OnDataContextChanged;
        }
    }
}

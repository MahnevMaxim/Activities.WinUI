using System.ComponentModel;

namespace WinUIExplorer
{
    /// <summary>Строка свойства элемента с чекбоксом «включить в селектор».</summary>
    public sealed class PropertyRow : INotifyPropertyChanged
    {
        public string Attribute { get; set; }
        public string DisplayName { get; set; }
        public string Value { get; set; }

        private bool _isIncluded;
        public bool IsIncluded
        {
            get => _isIncluded;
            set
            {
                if (_isIncluded == value) return;
                _isIncluded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsIncluded)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

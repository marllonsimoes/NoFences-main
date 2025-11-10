using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoFences.ViewModels
{
    /// <summary>
    /// Base class for ViewModels with manual INotifyPropertyChanged implementation.
    /// Compatible with .NET Framework 4.8.1 and C# 7.3 (no source generators required).
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed (auto-filled by compiler)</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the property value and raises PropertyChanged if the value changed.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="value">New value</param>
        /// <param name="propertyName">Name of the property (auto-filled by compiler)</param>
        /// <returns>True if the value was changed, false otherwise</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

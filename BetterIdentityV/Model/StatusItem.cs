using System;
using System.ComponentModel;
using BetterIdentityV.GameTask;
using BetterIdentityV.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterIdentityV.Model;

/// <summary>
/// 定义位于遮罩窗口状态栏中的状态
/// </summary>
public partial class StatusItem : ObservableObject, IDisposable
{
    public string Name { get; set; }
    private INotifyPropertyChanged _sourceObject { get; set; }
    private string _propertyName { get; set; }
    private ITaskTrigger? _trigger { get; set; }
    private INotifyPropertyChanged? _notifyTrigger { get; set; }

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private bool _isHealthy = true;

    public StatusItem(string name, INotifyPropertyChanged sourceObject, string propertyName = "Enabled", ITaskTrigger? trigger = null)
    {
        Name = name;
        _sourceObject = sourceObject;
        _propertyName = propertyName;
        _trigger = trigger;

        _sourceObject.PropertyChanged += OnSourcePropertyChanged;
        IsEnabled = GetSourceValue();
        IsHealthy = GetTriggerHealth();

        if (_trigger is INotifyPropertyChanged notifyTrigger)
        {
            _notifyTrigger = notifyTrigger;
            _notifyTrigger.PropertyChanged += OnTriggerPropertyChanged;
        }
    }

    private bool GetSourceValue()
    {
        var property = _sourceObject.GetType().GetProperty(_propertyName);
        ArgumentNullException.ThrowIfNull(property);
        var value = property.GetValue(_sourceObject);
        ArgumentNullException.ThrowIfNull(value);
        return (bool)value;
    }

    private bool GetTriggerHealth()
    {
        return _trigger?.IsHealthy ?? true;
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _propertyName)
        {
            IsEnabled = GetSourceValue();
        }
    }

    private void OnTriggerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ITaskTrigger.IsHealthy))
        {
            return;
        }

        UIDispatcherHelper.Invoke(() => IsHealthy = GetTriggerHealth());
    }

    public void Dispose()
    {
        _sourceObject.PropertyChanged -= OnSourcePropertyChanged;
        if (_notifyTrigger != null)
        {
            _notifyTrigger.PropertyChanged -= OnTriggerPropertyChanged;
        }

        GC.SuppressFinalize(this);
    }
}

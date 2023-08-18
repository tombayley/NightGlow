using CommunityToolkit.Mvvm.ComponentModel;
using NightGlow.Managers;
using System.Collections.Generic;

namespace NightGlow.Models;

public class RegSwitch : ObservableObject
{
    private readonly string _path;
    private readonly string _entryName;
    private readonly object _value;

    public bool Set
    {
        get
        {
            var currentValue = RegistryManager.GetValueOrDefault(_path, _entryName);
            return EqualityComparer<object>.Default.Equals(_value, currentValue);
        }
        set
        {
            if (value && !Set)
                RegistryManager.SetValue(_path, _entryName, _value);
            else if (!value && Set)
                RegistryManager.DeleteValue(_path, _entryName);
        }
    }

    public RegSwitch(string path, string entryName, object value)
    {
        _path = path;
        _entryName = entryName;
        _value = value;
    }
}

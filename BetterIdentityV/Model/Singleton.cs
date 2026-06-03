using System;
using System.Threading;

namespace BetterIdentityV.Model;

public class Singleton<T> where T : class
{
    protected static T? _instance;
    protected static object? syncRoot;

    public static T Instance => LazyInitializer.EnsureInitialized(ref _instance, ref syncRoot, CreateInstance);

    protected static T CreateInstance()
    {
        return (T)Activator.CreateInstance(typeof(T), true)!;
    }

    public static void DestroyInstance()
    {
        _instance = null;
    }
}

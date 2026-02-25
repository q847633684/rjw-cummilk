using System;

namespace EqualMilking.Helpers;

public static class EventHelper
{
    public static event Action OnPostLoadLong;

    public static void TriggerPostLoadLong()
    {
        OnPostLoadLong?.Invoke();
    }
    public static event Action OnPostNewGame;

    public static void TriggerPostNewGame()
    {
        OnPostNewGame?.Invoke();
    }
    public static event Action OnPostLoadGame;

    public static void TriggerPostLoadGame()
    {
        OnPostLoadGame?.Invoke();
    }
    public static event Action OnSettingsChanged;

    public static void TriggerSettingsChanged()
    {
        OnSettingsChanged?.Invoke();
    }
    public delegate void ReloadHandler(bool hotReload);
    public static event ReloadHandler OnDefsReloaded;

    public static void TriggerDefsReloaded(bool hotReload)
    {
        OnDefsReloaded?.Invoke(hotReload);
    }
}

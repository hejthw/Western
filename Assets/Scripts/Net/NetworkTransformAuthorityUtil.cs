using System;
using System.Reflection;
using FishNet.Component.Transforming;
using UnityEngine;

/// <summary>
/// В FishNet 4 поле <see cref="NetworkTransform"/> client-authoritative не вынесено в публичный API.
/// Для режима «сервер двигает игрока» (верёвка, лассо) временно выставляем server-authoritative синхронизацию.
/// </summary>
public static class NetworkTransformAuthorityUtil
{
    private static readonly FieldInfo ClientAuthoritativeField =
        typeof(NetworkTransform).GetField("_clientAuthoritative", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly MethodInfo ConfigureComponentsMethod =
        typeof(NetworkTransform).GetMethod("ConfigureComponents", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>Текущее значение _clientAuthoritative (true = клиент шлёт трансформ).</summary>
    public static bool GetClientAuthoritative(NetworkTransform networkTransform)
    {
        if (networkTransform == null || ClientAuthoritativeField == null)
            return true;
        try
        {
            return (bool)ClientAuthoritativeField.GetValue(networkTransform);
        }
        catch
        {
            return true;
        }
    }

    public static bool IsServerDrivingTransform(NetworkTransform networkTransform) =>
        networkTransform != null && !GetClientAuthoritative(networkTransform);

    /// <returns>false при ошибке (не меняйте внешний флаг «режим применён»).</returns>
    public static bool SetClientAuthoritative(NetworkTransform networkTransform, bool clientAuthoritative)
    {
        if (networkTransform == null)
            return false;

        if (ClientAuthoritativeField == null)
        {
            Debug.LogError("[NetworkTransformAuthorityUtil] Field _clientAuthoritative not found — FishNet version mismatch.");
            return false;
        }

        try
        {
            ClientAuthoritativeField.SetValue(networkTransform, clientAuthoritative);
            ConfigureComponentsMethod?.Invoke(networkTransform, null);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkTransformAuthorityUtil] SetClientAuthoritative failed: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }
}

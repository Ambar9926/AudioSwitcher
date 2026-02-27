using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AudioSwitcher;

public class AudioDeviceService
{
    private static readonly PropertyKey PKEY_FriendlyName =
        new(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);

    private readonly IMMDeviceEnumerator _enumerator;
    private readonly IPolicyConfig _policyConfig;

    public AudioDeviceService()
    {
        _enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
        _policyConfig = (IPolicyConfig)new PolicyConfigCom();
    }

    public List<(string Id, string Name)> GetPlaybackDevices()
    {
        var results = new List<(string, string)>();
        _enumerator.EnumAudioEndpoints(EDataFlow.eRender, 0x00000001, out var collection);
        collection.GetCount(out int count);
        for (int i = 0; i < count; i++)
        {
            collection.Item(i, out var device);
            device.GetId(out string id);
            results.Add((id, GetDeviceFriendlyName(device)));
        }
        return results;
    }

    public string GetDefaultDeviceId()
    {
        _enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out var device);
        device.GetId(out string id);
        return id;
    }

    public void SetDefaultDevice(string deviceId)
    {
        _policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
        _policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
        _policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications);
    }

    public string Toggle(string deviceA, string deviceB)
    {
        string currentId = GetDefaultDeviceId();
        string targetId = currentId == deviceA ? deviceB : deviceA;
        SetDefaultDevice(targetId);
        var devices = GetPlaybackDevices();
        var match = devices.Find(d => d.Id == targetId);
        return match.Name ?? "Unknown device";
    }

    private string GetDeviceFriendlyName(IMMDevice device)
    {
        try
        {
            device.OpenPropertyStore(0, out var store);
            var key = PKEY_FriendlyName;
            store.GetValue(ref key, out var value);
            return value.StringValue ?? "Unknown";
        }
        catch { return "Unknown"; }
    }
}
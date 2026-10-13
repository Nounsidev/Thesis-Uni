using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Newtonsoft.Json;
using System.Linq;
using Unity.Services.CloudSave.Models;

public class CloudSaveDataService : ICloudDataService
{
    private List<ItemKey> keys;
    #region Save Data
    public async Task SaveData(string key, object saveObject)
    {
        var data = new Dictionary<string, object> { { key, saveObject } };
        await Call(CloudSaveService.Instance.Data.Player.SaveAsync(data));
    }

    public async Task SaveData(params (string key, object saveObject)[] saveObjects)
    {
        var data = saveObjects.ToDictionary(item => item.key, item => item.saveObject);
        await Call(CloudSaveService.Instance.Data.Player.SaveAsync(data));
    }
    #endregion

    #region Load Data
    public async Task<T> LoadData<T>(string key)
    {
        var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
        return data.TryGetValue(key, out var value) ? Deserialize<T>(value.Value.GetAsString()) : default;
    }

    public async Task<T> TryLoadData<T>(string key)
    {
        var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
        return data.TryGetValue(key, out var value) ? Deserialize<T>(value.Value.GetAsString()) : default;
    }

    #endregion

    #region Additional Function

    public async Task LoadAllKeys()
    {
        keys = await CloudSaveService.Instance.Data.Player.ListAllKeysAsync();
    }

    public bool CheckKey(string key)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (key == keys[i].Key) return true;
        }
        return false;
    }

    public async Task DeleteKey(string key)
    {
        await Call(CloudSaveService.Instance.Files.Player.DeleteAsync(key, null));
    }

    public async Task DeleteAllKeys(int lenght)
    {
        var keys = await CloudSaveService.Instance.Data.Player.ListAllKeysAsync();
        for (int i = 0; i < lenght; i++)
        {
            await Call(DeleteKey(keys[i].Key));
        }
    }


    private static T Deserialize<T>(string input)
    {
        if (typeof(T) == typeof(string)) return (T)(object)input;
        return JsonConvert.DeserializeObject<T>(input);
    }
    #endregion

    #region Call Task
    private static async Task Call(Task action)
    {
        try
        {
            await action;
        }
        catch (CloudSaveValidationException e)
        {
            Debug.LogException(e);
        }
        catch (CloudSaveRateLimitedException e)
        {
            Debug.LogException(e);
        }
        catch (CloudSaveException e)
        {
            Debug.LogException(e);
        }
    }

    private static async Task<T> Call<T>(Task<T> action)
    {
        try
        {
            return await action;
        }
        catch (CloudSaveValidationException e)
        {
            Debug.LogException(e);
        }
        catch (CloudSaveRateLimitedException e)
        {
            Debug.LogException(e);
        }
        catch (CloudSaveException e)
        {
            Debug.LogException(e);
        }

        return default;
    }
    #endregion
}

#region Interface
public interface ICloudDataService
{
    Task SaveData(string key, object saveObject);
    Task SaveData(params (string key, object saveObject)[] saveObjects);
    Task<T> LoadData<T>(string key);
    Task<T> TryLoadData<T>(string key);
    bool CheckKey(string key);
    Task LoadAllKeys();
    Task DeleteAllKeys(int lenght);
    Task DeleteKey(string key);
}
#endregion

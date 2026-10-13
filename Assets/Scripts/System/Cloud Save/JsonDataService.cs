using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Text;

public class JsonDataService
{
    //Exception = errors that occur during execution

    //try = try some code that is considered dangerous
    //catch = catches and handles exceptions when they occur
    //finally = always executes regardless if exception is caucht or not


    //Generate KEY and IV:
    //Log.Debug($"Init")

    private const string KEY = "vgVrUnQuE9ka6zQStXXo5xC+C9f9NHsb4LSoq5zMjsg=";
    private const string IV = "ZXLC1V9f456NJBXmTwJtpA==";

    public bool SaveData<T>(string RelativePath, T Data, bool Encrypted)
    {
        string path = Application.persistentDataPath + RelativePath;

        try
        {
            if (File.Exists(path))
            {
                //Logger.Log("Data Exists. Deleting old file and writing new one");
                File.Delete(path);
            }
            else
            {
                Debug.Log("Writing file for the first time");
            }
            using FileStream stream = File.Create(path);
            if (Encrypted)
            {
                WriteEncryptedData(Data, stream);
            }
            else
            {
                //using keyword shorten down lenghty type declaration
                //FileStream stream = new FileStream(path, FileMode.Create);

                //Remember to close the stream or we can cause error and memory leak
                stream.Close();
                string contents = JsonConvert.SerializeObject(Data, Formatting.Indented);
                File.WriteAllText(path, contents);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.Log($"Unable to save data due to: {e.Message} {e.StackTrace}");
            return false;
        }
    }
    public T LoadData<T>(string RelativePath, bool Encrypted)
    {
        string path = Application.persistentDataPath + RelativePath;
        if (!File.Exists(path))
        {
            Debug.Log($"Can't Load File at {path}. File doesn't exist");
            throw new FileNotFoundException($"{path} doesn't exist");
        }
        try
        {
            T data;
            if (Encrypted)
            {
                data = ReadEncryptedData<T>(path);
            }
            else
            {
                data = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }

            return data;
        }
        catch (Exception e)
        {
            Debug.Log($"Failed to Load data due to:  {e.Message} {e.StackTrace}");
            throw e;
        }
    }

    private void WriteEncryptedData<T>(T data, FileStream stream)
    {
        //Aes stand for advance encrypted standard
        using Aes aesProvider = Aes.Create();

        aesProvider.Key = Convert.FromBase64String(KEY);
        aesProvider.IV = Convert.FromBase64String(IV);
        using ICryptoTransform cryptotransform = aesProvider.CreateEncryptor();
        using CryptoStream cryptostream = new CryptoStream(
            stream,//this stream
            cryptotransform,//running through this which has Key and IV Encryption
            CryptoStreamMode.Write // We gonna write data to the stream we create
            );


        cryptostream.Write(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data)));
    }

    private T ReadEncryptedData<T>(string path)
    {
        byte[] fileBytes = File.ReadAllBytes(path);
        using Aes aesProvider = Aes.Create();

        aesProvider.Key = Convert.FromBase64String(KEY);
        aesProvider.IV = Convert.FromBase64String(IV);

        using ICryptoTransform cryptoTransform = aesProvider.CreateDecryptor(aesProvider.Key, aesProvider.IV);
        using MemoryStream decryptionStream = new MemoryStream(fileBytes);
        using CryptoStream cryptoStream = new CryptoStream(decryptionStream, cryptoTransform, CryptoStreamMode.Read);
        using StreamReader reader = new StreamReader(cryptoStream);

        string result = reader.ReadToEnd();
        //Log.Debug($"Decrypted Result{result} ");
        return JsonConvert.DeserializeObject<T>(result);
    }
}

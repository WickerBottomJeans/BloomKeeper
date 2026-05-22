using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace DefaultNamespace
{
    public class LocalProgressRepository : IProgressRepository
{
    //C:/Users/Gumball/AppData/LocalLow/DefaultCompany/BloomKeeper\progress.dat
    private readonly string path = Path.Combine(Application.persistentDataPath, "progress.dat");
    private readonly string key = "please_dont_hack";
    
    public ProgressData Load()
    {
        if (!File.Exists(path)) 
            return new ProgressData();

        string encrypted = File.ReadAllText(path);
        string json = Decrypt(encrypted);
        return JsonConvert.DeserializeObject<ProgressData>(json);
    }

    public void Save(ProgressData progress)
    {
        string json = JsonConvert.SerializeObject(progress);
        string encrypted = Encrypt(json);
        File.WriteAllText(path, encrypted);
    }

    private string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
        return Convert.ToBase64String(result);
    }

    private string Decrypt(string encryptedText)
    {
        byte[] fullBytes = Convert.FromBase64String(encryptedText);
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        byte[] iv = new byte[aes.BlockSize / 8];
        byte[] encryptedBytes = new byte[fullBytes.Length - iv.Length];
        Buffer.BlockCopy(fullBytes, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullBytes, iv.Length, encryptedBytes, 0, encryptedBytes.Length);
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
}
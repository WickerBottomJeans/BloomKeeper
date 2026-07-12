using System;
using System.IO;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// Provides the guest custom ID used to log in to PlayFab
    /// </summary>
    public class GuestCustomIdStore
    {
        private const string GuestCustomIdFileName = "playfab_guest_custom_id.txt";

        public string GetOrCreateGuestCustomId()
        {
            string path = Path.Combine(Application.persistentDataPath, GuestCustomIdFileName);

            if (File.Exists(path))
            {
                string storedGuestCustomId = File.ReadAllText(path).Trim();
                if (!IsValidGuestCustomId(storedGuestCustomId)) throw new InvalidOperationException($"Stored PlayFab guest custom ID is invalid: {path}");

                return storedGuestCustomId;
            }

            string guestCustomId = CreateGuestCustomId();
            File.WriteAllText(path, guestCustomId);
            return guestCustomId;
        }

        private static string CreateGuestCustomId()
        {
            return $"guest_{Guid.NewGuid():N}";
        }

        private static bool IsValidGuestCustomId(string guestCustomId)
        {
            return !string.IsNullOrWhiteSpace(guestCustomId) && guestCustomId.StartsWith("guest_");
        }
    }
}

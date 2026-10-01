using System.Collections.Generic;
using UnityEngine;

namespace Game.Dialogue.Data
{
    /// <summary>
    /// Kumpulan semua CharacterData di game, dipakai DialogueManager untuk
    /// mencari data karakter berdasarkan ID (nama speaker dari file .ink).
    /// Cukup 1 asset ini untuk seluruh game — tinggal drag semua CharacterData
    /// yang ada ke list ini.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Dialogue/Character Database", fileName = "CharacterDatabase")]
    public class CharacterDatabase : ScriptableObject
    {
        public List<CharacterData> characters = new();

        private Dictionary<string, CharacterData> lookup;

        public CharacterData GetByID(string id)
        {
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(id, out var data) ? data : null;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, CharacterData>();
            foreach (var c in characters)
            {
                if (c == null || string.IsNullOrEmpty(c.characterID)) continue;
                lookup[c.characterID] = c;
            }
        }

        private void OnEnable() => lookup = null; // rebuild lazy tiap kali asset di-load ulang (misal habis di-edit)
    }
}

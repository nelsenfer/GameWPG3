using UnityEngine;

namespace Game.Dialogue.Data
{
    /// <summary>
    /// Data 1 karakter yang bisa muncul di chat bubble — baik hantu/NPC maupun MC sendiri.
    /// Bikin 1 asset per karakter (Create > Game > Dialogue > Character Data), lalu
    /// daftarkan semuanya ke CharacterDatabase supaya DialogueManager bisa nyari
    /// karakter berdasarkan ID yang ditulis di file .ink.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Dialogue/Character Data", fileName = "New Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Tooltip("ID unik, harus sama persis dengan nama speaker yang ditulis di file .ink (misal 'Hantu', 'MC').")]
        public string characterID;

        public string displayName;
        public Sprite portrait;

        [Tooltip("Centang kalau ini karakter pemain (MC) — dipakai ChatBubbleUI untuk flip posisi ke kanan.")]
        public bool isPlayerSide;
    }
}
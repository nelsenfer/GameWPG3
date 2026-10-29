using System;
using System.Collections.Generic;
using Ink.Runtime;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Mengenali "pilihan kontak" HP di Ink. Dua format didukung:
    ///   1) Teks pilihan:  * [ID::cuplikan chat::jumlah belum dibaca]
    ///      contoh:        * [Bos::Lembur dulu.::3]
    ///   2) Tag pilihan:   #kontak:ID #preview:teks #badge:angka   (kalau versi Ink-mu membacanya)
    /// Daftar kontak = SEMUA pilihan yang tersedia berformat kontak.
    /// </summary>
    public static class PhoneChoiceTags
    {
        private const string Separator = "::";

        public static bool TryGetTag(Choice choice, string key, out string value)
        {
            value = null;
            if (choice == null || choice.tags == null) return false;

            foreach (string raw in choice.tags)
            {
                string tag = raw.Trim();
                int colon = tag.IndexOf(':');
                if (colon <= 0) continue;

                if (string.Equals(tag.Substring(0, colon).Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    value = tag.Substring(colon + 1).Trim();
                    return true;
                }
            }
            return false;
        }

        public static bool TryParseContact(Choice choice, out string characterId, out string preview, out int badge)
        {
            characterId = null;
            preview = "";
            badge = 0;
            if (choice == null) return false;

            // Format teks: ID::preview::badge
            if (choice.text != null && choice.text.Contains(Separator))
            {
                string[] parts = choice.text.Split(new[] { Separator }, StringSplitOptions.None);
                characterId = parts[0].Trim();
                if (parts.Length > 1) preview = parts[1].Trim();
                if (parts.Length > 2) int.TryParse(parts[2].Trim(), out badge);
                return characterId.Length > 0;
            }

            // Format tag.
            if (TryGetTag(choice, "kontak", out characterId))
            {
                if (TryGetTag(choice, "preview", out string p)) preview = p;
                if (TryGetTag(choice, "badge", out string b)) int.TryParse(b, out badge);
                return true;
            }

            return false;
        }

        public static bool IsContactList(List<Choice> choices)
        {
            if (choices == null || choices.Count == 0) return false;
            foreach (Choice c in choices)
                if (!TryParseContact(c, out _, out _, out _)) return false;
            return true;
        }
    }
}
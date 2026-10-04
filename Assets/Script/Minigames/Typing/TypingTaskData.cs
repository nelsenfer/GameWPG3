using System.Collections.Generic;
using UnityEngine;

namespace Game.Typing
{
    /// <summary>
    /// Data 1 task mengetik (1 laporan / 1 berita = 1 ronde). Bikin 1 asset per task:
    /// Create > Game > Typing > Typing Task Data. GD bisa ubah isi dan syarat
    /// langsung dari Inspector tanpa buka kode.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Typing/Typing Task Data", fileName = "New Typing Task")]
    public class TypingTaskData : ScriptableObject
    {
        [Header("Isi Task")]
        public string taskTitle = "Laporan Bulanan";

        [Tooltip("Daftar baris yang diketik. 1 elemen = 1 baris pendek (sekitar 40-80 huruf). Kalau 1 baris selesai sebelum waktu habis, lanjut ke baris berikutnya. Isi lebih banyak baris (12-20) supaya pemain cepat tidak kehabisan. Hindari karakter '<' dan karakter yang susah diketik.")]
        [TextArea(1, 3)] public List<string> texts = new List<string>();

        [Tooltip("Kalau dicentang, urutan baris diacak tiap ronde dan tidak ada baris yang diulang sampai semua baris sudah keluar.")]
        public bool randomizeOrder = true;

        [Header("Aturan")]
        [Min(5f)] public float durationSeconds = 60f;
        [Tooltip("Jumlah huruf minimal yang harus diketik (benar + salah) sampai timer habis.")]
        [Min(0)] public int minTypedChars = 150;
        [Range(0f, 100f)] public float minAccuracyPercent = 80f;

        [Header("Pesan Cerita di Layar Hasil")]
        [TextArea] public string passMessage = "Laporan terkirim.";
        [TextArea] public string failMessage = "Revisi lagi...";
    }
}
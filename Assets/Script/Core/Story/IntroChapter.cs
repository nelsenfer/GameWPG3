using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ink.UnityIntegration;
using Game.Core;

namespace Game.Progression
{
    /// <summary>
    /// Bab intro: urutan cerita ditulis di kode (lihat Awake). Inspector hanya untuk
    /// drag objek scene, file ink, dan event efek. Pasang di ==Manager==.
    /// Kolom ink yang dikosongkan = bagian dialog itu dilewati.
    ///
    /// Urutan:
    ///  0. Chat HP (bos + Mama)
    ///  1. Narator tutorial
    ///  2. Ke meja -> minigame mengetik
    ///  3. Ngantuk (dialog)
    ///  4. Ambil kopi di dapur
    ///  (opsional) lampu berkedip saat masuk Zona Lampu
    ///  5. Kembali ke meja -> tampil meja berantakan (close-up)
    ///  6. Ambil lonceng -> kembali ke side-scroll
    ///  (opsional) 7. Cutscene hantu -> pindah dimensi (aktifkan Include Cutscene)
    /// </summary>
    public class IntroChapter : MonoBehaviour
    {
        [SerializeField] private StoryDirector director;

        [Header("Objek scene")]
        [Tooltip("Meja kerja (komputer). Dipakai untuk minigame DAN saat kembali dari dapur.")]
        [SerializeField] private GameObject meja;
        [SerializeField] private GameObject kopi;
        [Tooltip("Opsional: area di jalan pulang dari dapur. Kosong = beat lampu dilewati.")]
        [SerializeField] private GameObject zonaLampu;
        [Tooltip("Panel meja berantakan (close-up). Dibuka saat kembali ke meja, ditutup setelah lonceng diambil.")]
        [SerializeField] private GameObject mejaPanel;
        [Tooltip("Lonceng = objek di dalam Meja Panel yang punya komponen Button.")]
        [SerializeField] private GameObject lonceng;
        [Tooltip("Pemain ditahan (tidak bisa jalan) selama panel meja terbuka.")]
        [SerializeField] private PlayerController player;
        [SerializeField] private MinigameLauncher minigameKomputer;

        [Header("Dialog (Ink)")]
        [SerializeField] private InkFile prologChat;
        [Tooltip("Narasi tutorial (gerak, interaksi). Pakai #narasi di tiap baris.")]
        [SerializeField] private InkFile tutorialInk;
        [Tooltip("Dialog sebelum minigame (opsional).")]
        [SerializeField] private InkFile komputerInk;
        [SerializeField] private InkFile ngantukInk;
        [SerializeField] private InkFile kopiInk;
        [Tooltip("Dipakai kalau meja ditekan sebelum kopi diambil.")]
        [SerializeField] private InkFile komputerKopiDulu;
        [Tooltip("Dialog saat kembali ke meja.")]
        [SerializeField] private InkFile mejaInk;
        [SerializeField] private InkFile loncengInk;
        [SerializeField] private InkFile kagetInk;

        [Header("Efek (sambungkan nanti)")]
        [SerializeField] private UnityEvent onLampuKedip;
        [Tooltip("Pindah ke tampilan meja berantakan (ganti background).")]
        [SerializeField] private UnityEvent onTampilMeja;
        [Tooltip("Kembali dari tampilan meja ke side-scroll.")]
        [SerializeField] private UnityEvent onTutupMeja;
        [SerializeField] private UnityEvent onHantuMuncul;
        [SerializeField] private UnityEvent onLoncengBunyi;
        [SerializeField] private UnityEvent onPindahDimensi;

        [Header("Opsi")]
        [Tooltip("Tambahkan cutscene hantu + pindah dimensi di akhir. Matikan kalau target masih sampai lonceng.")]
        [SerializeField] private bool includeCutscene = false;

        private void Awake()
        {
            var beats = new List<StoryBeat>();

            // 0. Chat HP dari bos dan keluarga
            beats.Add(new StoryBeat
            {
                beatName = "Prolog chat",
                trigger = BeatTrigger.Auto,
                dialogue = prologChat
            });

            // 1. Narator tutorial, lalu misi ke meja
            beats.Add(new StoryBeat
            {
                beatName = "Narator tutorial",
                trigger = BeatTrigger.Auto,
                objective = "Pergi ke mejamu",
                dialogue = tutorialInk
            });

            // 2. Ke meja, minigame mengetik
            beats.Add(new StoryBeat
            {
                beatName = "Ngetik laporan",
                trigger = BeatTrigger.Interact,
                target = meja,
                prompt = "[E] Kerjakan laporan",
                objective = "Selesaikan laporan",
                dialogue = komputerInk,
                minigame = minigameKomputer
            });

            // 3. Ngantuk (dialog)
            beats.Add(new StoryBeat
            {
                beatName = "Ngantuk",
                trigger = BeatTrigger.Auto,
                objective = "Buat kopi di dapur",
                dialogue = ngantukInk
            });

            // 4. Ambil kopi di dapur
            beats.Add(new StoryBeat
            {
                beatName = "Ambil kopi",
                trigger = BeatTrigger.Interact,
                target = kopi,
                prompt = "[E] Ambil kopi",
                dialogue = kopiInk,
                hideTargetAfter = true,
                hints = KopiDulu()
            });

            // (opsional) lampu berkedip di jalan pulang
            if (zonaLampu != null)
            {
                var lampu = new StoryBeat
                {
                    beatName = "Lampu berkedip",
                    trigger = BeatTrigger.EnterArea,
                    target = zonaLampu,
                    objective = "Kembali ke mejamu",
                    hints = KopiDulu()
                };
                lampu.onTriggered.AddListener(onLampuKedip.Invoke);
                beats.Add(lampu);
            }

            // 5. Kembali ke meja: tampil meja berantakan (close-up)
            var lihatMeja = new StoryBeat
            {
                beatName = "Kembali ke meja",
                trigger = BeatTrigger.Interact,
                target = meja,
                prompt = "[E] Periksa meja",
                objective = "Periksa mejamu",
                dialogue = mejaInk
            };
            lihatMeja.onTriggered.AddListener(() =>
            {
                if (mejaPanel != null) mejaPanel.SetActive(true);
                if (player != null) player.SetFrozen(true);
                onTampilMeja.Invoke();
            });
            // Dialog selesai membuka kunci pemain; tahan lagi selama panel masih terbuka.
            lihatMeja.onFinished.AddListener(() => { if (player != null) player.SetFrozen(true); });
            beats.Add(lihatMeja);

            // 6. Klik lonceng di panel, panel ditutup, kembali ke side-scroll
            var ambilLonceng = new StoryBeat
            {
                beatName = "Ambil lonceng",
                trigger = BeatTrigger.ClickButton,
                target = lonceng,
                objective = "Ambil lonceng di meja",
                dialogue = loncengInk,
                hideTargetAfter = true
            };
            ambilLonceng.onFinished.AddListener(() =>
            {
                if (mejaPanel != null) mejaPanel.SetActive(false);
                if (player != null) player.SetFrozen(false);
                onTutupMeja.Invoke();
            });
            beats.Add(ambilLonceng);

            // 7. (opsional) Cutscene hantu, lonceng bunyi, pindah dimensi
            if (includeCutscene)
            {
                var cutscene = new StoryBeat
                {
                    beatName = "Cutscene hantu",
                    trigger = BeatTrigger.Auto,
                    clearObjective = true,
                    dialogue = kagetInk
                };
                cutscene.onTriggered.AddListener(onHantuMuncul.Invoke);
                cutscene.onFinished.AddListener(onLoncengBunyi.Invoke);
                cutscene.onFinished.AddListener(onPindahDimensi.Invoke);
                beats.Add(cutscene);
            }

            if (director != null) director.LoadBeats(beats);
            else Debug.LogError("[IntroChapter] Director belum di-assign.", this);
        }

        // Meja ditekan sebelum waktunya: "kopi dulu".
        private List<WaitingHint> KopiDulu()
        {
            var list = new List<WaitingHint>();
            if (meja != null && komputerKopiDulu != null)
                list.Add(new WaitingHint { target = meja, prompt = "[E] Periksa", dialogue = komputerKopiDulu });
            return list;
        }
    }
}
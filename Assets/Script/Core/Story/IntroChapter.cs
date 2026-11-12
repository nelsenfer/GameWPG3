using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
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
    ///  6. Ambil lonceng -> kembali ke side-scroll -> lampu kantor berkedip & hantu muncul di kiri layar
    ///  (opsional) 7. Cutscene dialog kaget / pindah dimensi (aktifkan Include Cutscene)
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

        [Header("Cahaya & Hantu (Setelah Ambil Lonceng)")]
        [Tooltip("Global Light 2D kantor yang akan berkedip setelah lonceng diambil.")]
        [SerializeField] private Light2D lampuKantor;
        [Tooltip("Objek hantu yang tiba-tiba muncul di sebelah kiri layar di sela kedipan lampu.")]
        [SerializeField] private GameObject hantu;
        [Tooltip("Jarak kemunculan hantu dari tepi kiri layar kamera (satuan world).")]
        [SerializeField] private float jarakHantuDariKiriLayar = 3.5f;
        [Tooltip("Posisi Y hantu saat muncul di lantai kantor.")]
        [SerializeField] private float posisiYHantu = -2.54f;

        [Header("Dunia Lain & Minigame Kotak (Space Dive)")]
        [Tooltip("Pemicu interaksi [E] Bunyikan lonceng yang muncul di posisi MC setelah dialog kaget selesai.")]
        [SerializeField] private GameObject pemicuLonceng;
        [Tooltip("Transisi Space Dive menuju Dunia Lain.")]
        [SerializeField] private SpaceDiveTransition transisiSpaceDive;
        [Tooltip("Minigame menyusun 4 kotak (A - D) di Dunia Lain.")]
        [SerializeField] private BoxSortingMinigame minigameKotak;

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
        [Tooltip("Dialog bersama Hantu Hyper-Responsibility saat tiba di Dunia Lain.")]
        [SerializeField] private InkFile duniaLainInk;
        [Tooltip("Dialog penutup setelah ke-4 kotak berhasil disusun.")]
        [SerializeField] private InkFile selesaiKotakInk;

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

        private Coroutine flickerRoutine;

        private void Awake()
        {
            if (lampuKantor == null)
            {
                GameObject lightObj = GameObject.Find("Global Light 2D");
                if (lightObj != null) lampuKantor = lightObj.GetComponent<Light2D>();
                if (lampuKantor == null) lampuKantor = FindFirstObjectByType<Light2D>();
            }

            if (hantu == null)
            {
                hantu = GameObject.Find("Hantu_HyperResponsibility");
                if (hantu == null) hantu = GameObject.Find("Hantu_MasaKecil");
            }

            // Sembunyikan hantu di awal permainan sampai momen lonceng diambil
            if (hantu != null)
            {
                hantu.SetActive(false);
            }

            if (pemicuLonceng == null)
            {
                pemicuLonceng = GameObject.Find("Pemicu_BunyikanLonceng");
                if (pemicuLonceng == null)
                {
                    pemicuLonceng = new GameObject("Pemicu_BunyikanLonceng");
                    pemicuLonceng.layer = 6; // Interactable
                    CircleCollider2D col = pemicuLonceng.AddComponent<CircleCollider2D>();
                    col.isTrigger = true;
                    col.radius = 1.15f;
                }
            }
            pemicuLonceng.SetActive(false);

            if (transisiSpaceDive == null)
                transisiSpaceDive = FindFirstObjectByType<SpaceDiveTransition>(FindObjectsInactive.Include);

            if (minigameKotak == null)
                minigameKotak = FindFirstObjectByType<BoxSortingMinigame>(FindObjectsInactive.Include);
            if (minigameKotak != null)
                minigameKotak.gameObject.SetActive(false);

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

            // 6. Klik lonceng di panel, panel ditutup, kembali ke side-scroll -> lampu berkedip & hantu muncul
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
                onTutupMeja.Invoke();

                if (flickerRoutine != null) StopCoroutine(flickerRoutine);
                flickerRoutine = StartCoroutine(KedipLampuDanMunculkanHantuRoutine());
            });
            beats.Add(ambilLonceng);

            // 7. Interaksi [E] Bunyikan lonceng di posisi MC -> Transisi Space Dive ke Dunia Lain
            var bunyikanLonceng = new StoryBeat
            {
                beatName = "Bunyikan lonceng",
                trigger = BeatTrigger.Interact,
                target = pemicuLonceng,
                prompt = "[E] Bunyikan lonceng",
                minigame = transisiSpaceDive,
                hideTargetAfter = true
            };
            bunyikanLonceng.onTriggered.AddListener(() =>
            {
                if (pemicuLonceng != null) pemicuLonceng.SetActive(false);
                onLoncengBunyi?.Invoke();
                onPindahDimensi?.Invoke();
            });
            beats.Add(bunyikanLonceng);

            // 8. Dialog Hantu Hyper-Responsibility di Dunia Lain -> Minigame menyusun 4 kotak (A - D)
            var duniaLainBeat = new StoryBeat
            {
                beatName = "Dunia Lain & Minigame Kotak",
                trigger = BeatTrigger.Auto,
                objective = "Susun ke-4 kotak beban (A - D) ke tempat yang tepat",
                dialogue = duniaLainInk,
                minigame = minigameKotak
            };
            beats.Add(duniaLainBeat);

            // 9. Dialog penutup setelah ke-4 kotak berhasil disusun
            var selesaiKotakBeat = new StoryBeat
            {
                beatName = "Selesai Minigame Kotak",
                trigger = BeatTrigger.Auto,
                clearObjective = true,
                dialogue = selesaiKotakInk
            };
            beats.Add(selesaiKotakBeat);

            if (director != null) director.LoadBeats(beats);
            else Debug.LogError("[IntroChapter] Director belum di-assign.", this);
        }

        private IEnumerator KedipLampuDanMunculkanHantuRoutine()
        {
            if (player != null) player.SetFrozen(true);

            // Jeda singkat setelah keluar dari panel meja ke mode side-scrolling
            yield return new WaitForSeconds(0.35f);

            onLampuKedip?.Invoke();

            float baseIntensity = lampuKantor != null ? lampuKantor.intensity : 1f;

            // Kedipan lampu kantor tahap 1 (sebelum hantu muncul)
            SetLightIntensity(0.12f);
            yield return new WaitForSeconds(0.08f);
            SetLightIntensity(baseIntensity);
            yield return new WaitForSeconds(0.14f);

            SetLightIntensity(0.04f);
            yield return new WaitForSeconds(0.10f);
            SetLightIntensity(baseIntensity * 0.85f);
            yield return new WaitForSeconds(0.07f);

            SetLightIntensity(0.15f);
            yield return new WaitForSeconds(0.06f);
            SetLightIntensity(baseIntensity);
            yield return new WaitForSeconds(0.18f);

            // Lampu padam sejenak di antara kedipan -> objek hantu muncul di sebelah kiri layar
            SetLightIntensity(0.0f);
            yield return new WaitForSeconds(0.14f);

            MunculkanHantuDiKiriLayar();
            onHantuMuncul?.Invoke();

            yield return new WaitForSeconds(0.10f);

            // Kedipan lampu tahap 2 (memperlihatkan hantu yang baru muncul)
            SetLightIntensity(baseIntensity * 0.9f);
            yield return new WaitForSeconds(0.12f);

            SetLightIntensity(0.06f);
            yield return new WaitForSeconds(0.09f);

            SetLightIntensity(baseIntensity * 0.75f);
            yield return new WaitForSeconds(0.08f);

            SetLightIntensity(0.15f);
            yield return new WaitForSeconds(0.07f);

            // Kembalikan cahaya normal
            SetLightIntensity(baseIntensity);

            // Putar dialog kaget setelah hantu muncul, lalu munculkan prompt [E] Bunyikan lonceng di posisi MC
            if (kagetInk != null)
            {
                var dm = FindFirstObjectByType<Game.Dialogue.Core.DialogueManager>();
                if (dm != null)
                {
                    dm.StartDialogue(kagetInk, AktifkanPemicuLonceng);
                    flickerRoutine = null;
                    yield break;
                }
            }

            if (player != null) player.SetFrozen(false);
            AktifkanPemicuLonceng();
            flickerRoutine = null;
        }

        private void LateUpdate()
        {
            if (pemicuLonceng != null && pemicuLonceng.activeSelf && player != null)
            {
                pemicuLonceng.transform.position = player.transform.position;
            }
        }

        private void AktifkanPemicuLonceng()
        {
            if (pemicuLonceng == null) return;

            if (player != null)
            {
                pemicuLonceng.transform.position = player.transform.position;
            }

            pemicuLonceng.SetActive(true);

            var progress = FindFirstObjectByType<StoryProgress>();
            if (progress != null)
            {
                progress.SetObjective("Bunyikan lonceng");
            }
        }

        private void SetLightIntensity(float intensity)
        {
            if (lampuKantor != null)
            {
                lampuKantor.intensity = intensity;
            }
        }

        private void MunculkanHantuDiKiriLayar()
        {
            if (hantu == null) return;

            Camera cam = Camera.main;
            if (cam != null)
            {
                float halfWidth = cam.orthographicSize * cam.aspect;
                float leftScreenX = cam.transform.position.x - halfWidth + jarakHantuDariKiriLayar;
                Vector3 pos = hantu.transform.position;
                pos.x = leftScreenX;
                pos.y = posisiYHantu;
                hantu.transform.position = pos;
            }

            SpriteRenderer sr = hantu.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sortingOrder <= 0)
            {
                sr.sortingOrder = 5;
            }

            hantu.SetActive(true);
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

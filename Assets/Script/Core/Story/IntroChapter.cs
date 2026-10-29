using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ink.UnityIntegration;

namespace Game.Progression
{
    /// <summary>
    /// Bab intro: urutan cerita ditulis di kode. Inspector hanya untuk
    /// drag objek scene, file Ink, dan event efek.
    /// </summary>
    public class IntroChapter : MonoBehaviour
    {
        [SerializeField] private StoryDirector director;

        [Header("Objek scene")]
        [SerializeField] private GameObject komputer;
        [SerializeField] private GameObject kopi;
        [Tooltip("Area (Collider2D) di jalan pulang dari dapur.")]
        [SerializeField] private GameObject zonaLampu;
        [Tooltip("Meja di scene side-scrolling.")]
        [SerializeField] private GameObject meja;
        [Tooltip("Lonceng di tampilan meja (close-up).")]
        [SerializeField] private GameObject lonceng;
        [SerializeField] private MinigameLauncher minigameKomputer;

        [Header("Dialog (Ink)")]
        [SerializeField] private InkFile prologChat;
        [SerializeField] private InkFile komputerInk;
        [SerializeField] private InkFile ngantukInk;
        [SerializeField] private InkFile kopiInk;
        [SerializeField] private InkFile komputerKopiDulu;
        [SerializeField] private InkFile mejaInk;
        [SerializeField] private InkFile loncengInk;
        [SerializeField] private InkFile kagetInk;

        [Header("Efek (sambungkan nanti)")]
        [SerializeField] private UnityEvent onLampuKedip;
        [SerializeField] private UnityEvent onTampilMeja;
        [SerializeField] private UnityEvent onTutupMeja;
        [SerializeField] private UnityEvent onHantuMuncul;
        [SerializeField] private UnityEvent onLoncengBunyi;
        [SerializeField] private UnityEvent onPindahDimensi;

        private void Awake()
        {
            List<StoryBeat> beats = new List<StoryBeat>();

            beats.Add(new StoryBeat
            {
                beatName = "Prolog chat",
                trigger = BeatTrigger.Auto,
                dialogue = prologChat
            });

            beats.Add(new StoryBeat
            {
                beatName = "Ngetik laporan",
                trigger = BeatTrigger.Interact,
                target = komputer,
                prompt = "[E] Pakai komputer",
                objective = "Selesaikan laporan",
                dialogue = komputerInk,
                minigame = minigameKomputer
            });

            beats.Add(new StoryBeat
            {
                beatName = "Ngantuk",
                trigger = BeatTrigger.Auto,
                objective = "Buat kopi di dapur",
                dialogue = ngantukInk
            });

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

            StoryBeat lampu = new StoryBeat
            {
                beatName = "Lampu berkedip",
                trigger = BeatTrigger.EnterArea,
                target = zonaLampu,
                objective = "Kembali ke meja",
                hints = KopiDulu()
            };
            AddListener(lampu.onTriggered, onLampuKedip);
            beats.Add(lampu);

            StoryBeat lihatMeja = new StoryBeat
            {
                beatName = "Lihat meja",
                trigger = BeatTrigger.Interact,
                target = meja,
                prompt = "[E] Lihat meja",
                objective = "Periksa mejamu",
                dialogue = mejaInk
            };
            AddListener(lihatMeja.onTriggered, onTampilMeja);
            beats.Add(lihatMeja);

            StoryBeat ambilLonceng = new StoryBeat
            {
                beatName = "Ambil lonceng",
                trigger = BeatTrigger.Interact,
                target = lonceng,
                prompt = "[E] Ambil lonceng",
                objective = "Ambil lonceng",
                dialogue = loncengInk
            };
            AddListener(ambilLonceng.onFinished, onTutupMeja);
            beats.Add(ambilLonceng);

            StoryBeat cutscene = new StoryBeat
            {
                beatName = "Cutscene hantu",
                trigger = BeatTrigger.Auto,
                clearObjective = true,
                dialogue = kagetInk
            };
            AddListener(cutscene.onTriggered, onHantuMuncul);
            AddListener(cutscene.onFinished, onLoncengBunyi);
            AddListener(cutscene.onFinished, onPindahDimensi);
            beats.Add(cutscene);

            if (director != null) director.LoadBeats(beats);
            else Debug.LogError("[IntroChapter] Director belum di-assign.", this);
        }

        private List<WaitingHint> KopiDulu()
        {
            List<WaitingHint> hints = new List<WaitingHint>();
            if (komputer != null && komputerKopiDulu != null)
            {
                hints.Add(new WaitingHint
                {
                    target = komputer,
                    prompt = "[E] Periksa",
                    dialogue = komputerKopiDulu
                });
            }

            return hints;
        }

        private static void AddListener(UnityEvent target, UnityEvent source)
        {
            if (source != null) target.AddListener(source.Invoke);
        }
    }
}

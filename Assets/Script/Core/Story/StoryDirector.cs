using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ink.UnityIntegration;
using Game.Core;
using Game.Dialogue.Core;

namespace Game.Progression
{
    public enum BeatTrigger
    {
        [Tooltip("Jalan sendiri begitu giliran beat ini tiba.")] Auto,
        [Tooltip("Jalan saat pemain menekan E pada objek Target.")] Interact,
        [Tooltip("Jalan saat pemain masuk ke area Target (collider trigger).")] EnterArea,
        [Tooltip("Jalan saat pemain mengklik tombol UI (Button) pada Target, misal benda di panel meja.")] ClickButton
    }

    /// <summary>Interaksi sampingan selama sebuah beat aktif (misal komputer: "kopi dulu").</summary>
    [Serializable]
    public class WaitingHint
    {
        public GameObject target;
        public string prompt = "[E] Periksa";
        public InkFile dialogue;
    }

    /// <summary>Satu kejadian dalam cerita. Dijalankan berurutan dari atas ke bawah.</summary>
    [Serializable]
    public class StoryBeat
    {
        public string beatName = "Beat";

        [Header("Kapan jalan")]
        public BeatTrigger trigger = BeatTrigger.Auto;
        [Tooltip("Objek yang di-interaksi (Interact) atau area (EnterArea). Kosong kalau Auto.")]
        public GameObject target;
        public string prompt = "[E] Periksa";

        [Header("Saat beat ini mulai aktif")]
        [Tooltip("Teks misi di HUD. Kosong = tidak diubah.")]
        public string objective;
        public bool clearObjective;
        public UnityEvent onActivated = new UnityEvent();

        [Header("Isi beat (urutan: event -> panel -> dialog -> minigame)")]
        public UnityEvent onTriggered = new UnityEvent();
        public GameObject showPanel;
        public InkFile dialogue;
        public MinigameLauncher minigame;

        [Header("Setelah selesai")]
        public UnityEvent onFinished = new UnityEvent();
        public bool hideTargetAfter;

        [Header("Selama beat ini menunggu")]
        public List<WaitingHint> hints = new List<WaitingHint>();
    }

    /// <summary>
    /// Satu-satunya tempat urutan cerita. Pasang di ==Manager==.
    /// Beat dijalankan linear; beat berikutnya baru aktif setelah beat sebelumnya selesai.
    /// </summary>
    public class StoryDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StoryProgress progress;
        [SerializeField] private DialogueManager dialogueManager;

        [Header("Cerita")]
        [SerializeField] private List<StoryBeat> beats = new List<StoryBeat>();

        [Header("Testing")]
        [Tooltip("Mulai dari beat nomor ini (0 = dari awal). Beat sebelumnya dilewati tanpa efek.")]
        [SerializeField] private int startAtBeat = 0;
        [SerializeField] private float autoDelay = 0.5f;

        private int index;
        private bool busy;
        private MinigameLauncher activeMinigame;
        private StoryBeat runningBeat;

        public StoryBeat CurrentBeat => index >= 0 && index < beats.Count ? beats[index] : null;

        private void Awake()
        {
            SetupBeatObjects();
        }

        /// <summary>Dipakai chapter yang menyusun beat lewat kode (misal IntroChapter).</summary>
        public void LoadBeats(List<StoryBeat> newBeats)
        {
            beats = newBeats;
            SetupBeatObjects();
        }

        private void SetupBeatObjects()
        {
            foreach (StoryBeat b in beats)
            {
                if (b.trigger == BeatTrigger.Interact) EnsureTarget(b.target);
                if (b.trigger == BeatTrigger.EnterArea) EnsureZone(b.target);
                if (b.trigger == BeatTrigger.ClickButton) EnsureButton(b.target);
                foreach (WaitingHint h in b.hints) EnsureTarget(h.target);
            }
        }

        private readonly HashSet<GameObject> hookedButtons = new HashSet<GameObject>();

        private void EnsureButton(GameObject go)
        {
            if (go == null || hookedButtons.Contains(go)) return;

            UnityEngine.UI.Button button = go.GetComponent<UnityEngine.UI.Button>();
            if (button == null) button = go.GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (button == null)
            {
                Debug.LogWarning("[StoryDirector] '" + go.name + "' butuh komponen Button (beat ClickButton).", go);
                return;
            }

            hookedButtons.Add(go);
            button.onClick.AddListener(() => ButtonClicked(go));
        }

        private void ButtonClicked(GameObject go)
        {
            StoryBeat beat = CurrentBeat;
            if (busy || beat == null) return;
            if (beat.trigger != BeatTrigger.ClickButton || beat.target != go) return;
            if (dialogueManager != null && dialogueManager.IsDialogueActive) return;
            Run(beat);
        }

        private void Start()
        {
            index = Mathf.Clamp(startAtBeat, 0, beats.Count);
            EnterBeat();
        }

        // ---------- setup objek ----------

        private void EnsureTarget(GameObject go)
        {
            if (go == null) return;
            StoryTarget t = go.GetComponent<StoryTarget>();
            if (t == null) t = go.AddComponent<StoryTarget>();
            t.director = this;
        }

        private void EnsureZone(GameObject go)
        {
            if (go == null) return;
            StoryZone z = go.GetComponent<StoryZone>();
            if (z == null) z = go.AddComponent<StoryZone>();
            z.director = this;
            Collider2D col = go.GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            else Debug.LogWarning("[StoryDirector] Area '" + go.name + "' butuh Collider2D.", go);
        }

        // ---------- alur beat ----------

        private void EnterBeat()
        {
            StoryBeat beat = CurrentBeat;
            if (beat == null)
            {
                Debug.Log("[StoryDirector] Semua beat selesai.");
                return;
            }

            if (beat.clearObjective) progress?.ClearObjective();
            else if (!string.IsNullOrWhiteSpace(beat.objective)) progress?.SetObjective(beat.objective);

            beat.onActivated?.Invoke();

            if (beat.trigger == BeatTrigger.Auto)
                StartCoroutine(AutoRoutine(index));
        }

        private IEnumerator AutoRoutine(int beatIndex)
        {
            yield return new WaitForSecondsRealtime(autoDelay);

            while (dialogueManager != null && dialogueManager.IsDialogueActive)
                yield return null;

            if (beatIndex != index || busy) yield break;
            Run(beats[beatIndex]);
        }

        private void Run(StoryBeat beat)
        {
            busy = true;
            runningBeat = beat;

            beat.onTriggered?.Invoke();
            if (beat.showPanel != null) beat.showPanel.SetActive(true);

            if (beat.dialogue != null)
            {
                if (dialogueManager == null)
                {
                    Debug.LogError("[StoryDirector] Dialogue Manager belum di-assign.", this);
                    Abort(beat);
                    return;
                }
                dialogueManager.StartDialogue(beat.dialogue, () => AfterDialogue(beat));
            }
            else
            {
                AfterDialogue(beat);
            }
        }

        private void AfterDialogue(StoryBeat beat)
        {
            if (beat.showPanel != null) beat.showPanel.SetActive(false);

            if (beat.minigame != null)
            {
                activeMinigame = beat.minigame;
                activeMinigame.Launch(() => Finish(beat));
            }
            else
            {
                Finish(beat);
            }
        }

        private void Update()
        {
            // Minigame ditutup tanpa lulus -> beat tetap, bisa dicoba lagi.
            if (activeMinigame != null && !activeMinigame.IsOpen)
            {
                activeMinigame = null;
                busy = false;
                StoryBeat beat = CurrentBeat;
                if (beat != null && beat.trigger == BeatTrigger.Auto)
                    StartCoroutine(AutoRoutine(index));
            }
        }

        private void Finish(StoryBeat beat)
        {
            activeMinigame = null;
            busy = false;
            runningBeat = null;

            if (beat.hideTargetAfter && beat.target != null) beat.target.SetActive(false);
            beat.onFinished?.Invoke();

            index++;
            EnterBeat();
        }

        private void Abort(StoryBeat beat)
        {
            if (beat.showPanel != null) beat.showPanel.SetActive(false);
            busy = false;
            runningBeat = null;
        }

        // ---------- dipanggil StoryTarget / StoryZone ----------

        private WaitingHint FindHint(GameObject go)
        {
            StoryBeat beat = CurrentBeat;
            if (beat == null) return null;
            foreach (WaitingHint h in beat.hints)
                if (h.target == go && h.dialogue != null) return h;
            return null;
        }

        private bool IsMainTarget(GameObject go)
        {
            StoryBeat beat = CurrentBeat;
            return beat != null && beat.trigger == BeatTrigger.Interact && beat.target == go;
        }

        public bool CanInteractWith(StoryTarget t)
        {
            if (busy) return false;
            if (dialogueManager != null && dialogueManager.IsDialogueActive) return false;
            return IsMainTarget(t.gameObject) || FindHint(t.gameObject) != null;
        }

        public string PromptFor(StoryTarget t)
        {
            if (IsMainTarget(t.gameObject)) return CurrentBeat.prompt;
            WaitingHint h = FindHint(t.gameObject);
            return h != null ? h.prompt : "";
        }

        public void Interacted(StoryTarget t)
        {
            if (!CanInteractWith(t)) return;

            if (IsMainTarget(t.gameObject))
            {
                Run(CurrentBeat);
                return;
            }

            WaitingHint hint = FindHint(t.gameObject);
            if (hint != null && dialogueManager != null)
            {
                busy = true;
                dialogueManager.StartDialogue(hint.dialogue, () => busy = false);
            }
        }

        public void ZoneEntered(StoryZone z)
        {
            StoryBeat beat = CurrentBeat;
            if (busy || beat == null) return;
            if (beat.trigger != BeatTrigger.EnterArea || beat.target != z.gameObject) return;
            Run(beat);
        }
    }
}
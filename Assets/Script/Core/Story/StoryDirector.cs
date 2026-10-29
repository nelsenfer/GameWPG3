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
        [Tooltip("Jalan sendiri begitu giliran beat ini tiba.")]
        Auto,
        [Tooltip("Jalan saat pemain menekan E pada objek Target.")]
        Interact,
        [Tooltip("Jalan saat pemain masuk ke area Target (collider trigger).")]
        EnterArea
    }

    [Serializable]
    public class WaitingHint
    {
        public GameObject target;
        public string prompt = "[E] Periksa";
        public InkFile dialogue;
    }

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
    /// Satu-satunya tempat urutan cerita. Beat dijalankan linear; beat berikutnya
    /// baru aktif setelah beat sebelumnya selesai.
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

        public StoryBeat CurrentBeat => index >= 0 && index < beats.Count ? beats[index] : null;

        private void Awake()
        {
            SetupBeatObjects();
        }

        /// <summary>Dipakai chapter yang menyusun beat lewat kode.</summary>
        public void LoadBeats(List<StoryBeat> newBeats)
        {
            beats = newBeats ?? new List<StoryBeat>();
            SetupBeatObjects();
        }

        private void SetupBeatObjects()
        {
            foreach (StoryBeat beat in beats)
            {
                if (beat == null) continue;

                if (beat.trigger == BeatTrigger.Interact) EnsureTarget(beat.target);
                if (beat.trigger == BeatTrigger.EnterArea) EnsureZone(beat.target);

                if (beat.hints == null) continue;
                foreach (WaitingHint hint in beat.hints)
                {
                    if (hint != null) EnsureTarget(hint.target);
                }
            }
        }

        private void Start()
        {
            index = Mathf.Clamp(startAtBeat, 0, beats.Count);
            EnterBeat();
        }

        private void EnsureTarget(GameObject go)
        {
            if (go == null) return;

            StoryTarget target = go.GetComponent<StoryTarget>();
            if (target == null) target = go.AddComponent<StoryTarget>();
            target.director = this;
        }

        private void EnsureZone(GameObject go)
        {
            if (go == null) return;

            StoryZone zone = go.GetComponent<StoryZone>();
            if (zone == null) zone = go.AddComponent<StoryZone>();
            zone.director = this;

            Collider2D collider = go.GetComponent<Collider2D>();
            if (collider != null) collider.isTrigger = true;
            else Debug.LogWarning("[StoryDirector] Area '" + go.name + "' butuh Collider2D.", go);
        }

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
            if (beat == null || busy) return;

            busy = true;
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

            if (beat.hideTargetAfter && beat.target != null)
                beat.target.SetActive(false);

            beat.onFinished?.Invoke();

            index++;
            EnterBeat();
        }

        private void Abort(StoryBeat beat)
        {
            if (beat.showPanel != null) beat.showPanel.SetActive(false);
            busy = false;
        }

        private WaitingHint FindHint(GameObject go)
        {
            StoryBeat beat = CurrentBeat;
            if (beat == null) return null;
            if (beat.hints == null) return null;

            foreach (WaitingHint hint in beat.hints)
            {
                if (hint != null && hint.target == go && hint.dialogue != null)
                    return hint;
            }

            return null;
        }

        private bool IsMainTarget(GameObject go)
        {
            StoryBeat beat = CurrentBeat;
            return beat != null && beat.trigger == BeatTrigger.Interact && beat.target == go;
        }

        public bool CanInteractWith(StoryTarget target)
        {
            if (target == null || busy) return false;
            if (dialogueManager != null && dialogueManager.IsDialogueActive) return false;
            return IsMainTarget(target.gameObject) || FindHint(target.gameObject) != null;
        }

        public string PromptFor(StoryTarget target)
        {
            if (target == null) return "";
            if (IsMainTarget(target.gameObject)) return CurrentBeat.prompt;

            WaitingHint hint = FindHint(target.gameObject);
            return hint != null ? hint.prompt : "";
        }

        public void Interacted(StoryTarget target)
        {
            if (!CanInteractWith(target)) return;

            if (IsMainTarget(target.gameObject))
            {
                Run(CurrentBeat);
                return;
            }

            WaitingHint hint = FindHint(target.gameObject);
            if (hint != null && dialogueManager != null)
            {
                busy = true;
                dialogueManager.StartDialogue(hint.dialogue, () => busy = false);
            }
        }

        public void ZoneEntered(StoryZone zone)
        {
            StoryBeat beat = CurrentBeat;
            if (zone == null || busy || beat == null) return;
            if (beat.trigger != BeatTrigger.EnterArea || beat.target != zone.gameObject) return;

            Run(beat);
        }
    }
}

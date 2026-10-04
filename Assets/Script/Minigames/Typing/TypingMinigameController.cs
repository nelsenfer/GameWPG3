using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;

namespace Game.Typing
{
    /// <summary>
    /// Minigame mengetik. Alur: Open() -> Idle (nunggu huruf pertama) -> Running
    /// (timer jalan) -> Finished (timer habis, tampil hasil) -> Close().
    ///
    /// Aturan:
    /// - Salah ketik boleh: huruf jadi merah dan kursor tetap maju. Backspace
    ///   menghapus huruf terakhir (hitungannya ikut dibatalkan), tapi hanya di
    ///   dalam baris yang sedang tampil.
    /// - Timer mulai saat huruf pertama diketik, dan SELALU jalan sampai habis.
    /// - Lulus kalau huruf yang diketik >= minTypedChars DAN akurasi >= minAccuracyPercent.
    /// - Kalau lulus, onPassed dipanggil saat tombol Tutup ditekan.
    /// - Escape menutup minigame saat belum mulai atau saat layar hasil.
    ///
    /// Taruh script ini di GameObject yang SELALU aktif (misal di ==Manager==).
    /// panelRoot adalah GameObject terpisah (child Canvas) yang di-show/hide.
    /// </summary>
    public class TypingMinigameController : MonoBehaviour
    {
        private enum State { Closed, Idle, Running, Finished }

        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private GameObject panelRoot;

        [Header("Tampilan Mengetik")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private TMP_Text timerText;
        [Tooltip("Opsional: petunjuk 'mulai ketik untuk memulai', otomatis hilang saat timer jalan.")]
        [SerializeField] private GameObject startHint;

        [Header("Layar Hasil")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultStatusText;
        [SerializeField] private TMP_Text resultWpmText;
        [SerializeField] private TMP_Text resultAccuracyText;
        [SerializeField] private TMP_Text resultCharsText;
        [SerializeField] private TMP_Text resultMessageText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button closeButton;

        [Header("Warna Huruf")]
        [SerializeField] private Color correctColor = new Color(0.3f, 0.8f, 0.4f);
        [SerializeField] private Color wrongColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color pendingColor = new Color(0.6f, 0.6f, 0.6f);

        private State state = State.Closed;
        private TypingTaskData task;
        private Action onPassedCallback;

        private string currentText;
        private bool[] results;
        private int cursor;
        private int[] order;
        private int orderPos;

        private int correctCount;
        private int wrongCount;
        private float remaining;
        private bool passed;
        private int openedFrame;

        private string correctHex, wrongHex, pendingHex, wrongMarkHex;

        public bool IsOpen => state != State.Closed;

        private void Awake()
        {
            correctHex = "#" + ColorUtility.ToHtmlStringRGB(correctColor);
            wrongHex = "#" + ColorUtility.ToHtmlStringRGB(wrongColor);
            pendingHex = "#" + ColorUtility.ToHtmlStringRGB(pendingColor);
            wrongMarkHex = "#" + ColorUtility.ToHtmlStringRGB(wrongColor) + "66";

            if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        /// <summary>Buka minigame untuk 1 task. onPassed dipanggil kalau pemain lulus dan menutup layar hasil.</summary>
        public void Open(TypingTaskData data, Action onPassed)
        {
            if (data == null || data.texts == null || data.texts.Count == 0)
            {
                Debug.LogError("[TypingMinigame] TypingTaskData kosong atau belum punya teks.", this);
                return;
            }

            task = data;
            onPassedCallback = onPassed;
            openedFrame = Time.frameCount; // abaikan input di frame yang sama dengan tombol E

            playerController?.SetFrozen(true);
            if (panelRoot != null) panelRoot.SetActive(true);
            if (titleText != null) titleText.text = data.taskTitle;

            ResetRound();
        }

        private void Update()
        {
            if (state == State.Closed) return;
            if (Time.frameCount <= openedFrame) return;

            if ((state == State.Idle || state == State.Finished) && Input.GetKeyDown(KeyCode.Escape))
            {
                OnCloseClicked();
                return;
            }

            if (state == State.Idle || state == State.Running)
                ReadTypedCharacters();

            if (state == State.Running)
            {
                remaining -= Time.deltaTime;
                if (remaining <= 0f)
                {
                    remaining = 0f;
                    UpdateTimerText();
                    Finish();
                }
                else
                {
                    UpdateTimerText();
                }
            }
        }

        private void ReadTypedCharacters()
        {
            foreach (char c in Input.inputString)
            {
                if (c == '\b')
                {
                    HandleBackspace();
                    continue;
                }

                if (char.IsControl(c)) continue; // enter, escape, dll diabaikan

                if (state == State.Idle) StartRound();
                HandleChar(c);
            }
        }

        private void StartRound()
        {
            state = State.Running;
            if (startHint != null) startHint.SetActive(false);
        }

        private void HandleChar(char c)
        {
            if (cursor >= currentText.Length) return;

            bool ok = c == currentText[cursor];
            results[cursor] = ok;
            if (ok) correctCount++; else wrongCount++;
            cursor++;

            if (cursor >= currentText.Length)
            {
                AdvanceText();
            }
            else
            {
                RefreshDisplay();
            }
        }

        private void HandleBackspace()
        {
            if (state != State.Running || cursor <= 0) return;

            cursor--;
            if (results[cursor]) correctCount--; else wrongCount--;
            results[cursor] = false;
            RefreshDisplay();
        }

        private void Finish()
        {
            state = State.Finished;

            int typed = correctCount + wrongCount;
            float accuracy = typed > 0 ? correctCount * 100f / typed : 0f;
            float minutes = task.durationSeconds / 60f;
            float wpm = (correctCount / 5f) / minutes; // standar: 5 huruf = 1 kata

            passed = typed >= task.minTypedChars && accuracy >= task.minAccuracyPercent;

            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultStatusText != null) resultStatusText.text = passed ? "LULUS" : "BELUM CUKUP";
            if (resultWpmText != null) resultWpmText.text = $"{wpm:0} WPM";
            if (resultAccuracyText != null) resultAccuracyText.text = $"Akurasi {accuracy:0}% (min {task.minAccuracyPercent:0}%)";
            if (resultCharsText != null) resultCharsText.text = $"Huruf {typed} (min {task.minTypedChars})  |  Benar {correctCount}  Salah {wrongCount}";
            if (resultMessageText != null) resultMessageText.text = passed ? task.passMessage : task.failMessage;

            if (retryButton != null) retryButton.gameObject.SetActive(!passed);
        }

        private void ResetRound()
        {
            state = State.Idle;
            correctCount = 0;
            wrongCount = 0;
            BuildOrder();
            passed = false;
            remaining = task.durationSeconds;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (startHint != null) startHint.SetActive(true);

            LoadText();
            UpdateTimerText();
        }

        /// <summary>Menyusun urutan baris (diacak kalau randomizeOrder menyala).</summary>
        private void BuildOrder()
        {
            int n = task.texts.Count;
            int previousLast = (order != null && order.Length == n) ? order[n - 1] : -1;

            order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;

            if (task.randomizeOrder)
            {
                for (int i = n - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }

                // Hindari baris yang sama muncul dua kali berturut-turut saat pindah putaran.
                if (n > 1 && order[0] == previousLast)
                    (order[0], order[1]) = (order[1], order[0]);
            }

            orderPos = 0;
        }

        private void AdvanceText()
        {
            orderPos++;
            if (orderPos >= order.Length) BuildOrder();
            LoadText();
        }

        private void LoadText()
        {
            // Enter tidak dihitung sebagai huruf, jadi baris baru diganti spasi.
            currentText = task.texts[order[orderPos]].Replace("\r", "").Replace("\n", " ").Trim();
            results = new bool[currentText.Length];
            cursor = 0;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (targetText == null) return;

            var sb = new StringBuilder(currentText.Length * 20);
            for (int i = 0; i < currentText.Length; i++)
            {
                char c = currentText[i];

                if (i < cursor)
                {
                    if (results[i])
                        sb.Append("<color=").Append(correctHex).Append('>').Append(c).Append("</color>");
                    else if (c == ' ')
                        sb.Append("<mark=").Append(wrongMarkHex).Append("> </mark>");
                    else
                        sb.Append("<color=").Append(wrongHex).Append('>').Append(c).Append("</color>");
                }
                else if (i == cursor)
                {
                    sb.Append("<u>").Append(c).Append("</u>");
                }
                else
                {
                    sb.Append("<color=").Append(pendingHex).Append('>').Append(c).Append("</color>");
                }
            }

            targetText.text = sb.ToString();
        }

        private void UpdateTimerText()
        {
            if (timerText != null) timerText.text = Mathf.CeilToInt(remaining).ToString();
        }

        private void OnRetryClicked()
        {
            if (state != State.Finished) return;
            openedFrame = Time.frameCount;
            ResetRound();
        }

        private void OnCloseClicked()
        {
            bool wasPassed = state == State.Finished && passed;
            Action callback = onPassedCallback;

            state = State.Closed;
            onPassedCallback = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            playerController?.SetFrozen(false);

            if (wasPassed) callback?.Invoke();
        }
    }
}
using System;
using System.Collections;
using UnityEngine;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Mekanik mengetik balasan pemain (tahan tombol untuk mengetik).
    /// Hanya dipakai untuk pesan pemain; pesan lawan bicara tidak lewat sini.
    /// </summary>
    [Serializable]
    public class PhoneReplyTyper
    {
        [SerializeField] private bool requireHoldToType = true;
        [Tooltip("Berapa lama pemain harus menahan tombol sebelum teks mulai diketik.")]
        [SerializeField] private float holdSeconds = 0.35f;
        [SerializeField] private float charsPerSecond = 35f;
        [Tooltip("Jeda sebelum mengetik otomatis (jika Require Hold mati).")]
        [SerializeField] private float autoTypeDelay = 0.4f;
        [Tooltip("Jeda setelah huruf pertama muncul.")]
        [SerializeField] private float firstCharPause = 0f;
        [Tooltip("Jeda setelah selesai diketik sebelum dikirim.")]
        [SerializeField] private float sendDelay = 0.17f;

        public IEnumerator Run(string full, PhoneChatView view, Func<bool> isBlocked)
        {
            full = full ?? string.Empty;
            view.SetInputText(string.Empty);

            if (requireHoldToType)
            {
                view.SetHoldFill(0f);
                view.SetHoldHint(true);
                while (Input.anyKey) yield return null; // tunggu tombol sebelumnya dilepas
            }
            else if (autoTypeDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(autoTypeDelay);
            }

            int total = full.Length;
            float cps = Mathf.Max(1f, charsPerSecond);
            float shown = 0f;
            float holdTimer = 0f;
            int lastCount = -1;
            bool typing = !requireHoldToType;

            while (shown < total)
            {
                if (requireHoldToType)
                {
                    bool holding = Input.anyKey && (isBlocked == null || !isBlocked());
                    if (holding)
                    {
                        holdTimer += Time.unscaledDeltaTime;
                        view.SetHoldFill(holdSeconds > 0f ? holdTimer / holdSeconds : 1f);
                        if (!typing && holdTimer >= holdSeconds)
                        {
                            typing = true;
                            view.SetHoldHint(false);
                        }
                    }
                    else
                    {
                        holdTimer = 0f;
                        view.SetHoldFill(0f);
                        if (typing)
                        {
                            typing = false;
                            view.SetHoldHint(true); // dilepas = pause
                        }
                    }
                }

                if (typing)
                {
                    bool isFirst = lastCount < 0;
                    shown = isFirst ? 1f : shown + cps * Time.unscaledDeltaTime;
                    int count = Mathf.Min(total, Mathf.FloorToInt(shown));

                    if (count != lastCount)
                    {
                        lastCount = count;
                        view.SetInputText(full.Substring(0, SafeLength(full, count)));
                    }

                    if (isFirst && firstCharPause > 0f)
                        yield return new WaitForSecondsRealtime(firstCharPause);
                }

                yield return null;
            }

            view.SetHoldHint(false);
            view.SetInputText(full);
            if (sendDelay > 0f) yield return new WaitForSecondsRealtime(sendDelay);
        }

        /// <summary>Jangan memotong di tengah tag rich text.</summary>
        private static int SafeLength(string s, int count)
        {
            if (count <= 0) return 0;
            if (count >= s.Length) return s.Length;

            int open = s.LastIndexOf('<', count - 1);
            if (open < 0 || s.LastIndexOf('>', count - 1) > open) return count;

            int close = s.IndexOf('>', open);
            return close >= 0 ? close + 1 : count;
        }
    }
}
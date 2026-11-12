using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Game.Core;

namespace Game.Progression
{
    /// <summary>
    /// Transisi ala "Space Dive" (A Space for the Unbound) saat lonceng dibunyikan.
    /// Mengubah pencahayaan dan latar menjadi nuansa Dunia Lain (biru keruh/muram).
    /// </summary>
    public class SpaceDiveTransition : MinigameLauncher
    {
        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Light2D globalLight;
        [SerializeField] private SpriteRenderer officeBackground;
        [SerializeField] private GameObject otherWorldBackdrop;
        [SerializeField] private GameObject boxMinigameRoot;

        [Header("Warna & Atmosfer Dunia Lain")]
        [SerializeField] private Color otherWorldLightColor = new Color(0.32f, 0.52f, 0.68f, 1f);
        [SerializeField] private float otherWorldLightIntensity = 0.82f;
        [SerializeField] private Color otherWorldBgTint = new Color(0.16f, 0.27f, 0.38f, 1f);
        [SerializeField] private Color otherWorldBackdropColor = new Color(0.07f, 0.14f, 0.21f, 1f);

        [Header("Timing & Kamera")]
        [SerializeField] private float zoomInDuration = 0.45f;
        [SerializeField] private float blackoutDuration = 0.25f;
        [SerializeField] private float zoomOutDuration = 0.65f;
        [SerializeField] private float zoomOrthoDelta = 0.85f;
        [SerializeField] private float shakeMagnitude = 0.12f;

        [Header("Audio (Opsional)")]
        [SerializeField] private string bellSfxId = "Click";
        [SerializeField] private string otherWorldBgmId = "";

        private bool isRunning;
        public override bool IsOpen => isRunning;

        private void Awake()
        {
            if (otherWorldBackdrop != null)
                otherWorldBackdrop.SetActive(false);
        }

        public override void Launch(Action onSuccess)
        {
            if (isRunning) return;
            StartCoroutine(DiveRoutine(onSuccess));
        }

        private IEnumerator DiveRoutine(Action onSuccess)
        {
            isRunning = true;

            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (globalLight == null)
            {
                GameObject lightObj = GameObject.Find("Global Light 2D");
                if (lightObj != null) globalLight = lightObj.GetComponent<Light2D>();
                if (globalLight == null) globalLight = FindFirstObjectByType<Light2D>();
            }
            if (officeBackground == null)
            {
                GameObject bgObj = GameObject.Find("background");
                if (bgObj != null) officeBackground = bgObj.GetComponent<SpriteRenderer>();
            }

            if (player != null) player.SetFrozen(true);

            // Hentikan BGM kantor dan bunyikan denting lonceng
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM();
                if (!string.IsNullOrEmpty(bellSfxId))
                    AudioManager.Instance.PlaySFX(bellSfxId);
            }

            Camera cam = Camera.main;
            float baseOrthoSize = cam != null ? cam.orthographicSize : 5f;
            float targetOrthoSize = Mathf.Max(2f, baseOrthoSize - zoomOrthoDelta);
            float baseLightIntensity = globalLight != null ? globalLight.intensity : 1f;
            Color baseLightColor = globalLight != null ? globalLight.color : Color.white;

            // Tahap 1: Denting lonceng -> kamera bergetar & zoom-in, cahaya berpendar biru-putih
            float elapsed = 0f;
            while (elapsed < zoomInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / zoomInDuration);
                float smoothT = t * t;

                if (cam != null)
                {
                    cam.orthographicSize = Mathf.Lerp(baseOrthoSize, targetOrthoSize, smoothT);
                    Vector2 shake = UnityEngine.Random.insideUnitCircle * (shakeMagnitude * (1f - t * 0.5f));
                    cam.transform.position += new Vector3(shake.x, shake.y, 0f);
                }

                if (globalLight != null)
                {
                    globalLight.color = Color.Lerp(baseLightColor, new Color(0.65f, 0.9f, 1f, 1f), smoothT);
                    globalLight.intensity = Mathf.Lerp(baseLightIntensity, 1.75f, smoothT);
                }

                yield return null;
            }

            // Tahap 2: Glitch / Blackout singkat tepat di puncak Space Dive
            if (globalLight != null)
            {
                globalLight.intensity = 0.02f;
            }
            yield return new WaitForSeconds(blackoutDuration * 0.5f);

            if (globalLight != null)
            {
                globalLight.intensity = 1.4f;
            }
            yield return new WaitForSeconds(0.05f);

            if (globalLight != null)
            {
                globalLight.intensity = 0f;
            }

            // Ubah dunia menjadi Dunia Lain (biru keruh/muram) saat gelap
            ApplyOtherWorldVisuals();

            yield return new WaitForSeconds(blackoutDuration * 0.5f);

            // Tahap 3: Layar menyala kembali di Dunia Lain + kamera zoom-out halus
            elapsed = 0f;
            while (elapsed < zoomOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / zoomOutDuration);
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);

                if (cam != null)
                {
                    cam.orthographicSize = Mathf.Lerp(targetOrthoSize, baseOrthoSize, smoothT);
                }

                if (globalLight != null)
                {
                    globalLight.color = otherWorldLightColor;
                    globalLight.intensity = Mathf.Lerp(0.08f, otherWorldLightIntensity, smoothT);
                }

                yield return null;
            }

            if (cam != null) cam.orthographicSize = baseOrthoSize;
            if (globalLight != null)
            {
                globalLight.color = otherWorldLightColor;
                globalLight.intensity = otherWorldLightIntensity;
            }

            if (AudioManager.Instance != null && !string.IsNullOrEmpty(otherWorldBgmId))
            {
                AudioManager.Instance.PlayBGM(otherWorldBgmId);
            }

            if (player != null) player.SetFrozen(false);
            isRunning = false;
            onSuccess?.Invoke();
        }

        private void ApplyOtherWorldVisuals()
        {
            if (officeBackground != null)
            {
                officeBackground.color = otherWorldBgTint;
            }

            if (otherWorldBackdrop != null)
            {
                otherWorldBackdrop.SetActive(true);
                SpriteRenderer sr = otherWorldBackdrop.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = otherWorldBackdropColor;
            }

            if (boxMinigameRoot != null)
            {
                boxMinigameRoot.SetActive(true);
            }
        }
    }
}

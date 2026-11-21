using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Game.Core;

namespace Game.Progression
{
    /// <summary>
    /// Transisi ala "Space Dive" (A Space for the Unbound) saat lonceng dibunyikan.
    /// Mendukung transisi dua arah:
    /// 1. Masuk ke Dunia Lain (Dive) - pencahayaan dan latar berubah jadi biru keruh/muram.
    /// 2. Kembali ke Dunia Nyata (Emerge) - pencahayaan dan latar kembali ke kantor normal.
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
        [SerializeField] private string bellSfxId = "Bell";
        [SerializeField] private string otherWorldBgmId = "Ambience";

        private bool isRunning;
        private bool isInOtherWorld;
        private Color originalLightColor = Color.white;
        private float originalLightIntensity = 1f;
        private Color originalBgColor = Color.white;

        public override bool IsOpen => isRunning;
        public bool IsInOtherWorld => isInOtherWorld;

        private void Awake()
        {
            if (otherWorldBackdrop != null)
                otherWorldBackdrop.SetActive(false);

            EnsureReferences();

            if (globalLight != null)
            {
                originalLightColor = globalLight.color;
                originalLightIntensity = globalLight.intensity;
            }

            if (officeBackground != null)
            {
                originalBgColor = officeBackground.color;
            }
        }

        private void EnsureReferences()
        {
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
            if (otherWorldBackdrop == null)
            {
                otherWorldBackdrop = GameObject.Find("Latar_DuniaLain");
            }
            if (boxMinigameRoot == null)
            {
                GameObject boxObj = GameObject.Find("Minigame_Kotak_DuniaLain");
                if (boxObj != null) boxMinigameRoot = boxObj;
            }
        }

        public override void Launch(Action onSuccess)
        {
            DiveToOtherWorld(onSuccess);
        }

        public void DiveToOtherWorld(Action onSuccess = null)
        {
            if (isRunning) return;
            StartCoroutine(DiveRoutine(onSuccess));
        }

        public void EmergeToRealWorld(Action onSuccess = null)
        {
            if (isRunning) return;
            StartCoroutine(EmergeRoutine(onSuccess));
        }

        private IEnumerator DiveRoutine(Action onSuccess)
        {
            isRunning = true;
            EnsureReferences();

            if (player != null) player.SetFrozen(true);

            // Denting lonceng & audio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM();
                string sfx = (string.IsNullOrEmpty(bellSfxId) || bellSfxId == "Click") ? "Bell" : bellSfxId;
                AudioManager.Instance.PlaySFX(sfx);
            }

            Camera cam = Camera.main;
            float baseOrthoSize = cam != null ? cam.orthographicSize : 5f;
            float targetOrthoSize = Mathf.Max(2f, baseOrthoSize - zoomOrthoDelta);
            float baseLightIntensity = globalLight != null ? globalLight.intensity : originalLightIntensity;
            Color baseLightColor = globalLight != null ? globalLight.color : originalLightColor;

            // Tahap 1: Kamera bergetar & zoom-in, cahaya berpendar
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

            // Tahap 2: Glitch / Blackout singkat di puncak Space Dive
            if (globalLight != null) globalLight.intensity = 0.02f;
            yield return new WaitForSeconds(blackoutDuration * 0.5f);

            if (globalLight != null) globalLight.intensity = 1.4f;
            yield return new WaitForSeconds(0.05f);

            if (globalLight != null) globalLight.intensity = 0f;

            // Terapkan visual Dunia Lain saat gelap
            ApplyOtherWorldVisuals();
            isInOtherWorld = true;

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

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAmbience(
                    string.IsNullOrEmpty(otherWorldBgmId) ? "Ambience" : otherWorldBgmId
                );
            }

            if (player != null) player.SetFrozen(false);
            isRunning = false;
            onSuccess?.Invoke();
        }

        private IEnumerator EmergeRoutine(Action onSuccess)
        {
            isRunning = true;
            EnsureReferences();

            if (player != null) player.SetFrozen(true);

            // Denting lonceng saat kembali
            if (AudioManager.Instance != null)
            {
                string sfx = (string.IsNullOrEmpty(bellSfxId) || bellSfxId == "Click") ? "Bell" : bellSfxId;
                AudioManager.Instance.PlaySFX(sfx);
            }

            Camera cam = Camera.main;
            float baseOrthoSize = cam != null ? cam.orthographicSize : 5f;
            float targetOrthoSize = Mathf.Max(2f, baseOrthoSize - zoomOrthoDelta);

            // Tahap 1: Kamera bergetar & zoom-in halus
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

                yield return null;
            }

            // Tahap 2: Blackout / Glitch singkat
            if (globalLight != null) globalLight.intensity = 0.02f;
            yield return new WaitForSeconds(blackoutDuration * 0.5f);

            if (globalLight != null) globalLight.intensity = 1.2f;
            yield return new WaitForSeconds(0.04f);

            if (globalLight != null) globalLight.intensity = 0f;

            // Kembalikan visual ke kantor normal saat gelap
            ApplyRealWorldVisuals();
            isInOtherWorld = false;

            yield return new WaitForSeconds(blackoutDuration * 0.5f);

            // Tahap 3: Layar terang kembali ke kantor + kamera zoom-out
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
                    globalLight.color = Color.Lerp(otherWorldLightColor, originalLightColor, smoothT);
                    globalLight.intensity = Mathf.Lerp(0.08f, originalLightIntensity, smoothT);
                }

                yield return null;
            }

            if (cam != null) cam.orthographicSize = baseOrthoSize;
            if (globalLight != null)
            {
                globalLight.color = originalLightColor;
                globalLight.intensity = originalLightIntensity;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM("MainBgm");
                AudioManager.Instance.PlayAmbience();
            }

            if (player != null) player.SetFrozen(false);
            isRunning = false;
            onSuccess?.Invoke();
        }

        public void ApplyOtherWorldVisuals()
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

        public void ApplyRealWorldVisuals()
        {
            if (officeBackground != null)
            {
                officeBackground.color = originalBgColor;
            }

            if (otherWorldBackdrop != null)
            {
                otherWorldBackdrop.SetActive(false);
            }

            if (boxMinigameRoot != null)
            {
                boxMinigameRoot.SetActive(false);
            }
        }
    }
}

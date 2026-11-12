using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Game.Core;

namespace Game.Progression
{
    /// <summary>
    /// Minigame side-scrolling di Dunia Lain:
    /// Pemain mengambil 4 kotak (A, B, C, D) satu per satu dan meletakkannya di 4 tempat (A, B, C, D).
    /// Jika kotak diletakkan di tempat yang benar, indikator kotak bercahaya di atas tempat itu menyala.
    /// Jika salah, kotak tetap di tempat itu (tanpa indikator menyala) dan bisa diambil lagi dengan [E].
    /// 
    /// Posisi kotak dan tempat bisa digeser sesuka hati langsung di Unity Editor (Scene View).
    /// </summary>
    public class BoxSortingMinigame : MinigameLauncher
    {
        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Sprite squareSprite;
        [SerializeField] private TMP_FontAsset fontAsset;

        [Header("Scene Objects (Daftar Tempat & Kotak di Scene)")]
        [Tooltip("Daftar 4 Tempat/Slot. Jika kosong, akan otomatis mencari child Tempat_A s/d Tempat_D.")]
        [SerializeField] private List<BoxSlotInteractable> slots = new List<BoxSlotInteractable>();
        [Tooltip("Daftar 4 Kotak. Jika kosong, akan otomatis mencari child Kotak_A s/d Kotak_D.")]
        [SerializeField] private List<SortableBoxInteractable> boxes = new List<SortableBoxInteractable>();

        [Header("Visual Settings")]
        [SerializeField] private Color boxColor = new Color(0.28f, 0.44f, 0.58f, 1f);
        [SerializeField] private Color boxBorderColor = new Color(0.62f, 0.82f, 0.96f, 1f);
        [SerializeField] private Color slotColor = new Color(0.12f, 0.20f, 0.28f, 0.85f);
        [SerializeField] private Color indicatorOffColor = new Color(0.18f, 0.24f, 0.30f, 0.55f);
        [SerializeField] private Color indicatorOnColor = new Color(0.40f, 1.00f, 0.88f, 1f);
        [SerializeField] private Vector3 carryOffset = new Vector3(0f, 1.45f, 0f);

        private static readonly string[] Ids = { "A", "B", "C", "D" };

        private SortableBoxInteractable carriedBox;
        private bool isRunning;
        private bool isCompleting;
        private Action onSuccessCallback;

        public override bool IsOpen => isRunning;
        public bool IsActiveMinigame => isRunning && !isCompleting;
        public SortableBoxInteractable CarriedBox => carriedBox;

        private void Awake()
        {
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (fontAsset == null) fontAsset = TMP_Settings.defaultFontAsset;

            SetupMinigameObjects();
        }

        public void SetupMinigameObjects()
        {
            slots.Clear();
            boxes.Clear();

            // 1. Cari atau hubungkan Tempat_A s/d Tempat_D
            for (int i = 0; i < Ids.Length; i++)
            {
                string id = Ids[i];
                Transform slotTr = transform.Find("Tempat_" + id);

                if (slotTr != null)
                {
                    EnsureSlotVisuals(slotTr.gameObject, id);

                    BoxSlotInteractable slot = slotTr.GetComponent<BoxSlotInteractable>();
                    if (slot == null) slot = slotTr.gameObject.AddComponent<BoxSlotInteractable>();

                    SpriteRenderer indSr = null;
                    Transform indTr = slotTr.Find("Indikator");
                    if (indTr != null) indSr = indTr.GetComponent<SpriteRenderer>();

                    slot.Initialize(this, id, indSr, indicatorOffColor, indicatorOnColor);
                    slots.Add(slot);
                }
            }

            // 2. Cari atau hubungkan Kotak_A s/d Kotak_D
            for (int i = 0; i < Ids.Length; i++)
            {
                string id = Ids[i];
                Transform boxTr = transform.Find("Kotak_" + id);

                if (boxTr != null)
                {
                    EnsureBoxVisuals(boxTr.gameObject, id);

                    SortableBoxInteractable box = boxTr.GetComponent<SortableBoxInteractable>();
                    if (box == null) box = boxTr.gameObject.AddComponent<SortableBoxInteractable>();

                    BoxCollider2D col = boxTr.GetComponent<BoxCollider2D>();
                    box.Initialize(this, id, col);
                    boxes.Add(box);
                }
            }
        }

        private void EnsureSlotVisuals(GameObject go, string id)
        {
            go.layer = 6; // Interactable layer

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            if (sr.sprite == null && squareSprite != null) sr.sprite = squareSprite;
            if (sr.color == Color.white) sr.color = slotColor;
            sr.sortingOrder = 4;

            BoxCollider2D col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = new Vector2(0f, 0.55f);
            col.size = new Vector2(1.15f, 2.2f);

            // Label text di bawah pedestal
            Transform labelTr = go.transform.Find("Label");
            if (labelTr == null)
            {
                GameObject labelGo = new GameObject("Label");
                labelGo.transform.SetParent(go.transform, false);
                labelGo.transform.localPosition = new Vector3(0f, -0.72f, 0f);
                labelTr = labelGo.transform;
            }
            TextMeshPro tmp = labelTr.GetComponent<TextMeshPro>();
            if (tmp == null) tmp = labelTr.gameObject.AddComponent<TextMeshPro>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = id;
            tmp.fontSize = 3.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.65f, 0.82f, 0.95f, 0.75f);
            tmp.rectTransform.sizeDelta = new Vector2(2f, 1f);
            MeshRenderer mr = tmp.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 5;

            // Indikator kotak kecil di atas slot
            Transform indTr = go.transform.Find("Indikator");
            if (indTr == null)
            {
                GameObject indGo = new GameObject("Indikator");
                indGo.transform.SetParent(go.transform, false);
                indGo.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                indGo.transform.localScale = new Vector3(0.36f, 0.36f, 1f);
                indTr = indGo.transform;
            }
            SpriteRenderer indSr = indTr.GetComponent<SpriteRenderer>();
            if (indSr == null) indSr = indTr.gameObject.AddComponent<SpriteRenderer>();
            if (indSr.sprite == null && squareSprite != null) indSr.sprite = squareSprite;
            indSr.color = indicatorOffColor;
            indSr.sortingOrder = 8;
        }

        private void EnsureBoxVisuals(GameObject go, string id)
        {
            go.layer = 6; // Interactable layer

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            if (sr.sprite == null && squareSprite != null) sr.sprite = squareSprite;
            if (sr.color == Color.white) sr.color = boxColor;
            sr.sortingOrder = 6;

            BoxCollider2D col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = new Vector2(0f, 0.65f);
            col.size = new Vector2(1.1f, 2.3f);

            // Inner box
            Transform borderTr = go.transform.Find("InnerBox");
            if (borderTr == null)
            {
                GameObject borderGo = new GameObject("InnerBox");
                borderGo.transform.SetParent(go.transform, false);
                borderGo.transform.localPosition = Vector3.zero;
                borderGo.transform.localScale = new Vector3(0.84f, 0.84f, 1f);
                borderTr = borderGo.transform;
            }
            SpriteRenderer innerSr = borderTr.GetComponent<SpriteRenderer>();
            if (innerSr == null) innerSr = borderTr.gameObject.AddComponent<SpriteRenderer>();
            if (innerSr.sprite == null && squareSprite != null) innerSr.sprite = squareSprite;
            innerSr.color = new Color(boxColor.r * 0.72f, boxColor.g * 0.72f, boxColor.b * 0.72f, 1f);
            innerSr.sortingOrder = 7;

            // Huruf
            Transform textTr = go.transform.Find("Huruf");
            if (textTr == null)
            {
                GameObject textGo = new GameObject("Huruf");
                textGo.transform.SetParent(go.transform, false);
                textGo.transform.localPosition = Vector3.zero;
                textTr = textGo.transform;
            }
            TextMeshPro tmp = textTr.GetComponent<TextMeshPro>();
            if (tmp == null) tmp = textTr.gameObject.AddComponent<TextMeshPro>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = id;
            tmp.fontSize = 5.2f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = boxBorderColor;
            tmp.rectTransform.sizeDelta = new Vector2(1.5f, 1.5f);
            MeshRenderer mr = tmp.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 8;
        }

        public override void Launch(Action onSuccess)
        {
            gameObject.SetActive(true);
            onSuccessCallback = onSuccess;
            isRunning = true;
            isCompleting = false;
            carriedBox = null;

            // Reset status semua slot dan kotak
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null) slots[i].ResetSlot();
            }

            if (player != null) player.SetFrozen(false);
        }

        private void LateUpdate()
        {
            if (carriedBox != null && player != null)
            {
                carriedBox.transform.position = player.transform.position + carryOffset;
            }

            // Efek denyut cahaya halus untuk indikator yang sudah benar
            float pulse = 0.88f + 0.12f * Mathf.Sin(Time.time * 4.5f);
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null)
                    slots[i].UpdatePulse(pulse);
            }
        }

        public void PickUpLooseBox(SortableBoxInteractable box)
        {
            if (!IsActiveMinigame || box == null || carriedBox != null) return;

            carriedBox = box;
            box.SetCarried(true);
            PlaySfx();
        }

        public void InteractWithSlot(BoxSlotInteractable slot)
        {
            if (!IsActiveMinigame || slot == null) return;

            // 1. Sedang membawa kotak dan slot kosong -> letakkan kotak ke slot
            if (carriedBox != null && slot.PlacedBox == null)
            {
                SortableBoxInteractable boxToPlace = carriedBox;
                carriedBox = null;
                slot.PlaceBox(boxToPlace);
                PlaySfx();
                CheckAllCorrect();
                return;
            }

            // 2. Tidak membawa kotak dan slot terisi -> ambil kembali kotak dari slot
            if (carriedBox == null && slot.PlacedBox != null)
            {
                SortableBoxInteractable taken = slot.TakeBox();
                carriedBox = taken;
                taken.SetCarried(true);
                PlaySfx();
                return;
            }

            // 3. Sedang membawa kotak dan slot sudah terisi -> tukar kotak
            if (carriedBox != null && slot.PlacedBox != null)
            {
                SortableBoxInteractable previousBox = slot.TakeBox();
                SortableBoxInteractable newBox = carriedBox;
                slot.PlaceBox(newBox);
                carriedBox = previousBox;
                previousBox.SetCarried(true);
                PlaySfx();
                CheckAllCorrect();
            }
        }

        private void CheckAllCorrect()
        {
            if (carriedBox != null || slots.Count == 0) return;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null || !slots[i].IsCorrect)
                    return;
            }

            // Ke-4 kotak sudah berada di tempat yang benar!
            StartCoroutine(CompleteRoutine());
        }

        private IEnumerator CompleteRoutine()
        {
            isCompleting = true;
            yield return new WaitForSeconds(0.85f);

            isRunning = false;
            isCompleting = false;

            Action cb = onSuccessCallback;
            onSuccessCallback = null;
            cb?.Invoke();
        }

        private void PlaySfx()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("Click");
        }
    }
}

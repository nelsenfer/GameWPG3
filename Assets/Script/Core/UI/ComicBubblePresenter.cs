using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Dialogue.Core;
using Game.Dialogue.Events;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Menampilkan dialog karakter sebagai bubble komik yang mengikuti pembicara.
    /// Posisi dan bentuk bubble tetap dikonfigurasi lewat Unity Editor.
    /// </summary>
    public class ComicBubblePresenter : MonoBehaviour
    {
        [Header("Bubble (satu, dipasang tetap di Canvas)")]
        [Tooltip("RectTransform root bubble (badan). Pivot sebaiknya (0.5, 0) = tengah-bawah.")]
        [SerializeField] private RectTransform bubbleRect;
        [SerializeField] private ChatBubbleUI bubble;
        [SerializeField] private Canvas canvas;

        [Header("Ekor bubble (opsional)")]
        [Tooltip("Gambar ekor, anak dari ComicBubble. Pivot (0.5, 1) = tengah-atas.")]
        [SerializeField] private RectTransform tailRect;
        [SerializeField] private Image tailImage;
        [Tooltip("Jarak minimal ekor dari sudut bubble.")]
        [SerializeField] private float tailEdgeMargin = 40f;
        [Tooltip("Ekor di sudut bubble: bubble melebar menjauh dari pembicara.")]
        [SerializeField] private bool tailAtCorner = true;

        [Header("Posisi")]
        [Tooltip("Kosong = Camera.main.")]
        [SerializeField] private Camera worldCamera;
        [Tooltip("Geser bubble dari titik kepala (piksel layar).")]
        [SerializeField] private Vector2 pixelOffset = new Vector2(0f, 60f);
        [Tooltip("Jarak minimal bubble dari tepi layar.")]
        [SerializeField] private float edgePadding = 20f;
        [Tooltip("Posisi bubble (0-1) kalau pembicara tidak punya Speaker Anchor.")]
        [SerializeField] private Vector2 fallbackViewport = new Vector2(0.5f, 0.75f);

        [Header("References")]
        [SerializeField] private DialogueManager dialogueManager;
        [Tooltip("Selama panel log terbuka, input dialog diabaikan.")]
        [SerializeField] private LogPanelController logPanel;

        [Header("Events")]
        [SerializeField] private DialogueLineEventChannel lineEventChannel;
        [SerializeField] private DialogueChoicesEventChannel choicesEventChannel;
        [SerializeField] private VoidEventChannelSO dialogueEndedChannel;

        private bool visible;
        private bool isChoiceActive;
        private SpeakerAnchor currentAnchor;

        private void Awake()
        {
            if (tailRect == null) return;

            LayoutElement layoutElement = tailRect.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = tailRect.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
        }

        private void OnEnable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised += HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised += HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised += HandleDialogueEnded;
            SetVisible(false);
        }

        private void OnDisable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised -= HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised -= HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised -= HandleDialogueEnded;
        }

        private void Update()
        {
            if (!visible || isChoiceActive) return;
            if (logPanel != null && logPanel.IsOpen) return;

            bool clicked = Input.GetMouseButtonDown(0) && !ActiveDialogueBoxController.IsPointerOverButton();
            if (clicked || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                dialogueManager?.ContinueStory();
        }

        private void LateUpdate()
        {
            if (visible) Reposition();
        }

        private void HandleLine(DialogueLinePayload payload)
        {
            isChoiceActive = false;

            if (payload.style != DialogueStyle.Character)
            {
                SetVisible(false);
                return;
            }

            SpeakerAnchor.TryGet(payload.speaker, out currentAnchor);
            SetVisible(true);
            bubble?.SetData(payload.speaker, payload.text);

            if (tailImage != null && bubble != null)
                tailImage.color = bubble.BackgroundColor;

            Canvas.ForceUpdateCanvases();
            if (bubbleRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRect);
            Reposition();
        }

        private void HandleChoices(System.Collections.Generic.List<Ink.Runtime.Choice> choices)
        {
            isChoiceActive = true;
        }

        private void HandleDialogueEnded()
        {
            isChoiceActive = false;
            currentAnchor = null;
            SetVisible(false);
        }

        private void Reposition()
        {
            if (bubbleRect == null || canvas == null) return;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 speakerScreen = Vector2.zero;
            bool hasTarget = currentAnchor != null && cam != null;
            Vector2 screen;
            Vector2 size = Vector2.Scale(bubbleRect.rect.size, bubbleRect.lossyScale);
            Vector2 pivot = bubbleRect.pivot;

            if (hasTarget)
            {
                Vector3 speakerWorld = cam.WorldToScreenPoint(currentAnchor.BubbleWorldPosition);
                speakerScreen = new Vector2(speakerWorld.x, speakerWorld.y);
                screen = speakerScreen + pixelOffset * canvas.scaleFactor;

                if (tailAtCorner)
                {
                    float sign = speakerScreen.x < Screen.width * 0.5f ? 1f : -1f;
                    float cornerInset = tailEdgeMargin * canvas.scaleFactor;
                    screen.x = speakerScreen.x + pixelOffset.x * canvas.scaleFactor
                             + sign * (size.x * 0.5f - cornerInset);
                }
            }
            else
            {
                screen = new Vector2(Screen.width * fallbackViewport.x, Screen.height * fallbackViewport.y);
            }

            float minX = edgePadding + size.x * pivot.x;
            float maxX = Screen.width - edgePadding - size.x * (1f - pivot.x);
            float minY = edgePadding + size.y * pivot.y;
            float maxY = Screen.height - edgePadding - size.y * (1f - pivot.y);

            screen.x = Mathf.Clamp(screen.x, minX, Mathf.Max(minX, maxX));
            screen.y = Mathf.Clamp(screen.y, minY, Mathf.Max(minY, maxY));

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect != null &&
                RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screen, uiCamera, out Vector3 world))
                bubbleRect.position = world;

            UpdateTail(hasTarget, speakerScreen, uiCamera);
        }

        private void UpdateTail(bool hasTarget, Vector2 speakerScreen, Camera uiCamera)
        {
            if (tailRect == null) return;

            tailRect.gameObject.SetActive(hasTarget);
            if (!hasTarget) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    bubbleRect, speakerScreen, uiCamera, out Vector2 target))
                return;

            Rect rect = bubbleRect.rect;
            float dxOut = Mathf.Max(rect.xMin - target.x, 0f, target.x - rect.xMax);
            float dyOut = Mathf.Max(rect.yMin - target.y, 0f, target.y - rect.yMax);
            Vector3 position;
            float rotation;

            if (dyOut >= dxOut)
            {
                float x = Mathf.Clamp(target.x, rect.xMin + tailEdgeMargin,
                    Mathf.Max(rect.xMin + tailEdgeMargin, rect.xMax - tailEdgeMargin));
                bool below = target.y < rect.center.y;
                position = new Vector3(x, below ? rect.yMin : rect.yMax, 0f);
                rotation = below ? 0f : 180f;
            }
            else
            {
                float y = Mathf.Clamp(target.y, rect.yMin + tailEdgeMargin,
                    Mathf.Max(rect.yMin + tailEdgeMargin, rect.yMax - tailEdgeMargin));
                bool left = target.x < rect.center.x;
                position = new Vector3(left ? rect.xMin : rect.xMax, y, 0f);
                rotation = left ? -90f : 90f;
            }

            tailRect.localPosition = position;
            tailRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private void SetVisible(bool show)
        {
            visible = show;
            if (bubbleRect != null) bubbleRect.gameObject.SetActive(show);
        }
    }
}

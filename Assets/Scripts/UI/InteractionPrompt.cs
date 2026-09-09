using UnityEngine;
using TMPro;

public class InteractionPrompt : MonoBehaviour
{
    public static InteractionPrompt Instance { get; private set; }

    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TextMeshProUGUI actionText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 1.2f, 0);

    private IInteractable currentTarget;
    // 介面參照吃不到 Unity 的 fake-null，物件被銷毀時 currentTarget 仍然不是 null，
    // 所以另外留一份 MonoBehaviour 參照來偵測（例如撿走的道具會把自己 Destroy 掉）
    private MonoBehaviour currentTargetBehaviour;
    private Camera mainCamera;

    void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
        promptRoot.SetActive(false);
    }

    void LateUpdate()
    {
        if (currentTarget == null) return;

        if (currentTargetBehaviour == null)   // 目標已經被銷毀，提示不該再留在畫面上
        {
            Hide();
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(currentTarget.PromptWorldPosition + worldOffset);
        promptRoot.transform.position = screenPos;
    }

    public void Show(IInteractable target)
    {
        currentTarget = target;
        currentTargetBehaviour = target as MonoBehaviour;
        actionText.text = target.InteractionPrompt;
        promptRoot.SetActive(true);
    }

    public void Hide()
    {
        currentTarget = null;
        currentTargetBehaviour = null;
        promptRoot.SetActive(false);
    }
}
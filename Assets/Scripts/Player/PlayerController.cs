using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;

    [Header("互動設定")]
    [SerializeField] private float interactionRange = 0.8f;
    [SerializeField] private LayerMask interactableLayerMask;

    [Header("音效")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float walkStepInterval = 0.4f;
    [SerializeField] private float sprintStepInterval = 0.25f;

    [Header("狀態（唯讀）")]
    private bool isMoving;
    private Vector2 lastMoveDir = Vector2.down;
    private static readonly int IsSprintingHash = Animator.StringToHash("isSprinting");
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Vector2 moveInput;
    private bool isSprinting;
    public Vector2 FacingDirection => lastMoveDir;
    public bool IsMoving => isMoving;
    public bool IsSprinting => isSprinting;
    private bool canMove = true; // 對話時設 false 鎖定移動

    private float stepTimer;
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int MoveXHash = Animator.StringToHash("moveX");
    private static readonly int MoveYHash = Animator.StringToHash("moveY");

    void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        sr   = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (!canMove)
        {
            moveInput = Vector2.zero;
            isMoving = false;
            UpdateAnimator();

            // 對話等鎖移動期間 CheckInteractable() 不會再跑，提示會停在鎖住前的狀態。
            // 這裡清掉 currentInteractable，解鎖後 CheckInteractable() 才會重新判定要不要顯示
            if (currentInteractable != null)
            {
                currentInteractable = null;
                InteractionPrompt.Instance.Hide();
            }
            return;
        }

        // 逐鍵讀取而不是 Input.GetAxisRaw，這樣移動鍵才能讓玩家自訂。
        // 方向鍵固定永遠可用（不進 KeyBindings），當作玩家把 WASD 改壞時的退路，
        // 跟 DialogManager 選選項的做法一致。
        // normalized 維持不變：斜向移動速度跟原本的 GetAxisRaw 版本完全相同
        float h = 0f, v = 0f;
        if (KeyBindings.GetKey(KeyBindings.GameAction.MoveLeft)  || Input.GetKey(KeyCode.LeftArrow))  h -= 1f;
        if (KeyBindings.GetKey(KeyBindings.GameAction.MoveRight) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (KeyBindings.GetKey(KeyBindings.GameAction.MoveDown)  || Input.GetKey(KeyCode.DownArrow))  v -= 1f;
        if (KeyBindings.GetKey(KeyBindings.GameAction.MoveUp)    || Input.GetKey(KeyCode.UpArrow))    v += 1f;
        moveInput  = new Vector2(h, v).normalized;
        isSprinting = KeyBindings.GetKey(KeyBindings.GameAction.Sprint);
        isMoving   = moveInput != Vector2.zero;

        // 記錄最後移動方向（給 Animator 用，停下時保持面向）
        if (isMoving) lastMoveDir = moveInput;

        // 面向翻轉：用 lastMoveDir 而不是即時輸入 h，避免邊界抖動
        if (lastMoveDir.x > 0) sr.flipX = false;
        else if (lastMoveDir.x < 0) sr.flipX = true;

        UpdateAnimator();
        void UpdateAnimator()
        {
            if (anim == null) return;

            anim.SetBool(IsMovingHash, isMoving);
            anim.SetBool(IsSprintingHash, isSprinting);
            anim.SetFloat(MoveXHash, lastMoveDir.x);
            anim.SetFloat(MoveYHash, lastMoveDir.y);
        }

        CheckInteractable();

        // 互動鍵
        if (KeyBindings.GetKeyDown(KeyBindings.GameAction.Interact))
            TryInteract();

        HandleFootsteps();
    }
    private IInteractable currentInteractable;
    // 圓心往面向推「半徑」的距離，讓圓的後緣剛好貼齊玩家腳下、前緣貼齊 interactionRange，
    // 涵蓋範圍跟原本的射線一致，但改用範圍偵測後「踩在可互動物品上方」也抓得到——
    // 原本用 Physics2D.Raycast 是一條沒有寬度的線，玩家腳底稍微超出物品的 collider
    // 就整條線落在物品外面而打不到，即使兩者其實非常近
    void CheckInteractable()
    {
        float radius = interactionRange / 2f;
        Vector2 center = rb.position + lastMoveDir * radius;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, interactableLayerMask);

        // 範圍內可能同時圈到好幾個可互動物（例如站在道具跟 NPC 中間），
        // 用「面向夾角」決定要跟哪個互動，角度相同才比距離——
        // 不能只取 OverlapCircleAll 回傳的第一個，那個順序不保證跟玩家想要的目標一致，
        // 也不能像舊版 Raycast 那樣「打到第一個 CanInteract=false 的就整個放棄」
        // （例如面對田裡沒熟的那格，會擋住正後方站著的 NPC 打不到）
        IInteractable target = null;
        float bestDot = -2f;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            IInteractable candidate = hit.GetComponent<IInteractable>();
            if (candidate == null || !candidate.CanInteract) continue;

            // ClosestPoint 而不是 collider.bounds.center：像田地這種大面積 collider，
            // bounds 中心可能離玩家實際要互動的格子很遠，ClosestPoint 才能反映真正貼近的那一點；
            // 玩家站在 collider 內部（例如踩在 trigger 型的拾取物上）時會直接回傳玩家自己的位置
            Vector2 closest = hit.ClosestPoint(rb.position);
            Vector2 toTarget = closest - rb.position;
            float dist = toTarget.magnitude;
            Vector2 dir = dist > 0.0001f ? toTarget / dist : lastMoveDir;
            float dot = Vector2.Dot(lastMoveDir, dir);

            bool better = target == null || dot > bestDot || (dot == bestDot && dist < bestDist);
            if (better)
            {
                target = candidate;
                bestDot = dot;
                bestDist = dist;
            }
        }

        if (target != currentInteractable)
        {
            currentInteractable = target;

            if (currentInteractable != null)
                InteractionPrompt.Instance.Show(currentInteractable);
            else
                InteractionPrompt.Instance.Hide();
        }
    }
    void FixedUpdate()
    {
        float speed = isSprinting ? sprintSpeed : moveSpeed;
        rb.linearVelocity = moveInput * speed;
    }

    void TryInteract()
    {
        currentInteractable?.Interact();
    }

    // 外部呼叫：對話時鎖定 / 解鎖移動
    public void SetCanMove(bool value) => canMove = value;

    void HandleFootsteps()
    {
        if (!isMoving)
        {
            stepTimer = 0f;   // 停下來時重置計時器，下次移動立刻能踏出第一步
            return;
        }

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            float interval = isSprinting ? sprintStepInterval : walkStepInterval;
            stepTimer = interval;

            if (footstepSound != null)
                AudioManager.Instance.PlaySFX(footstepSound);
        }
    }
}

// NPC、裂縫地點都實作這個介面，就能被 E 鍵觸發
public interface IInteractable
{
    void Interact();
    string InteractionPrompt { get; }   // 新增：顯示的動作文字，例如「對話」「開啟」

    // 提示 UI 要浮在哪個世界座標上。回傳座標而不是 Transform，是因為有些互動物的
    // 錨點不等於自己的 transform（門的錨點是 collider 中心、田的錨點是玩家），
    // 而且每幀重新取值才能跟著移動的目標跑
    Vector3 PromptWorldPosition { get; }

    bool CanInteract { get; }   // 現在互動會不會有實際效果（例如：面前沒有熟成作物就不算），false 時不顯示提示 UI
}
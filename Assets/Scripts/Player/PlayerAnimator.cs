using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer), typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    Animator         animator;
    SpriteRenderer   sr;
    PlayerController controller;

    static readonly int HashMoveX       = Animator.StringToHash("MoveX");
    static readonly int HashMoveY       = Animator.StringToHash("MoveY");
    static readonly int HashIsMoving    = Animator.StringToHash("IsMoving");
    static readonly int HashIsSprinting = Animator.StringToHash("IsSprinting");

    void Awake()
    {
        animator   = GetComponent<Animator>();
        sr         = GetComponent<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        animator.SetFloat(HashMoveX,       controller.FacingDirection.x);
        animator.SetFloat(HashMoveY,       controller.FacingDirection.y);
        animator.SetBool(HashIsMoving,     controller.IsMoving);
        animator.SetBool(HashIsSprinting,  controller.IsSprinting);

        if (controller.FacingDirection.x != 0)
            sr.flipX = controller.FacingDirection.x < 0;
    }
}

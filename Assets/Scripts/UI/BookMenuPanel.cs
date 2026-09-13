using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 把選單面板包裝成「一本會開闔的書」：開啟時書飛進來攤開，關閉時闔上飛走，
// 切換分頁時書維持攤開、只翻一頁過去。
// 子類別（背包／任務／情報）照舊覆寫 OnOpened / OnClosed 處理自己的內容，不用管動畫。
//
// 為什麼動畫不用 Animator：素材就是一組連號 PNG（開書 8 張、關書 8 張、翻頁左右各 9 張），
// 用 Animator 得多養 Controller + Clip + StateMachine，換一組幀就要重建一次；
// 這裡只是照順序換 sprite，一個陣列加一個計時器就夠，速度也能直接在 Inspector 調。
//
// **動畫幀的 sprite 必須是 Single 匯入模式**（rect = 整張貼圖、pivot 置中）。
// 素材每一幀的不透明範圍差很多（闔起來時只有 54x550、攤開後是 490x314），
// 如果用預設那種「裁切到內容邊界」的 sprite，同一個 RectTransform 會把每幀
// 拉伸成一樣大，書就會整個變形。
//
// 動畫期間頁面內容是藏起來的——書還在空中翻轉、或紙張正立起來時就把格子畫出來會穿幫。
// 側邊標籤條不屬於 pageContent（它要全程看得見），所以擺在 BookWindow 底下、Pages 外面。
public class BookMenuPanel : MenuPanelBase
{
    [Header("書本外觀（留空 = 不播動畫，退回直接開關 panelRoot）")]
    [Tooltip("播放動畫的那張 Image，尺寸要跟動畫幀的貼圖一致")]
    [SerializeField] private Image bookFrame;
    [Tooltip("書頁上的內容（格子、詳情），書完全攤開後才顯示。標籤條不要放進來")]
    [SerializeField] private GameObject pageContent;
    [Tooltip("側邊標籤條與書籤圖示。跟頁面內容分開管：翻頁時書沒有移動，標籤要留著；但開書／關書時書是從畫面外飛進來的，標籤留在原地會變成飄在半空的一排方塊")]
    [SerializeField] private GameObject bookChrome;

    [Header("動畫幀")]
    [SerializeField] private Sprite[] openFrames;
    [SerializeField] private Sprite[] closeFrames;
    [Tooltip("每一幀停留幾秒。8 幀 x 0.04 秒約 0.32 秒，再慢會拖到節奏")]
    [SerializeField] private float frameDuration = 0.04f;

    [Header("翻頁（留空 = 換分頁時直接切，不翻）")]
    [Tooltip("書攤開後的靜止圖。開書動畫最後一幀沒有書籤，靜止圖有，要指定才不會少一塊")]
    [SerializeField] private Sprite restingFrame;
    [Tooltip("往後翻（切到下面的標籤）：右頁往左翻，用 Page Flip/Left")]
    [SerializeField] private Sprite[] flipForwardFrames;
    [Tooltip("往前翻（切到上面的標籤）：左頁往右翻，用 Page Flip/Right")]
    [SerializeField] private Sprite[] flipBackFrames;
    [Tooltip("翻頁每幀停留幾秒。這個動畫是「點一下標籤的回饋」，要比開關書更快")]
    [SerializeField] private float flipFrameDuration = 0.025f;

    private Coroutine anim;
    private bool ready;   // Start 當下那次隱藏不該播動畫，見下方說明

    protected override void Start()
    {
        base.Start();     // 這裡面會呼叫一次 SetPanelVisible(false)
        ready = true;
    }

    // 沒設定書本外觀就完全退回父類別行為，這樣同一個類別也能給還沒換皮的面板用
    private bool HasBook => bookFrame != null && openFrames != null && openFrames.Length > 0;

    protected override void SetPanelVisible(bool visible)
    {
        if (!HasBook)
        {
            base.SetPanelVisible(visible);
            return;
        }

        // 換皮成書之後，panelRoot 指的那個舊面板已經沒在用，但它還留在場景裡。
        // 這裡一定要主動把它關掉：以前是靠父類別在 Start 把 panelRoot 關起來，
        // 現在走的是書這條分支，沒有人會去管它，它就會從開機起一直顯示——
        // 連標題畫面都會被蓋著一塊面板（這個坑在資訊視窗上已經踩過一次）
        base.SetPanelVisible(false);

        // 開場的初始隱藏：這時候還沒有人要看動畫，直接收乾淨就好。
        // 不擋掉的話遊戲一啟動就會有一本書在畫面上闔起來
        if (!ready)
        {
            bookFrame.enabled = false;
            if (pageContent != null) pageContent.SetActive(false);
            if (bookChrome != null) bookChrome.SetActive(false);
            return;
        }

        if (anim != null) StopCoroutine(anim);

        // 不管開還關，動畫跑的時候一律先把內容跟標籤藏起來（書正在飛）
        if (pageContent != null) pageContent.SetActive(false);
        if (bookChrome != null) bookChrome.SetActive(false);

        if (visible)
        {
            bookFrame.enabled = true;
            anim = StartCoroutine(PlayFrames(openFrames, true));
        }
        else
        {
            // 這裡刻意不直接關掉物件：關閉流程是「先隱藏再 OnClosed」，
            // 真的把自己 SetActive(false) 的話協程會被中止，關書動畫就沒機會播完。
            // 改成播完最後一幀才把 Image 關掉（見 PlayFrames）
            anim = StartCoroutine(PlayFrames(closeFrames, false));
        }
    }

    // 換分頁：書維持攤開，只翻一頁過去。
    // swapContent 會在紙張立起來（動畫正中間）時才被呼叫——這時新舊兩頁都被翻起來的紙擋住，
    // 玩家看到的是「翻過去之後變成新的一頁」，而不是「舊頁上的字當場變成另一組」。
    public void FlipPage(bool forward, System.Action swapContent)
    {
        var frames = forward ? flipForwardFrames : flipBackFrames;

        // 沒設定翻頁素材、或書根本沒在開著（例如用快捷鍵直接跳到某一頁）就直接換，不要卡住
        if (!HasBook || frames == null || frames.Length == 0 || !IsOpen || !ready)
        {
            swapContent?.Invoke();
            return;
        }

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(PlayFlip(frames, swapContent));
    }

    IEnumerator PlayFlip(Sprite[] frames, System.Action swapContent)
    {
        if (pageContent != null) pageContent.SetActive(false);

        int mid = frames.Length / 2;
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null) bookFrame.sprite = frames[i];
            if (i == mid)
            {
                swapContent?.Invoke();
                swapContent = null;
            }
            yield return new WaitForSecondsRealtime(flipFrameDuration);
        }

        // 幀數是偶數時上面的 mid 判斷還是會進去，但保險起見這裡再補一次
        swapContent?.Invoke();

        if (restingFrame != null) bookFrame.sprite = restingFrame;
        if (pageContent != null) pageContent.SetActive(true);
        anim = null;
    }

    IEnumerator PlayFrames(Sprite[] frames, bool showContentWhenDone)
    {
        if (frames != null)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null) bookFrame.sprite = frames[i];
                // 用 Realtime：選單開著時若之後加入暫停（timeScale = 0），動畫不該跟著停住
                yield return new WaitForSecondsRealtime(frameDuration);
            }
        }

        if (showContentWhenDone)
        {
            // 開書動畫的最後一幀沒有書籤（緞帶），靜止圖才有。停在最後一幀的話
            // 書籤會等到玩家第一次翻頁才突然冒出來，所以攤開後要換成靜止圖
            if (restingFrame != null) bookFrame.sprite = restingFrame;
            if (pageContent != null) pageContent.SetActive(true);
            if (bookChrome != null) bookChrome.SetActive(true);
        }
        else
        {
            bookFrame.enabled = false;
        }

        anim = null;
    }
}

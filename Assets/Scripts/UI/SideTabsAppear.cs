using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 書本攤開時，側邊那排標籤由上往下依序彈出來。
//
// 掛在 SideTabs 上，靠 OnEnable 觸發就好——書本是用「開關 bookChrome 這個物件」來收放標籤條的
// （見 BookMenuPanel），所以攤開的那一刻這個元件剛好會被啟用，不需要另外接事件。
//
// 動畫本身是素材包 Content/Side Tabs/Animated 那四張：寬度 10 → 40 → 36 → 31，
// 先衝出頭再縮回去，最後才換回平常的樣子。每一格之間差一個 stagger，就成了瀑布式展開。
//
// 動畫期間圖示與選取框要藏起來：那四張幀的框寬度一直在變，圖示留在原位會浮在框外面。
public class SideTabsAppear : MonoBehaviour
{
    [Tooltip("Content/Side Tabs/Animated 的 1~4")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameDuration = 0.04f;
    [Tooltip("相鄰兩格之間差多久開始。0 = 六格同時彈出")]
    [SerializeField] private float stagger = 0.05f;

    void OnEnable()
    {
        if (frames == null || frames.Length == 0) return;
        StartCoroutine(PlayAll());
    }

    void OnDisable()
    {
        // 書被關掉時協程會中止，必須自己把每一格還原，
        // 否則下次開書會停在動畫中途的那張窄框
        StopAllCoroutines();
        foreach (var tab in Tabs()) Restore(tab);
    }

    IEnumerator PlayAll()
    {
        var tabs = Tabs();
        foreach (var t in tabs) Hide(t);

        for (int i = 0; i < tabs.Count; i++)
        {
            StartCoroutine(PlayOne(tabs[i]));
            if (stagger > 0f) yield return new WaitForSecondsRealtime(stagger);
        }
    }

    IEnumerator PlayOne(TabParts t)
    {
        t.frame.enabled = true;
        foreach (var f in frames)
        {
            t.frame.sprite = f;
            yield return new WaitForSecondsRealtime(frameDuration);
        }
        Restore(t);
    }

    // --- 以下都是把「一格標籤」拆成它的三個部件，避免每個地方各自 Find 一次 ---

    struct TabParts
    {
        public Image frame;
        public Sprite resting;
        public GameObject icon;
        public GameObject highlight;
    }

    System.Collections.Generic.List<TabParts> Tabs()
    {
        var list = new System.Collections.Generic.List<TabParts>();
        foreach (Transform t in transform)
        {
            var img = t.GetComponent<Image>();
            if (img == null || t.GetComponent<Button>() == null) continue;   // RibbonIcon 沒有 Button，跳過
            var icon = t.Find("Icon");
            var hl = t.Find("Highlight");
            list.Add(new TabParts {
                frame = img,
                resting = restingCache.TryGetValue(img, out var s) ? s : (restingCache[img] = img.sprite),
                icon = icon != null ? icon.gameObject : null,
                highlight = hl != null ? hl.gameObject : null,
            });
        }
        return list;
    }

    // 第一次跑之前先把「平常的那張圖」記下來，之後才還原得回去
    private readonly System.Collections.Generic.Dictionary<Image, Sprite> restingCache
        = new System.Collections.Generic.Dictionary<Image, Sprite>();

    // 還沒輪到的格子要整個不畫。只藏圖示、框還留著的話，
    // 會看到六個空框先排好、再一個個「長出來」，跟素材原本的演出不一樣
    void Hide(TabParts t)
    {
        t.frame.enabled = false;
        if (t.icon != null) t.icon.SetActive(false);
        if (t.highlight != null) t.highlight.SetActive(false);
    }

    void Restore(TabParts t)
    {
        t.frame.enabled = true;
        t.frame.sprite = t.resting;
        if (t.icon != null) t.icon.SetActive(true);
        // 選取框不在這裡打開：哪一格該亮是 TabGroup 的事，這裡開了會跟它打架。
        // 改成通知它重畫一次
        var tg = GetComponentInParent<TabGroup>();
        if (tg != null) tg.RefreshVisuals();
    }
}

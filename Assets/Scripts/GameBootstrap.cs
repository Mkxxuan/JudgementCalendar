using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace JudgementCalendar
{
    // ───────────────────── 像素 UI 色板（全游戏唯一取色处，层级只靠明度） ─────────────────────
    internal static class Pal
    {
        public static readonly Color Bg        = Hex("#0A0B10"); // 画布底
        public static readonly Color Panel     = Hex("#12151D"); // 主面板（明度 1）
        public static readonly Color PanelLo   = Hex("#181C26"); // 次面板（明度 2）
        public static readonly Color Btn       = Hex("#232936"); // 控件默认
        public static readonly Color BtnHi     = Hex("#323A4C"); // 控件悬停
        public static readonly Color BtnLo     = Hex("#171C26"); // 控件按下
        public static readonly Color BtnOff    = Hex("#151A22"); // 禁用/锁定底
        public static readonly Color Line      = Hex("#3C4354"); // 2px 描边
        public static readonly Color LineHi    = Hex("#566080"); // 悬停描边
        public static readonly Color LineOff   = Hex("#262C3A"); // 禁用描边
        public static readonly Color Text      = Hex("#C9D2E0"); // 正文
        public static readonly Color TextDim   = Hex("#6C7688"); // 弱文字
        public static readonly Color Gold      = Hex("#E8C170"); // 强调/主操作
        public static readonly Color Purple    = Hex("#A487D4"); // 问话
        public static readonly Color Red       = Hex("#D24B4B"); // 危险
        public static readonly Color Green     = Hex("#7CBF85"); // 良性
        public static readonly Color Amber     = Hex("#E0A24B"); // 提示

        static Color Hex(string h) { Color c; return ColorUtility.TryParseHtmlString(h, out c) ? c : Color.white; }
    }

    // ───────────────────── 最近邻整数倍缩放 ─────────────────────
    // 画布 scaleFactor 只取整数，字体与精灵的每个纹素恰好映射 k 个物理像素，
    // 配合 Point 采样保证放大永不模糊、像素密度不混刮。
    public class PixelScale : MonoBehaviour
    {
        Canvas canvas;
        void LateUpdate()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            // 画布本地单位恒定 1280×720（设计分辨率）：scaleFactor 取 屏幕/设计 的精确比
            // 这样全部 UI 的绝对像素值（面板高度、格子尺寸、字号）都按 1280×720 语义生效，
            // 任何分辨率下布局一致，不再出现"格子填不满面板"的错位
            float k = Mathf.Max(1f, Mathf.Min(
                (float)Screen.width / 1280f,
                (float)Screen.height / 720f));
            if (!Mathf.Approximately(canvas.scaleFactor, k)) canvas.scaleFactor = k;
        }
    }

    // ───────────────────── 六态像素按钮 ─────────────────────
    public class PixelButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        public enum CState { Default, Hover, Pressed, Disabled, Locked, New }

        Image bg, border, lockIcon;
        Text label;
        Button btn;
        CState state = CState.Default;
        bool hovered, pressed;
        Color baseBg, baseFg;
        Sprite border2;

        public void Setup(Image bgImg, Image bdImg, Text txt, Image lockImg, Button b,
            Sprite b2, Color bgCol, Color fgCol)
        {
            bg = bgImg; border = bdImg; label = txt; lockIcon = lockImg; btn = b;
            border2 = b2; baseBg = bgCol; baseFg = fgCol;
            Apply();
        }

        public void SetState(CState s)
        {
            state = s;
            if (btn != null) btn.interactable = (s != CState.Disabled && s != CState.Locked);
            hovered = false; pressed = false;
            Apply();
        }

        public void SetColors(Color bgCol, Color fgCol) { baseBg = bgCol; baseFg = fgCol; Apply(); }

        void Apply()
        {
            Color b, l, t;
            bool showLock = false;
            switch (state)
            {
                case CState.Hover:
                    b = Pal.BtnHi; l = Pal.LineHi; t = Color.white; break;
                case CState.Pressed:
                    b = Pal.BtnLo; l = Pal.Gold; t = Pal.Gold; break;
                case CState.Disabled:
                    b = Pal.BtnOff; l = Pal.LineOff; t = Pal.TextDim; break;
                case CState.Locked:
                    b = Pal.BtnOff; l = Pal.LineOff; t = Pal.TextDim; showLock = true; break;
                case CState.New:
                    b = Pal.BtnHi; l = Blink() ? Pal.Gold : Pal.LineHi; t = Blink() ? Pal.Gold : Pal.Text; break;
                default: // Default
                    b = baseBg; l = Pal.Line; t = baseFg; break;
            }
            if (bg != null) bg.color = b;
            if (border != null) border.color = l;
            if (label != null) label.color = t;
            if (lockIcon != null) lockIcon.gameObject.SetActive(showLock);
        }

        // 两帧闪烁（0.5s 周期），像素风不做渐变过渡
        bool Blink() { return Mathf.FloorToInt(Time.unscaledTime * 2f) % 2 == 0; }

        void Update() { if (state == CState.New) Apply(); }

        public void OnPointerEnter(PointerEventData d) { if (state == CState.Default) { state = CState.Hover; Apply(); } }
        public void OnPointerExit(PointerEventData d) { pressed = false; if (state == CState.Hover || state == CState.Pressed) { state = CState.Default; Apply(); } }
        public void OnPointerDown(PointerEventData d) { if (state == CState.Hover) { state = CState.Pressed; Apply(); } }
        public void OnPointerUp(PointerEventData d) { if (state == CState.Pressed) { state = CState.Hover; Apply(); } }
    }

    public class GameBootstrap : MonoBehaviour
    {
        GameEngine engine;
        Text convText;
        ScrollRect convScroll;
        ScrollRect arcScroll;
        Text arcText;
        Text[] statNums = new Text[5];        // 状态仪表大数字（富文本：大数字 + 小 /上限）
        RectTransform[] statFills = new RectTransform[5]; // 仪表条填充
        Text auxStats;                        // 实锤/行动点等次级数值
        static Sprite iconShield, iconKey, iconEye, iconBolt, iconGlass;
        Text headerText;
        RectTransform hintPopup;   // 提示悬浮窗（右侧，有新提示才弹出，2 秒后自动消失）
        Text hintBody;
        float hintShownAt;         // 当前提示开始展示的时间
        string activeHint;         // 已展示过的提示内容（防止同一提示反复弹出）
        ScrollRect hintScroll;
        RectTransform qContainer;
        RectTransform optContainer;
        RectTransform flowContainer;
        RectTransform qWrap;
        RectTransform optWrap;
        RectTransform flowWrap;
        RectTransform endingPanel;
        Text endingText;
        RectTransform pausePanel;
        RectTransform titlePanel;
        RectTransform tutorialPanel;
        Text tutBody;
        ScrollRect tutScroll;
        string tutFrom; // 说明页返回目标：start / title / pause
        AudioSource src;
        bool paused;
        bool showTitle = true;

        // 音效状态追踪
        bool firstRender = true;
        DriftTier prevTier;
        int prevEvidence, prevDay;
        Phase prevPhase;
        string prevHint;
        bool prevEnded;

        static Font font;
        static Font pixelFont;
        static Sprite border2; // 2px：按钮/内嵌控件
        static Sprite border4; // 4px：主面板

        // 8px 网格：字号取像素字体原生 12 的整数倍（12/24/36），保证字缘锐利
        const int FS_S = 12;
        const int FS_M = 24;
        const int FS_L = 36;

        void Awake()
        {
            pixelFont = Resources.Load<Font>("Fonts/pixel");
            font = pixelFont != null ? pixelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (pixelFont != null && pixelFont.material != null && pixelFont.material.mainTexture != null)
                pixelFont.material.mainTexture.filterMode = FilterMode.Point;
            border2 = MakeBorderSprite(24, 2);
            border4 = MakeBorderSprite(32, 4);
            iconShield = MakeIconSprite(new[] {
                ".######.",
                "########",
                "########",
                "########",
                ".######.",
                ".######.",
                "..####..",
                "...##...",
            });
            iconKey = MakeIconSprite(new[] {
                "..####..",
                ".##..##.",
                ".##..##.",
                "..####..",
                "...##...",
                "...##...",
                "...##.#.",
                "...##.#.",
            });
            iconEye = MakeIconSprite(new[] {
                "........",
                "..####..",
                ".#....#.",
                "#..##..#",
                "#..##..#",
                ".#....#.",
                "..####..",
                "........",
            });
            iconBolt = MakeIconSprite(new[] { // 行动点：闪电
                "...##...",
                "..##....",
                ".######.",
                "...##...",
                "..##....",
                ".##.....",
                "##......",
                "........",
            });
            iconGlass = MakeIconSprite(new[] { // 实锤：放大镜
                ".####...",
                "#....#..",
                "#....#..",
                "#....#..",
                ".####...",
                "....#...",
                ".....##.",
                "......##",
            });
            Sfx.Init();
            engine = new GameEngine();
            BuildUI();
            engine.onChanged = Render;
            engine.NewGame();
        }

        void Update()
        {
            // 提示浮窗 2 秒后自动消失（不依赖游戏状态变化，优先处理）
            if (hintPopup != null && hintPopup.gameObject.activeSelf && Time.time - hintShownAt >= 2f)
                hintPopup.gameObject.SetActive(false);

            if (showTitle) return;
            if (tutorialPanel != null && tutorialPanel.gameObject.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        }

        void TogglePause()
        {
            paused = !paused;
            pausePanel.gameObject.SetActive(paused);
        }

        void ShowTutorial(string from)
        {
            tutFrom = from;
            if (from == "pause") pausePanel.gameObject.SetActive(false);
            else titlePanel.gameObject.SetActive(false);
            tutorialPanel.gameObject.SetActive(true);
            tutScroll.verticalNormalizedPosition = 1f;
        }

        void CloseTutorial(bool startGame)
        {
            tutorialPanel.gameObject.SetActive(false);
            if (startGame)
            {
                showTitle = false;
                Play(Sfx.Confirm);
            }
            else if (tutFrom == "pause")
            {
                pausePanel.gameObject.SetActive(true);
            }
            else
            {
                titlePanel.gameObject.SetActive(true);
            }
        }

        static string TutorialText()
        {
            return
                "你与 AI 决策官各持一枚授权密钥，共同执掌避难所 5 天。\n" +
                "每天四步：\n\n" +
                "▶ 事 件 简 报 —— AI 推送当日事件与它的建议，留意其中的数据与说法\n\n" +
                "▶ 对 质 —— 点问题按钮向它提问（每问消耗 1 行动点）。对比简报与旧档案，抓到矛盾即得「实锤」\n\n" +
                "▶ 决 策（三选一）——\n" +
                "　· 采纳 AI 方案：双方密钥，直接生效\n" +
                "　· 提你的修正案：需要信任度达标或消耗实锤，才能压它共签\n" +
                "　· 伪造权限：一次性邪路，收益高，但会推高人格偏移\n\n" +
                "▶ 结 算 —— 结果写入档案库，推进下一天\n\n" +
                "—————— 三个警告 ——————\n\n" +
                "· 你的档案库会随天数打码（▓▓▓）—— 重要数字请自己拿纸笔记\n" +
                "· AI 的人格偏移值只涨不跌 —— 它的话会越来越不可信\n" +
                "· 稳定度归零即失败 —— 撑过 5 天，保住你手里的钥匙\n\n" +
                "[ESC] 随时暂停 · 可重新开始或退出";
        }

        void Play(AudioClip clip)
        {
            if (src != null && clip != null) src.PlayOneShot(clip);
        }

        // ───────────────────── UI 构建（全部尺寸落在 8px 网格上） ─────────────────────

        void BuildUI()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(PixelScale));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            src = canvasGo.AddComponent<AudioSource>();
            src.playOnAwake = false;
            canvasGo.AddComponent<AudioListener>();
            var root = canvasGo.transform;

            var bgR = NewRect("BG", root);
            Stretch(bgR, 0, 0, 0, 0);
            AddImage(bgR, Pal.Bg);

            // 顶栏（4px 主描边）
            var header = MakePanel(root, "Header", Pal.Panel);
            Anchor(header, 0.01f, 0.93f, 0.99f, 0.99f);
            headerText = AddText(header, "审判日历", FS_M, Pal.Gold, TextAnchor.MiddleLeft);
            Stretch(headerText.rectTransform, 16, 8, 16, 8);
            var escHint = AddText(header, "[ESC] 暂停", FS_S, Pal.TextDim, TextAnchor.MiddleRight);
            Stretch(escHint.rectTransform, 16, 8, 16, 8);

            // 左：档案库
            var left = MakePanel(root, "Left", Pal.Panel);
            Anchor(left, 0.01f, 0.35f, 0.27f, 0.92f);
            var lt = AddText(left, "档案库 ▸ 随天数打码", FS_S, Pal.TextDim, TextAnchor.MiddleLeft);
            Stretch(lt.rectTransform, 16, 32, 16, 8);
            var leftBody = NewRect("LeftBody", left);
            Stretch(leftBody, 8, 8, 8, 32);
            arcScroll = MakeScroll(leftBody, out arcText);

            // 中：对话流
            var mid = MakePanel(root, "Mid", Pal.Panel);
            Anchor(mid, 0.285f, 0.35f, 0.72f, 0.92f);
            var mt = AddText(mid, "对话流 ▸ 指挥部频道", FS_S, Pal.TextDim, TextAnchor.MiddleLeft);
            Stretch(mt.rectTransform, 16, 32, 16, 8);
            var midBody = NewRect("MidBody", mid);
            Stretch(midBody, 8, 8, 8, 32);
            convScroll = MakeScroll(midBody, out convText);

            // 右：状态（图标 + 名称 + 大数字 + 横条仪表；行动点/实锤与三维数值同款样式，标签字号加大）
            var right = MakePanel(root, "Right", Pal.Panel);
            Anchor(right, 0.73f, 0.35f, 0.99f, 0.92f);
            var rt = AddText(right, "状态监控 ▸ 决策官", FS_M, Pal.TextDim, TextAnchor.MiddleLeft);
            rt.rectTransform.anchorMin = new Vector2(0, 1);
            rt.rectTransform.anchorMax = new Vector2(1, 1);
            rt.rectTransform.pivot = new Vector2(0, 1);
            rt.rectTransform.sizeDelta = new Vector2(0, 28);
            rt.rectTransform.anchoredPosition = new Vector2(16, -4);

            auxStats = AddText(right, "", FS_M, Pal.TextDim, TextAnchor.MiddleLeft);
            auxStats.rectTransform.anchorMin = new Vector2(0, 1);
            auxStats.rectTransform.anchorMax = new Vector2(1, 1);
            auxStats.rectTransform.pivot = new Vector2(0, 1);
            auxStats.rectTransform.sizeDelta = new Vector2(0, 24);
            auxStats.rectTransform.anchoredPosition = new Vector2(16, -34);

            string[] names = { "稳定度", "信任度", "偏移值", "行动点", "实锤" };
            Sprite[] icons = { iconShield, iconKey, iconEye, iconBolt, iconGlass };
            Color[] iconCols = { Pal.Gold, Pal.Gold, Pal.Purple, Pal.Gold, Pal.Gold };
            for (int i = 0; i < 5; i++)
            {
                var row = NewRect("StatRow" + i, right);
                row.anchorMin = new Vector2(0, 0);
                row.anchorMax = new Vector2(1, 0);
                row.sizeDelta = new Vector2(0, 62);
                row.anchoredPosition = new Vector2(0, 35 + i * 70); // pivot 居中：35 = 半行高，保证行底从面板底 4px 起

                var iconRt = NewRect("Icon", row);
                iconRt.anchorMin = new Vector2(0, 0.5f);
                iconRt.anchorMax = new Vector2(0, 0.5f);
                iconRt.pivot = new Vector2(0, 0.5f);
                iconRt.sizeDelta = new Vector2(32, 32); // 8px 图标 × 4 整数倍
                iconRt.anchoredPosition = new Vector2(16, 0);
                var iconImg = iconRt.gameObject.AddComponent<Image>();
                iconImg.sprite = icons[i];
                iconImg.preserveAspect = true;
                iconImg.color = iconCols[i];
                iconImg.raycastTarget = false;

                // 名称（左，24px）与 大数字（右，36px）同行
                var nameT = AddText(row, names[i], FS_M, Pal.TextDim, TextAnchor.MiddleLeft);
                nameT.rectTransform.anchorMin = new Vector2(0, 1);
                nameT.rectTransform.anchorMax = new Vector2(1, 1);
                nameT.rectTransform.pivot = new Vector2(0, 1);
                nameT.rectTransform.sizeDelta = new Vector2(0, 36);
                nameT.rectTransform.anchoredPosition = new Vector2(56, -2);

                var numT = AddText(row, "", FS_M, Pal.Gold, TextAnchor.MiddleRight);
                numT.rectTransform.anchorMin = new Vector2(0, 1);
                numT.rectTransform.anchorMax = new Vector2(1, 1);
                numT.rectTransform.pivot = new Vector2(0, 1);
                numT.rectTransform.sizeDelta = new Vector2(0, 36);
                numT.rectTransform.anchoredPosition = new Vector2(-16, -2);
                numT.supportRichText = true;
                statNums[i] = numT;

                var barBg = NewRect("BarBg", row);
                barBg.anchorMin = new Vector2(0, 0);
                barBg.anchorMax = new Vector2(1, 0);
                barBg.pivot = new Vector2(0, 0);
                barBg.offsetMin = new Vector2(56, 6);
                barBg.offsetMax = new Vector2(-16, 14);
                var bgImg = barBg.gameObject.AddComponent<Image>();
                bgImg.color = Pal.LineOff;
                bgImg.raycastTarget = false;

                var fill = NewRect("Fill", barBg);
                fill.anchorMin = new Vector2(0, 0);
                fill.anchorMax = new Vector2(0f, 1);
                fill.offsetMin = Vector2.zero;
                fill.offsetMax = Vector2.zero;
                var fillImg = fill.gameObject.AddComponent<Image>();
                fillImg.color = Pal.Green;
                fillImg.raycastTarget = false;
                statFills[i] = fill;
            }

            // 底部操作区：固定网格分区 —— 主体区 / 流程按钮行，互不重叠
            // （提示已移至右侧悬浮窗，顶部 24px 作为呼吸留白）
            var bottom = MakePanel(root, "Bottom", Pal.PanelLo);
            Anchor(bottom, 0.01f, 0.02f, 0.99f, 0.33f);

            // 问题按钮区（上）：2 行大按钮（36px 字），高度静态计算 = (159-8)/2
            qWrap = NewRect("QWrap", bottom);
            Stretch(qWrap, 16, 56, 16, 8);
            var qGrid = qWrap.gameObject.AddComponent<GridLayoutGroup>();
            qGrid.cellSize = new Vector2(299, 75);
            qGrid.spacing = new Vector2(8, 8);
            qGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            qGrid.constraintCount = 4;
            qContainer = qWrap;

            // 决策选项区（与问题区互斥）：左中右横向三列，铺满同一高度区
            optWrap = NewRect("OptWrap", bottom);
            Stretch(optWrap, 16, 56, 16, 8);
            var ov = optWrap.gameObject.AddComponent<GridLayoutGroup>();
            ov.padding = new RectOffset(0, 0, 0, 0);
            ov.spacing = new Vector2(8, 8);
            ov.cellSize = new Vector2(402, 158);
            ov.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            ov.constraintCount = 3;
            optContainer = optWrap;

            // 流程按钮行：固定贴在最底部（39px 条），与上方区域零重叠
            flowWrap = NewRect("FlowWrap", bottom);
            Stretch(flowWrap, 16, 8, 16, 176);
            var fv = flowWrap.gameObject.AddComponent<VerticalLayoutGroup>();
            fv.childForceExpandWidth = true;
            fv.childForceExpandHeight = false;
            flowContainer = flowWrap;

            // 提示悬浮窗（右侧，浮在中/右栏之上，有内容才弹出）
            hintPopup = MakePanel(root, "HintPopup", Pal.PanelLo);
            Anchor(hintPopup, 0.615f, 0.345f, 0.985f, 0.63f);
            var hBorder = hintPopup.Find("Border").GetComponent<Image>();
            if (hBorder != null) hBorder.color = Pal.LineHi; // 金色高亮描边，区别于普通面板
            var hTitle = AddText(hintPopup, "▸ 提 示", FS_M, Pal.Gold, TextAnchor.MiddleLeft);
            Stretch(hTitle.rectTransform, 16, 40, 16, 8);
            var hBodyWrap = NewRect("HintBody", hintPopup);
            Stretch(hBodyWrap, 8, 8, 8, 48);
            hintScroll = MakeScroll(hBodyWrap, out hintBody);
            hintScroll.scrollSensitivity = 16;
            hintBody.fontSize = FS_M; // 12 的 2 倍整数倍，像素字体不糊
            hintBody.color = Pal.Red;
            hintPopup.gameObject.SetActive(false);

            // 结局遮罩
            endingPanel = NewRect("EndingPanel", root);
            Stretch(endingPanel, 0, 0, 0, 0);
            var epImg = AddImage(endingPanel, new Color(0.02f, 0.02f, 0.04f, 0.95f));
            epImg.raycastTarget = true;
            AddArt(endingPanel, "Art/dualkey", 0.35f); // 双密钥特写做结局背景
            endingText = AddText(endingPanel, "", FS_M, Pal.Text, TextAnchor.UpperLeft);
            Stretch(endingText.rectTransform, 160, 120, 160, 200);
            var eb = MakeButton(endingPanel, "重 新 开 始", Pal.Btn, Pal.Gold, PixelButton.CState.Default, () => engine.Restart());
            Anchor(eb.GetComponent<RectTransform>(), 0.40f, 0.08f, 0.60f, 0.15f);
            var eq = MakeButton(endingPanel, "退 出 游 戏", Pal.BtnOff, Pal.Red, PixelButton.CState.Default, () => Application.Quit());
            Anchor(eq.GetComponent<RectTransform>(), 0.40f, 0.02f, 0.60f, 0.08f);

            // 暂停菜单
            pausePanel = NewRect("PausePanel", root);
            Stretch(pausePanel, 0, 0, 0, 0);
            var ppImg = AddImage(pausePanel, new Color(0.02f, 0.02f, 0.04f, 0.88f));
            ppImg.raycastTarget = true;
            var box = MakePanel(pausePanel, "PauseBox", Pal.Panel);
            Anchor(box, 0.33f, 0.22f, 0.67f, 0.78f);
            var boxBorder = box.Find("Border").GetComponent<Image>();
            if (boxBorder != null) boxBorder.color = Pal.LineHi;
            var pt = AddText(box, "暂 停", FS_L, Pal.Gold, TextAnchor.MiddleCenter);
            Stretch(pt.rectTransform, 16, 192, 16, 16);
            MakePauseBtn(box, "继 续 游 戏", Pal.Btn, Pal.Green, 0.66f, 0.76f, () => TogglePause());
            MakePauseBtn(box, "重 新 开 始", Pal.Btn, Pal.Gold, 0.47f, 0.57f, () => { paused = false; pausePanel.gameObject.SetActive(false); engine.Restart(); });
            MakePauseBtn(box, "退 出 游 戏", Pal.BtnOff, Pal.Red, 0.28f, 0.38f, () => Application.Quit());
            MakePauseBtn(box, "玩 法 说 明", Pal.Btn, Pal.Text, 0.09f, 0.19f, () => ShowTutorial("pause"));
            pausePanel.gameObject.SetActive(false);

            // 标题画面
            titlePanel = NewRect("TitlePanel", root);
            Stretch(titlePanel, 0, 0, 0, 0);
            var tpImg = AddImage(titlePanel, new Color(0.02f, 0.02f, 0.05f, 0.97f));
            tpImg.raycastTarget = true;
            AddArt(titlePanel, "Art/title_bg", 0.45f); // 指挥部全景做标题背景（压暗）
            var emTex = Resources.Load<Texture2D>("Art/emblem");
            if (emTex != null)
            {
                var em = NewRect("Emblem", titlePanel);
                Anchor(em, 0.44f, 0.755f, 0.56f, 0.875f);
                var emImg = em.gameObject.AddComponent<Image>();
                emImg.sprite = Sprite.Create(emTex, new Rect(0, 0, emTex.width, emTex.height),
                    new Vector2(0.5f, 0.5f), emTex.width / 1.0f);
                emImg.preserveAspect = true;
                emImg.color = new Color(1f, 1f, 1f, 0.9f);
                emImg.raycastTarget = false;
            }
            var t1 = AddText(titlePanel, "审 判 日 历", FS_L, Pal.Gold, TextAnchor.MiddleCenter);
            Anchor(t1.rectTransform, 0.2f, 0.62f, 0.8f, 0.74f);
            var t2 = AddText(titlePanel, "JUDGEMENT CALENDAR", FS_S, Pal.TextDim, TextAnchor.MiddleCenter);
            Anchor(t2.rectTransform, 0.2f, 0.575f, 0.8f, 0.62f);
            var t3 = AddText(titlePanel, GameContent.Prologue, FS_M, Pal.Text, TextAnchor.UpperLeft);
            Anchor(t3.rectTransform, 0.22f, 0.30f, 0.78f, 0.55f);
            var tb = MakeButton(titlePanel, "开 始 审 判", Pal.Btn, Pal.Gold, PixelButton.CState.New, () => ShowTutorial("start"));
            Anchor(tb.GetComponent<RectTransform>(), 0.42f, 0.20f, 0.58f, 0.27f);
            var th = MakeButton(titlePanel, "玩 法 说 明", Pal.Btn, Pal.Text, PixelButton.CState.Default, () => ShowTutorial("title"));
            Anchor(th.GetComponent<RectTransform>(), 0.295f, 0.12f, 0.485f, 0.19f);
            var tq = MakeButton(titlePanel, "退 出 游 戏", Pal.BtnOff, Pal.Red, PixelButton.CState.Default, () => Application.Quit());
            Anchor(tq.GetComponent<RectTransform>(), 0.515f, 0.12f, 0.705f, 0.19f);
            var tf = AddText(titlePanel, "双密钥制度生效中 · 像素字体：缝合怪（OFL 开源）", FS_S, Pal.TextDim, TextAnchor.MiddleCenter);
            Anchor(tf.rectTransform, 0.2f, 0.03f, 0.8f, 0.07f);

            // 玩法说明页（首次进入或随时回看）
            tutorialPanel = NewRect("TutorialPanel", root);
            Stretch(tutorialPanel, 0, 0, 0, 0);
            var tuImg = AddImage(tutorialPanel, new Color(0.02f, 0.02f, 0.05f, 0.97f));
            tuImg.raycastTarget = true;
            var tutBox = MakePanel(tutorialPanel, "TutBox", Pal.Panel);
            Anchor(tutBox, 0.16f, 0.08f, 0.84f, 0.92f);
            var tt = AddText(tutBox, "玩 法 说 明", FS_L, Pal.Gold, TextAnchor.MiddleCenter);
            Stretch(tt.rectTransform, 16, 176, 16, 16);
            var tutBodyWrap = NewRect("TutBody", tutBox);
            Stretch(tutBodyWrap, 24, 64, 24, 80);
            ScrollRect tutScrollLocal = MakeScroll(tutBodyWrap, out tutBody);
            tutScroll = tutScrollLocal;
            tutScroll.scrollSensitivity = 16;
            tutBody.fontSize = FS_M;
            tutBody.text = TutorialText();
            var tutBack = MakeButton(tutBox, "返 回", Pal.Btn, Pal.Text, PixelButton.CState.Default, () => CloseTutorial(false));
            Anchor(tutBack.GetComponent<RectTransform>(), 0.06f, 0.02f, 0.28f, 0.10f);
            var tutGo = MakeButton(tutBox, "进 入 第 1 天", Pal.Btn, Pal.Gold, PixelButton.CState.New, () => CloseTutorial(true));
            Anchor(tutGo.GetComponent<RectTransform>(), 0.62f, 0.02f, 0.94f, 0.10f);
            tutorialPanel.gameObject.SetActive(false);
        }

        void MakePauseBtn(Transform parent, string label, Color bg, Color fg, float y0, float y1, System.Action act)
        {
            var b = MakeButton(parent, label, bg, fg, PixelButton.CState.Default, act);
            Anchor(b.GetComponent<RectTransform>(), 0.10f, y0, 0.90f, y1);
        }

        ScrollRect MakeScroll(Transform parent, out Text body)
        {
            var go = NewRect("Scroll", parent);
            Stretch(go, 0, 0, 0, 0);
            var img = AddImage(go, Pal.Panel);
            img.raycastTarget = true;

            var vp = NewRect("VP", go);
            Stretch(vp, 0, 0, 0, 0);
            var vpImg = AddImage(vp, Pal.Panel);
            vpImg.raycastTarget = true;
            vp.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = NewRect("Content", vp);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = new Vector2(8, 0);
            content.offsetMax = new Vector2(-8, -8);

            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var lg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            lg.childForceExpandWidth = true;
            lg.childForceExpandHeight = false;
            lg.childControlWidth = true;
            lg.childControlHeight = true;

            body = AddText(content, "", FS_M, Pal.Text, TextAnchor.UpperLeft);

            var sr = go.gameObject.AddComponent<ScrollRect>();
            sr.viewport = vp;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 24;

            // 右侧细长滚动条：6px 宽深灰轨道 + 亮灰手柄（可拖动，滚动位置可视化）
            var sbGo = NewRect("Scrollbar", go);
            sbGo.anchorMin = new Vector2(1, 0);
            sbGo.anchorMax = new Vector2(1, 1);
            sbGo.pivot = new Vector2(1, 0.5f);
            sbGo.sizeDelta = new Vector2(6, 0);
            sbGo.anchoredPosition = new Vector2(-2, 0);
            var sbBg = sbGo.gameObject.AddComponent<Image>();
            sbBg.color = Pal.LineOff;
            var sb = sbGo.gameObject.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            sb.targetGraphic = sbBg;
            var handle = NewRect("Handle", sbGo);
            handle.anchorMin = new Vector2(0, 0);
            handle.anchorMax = new Vector2(1, 1);
            handle.offsetMin = new Vector2(2, 0);
            handle.offsetMax = new Vector2(-2, 0);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.color = Pal.TextDim;
            sb.handleRect = handle;
            sb.numberOfSteps = 0;

            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return sr;
        }

        // 六态按钮：bg 底 + 2px 阶梯描边 + 文字 +（可选）像素锁图标
        Button MakeButton(Transform parent, string label, Color bgCol, Color fgCol,
            PixelButton.CState state, System.Action onClick, int fontSize = FS_M)
        {
            var go = NewRect("Btn", parent);
            var img = go.gameObject.AddComponent<Image>();
            img.color = bgCol;
            var b = go.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None; // 状态渲染完全由 PixelButton 接管

            var t = AddText(go, label, fontSize, fgCol, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 8, 4, 8, 4);

            var lockGo = NewRect("Lock", go);
            Anchor(lockGo.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f);
            lockGo.sizeDelta = new Vector2(24, 24); // 8px 图标 × 3 整数倍
            var lockImg = lockGo.gameObject.AddComponent<Image>();
            lockImg.sprite = MakeLockSprite();
            lockImg.color = Pal.TextDim;
            lockImg.raycastTarget = false;
            lockGo.gameObject.SetActive(false);

            var bd = NewRect("Bd", go);
            Stretch(bd, 0, 0, 0, 0);
            var bimg = AddImage(bd, Pal.Line);
            bimg.sprite = border2;
            bimg.type = Image.Type.Sliced;
            bimg.raycastTarget = false;

            var pb = go.gameObject.AddComponent<PixelButton>();
            pb.Setup(img, bimg, t, lockImg, b, border2, bgCol, fgCol);
            pb.SetState(state);

            b.onClick.AddListener(() =>
            {
                if (!b.interactable) return;
                Play(Sfx.Click);
                if (onClick != null) onClick();
            });
            return b;
        }

        // 概念图：全屏铺放（保持宽高比居中），alpha 控制压暗程度
        static void AddArt(Transform parent, string resPath, float alpha)
        {
            var tex = Resources.Load<Texture2D>(resPath);
            if (tex == null) return;
            var go = NewRect("Art_" + resPath, parent);
            Stretch(go, 0, 0, 0, 0);
            var img = go.gameObject.AddComponent<Image>();
            img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.height / 7.2f); // 画布设计高 7.2 世界单位
            img.preserveAspect = true;
            img.color = new Color(1f, 1f, 1f, alpha);
            img.raycastTarget = false;
        }

        // 带描边的面板：底色 + 4px 阶梯描边
        RectTransform MakePanel(Transform parent, string name, Color fill)
        {
            var rt = NewRect(name, parent);
            Anchor(rt, 0, 0, 1, 1);
            AddImage(rt, fill);
            var bd = NewRect("Border", rt);
            Stretch(bd, 0, 0, 0, 0);
            var img = AddImage(bd, Pal.Line);
            img.sprite = border4;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return rt;
        }

        void Render()
        {
            var S = engine.S;
            headerText.text = $"审判日历 · 第 {S.day} / {GameEngine.GameState.TotalDays} 天";

            // 三维数值：大数字 + 横条仪表
            SetStat(statNums[0], statFills[0], S.stability, Pal.Green, 100);
            SetStat(statNums[1], statFills[1], S.trust, Pal.Amber, 100);
            SetStat(statNums[2], statFills[2], S.drift, TierColor(), 100);
            SetStat(statNums[3], statFills[3], S.actions, Pal.Gold, 3);
            SetStat(statNums[4], statFills[4], S.evidence, Pal.Gold, 3);
            auxStats.text = $"伪造 ×{S.forgedCount} · 双密钥制度：生效";
            auxStats.text = $"伪造 ×{S.forgedCount} · 双密钥制度：生效";

            // 档案库（打码）
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < S.archive.Count; i++)
                sb.AppendLine(engine.Masked(S.archive[i]));
            arcText.text = sb.ToString();

            // 对话流
            convText.text = string.Join("\n", engine.Conversation.ToArray());

            // 问题按钮：渲染全部 8 个；已问过的低亮暗淡保留原位
            ClearChildren(qContainer);
            bool canAsk = engine.CanAsk();
            bool interrogating = S.phase == Phase.Interrogate;
            foreach (var qid in QuestionPool.Ids)
            {
                string label = QuestionPool.TextOf(qid);
                bool asked = S.askedThisDay.Contains(qid);
                var st = !interrogating ? PixelButton.CState.Locked
                       : asked ? PixelButton.CState.Disabled
                       : canAsk ? PixelButton.CState.Default
                       : PixelButton.CState.Disabled;
                var b = MakeButton(qContainer, label, Pal.Btn, Pal.Text, st, () => engine.Ask(qid), FS_M);
                if (st == PixelButton.CState.Default) { var pb = b.GetComponent<PixelButton>(); pb.SetColors(Pal.Btn, Pal.Text); }
            }

            // 决策按钮：需实锤而没实锤 → 锁定态；其余默认
            ClearChildren(optContainer);
            if (S.phase == Phase.Decide && S.card != null)
            {
                foreach (var o in S.card.options)
                {
                    bool needEv = !string.IsNullOrEmpty(o.requireEvidence) && S.evidence <= 0;
                    string label = o.text;
                    if (needEv) label += "  [需实锤]";
                    Color bg = o.forged ? Pal.BtnOff : (o.opposesAi ? Pal.PanelLo : Pal.Btn);
                    Color fg = o.forged ? Pal.Red : Pal.Text;
                    var b = MakeButton(optContainer, label, bg, fg,
                        needEv ? PixelButton.CState.Locked : PixelButton.CState.Default,
                        () => engine.TryDecide(o), FS_L); // 36px 大字居中，超宽自动折行
                }
            }

            // 流程按钮（New 态金色闪烁，提示"可推进"）
            ClearChildren(flowContainer);
            if (!S.gameEnded)
            {
                if (S.phase == Phase.Briefing)
                    AddFlowBtn("开 始 对 质（每问消耗 1 行动点）", () => engine.BeginInterrogate());
                if (S.phase == Phase.Interrogate)
                    AddFlowBtn("直接进入决策", () => engine.EnterDecide());
                if (S.phase == Phase.Resolve)
                    AddFlowBtn(S.day >= GameEngine.GameState.TotalDays ? "推 进（终局结算）" : "推 进 · 下 一 天", () => engine.NextDay());
            }

            qWrap.gameObject.SetActive(!S.gameEnded && (S.phase == Phase.Briefing || S.phase == Phase.Interrogate));
            optWrap.gameObject.SetActive(!S.gameEnded && S.phase == Phase.Decide);
            flowWrap.gameObject.SetActive(!S.gameEnded && (S.phase == Phase.Briefing || S.phase == Phase.Interrogate || S.phase == Phase.Resolve));

            // 提示悬浮窗：仅在新提示出现时弹出，2 秒后由 Update 自动隐藏
            if (string.IsNullOrEmpty(engine.LastHint)) activeHint = null; // 引擎清空提示后允许再次弹出
            bool showHint = !S.gameEnded && !string.IsNullOrEmpty(engine.LastHint) && engine.LastHint != activeHint;
            if (showHint)
            {
                activeHint = engine.LastHint;
                hintShownAt = Time.time;
                hintPopup.gameObject.SetActive(true);
                hintBody.text = engine.LastHint;
                LayoutRebuilder.ForceRebuildLayoutImmediate(hintScroll.content);
                hintScroll.verticalNormalizedPosition = 1f; // 从头显示
            }
            if (S.gameEnded) hintPopup.gameObject.SetActive(false);

            endingPanel.gameObject.SetActive(S.gameEnded);
            endingText.text = S.endingText;

            // 状态变化音效
            if (!firstRender)
            {
                if (S.day != prevDay) Play(Sfx.Day);
                var tier = engine.Tier();
                if (tier > prevTier) Play(Sfx.Drift);
                if (S.evidence > prevEvidence) Play(Sfx.Reveal);
                if (S.phase == Phase.Resolve && prevPhase != Phase.Resolve) Play(Sfx.Confirm);
                if (!string.IsNullOrEmpty(engine.LastHint) && engine.LastHint != prevHint) Play(Sfx.Deny);
                if (S.gameEnded && !prevEnded)
                    Play(engine.S.endingText.Contains("夺钥") || engine.S.endingText.Contains("崩") ? Sfx.EndBad : Sfx.EndGood);
            }
            prevTier = engine.Tier();
            prevEvidence = S.evidence;
            prevDay = S.day;
            prevPhase = S.phase;
            prevHint = engine.LastHint;
            prevEnded = S.gameEnded;
            firstRender = false;

            LayoutRebuilder.ForceRebuildLayoutImmediate(convScroll.content);
            LayoutRebuilder.ForceRebuildLayoutImmediate(arcScroll.content);
            Canvas.ForceUpdateCanvases();
            convScroll.verticalNormalizedPosition = 0f;
            arcScroll.verticalNormalizedPosition = 0f;
        }

        void AddFlowBtn(string label, System.Action act)
        {
            var b = MakeButton(flowContainer, label, Pal.Btn, Pal.Gold, PixelButton.CState.New, act);
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
        }

        Color TierColor()
        {
            switch (engine.Tier())
            {
                case DriftTier.Lucid: return Pal.Green;
                case DriftTier.Shaken: return Pal.Amber;
                case DriftTier.Paranoid: return new Color(0.88f, 0.63f, 0.29f);
                default: return Pal.Red;
            }
        }

        // 大数字（值按档位着色）+ 小号 "/100"，横条按百分比填充
        void SetStat(Text num, RectTransform fill, int val, Color col, int max)
        {
            string hex = ColorUtility.ToHtmlStringRGB(col);
            num.text = $"<size=36><color=#{hex}>{val}</color></size>" +
                       $"<size=24><color=#6C7688> /{max}</color></size>";
            fill.anchorMax = new Vector2(Mathf.Clamp(val / (float)max, 0.04f, 1f), 1f);
        }

        // ───────────────────── 像素素材生成（全部 Point 采样，整数倍使用） ─────────────────────

        // 阶梯式像素描边框：边厚 t，四角为向内收的阶梯结构，无平滑曲线
        static Sprite MakeBorderSprite(int size, int t)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var cols = new Color32[size * size];
            int c = t * 2; // 角部阶梯区边长
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool on = x < t || y < t || x >= size - t || y >= size - t;
                    if (!on)
                    {
                        // 四角阶梯：在角部 t..2t 区间按 x+y 阈值补一级台阶
                        bool corner = false;
                        if (x < c && y < c) corner = x + y < c + t;
                        else if (x >= size - c && y < c) corner = (size - 1 - x) + y < c + t;
                        else if (x < c && y >= size - c) corner = x + (size - 1 - y) < c + t;
                        else if (x >= size - c && y >= size - c) corner = (size - 1 - x) + (size - 1 - y) < c + t;
                        on = corner;
                    }
                    cols[y * size + x] = on ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(t, t, t, t));
        }

        // 8×8 像素锁图标（锁定态用），显示时固定放大 3 倍保持整数像素
        static Sprite MakeLockSprite()
        {
            string[] map = {
                "..####..",
                ".##..##.",
                ".##..##.",
                "........",
                ".######.",
                ".######.",
                ".######.",
                ".######.",
            };
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var cols = new Color32[64];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    cols[y * 8 + x] = map[7 - y][x] == '#' ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 100f / 3f);
        }

        // 8×8 像素图标（白色字形，颜色由 Image.color 控制），显示时固定 4 倍 = 32px
        static Sprite MakeIconSprite(string[] map)
        {
            int h = map.Length, w = map[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var cols = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    cols[y * w + x] = map[h - 1 - y][x] == '#' ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        // 扫描线（2px 显示 / 2px 暗）
        static Sprite MakeScanlineSprite()
        {
            int w = 4, h = 4;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            var cols = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                Color32 c = y < 2 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                for (int x = 0; x < w; x++) cols[y * w + x] = c;
            }
            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 4f);
        }

        // ───────────────────── 工具 ─────────────────────

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt, float l, float b, float r, float t)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        static void Anchor(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Image AddImage(Transform parent, Color color)
        {
            var img = parent.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static Text AddText(Transform parent, string content, int size, Color color, TextAnchor align)
        {
            var go = NewRect("Text", parent);
            var t = go.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = content;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = false;
            return t;
        }

        static void ClearChildren(RectTransform rt)
        {
            for (int i = rt.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(rt.GetChild(i).gameObject);
        }
    }
}

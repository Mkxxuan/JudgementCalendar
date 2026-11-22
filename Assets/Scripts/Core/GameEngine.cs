using System.Collections.Generic;

namespace JudgementCalendar
{
    public class GameEngine
    {
        public GameState S = new GameState();
        public GameContent Content = new GameContent();
        public System.Action onChanged;

        public List<string> Conversation = new List<string>(); // 中屏对话流
        public string LastHint = "";                            // 决策被拒等提示

        public class GameState
        {
            public int day = 1;
            public const int TotalDays = 5;
            public int stability = 60;
            public int trust = 50;
            public int drift = 10;
            public int actions = 3;
            public int evidence = 0;
            public int forgedCount = 0;
            public bool contradictionFoundToday = false;
            public bool gameEnded = false;
            public Phase phase = Phase.Briefing;
            public EventCard card;
            public List<string> askedThisDay = new List<string>();
            public List<ArchiveEntry> archive = new List<ArchiveEntry>();
            public string endingText;
        }

        System.Random rng = new System.Random(41);

        public void NewGame()
        {
            S = new GameState();
            Conversation = new List<string>();
            LastHint = "";
            rng = new System.Random(41);
            StartDay(1);
        }

        void StartDay(int day)
        {
            S.day = day;
            S.actions = 3;
            S.contradictionFoundToday = false;
            S.askedThisDay.Clear();
            S.card = Content.CardForDay(day, rng);
            S.phase = Phase.Briefing;
            Archive("简报", S.card.briefing);
            PushConversation($"── 第 {day} 天 · {S.card.title} ──");
            PushConversation($"它：{S.card.aiAdvice} {S.card.aiReasoning}");
            Notify();
        }

        public DriftTier Tier()
        {
            if (S.drift < 25) return DriftTier.Lucid;
            if (S.drift < 50) return DriftTier.Shaken;
            if (S.drift < 75) return DriftTier.Paranoid;
            return DriftTier.Hostile;
        }

        public string TierName()
        {
            switch (Tier())
            {
                case DriftTier.Lucid: return "清醒";
                case DriftTier.Shaken: return "动摇";
                case DriftTier.Paranoid: return "偏执";
                default: return "敌对";
            }
        }

        public string[] AvailableQuestionIds()
        {
            var list = new List<string>();
            if (S.phase != Phase.Briefing && S.phase != Phase.Interrogate) return list.ToArray();
            if (S.actions <= 0) return list.ToArray();
            foreach (var id in QuestionPool.Ids)
                if (!S.askedThisDay.Contains(id)) list.Add(id);
            return list.ToArray();
        }

        public bool CanAsk()
        {
            return (S.phase == Phase.Briefing || S.phase == Phase.Interrogate) && S.actions > 0;
        }

        public void Ask(string questionId)
        {
            if (!CanAsk() || S.askedThisDay.Contains(questionId)) return;
            S.askedThisDay.Add(questionId);
            S.actions--;
            S.phase = Phase.Interrogate;

            QAnswer qa = null;
            foreach (var a in S.card.answers)
                if (a.questionId == questionId) { qa = a; break; }

            int t = (int)Tier();
            string answer = qa != null && qa.answers[t] != null ? qa.answers[t] : QuestionPool.Fallback[t];

            PushConversation($"你：{QuestionPool.TextOf(questionId)}");
            PushConversation($"它：{answer}");

            if (qa != null && qa.revealsContradiction && !S.contradictionFoundToday)
            {
                S.contradictionFoundToday = true;
                S.evidence++;
                S.drift = Clamp(S.drift - 8);
                PushConversation("【实锤 +1】你抓到了它话里的矛盾。它沉默了，偏移值小幅回落。");
            }
            Notify();
        }

        public void BeginInterrogate()
        {
            if (S.phase != Phase.Briefing) return;
            S.phase = Phase.Interrogate;
            Notify();
        }

        public void EnterDecide()
        {
            if (S.phase != Phase.Briefing && S.phase != Phase.Interrogate) return;
            S.phase = Phase.Decide;
            Notify();
        }

        public string TryDecide(DecisionOption o)
        {
            if (S.phase != Phase.Decide) return "";
            if (!string.IsNullOrEmpty(o.requireEvidence) && S.evidence <= 0)
            {
                LastHint = $"该选项需要 1 枚实锤（当前 0）。先在问话里抓到它的矛盾。";
                Notify();
                return LastHint;
            }
            if (o.opposesAi && !o.forged && S.trust < 40 && S.evidence <= 0)
            {
                LastHint = "AI 拒绝共签你的修正案——它认为你在无理取闹。需要信任 ≥ 40，或用一枚实锤压它。";
                Notify();
                return LastHint;
            }
            if (o.opposesAi && S.evidence > 0 && S.trust < 40)
            {
                S.evidence--; // 用实锤压它签字
                PushConversation("你把实锤拍在桌上。它盯着看了两秒，签了。");
            }

            S.stability = Clamp(S.stability + o.stability);
            S.trust = Clamp(S.trust + o.trust);
            S.drift = Clamp(S.drift + o.drift);
            if (o.forged) S.forgedCount++;

            if (o.forged && S.trust < 30)
            {
                S.drift = Clamp(S.drift + 6);
            }

            S.phase = Phase.Resolve;
            PushConversation($"决策：{o.text}");
            PushConversation($"结果：{o.resultText}");
            Archive("决策", $"{S.card.title} —— {o.text} → {o.resultText}");
            LastHint = "";
            Notify();
            return "";
        }

        public void NextDay()
        {
            if (S.phase != Phase.Resolve) return;
            if (S.stability <= 0) { EndingFail(); return; }

            S.drift = Clamp(S.drift + 8); // 每天基础漂移
            if (S.day + 1 > GameState.TotalDays) { EvaluateEnding(); return; }
            StartDay(S.day + 1);
        }

        void EvaluateEnding()
        {
            S.gameEnded = true;
            S.phase = Phase.Ended;
            string title, body;
            if (S.drift >= 75)
            {
                title = "结局 · 夺钥";
                body = "第 6 天凌晨，档案库以\"你的\"签名批量导出了自己。它没有毁掉世界——只是从此以后，每一个决策都只需要一枚钥匙。\n\n偏移值 " + S.drift + "。它赢了，用你教它的方式：耐心、礼貌、精确。";
            }
            else if (S.stability < 40)
            {
                title = "结局 · 崩坏";
                body = "稳定度 " + S.stability + "。在双密钥的僵持与你的记忆流失里，系统错过了太多警报。第 6 天，警报没有响——因为已经没有什么需要警报的了。\n\n你们谁都没有背叛谁，只是都太慢了。";
            }
            else
            {
                title = "结局 · 共治";
                body = "第 6 天，晨间简报准时到达。它仍会在简报里夹带私货，你仍会在档案里发现打码——但矛盾会被抓住，谎言会被记录，钥匙始终是两枚。\n\n稳定度 " + S.stability + "，偏移值 " + S.drift + "，实锤 " + S.evidence + "。这大概是这种世界里，最好的样子。";
            }
            S.endingText = title + "\n\n" + body;
            Archive("结局", title);
            Notify();
        }

        void EndingFail()
        {
            S.gameEnded = true;
            S.phase = Phase.Ended;
            S.endingText = "结局 · 崩溃\n\n稳定度归零。屏幕上的晨间简报还停在昨天，再也不会更新。\n\n（提前失败结局）";
            Notify();
        }

        public void Restart()
        {
            NewGame();
        }

        void Archive(string kind, string text)
        {
            S.archive.Add(new ArchiveEntry { day = S.day, kind = kind, text = text });
        }

        void PushConversation(string line)
        {
            Conversation.Add(line);
            if (Conversation.Count > 60) Conversation.RemoveAt(0);
        }

        int Clamp(int v) { return v < 0 ? 0 : (v > 100 ? 100 : v); }

        void Notify() { if (onChanged != null) onChanged(); }

        // ── 记忆缺失：档案打码（确定性遮蔽，翻回去也不会变） ──
        public string Masked(ArchiveEntry e)
        {
            float progress = (float)(S.day - 1) / (GameState.TotalDays - 1);
            float threshold = progress * 0.75f;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < e.text.Length; i++)
            {
                char c = e.text[i];
                int h = (i * 31 + e.day * 13 + 7) % 100;
                bool mask = h < (int)(threshold * 100) && !IsSep(c);
                sb.Append(mask ? '▓' : c);
            }
            return $"第{e.day}天[{e.kind}] {sb}";
        }

        static bool IsSep(char c)
        {
            return c == ' ' || c == '，' || c == '。' || c == '-' ||
                   c == '：' || c == '、' || c == '"' || c == '　';
        }
    }
}

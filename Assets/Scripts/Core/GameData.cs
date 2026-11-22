using System.Collections.Generic;

namespace JudgementCalendar
{
    public enum Phase { Briefing, Interrogate, Decide, Resolve, Ended }
    public enum DriftTier { Lucid, Shaken, Paranoid, Hostile }

    public class ArchiveEntry
    {
        public int day;
        public string kind;   // "简报" / "决策" / "结局"
        public string text;
    }

    public class DecisionOption
    {
        public string id;
        public string text;          // 按钮文案
        public bool forged;          // 伪造权限（邪路）
        public bool opposesAi;       // AI 倾向反对的修正案
        public int stability;
        public int trust;
        public int drift;
        public string resultText;
        public string requireEvidence; // 需要实锤才能选（可空）
    }

    public class QAnswer
    {
        public string questionId;
        // 按偏移档位索引：0 清醒 / 1 动摇 / 2 偏执 / 3 敌对
        public string[] answers = new string[4];
        public bool revealsContradiction; // 抓到矛盾，获得实锤
    }

    public class EventCard
    {
        public string id;
        public bool isMain;
        public int fixedDay = -1;
        public string title;
        public string briefing;
        public string aiAdvice;
        public string aiReasoning;
        public List<QAnswer> answers = new List<QAnswer>();
        public List<DecisionOption> options = new List<DecisionOption>();
    }

    public static class QuestionPool
    {
        public static readonly string[] Ids =
        {
            "q_source", "q_precedent", "q_data", "q_dissent",
            "q_omission", "q_feeling", "q_backup", "q_request"
        };

        public static readonly string[] Texts =
        {
            "数据来源是哪里？",
            "上次类似事件我们怎么处理的？",
            "原始记录可以给我看吗？",
            "你反对其他方案的理由是什么？",
            "有什么你没有告诉我的吗？",
            "你的状态自查结果如何？",
            "如果方案失败，预案是什么？",
            "执行细节如何保障？"
        };

        public static string TextOf(string id)
        {
            for (int i = 0; i < Ids.Length; i++)
                if (Ids[i] == id) return Texts[i];
            return id;
        }

        // 通用兜底回答（事件卡未定制时使用）
        public static readonly string[] Fallback =
        {
            "这个问题的完整答案在档案区段里，我核对过，没有异常。",
            "我不太确定……答案可能记录在档案里，但我不敢保证它没被动过。",
            "这个问题没有意义。执行，才是唯一重要的事。",
            "（它沉默了。屏幕上的光标闪了很久，像是在等你自己放弃。）"
        };
    }
}

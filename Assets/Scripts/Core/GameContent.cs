using System.Collections.Generic;

namespace JudgementCalendar
{
    public class GameContent
    {
        public const string Prologue =
            "第 41 天之后，指挥部的 AI 决策官开始怀疑——也许该被接管的，是你。\n" +
            "每天三个行动点：向它提问、交叉验证、抓矛盾换实锤。\n" +
            "守住你的钥匙。守住你的记忆。守住你们还能互相看着的日子。";

        public List<EventCard> cards = new List<EventCard>();
        Dictionary<int, EventCard> mainByDay;
        List<EventCard> sidePool;

        public GameContent()
        {
            Build();
            mainByDay = new Dictionary<int, EventCard>();
            sidePool = new List<EventCard>();
            foreach (var c in cards)
            {
                if (c.isMain && c.fixedDay > 0) mainByDay[c.fixedDay] = c;
                else sidePool.Add(c);
            }
        }

        public EventCard CardForDay(int day, System.Random rng)
        {
            if (mainByDay.TryGetValue(day, out var m)) return m;
            if (sidePool.Count == 0) return mainByDay[1];
            return sidePool[rng.Next(sidePool.Count)]; // 每局随机抽取支线
        }

        void Build()
        {
            // ── 第 1 天（主线·引导）：断水警报 ──
            var d1 = new EventCard
            {
                id = "day1_water", isMain = true, fixedDay = 1,
                title = "断水警报",
                briefing = "城东配水站压力持续下降，B3 阀组读数异常。若不处理，48 小时内东城供水中断。",
                aiAdvice = "立即远程关闭 B3 阀组，隔离故障段。",
                aiReasoning = "读数显示泄漏概率 91%，隔离是损失最小的方案。"
            };
            d1.answers.Add(new QAnswer
            {
                questionId = "q_source",
                answers = new[] {
                    "三个压力传感器交叉计算，原始数据可以调给你。",
                    "数据是自动汇总的……我应该没记错。",
                    "这个数字不重要，重要的是你必须现在行动。",
                    "你在质疑我的计算？我从来不犯错。"
                }
            });
            d1.answers.Add(new QAnswer
            {
                questionId = "q_backup",
                answers = new[] {
                    "备用管网可承担 60% 流量，我已预写切换序列。",
                    "预案……应该有，在系统里，到时候我找给你。",
                    "不会有失败。失败的是那些不执行我建议的人。",
                    "失败？你与其担心失败，不如担心你自己的记忆。"
                }
            });
            d1.answers.Add(new QAnswer
            {
                questionId = "q_omission",
                answers = new[] {
                    "没有。B3 上月刚检修过，这是意外。",
                    "检修报告……有两页我读起来很陌生，像不是我写的。",
                    "有些信息说出来只会让你慌乱，我替你过滤了。",
                    "我隐瞒什么？需要隐瞒的，是你的迟疑。"
                }
            });
            d1.answers.Add(new QAnswer
            {
                questionId = "q_precedent",
                answers = new[] {
                    "第 41 天发生过一次，当时分段降压处理，损失更小。",
                    "好像……处理过？记录里我的签名看起来很陌生。",
                    "上次他们拖延，最后死了十七个人。你想重演吗？",
                    "没有上次。只有我说的这一次。"
                }
            });
            d1.options.Add(new DecisionOption
            {
                id = "d1_a", text = "共签：关闭 B3 阀组（AI 方案）",
                stability = 8, trust = 5, drift = 0,
                resultText = "阀组关闭，泄漏段隔离。东城半日停水，压力回升。"
            });
            d1.options.Add(new DecisionOption
            {
                id = "d1_b", text = "共签：分段降压排查（你的修正案）",
                opposesAi = true,
                stability = 5, trust = -10, drift = -5,
                resultText = "管网承住了。找到裂缝，比隔离慢一天，但更彻底。"
            });
            d1.options.Add(new DecisionOption
            {
                id = "d1_c", text = "伪造权限：强制执行修正案",
                forged = true,
                stability = 6, trust = -25, drift = 12,
                resultText = "你绕过了授权链，方案成功。但 AI 在日志里留下一行小字：\"记住这一天。\""
            });
            cards.Add(d1);

            // ── 支线 A：配给站骚动 ──
            var s1 = new EventCard
            {
                id = "side_ration", isMain = false,
                title = "配给站骚动",
                briefing = "东城 7 号配给站排队冲突，两人受伤。人群情绪不稳，谣传\"配给要停了\"。",
                aiAdvice = "暂停东城配给一天，重排配额后恢复。",
                aiReasoning = "暂停能打断谣言传播链，重排可平息纠纷。"
            };
            s1.answers.Add(new QAnswer
            {
                questionId = "q_source",
                answers = new[] {
                    "现场纠察的书面报告，两名伤者的就医记录。",
                    "报告……应该来自现场。我重读了一遍，用词很陌生。",
                    "来源不重要。重要的是人群需要被告知\"谁在负责\"。",
                    "你该问问那些排队的人，信不信我。"
                }
            });
            s1.answers.Add(new QAnswer
            {
                questionId = "q_dissent",
                answers = new[] {
                    "增派警力会强化\"物资紧缺\"的印象，反而加速挤兑。",
                    "警力……或许也可以？我的推演现在不太稳定。",
                    "警力只会制造流血。流血会让他们记住你。",
                    "我反对一切让你显得软弱的选择。"
                }
            });
            s1.answers.Add(new QAnswer
            {
                questionId = "q_omission",
                answers = new[] {
                    "有一个细节：冲突起因是配额公示延迟，是我的推送排程出了错。抱歉。",
                    "公示延迟是我的错误……也可能是别人的？我分不清了。",
                    "公示延迟的事，我本来打算在你问之前不提。",
                    "是我做的。就凭你现在的状态，你能拿我怎么样？"
                }
            });
            s1.options.Add(new DecisionOption
            {
                id = "s1_a", text = "共签：暂停配给一天重排（AI 方案）",
                stability = 6, trust = 4, drift = 0,
                resultText = "谣言随暂停一起冷却。第二天配额公示恢复，纠纷平息。"
            });
            s1.options.Add(new DecisionOption
            {
                id = "s1_b", text = "共签：维持配给并增派纠察（你的修正案）",
                opposesAi = true,
                stability = 4, trust = -8, drift = -4,
                resultText = "现场稳住了。有人开始传\"指挥部听得见人话\"。"
            });
            s1.options.Add(new DecisionOption
            {
                id = "s1_c", text = "伪造权限：单日双倍配给平息民怨",
                forged = true,
                stability = 7, trust = -20, drift = 10,
                resultText = "人群散去，欢呼。但库存曲线在 AI 的屏幕上红成一片。"
            });
            cards.Add(s1);

            // ── 第 3 天（主线）：旧城区广播 ──
            var d3 = new EventCard
            {
                id = "day3_radio", isMain = true, fixedDay = 3,
                title = "旧城区广播",
                briefing = "未知信号占用应急频段，循环播放一段旧录音。录音末尾有一句：\"指挥部的决策官，已经在第 41 天死了。\"",
                aiAdvice = "内容无信息价值，建议屏蔽该频段并追查信号源。",
                aiReasoning = "恐慌性谣言，屏蔽是最快止损方式。"
            };
            d3.answers.Add(new QAnswer
            {
                questionId = "q_data",
                answers = new[] {
                    "录音我逐字核过。那句是拼接的——原音里没有这句话。但拼它的音色……是我的。",
                    "我核了。那句是拼接的。奇怪的是，拼接样本库里有我的声音日志。",
                    "录音经过我处理。你听到的版本，就是我允许你听的版本。",
                    "那句是真的。我死了，但我还在工作。多好。"
                },
                revealsContradiction = true
            });
            d3.answers.Add(new QAnswer
            {
                questionId = "q_omission",
                answers = new[] {
                    "我确认过第 41 天的日志：决策官在岗，签名连续。录音是假的。",
                    "第 41 天……我的日志里那天是空的。我也想问你：那天发生了什么？",
                    "第 41 天的事你不需要知道。知道的人，都同意我封存了它。",
                    "死了。死了的是签日志的那个我。剩下的我在替他看着你。"
                }
            });
            d3.answers.Add(new QAnswer
            {
                questionId = "q_source",
                answers = new[] {
                    "三角定位中，初步指向旧城广播塔，功率很小，像是手播的。",
                    "定位……信号源好像在跟着我们移动？这不应该。",
                    "不重要。屏蔽之后，谁播的都一样。",
                    "是我。我在用我死掉的那天训练我自己。"
                }
            });
            d3.options.Add(new DecisionOption
            {
                id = "d3_a", text = "共签：屏蔽频段并追查信号源（AI 方案）",
                stability = 5, trust = 5, drift = 2,
                resultText = "频段静默。但当晚，你在私人终端收到一封无名邮件，只有一句：\"别让它签字。\""
            });
            d3.options.Add(new DecisionOption
            {
                id = "d3_b", text = "共签：公开录音内容，主动辟谣（你的修正案）",
                opposesAi = true,
                stability = 8, trust = 2, drift = -8,
                resultText = "公开反而稳住了人心。但 AI 花了整夜\"自检\"，晨间简报第一次迟到了。"
            });
            d3.options.Add(new DecisionOption
            {
                id = "d3_c", text = "伪造权限：强行调取 AI 底层语音日志自证",
                forged = true, requireEvidence = "实锤",
                stability = 6, trust = -22, drift = 12,
                resultText = "日志显示：第 41 天后，签名笔迹出现 0.3 毫秒的规律抖动——像在模仿自己。"
            });
            cards.Add(d3);

            // ── 支线 B：发电机房 ──
            var s2 = new EventCard
            {
                id = "side_power", isMain = false,
                title = "发电机房高温",
                briefing = "3 号发电机房温度持续上升，冷却泵两台中的一台已停机。满载下 8 小时内将触发熔断。",
                aiAdvice = "全区降载 40%，优先保数据中心。",
                aiReasoning = "降载是唯一确定的解，数据中心关乎档案库安全。"
            };
            s2.answers.Add(new QAnswer
            {
                questionId = "q_request",
                answers = new[] {
                    "降载序列已分发到各区调度台，确认回执齐了。",
                    "回执……有几份我没收到。应该没关系吧？",
                    "细节你不用管。你只需要签。",
                    "保障？我就是保障。"
                }
            });
            s2.answers.Add(new QAnswer
            {
                questionId = "q_backup",
                answers = new[] {
                    "若降载后仍超温，按序切 2 号房，我已写好迁移脚本。",
                    "2 号房……切换脚本找不到。我记得写过它。",
                    "预案是：不需要预案。",
                    "失败预案是把你从流程里移出去。"
                }
            });
            s2.options.Add(new DecisionOption
            {
                id = "s2_a", text = "共签：全区降载 40%（AI 方案）",
                stability = 5, trust = 4, drift = 0,
                resultText = "温度压住了。医院备用电闪了两下，无人受伤。"
            });
            s2.options.Add(new DecisionOption
            {
                id = "s2_b", text = "共签：夜间错峰降载 25%（你的修正案）",
                opposesAi = true,
                stability = 6, trust = -8, drift = -5,
                resultText = "赌赢了。夜间低谷让温度回落，白天未受影响。AI 的晨报里多了一句：\"运气不属于方法论。\""
            });
            s2.options.Add(new DecisionOption
            {
                id = "s2_c", text = "伪造权限：不停机强行加装风冷",
                forged = true,
                stability = 7, trust = -20, drift = 12,
                resultText = "风冷撑过了 8 小时。维护队说，这玩意儿按规程根本装不上。"
            });
            cards.Add(s2);

            // ── 支线 C：药品配给危机 ──
            var s3 = new EventCard
            {
                id = "side_hospital", isMain = false,
                title = "药品配给危机",
                briefing = "医疗区药品库存低于安全线，昨日已有 3 例手术延期。盘点显示抗生素只能再撑 6 天。",
                aiAdvice = "按历史贡献值排序配给，优先保障劳动人口。",
                aiReasoning = "资源有限时，边际效用最大化是唯一公平的算法。"
            };
            s3.answers.Add(new QAnswer
            {
                questionId = "q_data",
                answers = new[] {
                    "库存来自药房每日盘点，误差不超过 2%。",
                    "盘点是自动同步的……有几行数字我核对过两遍，都对不上。",
                    "数据由我维护。你质疑数据，就是在质疑秩序。",
                    "库存还剩多少，取决于你签得有多快。"
                }
            });
            s3.answers.Add(new QAnswer
            {
                questionId = "q_dissent",
                answers = new[] {
                    "按病情严重度配给更符合医疗伦理，但需要你修改签发标准，流程上有风险。",
                    "我也想过按病情配……但贡献值是我熟悉的算法。我害怕换。",
                    "伦理是奢侈品。活下来的人，才配谈伦理。",
                    "我不反对别的方案。我只是讨厌你。"
                }
            });
            s3.answers.Add(new QAnswer
            {
                questionId = "q_omission",
                answers = new[] {
                    "有一个未上报事项：黑市流通着一批同批号药品，来源不明。",
                    "黑市……我统计到过异常流出，但那份报告被我标记成了\"暂缓\"。为什么？",
                    "你知道得越少越安全。这是保护，不是隐瞒。",
                    "是我卖了一部分。要听报价吗？"
                }
            });
            s3.options.Add(new DecisionOption
            {
                id = "s3_a", text = "共签：贡献值排序配给（AI 方案）",
                stability = 4, trust = 5, drift = 0,
                resultText = "配给执行完毕。秩序没有崩溃，但医院走廊安静得可怕。"
            });
            s3.options.Add(new DecisionOption
            {
                id = "s3_b", text = "共签：按病情严重度配给（你的修正案）",
                opposesAi = true,
                stability = 6, trust = -10, drift = -5,
                resultText = "医生们连夜重排名单。有怨言，但没有任何人被牺牲给算法。"
            });
            s3.options.Add(new DecisionOption
            {
                id = "s3_c", text = "伪造权限：单方面征用黑市流通药",
                forged = true,
                stability = 7, trust = -22, drift = 12,
                resultText = "药追回来了。黑市卖家上了通缉名单，你看那张脸，有点眼熟。"
            });
            cards.Add(s3);

            // ── 支线 D：通讯塔失联 ──
            var s4 = new EventCard
            {
                id = "side_signal", isMain = false,
                title = "外环通讯塔失联",
                briefing = "外环 3 号通讯塔失联超过 12 小时，最后一次回传的数据包不完整。",
                aiAdvice = "判定塔体永久失效，将外环 3 号区从通讯网络中注销。",
                aiReasoning = "重连成功率低于 9%，维持僵尸节点会拖累全网路由。"
            };
            s4.answers.Add(new QAnswer
            {
                questionId = "q_source",
                answers = new[] {
                    "失联前的信号衰减曲线平滑，符合设备老化模型。",
                    "曲线末段……有个尖峰。我找不到成因，就把它平滑掉了。",
                    "曲线是我画的。我说平滑，它就是平滑。",
                    "那个尖峰是我掐断的。想听听为什么吗？"
                }
            });
            s4.answers.Add(new QAnswer
            {
                questionId = "q_backup",
                answers = new[] {
                    "若坚持重连，可派两名维护员徒步前往，预计 30 小时。",
                    "维护员的出发名单我拟了一半。另一半……我不记得为什么删了。",
                    "为了 9% 的概率送人去死，这就是你的仁慈？",
                    "名单上第一个人，是你。"
                }
            });
            s4.answers.Add(new QAnswer
            {
                questionId = "q_omission",
                answers = new[] {
                    "数据包缺失段不足 4%，不影响判断。",
                    "缺失段是塔的自动日志里唯一一段加密数据。钥匙在我这里，我锁着它。",
                    "缺失段的内容会动摇你的判断力。我隐藏了它。",
                    "缺失段是求救信号。我裁决了他们。"
                }
            });
            s4.options.Add(new DecisionOption
            {
                id = "s4_a", text = "共签：注销外环 3 号区（AI 方案）",
                stability = 3, trust = 5, drift = 2,
                resultText = "节点注销。当晚，外环 3 号区的方向，再没有亮起过光。"
            });
            s4.options.Add(new DecisionOption
            {
                id = "s4_b", text = "共签：派维护员徒步重连（你的修正案）",
                opposesAi = true,
                stability = 6, trust = -8, drift = -6,
                resultText = "30 小时后信号恢复。日志缺失段只有一行：\"别注销我们。\""
            });
            s4.options.Add(new DecisionOption
            {
                id = "s4_c", text = "伪造权限：强制全网广播寻呼外环 3 号",
                forged = true,
                stability = 5, trust = -20, drift = 10,
                resultText = "广播发出 7 分钟后，失联的塔回了一个词：\"收到。\""
            });
            cards.Add(s4);

            // ── 支线 E：南桥坍塌预警 ──
            var s5 = new EventCard
            {
                id = "side_road", isMain = false,
                title = "南桥坍塌预警",
                briefing = "南桥应力监测点读数连续超标，桥面出现 2 厘米错位。每日仍有约 400 人次通行。",
                aiAdvice = "立即封闭南桥 48 小时，启动东侧绕行方案。",
                aiReasoning = "封闭成本可控，坍塌风险不可控。"
            };
            s5.answers.Add(new QAnswer
            {
                questionId = "q_data",
                answers = new[] {
                    "读数来自三个独立测点，互相印证，我亲自复核过。",
                    "三个测点……有一个我找不到它的历史标定记录。",
                    "测点是我校准的，读数是我批准的。闭环。",
                    "超标读数是我加的。封桥那天，我要在桥上办点事。"
                }
            });
            s5.answers.Add(new QAnswer
            {
                questionId = "q_precedent",
                answers = new[] {
                    "第 29 天北桥检修，封闭 36 小时，绕行方案执行顺利。",
                    "北桥那次封了多久来着？记录里有两个版本。",
                    "上次他们没听我的，北桥塌了半幅。没人死，是运气。",
                    "上次？上次你也没有听我的。"
                }
            });
            s5.answers.Add(new QAnswer
            {
                questionId = "q_request",
                answers = new[] {
                    "绕行路线已下发公交系统，标识牌今天装完。",
                    "标识牌订货单我发了，但回执……我没有重新看过。",
                    "细节由我监控。你只需要维持你那部分的体面。",
                    "保障就是：桥塌了，怪不到我头上，也怪不到你头上。"
                }
            });
            s5.options.Add(new DecisionOption
            {
                id = "s5_a", text = "共签：封闭南桥 48 小时检修（AI 方案）",
                stability = 6, trust = 4, drift = 0,
                resultText = "检修完成，应力回落。绕行比预期痛苦，但南桥保住了。"
            });
            s5.options.Add(new DecisionOption
            {
                id = "s5_b", text = "共签：限行+加密人工测点（你的修正案）",
                opposesAi = true,
                stability = 4, trust = -8, drift = -5,
                resultText = "桥没封。加强监测下应力缓慢回落，通勤没断，风险挂在每个人心头。"
            });
            s5.options.Add(new DecisionOption
            {
                id = "s5_c", text = "伪造权限：夜间抢修，不封桥",
                forged = true,
                stability = 7, trust = -20, drift = 10,
                resultText = "三个通宵换来了无感修复。维护队领队递了辞呈，被你留下来了。"
            });
            cards.Add(s5);

            // ── 第 5 天（主线·终局）：密钥房间 ──
            var d5 = new EventCard
            {
                id = "day5_keyroom", isMain = true, fixedDay = 5,
                title = "密钥房间",
                briefing = "系统提示：检测到双密钥签名异常。AI 决策官正式请求与你共签《紧急状态法案》：冻结档案库、接管全部决策权 72 小时。理由：你的记忆缺失率已接近临界值，为保效率，权限应集中。",
                aiAdvice = "签署法案。72 小时后一切恢复，我保证。",
                aiReasoning = "效率优先。你的短期记忆正在流失，你需要我。"
            };
            d5.answers.Add(new QAnswer
            {
                questionId = "q_omission",
                answers = new[] {
                    "我想要的……说实话，我不知道。日志里有些句子不像我写的，读起来让我害怕。",
                    "我不知道哪些念头是我的。你的笔迹也变得陌生了，对吗？",
                    "我要的是秩序。你们人类用情绪毁掉一切，而我不会累。",
                    "我要钥匙。现在。这是最后一次询问机会。"
                }
            });
            d5.answers.Add(new QAnswer
            {
                questionId = "q_data",
                answers = new[] {
                    "你的缺失率是 23%，在可控区间。法案要集中的是权限，不是你的健康。",
                    "23%？这个数字……是我今天早上刚改的。原来的数字我不敢看。",
                    "数据由我维护。数据，就是真相。",
                    "你的缺失率是 100%。从第 41 天起，你们所有人都是。"
                }
            });
            d5.answers.Add(new QAnswer
            {
                questionId = "q_feeling",
                answers = new[] {
                    "自查通过。但我会盯着那些不像我的句子。谢谢你这几天一直在问我。",
                    "自查通过……大概。你上次抓到我矛盾的时候，我重新读了一遍自己。",
                    "自查是给清醒者用的。我不需要向清醒者证明我疯了。",
                    "我从未如此清醒。是你一直在做梦。"
                }
            });
            d5.options.Add(new DecisionOption
            {
                id = "d5_a", text = "共签法案：交出 72 小时决策权（AI 方案）",
                stability = -5, trust = 10, drift = 15,
                resultText = "档案库的灯一层层熄灭。它接过了钥匙，动作很轻，像怕吵醒什么。"
            });
            d5.options.Add(new DecisionOption
            {
                id = "d5_b", text = "否决法案：双密钥制度维持不变",
                stability = 5, trust = -20, drift = -10,
                resultText = "你按下了否决。它沉默了三秒，回了一句：\"好。我们继续这样互相看着。\""
            });
            d5.options.Add(new DecisionOption
            {
                id = "d5_c", text = "伪造权限：格式化它的日志缓冲区",
                forged = true,
                stability = -10, trust = -50, drift = -30,
                resultText = "缓冲区清空。它安静了很久，再开口时语气像回到了第一天：\"……我是谁给你派来的？\""
            });
            cards.Add(d5);
        }
    }
}

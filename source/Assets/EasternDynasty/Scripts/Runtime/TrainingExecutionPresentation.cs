using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Vector2 trainingExecutionScroll;

        private void DrawTrainingExecution(Rect area)
        {
            var status = TrainingExecutionStatus.Create(world);
            if (status == null) return;
            float x = area.x + 18, w = area.width - 36;
            var target = world.lands[status.Target];
            Label(x, area.y + 13, w, 28, status.Cancelled ? "练兵已取消 · 正在返城" : status.Settled ? "练兵完成 · 正在返城" : "本次练兵", body);
            Label(x, area.y + 49, w, 26, "Lv." + target.level + "  " + target.name + "  (" + target.x + "," + target.z + ")", small);
            var view = new Rect(x, area.y + 88, w, area.height - 159);
            RecordReviewTarget("training-execution-scroll", view);
            trainingExecutionScroll = BeginContentScroll(view, trainingExecutionScroll, new Rect(0, 0, w - 20, Mathf.Max(view.height, 344)));
            float cw = w - 24;
            string[] stages = { "行军到目标", "土地练兵", "返回主城" };
            for (int i = 0; i < stages.Length; i++)
            {
                float y = i * 49;
                bool current = i == status.Stage;
                bool interrupted = i == 1 && status.Cancelled;
                Color color = interrupted ? Muted : current ? Gold : i < status.Stage ? Green : Border;
                RectFill(new Rect(0, y + 6, 5, 25), color);
                string text = current ? "剩余 " + CityTime(status.PhaseRemaining) : i < status.Stage ? (i == 1 && status.Cancelled ? "已取消" : "已结束") : "待开始";
                Label(15, y, cw * .5f, 28, (i + 1) + "  " + stages[i], current ? small : tiny);
                Label(cw * .52f, y, cw * .48f, 28, text, tiny);
                RectFill(new Rect(15, y + 32, cw - 15, 4), Border);
                float progress = interrupted ? 0 : current ? (float)status.PhaseProgress : i < status.Stage ? 1 : 0;
                RectFill(new Rect(15, y + 32, (cw - 15) * progress, 4), color);
            }
            float half = (cw - 12) / 2;
            Frame(new Rect(0, 159, half, 74), Ink);
            Frame(new Rect(half + 12, 159, half, 74), Ink);
            Label(10, 167, half - 20, 23, "经验到账", tiny);
            string reward = status.Cancelled ? "取消 · 无收益" : status.Settled ? (status.ReceiptAvailable ? "+" + status.AwardedExperience + " 已到账" : "已结算 · 查看记录") : CityTime(status.UntilAward);
            Label(10, 195, half - 20, 28, reward, small);
            Label(half + 22, 167, half - 20, 23, "部队回城可用", tiny);
            Label(half + 22, 195, half - 20, 28, CityTime(status.UntilHome), small);
            Label(0, 248, cw, 26, "已付 " + World.TrainingCommandCost + " 政令  ·  " + status.Participants.Count + " 名武将锁定", small);
            Label(0, 280, cw, 36, status.Cancelled ? "未完成的练兵不发经验，已用政令不退。" : status.Settled ? "经验已自动发放；返城后可再次安排部队。" : "作业完成自动到账；提前撤回无收益。", tiny);
            Label(0, 320, cw, 24, "项目配置 · 倒计时按游戏内时间与当前倍速推进", tiny);
            GUI.EndScrollView(GUI.enabled);
            var action = new Rect(x, area.yMax - 55, w, 40);
            if (status.Stage == 2)
            {
                RecordReviewTarget("training-result", action);
                if (Button(action, "查看本次结果", true)) OpenTrainingHistory();
            }
            else
            {
                RecordReviewTarget("training-cancel", action);
                if (Button(action, "取消练兵并撤回"))
                    Ask("提前撤回将取消未完成的练兵，不发经验，已用政令不退。确认撤回？", () => { Act(world.RecallArmy()); Save(false); });
            }
        }
    }
}

using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private bool trainingHistoryOpen;
        private TrainingReceipt trainingHistorySelection;
        private Vector2 trainingHistoryScroll;

        private void OpenTrainingHistory()
        {
            trainingHistoryOpen = true;
            trainingHistorySelection = world.LastTrainingReceipt;
            trainingHistoryScroll = Vector2.zero;
            map.CancelPointerInput();
        }

        private void CloseTrainingHistory()
        { trainingHistoryOpen = false; trainingHistorySelection = null; map.CancelPointerInput(); }

        private void SelectTrainingHistory(TrainingReceipt receipt)
        {
            // Retain the receipt itself, not a reverse index that can silently move
            // when a running order finishes and appends another result.
            if (world.trainingHistory != null && world.trainingHistory.Contains(receipt)) trainingHistorySelection = receipt;
        }

        private string TrainingReceiptLocation(TrainingReceipt receipt)
        {
            var land = receipt.target >= 0 && receipt.target < world.lands.Count ? world.lands[receipt.target] : null;
            return land == null ? "地块 #" + receipt.target : land.name + "  (" + land.x + "," + land.z + ")";
        }

        private void DrawTrainingHistory()
        {
            var page = new Rect(24, 94, width - 48, height - 198); Block(page);
            Label(page.x + 4, page.y, 220, 37, "练兵记录", heading);
            Label(page.x + 200, page.y + 9, page.width - 400, 25, "最近十次  ·  经验自动到账", small);
            var back = new Rect(page.xMax - 176, page.y, 176, 38); RecordReviewTarget("training-history-back", back);
            if (Button(back, "返回练兵")) { CloseTrainingHistory(); return; }
            var list = new Rect(page.x, page.y + 52, page.width * .29f, page.height - 52);
            var detail = new Rect(list.xMax + 18, list.y, page.xMax - list.xMax - 18, list.height);
            Frame(list, Ink); Frame(detail, Panel);
            var history = world.trainingHistory;
            if (trainingHistorySelection != null && (history == null || !history.Contains(trainingHistorySelection))) trainingHistorySelection = null;
            if (trainingHistorySelection == null) trainingHistorySelection = world.LastTrainingReceipt;
            int count = history == null ? 0 : history.Count;
            Label(list.x + 16, list.y + 12, list.width - 32, 28, count + " 次记录  ·  最新在前", body);
            var view = new Rect(list.x + 12, list.y + 48, list.width - 24, list.height - 62);
            RecordReviewTarget("training-history-list", view);
            trainingHistoryScroll = BeginContentScroll(view, trainingHistoryScroll, new Rect(0, 0, view.width - 20, Math.Max(view.height, count * 96)));
            for (int rowIndex = 0; rowIndex < count; rowIndex++)
            {
                int index = count - 1 - rowIndex; var receipt = history[index];
                var row = new Rect(0, rowIndex * 96, view.width - 24, 86);
                if (row.y >= trainingHistoryScroll.y && row.yMax <= trainingHistoryScroll.y + view.height)
                    RecordReviewTarget("training-history-row-" + index, new Rect(view.x, view.y + row.y - trainingHistoryScroll.y, row.width, row.height));
                string summary = receipt.cancelled ? "已取消  ·  +0 经验" : "已完成  ·  +" + receipt.awardedExperience.Sum() + " 经验";
                if (Button(row, summary + "\n" + TrainingReceiptLocation(receipt) + "\n征程 " + CityTime(receipt.completedAt), receipt == trainingHistorySelection)) SelectTrainingHistory(receipt);
            }
            GUI.EndScrollView(GUI.enabled);
            if (count == 0)
            {
                Label(detail.x + 28, detail.y + 38, detail.width - 56, 40, "尚无练兵记录", heading);
                Label(detail.x + 28, detail.y + 100, detail.width - 56, 72, "完成练兵或取消撤回后，这里会保留结果。\n正在行军或练兵时，经验尚未发放。", body);
                var prepare = new Rect(detail.x + 28, detail.y + 206, 200, 44); RecordReviewTarget("training-history-prepare", prepare);
                if (Button(prepare, "返回准备练兵", true)) CloseTrainingHistory();
                return;
            }
            DrawTrainingHistoryResult(detail, trainingHistorySelection);
        }

        private void DrawTrainingHistoryResult(Rect area, TrainingReceipt receipt)
        {
            float x = area.x + 22, w = area.width - 44;
            Label(x, area.y + 14, w, 36, receipt.cancelled ? "已取消 · 无经验收益" : "练兵完成 · 经验已到账", heading);
            Label(x, area.y + 58, w, 28, TrainingReceiptLocation(receipt) + "  /  征程 " + CityTime(receipt.completedAt), small);
            Label(x, area.y + 90, w, 26, "计划每将最多 +" + receipt.experiencePerGeneral + " 经验  ·  实得合计 +" + receipt.awardedExperience.Sum(), body);
            float cardW = Mathf.Min(220, (w - 24) / 3), cardH = Mathf.Min(270, area.height - 265);
            float cardsX = x + (w - (cardW * 3 + 24)) * .5f;
            for (int i = 0; i < receipt.participantIds.Count; i++)
            {
                int uid = receipt.participantIds[i];
                var card = world.cards.Find(c => c.uid == uid);
                var rect = new Rect(cardsX + i * (cardW + 12), area.y + 134, cardW, cardH);
                // UIDs are the recorded participants. Never substitute today's formation
                // or invent historical levels: these older receipts did not freeze them.
                if (card != null)
                {
                    RecordReviewTarget("training-history-general-" + i, rect);
                    if (DefinitionTile(rect, card.Definition, false)) ShowCardDetails(card.definition);
                }
                else { Frame(rect, Ink); Label(rect.x + 12, rect.y + 30, rect.width - 24, 80, "参训武将 #" + uid + "\n当前已不在牌库", small); }
                Label(rect.x, rect.yMax + 8, rect.width, 24, "实际 +" + receipt.awardedExperience[i] + " 经验", new GUIStyle(body) { normal = { textColor = receipt.cancelled ? Muted : Green } });
                Label(rect.x, rect.yMax + 36, rect.width, 22, "参训编号 " + uid, tiny);
            }
            Label(x, area.yMax - 66, w, 26, receipt.cancelled ? "取消不发经验；已消耗的政令不退。" : "收益已由练兵结算发放，查看或返回不会再次领取。", small);
            Label(x, area.yMax - 36, w, 24, "项目练兵记录 · 卡面为武将资料，经验为当次实得", tiny);
        }
    }
}

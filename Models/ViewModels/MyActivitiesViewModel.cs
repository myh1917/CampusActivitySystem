namespace CampusActivitySystem.Models.ViewModels
{
    /// <summary>
    /// 「我的活动」页面 ViewModel — 展示当前用户作为组织者创建的活动及其统计数据
    /// </summary>
    public class MyActivitiesViewModel
    {
        /// <summary>该组织者发布的活动列表（含每条活动的统计摘要）</summary>
        public List<MyActivityItem> Activities { get; set; } = new();

        /// <summary>发布的活动总数</summary>
        public int TotalActivities { get; set; }

        /// <summary>所有活动的报名人次总和</summary>
        public int TotalRegistrations { get; set; }

        /// <summary>所有活动的签到人次总和</summary>
        public int TotalSignIns { get; set; }

        /// <summary>需要关注的活动数（有待审核报名 或 已发布但即将开始）</summary>
        public int NeedsAttention { get; set; }
    }

    /// <summary>
    /// 列表中每条活动的摘要信息
    /// </summary>
    public class MyActivityItem
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Status { get; set; } = "DRAFT";
        public string Location { get; set; } = "";
        public int Capacity { get; set; }
        public int RegisteredCount { get; set; }
        public int SignedCount { get; set; }
        public int PendingAuditCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool NeedAudit { get; set; }
    }
}

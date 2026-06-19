using CampusActivitySystem.Models;

namespace CampusActivitySystem.Models.ViewModels;

public class SignControlViewModel
{
    public List<Activity> Activities { get; set; } = [];
    public Activity? SelectedActivity { get; set; }
    public SignInSession? ActiveSession { get; set; }
    public string? SignCode { get; set; }
    public int RegisteredCount { get; set; }
    public int SignedCount { get; set; }
    public List<SignIn> RecentSignIns { get; set; } = [];
}

public class StatisticsViewModel
{
    public List<Activity> Activities { get; set; } = [];
    public Activity? SelectedActivity { get; set; }
    public int RegisteredCount { get; set; }
    public int SignedCount { get; set; }
    public int UnsignedCount => Math.Max(0, RegisteredCount - SignedCount);
    public double SignRate => RegisteredCount == 0 ? 0 : SignedCount * 100.0 / RegisteredCount;
    public int QrCount { get; set; }
    public int CodeCount { get; set; }
    public int ManualCount { get; set; }
    public List<StatisticsRowViewModel> Rows { get; set; } = [];
}

public class StatisticsRowViewModel
{
    public long RegistrationId { get; set; }
    public string StudentNo { get; set; } = "";
    public string Name { get; set; } = "";
    public string College { get; set; } = "";
    public DateTime RegisteredAt { get; set; }
    public bool IsSigned { get; set; }
    public DateTime? CheckedAt { get; set; }
    public string Method { get; set; } = "";
}

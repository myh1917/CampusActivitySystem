using CampusActivitySystem.Data;
using CampusActivitySystem.Filters;
using CampusActivitySystem.Models;
using CampusActivitySystem.Models.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusActivitySystem.Controllers;

[Auth]
[Role("admin,organizer")]
public class StatisticsController : Controller
{
    private readonly AppDbContext _context;

    public StatisticsController(AppDbContext context) => _context = context;

    [HttpGet("/Statistics")]
    [HttpGet("/Statistics/Index")]
    [HttpGet("/Home/Statistics")]
    public async Task<IActionResult> Index(long? activityId)
    {
        var activities = await ManageableActivities().OrderByDescending(a => a.CreatedAt).ToListAsync();
        var selected = activityId.HasValue
            ? activities.FirstOrDefault(a => a.Id == activityId.Value)
            : activities.FirstOrDefault();
        var model = new StatisticsViewModel { Activities = activities, SelectedActivity = selected };
        if (selected != null) await PopulateAsync(model, selected.Id);
        return View(model);
    }

    public async Task<IActionResult> Export(long activityId, string type = "signin")
    {
        var activity = await ManageableActivities().FirstOrDefaultAsync(a => a.Id == activityId);
        if (activity == null) return Forbid();

        using var workbook = new XLWorkbook();
        if (type.Equals("registration", StringComparison.OrdinalIgnoreCase))
            await AddRegistrationSheetAsync(workbook, activity);
        else
            await AddSignInSheetAsync(workbook, activity);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var suffix = type.Equals("registration", StringComparison.OrdinalIgnoreCase) ? "报名名单" : "签到名单";
        var fileName = $"{SafeFileName(activity.Title)}_{suffix}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private async Task PopulateAsync(StatisticsViewModel model, long activityId)
    {
        var registrations = await _context.Registrations
            .Include(r => r.User)
            .Where(r => r.ActivityId == activityId && r.Status == "REGISTERED")
            .OrderBy(r => r.User.StudentNo)
            .ToListAsync();
        var signs = await _context.SignIns
            .Include(s => s.Session)
            .Where(s => s.Session.ActivityId == activityId)
            .OrderByDescending(s => s.CheckedAt)
            .ToListAsync();
        var latest = signs.GroupBy(s => s.RegistrationId).ToDictionary(g => g.Key, g => g.First());

        model.RegisteredCount = registrations.Count;
        model.SignedCount = registrations.Count(r => latest.ContainsKey(r.Id));
        model.QrCount = signs.Count(s => s.Method == "QR");
        model.CodeCount = signs.Count(s => s.Method == "CODE");
        model.ManualCount = signs.Count(s => s.Method == "MANUAL");
        model.Rows = registrations.Select(r =>
        {
            latest.TryGetValue(r.Id, out var sign);
            return new StatisticsRowViewModel
            {
                RegistrationId = r.Id,
                StudentNo = r.User.StudentNo,
                Name = r.User.Name,
                College = r.User.College,
                RegisteredAt = r.RegisteredAt,
                IsSigned = sign != null,
                CheckedAt = sign?.CheckedAt,
                Method = sign?.Method ?? ""
            };
        }).ToList();
    }

    private async Task AddRegistrationSheetAsync(XLWorkbook workbook, Activity activity)
    {
        var rows = await _context.Registrations.Include(r => r.User)
            .Where(r => r.ActivityId == activity.Id)
            .OrderBy(r => r.User.StudentNo).ToListAsync();
        var sheet = workbook.Worksheets.Add("报名名单");
        var headers = new[] { "活动", "学号", "姓名", "学院", "手机号", "报名时间", "状态" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            sheet.Cell(i + 2, 1).Value = SafeCell(activity.Title);
            sheet.Cell(i + 2, 2).Value = SafeCell(r.User.StudentNo);
            sheet.Cell(i + 2, 3).Value = SafeCell(r.User.Name);
            sheet.Cell(i + 2, 4).Value = SafeCell(r.User.College);
            sheet.Cell(i + 2, 5).Value = SafeCell(r.User.Phone);
            sheet.Cell(i + 2, 6).Value = r.RegisteredAt;
            sheet.Cell(i + 2, 7).Value = r.Status;
        }
        FormatSheet(sheet, headers.Length);
    }

    private async Task AddSignInSheetAsync(XLWorkbook workbook, Activity activity)
    {
        var rows = await _context.SignIns
            .Include(s => s.Session)
            .Include(s => s.Registration).ThenInclude(r => r.User)
            .Where(s => s.Session.ActivityId == activity.Id)
            .OrderBy(s => s.Registration.User.StudentNo).ThenBy(s => s.CheckedAt)
            .ToListAsync();
        var sheet = workbook.Worksheets.Add("签到名单");
        var headers = new[] { "活动", "学号", "姓名", "学院", "签到时间", "签到方式", "补签人ID", "补签原因" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        for (var i = 0; i < rows.Count; i++)
        {
            var s = rows[i];
            sheet.Cell(i + 2, 1).Value = SafeCell(activity.Title);
            sheet.Cell(i + 2, 2).Value = SafeCell(s.Registration.User.StudentNo);
            sheet.Cell(i + 2, 3).Value = SafeCell(s.Registration.User.Name);
            sheet.Cell(i + 2, 4).Value = SafeCell(s.Registration.User.College);
            sheet.Cell(i + 2, 5).Value = s.CheckedAt;
            sheet.Cell(i + 2, 6).Value = MethodName(s.Method);
            sheet.Cell(i + 2, 7).Value = s.OperatorId?.ToString() ?? "";
            sheet.Cell(i + 2, 8).Value = SafeCell(s.ManualReason);
        }
        FormatSheet(sheet, headers.Length);
    }

    private static void FormatSheet(IXLWorksheet sheet, int columns)
    {
        var header = sheet.Range(1, 1, 1, columns);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        sheet.RangeUsed()?.SetAutoFilter();
    }

    private IQueryable<Activity> ManageableActivities()
    {
        var roles = (HttpContext.Session.GetString("Roles") ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (roles.Contains("admin", StringComparer.OrdinalIgnoreCase)) return _context.Activities;
        var userId = long.TryParse(HttpContext.Session.GetString("UserId"), out var id) ? id : 0;
        return _context.Activities.Where(a => a.OrganizerId == userId);
    }

    private static string MethodName(string method) => method switch
    {
        "QR" => "二维码",
        "CODE" => "签到码",
        "MANUAL" => "手动补签",
        _ => method
    };

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    private static string SafeCell(string? value)
    {
        value ??= "";
        return value.Length > 0 && "=+-@".Contains(value[0]) ? $"'{value}" : value;
    }
}

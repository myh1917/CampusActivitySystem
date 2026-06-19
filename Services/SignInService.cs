using System.Security.Cryptography;
using System.Text;
using CampusActivitySystem.Data;
using CampusActivitySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusActivitySystem.Services;

public record SignInOperationResult(bool Success, string Message, SignIn? Record = null);
public record StartedSignInSession(SignInSession Session, string Code);

public class SignInService
{
    private readonly AppDbContext _context;
    private readonly byte[] _codeSecret;

    public SignInService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        var secret = configuration["SignIn:CodeSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            secret = "CampusActivitySystem-Development-SignIn-Secret";
        _codeSecret = Encoding.UTF8.GetBytes(secret);
    }

    public async Task<StartedSignInSession> StartAsync(long activityId, long createdBy, int durationMinutes)
    {
        durationMinutes = Math.Clamp(durationMinutes, 1, 240);
        var now = DateTime.Now;
        var oldSessions = await _context.SignInSessions
            .Where(s => s.ActivityId == activityId && s.Status == "ACTIVE")
            .ToListAsync();
        foreach (var old in oldSessions)
        {
            old.Status = "CLOSED";
            if (old.EndTime > now) old.EndTime = now;
        }

        var session = new SignInSession
        {
            ActivityId = activityId,
            Mode = "BOTH",
            SignCodeHash = "PENDING",
            StartTime = now,
            EndTime = now.AddMinutes(durationMinutes),
            Status = "ACTIVE",
            CreatedBy = createdBy,
            CreatedAt = now
        };
        _context.SignInSessions.Add(session);
        await _context.SaveChangesAsync();

        var code = GenerateCode(session);
        session.SignCodeHash = HashCode(code);
        await _context.SaveChangesAsync();
        return new StartedSignInSession(session, code);
    }

    public string GetDisplayCode(SignInSession session)
    {
        var code = GenerateCode(session);
        var expected = Convert.FromBase64String(HashCode(code));
        byte[] actual;
        try { actual = Convert.FromBase64String(session.SignCodeHash); }
        catch (FormatException) { return ""; }
        return CryptographicOperations.FixedTimeEquals(expected, actual) ? code : "";
    }

    public async Task<SignInOperationResult> CheckInByCodeAsync(long userId, string code, string? ipAddress)
    {
        code = code.Trim();
        if (code.Length != 6 || !code.All(char.IsDigit))
            return new(false, "请输入6位数字签到码");

        var now = DateTime.Now;
        var hash = HashCode(code);
        var session = await _context.SignInSessions
            .FirstOrDefaultAsync(s => s.SignCodeHash == hash && s.Status == "ACTIVE"
                                      && s.StartTime <= now && s.EndTime >= now);
        if (session == null)
            return new(false, "签到码无效、已过期或签到已关闭");

        return await CheckInAsync(session, userId, "CODE", ipAddress);
    }

    public async Task<SignInOperationResult> CheckInBySessionAsync(long userId, long sessionId, string? ipAddress)
    {
        var session = await _context.SignInSessions.FindAsync(sessionId);
        if (session == null)
            return new(false, "签到场次不存在");
        return await CheckInAsync(session, userId, "QR", ipAddress);
    }

    public async Task<SignInOperationResult> ManualAsync(
        long sessionId, long registrationId, long operatorId, string reason, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return new(false, "请填写补签原因");
        var session = await _context.SignInSessions.FindAsync(sessionId);
        if (session == null)
            return new(false, "签到场次不存在");

        var registration = await _context.Registrations
            .FirstOrDefaultAsync(r => r.Id == registrationId && r.ActivityId == session.ActivityId
                                      && r.Status == "REGISTERED");
        if (registration == null)
            return new(false, "该用户没有有效报名记录");

        return await CreateRecordAsync(session, registration, "MANUAL", operatorId,
            reason.Trim(), ipAddress);
    }

    private async Task<SignInOperationResult> CheckInAsync(
        SignInSession session, long userId, string method, string? ipAddress)
    {
        var now = DateTime.Now;
        if (session.Status != "ACTIVE" || now < session.StartTime || now > session.EndTime)
            return new(false, "签到尚未开始、已过期或已关闭");

        var registration = await _context.Registrations
            .FirstOrDefaultAsync(r => r.ActivityId == session.ActivityId && r.UserId == userId
                                      && r.Status == "REGISTERED");
        if (registration == null)
            return new(false, "您没有该活动的有效报名记录");

        return await CreateRecordAsync(session, registration, method, null, null, ipAddress);
    }

    private async Task<SignInOperationResult> CreateRecordAsync(
        SignInSession session, Registration registration, string method,
        long? operatorId, string? reason, string? ipAddress)
    {
        if (await _context.SignIns.AnyAsync(s => s.SessionId == session.Id
                                                 && s.RegistrationId == registration.Id))
            return new(false, "请勿重复签到");

        var record = new SignIn
        {
            SessionId = session.Id,
            RegistrationId = registration.Id,
            Method = method,
            CheckedAt = DateTime.Now,
            OperatorId = operatorId,
            ManualReason = reason ?? "",
            IpAddress = ipAddress ?? ""
        };
        registration.CheckinAt = record.CheckedAt;
        _context.SignIns.Add(record);
        try
        {
            await _context.SaveChangesAsync();
            return new(true, "签到成功", record);
        }
        catch (DbUpdateException)
        {
            _context.Entry(record).State = EntityState.Detached;
            return new(false, "请勿重复签到");
        }
    }

    private string GenerateCode(SignInSession session)
    {
        using var hmac = new HMACSHA256(_codeSecret);
        var input = Encoding.UTF8.GetBytes($"{session.Id}:{session.CreatedAt.Ticks}");
        var digest = hmac.ComputeHash(input);
        var number = BitConverter.ToUInt32(digest, 0) % 1_000_000;
        return number.ToString("D6");
    }

    private static string HashCode(string code)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    }
}

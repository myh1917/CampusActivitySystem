using CampusActivitySystem.Data;
using CampusActivitySystem.Models;
using CampusActivitySystem.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CampusActivitySystem.Tests;

public class SignInServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly SignInService _service;
    private readonly User _student;
    private readonly User _otherStudent;
    private readonly Activity _activity;
    private readonly Registration _registration;

    public SignInServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _student = NewUser("student", "2026001", "测试学生");
        _otherStudent = NewUser("other", "2026002", "未报名学生");
        var organizer = NewUser("organizer", "T001", "负责人");
        _context.Users.AddRange(_student, _otherStudent, organizer);
        _activity = new Activity
        {
            Title = "测试活动",
            Category = "测试",
            Organizer = organizer,
            Description = "",
            Location = "测试教室",
            Capacity = 100,
            RegisterStart = DateTime.Now.AddDays(-1),
            RegisterEnd = DateTime.Now.AddHours(-1),
            StartTime = DateTime.Now,
            EndTime = DateTime.Now.AddHours(2),
            Status = "PUBLISHED",
            Registrations = [],
            SignInSessions = []
        };
        _registration = new Registration
        {
            Activity = _activity,
            User = _student,
            Status = "REGISTERED",
            FormData = "",
            AuditComment = "",
            SignIns = []
        };
        _context.Registrations.Add(_registration);
        _context.SaveChanges();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SignIn:CodeSecret"] = "unit-test-secret"
            }).Build();
        _service = new SignInService(_context, configuration);
    }

    [Fact]
    public async Task ValidCodeCreatesSignInAndUpdatesRegistration()
    {
        var started = await _service.StartAsync(_activity.Id, _activity.OrganizerId, 30);
        var result = await _service.CheckInByCodeAsync(_student.Id, started.Code, "127.0.0.1");

        Assert.True(result.Success);
        Assert.Equal("CODE", result.Record?.Method);
        Assert.NotNull(_registration.CheckinAt);
        Assert.Single(_context.SignIns);
    }

    [Fact]
    public async Task InvalidCodeIsRejected()
    {
        await _service.StartAsync(_activity.Id, _activity.OrganizerId, 30);
        var result = await _service.CheckInByCodeAsync(_student.Id, "000000", null);

        Assert.False(result.Success);
        Assert.Empty(_context.SignIns);
    }

    [Fact]
    public async Task UserWithoutRegistrationIsRejected()
    {
        var started = await _service.StartAsync(_activity.Id, _activity.OrganizerId, 30);
        var result = await _service.CheckInByCodeAsync(_otherStudent.Id, started.Code, null);

        Assert.False(result.Success);
        Assert.Contains("报名", result.Message);
    }

    [Fact]
    public async Task DuplicateCheckInIsRejected()
    {
        var started = await _service.StartAsync(_activity.Id, _activity.OrganizerId, 30);
        var first = await _service.CheckInByCodeAsync(_student.Id, started.Code, null);
        var second = await _service.CheckInBySessionAsync(_student.Id, started.Session.Id, null);

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Contains("重复", second.Message);
        Assert.Single(_context.SignIns);
    }

    [Fact]
    public async Task ValidQrSessionCreatesQrSignIn()
    {
        var started = await _service.StartAsync(_activity.Id, _activity.OrganizerId, 30);
        var result = await _service.CheckInBySessionAsync(_student.Id, started.Session.Id, null);

        Assert.True(result.Success);
        Assert.Equal("QR", result.Record?.Method);
    }

    [Fact]
    public async Task ExpiredSessionIsRejected()
    {
        var started = await _service.StartAsync(_activity.Id, _activity.OrganizerId, 30);
        started.Session.EndTime = DateTime.Now.AddSeconds(-1);
        await _context.SaveChangesAsync();

        var result = await _service.CheckInBySessionAsync(_student.Id, started.Session.Id, null);

        Assert.False(result.Success);
        Assert.Contains("过期", result.Message);
    }

    [Fact]
    public async Task ManualCheckInStoresOperatorAndReason()
    {
        var started = await _service.StartAsync(_activity.Id, _activity.OrganizerId, 30);
        var result = await _service.ManualAsync(started.Session.Id, _registration.Id,
            _activity.OrganizerId, "设备故障", "127.0.0.1");

        Assert.True(result.Success);
        Assert.Equal("MANUAL", result.Record?.Method);
        Assert.Equal("设备故障", result.Record?.ManualReason);
        Assert.Equal(_activity.OrganizerId, result.Record?.OperatorId);
    }

    private static User NewUser(string account, string number, string name) => new()
    {
        Account = account,
        PasswordHash = "test",
        Name = name,
        StudentNo = number,
        College = "计算机学院",
        Phone = "13800000000",
        Status = "ACTIVE",
        UserRoles = []
    };

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}

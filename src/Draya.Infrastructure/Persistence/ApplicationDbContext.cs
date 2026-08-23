using Draya.Domain.Admin;
using Draya.Domain.Classrooms;
using Draya.Domain.Identity;
using Draya.Domain.Payments;
using Draya.Domain.Subscriptions;
using Draya.Domain.Wallets;
using Draya.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Draya.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<PlatformAdmin> PlatformAdmins => Set<PlatformAdmin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PiiMapping> PiiMappings => Set<PiiMapping>();
    public DbSet<UsageCounter> UsageCounters => Set<UsageCounter>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ClassroomType> ClassroomTypes => Set<ClassroomType>();
    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<ClassroomFeedback> ClassroomFeedback => Set<ClassroomFeedback>();
    public DbSet<ClassroomSection> ClassroomSections => Set<ClassroomSection>();    
    // Q&A Channel
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionReply> QuestionReplies => Set<QuestionReply>();
    public DbSet<QuestionVote> QuestionVotes => Set<QuestionVote>();
    // Wallet & Financial Model
    public DbSet<TeacherWallet> TeacherWallets => Set<TeacherWallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();
    public DbSet<TeacherPayoutAccount> TeacherPayoutAccounts => Set<TeacherPayoutAccount>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();

    // Materials
    public DbSet<Draya.Domain.Materials.LearningMaterial> LearningMaterials => Set<Draya.Domain.Materials.LearningMaterial>();
    public DbSet<Draya.Domain.Materials.MaterialVersion> MaterialVersions => Set<Draya.Domain.Materials.MaterialVersion>();
    public DbSet<Draya.Domain.Materials.MaterialChunk> MaterialChunks => Set<Draya.Domain.Materials.MaterialChunk>();
    public DbSet<Draya.Domain.Materials.VideoDetail> VideoDetails => Set<Draya.Domain.Materials.VideoDetail>();

    // Exams
    public DbSet<Draya.Domain.Exams.ExamGeneration> ExamGenerations => Set<Draya.Domain.Exams.ExamGeneration>();
    public DbSet<Draya.Domain.Exams.Exam> Exams => Set<Draya.Domain.Exams.Exam>();
    public DbSet<Draya.Domain.Exams.ExamQuestion> ExamQuestions => Set<Draya.Domain.Exams.ExamQuestion>();
    public DbSet<Draya.Domain.Exams.ExamQuestionOption> ExamQuestionOptions => Set<Draya.Domain.Exams.ExamQuestionOption>();

    public DbSet<Draya.Domain.Exams.StudentExamAttempt> StudentExamAttempts => Set<Draya.Domain.Exams.StudentExamAttempt>();
    public DbSet<Draya.Domain.Exams.StudentAnswer> StudentAnswers => Set<Draya.Domain.Exams.StudentAnswer>();
    public DbSet<Draya.Domain.Exams.AnswerGradingResult> AnswerGradingResults => Set<Draya.Domain.Exams.AnswerGradingResult>();
    public DbSet<Draya.Domain.Exams.ExamGradingJob> ExamGradingJobs => Set<Draya.Domain.Exams.ExamGradingJob>();

    // Reports
    public DbSet<Draya.Domain.Reports.PerformanceReport> PerformanceReports => Set<Draya.Domain.Reports.PerformanceReport>();
    public DbSet<Draya.Domain.Reports.StudentWeakness> StudentWeaknesses => Set<Draya.Domain.Reports.StudentWeakness>();
    public DbSet<Draya.Domain.Reports.StudentWeaknessHistory> StudentWeaknessHistories => Set<Draya.Domain.Reports.StudentWeaknessHistory>();
    public DbSet<Draya.Domain.Reports.WeaknessReview> WeaknessReviews => Set<Draya.Domain.Reports.WeaknessReview>();

    // Notifications
    public DbSet<Draya.Domain.Notifications.Notification> Notifications => Set<Draya.Domain.Notifications.Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

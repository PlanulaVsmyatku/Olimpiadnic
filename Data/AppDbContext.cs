using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Entities;

namespace Olimpiadnic.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AutoQuestion> AutoQuestions { get; set; }

    public virtual DbSet<AutoQuestionsSnapshot> AutoQuestionsSnapshots { get; set; }

    public virtual DbSet<AutoSubmissionResult> AutoSubmissionResults { get; set; }

    public virtual DbSet<Invite> Invites { get; set; }

    public virtual DbSet<ManualQuestionsConfig> ManualQuestionsConfigs { get; set; }

    public virtual DbSet<ManualQuestionsConfigSnapshot> ManualQuestionsConfigSnapshots { get; set; }

    public virtual DbSet<ManualSubmissionResult> ManualSubmissionResults { get; set; }

    public virtual DbSet<OlympStaff> OlympStaffs { get; set; }

    public virtual DbSet<Olympiad> Olympiads { get; set; }

    public virtual DbSet<OlympiadParticipant> OlympiadParticipants { get; set; }

    public virtual DbSet<OlympiadResult> OlympiadResults { get; set; }

    public virtual DbSet<OlympiadSnapshot> OlympiadSnapshots { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionAttachment> QuestionAttachments { get; set; }

    public virtual DbSet<QuestionAttachmentsSnapshot> QuestionAttachmentsSnapshots { get; set; }

    public virtual DbSet<QuestionsSnapshot> QuestionsSnapshots { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SubmissionItem> SubmissionItems { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AutoQuestion>(entity =>
        {
            entity.HasKey(e => e.QuestOptionId).HasName("PK__Auto_que__E4334DC7EA2CC8D1");

            entity.ToTable("Auto_questions");

            entity.HasIndex(e => e.QuestId, "IX_Auto_questions_quest_ID");

            entity.Property(e => e.QuestOptionId).HasColumnName("quest_option_ID");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.OptionText)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("option_text");
            entity.Property(e => e.QuestId).HasColumnName("quest_ID");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(d => d.Quest).WithMany(p => p.AutoQuestions)
                .HasForeignKey(d => d.QuestId)
                .HasConstraintName("FK__Auto_ques__quest__72C60C4A");
        });

        modelBuilder.Entity<AutoQuestionsSnapshot>(entity =>
        {
            entity.HasKey(e => e.QuestOptionId).HasName("PK__Auto_que__E4334DC7DE5D2A70");

            entity.ToTable("Auto_questions_snapshot");

            entity.Property(e => e.QuestOptionId).HasColumnName("quest_option_ID");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.OptionText)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("option_text");
            entity.Property(e => e.QuestSnapshotId).HasColumnName("quest_snapshot_ID");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(d => d.QuestSnapshot).WithMany(p => p.AutoQuestionsSnapshots)
                .HasForeignKey(d => d.QuestSnapshotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Auto_ques__quest__7F2BE32F");
        });

        modelBuilder.Entity<AutoSubmissionResult>(entity =>
        {
            entity.HasKey(e => new { e.SubmissionItemId, e.SelectedOptionId }).HasName("PK__Auto_Sub__993830C1D6F5E1D7");

            entity.ToTable("Auto_Submission_results");

            entity.Property(e => e.SubmissionItemId).HasColumnName("submission_item_ID");
            entity.Property(e => e.SelectedOptionId).HasColumnName("selected_option_ID");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");

            entity.HasOne(d => d.SubmissionItem).WithMany(p => p.AutoSubmissionResults)
                .HasForeignKey(d => d.SubmissionItemId)
                .HasConstraintName("FK__Auto_Subm__submi__0C85DE4D");
        });

        modelBuilder.Entity<Invite>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_Invites_InsteadOfUpdate"));

            entity.HasIndex(e => e.Email, "IX_Invites_Email");

            entity.HasIndex(e => e.ExpiresAt, "IX_Invites_ExpiresAt");

            entity.HasIndex(e => e.IsUsed, "IX_Invites_IsUsed");

            entity.HasIndex(e => e.Token, "IX_Invites_Token");

            entity.HasIndex(e => new { e.Token, e.IsUsed }, "IX_Invites_Token_IsUsed").HasFilter("([IsUsed]=(0))");

            entity.HasIndex(e => e.Token, "UQ__Invites__1EB4F817B57905CF").IsUnique();

            entity.Property(e => e.InviteId).HasColumnName("Invite_ID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.RoleId).HasColumnName("Role_ID");
            entity.Property(e => e.Token).HasMaxLength(255);
        });

        modelBuilder.Entity<ManualQuestionsConfig>(entity =>
        {
            entity.HasKey(e => e.QuestManualConfigId).HasName("PK__Manual_q__693CE9230F2F8ED8");

            entity.ToTable("Manual_questions_config");

            entity.HasIndex(e => e.QuestId, "IX_Manual_questions_config_quest_ID");

            entity.Property(e => e.QuestManualConfigId).HasColumnName("quest_manual_config_ID");
            entity.Property(e => e.MaxScore).HasColumnName("max_score");
            entity.Property(e => e.ModelAnswer)
                .HasMaxLength(1500)
                .IsUnicode(false)
                .HasColumnName("model_answer");
            entity.Property(e => e.QuestId).HasColumnName("quest_ID");

            entity.HasOne(d => d.Quest).WithMany(p => p.ManualQuestionsConfigs)
                .HasForeignKey(d => d.QuestId)
                .HasConstraintName("FK__Manual_qu__quest__76969D2E");
        });

        modelBuilder.Entity<ManualQuestionsConfigSnapshot>(entity =>
        {
            entity.HasKey(e => e.QuestManualConfigId).HasName("PK__Manual_q__693CE923A2C166A1");

            entity.ToTable("Manual_questions_config_snapshot");

            entity.Property(e => e.QuestManualConfigId).HasColumnName("quest_manual_config_ID");
            entity.Property(e => e.MaxScore).HasColumnName("max_score");
            entity.Property(e => e.ModelAnswer)
                .HasMaxLength(1500)
                .IsUnicode(false)
                .HasColumnName("model_answer");
            entity.Property(e => e.QuestSnapshotId).HasColumnName("quest_snapshot_ID");

            entity.HasOne(d => d.QuestSnapshot).WithMany(p => p.ManualQuestionsConfigSnapshots)
                .HasForeignKey(d => d.QuestSnapshotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Manual_qu__quest__02084FDA");
        });

        modelBuilder.Entity<ManualSubmissionResult>(entity =>
        {
            entity.HasKey(e => e.SubmissionItemId).HasName("PK__Manual_S__1FFF8177D22BE1B2");

            entity.ToTable("Manual_Submission_results");

            entity.Property(e => e.SubmissionItemId)
                .ValueGeneratedNever()
                .HasColumnName("submission_item_ID");
            entity.Property(e => e.AnswerText)
                .HasColumnType("text")
                .HasColumnName("answer_text");
            entity.Property(e => e.Commentary)
                .HasMaxLength(1500)
                .IsUnicode(false)
                .HasColumnName("commentary");
            entity.Property(e => e.ScoreValue).HasColumnName("score_value");

            entity.HasOne(d => d.SubmissionItem).WithOne(p => p.ManualSubmissionResult)
                .HasForeignKey<ManualSubmissionResult>(d => d.SubmissionItemId)
                .HasConstraintName("FK__Manual_Su__submi__10566F31");
        });

        modelBuilder.Entity<OlympStaff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Olymp_st__196CD19413F2E504");

            entity.ToTable("Olymp_staff");

            entity.HasIndex(e => e.OlympId, "IX_Olymp_staff_olymp_ID");

            entity.HasIndex(e => e.UserId, "IX_Olymp_staff_user_ID");

            entity.Property(e => e.StaffId).HasColumnName("staff_ID");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("assigned_at");
            entity.Property(e => e.OlimpRole)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("olimp_role");
            entity.Property(e => e.OlympId).HasColumnName("olymp_ID");
            entity.Property(e => e.UserId).HasColumnName("user_ID");

            entity.HasOne(d => d.Olymp).WithMany(p => p.OlympStaffs)
                .HasForeignKey(d => d.OlympId)
                .HasConstraintName("FK__Olymp_sta__olymp__6477ECF3");

            entity.HasOne(d => d.User).WithMany(p => p.OlympStaffs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Olymp_sta__user___656C112C");
        });

        modelBuilder.Entity<Olympiad>(entity =>
        {
            entity.HasKey(e => e.OlympId).HasName("PK__Olympiad__B43D3550C7094722");

            entity.Property(e => e.OlympId).HasColumnName("olymp_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Credentials)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("credentials");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EventEnd)
                .HasColumnType("datetime")
                .HasColumnName("event_end");
            entity.Property(e => e.EventStart)
                .HasColumnType("datetime")
                .HasColumnName("event_start");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("image_URL");
            entity.Property(e => e.RegistClosed)
                .HasColumnType("datetime")
                .HasColumnName("regist_closed");
            entity.Property(e => e.RegistOpen)
                .HasColumnType("datetime")
                .HasColumnName("regist_open");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("available")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("title");
        });

        modelBuilder.Entity<OlympiadParticipant>(entity =>
        {
            entity.HasKey(e => e.ParticipantId).HasName("PK__Olympiad__4E027C0EE6FB43D3");

            entity.ToTable("Olympiad_Participants");

            entity.HasIndex(e => e.OlympId, "IX_Olympiad_Participants_olymp_ID");

            entity.HasIndex(e => e.UserId, "IX_Olympiad_Participants_user_ID");

            entity.Property(e => e.ParticipantId).HasColumnName("participant_ID");
            entity.Property(e => e.CompletedAt)
                .HasColumnType("datetime")
                .HasColumnName("completed_at");
            entity.Property(e => e.OlympId).HasColumnName("olymp_ID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("reg_date");
            entity.Property(e => e.StartedAt)
                .HasColumnType("datetime")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("registered")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_ID");

            entity.HasOne(d => d.Olymp).WithMany(p => p.OlympiadParticipants)
                .HasForeignKey(d => d.OlympId)
                .HasConstraintName("FK__Olympiad___olymp__5FB337D6");

            entity.HasOne(d => d.User).WithMany(p => p.OlympiadParticipants)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Olympiad___user___5EBF139D");
        });

        modelBuilder.Entity<OlympiadResult>(entity =>
        {
            entity.HasKey(e => e.ResultsId).HasName("PK__Olympiad__F24EE3A7F477DE27");

            entity.ToTable("Olympiad_results");

            entity.HasIndex(e => e.ParticipantId, "IX_Olympiad_results_participant_ID");

            entity.Property(e => e.ResultsId).HasColumnName("results_ID");
            entity.Property(e => e.ParticipantId).HasColumnName("participant_ID");
            entity.Property(e => e.TotalScore).HasColumnName("total_score");

            entity.HasOne(d => d.Participant).WithMany(p => p.OlympiadResults)
                .HasForeignKey(d => d.ParticipantId)
                .HasConstraintName("FK__Olympiad___parti__04E4BC85");
        });

        modelBuilder.Entity<OlympiadSnapshot>(entity =>
        {
            entity.HasKey(e => e.OlympSnapId).HasName("PK__Olympiad__5272C28F3507EA20");

            entity.ToTable("Olympiad_snapshot");

            entity.Property(e => e.OlympSnapId).HasColumnName("olymp_snap_ID");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedAtSnap)
                .HasDefaultValueSql("(getdate())", "DF__Olympiad___creat__797309D9")
                .HasColumnType("datetime")
                .HasColumnName("created_at_snap");
            entity.Property(e => e.Credentials)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("credentials");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EventEnd)
                .HasColumnType("datetime")
                .HasColumnName("event_end");
            entity.Property(e => e.EventStart)
                .HasColumnType("datetime")
                .HasColumnName("event_start");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("image_URL");
            entity.Property(e => e.OriginalOlympId).HasColumnName("original_olymp_ID");
            entity.Property(e => e.RegistClosed)
                .HasColumnType("datetime")
                .HasColumnName("regist_closed");
            entity.Property(e => e.RegistOpen)
                .HasColumnType("datetime")
                .HasColumnName("regist_open");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestId).HasName("PK__Question__9A0E7CD5032DA9D8");

            entity.HasIndex(e => e.OlympId, "IX_Questions_olymp_ID");

            entity.Property(e => e.QuestId).HasColumnName("quest_ID");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.IsActual)
                .HasDefaultValue(true, "DF__Questions__is_ac__693CA210")
                .HasColumnName("is_actual");
            entity.Property(e => e.OlympId).HasColumnName("olymp_ID");
            entity.Property(e => e.QuestionOrder).HasColumnName("question_order");
            entity.Property(e => e.Type)
                .HasMaxLength(13)
                .IsUnicode(false)
                .HasColumnName("type");

            entity.HasOne(d => d.Olymp).WithMany(p => p.Questions)
                .HasForeignKey(d => d.OlympId)
                .HasConstraintName("FK__Questions__olymp__6A30C649");
        });

        modelBuilder.Entity<QuestionAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachId).HasName("PK__Question__7AA244C81ABCC237");

            entity.ToTable("Question_attachments");

            entity.HasIndex(e => e.QuestId, "IX_Question_attachments_quest_ID");

            entity.Property(e => e.AttachId).HasColumnName("attach_ID");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.QuestId).HasColumnName("quest_ID");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(d => d.Quest).WithMany(p => p.QuestionAttachments)
                .HasForeignKey(d => d.QuestId)
                .HasConstraintName("FK__Question___quest__6E01572D");
        });

        modelBuilder.Entity<QuestionAttachmentsSnapshot>(entity =>
        {
            entity.HasKey(e => e.AttachSnapId).HasName("PK__Question__8C21C1724ACF9623");

            entity.ToTable("Question_attachments_snapshot");

            entity.Property(e => e.AttachSnapId).HasColumnName("attach_snap_ID");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.QuestSnapshotId).HasColumnName("quest_snapshot_ID");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(d => d.QuestSnapshot).WithMany(p => p.QuestionAttachmentsSnapshots)
                .HasForeignKey(d => d.QuestSnapshotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Question___quest__09746778");
        });

        modelBuilder.Entity<QuestionsSnapshot>(entity =>
        {
            entity.HasKey(e => e.QuestSnapshotId).HasName("PK__Question__C51A6475B4B1E531");

            entity.ToTable("Questions_snapshot");

            entity.Property(e => e.QuestSnapshotId).HasColumnName("quest_snapshot_ID");
            entity.Property(e => e.OlympSnapId).HasColumnName("olymp_snap_ID");
            entity.Property(e => e.QuestIdOriginal).HasColumnName("quest_ID_original");
            entity.Property(e => e.QuestOrderSnapshot).HasColumnName("quest_order_snapshot");
            entity.Property(e => e.QuestionDescSnapshot)
                .HasColumnType("text")
                .HasColumnName("question_desc_snapshot");
            entity.Property(e => e.QuestionTypeSnapshot)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("question_type_snapshot");

            entity.HasOne(d => d.OlympSnap).WithMany(p => p.QuestionsSnapshots)
                .HasForeignKey(d => d.OlympSnapId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Questions__olymp__7C4F7684");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__760F9984F69944CA");

            entity.Property(e => e.RoleId).HasColumnName("role_ID");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<SubmissionItem>(entity =>
        {
            entity.HasKey(e => e.SubmissionItemsId).HasName("PK__Submissi__FACD4E7A46627801");

            entity.ToTable("Submission_items");

            entity.HasIndex(e => e.QuestSnapshotId, "IX_Submission_items_quest_snapshot_ID");

            entity.HasIndex(e => e.ResultsId, "IX_Submission_items_results_ID");

            entity.Property(e => e.SubmissionItemsId).HasColumnName("submission_items_ID");
            entity.Property(e => e.QuestSnapshotId).HasColumnName("quest_snapshot_ID");
            entity.Property(e => e.ResultsId).HasColumnName("results_ID");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("type");

            entity.HasOne(d => d.QuestSnapshot).WithMany(p => p.SubmissionItems)
                .HasForeignKey(d => d.QuestSnapshotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Submissio__quest__09A971A2");

            entity.HasOne(d => d.Results).WithMany(p => p.SubmissionItems)
                .HasForeignKey(d => d.ResultsId)
                .HasConstraintName("FK__Submissio__resul__08B54D69");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__B9BF330750B2F8FE");

            entity.ToTable("User");

            entity.HasIndex(e => e.Login, "UQ__User__7838F272A463D6DA").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActivated).HasColumnName("is_activated");
            entity.Property(e => e.LastLogin)
                .HasColumnType("datetime")
                .HasColumnName("last_login");
            entity.Property(e => e.Login)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("login");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("User_profiles");

            entity.HasIndex(e => e.Email, "IX_User_profiles_email");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_ID");
            entity.Property(e => e.City)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("city");
            entity.Property(e => e.ConsentFileUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("consent_file_URL");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("full_name");
            entity.Property(e => e.Kurator)
                .HasMaxLength(225)
                .IsUnicode(false)
                .HasColumnName("kurator");
            entity.Property(e => e.Organisation)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("organisation");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.PositionGrade)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("position_grade");

            entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .HasConstraintName("FK_User_profiles_User");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId }).HasName("PK__User_Rol__EEDFCA9F70402E69");

            entity.ToTable("User_Roles");

            entity.HasIndex(e => e.RoleId, "IX_User_Roles_role_ID");

            entity.HasIndex(e => e.UserId, "IX_User_Roles_user_ID");

            entity.Property(e => e.UserId).HasColumnName("user_ID");
            entity.Property(e => e.RoleId).HasColumnName("role_ID");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__User_Role__role___59063A47");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__User_Role__user___5812160E");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

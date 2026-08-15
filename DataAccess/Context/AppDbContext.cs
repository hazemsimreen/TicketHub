using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;

public class AppDbContext
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public new DbSet<Role> Roles => Set<Role>();
    public new DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<TicketPriority> TicketPriorities => Set<TicketPriority>();
    public DbSet<TicketStatus> TicketStatuses => Set<TicketStatus>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentProfile> AgentProfiles => Set<AgentProfile>();
    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationParticipant> ConversationParticipants =>
        Set<ConversationParticipant>();

    public DbSet<ConversationMessage> ConversationMessages =>
        Set<ConversationMessage>();

    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<TicketWatcher> TicketWatchers => Set<TicketWatcher>();

    public DbSet<TicketStatusHistory> TicketStatusHistories =>
        Set<TicketStatusHistory>();

    public DbSet<TicketFieldHistory> TicketFieldHistories =>
        Set<TicketFieldHistory>();

    public DbSet<WorkflowDefinition> WorkflowDefinitions =>
        Set<WorkflowDefinition>();

    public DbSet<WorkflowStep> WorkflowSteps =>
        Set<WorkflowStep>();

    public DbSet<WorkflowInstance> WorkflowInstances =>
        Set<WorkflowInstance>();

    public DbSet<WorkflowStepInstance> WorkflowStepInstances =>
        Set<WorkflowStepInstance>();

    public DbSet<TicketTransfer> TicketTransfers =>
        Set<TicketTransfer>();

    public DbSet<NotificationType> NotificationTypes =>
        Set<NotificationType>();

    public DbSet<Notification> Notifications =>
        Set<Notification>();

    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();

    // Role 4 — Collaboration, Files & Insight
    public DbSet<Rating> Ratings => Set<Rating>();


    // =========================================================
    // Automatic Audit Stamping + Soft Delete
    // =========================================================

    public override Task<int> SaveChangesAsync(
        CancellationToken ct = default)
    {
        ApplyAuditRules();

        return base.SaveChangesAsync(ct);
    }

    public override int SaveChanges()
    {
        ApplyAuditRules();

        return base.SaveChanges();
    }

    private void ApplyAuditRules()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:

                    entry.Entity.CreatedAt = now;
                    entry.Entity.IsDeleted = false;

                    break;


                case EntityState.Modified:

                    entry.Entity.UpdatedAt = now;

                    break;


                case EntityState.Deleted:

                    // Convert hard-delete → soft-delete
                    entry.State = EntityState.Modified;

                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;

                    break;
            }
        }
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // =========================================================
        // Identity User
        // =========================================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.HasOne(x => x.PrimaryDepartment)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.PrimaryDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // Department
        // =========================================================

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.HasOne(x => x.ParentDepartment)
                .WithMany(x => x.ChildDepartments)
                .HasForeignKey(x => x.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasData(
                new Department
                {
                    Id = 1,
                    Code = "ROADS",
                    Name = "Roads & Infrastructure",
                    ParentDepartmentId = null,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new Department
                {
                    Id = 2,
                    Code = "SANITATION",
                    Name = "Sanitation",
                    ParentDepartmentId = null,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new Department
                {
                    Id = 3,
                    Code = "LIGHTING",
                    Name = "Street Lighting",
                    ParentDepartmentId = null,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                }
            );
        });


        // =========================================================
        // Role
        // =========================================================

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.HasData(

                new Role
                {
                    Id = 1,
                    Code = "Admin",
                    IsDepartmentScoped = false,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new Role
                {
                    Id = 2,
                    Code = "DepartmentHead",
                    IsDepartmentScoped = true,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new Role
                {
                    Id = 3,
                    Code = "Employee",
                    IsDepartmentScoped = true,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new Role
                {
                    Id = 4,
                    Code = "Citizen",
                    IsDepartmentScoped = false,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                }

            );
        });


        // =========================================================
        // UserRole
        // =========================================================

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.UserId,
                x.RoleId,
                x.DepartmentId
            })
            .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Department)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // TicketPriority
        // =========================================================

        modelBuilder.Entity<TicketPriority>(entity =>
        {
            entity.ToTable("TicketPriorities");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.HasData(
                new TicketPriority
                {
                    Id = 1,
                    Code = "Low",
                    SortOrder = 1,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new TicketPriority
                {
                    Id = 2,
                    Code = "Medium",
                    SortOrder = 2,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new TicketPriority
                {
                    Id = 3,
                    Code = "High",
                    SortOrder = 3,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new TicketPriority
                {
                    Id = 4,
                    Code = "Urgent",
                    SortOrder = 4,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                }
            );
        });


        // =========================================================
        // TicketStatus
        // =========================================================

        modelBuilder.Entity<TicketStatus>(entity =>
        {
            entity.ToTable("TicketStatuses");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.HasData(
                new TicketStatus
                {
                    Id = 1,
                    Code = "Open",
                    IsTerminal = false,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new TicketStatus
                {
                    Id = 2,
                    Code = "InProgress",
                    IsTerminal = false,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new TicketStatus
                {
                    Id = 3,
                    Code = "OnHold",
                    IsTerminal = false,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new TicketStatus
                {
                    Id = 4,
                    Code = "Resolved",
                    IsTerminal = false,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new TicketStatus
                {
                    Id = 5,
                    Code = "Closed",
                    IsTerminal = true,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new TicketStatus
                {
                    Id = 6,
                    Code = "Cancelled",
                    IsTerminal = true,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                }
            );
        });


        // =========================================================
        // Category
        // =========================================================

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.HasOne(x => x.Department)
                .WithMany(x => x.Categories)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DefaultPriority)
                .WithMany(x => x.DefaultCategories)
                .HasForeignKey(x => x.DefaultPriorityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasData(
                new Category
                {
                    Id = 1,
                    Name = "Pothole",
                    DepartmentId = 1,
                    DefaultPriorityId = 3,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new Category
                {
                    Id = 2,
                    Name = "Broken Street Light",
                    DepartmentId = 3,
                    DefaultPriorityId = 2,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new Category
                {
                    Id = 3,
                    Name = "Uncollected Bins",
                    DepartmentId = 2,
                    DefaultPriorityId = 2,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new Category
                {
                    Id = 4,
                    Name = "Water Leak",
                    DepartmentId = 1,
                    DefaultPriorityId = 4,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                }
            );
        });


        // =========================================================
        // Ticket
        // =========================================================

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.TicketNumber)
                .IsUnique();

            entity.HasQueryFilter(x => !x.IsDeleted);

            entity.HasOne(x => x.SubmittedByUser)
                .WithMany(x => x.SubmittedTickets)
                .HasForeignKey(x => x.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Tickets)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Department)
                .WithMany(x => x.Tickets)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Priority)
                .WithMany(x => x.Tickets)
                .HasForeignKey(x => x.PriorityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Status)
                .WithMany(x => x.Tickets)
                .HasForeignKey(x => x.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AssignedToUser)
                .WithMany(x => x.AssignedTickets)
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // Conversation
        // =========================================================

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("Conversations");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.TicketId)
                .IsUnique();

            entity.HasOne(x => x.Ticket)
                .WithOne(x => x.Conversation)
                .HasForeignKey<Conversation>(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // ConversationParticipant
        // =========================================================

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.ToTable("ConversationParticipants");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.ConversationId,
                x.UserId
            })
            .IsUnique();

            entity.HasOne(x => x.Conversation)
                .WithMany(x => x.Participants)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.User)
                .WithMany(x => x.ConversationParticipants)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // ConversationMessage
        // =========================================================

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.ToTable("ConversationMessages");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SenderUser)
                .WithMany(x => x.SentMessages)
                .HasForeignKey(x => x.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // TicketComment
        // =========================================================

        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.ToTable("TicketComments");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Ticket)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AuthorUser)
                .WithMany(x => x.TicketComments)
                .HasForeignKey(x => x.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.StepInstance)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.StepInstanceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ParentComment)
                .WithMany(x => x.Replies)
                .HasForeignKey(x => x.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // Attachment
        // =========================================================

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.ToTable("Attachments");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Ticket)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Comment)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.CommentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Message)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.Restrict);

            // Role 4 addition
            entity.HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // TicketWatcher
        // =========================================================

        modelBuilder.Entity<TicketWatcher>(entity =>
        {
            entity.ToTable("TicketWatchers");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.TicketId,
                x.UserId
            })
            .IsUnique();

            entity.HasOne(x => x.Ticket)
                .WithMany(x => x.Watchers)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.User)
                .WithMany(x => x.WatchedTickets)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // TicketStatusHistory
        // =========================================================

        modelBuilder.Entity<TicketStatusHistory>(entity =>
        {
            entity.ToTable("TicketStatusHistory");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Ticket)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.FromStatus)
                .WithMany(x => x.FromStatusHistories)
                .HasForeignKey(x => x.FromStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ToStatus)
                .WithMany(x => x.ToStatusHistories)
                .HasForeignKey(x => x.ToStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ChangedByUser)
                .WithMany(x => x.StatusChanges)
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // TicketFieldHistory
        // =========================================================

        modelBuilder.Entity<TicketFieldHistory>(entity =>
        {
            entity.ToTable("TicketFieldHistory");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Ticket)
                .WithMany(x => x.FieldHistory)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ChangedByUser)
                .WithMany(x => x.FieldChanges)
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // WorkflowDefinition
        // =========================================================

        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.ToTable("WorkflowDefinitions");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Department)
                .WithMany(x => x.WorkflowDefinitions)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.WorkflowDefinitions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // Seed Data
            //
            // قالب افتراضي واحد (IsDefault = true) لكل قسم موجود
            // حالياً (Roads / Sanitation / Lighting)، بدون فئة محددة
            // (CategoryId = null) — أي ينطبق على كل تذاكر القسم
            // بغض النظر عن الفئة.
            // =====================================================

            entity.HasData(
                new WorkflowDefinition
                {
                    Id = Guid.Parse("11111111-0000-0000-0000-000000000001"),
                    Name = "Default Workflow - Roads & Infrastructure",
                    DepartmentId = 1,
                    CategoryId = null,
                    Version = 1,
                    IsDefault = true,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new WorkflowDefinition
                {
                    Id = Guid.Parse("11111111-0000-0000-0000-000000000002"),
                    Name = "Default Workflow - Sanitation",
                    DepartmentId = 2,
                    CategoryId = null,
                    Version = 1,
                    IsDefault = true,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new WorkflowDefinition
                {
                    Id = Guid.Parse("11111111-0000-0000-0000-000000000003"),
                    Name = "Default Workflow - Street Lighting",
                    DepartmentId = 3,
                    CategoryId = null,
                    Version = 1,
                    IsDefault = true,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                }
            );
        });


        // =========================================================
        // WorkflowStep
        // =========================================================

        modelBuilder.Entity<WorkflowStep>(entity =>
        {
            entity.ToTable("WorkflowSteps");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.WorkflowDefinition)
                .WithMany(x => x.Steps)
                .HasForeignKey(x => x.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.WorkflowSteps)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AssignedUser)
                .WithMany(x => x.AssignedWorkflowSteps)
                .HasForeignKey(x => x.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // Seed Data
            //
            // خطوتين لكل قالب افتراضي:
            //   1) DepartmentHead review  (RoleId = 2)
            //   2) Employee execution     (RoleId = 3)
            //
            // نفس نمط التصعيد المستخدم فعلياً بـ
            // TicketService.AssignTicketAsync (موظف من نفس القسم).
            // =====================================================

            entity.HasData(

                // ---------------------------------------------------
                // Roads & Infrastructure workflow steps
                // ---------------------------------------------------

                new WorkflowStep
                {
                    Id = Guid.Parse("21111111-0000-0000-0000-000000000001"),
                    WorkflowDefinitionId = Guid.Parse("11111111-0000-0000-0000-000000000001"),
                    StepOrder = 1,
                    RoleId = 2, // DepartmentHead
                    AssignedUserId = null,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new WorkflowStep
                {
                    Id = Guid.Parse("21111111-0000-0000-0000-000000000002"),
                    WorkflowDefinitionId = Guid.Parse("11111111-0000-0000-0000-000000000001"),
                    StepOrder = 2,
                    RoleId = 3, // Employee
                    AssignedUserId = null,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                // ---------------------------------------------------
                // Sanitation workflow steps
                // ---------------------------------------------------

                new WorkflowStep
                {
                    Id = Guid.Parse("22222222-0000-0000-0000-000000000001"),
                    WorkflowDefinitionId = Guid.Parse("11111111-0000-0000-0000-000000000002"),
                    StepOrder = 1,
                    RoleId = 2, // DepartmentHead
                    AssignedUserId = null,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new WorkflowStep
                {
                    Id = Guid.Parse("22222222-0000-0000-0000-000000000002"),
                    WorkflowDefinitionId = Guid.Parse("11111111-0000-0000-0000-000000000002"),
                    StepOrder = 2,
                    RoleId = 3, // Employee
                    AssignedUserId = null,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                // ---------------------------------------------------
                // Street Lighting workflow steps
                // ---------------------------------------------------

                new WorkflowStep
                {
                    Id = Guid.Parse("23333333-0000-0000-0000-000000000001"),
                    WorkflowDefinitionId = Guid.Parse("11111111-0000-0000-0000-000000000003"),
                    StepOrder = 1,
                    RoleId = 2, // DepartmentHead
                    AssignedUserId = null,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                },

                new WorkflowStep
                {
                    Id = Guid.Parse("23333333-0000-0000-0000-000000000002"),
                    WorkflowDefinitionId = Guid.Parse("11111111-0000-0000-0000-000000000003"),
                    StepOrder = 2,
                    RoleId = 3, // Employee
                    AssignedUserId = null,
                    CreatedAt = new DateTime(
                        2026, 1, 1, 0, 0, 0,
                        DateTimeKind.Utc),
                    IsDeleted = false
                }
            );
        });


        // =========================================================
        // WorkflowInstance
        // =========================================================

        modelBuilder.Entity<WorkflowInstance>(entity =>
        {
            entity.ToTable("WorkflowInstances");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Ticket)
                .WithMany(x => x.WorkflowInstances)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkflowDefinition)
                .WithMany(x => x.Instances)
                .HasForeignKey(x => x.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // WorkflowStepInstance
        // =========================================================

        modelBuilder.Entity<WorkflowStepInstance>(entity =>
        {
            entity.ToTable("WorkflowStepInstances");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.WorkflowInstance)
                .WithMany(x => x.StepInstances)
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkflowStep)
                .WithMany(x => x.StepInstances)
                .HasForeignKey(x => x.WorkflowStepId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AssignedToUser)
                .WithMany(x => x.AssignedStepInstances)
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // TicketTransfer
        // =========================================================

        modelBuilder.Entity<TicketTransfer>(entity =>
        {
            entity.ToTable("TicketTransfers");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Ticket)
                .WithMany(x => x.Transfers)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.OriginStepInstance)
                .WithMany(x => x.OriginTransfers)
                .HasForeignKey(x => x.OriginStepInstanceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.FromUser)
                .WithMany(x => x.SentTransfers)
                .HasForeignKey(x => x.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ToUser)
                .WithMany(x => x.ReceivedTransfers)
                .HasForeignKey(x => x.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ToDepartment)
                .WithMany(x => x.ReceivedTransfers)
                .HasForeignKey(x => x.ToDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // NotificationType
        // =========================================================

        modelBuilder.Entity<NotificationType>(entity =>
        {
            entity.ToTable("NotificationTypes");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Code)
                .IsUnique();


            entity.HasData(new NotificationType
            {
                Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                Code = "TicketStatusChanged",
                TitleTemplate = "Your ticket {TicketNumber} status changed to {NewStatus}",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            },

                new NotificationType
                {
                    Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
                    Code = "TicketAssigned",
                    TitleTemplate = "Your ticket {TicketNumber} was assigned to {AgentName}",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                },

                new NotificationType
                {
                    Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"),
                    Code = "TicketCommentAdded",
                    TitleTemplate = "A new comment was added to your ticket {TicketNumber}",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                },

                new NotificationType
                {
                    Id = Guid.Parse("a4444444-4444-4444-4444-444444444444"),
                    Code = "TicketTransferred",
                    TitleTemplate = "Your ticket {TicketNumber} was transferred to {Department}",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                });
        });


        // =========================================================
        // Notification
        // =========================================================

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.RecipientUser)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.NotificationType)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.NotificationTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Ticket)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // RefreshTokens
        // =========================================================

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.TokenHash)
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // =========================================================
        // Rating (Role 4)
        // =========================================================

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.ToTable("Ratings", t => t.HasCheckConstraint("CK_Rating_Stars_Range", "[Stars] BETWEEN 1 AND 5"));

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.TicketId)
                .IsUnique();

            entity.Property(x => x.Comment)
                .HasMaxLength(1000);

            entity.HasOne(x => x.Ticket)
                .WithMany()
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.RatedByUser)
                .WithMany()
                .HasForeignKey(x => x.RatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.ApplyOrganisationStaffCore();
    }

}


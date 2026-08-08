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
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<TicketPriority> TicketPriorities => Set<TicketPriority>();
    public DbSet<TicketStatus> TicketStatuses => Set<TicketStatus>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

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
                    Id = Guid.Parse("d0000000-0000-0000-0000-000000000001"),
                    Code = "IT",
                    Name = "IT Support",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
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
                new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Code = "Citizen", IsDepartmentScoped = false, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Code = "Agent", IsDepartmentScoped = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Code = "Admin", IsDepartmentScoped = false, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
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
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"),
                    Code = "Normal",
                    SortOrder = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
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
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000003"),
                    Code = "Open",
                    IsTerminal = false,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
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
                    Id = Guid.Parse("c0000000-0000-0000-0000-000000000001"),
                    Name = "General",
                    DepartmentId = Guid.Parse("d0000000-0000-0000-0000-000000000001"),
                    DefaultPriorityId = Guid.Parse("a0000000-0000-0000-0000-000000000002"),
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
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
    }
}
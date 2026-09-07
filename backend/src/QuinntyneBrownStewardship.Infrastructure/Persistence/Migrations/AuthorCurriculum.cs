using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuinntyneBrownStewardship.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(StewardshipDbContext))]
    [Migration("20260907155252_AuthorCurriculum")]
    public sealed class AuthorCurriculum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Authority is held on the account, so an operator can confer it without a screen that needs it.
            migrationBuilder.AddColumn<bool>(
                name: "IsAdministrator",
                table: "Participants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // The programme each key stood for becomes a record of its own.
            migrationBuilder.CreateTable(
                name: "Curricula",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    State = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curricula", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    // An audit outlives the content it records, so it carries no foreign key to it.
                    table.PrimaryKey("PK_CurriculumAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Curricula_Key",
                table: "Curricula",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumAudits_TargetId_At",
                table: "CurriculumAudits",
                columns: new[] { "TargetId", "At" });

            // One programme per key any module or cohort names. A key with modules behind it was being
            // read by whoever followed it, so it becomes published; a key only a cohort names has nothing
            // to read and stays draft, and that cohort is told its programme is not yet available.
            migrationBuilder.Sql(@"
                INSERT INTO [Curricula] ([Id], [Key], [Title], [State], [CreatedAt], [PublishedAt])
                SELECT NEWID(), k.[Key], k.[Key],
                       CASE WHEN EXISTS (SELECT 1 FROM [Modules] m WHERE m.[CurriculumKey] = k.[Key]) THEN N'Published' ELSE N'Draft' END,
                       SYSDATETIMEOFFSET(),
                       CASE WHEN EXISTS (SELECT 1 FROM [Modules] m WHERE m.[CurriculumKey] = k.[Key]) THEN SYSDATETIMEOFFSET() ELSE NULL END
                FROM (SELECT [CurriculumKey] AS [Key] FROM [Modules] UNION SELECT [CurriculumKey] FROM [Cohorts]) k;");

            // Expand, populate, then contract. Each column arrives nullable and is filled from the
            // programme its key resolves to before it refuses null, so no default outlives this migration.
            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumId",
                table: "Modules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Modules",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Revision",
                table: "Modules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Revision",
                table: "Sections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumId",
                table: "Cohorts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationWeeks",
                table: "Cohorts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionCadenceWeeks",
                table: "Cohorts",
                type: "int",
                nullable: true);

            // A module takes the state of the programme it belongs to, so a cohort reading it keeps reading it.
            // Twelve weeks at a session every two were the constants of the previous release, and are what
            // every existing cohort was actually running.
            migrationBuilder.Sql(@"
                UPDATE m SET m.[CurriculumId] = c.[Id] FROM [Modules] m JOIN [Curricula] c ON c.[Key] = m.[CurriculumKey];
                UPDATE ch SET ch.[CurriculumId] = c.[Id] FROM [Cohorts] ch JOIN [Curricula] c ON c.[Key] = ch.[CurriculumKey];
                UPDATE m SET m.[State] = c.[State] FROM [Modules] m JOIN [Curricula] c ON c.[Id] = m.[CurriculumId];
                UPDATE [Modules] SET [Revision] = NEWID();
                UPDATE [Sections] SET [Revision] = NEWID();
                UPDATE [Cohorts] SET [DurationWeeks] = 12, [SessionCadenceWeeks] = 2;");

            migrationBuilder.AlterColumn<Guid>(
                name: "CurriculumId",
                table: "Modules",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Modules",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Revision",
                table: "Modules",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Revision",
                table: "Sections",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CurriculumId",
                table: "Cohorts",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DurationWeeks",
                table: "Cohorts",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SessionCadenceWeeks",
                table: "Cohorts",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // The previous release gave the cohort key a default of 'starter'. That constraint holds the
            // column in place, so it goes before the column does.
            migrationBuilder.Sql(@"
                DECLARE @constraint sysname = (SELECT d.[name] FROM sys.default_constraints d
                    JOIN sys.columns c ON c.[object_id] = d.[parent_object_id] AND c.[column_id] = d.[parent_column_id]
                    WHERE d.[parent_object_id] = OBJECT_ID(N'[Cohorts]') AND c.[name] = N'CurriculumKey');
                IF @constraint IS NOT NULL EXEC(N'ALTER TABLE [Cohorts] DROP CONSTRAINT [' + @constraint + N']');");

            migrationBuilder.DropIndex(
                name: "IX_Modules_CurriculumKey_Ordinal",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "CurriculumKey",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "CurriculumKey",
                table: "Cohorts");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_CurriculumId_Ordinal",
                table: "Modules",
                columns: new[] { "CurriculumId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cohorts_CurriculumId",
                table: "Cohorts",
                column: "CurriculumId");

            migrationBuilder.AddForeignKey(
                name: "FK_Modules_Curricula_CurriculumId",
                table: "Modules",
                column: "CurriculumId",
                principalTable: "Curricula",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cohorts_Curricula_CurriculumId",
                table: "Cohorts",
                column: "CurriculumId",
                principalTable: "Curricula",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Modules_Curricula_CurriculumId",
                table: "Modules");

            migrationBuilder.DropForeignKey(
                name: "FK_Cohorts_Curricula_CurriculumId",
                table: "Cohorts");

            migrationBuilder.DropIndex(
                name: "IX_Modules_CurriculumId_Ordinal",
                table: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Cohorts_CurriculumId",
                table: "Cohorts");

            // The key each programme carries returns to the rows that followed it, by the same expansion
            // and contraction in reverse.
            migrationBuilder.AddColumn<string>(
                name: "CurriculumKey",
                table: "Modules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurriculumKey",
                table: "Cohorts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE m SET m.[CurriculumKey] = c.[Key] FROM [Modules] m JOIN [Curricula] c ON c.[Id] = m.[CurriculumId];
                UPDATE ch SET ch.[CurriculumKey] = c.[Key] FROM [Cohorts] ch JOIN [Curricula] c ON c.[Id] = ch.[CurriculumId];");

            migrationBuilder.AlterColumn<string>(
                name: "CurriculumKey",
                table: "Modules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CurriculumKey",
                table: "Cohorts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "starter",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_CurriculumKey_Ordinal",
                table: "Modules",
                columns: new[] { "CurriculumKey", "Ordinal" },
                unique: true);

            migrationBuilder.DropColumn(
                name: "CurriculumId",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "CurriculumId",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "DurationWeeks",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "SessionCadenceWeeks",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "IsAdministrator",
                table: "Participants");

            migrationBuilder.DropTable(
                name: "Curricula");

            migrationBuilder.DropTable(
                name: "CurriculumAudits");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Access.Participant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("nvarchar(254)");

                    b.Property<string>("EmailAddress")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("nvarchar(254)");

                    b.Property<bool>("IsAdministrator")
                        .HasColumnType("bit");

                    b.Property<bool>("IsMentor")
                        .HasColumnType("bit");

                    b.Property<string>("NormalizedEmail")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("nvarchar(254)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .IsUnique();

                    b.ToTable("Participants");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Access.ParticipantSession", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("LastActivityAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("ParticipantId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset?>("RevokedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.HasKey("Id");

                    b.HasIndex("ParticipantId");

                    b.HasIndex("TokenHash")
                        .IsUnique();

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Access.SignInAttempt", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTimeOffset>("At")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("NormalizedEmail")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("nvarchar(254)");

                    b.Property<string>("Origin")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<bool>("Refused")
                        .HasColumnType("bit");

                    b.Property<bool>("Succeeded")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail", "At");

                    b.HasIndex("Origin", "At");

                    b.ToTable("SignInAttempts");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Enrollment.Cohort", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CurriculumId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("DurationWeeks")
                        .HasColumnType("int");

                    b.Property<Guid?>("MentorId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("MentorName")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("nvarchar(254)");

                    b.Property<int>("SessionCadenceWeeks")
                        .HasColumnType("int");

                    b.Property<DateOnly>("StartDate")
                        .HasColumnType("date");

                    b.Property<string>("TimeZone")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.HasIndex("CurriculumId");

                    b.HasIndex("MentorId");

                    b.ToTable("Cohorts");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Enrollment.Enrollment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CohortId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<Guid>("ParticipantId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("CohortId");

                    b.HasIndex("ParticipantId")
                        .IsUnique()
                        .HasFilter("[IsActive] = 1");

                    b.ToTable("Enrollments");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.Curriculum", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTimeOffset?>("PublishedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("nvarchar(16)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("nvarchar(254)");

                    b.HasKey("Id");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("Curricula");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.CurriculumAudit", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<Guid>("ActorId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("At")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("CorrelationId")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<Guid>("TargetId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("TargetId", "At");

                    b.ToTable("CurriculumAudits");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.CurriculumModule", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("CurriculumId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("EffortEstimate")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Ordinal")
                        .HasColumnType("int");

                    b.PrimitiveCollection<string>("PracticeSteps")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("Revision")
                        .IsConcurrencyToken()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("nvarchar(16)");

                    b.Property<string>("Summary")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("nvarchar(254)");

                    b.HasKey("Id");

                    b.HasIndex("CurriculumId", "Ordinal")
                        .IsUnique();

                    b.ToTable("Modules");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.ModuleSection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("ModuleId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Ordinal")
                        .HasColumnType("int");

                    b.Property<string>("Reading")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("Revision")
                        .IsConcurrencyToken()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ModuleId", "Ordinal")
                        .IsUnique();

                    b.ToTable("Sections");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.PreparationPrompt", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ModuleId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Ordinal")
                        .HasColumnType("int");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ModuleId", "Ordinal")
                        .IsUnique();

                    b.ToTable("Prompts");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.SectionCompletion", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("CompletedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("EnrollmentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("SectionId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("SectionId");

                    b.HasIndex("EnrollmentId", "SectionId")
                        .IsUnique();

                    b.ToTable("Completions");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Notes.Note", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("EnrollmentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ModuleId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("PromptId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("RevisedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("Revision")
                        .IsConcurrencyToken()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("SessionId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ModuleId");

                    b.HasIndex("PromptId");

                    b.HasIndex("SessionId");

                    b.HasIndex("EnrollmentId", "PromptId")
                        .IsUnique()
                        .HasFilter("[PromptId] IS NOT NULL");

                    b.HasIndex("EnrollmentId", "RevisedAt");

                    b.ToTable("Notes", t =>
                        {
                            t.HasCheckConstraint("CK_Note_Attachment", "([ModuleId] IS NOT NULL AND [SessionId] IS NULL) OR ([ModuleId] IS NULL AND [SessionId] IS NOT NULL)");
                        });
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Scheduling.AvailabilitySlot", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("DurationMinutes")
                        .HasColumnType("int");

                    b.Property<Guid>("MentorId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("StartsAt")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.HasIndex("MentorId", "StartsAt")
                        .IsUnique();

                    b.ToTable("Availability");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Scheduling.Booking", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset?>("CancelledAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("EnrollmentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("SlotId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("EnrollmentId");

                    b.HasIndex("SlotId")
                        .IsUnique()
                        .HasFilter("[CancelledAt] IS NULL");

                    b.ToTable("Bookings");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Scheduling.BookingAudit", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<Guid>("ActorId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("At")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("BookingId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CorrelationId")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<Guid?>("PreviousSlotId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("SlotId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("BookingId", "At");

                    b.ToTable("BookingAudits");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Access.ParticipantSession", b =>
                {
                    b.HasOne("QuinntyneBrownStewardship.Domain.Access.Participant", null)
                        .WithMany()
                        .HasForeignKey("ParticipantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Enrollment.Cohort", b =>
                {
                    b.HasOne("QuinntyneBrownStewardship.Domain.Learning.Curriculum", "Curriculum")
                        .WithMany()
                        .HasForeignKey("CurriculumId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("QuinntyneBrownStewardship.Domain.Access.Participant", null)
                        .WithMany()
                        .HasForeignKey("MentorId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Curriculum");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Enrollment.Enrollment", b =>
                {
                    b.HasOne("QuinntyneBrownStewardship.Domain.Enrollment.Cohort", "Cohort")
                        .WithMany()
                        .HasForeignKey("CohortId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("QuinntyneBrownStewardship.Domain.Access.Participant", null)
                        .WithMany()
                        .HasForeignKey("ParticipantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Cohort");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.CurriculumModule", b =>
                {
                    b.HasOne("QuinntyneBrownStewardship.Domain.Learning.Curriculum", null)
                        .WithMany("Modules")
                        .HasForeignKey("CurriculumId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.ModuleSection", b =>
                {
                    b.HasOne("QuinntyneBrownStewardship.Domain.Learning.CurriculumModule", null)
                        .WithMany("Sections")
                        .HasForeignKey("ModuleId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.PreparationPrompt", b =>
                {
                    b.HasOne("QuinntyneBrownStewardship.Domain.Learning.CurriculumModule", null)
                        .WithMany("PreparationPrompts")
                        .HasForeignKey("ModuleId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.SectionCompletion", b =>
                {
                    b.HasOne("QuinntyneBrownStewardship.Domain.Enrollment.Enrollment", null)
                        .WithMany()
                        .HasForeignKey("EnrollmentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("QuinntyneBrownStewardship.Domain.Learning.ModuleSection", null)
                        .WithMany()
                        .HasForeignKey("SectionId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Notes.Note", b =>
                {
                    b.HasOne("QuinntyneBrownStewardship.Domain.Enrollment.Enrollment", null)
                        .WithMany()
                        .HasForeignKey("EnrollmentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("QuinntyneBrownStewardship.Domain.Learning.CurriculumModule", null)
                        .WithMany()
                        .HasForeignKey("ModuleId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("QuinntyneBrownStewardship.Domain.Learning.PreparationPrompt", null)
                        .WithMany()
                        .HasForeignKey("PromptId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("QuinntyneBrownStewardship.Domain.Scheduling.Booking", null)
                        .WithMany()
                        .HasForeignKey("SessionId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Scheduling.AvailabilitySlot", b =>
                {
                    b.HasOne("QuinntyneBrownStewardship.Domain.Access.Participant", null)
                        .WithMany()
                        .HasForeignKey("MentorId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Scheduling.Booking", b =>
                {
                    b.HasOne("QuinntyneBrownStewardship.Domain.Enrollment.Enrollment", null)
                        .WithMany()
                        .HasForeignKey("EnrollmentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("QuinntyneBrownStewardship.Domain.Scheduling.AvailabilitySlot", "Slot")
                        .WithMany()
                        .HasForeignKey("SlotId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Slot");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Scheduling.BookingAudit", b =>
                {
                    b.HasOne("QuinntyneBrownStewardship.Domain.Scheduling.Booking", null)
                        .WithMany()
                        .HasForeignKey("BookingId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.Curriculum", b =>
                {
                    b.Navigation("Modules");
                });

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.CurriculumModule", b =>
                {
                    b.Navigation("PreparationPrompts");

                    b.Navigation("Sections");
                });
#pragma warning restore 612, 618
        }
    }
}

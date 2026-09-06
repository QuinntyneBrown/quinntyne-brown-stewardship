using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuinntyneBrownStewardship.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(StewardshipDbContext))]
    [Migration("20260906133421_CompleteProgramme")]
    public sealed class CompleteProgramme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Participants",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsMentor",
                table: "Participants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CurriculumKey",
                table: "Cohorts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "starter");

            migrationBuilder.AddColumn<Guid>(
                name: "MentorId",
                table: "Cohorts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Cohorts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "America/Toronto");

            migrationBuilder.CreateTable(
                name: "Availability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MentorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Availability", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Availability_Participants_MentorId",
                        column: x => x.MentorId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffortEstimate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PracticeSteps = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Availability_SlotId",
                        column: x => x.SlotId,
                        principalTable: "Availability",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Prompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prompts_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reading = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sections_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingAudits_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PromptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevisedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.CheckConstraint("CK_Note_Attachment", "([ModuleId] IS NOT NULL AND [SessionId] IS NULL) OR ([ModuleId] IS NULL AND [SessionId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Notes_Bookings_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notes_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notes_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notes_Prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "Prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Completions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Completions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Completions_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Completions_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cohorts_MentorId",
                table: "Cohorts",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_Availability_MentorId_StartsAt",
                table: "Availability",
                columns: new[] { "MentorId", "StartsAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingAudits_BookingId_At",
                table: "BookingAudits",
                columns: new[] { "BookingId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_EnrollmentId",
                table: "Bookings",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SlotId",
                table: "Bookings",
                column: "SlotId",
                unique: true,
                filter: "[CancelledAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Completions_EnrollmentId_SectionId",
                table: "Completions",
                columns: new[] { "EnrollmentId", "SectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Completions_SectionId",
                table: "Completions",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_CurriculumKey_Ordinal",
                table: "Modules",
                columns: new[] { "CurriculumKey", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notes_EnrollmentId_PromptId",
                table: "Notes",
                columns: new[] { "EnrollmentId", "PromptId" },
                unique: true,
                filter: "[PromptId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_EnrollmentId_RevisedAt",
                table: "Notes",
                columns: new[] { "EnrollmentId", "RevisedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_ModuleId",
                table: "Notes",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_PromptId",
                table: "Notes",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_SessionId",
                table: "Notes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_ModuleId_Ordinal",
                table: "Prompts",
                columns: new[] { "ModuleId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sections_ModuleId_Ordinal",
                table: "Sections",
                columns: new[] { "ModuleId", "Ordinal" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cohorts_Participants_MentorId",
                table: "Cohorts",
                column: "MentorId",
                principalTable: "Participants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cohorts_Participants_MentorId",
                table: "Cohorts");

            migrationBuilder.DropTable(
                name: "BookingAudits");

            migrationBuilder.DropTable(
                name: "Completions");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Prompts");

            migrationBuilder.DropTable(
                name: "Availability");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Cohorts_MentorId",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Participants");

            migrationBuilder.DropColumn(
                name: "IsMentor",
                table: "Participants");

            migrationBuilder.DropColumn(
                name: "CurriculumKey",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "MentorId",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Cohorts");
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

                    b.Property<string>("CurriculumKey")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<Guid?>("MentorId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("MentorName")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("nvarchar(254)");

                    b.Property<DateOnly>("StartDate")
                        .HasColumnType("date");

                    b.Property<string>("TimeZone")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

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

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.CurriculumModule", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CurriculumKey")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("EffortEstimate")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Ordinal")
                        .HasColumnType("int");

                    b.PrimitiveCollection<string>("PracticeSteps")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Summary")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(254)
                        .HasColumnType("nvarchar(254)");

                    b.HasKey("Id");

                    b.HasIndex("CurriculumKey", "Ordinal")
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
                    b.HasOne("QuinntyneBrownStewardship.Domain.Access.Participant", null)
                        .WithMany()
                        .HasForeignKey("MentorId")
                        .OnDelete(DeleteBehavior.Restrict);
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

            modelBuilder.Entity("QuinntyneBrownStewardship.Domain.Learning.CurriculumModule", b =>
                {
                    b.Navigation("PreparationPrompts");

                    b.Navigation("Sections");
                });
#pragma warning restore 612, 618
        }
    }
}

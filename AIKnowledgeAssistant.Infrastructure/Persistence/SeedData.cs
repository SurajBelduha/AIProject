using AIKnowledgeAssistant.Domain.Entities;
using AIKnowledgeAssistant.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeAssistant.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.Organizations.AnyAsync())
        {
            return; // Already seeded
        }

        // ==========================================
        // 1. ORGANIZATION 1: Apex University
        // ==========================================
        var orgApex = new Organization
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Apex University",
            Slug = "apex-university",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        context.Organizations.Add(orgApex);

        var userApex = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-222222222222"),
            OrganizationId = orgApex.Id,
            Email = "admin@apex.edu",
            FullName = "Dean of Academic Affairs",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ApexAdmin2026!"),
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        context.Users.Add(userApex);

        var assistantApex = new Assistant
        {
            Id = Guid.Parse("11111111-1111-1111-1111-333333333333"),
            OrganizationId = orgApex.Id,
            Token = "ast_live_apex_univ_8f93e",
            Name = "Apex Academic & Scholarship Assistant",
            Description = "Automated assistant providing verified guidance on scholarships, academic standing, and university regulations.",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        context.Assistants.Add(assistantApex);

        // Doc 1: Scholarship and Honors Rules
        var docApex1 = new Document
        {
            Id = Guid.Parse("11111111-1111-1111-1111-444444444441"),
            OrganizationId = orgApex.Id,
            FileName = "Apex_University_Scholarship_and_Honors_Rules.pdf",
            OriginalFileName = "Scholarship_and_Honors_Rules.pdf",
            FileSizeBytes = 428000,
            PageCount = 5,
            ContentType = "application/pdf",
            StoragePath = "database_indexed",
            UploadedAt = DateTime.UtcNow.AddDays(-20)
        };
        context.Documents.Add(docApex1);

        var apexChunks1 = new List<DocumentChunk>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docApex1.Id,
                OrganizationId = orgApex.Id,
                ChunkIndex = 0,
                PageNumber = 1,
                Content = "APEX UNIVERSITY POLICY MANUAL - SECTION 1.0: GENERAL SCHOLARSHIP ELIGIBILITY. Apex University awards annual undergraduate merit scholarships to recognize exceptional academic achievement. To qualify for consideration, all applicants must maintain full-time enrollment status (minimum 12 credit hours per semester) and have an active student account in good standing. The baseline GPA requirement across all university-wide merit programs is a minimum cumulative GPA of 3.50 on a 4.00 unweighted grading scale."
            },
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docApex1.Id,
                OrganizationId = orgApex.Id,
                ChunkIndex = 1,
                PageNumber = 2,
                Content = "APEX UNIVERSITY SCHOLARSHIP RULES - SECTION 2.1: ACADEMIC STANDING & CLASS YEAR REQUIREMENTS. University Merit Scholarships are reserved for continuing students who have demonstrated sustained collegiate academic performance. Applicants must be of Sophomore standing (Year 2 or above), having completed a minimum of 30 earned semester credits at Apex University. First-year students (Freshmen) are not eligible for general University Merit Scholarships during their initial academic year, but may qualify for separate High School Entrance Grants."
            },
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docApex1.Id,
                OrganizationId = orgApex.Id,
                ChunkIndex = 2,
                PageNumber = 3,
                Content = "APEX UNIVERSITY SCHOLARSHIP TIERS - SECTION 3.0: AWARD TIERS & DEAN'S LIST. Tier A Merit Honors: Students possessing a cumulative GPA of 3.85 or higher receive an annual stipend of $8,000. Tier B Merit Honors: Students possessing a cumulative GPA between 3.50 and 3.84 receive an annual stipend of $5,000. Dean's Honors List Recognition: Awarded each semester to undergraduate students completing 14 or more graded credit units with a term GPA of 3.75 or higher and zero grades of Incomplete."
            },
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docApex1.Id,
                OrganizationId = orgApex.Id,
                ChunkIndex = 3,
                PageNumber = 4,
                Content = "APEX UNIVERSITY SCHOLARSHIP RULES - SECTION 4.2: STACKING RESTRICTIONS & ATHLETIC EXCLUSIONS. University Merit Scholarships CANNOT be combined, stacked, or held concurrently with any active Athletic, Varsity, or Sports Grant stipends. In accordance with university athletic regulatory bylaws, receiving athletic grant funds automatically disqualifies the student from receiving concurrent merit awards. If a student qualifies for both an athletic stipend and a merit scholarship, the student must select one single funding award in writing prior to the registration census date."
            },
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docApex1.Id,
                OrganizationId = orgApex.Id,
                ChunkIndex = 4,
                PageNumber = 5,
                Content = "APEX UNIVERSITY SCHOLARSHIP RULES - SECTION 5.0: ANNUAL RENEWAL PROCESS. Merit scholarships are renewable for up to three consecutive academic years (six semesters). To maintain eligibility for automatic renewal, the recipient must maintain a minimum cumulative GPA of 3.40 throughout the academic year and submit the Annual Scholarship Verification Petition to the Financial Aid Office no later than May 15."
            }
        };
        context.DocumentChunks.AddRange(apexChunks1);

        // Doc 2: Academic Integrity and Grading
        var docApex2 = new Document
        {
            Id = Guid.Parse("11111111-1111-1111-1111-444444444442"),
            OrganizationId = orgApex.Id,
            FileName = "Apex_University_Academic_Integrity_and_Grading.pdf",
            OriginalFileName = "Academic_Integrity_and_Grading.pdf",
            FileSizeBytes = 312000,
            PageCount = 3,
            ContentType = "application/pdf",
            StoragePath = "database_indexed",
            UploadedAt = DateTime.UtcNow.AddDays(-18)
        };
        context.Documents.Add(docApex2);

        var apexChunks2 = new List<DocumentChunk>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docApex2.Id,
                OrganizationId = orgApex.Id,
                ChunkIndex = 0,
                PageNumber = 1,
                Content = "APEX UNIVERSITY ACADEMIC POLICIES - SECTION 1.0: GRADING SCALE AND DEFINITIONS. Course letter grades carry the following numerical grade point equivalents: A = 4.0, A- = 3.7, B+ = 3.3, B = 3.0, B- = 2.7, C+ = 2.3, C = 2.0, D = 1.0, F = 0.0. An Incomplete (I) grade must be resolved by the end of the 8th week of the subsequent term; otherwise, the registrar converts it automatically to an F."
            },
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docApex2.Id,
                OrganizationId = orgApex.Id,
                ChunkIndex = 1,
                PageNumber = 2,
                Content = "APEX UNIVERSITY GRADING POLICY - SECTION 3.4: FORMAL GRADE APPEAL PROCEDURE. A student who believes a final semester grade was calculated erroneously or arbitrarily must first confer informally with the course instructor within 7 calendar days of grade publication. If unresolved, the student must submit a Formal Written Grade Appeal to the Department Chair within 14 calendar days of final grade publication. The Department Chair convenes an Academic Review Committee whose written determination is binding."
            },
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docApex2.Id,
                OrganizationId = orgApex.Id,
                ChunkIndex = 2,
                PageNumber = 3,
                Content = "APEX UNIVERSITY ACADEMIC STANDARDS - SECTION 4.1: PROBATION & COURSE RETAKE POLICY. Any student whose cumulative grade point average drops below 2.00 is placed on Academic Probation for the following semester. While on probation, a student may register for a maximum of 13 credit hours and cannot participate in intercollegiate athletics. Course Retake Policy: A student may repeat a course in which a grade of C- or lower was earned. A course may be repeated a maximum of two times (three total attempts)."
            }
        };
        context.DocumentChunks.AddRange(apexChunks2);


        // ==========================================
        // 2. ORGANIZATION 2: FinTech Global Solutions
        // ==========================================
        var orgFintech = new Organization
        {
            Id = Guid.Parse("22222222-2222-2222-2222-111111111111"),
            Name = "FinTech Global Solutions",
            Slug = "fintech-global",
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };
        context.Organizations.Add(orgFintech);

        var userFintech = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            OrganizationId = orgFintech.Id,
            Email = "admin@fintechglobal.com",
            FullName = "Global Operations Director",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("FinTechAdmin2026!"),
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };
        context.Users.Add(userFintech);

        var assistantFintech = new Assistant
        {
            Id = Guid.Parse("22222222-2222-2222-2222-333333333333"),
            OrganizationId = orgFintech.Id,
            Token = "ast_live_fintech_glob_4c21b",
            Name = "FinTech Corporate Operations Assistant",
            Description = "Corporate assistant for company policies, remote work, equipment allowance, and corporate travel reimbursements.",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };
        context.Assistants.Add(assistantFintech);

        // Doc 1: Remote Work & Equipment Policy
        var docFintech1 = new Document
        {
            Id = Guid.Parse("22222222-2222-2222-2222-444444444441"),
            OrganizationId = orgFintech.Id,
            FileName = "FinTech_Remote_Work_and_Equipment_Policy.pdf",
            OriginalFileName = "Remote_Work_and_Equipment_Policy.pdf",
            FileSizeBytes = 385000,
            PageCount = 4,
            ContentType = "application/pdf",
            StoragePath = "database_indexed",
            UploadedAt = DateTime.UtcNow.AddDays(-25)
        };
        context.Documents.Add(docFintech1);

        var fintechChunks1 = new List<DocumentChunk>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docFintech1.Id,
                OrganizationId = orgFintech.Id,
                ChunkIndex = 0,
                PageNumber = 1,
                Content = "FINTECH GLOBAL SOLUTIONS - REMOTE WORK FRAMEWORK. FinTech Global Solutions supports modern flexible workplace options including hybrid and fully remote arrangements. Employees categorized as Hybrid are expected to work from an authorized corporate hub at least two days per week. All remote arrangements must be approved by the Department Vice President and People Operations."
            },
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docFintech1.Id,
                OrganizationId = orgFintech.Id,
                ChunkIndex = 1,
                PageNumber = 2,
                Content = "FINTECH GLOBAL REMOTE WORK - SECTION 2.2: TENURE ELIGIBILITY REQUIREMENT. Full-time regular employees become eligible to request a 100% Full Remote Work arrangement only after completing a minimum of 6 continuous months of full-time tenure with the company. During the initial 6-month probationary period, new hires must operate under an on-site or hybrid schedule to facilitate team onboarding."
            },
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docFintech1.Id,
                OrganizationId = orgFintech.Id,
                ChunkIndex = 2,
                PageNumber = 3,
                Content = "FINTECH GLOBAL EQUIPMENT POLICY - SECTION 3.1: HOME OFFICE STIPEND & HARDWARE. Approved full-time remote employees are entitled to a one-time Home Office Setup Stipend of up to $1,200.00 USD, renewable every 24 months. Eligible expenses include ergonomic office chairs, external 4K monitors, docking stations, and mechanical keyboards. All company work must be conducted exclusively on corporate-issued laptops with active VPN and encrypted SSD storage."
            },
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docFintech1.Id,
                OrganizationId = orgFintech.Id,
                ChunkIndex = 3,
                PageNumber = 4,
                Content = "FINTECH GLOBAL REMOTE POLICY - SECTION 4.3: INTERNATIONAL REMOTE WORK LIMIT. Regular employees may work from outside their primary country of residence for a maximum aggregate of 30 calendar days per calendar year. Any international remote work exceeding 14 consecutive days mandates written prior authorization from Corporate Legal, Tax Compliance, and Information Security to ensure data sovereignty compliance."
            }
        };
        context.DocumentChunks.AddRange(fintechChunks1);

        // Doc 2: Travel and Expense Guidelines
        var docFintech2 = new Document
        {
            Id = Guid.Parse("22222222-2222-2222-2222-444444444442"),
            OrganizationId = orgFintech.Id,
            FileName = "FinTech_Travel_and_Expense_Guidelines.pdf",
            OriginalFileName = "Travel_and_Expense_Guidelines.pdf",
            FileSizeBytes = 490000,
            PageCount = 3,
            ContentType = "application/pdf",
            StoragePath = "database_indexed",
            UploadedAt = DateTime.UtcNow.AddDays(-22)
        };
        context.Documents.Add(docFintech2);

        var fintechChunks2 = new List<DocumentChunk>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docFintech2.Id,
                OrganizationId = orgFintech.Id,
                ChunkIndex = 0,
                PageNumber = 1,
                Content = "FINTECH GLOBAL EXPENSE POLICY - SECTION 1.2: PER DIEM MEAL ALLOWANCE. Corporate travel meal allowances are provided on a flat per-diem basis of $75.00 USD per full travel day ($50.00 USD for departure and arrival days). Personal alcohol, room entertainment, and mini-bar charges are strictly non-reimbursable. Travelers do not need to submit itemized food receipts when claiming standard per-diem."
            },
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docFintech2.Id,
                OrganizationId = orgFintech.Id,
                ChunkIndex = 1,
                PageNumber = 2,
                Content = "FINTECH GLOBAL TRAVEL GUIDELINES - SECTION 2.4: AIR TRAVEL BOOKING STANDARDS. Domestic Flights: All domestic flights within the employee's country or region with a scheduled duration under 5 continuous hours must be booked in Standard Economy / Main Cabin class. International Flights: Business Class travel is strictly permitted only for long-haul international flights having a nonstop duration exceeding 8 continuous hours."
            },
            new()
            {
                Id = Guid.NewGuid(),
                DocumentId = docFintech2.Id,
                OrganizationId = orgFintech.Id,
                ChunkIndex = 2,
                PageNumber = 3,
                Content = "FINTECH GLOBAL EXPENSE POLICIES - SECTION 3.2: EXPENSE PRE-APPROVAL & RECEIPTS. Pre-Approval Threshold: Any single business expense, conference registration, or itinerary exceeding $1,000.00 USD requires written pre-approval from the Department Vice President before booking or payment commitment. Receipt Requirement: Itemized VAT/tax receipts are mandatory for all non-per-diem expenditures equal to or exceeding $25.00 USD. Expense claims must be submitted within 30 days of incurring."
            }
        };
        context.DocumentChunks.AddRange(fintechChunks2);

        // Seed some sample usage logs for Apex University
        context.UsageLogs.AddRange(
            new UsageLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgApex.Id,
                AssistantId = assistantApex.Id,
                Question = "What is the minimum GPA required for the Tier A merit scholarship?",
                Answer = "According to Section 3.0 of the Scholarship Rules, Tier A Merit Honors requires a cumulative GPA of 3.85 or higher [Scholarship_and_Honors_Rules.pdf — Page 3].",
                Intent = QuestionIntent.DirectInformation,
                WasAnswered = true,
                RequiresHumanReview = false,
                SourceChunksUsedCount = 2,
                ResponseTimeMs = 85,
                Timestamp = DateTime.UtcNow.AddDays(-3)
            },
            new UsageLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgApex.Id,
                AssistantId = assistantApex.Id,
                Question = "I am a 3rd year student with a 3.7 GPA and an active sports scholarship. Am I eligible for a merit scholarship?",
                Answer = "Based on our structured policy assessment under Merit Scholarship and Honors, the application cannot be approved. While your cumulative GPA (3.7 >= 3.50) and class standing (Junior >= Sophomore) satisfy requirements, Section 4.2 strictly prohibits stacking Merit Scholarships with active athletic/sports stipends [Scholarship_and_Honors_Rules.pdf — Page 4].",
                Intent = QuestionIntent.MultiConditionDecision,
                WasAnswered = true,
                RequiresHumanReview = false,
                SourceChunksUsedCount = 3,
                ResponseTimeMs = 120,
                Timestamp = DateTime.UtcNow.AddDays(-2)
            }
        );

        await context.SaveChangesAsync();
    }
}

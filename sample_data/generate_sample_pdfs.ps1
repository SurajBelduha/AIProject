function New-Pdf {
    param(
        [string]$Path,
        [string[]]$PagesText
    )

    $pageCount = $PagesText.Length
    $stream = [System.IO.File]::Create($Path)
    $writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::ASCII)

    $offsets = New-Object System.Collections.Generic.List[long]
    $offsets.Add(0) # 0 is dummy

    $writer.Write("%PDF-1.4`n")

    # Obj 1: Catalog
    $offsets.Add($stream.Position)
    $writer.Write("1 0 obj`n<< /Type /Catalog /Pages 2 0 R >>`nendobj`n")

    # Obj 2: Pages
    $kids = (1..$pageCount | ForEach-Object { "$($_ * 2 + 1) 0 R" }) -join " "
    $offsets.Add($stream.Position)
    $writer.Write("2 0 obj`n<< /Type /Pages /Kids [$kids] /Count $pageCount >>`nendobj`n")

    # Pages and Contents
    for ($i = 0; $i -lt $pageCount; $i++) {
        $pageNum = $i + 1
        $pageObjId = $pageNum * 2 + 1
        $contentObjId = $pageNum * 2 + 2

        $text = $PagesText[$i] -replace "\r", "" -replace "\n", " "
        $text = $text -replace "\(", "\(" -replace "\)", "\)"
        $contentStream = "BT /F1 12 Tf 50 750 Td ($text) Tj ET"
        $contentLen = [System.Text.Encoding]::ASCII.GetByteCount($contentStream)

        # Page Object
        $offsets.Add($stream.Position)
        $writer.Write("$pageObjId 0 obj`n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents $contentObjId 0 R /Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> >>`nendobj`n")

        # Contents Object
        $offsets.Add($stream.Position)
        $writer.Write("$contentObjId 0 obj`n<< /Length $contentLen >>`nstream`n$contentStream`nendstream`nendobj`n")
    }

    # XRef Table
    $xrefPos = $stream.Position
    $totalObjs = $pageCount * 2 + 3
    $writer.Write("xref`n0 $totalObjs`n")
    $writer.Write("0000000000 65535 f `n")
    for ($i = 1; $i -lt $totalObjs; $i++) {
        $off = "{0:D10}" -f $offsets[$i]
        $writer.Write("$off 00000 n `n")
    }

    $writer.Write("trailer`n<< /Size $totalObjs /Root 1 0 R >>`nstartxref`n$xrefPos`n%%EOF`n")
    $writer.Flush()
    $stream.Close()
}

# 1. Apex University: Scholarship and Honors Rules (5 pages)
$apex1Pages = @(
    "APEX UNIVERSITY POLICY MANUAL - SECTION 1.0: GENERAL SCHOLARSHIP ELIGIBILITY. Apex University awards annual undergraduate merit scholarships to recognize exceptional academic achievement. To qualify for consideration, all applicants must maintain full-time enrollment status (minimum 12 credit hours per semester) and have an active student account in good standing. The baseline GPA requirement across all university-wide merit programs is a minimum cumulative GPA of 3.50 on a 4.00 unweighted grading scale.",
    "APEX UNIVERSITY SCHOLARSHIP RULES - SECTION 2.1: ACADEMIC STANDING AND CLASS YEAR REQUIREMENTS. University Merit Scholarships are reserved for continuing students who have demonstrated sustained collegiate academic performance. Applicants must be of Sophomore standing (Year 2 or above), having completed a minimum of 30 earned semester credits at Apex University. First-year students (Freshmen) are not eligible for general University Merit Scholarships during their initial academic year, but may qualify for separate High School Entrance Grants.",
    "APEX UNIVERSITY SCHOLARSHIP TIERS - SECTION 3.0: AWARD TIERS AND DEANS LIST. Tier A Merit Honors: Students possessing a cumulative GPA of 3.85 or higher receive an annual stipend of $8000. Tier B Merit Honors: Students possessing a cumulative GPA between 3.50 and 3.84 receive an annual stipend of $5000. Deans Honors List Recognition: Awarded each semester to undergraduate students completing 14 or more graded credit units with a term GPA of 3.75 or higher and zero grades of Incomplete.",
    "APEX UNIVERSITY SCHOLARSHIP RULES - SECTION 4.2: STACKING RESTRICTIONS AND ATHLETIC EXCLUSIONS. University Merit Scholarships CANNOT be combined, stacked, or held concurrently with any active Athletic, Varsity, or Sports Grant stipends. In accordance with university athletic regulatory bylaws, receiving athletic grant funds automatically disqualifies the student from receiving concurrent merit awards. If a student qualifies for both an athletic stipend and a merit scholarship, the student must select one single funding award in writing prior to the registration census date.",
    "APEX UNIVERSITY SCHOLARSHIP RULES - SECTION 5.0: ANNUAL RENEWAL PROCESS. Merit scholarships are renewable for up to three consecutive academic years (six semesters). To maintain eligibility for automatic renewal, the recipient must maintain a minimum cumulative GPA of 3.40 throughout the academic year and submit the Annual Scholarship Verification Petition to the Financial Aid Office no later than May 15."
)
New-Pdf -Path "sample_data\Apex_University_Scholarship_and_Honors_Rules.pdf" -PagesText $apex1Pages

# 2. Apex University: Academic Integrity and Grading (3 pages)
$apex2Pages = @(
    "APEX UNIVERSITY ACADEMIC POLICIES - SECTION 1.0: GRADING SCALE AND DEFINITIONS. Course letter grades carry the following numerical grade point equivalents: A = 4.0, A- = 3.7, B+ = 3.3, B = 3.0, B- = 2.7, C+ = 2.3, C = 2.0, D = 1.0, F = 0.0. An Incomplete (I) grade must be resolved by the end of the 8th week of the subsequent term; otherwise, the registrar converts it automatically to an F.",
    "APEX UNIVERSITY GRADING POLICY - SECTION 3.4: FORMAL GRADE APPEAL PROCEDURE. A student who believes a final semester grade was calculated erroneously or arbitrarily must first confer informally with the course instructor within 7 calendar days of grade publication. If unresolved, the student must submit a Formal Written Grade Appeal to the Department Chair within 14 calendar days of final grade publication. The Department Chair convenes an Academic Review Committee whose written determination is binding.",
    "APEX UNIVERSITY ACADEMIC STANDARDS - SECTION 4.1: PROBATION AND COURSE RETAKE POLICY. Any student whose cumulative grade point average drops below 2.00 is placed on Academic Probation for the following semester. While on probation, a student may register for a maximum of 13 credit hours and cannot participate in intercollegiate athletics. Course Retake Policy: A student may repeat a course in which a grade of C- or lower was earned. A course may be repeated a maximum of two times (three total attempts)."
)
New-Pdf -Path "sample_data\Apex_University_Academic_Integrity_and_Grading.pdf" -PagesText $apex2Pages

# 3. FinTech Global: Remote Work and Equipment Policy (4 pages)
$fintech1Pages = @(
    "FINTECH GLOBAL SOLUTIONS - REMOTE WORK FRAMEWORK. FinTech Global Solutions supports modern flexible workplace options including hybrid and fully remote arrangements. Employees categorized as Hybrid are expected to work from an authorized corporate hub at least two days per week. All remote arrangements must be approved by the Department Vice President and People Operations.",
    "FINTECH GLOBAL REMOTE WORK - SECTION 2.2: TENURE ELIGIBILITY REQUIREMENT. Full-time regular employees become eligible to request a 100% Full Remote Work arrangement only after completing a minimum of 6 continuous months of full-time tenure with the company. During the initial 6-month probationary period, new hires must operate under an on-site or hybrid schedule to facilitate team onboarding.",
    "FINTECH GLOBAL EQUIPMENT POLICY - SECTION 3.1: HOME OFFICE STIPEND AND HARDWARE. Approved full-time remote employees are entitled to a one-time Home Office Setup Stipend of up to $1200.00 USD, renewable every 24 months. Eligible expenses include ergonomic office chairs, external 4K monitors, docking stations, and mechanical keyboards. All company work must be conducted exclusively on corporate-issued laptops with active VPN and encrypted SSD storage.",
    "FINTECH GLOBAL REMOTE POLICY - SECTION 4.3: INTERNATIONAL REMOTE WORK LIMIT. Regular employees may work from outside their primary country of residence for a maximum aggregate of 30 calendar days per calendar year. Any international remote work exceeding 14 consecutive days mandates written prior authorization from Corporate Legal, Tax Compliance, and Information Security to ensure data sovereignty compliance."
)
New-Pdf -Path "sample_data\FinTech_Remote_Work_and_Equipment_Policy.pdf" -PagesText $fintech1Pages

# 4. FinTech Global: Travel and Expense Guidelines (3 pages)
$fintech2Pages = @(
    "FINTECH GLOBAL EXPENSE POLICY - SECTION 1.2: PER DIEM MEAL ALLOWANCE. Corporate travel meal allowances are provided on a flat per-diem basis of $75.00 USD per full travel day ($50.00 USD for departure and arrival days). Personal alcohol, room entertainment, and mini-bar charges are strictly non-reimbursable. Travelers do not need to submit itemized food receipts when claiming standard per-diem.",
    "FINTECH GLOBAL TRAVEL GUIDELINES - SECTION 2.4: AIR TRAVEL BOOKING STANDARDS. Domestic Flights: All domestic flights within the employee country or region with a scheduled duration under 5 continuous hours must be booked in Standard Economy / Main Cabin class. International Flights: Business Class travel is strictly permitted only for long-haul international flights having a nonstop duration exceeding 8 continuous hours.",
    "FINTECH GLOBAL EXPENSE POLICIES - SECTION 3.2: EXPENSE PRE-APPROVAL AND RECEIPTS. Pre-Approval Threshold: Any single business expense, conference registration, or itinerary exceeding $1000.00 USD requires written pre-approval from the Department Vice President before booking or payment commitment. Receipt Requirement: Itemized VAT tax receipts are mandatory for all non-per-diem expenditures equal to or exceeding $25.00 USD. Expense claims must be submitted within 30 days of incurring."
)
New-Pdf -Path "sample_data\FinTech_Travel_and_Expense_Guidelines.pdf" -PagesText $fintech2Pages

# 5. Oversized PDF for testing 20-page limit rejection (22 pages)
$oversizedPages = 1..22 | ForEach-Object { "Test Document Page $_ of 22 - Designed to test the mandatory limit of maximum 20 pages per PDF document." }
New-Pdf -Path "sample_data\Test_Oversized_22_Pages.pdf" -PagesText $oversizedPages

Write-Output "Sample PDF files generated successfully in sample_data directory!"

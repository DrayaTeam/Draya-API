# ITI — Graduation Project Evaluation Stage 1

## 2. Technology

**Tech Stack**
- **Frontend**: [Your Frontend Tech, e.g., Angular/React/Flutter]
- **Backend**: .NET 8 / C# Web API (Clean Architecture)
- **Database**: [Your Database, e.g., SQL Server/PostgreSQL]
- **Hosting**: [Your Hosting, e.g., Azure/AWS/Local]

**GitHub Repository URL**
- [Insert GitHub Link Here]

**Deployment Status**
- [Local Only / Staging / Live]

**Live Demo URL** *(Optional)*
- [Insert Live Demo Link Here, if applicable]

---

## 3. Team & Contribution

| Team Member Name | Role | Contribution Type (Design/Backend/Frontend/Testing/Docs/Other) | Contribution % |
| :--- | :--- | :--- | :--- |
| **[Name 1]** | [Role, e.g., Backend Developer] | Backend | [X]% |
| **[Name 2]** | [Role] | [Type] | [X]% |
| **[Name 3]** | [Role] | [Type] | [X]% |
| **[Name 4]** | [Role] | [Type] | [X]% |
| *(Total)* | | | *100%* |

**Contribution Evidence** *(Optional)*
- [Link to commit history or pull requests]

---

## 4. Functional Evidence

**What Works Today**
**Admin Flows:**
- **System Overseeing:** Complete management over all platform entities including Students, Teachers, and Supervisors.
- **Classroom & Content Moderation:** Ability to oversee all classrooms created by teachers and moderate platform content.
- **Financial & Revenue Management:** Full tracking of platform revenues, handling instructor payouts, and monitoring digital wallets and payment workflows.

**Teacher Flows:**
- **Classroom & Course Creation:** Ability to create virtual classrooms and organize them structurally into distinct sections.
- **Content & Material Management:** Upload and manage various learning materials (Videos, PDFs, DOCX). Includes background processing (e.g., via Cloudinary) and document versioning to ensure students always have the latest files.
- **AI Material Indexing (RAG Pipeline):** When teachers upload textual materials, the system automatically runs a background job to extract the text, chunk it, generate high-quality vector embeddings (using BGE-M3), and index it securely into a Qdrant Vector Database. This effortlessly builds a course-specific AI knowledge base.
- **Exams & Assessments:** Create tailored exams, construct exam question banks, and oversee student grading.
- **Interactive Instruction:** Respond to student inquiries through the dedicated interactive Q&A system within classroom sections.
- **Financial Tracking:** Access a personal digital wallet to track course sales, revenues, and payout balances.

**Student Flows:**
- **Enrollment & Discovery:** Browse, purchase, and enroll in teacher-led classrooms.
- **Learning & Consumption:** Access course materials with secure video streaming and downloadable files (PDFs, docs).
- **Exams & Performance:** Take assessments and exams, submitting attempts to get instant feedback and grades.
- **Interactive Q&A:** Ask questions directly to the teacher inside the classroom sections.
- **Wallet & Payments:** Fund a digital wallet to purchase courses securely and seamlessly through the platform's integrated payment system.

**User Testing Conducted**
- [Yes / No]

**User Testing Details** *(Required only if User Testing Conducted = Yes)*
- [Describe user testing details]

**Partnerships / Letters of Intent** *(Optional)*
- [Describe the claim; attach a document if available]

---

## 5. Quality & Compliance

**Arabic RTL Support**
- **Yes** (The backend is fully capable of storing and serving UTF-8 Arabic text, providing a solid foundation for an RTL frontend client).

**Data Protection Compliance Notes**
- The system adheres to strict security standards: 
  - **Authentication & Authorization:** Secure, stateless JWT (JSON Web Tokens) are used for all API endpoints.
  - **Password Security:** Passwords are securely hashed using ASP.NET Core Identity's `PasswordHasher`.
  - **Data Privacy (PII):** The database schema implements a `PiiMappingTable` to explicitly isolate, map, and secure Personally Identifiable Information, aiding in compliance with Egyptian data protection laws.

**Test Coverage** *(Optional)*
- **Comprehensive Coverage:** The solution is built with a robust testing suite divided across the Clean Architecture layers, featuring dedicated test projects for `Draya.Api.Tests`, `Draya.Application.Tests`, `Draya.Domain.Tests`, and `Draya.Infrastructure.Tests`.

---

## 6. Market Context

**Market Context**
- The platform addresses the critical need for a modern, centralized educational ecosystem. It eliminates the fragmentation of current tools by combining structured virtual classrooms, automated assessments, and secure financial wallets for instructors into a single, cohesive platform.

**Known Competitors** *(Optional)*
- [List known competitors, e.g., Udemy, Coursera, local educational platforms]

---

## 7. Post-Graduation Gap

**Gap to MVP**
- While the backend API is fully functional and feature-rich, the primary gap to a complete MVP is the development and integration of the frontend client applications (Web/Mobile) to consume these APIs. Additionally, integrating a live production payment gateway (if currently mocked) is required.

**Next Steps**
- Complete the development of the frontend web and mobile applications.
- Deploy the backend infrastructure and Qdrant vector database to a live staging environment (e.g., Azure or AWS).
- Conduct full end-to-end integration and User Acceptance Testing (UAT) with real users.

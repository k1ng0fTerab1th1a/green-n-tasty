# 🍃 Green & Tasty — Restaurant Management System

> A full-stack, cloud-native restaurant management platform built during a **6-week EPAM internship** by a team of 5 engineers. From table reservations and waiter workflows to automated weekly reports and anonymous QR feedback — everything runs serverless on AWS.

---

## 📋 Table of Contents

- [About the Project](#about-the-project)
- [Team](#team)
- [Architecture](#architecture)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Testing](#testing)

---

## About the Project

Green & Tasty is a multi-location restaurant management system built to replace paper-based workflows with a modern, real-time digital platform. The system serves three types of users:

- **Customers** — browse the menu, find available tables across locations, make and manage reservations, and leave feedback.
- **Waiters** — manage their assigned reservations, open orders, generate printed PDF receipts with embedded QR codes, and handle anonymous walk-in visitors.
- **Managers** — receive automated weekly performance reports delivered by email in both Excel and CSV formats.

The project was delivered over **6 weeks** as part of the EPAM internship program (Run #20), following real-world engineering practices: feature branches, code review, a layered .NET backend, and a React SPA frontend — all deployed on AWS.

---

## Team

A team of five engineers who took ownership from design to deployment:

| Name | Role |
|---|---|
| **Oleksii Tolstik** | Frontend |
| **Yevhenii Razinkov** | Backend |
| **Roman Genbach** | Backend |
| **Daryna Suprun** | Backend |
| **Danylo Biletskyi** | Backend |

---

## Architecture

<img width="833" height="469" alt="image" src="https://github.com/user-attachments/assets/d9bc556e-a7c8-4161-b63c-122c1a924442" />


The backend follows a clean three-layer architecture inside the Lambda:
- **Restaurant.Api** — ASP.NET Core controllers, request/response contracts, middleware
- **Restaurant.Core** — domain models, service interfaces, business logic
- **Restaurant.Infrastructure** — DynamoDB repositories, Cognito, SQS, S3, PDF generation

---

## Features

### 🔐 Authentication & Role Assignment
- **AWS Cognito** handles registration, login, email verification, and session management.
- On sign-up, the system checks a pre-seeded **WaiterList** table in DynamoDB: matching emails are automatically promoted to the `WAITER` role; everyone else becomes a `CUSTOMER`.
- Supports password change and email update with a re-verification flow through Cognito.

### 📍 Restaurant Locations & Menu
- Customers and guests can browse all restaurant branches, view location details (address, hours, capacity, rating), see the menu, and read customer feedback.
- Dishes can include name, price, weight, description, high-quality image (stored in S3), and full nutritional facts (calories, proteins, fats, carbs).
- Dishes can be marked **"On Stop"** to hide them from the ordering flow without deletion.

### 🗓️ Table Reservations
A reservation carries several layers of protection to prevent double-booking:

- **Slot locking** — reservations are divided into 15-minute slots (min 60 min, max 360 min). Booking atomically writes to a `TableDay` document using a **DynamoDB conditional transaction** that checks `NOT contains(reservedSlots, ...)` for every slot being claimed. Concurrent requests for the same table/time fail cleanly with a 409.
- **Booking window** — customers can reserve up to 14 days ahead; edit/cancel is disabled 30 minutes before start time.
- **Waiter auto-assignment** — the system looks up which waiter is mapped to the requested table and automatically assigns the reservation.
- **Anonymous reservations** — waiters can create walk-in reservations for unregistered visitors (stored with a visitor name instead of a customer ID).
- Both customers and their assigned waiter can view, edit, or cancel the reservation within the allowed window.

### 📦 Order Management (Waiter)
- Waiters open an order against an active (`InProgress`) reservation, selecting dishes from the live menu.
- Orders store a **snapshot of each dish's price at the time of ordering** — price changes never retroactively affect existing orders.
- Duplicate dish lines are merged by quantity before saving.
- **Idempotency** — the `Order` entity keeps a `processedOperationIds` set and a `version` counter to safely handle retried mutations.
- The waiter can close the order at any time; completion timestamp is recorded automatically.

### 🧾 Receipt & QR Code Feedback
- On order close, the waiter requests a **PDF receipt** generated server-side by **QuestPDF** — formatted as an 80 mm thermal receipt with itemised dishes, quantities, unit prices, and a grand total.
- For **anonymous (walk-in) reservations**, the receipt footer includes a **QR code** (generated with **QRCoder**) containing a one-time `secretCode` link. Scanning it opens a public feedback page — no login required. The secret code is invalidated after the first submission.
- **Authenticated customers** can leave feedback directly from the app, but availability is gated by reservation phase: the **service rating** becomes available once the reservation moves to `InProgress`; the **cuisine rating** only unlocks after the waiter marks the meal as served. Customers can also edit their previously submitted feedback.
- Both flows accept two independent 1–5 star ratings (**service** and **cuisine**) plus an optional comment. Each rating slot can only be filled once per reservation.
- After the feedback is saved, an SQS event is published so the Reports Handler can aggregate it for weekly reports.

### 📊 Automated Weekly Reports
A fully automated reporting pipeline, triggered every week by a **CloudWatch scheduled event**:

1. **Reports Handler Lambda** — listens on SQS for `ReservationCompleted` and `FeedbackCreated` events and incrementally builds report entries in a dedicated `Reports` DynamoDB table.
2. **Reports Sender Lambda** — runs on schedule, queries the last 7 days vs the previous 7-day period, and computes:
   - **Staff performance**: orders per waiter, delta vs previous period (%), avg/min service feedback.
   - **Location comparison**: orders per branch, revenue, delta (%), avg/min cuisine feedback and delta.
3. Report is exported in both **Excel** (.xlsx) and **CSV** formats and delivered via **AWS SES** to the manager's inbox.

### 👤 User Profile
Authenticated users can update their display name, avatar (uploaded to S3), and initiate a password change or email update (with re-verification flow through Cognito).

---

## Tech Stack

### Backend
| Concern | Technology |
|---|---|
| Runtime | .NET 8, ASP.NET Core (hosted in AWS Lambda) |
| Database | AWS DynamoDB (on-demand, GSIs for all query patterns) |
| Auth | AWS Cognito (User Pools, JWT) |
| Messaging | AWS SQS (event-driven report pipeline) |
| Email | AWS SES |
| File storage | AWS S3 (dish images, user avatars, static site) |
| PDF generation | QuestPDF |
| QR codes | QRCoder |
| Result handling | FluentResults |
| Reports export | ClosedXML (Excel), CsvHelper |

### Frontend
| Concern | Technology |
|---|---|
| Framework | React 19 + Vite |
| Routing | React Router v7 |
| HTTP | Axios |

### Testing & QA
| Layer | Technology |
|---|---|
| Unit tests | xUnit, Moq, FluentAssertions |
| Integration tests | ASP.NET Core `WebApplicationFactory`, local DynamoDB |
| System tests | xUnit end-to-end against a live environment |

### Infrastructure & Delivery
| Concern | Tool |
|---|---|
| IaC / deployment | AWS Syndicate |
| API gateway | AWS API Gateway |
| CDN / hosting | S3 static hosting |

---

## Project Structure

```
restaurant-app/
├── backend/
│   └── restaurant-backend-app/
│       └── dnapp/lambdas/
│           ├── restaurant-api/          # Main API Lambda
│           │   ├── Restaurant.Api/      # Controllers, contracts, middleware
│           │   ├── Restaurant.Core/     # Domain models, services, interfaces
│           │   └── Restaurant.Infrastructure/  # DynamoDB, Cognito, SQS, S3, PDF
│           ├── reports-handler/         # SQS-triggered report aggregator
│           └── reports-sender/          # Scheduled weekly report emailer
│
├── frontend/
│   └── restaurant-frontend-app/         # React SPA (Vite)
│       └── src/
│           ├── pages/                   # LocationPage, MenuPage, ReservationsPage, …
│           ├── components/              # Shared UI, modals, cards, layouts
│           ├── services/                # Axios API clients
│           └── auth/                    # Cognito auth context & hooks
│
└── automation-qa/
    ├── UnitTests/                       # Service-layer unit tests
    ├── IntegrationTests/                # Full API integration tests (WebApplicationFactory)
    └── SystemTests/                     # Live end-to-end system tests
```

---

## Testing

The project has three levels of automated testing:

- **Unit tests** — cover most service-layer business logic (reservation slot conflicts, order idempotency, role assignment, feedback limits, etc.) using mocked repositories.
- **Integration tests** — spin up the full ASP.NET Core pipeline in-process with a local DynamoDB container; cover API endpoints including auth flows, booking race conditions, and receipt generation.
- **System tests** — run against a deployed environment to verify critical paths end-to-end (auth flow, reservation lifecycle, feedback submission).

---

*Built with ☕ and a lot of GitLab merge requests during EPAM Internship Run #20.*

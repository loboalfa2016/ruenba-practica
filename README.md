# FrameRent — Audiovisual Equipment Rental Solution

FrameRent is an enterprise audiovisual equipment rental platform designed to replace fragile spreadsheet workflows and manual WhatsApp management. The system prevents double bookings, enforces strict date-based inventory availability, automatically computes rental pricing, enforces customer limits, and controls the full rental lifecycle from initial reservation to gear return and overdue penalties.

---

## 🏛 Clean Architecture Overview

The solution adheres strictly to **Clean Architecture** principles, maintaining inward dependency flow towards the core domain without external infrastructure coupling:

```
                  ┌─────────────────────────────────────────┐
                  │       FrameRent.Domain (Entities,       │
                  │        Enums, Business Invariants)      │
                  └────────────────────▲────────────────────┘
                                       │
                  ┌────────────────────┴────────────────────┐
                  │          FrameRent.Application          │
                  │    (DTOs, Service Contracts & Logic)    │
                  └────────────────────▲────────────────────┘
                                       │
                  ┌────────────────────┴────────────────────┐
                  │        FrameRent.Infrastructure         │
                  │ (EF Core, Npgsql, Identity, JWT, Seeds) │
                  └──────▲───────────────────────────▲──────┘
                         │                           │
          ┌──────────────┴──────────┐     ┌──────────┴──────────────┐
          │      FrameRent.API      │     │      FrameRent.Web      │
          │ (REST Endpoints, Scalar)│     │  (Option A Presentation)│
          └─────────────────────────┘     └─────────────────────────┘
```

- **Domain (`FrameRent.Domain`)**: Pure C# domain model containing core invariants and state transitions for `Equipment` and `Rental`.
- **Application (`FrameRent.Application`)**: Data Transfer Objects (DTOs), service abstractions, business logic validation, availability calculations, and reporting.
- **Infrastructure (`FrameRent.Infrastructure`)**: Entity Framework Core persistence with PostgreSQL (`Npgsql`), ASP.NET Core Identity, JWT token generation, and database initializers.
- **API (`FrameRent.API`)**: RESTful API exposing v1 endpoints with JWT authentication, role authorization, and Scalar API documentation.
- **Web (`FrameRent.Web`)**: ASP.NET Core MVC presentation layer directly consuming Application services with a custom cinematic dark theme (Deep Slate Navy, Energetic Orange, and Emerald Green).
- **Tests (`FrameRent.Tests`)**: Comprehensive xUnit test suite testing all domain rules, lifecycle transitions, overdue penalties, and application availability algorithms.

---

## 🚀 Key Business Rules Implemented

1. **Valid Dates & Duration Limit**: Rental start dates cannot be in the past; end dates must be strictly after the start date. A rental can last a maximum of 15 days.
2. **Dynamic Inventory Availability**: Available units are calculated dynamically based on overlapping approved or delivered rentals within the requested date window:
   $$\text{Booked Units} = \sum_{\text{Approved/Delivered rentals with overlap}} \text{Quantity}$$
   $$\text{Available Units} = \max(0, \text{TotalUnits} - \text{Booked Units})$$
3. **Automated Price Calculation**: Total cost is calculated by the system ($\text{Rental Days} \times \text{Daily Rate} \times \text{Quantity}$); any client-submitted price is ignored.
4. **Client Concurrency Limit**: A customer cannot have more than 3 pending or approved rentals simultaneously.
5. **Rental Lifecycle Transitions**:
   - $\text{Pending} \rightarrow \text{Approved} \rightarrow \text{Delivered} \rightarrow \text{Returned}$
   - $\text{Pending} \rightarrow \text{Rejected}$
   - $\text{Pending or Approved} \rightarrow \text{Canceled}$ (only prior to the start date)
   Any invalid transition is rejected with an explanatory domain error.
6. **Approval Re-validation**: Availability is re-verified upon approval in case fleet inventory changed since the initial request was filed.
7. **Overdue Penalty Calculation**: If equipment is returned after the scheduled end date, an automated penalty is calculated:
   $$\text{Penalty} = \text{Overdue Days} \times \text{Daily Rate} \times \text{Quantity} \times 1.5$$
8. **Soft Deactivation**: Equipment is never deleted. Deactivation is forbidden if approved or delivered rentals exist for that item. Inactive gear is omitted from the catalog.
9. **Business Intelligence**: Staff can generate a report of the most rented equipment sorted by rental count descending and total revenue generated.

---

## 🛠 Tech Stack

- **Platform**: .NET 10 (C# 13)
- **Database**: PostgreSQL 15 via Docker Compose
- **ORM**: Entity Framework Core 10 (Code-First with Npgsql provider)
- **Security & Identity**: ASP.NET Core Identity & JWT Bearer Authentication (`System.IdentityModel.Tokens.Jwt`)
- **Interactive Documentation**: Scalar API Reference (`Scalar.AspNetCore`)
- **Presentation**: ASP.NET Core MVC with Razor & custom cinematic design system
- **Testing**: xUnit with InMemory EF Core provider

---

## 🔑 Default Staff Credentials

The system seeds the initial staff account on startup:

| Field | Value |
|---|---|
| **Email** | `admin@framerent.com` |
| **Password** | `StaffSecure2026*` |
| **Role** | `Staff` |

> Any user registered through the `/api/v1/auth/register` endpoint or the Web registration form is automatically assigned the `Client` role.

---

## ⚙️ Setup and Execution Instructions

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker & Docker Compose](https://www.docker.com/)

### 2. Start PostgreSQL Database
From the root folder, launch the PostgreSQL container:
```bash
docker compose up -d
```
The database will be exposed at `localhost:5432` with user `postgres` and database name `FrameRentDb`.

### 3. Apply EF Core Migrations
Apply the existing Code-First migration to PostgreSQL:
```bash
dotnet ef database update --project src/FrameRent.Infrastructure --startup-project src/FrameRent.API
```

### 4. Run the Automated Tests (xUnit)
Run the complete test suite to verify business invariants:
```bash
dotnet test
```

### 5. Launch the REST API
```bash
dotnet run --project src/FrameRent.API
```
- Interactive API Reference (Scalar): `https://localhost:5001/scalar/v1` or `http://localhost:5000/scalar/v1`
- OpenAPI JSON Spec: `/openapi/v1.json`

### 6. Launch the Web Application (FrameRent.Web)
In a separate terminal:
```bash
dotnet run --project src/FrameRent.Web
```
Visit `https://localhost:5100` (or the console URL) in your browser:
- **Visitors**: Explore gear, check date-based availability live.
- **Clients**: Register, sign in, request rentals with live cost estimates, view "My Rentals", cancel reservations.
- **Staff**: Access the Staff Portal to approve, reject, deliver, return (with live late fee calculation preview), and inspect the Top Equipment Report.

---

## 📡 REST API Endpoints Specification

### Authentication
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `POST` | `/api/v1/auth/register` | Public | Registers a new client account (`Role = Client`) |
| `POST` | `/api/v1/auth/login` | Public | Authenticates credentials and returns a signed JWT token |

### Equipment Fleet
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `GET` | `/api/v1/equipment` | Public | Returns active equipment catalog |
| `GET` | `/api/v1/equipment/{id}` | Public | Detailed view of a piece of equipment |
| `GET` | `/api/v1/equipment/{id}/availability?from=&to=` | Public | Computes real-time available units for a date interval |
| `POST` | `/api/v1/equipment` | Staff | Registers new equipment into fleet |
| `PUT` | `/api/v1/equipment/{id}` | Staff | Updates equipment properties |
| `PATCH` | `/api/v1/equipment/{id}/deactivate` | Staff | Deactivates equipment (blocks if active rentals exist) |

### Rentals
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `POST` | `/api/v1/rentals` | Client | Submits a new rental request |
| `GET` | `/api/v1/rentals/mine` | Client | Retrieves current customer's rentals |
| `GET` | `/api/v1/rentals/{id}` | Owner or Staff | Detailed view of an individual rental |
| `PATCH` | `/api/v1/rentals/{id}/cancel` | Owner | Cancels rental (if Pending/Approved & before start date) |
| `GET` | `/api/v1/rentals?status=` | Staff | Lists all rentals across clients with optional status filter |
| `PATCH` | `/api/v1/rentals/{id}/approve` | Staff | Approves rental (re-verifies fleet availability) |
| `PATCH` | `/api/v1/rentals/{id}/reject` | Staff | Rejects rental request |
| `PATCH` | `/api/v1/rentals/{id}/deliver` | Staff | Records handover of equipment |
| `PATCH` | `/api/v1/rentals/{id}/return` | Staff | Records gear return and computes 1.5x penalty if late |

### Reports
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `GET` | `/api/v1/reports/topequipment` | Staff | Lists most rented equipment sorted descending by rental count |

---

## 👤 Author

- **Full Name**: Emanuel Sandoval Jimenez
- **Clan**: .NET Clan
- **Email**: emanuelsj2008@gmail.com
- **National ID / Document**: CC 1000000000 <!-- Update with your ID number -->

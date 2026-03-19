# ClassicFit: Apparel Fashion Sales and Support Management System

## Student Information

- **Name:** IVY CARL M. BENJAMIN
- **Subject Code:** IT15/L - Integrative Programming and Technologies 8460
- **Time:** 3:30 PM - 5:30 PM
- **Business Process Topic:** #20 Call Center / Contact Center System

## Project Title

**ClassicFit: An Apparel Fashion Sales Support Management System**

## Products and Services

- Customer Support
- Product Sales

## Tools Needed

- Visual Studio 2022 (IDE)
- SQL Server Management Studio (SSMS)
- Domain
- Hosting
- GitHub

## Technology Stack

- **Backend:** C# with ASP.NET Core Web API
- **Frontend:** Blazor WebAssembly, Tailwind CSS (UI)
- **Database:** SQL Server
- **Deployment:** MonsterASP.NET
- **External APIs:**
  - ERP Inventory API
  - ERP Finance API

## Security Features

- Authentication (JWT)
- Authorization (Role-Based Access)
- Input Validation
- Password Hashing

## Target Users

- Customer
- Call Center Agents
- Team Supervisors
- Admin
- Super Admin

## Subsystems / Management Transactions / Modules

- Ticket Management Module
- Call Logging Module
- Customer Interaction History Module
- Sales and Order Management Module
- Performance Reports Module
- Inventory Module

## Project Objectives

1. Create a suitable platform for managing shirt orders, returns, and customer inquiries.
2. Minimize manual work by automatically recording calls and generating tickets linked to relevant data.
3. Track customer interaction history, including all calls, tickets, and shirt orders, to provide complete context for each customer.
4. Present real-time dashboards for supervisors showing handling time, resolution rate, and shirt sales conversion.

## Project Description

The **ClassicFit Apparel Fashion: Sales and Support Management System** transforms a standard call center workflow into a retail-specific ERP-connected hub by combining customer support and shirt sales with direct integration to inventory and finance systems.

Customers can place orders, request returns, or seek support. Agents manage calls and tickets with instant ERP access. Supervisors monitor performance through real-time dashboards. Administrators control users, roles, and overall security.

The platform delivers measurable impact by streamlining workflows, improving customer satisfaction through faster and more accurate ERP-backed support, and increasing agent productivity through integrated tools and real-time operational data.

Built with ASP.NET Core Web API and C# for scalable backend services, Blazor WebAssembly for interactive frontend experiences, Tailwind CSS for responsive UI, and SQL Server for reliable transactions, the system is designed for enterprise-grade performance.

Integration with the **ERP Inventory API** provides real-time stock visibility, while the **ERP Finance API** enables secure payment validation and invoicing. This makes the solution specific, measurable, achievable, relevant, and time-bound for modern retail apparel operations.

## Role-Based Access (Call Center / Contact Center System)

### Customer

- Initiate support via phone, chat, email, or social media
- Place shirt orders or request returns

### Call Center Agent

- Handle calls/chats with automatic timers
- Create and update tickets
- Process shirt sales orders
- View customer 360-degree history
- View personal performance statistics

### Team Supervisor

- Monitor all team sessions across channels
- View and prioritize tickets
- Access sales orders and performance data
- Generate team analytics reports
- Access customer interaction history

### System Administrator

- Manage user accounts and roles
- Access all modules (Tickets, Calls, Orders, Reports)
- View audit logs and system monitoring data

### Super Admin

- Manage accounts
- Full audit logging control
- View platform dashboard

## Professor Requirement Compliance Checklist

The following checklist maps required modules to the current system implementation status:

| Required Module / Subsystem | Status | Current Evidence in System | Notes |
|---|---|---|---|
| Ticket Management Module | Fully Met | Ticket CRUD, assignment, escalation workflows | Implemented in tickets API/services |
| Call Logging Module | Fully Met | Call CRUD, start/end actions, call summary | Implemented in calls API/services |
| Customer Interaction History Module | Fully Met | Unified customer timeline for calls, tickets, orders | Implemented through timeline endpoint |
| Sales and Order Management Module | Fully Met | Order CRUD, status updates, totals, payment status fields | Implemented in orders API/services |
| Inventory Module | Fully Met | Product/inventory operations with ERP inventory integration | Implemented in product + ERP inventory endpoints |
| Performance Reports Module | Fully Met | Reports API with performance filtering and CSV/PDF export | Implemented via reports endpoints |
| Automatic call recording and ticket generation linkage | Fully Met | Unresolved call outcomes now auto-create linked follow-up tickets | Implemented in call end workflow with ticket auto-generation |
| Multi-channel support (chat/email/social) | Fully Met | Channel interaction endpoints for chat, email, and social media | Implemented in channels API with create/list/resolve actions |

### Recommended Next Enhancement

1. Add a **compliance demo script** (module-by-module walkthrough for defense).

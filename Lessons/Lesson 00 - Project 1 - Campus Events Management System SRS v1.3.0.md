# Campus Events Management System - Software Requirements Specification (SRS)

## Table of Contents

- [Document Control](#document-control)
  - [Revision History](#revision-history)
- [1. Introduction](#introduction)
  - [1.1 Purpose](#purpose)
  - [1.2 Scope](#scope)
  - [1.3 Out of Scope / Future Enhancements](#out-of-scope-future-enhancements)
  - [1.4 Intended Audience](#intended-audience)
  - [1.5 Definitions and Terms](#definitions-and-terms)
  - [1.6 Requirement Conventions](#requirement-conventions)
- [2. Product Overview](#product-overview)
  - [2.1 Product Perspective](#product-perspective)
  - [2.2 Major Product Capabilities](#major-product-capabilities)
  - [2.3 User Classes and Actors](#user-classes-and-actors)
  - [2.4 Operating Environment](#operating-environment)
  - [2.5 Assumptions and Dependencies](#assumptions-and-dependencies)
- [3. Design Constraints](#design-constraints)
  - [3.1 Required Technology and Architecture](#required-technology-and-architecture)
  - [3.2 Architectural Boundaries](#architectural-boundaries)
- [4. Conceptual Domain Model](#conceptual-domain-model)
  - [4.1 Core Entities](#core-entities)
- [5. Business Rules](#business-rules)
  - [5.1 Authentication and Authorization Rules](#authentication-and-authorization-rules)
  - [5.2 Event Rules](#event-rules)
  - [5.3 Registration Rules](#registration-rules)
  - [5.4 Category Rules](#category-rules)
- [6. Use Cases](#use-cases)
  - [6.1 UC-001 — Browse, Search, and View Events](#uc-001-browse-search-and-view-events)
  - [6.2 UC-002 — Create an Account and Authenticate](#uc-002-create-an-account-and-authenticate)
  - [6.3 UC-003 — Register for an Event](#uc-003-register-for-an-event)
  - [6.4 UC-004 — Review and Cancel Own Registrations](#uc-004-review-and-cancel-own-registrations)
  - [6.5 UC-005 — Create and Manage an Owned Event](#uc-005-create-and-manage-an-owned-event)
  - [6.6 UC-006 — Review Registrations for an Owned Event](#uc-006-review-registrations-for-an-owned-event)
  - [6.7 UC-007 — Manage Categories and Organizer Access](#uc-007-manage-categories-and-organizer-access)
  - [6.8 UC-008 — Perform Administrative Event Oversight](#uc-008-perform-administrative-event-oversight)
- [7. Functional Requirements](#functional-requirements)
  - [7.1 Accounts and Authentication](#accounts-and-authentication)
  - [7.2 Event Discovery, Search, and Browsing](#event-discovery-search-and-browsing)
  - [7.3 Event Management](#event-management)
  - [7.4 Event Registration](#event-registration)
  - [7.5 Administration](#administration)
- [8. Data Requirements](#data-requirements)
  - [8.1 Required Event Information](#required-event-information)
  - [8.2 Required Registration Information](#required-registration-information)
  - [8.3 Data Integrity Expectations](#data-integrity-expectations)
- [9. External Interface Requirements](#external-interface-requirements)
  - [9.1 User Interface](#user-interface)
  - [9.2 Browser Interface](#browser-interface)
  - [9.3 Authentication Interface](#authentication-interface)
  - [9.4 External Services](#external-services)
- [10. Non-Functional Requirements](#non-functional-requirements)
  - [10.1 Security](#security)
  - [10.2 Performance](#performance)
  - [10.3 Usability](#usability)
  - [10.4 Accessibility](#accessibility)
  - [10.5 Reliability and Data Correctness](#reliability-and-data-correctness)
  - [10.6 Compatibility](#compatibility)
- [11. Error Handling and Exception Behavior](#error-handling-and-exception-behavior)
- [12. Acceptance Criteria / Definition of Done](#acceptance-criteria-definition-of-done)
- [13. Requirements Traceability Matrix](#requirements-traceability-matrix)
- [14. Requirements-to-Course Concept Map](#requirements-to-course-concept-map)

---

<a id="document-control"></a>
## Document Control

**Document version:** 1.3

Version 1.0 is the semester baseline. Later approved requirement changes should be associated with a Change Request identifier such as `CR-001`. Existing requirement identifiers should normally remain stable. Retired requirements are marked retired; their identifiers are not silently reassigned to different behavior.

<br>

<a id="revision-history"></a>
### Revision History

| Version | Date | Author | Reviewer/Approver | Change Request | Summary of Changes |
| --- | --- | --- | --- | --- | --- |
| 1.0 | 2026‑08‑30 | HBV | HBV | — | Initial semester baseline |
| 1.1 | 2026-09-01 | HBV | HBV | CR-001 | Navigable bookmarks added |
| 1.2 | 2026-09-02 | HBV | HBV | CR-002 | Navigable TOC added |
| 1.3 | 2026-09-26 | HBV | HBV | CR-003 | Added public Event catalog pagination (`FR-SRH-009`); updated UC-001, scope, interfaces, performance, accessibility, error handling, acceptance criteria, traceability, and course concept map. |

---

<br>

<a id="introduction"></a>
# 1. Introduction

<a id="purpose"></a>
## 1.1 Purpose

This Software Requirements Specification defines the required behavior of the **Campus Events Management System (CEMS)** for Project #1 in the Full-Stack Development course.

CEMS is a server-rendered web application that allows members of a campus community to discover events, register for events, and manage event participation. Authorized organizers can create and manage events, while administrators can manage event categories, organizer access, and exceptional event issues.

This document is the authoritative reference for what the application must do. Students are expected to use it as a roadmap while making design and implementation decisions throughout the project.

<a id="scope"></a>
## 1.2 Scope

The system will support these main capabilities:

- public browsing of upcoming campus events;
- event search, category filtering, and pagination of public catalog results;
- account registration, login, and logout;
- participant registration for available events;
- participant cancellation of an existing registration;
- organizer creation and management of owned events;
- organizer viewing of registrations for owned events;
- administrator management of event categories and organizer access;
- administrator oversight of events;
- persistent storage of application data in a relational database.

The project intentionally focuses on a traditional server-rendered multi-page web application. Features such as real-time chat, live notifications, SPA state management, payment processing, and advanced third-party integrations are outside the required scope.

<a id="out-of-scope-future-enhancements"></a>
## 1.3 Out of Scope / Future Enhancements

The following capabilities are intentionally **not required** for Project #1. They may be appropriate for a later project or future course work.

- single-page application architecture;
- React or another SPA framework for the primary user interface;
- a separate public REST API for the main application;
- real-time chat;
- live push notifications;
- typing indicators, presence, or other real-time collaboration features;
- event waitlists;
- payment processing or paid tickets;
- QR-code tickets or check-in scanning;
- calendar synchronization;
- email or SMS notifications;
- image or document uploads;
- maps or geolocation services;
- complex event recommendations;
- advanced analytics dashboards;
- multi-campus or multi-tenant isolation;
- external university identity integration;
- native mobile applications;
- audit-history or event-sourcing systems;
- background job processing;
- advanced caching infrastructure;
- microservices.

These exclusions are intentional scope controls. Students are not expected to implement them to satisfy Project #1.

<a id="intended-audience"></a>
## 1.4 Intended Audience

This document is intended primarily for:

- students implementing Project #1;
- the course instructor evaluating the project;
- teaching assistants or reviewers supporting the course.

The document specifies expected system behavior. It does not prescribe detailed implementation choices except where a technology or architectural choice is an explicit course design constraint.

<a id="definitions-and-terms"></a>
## 1.5 Definitions and Terms

| Term | Meaning |
|---|---|
| **Visitor** | A person using the application without being authenticated. |
| **Participant** | An authenticated user who can register for campus events. |
| **Organizer** | An authenticated user with permission to create and manage events that they own. |
| **Administrator** | An authenticated user with application-wide administrative permissions. |
| **Event** | A scheduled campus activity that users may view and, when allowed, register to attend. |
| **Registration** | The current association showing that a participant has reserved a place in an event. |
| **Category** | A label used to classify events, such as Academic, Social, Sports, or Career. |
| **Published Event** | An event that has been made visible to visitors and participants. |
| **Draft Event** | An event that exists but has not yet been published publicly. |
| **Cancelled Event** | An event that was published but is no longer scheduled to take place. |

<a id="requirement-conventions"></a>
## 1.6 Requirement Conventions

Mandatory requirements use the word **shall**.

Requirement identifiers use these prefixes:

- [`FR-ACC`](#accounts-and-authentication) — account and authentication requirements;
- [`FR-SRH`](#event-discovery-search-and-browsing) — event discovery, search, and browsing requirements;
- [`FR-EVT`](#event-management) — event management requirements;
- [`FR-REG`](#event-registration) — event registration requirements;
- [`FR-ADM`](#administration) — administrative requirements;
- [`BR-*`](#business-rules) — business rules;
- [`NFR-*`](#non-functional-requirements) — non-functional requirements.

---

<a id="product-overview"></a>
# 2. Product Overview

<a id="product-perspective"></a>
## 2.1 Product Perspective

CEMS is a small campus event application intended to demonstrate how a complete web product works across the browser, server application, business logic, authentication system, relational database, testing workflow, and deployed environment.

The application is not intended to represent a complete university information system. It does not integrate with official student records, course registration, payments, or campus identity services.

<a id="major-product-capabilities"></a>
## 2.2 Major Product Capabilities

The completed system will allow users to move through several complete workflows:

1. A visitor browses, searches, filters, and navigates paginated public Event results before opening an event's details.
2. A visitor creates an account and authenticates.
3. A participant registers for an available event.
4. A participant reviews or cancels an existing registration.
5. An organizer creates an event, edits it, and publishes it.
6. An organizer reviews the participants registered for an owned event.
7. An administrator manages categories and grants or removes organizer access.
8. An administrator can intervene when an event requires application-wide oversight.

These workflows are intended to exercise multiple application layers rather than isolated CRUD operations.

<a id="user-classes-and-actors"></a>
## 2.3 User Classes and Actors

| Actor | General Capabilities |
|---|---|
| **Visitor** | Browse, search, filter, navigate paginated public Event results, and view published events; create an account; log in. |
| **Participant** | All visitor capabilities plus register for events, cancel own registrations, and view own registrations. |
| **Organizer** | All participant capabilities plus create, edit, publish, cancel, and manage registrations for owned events. |
| **Administrator** | Application-wide oversight; manage categories; grant or remove organizer access; review and cancel any event. |

A single authenticated user may have participant capabilities and may additionally have the Organizer or Administrator role.

<a id="operating-environment"></a>
## 2.4 Operating Environment

The application will run as a web application accessed through a modern desktop or mobile web browser. Application data will be stored in a relational database. The final system will be deployed to an instructor-approved hosting environment.

<a id="assumptions-and-dependencies"></a>
## 2.5 Assumptions and Dependencies

- Users have access to a modern web browser and network connection.
- The instructor may provide initial seed data such as administrator accounts and event categories.
- The application does not verify whether a registered account belongs to an actual university student or employee.
- Event organizers are trusted campus users who receive organizer access through an administrator.
- The system uses a single application time zone chosen for the course environment.

---

<a id="design-constraints"></a>
# 3. Design Constraints

This section identifies imposed project constraints. These are not functional requirements.

<a id="required-technology-and-architecture"></a>
## 3.1 Required Technology and Architecture

The application shall be implemented using the following course-approved technology:

- ASP.NET Core;
- C#;
- ASP.NET Core MVC;
- Razor Views;
- a server-rendered multi-page application architecture;
- Entity Framework Core;
- a relational database; preferably SQLServer to keep integration compatibility at maximum
- ASP.NET Core Identity or an instructor-approved equivalent ASP.NET authentication mechanism;
- HTML and CSS; no CSS framework (e.g., Bootstrap, Tailwind);
- client-side JavaScript only where it provides a clear usability benefit; no JS framework (e.g., jQuery, React, Vue, Angular);
- Git and GitHub for version control;
- automated testing for selected critical behaviors;
- deployment to an instructor-approved environment.

The conceptual application flow is:

```text
Browser
   ↓
ASP.NET Core MVC
   ↓
Application / Business Logic
   ↓
Entity Framework Core
   ↓
Relational Database
```

<a id="architectural-boundaries"></a>
## 3.2 Architectural Boundaries

- The primary application shall remain a server-rendered MPA.
- A separate SPA front end is not required.
- A separate REST API is not required for the main application.
- Advanced architectural patterns such as CQRS, MediatR, microservices, or Clean Architecture are not required.
- Students may organize application code using reasonable separation of concerns appropriate to the project size.

---

<a id="conceptual-domain-model"></a>
# 4. Conceptual Domain Model

The following domain concepts represent the minimum required data model. This section describes the information conceptually and does not prescribe C# classes or database table definitions.

<a id="core-entities"></a>
## 4.1 Core Entities

| Entity | Purpose | Important Relationships |
|---|---|---|
| **User** | Represents an authenticated application user and the user's assigned access roles. | A User may organize many Events. A User may register for many Events through Registrations. |
| **Category** | Classifies events into simple discoverable groups. | One Category may classify many Events. Each Event belongs to one Category. |
| **Event** | Stores the scheduled campus activity and its publication state. | Each Event belongs to one Organizer User and one Category. An Event may have many Registrations. |
| **Registration** | Represents a participant's current reserved place in an Event. | Each Registration belongs to one User and one Event. |

---

<a id="business-rules"></a>
# 5. Business Rules

<a id="authentication-and-authorization-rules"></a>
## 5.1 Authentication and Authorization Rules

| ID | Rule |
|---|---|
| <a id="br-aut-001"></a>[**BR&#8209;AUT&#8209;001**](#br-aut-001) | Registration management, organizer functions, and administrator functions require an authenticated user. |
| <a id="br-aut-002"></a>[**BR&#8209;AUT&#8209;002**](#br-aut-002) | An organizer may manage only events owned by that organizer unless the user also has Administrator access. |
| <a id="br-aut-003"></a>[**BR&#8209;AUT&#8209;003**](#br-aut-003) | Administrative functions are available only to users with Administrator access. |
| <a id="br-aut-004"></a>[**BR&#8209;AUT&#8209;004**](#br-aut-004) | Authorization shall be enforced by the server even when the user interface hides or disables an unavailable action. |

<a id="event-rules"></a>
## 5.2 Event Rules

| ID | Rule |
|---|---|
| <a id="br-evt-001"></a>[**BR&#8209;EVT&#8209;001**](#br-evt-001) | An event start date and time shall occur before its end date and time. |
| <a id="br-evt-002"></a>[**BR&#8209;EVT&#8209;002**](#br-evt-002) | A Draft event may be published only when its start date and time are in the future. |
| <a id="br-evt-003"></a>[**BR&#8209;EVT&#8209;003**](#br-evt-003) | Event capacity shall be a whole number greater than zero. |
| <a id="br-evt-004"></a>[**BR&#8209;EVT&#8209;004**](#br-evt-004) | Every event shall reference an existing event category. |
| <a id="br-evt-005"></a>[**BR&#8209;EVT&#8209;005**](#br-evt-005) | Only Published upcoming events are normally shown in public event browsing results. |
| <a id="br-evt-006"></a>[**BR&#8209;EVT&#8209;006**](#br-evt-006) | A Published event may be cancelled only before the event starts. |
| <a id="br-evt-007"></a>[**BR&#8209;EVT&#8209;007**](#br-evt-007) | A Cancelled event shall not return to Published status in the required version of the project. |
| <a id="br-evt-008"></a>[**BR&#8209;EVT&#8209;008**](#br-evt-008) | A Draft event may be permanently deleted only when it has no current registrations. |
| <a id="br-evt-009"></a>[**BR&#8209;EVT&#8209;009**](#br-evt-009) | An organizer may not change the participant capacity of a Published event to a value lower than the number of current registrations. |
| <a id="br-evt-010"></a>[**BR&#8209;EVT&#8209;010**](#br-evt-010) | A Cancelled event may no longer be edited or published by its organizer in the required version of the project. |

<a id="registration-rules"></a>
## 5.3 Registration Rules

| ID | Rule |
|---|---|
| <a id="br-reg-001"></a>[**BR&#8209;REG&#8209;001**](#br-reg-001) | Only authenticated participants may hold event registrations. |
| <a id="br-reg-002"></a>[**BR&#8209;REG&#8209;002**](#br-reg-002) | A participant may hold at most one current registration for a particular event. |
| <a id="br-reg-003"></a>[**BR&#8209;REG&#8209;003**](#br-reg-003) | The number of current registrations shall not exceed the event capacity. |
| <a id="br-reg-004"></a>[**BR&#8209;REG&#8209;004**](#br-reg-004) | Registration is permitted only for a Published event that has not started or been cancelled. |
| <a id="br-reg-005"></a>[**BR&#8209;REG&#8209;005**](#br-reg-005) | A participant may cancel only the participant's own registration. |
| <a id="br-reg-006"></a>[**BR&#8209;REG&#8209;006**](#br-reg-006) | Cancelling a registration removes that participant from the event's current registration count. |

<a id="category-rules"></a>
## 5.4 Category Rules

| ID | Rule |
|---|---|
| <a id="br-cat-001"></a>[**BR&#8209;CAT&#8209;001**](#br-cat-001) | Category names shall be unique without regard to letter case. |
| <a id="br-cat-002"></a>[**BR&#8209;CAT&#8209;002**](#br-cat-002) | A category referenced by one or more events shall not be deleted. |

---

<a id="use-cases"></a>
# 6. Use Cases

This section describes the principal end-to-end interactions supported by CEMS. The use cases correspond to the workflows summarized in Section 2.2 and organize behavior already defined by the business rules, functional requirements, and error behavior elsewhere in this specification. They do not introduce additional requirements. When a use case summary and a linked requirement differ in level of detail, the linked requirement is authoritative.

Use cases use identifiers in the form `UC-###`.

<a id="uc-001-browse-search-and-view-events"></a>
## 6.1 UC-001 — Browse, Search, and View Events

**Goal:** Allow a visitor or authenticated user to discover published upcoming events through paginated browsing, search, and filtering, and to view event details.

**Primary Actor:** Visitor or authenticated user.

**Preconditions:** None.

**Trigger:** The user wants to discover or inspect a campus event.

**Main Flow:**

1. The user opens the event browsing interface.
2. The system displays the first bounded page of published upcoming events in chronological order by start date and time unless another supported ordering is available.
3. The user may search using text matching at least an event title or location.
4. The user may filter events by category. A new search or filter submission returns to the first page of its matching results.
5. When matching results span multiple pages, the user may navigate to another available results page; the system retains the active search and Category criteria, chronological ordering, and the total matching result count.
6. The user selects a published event from the displayed page.
7. The system displays the event title, description, category, location, start and end date/time, organizer name, capacity information, and current registration availability.

**Alternate Flows:**

- **A3.1 / A4.1.** If a search or category filter produces no matching events, the system displays a clear no-results message without pagination links.
- **A5.1.** If a requested results-page number is missing, malformed, nonpositive, or beyond the available pages, the system applies the public-catalog page-boundary behavior in Section 11.
- **A1.1.** A previously published cancelled event remains viewable by direct link and is clearly identified as cancelled.
- **A2.1.** A cancelled event does not appear as available for new registration.

**Exception Flows:**

- None.

**Postconditions:** The user has viewed the available event list, search/filter results, or the details of a selected event; no application data is changed.

**Related Requirements:** [`FR-SRH-001`](#fr-srh-001), [`FR-SRH-002`](#fr-srh-002), [`FR-SRH-003`](#fr-srh-003), [`FR-SRH-004`](#fr-srh-004), [`FR-SRH-005`](#fr-srh-005), [`FR-SRH-006`](#fr-srh-006), [`FR-SRH-007`](#fr-srh-007), [`FR-SRH-008`](#fr-srh-008), [`FR-SRH-009`](#fr-srh-009).

**Related Business Rules:** [`BR-EVT-005`](#br-evt-005).

<a id="uc-002-create-an-account-and-authenticate"></a>
## 6.2 UC-002 — Create an Account and Authenticate

**Goal:** Allow a visitor to create a participant account and allow a registered user to authenticate and end an authenticated session.

**Primary Actor:** Visitor or registered user.

**Preconditions:** The user is not authenticated when starting account registration or login.

**Trigger:** The user wants to create an account, authenticate, or access an operation that requires authentication.

**Main Flow:**

1. A visitor provides the required account information to create a participant account.
2. The system creates the account when the supplied login identity is not already associated with another account.
3. A registered user provides valid credentials to authenticate.
4. The system establishes the authenticated session.
5. When authentication was required because the user attempted to access a protected page, the system allows the user to return to the originally requested page after successful authentication when practical.
6. An authenticated user may log out to end the current authenticated session.

**Alternate Flows:**

- **A3.1.** If an unauthenticated user attempts a protected operation, the system directs the user through the authentication flow before permitting that operation.

**Exception Flows:**

- **E2.1.** If the supplied login identity already belongs to an existing account, the system rejects account creation.
- **E3.1.** If login credentials are invalid, the system rejects authentication and displays a generic failure message without revealing whether a specific account exists.

**Postconditions:** A participant account may have been created, an authenticated session may have been established, or an existing authenticated session may have been ended, depending on the path completed.

**Related Requirements:** [`FR-ACC-001`](#fr-acc-001), [`FR-ACC-002`](#fr-acc-002), [`FR-ACC-003`](#fr-acc-003), [`FR-ACC-004`](#fr-acc-004), [`FR-ACC-005`](#fr-acc-005), [`FR-ACC-006`](#fr-acc-006), [`FR-ACC-007`](#fr-acc-007).

**Related Business Rules:** [`BR-AUT-001`](#br-aut-001), [`BR-AUT-004`](#br-aut-004).

<a id="uc-003-register-for-an-event"></a>
## 6.3 UC-003 — Register for an Event

**Goal:** Allow an authenticated participant to reserve a place in an eligible event.

**Primary Actor:** Participant.

**Preconditions:** The participant is authenticated and the selected event exists.

**Trigger:** The participant requests registration for an event.

**Main Flow:**

1. The participant views an event for which registration is available.
2. The participant requests to register.
3. The system verifies that the event is Published, has not started, has not been cancelled, and has available capacity.
4. The system verifies that the participant does not already hold a current registration for the event.
5. The system creates the registration.
6. The system provides clear confirmation that the participant is registered.

**Alternate Flows:**

- **A2.1.** An unauthenticated user attempting to register must authenticate before the protected registration operation can proceed.

**Exception Flows:**

- **E4.1.** If the participant already has a current registration for the event, the system refuses the duplicate registration and informs the participant.
- **E3.1.** If the event reaches capacity before registration completes, the system refuses the registration and informs the participant that no places are available.
- **E3.2.** If the event is Draft, Cancelled, past, or already started, the system refuses the registration and explains that registration is unavailable.

**Postconditions:** On success, one current registration associates the participant with the event and the event's remaining capacity reflects that registration. On failure, no registration is created.

**Related Requirements:** [`FR-REG-001`](#fr-reg-001), [`FR-REG-002`](#fr-reg-002), [`FR-REG-003`](#fr-reg-003), [`FR-REG-004`](#fr-reg-004), [`FR-REG-005`](#fr-reg-005), [`FR-ACC-006`](#fr-acc-006), [`FR-ACC-007`](#fr-acc-007).

**Related Business Rules:** [`BR-REG-001`](#br-reg-001), [`BR-REG-002`](#br-reg-002), [`BR-REG-003`](#br-reg-003), [`BR-REG-004`](#br-reg-004).

<a id="uc-004-review-and-cancel-own-registrations"></a>
## 6.4 UC-004 — Review and Cancel Own Registrations

**Goal:** Allow an authenticated participant to review current registrations and cancel an eligible registration.

**Primary Actor:** Participant.

**Preconditions:** The participant is authenticated.

**Trigger:** The participant wants to review or cancel one of the participant's current registrations.

**Main Flow:**

1. The participant opens the participant registration list.
2. The system displays the participant's current event registrations.
3. The participant selects one of the participant's registrations for cancellation before the event begins.
4. The system verifies that the registration belongs to the participant and is eligible for cancellation.
5. The system cancels the registration.
6. The system removes that participant from the event's current registration count, making the place available to another eligible participant.
7. The system provides visible success or failure feedback for the cancellation operation.

**Alternate Flows:**

- **A1.1.** An unauthenticated user must authenticate before registration management can proceed.

**Exception Flows:**

- **E4.1.** The system refuses an attempt to cancel another participant's registration.
- **E4.2.** The system refuses cancellation when the event has already begun.

**Postconditions:** On successful cancellation, the participant no longer has the current registration and the released place is available for another eligible participant. Viewing registrations without cancelling does not change application data.

**Related Requirements:** [`FR-REG-006`](#fr-reg-006), [`FR-REG-007`](#fr-reg-007), [`FR-REG-008`](#fr-reg-008), [`FR-ACC-006`](#fr-acc-006), [`NFR-USA-004`](#nfr-usa-004).

**Related Business Rules:** [`BR-REG-005`](#br-reg-005), [`BR-REG-006`](#br-reg-006), [`BR-AUT-001`](#br-aut-001).

<a id="uc-005-create-and-manage-an-owned-event"></a>
## 6.5 UC-005 — Create and Manage an Owned Event

**Goal:** Allow an organizer to create and manage events owned by that organizer through the supported event lifecycle.

**Primary Actor:** Organizer.

**Preconditions:** The organizer is authenticated and has Organizer access.

**Trigger:** The organizer wants to create a new event or manage an existing owned event.

**Main Flow:**

1. The organizer opens the management list of events owned by that organizer.
2. The organizer creates a new event and supplies the required event information: title, description, category, location, start and end date/time, and participant capacity.
3. The system validates the event information and applicable business rules.
4. The system saves a newly created event as Draft.
5. The organizer may edit an owned event while it remains eligible for modification.
6. The organizer requests publication of an eligible Draft event.
7. The system verifies that the event is eligible for publication, including that its start date/time is in the future.
8. The system changes the event to Published and makes it available through normal public browsing and discovery while it remains upcoming.

**Alternate Flows:**

- **A5.1.** An organizer may cancel an owned Published event only before the event starts. The event record is preserved and its status becomes Cancelled.
- **A5.2.** A Draft event may be permanently deleted only when it has no current registrations.

**Exception Flows:**

- **E3.1 / E5.1.** If required event data is missing or violates applicable business rules, the system rejects creation or modification and provides validation feedback.
- **E5.2 / E6.1.** A Cancelled event cannot return to Published status and can no longer be edited or published by its organizer in the required version of the project.
- **E5.3.** The system refuses an attempt to lower the capacity of a Published event below its number of current registrations.
- **E1.1 / E5.4.** An organizer may not manage an event owned by another organizer unless the user also has Administrator access.

**Postconditions:** Depending on the completed action, an owned event is persisted as Draft, updated, Published, Cancelled, or—when eligible—deleted. Invalid or unauthorized changes are not applied.

**Related Requirements:** [`FR-EVT-001`](#fr-evt-001), [`FR-EVT-002`](#fr-evt-002), [`FR-EVT-003`](#fr-evt-003), [`FR-EVT-004`](#fr-evt-004), [`FR-EVT-005`](#fr-evt-005), [`FR-EVT-006`](#fr-evt-006), [`FR-EVT-007`](#fr-evt-007), [`FR-EVT-008`](#fr-evt-008), [`FR-EVT-009`](#fr-evt-009), [`FR-EVT-010`](#fr-evt-010), [`FR-EVT-011`](#fr-evt-011), [`FR-ACC-006`](#fr-acc-006).

**Related Business Rules:** [`BR-AUT-001`](#br-aut-001), [`BR-AUT-002`](#br-aut-002), [`BR-AUT-004`](#br-aut-004), [`BR-EVT-001`](#br-evt-001), [`BR-EVT-002`](#br-evt-002), [`BR-EVT-003`](#br-evt-003), [`BR-EVT-004`](#br-evt-004), [`BR-EVT-006`](#br-evt-006), [`BR-EVT-007`](#br-evt-007), [`BR-EVT-008`](#br-evt-008), [`BR-EVT-009`](#br-evt-009), [`BR-EVT-010`](#br-evt-010).

<a id="uc-006-review-registrations-for-an-owned-event"></a>
## 6.6 UC-006 — Review Registrations for an Owned Event

**Goal:** Allow an organizer to review the current participants and capacity status for an event owned by that organizer.

**Primary Actor:** Organizer.

**Preconditions:** The organizer is authenticated, has Organizer access, and the selected event is owned by that organizer unless the user also has Administrator access.

**Trigger:** The organizer wants to inspect registrations for an owned event.

**Main Flow:**

1. The organizer selects an owned event from the organizer's management interface.
2. The organizer opens the registration view for that event.
3. The system displays the current participants registered for the event.
4. The system displays the current registration count and remaining capacity.

**Alternate Flows:**

- None.

**Exception Flows:**

- **E1.1.** If an organizer attempts to access registration management for an event owned by another organizer, the system refuses the unauthorized operation unless the user also has Administrator access.

**Postconditions:** The organizer has reviewed the event's current registration information; no registration data is changed by this use case.

**Related Requirements:** [`FR-REG-009`](#fr-reg-009), [`FR-REG-010`](#fr-reg-010), [`FR-ACC-006`](#fr-acc-006).

**Related Business Rules:** [`BR-AUT-001`](#br-aut-001), [`BR-AUT-002`](#br-aut-002), [`BR-AUT-004`](#br-aut-004).

<a id="uc-007-manage-categories-and-organizer-access"></a>
## 6.7 UC-007 — Manage Categories and Organizer Access

**Goal:** Allow an administrator to maintain event categories and grant or remove Organizer access for existing users.

**Primary Actor:** Administrator.

**Preconditions:** The administrator is authenticated and has Administrator access.

**Trigger:** The administrator wants to change organizer permissions or maintain event categories.

**Main Flow:**

1. The administrator accesses an administrative function.
2. The administrator may grant or remove Organizer role access for an existing user.
3. The administrator may create a new event category.
4. The administrator may edit the name of an existing category when the resulting name is valid and unique without regard to letter case.
5. The administrator may delete a category when no event currently references that category.
6. The system persists each valid administrative change.

**Alternate Flows:**

- None.

**Exception Flows:**

- **E1.1.** The system refuses administrative functions for a user without Administrator access.
- **E3.1 / E4.1.** The system refuses a category name that would violate the category-name uniqueness rule.
- **E5.1.** If one or more events reference a category, the system refuses deletion and explains that the category is in use.

**Postconditions:** Valid organizer-role or category changes are persisted; invalid or unauthorized changes are not applied.

**Related Requirements:** [`FR-ADM-001`](#fr-adm-001), [`FR-ADM-002`](#fr-adm-002), [`FR-ADM-005`](#fr-adm-005), [`FR-ADM-006`](#fr-adm-006), [`FR-ADM-007`](#fr-adm-007).

**Related Business Rules:** [`BR-AUT-003`](#br-aut-003), [`BR-AUT-004`](#br-aut-004), [`BR-CAT-001`](#br-cat-001), [`BR-CAT-002`](#br-cat-002).

<a id="uc-008-perform-administrative-event-oversight"></a>
## 6.8 UC-008 — Perform Administrative Event Oversight

**Goal:** Allow an administrator to review events application-wide and cancel any eligible Published event when administrative intervention is required.

**Primary Actor:** Administrator.

**Preconditions:** The administrator is authenticated and has Administrator access.

**Trigger:** The administrator needs application-wide visibility of events or needs to cancel an eligible event regardless of organizer.

**Main Flow:**

1. The administrator opens the event oversight list.
2. The system displays all events regardless of organizer or status.
3. The administrator selects a Published event that has not yet started.
4. The administrator requests cancellation of the event.
5. The system verifies that the event is still eligible for cancellation.
6. The system cancels the event while preserving the event record and its Cancelled status.

**Alternate Flows:**

- None.

**Exception Flows:**

- **E5.1.** The system refuses event cancellation when the event is not an eligible Published event or has already started.
- **E1.1.** The system refuses administrative oversight operations for a user without Administrator access.
- **E6.1.** A Cancelled event does not return to Published status in the required version of the project.

**Postconditions:** On successful cancellation, the event remains persisted with Cancelled status and is no longer available for new registration. Viewing the oversight list without cancelling an event does not change application data.

**Related Requirements:** [`FR-ADM-001`](#fr-adm-001), [`FR-ADM-003`](#fr-adm-003), [`FR-ADM-004`](#fr-adm-004), [`FR-EVT-010`](#fr-evt-010).

**Related Business Rules:** [`BR-AUT-003`](#br-aut-003), [`BR-AUT-004`](#br-aut-004), [`BR-EVT-006`](#br-evt-006), [`BR-EVT-007`](#br-evt-007).


---

<a id="functional-requirements"></a>
# 7. Functional Requirements

<a id="accounts-and-authentication"></a>
## 7.1 Accounts and Authentication

| ID | Title | Requirement |
|---|---|---|
| <a id="fr-acc-001"></a>[**FR&#8209;ACC&#8209;001**](#fr-acc-001) | Account&nbsp;Registration | The system shall allow a visitor to create a participant account using the required account information. |
| <a id="fr-acc-002"></a>[**FR&#8209;ACC&#8209;002**](#fr-acc-002) | Unique&nbsp;Account&nbsp;Identity | The system shall reject creation of an account when the supplied login identity is already associated with an existing account. |
| <a id="fr-acc-003"></a>[**FR&#8209;ACC&#8209;003**](#fr-acc-003) | Login | The system shall allow a registered user to authenticate using valid credentials. |
| <a id="fr-acc-004"></a>[**FR&#8209;ACC&#8209;004**](#fr-acc-004) | Invalid Login | The system shall reject invalid login attempts without revealing whether a specific account exists. |
| <a id="fr-acc-005"></a>[**FR&#8209;ACC&#8209;005**](#fr-acc-005) | Logout | The system shall allow an authenticated user to end the current authenticated session. |
| <a id="fr-acc-006"></a>[**FR&#8209;ACC&#8209;006**](#fr-acc-006) | Protected Operations | The system shall require authentication before a user can register for an event, cancel a registration, or use organizer or administrator capabilities. |
| <a id="fr-acc-007"></a>[**FR&#8209;ACC&#8209;007**](#fr-acc-007) | Login Return Flow | When a visitor attempts to access an authentication-required page, the system shall provide a login flow that allows the user to return to the originally requested page after successful authentication when practical. |

<a id="event-discovery-search-and-browsing"></a>
## 7.2 Event Discovery, Search, and Browsing

| ID | Title | Requirement |
|---|---|---|
| <a id="fr-srh-001"></a>[**FR&#8209;SRH&#8209;001**](#fr-srh-001) | Browse Upcoming Events | The system shall allow visitors and authenticated users to view a list of published upcoming events. |
| <a id="fr-srh-002"></a>[**FR&#8209;SRH&#8209;002**](#fr-srh-002) | Event Ordering | The system shall present upcoming events in chronological order by event start date and time unless the user applies another supported search or filter. |
| <a id="fr-srh-003"></a>[**FR&#8209;SRH&#8209;003**](#fr-srh-003) | Event Details | The system shall allow users to open a detail page for a published event. |
| <a id="fr-srh-004"></a>[**FR&#8209;SRH&#8209;004**](#fr-srh-004) | Event Detail Information | The event detail page shall display the event title, description, category, location, start date and time, end date and time, organizer name, capacity information, and current registration availability. |
| <a id="fr-srh-005"></a>[**FR&#8209;SRH&#8209;005**](#fr-srh-005) | Search Events | The system shall allow users to search published upcoming events using text that matches at least the event title or location. |
| <a id="fr-srh-006"></a>[**FR&#8209;SRH&#8209;006**](#fr-srh-006) | Filter by Category | The system shall allow users to filter published upcoming events by category. |
| <a id="fr-srh-007"></a>[**FR&#8209;SRH&#8209;007**](#fr-srh-007) | No Results Feedback | When a search or filter produces no matching events, the system shall display a clear no&#8209;results message rather than an empty unexplained page. |
| <a id="fr-srh-008"></a>[**FR&#8209;SRH&#8209;008**](#fr-srh-008) | Cancelled Event Visibility | A previously published event that has been cancelled shall remain viewable by direct link and shall be clearly identified as cancelled, but it shall not appear as an available event for new registration. |
| <a id="fr-srh-009"></a>[**FR&#8209;SRH&#8209;009**](#fr-srh-009) | Paginated Event Browsing | The system shall divide public Published/upcoming Event browse, search, and Category-filter results into bounded pages. When applicable, it shall provide Previous and Next navigation, identify the current page and total matching result count, and preserve active search text, Category selection, and chronological ordering while navigating between pages. Submitting new search or Category criteria shall display the first page of the matching results. Invalid and out-of-range page requests shall follow the behavior defined in Section 11. |

<a id="event-management"></a>
## 7.3 Event Management

| ID | Title | Requirement |
|---|---|---|
| <a id="fr-evt-001"></a>[**FR&#8209;EVT&#8209;001**](#fr-evt-001) | Create Event | The system shall allow an organizer to create a new event owned by that organizer. |
| <a id="fr-evt-002"></a>[**FR&#8209;EVT&#8209;002**](#fr-evt-002) | Draft Creation | A newly created event shall initially be saved as a Draft event. |
| <a id="fr-evt-003"></a>[**FR&#8209;EVT&#8209;003**](#fr-evt-003) | Event Information | The system shall collect, at minimum, an event title, description, category, location, start date and time, end date and time, and participant capacity. |
| <a id="fr-evt-004"></a>[**FR&#8209;EVT&#8209;004**](#fr-evt-004) | Event Validation | The system shall reject event creation or modification when required event data is missing or violates applicable business rules. |
| <a id="fr-evt-005"></a>[**FR&#8209;EVT&#8209;005**](#fr-evt-005) | Organizer Event List | The system shall allow an organizer to view a management list of events owned by that organizer, including Draft, Published, and Cancelled events. |
| <a id="fr-evt-006"></a>[**FR&#8209;EVT&#8209;006**](#fr-evt-006) | Edit Owned Event | The system shall allow an organizer to edit an event owned by that organizer when the event is still eligible for modification under the business rules. |
| <a id="fr-evt-007"></a>[**FR&#8209;EVT&#8209;007**](#fr-evt-007) | Publish Event | The system shall allow an organizer to publish an eligible Draft event. |
| <a id="fr-evt-008"></a>[**FR&#8209;EVT&#8209;008**](#fr-evt-008) | Public Availability After Publication | After an event is published, the system shall make it available through normal event browsing and discovery when it is still upcoming. |
| <a id="fr-evt-009"></a>[**FR&#8209;EVT&#8209;009**](#fr-evt-009) | Cancel Owned Event | The system shall allow an organizer to cancel an owned Published event that has not yet started. |
| <a id="fr-evt-010"></a>[**FR&#8209;EVT&#8209;010**](#fr-evt-010) | Cancelled Event State | When an event is cancelled, the system shall preserve the event record and display its Cancelled status to users who access it. |
| <a id="fr-evt-011"></a>[**FR&#8209;EVT&#8209;011**](#fr-evt-011) | Delete Draft Event | The system shall allow an organizer to permanently delete an owned Draft event only when no current registrations exist for that event. |

<a id="event-registration"></a>
## 7.4 Event Registration

| ID | Title | Requirement |
|---|---|---|
| <a id="fr-reg-001"></a>[**FR&#8209;REG&#8209;001**](#fr-reg-001) | Register for Event | The system shall allow an authenticated participant to register for an eligible event that has available capacity. |
| <a id="fr-reg-002"></a>[**FR&#8209;REG&#8209;002**](#fr-reg-002) | Registration Confirmation | After a successful registration, the system shall provide clear confirmation that the participant is registered. |
| <a id="fr-reg-003"></a>[**FR&#8209;REG&#8209;003**](#fr-reg-003) | Prevent Duplicate Registration | The system shall prevent a participant from holding more than one current registration for the same event. |
| <a id="fr-reg-004"></a>[**FR&#8209;REG&#8209;004**](#fr-reg-004) | Enforce Event Capacity | The system shall prevent a registration from being created when the event has reached its participant capacity. |
| <a id="fr-reg-005"></a>[**FR&#8209;REG&#8209;005**](#fr-reg-005) | Registration Eligibility | The system shall prevent new registration for Draft, Cancelled, past, or already&#8209;started events. |
| <a id="fr-reg-006"></a>[**FR&#8209;REG&#8209;006**](#fr-reg-006) | View My Registrations | The system shall allow an authenticated participant to view the participant's current event registrations. |
| <a id="fr-reg-007"></a>[**FR&#8209;REG&#8209;007**](#fr-reg-007) | Cancel Own Registration | The system shall allow a participant to cancel the participant's own registration before the event begins. |
| <a id="fr-reg-008"></a>[**FR&#8209;REG&#8209;008**](#fr-reg-008) | Release Capacity | When a participant cancels a registration, the system shall make that place available for another eligible participant. |
| <a id="fr-reg-009"></a>[**FR&#8209;REG&#8209;009**](#fr-reg-009) | Organizer Registration List | The system shall allow an organizer to view the current participants registered for an event owned by that organizer. |
| <a id="fr-reg-010"></a>[**FR&#8209;REG&#8209;010**](#fr-reg-010) | Registration Count | The system shall display the current number of registrations and remaining capacity for an event to its organizer. |

<a id="administration"></a>
## 7.5 Administration

| ID | Title | Requirement |
|---|---|---|
| <a id="fr-adm-001"></a>[**FR&#8209;ADM&#8209;001**](#fr-adm-001) | Administrative Access | The system shall restrict administrative functions to authenticated administrators. |
| <a id="fr-adm-002"></a>[**FR&#8209;ADM&#8209;002**](#fr-adm-002) | Manage Organizer Access | The system shall allow an administrator to grant or remove Organizer role access for an existing user. |
| <a id="fr-adm-003"></a>[**FR&#8209;ADM&#8209;003**](#fr-adm-003) | View Event Oversight List | The system shall allow an administrator to view a management list of all events regardless of organizer or status. |
| <a id="fr-adm-004"></a>[**FR&#8209;ADM&#8209;004**](#fr-adm-004) | Cancel Any Eligible Event | The system shall allow an administrator to cancel any Published event that has not yet started. |
| <a id="fr-adm-005"></a>[**FR&#8209;ADM&#8209;005**](#fr-adm-005) | Create Category | The system shall allow an administrator to create an event category. |
| <a id="fr-adm-006"></a>[**FR&#8209;ADM&#8209;006**](#fr-adm-006) | Edit Category | The system shall allow an administrator to modify the name of an existing event category when the resulting name is valid and unique. |
| <a id="fr-adm-007"></a>[**FR&#8209;ADM&#8209;007**](#fr-adm-007) | Delete Unused Category | The system shall allow an administrator to delete a category only when no event currently references that category. |

---

<a id="data-requirements"></a>
# 8. Data Requirements

The system shall persist the information required to support the functional requirements.

<a id="required-event-information"></a>
## 8.1 Required Event Information

At minimum, the Event concept must support:

- unique identity;
- title;
- description;
- category;
- location;
- start date and time;
- end date and time;
- participant capacity;
- status: Draft, Published, or Cancelled;
- owning organizer;
- creation information sufficient for normal application management.

<a id="required-registration-information"></a>
## 8.2 Required Registration Information

At minimum, the Registration concept must support:

- unique identity or equivalent relational identity;
- participant;
- event;
- registration creation date and time.

The required system does not need a registration-history or waitlist model. Cancellation may remove the current registration rather than retaining an audit history.

<a id="data-integrity-expectations"></a>
## 8.3 Data Integrity Expectations

The data model shall support these integrity expectations:

- account login identities are unique according to the configured authentication system;
- category names are unique according to [`BR-CAT-001`](#br-cat-001);
- a participant cannot hold duplicate current registrations for one event;
- every event references an existing category;
- every event references an owning organizer;
- every registration references an existing user and event;
- event capacity is greater than zero;
- stored event dates follow the event timing rules.

---

<a id="external-interface-requirements"></a>
# 9. External Interface Requirements

<a id="user-interface"></a>
## 9.1 User Interface

The application shall provide server-rendered web pages sufficient to complete all required workflows.

The interface shall include, at minimum:

- application navigation appropriate to the current user's authentication state and role;
- an event browse page with search, Category filtering, current-page/result-count information, and pagination controls when applicable;
- an event detail page;
- account registration and login interfaces;
- a participant registration list;
- organizer event-management pages;
- event create and edit forms;
- an organizer registration view;
- administrator category-management pages;
- administrator user/organizer-management capability;
- clear success and error feedback after important actions.

The exact page layout and visual design are implementation choices provided the required behavior remains clear and usable.

<a id="browser-interface"></a>
## 9.2 Browser Interface

The system shall operate through standard HTTP/HTTPS browser interactions using server-rendered HTML. Client-side JavaScript may enhance usability but shall not be required to bypass server-side validation or authorization.

<a id="authentication-interface"></a>
## 9.3 Authentication Interface

The application shall provide web-based registration, login, logout, and access-control behavior through the course-approved ASP.NET authentication mechanism.

<a id="external-services"></a>
## 9.4 External Services

No external third-party service is required by this project.

---

<a id="non-functional-requirements"></a>
# 10. Non-Functional Requirements

<a id="security"></a>
## 10.1 Security

| ID | Requirement |
|---|---|
| <a id="nfr-sec-001"></a>[**NFR&#8209;SEC&#8209;001**](#nfr-sec-001) | The system shall enforce authorization on the server for every protected operation. |
| <a id="nfr-sec-002"></a>[**NFR&#8209;SEC&#8209;002**](#nfr-sec-002) | The system shall use the approved authentication framework for password storage and credential verification rather than storing plaintext passwords. |
| <a id="nfr-sec-003"></a>[**NFR&#8209;SEC&#8209;003**](#nfr-sec-003) | State&#8209;changing form requests shall use the framework's normal protection against cross&#8209;site request forgery where applicable. |
| <a id="nfr-sec-004"></a>[**NFR&#8209;SEC&#8209;004**](#nfr-sec-004) | The application shall not expose secret keys, connection credentials, or production passwords in source control. |
| <a id="nfr-sec-005"></a>[**NFR&#8209;SEC&#8209;005**](#nfr-sec-005) | User-visible unexpected-error responses shall not reveal stack traces, database connection details, or other sensitive server information. |

<a id="performance"></a>
## 10.2 Performance

| ID | Requirement |
|---|---|
| <a id="nfr-per-001"></a>[**NFR&#8209;PER&#8209;001**](#nfr-per-001) | With a representative classroom dataset of up to 500 events and 2,000 current registrations, normal event browse and detail pages shall complete within approximately two seconds under single&#8209;user instructor test conditions on the approved environment. |
| <a id="nfr-per-002"></a>[**NFR&#8209;PER&#8209;002**](#nfr-per-002) | Public Event search, Category filtering, and pagination shall be performed by server-side database queries before Event rows are materialized. The server shall retrieve only the requested page's matching Event rows rather than the entire matching result set, and the browser shall not be required to load the complete result set into client memory. |

<a id="usability"></a>
## 10.3 Usability

| ID | Requirement |
|---|---|
| <a id="nfr-usa-001"></a>[**NFR&#8209;USA&#8209;001**](#nfr-usa-001) | Navigation shall clearly indicate the primary actions available to the current user. |
| <a id="nfr-usa-002"></a>[**NFR&#8209;USA&#8209;002**](#nfr-usa-002) | Forms shall display understandable validation feedback and preserve valid user-entered values when a submission fails validation. |
| <a id="nfr-usa-003"></a>[**NFR&#8209;USA&#8209;003**](#nfr-usa-003) | Destructive actions such as cancelling an event or deleting a Draft event shall require an explicit user action that makes the destructive nature of the operation clear. |
| <a id="nfr-usa-004"></a>[**NFR&#8209;USA&#8209;004**](#nfr-usa-004) | Important operations such as event publication, event registration, registration cancellation, and event cancellation shall provide visible success or failure feedback. |

<a id="accessibility"></a>
## 10.4 Accessibility

| ID | Requirement |
|---|---|
| <a id="nfr-acc-001"></a>[**NFR&#8209;ACC&#8209;001**](#nfr-acc-001) | Form controls shall have programmatically associated labels or equivalent accessible names. |
| <a id="nfr-acc-002"></a>[**NFR&#8209;ACC&#8209;002**](#nfr-acc-002) | Core application workflows, including public Event pagination controls, shall be operable using a keyboard without requiring a mouse&#8209;only interaction. |
| <a id="nfr-acc-003"></a>[**NFR&#8209;ACC&#8209;003**](#nfr-acc-003) | Pages shall use meaningful document headings and semantic HTML appropriate to the content, including understandable public Event page-navigation state. |

<a id="reliability-and-data-correctness"></a>
## 10.5 Reliability and Data Correctness

| ID | Requirement |
|---|---|
| <a id="nfr-rel-001"></a>[**NFR&#8209;REL&#8209;001**](#nfr-rel-001) | Successfully completed create, update, registration, and cancellation operations shall remain reflected after the user refreshes the page or begins a new session. |
| <a id="nfr-rel-002"></a>[**NFR&#8209;REL&#8209;002**](#nfr-rel-002) | Validation or authorization failures shall not partially apply the requested data change. |
| <a id="nfr-rel-003"></a>[**NFR&#8209;REL&#8209;003**](#nfr-rel-003) | The saved application state shall not contain more current registrations for an event than the event's configured capacity as a result of normal application workflows. |

<a id="compatibility"></a>
## 10.6 Compatibility

| ID | Requirement |
|---|---|
| <a id="nfr-com-001"></a>[**NFR&#8209;COM&#8209;001**](#nfr-com-001) | The application shall support the current stable versions used in the course of at least two major modern desktop browsers, such as Chrome, Edge, or Firefox. |
| <a id="nfr-com-002"></a>[**NFR&#8209;COM&#8209;002**](#nfr-com-002) | Core pages shall remain usable on common laptop and mobile-width browser viewports without horizontal scrolling for normal page content. |

---

<a id="error-handling-and-exception-behavior"></a>
# 11. Error Handling and Exception Behavior

The application shall respond to expected problems in a manner that keeps the user informed and preserves valid data.

| Condition | Expected System Behavior |
|---|---|
| Invalid account or event form | Reject the submission, redisplay the form, identify validation problems, and preserve valid entered values when practical. |
| Invalid login | Reject authentication and display a generic authentication failure message. |
| Unauthenticated access to a protected page | Direct the user through the authentication flow and return the user to the requested destination when practical. |
| Authenticated but unauthorized operation | Refuse the operation and display an appropriate access-denied response without changing protected data. |
| Event or other requested resource does not exist | Display an appropriate not-found response rather than a generic unhandled exception. |
| Missing, invalid, or out-of-range public Event results-page number | Use the first results page when the page number is missing, malformed, zero, or negative. For a request beyond the last available page, display the last page. If no Events match the criteria, display the no-results message without pagination links. Preserve applicable search and Category criteria. |
| Duplicate event registration | Refuse the duplicate registration and inform the participant that a current registration already exists. |
| Event reaches capacity before registration completes | Refuse the registration and inform the participant that no places are available. |
| Attempt to register for Draft, Cancelled, or started event | Refuse the registration and explain that registration is unavailable. |
| Attempt to lower event capacity below current registrations | Refuse the update and explain why the capacity cannot be reduced to that value. |
| Attempt to delete a category currently used by events | Refuse the deletion and explain that the category is in use. |
| Unexpected server failure | Preserve already committed valid data, avoid exposing sensitive technical details to the user, and display a general failure message or error page. |

---

<a id="acceptance-criteria-definition-of-done"></a>
# 12. Acceptance Criteria / Definition of Done

Project #1 is considered complete when the application satisfies the required scope and demonstrates a coherent end-to-end implementation.

| ID | Acceptance Criterion |
|---|---|
| <a id="ac-001"></a>[**AC&#8209;001**](#ac-001) | Visitors can browse, search, filter, navigate paginated results while retaining active search/Category criteria and chronological ordering, and view published upcoming events according to [`FR-SRH-*`](#event-discovery-search-and-browsing). First/last-page boundaries and no-results behavior work as specified. |
| <a id="ac-002"></a>[**AC&#8209;002**](#ac-002) | Users can register, log in, log out, and are prevented from using protected capabilities without authentication according to [`FR-ACC-*`](#accounts-and-authentication). |
| <a id="ac-003"></a>[**AC&#8209;003**](#ac-003) | Organizers can create, validate, edit, publish, cancel, and when eligible delete owned events according to [`FR-EVT-*`](#event-management) and the applicable business rules. |
| <a id="ac-004"></a>[**AC&#8209;004**](#ac-004) | Participants can register for eligible events, view their registrations, and cancel their own registrations according to [`FR-REG-*`](#event-registration). |
| <a id="ac-005"></a>[**AC&#8209;005**](#ac-005) | Capacity, duplicate&#8209;registration, event&#8209;status, ownership, and category rules are enforced by the server and persisted data remains valid. |
| <a id="ac-006"></a>[**AC&#8209;006**](#ac-006) | Organizers can view current registrations for owned events without gaining management access to events owned by other organizers. |
| <a id="ac-007"></a>[**AC&#8209;007**](#ac-007) | Administrators can manage organizer access, event categories, and application&#8209;wide event cancellation according to [`FR-ADM-*`](#administration). |
| <a id="ac-008"></a>[**AC&#8209;008**](#ac-008) | Application data persists in the relational database and remains available after page refresh and normal application restart or redeployment procedures supported by the course environment. |
| <a id="ac-009"></a>[**AC&#8209;009**](#ac-009) | Important validation, authentication, authorization, and business&#8209;rule failures produce controlled user&#8209;visible feedback rather than unhandled errors. |
| <a id="ac-010"></a>[**AC&#8209;010**](#ac-010) | Automated tests cover a representative set of critical business behaviors, including at least registration eligibility/capacity and one authorization&#8209;sensitive workflow. |
| <a id="ac-011"></a>[**AC&#8209;011**](#ac-011) | The application satisfies the required security, usability, accessibility, reliability, and compatibility expectations defined in Section 10. |
| <a id="ac-012"></a>[**AC&#8209;012**](#ac-012) | The application is deployed to an instructor&#8209;approved environment and its required end&#8209;to&#8209;end workflows can be demonstrated there. |
| <a id="ac-013"></a>[**AC&#8209;013**](#ac-013) | The repository includes concise setup documentation describing prerequisites, configuration, database setup/migration steps, and how to run the application and tests. |

---

<a id="requirements-traceability-matrix"></a>
# 13. Requirements Traceability Matrix

The traceability matrix may be completed progressively as the project is implemented. It connects a requirement to the application parts and tests that realize it, allowing students (and the instructors) to verify that requirements have not been lost during development.

| Requirement ID | Feature / Component / Artifact | Test(s) / Evidence | Status | Notes / Comments |
|---|---|---|---|---|
| [`FR-SRH-009`](#fr-srh-009) | Planned: public `EventsController.Index`, `EventSearchViewModel`, `Views/Events/Index.cshtml`, and pagination CSS | Lesson 3 multi-page and combined-filter scenarios; boundary/no-results checks; Lesson 9 regression and deployed smoke-test evidence | Planned — Lesson 3 | CR-003 (approved); applies to the public Event catalog only; does not add pagination to Organizer or Administrator management lists. |
| &nbsp; | &nbsp; | &nbsp; | &nbsp; | &nbsp; |
| &nbsp; | &nbsp; | &nbsp; | &nbsp; | &nbsp; |

---

<a id="requirements-to-course-concept-map"></a>
# 14. Requirements-to-Course Concept Map

This table is a teaching reference, not an additional set of requirements.

| Project Feature | Representative Requirements | Full-Stack Concepts Encountered |
|---|---|---|
| Browse and view events | [`FR-SRH-001`](#fr-srh-001)–[`FR-SRH-004`](#fr-srh-004) | MVC request flow, routing, querying, view models, server-rendered UI |
| Search and category filtering | [`FR-SRH-005`](#fr-srh-005)–[`FR-SRH-007`](#fr-srh-007) | query parameters, filtering, database queries, empty states |
| Paginated public Event results | [`FR-SRH-009`](#fr-srh-009) | GET page state, total matching counts, deterministic chronological ordering, database-side bounded queries, view-model page metadata, Razor page navigation, keyboard accessibility |
| Account and login flows | [`FR-ACC-*`](#accounts-and-authentication) | authentication, sessions/cookies, protected routes, security |
| Create and edit event | [`FR-EVT-001`](#fr-evt-001)–[`FR-EVT-006`](#fr-evt-006) | forms, model binding, validation, EF Core, relational data |
| Publish and cancel event | [`FR-EVT-007`](#fr-evt-007)–[`FR-EVT-010`](#fr-evt-010) | business rules, state transitions, authorization, error handling |
| Event registration | [`FR-REG-001`](#fr-reg-001)–[`FR-REG-008`](#fr-reg-008) | many-to-many relationships, validation, business logic, capacity rules |
| Organizer registration view | [`FR-REG-009`](#fr-reg-009)–[`FR-REG-010`](#fr-reg-010) | ownership authorization, relational querying, aggregate counts |
| Category administration | [`FR-ADM-005`](#fr-adm-005)–[`FR-ADM-007`](#fr-adm-007) | CRUD, referential integrity, administrative authorization |
| Deployment and final verification | [`AC-008`](#ac-008)–[`AC-013`](#ac-013) | environments, configuration, migrations, testing, production workflow |

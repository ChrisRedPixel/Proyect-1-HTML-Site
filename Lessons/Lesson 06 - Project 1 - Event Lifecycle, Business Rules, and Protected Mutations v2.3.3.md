# **Lesson 6 — Event Lifecycle, Business Rules, and Protected Mutations**

Revision: 2026.10.25.5

# Table of Contents

- [Review of Last Lesson](#review-of-last-lesson)
- [Learning Objectives](#learning-objectives)
- [SRS Requirements and Use Cases](#srs-requirements-and-use-cases)
- [Guided Feature Preview](#guided-feature-preview)
- [Guided Design and Coding](#guided-design-and-coding)
	- [Inspect the inherited project and preserve its contracts](#inspect-the-inherited-project-and-preserve-its-contracts)
	- [Add transient lifecycle feedback to the management view](#add-transient-lifecycle-feedback-to-the-management-view)
	- [Implement the guarded Publish command](#implement-the-guarded-publish-command)
	- [Observe the inherited catalog respond to publication](#observe-the-inherited-catalog-respond-to-publication)
	- [Implement cancellation without removing the Event](#implement-cancellation-without-removing-the-event)
	- [Preserve direct Details access for a Cancelled Event](#preserve-direct-details-access-for-a-cancelled-event)
	- [Add a read-only Draft-deletion confirmation](#add-a-read-only-draft-deletion-confirmation)
	- [Implement the guarded deletion POST](#implement-the-guarded-deletion-post)
	- [Evolve Edit eligibility to owned Draft and Published Events](#evolve-edit-eligibility-to-owned-draft-and-published-events)
	- [Integrate the lifecycle-aware management view](#integrate-the-lifecycle-aware-management-view)
	- [Inspect the end-to-end boundary and build](#inspect-the-end-to-end-boundary-and-build)
- [Guided Discussion](#guided-discussion)
	- [Lifecycle transitions as a domain contract](#lifecycle-transitions-as-a-domain-contract)
	- [Safe HTTP methods and the confirmation boundary](#safe-http-methods-and-the-confirmation-boundary)
	- [Server authority across layered checks](#server-authority-across-layered-checks)
	- [Time is an input to a business decision](#time-is-an-input-to-a-business-decision)
	- [Cancellation, deletion, and discoverability](#cancellation-deletion-and-discoverability)
	- [Controlled feedback and expected failure paths](#controlled-feedback-and-expected-failure-paths)
	- [Data-dependent invariants and design alternatives](#data-dependent-invariants-and-design-alternatives)
- [Guided Verification and Validation](#guided-verification-and-validation)
	- [Automated](#automated)
		- [Adapt the inherited test expectations](#adapt-the-inherited-test-expectations)
		- [Add lifecycle HTTP and persistence tests](#add-lifecycle-http-and-persistence-tests)
		- [Execute the cumulative suite and interpret evidence](#execute-the-cumulative-suite-and-interpret-evidence)
	- [Manual](#manual)
	- [SRS Traceability Matrix Update with Evidence](#srs-traceability-matrix-update-with-evidence)
- [Guided Exercises](#guided-exercises)
	- [Exercise: model a transition table from evidence](#exercise-model-a-transition-table-from-evidence)
	- [Exercise: challenge the boundary-time rule](#exercise-challenge-the-boundary-time-rule)
	- [Exercise: test forged ownership across lifecycle commands](#exercise-test-forged-ownership-across-lifecycle-commands)
	- [Exercise: evaluate expected failures and feedback](#exercise-evaluate-expected-failures-and-feedback)
	- [Exercise: specify data-dependent invariants](#exercise-specify-data-dependent-invariants)
- [Troubleshooting Common Problems](#troubleshooting-common-problems)
- [Checking Your Understanding](#checking-your-understanding)
- [Summary](#summary)
- [Checkpoint](#checkpoint)
- [Next Lesson Preview](#next-lesson-preview)

# Review of Last Lesson

In Lesson 5, you separated untrusted form input from persisted domain entities, combined field, cross-field, and database-backed validation, and enforced authenticated role access together with resource ownership. You also practiced verifying both the HTTP result and the database state so a rejected request cannot silently change protected data.

CEMS now supports Organizer-only `/Events/Manage`, creation of server-owned Draft Events, and Edit of owned Draft Events using `EventInputModel`. The inherited application also provides public Published/upcoming browsing with search, Category filtering, selectable 10/25/50 page sizes, Event Details, Identity accounts, and SQL Server-backed automated tests. You will add guarded lifecycle commands and make owned Published Events editable while retaining these boundaries.

# Learning Objectives

By the end of this lesson, you should be able to:

1. **Model** a domain lifecycle as explicitly permitted transitions, guarded by state and contextual business conditions.
2. **Design** safe request contracts that distinguish read-only retrieval from state-changing commands and require appropriate forgery protection.
3. **Apply** layered authorization and current-state eligibility checks before modifying an owned resource.
4. **Analyze** time-dependent business rules and design reproducible boundary checks using a controllable clock.
5. **Compare** destructive deletion with state-preserving cancellation and evaluate their effects on subsequent retrieval.
6. **Design** controlled user feedback for successful operations, expected business-rule refusals, and unexpected failures.
7. **Verify** protected mutations through positive, negative, temporal, persistence, and HTTP-level evidence while preserving regression coverage.
8. **Identify** the persisted data and consistency boundaries required to enforce cross-record business invariants.
9. **Assess** incremental changes for accessibility, maintainability, and repository traceability.

# SRS Requirements and Use Cases

The approved CEMS SRS defines the Organizer workflow in **UC‑005 — Create and Manage an Owned Event**, alongside public discovery in **UC‑001**. Implement lifecycle commands within the existing Organizer‑owned Event workflow and preserve the public catalog contract.

| SRS reference | Required behavior / constraint | Lesson contribution |
| --- | --- | --- |
| `UC‑005`, `FR‑EVT‑007`, `BR‑EVT‑002` | An Organizer publishes an owned Draft only if its start is in the future | Guarded Publish POST updates persisted status |
| `FR‑EVT‑008`, `BR‑EVT‑005`, `FR‑SRH‑001‑002`, `FR‑SRH‑009` | Published/upcoming Events become discoverable through the existing ordered, paginated catalog | Preserve inherited public query and its 10/25/50 page‑size contract; test status‑dependent results |
| `FR‑EVT‑009`, `BR‑EVT‑006` | An Organizer may cancel an owned Published Event only before start | Guarded Cancel POST; exact‑boundary refusal |
| `FR‑EVT‑010`, `FR‑SRH‑008`, `BR‑EVT‑007` | Cancellation preserves the Event; direct Details shows Cancelled; normal browsing excludes it; republishing is disallowed | Preserve row and direct Details access without exposing Drafts |
| `FR‑EVT‑011`, `BR‑EVT‑008` | An owned Draft is deletable only with zero current Registrations | Confirmation GET and guarded Delete POST with owner/status checks; registration‑count requirement tracked as `PARTIAL` |
| `FR‑EVT‑004`, `FR‑EVT‑006`, `BR‑EVT‑001`, `BR‑EVT‑003‑004`, `BR‑EVT‑009‑010` | Validate eligible owned Event changes; refuse Cancelled edits and Published capacity reductions below current Registrations | Extend Edit to owned Draft and Published Events using existing validation; registration‑count capacity floor tracked as `PARTIAL` |
| `FR‑EVT‑005`, `FR‑ACC‑006`, `BR‑AUT‑001‑002`, `BR‑AUT‑004`, `NFR‑SEC‑001` | Authenticated Organizer sees and manages only owned Events; server authorizes all mutations | Preserve scoped Manage and authenticated‑ownership queries in lifecycle commands; account protection remains recurring |
| `NFR‑SEC‑003`, `NFR‑USA‑003‑004`, `NFR‑ACC‑001‑003` | Protect mutation requests, communicate destructive actions and result feedback, and provide accessible interaction | POST antiforgery, confirmation View, semantic controls, and transient feedback with browser checks |
| `NFR‑REL‑001‑002`, `AC‑003`, `AC‑005`, `AC‑009‑010` | Preserve valid persisted state; provide controlled failures and automated checks of critical behavior | Exercise lifecycle regression scenarios and record their HTTP and persistence outcomes |

**Lesson-level acceptance contract:** An authenticated Organizer can publish an owned future Draft, cancel an owned Published Event before its start, view a Cancelled Event by its known Details URL, confirm permanent deletion of an owned Draft with zero current Registrations, and edit owned Draft and Published Events. Each mutation checks authenticated ownership, persisted status, time-dependent eligibility where applicable, and antiforgery protection. Rejected requests preserve the existing Event state. The public catalog, account flows, Create validation, and 10/25/50 pagination continue to work.

# Guided Feature Preview

**Feature preview:** Sign in as an Organizer who owns a future Draft named *Campus Robotics Forum*. On **Manage My Events**, the Draft presents **Edit**, **Publish**, and **Delete Draft**. Submitting **Publish** updates its persisted state to `Published`, redirects to Manage with a success message, and makes the upcoming Event eligible for the existing public catalog. A visitor can find it through search or Category filtering and use the inherited pagination controls.

The same Organizer selects **Cancel Event** while the Published Event is still in the future. The application preserves the record with `Cancelled` status and displays feedback. The Event disappears from ordinary Published/upcoming search results, but its known `/Events/Details/{id}` address still returns a page prominently labeled **Cancelled**. Re-submitting Publish for the Cancelled Event produces controlled refusal feedback.

For another owned Draft with zero current Registrations, select **Delete Draft** to inspect its permanent-deletion warning. **Keep Event** returns to Manage with the record intact; **Permanently Delete Draft** submits the deletion POST. The server accepts deletion for an owned Draft and refuses requests for Published, Cancelled, or foreign-owned Events. The validated Edit form accepts owned Draft and Published Events and protects Cancelled Events from modification.

**Lifecycle at a glance:**

```text
Draft --Publish (owned, future)--> Published --Cancel (before start)--> Cancelled [terminal]
  |                                  |                                   |
  +--Edit                            +--Edit                             +--Details (direct link)
  +--Delete (confirmation + POST)

Public catalog: Published, upcoming, filtered and paginated
```

> **Screenshot placeholder — Organizer lifecycle controls**
>
> **Capture:** `/Events/Manage` with separate future Draft, future Published, and Cancelled Events.
>
> **Expected visible result:** Each row shows its status and context-appropriate links or POST actions; feedback appears after a redirect.
>
> **Application state:** Development environment, authorized Organizer, and owned example Events.
>
> ![Expected Organizer management list with lifecycle-specific action controls](images/lesson-06/organizer-lifecycle-controls.png)

Use the existing `Event` and `Category` data, authenticated Organizer identity, state transitions, and application-local clock for these scenarios.

# Guided Design and Coding

## Inspect the inherited project and preserve its contracts

Start from the accepted Lesson 5 checkpoint in the solution directory. Keep the terminal in the solution directory throughout the lesson. Create a dedicated branch before editing. Resolve any errors before editing the lifecycle.

```bat
git switch -c lesson-06-event-lifecycle
dotnet build
```

**Expected checkpoint:** The existing public pages, authentication, Organizer list, and Draft Create/Edit build and run successfully.

## Add transient lifecycle feedback to the management view

First, inject the established clock at the top of the existing management View, above `@model`, and capture a single `now` value in its Razor header block:

#### File: `CampusEvents/Views/Events/Manage.cshtml`

Add to the existing directives and header block:

```cshtml
@inject TimeProvider Clock

@{
	var now = Clock.GetLocalNow().DateTime;
}
```

A command that changes persistent state should redirect rather than simply rendering the POST response. Use `TempData` for a one-redirect message. Place the feedback markup after the existing page heading and before the empty-state/list branch.

```cshtml
@if (TempData["StatusMessage"] is string statusMessage)
{
	<p class="status-message" role="status">
		<strong>Success:</strong> @statusMessage
	</p>
}

@if (TempData["ErrorMessage"] is string errorMessage)
{
	<p class="error-message" role="alert">
		@errorMessage
	</p>
}
```

Use the success region after valid transitions and the error region for refused operations on an owned Event. For unknown or foreign-owned IDs, return `EventNotFound()` from the protected action.

#### File: `CampusEvents/wwwroot/css/site.css`

Append:

```css
.status-message, .error-message {
	padding: 0.75rem 1rem;
	margin-bottom: 1rem;
	border: 1px solid currentColor;
	border-radius: 0.35rem;
}

.event-actions {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 0.5rem;
}

.event-action-form {
	margin: 0;
}

.button-danger {
	display: inline-block;
	border: 1px solid currentColor;
	border-radius: 0.3rem;
	padding: 0.55rem 0.85rem;
	font: inherit;
	font-weight: 600;
	cursor: pointer;
}

.button-danger:focus-visible, .button-primary:focus-visible, .button-secondary:focus-visible {
	outline: 3px solid currentColor;
	outline-offset: 2px;
}

.confirmation-panel {
	max-width: 42rem;
	padding: 1rem;
	border: 1px solid currentColor;
	border-radius: 0.35rem;
}
```

The feedback regions render when a lifecycle command sets the matching `TempData` key.

## Implement the guarded Publish command

Publication is a state transition, not an arbitrary posted status update. Accept only an Event `id` from the route, resolve the authenticated Organizer, re-query a tracked persisted Event **owned by that user**, and check both status and future start before changing `Status`.

#### File: `CampusEvents/Controllers/EventsController.cs`

Add:

```csharp
[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Publish(int id)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id);
	if (eventItem is null) return EventNotFound();

	if (eventItem.Status != EventStatus.Draft)
	{
		TempData["ErrorMessage"] = "Only a Draft Event can be published.";
		return RedirectToAction(nameof(Manage));
	}

	var now = _timeProvider.GetLocalNow().DateTime;
	if (eventItem.StartDateTime <= now)
	{
		TempData["ErrorMessage"] = "A Draft Event can be published only before its start date and time.";
		return RedirectToAction(nameof(Manage));
	}

	eventItem.Status = EventStatus.Published;
	await _context.SaveChangesAsync();
	TempData["StatusMessage"] = $"{eventItem.Title} was published.";
	return RedirectToAction(nameof(Manage));
}
```

The route supplies an Event ID; the server resolves ownership and reads the persisted status. Handler returns `EventNotFound()` for foreign IDs and not-owners. The strict time guard refuses publication if the start is not in the future.

Now expose the request through a real POST form. The controller enforces the authorization and state guards on each request. The management View uses the injected `TimeProvider` to calculate `now` for its action controls.

#### File: `CampusEvents/Views/Events/Manage.cshtml`

Insert inside each owned Draft item:

```cshtml
@if (eventItem.Status == EventStatus.Draft && eventItem.StartDateTime > now)
{
	<form asp-controller="Events" asp-action="Publish" asp-route-id="@eventItem.Id" method="post"
		asp-antiforgery="true" class="event-action-form">
		<button type="submit" class="button-primary">Publish</button>
	</form>
}
```

**Expected checkpoint:** The newly Published upcoming Event becomes eligible in the catalog's normal result set.

## Implement cancellation without removing the Event

Cancellation changes the Event's state; it is not deletion. Keep the persisted record and permit the command only when the owned Event is currently `Published` and its start remains in the future.

#### File: `CampusEvents/Controllers/EventsController.cs`

Add:

```csharp
[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Cancel(int id)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id);
	if (eventItem is null) return EventNotFound();

	if (eventItem.Status != EventStatus.Published)
	{
		TempData["ErrorMessage"] = "Only a Published Event can be cancelled.";
		return RedirectToAction(nameof(Manage));
	}

	var now = _timeProvider.GetLocalNow().DateTime;
	if (eventItem.StartDateTime <= now)
	{
		TempData["ErrorMessage"] = "A Published Event can be cancelled only before it starts.";
		return RedirectToAction(nameof(Manage));
	}

	eventItem.Status = EventStatus.Cancelled;
	await _context.SaveChangesAsync();
	TempData["StatusMessage"] = $"{eventItem.Title} was cancelled.";
	return RedirectToAction(nameof(Manage));
}
```

#### File: `CampusEvents/Views/Events/Manage.cshtml`

Add the cancellation form inside the Published branch:

```cshtml
@if (eventItem.StartDateTime > now)
{
	<form asp-action="Cancel" asp-route-id="@eventItem.Id" method="post" asp-antiforgery="true" class="event-action-form">
		<button type="submit" class="button-danger">Cancel Event</button>
	</form>
}
```

**Expected checkpoint:** A valid future Published Event changes to Cancelled and remains in the Organizer list. A second Cancel request or an already-started Published Event receives controlled feedback and leaves the database unchanged. The Event record remains queryable by ID after cancellation, but the public catalog excludes its new Cancelled status, while still retaining the other eligible results.

## Preserve direct Details access for a Cancelled Event

Use distinct eligibility contracts for public catalog queries and direct Details URLs. The catalog lists only available upcoming Published Events; the SRS requires a **previously Published, now Cancelled** Event to remain accessible through its known link.

#### File: `CampusEvents/Controllers/EventsController.cs`

Replace only the existing Details action:

```csharp
[HttpGet]
public async Task<IActionResult> Details(int id)
{
	var eventItem = await _context.Events
		.AsNoTracking()
		.Include(item => item.Category)
		.Include(item => item.Organizer)
		.SingleOrDefaultAsync(item => item.Id == id &&
			(item.Status == EventStatus.Published || item.Status == EventStatus.Cancelled));

	if (eventItem is null) return EventNotFound();

	return View(eventItem);
}
```

#### File: `CampusEvents/Views/Events/Details.cshtml`

Insert near the existing heading and metadata:

```cshtml
@if (Model.Status == CampusEvents.Models.EventStatus.Cancelled)
{
	<p class="error-message" role="status">
		This Event has been cancelled.
	</p>
}
```

**Expected checkpoint:** `/Events/Details/{cancelledId}` returns HTTP 200 with a clearly visible Cancelled message; a Draft or missing ID still returns the inherited 404 view. A Cancelled Event remains absent from `/Events` even if it is future-dated.

## Add a read-only Draft-deletion confirmation

Deletion is the irreversible operation in this workflow. Separate the **safe confirmation GET** from the **mutating POST**. On GET, enforce Organizer access, ownership, and Draft status; use `AsNoTracking()` for this read-only representation.

#### File: `CampusEvents/Controllers/EventsController.cs`

Add:

```csharp
[HttpGet]
[Authorize(Roles = RoleNames.Organizer)]
public async Task<IActionResult> Delete(int id)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events
		.AsNoTracking()
		.Include(item => item.Category)
		.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id && item.Status == EventStatus.Draft);

	if (eventItem is null) return EventNotFound();

	return View(eventItem);
}
```

#### File: `CampusEvents/Views/Events/Delete.cshtml`

Create:

```cshtml
@model CampusEvents.Models.Event

@{
	ViewData["Title"] = "Delete Draft Event";
}

<h1>Delete Draft Event</h1>
<div class="confirmation-panel">
	<p>
		You are about to permanently delete this Draft Event.
		This action removes the Event record and cannot be undone here.
	</p>
	<dl class="event-meta">
		<div><dt>Title</dt><dd>@Model.Title</dd></div>
		<div><dt>Status</dt><dd>@Model.Status</dd></div>
		<div><dt>Category</dt><dd>@Model.Category.Name</dd></div>
		<div>
			<dt>Starts</dt>
			<dd><time datetime="@Model.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss")">
				@Model.StartDateTime.ToString("f")</time></dd>
		</div>
	</dl>
	<div class="form-actions">
		<form asp-controller="Events" asp-action="DeleteConfirmed"
			asp-route-id="@Model.Id" method="post" asp-antiforgery="true">
			<button type="submit" class="button-danger">
				Permanently Delete Draft
			</button>
		</form>
		<a asp-controller="Events" asp-action="Manage" class="button-secondary">Keep Event</a>
	</div>
</div>
```

#### File: `CampusEvents/Views/Events/Manage.cshtml`

Add the **Delete Draft** link inside the Draft branch:

```cshtml
<a asp-action="Delete" asp-route-id="@eventItem.Id" class="button-danger">Delete Draft</a>
```

> **Screenshot placeholder — Draft deletion confirmation**
>
> **Capture:** `/Events/Delete/{id}` for an owned Draft, before selecting either action.
>
> **Expected visible result:** Event title, Draft status, a permanent-deletion warning, a clearly labeled destructive button, and a Keep Event link.
>
> **Application state:** Development environment, authenticated owner, and an owned Draft Event.
>
> ![Expected Draft deletion confirmation with descriptive warning and Keep Event link](images/lesson-06/delete-draft-confirmation.png)

**Expected checkpoint:** The link opens a distinct confirmation page. Opening the page or choosing **Keep Event** preserves the Draft. A GET to `/Events/DeleteConfirmed/{id}` receives HTTP 405 (Method Not Allowed) with the inherited endpoint routing.

## Implement the guarded deletion POST

Re-query the Event on POST so the deletion decision uses current ownership and Draft status. Remove the Event only after these guards succeed.

#### File: `CampusEvents/Controllers/EventsController.cs`

Add:

```csharp
[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id);
	if (eventItem is null) return EventNotFound();

	if (eventItem.Status != EventStatus.Draft)
	{
		TempData["ErrorMessage"] = "Only a Draft Event can be permanently deleted.";
		return RedirectToAction(nameof(Manage));
	}

	_context.Events.Remove(eventItem);
	await _context.SaveChangesAsync();
	TempData["StatusMessage"] = $"{eventItem.Title} was permanently deleted.";
	return RedirectToAction(nameof(Manage));
}
```

**Expected checkpoint:** The confirmed POST removes an eligible owned Draft and redirects with feedback. Published, Cancelled, and foreign-owned Events are preserved.

## Evolve Edit eligibility to owned Draft and Published Events

Extend the inherited Draft-only Edit GET and POST actions to accept owned `Draft` or `Published` Events, with explicit refusal for `Cancelled`.

#### File: `CampusEvents/Controllers/EventsController.cs`

Replace the existing Edit GET:

```csharp
[HttpGet]
[Authorize(Roles = RoleNames.Organizer)]
public async Task<IActionResult> Edit(int id)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events
		.AsNoTracking()
		.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id);

	if (eventItem is null) return EventNotFound();

	if (eventItem.Status == EventStatus.Cancelled)
	{
		TempData["ErrorMessage"] = "A Cancelled Event can no longer be edited.";
		return RedirectToAction(nameof(Manage));
	}

	if (eventItem.Status != EventStatus.Draft && eventItem.Status != EventStatus.Published) return EventNotFound();

	var model = new EventInputModel
	{
		Title = eventItem.Title,
		Description = eventItem.Description,
		CategoryId = eventItem.CategoryId,
		Location = eventItem.Location,
		StartDateTime = eventItem.StartDateTime,
		EndDateTime = eventItem.EndDateTime,
		Capacity = eventItem.Capacity
	};

	await PopulateCategoryOptionsAsync(model);
	return View(model);
}
```

#### File: `CampusEvents/Controllers/EventsController.cs`

Replace the existing Edit POST:

```csharp
[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, EventInputModel model)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id);
	if (eventItem is null) return EventNotFound();

	if (eventItem.Status == EventStatus.Cancelled)
	{
		TempData["ErrorMessage"] = "A Cancelled Event can no longer be edited.";
		return RedirectToAction(nameof(Manage));
	}

	if (eventItem.Status != EventStatus.Draft && eventItem.Status != EventStatus.Published) return EventNotFound();

	await ValidateCategoryAsync(model);
	if (!ModelState.IsValid)
	{
		await PopulateCategoryOptionsAsync(model);
		return View(model);
	}

	// Apply validated editable fields to the persisted Event.
	eventItem.Title = model.Title.Trim();
	eventItem.Description = model.Description.Trim();
	eventItem.CategoryId = model.CategoryId!.Value;
	eventItem.Location = model.Location.Trim();
	eventItem.StartDateTime = model.StartDateTime!.Value;
	eventItem.EndDateTime = model.EndDateTime!.Value;
	eventItem.Capacity = model.Capacity!.Value;

	await _context.SaveChangesAsync();
	TempData["StatusMessage"] = $"{eventItem.Title} was updated.";
	return RedirectToAction(nameof(Manage));
}
```

#### File: `CampusEvents/Views/Events/Edit.cshtml`

Replace the Draft-only heading and paragraph:

```cshtml
<h1>Edit Event</h1>
<p>Use this form to update an owned Draft or Published Event.</p>
```

**Expected checkpoint:** An owned Published Event opens and saves through Edit, retaining its Published status and owner; an owned Draft edits; a Cancelled Event redirects with refusal feedback; a foreign Event returns not found. Inspect editable fields and server-managed fields in a fresh context.

## Integrate the lifecycle-aware management view

Consolidate the accumulated action controls into one `Manage.cshtml` View.

#### File: `CampusEvents/Views/Events/Manage.cshtml`

Final version:

```cshtml
@using CampusEvents.Models
@inject TimeProvider Clock
@model IReadOnlyList<Event>

@{
	ViewData["Title"] = "Manage Events";
	var now = Clock.GetLocalNow().DateTime;
}

<header class="page-heading">
	<div>
		<h1>Manage My Events</h1>
		<p>View and manage Events owned by your Organizer account.</p>
	</div>
	<a asp-controller="Events" asp-action="Create" class="button-primary">Create Event</a>
</header>

@if (TempData["StatusMessage"] is string statusMessage)
{
	<p class="status-message" role="status">
		<strong>Success:</strong> @statusMessage
	</p>
}

@if (TempData["ErrorMessage"] is string errorMessage)
{
	<p class="error-message" role="alert">@errorMessage</p>
}

@if (Model.Count == 0)
{
	<p role="status">You do not own any Events yet.</p>
}
else
{
	<div class="event-management-list">
		@foreach (var eventItem in Model)
		{
			<article class="event-management-item">
				<div>
					<h2>@eventItem.Title</h2>
					<dl class="event-meta">
						<div><dt>Status</dt><dd>@eventItem.Status</dd></div>
						<div><dt>Category</dt><dd>@eventItem.Category.Name</dd></div>
						<div>
							<dt>Starts</dt>
							<dd>
								<time datetime="@eventItem.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss")">
									@eventItem.StartDateTime.ToString("f")
								</time>
							</dd>
						</div>
						<div><dt>Capacity</dt><dd>@eventItem.Capacity</dd></div>
					</dl>
				</div>
				<div class="event-actions">
					@if (eventItem.Status == EventStatus.Draft)
					{
						<a asp-controller="Events" asp-action="Edit" asp-route-id="@eventItem.Id" class="button-secondary">Edit</a>
						@if (eventItem.StartDateTime > now)
						{
							<form asp-controller="Events" asp-action="Publish" asp-route-id="@eventItem.Id" method="post"
								asp-antiforgery="true" class="event-action-form">
								<button type="submit" class="button-primary">Publish</button>
							</form>
						}
						<a asp-controller="Events" asp-action="Delete" asp-route-id="@eventItem.Id" class="button-danger">Delete Draft</a>
					}
					else if (eventItem.Status == EventStatus.Published)
					{
						<a asp-action="Details" asp-route-id="@eventItem.Id" class="button-secondary">View Details</a>
						<a asp-controller="Events" asp-action="Edit" asp-route-id="@eventItem.Id" class="button-secondary">Edit</a>
						@if (eventItem.StartDateTime > now)
						{
							<form asp-controller="Events" asp-action="Cancel" asp-route-id="@eventItem.Id" method="post"
								asp-antiforgery="true" class="event-action-form">
								<button type="submit" class="button-danger">Cancel Event</button>
							</form>
						}
					}
					else if (eventItem.Status == EventStatus.Cancelled)
					{
						<a asp-action="Details" asp-route-id="@eventItem.Id" class="button-secondary">View Details</a>
					}
				</div>
			</article>
		}
	</div>
}
```

The View displays available actions using the shared clock; each POST independently rechecks the authenticated owner and the persisted Event state.

> **Screenshot placeholder — publication and cancellation feedback**
>
> **Capture:** `/Events/Manage` immediately after Publish or Cancel redirects, using a future Event owned by the Organizer.
>
> **Expected visible result:** Status changes and success feedback, with updated available actions and retained existing Event metadata.
>
> **Application state:** Authenticated Organizer, owned Event ID, and a future start time for the chosen transition.
>
> ![Expected redirected management page with lifecycle status and success feedback](images/lesson-06/lifecycle-feedback.png)

## Inspect the end-to-end boundary and build

The implementation should now have these action contracts:

```text
POST /Events/Publish/{id}					→ Publish(int id)
POST /Events/Cancel/{id}					→ Cancel(int id)
GET  /Events/Delete/{id}					→ Delete(int id) (read-only)
POST /Events/DeleteConfirmed/{id}	→ DeleteConfirmed(int id)
GET  /Events/Edit/{id}						→ Edit(int id) (Draft or Published)
POST /Events/Edit/{id}						→ Edit(int id, EventInputModel model)
GET  /Events/Details/{id}					→ Details(int id) (Published or Cancelled)
GET  /Events											→ Index(search, categoryId, page, pageSize)
```

Build and run it:

```bat
dotnet build
dotnet run --project src/CampusEvents
```

# Guided Discussion

## Lifecycle transitions as a domain contract

A CRUD vocabulary alone does not describe when a command is legal. **Create** and **Edit** communicate broad data operations, but **Publish** and **Cancel** communicate meaningful changes in the domain's obligations. A state transition specifies both the allowed origin/destination pair and additional guards such as an authenticated actor, ownership, and the Event's start time.

| Persisted state | Edit | Publish | Cancel | Delete |
| --- | --- | --- | --- | --- |
| Draft | Eligible owned Event | Only owned and future | Not allowed | Owned, with confirmation and the zero-current-registration condition |
| Published | Eligible owned Event, respecting the current-registration Capacity floor | Not allowed | Only owned and before start | Not allowed |
| Cancelled | Not allowed | Not allowed | Not allowed | Prohibited |

Read this matrix together with Organizer role and ownership requirements. The server stores each status and permits changes through guarded commands. The same modeling technique applies when a workflow gains additional transitions.

Use state diagrams to visualize transitions and matrices to derive accepted and rejected requests. In another system, analogous transitions could describe payment authorization, order fulfillment, document approval, or account suspension.

<br>

## Safe HTTP methods and the confirmation boundary

GET is intended for retrieving a representation without performing a requested state change. Publishing, cancelling, editing, and deleting are commands that alter persisted data and therefore use POST in this server-rendered application. A browser prefetch, crawler, refresh, or copied link must not accidentally trigger a destructive mutation. Each POST also passes authentication, role authorization, ownership, current-state guards, and antiforgery validation.

Deletion uses a **two-step** interaction because the consequence is irreversible: GET describes the record and consequence; POST performs the operation after explicit confirmation. At confirmation time, the POST re-evaluates the persisted Event before deletion. Compared with JavaScript confirmation dialogs, the server-rendered page remains functional without client scripting and supplies a stable, accessible opportunity to reconsider.

**Request/response trace (illustrative):**

```text
GET /Events/Delete/31 -> 200 confirmation; Event remains unchanged
POST /Events/DeleteConfirmed/31 + antiforgery token
  -> verify authenticated Organizer + owned current Draft
  -> DELETE once
  -> 302 /Events/Manage with short-lived success feedback
```

A rejected command preserves the persisted Event. A `404` protects the privacy of Event IDs outside the Organizer's accessible collection. A redirect with feedback is useful for a known, owned resource in an invalid state. Distinguishing those cases avoids both unnecessary information disclosure and opaque user feedback.

<br>

## Server authority across layered checks

The workflow uses separate decisions with different trust boundaries:

| Layer | Question | Why it matters |
| --- | --- | --- |
| Authentication | Is there a trustworthy signed-in user? | Browser-supplied user IDs are not a source of identity |
| Role authorization | Is this user allowed to enter the Organizer operation? | A signed-in Participant does not automatically acquire Organizer permissions |
| Ownership | Is this Event owned by the authenticated Organizer? | An Organizer cannot mutate another Organizer's row through a changed ID |
| Current-state eligibility | Is the persisted status and start time compatible with this command now? | An action presented earlier may have become stale |
| Data validation | Are all submitted editable fields valid? | An eligible owner cannot bypass business invariants with malformed values |
| Persistence | Did the expected row state change? | Query the saved record to verify the durable state transition |

Combining `Id` and `OrganizerId` in the persisted resource query makes ownership part of the retrieval contract. After loading an owned Event, the controller checks status and can return informative feedback. The tracked Event is modified and saved after its authorization, eligibility, and validation checks.

**Concurrency consideration:** Another request can update a row after it is read. When multiple operations compete over the same invariant, use a suitable concurrency token, database transaction, or coordinated locking strategy. For registration counts and Published Capacity, coordinate competing writes to the same Event.

<br>

## Time is an input to a business decision

The comparison `StartDateTime > now` is part of the product rule, not merely a display preference. A test at 11:59 and a test at 12:00 should not depend on how quickly the machine executes. An injected clock makes both outcomes reproducible and allows the View's optional affordances and the controller's authoritative eligibility checks to refer to the same application-local convention.

For applications spanning time zones, UTC instants and explicit zone conversions are useful alternatives. CEMS retains its established local `DateTime` persistence and `GetLocalNow().DateTime` convention. Record the fixed instant and test time zone alongside each boundary result.

<br>

## Cancellation, deletion, and discoverability

Cancelling and deleting have different effects on the identity of a resource. A Cancelled Event retains its ID, Organizer relationship, and Details representation and can continue to explain an earlier link or cancellation notice. Deletion removes the Draft row, so a Details URL for that record no longer resolves. Normal public browsing is a **filtered read model** and does not expose every persistent record; it naturally responds to the same row changing status.

Keeping the public catalog as a filtered view of the Event table avoids synchronizing duplicate status data. The filter separates public availability from the Event's continued existence. Separating those concepts is useful in product catalogs, document archives, and other systems where discoverability and retention differ.

<br>

## Controlled feedback and expected failure paths

Posting Cancel on a Draft is an expected invalid business action. Handle it through a deliberate check and clear feedback. Unexpected SQL connection failures, programming errors, and unhandled exceptions require logging and the application's normal error-handling pathway; they must not be converted into misleading success or swallowed silently.

`TempData` carries a single-redirect message; the Event row stores the current status. Use semantic `role="status"` for nonurgent success updates and `role="alert"` for refusal feedback, while considering screen-reader and keyboard behavior in real browser checks. A result object or domain service can coordinate rules shared across many workflows. Here the local controller actions keep the guards adjacent to each command; extract a shared abstraction when responsibilities or maintenance needs justify the added layer.

<br>

## Data-dependent invariants and design alternatives

Some invariants depend on relationships beyond the Event row. For `BR-EVT-008`, eligible Draft deletion requires zero current Registrations; for `BR-EVT-009`, a Published Capacity reduction requires the requested value to be at least the current Registration count. A persisted Registration relation supplies these counts. Coordinate concurrent registration, deletion, and capacity changes through an appropriate shared consistency boundary.

A state-machine package, command pipeline, or separate lifecycle controller could express transitions more formally. Compare those alternatives with the current compact controller actions in terms of cohesion, indirection, transactional boundaries, and maintenance cost.

<br>

# Guided Verification and Validation

Verify request handling, authorization, and database transitions against the specified contracts. Validate the resulting forms, messages, navigation, and accessibility through the Organizer workflow. Inspect both HTTP behavior and persisted state.

<br>

## Automated

The inherited `CampusEvents.Tests` project uses xUnit, `WebApplicationFactory<Program>`, Identity cookies, antiforgery tokens, `FakeTimeProvider`, and disposable migrated SQL Server databases through `IsolatedSqlServerDatabase`. Continue with this relational test fixture and an authorized SQL Server/LocalDB endpoint.

**Test-case inventory — new and retained scenarios:**

| Test / case | Behavior or boundary | Expected assertion / evidence |
| --- | --- | --- |
| New: `Publish_OwnedFutureDraft_UpdatesStateAndPublicDiscovery` | Valid Publish; selected page size and search; subsequent Cancel | Persisted Published then Cancelled; present then absent from public results; known Cancelled Details remains visible |
| New: `Publish_RejectedStatesOrTimes_NeverWrite` (4 parameterized cases) | Already Published, Cancelled, exact start, past start | Controlled refusal; unchanged persisted Event fields |
| New: `Cancel_RefusesIneligibleStateOrStart` (4 parameterized cases) | Draft, Cancelled, exact start, past start | Controlled refusal; no row deletion or status change |
| New: `Delete_ConfirmationGetIsSafe_AndPostDeletesDraft` | Confirmation GET and explicit POST | GET preserves the Draft; POST removes the selected owned Draft |
| New: `Delete_RefusesPublishedCancelledAndForeign` (3 parameterized cases) | Wrong status or changed foreign ID | Row survives; foreign ID is not found |
| New: `Edit_Published_ValidAndInvalidChanges` | Valid Published Edit and invalid Category edit | Allowed fields saved; owner/status/creation immutable; invalid POST preserves prior row |
| New: `Edit_CancelledIsRefused` | GET and POST on owned Cancelled Event | Controlled refusal; unchanged database state |
| New: `LifecyclePosts_RequireRoleAndAntiforgery` (4 parameterized cases) | Each of Publish, Cancel, DeleteConfirmed, and Edit: anonymous/Participant access and missing token | No mutation; correct authentication, authorization, and antiforgery result |
| New: `Lifecycle_ForeignOwnerCannotMutate` (2 parameterized cases) | Foreign owner tries both Publish and Cancel | Not found; unchanged foreign Event fields |
| Retained: `Manage_RealRoleAndOwnerScoping_IsEnforced` and Create/Create-invalid tests | Authentication, owner-scoped Manage, Draft creation, validation | Existing HTTP/database assertions remain intact |
| Updated inherited: `Edit_ForeignOrIneligibleEvent_DoesNotMutate` (2 parameterized cases) | Foreign-owner and Cancelled-state access | Foreign requests return 404; Cancelled GET/POST redirects preserve the existing row |
| Retained: public Index, Details, pagination, and authentication tests | Public filtering, order, 10/25/50 sizes, 404 behavior and identity | Run cumulative suite; preserve assertions not invalidated by new accepted behavior |

Each `[InlineData]` entry in a `[Theory]` is a separately discoverable case. Inspect test-runner discovery and execution output to record case totals and outcomes.

<br>

### Adapt the inherited test expectations

In `CampusEvents.Tests/OrganizerEventHttpTests.cs`, update `Edit_ForeignOrIneligibleEvent_DoesNotMutate` to cover `"foreign"` and `"cancelled"` cases. Assert HTTP 404 for foreign access and a redirect to `/Events/Manage` for Cancelled GET and POST requests, with unchanged persisted fields in both cases. In `Edit_Published_ValidAndInvalidChanges`, assert that a valid Published Edit persists the submitted editable fields while retaining server-managed values.

**Replace the entire existing method with:**

```csharp
[Theory]
[InlineData("foreign")]
[InlineData("cancelled")]
public async Task Edit_ForeignOrIneligibleEvent_DoesNotMutate(string kind)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, otherId) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, kind == "foreign" ? otherId : ownerId, "Protected Original Event",
		kind == "cancelled" ? EventStatus.Cancelled : EventStatus.Draft);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	var get = await browser.GetAsync($"/Events/Edit/{id}");
	if (kind == "foreign") Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
	else Assert.Equal("/Events/Manage", Location(get).AbsolutePath);

	var fields = ValidFields(category, await FormTokenAsync(browser, "/Events/Create"));
	fields["Title"] = "Forged Update";
	fields["OrganizerId"] = ownerId;
	fields["Status"] = "Draft";
	var post = await SendFormAsync(browser, $"/Events/Edit/{id}", fields);
	if (kind == "foreign") Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
	else Assert.Equal("/Events/Manage", Location(post).AbsolutePath);

	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
}
```

Keep the existing Draft-Edit success/invalid-input tests and the shared HTTP/database fixture helpers. Insert the new methods **inside `OrganizerEventHttpTests`**, immediately before the class closing brace.

<br>

### Add lifecycle HTTP and persistence tests

**File: `CampusEvents.Tests/OrganizerEventHttpTests.cs` — append inside the class**

Begin with two helpers: one changes the persisted start time and the other submits a lifecycle command with an antiforgery token from the signed-in session.

```csharp
private static async Task SetStartAsync(IsolatedSqlServerDatabase database, int id, DateTime start)
{
	await using var db = database.CreateContext();
	var item = await db.Events.SingleAsync(e => e.Id == id);
	item.StartDateTime = start;
	item.EndDateTime = start.AddHours(2);
	await db.SaveChangesAsync();
}

private static async Task<HttpResponseMessage> CommandAsync(HttpClient browser, string path)
{
	var token = await FormTokenAsync(browser, "/Events/Create");
	var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
	return await SendFormAsync(browser, path, fields);
}
```

**Publish and Cancel cases:** Verify both transitions and the exact-start boundaries using the shared fixed clock.

```csharp
[Fact]
public async Task Publish_OwnedFutureDraft_UpdatesStateAndPublicDiscovery()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, ownerId, "Future Robotics Forum", EventStatus.Draft);
	using var organizer = Browser(host);
	using var visitor = Browser(host);
	await SignInAsync(organizer, OwnerEmail);

	var query = $"/Events?search=Robotics&categoryId={category}&pageSize=25";
	var before = await visitor.GetAsync(query);
	Assert.Equal(HttpStatusCode.OK, before.StatusCode);
	Assert.DoesNotContain("Future Robotics Forum", await before.Content.ReadAsStringAsync());

	var publish = await CommandAsync(organizer, $"/Events/Publish/{id}");
	Assert.Equal("/Events/Manage", Location(publish).AbsolutePath);
	Assert.Equal(EventStatus.Published, (await ReadEventAsync(database, id)).Status);
	var published = await visitor.GetAsync(query);
	Assert.Equal(HttpStatusCode.OK, published.StatusCode);
	Assert.Contains("Future Robotics Forum", await published.Content.ReadAsStringAsync());

	var cancel = await CommandAsync(organizer, $"/Events/Cancel/{id}");
	Assert.Equal("/Events/Manage", Location(cancel).AbsolutePath);
	Assert.Equal(EventStatus.Cancelled, (await ReadEventAsync(database, id)).Status);
	var absent = await visitor.GetAsync(query);
	Assert.Equal(HttpStatusCode.OK, absent.StatusCode);
	Assert.DoesNotContain("Future Robotics Forum", await absent.Content.ReadAsStringAsync());
	var details = await visitor.GetAsync($"/Events/Details/{id}");
	Assert.Equal(HttpStatusCode.OK, details.StatusCode);
	Assert.Contains("This Event has been cancelled", await details.Content.ReadAsStringAsync());
}

[Theory]
[InlineData("published")]
[InlineData("cancelled")]
[InlineData("at-start")]
[InlineData("past-start")]
public async Task Publish_RejectedStatesOrTimes_NeverWrite(string reason)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	var clock = FixedClock();
	using var host = CreateHost(database, clock);
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var status = reason switch
	{
		"published" => EventStatus.Published,
		"cancelled" => EventStatus.Cancelled,
		_ => EventStatus.Draft
	};
	var id = await EventAsync(database, category, ownerId, "Guarded Event", status);
	var now = clock.GetLocalNow().DateTime;
	if (reason == "at-start") await SetStartAsync(database, id, now);
	if (reason == "past-start") await SetStartAsync(database, id, now.AddMinutes(-1));
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	var response = await CommandAsync(browser, $"/Events/Publish/{id}");
	Assert.Equal("/Events/Manage", Location(response).AbsolutePath);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
	var feedback = await browser.GetAsync("/Events/Manage");
	Assert.Contains("error-message", await feedback.Content.ReadAsStringAsync());
}

[Theory]
[InlineData("draft")]
[InlineData("cancelled")]
[InlineData("at-start")]
[InlineData("past-start")]
public async Task Cancel_RefusesIneligibleStateOrStart(string reason)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	var clock = FixedClock();
	using var host = CreateHost(database, clock);
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var status = reason switch
	{
		"draft" => EventStatus.Draft,
		"cancelled" => EventStatus.Cancelled,
		_ => EventStatus.Published
	};
	var id = await EventAsync(database, category, ownerId, "Guarded Cancel", status);
	var now = clock.GetLocalNow().DateTime;
	if (reason == "at-start") await SetStartAsync(database, id, now);
	if (reason == "past-start") await SetStartAsync(database, id, now.AddMinutes(-1));
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	Assert.Equal("/Events/Manage", Location(await CommandAsync(browser, $"/Events/Cancel/{id}")).AbsolutePath);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
	var feedback = await browser.GetAsync("/Events/Manage");
	Assert.Contains("error-message", await feedback.Content.ReadAsStringAsync());
}
```

**Delete cases:** Exercise the safe confirmation GET, explicit deletion POST, invalid states, and foreign ownership.

```csharp
[Fact]
public async Task Delete_ConfirmationGetIsSafe_AndPostDeletesDraft()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, ownerId, "Disposable Draft", EventStatus.Draft);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	var confirmation = await browser.GetAsync($"/Events/Delete/{id}");
	Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
	Assert.Contains("Permanently Delete Draft", await confirmation.Content.ReadAsStringAsync());
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
	var safeGet = await browser.GetAsync($"/Events/DeleteConfirmed/{id}");
	Assert.Equal(HttpStatusCode.MethodNotAllowed, safeGet.StatusCode);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));

	Assert.Equal("/Events/Manage", Location(await CommandAsync(browser, $"/Events/DeleteConfirmed/{id}")).AbsolutePath);
	await using var verify = database.CreateContext();
	Assert.False(await verify.Events.AnyAsync(e => e.Id == id));
}

[Theory]
[InlineData("published")]
[InlineData("cancelled")]
[InlineData("foreign")]
public async Task Delete_RefusesPublishedCancelledAndForeign(string reason)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, otherId) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, reason == "foreign" ? otherId : ownerId,
		"Retained Event", reason == "published" ? EventStatus.Published :
		reason == "cancelled" ? EventStatus.Cancelled : EventStatus.Draft);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync($"/Events/Delete/{id}")).StatusCode);
	var response = await CommandAsync(browser, $"/Events/DeleteConfirmed/{id}");
	if (reason == "foreign") Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	else Assert.Equal("/Events/Manage", Location(response).AbsolutePath);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
}
```

**Edit and access-control cases:** Verify field allowlisting, invalid form behavior, role boundaries, and antiforgery enforcement.

```csharp
[Fact]
public async Task Edit_Published_ValidAndInvalidChanges()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, otherId) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, ownerId, "Original Published", EventStatus.Published);
	var created = (await ReadEventAsync(database, id)).CreatedAt;
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);
	var token = await FormTokenAsync(browser, $"/Events/Edit/{id}");
	var fields = ValidFields(category, token);
	fields["Title"] = "Revised Published Event";
	fields["OrganizerId"] = otherId;
	fields["Status"] = "Cancelled";
	fields["CreatedAt"] = "2000-01-01T00:00:00";

	Assert.Equal("/Events/Manage", Location(await SendFormAsync(browser, $"/Events/Edit/{id}", fields)).AbsolutePath);
	var saved = await ReadEventAsync(database, id);
	Assert.Equal("Revised Published Event", saved.Title);
	Assert.Equal("Hands-on lab", saved.Description);
	Assert.Equal(category, saved.CategoryId);
	Assert.Equal("Engineering Hall", saved.Location);
	Assert.Equal(new DateTime(2026, 5, 8, 10, 0, 0), saved.StartDateTime);
	Assert.Equal(new DateTime(2026, 5, 8, 12, 0, 0), saved.EndDateTime);
	Assert.Equal(40, saved.Capacity);
	Assert.Equal(ownerId, saved.OrganizerId);
	Assert.Equal(EventStatus.Published, saved.Status);
	Assert.Equal(created, saved.CreatedAt);

	fields["CategoryId"] = "999999";
	fields["Title"] = "Not Saved";
	var invalid = await SendFormAsync(browser, $"/Events/Edit/{id}", fields);
	Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
	var html = await invalid.Content.ReadAsStringAsync();
	Assert.Contains("Select an existing Category", html);
	Assert.Equal(EventFields(saved), EventFields(await ReadEventAsync(database, id)));
}

[Fact]
public async Task Edit_CancelledIsRefused()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, ownerId, "Frozen Cancelled", EventStatus.Cancelled);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	Assert.Equal("/Events/Manage", Location(await browser.GetAsync($"/Events/Edit/{id}")).AbsolutePath);
	var fields = ValidFields(category, await FormTokenAsync(browser, "/Events/Create"));
	Assert.Equal("/Events/Manage", Location(await SendFormAsync(browser, $"/Events/Edit/{id}", fields)).AbsolutePath);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
}

[Theory]
[InlineData("Publish")]
[InlineData("Cancel")]
[InlineData("DeleteConfirmed")]
[InlineData("Edit")]
public async Task LifecyclePosts_RequireRoleAndAntiforgery(string action)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, ownerId, "Protected Draft", EventStatus.Draft);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	var path = $"/Events/{action}/{id}";

	using var anonymous = Browser(host);
	var anonymousResponse = await anonymous.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()));
	Assert.Equal("/Account/Login", Location(anonymousResponse).AbsolutePath);

	using var participant = Browser(host);
	await SignInAsync(participant, ParticipantEmail);
	var participantToken = await FormTokenAsync(participant, "/Participant");
	var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = participantToken };
	var forbidden = await SendFormAsync(participant, path, fields);
	Assert.Equal("/Account/AccessDenied", Location(forbidden).AbsolutePath);

	using var organizer = Browser(host);
	await SignInAsync(organizer, OwnerEmail);
	var noToken = await organizer.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()));
	Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
}

[Theory]
[InlineData("Publish")]
[InlineData("Cancel")]
public async Task Lifecycle_ForeignOwnerCannotMutate(string action)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (_, otherId) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var status = action == "Publish" ? EventStatus.Draft : EventStatus.Published;
	var id = await EventAsync(database, category, otherId, "Other Organizer Event", status);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	var result = await CommandAsync(browser, $"/Events/{action}/{id}");
	Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
}
```

Each case provisions a disposable migrated SQL Server database and Identity accounts. `FixedClock()` supplies `2026-04-15 12:00:00` in a UTC−04 zone; `SetStartAsync()` sets the Event's start and end to exercise equality and past-start boundaries. The signed-in Participant gets a real antiforgery token from the shared layout's Logout form on `/Participant`. This isolates role authorization from token validation. Keep the inherited test-host authentication, routing, and antiforgery configuration. The foreign-owner theory checks Publish and Cancel; the Delete and Edit scenarios also exercise ownership restrictions.

<br>

### Execute the cumulative suite and interpret evidence

Use the solution directory and the inherited authorized SQL Server LocalDB instance `(localdb)\MSSQLLocalDB`, or the supported `CEMS_TEST_SQLSERVER` override that targets a disposable test instance. The fixture creates and migrates an isolated database for each test and owns cleanup. Point the tests only at the isolated fixture database, using an authorized disposable instance.

```bat
dotnet --info
dotnet restore .\CampusEvents.slnx
dotnet build .\CampusEvents.slnx --no-restore
dotnet test .\CampusEvents.Tests\CampusEvents.Tests.csproj --no-build --no-restore --logger "console;verbosity=normal"
```

Record the **discovered and executed** case totals, pass/fail/skip results, and test environment from the runner. Investigate database initialization failures, unexpected status codes, invalid tokens, and persisted-state discrepancies separately. Pair HTTP assertions with fresh SQL Server queries that confirm the expected state or unchanged protected fields.

<br>

## Manual

Use an authorized Development database with clearly identified disposable Events and the existing seeded Organizer. For ownership checks, use a second, separately authenticated Organizer account created in an approved local test environment. Record Event IDs, initial persisted states, application-local time, browser responses, actual observed feedback, and resulting persisted fields. Use disposable Event IDs, send direct HTTP requests for protected operations, and inspect the resulting persisted state.

| Scenario | Prerequisites, inputs, and steps | Expected result and evidence |
| --- | --- | --- |
| Publish a future Draft | Sign in as owner; create a valid Draft starting after the current application-local time; open Manage and submit Publish | POST redirects to Manage; success feedback; status persisted as Published. Record ID, before/after database state, and browser capture |
| Published discovery with existing pagination | Prepare at least 26 Published upcoming Events matching the same search text and Category, including the newly Published Event; open `/Events` with those filters and page size 25; use Next and Previous | Two or more pages are available; eligible Events appear on the expected pages, and search, Category, and page-size selections persist across navigation |
| Refuse nonfuture publication | Use an owned Draft starting exactly at now or earlier; send an authorized Publish POST directly with real token | Controlled refusal and unchanged Draft row; record request response and persisted snapshot |
| Refuse repeated Publish | Publish once, then submit another valid POST to the same ID | Controlled refusal; status remains Published; no new transition |
| Cancel a future Published Event | Use owned future Published Event, submit Cancel POST | Status becomes Cancelled, original ID/other columns remain, feedback appears; record database snapshot |
| Check Cancelled direct Details | Copy URL before cancellation; after Cancel, open known `/Events/Details/{id}`; search public catalog for same Event | Details shows Cancelled with HTTP 200; public browse/search excludes it. Capture both results |
| Refuse invalid cancellation | Submit Cancel on an owned Draft, Cancelled Event, and Published Event exactly at or after its start | Controlled refusal; no deletion or status change. Record each starting state and actual outcome |
| Safe deletion confirmation | Open Delete on an owned Draft; inspect title/status/irreversibility message; choose Keep Event | Confirmation GET does not write; Event remains; leaving page is safe |
| Confirm Draft deletion | Reopen the confirmation and submit **Permanently Delete Draft** | POST redirects with feedback; the exact Draft ID is absent from the database; unrelated rows remain |
| Reject wrong-state deletion | Send Delete GET and DeleteConfirmed POST for owned Published and Cancelled IDs | GET is not found; POST refuses mutation; each row remains in its previous status |
| Published Edit | Edit owned Published Event's title/location/capacity using valid fields; forge extra `Status` and `OrganizerId` keys for an additional direct POST probe | Allowed fields update; original owner, Published status, and CreatedAt stay unchanged; Category validation still applies |
| Invalid Published Edit | Submit nonexistent Category or invalid end-before-start values | Form returns validation feedback with attempted values and Category options; no partial persistence |
| Reject Cancelled Edit and republish | Attempt Edit GET/POST and Publish POST on owned Cancelled Event | Controlled refusal; no status change or data update |
| Enforce ownership | Sign in as Organizer B; alter route IDs to Organizer A's Draft/Published Events across Publish, Cancel, Delete, and Edit POSTs | Each target is refused; no Event A mutation. Capture URL, resulting status, and unchanged database snapshot |
| Enforce authentication and forgery protection | As anonymous visitor and signed-in non-Organizer, try lifecycle endpoints; as Organizer, submit POST without antiforgery token | Expected authentication/authorization redirects or refusal and token rejection; no state change |
| Retain authentication and Create | Verify Registration, Login, generic invalid-login response, POST Logout, Create validation, and server-assigned Draft owner/status | Existing Lesson 4–5 behavior remains; record the actual outcomes |
| Retain catalog and Details boundaries | Test 10/25/50 page sizes, ordering, search retention, invalid page size, Draft Details, and nonexistent Details ID | Previously supported catalog controls still work; public Draft and missing Details remain 404 |
| Accessible interaction and responsive display | Use keyboard only on Manage and Delete; inspect focus, form labels, buttons, notices, narrow viewport, and screen reader if available | All actions discoverable and operable; destructive language explicit; feedback readable and visibly associated with the outcome |

For equality and one-second time boundaries, use the fixed-clock test host and isolated database. In the Development browser, choose known future or past times, inspect the POST, antiforgery token, redirect or refusal response, and subsequent GET, and compare the persisted Event state.

<br>

## SRS Traceability Matrix Update with Evidence

Update this working traceability matrix with your executed checks. Begin with `IMPLEMENTED—UNVERIFIED` for authored functionality; use `VERIFIED FOR LESSON SLICE` when the corresponding test results and observations have been recorded. Retain `PARTIAL` for requirements that also depend on Registration counts.

| Requirement / Reference | Implementation / Artifact | Verification / Evidence | Status | Notes |
| --- | --- | --- | --- | --- |
| `UC-005`, `FR-EVT-007`, `BR-EVT-002` | `EventsController.Publish`, Manage POST form, injected clock | Publish success, status mismatch, exact/past start, own/foreign ID tests; SQL snapshots | IMPLEMENTED—UNVERIFIED | Verify actual current-state eligibility and persisted transition |
| `FR-EVT-008`, `BR-EVT-005`, `FR-SRH-001–002`, `FR-SRH-009` | Inherited Index `search/categoryId/page/pageSize` query | Search/filter/page-size tests before/after Publish and Cancel; browser records | IMPLEMENTED—UNVERIFIED | Preserve 10/25/50 page-size behavior and deterministic ordering |
| `FR-EVT-009`, `BR-EVT-006` | `EventsController.Cancel`, Manage POST form | Cancel success, repeated request, exact/past start, ownership tests | IMPLEMENTED—UNVERIFIED | Must compare saved row before/after each refusal |
| `FR-EVT-010`, `FR-SRH-008`, `BR-EVT-007` | Persisted Cancelled state; Details accepts Published/Cancelled only | Direct link HTTP 200, visible cancellation notice, catalog exclusion, republish refusal | IMPLEMENTED—UNVERIFIED | Cancelled is terminal; Draft Details returns 404 |
| `FR-EVT-011`, `BR-EVT-008` | Delete confirmation GET; guarded `DeleteConfirmed` POST | GET-no-write, valid Draft POST, wrong-state and foreign-ID checks | PARTIAL | Requires a current-Registration count check |
| `FR-EVT-004`, `FR-EVT-006`, `BR-EVT-009–010` | Draft/Published Edit GET/POST; original input/Category validation; Cancelled rejection | Valid Published update, forged fields, invalid Category, Cancelled Edit tests | PARTIAL | Requires an authoritative current-Registration count and capacity floor |
| `FR-EVT-005`, `FR-ACC-006`, `BR-AUT-001–002`, `BR-AUT-004` | Owned Manage and per-action role/ownership checks | Real HTTP auth, ownership and direct POST tests | PARTIAL | This implementation secures the Organizer endpoints; the SRS also covers Participant and Administrator endpoints |
| `NFR-SEC-001`, `NFR-SEC-003` | Role and owner guards; POST antiforgery | Missing-token, anonymous, non-Organizer, forged-ID requests | IMPLEMENTED—UNVERIFIED | Confirm checks remain server-enforced even without UI control |
| `NFR-USA-003–004`, `NFR-ACC-001–003` | Confirmation View; error/status messages; semantic controls and CSS | Keyboard, focus, viewport, accessibility and feedback observations | IMPLEMENTED—UNVERIFIED | Record focus order, announcement behavior, and clarity of destructive controls |
| `NFR-REL-001–002`, `AC-003`, `AC-005`, `AC-009–010` | Guarded mutations, persisted Event entity, cumulative test suite | Test output, SQL row comparisons, manual browser observations | PARTIAL | Lifecycle checks address one authorization-sensitive workflow; the SRS also requires registration/capacity coverage |

**Evidence context:** Date: Pending | Environment / dataset: Pending | Procedure / command or scenario: Pending | Observed outcome: Not executed | Commit / repository reference: Not available.

Store command output, test totals, relevant database snapshots, browser captures, and commit references in the project's traceability record. Associate the Organizer lifecycle results with `AC-003`, `AC-005`, `AC-009`, and `AC-010` according to the scope of each criterion.

After completing verification and recording your actual results in the traceability matrix, review the changes and create a focused Git commit:

```bat
git status --short
git diff --check
git diff
git add CampusEvents/Controllers/EventsController.cs ^
	CampusEvents/Views/Events/Manage.cshtml ^
	CampusEvents/Views/Events/Details.cshtml ^
	CampusEvents/Views/Events/Edit.cshtml ^
	CampusEvents/Views/Events/Delete.cshtml ^
	CampusEvents/wwwroot/css/site.css ^
	CampusEvents.Tests/OrganizerEventHttpTests.cs
git diff --cached --stat
git commit -m "Add guarded organizer event lifecycle and regression tests"
git push -u origin lesson-06-event-lifecycle
```

Run these commands from the outer solution directory. Inspect the staged changes before committing, record the resulting commit identifier, and keep credentials and local secrets out of the repository.

<br>

# Guided Exercises

<br>

## Exercise: model a transition table from evidence

**Purpose and expected outcome:** Practice Objectives 1, 5, and 8 by translating business rules into explicit allowed and rejected transitions.

**Prerequisites and SRS ties:** Completed lifecycle implementation; `FR-EVT-007–011`, `BR-EVT-002`, and `BR-EVT-006–010`.

**Task:** Produce a table covering each starting Event status and the commands Publish, Cancel, Delete, and Edit. Include the owner check, time guard, persisted result, and Registration-count dependencies.

**Hints or checkpoints:** Check each server guard directly. Indicate which requirements depend on current Registration counts.

**Deliverable:** One transition matrix with a short rationale for each refusal category.

**Completion and verification:** Compare the matrix against direct POST results and database snapshots for one permitted and one rejected transition.

<br>

## Exercise: challenge the boundary-time rule

**Purpose and expected outcome:** Practice Objectives 4 and 7 by creating deterministic temporal tests that distinguish strict comparisons from inclusive comparisons.

**Prerequisites and SRS ties:** Existing fake clock and SQL Server fixture; `BR-EVT-002` and `BR-EVT-006`.

**Task:** Add a new parameterized regression that tests a start one second before, exactly at, and one second after the fixed application-local time for one lifecycle command. Use the existing injected fixed clock.

**Hints or checkpoints:** Control `EndDateTime` so the Event remains valid; examine the `<= now` refusal branch and compare the row before/after.

**Deliverable:** New authored test source with three independently discoverable time cases.

**Completion and verification:** Run the selected test filter and record the discovered cases, pass/fail results, and Event-state assertions.

**Challenge (optional):** Repeat for the other lifecycle command and compare which guards remain unchanged.

<br>

## Exercise: test forged ownership across lifecycle commands

**Purpose and expected outcome:** Practice Objectives 2, 3, and 7 by testing a direct-HTTP attack that bypasses the UI.

**Prerequisites and SRS ties:** Two SQL-backed Organizer accounts, owned Events for each, authenticated test-host client; `BR-AUT-002`, `BR-AUT-004`, and `NFR-SEC-001`.

**Task:** Have Organizer B obtain a legitimate token, replace each route ID with Organizer A's eligible Event ID, and send Publish, Cancel, and DeleteConfirmed POSTs. Also attempt Edit POST with forged owner/status fields.

**Hints or checkpoints:** Use a suitable status for each operation. A valid token checks request origin; the independent ownership query authorizes access to an Event. Check before/after snapshots.

**Deliverable:** Focused HTTP integration tests and documented request/result pairs.

**Completion and verification:** Every foreign mutation is refused and Organizer A's rows remain unchanged; record actual runner and database evidence.

<br>

## Exercise: evaluate expected failures and feedback

**Purpose and expected outcome:** Practice Objectives 6 and 9 by improving a useful feedback detail without altering server authority.

**Prerequisites and SRS ties:** Working Manage feedback and Delete confirmation; `NFR-USA-003–004`, `NFR-ACC-001–003`.

**Task:** Review a redirected error message and a deletion warning using keyboard-only navigation. Improve one wording, focus, or semantic issue you observe; preserve controller authorization and business checks.

**Hints or checkpoints:** Capture the initial and changed experience; check keyboard focus, text contrast, and screen-reader announcement behavior.

**Deliverable:** Small View/CSS change with a concise accessibility observation record.

**Completion and verification:** Reproduce the before and after scenarios and explain precisely what changed for the user without affecting persisted Event state.

<br>

## Exercise: specify data-dependent invariants

**Purpose and expected outcome:** Practice Objectives 8 and 9 by specifying the relational data and coordination needed to enforce a multi-record business invariant.

**Prerequisites and SRS ties:** Event lifecycle state machine and SRS `FR-EVT-011`, `BR-EVT-008–009`.

**Task:** Specify the persisted Registration association and two invariants: a Draft with a current Registration is ineligible for deletion, and a Published Event with N current Registrations requires Capacity ≥ N. Identify the reads and writes that must share a consistency boundary.

**Hints or checkpoints:** Describe the Registration-to-Event relationship, the count predicate, and the database consistency strategy. Keep this exercise at the design level.

**Deliverable:** A short invariants specification with example valid and rejected operations and the required data relationships.

**Completion and verification:** Compare each invariant to `BR-EVT-008–009` and explain which persisted rows determine acceptance and refusal.

<br>

# Troubleshooting Common Problems

| Symptom | Likely cause / diagnosis | Correction / verification |
| --- | --- | --- |
| A GET URL changes an Event's state | A mutation action was exposed as GET | Require `[HttpPost]`, antiforgery, and a separate safe confirmation GET; verify GET leaves DB unchanged |
| Publish works on a started Draft | Check uses `<` instead of `<=`, or compares against a different clock | Use `StartDateTime <= _timeProvider.GetLocalNow().DateTime` for refusal; test equality under a fixed clock |
| A forged ID changes another Organizer's Event | Resource lookup uses ID without OrganizerId | Query by both ID and authenticated Organizer ID before state checks; retest direct POST |
| Cancelled Event disappears even by known Details URL | Details still filters Published only | Permit Published or Cancelled in Details, retain the `EventNotFound` response for Draft/missing; test URL directly |
| Cancelled Event appears in normal public results | Index eligibility predicate was weakened while changing Details | Restore Published **and** future filter before search, count, and pagination |
| Public catalog unexpectedly loses page-size selection | Old Index method or View replaced the four-parameter query contract | Restore `pageSize`, allowed 10/25/50 values, preservation of filters, and paging regression tests |
| Changed status appears in Manage but not in SQL Server | UI state or message was mistaken for persistence, or save path missing | Inspect Event status in a fresh DbContext after POST; verify `SaveChangesAsync()` after all checks |
| Delete confirmation removes a row | Deletion logic was placed in GET | Keep GET `AsNoTracking()`; mutation only in protected `DeleteConfirmed` POST |
| Published Edit becomes 404 | Draft-only query was left in one Edit action | Update GET and POST eligibility consistently; retain Cancelled refusal |
| Existing Edit regression fails for a Published Event | The test retains Draft-only edit eligibility | Update eligibility assertions and verify eligible Published editing and protected-field preservation |
| Invalid Edit changes part of the row | Assignment or SaveChanges occurs before validation | Rebuild Category options and return before any mutation; compare complete snapshots |
| Submit returns HTTP 400 when it should succeed | Antiforgery token absent/invalid, usually on manually constructed POST | Fetch a genuine form token in same cookie session; retain `[ValidateAntiForgeryToken]` |
| Test fails because local time differs | Static clock used in new action or test zone inconsistent | Reuse injected `TimeProvider` and fixed UTC−04 test zone; record local boundary |
| A test reports status 405 rather than 404 for GET to POST-only endpoint | Endpoint routing matched path but disallowed HTTP method | Confirm there was no mutation; assert the actual method-not-allowed response for that host |
| Test project cannot connect to SQL Server | LocalDB unavailable, incorrect override, or missing migration permission | Inspect isolated fixture connection and migration logs; use an authorized disposable SQL Server endpoint only |
| Lifecycle functions accidentally block anonymous browsing | Role authorization placed at controller class scope | Keep role attributes on protected actions only; regression-test anonymous Index and Details |
| Success or failure text is missing after redirect | Feedback key mismatch, double consumption of `TempData`, or wrong redirect target | Use `StatusMessage`/`ErrorMessage` once in Manage; inspect response and single-redirect flow |
| Buttons render without project styling | Missing or mismatched `site.css` selectors | Use semantic HTML and project-owned button classes; verify keyboard focus and narrow viewport |

<br>

# Checking Your Understanding

<details><summary>Why is publishing an Event a state transition rather than a generic edit?</summary>

Publication has a restricted origin state and additional preconditions. A generic edit could accept arbitrary field changes without expressing that only an owned future Draft may become Published. An explicit command allows clear guards, tests, and feedback.

</details>

<details><summary>What must the server check when a Publish request arrives?</summary>

The server authenticates the actor, checks the Organizer role and Event ownership, and evaluates the persisted status and start time for each request. These checks also protect direct POST requests and stale pages.

</details>

<details><summary>What should happen when an owned Published Event's start equals the current application time?</summary>

Cancellation must be refused, because the SRS permits it only **before** start. The same strict comparison applies when publishing a Draft: the start must be in the future, not equal to now. A frozen clock supports deterministic equality tests.

</details>

<details><summary>Why does cancellation retain the record while deletion removes it?</summary>

Cancellation preserves identity and a direct-link explanation for an Event that was published. Eligible Draft deletion removes an unneeded unpublished record. The public catalog filters to Published upcoming Events; direct Details links remain available for Cancelled Events.

</details>

<details><summary>Why must DeleteConfirmed reload the Event after the confirmation page was shown?</summary>

The confirmation GET and POST are separate requests. Reload ownership and status at confirmation time to apply the command to current persisted data. Cross-record invariants require an appropriate transactional or locking boundary when concurrent requests can alter eligibility.

</details>

<details><summary>Which persisted information is required for the full Draft-deletion rule?</summary>

`BR-EVT-008` requires the count of current Registrations for the Event. A complete deletion guard evaluates Draft status, ownership, and zero current Registrations together.

</details>

<details><summary>How do you verify a state transition across HTTP and persistence?</summary>

Check the HTTP response and follow-up page, then query the saved Event from a fresh database context. Compare the saved status and unchanged owner, creation time, and other protected fields with the required transition.

</details>

<details><summary>When should a team introduce a separate lifecycle domain service?</summary>

When business logic spans multiple consumers, grows complex, or needs an explicit transactional boundary, extraction may improve cohesion and testability. For a small number of simple guarded actions, adding a new layer can create unnecessary indirection. The decision should follow actual responsibilities and maintenance pressures.

</details>

<br>

# Summary

You **modeled guarded state transitions** and distinguished legal domain commands from arbitrary updates, using the interaction between current state, actor, and contextual conditions to explain each permitted change. You **designed safe request contracts** that separate retrieval from mutation, combined explicit confirmation with forgery defenses, and **applied layered authorization** independently of the previously rendered interface.

You **analyzed temporal boundaries** as testable inputs rather than ambient conditions and compared the consequences of preserving a resource in a terminal state with permanently deleting it. You also **designed controlled responses** for expected refusals, reserving exceptional handling for failures outside the normal business contract.

Finally, you **practiced verification** by pairing protocol behavior with persisted before-and-after state, exploring negative and boundary cases, and checking accessibility. You also **identified data dependencies** for cross-record invariants and strengthened incremental integration through reviewable changes and clear repository history.

<br>

# Checkpoint

**Inherited baseline:** CEMS already has a server-rendered ASP.NET Core MVC/Razor application with SQL Server persistence, Identity authentication, public Published/upcoming catalog and Details, search/Category filtering with selectable 10/25/50 pagination, Organizer-only owned management, and validated Create and Draft-only Edit using `EventInputModel`. The existing solution also has an isolated SQL Server HTTP/integration testing foundation.

**New in this lesson:**

- `POST /Events/Publish/{id}` and `POST /Events/Cancel/{id}` perform guarded Organizer-owned status transitions using current persisted state and the established injected clock.
- `GET /Events/Delete/{id}` displays a read-only destructive-action confirmation; `POST /Events/DeleteConfirmed/{id}` deletes an eligible owned Draft after an explicit POST.
- `GET/POST /Events/Edit/{id}` now supports owned Draft and Published Events while refusing Cancelled Events; submitted owner and status remain server-controlled.
- Public `Details` now permits Published and Cancelled Events and displays cancellation clearly; public Index remains Published/upcoming-only with its inherited search, Category, and pagination contract.
- `Views/Events/Manage.cshtml` presents state-aware action controls and `TempData` success/error feedback using the shared time abstraction; `Views/Events/Delete.cshtml` and the relevant `site.css` styles supply the confirmation experience.
- `CampusEvents.Tests/OrganizerEventHttpTests.cs` includes lifecycle HTTP/persistence scenarios and revised Edit eligibility assertions.

**Resulting baseline for next lesson:** CEMS now supports Organizer-owned lifecycle commands and direct Cancelled Details alongside its established public browsing, validation, and account workflows. It retains the relational persistence, authentication, and protected Event workflows that support the Participant Registration subsystem.

<br>

# Next Lesson Preview

Lesson 7 introduces Participant registration as a relational association between an authenticated user and an eligible Published Event. You will derive current counts and remaining capacity from persisted Registrations, prevent duplicate and over-capacity registration—including competing last-seat requests—and support viewing and cancelling your own registrations. You will apply persisted registration counts to Draft deletion and Published capacity changes.

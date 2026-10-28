# **Lesson 5 — Organizer Event Forms, Validation, and Ownership**

<br>

# Learning Outcomes

After completing this lesson, you should be able to:

1. trace the GET/POST lifecycle of an MVC form
2. explain how model binding creates an input model from submitted form values
3. use an input model rather than binding a persisted EF Core entity directly
4. use Data Annotations for straightforward field validation
5. implement cross-field validation for Event start/end times
6. explain how `ModelState` controls whether a POST may proceed
7. repopulate Category options after a failed POST
8. explain why valid submitted values remain visible after validation failure
9. use POST/Redirect/GET after a successful mutation
10. resolve the current authenticated `ApplicationUser`
11. use role authorization to protect Organizer-only actions
12. enforce Event ownership on the server before editing
13. explain why `OrganizerId` and `Status` must not be trusted from submitted form data
14. explain over-posting and how a dedicated input model reduces that risk
15. use anti-forgery protection on state-changing forms
16. explain why validation or authorization failures must leave persisted data unchanged

---

<br>

# Where We Are in the Project

At the end of Lesson 4, CEMS can identify authenticated users and distinguish the additional `Organizer` and `Administrator` roles. The application already has:

- persisted `Event` and `Category` data
- public Published/upcoming Event browsing
- title/location search
- Category filtering
- Event public catalog pagination with retained page size, search, and category state
- Event Details
- ASP.NET Core Identity registration, login, and POST logout
- authentication-aware navigation
- an authentication-only `/Participant` page
- the exact `Organizer` and `Administrator` role names
- the existing Organizer identity `seed-organizer-001`
- the Event → Organizer relationship through `ApplicationUser`
- server-side authorization through `[Authorize]`
- semantic Razor/HTML and project-owned `wwwroot/css/site.css`
- no Bootstrap, jQuery, or JavaScript validation framework

What is still missing is an actual Organizer product workflow. Until now, Events have been created through Development seed data. An Organizer can authenticate, but that Organizer cannot yet open a management page, create a new Event, or edit an owned Event. This lesson changes that.

By the end of Lesson 5, an Organizer will be able to:

1. open an Organizer-only Event management page
2. see only Events owned by that Organizer, across all statuses
3. open a server-rendered Create form
4. submit the Event fields permitted by the application
5. receive server-side validation feedback
6. have the owner assigned from the authenticated user rather than from submitted form data
7. have every new Event forced to `Draft`
8. edit an eligible owned Draft Event
9. be blocked from editing another Organizer's Event even when attempting a direct URL request

This lesson does **not** add Publish, Cancel, Delete, Registration, or Administrator product workflows.

---

<br>

# Part A — Full-Stack Concepts and ASP.NET Features

## 1. SRS Requirements and Use Cases

The SRS requires an Organizer workflow with several related behaviors:

- `FR-EVT-001` — an Organizer can create an Event owned by that Organizer
- `FR-EVT-002` — a new Event begins as `Draft`
- `FR-EVT-003` — the form collects title, description, category, location, start date/time, end date/time, and capacity
- `FR-EVT-004` — invalid creation or modification is rejected
- `FR-EVT-005` — the Organizer can view a management list of owned Events across Draft, Published, and Cancelled statuses
- `FR-EVT-006` — the Organizer can edit an owned Event when it is eligible for modification

This lesson implements the Create workflow and the owned-**Draft** portion of Edit.

Published-event editing is deliberately not completed here. Lesson 6 will evolve editing eligibility when Event lifecycle behavior is introduced, and Lesson 7 will finalize the registration-dependent capacity restriction.

The lesson also reinforces:

- `FR-ACC-006` — protected operations remain
- `BR-AUT-002` — Organizers may manage only owned Events through the Organizer workflow
- `BR-AUT-004` — server authorization remains authoritative even when the UI hides an action
- `BR-EVT-001` — start must occur before end
- `BR-EVT-003` — capacity must be a whole number greater than zero
- `BR-EVT-004` — every Event must reference an existing Category
- `NFR-USA-002` — forms provide understandable validation feedback and preserve valid entered values
- `NFR-ACC-001` through `NFR-ACC-003` — form labels, keyboard operation, headings, and semantic HTML remain important
- `NFR-REL-001` — successful creates/updates remain persisted across later requests
- `NFR-REL-002` — validation or authorization failure must not partially apply a requested change

---

<br>

## 2. Form Requests Are a Two-Request Conversation

A server-rendered MVC form normally uses at least two request paths.

For Create:

```text
GET /Events/Create
	↓
server prepares form state
	↓
Razor renders HTML form
	↓
browser displays form

user submits
	↓
POST /Events/Create
	↓
MVC model binding
	↓
validation / ModelState
	↓
valid?
	├─ no		→ return the form with errors
	└─ yes	→ assign protected fields on server
							↓
			 			save
							↓
			 			redirect to GET /Events/Manage
```

> The GET request does not create an Event. It prepares the page.

> The POST request receives user input and attempts the state change.

Keeping those responsibilities separate makes the request flow easier to reason about and gives us a natural place to validate before persistence.

---

<br>

## 3. Model Binding: How Form Values Reach C#

Suppose a form submits fields named: `Title`, `Description`, `CategoryId`, `Location`, `StartDateTime`, `EndDateTime`, and `Capacity`. MVC model binding looks for matching properties on the action parameter.

If the POST action is:

```csharp
public async Task<IActionResult> Create(string title, string description, int categoryId, string location, long startDateTime, long endDataTime, int capacity)
```

MVC attempts to map each parameter before the action body runs.

Or if the POST action is:

```csharp
public async Task<IActionResult> Create(EventInputModel model)
```

MVC attempts to populate that `EventInputModel` before the action body runs.

Conceptually:

```text
HTTP form fields
	↓
MVC model binder
	↓
EventInputModel
	↓
validation
	↓
ModelState
```

This is convenient, but it also creates a security question:

> Which properties should the browser be allowed to submit?

That leads to the input-model design.

---

<br>

## 4. Why We Will Not Bind `Event` Directly

The persisted `Event` entity contains more than editable form data. An Event has protected application state such as `OrganizerId` and `Status`. Those values must not be controlled by a create form.

Imagine a dangerous form model that accepted every `Event` property. A malicious request could try to send:

```text
OrganizerId = someone-else
Status = Published
```

Even if the normal Razor page never displays those inputs, a user can create a request manually. A hidden input would not solve the problem. Hidden input is still browser input.

The safer design is:

```text
browser-editable fields
	↓
EventInputModel
	↓
server validation
	↓
server creates/updates Event
	↓
protected fields come from trusted server state
	├─ OrganizerId → cookie reconstructed user ID
	└─ Status → EventStatus.Draft
```

Neither value comes from the submitted model. This is one example of reducing **over-posting**: the POST model exposes only the values that this operation is supposed to accept.

---

<br>

## 5. Three Different Kinds of Validation

This lesson uses three related validation layers.

### 5.1 Straightforward field validation

Examples:

- Title is required.
- Description is required.
- Category selection is required.
- Location is required.
- Start date/time is required.
- End date/time is required.
- Capacity is required.
- Capacity must be greater than zero.

These fit naturally into *Data Annotations*.

<br>

### 5.2 Cross-field validation

The Event timing rule compares two values:

```text
StartDateTime < EndDateTime
```

A property-level attribute on only one field does not naturally describe the relationship. We will use `IValidatableObject` on `EventInputModel` so the model can compare both submitted values.

<br>

### 5.3 Database-backed business validation

A submitted `CategoryId` may be a positive number and still refer to no actual Category. The browser's `<select>` normally prevents that value from appearing in the UI, but the browser is not the authority. The POST action will verify that the category exists in the database. The existing database foreign key remains an additional integrity boundary. Application validation gives the user understandable feedback before `SaveChangesAsync()`, while the relational constraint protects persisted relationships.

---

<br>

## 6. `ModelState` Is the POST Decision Point

MVC builds `ModelState` while binding and validating submitted values.

A useful mental model is:

```text
submitted request
	↓
model binding
	↓
Data Annotations
	↓
cross-field validation
	↓
controller-added validation errors
	↓
ModelState
```

The POST action should not persist changes when `!ModelState.IsValid`. Instead, it redisplays the same model.

This is important for two reasons:

1. no invalid change is saved
2. the submitted values remain available to Razor

Razor Tag Helpers prefer attempted values from `ModelState` when redisplaying a failed form. That is why a valid title does not disappear merely because end time was invalid.

---

<br>

## 7. Category Options Must Be Rebuilt After a Failed POST

The browser submits the selected `CategoryId`. It does **not** submit the entire trusted list of valid Categories that the server originally loaded.

Therefore:

```text
GET Create
	↓
load Category options
	↓
render form
```

and after a failed POST:

```text
POST Create
	↓
validation fails
	↓
load Category options again
	↓
return View(model)
```

If we forget the second category load, the page can redisplay with an empty `<select>` even though `ModelState` correctly contains the user's selected value. This is a common MVC form bug.

---

<br>

## 8. Role Authorization and Resource Ownership Are Different Checks

Lesson 4 introduced authentication and role authorization.

For an Organizer action, the first boundary is:

```csharp
[Authorize(Roles = RoleNames.Organizer)]
```

That answers:

> Does the current user have Organizer access?

Editing an Event requires another question:

> Does this Organizer own this Event?

That second question is resource-specific. Role membership alone is not sufficient.

The full Edit protection is:

```text
request Edit
	↓
Organizer role required
	↓
resolve current user
	↓
re-query Event from database
	↓
Event owned by current user?
	↓
Event currently eligible for editing?
	├─ no		→ do not mutate
	└─ yes 	→ validation may continue
```

A hidden Edit button is only a usability choice. The server query is the actual ownership boundary.

---

<br>

## 9. Edit Eligibility

At this point in the project, Lesson 5 deliberately edits only owned `Draft` Events.

That means the Organizer Edit query will require:

```text
Event.Id == requested id
	AND
Event.OrganizerId == current organizer id
	AND
Event.Status == Draft
```

---

<br>

# Part B — Guided Evolutionary Code Walkthrough

## 10. Add an Organizer Management Action First

Before building a form, give the Organizer a place to see owned Events. We will protect only the new Organizer actions.

#### File: `Controllers/EventsController.cs`

Add these `using` directives if they are not already present:

```csharp
using CampusEvents.Models;
using CampusEvents.Security;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
```

Evolve the controller fields and constructor to:

```csharp
private readonly ILogger<EventsController> _logger;
private readonly ApplicationDbContext _context;
private readonly UserManager<ApplicationUser> _userManager;

public EventsController(
	ILogger<EventsController> logger,
	ApplicationDbContext context,
	UserManager<ApplicationUser> userManager)
{
	_logger = logger;
	_context = context;
	_userManager = userManager;
}
```

Now add:

```csharp
[HttpGet]
[Authorize(Roles = RoleNames.Organizer)]
public async Task<IActionResult> Manage()
{
	var organizer = await _userManager.GetUserAsync(User);

	if (organizer is null)
	{
		return Challenge();
	}

	var events = await _context.Events
		.AsNoTracking()
		.Include(eventItem => eventItem.Category)
		.Where(eventItem => eventItem.OrganizerId == organizer.Id)
		.OrderBy(eventItem => eventItem.StartDateTime)
		.ThenBy(eventItem => eventItem.Id)
		.ToListAsync();

	return View(events);
}
```

`[Authorize(Roles = RoleNames.Organizer)]` requires the exact Organizer role established in Lesson 4. `GetUserAsync(User)` resolves the authenticated principal back to CEMS's persisted `ApplicationUser`.

The database query contains:

```csharp
.Where(eventItem => eventItem.OrganizerId == organizer.Id)
```

That is why the management list is scoped to the current Organizer. `FR-EVT-005` requires the management list to include the current Organizer's Events across statuses. The public catalog has a Published/upcoming filter; the Organizer management page intentionally does not reuse that public restriction.

---

<br>

## 11. Build the Management View

#### File: `Views/Events/Manage.cshtml`

Create:

```cshtml
@using CampusEvents.Models
@model IReadOnlyList<Event>

@{
	ViewData["Title"] = "Manage Events";
}

<header class="page-heading">
	<div>
		<h1>Manage My Events</h1>
		<p>
			View Events owned by your Organizer account.
		</p>
	</div>

	<a asp-controller="Events"
		asp-action="Create"
		class="button-primary">
		Create Event
	</a>
</header>

@if (Model.Count == 0)
{
	<p role="status">
		You do not own any Events yet.
	</p>
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
						<div>
							<dt>Status</dt>
							<dd>@eventItem.Status</dd>
						</div>
						<div>
							<dt>Category</dt>
							<dd>@eventItem.Category.Name</dd>
						</div>
						<div>
							<dt>Starts</dt>
							<dd>
								<time>
									@eventItem.StartDateTime.ToString("f")
								</time>
							</dd>
						</div>
						<div>
							<dt>Capacity</dt>
							<dd>@eventItem.Capacity</dd>
						</div>
					</dl>
				</div>

				@if (eventItem.Status == EventStatus.Draft)
				{
					<a asp-controller="Events"
						asp-action="Edit"
						asp-route-id="@eventItem.Id"
						class="button-secondary">
						Edit
					</a>
				}
			</article>
		}
	</div>
}
```

---

<br>

## 12. Add Organizer Navigation

Lesson 4 already imports `CampusEvents.Security` in the layout and uses role checks for role labels. Now that `/Events/Manage` actually exists, add a navigation link for Organizers.

#### File: `Views/Shared/_Layout.cshtml`

Inside the authenticated navigation section, after the existing Participant Area link, add:

```cshtml
@if (User.IsInRole(RoleNames.Organizer))
{
	<li>
		<a class="site-nav-link"
			asp-controller="Events"
			asp-action="Manage">
			Organizer Events
		</a>
	</li>
}
```

The conditional link is a convenience. The `[Authorize(Roles = RoleNames.Organizer)]` remains the real role boundary.

An authenticated Participant without Organizer role can type:

```text
/Events/Manage
```

The server still refuses access.

---

<br>

## 13. Add Small Organizer-Page Styling

#### File: `wwwroot/css/site.css`

Append these project-owned styles:

```css
.page-heading {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	justify-content: space-between;
	gap: 1rem;
	margin-bottom: 1.5rem;
}

.page-heading h1 {
	margin-bottom: 0.25rem;
}

.page-heading p {
	margin: 0;
}

.event-management-list {
	display: grid;
	gap: 1rem;
}

.event-management-item {
	display: flex;
	flex-wrap: wrap;
	align-items: start;
	justify-content: space-between;
	gap: 1rem;
	padding: 1rem;
	border: 1px solid #d0d7de;
	border-radius: 0.35rem;
}

.event-management-item h2 {
	margin-top: 0;
}

.event-meta {
	display: grid;
	gap: 0.5rem;
	margin: 0;
}

.event-meta > div {
	display: grid;
	grid-template-columns: 7rem minmax(0, 1fr);
	gap: 0.5rem;
}

.event-meta dt {
	font-weight: 700;
}

.event-meta dd {
	margin: 0;
}
```

---

<br>

## 14. Management-Page Checkpoint

Build and run it:

```cmd
dotnet build
dotnet run
```

Expected behavior:

1. a normal authenticated Participant does not gain `/Events/Manage`
2. the seeded organizer can reach `/Events/Manage`
3. the list contains events whose `OrganizerId` matches that Organizer
4. Draft, Published, and Cancelled rows may all appear
5. only Draft rows show an Edit link
6. the public `/Events` page still behaves exactly as before

---

<br>

## 15. Introduce `EventInputModel`

The Organizer form needs a type that represents only submitted Event-editing fields.

#### File: `ViewModels/EventInputModel.cs`

Create:

```csharp
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class EventInputModel : IValidatableObject
{
	[Required]
	public string Title { get; set; } = string.Empty;

	[Required]
	public string Description { get; set; } = string.Empty;

	[Required]
	[Range(1, int.MaxValue, ErrorMessage = "Select a Category.")]
	[Display(Name = "Category")]
	public int? CategoryId { get; set; }

	[Required]
	public string Location { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.DateTime)]
	[Display(Name = "Start date and time")]
	public DateTime? StartDateTime { get; set; }

	[Required]
	[DataType(DataType.DateTime)]
	[Display(Name = "End date and time")]
	public DateTime? EndDateTime { get; set; }

	[Required]
	[Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than zero.")]
	public int? Capacity { get; set; }

	public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (StartDateTime.HasValue && EndDateTime.HasValue && StartDateTime.Value >= EndDateTime.Value)
		{
			yield return new ValidationResult(
				"End date and time must be after the start date and time.",
				new[] { nameof(EndDateTime) });
		}
	}
}
```

The persisted Event uses values such as an integer capacity and date/time values. The form model deliberately uses: `int?` and `DateTime?` for required value-type inputs. That lets us distinguish *no value submitted* (null) from *a real integer/date was submitted* (not null). Remember that, if not nullable, `int` and `DateTime` would otherwise take default values (e.g., 0, Now).

The `[Required]` attributes can then provide clear form validation. After `ModelState` is valid, the controller knows these nullable properties contain values and can safely use `.Value`.

The model now handles:

- Required fields
- Capacity > 0
- Start < End

In addition, the database reinforces:

- Event → Category foreign key
- Event → Organizer foreign key
- CK_Events_CapacityPositive
- CK_Events_StartBeforeEnd

The layers complement one another:

- input-model validation → understandable form feedback
- controller/database lookup → business validation using current persisted data
- database constraints → persisted integrity boundary

We are not replacing the database constraints. We are adding earlier validation so normal invalid requests can be rejected before persistence. If a request somehow managed to bypass these early checks, the database would still caught them.

`EventInputModel` is a View/input model, not an EF Core entity. No database migration is required for this lesson because we are not changing the persisted Event schema.

Build it:

```cmd
dotnet build
```

---

## 16. Add Create GET

Create and Edit both need the same category list. Add these private helpers inside `EventsController`.

#### File: `Controllers/EventsController.cs`

Add:

```csharp
private async Task PopulateCategoryOptionsAsync(EventInputModel model)
{
	var categories = await _context.Categories
		.AsNoTracking()
		.OrderBy(category => category.Name)
		.ToListAsync();

	model.CategoryOptions = categories
		.Select(category => new SelectListItem(category.Name, category.Id.ToString()))
		.ToList();
}

private async Task ValidateCategoryAsync(EventInputModel model)
{
	if (!model.CategoryId.HasValue || model.CategoryId.Value <= 0)
	{
		return;
	}

	var categoryExists = await _context.Categories
		.AnyAsync(category => category.Id == model.CategoryId.Value);

	if (!categoryExists)
	{
		ModelState.AddModelError(
			nameof(model.CategoryId),
			"Select an existing Category.");
	}
}
```

The Create GET prepares an empty form plus valid Category options:

```csharp
[HttpGet]
[Authorize(Roles = RoleNames.Organizer)]
public async Task<IActionResult> Create()
{
	var model = new EventInputModel();

	await PopulateCategoryOptionsAsync(model);

	return View(model);
}
```

---

<br>

## 17. Build the Create Form

#### File: `Views/Events/Create.cshtml`

Create:

```cshtml
@model CampusEvents.ViewModels.EventInputModel

@{
	ViewData["Title"] = "Create Event";
}

<h1>Create Event</h1>

<p>
	New Events are saved as Draft and belong to your Organizer account.
</p>

<form asp-controller="Events"
		asp-action="Create"
		method="post"
		asp-antiforgery="true"
		class="event-form">

	<div asp-validation-summary="All"
		class="validation-summary"
		role="alert"></div>

	<div class="form-field">
		<label asp-for="Title" class="field-label"></label>
		<input asp-for="Title" class="field-control" required />
		<span asp-validation-for="Title" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Description" class="field-label"></label>
		<textarea asp-for="Description"
			class="field-control event-description"
			rows="6"
			required></textarea>
		<span asp-validation-for="Description" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="CategoryId" class="field-label"></label>
		<select asp-for="CategoryId"
			asp-items="Model.CategoryOptions"
			class="field-control"
			required>
			<option value="">Select a Category</option>
		</select>
		<span asp-validation-for="CategoryId" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Location" class="field-label"></label>
		<input asp-for="Location" class="field-control" required />
		<span asp-validation-for="Location" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="StartDateTime" class="field-label"></label>
		<input asp-for="StartDateTime"
			type="datetime-local"
			asp-format="{0:yyyy-MM-ddTHH:mm}"
			class="field-control"
			required />
		<span asp-validation-for="StartDateTime" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="EndDateTime" class="field-label"></label>
		<input asp-for="EndDateTime"
			type="datetime-local"
			asp-format="{0:yyyy-MM-ddTHH:mm}"
			class="field-control"
			required />
		<span asp-validation-for="EndDateTime" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Capacity" class="field-label"></label>
		<input asp-for="Capacity"
			type="number"
			min="1"
			class="field-control"
			required />
		<span asp-validation-for="Capacity" class="validation-message"></span>
	</div>

	<div class="form-actions">
		<button type="submit" class="button-primary">Save Draft</button>

		<a asp-controller="Events"
			asp-action="Manage"
			class="button-secondary">
			Cancel
		</a>
	</div>
</form>
```

The form includes HTML hints such as: `required`, `min="1"`, `type="datetime-local"`. Those can improve the normal browser experience, but they are not the final validation boundary. A direct HTTP request can omit or alter those values.

The authoritative sequence remains:

```text
request
	↓
model binding
	↓
server-side validation
	↓
ModelState
	↓
controller rules
	↓
database integrity
```

---

<br>

## 18. Add Event-Form Styling

#### File: `wwwroot/css/site.css`

Append:

```css
.event-form {
	width: min(100%, 42rem);
	display: grid;
	gap: 1rem;
}

.event-description {
	min-height: 8rem;
	resize: vertical;
}

.form-actions {
	display: flex;
	flex-wrap: wrap;
	gap: 0.75rem;
}
```

---

<br>

## 19. Add Create POST in Stages

The Create POST has five responsibilities:

1. receive/bind input
2. verify form/business validation
3. resolve current Organizer
4. assign protected Event fields on server
5. persist + redirect

#### File: `Controllers/EventsController.cs`

Add:

```csharp
[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(EventInputModel model)
{
	await ValidateCategoryAsync(model);

	if (!ModelState.IsValid)
	{
		await PopulateCategoryOptionsAsync(model);
		return View(model);
	}

	var organizer = await _userManager.GetUserAsync(User);

	if (organizer is null)
	{
		return Challenge();
	}

	var eventItem = new Event
	{
		Title = model.Title.Trim(),
		Description = model.Description.Trim(),
		CategoryId = model.CategoryId!.Value,
		Location = model.Location.Trim(),
		StartDateTime = model.StartDateTime!.Value,
		EndDateTime = model.EndDateTime!.Value,
		Capacity = model.Capacity!.Value,

		OrganizerId = organizer.Id,
		Status = EventStatus.Draft
	};

	_context.Events.Add(eventItem);
	await _context.SaveChangesAsync();

	return RedirectToAction(nameof(Manage));
}
```

---

<br>

## 20. Trace the Create POST Carefully

### Step 1 — MVC binds the model

Before the method body executes, MVC has already attempted to populate:

```csharp
EventInputModel model
```

and perform Data Annotation / `IValidatableObject` validation.

### Step 2 — The controller verifies Category existence

```csharp
await ValidateCategoryAsync(model);
```

A submitted positive integer is not automatically a valid Category.

### Step 3 — Invalid requests return before persistence

```csharp
if (!ModelState.IsValid)
{
	await PopulateCategoryOptionsAsync(model);
	return View(model);
}
```

No `Event` entity has been added to the DbContext at this point. No persisted change should occur. The category options are rebuilt because the browser submitted only the selected ID, not the trusted option list.

### Step 4 — Resolve the current user

```csharp
var organizer = await _userManager.GetUserAsync(User);
```

The owner is derived from authenticated server state.

### Step 5 — Protected fields are assigned on the server

```csharp
OrganizerId = organizer.Id,
Status = EventStatus.Draft
```

That makes a request with added illegal values such as this irrelevant:

```text
OrganizerId=another-user
Status=Published
```

These fields do not exist on `EventInputModel`. There are no matching input-model properties for MVC to bind.

### Step 6 — Persistence happens only after validation

```csharp
_context.Events.Add(eventItem);
await _context.SaveChangesAsync();
```

### Step 7 — POST/Redirect/GET

The successful POST returns:

```csharp
RedirectToAction(nameof(Manage))
```

The browser then performs a fresh GET. This prevents the normal successful page from being the response body of the POST itself and avoids a common "refresh resubmits the form" experience.

---

<br>

## 21. Create Validation Scenarios

> **NOTE**: some of the scenarios below will not even submit, because they will get blocked by the UI validation checks. Use browser developer tools to temporarily change the form values or remove the validations checks from the cshtml for these verification exercises or create appropriate `curl` requests that bypass UI entirely.

Run:

```cmd
dotnet build
dotnet run
```

Sign in as the seeded organizer and open:

```text
/Events/Create
```

Use these scenarios.

### Scenario A — Missing required input

Leave one or more required fields empty.

Expected:

- server-side validation rejects the request
- the form is redisplayed
- validation feedback identifies the problem
- other valid values remain present
- no event is persisted

### Scenario B — End is not after Start

Submit:

- Start: 10:00
- End: 09:00

Expected:

- `End date and time must be after the start date and time.`
- no event is persisted

### Scenario C — Non-positive capacity

Submit:

- Capacity = 0

Expected:

- `Capacity must be greater than zero.`
- no event is persisted

### Scenario D — Nonexistent Category manipulation

> **NOTE**: The normal `<select>` contains only real Categories. Use browser developer tools to temporarily change the selected option value to an ID that does not exist.

Submit:

- 9999

Expected:

- `Select an existing Category.`
- no event is persisted

This proves that the server does not assume that a value came from the rendered list.

### Scenario E — Valid Create

Submit valid Event data.

Expected:

- a new Event row is saved
- the browser redirects to `/Events/Manage`
- the Event appears in the management list
- the new Event status is `Draft`
- the new Event owner is the authenticated Organizer
- refreshing `/Events/Manage` still shows the Event

The last point demonstrates persistence across requests.

---

<br>

## 22. Why Valid Values Survive Validation Failure?

Suppose the Organizer submits:

```text
Title: Campus Robotics Showcase
Location: Engineering Atrium
Capacity: 0
Start: October 8, 2:00 PM
End: October 8, 1:00 PM
```

The action does `return View(model);` and does not redirect. At this point Razor has both: `EventInputModel` and `ModelState` attempted values/errors. The Tag Helpers in the cshtml help redisplay the attempted values. The user should correct the only the invalid fields rather than retyping every valid field. That directly supports `NFR-USA-002`.

---

<br>

## 23. Add Edit GET

Create is now working conceptually. Edit reuses the same input shape but starts from a persisted Event.

#### File: `Controllers/EventsController.cs`

Add:

```csharp
[HttpGet]
[Authorize(Roles = RoleNames.Organizer)]
public async Task<IActionResult> Edit(int id)
{
	var organizer = await _userManager.GetUserAsync(User);

	if (organizer is null)
	{
		return Challenge();
	}

	var eventItem = await _context.Events
		.AsNoTracking()
		.FirstOrDefaultAsync(eventItem =>
			eventItem.Id == id &&
			eventItem.OrganizerId == organizer.Id &&
			eventItem.Status == EventStatus.Draft);

	if (eventItem is null)
	{
		return NotFound();
	}

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

The Edit query is an authorization boundary.

Focus on this predicate:

```csharp
eventItem.Id == id &&
eventItem.OrganizerId == organizer.Id &&
eventItem.Status == EventStatus.Draft
```

The URL supplies only `id`. The server adds organizer ownership and Draft eligibility. Changing: `/Events/Edit/10` to `/Events/Edit/11` does not transfer ownership. If Event 11 is not an owned Draft Event, the query returns `null` and the action returns `NotFound()`. Responding `NotFound` also avoids using the organizer workflow to reveal whether an inaccessible ID belongs to another user or simply does not exist.

---

<br>

## 24. Build the Edit View

#### File: `Views/Events/Edit.cshtml`

Create:

```cshtml
@model CampusEvents.ViewModels.EventInputModel

@{
	ViewData["Title"] = "Edit Event";
	var eventId = ViewContext.RouteData.Values["id"];
}

<h1>Edit Draft Event</h1>

<p>Lesson 5 permits editing an owned Draft Event.</p>

<form asp-controller="Events"
	asp-action="Edit"
	asp-route-id="@eventId"
	method="post"
	asp-antiforgery="true"
	class="event-form">

	<div asp-validation-summary="All" class="validation-summary" role="alert"></div>

	<div class="form-field">
		<label asp-for="Title" class="field-label"></label>
		<input asp-for="Title" class="field-control" required />
		<span asp-validation-for="Title" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Description" class="field-label"></label>
		<textarea asp-for="Description"
			class="field-control event-description"
			rows="6"
			required></textarea>
		<span asp-validation-for="Description" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="CategoryId" class="field-label"></label>
		<select asp-for="CategoryId"
			asp-items="Model.CategoryOptions"
			class="field-control"
			required>
			<option value="">Select a Category</option>
		</select>
		<span asp-validation-for="CategoryId" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Location" class="field-label"></label>
		<input asp-for="Location" class="field-control" required />
		<span asp-validation-for="Location" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="StartDateTime" class="field-label"></label>
		<input asp-for="StartDateTime"
			type="datetime-local"
			asp-format="{0:yyyy-MM-ddTHH:mm}"
			class="field-control"
			required />
		<span asp-validation-for="StartDateTime" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="EndDateTime" class="field-label"></label>
		<input asp-for="EndDateTime"
			type="datetime-local"
			asp-format="{0:yyyy-MM-ddTHH:mm}"
			class="field-control"
			required />
		<span asp-validation-for="EndDateTime" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Capacity" class="field-label"></label>
		<input asp-for="Capacity"
			type="number"
			min="1"
			class="field-control"
			required />
		<span asp-validation-for="Capacity" class="validation-message"></span>
	</div>

	<div class="form-actions">
		<button type="submit" class="button-primary">Save Changes</button>

		<a asp-controller="Events"
			asp-action="Manage"
			class="button-secondary">
			Cancel
		</a>
	</div>
</form>
```

---

<br>

## 25. Add Edit POST — Re-Query Before Mutation

The Edit POST must not accept a complete incoming `Event` and attach it.

Instead:

```text
route id
	↓
resolve current Organizer
	↓
query persisted owned Draft Event
	↓
not found / not owned / not Draft?
	├─ yes	→ stop
	└─ no		→ validate submitted editable fields
							↓
			 			mutate allowed properties
							↓
			 			SaveChangesAsync
```

#### File: `Controllers/EventsController.cs`

Add:

```csharp
[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, EventInputModel model)
{
	var organizer = await _userManager.GetUserAsync(User);

	if (organizer is null)
	{
		return Challenge();
	}

	var eventItem = await _context.Events
		.FirstOrDefaultAsync(eventItem =>
			eventItem.Id == id &&
			eventItem.OrganizerId == organizer.Id &&
			eventItem.Status == EventStatus.Draft);

	if (eventItem is null)
	{
		return NotFound();
	}

	await ValidateCategoryAsync(model);

	if (!ModelState.IsValid)
	{
		await PopulateCategoryOptionsAsync(model);
		return View(model);
	}

	eventItem.Title = model.Title.Trim();
	eventItem.Description = model.Description.Trim();
	eventItem.CategoryId = model.CategoryId!.Value;
	eventItem.Location = model.Location.Trim();
	eventItem.StartDateTime = model.StartDateTime!.Value;
	eventItem.EndDateTime = model.EndDateTime!.Value;
	eventItem.Capacity = model.Capacity!.Value;

	await _context.SaveChangesAsync();

	return RedirectToAction(nameof(Manage));
}
```

The ordering is deliberate. Ownership must be checked before mutation.

The correct pattern is:

```text
identify resource
	↓
authorize resource
	↓
validate requested changes
	↓
mutate allowed properties
	↓
persist
```

That makes `NFR-REL-002` easier to reason about:

- authorization failure → no mutation
- validation failure → no mutation
- only valid authorized requests reach the assignment block

The edit operation must not include changes to `OrganizerId` or `Status`. *Ownership* is permanently fixed and must not change through the edit form. *Status* must not change through the edit form because it is a controlled lifecycle transition, not a simple assignment. This separation prevents an event edit operation from quietly publishing, cancelling, transfering-ownership, or some combination of those operations.

Build and run it:

```cmd
dotnet build
dotnet run
```

Expected routes now include:

```text
GET	/Events/Manage
GET	/Events/Create
POST /Events/Create
GET	/Events/Edit/{id}
POST /Events/Edit/{id}
```

All five are Organizer-only. The two POST routes also require anti-forgery validation.

---

<br>

# Part C — Guided Verification and Small Student Change

## 26. Verification Matrix

Work through these scenarios against your local Development database.

Record what you expected before each request and what actually happened afterward.

<br>

| Scenario | Expected Server Result | Expected Persistence Result |
|---|---|---|
| Participant requests `/Events/Manage` | access refused | no change |
| Organizer requests `/Events/Manage` | page renders | no change |
| Manage list | only current Organizer's Events | no change |
| Missing required Create value | form redisplays with validation | no Event added |
| End <= Start | timing validation error | no Event added |
| Capacity <= 0 | capacity validation error | no Event added |
| Nonexistent Category ID | Category validation error | no Event added |
| Valid Create | redirect to Manage | new owned Draft Event persisted |
| Refresh after Create | Event still visible | persisted row remains |
| Owned Draft Edit GET | form renders populated values | no change yet |
| Invalid owned Edit POST | form redisplays | Event remains unchanged |
| Valid owned Edit POST | redirect to Manage | permitted fields updated |
| Another owner's Event ID | request refused/not found | no Event change |
| Published/Cancelled ID in Lesson 5 Edit | request refused/not found | no Event change |
| Public browse/search/filter/pagination/details regression | existing page boundaries, result counts, and filter-preserving navigation remain | no unintended change |

<br>

---

## 37. Verify Server-Assigned Owner and Status

After a successful Create, inspect the new Event through your database tooling or debugger.

The important values are:

```text
OrganizerId = current authenticated Organizer's ApplicationUser.Id
Status		= Draft
```

Those values must be true even though the Create form did not contain either field.

That is exactly the point.

Protected fields were assigned from server state.

---

## 38. Verify Failed Create Does Not Partially Persist

Before submitting an invalid Create form, note the current Event count.

Submit invalid data.

Then inspect the database again.

Expected:

```text
Event count unchanged
```

The action returned before:

```csharp
_context.Events.Add(eventItem);
await _context.SaveChangesAsync();
```

That is the structural reason no partial Event is created.

---

## 39. Verify Failed Edit Does Not Partially Persist

Choose an owned Draft Event.

Record several existing values, such as:

```text
Title
StartDateTime
EndDateTime
Capacity
```

Submit an Edit where:

- Title is changed to a valid new value;
- End time violates the timing rule.

Expected:

- validation fails;
- the form shows the submitted Title and timing values;
- the database still contains the original Title and original timing values.

Why?

The POST does not assign editable entity properties until after:

```csharp
if (!ModelState.IsValid)
{
	...
	return View(model);
}
```

The valid Title survives **in the form**, but not in persisted data until the whole request is valid.

---

## 40. Demonstrate the Changed-ID Ownership Attack

The browser normally creates an Edit link only for Events in the current Organizer's management list.

That does not prevent a user from changing the URL.

Use an Event whose `OrganizerId` is different from the currently signed-in Organizer.

For example, if your own Event is:

```text
/Events/Edit/12
```

and another owner's Event is ID `19`, request:

```text
/Events/Edit/19
```

Expected:

```text
404 / Not Found
```

because the server query requires:

```csharp
eventItem.Id == id &&
eventItem.OrganizerId == organizer.Id &&
eventItem.Status == EventStatus.Draft
```

Then attempt the same idea against the POST by changing the route ID before submission.

Expected:

- the inaccessible Event is not returned by the ownership query;
- no property assignment runs against that Event;
- `SaveChangesAsync()` does not modify that Event.

If your current Development seed has Events for only the preserved `seed-organizer-001` owner, use instructor-prepared alternate ownership test data for this scenario rather than weakening or changing the product authorization code.

---

## 41. Regression Check the Public Catalog

Lesson 5 added Organizer actions to the existing `EventsController`.

That makes regression checking especially important.

Verify:

```text
/Events
```

still:

- shows only Published/upcoming Events;
- preserves title/location search;
- preserves Category filtering;
- preserves filter values;
- preserves chronological ordering by start time and Event `Id`;
- shows the total number of matching Events and no more than ten results per page;
- carries `search` and `categoryId` through Previous/Next links;
- returns to page 1 when new search or Category criteria are submitted;
- handles first, last, out-of-range, and zero-result pages with the established feedback.

If your Development catalog has ten or fewer matching Events, use the Lesson 3 multi-page test data to exercise a second page. Verify an existing Event Details URL still follows the prior Lesson 4 behavior.

Organizer management must not accidentally replace the public catalog.

---

## 42. Regression Check Authentication

Verify that Lesson 4 behavior still works:

- Register remains available to anonymous visitors.
- Login still authenticates through Identity.
- Invalid Login remains generic.
- Logout remains POST-only.
- `/Participant` remains authentication-only.
- Organizer role labels still appear for the Organizer.
- The new Organizer Events link is visible only when appropriate.
- Direct requests remain protected even when a navigation link is absent.

---

# 43. Small Student Change — Add Location to the Management List

The management page already displays several Event properties.

Your one independent modification is to add the Event's Location to the existing definition list.

### File: `Views/Events/Manage.cshtml`

Inside `<dl class="event-meta">`, add one new `<div>` following the established pattern:

```cshtml
<div>
	<dt>Location</dt>
	<dd>@eventItem.Location</dd>
</div>
```

Then run:

```cmd
dotnet build
dotnet run
```

Verify that the management list now displays Location for every owned Event.

This is an adjacent transfer: you are applying the already-demonstrated display pattern to one additional Event property. You are not inventing a new controller, authorization mechanism, relationship, or workflow.

---

# Part D — Traceability and Recap

## 44. Trace Create Through the Full Stack

Rehearse the successful Create flow:

```text
Organizer clicks Create Event
	↓
GET /Events/Create
	↓
[Authorize(Roles = Organizer)]
	↓
controller creates EventInputModel
	↓
controller loads Category options
	↓
Razor renders form
	↓
Organizer submits
	↓
POST /Events/Create
	↓
anti-forgery validation
	↓
model binding
	↓
Data Annotation validation
	↓
IValidatableObject cross-field validation
	↓
Category existence validation
	↓
ModelState valid
	↓
UserManager resolves current ApplicationUser
	↓
controller constructs Event
	↓
OrganizerId = current user ID
Status = Draft
	↓
EF Core Add + SaveChangesAsync
	↓
redirect
	↓
GET /Events/Manage
	↓
new Draft Event appears
```

---

## 45. Trace Failed Create

```text
POST /Events/Create
	↓
model binding
	↓
validation error
	↓
ModelState invalid
	↓
Category options repopulated
	↓
return View(model)
	↓
same attempted values + errors rendered
	↓
no Event added
	↓
no SaveChangesAsync
```

---

## 46. Trace Edit Authorization

```text
GET/POST /Events/Edit/{id}
	↓
Organizer role authorization
	↓
resolve current ApplicationUser
	↓
database query requires:
	requested id
	+ current OrganizerId
	+ Draft status
	↓
match?
	├─ no	→ NotFound, no mutation
	└─ yes → continue
```

For POST only:

```text
owned Draft Event
	↓
validate submitted editable fields
	↓
ModelState valid?
	├─ no	→ return form, no entity mutation
	└─ yes → copy permitted fields
				↓
			 SaveChangesAsync
				↓
			 redirect to Manage
```

---

## 47. Requirement Status at the End of Lesson 5

### Complete at the intended Lesson 5 level

- `FR-EVT-001` — Organizer Create exists and ownership is server-assigned.
- `FR-EVT-002` — every newly created Event is forced to `Draft`.
- `FR-EVT-003` — the form collects the required Event fields.
- `FR-EVT-005` — the Organizer management list includes that Organizer's Events across statuses.

### Partial

- `FR-EVT-004` — Create and owned-Draft Edit validation are implemented; later Published-event modification rules still remain.
- `FR-EVT-006` — owned Draft editing is implemented; Published/Cancelled edit eligibility is explicitly evolved in Lesson 6, and the registration-dependent capacity rule is finalized in Lesson 7.
- `FR-ACC-006` — protected Organizer operations are now materially added, but the requirement remains **PARTIAL / RECURRING** because later Participant and Administrator product operations still remain.

### Reinforced

- `FR-SRH-009` — the public catalog retains ten-Event pages, filtered total counts, and filter-preserving page navigation.
- `BR-AUT-002` — Organizer management is ownership-scoped.
- `BR-AUT-004` — the server rechecks role, ownership, and eligibility regardless of UI links.
- `BR-EVT-001` — Start must be before End.
- `BR-EVT-003` — Capacity must be a positive whole number.
- `BR-EVT-004` — selected Category must exist.
- `NFR-USA-002` — forms show validation feedback and preserve valid submitted values.
- `NFR-ACC-001` — form controls have associated labels.
- `NFR-ACC-002` — the workflow uses standard keyboard-operable controls.
- `NFR-ACC-003` — pages continue to use meaningful headings and semantic markup.
- `NFR-REL-001` — successful Create/Edit changes are persisted.
- `NFR-REL-002` — validation/authorization failures do not partially apply changes.

---

## 48. What Remains Intentionally Deferred

Lesson 5 does **not** contain:

- Publish actions;
- Cancel actions;
- Delete actions;
- Event lifecycle command UI;
- Registration entity;
- Registration `DbSet`;
- Registration relationships;
- Registration migration;
- Participant Event registration;
- My Registrations;
- Organizer registration oversight;
- Administrator Event-management pages;
- Organizer-access management UI;
- Category administration CRUD;
- Repository;
- Unit of Work;
- CQRS;
- MediatR;
- Clean Architecture;
- microservices;
- REST API;
- SPA framework;
- Razor Pages authentication redesign;
- Bootstrap;
- jQuery;
- JavaScript validation frameworks.

No automated test project is introduced in Lesson 5.

---

# Check Your Understanding

<details>
<summary>Why does Lesson 5 use `EventInputModel` instead of accepting a complete `Event` entity from the Create form?</summary>

`EventInputModel` exposes only the fields the Organizer is allowed to submit. Protected Event state such as `OrganizerId` and `Status` is not bindable from the form, reducing over-posting risk and keeping those values under server control.

</details>

<details>
<summary>Why must the Create POST repopulate `CategoryOptions` before returning a failed form?</summary>

The browser submits the selected `CategoryId`, not the trusted server-side list of Category options. When validation fails and the View is returned, the controller must rebuild that option list so Razor can render the Category select again.

</details>

<details>
<summary>What is the difference between the Organizer role check and the Event ownership check in Edit?</summary>

The Organizer role check determines whether the user may enter Organizer functionality at all. The ownership check determines whether that Organizer may manage the specific requested Event. Both are server-side authorization decisions.

</details>

<details>
<summary>Why does changing `/Events/Edit/12` to another Event ID not grant access to that Event?</summary>

The server does not trust the route ID by itself. It re-queries for an Event that matches the requested ID, the current Organizer's ID, and the allowed Draft status. A different owner's Event therefore does not satisfy the query.

</details>

<details>
<summary>Why are `OrganizerId` and `Status` assigned after validation in the Create POST instead of being hidden form fields?</summary>

Hidden form fields are still user-controlled request input. The server already knows the authenticated Organizer and the required initial status, so it assigns `OrganizerId` from the current `ApplicationUser` and forces `Status` to `EventStatus.Draft`.

</details>

<details>
<summary>Why does an invalid Edit POST leave the persisted Event unchanged even if some submitted fields were valid?</summary>

The controller does not copy any submitted values onto the tracked Event until `ModelState` is valid. On validation failure it returns the input model to the View before the mutation block and before `SaveChangesAsync()`.

</details>

<details>
<summary>Why is the Edit link shown only for Draft Events if the Edit action already checks Draft status on the server?</summary>

The conditional link gives the user an interface that advertises only currently available actions. The server check is still required because users can bypass the rendered UI and send direct requests.

</details>

<details>
<summary>Why does Lesson 5 not add Publish, Cancel, or Delete methods while it is already modifying `EventsController`?</summary>

Those operations introduce Event lifecycle state transitions and additional business rules scheduled for Lesson 6. Lesson 5 stays focused on forms, validation, ownership, and owned Draft editing rather than introducing future functionality early.

</details>

---

# Common Mistakes / Debugging Notes

## 49. Putting Organizer Authorization on the Whole Controller

If you add:

```csharp
[Authorize(Roles = RoleNames.Organizer)]
public class EventsController : Controller
```

you would accidentally protect established public Event browsing and Details.

Protect only the new Organizer actions.

---

## 50. Binding the Persisted `Event` Entity Directly

Avoid:

```csharp
public async Task<IActionResult> Create(Event eventItem)
```

for this lesson's form.

That makes it too easy for properties outside the intended form contract to become bindable.

Use:

```csharp
EventInputModel
```

and map permitted values deliberately.

---

## 51. Adding Hidden `OrganizerId` or `Status` Inputs

Do not add:

```html
<input type="hidden" name="OrganizerId" ...>
<input type="hidden" name="Status" ...>
```

Those values would still come from the browser.

Assign them on the server.

---

## 52. Forgetting `[ValidateAntiForgeryToken]`

Create and Edit POSTs change application state.

Keep:

```csharp
[ValidateAntiForgeryToken]
```

and:

```html
asp-antiforgery="true"
```

---

## 53. Forgetting to Repopulate Categories

If a failed form renders with an empty Category list, check for:

```csharp
await PopulateCategoryOptionsAsync(model);
```

before:

```csharp
return View(model);
```

---

## 54. Checking Only `CategoryId > 0`

A positive number is not proof that a Category exists.

Keep the database-backed `AnyAsync(...)` check.

---

## 55. Mutating Before Ownership Is Proven

Do not copy fields onto an Event and then ask whether the user owns it.

Re-query the owned resource first.

---

## 56. Using a Hidden Edit Button as Authorization

The management View may hide Edit for non-Draft Events.

The Edit action must still check:

```text
current owner
+
Draft status
```

because direct requests bypass UI visibility.

---

## 57. Calling `SaveChangesAsync()` Before Validation Is Complete

The correct order is:

```text
validation
	↓
authorization/resource eligibility
	↓
mutation
	↓
SaveChangesAsync
```

Do not persist and then try to "undo" an expected validation failure.

---

## 58. Forgetting That ModelState Preserves Attempted Values

If a POST fails, do not replace the model with a brand-new blank input model.

Return the submitted model after rebuilding only server-generated supporting state such as Category options.

---

## 59. Adding a Migration for `EventInputModel`

`EventInputModel` is not an EF entity.

This lesson does not change the Event schema.

Do not create a Lesson 5 migration merely because a new ViewModel file exists.

---

## 60. Accidentally Adding Lifecycle Behavior

Do not add:

```text
Publish
Cancel
Delete
```

to the management page yet.

Their absence is intentional at the Lesson 5 boundary.

---

# End-of-Lesson Code State

At the end of Lesson 5, the continuously evolving `CampusEvents` application has the following new Organizer behavior:

```text
Organizer
	↓
GET /Events/Manage
	↓
owned Events across statuses

Organizer
	↓
GET /Events/Create
	↓
EventInputModel + Category options
	↓
POST /Events/Create
	↓
server validation
	↓
current user becomes owner
	↓
Status = Draft
	↓
persist
	↓
redirect to Manage

Organizer
	↓
GET /Events/Edit/{id}
	↓
owned Draft query
	↓
populated EventInputModel

Organizer
	↓
POST /Events/Edit/{id}
	↓
re-query owned Draft before mutation
	↓
server validation
	↓
copy permitted editable fields
	↓
persist
	↓
redirect to Manage
```

## Files created

```text
ViewModels/EventInputModel.cs
Views/Events/Manage.cshtml
Views/Events/Create.cshtml
Views/Events/Edit.cshtml
```

## Files materially modified

```text
Controllers/EventsController.cs
Views/Shared/_Layout.cshtml
wwwroot/css/site.css
```

## Persisted model/database state

The existing persisted `Event`, `Category`, `ApplicationUser`, Identity schema, Event → Category relationship, and Event → Organizer relationship remain in place.

No Lesson 5 migration is required.

No `Registration` entity exists.

## Exact Lesson 5 editing eligibility

```text
owned Draft Event		→ editable
owned Published Event	→ not yet edited by Lesson 5 workflow
owned Cancelled Event	→ not editable
another owner's Event	→ not accessible through Organizer Edit
```

Lesson 6 must evolve this policy to the explicit lifecycle-aware Draft/Published/Cancelled rules.

Lesson 7 must finalize the registration-dependent Published capacity restriction.

## Protected-field rule

Create never trusts submitted:

```text
OrganizerId
Status
```

Instead:

```text
OrganizerId = authenticated Organizer's ApplicationUser.Id
Status		= EventStatus.Draft
```

Edit never changes ownership or status.

## Validation rule

Server validation remains authoritative:

```text
required fields
+
Capacity > 0
+
Start < End
+
Category exists
```

Failed validation redisplays the entered values and does not persist a partial change.

Failed ownership/eligibility checks occur before entity mutation and do not persist a change.

## Preserved prior behavior

Lesson 5 does not redesign Lesson 4.

The application still preserves:

- `ApplicationUser : IdentityUser`;
- `ApplicationDbContext : IdentityDbContext<ApplicationUser>`;
- the `seed-organizer-001` ownership identity;
- exact `Organizer` and `Administrator` role names;
- Participant as the authenticated-user baseline rather than an Identity role;
- email-based registration/login;
- POST-only logout;
- safe Login return URLs;
- established middleware order;
- `/Participant` as the authentication-only stepping-stone;
- Lesson 3 public browse/search/filter/pagination/details behavior, including the inherited `Index(string? search, int? categoryId, int? page)` action;
- SQL Server LocalDB through `DefaultConnection`;
- Event data-integrity constraints;
- semantic Razor/HTML;
- project-owned CSS;
- no Bootstrap;
- no jQuery;
- no JavaScript validation framework.

The next lesson begins from this exact Organizer-owned Draft Create/Edit state and adds explicit Event lifecycle operations without introducing Registration yet.

---

<br>

# Summary

## What You Learned

Lesson 5 turned the authentication and authorization foundation from Lesson 4 into the first real Organizer product workflow. The important result is not merely that CEMS now has Create and Edit pages. You now have a complete server-rendered form pattern that separates browser input from persisted application state and keeps authorization and business rules on the server.

You should now be able to explain that:

- an MVC form is a two-request conversation: a GET prepares and renders the form, while a POST receives submitted values, performs model binding and validation, applies trusted server-side rules, and either redisplays the form or persists a valid change;
- **model binding** creates an input object from submitted request values, while **ModelState** records binding and validation results and determines whether the POST is eligible to continue;
- a purpose-specific input model such as `EventInputModel` is preferable to binding an EF Core entity directly because the form should expose only the fields the user is permitted to submit;
- restricting the submitted shape reduces **over-posting** risk, but protected values must still be assigned or preserved explicitly by trusted server code;
- `OrganizerId` must come from the authenticated `ApplicationUser`, not from browser input, and a newly created Event must be forced to `EventStatus.Draft` by the server;
- straightforward field rules can be expressed with Data Annotations, while cross-field rules such as `StartDateTime < EndDateTime` require validation that considers more than one submitted value;
- when validation fails, the POST should return the same form with understandable errors, preserve valid entered values through `ModelState`, and repopulate supporting data such as Category choices;
- a successful mutation should normally use **POST/Redirect/GET** so the browser receives a fresh GET after persistence rather than remaining on the completed POST response;
- role authorization answers whether the current user has Organizer capability, while resource ownership answers whether that Organizer may act on a particular Event;
- ownership and current eligibility must be re-established on the server before an Edit mutation rather than inferred from a hidden field, a previously rendered page, or a guessed URL;
- anti-forgery protection belongs on state-changing forms; and
- validation or authorization failure must leave persisted data unchanged.

A useful mental model for the Organizer form workflow is:

```text
GET form request
	↓
server prepares input model + supporting choices
	↓
Razor renders form
	↓
user submits POST
	↓
model binding
	↓
server validation / ModelState
	↓
role + ownership + eligibility checks
	↓
server assigns or preserves protected fields
	↓
valid mutation is persisted
	↓
redirect to GET
```

The central design principle is that **submitted values describe what the user is asking to change; they do not determine what the user is allowed to change**. The server remains responsible for ownership, status, authorization, and every other protected invariant.

## Where CEMS Stands

CEMS now has its first Organizer-facing product workflow. An authenticated Organizer can open `/Events/Manage`, see only owned Events, create a new Event, receive server-side validation feedback, and edit an eligible owned Draft Event. New Events are owned by the authenticated Organizer and begin in `Draft` regardless of what a browser might attempt to submit.

The application also now has a reusable server-rendered form pattern built from `EventInputModel`, model binding, `ModelState`, Category-option repopulation, anti-forgery protection, ownership checks, and POST/Redirect/GET. No Lesson 5 migration is required because the input model is not an EF Core entity and this lesson does not change the existing Event schema.

At this boundary, Event editing eligibility is intentionally narrow:

```text
owned Draft Event		→ editable
owned Published Event	→ not yet edited by Lesson 5 workflow
owned Cancelled Event	→ not editable
another owner's Event	→ not accessible through Organizer Edit
```

CEMS still does not provide Publish, Cancel, Delete, Registration, or Administrator product workflows. The application can create and edit an owned Draft, but it does not yet model the allowed transitions that move an Event through its lifecycle.

That is the next problem the project must solve.

## Coming Next

In **Lesson 6 — Event Lifecycle, Business Rules, and Protected Mutations**, you will evolve Event management from Draft-only Create/Edit into an explicit lifecycle.

The key change is that operations such as Publish, Cancel, and Delete are not interchangeable CRUD updates. Each represents a specific state transition with its own authorization and business-rule preconditions:

```text
Draft
	├─ edit
	├─ publish → Published
	└─ delete	→ row removed when eligible

Published
	├─ edit when eligible
	└─ cancel	→ Cancelled

Cancelled
	└─ preserved, but not returned to Published
```

You will implement these changes as protected server-side mutations, re-query the persisted Event before changing it, enforce Organizer ownership and current Event state on every lifecycle POST, use anti-forgery protection, distinguish cancellation from permanent Draft deletion, and provide controlled success or expected-failure feedback.

Lesson 6 will also broaden Edit from the temporary Lesson 5 Draft-only rule to the lifecycle-aware Draft/Published policy while continuing to refuse Cancelled Events.

Two rules will remain deliberately incomplete because CEMS still has no Registration data: Draft deletion cannot yet prove that an Event has zero current registrations, and Published Capacity editing cannot yet compare a requested Capacity with a real registration count. Those gaps will remain visible rather than being simulated with fake data.

The key continuity is this: **Lesson 5 established how to accept a protected Event mutation safely. Lesson 6 will reuse that form, authorization, ownership, validation, and persistence discipline while adding explicit rules about which Event state transitions are allowed.**


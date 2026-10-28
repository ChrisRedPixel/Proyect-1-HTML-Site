# **Lesson 3 — Search, Filtering, Pagination, and View-Specific Models**

Revision: 2026.10.10

# Table of Contents

- [What You Will Learn](#what-you-will-learn)
- [Introduction](#introduction)
- [Part A — Understand the Search/Filter Request Before You Code](#part-a-understand-the-searchfilter-request-before-you-code)
  - [The product problem: narrow the catalog without changing what is public](#the-product-problem-narrow-the-catalog-without-changing-what-is-public)
  - [One request crosses several layers](#one-request-crosses-several-layers)
  - [Request state is different from persisted application data](#request-state-is-different-from-persisted-application-data)
  - [Query composition and materialization are different phases](#query-composition-and-materialization-are-different-phases)
  - [A page can need a shape that no single entity represents](#a-page-can-need-a-shape-that-no-single-entity-represents)
- [Part B — Guided Implementation](#part-b-guided-implementation)
- [1. Start from the Existing Database-Backed Catalog](#start-from-the-existing-database-backed-catalog)
- [2. Represent Filter State with a GET Request](#represent-filter-state-with-a-get-request)
  - [2.1 Add the optional action parameters](#add-the-optional-action-parameters)
  - [2.2 See how MVC binds the query string](#see-how-mvc-binds-the-query-string)
- [3. Build a Composable EF Core Query](#build-a-composable-ef-core-query)
- [4. Add Text Search](#add-text-search)
  - [Why `Description` is not included in this search](#why-description-is-not-included-in-this-search)
- [5. Add Category Filtering](#add-category-filtering)
- [6. Preserve Ordering and Keep Filtering in the Database](#preserve-ordering-and-keep-filtering-in-the-database)
- [7. Introduce `EventSearchViewModel`](#introduce-eventsearchviewmodel)
- [8. Update `EventsController.Index()`](#update-eventscontrollerindex)
  - [Why Categories use a separate query?](#why-categories-use-a-separate-query)
- [9. Build the GET Search and Filter Form](#build-the-get-search-and-filter-form)
  - [Why some instances of category do not have a @ and others do?](#why-some-instances-of-category-do-not-have-a-and-others-do)
  - [Why the field names matter?](#why-the-field-names-matter)
  - [Why the filter state remains visible?](#why-the-filter-state-remains-visible)
  - [Add the local styles](#add-the-local-styles)
  - [Why wwwroot/css/site.css?](#why-wwwrootcsssitecss)
  - [Accessible and responsive form behavior](#accessible-and-responsive-form-behavior)
- [10. Render Results and Handle No Matches](#render-results-and-handle-no-matches)
- [11. Retrieve One Page of Matching Events](#retrieve-one-page-of-matching-events)
  - [11.1 Choose How to Navigate a Large Result Set](#choose-how-to-navigate-a-large-result-set)
  - [11.2 Count the matching Events before selecting a page](#count-the-matching-events-before-selecting-a-page)
  - [11.3 Add page information to the View Model](#add-page-information-to-the-view-model)
  - [11.4 Bind and normalize the requested page](#bind-and-normalize-the-requested-page)
  - [11.5 Inspect the database queries](#inspect-the-database-queries)
- [12. Add Page Navigation to the Results View](#add-page-navigation-to-the-results-view)
  - [Add the pagination styles](#add-the-pagination-styles)
- [13. Trace One Search/Filter Request](#trace-one-searchfilter-request)
- [14. Guided Verification](#guided-verification)
  - [14.1 No filters](#no-filters)
  - [14.2 Search only](#search-only)
  - [14.3 Category only](#category-only)
  - [14.4 Search and Category together](#search-and-category-together)
  - [14.5 A valid Category with no public results](#a-valid-category-with-no-public-results)
  - [14.6 Search that matches only a non-public Event](#search-that-matches-only-a-non-public-event)
  - [14.7 Preserved filter state](#preserved-filter-state)
  - [14.8 Details behavior](#details-behavior)
  - [14.9 Add Development Events for a multi-page test](#add-development-events-for-a-multi-page-test)
  - [14.10 Navigate between pages and check ordering](#navigate-between-pages-and-check-ordering)
  - [14.11 Keep filters across pages and reset on a new search](#keep-filters-across-pages-and-reset-on-a-new-search)
  - [14.12 Check missing, invalid, and out-of-range page numbers](#check-missing-invalid-and-out-of-range-page-numbers)
    - [Fallback behavior is a design decision](#fallback-behavior-is-a-design-decision)
  - [14.13 Confirm one-page and zero-result behavior](#confirm-one-page-and-zero-result-behavior)
- [15. Small Student Challenge — Let the Visitor Choose the Page Size](#small-student-challenge-let-the-visitor-choose-the-page-size)
  - [15.1 Attempt the challenge before reading the solution](#attempt-the-challenge-before-reading-the-solution)
  - [15.2 Acceptance criteria](#acceptance-criteria)
  - [15.3 One possible solution](#one-possible-solution)
    - [15.3.1 Update the View Model with the available page-size choices](#update-the-view-model-with-the-available-page-size-choices)
    - [15.3.2 Replace the fixed page-size constant with a default and an allowed set](#replace-the-fixed-page-size-constant-with-a-default-and-an-allowed-set)
    - [15.3.3 Bind and validate `pageSize`](#bind-and-validate-pagesize)
    - [15.3.4 Use the validated page size for pagination](#use-the-validated-page-size-for-pagination)
    - [15.3.5 Add the page-size dropdown to the GET form](#add-the-page-size-dropdown-to-the-get-form)
    - [15.3.6 Add First and Last and preserve request state in navigation links](#add-first-and-last-and-preserve-request-state-in-navigation-links)
    - [15.3.7 Expand the filter layout for the new field](#expand-the-filter-layout-for-the-new-field)
    - [15.3.8 Show the current result range](#show-the-current-result-range)
  - [15.4 Verify your implementation](#verify-your-implementation)
  - [15.5 Improve Invalid Request Feedback with a Razor View](#improve-invalid-request-feedback-with-a-razor-view)
    - [Observe the original HTTP 400 response](#observe-the-original-http-400-response)
    - [Create a shared error View Model](#create-a-shared-error-view-model)
    - [Create a shared BadRequest Razor View](#create-a-shared-badrequest-razor-view)
    - [Return HTML while preserving HTTP 400](#return-html-while-preserving-http-400)
    - [Verify the improved error response](#verify-the-improved-error-response)
  - [Restore the original Development seed state](#restore-the-original-development-seed-state)
- [16. Final Code Check](#final-code-check)
  - [16.1 Final `ViewModels/EventSearchViewModel.cs`](#final-viewmodelseventsearchviewmodelcs)
  - [Final `ViewModels/ErrorPageViewModel.cs`](#final-viewmodelserrorpageviewmodelcs)
  - [16.2 Final `Controllers/EventsController.cs`](#final-controllerseventscontrollercs)
  - [16.3 Final `Views/Events/Index.cshtml`](#final-viewseventsindexcshtml)
  - [Final `Views/Shared/BadRequest.cshtml`](#final-viewssharedbadrequestcshtml)
  - [16.4 Final Lesson 3 additions to `wwwroot/css/site.css`](#final-lesson-3-additions-to-wwwrootcsssitecss)
- [17. Check Your Understanding](#check-your-understanding)
- [18. Common Mistakes](#common-mistakes)
  - [18.1 The form field name does not match the action parameter](#the-form-field-name-does-not-match-the-action-parameter)
  - [18.2 The query is allowed to become broader than the operation needs](#the-query-is-allowed-to-become-broader-than-the-operation-needs)
  - [18.3 `ToListAsync()` happens before the optional filters](#tolistasync-happens-before-the-optional-filters)
  - [18.4 Search accidentally exposes Draft or Cancelled Events](#search-accidentally-exposes-draft-or-cancelled-events)
  - [18.5 Category filtering uses the wrong Event property](#category-filtering-uses-the-wrong-event-property)
  - [18.6 One optional filter replaces the other](#one-optional-filter-replaces-the-other)
  - [18.7 The Index View still uses the Lesson 2 model](#the-index-view-still-uses-the-lesson-2-model)
  - [18.8 Search text disappears after submission](#search-text-disappears-after-submission)
  - [18.9 The selected Category is lost](#the-selected-category-is-lost)
  - [18.10 Category choices are empty](#category-choices-are-empty)
  - [18.11 Chronological ordering disappears](#chronological-ordering-disappears)
  - [18.12 A zero-result query shows a blank page](#a-zero-result-query-shows-a-blank-page)
  - [18.13 The total count is taken after `Skip()` and `Take()`](#the-total-count-is-taken-after-skip-and-take)
  - [18.14 Page retrieval has no stable tie-breaker](#page-retrieval-has-no-stable-tie-breaker)
  - [18.15 Pagination links discard active request state](#pagination-links-discard-active-request-state)
  - [18.16 A changed form request retains the previous page number](#a-changed-form-request-retains-the-previous-page-number)
  - [18.17 No-results pages still show pagination links](#no-results-pages-still-show-pagination-links)
  - [18.18 The page-size dropdown is trusted without server-side validation](#the-page-size-dropdown-is-trusted-without-server-side-validation)
  - [18.19 Page-size state is lost during navigation](#page-size-state-is-lost-during-navigation)
  - [18.20 The error View accidentally returns HTTP 200](#the-error-view-accidentally-returns-http-200)
- [What You Will Build](#what-you-will-build)
- [19. What You Built](#what-you-built)
- [20. Summary](#summary)
  - [What You Learned](#what-you-learned)
  - [Where CEMS Stands](#where-cems-stands)
  - [Coming Next](#coming-next)


<br>

<a id="what-you-will-learn"></a>
# What You Will Learn

After completing the lesson, you should be able to:

1. choose appropriate HTTP semantics for read-only operations and represent retrieval criteria as request state rather than persisted domain state
2. distinguish persisted domain data, transient request state, and response/presentation state, and design a response-specific model when one screen needs data from multiple concerns
3. preserve existing authorization and visibility invariants while composing optional filters that only narrow an already-allowed result set
4. distinguish query composition from materialization, identify the data-access boundary, and apply filtering and result bounds before materialization to reduce over-fetching and unnecessary resource use
5. separate the responsibilities of filtering, counting, deterministic ordering, and bounded retrieval when designing a paginated data flow
6. explain why pagination requires a stable sort order and use a deterministic tie-breaker so page boundaries remain predictable
7. compare navigation patterns such as numbered pages and incremental scrolling independently from retrieval strategies such as offset and cursor/keyset pagination
8. design server-side validation for client-controlled request values, including defaults, allowed values, bounds, normalization, and rejection behavior
9. propagate request state consistently across forms and navigation while deliberately resetting dependent state when retrieval criteria change
10. derive navigation targets, total-page metadata, and visible result ranges from canonical pagination state instead of duplicating or hard-coding presentation logic
11. distinguish valid empty results from malformed or invalid requests, and choose fallback versus explicit error responses as a documented request-contract and observability decision
12. trace one request end to end across browser input, request binding, validation, query construction, database execution, response shaping, and rendered output, assigning each responsibility to the appropriate layer
13. design and execute positive, boundary, and negative tests for search and pagination behavior, including manually constructed requests that bypass the rendered user interface
14. recognize resource bounding as both a performance and defensive-design concern when user-controlled inputs affect query breadth or response size

<br>

<a id="introduction"></a>
# Introduction

In Lesson 2, you replaced the temporary Event data with a real relational catalog backed by a DBMS and Entity Framework Core. The `/Events` page now loads persisted Events, shows only Published upcoming Events, orders them chronologically, and links each result to its existing Details page.

That catalog works, but every visitor currently sees the same complete public list. In this lesson, you will evolve the existing Index flow so a visitor can narrow the catalog by text and Category, then move between manageable pages of matching results. The database will apply the filters and return only the Events needed for the current page.

---

<br>

<a id="part-a-understand-the-searchfilter-request-before-you-code"></a>
# Part A — Understand the Search/Filter Request Before You Code

Before changing `EventsController`, build a mental model of what this feature is doing. Search, filtering, and pagination may look like small page changes, but one request crosses several application boundaries: HTTP, MVC model binding, EF Core query composition, SQL execution, controller-to-View data transfer, and Razor rendering. Understanding those boundaries makes the code easier to reason about and helps prevent subtle performance and security problems.

<br>

<a id="the-product-problem-narrow-the-catalog-without-changing-what-is-public"></a>
## The product problem: narrow the catalog without changing what is public

At the end of Lesson 2, CEMS already has a public catalog rule: normal visitors may browse Events that are **Published and upcoming**. Lesson 3 does not redefine that rule. It adds optional criteria that let a visitor ask for a smaller subset of that already-eligible public catalog.

A useful way to think about the result is:

```text
publicly eligible Events
	AND optional text criterion
	AND optional Category criterion
	= matching Events

chronological ordering + requested page
	= Events rendered for this request
```

If neither optional criterion is supplied, the result is simply the existing public catalog. If one or both are supplied, they narrow that catalog. A search term must never make a Draft, Cancelled, or otherwise ineligible Event public merely because some text happens to match.

This gives the lesson an important invariant:

> search and Category filtering may narrow public visibility but they must never widen public visibility

The SRS expresses the visitor-facing behavior in `FR-SRH-005` (text search), `FR-SRH-006` (Category filtering), `FR-SRH-007` (no-results feedback), and `FR-SRH-009` (paginated browsing). Together, these requirements describe one public catalog: the filters determine which Events match, and pagination determines which portion of those matches the visitor sees.

<br>

<a id="one-request-crosses-several-layers"></a>
## One request crosses several layers

A completed request will eventually follow this path:

```text
Browser sends GET /Events?search=...&categoryId=...&page=...
		↓
MVC binds query-string values to action parameters
		↓
EventsController starts with the Published/upcoming query
		↓
optional predicates are composed onto IQueryable<Event>
		↓
CountAsync() obtains the total number of matching Events
		↓
OrderBy + ThenBy establish a stable chronological sequence
		↓
Skip + Take select the requested page
		↓
ToListAsync() retrieves only that page's Event rows
		↓
controller combines results + Category choices + filter/page state
		↓
EventSearchViewModel
		↓
Razor renders the form, results, and page navigation
		↓
HTML response returns to the browser
```

Notice that the browser does not receive the full Event table and then decide which rows to hide. The server receives the criteria, the database query is constrained, and the application materializes only the requested page of matching Events.

<br>

<a id="request-state-is-different-from-persisted-application-data"></a>
## Request state is different from persisted application data

The search text, selected Category, and requested page number are **request state**. They describe what the visitor asked to see in this request. They are not new properties of an Event and do not need a database migration.

By contrast, `Event`, `Category`, `EventStatus`, and the relationships introduced earlier represent persisted application data and domain rules. Lesson 3 will keep those entities unchanged while adding temporary page state around them.

This distinction becomes important later when the Index page needs a View Model. The page will need both kinds of information at the same time: persisted domain data to display and transient request state to reconstruct the current filter controls.

<br>

<a id="query-composition-and-materialization-are-different-phases"></a>
## Query composition and materialization are different phases

EF Core lets the application build an `IQueryable<Event>` before the matching rows are loaded into application memory. Think of the `IQueryable` as a description of database work that EF Core can continue extending. Calls such as `Where(...)` and `OrderBy(...)` can contribute to that description.

`ToListAsync()` is the important boundary for retrieving Events. It materializes the query result: EF Core sends the composed database operation to SQL Server, receives rows, and creates Event objects in application memory. After that point, ordinary LINQ over the resulting `List<Event>` operates on in-memory objects unless another database query is started.

For pagination, `CountAsync()` performs a separate database operation that returns one number: how many Events satisfy the visibility and optional filter predicates. `Skip()` and `Take()` remain part of the Event query, so `ToListAsync()` retrieves one page instead of every matching Event. Ordering by start time and then `Id` makes the boundary between pages predictable even when two Events begin at the same time.

For this workflow, keeping applicable predicates before materialization matters for both performance and security. A narrowly composed database query avoids unnecessary transfer and processing, reduces over-fetching, limits unnecessary data residency in application memory, and reduces exposure to resource-exhaustion problems caused by unnecessarily broad operations. These concerns become more important as data volume and request concurrency grow.

Do not confuse that query-constraining concern with the public-visibility rule. The `Published` and upcoming predicate determines which Events belong in the public browsing set at all. The text and Category predicates constrain the requested result. Both ideas matter, but they protect different properties of the system.

<br>

<a id="a-page-can-need-a-shape-that-no-single-entity-represents"></a>
## A page can need a shape that no single entity represents

The final Index page will need several things at once:

```text
Events on the current page
Category choices
current search text
current selected Category
current page and page size
total matching Events and total pages
```

No single `Event` entity represents that page. An Event describes one domain object; it should not acquire properties merely because one Razor page needs search criteria, dropdown state, or page-navigation information. Later in the lesson, `EventSearchViewModel` will model the **response needs of this page** without changing the persistence model.

With that mental model in place, you can now evolve the existing Lesson 2 request one responsibility at a time.

---

<br>

<a id="part-b-guided-implementation"></a>
# Part B — Guided Implementation

<a id="start-from-the-existing-database-backed-catalog"></a>
# 1. Start from the Existing Database-Backed Catalog

Your current public `Index()` action from Lesson 2 is:

**File: `Controllers/EventsController.cs`**

```csharp
public async Task<IActionResult> Index()
{
	_logger.LogInformation("Loading Published upcoming CEMS events.");

	var now = DateTime.Now;

	var query = _context.Events
		.AsNoTracking()
		.Include(eventItem => eventItem.Category)
		.Where(eventItem =>
			eventItem.Status == EventStatus.Published &&
			eventItem.StartDateTime > now)
		.OrderBy(eventItem => eventItem.StartDateTime)
		.ThenBy(eventItem => eventItem.Id);
				
	var events = await query.ToListAsync();

	return View(events);
}
```

This query already protects important public-browsing behavior:

```text
Published AND StartDateTime > now
```

The new search and Category filters must **narrow this valid public query**. They must not replace it. Treat the existing Published/upcoming predicate as an invariant carried forward from Lesson 2. The optional filters answer a different question: among Events that are already eligible for public browsing, which ones match this visitor's current criteria?

That distinction matters. A Draft or Cancelled Event must remain absent even if its title or location happens to match the user's search text. Search affects relevance within the public catalog; it does not decide what is authorized for public display.

---

<br>

<a id="represent-filter-state-with-a-get-request"></a>
# 2. Represent Filter State with a GET Request

Search and filtering are read-only operations. The visitor is asking the application to return a different representation of the same public catalog; the visitor is not creating, editing, publishing, cancelling, or deleting data.

That makes the filter values a natural part of the GET request. They describe the representation the visitor wants returned, and they can be represented in the URL without modifying persisted application state. A URL such as `/Events?search=Library&categoryId=1` therefore means, conceptually, "show me the Events representation produced with these criteria."

The controller cannot apply criteria it never receives, so the first implementation step is to let `Index()` accept the two optional request values. At this stage you will change only the request boundary; the database query will remain unchanged so you can observe one responsibility at a time.

<br>

<a id="add-the-optional-action-parameters"></a>
## 2.1 Add the optional action parameters

Open:

**File: `Controllers/EventsController.cs`**

Change:

```csharp
public async Task<IActionResult> Index()
```

to:

```csharp
public async Task<IActionResult> Index(string? search, int? categoryId)
```

Both parameters are nullable because a visitor may use:

- neither filter;
- search only;
- Category only;
- both filters.

<br>

<a id="see-how-mvc-binds-the-query-string"></a>
## 2.2 See how MVC binds the query string

Now that the parameter names have been introduced, consider these requests.

With no query string:

```text
/Events
```

MVC supplies:

```text
search = null
categoryId = null
```

With a text criterion:

```text
/Events?search=Library
```

MVC supplies:

```text
search = "Library"
categoryId = null
```

The existing conventional route does not change. Query-string values are separate from the route pattern and can be bound to action parameters with matching names. Because GET places the current criteria in the URL, refreshing the page can repeat the same read-only request, and a filtered URL remains meaningful to bookmark or share.

Build the project:

```cmd
dotnet build
```

At this point, the page behaves exactly as it did at the end of Lesson 2 because the two new parameters are not used yet.

---

<br>

<a id="build-a-composable-ef-core-query"></a>
# 3. Build a Composable EF Core Query

The current Lesson 2 code materializes the query into a list. It separates **query composition** from **query execution**.

This line:

```csharp
var query = _context.Events
	.AsNoTracking()
	.Include(eventItem => eventItem.Category)
	.Where(eventItem =>
		eventItem.Status == EventStatus.Published &&
		eventItem.StartDateTime > now)
	.OrderBy(eventItem => eventItem.StartDateTime)
	.ThenBy(eventItem => eventItem.Id);
```

creates a query that can still be extended. The variable does not yet contain a finished collection of Event objects. Instead, it represents a query expression that EF Core can continue composing.

The application has described part of the database operation it wants EF Core to perform. The query says which entity set to read, that the operation is read-only, that Category data is needed, and which Events are eligible for public browsing. It has not yet produced the final `List<Event>`.

The materialization point remains:

```csharp
.ToListAsync()
```

That call crosses an important boundary. Before materialization, additional LINQ operators can still contribute to the database query. At materialization, EF Core translates the composed query for the configured provider, executes it, receives the matching rows, and creates the in-memory objects returned to your application.

A useful comparison is:

```text
IQueryable<Event>
	= a database query that can still be composed

List<Event>
	= Event objects that have already been materialized in memory
```

This distinction matters because the optional conditions can become part of the database operation instead of being applied only after a broader result set has already crossed the database/application boundary. Keeping the query constrained before materialization reduces unnecessary database-to-application transfer and in-memory processing. It also supports safer query behavior by reducing over-fetching, unnecessary data residency in application memory, and avoidable resource pressure from broad result sets.

You now have the structure needed for optional criteria. The next step is to add one predicate at a time while preserving the existing public-browsing invariant.

---

<br>

<a id="add-text-search"></a>
# 4. Add Text Search

The controller can now receive `search`, and the Event query can remain composable until materialization. Those two pieces can now be connected.

The public search requirement uses text that matches at least the Event title or location. In this lesson, the search definition is exactly those two fields. The text criterion is optional, so it should add a predicate only when the visitor supplied meaningful text; otherwise the base public query should remain unchanged.

Open:

**File: `Controllers/EventsController.cs`**

```csharp
public async Task<IActionResult> Index(string? search, int? categoryId)
{
	_logger.LogInformation("Loading Published upcoming CEMS events.");

	var now = DateTime.Now;

	var query = _context.Events
			.AsNoTracking()
			.Include(eventItem => eventItem.Category)
			.Where(eventItem =>
				eventItem.Status == EventStatus.Published &&
				eventItem.StartDateTime > now);

	if (!string.IsNullOrWhiteSpace(search))
	{
			var searchTerm = search.Trim();

			query = query.Where(eventItem =>
				eventItem.Title.Contains(searchTerm) ||
				eventItem.Location.Contains(searchTerm));
	}

	var events = await query
			.OrderBy(eventItem => eventItem.StartDateTime)
			.ThenBy(eventItem => eventItem.Id)
			.ToListAsync();

	return View(events);
}
```

This guard:

```csharp
if (!string.IsNullOrWhiteSpace(search))
```

means null, empty, or whitespace-only input behaves as **no text filter**.

For a real search term, the added predicate means:

```text
Title contains the search term
OR
Location contains the search term
```

The search predicate narrows the already-valid public query. It does not replace:

```text
Published AND upcoming
```

<a id="why-description-is-not-included-in-this-search"></a>
## Why `Description` is not included in this search

The search intentionally checks only `Title` and `Location`. Those fields define the public text-search behavior implemented in this lesson and represent short, focused values that a visitor is likely to use when looking for an Event. `FR-SRH-005` requires search text to match **at least** the Event title or location, so this implementation satisfies the required minimum directly.

It would be easy to extend the predicate:

```csharp
query = query.Where(eventItem =>
	eventItem.Title.Contains(searchTerm) ||
	eventItem.Location.Contains(searchTerm) ||
	eventItem.Description.Contains(searchTerm));
```

However, adding `Description` changes more than the number of fields being searched. A description is free-form text and can contain substantially more content than a title or location. A substring search over that text asks the database to examine a broader body of data for every candidate Event. As the number of Events and the size of their descriptions grow, that additional work can make this style of search increasingly expensive.

This does **not** mean that descriptions should never be searchable. The SRS does not prohibit searching additional fields. It means that broad free-text search deserves an intentional design. A larger application might introduce database full-text search, a dedicated search service, or another indexing strategy designed for searching substantial text efficiently.

For CEMS, the required behavior is deliberately focused: search must match at least the Event `Title` or `Location`. Keeping `Description` out of this lesson's predicate implements that required behavior directly while keeping the first search implementation straightforward and bounded in scope.

Reset the database:

```cmd
dotnet ef database drop --force -- --environment Development
dotnet ef database update -- --environment Development
```

Run the application:

```cmd
dotnet run
```

Request:

```text
/Events?search=Library
```

With the Lesson 2 seed data, **Library Research Night** should match.

Then request:

```text
/Events?search=Student+Center
/Events?search=Student%20Center
```

**Career Networking Lunch** should match because of its location.

The database also contains **Welcome Mixer Planning Draft** at a Student Center location, but that Event must remain absent because the base query already requires `EventStatus.Published`.

The Razor page still has no search form. For now, you are testing the server behavior directly through the URL.

---

<br>

<a id="add-category-filtering"></a>
# 5. Add Category Filtering

Text search now narrows the base query when text is present, but the second request value, `categoryId`, is still unused. Add it to the **same** query rather than starting a second query. That way, when both criteria are present, EF Core can compose them together as constraints on one result set.

The second optional criterion uses the existing `CategoryId` foreign key from the Lesson 2 `Event` entity. No new relationship or database change is necessary.

Open:

**File: `Controllers/EventsController.cs`**

After the text-search block and before ordering, add:

```csharp
if (categoryId.HasValue)
{
	query = query.Where(eventItem =>
		eventItem.CategoryId == categoryId.Value);
}
```

If no Category is supplied, the second `if` block does nothing.

If a Category is supplied, the Event must also satisfy:

```csharp
eventItem.CategoryId == categoryId.Value
```

When both optional values are present, both conditions become part of the query:

```text
Published
AND upcoming
AND (title contains search OR location contains search)
AND CategoryId matches
```

Neither optional filter replaces the other.

---

<br>

<a id="preserve-ordering-and-keep-filtering-in-the-database"></a>
# 6. Preserve Ordering and Keep Filtering in the Database

Filtering determines **which** Events remain. Ordering determines the sequence in which those remaining Events appear.

As we saw in Lesson 02, EF Core can show the SQL corresponding to an `IQueryable` using `query.ToQueryString();` for temporary inspection. You may also see the final executed SQL in the log.

Compare with premature materialization. The following example is an **instructional contrast only**. Do not leave it in your application.

```csharp
var events = await _context.Events
	.AsNoTracking()
	.Include(eventItem => eventItem.Category)
	.Where(eventItem =>
		eventItem.Status == EventStatus.Published &&
		eventItem.StartDateTime > now)
	.ToListAsync();

if (!string.IsNullOrWhiteSpace(search))
{
	var searchTerm = search.Trim();

	events = events
		.Where(eventItem =>
			eventItem.Title.Contains(searchTerm) ||
			eventItem.Location.Contains(searchTerm))
		.ToList();
}

if (categoryId.HasValue)
{
	events = events
		.Where(eventItem => eventItem.CategoryId == categoryId.Value)
		.ToList();
}
```

The problem **for this search/filter workflow** is the position of:

```csharp
.ToListAsync()
```

It runs before the optional search and Category predicates. The DBMS therefore returns the broad Published/upcoming set first, and the later optional filters run over C# objects in application memory.

For a tiny development seed set, that difference may look harmless. In a real system, however, the placement of the materialization boundary determines how much data can cross from the database into the application before the user's criteria are applied. An unnecessarily broad materialization can increase database work, network transfer, allocation, garbage collection, and request latency.

It can also become security-relevant. Over-fetching can bring data into transit or application memory that the current operation did not need, and unbounded or unnecessarily broad result sets can contribute to memory, CPU, and concurrency pressure that an attacker or accidental high-volume usage could amplify. Query constraints are therefore part of responsible resource and data-exposure control, not merely an optimization detail.

<br>

> **NOTE:** This does not mean every use of in-memory filtering is universally wrong. Sometimes the application intentionally operates on an already-small, already-authorized in-memory collection. It means that for this database search/filter requirement, the search and Category constraints belong naturally in the database query before materialization. The application should ask the database for the result it actually needs rather than routinely loading a broader set and narrowing it afterward.

---

<br>

<a id="introduce-eventsearchviewmodel"></a>
# 7. Introduce `EventSearchViewModel`

The Event query now works, but the Index page needs more than the matching Events.

The page must also render:

```text
Category choices
current search text
current selected Category
```

A collection of Events alone cannot carry all of that page-specific state cleanly. This is the point where a View Model becomes useful.

The need appears because the **shape of a web page is not always the shape of a database entity**. An `Event` represents one Event in the application domain. The Index page, however, represents an interaction: it needs many Events, the available Category choices, and the request values that produced the current result set. Those concerns belong to the page response, not to one Event row.

Create:

**File: `ViewModels/EventSearchViewModel.cs`**

```csharp
using CampusEvents.Models;
using System;
using System.Collections.Generic;

namespace CampusEvents.ViewModels;

public class EventSearchViewModel
{
	public IReadOnlyList<Event> Events { get; set; } = Array.Empty<Event>();

	public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();

	public string? Search { get; set; }

	public int? CategoryId { get; set; }
}
```

Each property has one page-level responsibility:

- `Events` — the filtered, chronologically ordered Event results;
- `Categories` — the choices for the Category filter;
- `Search` — the current search text;
- `CategoryId` — the currently selected Category ID.

This View Model is not a database entity. It does not replace `Event` or `Category`, and it does not belong in `ApplicationDbContext`. It describes the data needed to render one page.

Keep the responsibilities distinct:

```text
Event / Category
	→ domain and persistence data

EventSearchViewModel
	→ presentation data for one Index response
```

This separation prevents the persistence model from accumulating UI-only properties such as search text or selected dropdown values. It also gives the View a strongly typed object whose shape matches what that View actually needs.

At this point the application has the right query behavior and the right page-data shape, but they are still disconnected. The controller must now assemble the View Model from the current request and the database results.

Build the project:

```cmd
dotnet build
```

The new class should compile. It is not connected to the controller or View until the next steps.

---

<br>

<a id="update-eventscontrollerindex"></a>
# 8. Update `EventsController.Index()`

The controller is the point where the request criteria, EF Core queries, and presentation model come together. It will not make Razor query the database, and it will not make the entities carry UI state. Instead, it will perform the data-access work and construct the object the View needs for this response.

The controller now needs to:

1. compose and materialize the filtered Event query;
2. load the available Categories;
3. place both result data and current filter state into the new View Model.

Open:

**File: `Controllers/EventsController.cs`**

Add this namespace:

```csharp
using CampusEvents.ViewModels;
```

Then use this `Index()` implementation:

```csharp
public async Task<IActionResult> Index(string? search, int? categoryId)
{
	_logger.LogInformation(
		"Loading Published upcoming CEMS events with public filters.");

	var now = DateTime.Now;

	var query = _context.Events
		.AsNoTracking()
		.Include(eventItem => eventItem.Category)
		.Where(eventItem =>
			eventItem.Status == EventStatus.Published &&
			eventItem.StartDateTime > now);

	if (!string.IsNullOrWhiteSpace(search))
	{
		var searchTerm = search.Trim();

		query = query.Where(eventItem =>
			eventItem.Title.Contains(searchTerm) ||
			eventItem.Location.Contains(searchTerm));
	}

	if (categoryId.HasValue)
	{
		query = query.Where(eventItem =>
			eventItem.CategoryId == categoryId.Value);
	}

	var events = await query
		.OrderBy(eventItem => eventItem.StartDateTime)
		.ThenBy(eventItem => eventItem.Id)
		.ToListAsync();

	var categories = await _context.Categories
		.AsNoTracking()
		.OrderBy(category => category.Name)
		.ToListAsync();

	var viewModel = new EventSearchViewModel
	{
		Events = events,
		Categories = categories,
		Search = search,
		CategoryId = categoryId
	};

	return View(viewModel);
}
```

<br>

<a id="why-categories-use-a-separate-query"></a>
## Why Categories use a separate query?

The Event query answers:

```text
Which public Events match the current criteria?
```

The Category query answers:

```text
Which Categories should the filter control offer?
```

Those are separate page-data needs.

The Category query is also read-only, so it uses `AsNoTracking()`. It orders Categories by name so the dropdown is predictable.

The controller then preserves the current request state:

```csharp
Search = search,
CategoryId = categoryId
```

That state will let Razor redisplay the values that produced the current result set. This is the beginning of the request/response round trip: query-string values entered by the visitor become action parameters, then View Model properties, then rendered form values in the response.

The controller now returns:

```csharp
return View(viewModel);
```

The old Index View still expects a collection of Events. The controller and View therefore disagree about the model type at this checkpoint. Update the View next before trying to run the page so both sides agree on `EventSearchViewModel`.

---

<br>

<a id="build-the-get-search-and-filter-form"></a>
# 9. Build the GET Search and Filter Form

The server can now accept criteria, apply them in the database query, and return those criteria in the View Model. What is still missing is the browser-side control that produces the GET request. The form is therefore not a separate filtering system; it is the user interface for constructing the query string that the controller already knows how to process.

Open:

**File: `Views/Events/Index.cshtml`**

The Lesson 2 model declaration was:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>
```

Replace it with:

```cshtml
@model CampusEvents.ViewModels.EventSearchViewModel
```

Keep the existing page heading:

```cshtml
<h1>Upcoming Campus Events</h1>
```

Then add the search/filter form immediately below it:

```cshtml
<form asp-controller="Events"
		asp-action="Index"
		method="get"
		class="event-filter-form"
		aria-label="Search and filter campus events">

	<div class="event-filter-field">
		<label for="search" class="event-filter-label">Search title or location</label>
		<input id="search"
				name="search"
				type="search"
				class="event-filter-control"
				value="@Model.Search"
				aria-describedby="searchHelp" />
		<div id="searchHelp" class="event-filter-help">
			Matches Event title or location.
		</div>
	</div>

	<div class="event-filter-field">
		<label for="categoryId" class="event-filter-label">Category</label>
		<select id="categoryId"
				name="categoryId"
				class="event-filter-control">
			<option value="">All categories</option>

			@foreach (var category in Model.Categories)
			{
				if (Model.CategoryId == category.Id)
				{
					<option value="@category.Id" selected>@category.Name</option>
				}
				else
				{
					<option value="@category.Id">@category.Name</option>
				}
			}
		</select>
	</div>

	<div class="event-filter-actions">
		<button type="submit" class="event-filter-submit">
			Apply filters
		</button>

		<a asp-controller="Events"
			 asp-action="Index"
			 class="event-filter-clear">
			Clear
		</a>
	</div>
</form>
```

<br>

<a id="why-some-instances-of-category-do-not-have-a-and-others-do"></a>
## Why some instances of category do not have a @ and others do?

In Razor, `@` tells the Razor engine to switch from HTML to C#. In your code, `category` is used in two different contexts:

1. Inside a C# code block, no `@` is needed.

```cshtml
@foreach (var category in Model.Categories)
{
    if (Model.CategoryId == category.Id)
    {
        // Already inside C#
    }
}
```

The `@foreach` switches Razor into C# mode. Inside its braces, `category.Id` is already interpreted as C#.

2. Inside HTML, `@` is required to output a C# value.

```cshtml
<option value="@category.Id">@category.Name</option>
```

Here, Razor is processing HTML, so you need `@` to tell it to evaluate `category.Id` and `category.Name` as C# expressions.

The `<option>` element is physically inside the foreach C# block, but Razor automatically switches back to HTML mode when it encounters an HTML element.

The resulting HTML might look like this:

```html
<option value="5">Music</option>
```

The general rule: Use `@` when transitioning from HTML to C#. Once you're already inside C# code, you don't need it again unless you've transitioned back into HTML.

<br>

<a id="why-the-field-names-matter"></a>
## Why the field names matter?

The form controls use:

```text
name="search"
name="categoryId"
```

Those names match the controller parameters:

```csharp
Index(string? search, int? categoryId)
```

When the browser submits the GET form, the resulting URL can contain values such as:

```text
/Events?search=Workshop&categoryId=1
```

MVC can then bind those named query-string values back to the action parameters.

<br>

<a id="why-the-filter-state-remains-visible"></a>
## Why the filter state remains visible?

The text box uses:

```cshtml
value="@Model.Search"
```

The controller stored the submitted text in `Model.Search`, so Razor puts that text back into the control.

For the Category dropdown, Razor compares:

```cshtml
Model.CategoryId == category.Id
```

and renders `selected` on the matching option.

The round trip is:

```text
query string
	↓
Index parameters
	↓
EventSearchViewModel
	↓
rendered form values
```

The page therefore shows the visitor which criteria produced the current results.

<br>

<a id="add-the-local-styles"></a>
## Add the local styles

Use the existing project stylesheet under `wwwroot`.

Open:

**File: `wwwroot/css/site.css`**

Append these Lesson 3 styles. Do not replace unrelated styles that are already in the file.

```css

.event-filter-form {
	display: grid;
	grid-template-columns:
		minmax(15rem, 2fr)
		minmax(12rem, 1fr)
		minmax(14rem, auto);
	gap: 1rem;
	margin-bottom: 1.5rem;
	align-items: start;
}

.event-filter-field {
	min-width: 0;
}

.event-filter-label {
	display: block;
	margin-bottom: 0.35rem;
	font-weight: 600;
	line-height: 1.5;
}

.event-filter-control {
	width: 100%;
	box-sizing: border-box;
	padding: 0.55rem 0.65rem;
	font: inherit;
	border: 1px solid #767676;
	border-radius: 0.25rem;
	background-color: #fff;
	color: #111;
}

.event-filter-help {
	margin-top: 0.35rem;
	font-size: 0.875rem;
}

.event-filter-actions {
	display: flex;
	gap: 0.5rem;
	padding-top: calc(1.5em + 0.35rem);
}

.event-filter-submit,
.event-filter-clear {
	flex: 1;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	box-sizing: border-box;
	min-height: 2.5rem;
	padding: 0.55rem 0.75rem;
	border: 1px solid currentColor;
	border-radius: 0.25rem;
	font: inherit;
	text-decoration: none;
	cursor: pointer;
}

.event-filter-submit {
	background-color: #222;
	color: #fff;
}

.event-filter-clear {
	background-color: #fff;
	color: #222;
}

.event-result-count {
	margin: 0 0 1rem;
}

.event-card {
	margin-bottom: 1.5rem;
}

.event-card-title {
	margin: 0 0 0.5rem;
	font-size: 1.25rem;
}

.cems-visually-hidden {
	position: absolute;
	width: 1px;
	height: 1px;
	padding: 0;
	margin: -1px;
	overflow: hidden;
	clip: rect(0, 0, 0, 0);
	white-space: nowrap;
	border: 0;
}

@media (max-width: 47.99rem) {
	.event-filter-form {
		grid-template-columns: 1fr;
	}

	.event-filter-actions {
		align-items: stretch;
		padding-top: 0;
	}
}
```

<br>

<a id="why-wwwrootcsssitecss"></a>
## Why wwwroot/css/site.css?

The template used for all pages layout `Views/Shared/_Layout.cshtml` contains the following HTML code:

```html
<!DOCTYPE html>
<html lang="en">

<head>
	...
	<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
	...
</head>

```

<br>

<a id="accessible-and-responsive-form-behavior"></a>
## Accessible and responsive form behavior

The form uses visible `<label>` elements for both controls.

The local `.event-filter-form` rule uses CSS Grid for the wider layout. The media query changes that grid to a single column on narrow viewports, so each major control group uses the available width.

---

<br>

<a id="render-results-and-handle-no-matches"></a>
# 10. Render Results and Handle No Matches

The form can now produce a filtered GET request and reconstruct the submitted criteria from the View Model. The remaining View responsibility is to render the result set itself.

Because the Index View now receives the page-specific View Model, the Event collection is:

```csharp
Model.Events
```

Change the old loop:

```cshtml
@foreach (var eventItem in Model)
```

to:

```cshtml
@foreach (var eventItem in Model.Events)
```

Keep the existing Lesson 2 result markup and Details link.

Then add an explicit no-results branch.

**File: `Views/Events/Index.cshtml`**

```cshtml
@if (Model.Events.Count == 0)
{
	<p role="status">
		No published upcoming events match your current criteria.
	</p>
}
else
{
	<section aria-labelledby="event-list-heading">
		<h2 id="event-list-heading" class="cems-visually-hidden">
			Published upcoming events
		</h2>

		@foreach (var eventItem in Model.Events)
		{
			<article class="event-card">
				<h3 class="event-card-title">
					<a asp-controller="Events"
						 asp-action="Details"
						 asp-route-id="@eventItem.Id">
						@eventItem.Title
					</a>
				</h3>

				<p><strong>Category:</strong> @eventItem.Category.Name</p>
				<p><strong>Location:</strong> @eventItem.Location</p>
				<p>
					<strong>Starts:</strong>
					<time>@eventItem.StartDateTime.ToString("f")</time>
				</p>
			</article>
		}
	</section>
}
```

A query that returns zero rows can still be completely successful. The request was valid, the database query ran correctly, the controller handled the empty collection correctly, and Razor returned valid HTML. "No matches" is therefore a **domain result**, not an infrastructure failure. The page should communicate that result to the visitor rather than treating it as an exception. The search/filter form stays visible above this branch, so the visitor can immediately change the criteria.

Build and run:

```cmd
dotnet build
dotnet run
```

Open:

```text
/Events
```

With the Lesson 2 seed data, you should still see:

1. Library Research Night
2. Career Networking Lunch
3. Study Skills Workshop

The search/filter form should appear above the results.

---

<br>

<a id="retrieve-one-page-of-matching-events"></a>
# 11. Retrieve One Page of Matching Events

Your search and Category filters now produce the correct matching set, and Razor can show that set. With three Published upcoming Events in the initial seed data, the whole list still fits comfortably on one screen. As the catalog grows, however, the same query could return dozens or hundreds of matching Events. Showing all of them in one response makes the page longer and transfers rows the visitor may not need yet.

**Pagination** divides the ordered result into smaller groups called pages. CEMS will show ten Events at a time. A visitor can request another page using the same GET-based approach already used for search and Category filtering.

For example:

```text
/Events?search=Workshop&categoryId=1&page=2
```

The request asks for the second page of Published upcoming Events that match both criteria. `page` is another request value, not a property of an Event.

<br>

<a id="choose-how-to-navigate-a-large-result-set"></a>
## 11.1 Choose How to Navigate a Large Result Set

When a catalog contains more results than one screen can show comfortably, the application needs a way to reveal additional Events. The word **pagination** is sometimes used for several related ideas, but three decisions are involved: how visitors navigate, how the database selects results, and how the browser renders them. Separating those decisions helps you understand why the same database technique can support different interfaces.

<br>

**How does the visitor move through the results?**

With **numbered pagination**, the visitor moves between distinct result pages using links such as **Previous**, **Next**, or a specific page number. A page number can appear in a URL, so a visitor can bookmark or share a particular position in a filtered catalog. In CEMS, each navigation will request a new page of matching Events.

With **infinite scrolling**, the page reveals more results as the visitor approaches the end of the current list. The newly retrieved items are usually appended below the existing ones. A related interface uses an explicit **Load more** button instead of loading automatically. These interfaces can work well for a continuously browsed feed, although returning to a particular position or reaching the page footer can require additional care.

<br>

**How does the database find the next group?**

With **offset-based pagination**, the query sorts the matching rows, skips a specified number, and takes the next group. CEMS will use `Skip()` and `Take()` for this purpose. If 27 Events match and the page size is ten, page 2 skips the first ten rows and retrieves rows 11–20. A separate count lets the page display the total number of matches and calculate how many numbered pages exist.

With **cursor-based pagination**, also called **keyset pagination**, the next request supplies a position in the ordered results instead of a page number. For a catalog ordered by `StartDateTime` and then `Id`, that position can be based on the start time and ID of the last Event returned. The next query selects Events that come *after* those ordering values and takes the next ten. It does not need to skip every preceding row. This can be efficient when moving deeply through a large catalog and can reduce page-boundary shifts when new Events are inserted before the cursor. The tradeoff is that a cursor naturally supports moving forward from a known position, not jumping directly to an arbitrary numbered page.

<br>

**How does the browser display the new results?**

A **server-side rendered (SSR)** application can follow a normal link, request another page, and receive freshly rendered HTML. A **client-side rendered (CSR)** or hybrid application can request additional data or an HTML fragment and update the existing page without replacing the entire document. These rendering approaches do not determine the database technique: an SSR application can use a cursor with a **Next** link, and an infinite-scrolling interface can use offset-based queries.

The three decisions can be combined in different ways:

<br>

| Visitor experience | Database retrieval | Browser behavior |
|---|---|---|
| Numbered pages | Offset: order, skip, and take | A newly rendered page |
| Next N Results / Load More link | Cursor: continue after the last ordering values | Can be both: newly rendered page or client-side code replaces or appends content |
| Infinite scrolling | Often cursor-based, but offset is possible | Client-side code appends content |

<br>

The important distinction is that **cursor-based pagination is a way to retrieve data; infinite scrolling is a way to present it**. Neither requires the other.

For the CEMS catalog, numbered pages make the current position visible and give visitors straightforward URLs for filtered results. With a modest, structured Event catalog, offset-based retrieval is easy to connect to the GET parameters and EF Core query you have already built. Each request retrieves only the selected ten Events, even though Razor returns a complete HTML page.

<br>

**Choosing Pagination and Scrolling Strategies for Different Application Architectures**

Pagination and scrolling strategies are closely related to an application's rendering architecture, but they are not determined by it. When designing a web application, we make three separate decisions: how users navigate results, how the database retrieves those results, and how the browser renders or updates the interface. These decisions can be combined in different ways. However, certain combinations are particularly well suited to specific application architectures and client platforms.

For example, consider three ways to implement the CEMS Event catalog:

<br>

| Architecture | Data retrieval | Presentation and browser behavior |
|---|---|---|
| Traditional SSR | Offset-based pagination | The browser requests a numbered page, and the server returns a complete HTML document. |
| SSR with client-side enhancement | Cursor-based pagination | JavaScript requests additional server-rendered HTML fragments and appends them to the existing page. |
| Hybrid SSR/CSR | Cursor-based pagination | The server renders the initial page. JavaScript subsequently retrieves additional data and renders new content in the browser. |

<br>

The second approach illustrates an important distinction. Adding JavaScript to an SSR application does not automatically make it a hybrid-rendered application. If the server continues to generate the HTML and JavaScript simply inserts it into the document, the application still uses server-side rendering, enhanced with client-side interaction.

A hybrid-rendered application, by contrast, distributes rendering responsibilities between the server and browser. For example, the server might render the initial Event catalog while JavaScript retrieves subsequent results as JSON and generates the additional Event cards.

The characteristics of the application also influence these choices. A structured Event catalog benefits from numbered pagination because visitors can navigate directly to specific pages and preserve search criteria in the URL. A continuously updated activity feed may benefit from cursor-based retrieval and infinite scrolling because users generally move forward through a sequence of results rather than jump to a particular page.

Native mobile applications commonly use cursor-based APIs with incremental interface updates. These allow the client to retrieve additional results as needed without downloading the entire collection. For CEMS, numbered pagination with offset-based database queries fits the server-rendered MVC architecture and the way visitors browse a searchable Event catalog. Each request retrieves a bounded set of Events, and Razor renders the corresponding page.

<br>

> **The key principle is that rendering architecture, database retrieval, and presentation behavior are separate design decisions.** Choosing them together allows us to match the application's technical implementation to its users' navigation needs.

<br>

Now you can implement those decisions: count the matching Events, select the requested page in the database, and pass the page information to the View.

<br>

<a id="count-the-matching-events-before-selecting-a-page"></a>
## 11.2 Count the matching Events before selecting a page

The controller needs two answers from the database:

```text
How many public Events match the current filters?
Which ten matching Events belong on the requested page?
```

Those are different queries. `CountAsync()` returns the total matching count without materializing all matching Event objects. After the count is known, the controller can determine whether the requested page exists, apply stable ordering, and retrieve only the required rows.

Suppose 27 Events match the current filters and the page size is ten:

| Requested page | Rows to skip | Rows to take | Displayed results |
|---|---:|---:|---|
| 1 | 0 | 10 | 1–10 |
| 2 | 10 | 10 | 11–20 |
| 3 | 20 | 10 | 21–27 |

The number of rows to skip follows:

```text
(currentPage - 1) × pageSize
```

For example, once the filtered `query` is available, page 2 can be retrieved like this:

```csharp
const int pageSize = 10;
var currentPage = 2;

var events = await query
	.OrderBy(eventItem => eventItem.StartDateTime)
	.ThenBy(eventItem => eventItem.Id)
	.Skip((currentPage - 1) * pageSize)
	.Take(pageSize)
	.ToListAsync();
```

The final controller will calculate `currentPage` from the request rather than fixing it at 2.

`ThenBy(eventItem => eventItem.Id)` matters when Events share the same start time. Their IDs give the database a consistent way to decide which Event comes first; otherwise the boundary between two pages could be ambiguous.

<br>

<a id="add-page-information-to-the-view-model"></a>
## 11.3 Add page information to the View Model

The existing `EventSearchViewModel` already carries the current search criteria and matching Events. Its `Events` property will now contain **only the current page**. Razor also needs to know which page it is displaying and how many results exist across all pages.

Open:

**File: `ViewModels/EventSearchViewModel.cs`**

Add these properties below `CategoryId`:

```csharp
public int CurrentPage { get; set; } = 1;

public int PageSize { get; set; }

public int TotalCount { get; set; }

public int TotalPages { get; set; }
```

Keep the existing `Events`, `Categories`, `Search`, and `CategoryId` properties. The completed View Model now combines filtered domain data with the request and response information needed by this one page.

<br>

<a id="bind-and-normalize-the-requested-page"></a>
## 11.4 Bind and normalize the requested page

Open:

**File: `Controllers/EventsController.cs`**

Add a fixed page-size constant inside `EventsController`, next to its existing fields:

```csharp
private const int PublicPageSize = 10;
```

Then replace the existing `Index()` action with the following implementation:

```csharp
public async Task<IActionResult> Index(string? search, int? categoryId, int? page)
{
	_logger.LogInformation("Loading Published upcoming CEMS events with public filters and paging.");

	var now = DateTime.Now;

	IQueryable<Event> query = _context.Events
		.AsNoTracking()
		.Include(eventItem => eventItem.Category)
		.Where(eventItem =>
			eventItem.Status == EventStatus.Published &&
			eventItem.StartDateTime > now);

	if (!string.IsNullOrWhiteSpace(search))
	{
		var searchTerm = search.Trim();

		query = query.Where(eventItem =>
			eventItem.Title.Contains(searchTerm) ||
			eventItem.Location.Contains(searchTerm));
	}

	if (categoryId.HasValue)
	{
		query = query.Where(eventItem =>
			eventItem.CategoryId == categoryId.Value);
	}

	var totalCount = await query.CountAsync();
	var totalPages = (int) Math.Ceiling(totalCount / (double) PublicPageSize);

	var currentPage = page.GetValueOrDefault(1);

	if (currentPage < 1)
	{
		currentPage = 1;
	}

	if (totalPages > 0 && currentPage > totalPages)
	{
		currentPage = totalPages;
	}

	if (totalPages == 0)
	{
		currentPage = 1;
	}

	var events = await query
		.OrderBy(eventItem => eventItem.StartDateTime)
		.ThenBy(eventItem => eventItem.Id)
		.Skip((currentPage - 1) * PublicPageSize)
		.Take(PublicPageSize)
		.ToListAsync();

	var categories = await _context.Categories
		.AsNoTracking()
		.OrderBy(category => category.Name)
		.ToListAsync();

	var viewModel = new EventSearchViewModel
	{
		Events = events,
		Categories = categories,
		Search = search,
		CategoryId = categoryId,
		CurrentPage = currentPage,
		PageSize = PublicPageSize,
		TotalCount = totalCount,
		TotalPages = totalPages
	};

	return View(viewModel);
}
```

MVC binds `page` from the query string just as it binds `search` and `categoryId`. The parameter is nullable because `/Events` must work without a page number. If the page number is missing or cannot be converted to an integer, the action uses page 1. Zero and negative values also become page 1. A number larger than the available page count selects the last available page.

When no Events match, `TotalCount` and `TotalPages` are both zero. The controller keeps `CurrentPage` at 1, and the existing no-results message remains available to Razor.

Notice the position of `CountAsync()`: the Published/upcoming rule and **both optional filters** are already part of `query`. The count therefore describes the same matching set that the subsequent page query uses. The `OrderBy`, `ThenBy`, `Skip`, and `Take` operations then describe which subset of that set becomes `Model.Events`.

<br>

<a id="inspect-the-database-queries"></a>
## 11.5 Inspect the database queries

Set a breakpoint immediately after `CountAsync()` and inspect `totalCount`. Then step to `ToListAsync()` and inspect `events.Count`.

For a catalog with 27 matches on page 2, you should observe:

```text
totalCount = 27
totalPages = 3
currentPage = 2
events.Count = 10
```

The count query returns an integer. The page query returns at most ten Event rows and their Category information. Neither query requires the application to load all 27 matching Event objects.

---

<br>

<a id="add-page-navigation-to-the-results-view"></a>
# 12. Add Page Navigation to the Results View

The controller can now respond to requests for different pages, but a visitor still needs a way to make those requests. Keep the GET search/filter form at the top of the page and add Previous and Next links after the Event list.

Open:

**File: `Views/Events/Index.cshtml`**

Inside the existing `else` branch, immediately after the Event list's closing `</section>` and before the branch's closing brace, add:

```cshtml
<p class="event-page-status">
	Page @Model.CurrentPage of @Model.TotalPages
</p>

@if (Model.TotalPages > 1)
{
	<nav class="event-pagination" aria-label="Event results pages">
		<div class="event-pagination-actions">
			@if (Model.CurrentPage > 1)
			{
				<a asp-controller="Events"
				   asp-action="Index"
				   asp-route-search="@Model.Search"
				   asp-route-categoryId="@Model.CategoryId"
				   asp-route-page="@(Model.CurrentPage - 1)"
				   class="event-page-link"
				   rel="prev">Previous</a>
			}

			@if (Model.CurrentPage < Model.TotalPages)
			{
				<a asp-controller="Events"
				   asp-action="Index"
				   asp-route-search="@Model.Search"
				   asp-route-categoryId="@Model.CategoryId"
				   asp-route-page="@(Model.CurrentPage + 1)"
				   class="event-page-link"
				   rel="next">Next</a>
			}
		</div>
	</nav>
}
```

The `asp-route-*` attributes tell MVC to generate a new URL containing the current search and Category values alongside the new page number. For example, selecting **Next** from page 1 of the Workshop search produces a request for page 2 **with the Workshop search still active**.

There is no Previous link on page 1 and no Next link on the final page. When the results fit on one page, the page status still shows **Page 1 of 1**, but the navigation links are omitted. If no Events match, the no-results branch shows neither a page status nor navigation links. The `<nav>` element and its accessible label identify the links as navigation for the Event results; ordinary links are also keyboard-operable without JavaScript.

Submitting **Apply filters** sends the values in the GET form, which contains no `page` field. A new search or Category selection therefore starts on page 1. The **Clear** link continues to return to the unfiltered first page.

<br>

<a id="add-the-pagination-styles"></a>
## Add the pagination styles

Open:

**File: `wwwroot/css/site.css`**

Append these styles below the existing Lesson 3 rules:

```css
.event-pagination {
	display: flex;
	align-items: center;
	justify-content: space-between;
	flex-wrap: wrap;
	gap: 0.75rem;
	margin: 1.5rem 0;
}

.event-page-status {
	margin: 1rem 0 0;
	font-weight: 600;
}

.event-pagination-actions {
	display: flex;
	flex-wrap: wrap;
	gap: 0.5rem;
}

.event-page-link {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	min-height: 2.5rem;
	padding: 0.5rem 0.85rem;
	border: 1px solid #222;
	border-radius: 0.25rem;
	color: #222;
	background-color: #fff;
	text-decoration: none;
}

.event-page-link:hover {
	background-color: #eee;
}

.event-page-link:focus-visible {
	outline: 3px solid #125a9c;
	outline-offset: 2px;
}
```

The navigation wraps onto another line when the viewport is narrow, and its links have visible keyboard focus. These rules use the same project stylesheet as the search form.

Build and run the application again:

```cmd
dotnet build
dotnet run
```

Your original three Published upcoming Events still appear on page 1. You will add more Development Events during guided verification to exercise the additional pages.

---

<br>

<a id="trace-one-searchfilter-request"></a>
# 13. Trace One Search/Filter Request

At the beginning of the lesson, you saw the intended request flow before any code changed. Now revisit that model using the implementation you have built.

Suppose the visitor has submitted both a search term and Category selection, then selects **Next** to reach page 2.

The request now follows this flow:

```text
Browser sends GET /Events?search=...&categoryId=...&page=2
		↓
MVC binds search, categoryId, and page
		↓
EventsController.Index(search, categoryId, page)
		↓
controller starts from Published/upcoming Events
		↓
optional title/location and Category predicates are composed
		↓
CountAsync() counts all matching Events
		↓
controller validates the requested page
		↓
OrderBy(StartDateTime) + ThenBy(Id)
		↓
Skip + Take select the requested page
		↓
ToListAsync() retrieves only that page's Events
		↓
controller loads Category choices
		↓
controller builds EventSearchViewModel with page metadata
		↓
Razor renders form state + current results + navigation
		↓
HTML returns to the browser
```

Notice that the browser does not receive every matching Event and then hide unwanted rows with JavaScript. It sends the active criteria and requested page; the server and database determine both the matching set and the bounded page.

The optional predicates remain in the EF Core query for both `CountAsync()` and `ToListAsync()`. The count describes the entire filtered set, while the page query retrieves only the selected rows. The View Model carries both kinds of information to Razor, which can show the correct results, current criteria, and navigation links.

---

<br>

<a id="guided-verification"></a>
# 14. Guided Verification

Use the six-Event Development seed set established in Lesson 2.

The Published/upcoming Events are:

1. **Library Research Night** — Academic
2. **Career Networking Lunch** — Career
3. **Study Skills Workshop** — Academic

The database also contains Draft, Cancelled, and Published-past Events that normal public browsing must continue to exclude.

<br>

<a id="no-filters"></a>
## 14.1 No filters

Open:

```text
/Events
```

Leave the search box empty and keep **All categories** selected.

You should see:

1. Library Research Night
2. Career Networking Lunch
3. Study Skills Workshop

Confirm that the results remain chronological.

<br>

<a id="search-only"></a>
## 14.2 Search only

Search for:

```text
Student Center
```

You should see:

```text
Career Networking Lunch
```

The Draft **Welcome Mixer Planning Draft** also has a Student Center location, but it must remain absent because optional search narrows the Published/upcoming query rather than replacing it.

<br>

<a id="category-only"></a>
## 14.3 Category only

Clear the search text and choose **Academic**.

You should see:

1. Library Research Night
2. Study Skills Workshop

Confirm that the two Events remain chronologically ordered.

<br>

<a id="search-and-category-together"></a>
## 14.4 Search and Category together

Choose **Academic** and search for:

```text
Workshop
```

You should see:

```text
Study Skills Workshop
```

Both optional predicates are active:

```text
text matches
AND
Category matches
```

<br>

<a id="a-valid-category-with-no-public-results"></a>
## 14.5 A valid Category with no public results

Choose **Social** with no search text.

The seeded Social Events are Draft or Cancelled. The page should show:

```text
No published upcoming events match your current criteria.
```

This is a valid empty result.

<br>

<a id="search-that-matches-only-a-non-public-event"></a>
## 14.6 Search that matches only a non-public Event

Search for:

```text
Movie
```

The database contains **Outdoor Movie Night**, but it is Cancelled.

The page should again display:

```text
No published upcoming events match your current criteria.
```

rather than exposing the Cancelled Event.

<br>

<a id="preserved-filter-state"></a>
## 14.7 Preserved filter state

Enter a search term, choose a Category, and select **Apply filters**.

After the response loads, confirm:

- the text you entered is still in the search box;
- the Category you selected is still selected;
- the URL contains the submitted GET query-string values.

<br>

<a id="details-behavior"></a>
## 14.8 Details behavior

Use a filter that returns at least one Event, then select its linked title.

The existing Lesson 2 Details page should still open normally.

Search/filter changes did not modify `Details(int id)`, its View, or controlled not-found behavior.

<br>

<a id="add-development-events-for-a-multi-page-test"></a>
## 14.9 Add Development Events for a multi-page test

The original seed set contains only three Published upcoming Events, so it cannot fill more than one page. Use a temporary Development dataset to see how the page behaves when there are more than ten matches. The extra Events will belong to the existing Academic Category and to the seeded organizer.

Open:

**File: `Program.cs`**

Inside the existing `if (app.Environment.IsDevelopment())` block, immediately after `await DbInitializer.SeedAsync(context);`, temporarily add:

```csharp
var academicId = await context.Categories
	.Where(category => category.Name == "Academic")
	.Select(category => category.Id)
	.SingleAsync();

if (!await context.Events.AnyAsync(eventItem =>
	eventItem.Title == "Pagination Practice Workshop 01"))
{
	var createdAt = DateTime.Now;
	var practiceEvents = new List<Event>();

	for (var number = 1; number <= 52; number++)
	{
		var start = DateTime.Today
			.AddDays(20 + (number - 1) / 2)
			.AddHours(14);

		practiceEvents.Add(new Event
		{
			Title = $"Pagination Practice Workshop {number:00}",
			Description = "A Development Event for testing paginated results.",
			Location = "Learning Commons",
			StartDateTime = start,
			EndDateTime = start.AddHours(2),
			Capacity = 30,
			Status = EventStatus.Published,
			CreatedAt = createdAt,
			CategoryId = academicId,
			OrganizerId = "seed-organizer-001"
		});
	}

	context.Events.AddRange(practiceEvents);
	await context.SaveChangesAsync();
}
```

Add `using CampusEvents.Models;` and `using System.Collections.Generic;` at the top of `Program.cs` if they are not already present. These Events have upcoming relative dates, and each pair shares a start time. Their titles allow you to filter them together; their shared times let you inspect the `ThenBy(Id)` ordering.

Run the application. With the original three public Events and these 52 additions, `/Events` should have **55 matching Events across six pages**.

<br>

<a id="navigate-between-pages-and-check-ordering"></a>
## 14.10 Navigate between pages and check ordering

Open `/Events` without filters and confirm:

- page 1 displays ten Events and offers **Next**, but not Previous;
- pages 2 through 5 display ten Events and offer both links;
- page 6 displays five Events and offers **Previous**, but not Next;
- the page status changes from **Page 1 of 6** through **Page 6 of 6**;
- Events remain in ascending start-time order; when two start times match, their IDs determine the order.

Use your browser's address bar to open `/Events?page=2` directly. The same second page should appear.

<br>

<a id="keep-filters-across-pages-and-reset-on-a-new-search"></a>
## 14.11 Keep filters across pages and reset on a new search

Search for **Pagination Practice Workshop**, select **Academic**, and submit the form. All 52 practice Events match, so the response has six pages. Select **Next** and check that the URL retains both `search` and `categoryId` as well as `page=2`. The search box and Category selection should still show the current criteria.

While on the second page, change the search to **Library** and submit **Apply filters**. The new search starts on page 1 and shows **Library Research Night**. Clear the filters to return to the unfiltered first page.

<br>

<a id="check-missing-invalid-and-out-of-range-page-numbers"></a>
## 14.12 Check missing, invalid, and out-of-range page numbers

Try these requests with the expanded dataset:

| Request | Expected behavior |
|---|---|
| `/Events` | First page |
| `/Events?page=0` | First page |
| `/Events?page=-3` | First page |
| `/Events?page=abc` | First page |
| `/Events?page=999` | Last available page (page 6) |

Repeat the out-of-range request with an active filter to confirm that the controller chooses the last page **of the filtered result set**, not the last page of the unfiltered catalog.

<a id="fallback-behavior-is-a-design-decision"></a>
### Fallback behavior is a design decision

The behavior you just verified is intentionally forgiving, but it is **not required** by pagination, HTTP, or ASP.NET Core MVC. CEMS chooses to normalize missing, malformed, below-range, and above-range page requests into a usable page so the visitor still receives catalog results. Another application may define a stricter request contract.

It is useful to distinguish **omission** from **invalid input**. A request such as:

```text
/Events
```

can reasonably mean "use the default page" because the `page` parameter is optional. That does not require an explicitly supplied invalid value to receive the same treatment. A stricter application could reject requests such as:

```text
/Events?page=0
/Events?page=-3
/Events?page=abc
```

with **400 Bad Request** because the supplied value does not satisfy the endpoint's page-number contract. An application could also reject a syntactically valid page that does not exist, such as `page=999`. Depending on how the application defines numbered result pages, that case may be treated as **400 Bad Request** or as **404 Not Found** rather than being silently clamped to the final available page.

Strict handling can improve observability. Suppose application code accidentally generates:

```text
/Events?page=0
```

or:

```text
/Events?page=abc
```

If the server silently substitutes page 1, the page still appears to work and the defect can survive testing. Rejecting the request makes the contract violation visible through tests, logs, and monitoring instead of hiding it behind fallback behavior. This can also support defensive request handling by keeping the accepted input domain explicit, although strict rejection should not be treated as a security control by itself.

Neither policy is universally correct. A user-facing catalog may intentionally prefer forgiving navigation, while an API or an application that values strict client/server contracts may prefer rejection. The important engineering decision is to define the behavior deliberately, distinguish defaulting from correcting invalid input, document the contract, and test it consistently.

<br>

<a id="confirm-one-page-and-zero-result-behavior"></a>
## 14.13 Confirm one-page and zero-result behavior

Search for **Library**. Only one Event should match, so the page status should be **Page 1 of 1** with no Previous or Next links. Then search for **Movie**. The matching Event is Cancelled and therefore not public. The page should display the no-results message and no pagination navigation.


---

<br>

<a id="small-student-challenge-let-the-visitor-choose-the-page-size"></a>
# 15. Small Student Challenge — Let the Visitor Choose the Page Size

So far, CEMS always retrieves ten matching Events per page. The controller decides that value with a fixed constant, while the View Model already carries the resulting page size to Razor.

Now extend the feature so the visitor can choose how many Events appear on each page, jump directly to the first or last available page, **and** see the range of results currently visible.

The completed interface should let the visitor choose:

```text
10 Events per page
25 Events per page
50 Events per page
```

The selected page size becomes part of the GET request state alongside `search`, `categoryId`, and `page`. For example:

```text
/Events?search=Workshop&categoryId=1&page=2&pageSize=25
```

means, conceptually:

```text
show page 2
of the Published/upcoming Events
that match the current search and Category
using 25 Events per page
```

The page should also make the current slice of the matching set visible. For example, a request showing the second group of 25 from 55 matching Events should display:

```text
Showing 26–50 of 55 events.
```

<br>

<a id="attempt-the-challenge-before-reading-the-solution"></a>
## 15.1 Attempt the challenge before reading the solution

Do not continue immediately to the provided implementation.

First inspect the code you have already built and attempt the change yourself. You have already used every major technique this feature requires: query-string model binding, View Models, GET form state, validation, `CountAsync()`, `Skip()` and `Take()`, Tag Helpers, and conditional Razor rendering.

Before you code, answer these questions for yourself:

1. Which files need to change, and what responsibility does each one have?
2. Should `EventsController.Index()` receive another action parameter? If so, what type should it use?
3. What should happen when `pageSize` is omitted from the request?
4. The interface offers only 10, 25, and 50. Should a manually constructed request such as `/Events?pageSize=37` be accepted?
5. What should happen for `/Events?pageSize=0`, `/Events?pageSize=-10`, `/Events?pageSize=abc`, or `/Events?pageSize=1000000`?
6. Which pagination calculations currently depend on the fixed page size?
7. `EventSearchViewModel` already has a `PageSize` property. Is that sufficient for rendering the new dropdown, or does Razor also need the list of permitted choices?
8. What page number should **First** request, and how can Razor determine the page number for **Last**?
9. Should **First** and **Previous** appear on page 1? Should **Next** and **Last** appear on the final page?
10. Do **First** and **Last** require new controller actions or new View Model properties, or can the existing pagination state already describe those requests?
11. How will all four pagination links preserve the current search, Category, and page-size state?
12. When the visitor changes the search, Category, or page size and submits the form, should the application keep the old page number or begin again on page 1?
13. How can the result-range display prove that `CurrentPage`, `PageSize`, `Events.Count`, and `TotalCount` agree with one another?
14. Does the fact that the browser renders a dropdown eliminate the need for server-side validation? Why or why not?

Try to implement the feature now. Build it and exercise several URLs before reading further.

Then compare your solution with the implementation below. Do not treat the provided version as the only possible code structure. Instead, check whether your solution satisfies the same request contract, keeps database retrieval bounded, preserves request state correctly, and produces the required visitor-facing behavior.

<br>

<a id="acceptance-criteria"></a>
## 15.2 Acceptance criteria

Your solution should satisfy all of the following:

- `/Events` still defaults to 10 Events per page;
- the form offers only 10, 25, and 50 Events per page;
- the selected page size remains selected after the form is submitted;
- changing search, Category, or page size through the form starts again on page 1 because the form does not submit a `page` field;
- **First** and **Previous** appear only when `CurrentPage > 1`;
- **Next** and **Last** appear only when `CurrentPage < TotalPages`;
- First, Previous, Next, and Last preserve `search`, `categoryId`, and `pageSize`;
- **First** requests page 1 and **Last** requests `TotalPages` without requiring new controller actions;
- the **Clear** link removes the search and Category filters while preserving the selected page size;
- explicitly supplied unsupported or malformed page-size values return **400 Bad Request**;
- `CountAsync()` still counts the complete matching set before page retrieval;
- `Skip()` and `Take()` use the validated page size;
- the result range reflects the current page and page size;
- zero-result pages continue to show the existing no-results message without a result range or pagination links.

This exercise intentionally treats `pageSize` more strictly than the `page` parameter implemented earlier. The earlier page-number normalization remains unchanged. Here, the page-size contract is deliberately narrow because the value directly controls how many rows one request asks the database to materialize and how much result data the response may contain.

<br>

<a id="one-possible-solution"></a>
## 15.3 One possible solution

The following implementation is one way to satisfy the challenge. Compare each step with your own solution before changing your code.

<br>

<a id="update-the-view-model-with-the-available-page-size-choices"></a>
### 15.3.1 Update the View Model with the available page-size choices

Open:

**File: `ViewModels/EventSearchViewModel.cs`**

`PageSize` already stores the **selected** page size. Razor now also needs the set of page-size choices it should render, so add:

```csharp
public IReadOnlyList<int> PageSizeOptions { get; set; } = Array.Empty<int>();
```

This is presentation data, just like the Category choices. It does not belong on the `Event` entity and does not require a database migration.

The distinction is:

```text
PageSize
    = the validated page size selected for this response

PageSizeOptions
    = the page-size choices the View should offer
```

<br>

<a id="replace-the-fixed-page-size-constant-with-a-default-and-an-allowed-set"></a>
### 15.3.2 Replace the fixed page-size constant with a default and an allowed set

Open:

**File: `Controllers/EventsController.cs`**

Replace:

```csharp
private const int PublicPageSize = 10;
```

with:

```csharp
private const int DefaultPageSize = 10;

private static readonly int[] AllowedPageSizes =
{
	10,
	25,
	50
};
```

The default answers what happens when the visitor omits `pageSize`. The allowed set defines the explicit values the endpoint accepts when `pageSize` is supplied.

<br>

<a id="bind-and-validate-pagesize"></a>
### 15.3.3 Bind and validate `pageSize`

Change the action signature from:

```csharp
public async Task<IActionResult> Index(string? search, int? categoryId, int? page)
```

to:

```csharp
public async Task<IActionResult> Index(
	string? search,
	int? categoryId,
	int? page,
	int? pageSize)
```

Near the beginning of the action, before running the Event queries, add:

```csharp
var pageSizeWasSupplied = Request.Query.ContainsKey(nameof(pageSize));

if (pageSizeWasSupplied &&
	(!pageSize.HasValue || !AllowedPageSizes.Contains(pageSize.Value)))
{
	return BadRequest();
}

var currentPageSize = pageSize.GetValueOrDefault(DefaultPageSize);
```

Why check both the bound value and whether the query-string key was supplied?

A missing `pageSize` and a malformed `pageSize` can both leave the nullable action parameter without an integer value. They do **not** mean the same thing for this contract:

```text
/Events
    → pageSize omitted
    → use the default of 10

/Events?pageSize=abc
    → pageSize explicitly supplied but not a valid integer
    → 400 Bad Request
```

The browser's `<select>` element does not make this server-side validation optional. Query strings are client-controlled: a visitor, test, script, or other client can construct a URL without using the rendered form.

The allowed set also keeps the resource boundary explicit. Without validation, a request such as:

```text
/Events?pageSize=1000000
```

could ask the application to retrieve and materialize far more rows than the interface was designed to permit.

<br>

<a id="use-the-validated-page-size-for-pagination"></a>
### 15.3.4 Use the validated page size for pagination

Replace the fixed-size total-page calculation:

```csharp
var totalPages = (int)Math.Ceiling(
	totalCount / (double)PublicPageSize);
```

with:

```csharp
var totalPages = (int)Math.Ceiling(
	totalCount / (double)currentPageSize);
```

Then replace:

```csharp
.Skip((currentPage - 1) * PublicPageSize)
.Take(PublicPageSize)
```

with:

```csharp
.Skip((currentPage - 1) * currentPageSize)
.Take(currentPageSize)
```

Finally, update the View Model construction:

```csharp
PageSize = currentPageSize,
PageSizeOptions = AllowedPageSizes,
```

The important boundary remains:

```text
validated page size
    ↓
Skip + Take
    ↓
ToListAsync()
    ↓
only the permitted current page is materialized
```

<br>

<a id="add-the-page-size-dropdown-to-the-get-form"></a>
### 15.3.5 Add the page-size dropdown to the GET form

Open:

**File: `Views/Events/Index.cshtml`**

After the Category field and before the action buttons, add:

```cshtml
<div class="event-filter-field">
	<label for="pageSize" class="event-filter-label">Events per page</label>
	<select id="pageSize"
			name="pageSize"
			class="event-filter-control">
		@foreach (var pageSizeOption in Model.PageSizeOptions)
		{
			if (Model.PageSize == pageSizeOption)
			{
				<option value="@pageSizeOption" selected>@pageSizeOption</option>
			}
			else
			{
				<option value="@pageSizeOption">@pageSizeOption</option>
			}
		}
	</select>
</div>
```

The control name matches the new action parameter:

```text
name="pageSize"
        ↓
Index(..., int? pageSize)
```

The View Model provides both the choices and the currently selected value, so Razor can reconstruct the control after each request.

The form still does not contain a `page` field. Submitting a changed search, Category, or page size therefore starts a new result request on page 1.

<br>

<a id="add-first-and-last-and-preserve-request-state-in-navigation-links"></a>
### 15.3.6 Add First and Last and preserve request state in navigation links

The **Clear** link should remove the search and Category filters but keep the visitor's display-size choice. Add:

```cshtml
asp-route-pageSize="@Model.PageSize"
```

to that link.

Now extend the pagination navigation. **First** and **Last** do not require new controller actions. The existing `Index()` action already accepts a page number, and the View Model already tells Razor both the current page and the total number of pages:

```text
First     → page=1
Previous  → page=CurrentPage - 1
Next      → page=CurrentPage + 1
Last      → page=TotalPages
```

Replace the existing pagination block with:

```cshtml
@if (Model.TotalPages > 1)
{
	<nav class="event-pagination" aria-label="Event results pages">
		<div class="event-pagination-actions">
			@if (Model.CurrentPage > 1)
			{
				<a asp-controller="Events"
				   asp-action="Index"
				   asp-route-search="@Model.Search"
				   asp-route-categoryId="@Model.CategoryId"
				   asp-route-pageSize="@Model.PageSize"
				   asp-route-page="1"
				   class="event-page-link">First</a>

				<a asp-controller="Events"
				   asp-action="Index"
				   asp-route-search="@Model.Search"
				   asp-route-categoryId="@Model.CategoryId"
				   asp-route-pageSize="@Model.PageSize"
				   asp-route-page="@(Model.CurrentPage - 1)"
				   class="event-page-link"
				   rel="prev">Previous</a>
			}

			@if (Model.CurrentPage < Model.TotalPages)
			{
				<a asp-controller="Events"
				   asp-action="Index"
				   asp-route-search="@Model.Search"
				   asp-route-categoryId="@Model.CategoryId"
				   asp-route-pageSize="@Model.PageSize"
				   asp-route-page="@(Model.CurrentPage + 1)"
				   class="event-page-link"
				   rel="next">Next</a>

				<a asp-controller="Events"
				   asp-action="Index"
				   asp-route-search="@Model.Search"
				   asp-route-categoryId="@Model.CategoryId"
				   asp-route-pageSize="@Model.PageSize"
				   asp-route-page="@Model.TotalPages"
				   class="event-page-link">Last</a>
			}
		</div>
	</nav>
}
```

The boundary conditions keep irrelevant links out of the interface:

```text
page 1       → Next + Last
middle page  → First + Previous + Next + Last
final page   → First + Previous
one page     → no pagination navigation
```

All four links carry `search`, `categoryId`, and `pageSize`, so moving directly to either boundary does not silently change the active result set or return to the default page size. Only Previous and Next use `rel="prev"` and `rel="next"`; First and Last remain ordinary navigation links.

<br>

<a id="expand-the-filter-layout-for-the-new-field"></a>
### 15.3.7 Expand the filter layout for the new field

Open:

**File: `wwwroot/css/site.css`**

The wide-screen form originally defined three columns: search, Category, and actions. It now needs a fourth column for page size.

Change the `.event-filter-form` columns to:

```css
.event-filter-form {
	display: grid;
	grid-template-columns:
		minmax(15rem, 2fr)
		minmax(12rem, 1fr)
		minmax(9rem, auto)
		minmax(14rem, auto);
	gap: 1rem;
	margin-bottom: 1.5rem;
	align-items: start;
}
```

Keep the existing narrow-screen media query. It still changes the form to one column, so the new page-size field naturally stacks with the other controls on small viewports.

<br>

<a id="show-the-current-result-range"></a>
### 15.3.8 Show the current result range

The original student change is still part of the challenge.

Immediately after the closing `</form>` tag and before the no-results branch, add:

```cshtml
@if (Model.TotalCount > 0)
{
	var firstResult = (Model.CurrentPage - 1) * Model.PageSize + 1;
	var lastResult = firstResult + Model.Events.Count - 1;

	<p class="event-result-count">
		Showing @firstResult–@lastResult of @Model.TotalCount events.
	</p>
}
```

The calculation uses the actual number of Events on the current page for `lastResult`. That matters on the final page, which may contain fewer rows than `Model.PageSize`.

For the 55-Event Development dataset, examples include:

```text
pageSize=10, page=1  → Showing 1–10 of 55 events.
pageSize=10, page=6  → Showing 51–55 of 55 events.
pageSize=25, page=1  → Showing 1–25 of 55 events.
pageSize=25, page=2  → Showing 26–50 of 55 events.
pageSize=25, page=3  → Showing 51–55 of 55 events.
pageSize=50, page=2  → Showing 51–55 of 55 events.
```

When no Events match, `TotalCount` is zero, so the result-range line is omitted and the existing no-results branch remains responsible for the response message.

<br>

<a id="verify-your-implementation"></a>
## 15.4 Verify your implementation

Build and run:

```cmd
dotnet build
dotnet run
```

First verify the feature through the interface:

- open `/Events` and confirm **10** is selected;
- choose **25**, submit the form, and confirm the first page shows up to 25 Events, the range reads **Showing 1–25 of 55 events.**, and the available navigation is **Next** and **Last**;
- select **Next** and confirm the URL preserves `pageSize=25`, the range becomes **Showing 26–50 of 55 events.**, and all four links are available;
- select **Last** and confirm the request goes directly to page 3 with **Showing 51–55 of 55 events.**, preserving the active request state;
- select **First** and confirm the request returns directly to page 1 without losing the selected page size;
- navigate to the final page again and confirm only **First** and **Previous** remain;
- choose **50**, submit, and confirm there are two pages and **Last** moves directly from page 1 to page 2;
- enter a search or choose a Category while using a non-default page size and confirm the new request begins on page 1 while preserving the selected page size;
- select **Clear** and confirm the search and Category reset while the chosen page size remains selected.

Then test the server contract directly through the address bar:

| Request | Expected behavior |
|---|---|
| `/Events` | Page size defaults to 10 |
| `/Events?pageSize=10` | Accepted |
| `/Events?pageSize=25` | Accepted |
| `/Events?pageSize=50` | Accepted |
| `/Events?pageSize=0` | 400 Bad Request |
| `/Events?pageSize=-10` | 400 Bad Request |
| `/Events?pageSize=37` | 400 Bad Request |
| `/Events?pageSize=abc` | 400 Bad Request |
| `/Events?pageSize=1000000` | 400 Bad Request |

Finally, combine request values:

```text
/Events?search=Pagination+Practice+Workshop&categoryId=1&page=2&pageSize=25
```

Use the actual Academic Category ID in your database if it is not `1`. Confirm that the search, Category, page number, selected page size, result range, and retrieved Events all describe the same request state.

<br>

<a id="improve-invalid-request-feedback-with-a-razor-view"></a>
## 15.5 Improve Invalid Request Feedback with a Razor View

The page-size validation already enforces the server contract. However, `BadRequest()` by itself produces an HTTP 400 response without an Events page. This is an opportunity to separate two responsibilities you have already studied: **the HTTP status describes the result of the request**, while **a Razor View determines whether the response contains useful HTML**.

Keep the original `BadRequest()` implementation long enough to inspect it. Then evolve only the error response; do not change the allowed page sizes, normalization of valid requests, or the EF Core query.

<br>

<span id="observe-the-original-http-400-response"></span>
### Observe the original HTTP 400 response

With the existing code still in place:

```csharp
if (pageSizeWasSupplied &&
    (!pageSize.HasValue || !AllowedPageSizes.Contains(pageSize.Value)))
{
    return BadRequest();
}
```

Request `/Events?pageSize=1`. Open the browser's Developer Tools (**F12 → Network**), select the document request, and inspect the **Status** value. It should be **400 Bad Request**. The document may appear empty because `BadRequest()` returns a `BadRequestResult`; it does not tell MVC to render a Razor View. This is different from a successful query that returns no matching Events, which should render the normal Index View with HTTP 200 and its no-results message.

<br>

<span id="create-a-shared-error-view-model"></span>
### Create a shared error View Model

 #### File: **`ViewModels/ErrorPageViewModel.cs`**

 Create:

```csharp
namespace CampusEvents.ViewModels;

public class ErrorPageViewModel
{
    public string Title { get; init; } = "Invalid Request";

    public string Message { get; init; } = "The request could not be processed.";

    public string? ReturnUrl { get; init; }

    public string ReturnLabel { get; init; } = "Return";
}
```

This View Model holds only presentation information. It does not define an Event, contain validation logic, or represent a persisted database entity. The controller will supply an error message specific to page-size validation, but the presentation contract remains reusable for other invalid requests.

<br>

<span id="create-a-shared-badrequest-razor-view"></span>
### Create a shared BadRequest Razor View

#### File: **`Views/Shared/BadRequest.cshtml`**

Create:

```cshtml
@model CampusEvents.ViewModels.ErrorPageViewModel

@{
    ViewData["Title"] = Model.Title;
}

<h1>@Model.Title</h1>

<p role="alert">@Model.Message</p>

@if (!string.IsNullOrWhiteSpace(Model.ReturnUrl))
{
    <a href="@Model.ReturnUrl">@Model.ReturnLabel</a>
}
```

A shared View is appropriate because HTTP 400 presentation is not exclusive to Events. Razor HTML-encodes the displayed string values. Populate `ReturnUrl` from trusted server-side URL generation, **not directly from an unvalidated user-supplied URL**.

<br>

<span id="return-html-while-preserving-http-400"></span>
### Return HTML while preserving HTTP 400

In **`Controllers/EventsController.cs`**, keep `using CampusEvents.ViewModels;` and replace the original `return BadRequest();` branch in `Index()` with:

```csharp
if (pageSizeWasSupplied &&
    (!pageSize.HasValue || !AllowedPageSizes.Contains(pageSize.Value)))
{
    var errorViewModel = new ErrorPageViewModel
    {
        Title = "Invalid Page Size",
        Message = "Choose a page size of 10, 25, or 50.",
        ReturnUrl = Url.Action("Index", "Events"),
        ReturnLabel = "Return to Events"
    };

    Response.StatusCode = StatusCodes.Status400BadRequest;

    return View("BadRequest", errorViewModel);
}
```

`View("BadRequest", errorViewModel)` selects a Razor View by name and supplies its strongly typed model. From `EventsController`, MVC can find the file at `Views/Shared/BadRequest.cshtml` after looking in the controller's own Views folder. Setting `Response.StatusCode` preserves the HTTP 400 outcome. **Returning the View without setting the status code would normally produce HTTP 200**, even though the page said "Invalid Page Size."

This is a deliberately explicit lesson-level solution. Later lessons can introduce centralized status-code handling if several controllers need the same behavior. There is no need to add error-handling middleware or a generic inheritance hierarchy here.

<br>

<span id="verify-the-improved-error-response"></span>
### Verify the improved error response

Build and run the updated project:

```cmd
dotnet build
dotnet run
```

Verify these cases in the browser and in the **Network** tab:

| Request | Expected HTTP status | Expected HTML |
|---|---|---|
| `/Events?pageSize=1` | 400 | Shared **Invalid Page Size** page with a Return to Events link |
| `/Events?pageSize=abc` | 400 | Same shared error page |
| `/Events?pageSize=25` | 200 | Normal Events catalog with up to 25 matches |
| `/Events?search=unlikely-no-match` | 200 | Normal Events page with a no-results message |

Click **Return to Events** and confirm that it opens the normal catalog. The HTTP status and the HTML body are independent: an unsuccessful request can still provide a useful, server-rendered explanation.

<br>

<a id="restore-the-original-development-seed-state"></a>
## Restore the original Development seed state

After completing the pagination checks, remove the temporary insertion block from `Program.cs`.

```cmd
dotnet ef database drop --force -- --environment Development
dotnet ef database update -- --environment Development
```

Run `/Events` once more and confirm the original three Published upcoming Events appear without page-navigation links. The page-size control should default to 10, and the result range should read:

```text
Showing 1–3 of 3 events.
```

The rest of the application continues from the same six-Event Development seed data used in the earlier exercises.

---

<br>

<a id="final-code-check"></a>
# 16. Final Code Check

Several files changed during the lesson. Compare your final versions with the following snapshots.

<br>

<a id="final-viewmodelseventsearchviewmodelcs"></a>
## 16.1 Final `ViewModels/EventSearchViewModel.cs`

```csharp
using CampusEvents.Models;
using System;
using System.Collections.Generic;

namespace CampusEvents.ViewModels;

public class EventSearchViewModel
{
	public IReadOnlyList<Event> Events { get; set; } = Array.Empty<Event>();

	public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();

	public string? Search { get; set; }

	public int? CategoryId { get; set; }

	public int CurrentPage { get; set; } = 1;

	public int PageSize { get; set; }

	public IReadOnlyList<int> PageSizeOptions { get; set; } = Array.Empty<int>();

	public int TotalCount { get; set; }

	public int TotalPages { get; set; }
}
```

<br>

<span id="final-viewmodelserrorpageviewmodelcs"></span>
### Final `ViewModels/ErrorPageViewModel.cs`

```csharp
namespace CampusEvents.ViewModels;

public class ErrorPageViewModel
{
    public string Title { get; init; } = "Invalid Request";

    public string Message { get; init; } = "The request could not be processed.";

    public string? ReturnUrl { get; init; }

    public string ReturnLabel { get; init; } = "Return";
}
```

<br>

<a id="final-controllerseventscontrollercs"></a>
## 16.2 Final `Controllers/EventsController.cs`

```csharp
using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
	private readonly ILogger<EventsController> _logger;
	private readonly ApplicationDbContext _context;
	private const int DefaultPageSize = 10;

	private static readonly int[] AllowedPageSizes =
	{
		10,
		25,
		50
	};

	public EventsController(ILogger<EventsController> logger, ApplicationDbContext context)
	{
		_logger = logger;
		_context = context;
	}

	public async Task<IActionResult> Index(string? search, int? categoryId, int? page, int? pageSize)
	{
		_logger.LogInformation("Loading Published upcoming CEMS events with public filters and paging.");

		var pageSizeWasSupplied = Request.Query.ContainsKey(nameof(pageSize));

		if (pageSizeWasSupplied && (!pageSize.HasValue || !AllowedPageSizes.Contains(pageSize.Value)))
		{
			var errorViewModel = new ErrorPageViewModel
			{
				Title = "Invalid Page Size",
				Message = "Choose a page size of 10, 25, or 50.",
				ReturnUrl = Url.Action("Index", "Events"),
				ReturnLabel = "Return to Events"
			};

			Response.StatusCode = StatusCodes.Status400BadRequest;

			return View("BadRequest", errorViewModel);
		}

		var currentPageSize = pageSize.GetValueOrDefault(DefaultPageSize);
		var now = DateTime.Now;

		IQueryable<Event> query = _context.Events
			.AsNoTracking()
			.Include(eventItem => eventItem.Category)
			.Where(eventItem =>
				eventItem.Status == EventStatus.Published &&
				eventItem.StartDateTime > now);

		if (!string.IsNullOrWhiteSpace(search))
		{
			var searchTerm = search.Trim();

			query = query.Where(eventItem =>
				eventItem.Title.Contains(searchTerm) ||
				eventItem.Location.Contains(searchTerm));
		}

		if (categoryId.HasValue)
		{
			query = query.Where(eventItem =>
				eventItem.CategoryId == categoryId.Value);
		}

		var totalCount = await query.CountAsync();
		var totalPages = (int)Math.Ceiling(totalCount / (double)currentPageSize);

		var currentPage = page.GetValueOrDefault(1);

		if (currentPage < 1)
		{
			currentPage = 1;
		}

		if (totalPages > 0 && currentPage > totalPages)
		{
			currentPage = totalPages;
		}

		if (totalPages == 0)
		{
			currentPage = 1;
		}

		var events = await query
			.OrderBy(eventItem => eventItem.StartDateTime)
			.ThenBy(eventItem => eventItem.Id)
			.Skip((currentPage - 1) * currentPageSize)
			.Take(currentPageSize)
			.ToListAsync();

		var categories = await _context.Categories
			.AsNoTracking()
			.OrderBy(category => category.Name)
			.ToListAsync();

		var viewModel = new EventSearchViewModel
		{
			Events = events,
			Categories = categories,
			Search = search,
			CategoryId = categoryId,
			CurrentPage = currentPage,
			PageSize = currentPageSize,
			PageSizeOptions = AllowedPageSizes,
			TotalCount = totalCount,
			TotalPages = totalPages
		};

		return View(viewModel);
	}

	public async Task<IActionResult> Details(int id)
	{
		var eventItem = await _context.Events
			.AsNoTracking()
			.Include(item => item.Category)
			.Include(item => item.Organizer)
			.SingleOrDefaultAsync(item =>
				item.Id == id &&
				item.Status == EventStatus.Published);

		if (eventItem is null)
		{
			Response.StatusCode = StatusCodes.Status404NotFound;
			return View("EventNotFound");
		}

		return View(eventItem);
	}
}
```

<br>

<a id="final-viewseventsindexcshtml"></a>
## 16.3 Final `Views/Events/Index.cshtml`

```cshtml
@model CampusEvents.ViewModels.EventSearchViewModel

@{
	ViewData["Title"] = "Events";
}

<h1>Upcoming Campus Events</h1>

<form asp-controller="Events"
		asp-action="Index"
		method="get"
		class="event-filter-form"
		aria-label="Search and filter campus events">

	<div class="event-filter-field">
		<label for="search" class="event-filter-label">Search title or location</label>
		<input id="search"
				 name="search"
				 type="search"
				 class="event-filter-control"
				 value="@Model.Search"
				 aria-describedby="searchHelp" />
		<div id="searchHelp" class="event-filter-help">
			Matches Event title or location.
		</div>
	</div>

	<div class="event-filter-field">
		<label for="categoryId" class="event-filter-label">Category</label>
		<select id="categoryId"
				name="categoryId"
				class="event-filter-control">
			<option value="">All categories</option>

			@foreach (var category in Model.Categories)
			{
				if (Model.CategoryId == category.Id)
				{
					<option value="@category.Id" selected>@category.Name</option>
				}
				else
				{
					<option value="@category.Id">@category.Name</option>
				}
			}
		</select>
	</div>

	<div class="event-filter-field">
		<label for="pageSize" class="event-filter-label">Events per page</label>
		<select id="pageSize"
				name="pageSize"
				class="event-filter-control">
			@foreach (var pageSizeOption in Model.PageSizeOptions)
			{
				if (Model.PageSize == pageSizeOption)
				{
					<option value="@pageSizeOption" selected>@pageSizeOption</option>
				}
				else
				{
					<option value="@pageSizeOption">@pageSizeOption</option>
				}
			}
		</select>
	</div>

	<div class="event-filter-actions">
		<button type="submit" class="event-filter-submit">
			Apply filters
		</button>

		<a asp-controller="Events"
			 asp-action="Index"
			 asp-route-pageSize="@Model.PageSize"
			 class="event-filter-clear">
			Clear
		</a>
	</div>
</form>

@if (Model.TotalCount > 0)
{
	var firstResult = (Model.CurrentPage - 1) * Model.PageSize + 1;
	var lastResult = firstResult + Model.Events.Count - 1;

	<p class="event-result-count">
		Showing @firstResult–@lastResult of @Model.TotalCount events.
	</p>
}

@if (Model.Events.Count == 0)
{
	<p role="status">
		No published upcoming events match your current criteria.
	</p>
}
else
{
	<section aria-labelledby="event-list-heading">
		<h2 id="event-list-heading" class="cems-visually-hidden">
			Published upcoming events
		</h2>

		@foreach (var eventItem in Model.Events)
		{
			<article class="event-card">
				<h3 class="event-card-title">
					<a asp-controller="Events"
						 asp-action="Details"
						 asp-route-id="@eventItem.Id">
						@eventItem.Title
					</a>
				</h3>

				<p><strong>Category:</strong> @eventItem.Category.Name</p>
				<p><strong>Location:</strong> @eventItem.Location</p>
				<p>
					<strong>Starts:</strong>
					<time>@eventItem.StartDateTime.ToString("f")</time>
				</p>
			</article>
		}
	</section>

	<p class="event-page-status">
		Page @Model.CurrentPage of @Model.TotalPages
	</p>

	@if (Model.TotalPages > 1)
	{
		<nav class="event-pagination" aria-label="Event results pages">
			<div class="event-pagination-actions">
				@if (Model.CurrentPage > 1)
				{
					<a asp-controller="Events"
					   asp-action="Index"
					   asp-route-search="@Model.Search"
					   asp-route-categoryId="@Model.CategoryId"
					   asp-route-pageSize="@Model.PageSize"
					   asp-route-page="1"
					   class="event-page-link">First</a>

					<a asp-controller="Events"
					   asp-action="Index"
					   asp-route-search="@Model.Search"
					   asp-route-categoryId="@Model.CategoryId"
					   asp-route-pageSize="@Model.PageSize"
					   asp-route-page="@(Model.CurrentPage - 1)"
					   class="event-page-link"
					   rel="prev">Previous</a>
				}

				@if (Model.CurrentPage < Model.TotalPages)
				{
					<a asp-controller="Events"
					   asp-action="Index"
					   asp-route-search="@Model.Search"
					   asp-route-categoryId="@Model.CategoryId"
					   asp-route-pageSize="@Model.PageSize"
					   asp-route-page="@(Model.CurrentPage + 1)"
					   class="event-page-link"
					   rel="next">Next</a>

					<a asp-controller="Events"
					   asp-action="Index"
					   asp-route-search="@Model.Search"
					   asp-route-categoryId="@Model.CategoryId"
					   asp-route-pageSize="@Model.PageSize"
					   asp-route-page="@Model.TotalPages"
					   class="event-page-link">Last</a>
				}
			</div>
		</nav>
	}
}
```

<br>

<br>

<span id="final-viewssharedbadrequestcshtml"></span>
### Final `Views/Shared/BadRequest.cshtml`

```cshtml
@model CampusEvents.ViewModels.ErrorPageViewModel

@{
    ViewData["Title"] = Model.Title;
}

<h1>@Model.Title</h1>

<p role="alert">@Model.Message</p>

@if (!string.IsNullOrWhiteSpace(Model.ReturnUrl))
{
    <a href="@Model.ReturnUrl">@Model.ReturnLabel</a>
}
```

<br>

<a id="final-lesson-3-additions-to-wwwrootcsssitecss"></a>
## 16.4 Final Lesson 3 additions to `wwwroot/css/site.css`

Keep the existing stylesheet content and include these Lesson 3 rules:

```css
.event-filter-form {
	display: grid;
	grid-template-columns:
		minmax(15rem, 2fr)
		minmax(12rem, 1fr)
		minmax(9rem, auto)
		minmax(14rem, auto);
	gap: 1rem;
	margin-bottom: 1.5rem;
	align-items: start;
}

.event-filter-field {
	min-width: 0;
}

.event-filter-label {
	display: block;
	margin-bottom: 0.35rem;
	font-weight: 600;
	line-height: 1.5;
}

.event-filter-control {
	width: 100%;
	box-sizing: border-box;
	padding: 0.55rem 0.65rem;
	font: inherit;
	border: 1px solid #767676;
	border-radius: 0.25rem;
	background-color: #fff;
	color: #111;
}

.event-filter-help {
	margin-top: 0.35rem;
	font-size: 0.875rem;
}

.event-filter-actions {
	display: flex;
	gap: 0.5rem;
	padding-top: calc(1.5em + 0.35rem);
}

.event-filter-submit,
.event-filter-clear {
	flex: 1;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	box-sizing: border-box;
	min-height: 2.5rem;
	padding: 0.55rem 0.75rem;
	border: 1px solid currentColor;
	border-radius: 0.25rem;
	font: inherit;
	text-decoration: none;
	cursor: pointer;
}

.event-filter-submit {
	background-color: #222;
	color: #fff;
}

.event-filter-clear {
	background-color: #fff;
	color: #222;
}

.event-result-count {
	margin: 0 0 1rem;
}

.event-card {
	margin-bottom: 1.5rem;
}

.event-card-title {
	margin: 0 0 0.5rem;
	font-size: 1.25rem;
}

.cems-visually-hidden {
	position: absolute;
	width: 1px;
	height: 1px;
	padding: 0;
	margin: -1px;
	overflow: hidden;
	clip: rect(0, 0, 0, 0);
	white-space: nowrap;
	border: 0;
}

@media (max-width: 47.99rem) {
	.event-filter-form {
		grid-template-columns: 1fr;
	}

	.event-filter-actions {
		align-items: stretch;
		padding-top: 0;
	}
}

.event-pagination {
	display: flex;
	align-items: center;
	justify-content: space-between;
	flex-wrap: wrap;
	gap: 0.75rem;
	margin: 1.5rem 0;
}

.event-page-status {
	margin: 1rem 0 0;
	font-weight: 600;
}

.event-pagination-actions {
	display: flex;
	flex-wrap: wrap;
	gap: 0.5rem;
}

.event-page-link {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	min-height: 2.5rem;
	padding: 0.5rem 0.85rem;
	border: 1px solid #222;
	border-radius: 0.25rem;
	color: #222;
	background-color: #fff;
	text-decoration: none;
}

.event-page-link:hover {
	background-color: #eee;
}

.event-page-link:focus-visible {
	outline: 3px solid #125a9c;
	outline-offset: 2px;
}

```

After the pagination practice data is removed, the original six-Event seed state is restored.

---

<br>

<a id="check-your-understanding"></a>
# 17. Check Your Understanding

<details>
<summary>Why is GET appropriate for the Lesson 3 search/filter form?</summary>

The request reads and narrows the public catalog without changing persisted application state. GET also represents the current criteria in the request URL.

</details>

<details>
<summary>What is the practical purpose of keeping the Event query as an `IQueryable` until after optional filters are added?</summary>

It lets EF Core compose the search and Category predicates into the database query before `ToListAsync()` materializes the matching rows. That reduces unnecessary transfer and in-memory processing, limits over-fetching and unnecessary data residency in application memory, and helps bound the work performed for the request. Those properties matter for both performance and security.

</details>

<details>
<summary>Why are the optional filters applied before `ToListAsync()` in the final implementation?</summary>

Because `ToListAsync()` is the materialization boundary. Predicates added before it can be translated into the database operation, so SQL Server returns the result needed for the request instead of a broader set that the application must narrow afterward. This improves efficiency and helps reduce over-fetching, unnecessary data exposure in transit or memory, and resource-exhaustion risk from unnecessarily broad operations.

</details>

<details>
<summary>Why does the Index page need `EventSearchViewModel` instead of only `IReadOnlyList&lt;Event&gt;`?</summary>

The page needs more than Event results: it also needs Category choices, page-size choices, the current search text, the selected Category, and information about the requested page and total matches. The View Model packages that page-specific response state for Razor without adding UI-only properties to the `Event` entity.

</details>

<details>
<summary>How do text search and Category filtering work when both values are supplied?</summary>

Both optional predicates are composed onto the same Published/upcoming query. An Event must satisfy the text condition and the selected Category condition before it appears.

</details>

<details>
<summary>Why does Lesson 3 search `Title` and `Location` but not `Description`?</summary>

`FR-SRH-005` requires the search text to match at least the Event title or location, so those two fields satisfy the required search behavior. They are also short, focused values compared with a free-form `Description`. Adding `Description.Contains(searchTerm)` would make the database examine a broader body of text for each candidate Event, which can become more expensive as the catalog and descriptions grow. Excluding `Description` keeps this lesson's implementation focused and bounded in scope; it does not mean descriptions can never be searchable. A larger application could add broader free-text search using an intentional strategy such as database full-text search, a dedicated search service, or appropriate indexing.

</details>

<details>
<summary>Why can a Draft or Cancelled Event remain hidden even when its title or location matches the search term?</summary>

The base query first restricts normal public browsing to Published upcoming Events. Search and Category filtering only narrow that already-eligible set.

</details>

<details>
<summary>Are cursor-based pagination and infinite scrolling the same thing? Does either one require CSR?</summary>

No. Cursor-based pagination is a database retrieval technique: the query continues after a known position in an ordered result set. Infinite scrolling is a user-interface pattern that appends more results as the visitor scrolls. An SSR application can use a cursor with ordinary Next links, and a CSR or hybrid application can implement infinite scrolling with either cursor-based or offset-based retrieval.

</details>

<details>
<summary>Why does the controller call `CountAsync()` before `Skip()` and `Take()`?</summary>

The total count must describe **all Events matching the current filters**, not just the Events retrieved for one page. It tells the controller how many pages exist for the validated page size and whether a requested page is in range.

</details>

<details>
<summary>Why does the paginated query include `ThenBy(eventItem => eventItem.Id)`?</summary>

Start times are not necessarily unique. The ID provides a stable secondary sort key so Events with equal start times retain a predictable order across page boundaries.

</details>

<details>
<summary>How do pagination links preserve filters while a new search starts on page 1?</summary>

First, Previous, Next, and Last include the current `Search`, `CategoryId`, `PageSize`, and target `page` value through MVC Tag Helpers. The GET form contains search, Category, and page-size controls but no `page` control, so submitting changed criteria or a changed page size produces a request for the first page.

</details>

<details>
<summary>Why can First and Last be implemented entirely in Razor without adding new controller actions?</summary>

They are not new operations; they are links to existing representations of the same `Index()` action. Razor already knows the required target page numbers from the View Model: First requests page 1, and Last requests `TotalPages`. The existing action can process either request while the links preserve the current search, Category, and page-size state.

</details>

<details>
<summary>Why is silently correcting an invalid page number a design decision rather than required behavior, and what is a stricter alternative?</summary>

A missing `page` value can reasonably mean page 1 because the parameter is optional, but an explicitly supplied value such as `page=0`, `page=-3`, or `page=abc` violates a stricter positive-integer page contract. Instead of normalizing those values, an application can return `400 Bad Request`. A syntactically valid page beyond the available range can likewise be rejected rather than clamped, with `400 Bad Request` or `404 Not Found` depending on the documented contract. Strict rejection makes malformed links and client bugs more observable because they fail visibly instead of silently producing a different valid page.

</details>

<details>
<summary>Why must `pageSize` be validated on the server even though the View renders it as a dropdown?</summary>

The dropdown constrains only the interface the application renders. Query-string values are client-controlled, so a visitor, script, test, or other client can request values that never appeared in the HTML. Server-side validation enforces the actual request contract. Restricting `pageSize` to the documented choices also bounds how many Event rows one request can ask the database to retrieve and how much result data the application materializes and returns.

</details>

<details>
<summary>Why does the final `EventSearchViewModel` need both `PageSize` and `PageSizeOptions`?</summary>

`PageSize` represents the validated choice for the current response. `PageSizeOptions` represents the choices Razor should offer in the dropdown. They are different pieces of page-specific presentation state, just as `CategoryId` identifies the current Category choice while `Categories` provides the available Category options.

</details>

<details>
<summary>How is the visible result range calculated, and why does the last result use `Model.Events.Count`?</summary>

The first visible position is `(CurrentPage - 1) * PageSize + 1`. The last visible position is `firstResult + Events.Count - 1`. Using the actual current-page count matters because the final page may contain fewer Events than the selected page size.

</details>

<details>
<summary>Why does `BadRequest()` produce HTTP 400 without an MVC Razor page, while `View("BadRequest", errorViewModel)` needs the status code set explicitly?</summary>

`BadRequest()` produces a status-only MVC result and does not select a Razor View. `View("BadRequest", errorViewModel)` selects the shared View and supplies its model, but a normal `ViewResult` defaults to success unless the response status is set. Setting `Response.StatusCode = StatusCodes.Status400BadRequest` before returning the View preserves the rejection while adding readable HTML. A valid search with no results remains HTTP 200, not HTTP 400.

</details>

---

<br>

<a id="common-mistakes"></a>
# 18. Common Mistakes

<a id="the-form-field-name-does-not-match-the-action-parameter"></a>
## 18.1 The form field name does not match the action parameter

The controller accepts:

```csharp
Index(string? search, int? categoryId, int? page, int? pageSize)
```

The form submits the request-state controls it owns:

```html
name="search"
name="categoryId"
name="pageSize"
```

A name mismatch prevents MVC from binding the expected value.

<br>

<a id="the-query-is-allowed-to-become-broader-than-the-operation-needs"></a>
## 18.2 The query is allowed to become broader than the operation needs

Do not treat query bounding as only a performance concern. A database-backed endpoint should avoid routinely materializing a much broader result set than the current operation requires. Over-fetching increases data transfer and memory residency and can amplify CPU, memory, and concurrency pressure.

For Lesson 3, keep both layers of constraint visible in your reasoning:

```text
public eligibility: Published + upcoming
request scope: optional search + optional Category
```

The request-specific filters should be composed before counting and page retrieval. `Skip()` and `Take()` then bound the materialized Event collection.

<br>

<a id="tolistasync-happens-before-the-optional-filters"></a>
## 18.3 `ToListAsync()` happens before the optional filters

The final Event query must follow:

```text
base public predicate
→ optional search
→ optional Category
→ CountAsync (total matches)
→ OrderBy + ThenBy
→ Skip + Take
→ ToListAsync (one page)
```

Do not leave the premature-materialization comparison in the application.

<br>

<a id="search-accidentally-exposes-draft-or-cancelled-events"></a>
## 18.4 Search accidentally exposes Draft or Cancelled Events

Do not replace the Lesson 2 base predicate.

Keep:

```csharp
.Where(eventItem =>
	eventItem.Status == EventStatus.Published &&
	eventItem.StartDateTime > now)
```

Optional filters narrow this result set.

<br>

<a id="category-filtering-uses-the-wrong-event-property"></a>
## 18.5 Category filtering uses the wrong Event property

The established foreign key is:

```csharp
CategoryId
```

The final predicate is:

```csharp
eventItem.CategoryId == categoryId.Value
```

not

```csharp
eventItem.Category == category.Value
```

<br>

<a id="one-optional-filter-replaces-the-other"></a>
## 18.6 One optional filter replaces the other

Both filters should update the same `IQueryable<Event>`:

```csharp
query = query.Where(...);
```

When both inputs are present, both predicates apply.

<br>

<a id="the-index-view-still-uses-the-lesson-2-model"></a>
## 18.7 The Index View still uses the Lesson 2 model

The final View should begin with:

```cshtml
@model CampusEvents.ViewModels.EventSearchViewModel
```

not

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>
```

The Event collection is now:

```csharp
Model.Events
```

not

```csharp
Model
```

<br>

<a id="search-text-disappears-after-submission"></a>
## 18.8 Search text disappears after submission

Confirm that the controller uses:

```csharp
Search = search
```

and the input uses:

```cshtml
value="@Model.Search"
```

<br>

<a id="the-selected-category-is-lost"></a>
## 18.9 The selected Category is lost

Confirm that the controller uses:

```csharp
CategoryId = categoryId
```

and the Razor options compare:

```cshtml
Model.CategoryId == category.Id
```

<br>

<a id="category-choices-are-empty"></a>
## 18.10 Category choices are empty

The controller must populate them:

```csharp
var categories = await _context.Categories
	.AsNoTracking()
	.OrderBy(category => category.Name)
	.ToListAsync();
```

and then assign:

```csharp
Categories = categories
```

<br>

<a id="chronological-ordering-disappears"></a>
## 18.11 Chronological ordering disappears

The paginated query must retain both ordering keys:

```csharp
.OrderBy(eventItem => eventItem.StartDateTime)
.ThenBy(eventItem => eventItem.Id)
```

Apply the ordering after the optional filters and before `Skip()` and `Take()`.

<br>

<a id="a-zero-result-query-shows-a-blank-page"></a>
## 18.12 A zero-result query shows a blank page

Keep the explicit branch:

```cshtml
@if (Model.Events.Count == 0)
{
	<p role="status">
		No published upcoming events match your current criteria.
	</p>
}
```

Zero matches are a valid result, not a server error.

<br>

<a id="the-total-count-is-taken-after-skip-and-take"></a>
## 18.13 The total count is taken after `Skip()` and `Take()`

A page may contain only part of a much larger matching set. Call `CountAsync()` on the fully filtered query **before** selecting the page, so `TotalCount` describes the entire matching set rather than only the rows returned by the current `Take()`.

<br>

<a id="page-retrieval-has-no-stable-tie-breaker"></a>
## 18.14 Page retrieval has no stable tie-breaker

`OrderBy(StartDateTime)` alone does not establish the order of Events with equal start times. Keep `ThenBy(Id)` before `Skip()` and `Take()` so the database has a predictable page boundary.

<br>

<a id="pagination-links-discard-active-request-state"></a>
## 18.15 Pagination links discard active request state

First, Previous, Next, and Last must preserve the current search, Category, and page-size values while supplying the target page number. Keep `asp-route-search`, `asp-route-categoryId`, `asp-route-pageSize`, and `asp-route-page` together on all four links. Otherwise navigation can silently change the result set or return to the default page size.

<br>

<a id="a-changed-form-request-retains-the-previous-page-number"></a>
## 18.16 A changed form request retains the previous page number

Do not add a hidden `page` field to the GET form. Changed search text, Category, or page size begins again on page 1; only the pagination links request another page explicitly.

<br>

<a id="no-results-pages-still-show-pagination-links"></a>
## 18.17 No-results pages still show pagination links

Render page navigation only when `TotalPages > 1` and there are results. A valid zero-result query displays the existing no-results message without a result range or links to empty pages.

<br>

<a id="the-page-size-dropdown-is-trusted-without-server-side-validation"></a>
## 18.18 The page-size dropdown is trusted without server-side validation

Do not assume the visitor can submit only values rendered by the `<select>` element. Query strings can be constructed manually.

The final controller accepts only the documented page sizes:

```text
10
25
50
```

A malformed or unsupported explicit value should return `400 Bad Request`. This keeps the request contract explicit and prevents one request from selecting an unexpectedly large materialization size.

<br>

<a id="page-size-state-is-lost-during-navigation"></a>
## 18.19 Page-size state is lost during navigation

The GET form submits `pageSize` automatically because the dropdown is a named control, but First, Previous, Next, Last, and Clear are links rather than form submissions. Their Tag Helpers must therefore carry the page-size value explicitly when that state should be preserved.

<br>

<span id="the-error-view-accidentally-returns-http-200"></span>
## 18.20 The error View accidentally returns HTTP 200

Calling only `return View("BadRequest", errorViewModel);` normally renders the error message with a success status. Set `Response.StatusCode = StatusCodes.Status400BadRequest` before returning the View, then verify the **document response status**, not only the visible heading. `BadRequest()` by itself correctly returns 400 but does not render this shared View.

<br>

---

<br>

<a id="what-you-will-build"></a>
# What You Will Build

By the end of this lesson, the public Events page will let a visitor:

- search Published upcoming Events by title or location
- filter Published upcoming Events by Category
- combine text search and Category filtering
- keep the current search text and Category selection after submitting the form
- receive a clear message when nothing matches
- navigate between bounded result pages with First, Previous, Next, and Last controls when applicable
- choose 10, 25, or 50 Events per page
- see the current result range, current page, and total number of matching Events
- keep search, Category, and page-size state while moving between pages
- see a shared Razor error page when `pageSize` is invalid, while the server still returns HTTP 400

The browser will submit filter, page, and page-size values with normal GET requests. The controller will compose the EF Core query, validate the visitor-controlled page size, count the matching Events, retrieve one ordered and bounded page, and pass both the results and page-specific state to Razor. Invalid page-size requests will instead render the shared `BadRequest` View with an `ErrorPageViewModel` and retain HTTP 400.

<br>

<a id="what-you-built"></a>
# 19. What You Built

You began with the database-backed public catalog from Lesson 2 and evolved the existing request into a searchable, filterable, paginated Event list with a visitor-selectable bounded page size.

The final Event query follows this sequence:

```text
request search + Category + page + pageSize
	↓
validate supplied pageSize against 10 / 25 / 50
	↓
_context.Events
	↓
AsNoTracking + Include(Category)
	↓
Published + upcoming
	↓
optional title/location search
	↓
optional CategoryId filter
	↓
CountAsync() → total matching Events
	↓
OrderBy(StartDateTime) + ThenBy(Id)
	↓
Skip + Take → requested page using validated page size
	↓
ToListAsync() → only this bounded page's Events
```

The controller also loads the Category choices and assembles the response:

```text
current page's Events + Category choices
+ current Search + current CategoryId
+ current page + validated page size + page-size choices
+ total matches + total pages
	↓
EventSearchViewModel
	↓
Views/Events/Index.cshtml
```

The completed public catalog now supports:

- text search by Event title or location;
- Category filtering and combined search/filter requests;
- deterministic chronological ordering, including Events with equal start times;
- visitor-selectable pages of 10, 25, or 50 matching Events;
- server-side validation of the client-controlled page-size request value;
- First, Previous, Next, and Last pagination links when applicable;
- a distinction between numbered navigation, database pagination techniques, and browser rendering approaches;
- preserved search, Category, and page-size state across page navigation;
- a result-range display and clear current-page information;
- a documented distinction between forgiving page-number normalization and stricter request validation;
- a clear no-results message without unnecessary navigation;
- the existing Details links and Details workflow;
- responsive, labeled search/filter/page-size controls and keyboard-accessible pagination links.

The requirement state at the end of the lesson is:

- `FR-SRH-001` — **COMPLETE**
- `FR-SRH-002` — **COMPLETE**
- `FR-SRH-003` — **COMPLETE**
- `FR-SRH-004` — **PARTIAL**
- `FR-SRH-005` — **COMPLETE**
- `FR-SRH-006` — **COMPLETE**
- `FR-SRH-007` — **COMPLETE**
- `FR-SRH-009` — **COMPLETE**

`FR-SRH-004` remains partial because current registration availability requires persisted Registration data. The Event and Category entity classes and database schema are unchanged; the work in this lesson changes how the public catalog request is processed and rendered.

---

<br>

<a id="summary"></a>
# 20. Summary

<a id="what-you-learned"></a>
## What You Learned

Lesson 3 evolved the public Event catalog without changing the rule that determines which Events are public. The durable lesson is that search, filtering, pagination, page-size selection, and page-specific presentation state are coordinated parts of one request flow rather than independent copies of the Event collection.

You should now be able to explain that:

- GET is appropriate for a read-only catalog request because search text, Category selection, page number, and page size describe the resource representation the visitor wants to retrieve rather than a change to persisted application state;
- search text, selected Category, page number, and page size are **request state**, not new properties of an `Event`, so they do not require a database migration;
- a view-specific model is appropriate when the response needs both domain data and presentation state. `EventSearchViewModel` carries the current Events, Category choices, page-size choices, current criteria, and pagination metadata required by this View;
- public visibility remains the starting invariant: optional search and Category predicates may narrow the Published/upcoming catalog but must never widen it;
- an `IQueryable<Event>` represents database work that can continue to be composed before execution, while `ToListAsync()` is a materialization boundary that retrieves rows and creates C# objects;
- keeping applicable predicates and page bounds before materialization reduces unnecessary transfer and in-memory processing and helps keep the work of each request explicit and bounded;
- `CountAsync()` and page retrieval answer different questions: the count describes the complete matching set, while `Skip(...)`, `Take(...)`, and `ToListAsync()` retrieve only the requested page;
- stable pagination requires deterministic ordering, including a tie-breaker such as `ThenBy(Id)` when multiple Events share the same start time;
- numbered pagination and infinite scrolling are interface/navigation patterns, while offset and cursor/keyset pagination are database retrieval techniques;
- fallback behavior for missing, invalid, and out-of-range page values is an application contract decision: omission can default to page 1, while stricter applications may reject explicitly invalid values with `400 Bad Request` and may reject nonexistent pages rather than silently clamping them;
- visitor-controlled `pageSize` values must be validated on the server even when the rendered UI uses a dropdown, because clients can construct their own query strings and page size directly affects how much data one request can retrieve and materialize;
- the final page-size contract accepts 10, 25, or 50, defaults an omitted value to 10, and rejects malformed or unsupported explicit values with `400 Bad Request`;
- First, Previous, Next, and Last are navigation links to the same `Index()` action: First targets page 1, Last targets `TotalPages`, and all four links preserve search, Category, and page-size state while supplying the appropriate target page;
- pagination links preserve continuing request state, while submitting changed form criteria begins again on page 1 because the form does not submit the old page number;
- the visible result range can be derived from `CurrentPage`, `PageSize`, `Events.Count`, and `TotalCount`, with the actual current-page count preventing the final range from overstating the last partial page;
- a zero-result search is a valid application result that should receive clear user feedback rather than being treated as an error.

A useful mental model to carry forward is:

```text
GET request state
(search + Category + page + pageSize)
    ↓
validate request values that have a strict contract
    ↓
publicly eligible Events
    ↓
optional request-specific predicates
    ↓
count the complete matching set
    ↓
stable ordering
    ↓
select one bounded page using the validated page size
    ↓
materialize that page
    ↓
combine results + choices + request state in a View Model
    ↓
Razor reconstructs controls, result range, results, and navigation
```

The browser's query string affects which representation is returned, but filtering, counting, ordering, validation, and page selection still happen on the server and in the database query rather than by loading every Event into the browser and hiding rows afterward.

<a id="where-cems-stands"></a>
## Where CEMS Stands

CEMS now has a database-backed public catalog that visitors can search by title or location, filter by Category, choose a bounded page size, inspect the current result range, and navigate with First, Previous, Next, and Last while retaining the active search, Category, and page-size state. The application also distinguishes persisted domain data from temporary request state and uses `EventSearchViewModel` to provide the Razor View with the complete page-specific data it needs.

The public discovery requirements introduced so far are complete except for `FR-SRH-004`. That requirement remains partial because CEMS can display configured Event Capacity but still has no persisted Registration data from which to calculate current registrations or remaining places.

A more immediate architectural limitation now becomes important: every request is still effectively made by an unknown visitor. CEMS can persist Events and Categories and can tailor the public catalog to query-string criteria, but it cannot yet identify the person making a request or protect operations according to that identity.

That limitation gives the next lesson its starting point.

<a id="coming-next"></a>
## Coming Next

In **Lesson 4 — Identity, Authentication, Roles, and Protected Access**, you will add the account and session foundation that later Participant, Organizer, and Administrator workflows require.

The request pipeline will gain a trustworthy authenticated-user context:

```text
visitor submits credentials
    ↓
ASP.NET Core Identity verifies account/password state
    ↓
authentication cookie represents the signed-in session
    ↓
later request reaches ASP.NET Core
    ↓
authentication reconstructs User
    ↓
authorization decides whether protected code may run
```

You will distinguish **authentication**—establishing who the user is—from **authorization**—deciding what that authenticated user may do. You will also add account registration, Login, POST Logout, safe local return destinations, the exact `Organizer` and `Administrator` roles, authentication-aware navigation, anti-forgery protection for state-changing account/session forms, and a small `/Participant` route protected on the server.

The key continuity is this: **Lesson 4 does not replace the public catalog or its request-state model. It adds identity and authorization to the same ASP.NET Core request pipeline so later features can safely associate protected operations with a specific authenticated user.**


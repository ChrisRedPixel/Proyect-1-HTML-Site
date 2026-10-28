# **Lesson 1 — SRS to MVC: First Vertical Slice**

Revision: 2026.10.8

<br>

# Table of Contents

- [What You Will Learn](#what-you-will-learn)
- [Introduction](#introduction)
- [1. From Requirements to a Running Application](#from-requirements-to-a-running-application)
	- [1.1 The CEMS SRS](#the-cems-srs)
	- [1.2 CEMS terminology](#cems-terminology)
	- [1.3 Requirement identifiers](#requirement-identifiers)
- [2. How a Server-Rendered MVC Application Works](#how-a-server-rendered-mvc-application-works)
	- [2.1 Server-rendered pages](#server-rendered-pages)
	- [2.2 Multi-page navigation](#multi-page-navigation)
	- [2.3 HTTP request and response](#http-request-and-response)
	- [2.4 MVC at a practical level](#mvc-at-a-practical-level)
- [3. Create and Run the CEMS Project](#create-and-run-the-cems-project)
	- [3.1 Install Visual Studio Code and C# Extensions](#install-visual-studio-code-and-cs-extensions)
	- [3.2 Check the installed .NET SDK](#check-the-installed-net-sdk)
- [4. Tour the Parts of the Project You Need Today](#tour-the-parts-of-the-project-you-need-today)
- [5. Understand `Program.cs`](#understand-program-cs)
	- [5.1 Service registration](#service-registration)
	- [5.2 Middleware and the request pipeline](#middleware-and-the-request-pipeline)
	- [5.3 Conventional routing](#conventional-routing)
		- [5.3.1 Attribute routing exists, but CEMS will introduce it later](#attribute-routing-exists-but-cems-will-introduce-it-later)
	- [5.4 Trace the generated route](#trace-the-generated-route)
- [6. Build the First CEMS Events Page](#build-the-first-cems-events-page)
	- [6.1 Create `EventsController`](#create-eventscontroller)
		- [6.1.1 `EventsController : Controller`](#eventscontroller-controller)
		- [6.1.2 Constructor injection](#constructor-injection)
		- [6.1.3 `Index()`](#index-action)
	- [6.2 Create the Razor View](#create-the-razor-view)
	- [6.3 Pass a value from the controller to the View](#pass-a-value-from-the-controller-to-the-view)
- [7. Add Events to the Site Navigation](#add-events-to-the-site-navigation)
- [8. Establish Git and Safe Configuration Habits](#establish-git-and-safe-configuration-habits)
- [9. Guided Practice: Observe the Request and Change the Temporary Event](#guided-practice-observe-the-request-and-change-the-temporary-event)
	- [9.1 Observe `EventsController.Index()` running](#observe-eventscontroller-index-running)
	- [9.2 Trace the current value](#trace-the-current-value)
	- [9.3 Make the bounded change](#make-the-bounded-change)
- [10. Trace `/Events` from Browser to HTML](#trace-events-from-browser-to-html)
- [11. Check Your Understanding](#check-your-understanding)
- [12. Common Mistakes](#common-mistakes)
	- [12.1 `/Events` returns a View-not-found error](#events-returns-a-view-not-found-error)
	- [12.2 `/Events` returns 404](#events-returns-404)
	- [12.3 The View reports a model-type error](#the-view-reports-a-model-type-error)
	- [12.4 `@model` and `@Model` look like the same thing](#model-and-model-look-like-the-same-thing)
	- [12.5 The Events navigation link does not work](#the-events-navigation-link-does-not-work)
	- [12.6 The local URL uses a different port](#the-local-url-uses-a-different-port)
- [13. What You Built](#what-you-built)
	- [13.1 Final code checkpoint](#final-code-checkpoint)
- [14. Summary](#summary)
- [15 Coming Next](#coming-next)

<br id="what-you-will-learn">

# What You Will Learn

After completing this lesson, you should be able to:

1. explain the purpose of a software requirements specification
2. distinguish required product behavior from implementation choices
3. describe a server-rendered multi-page application
4. trace a request through routing, controller action, Razor View, and response
5. use logging or breakpoints to observe request execution
6. explain service registration, dependency injection, middleware, routing, and endpoint execution
7. explain the controller/View model contract
8. explain server-side rendering via C# + HTML templating (Razor)

<br id="introduction">

# Introduction

Throughout the course, you will develop one ASP.NET Core application from a small MVC shell into a full-fledged campus-events system called **Campus Events Management System (CEMS)**. This lesson begins before any CEMS application exists. You will start by examining the product requirements, creating an ASP.NET Core MVC project from a template, and building the first real vertical slice of the application: a browser request to `/Events` will reach a controller action, pass a value to a Razor View, and return rendered HTML. The goal is not to build the full event catalog yet. The goal is to understand the path a request follows through a server-rendered MVC application so that later features have a foundation you can reason about.

Lesson 1 therefore concentrates on one small but complete idea:

```text
browser request
	↓
ASP.NET Core
	↓
routing
	↓
controller action
	↓
Razor View
	↓
rendered HTML response
```

You will see several ASP.NET Core mechanisms around that flow — service registration, dependency injection, middleware, routing, View selection, Tag Helpers, and configuration. A major goal of this lesson is to give each mechanism a clear place in your mental model before later lessons add databases, authentication, validation, and authorization.

---

<br id="from-requirements-to-a-running-application">

# 1. From Requirements to a Running Application

<span id="the-cems-srs"></span>

## 1.1 The CEMS SRS

Before writing C# code, it is useful to know what you are building and where the rules for the product come from. A **Software Requirements Specification**, or **SRS**, describes what the finished system must do. For CEMS, the SRS defines the required product behavior, user types, terminology, business rules, quality expectations, and project scope.

That is different from an implementation decision.

For example:

- “Visitors can browse published upcoming campus events.” is product behavior stipulated in the SRS.
- “Use ASP.NET Core MVC and Razor Views” is an implementation constraint stipulated in the SRS.
- “Using DTOs and contracts to safeguard boundaries” is an *implementation choice*.
- “Using controlled-construction to enforce domain invariants.” is an *implementation choice*.
- “Using authentication cookies to establish sessions” is an *implementation choice*.

The distinction depends on what the SRS explicitly requires. A technology, architectural pattern, or authentication mechanism is an implementation choice unless the specification mandates it.

Keeping these categories separate is important. A framework may make many features possible, but the application should evolve according to the product requirements and constraints, rather than simply adopting features because the framework provides them.

<br id="cems-terminology">

## 1.2 CEMS terminology

You will see the following terms throughout the project:

| Term | Meaning in CEMS |
|---|---|
| **Visitor** | A person using the application without being authenticated |
| **Participant** | An authenticated user who can register for events |
| **Organizer** | An authenticated user who can manage events they own |
| **Administrator** | An authenticated user with application-wide administrative capabilities |
| **Event** | A scheduled campus activity |
| **Category** | A classification for an event |
| **Registration** | A participant's current reserved place in an event |
| **Draft** | An event that exists but is not yet published publicly |
| **Published** | An event that has been made publicly visible |
| **Cancelled** | A previously published event that is no longer scheduled to occur |

These are product concepts. In this lesson, you will use them as vocabulary only. You will **not** create CEMS `Event`, `Category`, or `Registration` classes yet.

<br id="requirement-identifiers">

## 1.3 Requirement identifiers

The SRS uses identifiers to organize requirements. You will encounter groups such as:

- `FR-ACC-*` — accounts and authentication
- `FR-SRH-*` — event discovery and search
- `FR-EVT-*` — event management
- `FR-REG-*` — event registration
- `FR-ADM-*` — administration
- `BR-*` — business rules
- `NFR-*` — non-functional requirements

Lesson 1 does not complete any functional `FR-*` requirement. The `/Events` page you create is an architectural first slice, not yet the real persisted event-browsing feature. That distinction is important for the rest of the project. A vertical slice can be technically real without yet satisfying the full product requirement. In this lesson, the request, controller, View, navigation, and response are real; the Event data is intentionally temporary. Later lessons will evolve this same slice rather than throw it away and start over.

---

<br id="how-a-server-rendered-mvc-application-works">

# 2. How a Server-Rendered MVC Application Works

<span id="server-rendered-pages"></span>

## 2.1 Server-rendered pages

CEMS is a **server-side rendered** application or **SSR**. When a browser requests a page from a server-rendered ASP.NET Core MVC application, the main work happens on the server:

```text
Browser
	↓ HTTP request
ASP.NET Core MVC application
	↓ controller/application logic
Razor View renders HTML
	↓ HTTP response
Browser receives HTML
```

The server executes the C# and Razor code, creates HTML, and sends that HTML to the browser. This gives the server responsibility for the application logic that produces the page. The browser still participates in HTTP, navigation, forms, CSS, and any later client-side behavior, but it does not execute the C# controller source code in order to display a server-rendered page, the server does.

<br id="multi-page-navigation">

## 2.2 Multi-page navigation

In a multi-page application, often shortened to **MPA**, navigating to another application URL normally produces another HTTP request. Moving between pages causes the browser to make a request, ASP.NET Core to process it, and the server to return a rendered response.

<br id="http-request-and-response">

## 2.3 HTTP request and response

For the Events page, the browser will make a GET request conceptually like this:

```text
GET /Events
```

A successful response will contain HTML:

```text
HTTP status: 200 OK
Content-Type: text/html
Body: <rendered HTML>
```

The URL `/Events` is **not** the path to a Razor file. The browser does not directly open `Views/Events/Index.cshtml` as if it were serving an static file. Routing and MVC conventions decide which server-side code handles the URL.

<br id="mvc-at-a-practical-level">

## 2.4 MVC at a practical level

MVC stands for **Model–View–Controller**.

For this lesson, use this practical mental model:

- **Controller**: receives a routed request and decides what application action should occur.
- **Action method/handler**: a public controller method that can handle a request.
- **Model**: server-side data supplied to a View.
- **View**: a Razor template that turns server-side data (model) into HTML.

Your first CEMS flow will eventually look like this:

```text
/Events
	↓
EventsController
	↓
Index()
	↓
temporary string value as model
	↓
Views/Events/Index.cshtml
	↓
HTML response
```

The “model” in Lesson 1 is deliberately just a `string`. You do not need a real `Event` class to learn the request/render cycle.

Also keep two different MVC conventions separate:

```text
routing convention
	→ decides which controller action handles the request

View-location convention
	→ helps MVC locate the Razor View after an action returns View()
```

For `/Events`, routing will eventually select `EventsController.Index()`. Later, when that action returns `View()`, MVC will use the controller/action names to look for `Views/Events/Index.cshtml`. Those are two different stages of the request.

---

<br id="create-and-run-the-cems-project">

# 3. Create and Run the CEMS Project

You are starting with no CEMS application, so the first step is to prepare your development environment and create a standard ASP.NET Core MVC project.

<br id="install-visual-studio-code-and-cs-extensions">

## 3.1 Install Visual Studio Code and C# Extensions

If VS Code is not installed, download it from the official [Visual Studio Code website](https://code.visualstudio.com/Download) and complete the installation.

1. Open VS Code and select **Extensions** from the Activity Bar (or press **Ctrl+Shift+X**).
2. Search for and install **C# Dev Kit** by Microsoft. Installing **C# Dev Kit** normally installs other required extensions automatically.
3. Run the `.NET Install Tool` and install the latest stable SDK.
4. Restart your computer to ensure the installation and environment changes take effect.

<br id="check-the-installed-net-sdk">

## 3.2 Check the installed .NET SDK

Open a terminal and run:

```cmd
dotnet --version
```

This displays the installed .NET SDK version (e.g., `10.0.12`). Use the latest SDK version and keep using that version throughout the project rather than changing versions between lessons.

If the command fails, follow these steps:

1. Visit Microsoft's official [.NET download page](https://dotnet.microsoft.com/en-us/download/dotnet).
2. Select the latest stable .NET version and download the **SDK** (not the Runtime) for your operating system.
3. Run the installer and complete the installation.
4. Restart your computer to ensure the installation and environment changes take effect.
5. Open terminal and run `dotnet --version` again to verify the installation.

Then verify that the ASP.NET Core MVC template is available:

```cmd
dotnet new list
```

Once these commands work, your development environment is ready to create the CEMS project.

**Create the MVC project**

1. Create a new empty folder named `CampusEvents` or create and clone an empty repository with the same name from GitHub.

```cmd
git clone https://github.com/<username>/CampusEvents.git
```

2. Open the folder in VS Code.
3. Open the terminal in VS Code and type:

If you did not clone the repository from GitHub do this step first:

```cmd
git init
```

Then:

```cmd
dotnet new gitignore
dotnet new sln
dotnet new mvc
dotnet sln add .
```

This lesson establishes three names that later code will use:

- **Solution name:** `CampusEvents`
- **Project name:** `CampusEvents`
- **Root namespace:** `CampusEvents`

Compile the generated project:

```cmd
dotnet build
```

A successful build gives you a known-good baseline. If a later change causes a compile error, you know the project was working before your edits.

Start the application by typing

```cmd
dotnet run
```

in the terminal or by selecting

```text
Run > Start Debugging (F5)
```
from the menu.

Use the HTTP or HTTPS URL printed in the console. The port number may differ from one run to another.

At this point, you are looking at the generated MVC template. It is not yet CEMS-specific, but it proves that the project can build and run.

---

<br id="tour-the-parts-of-the-project-you-need-today">

# 4. Tour the Parts of the Project You Need Today

You do not need to memorize every generated file. Focus on the pieces involved in the first request flow.

```text
CampusEvents/
│
├── Controllers/
│		└── HomeController.cs
│
├── Models/
│		└── ErrorViewModel.cs
│
├── Views/
│		├── Home/
│		│		└── Index.cshtml
│		├── Shared/
│		│		└── _Layout.cshtml
│		└── _ViewImports.cshtml
│
├── wwwroot/
│
├── appsettings.Development.json
├── appsettings.json
├── CampusEvents.csproj
├── CampusEvents.slnx
└── Program.cs
```

#### File: `Program.cs`

This is the application startup file and *composition root*. It registers services, builds the app, configures the request pipeline, and maps routes.

A useful first mental model is:

```text
startup
│
├─ 1. register services
│		↓
│	dependency-injection container knows what can be provided
│
├─ 2. configure the request pipeline
│		↓
│	ordered middleware processes requests
│
└─ 3. map endpoints/routes
		↓
	 requests can reach controller actions
```

You do not need the internals of these systems yet. You do need to recognize that each line in `Program.cs` participates in a different part of startup or request handling.

#### Folder: `Controllers/`

Controllers contain action methods that can respond to routed requests.

#### Folder: `Views/`

Views are Razor templates that render HTML on the server.

#### File: `Views/Shared/_Layout.cshtml`

The shared layout contains page structure used across multiple Views, including site navigation.

#### Folder: `wwwroot/`

This folder contains static files such as CSS, JavaScript, and images.

#### File: `appsettings.json` and `appsettings.Development.json`

These files hold configuration. Normal non-secret configuration can live here, but passwords, API keys, tokens, and production credentials should not be committed to source control.

#### File: `Models/ErrorViewModel.cs`

This file supports template error handling. It is not a CEMS domain entity.

---

<br id="understand-program-cs">

# 5. Understand `Program.cs`

#### File: `Program.cs`

Open `Program.cs`. The exact generated file can vary slightly across .NET versions, but the Lesson 1 baseline is equivalent to the following:

```csharp
// Services registration

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Middleware configuration

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Routes/endpoints mapping

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

`Program.cs` is more or less divided into three sections:

1. Services registration
2. Middleware configuration
3. Routes/endpoints mapping

<br id="service-registration">

## 5.1 Service registration

ASP.NET Core uses a built-in **dependency injection container**. Applications register services during startup, and framework components can receive those services when they are created.

```csharp
var builder = WebApplication.CreateBuilder(args);
```

automatically registers ASP.NET Core's default logging services in the built-in DI container, including:

- `ILogger<T>`: Used for logging within controllers and services.
- `ILoggerFactory`: Used to create logger instances.
- `ILoggerProvider`: The interface implemented by logging providers.

You do not need to explicitly register `ILogger<T>` using `builder.Services.AddScoped()` or `AddSingleton()`.

This registers the services required for MVC controllers and Razor views:

```csharp
builder.Services.AddControllersWithViews();
```

<br id="middleware-and-the-request-pipeline">

## 5.2 Middleware and the request pipeline

These statements participate in configuring how requests move through the application:

```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
```

You do not need to study middleware internals yet. At this level, think of middleware as ordered stages in the application's request-processing pipeline.

Conceptually:

```text
incoming request
	↓
middleware pipeline
		↓
	HTTPS redirection
			↓
	static-file handling
			↓
	routing
			↓
	authorization
			↓
selected endpoint
	↓
response travels back through the pipeline
```

The exact behavior of each middleware component can become more sophisticated than this diagram suggests, but the important idea is that **order matters**. A request moves through the configured pipeline, and some stages depend on work established by earlier stages.

`UseRouting()` participates in matching the incoming request to a configured endpoint, such as a controller action selected through the application's routing rules.

The template contains `UseAuthorization()`, but CEMS authorization behavior is not implemented in this lesson. You are simply preserving the normal MVC startup pipeline so later authentication and authorization features can evolve from an ordinary MVC application.

<br id="conventional-routing">

## 5.3 Conventional routing

This route is central to the lesson:

```csharp
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");
```

The route pattern has three pieces:

```text
controller / action / optional id
```

It also provides defaults:

- controller: `Home`
- action: `Index`

That means the root URL:

```text
/
```

is converted to:

```text
/Home/Index
```

and can be handled by:

```text
HomeController.Index()
```

<br id="attribute-routing-exists-but-cems-will-introduce-it-later">

### 5.3.1 Attribute routing exists, but CEMS will introduce it later

ASP.NET Core MVC also supports **attribute routing**, where route templates and HTTP-method attributes are placed directly on controllers or actions.

For example, later code can use attributes such as:

```csharp
[Route("organizer/events")]
```

or:

```csharp
[HttpGet("{id:int}/edit")]
```

Lesson 1 will **not** switch to that routing style yet. The generated conventional route gives you one simple rule to trace while you are learning the request/render cycle.

Later, when CEMS gains Organizer workflows with multiple GET and POST operations — Create, Edit, Publish, Cancel, and Delete — attribute routing will be introduced because explicit route definitions will solve a real readability and route-organization problem. At that point you will be able to compare the two styles from experience rather than treating attribute routing as unrelated syntax.

For now, keep:

```text
{controller=Home}/{action=Index}/{id?}
```

and learn how one conventional route maps URLs to controller actions.

<br id="trace-the-generated-route">

## 5.4 Trace the generated route

Open `Controllers/HomeController.cs` and locate its `Index()` action. Then look at:

```text
Views/Home/Index.cshtml
```

Request the root page in the browser and trace the flow:

```text
Browser GET /
	↓
default conventional route
	↓
controller = Home
	↓
action = Index
	↓
HomeController.Index()
	↓
return View()
	↓
Views/Home/Index.cshtml
	↓
Razor renders HTML
	↓
HTML response returns to browser
```

The browser only knows that it requested `/`. MVC decides which controller action and View are involved.

Notice that two decisions occur:

1. `GET /`	--> routing selects HomeController.Index()
2. `View()`	--> view-location conventions select Views/Home/Index.cshtml

Routing gets the request **to an action method/handler**. The View-location convention helps MVC find the **Razor template** after the action asks for a View.

---

<br id="build-the-first-cems-events-page">

# 6. Build the First CEMS Events Page

<span id="create-eventscontroller"></span>

## 6.1 Create `EventsController`

#### File: `Controllers/EventsController.cs`

Create the file with this code:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
	private readonly ILogger<EventsController> _logger;

	public EventsController(ILogger<EventsController> logger)
	{
		_logger = logger;
	}

	public IActionResult Index()
	{
		_logger.LogInformation("Handling request for the CEMS Events page.");
		return View();
	}
}
```

Several ideas appear here for the first time.

<br id="eventscontroller-controller">

### 6.1.1 `EventsController : Controller`

The class name ends in `Controller`, which matches MVC controller conventions. Inheriting from `Controller` gives the class MVC controller functionality such as the `View(...)` methods used to return Razor Views.

<br id="constructor-injection">

### 6.1.2 Constructor injection

The constructor receives:

```csharp
ILogger<EventsController> logger
```

You did not have to write `new Logger(...)` anywhere. ASP.NET Core creates the controller and supplies a compatible logger through dependency injection.

Connect this back to `Program.cs`:


```text
ASP.NET Core logging services exist
		↓
MVC creates EventsController
		↓
constructor requests ILogger<EventsController>
		↓
dependency injection supplies it
		↓
controller stores and uses the service
```

The controller declares a dependency; it does not take responsibility for constructing the logging infrastructure itself.

The field:

```csharp
private readonly ILogger<EventsController> _logger;
```

stores that service so the action can use it.

<br id="index-action">

### 6.1.3 `Index()`

An **action method/handler** is a public method on a controller that MVC can select to handle a request.

The method:

```csharp
public IActionResult Index()
```

is the controller action for the `/Events/Index` request.

This line creates a visible diagnostic message when the action runs:

```csharp
_logger.LogInformation("Handling request for the CEMS Events page.");
```

This line asks MVC to render the View associated with the current controller and action:

```csharp
return View();
```

At this moment, the controller contains no database access and no real CEMS Event object.

<br id="create-the-razor-view">

## 6.2 Create the Razor View

If you request `/Events` before creating the View, MVC can find the controller action but cannot find the corresponding Razor file.

Create the folder `Views/Events` if necessary, then create the View.

#### File: `Views/Events/Index.cshtml`

```cshtml
@{
	ViewData["Title"] = "Events";
}

<h1>Campus Events</h1>

<p>
	This is the first CEMS Events page. Event information is temporary in
	Lesson 1 and is not loaded from a database.
</p>

<section>
	<h2>Temporary Event Preview</h2>
	<p>Campus Welcome Mixer</p>
	<p>Student Center</p>
</section>
```

When `EventsController.Index()` calls `View()` without naming a View explicitly, MVC uses conventions to look for:

```text
EventsController + Index()
		↓
Views/Events/Index.cshtml
```

This is a **View-location convention**, not the routing convention that selected `EventsController.Index()` in the first place.

```text
/Events
	↓ routing
EventsController.Index()
	↓ return View()
View-location convention
	↓
Views/Events/Index.cshtml
```

Razor executes on the server. The browser never opens the `.cshtml` file directly.

Build and run it:

```cmd
dotnet build
dotnet run
```

Alternatively, you may use .NET Hot Reload:

```cmd
dotnet watch run
```

With this feature, whenever you change and save a project file, the app will auto build, run, and refresh.

Request:

```text
/Events
```

**What You Should See**

The page should include:

```text
Campus Events
Temporary Event Preview
Campus Welcome Mixer
Student Center
```

You should also see the controller log message in the application output:

```text
Handling request for the CEMS Events page.
```

**Why This Matters?**

You now have a real MVC vertical slice:

```text
GET /Events
	↓
EventsController.Index()
	↓
Views/Events/Index.cshtml
	↓
HTML response
```

The event title is still hard-coded directly in the View. The next step moves that title to the controller so you can see data travel from C# into Razor.

<br id="pass-a-value-from-the-controller-to-the-view">

## 6.3 Pass a value from the controller to the View

You are not creating a CEMS `Event` class yet. Instead, use one `string`.

This lets you learn the **controller/View model contract** without introducing domain modeling yet:

1. controller supplies a value of some type
2. View declares the type it expects
3. the two types must agree

For this lesson the contract is intentionally simple:

```text
controller supplies: string
View declares: @model string
```

Later lessons will keep the same idea while replacing `string` with collections, entities, and page-specific View Models.

#### File: `Controllers/EventsController.cs`

Change only the `Index()` action to:

```csharp
public IActionResult Index()
{
	_logger.LogInformation("Handling request for the CEMS Events page.");

	var featuredEventTitle = "Virgin Freshmen Sacrifice Ritual";

	return View(model: featuredEventTitle);
}
```

The action now creates a value on the server and passes it to the View.

The call:

```csharp
return View(model: featuredEventTitle);
```

returns a View result and supplies the string as the View's model. Here, lowercase `model:` is the name of the C# method parameter receiving `featuredEventTitle`.

Now update the Razor View so it declares the type it expects.

#### File: `Views/Events/Index.cshtml`

```cshtml
@model string

@{
	ViewData["Title"] = "Events";
}

<h1>Campus Events</h1>

<p>
	This is the first CEMS Events page. Event information is temporary in
	Lesson 1 and is not loaded from a database.
</p>

<section>
	<h2>Temporary Event Preview</h2>
	<p><strong>@Model</strong></p>
	<p>Student Center</p>
</section>
```

The directive:

```cshtml
@model string
```

means that this Razor View expects its model to be a `string`. Lowercase `@model` is a **Razor directive** that declares the model type; it does not receive the model value.

The expression:

```cshtml
@Model
```

reads the string that the controller passed to the View. Uppercase `@Model` is the **C# property** that exposes that value inside the View.

> **Naming note:** `model:`, `@model`, and `@Model` are three different things. Think of this as a small ASP.NET naming hangover—as if the framework designers had one drink too many before choosing three nearly identical labels—or, perhaps more charitably, as the result of naming important framework mechanisms before anyone had enough coffee. Capitalization matters.
>
> - `model:` — C# named argument that supplies the value to `View(...)`
> - `@model` — Razor directive that declares the model type
> - `@Model` — C# property used in the View to access the model value

Follow the **value** from start to finish:

```text
var featuredEventTitle = "Virgin Freshmen Sacrifice Ritual"
		↓
return View(model: featuredEventTitle)
		↓
View's @model directive declares the model type as a string
		↓
View's @Model property receives the string
		↓
rendered HTML
```

Separately, `@model string` declares that the View's `Model` property has type `string`. It is not a step through which the value flows. If the controller later supplies a different type, the View must evolve to expect that type. You will see exactly that transition in Lesson 2 when the page stops receiving one temporary string and starts receiving persisted Event data.


Build and run it:

```cmd
dotnet build
dotnet run
```

Request `/Events`.

**What You Should See**

The visible title says:

```text
Virgin Freshmen Sacrifice Ritual
```

The source of the value and the value changed. The controller now supplies the title, and the View decides how to render it.

---

<br id="add-events-to-the-site-navigation">

# 7. Add Events to the Site Navigation

Typing `/Events` works, but a normal application should provide navigation.

Open the shared layout and add an Events item beside the existing generated navigation links.

#### File: `Views/Shared/_Layout.cshtml`

Add this list item to the main navigation list:

```cshtml
<li class="nav-item">
	<a class="nav-link text-dark"
		asp-area="" 
		asp-controller="Events"
		asp-action="Index">Events</a>
</li>
```

Do not replace the rest of the generated layout.

The attributes:

```cshtml
asp-controller="Events"
asp-action="Index"
```

are MVC Tag Helper attributes. They describe the controller/action target rather than hard-coding a URL string. The Razor source expresses the MVC destination; the rendered HTML still contains a normal link that a browser understands.

The generated `_ViewImports.cshtml` normally enables the standard MVC Tag Helpers.

Build and run it:

```cmd
dotnet build
dotnet run
```

Request **Events** in the navigation bar.

**What You Should See**

The browser should navigate to the Events page, and the controller log message should appear again.

The link proves that shared navigation, routing, controller selection, and Razor rendering are working together.

---

<br id="establish-git-and-safe-configuration-habits">

# 8. Establish Git and Safe Configuration Habits

You now have a meaningful working state worth recording in source control. This lesson does not attempt to teach every Git feature. The goal is to establish a safe, repeatable project workflow before the codebase becomes more complex.

Stage the current files:

```cmd
git add .
git commit -m "Create CEMS MVC shell and Events page"
```

If you did not clone the project from GitHub, connect it to a remote GitHub repository with the following commands:

```cmd
git remote add origin https://github.com/<your-account>/CampusEvents.git
git branch -M main
```

Replace the example URL with the actual repository URL for your project.

Then sync it:

```cmd
git push
```

---

<br id="guided-practice-observe-the-request-and-change-the-temporary-event">

# 9. Guided Practice: Observe the Request and Change the Temporary Event

<span id="observe-eventscontroller-index-running"></span>

## 9.1 Observe `EventsController.Index()` running

This practice uses the code you have already built. You are not designing a new feature. Use either a breakpoint or the existing log message.

<br>

### Option A: Breakpoint

Place a breakpoint inside:

```csharp
public IActionResult Index()
```

Run the application under the debugger `Run > Start Debugging (F5)` and request:

```text
curl -X GET https://localhost:5051/Events
```

Or click the `Events` navigation link on the web browser.

When execution pauses, identify:

- the `EventsController` class
- the `Index()` action
- the local `featuredEventTitle` variable
- the `return View(...)` line

Continue execution and watch the page render.

<br>

### Option B: Logging

Run the application normally and watch the console while requesting `/Events`.

You should see:

```text
Handling request for the CEMS Events page.
```

The message is evidence that the browser request caused the server-side action to execute.

<br id="trace-the-current-value">

## 9.2 Trace the current value

Before changing anything, find this controller line:

```csharp
var featuredEventTitle = "Virgin Freshmen Sacrifice Ritual";
```

Then find this Razor expression:

```cshtml
<strong>@Model</strong>
```

The value flow is:

```text
controller creates string
		↓
controller passes string through View(model: ...)
		↓
View's Model property contains the string
		↓
Razor inserts @Model into HTML
```

<br id="make-the-bounded-change">

## 9.3 Make the bounded change

Now make the required small change.

#### File: `Controllers/EventsController.cs`

Change:

```csharp
var featuredEventTitle = "Virgin Freshmen Sacrifice Ritual";
```

to:

```csharp
var featuredEventTitle = "Sophomore's Bloodfest";
```

Before refreshing the browser, predict the result. You are changing the controller value, not the Razor markup.

Build and run it:

```cmd
dotnet build
dotnet run
```

Refresh `/Events`.

**What You Should See**

The page should now display:

```text
Sophomore's Bloodfest
```

The View did not need to change because it still renders `@Model`.

**Why This Matters**

This small change demonstrates an important separation:

- the controller supplies data
- the View controls presentation
- Razor combines the two on the server
- the browser receives the resulting HTML

---

<br id="trace-events-from-browser-to-html">

# 10. Trace `/Events` from Browser to HTML

With the final Lesson 1 value in place, trace the request from beginning to end.

```text
Browser requests /Events
		↓
ASP.NET Core receives the request
		↓
Routing selects EventsController.Index()
		↓
Index() creates/passes "Sophomore's Bloodfest"
		↓
MVC renders Views/Events/Index.cshtml
		↓
Razor generates HTML on the server
		↓
HTML response is returned to the browser
```

Here is what each step means.

1. The browser requests `/Events`

The browser sends an HTTP GET request. It does not request a `.cshtml` file and does not execute any C#.

2. ASP.NET Core receives the request

The request enters the pipeline configured in `Program.cs`.

3. Conventional routing selects the controller and action

The route is:

```text
{controller=Home}/{action=Index}/{id?}
```

For `/Events`, MVC determines:

```text
controller = Events
action = Index
```

That selects:

```text
EventsController.Index()
```

4. The action creates and passes the value

The final action contains:

```csharp
public IActionResult Index()
{
	_logger.LogInformation("Handling request for the CEMS Events page.");

	var featuredEventTitle = "Sophomore's Bloodfest";

	return View(model: featuredEventTitle);
}
```

The controller creates the string and passes it to the View.

5. MVC locates the Razor View

Because the action returns `View()` without naming a different View, the normal convention points to:

```text
Views/Events/Index.cshtml
```

6. Razor generates HTML

The View declares the type of its model with:

```cshtml
@model string
```

and accesses the model value through:

```cshtml
@Model
```

Razor runs on the server and inserts `Sophomore's Bloodfest` into the generated HTML.

7. The browser receives HTML

The browser receives the rendered response. It does not receive the C# controller source and it does not receive a JSON object.

That is why this action:

```csharp
return View(model: featuredEventTitle);
```

produces an HTML page: it returns a View result, MVC renders the Razor View, and Razor produces HTML.

At this point you can also place the major framework mechanisms in order:

1. startup and composition root:

```text
AddControllersWithViews() → MVC services are available
```

2. request:

```text
middleware pipeline
	↓
routing
	↓
EventsController.Index()
```

3. request-scoped controller instance construction:

```text
dependency injection supplies ILogger<EventsController>
```

4. action result:

```text
return View(model: featuredEventTitle)
	↓
View-location convention finds Views/Events/Index.cshtml
	↓
Controller passes view model (string) to the view (@Model)
	↓
HTML response
```

No database participates in this request. The title exists only as a temporary string created during the request.

---

<br id="check-your-understanding">

## 11. Check Your Understanding

Answer these questions using the code you just worked with.

<details>
<summary>1. How do `AddControllersWithViews()`, `UseRouting()`, and `MapControllerRoute(...)` contribute to MVC request handling?</summary>

`AddControllersWithViews()` registers the MVC services needed to use controllers and Razor Views.

`UseRouting()` adds routing middleware that matches incoming requests to registered endpoints.

`MapControllerRoute(...)` defines a conventional route pattern that connects URLs to controller actions.

Together, these components prepare MVC to handle incoming requests and determine which controller action should execute.

</details>

<details>
<summary>2. Why does requesting `/Events` execute `EventsController.Index()`, while requesting `/` executes `HomeController.Index()`?</summary>

The conventional route pattern is:

`{controller=Home}/{action=Index}/{id?}`

When the browser requests `/Events`, the `Events` URL segment identifies `EventsController`, while the missing action segment defaults to `Index`.

When the browser requests `/`, both segments are missing, so the route uses the defaults `Home` and `Index`.

</details>

<details>
<summary>3. What does dependency injection contribute when ASP.NET Core creates `EventsController`?</summary>

`EventsController` declares its dependency on `ILogger<EventsController>` through its constructor.

ASP.NET Core uses dependency injection to supply the logger when creating the controller, rather than requiring the controller to construct it.

The logging services are registered automatically by `WebApplication.CreateBuilder(args)`, while `AddControllersWithViews()` registers the MVC services.

</details>

<details>
<summary>4. Why does `EventsController` inherit from `Controller`?</summary>

Inheriting from `Controller` provides MVC-specific functionality, including the `View(...)` methods used to return Razor View results.

This lets the controller focus on handling requests and preparing data without implementing the underlying MVC infrastructure.

</details>

<details>
<summary>5. How is routing to `EventsController.Index()` different from MVC locating `Views/Events/Index.cshtml`?</summary>

Routing determines which controller action handles an incoming request.

After `EventsController.Index()` returns a View result, MVC uses View-location conventions to find the corresponding Razor file.

For the `Index` action in `EventsController`, the conventional View location is `Views/Events/Index.cshtml`.

These are two separate stages: **selecting the controller action** and **locating the View used to render its result**.

</details>

<details>
<summary>6. What does `return View(model: featuredEventTitle)` do, and what does `model:` represent?</summary>

`View(model: featuredEventTitle)` creates a View result and supplies `featuredEventTitle` as the model value that will be available to the Razor View.

The lowercase `model:` is a C# named argument identifying the parameter of the `View(...)` method that receives the value.

It is distinct from Razor's `@model` directive and the View's `@Model` property.

</details>

<details>
<summary>7. What is the controller/View model contract, and how do `@model` and `@Model` participate in it?</summary>

The controller supplies a value of type `string`, and the Razor View declares:

`@model string`

The lowercase `@model` directive specifies the type of model the View expects.

The uppercase `@Model` property provides access to the actual string value supplied by the controller.

The contract requires the supplied value to be compatible with the type declared by the View.

</details>

<details>
<summary>8. Where does Razor execute, and what does the browser ultimately receive?</summary>

Razor executes on the server, combining the View template with the model data supplied by the controller to generate HTML.

The browser receives an HTTP response containing the rendered HTML.

It does not execute the controller's C# code or the Razor instructions.

</details>

<details>
<summary>9. If the value of `featuredEventTitle` changes but the Razor markup remains unchanged, what happens?</summary>

After the updated controller code is applied to the running application, the page displays the new string.

The Razor expression `<strong>@Model</strong>` does not need to change because it renders the current model value, regardless of the specific string supplied.

This demonstrates how the controller can change the data without changing the View's presentation logic.

</details>

<details>
<summary>10. Why is the temporary Events page not yet a real CEMS event catalog?</summary>

The MVC request and rendering process is functional, but the event information is still a hard-coded string created by the controller.

The application does not yet use an Event domain model, retrieve persisted event records, or display a database-backed catalog.

Those capabilities will be introduced in later lessons.

</details>

---

<br id="common-mistakes">

# 12. Common Mistakes

<span id="events-returns-a-view-not-found-error"></span>

## 12.1 `/Events` returns a View-not-found error

Check the View path:

```text
Views/Events/Index.cshtml
```

MVC uses the controller and action names to locate the conventional View path.

If the controller action is running — you can see the log message — but MVC reports that the View cannot be found, routing has already done its job. The problem is now in View selection/location, not in the route that selected the action.

<br id="events-returns-404">

## 12.2 `/Events` returns 404

Check that:

- the class is named `EventsController`;
- it inherits from `Controller`;
- `Index()` is public;
- the default conventional route still exists in `Program.cs`.

<br id="the-view-reports-a-model-type-error">

## 12.3 The View reports a model-type error

The View expects:

```cshtml
@model string
```

The controller must therefore pass a string:

```csharp
return View(model: featuredEventTitle);
```

A different model type would require a corresponding View change.

<br id="model-and-model-look-like-the-same-thing">

## 12.4 `@model` and `@Model` look like the same thing

They are different and case-sensitive:

```text
@model = Razor directive that declares the model type
@Model = C# property that accesses the model value
```

The `model:` in `View(model: featuredEventTitle)` is yet another use: it is the C# parameter name used to supply that value.

<br id="the-events-navigation-link-does-not-work">

## 12.5 The Events navigation link does not work

Check the Tag Helper target:

```cshtml
asp-controller="Events"
asp-action="Index"
```

Also make sure the generated MVC Tag Helpers remain enabled through `_ViewImports.cshtml`.

<br id="the-local-url-uses-a-different-port">

## 12.6 The local URL uses a different port

Use the URL printed by `dotnet run` or shown by your development environment. Do not assume a fixed port number.

```text
https://localhost:[port]/
```

---

<br id="what-you-built">

# 13. What You Built

You started with no CEMS application and finished with a working ASP.NET Core MVC vertical slice.

Your application now has:

- the `CampusEvents` project and root namespace
- MVC controller and Razor View services
- the conventional route `{controller=Home}/{action=Index}/{id?}`
- built-in logging supplied through dependency injection
- `EventsController.Index()` handling `/Events`
- a `string` passed from the controller to a Razor View
- `Views/Events/Index.cshtml` rendering the string as HTML
- shared navigation link that requests `GET /Events`
- a Git repository and standard ASP.NET `.gitignore`

The Events page is still intentionally simple and non-persistent. It gives you a concrete request/render cycle that later project work can extend. The route style itself will evolve later when the Organizer workflows create a real need for explicit attribute routes. The request/controller/View principles learned here will remain the same.

<br id="final-code-checkpoint">

## 13.1 Final code checkpoint

Use the following snapshots to confirm your Lesson 1 code state.

#### File: `Program.cs`

```csharp
// Services registration
 
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Middleware configuration

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Routes/endpoints mapping

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

#### File: `Controllers/EventsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
	private readonly ILogger<EventsController> _logger;

	public EventsController(ILogger<EventsController> logger)
	{
		_logger = logger;
	}

	public IActionResult Index()
	{
		_logger.LogInformation("Handling request for the CEMS Events page.");

		var featuredEventTitle = "Sophomore's Bloodfest";

		return View(model: featuredEventTitle);
	}
}
```

#### File: `Views/Events/Index.cshtml`

```cshtml
@model string

@{
	ViewData["Title"] = "Events";
}

<h1>Campus Events</h1>

<p>
	This is the first CEMS Events page. Event information is temporary in
	Lesson 1 and is not loaded from a database.
</p>

<section>
	<h2>Temporary Event Preview</h2>
	<p><strong>@Model</strong></p>
	<p>Student Center</p>
</section>
```

#### File: `Views/Shared/_Layout.cshtml` — Events navigation addition

```cshtml
<li class="nav-item">
	<a class="nav-link text-dark"
		asp-controller="Events"
		asp-action="Index">Events</a>
</li>
```

At this point, you should be able to explain the full path from a browser request for `/Events` to the HTML displayed on the screen. That request/render cycle is the foundation you will build on as CEMS gains real data and additional behavior.

---

<br id="summary">

# 14. Summary

Lesson 1 established the architectural foundation for CEMS. The most important ideas are not the temporary Event title or the specific files you created; they are the responsibilities that different parts of the application have.

You should now be able to explain that:

- the SRS describes required product behavior, while framework choices and project structure describe how this course project will implement that behavior
- a server-rendered ASP.NET Core MVC request moves through the application before Razor returns HTML to the browser
- service registration, dependency injection, middleware, routing, controller execution, and View rendering are related parts of one request pipeline, but they perform different jobs
- conventional routing decides which controller action should handle a matching request, while MVC View-location conventions decide which Razor View a controller renders when a View name is not supplied explicitly
- a controller and a strongly typed Razor View have a model contract: the value passed by the controller must match the type declared by the View
- Tag Helpers can generate application navigation without hard-coding a URL string
- logging and breakpoints let you observe server-side execution rather than guessing what the application is doing
- a vertical slice can be technically complete even when the product feature is intentionally incomplete. The `/Events` request, controller, View, navigation, and response are real even though the Event data is still temporary

A useful mental model to carry forward is:

```text
Program.cs startup
	├─ register services
	├─ configure middleware
	└─ map routes

GET /Events
	↓
routing selects EventsController.Index()
	↓
DI has supplied the controller's dependencies
	↓
controller supplies a string model
	↓
View-location convention finds Index.cshtml
	↓
Razor renders the model into HTML
	↓
browser receives the response
```

Later lessons will make the application work inside that flow more substantial, but the overall request/response structure will remain recognizable.

<br id="coming-next">

# 15 Coming Next

In **Lesson 2 — Relational Data and the Public Event Catalog**, you will keep the MVC request/render cycle from this lesson and replace the temporary controller-created Event data with relational data stored in a database management system and accessed through Entity Framework Core.

# **Lesson 4 — Identity, Authentication, Roles, and Protected Access**

Revision: 2026.10.10

<br>

# Table of Contents

- [What You Will Learn](#what-you-will-learn)
- [Introduction](#introduction)
- [1. Authentication and Authorization Are Different](#authentication-and-authorization-are-different)
  - [1.1 Participant and Role Semantics](#participant-and-role-semantics)
- [2. Why ASP.NET Core Identity](#why-asp-net-core-identity)
  - [2.1 Authentication Cookie Mental Model](#authentication-cookie-mental-model)
  - [2.2 Login Identifier](#login-identifier)
  - [2.3 Invalid Login enumerates accounts](#invalid-login-enumerates-accounts)
  - [2.4 Safe Return URLs](#safe-return-urls)
    - [2.4.1 Why does it matter that the URL be local?](#why-does-it-matter-that-the-url-be-local)
- [3. Security Boundaries](#security-boundaries)
  - [3.1 Anti-Forgery](#anti-forgery)
    - [3.1.1 SameSite Cookies and CSRF](#samesite-cookies-and-csrf)
- [4. Connect the Existing `ApplicationUser` to Identity](#connect-the-existing-applicationuser-to-identity)
  - [4.1 Add Identity EF Stores and Evolve the User/DbContext](#add-identity-ef-stores-and-evolve-the-user-dbcontext)
- [5. Create and Apply the Identity Migration](#create-and-apply-the-identity-migration)
- [6. Remove Bootstrap, jQuery, and jQuery Validation](#remove-bootstrap-jquery-and-jquery-validation)
- [7. Build Account Registration](#build-account-registration)
  - [7.1 Add Participant Account Registration](#add-participant-account-registration)
  - [7.2 ModelState](#modelstate)
- [8. Enforce a Unique Login Identity](#enforce-a-unique-login-identity)
- [9. Build Login and Preserve Safe Return State](#build-login-and-preserve-safe-return-state)
  - [9.1 Add Login](#add-login)
  - [9.2 Configure the application cookie Login path](#configure-the-application-cookie-login-path)
- [10. Add Logout as a POST](#add-logout-as-a-post)
- [11. Define Roles and Configure Development Secrets](#define-roles-and-configure-development-secrets)
  - [11.1 Configure Development-Only Account Secrets](#configure-development-only-account-secrets)
  - [11.2 Do not store development credentials in configuration files](#do-not-store-development-credentials-in-configuration-files)
- [12. Seed Administrator Access, Then Organizer Access](#seed-administrator-access-then-organizer-access)
- [13. Make Navigation Authentication-Aware](#make-navigation-authentication-aware)
- [14. Add the Protected Participant Page, Then Finish Navigation](#add-the-protected-participant-page-then-finish-navigation)
  - [14.1 Introduce the protected Participant endpoint](#introduce-the-protected-participant-endpoint)
  - [14.2 Return to `_Layout.cshtml` and add the final Participant Area link](#return-to-layout-cshtml-and-add-the-final-participant-area-link)
- [15. Test Direct URL Authorization and Login Return Flow](#test-direct-url-authorization-and-login-return-flow)
  - [15.1 Prove That Hidden UI Is Not Authorization](#prove-that-hidden-ui-is-not-authorization)
- [16. Inspect Anti-Forgery Protection](#inspect-anti-forgery-protection)
- [17. Guided Verification](#guided-verification)
  - [Verify Retained Events Behavior](#verify-retained-events-behavior)
  - [17.1 Verify the Account and Access Matrix](#verify-the-account-and-access-matrix)
- [18. Small Change — Add Authenticated Identity Text](#small-change-add-authenticated-identity-text)
- [19. Check Your Understanding](#check-your-understanding)
- [20. Common Mistakes / Debugging Notes](#common-mistakes-debugging-notes)
  - [20.1 `ApplicationUser` still declares its own `Id`](#applicationuser-still-declares-its-own-id)
  - [20.2 `ApplicationDbContext` still inherits from `DbContext`](#applicationdbcontext-still-inherits-from-dbcontext)
  - [20.3 Specific Invalid Login Feedback](#specific-invalid-login-feedback)
  - [20.4 Login redirects to an arbitrary supplied URL](#login-redirects-to-an-arbitrary-supplied-url)
  - [20.5 Logout is GET](#logout-is-get)
  - [20.6 Seed passwords are hard-coded](#seed-passwords-are-hard-coded)
  - [20.7 Hidden navigation replaces `[Authorize]`](#hidden-navigation-replaces-authorize)
- [21. Final Code Check](#final-code-check)
- [22. What You Built](#what-you-built)
- [23. Summary](#summary)

<br id="what-you-will-learn">

# What You Will Learn

After completing the lesson, you should be able to:

1. distinguish authentication from authorization
2. explain why Participant is the normal authenticated-user capability rather than an Identity role
3. explain the practical role of authentication and authorization middleware
4. explain why Identity owns password hashing and verification
5. validate a safe local return destination
6. use anti-forgery protection on state-changing POST forms

<br id="introduction">

# Introduction

In Lessons 1–3, you built a server-rendered CEMS application with a relational Event catalog, Published/upcoming browsing, title/location search, Category filtering, visitor-selectable 10-, 25-, or 50-Event pages with retained GET filter state, no-results feedback, and Event Details.

The application still cannot identify a visitor. In this lesson, you will evolve the same `CampusEvents` project so visitors can create accounts, authenticate, end authenticated sessions, and reach a small server-protected Participant page.

The important change in this lesson is not merely that CEMS gains a Login page. Until now, every request has effectively been handled as a request from an unknown visitor. The application can query Events and Categories because those records are persisted in the database, but it has no reliable way to connect a browser request to a particular application user. That limitation prevents CEMS from supporting the protected Participant, Organizer, and Administrator workflows required later in the project.

Lesson 4 introduces the infrastructure that closes that gap. You will connect the existing `ApplicationUser` to ASP.NET Core Identity, create account and login workflows, let the authentication system remember a successful login across later requests, and then use authorization to decide whether a request may reach protected code. The goal is not to memorize a collection of Identity API calls. The goal is to understand the chain of responsibility from browser request to authenticated `User` to server-side authorization decision.

Keep this high-level progression in mind throughout the lesson:

```text
visitor submits credentials
	↓
Identity verifies account/password state
	↓
authentication cookie represents the signed-in session
	↓
later request reaches ASP.NET Core
	↓
authentication middleware reconstructs User
	↓
authorization decides whether protected code may run
```

The SRS requires CEMS to let a visitor create an account, reject duplicate login identities, authenticate valid credentials, reject invalid credentials without revealing whether an account exists, and let an authenticated user log out. It also requires authentication before later Participant, Organizer, and Administrator operations, and it requires a practical return-to-page flow when an anonymous visitor first requests a protected page.

Those requirements belong together because they describe one continuous user story. A visitor begins outside the authenticated part of the system. Registration creates an identity that CEMS can recognize. Login proves that the visitor controls that identity. The authentication cookie carries that signed-in state to later requests. Authorization then uses the resulting user information to protect server operations. Logout deliberately ends that authenticated state.

This lesson implements the account and session foundation now because later lessons need a trustworthy current user before they can safely answer questions such as “Which Organizer owns this Event?” or “Which Participant is registering?” The protected `/Participant` page is intentionally small: its purpose is to prove the authentication and authorization pipeline before more valuable protected product features depend on it.

---

<br id="authentication-and-authorization-are-different">

# 1. Authentication and Authorization Are Different

Authentication and authorization answer two different questions, and a protected application normally answers them in that order.

**Authentication** asks:

> Who is the user?

When a visitor submits an email and password, CEMS is trying to establish identity. If the credentials are valid, ASP.NET Core Identity creates an authenticated session, and later requests can expose that identity through `User.Identity`. Registration, login, authentication cookies, and logout all belong primarily to this authentication problem.

**Authorization** asks a different question:

> Is this authenticated user allowed to perform this operation?

Knowing who made a request does not automatically mean that the request should be allowed. An authenticated participant may eventually register for an Event, but that same account should not automatically receive Administrator capabilities. Authorization uses facts about the authenticated user—such as whether the user is signed in, whether the user has a role, and later whether the user owns a particular Event—to make that decision. `[Authorize]`, role checks, and later Organizer ownership rules belong to this layer.

Login and authorization therefore work together, but they are not interchangeable. A Login page can establish identity; it does not, by itself, protect every operation in the application. Protection must still be enforced where the server handles the protected request.

<br id="participant-and-role-semantics">

## 1.1 Participant and Role Semantics

CEMS uses **Participant** as the normal capability level of an authenticated user. In other words, creating and authenticating a normal account is enough to establish the baseline user identity that later Participant features will use.

Organizer and Administrator are additional permissions layered on top of that authenticated baseline:

The user-class model is:

```text
Visitor	= anonymous

Participant	= authenticated baseline

Organizer	= authenticated + Organizer role

Administrator	= authenticated + Administrator role
```

```text
authenticated user
	↓
Participant → base Participant capabilities

+ Organizer role → additional Organizer capabilities

+ Administrator role → additional Administrator capabilities
```

There is no `"Participant"` Identity role. That distinction prevents the application from treating every user category as a role when the SRS does not define it that way. Later code can ask whether a user is authenticated for Participant behavior and reserve explicit role checks for the additional `Organizer` and `Administrator` permissions.

---

<br id="why-asp-net-core-identity">

# 2. Why ASP.NET Core Identity

Authentication is a poor place to invent application-specific infrastructure. Password storage, credential verification, account lookup, login cookies, role membership, and security-sensitive account rules all interact. A home-grown solution would require CEMS to get each of those details right and keep them consistent.

ASP.NET Core Identity provides the framework-managed user store, normalized login identities, password hashing and verification, authentication cookies, `UserManager`, `SignInManager`, `RoleManager`, and role membership. CEMS supplies its application-specific user type and configuration; Identity supplies the security-oriented account machinery around it.

That division of responsibility matters when you read the code later. CEMS will create an `ApplicationUser` and pass the submitted password to `UserManager`. It will **not** calculate a password hash itself, assign `PasswordHash`, or store a plaintext password. When CEMS logs a user in, it will ask `SignInManager` to verify credentials and establish the signed-in state rather than comparing stored passwords in controller code.

<br id="authentication-cookie-mental-model">

## 2.1 Authentication Cookie Mental Model

HTTP requests are separate messages. After a successful login request finishes, the next request still needs some way to tell the server that it belongs to the same authenticated browser session. The authentication cookie supplies that continuity.

```text
successful login
	↓
Identity establishes authenticated session state
	↓
browser receives authentication cookie
	↓
browser sends cookie on later requests
	↓
UseAuthentication() middleware examines the request
	↓
ASP.NET Core reconstructs User
```

One important point is that the browser does **not** keep resubmitting the user's raw password on every request. The password is used during credential verification. After successful authentication, later requests carry the authentication cookie, and ASP.NET Core uses it to rebuild the request's user principal. Controller code and Razor Views can then inspect `User.Identity?.IsAuthenticated`, `User.Identity?.Name`, and role membership.

Logout ends that authenticated session. A later protected request must authenticate again.

Another important point is that we do not trust request headers, cookies, tokens, or payloads simply because they have official-looking names such as `X-User-Id: ...`, `Cookie: AuthCookie=...`, `Authorization: Bearer ...`, or `{ accessToken: ... }`. These values are still client-supplied input and can potentially be forged or modified. An authentication cookie can be trusted only after a server-trusted, battle-tested authentication mechanism — not an invented or custom one — validates that it was legitimately issued and has not been tampered with.

<br id="login-identifier">

## 2.2 Login Identifier

*ASP.NET Core Identity* can support a username independently from an email address, but CEMS does not need two separate login identifiers for this project. Lesson 4 therefore uses the user's email address as the login name:

```text
UserName = Email
```

*Identity* is also configured with:

```csharp
options.User.RequireUniqueEmail = true;
```

This avoids introducing a separate username field. The email serves as the user's **login identifier**, while *Identity*'s internal user ID remains the stable identifier for the account. The email can therefore be changed later without changing the user's underlying identity.

<br id="invalid-login-enumerates-accounts">

## 2.3 Invalid Login enumerates accounts

"Enumerate accounts" means to reveal whether an account exists or not based on the feedback messages given by failed login attempts. From a security perspective, even determining that an account does not exist is useful information to an attacker because it allows them to distinguish valid usernames or email addresses from invalid ones. This can help narrow the targets for password guessing, credential-stuffing, phishing, or other attacks.  In other words, it reduces an attacker's search space. For example, if an attacker knows that a user commonly uses three email addresses and the application reveals that one of them is not registered, the attacker can focus subsequent attempts on the remaining two. For that reason, login failures should normally return a generic message such as `Invalid login attempt.` rather than revealing whether the username/email or password was incorrect.

For example, if a failure message says:

```text
Invalid username.
```

when the username is not present in the database this one reveal that an account does not exist and if a failure message says:

```text
Invalid password.
```

when the given password does not match the one in the database, this one reveals that an account does exists.

To avoid enumerating accounts, show one generic failure message:

```text
Invalid login attempt.
```

Whether the email is unknown or the password is incorrect, the user sees the same message. The application still knows internally that authentication failed; it simply avoids exposing unnecessary account details.

<br id="safe-return-urls">

## 2.4 Safe Return URLs

Protected access introduces another usability problem. Suppose an anonymous visitor requests `/Participant`. After being sent to Login and authenticating successfully, returning to `/Participant` is more useful than always sending the user to the Events page.

That original destination travels through the login flow as a **return URL**. However, the return URL is request input. CEMS must not blindly redirect to any arbitrary address supplied by the browser. Lesson 4 therefore validates the destination with:

```csharp
Url.IsLocalUrl(...)
```

before using:

```csharp
LocalRedirect(...)
```

A local URL points back into this application. If the supplied value is missing or nonlocal, CEMS falls back to the Events page. This gives users a convenient return flow without turning the Login action into an unrestricted redirect mechanism.

<br id="why-does-it-matter-that-the-url-be-local">

### 2.4.1 Why does it matter that the URL be local?

It matters because the return URL comes from the user's request, so an attacker can potentially control it. Without checking that the URL is local, an attacker could create a link such as:

```text
/Login?returnUrl=https://evil.example.com/fake-login
```

A victim could visit the legitimate CEMS login page, authenticate successfully, and then CEMS would redirect them to the attacker's site. Because the redirect happened immediately after a legitimate login, the destination may appear trustworthy. This is called an open redirect vulnerability and is commonly useful in phishing attacks. Requiring the return URL to be local prevents that.

The important security principle is that redirect destinations are untrusted input just like form fields or query-string values. Validating the return URL lets CEMS preserve the convenient "send me back to the page I originally requested" behavior without allowing attackers to use CEMS as a trusted-looking redirector.

---

<br id="security-boundaries">

# 3. Security Boundaries

As soon as navigation becomes authentication-aware, it is tempting to think that hiding a link protects the corresponding feature. It does not. The browser is only one client of the application, and a user can type a URL directly, edit a form submission, or send a request without using the page controls you rendered.

```text
hidden navigation link = usability decision

[Authorize] = server access-control decision
```

The layout should avoid advertising actions that do not make sense for the current user, but the controller/action remains responsible for enforcing access. Typing a URL directly bypasses navigation; it does not bypass `[Authorize]`. This makes an important full-stack principle visible: browser-side validation can improve usability, but server-side `ModelState`, Identity validation, authorization, and database constraints remain authoritative because the server cannot assume that every request came through your intended UI. This distinction will become even more important in later lessons when ownership and role checks protect actual Event mutations.

<br id="anti-forgery">

## 3.1 Anti-Forgery

Anti-forgery protection defends against **Cross-Site Request Forgery (CSRF)**. A CSRF attack occurs when another website causes a user's browser to send a request to CEMS while that user is already signed in.

The problem is that browsers can attach cookies automatically. Suppose Alice is signed in to CEMS. She then visits an attacker's website containing:

```html
<form action="https://cems.example.com/Account/Logout" method="post">
</form>

<script>
    document.forms[0].submit();
</script>
```

The attacker does not know Alice's password or authentication cookie. However, if Alice's browser is allowed to send the CEMS authentication cookie with that request, CEMS could see an authenticated POST and mistakenly treat it as an action Alice intentionally submitted. In other words, the attacker does not steal or forge the authentication cookie in a CSRF attack. The attack works by abusing the browser's ability to send the victim's legitimate cookie automatically. That distinction is what makes the purpose of anti-forgery much easier to understand.

The authentication cookie answers:

> **Who is making this request?**

It does not, by itself, answer:

> **Did this request come from a form generated by CEMS?**

That is the purpose of the anti-forgery token.

For a protected Razor form, CEMS includes an unpredictable anti-forgery value with the form. Conceptually, the legitimate request contains both:

```text
Authentication cookie → proves the signed-in identity
Anti-forgery token     → proves the POST came through a form issued by CEMS
```

A malicious site may be able to cause the browser to send a request, but it cannot normally read a CEMS page and obtain the valid anti-forgery token because browsers prevent one origin from reading another origin's content.

Razor forms can explicitly enable the token with:

```html
asp-antiforgery="true"
```

ASP.NET Core places the request token in the generated form. The corresponding controller action requires validation:

```csharp
[ValidateAntiForgeryToken]
[HttpPost]
public async Task<IActionResult> Logout()
{
    // ...
}
```

When the POST arrives, ASP.NET Core validates the submitted anti-forgery token. The attacker's forged form does not contain a valid token, so the request is rejected before the action is allowed to perform the protected operation.

The important distinction is:

```text
Authentication → Who is the user?
Authorization  → Is that user allowed to perform this operation?
Anti-forgery   → Did the state-changing request originate through the expected application flow?
```

Anti-forgery protection is therefore required even when an endpoint already requires authentication. Authentication prevents an attacker from pretending to be Alice; anti-forgery protection prevents an attacker from tricking Alice's browser into performing an action as Alice.

<br id="samesite-cookies-and-csrf">

### 3.1.1 SameSite Cookies and CSRF

`SameSite` cookies can reduce CSRF risk by controlling when the browser sends cookies with cross-site requests.

For example, with `SameSite=Strict` the browser generally does not send the authentication cookie when a request originates from another site. In that case, the simple CSRF attack shown above would fail because CEMS would not see an authenticated request. However, `SameSite=Strict` is not always practical. It can interfere with legitimate cross-site navigation, such as a user following a link from another website into CEMS and unexpectedly appearing signed out because the authentication cookie is withheld. `SameSite` should therefore not replace anti-forgery protection. `SameSite` is a browser-enforced cookie policy, while anti-forgery is a server-enforced validation.

```text
SameSite			→ limits when authentication cookies are sent
Anti-forgery	→ validates that the POST came through the expected application flow
```

Using both provides stronger protection than relying on either mechanism alone. Anti-forgery also keeps the decision to accept a state-changing request on the server, rather than relying only on browser cookie behavior.

---

<br id="connect-the-existing-applicationuser-to-identity">

# 4. Connect the Existing `ApplicationUser` to Identity

The existing `ApplicationUser` was introduced in an earlier lesson because an Event needed an Organizer relationship before login existed. At that point the class acted as a minimal application user record. The same application user becomes the ASP.NET Core Identity user. Lesson 4 preserves the Event relationship and evolves `ApplicationUser` into the real Identity type. One user object now participates in both the application domain and the authentication system.

<br id="add-identity-ef-stores-and-evolve-the-user-dbcontext">

## 4.1 Add Identity EF Stores and Evolve the User/DbContext

Add the Identity EF Core package at a version compatible with the established course framework:

```cmd
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

#### File: `Models/ApplicationUser.cs`

Replace the Lesson 3 class with:

```csharp
using Microsoft.AspNetCore.Identity;

namespace CampusEvents.Models;

public class ApplicationUser : IdentityUser
{
	public string DisplayName { get; set; } = string.Empty;

	public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
}
```

`IdentityUser` now supplies the string `Id` and Identity account fields. That is why the old manually declared `Id` property disappears: declaring another one would duplicate a responsibility the base Identity type already owns. `DisplayName` and `OrganizedEvents` remain because they belong specifically to CEMS rather than to the generic Identity system.

#### File: `Data/ApplicationDbContext.cs`

Use:

```csharp
using CampusEvents.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{

	}

	public DbSet<Category> Categories => Set<Category>();

	public DbSet<Event> Events => Set<Event>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<ApplicationUser>(entity =>
		{
			entity.Property(user => user.DisplayName).IsRequired();
		});

		modelBuilder.Entity<Category>(entity =>
		{
			entity.Property(category => category.Name).IsRequired();
		});

		modelBuilder.Entity<Event>(entity =>
		{
			entity.Property(eventItem => eventItem.Title).IsRequired();
			entity.Property(eventItem => eventItem.Description).IsRequired();
			entity.Property(eventItem => eventItem.Location).IsRequired();

			entity.HasOne(eventItem => eventItem.Category)
				.WithMany(category => category.Events)
				.HasForeignKey(eventItem => eventItem.CategoryId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(eventItem => eventItem.Organizer)
				.WithMany(user => user.OrganizedEvents)
				.HasForeignKey(eventItem => eventItem.OrganizerId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.ToTable("Events", table =>
			{
				table.HasCheckConstraint(
					"CK_Events_StartBeforeEnd",
					"[StartDateTime] < [EndDateTime]");

				table.HasCheckConstraint(
					"CK_Events_CapacityPositive",
					"[Capacity] > 0");
			});
		});
	}
}
```

Identity's base configuration now owns the `AspNetUsers` mapping and supporting Identity entities. The old manual user key/table mapping is removed.

Notice the call to `base.OnModelCreating(modelBuilder)`. `IdentityDbContext<ApplicationUser>` has its own EF Core model configuration for users, roles, claims, logins, tokens, and their relationships. Calling the base implementation lets that framework configuration run before CEMS adds its own `DisplayName`, Category, Event, and ownership rules. Inheriting from `IdentityDbContext` without allowing its model configuration to run would leave the Identity schema incomplete.

This is also why changing the DbContext is more than a C# inheritance exercise. EF Core's model has changed, so the relational schema will need a migration in the next section.

#### File: `Program.cs`

Add:

```csharp
using Microsoft.AspNetCore.Identity;
```

The model classes define what an Identity-aware user and database context look like, but controllers still need services that know how to create users, verify passwords, sign users in, and manage roles. Those services are registered with dependency injection in `Program.cs`.

After `AddDbContext`, configure:

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.User.RequireUniqueEmail = true;

	options.Password.RequiredLength = 8;
	options.Password.RequireDigit = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireUppercase = true;
	options.Password.RequireNonAlphanumeric = false;

	options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();
```

`AddIdentity<ApplicationUser, IdentityRole>` tells the application which user and role types Identity should manage. The options block establishes account rules such as unique email and the password policy. `AddEntityFrameworkStores<ApplicationDbContext>()` connects those managers to the same EF Core/SQL Server database used by CEMS.

Service registration alone does not inspect incoming requests. The request pipeline also needs authentication and authorization middleware. After `UseRouting()`:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Final middleware order:

```text
UseRouting
UseAuthentication
UseAuthorization
MapControllerRoute
```

Read that sequence from the perspective of one request. Routing identifies the endpoint the request is trying to reach. Authentication examines the request and reconstructs the current `User` when a valid authentication cookie is present. Authorization can then evaluate that user against the endpoint's access requirements. Finally, the mapped controller action is allowed to execute only when the pipeline permits it.

At this point the application knows **how** authentication will participate in requests, but the database still reflects the older Lesson 3 model. Compile now, then update the schema before trying account workflows.

Build it:

```cmd
dotnet build
```

**NOTE:** Do not proceed to the next section until the build succeeds.

---

<br id="create-and-apply-the-identity-migration">

# 5. Create and Apply the Identity Migration

The C# model now describes more than the Lesson 3 database contains. `ApplicationUser` has become an Identity user, and `ApplicationDbContext` now includes Identity's supporting entities. EF Core therefore needs to translate that model change into a relational schema change.

This migration is especially important to inspect because it should **extend** the existing user infrastructure rather than disconnect Event ownership. The database is gaining the tables and columns Identity needs, while `Events.OrganizerId` must continue pointing to the same `AspNetUsers` identity.

Create:

```cmd
dotnet ef migrations add AddIdentityAuthentication -- --environment Development
```

Inspect the migration.

Expected changes include:

- additional Identity columns on existing `AspNetUsers`
- `AspNetRoles`
- `AspNetUserRoles`
- `AspNetUserClaims`
- `AspNetRoleClaims`
- `AspNetUserLogins`
- `AspNetUserTokens`
- Identity indexes

The migration must preserve:

```text
Events.OrganizerId → AspNetUsers.Id
```

That line captures the central continuity decision in this lesson: the user who authenticates through Identity is the same user that participates in the Event ownership relationship. Inspecting the migration is therefore a design check, not merely an EF Core ritual.

Apply:

```cmd
dotnet ef database update -- --environment Development
```

The established Development EF CLI convention remains unchanged.

**Migration continuity verification:** This operation must evolve the existing Development database rather than drop or recreate it. Before applying the migration, record the existing `AspNetUsers.Id` value `seed-organizer-001` and the `OrganizerId` values of its Events. After applying the migration, verify all of the following:

- The user row with `Id = "seed-organizer-001"` still exists in `AspNetUsers` and retains its CEMS `DisplayName`.
- Existing `Events.OrganizerId` references are unchanged and still point to that row.
- Identity's roles, user-role, claims, login, and token tables and required indexes were added without deleting existing Events or Categories.
- The migration did not unexpectedly drop/recreate `AspNetUsers` or alter the existing Organizer's primary key.

Inspect the generated migration **before** applying it. If it proposes destructive changes to existing user or Event records, do not proceed until the mapping discrepancy has been resolved. Later, after Section 12 seeding, verify that this **same** Organizer can authenticate and has the `Organizer` role. Do not test continuity by resetting the database.

---

<br id="remove-bootstrap-jquery-and-jquery-validation">

# 6. Remove Bootstrap, jQuery, and jQuery Validation

The course architecture requires project-owned CSS and no JavaScript framework/library. The generated MVC template gave the project useful starting files, but Lesson 4 is the point where those inherited presentation dependencies are removed.

Treat this as a controlled refactor. You are changing how the pages are styled and removing client-side helper libraries; you are **not** changing the established Events query, search/filter or pagination semantics, controller routes, or database behavior. The Lesson 3 `ErrorPageViewModel` and `Views/Shared/BadRequest.cshtml` must also remain in the project: the invalid page-size branch still returns a rendered error View with HTTP 400. Frequent regression checks matter because a UI cleanup should not accidentally alter product behavior.

Remove inherited library folders:

```cmd
if exist wwwroot\lib\bootstrap rmdir /s /q wwwroot\lib\bootstrap
if exist wwwroot\lib\jquery rmdir /s /q wwwroot\lib\jquery
if exist wwwroot\lib\jquery-validation rmdir /s /q wwwroot\lib\jquery-validation
if exist wwwroot\lib\jquery-validation-unobtrusive rmdir /s /q wwwroot\lib\jquery-validation-unobtrusive
```

Remove unused default project JavaScript and validation partial if present:

```cmd
if exist wwwroot\js\site.js del /q wwwroot\js\site.js
if exist Views\Shared\_ValidationScriptsPartial.cshtml del /q Views\Shared\_ValidationScriptsPartial.cshtml
```

#### File: `wwwroot/css/site.css`

Replace the inherited stylesheet with project-owned CSS:

```css
:root {
	font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
	line-height: 1.5;
	color: #1f2328;
	background: #ffffff;
}

* {
	box-sizing: border-box;
}

body {
	min-height: 100vh;
	margin: 0;
	display: flex;
	flex-direction: column;
}

a {
	color: #174ea6;
}

a:focus-visible,
button:focus-visible,
input:focus-visible,
select:focus-visible {
	outline: 3px solid #7aa7e8;
	outline-offset: 2px;
}

.site-shell {
	width: min(72rem, calc(100% - 2rem));
	margin-inline: auto;
}

.site-header {
	border-bottom: 1px solid #d0d7de;
	background: #f6f8fa;
}

.site-header-inner {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 1rem;
	padding-block: 0.85rem;
}

.site-brand {
	color: #1f2328;
	font-weight: 700;
	text-decoration: none;
}

.site-nav {
	flex: 1;
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	justify-content: space-between;
	gap: 1rem;
}

.site-nav-list {
	display: flex;
	flex-wrap: wrap;
	gap: 0.8rem;
	margin: 0;
	padding: 0;
	list-style: none;
}

.site-nav-link {
	display: inline-block;
	padding: 0.35rem 0;
}

.auth-status {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 0.65rem;
}

.role-label {
	padding: 0.15rem 0.45rem;
	border: 1px solid #8c959f;
	border-radius: 999px;
	font-size: 0.85rem;
}

.logout-form {
	margin: 0;
}

.link-button {
	border: 0;
	padding: 0;
	background: transparent;
	color: #174ea6;
	font: inherit;
	text-decoration: underline;
	cursor: pointer;
}

.page-content {
	flex: 1;
	padding-block: 1.5rem 2.5rem;
}

.site-footer {
	border-top: 1px solid #d0d7de;
	padding-block: 1rem;
	background: #f6f8fa;
}

.account-form {
	width: min(100%, 32rem);
	display: grid;
	gap: 1rem;
}

.form-field {
	display: grid;
	gap: 0.35rem;
}

.field-label {
	font-weight: 600;
}

.field-control {
	width: 100%;
	min-height: 2.5rem;
	padding: 0.55rem 0.65rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	background: #ffffff;
	color: inherit;
	font: inherit;
}

.button-primary,
.button-secondary {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	min-height: 2.5rem;
	padding: 0.55rem 0.8rem;
	border: 1px solid #1f2328;
	border-radius: 0.25rem;
	font: inherit;
	text-decoration: none;
	cursor: pointer;
}

.button-primary {
	background: #1f2328;
	color: #ffffff;
}

.button-secondary {
	background: #ffffff;
	color: #1f2328;
}

.validation-summary,
.validation-message {
	color: #9a0007;
}

.validation-summary:empty,
.validation-message:empty {
	display: none;
}

.event-filter-form {
	display: grid;
	grid-template-columns: minmax(0, 5fr) minmax(0, 4fr) minmax(0, 2fr) minmax(0, 3fr);
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
}

.event-filter-control {
	width: 100%;
	min-height: 2.5rem;
	padding: 0.55rem 0.65rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	background: #ffffff;
	color: inherit;
	font: inherit;
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

.event-filter-actions > * {
	flex: 1;
}

.event-result-count {
	margin: 0 0 1rem;
}

.event-page-status {
	margin: 1rem 0 0;
	font-weight: 600;
}

.event-pagination {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 0.75rem;
	margin: 1.5rem 0;
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
	padding: 0.55rem 0.8rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	background: #ffffff;
	color: #174ea6;
	text-decoration: none;
}

.event-page-link:hover {
	background: #f6f8fa;
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

@media (max-width: 48rem) {
	.site-header-inner,
	.site-nav {
		align-items: flex-start;
	}

	.site-nav {
		flex-direction: column;
	}

	.event-filter-form {
		grid-template-columns: 1fr;
	}

	.event-filter-actions {
		flex-direction: column;
		padding-top: 0;
	}
}
```

#### File: `Views/Shared/_Layout.cshtml`

Before authentication-aware navigation is introduced, use this framework-free layout:

```cshtml
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<title>@ViewData["Title"] - CampusEvents</title>
	<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
	<header class="site-header">
		<div class="site-shell site-header-inner">
			<a class="site-brand"
				 asp-controller="Home"
				 asp-action="Index">CampusEvents</a>

			<nav class="site-nav" aria-label="Primary navigation">
				<ul class="site-nav-list">
					<li>
						<a class="site-nav-link"
							 asp-controller="Home"
							 asp-action="Index">Home</a>
					</li>
					<li>
						<a class="site-nav-link"
							 asp-controller="Events"
							 asp-action="Index">Events</a>
					</li>
					<li>
						<a class="site-nav-link"
							 asp-controller="Home"
							 asp-action="Privacy">Privacy</a>
					</li>
				</ul>
			</nav>
		</div>
	</header>

	<main class="site-shell page-content">
		@RenderBody()
	</main>

	<footer class="site-footer">
		<div class="site-shell">
			<span>&copy; CampusEvents</span>
		</div>
	</footer>
</body>
</html>
```

#### File: `Views/Events/Index.cshtml`

Preserve the full Lesson 3 GET form: `search`, `categoryId`, and `pageSize` (10, 25, or 50), with a new filter submission starting at page 1. Clear resets search and Category but preserves the selected page size; First/Previous/Next/Last links preserve all current criteria. Keep the result range and pagination links while applying project-owned styles:

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
		<button type="submit" class="button-primary">
			Apply filters
		</button>

		<a asp-controller="Events"
			 asp-action="Index"
			 asp-route-pageSize="@Model.PageSize"
			 class="button-secondary">
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

Remove inherited Bootstrap-only classes from template Home/other core Views while preserving their text/content.

Build and run it:

```cmd
dotnet build
dotnet run
```

Verify Home, Events, search/filter, page status, and Details still work with no Bootstrap/jQuery resource requests. With the expanded Development dataset from Lesson 3, test First, Previous, Next, and Last, confirming that page navigation retains search, Category, and the selected page size (10, 25, or 50); confirm Clear retains page size and the four-column form remains responsive.

Verify `/Events?pageSize=1` after the UI refactor: it must still render the shared `BadRequest.cshtml` with the `ErrorPageViewModel` and return HTTP **400**, not 200 or a blank response. Do not remove or replace that shared View while editing `_Layout.cshtml` or `site.css`.

Also notice what did **not** disappear with jQuery validation: the controller can still reject invalid models, Identity can still reject invalid account state, and protected POST actions can still validate anti-forgery tokens. Those are server responsibilities and remain intact regardless of which client-side libraries are present.

---


<br id="build-account-registration">

# 7. Build Account Registration

Registration is the first complete account workflow. A visitor needs a form, MVC needs a safe shape to receive the submitted values, and Identity needs to create the durable application account. Separating those responsibilities makes the flow easier to reason about:

```text
GET /Account/Register
	↓
empty registration form
	↓
POST submitted fields
	↓
RegisterViewModel + ModelState
	↓
UserManager creates ApplicationUser
	↓
Identity validates account/password rules
	↓
redirect or redisplay validation feedback
```

The first account UI artifact is therefore the registration input model.

#### File: `ViewModels/RegisterViewModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class RegisterViewModel
{
	[Required]
	[StringLength(100)]
	public string DisplayName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	[Compare(nameof(Password))]
	public string ConfirmPassword { get; set; } = string.Empty;
}
```

This is request/form state, not an EF entity. The distinction is deliberate. `RegisterViewModel` describes what the registration form is allowed to receive: display name, email, password, and password confirmation. `ApplicationUser`, by contrast, is a persisted Identity/domain object with many fields that should not be bound directly from an anonymous registration request.

The data-annotation attributes also serve two related purposes. MVC uses them when validating the bound POST model, and Razor Tag Helpers can use the same metadata when rendering form fields and validation messages. `[Compare(nameof(Password))]` is specifically a form concern: password confirmation helps detect typing mistakes, but `ConfirmPassword` does not belong in the persisted user record.

Build it:

```cmd
dotnet build
```

<br id="add-participant-account-registration">

## 7.1 Add Participant Account Registration

Now connect the form model to Identity. The GET action and POST action have different jobs. GET renders an empty form. POST receives the submitted values, lets MVC validate the input model, constructs the application user fields that CEMS actually wants to persist, and delegates secure account creation to `UserManager<ApplicationUser>`.

#### File: `Controllers/AccountController.cs`

Introduce registration only:

```csharp
using CampusEvents.Models;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Controllers;

public class AccountController : Controller
{
	private readonly UserManager<ApplicationUser> _userManager;

	public AccountController(UserManager<ApplicationUser> userManager)
	{
		_userManager = userManager;
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Register()
	{
		return View();
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Register(RegisterViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var user = new ApplicationUser
		{
			UserName = model.Email,
			Email = model.Email,
			DisplayName = model.DisplayName
		};

		var result = await _userManager.CreateAsync(user, model.Password);

		if (result.Succeeded)
		{
			return RedirectToAction("Index", "Events"); // Redirects to /Events/Index
		}

		foreach (var error in result.Errors)
		{
			ModelState.AddModelError(string.Empty, error.Description);
		}

		return View(model);
	}
}
```

Read the POST action in stages. `ModelState.IsValid` checks the form-level validation rules first. The `ApplicationUser` initializer copies only the account values CEMS intends to store. `_userManager.CreateAsync(user, model.Password)` then hands the user object and plaintext submitted password to Identity for account creation and password processing. CEMS does not set `PasswordHash`.

The returned `IdentityResult` matters because passing MVC validation does not guarantee that Identity will accept the account. A password can still violate the configured Identity policy, and the email can still conflict with an existing account. When Identity returns errors, the controller adds their descriptions to `ModelState` and redisplays the same form so the user can correct the submission.

If creation succeeds, the action redirects to Events. Registration and login are intentionally separate in this lesson: creating a stored account does not automatically establish the browser's authenticated session.

<br id="modelstate">

## 7.2 ModelState

`ModelState` is provided by ASP.NET Core MVC through the controller base class:

```csharp
public class AccountController : Controller
{
	...
}
```

Before the action runs, ASP.NET Core performs **model binding**, which copies submitted form values into `RegisterViewModel`, and **model validation**, which checks rules such as `[Required]`, `[StringLength(100)]`, `[EmailAddress]`, `[DataType(DataType.Password)]`, and `[Compare(nameof(Password))]`. Any binding or validation errors are recorded in `ModelState`.

Therefore:

```csharp
if (!ModelState.IsValid)
{
    return View(model);
}
```

means that if ASP.NET Core found invalid or missing input, the registration form is displayed again with the submitted model and its validation errors.

#### File: `Views/Account/Register.cshtml`

```cshtml
@model CampusEvents.ViewModels.RegisterViewModel

@{
	ViewData["Title"] = "Register";
}

<h1>Create Account</h1>

<p>
	Create a CEMS account. An authenticated account has Participant
	capabilities; no Participant role is required.
</p>

<form asp-controller="Account"
		asp-action="Register"
		method="post"
		asp-antiforgery="true"
		class="account-form">

	<div asp-validation-summary="ModelOnly"
		class="validation-summary"
		role="alert"></div>

	<div class="form-field">
		<label asp-for="DisplayName" class="field-label"></label>
		<input asp-for="DisplayName"
				class="field-control"
				autocomplete="name"
				required />
		<span asp-validation-for="DisplayName"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Email" class="field-label"></label>
		<input asp-for="Email"
				class="field-control"
				autocomplete="email"
				required />
		<span asp-validation-for="Email"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Password" class="field-label"></label>
		<input asp-for="Password"
				class="field-control"
				autocomplete="new-password"
				required />
		<span asp-validation-for="Password"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="ConfirmPassword" class="field-label"></label>
		<input asp-for="ConfirmPassword"
				class="field-control"
				autocomplete="new-password"
				required />
		<span asp-validation-for="ConfirmPassword"
				class="validation-message"></span>
	</div>

	<button type="submit" class="button-primary">
		Create account
	</button>
</form>
```

The Razor View binds each input to `RegisterViewModel` with `asp-for`, displays field errors with `asp-validation-for`, and includes a model-level validation summary for errors returned by Identity. The form also includes anti-forgery protection because account creation changes server-side state.

A modern browser may still perform simple HTML validation such as `required`, but server-side `ModelState` and Identity validation remain authoritative because a request can be sent without using this rendered form.

Build and run it:

```cmd
dotnet build
dotnet run
```

Open:

```text
/Account/Register
```

Create a unique account with a password that meets the configured policy. Successful registration creates the account and redirects to Events. It does not automatically log the user in.

---

<br id="enforce-a-unique-login-identity">

# 8. Enforce a Unique Login Identity

A login identity is useful only when it identifies one account unambiguously. If two accounts could share the same email, a later login request would not have a single account to authenticate. That is why uniqueness is not merely a friendly form suggestion; it is part of the account system's data rules.

A registration attempt with the same email should reject creation, and the View should redisplay the validation errors. The application does not rely on a client-side “is this email available?” check as the final authority. The configured Identity user store performs the authoritative account-creation check.

---

<br id="build-login-and-preserve-safe-return-state">

# 9. Build Login and Preserve Safe Return State

The login flow also has to remember where the user was trying to go. That return destination belongs to the current request flow, not to the user's permanent account record.

```text
request protected page while anonymous
	↓
redirect to Login + ReturnUrl
	↓
POST credentials + ReturnUrl
	↓
SignInManager verifies credentials
	↓
valid? establish authenticated session
	↓
local ReturnUrl? go there, otherwise → Events
```

#### File: `ViewModels/LoginViewModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class LoginViewModel
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	public string? ReturnUrl { get; set; }
}
```

`ReturnUrl` is request/UI state, not persisted user data. `Email` and `Password` are the credentials submitted for this authentication attempt; `ReturnUrl` is only navigation context carried through the form. Nothing in this view model needs to become a new EF Core entity.

Build it:

```cmd
dotnet build
```

<br id="add-login">

## 9.1 Add Login

Evolve `AccountController` to inject `SignInManager<ApplicationUser>` and add Login. `UserManager` remains responsible for user/account operations such as creation; `SignInManager` coordinates sign-in and sign-out behavior. Keeping those responsibilities in framework services prevents the controller from becoming a password-verification implementation.

#### File: `Controllers/AccountController.cs`

```csharp
using CampusEvents.Models;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Controllers;

public class AccountController : Controller
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly SignInManager<ApplicationUser> _signInManager;

	public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
	{
		_userManager = userManager;
		_signInManager = signInManager;
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Register()
	{
		return View();
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Register(RegisterViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var user = new ApplicationUser
		{
			UserName = model.Email,
			Email = model.Email,
			DisplayName = model.DisplayName
		};

		var result = await _userManager.CreateAsync(user, model.Password);

		if (result.Succeeded)
		{
			return RedirectToAction("Index", "Events");
		}

		foreach (var error in result.Errors)
		{
			ModelState.AddModelError(string.Empty, error.Description);
		}

		return View(model);
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Login(string? returnUrl = null)
	{
		return View(new LoginViewModel
		{
			ReturnUrl = returnUrl
		});
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(LoginViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var result = await _signInManager.PasswordSignInAsync(
			model.Email,
			model.Password,
			isPersistent: false,
			lockoutOnFailure: false);

		if (result.Succeeded)
		{
			if (Url.IsLocalUrl(model.ReturnUrl))
			{
				return LocalRedirect(model.ReturnUrl);
			}

			return RedirectToAction("Index", "Events");
		}

		ModelState.AddModelError(string.Empty, "Invalid login attempt.");

		return View(model);
	}
}
```

The POST again starts with `ModelState`: malformed input does not need to reach the authentication service. `PasswordSignInAsync` then asks Identity to authenticate the submitted email/password pair and, on success, establish the application's cookie-based signed-in state. `isPersistent: false` means this lesson is not asking for a long-lived “remember me” login, and `lockoutOnFailure: false` means this sign-in call is not configured to count this failed attempt toward lockout.

Only after authentication succeeds does the controller consider the return destination. That order matters: an anonymous request should never gain access to a protected destination merely because it supplied a `ReturnUrl`. The value is also checked with `Url.IsLocalUrl` before `LocalRedirect`, so successful authentication cannot be used as a trampoline to an arbitrary external address.

On failure, the controller adds one model-level error — `Invalid login attempt.` — regardless of whether the submitted email exists. That is the generic failure behavior introduced earlier.

#### File: `Views/Account/Login.cshtml`

```cshtml
@model CampusEvents.ViewModels.LoginViewModel

@{
	ViewData["Title"] = "Login";
}

<h1>Login</h1>

<form asp-controller="Account"
		asp-action="Login"
		method="post"
		asp-antiforgery="true"
		class="account-form">

	<input asp-for="ReturnUrl" type="hidden" />

	<div asp-validation-summary="ModelOnly"
		class="validation-summary"
		role="alert"></div>

	<div class="form-field">
		<label asp-for="Email" class="field-label"></label>
		<input asp-for="Email"
				class="field-control"
				autocomplete="email"
				required />
		<span asp-validation-for="Email"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Password" class="field-label"></label>
		<input asp-for="Password"
				class="field-control"
				autocomplete="current-password"
				required />
		<span asp-validation-for="Password"
				class="validation-message"></span>
	</div>

	<button type="submit" class="button-primary">
		Login
	</button>
</form>
```

The hidden `ReturnUrl` field is what lets the navigation context survive the round trip from Login GET to Login POST. The password field itself is not persisted by the View; it is simply submitted to the controller for Identity to verify.

Both a wrong password for a known account and credentials for an unknown account produce:

```text
Invalid login attempt.
```

<br id="configure-the-application-cookie-login-path">

## 9.2 Configure the application cookie Login path

The Login route now exists, so update `Program.cs` after the Identity service registration:

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Account/Login";
});
```

This is the concrete destination used when an anonymous request reaches an `[Authorize]`-protected action. The authorization system does not need every controller to manually redirect anonymous users. The authentication cookie configuration tells the framework where the application's login experience lives.

Build and run it:

```cmd
dotnet build
dotnet run
```

Test valid and invalid credentials.

---

<br id="add-logout-as-a-post">

# 10. Add Logout as a POST

Login creates authenticated session state; logout deliberately removes it. Because that changes server-observed authentication state, logout is modeled as a command-like POST rather than as a simple navigational GET. The action also requires an authenticated user and validates an anti-forgery token.

A GET request should normally be safe to follow for navigation and should not perform an operation whose purpose is to change session state. This matters because browsers are not the only clients that follow GET links. Search-engine crawlers, link preview generators, security scanners, and other automated tools may request GET URLs automatically. If logout were exposed as a GET endpoint, one of those automated requests could unintentionally sign a user out.

#### File: `Controllers/AccountController.cs`

Add:

```csharp
[HttpPost]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Logout()
{
    await _signInManager.SignOutAsync();

    return RedirectToAction("Index", "Events");
}
```

`SignOutAsync()` tells the authentication system to end the current signed-in session. The redirect afterward simply determines which page the now-anonymous browser sees next. Logout changes authentication-session state and is therefore implemented as a protected POST rather than as a convenience GET link.

---

<br id="define-roles-and-configure-development-secrets">

# 11. Define Roles and Configure Development Secrets

Authentication gives CEMS a current user. Roles add named permissions that only some authenticated users should have. The SRS defines two such elevated role names for this project: `Organizer` and `Administrator`. Centralizing those exact strings prevents controllers, Views, and seed code from gradually drifting into inconsistent spellings.

Remember that a role is not the same thing as an account. A normal authenticated account already has Participant capabilities. Assigning the Organizer role adds Organizer permissions; assigning the Administrator role adds Administrator access where later code checks for it.

#### File: `Security/RoleNames.cs`

```csharp
namespace CampusEvents.Security;

public static class RoleNames
{
	public const string Organizer = "Organizer";

	public const string Administrator = "Administrator";
}
```

No Participant role is created.

<br id="configure-development-only-account-secrets">

## 11.1 Configure Development-Only Account Secrets

The course also needs predictable Development accounts so you can verify role-aware behavior. Their email/password values are configuration, not application source code. Passwords especially should not be committed into `Program.cs`, a seeder class, or a tracked JSON file merely because the accounts are for Development.

ASP.NET Core user secrets provide a local Development configuration source that can supply those values without recording the actual passwords in the repository. The seeder will read configuration keys; the secret store supplies their local values.

Initialize user secrets once:

```cmd
dotnet user-secrets init
```

Set Administrator values:

```cmd
dotnet user-secrets set "SeedAccounts:Administrator:Email" "admin@campus.local"
dotnet user-secrets set "SeedAccounts:Administrator:Password" "AdminDev2026!"
```

Set Organizer values:

```cmd
dotnet user-secrets set "SeedAccounts:Organizer:Email" "organizer@campus.local"
dotnet user-secrets set "SeedAccounts:Organizer:Password" "OrganizerDev2026!"
```

These are **illustrative local-development credentials only**, not production credentials. Both examples satisfy the configured Identity password policy (at least eight characters, uppercase, lowercase, and digit). Replace these example passwords with distinct, locally generated values before running the seeder. Do not reuse them for any other environment. These commands put the values in your local Secret Manager store, not in source-controlled project files.

<br id="do-not-store-development-credentials-in-configuration-files">

## 11.2 Do not store development credentials in configuration files

Even development-only seeded-account passwords must **not** be stored in committed configuration files such as:

```text
appsettings.json
appsettings.Development.json
```

There are several reasons for this rule.

1. Source-controlled configuration has a long lifetime. Removing a password from the current version of a file does not necessarily remove it from Git history. Anyone with access to that history may still be able to recover the credential.

2. Development credentials have a tendency to be reused. A credential initially created for local development may later be copied into a shared environment, reused by a developer, or accidentally become part of a deployment process. Treating development credentials as secrets avoids establishing a practice that becomes dangerous when environments evolve.

3. Configuration files are commonly copied, published, packaged, shared, or used as deployment templates. A secret placed in one of those files can therefore travel farther than originally intended.

4. Keeping secrets separate from source code establishes the correct operational boundary: the application describes **which configuration values it requires**, while the environment supplies sensitive values. Production environments will normally use an appropriate external secret-management mechanism rather than development Secret Manager.

For local development, CEMS uses the **.NET Secret Manager (`dotnet user-secrets`)** for seeded-account passwords and related configuration. Secret Manager keeps these development values outside the project directory so they are not accidentally committed with the repository.

> **Important:** Secret Manager protects against accidentally committing development secrets to source control. It is not a production secret store and does not make its stored values encrypted production credentials. It is a development configuration mechanism for keeping sensitive values outside the repository.

---

<br id="seed-administrator-access-then-organizer-access">

# 12. Seed Administrator Access, Then Organizer Access

Development seeding is not only about convenience. It must preserve the data relationships already created in earlier lessons. The existing seeded Organizer owns Events through the fixed ID `seed-organizer-001`, so replacing that row with a new unrelated Identity user would break the meaning of those foreign keys.

The lesson therefore performs two different tasks: it ensures the role/account infrastructure exists for testing, and it **upgrades the existing Organizer identity in place** so the same user can now authenticate.

Both role/account paths remain in one idempotent seeder. Establish the Administrator role/account first, then establish the Organizer role/account while preserving the existing Event ownership identity.

The existing seed Organizer still has ID:

```text
seed-organizer-001
```

Lesson 4 upgrades that same row into a usable Organizer login after the Administrator classroom account has been established.

#### File: `Data/IdentitySeedData.cs`

```csharp
using CampusEvents.Models;
using CampusEvents.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CampusEvents.Data;

public static class IdentitySeedData
{
	private const string ExistingOrganizerId = "seed-organizer-001";
	private const string OrganizerDisplayName = "Campus Events Office";
	private const string AdministratorDisplayName = "CEMS Administrator";

	public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
	{
		var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
		var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

		var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeedData");

		await EnsureRoleAsync(roleManager, RoleNames.Administrator);

		var administratorEmail = configuration["SeedAccounts:Administrator:Email"];
		var administratorPassword = configuration["SeedAccounts:Administrator:Password"];
		var organizerEmail = configuration["SeedAccounts:Organizer:Email"];
		var organizerPassword = configuration["SeedAccounts:Organizer:Password"];

		var canSeedClassroomAccounts =
			!string.IsNullOrWhiteSpace(administratorEmail) &&
			!string.IsNullOrWhiteSpace(administratorPassword) &&
			!string.IsNullOrWhiteSpace(organizerEmail) &&
			!string.IsNullOrWhiteSpace(organizerPassword);

		if (canSeedClassroomAccounts)
		{
			await EnsureAdministratorAsync(
				userManager,
				administratorEmail!,
				administratorPassword!);
		}

		await EnsureRoleAsync(roleManager, RoleNames.Organizer);

		if (!canSeedClassroomAccounts)
		{
			logger.LogWarning(
				"Development roles exist, but accounts were not " +
				"seeded because one or more SeedAccounts user-secret values " +
				"are missing.");

			return;
		}

		await EnsureOrganizerAsync(userManager, organizerEmail!, organizerPassword!);
	}

	private static void EnsureSucceeded(IdentityResult result, string operation)
	{
		if (result.Succeeded)
		{
			return;
		}

		var descriptions = string.Join("; ", result.Errors.Select(error => error.Description));

		throw new InvalidOperationException($"Identity seeding failed while {operation}: {descriptions}");
	}

	private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
	{
		if (await roleManager.RoleExistsAsync(roleName))
		{
			return;
		}

		var result = await roleManager.CreateAsync(new IdentityRole(roleName));

		EnsureSucceeded(result, $"creating role '{roleName}'");
	}

	private static async Task EnsureAdministratorAsync(UserManager<ApplicationUser> userManager, string email, string password)
	{
		var user = await userManager.FindByEmailAsync(email);

		if (user is null)
		{
			user = new ApplicationUser
			{
				UserName = email,
				Email = email,
				DisplayName = AdministratorDisplayName
			};

			var createResult = await userManager.CreateAsync(user, password);

			EnsureSucceeded(createResult, "creating the classroom Administrator account");
		}
		else
		{
			user.DisplayName = AdministratorDisplayName;

			var updateResult = await userManager.UpdateAsync(user);

			EnsureSucceeded(updateResult, "updating the classroom Administrator account");

			if (!await userManager.HasPasswordAsync(user))
			{
				var passwordResult = await userManager.AddPasswordAsync(user, password);

				EnsureSucceeded(passwordResult, "adding the classroom Administrator password");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.Administrator))
		{
			var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Administrator);

			EnsureSucceeded(roleResult, "assigning the Administrator role");
		}
	}

	private static async Task EnsureOrganizerAsync(UserManager<ApplicationUser> userManager, string email, string password)
	{
		var user = await userManager.FindByIdAsync(ExistingOrganizerId);

		if (user is null)
		{
			user = new ApplicationUser
			{
				Id = ExistingOrganizerId,
				UserName = email,
				Email = email,
				DisplayName = OrganizerDisplayName
			};

			var createResult = await userManager.CreateAsync(user, password);

			EnsureSucceeded(createResult, "creating the classroom Organizer account");
		}
		else
		{
			if (string.IsNullOrEmpty(user.SecurityStamp))
			{
				user.SecurityStamp = Guid.NewGuid().ToString();
			}

			user.UserName = email;
			user.Email = email;
			user.DisplayName = OrganizerDisplayName;

			var updateResult = await userManager.UpdateAsync(user);

			EnsureSucceeded(updateResult, "updating the existing seed Organizer identity");

			if (!await userManager.HasPasswordAsync(user))
			{
				var passwordResult = await userManager.AddPasswordAsync(user, password);

				EnsureSucceeded(passwordResult, "adding the classroom Organizer password");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.Organizer))
		{
			var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Organizer);

			EnsureSucceeded(roleResult, "assigning the Organizer role");
		}
	}
}
```

Read the completed flow in this order:

```text
ensure Administrator role
	↓
ensure Administrator account
	↓
assign Administrator role
	↓
ensure Organizer role
	↓
upgrade/ensure seed Organizer account
	↓
assign Organizer role
```

Read the helper methods as “ensure” operations rather than unconditional inserts. Each operation first asks whether the role, user, password, or membership already exists and only creates/adds what is missing. That makes the seeder idempotent: repeated Development startup does not duplicate roles, users, passwords, or role assignments.

This is also a useful example of delegating Identity work to the framework managers. `RoleManager` owns role creation and existence checks. `UserManager` owns user lookup, account creation/update, password attachment, and role membership. The seeder coordinates those operations; it does not directly manipulate Identity tables.

#### File: `Program.cs`

The Development seed block remains:

```csharp
if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();

	var services = scope.ServiceProvider;

	var context = services.GetRequiredService<ApplicationDbContext>();

	await DbInitializer.SeedAsync(context);

	await IdentitySeedData.SeedAsync(services, builder.Configuration);
}
```

`DbInitializer` remains responsible for the established Category/Event dataset.

`IdentitySeedData` establishes the Administrator classroom account first, then upgrades the existing Organizer identity and assigns its Organizer role.

Build and run it:

```cmd
dotnet build
dotnet run
```

Verify the roles/accounts exist as expected, the six Event records remain intact, and `seed-organizer-001` still owns the seeded Events. Confirm the existing Organizer row has gained the configured Identity login/password and `Organizer` role without acquiring a new user ID; verify it can sign in. The Development seeder must not rewrite the Event ownership relationship.

---

<br id="make-navigation-authentication-aware">

# 13. Make Navigation Authentication-Aware

Once authentication state exists, the shared layout can make the interface more useful. Anonymous visitors should see ways to Register or Login. Authenticated users should see their current identity and a Logout command. Role labels can give useful context when the current user has elevated access.

This is **presentation logic**, not authorization logic. The layout is reading the already reconstructed `User` to decide what to show. It is not granting permission to controller actions. That distinction lets the UI be helpful without making security depend on whether a link happened to be rendered or not. The account routes and role constants already exist, so the layout can now respond to authentication state.

At this first navigation stage:

```text
anonymous
	→ Home / Events / Privacy / Register / Login

authenticated
	→ Home / Events / Privacy / Logout
	+ authenticated-user indicator
	+ applicable Organizer/Administrator role labels
```

#### File: `Views/Shared/_Layout.cshtml`

Use this intermediate authentication-aware layout:

```cshtml
@using CampusEvents.Security

<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<title>@ViewData["Title"] - CampusEvents</title>
	<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
	<header class="site-header">
		<div class="site-shell site-header-inner">
			<a class="site-brand"
				asp-controller="Home"
				asp-action="Index">CampusEvents</a>

			<nav class="site-nav" aria-label="Primary navigation">
				<ul class="site-nav-list">
					<li>
						<a class="site-nav-link"
							asp-controller="Home"
							asp-action="Index">Home</a>
					</li>
					<li>
						<a class="site-nav-link"
							asp-controller="Events"
							asp-action="Index">Events</a>
					</li>
					<li>
						<a class="site-nav-link"
							asp-controller="Home"
							asp-action="Privacy">Privacy</a>
					</li>

					@if (User.Identity?.IsAuthenticated != true)
					{
						<li>
							<a class="site-nav-link"
								asp-controller="Account"
								asp-action="Register">Register</a>
						</li>
						<li>
							<a class="site-nav-link"
								asp-controller="Account"
								asp-action="Login">Login</a>
						</li>
					}
				</ul>

				@if (User.Identity?.IsAuthenticated == true)
				{
					<div class="auth-status">
						<span>@User.Identity.Name</span>

						@if (User.IsInRole(RoleNames.Organizer))
						{
							<span class="role-label">Organizer</span>
						}

						@if (User.IsInRole(RoleNames.Administrator))
						{
							<span class="role-label">Administrator</span>
						}

						<form asp-controller="Account"
								asp-action="Logout"
								method="post"
								asp-antiforgery="true"
								class="logout-form">
							<button type="submit"
									class="link-button">
								Logout
							</button>
						</form>
					</div>
				}
			</nav>
		</div>
	</header>

	<main class="site-shell page-content">
		@RenderBody()
	</main>

	<footer class="site-footer">
		<div class="site-shell">
			<span>&copy; CampusEvents</span>
		</div>
	</footer>
</body>
</html>
```

`User.Identity?.IsAuthenticated` answers the baseline signed-in question. `User.IsInRole(...)` asks the separate role-membership question. Notice how the View uses both without confusing them: every authenticated user gets the normal signed-in UI, while Organizer/Administrator labels appear only when the corresponding elevated role is present.

Build and run it:

```cmd
dotnet build
dotnet run
```

Verify:

- anonymous users see Register and Login
- authenticated users see their identity indicator and Logout
- Organizer/Administrator labels appear only for matching roles
- the Logout form still uses POST and anti-forgery protection

---

<br id="add-the-protected-participant-page-then-finish-navigation">

# 14. Add the Protected Participant Page, Then Finish Navigation

So far the application can create users, authenticate them, remember the signed-in state, and render different navigation. None of that alone proves that the server will refuse an anonymous request to protected code. The next step creates a deliberately small endpoint whose only important behavior is its access boundary.

The initial authentication-aware navigation now exists. Next, introduce the deliberately small protected endpoint; only after that route exists will the layout link to it.

<br id="introduce-the-protected-participant-endpoint">

## 14.1 Introduce the protected Participant endpoint

Protected route:

```text
/Participant
```

#### File: `Controllers/ParticipantController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Controllers;

public class ParticipantController : Controller
{
	[Authorize]
	public IActionResult Index()
	{
		return View();
	}
}
```

#### File: `Views/Participant/Index.cshtml`

```cshtml
@{
	ViewData["Title"] = "Participant Area";
}

<h1>Participant Area</h1>

<p>
	You are authenticated and can access this protected page.
</p>

<p>
	Event-registration features are not implemented yet.
</p>
```

`[Authorize]` is evaluated by the authorization system before the action is allowed to run. With no role or policy specified, this action requires an authenticated user but does not require Organizer or Administrator membership. That matches the CEMS rule that Participant is the authenticated baseline rather than an Identity role.

The View is intentionally plain because the product feature being taught here is protection, not Participant Event Registration. Later lessons will place real Participant workflows behind the authenticated boundary once their domain rules exist.

> This route requires authentication but no Organizer/Administrator role.

<br id="return-to-layout-cshtml-and-add-the-final-participant-area-link">

## 14.2 Return to `_Layout.cshtml` and add the final Participant Area link

The `/Participant` route now exists, so the layout may safely reference it.

The complete final layout is:

```cshtml
@using CampusEvents.Security

<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<title>@ViewData["Title"] - CampusEvents</title>
	<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
	<header class="site-header">
		<div class="site-shell site-header-inner">
			<a class="site-brand"
				asp-controller="Home"
				asp-action="Index">CampusEvents</a>

			<nav class="site-nav" aria-label="Primary navigation">
				<ul class="site-nav-list">
					<li>
						<a class="site-nav-link"
							asp-controller="Home"
							asp-action="Index">Home</a>
					</li>
					<li>
						<a class="site-nav-link"
							asp-controller="Events"
							asp-action="Index">Events</a>
					</li>
					<li>
						<a class="site-nav-link"
							asp-controller="Home"
							asp-action="Privacy">Privacy</a>
					</li>

					@if (User.Identity?.IsAuthenticated == true)
					{
						<li>
							<a class="site-nav-link"
								asp-controller="Participant"
								asp-action="Index">Participant Area</a>
						</li>
					}
					else
					{
						<li>
							<a class="site-nav-link"
								asp-controller="Account"
								asp-action="Register">Register</a>
						</li>
						<li>
							<a class="site-nav-link"
								asp-controller="Account"
								asp-action="Login">Login</a>
						</li>
					}
				</ul>

				@if (User.Identity?.IsAuthenticated == true)
				{
					<div class="auth-status">
						<span>@User.Identity.Name</span>

						@if (User.IsInRole(RoleNames.Organizer))
						{
							<span class="role-label">Organizer</span>
						}

						@if (User.IsInRole(RoleNames.Administrator))
						{
							<span class="role-label">Administrator</span>
						}

						<form asp-controller="Account"
								asp-action="Logout"
								method="post"
								asp-antiforgery="true"
								class="logout-form">
							<button type="submit"
									class="link-button">
								Logout
							</button>
						</form>
					</div>
				}
			</nav>
		</div>
	</header>

	<main class="site-shell page-content">
		@RenderBody()
	</main>

	<footer class="site-footer">
		<div class="site-shell">
			<span>&copy; CampusEvents</span>
		</div>
	</footer>
</body>
</html>
```

This is the final navigation state.

Anonymous actions are:

```text
Home | Events | Privacy | Register | Login
```

Authenticated actions are:

```text
Home | Events | Privacy | Participant Area | Logout
```

Organizer/Administrator role labels continue to appear only for matching roles. No Organizer or Administrator product-management links are introduced.

Build and run it:

```cmd
dotnet build
dotnet run
```

Verify:

- authenticated users now see `Participant Area`
- anonymous users still see Register/Login rather than the `Participant Area` link

---

<br id="test-direct-url-authorization-and-login-return-flow">

# 15. Test Direct URL Authorization and Login Return Flow

Now trace the entire protected-request story end to end. This verification matters because authentication, cookie configuration, authorization, the Login actions, and the hidden `ReturnUrl` field only make sense as one connected request flow.

An anonymous request should never execute `ParticipantController.Index`. Instead, authorization detects that authentication is required, the cookie handler redirects to the configured Login path, and the original local destination travels through the login form via hidden `ReturnUrl` input and then via `ReturnUrl` query string after form submission. After successful authentication, the controller validates that destination and returns the user to it.

<br id="prove-that-hidden-ui-is-not-authorization">

## 15.1 Prove That Hidden UI Is Not Authorization

Log out. Notice that the *Participant Area* link disappears from the UI.

Now request `/Participant` via URL.

The request is still blocked because:

```csharp
[Authorize]
```

is the server boundary.

The authentication cookie handler redirects to Login with a return destination equivalent to:

```text
/Account/Login?ReturnUrl=%2FParticipant
```

The Login GET action handler accepts:

```csharp
Login(string? returnUrl)
```

and the View preserves it through:

```cshtml
<input asp-for="ReturnUrl" type="hidden" />
```

After successful authentication:

```csharp
if (Url.IsLocalUrl(model.ReturnUrl))
{
	return LocalRedirect(model.ReturnUrl);
}
```

Only local return targets are followed. Notice that the return URL controls **navigation after successful authentication**; it does not bypass authentication or authorization. The protected action remains responsible for requiring an authenticated user.

This final check deliberately avoids the normal navigation. If security depended on the layout hiding a link, typing the route manually would defeat the design. Instead, the server evaluates the request independently of how the user discovered the URL.

This directly demonstrates `BR-AUT-004` and `NFR-SEC-001`.

---

<br id="inspect-anti-forgery-protection">

# 16. Inspect Anti-Forgery Protection

Authorization answers whether the current user may perform an operation. Anti-forgery protection answers a different security question for state-changing browser forms: did this POST carry the framework token expected from the application's own form flow? Both checks can matter on the same action.

The Logout form contains:

```cshtml
<form asp-controller="Account"
		asp-action="Logout"
		method="post"
		asp-antiforgery="true"
		class="logout-form">
```

The action contains:

```csharp
[ValidateAntiForgeryToken]
```

Inspect rendered HTML (press CTRL+SHIFT+I or CTRL+SHIFT+C or F12) and locate the generated hidden anti-forgery token. The Razor form generates the token value; `[ValidateAntiForgeryToken]` validates the submitted request. The application does not need to manually generate, store, or compare the token.

Registration and Login use the same POST protection pattern. In later lessons, use the same reasoning for other state-changing form submissions: UI visibility, authentication/authorization, model/business validation, and anti-forgery protection solve different problems and should not be substituted for one another.

---

<br id="guided-verification">

# 17. Guided Verification

Do not treat the following scenarios as a click-through checklist only. For each one, identify which layer is responsible for the observed result: MVC model binding/`ModelState`, Identity account management, `SignInManager`, authentication middleware, authorization, role membership, Razor conditional rendering, anti-forgery protection, or the existing EF Core catalog behavior. The goal is to prove that the pieces work together and to practice locating the authoritative decision in the stack.

<br id="verify-retained-events-behavior">

## Verify Retained Events Behavior

Before testing the new Identity flows, confirm that Lesson 3 still works after the styling and authentication changes. Use published upcoming seed Events and, for multi-page navigation, the expanded Development dataset from Lesson 3.

| Request or action | Expected result |
|---|---|
| `/Events` | HTTP 200; public Published/upcoming catalog |
| `/Events?pageSize=25` | HTTP 200; selected size is retained |
| `/Events?pageSize=1` | HTTP 400; `Views/Shared/BadRequest.cshtml` renders the invalid-size message |
| Valid search with no matches | HTTP 200; no-results message (not an error page) |
| Existing published Event Details | HTTP 200; Event details shown |
| Missing Event Details | HTTP 404; preserve the existing Lesson 2 not-found behavior and any feature-specific View used there |
| First/Previous/Next/Last | Preserve search, Category, page number, and page size |
| Clear filters | Clears search/Category while retaining the chosen page size |
| Anonymous `/Participant` | Redirects to Login |
| Authenticated `/Participant` | Protected Participant page renders |

Inspect HTTP status codes in browser Developer Tools, not only the visual page. The 400 error View remains a public request-validation response; adding Identity must not turn it into an authorization failure.

<br id="verify-the-account-and-access-matrix">

## 17.1 Verify the Account and Access Matrix

### Scenario 1 — Register a normal participant account

While signed out, open:

```text
/Account/Register
```

Create a new account with a unique email and valid password.

Confirm:

- the account is created
- the browser returns to Events
- the user is not silently assigned Organizer or Administrator access

<br>

### Scenario 2 — Duplicate account identity

Submit registration again with the same email.

Confirm:

- creation is rejected
- validation feedback appears
- no duplicate login identity is created

<br>

## Scenario 3 — Valid login

Open:

```text
/Account/Login
```

Use the account from Scenario 1.

Confirm:

- login succeeds
- authenticated navigation appears
- no Participant role was required

<br>

## Scenario 4 — Invalid login

Test:

```text
known email + wrong password
```

and:

```text
unknown email + arbitrary password
```

Both should display:

```text
Invalid login attempt.
```

<br>

## Scenario 5 — Logout

Log in and use the Logout button.

Confirm:

- Logout uses POST
- the authenticated session ends
- `Participant Area` link is hidden

<br>

## Scenario 6 — Protected direct URL

While anonymous, request:

```text
/Participant
```

via URL.

Confirm:

- the protected page does not render anonymously
- Login is shown through an authentication redirect
- a return destination is preserved

<br>

## Scenario 7 — Login return flow

From Scenario 6, log in.

Confirm that successful authentication returns to:

```text
/Participant
```

<br>

## Scenario 8 — Participant is not a role

Log in with the ordinary participant account.

Confirm:

- `/Participant` works
- the user is authenticated
- a Participant role was not created

Explain:

```text
Participant capability = authenticated baseline
```

<br>

## Scenario 9 — Administrator classroom account

Log in with the locally configured Administrator account.

Confirm:

- the layout displays `Administrator`
- no Administrator product-management destination exists yet

<br>

## Scenario 10 — Organizer classroom account

Log in with the locally configured Organizer Development account.

Confirm:

- the layout displays `Organizer`
- existing seeded Events still resolve the Organizer relationship through `seed-organizer-001`
- no Organizer management destination exists yet

<br>

## Scenario 11 — Anti-forgery

Inspect the rendered Logout form and identify the hidden anti-forgery token.

Connect it to:

```csharp
[ValidateAntiForgeryToken]
```

on the Logout action.

<br>

## Scenario 12 — Public-catalog regression

Verify while anonymous and authenticated:

- `/Events` still loads
- search still works
- Category filtering still works
- combined filtering still works
- a new search starts on page 1
- the result range and total count describe the filtered set
- First, Previous, Next, and Last retain the active search text, Category selection, and page size
- chronological ordering remains stable across page boundaries
- the first and last pages show only the links that apply
- one-page and no-results searches do not show pagination links
- Details links still work
- invalid Event IDs still produce controlled not-found behavior

---

<br id="small-change-add-authenticated-identity-text">

# 18. Small Change — Add Authenticated Identity Text

This is a bounded modification using authentication state already introduced.

Do not add a new service, role, query, or controller.

#### File: `Views/Participant/Index.cshtml`

After the heading, add:

```cshtml
<p>
	Signed in as <strong>@User.Identity?.Name</strong>.
</p>
```

Test with:

- a normal participant account
- the Organizer classroom account
- the Administrator classroom account

This display refinement is part of the final Lesson 4 codebase state (so do it!).

---

<br id="check-your-understanding">

# 19. Check Your Understanding

Try to answer these questions by yourself, then verify your answers against those given.

<details>
<summary>What is the difference between authentication and authorization?</summary>

Authentication establishes who the current user is. Authorization decides whether that authenticated user may access a particular operation.

</details>

<details>
<summary>Why does CEMS not create a `"Participant"` Identity role?</summary>

Participant is the baseline capability of an authenticated CEMS user. Organizer and Administrator are additional roles layered on top of that authenticated baseline.

</details>

<details>
<summary>Why does invalid Login always use the generic message `Invalid login attempt.`?</summary>

The response should not reveal whether a particular account exists or whether only the password was wrong.

</details>

<details>
<summary>Why is Logout implemented as a POST with anti-forgery validation?</summary>

Logout changes authentication-session state, so it is a write operation, not a read operation. Anti-forgery validation is necessary to avoid CSRF attacks.

</details>

<details>
<summary>Why is hiding the Participant Area link from anonymous users not enough to secure `/Participant`?</summary>

A user can request the URL directly. The `[Authorize]` attribute on the server is the actual security boundary.

</details>

<details>
<summary>Why does Login validate the return URL before redirecting?</summary>

The return destination comes from request input. Restricting it to a local URL prevents a successful login from becoming an open redirect to an arbitrary external destination.

</details>

---

<br id="common-mistakes-debugging-notes">

# 20. Common Mistakes / Debugging Notes

<br id="applicationuser-still-declares-its-own-id">

## 20.1 `ApplicationUser` still declares its own `Id`

Do not keep a second custom `Id` after inheriting from `IdentityUser`.

<br id="applicationdbcontext-still-inherits-from-dbcontext">

## 20.2 `ApplicationDbContext` still inherits from `DbContext`

Final inheritance:

```csharp
IdentityDbContext<ApplicationUser>
```

and `base.OnModelCreating(modelBuilder)` must execute.

<br id="specific-invalid-login-feedback">

## 20.3 Specific Invalid Login Feedback

A login form should not help an unauthenticated visitor discover which email addresses are registered. If the application displayed one message for an unknown email and another for a wrong password, those different responses would reveal account-existence information.

Use the same visible failure for invalid credentials:

```text
Invalid login attempt.
```

<br id="login-redirects-to-an-arbitrary-supplied-url">

## 20.4 Login redirects to an arbitrary supplied URL

Keep the `Url.IsLocalUrl(...)` check before `LocalRedirect(...)`.

<br id="logout-is-get">

## 20.5 Logout is GET

Final Logout is an authenticated anti-forgery-protected POST.

<br id="seed-passwords-are-hard-coded">

## 20.6 Seed passwords are hard-coded

Use the established `SeedAccounts:*` user-secret keys.

<br id="hidden-navigation-replaces-authorize">

## 20.7 Hidden navigation replaces `[Authorize]`

Navigation visibility is not a security boundary.

---

<br id="final-code-check">

# 21. Final Code Check

**Retained Lesson 3 files (not replaced by this lesson):** `Controllers/EventsController.cs`, `ViewModels/EventSearchViewModel.cs`, `ViewModels/ErrorPageViewModel.cs`, `Views/Events/Index.cshtml`, `Views/Shared/BadRequest.cshtml`, and the existing `Views/Events/Details.cshtml` and not-found handling. Keep the Event search, filtering, ordered paging, allowed sizes (10, 25, 50), HTTP 400 error rendering, and HTTP 404 Details behavior. The Index View and its CSS may be restyled in Section 6, but their request/response contract is unchanged.

The following code snapshots primarily show Lesson 4 additions and modifications. **Omission from a snapshot does not mean that an existing Lesson 3 file should be deleted.**

#### File: `Security/RoleNames.cs`

```csharp
namespace CampusEvents.Security;

public static class RoleNames
{
	public const string Organizer = "Organizer";

	public const string Administrator = "Administrator";
}
```

#### File: `Models/ApplicationUser.cs`

In a terminal, at the project root folder run:

```cmd
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

```csharp
using Microsoft.AspNetCore.Identity;

namespace CampusEvents.Models;

public class ApplicationUser : IdentityUser
{
	public string DisplayName { get; set; } = string.Empty;

	public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
}
```

#### File: `Data/IdentitySeedData.cs`

```csharp
using CampusEvents.Models;
using CampusEvents.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CampusEvents.Data;

public static class IdentitySeedData
{
	private const string ExistingOrganizerId = "seed-organizer-001";
	private const string OrganizerDisplayName = "Campus Events Office";
	private const string AdministratorDisplayName = "CEMS Administrator";

	public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
	{
		var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
		var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

		var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeedData");

		await EnsureRoleAsync(roleManager, RoleNames.Administrator);

		var administratorEmail = configuration["SeedAccounts:Administrator:Email"];
		var administratorPassword = configuration["SeedAccounts:Administrator:Password"];
		var organizerEmail = configuration["SeedAccounts:Organizer:Email"];
		var organizerPassword = configuration["SeedAccounts:Organizer:Password"];

		var canSeedClassroomAccounts =
			!string.IsNullOrWhiteSpace(administratorEmail) &&
			!string.IsNullOrWhiteSpace(administratorPassword) &&
			!string.IsNullOrWhiteSpace(organizerEmail) &&
			!string.IsNullOrWhiteSpace(organizerPassword);

		if (canSeedClassroomAccounts)
		{
			await EnsureAdministratorAsync(
				userManager,
				administratorEmail!,
				administratorPassword!);
		}

		await EnsureRoleAsync(roleManager, RoleNames.Organizer);

		if (!canSeedClassroomAccounts)
		{
			logger.LogWarning(
				"Development roles exist, but accounts were not " +
				"seeded because one or more SeedAccounts user-secret values " +
				"are missing.");

			return;
		}

		await EnsureOrganizerAsync(userManager, organizerEmail!, organizerPassword!);
	}

	private static void EnsureSucceeded(IdentityResult result, string operation)
	{
		if (result.Succeeded)
		{
			return;
		}

		var descriptions = string.Join("; ", result.Errors.Select(error => error.Description));

		throw new InvalidOperationException($"Identity seeding failed while {operation}: {descriptions}");
	}

	private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
	{
		if (await roleManager.RoleExistsAsync(roleName))
		{
			return;
		}

		var result = await roleManager.CreateAsync(new IdentityRole(roleName));

		EnsureSucceeded(result, $"creating role '{roleName}'");
	}

	private static async Task EnsureAdministratorAsync(UserManager<ApplicationUser> userManager, string email, string password)
	{
		var user = await userManager.FindByEmailAsync(email);

		if (user is null)
		{
			user = new ApplicationUser
			{
				UserName = email,
				Email = email,
				DisplayName = AdministratorDisplayName
			};

			var createResult = await userManager.CreateAsync(user, password);

			EnsureSucceeded(createResult, "creating the classroom Administrator account");
		}
		else
		{
			user.DisplayName = AdministratorDisplayName;

			var updateResult = await userManager.UpdateAsync(user);

			EnsureSucceeded(updateResult, "updating the classroom Administrator account");

			if (!await userManager.HasPasswordAsync(user))
			{
				var passwordResult = await userManager.AddPasswordAsync(user, password);

				EnsureSucceeded(passwordResult, "adding the classroom Administrator password");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.Administrator))
		{
			var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Administrator);

			EnsureSucceeded(roleResult, "assigning the Administrator role");
		}
	}

	private static async Task EnsureOrganizerAsync(UserManager<ApplicationUser> userManager, string email, string password)
	{
		var user = await userManager.FindByIdAsync(ExistingOrganizerId);

		if (user is null)
		{
			user = new ApplicationUser
			{
				Id = ExistingOrganizerId,
				UserName = email,
				Email = email,
				DisplayName = OrganizerDisplayName
			};

			var createResult = await userManager.CreateAsync(user, password);

			EnsureSucceeded(createResult, "creating the classroom Organizer account");
		}
		else
		{
			if (string.IsNullOrEmpty(user.SecurityStamp))
			{
				user.SecurityStamp = Guid.NewGuid().ToString();
			}

			user.UserName = email;
			user.Email = email;
			user.DisplayName = OrganizerDisplayName;

			var updateResult = await userManager.UpdateAsync(user);

			EnsureSucceeded(updateResult, "updating the existing seed Organizer identity");

			if (!await userManager.HasPasswordAsync(user))
			{
				var passwordResult = await userManager.AddPasswordAsync(user, password);

				EnsureSucceeded(passwordResult, "adding the classroom Organizer password");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.Organizer))
		{
			var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Organizer);

			EnsureSucceeded(roleResult, "assigning the Organizer role");
		}
	}
}

```

#### File: `Data/ApplicationDbContext.cs`

```csharp
using CampusEvents.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{

	}

	public DbSet<Category> Categories => Set<Category>();

	public DbSet<Event> Events => Set<Event>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<ApplicationUser>(entity =>
		{
			entity.Property(user => user.DisplayName).IsRequired();
		});

		modelBuilder.Entity<Category>(entity =>
		{
			entity.Property(category => category.Name).IsRequired();
		});

		modelBuilder.Entity<Event>(entity =>
		{
			entity.Property(eventItem => eventItem.Title).IsRequired();
			entity.Property(eventItem => eventItem.Description).IsRequired();
			entity.Property(eventItem => eventItem.Location).IsRequired();

			entity.HasOne(eventItem => eventItem.Category)
				.WithMany(category => category.Events)
				.HasForeignKey(eventItem => eventItem.CategoryId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(eventItem => eventItem.Organizer)
				.WithMany(user => user.OrganizedEvents)
				.HasForeignKey(eventItem => eventItem.OrganizerId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.ToTable("Events", table =>
			{
				table.HasCheckConstraint(
					"CK_Events_StartBeforeEnd",
					"[StartDateTime] < [EndDateTime]");

				table.HasCheckConstraint(
					"CK_Events_CapacityPositive",
					"[Capacity] > 0");
			});
		});
	}
}
```

#### File: `ViewModels/RegisterViewModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class RegisterViewModel
{
	[Required]
	[StringLength(100)]
	public string DisplayName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	[Compare(nameof(Password))]
	public string ConfirmPassword { get; set; } = string.Empty;
}
```

#### File: `ViewModels/LoginViewModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class LoginViewModel
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	public string? ReturnUrl { get; set; }
}
```

#### File: `Controllers/AccountController.cs`

```csharp
using CampusEvents.Models;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Controllers;

public class AccountController : Controller
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly SignInManager<ApplicationUser> _signInManager;

	public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
	{
		_userManager = userManager;
		_signInManager = signInManager;
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Register()
	{
		return View();
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Register(RegisterViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var user = new ApplicationUser
		{
			UserName = model.Email,
			Email = model.Email,
			DisplayName = model.DisplayName
		};

		var result = await _userManager.CreateAsync(user, model.Password);

		if (result.Succeeded)
		{
			return RedirectToAction("Index", "Events");
		}

		foreach (var error in result.Errors)
		{
			ModelState.AddModelError(string.Empty, error.Description);
		}

		return View(model);
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Login(string? returnUrl = null)
	{
		return View(new LoginViewModel
		{
			ReturnUrl = returnUrl
		});
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(LoginViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var result = await _signInManager.PasswordSignInAsync(
			model.Email,
			model.Password,
			isPersistent: false,
			lockoutOnFailure: false);

		if (result.Succeeded)
		{
			if (Url.IsLocalUrl(model.ReturnUrl))
			{
				return LocalRedirect(model.ReturnUrl);
			}

			return RedirectToAction("Index", "Events");
		}

		ModelState.AddModelError(string.Empty, "Invalid login attempt.");

		return View(model);
	}

	[HttpPost]
	[Authorize]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Logout()
	{
		await _signInManager.SignOutAsync();

		return RedirectToAction("Index", "Events");
	}
}
```

#### File: `Views/Account/Register.cshtml`

```cshtml
@model CampusEvents.ViewModels.RegisterViewModel

@{
	ViewData["Title"] = "Register";
}

<h1>Create Account</h1>

<p>
	Create a CEMS account. An authenticated account has Participant
	capabilities; no Participant role is required.
</p>

<form asp-controller="Account"
		asp-action="Register"
		method="post"
		asp-antiforgery="true"
		class="account-form">

	<div asp-validation-summary="ModelOnly"
		class="validation-summary"
		role="alert"></div>

	<div class="form-field">
		<label asp-for="DisplayName" class="field-label"></label>
		<input asp-for="DisplayName"
				class="field-control"
				autocomplete="name"
				required />
		<span asp-validation-for="DisplayName"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Email" class="field-label"></label>
		<input asp-for="Email"
				class="field-control"
				autocomplete="email"
				required />
		<span asp-validation-for="Email"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Password" class="field-label"></label>
		<input asp-for="Password"
				class="field-control"
				autocomplete="new-password"
				required />
		<span asp-validation-for="Password"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="ConfirmPassword" class="field-label"></label>
		<input asp-for="ConfirmPassword"
				class="field-control"
				autocomplete="new-password"
				required />
		<span asp-validation-for="ConfirmPassword"
				class="validation-message"></span>
	</div>

	<button type="submit" class="button-primary">
		Create account
	</button>
</form>
```

#### File: `Views/Account/Login.cshtml`

```cshtml
@model CampusEvents.ViewModels.LoginViewModel

@{
	ViewData["Title"] = "Login";
}

<h1>Login</h1>

<form asp-controller="Account"
		asp-action="Login"
		method="post"
		asp-antiforgery="true"
		class="account-form">

	<input asp-for="ReturnUrl" type="hidden" />

	<div asp-validation-summary="ModelOnly"
		class="validation-summary"
		role="alert"></div>

	<div class="form-field">
		<label asp-for="Email" class="field-label"></label>
		<input asp-for="Email"
				class="field-control"
				autocomplete="email"
				required />
		<span asp-validation-for="Email"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Password" class="field-label"></label>
		<input asp-for="Password"
				class="field-control"
				autocomplete="current-password"
				required />
		<span asp-validation-for="Password"
				class="validation-message"></span>
	</div>

	<button type="submit" class="button-primary">
		Login
	</button>
</form>
```

#### File: `Controllers/ParticipantController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Controllers;

public class ParticipantController : Controller
{
	[Authorize]
	public IActionResult Index()
	{
		return View();
	}
}
```

#### File: `Views/Participant/Index.cshtml`

Use this final View:

```cshtml
@{
	ViewData["Title"] = "Participant Area";
}

<h1>Participant Area</h1>

<p>
	Signed in as <strong>@User.Identity?.Name</strong>.
</p>

<p>
	You are authenticated and can access this protected page.
</p>

<p>
	Event-registration features are not implemented yet.
</p>
```

#### File: `Views/Shared/_Layout.cshtml`

```cshtml
@using CampusEvents.Security

<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<title>@ViewData["Title"] - CampusEvents</title>
	<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
	<header class="site-header">
		<div class="site-shell site-header-inner">
			<a class="site-brand"
				asp-controller="Home"
				asp-action="Index">CampusEvents</a>

			<nav class="site-nav" aria-label="Primary navigation">
				<ul class="site-nav-list">
					<li>
						<a class="site-nav-link"
							asp-controller="Home"
							asp-action="Index">Home</a>
					</li>
					<li>
						<a class="site-nav-link"
							asp-controller="Events"
							asp-action="Index">Events</a>
					</li>
					<li>
						<a class="site-nav-link"
							asp-controller="Home"
							asp-action="Privacy">Privacy</a>
					</li>

					@if (User.Identity?.IsAuthenticated == true)
					{
						<li>
							<a class="site-nav-link"
								asp-controller="Participant"
								asp-action="Index">Participant Area</a>
						</li>
					}
					else
					{
						<li>
							<a class="site-nav-link"
								asp-controller="Account"
								asp-action="Register">Register</a>
						</li>
						<li>
							<a class="site-nav-link"
								asp-controller="Account"
								asp-action="Login">Login</a>
						</li>
					}
				</ul>

				@if (User.Identity?.IsAuthenticated == true)
				{
					<div class="auth-status">
						<span>@User.Identity.Name</span>

						@if (User.IsInRole(RoleNames.Organizer))
						{
							<span class="role-label">Organizer</span>
						}

						@if (User.IsInRole(RoleNames.Administrator))
						{
							<span class="role-label">Administrator</span>
						}

						<form asp-controller="Account"
								asp-action="Logout"
								method="post"
								asp-antiforgery="true"
								class="logout-form">
							<button type="submit"
									class="link-button">
								Logout
							</button>
						</form>
					</div>
				}
			</nav>
		</div>
	</header>

	<main class="site-shell page-content">
		@RenderBody()
	</main>

	<footer class="site-footer">
		<div class="site-shell">
			<span>&copy; CampusEvents</span>
		</div>
	</footer>
</body>
</html>
```

#### File: `wwwroot/css/site.css`

Replace the inherited stylesheet with project-owned CSS:

```css
:root {
	font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
	line-height: 1.5;
	color: #1f2328;
	background: #ffffff;
}

* {
	box-sizing: border-box;
}

body {
	min-height: 100vh;
	margin: 0;
	display: flex;
	flex-direction: column;
}

a {
	color: #174ea6;
}

a:focus-visible,
button:focus-visible,
input:focus-visible,
select:focus-visible {
	outline: 3px solid #7aa7e8;
	outline-offset: 2px;
}

.site-shell {
	width: min(72rem, calc(100% - 2rem));
	margin-inline: auto;
}

.site-header {
	border-bottom: 1px solid #d0d7de;
	background: #f6f8fa;
}

.site-header-inner {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 1rem;
	padding-block: 0.85rem;
}

.site-brand {
	color: #1f2328;
	font-weight: 700;
	text-decoration: none;
}

.site-nav {
	flex: 1;
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	justify-content: space-between;
	gap: 1rem;
}

.site-nav-list {
	display: flex;
	flex-wrap: wrap;
	gap: 0.8rem;
	margin: 0;
	padding: 0;
	list-style: none;
}

.site-nav-link {
	display: inline-block;
	padding: 0.35rem 0;
}

.auth-status {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 0.65rem;
}

.role-label {
	padding: 0.15rem 0.45rem;
	border: 1px solid #8c959f;
	border-radius: 999px;
	font-size: 0.85rem;
}

.logout-form {
	margin: 0;
}

.link-button {
	border: 0;
	padding: 0;
	background: transparent;
	color: #174ea6;
	font: inherit;
	text-decoration: underline;
	cursor: pointer;
}

.page-content {
	flex: 1;
	padding-block: 1.5rem 2.5rem;
}

.site-footer {
	border-top: 1px solid #d0d7de;
	padding-block: 1rem;
	background: #f6f8fa;
}

.account-form {
	width: min(100%, 32rem);
	display: grid;
	gap: 1rem;
}

.form-field {
	display: grid;
	gap: 0.35rem;
}

.field-label {
	font-weight: 600;
}

.field-control {
	width: 100%;
	min-height: 2.5rem;
	padding: 0.55rem 0.65rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	background: #ffffff;
	color: inherit;
	font: inherit;
}

.button-primary,
.button-secondary {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	min-height: 2.5rem;
	padding: 0.55rem 0.8rem;
	border: 1px solid #1f2328;
	border-radius: 0.25rem;
	font: inherit;
	text-decoration: none;
	cursor: pointer;
}

.button-primary {
	background: #1f2328;
	color: #ffffff;
}

.button-secondary {
	background: #ffffff;
	color: #1f2328;
}

.validation-summary,
.validation-message {
	color: #9a0007;
}

.validation-summary:empty,
.validation-message:empty {
	display: none;
}

.event-filter-form {
	display: grid;
	grid-template-columns: minmax(0, 5fr) minmax(0, 4fr) minmax(0, 2fr) minmax(0, 3fr);
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
}

.event-filter-control {
	width: 100%;
	min-height: 2.5rem;
	padding: 0.55rem 0.65rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	background: #ffffff;
	color: inherit;
	font: inherit;
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

.event-filter-actions > * {
	flex: 1;
}

.event-result-count {
	margin: 0 0 1rem;
}

.event-page-status {
	margin: 1rem 0 0;
	font-weight: 600;
}

.event-pagination {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 0.75rem;
	margin: 1.5rem 0;
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
	padding: 0.55rem 0.8rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	background: #ffffff;
	color: #174ea6;
	text-decoration: none;
}

.event-page-link:hover {
	background: #f6f8fa;
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

@media (max-width: 48rem) {
	.site-header-inner,
	.site-nav {
		align-items: flex-start;
	}

	.site-nav {
		flex-direction: column;
	}

	.event-filter-form {
		grid-template-columns: 1fr;
	}

	.event-filter-actions {
		flex-direction: column;
		padding-top: 0;
	}
}
```

#### File: `Program.cs`

```csharp
using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Services registration

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString =
	builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException(
		"Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));  // comment for MacOS or Linux
/*options.UseSqlite(connectionString));		// uncomment for MacOS or Linux*/

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.User.RequireUniqueEmail = true;

	options.Password.RequiredLength = 8;
	options.Password.RequireDigit = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireUppercase = true;
	options.Password.RequireNonAlphanumeric = false;

	options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Account/Login";
});

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
app.UseAuthentication();
app.UseAuthorization();

// Routes/endpoints mapping

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();

	var services = scope.ServiceProvider;
	var context = services.GetRequiredService<ApplicationDbContext>();

	await DbInitializer.SeedAsync(context);
	await IdentitySeedData.SeedAsync(services, builder.Configuration);
}

app.Run();
```

Read the final startup file as a dependency-and-request-pipeline summary. The DbContext registration connects EF Core to SQL Server. Identity registration adds the user/role managers and account rules. The cookie configuration identifies the Login route. `UseAuthentication()` reconstructs the request user, and `UseAuthorization()` enforces endpoint access rules. Development seeding then uses those registered services to establish classroom roles/accounts while preserving the existing catalog data.

#### Final database/migration state

Create and apply Lesson 4 migration:

```text
dotnet ef migrations add AddIdentityAuthentication -- --environment Development
dotnet ef database update -- --environment Development
```

---

<br>

# What You Will Build

By the end of this lesson, CEMS will support:


<br id="what-you-built">

# 22. What You Built

The application now supports:

- participant account registration
- email as a unique login identity
- valid login through ASP.NET Core Identity
- generic invalid-login feedback
- safe return to an originally requested protected local page
- POST logout
- anti-forgery protection on state-changing account/session forms
- exact `Administrator` and `Organizer` roles
- Development-only accounts backed by user-secret configuration
- authentication-aware navigation
- a small `/Participant` route protected by `[Authorize]`
- server authorization that remains effective even when the UI hides a link
- project-owned CSS with no Bootstrap or other CSS framework
- no jQuery, jQuery validation, Bootstrap JavaScript, or other JavaScript framework/library
- unchanged Lesson 3 browse/search/filter/page-size/pagination/details behavior (including First/Previous/Next/Last, preserved page-size state, and the four-column form), for anonymous and authenticated visitors

The account requirement state is:

- `FR-ACC-001` — **COMPLETE**
- `FR-ACC-002` — **COMPLETE**
- `FR-ACC-003` — **COMPLETE**
- `FR-ACC-004` — **COMPLETE**
- `FR-ACC-005` — **COMPLETE**
- `FR-ACC-006` — **PARTIAL / RECURRING**
- `FR-ACC-007` — **COMPLETE**

`FR-ACC-006` remains partial because later Participant, Organizer, and Administrator product operations have not all been introduced and protected yet.

---

<br id="summary">

# 23. Summary

## What You Learned

Lesson 4 changed CEMS from an application that could only process requests from unknown visitors into one that can establish a server-recognized user identity and enforce protected access across later requests. In doing so, the lesson connected browser behavior, MVC request processing, ASP.NET Core Identity, dependency injection, middleware, EF Core, Razor, configuration, and the database into one authentication and authorization flow.

You should now be able to explain that:

- **authentication** establishes who the user is, while **authorization** decides whether that authenticated user may perform a particular operation
- authentication is security-sensitive infrastructure that should be delegated to a trusted, battle-tested framework such as ASP.NET Core Identity rather than implemented with custom password hashing, password comparison, cookie handling, or role storage
- an email address can serve as a changeable login identifier while Identity's internal user ID remains the stable account identifier used by application relationships
- request headers, cookies, tokens, form fields, query strings, and payloads remain untrusted client-supplied input until the appropriate server-side mechanism validates them
- a successful Login does not cause the browser to resend the password on later requests. Instead, the browser sends the authentication cookie, authentication middleware validates it and reconstructs `User`, and authorization can then evaluate the requested endpoint
- middleware order matters: routing identifies the endpoint, authentication reconstructs the current user, and authorization evaluates whether that user may reach the endpoint
- unique login identities must be enforced by the account system, and invalid Login feedback should remain generic so the application does not help an attacker enumerate registered accounts and reduce the search space
- a return destination is request state, not trusted account data. It can be carried through the Login form, but it must be validated as local before redirecting so Login cannot become an open redirect to an attacker-controlled site
- **anti-forgery protection** addresses CSRF by requiring a state-changing browser POST to contain a framework-issued token in addition to any authentication cookie. `SameSite` cookie policy can reduce CSRF exposure, but it is complementary browser behavior rather than a replacement for server-side anti-forgery validation
- state-changing operations such as Logout should use POST rather than GET. GET requests may be followed automatically by browsers, crawlers, link-preview generators, scanners, and other clients, so GET should not be used as a convenience command that changes authentication-session state
- `Participant` is the normal authenticated-user capability in CEMS rather than an Identity role, while `Organizer` and `Administrator` are additional role-based permissions whose exact names are centralized rather than duplicated as string literals throughout the application
- Development passwords and other secrets belong outside tracked source and committed configuration. The application declares the configuration values it needs, while the environment supplies sensitive values through an appropriate mechanism such as .NET Secret Manager for local Development
- authentication-aware Razor navigation is presentation logic that improves usability, while `[Authorize]` and other server-side authorization checks remain the security boundary even when a user bypasses the intended UI and requests a protected URL directly
- removing Bootstrap, jQuery, and client-side validation libraries is a controlled refactor: presentation dependencies can change while server-side validation, authentication, authorization, anti-forgery protection, database behavior, and established catalog behavior are preserved through regression testing.

A useful full-stack mental model to carry forward is:

```text
browser submits request
	↓
MVC model binding creates request/input state
	↓
ModelState + application/framework validation
	↓
Identity performs account or credential work
	↓
successful Login establishes authentication cookie
	↓
later request reaches routing
	↓
authentication middleware validates cookie and reconstructs User
	↓
authorization evaluates the requested endpoint
	↓
protected controller/action may run
	↓
Razor renders UI appropriate to the resulting server state
```

The central engineering principle is that **the server remains authoritative**. Browser behavior and Razor rendering can improve usability, but security and data integrity do not depend on the client following the intended interface. The server validates request data, authentication state, authorization, anti-forgery tokens, account rules, and database constraints before accepting protected behavior.

<br>

## Coming Next

In **Lesson 5 — Organizer Event Forms, Validation, and Ownership**, you will use the authenticated-user foundation from this lesson to build the first real Organizer product workflow.

The application will add server-rendered Create and Edit forms and a protected Organizer management page.

You will distinguish a purpose-specific **input model** from a persisted EF Core entity, use Data Annotations and cross-field validation, repopulate Category choices after an invalid POST, preserve valid submitted values when validation fails, and use POST/Redirect/GET after a successful mutation.

**Lesson 5 turns authentication and authorization from infrastructure into product behavior. CEMS already knows who the user is; next it will use that identity, role, and resource ownership to decide which Event-management operations the user may perform and which values the server must control.**

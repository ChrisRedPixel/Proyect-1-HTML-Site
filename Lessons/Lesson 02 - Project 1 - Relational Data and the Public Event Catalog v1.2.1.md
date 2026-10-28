# **Lesson 2 — Relational Data and the Public Event Catalog**

Revision: 2026.10.4

# Table of Contents

- [What You Will Learn](#what-you-will-learn)
- [Introduction](#introduction)
- [1. From Temporary Data to Persistence](#from-temporary-data-to-persistence)
  - [1.1 Why persistence matters](#why-persistence-matters)
  - [1.2 Relational data in this project](#relational-data-in-this-project)
- [2. Modeling CEMS Data](#modeling-cems-data)
  - [2.1 Add `EventStatus`](#add-eventstatus)
  - [2.2 Add `ApplicationUser`](#add-applicationuser)
  - [2.3 Add `Category`](#add-category)
  - [2.4 Add the first version of `Event`](#add-the-first-version-of-event)
- [3. Add EF Core and DBMS](#add-ef-core-and-dbms)
  - [3.1 Install the EF Core packages](#install-the-ef-core-packages)
  - [3.2 Database Management System (DBMS)](#database-management-system-dbms)
    - [3.2.1 Install SQL Server Express](#install-sql-server-express)
    - [3.2.2 Install SQLite](#install-sqlite)
  - [3.3 Create `ApplicationDbContext`](#create-applicationdbcontext)
  - [3.4 Configure `DefaultConnection`](#configure-defaultconnection)
    - [3.4.1 Connection Strings That Contain Sensitive Information](#connection-strings-that-contain-sensitive-information)
    - [3.4.2 Secret Manager](#secret-manager)
    - [3.4.3 System Environment Variables](#system-environment-variables)
    - [3.4.4 Configuration Precedence](#configuration-precedence)
  - [3.5 Register the `DbContext` in `Program.cs`](#register-the-dbcontext-in-programcs)
- [4. Create the Database with a Migration](#create-the-database-with-a-migration)
  - [4.1 Check the EF CLI](#check-the-ef-cli)
  - [4.2 Create `InitialCemsCatalog`](#create-initialcemscatalog)
  - [4.3 Inspect the migration](#inspect-the-migration)
  - [4.4 Apply the migration](#apply-the-migration)
  - [4.5 Inspect the database](#inspect-the-database)
- [5. Add Development Seed Data](#add-development-seed-data)
  - [5.1 Create the initializer](#create-the-initializer)
  - [5.2 Run the initializer only in Development](#run-the-initializer-only-in-development)
- [6. Query Published Upcoming Events](#query-published-upcoming-events)
  - [6.1 Replace the Temporary Event Value with a Database Query](#replace-the-temporary-event-value-with-a-database-query)
  - [6.2 Read the Composed Query from Top to Bottom](#read-the-composed-query-from-top-to-bottom)
    - [6.2.1 Read without change tracking](#read-without-change-tracking)
    - [6.2.2 Eagerly load the related Category](#eagerly-load-the-related-category)
    - [6.2.3 Apply filtering before materialization](#apply-filtering-before-materialization)
    - [6.2.4 Order the result deterministically](#order-the-result-deterministically)
  - [6.3 Cross the Execution and Materialization Boundary](#cross-the-execution-and-materialization-boundary)
  - [6.4 Constrained Queries and Bounded Operations](#constrained-queries-and-bounded-operations)
  - [6.5 Why `async` and `await` Are Useful Here](#why-async-and-await-are-useful-here)
- [7. Render Persisted Events](#render-persisted-events)
- [8. Add an Event Detail Page](#add-an-event-detail-page)
  - [8.1 Add the controller action](#add-the-controller-action)
  - [8.2 Create the Razor View](#create-the-razor-view)
  - [8.3 Configured capacity is not live availability](#configured-capacity-is-not-live-availability)
- [9. Link the Catalog to Event Details](#link-the-catalog-to-event-details)
- [10. Handle Missing Events](#handle-missing-events)
- [11. Guided Practice](#guided-practice)
  - [11.1 Follow the relationships on an event details request](#follow-the-relationships-on-an-event-details-request)
  - [11.2 Add one seed Event](#add-one-seed-event)
- [12. Check Your Understanding](#check-your-understanding)
- [13. Common Mistakes](#common-mistakes)
  - [13.1 `UseSqlServer` or `UseSqlite` is not recognized](#usesqlserver-or-usesqlite-is-not-recognized)
  - [13.2 `dotnet ef` is not recognized](#dotnet-ef-is-not-recognized)
  - [13.3 The SQL Server LocalDB or SQLite connection fails](#the-sql-server-localdb-or-sqlite-connection-fails)
  - [13.4 An EF command cannot find `DefaultConnection`](#an-ef-command-cannot-find-defaultconnection)
  - [13.5 EF Core SQL does not appear in the application output](#ef-core-sql-does-not-appear-in-the-application-output)
  - [13.6 The migration is empty](#the-migration-is-empty)
  - [13.7 Seed insertion fails](#seed-insertion-fails)
  - [13.8 `/Events` shows Draft or Cancelled Events](#events-shows-draft-or-cancelled-events)
  - [13.9 Category data fails in the catalog](#category-data-fails-in-the-catalog)
  - [13.10 Organizer data fails on the Event page](#organizer-data-fails-on-the-event-page)
  - [13.11 The Razor model does not match the controller](#the-razor-model-does-not-match-the-controller)
  - [13.12 Your new seed Event does not appear after editing `DbInitializer`](#your-new-seed-event-does-not-appear-after-editing-dbinitializer)
- [What You Will Build / What You Built](#what-you-will-build-what-you-built)
- [14. Summary](#summary)
- [Appendix — Production Hardening: Constraining and Bounding Work](#appendix-production-hardening-constraining-and-bounding-work)

<br>

<a id="what-you-will-learn"></a>
# What You Will Learn

After completing the lesson, you should be able to:

1. explain what navigation properties are
2. explain what EF Core and a `DbContext` contribute to an MVC application
3. explain what migrations are and how to create and apply them
4. explain the purpose of Development seed data
5. trace database-provider registration and `DbContext` dependency injection at the composition root
6. distinguish between an EF Core `DbContext`, a migration, and the actual database schema
7. explain why filtering and ordering should occur in the database query rather than in Razor
8. trace an EF Core query from controller-side LINQ through executed SQL to materialized C# objects
9. explain tracking, eager loading, query execution, and materialization
10. distinguish expected failures (4xx) from unexpected failures (5xx)
11. discuss production hardening concepts and tools

<br>

<a id="introduction"></a>
# Introduction

In the previous lesson, you built an ASP.NET Core MVC request/response cycle. A browser could request `/Events`, `EventsController.Index()` supplied a temporary string, and Razor rendered HTML.

In this lesson, you will replace that temporary string with relational data stored in a **database management system (DBMS)** and accessed through **Entity Framework Core (EF Core)**.

This lesson adds a **persistence** inside that request:

```text
HTTP request
		↓
Controller
		↓
Application / domain logic
		↓
Persistence
———————————————————————————————————————————————————
	EF Core translates LINQ expression to SQL
			↓
	DBMS executes SQL
			↓
	Result set
			↓
	EF Core materializes C# objects from result set
———————————————————————————————————————————————————
		↓
Controller passes model/data to Razor
		↓
Razor renders HTML
		↓
HTTP response
```

---

<br>

<a id="from-temporary-data-to-persistence"></a>
# 1. From Temporary Data to Persistence

Your current `/Events` flow looks like this:

```text
Browser requests /Events
		↓
conventional routing
		↓
EventsController.Index()
		↓
————————————————————————————————
creates temporary string
		↓
————————————————————————————————
passes temporary string to Razor
		↓
Views/Events/Index.cshtml
		↓
HTML response
```

The MVC part is already real. What changes in this lesson is the source of the Event data.

After persistence is added, the browse flow will become:

```text
Browser requests /Events
		↓
conventional routing
		↓
EventsController.Index()
		↓
————————————————————————————————
ApplicationDbContext assembles query
		↓
sends the query to DBMS
		↓
converts result set into C# objects
		↓
————————————————————————————————
passes persisted Event collection to Razor
		↓
Views/Events/Index.cshtml
		↓
HTML response
```

<br>

<a id="why-persistence-matters"></a>
## 1.1 Why persistence matters

Relational persistence allows data to be shared and secured.

Some rules can also be protected at the database level:

- every Event must reference an existing Category
- every Event must reference an organizer
- start time must be before end time
- capacity must be greater than zero

Later lessons will add form validation before invalid data reaches the database. Database constraints and application validation work together.

That distinction matters. Application validation can give a user a helpful message before an invalid write is attempted. Database constraints protect persisted data even when some other path reaches the database (for example, a bug, a script, an administrative tool, or future application code). The database is therefore not merely storage; it is also one of the places where important data invariants can be enforced.

<br>

<a id="relational-data-in-this-project"></a>
## 1.2 Relational data in this project

CEMS needs to store related information rather than one large disconnected record.

- Category 1:N Events = Each event has one category : each category may have many events
- Organizer 1:N Events = Each event has one organizer : each organizer may have many events

```text
Category
 	├─ Id (PK)
 	└─ Name

ApplicationUser
 	├─ Id (PK)
 	└─ DisplayName

Event
 	├─ CategoryId (FK)
 	└─ OrganizerId (FK)
```

A **primary key (PK)** uniquely identifies one record. A **foreign key (FK)** references a related record key.

```csharp
public class Event
{
	...

	public int CategoryId { get; set; }
	public Category Category { get; set; } = null!;

	public string OrganizerId { get; set; }
	public ApplicationUser Organizer { get; set; } = null!;
}
```

A **navigation property** gives C# code an object-oriented way to work with that relationship.

For example, an Event can store:

```csharp
public int CategoryId { get; set; }
```

and also expose:

```csharp
public Category Category { get; set; } = null!;
```

`CategoryId` is the stored relationship value. `Category` is the related C# object that EF Core can load when needed.

It helps to separate the **relational representation** from the **object-oriented representation**.

The navigation properties are not extra columns containing whole C# objects. The database still stores single value attributes and keys. EF Core uses the relationship metadata to connect those rows back into related C# objects when the application requests that related data.

Event<br>
 	├─ CategoryId (FK)<br>
 	├─ <s>Category</s><br>
 	├─ OrganizerId (FK)<br>
 	└─ <s>ApplicationUser</s><br>

The organizer relationship follows the same general idea.

---

<br>

<a id="modeling-cems-data"></a>
# 2. Modeling CEMS Data

Before EF Core can persist CEMS data, the application needs a model that expresses the concepts it cares about. In this lesson those concepts are an Event, its Category, its lifecycle status, and the user reference that owns or organizes it.

An **entity class** is a C# type that EF Core can map to relational data. The class expresses the application's object-oriented view of the concept; EF Core later builds mapping metadata that describes how those properties and relationships correspond to tables, columns, keys, and constraints.

Do not assume that every C# property means “create a column exactly like this.” Some properties are scalar values that map naturally to columns, while navigation properties represent relationships among entities. The purpose of the model is to let C# code work with meaningful application concepts while EF Core handles the relational mapping.

<br>

<a id="add-eventstatus"></a>
## 2.1 Add `EventStatus`

CEMS currently recognizes exactly three Event states:

- `Draft`
- `Published`
- `Cancelled`

Open a new file:

**File: `Models/EventStatus.cs`**

```csharp
namespace CampusEvents.Models;

public enum EventStatus
{
	Draft,
	Published,
	Cancelled
}
```

An enum makes those states explicit in C#. `EventStatus.Published` communicates much more clearly than a numeric value such as `1`.

<br>

<a id="add-applicationuser"></a>
## 2.2 Add `ApplicationUser`

Now create the small user model that provides Event ownership.

**File: `Models/ApplicationUser.cs`**

```csharp
namespace CampusEvents.Models;

public class ApplicationUser
{
	public string Id { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
}
```

The `Id` uses a `string`, which matches the default key shape used by ASP.NET Core Identity. At this point, however, this class is only a persisted ownership reference. You are **not** building registration, login, passwords, roles, or authorization in this lesson.

CEMS needs ownership data before it needs a complete authentication workflow. Keeping the same user identity shape allows later lessons to add authentication capabilities without changing the meaning of `Event.OrganizerId`.

`DisplayName` gives CEMS a human-readable organizer name that can later appear associated with an Event.

`OrganizedEvents` is the collection navigation property for the one-to-many relationship between an `ApplicationUser` and the Events they organize. Initializing it with an empty `List<Event>` keeps the collection ready to use and avoids null checks when Events are added or loaded through Entity Framework Core.

**NOTE**: The editor will mark `Event` as undefined for `ApplicationUser` until you create the `Event` model (below).

<br>

<a id="add-category"></a>
## 2.3 Add `Category`

Create:

**File: `Models/Category.cs`**

```csharp
namespace CampusEvents.Models;

public class Category
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public ICollection<Event> Events { get; set; } = new List<Event>();
}
```

`Id` is the Category primary key. `Name` is the label users will see. `Events` is the one-to-many navigation that represents all Events assigned to the Category.

The collection is ***not*** one giant column in the Categories table. It represents a relationship that EF Core understands.

The one-to-many relationship will eventually have this shape:

```text
Categories
----------
Id = 1, Name = Academic

Events
------
Id = 10, CategoryId = 1, ...
Id = 11, CategoryId = 1, ...
```

The database expresses the relationship by storing `CategoryId` on each Event row. The `Category.Events` collection is the C# view of that same relationship from the Category side.

**NOTE**: The editor will mark `Event` as undefined for `Category` until you create the `Event` model (below).

<br>

<a id="add-the-first-version-of-event"></a>
## 2.4 Add the first version of `Event`

Create:

**File: `Models/Event.cs`**

```csharp
namespace CampusEvents.Models;

public class Event
{
	public int Id { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string Location { get; set; } = string.Empty;

	public DateTime StartDateTime { get; set; }

	public DateTime EndDateTime { get; set; }

	public int Capacity { get; set; }

	public EventStatus Status { get; set; }

	public DateTime CreatedAt { get; set; }

	public int CategoryId { get; set; }

	public Category Category { get; set; } = null!;

	public string OrganizerId { get; set; } = string.Empty;

	public ApplicationUser Organizer { get; set; } = null!;
}
```

This model contains the Event information needed at this stage: identity, title, description, location, start and end date/time, capacity, status, creation time, category relationship, category, organizer relationship, and organizer.

There is no Registration collection. Registration belongs to a later stage of the project.

Build the project before adding EF Core:

```cmd
dotnet build
```

At this checkpoint, the classes should compile. They are still ordinary C# classes; no database persistence exists yet.

---

<br>

<a id="add-ef-core-and-dbms"></a>
# 3. Add EF Core and DBMS

The entity classes describe CEMS concepts, but ordinary C# classes do not know how to create SQL tables, translate LINQ into SQL, send commands to DBMS, or rebuild related objects from returned rows. **Entity Framework Core (EF Core)** provides that persistence layer.

A useful mental model is:

```text
CEMS entity objects and LINQ
		↓
ApplicationDbContext
		↓
EF Core model + query/change services
		↓
DBMS provider (SQL Server / SQLite / MySQL)
		↓
Database Management System (DBMS)
```

Each part has a distinct role:

- the **entity classes** describe CEMS data in C#
- `ApplicationDbContext` is CEMS's EF Core entry point and contains the model configuration
- **EF Core** uses that model to translate supported LINQ expressions into SQL
- the **DBMS provider** knows how EF Core should communicate with DBMS
- **DBMS** is the relational database engine that actually stores the records

This separation explains why several setup steps are required. Installing EF Core does not create a database. Defining entity classes does not create tables. Registering `ApplicationDbContext` does not apply a schema. Those pieces cooperate, but they are not the same thing.

<br>

<a id="install-the-ef-core-packages"></a>
## 3.1 Install the EF Core packages

From the `CampusEvents` project directory, install the SQL Server provider:

```cmd
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

or if using MacOS or Linux, install the SQLite provider:

```cmd
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

Add EF Core's design-time package for migrations:

```cmd
dotnet add package Microsoft.EntityFrameworkCore.Design
```

The two packages support different parts of the workflow. `Microsoft.EntityFrameworkCore.Sqlxxx` supplies the DBMS provider used when the application talks to DBMS. `Microsoft.EntityFrameworkCore.Design` supplies design-time support needed by tooling such as migrations.

Use package versions compatible with the .NET/EF Core version already established. Do not change major framework versions.

Build it:

```cmd
dotnet build
```

<br>

<a id="database-management-system-dbms"></a>
## 3.2 Database Management System (DBMS)

Choose the DBMS of your preference. This lesson uses "SQL Server LocalDB" for Windows and "SQLite" for MacOS and Linux (although it also works with Windows). You may use any ASP.NET supported DBMS provider / DBMS.

<br>

<a id="install-sql-server-express"></a>
### 3.2.1 Install SQL Server Express

This lesson uses **SQL Server LocalDB** on Windows. On a development machine with LocalDB installed, you can inspect the default instance with:

```cmd
sqllocaldb info MSSQLLocalDB
```

If Windows reports that sqllocaldb is not recognized, install LocalDB before continuing.

Download [Microsoft SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads).

Microsoft's SQL Server Express installer provides LocalDB as a selectable feature. During setup, make sure LocalDB is selected as an installed feature.

After installation, restart your computer and check the `sqllocaldb` command again. Do not continue until the LocalDB installation has completed successfully.

<br>

<a id="install-sqlite"></a>
### 3.2.2 Install SQLite

This lesson uses **SQLite** on MacOS and Linux. On a development machine with SQLite installed, you can check the installed version with:

```cmd
sqlite3 --version
```

If the terminal reports that `sqlite3` is not recognized or cannot be found, install SQLite before continuing.

On **MacOS**, SQLite is typically already available. If it is not installed, or you want to install it using Homebrew, run:

```cmd
brew install sqlite
```

If Homebrew is not installed, you may install SQLite using the binaries available from the [SQLite Download Page](https://www.sqlite.org/download.html).

On **Debian or Ubuntu Linux**, install SQLite with:

```cmd
sudo apt update
sudo apt install sqlite3
```

Other Linux distributions may provide SQLite through their respective package managers.

Do not continue until the SQLite installation has completed successfully and the `sqlite3` command is available.

<br>

<a id="create-applicationdbcontext"></a>
## 3.3 Create `ApplicationDbContext`

`ApplicationDbContext` is where CEMS tells EF Core which entity sets belong to the current persistence model and where explicit mapping rules live. Think of the context as the application's EF Core working boundary.

The context does not contain all database records inside itself. Its `DbSet<T>` properties are entry points for querying and saving entities (not actual data collections). The context also supplies EF Core with relationship and constraint configuration that cannot be inferred or that CEMS wants to make explicit.

Create the `Data` folder, then create:

**File: `Data/ApplicationDbContext.cs`**

```csharp
using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public class ApplicationDbContext : DbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)	: base(options)
	{
	}

	public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

	public DbSet<Category> Categories => Set<Category>();

	public DbSet<Event> Events => Set<Event>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<ApplicationUser>(entity =>
		{
			entity.ToTable("AspNetUsers");

			entity.HasKey(user => user.Id);

			entity.Property(user => user.Id)
				.HasMaxLength(450)
				.ValueGeneratedNever();

			entity.Property(user => user.DisplayName)
				.IsRequired();
		});

		modelBuilder.Entity<Category>(entity =>
		{
			entity.Property(category => category.Name)
				.IsRequired();
		});

		modelBuilder.Entity<Event>(entity =>
		{
			entity.Property(eventItem => eventItem.Title)
				.IsRequired();

			entity.Property(eventItem => eventItem.Description)
				.IsRequired();

			entity.Property(eventItem => eventItem.Location)
				.IsRequired();

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

There are several important pieces here. Read this class as **model configuration**, not as a hand-written SQL schema. EF Core combines conventions with the explicit rules in `OnModelCreating(...)` to build its model of CEMS persistence.

Some things are conventional, for example, an `Id` property is naturally treated as a key in common EF Core models. Other decisions are explicit here because CEMS wants a particular database shape or behavior, such as the `AspNetUsers` table name, the organizer string-key configuration, restricted deletes, and the Event check constraints.

### `DbSet<T>`

These properties give EF Core query/save entry points for the current entities:

```csharp
public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
public DbSet<Category> Categories => Set<Category>();
public DbSet<Event> Events => Set<Event>();
```

Later, the controller can begin an Event query with:

```csharp
_context.Users
_context.Categories
_context.Events
```

That expression does **not** mean “all Events have already been loaded into memory.” It gives EF Core a starting point from which a database query can be composed. Later calls such as `Where(...)`, `OrderBy(...)`, and `Include(...)` can add to that query before execution.

The same `DbSet<T>` can also participate in save operations. In other words, `DbSet<T>` is a persistence entry point for an entity type, not a replacement for a `List<T>` stored inside the application.

### Event → Category

This relationship:

```csharp
entity.HasOne(eventItem => eventItem.Category)
	.WithMany(category => category.Events)
	.HasForeignKey(eventItem => eventItem.CategoryId)
	.OnDelete(DeleteBehavior.Restrict);
```

means many Events can refer to one Category through `CategoryId`.

### Event → Organizer

This relationship:

```csharp
entity.HasOne(eventItem => eventItem.Organizer)
	.WithMany(user => user.OrganizedEvents)
	.HasForeignKey(eventItem => eventItem.OrganizerId)
	.OnDelete(DeleteBehavior.Restrict);
```

means many Events can be organized by one user reference through `OrganizerId`.

The navigation pairs make the relationships convenient to traverse in C#. The foreign-key columns are what preserve the actual references in relational storage.

A **foreign key** protects referential integrity at the database level. Even if normal application code supplies valid IDs, the database constraint prevents an `Event` from being stored with a `CategoryId` or `OrganizerId` that does not correspond to an existing related row. This protects the data from bugs, scripts, manual database changes, or other code paths that might bypass normal application logic.

`DeleteBehavior.Restrict` prevents a referenced category or organizer row from being silently cascade-deleted along with Events. A **cascade delete** means that deleting one database row automatically causes related dependent rows to be deleted as well. Using `Restrict` requires those relationships to be handled explicitly instead of allowing a delete to remove related data automatically.

### Database check constraints

The Event table includes:

```text
[StartDateTime] < [EndDateTime]
```

and:

```text
[Capacity] > 0
```

These constraints support the Event timing and positive-capacity business rules in persisted data.

The user table is named `AspNetUsers`. The current class is not yet a complete authenticated Identity user, but this table name and string key give later account work a compatible starting point.

<br>

<a id="configure-defaultconnection"></a>
## 3.4 Configure `DefaultConnection`

Open:

**File: `appsettings.Development.json`**

Use the Development configuration below:

```json
{
	"Logging": {
		"LogLevel": {
			"Default": "Information",
			"Microsoft.AspNetCore": "Warning",
			"Microsoft.EntityFrameworkCore.Database.Command": "Information"
		}
	},
	"ConnectionStrings": {
		"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=CampusEventsDb;Trusted_Connection=True;MultipleActiveResultSets=true"
	}
}
```

The explicit `Microsoft.EntityFrameworkCore.Database.Command` setting makes the SQL commands executed by EF Core visible at `Information` level in the application output. `Default` is already `Information` here, so the category-specific setting is intentionally redundant: it documents the observability this lesson depends on and keeps the exercise working if broader logging levels are changed later.

This connection string targets the local database `CampusEventsDb` through Windows integrated authentication. It contains no database password.

The connection string tells the configured provider **which database instance and database name to use**.

Production credentials, secret keys, passwords, encryption keys, API keys, and tokens **must not be committed to source control (e.g., Git/GitHub)**.

Because this LocalDB connection string does not contain sensitive credentials, it is acceptable to keep it in `appsettings.Development.json`, but it is a bad practice that may create a bad habit and, therefore, should be discouraged.

<br>

<a id="connection-strings-that-contain-sensitive-information"></a>
### 3.4.1 Connection Strings That Contain Sensitive Information

Some connection strings contain sensitive information and should not be stored in `appsettings.json`, `appsettings.Development.json`, or any other file committed to the repository.

For example, an encrypted SQLite database could use a connection string containing a password:

```text
Data Source=campus-events.db;Password=MySecretDatabasePassword
```

In this case, the password makes the entire connection string sensitive.

> **Important:** The `Password` option in `Microsoft.Data.Sqlite` is an encryption key and only has an effect when the underlying native SQLite library supports encryption.

For local development, ASP.NET Core provides the **Secret Manager** tool through the `dotnet user-secrets` command. User Secrets stores development configuration outside the project directory so that it is not accidentally committed to source control (e.g., Git/GitHub).

<br>

<a id="secret-manager"></a>
### 3.4.2 Secret Manager

From the directory containing the project's `.csproj` file, initialize User Secrets:

```cmd
dotnet user-secrets init
```

Then store the connection string under the same `ConnectionStrings:DefaultConnection` configuration key:

```cmd
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=CampusEventsDb;Trusted_Connection=True;MultipleActiveResultSets=true"
```

or if using SQLite (for Mac/Linux users)

```cmd
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=campus-events.db;Password=MySecretDatabasePassword"
```

You can verify that the configuration value was registered:

```cmd
dotnet user-secrets list
```

or by opening

```cmd
%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
C:\Users\<YourUserName>\AppData\Roaming\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
```

<br>

<a id="system-environment-variables"></a>
### 3.4.3 System Environment Variables

As a more convenient, yet less secure, alternative to secret managers you may store sensitive variables as system environment variables.

> **Note:** Some secret managers or system environment variable tools use double underscore (__) instead of the colon (:) as a configuration key path separator. For example:

```cmd
set "ConnectionStrings__DefaultConnection=Server=(localdb)\MSSQLLocalDB;Database=CampusEventsDb;Trusted_Connection=True;MultipleActiveResultSets=true"
```

or if using SQLite (for Mac/Linux users)

```bash
export ConnectionStrings__DefaultConnection="Data Source=campus-events.db;Password=MySecretDatabasePassword"
```

If using system environment variables instead of a secret manager you may see the secrets (and other variables) by typing on a terminal:

```cmd
set
```

or if using SQLite (for Mac/Linux users)

```bash
env or printenv
```

The application can continue retrieving the connection string normally:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
```

<br>

<a id="configuration-precedence"></a>
### 3.4.4 Configuration Precedence

The effective precedence from lowest-to-highest is:

1. Fallback configuration
2. appsettings.json
3. appsettings.{Environment}.json
4. User Secrets (when running in Development)
5. Environment variables
6. Command-line arguments

So if we set the connection string in all these places:

**Fallback configuration**

```csharp
var connectionString = 
	builder.Configuration.GetConnectionString("DefaultConnection") ?? "F";
```

**appsettings.json**

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "A"
	}
}
```

**appsettings.Development.json**

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "B"
	}
}
```

**Secret Manager (secrets.json)**

```json
{
	"ConnectionStrings:DefaultConnection": "C"
}
```

**System Environment Variable**

```cmd
set "ConnectionStrings__DefaultConnection=D"
```

**Command-line arguments**

```cmd
dotnet run --ConnectionStrings:DefaultConnection="E"
```

then the connection string used will be "E". Not because it was set last, but because it was specified as a command-line argument. If we change the order of the different places we set the connection string, the value used will still be "E".

Although it is relatively safe because it contains non-sensitive information, to avoid creating bad habits, do not put the connection string in the configuration file, not even during development:

```json
{
	"Logging": {
		"LogLevel": {
			"Default": "Information",
			"Microsoft.AspNetCore": "Warning",
			"Microsoft.EntityFrameworkCore.Database.Command": "Information"
		}
	}
}
```

The .NET User Secrets tool is intended for **development only**. They keep secrets outside the project directory, but the stored values are not encrypted and should not be treated as a production secret store.

For deployed applications, use the secret-management mechanism provided by the hosting environment, such as **Azure Key Vault**, a cloud secret manager, or another controlled production secret store.

<br>

<a id="register-the-dbcontext-in-programcs"></a>
## 3.5 Register the `DbContext` in `Program.cs`

The entity model and connection string now exist, but application code still needs a standard way to obtain a correctly configured `ApplicationDbContext`. ASP.NET Core dependency injection provides that connection between configuration and consuming code.

The registration will effectively establish this path:

```text
code asks for `ApplicationDbContext`
		↓
ASP.NET Core dependency injection
		↓
uses `DefaultConnection` configuration value as `connectionString`
		↓
configures EF Core with `UseSqlServer(connectionString)` / `UseSqlite(connectionString)`
		↓
constructs `ApplicationDbContext`
```

This is why controllers later request `ApplicationDbContext` in a constructor instead of constructing one manually.

Open:

**File: `Program.cs`**

At this point, before seed logic is added, use:

```csharp
using CampusEvents.Data;
using Microsoft.EntityFrameworkCore;

// Services registration

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException(
		"Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));  // comment for MacOS or Linux
/*options.UseSqlite(connectionString));   // uncomment for MacOS or Linux*/

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

app.MapStaticAssets();
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}")
	.WithStaticAssets();

app.Run();
```

The existing MVC registration remains:

```csharp
builder.Services.AddControllersWithViews();
```

The new registration is:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));  // comment for MacOS or Linux
/*options.UseSqlite(connectionString));   // uncomment for MacOS or Linux*/
```

This tells ASP.NET Core how to construct `ApplicationDbContext` when application code requests it through dependency injection.

It also keeps configuration out of the controller. `EventsController` should know that it needs CEMS data; it should not need to know how to parse a connection string or configure the DBMS provider. That separation keeps the controller focused on request behavior.

The conventional route remains unchanged:

```text
{controller=Home}/{action=Index}/{id?}
```

Build it:

```cmd
dotnet build
```

Do not create a migration until this model and configuration compile.

---

<br>

<a id="create-the-database-with-a-migration"></a>
# 4. Create the Database with a Migration

At this point, CEMS has C# entity classes and EF Core model configuration, but that does **not** mean DBMS already contains matching tables. The application model and the physical database schema are separate artifacts.

EF Core migrations provide a versioned bridge between them.

Keep these three states distinct:

```text
1. EF Core model
	 C# entities + ApplicationDbContext configuration

2. Migration
	 versioned instructions describing a schema change

3. Database schema
	 the tables, columns, keys, and constraints that actually exist in DBMS
```

A migration records how EF Core believes the relational schema should change to match the model at that point in the project. Creating a migration generates project files that describe the schema operations. Applying the migration executes the pending schema changes against the configured database.

The workflow is therefore:

```text
change/build EF Core model
		↓
dotnet ef migrations add ...
		↓
inspect generated migration
		↓
dotnet ef database update ...
		↓
DBMS schema changes
```

This distinction becomes important in later lessons. Editing an entity class does not directly alter DBMS, and generating a migration is not the same action as applying that migration to a database.

<br>

<a id="check-the-ef-cli"></a>
## 4.1 Check the EF CLI

```cmd
dotnet ef --version
```

The EF CLI major version should match the EF Core package major version used by the project. If the `ef` tool is not recognized you need to install it first. For a global installation or update, from a terminal:

```cmd
dotnet tool install --global dotnet-ef
```
or

```cmd
dotnet tool update --global dotnet-ef
```

<br>

<a id="create-initialcemscatalog"></a>
## 4.2 Create `InitialCemsCatalog`

Run:

```cmd
dotnet ef migrations add InitialCemsCatalog -- --environment Development
```

The arguments after `--` are forwarded to the ASP.NET Core startup application. That causes Development configuration to load while EF Core creates the design-time `ApplicationDbContext`.

EF Core creates a `Migrations` folder whose generated migration files include the logical name `InitialCemsCatalog`.

Treat generated migrations as schema-change history, not as disposable output. The migration should correspond to the model change you intended. That is why the next step is inspection rather than immediately updating the database.

<br>

<a id="inspect-the-migration"></a>
## 4.3 Inspect the migration

Open the generated migration. Look for evidence of the model you just created:

- `AspNetUsers`
- `Categories`
- `Events`
- primary keys
- `Events.CategoryId`
- `Events.OrganizerId`
- foreign keys to Categories and `AspNetUsers`
- `CK_Events_StartBeforeEnd`
- `CK_Events_CapacityPositive`

A portion of the generated Events-table code should express ideas similar to this:

```csharp
migrationBuilder.CreateTable(
	name: "Events",
	columns: table => new
	{
		Id = table.Column<int>(nullable: false),
		Title = table.Column<string>(nullable: false),
		Description = table.Column<string>(nullable: false),
		Location = table.Column<string>(nullable: false),
		StartDateTime = table.Column<DateTime>(nullable: false),
		EndDateTime = table.Column<DateTime>(nullable: false),
		Capacity = table.Column<int>(nullable: false),
		Status = table.Column<int>(nullable: false),
		CreatedAt = table.Column<DateTime>(nullable: false),
		CategoryId = table.Column<int>(nullable: false),
		OrganizerId = table.Column<string>(nullable: false)
	},
	constraints: table =>
	{
		table.PrimaryKey("PK_Events", eventItem => eventItem.Id);
		table.CheckConstraint(
			"CK_Events_CapacityPositive",
			"[Capacity] > 0");
		table.CheckConstraint(
			"CK_Events_StartBeforeEnd",
			"[StartDateTime] < [EndDateTime]");
	});
```

Exact DBMS annotations and generated type details may vary with the EF Core version used. Inspect the generated migration rather than replacing it with this abbreviated teaching fragment.

When you inspect the migration, translate it back into the concepts you modeled:

```text
entity identity					→ primary key
Event.CategoryId			 	→ foreign key
Event.OrganizerId				→ foreign key
start-before-end rule		→ check constraint
positive-capacity rule 	→ check constraint
```

The goal is not to memorize generated migration syntax. The goal is to verify that the database change EF Core intends to make matches the CEMS model you intended to build.

<br>

<a id="apply-the-migration"></a>
## 4.4 Apply the migration

Creating `InitialCemsCatalog` changed the project by adding migration files. The database itself still needs the schema update. `database update` is the step that applies the pending migration to `CampusEventsDb`.

Create the LocalDB schema:

```cmd
dotnet ef database drop --force -- --environment Development
dotnet ef database update -- --environment Development
```

<br>

<a id="inspect-the-database"></a>
## 4.5 Inspect the database

Now inspect `CampusEventsDb` with Visual Studio SQL Server Object Explorer or some other SQL Server inspection tool. The VS Code equivalent is:

**SQL Server Extension**<br>
**Publisher**: Microsoft<br>
**Extension ID**: ms-mssql.mssql<br>

It includes an actual Object Explorer where you can inspect servers, databases, tables, views, stored procedures, functions, and other database objects. It also supports table/schema designers, editing table data, query execution, execution plans, schema compare, backup/restore, and database object search.

Install it from a terminal:

```cmd
code --install-extension ms-mssql.mssql
```

or install it from the `Extensions (CTRL+SHIFT+X)` view in VS Code.

After installation, you'll see the MSSQL icon in the VS Code Activity Bar. From there you can add a SQL Server connection and expand it much like:

```text
SQL Server
└── localhost
	└── MyDatabase
		├── Tables
		│	 ├── dbo.Users
		│	 └── dbo.Orders
		├── Views
		├── Stored Procedures
		├── Functions
		└── Security
```

At this moment, the schema exists but the CEMS tables do not yet contain the Event records.

---

<br>

<a id="add-development-seed-data"></a>
# 5. Add Development Seed Data

A database-backed catalog is difficult to reason about or verify with an empty database. We will therefore add a small **Development‑only dataset** with deliberately different Event states and dates.

The purpose is not merely to make the page look populated. Good seed data should let you test competing cases. If every Event were Published and upcoming, a page that accidentally rendered every record could appear correct. By storing Published, Draft, Cancelled, upcoming, and past records together, the database gives you evidence that the public query is enforcing the intended rules.

Keep the scope of this initializer clear:

```text
Development-only data ≠ production data provisioning
```

The initializer is intentionally simple because its job is to create a predictable local state for this project stage.

The first seed set includes:

- three Categories
- one organizer reference
- two Published upcoming Events
- one Draft upcoming Event
- one Cancelled upcoming Event
- one Published past Event

This lets you prove that the public query is selecting the correct rows instead of simply rendering every Event.

<br>

<a id="create-the-initializer"></a>
## 5.1 Create the initializer

Create:

**File: `Data/DbInitializer.cs`**

```csharp
using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public static class DbInitializer
{
	private const string OrganizerId = "seed-organizer-001";

	public static async Task SeedAsync(ApplicationDbContext context)
	{
		if (await context.Events.AnyAsync())
		{
			return;
		}

		var organizer = new ApplicationUser
		{
			Id = OrganizerId,
			DisplayName = "Campus Events Office"
		};

		var academic = new Category
		{
			Name = "Academic"
		};

		var social = new Category
		{
			Name = "Social"
		};

		var career = new Category
		{
			Name = "Career"
		};

		context.Users.Add(organizer);
		context.Categories.AddRange(academic, social, career);

		await context.SaveChangesAsync();

		var createdAt = DateTime.Now;

		var libraryStart = DateTime.Today.AddDays(30).AddHours(18);
		var careerStart = DateTime.Today.AddDays(50).AddHours(12);
		var draftStart = DateTime.Today.AddDays(70).AddHours(15);
		var cancelledStart = DateTime.Today.AddDays(90).AddHours(20);
		var pastStart = DateTime.Today.AddDays(-10).AddHours(13);

		var events = new[]
		{
			new Event
			{
				Title = "Library Research Night",
				Description = "An evening workshop on research databases and source evaluation.",
				Location = "Main Library, Room 204",
				StartDateTime = libraryStart,
				EndDateTime = libraryStart.AddHours(2),
				Capacity = 60,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = academic.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Career Networking Lunch",
				Description = "Meet local employers and practice short professional introductions.",
				Location = "Student Center Ballroom",
				StartDateTime = careerStart,
				EndDateTime = careerStart.AddHours(2),
				Capacity = 120,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = career.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Welcome Mixer Planning Draft",
				Description = "A draft event used to demonstrate that Draft Events are not public.",
				Location = "Student Center Patio",
				StartDateTime = draftStart,
				EndDateTime = draftStart.AddHours(2),
				Capacity = 80,
				Status = EventStatus.Draft,
				CreatedAt = createdAt,
				CategoryId = social.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Outdoor Movie Night",
				Description = "A cancelled event retained in the database for lifecycle examples.",
				Location = "Campus Green",
				StartDateTime = cancelledStart,
				EndDateTime = cancelledStart.AddHours(3),
				Capacity = 200,
				Status = EventStatus.Cancelled,
				CreatedAt = createdAt,
				CategoryId = social.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Alumni Panel Archive",
				Description = "A past Published event used to demonstrate upcoming-event filtering.",
				Location = "Business Building Auditorium",
				StartDateTime = pastStart,
				EndDateTime = pastStart.AddHours(2),
				Capacity = 100,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = career.Id,
				OrganizerId = organizer.Id
			}
		};

		context.Events.AddRange(events);

		await context.SaveChangesAsync();
	}
}
```

The initializer begins with:

```csharp
if (await context.Events.AnyAsync())
{
	return;
}
```

That prevents the same dataset from being inserted every time the application starts.

This guard is intentionally coarse: once any Event exists, the initializer assumes the Event dataset has already been established. It is enough for the lesson's controlled Development workflow, but it does not compare and synchronize individual seed records. You will see the consequence later when changing the initializer requires rebuilding the Development database.

It saves the organizer and Categories before the Events because the integer Category IDs are generated when those Category rows are persisted.

**NOTE:** The Event dates are relative to the day the seed code runs. So, we shall expect some events to become unavailable as days pass by in the course.

<br>

<a id="run-the-initializer-only-in-development"></a>
## 5.2 Run the initializer only in Development

Now evolve:

**File: `Program.cs`**

to its Lesson 2 startup form:

```csharp
using CampusEvents.Data;
using Microsoft.EntityFrameworkCore;

// Services registration

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException(
		"Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));  // comment for MacOS or Linux
/*options.UseSqlite(connectionString));   // uncomment for MacOS or Linux*/

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

app.MapStaticAssets();
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}")
	.WithStaticAssets();

if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();
	var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	await DbInitializer.SeedAsync(context);
}

app.Run();
```

The new Development block creates a service scope, resolves `ApplicationDbContext`, and calls:

```csharp
await DbInitializer.SeedAsync(context);
```

This is another example of dependency injection: the controller is not the only code that can receive registered services.

Build and run it:

```cmd
dotnet build
dotnet run
```

On the first Development run against the empty schema, inspect the database and confirm:

- `AspNetUsers` contains `seed-organizer-001`
- Categories contains Academic, Social, and Career
- Events contains five rows

At this point, the database contains Event data, but `/Events` still uses the temporary Lesson 1 string. Persistence has been created first; the web request will use it next.

---

<br>

<a id="query-published-upcoming-events"></a>
# 6. Query Published Upcoming Events

The database now contains multiple kinds of Event rows, but the public `/Events` endpoint must not simply retrieve all of them. The product rule for normal public browsing is narrower: only **Published upcoming Events** belong in this response.

That rule should be enforced while selecting data, not after broader data has already been loaded into the application.

```text
all stored Events
		↓
database query applies filtering
		↓
Published + upcoming Events only
		↓
materialize the eligible result
		↓
Razor renders that result
```

This is both a correctness boundary and a safer data-access pattern. Querying only the rows relevant to the public request avoids unnecessary transfer and in-memory exposure, reduces unnecessary work, and makes it harder for later presentation code to accidentally reveal rows that were never meant to enter the public result set. Razor's responsibility is presentation. It should not receive Draft and Cancelled Events and be trusted to hide them correctly.

<br>

<a id="replace-the-temporary-event-value-with-a-database-query"></a>
## 6.1 Replace the Temporary Event Value with a Database Query

Open:

**File: `Controllers/EventsController.cs`**

```csharp
using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
		private readonly ILogger<EventsController> _logger;
		private readonly ApplicationDbContext _context;

		public EventsController(
				ILogger<EventsController> logger,
				ApplicationDbContext context)
		{
				_logger = logger;
				_context = context;
		}

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
}
```

The important transition is this:

```text
Lesson 1:
controller creates "Library Research Night"

Lesson 2:
controller asks the database which Events qualify
```

<br>

<a id="read-the-composed-query-from-top-to-bottom"></a>
## 6.2 Read the Composed Query from Top to Bottom

The query starts from:

```csharp
_context.Events
```

This identifies the persisted Event set that EF Core can query.

<br>

<a id="read-without-change-tracking"></a>
### 6.2.1 Read without change tracking

The page is read-only, so the query uses:

```csharp
.AsNoTracking()
```

EF Core can track materialized entities so later changes to those objects can be detected and persisted. This action has no intention of editing the Event objects it reads; it only needs them long enough to render a response. `AsNoTracking()` tells EF Core that change tracking is unnecessary for this query.

<br>

<a id="eagerly-load-the-related-category"></a>
### 6.2.2 Eagerly load the related Category

The View needs the Category name, so the query loads that relationship:

```csharp
.Include(eventItem => eventItem.Category)
```

The `Category` navigation property describes the relationship in the model, but that does not mean every Event query automatically retrieves Category data. `Include(...)` asks EF Core to **eagerly load** the related Category needed for this result so Razor can use `eventItem.Category.Name` without depending on an additional loading mechanism.

For a simple reference navigation such as this one, EF Core commonly retrieves the related row through a SQL join, although the exact SQL strategy can vary.

<br>

<a id="apply-filtering-before-materialization"></a>
### 6.2.3 Apply filtering before materialization

The public browse rule is applied before the data is materialized:

```csharp
.Where(eventItem =>
		eventItem.Status == EventStatus.Published &&
		eventItem.StartDateTime > now)
```

That excludes:

- Draft Events
- Cancelled Events
- Past Events

This predicate defines the public data set **before materialization**. That matters beyond presentation. A pattern such as “load every Event and hide the non-public ones in Razor” would over-fetch rows the request does not need, place non-public data in application memory unnecessarily, increase processing and transfer work, and make accidental exposure easier. The query should return only the data the public endpoint is allowed and expected to work with.

<br>

<a id="order-the-result-deterministically"></a>
### 6.2.4 Order the result deterministically

The remaining Events are ordered chronologically, with `Id` as a tie-breaker when two Events have the same start time:

```csharp
.OrderBy(eventItem => eventItem.StartDateTime)
.ThenBy(eventItem => eventItem.Id)
```

`StartDateTime` remains the product-defined primary ordering. Because `Id` is unique, the secondary ordering makes equal-start-time results deterministic without changing their chronological grouping.

Without a tie-breaker, the database is not required to return Events with the same `StartDateTime` in any particular relative order. Their order may therefore vary between executions as data, indexes, query plans, or the database environment change.

Deterministic ordering becomes increasingly important during production hardening. Stable ordering makes repeated requests reproducible, reduces flaky ordering assertions in regression tests, simplifies troubleshooting, and becomes essential if the result is later paginated because unstable ordering can cause rows to appear on different pages between requests.

<br>

<a id="cross-the-execution-and-materialization-boundary"></a>
## 6.3 Cross the Execution and Materialization Boundary

At this point, `query` still represents database work to be performed. The next statement crosses the execution/materialization boundary:

```csharp
var events = await query.ToListAsync();
```

Before this call, the LINQ operations describe the database work EF Core should perform. `ToListAsync()` causes the composed query to execute asynchronously and the returned relational rows to be materialized as C# `Event` objects in a list.

Keeping the public filter before materialization means that the DBMS performs the filtering and the application receives the **constrained result set** relevant to the page instead of a broader set that must be narrowed afterward.

<br>

<a id="constrained-queries-and-bounded-operations"></a>
## 6.4 Constrained Queries and Bounded Operations

The word **constrained** is important here. A query constraint determines **which data qualifies** for an operation. The public predicate:

```csharp
eventItem.Status == EventStatus.Published &&
eventItem.StartDateTime > now
```

constrains the result to Published upcoming Events.

It does **not** place a strict upper limit on **how many** Events can qualify. If the database eventually contains 50,000 Published upcoming Events, all 50,000 still satisfy the predicate.

Keep this distinction visible:

```text
constraint
		→ limits WHICH data or work qualifies

hard bound
		→ limits HOW MUCH data or work may occur
```

For example:

```csharp
.Where(eventItem => eventItem.Status == EventStatus.Published)
```

constrains the result.

A later query modification such as:

```csharp
.Take(50)
```

would establish a strict maximum of 50 returned rows for that operation.

This distinction matters increasingly in production systems. Where an operation could otherwise grow without a practical limit, applications commonly introduce explicit bounds on input size, result size, execution time, retry count, request frequency, concurrent work, or other resource consumption. These bounds make the cost of one request more predictable and improve scalability, reliability, resilience, and abuse resistance.

CEMS does **not** add a hard result-count bound in this lesson. The current task is to establish the vocabulary and the design principle:

> **Constrain work to what the operation needs, and introduce explicit bounds when the amount or duration of that work could otherwise grow without an acceptable limit.**

Lesson 3 will further constrain the public query with search and Category predicates before materialization. Those additional predicates still do not create a strict maximum result count; they narrow eligibility rather than cap cardinality.

<br>

<a id="why-async-and-await-are-useful-here"></a>
## 6.5 Why `async` and `await` Are Useful Here

Database work involves I/O. An asynchronous action can wait for database I/O without unnecessarily blocking the request-handling thread for the entire wait.

The action signature:

```csharp
public async Task<IActionResult> Index()
```

and:

```csharp
await query.ToListAsync();
```

form one coherent asynchronous flow.

Asynchrony does not itself bound the amount of work performed. A very large query is still very large when executed asynchronously. This is another reason to keep **asynchronous execution**, **query constraints**, and **resource bounds** conceptually separate.

---

<br>

<a id="render-persisted-events"></a>
# 7. Render Persisted Events

The controller is now database-backed, but the existing View still expects `@model string`. The data source has changed, so the presentation contract must change with it. We will fix that mismatch in this section.

Open:

**File: `Views/Events/Index.cshtml`**

Replace the Lesson 1 string View with this first persisted version:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>

@{
	ViewData["Title"] = "Events";
}

<h1>Upcoming Campus Events</h1>

<section aria-labelledby="event-list-heading">
	<h2 id="event-list-heading" class="visually-hidden">Published upcoming events</h2>

	@foreach (var eventItem in Model)
	{
		<article>
			<h3>@eventItem.Title</h3>

			<p><strong>Category:</strong> @eventItem.Category.Name</p>
			<p><strong>Location:</strong> @eventItem.Location</p>
			<p>
				<strong>Starts:</strong>
				<time>@eventItem.StartDateTime.ToString("f")</time>
			</p>
		</article>
	}
</section>
```

The model has changed from:

```cshtml
@model string
```

to:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>
```

The controller returns a collection, and the View now declares a compatible collection model.

You may avoid using the fully qualified namespaces if your `_ViewImports.cshtml` contains:

```cshtml
@using System.Collections.Generic
@using CampusEvents.Models
```

then your view can simply use:

```cshtml
@model IReadOnlyList<Event>
```

This is an MVC contract. `return View(events);` passes an Event collection to Razor, so the View must declare a model type that can receive that collection. The database and EF Core are no longer involved at this point; Razor is working with the C# objects the controller supplied.

Each Event is rendered as an `<article>`, with semantic headings and a `<time>` element for its start date.

At this checkpoint, the Event title is deliberately plain heading text. The page that will display one Event has not been introduced yet, so the catalog does not link to a destination that does not exist.

Run it:

```cmd
dotnet build
dotnet run
```

Request:

```text
/Events
```

<br>

---

## Observe the Query Execute

The flow above is not only conceptual. ASP.NET Core logging and the debugger let you observe the important boundaries.

Add the following line after the query building and before query materialization.

```csharp
string sql = query.ToQueryString();
```

Place one breakpoint at the beginning of the `Index()`, run the application under the debugger, and request:

```text
/Events
```

After the breakpoint, step over each instruction one at a time. While paused, inspect the SQL representation of the composed query in the debugger's Watch window. `ToQueryString()` is a debugging aid. It lets you inspect EF Core's SQL representation of the LINQ query **without executing the query**.

Continue execution. In the application output, find an EF Core log information entry that shows you the actual SQL command sent to the DBMS. The logged command should contain SQL corresponding to the LINQ expression. Look for the relational equivalents of:

```text
Include(...)	→ related Category data, commonly through a JOIN
Where(...)		→ WHERE
OrderBy(...)	→ ORDER BY StartDateTime
ThenBy(...)		→ additional ORDER BY key for Id
```

Before continuing, inspect the seed data and predict the public result set.
```text
Status == Published && StartDateTime > now
```

<details>
<summary>Q. Which initial seeded Events satisfy both conditions?</summary>
A. Your expected answer should be:

1. Library Research Night
2. Career Networking Lunch

They should appear in that order because their start times are three and five days after the seed date.

**NOTE:** If you do not see both events, check whether three or five days have passed since you ran the DbInitializer for the first time. If so, it is not a bug, but the app working as it should. You will need to drop the database, apply the migration, and run the app again to reset the seeded data to newer start times.

```cmd
dotnet ef database drop --force -- --environment Development
dotnet ef database update -- --environment Development
```
</details>

<br>

Continue execution to just after `query.ToListAsync()` , inspect `events`. It now contains materialized C# `Event` objects, including the eagerly loaded `Category` objects required by the View.

You have now observed the same query at three stages:

```text
composed IQueryable<Event>
		↓	inspect with query.ToQueryString()
SQL command execution
		↓	observe Microsoft.EntityFrameworkCore.Database.Command log
materialized List<Event>
		↓	inspect with debugger after await
C# objects available to the controller
```

The browse request now works like this, and the bracketed stages have observable evidence:

```text
Browser requests /Events
				↓
routing selects EventsController.Index()
				↓
controller composes IQueryable<Event>
				↓
LINQ has a SQL representation [debugger / ToQueryString()]
				↓
EF Core executes SQL [Database.Command log]
				↓
DBMS filters and orders rows
				↓
EF Core has materialized List<Event> [debugger after ToListAsync()]
				↓
controller passes the collection to Razor
```

The important boundary is between **query description**, **database-command execution**, and **materialized objects**. The debugger and `ToQueryString()` let you inspect the composed query before execution. The EF Core `Database.Command` log supplies runtime evidence that a command was executed. The debugger after `ToListAsync()` lets you inspect the C# objects produced from the result.

---

<br>

The initial seed should produce:

1. **Library Research Night**
2. **Career Networking Lunch**

You should not see:

- Welcome Mixer Planning Draft
- Outdoor Movie Night
- Alumni Panel Archive

At this point, `/Events` is no longer displaying the temporary Lesson 1 string. It is rendering persisted relational data.

---

<br>

<a id="add-an-event-detail-page"></a>
# 8. Add an Event Detail Page

The catalog now lists persisted Events. The next product problem is resource identity: when a visitor selects one Event, the server needs a stable way to identify which persisted Event is being requested.

The Event primary key provides that identity. The route can carry the ID to the controller, but the ID alone is **not** enough to decide whether the public request should succeed. The Event must also satisfy the public availability rule for this endpoint. This is preferable to first loading any Event by ID and then deciding in the View whether it should be shown.

The existing conventional route is:

```text
{controller=Home}/{action=Index}/{id?}
```

A request can therefore place an integer in the optional `id` segment, for example:

```text
/Events/Details/1
```

Routing can bind that value to a parameter named `id`.

<br>

<a id="add-the-controller-action"></a>
## 8.1 Add the controller action

Open:

**File: `Controllers/EventsController.cs`**

Add the following action below `Index()`:

```csharp
public async Task<IActionResult> Details(int id)
{
	var query = _context.Events
		.AsNoTracking()
		.Include(item => item.Category)
		.Include(item => item.Organizer)
		.Where(item =>
			item.Id == id &&
			item.Status == EventStatus.Published);

	string sql = query.ToQueryString();

	var eventItem = await query.SingleOrDefaultAsync();

	if (eventItem is null)
	{
		Response.StatusCode = StatusCodes.Status404NotFound;
		return View("EventNotFound");
	}

	return View(eventItem);
}
```

Because this action uses `StatusCodes`, add this namespace to the controller imports:

```csharp
using Microsoft.AspNetCore.Http;
```

The query loads both relationships required by the page:

```csharp
.Include(item => item.Category)
.Include(item => item.Organizer)
```

Notice that the predicate combines identity and public eligibility:

```csharp
item.Id == id && item.Status == EventStatus.Published
```

so a guessed Draft ID is not exposed through this public workflow.

The controller is not asking, “Does any Event with this ID exist?” and then separately exposing its status. It is asking, “Is there a Published Event matching this public request?” A nonexistent ID and a non-public Event ID therefore both produce no public resource from this query. That keeps the public endpoint focused on what the visitor is entitled to retrieve.

`SingleOrDefaultAsync(...)` returns one matching Event or `null`. Because `Id` is the primary key, more than one matching Event should not exist.

<br>

<a id="create-the-razor-view"></a>
## 8.2 Create the Razor View

Create:

**File: `Views/Events/Details.cshtml`**

```cshtml
@model CampusEvents.Models.Event

@{
	ViewData["Title"] = Model.Title;
}

<article>
	<header>
		<h1>@Model.Title</h1>
	</header>

	<p>@Model.Description</p>

	<dl>
		<dt>Category</dt>
		<dd>@Model.Category.Name</dd>

		<dt>Location</dt>
		<dd>@Model.Location</dd>

		<dt>Starts</dt>
		<dd><time>@Model.StartDateTime.ToString("f")</time></dd>

		<dt>Ends</dt>
		<dd><time>@Model.EndDateTime.ToString("f")</time></dd>

		<dt>Organizer</dt>
		<dd>@Model.Organizer.DisplayName</dd>

		<dt>Configured capacity</dt>
		<dd>@Model.Capacity</dd>

		<dt>Current registration availability</dt>
		<dd>
			Live availability is not available yet because Registration data
			has not been introduced.
		</dd>
	</dl>
</article>

<p>
	<a asp-controller="Events" asp-action="Index">Back to Events</a>
</p>
```

This page displays the Event's:

- title
- description
- Category
- location
- start date/time
- end date/time
- organizer display name
- configured capacity

<br>

<a id="configured-capacity-is-not-live-availability"></a>
## 8.3 Configured capacity is not live availability

Suppose the page displays:

```text
Configured capacity: 60
```

That tells you the Event's maximum participant capacity. It does **not** tell you how many places remain. Eventually:

```text
remaining capacity 	= configured capacity - current registration count
```

The current registration count cannot be calculated yet because the real Registration relationship has not been introduced. For that reason, `FR-SRH-004` is still partial. The correct approach is to display the configured capacity now and explicitly state that live availability is not available yet. Do not invent a fake remaining-capacity value.

---

<br>

<a id="link-the-catalog-to-event-details"></a>
# 9. Link the Catalog to Event Details

The destination action and its Razor View now exist. Return to:

**File: `Views/Events/Index.cshtml`**

Inside the `foreach`, replace
```cshtml
<h3>@eventItem.Title</h3>
```

with:

```cshtml
<h3>
	<a asp-controller="Events" asp-action="Details" asp-route-id="@eventItem.Id">@eventItem.Title</a>
</h3>
```

The Tag Helpers use the controller, action, and Event ID to generate the link for that Event. The generated URL now takes the form:

```text
/Events/Details/1
```

Build and run the application, open `/Events`, and select one of the published upcoming Event titles. The request should load the corresponding persisted Event rather than a hard-coded page.

---

<br>

<a id="handle-missing-events"></a>
# 10. Handle Missing Events

A public resource lookup has two legitimate outcomes: a matching public Event exists, or it does not. “Not found” is therefore an expected HTTP result, not necessarily an application failure. For this endpoint, the same public outcome is appropriate when:

```text
the requested ID does not exist or does not identify a Published event
		↓
404 Not Found
```

The response should still be useful to a person, but it should also preserve correct HTTP semantics for browsers, automated clients, logs, and diagnostics. The controller already checks whether the database query returned `null`. Now add the View used for the controlled response.

Create:

**File: `Views/Events/EventNotFound.cshtml`**

```cshtml
@{
	ViewData["Title"] = "Event Not Found";
}

<h1>Event Not Found</h1>

<p>
	The requested published event could not be found.
</p>

<p>
	<a asp-controller="Events" asp-action="Index">Back to Events</a>
</p>
```

When no matching Published Event exists, the action does two things:

```csharp
Response.StatusCode = StatusCodes.Status404NotFound;
return View("EventNotFound");
```

The browser receives a human-readable page **and** the HTTP response status remains 404.

Now that the concrete action exists, the request can be traced precisely:

```text
GET /Events/Details/{id}
	↓
route binds {id}
	↓
EventsController.Details(id)
	↓
Published Event lookup + Category + Organizer
	↓
found? ── no ──> 404 EventNotFound View
	│
	 yes
	↓
Views/Events/Details.cshtml
	↓
HTML response
```

Request an ID that does not exist:

```text
/Events/Details/999999
```

You should see:

- the application remains running
- the Event Not Found page appears
- the response status is 404
- no null-reference exception is thrown
- no database details or stack trace are exposed

A missing resource is an expected web condition. Handling it deliberately is better than allowing `null` to flow into code that assumes an Event exists. The controlled 404 also avoids revealing an unnecessary distinction between “there is no Event with that ID” and “an Event with that ID exists but is not available through this public endpoint.” The public contract is simply that no matching public Event was found.

---

<br>

<a id="guided-practice"></a>
# 11. Guided Practice

<a id="follow-the-relationships-on-an-event-details-request"></a>
## 11.1 Follow the relationships on an event details request

Open Library Research Night from the catalog. The browser receives one HTML page, but the server can assemble that page from related relational records. Run CEMS under the debugger and trace one request to `/Events/Details/1`:

1. set a breakpoint at the beginning of `EventsController.Details()`
2. request `/Events/Details/1`
3. inspect `string sql = query.ToQueryString();`
4. identify the LINQ → SQL mappings in the debugger Watch window and the terminal log entry for the query
5. inspect the materialized `eventItem` at `SingleOrDefaultAsync()`.
6. expand the event and confirm that its `Category` and `Organizer` navigation properties are populated because the query used `Include(...)` for both.

Record the evidence for each stage:

```text
LINQ query composition	→ controller source + debugger
SQL representation			→ query.ToQueryString()
SQL command execution		→ Database.Command Information log
materialized C# objects → debugger after SingleOrDefaultAsync
```

Now do the same with:

```text
/Events/Details/999999
```

Observe the sequence:

1. routing binds `999999` to `id`
2. the EF Core query finds no matching Published Event
3. `eventItem` is `null`
4. the controller sets HTTP 404
5. `EventNotFound.cshtml` renders a controlled response

<br>

<a id="add-one-seed-event"></a>
## 11.2 Add one seed Event

Now make one bounded change using the seed pattern already demonstrated.

Open:

**File: `Data/DbInitializer.cs`**

Add this start-time variable with the existing date variables:

```csharp
var workshopStart = DateTime.Today.AddDays(60).AddHours(16);
```

Add this Event to the `events` array:

```csharp
new Event
{
	Title = "Study Skills Workshop",
	Description = "A practical session on planning, note-taking, and exam preparation.",
	Location = "Learning Commons, Room 110",
	StartDateTime = workshopStart,
	EndDateTime = workshopStart.AddHours(1),
	Capacity = 40,
	Status = EventStatus.Published,
	CreatedAt = createdAt,
	CategoryId = academic.Id,
	OrganizerId = organizer.Id
}
```

Run it:

```cmd
dotnet build
dotnet run
```

<details>
<summary>Q. Why it does not show on the Event list or Details page even though it is a Published upcoming event?</summary>
A. The initializer exits when Events already exist:

```csharp
if (await context.Events.AnyAsync())
{
	return;
}
```

Your existing database has already been seeded, so editing the initializer does not rewrite those stored rows. Stop the application and reset the local database by dropping it and reapplying the existing migration:

```cmd
dotnet ef database drop --force -- --environment Development
dotnet ef database update -- --environment Development
```

Then run the application again:

```cmd
dotnet run
```

The database is empty after the reset, so the initializer runs again and builds the six-Event seed set.
</details>

---

<br>

<a id="check-your-understanding"></a>
# 12. Check Your Understanding

Answer these:

<details>
<summary>1. What is the difference between <code>CategoryId</code> and <code>Category</code> on an Event?</summary>

`CategoryId` is the foreign-key value stored on the Event row. It identifies which Category the Event belongs to.

`Category` is the navigation property that gives C# code access to the related `Category` object when EF Core loads that relationship.

</details>

<details>
<summary>2. Why does an Event contain both <code>OrganizerId</code> and <code>Organizer</code>?</summary>

`OrganizerId` is the foreign-key value stored on the Event row and identifies the related organizer.

`Organizer` is the navigation property that EF Core can populate with the related `ApplicationUser` object. Together, they provide both the stored relational reference and an object-oriented way to work with the organizer in C#.

</details>

<details>
<summary>3. What does <code>ApplicationDbContext</code> inherit from, and what role does it play in CEMS?</summary>

`ApplicationDbContext` inherits from EF Core's `DbContext`.

It represents CEMS's database context and provides EF Core with the configuration and entry points needed to query and save `ApplicationUser`, `Category`, and `Event` data. It also defines relationships and database constraints for the current model.

</details>

<details>
<summary>4. What is the purpose of the <code>InitialCemsCatalog</code> migration?</summary>

The `InitialCemsCatalog` migration records how EF Core believes the relational database schema should be created to match the C# model.

It creates the initial tables, primary keys, foreign keys, relationships, and check constraints needed for the CEMS catalog.

</details>

<details>
<summary>5. Why are Draft and Cancelled Events stored in the database but absent from normal public browsing?</summary>

Draft and Cancelled Events are valid stored Event records, but they do not satisfy the public browsing rule.

The public query requires an Event to have `EventStatus.Published`, so Draft and Cancelled Events remain in the database for their lifecycle state but are excluded from the public catalog.

</details>

<details>
<summary>6. Why is <code>.OrderBy(eventItem =&gt; eventItem.StartDateTime)</code> part of a product requirement rather than merely a visual preference?</summary>

CEMS SRS requires upcoming Events to appear in chronological order by start time. The ordering therefore determines how the public catalog is supposed to behave, rather than serving only as a presentation choice.

Applying `OrderBy` in the query ensures the Events are returned from the database in the required order.

</details>

<details>
<summary>7. Why does the Index query use <code>.Include(eventItem =&gt; eventItem.Category)</code>?</summary>

The Index View displays each Event's Category name through `eventItem.Category.Name`.

`.Include(eventItem => eventItem.Category)` tells EF Core to load the related Category data along with the Events so that information is available when the View renders the catalog.

</details>

<details>
<summary>8. Why does the one-Event query include both Category and Organizer?</summary>

The Event detail page displays information from both related records: the category `Name` and the organizer's `DisplayName`.

The query therefore includes both `Category` and `Organizer` so EF Core loads the related objects needed by the View.

</details>

<details>
<summary>9. Why is configured capacity available now while current registration availability is not?</summary>

Configured capacity is stored directly on the Event through its `Capacity` property, so it is already available.

Current registration availability would require Registration data so CEMS could determine how many registrations currently exist and subtract that count from the configured capacity. The Registration relationship has not been introduced yet, so live availability cannot be calculated.

</details>

<details>
<summary>10. What HTTP status should a nonexistent Event request produce, and why is that preferable to an unhandled null-reference exception?</summary>

A nonexistent or unavailable Event request should produce HTTP `404 Not Found`.

A 404 is the appropriate controlled response when the requested resource does not exist. It allows CEMS to display a human-readable not-found page while preserving the correct HTTP status, instead of allowing `null` to reach code that assumes an Event exists and causing an unhandled exception.

</details>

<details>
<summary>11. What is the difference between the EF Core model, the <code>InitialCemsCatalog</code> migration, and the LocalDB schema?</summary>

The EF Core model is the persistence model EF Core builds from the C# entity classes and `ApplicationDbContext` configuration.

`InitialCemsCatalog` is the versioned schema-change description generated from that model for this stage of the project.

The LocalDB schema is the set of tables, columns, keys, and constraints that physically exists after the migration is applied with `dotnet ef database update`.

</details>

<details>
<summary>12. What does <code>ToListAsync()</code> change in the Index query?</summary>

Before `ToListAsync()`, the EF Core LINQ operations describe the database query to perform. `ToListAsync()` executes that query asynchronously and materializes the returned rows as C# `Event` objects.

Because the Published/upcoming predicate appears before this materialization point, DBMS can restrict the result before those Event objects enter application memory.

</details>

<details>
<summary>13. What observable evidence shows the transition from a composed EF Core query to executed SQL and then to materialized C# objects?</summary>

Before execution, the debugger can inspect the composed `query`, and `query.ToQueryString()` can show EF Core's SQL representation without running the query.

When `await query.ToListAsync()` executes, an `Information`-level `Microsoft.EntityFrameworkCore.Database.Command` log entry provides runtime evidence that EF Core sent a database command.

After the `await` completes, the debugger can inspect the populated `List<Event>`. Those objects are the materialized result of the database operation.

</details>

<details>
<summary>14. Why is filtering non-public Events only in Razor a poor design even if the final HTML appears correct?</summary>

Razor is a presentation layer. Loading broad data and hiding rows only during rendering over-fetches data, unnecessarily exposes non-public rows to application memory, increases processing and transfer work, and creates more opportunities for accidental disclosure.

The public eligibility rule belongs in the data query so the controller receives only the rows that this public workflow should operate on.

</details>

<details>
<summary>15. Why does a foreign key still matter when application code normally supplies a valid related ID?</summary>
A foreign key matters because it makes the database itself guarantee that relationships stay valid, even if application code, scripts, migrations, or concurrent transactions make mistakes. Application validation improves behavior; foreign keys protect data integrity.
</details>

---

<br>

<a id="common-mistakes"></a>
# 13. Common Mistakes

<a id="usesqlserver-or-usesqlite-is-not-recognized"></a>
## 13.1 `UseSqlServer` or `UseSqlite` is not recognized

Confirm the provider package is installed:

```cmd
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

and that `Program.cs` contains:

```csharp
using Microsoft.EntityFrameworkCore;
```

<br>

<a id="dotnet-ef-is-not-recognized"></a>
## 13.2 `dotnet ef` is not recognized

Use the course-approved `dotnet-ef` tool version that matches the installed EF Core major version. Do not randomly install a newer incompatible major version.

<br>

<a id="the-sql-server-localdb-or-sqlite-connection-fails"></a>
## 13.3 The SQL Server LocalDB or SQLite connection fails

Confirm the SQL Server LocalDB instance:

```cmd
sqllocaldb info MSSQLLocalDB
```
or SQLite instance:

```cmd
sqlite3 --version
```

and verify that `appsettings.Development.json` or `user-secrets` contains:

```text
"ConnectionStrings:DefaultConnection": "Server=(localdb)\MSSQLLocalDB;Database=CampusEventsDb;Trusted_Connection=True;MultipleActiveResultSets=true"
```

or that `system environment variables` contains:

```text
"ConnectionStrings__DefaultConnection=Server=(localdb)\MSSQLLocalDB;Database=CampusEventsDb;Trusted_Connection=True;MultipleActiveResultSets=true"
```

**NOTE:** In JSON, the backslash appears as `\\`.

Use the following commands to verify:

```cmd
dotnet user-secrets list
```

or:

```bash
set or env or printenv
```

<br>

<a id="an-ef-command-cannot-find-defaultconnection"></a>
## 13.4 An EF command cannot find `DefaultConnection`

The connection string is in Development configuration. Schema-changing EF commands in this lesson must forward that environment, for example:

```cmd
dotnet ef database update -- --environment Development
```

<br>

<a id="ef-core-sql-does-not-appear-in-the-application-output"></a>
## 13.5 EF Core SQL does not appear in the application output

Confirm that the application is running with the Development logging configuration and that `appsettings.Development.json` contains:

```json
"Microsoft.EntityFrameworkCore.Database.Command": "Information"
```

Then request `/Events` again and look for a log category similar to:

```text
Microsoft.EntityFrameworkCore.Database.Command
```

<br>

<a id="the-migration-is-empty"></a>
## 13.6 The migration is empty

Check that:

- `ApplicationDbContext` derives from `DbContext`
- its `DbSet` properties exist
- the SQL Server / SQLite and Design packages are installed
- `Program.cs` registers `ApplicationDbContext`

<br>

<a id="seed-insertion-fails"></a>
## 13.7 Seed insertion fails

Check that each Event satisfies:

```text
StartDateTime < EndDateTime
Capacity > 0
```

Also confirm that its category and organizer rows exist.

<br>

<a id="events-shows-draft-or-cancelled-events"></a>
## 13.8 `/Events` shows Draft or Cancelled Events

The final query must include both:

```csharp
eventItem.Status == EventStatus.Published &&
eventItem.StartDateTime > now
```

Do not load every row and hide unwanted Events only in Razor. That would move the public-data boundary into the presentation layer. It would also over-fetch rows, increase in-memory data exposure and resource use, and make the page dependent on every rendering path remembering to suppress non-public data. Keep the eligibility predicate in the database query.

<br>

<a id="category-data-fails-in-the-catalog"></a>
## 13.9 Category data fails in the catalog

The Index query needs:

```csharp
.Include(eventItem => eventItem.Category)
```

because the View reads `eventItem.Category.Name`.

<br>

<a id="organizer-data-fails-on-the-event-page"></a>
## 13.10 Organizer data fails on the Event page

The one-Event query needs:

```csharp
.Include(item => item.Organizer)
```

because the page reads the organizer's `DisplayName`.

<br>

<a id="the-razor-model-does-not-match-the-controller"></a>
## 13.11 The Razor model does not match the controller

The final Index View expects:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>
```

and the controller passes the Event collection returned by `ToListAsync()`.

The Event page expects:

```cshtml
@model CampusEvents.Models.Event
```

and the controller passes one Event object.

<br>

<a id="your-new-seed-event-does-not-appear-after-editing-dbinitializer"></a>
## 13.12 Your new seed Event does not appear after editing `DbInitializer`

The existing database is already populated, so `Events.AnyAsync()` causes the initializer to return. Use the Development database reset workflow from the guided practice when you intentionally want to rebuild the seed dataset.

---

<br>

<a id="what-you-will-build-what-you-built"></a>
# What You Will Build / What You Built

By the end of this lesson, CEMS will have its first database-backed vertical slice:

- Event, Category, and organizer-ownership data stored relationally
- an EF Core `ApplicationDbContext`
- an initial migration named `InitialCemsCatalog`
- Development-only seed data
- a public catalog that shows only Published upcoming Events
- deterministic chronological ordering
- a page that displays Event details
- explicit HTTP 404 behavior when a requested Event is unavailable

The public browsing work completes `FR-SRH-001`, `FR-SRH-002`, and `FR-SRH-003`. `FR-SRH-004` remains partial because current registration availability depends on Registration data that does not exist yet.

<br>

<a id="summary"></a>
# 14. Summary

## What You Learned

Lesson 2 replaced temporary controller-created Event data with a real relational persistence layer. The most important ideas are not only the classes and migration you created; they are the boundaries between the C# object model, EF Core, the database schema, query construction, query execution, and Razor rendering.

You should now be able to explain that:

- `Event`, `Category`, and `ApplicationUser` represent related application data, while foreign keys store relationships in the database and navigation properties give C# code object-oriented access to related entities;
- `ApplicationDbContext` is the EF Core unit through which the application describes and queries persisted data, but the `DbContext`, a migration, and the actual database schema are different things;
- a migration records an intended schema change, while applying that migration changes the database itself;
- Development seed data gives the application repeatable classroom data without turning seed logic into ordinary production behavior;
- an `IQueryable<Event>` can be composed before Event rows are loaded into application memory;
- filtering and ordering belong in the database query when the database can perform that work, rather than loading broad data and hiding or sorting it later in Razor;
- `ToQueryString()` can help inspect the SQL representation of a query without executing it, while `ToListAsync()` crosses the execution/materialization boundary and produces C# objects from the returned rows;
- eager loading with `Include(...)` is necessary when the rendered result depends on related data that the query must load;
- a missing or unavailable Event is an expected request outcome that can produce a controlled `404 Not Found`, while an unexpected application failure belongs to a different error path; and
- production-oriented reliability begins with deliberately constraining work: queries, result sizes, inputs, time, concurrency, and other resource-consuming operations should be bounded where practical.

A useful persistence mental model to carry forward is:

```text
C# entity model
    ↓
ApplicationDbContext + EF Core configuration
    ↓
migration history
    ↓
DBMS schema and rows
```

The request flow from Lesson 1 is still recognizable, but the application work in the middle is now database-backed:

```text
HTTP request
    ↓
controller composes IQueryable<Event>
    ↓
EF Core translates the query
    ↓
DBMS executes SQL
    ↓
EF Core materializes C# objects
    ↓
controller passes data to Razor
    ↓
HTML response
```

That distinction between **query composition** and **materialization** is especially important. Later lessons will add more predicates, paging, authorization-sensitive constraints, and registration-dependent rules without changing the basic principle that the database should receive the narrowest appropriate query before rows are materialized.

## Where CEMS Stands

CEMS now has its first database-backed product slice. `/Events` loads persisted Events, restricts the public catalog to Published upcoming Events, orders results chronologically with an `Id` tie-breaker, loads required related data, and renders the result through Razor. A visitor can also open an eligible Event Details page, while unavailable Event IDs receive controlled not-found behavior.

The completed browse flow is:

```text
GET /Events
    ↓
EventsController.Index()
    ↓
_context.Events
    ↓
Published + future-start filter
    ↓
OrderBy(StartDateTime) + ThenBy(Id)
    ↓
ToListAsync()
    ↓
DBMS command execution
    ↓
EF Core materialization
    ↓
List<Event>
    ↓
Views/Events/Index.cshtml
    ↓
HTML response
```

The application now includes:

- DBMS configuration through `DefaultConnection`;
- EF Core provider and design-time support;
- `EventStatus` with Draft, Published, and Cancelled;
- persisted `Category`, `Event`, and `ApplicationUser` models;
- Event → Category and Event → Organizer relationships;
- database constraints for start/end ordering and positive Capacity;
- the applied `InitialCemsCatalog` migration;
- Development-only seed data;
- an asynchronous database-backed public Event catalog;
- observable EF Core query behavior through `ToQueryString()`, `Database.Command` logging, and debugger inspection after materialization;
- Published/upcoming filtering;
- deterministic chronological ordering;
- a persisted Event Details page;
- configured Capacity display; and
- controlled `404 Not Found` behavior for unavailable Event IDs.

The functional requirement state at this point is:

- `FR-SRH-001` — **complete**;
- `FR-SRH-002` — **complete**;
- `FR-SRH-003` — **complete**;
- `FR-SRH-004` — **partial**.

`FR-SRH-004` remains partial for one specific reason: CEMS can display configured Capacity, but it cannot yet calculate live/current registration availability because Registration data does not exist yet.

The public catalog also has an intentional usability limitation: every visitor currently receives the same complete Published/upcoming list. There is no text search, Category filter, or page-sized result navigation yet.

## Coming Next

In **Lesson 3 — Search, Filtering, Pagination, and View-Specific Models**, you will keep the public-visibility rule established in this lesson and let visitors narrow that already-eligible catalog by search text and Category.

The important rule is that search and filtering will **narrow** public visibility; they must never make a Draft, Cancelled, past, or otherwise ineligible Event public simply because some optional search criterion matches it.

The query will evolve from one fixed public list into a request-specific database query:

```text
Published + upcoming Events
    ↓
optional text predicate
    ↓
optional Category predicate
    ↓
CountAsync() for total matches
    ↓
stable ordering
    ↓
Skip(...) + Take(...)
    ↓
ToListAsync() for one page
    ↓
view-specific model containing results + request state
    ↓
Razor renders filters, results, and pagination
```

You will also distinguish persisted application data from temporary **request state** such as search text, selected Category, and page number. That distinction will motivate the first view-specific model: the page needs both domain data to display and transient state to reconstruct the visitor's current search and pagination controls.

The key continuity is this: **Lesson 3 does not replace the EF Core query pipeline you built here. It extends that pipeline by composing additional predicates and paging operations before materialization, so the database still performs the filtering and returns only the rows needed for the current request.**

<br>

<a id="appendix-production-hardening-constraining-and-bounding-work"></a>
# Appendix — Production Hardening: Constraining and Bounding Work

The value of bounded operations is broader than performance. In production, bounding work improves predictability, scalability, reliability, resource control, and security. It reduces unnecessary database I/O, network transfer, application memory, CPU usage, lock/connection duration, and the chance that one expensive request consumes disproportionate resources. It also reduces the blast radius of unexpectedly large datasets or abusive requests.

Production systems use several kinds of constraints and bounds. You do not need to implement all of them in CEMS now. The purpose of this primer is to give you vocabulary that will recur as the applications become more production-oriented.

<br>

| Concept / tool | What it controls and where it will reappear |
|---|---|
| **Query constraints** | `Where(...)` limits which rows qualify before materialization. This reduces ***over-fetching*** but does not guarantee a maximum row count. This will be discussed concretely later in Lessons 3, 11, 12, 15, and 19. |
| **Deterministic ordering** | A unique secondary key such as `ThenBy(Id)` gives equal primary-sort values a stable order. Stable ordering improves reproducibility and is necessary for reliable paging strategies. This will be reinforced later in Lessons 3, 12, 18, and 19. |
| **Projection / data-shape constraints** | `Select(...)` into a purpose-specific DTO limits which fields cross an application boundary. It bounds response *shape*, although not necessarily row count. This will be discussed concretely later in Lessons 11, 15, 18, and 19. |
| **Input-size bounds** | A request can reject values outside an explicit supported range, such as search text longer than 100 characters. Input bounds prevent one field from growing without limit and make validation/resource expectations clearer. This will be discussed concretely later in Lessons 12, 13, 16, 17, 18, and 19. |
| **Strict result-count caps** | A construct such as `.Take(20)` gives a query a known maximum number of returned rows. This is a true result-size bound rather than only a filtering constraint. In Lesson 3, `.Take(pageSize)` limits each public catalog page to a fixed number of Events. |
| **Pagination of potentially large ordinary lists** | Pagination returns one bounded portion of an ordered result at a time. Lesson 3 implements numbered, offset-based pagination for the public Event catalog using stable ordering, `CountAsync()`, and `Skip(...).Take(pageSize)`. It also compares cursor/keyset pagination with offset pagination and distinguishes database retrieval from interface patterns such as infinite scrolling. |
| **Cancellation of obsolete work** | Cancellation asks work to stop when it no longer represents current intent, such as abandoning an older search after a newer search begins. Cancellation is expected control flow, not necessarily failure. This will be discussed concretely later in Lessons 12, 16, 18, and 19. |
| **Debounce and serialized client work** | Debouncing waits for a short quiet period before starting rapidly repeated work; serialization can restrict a workflow to one in-flight mutation at a time. These controls reduce unnecessary overlap. This will be discussed concretely later in Lessons 16, 18, and 19. |
| **Request timeouts** | A timeout establishes a maximum acceptable duration for a request or operation. It protects the system from work that waits indefinitely and differs from cancellation merely because newer intent replaced older intent. This will be discussed concretely later in Lessons 12 and 19. |
| **Rate limiting** | A rate limiter caps how frequently a client, identity, address, or endpoint may perform an operation. Common policies include fixed-window, sliding-window, token-bucket, and concurrency limits. This will be discussed concretely later in Lessons 13 and 19. |
| **Server request-body size limits** | A server can reject an HTTP body that exceeds an allowed transport-level size before an endpoint is forced to accept arbitrarily large payloads. This is distinct from validating individual fields after model binding. This will be discussed concretely later in Lessons 17 and 19. |
| **Database command timeout policy** | A database command timeout limits how long the provider permits a command to run before treating it as timed out. It is a lifetime safeguard, not a substitute for efficient queries and indexes. This will be discussed concretely later in Lesson 19. |
| **Bounded retry and backoff policies** | A retry policy permits only a finite number of retry attempts and spaces them over time, often with increasing delay and jitter. The goal is recovery from transient failure without retrying forever or creating a retry storm. This will be discussed concretely later in Lessons 12, 17, and 19. |
| **Concurrency bounds** | Concurrency controls limit simultaneous work. Examples include one in-flight autosave, a semaphore, a bounded queue, or an endpoint concurrency limiter. This will be discussed concretely later in Lessons 16, 18, and 19. |
| **Batch-size bounds** | A batch limit caps how many records or side effects one unit of work processes at once. This can keep memory use, transaction duration, and failure impact predictable when a workflow fans out across many records. This will be discussed concretely later in Lessons 17 and 19. |
| **Transactional scope and locking** | Transactions and locks deliberately define which reads and writes must succeed together and which current rows participate in one authoritative decision. They bound the atomic decision surface even though they are not result-count caps. This will be discussed concretely later in Lessons 15, 17, 18, and 19. |

<br>

A production-hardening review often asks the same question in several dimensions:

> **What can grow here, and what prevents one request, user, failure, or workload from consuming an unreasonable amount of system resources?**

In production systems, operations should be bounded where practical so that one request cannot consume an unbounded amount of database, network, CPU, or memory resources. Techniques such as filtering, pagination, explicit limits, timeouts, and constrained request inputs make resource consumption more predictable and improve scalability and resilience.

For the current CEMS catalog, the Published/upcoming predicate **constrains** the query. It does not yet impose a hard maximum result count. That distinction is intentional and will become more concrete as later lessons introduce additional hardening tools.

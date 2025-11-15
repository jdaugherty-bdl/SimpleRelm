# SimpleRelm

**SimpleRelm** is a lightweight, actively-developed ORM for C# developers who want something simpler than the big, full-featured frameworks — without giving up strongly-typed access to their data.

Instead of trying to be a “do everything” data layer, SimpleRelm focuses on:

- **Simple, readable code** – keep your models and data access straightforward and easy to follow.
- **Fast startup and low overhead** – ideal for services and apps where heavy ORMs are overkill.
- **Minimal configuration** – get from connection string to working queries with as little setup as possible.
- **Extensible design** – new features and improvements are being added with real-world usage in mind.

SimpleRelm is built for .NET Framework and .NET (Core) developers who want:

- A quick way to map query results to C# objects
- Basic ORM conveniences without a huge learning curve
- A codebase that’s small enough to understand, extend, and debug

> ⚠️ **Project status:** This library is under active development. APIs may evolve as new features are added and edge cases are discovered. Feedback, issues, and pull requests are very welcome.

---

## Features

- **Lightweight ORM**  
  Thin abstraction on top of ADO.NET to keep things transparent and predictable.

- **POCO-friendly mapping**  
  Map query results directly into your own C# classes without invasive attributes or base classes.

- **Explicit transactions**  
  You stay in control of when transactions begin, commit, and roll back — no hidden magic.

- **`using`-friendly API**  
  Designed to be used in a `using` block so connections/transactions are cleaned up correctly.

- **Framework & Core friendly**  
  Built with traditional .NET Framework apps in mind, but usable from modern .NET (Core) projects as well.

- **Actively evolving**  
  New features and refinements are being added as the library is used in real projects.

*(If there are specific features you want to call out — e.g., parameterized queries, async APIs, bulk operations — you can drop additional bullets here.)*

---

## Getting started

# keel
Project base to support microservices using .NET

## GitHub Packages

Each project has its own GitHub Actions workflow for creating and publishing a NuGet package to GitHub Packages.

- Manual publish: run the workflow for the project in the `Actions` tab.
- Publish by tag: create a tag using the format `<PackageId>-v<version>`.

Examples:

- `Keel.Domain.CleanCode-v1.0.0`
- `Keel.Infra.Db-v1.0.0`
- `Keel.Infra.WebApi-v1.0.0`

Packages are published to:

- `https://nuget.pkg.github.com/adrianosepe/index.json`

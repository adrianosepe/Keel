# keel
Project base to support microservices using .NET

## GitHub Packages

Each project has its own GitHub Actions workflow for creating and publishing a NuGet package to GitHub Packages.

The same workflow run also publishes the package to NuGet.org.

- Manual publish: run the workflow for the project in the `Actions` tab.
- Publish by tag: create a tag using the format `<PackageId>-v<version>`. The `<version>` value must match the package `Version` evaluated by MSBuild.

Examples:

- `Keel.Domain.CleanCode-v1.1`
- `Keel.Infra.Db-v1.1`
- `Keel.Infra.WebApi-v1.1`

Packages are published to:

- `https://nuget.pkg.github.com/adrianosepe/index.json`
- `https://www.nuget.org/packages`

Required GitHub Actions secret:

- `NUGET_API_KEY`: API key generated in NuGet.org for package push

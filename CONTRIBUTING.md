# Contributing

Thanks for contributing to Bugarena.

## Before you start

- Read the project overview and setup guidance in `README.md`.
- Follow `CODE_OF_CONDUCT.md` in issues, reviews, and pull requests.
- Use GitHub issues for bugs, regressions, and feature requests.
- For security issues, follow `SECURITY.md` and do not open a public issue with exploit details.

## Change expectations

- Keep changes small, reviewable, and scoped to one concern.
- Update documentation when behavior, architecture, or operational guidance changes.
- Add or update tests when you change runtime behavior.
- Do not commit secrets, provider credentials, or local environment artifacts.
- Preserve the broker boundary: secrets stay out of source, app settings, Dockerfiles, and agent-mode application paths.

## Development workflow

The main project validation path is documented in `README.md`. The normal sequence is:

```bash
dotnet restore Bugarena.sln
dotnet build Bugarena.sln --no-restore
dotnet test Bugarena.sln --no-build
```

The platform smoke tests are intended to run inside the `agent` container with the `testinfra` profile enabled.

## Pull requests

- Describe the problem and the behavior change clearly.
- Call out any operational or security impact.
- Include the validation you ran.
- Keep generated noise out of commits.

If you are changing provider or proxy behavior, include the allowed methods, allowed path prefixes, auth mode, and failure behavior in the PR description.

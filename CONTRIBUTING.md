# Contributing to ClaimsOps

Thank you for your interest in contributing to ClaimsOps! This document provides guidelines for contributing to this project.

## Code of Conduct

Be respectful, inclusive, and professional in all interactions.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/claimsops-app.git
   cd claimsops-app
   ```

3. **Set up development environment**:
   ```bash
   cd docker
   cp .env.example .env
   docker compose up -d
   ```

4. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   # OR
   git checkout -b fix/your-bug-fix
   ```

## Development Workflow

### Branch Naming Convention

- `feature/` - New features or enhancements
- `fix/` - Bug fixes
- `docs/` - Documentation updates
- `chore/` - Maintenance tasks, dependencies
- `refactor/` - Code refactoring without behavior change
- `test/` - Adding or updating tests

**Examples:**
- `feature/add-claim-approval`
- `fix/database-connection-leak`
- `docs/update-api-documentation`
- `chore/update-dependencies`

### Commit Message Format

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>: <description>

[optional body]

[optional footer]
```

**Types:**
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `style:` - Code style changes (formatting, no logic change)
- `refactor:` - Code refactoring
- `test:` - Adding or updating tests
- `chore:` - Maintenance tasks

**Examples:**
```
feat: add claim approval endpoint

fix: resolve database connection timeout issue

docs: update README with deployment instructions

chore: update Entity Framework to 10.0.1
```

## Making Changes

### Code Style

**C# (.NET)**
- Follow [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use PascalCase for public members
- Use camelCase for private fields
- Add XML documentation comments for public APIs

**Python (FastAPI)**
- Follow [PEP 8](https://peps.python.org/pep-0008/)
- Use snake_case for functions and variables
- Add type hints
- Add docstrings for functions

### Testing

- Write unit tests for new features
- Ensure all tests pass before submitting PR:
  ```bash
  # .NET tests
  cd services/claims-service
  dotnet test
  
  # Run smoke tests
  ./tests/smoke-tests.sh
  ```

### Documentation

- Update README.md if you add new features
- Update API documentation for endpoint changes
- Add inline comments for complex logic
- Keep both English and Spanish READMEs in sync

## Submitting a Pull Request

1. **Commit your changes**:
   ```bash
   git add .
   git commit -m "feat: add your feature"
   ```

2. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

3. **Create a Pull Request**:
   - Go to the [original repository](https://github.com/SvillarroelZ/claimsops-app)
   - Click "New Pull Request"
   - Select your fork and branch
   - Fill in the PR template with:
     - Clear description of changes
     - Link to related issues
     - Screenshots (if UI changes)
     - Testing performed

4. **Address review feedback**:
   - Make requested changes
   - Commit with descriptive messages
   - Push updates to the same branch

5. **Squash commits** (if requested):
   ```bash
   git rebase -i HEAD~N  # N = number of commits
   # Mark commits as 'squash' except the first
   git push --force-with-lease
   ```

## Pull Request Checklist

Before submitting, ensure:

- [ ] Code follows project style guidelines
- [ ] All tests pass locally
- [ ] New code has test coverage
- [ ] Documentation is updated
- [ ] Commit messages follow Conventional Commits
- [ ] No secrets or credentials in code
- [ ] `.env` file is not committed
- [ ] Branch is up to date with main

## Reporting Bugs

Use the [Bug Report template](.github/ISSUE_TEMPLATE/bug_report.md) and include:

- Clear description of the bug
- Steps to reproduce
- Expected vs actual behavior
- Environment details (OS, .NET version, etc.)
- Logs or error messages

## Requesting Features

Use the [Feature Request template](.github/ISSUE_TEMPLATE/feature_request.md) and include:

- Clear description of the feature
- Use case / problem it solves
- Proposed solution (if any)
- Alternative solutions considered

## Questions?

- Check the [docs/runbook.md](docs/runbook.md) for technical details
- Open a Discussion on GitHub
- Review existing Issues and Pull Requests

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).

---

Thank you for contributing to ClaimsOps.

# azdbr

Azure SQL Database Refresh .NET global tool. Safely refresh a staging or development Azure SQL database from production within the same Microsoft Entra tenant.

## Install

```bash
dotnet tool install -g azdbr
```

## Usage

```bash
azdbr refresh <source-server> <source-database> <target-server> <target-database>
```

Example:

```bash
azdbr refresh prod-server prod-db staging staging-db
azdbr refresh prod-server prod-db staging staging-db --dry-run
azdbr refresh prod-server prod-db staging staging-db -y --service-objective S2
azdbr refresh prod-server prod-db staging staging-db --keep-old-database
```

### Options

| Option                        | Description                                                |
| ----------------------------- | ---------------------------------------------------------- |
| `--tenant-id`                 | Optional Microsoft Entra tenant id for authentication      |
| `--service-objective`         | Optional service objective for the copied database         |
| `--backup-storage-redundancy` | Optional redundancy: `LOCAL`, `ZONE`, `GEO`, `GEOZONE`     |
| `--keep-old-database`         | Keep the renamed old database after success                |
| `--dry-run`                   | Run preflight and print planned steps only                 |
| `-y`, `--yes`                 | Skip interactive confirmation                              |
| `--command-timeout-minutes`   | SQL command timeout for non-copy operations (default: 30)  |
| `--copy-timeout-minutes`      | Maximum wait time for copy completion (default: 240)       |

## What it does

1. Validates that the target database exists and no leftover `*-old` database remains
2. Kills active user sessions on the target database
3. Renames the target database to `<target-database>-old`
4. Verifies the target database name is free
5. Creates a new database with `CREATE DATABASE ... AS COPY OF`
6. Fixes orphaned login-based users
7. Syncs staging-only roles, users, and role memberships from the old database
8. Drops the old database by default

## Permissions

The identity running `azdbr` must:

- Authenticate with Microsoft Entra ID via `DefaultAzureCredential`
- Be a member of `dbmanager` in `master` on both source and target servers
- Have `db_owner` on the source database

See [Copy a database in Azure SQL Database](https://learn.microsoft.com/en-us/azure/azure-sql/database/database-copy) for platform details.

## Limitations

- Same Microsoft Entra tenant only in v1
- Cross-tenant copy requires SQL authentication and is not supported by this tool
- Database copy over a private endpoint to the destination server is not supported by Azure SQL T-SQL copy
- Both server firewalls must allow the machine running `azdbr`

## Build and test

```bash
dotnet cake.cs
```

This runs restore, build, test, pack, and tool integration tests (`azdbr --help`).

### Integration tests

The Cake pipeline installs the packed tool locally and runs:

1. `azdbr --help` (always)
2. `azdbr refresh ... -y` against real Azure SQL when integration secrets are configured

Set these environment variables (or GitHub Actions secrets) to enable the Azure SQL integration test:

| Variable                              | Description                            |
| ------------------------------------- | -------------------------------------- |
| `AZDBR_INTEGRATION_SOURCE_SERVER`     | Source logical server                  |
| `AZDBR_INTEGRATION_SOURCE_DATABASE`   | Source database                        |
| `AZDBR_INTEGRATION_TARGET_SERVER`     | Target logical server                  |
| `AZDBR_INTEGRATION_TARGET_DATABASE`   | Target database (must exist)           |
| `AZDBR_INTEGRATION_TENANT_ID`         | Optional Entra tenant for the refresh  |
| `AZURE_TENANT_ID`                     | Service principal tenant (CI)          |
| `AZURE_CLIENT_ID`                     | Service principal client id (CI)       |
| `AZURE_CLIENT_SECRET`                 | Service principal secret (CI)          |

Locally, the Azure refresh test runs when the four `AZDBR_INTEGRATION_*` server/database variables are set and you are authenticated via `az login` / `DefaultAzureCredential`. In CI, the same four variables plus Azure service principal credentials are required.

## License

MIT

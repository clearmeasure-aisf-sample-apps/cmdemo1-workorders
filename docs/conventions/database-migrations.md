# Database Migrations

Add a numbered DbUp script under `src/Database/scripts/Update/`. The facts below are the ones the Database project and the existing docs already state.

## Where the scripts live

Update scripts are files in `src/Database/scripts/Update/`. [`UpdateDatabaseCommand`](../../src/Database/Console/UpdateDatabaseCommand.cs) loads that `Update` directory from the filesystem and records each applied script in the `dbo.SchemaVersions` journal. The same command and journal are described in [DbUp](../glossary/dbup.md).

## Add a script

1. List `src/Database/scripts/Update/` and note the numbering convention `###_Description.sql`.
2. Name the new file with the next number: the highest existing number, plus one.
3. Indent the SQL with tabs. `033_AddPriorityAndBuildingToWorkOrder.sql` is one script that already does this.

`CLAUDE.md` and the cloud-agent instructions state the same three rules: location `src/Database/scripts/Update/`, name `###_Description.sql`, tabs for indentation. [Lab 04](../../labs/lab-04-database-migration.md) walks through listing the directory, incrementing the highest number, and using tabs.

## Apply it locally

Run `Build` (from `build.ps1`) or `.\PrivateBuild.ps1`. That is the local apply step those docs already name: the private build runs the Update scripts.

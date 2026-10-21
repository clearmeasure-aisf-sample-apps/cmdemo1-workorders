# Database migrations

Database schema changes are managed with DbUp scripts in `src/Database/scripts/Update/`.

Name each new script with the next sequential three-digit number, followed by an underscore and a short description, for example `040_AddWorkOrderField.sql`. Check the highest existing number before choosing the next one.

Use TAB characters for indentation in SQL scripts.

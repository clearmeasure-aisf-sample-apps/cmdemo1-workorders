# CSV Bulk Import of Work Orders

The bulk import endpoint allows you to create multiple draft work orders in a single request by uploading a CSV file.

## Endpoint

```
POST /api/work-orders/bulk-import
POST /api/v1.0/work-orders/bulk-import
```

## Authentication

No authentication is required. The endpoint accepts anonymous requests.

## Request

- **Content-Type:** `multipart/form-data`
- **Form field name:** `file`
- **File type:** CSV (`.csv` extension or `text/csv` / `application/vnd.ms-excel` MIME type)
- **Maximum file size:** 10 MB

## CSV Format

The CSV must include a header row. Headers are case-insensitive (matched using `OrdinalIgnoreCase`).

### Required Columns

- `Title`
- `Description`
- `CreatorUsername`

### Optional Columns

- `RoomNumber`

Each data row must have non-blank values for all required columns (Title, Description, and CreatorUsername). Blank rows (containing only whitespace) are skipped and not counted as errors.

### Character Encoding

The CSV must be UTF-8 encoded. A UTF-8 byte-order mark (BOM) is allowed and will be detected automatically.

### Quoting and Field Escaping

- Quoted fields: surround a field with double quotes (`"`) if it contains a comma, newline, or quote.
- Escaped quotes: a literal double quote inside a quoted field is represented as two consecutive double quotes (`""`).
- Multiline fields: a quoted field can span multiple physical lines in the file.

Example:
```csv
Title,Description,CreatorUsername,RoomNumber
Simple task,"A comma, here",user1,101
"Complex, title","Line one
Line two",user2,102
"Quote ""inside""","He said ""hi""",user3,
```

## Response

**Status Code:** 200 OK on success, 400 Bad Request on error

### Success Response (200 OK)

```json
{
  "createdCount": 2,
  "results": [
    {
      "lineNumber": 2,
      "success": true,
      "workOrderNumber": "WO-2024-001",
      "error": null
    },
    {
      "lineNumber": 3,
      "success": false,
      "workOrderNumber": null,
      "error": "Employee with username 'unknown-user' was not found."
    }
  ]
}
```

Each row in the CSV is processed independently. A successful import returns HTTP 200 with a detailed result for each row, indicating which rows succeeded and why any rows failed. The `createdCount` field indicates the total number of draft work orders created.

### Error Response (400 Bad Request)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "..."
}
```

The following validation errors result in an immediate 400 response with one of these messages:

- `"A non-empty CSV file is required (form field name: file)."` — No file was uploaded, or the file is empty.
- `"Upload must be a .csv file."` — The file does not have a `.csv` extension and its MIME type is not `text/csv` or `application/vnd.ms-excel`.
- `"CSV is empty."` — The CSV file contains no header row.
- `"CSV contains no data rows."` — The CSV has a header row but no data rows (blank rows are not counted).
- `"Missing required column \"{column}\". Expected header: Title, Description, CreatorUsername, RoomNumber (optional)."` — One of the required columns is missing from the header.

Per-row validation errors appear in the response body (HTTP 200):

- `"Title, Description, and CreatorUsername are required on each data row."` — A required field was blank or missing on a specific row.
- `"Employee with username '{username}' was not found."` — The creator username does not exist in the system.
- Any other exception message from the work order creation process (e.g., database or application errors).

## Example Usage

### CSV File Content

```csv
Title,Description,CreatorUsername,RoomNumber
Fix leak,Under the sink,john.doe,101
Replace tiles,Main lobby floor,jane.smith,
Paint walls,"Color: blue, walls: all",bob.johnson,204
```

### Curl Request

```bash
curl -X POST \
  -H "Content-Type: multipart/form-data" \
  -F "file=@work-orders.csv" \
  https://localhost:7174/api/work-orders/bulk-import
```

### Response Example

```json
{
  "createdCount": 3,
  "results": [
    {
      "lineNumber": 2,
      "success": true,
      "workOrderNumber": "WO-2024-001",
      "error": null
    },
    {
      "lineNumber": 3,
      "success": true,
      "workOrderNumber": "WO-2024-002",
      "error": null
    },
    {
      "lineNumber": 4,
      "success": true,
      "workOrderNumber": "WO-2024-003",
      "error": null
    }
  ]
}
```

## Notes

- All created work orders begin in **Draft** status and can be assigned or completed later.
- The `RoomNumber` field is optional and may be left blank on a per-row basis.
- Creator usernames are validated against existing employees in the system.
- Rate limiting applies to this endpoint via the configured API rate-limit policy.

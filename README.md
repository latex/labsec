# labsec

Security lab tool for executing security tasks including SAST, DAST scans, CVE verification, and security recommendations.

## Features

### Security Scanning
- **SAST (Static Application Security Testing)**: Analyzes source code for security vulnerabilities
- **DAST (Dynamic Application Security Testing)**: Tests running applications for security issues  
- **CVE Verification**: Checks for known vulnerabilities in technologies and dependencies
- **Security Recommendations**: Provides prioritized actionable recommendations

### CVE Management
- Sync and cache CVE data from multiple sources
- Query and filter CVEs with advanced options
- List available CVE data sources

### Directives Management
- Add, list, update, and delete configuration directives

## Installation

```bash
dotnet build
```

## Usage

### Security Scanning

#### Run all security scans
```bash
omama-cli scan --target <path|url|keyword> [--format <text|json|md>]
```

#### Run SAST scan on source code
```bash
omama-cli scan sast --target /path/to/code
```

The SAST scanner detects:
- SQL Injection vulnerabilities
- Hardcoded credentials
- Weak cryptography (DES, MD5)
- Cross-Site Scripting (XSS) risks
- Insecure deserialization

#### Run DAST scan on web application
```bash
omama-cli scan dast --target https://example.com
```

The DAST scanner checks for:
- Missing security headers (CSP, HSTS, X-Frame-Options, etc.)
- Unencrypted HTTP connections
- Server information disclosure

#### Run CVE scan
```bash
omama-cli scan cve --target "spring framework"
```

Searches for known CVEs related to the specified technology or keyword.

#### Output formats
- `--format text`: Human-readable text output (default)
- `--format json`: Machine-readable JSON output
- `--format md`: Markdown formatted output

### CVE Management

#### Sync CVE data
```bash
omama-cli sync [--keyword <keyword> | --latest <n>] [--force] [--json]
```

#### List synced CVEs with filters
```bash
omama-cli list cves [--q <text>] [--since <yyyy-MM-dd>] [--severity <comma>] [--minScore <n>] [--format <text|json|md>]
```

#### List CVE data sources
```bash
omama-cli list source [--json]
```

### Directives

#### Add directive
```bash
omama-cli add --name <name> --description <desc> --value <value>
```

#### List directives
```bash
omama-cli list
```

#### Update directive
```bash
omama-cli update --name <name> --value <new-value>
```

#### Delete directive
```bash
omama-cli delete --name <name>
```

## Examples

### Example 1: Full security scan
```bash
omama-cli scan --target ./my-project --format md > security-report.md
```

### Example 2: Check website security headers
```bash
omama-cli scan dast --target https://myapp.com
```

### Example 3: Find CVEs for a technology
```bash
omama-cli scan cve --target "log4j" --format json
```

## Architecture

The tool consists of modular scanners that implement the `ISecurityScanner` interface:
- `SastScanner`: Static code analysis
- `DastScanner`: Dynamic application testing
- `CveScanner`: CVE database queries

The `SecurityScanService` orchestrates scans and generates prioritized security recommendations based on severity levels (Critical, High, Medium, Low, Info).

## Development

### Run tests
```bash
dotnet test
```

### Build
```bash
dotnet build
```

## License

See LICENSE file for details.

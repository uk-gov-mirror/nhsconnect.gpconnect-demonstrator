# GPC Ping Service

## Overview

GPC Ping is a lightweight ASP.NET Core service designed to validate JWT tokens and provide diagnostic information. It
serves as a simple validation endpoint that can be used to test connectivity and authentication within a GP Connect
ecosystem.

## Features

- **JWT Token Validation**: Accepts and validates JWT tokens provided in the Authorization header
    - Informs the user which claims are valid and which are not (with brief reason as to why)
- **Docker Support**: Includes Docker configuration for containerized deployment
-

## Prerequisites

- .NET 9.0 SDK or higher
- Docker (optional, for containerized deployment)

## Getting Started

### Running Locally

1. Clone the repository pointing at branch `gpc-ping`

```bash
git clone -b gpc-ping https://github.com/nhsconnect/gpconnect-demonstrator.git
```

2. Navigate to the project directory

```bash
 cd .\gpconnect-demonstrator\gpc-ping
```

3. Run the application:

```bash
dotnet run
```

4. The service will be available at:
    - HTTP: `http://localhost:5005/gpc-ping`
    - HTTPS: `https://localhost:7187/gpc-ping`

### Docker Deployment

1. Build the Docker image:

``` bash
docker build -t gpc-ping .
```

2. Run the container:

``` bash
docker run -p 8080:8080 -p 8081:8081 gpc-ping
```

## Usage

**Query Parameters:**

- _version_ : v0.7.4, v1.2.7, v1.5.0, v1.6.0 are valid values currently

### Testing the Endpoint

You can test the endpoint using curl:

```bash
curl --location 'http://localhost:5005/gpc-ping?version=v0.7.4' \
--header 'Authorization: Bearer eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJpc3MiOiJodHRwczovL0NvbnN1bWVyU3lzdGVtVVJMIiwic3ViIjoiMSIsImF1ZCI6Imh0dHBzOi8vYXV0aG9yaXplLmZoaXIubmhzLm5ldC90b2tlbi8iLCJleHAiOjE3NDc3MzE1MjcsImlhdCI6MTc0NzczMTIyNywicmVhc29uX2Zvcl9yZXF1ZXN0IjoiZGlyZWN0Y2FyZSIsInJlcXVlc3RpbmdfZGV2aWNlIjp7InJlc291cmNlVHlwZSI6IkRldmljZSIsImlkZW50aWZpZXIiOlt7InN5c3RlbSI6IkdQQ29ubmVjdFRlc3RTeXN0ZW0iLCJ2YWx1ZSI6IkNsaWVudCJ9XSwibW9kZWwiOiJ2MSIsInZlcnNpb24iOiIxLjEifSwicmVxdWVzdGluZ19vcmdhbml6YXRpb24iOnsicmVzb3VyY2VUeXBlIjoiT3JnYW5pemF0aW9uIiwiaWRlbnRpZmllciI6W3sic3lzdGVtIjoiaHR0cHM6Ly9maGlyLm5ocy51ay9JZC9vZHMtb3JnYW5pemF0aW9uLWNvZGUiLCJ2YWx1ZSI6IkdQQ0EwMDAxIn1dLCJuYW1lIjoiR1AgQ29ubmVjdCBBc3N1cmFuY2UifSwicmVxdWVzdGluZ19wcmFjdGl0aW9uZXIiOnsicmVzb3VyY2VUeXBlIjoiUHJhY3RpdGlvbmVyIiwiaWQiOiIxIiwiaWRlbnRpZmllciI6W3sic3lzdGVtIjoiaHR0cHM6Ly9maGlyLm5ocy51ay9JZC9zZHMtdXNlci1pZCIsInZhbHVlIjoiR0NBU0RTMDAwMSJ9LHsic3lzdGVtIjoiaHR0cHM6Ly9maGlyLm5ocy51ay9JZC9zZHMtcm9sZS1wcm9maWxlLWlkIiwidmFsdWUiOiIxMTIyMzM0NDU1NjYifSx7InN5c3RlbSI6Imh0dHBzOi8vY29uc3VtZXJzdXBwbGllci5jb20vSWQvdXNlci1ndWlkIiwidmFsdWUiOiI5OGVkNGY3OC04MTRkLTQyNjYtOGQ1Yi1jZGU3NDJmMzA5M2MifV0sIm5hbWUiOlt7ImZhbWlseSI6IkFzc3VyYW5jZVByYWN0aXRpb25lciIsImdpdmVuIjpbIkFzc3VyYW5jZVRlc3QiXSwicHJlZml4IjpbIk1yIl19XX0sInJlcXVlc3RlZF9zY29wZSI6InBhdGllbnQvKi5yZWFkIn0.'
```

or by using a HTTP Client such as `Postman` creating a new http/https request to the relevant endpoint, including a
`version` query parameter. Ensuring you pass in a bearer token in the Authorization header


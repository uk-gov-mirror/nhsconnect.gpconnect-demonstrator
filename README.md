# GP Connect Demonstrator — WireMock

This project provides mapping files and test data for the GP Connect demonstrator, packaged with WireMock 3.13.1 in a Docker image. It replaces the previous Postman-based mock server with a lightweight, containerised solution.

## Prerequisites

Before you begin, ensure you have the following installed on your system:

- **Docker**
  - **Windows & macOS**: Install [Docker Desktop](https://docs.docker.com/desktop/) and follow the setup instructions.
  - **Ubuntu**:
    ```bash
    sudo apt-get update
    sudo apt-get install -y docker.io
    sudo systemctl enable --now docker
    ```

## Getting the WireMock Image

1. **Clone the repository** and switch to the `demonstrator-wiremock` branch:

   ```bash
   git clone https://github.com/nhsconnect/gpconnect-demonstrator.git
   cd gpconnect-demonstrator
   git checkout demonstrator-wiremock
   ```

2. **Locate the Docker image archive** in the repository root. The file is named:

   ```
   demonstrator-data-<VERSION>.tar
   ```

   Replace `<VERSION>` (e.g., `v0.7.4`) with the version you want to test against.

3. **Load the Docker image** into your local Docker registry:

   ```bash
   docker load -i demonstrator-data-<VERSION>.tar
   ```

## Running the Mock Server

Launch the WireMock container with the following command:

```bash
docker run --rm -it -p 8080:8080 demonstrator-data-<VERSION> --verbose --enable-browser
```

### Parameter Breakdown

| Parameter                   | Description                                                                        |
| --------------------------- | ---------------------------------------------------------------------------------- |
| `--rm`                      | Automatically remove the container once it stops.                                  |
| `-it`                       | Allocate a pseudo-TTY and keep STDIN open, allowing interactive logs.              |
| `-p 8080:8080`              | Map port 8080 in the container to port 8080 on the host, exposing the mock server. |
| `demonstrator-data-VERSION` | Docker image name and tag (matches the loaded archive).                            |
| `--verbose`                 | Enable detailed logging output from WireMock.                                      |
| `--enable-browser`          | Activate the WireMock browser-based UI for exploring mappings and requests.        |

### Additional WireMock CLI Options

You can pass any standard WireMock CLI parameter to customise behaviour:

- `--root-dir <DIR>`: Specify a custom directory for `mappings/` and `__files/`.
- `--port <PORT>`: Change the listening port (default is `8080`).
- `--global-response-templating`: Enable Handlebars templating for all responses.
- `--async-response-enabled`: Allow asynchronous, delayed responses.
- `--disable-content-cache`: Turn off response content caching.

For a full list of supported options, see the [WireMock CLI documentation](http://wiremock.org/docs/running-standalone/).

## Testing the Mock Server

Once running, you can test endpoints using `curl`, Postman, or your application:

- **List all mappings**:

  ```bash
  curl http://localhost:8080/__admin/mappings
  ```

- **Browse via UI**: Navigate to [http://localhost:8080/\_\_admin/browser/](http://localhost:8080/__admin/browser/) in your web browser.

- **Hit a mocked endpoint**:

  ```bash
  curl http://localhost:8080/Patient/12345
  ```

  (Adjust the path to match any of the mappings in `mappings/`.)

## Contributing

Contributions, issues, and feature requests are welcome! Please:

1. Fork the repository.
2. Create a new branch (e.g., `feature/my-mock-update`).
3. Commit your changes and push the branch.
4. Open a Pull Request against `demonstrator-wiremock`.

---

*This README replaces the previous Postman-based instructions. For legacy documentation, see the **`postman-replacement`** branch.*


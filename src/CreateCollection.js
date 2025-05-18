// CreateCollection.js
// Script to create a Postman collection from test data

const sdk = require('postman-collection');
const fs = require('fs');
const path = require('path');

/**
 * Reads the NHSNoMap.csv file and creates a mapping of NHS numbers to patient identifiers
 * @returns {Map} - Map of NHS numbers to patient identifiers
 */
function readNHSNoMap() {
    const nhsNoMapPath = 'NHSNoMap.csv';
    console.log(`Reading NHS number mapping from: ${nhsNoMapPath}`);

    try {
        const nhsNoMapData = fs.readFileSync(nhsNoMapPath, 'utf8');
        const lines = nhsNoMapData.split('\n');

        // Create a map of NHS numbers to patient identifiers
        const nhsNoMap = new Map();
        // Create an array to store the raw mappings for collection variables
        const nhsMappings = [];

        // Skip the header line
        for (let i = 1; i < lines.length; i++) {
            const line = lines[i].trim();
            if (line) {
                const [patientId, nhsNo] = line.split(',');
                nhsNoMap.set(nhsNo, `{{${patientId}}}`);

                // Add the mapping to the array for collection variables
                nhsMappings.push({
                    patientId,
                    nhsNo
                });
            }
        }

        console.log(`Loaded ${nhsNoMap.size} NHS number mappings`);
        return { nhsNoMap, nhsMappings };
    } catch (error) {
        console.error('Error reading NHS number mapping:', error);
        return { nhsNoMap: new Map(), nhsMappings: [] };
    }
}

/**
 * Creates a Postman collection from test data
 * @param {string} testDataFilePath - Path to the test data file (optional)
 */
function createCollection(testDataFilePath) {
    // Set default file path if not provided
    const filePath = testDataFilePath || 'ExampleData/output/gpcAcceptanceTestData.passed.json';

    console.log(`Reading test data from: ${filePath}`);

    try {
        // Read NHS number mapping
        const { nhsNoMap, nhsMappings } = readNHSNoMap();

        // Read and parse the test data file
        const testData = JSON.parse(fs.readFileSync(filePath, 'utf8'));

        // Create collection variables from NHS mappings
        const collectionVariables = nhsMappings.map(mapping => ({
            key: mapping.patientId,
            value: mapping.nhsNo,
            type: "string"
        }));

        // Create a new collection
        const collection = new sdk.Collection({
            info: {
                name: "GpcAcceptTestEndpoints"
            },
            item: [],
            variable: collectionVariables
        });

        // Map to store folders by name to avoid duplicates
        const folderMap = new Map();

        // Loop through all objects in the test data
        testData.forEach(testItem => {
            if (!testItem.requestName) {
                console.warn('Test item missing requestName, skipping...');
                return;
            }

            // Split requestName to get folder structure
            // Example: "Patient/$gpc.getstructuredrecord" -> ["Patient", "$gpc.getstructuredrecord"]
            const pathParts = testItem.requestName.split('/');

            // Create or get main folder
            let mainFolder;
            if (folderMap.has(pathParts[0])) {
                mainFolder = folderMap.get(pathParts[0]);
            } else {
                mainFolder = new sdk.ItemGroup({
                    name: pathParts[0],
                    item: []
                });
                folderMap.set(pathParts[0], mainFolder);
                collection.items.add(mainFolder);
            }

            // If there's a subfolder path
            if (pathParts.length > 1) {
                // Create subfolder
                const subFolderName = pathParts.slice(1).join('/');
                let subFolder;

                // Check if subfolder already exists in main folder
                const existingSubFolder = mainFolder.items.find(item => item.name === subFolderName);

                if (existingSubFolder) {
                    subFolder = existingSubFolder;
                } else {
                    subFolder = new sdk.ItemGroup({
                        name: subFolderName,
                        item: []
                    });
                    mainFolder.items.add(subFolder);
                }

                // Create request item
                const requestItem = createRequestItem(testItem, nhsNoMap);
                subFolder.items.add(requestItem);
            } else {
                // No subfolder, add request directly to main folder
                const requestItem = createRequestItem(testItem, nhsNoMap);
                mainFolder.items.add(requestItem);
            }
        });

        // Write the collection to a file
        const outputDir = 'output';
        if (!fs.existsSync(outputDir)) {
            fs.mkdirSync(outputDir);
        }

        const fileName = path.join(outputDir, 'GpcAcceptTestEndpoints.json');
        const collectionJson = JSON.stringify(collection.toJSON(), null, 2);

        fs.writeFileSync(fileName, collectionJson);
        console.log(`Collection written to file: ${fileName}`);

        return collection;
    } catch (error) {
        console.error('Error creating collection:', error);
        throw error;
    }
}

/**
 * Replaces NHS numbers with patient identifiers in a string
 * @param {string} value - The string that might contain NHS numbers
 * @param {Map} nhsNoMap - Map of NHS numbers to patient identifiers
 * @returns {string} - The string with NHS numbers replaced with patient identifiers
 */
function replaceNHSNumbers(value, nhsNoMap) {
    if (!value || typeof value !== 'string' || nhsNoMap.size === 0) {
        return value;
    }

    // Look for patterns like "https://fhir.nhs.uk/Id/nhs-number|9690937375"
    return value.replace(/https:\/\/fhir\.nhs\.uk\/Id\/nhs-number\|(\d+)/g, (match, nhsNumber) => {
        const patientId = nhsNoMap.get(nhsNumber);
        if (patientId) {
            console.log(`Replacing NHS number ${nhsNumber} with ${patientId}`);
            return `https://fhir.nhs.uk/Id/nhs-number|${patientId}`;
        }
        return match;
    });
}

/**
 * Creates a Postman request item from test data
 * @param {Object} testItem - Test data item
 * @param {Map} nhsNoMap - Map of NHS numbers to patient identifiers
 * @returns {Object} - Postman request item
 */
function createRequestItem(testItem, nhsNoMap) {
    const requestData = testItem.data.request;
    const responseData = testItem.data.response;

    // Create headers
    const headers = [];
    if (requestData.headers) {
        requestData.headers.forEach(header => {
            // If the header is Ssp-TraceID, replace its value with {{$guid}}
            if (header.name === 'Ssp-TraceID') {
                headers.push({
                    key: header.name,
                    value: '{{$guid}}'
                });
            } else {
                // Replace NHS numbers in header values
                const replacedValue = replaceNHSNumbers(header.value, nhsNoMap);
                headers.push({
                    key: header.name,
                    value: replacedValue
                });
            }
        });
    }

    // Create URL parameters
    const queryParams = [];
    if (requestData.parameters) {
        if (Array.isArray(requestData.parameters)) {
            requestData.parameters.forEach(param => {
                // Replace NHS numbers in parameter values
                const replacedValue = replaceNHSNumbers(param.value, nhsNoMap);
                queryParams.push({
                    key: param.name,
                    value: replacedValue
                });
            });
        }
    }

    // Create request body
    let body = null;
    if (requestData.body && requestData.body.value) {
        // Replace NHS numbers in request body
        const replacedBody = replaceNHSNumbers(requestData.body.value, nhsNoMap);
        body = {
            mode: 'raw',
            raw: replacedBody
        };
    }

    // Create URL
    // Replace NHS numbers in the endpoint URL
    const replacedEndpointUrl = replaceNHSNumbers(testItem.data.endpointUrl, nhsNoMap);
    const url = {
        raw: replacedEndpointUrl,
        host: [replacedEndpointUrl.replace(/^https?:\/\//, '').split('/')[0]],
        path: replacedEndpointUrl.replace(/^https?:\/\/[^/]+\//, '').split('/')
    };

    // Add query parameters to URL if they exist
    if (queryParams.length > 0) {
        url.query = queryParams;
    }

    // Create response
    // Replace NHS numbers in response headers
    const responseHeaders = responseData.headers ? responseData.headers.map(h => ({
        key: h.name,
        value: replaceNHSNumbers(h.value, nhsNoMap)
    })) : [];

    // Replace NHS numbers in response body
    const responseBody = replaceNHSNumbers(responseData.body || "", nhsNoMap);

    const exampleResponse = new sdk.Response({
        name: "Example Response",
        code: responseData.statusCode,
        status: responseData.statusCode === "200" ? "OK" : "Error",
        header: responseHeaders,
        body: responseBody
    });

    // Create request item
    return new sdk.Item({
        name: testItem.dir || testItem.requestName,
        request: {
            url: url,
            method: requestData.method,
            header: headers,
            body: body
        },
        response: [exampleResponse]
    });
}

// If this script is run directly (not imported)
if (require.main === module) {
    // Check if a file path was provided as a command line argument
    const testDataFilePath = process.argv[2];
    createCollection(testDataFilePath);
}

// Export the function for use in other modules
module.exports = { createCollection };

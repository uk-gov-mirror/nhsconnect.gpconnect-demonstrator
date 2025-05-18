// CreateCollection.js
// Script to create a Postman collection from test data

const sdk = require('postman-collection');
const fs = require('fs');
const path = require('path');

/**
 * Creates a Postman collection from test data
 * @param {string} testDataFilePath - Path to the test data file (optional)
 */
function createCollection(testDataFilePath) {
    // Set default file path if not provided
    const filePath = testDataFilePath || 'ExampleData/output/gpcAcceptanceTestData.passed.json';
    
    console.log(`Reading test data from: ${filePath}`);
    
    try {
        // Read and parse the test data file
        const testData = JSON.parse(fs.readFileSync(filePath, 'utf8'));
        
        // Create a new collection
        const collection = new sdk.Collection({
            info: {
                name: "GpcAcceptTestEndpoints"
            },
            item: []
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
                const requestItem = createRequestItem(testItem);
                subFolder.items.add(requestItem);
            } else {
                // No subfolder, add request directly to main folder
                const requestItem = createRequestItem(testItem);
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
 * Creates a Postman request item from test data
 * @param {Object} testItem - Test data item
 * @returns {Object} - Postman request item
 */
function createRequestItem(testItem) {
    const requestData = testItem.data.request;
    const responseData = testItem.data.response;
    
    // Create headers
    const headers = [];
    if (requestData.headers) {
        requestData.headers.forEach(header => {
            headers.push({
                key: header.name,
                value: header.value
            });
        });
    }
    
    // Create URL parameters
    const queryParams = [];
    if (requestData.parameters) {
        if (Array.isArray(requestData.parameters)) {
            requestData.parameters.forEach(param => {
                queryParams.push({
                    key: param.name,
                    value: param.value
                });
            });
        }
    }
    
    // Create request body
    let body = null;
    if (requestData.body && requestData.body.value) {
        body = {
            mode: 'raw',
            raw: requestData.body.value
        };
    }
    
    // Create URL
    const url = {
        raw: testItem.data.endpointUrl,
        host: [testItem.data.endpointUrl.replace(/^https?:\/\//, '').split('/')[0]],
        path: testItem.data.endpointUrl.replace(/^https?:\/\/[^/]+\//, '').split('/')
    };
    
    // Add query parameters to URL if they exist
    if (queryParams.length > 0) {
        url.query = queryParams;
    }
    
    // Create response
    const exampleResponse = new sdk.Response({
        name: "Example Response",
        code: responseData.statusCode,
        status: responseData.statusCode === "200" ? "OK" : "Error",
        header: responseData.headers ? responseData.headers.map(h => ({
            key: h.name,
            value: h.value
        })) : [],
        body: responseData.body || ""
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

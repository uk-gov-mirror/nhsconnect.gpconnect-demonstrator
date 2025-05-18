const fs = require('fs').promises;
const path = require('path');
const { existsSync, readFileSync } = require('fs');

// Function to extract scenario name from directory name by removing hyphen and number at the end
function extractScenarioName(dirName) {
    return dirName.replace(/-\d+$/, '');
}

// Function to determine test result from TestRunLog.txt
function getTestResult(scenarioName) {
    try {
        const testRunLogPath = path.join(__dirname, 'ExampleData', 'TestRunLog.txt');
        if (!existsSync(testRunLogPath)) return 'Undetermined';

        const content = readFileSync(testRunLogPath, 'utf8');
        const lines = content.split('\n');

        for (const line of lines) {
            // Look for lines that contain the scenario name followed by parameters and Pass/Fail
            if (line.includes(scenarioName)) {
                if (line.endsWith(',Pass')) {
                    return 'Passed';
                } else if (line.endsWith(',Fail')) {
                    return 'Failed';
                }
            }
        }

        return 'Undetermined';
    } catch (error) {
        console.error('Error determining test result:', error);
        return 'Undetermined';
    }
}

// Function to extract failure reason from TestRunLog.txt
function getFailureReason(scenarioName) {
    try {
        const testRunLogPath = path.join(__dirname, 'ExampleData', 'TestRunLog.txt');
        if (!existsSync(testRunLogPath)) return null;

        const content = readFileSync(testRunLogPath, 'utf8');
        const lines = content.split('\n');

        let failureLineIndex = -1;

        // Find the line with the scenario name that ends with ",Fail"
        for (let i = 0; i < lines.length; i++) {
            if (lines[i].includes(scenarioName) && lines[i].endsWith(',Fail')) {
                failureLineIndex = i;
                break;
            }
        }

        if (failureLineIndex === -1) return null;

        // Find the next line with hyphens
        let nextHyphenLineIndex = -1;
        for (let i = failureLineIndex + 1; i < lines.length; i++) {
            if (lines[i].startsWith('----')) {
                nextHyphenLineIndex = i;
                break;
            }
        }

        if (nextHyphenLineIndex === -1) return null;

        // Extract the text between the failure line and the next hyphen line
        const failureReason = lines.slice(failureLineIndex + 1, nextHyphenLineIndex).join('\n').trim();
        return failureReason.length > 0 ? failureReason : null;
    } catch (error) {
        console.error('Error extracting failure reason:', error);
        return null;
    }
}

// Function to extract value between XML tags
function extractXmlValue(content, tagName) {
    const regex = new RegExp(`<${tagName}>(.*?)<\/${tagName}>`, 's');
    const match = content.match(regex);
    return match ? match[1] : null;
}

// Function to decode HTML entities
function decodeHtmlEntities(text) {
    return text
        .replace(/&amp;quot;/g, '"')
        .replace(/&amp;lt;/g, '<')
        .replace(/&amp;gt;/g, '>')
        .replace(/&amp;amp;/g, '&')
        .replace(/&amp;#39;/g, "'");
}

// Function to extract NHS number from request body for Patient/$gpc.getstructuredrecord
function extractNhsNumber(requestBody) {
    try {
        // Decode HTML entities in the request body
        const decodedBody = decodeHtmlEntities(requestBody);

        // Parse the JSON
        const parsedBody = JSON.parse(decodedBody);

        // Find the parameter with name "patientNHSNumber"
        const patientParam = parsedBody.parameter.find(param => param.name === "patientNHSNumber");

        // Extract the NHS number
        if (patientParam && patientParam.valueIdentifier && patientParam.valueIdentifier.value) {
            return patientParam.valueIdentifier.value;
        }
    } catch (error) {
        console.error('Error extracting NHS number:', error);
    }

    return null;
}

// Function to extract query parameters from ConsoleLog.txt
function extractQueryParameters(dirPath) {
    try {
        const consoleLogPath = path.join(dirPath, 'ConsoleLog.txt');
        if (!existsSync(consoleLogPath)) return {};

        const content = readFileSync(consoleLogPath, 'utf8');
        const lines = content.split('\n');

        const queryParameters = {};
        const headerKeys = new Set();

        // First, collect all header keys to avoid duplicating parameters that are already in headers
        for (const line of lines) {
            if (line.startsWith('Header - ')) {
                const headerMatch = line.match(/Header - (.*?) ->/);
                if (headerMatch) {
                    headerKeys.add(headerMatch[1]);
                }
            }
        }

        // Then extract parameters from "Added Key" lines
        for (const line of lines) {
            if (line.startsWith('Added Key=')) {
                // The format in ConsoleLog.txt is: Added Key='key' Value='value'
                const match = line.match(/Added Key='(.*?)' Value='(.*?)'/);
                if (match) {
                    const key = match[1];
                    const value = match[2];

                    // Only add if not already in headers
                    if (!headerKeys.has(key)) {
                        queryParameters[key] = value;
                    }
                }
            }
        }

        return queryParameters;
    } catch (error) {
        console.error('Error extracting query parameters:', error);
        return {};
    }
}

// Function to parse XML to JSON
function parseXmlToJson(xmlContent) {
    // This is a simplified XML parser for this specific use case
    // It extracts key elements from the HttpContext.xml structure
    const result = {
        request: {
        },
        response: {},
        httpcontext: {}
    };

    // Extract request information
    const requestMatch = xmlContent.match(/<request([^>]*)>(.*?)<\/request>/s);
    if (requestMatch) {
        const requestAttributes = requestMatch[1];
        const requestContent = requestMatch[2];

        // Extract endpointUrl from request attributes
        const endpointUrlMatch = requestAttributes.match(/endpointUrl="([^"]*)"/);
        if (endpointUrlMatch) {
            result.endpointUrl = endpointUrlMatch[1];
        }

        // Extract request headers
        result.request.headers = [];
        const headerMatches = requestContent.matchAll(/<requestHeader name="(.*?)" value="(.*?)" \/>/g);
        for (const match of headerMatches) {
            result.request.headers.push({
                name: match[1],
                value: match[2]
            });
        }

        // Keep method, contentType, body, and parameters at the request level for backward compatibility
        result.request.method = extractXmlValue(requestContent, 'requestMethod');
        result.request.contentType = extractXmlValue(requestContent, 'requestContentType');
        result.request.body = extractXmlValue(requestContent, 'requestBody');

        // Extract request parameters
        result.request.parameters = [];
        const parametersMatch = requestContent.match(/<requestParameters>(.*?)<\/requestParameters>/s);
        if (parametersMatch) {
            const parametersContent = parametersMatch[1];
            const paramMatches = parametersContent.matchAll(/<requestParameter name="(.*?)" value="(.*?)" \/>/g);
            for (const match of paramMatches) {
                result.request.parameters.push({
                    name: match[1],
                    value: match[2]
                });
            }
        } else {
            // For backward compatibility, keep the old format
            result.request.parameters = extractXmlValue(requestContent, 'requestParameters') || '';
        }
    }

    // Extract response information
    const responseMatch = xmlContent.match(/<response>(.*?)<\/response>/s);
    if (responseMatch) {
        const responseContent = responseMatch[1];

        // Extract response properties
        result.response.contentType = extractXmlValue(responseContent, 'responseContentType');
        result.response.statusCode = extractXmlValue(responseContent, 'responseStatusCode');
        result.response.timeInMilliseconds = extractXmlValue(responseContent, 'responseTimeInMilliseconds');
        result.response.timeAcceptable = extractXmlValue(responseContent, 'responseTimeAcceptable');

        // Extract response headers
        result.response.headers = [];
        const headerMatches = responseContent.matchAll(/<responseHeader name="(.*?)" value="(.*?)" \/>/g);
        for (const match of headerMatches) {
            result.response.headers.push({
                name: match[1],
                value: match[2]
            });
        }

        // Extract response body
        result.response.body = extractXmlValue(responseContent, 'responseBody');
    }

    // Extract httpContext attributes
    const httpContextMatch = xmlContent.match(/<httpContext([^>]*)>/);
    if (httpContextMatch) {
        const attributes = httpContextMatch[1];

        const attrMatches = attributes.matchAll(/(\w+)="([^"]*)"/g);
        for (const match of attrMatches) {
            result.httpcontext[match[1]] = match[2];
        }
    }

    return result;
}

async function processExampleData() {
    const exampleDataDir = path.join(__dirname, 'ExampleData');
    const results = [];

    try {
        // Get all subdirectories in the ExampleData folder
        const directories = await fs.readdir(exampleDataDir);

        for (const dir of directories) {
            const dirPath = path.join(exampleDataDir, dir);

            // Check if it's a directory
            const stats = await fs.stat(dirPath);
            if (!stats.isDirectory()) continue;

            // Check if HttpContext.xml exists in this directory
            const httpContextPath = path.join(dirPath, 'HttpContext.xml');
            if (!existsSync(httpContextPath)) continue;

            // Read and parse the HttpContext.xml file
            let xmlContent = await fs.readFile(httpContextPath, 'utf8');

            // Extract query parameters from ConsoleLog.txt
            const queryParameters = extractQueryParameters(dirPath);

            // Update the HttpContext.xml file with the extracted query parameters
            if (Object.keys(queryParameters).length > 0) {
                // Create XML for query parameters
                const queryParamsXml = Object.entries(queryParameters)
                    .map(([key, value]) => `      <requestParameter name="${key}" value="${value}" />`)
                    .join('\n');

                // Replace empty requestParameters tag with populated one
                xmlContent = xmlContent.replace(
                    /<requestParameters\s*\/>/,
                    `<requestParameters>\n${queryParamsXml}\n    </requestParameters>`
                );

                // Write the updated XML back to the file
                await fs.writeFile(httpContextPath, xmlContent, 'utf8');
            }

            // Extract the requestUrl
            const requestUrlMatch = xmlContent.match(/<requestUrl>(.*?)<\/requestUrl>/);
            if (!requestUrlMatch) continue;

            const requestUrl = requestUrlMatch[1];

            // Parse the XML content to JSON
            const data = parseXmlToJson(xmlContent);

            // Check if this is a Patient/$gpc.getstructuredrecord request
            let name = requestUrl;
            if (requestUrl === "Patient/$gpc.getstructuredrecord" && data.requestBody) {
                // Extract NHS number from request body
                const nhsNumber = extractNhsNumber(data.requestBody);
                if (nhsNumber) {
                    // Append NHS number to name
                    name = `${requestUrl} ${nhsNumber}`;
                }
            }

            // Extract scenario name and determine test result
            const scenarioName = extractScenarioName(dir);
            const testResult = getTestResult(scenarioName);

            // Create the result object
            const resultObj = {
                name,
                dir,
                testResult,
                data
            };

            // If test failed, add the reason for failure
            if (testResult === 'Failed') {
                const reasonFailed = getFailureReason(scenarioName);
                if (reasonFailed) {
                    resultObj.reasonFailed = reasonFailed;
                }
            }

            results.push(resultObj);
        }

        // Log the results
        console.log(JSON.stringify(results, null, 2));

        // Create an output directory if it doesn't exist
        const outputDir = path.join(__dirname, 'output');
        if (!existsSync(outputDir)) {
            await fs.mkdir(outputDir, { recursive: true });
        }

        // Write results to a JSON file
        const outputFile = path.join(outputDir, 'exampleData.json');
        await fs.writeFile(outputFile, JSON.stringify(results, null, 2));
        console.log(`Results written to ${outputFile}`);

    } catch (error) {
        console.error('Error processing example data:', error);
    }
}

// Run the function
processExampleData();

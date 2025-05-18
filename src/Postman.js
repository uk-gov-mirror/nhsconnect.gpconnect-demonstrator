// Example usage of a postman-collection package
const sdk = require('postman-collection');
const fs = require('fs');

// Create a new collection
const myCollection = new sdk.Collection({
    info: {
        name: "Sample Collection"
    },
    item: [],

});

// Create a new request
const exampleResponse = new sdk.Response({
    name: "Example Response",
    code: 200,
    status: "OK",
    header: [
        {
            key: "Content-Type",
            value: "application/json; charset=utf-8"
        }
    ],
    body: JSON.stringify({
        args: {},
        headers: {
            "x-forwarded-proto": "https",
            "host": "postman-echo.com",
            "accept": "*/*",
            "content-type": "application/json",
            "user-agent": "PostmanRuntime/7.29.0"
        },
        url: "https://postman-echo.com/get"
    })
});

// Add the request to the collection
const item = new sdk.Item({
    name: "test request",
    request: {
        url: 'https://postman-echo.com/post',
        method: 'POST',
        header: [
            {
                key: 'Content-Type',
                value: 'application/json'
            }
        ],
        body: {
            mode: 'raw',
            raw: JSON.stringify({ key: 'value' })
        }
    },
    response: [exampleResponse]
});
myCollection.items.add(item);

// Print the collection as JSON
console.log(JSON.stringify(myCollection.toJSON(), null, 2));

//write to file with filename same as myCollection.info.name
const collectionName = myCollection.name;
const fileName = `../output/${collectionName.replace(/\s+/g, '_')}.json`;
const collectionJson = JSON.stringify(myCollection.toJSON(), null, 2);

// Write the collection to a file
fs.writeFileSync(fileName, collectionJson);
console.log(`Collection written to file: ${fileName}`);

console.log('Postman collection created successfully!');

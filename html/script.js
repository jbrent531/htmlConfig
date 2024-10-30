let configData;

// Extract IP Address from the URL
const serverAddress = window.location.hostname;
console.log("Server IP Address:", serverAddress);
const fileUrl = `http://${serverAddress}:8086/ClientConfig.json`;

// Initial load and setup
fetchAndDisplayConfig();

// Fetch the JSON data
async function fetchAndDisplayConfig() {
    try {
        const response = await fetch(fileUrl);
        if (!response.ok) throw new Error(`Error: ${response.statusText}`);

        configData = await response.json();
        createMainKeyButtons(configData);
        
    } catch (error) {
        document.getElementById("configDisplay").innerText = `Failed to load configuration: ${error.message}`;
    }
}

// Create buttons for each main key in the config
function createMainKeyButtons(data) {
    const buttonsContainer = document.getElementById("buttonsContainer");
    buttonsContainer.innerHTML = "";  // Clear any existing buttons

    // Create a button for each main key
    Object.keys(data).forEach(key => {
        const button = document.createElement("button");
        button.textContent = key;
        button.onclick = function(){
            populateDropdown(key)
            console.log("Button Pressed: ", key)
        }
        buttonsContainer.appendChild(button);
    });
}

// Populate dropdown with each nested key in the selected main key
function populateDropdown(mainKey) {
    const dropdown = document.getElementById("nestedKeyDropdown");
    dropdown.innerHTML = ""; // Clear previous options

    // Check if configData is defined and contains the mainKey
    if (configData && configData[mainKey]) {
        const items = configData[mainKey]; // Access the specific key's data

        // Populate dropdown with the items from the selected main key
        items.forEach(item => {
            const option = document.createElement("option");
            option.value = item.ID; // Use a unique identifier for each item
            option.textContent = item.Name || `Item ${item.ID}`; // Display the name of the item or a fallback
            dropdown.appendChild(option);
        });

        // Set the first item to be selected if there are items
        if (items.length > 0) {
            dropdown.value = items[0].ID; // Select the first item by default
            showSection(items[0].Name, items[0]); // Immediately display the first item's data
        }

        // Add event listener for dropdown selection
        dropdown.onchange = function() {
            const selectedID = this.value;
            const selectedItem = items.find(item => item.ID == selectedID);
            if (selectedItem) {
                showSection(selectedItem.Name, selectedItem); // Pass the selected item's data to showSection
            }
        };
    } else {
        console.error(`No data found for key: ${mainKey}`);
    }
}

// Display data for the selected key only
function showSection(title, data) {
    const configDisplay = document.getElementById("configDisplay");
    configDisplay.innerHTML = "";  // Clear previous display

    const sectionContainer = document.createElement("div");
    sectionContainer.className = "config-section active";  // Make the section visible
    sectionContainer.id = `section-${title}`;

    sectionContainer.appendChild(createEditableFields(data, title));
    configDisplay.appendChild(sectionContainer);
    addInputListeners();  // Ensure listeners are added for inputs
}

// Add event listeners to inputs
function addInputListeners() {
    document.querySelectorAll("input[data-path]").forEach(input => {
        input.addEventListener("input", (event) => {
            const { path } = event.target.dataset;
            const value = event.target.value;
            updateConfigData(path, value);
        });
    });
}

// Recursive function to create editable fields
function createEditableFields(obj, parentPath) {
    const container = document.createElement("div");
    container.className = "config-section";

    //use a specific key as the section title
    if(obj.Name){
        const sectionTitle = document.createElement("h3");
        sectionTitle.textContent = obj.Name;
        container.appendChild(sectionTitle);
    }


    Object.entries(obj).forEach(([key, value]) => {
        const fieldPath = parentPath ? `${parentPath}.${key}` : key;

        if (typeof value === "object" && value !== null) {

            if (Array.isArray(value)) {
                value.forEach((item, index) => {
                    const itemPath = `${fieldPath}[${index}]`;
                    const arrayContainer = document.createElement("div");
                    arrayContainer.className = "config-section";
                    arrayContainer.appendChild(createEditableFields(item, itemPath));
                    container.appendChild(arrayContainer);
                });
            } else {
                container.appendChild(createEditableFields(value, fieldPath));
            }
        } else {
            const label = document.createElement("label");
            label.textContent = key;
            label.htmlFor = fieldPath;

            const input = document.createElement("input");
            input.type = "text";
            input.id = fieldPath;
            input.value = value;
            input.dataset.path = fieldPath;

            const fieldDiv = document.createElement("div");
            fieldDiv.appendChild(label);
            fieldDiv.appendChild(input);

            container.appendChild(fieldDiv);
        }
    });
    return container;
}

// Update configData with new values
function updateConfigData(path, newValue) {
    const keys = path.replace(/\[(\d+)\]/g, '.$1').split(".");
    let obj = configData;

    keys.slice(0, -1).forEach(key => {
        if (!obj[key]) obj[key] = isNaN(key) ? {} : [];
        obj = obj[key];
    });

    const lastKey = keys[keys.length - 1];
    obj[lastKey] = isNaN(newValue) ? newValue : Number(newValue);
}

// Save configuration data back to the server
async function saveConfig() {
    try {
        const response = await fetch('ClientConfig.json', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(configData)
        });
        
        if (!response.ok) throw new Error(`Error: ${response.statusText}`);
        alert("Configuration saved successfully!");
    } catch (error) {
        alert(`Failed to save configuration: ${error.message}`);
    }
}


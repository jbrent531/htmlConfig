const fileListUrl = 'http://10.0.0.248:8086/list-files';

// Function to load all the files in the config directory --> Returns json array of fileNames
async function loadFileList() {
    try {
        const response = await fetch(fileListUrl);
        if (!response.ok) throw new Error('Network response was not ok');
        const fileNames = await response.json();
        console.log(fileNames);
        populateFileList(fileNames);
    } catch (error) {
        console.error('Error fetching file list:', error);
    }
}

// Function to populate the file list and create buttons
async function populateFileList(fileNames) {
    const buttonContainer = document.getElementById('button-container'); 
    for (const fileName of fileNames) {
        console.log(fileName);
        createButtons(buttonContainer, fileName);   
    }
}

// Function to create buttons for each fileName
function createButtons(container, fileName) {
        const button = document.createElement('button');
        button.textContent = fileName; 
        container.appendChild(button);
}

// Load the file list on page load
loadFileList();
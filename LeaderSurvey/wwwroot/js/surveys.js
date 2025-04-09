// Global variables
let currentSurvey = null;
let surveyData = [];
let categoryData = {};
let filteredSurveys = [];
let selectedLeaderId = '';
let selectedArea = '';
let startDate = null;
let endDate = null;

// Survey Editor Functions
function createNewSurvey() {
    document.getElementById('editorTitle').textContent = 'New Survey';
    document.getElementById('surveyForm').reset();
    document.getElementById('questionsContainer').innerHTML = '';
    window.questionCount = 0;
    updateQuestionCounter();
    showSurveyEditor();
}

// Delete Survey Function
async function deleteSurvey(id, name) {
    console.log(`Delete button pressed for survey: ${name} (ID: ${id})`);
    showNotification(`Delete button pressed for survey: ${name}`, 'info');

    if (!confirm(`Are you sure you want to delete survey "${name}"? This action cannot be undone.`)) {
        return;
    }

    try {
        const response = await fetch(`/api/Surveys/${id}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        });

        let responseData;
        try {
            responseData = await response.json();
        } catch (e) {
            // If response is not JSON, create a default message
            responseData = { message: response.ok ? 'Survey deleted successfully' : 'Failed to delete survey' };
        }

        if (response.ok) {
            const row = document.querySelector(`tr[data-id="${id}"]`);
            if (row) {
                row.remove();
            }
            showNotification(responseData.message || 'Survey deleted successfully', 'success');
        } else {
            showNotification(responseData.message || 'Failed to delete survey', 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showNotification('An error occurred while deleting the survey', 'error');
    }
}

// These functions are no longer used - we now use direct links in the HTML
// Keeping the function signatures for backward compatibility
function viewSurvey(id) {
    console.log('viewSurvey is deprecated - using direct links now');
    window.location.href = `/EditSurvey?id=${id}&viewMode=true`;
}

function editSurvey(id) {
    console.log('editSurvey is deprecated - using direct links now');
    window.location.href = `/EditSurvey?id=${id}`;
}

function showSurveyViewer() {
    const viewer = document.getElementById('surveyViewer');
    if (viewer) {
        viewer.style.display = 'block';
    }
}

function closeSurveyViewer() {
    const viewer = document.getElementById('surveyViewer');
    if (viewer) {
        viewer.style.display = 'none';
    }
}

function switchTab(tabName) {
    // Update tab buttons
    document.querySelectorAll('.nav-link').forEach(tab => {
        tab.classList.remove('active');
    });
    document.getElementById(`${tabName}-tab`).classList.add('active');

    // Update tab content
    document.querySelectorAll('.tab-content').forEach(content => {
        content.classList.remove('active');
    });
    document.getElementById(`${tabName}Tab`).classList.add('active');

    if (tabName === 'preview') {
        updatePreview();
    }
}

// This function is no longer used - we now load surveys directly in the EditSurvey page
async function loadSurvey(id) {
    console.log('loadSurvey is deprecated - using direct page navigation now');
    // Redirect to the EditSurvey page instead
    window.location.href = `/EditSurvey?id=${id}`;
}

function addExistingQuestion(question, number) {
    const questionItem = createQuestionElement(number);
    questionItem.querySelector('input[type="text"]').value = question.text;
    questionItem.querySelector('select').value = question.questionType;
    document.getElementById('questionsContainer').appendChild(questionItem);
}

function createQuestionElement(number) {
    const div = document.createElement('div');
    div.className = 'question-item d-flex align-items-center mb-3';
    div.innerHTML = `
        <i class="bi bi-grip-vertical question-drag-handle"></i>
        <div class="question-number">${number}</div>
        <div class="flex-grow-1">
            <input type="text" class="form-control mb-2" placeholder="Enter your question here..." required>
            <select class="question-type-select">
                <option value="yesno">Yes/No</option>
                <option value="score">Score (0-10)</option>
            </select>
        </div>
        <button class="cfa-btn cfa-btn-sm cfa-btn-outline ms-3" onclick="deleteQuestion(this)">
            <i class="bi bi-trash"></i>
        </button>
        <div class="my-pleasure">My Pleasure!</div>
    `;
    return div;
}

function updatePreview() {
    const previewTitle = document.getElementById('previewTitle');
    const previewQuestions = document.getElementById('previewQuestions');

    if (!previewTitle || !previewQuestions) {
        console.error('Preview elements not found');
        return;
    }

    const surveyNameInput = document.getElementById('surveyName');
    previewTitle.textContent = surveyNameInput ? surveyNameInput.value || 'Survey Preview' : 'Survey Preview';

    const questionsContainer = document.getElementById('questionsContainer');
    if (!questionsContainer) {
        console.error('Questions container not found');
        return;
    }

    const questions = Array.from(questionsContainer.children);
    previewQuestions.innerHTML = questions.map((q, i) => {
        const text = q.querySelector('input[type="text"]')?.value || '';
        const type = q.querySelector('select')?.value || 'yesno';

        return `
            <div class="card mb-3 border-light shadow-sm">
                <div class="card-header bg-light">
                    <strong><i class="bi bi-question-circle me-2"></i>${text}</strong>
                </div>
                <div class="card-body">
                    ${type === 'yesno' ? `
                        <div class="btn-group w-100">
                            <button type="button" class="btn btn-outline-success w-50">Yes</button>
                            <button type="button" class="btn btn-outline-danger w-50">No</button>
                        </div>
                    ` : `
                        <input type="range" class="form-range" min="0" max="10" step="1">
                        <div class="d-flex justify-content-between">
                            <span>0</span>
                            <span>5</span>
                            <span>10</span>
                        </div>
                    `}
                </div>
            </div>
        `;
    }).join('');
}

// Category and Filter Functions
function applyFilters() {
    // Get filter values
    const leaderFilter = document.getElementById('leaderFilter');
    const areaFilter = document.getElementById('areaFilter');

    selectedLeaderId = leaderFilter.value;
    selectedArea = areaFilter.value;

    // Add visual indication for active filters
    if (selectedLeaderId) {
        leaderFilter.classList.add('active');
    } else {
        leaderFilter.classList.remove('active');
    }

    if (selectedArea) {
        areaFilter.classList.add('active');
    } else {
        areaFilter.classList.remove('active');
    }

    // Filter surveys based on selected criteria
    filteredSurveys = surveyData.filter(survey => {
        // Filter by leader
        if (selectedLeaderId && survey.leaderId != selectedLeaderId) {
            return false;
        }

        // Filter by area
        if (selectedArea && survey.area !== selectedArea) {
            return false;
        }

        // Filter by date range
        if (startDate && endDate) {
            if (!survey.monthYear) {
                return false; // Skip surveys without a date
            }

            // Parse the survey date properly
            const surveyDateParts = survey.monthYear.split('-');
            const surveyYear = parseInt(surveyDateParts[0]);
            const surveyMonth = parseInt(surveyDateParts[1]) - 1; // JavaScript months are 0-indexed
            const surveyDate = new Date(surveyYear, surveyMonth, 1);

            // Set time to beginning of day for accurate comparison
            surveyDate.setHours(0, 0, 0, 0);
            const startDateCopy = new Date(startDate);
            startDateCopy.setHours(0, 0, 0, 0);
            const endDateCopy = new Date(endDate);
            endDateCopy.setHours(23, 59, 59, 999);

            if (surveyDate < startDateCopy || surveyDate > endDateCopy) {
                return false;
            }
        }

        return true;
    });

    // Update the table with filtered surveys
    updateSurveyTable();

    // Update category statistics if a leader is selected
    if (selectedLeaderId) {
        document.getElementById('categoryStats').style.display = 'block';
        updateCategoryStatistics();
    } else {
        document.getElementById('categoryStats').style.display = 'none';
    }
}

function updateSurveyTable() {
    const tableBody = document.querySelector('#surveysTable tbody');
    if (!tableBody) return;

    // Clear the table
    tableBody.innerHTML = '';

    // Add filtered surveys to the table
    filteredSurveys.forEach(survey => {
        const row = document.createElement('tr');
        row.dataset.id = survey.id;

        // Format the date
        const date = survey.monthYear ? new Date(survey.monthYear).toLocaleDateString('en-US', { year: 'numeric', month: 'long' }) : '';

        row.innerHTML = `
            <td>${survey.name}</td>
            <td>${survey.leaderName || ''}</td>
            <td>
                <span class="badge area-badge area-${survey.area.toLowerCase()}">${survey.area}</span>
            </td>
            <td>${date}</td>
            <td>
                <span class="status-badge status-${survey.status.toLowerCase()}">
                    ${survey.status}
                </span>
            </td>
            <td>
                <a href="/TakeSurvey?surveyId=${survey.id}" class="cfa-btn cfa-btn-sm me-1" style="background-color: #28a745; color: white;">
                    <i class="bi bi-check2-square"></i> Take Survey
                </a>
                <a href="/EditSurvey?id=${survey.id}&viewMode=true" class="cfa-btn cfa-btn-sm me-1">
                    <i class="bi bi-eye"></i> View
                </a>
                <a href="/EditSurvey?id=${survey.id}" class="cfa-btn cfa-btn-sm me-1">
                    <i class="bi bi-pencil-square"></i> Edit
                </a>
                <button class="cfa-btn cfa-btn-sm cfa-btn-outline"
                        onclick="deleteSurvey(${survey.id}, '${survey.name.replace(/'/g, "\\'")}')"
                        style="border-color: #dc3545; color: #dc3545 !important;">
                    <i class="bi bi-trash"></i> Delete
                </button>
            </td>
        `;

        tableBody.appendChild(row);
    });
}

async function fetchSurveyData() {
    return new Promise((resolve, reject) => {
        try {
        // In a real application, this would be an API call
        // For now, we'll just get the data from the table
        const rows = document.querySelectorAll('#surveysTable tbody tr');
        surveyData = Array.from(rows).map(row => {
            const id = parseInt(row.dataset.id);
            const name = row.cells[0].textContent.trim();
            const leaderName = row.cells[1].textContent.trim();
            const leaderId = row.cells[1].dataset.leaderId || '';
            const area = row.cells[2].textContent.trim();
            const dateText = row.cells[3].textContent.trim();
            const status = row.cells[4].textContent.trim();

            // Parse the date
            let monthYear = null;
            if (dateText) {
                const dateParts = dateText.split(' ');
                if (dateParts.length >= 2) {
                    // Convert month name to month number (1-12)
                    const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
                    const month = monthNames.findIndex(m => m === dateParts[0]) + 1;
                    const year = parseInt(dateParts[1]);

                    if (month > 0 && !isNaN(year)) {
                        monthYear = `${year}-${month.toString().padStart(2, '0')}`;
                        console.log(`Parsed date: ${dateText} -> ${monthYear}`);
                    }
                }
            }

            return { id, name, leaderName, leaderId, area, monthYear, status };
        });

        // Initialize filtered surveys with all surveys
        filteredSurveys = [...surveyData];

        console.log('Loaded survey data:', surveyData);
            resolve(surveyData);
        } catch (error) {
            console.error('Error fetching survey data:', error);
            showNotification('Error loading survey data', 'error');
            reject(error);
        }
    });
}

async function fetchCategoryData() {
    return new Promise((resolve, reject) => {
        try {
        // In a real application, this would be an API call
        // For now, we'll just get the data from the category cards
        const categoryCards = document.querySelectorAll('.category-card');
        categoryCards.forEach(card => {
            const categoryId = parseInt(card.dataset.categoryId);
            const categoryName = card.querySelector('.category-name').textContent.trim();

            categoryData[categoryId] = {
                id: categoryId,
                name: categoryName,
                questions: [] // This would be populated from an API call
            };
        });

        console.log('Loaded category data:', categoryData);
            resolve(categoryData);
        } catch (error) {
            console.error('Error fetching category data:', error);
            showNotification('Error loading category data', 'error');
            reject(error);
        }
    });
}

function updateCategoryStatistics() {
    // This would be populated from an API call in a real application
    // For now, we'll just show some dummy data
    Object.values(categoryData).forEach(category => {
        const card = document.querySelector(`.category-card[data-category-id="${category.id}"]`);
        if (!card) return;

        // Update Yes/No statistics
        const yesnoCount = Math.floor(Math.random() * 10) + 1; // Random number between 1 and 10
        const yesPercentage = Math.floor(Math.random() * 100); // Random percentage

        card.querySelector('.yesno-count').textContent = yesnoCount;
        card.querySelector('.yesno-percentage').textContent = `${yesPercentage}%`;

        // Update Score statistics
        const scoreCount = Math.floor(Math.random() * 10) + 1; // Random number between 1 and 10
        const scoreAverage = (Math.random() * 10).toFixed(1); // Random score between 0 and 10

        card.querySelector('.score-count').textContent = scoreCount;
        card.querySelector('.score-average').textContent = scoreAverage;

        // Populate questions (in a real app, these would come from the API)
        const questionsContainer = card.querySelector('.category-questions');
        questionsContainer.innerHTML = '';

        // Add some dummy questions
        for (let i = 1; i <= 3; i++) {
            const questionType = Math.random() > 0.5 ? 'yesno' : 'score';
            const questionItem = document.createElement('div');
            questionItem.className = 'question-item';
            questionItem.innerHTML = `
                <span>Sample question ${i} for ${category.name}</span>
                <span class="question-type-badge question-type-${questionType}">
                    ${questionType === 'yesno' ? 'Yes/No' : 'Score'}
                </span>
            `;
            questionsContainer.appendChild(questionItem);
        }
    });
}

function initializeDateRangeButtons() {
    const dateRangeButtons = document.querySelectorAll('.date-range-btn');
    const customDateRange = document.getElementById('customDateRange');

    // Set initial date range to 'all' (no filtering)
    startDate = null;
    endDate = null;

    dateRangeButtons.forEach(button => {
        button.addEventListener('click', function() {
            // Remove active class from all buttons
            dateRangeButtons.forEach(btn => btn.classList.remove('active'));

            // Add active class to clicked button
            this.classList.add('active');

            const range = this.dataset.range;
            console.log('Selected date range:', range);

            // Show/hide custom date range inputs
            if (range === 'custom') {
                customDateRange.style.display = 'flex';

                // If custom inputs already have values, use them
                const startDateInput = document.getElementById('startDate');
                const endDateInput = document.getElementById('endDate');

                if (startDateInput.value && endDateInput.value) {
                    startDate = new Date(startDateInput.value);
                    endDate = new Date(endDateInput.value);
                    // Set the end date to the end of the month
                    endDate.setMonth(endDate.getMonth() + 1, 0);
                    applyFilters();
                }
            } else {
                customDateRange.style.display = 'none';

                // Set date range based on selection
                const now = new Date();
                const currentYear = now.getFullYear();
                const currentMonth = now.getMonth();

                if (range === 'all') {
                    startDate = null;
                    endDate = null;
                } else if (range === 'current-month') {
                    // First day of current month
                    startDate = new Date(currentYear, currentMonth, 1);
                    // Last day of current month
                    endDate = new Date(currentYear, currentMonth + 1, 0);
                    console.log('Current month range:', startDate, 'to', endDate);
                } else if (range === 'last-3-months') {
                    // First day of 2 months ago
                    startDate = new Date(currentYear, currentMonth - 2, 1);
                    // Last day of current month
                    endDate = new Date(currentYear, currentMonth + 1, 0);
                    console.log('Last 3 months range:', startDate, 'to', endDate);
                }

                // Apply filters
                applyFilters();
            }
        });
    });

    // Handle custom date range inputs
    const startDateInput = document.getElementById('startDate');
    const endDateInput = document.getElementById('endDate');

    // Set initial values to current month
    const now = new Date();
    const currentYear = now.getFullYear();
    const currentMonth = (now.getMonth() + 1).toString().padStart(2, '0'); // +1 because months are 0-indexed
    startDateInput.value = `${currentYear}-${currentMonth}`;
    endDateInput.value = `${currentYear}-${currentMonth}`;

    startDateInput.addEventListener('change', function() {
        if (this.value) {
            const dateParts = this.value.split('-');
            const year = parseInt(dateParts[0]);
            const month = parseInt(dateParts[1]) - 1; // JavaScript months are 0-indexed
            startDate = new Date(year, month, 1);
            console.log('Custom start date set to:', startDate);
            applyFilters();
        }
    });

    endDateInput.addEventListener('change', function() {
        if (this.value) {
            const dateParts = this.value.split('-');
            const year = parseInt(dateParts[0]);
            const month = parseInt(dateParts[1]) - 1; // JavaScript months are 0-indexed
            // Set to last day of the selected month
            endDate = new Date(year, month + 1, 0);
            console.log('Custom end date set to:', endDate);
            applyFilters();
        }
    });
}

function initializeCategoryCards() {
    const expandButtons = document.querySelectorAll('.category-expand-btn');

    expandButtons.forEach(button => {
        button.addEventListener('click', function() {
            const card = this.closest('.category-card');
            const questionsContainer = card.querySelector('.category-questions');

            // Toggle expanded class
            this.classList.toggle('expanded');

            // Toggle questions visibility
            if (questionsContainer.style.display === 'none') {
                questionsContainer.style.display = 'block';
            } else {
                questionsContainer.style.display = 'none';
            }
        });
    });
}

// Initialize modal when document is ready
document.addEventListener('DOMContentLoaded', async function() {
    initializeModal();
    initializePreviewTab();

    // Initialize survey filters and categories
    await fetchSurveyData();
    await fetchCategoryData();
    initializeDateRangeButtons();
    initializeCategoryCards();

    // Set up event listeners for filters
    document.getElementById('leaderFilter').addEventListener('change', applyFilters);
    document.getElementById('areaFilter').addEventListener('change', applyFilters);

    // Apply initial filters (if any are set in the URL)
    const urlParams = new URLSearchParams(window.location.search);
    const leaderId = urlParams.get('leaderId');
    const area = urlParams.get('area');

    if (leaderId) {
        document.getElementById('leaderFilter').value = leaderId;
    }

    if (area) {
        document.getElementById('areaFilter').value = area;
    }

    // Apply filters based on initial values
    applyFilters();

    // Log initial state
    console.log('Initial survey data loaded:', surveyData.length, 'surveys');
    console.log('Initial filtered surveys:', filteredSurveys.length, 'surveys');
});

// This function is kept for backward compatibility
// We no longer use modals for survey editing
function initializeModal() {
    // We're using the new approach with separate pages
    console.log('Using new survey editing approach with separate pages');
    return;
}

function initializePreviewTab() {
    const previewTab = document.getElementById('preview-tab');
    if (previewTab) {
        previewTab.addEventListener('click', updatePreview);
    }
}

// Notification function
function showNotification(message, type = 'info') {
    // Create notification element if it doesn't exist
    let notificationContainer = document.getElementById('notification-container');
    if (!notificationContainer) {
        notificationContainer = document.createElement('div');
        notificationContainer.id = 'notification-container';
        notificationContainer.style.position = 'fixed';
        notificationContainer.style.top = '20px';
        notificationContainer.style.right = '20px';
        notificationContainer.style.zIndex = '9999';
        document.body.appendChild(notificationContainer);
    }

    // Create the notification
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} alert-dismissible fade show`;
    notification.role = 'alert';
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    // Add to container
    notificationContainer.appendChild(notification);

    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => {
            notification.remove();
        }, 300);
    }, 5000);
}

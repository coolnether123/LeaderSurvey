// Global variables
let currentSurvey = null;
let surveyData = [];
let categoryData = {};
let filteredSurveys = [];
let selectedLeaderId = '';
let selectedArea = '';
let startDate = null;
let endDate = null;
let currentSortColumn = '';
let currentSortDirection = '';

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

// Re-enable Survey Function
async function reEnableSurvey(id, name) {
    if (!confirm(`Are you sure you want to re-enable the survey "${name}"? This will allow it to be taken again.`)) {
        return;
    }

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        const response = await fetch('?handler=ReEnable', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ id: id })
        });

        const result = await response.json();

        if (result.success) {
            showNotification('Survey re-enabled successfully', 'success');

            // Update the survey in our arrays
            const updateSurveyInArray = (surveys) => {
                const survey = surveys.find(s => s.id === id);
                if (survey) {
                    survey.status = 'Active';
                    survey.completedDate = null;
                }
            };

            updateSurveyInArray(allSurveys);
            updateSurveyInArray(filteredSurveys);

            // Update the table display
            updateSurveyTable();
        } else {
            showNotification(result.message || 'Failed to re-enable survey', 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showNotification('An error occurred while re-enabling the survey', 'error');
    }
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

    console.log('Applying filters with:', {
        leader: selectedLeaderId,
        area: selectedArea,
        startDate: startDate ? startDate.toISOString() : null,
        endDate: endDate ? endDate.toISOString() : null
    });

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
        if (startDate || endDate) {
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

            console.log(`Survey ${survey.id} date: ${surveyDate.toISOString()}`);

            // Check start date if it exists
            if (startDate) {
                const startDateCopy = new Date(startDate);
                startDateCopy.setHours(0, 0, 0, 0);

                if (surveyDate < startDateCopy) {
                    console.log(`Survey ${survey.id} filtered out by start date: ${surveyDate} < ${startDateCopy}`);
                    return false;
                }
            }

            // Check end date if it exists
            if (endDate) {
                const endDateCopy = new Date(endDate);
                endDateCopy.setHours(23, 59, 59, 999);

                if (surveyDate > endDateCopy) {
                    console.log(`Survey ${survey.id} filtered out by end date: ${surveyDate} > ${endDateCopy}`);
                    return false;
                }
            }
        }

        return true;
    });



    // Apply current sort if one is active
    if (currentSortColumn && currentSortDirection) {
        // Sort the filtered results
        filteredSurveys.sort((a, b) => {
            let valueA, valueB;

            switch (currentSortColumn) {
                case 'name':
                    valueA = a.name.toLowerCase();
                    valueB = b.name.toLowerCase();
                    break;
                case 'leader':
                    valueA = (a.leaderName || '').toLowerCase();
                    valueB = (b.leaderName || '').toLowerCase();
                    break;
                case 'area':
                    valueA = a.area.toLowerCase();
                    valueB = b.area.toLowerCase();
                    break;
                case 'date':
                    valueA = a.monthYear ? new Date(a.monthYear + '-01') : new Date(0);
                    valueB = b.monthYear ? new Date(b.monthYear + '-01') : new Date(0);
                    break;
                case 'status':
                    valueA = a.status.toLowerCase();
                    valueB = b.status.toLowerCase();

                    // If both are completed, sort by completion date
                    if (valueA === 'completed' && valueB === 'completed') {
                        const dateA = a.completedDate ? new Date(a.completedDate) : new Date(0);
                        const dateB = b.completedDate ? new Date(b.completedDate) : new Date(0);
                        return currentSortDirection === 'asc' ? dateA - dateB : dateB - dateA;
                    }
                    break;
                default:
                    return 0;
            }

            if (currentSortColumn === 'date') {
                if (currentSortDirection === 'asc') {
                    return valueA - valueB;
                } else {
                    return valueB - valueA;
                }
            }

            if (currentSortDirection === 'asc') {
                return valueA.localeCompare(valueB);
            } else {
                return valueB.localeCompare(valueA);
            }
        });
    }

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

        // Format the date properly without timezone issues
        let date = '';
        if (survey.monthYear) {
            // Parse the YYYY-MM format and create date in UTC to avoid timezone shifts
            const parts = survey.monthYear.split('-');
            if (parts.length >= 2) {
                const year = parseInt(parts[0]);
                const month = parseInt(parts[1]); // Keep as 1-12, don't subtract 1 yet

                // Create date string manually to avoid timezone conversion
                const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
                                  'July', 'August', 'September', 'October', 'November', 'December'];
                if (month >= 1 && month <= 12) {
                    date = `${monthNames[month - 1]} ${year}`;
                }
            }
        }

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
                ${survey.status.toLowerCase() === 'completed' && survey.completedDate ?
                    `<div class="completion-date">${new Date(survey.completedDate).toLocaleDateString('en-US', { timeZone: 'America/Chicago' })}</div>` :
                    ''}
            </td>
            <td>
                <a href="/TakeSurvey?surveyId=${survey.id}" class="cfa-btn cfa-btn-sm me-1 ${survey.status.toLowerCase() === 'completed' ? 'disabled' : ''}" style="background-color: #28a745; color: white;" ${survey.status.toLowerCase() === 'completed' ? 'aria-disabled="true"' : ''}>
                    <i class="bi bi-check2-square"></i> Take Survey
                </a>
                ${survey.status.toLowerCase() === 'completed' ?
                    `<button class="cfa-btn cfa-btn-sm me-1"
                            onclick="reEnableSurvey(${survey.id}, '${survey.name.replace(/'/g, "\\'")}')"
                            style="background-color: #ffc107; color: black;">
                        <i class="bi bi-arrow-clockwise"></i> Re-enable
                    </button>` : ''}
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

// Sorting Functions
function cycleSortColumn(column) {
    let direction;

    // Determine next sort direction
    if (currentSortColumn === column) {
        // Same column - cycle through: asc -> desc -> none (unsorted)
        if (currentSortDirection === 'asc') {
            direction = 'desc';
        } else if (currentSortDirection === 'desc') {
            // Clear sorting - show original order
            direction = '';
            currentSortColumn = '';
            currentSortDirection = '';
        } else {
            direction = 'asc';
        }
    } else {
        // Different column - start with ascending
        direction = 'asc';
    }

    if (direction === '') {
        // Clear sorting - reset to original order without any sorting
        clearSorting();
    } else {
        sortTable(column, direction);
    }
}

function clearSorting() {
    // Reset sort state
    currentSortColumn = '';
    currentSortDirection = '';

    // Update visual indicators
    updateSortIndicators('', '');

    // Reset to original order (by date descending as default)
    filteredSurveys.sort((a, b) => {
        const dateA = a.monthYear ? new Date(a.monthYear + '-01') : new Date(0);
        const dateB = b.monthYear ? new Date(b.monthYear + '-01') : new Date(0);
        return dateB - dateA; // Descending order
    });

    // Update the table display
    updateSurveyTable();
}

function sortTable(column, direction) {
    // Update visual indicators
    updateSortIndicators(column, direction);

    // Store current sort state
    currentSortColumn = column;
    currentSortDirection = direction;

    // Sort the filtered surveys array
    filteredSurveys.sort((a, b) => {
        let valueA, valueB;

        switch (column) {
            case 'name':
                valueA = a.name.toLowerCase();
                valueB = b.name.toLowerCase();
                break;
            case 'leader':
                valueA = (a.leaderName || '').toLowerCase();
                valueB = (b.leaderName || '').toLowerCase();
                break;
            case 'area':
                valueA = a.area.toLowerCase();
                valueB = b.area.toLowerCase();
                break;
            case 'date':
                // Parse the date for sorting
                valueA = a.monthYear ? new Date(a.monthYear + '-01') : new Date(0);
                valueB = b.monthYear ? new Date(b.monthYear + '-01') : new Date(0);
                break;
            case 'status':
                // For status, we want to sort by status first, then by completion date for completed surveys
                valueA = a.status.toLowerCase();
                valueB = b.status.toLowerCase();

                // If both are completed, sort by completion date
                if (valueA === 'completed' && valueB === 'completed') {
                    const dateA = a.completedDate ? new Date(a.completedDate) : new Date(0);
                    const dateB = b.completedDate ? new Date(b.completedDate) : new Date(0);
                    return direction === 'asc' ? dateA - dateB : dateB - dateA;
                }
                break;
            default:
                return 0;
        }

        // Handle date comparison
        if (column === 'date') {
            if (direction === 'asc') {
                return valueA - valueB;
            } else {
                return valueB - valueA;
            }
        }

        // Handle string comparison
        if (direction === 'asc') {
            return valueA.localeCompare(valueB);
        } else {
            return valueB.localeCompare(valueA);
        }
    });

    // Update the table display
    updateSurveyTable();
}

function updateSortIndicators(column, direction) {
    // Reset all sort indicators
    document.querySelectorAll('.sortable').forEach(header => {
        header.classList.remove('sort-active', 'sort-asc', 'sort-desc');
    });

    // Set active sort indicator
    if (column && direction) {
        const columnHeader = document.querySelector(`th[data-column="${column}"]`);
        if (columnHeader) {
            columnHeader.classList.add('sort-active');
            columnHeader.classList.add(`sort-${direction}`);
        }
    }
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

            // Extract status and completion date from status cell
            const statusCell = row.cells[4];
            const statusBadge = statusCell.querySelector('.status-badge');
            const status = statusBadge ? statusBadge.textContent.trim() : '';
            const completionDateElement = statusCell.querySelector('.completion-date');
            const completedDate = completionDateElement ? completionDateElement.textContent.trim() : null;

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

            return { id, name, leaderName, leaderId, area, monthYear, dateText, status, completedDate };
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

async function updateCategoryStatistics() {
    // Show loading state
    Object.values(categoryData).forEach(category => {
        const card = document.querySelector(`.category-card[data-category-id="${category.id}"]`);
        if (!card) return;

        card.querySelector('.yesno-count').textContent = '...';
        card.querySelector('.yesno-percentage').textContent = '...';
        card.querySelector('.score-count').textContent = '...';
        card.querySelector('.score-average').textContent = '...';
    });

    try {
        // Build query parameters
        const params = new URLSearchParams();
        if (selectedLeaderId) {
            params.append('leaderId', selectedLeaderId);
        }
        if (selectedArea) {
            params.append('area', selectedArea);
        }
        if (startDate) {
            params.append('startDate', startDate.toISOString());
        }
        if (endDate) {
            params.append('endDate', endDate.toISOString());
        }

        // Fetch statistics from the API
        const response = await fetch(`/api/Categories/Statistics?${params.toString()}`);
        if (!response.ok) {
            throw new Error('Failed to fetch category statistics');
        }

        const data = await response.json();
        console.log('Category statistics:', data);

        // Update the UI with the fetched data
        data.statistics.forEach(stat => {
            const category = stat.category;
            const card = document.querySelector(`.category-card[data-category-id="${category.id}"]`);
            if (!card) return;

            // Update Yes/No statistics
            card.querySelector('.yesno-count').textContent = stat.yesNoAnswers || 0;
            card.querySelector('.yesno-percentage').textContent = `${stat.yesPercentage || 0}%`;

            // Update Score statistics
            card.querySelector('.score-count').textContent = stat.scoreAnswers || 0;
            card.querySelector('.score-average').textContent = stat.averageScore || '0.0';

            // Populate questions
            const questionsContainer = card.querySelector('.category-questions');
            questionsContainer.innerHTML = '';

            // Add questions from the API response
            if (stat.questions && stat.questions.length > 0) {
                stat.questions.forEach(question => {
                    const questionItem = document.createElement('div');
                    questionItem.className = 'question-item';
                    questionItem.innerHTML = `
                        <span>${question.text}</span>
                        <span class="question-type-badge question-type-${question.questionType}">
                            ${question.questionType === 'yesno' ? 'Yes/No' : 'Score'}
                        </span>
                    `;
                    questionsContainer.appendChild(questionItem);
                });
            } else {
                // No questions found
                const noQuestionsItem = document.createElement('div');
                noQuestionsItem.className = 'question-item text-muted';
                noQuestionsItem.textContent = 'No questions found for this category';
                questionsContainer.appendChild(noQuestionsItem);
            }
        });
    } catch (error) {
        console.error('Error fetching category statistics:', error);
        showNotification('Error loading category statistics', 'error');

        // Show error state in the UI
        Object.values(categoryData).forEach(category => {
            const card = document.querySelector(`.category-card[data-category-id="${category.id}"]`);
            if (!card) return;

            card.querySelector('.yesno-count').textContent = '0';
            card.querySelector('.yesno-percentage').textContent = '0%';
            card.querySelector('.score-count').textContent = '0';
            card.querySelector('.score-average').textContent = '0.0';

            // Show error message in questions container
            const questionsContainer = card.querySelector('.category-questions');
            questionsContainer.innerHTML = '<div class="text-danger">Failed to load statistics</div>';
        });
    }
}

function initializeDateRangeButtons() {
    const dateRangeButtons = document.querySelectorAll('.date-range-btn');
    const customDateRange = document.getElementById('customDateRange');

    // Set initial date range to 'all' (no filtering)
    startDate = null;
    endDate = null;

    // Set the 'all' button as active by default
    const allButton = document.querySelector('.date-range-btn[data-range="all"]');
    if (allButton) {
        allButton.classList.add('active');
    }

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

                // Get the date input elements
                const startDateInput = document.getElementById('startDate');
                const endDateInput = document.getElementById('endDate');

                // If the inputs don't have values, set default values (first day of month to today)
                if (!startDateInput.value) {
                    startDateInput.value = firstDayOfMonth;
                }

                if (!endDateInput.value) {
                    endDateInput.value = today;
                }

                // Use the input values to set the date range
                if (startDateInput.value && endDateInput.value) {
                    startDate = new Date(startDateInput.value);
                    startDate.setHours(0, 0, 0, 0); // Start of day

                    endDate = new Date(endDateInput.value);
                    endDate.setHours(23, 59, 59, 999); // End of day

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

                    // Clear the date inputs
                    startDateInput.value = '';
                    endDateInput.value = '';
                } else if (range === 'current-month') {
                    // First day of current month
                    startDate = new Date(currentYear, currentMonth, 1);
                    startDate.setHours(0, 0, 0, 0);

                    // Current day of current month
                    endDate = new Date();
                    endDate.setHours(23, 59, 59, 999);

                    // Update the date inputs to match
                    const startMonth = (currentMonth + 1).toString().padStart(2, '0');
                    const startDay = '01';
                    startDateInput.value = `${currentYear}-${startMonth}-${startDay}`;

                    const endMonth = (currentMonth + 1).toString().padStart(2, '0');
                    const endDay = now.getDate().toString().padStart(2, '0');
                    endDateInput.value = `${currentYear}-${endMonth}-${endDay}`;

                    console.log('Current month range:', startDate, 'to', endDate);
                } else if (range === 'last-3-months') {
                    // First day of 2 months ago
                    const threeMonthsAgo = new Date(currentYear, currentMonth - 2, 1);
                    startDate = threeMonthsAgo;
                    startDate.setHours(0, 0, 0, 0);

                    // Current day
                    endDate = new Date();
                    endDate.setHours(23, 59, 59, 999);

                    // Update the date inputs to match
                    const startYear = threeMonthsAgo.getFullYear();
                    const startMonth = (threeMonthsAgo.getMonth() + 1).toString().padStart(2, '0');
                    const startDay = '01';
                    startDateInput.value = `${startYear}-${startMonth}-${startDay}`;

                    const endMonth = (currentMonth + 1).toString().padStart(2, '0');
                    const endDay = now.getDate().toString().padStart(2, '0');
                    endDateInput.value = `${currentYear}-${endMonth}-${endDay}`;

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

    // Set initial values to current date range (but don't show them until custom is clicked)
    const now = new Date();
    const currentYear = now.getFullYear();
    const currentMonth = (now.getMonth() + 1).toString().padStart(2, '0'); // +1 because months are 0-indexed
    const currentDay = now.getDate().toString().padStart(2, '0');

    // Prepare values but don't set them yet
    const firstDayOfMonth = `${currentYear}-${currentMonth}-01`;
    const today = `${currentYear}-${currentMonth}-${currentDay}`;

    // We'll set these values when the custom button is clicked
    // startDateInput.value = firstDayOfMonth;
    // endDateInput.value = today;

    startDateInput.addEventListener('change', function() {
        if (this.value) {
            startDate = new Date(this.value);
            // Set time to beginning of day
            startDate.setHours(0, 0, 0, 0);
            console.log('Custom start date set to:', startDate);

            // Make sure the active class is applied to the custom button
            const customButton = document.querySelector('.date-range-btn[data-range="custom"]');
            if (customButton) {
                document.querySelectorAll('.date-range-btn').forEach(btn => btn.classList.remove('active'));
                customButton.classList.add('active');
            }

            applyFilters();
        }
    });

    endDateInput.addEventListener('change', function() {
        if (this.value) {
            endDate = new Date(this.value);
            // Set time to end of day
            endDate.setHours(23, 59, 59, 999);
            console.log('Custom end date set to:', endDate);

            // Make sure the active class is applied to the custom button
            const customButton = document.querySelector('.date-range-btn[data-range="custom"]');
            if (customButton) {
                document.querySelectorAll('.date-range-btn').forEach(btn => btn.classList.remove('active'));
                customButton.classList.add('active');
            }

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

    // Set default sort by date (descending) to match server-side default
    sortTable('date', 'desc');

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

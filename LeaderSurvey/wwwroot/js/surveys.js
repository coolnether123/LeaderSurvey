// Global variables
let activeFilters = new Map();
let questionCount = 0;

// Initialize Sortable for questions
document.addEventListener('DOMContentLoaded', function() {
    initializeSortable();
    calculateScrollbarWidth();
});

function initializeSortable() {
    const questionsContainer = document.querySelector('.questions-sortable');
    if (questionsContainer) {
        new Sortable(questionsContainer, {
            animation: 150,
            handle: '.question-item',
            ghostClass: 'question-item-ghost'
        });
    }
}

// Filter Functions
function applyFilters() {
    // Get filter values
    const leaderFilter = document.getElementById('leaderFilter').value;
    const areaFilter = document.getElementById('areaFilter').value;
    const dateFilter = document.getElementById('dateFilter').value;
    const statusFilter = document.getElementById('statusFilter').value;

    // Update active filters
    updateActiveFilters('leader', leaderFilter);
    updateActiveFilters('area', areaFilter);
    updateActiveFilters('date', dateFilter);
    updateActiveFilters('status', statusFilter);

    // Filter table rows
    filterTableRows();
}

function updateActiveFilters(type, value) {
    if (value) {
        activeFilters.set(type, value);
    } else {
        activeFilters.delete(type);
    }
    renderActiveFilterTags();
}

function renderActiveFilterTags() {
    const container = document.getElementById('activeFilters');
    container.innerHTML = '';

    activeFilters.forEach((value, type) => {
        const tag = document.createElement('span');
        tag.className = 'filter-tag';
        tag.innerHTML = `
            ${getFilterLabel(type, value)}
            <i class="bi bi-x-circle" onclick="removeFilter('${type}')"></i>
        `;
        container.appendChild(tag);
    });
}

function getFilterLabel(type, value) {
    switch (type) {
        case 'leader':
            return `Leader: ${document.querySelector(`#leaderFilter option[value="${value}"]`).text}`;
        case 'area':
            return `Area: ${value}`;
        case 'date':
            return `Date: ${new Date(value + '-01').toLocaleDateString('en-US', { month: 'long', year: 'numeric' })}`;
        case 'status':
            return `Status: ${value.charAt(0).toUpperCase() + value.slice(1)}`;
        default:
            return '';
    }
}

function removeFilter(type) {
    document.getElementById(`${type}Filter`).value = '';
    activeFilters.delete(type);
    renderActiveFilterTags();
    filterTableRows();
}

function filterTableRows() {
    const rows = document.querySelectorAll('#surveysTable tbody tr');

    rows.forEach(row => {
        let visible = true;

        activeFilters.forEach((value, type) => {
            const rowValue = row.dataset[type];
            if (rowValue !== value) {
                visible = false;
            }
        });

        row.style.display = visible ? '' : 'none';
    });
}

// Survey Editor Functions
function createNewSurvey() {
    document.getElementById('editorTitle').textContent = 'New Survey';
    document.getElementById('surveyForm').reset();
    document.getElementById('questionsContainer').innerHTML = '';
    questionCount = 0;
    updateQuestionCounter();
    showSurveyEditor();
}

function editSurvey(surveyId) {
    // TODO: Fetch survey data and populate form
    document.getElementById('editorTitle').textContent = 'Edit Survey';
    showSurveyEditor();
}

function showSurveyEditor() {
    document.getElementById('surveyEditor').style.display = 'block';
    document.body.style.overflow = 'hidden';
}

function closeSurveyEditor() {
    document.getElementById('surveyEditor').style.display = 'none';
    document.body.style.overflow = '';
}

// Question Management
function addQuestion() {
    if (questionCount >= 10) {
        showNotification('Maximum 10 questions allowed', 'warning');
        return;
    }

    questionCount++;
    const questionHtml = `
        <div class="question-item" data-question-id="${questionCount}">
            <div class="question-content">
                <input type="text" class="form-control" 
                       placeholder="Enter question text" 
                       name="questions[${questionCount}]" required>
            </div>
            <div class="question-actions">
                <button type="button" class="cfa-btn cfa-btn-sm" 
                        onclick="removeQuestion(this)">
                    <i class="bi bi-trash"></i>
                </button>
            </div>
        </div>
    `;

    document.getElementById('questionsContainer').insertAdjacentHTML('beforeend', questionHtml);
    updateQuestionCounter();
}

function removeQuestion(button) {
    button.closest('.question-item').remove();
    questionCount--;
    updateQuestionCounter();
}

function updateQuestionCounter() {
    document.getElementById('questionCounter').textContent = `${questionCount}/10`;
}

// Utility Functions
function showNotification(message, type = 'info') {
    // TODO: Implement notification system
    alert(message);
}

function applyLeaderFilter(leaderId) {
    document.getElementById('leaderFilter').value = leaderId;
    applyFilters();
}
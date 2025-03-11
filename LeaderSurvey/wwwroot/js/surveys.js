// Global variables
let currentSurvey = null;

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

        if (response.ok) {
            const row = document.querySelector(`tr[data-id="${id}"]`);
            if (row) {
                row.remove();
            }
            showNotification('Survey deleted successfully', 'success');
        } else {
            const errorData = await response.json();
            showNotification(errorData.message || 'Failed to delete survey', 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showNotification('An error occurred while deleting the survey', 'error');
    }
}

function viewSurvey(id) {
    const viewerTitle = document.getElementById('viewerTitle');
    if (viewerTitle) {
        viewerTitle.textContent = 'View Survey';
    }
    
    loadSurvey(id);
    showSurveyViewer();
}

function editSurvey(id) {
    const viewerTitle = document.getElementById('viewerTitle');
    if (viewerTitle) {
        viewerTitle.textContent = 'Edit Survey';
    }
    
    loadSurvey(id);
    showSurveyViewer();
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

async function loadSurvey(id) {
    try {
        const response = await fetch(`/api/Surveys/${id}`);
        if (!response.ok) throw new Error('Failed to load survey');
        
        currentSurvey = await response.json();
        
        // Populate form fields
        document.getElementById('surveyId').value = currentSurvey.id;
        document.getElementById('surveyName').value = currentSurvey.name;
        document.getElementById('surveyArea').value = currentSurvey.area;
        document.getElementById('surveyMonthYear').value = currentSurvey.monthYear?.substring(0, 7) || '';
        
        // Load questions
        const container = document.getElementById('questionsContainer');
        container.innerHTML = '';
        window.questionCount = currentSurvey.questions.length;
        
        currentSurvey.questions.forEach((question, index) => {
            addExistingQuestion(question, index + 1);
        });
        
        updateQuestionCounter();
        updatePreview();
    } catch (error) {
        showNotification('Failed to load survey', 'error');
    }
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

// Initialize modal when document is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeModal();
    initializePreviewTab();
});

function initializeModal() {
    const modalElement = document.getElementById('surveyViewerModal');
    if (!modalElement) {
        console.error('Survey modal element not found');
        return;
    }
    surveyModal = new bootstrap.Modal(modalElement);
}

function initializePreviewTab() {
    const previewTab = document.getElementById('preview-tab');
    if (previewTab) {
        previewTab.addEventListener('click', updatePreview);
    }
}

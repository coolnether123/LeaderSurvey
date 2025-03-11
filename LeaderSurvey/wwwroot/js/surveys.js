// Global variables
let activeFilters = new Map();

// Only declare questionCount if it doesn't exist
if (typeof window.questionCount === 'undefined') {
    window.questionCount = 0;
}

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

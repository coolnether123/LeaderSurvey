// Global variables
let questions = [];

function getAddQuestionButton() {
    return document.querySelector('[data-role="add-question"]');
}

// Initialize everything when the document is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize Sortable
    if (document.getElementById('questionsContainer')) {
        new Sortable(document.getElementById('questionsContainer'), {
            animation: 150,
            handle: '.drag-handle',
            onEnd: function() {
                updateQuestionNumbers();
                // Reorder the questions array to match the new DOM order
                const rows = document.querySelectorAll('#questionsContainer tr');
                const newQuestions = [];
                rows.forEach(row => {
                    const index = parseInt(row.getAttribute('data-index'));
                    if (!isNaN(index)) {
                        newQuestions.push(questions[index]);
                    }
                });
                questions = newQuestions;
            }
        });
    }

    // Initialize form submission handling
    const surveyForm = document.getElementById('surveyForm');
    if (surveyForm) {
        surveyForm.addEventListener('submit', function(e) {
            if (questions.length === 0) {
                e.preventDefault();
                alert('Please add at least one question to the survey.');
                return false;
            }
            return true;
        });
    }

    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });
    
    updateQuestionCounter();
});

window.addNewQuestionRow = function(e) {
    if (e) e.preventDefault();
    
    if (questions.length >= 10) {
        alert('Maximum 10 questions allowed');
        return;
    }

    const tbody = document.getElementById('questionsContainer');
    const questionNumber = questions.length + 1;
    
    const tr = document.createElement('tr');
    tr.dataset.isNew = 'true';
    tr.dataset.index = questions.length.toString();
    
    tr.innerHTML = `
        <td>${questionNumber}</td>
        <td>
            <input type="text" class="form-control" placeholder="Enter question text" required>
        </td>
        <td>
            <select class="form-select">
                <option value="yesno">Yes/No</option>
                <option value="score">Score (0-10)</option>
            </select>
        </td>
        <td>
            <button type="button" class="btn btn-sm btn-success me-2" onclick="saveQuestionRow(this)">
                <i class="bi bi-check-lg"></i>
            </button>
            <button type="button" class="btn btn-sm btn-danger" onclick="removeQuestionRow(this)">
                <i class="bi bi-x-lg"></i>
            </button>
        </td>
        <td>
            <i class="bi bi-grip-vertical drag-handle"></i>
        </td>
    `;
    
    tbody.appendChild(tr);
    tr.querySelector('input').focus({preventScroll: true});
    
    // Only disable the Add Question button while editing
    const addButton = getAddQuestionButton();
    if (addButton) addButton.disabled = true;
    
    updateQuestionCounter();
};

window.saveQuestionRow = function(button) {
    const row = button.closest('tr');
    const input = row.querySelector('input');
    const select = row.querySelector('select');
    
    if (!input.value.trim()) {
        input.classList.add('is-invalid');
        return;
    }

    const questionText = input.value.trim();
    const questionType = select.value;
    const index = parseInt(row.dataset.index);
    
    if (row.dataset.isNew === 'true') {
        questions.push({
            text: questionText,
            type: questionType
        });
        row.dataset.isNew = 'false';
    } else {
        questions[index] = {
            text: questionText,
            type: questionType
        };
    }

    row.innerHTML = `
        <td>${row.cells[0].textContent}</td>
        <td>${questionText}</td>
        <td>${questionType === 'yesno' ? 'Yes/No' : 'Score (0-10)'}</td>
        <td>
            <button type="button" class="btn btn-sm btn-outline-primary me-2" onclick="editQuestionRow(this)">
                <i class="bi bi-pencil"></i>
            </button>
            <button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteQuestionRow(this)">
                <i class="bi bi-trash"></i>
            </button>
        </td>
        <td>
            <i class="bi bi-grip-vertical drag-handle"></i>
        </td>
    `;

    // Re-enable the Add Question button after saving
    const addButton = getAddQuestionButton();
    if (addButton) addButton.disabled = false;
    
    updateQuestionCounter();
    updateQuestionNumbers();
    updateQuestionsInput();
};

window.editQuestionRow = function(button) {
    const row = button.closest('tr');
    const index = parseInt(row.dataset.index);
    const question = questions[index];
    
    row.innerHTML = `
        <td>${row.cells[0].textContent}</td>
        <td>
            <input type="text" class="form-control" value="${question.text}" required>
        </td>
        <td>
            <select class="form-select">
                <option value="yesno" ${question.type === 'yesno' ? 'selected' : ''}>Yes/No</option>
                <option value="score" ${question.type === 'score' ? 'selected' : ''}>Score (0-10)</option>
            </select>
        </td>
        <td>
            <button type="button" class="btn btn-sm btn-success me-2" onclick="saveQuestionRow(this)">
                <i class="bi bi-check-lg"></i>
            </button>
            <button type="button" class="btn btn-sm btn-danger" onclick="cancelEdit(this, ${index})">
                <i class="bi bi-x-lg"></i>
            </button>
        </td>
        <td>
            <i class="bi bi-grip-vertical drag-handle"></i>
        </td>
    `;
    
    row.querySelector('input').focus();
    document.getElementById('addQuestionBtn').disabled = true;
};

window.cancelEdit = function(button, index) {
    const row = button.closest('tr');
    const question = questions[index];
    
    row.innerHTML = `
        <td>${row.cells[0].textContent}</td>
        <td>${question.text}</td>
        <td>${question.type === 'yesno' ? 'Yes/No' : 'Score (0-10)'}</td>
        <td>
            <button type="button" class="btn btn-sm btn-outline-primary me-2" onclick="editQuestionRow(this)">
                <i class="bi bi-pencil"></i>
            </button>
            <button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteQuestionRow(this)">
                <i class="bi bi-trash"></i>
            </button>
        </td>
        <td>
            <i class="bi bi-grip-vertical drag-handle"></i>
        </td>
    `;
    
    document.getElementById('addQuestionBtn').disabled = false;
};

window.removeQuestionRow = function(button) {
    const row = button.closest('tr');
    
    // If this is a saved question (not a new one being edited)
    if (row.dataset.isNew !== 'true') {
        // Remove the question from the questions array
        const index = parseInt(row.dataset.index);
        questions.splice(index, 1);
    }
    
    row.remove();
    
    // Re-enable the Add Question button
    const addButton = getAddQuestionButton();
    if (addButton) addButton.disabled = false;
    
    updateQuestionCounter();
    updateQuestionNumbers();
    updateQuestionsInput(); // If you have this function to sync with hidden form fields
};

window.deleteQuestionRow = function(button) {
    const row = button.closest('tr');
    if (!row) return;

    // Remove the row
    row.remove();

    // Update the question counter
    updateQuestionCounter();

    // Reorder remaining questions
    reorderQuestions();
};

function reorderQuestions() {
    const rows = document.querySelectorAll('#questionsContainer tr');
    rows.forEach((row, index) => {
        row.querySelector('td:first-child').textContent = (index + 1).toString();
    });
}

function updateQuestionNumbers() {
    const rows = document.querySelectorAll('#questionsContainer tr');
    rows.forEach((row, index) => {
        const numberCell = row.cells[0];
        if (numberCell) {
            numberCell.textContent = (index + 1).toString();
        }
        // Update the data-index attribute to match the new order
        row.dataset.index = index.toString();
    });
}

function updateQuestionCounter() {
    const totalQuestions = document.querySelectorAll('#questionsContainer tr').length;
    const counterElement = document.getElementById('question-counter');
    if (counterElement) {
        counterElement.textContent = `${totalQuestions}/10`;
    }
}

function updateQuestionsInput() {
    let questionsInput = document.getElementById('questionsInput');
    if (!questionsInput) {
        questionsInput = document.createElement('input');
        questionsInput.type = 'hidden';
        questionsInput.name = 'Survey.Questions';
        questionsInput.id = 'questionsInput';
        document.getElementById('surveyForm').appendChild(questionsInput);
    }
    questionsInput.value = JSON.stringify(questions);
}

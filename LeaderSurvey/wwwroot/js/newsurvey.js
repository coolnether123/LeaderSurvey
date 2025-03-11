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
    
    // Disable the Add Question button while editing
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
            <button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteQuestion(this)">
                <i class="bi bi-trash"></i>
            </button>
        </td>
        <td>
            <i class="bi bi-grip-vertical drag-handle"></i>
        </td>
    `;

    // Re-enable the Add Question button
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
            <button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteQuestion(this)">
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
    row.remove();
    
    // Re-enable the Add Question button
    const addButton = getAddQuestionButton();
    if (addButton) addButton.disabled = false;
    
    updateQuestionCounter();
    updateQuestionNumbers();
};

window.deleteQuestion = function(button) {
    if (confirm('Are you sure you want to delete this question?')) {
        const row = button.closest('tr');
        const index = parseInt(row.dataset.index);
        questions.splice(index, 1);
        row.remove();
        
        // Update indices for remaining rows
        const rows = document.querySelectorAll('#questionsContainer tr');
        rows.forEach((row, idx) => {
            row.dataset.index = idx.toString();
        });
        
        updateQuestionNumbers();
        updateQuestionCounter();
        updateQuestionsInput();
    }
};

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
    const counter = document.getElementById('question-counter');
    const currentCount = questions.length;
    counter.textContent = `${currentCount}/10`;
    
    // Update Add Question button state
    const addButton = getAddQuestionButton();
    if (addButton) {
        addButton.disabled = currentCount >= 10 || document.querySelector('#questionsContainer tr[data-is-new="true"]') !== null;
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

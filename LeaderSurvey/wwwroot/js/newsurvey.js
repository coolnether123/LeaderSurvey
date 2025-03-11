// Global variables
let questions = [];
let questionModal = null;

// Initialize everything when the document is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize the Bootstrap modal
    questionModal = new bootstrap.Modal(document.getElementById('questionModal'));
    
    // Initialize Sortable
    if (document.getElementById('questionsContainer')) {
        new Sortable(document.getElementById('questionsContainer'), {
            animation: 150,
            handle: '.drag-handle',
            onEnd: updateQuestionNumbers
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
});

// Function to show the add question modal
window.showAddQuestionModal = function() {
    if (questions.length >= 10) {
        alert('Maximum 10 questions allowed');
        return;
    }
    
    document.getElementById('questionModalLabel').textContent = 'Add New Question';
    document.getElementById('questionForm').reset();
    document.getElementById('questionIndex').value = '';
    questionModal.show();
};

// Function to save the question
window.saveQuestion = function() {
    const form = document.getElementById('questionForm');
    
    if (!form.checkValidity()) {
        form.classList.add('was-validated');
        return;
    }

    const questionText = document.getElementById('questionText').value.trim();
    const questionType = document.getElementById('questionType').value;
    const questionIndex = document.getElementById('questionIndex').value;

    if (questionIndex === '') {
        // Add new question
        addQuestionToTable({
            text: questionText,
            type: questionType
        });
    } else {
        // Update existing question
        updateQuestionInTable(parseInt(questionIndex), {
            text: questionText,
            type: questionType
        });
    }

    questionModal.hide();
    form.classList.remove('was-validated');
};

function addQuestionToTable(question) {
    const questionNumber = questions.length + 1;
    questions.push(question);

    const tbody = document.getElementById('questionsContainer');
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td>${questionNumber}</td>
        <td>${question.text}</td>
        <td>${question.type === 'yesno' ? 'Yes/No' : 'Score (0-10)'}</td>
        <td>
            <button type="button" class="btn btn-sm btn-outline-primary me-2" onclick="editQuestion(${questions.length - 1})">
                <i class="bi bi-pencil"></i>
            </button>
            <button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteQuestion(${questions.length - 1})">
                <i class="bi bi-trash"></i>
            </button>
        </td>
        <td>
            <i class="bi bi-grip-vertical drag-handle"></i>
        </td>
    `;
    tbody.appendChild(tr);
    
    updateQuestionCounter();
    updateQuestionsInput();
}

window.editQuestion = function(index) {
    const question = questions[index];
    document.getElementById('questionModalLabel').textContent = 'Edit Question';
    document.getElementById('questionText').value = question.text;
    document.getElementById('questionType').value = question.type;
    document.getElementById('questionIndex').value = index;
    questionModal.show();
};

window.deleteQuestion = function(index) {
    if (confirm('Are you sure you want to delete this question?')) {
        questions.splice(index, 1);
        updateQuestionTable();
        updateQuestionCounter();
        updateQuestionsInput();
    }
};

function updateQuestionTable() {
    const tbody = document.getElementById('questionsContainer');
    tbody.innerHTML = '';
    questions.forEach((question, index) => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${index + 1}</td>
            <td>${question.text}</td>
            <td>${question.type === 'yesno' ? 'Yes/No' : 'Score (0-10)'}</td>
            <td>
                <button type="button" class="btn btn-sm btn-outline-primary me-2" onclick="editQuestion(${index})">
                    <i class="bi bi-pencil"></i>
                </button>
                <button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteQuestion(${index})">
                    <i class="bi bi-trash"></i>
                </button>
            </td>
            <td>
                <i class="bi bi-grip-vertical drag-handle"></i>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function updateQuestionCounter() {
    const counter = document.getElementById('question-counter');
    counter.textContent = `${questions.length}/10`;
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

function updateQuestionNumbers() {
    const rows = document.querySelectorAll('#questionsContainer tr');
    rows.forEach((row, index) => {
        row.cells[0].textContent = (index + 1).toString();
    });
}

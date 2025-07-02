// Questions handling functions
function initializeQuestions() {
    if (typeof window.questionCount === 'undefined') {
        window.questionCount = 0;
    }
    updateQuestionCounter();
    initializeSortable();
}

function addQuestion() {
    if (window.questionCount >= 10) {
        showNotification("Maximum of 10 questions reached!", "warning");
        return;
    }

    window.questionCount++;
    updateQuestionCounter();

    const questionItem = document.createElement('div');
    questionItem.className = 'question-item d-flex align-items-center';
    questionItem.innerHTML = `
        <i class="bi bi-grip-vertical question-drag-handle"></i>
        <div class="question-number">${window.questionCount}</div>
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

    document.getElementById('questions-container').appendChild(questionItem);
    initializeSortable();
}

function deleteQuestion(button) {
    if (confirm('Are you sure you would like to delete this question?')) {
        const questionItem = button.closest('.question-item');
        const myPleasure = questionItem.querySelector('.my-pleasure');
        
        myPleasure.style.opacity = '1';
        questionItem.classList.add('deleting');
        
        setTimeout(() => {
            questionItem.remove();
            window.questionCount--;
            updateQuestionCounter();
            renumberQuestions();
        }, 500);
    }
}

function updateQuestionCounter() {
    document.getElementById('question-counter').textContent = `${window.questionCount}/10`;
}

function renumberQuestions() {
    const questions = document.querySelectorAll('.question-number');
    questions.forEach((question, index) => {
        question.textContent = index + 1;
    });
}

function initializeSortable() {
    new Sortable(document.getElementById('questions-container'), {
        animation: 150,
        handle: '.question-drag-handle',
        ghostClass: 'sortable-ghost',
        onEnd: renumberQuestions
    });
}

// Initialize sortable when the page loads
document.addEventListener('DOMContentLoaded', function() {
    initializeQuestions();
});